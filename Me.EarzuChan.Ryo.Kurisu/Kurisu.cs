using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.AppEvents.Handlers;
using Me.EarzuChan.Ryo.Kurisu.Exceptions;
using Me.EarzuChan.Ryo.Kurisu.Utils;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Kurisu.WebCalls.Responders;
using Me.EarzuChan.Ryo.Kurisu.WebEvents.Handlers;
using Me.EarzuChan.Ryo.Kurisu.HostBackends;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Kurisu;

internal sealed record RegisteredWebEventHandler(WebEventHandlerAttribute Attribute, Type HandlerType);

internal sealed record RegisteredWebCallResponder(WebCallResponderAttribute Attribute, Type ResponderType);

internal sealed record RegisteredAppEventHandler(AppEventHandlerAttribute Attribute, Type HandlerType);

public class WebLetter(string name, params object[] args) {
    public string Name = name;
    public object[] Args = args;
}

public class KurisuApp {
    internal readonly KurisuAppProfile Profile;
    internal readonly ArrayList Dependencies;
    internal readonly IReadOnlyDictionary<string, RegisteredWebEventHandler> WebEventHandlers;
    internal readonly IReadOnlyDictionary<AppEventType, RegisteredAppEventHandler> AppEventHandlers;
    internal readonly IReadOnlyDictionary<string, RegisteredWebCallResponder> WebCallResponders;
    internal readonly IHostBackend HostBackend;

    private readonly KurisuAppContext Context;

    private bool _appInitializedRaised;
    private bool _stopping;
    private bool _stopped;

    internal KurisuApp(KurisuAppProfile profile, ArrayList dependencies,
        IReadOnlyDictionary<string, RegisteredWebEventHandler> webEventHandlers,
        IReadOnlyDictionary<AppEventType, RegisteredAppEventHandler> appEventHandlers,
        IReadOnlyDictionary<string, RegisteredWebCallResponder> webCallResponders, IHostBackend hostBackend) {
        Profile = profile;
        Dependencies = dependencies;
        WebEventHandlers = webEventHandlers;
        AppEventHandlers = appEventHandlers;
        WebCallResponders = webCallResponders;

        Context = new KurisuAppContext(this);

        hostBackend.AppReady += OnWindowReady;
        hostBackend.AppClosed += OnWindowClosed;
        hostBackend.Init(this);
        HostBackend = hostBackend;
    }

    public void Run() => HostBackend.Show();

    public void Stop() {
        if (_stopping) return;
        _stopping = true;

        HostBackend.Close();
        EnsureAppStopped();
    }

    private void OnWindowReady() {
        if (_appInitializedRaised) return;
        _appInitializedRaised = true;
        TriggerAppEvent(new AppEvent(AppEventType.AppInitialized));
    }

    private void OnWindowClosed() => EnsureAppStopped();

    private void EnsureAppStopped() {
        if (_stopped) return;
        _stopped = true;
        TriggerAppEvent(new AppEvent(AppEventType.AppStopped));
    }

    internal void HandleWebEvent(WebLetter model) {
        Trace.WriteLine($"WebEvent名称：{model.Name} 参数数：{model.Args.Length}");

        if (TrySplitBuiltInRoute(model.Name, out var apiName, out var commandName)) {
            if (TryHandleBuiltInWebEvent(apiName, commandName, model.Args, out var error)) {
                if (error != null) Trace.WriteLine(error);
                return;
            }
        } else Trace.WriteLine("该WebEvent为普通WebEvent");

        if (!WebEventHandlers.TryGetValue(model.Name, out var registration)) {
            Trace.WriteLine($"找不到WebEvent {model.Name} 可用的Handler");
            return;
        }

        if (!InvocationBindingUtils.TryCreateInstance<IWebEventHandler>(registration.HandlerType, model.Args, out var handler, out var bindingError)) {
            Trace.WriteLine($"WebEvent {model.Name} 参数绑定失败：{bindingError}");
            return;
        }

        if (handler == null) {
            Trace.WriteLine($"WebEvent {model.Name} 参数绑定失败：Handler实例为null");
            return;
        }

        try {
            handler.Handle(Context);
        } catch (Exception e) {
            Trace.WriteLine(TextUtils.MakeErrorMsgText($"响应WebEvent {model.Name} 失败", e, true));
        }
    }

