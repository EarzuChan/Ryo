using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.WinWebAppSystem;
using Me.EarzuChan.Ryo.WinWebAppSystem.AppEvents;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents;
using System.Diagnostics;

namespace Me.EarzuChan.Ryo.WinWebAppTest
{
    public static class StartUp
    {
        [STAThread]
        private static void Main(string[] args)
        {
            WinWebApp
                .CreateBuilder(new()
                {
#if DEBUG
                    DebugMode = true,
                    UseIcon = false,
                    /*DebugAutomaticOpenDevTool = false,
                    DebugStartUpWithDebugUrl = false,*/
#endif
                })
                .UseDefaultWindowBackend()
                .ProvideDependency<MassManager>()
                .Build()
                .Run();
        }
    }
}