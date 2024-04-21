using System.Diagnostics;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class LogUtils
    {
        public static event Action<string> Logger;
        private static void TraceLogger(string str) => Trace.WriteLine(str);

        static LogUtils() => UseTraceLogger();

        public static void UseTraceLogger() => Logger += TraceLogger;

        public static void StopUsingTraceLogger() => Logger -= TraceLogger;

        public const bool AllowPrintDebugInfo = false;

        public static void PrintError(String info, Exception e, bool printStack = true) => Logger?.Invoke(TextUtils.MakeErrorMsgText(info, e, printStack));

        public static void PrintWarning(params string[] args) => Logger?.Invoke(TextUtils.MakeMsgText("Warning", args));

        public static void PrintInfo(params string[] args) => Logger?.Invoke(TextUtils.MakeMsgText("Info", args));
    }
}