using Me.EarzuChan.Ryo.Adaptions;
using Me.EarzuChan.Ryo.Formations;
using Me.EarzuChan.Ryo.Masses;
using Me.EarzuChan.Ryo.Utils;
using Newtonsoft.Json;
using SixLabors.ImageSharp.Processing;
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

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var fileName = Path.GetFileNameWithoutExtension(PathString);
            var mass = MassManager.INSTANCE.LoadMassFile(PathString, fileName);

            commandFrame.PrintLine("已载入，索引信息如下：\n\n" + MassManager.INSTANCE.GetInfo(mass));
            commandFrame.PrintLine($"\n档案已载入，名称：{fileName.ToUpper()}，稍后可凭借该名称Dump文件、查看索引信息、进行增删改查等操作。");
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

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            if (mass != null) commandFrame.PrintLine(FileName.ToUpper() + "的索引信息：\n\n" + MassManager.INSTANCE.GetInfo(mass));
            else throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");
        }
    }

    [Command("New", "Create a new empty file")]
    public class NewCommand : ICommand
    {
        public string FileName;
        public NewCommand(string fileName)
        {
            FileName = fileName;
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            if (mass == null)
            {
                var newMass = new MassFile();
                MassManager.INSTANCE.AddMassFile(newMass, FileName);

                commandFrame.PrintLine("文件：" + FileName + " 添加成功");
            }
            else throw new Exception("存在已加载的同名文件，请换一个名字");
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
        public void Execute(ICommand.CommandFrame commandFrame) => MassManager.INSTANCE.UnloadMassFile(FileName);

    }

    [Command("Help", "Get the help infomations of this application")]
    public class HelpCommand : ICommand
    {
        public void Execute(ICommand.CommandFrame commandFrame)
        {
            commandFrame.PrintLine($"如今支持{commandFrame.Manager.commands.Count}个命令，使用方法如下：");

            int i = 1;
            foreach (var cmd in commandFrame.Manager.commands)
            {
                commandFrame.PrintLine($"No.{i++} {cmd.Key.Name} 用法：{cmd.Key.Information}");
            }

            if (!commandFrame.Manager.RunningWithArgs) commandFrame.PrintLine("\n如需退出，键入Exit。");
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

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                var typename = AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(mass.ItemAdaptions[mass.ItemBlobs[Id].AdaptionId].DataJavaClz);
                var item = mass.Get<object>(Id);

                commandFrame.PrintLine($"数据类型：{typename}\n\n内置读取器：\n{FormatManager.INSTANCE.OldItemToString(item)}\n\nNewtonsoft读取器：\n{FormatManager.INSTANCE.ItemToString(item)}");
            }
            catch (Exception ex)
            {
                throw new Exception($"不能获取对象", ex);
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

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                var map = mass.IdStrPairs;
                commandFrame.PrintLine("搜索结果：");
                foreach (var item in map)
                {
                    var itBlob = mass.ItemBlobs[item.Value];
                    var adaTion = mass.ItemAdaptions[itBlob.AdaptionId];
                    if (item.Key.ToLower().Contains(SearchName)) commandFrame.PrintLine($"Id.{item.Value} 名称：{item.Key} 大小：{itBlob.Data.Length} 类型：{adaTion.DataJavaClz} 适配器：{adaTion.AdapterJavaClz}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"不能查找对象", ex);
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

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                using var fileStream = FileUtils.OpenFile(PathName, true, true);
                mass.Save(fileStream);

                commandFrame.PrintLine("已保存");
            }
            catch (Exception ex)
            {
                throw new Exception($"不能写出档案", ex);
            }
        }
    }

    [Command("LoadTestFile", "For dev only", true)]
    public class LoadTestFileCommand : ICommand
    {
        public void Execute(ICommand.CommandFrame commandFrame)
        {
            commandFrame.Manager.ParseCommand("Load", "D:\\A Sources\\WeakPipeRecovery\\assets\\fuqi.fs");
        }
    }

    // TODO:解压并拼接
    [Command("UnpackImage", "Unpack the images from a texture file.")]
    public class UpackImageCommand : ICommand
    {
        public enum MODE
        {
            FullInfoAndDump,
            HighestComposed
        }

        public string FileName;
        public MODE Mode = MODE.HighestComposed;

        public UpackImageCommand(string fileName)
        {
            FileName = fileName;
        }

        public UpackImageCommand(string fileName, string mode)
        {
            FileName = fileName;
            Mode = (MODE)int.Parse(mode);
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            try
            {
                using var stream = FileUtils.OpenFile(FileName);

                var fileName = Path.GetFileNameWithoutExtension(stream.Name);

                var textureFile = new TextureFile();
                textureFile.Load(stream);

                switch (Mode)
                {
                    case MODE.FullInfoAndDump:
                        {
                            commandFrame.PrintLine($"名称：{fileName.ToUpper()}\n\n信息如下：\n");

                            commandFrame.PrintLine(FileName.ToUpper() + "的索引信息：\n");
                            commandFrame.PrintLine($"图片碎片数：{textureFile.ItemBlobs.Count}");

                            commandFrame.PrintLine($"\n图片模式适配项数：{textureFile.ItemAdaptions.Count}");
                            foreach (var item in textureFile.ItemAdaptions) commandFrame.PrintLine($"-- 数据类型：{item.DataJavaClz} 适配器：{item.AdapterJavaClz}");

                            commandFrame.PrintLine($"\n图片项数：{textureFile.ImageIDsArray.Count}");
                            for (var i = 0; i < textureFile.ImageIDsArray.Count; i++) commandFrame.PrintLine($"-- No.{i + 1} 对应的ID：[{FormatManager.INSTANCE.ItemToString(textureFile.ImageIDsArray[i])}]");

                            if (textureFile.ImageIDsArray.Count == 0) return;

                            commandFrame.PrintLine("\n解析各项图片：");

                            int piece = 1;
                            for (int i = 0; i < textureFile.ImageIDsArray.Count; i++)
                            {
                                int[] items = textureFile.ImageIDsArray[i].ToArray();

                                foreach (int id in items)
                                {
                                    var imageBlob = textureFile.Get<FragmentalImage>(id);

                                    //LogUtil.INSTANCE.PrintInfo($"\n总片数：{imageBlob.ClipCount}，总层数：{imageBlob.SliceWidths.Length}，解析各项图片：");

                                    if (imageBlob != null && imageBlob.ClipSize != 0)
                                    {
                                        commandFrame.PrintLine($"-- No.{piece} 属于第{i + 1}格式 最大碎片长宽：[{imageBlob.ClipSize}] 层级高：[{FormatManager.INSTANCE.ItemToString(imageBlob.LevelHeights)}] 层级宽：[{FormatManager.INSTANCE.ItemToString(imageBlob.LevelWidths)}] 层级数：{imageBlob.RyoPixmaps.Length}");
                                        string pathName = FileName + $" No_{piece} Dumps";
                                        commandFrame.PrintLine("-- 该图片的相关资源将被写出在：" + pathName);
                                        if (!Directory.Exists(pathName)) Directory.CreateDirectory(pathName);

                                        for (int level = 0; level < imageBlob.RyoPixmaps.Length; level++)
                                        {
                                            RyoPixmap[] pixs = imageBlob.RyoPixmaps[level];
                                            commandFrame.PrintLine($"---- 第{level + 1}层 有{pixs.Length}个");

                                            string levelPathName = pathName + $"\\Lv_{level + 1}";
                                            if (!Directory.Exists(levelPathName)) Directory.CreateDirectory(levelPathName);
                                            for (int no = 0; no < pixs.Length; no++)
                                            {
                                                RyoPixmap pix = pixs[no];
                                                commandFrame.PrintLine($"------ 第{no + 1}个 类型：{pix.Format}");// 像素数：{pix.GetPixelsCount()}");

                                                string levelFileName = $"{levelPathName}\\No_{no + 1}.{(pix.IsJPG ? "jpg" : "png")}";

                                                try
                                                {
                                                    var it = pix.ToImage() ?? throw new Exception("图片转换失败，故写不出");
                                                    // var streamHere = new FileStream(levelFileName, FileMode.OpenOrCreate);
                                                    it.Save(levelFileName);
                                                    // streamHere.Dispose();
                                                }
                                                catch (Exception ex)
                                                {
                                                    commandFrame.PrintLine($"不能写出图片 只因：{ex}");
                                                }
                                            }
                                        }
                                        piece++;
                                    }
                                }
                            }

                            break;
                        }

                    case MODE.HighestComposed:
                        {
                            var fragmentalImage = textureFile.Get<FragmentalImage>(textureFile.ImageIDsArray.First().First()) ?? throw new FileNotFoundException("无法找到默认图片");

                            Image outputImage = TextureUtils.MakeFullImage(fragmentalImage);

                            // 保存拼接好的图像
                            string savePath = FileName.Replace(".texture", "");
                            if (fragmentalImage.RyoPixmaps.First().First().IsJPG)
                            {
                                commandFrame.PrintLine("该文件实际上被转存为了Jpg");

                                if (savePath.Contains(".png")) savePath = savePath.Replace(".png", ".jpg");
                            }

                            outputImage.Save(savePath);
                            commandFrame.PrintLine("写出成功，路径：" + savePath);

                            break;
                        }

                    default:
                        throw new NotImplementedException("暂不支持" + Mode);
                }
            }
            catch (Exception e)
            {
                throw new Exception("解包图片失败", e);
            }
        }
    }

    [Command("TestConversion", "For Dev only", true)]
    public class TestConversionCommand : ICommand
    {
        public string? Name { get; set; }

        public TestConversionCommand() { }
        public TestConversionCommand(string name) => Name = name;
        public void Execute(ICommand.CommandFrame commandFrame)
        {
            List<string> types = new() { "[I", "java.lang.Integer", "cust0m", "[Lcust0m;", "java.lang.String", "[Ljava.lang.String;", "[[B", "[B" };

            if (Name != null) types.Add(Name);

            int i = 1;
            foreach (var item in types)
            {
                var ryo = AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(item);
                var re = AdaptionManager.INSTANCE.GetJavaClzByRyoType(ryo);
                commandFrame.PrintLine($"No.{i}  原：{item}  结果：{re}  Java短名：{ryo.ShortName}  Java名：{ryo.Name}  自定义：{ryo.IsAdaptableCustom}  是列表：{ryo.IsArray}  C#类：{ryo.BaseType}");
                i++;
            }
        }
    }

    [Command("TestConversion2", "For Dev only", true)]
    public class TestConversion2Command : ICommand
    {
        // public string MassName;
        public ArrayList Items = new() { "测试", 114514, 1.0F, new UserMessage("太美丽", true), new int[] { 0 } };

        /*public TestConv2Command(string massName)
        {
            MassName = massName;
        }*/

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i]!.GetType();
                var ryo = AdaptionManager.INSTANCE.GetRyoTypeByCsClz(item);
                var re = AdaptionManager.INSTANCE.GetCsClzByRyoType(ryo);
                commandFrame.PrintLine($"No.{i + 1}  原：{item}  结果：{re}  Java短名：{ryo.ShortName}  Java名：{ryo.Name}  自定义：{ryo.IsAdaptableCustom}  是列表：{ryo.IsArray}  C#类：{ryo.BaseType}");
            }
        }
    }

    // NotSupportNow
    /*[Command("RpSm", "Replace a SenderMessage in a file", true)]
    public class RpSmCommand : ICommand
    {
        public string FileName;
        public int Id;
        public string Msg;

        public RpSmCommand(string fileName, string id, string msg)
        {
            FileName = fileName;
            Id = int.Parse(id);
            Msg = msg;
        }

        public void Execute(ICommand.ICommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                var msg = mass.Get<SenderMessage>(Id);
                msg.Message = Msg;
                mass.Set(Id, msg);
            }
            catch (Exception ex)
            {
                LogUtils.INSTANCE.PrintError($"添加失败", ex);
            }
        }
    }*/

    /*[Command("RpCs", "Replace a Conversastions in a file", true)]
    public class RpCsCommand : ICommand
    {
        public string FileName;
        public int Id;
        public string Msg;

        public RpCsCommand(string fileName, string id, string msg)
        {
            FileName = fileName;
            Id = int.Parse(id);
            Msg = msg;
        }

        public void Execute(ICommand.ICommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                var msg = mass.Get<Conversations>(Id);
                foreach (var item in msg.ConversationArray)
                {
                    foreach (var sbit in item.SenderMessagers) sbit.Message += msg + "，太美丽！";
                    foreach (var sbit in item.UserMessages) sbit.Message += msg + "，只有为你感激。";
                }
                mass.Set(Id, msg);
            }
            catch (Exception ex)
            {
                LogUtils.INSTANCE.PrintError($"添加失败", ex);
            }
        }
    }*/

    [Command("AddConversations", "Add a crazy Conversastions in a file")]
    public class AddConversationsCommand : ICommand
    {
        public string FileName;
        public string Msg;

        public AddConversationsCommand(string fileName, string msg)
        {
            FileName = fileName;
            Msg = msg;
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                var umsg = new UserMessage(Msg + "，太美丽！", false);
                var smsg = new SenderMessage(Msg + "，只有为你感激。", "原神", "1919/8/10", "11:45", 1, 1, "太美丽", 1);
                string[] strArr = { Msg };
                UserMessage[] umsgArr = { umsg };
                SenderMessage[] smsgArr = { smsg };
                Conversation[] msgs = { new Conversation(strArr, Msg, umsgArr, true, smsgArr, strArr, strArr, Msg) };
                var msg = new Conversations(Msg, msgs);
                mass.Add(Msg, msg);
            }
            catch (Exception ex)
            {
                throw new Exception($"添加失败", ex);
            }
        }
    }

    [Command("AddOuter", "For Dev only", true)]
    public class AddOuterCommand : ICommand
    {
        public string FileName;
        public string Msg;

        public AddOuterCommand(string fileName, string msg)
        {
            FileName = fileName;
            Msg = msg;
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                var msg = new YsbNmslOuter(Msg, new YsbNmslInner(Msg));
                mass.Add(Msg, msg);
            }
            catch (Exception ex)
            {
                throw new Exception($"添加失败", ex);
            }
        }
    }

    [Command("AddHugeOuter", "For Dev only", true)]
    public class AddHugeOuterCommand : ICommand
    {
        public string FileName;
        public string Msg;

        public AddHugeOuterCommand(string fileName, string msg)
        {
            FileName = fileName;
            Msg = msg;
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                var msg = new YsbNmslHugeOuter(Msg, new YsbNmslInner(Msg), new YsbNmslInner[] { new YsbNmslInner("叶仕斌" + Msg) });
                mass.Add(Msg, msg);
            }
            catch (Exception ex)
            {
                throw new Exception($"添加失败", ex);
            }
        }
    }

    [Command("AddInner", "For Dev only", true)]
    public class AddInnerCommand : ICommand
    {
        public string FileName;
        public string Msg;

        public AddInnerCommand(string fileName, string msg)
        {
            FileName = fileName;
            Msg = msg;
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                var msg = new YsbNmslInner(Msg);
                mass.Add(Msg, msg);
            }
            catch (Exception ex)
            {
                throw new Exception($"添加失败", ex);
            }
        }
    }

    [Command("ParseConversations", "Parse a Conversations by a json text into a file")]
    public class ParseConversationsCommand : ICommand
    {
        public string FileName;
        public string MsgPath;
        public string ItemName;

        public ParseConversationsCommand(string fileName, string msgPath, string itemName)
        {
            FileName = fileName;
            MsgPath = msgPath;// .Replace("\\\"","\"");
            ItemName = itemName;
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");

                string msgText = File.ReadAllText(MsgPath);

                var msg = JsonConvert.DeserializeObject<Conversations>(msgText) ?? throw new NullReferenceException("序列化Json失败，请检查你的输入（注：Json中正常的“\"”请用“\\\"”转义）");

                mass.Add(ItemName, msg);
            }
            catch (Exception ex)
            {
                throw new Exception($"添加失败", ex);
            }
        }
    }

    // 裁切
    [Command("PackImage", "Pack a image to a texture file and save it")]
    public class PackImageCommand : ICommand
    {
        public string FileName;
        public string ImgPath;

        public PackImageCommand(string fileName, string imgPath)
        {
            FileName = fileName;
            ImgPath = imgPath;
        }

        public PackImageCommand(string imgPath)
        {
            ImgPath = imgPath;
            FileName = imgPath + ".texture";
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            try
            {
                using FileStream fileStream = FileUtils.OpenFile(ImgPath);

                FragmentalImage image = TextureUtils.FastCreateImage(fileStream);

                var txfile = new TextureFile();
                txfile.ImageIDsArray.Add(new() { txfile.Add(image) });

                using FileStream saveStream = FileUtils.OpenFile(FileName, true, true, false);
                txfile.Save(saveStream);

                commandFrame.PrintLine("保存成功，路径：" + FileName);
            }
            catch (Exception ex)
            {
                throw new Exception("图片保存失败", ex);
            }
        }
    }

    [Command("Inflate", "Inflate a file")]
    public class InflateCommand : ICommand
    {
        public enum FILETYPE
        {
            FileSystem,
            TextureFile
        }

        public FILETYPE FileType = FILETYPE.FileSystem;
        public string FileName;
        // public bool WriteToOriginal = false;

        public InflateCommand(string fileName)
        {
            FileName = fileName;
        }

        public InflateCommand(string fileName, string fileType)
        {
            FileName = fileName;
            FileType = (FILETYPE)int.Parse(fileType);
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            using FileStream fileStream = FileUtils.OpenFile(FileName);
            Mass mass = FileType switch
            {
                FILETYPE.FileSystem => new MassFile(),
                FILETYPE.TextureFile => new TextureFile(),
                _ => throw new NotSupportedException("暂不支持该类型文件"),
            };
            mass.Load(fileStream);

            var fileName = FileName + "_inflated";
            using var writer = FileUtils.OpenFile(fileName, true, true);
            mass.Save(writer, true);

            commandFrame.PrintLine("写出到" + fileName);
        }
    }

    // 增加一个写出到文件夹然后格式化为Json或者就是Raw
}