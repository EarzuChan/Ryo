using Me.EarzuChan.Ryo.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Me.EarzuChan.Ryo.ConsoleSystem.Commands.ICommand;

namespace Me.EarzuChan.Ryo.ConsoleSystem.CommandManagement
{
    public class CommandApplication
    {
        // Runner
        // Cmds
        // 依赖注入
        private ICommandApplicationConsoleBackend ConsoleBackend;
        private ArrayList Dependencies;

        public void Run()
        {
        }

        protected CommandApplication(ICommandApplicationConsoleBackend consoleBackend, ArrayList dependencies)
        {
            ConsoleBackend = consoleBackend;
            Dependencies = dependencies;
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
            private CommandApplicationProfile Profile;
            private ArrayList Dependencies = new();
            private ICommandApplicationConsoleBackend? ConsoleBackend;

            internal CommandApplicationBuilder(CommandApplicationProfile profile)
            {
                Profile = profile;
            }

            public CommandApplication Build()
            {
                if (ConsoleBackend == null) throw new RyoException("未配置控制台后端");

                CommandApplication application = new(ConsoleBackend, Dependencies);

                return application;
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

        public class CommandApplicationProfile
        {
            public string Name = "Ryo Console";
            public string Description = "©2024 Earzu Chan";
            public string Version = "1.0.0.0";
            public Encoding Encoding = Encoding.UTF8;
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
