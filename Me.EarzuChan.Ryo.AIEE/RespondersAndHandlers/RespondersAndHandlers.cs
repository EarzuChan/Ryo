using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Kurisu.WebCalls.Responders;
using Me.EarzuChan.Ryo.Kurisu.WebEvents.Handlers;
using System.Diagnostics;
using Me.EarzuChan.Ryo.AIEE.Data;
using Me.EarzuChan.Ryo.AIEE.Exceptions;
using Me.EarzuChan.Ryo.AIEE.Utils;
using Me.EarzuChan.Ryo.Core.Codecations;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Exceptions;
using Me.EarzuChan.Ryo.Extensions.MassExtensions;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.AppEvents.Handlers;
using Me.EarzuChan.Ryo.Utils;
using Newtonsoft.Json.Linq;

namespace Me.EarzuChan.Ryo.AIEE.RespondersAndHandlers;

[WebEventHandler("NewVolume")]
public class NewVolumeHandler(string namePrefix) : IWebEventHandler {
    public void Handle(KurisuAppContext context) {
        var localVolumeManager = context.Inject<LocalVolumeManager>()!;
        var newMassRepo = context.Inject<NewMassRepository>()!;

        localVolumeManager.CreateNewVolume($"{namePrefix}{newMassRepo.GetAndIncrementNewMassCounter()}");
    }
}

[WebCallResponder("GetAllDataTypes")]
public class GetAllDataTypesResponder : IWebCallResponder {
    public WebResponse Respond(KurisuAppContext context) => new(WebResponseState.Success, DataTypeSchemaUtils.GetAllDataTypeSchemas());
}

[WebEventHandler("OpenVolume")]
public class OpenVolumeHandler : IWebEventHandler {
    public void Handle(KurisuAppContext context) {
        var localVolumeManager = context.Inject<LocalVolumeManager>()!;

        var filePath = context.ExecuteAppCommand<string?>(KurisuAppCommand.OpenFileDialog, "MassFile", "fs");
        if (filePath is null) return;

        localVolumeManager.OpenLocalVolume(filePath);
    }
}

[WebEventHandler("OpenLink")]
public class OpenLinkHandler(string link) : IWebEventHandler {
    public void Handle(KurisuAppContext context) => Process.Start(new ProcessStartInfo {
        FileName = link,
        UseShellExecute = true
    });
}

[AppEventHandler(AppEventType.AppInitialized)]
public class AppInitializedHandler : IAppEventHandler {
    public void Handle(KurisuAppContext context) {
        var man = context.Inject<LocalVolumeManager>()!;

        man.VolumesChanged += (volumes) => MiscUtils.EmitOpenedVolumes(context, volumes);
        man.VolumeItemDeleted += (volume, itemId) => { context.EmitWebEvent(new("VolumeItemDeleted", volume, itemId)); };
        man.VolumeItemRenamed += (volume, old, name) => { context.EmitWebEvent(new("VolumeItemRenamed", volume, old, name)); };
        man.VolumeIdsRemapped += (volume, idMap) => { context.EmitWebEvent(new("VolumeIdsRemapped", volume, idMap)); };
    }
}

[WebEventHandler("NotifyOpenedVolumes")]
public class NotifyOpenedFilesHandler : IWebEventHandler {
    public void Handle(KurisuAppContext context) =>
        MiscUtils.EmitOpenedVolumes(context, context.Inject<LocalVolumeManager>()!.Volumes);
}

// TODO：另外，无名项目怎么编辑/保存？
[WebCallResponder("SaveItem")]
public class SaveItemResponder(string volumeName, int itemId, string itemName, object data, string dataTypeName, int expectedVolumeRevision) : IWebCallResponder {
    private static object ConvertPayload(object payload, Type targetType) {
        switch (payload) {
            case JObject jobj:
                Trace.WriteLine("Data is JObject");
                return jobj.ToObject(targetType) ?? throw new RyoException($"无法将 JObject 转换到 {targetType}");

            case JArray jarr when targetType.IsArray:
                Trace.WriteLine($"Data is JArray, {jarr.Count} items");
                return jarr.ToObject(targetType) ?? throw new RyoException($"无法将 JArray 转换到 {targetType}");

            default:
                Trace.WriteLine($"Data is not JObject/JArray, fallback raw type: {payload.GetType()}");
                return payload;
        }
    }

