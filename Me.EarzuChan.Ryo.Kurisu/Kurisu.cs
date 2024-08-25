using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.AppEvents.Handlers;
using Me.EarzuChan.Ryo.Kurisu.Exceptions;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Kurisu.WebCalls.Responders;
using Me.EarzuChan.Ryo.Kurisu.WebEvents.Handlers;
using Me.EarzuChan.Ryo.Kurisu.WindowManagers;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Kurisu;

public class WebLetter(string name, params object[] args)
{
    public string Name = name;

    public object[] Args = args;
}

public class KurisuApp
{
    internal readonly KurisuAppProfile Profile;
    internal readonly ArrayList Dependencies;
    internal readonly Dictionary<WebEventHandlerAttribute, Type> WebEventHandlers;
    internal readonly Dictionary<AppEventHandlerAttribute, Type> AppEventHandlers;
    internal readonly Dictionary<WebCallResponderAttribute, Type> WebCallResponders;
    internal readonly IKurisuWindowManager WindowManager;

    private readonly KurisuAppContext Context;

    internal KurisuApp(KurisuAppProfile profile, ArrayList dependencies,
        Dictionary<WebEventHandlerAttribute, Type> webEventHandlers,
        Dictionary<AppEventHandlerAttribute, Type> appEventHandlers,
        Dictionary<WebCallResponderAttribute, Type> webCallResponders, IKurisuWindowManager window)
    {
        Profile = profile;
        Dependencies = dependencies;

        WebEventHandlers = webEventHandlers;
        AppEventHandlers = appEventHandlers;
        WebCallResponders = webCallResponders;

        window.Init(this);
        WindowManager = window;

        Context = new(this);
    }

    public void Run()
    {
        TriggerAppEvent(new(AppEventType.AppInitialized));

        WindowManager.Show();
    }

    public void Stop()
    {
        TriggerAppEvent(new(AppEventType.AppStopped));

        WindowManager.Close();
    }

    internal void HandleWebEvent(WebLetter model)
    {
        Trace.WriteLine($"WebEvent名称：{model.Name} 参数数：{model.Args.Length}");

        var multi = model.Name.Split(':');
        if (multi.Length == 2)
        {
            Trace.WriteLine($"该WebEvent意图调用内置Api：{multi[0]}，需求：{multi[1]}");

            switch (multi[0])
            {
                case "AppProperty":
                    var property = Enum.Parse<KurisuAppProperty>(multi[1]);
                    Context.SetAppProperty(property, model.Args);
                    break;
                case "AppCommand":
                    var command = Enum.Parse<KurisuAppCommand>(multi[1]);
                    Context.ExecuteAppCommand(command, model.Args);
                    break;
                default:
                    Trace.WriteLine($"没有内置Api：{multi[0]}");
                    break;
            }
        }
        else Trace.WriteLine("该WebEvent为普通WebEvent");

        foreach (var hdl in WebEventHandlers)
        {
            if (model.Name != hdl.Key.EventName) continue;
            var constructors = hdl.Value.GetConstructors();
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                // TODO:以后要验证参数类型匹配情况
                // Int64阿弥诺斯
                if (model.Args.Length != parameters.Length) continue;
                try
                {
                    var command = (IWebEventHandler)constructor.Invoke(model.Args);
                    command.Handle(Context);
                }
                catch (Exception e)
                {
                    Trace.WriteLine(TextUtils.MakeErrorMsgText($"响应WebEvent {hdl.Key.EventName} 失败", e, true));
                    //TODO:向前端发送错误信息
                }

                return;
            }

            Trace.WriteLine($"WebEvent {hdl.Key.EventName} 的参数不对");
            return;
        }

