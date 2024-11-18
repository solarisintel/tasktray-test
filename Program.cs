using System;
using System.Diagnostics; // ProcessStartInfoで必要
using System.Drawing;
using System.Windows.Forms;

using System.IO;

// windowsイベント通知
using Microsoft.Win32;

// http post
using System.Net.Http;

using System.Timers;
using System.Collections.Generic;
using System.Text;


// 参考
// https://qiita.com/tukiyo3/items/b11c796f1679cd605209



class MainWindow
{
    static void Main()
    {
        Launcher launcher = new Launcher();
        Application.Run();
    }
}

class Launcher : Form
{

    static string machineName;
    static string userName;
    static string logFileName;
    static string logPath;
    string logFilePath;
    static int transferStep = 0; // 0: Send ComputerName+SessionUser, 1: send app log

    static string serverResponseString1;
    static string postURL1 = "https://pochi.mydns.jp/post1/index.php";

    static string serverResponseString2;
    static string postURL2 = "https://pochi.mydns.jp/post2/index.php";
    static string readingLogFilePath;

    FileSystemWatcher watcher;

    System.Timers.Timer appTimer;

    public Launcher()
    {
        this.ShowInTaskbar = false;
        this.setComponents();
    }

    private void cmdHello()
    {
        MessageBox.Show("こんにちは。");
    }

    private void cmdIe()
    {
        ProcessStartInfo processStartInfo = new ProcessStartInfo("C:\\Program Files (x86)\\Internet Explorer\\iexplore.exe", "http://yahoo.co.jp");
        Process process = Process.Start(processStartInfo);
    }

    private void Close_Click()
    {
        File.AppendAllText(logFilePath, GetNowTime() + "exit" + "\n");

        //イベントを解放する
        //フォームDisposeメソッド内の基本クラスのDisposeメソッド呼び出しの前に
        //記述してもよい
        SystemEvents.SessionEnding -=
            new SessionEndingEventHandler(SystemEvents_SessionEnding);

        // タイマーの破棄
        appTimer.Stop();
        appTimer.Dispose();

        Application.Exit();

    }

    private void setComponents()
    {
        NotifyIcon icon = new NotifyIcon();
        icon.Icon = new Icon("app.ico");
        icon.Visible = true;
        icon.Text = "常駐アプリテスト";
        ContextMenuStrip menu = new ContextMenuStrip();
        ToolStripMenuItem menuItem = new ToolStripMenuItem();

        menu.Items.AddRange(new ToolStripMenuItem[]{
            new ToolStripMenuItem("解除の申請送信する", null, (s,e)=>{cmdHello();}),
            new ToolStripMenuItem("解除済の確認する", null, (s,e)=>{cmdIe();}),
            new ToolStripMenuItem("終了", null, (s,e)=>{Close_Click();})
        });

        icon.ContextMenuStrip = menu;

        //イベントをイベントハンドラに関連付ける
        //フォームコンストラクタなどの適当な位置に記述してもよい
        SystemEvents.SessionEnding += new SessionEndingEventHandler(SystemEvents_SessionEnding);

        // Get Session Info 
        machineName = Environment.MachineName;
        userName = Environment.UserName;

        // log file name (Machine-Date.log)
        DateTime dt = DateTime.Now;
        //logFileName = machineName + "-" + dt.ToString("yyyyMMdd") + ".log";
        logFileName = dt.ToString("yyyyMMdd") + ".log";

        logPath = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Documents\\DownloadEraser";
        if (!Directory.Exists(logPath))
        {
            Directory.CreateDirectory(logPath);
        }

        logFilePath = logPath + "\\" + logFileName;

        // timer Setting 
        appTimer = new System.Timers.Timer();
        //繰り返しよばれたければtrue、1回だけで良ければfalse
        appTimer.AutoReset = true;
        // タイマ満了までの時間(10秒)
        appTimer.Interval = 10000;
        // 満了時の処理
        appTimer.Elapsed += OnTimerElapsed;
        // タイマスタート
        appTimer.Start();


        watcher = new FileSystemWatcher(Environment.GetEnvironmentVariable("USERPROFILE") + "\\Downloads");
        // 監視パラメータの設定
        watcher.NotifyFilter = (NotifyFilters.LastWrite
            | NotifyFilters.FileName
            | NotifyFilters.DirectoryName
            | NotifyFilters.Attributes
            | NotifyFilters.CreationTime
            | NotifyFilters.Size
            | NotifyFilters.LastAccess
            | NotifyFilters.Security);

        // サブディレクトリは監視しない
        watcher.IncludeSubdirectories = false;
        // イベントハンドラの設定
        watcher.Created += new FileSystemEventHandler(watcher_Created);

        File.AppendAllText(logFilePath, GetNowTime() + "start\n");


    }

