using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using Me.EarzuChan.Ryo.Utils;
using System.Diagnostics;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.AppEvents.Handlers;
using Me.EarzuChan.Ryo.Kurisu.Exceptions;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Kurisu.WebCalls.Responders;
using Me.EarzuChan.Ryo.Kurisu.WebEvents.Handlers;
using Me.EarzuChan.Ryo.Kurisu.WindowBackends;

namespace Me.EarzuChan.Ryo.Kurisu
{
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
        internal readonly IKurisuAppWindowBackend AppWindowBackend;

        private readonly KurisuAppContext Context;

        internal KurisuApp(KurisuAppProfile profile, ArrayList dependencies, Dictionary<WebEventHandlerAttribute, Type> webEventHandlers, Dictionary<AppEventHandlerAttribute, Type> appEventHandlers, Dictionary<WebCallResponderAttribute, Type> webCallResponders, IKurisuAppWindowBackend appWindow)
        {
            Profile = profile;
            Dependencies = dependencies;

            WebEventHandlers = webEventHandlers;
            AppEventHandlers = appEventHandlers;
            WebCallResponders = webCallResponders;

            appWindow.Init(this);
            AppWindowBackend = appWindow;

            Context = new(this);
        }

        public void Run()
        {
            AppWindowBackend.Show();
        }

        public void Stop()
        {
            AppWindowBackend.Close();
        }

        internal void HandleWebEvent(WebLetter model)
        {
            Trace.WriteLine($"WebEvent名称：{model.Name} 参数数：{model.Args.Length}");

            foreach (var hdl in WebEventHandlers)
            {
                if (model.Name == hdl.Key.EventName)
                {
                    var constructors = hdl.Value.GetConstructors();
                    foreach (var constructor in constructors)
                    {
                        var parameters = constructor.GetParameters();
                        // TODO:以后要验证参数类型匹配情况
                        // Int64阿弥诺斯
                        if (model.Args.Length == parameters.Length)
                        {
                            try
                            {
                                // 为支持快速回调的
                                if (typeof(IWebEventHandler).IsAssignableFrom(hdl.Value))
                                {
                                    var command = (IWebEventHandler)constructor.Invoke(model.Args);

                                    command.Handle(Context);
                                }
                                else
                                {
                                    var command = (IWebEventHandlerForCallBack)constructor.Invoke(model.Args);

                                    EmitWebEvent(new($"CallBack{hdl.Key.EventName}", command.Handle(Context)));
                                }
                            }
                            catch (Exception e)
                            {
                                Trace.WriteLine(TextUtils.MakeErrorMsgText($"执行WebEvent {hdl.Key.EventName} 失败", e, true));
                            }
                            return;
                        }
                    }
                    Trace.WriteLine($"WebEvent {hdl.Key.EventName} 的参数不对");
                    return;
                }
            }
            Trace.WriteLine($"找不到WebEvent {model.Name} 可用的Handler");
        }

