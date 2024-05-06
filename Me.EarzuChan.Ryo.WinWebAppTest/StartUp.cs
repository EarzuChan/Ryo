using Me.EarzuChan.Ryo.WinWebAppSystem;
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
                .RegisterAppEventListener(WinWebAppEvent.AppWindowStateChanged, (ctx, state) =>
                {
                    ctx.EmitWebEvent(new WebEvent("AppWindowStateChanged", new object[] { state! }));
                })
                .Build()
                .Run();
        }
    }
}