using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Kurisu.WebCalls.Responders;
using Me.EarzuChan.Ryo.Kurisu.WebEvents.Handlers;
using System.Diagnostics;
using System.IO;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Editor.Utils;
using Me.EarzuChan.Ryo.Extensions.MassExtensions;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.AppEvents.Handlers;
using Me.EarzuChan.Ryo.Utils;
using Newtonsoft.Json.Linq;

namespace Me.EarzuChan.Ryo.Editor.RespondersAndHandlers;

[WebCallResponder("GetAllDataTypes")]
public class GetAllDataTypesResponder : IWebCallResponder
{
    public WebResponse Respond(KurisuAppContext context) =>
        new(WebResponseState.Success, DataTypeSchemaUtils.GetAllDataTypeSchemas());
}

[WebEventHandler("OpenVolume")]
public class OpenFileHandler : IWebEventHandler
{
    public void Handle(KurisuAppContext context)
    {
        var massManager = context.Inject<LocalVolumeManager>()!;

        var filePath = MiscUtils.OpenFileByDialog("MassFile", "fs");
        if (filePath is null) return;

        massManager.OpenLocalVolume(filePath);
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
        context.Inject<LocalVolumeManager>()!.VolumesChanged +=
            (volumes) => MiscUtils.EmitOpenedVolumes(context, volumes);
    }
}

[WebEventHandler("NotifyOpenedVolumes")]
public class NotifyOpenedFilesHandler : IWebEventHandler
{
    public void Handle(KurisuAppContext context) =>
        MiscUtils.EmitOpenedVolumes(context, context.Inject<LocalVolumeManager>()!.Volumes);
}

// TODO:如果是基本类型，参数不是JObject，懆称冯的福
[WebCallResponder("SaveItem")]
public class SaveItemHandler(string volumeName, string itemName, JObject data) : IWebCallResponder
{
    public WebResponse Respond(KurisuAppContext context)
    {
        int newId = -1;

        context.Inject<LocalVolumeManager>()!.Also(it =>
        {
            Trace.WriteLine($"Saving {itemName} of {volumeName}: {data.GetType()}\n{data}");

            it.GetVolumeByName(volumeName)
                .Ensure(vol => vol[itemName].Type.JavaClassToRyoType().ToCsType().Ensure(typ =>
                {
                    Trace.WriteLine($"Got Cs Type {itemName}: {typ}");
                    data.ToObject(typ).Ensure(obj =>
                    {
                        vol.Add(itemName, obj);
                        newId = vol[itemName].Id;
                    });
                }));
        });

        return newId == -1
            ? new WebResponse(WebResponseState.Failure)
            : new WebResponse(WebResponseState.Success, newId);
    }
}

[WebEventHandler("CloseVolume")]
public class CloseVolumeHandler(string volumeName) : IWebEventHandler
{
    public void Handle(KurisuAppContext context) =>
        context.Inject<LocalVolumeManager>()!.Also(it =>
        {
            var volume = it.GetVolumeByName(volumeName);
            volume?.Also(it.Close);
        });
}

[WebEventHandler("SaveVolume")]
public class SaveVolumeHandler(string volumeName, bool saveAs) : IWebEventHandler
{
    public void Handle(KurisuAppContext context) =>
        context.Inject<LocalVolumeManager>()!.Also(it =>
        {
            Trace.WriteLine($"保存{volumeName} {saveAs}");
            var volume = it.GetVolumeByName(volumeName);
            volume?.Also(vol => it.Save(vol, () => MiscUtils.SaveFileByDialog("MassFile", "fs"), saveAs));
        });
}

[WebCallResponder("GetFullFileModel")]
public class GetFullFileModelResponder(string volumeName, int fileId) : IWebCallResponder
{
    public WebResponse Respond(KurisuAppContext context)
    {
        var volumeManager = context.Inject<LocalVolumeManager>()!;
        var volume = volumeManager.GetVolumeByName(volumeName);

        if (volume is null) return new(WebResponseState.Failure, "LocalVolume not found");

        return new(WebResponseState.Success, volume[fileId]);
    }
}