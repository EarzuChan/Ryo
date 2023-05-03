using Me.EarzuChan.Ryo.Utils;
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
}