        Trace.WriteLine($"找不到WebEvent {model.Name} 可用的Handler");
    }

    internal WebResponse RespondWebCall(WebLetter model)
    {
        Trace.WriteLine($"WebCall名称：{model.Name} 参数数：{model.Args.Length}");

        var multi = model.Name.Split(':');
        if (multi.Length == 2)
        {
            Trace.WriteLine($"该WebCall意图调用内置Api：{multi[0]}，需求：{multi[1]}");

            switch (multi[0])
            {
                case "AppProperty":
                    var property = Enum.Parse<KurisuAppProperty>(multi[1]);
                    var result = Context.GetAppProperty<object>(property);
                    return result == null
                        ? new(WebResponseState.Failure, $"找不到属性{property}")
                        : new(WebResponseState.Success, result);
                case "AppCommand":
                    var command = Enum.Parse<KurisuAppCommand>(multi[1]);
                    var retVal = Context.ExecuteAppCommand<object>(command, model.Args);
                    return retVal == null
                        ? new(WebResponseState.Failure, $"执行命令{command}失败")
                        : new(WebResponseState.Success, retVal);
                default:
                    Trace.WriteLine($"没有内置Api：{multi[0]}");
                    return new WebResponse(WebResponseState.Failure, $"没有内置Api：{multi[0]}");
            }
        }

        Trace.WriteLine("该WebCall为普通WebCall");

        foreach (var rpd in WebCallResponders)
        {
            if (model.Name != rpd.Key.EventName) continue;
            var constructors = rpd.Value.GetConstructors();
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                // TODO:以后要验证参数类型匹配情况
                // Int64阿弥诺斯
                if (model.Args.Length != parameters.Length) continue;
                try
                {
                    var command = (IWebCallResponder)constructor.Invoke(model.Args);

                    return command.Respond(Context);
                }
                catch (Exception e)
                {
                    var eText = TextUtils.MakeErrorMsgText($"执行WebCall {rpd.Key.EventName} 失败", e, true);
                    Trace.WriteLine(eText);
                    return new(WebResponseState.Failure, eText);
                }
            }

            var wrongArgs = $"WebCall {rpd.Key.EventName} 的参数不对";
            Trace.WriteLine(wrongArgs);
            return new(WebResponseState.Failure, wrongArgs);
        }

        var noSuchWebEvent = $"找不到WebCall {model.Name} 可用的Responder";
        Trace.WriteLine(noSuchWebEvent);
        return new(WebResponseState.Failure, noSuchWebEvent);
    }

    internal void EmitWebEvent(WebLetter model)
    {
        // TODO:可能需要验证、解耦（转文本 发送层）
        WindowManager.EmitWebEvent(model);
    }

    // TODO:呃呃
    internal void TriggerAppEvent(AppEvent appEvent)
    {
        Trace.WriteLine($"AppEvent类型：{appEvent.EventType} 参数数：{appEvent.Args.Length}");

        Context.EmitWebEvent(new WebLetter($"AppEvent:{appEvent.EventType}", appEvent.Args));

        foreach (var constructors in from hdl in AppEventHandlers
                 where appEvent.EventType == hdl.Key.EventType
                 select hdl.Value.GetConstructors())
        {
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                // TODO:以后要验证参数类型匹配情况
                if (appEvent.Args.Length != parameters.Length) continue;
                try
                {
                    // 为支持快速回调的
                    var command = (IAppEventHandler)constructor.Invoke(appEvent.Args);

                    command.Handle(Context);
                }
                catch (Exception e)
                {
                    Trace.WriteLine(TextUtils.MakeErrorMsgText($"执行AppEvent {appEvent.EventType} 失败", e, true));
                }

                return;
            }

            Trace.WriteLine($"AppEvent {appEvent.EventType} 的参数不对");
            return;
        }

        Trace.WriteLine($"找不到AppEvent {appEvent.EventType} 可用的Handler");
    }

    public static KurisuAppBuilder CreateBuilder(KurisuAppProfile profile) => new(profile);
}

public class KurisuAppBuilder
{
    private readonly KurisuAppProfile Profile;
    private readonly ArrayList Dependencies = [];
    private readonly Dictionary<WebEventHandlerAttribute, Type> WebEventHandlers = new();
    private readonly Dictionary<string, object> Stuffs = new();
    private readonly Dictionary<AppEventHandlerAttribute, Type> AppEventHandlers = new();
    private readonly Dictionary<WebCallResponderAttribute, Type> WebCallResponders = new();
    private IKurisuWindowManager? AppWindowBackend;
    private bool IsBuilt;

