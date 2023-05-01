using System.Diagnostics;

namespace Me.EarzuChan.Ryo.Utils
{
    public class LogUtil
    {
        public static LogUtil INSTANCE { get { instance ??= new(); return instance; } }
        private static LogUtil? instance;
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
}