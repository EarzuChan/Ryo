using System.Diagnostics;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class LogUtils
    {
        private static Action<string> Logger = str => Trace.WriteLine(str);
        public const bool AllowPrintDebugInfo = false;

        public static void SetLogger(Action<string> logger) => Logger = logger;

        public static void PrintError(String info, Exception e, bool printStack = true) => Logger(TextUtils.MakeErrorText(info, e, printStack));

        public static void PrintInfo(params string[] args) => Logger(string.Join(' ', args));
    }
}