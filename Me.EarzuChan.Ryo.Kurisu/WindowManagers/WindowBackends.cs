using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.Misc;
using Me.EarzuChan.Ryo.Kurisu.Utils;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Me.EarzuChan.Ryo.Kurisu.WindowManagers
{
    public enum KurisuAppWindowState
    {
        Normal,
        Maximized,
        Minimized
    }

    internal class KurisuAppWpfWindowManager : IKurisuAppWindowManager
    {

        private KurisuApp App;
        private readonly WebView2 WebView = new();
        private readonly Application WpfApp = new();
        private readonly Window WpfWindow = new();

        internal KurisuAppWpfWindowManager() { }

        private void OnStateChanged(object? _, EventArgs __)
        {
            App.TriggerAppEvent(new(AppEventType.AppWindowStateChanged, WpfWindow.WindowState == WindowState.Maximized ? KurisuAppWindowState.Maximized : WpfWindow.WindowState == WindowState.Minimized ? KurisuAppWindowState.Minimized : KurisuAppWindowState.Normal));
        }

        private async void InitWebView(object _, RoutedEventArgs __)
        {
            // 当窗口加载好

            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, null, new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = "--enable-features=msWebView2EnableDraggableRegions" });

            // 加载核心 
            await WebView.EnsureCoreWebView2Async(webView2Environment);
        }

        private void InitWebApp(object? _, CoreWebView2InitializationCompletedEventArgs __)
        {
            // 当浏览器加载好

            // 资源拨弄成虚拟主机
            WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(App.Profile.VirtualHostNameName, App.Profile.WebResourcePath, CoreWebView2HostResourceAccessKind.Deny);

            // TODO:FIX BABE
            WebView.CoreWebView2.Navigate(App.Profile.DebugMode && App.Profile.DebugStartUpWithDebugUrl ? App.Profile.DebugStartUpUrl : App.Profile.StartUpUrl);

            // 提供对象 互操作
            WebView.CoreWebView2.AddHostObjectToScript("webApis", new KurisuAppApiBridge(App));
            // Trace.WriteLine(MassServer.GetMasses());
            // 其实在Js侧写个包也可以做到"Request"，还需要提供专门的Request接口吗

            // 回调接受消息
            WebView.CoreWebView2.WebMessageReceived += (_, e) => App.HandleWebEvent(DataModelParsingUtils.ParseWebLetterJson(e.WebMessageAsJson));

            // 打开控制台
            if (App.Profile.DebugMode && App.Profile.DebugAutomaticOpenDevTool) WebView.CoreWebView2.OpenDevToolsWindow();
        }

        public void SetWindowState(KurisuAppWindowState state) => WpfWindow.WindowState = state == KurisuAppWindowState.Maximized ? WindowState.Maximized : state == KurisuAppWindowState.Normal ? WindowState.Normal : WindowState.Minimized;

        public void Init(KurisuApp app)
        {
            App = app;

            WpfWindow.Title = App.Profile.Name;
            WpfWindow.Width = App.Profile.WindowWidth;
            WpfWindow.Height = App.Profile.WindowHeight;
            if (App.Profile.UseIcon) WpfWindow.Icon = new BitmapImage(new Uri(App.Profile.Icon, UriKind.RelativeOrAbsolute));

            WpfWindow.Content = WebView;

            if (App.Profile.WindowBorderless) WindowChrome.SetWindowChrome(WpfWindow, new WindowChrome()
            {
                ResizeBorderThickness = new Thickness(8),
                CaptionHeight = 0,
            });

            WpfWindow.StateChanged += OnStateChanged;
            WpfWindow.Loaded += InitWebView;
            WebView.CoreWebView2InitializationCompleted += InitWebApp;
        }

        public void EmitWebEvent(WebLetter model) => WebView.CoreWebView2.PostWebMessageAsJson(DataSerializationUtils.ToJson(model));

        public void Close()
        {
            WpfApp.Shutdown();
        }

        public void Show()
        {
            WpfApp.Run(WpfWindow);
        }

        public KurisuAppWindowState GetWindowState() => WpfWindow.WindowState == WindowState.Maximized ? KurisuAppWindowState.Maximized : WpfWindow.WindowState == WindowState.Normal ? KurisuAppWindowState.Normal : KurisuAppWindowState.Minimized;
    }

    public interface IKurisuAppWindowManager
    {
        public void SetWindowState(KurisuAppWindowState state);

        public void Close();

        public void Show();

        public void Init(KurisuApp app);

        public void EmitWebEvent(WebLetter model);
        public KurisuAppWindowState GetWindowState();
    }
}
