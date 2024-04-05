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

namespace Me.EarzuChan.Ryo.WinWebAppSystem
{

    public class WinWebApp
    {
        internal readonly WinWebAppProfile Profile;
        internal readonly ArrayList Dependencies;
        internal readonly Dictionary<HandlerAttribute, Type> Handlers;
        internal readonly WinWebAppWindow WpfWindow;
        internal readonly WebView2 WebView;

        private readonly Application WpfApp;

        private readonly WinWebAppContext Context;

        internal WinWebApp(WinWebAppProfile profile, ArrayList dependencies, Dictionary<HandlerAttribute, Type> handlers)
        {
            Profile = profile;
            Dependencies = dependencies;
            Handlers = handlers;

            WpfApp = new Application();
            WebView = new WebView2();
            WpfWindow = new WinWebAppWindow(this);

            Context = new(this);
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
                        // 以后要验证参数类型匹配情况
                        if (model.Args.Length == parameters.Length)
                        {
                            try
                            {
                                var command = (IHandler)constructor.Invoke(model.Args);

                                command.Handle(Context);
                            }
                            catch (Exception e)
                            {
                                Trace.WriteLine(TextUtils.MakeErrorText("执行事件失败", e, true));
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

        public void Run() => WpfApp.Run(WpfWindow);

        public static WinWebAppBuilder CreateBuilder(WinWebAppProfile profile) => new(profile);

        public static WinWebAppBuilder CreateBuilder() => CreateBuilder(new());
    }

    public class WinWebAppBuilder
    {
        private readonly WinWebAppProfile Profile;
        private readonly ArrayList Dependencies = new();
        private readonly Dictionary<HandlerAttribute, Type> Handlers = new();
        private bool IsBuilt = false;

        internal WinWebAppBuilder(WinWebAppProfile profile)
        {
            Profile = profile;
        }

        public WinWebApp Build()
        {
            if (IsBuilt) throw new InvalidOperationException("Builder instance has already built a product");

            if (Profile.HandlerRegistrationStrategy == HandlerRegistrationStrategy.ScanAndRegisterAutomatically) ScanHandlers();

            WinWebApp application = new(Profile, Dependencies, Handlers);

            IsBuilt = true;

            return application;
        }

        private void ScanHandlers()
        {
            // Commands.Clear();

            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<HandlerAttribute>();
                if (attribute != null && typeof(IHandler).IsAssignableFrom(type))
                {
                    if (!attribute.Scannable) continue;

                    RegisterHandler(attribute, type);
                }
            }
        }

        public WinWebAppBuilder RegisterHandler(HandlerAttribute commandAttribute, Type command)
        {
            Handlers.Add(commandAttribute, command);

            return this;
        }

        public WinWebAppBuilder ProvideDependency<T>() where T : new()
        {
            //检测是否已经有T的实例
            if (!Dependencies.OfType<T>().Any()) Dependencies.Add(new T());

            return this;
        }
    }

    public class WinWebAppProfile
    {

        public string Name = "Ryo App";
        public string Version = "1.0.0.0";

        public int WindowWidth = 1200;
        public int WindowHeight = 800;

        public bool IsDev = false;

        public HandlerRegistrationStrategy HandlerRegistrationStrategy = HandlerRegistrationStrategy.ScanAndRegisterAutomatically;

        public bool WindowBorderless = true;
    }

    internal class WinWebAppWindow : Window
    {
        private readonly WinWebApp App;

        internal WinWebAppWindow(WinWebApp app)
        {
            App = app;

            Title = app.Profile.Name;
            Width = app.Profile.WindowWidth;
            Height = app.Profile.WindowHeight;

            Content = app.WebView;

            if (App.Profile.WindowBorderless) WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome()
            {
                ResizeBorderThickness = new Thickness(8),
                CaptionHeight = 0,
            });

            Loaded += InitWebView;
        }

        private async void InitWebView(object _, RoutedEventArgs __)
        {
            // 注册回调
            App.WebView.CoreWebView2InitializationCompleted += OnWebViewInited;

            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, null, new CoreWebView2EnvironmentOptions { AdditionalBrowserArguments = "--enable-features=msWebView2EnableDraggableRegions" });

            // 加载核心 
            await App.WebView.EnsureCoreWebView2Async(webView2Environment);

            // 加载资源
            // WebView.CoreWebView2.SetVirtualHostNameToFolderMapping("ryo_web_frontend", "WebResources", CoreWebView2HostResourceAccessKind.Deny);

            // TODO:FIX BABE
            App.WebView.CoreWebView2.Navigate(App.Profile.IsDev ? "http://127.0.0.1:5173/" : "https://ryo_web_frontend/index.html");
        }

        private void OnWebViewInited(object? _, CoreWebView2InitializationCompletedEventArgs __)
        {
            // 当浏览器加载好

            // 注入对象 互操作
            // MyWebView2.CoreWebView2.AddHostObjectToScript("handler", handler);
            // Trace.WriteLine(MassServer.GetMasses());

            if (App.Profile.IsDev) App.WebView.CoreWebView2.OpenDevToolsWindow();

            App.WebView.CoreWebView2.WebMessageReceived += (_, e) => App.HandleWebEvent(e.WebMessageAsJson);
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
            App.WebView.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(model));
        }
    }
}