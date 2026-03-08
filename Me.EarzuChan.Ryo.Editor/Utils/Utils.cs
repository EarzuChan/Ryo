using System.Net;
using Me.EarzuChan.Ryo.Editor.Exceptions;
using Me.EarzuChan.Ryo.Extensions.MassExtensions;
using Me.EarzuChan.Ryo.Kurisu;

namespace Me.EarzuChan.Ryo.Editor.Utils;

public static class MiscUtils {
    public static void EmitOpenedVolumes(KurisuAppContext context, Dictionary<LocalVolume, LocalVolumeMetaData> dic) {
        var openedMasses =
            dic.Select(pair => new {
                name = pair.Key.VolumeName,
                revision = pair.Key.Revision,
                items = pair.Key.IdStrPairs.Select(item => new {
                    id = item.Value,
                    name = item.Key,
                })
            });

        context.EmitWebEvent(new("OpenedVolumesChanged", openedMasses));
    }
}