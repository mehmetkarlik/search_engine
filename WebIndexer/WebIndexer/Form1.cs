using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace WebIndexer
{
    public partial class Form1 : Form
    {
        List<TaskClass> taskListesi = new List<TaskClass>();
        ManualResetEvent mrse = new ManualResetEvent(false);
        ConcurrentQueue<DataRow> kuyruk = new ConcurrentQueue<DataRow>();
        bool kuyrukBosmu;
        int taskCount = 0;
        public Form1()
        {
            InitializeComponent();
            kuyrukBosmu = true;
            btnBaslat.Enabled = false;
            btnDevam.Enabled = false;
            btnDuraklat.Enabled = false;
            btnDurdur.Enabled = false;

            KuyrukDoldur();

            btnBaslat.Enabled = true;
        }

        public void KuyrukDoldur()
        {
            if (kuyrukBosmu == false)
            {
                return;
            }

            DataTable dt = Database.KuyrukDoldur();
            foreach (DataRow dr in dt.Rows)
            {
                kuyruk.Enqueue(dr);
            }
            kuyrukBosmu = false;

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


            this.Invoke(new MethodInvoker(delegate ()
            {

                lstDurum.HorizontalScrollbar = true;
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

                using (MySqlConnection connection = new MySqlConnection(Database.connectionString))
                {
                    Database.updatePage(t.pageId, null, false, connection);
                }

                
            }

            taskListesi = new List<TaskClass>();
        }
        
        private void btnBaslat_Click(object sender, EventArgs e)
        {
            
            if (txtTaskCount.Text != "")
                taskCount = int.Parse(txtTaskCount.Text);

            if (taskCount > 10 || taskCount < 1)
            {
                MessageBox.Show("1 ile 10 aralığında bir değer giriniz.");
                txtTaskCount.Text = "";
                return;
            }
                

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            Task task = new Task(taskStart, ct);

            task.Start();

            taskListesi.Add(new TaskClass(task, cts, task.Id, 0));
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
                Task task = new Task(index, ct);

                task.Start();
                taskListesi.Add(new TaskClass(task, cts, task.Id, 0));
                Thread.Sleep(1500);

            }
        }

        private void index()
        {

            while (true)
            {
                mrse.WaitOne();
                try
                {
                    if (kuyruk.Count > 0)
                    {
                        DataRow dr = null;
                        kuyruk.TryDequeue(out dr);
                        using (MySqlConnection connection = new MySqlConnection(Database.connectionString))
                        {
                            Database.updatePage((int)dr["id"], null, true, connection);
                        }

                        this.Invoke(new MethodInvoker(delegate ()
                        {
                            lstDurum.TopIndex = lstDurum.Items.Add(dr["url"].ToString() + " sayfası indexleniyor..");
                        }));

                        string icerik = dr["icerik"].ToString();
                        int id = (int)dr["id"];
                        for (int i = 0; i < taskListesi.Count; i++)
                        {
                            if (Task.CurrentId == taskListesi[i].taskId)
                            {
                                taskListesi[i].pageId = id;
                            }
                        }

                        icerik = Regex.Replace(icerik, @"[^\w\s]", "");
                        string[] dizi = icerik.Split(' ');
                        List<string> list = dizi.ToList();
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

                        list = list.ConvertAll(d => d.ToLower());
                        dizi = list.ToArray();
                        var counts = new Dictionary<string, int>();

                        foreach (string value in dizi)
                        {
                            if (counts.ContainsKey(value))
                                counts[value] = counts[value] + 1;
                            else
                                counts.Add(value, 1);
                        }
                        using (MySqlConnection connection = new MySqlConnection(Database.connectionString))
                        {
                            Database.addToken(id, counts, connection);

                            Database.updatePage(id, DateTime.Now, false, connection);
                        }

                        this.Invoke(new MethodInvoker(delegate ()
                        {
                            lstDurum.TopIndex = lstDurum.Items.Add(dr["url"].ToString() + " sayfasının indekslenme işlemi tamamlandı.");
                        }));
                        Thread.Sleep(1000);

                    }
                    else
                    {
                        if (kuyrukBosmu == true)
                            continue;

                        Pause();
                        kuyrukBosmu = true;
                        KuyrukDoldur();
                        kuyrukBosmu = false;
                        Resume();
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke(new MethodInvoker(delegate ()
                    {
                        if (!ex.Message.Contains("Thread was being aborted"))
                        {

                            lstDurum.TopIndex = lstDurum.Items.Add(" -- Hata -- " + ex.Message);
                        }

                    }));
                    continue;
                }

            }
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
            btnBaslat.Enabled = true;
            txtTaskCount.Enabled = true;
            btnDevam.Enabled = false;
            btnDuraklat.Enabled = false;
            btnDurdur.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            Task ts = new Task(BasaDon, ct);
            ts.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Pause();
            label3.Text = "Form Kapanıyor Lütfen Bekleyiniz..";
            KillTheTasks();
            e.Cancel = false;
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
    }

    public class TaskClass
    {
        public TaskClass(Task task, CancellationTokenSource cancelTokenSource, int taskId, int pageId)
        {
            this.task = task;
            this.cancelTokenSource = cancelTokenSource;
            this.taskId = taskId;
            this.pageId = pageId;
        }
        public Task task { get; set; }
        public int pageId { get; set; }
        public int taskId { get; set; }
        public CancellationTokenSource cancelTokenSource { get; set; }
    }

}
