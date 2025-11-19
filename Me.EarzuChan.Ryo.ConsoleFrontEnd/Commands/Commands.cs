using Me.EarzuChan.Ryo.Core.Formations.DataFormations.PipeDream;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Exceptions;
using Me.EarzuChan.Ryo.Exceptions.FileExceptions;
using Me.EarzuChan.Ryo.Utils;
using SixLabors.ImageSharp;
using System.Text;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Core.Formations.HelperFormations;
using Me.EarzuChan.Ryo.ConsoleSystem.Commands;
using Me.EarzuChan.Ryo.ConsoleSystem;

namespace Me.EarzuChan.Ryo.ConsoleFrontEnd.Commands;

// -- 文件操作 --

[Command("Open", "Open a file from path")]
public class OpenCommand : ICommand
{
    private readonly string _pathString;
    private readonly string? _customFileName;

    public OpenCommand(string path) => _pathString = path;

    public OpenCommand(string path, string customFileName)
    {
        _pathString = path;
        _customFileName = customFileName;
    }

    public void Execute(ConsoleApplicationContext ctx)
    {
        var fileName = _customFileName ?? Path.GetFileNameWithoutExtension(_pathString);
        var mass = ctx.Inject<MassManager>()!.LoadMassFile(_pathString, fileName);

        ctx.PrintLine($"Loaded, index information as follows:\n\n{mass.GetInfo()}");
        ctx.PrintLine(
            $"\nFile loaded, named {fileName.MakeFirstCharUpper()}, you can later dump the file, view index information, perform CRUD operations, and more using this name.");
    }
}

[Command("Info", "Show the info of a file")]
public class InfoCommand(string fileName) : ICommand
{
    public void Execute(ConsoleApplicationContext ctx) =>
        LangExt.WrappedTry("Couldn't show file info",
            () => ctx.PrintLine(fileName.ToUpper() + "的索引信息：\n\n" +
                                ctx.Inject<MassManager>()!.GetMassFileOrThrow(fileName).GetInfo())
        );
}

[Command("New", "Create a new empty file")]
public class NewCommand(string fileName) : ICommand
{
    public void Execute(ConsoleApplicationContext ctx)
    {
        var massManager = ctx.Inject<MassManager>()!;

        if (massManager.ExistsMass(fileName))
            throw new RyoException("A file with the same name is already loaded, please choose a different name.");

        var newMass = new MassFile();
        massManager.AddMassFile(newMass, fileName);

        ctx.PrintLine($"File: {fileName} added successfully.");
    }
}

[Command("Close", "Close a file by its name")]
public class CloseCommand(string fileName) : ICommand
{
    public void Execute(ConsoleApplicationContext ctx)
    {
        var massManager = ctx.Inject<MassManager>()!;

        if (massManager.ExistsMass(fileName))
        {
            massManager.UnloadMassFile(fileName);
            ctx.PrintLine($"File: {fileName} closed successfully.");
        }
        else throw new NoSuchFileException(fileName);
    }
}

[Command("View", "View the item of your given id in a file")]
public class ViewCommand(string fileName, string id) : ICommand
{
    private readonly int _id = int.Parse(id);

    public void Execute(ConsoleApplicationContext ctx) =>
        LangExt.WrappedTry("Cannot retrieve object", () =>
        {
            var mass = ctx.Inject<MassManager>()!.GetMassFileOrThrow(fileName);

            var typename = mass.ItemAdaptions[mass.ItemBlobs[_id].AdaptionId].DataJavaClz.JavaClassToRyoType();
            var item = mass.Get<object>(_id).Data;
            var itemName = mass.IdStrPairs.Where(pair => pair.Value == _id).Select(pair => pair.Key).FirstOrDefault();

            var newtonJson = item.ToJson();

            ctx.PrintLine(
                $"{(itemName != null ? $"Item Name: {itemName}" : "This is a sub-item, no item name")} Data original type: {typename}\n\nBuilt-in reader:\n{item.ToJsonWithInternalAlgorithm()}\n\nNewtonsoft reader:\n{newtonJson}");

            var dump = ctx.ReadYesOrNo("\nHaha, do you want to dump");

            if (!dump) return;

            var dumpPath = ctx.ReadLine("\nInput dump directory");
            if (!string.IsNullOrEmpty(dumpPath))
            {
                Directory.CreateDirectory(dumpPath); // 检测文件夹纯真

                itemName ??= ctx.ReadLine("\nName for the project"); // 无名需要命名

                if (!string.IsNullOrEmpty(itemName))
                {
                    dumpPath = dumpPath + "/" + itemName.Replace('/', '_');

                    // 序列化为Json
                    var jsonFilePath = dumpPath + ".json";
                    using var jsonFile = FileUtils.OpenFile(jsonFilePath, true, true);
                    jsonFile.Write(Encoding.UTF8.GetBytes(newtonJson));
                    ctx.PrintLine($"\nJson file saved to {jsonFilePath}");

                    ctx.PrintLine("\nDumped.");
                }
                else ctx.PrintLine("Filename is not correct, no dumping.");
            }
            else ctx.PrintLine("Dump directory is not correct, no dumping.");
        });
}

