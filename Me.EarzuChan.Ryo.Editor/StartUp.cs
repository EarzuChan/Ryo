using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Kurisu;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.WebEvents;
using System.Diagnostics;

namespace Me.EarzuChan.Ryo.Editor
{
    public static class StartUp
    {
        [STAThread]
        private static void Main(string[] args)
        {
            KurisuApp
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