using System.Diagnostics;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Kurisu;
using Microsoft.Win32;

namespace Me.EarzuChan.Ryo.Editor.Utils
{
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

        public static void EmitOpenedMasses(KurisuAppContext context)
        {
            var massManager = context.Inject<MassManager>();
            var openedMasses =
                massManager.MassFiles.Select(pair => new
                {
                    name = pair.Key,
                    items = pair.Value.IdStrPairs.Select(item => new
                    {
                        id = item.Value,
                        name = item.Key
                    })
                });

            context.EmitWebEvent(new
                ("OpenedFilesChanged", openedMasses));
        }
    }
}