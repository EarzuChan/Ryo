using Me.EarzuChan.Ryo.Core.Adaptions;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Utils;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection;
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
using Path = System.IO.Path;
using Me.EarzuChan.Ryo.Exceptions.FileExceptions;

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

            // FIXME:窗口白条问题

            InitWebView();
        }

        private async void InitWebView()
        {
            // 注册回调
            MyWebView2.CoreWebView2InitializationCompleted += OnWebViewInited;

            // 加载核心
            await MyWebView2.EnsureCoreWebView2Async();

            // 加载资源
            MyWebView2.CoreWebView2.SetVirtualHostNameToFolderMapping("ryo_web_frontend", "WebResources", CoreWebView2HostResourceAccessKind.Deny);
            MyWebView2.CoreWebView2.Navigate("https://ryo_web_frontend/index.html");

#if DEBUG
            MyWebView2.CoreWebView2.Navigate("http://127.0.0.1:5173/");
#endif
        }

        private void OnWebViewInited(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            // 当浏览器加载好

            // 注入对象 互操作
            MyWebView2.CoreWebView2.AddHostObjectToScript("massServer", MassServer);
            // Trace.WriteLine(MassServer.GetMasses());

#if DEBUG
            // 打开开发者工具
            MyWebView2.CoreWebView2.OpenDevToolsWindow();
#endif
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e) => DragMove();

    }

    public class MassServer
    {
        public class FileBean
        {
            [JsonProperty("label")]
            public string Name;

            [JsonProperty("children")]
            public ItemBean[] Items;

            public FileBean(string name, ItemBean[] items)
            {
                Name = name;
                Items = items;
            }
        }

        public class ItemBean
        {
            [JsonIgnore]
            public int Id;

            [JsonProperty("label")]
            public string Name;

            public ItemBean(int id, string name)
            {
                Id = id;
                Name = name;
            }
        }

        public MassManager MassManager = new();

        // public MassServer() => LogUtils.PrintInfo("MassServer Inited");

        public string GetFileTreeData() => FormatUtils.NewtonsoftItemToJson(MassManager.MassFiles.Select(oneMass => new FileBean(oneMass.Key, oneMass.Value.IdStrPairs.Select(pair => new ItemBean(pair.Value, pair.Key/*, oneMass.Value.ItemAdaptions[oneMass.Value.ItemBlobs[pair.Value].AdaptionId].DataJavaClz, oneMass.Value.ItemAdaptions[oneMass.Value.ItemBlobs[pair.Value].AdaptionId].AdapterJavaClz*/)).ToArray())).ToArray());

        public void OpenFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "Open MassFile (.fs)",
                Filter = "MassFile|*.fs",
                FileName = string.Empty,
                FilterIndex = 1,
                Multiselect = false,
                RestoreDirectory = true,
                DefaultExt = "fs"
            };
            if (openFileDialog.ShowDialog() == false) return;

            MassManager.LoadMassFile(openFileDialog.FileName, Path.GetFileNameWithoutExtension(openFileDialog.FileName));
        }

        public void NewFile() => MassManager.MassFiles.Add($"file {MassManager.MassFiles.Count + 1}", new MassFile());

        public void NewFile(string fileName) => MassManager.MassFiles.Add(fileName, new MassFile());

        public void ExitApp() => Environment.Exit(0);

        public string GetItemData(string fileName, string itemName)
        {
            try
            {
                var mass = MassManager.MassFiles[fileName];

                return FormatUtils.NewtonsoftItemToJson(mass.Get<object>(mass.IdStrPairs[itemName]));
            }
            catch (Exception ex)
            {
                return $"{{\"error_getting\":\"{ex.Message}\"}}";
            }
        }

        // TODO:十分甚至九分不稳健 能跑进行
        public void SetItemData(string fileName, string itemName, string json)
        {
            try
            {
                Trace.WriteLine($"文件：{fileName} 项目：{itemName} Json：{json}");

                var mass = MassManager.MassFiles[fileName];

                var itemBlob = mass.ItemBlobs[mass.IdStrPairs[itemName]];

                var itemAdaption = mass.ItemAdaptions[itemBlob.AdaptionId];

                var dataRyoType = AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(itemAdaption.DataJavaClz);

                Trace.WriteLine(dataRyoType);

                MethodInfo tempMethod = typeof(FormatUtils).GetMethod("NewtonsoftJsonToItem").MakeGenericMethod(dataRyoType.BaseType);
                object value = tempMethod.Invoke(null, new[] { json });

                mass.Add(itemName, value);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"设文件项目爆了：\n{ex}");
            }
        }

        public void SaveFile(string fileName)
        {
            try
            {
                ControlFlowUtils.TryCatchingThenThrow("Cannot save object", () =>
                {
                    SaveFileDialog saveFileDialog = new()
                    {
                        Title = "Save MassFile (.fs)",
                        Filter = "MassFile|*.fs",
                        FileName = string.Empty,
                        FilterIndex = 1,
                        RestoreDirectory = true,
                        DefaultExt = "fs"
                    };
                    if (saveFileDialog.ShowDialog() == false) throw new Exception("未选取文件");

                    var mass = MassManager.GetMassFileOrThrow(fileName);

                    using var fileStream = FileUtils.OpenFile(saveFileDialog.FileName, true, true);
                    mass.Save(fileStream);
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"保存文件爆了：\n{ex}");
            }
        }

        public void CloseFile(string fileName)
        {
            try
            {
                if (MassManager.ExistsMass(fileName)) MassManager.UnloadMassFile(fileName);
                else throw new NoSuchFileException(fileName);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"关闭文件爆了：\n{ex}");
            }
        }
    }
}
