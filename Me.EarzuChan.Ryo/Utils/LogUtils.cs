using System.Diagnostics;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class LogUtils
    {
        private static Action<string> Logger = str => Trace.WriteLine(str);
        public const bool AllowPrintDebugInfo = false;

        public static void SetLogger(Action<string> logger) => Logger = logger;

        public static void PrintError(String info, Exception e, bool printStack = true) => Logger(MakeErrorText(info, e, printStack));

        public static string MakeErrorText(string info, Exception? e, bool withStack = false)
        {
            var str = $"Error: {info}";
            while (e != null)
            {
                str += $"\n\nError Details:\n--------------\nException: {e.GetType()}\nMessage: {e.Message}\nSource: {e.Source}\nStack Trace:\n";
                if (withStack) str += e.StackTrace;
                e = e.InnerException;
            }
            return str;
        }

        public static void PrintInfo(params string[] args) => Logger(string.Join(' ', args));
    }
}