using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Utils;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Me.EarzuChan.Ryo.DesktopTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class WPFWindow : Window
    {
        public MassServer MassServer = new();

        public WPFWindow()
        {
            InitializeComponent();

            MassServer.MassManager.LoadMassFile("D:\\A Sources\\WeakPipeRecovery\\assets\\content.fs", "Content");

            InitWebView();
        }

        private async void InitWebView()
        {
            MyWebView2.CoreWebView2InitializationCompleted += OnWebViewInited;

            // 加载

            await MyWebView2.EnsureCoreWebView2Async();
            // var text = File.ReadAllText("WebResources/index.html");
            // MyWebView2.NavigateToString(text);

            MyWebView2.CoreWebView2.SetVirtualHostNameToFolderMapping("genshin.launch", "../../../WebResources", CoreWebView2HostResourceAccessKind.Deny);
            //导航
            MyWebView2.CoreWebView2.Navigate("https://genshin.launch/index.html");
        }

        private void OnWebViewInited(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            // 注入对象 互操作
            MyWebView2.CoreWebView2.AddHostObjectToScript("massServer", MassServer);
            Trace.WriteLine(MassServer.GetMasses());

            // 每当页面加载好
            /*MyWebView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
 """
const MassServer = window.chrome.webview.hostObjects.MassServer

(async () => {
    // 获取文本列表
    const textList = await MassServer.GetMasses()

    // 创建列表DOM
    const listContainer = document.createElement("ul")
    listContainer.style.backgroundColor = "black"
    listContainer.style.listStyle = "none"
    listContainer.style.padding = "10px"
    document.body.appendChild(listContainer)

    // 循环创建DOM项
    textList.forEach(text => {
        const listItem = document.createElement("li")
        listItem.style.color = "white"
        listItem.textContent = text
        listContainer.appendChild(listItem)
    })
})()
""");*/

            // 打开开发者工具
            MyWebView2.CoreWebView2.OpenDevToolsWindow();
        }
    }

    public class MassServer
    {
        public class MassBean
        {
            [JsonProperty("label")]
            public string Name;

            [JsonProperty("children")]
            public MassItemBean[] Items;

            public MassBean(string name, MassItemBean[] items)
            {
                Name = name;
                Items = items;
            }
        }

        public class MassItemBean
        {
            [JsonIgnore]// [JsonProperty("id")]
            public int Id;

            [JsonProperty("label")]
            public string Name;

            [JsonProperty("type")]
            public string Type;

            [JsonIgnore]// [JsonProperty("adapter_type")]
            public string AdapterType;

            public MassItemBean(int id, string name, string type, string adapterType)
            {
                Id = id;
                Name = name;
                Type = type;
                AdapterType = adapterType;
            }
        }

        public MassManager MassManager = new();

        public MassServer() => LogUtils.PrintInfo("MassServer Inited");

        /*public string DoSth(string text)
        {
            var a = $"Hello, {text}";

            Trace.WriteLine(a);

            return a;
        }*/

        public string GetMasses() => FormatUtils.NewtonsoftItemToJson(MassManager.MassFiles.Select(oneMass => new MassBean(oneMass.Key, oneMass.Value.IdStrPairs.Select(pair => new MassItemBean(pair.Value, pair.Key, oneMass.Value.ItemAdaptions[oneMass.Value.ItemBlobs[pair.Value].AdaptionId].DataJavaClz, oneMass.Value.ItemAdaptions[oneMass.Value.ItemBlobs[pair.Value].AdaptionId].AdapterJavaClz)).ToArray())).ToArray());
    }
}
