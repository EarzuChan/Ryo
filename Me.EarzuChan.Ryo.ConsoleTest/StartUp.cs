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
    .CreateBuilder()
    .UseDefaultConsoleBackend()
    .Build()
    .Run();