    internal KurisuAppBuilder(KurisuAppProfile profile)
    {
        Profile = profile;
    }

    public KurisuApp Build()
    {
        if (IsBuilt) throw new InvalidOperationException("Builder instance has already built a product");

        if (AppWindowBackend == null) throw new KurisuAppBuildingException("没有可使用的窗口后端");

        if (Profile.WebEventHandlerRegistrationStrategy ==
            WebEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically) ScanWebEventHandlers();
        if (Profile.AppEventHandlerRegistrationStrategy ==
            AppEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically) ScanAppEventHandlers();
        if (Profile.WebCallResponderRegistrationStrategy ==
            WebCallResponderRegistrationStrategy.ScanAndRegisterAutomatically) ScanWebCallResponders();

        KurisuApp application = new(Profile, Dependencies, WebEventHandlers, AppEventHandlers, WebCallResponders,
            AppWindowBackend);

        IsBuilt = true;

        return application;
    }

    public KurisuAppBuilder UseDefaultWindowBackend() => UseWindowBackend(new WpfKurisuWindowManager());

    public KurisuAppBuilder UseWindowBackend(IKurisuWindowManager windowManager) => this.Also(_ =>
    {
        if (AppWindowBackend != null) throw new InvalidOperationException("不允许重复使用窗口");
        AppWindowBackend = windowManager;
    });

    private void ScanWebEventHandlers()
    {
        var types = TypeUtils.GetAppAllTypes();
        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<WebEventHandlerAttribute>();
            if (attribute == null || !typeof(IWebEventHandler).IsAssignableFrom(type) ||
                !attribute.Scannable) continue;

            RegisterWebEventHandlerDirectly(attribute, type);
        }
    }

    private void ScanAppEventHandlers()
    {
        var types = TypeUtils.GetAppAllTypes();
        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<AppEventHandlerAttribute>();
            if (attribute != null && typeof(IAppEventHandler).IsAssignableFrom(type))
            {
                if (!attribute.Scannable) continue;

                RegisterAppEventHandlerDirectly(attribute, type);
            }
        }
    }

    private void ScanWebCallResponders()
    {
        var types = TypeUtils.GetAppAllTypes();
        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<WebCallResponderAttribute>();
            if (attribute != null && typeof(IWebCallResponder).IsAssignableFrom(type))
            {
                if (!attribute.Scannable) continue;

                RegisterWebCallResponderDirectly(attribute, type);
            }
        }
    }

    public KurisuAppBuilder RegisterWebEventHandler(WebEventHandlerAttribute handlerAttribute, Type handler) =>
        this.Also(_ =>
        {
            if (handlerAttribute == null || !typeof(IWebEventHandler).IsAssignableFrom(handler))
                throw new KurisuAppBuildingException("检查你注册处理器时提供的参数");

            RegisterWebEventHandlerDirectly(handlerAttribute, handler);
        });

    public KurisuAppBuilder RegisterAppEventHandler(AppEventHandlerAttribute handlerAttribute, Type handler) =>
        this.Also(_ =>
        {
            if (handlerAttribute == null || !typeof(IAppEventHandler).IsAssignableFrom(handler))
                throw new KurisuAppBuildingException("检查你注册处理器时提供的参数");

            RegisterAppEventHandlerDirectly(handlerAttribute, handler);
        });

    public KurisuAppBuilder RegisterWebCallResponder(WebCallResponderAttribute responderAttribute, Type handler) =>
        this.Also(_ =>
        {
            if (responderAttribute == null || !typeof(IWebCallResponder).IsAssignableFrom(handler))
                throw new KurisuAppBuildingException("检查你注册处理器时提供的参数");

            RegisterWebCallResponderDirectly(responderAttribute, handler);
        });

    private void RegisterWebEventHandlerDirectly(WebEventHandlerAttribute handlerAttribute, Type handler)
    {
        if (handlerAttribute.IsDev && !Profile.DebugMode) return;

        WebEventHandlers.Add(handlerAttribute, handler);
    }

    private void RegisterAppEventHandlerDirectly(AppEventHandlerAttribute handlerAttribute, Type handler)
    {
        if (handlerAttribute.IsDev && !Profile.DebugMode) return;

        AppEventHandlers.Add(handlerAttribute, handler);
    }

    private void RegisterWebCallResponderDirectly(WebCallResponderAttribute responderAttribute, Type handler)
    {
        if (responderAttribute.IsDev && !Profile.DebugMode) return;

        WebCallResponders.Add(responderAttribute, handler);
    }

    public KurisuAppBuilder ProvideDependency<T>() where T : new() => ProvideDependency(new T());

    public KurisuAppBuilder ProvideDependency<T>(T dependency) => this.Also(_ =>
    {
        if (Dependencies.OfType<T>().Any()) throw new KurisuAppBuildingException("已经提供过该依赖项了");
        Dependencies.Add(dependency);
    });

    public KurisuAppBuilder Provide<T>(string name, T stuff) => this.Also(_ => stuff.Ensure(it =>
    {
        Stuffs.Add(name, it!);
    }));
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
    WebEventHandlerRegistrationStrategy WebEventHandlerRegistrationStrategy =
        WebEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically,
    AppEventHandlerRegistrationStrategy AppEventHandlerRegistrationStrategy =
        AppEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically,
    WebCallResponderRegistrationStrategy WebCallResponderRegistrationStrategy =
        WebCallResponderRegistrationStrategy.ScanAndRegisterAutomatically,
    bool DebugStartUpWithDebugUrl = false,
    bool DebugAutomaticOpenDevTool = true,
    bool DebugMode = false
);

