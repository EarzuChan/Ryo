using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.Misc;
using Me.EarzuChan.Ryo.Kurisu.Utils;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Utils;
using KurisuWebResponse = Me.EarzuChan.Ryo.Kurisu.WebCalls.WebResponse;

namespace Me.EarzuChan.Ryo.Kurisu.WindowManagers;

internal sealed class BrowserKurisuWindowManager : IKurisuWindowManager {
    private const string WebSocketRoute = "/__kurisu_bridge";

    private readonly Lock _syncRoot = new();
    private readonly ManualResetEventSlim _closedSignal = new(false);

    private KurisuApp? _app;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private WebSocket? _webSocket;
    private string _rootFolder = string.Empty;
    private string _startUpUrl = string.Empty;
    private KurisuWindowState _windowState = KurisuWindowState.Maximized;

    private int _closed;
    private int _appReadyRaised;

    public event Action? AppReady;
    public event Action? AppClosed;

    public void Init(KurisuApp app) {
        _app = app;
        // _windowState = app.Profile.StartUpWindowState; DISABLED due to IT'S A FXXKING BROWSER
        _rootFolder = PathResolutionUtils.ResolveWebResourcePath(app.Profile.WebResourcePath);

        if (!Directory.Exists(_rootFolder)) throw new DirectoryNotFoundException($"WebResourcePath 不存在：{_rootFolder}");

        var port = GetAvailableTcpPort();
        var baseUrl = $"http://127.0.0.1:{port}/";
        var bridgeWsUrl = $"ws://127.0.0.1:{port}{WebSocketRoute}";
        _startUpUrl = BuildStartUpUrl(app, baseUrl, bridgeWsUrl);

        _listener = new HttpListener();
        _listener.Prefixes.Add(baseUrl);
        _listener.Start();

        _cts = new CancellationTokenSource();
        Task.Run(() => RunServerLoopAsync(_cts.Token));
    }

    public void Show() {
        if (string.IsNullOrWhiteSpace(_startUpUrl)) throw new InvalidOperationException("Window 未初始化");

        try {
            Process.Start(new ProcessStartInfo {
                FileName = _startUpUrl,
                UseShellExecute = true
            });
        } catch (Exception ex) {
            throw new InvalidOperationException($"启动浏览器失败：{_startUpUrl}", ex);
        }

        _closedSignal.Wait();
    }

    public void Close() {
        if (Interlocked.Exchange(ref _closed, 1) == 1) return;

        try {
            _cts?.Cancel();
        } catch {
            // Ignore
        }

        try {
            _listener?.Stop();
            _listener?.Close();
        } catch {
            // Ignore
        }

        lock (_syncRoot) {
            if (_webSocket != null) {
                try {
                    if (_webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived) _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "App closing", CancellationToken.None).GetAwaiter().GetResult();
                } catch {
                    // Ignore
                } finally {
                    _webSocket.Dispose();
                    _webSocket = null;
                }
            }
        }

