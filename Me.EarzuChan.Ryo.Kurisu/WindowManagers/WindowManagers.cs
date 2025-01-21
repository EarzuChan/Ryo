using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.Misc;
using Me.EarzuChan.Ryo.Kurisu.Utils;
using Me.EarzuChan.Ryo.Utils;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Me.EarzuChan.Ryo.Kurisu.WindowManagers;

public enum KurisuWindowState
{
    Normal,
    Maximized,
    Minimized
}

internal class WpfKurisuWindowManager : IKurisuWindowManager
{
    private KurisuApp? App;

    private readonly WebView2 WebView = new();
    private readonly Application WpfApp = new();
    private readonly Window WpfWindow = new();

    internal WpfKurisuWindowManager()
    {
    }

    private void OnStateChanged(object? _, EventArgs __) =>
        App?.TriggerAppEvent(new(AppEventType.AppWindowStateChanged,
            ToKurisuWindowState(WpfWindow.WindowState)));

    private async void InitWebView(object _, RoutedEventArgs __)
    {
        // 当窗口加载好

        var webView2Environment = await CoreWebView2Environment.CreateAsync(null, null,
            new CoreWebView2EnvironmentOptions
                { AdditionalBrowserArguments = "--enable-features=msWebView2EnableDraggableRegions" });

        // 加载核心 
        await WebView.EnsureCoreWebView2Async(webView2Environment);
    }

    private void InitWebApp(object? _, CoreWebView2InitializationCompletedEventArgs __) => App.Ensured(app =>
    {
        // 当浏览器加载好

        // 资源拨弄成虚拟主机
        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(app.Profile.VirtualHostName,
            app.Profile.WebResourcePath, CoreWebView2HostResourceAccessKind.Deny);

        // TODO:FIX BABE
        // WebView.CoreWebView2.Navigate();

        // 提供对象 互操作
        WebView.CoreWebView2.AddHostObjectToScript("webApis", new KurisuAppExposedApiBridge(app));
        // Trace.WriteLine(MassServer.GetMasses());
        // 其实在Js侧写个包也可以做到"Request"，还需要提供专门的Request接口吗

        // 回调接受消息
        WebView.CoreWebView2.WebMessageReceived += (_, e) =>
            app.HandleWebEvent(DataModelParsingUtils.ParseWebLetterJson(e.WebMessageAsJson));

        // 打开控制台
        if (app.Profile is { DebugMode: true, DebugAutomaticOpenDevTool: true })
            WebView.CoreWebView2.OpenDevToolsWindow();
    });

    public void SetWindowState(KurisuWindowState state) => WpfWindow.WindowState =
        state switch
        {
            KurisuWindowState.Maximized => WindowState.Maximized,
            KurisuWindowState.Normal => WindowState.Normal,
            _ => WindowState.Minimized
        };

    public void Init(KurisuApp app)
    {
        App = app;

        WpfWindow.Title = app.Profile.Name;
        WpfWindow.Width = app.Profile.WindowWidth;
        WpfWindow.Height = app.Profile.WindowHeight;
        if (app.Profile.UseIcon)
            WpfWindow.Icon = new BitmapImage(new Uri(app.Profile.Icon, UriKind.RelativeOrAbsolute));

        WpfWindow.Content = WebView.Also(it =>
            it.Source = new Uri(app.Profile is { DebugMode: true, DebugStartUpWithDebugUrl: true }
                ? app.Profile.DebugStartUpUrl
                : app.Profile.StartUpUrl));

        if (app.Profile.WindowBorderless)
            WindowChrome.SetWindowChrome(WpfWindow, new WindowChrome()
            {
                ResizeBorderThickness = new Thickness(8),
                CaptionHeight = 0,
            });

        WpfWindow.StateChanged += OnStateChanged;
        WpfWindow.Loaded += InitWebView;
        WebView.CoreWebView2InitializationCompleted += InitWebApp;
    }

    public void EmitWebEvent(OldWebLetter model) =>
        WebView.CoreWebView2?.PostWebMessageAsJson(model.ToJson());

    public void Close() => WpfApp.Shutdown();

    public void Show() => WpfApp.Run(WpfWindow);

    public KurisuWindowState GetWindowState() => ToKurisuWindowState(WpfWindow.WindowState);

    private static KurisuWindowState ToKurisuWindowState(WindowState state) =>
        state switch
        {
            WindowState.Maximized => KurisuWindowState.Maximized,
            WindowState.Minimized => KurisuWindowState.Minimized,
            _ => KurisuWindowState.Normal
        };
}

public interface IKurisuWindowManager
{
    public void SetWindowState(KurisuWindowState state);

    public void Close();

    public void Show();

    public void Init(KurisuApp app);

    public void EmitWebEvent(OldWebLetter model);
    public KurisuWindowState GetWindowState();
}