    internal WebResponse RespondWebCall(WebLetter model) {
        Trace.WriteLine($"WebCall名称：{model.Name} 参数数：{model.Args.Length}");

        if (TrySplitBuiltInRoute(model.Name, out var apiName, out var commandName)) {
            Trace.WriteLine($"该WebCall意图调用内置Api：{apiName}，需求：{commandName}");

            if (TryHandleBuiltInWebCall(apiName, commandName, model.Args, out var response)) return response;

            var noSuchBuiltInApi = $"没有内置Api：{apiName}";
            Trace.WriteLine(noSuchBuiltInApi);
            return WebResponse.Failure(KurisuErrorCode.BuiltinApiNotFound, noSuchBuiltInApi, new { apiName, commandName });
        }

        Trace.WriteLine("该WebCall为普通WebCall");

        if (!WebCallResponders.TryGetValue(model.Name, out var registration)) {
            var noSuchResponder = $"找不到WebCall {model.Name} 可用的Responder";
            Trace.WriteLine(noSuchResponder);
            return WebResponse.Failure(KurisuErrorCode.WebCallNotFound, noSuchResponder, new { model.Name });
        }

        if (!InvocationBindingUtils.TryCreateInstance<IWebCallResponder>(registration.ResponderType, model.Args, out var responder, out var bindingError)) {
            var wrongArgs = $"WebCall {model.Name} 的参数不对：{bindingError}";
            Trace.WriteLine(wrongArgs);
            return WebResponse.Failure(KurisuErrorCode.WebCallInvalidArguments, wrongArgs, new { model.Name, model.Args });
        }

        if (responder == null)
            return WebResponse.Failure(KurisuErrorCode.WebCallInvalidResponder, $"WebCall {model.Name} 绑定出的Responder为null");

        try {
            return responder.Respond(Context) ?? WebResponse.Failure(KurisuErrorCode.WebCallNullResponse, $"WebCall {model.Name} 返回了 null");
        } catch (KurisuKnownException e) {
            return WebResponse.Failure(e.ErrorCode, e.Message, e.Details);
        } catch (Exception e) {
            var eText = TextUtils.MakeErrorMsgText($"执行WebCall {model.Name} 失败", e, true);
            Trace.WriteLine(eText);
            return WebResponse.Failure(KurisuErrorCode.WebCallExecutionError, eText, e.Message);
        }
    }

    internal void EmitWebEvent(WebLetter model) => HostBackend.EmitWebEvent(model);

    internal void TriggerAppEvent(AppEvent appEvent) {
        Trace.WriteLine($"AppEvent类型：{appEvent.EventType} 参数数：{appEvent.Args.Length}");

        Context.EmitWebEvent(new WebLetter($"AppEvent:{appEvent.EventType}", appEvent.Args));

        if (!AppEventHandlers.TryGetValue(appEvent.EventType, out var registration)) {
            Trace.WriteLine($"找不到AppEvent {appEvent.EventType} 可用的Handler");
            return;
        }

        if (!InvocationBindingUtils.TryCreateInstance<IAppEventHandler>(registration.HandlerType, appEvent.Args, out var handler, out var bindingError)) {
            Trace.WriteLine($"AppEvent {appEvent.EventType} 参数绑定失败：{bindingError}");
            return;
        }

        if (handler == null) {
            Trace.WriteLine($"AppEvent {appEvent.EventType} 参数绑定失败：Handler实例为null");
            return;
        }

        try {
            handler.Handle(Context);
        } catch (Exception e) {
            Trace.WriteLine(TextUtils.MakeErrorMsgText($"执行AppEvent {appEvent.EventType} 失败", e, true));
        }
    }

    private bool TryHandleBuiltInWebEvent(string apiName, string commandName, object[] args, out string? error) {
        error = null;

        switch (apiName) {
            case "AppProperty":
                if (!TryParseEnum<KurisuAppProperty>(commandName, out var property, out var parseError)) {
                    error = parseError;
                    return true;
                }

                try {
                    Context.SetAppProperty(property, args);
                } catch (KurisuKnownException e) {
                    error = $"[{e.ErrorCode}] {e.Message}";
                } catch (Exception e) {
                    error = TextUtils.MakeErrorMsgText($"设置属性 {property} 失败", e, true);
                }

                return true;

            case "Preference":
                if (args.Length < 1) {
                    error = "Preference WebEvent 至少需要一个参数（value）";
                    return true;
                }

                Context.SetPreference(commandName, args[0]);
                return true;

            case "AppCommand":
                if (!TryParseEnum<KurisuAppCommand>(commandName, out var command, out parseError)) {
                    error = parseError;
                    return true;
                }

                try {
                    Context.ExecuteAppCommand(command, args);
                } catch (KurisuKnownException e) {
                    error = $"[{e.ErrorCode}] {e.Message}";
                } catch (Exception e) {
                    error = TextUtils.MakeErrorMsgText($"执行命令 {command} 失败", e, true);
                }

                return true;

            default: return false;
        }
    }

