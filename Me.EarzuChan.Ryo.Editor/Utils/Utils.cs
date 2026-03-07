using System.Collections.Concurrent;
using System.Diagnostics;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Extensions.MassExtensions;
using Me.EarzuChan.Ryo.Kurisu;
#if WINDOWS
using Microsoft.Win32;
#endif

namespace Me.EarzuChan.Ryo.Editor.Utils;

public static class MiscUtils
{
    public static string? OpenFileByDialog(string fileDescription, string fileExtension)
    {
#if WINDOWS
        var openFileDialog = new OpenFileDialog
        {
            Filter = $"{fileDescription} (*.{fileExtension})|*.{fileExtension}",
            Title = $"打开{fileDescription}"
        };

        return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
#else
        Trace.WriteLine($"OpenFileDialog is not supported on this platform. desc={fileDescription}, ext={fileExtension}");
        return null;
#endif
    }

    public static string? SaveFileByDialog(string fileDescription, string fileExtension)
    {
#if WINDOWS
        var saveFileDialog = new SaveFileDialog
        {
            Filter = $"{fileDescription} (*.{fileExtension})|*.{fileExtension}",
            Title = $"保存{fileDescription}"
        };

        return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;
#else
        Trace.WriteLine($"SaveFileDialog is not supported on this platform. desc={fileDescription}, ext={fileExtension}");
        return null;
#endif
    }

    public static void EmitOpenedVolumes(KurisuAppContext context, Dictionary<LocalVolume, LocalVolumeMetaData> dic)
    {
        var openedMasses =
            dic.Select(pair => new
            {
                name = pair.Key.VolumeName,
                revision = pair.Key.Revision,
                items = pair.Key.IdStrPairs.Select(item =>
                    new
                    {
                        id = item.Value,
                        name = item.Key,
                    })
            });

        context.EmitWebEvent(new ("OpenedVolumesChanged", openedMasses));
    }
}
