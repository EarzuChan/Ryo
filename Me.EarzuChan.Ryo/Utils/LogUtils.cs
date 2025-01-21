using System.Diagnostics;

namespace Me.EarzuChan.Ryo.Utils;

public static class LogUtils
{
    public static event Action<string>? Logger;

    private static void InternalTraceLogger(string str) => Trace.WriteLine(str);
    private static bool usingInternalTraceLogger = false;

    static LogUtils()
    {
        UseInternalTraceLogger();
    }

    public static void UseInternalTraceLogger()
    {
        if (usingInternalTraceLogger) throw new InvalidOperationException("InternalTraceLogger is already in use.");
        
        Logger += InternalTraceLogger;
        usingInternalTraceLogger = true;
    }

    public static void StopUsingInternalTraceLogger()
    {
        if (!usingInternalTraceLogger) throw new InvalidOperationException("InternalTraceLogger is not in use.");

        Logger -= InternalTraceLogger;
        usingInternalTraceLogger = false;
    }

    public static void PrintError(String info, Exception e, bool printStack = true) =>
        Logger?.Invoke(TextUtils.MakeErrorMsgText(info, e, printStack));

    public static void PrintWarning(params string[] args) => Logger?.Invoke(TextUtils.MakeMsgText("Warning", args));

    public static void PrintInfo(params string[] args) => Logger?.Invoke(TextUtils.MakeMsgText("Info", args));
}