    private bool TryHandleBuiltInWebCall(string apiName, string commandName, object[] args, out WebResponse response) {
        response = WebResponse.Failure(KurisuErrorCode.BuiltinApiNotFound, $"没有内置Api：{apiName}");

        switch (apiName) {
            case "AppProperty":
                if (!TryParseEnum<KurisuAppProperty>(commandName, out var property, out var parseError)) {
                    response = WebResponse.Failure(KurisuErrorCode.BuiltinPropertyParseFailed, parseError, commandName);
                    return true;
                }

                try {
                    var propertyValue = Context.GetAppProperty<object>(property);
                    response = propertyValue == null
                        ? WebResponse.Failure(KurisuErrorCode.BuiltinPropertyNotFound, $"找不到该属性{property}")
                        : WebResponse.Success(propertyValue);
                } catch (KurisuKnownException e) {
                    response = WebResponse.Failure(e.ErrorCode, e.Message, e.Details);
                } catch (Exception e) {
                    response = WebResponse.Failure(KurisuErrorCode.BuiltinPropertyReadFailed,
                        TextUtils.MakeErrorMsgText($"读取属性{property}失败", e, true));
                }

                return true;

            case "Preference":
                var defaultValue = args.Length == 1 ? args[0] : null;
                var preferenceValue = defaultValue == null ? Context.GetPreference<object>(commandName) : Context.GetPreference(commandName, defaultValue);
                response = preferenceValue == null
                    ? WebResponse.Failure(KurisuErrorCode.BuiltinPreferenceNotFound, $"找不到该偏好项{commandName}")
                    : WebResponse.Success(preferenceValue);
                return true;

            case "AppCommand":
                if (!TryParseEnum<KurisuAppCommand>(commandName, out var command, out parseError)) {
                    response = WebResponse.Failure(KurisuErrorCode.BuiltinCommandParseFailed, parseError, commandName);
                    return true;
                }

                try {
                    var returnValue = Context.ExecuteAppCommand<object>(command, args);
                    response = returnValue == null
                        ? WebResponse.Success()
                        : WebResponse.Success(returnValue);
                } catch (KurisuKnownException e) {
                    response = WebResponse.Failure(e.ErrorCode, e.Message, e.Details);
                } catch (Exception e) {
                    response = WebResponse.Failure(KurisuErrorCode.BuiltinCommandFailed,
                        TextUtils.MakeErrorMsgText($"执行命令{command}失败", e, true));
                }

                return true;

            default: return false;
        }
    }

    private static bool TrySplitBuiltInRoute(string name, out string apiName, out string commandName) {
        var separatorIndex = name.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex >= name.Length - 1) {
            apiName = string.Empty;
            commandName = string.Empty;
            return false;
        }