    public WebResponse Respond(KurisuAppContext context) {
        var newId = -1;
        var volumeRevision = -1;

        context.Inject<LocalVolumeManager>()!.Also(it => {
            Trace.WriteLine($"Saving {itemName}#{itemId} of {volumeName}: {data.GetType()}");

            it.GetVolumeByName(volumeName)
                .EnsureNotNull(vol => {
                    if (expectedVolumeRevision >= 0 && vol.Revision != expectedVolumeRevision) throw new RyoException($"卷版本冲突：期望{expectedVolumeRevision}，当前{vol.Revision}。请先刷新后再保存。");

                    RyoType ryoType;

                    try {
                        ryoType = dataTypeName.DataTypeNameToRyoType();
                    } catch (Exception e2) {
                        Trace.WriteLine($"我没招了：{e2.Message}");
                        throw new RyoException("取得RyoType失败", e2);
                    }


                    ryoType.ToCsType().EnsureNotNull(typ => {
                        Trace.WriteLine($"Got Cs Type {itemName}: {typ}");
                        var payload = ConvertPayload(data, typ);

                        var targetId = itemId;
                        var hasTargetId = targetId >= 0 && vol.IdStrPairs.Values.Contains(targetId);
                        if (!hasTargetId && vol.IdStrPairs.TryGetValue(itemName, out var idByName)) {
                            targetId = idByName;
                            hasTargetId = true;
                        }

                        if (hasTargetId) {
                            Trace.WriteLine($"Upsert by id: {targetId}");
                            vol.Set(targetId, payload);
                            newId = targetId;
                        } else {
                            Trace.WriteLine($"Upsert as add: {itemName}");
                            vol.Add(itemName, payload);
                            newId = vol[itemName].Id;
                        }

                        volumeRevision = vol.Revision;
                    });
                });
        });

        Trace.WriteLine($"Saved, new id: {newId}, revision: {volumeRevision}");

        return newId == -1 || volumeRevision == -1
            ? WebResponse.Failure(EditorErrorCode.SaveItemFailed, "SaveItem 未获得有效的新 id 或 revision")
            : WebResponse.Success(newId, volumeRevision);
    }
}

[WebEventHandler("CloseVolume")]
public class CloseVolumeHandler(string volumeName) : IWebEventHandler {
    public void Handle(KurisuAppContext context) => context.Inject<LocalVolumeManager>()!.Also(it => {
        var volume = it.GetVolumeByName(volumeName);
        volume?.Also(it.Close);
    });
}

[WebEventHandler("SaveVolume")]
public class SaveVolumeHandler(string volumeName, bool saveAs) : IWebEventHandler {
    public void Handle(KurisuAppContext context) => context.Inject<LocalVolumeManager>()!.Also(it => {
        Trace.WriteLine($"保存{volumeName} {saveAs}");
        var volume = it.GetVolumeByName(volumeName);
        volume?.Also(vol => it.Save(
            vol,
            () => context.ExecuteAppCommand<string?>(KurisuAppCommand.SaveFileDialog, "MassFile", "fs"),
            saveAs
        ));
    });
}

[WebEventHandler("GcVolume")]
public class GcVolumeHandler(string volumeName) : IWebEventHandler {
    public void Handle(KurisuAppContext context) => context.Inject<LocalVolumeManager>()!.Also(it => {
        var volume = it.GetVolumeByName(volumeName);
        volume?.CollectGarbage();
    });
}

// TODO：复制副本（需要Mass里面改）
// TODO：客户端未保存提示，未写入后端项目提示
[WebEventHandler("DeleteItem")]
public class DeleteItemHandler(string volumeName, string itemName) : IWebEventHandler {
    public void Handle(KurisuAppContext context) => context.Inject<LocalVolumeManager>()!.Also(it => {
        Trace.WriteLine($"Deleting {itemName} of {volumeName}");

        it.GetVolumeByName(volumeName).EnsureNotNull(vol => vol.Remove(itemName));
    });
}

// TODO：前端实现这功能的入口
[WebEventHandler("GiveItemName")]
public class GiveItemNameHandler(string volumeName, int id, string name) : IWebEventHandler {
    public void Handle(KurisuAppContext context) => context.Inject<LocalVolumeManager>()!.Also(it => {
        Trace.WriteLine($"Giving No.{id} a name {name} of {volumeName}");

        it.GetVolumeByName(volumeName).EnsureNotNull(vol => vol.GiveName(id, name));
    });
}

[WebEventHandler("RenameItem")]
public class RenameItemHandler(string volumeName, string oldItemName, string newItemName) : IWebEventHandler {
    public void Handle(KurisuAppContext context) => context.Inject<LocalVolumeManager>()!.Also(it => {
        Trace.WriteLine($"Renaming {oldItemName} of {volumeName} to {newItemName}");

        it.GetVolumeByName(volumeName).EnsureNotNull(vol => vol.Rename(oldItemName, newItemName));
    });
}

[WebCallResponder("GetFullFileModel")]
public class GetFullFileModelResponder(string volumeName, int fileId) : IWebCallResponder {
    public WebResponse Respond(KurisuAppContext context) {
        var volumeManager = context.Inject<LocalVolumeManager>()!;
        var volume = volumeManager.GetVolumeByName(volumeName);

        if (volume is null) return WebResponse.Failure(EditorErrorCode.VolumeNotFound, "LocalVolume not found", volumeName);

        return WebResponse.Success(volume[fileId]);
    }
}