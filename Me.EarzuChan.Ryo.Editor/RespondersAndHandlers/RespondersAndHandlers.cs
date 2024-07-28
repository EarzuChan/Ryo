using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Kurisu.WebCalls.Responders;
using Me.EarzuChan.Ryo.Kurisu.WebEvents.Handlers;
using System.Diagnostics;
using System.IO;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Editor.Utils;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.AppEvents.Handlers;

namespace Me.EarzuChan.Ryo.Editor.RespondersAndHandlers;

[WebCallResponder("GetAllDataTypes")]
public class GetAllDataTypesResponder : IWebCallResponder
{
    public WebResponse Respond(KurisuAppContext context) =>
        new(WebResponseState.Success, DataTypeSchemaUtils.GetAllDataTypeSchemas());
}

[WebEventHandler("OpenFile")]
public class OpenFileHandler : IWebEventHandler
{
    public void Handle(KurisuAppContext context)
    {
        var massManager = context.Inject<MassManager>()!;

        var filePath = MiscUtils.OpenFileByDialog("MassFile", "fs");
        if (filePath is null) return;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        massManager.LoadMassFile(filePath, fileName);
    }
}

[WebEventHandler("OpenLink")]
public class OpenLinkHandler(string link) : IWebEventHandler
{
    public void Handle(KurisuAppContext context) => Process.Start(new ProcessStartInfo
    {
        FileName = link,
        UseShellExecute = true
    });
}

[AppEventHandler(AppEventType.AppInitialized)]
public class AppInitializedHandler : IAppEventHandler
{
    public void Handle(KurisuAppContext context)
    {
        context.Inject<MassManager>()!.MassFilesChanged +=
            (masses) => MiscUtils.EmitOpenedMasses(context, masses);
    }
}

[WebEventHandler("NotifyOpenedFiles")]
public class NotifyOpenedFilesHandler : IWebEventHandler
{
    public void Handle(KurisuAppContext context) =>
        MiscUtils.EmitOpenedMasses(context, context.Inject<MassManager>()!.MassFiles);
}