public class KurisuAppContext
{
    private readonly KurisuApp App;

    internal KurisuAppContext(KurisuApp app) => App = app;

    public T? Inject<T>() where T : class => LanguageExtensiveUtils.TryCatchingThenThrow("Cannot inject dependency",
        () => App.Dependencies.OfType<T>().First(),
        new Dictionary<Type, string> { { typeof(InvalidOperationException), "No such a dependency" } });

    // AppService
    public void EmitWebEvent(WebLetter model) => App.EmitWebEvent(model);


    public void SetAppProperty(KurisuAppProperty property, params object[] value)
    {
        switch (property)
        {
            case KurisuAppProperty.WindowState:
                App.WindowManager.SetWindowState((KurisuWindowState)value[0]);
                break;
            case KurisuAppProperty.WindowWidth:
                break;
            case KurisuAppProperty.WindowHeight:
                break;
            case KurisuAppProperty.WindowTitle:
                break;
            case KurisuAppProperty.WindowUrl:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, "没有合乎的属性");
        }
    }

    public T? GetAppProperty<T>(KurisuAppProperty property)
    {
        object? returnValue = null;

        switch (property)
        {
            case KurisuAppProperty.WindowState:
                returnValue = App.WindowManager.GetWindowState();
                break;
            case KurisuAppProperty.WindowWidth:

                break;
            case KurisuAppProperty.WindowHeight:
                break;
            case KurisuAppProperty.WindowTitle:
                break;
            case KurisuAppProperty.WindowUrl:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(property), property, "没有合乎的属性");
        }

        return (T?)returnValue;
    }

    public T? ExecuteAppCommand<T>(KurisuAppCommand command, params object[] modelArgs)
    {
        object? returnValue = null;

        switch (command)
        {
            case KurisuAppCommand.StopApp:
                App.Stop();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, "没有合乎的命令");
        }

        return (T?)returnValue;
    }

    public void ExecuteAppCommand(KurisuAppCommand command, params object[] modelArgs) =>
        ExecuteAppCommand<object>(command, modelArgs);
}

public enum KurisuAppProperty
{
    WindowState,
    WindowWidth,
    WindowHeight,
    WindowTitle,
    WindowUrl,
}

public enum KurisuAppCommand
{
    StopApp,
}

// TODO:解耦Browser（Window），以后支持WinUI3，并且Browser要实现EmitWebEvent的接口