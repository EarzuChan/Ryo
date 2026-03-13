using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.Exceptions;
using Me.EarzuChan.Ryo.Kurisu.Misc;
using Me.EarzuChan.Ryo.Kurisu.Utils;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Utils;
using Photino.NET;

namespace Me.EarzuChan.Ryo.Kurisu.HostBackends;

internal sealed class PhotinoHostBackend : IHostBackend {
    private KurisuApp? _app;
    private PhotinoWindow? _window;
    private bool _appReadyRaised;
    private bool _appClosedRaised;
    private bool _bridgeReady;
    private bool _windowClosed;
    private readonly Lock _bridgeLock = new();
    private readonly Queue<KurisuBridgeMessage> _pendingBridgeMessages = new();
    private KurisuWindowState _windowState = KurisuWindowState.Normal;

    private DragState? _dragState;
    private string _webResourcePath = string.Empty;
    private string _customSchemeName = DefaultCustomScheme;

    public event Action? AppReady;
    public event Action? AppClosed;

    public void Init(KurisuApp app) {
        _app = app;
        _windowClosed = false;
        _bridgeReady = false;
        _windowState = KurisuWindowState.Normal;
        lock (_bridgeLock) _pendingBridgeMessages.Clear();

        _webResourcePath = PathResolutionUtils.ResolveWebResourcePath(app.Profile.WebResourcePath);
        _customSchemeName = ResolveCustomSchemeName(app.Profile.VirtualHostName);

        var startEntry = ResolveStartEntry(app.Profile, _customSchemeName);
        Trace.WriteLine($"Photino startup entry: {startEntry}");
        var enableTransparentWindow = app.Profile.WindowBorderless && OperatingSystem.IsMacOS();
        if (app.Profile.WindowBorderless && !enableTransparentWindow) Trace.WriteLine("Photino: 当前平台禁用透明窗口，仅在 macOS 使用透明背景。");

        _window = new PhotinoWindow()
            .SetTitle(app.Profile.Name)
            .SetUseOsDefaultLocation(false)
            .SetUseOsDefaultSize(false)
            .SetLeft(80)
            .SetTop(80)
            .SetSize(app.Profile.WindowWidth, app.Profile.WindowHeight)
            .SetChromeless(app.Profile.WindowBorderless)
            .SetTransparent(enableTransparentWindow)
            .SetResizable(true)
            .SetDevToolsEnabled(app.Profile is { IsDebug: true, DebugAutomaticOpenDevTool: true })
            .SetMinSize(640, 480)
            .RegisterWindowCreatedHandler((_, _) => Trace.WriteLine("Photino window created"))
            .RegisterWindowClosingHandler((_, _) => {
                _windowClosed = true;
                lock (_bridgeLock) {
                    _bridgeReady = false;
                    _pendingBridgeMessages.Clear();
                }
                RaiseAppClosedOnce();
                return false;
            })
            .RegisterCustomSchemeHandler(_customSchemeName, OnCustomSchemeRequest)
            .RegisterWebMessageReceivedHandler(OnWebMessageReceived);

        LoadStartupEntry(_window, startEntry);

        _window.WindowMaximizedHandler = (_, _) => UpdateWindowState(KurisuWindowState.Maximized);
        _window.WindowRestoredHandler = (_, _) => UpdateWindowState(KurisuWindowState.Normal);
        _window.WindowMinimizedHandler = (_, _) => UpdateWindowState(KurisuWindowState.Minimized);

        if (app.Profile.UseIcon && !string.IsNullOrWhiteSpace(app.Profile.Icon)) {
            try {
                _window.SetIconFile(app.Profile.Icon);
            } catch (Exception e) {
                Trace.WriteLine(TextUtils.MakeErrorMsgText($"Photino 加载图标失败：{app.Profile.Icon}", e, true));
            }
        }

        _ = TrySetWindowState(app.Profile.StartUpWindowState);
    }

    public void Show() {
        if (_window == null) throw new InvalidOperationException("Window 未初始化");
        _window.WaitForClose();
        RaiseAppClosedOnce();
    }

