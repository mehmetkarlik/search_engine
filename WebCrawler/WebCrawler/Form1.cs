using HtmlAgilityPack;
using MongoDB.Bson;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebCrawler
{
    public partial class Form1 : Form
    {
        bool kuyrukBosmu;
        int taskCount = 0;

        List<TaskClass> taskListesi = new List<TaskClass>();
        ManualResetEvent mrse = new ManualResetEvent(false);
        ConcurrentQueue<string> urlList = new ConcurrentQueue<string>();

        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DataTable dt = Database.get1000RecordInLinkCollection();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                urlList.Enqueue(dt.Rows[i]["url"].ToString());
            }
            kuyrukBosmu = false;

            BasaDon();
        }

        public void BasaDon()
        {

            this.Invoke(new MethodInvoker(delegate ()
            {
                btnDuraklat.Enabled = false;
                btnDevam.Enabled = false;
                btnDurdur.Enabled = false;
                btnBaslat.Enabled = true;

            }));

            //Database.deleteDatasInCollections();
            //for (int i = 0; i < lstUrlList.Items.Count; i++)
            //{
            //    Database.addLink(lstUrlList.Items[i].ToString(), null);
            //}

            this.Invoke(new MethodInvoker(delegate ()
            {
                lstUrlList.HorizontalScrollbar = true;
                lstDurum.HorizontalScrollbar = true;
                //timer1.Interval = 10;

                //timer1.Start();
                lstDurum.Items.Clear();
            }));
        }

        public void Resume()
        {
            mrse.Set();
        }

        public void Pause()
        {
            mrse.Reset();
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
        private void KillTheTasks()
        {
            Pause();

            for (int i = 0; i < taskListesi.Count; i++)
            {
                TaskClass t = taskListesi[i];
                t.cancelTokenSource.Cancel();
            }

            taskListesi = new List<TaskClass>();
        }
        
        private void btnBaslat_Click(object sender, EventArgs e)
        {
            
            if (txtTaskCount.Text != "")
                taskCount = int.Parse(txtTaskCount.Text);

            if (taskCount > 100 || taskCount < 1)
            {
                MessageBox.Show("1 ile 100 aralığında bir değer giriniz.");
                txtTaskCount.Text = "";
                return;
            }

            
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            Task task = new Task(taskStart, ct);
            //task.IsBackground = true;

            task.Start();

            taskListesi.Add(new TaskClass(task, cts));
            txtTaskCount.Enabled = false;
            btnBaslat.Enabled = false;
            btnDuraklat.Enabled = true;
            btnDurdur.Enabled = true;
            Resume();
        }

        private void taskStart()
        {
            taskListesi = new List<TaskClass>();
            for (int i = 0; i < taskCount; i++)
            {
                mrse.WaitOne();
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                Task task = new Task(crawl, ct);
                //th.IsBackground = true;
                task.Start();
                taskListesi.Add(new TaskClass(task, cts));

            }
        }

        private void crawl()
        {

            while (true)
           {
                mrse.WaitOne();
                string url = "";
                try
                {
                    Random rastgele = new Random();

                    Thread.Sleep(rastgele.Next(1, 10) * 1000 + rastgele.Next(1, 10) * 100 + rastgele.Next(1, 10) * 10 + rastgele.Next(1, 10));
                    if (kuyrukBosmu == true)
                        continue;

                    if (urlList.Count == 0)
                    {
                        kuyrukBosmu = true;
                        Pause();
                        DataTable dt = Database.get1000RecordInLinkCollection();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            urlList.Enqueue(dt.Rows[i]["url"].ToString());
                        }
                        kuyrukBosmu = false;
                        Resume();
                    }
                    urlList.TryDequeue(out url);

                    if (url == "" || url.Contains("Başarısız Kayıt"))
                    {
                        if (url != "")
                        {
                            throw new Exception(url);
                        }
                        continue;
                    }


                    try
                    {

                        HttpWebRequest webRequest = null;
                        webRequest = HttpWebRequest.Create(url) as HttpWebRequest;
                        webRequest.Accept = "text/html";

                        webRequest.Timeout = 60000;
                        webRequest.Method = "GET";

                        WebResponse webResponse = null;
                        webResponse = webRequest.GetResponse();

                        string url2 = webResponse.ResponseUri.ToString();

                        if (url != url2)
                        {
                            using (MySqlConnection connection = new MySqlConnection(Database.connectionString))
                            {
                                Database.deleteLinkInLinkCollections(url, connection);
                                Database.deleteLinkInLinks(url, connection);
                                Database.deleteLinkInPageCollections(url, connection);
                            }
                            url = url2;
                        }

                        if (url[url.Length - 1] == '/')
                        {
                            url = url.Remove(url.Length - 1);
                        }
                        var Date = webResponse.Headers["Last-Modified"];

                        if (Date == null)
                        {
                            Date = webResponse.Headers["Date"];
                        }

                        DateTime dateTime;

                        dateTime = DateTime.Parse(Date);

                        List<string> linkler = new List<string>();

                        string icerik = "";
                        string responseString = "";
                        var encoding = ASCIIEncoding.UTF8;
                        this.Invoke(new MethodInvoker(delegate ()
                        {

                            lstDurum.TopIndex = lstDurum.Items.Add(url + " sayfası internetten çekiliyor..");

                        }));
                        using (var responseStream = webResponse.GetResponseStream())
                        {
                            using (var streamReader = new StreamReader(responseStream))
                            {
                                responseString = streamReader.ReadToEnd();
                            }
                        }

                        icerik = responseString;
                        try
                        {
                            HtmlAgilityPack.HtmlDocument htmldoc = new HtmlAgilityPack.HtmlDocument();
                            htmldoc.LoadHtml(icerik);
                            linkler = addLinks(url, icerik);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("Object reference not set"))
                            {
                                var hw = new HtmlWeb();
                                var doc = hw.Load(url);
                                icerik = doc.DocumentNode.OuterHtml;
                                linkler = addLinks(url, icerik);
                            }
                        }
                        using (MySqlConnection connection = new MySqlConnection(Database.connectionString))
                        {
                            
                            string deger = Database.addPage(url, dateTime, responseString, linkler.Count, connection);

                            if (deger == "")
                            {

                                for (int i = 0; i < linkler.Count; i++)
                                {
                                    if (url != linkler[i])
                                    {
                                        Database.addLink(url, linkler[i], connection);
                                    }
                                }

                            }
                            else
                            {
                                throw new Exception(deger);
                            }
                            this.Invoke(new MethodInvoker(delegate ()
                            {

                                lstDurum.TopIndex = lstDurum.Items.Add(url + " sayfası kayıt edildi.");

                            }));
                        }


                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("");
                        Debug.WriteLine("### Form1.cs ###");
                        Debug.WriteLine(ex.Message + ex.StackTrace);
                        this.Invoke(new MethodInvoker(delegate ()
                        {

                            lstDurum.TopIndex = lstDurum.Items.Add(url + " -- Hata -- " + ex.Message);

                        }));
                        using (MySqlConnection connection = new MySqlConnection(Database.connectionString))
                        {
                            Database.deleteLinkInLinkCollections(url, connection);
                            Database.deleteLinkInLinks(url, connection);
                            Database.deleteLinkInPageCollections(url, connection);
                        }

                        continue;
                    }
                    using (MySqlConnection connection = new MySqlConnection(Database.connectionString))
                    {
                        Database.deleteLinkInLinkCollections(url, connection);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("");
                    Debug.WriteLine("### Form1.cs ###");
                    Debug.WriteLine(ex.Message + ex.StackTrace);
                    using (MySqlConnection connection = new MySqlConnection(Database.connectionString))
                    {
                        Database.deleteLinkInLinkCollections(url, connection);
                        Database.deleteLinkInLinks(url, connection);
                        Database.deleteLinkInPageCollections(url, connection);
                    }

                    this.Invoke(new MethodInvoker(delegate ()
                        {
                            lstDurum.TopIndex = lstDurum.Items.Add(url + " -- Hata -- " + ex.Message);
                        }));
                    continue;
                }

            }
        }
        

        private List<string> addLinks(string url, string icerik)
        {
            List<string> linkler = new List<string>();
            if (!icerik.Contains("html"))
            {
                throw new Exception("");
            }
            HtmlAgilityPack.HtmlDocument htmldoc = new HtmlAgilityPack.HtmlDocument();
            htmldoc.LoadHtml(icerik);
            HtmlNodeCollection colllection = htmldoc.DocumentNode.SelectNodes("//a[@href]");
            if (colllection != null)
            {
                foreach (HtmlNode nodelink in colllection)
                {
                    HtmlAttribute att = nodelink.Attributes["href"];
                    var link = "";
                    

                    if (att.Value == "" || att.Value.Contains("javascript") || att.Value == "/" || att.Value.Contains("#") || att.Value == "." || att.Value == "mailto:")
                    {
                        continue;
                    }

                    if (att.Value[0] == '.')
                    {
                        att.Value = att.Value.Remove(0, 1);
                    }



                    if (!att.Value.StartsWith("http"))
                    {
                        if (att.Value[0] == '.')
                        {
                            att.Value = att.Value.Remove(0, 1);
                        }
                        if (att.Value[0] != '/')
                        {
                            link = "/" + att.Value;
                        }
                        else
                        {
                            link = att.Value;
                        }
                        link = url + link;
                    }
                    else
                    {
                        link = att.Value;
                    }




                    if (link[link.Length - 1] == '.')
                    {
                        link = link.Remove(link.Length - 1, 1);
                    }

                    if (link[link.Length - 1] == '/')
                    {
                        link = link.Remove(link.Length - 1, 1);
                    }

                    if (link.Substring(0, 10).Contains("http://"))
                    {
                        link = link.Remove(0, "http://".Length);
                        link = link.Replace("//", "/");
                        link = "http://" + link;
                    }
                    else if (link.Substring(0, 10).Contains("https://"))
                    {
                        link = link.Remove(0, "https://".Length);
                        link = link.Replace("//", "/");
                        link = "https://" + link;
                    }
                    char x = link.Last();
                    if (x == '/')
                    {
                        link = link.Remove(link.Length - 1, 1);
                    }

                    if (link[link.Length - 1] == '.')
                    {
                        link = link.Remove(link.Length - 1, 1);
                    }

                    if (link[link.Length - 1] == '/')
                    {
                        link = link.Remove(link.Length - 1, 1);
                    }

                    if (url != link)
                    {
                        if (!link.Contains(".jpg") && !link.Contains(".gif") && !link.Contains(".png") && !link.Contains(".jpeg") && !link.Contains(".pdf")
                        && !link.Contains(".doc") && !link.Contains(".Jpeg") && !link.Contains(".js") && !link.Contains(".css") && !link.Contains(".svg")
                        && !link.Contains(".mp4") && !link.Contains(".flv") && !link.Contains(".eot") && !link.Contains(".woff") && !link.Contains(".ttf")
                        && !link.Contains("'") && !link.Contains("<") && !link.Contains(".dtd") && !link.Contains(".JPG") && !link.Contains(".flv"))
                        {
                            if (link.Length < 200)
                            {
                                linkler.Add(link);
                            }
                        }

                    }
                }
            }
            else
            {
                //html içerik script içerisine gömülmüş ilerde bunuda çıkartıcak bir kod yazabilirsin.
            }
            foreach(string link in linkler)
            {
                link.Replace(" ", "");
            }
            linkler = linkler.Distinct().ToList();
            return linkler;

        }
        private void btnDuraklat_Click(object sender, EventArgs e)
        {
            Pause();
            btnDuraklat.Enabled = false;
            btnDevam.Enabled = true;
        }

        private void btnDevam_Click(object sender, EventArgs e)
        {
            Resume();
            btnDuraklat.Enabled = true;
            btnDevam.Enabled = false;
        }

        private void btnDurdur_Click(object sender, EventArgs e)
        {
            KillTheTasks();
            txtTaskCount.Enabled = true;
            btnBaslat.Enabled = true;
            btnDevam.Enabled = false;
            btnDuraklat.Enabled = false;
            btnDurdur.Enabled = false;
        }

        private void txtTaskCount_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
        (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }

        private void kopyalaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(lstDurum.SelectedItem.ToString(), TextDataFormat.Text);
        }

        private void lstDurum_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point p = new Point(e.X, e.Y);
                int index = lstDurum.IndexFromPoint(p);
                if (index >=0)
                {
                    lstDurum.SelectedItem = lstDurum.Items[index];
                }
            }
        }
    }

    public class TaskClass
    {
        public TaskClass(Task task, CancellationTokenSource cancelTokenSource)
        {
            this.task = task;
            this.cancelTokenSource = cancelTokenSource;
        }
        public Task task { get; set; }
        public CancellationTokenSource cancelTokenSource { get; set; }
    }
}
