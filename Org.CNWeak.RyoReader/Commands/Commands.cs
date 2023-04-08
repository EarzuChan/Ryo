﻿using Me.EarzuChan.Ryo.Adaptions;
using Me.EarzuChan.Ryo.Formations;
using Me.EarzuChan.Ryo.Masses;
using Me.EarzuChan.Ryo.Utils;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Me.EarzuChan.Ryo.Commands
{
    [Command("Load", "Load file from path")]
    public class LoadCommand : ICommand
    {
        public string PathString;
        public LoadCommand(string path)
        {
            PathString = path;
        }

        public void Execute()
        {
            var fileName = Path.GetFileNameWithoutExtension(PathString);
            var mass = MassManager.INSTANCE.LoadMassFile(PathString, fileName);

            LogUtil.INSTANCE.PrintInfo("已载入，索引信息如下：\n\n" + MassManager.INSTANCE.GetInfo(mass));
            LogUtil.INSTANCE.PrintInfo($"\n档案已载入，名称：{fileName.ToUpper()}，稍后可凭借该名称Dump文件、查看索引信息、进行增删改查等操作。");
        }
    }

    [Command("Info", "Show the info of a file")]
    public class InfoCommand : ICommand
    {
        public string FileName;
        public InfoCommand(string fileName)
        {
            FileName = fileName;
        }
        public void Execute()
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            if (mass != null)
            {
                LogUtil.INSTANCE.PrintInfo(FileName.ToUpper() + "的索引信息：\n\n" + MassManager.INSTANCE.GetInfo(mass));
            }
            else LogUtil.INSTANCE.PrintInfo("不能显示信息，文件名不正确");
        }
    }

    [Command("Unload", "Unload a file by its name")]
    public class UnloadCommand : ICommand
    {
        public string FileName;
        public UnloadCommand(string fileName)
        {
            FileName = fileName;
        }
        public void Execute()
        {
            OldMassManager.INSTANCE.MassList.Remove(OldMassManager.INSTANCE.MassList.Find(a => a.Name == FileName)!);
        }
    }

    /*[Command("Dump", "Dump the objects blob of a file")]
    public class DumpCommand : ICommand
    {
        public string FileName;
        public DumpCommand(string str)
        {
            FileName = str;
        }
        public void Execute()
        {
            var mass = OldMassManager.INSTANCE.GetMassFileByFileName(FileName);
            if (mass != null && mass.ReadyToUse)
            {
                LogUtil.INSTANCE.PrintInfo($"找到文件：{FileName.ToUpper()}");
                LogUtil.INSTANCE.PrintInfo(mass.DumpBuffer());
            }
            else LogUtil.INSTANCE.PrintInfo($"找不到文件：{FileName.ToUpper()}，请查看拼写是否正确，文件是否已正确加载？");
        }
    }*/

    [Command("Help", "Get the help infomations of this application")]
    public class HelpCommand : ICommand
    {
        public void Execute()
        {
            LogUtil.INSTANCE.PrintInfo($"如今支持{CommandManager.INSTANCE.commands.Count}个命令，使用方法如下：");

            int i = 1;
            foreach (var cmd in CommandManager.INSTANCE.commands)
            {
                LogUtil.INSTANCE.PrintInfo($"No.{i++} {cmd.Key.Name} 用法：{cmd.Key.Help}");
            }

            if (!CommandManager.INSTANCE.RunningWithArgs) LogUtil.INSTANCE.PrintInfo("\n如需退出，键入Exit。");
        }
    }

    [Command("Get", "Get the item infomation of your given id in a file")]
    public class GetCommand : ICommand
    {
        public string FileName;
        public int Id;

        public GetCommand(string fileName, string id)
        {
            FileName = fileName;
            Id = int.Parse(id);
        }

        public void Execute()
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                var typename = AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(mass.ItemAdaptions[mass.ItemBlobs[Id].AdaptionId].DataJavaClz);
                var item = mass.Get<object>(Id);

                LogUtil.INSTANCE.PrintInfo($"数据类型：{typename}\n\n内置读取器：\n{FormatManager.INSTANCE.OldItemToString(item)}\n\nNewtonsoft读取器：\n{FormatManager.INSTANCE.ItemToString(item)}");
            }
            catch (Exception ex)
            {
                LogUtil.INSTANCE.PrintError($"不能获取对象", ex);
            }
        }
    }

    [Command("Search", "Search the target item in a file")]
    public class SearchCommand : ICommand
    {
        public string FileName;
        public string SearchName;
        public SearchCommand(string fileName, string searchName)
        {
            FileName = fileName;
            SearchName = searchName;
        }

        public void Execute()
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("档案不存在");

                var map = mass.IdStrPairs;
                LogUtil.INSTANCE.PrintInfo("搜索结果：");
                foreach (var item in map)
                {
                    if (item.Key.ToLower().Contains(SearchName)) LogUtil.INSTANCE.PrintInfo($"Id.{item.Value} 名称：{item.Key}");
                }
            }
            catch (Exception ex)
            {
                LogUtil.INSTANCE.PrintError($"不能查找对象", ex);
            }
        }
    }

    [Command("Write", "Saves the file to the path you given")]
    public class WriteCommand : ICommand
    {
        public string FileName;
        public string PathName;

        public WriteCommand(string fileName, string pathName)
        {
            FileName = fileName;
            PathName = pathName;
        }

        public void Execute()
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new InvalidDataException("档案不存在");

                using var fileStream = new FileStream(PathName, FileMode.OpenOrCreate);
                mass.Save(fileStream);

                LogUtil.INSTANCE.PrintInfo("已保存");
            }
            catch (Exception ex)
            {
                LogUtil.INSTANCE.PrintError($"不能写出档案", ex);
            }
        }
    }

    [Command("Fuqi", "For dev only", true)]
    public class FuqiCommand : ICommand
    {
        public void Execute()
        {
            CommandManager.INSTANCE.ParseCommand("Load", "D:\\A Sources\\WeakPipeRecovery\\assets\\fuqi.fs");
        }
    }

    /*[Command("Seek", "Seek the images from a texture file.", true)]
    public class SeekCommand : ICommand
    {
        public string FileName;

        public SeekCommand(string fileName)
        {
            FileName = fileName;
        }

        public void Execute()
        {
            try
            {
                var stream = new FileStream(FileName, FileMode.Open);
                if (stream.Length == 0) throw new Exception("文件长度为零");

                var fileName = Path.GetFileNameWithoutExtension(stream.Name);

                var textureFile = new OldTextureFile(fileName);

                textureFile.Load(stream);

                if (textureFile.ReadyToUse)
                {
                    LogUtil.INSTANCE.PrintInfo($"名称：{fileName.ToUpper()}\n\n纹样信息如下：\n");

                    LogUtil.INSTANCE.PrintInfo(FileName.ToUpper() + "的索引信息：\n");
                    LogUtil.INSTANCE.PrintInfo($"图片碎片数：{textureFile.ObjCount}");
                    for (var i = 0; i < textureFile.ObjCount; i++) LogUtil.INSTANCE.PrintInfo($"-- Id.{i} 适配项ID：{textureFile.DataAdapterIdArray[i]} 已压缩：{(textureFile.IsItemDeflatedArray[i] ? "是" : "否")} 起始索引：{(i == 0 ? 0 : textureFile.EndPosition[i - 1])} 长度：{textureFile.EndPosition[i] - (i == 0 ? 0 : textureFile.EndPosition[i - 1])}");

                    LogUtil.INSTANCE.PrintInfo($"\n图片模式适配项数：{textureFile.MyRegableDataAdaptionList.Count}");
                    foreach (var item in textureFile.MyRegableDataAdaptionList) LogUtil.INSTANCE.PrintInfo($"-- Id.{item.Id} 数据类型：{item.DataJavaClz} 适配器：{item.AdapterJavaClz}");

                    LogUtil.INSTANCE.PrintInfo($"\n真正图片数：{textureFile.ImageIDsArray.Length}");
                    for (var i = 0; i < textureFile.ImageIDsArray.Length; i++) LogUtil.INSTANCE.PrintInfo($"-- No.{i + 1} 图片的各种格式对应的ID：[{FormatManager.INSTANCE.ItemToString(textureFile.ImageIDsArray[i])}]");

                    //LogUtil.INSTANCE.PrintInfo("将写出DUMP数据");
                    if (textureFile.ImageIDsArray.Length == 0) return;

                    LogUtil.INSTANCE.PrintInfo("\n解析各项图片：");

                    int piece = 1;
                    for (int i = 0; i < textureFile.ImageIDsArray.Length; i++)
                    {
                        int[] items = textureFile.ImageIDsArray[i];

                        foreach (int id in items)
                        {
                            var imageBlob = textureFile.GetItemById<FragmentalImage>(id);

                            //LogUtil.INSTANCE.PrintInfo($"\n总片数：{imageBlob.ClipCount}，总层数：{imageBlob.SliceWidths.Length}，解析各项图片：");

                            if (imageBlob != null && imageBlob.ClipCount != 0)
                            {
                                LogUtil.INSTANCE.PrintInfo($"-- No.{piece} 属于第{i + 1}张 碎片数：[{imageBlob.ClipCount}] 碎片宽：[{FormatManager.INSTANCE.ItemToString(imageBlob.SliceHeights)}] 碎片高：[{FormatManager.INSTANCE.ItemToString(imageBlob.SliceWidths)}] 碎片集层数：{imageBlob.RyoPixmaps.Length}");
                                string pathName = textureFile.FullPath + $".No_{piece}.dumps";
                                LogUtil.INSTANCE.PrintInfo("-- 该图片的相关资源将被写出在：" + pathName);
                                if (!Directory.Exists(pathName)) Directory.CreateDirectory(pathName);

                                for (int level = 0; level < imageBlob.RyoPixmaps.Length; level++)
                                {
                                    RyoPixmap[] pixs = imageBlob.RyoPixmaps[level];
                                    LogUtil.INSTANCE.PrintInfo($"---- 第{level + 1}层 有{pixs.Length}个");

                                    string levelPathName = pathName + $"\\Lv_{level + 1}";
                                    if (!Directory.Exists(levelPathName)) Directory.CreateDirectory(levelPathName);
                                    for (int no = 0; no < pixs.Length; no++)
                                    {
                                        RyoPixmap pix = pixs[no];
                                        LogUtil.INSTANCE.PrintInfo($"------ 第{no + 1}个 类型：{pix.Format} 像素数：{pix.GetPixelsCount()}");

                                        string levelFileName = levelPathName + $"\\No_{no + 1}.png";

                                        try
                                        {
                                            Bitmap? it = (Bitmap)pix;
                                            if (it == null) throw new Exception("图片NULL，故写不出");

                                            var streamHere = new FileStream(levelFileName, FileMode.OpenOrCreate);
                                            it!.Save(streamHere, ImageFormat.Png);
                                            streamHere.Dispose();
                                        }
                                        catch (Exception ex)
                                        {
                                            LogUtil.INSTANCE.PrintError("不能写出图片", ex);
                                        }
                                    }
                                }
                                piece++;
                            }
                        }
                    }
                }
                else
                {
                    LogUtil.INSTANCE.PrintInfo("查看失败，文件无效，请检查您指定的文件是否存在、正确、未损坏。");
                    //CommandManager.INSTANCE.ParseCommand("Unload", fileName);
                }
            }
            catch (Exception e)
            {
                LogUtil.INSTANCE.PrintError("载入文件失败", e);
            }
        }
    }*/

    [Command("TestConv", "For Dev only", true)]
    public class TestConvCommand : ICommand
    {
        public string? Name { get; set; }

        public TestConvCommand() { }
        public TestConvCommand(string name) => Name = name;
        public void Execute()
        {
            List<string> types = new() {  "[I", "java.lang.Integer", "cust0m", "[Lcust0m;", "java.lang.String", "[Ljava.lang.String;", "[[B", "[B" };

            if (Name != null) types.Add(Name);

            int i = 1;
            foreach (var item in types)
            {
                var ryo = AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(item);
                var re = AdaptionManager.INSTANCE.GetJavaClzByRyoType(ryo);
                LogUtil.INSTANCE.PrintInfo($"No.{i}  原：{item}  结果：{re}  Java短名：{ryo.ShortName}  Java名：{ryo.Name}  自定义：{ryo.IsAdaptableCustom}  是列表：{ryo.IsArray}  C#类：{ryo.BaseType}");
                i++;
            }
        }
    }

    [Command("TestConv2", "For Dev only", true)]
    public class TestConv2Command : ICommand
    {
        // public string MassName;
        public ArrayList Items = new() { "测试", 114514, 1.0F, new UserMessage("太美丽", true), new int[] { 0 } };

        /*public TestConv2Command(string massName)
        {
            MassName = massName;
        }*/

        public void Execute()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i]!.GetType();
                var ryo = AdaptionManager.INSTANCE.GetRyoTypeByCsClz(item);
                var re = AdaptionManager.INSTANCE.GetCsClzByRyoType(ryo);
                LogUtil.INSTANCE.PrintInfo($"No.{i+1}  原：{item}  结果：{re}  Java短名：{ryo.ShortName}  Java名：{ryo.Name}  自定义：{ryo.IsAdaptableCustom}  是列表：{ryo.IsArray}  C#类：{ryo.BaseType}");
            }
        }
    }
}