    public void Close() {
        if (_window == null) return;
        _windowClosed = true;
        lock (_bridgeLock) {
            _bridgeReady = false;
            _pendingBridgeMessages.Clear();
        }
        _window.Close();
        RaiseAppClosedOnce();
    }

    public void EmitWebEvent(WebLetter model) {
        SendBridgeMessage(new KurisuBridgeMessage {
            Kind = KurisuBridgeMessageKinds.WebEvent,
            Letter = model
        });
    }

    public KurisuHostCapabilities GetHostCapabilities() => new(
        SupportsWindowControls: true,
        SupportsWindowStateRead: true,
        SupportsWindowStateWrite: true,
        SupportsOpenFileDialog: true,
        SupportsSaveFileDialog: true
    );

    public bool TrySetWindowState(KurisuWindowState state) {
        if (_window == null) return false;

        switch (state) {
            case KurisuWindowState.Maximized: return TrySetMaximizedState();

            case KurisuWindowState.Minimized: return TrySetMinimizedState();

            default: return TrySetNormalState();
        }
    }

    public bool TryGetWindowState(out KurisuWindowState state) {
        if (_window == null) {
            state = KurisuWindowState.Normal;
            return false;
        }

        state = _window.Minimized ? KurisuWindowState.Minimized : _window.Maximized ? KurisuWindowState.Maximized : KurisuWindowState.Normal;
        _windowState = state;
        return true;
    }

    public bool TryOpenFileByDialog(string fileDescription, string fileExtension, out string? filePath) {
        if (_window == null) {
            filePath = null;
            return false;
        }

        var normalizedExtension = NormalizeExtension(fileExtension);
        var selections = _window.ShowOpenFile(
            $"打开{fileDescription}",
            string.Empty,
            false,
            [(fileDescription, [normalizedExtension])]
        );

        filePath = selections is { Length: > 0 } ? selections[0] : null;
        return true;
    }

    // FIXME：Photino的保存文件API问题：Win保存窗口出不来（无报错失败。虽说我不打算让AIEE版在Win上跑，所以这个Win的行为也许可以忽略）、Linux（Arch）保存完丢后缀名、mac
    public bool TrySaveFileByDialog(string fileDescription, string fileExtension, string? suggestedFileName, out string? filePath) {
        if (_window == null) {
            filePath = null;
            return false;
        }

        var normalizedExtension = NormalizeExtension(fileExtension);
        var defaultPath = string.Empty;
        if (!string.IsNullOrWhiteSpace(suggestedFileName)) {
            var suggested = suggestedFileName.Trim();
            if (!suggested.EndsWith($".{normalizedExtension}", StringComparison.OrdinalIgnoreCase)) suggested = $"{suggested}.{normalizedExtension}";
            defaultPath = suggested;
        }
        filePath = _window.ShowSaveFile(
            $"保存{fileDescription}",
            defaultPath,
            [(fileDescription, [normalizedExtension])]
        );
        return true;
    }

    private void OnWebMessageReceived(object? _, string json) {
        if (_app == null) return;
        
        EnsureBridgeReady();

        KurisuBridgeMessage message;
        try {
            message = DataModelParsingUtils.ParseBridgeMessageJson(json);
        } catch (Exception ex) {
            Trace.WriteLine(TextUtils.MakeErrorMsgText("解析Photino Bridge消息失败", ex, true));
            return;
        }

        switch (message.Kind) {
            case KurisuBridgeMessageKinds.WebEvent:
                if (message.Letter == null) return;
                if (TryHandleHostWindowWebEvent(message.Letter)) return;

                try {
                    _app.HandleWebEvent(message.Letter);
                } catch (Exception ex) {
                    Trace.WriteLine(TextUtils.MakeErrorMsgText($"处理WebEvent失败：{message.Letter.Name}", ex, true));
                }

                return;

            case KurisuBridgeMessageKinds.WebCallRequest:
                HandleWebCallRequest(message);
                return;

            default:
                if (string.IsNullOrWhiteSpace(message.RequestId)) return;
                SendWebCallResponse(message.RequestId, WebResponse.Failure(KurisuErrorCode.BridgeUnknownKind, $"未知消息类型：{message.Kind}"));
                return;
        }
    }

