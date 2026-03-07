using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.Misc;
using Me.EarzuChan.Ryo.Kurisu.Utils;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Utils;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using DrawingColor = System.Drawing.Color;

namespace Me.EarzuChan.Ryo.Kurisu.WindowManagers;

internal sealed class Win32KurisuWindowManager : IKurisuWindowManager {
    private KurisuApp? App;
    private bool _webAppReadyRaised;
    private KurisuWindowState? _lastReportedWindowState;

    private WebView2? WebView;
    private KurisuHostWindow? Window;
    private Uri? StartUpUri;

    private static bool _dpiConfigured;

    public event Action? AppReady;
    public event Action? AppClosed;

    public void Init(KurisuApp app) {
        EnsureProcessDpiConfigured();

        App = app;
        StartUpUri = new Uri(app.Profile is { IsDebug: true, DebugStartUpWithDebugUrl: true } ? app.Profile.DebugStartUpUrl : app.Profile.StartUpUrl);

        Window = new KurisuHostWindow();
        WebView = new WebView2 { Dock = DockStyle.Fill };

        Window.Text = app.Profile.Name;
        Window.Width = app.Profile.WindowWidth;
        Window.Height = app.Profile.WindowHeight;
        Window.MinimizeBox = true;
        Window.MaximizeBox = true;

        if (app.Profile.UseIcon) {
            try {
                Window.Icon = new Icon(app.Profile.Icon);
            } catch (Exception e) {
                Trace.WriteLine(TextUtils.MakeErrorMsgText($"加载窗口图标失败：{app.Profile.Icon}", e, true));
            }
        }

        ConfigureWindowFrame(app.Profile.WindowBorderless);
        Window.AttachWebView(WebView);

        SetWindowState(app.Profile.StartUpWindowState);
        _lastReportedWindowState = ToKurisuWindowState(Window.WindowState);

        Window.Load += OnWindowLoaded;
        Window.FormClosed += OnWindowClosed;
        Window.Resize += OnWindowResized;
        Window.ResizeBegin += (_, _) => Window.MarkUserSizing(true);
        Window.ResizeEnd += (_, _) => Window.MarkUserSizing(false);
    }

    public void Show() {
        if (Window == null) throw new InvalidOperationException("Window 未初始化");
        Application.Run(Window);
    }

    public void Close() {
        if (Window == null) return;
        if (!Window.IsDisposed) Window.Close();
    }

    public void SetWindowState(KurisuWindowState state) {
        if (Window == null) return;

        Window.WindowState = state switch {
            KurisuWindowState.Maximized => FormWindowState.Maximized,
            KurisuWindowState.Minimized => FormWindowState.Minimized,
            _ => FormWindowState.Normal
        };
    }

    public KurisuWindowState GetWindowState() => Window == null
        ? KurisuWindowState.Normal
        : ToKurisuWindowState(Window.WindowState);

    public KurisuHostCapabilities GetHostCapabilities() => new(true);

    public void EmitWebEvent(WebLetter model) {
        var message = new KurisuBridgeMessage {
            Kind = KurisuBridgeMessageKinds.WebEvent,
            Letter = model
        };

        WebView?.CoreWebView2?.PostWebMessageAsJson(message.ToJson());
    }

    private void OnWindowResized(object? _, EventArgs __) {
        if (Window == null) return;

        var currentState = ToKurisuWindowState(Window.WindowState);
        if (_lastReportedWindowState == currentState) return;

        _lastReportedWindowState = currentState;
        Trace.WriteLine($"窗口状态变更：{Window.WindowState}");
        App?.TriggerAppEvent(new AppEvent(AppEventType.AppWindowStateChanged, currentState));
    }

