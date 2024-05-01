using Me.EarzuChan.Ryo.WinWebAppSystem;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents;
using System.Diagnostics;

namespace Me.EarzuChan.Ryo.WinWebAppTest
{
    public class StartUp
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
                .AddAppEventHandler(WinWebAppEvent.AppMaximizationChanged, (state, ctx) =>
                {
                    ctx.EmitWebEvent(new WebEvent { Name = "AppMaximizationChanged", Args = new object[] { state } });
                })
                .Build()
                .Run();
        }
    }
}