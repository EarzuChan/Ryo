<<<<<<< HEAD
﻿using Me.EarzuChan.Ryo.Utils;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Me.EarzuChan.Ryo.Commands
{
    public class CommandManager
    {
        /*public static CommandManager INSTANCE { get { instance ??= new(); return instance; } }
        private static CommandManager? instance;*/

        public readonly Dictionary<CommandAttribute, Type> commands = new();
        public bool RunningWithArgs = false;
        public bool IsDev
        {
            get => isDev;
            set
            {
                if (isDev != value)
                {
                    isDev = value;
                    // LogUtil.INSTANCE.PrintInfo($"注册为{value}");
                    RegCmds();
                }
            }
        }
        private bool isDev = false;

        public IConsole ConsoleImplement = new DefaultConsole();

        public readonly ICommand.CommandFrame CommandFrame;

        public void RegCmds()
        {
            commands.Clear();

            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<CommandAttribute>();
                if (attribute != null && typeof(ICommand).IsAssignableFrom(type))
                {
                    if (attribute.IsDev && !isDev) continue;
                    // DEBUG逻辑
                    // UsefulUtils.INSTANCE.PrintInfo("标识符", (attribute == null ? "就是" : "不是"), "类型", (type == null ? "就是" : "不是"));
                    commands.Add(attribute, type);
                }
            }
        }

        public CommandManager()
        {
            RegCmds();
            CommandFrame = new ICommand.CommandFrame(this);
        }

        public CommandManager(IConsole console) : this() => ConsoleImplement = console;

        public void ParseCommandLine(string input)
        {
            // 解析命令行参数
            /*string pattern = @"([^"", true)]\S*|"".+?"")\s*";
            MatchCollection matches = Regex.Matches(input, pattern);
            string[] cmdArgs = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                cmdArgs[i] = matches[i].Value.Trim();
                if (cmdArgs[i].StartsWith("\"") && cmdArgs[i].EndsWith("\""))
                {
                    cmdArgs[i] = cmdArgs[i][1..^1];
                }
            }*/
            ParseCommand(ParseCommandLineArguments(input));
        }

        public string[] ParseCommandLineArguments(string commandLine)
        {
            // LogUtil.INSTANCE.PrintInfo("接受的字符串：" + commandLine);

            List<string> arguments = new List<string>();

            StringBuilder currentArgument = new StringBuilder();
            bool insideQuote = false;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                bool hasBefore = i != 0;

                //LogUtil.INSTANCE.PrintInfo("当前字符：" + c);

                if (c == '"' && (!hasBefore || commandLine[i - 1] != '\\'))
                {
                    insideQuote = !insideQuote;
                }
                else if (c == ' ' && !insideQuote)
                {
                    if (currentArgument.Length > 0)
                    {
                        arguments.Add(currentArgument.ToString());
                        currentArgument.Clear();
                    }
                }
                else
                {
                    currentArgument.Append(c);
                }
            }


            if (currentArgument.Length > 0)
            {
                arguments.Add(currentArgument.ToString());
            }

            return arguments.ToArray();
        }

        public void ParseCommand(params string[] args)
        {
            // 解析命令并执行相关操作
            //Console.WriteLine("解析命令：");
            if (args == null || args.Length == 0)
            {
                ConsoleImplement.PrintLine("Command is empty. Enter 'Help' to view supported commands.");
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
                            try
                            {
                                var command = (ICommand)constructor.Invoke(cmdArgs);
                                // TODO:可选参数
                                command.Execute(CommandFrame);
                            }
                            catch (Exception e)
                            {
                                ConsoleImplement.PrintLine(LogUtils.MakeErrorText("An unhandled exception occurred during command execution.", e, true));
                            }
                            return;
                        }
                    }
                    ConsoleImplement.PrintLine($"Error: The parameter is incorrect. Usage of {cmd.Key.Name}: {cmd.Key.Information}");
                    return;
                }
            }
            ConsoleImplement.PrintLine($"Error: '{givenCmdName}' is not recognized as an available command, enter 'Help' for more information.");
        }
    }

    public interface IConsole
    {
        public void PrintLine(string text);
        public string ReadLine();
        public void Print(string text);
        public ConsoleKey ReadKey();
    }

    // 默认实现，基于 Console 类
    public class DefaultConsole : IConsole
    {
        public void PrintLine(string text) => Console.WriteLine(text);

        public string ReadLine()
        {
            string? str = Console.ReadLine();
            // if (str == null) LogUtils.PrintInfo("读行遇到问题");
            return str ?? "";
        }

        public ConsoleKey ReadKey()
        {
            var c = Console.ReadKey().Key;
            Console.WriteLine();
            return c;
        }

        public void Print(string text) => Console.Write(text);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; set; }
        public string Information { get; set; }
        public bool IsDev { get; set; }

        public CommandAttribute(string name, string info = "No description available.")
        {
            Name = name;
            Information = info;
        }
        public CommandAttribute(string name, string info, bool isDev)
        {
            Name = name;
            Information = info;
            IsDev = isDev;
        }
    }

    public interface ICommand
    {
        void Execute(CommandFrame commandFrame);

        public class CommandFrame
        {
            public readonly CommandManager Manager;

            public CommandFrame(CommandManager commandManager) => Manager = commandManager;

            public void PrintLine(string text = "") => Manager.ConsoleImplement.PrintLine(text);

            public string ReadLine(string title, bool hideColon = false)
            {
                Manager.ConsoleImplement.Print($"{title}{(hideColon ? "" : ": ")}");
                return Manager.ConsoleImplement.ReadLine();
            }

            public void Print(string text) => Manager.ConsoleImplement.Print(text);

            public bool ReadYesOrNo(string title, ConsoleKey yesKey = ConsoleKey.Y, ConsoleKey noKey = ConsoleKey.N)
            {
                string yesKeyName = SolveKeyName(yesKey.ToString());
                string noKeyName = SolveKeyName(noKey.ToString());
                ConsoleKey yesKeySecond = SolveSecondKey(yesKey);
                ConsoleKey noKeySecond = SolveSecondKey(noKey);

                while (true)
                {
                    Manager.ConsoleImplement.Print($"{title} [{yesKeyName}/{noKeyName}]: ");
                    var inputKey = Manager.ConsoleImplement.ReadKey();
                    if (inputKey == yesKey || inputKey == yesKeySecond) return true;
                    else if (inputKey == noKey || inputKey == noKeySecond) return false;
                    Manager.ConsoleImplement.PrintLine($"Invalid inputKey. Please press '{yesKeyName}' or '{noKeyName}'.");
                }
            }

            private static string SolveKeyName(string oriKeyName) => oriKeyName.StartsWith('D') && oriKeyName.Length == 2 ? oriKeyName[1..] : oriKeyName == "DownArrow" ? "↓" : oriKeyName == "UpArrow" ? "↑" : oriKeyName;

            private static ConsoleKey SolveSecondKey(ConsoleKey oriKey) => oriKey == ConsoleKey.D1 ? ConsoleKey.NumPad1 : oriKey == ConsoleKey.D0 ? ConsoleKey.NumPad0 : oriKey;
        }
    }