    private void HandleWebCallRequest(KurisuBridgeMessage message) {
        if (_app == null || string.IsNullOrWhiteSpace(message.RequestId)) return;
        if (message.Letter == null) {
            SendWebCallResponse(message.RequestId, WebResponse.Failure(KurisuErrorCode.BridgeInvalidRequest, "WebCallRequest 的 letter 为空"));
            return;
        }

        WebResponse response;
        try {
            response = _app.RespondWebCall(message.Letter);
        } catch (Exception ex) {
            Trace.WriteLine(TextUtils.MakeErrorMsgText($"处理WebCall失败：{message.Letter.Name}", ex, true));
            response = WebResponse.Failure(KurisuErrorCode.BridgeWebCallException, ex.Message);
        }

        SendWebCallResponse(message.RequestId, response);
    }

    private void SendWebCallResponse(string requestId, WebResponse response) {
        SendBridgeMessage(new KurisuBridgeMessage {
            Kind = KurisuBridgeMessageKinds.WebCallResponse,
            RequestId = requestId,
            Response = response
        });
    }

    private void SendBridgeMessage(KurisuBridgeMessage message) {
        if (_window == null || _windowClosed) return;

        lock (_bridgeLock) {
            if (!_bridgeReady) {
                _pendingBridgeMessages.Enqueue(message);
                return;
            }
        }

        SendBridgeMessageDirectly(message);
    }

    private void EnsureBridgeReady() {
        if (_bridgeReady || _windowClosed) return;

        Queue<KurisuBridgeMessage> pending;
        lock (_bridgeLock) {
            if (_bridgeReady || _windowClosed) return;
            _bridgeReady = true;

            pending = new Queue<KurisuBridgeMessage>(_pendingBridgeMessages);
            _pendingBridgeMessages.Clear();
        }

        RaiseAppReadyOnce();
        foreach (var message in pending) SendBridgeMessageDirectly(message);
    }

    private void SendBridgeMessageDirectly(KurisuBridgeMessage message) {
        if (_window == null || _windowClosed) return;

        var json = message.ToJson();
        _window.SendWebMessage(json);
    }

    private bool TryHandleHostWindowWebEvent(WebLetter eventLetter) {
        if (_window == null) return false;
        if (!eventLetter.Name.StartsWith(HostWindowEventPrefix, StringComparison.OrdinalIgnoreCase)) return false;

        switch (eventLetter.Name) {
            case "HostWindow:DragBegin": return TryBeginDrag(eventLetter.Args);
            
            case "HostWindow:DragMove": return TryMoveDrag(eventLetter.Args);
            
            case "HostWindow:DragEnd":
                _dragState = null;
                return true;
        
            default: return true;
        }
    }

    private bool TryBeginDrag(object[] args) {
        if (_window == null) return true;
        if (!CanMoveOrResizeWindow()) return true;

        var screenX = ReadIntArg(args, 0);
        var screenY = ReadIntArg(args, 1);
        _dragState = new DragState(screenX, screenY, _window.Left, _window.Top);
        return true;
    }

    private bool TryMoveDrag(object[] args) {
        if (_window == null || _dragState == null) return true;
        if (!CanMoveOrResizeWindow()) return true;

        var currentScreenX = ReadIntArg(args, 0);
        var currentScreenY = ReadIntArg(args, 1);
        var nextLeft = _dragState.StartLeft + (currentScreenX - _dragState.StartScreenX);
        var nextTop = _dragState.StartTop + (currentScreenY - _dragState.StartScreenY);

        if (nextLeft == _window.Left && nextTop == _window.Top) return true;

        _window.MoveTo(nextLeft, nextTop, true);
        return true;
    }

    private bool TrySetMaximizedState() {
        if (_window == null) return false;
        if (_window.Minimized) _window.SetMinimized(false);
        if (!_window.Maximized) _window.SetMaximized(true);

        UpdateWindowState(KurisuWindowState.Maximized);
        return true;
    }

