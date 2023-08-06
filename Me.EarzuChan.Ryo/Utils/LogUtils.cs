using System.Diagnostics;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class LogUtils
    {
        private static Action<string> Logger = str => Trace.WriteLine(str);
        public const bool AllowPrintDebugInfo = false;

        public static void SetLogger(Action<string> logger) => Logger = logger;

        public static void PrintError(String info, Exception e, bool printStack = true) => Logger(MakeErrorLog(info, e, printStack));

        public static string MakeErrorLog(string info, Exception e, bool printStack = false)
        {
            var str = "错误：" + info + "，因为：" + e.Message;
            if (e.StackTrace != null && printStack)
            {
                var stack = e.StackTrace;
                if (e.InnerException != null && e.InnerException.StackTrace != null) stack = e.InnerException.StackTrace + "\n" + stack;
                str += stack;
            }
            return str;
        }

        public static void PrintInfo(params string[] args) => Logger(string.Join(' ', args));
    }
}