=======
﻿using Me.EarzuChan.Ryo.Utils;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Me.EarzuChan.Ryo.Commands
{
    public class CommandManager
    {
        public static CommandManager INSTANCE { get { instance ??= new(); return instance; } }
        private static CommandManager? instance;

        public readonly Dictionary<CommandAttribute, Type> commands = new();
        public bool RunningWithArgs = false;
        public bool IsDev
        {
            get => isDev;
            set
            {
                if (isDev != value)
                {
                    isDev = value;
                    // LogUtil.INSTANCE.PrintInfo($"注册为{value}");
                    RegCmds();
                }
            }
        }
        private bool isDev = false;

        public void RegCmds()
        {
            commands.Clear();

            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<CommandAttribute>();
                if (attribute != null && typeof(ICommand).IsAssignableFrom(type))
                {
                    if (attribute.IsDev && !isDev) continue;
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
            /*string pattern = @"([^""]\S*|"".+?"")\s*";
            MatchCollection matches = Regex.Matches(input, pattern);
            string[] cmdArgs = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                cmdArgs[i] = matches[i].Value.Trim();
                if (cmdArgs[i].StartsWith("\"") && cmdArgs[i].EndsWith("\""))
                {
                    cmdArgs[i] = cmdArgs[i][1..^1];
                }
            }*/
            ParseCommand(ParseCommandLineArguments(input));
        }

        public string[] ParseCommandLineArguments(string commandLine)
        {
            // LogUtil.INSTANCE.PrintInfo("接受的字符串：" + commandLine);

            List<string> arguments = new List<string>();

            StringBuilder currentArgument = new StringBuilder();
            bool insideQuote = false;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                bool hasBefore = i != 0;

                //LogUtil.INSTANCE.PrintInfo("当前字符：" + c);

                if (c == '"' && (!hasBefore || commandLine[i - 1] != '\\'))
                {
                    insideQuote = !insideQuote;
                }
                else if (c == ' ' && !insideQuote)
                {
                    if (currentArgument.Length > 0)
                    {
                        arguments.Add(currentArgument.ToString());
                        currentArgument.Clear();
                    }
                }
                else
                {
                    currentArgument.Append(c);
                }
            }


            if (currentArgument.Length > 0)
            {
                arguments.Add(currentArgument.ToString());
            }

            return arguments.ToArray();
        }

        public void ParseCommand(params string[] args)
        {
            // 解析命令并执行相关操作
            //Console.WriteLine("解析命令：");
            if (args == null || args.Length == 0)
            {
                LogUtils.INSTANCE.PrintInfo("命令为空，输入Help查看支持的命令。");
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
                            try
                            {
                                var command = (ICommand)constructor.Invoke(cmdArgs);
                                command.Execute();
                            }
                            catch (Exception e)
                            {
                                LogUtils.INSTANCE.PrintError("命令执行出错", e);
                            }
                            return;
                        }
                    }
                    LogUtils.INSTANCE.PrintInfo($"The parameter is incorrect. Usage of {cmd.Key.Name}: {cmd.Key.Help}");
                    return;
                }
            }
            LogUtils.INSTANCE.PrintInfo($"'{givenCmdName}' is not recognized as an available command, enter 'Help' for more information.");
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
>>>>>>> 62013cbf2e8e2181ffdf41d357ae518f5ad00c74
}