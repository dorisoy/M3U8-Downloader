using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Text;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Devices;

namespace M3U8_Downloader
{
    public partial class PMUDownloaderForm : Form
    {
        //public const string  VideoRootPath = "\\\\cheeky\\data\\RaceData\\VideoFinish\\Temp\\";
        public const string VideoRootPath = "\\\\diskstation\\web\\videos\\PMU\\";
        //\\diskstation\web\videos
        private bool DownloadAtRandomInterval = false;
        private bool stopApp;
        private Random RandomGen = new Random();
        public bool CurDownloaded = false;
        public DownLoadQueue myDownLoadQueue;

        public int defaultTimeout = 1800000;

        public MySqlHelper mySqlHelper;
        //任务栏进度条的实现。
        private TaskbarManager windowsTaskbar = TaskbarManager.Instance;


        //拖动窗口
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        private const int WM_SETREDRAW = 0xB;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;

        [DllImport("kernel32.dll")]
        static extern bool GenerateConsoleCtrlEvent(int dwCtrlEvent, int dwProcessGroupId);
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();
        [DllImport("user32.dll")]
        public static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

        [DllImport("kernel32.dll")]
        static extern int SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);


        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref System.UInt32 pcchCookieData, int dwFlags, IntPtr lpReserved);
        private static string GetCookies(string url)
        {
            uint datasize = 1024;
            StringBuilder cookieData = new StringBuilder((int)datasize);
            if (!InternetGetCookieEx(url, null, cookieData, ref datasize, 0x2000, IntPtr.Zero))
            {
                if (datasize < 0)
                    return null;

                cookieData = new StringBuilder((int)datasize);
                if (!InternetGetCookieEx(url, null, cookieData, ref datasize, 0x00002000, IntPtr.Zero))
                    return null;
            }
            //Clipboard.SetDataObject(cookieData.ToString() + "-----" + url);
            return cookieData.ToString();
        }

        public string GetCookieString(WebBrowser webBrowser)
        {
            if (webBrowser.Url == null)
                return null;
            string dir = webBrowser.Url.Host;
            FileStream fr = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.Cookies) + "\\index.dat", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] __dat = new byte[(int)fr.Length];
            fr.Read(__dat, 0, __dat.Length);
            fr.Close();
            fr.Dispose();
            string __datstream = Encoding.Default.GetString(__dat);
            int p1 = 0;
            p1 = __datstream.IndexOf("@" + dir, p1);
            if (p1 == -1)
                p1 = __datstream.IndexOf("@" + dir.Substring(dir.IndexOf('.') + 1));
            if (p1 == -1)
                return webBrowser.Document.Cookie;
            int p2 = __datstream.IndexOf(".txt", p1 + 1);
            p1 = __datstream.LastIndexOf('@', p2);
            string dm = __datstream.Substring(p1 + 1, p2 - p1 + 3).TrimStart('?');
            p1 = __datstream.LastIndexOf(":", p1);
            p2 = __datstream.IndexOf('@', ++p1);
            __datstream = string.Format("{0}@{1}", __datstream.Substring(p1, p2 - p1), dm);

            Dictionary<string, string> __cookiedicts = new Dictionary<string, string>();
            string __n;
            StringBuilder __cookies = new StringBuilder();
            __datstream = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Cookies) + "\\" + __datstream, Encoding.Default);
            p1 = -2;
            do
            {
                p1 += 2;
                p2 = __datstream.IndexOf('\n', p1);
                if (p2 == -1)
                    break;
                __n = __datstream.Substring(p1, p2 - p1);
                p1 = p2 + 1;
                p2 = __datstream.IndexOf('\n', p1);
                if (!__cookiedicts.ContainsKey(__n))
                    __cookiedicts.Add(__n, __datstream.Substring(p1, p2 - p1));
            }
            while ((p1 = __datstream.IndexOf("*\n", p1)) > -1);
            if (webBrowser.Document.Cookie != null && webBrowser.Document.Cookie.Length > 0)
            {
                foreach (string s in webBrowser.Document.Cookie.Split(';'))
                {
                    p1 = s.IndexOf('=');
                    if (p1 == -1)
                        continue;
                    __datstream = s.Substring(0, p1).TrimStart();
                    if (__cookiedicts.ContainsKey(__datstream))
                        __cookiedicts[__datstream] = s.Substring(p1 + 1);
                    else
                        __cookiedicts.Add(__datstream, s.Substring(p1 + 1));
                }
            }
            foreach (string s in __cookiedicts.Keys)
            {
                if (__cookies.Length > 0)
                    __cookies.Append(';');
                __cookies.Append(s);
                __cookies.Append('=');
                __cookies.Append(__cookiedicts[s]);
            }
            return __cookies.ToString();
        }


        private SQLiteHelper mySQLiteHelper;

        //private void RefreshTextBoxControl()
        //{
        //    SendMessage(textBox_Info.Handle, WM_SETREDRAW, 0, IntPtr.Zero);//禁止重绘
        //    // 重新布局
        //    SendMessage(textBox_Info.Handle, WM_SETREDRAW, 1, IntPtr.Zero);//取消禁止
        //    textBox_Info.Refresh();
        //}


        int ffmpegid = -1;
        Double big = 0;
        Double small = 0;

        private WebBrowser myWebBrowser;

        private String SESSIONID;


        //不影响点击任务栏图标最大最小化
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_MINIMIZEBOX = 0x00020000;  // Winuser.h中定义
                CreateParams cp = base.CreateParams;
                cp.Style = cp.Style | WS_MINIMIZEBOX;   // 允许最小化操作
                return cp;
            }
        }

        public PMUDownloaderForm()
        {
            InitializeComponent();
            Init();
            mySqlHelper = new MySqlHelper();
            myDownLoadQueue = new DownLoadQueue();
            //禁止编译器对跨线程访问做检查
            Control.CheckForIllegalCrossThreadCalls = false;  
            mySQLiteHelper = new SQLiteHelper();
            SQLiteHelper.SetConnectionString(System.AppDomain.CurrentDomain.BaseDirectory + "Data.db3", "");
            myWebBrowser = NewBrowser();
        }

        private void textBox_Adress_DragEnter(object sender, DragEventArgs e)
        {

            e.Effect = DragDropEffects.All;
        }

        private void textBox_Adress_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }
        private void textBox_Adress_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                //获取拖拽的文件地址
                var filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
                var hz = filenames[0].LastIndexOf('.') + 1;
                var houzhui = filenames[0].Substring(hz);//文件后缀名
                if (houzhui == "m3u8" || houzhui == "mkv" || houzhui == "avi" || houzhui == "mp4" || houzhui == "ts" || houzhui == "flv" || houzhui == "f4v" ||
                    houzhui == "wmv" || houzhui == "wm" || houzhui == "mpeg" || houzhui == "mpg" || houzhui == "m4v" || houzhui == "3gp" || houzhui == "rm" ||
                    houzhui == "rmvb" || houzhui == "mov" || houzhui == "qt" || houzhui == "m2ts" || houzhui == "m3u" || houzhui == "mts" || houzhui == "txt") //只允许拖入部分文件
                {
                    e.Effect = DragDropEffects.All;
                    string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
                    textBox_Adress.Text = path; //将获取到的完整路径赋值到textBox1
                }

            }

        }

        private void button_Quit_Click(object sender, EventArgs e)
        {
            SaveSettings();
            try
            {
                if (Process.GetProcessById(ffmpegid) != null)
                {
                    if (MessageBox.Show("已启动下载进程，确认退出吗？\n（这有可能是强制的）", "请确认您的操作", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                    {
                        Stop();
                        MessageBox.Show("已经发送命令！\n若进程仍然存在则强制结束！", "请确认");
                        try
                        {
                            if (Process.GetProcessById(ffmpegid) != null)  //如果进程还存在就强制结束它
                            {
                                Process.GetProcessById(ffmpegid).Kill();
                                Dispose();
                                Application.Exit();
                            }
                        }
                        catch
                        {
                            Dispose();
                            Application.Exit();
                        }

                    }
                    else
                    {
                    }
                }
            }
            catch
            {
                Dispose();
                Application.Exit();
            }

        }

        private void button_ChangePath_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox_DownloadPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button_OpenPath_Click(object sender, EventArgs e)
        {
            Process.Start(textBox_DownloadPath.Text);
        }

        private void linkLabel_Stop_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Stop();
        }

        //格式化大小输出
        public static String FormatFileSize(Double fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }
            else if (fileSize >= 1024 * 1024 * 1024)
            {
                return string.Format("{0:########0.00} GB", ((Double)fileSize) / (1024 * 1024 * 1024));
            }
            else if (fileSize >= 1024 * 1024)
            {
                return string.Format("{0:####0.00} MB", ((Double)fileSize) / (1024 * 1024));
            }
            else if (fileSize >= 1024)
            {
                return string.Format("{0:####0.00} KB", ((Double)fileSize) / 1024);
            }
            else
            {
                return string.Format("{0} bytes", fileSize);
            }
        }

        private void textBox_Info_TextChanged(object sender, EventArgs e)
        {
            if (textBox_Info.GetLineFromCharIndex(textBox_Info.TextLength) + 1 > 14)
                textBox_Info.ScrollBars = ScrollBars.Vertical;
            if (textBox_Info.GetLineFromCharIndex(textBox_Info.TextLength) + 1 <= 14)
                textBox_Info.ScrollBars = ScrollBars.None;

            Regex duration = new Regex(@"Duration: (\d\d[.:]){3}\d\d", RegexOptions.Compiled | RegexOptions.Singleline);//取总视频时长
            label5.Text = "[TotalTime：" + duration.Match(textBox_Info.Text).Value.Replace("Duration: ", "") + "]";
            Regex regex = new Regex(@"(\d\d[.:]){3}\d\d", RegexOptions.Compiled | RegexOptions.Singleline);//取视频时长以及Time属性
            var time = regex.Matches(textBox_forRegex.Text);
            //取已下载大小
            Regex size = new Regex(@"[1-9][0-9]{0,}kB time", RegexOptions.Compiled | RegexOptions.Singleline);
            var sizekb = size.Matches(textBox_forRegex.Text);
            if (time.Count > 0 && sizekb.Count > 0)
            {
                try
                {
                    label6.Text = "[Downloaded：" + time.OfType<Match>().Last() + "，" + FormatFileSize(Convert.ToDouble(sizekb.OfType<Match>().Last().ToString().Replace("kB time", "")) * 1024) + "]";
                }
                catch (Exception ex)
                { }
            }
            Regex fps = new Regex(@", (\S+)\sfps", RegexOptions.Compiled | RegexOptions.Singleline);//取视频帧数
            Regex resolution = new Regex(@", \d{2,}x\d{2,}", RegexOptions.Compiled | RegexOptions.Singleline);//取视频分辨率
            label7.Text = "[Video information：" + resolution.Match(textBox_Info.Text).Value.Replace(", ", "") + "，" + fps.Match(textBox_Info.Text).Value.Replace(", ", "") + "]";
            //防止程序太快 无法截取
            if (time.Count > 0 && sizekb.Count > 0) 
            {
                try
                {
                    //"[TotalTime：00:04:40.00]"
                    var time1 = label5.Text.Substring(11, 2);
                    var time2 = label5.Text.Substring(14, 2);
                    var time3 = label5.Text.Substring(17, 2);
                    var time4 = label5.Text.Substring(20, 2);

                    Double All = Convert.ToDouble(Convert.ToDouble(time1) * 60 * 60 + Convert.ToDouble(time2) * 60
                + Convert.ToDouble(time3) + Convert.ToDouble(time4) / 100);

                    //"[Downloaded：00:00:02.98，45.84 MB]"
                    var time5 = label6.Text.Substring(12, 2);
                    var time6 = label6.Text.Substring(15, 2);
                    var time7 = label6.Text.Substring(18, 2);
                    var time8 = label6.Text.Substring(21, 2);

                    Double Downloaded = Convert.ToDouble(Convert.ToDouble(time5) * 60 * 60 + Convert.ToDouble(time6) * 60
                    + Convert.ToDouble(time7) + Convert.ToDouble(time8) / 100);

                    if (All == 0) All = 1;  //防止被除数为零导致程序崩溃
                    Double Progress = (Downloaded / All) * 100;

                    if (Progress > 100)  //防止进度条超过百分之百
                        Progress = 100;
                    if (Progress < 0)  //防止进度条小于零……
                        Progress = 0;

                    ProgressBar.Value = Convert.ToInt32(Progress);
                    windowsTaskbar.SetProgressValue(Convert.ToInt32(Progress), 100, this.Handle);
                    label_Progress.Visible = true;
                    label_Progress.Text = "Completed：" + String.Format("{0:F}", Progress) + "%";
                    double fileSize = (big - small) * 1024;
                    this.Text = "Completed：" + String.Format("{0:F}", Progress) + "%" + " [" + FormatFileSize(fileSize > 0 ? fileSize : 0) + "/s]";
                }
                catch (Exception ex)
                {
                    try
                    {
                        label5.Text = "[TotalTime：NULL]";

                        var time5 = label6.Text.Substring(12, 2);
                        var time6 = label6.Text.Substring(15, 2);
                        var time7 = label6.Text.Substring(18, 2);
                        var time8 = label6.Text.Substring(21, 2);

                        Double Downloaded = Convert.ToDouble(Convert.ToDouble(time5) * 60 * 60 + Convert.ToDouble(time6) * 60
                        + Convert.ToDouble(time7) + Convert.ToDouble(time8) / 100);

                        Double Progress = 100;

                        if (Progress > 100)  //防止进度条超过百分之百
                            Progress = 100;
                        if (Progress < 0)  //防止进度条小于零……
                            Progress = 0;

                        ProgressBar.Value = Convert.ToInt32(Progress);
                        windowsTaskbar.SetProgressValue(Convert.ToInt32(Progress), 100, this.Handle);
                        label_Progress.Visible = true;
                        label_Progress.Text = "Completed：" + String.Format("{0:F}", Progress) + "%";
                        double fileSize = (big - small) * 1024;
                        this.Text = "Completed：" + String.Format("{0:F}", Progress) + "%" + " [" + FormatFileSize(fileSize > 0 ? fileSize : 0) + "/s]";
                    }
                    catch (Exception exs) 
                    {
                        CommonHelper.WriteLog(exs.Message, "SetProgressValue");
                    }
                }
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            myWebBrowser.ScriptErrorsSuppressed = true;
            this.panel1.Controls.Add(myWebBrowser);
            ////初始化进度条
            windowsTaskbar.SetProgressState(TaskbarProgressBarState.Normal, this.Handle);
            windowsTaskbar.SetProgressValue(0, 100, this.Handle);

            if (!File.Exists(@"Tools\ffmpeg.exe"))  //判断程序目录有无ffmpeg.exe
            {
                MessageBox.Show("Could not find Tools\\ffmpeg.exe", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Dispose();
                Application.Exit();
            }
            if (File.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "\\M3u8_Downloader_Settings.xml"))  //判断程序目录有无配置文件，并读取文件
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(@System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "\\M3u8_Downloader_Settings.xml");    //加载Xml文件  
                XmlNodeList topM = doc.SelectNodes("Settings");
                foreach (XmlElement element in topM)
                {
                    textBox_DownloadPath.Text = element.GetElementsByTagName("DownPath")[0].InnerText;
                    if (element.GetElementsByTagName("ExtendName")[0].InnerText == "MP4") { radioButton1.Checked = true; }
                    if (element.GetElementsByTagName("ExtendName")[0].InnerText == "MKV") { radioButton2.Checked = true; }
                    if (element.GetElementsByTagName("ExtendName")[0].InnerText == "TS") { radioButton3.Checked = true; }
                    if (element.GetElementsByTagName("ExtendName")[0].InnerText == "FLV") { radioButton4.Checked = true; }
                }
            }
            else  //若无配置文件，获取当前程序运行路径，即为默认下载路径
            {
                string lujing = System.Environment.CurrentDirectory;
                textBox_DownloadPath.Text = lujing;
            }
            //
            this.BeginInvoke(new Action(() => {
                WriteToLable(label9, "Loading page for infocentre.pmu.fr...");
                LoginSite();
                WriteToLable(label9, "Logged in");
            }));
        }


        internal void WriteToLable(Label label, String message)
        {
            if (label.InvokeRequired)
            {
                this.Invoke((Action)(() =>
                    WriteToLable(label, message)
                ));
                return;
            }
            label.Text = string.Format("{0}", message);
            Application.DoEvents();
        }

        //webBrowser1
        internal void InitWebBrowser(WebBrowser webBrowser)
        {
            if (webBrowser.InvokeRequired)
            {
                this.Invoke((Action)(() =>
                   this.myWebBrowser = webBrowser
                ));
                return;
            }
            this.myWebBrowser = webBrowser;
             Application.DoEvents();
        }


        private void textBox_Adress_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
                return;
            if (e.KeyChar == (char)1)       // Ctrl-A 相当于输入了AscII=1的控制字符
            {
                textBox.SelectAll();
                e.Handled = true;      // 不再发出“噔”的声音
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void button_Exit_Click(object sender, EventArgs e)
        {
            SaveSettings();
            try
            {
                if (this.myWebBrowser != null)
                {
                    this.myWebBrowser.Stop();
                    this.myWebBrowser.Dispose();
                }
                stopApp = true;

                if (Process.GetProcessById(ffmpegid) != null)
                {
                    if (MessageBox.Show("已启动下载进程，确认退出吗？\n（这有可能是强制的）", "请确认您的操作", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                    {
                        Stop();
                        MessageBox.Show("已经发送命令！\n若进程仍然存在则强制结束！", "请确认");
                        try
                        {
                            if (Process.GetProcessById(ffmpegid) != null)  //如果进程还存在就强制结束它
                            {
                                Process.GetProcessById(ffmpegid).Kill();
                                Dispose();
                                Application.Exit();
                            }
                        }
                        catch
                        {
                            Dispose();
                            Application.Exit();
                        }

                    }
                    else
                    {
                    }
                }
            }
            catch
            {
                Dispose();
                Application.Exit();
            }
        }



        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            MoveFrom();
        }

        private void label8_MouseDown(object sender, MouseEventArgs e)
        {
            MoveFrom();
        }

        private void label14_Click(object sender, EventArgs e)
        {
            Process.Start("https://ffmpeg.zeranoe.com/builds/win32/static/");
        }

        private void Label_Monitor_Click(object sender, EventArgs e)
        {
            Exist_Run(@"Tools\HttpFileMonitor.exe");
        }

        private void Label_WriteLog_Click(object sender, EventArgs e)
        {
            String LogName = "日志-" + System.DateTime.Now.ToString("yyyy.MM.dd-HH.mm.ss") + ".txt";
            StreamWriter log = new StreamWriter(LogName);
            log.WriteLine("━━━━━━━━━━━━━━\r\n"
                + "■M3U8 Downloader 用户日志\r\n\r\n"
                + "■" + System.DateTime.Now.ToString("F") + "\r\n\r\n"
                + "■输入：" + textBox_Adress.Text + "\r\n\r\n"
                + "■输出：" + textBox_DownloadPath.Text + "\\" + textBox_Name.Text + houzhui.Text + "\r\n\r\n"
                + "■FFmpeg命令：ffmpeg " + Command.Text + "\r\n"
                + "━━━━━━━━━━━━━━"
                + "\r\n\r\n"
                + textBox_Info.Text);
            log.Close();
            MessageBox.Show("日志已生成到程序目录！", "提示信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void label_Update_Click(object sender, EventArgs e)
        {
            Process.Start("http://pan.baidu.com/s/1dF4uDuL");
        }

        private void label_OpenTool_Click(object sender, EventArgs e)
        {
            Exist_Run(@"Tools\Batch Download.exe");
        }

        private void label_Progress_MouseDown(object sender, MouseEventArgs e)
        {
            MoveFrom();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                //取已下载大小
                Regex size = new Regex(@"[1-9][0-9]{0,}kB time", RegexOptions.Compiled | RegexOptions.Singleline);
                var sizekb = size.Matches(textBox_forRegex.Text);
                big = Convert.ToDouble(sizekb.OfType<Match>().Last().ToString().Replace("kB time", ""));
                double fileSize = (big - small) * 1024;
                label8.Text = "[" + FormatFileSize(fileSize > 0 ? fileSize : 0) + "/s]";
            }
            catch (Exception) { }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            small = big;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            try
            {
                if (Process.GetProcessById(ffmpegid) != null)
                {
                    if (MessageBox.Show("已启动下载进程，确认退出吗？\n（这有可能是强制的）", "请确认您的操作", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                    {
                        Stop();
                        MessageBox.Show("已经发送命令！\n若进程仍然存在则强制结束！", "请确认");
                        try
                        {
                            if (Process.GetProcessById(ffmpegid) != null)  //如果进程还存在就强制结束它
                            {
                                Process.GetProcessById(ffmpegid).Kill();
                                Dispose();
                                Application.Exit();
                            }
                        }
                        catch
                        {
                            Dispose();
                            Application.Exit();
                        }

                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
            }
            catch
            {
                Dispose();
                Application.Exit();
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            //DateTime viewTimer = new DateTime();
            //myProgressBar1.Value = 0;
            Application.DoEvents();
            //string racedatafile = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Files\\RaceData.txt";
            string rsRacedatafile = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Files\\RSRaceData.txt";
            var raceLists = new List<RaceInfo>();
            //var fileInfo = new FileInfo(racedatafile);
            var fileInfo = new FileInfo(rsRacedatafile);
            if (fileInfo.Exists)
            {
                //raceLists = ReadLineFile(racedatafile);
                //RDate,RCourse, RRaceNo
                raceLists = ReadRSLineFile(rsRacedatafile).Select(r => r).Where(r => r.MeetingDiscipline == "T").OrderBy(r => r.MeetingDate).ThenBy(r => r.MeetingCourseCode).ThenBy(r => r.RaceNumber).ToList();
                raceLists.ForEach(r =>
                {
                    string sql = "select id from raceinfo where MeetingId='" + r.MeetingId + "' and MeetingCourseCode='" + r.MeetingCourseCode + "' and MeetingDiscipline='" + r.MeetingDiscipline + "' and MeetingDate='" + r.MeetingDate.ToString("yyyy-MM-dd") + "' and RaceId='" + r.RaceId + "' and RaceNumber=" + Convert.ToInt32(r.RaceNumber) + "";
                    try
                    {
                        var exist = mySQLiteHelper.ExecuteScalar(sql);
                        if (Convert.ToInt32(exist) == 0)
                        {
                            sql = "insert into raceinfo (MeetingId,MeetingCourse,MeetingCourseID,MeetingCourseCode,MeetingDiscipline,MeetingDate,RaceId,RaceName,RaceNumber,PlayerUrl) values (" + r.MeetingId + ",'" + r.MeetingCourse.Replace("'", "''") + "',0,'" + r.MeetingCourseCode + "','" + r.MeetingDiscipline + "','" + r.MeetingDate.ToString("yyyy-MM-dd") + "'," + r.RaceId + ",'" + r.RaceName.Replace("'", "''") + "'," + Convert.ToInt32(r.RaceNumber) + ",'" + r.PlayerUrl + "')";
                            mySQLiteHelper.ExecuteNonQuery(sql);
                        }
                    }
                    catch (Exception ex)
                    {
                        CommonHelper.WriteFile(sql, "InsertRaceInfo-Error");
                    }
                });
    
                myProgressBar1.Value = 0;
                Application.DoEvents();
                if (this.myWebBrowser.Document != null)
                {
                    bool newBrowser = false;
                    int index = 0;
                    string mycookies = "";
                    mycookies = GetCookies(myWebBrowser.Url.ToString());
                    SESSIONID = RequestVerificationToken(mycookies, "JSESSIONID").TrimStart().TrimEnd();
                    foreach (var race in raceLists)
                    {
                        index++;
                        if (myWebBrowser == null)
                        {
                            myWebBrowser = NewBrowser();
                            this.panel1.Controls.Add(myWebBrowser);
                            LoginSite();
                            mycookies = GetCookies(myWebBrowser.Url.ToString());
                            SESSIONID = RequestVerificationToken(mycookies, "JSESSIONID").TrimStart().TrimEnd();
                            newBrowser = true;
                        }
                        //=======================================================
                        mycookies = GetCookies(myWebBrowser.Url.ToString());
                        if (SESSIONID != RequestVerificationToken(mycookies, "JSESSIONID").TrimStart().TrimEnd())
                        {
                            LoginSite();
                            mycookies = GetCookies(myWebBrowser.Url.ToString());
                            SESSIONID = RequestVerificationToken(mycookies, "JSESSIONID").TrimStart().TrimEnd();
                        }
                        InvokeDownLoadVidedoFile(myWebBrowser, raceLists, race, index);
                        //=======================================================
                        InvokeProgressChanged(index, raceLists.Count);
                        if (newBrowser && myWebBrowser.Document != null)
                        {
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(myWebBrowser.ActiveXInstance);
                            myWebBrowser.Dispose();
                            myWebBrowser = null;
                        }
                    }
                }
            }
        }

        public void AddFieldDownload(RaceInfo r)
        {
            string sql = "select id from RaceFailed where MeetingId='" + r.MeetingId + "' and MeetingCourseCode='" + r.MeetingCourseCode + "' and MeetingDiscipline='" + r.MeetingDiscipline + "' and MeetingDate='" + r.MeetingDate.ToString("yyyy-MM-dd") + "' and RaceId='" + r.RaceId + "' and RaceNumber=" + Convert.ToInt32(r.RaceNumber) + "";
            try
            {
                var exist = mySQLiteHelper.ExecuteScalar(sql);
                if (Convert.ToInt32(exist) == 0)
                {
                    sql = "insert into RaceFailed (MeetingId,MeetingCourse,MeetingCourseID,MeetingCourseCode,MeetingDiscipline,MeetingDate,RaceId,RaceName,RaceNumber,PlayerUrl) values (" + r.MeetingId + ",'" + r.MeetingCourse.Replace("'", "''") + "',0,'" + r.MeetingCourseCode + "','" + r.MeetingDiscipline + "','" + r.MeetingDate.ToString("yyyy-MM-dd") + "'," + r.RaceId + ",'" + r.RaceName.Replace("'", "''") + "'," + Convert.ToInt32(r.RaceNumber) + ",'" + r.PlayerUrl + "')";
                    mySQLiteHelper.ExecuteNonQuery(sql);

                    CommonHelper.WriteFile(sql, "InsertRaceFailed");
                }
            }
            catch (Exception ex)
            {
                CommonHelper.WriteFile(sql, "InsertRaceFailed-Error");
            }
        }
        public WebBrowser NewBrowser()
        {
            var webBrowser = new WebBrowser();
            webBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            webBrowser.Location = new System.Drawing.Point(0, 0);
            webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            webBrowser.Name = "webBrowser1";
            webBrowser.Size = new System.Drawing.Size(995, 248);
            webBrowser.TabIndex = 0;
            return webBrowser;
        }
        public void InvokeDownLoadVidedoFile(WebBrowser webBrowser, List<RaceInfo> raceLists, RaceInfo race, int index)
        {
            WriteToLable(label9, "[" + index + "/" + raceLists.Count + "] Matching video address for " + race.PlayerUrl);
            string fileName = String.Format("{0}-{1}{2}-race{3}", race.MeetingDate.ToString("yyyyMMdd"), race.MeetingCourseCode, race.MeetingDiscipline, race.RaceNumber);
            string savePath = VideoRootPath + race.MeetingDate.ToString("yyyy") + "\\" + race.MeetingDate.ToString("MM") + "\\" + race.MeetingDate.ToString("dd");
            string filePath = savePath + "\\" + fileName + ".mp4";
            FileInfo videoFile = new FileInfo(filePath);
            if (!videoFile.Exists)
            {
                try
                {
                    string mycookies = webBrowser.Document.Cookie;
                    string frameHtml = "";
                    string videoLink = "";
                    webBrowser.Navigate(new Uri(race.PlayerUrl));
                    mycookies = webBrowser.Document.Cookie;
                    //Waiting
                    var viewTimer = DateTime.Now.AddMinutes(2);
                    while (webBrowser.ReadyState != WebBrowserReadyState.Complete || webBrowser.IsBusy)
                    {
                        Application.DoEvents();
                        if (DateTime.Compare(DateTime.Now, viewTimer) >= 0 || stopApp)
                        {
                            break;
                        }
                    }
                    int retry = 5;
                    frameHtml = GetFrameHtml(webBrowser);
                    while (string.IsNullOrEmpty(frameHtml) && retry >= 1)
                    {
                        frameHtml = GetFrameHtml(webBrowser);
                        retry--;
                    }
                    string pattern = "Clappr.HTML5Video.canPlay(\"[URL]\");";
                    videoLink = FigureURL(frameHtml, pattern, false);
                    if (!string.IsNullOrEmpty(videoLink))
                    {
                        WriteToLable(label1, "[" + index + "/" + raceLists.Count + "] DownLoading..." + videoLink);
                        this.textBox_DownloadPath.Text = savePath;
                        this.textBox_Adress.Text = videoLink;
                        this.textBox_Name.Text = fileName;
                        //
                        this.CurDownloaded = false;

                        #region

                        var downloadFile = new DownloadFile();
                        downloadFile.FileAddress = textBox_Adress.Text;
                        downloadFile.DownLoadPath = textBox_DownloadPath.Text;
                        downloadFile.FileName = textBox_Name.Text;

                        this.textBox_Name.Text = downloadFile.FileName;

                        if (radioButton1.Checked == true)
                        {
                            downloadFile.FileFormat = ".mp4";
                            downloadFile.CommandText = "-threads 0 -i " + "\"" + downloadFile.FileAddress + "\"" + " -c copy -y -bsf:a aac_adtstoasc -movflags +faststart " + "\"" + downloadFile.DownLoadPath + "\\" + downloadFile.FileName + downloadFile.FileFormat + "\"";
                        }
                        if (radioButton2.Checked == true)
                        {
                            downloadFile.FileFormat = ".mkv";
                            downloadFile.CommandText = "-threads 0 -i " + "\"" + downloadFile.FileAddress + "\"" + " -c copy -y -bsf:a aac_adtstoasc " + "\"" + downloadFile.DownLoadPath + "\\" + downloadFile.FileName + downloadFile.FileFormat + "\"";
                        }
                        if (radioButton3.Checked == true)
                        {
                            downloadFile.FileFormat = ".ts";
                            downloadFile.CommandText = "-threads 0 -i " + "\"" + downloadFile.FileAddress + "\"" + " -c copy -y -f mpegts " + "\"" + downloadFile.DownLoadPath + "\\" + downloadFile.FileName + downloadFile.FileFormat + "\"";
                        }
                        if (radioButton4.Checked == true)
                        {
                            downloadFile.FileFormat = ".flv";
                            downloadFile.CommandText = "-threads 0 -i " + "\"" + downloadFile.FileAddress + "\"" + " -c copy -y -f f4v -bsf:a aac_adtstoasc " + "\"" + downloadFile.DownLoadPath + "\\" + downloadFile.FileName + downloadFile.FileFormat + "\"";
                        }

       
                        if (!Directory.Exists(downloadFile.DownLoadPath))
                        {
                            Directory.CreateDirectory(downloadFile.DownLoadPath);
                        }
                        else
                        {
                            SetTextBox(textBox_Info, "");
                            SetTextBox(textBox_forRegex, "");
                            InitComponent();
                            //===================
                            //Download(downloadFile);
                            // 启动进程执行相应命令,此例中以执行ffmpeg.exe为例  
                            //RealActionAsync(@"Tools\ffmpeg.exe", downloadFile.CommandText);
                            IAsyncResult asyncResult = this.BeginInvoke(new Func<DownloadFile, bool, ApplicationOutput>(DownLoadDoWorkAsync), downloadFile, false);
                            viewTimer = DateTime.Now.AddMinutes(30);
                            while ((!asyncResult.IsCompleted && !CurDownloaded) || DateTime.Compare(DateTime.Now, viewTimer) >= 0)
                            {
                                Application.DoEvents();
                            }
                            var downloadResult = (ApplicationOutput)this.EndInvoke(asyncResult);
                            ////下载完成时
                            if (downloadResult.IsCompleted)
                            {
                                WriteToLable(label1, "[" + index + "/" + raceLists.Count + "] DownLoad Completed");
                            }
                            else
                            {
                                WriteToLable(label1, "[" + index + "/" + raceLists.Count + "] DownLoad failed:" + downloadResult.OutPut);
                                AddFieldDownload(race);
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        videoLink = "NULL";
                        WriteToLable(label1, "[" + index + "/" + raceLists.Count + "] DownLoading failed, videoLink is Null:" + videoLink);
                        AddFieldDownload(race);
                    }
                }
                catch (Exception ex)
                {
                    CommonHelper.WriteLog(ex.Message, "Error");
                }
            }
            else
            {
                WriteToLable(label1, "[" + index + "/" + raceLists.Count + "] File is Exists:" + filePath);
            }
        }
    
        public string GetFrameHtml(WebBrowser webBrowser)
        {
            DateTime viewTimer = new DateTime();
            string frameHtml = "";
            string videoLink = "";
            if (webBrowser.Document != null)
            {
                HtmlElementCollection iframes = webBrowser.Document.GetElementsByTagName("iframe");
                foreach (HtmlElement iframe in iframes)
                {
                    if (iframe != null)
                    {
                        videoLink = iframe.GetAttribute("src");
                        break;
                    }
                }
                if (!String.IsNullOrEmpty(videoLink))
                {
                    webBrowser.Navigate(new Uri(videoLink));
                    //Waiting
                    viewTimer = DateTime.Now.AddMinutes(2);
                    while (webBrowser.ReadyState != WebBrowserReadyState.Complete || webBrowser.IsBusy)
                    {
                        Application.DoEvents();
                        if (DateTime.Compare(DateTime.Now, viewTimer) >= 0 || stopApp)
                        {
                            break;
                        }
                    }
                    if (webBrowser.Document != null)
                    {
                        HtmlElement frameBody = webBrowser.Document.GetElementsByTagName("body")[0];
                        if (frameBody != null)
                        {
                            frameHtml = frameBody.InnerHtml;
                        }
                    }
                }
            }
            return frameHtml;
        }

        public string FigureURL(string Doc, string pattern, bool settled)
        {
            string retString = "";
            if (!settled && pattern.IndexOf("[URL]") >= 0)
            {
                Match m = GetMatchRigid(Doc, pattern, "[URL]");
                if (m.Success)
                {
                    retString = m.Groups["TARGET"].Value;
                }
            }
            else
                retString = pattern;
            return retString;
        }
        /// <summary>
        /// 按严格的匹配方式获取一个目标的匹配结果
        /// </summary>
        /// <param name="input"></param>
        /// <param name="pattern"></param>
        /// <param name="find"></param>
        /// <returns></returns>
        public static Match GetMatchRigid(string input, string pattern, string find)
        {
            string _pattn = Regex.Escape(pattern);
            _pattn = _pattn.Replace(@"\[VARIABLE]", @"[\s\S]*?");
            if (Regex.Match(pattern.TrimEnd(), Regex.Escape(find) + "$", RegexOptions.Compiled).Success)
                _pattn = _pattn.Replace(@"\" + find, @"(?<TARGET>[\s\S]+)");
            else
                _pattn = _pattn.Replace(@"\" + find, @"(?<TARGET>[\s\S]+?)");
            Regex r = new Regex(_pattn, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Match m = r.Match(input);
            return m;
        }

        private string RequestVerificationToken(string cookies, string key = "SessionId")
        {
            Dictionary<string, string> siteCookies = new Dictionary<string, string>();
            var tempArry = cookies.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (var temp in tempArry)
            {
                var tempKeyArry = temp.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (tempKeyArry.Count > 1)
                {
                    siteCookies.Add(tempKeyArry[0].TrimStart().TrimEnd(), tempKeyArry[1]);
                }
            }
            //return keyValue;
            var session = siteCookies.Select(c => c).Where(c => c.Key.Trim().ToUpper() == key.Trim().ToUpper()).FirstOrDefault();
            if (!string.IsNullOrEmpty(session.Value))
                return session.Value;
            else
                return "";
        }
        public void LoginSite()
        { 
            DateTime viewTimer = new DateTime();
            WebBrowserNavigate(this.myWebBrowser, "https://www.infocentre.pmu.fr/PMU/Phantoms/PhantomDemarrage");
            if (myWebBrowser.Document != null)
            {
                HtmlElement formLogin = myWebBrowser.Document.GetElementById("formLogin");
                if (formLogin != null)
                {
                    var codeContact = formLogin.Document.GetElementById("codeContact");
                    if (codeContact != null)
                    {
                        codeContact.SetAttribute("value", "Callen");
                    }
                    HtmlElement submitButton = null;
                    HtmlElementCollection mdpContacts = formLogin.Document.GetElementsByTagName("input");
                    foreach (HtmlElement mdpContact in mdpContacts)
                    {
                        if (mdpContact.Name == "mdpContact")
                        {
                            mdpContact.SetAttribute("value", "12345678");
                        }
                        if (mdpContact.GetAttribute("type") == "submit")
                        {
                            submitButton = mdpContact;
                        }
                    }
                    if (submitButton != null)
                    {
                        submitButton.InvokeMember("click");
                        Application.DoEvents();
                    }
                }
                //Waiting
                Waiting(10, "Login", "Login iinfocentre.pmu.fr...");
                viewTimer = DateTime.Now.AddMinutes(2);
                while (myWebBrowser.ReadyState != WebBrowserReadyState.Complete || myWebBrowser.IsBusy)
                {
                    Application.DoEvents();
                    if (DateTime.Compare(DateTime.Now, viewTimer) >= 0 || stopApp)
                    {
                        break;
                    }
                }
                Application.DoEvents();
                string mycookies = myWebBrowser.Document.Cookie;
                InitWebBrowser(myWebBrowser);
            }
        }

        public void Waiting(double wtime, string waitingFor, string finishStatus)
        {
            DateTime waitingTimer = DateTime.Now.AddSeconds(wtime);
            while (DateTime.Compare(DateTime.Now, waitingTimer) < 0 && !stopApp)
            {
                //label9.Text = "Waiting For " + waitingFor + " " + (waitingTimer - DateTime.Now).TotalSeconds.ToString("0") + "s...";
                WriteToLable(label9," Waiting For " + waitingFor + " " + (waitingTimer - DateTime.Now).TotalSeconds.ToString("0") + "s...");
                Application.DoEvents();
            }
            WriteToLable(label9,finishStatus);
        }
    
        public void WebBrowserNavigate(WebBrowser webBrowser, string URL = "", int timeout = 60)
        {
            //FlushMemory();
            //int NoOfTry = 5;
            var startTime = DateTime.Now;
            Sleep(RandomGen.Next(10));
            if (!string.IsNullOrEmpty(URL))
            {
                webBrowser.Navigate(new Uri(URL));
                while ((webBrowser.ReadyState != WebBrowserReadyState.Complete || webBrowser.IsBusy) && DateTime.Now < startTime.AddSeconds(timeout))
                {
                    Application.DoEvents();
                }
            }
        }

        public void TryLoad(WebBrowser webBrowser, string URL = "", int timeout = 60)
        {
            if (DownloadAtRandomInterval)
            {
                Sleep(RandomGen.Next(10));
            }
            if (!string.IsNullOrEmpty(URL))
            {
                webBrowser.Navigate(new Uri(URL));
            }
            var startTime = DateTime.Now;
            Application.DoEvents();
            while ((webBrowser.ReadyState != WebBrowserReadyState.Complete || webBrowser.IsBusy) && DateTime.Now < startTime.AddSeconds(timeout))
            {
                Application.DoEvents();
            }
        }

        public void Sleep(int seconds)
        {
            var Current = DateTime.Now.AddSeconds(seconds);
            while (DateTime.Now < Current)
            {
                Application.DoEvents();
            }
        }

        public void FlushMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public List<RaceInfo> ReadLineFile(string filePath)
        {
            var contents = new List<RaceInfo>();
            FileStream fileStream = null;
            StreamReader streamReader = null;
            myProgressBar1.Value = 0;
            myProgressBar1.Maximum = 100;
            try
            {
                int curLine = 1;
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                streamReader = new StreamReader(fileStream, Encoding.Default);
                fileStream.Seek(0, SeekOrigin.Begin);
                string[] lines = System.IO.File.ReadAllLines(filePath);
                string content = streamReader.ReadLine();
                while (content != null)
                {
                    if (content.IndexOf("|") >= 0)
                    {
                        label9.Text = "[" + curLine + "/" + lines.Length + "] Importing...";
                        Application.DoEvents();
                        //Harnessed|20592805|HIPPODROME DE PARIS-VINCENNES |1/01/2014|http://www.infocentre.pmu.fr/PMU/Phantoms/PhantomHome?P1=20592805&L=2&J=20140101&T=1.shtml|20663498|1|DE CORDEMAIS |http://www.infocentre.pmu.fr/PMU/Phantoms/PhantomHome?P1=20592805&L=2&J=20140101&P2=20663498.shtml
                        var splitArry = content.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        if (splitArry.Length >= 1)
                        {
                            var raceInfo = new RaceInfo();
                            raceInfo.MeetingDiscipline = "T";
                            if (splitArry[0] == "Harnessed" || splitArry[0] == "Attelé")
                            {
                                raceInfo.MeetingDiscipline = "H";
                            }
                            else if (splitArry[0] == "Flat" || splitArry[0] == "Plat")
                            { }
                            else if (splitArry[0] == "Ridden" || splitArry[0] == "Monté")
                            {
                                raceInfo.MeetingDiscipline = "H";
                            }
                            else if (splitArry[0] == "Steeple Chase" || splitArry[0] == "Cross" || splitArry[0] == "Hurdle")
                            { }
                            else
                            {
                                CommonHelper.WriteLog("Discipline Not Found:  " + splitArry[0], "Discipline");
                            }
                            raceInfo.MeetingId = splitArry[1];
                            raceInfo.MeetingCourse = splitArry[2].Replace("Hippodrome de", "").Replace("HIPPODROME D'", "").Replace("HIPPODROME DE", "");
                            raceInfo.MeetingDate = DateTime.ParseExact(splitArry[3], "d/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                            //raceInfo.MeetingCourseCode = kGetCourseCode(raceInfo.MeetingCourse.ToUpper().TrimStart().TrimEnd());
                            //raceInfo.MeetingCourseID = kGetCourseID(raceInfo.MeetingCourse.ToUpper().TrimStart().TrimEnd());
                            raceInfo.RaceId = splitArry[5];
                            raceInfo.RaceNumber = splitArry[6];
                            raceInfo.RaceName = splitArry[7];
                            raceInfo.PlayerUrl = String.Format("http://www.infocentre.pmu.fr/PMU/Phantoms/PhantomDocumentSousOnglet?0,0,P1={0}&L=2&J={1}&T=1&VD=1&R=304&SR=3044&P2={2}.shtml", raceInfo.MeetingId, raceInfo.MeetingDate.ToString("yyyyMMdd"), raceInfo.RaceId);
                            //Harnessed|20592805|HIPPODROME DE PARIS-VINCENNES |1/01/2014|http://www.infocentre.pmu.fr/PMU/Phantoms/PhantomHome?P1=20592805&L=2&J=20140101&T=1.shtml|20663502|2|CROISE-LAROCHE |http://www.infocentre.pmu.fr/PMU/Phantoms/PhantomHome?P1=20592805&L=2&J=20140101&P2=20663502.shtml

                            if (!string.IsNullOrEmpty(raceInfo.MeetingCourseCode))
                            {
                                contents.Add(raceInfo);
                                CommonHelper.WriteFile("" + raceInfo.MeetingDiscipline + "|" + raceInfo.MeetingId + "|" + raceInfo.MeetingCourse + "|" + raceInfo.MeetingCourseCode + "|" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "|" + raceInfo.RaceId + "|" + raceInfo.RaceName + "|" + raceInfo.RaceNumber + "|" + raceInfo.PlayerUrl, "RSRaceData");
                            }
                            else
                            {
                                CommonHelper.WriteFile("" + raceInfo.MeetingDiscipline + "|" + raceInfo.MeetingId + "|" + raceInfo.MeetingCourse + "|" + raceInfo.MeetingCourseCode + "|" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "|" + raceInfo.RaceId + "|" + raceInfo.RaceName + "|" + raceInfo.RaceNumber + "|" + raceInfo.PlayerUrl, "RSRaceData-Null");
                            }
                        }
                    }
                    content = streamReader.ReadLine();
                    double percent = curLine / lines.Length * 100;
                    myProgressBar1.Value = Convert.ToInt32(percent) <= 0 ? 1 : Convert.ToInt32(percent);
                    Application.DoEvents();
                    curLine++;
                }
            }
            catch (Exception ex)
            {
                CommonHelper.WriteLog(ex.Message, "ReadLineFile");
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }
            return contents;
        }


        public List<RaceInfo> ReadRSLineFile(string filePath)
        {
            var contents = new List<RaceInfo>();
            FileStream fileStream = null;
            StreamReader streamReader = null;
            myProgressBar1.Value = 0;
            myProgressBar1.Maximum = 100;
            try
            {
                int curLine = 1;
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                streamReader = new StreamReader(fileStream, Encoding.Default);
                fileStream.Seek(0, SeekOrigin.Begin);
                string[] lines = System.IO.File.ReadAllLines(filePath);
                string content = streamReader.ReadLine();
                int Counter = 0;
                while (content != null)
                {
                    if (content.IndexOf("|") >= 0)
                    {
                        label9.Text = "[" + curLine + "/" + lines.Length + "/" + Counter + "] Importing...";
                        Application.DoEvents();
                        // CommonHelper.WriteFile("" + raceInfo.MeetingDiscipline + "|" + raceInfo.MeetingId + "|" + raceInfo.MeetingCourse + "|" + raceInfo.MeetingCourseCode + "|" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "|" + raceInfo.RaceId + "|" + raceInfo.RaceName + "|" + raceInfo.RaceNumber + "|" + raceInfo.PlayerUrl, "RSRaceDada");
                        //H|20592805| PARIS-VINCENNES |PAVI|2014-01-01|20663498|DE CORDEMAIS |1|http://www.infocentre.pmu.fr/PMU/Phantoms/PhantomDocumentSousOnglet?0,0,P1=20592805&L=2&J=20140101&T=1&VD=1&R=304&SR=3044&P2=20663498.shtml
                        var splitArry = content.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        if (splitArry.Length >= 1)
                        {
                            try
                            {
                                var raceInfo = new RaceInfo();
                                raceInfo.MeetingDiscipline = System.Web.HttpUtility.UrlEncode(splitArry[0]).Replace("%ef", "").Replace("%bb", "").Replace("%bf", "").Trim().ToUpper();
                                raceInfo.MeetingId = splitArry[1];
                                raceInfo.MeetingCourse = splitArry[2].TrimStart().TrimEnd();
                                raceInfo.MeetingDate = DateTime.ParseExact(splitArry[4], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                                raceInfo.RaceId = splitArry[5];
                                raceInfo.RaceName = splitArry[6];
                                raceInfo.RaceNumber = splitArry[7].Trim();
                                if (raceInfo.RaceNumber.Length == 1)
                                    raceInfo.RaceNumber = "0" + raceInfo.RaceNumber;
                                raceInfo.PlayerUrl = splitArry[8];

                                if (raceInfo.MeetingDiscipline == "T")
                                {
                                    //CommonHelper.WriteFile("" + raceInfo.MeetingDiscipline + "|" + raceInfo.MeetingCourse + "|" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "|" + raceInfo.RaceNumber + "", "kGetCourseCode-SQL");

                                    raceInfo.MeetingCourseCode = kGetCourseCode(raceInfo.MeetingCourse, raceInfo.MeetingDate.ToString("yyyy-MM-dd"), Convert.ToInt32(raceInfo.RaceNumber), raceInfo.MeetingDiscipline);

                                    if (!string.IsNullOrEmpty(raceInfo.MeetingCourseCode))
                                    {
                                        string sql = "select * from tracker where CourseName='" + raceInfo.MeetingCourse.Replace("'", "''").Replace("-", " ").TrimStart().TrimEnd() + "' and RaceDate='" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "'";
                                        var exist = mySQLiteHelper.ExecuteScalar(sql);
                                        if (Convert.ToInt32(exist) > 0)
                                        {
                                            contents.Add(raceInfo);
                                            Counter++;
                                        }
                                        else
                                        {
                                            CommonHelper.WriteFile("" + raceInfo.MeetingCourse + "|" + raceInfo.MeetingCourseCode + "|" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "|" + raceInfo.RaceId + "|" + raceInfo.RaceName + "|" + raceInfo.RaceNumber + "|" + raceInfo.PlayerUrl, "Tracker-Null");
                                        }
                                    }
                                    else
                                    {
                                        CommonHelper.WriteFile("" + raceInfo.MeetingDiscipline + "|" + raceInfo.MeetingId + "|" + raceInfo.MeetingCourse + "|" + raceInfo.MeetingCourseCode + "|" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "|" + raceInfo.RaceId + "|" + raceInfo.RaceName + "|" + raceInfo.RaceNumber + "|" + raceInfo.PlayerUrl, "kGetCourseCode-Null");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                CommonHelper.WriteLog(ex.Message, "ReadRSLineFile");
                            }
                           
                        }
                    }
                    content = streamReader.ReadLine();
                    InvokeProgressChanged(curLine, lines.Length);
                    Application.DoEvents();
                    curLine++;
                }
            }
            catch (Exception ex)
            {
                CommonHelper.WriteLog(ex.Message, "ReadRSLineFile");
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }
            return contents;
        }

    
        private void InvokeProgressChanged(int CurLine, int TotleLine)
        {
            if (this.InvokeRequired == true)
            {
                this.BeginInvoke(new MethodInvoker(delegate()
                {
                    decimal MyDecimal = (Convert.ToDecimal(CurLine) / Convert.ToDecimal(TotleLine)) * 100;
                    this.myProgressBar1.Value = Convert.ToInt32(MyDecimal);
                }));
            }
            else
            {
                decimal MyDecimal = (Convert.ToDecimal(CurLine) / Convert.ToDecimal(TotleLine)) * 100;
                this.myProgressBar1.Value = Convert.ToInt32(MyDecimal);
            }
        }

        public List<RaceInfo> ReadTrackerLineFile(string filePath)
        {
            var contents = new List<RaceInfo>();
            FileStream fileStream = null;
            StreamReader streamReader = null;
            myProgressBar1.Value = 0;
            myProgressBar1.Maximum = 100;
            try
            {
                int curLine = 1;
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                streamReader = new StreamReader(fileStream, Encoding.Default);
                fileStream.Seek(0, SeekOrigin.Begin);
                string[] lines = System.IO.File.ReadAllLines(filePath);
                string content = streamReader.ReadLine();
                while (content != null)
                {
                    if (content.IndexOf("|") >= 0)
                    {
                        label9.Text = "[" + curLine + "/" + lines.Length + "] Importing tracker...";
                        Application.DoEvents();
                        var splitArry = content.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        if (splitArry.Length >= 1)
                        {
                            var raceInfo = new RaceInfo();
                            raceInfo.MeetingDate = DateTime.ParseExact(splitArry[0], "d-M-yy", System.Globalization.CultureInfo.InvariantCulture);
                            raceInfo.MeetingCourse = splitArry[1].TrimStart().TrimEnd();
                            raceInfo.RaceNumber = splitArry[2].Trim();
                            contents.Add(raceInfo);
                        }
                    }
                    content = streamReader.ReadLine();
                    double percent = curLine / lines.Length * 100;
                    myProgressBar1.Value = Convert.ToInt32(percent) <= 0 ? 1 : Convert.ToInt32(percent);
                    Application.DoEvents();
                    curLine++;
                }
            }
            catch (Exception ex)
            {
                CommonHelper.WriteLog(ex.Message, "ReadRSLineFile");
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }
            return contents;
        }



        public List<RaceInfo> ReadAWLineFile(string filePath)
        {
            var contents = new List<RaceInfo>();
            FileStream fileStream = null;
            StreamReader streamReader = null;
            myProgressBar1.Value = 0;
            myProgressBar1.Maximum = 100;
            try
            {
                int curLine = 1;
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                streamReader = new StreamReader(fileStream, Encoding.Default);
                fileStream.Seek(0, SeekOrigin.Begin);
                string[] lines = System.IO.File.ReadAllLines(filePath);
                string content = streamReader.ReadLine();
                int Counter = 0;
                while (content != null)
                {
                    if (content.IndexOf("|") >= 0)
                    {
                        label9.Text = "[" + curLine + "/" + lines.Length + "/" + Counter + "] Importing...";
                        Application.DoEvents();
                        // CommonHelper.WriteFile("" + raceInfo.MeetingDiscipline + "|" + raceInfo.MeetingId + "|" + raceInfo.MeetingCourse + "|" + raceInfo.MeetingCourseCode + "|" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "|" + raceInfo.RaceId + "|" + raceInfo.RaceName + "|" + raceInfo.RaceNumber + "|" + raceInfo.PlayerUrl, "RSRaceDada");
                        //H|20592805| PARIS-VINCENNES |PAVI|2014-01-01|20663498|DE CORDEMAIS |1|http://www.infocentre.pmu.fr/PMU/Phantoms/PhantomDocumentSousOnglet?0,0,P1=20592805&L=2&J=20140101&T=1&VD=1&R=304&SR=3044&P2=20663498.shtml
                        var splitArry = content.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        if (splitArry.Length >= 1)
                        {
                            try
                            {
                                var raceInfo = new RaceInfo();
                                raceInfo.MeetingDiscipline = System.Web.HttpUtility.UrlEncode(splitArry[0]).Replace("%ef", "").Replace("%bb", "").Replace("%bf", "").Trim().ToUpper();
                                raceInfo.MeetingId = splitArry[1];
                                raceInfo.MeetingCourse = splitArry[2].TrimStart().TrimEnd();
                                raceInfo.MeetingDate = DateTime.ParseExact(splitArry[4], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                                raceInfo.RaceId = splitArry[5];
                                raceInfo.RaceName = splitArry[6];
                                raceInfo.RaceNumber = splitArry[7].Trim();
                                if (raceInfo.RaceNumber.Length == 1)
                                    raceInfo.RaceNumber = "0" + raceInfo.RaceNumber;
                                raceInfo.PlayerUrl = splitArry[8];

                                string sql = "select SourceCourse from rs.tblcourselookup join rs.tblcourses on CrsUniqueID = SourceCourse where abbreviation = '" + raceInfo.MeetingCourse.Replace("'", "''") + " AW' group by SourceCourse";
                                DataTable dt = mySqlHelper.ExecuteDataTable(sql);
                                string RCourse="";
                                if (dt.Rows.Count > 0)
                                {
                                    DataTable dt2 = null;
                                    foreach (var row in dt.Rows)
                                    {
                                        sql = "select RCourse from tblraces where RDate='" + splitArry[4] + "' and  RRaceNo=" + Convert.ToInt32(raceInfo.RaceNumber) + " and RCourseID = " + Convert.ToInt64(dt.Rows[0]["SourceCourse"]) + " and RDiscipline='" + raceInfo.MeetingDiscipline + "'";
                                        dt2 = mySqlHelper.ExecuteDataTable(sql);
                                        if (dt2.Rows.Count > 0)
                                        {
                                            RCourse = dt2.Rows[0]["RCourse"].ToString();
                                            break;
                                        }

                                    }
                                    if (dt2 != null)
                                        dt2.Dispose();
                                }
                                if (dt != null)
                                    dt.Dispose();

                                raceInfo.MeetingCourseCode = RCourse;
                                if (!string.IsNullOrEmpty(raceInfo.MeetingCourseCode))
                                {
                                    contents.Add(raceInfo);
                                    Counter++;
                                }
                            }
                            catch (Exception ex)
                            {
                                CommonHelper.WriteLog(ex.Message, "ReadRSLineFile");
                            }

                        }
                    }
                    content = streamReader.ReadLine();
                    InvokeProgressChanged(curLine, lines.Length);
                    curLine++;
                }
            }
            catch (Exception ex)
            {
                CommonHelper.WriteLog(ex.Message, "ReadRSLineFile");
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }
            return contents;
        }

        public string kGetCourseCode(string CourseName, string RDate, int RRaceNo, string RDiscipline)
        {
            string RCourse = "";
            DataTable dt;
            string sql = "";
            sql = "select SourceCourse from rs.tblcourselookup join rs.tblcourses on CrsUniqueID = SourceCourse where abbreviation = '" + CourseName.Replace("'", "''") + "' group by SourceCourse";
            dt = mySqlHelper.ExecuteDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                var sourceCourses = new List<long>();
                DataTable dt2 = null;
                var i = 0;
                foreach (var row in dt.Rows)
                {
                    sourceCourses.Add(Convert.ToInt64(dt.Rows[i]["SourceCourse"]));
                    i++;
                }
                if (sourceCourses.Count > 0)
                {
                    sql = "select RCourse from tblraces where RDate='" + RDate + "' and  RRaceNo=" + RRaceNo + " and RCourseID in(" + string.Join(",", sourceCourses.ToArray()) + ") and RDiscipline='" + RDiscipline + "'";
                    dt2 = mySqlHelper.ExecuteDataTable(sql);
                    if (dt2.Rows.Count > 0)
                    {
                        RCourse = dt2.Rows[0]["RCourse"].ToString();
                    }
                }
                if (dt2 != null)
                    dt2.Dispose();
            }

            if (string.IsNullOrEmpty(RCourse))
            {
                var sourceCourses = new List<long>();
                DataTable dt2 = null;
                sql = "select SourceCourse from rs.tblcourselookup join rs.tblcourses on CrsUniqueID = SourceCourse where abbreviation = '" + CourseName.Replace("'", "''") + " AW' group by SourceCourse";
                dt = mySqlHelper.ExecuteDataTable(sql);
                var i = 0;
                foreach (var row in dt.Rows)
                {
                    sourceCourses.Add(Convert.ToInt64(dt.Rows[i]["SourceCourse"]));
                    i++;
                }
                if (sourceCourses.Count>0)
                {
                    sql = "select RCourse from tblraces where RDate='" + RDate + "' and  RRaceNo=" + RRaceNo + " and RCourseID in(" + string.Join(",", sourceCourses.ToArray()) + ") and RDiscipline='" + RDiscipline + "'";
                    dt2 = mySqlHelper.ExecuteDataTable(sql);
                    if (dt2.Rows.Count > 0)
                    {
                        RCourse = dt2.Rows[0]["RCourse"].ToString();
                    }
                }
                if (dt2 != null)
                    dt2.Dispose();
            }

            //替换HIPPODROME DE 再次查找
            if (string.IsNullOrEmpty(RCourse))
            {
                CourseName = CourseName.Replace("'", "''");
                CourseName = CourseName.Replace("HIPPODROME DE", "").TrimStart().TrimEnd();

                var sourceCourses = new List<long>();
                DataTable dt2 = null;
                sql = "select SourceCourse from rs.tblcourselookup join rs.tblcourses on CrsUniqueID = SourceCourse where abbreviation = '" + CourseName + "' group by SourceCourse";
                dt = mySqlHelper.ExecuteDataTable(sql);
                var i = 0;
                foreach (var row in dt.Rows)
                {
                    sourceCourses.Add(Convert.ToInt64(dt.Rows[i]["SourceCourse"]));
                    i++;
                }
                if (sourceCourses.Count > 0)
                {
                    sql = "select RCourse from tblraces where RDate='" + RDate + "' and  RRaceNo=" + RRaceNo + " and RCourseID in(" + string.Join(",", sourceCourses.ToArray()) + ") and RDiscipline='" + RDiscipline + "'";
                    dt2 = mySqlHelper.ExecuteDataTable(sql);
                    if (dt2.Rows.Count > 0)
                    {
                        RCourse = dt2.Rows[0]["RCourse"].ToString();
                    }
                }
                if (dt2 != null)
                    dt2.Dispose();

                if (string.IsNullOrEmpty(RCourse))
                {
                    sourceCourses = new List<long>();
                    DataTable dt3 = null;
                    sql = "select SourceCourse from rs.tblcourselookup join rs.tblcourses on CrsUniqueID = SourceCourse where abbreviation = '" + CourseName.Replace("'", "''") + " AW' group by SourceCourse";
                    dt = mySqlHelper.ExecuteDataTable(sql);
                    i = 0;
                    foreach (var row in dt.Rows)
                    {
                        sourceCourses.Add(Convert.ToInt64(dt.Rows[i]["SourceCourse"]));
                        i++;
                    }
                    if (sourceCourses.Count > 0)
                    {
                        sql = "select RCourse from tblraces where RDate='" + RDate + "' and  RRaceNo=" + RRaceNo + " and RCourseID in(" + string.Join(",", sourceCourses.ToArray()) + ") and RDiscipline='" + RDiscipline + "'";
                        dt3 = mySqlHelper.ExecuteDataTable(sql);
                        if (dt3.Rows.Count > 0)
                        {
                            RCourse = dt3.Rows[0]["RCourse"].ToString();
                        }
                    }
                    if (dt3 != null)
                        dt3.Dispose();
                }
            }
            dt.Dispose();
            return RCourse;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //20140101-PAVI﻿H-race03
            //System.Web.HttpUtility.
            //string name = System.Web.HttpUtility.UrlEncode("20140101-PAVI﻿H-race03").Replace("%ef", "").Replace("%bb", "").Replace("%bf", "");
            //"20140101-PAVI%ef%bb%bfH-race03"
           // System.Diagnostics.Debug.Print(name);
            this.BeginInvoke(new Action(() =>
            {
                button2.Text = "" + GetFileNum(VideoRootPath);
            }));
        }

        public int GetFileNum(string srcPath)
        {
            string rsRacedatafile = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Files\\RSAWRaceData.txt";
            var raceLists = new List<RaceInfo>();
            var fileInfo = new FileInfo(rsRacedatafile);
            var racelists = ReadAWLineFile(rsRacedatafile);

            int fileNum = 0;
            //创建一个队列用于保存子目录
            Queue<string> pathQueue = new Queue<string>();
            //首先把根目录排入队中
            pathQueue.Enqueue(srcPath);
            //开始循环查找文件，直到队列中无任何子目录
            while (pathQueue.Count > 0)
            {
                //从队列中取出一个目录，把该目录下的所有子目录排入队中
                DirectoryInfo diParent = new DirectoryInfo(pathQueue.Dequeue());
                foreach (DirectoryInfo diChild in diParent.GetDirectories())
                    pathQueue.Enqueue(diChild.FullName);
                //查找该目录下的所有文件，依次处理
                foreach (FileInfo fi in diParent.GetFiles())
                {
                    //Computer MyComputer = new Computer();
                    //string newfilename = System.Web.HttpUtility.UrlEncode(fi.Name).Replace("%ef", "").Replace("%bb", "").Replace("%bf", "");
                    //newfilename = System.Web.HttpUtility.UrlDecode(newfilename);
                    //string filename = fi.FullName;
                    //string newfilename = "";
                    ////20140101-PAVIFH-race03.mp4
                    //try
                    //{
                    //    string tempPath = VideoRootPath + "Temp\\";
                    //    var temArry = fi.Name.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    //    if (temArry.Length > 0)
                    //    {
                    //        var fileDate = DateTime.ParseExact(temArry[0], "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                    //        tempPath += fileDate.ToString("yyyy") + "\\" + fileDate.ToString("MM") + "\\" + fileDate.ToString("dd") + "\\";
                    //        string meetingCourseCode = temArry[1].Substring(0, temArry[1].Length - 1);
                    //        int RRaceNo = Convert.ToInt32(temArry[2].Replace("race", "").Replace(".mp4", ""));
                    //        string discipline = temArry[1].Substring(temArry[1].Length - 1, 1);
                    //        //"CAGAW"
                    //        //"GAGN"
                    //        var currace = racelists.Select(r => r).Where(r => r.MeetingDiscipline == discipline && r.MeetingDate.ToString("yyyy-MM-dd") == fileDate.ToString("yyyy-MM-dd") && Convert.ToInt32(r.RaceNumber) == RRaceNo && meetingCourseCode.IndexOf(r.MeetingCourseCode) >= 0).FirstOrDefault();
                    //        if (currace != null)
                    //        {
                    //            newfilename = temArry[0] + "-" + currace.MeetingCourseCode + discipline + "-" + temArry[2];
                    //            if (!Directory.Exists(tempPath))
                    //            {
                    //                Directory.CreateDirectory(tempPath);
                    //            }
                    //            fi.CopyTo(tempPath + newfilename);
                    //        }
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    System.Diagnostics.Debug.Print(ex.Message);
                    //}
                    fileNum++;
                }
            }
            return fileNum;
        }


        public bool CourseCodeIsExist(string RCourse, string RDate, int RRaceNo, string RDiscipline)
        {
            bool isExist = false;
            DataTable dt;
            string sql = "";
            sql = "select RCourseID from tblraces where RCourse='" + RCourse + "' and RDate='" + RDate + "' and  RRaceNo=" + RRaceNo + " and RDiscipline='" + RDiscipline + "'";
            dt = mySqlHelper.ExecuteDataTable(sql);
            if (dt.Rows.Count > 0)
            {
                RCourse = dt.Rows[0]["RCourseID"].ToString();
                isExist = true;
            }
            dt.Dispose();
            return isExist;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
            string rsRacedatafile = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Files\\Tracker.txt";
            var raceLists = new List<RaceInfo>();
            var fileInfo = new FileInfo(rsRacedatafile);
            if (fileInfo.Exists)
            {
                raceLists = ReadTrackerLineFile(rsRacedatafile);
                myProgressBar1.Value = 0;
                Application.DoEvents();
                int index = 0;
                foreach (var race in raceLists)
                {
                    index++;
                    label9.Text = "[" + index + "/" + raceLists.Count + "] Add Tracker...";
                    //=======================================================
                    try
                    {
                        string sql = "select * from tracker where CourseName='" + race.MeetingCourse.Replace("'", "''").Replace("-", " ") + "' and RaceDate='" + race.MeetingDate.ToString("yyyy-MM-dd") + "' and RaceNumber=" + race.RaceNumber + "";
                        var exist = mySQLiteHelper.ExecuteScalar(sql);
                        if (Convert.ToInt32(exist) == 0)
                        {
                            //"Lion D'Angers"
                            sql = "insert into tracker (CourseName,RaceDate,RaceNumber) values ('" + race.MeetingCourse.Replace("'", "''").Replace("-", " ").ToUpper() + "','" + race.MeetingDate.ToString("yyyy-MM-dd") + "','" + race.RaceNumber + "')";
                            mySQLiteHelper.ExecuteNonQuery(sql);
                        }
                    }
                    catch (Exception ex)
                    {
                        CommonHelper.WriteLog(ex.Message, "ExecuteScalar");
                    }
                    //=======================================================
                    myProgressBar1.Value = index / raceLists.Count * 100;
                    Application.DoEvents();
                }
            }
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            RaceInfoForm fl = new RaceInfoForm();
            fl.Owner = this;
            fl.ShowDialog();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string filePath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Files\\Tracker-Null.txt";
            var contents = new List<string>();
            FileStream fileStream = null;
            StreamReader streamReader = null;
            myProgressBar1.Value = 0;
            myProgressBar1.Maximum = 100;
            try
            {
                int curLine = 1;
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                streamReader = new StreamReader(fileStream, Encoding.Default);
                fileStream.Seek(0, SeekOrigin.Begin);
                string[] lines = System.IO.File.ReadAllLines(filePath);
                string content = streamReader.ReadLine();
                int Counter = 0;
                while (content != null)
                {
                    if (content.IndexOf("|") >= 0)
                    {
                        label9.Text = "[" + curLine + "/" + lines.Length + "/" + Counter + "] Importing...";
                        Application.DoEvents();

                        //CAGNES-SUR-MER|CAGN|2014-01-01|20450021|FRAGONARD |01|http://www.infocentre.pmu.fr/PMU/Phantoms/PhantomDocumentSousOnglet?0,0,P1=20450020&L=2&J=20140101&T=1&VD=1&R=304&SR=3044&P2=20450021.shtml

                        var splitArry = content.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        if (splitArry.Length >= 1)
                        {
                            try
                            {
                                var raceInfo = new RaceInfo();
                                raceInfo.MeetingCourse = splitArry[0].TrimStart().TrimEnd();
                                raceInfo.MeetingDate = DateTime.ParseExact(splitArry[2], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                                raceInfo.RaceNumber = splitArry[5].Trim();

                                string sql = "select * from tracker where  RaceDate='" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "' and RaceNumber=" + Convert.ToInt32(raceInfo.RaceNumber) + "";
                                var exist = mySQLiteHelper.ExecuteScalar(sql);
                                if (Convert.ToInt32(exist) > 0)
                                {
                                    if (!contents.Contains(raceInfo.MeetingCourse))
                                    {
                                        contents.Add(raceInfo.MeetingCourse);
                                        CommonHelper.WriteFile("" + raceInfo.MeetingCourse + "|" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "|" + raceInfo.RaceNumber ,  "Match-Tracker");
                                    }
                                }
                                else
                                {
                                    //CommonHelper.WriteFile("" + raceInfo.MeetingCourse + "|" + raceInfo.MeetingCourseCode + "|" + raceInfo.MeetingDate.ToString("yyyy-MM-dd") + "|" + raceInfo.RaceId + "|" + raceInfo.RaceName + "|" + raceInfo.RaceNumber + "|" + raceInfo.PlayerUrl, "Tracker-Null");
                                }

                            }
                            catch (Exception ex)
                            {
                                CommonHelper.WriteLog(ex.Message, "ReadRSLineFile");
                            }

                        }
                    }
                    content = streamReader.ReadLine();
                    InvokeProgressChanged(curLine, lines.Length);
                    Application.DoEvents();
                    curLine++;
                }
            }
            catch (Exception ex)
            {
                CommonHelper.WriteLog(ex.Message, "ReadRSLineFile");
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }
                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }
        }  

    }

}



namespace M3U8_Downloader
{
    class MyProgressBar : ProgressBar //新建一个MyProgressBar类，它继承了ProgressBar的所有属性与方法
    {
        public MyProgressBar()
        {
            base.SetStyle(ControlStyles.UserPaint, true);//使控件可由用户自由重绘
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            SolidBrush brush = null;
            Rectangle bounds = new Rectangle(0, 0, base.Width, base.Height);
            e.Graphics.FillRectangle(new SolidBrush(this.BackColor), 1, 1, bounds.Width - 2, bounds.Height - 2);//此处完成背景重绘，并且按照属性中的BackColor设置背景色
            bounds.Height -= 4;
            bounds.Width = ((int)(bounds.Width * (((double)base.Value) / ((double)base.Maximum)))) - 4;//是的进度条跟着ProgressBar.Value值变化
            brush = new SolidBrush(this.ForeColor);
            e.Graphics.FillRectangle(brush, 2, 2, bounds.Width, bounds.Height);//此处完成前景重绘，依旧按照Progressbar的属性设置前景色
        }
    }

    // 1.定义委托  
    public delegate void DelReadStdOutput(string result);
    public delegate void DelReadErrOutput(string result);

    public partial class PMUDownloaderForm : Form
    {
        // 2.定义委托事件  
        public event DelReadStdOutput ReadStdOutput;
        public event DelReadErrOutput ReadErrOutput;


        private void button_Download_Click(object sender, EventArgs e)
        {
            var downloadFile = new DownloadFile();
            this.Invoke(new Action(() => { DownloadVideo(downloadFile); }));
        }

        private void DownloadVideo(DownloadFile downloadFile)
        {
            if (!Directory.Exists(downloadFile.DownLoadPath))
            {
                Directory.CreateDirectory(downloadFile.DownLoadPath);
            }
            else
            {
                SetTextBox(textBox_Info, "");
                SetTextBox(textBox_forRegex, "");
                //===================
                Download(downloadFile);
                //===================
                InitComponent();
            }
        }

        public ApplicationOutput DownLoadDoWorkAsync(DownloadFile downloadFile, bool ignoreErrorCode)
        {
            this.CurDownloaded = false;
            using (Process CmdProcess = new Process())
            {
                CmdProcess.StartInfo.FileName = @"Tools\ffmpeg.exe";      // 命令  
                CmdProcess.StartInfo.Arguments = downloadFile.CommandText;      // 参数  

                CmdProcess.StartInfo.CreateNoWindow = true;         // 不创建新窗口  
                CmdProcess.StartInfo.UseShellExecute = false;
                CmdProcess.StartInfo.RedirectStandardInput = true;  // 重定向输入  
                CmdProcess.StartInfo.RedirectStandardOutput = true; // 重定向标准输出  
                CmdProcess.StartInfo.RedirectStandardError = true;  // 重定向错误输出  
                //CmdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;  

                StringBuilder stdOutput = new StringBuilder();
                StringBuilder stdError = new StringBuilder();
                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    CmdProcess.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            //stdOutput.AppendLine(e.Data);
                            ReadStdOutputAction(e.Data.ToString());
                        }
                        else
                        {
                            //outputWaitHandle.Set();
                        }
                    };

                    CmdProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            //stdError.AppendLine(e.Data);
                            if (e.Data.IndexOf("Forbidden") >= 0 || e.Data.IndexOf("Server 404") >= 0 || e.Data.IndexOf("403 Forbidden") >= 0)
                            {
                                this.CurDownloaded = true;
                            }
                            ReadErrOutputAction(e.Data.ToString());
                        }
                        else
                        {
                            //errorWaitHandle.Set();
                        }
                    };

                    string processOutput = string.Empty;

                    // 启用Exited事件  
                    CmdProcess.EnableRaisingEvents = true;

                    // 注册进程结束事件  
                    CmdProcess.Exited += new EventHandler(CmdProcess_Exited);

                    CmdProcess.Start();

                    //获取ffmpeg.exe的进程ID
                    ffmpegid = CmdProcess.Id;

                    CmdProcess.BeginOutputReadLine();
                    CmdProcess.BeginErrorReadLine();

                    bool completed = false;

                    var viewTimer = DateTime.Now.AddMinutes(30);
                    while (!CmdProcess.HasExited || DateTime.Compare(DateTime.Now, viewTimer) >= 0)
                    {
                        Application.DoEvents();
                    }
                    completed = true;
                    return new ApplicationOutput
                    {
                        ReturnValue = (uint)CmdProcess.ExitCode,
                        OutPut = processOutput,
                        IsCompleted = completed
                    };
                }
            }
        }

        public async Task<bool> DownLoadDoWorkAsync(DownloadFile downloadFile, IProgress<DownloadFile> progress)
        {
            return await Task.Run(() =>
            {
                return false;
            });
        }

        internal void SetTextBox(TextBox textBox, String message)
        {
            if (textBox.InvokeRequired)
            {
                this.Invoke((Action)(() =>
                    SetTextBox(textBox, message)
                ));
                return;
            }
            textBox.Text = string.Format("{0}", message);
        }

        internal void InitComponent()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => {
                    linkLabel_Stop.Visible = true;
                    label5.Visible = true;
                    label6.Visible = true;
                    label7.Visible = true;
                    label8.Visible = true;
                    timer1.Enabled = true;
                    timer2.Enabled = true;
                }));
            }
            linkLabel_Stop.Visible = true;
            label5.Visible = true;
            label6.Visible = true;
            label7.Visible = true;
            label8.Visible = true;
            timer1.Enabled = true;
            timer2.Enabled = true;
        }

        private void Exist_Run(string FileName)
        {
            if (File.Exists(FileName))  //判断有无某文件
            {
                Process.Start(FileName);
            }
            else
            {
                MessageBox.Show("没有找到" + FileName, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //移动窗口
        private void MoveFrom()
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        private void SaveSettings()
        {
            string ExtendName = "";
            if (radioButton1.Checked == true) { ExtendName = "MP4"; }
            if (radioButton2.Checked == true) { ExtendName = "MKV"; }
            if (radioButton3.Checked == true) { ExtendName = "TS"; }
            if (radioButton4.Checked == true) { ExtendName = "FLV"; }


            XmlTextWriter xml = new XmlTextWriter(@System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + "\\M3u8_Downloader_Settings.xml", Encoding.UTF8);
            xml.Formatting = Formatting.Indented;
            xml.WriteStartDocument();
            xml.WriteStartElement("Settings");

            xml.WriteStartElement("DownPath"); xml.WriteCData(textBox_DownloadPath.Text); xml.WriteEndElement();
            xml.WriteStartElement("ExtendName"); xml.WriteCData(ExtendName); xml.WriteEndElement();

            xml.WriteEndElement();
            xml.WriteEndDocument();
            xml.Flush();
            xml.Close();

        }

        private void Download(DownloadFile downloadFile)
        {
            // 启动进程执行相应命令,此例中以执行ffmpeg.exe为例  
            RealAction(@"Tools\ffmpeg.exe", downloadFile.CommandText);
        }

        //public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    var tcs = new TaskCompletionSource<object>();
        //    process.EnableRaisingEvents = true;
        //    process.Exited += (sender, args) => tcs.SetResult(null);
        //    if (cancellationToken != default(CancellationToken))
        //        cancellationToken.Register(tcs.SetCanceled);
        //    return tcs.Task;
        //}


        private void RealAction(string StartFileName, string StartFileArg)
        {
            this.CurDownloaded = false;

            Process CmdProcess = new Process();
            CmdProcess.StartInfo.FileName = StartFileName;      // 命令  
            CmdProcess.StartInfo.Arguments = StartFileArg;      // 参数  

            CmdProcess.StartInfo.CreateNoWindow = true;         // 不创建新窗口  
            CmdProcess.StartInfo.UseShellExecute = false;
            CmdProcess.StartInfo.RedirectStandardInput = true;  // 重定向输入  
            CmdProcess.StartInfo.RedirectStandardOutput = true; // 重定向标准输出  
            CmdProcess.StartInfo.RedirectStandardError = true;  // 重定向错误输出  
            //CmdProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;  

            CmdProcess.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            CmdProcess.ErrorDataReceived += new DataReceivedEventHandler(p_ErrorDataReceived);

            CmdProcess.EnableRaisingEvents = true;                      // 启用Exited事件  
            CmdProcess.Exited += new EventHandler(CmdProcess_Exited);   // 注册进程结束事件  
            
            CmdProcess.Start();

            //获取ffmpeg.exe的进程ID
            ffmpegid = CmdProcess.Id;
            CmdProcess.BeginOutputReadLine();
            CmdProcess.BeginErrorReadLine();

            //修改C#代码，将'CmdProcess.StartInfo.RedirectStandardOutput = false;'，这样所有的输出会在命令行屏幕上直接输出，不会重定向到标准输出流中。
            //'CmdProcess.BeginOutputReadLine();' 或 'CmdProcess.StandardOutput.ReadToEnd();'，通过读取输出流，以便释放相应的缓冲。
            //如果打开注释，则以同步方式执行命令，此例子中用Exited事件异步执行。  
            //CmdProcess.WaitForExit();       

        }

        public void Stop()
        {
            AttachConsole(ffmpegid);
            SetConsoleCtrlHandler(IntPtr.Zero, true);
            GenerateConsoleCtrlEvent(0, 0);
            FreeConsole();
        }

        //以下为实现异步输出CMD信息

        private void Init()
        {
            //3.将相应函数注册到委托事件中  
            ReadStdOutput += new DelReadStdOutput(ReadStdOutputAction);
            ReadErrOutput += new DelReadErrOutput(ReadErrOutputAction);
        }

        private void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                // 4. 异步调用，需要invoke  
                this.Invoke(ReadStdOutput, new object[] { e.Data });
            }
        }

        private void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if (e.Data.IndexOf("Forbidden") >= 0 || e.Data.IndexOf("Server 404") >= 0 || e.Data.IndexOf("403 Forbidden") >= 0)
                {
                    this.CurDownloaded = true;
                    //_waitDownloadedHandle.Set();
                }
                this.Invoke(ReadErrOutput, new object[] { e.Data });
            }
        }

        private void ReadStdOutputAction(string result)
        {
            InvokeTextBox(textBox_forRegex, result);
            InvokeTextBox_AppendText(textBox_Info, result + "\r\n");
        }

        private void ReadErrOutputAction(string result)
        {
            InvokeTextBox(textBox_forRegex,result);
            InvokeTextBox_AppendText(textBox_Info, result + "\r\n");
        }

        private void InvokeTextBox(TextBox textBox, string result)
        {
            if (textBox.InvokeRequired)
            {
                this.Invoke((Action)(() =>
                        InvokeTextBox(textBox,result)
                    ));
                return;
            }
            textBox.Text = result;
        }

        private void InvokeTextBox_AppendText(TextBox textBox, string result)
        {
            if (textBox.InvokeRequired)
            {
                this.Invoke((Action)(() =>
                        InvokeTextBox_AppendText(textBox,result)
                    ));
                return;
            }
            textBox.AppendText(result);
        }

        /// <summary>
        /// 进度条状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmdProcess_Exited(object sender, EventArgs e)
        {
            //var process = (Process)sender;
            FlashWindow(this.Handle, true);
            //设置任务栏进度条状态
            windowsTaskbar.SetProgressState(TaskbarProgressBarState.NoProgress, this.Handle);
            this.Text = "M3U8 Downloader";
            this.label_Progress.Text = "Completed：" + "100.00%";
            ProgressBar.Value = 100;
            timer1.Enabled = false;
            timer2.Enabled = false;
            label8.Text = "";
            textBox_Adress.Text = "";
            textBox_Name.Text = "";
            this.CurDownloaded = true;
            // 执行结束后触发
            try
            {
                //Stop();
                var ffmpeg = Process.GetProcessById(ffmpegid);
                //如果进程还存在就强制结束它
                if (ffmpeg != null)
                {
                    ffmpeg.Close();
                    ffmpeg.Dispose();
                    //ffmpeg.Kill();
                }
            }
            catch (Exception ex)
            {
                CommonHelper.WriteLog(ex.Message, "Killffmpeg");
            }
        }
    }

    public static class ExitedExtension
    {
        public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.SetResult(null);
            //process.Exited += new EventHandler(CmdProcess_Exited);
            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(tcs.SetCanceled);
            return tcs.Task;
        }
    }
    public class DownloadFile
    {
        public string FileFormat { get; set; }
        public string FileName { get; set; }
        public string FileAddress { get; set; }
        public string DownLoadPath { get; set; }
        public string CommandText { get; set; }
    }

    public class ApplicationOutput
    {
        public int ffmpegid { get; set; }
        public bool IsCompleted { get; set; }
        public string OutPut { get; set; }
        public uint ReturnValue { get; set; }

    }

    public class DownLoadQueue
    {
        public bool IsQueueComplete { get; set; }

        private Queue<DownloadFile> queue = new Queue<DownloadFile>();

        /// <summary>
        /// 添加到队列
        /// </summary>
        /// <param name="file"></param>
        public void Add(DownloadFile file)
        {
            queue.Enqueue(file);
        }

        /// <summary>
        /// 获取或删除一个队列元素
        /// </summary>
        /// <returns></returns>
        public DownloadFile Get()
        {
            if (queue.Count != 0)
                return queue.Dequeue();
            else
                return null;
        }
        /// <summary>
        /// 是否存在于队列中
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool IsGet(DownloadFile file)
        {
            bool resule = false;
            resule = queue.Contains(file);
            return resule;
        }
        /// <summary>
        /// Url是否存在于队列中
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool IsGetFileId(DownloadFile file)
        {
            bool resule = false;
            foreach (var obj in this.queue)
            {
                if (obj.FileName == file.FileName)
                {
                    resule = true;
                    break;
                }
            }
            return resule;
        }
        /// <summary>
        /// 队列是否有元素
        /// </summary>
        /// <returns></returns>
        public bool IsHaveElement()
        {
            if (queue.Count <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// 获取队列数
        /// </summary>
        /// <returns></returns>
        public int GetQueueCount()
        {
            return queue.Count;
        }
        /// <summary>
        /// 获取队列列表
        /// </summary>
        /// <returns></returns>
        public List<DownloadFile> GetQueues()
        {
            var qlist = new List<DownloadFile>();
            foreach (var obj in this.queue)
            {
                qlist.Add(obj);
            }
            return qlist;
        }
    }
}