[Command("Copy", "Copy the item of your given id in a file")]
public class CopyCommand(string fileName, string id) : ICommand
{
    private readonly int _id = int.Parse(id);

    public void Execute(ConsoleApplicationContext ctx) =>
        LangExt.WrappedTry("Cannot copy object", () =>
        {
            var mass = ctx.Inject<MassManager>()!.GetMassFileOrThrow(fileName);

            var newId = mass.Copy(_id);

            ctx.PrintLine($"Item {_id} copied to {newId} successfully.");
        });
}

[Command("CopyByName", "Copy the item of your given name in a file")]
public class CopyByNameCommand(string fileName, string oldName, string newName) : ICommand
{
    public void Execute(ConsoleApplicationContext ctx) =>
        LangExt.WrappedTry("Cannot copy object", () =>
        {
            var mass = ctx.Inject<MassManager>()!.GetMassFileOrThrow(fileName);

            mass.Copy(oldName, newName);

            ctx.PrintLine($"Item {oldName} copied to {newName} successfully.");
        });
}

[Command("Search", "Search the target item in a file")]
public class SearchCommand(string fileName, string searchName) : ICommand
{
    public void Execute(ConsoleApplicationContext ctx) =>
        LangExt.WrappedTry("Cannot locate object", () =>
        {
            var mass = ctx.Inject<MassManager>()!.GetMassFileOrThrow(fileName);

            var map = mass.IdStrPairs;
            ctx.PrintLine("Search Results:");
            foreach (var item in map)
            {
                var itBlob = mass.ItemBlobs[item.Value];
                var adaTion = mass.ItemAdaptions[itBlob.AdaptionId];
                if (item.Key.Contains(searchName, StringComparison.CurrentCultureIgnoreCase))
                    ctx.PrintLine(
                        $"Id.{item.Value} Name：{item.Key} Size：{itBlob.Data?.Length ?? -1} Type：{adaTion.DataJavaClz} Adapter：{adaTion.AdapterJavaClz}");
            }
        });
}

[Command("Write", "Write the file to the path you given")]
public class WriteCommand(string fileName, string pathName) : ICommand
{
    public void Execute(ConsoleApplicationContext ctx) =>
        LangExt.WrappedTry("Cannot save object", () =>
        {
            var mass = ctx.Inject<MassManager>()!.GetMassFileOrThrow(fileName);

            using var fileStream = FileUtils.OpenFile(pathName, true, true);
            mass.Save(fileStream);

            ctx.PrintLine("Saved");
        });
}

[Command("Delete", "Delete an item by its id from a file")]
public class DeleteCommand(string fileName, string id) : ICommand
{
    private readonly int _id = int.Parse(id);

    public void Execute(ConsoleApplicationContext ctx) =>
        LangExt.WrappedTry("Cannot delete object", () =>
        {
            var mass = ctx.Inject<MassManager>()!.GetMassFileOrThrow(fileName);

            var itemName = mass.IdStrPairs.Where(pair => pair.Value == _id).Select(pair => pair.Key).FirstOrDefault();

            var result = mass.Remove(_id);

            ctx.PrintLine($"Item {itemName ?? $"No.{_id}"} deleted: {result}");
        });
}

[Command("GC", "Perform GC to a file")]
public class GcCommand(string fileName) : ICommand
{
    public void Execute(ConsoleApplicationContext context) =>
        LangExt.WrappedTry("Cannot GC a file", () =>
        {
            var mass = context.Inject<MassManager>()!.GetMassFileOrThrow(fileName);

            mass.CollectGarbage();

            context.PrintLine("GC performed");
        });
}

