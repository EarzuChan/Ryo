using System.Collections.Concurrent;
using System.Diagnostics;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Extensions.MassExtensions;
using Me.EarzuChan.Ryo.Kurisu;
using Microsoft.Win32;

namespace Me.EarzuChan.Ryo.Editor.Utils;

public static class MiscUtils
{
    public static string? OpenFileByDialog(string fileDescription, string fileExtension)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = $"{fileDescription} (*.{fileExtension})|*.{fileExtension}",
            Title = $"打开{fileDescription}"
        };

        return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
    }

    public static void EmitOpenedVolumes(KurisuAppContext context, Dictionary<LocalVolume, LocalVolumeMetaData> dic)
    {
        var openedMasses =
            dic.Select(pair => new
            {
                name = pair.Key.VolumeName,
                items = pair.Key.IdStrPairs.Select(item =>
                    new
                    {
                        id = item.Value,
                        name = item.Key,
                    })
            });

        context.EmitWebEvent(new
            ("OpenedVolumesChanged", openedMasses));
    }
}