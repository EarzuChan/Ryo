using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Utils;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Me.EarzuChan.Ryo.DesktopFrontEnd.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WPFWindow : Window
    {
        public WPFWindow()
        {
            InitializeComponent();

            InitWebView();
        }

        private async void InitWebView()
        {
            webView21.CoreWebView2InitializationCompleted += OnWebViewInited;

            await webView21.EnsureCoreWebView2Async(); // 加载
        }

        private void OnWebViewInited(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            // 注入对象 互操作
            webView21.CoreWebView2.AddHostObjectToScript("MassServer", new MassServer());

            // 每当页面加载好
            webView21.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("var MassServer = window.chrome.webview.hostObjects.MassServer");

            // 打开开发者工具
            webView21.CoreWebView2.OpenDevToolsWindow();
        }
    }

#pragma warning disable CS0618 // 类型或成员已过时
    [ClassInterface(ClassInterfaceType.AutoDual)]
#pragma warning restore CS0618 // 类型或成员已过时
    [ComVisible(true)]
    public class MassServer
    {
        public MassManager MassManager = new();

        public MassServer() => LogUtils.PrintInfo("MassServer Inited");

        public string DoSth(string text)
        {
            var a = $"Hello, {text}";

            Trace.WriteLine(a);

            return a;
        }

        public string[] GetMasses() =>MassManager.
    }
}
