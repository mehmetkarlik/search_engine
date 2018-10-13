using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PageRankCalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string connectionString = @"server=localhost;userid=root;password=karlik;database=search_engine;SslMode=none";
                MySqlConnection con = new MySqlConnection(connectionString);
                MySqlCommand command = new MySqlCommand();
                command.Connection = con;
                MySqlDataAdapter adp = new MySqlDataAdapter();
                command.CommandText = "Select count(*) from pagecollection";
                DataTable dtCount = new DataTable();
                adp.SelectCommand = command;
                adp.Fill(dtCount);
                int total = int.Parse(dtCount.Rows[0][0].ToString());
                int baslangic = 0;
                while (baslangic != total)
                {
                    MySqlConnection cn = new MySqlConnection(connectionString);
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = cn;
                        MySqlDataAdapter ad = new MySqlDataAdapter();
                        cmd.CommandText = "Select * from pagecollection where counted = 0 LIMIT 1000";
                        DataTable dtx = new DataTable();
                        ad.SelectCommand = cmd;
                        ad.Fill(dtx);


                        if (dtx.Rows.Count == 0)
                        {
                            //return;
                            cn.Open();
                            using (MySqlCommand cmd3 = new MySqlCommand())
                            {
                                cmd3.Connection = cn;
                                cmd3.CommandText = "UPDATE pagecollection SET counted = 0";
                                cmd3.CommandTimeout = 60000;
                                cmd3.ExecuteNonQuery();
                                cn.Close();
                                baslangic++;
                                continue;
                            }

                        }

                        foreach (DataRow dr in dtx.Rows)
                        {
                            try
                            {
                                using (MySqlCommand cmd2 = new MySqlCommand())
                                {
                                    cmd2.Connection = cn;
                                    cmd2.CommandText = "SELECT * FROM links WHERE link = '" + dr["url"].ToString() + "'";
                                    DataTable dt = new DataTable();
                                    ad.SelectCommand = cmd2;
                                    ad.Fill(dt);


                                    //Formül 0.15 + 0.85 * (pr(p1) / c(p1) + pr(p2) / c(p2) + ...)
                                    double lastPagerank = 0.15f;
                                    foreach (DataRow drLink in dt.Rows)
                                    {
                                        try
                                        {
                                            using (MySqlCommand cmd3 = new MySqlCommand())
                                            {
                                                cmd3.Connection = cn;
                                                cmd3.CommandText = "Select * from pagecollection WHERE url = '" + drLink["url"].ToString() + "'";
                                                DataTable dt2 = new DataTable();
                                                ad.SelectCommand = cmd3;

                                                ad.Fill(dt2);

                                                if (dt2.Rows.Count == 0)
                                                {
                                                        continue;
                                                }

                                                double linkCount = Double.Parse(dt2.Rows[0]["linkCount"].ToString());
                                                double pageRank = Double.Parse(dt2.Rows[0]["pagerank"].ToString());


                                                if (linkCount == 0)
                                                    continue;
                                                else
                                                {
                                                    lastPagerank += 0.85f * (pageRank / linkCount);
                                                }
                                            }

                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine(drLink["url"].ToString() + " " + ex.Message + ex.StackTrace);
                                            continue;
                                        }

                                    }
                                    
                                    using (MySqlCommand cmd4 = new MySqlCommand())
                                    {
                                        cn.Open();
                                        cmd4.Connection = cn;
                                        cmd4.CommandText = "UPDATE pagecollection SET counted = 1, pagerank= @pagerank WHERE id = '" + dr["id"].ToString() + "'";
                                        cmd4.Parameters.AddWithValue("@pagerank", lastPagerank);
                                        cmd4.ExecuteNonQuery();
                                        cn.Close();
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message + " " + ex.StackTrace);
                                cn.Open();
                                using (MySqlCommand cmd4 = new MySqlCommand())
                                {
                                    cmd4.Connection = cn;
                                    cmd4.CommandText = "UPDATE pagecollection SET counted = 1 WHERE id = '" + dr["id"].ToString() + "'";
                                    cmd4.ExecuteNonQuery();
                                    cn.Close();
                                }
                                
                            }
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + " " + ex.StackTrace);

            }
        }

    }
}