[Command("ImportDialogueTree", "Import dialogue tree from a json file into a file, only for PipeDreams now")]
public class ImportDialogueTreeCommand(string fileName, string jsonFilePath, string itemName) : ICommand
{
    public void Execute(ConsoleApplicationContext ctx) =>
        LangExt.WrappedTry("Parse failed", () =>
        {
            var mass = ctx.Inject<MassManager>()!.GetMassFileOrThrow(fileName);

            // TODO：创建通用的拨弄 我们得约定一个输出格式 才能输入

            var msgText = File.ReadAllText(jsonFilePath);

            var msg = msgText.JsonToObject<DialogueTreeDescriptor>() ??
                      throw new NullReferenceException("序列化Json失败，请检查你的输入（注：Json中正常的“\"”请用“\\\"”转义）");

            var result = mass.Add(itemName, msg);

            ctx.PrintLine($"Added, mode is {result.ToString().MakeFirstCharLower()}");
        });
}

// For Testing Purpose
[Command("R2D", "Replace to PipeDreams' Dialogue Tree for Id", true)]
public class R2DCommand(string fileName, string jsonFilePath, string itemId) : ICommand
{
    private readonly int _itemId = int.Parse(itemId);

    public void Execute(ConsoleApplicationContext ctx) =>
        LangExt.WrappedTry("Parse failed", () =>
        {
            var mass = ctx.Inject<MassManager>()!.GetMassFileOrThrow(fileName);

            var msgText = File.ReadAllText(jsonFilePath);

            var msg = msgText.JsonToObject<DialogueTreeDescriptor>() ??
                      throw new NullReferenceException("序列化Json失败，请检查你的输入（注：Json中正常的“\"”请用“\\\"”转义）");

            mass.Set(_itemId, msg);

            ctx.PrintLine("Replaced");
        });
}

[Command("OperateDialogueTree", "Only for PipeDreams now")]
public class OperateDialogueTreeCommand(string fileName, string id) : ICommand
{
    private readonly int _id = int.Parse(id);

    private static string Edit(ConsoleApplicationContext ctx, string ori)
    {
        while (true)
        {
            var doTrans = ctx.ReadYesOrNo("Do you want to edit it", ConsoleKey.Enter, ConsoleKey.DownArrow);

            if (!doTrans) return ori;
            var str = ctx.ReadLine("Content");

            if (ctx.ReadYesOrNo("Save changes", ConsoleKey.Enter, ConsoleKey.Backspace)) return str;
        }
    }

    public void Execute(ConsoleApplicationContext ctx)
    {
        LangExt.WrappedTry("Operating Dialogue Tree encountered an issue", () =>
            {
                var mass = ctx.Inject<MassManager>()!.GetMassFileOrThrow(fileName);

                var dialogue = mass.Get<DialogueTreeDescriptor>(_id).Data;

                var dialogueItemName = mass.IdStrPairs.Where(pair => pair.Value == _id).Select(pair => pair.Key).FirstOrDefault();

                ctx.PrintLine(
                    $"{(dialogueItemName != null ? $"Item Name: {dialogueItemName}" : "This is a sub-item, no item name")} Dialogue Tree Namespace: {dialogue.DialogueNameSpace} Conversation Count: {dialogue.ConversationList.Count}");

                var applyEdit = dialogueItemName != null && ctx.ReadYesOrNo("Apply edit mode");

                var no = 1;
                foreach (var conv in dialogue.ConversationList)
                {
                    ctx.PrintLine(
                        $"--------\nDialogue Part {no}:\nTags: {conv.Tags.ToJson()}\nTags to lock: {conv.TagsToLock.ToJson()}\nTags to unlock: {conv.TagsToUnlock.ToJson()}\nStatus: {conv.Status} Unread: {conv.StateOfDiswatch} Trigger: {conv.Trigger}");
                    int all = conv.SenderMessagers.Count;
                    ctx.PrintLine($"\nSender's Conversation Count: {all}");
                    int no2 = 1;
                    foreach (var send in conv.SenderMessagers)
                    {
                        ctx.PrintLine(
                            $"----\nSender's Conversation {no2}/{all}:\nDate Text: {send.DateText} Time Text: {send.TimeText} Original: {send.Origin}\nTrigger: {send.Trigger} Trigger Time: {send.TriggerTime}\nIdle Time: {send.IdleTime} Typing Time: {send.TypingTime}:\n{send.Message}");
                        if (applyEdit) send.Message = Edit(ctx, send.Message); // else send.Message += "日恁毛";
                        no2++;
                    }

                    all = conv.UserMessages.Count;
                    ctx.PrintLine($"\nUser's Conversation Count: {all}");
                    no2 = 1;
                    foreach (var user in conv.UserMessages)
                    {
                        ctx.PrintLine($"----\nUser's Conversation {no2}/{all} Hidden: {user.IsHidden}:\n{user.Message.Trim()}");
                        if (applyEdit) user.Message = Edit(ctx, user.Message); // else user.Message += "物支浪了吸";
                        no2++;
                    }

                    no++;
                }

                if (!applyEdit || !ctx.ReadYesOrNo("\n--------\nSave all changes")) return;

                mass.Add(dialogueItemName!, dialogue);
                ctx.PrintLine("Changes saved.");
            },
            new Dictionary<Type, string>
                { { typeof(InvalidCastException), "The item got by the id you given is not a Dialogue Tree item" } });
    }
}

