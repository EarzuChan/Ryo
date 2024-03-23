using Me.EarzuChan.Ryo.ConsoleSystem.CommandManagement;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Utils;
using System.Text;

namespace Me.EarzuChan.Ryo.ConsoleFrontEnd
{
    public static class StartUp
    {
        public static CommandManager CommandManager = new();

        public static MassManager MassManager = new();

        public static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.Unicode;

            LogUtils.Logger += str => Console.WriteLine(str); // 重定向日志输出_使用缺省

#if DEBUG
            CommandManager.IsDev = true;
#endif

            // 我在思考一种新命令范式，构造时可选参数对接Builder，然后Exec方法有FrameArgs，提供Console对象可以读取写出

            CommandManager.InitConsole();
        }
    }
}