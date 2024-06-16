using Me.EarzuChan.Ryo.ConsoleSystem.Utils;
using Me.EarzuChan.Ryo.Exceptions;
using Me.EarzuChan.Ryo.Utils;
using System.Collections;
using System.Reflection;
using System.Text;
using Me.EarzuChan.Ryo.ConsoleSystem.Backend;
using static Me.EarzuChan.Ryo.ConsoleSystem.Commands.ICommand;
using Me.EarzuChan.Ryo.ConsoleSystem.Commands;

namespace Me.EarzuChan.Ryo.ConsoleSystem
{
    public class ConsoleApplication
    {
        // Runner
        // Cmds
        // 依赖注入
        internal readonly ConsoleApplicationProfile Profile;
        internal readonly IConsoleApplicationConsoleBackend ConsoleBackend;
        internal readonly ArrayList Dependencies;
        internal readonly Dictionary<CommandAttribute, Type> Commands;
        private readonly ConsoleApplicationContext Context;

        public void Run()
        {
            // Welcome Info
            Context.PrintLine($"=========================================================\n        {Profile.Name} [Version {Profile.Version}]\n   Copyright (C) {Profile.Description}. All rights reserved.\n=========================================================\n\nType 'help' to see the list of available commands.");

            while (true)
            {
                string input = Context.ReadLine("\n> ", true).Trim();
                if (input.ToLower() == "exit") break;

                ParseCommand(input);
            }

            Context.PrintLine($"\nExiting {Profile.Name}... Have a great day!");

            Thread.Sleep(1500);
        }

        public async Task RunAsync() => await Task.Run(Run);

        public void ParseCommand(string input) => ParseCommand(ConsoleUtils.ParseCommandLineArguments(input));

