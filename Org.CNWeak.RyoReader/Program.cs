using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Org.CNWeak.RyoReader
{
    class Program
    {
        //MassManager MassManagerInstance = new MassManager();

        /*static byte[] DecompressBuffer(byte[] source)
        {
            using (MemoryStream sourceStream = new MemoryStream(source))
            using (DeflateStream decompressionStream = new DeflateStream(sourceStream, CompressionMode.Decompress))
            using (MemoryStream destinationStream = new MemoryStream())
            {
                decompressionStream.CopyTo(destinationStream);
                return destinationStream.ToArray();
            }
        }*/
        /*Console.WriteLine("Use \"load <filename>\" to load a fs file.");
            string command = Console.ReadLine();
            string[] parts = command.Split(' ');
            if (parts.Length != 3 || parts[0] != "load")
            {
                Console.WriteLine("Invalid command");
                return;
            }
            string filename = string.Join(' ', parts.Skip(1)).Trim();
            if (filename.StartsWith('"') && filename.EndsWith('"')) filename = filename.Substring(1, filename.Length - 2);
            try
            {
                using (FileStream inputStream = new FileStream(filename, FileMode.Open))
                {
                    InputStreamReader inputStreamReader = new InputStreamReader(inputStream);
                    bool hasFormatString = inputStreamReader.CheckHasString("FileSystem");
                    int secondInt = inputStreamReader.ReadInt() >> 1;
                    Console.WriteLine("Is Valid: " + hasFormatString);
                    Console.WriteLine("压缩大小: " + secondInt);

                    try
                    {
                        byte[] 解压缩了的 = DecompressBuffer(inputStreamReader.ReadBytes(secondInt));

                        Console.WriteLine("解压缩大小: " + 解压缩了的.Length);
                    }catch(Exception ex)
                    {
                        Console.WriteLine("解压缩失败: "+ex.Message);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File not found: " + filename);
            }
            catch (IOException)
            {
                Console.WriteLine("Error reading file: " + filename);
            }*/


        static void Main(string[] args)
        {
            Action<string> logListener = (info) =>
            {
                Console.WriteLine(info);
            };
            CommandManager manager = new();
            manager.RegDefaultCmds();

#if DEBUG
            manager.RegDevCmds();
#endif

            manager.setLogListener(logListener);
            StaticUtils.LogListener = logListener;

            if (args.Length > 0)
            {
                manager.ParseCommand(args);
                return;
            }

            Console.WriteLine("欢迎使用Ryo Reader：");
            while (true)
            {
                Console.Write("\n> ");
                string input = Console.ReadLine().Trim();
                if (input.ToLower() == "exit") break;

                manager.ParseCommandLine(input);
            }
        }

        class CommandManager
        {
            private List<Command> commands = new List<Command>();
            private Action<string> LogListener;

            public CommandManager()
            {
                LogListener = (str) => { Trace.WriteLine(str); };

                commands.Add(new Command("Help", "获得软件命令帮助。"));
            }

            public void RegDefaultCmds()
            {
                commands.Add(new Command("Load", "Load data from a file."));
            }

            public void RegDevCmds()
            {
                commands.Add(new Command("Dump", "Dump data to a file."));
                commands.Add(new Command("Pack", "Pack data into a compressed format."));
            }

            public void Load(string path)
            {
                try
                {
                    var stream = new FileStream(path, FileMode.Open);
                    if (stream.Length == 0) throw new Exception("文件长度为零");

                    var fileName = Path.GetFileNameWithoutExtension(stream.Name);
                    LogListener("正在加载：" + fileName);
                    new MassFile(fileName).Load(stream, "FileSystem");
                }
                catch (Exception e)
                {
                    PrintError("加载文件失败", e);
                }
            }

            private void PrintError(string v, Exception e)
            {
                StaticUtils.PrintError(LogListener, v, e);
            }

            public void Dump()
            {
                LogListener("Dump data to a file.");
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
                    LogListener("可能命令为空，输入Help查看支持的命令。");
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
                            return;
                        }
                        else LogListener($"您的参数有误，{cmd.Name}的用法：{cmd.Help}");
                    }
                }
                LogListener("找不到命令，输入Help查看支持的命令。");
            }

            public void setLogListener(Action<string> logListener)
            {
                LogListener = logListener;
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
    }

    static class MassManager
    {
        public static List<MassFile> MassList = new();
    }

    class MassFile
    {
        public string Name = "";
        public InputStreamReader Reader = new(Stream.Null);
        public OutputStreamWriter Writer = new();

        public int ObjCount = 0;
        public int CurrentObjCount = 0;
        public List<int> 序列化了的类IdGroup = new();
        public List<int> 索引对Group = new();
        public List<int> IntGroup3 = new();
        public List<int> IntGroup4 = new();
        public int G3L1 = 0;
        public int RegCount = 0;
        public int KVCount = 0;
        public byte[] Buffer = Array.Empty<byte>();
        public Dictionary<string, int> MyIdStrMap = new();

        public bool ReadyToUse = false;
        public bool IsEmpty = true;

        public MassFile(string name)
        {
            Name = name;
        }

        public void Clean()
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
            RegCount = 0;
            KVCount = 0;
            Buffer = Array.Empty<byte>();

            IsEmpty = true;
            ReadyToUse = true;
        }

        public void Load(FileStream file, string format)
        {
            if (!IsEmpty) Clean();
            try
            {
                Reader = new InputStreamReader(file);

                var isCorrectFormat = Reader.CheckHasString(format);
                if (!isCorrectFormat) return;

                var inflateLen = Reader.ReadInt() >> 1;
                var inflated = StaticUtils.Inflate(Reader.ReadBytes(inflateLen), 0, inflateLen);

                if (inflated != null && inflated.Length != 0)
                {
                    // WRITE TO FILE
                    /*var indexFile = new FileInfo("$newPath.index");
                    if (!indexFile.Exists) indexFile.Create();
                    File.WriteAllBytes(indexFile.FullName, inflated);*/

                    using var inflatedDataReader = new InputStreamReader(new MemoryStream(inflated));
                    ObjCount = inflatedDataReader.ReadInt();
                    CurrentObjCount = inflatedDataReader.ReadInt();

                    for (var i = 0; i < ObjCount * 3; i++)
                        序列化了的类IdGroup.Add(inflatedDataReader.ReadInt());

                    for (var i = 0; i < ObjCount * 3; i++)
                        索引对Group.Add(inflatedDataReader.ReadInt());

                    for (var i = 0; i < ObjCount * 3; i++)
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
                        // REG
                    }

                    var idStrMapCount = inflatedDataReader.ReadInt();
                    for (var i = 0; i < idStrMapCount; i++)
                    {
                        var str = inflatedDataReader.ReadString();
                        var id = inflatedDataReader.ReadInt();
                        MyIdStrMap.Add(str, id);
                    }
                }
                /*else
                {
                    //NO INDEX INFO!
                }*/
            }
            catch (Exception e)
            {
                StaticUtils.PrintError(null, "加载Mass失败", e);
            }
        }
    }

    class InputStreamReader : IDisposable
    {
        private BinaryReader reader;

        public InputStreamReader(Stream inputStream)
        {
            this.reader = new BinaryReader(inputStream);
        }

        public byte[] ReadBytes(int length)
        {
            return reader.ReadBytes(length);
        }

        public int ReadInt()
        {
            int result = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
            return result;
        }

        public string ReadBytesToHexString(int length)
        {
            byte[] bytes = reader.ReadBytes(length);

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
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        public bool CheckHasString(string str)
        {
            return ReadString(Encoding.UTF8.GetBytes(str).Length) == str;
        }

        public void Dispose()
        {
            reader.Dispose();
        }
    }

    class OutputStreamWriter { }

    static class StaticUtils
    {
        public static Action<string> LogListener = (str) => { Trace.WriteLine(str); };
        public static void PrintError(Action<string> logger, String info, Exception e)
        {
            if (logger != null)
            {
                logger(info + " " + e.Message);
                logger("堆栈：" + e.StackTrace);
                return;
            }
            LogListener(info + " " + e.Message);
            LogListener("堆栈：" + e.StackTrace);
        }

        public static byte[] Inflate(byte[] input, int offset, int length)
        {
            try
            {
                using (var inputStream = new MemoryStream(input, offset, length))
                using (var outputStream = new MemoryStream())
                using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(outputStream);
                    return outputStream.ToArray();
                }
            }
            catch (Exception e)
            {
                PrintError(null,"解压出错",e);
                return null;
            }
        }
    }
}