        apiName = name[..separatorIndex];
        commandName = name[(separatorIndex + 1)..];
        return true;
    }

    private static bool TryParseEnum<TEnum>(string value, out TEnum enumValue, out string error)
        where TEnum : struct, Enum {
        if (!Enum.TryParse(value, true, out enumValue)) {
            error = $"无法把 `{value}` 解析为 `{typeof(TEnum).Name}`";
            return false;
        }

        if (!Enum.IsDefined(typeof(TEnum), enumValue)) {
            error = $"`{value}` 不是合法的 `{typeof(TEnum).Name}` 值";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static KurisuAppBuilder CreateBuilder(KurisuAppProfile profile) => new(profile);
}

public class KurisuAppBuilder {
    private readonly KurisuAppProfile Profile;
    private readonly ArrayList Dependencies = [];
    private readonly Dictionary<string, RegisteredWebEventHandler> WebEventHandlers = new(StringComparer.Ordinal);
    private readonly Dictionary<AppEventType, RegisteredAppEventHandler> AppEventHandlers = new();
    private readonly Dictionary<string, RegisteredWebCallResponder> WebCallResponders = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> Stuffs = new();
    private IHostBackend? AppHostBackend;
    private bool IsBuilt;

    internal KurisuAppBuilder(KurisuAppProfile profile) => Profile = profile;

    public KurisuApp Build() {
        if (IsBuilt) throw new InvalidOperationException("Builder instance has already built a product");
        if (AppHostBackend == null) throw new KurisuAppBuildingException("没有可使用的宿主后端");

        if (Profile.WebEventHandlerRegistrationStrategy == RegistrationStrategy.ScanAndRegisterAutomatically)
            ScanWebEventHandlers();
        if (Profile.AppEventHandlerRegistrationStrategy == RegistrationStrategy.ScanAndRegisterAutomatically)
            ScanAppEventHandlers();
        if (Profile.WebCallResponderRegistrationStrategy == RegistrationStrategy.ScanAndRegisterAutomatically)
            ScanWebCallResponders();

        KurisuApp application = new(
            Profile,
            Dependencies,
            new Dictionary<string, RegisteredWebEventHandler>(WebEventHandlers, StringComparer.Ordinal),
            new Dictionary<AppEventType, RegisteredAppEventHandler>(AppEventHandlers),
            new Dictionary<string, RegisteredWebCallResponder>(WebCallResponders, StringComparer.Ordinal),
            AppHostBackend);

        IsBuilt = true;
        return application;
    }

    public KurisuAppBuilder UseDefaultHostBackend() {
#if WINDOWS
        return UseHostBackend(new Win32HostBackend());
#else
        return UseHostBackend(new PhotinoHostBackend());
#endif
    }

    public KurisuAppBuilder UseHostBackend(IHostBackend hostBackend) => this.Also(_ => {
        if (AppHostBackend != null) throw new InvalidOperationException("不允许重复设置宿主后端");
        AppHostBackend = hostBackend;
    });

    private void ScanWebEventHandlers() {
        var types = TypeUtils.GetAppAllTypes();
        foreach (var type in types) {
            var attribute = type.GetCustomAttribute<WebEventHandlerAttribute>();
            if (attribute == null || !typeof(IWebEventHandler).IsAssignableFrom(type) || !attribute.Scannable) continue;
            RegisterWebEventHandlerDirectly(attribute, type);
        }
    }

    private void ScanAppEventHandlers() {
        var types = TypeUtils.GetAppAllTypes();
        foreach (var type in types) {
            var attribute = type.GetCustomAttribute<AppEventHandlerAttribute>();
            if (attribute == null || !typeof(IAppEventHandler).IsAssignableFrom(type) || !attribute.Scannable) continue;
            RegisterAppEventHandlerDirectly(attribute, type);
        }
    }

    private void ScanWebCallResponders() {
        var types = TypeUtils.GetAppAllTypes();
        foreach (var type in types) {
            var attribute = type.GetCustomAttribute<WebCallResponderAttribute>();
            if (attribute == null || !typeof(IWebCallResponder).IsAssignableFrom(type) || !attribute.Scannable) continue;
            RegisterWebCallResponderDirectly(attribute, type);
        }
    }

    public KurisuAppBuilder RegisterWebEventHandler(WebEventHandlerAttribute handlerAttribute, Type handler) => this.Also(_ => {
        if (handlerAttribute == null || !typeof(IWebEventHandler).IsAssignableFrom(handler))
            throw new KurisuAppBuildingException("检查你注册处理器时提供的参数");

        RegisterWebEventHandlerDirectly(handlerAttribute, handler);
    });

    public KurisuAppBuilder RegisterAppEventHandler(AppEventHandlerAttribute handlerAttribute, Type handler) => this.Also(_ => {
        if (handlerAttribute == null || !typeof(IAppEventHandler).IsAssignableFrom(handler))
            throw new KurisuAppBuildingException("检查你注册处理器时提供的参数");

        RegisterAppEventHandlerDirectly(handlerAttribute, handler);
    });

    public KurisuAppBuilder RegisterWebCallResponder(WebCallResponderAttribute responderAttribute, Type handler) => this.Also(_ => {
        if (responderAttribute == null || !typeof(IWebCallResponder).IsAssignableFrom(handler))
            throw new KurisuAppBuildingException("检查你注册处理器时提供的参数");

        RegisterWebCallResponderDirectly(responderAttribute, handler);
    });

    private void RegisterWebEventHandlerDirectly(WebEventHandlerAttribute handlerAttribute, Type handler) {
        if (handlerAttribute.IsDev && !Profile.IsDebug) return;

        if (WebEventHandlers.ContainsKey(handlerAttribute.EventName))
            throw new KurisuAppBuildingException(
                $"WebEvent `{handlerAttribute.EventName}` 已注册，不允许重复注册。请保持一个事件只对应一个 Handler。");

        WebEventHandlers.Add(handlerAttribute.EventName, new RegisteredWebEventHandler(handlerAttribute, handler));
    }

    private void RegisterAppEventHandlerDirectly(AppEventHandlerAttribute handlerAttribute, Type handler) {
        if (handlerAttribute.IsDev && !Profile.IsDebug) return;

        if (AppEventHandlers.ContainsKey(handlerAttribute.EventType))
            throw new KurisuAppBuildingException(
                $"AppEvent `{handlerAttribute.EventType}` 已注册，不允许重复注册。");

        AppEventHandlers.Add(handlerAttribute.EventType, new RegisteredAppEventHandler(handlerAttribute, handler));
    }

    private void RegisterWebCallResponderDirectly(WebCallResponderAttribute responderAttribute, Type handler) {
        if (responderAttribute.IsDev && !Profile.IsDebug) return;

        if (WebCallResponders.ContainsKey(responderAttribute.EventName))
            throw new KurisuAppBuildingException(
                $"WebCall `{responderAttribute.EventName}` 已注册，不允许重复注册。一个方法只能有一个 Responder。");

        WebCallResponders.Add(responderAttribute.EventName, new RegisteredWebCallResponder(responderAttribute, handler));
    }

    public KurisuAppBuilder ProvideDependency<T>() where T : new() => ProvideDependency(new T());

    public KurisuAppBuilder ProvideDependency<T>(T dependency) => this.Also(_ => {
        if (Dependencies.OfType<T>().Any()) throw new KurisuAppBuildingException("已经提供过该依赖项了");
        Dependencies.Add(dependency);
    });

    public KurisuAppBuilder Provide<T>(string name, T stuff) => this.Also(_ => stuff.EnsureNotNull(it => { Stuffs.Add(name, it); }));
}

// 解耦网址 重构窗口模式
public record KurisuAppProfile(
    string Icon,
    string StartUpUrl,
    string DebugStartUpUrl,
    bool UseIcon = false,
    string Name = "Kurisu App",
    string Version = "1.0.0.0",
    string VirtualHostName = "kurisu_app",
    string WebResourcePath = "WebResources",
    bool WindowBorderless = true,
    int WindowWidth = 1200,
    int WindowHeight = 800,
    KurisuWindowState StartUpWindowState = KurisuWindowState.Normal,
    RegistrationStrategy WebEventHandlerRegistrationStrategy = RegistrationStrategy.ScanAndRegisterAutomatically,
    RegistrationStrategy AppEventHandlerRegistrationStrategy = RegistrationStrategy.ScanAndRegisterAutomatically,
    RegistrationStrategy WebCallResponderRegistrationStrategy = RegistrationStrategy.ScanAndRegisterAutomatically,
    bool DebugStartUpWithDebugUrl = false,
    bool DebugAutomaticOpenDevTool = true,
    bool IsDebug = false
);

public class KurisuAppContext {
    private readonly KurisuApp App;

    internal KurisuAppContext(KurisuApp app) => App = app;

    public T? Inject<T>() where T : class => LangExt.WrappedTry("Cannot inject dependency", () => App.Dependencies.OfType<T>().First(), new Dictionary<Type, string> { { typeof(InvalidOperationException), "No such a dependency" } });

    public void EmitWebEvent(WebLetter model) => App.EmitWebEvent(model);

    public void SetAppProperty(KurisuAppProperty property, params object[] value) {
        switch (property) {
            case KurisuAppProperty.WindowState:
                if (value.Length < 1)
                    throw new KurisuKnownException(KurisuErrorCode.InvalidArgument, "WindowState 属性要求至少一个参数");

                if (!InvocationBindingUtils.TryConvertArgument(value[0], typeof(KurisuWindowState),
                        out var stateValue, out var error))
                    throw new KurisuKnownException(KurisuErrorCode.InvalidArgument, $"WindowState 参数类型不对：{error}");

                EnsureCapability(App.HostBackend.GetHostCapabilities().SupportsWindowStateWrite, "window_state_write");
                if (!App.HostBackend.TrySetWindowState((KurisuWindowState)stateValue!))
                    throw new KurisuKnownException(KurisuErrorCode.HostOperationFailed, "宿主后端拒绝设置窗口状态");
                break;

            default: throw new ArgumentOutOfRangeException(nameof(property), property, "没有合乎的属性");
        }
    }

    public void SetPreference(string key, object value) => PreferenceUtils.SetPreference(key, value);

    public T? GetPreference<T>(string key, T? defaultValue = default) => PreferenceUtils.GetPreference(key, defaultValue);

    public T? GetAppProperty<T>(KurisuAppProperty property) {
        object returnValue = property switch {
            KurisuAppProperty.WindowState => GetWindowStateOrThrow(),
            KurisuAppProperty.HostCapabilities => App.HostBackend.GetHostCapabilities(),
            KurisuAppProperty.IsDebug => App.Profile.IsDebug,
            _ => throw new ArgumentOutOfRangeException(nameof(property), property, "没有合乎的属性")
        };

        return (T?)returnValue;
    }

    public T? ExecuteAppCommand<T>(KurisuAppCommand command, params object[] modelArgs) {
        object? returnValue = null;

        switch (command) {
            case KurisuAppCommand.StopApp:
                App.Stop();
                break;
            
            case KurisuAppCommand.OpenFileDialog:
                returnValue = OpenFileByDialogOrThrow(modelArgs);
                break;
            
            case KurisuAppCommand.SaveFileDialog:
                returnValue = SaveFileByDialogOrThrow(modelArgs);
                break;

            default: throw new ArgumentOutOfRangeException(nameof(command), command, "没有合乎的命令");
        }

        return (T?)returnValue;
    }

    public void ExecuteAppCommand(KurisuAppCommand command, params object[] modelArgs) => ExecuteAppCommand<object>(command, modelArgs);

    private KurisuWindowState GetWindowStateOrThrow() {
        EnsureCapability(App.HostBackend.GetHostCapabilities().SupportsWindowStateRead, "window_state_read");
        
        if (!App.HostBackend.TryGetWindowState(out var state)) throw new KurisuKnownException(KurisuErrorCode.HostOperationFailed, "宿主后端拒绝读取窗口状态");

        return state;
    }

    private string? OpenFileByDialogOrThrow(object[] modelArgs) {
        EnsureCapability(App.HostBackend.GetHostCapabilities().SupportsOpenFileDialog, "open_file_dialog");
        var fileDescription = ReadRequiredStringArg(modelArgs, 0, "fileDescription");
        var fileExtension = ReadRequiredStringArg(modelArgs, 1, "fileExtension");

        return !App.HostBackend.TryOpenFileByDialog(fileDescription, fileExtension, out var filePath) ? throw new KurisuKnownException(KurisuErrorCode.HostOperationFailed, "宿主后端拒绝执行打开文件对话框") : filePath;
    }

    private string? SaveFileByDialogOrThrow(object[] modelArgs) {
        EnsureCapability(App.HostBackend.GetHostCapabilities().SupportsSaveFileDialog, "save_file_dialog");
        var fileDescription = ReadRequiredStringArg(modelArgs, 0, "fileDescription");
        var fileExtension = ReadRequiredStringArg(modelArgs, 1, "fileExtension");

        return !App.HostBackend.TrySaveFileByDialog(fileDescription, fileExtension, out var filePath) ? throw new KurisuKnownException(KurisuErrorCode.HostOperationFailed, "宿主后端拒绝执行保存文件对话框") : filePath;
    }

    private static void EnsureCapability(bool supported, string capabilityName) {
        if (!supported) throw new KurisuKnownException(KurisuErrorCode.CapabilityNotSupported, $"当前宿主后端不支持能力：{capabilityName}", new { capability = capabilityName });
    }

    private static string ReadRequiredStringArg(object[] args, int index, string name) {
        if (args.Length <= index) throw new KurisuKnownException(KurisuErrorCode.InvalidArgument, $"缺少参数 `{name}`");

        var value = args[index];
        if (value is string text && !string.IsNullOrWhiteSpace(text)) return text;
        throw new KurisuKnownException(KurisuErrorCode.InvalidArgument, $"参数 `{name}` 需为非空字符串");
    }
}

public enum KurisuAppProperty {
    WindowState,
    HostCapabilities,
    IsDebug
}

public enum KurisuAppCommand {
    StopApp,
    OpenFileDialog,
    SaveFileDialog
}