    private void watcher_Created(object sender, FileSystemEventArgs e)
    {
        string fileName;
        fileName = e.Name;
        File.AppendAllText(logFilePath, GetNowTime() + "created " + fileName + "\n");
    }

    // ログファイル用
    private string GetNowTime()
    {
        DateTime dt = DateTime.Now;
        return dt.ToString("yyyy/MM/dd HH:mm:ss ");
    }

    private static async void Post(string url, Dictionary<string, string> parameters)
    {
        // 受信は非同期と考えるべきでそのためserverResponseは必要に応じて変数を複数確保すべき
        using (var client = new HttpClient())
        {
            try
            {
                // タイムアウト時間の設定(3秒)
                client.Timeout = TimeSpan.FromMilliseconds(3000);
                var content = new FormUrlEncodedContent(parameters);
                var res = await client.PostAsync(url, content);
                // 取得
                if (transferStep == 0) { 
                    serverResponseString1 = await res.Content.ReadAsStringAsync();
                    Console.WriteLine("Reponse String=" +  serverResponseString1);
                    if (serverResponseString1.Substring(0, 2) == "OK")
                    {
                        transferStep = 1;
                    }
                } else if (transferStep == 1) {
                    serverResponseString2 = await res.Content.ReadAsStringAsync();
                    Console.WriteLine("Reponse String=" + serverResponseString1);
                    if (serverResponseString2.Substring(0, 2) == "OK")
                    {
                        // 送信済はファイル削除
                        File.Delete(readingLogFilePath);
                        transferStep = 1;
                    }
                }

            }
            catch (Exception e)
            {
                if (transferStep == 0)
                {
                    serverResponseString1 = "error";
                }
                Console.WriteLine(e.Message);
                MessageBox.Show("サーバから応答がありません");

            }
        }
    }




    //ログオフ、シャットダウンをログに出力する
    private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
    {
        if (e.Reason == SessionEndReasons.Logoff)
        {
            File.AppendAllText(logFilePath, GetNowTime() + "logoff\n");
        }
        else if (e.Reason == SessionEndReasons.SystemShutdown)
        {
            File.AppendAllText(logFilePath, GetNowTime() + "shutdown\n");
        }
    }

    private static void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        Console.WriteLine($"{DateTime.Now} OnTimer");

        // 次に送るデータの切り替えはpost後のreponseで判断する
        if (transferStep == 0)
        {
            var parameters = new Dictionary<string, string>()
            {
                { "pc", machineName },
                { "user", userName },
            };
            Console.WriteLine($"{DateTime.Now} Post start");
            Post(postURL1, parameters);
            Console.WriteLine($"{DateTime.Now} posted");
        }
        if (transferStep == 1)
        {
            // カレントフォルダ内のファイル名だけ表示
            var files = Directory.EnumerateFiles(logPath, "*.log", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                // 今作成中のログファイルでなければすべて読み込む
                if (fileName != logFileName) {
                    readingLogFilePath = file;
                    var logText = File.ReadAllText(readingLogFilePath, Encoding.GetEncoding("shift-jis"));
                    Console.WriteLine($"{DateTime.Now} log" + logText);
                    var parameters = new Dictionary<string, string>()
                    {
                        { "pc", machineName },
                        { "log", logText },
                    };
                    Post(postURL2, parameters);
                    break;
                }
            }

        }

    }
}
