﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.ConsoleSystem.Utils
{
    public static class ConsoleUtils
    {
        public static string[] ParseCommandLineArguments(string commandLine)
        {
            List<string> arguments = new();

            StringBuilder currentArgument = new();
            bool insideQuote = false;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                bool hasBefore = i != 0;

                if (c == '"' && (!hasBefore || commandLine[i - 1] != '\\')) insideQuote = !insideQuote;
                else if (c == ' ' && !insideQuote)
                {
                    if (currentArgument.Length > 0)
                    {
                        arguments.Add(currentArgument.ToString());
                        currentArgument.Clear();
                    }
                }
                else currentArgument.Append(c);
            }


            if (currentArgument.Length > 0) arguments.Add(currentArgument.ToString());

            return arguments.ToArray();
        }

        public static string SolveKeyName(string oriKeyName) => oriKeyName.StartsWith('D') && oriKeyName.Length == 2 ? oriKeyName[1..] : oriKeyName == "DownArrow" ? "↓" : oriKeyName == "UpArrow" ? "↑" : oriKeyName;

        public static ConsoleKey SolveSecondKey(ConsoleKey oriKey) => oriKey == ConsoleKey.D1 ? ConsoleKey.NumPad1 : oriKey == ConsoleKey.D0 ? ConsoleKey.NumPad0 : oriKey;
    }
}