// -- 外部 --

[Command("Inflate", "Inflate a outside file")]
public class InflateCommand : ICommand
{
    private enum FileType
    {
        FileSystem,
        TextureFile
    }

    private readonly FileType _fileType = FileType.FileSystem;
    private readonly string _fileName;

    public InflateCommand(string fileName)
    {
        _fileName = fileName;
    }

    public InflateCommand(string fileName, string fileType)
    {
        _fileName = fileName;
        _fileType = (FileType)int.Parse(fileType);
    }

    public void Execute(ConsoleApplicationContext ctx)
    {
        using var fileStream = FileUtils.OpenFile(_fileName);
        Mass mass = _fileType switch
        {
            FileType.FileSystem => new MassFile(),
            FileType.TextureFile => new TextureFile(),
            _ => throw new NotSupportedException("This file type is not supported at the moment."),
        };
        mass.Load(fileStream);

        var fileName = _fileName + "_inflated";
        using var writer = FileUtils.OpenFile(fileName, true, true);
        mass.Save(writer, false);

        ctx.PrintLine($"Write out to {fileName}.");
    }
}

// TODO：未来按新TxFile优化
[Command("UnpackImage", "Unpack the images from a texture file.")]
public class UnpackImageCommand : ICommand
{
    private enum Mode
    {
        FullInfoAndDump,
        HighestComposed
    }

    private readonly string _fileName;
    private readonly Mode _mode = Mode.HighestComposed;

    public UnpackImageCommand(string fileName) => _fileName = fileName;

    public UnpackImageCommand(string fileName, string mode)
    {
        _fileName = fileName;
        _mode = (Mode)int.Parse(mode);
    }

