namespace Me.EarzuChan.Ryo.ConsoleSystem.Backend
{
    public interface IConsoleApplicationConsoleBackend
    {
        public void PrintLine(string text);

        public string ReadLine();

        public void Print(string text);

        public ConsoleKey ReadKey();

        public void InitBackend(ConsoleApplicationProfile profile);
    }

    internal class DefaultConsoleApplicationConsoleBackend : IConsoleApplicationConsoleBackend
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

        public void InitBackend(ConsoleApplicationProfile profile)
        {
            Console.InputEncoding = profile.Encoding;
            Console.OutputEncoding = profile.Encoding;
        }
    }
}
