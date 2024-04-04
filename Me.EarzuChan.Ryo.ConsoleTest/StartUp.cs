using Me.EarzuChan.Ryo.ConsoleSystem;
using Me.EarzuChan.Ryo.ConsoleSystem.Commands;
using Me.EarzuChan.Ryo.ConsoleSystem.OldCommandManagement;
using Me.EarzuChan.Ryo.ConsoleTest.Commands;
using Me.EarzuChan.Ryo.Core.Masses;
using System.Text;

/*Console.InputEncoding = Encoding.Unicode;

OldCommandManager commandManager = new(new OldConsoleInfo { Name = "Console Test" });

commandManager.InitConsole();*/

ConsoleApplication
    .CreateBuilder(new()
    {
        CommandRegistrationStrategy = CommandRegistrationStrategy.RegisterManually
    })
    .ProvideDependency<MassManager>()
    .UseDefaultConsoleBackend()
    .RegisterCmd(new("太美丽"), typeof(TsCsCommand))
    // 操你妈C#不支持匿名类继承，不能秀一波优雅注册了
    .Build()
    .Run();