    private bool TrySetMinimizedState() {
        if (_window == null) return false;
        if (_window.Minimized && _windowState == KurisuWindowState.Minimized) return true;

        _window.SetMinimized(true);
        UpdateWindowState(KurisuWindowState.Minimized);
        return true;
    }

    private bool TrySetNormalState() {
        if (_window == null) return false;
        if (_windowState == KurisuWindowState.Normal && !_window.Minimized && !_window.Maximized) return true;

        if (_window.Minimized) _window.SetMinimized(false);
        if (_window.Maximized) _window.SetMaximized(false);

        UpdateWindowState(KurisuWindowState.Normal);
        return true;
    }

    private bool CanMoveOrResizeWindow() {
        if (_window == null) return false;
        return !_window.Minimized && !_window.Maximized;
    }

    private void UpdateWindowState(KurisuWindowState state) {
        if (_windowState == state) return;
        _windowState = state;
        EmitWindowStateChanged(state);
    }

    private void EmitWindowStateChanged(KurisuWindowState state) => _app?.TriggerAppEvent(new AppEvent(AppEventType.AppWindowStateChanged, state));

    private static string ResolveStartEntry(KurisuAppProfile profile, string customSchemeName) {
        if (profile is { IsDebug: true, DebugStartUpWithDebugUrl: true } && !string.IsNullOrWhiteSpace(profile.DebugStartUpUrl)) return profile.DebugStartUpUrl;

        if (TryUseRawStartUpUrl(profile, customSchemeName, out var explicitStart)) return explicitStart;

        var startupPath = ResolveStartupPathFromVirtualHostUrl(profile.StartUpUrl, profile.VirtualHostName);
        return $"{customSchemeName}://{CustomSchemeHost}{startupPath}";
    }

    private static bool TryUseRawStartUpUrl(KurisuAppProfile profile, string customSchemeName, out string startUrl) {
        startUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(profile.StartUpUrl)) return false;
        if (!Uri.TryCreate(profile.StartUpUrl, UriKind.Absolute, out var uri)) return false;