        // 解析命令并执行相关操作
        public void ParseCommand(params string[] args)
        {
            Context.PrintLine();

            if (args == null || args.Length == 0)
            {
                Context.PrintLine("Command is empty. Enter 'Help' to view supported commands.");
                return;
            }

            var givenCmdName = args[0].ToLower();
            var cmdArgs = args.Skip(1).ToArray();

            // 可选参数的分类

            foreach (var cmd in Commands)
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

                                command.Execute(Context);
                            }
                            catch (Exception e)
                            {
                                Context.PrintLine(TextUtils.MakeErrorMsgText("An unhandled exception occurred during command execution.", e, true));
                            }
                            return;
                        }
                    }
                    Context.PrintLine($"Error: The parameter is incorrect. Usage of {cmd.Key.Name}: {cmd.Key.Description}");
                    return;
                }
            }
            Context.PrintLine($"Error: '{givenCmdName}' is not recognized as an available command, enter 'Help' for more information.");
        }

        internal ConsoleApplication(ConsoleApplicationProfile profile, IConsoleApplicationConsoleBackend consoleBackend, ArrayList dependencies, Dictionary<CommandAttribute, Type> commands)
        {
            Profile = profile;
            ConsoleBackend = consoleBackend;
            Dependencies = dependencies;
            Commands = commands;
            Context = new(this);
        }

        public static ConsoleApplicationBuilder CreateBuilder(ConsoleApplicationProfile profile) => new(profile);

        public static ConsoleApplicationBuilder CreateBuilder() => CreateBuilder(new());
    }

    public class ConsoleApplicationBuilder
    {
        private readonly ConsoleApplicationProfile Profile;
        private readonly ArrayList Dependencies = new();
        private readonly Dictionary<CommandAttribute, Type> Commands = new();
        private IConsoleApplicationConsoleBackend? ConsoleBackend;
        private bool IsBuilt = false;

        internal ConsoleApplicationBuilder(ConsoleApplicationProfile profile)
        {
            Profile = profile;
        }

        public ConsoleApplication Build()
        {
            if (IsBuilt) throw new InvalidOperationException("Builder instance has already built a product");

            if (ConsoleBackend == null) throw new RyoException("未配置控制台后端");

            if (Profile.CommandRegistrationStrategy == CommandRegistrationStrategy.ScanAndRegisterAutomatically) ScanCmds();

            RegisterCmd(new("Help", "Get the help infomations of this application"), typeof(HelpCommand));

            ConsoleApplication application = new(Profile, ConsoleBackend, Dependencies, Commands);

            IsBuilt = true;

            return application;
        }

        private void ScanCmds()
        {
            // Commands.Clear();

            var types = TypeUtils.GetAppAllTypes();
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<CommandAttribute>();
                if (attribute != null && typeof(ICommand).IsAssignableFrom(type))
                {
                    if (!attribute.Scannable || (attribute.IsDev && !Profile.IsDev)) continue;

                    RegisterCmd(attribute, type);
                }
            }
        }

        public ConsoleApplicationBuilder RegisterCmd(CommandAttribute commandAttribute, Type command)
        {
            Commands.Add(commandAttribute, command);

            return this;
        }

        public ConsoleApplicationBuilder ProvideDependency<T>() where T : new()
        {
            //检测是否已经有T的实例
            if (!Dependencies.OfType<T>().Any()) Dependencies.Add(new T());

            return this;
        }

        public ConsoleApplicationBuilder UseConsoleBackend<T>() where T : IConsoleApplicationConsoleBackend, new()
        {
            if (ConsoleBackend != null) throw new RyoException("已配置控制台后端");

            ConsoleBackend = new T();
            ConsoleBackend.InitBackend(Profile);

            return this;
        }

        public ConsoleApplicationBuilder UseDefaultConsoleBackend()
        {
            return UseConsoleBackend<DefaultConsoleApplicationConsoleBackend>();
        }
    }

    public class ConsoleApplicationProfile
    {

        public string Name { get; init; } = "Ryo Console";
        public string Description { get; init; } = "©2024 Earzu Chan";
        public string Version { get; init; } = "1.0.0.0";
        public Encoding Encoding { get; init; } = Encoding.UTF8;

        public CommandRegistrationStrategy CommandRegistrationStrategy { get; init; } = CommandRegistrationStrategy.ScanAndRegisterAutomatically;
        public bool IsDev { get; init; } = false;
    }

    public class ConsoleApplicationContext
    {
        private readonly ConsoleApplication App;

        internal ConsoleApplicationContext(ConsoleApplication app) => App = app;

        public T Inject<T>() where T : class =>
            ControlFlowUtils.TryCatchingThenThrow<T>("Cannot inject dependency", () => App.Dependencies.OfType<T>().First(), new Dictionary<Type, string> { { typeof(InvalidOperationException), "No such a dependency" } });

        public Dictionary<CommandAttribute, Type> Commands => App.Commands;

        public ConsoleApplicationProfile Profile => App.Profile;

        public void PrintLine(string text = "") => App.ConsoleBackend.PrintLine(text);

        public string ReadLine(string title, bool hideColon = false)
        {
            App.ConsoleBackend.Print($"{title}{(hideColon ? "" : ": ")}");
            return App.ConsoleBackend.ReadLine();
        }

        public void Print(string text) => App.ConsoleBackend.Print(text);

        public bool ReadYesOrNo(string title, ConsoleKey yesKey = ConsoleKey.Y, ConsoleKey noKey = ConsoleKey.N)
        {
            string yesKeyName = ConsoleUtils.SolveKeyName(yesKey.ToString());
            string noKeyName = ConsoleUtils.SolveKeyName(noKey.ToString());
            ConsoleKey yesKeySecond = ConsoleUtils.SolveSecondKey(yesKey);
            ConsoleKey noKeySecond = ConsoleUtils.SolveSecondKey(noKey);

            while (true)
            {
                App.ConsoleBackend.Print($"{title} [{yesKeyName}/{noKeyName}]: ");
                var inputKey = App.ConsoleBackend.ReadKey();
                if (inputKey == yesKey || inputKey == yesKeySecond) return true;
                else if (inputKey == noKey || inputKey == noKeySecond) return false;
                App.ConsoleBackend.PrintLine($"Invalid inputKey. Please press '{yesKeyName}' or '{noKeyName}'.");
            }
        }
    }
}
