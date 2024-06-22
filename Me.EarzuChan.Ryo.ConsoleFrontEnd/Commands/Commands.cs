using Me.EarzuChan.Ryo.Core.Adaptations;
using Me.EarzuChan.Ryo.Core.Formations.DataFormations.PipeDream;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Exceptions.Utils;
using Me.EarzuChan.Ryo.Exceptions;
using Me.EarzuChan.Ryo.Exceptions.FileExceptions;
using Me.EarzuChan.Ryo.Utils;
using SixLabors.ImageSharp;
using System.Text;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Core.Formations.HelperFormations;
using Me.EarzuChan.Ryo.ConsoleSystem.Commands;
using Me.EarzuChan.Ryo.ConsoleSystem;

namespace Me.EarzuChan.Ryo.ConsoleFrontEnd.Commands
{
    [Command("Open", "Open a file from path")]
    public class OpenCommand : ICommand
    {
        public string PathString;
        public string? CustomFileName;

        public OpenCommand(string path) => PathString = path;

        public OpenCommand(string path, string customFileName)
        {
            PathString = path;
            CustomFileName = customFileName;
        }

        public void Execute(ConsoleApplicationContext commandFrame)
        {
            var fileName = CustomFileName ?? Path.GetFileNameWithoutExtension(PathString);
            var mass = commandFrame.Inject<MassManager>().LoadMassFile(PathString, fileName);

            commandFrame.PrintLine($"Loaded, index information as follows:\n\n{mass.GetInfo()}");
            commandFrame.PrintLine($"\nFile loaded, named {fileName.MakeFirstCharUpper()}, you can later dump the file, view index information, perform CRUD operations, and more using this name.");
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

        public void Execute(ConsoleApplicationContext commandFrame)
        {
            ControlFlowUtils.TryCatchingThenThrow("Couldn't show file info", () => commandFrame.PrintLine(FileName.ToUpper() + "的索引信息：\n\n" + MassUtils.GetInfo(commandFrame.Inject<MassManager>().GetMassFileOrThrow(FileName)))
            );
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

        public void Execute(ConsoleApplicationContext commandFrame)
        {
            if (commandFrame.Inject<MassManager>().ExistsMass(FileName)) throw new RyoException("A file with the same name is already loaded, please choose a different name.");

            var newMass = new MassFile();
            commandFrame.Inject<MassManager>().AddMassFile(newMass, FileName);

            commandFrame.PrintLine($"File: {FileName} added successfully.");

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
        public void Execute(ConsoleApplicationContext commandFrame)
        {
            if (commandFrame.Inject<MassManager>().ExistsMass(FileName)) commandFrame.Inject<MassManager>().UnloadMassFile(FileName);
            else throw new NoSuchFileException(FileName);
        }

    }

    [Command("View", "View the item of your given id in a file")]
    public class ViewCommand : ICommand
    {
        public string FileName;
        public int Id;

        public ViewCommand(string fileName, string id)
        {
            FileName = fileName;
            Id = int.Parse(id);
        }

        public void Execute(ConsoleApplicationContext commandFrame)
        {
            ControlFlowUtils.TryCatchingThenThrow("Cannot retrieve object", () =>
            {
                var mass = commandFrame.Inject<MassManager>().GetMassFileOrThrow(FileName);

                var typename = mass.ItemAdaptions[mass.ItemBlobs[Id].AdaptionId].DataJavaClz.JavaClassToRyoType();
                var item = mass.Get<object>(Id);
                var itemName = mass.IdStrPairs.Where(pair => pair.Value == Id).Select(pair => pair.Key).FirstOrDefault();

                string newtonJson = DataSerializationUtils.ToJson(item);

                commandFrame.PrintLine($"{(itemName != null ? $"Item Name: {itemName}" : "This is a sub-item, no item name")} Data original type: {typename}\n\nBuilt-in reader:\n{DataSerializationUtils.ToJsonWithInternalAlgorithm(item)}\n\nNewtonsoft reader:\n{newtonJson}");

                bool dump = commandFrame.ReadYesOrNo("\nHaha, do you want to dump");
                if (dump)
                {
                    string dumpPath = commandFrame.ReadLine("\nInput dump directory");
                    if (!string.IsNullOrEmpty(dumpPath))
                    {
                        Directory.CreateDirectory(dumpPath); // 检测文件夹纯真

                        itemName ??= commandFrame.ReadLine("\nName for the project"); // 无名需要命名

                        if (!string.IsNullOrEmpty(itemName))
                        {
                            dumpPath = dumpPath + "/" + itemName.Replace('/', '_');

                            // 可Dump类型 Dump它列表
                            if (item.GetType().IsAssignableTo(typeof(IDumpable)))
                            {
                                commandFrame.PrintLine("\nThis item supports file dumping, so let's try.\n");

                                var dumpDic = ((IDumpable)item).GetDumpableObjects();

                                if (dumpDic != null)
                                {
                                    foreach (var dumpObj in dumpDic)
                                    {
                                        if (dumpObj.Value == null || string.IsNullOrWhiteSpace(dumpObj.Key))
                                        {
                                            commandFrame.PrintLine($"Sub-item {dumpObj.Key} can't be dump.");
                                            continue;
                                        }

                                        string dumpFilePath = dumpPath + "." + dumpObj.Key;
                                        using var dumpFile = FileUtils.OpenFile(dumpFilePath, true, true);
                                        dumpFile.Write(dumpObj.Value);
                                        commandFrame.PrintLine($"Sub-item {dumpObj.Key} saved to {dumpFilePath}.");
                                    }
                                }
                                else commandFrame.PrintLine($"{item.GetType()}实现了IDumpable接口，却返回Null，系来骗、来偷袭");
                            }

                            // 序列化为Json
                            string jsonFilePath = dumpPath + ".json";
                            using var jsonFile = FileUtils.OpenFile(jsonFilePath, true, true);
                            jsonFile.Write(Encoding.UTF8.GetBytes(newtonJson));
                            commandFrame.PrintLine($"\nJson file saved to {jsonFilePath}");

                            commandFrame.PrintLine("\nDumped.");
                        }
                        else commandFrame.PrintLine("Filename is not correct, no dumping.");
                    }
                    else commandFrame.PrintLine("Dump directory is not correct, no dumping.");
                }
            });
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

        public void Execute(ConsoleApplicationContext commandFrame)
        {
            ControlFlowUtils.TryCatchingThenThrow("Cannot locate object", () =>
            {
                var mass = commandFrame.Inject<MassManager>().GetMassFileOrThrow(FileName);

                var map = mass.IdStrPairs;
                commandFrame.PrintLine("Search Results:");
                foreach (var item in map)
                {
                    var itBlob = mass.ItemBlobs[item.Value];
                    var adaTion = mass.ItemAdaptions[itBlob.AdaptionId];
                    if (item.Key.ToLower().Contains(SearchName)) commandFrame.PrintLine($"Id.{item.Value} Name：{item.Key} Size：{itBlob.Data.Length} Type：{adaTion.DataJavaClz} Adapter：{adaTion.AdapterJavaClz}");
                }
            });
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

        public void Execute(ConsoleApplicationContext commandFrame)
        {
            ControlFlowUtils.TryCatchingThenThrow("Cannot save object", () =>
            {
                var mass = commandFrame.Inject<MassManager>().GetMassFileOrThrow(FileName);

                using var fileStream = FileUtils.OpenFile(PathName, true, true);
                mass.Save(fileStream);

                commandFrame.PrintLine("Saved.");
            });
        }
    }

    [Command("UnpackImage", "Unpack the images from a texture file.")]
    public class UnpackImageCommand : ICommand
    {
        public enum MODE
        {
            FullInfoAndDump,
            HighestComposed
        }

        public string FileName;
        public MODE Mode = MODE.HighestComposed;

        public UnpackImageCommand(string fileName)
        {
            FileName = fileName;
        }

        public UnpackImageCommand(string fileName, string mode)
        {
            FileName = fileName;
            Mode = (MODE)int.Parse(mode);
        }

        public void Execute(ConsoleApplicationContext commandFrame)
        {
            ControlFlowUtils.TryCatchingThenThrow("Couldn't unpack image", () =>
            {
                using var stream = FileUtils.OpenFile(FileName);

                var fileName = Path.GetFileNameWithoutExtension(stream.Name);

                var textureFile = new TextureFile();
                textureFile.Load(stream);

                switch (Mode)
                {
                    // HACK:罗里吧嗦 解包还是蛮麻烦，多种张图片，多种格式，多层剪辑 我测你们妈
                    case MODE.FullInfoAndDump:
                        {
                            commandFrame.PrintLine($"名称：{fileName.ToUpper()}\n\n信息如下：\n");

                            commandFrame.PrintLine(FileName.ToUpper() + "的索引信息：\n");
                            commandFrame.PrintLine($"图片碎片数：{textureFile.ItemBlobs.Count}");

                            commandFrame.PrintLine($"\n图片模式适配项数：{textureFile.ItemAdaptions.Count}");
                            foreach (var item in textureFile.ItemAdaptions) commandFrame.PrintLine($"-- 数据类型：{item.DataJavaClz} 适配器：{item.AdapterJavaClz}");

                            commandFrame.PrintLine($"\n图片项数：{textureFile.ImageIDsArray.Count}");
                            for (var i = 0; i < textureFile.ImageIDsArray.Count; i++) commandFrame.PrintLine($"-- No.{i + 1} 对应的ID：[{DataSerializationUtils.ToJson(textureFile.ImageIDsArray[i])}]");

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
                                        commandFrame.PrintLine($"-- No.{piece} 属于第{i + 1}格式 最大碎片长宽：[{imageBlob.ClipSize}] 层级高：[{imageBlob.LevelHeights.ToJson()}] 层级宽：[{imageBlob.LevelWidths.ToJson()}] 层级数：{imageBlob.RyoPixmaps.Length}");
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
                                                commandFrame.PrintLine($"------ 第{no + 1}个 类型：{pix.Format}");

                                                string levelFileName = $"{levelPathName}\\No_{no + 1}.{(pix.IsJPG ? "jpg" : "png")}";

                                                try
                                                {
                                                    var it = pix.ToImage() ?? throw new RyoException("Fail to convert this image");

                                                    it.Save(levelFileName);
                                                }
                                                catch (Exception ex)
                                                {
                                                    commandFrame.PrintLine($"Cannot dump this image due to {ex.Message.MakeFirstCharLower}.");
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

                            Image outputImage = fragmentalImage.ToImage();

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
                        throw new NotImplementedException("No such mode of your given id" + Mode);
                }
            });
        }
    }

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

        public void Execute(ConsoleApplicationContext commandFrame)
        {
            ControlFlowUtils.TryCatchingThenThrow("Couldn't pack image to a \".texture\"", () =>
            {
                using FileStream fileStream = FileUtils.OpenFile(ImgPath);

                FragmentalImage image = TextureUtils.FastCreateImage(fileStream);

                var txfile = new TextureFile();
                txfile.ImageIDsArray.Add(new() { txfile.Add(image) });

                using FileStream saveStream = FileUtils.OpenFile(FileName, true, true, false);
                txfile.Save(saveStream);

                commandFrame.PrintLine("Saved to " + FileName);
            });
        }
    }

    [Command("ImportDialogueTree", "Import dialogue tree from a json file into a file, only for PipeDreams now")]
    public class ImportDialogueTreeCommand : ICommand
    {
        public string FileName;
        public string JsonFilePath;
        public string ItemName;

        public ImportDialogueTreeCommand(string fileName, string jsonFilePath, string itemName)
        {
            FileName = fileName;
            JsonFilePath = jsonFilePath;
            ItemName = itemName;
        }

        public void Execute(ConsoleApplicationContext commandFrame)
        {
            ControlFlowUtils.TryCatchingThenThrow("Parse failed", () =>
            {
                var mass = commandFrame.Inject<MassManager>().GetMassFileOrThrow(FileName);

                // TODO:创建通用的拨弄 我们得约定一个输出格式 才能输入

                string msgText = File.ReadAllText(JsonFilePath);

                var msg = DataSerializationUtils.JsonToObject<DialogueTreeDescriptor>(msgText) ?? throw new NullReferenceException("序列化Json失败，请检查你的输入（注：Json中正常的“\"”请用“\\\"”转义）");

                var result = mass.Add(ItemName, msg);

                commandFrame.PrintLine($"Added, mode is {result.ToString().MakeFirstCharLower()}");
            });
        }
    }

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

        private static string Edit(ConsoleApplicationContext commandFrame, string ori)
        {
            while (true)
            {
                bool doTrans = commandFrame.ReadYesOrNo("Do you want to edit it", ConsoleKey.Enter, ConsoleKey.DownArrow);

                if (!doTrans) return ori;
                string str = commandFrame.ReadLine("Content");

                if (commandFrame.ReadYesOrNo("Save changes", ConsoleKey.Enter, ConsoleKey.Backspace)) return str;
            }
        }

        public void Execute(ConsoleApplicationContext commandFrame)
        {
            ControlFlowUtils.TryCatchingThenThrow("Operating Dialogue Tree encountered an issue", () =>
            {
                var mass = commandFrame.Inject<MassManager>().GetMassFileOrThrow(FileName);

                var dialogue = mass.Get<DialogueTreeDescriptor>(Id);

                var dialogueItemName = mass.IdStrPairs.Where(pair => pair.Value == Id).Select(pair => pair.Key).FirstOrDefault();

                if (dialogue != null)
                {
                    commandFrame.PrintLine($"{(dialogueItemName != null ? $"Item Name: {dialogueItemName}" : "This is a sub-item, no item name")} Dialogue Tree Namespace: {dialogue.DialogueNameSpace} Conversation Count: {dialogue.ConversationList.Count}");

                    bool applyEdit = dialogueItemName != null && commandFrame.ReadYesOrNo("Apply edit mode");

                    int no = 1;
                    foreach (var conv in dialogue.ConversationList)
                    {
                        commandFrame.PrintLine($"--------\nDialogue Part {no}:\nTags: {conv.Tags.ToJson()}\nTags to lock: {conv.TagsToLock.ToJson()}\nTags to unlock: {conv.TagsToUnlock.ToJson()}\nStatus: {conv.Status} Unread: {conv.StateOfDiswatch} Trigger: {conv.Trigger}");
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

                    if (applyEdit && commandFrame.ReadYesOrNo("\n--------\nSave all changes"))
                    {
                        mass.Add(dialogueItemName!, dialogue);
                        commandFrame.PrintLine("Changes saved.");
                    }
                }
            }, new Dictionary<Type, string> { { typeof(InvalidCastException), "The item got by the id you given is not a Dialogue Tree item" } });
        }
    }

    // Mass那边是否配合完了项目压缩
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

        public void Execute(ConsoleApplicationContext commandFrame)
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
            mass.Save(writer, false);

            commandFrame.PrintLine($"Write out to {fileName}.");
        }
    }

    // TODO:增加一个全体写出到文件夹然后格式化为Json或者就是Raw或者是Dumped
}
