using System.Net;
using Me.EarzuChan.Ryo.AIEE.Exceptions;
using Me.EarzuChan.Ryo.Extensions.MassExtensions;
using Me.EarzuChan.Ryo.Kurisu;

namespace Me.EarzuChan.Ryo.AIEE.Utils;

public static class MiscUtils {
    public static void EmitOpenedVolumes(KurisuAppContext context, Dictionary<LocalVolume, LocalVolumeMetaData> dic) {
        var openedMasses =
            dic.Select(pair => new {
                id = pair.Key.VolumeId,
                name = pair.Key.VolumeName,
                localPath = pair.Value.LocalPath,
                revision = pair.Key.Revision,
                unsaved = pair.Key.Unsaved,
                items = pair.Key.IdStrPairs.Select(item => new {
                    id = item.Value,
                    name = item.Key,
                    dirtyInVolume = pair.Key.IsItemUnsaved(item.Value),
                })
            });

        context.EmitWebEvent(new("OpenedVolumesChanged", openedMasses));
    }
}
