using ICSharpCode.SharpZipLib.Zip.Compression;
using Me.Earzu.Ryo.Commands;
using Me.Earzu.Ryo.Formats;
using Me.Earzu.Ryo.IO;
using Me.Earzu.Ryo.Mass;
using Me.Earzu.Ryo.Utils;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Me.Earzu.Ryo
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

            public CommandManager() => RegCmds();

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

                    mass.Load(stream);

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
                var mass = MassManager.INSTANCE.MassList.Find((MassBase m) => m.Name.ToLower() == FileName.ToLower());
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
                    if (!typeof(MassFile).IsInstanceOfType(mass)) return;
                    foreach (var item in mass.MyRegableDataAdaptionList) LogUtil.INSTANCE.PrintInfo($"-- Id.{item.Id} 数据类型：{item.DataType} 适配器：{item.AdapterType}");
                    LogUtil.INSTANCE.PrintInfo($"\n正式数据项数：{((MassFile)mass).MyIdStrMap.Count}");
                    foreach (var item in ((MassFile)mass).MyIdStrMap) LogUtil.INSTANCE.PrintInfo($"-- Id.{item.Value} Name：{item.Key}");
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
                    var typename = mass.MyRegableDataAdaptionList[mass.DataAdapterIdArray[Id]].DataType;
                    var item = mass.GetItemById(Id);
                    /*if (buffer == null || buffer.Length == 0) throw new Exception("请求的对象为空");
                    if (string.IsNullOrWhiteSpace(mass.FullPath)) throw new Exception("保存路径为空");*/

                    LogUtil.INSTANCE.PrintInfo($"数据类型：{typename}\n数据：{FormatManager.INSTANCE.ItemToString(item)}");

                    /*using (FileStream fs = new(mass.FullPath + $".{Id}.dump", FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(buffer, 0, buffer.Length);
                    }
                    LogUtil.INSTANCE.PrintInfo("保存成功，路径：" + mass.FullPath + $".{Id}.dump");*/
                }
                catch (Exception ex)
                {
                    LogUtil.INSTANCE.PrintError($"不能获取对象 因为", ex);
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
    }

    namespace Formats
    {
        public class FormatManager
        {
            public static FormatManager INSTANCE { get { instance ??= new(); return instance; } }
            private static FormatManager? instance;

            public Dictionary<ConverterAttribute, IConverter> converters = new();

            public FormatManager() => RegConverterProxies();

            public void RegConverterProxies()
            {
                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
                foreach (var type in types)
                {
                    var attribute = type.GetCustomAttribute<ConverterAttribute>();
                    if (attribute != null && typeof(IConverter).IsAssignableFrom(type)) converters.Add(attribute, (IConverter)Activator.CreateInstance(type)!);
                }
            }

            public IConverter GetConverterByName(string name)
            {
                foreach (var item in converters) if (name == item.Key.Name && item.Key.AllMatches) return item.Value;
                LogUtil.INSTANCE.PrintInfo($"没有对于{name}的精确匹配");
                foreach (var item in converters) if (name.StartsWith(item.Key.Name) && !item.Key.AllMatches) return item.Value;
                throw new NotSupportedException($"也没有初略匹配，故格式{name}暂不支持");
            }

            public object ParseItem(MassBase mass, MassBaseFormatReader reader, string type)
            {
                try
                {
                    return GetConverterByName(type).CovertToObject(mass, reader);
                }
                catch (Exception e)
                {
                    LogUtil.INSTANCE.PrintError("怎么回事呢：", e);
                }

                return reader.ReadAllBytes();
            }

            public string ItemToString(object item)
            {
                Type type = item.GetType();
                if (item == null) return "项目为Null";
                else if (type == typeof(int[]))
                {
                    int[] iArr = (int[])item;
                    return $"整数数组，长度：{iArr.Length}\n内容：{{{string.Join(",", iArr.Select(i => i.ToString()))}}}";
                }
                else if (type == typeof(byte[]))
                {
                    var it = (MassBaseFormatReader)(byte[])item;
                    return $"字节数组，长度：{it.Length}\n内容：{{{it.ReadBytesToHexString((int)it.Length)}}}";
                }
                else
                {
                    string? str = item.ToString();
                    return str == null ? "项目无内置转换，且强制转换结果为Null" : str!;
                }
            }
        }

        [AttributeUsage(AttributeTargets.Class)]
        public class ConverterAttribute : Attribute
        {
            public string Name { get; set; }
            public bool AllMatches { get; set; }

            public ConverterAttribute(string name)
            {
                Name = name;
                AllMatches = true;
            }

            public ConverterAttribute(string name, bool allMatches)
            {
                Name = name;
                AllMatches = allMatches;
            }
        }

        public interface IConverter
        {
            object CovertToObject(MassBase mass, MassBaseFormatReader reader);
            byte[] CovertToBytes(object item);
        }

        [Converter("java.lang.Integer")]
        public class IntConverter : IConverter
        {
            public byte[] CovertToBytes(object item)
            {
                throw new NotImplementedException();
            }

            public object CovertToObject(MassBase mass, MassBaseFormatReader reader) => reader.ReadInt();
        }

        [Converter("java.lang.String")]
        public class StringConverter : IConverter
        {
            public byte[] CovertToBytes(object item)
            {
                throw new NotImplementedException();
            }

            public object CovertToObject(MassBase mass, MassBaseFormatReader reader) => reader.ReadString();
        }

        [Converter("[I")]
        public class IntArrayConverter : IConverter
        {
            public byte[] CovertToBytes(object item)
            {
                throw new NotImplementedException();
            }

            public object CovertToObject(MassBase mass, MassBaseFormatReader reader)
            {
                int i = reader.ReadInt();
                int[] iArr = new int[i];
                for (int i2 = 0; i2 < i; i2++)
                {
                    iArr[i2] = reader.ReadInt();
                }
                return iArr;
            }
        }

        [Converter("[B")]
        public class ByteArrayConverter : IConverter
        {
            public byte[] CovertToBytes(object item)
            {
                throw new NotImplementedException();
            }

            public object CovertToObject(MassBase mass, MassBaseFormatReader reader)
            {
                return reader.ReadBytes(reader.ReadInt());
            }
        }

        [Converter("sengine.graphics2d.FontSprites")]
        public class FuqiFormatConverter : IConverter
        {
            public byte[] CovertToBytes(object item)
            {
                throw new NotImplementedException();
            }

            public object CovertToObject(MassBase mass, MassBaseFormatReader reader)
            {
                mass.Read<int[]>();
                mass.Read<byte[][]>();
                reader.ReadFloat();
                reader.ReadInt();
                return "成功😄✌";
            }
        }

        [Converter("[", false)]
        public class ObjectArrayConverter : IConverter
        {
            public byte[] CovertToBytes(object item)
            {
                throw new NotImplementedException();
            }

            public object CovertToObject(MassBase mass, MassBaseFormatReader reader)
            {
                object[] objArr = new object[reader.ReadInt()];
                mass.Reference(objArr);
                for (int i = 0; i < objArr.Length; i++) objArr[i] = mass.Read<object>();
                return objArr;
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
                Logger("堆栈：\n" + e.StackTrace);
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
        public class MassBaseFormatReader : IDisposable
        {
            private BinaryReader Reader;
            public long RestLength
            {
                get => Length - Position;
            }
            public long Length
            {
                get => Reader.BaseStream.Length; //不应该，应该是读缓冲区，虽然我没用
            }
            public long Position
            {
                get => Reader.BaseStream.Position; //不应该，应该是读缓冲区，虽然我没用
            }
            //public byte[] Buffer { get; set; }

            public MassBaseFormatReader(Stream inputStream)
            {
                Reader = new BinaryReader(inputStream);
            }

            public byte[] ReadBytes(int length)
            {
                return Reader.ReadBytes(length);
            }

            public int ReadInt() => BitConverter.ToInt32(Reader.ReadBytes(4).Reverse().ToArray(), 0);

            public string ReadBytesToHexString(int length)
            {
                byte[] bytes = Reader.ReadBytes(length);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(string.Format("{0:X2}", bytes[i]));
                    if (i < bytes.Length - 1)
                    {
                        sb.Append(',');
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

            public float ReadFloat() => BitConverter.ToSingle(ReadBytes(4).Reverse().ToArray(), 0);

            public static implicit operator MassBaseFormatReader(byte[] buffer)
            {
                return new MassBaseFormatReader(new MemoryStream(buffer));
            }
        }

        public class MassBaseFormatWriter { }

        public class UsefulBuffer
        {
            public byte[] Buffer;
            public int Length { get => Buffer.Length; }
            public int Limit { set; get; }
            public int Position { set; get; }

            public UsefulBuffer() => Buffer = Array.Empty<byte>();

            public UsefulBuffer(byte[] buffer) => Buffer = buffer;
        }
    }

    namespace Mass
    {
        public class MassFile : MassBase
        {
            // 常量
            public new const string EXTENDED_NAME = "FileSystem";

            // 成员变量
            public Dictionary<string, int> MyIdStrMap = new();

            public MassFile(string name) : base(name) { }

            public override void AfterLoadingIndex(MassBaseFormatReader inflatedDataReader)
            {
                // LogUtil.INSTANCE.PrintInfo("芝士");

                var idStrMapCount = inflatedDataReader.ReadInt();
                for (var i = 0; i < idStrMapCount; i++)
                {
                    var str = inflatedDataReader.ReadString();
                    var id = inflatedDataReader.ReadInt();
                    MyIdStrMap.Add(str, id);
                }
            }

            public void Load(FileStream file)
            {
                Load(file, EXTENDED_NAME);
            }
        }

        public class MassBase
        {
            // 常量
            public const string EXTENDED_NAME = "MASS";

            // 文件名和路径
            public string Name = "";
            public string FullPath = "";

            // 暂存读写器
            public MassBaseFormatReader Reader = new(Stream.Null);
            public MassBaseFormatWriter Writer = new();

            // 相关成员变量
            public int ObjCount = 0;
            public int CurrentObjCount = 0;
            public List<int> DataAdapterIdArray = new();
            public List<int> EndPosition = new();
            public List<bool> IsItemDeflatedArray = new();
            public List<int> StickedDataList = new();
            public List<int> StickedMetaDataList = new();
            public byte[] RealItemDataBuffer = Array.Empty<byte>();
            public UsefulBuffer WorkBuffer = new UsefulBuffer();
            public int StickedMetaDataItemCount = 0;
            public List<RegableDataAdaption> MyRegableDataAdaptionList = new();

            // 暂存变量
            public bool ReadyToUse = true;
            public bool IsEmpty = true;
            public int SavedStickedDataIntArrayIdMinusOne = -1;
            public int SavedStickedDataIntArrayId = -1;
            public int SavedId = -1;

            // 可注册项
            public class RegableDataAdaption
            {
                public string DataType { get; set; }
                public string AdapterType { get; set; }
                public int Id { get; set; }
                // TODO:类型适配器

                public RegableDataAdaption(int id, string dataType, string adapterType)
                {
                    Id = id;
                    DataType = dataType;
                    AdapterType = adapterType;
                }
            }

            // 根据ID获取项目
            public object GetItemById(int id)
            {
                if (!ReadyToUse || IsEmpty) throw new Exception("文件未准备好或为空");

                bool isItemDeflated = IsItemDeflatedArray[id];
                int dataAdapterId = DataAdapterIdArray[id];

                //Class <?> theObjTypeShouldBe = 方法_取已序列化的类(idOfXuLieHuaQi);
                //接口_序列化器 <?> xuliehuaqi = 方法_取序列化器(idOfXuLieHuaQi);

                /*byte[] oldWorkBuffer = Reader.Buffer;
                int inputReaderPosition = (int)Reader.Position;
                int inputReaderLimit = (int)Reader.Length;*/
                UsefulBuffer workBuffer = WorkBuffer;
                int oldStickedDataIntArrayIdMinusOne = SavedStickedDataIntArrayIdMinusOne;
                int oldStickedDataIntArrayId = SavedStickedDataIntArrayId;
                int oldIdSaves = SavedId;

                int blobStartPosition = id == 0 ? 0 : EndPosition[id - 1];
                int blobLength = EndPosition[id] - blobStartPosition;

                SavedId = id;
                SavedStickedDataIntArrayIdMinusOne = id == 0 ? 0 : StickedDataList[id - 1];
                SavedStickedDataIntArrayId = StickedDataList[id];

                WorkBuffer.Buffer = isItemDeflated ? CompressionUtil.INSTANCE.Inflate(RealItemDataBuffer.Skip(blobStartPosition).Take(blobLength).ToArray(), 0, blobLength) : RealItemDataBuffer.Skip(blobStartPosition).Take(blobLength).ToArray();
                //LogUtil.INSTANCE.PrintInfo("源长度",RealItemDataBuffer.Length.ToString(),"ID", id.ToString(), "起始", blobStartPosition.ToString(), "长度", buffer.Length.ToString(), "3-1", SavedStickedDataIntArrayIdMinusOne.ToString(), "3", SavedStickedDataIntArrayId.ToString());

                //if (typeof(T) == typeof(byte[])) item = (T)(object)WorkBuffer.Buffer;
                //else
                //{
                object item = FormatManager.INSTANCE.ParseItem(this, (MassBaseFormatReader)WorkBuffer.Buffer, MyRegableDataAdaptionList[DataAdapterIdArray[id]].DataType);
                //}

                WorkBuffer = workBuffer;
                return item;
            }

            // 构造
            public MassBase(string name)
            {
                Name = name;
                MassManager.INSTANCE.MassList.Add(this);
            }

            // 重置
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
                    WorkBuffer = new UsefulBuffer();

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

            // 加载
            public void Load(FileStream file, string format)
            {
                FullPath = file.Name;

                if (!IsEmpty) Clean();

                ReadyToUse = false;
                try
                {
                    Reader = new MassBaseFormatReader(file);

                    var isCorrectFormat = Reader.CheckHasString(format);
                    if (!isCorrectFormat) return;

                    var num = Reader.ReadInt();
                    var isDeflated = (num & 1) != 0;
                    var deflateLen = num >> 1;
                    var indexDataBlob = isDeflated ? CompressionUtil.INSTANCE.Inflate(Reader.ReadBytes(deflateLen), 0, deflateLen) : Reader.ReadBytes(deflateLen);

                    if (indexDataBlob != null && indexDataBlob.Length != 0)
                    {
                        using var inflatedDataReader = new MassBaseFormatReader(new MemoryStream(indexDataBlob));
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

                        AfterLoadingIndex(inflatedDataReader);
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

            public virtual void AfterLoadingIndex(MassBaseFormatReader inflatedDataReader)
            {
                //LogUtil.INSTANCE.PrintInfo("怎么回事");
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

            // 读子项
            public T Read<T>()
            {
                if (SavedStickedDataIntArrayIdMinusOne == SavedStickedDataIntArrayId) throw new Exception("噬主了");

                SavedStickedDataIntArrayIdMinusOne++;
                int metaOfIdMinusOne = StickedMetaDataList[SavedStickedDataIntArrayIdMinusOne];
                int subitemId = metaOfIdMinusOne >> 2;

                LogUtil.INSTANCE.PrintInfo($"读子项的ID：{subitemId}");
                if ((metaOfIdMinusOne & 3) == 3) return (T)GetItemById(subitemId);
                throw new NotSupportedException("三大欲望");
            }

            // 引用（实际上是暂存）
            public void Reference(object objArr)
            {
                //TODO:实际上是暂存下
                //throw new NotImplementedException();
            }
        }
    }

    public class MassManager
    {
        public static MassManager INSTANCE { get { instance ??= new(); return instance; } }
        private static MassManager? instance;

        public List<MassBase> MassList = new();

        public MassBase? GetMassFileByFileName(string fileName) => MassList.Find((MassBase m) => m.Name.ToLower() == fileName.ToLower());
    }
}