using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Kurisu.WebCalls.Responders;
using Me.EarzuChan.Ryo.Kurisu.WebEvents.Handlers;
using System.Diagnostics;
using System.IO;
using Me.EarzuChan.Ryo.Core.Codecations;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Editor.Data;
using Me.EarzuChan.Ryo.Editor.Utils;
using Me.EarzuChan.Ryo.Exceptions;
using Me.EarzuChan.Ryo.Extensions.MassExtensions;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.AppEvents.Handlers;
using Me.EarzuChan.Ryo.Utils;
using Newtonsoft.Json.Linq;

namespace Me.EarzuChan.Ryo.Editor.RespondersAndHandlers;

[WebEventHandler("NewVolume")]
public class NewVolumeHandler(string namePrefix) : IWebEventHandler
{
    public void Handle(KurisuAppContext context)
    {
        var localVolumeManager = context.Inject<LocalVolumeManager>()!;
        var newMassRepo = context.Inject<NewMassRepository>()!;

        localVolumeManager.CreateNewVolume($"{namePrefix}{newMassRepo.GetAndIncrementNewMassCounter()}");
    }
}

[WebCallResponder("GetAllDataTypes")]
public class GetAllDataTypesResponder : IWebCallResponder
{
    public WebResponse Respond(KurisuAppContext context) =>
        new(WebResponseState.Success, DataTypeSchemaUtils.GetAllDataTypeSchemas());
}

[WebEventHandler("OpenVolume")]
public class OpenVolumeHandler : IWebEventHandler
{
    public void Handle(KurisuAppContext context)
    {
        var localVolumeManager = context.Inject<LocalVolumeManager>()!;

        var filePath = MiscUtils.OpenFileByDialog("MassFile", "fs");
        if (filePath is null) return;

        localVolumeManager.OpenLocalVolume(filePath);
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
        var man = context.Inject<LocalVolumeManager>()!;
        
        man.VolumesChanged += (volumes) => MiscUtils.EmitOpenedVolumes(context, volumes);
        man.VolumeItemDeleted += (volume, itemId) => { context.EmitWebEvent(new("VolumeItemDeleted", volume, itemId)); };
        man.VolumeItemRenamed += (volume, old, name) => { context.EmitWebEvent(new("VolumeItemRenamed", volume, old, name)); };
        man.VolumeIdsRemapped += (volume, idMap) => { context.EmitWebEvent(new("VolumeIdsRemapped", volume, idMap)); };
    }
}

[WebEventHandler("NotifyOpenedVolumes")]
public class NotifyOpenedFilesHandler : IWebEventHandler
{
    public void Handle(KurisuAppContext context) =>
        MiscUtils.EmitOpenedVolumes(context, context.Inject<LocalVolumeManager>()!.Volumes);
}

// TODO：另外，无名项目怎么编辑/保存？
// TODO：如果是基本类型，参数不是JObject，懆称冯的福
[WebCallResponder("SaveItem")]
public class SaveItemResponder(string volumeName, string itemName, object data, string dataTypeName) : IWebCallResponder
{
    public WebResponse Respond(KurisuAppContext context)
    {
        var newId = -1;

        context.Inject<LocalVolumeManager>()!.Also(it =>
        {
            Trace.WriteLine($"Saving {itemName} of {volumeName}: {data.GetType()}");

            it.GetVolumeByName(volumeName)
                .EnsureNotNull(vol =>
                {
                    RyoType ryoType;

                    try
                    {
                        ryoType = dataTypeName.DataTypeNameToRyoType();
                    }
                    catch (Exception e2)
                    {
                        Trace.WriteLine($"我没招了：{e2.Message}");
                        throw new RyoException("取得RyoType失败", e2);
                    }


                    ryoType.ToCsType().EnsureNotNull(typ =>
                    {
                        Trace.WriteLine($"Got Cs Type {itemName}: {typ}");
                        switch (data)
                        {
                            case JObject jobj:
                                Trace.WriteLine("Data is JObject");
                                jobj.ToObject(typ).EnsureNotNull(obj =>
                                {
                                    vol.Add(itemName, obj);
                                    newId = vol[itemName].Id;
                                });
                                break;

                            case JArray jarr when typ.IsArray:
                                Trace.WriteLine($"Data is JArray, {jarr.Count} items");
                                jarr.ToObject(typ).EnsureNotNull(arr =>
                                {
                                    vol.Add(itemName, arr);
                                    newId = vol[itemName].Id;
                                });
                                break;

                            default:
                                Trace.WriteLine($"Data is not JObject or JArray, but {data.GetType()}");
                                vol.Add(itemName, data);
                                newId = vol[itemName].Id;
                                break;
                        }
                    });
                });
        });

        Trace.WriteLine($"Saved, new id: {newId}");

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

[WebEventHandler("GcVolume")] // FIXME：有问题，这样前端的已打开标签页Id怎么刷新？
public class GcVolumeHandler(string volumeName) : IWebEventHandler
{
    public void Handle(KurisuAppContext context) =>
        context.Inject<LocalVolumeManager>()!.Also(it =>
        {
            var volume = it.GetVolumeByName(volumeName);
            volume?.CollectGarbage();
        });
}

// TODO：复制副本（需要Mass里面改）
// TODO：客户端未保存提示，未写入后端项目提示
[WebEventHandler("DeleteItem")]
public class DeleteItemHandler(string volumeName, string itemName) : IWebEventHandler
{
    public void Handle(KurisuAppContext context) => context.Inject<LocalVolumeManager>()!.Also(it =>
    {
        Trace.WriteLine($"Deleting {itemName} of {volumeName}");

        it.GetVolumeByName(volumeName).EnsureNotNull(vol => vol.Remove(itemName));
    });
}

// TODO：前端实现这功能的入口
[WebEventHandler("GiveItemName")]
public class GiveItemNameHandler(string volumeName, int id, string name) : IWebEventHandler
{
    public void Handle(KurisuAppContext context) => context.Inject<LocalVolumeManager>()!.Also(it =>
    {
        Trace.WriteLine($"Giving No.{id} a name {name} of {volumeName}");

        it.GetVolumeByName(volumeName).EnsureNotNull(vol => vol.GiveName(id, name));
    });
}

[WebEventHandler("RenameItem")]
public class RenameItemHandler(string volumeName, string oldItemName, string newItemName) : IWebEventHandler
{
    public void Handle(KurisuAppContext context) => context.Inject<LocalVolumeManager>()!.Also(it =>
    {
        Trace.WriteLine($"Renaming {oldItemName} of {volumeName} to {newItemName}");

        it.GetVolumeByName(volumeName).EnsureNotNull(vol => vol.Rename(oldItemName, newItemName));
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