        _closedSignal.Set();
        AppClosed?.Invoke();
    }

    public void SetWindowState(KurisuWindowState state) {
        if (_windowState == state) return;
        _windowState = state;
        _app?.TriggerAppEvent(new AppEvent(AppEventType.AppWindowStateChanged, _windowState));
    }

    public KurisuWindowState GetWindowState() => _windowState;

    public KurisuHostCapabilities GetHostCapabilities() => new(false);

    public void EmitWebEvent(WebLetter model) {
        var message = new KurisuBridgeMessage {
            Kind = KurisuBridgeMessageKinds.WebEvent,
            Letter = model
        };

        _ = SendBridgeMessageAsync(message);
    }

    private async Task RunServerLoopAsync(CancellationToken token) {
        if (_listener == null) return;

        while (!token.IsCancellationRequested && _listener.IsListening) {
            HttpListenerContext context;
            try {
                context = await _listener.GetContextAsync();
            } catch (Exception ex) when (ex is ObjectDisposedException or HttpListenerException) {
                break;
            }

            _ = Task.Run(() => HandleContextAsync(context, token), token);
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken token) {
        try {
            if (context.Request.IsWebSocketRequest && string.Equals(context.Request.Url?.AbsolutePath, WebSocketRoute, StringComparison.OrdinalIgnoreCase)) {
                await HandleWebSocketAsync(context, token);
                return;
            }

            await ServeStaticAsync(context, token);
        } catch (Exception ex) {
            Trace.WriteLine(TextUtils.MakeErrorMsgText("处理Browser后端请求失败", ex, true));
            TryWriteStatus(context.Response, HttpStatusCode.InternalServerError);
        }
    }

    private async Task HandleWebSocketAsync(HttpListenerContext context, CancellationToken token) {
        WebSocket? socket = null;
        try {
            var wsContext = await context.AcceptWebSocketAsync(null);
            socket = wsContext.WebSocket;

            lock (_syncRoot) {
                _webSocket?.Dispose();
                _webSocket = socket;
            }

            RaiseAppReadyOnce();
            await ReceiveWebSocketLoopAsync(socket, token);
        } finally {
            lock (_syncRoot) if (ReferenceEquals(_webSocket, socket)) _webSocket = null;

            socket?.Dispose();
        }
    }

    private async Task ReceiveWebSocketLoopAsync(WebSocket socket, CancellationToken token) {
        var buffer = new byte[16 * 1024];
        var textBuilder = new StringBuilder();

        while (!token.IsCancellationRequested && socket.State is WebSocketState.Open or WebSocketState.CloseReceived) {
            WebSocketReceiveResult result;
            try {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            } catch (OperationCanceledException) {
                break;
            } catch (Exception ex) {
                Trace.WriteLine(TextUtils.MakeErrorMsgText("Browser后端接收WebSocket消息失败", ex, true));
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close) {
                try {
                    if (socket.State == WebSocketState.CloseReceived)
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                } catch {
                    // Ignore
                }

                break;
            }

            if (result.MessageType != WebSocketMessageType.Text) continue;

            textBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (!result.EndOfMessage) continue;

            var messageJson = textBuilder.ToString();
            textBuilder.Clear();
            HandleIncomingBridgeMessage(messageJson);
        }
    }

    private void HandleIncomingBridgeMessage(string json) {
        if (_app == null) return;

        KurisuBridgeMessage message;
        try {
            message = DataModelParsingUtils.ParseBridgeMessageJson(json);
        } catch (Exception ex) {
            Trace.WriteLine(TextUtils.MakeErrorMsgText("解析Bridge消息失败", ex, true));
            return;
        }

        switch (message.Kind) {
            case KurisuBridgeMessageKinds.WebEvent:
                if (message.Letter == null) return;
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
                var response = KurisuWebResponse.Failure("bridge_unknown_kind", $"未知消息类型：{message.Kind}");
                SendWebCallResponse(message.RequestId, response);

                return;
        }
    }

    private void HandleWebCallRequest(KurisuBridgeMessage message) {
        if (_app == null || string.IsNullOrWhiteSpace(message.RequestId)) return;
        if (message.Letter == null) {
            SendWebCallResponse(message.RequestId, KurisuWebResponse.Failure("bridge_invalid_request", "WebCallRequest 的 letter 为空"));
            return;
        }

        KurisuWebResponse response;
        try {
            response = _app.RespondWebCall(message.Letter);
        } catch (Exception ex) {
            Trace.WriteLine(TextUtils.MakeErrorMsgText($"处理WebCall失败：{message.Letter.Name}", ex, true));
            response = KurisuWebResponse.Failure("bridge_web_call_exception", ex.Message);
        }

        SendWebCallResponse(message.RequestId, response);
    }

    private void SendWebCallResponse(string requestId, KurisuWebResponse response) {
        var message = new KurisuBridgeMessage {
            Kind = KurisuBridgeMessageKinds.WebCallResponse,
            RequestId = requestId,
            Response = response
        };

        _ = SendBridgeMessageAsync(message);
    }

    private Task SendBridgeMessageAsync(KurisuBridgeMessage message) {
        WebSocket? socket;
        lock (_syncRoot) socket = _webSocket;

        if (socket is not { State: WebSocketState.Open }) return Task.CompletedTask;

        var json = message.ToJson();
        var bytes = Encoding.UTF8.GetBytes(json);
        return socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task ServeStaticAsync(HttpListenerContext context, CancellationToken token) {
        if (!HttpMethods.IsGetOrHead(context.Request.HttpMethod)) {
            TryWriteStatus(context.Response, HttpStatusCode.MethodNotAllowed);
            return;
        }

        var relativePath = context.Request.Url?.AbsolutePath ?? "/";
        if (relativePath == "/") relativePath = "/index.html";
        relativePath = Uri.UnescapeDataString(relativePath.TrimStart('/'));

        var fullPath = Path.GetFullPath(Path.Combine(_rootFolder, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(_rootFolder, StringComparison.OrdinalIgnoreCase)) {
            TryWriteStatus(context.Response, HttpStatusCode.Forbidden);
            return;
        }

        if (!File.Exists(fullPath)) {
            TryWriteStatus(context.Response, HttpStatusCode.NotFound);
            return;
        }

        context.Response.ContentType = GetContentType(fullPath);
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        if (!HttpMethods.IsHead(context.Request.HttpMethod)) {
            var bytes = await File.ReadAllBytesAsync(fullPath, token);
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, token);
        }

        context.Response.Close();
    }

    private void RaiseAppReadyOnce() {
        if (Interlocked.Exchange(ref _appReadyRaised, 1) == 1) return;
        AppReady?.Invoke();
    }

    private static int GetAvailableTcpPort() {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string BuildStartUpUrl(KurisuApp app, string baseUrl, string bridgeWsUrl) {
        if (app.Profile is { IsDebug: true, DebugStartUpWithDebugUrl: true }
            && Uri.TryCreate(app.Profile.DebugStartUpUrl, UriKind.Absolute, out var debugUri)) {
            return AppendQuery(debugUri.ToString(), "kurisuBridgeWs", bridgeWsUrl);
        }

        return AppendQuery(baseUrl, "kurisuBridgeWs", bridgeWsUrl);
    }

    private static string AppendQuery(string url, string key, string value) {
        var separator = url.Contains('?') ? '&' : '?';
        return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }

    private static string GetContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch {
        ".html" => "text/html; charset=utf-8",
        ".js" => "application/javascript; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".json" => "application/json; charset=utf-8",
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".ico" => "image/x-icon",
        _ => "application/octet-stream"
    };

    private static void TryWriteStatus(HttpListenerResponse response, HttpStatusCode code) {
        try {
            response.StatusCode = (int)code;
            response.Close();
        } catch {
            // Ignore
        }
    }

    private static class HttpMethods {
        public static bool IsGetOrHead(string method) => IsGet(method) || IsHead(method);
        public static bool IsGet(string method) => string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);
        public static bool IsHead(string method) => string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase);
    }
}
