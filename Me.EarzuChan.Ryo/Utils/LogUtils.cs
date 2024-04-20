using System.Diagnostics;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class LogUtils
    {
        public static event Action<string> Logger = str => Trace.WriteLine(str); // 有初始值

        public const bool AllowPrintDebugInfo = false;

        public static void PrintError(String info, Exception e, bool printStack = true) => Logger?.Invoke(TextUtils.MakeErrorText(info, e, printStack));

        public static void PrintWarning(params string[] args) => Logger?.Invoke(TextUtils.MakeMsgText("Warning", args));

        public static void PrintInfo(params string[] args) => Logger?.Invoke(TextUtils.MakeMsgText("Info", args));
    }
}