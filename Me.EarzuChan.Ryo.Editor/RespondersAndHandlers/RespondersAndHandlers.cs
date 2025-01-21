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

[OldWebCallResponder("GetAllDataTypes")]
public class GetAllDataTypesResponder : IOldWebCallResponder
{
    public OldWebResponse Respond(KurisuAppContext context) =>
        new(WebResponseState.Success, DataTypeSchemaUtils.GetAllDataTypeSchemas());
}

[WebEventHandler("OpenVolume")]
public class OpenVolumeHandler : IWebEventHandler
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
[OldWebCallResponder("SaveItem")]
public class SaveItemHandler(string volumeName, string itemName, object data) : IOldWebCallResponder
{
    public OldWebResponse Respond(KurisuAppContext context)
    {
        int newId = -1;

        context.Inject<LocalVolumeManager>()!.Also(it =>
        {
            Trace.WriteLine($"Saving {itemName} of {volumeName}: {data.GetType()}");

            it.GetVolumeByName(volumeName)
                .Ensure(vol => vol[itemName].RawJavaClass.JavaClassToRyoType().ToCsType().Ensure(typ =>
                {
                    Trace.WriteLine($"Got Cs Type {itemName}: {typ}");
                    switch (data)
                    {
                        case JObject jobj:
                            Trace.WriteLine("Data is JObject");
                            jobj.ToObject(typ).Ensure(obj =>
                            {
                                vol.Add(itemName, obj);
                                newId = vol[itemName].Id;
                            });
                            break;
                        case JArray jarr when typ.IsArray:
                            Trace.WriteLine($"Data is JArray, {jarr.Count} items");
                            jarr.ToObject(typ).Ensure(arr =>
                            {
                                vol.Add(itemName, arr);
                                newId = vol[itemName].Id;
                            });
                            break;
                        default:
                            Trace.WriteLine($"Data is not JObject or JArray, but {data.GetType()}");
                            vol.Add(itemName, data);
                            break;
                    }
                }));
        });

        return newId == -1
            ? new OldWebResponse(WebResponseState.Failure)
            : new OldWebResponse(WebResponseState.Success, newId);
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

[OldWebCallResponder("GetFullFileModel")]
public class GetFullFileModelResponder(string volumeName, int fileId) : IOldWebCallResponder
{
    public OldWebResponse Respond(KurisuAppContext context)
    {
        var volumeManager = context.Inject<LocalVolumeManager>()!;
        var volume = volumeManager.GetVolumeByName(volumeName);

        if (volume is null) return new(WebResponseState.Failure, "LocalVolume not found");

        return new(WebResponseState.Success, volume[fileId]);
    }
}