        internal WebResponse RespondWebCall(WebLetter model)
        {
            Trace.WriteLine($"WebCall名称：{model.Name} 参数数：{model.Args.Length}");

            foreach (var rpd in WebCallResponders)
            {
                if (model.Name == rpd.Key.EventName)
                {
                    var constructors = rpd.Value.GetConstructors();
                    foreach (var constructor in constructors)
                    {
                        var parameters = constructor.GetParameters();
                        // TODO:以后要验证参数类型匹配情况
                        // Int64阿弥诺斯
                        if (model.Args.Length == parameters.Length)
                        {
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
                    }

                    var wrongArgs = $"WebCall {rpd.Key.EventName} 的参数不对";
                    Trace.WriteLine(wrongArgs);
                    return new(WebResponseState.Failure, wrongArgs);
                }
            }

            var noSuchWebEvent = $"找不到WebCall {model.Name} 可用的Responder";
            Trace.WriteLine(noSuchWebEvent);
            return new(WebResponseState.Failure, noSuchWebEvent);
        }

        internal void EmitWebEvent(WebLetter model)
        {
            // TODO:可能需要验证、解耦（转文本 发送层）
            AppWindowBackend.EmitWebEvent(model);
        }

        internal void TriggerAppEvent(AppEvent appEvent)
        {
            Trace.WriteLine($"AppEvent类型：{appEvent.EventType} 参数数：{appEvent.Args.Length}");

            foreach (var hdl in AppEventHandlers)
            {
                if (appEvent.EventType == hdl.Key.EventType)
                {
                    var constructors = hdl.Value.GetConstructors();
                    foreach (var constructor in constructors)
                    {
                        var parameters = constructor.GetParameters();
                        // TODO:以后要验证参数类型匹配情况
                        if (appEvent.Args.Length == parameters.Length)
                        {
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
                    }
                    Trace.WriteLine($"AppEvent {appEvent.EventType} 的参数不对");
                    return;
                }
            }
            Trace.WriteLine($"找不到AppEvent {appEvent.EventType} 可用的Handler");
        }

        public static KurisuAppBuilder CreateBuilder(KurisuAppProfile profile) => new(profile);

        public static KurisuAppBuilder CreateBuilder() => CreateBuilder(new());
    }

    public class KurisuAppBuilder
    {
        private readonly KurisuAppProfile Profile;
        private readonly ArrayList Dependencies = new();
        private readonly Dictionary<WebEventHandlerAttribute, Type> WebEventHandlers = new();
        private readonly Dictionary<AppEventHandlerAttribute, Type> AppEventHandlers = new();
        private readonly Dictionary<WebCallResponderAttribute, Type> WebCallResponders = new();
        private IKurisuAppWindowBackend? AppWindowBackend = null;
        private bool IsBuilt = false;

        internal KurisuAppBuilder(KurisuAppProfile profile)
        {
            Profile = profile;
        }

        public KurisuApp Build()
        {
            if (IsBuilt) throw new InvalidOperationException("Builder instance has already built a product");

            if (AppWindowBackend == null) throw new KurisuAppBuildingException("没有可使用的窗口后端");

            if (Profile.WebEventHandlerRegistrationStrategy == WebEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically) ScanWebEventHandlers();
            if (Profile.AppEventHandlerRegistrationStrategy == AppEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically) ScanAppEventHandlers();
            if (Profile.WebCallResponderRegistrationStrategy == WebCallResponderRegistrationStrategy.ScanAndRegisterAutomatically) ScanWebCallResponders();

            KurisuApp application = new(Profile, Dependencies, WebEventHandlers, AppEventHandlers, WebCallResponders, AppWindowBackend);

            IsBuilt = true;

            return application;
        }

        public KurisuAppBuilder UseDefaultWindowBackend() => UseWindowBackend(new KurisuAppWpfWindowBackend());

        public KurisuAppBuilder UseWindowBackend(IKurisuAppWindowBackend windowBackend)
        {
            if (AppWindowBackend != null) throw new InvalidOperationException("不允许重复使用窗口");
            AppWindowBackend = windowBackend;
            return this;
        }

        private void ScanWebEventHandlers()
        {
            var types = TypeUtils.GetAppAllTypes();
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<WebEventHandlerAttribute>();
                if (attribute != null && (typeof(IWebEventHandler).IsAssignableFrom(type) || typeof(IWebEventHandlerForCallBack).IsAssignableFrom(type)))
                {
                    if (!attribute.Scannable) continue;

                    RegisterWebEventHandlerDirectly(attribute, type);
                }
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

        public KurisuAppBuilder RegisterWebEventHandler(WebEventHandlerAttribute handlerAttribute, Type handler)
        {
            if (handlerAttribute == null || (!typeof(IWebEventHandler).IsAssignableFrom(handler) && typeof(IWebEventHandlerForCallBack).IsAssignableFrom(handler))) throw new KurisuAppBuildingException("检查你注册处理器时提供的参数");

            RegisterWebEventHandlerDirectly(handlerAttribute, handler);

            return this;
        }

        public KurisuAppBuilder RegisterAppEventHandler(AppEventHandlerAttribute handlerAttribute, Type handler)
        {
            if (handlerAttribute == null || !typeof(IAppEventHandler).IsAssignableFrom(handler)) throw new KurisuAppBuildingException("检查你注册处理器时提供的参数");

            RegisterAppEventHandlerDirectly(handlerAttribute, handler);

            return this;
        }

        public KurisuAppBuilder RegisterWebCallResponder(WebCallResponderAttribute responderAttribute, Type handler)
        {
            if (responderAttribute == null || !typeof(IWebCallResponder).IsAssignableFrom(handler)) throw new KurisuAppBuildingException("检查你注册处理器时提供的参数");

            RegisterWebCallResponderDirectly(responderAttribute, handler);

            return this;
        }

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

        public KurisuAppBuilder ProvideDependency<T>() where T : new()
        {
            //检测是否已经有T的实例
            if (Dependencies.OfType<T>().Any()) throw new KurisuAppBuildingException($"已经提供过{typeof(T)}类型的依赖项了");
            Dependencies.Add(new T());

            return this;
        }
    }

    public class KurisuAppProfile
    {
        // Basic Profile
        public string Name { get; init; } = "Ryo App";
        public string Icon { get; init; } = "AppResources/icon_ryo_app.ico";
        public bool UseIcon { get; init; } = true;
        public string Version { get; init; } = "1.0.0.0";
        public string VirtualHostNameName { get; init; } = "ryo_web_frontend";
        public string StartUpUrl { get; init; } = "https://ryo_web_frontend/index.html";
        public string WebResourcePath { get; init; } = "WebResources";
        public bool WindowBorderless { get; init; } = true;
        public int WindowWidth { get; init; } = 1200;
        public int WindowHeight { get; init; } = 800;
        public KurisuAppWindowState StartUpWindowState { get; init; } = KurisuAppWindowState.Normal;
        public WebEventHandlerRegistrationStrategy WebEventHandlerRegistrationStrategy { get; init; } = WebEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically;
        public AppEventHandlerRegistrationStrategy AppEventHandlerRegistrationStrategy { get; init; } = AppEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically;
        public WebCallResponderRegistrationStrategy WebCallResponderRegistrationStrategy { get; init; } = WebCallResponderRegistrationStrategy.ScanAndRegisterAutomatically;

        // Debug Profile
        public bool DebugStartUpWithDebugUrl { get; init; } = true;
        public bool DebugAutomaticOpenDevTool { get; init; } = true;
        public string DebugStartUpUrl { get; init; } = "http://localhost:5173/";
        public bool DebugMode { get; init; } = false;
    }

    public class KurisuAppContext
    {
        private readonly KurisuApp App;

        internal KurisuAppContext(KurisuApp app) => App = app;

        public T Inject<T>() where T : class =>
            ControlFlowUtils.TryCatchingThenThrow<T>("Cannot inject dependency", () => App.Dependencies.OfType<T>().First(), new Dictionary<Type, string> { { typeof(InvalidOperationException), "No such a dependency" } });

        // AppService
        public void EmitWebEvent(WebLetter model)
        {
            // TODO:Unstable Babe
            App.EmitWebEvent(model);
        }

        public void StopApp() => App.Stop();

        // AppWindowService
        public void SetAppWindowState(KurisuAppWindowState state) => App.AppWindowBackend.SetWindowState(state);

        public KurisuAppWindowState GetAppWindowState() => App.AppWindowBackend.GetWindowState();
    }

    // TODO:解耦Browser（Window），以后支持WinUI3，并且Browser要实现EmitWebEvent的接口
}