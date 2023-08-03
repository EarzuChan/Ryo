using Me.EarzuChan.Ryo.Formations;
using Me.EarzuChan.Ryo.Masses;
using Me.EarzuChan.Ryo.Utils;
using Me.EarzuChan.Ryo.WPFImageConverter.Langs;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Image = SixLabors.ImageSharp.Image;
using Path = System.IO.Path;

namespace Me.EarzuChan.Ryo.WPFImageConverter
{
    namespace Langs
    {
        public interface ILang
        {
            string ByName(string name);
        }
        public static class LangHelper
        {
            public static ILang? GetForeignLang()
            {
                Trace.WriteLine("中文：" + IsChineseLanguage());
                if (IsChineseLanguage()) return null;
                return new EnglishLang();
            }

            public static bool IsChineseLanguage()
            {
#if DEBUG
                return false;
#endif
                CultureInfo culture = Thread.CurrentThread.CurrentCulture;
                return culture.Name.StartsWith("zh-");
            }
        }

        public class EnglishLang : ILang
        {
            public string ByName(string name)
            {
                return name switch
                {
                    "tx2img" => "To Image",
                    "img2tx" => "To Resource",
                    "open_saved_path" => "Open Folder",
                    "override_texture_bak" => "Override Bak",
                    "open" => "Open",
                    "save" => "Save",
                    _ => name,
                };
            }
        }
    }


    public partial class MainWindow : Window
    {
        public static MainWindow INSTANCE;

        private ModeBase _currentMode;
        public bool OpenSavedPath = true;
        public bool OverrideTextureBak = true;

        public MainWindow()
        {
            InitializeComponent();

            // 默认 Mode
            _currentMode = new Mode2Img();

            var lang = LangHelper.GetForeignLang();
            if (lang != null)
            {
                RadioA.Content = lang.ByName("tx2img");
                RadioB.Content = lang.ByName("img2tx");
                OpenSavedPathCheckBox.Content = lang.ByName("open_saved_path");
                OverrideTextureBakCheckBox.Content = lang.ByName("override_texture_bak");
                OpenButton.Content = lang.ByName("open");
                SaveButton.Content = lang.ByName("save");
            }

            INSTANCE = this;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ImageView.Source = null;

            if (sender == RadioA)
                _currentMode = new Mode2Img();
            else if (sender == RadioB)
                _currentMode = new Mode2Tx();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentMode.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "打开错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentMode.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "保存错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowImage(Image img)
        {
            using var memoryStream = new MemoryStream();
            img.Save(memoryStream, new PngEncoder());
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            ImageView.Source = bitmapImage;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == OverrideTextureBakCheckBox) OverrideTextureBak = OverrideTextureBakCheckBox.IsChecked == true;
            else if (sender == OpenSavedPathCheckBox) OpenSavedPath = OpenSavedPathCheckBox.IsChecked == true;
        }
    }

    public abstract class ModeBase
    {
        public abstract void Open();
        public abstract void Save();
    }

    public class Mode2Img : ModeBase
    {
        protected Image? Img;
        protected string? DefaultSaveName;

        public override void Open()
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "打开图片资源",
                Filter = "图片资源 (*.texture)|*.texture"
            };
            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string fileName = openFileDialog.FileName;
                using var fileStream = FileUtils.OpenFile(fileName);

                DefaultSaveName = Path.GetFileNameWithoutExtension(fileName);

                TextureFile textureFile = new();
                textureFile.Load(fileStream);

                int imgId = textureFile.ImageIDsArray.First().First();

                Img = TextureUtils.MakeFullImage(textureFile.Get<FragmentalImage>(imgId));

                MainWindow.INSTANCE.ShowImage(Img);

            }
            else throw new Exception("未正确选取图片资源");
        }

        public override void Save()
        {
            if (Img == null) throw new Exception("未打开图片");

            var saveFileDialog = new SaveFileDialog
            {
                Title = "保存图片文件",
                Filter = "图片文件 (*.jpg;*.png)|*.jpg;*.png",
            };
            if (DefaultSaveName != null) saveFileDialog.FileName = DefaultSaveName;

            bool? result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                // 获取用户选择的文件名
                string fileName = saveFileDialog.FileName;

                Img.Save(fileName);

                if (MainWindow.INSTANCE.OpenSavedPath) Process.Start("explorer.exe", $"/select,\"{fileName}\"");
            }
            else throw new Exception("未正确选取图片保存地址");
        }
    }

    public class Mode2Tx : ModeBase
    {
        private Image? Img;
        private string? DefaultSaveName;

        public override void Open()
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "打开图片文件",
                Filter = "图片文件 (*.jpg;*.png;*.jpeg;*.bmp)|*.jpg;*.png;*.jpeg;*.bmp"
            };
            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string fileName = openFileDialog.FileName;
                using var fileStream = FileUtils.OpenFile(fileName);

                Img = Image.Load(fileStream);

                MainWindow.INSTANCE.ShowImage(Img);

                DefaultSaveName = Path.GetFileName(fileName) + ".texture";
            }
            else throw new Exception("未正确选取图片资源");
        }

        public override void Save()
        {
            if (Img == null) throw new Exception("未打开图片");

            SaveFileDialog saveFileDialog = new()
            {
                Title = "保存图片资源",
                Filter = "图片资源 (*.texture)|*.texture"
            };
            if (DefaultSaveName != null) saveFileDialog.FileName = DefaultSaveName;

            bool? result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                string fileName = saveFileDialog.FileName;

                if (MainWindow.INSTANCE.OverrideTextureBak && File.Exists(fileName)) File.WriteAllBytes(fileName + ".bak", File.ReadAllBytes(fileName));

                using var fileStream = FileUtils.OpenFile(fileName, true, true, false);

                TextureFile textureFile = new();
                int imgId = textureFile.Add(TextureUtils.CreateImage(Img, 512));
                textureFile.ImageIDsArray.Add(new List<int> { imgId });

                textureFile.Save(fileStream);
            }
            else throw new Exception("未正确选取图片资源保存地址");
        }
    }
}
