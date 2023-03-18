using ICSharpCode.SharpZipLib.Zip.Compression;
using Org.CNWeak.Ryo.Commands;
using Org.CNWeak.Ryo.IO;
using Org.CNWeak.Ryo.Mass;
using Org.CNWeak.Ryo.Utils;
using System.Diagnostics;
using System.IO;
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
            UsefulUtils.INSTANCE.SetLogger(str => Console.WriteLine(str));

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
                    UsefulUtils.INSTANCE.PrintInfo("命令为空，输入Help查看支持的命令。");
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
                        UsefulUtils.INSTANCE.PrintInfo($"The parameter is incorrect. Usage of {cmd.Key.Name}: {cmd.Key.Help}");
                        return;
                    }
                }
                UsefulUtils.INSTANCE.PrintInfo($"'{givenCmdName}' is not recognized as an available command, enter 'Help' for more information.");
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
            public string path;
            public LoadCommand(string path)
            {
                this.path = path;
            }

            public void Execute()
            {
                try
                {
                    var stream = new FileStream(this.path, FileMode.Open);
                    if (stream.Length == 0) throw new Exception("文件长度为零");

                    var fileName = Path.GetFileNameWithoutExtension(stream.Name);
                    //LogListener("正在加载：" + fileName);

                    var mass = new MassFile(fileName);

                    mass.Load(stream, "FileSystem");

                    if (mass.ReadyToUse)
                    {
                        CommandManager.INSTANCE.ParseCommand("Info", fileName);
                        UsefulUtils.INSTANCE.PrintInfo($"\n档案已载入，名称：{fileName.ToUpper()}，稍后可凭借该名称Dump文件、查看索引信息、进行增删改查等操作。");
                    }
                    else
                    {
                        UsefulUtils.INSTANCE.PrintInfo("加载失败，文件无效，请检查您指定的文件是否存在、正确、未损坏。");
                        CommandManager.INSTANCE.ParseCommand("Unload", fileName);
                    }
                }
                catch (Exception e)
                {
                    UsefulUtils.INSTANCE.PrintError("加载文件失败", e);
                }
            }
        }

        [Command("Info", "Show the info of a file")]
        public class InfoCommand : ICommand
        {
            public string str;
            public InfoCommand(string str)
            {
                this.str = str;
            }
            public void Execute()
            {
                var mass = MassManager.INSTANCE.MassList.Find((MassFile m) => m.Name.ToLower() == str.ToLower());
                if (mass != null && mass.ReadyToUse)
                {
                    UsefulUtils.INSTANCE.PrintInfo(str.ToUpper() + "的索引信息：\n");
                    UsefulUtils.INSTANCE.PrintInfo($"整数数组数据数：{mass.ObjCount}，以下为已序列化类ID、压缩情况、索引、G3：");
                    for (var i = 0; i < mass.ObjCount; i++) UsefulUtils.INSTANCE.PrintInfo($"-- No.{i + 1} 已序列化类ID：{mass.已序列化类IDs[i]} 已压缩：{(mass.已压缩List[i] ? "是" : "否")} 末尾索引：{mass.EndPosition[i]} G3：{mass.IntGroup3[i]}");
                    UsefulUtils.INSTANCE.PrintInfo($"G3L1数：{mass.G3L1}，以下为G4");
                    int currentIndex = 0;
                    while (currentIndex < mass.G3L1)
                    {
                        List<string> str = new();
                        for (int i = 0; i < 4 && currentIndex < mass.G3L1; i++)
                        {
                            str.Add("No." + (currentIndex + 1) + "：" + mass.IntGroup4[currentIndex]);
                            currentIndex++;
                        }
                        UsefulUtils.INSTANCE.PrintInfo(str.ToArray());
                    }
                    UsefulUtils.INSTANCE.PrintInfo($"\n类注册数：{mass.MyRegList.Count}");
                    foreach (var item in mass.MyRegList) UsefulUtils.INSTANCE.PrintInfo($"-- No.{item.Id} 类名：{item.ClazzName} 序列化器：{item.序列化器Name}");
                    UsefulUtils.INSTANCE.PrintInfo($"\n成员数：{mass.MyIdStrMap.Count}");
                    var j = 1;
                    foreach (var item in mass.MyIdStrMap)
                    {
                        UsefulUtils.INSTANCE.PrintInfo($"-- Id.{item.Value} Name：{item.Key} 对应No.{j}");
                        j++;
                    }

                }
            }
        }

        [Command("Unload", "Unload a file by its name")]
        public class UnloadCommand : ICommand
        {
            public string fileName;
            public UnloadCommand(string fileName)
            {
                this.fileName = fileName;
            }
            public void Execute()
            {
                MassManager.INSTANCE.MassList.Remove(MassManager.INSTANCE.MassList.Find(a => a.Name == fileName)!);
            }
        }

        [Command("Dump", "Dump the objects blob of a file")]
        public class DumpCommand : ICommand
        {
            public string str;
            public DumpCommand(string str)
            {
                this.str = str;
            }
            public void Execute()
            {
                var mass = MassManager.INSTANCE.MassList.Find((MassFile m) => m.Name.ToLower() == str.ToLower());
                if (mass != null && mass.ReadyToUse)
                {
                    UsefulUtils.INSTANCE.PrintInfo($"找到文件：{str.ToUpper()}");
                    UsefulUtils.INSTANCE.PrintInfo(mass.DumpBuffer());
                }
                else UsefulUtils.INSTANCE.PrintInfo($"找不到文件：{str.ToUpper()}，请查看拼写是否正确，文件是否已正确加载？");
            }
        }

        [Command("Help", "Get the help infomations of this application")]
        public class HelpCommand : ICommand
        {
            public void Execute()
            {
                UsefulUtils.INSTANCE.PrintInfo($"如今支持{CommandManager.INSTANCE.commands.Count}个命令，使用方法如下：");

                int i = 1;
                foreach (var cmd in CommandManager.INSTANCE.commands)
                {
                    UsefulUtils.INSTANCE.PrintInfo($"No.{i++} {cmd.Key.Name} 用法：{cmd.Key.Help}");
                }

                if (!CommandManager.INSTANCE.RunningWithArgs) UsefulUtils.INSTANCE.PrintInfo("\n如需退出，键入Exit。");
            }
        }
    }

    namespace Utils
    {
        public class UsefulUtils
        {
            public static UsefulUtils INSTANCE { get { instance ??= new(); return instance; } }
            private static UsefulUtils? instance;
            private Action<string> Logger = (str) => { Trace.WriteLine(str); };
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
                    PrintError("解压出错", e);
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

            public InputStreamReader(Stream inputStream)
            {
                this.Reader = new BinaryReader(inputStream);
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
            public List<int> 已序列化类IDs = new();
            public List<int> EndPosition = new();
            public List<bool> 已压缩List = new();
            public List<int> IntGroup3 = new();
            public List<int> IntGroup4 = new();
            public int G3L1 = 0;
            //public int RegCount = 0;
            public int KVCount = 0;
            public byte[] Buffer = Array.Empty<byte>();
            public Dictionary<string, int> MyIdStrMap = new();
            public List<RegItem> MyRegList = new();

            public bool ReadyToUse = true;
            public bool IsEmpty = true;

            public class RegItem
            {
                public string ClazzName { get; set; }
                public string 序列化器Name { get; set; }
                public int Id { get; set; }

                public RegItem(int id, string str1, string str2)
                {
                    Id = id;
                    ClazzName = str1;
                    序列化器Name = str2;
                }
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
                    已序列化类IDs = new();
                    EndPosition = new();
                    IntGroup3 = new();
                    IntGroup4 = new();
                    G3L1 = 0;
                    //RegCount = 0;
                    KVCount = 0;
                    Buffer = Array.Empty<byte>();

                    IsEmpty = true;
                    ReadyToUse = true;
                }
                catch (Exception e)
                {
                    UsefulUtils.INSTANCE.PrintError("加载Mass失败", e);
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
                    var deflateLen = num >> 1;//Reader.ReadInt() >> 1;
                    var indexDataBlob = isDeflated ? UsefulUtils.INSTANCE.Inflate(Reader.ReadBytes(deflateLen), 0, deflateLen) : Reader.ReadBytes(deflateLen);

                    if (indexDataBlob != null && indexDataBlob.Length != 0)
                    {
                        using var inflatedDataReader = new InputStreamReader(new MemoryStream(indexDataBlob));
                        ObjCount = inflatedDataReader.ReadInt();
                        CurrentObjCount = ObjCount;

                        for (var i = 0; i < ObjCount; i++)
                        {
                            int j = inflatedDataReader.ReadInt();
                            已序列化类IDs.Add(j >> 1);
                            已压缩List.Add((j & 1) != 0);
                        }

                        for (var i = 0; i < ObjCount; i++)
                            EndPosition.Add(inflatedDataReader.ReadInt());

                        for (var i = 0; i < ObjCount; i++)
                            IntGroup3.Add(inflatedDataReader.ReadInt());

                        G3L1 = IntGroup3.Last();

                        for (var i = 0; i < G3L1; i++)
                            IntGroup4.Add(inflatedDataReader.ReadInt());

                        var regCount = inflatedDataReader.ReadInt();
                        for (var i = 0; i < regCount; i++)
                        {
                            var id = inflatedDataReader.ReadInt();
                            var str1 = inflatedDataReader.ReadString();
                            var str2 = inflatedDataReader.ReadString();
                            MyRegList.Add(new RegItem(id, str1, str2));
                        }

                        var idStrMapCount = inflatedDataReader.ReadInt();
                        for (var i = 0; i < idStrMapCount; i++)
                        {
                            var str = inflatedDataReader.ReadString();
                            var id = inflatedDataReader.ReadInt();
                            MyIdStrMap.Add(str, id);
                        }
                    }
                    Buffer = Reader.ReadAllBytes();

                    ReadyToUse = true;
                    IsEmpty = false;
                }
                catch (Exception e)
                {
                    UsefulUtils.INSTANCE.PrintError("加载Mass失败", e);
                }

                file.Dispose();
            }

            public string DumpBuffer()
            {
                if (!ReadyToUse || IsEmpty) return "未准备好或者内容为空";
                if (String.IsNullOrWhiteSpace(FullPath)) return "保存路径为空";
                var info = "保存";
                try
                {
                    using (FileStream fs = new (FullPath + ".dump", FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(Buffer, 0, Buffer.Length);
                    }
                    info += "成功，路径：" + FullPath + ".dump";
                }
                catch (Exception ex)
                {
                    UsefulUtils.INSTANCE.PrintError("保存时错误", ex);
                    info += "保存失败（悲";
                }
                return info;
            }
        }

        public class MassManager
        {
            public static MassManager INSTANCE { get { instance ??= new(); return instance; } }
            private static MassManager? instance;

            public List<MassFile> MassList = new();
        }
    }
}