using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

namespace WebSearcher
{
    public partial class Default : System.Web.UI.Page
    {
        public static string connectionString = @"server=localhost;userid=root;password=karlik;database=search_engine;SslMode=none";

        protected void Page_Load(object sender, EventArgs e)
        {
        }
        DateTime dtMili = new DateTime(), dtMili2 = new DateTime();
        private void BindRepeater()
        {
            try
            {
                DataSet dsCaching = new DataSet();

                dtMili = DateTime.Now;
                
                //Do your database connection stuff and get your data
                MySqlConnection cn = new MySqlConnection(connectionString);
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = cn;
                MySqlDataAdapter ad = new MySqlDataAdapter(cmd);

               

                List<string> list = new List<string>();
                list = txtSearch.Text.Split(' ').ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = list[i].Trim();
                }
                list = list.Distinct().ToList();

                list = list.ConvertAll(d => d.ToLower());
                list.RemoveAll(item => item.ToLower() == "in"); list.RemoveAll(item => item.ToLower() == "a"); list.RemoveAll(item => item.ToLower() == "or"); list.RemoveAll(item => item.ToLower() == "and");
                list.RemoveAll(item => item.ToLower() == "b"); list.RemoveAll(item => item.ToLower() == "c"); list.RemoveAll(item => item.ToLower() == "c"); list.RemoveAll(item => item.ToLower() == "e");
                list.RemoveAll(item => item.ToLower() == "f"); list.RemoveAll(item => item.ToLower() == "g"); list.RemoveAll(item => item.ToLower() == "h"); list.RemoveAll(item => item.ToLower() == "ı");
                list.RemoveAll(item => item.ToLower() == "i"); list.RemoveAll(item => item.ToLower() == "j"); list.RemoveAll(item => item.ToLower() == "k"); list.RemoveAll(item => item.ToLower() == "l");
                list.RemoveAll(item => item.ToLower() == "m"); list.RemoveAll(item => item.ToLower() == "n"); list.RemoveAll(item => item.ToLower() == "o"); list.RemoveAll(item => item.ToLower() == "ö");
                list.RemoveAll(item => item.ToLower() == "p"); list.RemoveAll(item => item.ToLower() == "s"); list.RemoveAll(item => item.ToLower() == "ü"); list.RemoveAll(item => item.ToLower() == "y");
                list.RemoveAll(item => item.ToLower() == "q"); list.RemoveAll(item => item.ToLower() == "t"); list.RemoveAll(item => item.ToLower() == "v"); list.RemoveAll(item => item.ToLower() == "z");
                list.RemoveAll(item => item.ToLower() == "r"); list.RemoveAll(item => item.ToLower() == "u"); list.RemoveAll(item => item.ToLower() == "x"); list.RemoveAll(item => item.ToLower() == "w");
                list.RemoveAll(item => item.ToLower() == "veya"); list.RemoveAll(item => item.ToLower() == "ve"); list.RemoveAll(item => item.ToLower() == "");

                    cmd.CommandText = "Select * from caching where query='";
                string query = "";
                    for (int i = 0; i < list.Count; i++)
                    {
                        query += list[i];
                        if (list.Count != i+1)
                        {
                            query += ",";
                        }
                        else
                        {
                            cmd.CommandText += query +  "'";
                        }
                    }

                    //save the result in data table
                    
                    ad.SelectCommand = cmd;
                    ad.Fill(dsCaching);


                if (dsCaching.Tables[0].Rows.Count < 1)
                {
                    List<DataTable> sonuclar = new List<DataTable>();

                    for (int j = 0; j < list.Count; j++)
                    {
                        cmd.CommandText = "Select * from bigindex where token='" + list[j].ToLower() + "'";

                        //save the result in data table
                        DataTable dt = new DataTable();
                        ad.SelectCommand = cmd;
                        ad.Fill(dt);

                        DataSet ds = new DataSet();
                        try
                        {
                            ds.ReadXml(new StringReader(dt.Rows[0]["array"].ToString()));
                        }
                        catch (Exception ex)
                        {
                            DataTable sonuc2 = new DataTable();

                            sonuc2.Columns.Add("url", typeof(string));
                            sonuc2.Columns.Add("title", typeof(string));
                            sonuc2.Columns.Add("description", typeof(string));
                            sonuc2.Columns.Add("rank", typeof(double));
                            sonuc2.Columns.Add("drawed", typeof(string));
                            sonuclar.Add(sonuc2);
                            continue;
                        }


                        dt = new DataTable();

                        DataView dv = ds.Tables[0].DefaultView;
                        dv.Sort = "frekans desc";
                        dt = dv.ToTable();

                        DataTable sonuc = new DataTable();

                        sonuc.Columns.Add("url", typeof(string));
                        sonuc.Columns.Add("title", typeof(string));
                        sonuc.Columns.Add("description", typeof(string));
                        sonuc.Columns.Add("rank", typeof(double));
                        sonuc.Columns.Add("drawed", typeof(string));
                        try
                        {
                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                DataRow dr = sonuc.NewRow();


                                cn = new MySqlConnection(connectionString);
                                cmd = new MySqlCommand();
                                cmd.Connection = cn;
                                ad = new MySqlDataAdapter(cmd);
                                cmd.CommandText = "Select * from pageCollection where id='" + dt.Rows[i]["docId"].ToString() + "'";

                                //save the result in data table
                                DataTable dtx = new DataTable();
                                ad.SelectCommand = cmd;
                                ad.Fill(dtx);

                                dr["rank"] = (Double.Parse(dtx.Rows[0]["pagerank"].ToString()) / 10) + Double.Parse(dt.Rows[i]["frekans"].ToString());
                                string title = dtx.Rows[0]["title"].ToString();

                                string icerik = dtx.Rows[0]["icerik"].ToString();

                                int index = icerik.ToUpper().IndexOf(list[0].ToUpper());
                                if (index < 0)
                                    continue;
                                if (index + 400 < icerik.Length)
                                    icerik = icerik.Substring(index, 400) + "...";
                                else
                                    icerik = icerik.Substring(index, icerik.Length - index - 1) + "...";

                                dr["description"] = icerik;


                                dr["url"] = dtx.Rows[0]["url"].ToString();

                                
                                if (title == "")
                                {
                                    title = icerik.Substring(0, 50) + "...";
                                }
                                dr["title"] = title;
                                dr["drawed"] = "";


                                sonuc.Rows.Add(dr);
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.Write(ex.Message);
                        }
                        
                        DataView dvSonuc = sonuc.DefaultView;
                        dvSonuc.Sort = "rank desc";

                        sonuc = dvSonuc.ToTable();

                        sonuclar.Add(sonuc);
                    }


                    int x = 0;
                    for (int i = 0; i < sonuclar.Count; i++)
                    {
                        if (sonuclar[i].Rows.Count > x)
                        {
                            x = sonuclar[i].Rows.Count;
                        }
                    }


                    DataTable sonucSon = new DataTable();

                    sonucSon.Columns.Add("url", typeof(string));
                    sonucSon.Columns.Add("title", typeof(string));
                    sonucSon.Columns.Add("description", typeof(string));
                    sonucSon.Columns.Add("rank", typeof(double));
                    sonucSon.Columns.Add("drawed", typeof(string));

                    if (sonuclar.Count > 1)
                    {
                        for (int i = 0; i < sonuclar.Count; i++)
                        {
                            for (int j = 0; j < x; j++)
                            {
                                try
                                {
                                    int temp = 1;
                                    double rank = 0;
                                    string title = "";
                                    string description = "";
                                    string url = sonuclar[i].Rows[j]["url"].ToString();

                                    for (int m = 0; m < sonuclar.Count; m++)
                                    {
                                        if (m == i)
                                        {
                                            continue;
                                        }
                                        for (int y = 0; y < x; y++)
                                        {
                                            try
                                            {
                                                if (sonuclar[m].Rows[y]["url"].ToString() == url)
                                                {
                                                    temp++;
                                                    rank += Double.Parse(sonuclar[m].Rows[y]["rank"].ToString());
                                                    if (title == "")
                                                    {
                                                        title = sonuclar[m].Rows[y]["title"].ToString();
                                                        description = sonuclar[m].Rows[y]["description"].ToString();
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex.Message);
                                                break;
                                            }

                                        }
                                    }

                                    if (temp == sonuclar.Count)
                                    {
                                        DataRow dr = sonucSon.NewRow();
                                        dr["url"] = url;
                                        dr["title"] = title;
                                        dr["description"] = description;
                                        dr["rank"] = rank;
                                        string drawed = "";
                                        for (int q = 0; q < list.Count; q++)
                                        {
                                            if (description.ToUpper().Contains(list[q].ToUpper()))
                                            {
                                                continue;
                                            }
                                            else
                                            {
                                                if (drawed != "")
                                                    drawed += " ";

                                                drawed += list[q];
                                            }
                                        }
                                        dr["drawed"] = drawed;
                                        sonucSon.Rows.Add(dr);
                                    }

                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                    break;
                                }

                            }
                        }

                        //for (int i = 0; i < sonuclar.Count; i++)
                        //{
                        //    sonucSon.Merge(sonuclar[i]);
                        //}

                        for (int o = 0; o < sonucSon.Rows.Count; o++)
                        {
                            string URL = sonucSon.Rows[o]["url"].ToString();
                            for (int e = (o + 1); e < sonucSon.Rows.Count; e++)
                            {
                                if (URL == sonucSon.Rows[e]["url"].ToString())
                                {
                                    sonucSon.Rows.Remove(sonucSon.Rows[e]);
                                    e--;
                                }
                            }
                        }

                        //diğer tablolaları ekle üzerine çizik ekleyerek.

                        for (int c = 0; c < sonuclar.Count; c++)
                        {

                            for (int n = 0; n < sonuclar[c].Rows.Count; n++)
                            {

                                string url = sonuclar[c].Rows[n]["url"].ToString();
                                string title = sonuclar[c].Rows[n]["title"].ToString();
                                string description = sonuclar[c].Rows[n]["description"].ToString();
                                string rank = sonuclar[c].Rows[n]["rank"].ToString();
                                string drawed = "";

                                bool contain = false;
                                for (int f = 0; f < sonucSon.Rows.Count; f++)
                                {
                                    if (sonucSon.Rows[f]["url"].ToString() == url)
                                    {
                                        contain = true;
                                    }
                                }

                                if (contain == false)
                                {
                                    for (int q = 0; q < list.Count; q++)
                                    {
                                        if (description.ToUpper().Contains(list[q].ToUpper()))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            if (drawed != "")
                                                drawed += " ";

                                            drawed += list[q];
                                        }
                                    }

                                    DataRow newRow = sonucSon.NewRow();
                                    newRow["url"] = url;
                                    newRow["title"] = title;
                                    newRow["description"] = description;
                                    newRow["rank"] = rank;
                                    newRow["drawed"] = drawed;

                                    sonucSon.Rows.Add(newRow);
                                }
                            }

                        }

                        for (int k = 0; k < sonucSon.Rows.Count; k++)
                        {
                            string description = sonucSon.Rows[k]["description"].ToString();
                            string title = sonucSon.Rows[k]["title"].ToString();
                            int gec = 0;
                            for (int l = 0; l < list.Count; l++)
                            {
                                if (gec == 1)
                                    continue;

                                if (description.ToUpper().Contains(list[l].ToUpper()))
                                {
                                    int index = description.ToUpper().IndexOf(list[l].ToUpper());
                                    if (index + 400 < description.Length)
                                        description = description.Substring(index, 400) + "...";
                                    else
                                        description = description.Substring(index, description.Length - index - 1) + "...";

                                    sonucSon.Rows[k]["description"] = description;
                                    if (title == "")
                                    {
                                        sonucSon.Rows[k]["title"] = description.Substring(0, 50) + "...";
                                    }
                                    gec = 1;
                                }
                            }
                            if (gec == 0)
                            {
                                sonucSon.Rows.RemoveAt(k);
                                k--;
                            }

                        }


                        Session["sonuc"] = sonucSon;

                        DataSet ds = new DataSet();
                        ds.Tables.Add(sonucSon);
                        ds.AcceptChanges();
                        MySqlCommand cmd2 = new MySqlCommand();
                        cmd2.Connection = cn;
                        cn.Open();
                        cmd2.CommandText = "INSERT INTO caching(query,array) VALUES(@query,@array) ;";
                        cmd2.Prepare();
                        
                        cmd2.Parameters.AddWithValue("@query", query);
                        cmd2.Parameters.AddWithValue("@array", ds.GetXml());
                        cmd2.ExecuteNonQuery();
                        cn.Close();

                        // caching tablosuna ekleme yap.

                    }
                    else
                    {
                        if(sonuclar.Count == list.Count)
                        { 
                        Session["sonuc"] = sonuclar[0];
                        }
                        else
                        {

                        }

                        DataSet ds = new DataSet();
                        ds.Tables.Add(sonuclar[0]);
                        ds.AcceptChanges();
                        MySqlCommand cmd2 = new MySqlCommand();
                        cmd2.Connection = cn;
                        cn.Open();
                        cmd2.CommandText = "INSERT INTO caching(query,array) VALUES(@query,@array) ;";
                        cmd2.Prepare();

                        cmd2.Parameters.AddWithValue("@query", query);
                        cmd2.Parameters.AddWithValue("@array", ds.GetXml());
                        cmd2.ExecuteNonQuery();
                        cn.Close();
                        // caching tablosuna ekleme yap.

                    }
                }
                else
                {
                    //Caching tablosundan çek göster.
                    DataSet sonuc = new DataSet();
                    string dataString = dsCaching.Tables[0].Rows[0]["array"].ToString();
                    StringReader sr = new StringReader(dataString);
                    sonuc.ReadXml(sr);

                    Session["sonuc"] = sonuc.Tables[0];
                }
                dtMili2 = DateTime.Now;
                geldi = true;
                Pager();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                rptPaging.Visible = false;
                RepCourse.Visible = false;
                pnlSonucYok.Visible = true;
                lblTime.Text = "";
            }



        }
        bool geldi = false;
        double ms;
        private void Pager()
        {
            
            DataTable sonuc = (DataTable)Session["sonuc"];
            if (sonuc.Rows.Count > 0)
            {
                if (geldi)
                {
                    TimeSpan span = dtMili2 - dtMili;
                    ms = (double)span.TotalSeconds;
                    geldi = false;
                }
                
                lblTime.Text = "Toplamda "+ sonuc.Rows.Count.ToString() + " sonuç bulundu ("+ ms.ToString() +" saniye)";
                rptPaging.Visible = true;
                RepCourse.Visible = true;
                pnlSonucYok.Visible = false;
                PagedDataSource pgitems = new PagedDataSource();
                pgitems.DataSource = sonuc.DefaultView;
                pgitems.AllowPaging = true;

                //Control page size from here 
                pgitems.PageSize = 7;
                pgitems.CurrentPageIndex = PageNumber;
                if (pgitems.PageCount > 1)
                {
                    rptPaging.Visible = true;
                    ArrayList pages = new ArrayList();
                    for (int i = 0; i <= pgitems.PageCount - 1; i++)
                    {
                        pages.Add((i + 1).ToString());
                    }
                    rptPaging.DataSource = pages;
                    rptPaging.DataBind();
                }
                else
                {
                    rptPaging.Visible = false;
                }

                //Finally, set the datasource of the repeater
                RepCourse.DataSource = pgitems;
                RepCourse.DataBind();
            }
            else
            {
                lblTime.Text = "";
                rptPaging.Visible = false;
                RepCourse.Visible = false;
                pnlSonucYok.Visible = true;
            }
           
        }
        //This method will fire when clicking on the page no link from the pager repeater
        protected void rptPaging_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
        {
            PageNumber = Convert.ToInt32(e.CommandArgument) - 1;
            Pager();
        }
        public int PageNumber
        {
            get
            {
                if (ViewState["PageNumber"] != null)
                {
                    return Convert.ToInt32(ViewState["PageNumber"]);
                }
                else
                {
                    return 0;
                }
            }
            set { ViewState["PageNumber"] = value; }
        }
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindRepeater();
        }

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