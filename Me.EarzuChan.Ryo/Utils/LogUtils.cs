<<<<<<< HEAD
﻿using System.Diagnostics;

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
=======
﻿using System.Diagnostics;

namespace Me.EarzuChan.Ryo.Utils
{
    public class LogUtils
    {
        public static LogUtils INSTANCE { get { instance ??= new(); return instance; } }
        private static LogUtils? instance;
        private Action<string> Logger = str => Trace.WriteLine(str);
        public bool AllowPrintDebugInfo = false;

        public void SetLogger(Action<string> logger) => Logger = logger;

        public void PrintError(String info, Exception e, bool printStack = true)
        {
            Logger("错误：" + info + "，因为：" + e.Message);
            if (e.StackTrace != null && printStack)
            {
                var stack = e.StackTrace;
                if (e.InnerException != null && e.InnerException.StackTrace != null) stack = e.InnerException.StackTrace + "\n" + stack;
                Logger(stack);
            }
        }

        public void PrintInfo(params string[] args)
        {
            Logger(string.Join(' ', args));
        }

        public void PrintDebugInfo(params string[] args)
        {
            if (AllowPrintDebugInfo) PrintInfo("额外调试信息：" + args);
        }
    }
>>>>>>> 62013cbf2e8e2181ffdf41d357ae518f5ad00c74
}