using Me.EarzuChan.Ryo.ConsoleSystem.Commands;
using Me.EarzuChan.Ryo.ConsoleSystem.Utils;
using Me.EarzuChan.Ryo.Utils;
using System;
using System.Reflection;
using static Me.EarzuChan.Ryo.ConsoleSystem.Commands.ICommand;

namespace Me.EarzuChan.Ryo.ConsoleSystem.CommandManagement
{
    [Obsolete("推荐使用ConsoleApplication")]
    public class CommandManager
    {
        public readonly Dictionary<CommandAttribute, Type> commands = new();

        // 开发者模式
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

        // 控制台实现
        public readonly IConsoleImplement ConsoleImplement;

        // 控制台信息
        public readonly ConsoleInfo ConsoleInfo;

        // 全局命令调用帧
        public readonly CommandFrame CommandFrame;

        // 注册命令
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

        public CommandManager(ConsoleInfo? consoleInfo = null, IConsoleImplement? consoleImplement = null)
        {
            ConsoleInfo = consoleInfo ?? new();
            ConsoleImplement = consoleImplement ?? new DefaultConsoleImplement();

            RegCmds();
            CommandFrame = new(this);
        }

        public void ParseCommand(string input) => ParseCommand(ConsoleUtils.ParseCommandLineArguments(input));

        // 解析命令并执行相关操作
        public void ParseCommand(params string[] args)
        {
            CommandFrame.PrintLine();

            if (args == null || args.Length == 0)
            {
                CommandFrame.PrintLine("Command is empty. Enter 'Help' to view supported commands.");
                return;
            }

            var givenCmdName = args[0].ToLower();
            var cmdArgs = args.Skip(1).ToArray();

            // 可选参数的分类

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

                                // TODO:可选参数Builder

                                command.Execute(CommandFrame);
                            }
                            catch (Exception e)
                            {
                                CommandFrame.PrintLine(TextUtils.MakeErrorText("An unhandled exception occurred during command execution.", e, true));
                            }
                            return;
                        }
                    }
                    CommandFrame.PrintLine($"Error: The parameter is incorrect. Usage of {cmd.Key.Name}: {cmd.Key.Information}");
                    return;
                }
            }
            CommandFrame.PrintLine($"Error: '{givenCmdName}' is not recognized as an available command, enter 'Help' for more information.");
        }

        // 控制台线程运行我
        public void InitConsole()
        {
            // Welcome Info
            CommandFrame.PrintLine($"=========================================================\n        {ConsoleInfo.Name} [Version {ConsoleInfo.Version}]\n   Copyright (C) {ConsoleInfo.Copyright}. All rights reserved.\n=========================================================\n\nType 'help' to see the list of available commands.");

            while (true)
            {
                string input = CommandFrame.ReadLine("\n> ", true).Trim();
                if (input.ToLower() == "exit") break;

                ParseCommand(input);
            }

            CommandFrame.PrintLine($"\nExiting {ConsoleInfo.Name}... Have a great day!");

            Thread.Sleep(1500);
        }
    }

    public interface IConsoleImplement
    {
        public void PrintLine(string text);
        public string ReadLine();
        public void Print(string text);
        public ConsoleKey ReadKey();
    }

    public class DefaultConsoleImplement : IConsoleImplement
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

    public class ConsoleInfo
    {
        public string Name { get; set; } = "Ryo Console";
        public string Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown Version";
        public string Copyright { get; set; } = "Earzu Chan";
    }
}