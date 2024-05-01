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

namespace Me.EarzuChan.Ryo.WinWebAppSystem
{

    public class WinWebApp
    {
        internal readonly WinWebAppProfile Profile;
        internal readonly ArrayList Dependencies;
        internal readonly Dictionary<WebEventHandlerAttribute, Type> Handlers;
        internal readonly Dictionary<WinWebAppEvent, Action<object, WinWebAppContext>> AppEventListeners;
        internal readonly WinWebAppWindow WpfWindow; // 解耦这个
        internal readonly WebView2 WebView;

        private readonly Application WpfApp; // 解耦这个

        internal readonly WinWebAppContext Context;

        internal WinWebApp(WinWebAppProfile profile, ArrayList dependencies, Dictionary<WebEventHandlerAttribute, Type> handlers, Dictionary<WinWebAppEvent, Action<object, WinWebAppContext>> appEventListeners)
        {
            Profile = profile;
            Dependencies = dependencies;
            AppEventListeners = appEventListeners;

            Handlers = handlers;

            WpfApp = new Application();
            WebView = new WebView2();
            WpfWindow = new WinWebAppWindow(this);

            Context = new(this);
        }

        public void Run() => WpfApp.Run(WpfWindow);

        public void Stop()
        {
            WpfApp.Shutdown();
        }

        public void HandleWebEvent(string str) => HandleWebEvent(WebEventUtils.ParseWebEventJson(str));

        public void HandleWebEvent(WebEvent model)
        {
            Trace.WriteLine($"事件名称：{model.Name} 参数数：{model.Args.Length}");

            foreach (var hdl in Handlers)
            {
                if (model.Name == hdl.Key.EventName.ToLower())
                {
                    var constructors = hdl.Value.GetConstructors();
                    foreach (var constructor in constructors)
                    {
                        var parameters = constructor.GetParameters();
                        // TODO:以后要验证参数类型匹配情况
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

                                    EmitWebEvent(new() { Name = $"CallBack{hdl.Key.EventName}", Args = command.Handle(Context) });
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
            WebView.CoreWebView2.PostWebMessageAsJson(SerializationUtils.ToJson(model));
        }

        public static WinWebAppBuilder CreateBuilder(WinWebAppProfile profile) => new(profile);

        public static WinWebAppBuilder CreateBuilder() => CreateBuilder(new());
    }

    public class WinWebAppBuilder
    {
        private readonly WinWebAppProfile Profile;
        private readonly ArrayList Dependencies = new();
        private readonly Dictionary<WebEventHandlerAttribute, Type> WebEventHandlers = new();
        private readonly Dictionary<WinWebAppEvent, Action<object, WinWebAppContext>> AppEventListeners = new();
        private bool IsBuilt = false;

        internal WinWebAppBuilder(WinWebAppProfile profile)
        {
            Profile = profile;
        }

        public WinWebApp Build()
        {
            if (IsBuilt) throw new InvalidOperationException("Builder instance has already built a product");

            if (Profile.WebEventHandlerRegistrationStrategy == WebEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically) ScanWebEventHandlers();

            WinWebApp application = new(Profile, Dependencies, WebEventHandlers, AppEventListeners);

            IsBuilt = true;

            return application;
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


        public WinWebAppBuilder AddAppEventHandler(WinWebAppEvent appEvent, Action<object, WinWebAppContext> listener)
        {
            AppEventListeners.Add(appEvent, listener);

            return this;
        }
    }

    public enum WinWebAppEvent
    {
        AppMaximizationChanged
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
        public WebEventHandlerRegistrationStrategy WebEventHandlerRegistrationStrategy { get; init; } = WebEventHandlerRegistrationStrategy.ScanAndRegisterAutomatically;

        // Debug Profile
        public bool DebugStartUpWithDebugUrl { get; init; } = true;
        public bool DebugAutomaticOpenDevTool { get; init; } = true;
        public string DebugStartUpUrl { get; init; } = "http://localhost:5173/";
        public bool DebugMode { get; init; } = false;
    }

    internal class WinWebAppWindow : Window
    {
        private readonly WinWebApp App;

        internal WinWebAppWindow(WinWebApp app)
        {
            App = app;

            Title = App.Profile.Name;
            Width = App.Profile.WindowWidth;
            Height = App.Profile.WindowHeight;
            if (App.Profile.UseIcon) Icon = new BitmapImage(new Uri(App.Profile.Icon, UriKind.RelativeOrAbsolute));

            Content = App.WebView;

            if (App.Profile.WindowBorderless) WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome()
            {
                ResizeBorderThickness = new Thickness(8),
                CaptionHeight = 0,
            });

            StateChanged += OnStateChanged;
            Loaded += InitWebView;
            App.WebView.CoreWebView2InitializationCompleted += InitWebApp;
        }

        private void OnStateChanged(object? _, EventArgs __)
        {
            App.AppEventListeners[WinWebAppEvent.AppMaximizationChanged](WindowState == WindowState.Maximized, App.Context);
        }

        private async void InitWebView(object _, RoutedEventArgs __)
        {
            // 当窗口加载好

            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, null, new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = "--enable-features=msWebView2EnableDraggableRegions" });

            // 加载核心 
            await App.WebView.EnsureCoreWebView2Async(webView2Environment);
        }

        private void InitWebApp(object? _, CoreWebView2InitializationCompletedEventArgs __)
        {
            // 当浏览器加载好

            // 资源拨弄成虚拟主机
            App.WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(App.Profile.VirtualHostNameName, App.Profile.WebResourcePath, CoreWebView2HostResourceAccessKind.Deny);

            // TODO:FIX BABE
            App.WebView.CoreWebView2.Navigate(App.Profile.DebugMode && App.Profile.DebugStartUpWithDebugUrl ? App.Profile.DebugStartUpUrl : App.Profile.StartUpUrl);

            // 提供对象 互操作
            // MyWebView2.CoreWebView2.AddHostObjectToScript("handler", handler);
            // Trace.WriteLine(MassServer.GetMasses());
            // 其实在Js侧写个包也可以做到"Request"，还需要提供专门的Request接口吗

            // 回调接受消息
            App.WebView.CoreWebView2.WebMessageReceived += (_, e) => App.HandleWebEvent(e.WebMessageAsJson);

            // 打开控制台
            if (App.Profile.DebugMode && App.Profile.DebugAutomaticOpenDevTool) App.WebView.CoreWebView2.OpenDevToolsWindow();
        }
    }

    public class WinWebAppContext
    {
        private readonly WinWebApp App;

        internal WinWebAppContext(WinWebApp app) => App = app;

        public T Inject<T>() where T : class =>
            ControlFlowUtils.TryCatchingThenThrow<T>("Cannot inject dependency", () => App.Dependencies.OfType<T>().First(), new Dictionary<Type, string> { { typeof(InvalidOperationException), "No such a dependency" } });

        public void EmitWebEvent(WebEvent model)
        {
            // TODO:Unstable Babe
            App.EmitWebEvent(model);
        }

        public bool IsAppMaximized => App.WpfWindow.WindowState == WindowState.Maximized;

        public void SetAppMaximized(bool value)
        {
            /*if (App.WpfWindow.WindowState != WindowState.Minimized)*/
            App.WpfWindow.WindowState = value ? WindowState.Maximized : WindowState.Normal;
        }

        public void MinimizeApp() => App.WpfWindow.WindowState = WindowState.Minimized;

        public void StopApp() => App.Stop();
    }

    // TODO:解耦Browser（Window），以后支持WinUI3，并且Browser要实现EmitWebEvent的接口
}