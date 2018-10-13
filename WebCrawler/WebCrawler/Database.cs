using Microsoft.TeamFoundation.WorkItemTracking.Controls;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebCrawler
{
    public class LinkCollection
    {
        public string Url { get; set; }
    }
    public class Database
    {
        public static string connectionString = @"server=localhost;userid=root;password=karlik;database=search_engine;pooling=true;max pool size=10000;";
        public static DataTable get1000RecordInLinkCollection()
        {
            MySqlConnection connection = new MySqlConnection(connectionString);
            if (connection.State != ConnectionState.Open)
                connection.Open();
            string stm = "set net_write_timeout=99999; set net_read_timeout=99999;" + "SELECT * FROM linkcollection Where Limit 1000 ";
            MySqlDataAdapter dataAdapter = new MySqlDataAdapter();
            dataAdapter.SelectCommand = new MySqlCommand(stm, connection);
            DataTable table = new DataTable();
            dataAdapter.Fill(table);
            connection.Close();
            return table;
        }

        public static void addLink(string url, string link, MySqlConnection connection)
        {
            try
            {

                if (link != null)
                {
                    bool varmi = Database.getRecordInLinks(url, link, connection);
                    if (varmi == false)
                    {
                        using (MySqlCommand cmd = new MySqlCommand())
                        {
                            cmd.Connection = connection;
                            cmd.CommandTimeout = 60000;
                            if (connection.State != ConnectionState.Open)
                                connection.Open();
                            cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "INSERT INTO links(url,link) VALUES(@url, @link);";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@url", url);
                            cmd.Parameters.AddWithValue("@link", link);
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                    }

                    varmi = Database.getRecordInLinkCollection(link, connection);
                    if (varmi == false)
                    {
                        using (MySqlCommand cmd = new MySqlCommand())
                        {
                            cmd.Connection = connection;
                            cmd.CommandTimeout = 60000;
                            if (connection.State != ConnectionState.Open)
                                connection.Open();
                            cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "INSERT INTO linkcollection (url) VALUES (@link);";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@link", link);
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }
                else
                {
                    bool varmi = Database.getRecordInLinkCollection(link, connection);
                    if (varmi == false)
                    {
                        using (MySqlCommand cmd = new MySqlCommand())
                        {
                            cmd.Connection = connection;
                            cmd.CommandTimeout = 60000;
                            if (connection.State != ConnectionState.Open)
                                connection.Open();
                            cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "INSERT INTO linkcollection (url) VALUES (@link);";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@link", link);
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("");
                Debug.WriteLine("### addLink ### " + "Url: " + url + " Link: " + link + " Connection: " + connection.State.ToString());
                Debug.WriteLine(ex.Message);
                connection.Close();
            }

        }

        private static bool getRecordInLinks(string url, string link, MySqlConnection connection)
        {
            bool varmi = false;
            {
                try
                {
                    string stm = "SELECT * FROM links where url=@url and link=@link;";

                    using (MySqlCommand cmd = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999;" + stm, connection))
                    {
                        cmd.CommandTimeout = 60000;
                        cmd.Parameters.AddWithValue("@url", url);
                        cmd.Parameters.AddWithValue("@link", link);

                        if (connection.State != ConnectionState.Open)
                            connection.Open();
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.HasRows)
                            {
                                varmi = true;
                                break;
                            }
                            reader.Close();
                        }
                        connection.Close();
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("");
                    Debug.WriteLine("### getRecordInLinks ###" + "Url: " + url + " Link: " + link + " Connection: " + connection.State.ToString());
                    Debug.WriteLine(ex.Message);
                    connection.Close();
                }
            }

            return varmi;
        }

        public static string addPage(string url, DateTime? tarih, string htmlIcerik, int linkCount, MySqlConnection connection)
        {
            double pagerank = 0.15f;
            string titleString = "", icerik = "";
            DateTime? indekslemeTarihi = null, kayitTarihi = tarih;
            bool indekslemeYapiliyormu = false;
            string str = "";
            int counted = 0;

            try
            {
                DataTable table = new DataTable();
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = connection;
                    cmd.CommandTimeout = 60000;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    string stm = "set net_write_timeout=99999; set net_read_timeout=99999;" + "SELECT * FROM pagecollection where url=@url;";
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter();
                    cmd.CommandText = stm;
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@url", url);
                    dataAdapter.SelectCommand = cmd;
                    dataAdapter.Fill(table);
                    connection.Close();
                }
                using (MySqlCommand cmd = new MySqlCommand())
                {

                    if (table.Rows.Count != 0)
                    {
                        counted = int.Parse(table.Rows[0]["counted"].ToString());
                        cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "UPDATE pagecollection SET title = @title,htmlicerik = @htmlicerik,icerik = @icerik,pagerank = @pagerank, kayittarihi = @kayittarihi,indekslemetarihi = @indekslemetarihi,indekslemeyapiliyormu = @indekslemeyapiliyormu, linkCount = @linkCount, counted = @counted WHERE url=@url;";
                    }
                    else
                    {
                        cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "INSERT INTO pagecollection(url,title,htmlicerik,icerik,pagerank,kayittarihi,indekslemetarihi,indekslemeyapiliyormu,linkcount,counted) VALUES(@url,@title,@htmlicerik,@icerik,@pagerank,@kayittarihi,@indekslemetarihi,@indekslemeyapiliyormu, @linkCount, @counted)";
                    }

                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(htmlIcerik);
                    var title = doc.DocumentNode.SelectSingleNode("//title");
                    icerik = HtmlFilter.ConvertToPlainText(htmlIcerik);

                    icerik = Regex.Replace(icerik, @"\s+", " ");
                    icerik = Regex.Replace(icerik, @"\t+", " ");
                    icerik = Regex.Replace(icerik, @"\r+", " ");

                    //htmlIcerik = Zip(htmlIcerik);
                    //icerik = Zip(icerik);

                    titleString = "";

                    if (title != null)
                    {
                        titleString = title.InnerText;
                    }

                    if (table.Rows.Count != 0)
                    {
                        pagerank = Double.Parse(table.Rows[0]["pagerank"].ToString());
                        Debug.WriteLine(pagerank);
                    }
                    else
                    {
                        pagerank = 0.15f;
                        Debug.WriteLine(pagerank);
                    }


                    cmd.Connection = connection;
                    cmd.CommandTimeout = 60000;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.Parameters.AddWithValue("@title", titleString);
                    cmd.Parameters.AddWithValue("@htmlicerik", htmlIcerik);
                    cmd.Parameters.AddWithValue("@icerik", icerik);
                    cmd.Parameters.AddWithValue("@pagerank", pagerank);
                    cmd.Parameters.AddWithValue("@kayittarihi", kayitTarihi);
                    cmd.Parameters.AddWithValue("@indekslemetarihi", indekslemeTarihi);
                    cmd.Parameters.AddWithValue("@indekslemeyapiliyormu", indekslemeYapiliyormu);
                    cmd.Parameters.AddWithValue("@linkCount", linkCount);
                    cmd.Parameters.AddWithValue("@counted", counted);
                    cmd.ExecuteNonQuery();
                    connection.Close();

                    str = "";
                }
            }
            catch (Exception ex)
            {
                connection.Close();
                try
                {
                    Debug.WriteLine("");
                    Debug.WriteLine("### addPage ###" + "Url: " + url + " Title: " + titleString.Substring(0, 10) + " HtmlIcerik: " + htmlIcerik.Substring(0, 10) + " Icerik: " + icerik.Substring(0, 10) + " PageRank: " + pagerank +
                        " KayitTarihi: " + kayitTarihi + " IndekslemeTarihi: " + indekslemeTarihi + " IndekslemeYapiliyormu: " + indekslemeYapiliyormu + " LinkCount: " + linkCount + " Counted: " + counted + " Connection: " + connection.State.ToString());
                    Debug.WriteLine(ex.Message);
                    str = ex.Message + ex.StackTrace;
                }
                catch (Exception ex2)
                {
                    Debug.WriteLine("### addPage ###" + "Url: " + url + " Connection: " + connection.State.ToString());
                    Debug.WriteLine(ex.Message + ex2.Message);
                }


            }

            return str;
        }

        public static bool getRecordInPageCollection(string url, MySqlConnection connection)
        {
            bool varmi = false;

            try
            {
                string stm = "set net_write_timeout=99999; set net_read_timeout=99999;" + "SELECT * FROM pageCollection where url=@url;";
                using (MySqlCommand cmd = new MySqlCommand(stm, connection))
                {
                    cmd.CommandTimeout = 60000;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@url", url);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.HasRows)
                        {
                            varmi = true;
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
                Debug.WriteLine("### getRecordInPageCollection ###" + "Url: " + url);
                Debug.WriteLine(ex.Message);
            }

            return varmi;
        }


        public static bool getRecordInLinkCollection(string url, MySqlConnection connection)
        {

            bool varmi = false;

            try
            {
                string stm = "set net_write_timeout=99999; set net_read_timeout=99999;" + "SELECT * FROM linkCollection where url=@url;";
                using (MySqlCommand cmd = new MySqlCommand(stm, connection))
                {
                    cmd.CommandTimeout = 60000;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@url", url);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.HasRows)
                        {
                            varmi = true;
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
                Debug.WriteLine("### getRecordInLinkCollection ###" + "Url: " + url);
                Debug.WriteLine(ex.Message);
            }

            return varmi;
        }


        public static void deleteDatasInCollections(MySqlConnection connection)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.CommandTimeout = 60000;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.Connection = connection;
                    cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "delete from pagecollection;";
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.CommandTimeout = 60000;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.Connection = connection;
                    cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "delete from linkcollection;";
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }

                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.CommandTimeout = 60000;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.Connection = connection;
                    cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "delete from links;";
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }

            }
            catch (Exception ex)
            {
                connection.Close();
                Debug.WriteLine("");
                Debug.WriteLine("### deleteDatasInCollections ###");
                Debug.WriteLine(ex.Message);
            }
        }

        public static void deleteLinkInLinkCollections(string url, MySqlConnection connection)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.CommandTimeout = 60000;
                    cmd.Connection = connection;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "delete from linkcollection where url=@url;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                connection.Close();
                Debug.WriteLine("");
                Debug.WriteLine("### deleteLinkInLinkCollections ###" + "Url: " + url);
                Debug.WriteLine(ex.Message);
            }

        }


        public static void deleteLinkInPageCollections(string url, MySqlConnection connection)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.CommandTimeout = 60000;
                    cmd.Connection = connection;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "delete from pagecollection where url=@url;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                connection.Close();
                Debug.WriteLine("");
                Debug.WriteLine("### deleteLinkInPageCollections ###" + "Url: " + url);
                Debug.WriteLine(ex.Message);
            }

        }


        public static void deleteLinkInLinks(string url, MySqlConnection connection)
        {
            try
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.CommandTimeout = 60000;
                    cmd.Connection = connection;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "delete from links where url=@url;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.CommandTimeout = 60000;
                    cmd.Connection = connection;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    cmd.CommandText = "set net_write_timeout=99999; set net_read_timeout=99999;" + "delete from links where link=@url;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                connection.Close();
                Debug.WriteLine("");
                Debug.WriteLine("### deleteLinkInLinks ###" + "Url: " + url);
                Debug.WriteLine(ex.Message);
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