        var isVirtualHostUrl = string.Equals(uri.Host, profile.VirtualHostName, StringComparison.OrdinalIgnoreCase);
        if (isVirtualHostUrl) return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, customSchemeName, StringComparison.OrdinalIgnoreCase))
            return false;
        
        startUrl = profile.StartUpUrl;
        return true;

    }

    private static string NormalizeExtension(string extension) {
        var normalized = extension.Trim().TrimStart('*').TrimStart('.');
        return string.IsNullOrWhiteSpace(normalized) ? "*" : normalized;
    }

    private static int ReadIntArg(object[] args, int index) {
        if (args.Length <= index) return 0;
        var value = args[index];

        return value switch {
            int direct => direct,
            long longValue => (int)longValue,
            double doubleValue => (int)doubleValue,
            float floatValue => (int)floatValue,
            decimal decimalValue => (int)decimalValue,
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => 0
        };
    }

    private Stream OnCustomSchemeRequest(object sender, string scheme, string url, out string contentType) {
        try {
            var resolvedPath = ResolveWebResourceFilePath(url);
            if (resolvedPath == null) {
                contentType = "text/plain; charset=utf-8";
                return CreateTextContent("404 Not Found");
            }

            contentType = ResolveContentType(resolvedPath);
            return File.OpenRead(resolvedPath);
        } catch (Exception e) {
            Trace.WriteLine(TextUtils.MakeErrorMsgText($"Photino custom scheme 处理失败：{url}", e, true));
            contentType = "text/plain; charset=utf-8";
            return CreateTextContent("500 Internal Error");
        }
    }

    private string? ResolveWebResourceFilePath(string url) {
        var root = Path.GetFullPath(_webResourcePath);
        if (!Directory.Exists(root)) {
            Trace.WriteLine($"Photino WebResources 目录不存在：{root}");
            return null;
        }

        var relativePath = ExtractRelativePath(url);
        if (string.IsNullOrWhiteSpace(relativePath)) relativePath = "index.html";

        relativePath = Uri.UnescapeDataString(relativePath).Replace('\\', '/').TrimStart('/');
        if (relativePath.EndsWith('/')) relativePath += "index.html";

        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) segments = ["index.html"];
        foreach (var segment in segments) {
            if (string.Equals(segment, "..", StringComparison.Ordinal)) return null;
        }

        var combinedPath = Path.Combine(root, Path.Combine(segments));
        var fullPath = Path.GetFullPath(combinedPath);
        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)) return null;

        if (Directory.Exists(fullPath)) fullPath = Path.Combine(fullPath, "index.html");
        if (File.Exists(fullPath)) return fullPath;

        if (string.IsNullOrEmpty(Path.GetExtension(fullPath))) {
            var fallback = Path.Combine(root, "index.html");
            if (File.Exists(fallback)) return fallback;
        }

        return null;
    }

    private static string ExtractRelativePath(string url) {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri)) {
            var absolutePath = absoluteUri.AbsolutePath;
            if (string.IsNullOrWhiteSpace(absolutePath) || absolutePath == "/") return "index.html";
            return absolutePath.TrimStart('/');
        }

        var start = url.IndexOf("://", StringComparison.Ordinal);
        var rawPath = start >= 0 ? url[(start + 3)..] : url;

        var slashIndex = rawPath.IndexOf('/');
        rawPath = slashIndex >= 0 ? rawPath[(slashIndex + 1)..] : string.Empty;

        var queryOrFragment = rawPath.IndexOfAny(['?', '#']);
        if (queryOrFragment >= 0) rawPath = rawPath[..queryOrFragment];

        return string.IsNullOrWhiteSpace(rawPath) ? "index.html" : rawPath;
    }

    private static MemoryStream CreateTextContent(string text) => new(Encoding.UTF8.GetBytes(text));

    private static string ResolveContentType(string resourcePath) {
        var extension = Path.GetExtension(resourcePath);
        return extension switch {
            ".html" => "text/html; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".mjs" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".otf" => "font/otf",
            ".txt" => "text/plain; charset=utf-8",
            ".map" => "application/json; charset=utf-8",
            ".wasm" => "application/wasm",
            _ => "application/octet-stream"
        };
    }

    private static string ResolveCustomSchemeName(string virtualHostName) {
        var source = string.IsNullOrWhiteSpace(virtualHostName) ? DefaultCustomScheme : virtualHostName.Trim().ToLowerInvariant();
        var chars = new List<char>(source.Length);
        foreach (var c in source) {
            if (char.IsLetterOrDigit(c)) chars.Add(c);
        }

        var normalized = new string(chars.ToArray());
        if (string.IsNullOrWhiteSpace(normalized)) return DefaultCustomScheme;

        if (!char.IsLetter(normalized[0])) normalized = $"app{normalized}";
        return normalized;
    }

    private static string ResolveStartupPathFromVirtualHostUrl(string startupUrl, string virtualHostName) {
        if (!Uri.TryCreate(startupUrl, UriKind.Absolute, out var uri)) return "/index.html";
        if (!string.Equals(uri.Host, virtualHostName, StringComparison.OrdinalIgnoreCase)) return "/index.html";

        var path = string.IsNullOrWhiteSpace(uri.AbsolutePath) || uri.AbsolutePath == "/" ? "/index.html" : uri.AbsolutePath;
        return $"{path}{uri.Query}{uri.Fragment}";
    }

    private static void LoadStartupEntry(PhotinoWindow window, string startEntry) {
        if (Uri.TryCreate(startEntry, UriKind.Absolute, out var uri)) {
            window.Load(uri);
            return;
        }

        window.Load(startEntry);
    }

    private void RaiseAppReadyOnce() {
        if (_appReadyRaised) return;
        _appReadyRaised = true;
        AppReady?.Invoke();
    }

    private void RaiseAppClosedOnce() {
        if (_appClosedRaised) return;
        _appClosedRaised = true;
        AppClosed?.Invoke();
    }

    private const string HostWindowEventPrefix = "HostWindow:";
    private const string CustomSchemeHost = "app";
    private const string DefaultCustomScheme = "kurisu-app";

    private sealed record DragState(int StartScreenX, int StartScreenY, int StartLeft, int StartTop);
}