    public void Execute(ConsoleApplicationContext ctx)
    {
        LangExt.WrappedTry("Couldn't unpack image", () =>
        {
            using var stream = FileUtils.OpenFile(_fileName);

            var fileName = Path.GetFileNameWithoutExtension(stream.Name);

            var textureFile = new TextureFile();
            textureFile.Load(stream);

            switch (_mode)
            {
                // HACK:罗里吧嗦 解包还是蛮麻烦，多种张图片，多种格式，多层剪辑 我测你们妈
                case Mode.FullInfoAndDump:
                {
                    ctx.PrintLine($"名称：{fileName.ToUpper()}\n\n信息如下：\n");

                    ctx.PrintLine(_fileName.ToUpper() + "的索引信息：\n");
                    ctx.PrintLine($"图片碎片数：{textureFile.ItemBlobs.Count}");

                    ctx.PrintLine($"\n图片模式适配项数：{textureFile.ItemAdaptions.Count}");
                    foreach (var item in textureFile.ItemAdaptions)
                        ctx.PrintLine($"-- 数据类型：{item.DataJavaClz} 适配器：{item.AdapterJavaClz}");

                    ctx.PrintLine($"\n图片项数：{textureFile.ImageIDsArray.Count}");
                    for (var i = 0; i < textureFile.ImageIDsArray.Count; i++)
                        ctx.PrintLine($"-- No.{i + 1} 对应的ID：[{DataSerializationUtils.ToJson(textureFile.ImageIDsArray[i])}]");

                    if (textureFile.ImageIDsArray.Count == 0) return;

                    ctx.PrintLine("\n解析各项图片：");

                    int piece = 1;
                    for (int i = 0; i < textureFile.ImageIDsArray.Count; i++)
                    {
                        int[] items = textureFile.ImageIDsArray[i].ToArray();

                        foreach (int id in items)
                        {
                            var imageBlob = textureFile.Get<FragmentalImage>(id).Data;

                            //LogUtil.INSTANCE.PrintInfo($"\n总片数：{imageBlob.ClipCount}，总层数：{imageBlob.SliceWidths.Length}，解析各项图片：");

                            if (imageBlob.ClipSize == 0) continue;

                            ctx.PrintLine(
                                $"-- No.{piece} 属于第{i + 1}格式 最大碎片长宽：[{imageBlob.ClipSize}] 层级高：[{imageBlob.LevelHeights.ToJson()}] 层级宽：[{imageBlob.LevelWidths.ToJson()}] 层级数：{imageBlob.RyoPixmaps.Length}");
                            var pathName = _fileName + $" No_{piece} Dumps";
                            ctx.PrintLine("-- 该图片的相关资源将被写出在：" + pathName);
                            if (!Directory.Exists(pathName)) Directory.CreateDirectory(pathName);

                            for (var level = 0; level < imageBlob.RyoPixmaps.Length; level++)
                            {
                                RyoPixmap[] pixs = imageBlob.RyoPixmaps[level];
                                ctx.PrintLine($"---- 第{level + 1}层 有{pixs.Length}个");

                                var levelPathName = pathName + $"\\Lv_{level + 1}";
                                if (!Directory.Exists(levelPathName)) Directory.CreateDirectory(levelPathName);
                                for (int no = 0; no < pixs.Length; no++)
                                {
                                    RyoPixmap pix = pixs[no];
                                    ctx.PrintLine($"------ 第{no + 1}个 类型：{pix.Format}");

                                    string levelFileName = $"{levelPathName}\\No_{no + 1}.{(pix.IsJPG ? "jpg" : "png")}";

                                    try
                                    {
                                        var it = pix.ToImage() ?? throw new RyoException("Fail to convert this image");

                                        it.Save(levelFileName);
                                    }
                                    catch (Exception ex)
                                    {
                                        ctx.PrintLine($"Cannot dump this image due to {ex.Message.MakeFirstCharLower}.");
                                    }
                                }
                            }

                            piece++;
                        }
                    }

                    break;
                }

                case Mode.HighestComposed:
                {
                    var fragmentalImage = textureFile.Get<FragmentalImage>(textureFile.ImageIDsArray.First().First()).Data ??
                                          throw new FileNotFoundException("无法找到默认图片");

                    Image outputImage = fragmentalImage.ToImage();

                    // 保存拼接好的图像
                    string savePath = _fileName.Replace(".texture", "");
                    if (fragmentalImage.RyoPixmaps.First().First().IsJPG)
                    {
                        ctx.PrintLine("该文件实际上被转存为了Jpg");

                        if (savePath.Contains(".png")) savePath = savePath.Replace(".png", ".jpg");
                    }

                    outputImage.Save(savePath);
                    ctx.PrintLine("写出成功，路径：" + savePath);

                    break;
                }

                default:
                    throw new NotImplementedException("No such mode of your given id" + _mode);
            }
        });
    }
}

// TODO：未来按新TxFile优化
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

    public void Execute(ConsoleApplicationContext ctx)
    {
        LangExt.WrappedTry("Couldn't pack image to a \".texture\"", () =>
        {
            using FileStream fileStream = FileUtils.OpenFile(ImgPath);

            FragmentalImage image = TextureUtils.FastCreateImage(fileStream);

            var txfile = new TextureFile();
            txfile.ImageIDsArray.Add(new() { txfile.Add(image) });

            using FileStream saveStream = FileUtils.OpenFile(FileName, true, true, false);
            txfile.Save(saveStream);

            ctx.PrintLine("Saved to " + FileName);
        });
    }
}