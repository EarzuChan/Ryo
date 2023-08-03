using Me.EarzuChan.Ryo.Commands;
using Me.EarzuChan.Ryo.Utils;
using System.Reflection;
using System.Text;

namespace Me.EarzuChan.Ryo
{
    public class ProgramMainBlob
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.Unicode;

            LogUtils.INSTANCE.SetLogger(str => Console.WriteLine(str));
#if DEBUG
            CommandManager.INSTANCE.IsDev = true;
            // 我在思考一种新命令范式，构造时可选参数对接Builder，然后Exec方法有FrameArgs，提供Console对象可以读取写出
#endif

            //LogUtil.INSTANCE.PrintInfo($"当前Dev状态：{CommandManager.INSTANCE.IsDev}");
            if (args.Length > 0)
            {
                CommandManager.INSTANCE.RunningWithArgs = true;
                CommandManager.INSTANCE.ParseCommand(args);
                return;
            }

            Console.WriteLine($"Ryo Console [Version {Assembly.GetExecutingAssembly().GetName().Version}]\nCopyright (C) Earzu Chan. All rights reserved.");
            while (true)
            {
                Console.Write("\nE:\\Ryo\\User>");
                string input = Console.ReadLine()!.Trim();
                if (input.ToLower() == "exit") break;

                CommandManager.INSTANCE.ParseCommandLine(input);
            }
        }
    }
}