    private void OnWindowLoaded(object? _, EventArgs __) {
        var initTask = InitWebViewAsync();
        initTask.ContinueWith(task => {
            Trace.WriteLine(TextUtils.MakeErrorMsgText("初始化WebView2失败", task.Exception!, true));
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private void OnWindowClosed(object? _, FormClosedEventArgs __) => AppClosed?.Invoke();

    private async Task InitWebViewAsync() {
        if (WebView == null) throw new InvalidOperationException("WebView 未初始化");

        var webView2Environment = await CoreWebView2Environment.CreateAsync(null, null,
            new CoreWebView2EnvironmentOptions {
                AdditionalBrowserArguments = "--enable-features=msWebView2EnableDraggableRegions"
            });

        await WebView.EnsureCoreWebView2Async(webView2Environment);
        InitWebApp();

        if (StartUpUri == null) throw new InvalidOperationException("StartUpUri 未初始化");
        WebView.Source = StartUpUri;
    }

    private void RaiseAppReadyOnce() {
        if (_webAppReadyRaised) return;
        _webAppReadyRaised = true;
        AppReady?.Invoke();
    }

    private void InitWebApp() => App.EnsureNotNull(app => {
        if (WebView == null || WebView.CoreWebView2 == null) throw new InvalidOperationException("WebView2 Core 未初始化");
        var webResourcePath = PathResolutionUtils.ResolveWebResourcePath(app.Profile.WebResourcePath);

        WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            app.Profile.VirtualHostName,
            webResourcePath,
            CoreWebView2HostResourceAccessKind.Deny);

        WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        WebView.CoreWebView2.NavigationCompleted += (_, _) => RaiseAppReadyOnce();

        if (app.Profile is { IsDebug: true, DebugAutomaticOpenDevTool: true }) {
            WebView.CoreWebView2.OpenDevToolsWindow();
        }
    });

    private void OnWebMessageReceived(object? _, CoreWebView2WebMessageReceivedEventArgs e) {
        if (App == null) return;

        KurisuBridgeMessage message;
        try {
            message = DataModelParsingUtils.ParseBridgeMessageJson(e.WebMessageAsJson);
        } catch (Exception ex) {
            Trace.WriteLine(TextUtils.MakeErrorMsgText("解析Bridge消息失败", ex, true));
            return;
        }

        switch (message.Kind) {
            case KurisuBridgeMessageKinds.WebEvent:
                if (message.Letter == null) {
                    Trace.WriteLine("收到无效WebEvent消息：letter为空");
                    return;
                }

                try {
                    App.HandleWebEvent(message.Letter);
                } catch (Exception ex) {
                    Trace.WriteLine(TextUtils.MakeErrorMsgText($"处理WebEvent失败：{message.Letter.Name}", ex, true));
                }

                return;

            case KurisuBridgeMessageKinds.WebCallRequest:
                HandleWebCallRequest(message);
                return;

            default:
                Trace.WriteLine($"未知Bridge消息类型：{message.Kind}");
                if (!string.IsNullOrWhiteSpace(message.RequestId)) SendWebCallResponse(message.RequestId, WebResponse.Failure("bridge_unknown_kind", $"未知消息类型：{message.Kind}"));

                return;
        }
    }

    private void HandleWebCallRequest(KurisuBridgeMessage message) {
        if (App == null) return;

        if (string.IsNullOrWhiteSpace(message.RequestId)) {
            Trace.WriteLine("收到无效WebCallRequest：requestId为空");
            return;
        }

        if (message.Letter == null) {
            SendWebCallResponse(message.RequestId, WebResponse.Failure("bridge_invalid_request", "WebCallRequest 的 letter 为空"));
            return;
        }

        WebResponse response;
        try {
            response = App.RespondWebCall(message.Letter);
        } catch (Exception ex) {
            Trace.WriteLine(TextUtils.MakeErrorMsgText($"处理WebCall失败：{message.Letter.Name}", ex, true));
            response = WebResponse.Failure("bridge_web_call_exception", ex.Message);
        }

        SendWebCallResponse(message.RequestId, response);
    }

    private void SendWebCallResponse(string requestId, WebResponse response) {
        var message = new KurisuBridgeMessage {
            Kind = KurisuBridgeMessageKinds.WebCallResponse,
            RequestId = requestId,
            Response = response
        };

        WebView?.CoreWebView2?.PostWebMessageAsJson(message.ToJson());
    }

    private void ConfigureWindowFrame(bool borderless) {
        if (Window == null || WebView == null) return;

        Window.Borderless = borderless;

        if (borderless) {
            Window.FormBorderStyle = FormBorderStyle.None;
            Window.BackColor = DrawingColor.Black;
            Window.Padding = Padding.Empty;
            Window.SetResizeOverlayEnabled(true);
            Window.SetBackdropTransparencyEnabled(true);

            // 使用透明背景，使网页可直接自绘圆角/阴影等窗口外观。
            WebView.DefaultBackgroundColor = DrawingColor.Transparent;
        } else {
            Window.FormBorderStyle = FormBorderStyle.Sizable;
            Window.SetBackdropTransparencyEnabled(false);
            Window.BackColor = SystemColors.Control;
            Window.SetResizeOverlayEnabled(false);
            WebView.DefaultBackgroundColor = DrawingColor.White;
        }

        Window.RefreshWindowRegion();
        Window.SyncResizeHandles();
    }

    private static KurisuWindowState ToKurisuWindowState(FormWindowState state) => state switch {
        FormWindowState.Maximized => KurisuWindowState.Maximized,
        FormWindowState.Minimized => KurisuWindowState.Minimized,
        _ => KurisuWindowState.Normal
    };

    private static void EnsureProcessDpiConfigured() {
        if (_dpiConfigured) return;

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        _dpiConfigured = true;
    }
}

internal sealed class KurisuHostWindow : Form {
    private const int WmNcHitTest = 0x0084;
    private const int HtClient = 1;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmEnterSizeMove = 0x0231;
    private const int WmExitSizeMove = 0x0232;

    private const int ResizeBorderThicknessBase = 8;
    private const int MinResizeBorderThickness = 6;
    private const int MaxResizeBorderThickness = 14;
    private const int RoundedCornerRadius = 16;

    private bool _isUserSizing;
    private bool _resizeOverlayEnabled;
    private bool _backdropTransparencyEnabled;
    private Control? _webViewControl;
    private readonly IReadOnlyDictionary<int, ResizeHandleOverlay> _resizeHandles;

    public KurisuHostWindow() {
        _resizeHandles = CreateResizeHandles();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Borderless { get; set; }

    public void MarkUserSizing(bool sizing) => _isUserSizing = sizing;

    public void AttachWebView(Control webViewControl) {
        _webViewControl = webViewControl;
        Controls.Add(webViewControl);

        foreach (var handle in _resizeHandles.Values) {
            if (handle.Parent == null) Controls.Add(handle);
        }

        webViewControl.SendToBack();
        BringResizeHandlesToFront();
        SyncResizeHandles();
    }

    public void SetResizeOverlayEnabled(bool enabled) {
        _resizeOverlayEnabled = enabled;
        SyncResizeHandles();
    }

    public void SetBackdropTransparencyEnabled(bool enabled) {
        _backdropTransparencyEnabled = enabled;
        ApplyBackdropTransparency();
    }

    public void SyncResizeHandles() {
        _webViewControl?.SetBounds(0, 0, ClientSize.Width, ClientSize.Height);

        var shouldShow = _resizeOverlayEnabled && Borderless && WindowState == FormWindowState.Normal;
        foreach (var handle in _resizeHandles.Values) handle.Visible = shouldShow;

        if (!shouldShow) return;

        BringResizeHandlesToFront();

        var width = ClientSize.Width;
        var height = ClientSize.Height;
        var t = GetResizeBorderThickness();

        var innerWidth = Math.Max(0, width - (t * 2));
        var innerHeight = Math.Max(0, height - (t * 2));

        SetHandleBounds(HtTopLeft, 0, 0, t, t);
        SetHandleBounds(HtTop, t, 0, innerWidth, t);
        SetHandleBounds(HtTopRight, Math.Max(0, width - t), 0, t, t);

        SetHandleBounds(HtLeft, 0, t, t, innerHeight);
        SetHandleBounds(HtRight, Math.Max(0, width - t), t, t, innerHeight);

        SetHandleBounds(HtBottomLeft, 0, Math.Max(0, height - t), t, t);
        SetHandleBounds(HtBottom, t, Math.Max(0, height - t), innerWidth, t);
        SetHandleBounds(HtBottomRight, Math.Max(0, width - t), Math.Max(0, height - t), t, t);
    }

    public void RefreshWindowRegion() {
        if (!Borderless || WindowState != FormWindowState.Normal || Width <= 0 || Height <= 0) {
            Region = null;
            return;
        }

        Region?.Dispose();
        using var path = CreateRoundedRectPath(ClientRectangle, RoundedCornerRadius);
        Region = new Region(path);
    }

    protected override void WndProc(ref Message m) {
        if (m.Msg == WmEnterSizeMove) _isUserSizing = true;
        if (m.Msg == WmExitSizeMove) _isUserSizing = false;

        if (Borderless && m.Msg == WmNcHitTest && WindowState == FormWindowState.Normal && !_isUserSizing) {
            var hitTest = HitTestResizeBorder(m.LParam);
            if (hitTest != HtClient) {
                m.Result = (IntPtr)hitTest;
                return;
            }
        }

        base.WndProc(ref m);
    }

    protected override void OnResize(EventArgs e) {
        base.OnResize(e);
        SyncResizeHandles();
        RefreshWindowRegion();
        ApplyBackdropTransparency();
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        ApplyBackdropTransparency();
    }

    private IReadOnlyDictionary<int, ResizeHandleOverlay> CreateResizeHandles() {
        return new Dictionary<int, ResizeHandleOverlay> {
            [HtTopLeft] = CreateResizeHandle(HtTopLeft, Cursors.SizeNWSE),
            [HtTop] = CreateResizeHandle(HtTop, Cursors.SizeNS),
            [HtTopRight] = CreateResizeHandle(HtTopRight, Cursors.SizeNESW),
            [HtLeft] = CreateResizeHandle(HtLeft, Cursors.SizeWE),
            [HtRight] = CreateResizeHandle(HtRight, Cursors.SizeWE),
            [HtBottomLeft] = CreateResizeHandle(HtBottomLeft, Cursors.SizeNESW),
            [HtBottom] = CreateResizeHandle(HtBottom, Cursors.SizeNS),
            [HtBottomRight] = CreateResizeHandle(HtBottomRight, Cursors.SizeNWSE)
        };
    }

    private ResizeHandleOverlay CreateResizeHandle(int hitCode, Cursor cursor) {
        var handle = new ResizeHandleOverlay { Cursor = cursor };

        handle.Tag = hitCode;
        handle.MouseDown += OnResizeHandleMouseDown;
        return handle;
    }

    private void OnResizeHandleMouseDown(object? sender, MouseEventArgs e) {
        if (e.Button != MouseButtons.Left || !Borderless || WindowState != FormWindowState.Normal) return;
        if (sender is not Control { Tag: int hitCode }) return;

        _isUserSizing = true;
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(Handle, WmNcLButtonDown, (IntPtr)hitCode, IntPtr.Zero);
    }

    private void SetHandleBounds(int hitCode, int x, int y, int width, int height) {
        if (_resizeHandles.TryGetValue(hitCode, out var handle)) handle.SetBounds(x, y, width, height);
    }

    private void BringResizeHandlesToFront() {
        foreach (var handle in _resizeHandles.Values) handle.BringToFront();
    }

    private void ApplyBackdropTransparency() {
        if (!IsHandleCreated) return;

        var margins = _backdropTransparencyEnabled && Borderless
            ? new NativeMethods.Margins(-1)
            : new NativeMethods.Margins(0);

        _ = NativeMethods.DwmExtendFrameIntoClientArea(Handle, ref margins);
    }

    private int HitTestResizeBorder(IntPtr lParam) {
        var screenPoint = new Point(GetSignedLowWord(lParam), GetSignedHighWord(lParam));
        var clientPoint = PointToClient(screenPoint);

        var borderThickness = GetResizeBorderThickness();
        var width = ClientSize.Width;
        var height = ClientSize.Height;

        var onLeft = clientPoint.X >= 0 && clientPoint.X < borderThickness;
        var onRight = clientPoint.X <= width && clientPoint.X > width - borderThickness;
        var onTop = clientPoint.Y >= 0 && clientPoint.Y < borderThickness;
        var onBottom = clientPoint.Y <= height && clientPoint.Y > height - borderThickness;

        switch (onTop) {
            case true when onLeft:
                return HtTopLeft;
            case true when onRight:
                return HtTopRight;
        }

        switch (onBottom) {
            case true when onLeft:
                return HtBottomLeft;
            case true when onRight:
                return HtBottomRight;
        }

        if (onLeft) return HtLeft;
        if (onRight) return HtRight;
        if (onTop) return HtTop;

        return onBottom ? HtBottom : HtClient;
    }

    private int GetResizeBorderThickness() {
        var scaled = (int)Math.Round(ResizeBorderThicknessBase * (DeviceDpi / 96.0));
        return Math.Clamp(scaled, MinResizeBorderThickness, MaxResizeBorderThickness);
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle bounds, int radius) {
        var diameter = radius * 2;
        var path = new GraphicsPath();

        path.StartFigure();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    private static int GetSignedLowWord(IntPtr value) => (short)(value.ToInt64() & 0xFFFF);

    private static int GetSignedHighWord(IntPtr value) => (short)((value.ToInt64() >> 16) & 0xFFFF);

    private static class NativeMethods {
        [DllImport("user32.dll")]
        internal static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Margins {
            public int CxLeftWidth;
            public int CxRightWidth;
            public int CyTopHeight;
            public int CyBottomHeight;

            public Margins(int all) {
                CxLeftWidth = all;
                CxRightWidth = all;
                CyTopHeight = all;
                CyBottomHeight = all;
            }
        }

        [DllImport("dwmapi.dll")]
        internal static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);
    }
}

internal sealed class ResizeHandleOverlay : Control {
    private const int WsExTransparent = 0x20;

    public ResizeHandleOverlay() {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
        TabStop = false;
    }

    protected override CreateParams CreateParams {
        get {
            var createParams = base.CreateParams;
            createParams.ExStyle |= WsExTransparent;
            return createParams;
        }
    }

    protected override void OnPaintBackground(PaintEventArgs pevent) {
        // Keep hit area visually invisible.
    }

    protected override void OnPaint(PaintEventArgs e) {
        // Keep hit area visually invisible.
    }
}
