using Me.EarzuChan.Ryo.Extensions.MassExtensions;
using Me.EarzuChan.Ryo.Kurisu;

namespace Me.EarzuChan.Ryo.AIEE;

public static class StartUp {
    [STAThread]
    private static void Main(string[] args) => KurisuApp
        .CreateBuilder(new(
            Icon: "AppResources/icon_ryo_app.ico",
            StartUpUrl: "https://ryo_web_frontend/index.html",
            DebugStartUpUrl: "http://localhost:5173/",
            Name: "Ryo App",
            DebugStartUpWithDebugUrl: true,
            VirtualHostName: "ryo_web_frontend"
#if DEBUG
            , IsDebug: true
#endif
        ))
        .UseDefaultHostBackend()
        .ProvideDependency<LocalVolumeManager>()
        .Build()
        .Run();
}