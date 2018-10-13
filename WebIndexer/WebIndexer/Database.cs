using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace WebIndexer
{
    public class LinkCollection
    {
        public string Url { get; set; }
    }
    public class Database
    {

        public static string connectionString = @"server=localhost;userid=root;password=karlik;database=search_engine;pooling=true;max pool size=1000000; Allow User Variables=True;";

        public static DataTable KuyrukDoldur()
        {
            DataTable dt = new DataTable();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    string stm = "set net_write_timeout=99999; set net_read_timeout=99999;" + "SELECT * FROM search_engine.pagecollection USE INDEX (NewIndex) where (kayitTarihi > indekslemeTarihi OR indekslemeTarihi is null) AND indekslemeyapiliyormu = 0 Limit 100;";
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter();
                    MySqlCommand cmd = new MySqlCommand(stm, connection);
                    cmd.CommandTimeout = 600;
                    dataAdapter.SelectCommand = cmd;
                    dataAdapter.Fill(dt);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("### KuyrukDoldur ###" + ex.Message + ex.StackTrace);
                }
            }
            return dt;
        }

        public static void updatePage(int id, DateTime? indekslemeTarihi, bool indekslemeYapiliyormu, MySqlConnection connection)
        {
                try
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = connection;
                    cmd.CommandTimeout = 600;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                        cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "UPDATE pagecollection SET indekslemetarihi = @indekslemetarihi,indekslemeyapiliyormu = @indekslemeyapiliyormu WHERE id=@id;";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@indekslemetarihi", indekslemeTarihi);
                        cmd.Parameters.AddWithValue("@indekslemeyapiliyormu", indekslemeYapiliyormu);
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                   
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("### updatePage ###" + " PageId: " + id + " Hata: " + ex.Message);
                }
                finally
                {
                    if (connection != null)
                        connection.Close();
                }
                
        }

        public static string getToken(string token, MySqlConnection connection)
        {
            string array = "";
            try
            {
                string stm = "set net_write_timeout=99999; set net_read_timeout=99999;" + "SELECT * FROM search_engine.bigindex where  token=@token;";
                using (MySqlCommand cmd = new MySqlCommand(stm, connection))
                {
                    cmd.CommandTimeout = 600;
                    if(connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@token", token);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            array = reader["array"].ToString();
                            break;
                        }
                        reader.Close();
                    }
                    connection.Close();
                }

            }
            catch (Exception ex)
            {
                connection.Close();
                Debug.WriteLine("");
                Debug.WriteLine("### getToken ###" + "Token: " + token);
                Debug.WriteLine(ex.Message);
                array = "error";
            }

            return array;
        }

        public static void addToken(int docId, Dictionary<string, int> liste, MySqlConnection connection)
        {
            string token = "";
            string etArray = "";
            try
            {
                for (int i = 0; i < liste.Count; i++)
                {
                    token = liste.Keys.ElementAt(i);
                    int frekans = liste.Values.ElementAt(i);

                    string array = getToken(token,connection);
                    
                    if (array != "" && array != "error")
                    {
                        DataSet ds = new DataSet();
                        ds.ReadXml(XmlReader.Create(new StringReader(array)));
                        DataTable dt = ds.Tables[0];
                        if (dt != null)
                        {
                            bool docIdVarmi = false;
                            string id = docId.ToString();
                            foreach (DataRow row in dt.Rows)
                            {
                                if (row["docId"].ToString() == id)
                                {
                                    docIdVarmi = true;
                                    row["frekans"] = frekans;

                                    dt.AcceptChanges();
                                    ds.AcceptChanges();


                                    using (MySqlCommand cmd = new MySqlCommand())
                                    {
                                        cmd.CommandTimeout = 600;
                                        if (connection.State != ConnectionState.Open)
                                            connection.Open();
                                        cmd.Connection = connection;
                                        cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "UPDATE bigindex SET array = @array WHERE token=@token;";
                                        cmd.Prepare();
                                        cmd.Parameters.AddWithValue("@token", token);
                                        etArray = ds.GetXml();
                                        cmd.Parameters.AddWithValue("@array", etArray);
                                        cmd.ExecuteNonQuery();
                                        connection.Close();
                                    }
                                    break;
                                }
                            }

                            if (docIdVarmi == false)
                            {
                                DataRow row = dt.NewRow();
                                row["docId"] = docId;
                                row["frekans"] = frekans;

                                dt.Rows.Add(row);

                                dt.AcceptChanges();
                                ds.AcceptChanges();

                                using (MySqlCommand cmd = new MySqlCommand())
                                {
                                    cmd.CommandTimeout = 600;
                                    if (connection.State != ConnectionState.Open)
                                        connection.Open();
                                    cmd.Connection = connection;
                                    cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "UPDATE bigindex SET array = @array WHERE token=@token;";
                                    cmd.Prepare();
                                    cmd.Parameters.AddWithValue("@token", token);
                                    etArray = ds.GetXml();
                                    cmd.Parameters.AddWithValue("@array", etArray);
                                    cmd.ExecuteNonQuery();
                                    connection.Close();
                                }
                            }
                            //dt yi docId ile kontrol et.
                            //docId yoksa dt den bir datarow üret ve eklemeyi yap.
                            //docId varsa frekans değerini güncelle.
                            //Database de güncelleme metodunu çalıştır.
                        }
                        else
                        {
                            DataTable dTable = new DataTable();

                            dTable.Columns.Add("docId", typeof(int));
                            dTable.Columns.Add("frekans", typeof(int));
                            DataRow row = dTable.NewRow();
                            row["docId"] = docId;
                            row["frekans"] = frekans;
                            dTable.Rows.Add(row);

                            dTable.AcceptChanges();
                            DataSet dsx = new DataSet();
                            dsx.Tables.Add(dTable);
                            dsx.AcceptChanges();

                            using (MySqlCommand cmd = new MySqlCommand())
                            {
                                cmd.CommandTimeout = 600;
                                if (connection.State != ConnectionState.Open)
                                    connection.Open();
                                cmd.Connection = connection;
                                cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "UPDATE bigindex SET array = @array WHERE token=@token;";
                                cmd.Prepare();
                                cmd.Parameters.AddWithValue("@token", token);
                                etArray = dsx.GetXml();
                                cmd.Parameters.AddWithValue("@array", etArray);
                                cmd.ExecuteNonQuery();
                                connection.Close();
                            }
                        }
                    }
                    else if(array == "")
                    {

                        DataTable dTable = new DataTable();

                        dTable.Columns.Add("docId", typeof(int));
                        dTable.Columns.Add("frekans", typeof(int));
                        DataRow row = dTable.NewRow();
                        row["docId"] = docId;
                        row["frekans"] = frekans;
                        dTable.Rows.Add(row);

                        dTable.AcceptChanges();
                        DataSet ds = new DataSet();
                        ds.Tables.Add(dTable);
                        ds.AcceptChanges();

                        using (MySqlCommand cmd = new MySqlCommand())
                        {
                            cmd.CommandTimeout = 600;
                            if (connection.State != ConnectionState.Open)
                                connection.Open();
                            cmd.Connection = connection;
                            cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "INSERT INTO bigindex(token,array) VALUES(@token,@array) ;";
                            cmd.Prepare();
                            if (token.Length > 44)
                            {
                                continue;
                            }
                            cmd.Parameters.AddWithValue("@token", token);
                            etArray = ds.GetXml();
                            cmd.Parameters.AddWithValue("@array", etArray);
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                        
                    }

                }

            }
            catch (Exception ex)
            {
                connection.Close();
                Debug.WriteLine("");
                Debug.WriteLine("### addToken ###" + " docId: " + docId + " Hata: " + ex.Message +" Hata Yeri: " + ex.StackTrace + " ## Token : " + token + " ##Array : " + etArray);
            }
            finally
            {
                connection.Close();
            }

        }
        

        //public static string Zip(string text)
        //{
        //    byte[] buffer = System.Text.Encoding.Unicode.GetBytes(text);
        //    MemoryStream ms = new MemoryStream();
        //    using (System.IO.Compression.GZipStream zip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true))
        //    {
        //        zip.Write(buffer, 0, buffer.Length);
        //    }

        //    ms.Position = 0;
        //    MemoryStream outStream = new MemoryStream();

        //    byte[] compressed = new byte[ms.Length];
        //    ms.Read(compressed, 0, compressed.Length);

        //    byte[] gzBuffer = new byte[compressed.Length + 4];
        //    System.Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
        //    System.Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
        //    return Convert.ToBase64String(gzBuffer);
        //}

        //public static string UnZip(string compressedText)
        //{
        //    byte[] gzBuffer = Convert.FromBase64String(compressedText);
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        int msgLength = BitConverter.ToInt32(gzBuffer, 0);
        //        ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

        //        byte[] buffer = new byte[msgLength];

        //        ms.Position = 0;
        //        using (System.IO.Compression.GZipStream zip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
        //        {
        //            zip.Read(buffer, 0, buffer.Length);
        //        }

        //        return System.Text.Encoding.Unicode.GetString(buffer, 0, buffer.Length);
        //    }
        //}
    }
}
