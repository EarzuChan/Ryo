using Me.EarzuChan.Ryo.ConsoleSystem.OldCommandManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Me.EarzuChan.Ryo.ConsoleSystem.OldCommands.IOldCommand;

namespace Me.EarzuChan.Ryo.ConsoleSystem.OldCommands
{
    [Obsolete("兼容性Babe")]
    public interface IOldCommand
    {
        public const string OLD_NO_DESCRIPTION = "No description available.";

        void Execute(CommandFrame commandFrame);

        public class CommandFrame
        {
            // 要不要独立包装，以免披露太多
            public readonly OldCommandManager CommandManager;

            public CommandFrame(OldCommandManager commandManager) => CommandManager = commandManager;

            public void PrintLine(string text = "") => CommandManager.ConsoleImplement.PrintLine(text);

            public string ReadLine(string title, bool hideColon = false)
            {
                CommandManager.ConsoleImplement.Print($"{title}{(hideColon ? "" : ": ")}");
                return CommandManager.ConsoleImplement.ReadLine();
            }

            public void Print(string text) => CommandManager.ConsoleImplement.Print(text);

            public bool ReadYesOrNo(string title, ConsoleKey yesKey = ConsoleKey.Y, ConsoleKey noKey = ConsoleKey.N)
            {
                string yesKeyName = SolveKeyName(yesKey.ToString());
                string noKeyName = SolveKeyName(noKey.ToString());
                ConsoleKey yesKeySecond = SolveSecondKey(yesKey);
                ConsoleKey noKeySecond = SolveSecondKey(noKey);

                while (true)
                {
                    CommandManager.ConsoleImplement.Print($"{title} [{yesKeyName}/{noKeyName}]: ");
                    var inputKey = CommandManager.ConsoleImplement.ReadKey();
                    if (inputKey == yesKey || inputKey == yesKeySecond) return true;
                    else if (inputKey == noKey || inputKey == noKeySecond) return false;
                    CommandManager.ConsoleImplement.PrintLine($"Invalid inputKey. Please press '{yesKeyName}' or '{noKeyName}'.");
                }
            }

            private static string SolveKeyName(string oriKeyName) => oriKeyName.StartsWith('D') && oriKeyName.Length == 2 ? oriKeyName[1..] : oriKeyName == "DownArrow" ? "↓" : oriKeyName == "UpArrow" ? "↑" : oriKeyName;

            private static ConsoleKey SolveSecondKey(ConsoleKey oriKey) => oriKey == ConsoleKey.D1 ? ConsoleKey.NumPad1 : oriKey == ConsoleKey.D0 ? ConsoleKey.NumPad0 : oriKey;
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class OldCommandAttribute : Attribute
        {
            public string Name { get; set; }

            public string Information { get; set; }

            public bool IsDev { get; set; }

            public OldCommandAttribute(string name, string info = OLD_NO_DESCRIPTION)
            {
                Name = name;
                Information = info;
            }

            public OldCommandAttribute(string name, bool isDev, string info = OLD_NO_DESCRIPTION)
            {
                Name = name;
                IsDev = isDev;
                Information = info;
            }
        }
    }

    [OldCommand("Help", "Get the help infomations of this application")]
    public class HelpCommand : IOldCommand
    {
        public void Execute(CommandFrame commandFrame)
        {
            commandFrame.PrintLine($"{commandFrame.CommandManager.commands.Count} Available Commands:\n-------------------");

            int i = 1;
            foreach (var cmd in commandFrame.CommandManager.commands) commandFrame.PrintLine($"{i++}. {cmd.Key.Name} - {cmd.Key.Information}");

            commandFrame.PrintLine($"\nType 'exit' to exit the {commandFrame.CommandManager.ConsoleInfo.Name}.");
        }
    }
}
