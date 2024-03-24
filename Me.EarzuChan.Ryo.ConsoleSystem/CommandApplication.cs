using Me.EarzuChan.Ryo.ConsoleSystem.CommandManagement;
using Me.EarzuChan.Ryo.ConsoleSystem.Commands;
using Me.EarzuChan.Ryo.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Me.EarzuChan.Ryo.ConsoleSystem.Commands.ICommand;

namespace Me.EarzuChan.Ryo.ConsoleSystem
{
    public class CommandApplication
    {
        // Runner
        // Cmds
        // 依赖注入
        private readonly ICommandApplicationConsoleBackend ConsoleBackend;
        private readonly ArrayList Dependencies;
        private readonly Dictionary<CommandAttribute, Type> Commands;

        public void Run()
        {
            throw new NotImplementedException("啊啊啊还要包装Nmmd");
        }

        public async void RunAsync()
        {
            throw new NotImplementedException("待办");
        }

        protected CommandApplication(ICommandApplicationConsoleBackend consoleBackend, ArrayList dependencies, Dictionary<CommandAttribute, Type> commands)
        {
            ConsoleBackend = consoleBackend;
            Dependencies = dependencies;
            Commands = commands;
        }

        public static CommandApplicationBuilder CreateBuilder(CommandApplicationProfile profile)
        {
            CommandApplicationBuilder builder = new(profile);
            return builder;
        }

        public static CommandApplicationBuilder CreateBuilder()
        {
            return CreateBuilder(new());
        }

        public class CommandApplicationBuilder
        {
            private readonly CommandApplicationProfile Profile;
            private readonly ArrayList Dependencies = new();
            private readonly Dictionary<CommandAttribute, Type> Commands = new();
            private ICommandApplicationConsoleBackend? ConsoleBackend;

            internal CommandApplicationBuilder(CommandApplicationProfile profile)
            {
                Profile = profile;
            }

            public CommandApplication Build()
            {
                if (ConsoleBackend == null) throw new RyoException("未配置控制台后端");

                if (Profile.CommandRegistrationStrategy == CommandRegistrationStrategy.ScanAndRegisterAutomatically) ScanCmds();

                CommandApplication application = new(ConsoleBackend, Dependencies, Commands);

                return application;
            }

            public void ScanCmds()
            {
                Commands.Clear();

                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
                foreach (var type in types)
                {
                    var attribute = type.GetCustomAttribute<CommandAttribute>();
                    if (attribute != null && typeof(ICommand).IsAssignableFrom(type))
                    {
                        if (attribute.IsDev && !Profile.IsDev) continue;
                        // DEBUG逻辑
                        // UsefulUtils.INSTANCE.PrintInfo("标识符", (attribute == null ? "就是" : "不是"), "类型", (type == null ? "就是" : "不是"));
                        Commands.Add(attribute, type);
                    }
                }
            }

            public CommandApplicationBuilder AddDependency<T>() where T : new()
            {
                Dependencies.Add(new T());

                return this;
            }

            public CommandApplicationBuilder UseConsoleBackend<T>() where T : ICommandApplicationConsoleBackend, new()
            {
                if (ConsoleBackend != null) throw new RyoException("已配置控制台后端");

                ConsoleBackend = new T();
                ConsoleBackend.InitBackend(Profile);

                return this;
            }

            public CommandApplicationBuilder UseDefaultConsoleBackend()
            {
                return UseConsoleBackend<DefaultCommandApplicationConsoleBackend>();
            }
        }

        public enum CommandRegistrationStrategy
        {
            ScanAndRegisterAutomatically,
            RegisterManually,
        }

        public class CommandApplicationProfile
        {

            public string Name = "Ryo Console";
            public string Description = "©2024 Earzu Chan";
            public string Version = "1.0.0.0";
            public Encoding Encoding = Encoding.UTF8;

            public CommandRegistrationStrategy CommandRegistrationStrategy = CommandRegistrationStrategy.ScanAndRegisterAutomatically;
            public bool IsDev = false;
        }

        public interface ICommandApplicationConsoleBackend
        {
            public void PrintLine(string text);

            public string ReadLine();

            public void Print(string text);

            public ConsoleKey ReadKey();

            public void InitBackend(CommandApplicationProfile profile);
        }

        internal class DefaultCommandApplicationConsoleBackend : ICommandApplicationConsoleBackend
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

            public void InitBackend(CommandApplicationProfile profile)
            {
                Console.InputEncoding = profile.Encoding;
                Console.OutputEncoding = profile.Encoding;
            }
        }
    }
}
