
using Me.EarzuChan.Ryo.ConsoleSystem;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Utils;

#if DEBUG
LogUtils.Logger += str => Console.WriteLine(str);
#endif

ConsoleApplication
    .CreateBuilder(new ConsoleApplicationProfile
    {
#if DEBUG
        IsDev = true,
#endif
    })
    .ProvideDependency<MassManager>()
    .UseDefaultConsoleBackend()
    .Build()
    .Run();
