<<<<<<< HEAD
﻿using Me.EarzuChan.Ryo.Commands;
using Me.EarzuChan.Ryo.Utils;
using System.Reflection;
using System.Text;

namespace Me.EarzuChan.Ryo
{
    public class ProgramMainBlob
    {
        static void Main(string[] args)
        {
            //
            // Console.WriteLine(FormatUtils.NewtonsoftItemToJson(FormatUtils.SystemBinaryToItem(File.ReadAllBytes("E:\\2341\\content_chats_introteddy1.DialogueTreeDescriptor.bin"))));

            // 系统配置
            Console.InputEncoding = Encoding.Unicode;

            LogUtils.SetLogger(str => Console.WriteLine(str)); // 重定向日志输出_使用缺省

            CommandManager commandManager = new();
#if DEBUG
            commandManager.IsDev = true;
            // 我在思考一种新命令范式，构造时可选参数对接Builder，然后Exec方法有FrameArgs，提供Console对象可以读取写出
#endif

            //LogUtil.INSTANCE.PrintInfo($"当前Dev状态：{CommandManager.INSTANCE.IsDev}");
            if (args.Length > 0)
            {
                commandManager.CommandFrame.PrintLine("=========================================================\n     Ryo Console [Special Effects Mode]\n=========================================================\nError: Quick Access Feature is currently disabled.\nPlease run the console without any parameters for full functionality.\nExiting Ryo Console... Thank you for understanding.");
                // TODO:真正的快速使用
                /*commandManager.RunningWithArgs = true;
                commandManager.ParseCommand(args);*/
                return;
            }

            // Welcome Info
            commandManager.CommandFrame.PrintLine($"=========================================================\n        Ryo Console [Version {Assembly.GetExecutingAssembly().GetName().Version}]\n   Copyright (C) Earzu Chan. All rights reserved.\n=========================================================\n\nType 'help' to see the list of available commands.");

            while (true)
            {
                string input = commandManager.CommandFrame.ReadLine("\n> ", true).Trim();
                if (input.ToLower() == "exit") break;

                commandManager.ParseCommandLine(input);
            }

            commandManager.CommandFrame.PrintLine("\nExiting Ryo Console... Have a great day!");

            Thread.Sleep(1500);
        }
    }
=======
﻿using Me.EarzuChan.Ryo.Commands;
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
>>>>>>> 62013cbf2e8e2181ffdf41d357ae518f5ad00c74
}