using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.ConsoleSystem.Utils
{
    public static class ConsoleUtils
    {
        public string[] ParseCommandLineArguments(string commandLine)
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
    }
}
