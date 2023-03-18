using ICSharpCode.SharpZipLib.Zip.Compression;
using Org.CNWeak.Ryo.Commands;
using Org.CNWeak.Ryo.IO;
using Org.CNWeak.Ryo.Mass;
using Org.CNWeak.Ryo.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Org.CNWeak.Ryo
{
    public class ProgramMainBlob
    {
        static void Main(string[] args)
        {
            LogUtil.INSTANCE.SetLogger(str => Console.WriteLine(str));

            if (args.Length > 0)
            {
                CommandManager.INSTANCE.RunningWithArgs = true;
                CommandManager.INSTANCE.ParseCommand(args);
                return;
            }

            Console.WriteLine($"Ryo Console[Version {Assembly.GetExecutingAssembly().GetName().Version}]\nCopyright (C) CNWeak Organization. All rights reserved.");
            while (true)
            {
                Console.Write("\nW:\\Ryo\\User>");
                string input = Console.ReadLine()!.Trim();
                if (input.ToLower() == "exit") break;

                CommandManager.INSTANCE.ParseCommandLine(input);
            }
        }
    }

    namespace Commands
    {
        public class CommandManager
        {
            public static CommandManager INSTANCE { get { instance ??= new(); return instance; } }
            private static CommandManager? instance;

            public readonly Dictionary<CommandAttribute, Type> commands = new();
            public bool RunningWithArgs = false;

            public void RegCmds()
            {
                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
                foreach (var type in types)
                {
                    var attribute = type.GetCustomAttribute<CommandAttribute>();
                    if (attribute != null && typeof(ICommand).IsAssignableFrom(type))
                    {
                        // DEBUG逻辑
                        // UsefulUtils.INSTANCE.PrintInfo("标识符", (attribute == null ? "就是" : "不是"), "类型", (type == null ? "就是" : "不是"));
                        commands.Add(attribute, type);
                    }
                }
            }

            public CommandManager()
            {
                RegCmds();
            }

            public void ParseCommandLine(string input)
            {
                // 解析命令行参数
                string pattern = @"([^""]\S*|"".+?"")\s*";
                MatchCollection matches = Regex.Matches(input, pattern);
                string[] cmdArgs = new string[matches.Count];
                for (int i = 0; i < matches.Count; i++)
                {
                    cmdArgs[i] = matches[i].Value.Trim();
                    if (cmdArgs[i].StartsWith("\"") && cmdArgs[i].EndsWith("\""))
                    {
                        cmdArgs[i] = cmdArgs[i][1..^1];
                    }
                }
                ParseCommand(cmdArgs);
            }

            public void ParseCommand(params string[] args)
            {
                // 解析命令并执行相关操作
                //Console.WriteLine("解析命令：");
                if (args == null || args.Length == 0)
                {
                    LogUtil.INSTANCE.PrintInfo("命令为空，输入Help查看支持的命令。");
                    return;
                }

                var givenCmdName = args[0].ToLower();
                var cmdArgs = args.Skip(1).ToArray();
                foreach (var cmd in commands)
                {
                    if (givenCmdName == cmd.Key.Name.ToLower())
                    {
                        var constructors = cmd.Value.GetConstructors();
                        foreach (var constructor in constructors)
                        {
                            var parameters = constructor.GetParameters();
                            if (cmdArgs.Length == parameters.Length)
                            {
                                var command = (ICommand)constructor.Invoke(cmdArgs);
                                command.Execute();
                                return;
                            }
                        }
                        LogUtil.INSTANCE.PrintInfo($"The parameter is incorrect. Usage of {cmd.Key.Name}: {cmd.Key.Help}");
                        return;
                    }
                }
                LogUtil.INSTANCE.PrintInfo($"'{givenCmdName}' is not recognized as an available command, enter 'Help' for more information.");
            }
        }

        [AttributeUsage(AttributeTargets.Class)]
        public class CommandAttribute : Attribute
        {
            public string Name { get; set; }
            public string Help { get; set; }
            public bool IsDev { get; set; }

            public CommandAttribute(string name, string help)
            {
                Name = name;
                Help = help;
            }
            public CommandAttribute(string name, string help, bool isDev)
            {
                Name = name;
                Help = help;
                IsDev = isDev;
            }
        }

        public interface ICommand
        {
            void Execute();
        }

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
                try
                {
                    var stream = new FileStream(PathString, FileMode.Open);
                    if (stream.Length == 0) throw new Exception("文件长度为零");

                    var fileName = Path.GetFileNameWithoutExtension(stream.Name);
                    //LogListener("正在加载：" + fileName);

                    var mass = new MassFile(fileName);

                    mass.Load(stream, "FileSystem");

                    if (mass.ReadyToUse)
                    {
                        LogUtil.INSTANCE.PrintInfo("已载入，索引信息如下：\n");
                        CommandManager.INSTANCE.ParseCommand("Info", fileName);
                        LogUtil.INSTANCE.PrintInfo($"\n档案已载入，名称：{fileName.ToUpper()}，稍后可凭借该名称Dump文件、查看索引信息、进行增删改查等操作。");
                    }
                    else
                    {
                        LogUtil.INSTANCE.PrintInfo("载入失败，文件无效，请检查您指定的文件是否存在、正确、未损坏。");
                        CommandManager.INSTANCE.ParseCommand("Unload", fileName);
                    }
                }
                catch (Exception e)
                {
                    LogUtil.INSTANCE.PrintError("载入文件失败", e);
                }
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
                var mass = MassManager.INSTANCE.MassList.Find((MassFile m) => m.Name.ToLower() == FileName.ToLower());
                if (mass != null && mass.ReadyToUse)
                {
                    LogUtil.INSTANCE.PrintInfo(FileName.ToUpper() + "的索引信息：\n");
                    LogUtil.INSTANCE.PrintInfo($"碎片数据数：{mass.ObjCount}");
                    for (var i = 0; i < mass.ObjCount; i++) LogUtil.INSTANCE.PrintInfo($"-- Id.{i} 适配项ID：{mass.DataAdapterIdArray[i]} 已压缩：{(mass.IsItemDeflatedArray[i] ? "是" : "否")} 起始索引：{(i == 0 ? 0 : mass.EndPosition[i - 1])} 长度：{mass.EndPosition[i] - (i == 0 ? 0 : mass.EndPosition[i - 1])} 粘连数据：{mass.StickedDataList[i]}");
                    LogUtil.INSTANCE.PrintInfo($"\n粘连元数据数：{mass.StickedMetaDataItemCount}，以下为粘连元数据");
                    int currentIndex = 0;
                    while (currentIndex < mass.StickedMetaDataItemCount)
                    {
                        List<string> str = new();
                        for (int i = 0; i < 4 && currentIndex < mass.StickedMetaDataItemCount; i++)
                        {
                            str.Add("No." + (currentIndex + 1) + "：" + mass.StickedMetaDataList[currentIndex]);
                            currentIndex++;
                        }
                        LogUtil.INSTANCE.PrintInfo(str.ToArray());
                    }
                    LogUtil.INSTANCE.PrintInfo($"\n数据适配项数：{mass.MyRegableDataAdaptionList.Count}");
                    foreach (var item in mass.MyRegableDataAdaptionList) LogUtil.INSTANCE.PrintInfo($"-- Id.{item.Id} 数据类型：{item.DataType} 适配器：{item.AdapterType}");
                    LogUtil.INSTANCE.PrintInfo($"\n正式数据项数：{mass.MyIdStrMap.Count}");
                    foreach (var item in mass.MyIdStrMap) LogUtil.INSTANCE.PrintInfo($"-- Id.{item.Value} Name：{item.Key}");
                }
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
                MassManager.INSTANCE.MassList.Remove(MassManager.INSTANCE.MassList.Find(a => a.Name == FileName)!);
            }
        }

        [Command("Dump", "Dump the objects blob of a file")]
        public class DumpCommand : ICommand
        {
            public string FileName;
            public DumpCommand(string str)
            {
                FileName = str;
            }
            public void Execute()
            {
                var mass = MassManager.INSTANCE.GetMassFileByFileName(FileName);
                if (mass != null && mass.ReadyToUse)
                {
                    LogUtil.INSTANCE.PrintInfo($"找到文件：{FileName.ToUpper()}");
                    LogUtil.INSTANCE.PrintInfo(mass.DumpBuffer());
                }
                else LogUtil.INSTANCE.PrintInfo($"找不到文件：{FileName.ToUpper()}，请查看拼写是否正确，文件是否已正确加载？");
            }
        }

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
        public class ReadCommand : ICommand
        {
            public string FileName;
            public int Id;
            public ReadCommand(string fileName, string id)
            {
                FileName = fileName;
                Id = int.Parse(id);
            }
            public void Execute()
            {
                var mass = MassManager.INSTANCE.GetMassFileByFileName(FileName);
                try
                {
                    if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");
                    var buffer = mass!.GetItemById<byte[]>(Id);
                    if (buffer == null || buffer.Length == 0) throw new Exception("请求的对象为空");
                    if (string.IsNullOrWhiteSpace(mass.FullPath)) throw new Exception("保存路径为空");

                    using (FileStream fs = new(mass.FullPath + $".{Id}.dump", FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(buffer, 0, buffer.Length);
                    }
                    LogUtil.INSTANCE.PrintInfo("保存成功，路径：" + mass.FullPath + $".{Id}.dump");
                }
                catch (Exception ex)
                {
                    LogUtil.INSTANCE.PrintError($"不能写出对象，因为{ex.Message}", ex);
                }
            }
        }
    }

    namespace Utils
    {
        public class LogUtil
        {
            public static LogUtil INSTANCE { get { instance ??= new(); return instance; } }
            private static LogUtil? instance;

            private Action<string> Logger = str => Trace.WriteLine(str);

            public void SetLogger(Action<string> logger) => Logger = logger;
            public void PrintError(String info, Exception e)
            {
                Logger(info + " " + e.Message);
                Logger("堆栈：" + e.StackTrace);
            }

            public void PrintInfo(params string[] args)
            {
                Logger(string.Join(' ', args));
            }
        }

        public class CompressionUtil
        {
            public static CompressionUtil INSTANCE { get { instance ??= new(); return instance; } }
            private static CompressionUtil? instance;

            public byte[] Inflate(byte[] input, int offset, int length)
            {
                try
                {
                    var outStream = new MemoryStream();
                    var inflater = new Inflater();
                    inflater.SetInput(input, offset, length);
                    var buffer = new byte[1024];
                    while (!inflater.IsFinished)
                    {
                        int count = inflater.Inflate(buffer);
                        outStream.Write(buffer, 0, count);
                    }

                    return outStream.ToArray();
                }
                catch (Exception e)
                {
                    LogUtil.INSTANCE.PrintError("解压出错", e);
                    return Array.Empty<byte>();
                }
            }
        }
    }

    namespace IO
    {
        public class InputStreamReader : IDisposable
        {
            private BinaryReader Reader;
            public long RestLength
            {
                get => Length - Position;
            }
            public long Length
            {
                get => Reader.BaseStream.Length;
            }
            public long Position
            {
                get => Reader.BaseStream.Position;
            }
            public byte[] Buffer { get; set; }

            public InputStreamReader(Stream inputStream)
            {
                Reader = new BinaryReader(inputStream);
            }

            public byte[] ReadBytes(int length)
            {
                return Reader.ReadBytes(length);
            }

            public int ReadInt()
            {
                int result = BitConverter.ToInt32(Reader.ReadBytes(4).Reverse().ToArray(), 0);
                return result;
            }

            public string ReadBytesToHexString(int length)
            {
                byte[] bytes = Reader.ReadBytes(length);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(string.Format("{0:X2}", bytes[i]));
                    if (i < bytes.Length - 1)
                    {
                        sb.Append("-");
                    }
                }

                return sb.ToString();
            }

            public string ReadString()
            {
                int length = ReadInt();
                return ReadString(length);
            }
            public string ReadString(int length)
            {
                byte[] bytes = Reader.ReadBytes(length);
                return Encoding.UTF8.GetString(bytes);
            }

            public bool CheckHasString(string str) => ReadString(Encoding.UTF8.GetBytes(str).Length) == str;

            public void Dispose()
            {
                Reader.Dispose();
            }

            public byte[] ReadAllBytes() => Reader.ReadBytes((int)RestLength);
        }

        public class OutputStreamWriter { }
    }

    namespace Mass
    {
        public class MassFile
        {
            public string Name = "";
            public string FullPath = "";

            public InputStreamReader Reader = new(Stream.Null);
            public OutputStreamWriter Writer = new();

            public int ObjCount = 0;
            public int CurrentObjCount = 0;
            public List<int> DataAdapterIdArray = new();
            public List<int> EndPosition = new();
            public List<bool> IsItemDeflatedArray = new();
            public List<int> StickedDataList = new();
            public List<int> StickedMetaDataList = new();
            public byte[] RealItemDataBuffer = Array.Empty<byte>();
            public byte[] WorkBuffer = Array.Empty<byte>();
            public Dictionary<string, int> MyIdStrMap = new();
            public int StickedMetaDataItemCount = 0;
            public List<RegableDataAdaption> MyRegableDataAdaptionList = new();

            public bool ReadyToUse = true;
            public bool IsEmpty = true;
            public int SavedStickedDataIntArrayIdMinusOne = -1;
            public int SavedStickedDataIntArrayId = -1;
            public int SavedId = -1;

            public class RegableDataAdaption
            {
                public string DataType { get; set; }
                public string AdapterType { get; set; }
                public int Id { get; set; }

                public RegableDataAdaption(int id, string dataType, string adapterType)
                {
                    Id = id;
                    DataType = dataType;
                    AdapterType = adapterType;
                }
            }

            public T? GetItemById<T>(int id)
            {
                if (!ReadyToUse) throw new Exception("文件未准备好");

                bool isItemDeflated = IsItemDeflatedArray[id];
                int dataAdapterId = DataAdapterIdArray[id];

                //Class <?> theObjTypeShouldBe = 方法_取已序列化的类(idOfXuLieHuaQi);
                //接口_序列化器 <?> xuliehuaqi = 方法_取序列化器(idOfXuLieHuaQi);

                byte[] oldWorkBuffer = WorkBuffer;
                /*byte[] bufferOfInputStream = Reader.Buffer;
                int inputReaderPosition = (int)Reader.Position;
                int inputReaderLimit = (int)Reader.Length;*/
                int oldStickedDataIntArrayIdMinusOne = SavedStickedDataIntArrayIdMinusOne;
                int oldStickedDataIntArrayId = SavedStickedDataIntArrayId;
                int oldIdSaves = SavedId;

                int blobStartPosition = id == 0 ? 0 : EndPosition[id - 1];
                int blobLength = EndPosition[id] - blobStartPosition;

                SavedId = id;
                SavedStickedDataIntArrayIdMinusOne = id == 0 ? 0 : StickedDataList[id - 1];
                SavedStickedDataIntArrayId = StickedDataList[id];

                //LogUtil.INSTANCE.PrintInfo("ID", id.ToString(), "起始", blobStartPosition.ToString(), "长度", blobLength.ToString(), "3-1", SavedStickedDataIntArrayIdMinusOne.ToString(), "3", SavedStickedDataIntArrayId.ToString());

                WorkBuffer = isItemDeflated ? CompressionUtil.INSTANCE.Inflate(RealItemDataBuffer.Skip(blobStartPosition).Take(blobLength).ToArray(), 0, blobLength) : RealItemDataBuffer.Skip(blobStartPosition).Take(blobLength).ToArray();

                if (typeof(T) == typeof(byte[])) return (T)(object)WorkBuffer;
                else
                {
                    //TODO:拨弄构建李建新
                }


                WorkBuffer = oldWorkBuffer;
                return default;
            }

            public MassFile(string name)
            {
                Name = name;
                MassManager.INSTANCE.MassList.Add(this);
            }

            public void Clean()
            {
                ReadyToUse = false;
                try
                {
                    Reader = new(Stream.Null);
                    Writer = new();
                    ObjCount = 0;
                    CurrentObjCount = 0;
                    DataAdapterIdArray = new();
                    EndPosition = new();
                    StickedDataList = new();
                    StickedMetaDataList = new();
                    StickedMetaDataItemCount = 0;
                    RealItemDataBuffer = Array.Empty<byte>();
                    WorkBuffer = Array.Empty<byte>();

                    SavedStickedDataIntArrayId = -1;
                    SavedStickedDataIntArrayIdMinusOne = -1;
                    SavedId = -1;
                    IsEmpty = true;
                    ReadyToUse = true;
                }
                catch (Exception e)
                {
                    LogUtil.INSTANCE.PrintError("加载Mass失败", e);
                }
            }

            public void Load(FileStream file, string format)
            {
                FullPath = file.Name;

                if (!IsEmpty) Clean();

                ReadyToUse = false;
                try
                {
                    Reader = new InputStreamReader(file);

                    var isCorrectFormat = Reader.CheckHasString(format);
                    if (!isCorrectFormat) return;

                    var num = Reader.ReadInt();
                    var isDeflated = (num & 1) != 0;
                    var deflateLen = num >> 1;
                    var indexDataBlob = isDeflated ? CompressionUtil.INSTANCE.Inflate(Reader.ReadBytes(deflateLen), 0, deflateLen) : Reader.ReadBytes(deflateLen);

                    if (indexDataBlob != null && indexDataBlob.Length != 0)
                    {
                        using var inflatedDataReader = new InputStreamReader(new MemoryStream(indexDataBlob));
                        ObjCount = inflatedDataReader.ReadInt();
                        CurrentObjCount = ObjCount;

                        for (var i = 0; i < ObjCount; i++)
                        {
                            int j = inflatedDataReader.ReadInt();
                            DataAdapterIdArray.Add(j >> 1);
                            IsItemDeflatedArray.Add((j & 1) != 0);
                        }

                        for (var i = 0; i < ObjCount; i++)
                            EndPosition.Add(inflatedDataReader.ReadInt());

                        for (var i = 0; i < ObjCount; i++)
                            StickedDataList.Add(inflatedDataReader.ReadInt());

                        StickedMetaDataItemCount = StickedDataList.Last();

                        for (var i = 0; i < StickedMetaDataItemCount; i++)
                            StickedMetaDataList.Add(inflatedDataReader.ReadInt());

                        var regCount = inflatedDataReader.ReadInt();
                        for (var i = 0; i < regCount; i++)
                        {
                            var id = inflatedDataReader.ReadInt();
                            var str1 = inflatedDataReader.ReadString();
                            var str2 = inflatedDataReader.ReadString();
                            MyRegableDataAdaptionList.Add(new RegableDataAdaption(id, str1, str2));
                        }

                        var idStrMapCount = inflatedDataReader.ReadInt();
                        for (var i = 0; i < idStrMapCount; i++)
                        {
                            var str = inflatedDataReader.ReadString();
                            var id = inflatedDataReader.ReadInt();
                            MyIdStrMap.Add(str, id);
                        }
                    }
                    RealItemDataBuffer = Reader.ReadAllBytes();

                    ReadyToUse = true;
                    IsEmpty = false;
                }
                catch (Exception e)
                {
                    LogUtil.INSTANCE.PrintError("加载Mass失败", e);
                }
                finally
                {
                    file.Dispose();
                }
            }

            public string DumpBuffer()
            {
                if (!ReadyToUse || IsEmpty) return "未准备好或者内容为空";
                if (string.IsNullOrWhiteSpace(FullPath)) return "保存路径为空";
                var info = "保存";
                try
                {
                    using (FileStream fs = new(FullPath + ".dump", FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(RealItemDataBuffer, 0, RealItemDataBuffer.Length);
                    }
                    info += "成功，路径：" + FullPath + ".dump";
                }
                catch (Exception ex)
                {
                    LogUtil.INSTANCE.PrintError("保存时错误", ex);
                    info += "失败（悲";
                }
                return info;
            }
        }

        public class MassManager
        {
            public static MassManager INSTANCE { get { instance ??= new(); return instance; } }
            private static MassManager? instance;

            public List<MassFile> MassList = new();

            public MassFile? GetMassFileByFileName(string fileName) => MassList.Find((MassFile m) => m.Name.ToLower() == fileName.ToLower());
        }
    }
}