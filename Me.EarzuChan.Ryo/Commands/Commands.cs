using Me.EarzuChan.Ryo.Adaptions;
using Me.EarzuChan.Ryo.Commands.CommandExceptions;
using Me.EarzuChan.Ryo.Formations;
using Me.EarzuChan.Ryo.Formations.PipeDream;
using Me.EarzuChan.Ryo.Masses;
using Me.EarzuChan.Ryo.Utils;
using Newtonsoft.Json;
using System.Text;
using static Me.EarzuChan.Ryo.Commands.ICommand;

namespace Me.EarzuChan.Ryo.Commands
{
    [Command("Open", "Open a file from path")]
    public class OpenCommand : ICommand
    {
        public string PathString;
        public OpenCommand(string path)
        {
            PathString = path;
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var fileName = Path.GetFileNameWithoutExtension(PathString);
            var mass = MassManager.INSTANCE.LoadMassFile(PathString, fileName);

            commandFrame.PrintLine($"Loaded, index information as follows:\n\n{MassManager.INSTANCE.GetInfo(mass)}");
            commandFrame.PrintLine($"\nFile loaded, name: {fileName.ToUpper()}, you can later dump the file, view index information, perform CRUD operations, and more using this name.");
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
            else throw new NoSuchFileException();
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

                commandFrame.PrintLine($"File: {FileName} added successfully.");
            }
            else throw new Exception("A file with the same name is already loaded, please choose a different name.");
        }
    }

    [Command("Close", "Close a file by its name")]
    public class CloseCommand : ICommand
    {
        public string FileName;
        public CloseCommand(string fileName)
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
            commandFrame.PrintLine($"{commandFrame.Manager.commands.Count} Available Commands:\n-------------------");

            int i = 1;
            foreach (var cmd in commandFrame.Manager.commands)
            {
                commandFrame.PrintLine($"{i++}. {cmd.Key.Name} - {cmd.Key.Information}");
            }

            if (!commandFrame.Manager.RunningWithArgs) commandFrame.PrintLine("\nType 'exit' to exit the command console.");
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
            // commandFrame.ReadLine("太美丽");

            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new NoSuchFileException();

                var typename = AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(mass.ItemAdaptions[mass.ItemBlobs[Id].AdaptionId].DataJavaClz);
                var item = mass.Get<object>(Id);
                var itemName = mass.IdStrPairs.Where(pair => pair.Value == Id).Select(pair => pair.Key).FirstOrDefault();

                string newtonJson = FormatUtils.NewtonsoftItemToJson(item);

                commandFrame.PrintLine($"{(itemName != null ? $"Item Name: {itemName}" : "This is a sub-item, no item name")} Data type: {typename}\n\nBuilt-in reader:\n{FormatUtils.InsidedItemToJson(item)}\n\nNewtonsoft reader:\n{newtonJson}");

                bool dump = commandFrame.ReadYesOrNo("Haha, do you want to dump");
                if (dump)
                {
                    string dumpPath = commandFrame.ReadLine("Input dump directory");
                    if (!string.IsNullOrEmpty(dumpPath))
                    {
                        Directory.CreateDirectory(dumpPath); // 检测文件夹纯真

                        // string xml = FormatUtils.SystemItemToXml(item);

                        itemName ??= commandFrame.ReadLine("Name for the project"); // 无名需要命名

                        if (!string.IsNullOrEmpty(itemName))
                        {
                            dumpPath = dumpPath + "/" + itemName.Replace('/', '_');
                            if (item.GetType().IsSerializable)
                            {
                                byte[] bin = FormatUtils.SystemItemToBinary(item);
                                using var binFile = FileUtils.OpenFile(dumpPath + ".bin", true, true);
                                binFile.Write(bin);
                            }
                            /*using var xmlFile = FileUtils.OpenFile(dumpPath + ".xml", true, true);
                            xmlFile.Write(Encoding.UTF8.GetBytes(xml));*/
                            using var jsonFile = FileUtils.OpenFile(dumpPath + ".json", true, true);
                            jsonFile.Write(Encoding.UTF8.GetBytes(newtonJson));

                            commandFrame.PrintLine("Saved.");
                        }
                        else commandFrame.PrintLine("Filename is not correct, no dumping.");
                    }
                    else commandFrame.PrintLine("Dump directory is not correct, no dumping.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot retrieve object due to {ex.Message.MakeFirstCharLower()}.", ex);
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
                if (mass == null) throw new NoSuchFileException();

                var map = mass.IdStrPairs;
                commandFrame.PrintLine("Search Results:");
                foreach (var item in map)
                {
                    var itBlob = mass.ItemBlobs[item.Value];
                    var adaTion = mass.ItemAdaptions[itBlob.AdaptionId];
                    if (item.Key.ToLower().Contains(SearchName)) commandFrame.PrintLine($"Id.{item.Value} Name：{item.Key} Size：{itBlob.Data.Length} Type：{adaTion.DataJavaClz} Adapter：{adaTion.AdapterJavaClz}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot locate object due to {ex.Message.MakeFirstCharLower()}.", ex);
            }
        }
    }

    [Command("Save", "Save the file to the path you given")]
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
                if (mass == null) throw new NoSuchFileException();

                using var fileStream = FileUtils.OpenFile(PathName, true, true);
                mass.Save(fileStream);

                commandFrame.PrintLine("Saved.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot save object due to {ex.Message.MakeFirstCharLower()}.", ex);
            }
        }
    }

    /*[Command("LoadTestFile", "For dev only", true)]
    public class LoadTestFileCommand : ICommand
    {
        public void Execute(ICommand.CommandFrame commandFrame)
        {
            commandFrame.Manager.ParseCommand("Load", "D:\\A Sources\\WeakPipeRecovery\\assets\\fuqi.fs");
        }
    }*/

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
                            for (var i = 0; i < textureFile.ImageIDsArray.Count; i++) commandFrame.PrintLine($"-- No.{i + 1} 对应的ID：[{FormatUtils.NewtonsoftItemToJson(textureFile.ImageIDsArray[i])}]");

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
                                        commandFrame.PrintLine($"-- No.{piece} 属于第{i + 1}格式 最大碎片长宽：[{imageBlob.ClipSize}] 层级高：[{FormatUtils.NewtonsoftItemToJson(imageBlob.LevelHeights)}] 层级宽：[{FormatUtils.NewtonsoftItemToJson(imageBlob.LevelWidths)}] 层级数：{imageBlob.RyoPixmaps.Length}");
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

    /*[Command("AddConversations", "Add a crazy Conversastions in a file")]
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
                if (mass == null) throw new NoSuchFileException();

                var umsg = new UserMessage(Msg + "，太美丽！", false);
                var smsg = new SenderMessage(Msg + "，只有为你感激。", "原神", "1919/8/10", "11:45", 1, 1, "太美丽", 1);
                string[] strArr = { Msg };
                UserMessage[] umsgArr = { umsg };
                SenderMessage[] smsgArr = { smsg };
                Conversation[] msgs = { new Conversation(strArr, Msg, umsgArr, true, smsgArr, strArr, strArr, Msg) };
                var msg = new DialogueTreeDescriptor(Msg, msgs);
                mass.Add(Msg, msg);
            }
            catch (Exception ex)
            {
                throw new Exception($"添加失败", ex);
            }
        }
    }*/

    /*[Command("AddOuter", "For Dev only", true)]
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
                if (mass == null) throw new NoSuchFileException();

                var msg = new YsbNmslOuter(Msg, new YsbNmslInner(Msg));
                mass.Add(Msg, msg);
            }
            catch (Exception ex)
            {
                throw new Exception($"Add failed.", ex);
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
                if (mass == null) throw new NoSuchFileException();

                var msg = new YsbNmslHugeOuter(Msg, new YsbNmslInner(Msg), new YsbNmslInner[] { new YsbNmslInner("Yoasobi" + Msg) });
                mass.Add(Msg, msg);
            }
            catch (Exception ex)
            {
                throw new Exception($"Add failed.", ex);
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
                if (mass == null) throw new NoSuchFileException();

                var msg = new YsbNmslInner(Msg);
                mass.Add(Msg, msg);
            }
            catch (Exception ex)
            {
                throw new Exception($"Add failed.", ex);
            }
        }
    }*/

    [Command("ParseDialogueTree", "Parse a DialogueTree by a json text into a file")]
    public class ParseDialogueTreeCommand : ICommand
    {
        public string FileName;
        public string MsgPath;
        public string ItemName;

        public ParseDialogueTreeCommand(string fileName, string msgPath, string itemName)
        {
            FileName = fileName;
            MsgPath = msgPath;
            ItemName = itemName;
        }

        public void Execute(ICommand.CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new NoSuchFileException();

                string msgText = File.ReadAllText(MsgPath);

                var msg = JsonConvert.DeserializeObject<DialogueTreeDescriptor>(msgText) ?? throw new NullReferenceException("序列化Json失败，请检查你的输入（注：Json中正常的“\"”请用“\\\"”转义）");

                mass.Add(ItemName, msg);
            }
            catch (Exception ex)
            {
                throw new Exception($"Add failed.", ex);
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
                _ => throw new NotSupportedException("This file type is not supported at the moment."),
            };
            mass.Load(fileStream);

            var fileName = FileName + "_inflated";
            using var writer = FileUtils.OpenFile(fileName, true, true);
            mass.Save(writer, true);

            commandFrame.PrintLine($"Write out to {fileName}.");
        }
    }

    // TODO:增加一个全体写出到文件夹然后格式化为Json或者就是Raw或者是Dumped

    [Command("OperateDialogueTree", "Only for PipeDreams now")]
    public class OperateDialogueTreeCommand : ICommand
    {
        public string FileName;
        public int Id;

        public OperateDialogueTreeCommand(string fileName, string id)
        {
            FileName = fileName;
            Id = int.Parse(id);
        }

        private static string Edit(CommandFrame commandFrame, string ori)
        {
            while (true)
            {
                bool doTrans = commandFrame.ReadYesOrNo("Do you want to edit it", ConsoleKey.Enter, ConsoleKey.DownArrow);

                if (!doTrans) return ori;
                string str = commandFrame.ReadLine("Content");

                if (commandFrame.ReadYesOrNo("Save changes", ConsoleKey.Enter, ConsoleKey.Backspace)) return str;
            }
        }

        public void Execute(CommandFrame commandFrame)
        {
            var mass = MassManager.INSTANCE.GetMassFile(FileName);
            try
            {
                if (mass == null) throw new NoSuchFileException();

                var dialogue = mass.Get<DialogueTreeDescriptor>(Id);

                var dialogueItemName = mass.IdStrPairs.Where(pair => pair.Value == Id).Select(pair => pair.Key).FirstOrDefault();

                if (dialogue != null)
                {
                    commandFrame.PrintLine($"{(dialogueItemName != null ? $"Item Name: {dialogueItemName}" : "This is a sub-item, no item name")} Dialogue Tree Namespace: {dialogue.DialogueNameSpace} Conversation Count: {dialogue.ConversationList.Count}");

                    bool applyEdit = dialogueItemName != null && commandFrame.ReadYesOrNo("Apply edit mode");

                    int no = 1;
                    foreach (var conv in dialogue.ConversationList)
                    {
                        commandFrame.PrintLine($"--------\nDialogue Part {no}:\nTags: {FormatUtils.NewtonsoftItemToJson(conv.Tags)}\nTags to lock: {FormatUtils.NewtonsoftItemToJson(conv.TagsToLock)}\nTags to unlock: {FormatUtils.NewtonsoftItemToJson(conv.TagsToUnlock)}\nStatus: {conv.Status} Unread: {conv.StateOfDiswatch} Trigger: {conv.Trigger}");
                        int all = conv.SenderMessagers.Count;
                        commandFrame.PrintLine($"\nSender's Conversation Count: {all}");
                        int no2 = 1;
                        foreach (var send in conv.SenderMessagers)
                        {
                            commandFrame.PrintLine($"----\nSender's Conversation {no2}/{all}:\nDate Text: {send.DateText} Time Text: {send.TimeText} Original: {send.Origin}\nTrigger: {send.Trigger} Trigger Time: {send.TriggerTime}\nIdle Time: {send.IdleTime} Typing Time: {send.TypingTime}:\n{send.Message}");
                            if (applyEdit) send.Message = Edit(commandFrame, send.Message); // else send.Message += "日恁毛";
                            no2++;
                        }
                        all = conv.UserMessages.Count;
                        commandFrame.PrintLine($"\nUser's Conversation Count: {all}");
                        no2 = 1;
                        foreach (var user in conv.UserMessages)
                        {
                            commandFrame.PrintLine($"----\nUser's Conversation {no2}/{all} Hidden: {user.IsHidden}:\n{user.Message.Trim()}");
                            if (applyEdit) user.Message = Edit(commandFrame, user.Message); // else user.Message += "物支浪了吸";
                            no2++;
                        }
                        no++;
                    }

                    if (applyEdit)
                    {
                        Thread.Sleep(1000);
                        if (commandFrame.ReadYesOrNo("\n--------\nSave all changes"))
                        {
                            mass.Add(dialogueItemName!, dialogue);
                            commandFrame.PrintLine("Changes saved.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(InvalidCastException)) ex = new Exception("This is not a Dialogue Tree item");
                throw new Exception($"Operating Dialogue Tree encountered an issue, due to {ex.Message.MakeFirstCharLower()}.", ex);
            }
        }
    }

    namespace CommandExceptions
    {
        public class NoSuchFileException : Exception
        {
            public NoSuchFileException() : base("The requested file does not exist. Please check if it has been loaded successfully and ensure the correct spelling of the file name.") { }
        }
    }
}
