using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Org.CNWeak.RyoManipulator;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Org.CNWeak.RyoManipulator
{
    class ProgramMainBlob
    {
        public static CommandManager CommandManager = new();
        public static MassManager MassManager = new();
        public static UsefulUtils UsefulUtils = new();

        static void Main(string[] args)
        {
            static void logListener(string info)
            {
                Console.WriteLine(info);
            }

            CommandManager.RegDefaultCmds();

#if DEBUG
            CommandManager.RegDevCmds();
#endif

            CommandManager.SetLogListener(logListener);
            CommandManager.SetMassManager(MassManager);
            UsefulUtils.SetLogListener(logListener);

            if (args.Length > 0)
            {
                CommandManager.RunningWithArgs = true;
                CommandManager.ParseCommand(args);
                return;
            }

            Console.WriteLine($"Ryo Manipulator [Version {Assembly.GetExecutingAssembly().GetName().Version}]\nCopyright (C) CNWeak Organization. All rights reserved.");
            while (true)
            {
                Console.Write("\nW:\\Ryo\\User>");
                string input = Console.ReadLine().Trim();
                if (input.ToLower() == "exit") break;

                CommandManager.ParseCommandLine(input);
            }
        }
    }

    class CommandManager
    {
        private List<Command> commands = new List<Command>();
        private Action<string> LogListener;
        private MassManager MassManager;
        private UsefulUtils UsefulUtils;
        public bool RunningWithArgs = false;

        public CommandManager()
        {
            LogListener = (str) => { Trace.WriteLine(str); };

            commands.Add(new Command("Help", "获得软件命令帮助，无需参数"));
        }

        public void RegDefaultCmds()
        {
            commands.Add(new Command("Load", "从文件路径加载档案（*.fs），参数为路径"));
            commands.Add(new Command("Info", "查看档案的索引信息，参数为档案名称"));
            commands.Add(new Command("Dump", "转储数据到相同目录的Dump文件中，参数为档案名称"));
        }

        public void RegDevCmds()
        {
            commands.Add(new Command("Pack", "打包经过修改的档案回原文件，参数为档案名称"));
        }

        public void Load(string path)
        {
            try
            {
                var stream = new FileStream(path, FileMode.Open);
                if (stream.Length == 0) throw new Exception("文件长度为零");

                var fileName = Path.GetFileNameWithoutExtension(stream.Name);
                //LogListener("正在加载：" + fileName);

                var mass = new MassFile(fileName);

                mass.Load(stream, "FileSystem");

                if (mass.ReadyToUse)
                {
                    Info(fileName);
                    LogListener($"\n档案已载入，名称：{fileName.ToUpper()}，稍后可凭借该名称Dump文件、查看索引信息、进行增删改查等操作。");
                }
                else
                {
                    LogListener("加载失败，文件无效，请检查您指定的文件是否存在、正确、未损坏。");
                    MassManager.MassList.Remove(mass);
                }
            }
            catch (Exception e)
            {
                PrintError("加载文件失败", e);
            }
        }

        private void PrintError(string v, Exception e)
        {
            UsefulUtils.PrintError(v, e);
        }

        public void Dump(string str)
        {
            var mass = MassManager.MassList.Find((MassFile m) => m.Name.ToLower() == str.ToLower());
            if (mass != null && mass.ReadyToUse)
            {
                LogListener($"找到文件：{str.ToUpper()}");
                LogListener(mass.DumpBuffer());
            }
            else LogListener($"找不到文件：{str.ToUpper()}，请查看拼写是否正确，文件是否已正确加载？");
        }

        public void Info(string str)
        {
            var mass = MassManager.MassList.Find((MassFile m) => m.Name.ToLower() == str.ToLower());
            if (mass != null && mass.ReadyToUse)
            {
                LogListener(str.ToUpper() + "的索引信息：\n");
                LogListener($"整数数组数据数：{mass.ObjCount}");
                LogListener($"G3L1数：{mass.G3L1}");
                LogListener($"\n类注册数：{mass.MyRegList.Count}");
                foreach (var item in mass.MyRegList) LogListener($"-- No.{item.Id} 类名：{item.ClazzName} 序列化器：{item.序列化器Name}");
                LogListener($"\n成员数：{mass.MyIdStrMap.Count}");
                foreach (var item in mass.MyIdStrMap) LogListener($"-- Id.{item.Value} Name：{item.Key}");
            }
        }

        public void Pack()
        {
            LogListener("Pack data into a compressed format.");
        }

        public void Help()
        {
            LogListener($"如今支持{commands.Count}个命令，使用方法如下：");

            int i = 1;
            foreach (var cmd in commands)
            {
                LogListener($"No.{i++} {cmd.Name} 用法：{cmd.Help}");
            }

            if (!RunningWithArgs) LogListener("\n如需退出，键入Exit。");
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

        public void ParseCommand(string[] args)
        {
            // 解析命令并执行相关操作
            //Console.WriteLine("解析命令：");
            if (args == null || args.Length == 0)
            {
                LogListener("命令为空，输入Help查看支持的命令。");
                return;
            }

            var givenCmdName = args[0].ToLower();
            var cmdArgs = args.Skip(1).ToArray();
            foreach (var cmd in commands)
            {
                if (givenCmdName == cmd.Name.ToLower())
                {
                    if (cmdArgs.Length == cmd.Method.GetParameters().Length)
                    {
                        cmd.Method.Invoke(this, cmdArgs);
                        //return;
                    }
                    else LogListener($"您的参数有误，{cmd.Name}的用法：{cmd.Help}");
                    return;
                }
            }
            LogListener($"'{givenCmdName}' is not recognized as an available command, enter 'Help' for more information.");
        }

        public void SetLogListener(Action<string> logListener)
        {
            LogListener = logListener;
        }

        public void SetMassManager(MassManager massManager)
        {
            MassManager = massManager;
        }
    }

    class Command
    {
        public string Name { get; set; }
        public MethodInfo Method { get; set; }
        public string Help { get; set; }

        public Command(string name, string help)
        {
            Name = name;
            Method = GetMethod(name);
            Help = help;
        }

        private static MethodInfo GetMethod(string methodName) => typeof(CommandManager).GetMethod(methodName);
    }

    class MassManager
    {
        public List<MassFile> MassList = new();
        public static MassManager Instance;
        public MassManager() => Instance = this;
    }
}

class RegItem
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

class MassFile
{
    public string Name = "";
    public string FullPath = "";

    public InputStreamReader Reader = new(Stream.Null);
    public OutputStreamWriter Writer = new();

    public int ObjCount = 0;
    public int CurrentObjCount = 0;
    public List<int> 序列化了的类IdGroup = new();
    public List<int> 索引对Group = new();
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

    public MassFile(string name)
    {
        Name = name;
        MassManager.Instance.MassList.Add(this);
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
            序列化了的类IdGroup = new();
            索引对Group = new();
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
            UsefulUtils.Instance.PrintError("加载Mass失败", e);
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
            var indexDataBlob = isDeflated ? UsefulUtils.Instance.Inflate(Reader.ReadBytes(deflateLen), 0, deflateLen) : Reader.ReadBytes(deflateLen);

            if (indexDataBlob != null && indexDataBlob.Length != 0)
            {
                using var inflatedDataReader = new InputStreamReader(new MemoryStream(indexDataBlob));
                ObjCount = inflatedDataReader.ReadInt();
                CurrentObjCount = ObjCount;

                for (var i = 0; i < ObjCount; i++)
                    序列化了的类IdGroup.Add(inflatedDataReader.ReadInt());

                for (var i = 0; i < ObjCount; i++)
                    索引对Group.Add(inflatedDataReader.ReadInt());

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
            UsefulUtils.Instance.PrintError("加载Mass失败", e);
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
            using (FileStream fs = new FileStream(FullPath + ".dump", FileMode.Create, FileAccess.Write))
            {
                fs.Write(Buffer, 0, Buffer.Length);
            }
            info += "成功，路径：" + FullPath + ".dump";
        }
        catch (Exception ex)
        {
            UsefulUtils.Instance.PrintError("保存时错误", ex);
            info += "保存失败（悲";
        }
        return info;
    }
}

class InputStreamReader : IDisposable
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

class OutputStreamWriter { }

class UsefulUtils
{
    public static UsefulUtils Instance;
    public Action<string> LogListener = (str) => { Trace.WriteLine(str); };
    public void SetLogListener(Action<string> logger) => LogListener = logger;
    public void PrintError(String info, Exception e)
    {
        LogListener(info + " " + e.Message);
        LogListener("堆栈：" + e.StackTrace);
    }

    public UsefulUtils() => Instance = this;

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
            return null;
        }
    }
}