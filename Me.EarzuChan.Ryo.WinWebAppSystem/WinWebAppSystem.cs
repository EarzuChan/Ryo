using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows;
using System.Windows.Shell;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Me.EarzuChan.Ryo.Utils;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents.Handlers;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents;
using System.Xml.Linq;
using Me.EarzuChan.Ryo.WinWebAppSystem.Utils;
using System.Runtime.InteropServices;
using Me.EarzuChan.Ryo.WinWebAppSystem.Exceptions;
using Me.EarzuChan.Ryo.Extensions.Utils;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Me.EarzuChan.Ryo.WinWebAppSystem.Windows;

namespace Me.EarzuChan.Ryo.WinWebAppSystem
{

    public class WinWebApp
    {
        internal readonly WinWebAppProfile Profile;
        internal readonly ArrayList Dependencies;
        internal readonly Dictionary<WebEventHandlerAttribute, Type> Handlers;
        internal readonly Dictionary<WinWebAppEvent, Action<WinWebAppContext, object?>> AppEventListeners;
        internal readonly IWinWebAppWindowBackend AppWindowBackend;

        private readonly WinWebAppContext Context;

        internal WinWebApp(WinWebAppProfile profile, ArrayList dependencies, Dictionary<WebEventHandlerAttribute, Type> handlers, Dictionary<WinWebAppEvent, Action<WinWebAppContext, object?>> appEventListeners, IWinWebAppWindowBackend appWindow)
        {
            Profile = profile;
            Dependencies = dependencies;
            AppEventListeners = appEventListeners;

            Handlers = handlers;

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

        public void HandleWebEvent(string str) => HandleWebEvent(WebEventUtils.ParseWebEventJson(str));

        public void HandleWebEvent(WebEvent model)
        {
            Trace.WriteLine($"事件名称：{model.Name} 参数数：{model.Args.Length}");

            foreach (var hdl in Handlers)
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
                                Trace.WriteLine(TextUtils.MakeErrorMsgText("执行事件失败", e, true));
                            }
                            return;
                        }
                    }
                    Trace.WriteLine($"事件\"{hdl.Key.EventName}\"的参数不对");
                    return;
                }
            }
            Trace.WriteLine($"找不到事件\"{model.Name}\"可用的Handler");
        }

        public void EmitWebEvent(WebEvent model)
        {
            // TODO:可能需要验证、解耦（转文本 发送层）
            AppWindowBackend.EmitWebEvent(model);
        }

        internal void TriggerAppEvent(WinWebAppEvent appEvent, object? arg = null)
        {
            if (!AppEventListeners.ContainsKey(appEvent)) return;

            AppEventListeners[appEvent].Invoke(Context, arg);

            // TODO:可能需要弄一个数据类型，允许一事件多监听器挂载
        }

        public static WinWebAppBuilder CreateBuilder(WinWebAppProfile profile) => new(profile);

        public static WinWebAppBuilder CreateBuilder() => CreateBuilder(new());
    }

    public class WinWebAppBuilder
    {
        private readonly WinWebAppProfile Profile;
        private readonly ArrayList Dependencies = new();
        private readonly Dictionary<WebEventHandlerAttribute, Type> WebEventHandlers = new();
        private readonly Dictionary<WinWebAppEvent, Action<WinWebAppContext, object?>> AppEventListeners = new();
        private IWinWebAppWindowBackend? AppWindowBackend = null;
        private bool IsBuilt = false;

        internal WinWebAppBuilder(WinWebAppProfile profile)
        {
            Profile = profile;
        }

        public WinWebApp Build()
        {
            if (IsBuilt) throw new InvalidOperationException("Builder instance has already built a product");

            if (AppWindowBackend == null) throw new WinWebAppBuildingException("没有可使用的窗口后端");

            if (Profile.WebEventHandlerRegistrationStrategy == WebEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically) ScanWebEventHandlers();

            WinWebApp application = new(Profile, Dependencies, WebEventHandlers, AppEventListeners, AppWindowBackend);

            IsBuilt = true;

            return application;
        }

        public WinWebAppBuilder UseDefaultWindowBackend() => UseWindowBackend(new WinWebAppWpfWindowBackend());

        public WinWebAppBuilder UseWindowBackend(IWinWebAppWindowBackend windowBackend)
        {
            if (AppWindowBackend != null) throw new InvalidOperationException("不允许重复使用窗口");
            AppWindowBackend = windowBackend;
            return this;
        }

        private void ScanWebEventHandlers()
        {
            // Commands.Clear();

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

        public WinWebAppBuilder RegisterWebEventHandler(WebEventHandlerAttribute handlerAttribute, Type handler)
        {
            if (handlerAttribute == null || (!typeof(IWebEventHandler).IsAssignableFrom(handler) && typeof(IWebEventHandlerForCallBack).IsAssignableFrom(handler))) throw new WinWebAppBuildingException("检查你注册处理器时提供的参数");

            RegisterWebEventHandlerDirectly(handlerAttribute, handler);

            return this;
        }

        private void RegisterWebEventHandlerDirectly(WebEventHandlerAttribute handlerAttribute, Type handler)
        {
            if (handlerAttribute.IsDev && !Profile.DebugMode) return;

            WebEventHandlers.Add(handlerAttribute, handler);
        }

        public WinWebAppBuilder ProvideDependency<T>() where T : new()
        {
            //检测是否已经有T的实例
            if (!Dependencies.OfType<T>().Any()) Dependencies.Add(new T());

            return this;
        }


        public WinWebAppBuilder RegisterAppEventListener(WinWebAppEvent appEvent, Action<WinWebAppContext, object?> listener) // TODO:想起Immediately
        {
            AppEventListeners.Add(appEvent, listener);

            return this;
        }
    }

    public enum WinWebAppEvent
    {
        AppWindowStateChanged
    }

    public class WinWebAppProfile
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
        public WinWebAppWindowState StartUpWindowState { get; init; } = WinWebAppWindowState.Normal;
        public WebEventHandlerRegistrationStrategy WebEventHandlerRegistrationStrategy { get; init; } = WebEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically;

        // Debug Profile
        public bool DebugStartUpWithDebugUrl { get; init; } = true;
        public bool DebugAutomaticOpenDevTool { get; init; } = true;
        public string DebugStartUpUrl { get; init; } = "http://localhost:5173/";
        public bool DebugMode { get; init; } = false;
    }

    public class WinWebAppContext
    {
        private readonly WinWebApp App;

        internal WinWebAppContext(WinWebApp app) => App = app;

        public T Inject<T>() where T : class =>
            ControlFlowUtils.TryCatchingThenThrow<T>("Cannot inject dependency", () => App.Dependencies.OfType<T>().First(), new Dictionary<Type, string> { { typeof(InvalidOperationException), "No such a dependency" } });

        // AppService
        public void EmitWebEvent(WebEvent model)
        {
            // TODO:Unstable Babe
            App.EmitWebEvent(model);
        }

        public void StopApp() => App.Stop();

        // AppWindowService
        public void SetAppWindowState(WinWebAppWindowState state) => App.AppWindowBackend.SetWindowState(state);
    }

    // TODO:解耦Browser（Window），以后支持WinUI3，并且Browser要实现EmitWebEvent的接口
}