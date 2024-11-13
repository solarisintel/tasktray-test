using System;
using System.Diagnostics; // ProcessStartInfoで必要
using System.Drawing;
using System.Windows.Forms;

using System.IO;

// windowsイベント通知
using Microsoft.Win32;
using System.Net.Http;

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
    static String serverResponseJson;

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
        //ProcessStartInfo processStartInfo = new ProcessStartInfo("C:\\Program Files (x86)\\Internet Explorer\\iexplore.exe", "http://yahoo.co.jp");
        //Process process = Process.Start(processStartInfo);

        var jsonString = "{ \"age\" : 20, \"name\" : \"太郎\"  }";
        Post("https://pochi.mydns.jp/json", jsonString);

    }

    private static async void Post(string url, string request)
    {
        using (var client = new HttpClient())
        {
            try
            {
                // タイムアウト時間の設定(3秒)
                client.Timeout = TimeSpan.FromMilliseconds(3000);

                //POSTリクエスト(JSON)
                var content = new StringContent(request, System.Text.Encoding.UTF8, "application/json");
                var res = await client.PostAsync(url, content);
                //取得
                serverResponseJson = await res.Content.ReadAsStringAsync();
                Console.WriteLine(serverResponseJson);
            }
            catch (Exception e)
            {
                serverResponseJson = "error";
                Console.WriteLine(e.Message);
                MessageBox.Show("サーバから応答がありません");

            }
        }
    }

    private void Close_Click()
    {
        File.AppendAllText(Environment.GetEnvironmentVariable("USERPROFILE") + "\\Documents" + "\\test.log", "pg exit\n");
        //イベントを解放する
        //フォームDisposeメソッド内の基本クラスのDisposeメソッド呼び出しの前に
        //記述してもよい
        SystemEvents.SessionEnding -=
            new SessionEndingEventHandler(SystemEvents_SessionEnding);
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
    }

    //ログオフ、シャットダウンしようとしているとき
    private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
    {
        if (e.Reason == SessionEndReasons.Logoff)
        {
            File.AppendAllText(Environment.GetEnvironmentVariable("USERPROFILE") + "\\Documents" + "\\test.log", "logoff\n");
        }
        else if (e.Reason == SessionEndReasons.SystemShutdown)
        {
            File.AppendAllText(Environment.GetEnvironmentVariable("USERPROFILE") + "\\Documents" + "\\test.log", "shutdown\n");
        }
    }
}
