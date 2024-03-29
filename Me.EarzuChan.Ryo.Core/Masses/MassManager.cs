using Me.EarzuChan.Ryo.Exceptions.FileExceptions;
using Me.EarzuChan.Ryo.Core.Utils;
using System.Text;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Core.Masses
{
    public class MassManager
    {
        public readonly Dictionary<string, MassFile> MassFiles = new();

        public MassFile GetMassFileOrThrow(string fileName)
        {
            fileName = fileName.Trim().ToLower();

            var massFile = MassFiles.FirstOrDefault(item => item.Key.ToLower() == fileName).Value;

            return massFile ?? throw new NoSuchFileException(fileName);
        }

        public bool ExistsMass(string fileName) =>MassFiles.ContainsKey(fileName);

        // TODO:改写
        public MassFile LoadMassFile(string filePath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("文件名不该为空");
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException("文件路径不该为空");
            fileName = fileName.Trim().ToLower();
            filePath = filePath.Trim();

            using var fileStream = FileUtils.OpenFile(filePath);

            var massFile = new MassFile();
            try
            {
                massFile.Load(fileStream);
            }
            catch (Exception ex)
            {
                throw new FileLoadException("文件加载出错，" + ex.Message, ex);
            }
            MassFiles.Add(fileName, massFile);

            return massFile;
        }

        public void UnloadMassFile(string fileName) => MassFiles.Remove(fileName.Trim().ToLower());

        public string GetInfo(MassFile mass)
        {
            int itemBlobCount = mass.ItemBlobs.Count;
            StringBuilder info = new();
            info.AppendLine($"总项目数：{itemBlobCount} 主项目数：{mass.IdStrPairs.Count}\n\n项目数据：");

            for (var i = 0; i < itemBlobCount; i++)
            {
                var itemBlob = mass.ItemBlobs[i];
                info.AppendLine($"-- Id.{i} 适配项ID：{itemBlob.AdaptionId} 长度：{itemBlob.Data.Length} 粘连索引：{itemBlob.StickyIndex}");
            }

            int stickyMetaDataCount = mass.StickyMetaDatas.Count;
            info.AppendLine($"\n粘连元数据数：{stickyMetaDataCount}\n\n粘连元数据：");

            int currentIndex = 0;
            while (currentIndex < stickyMetaDataCount)
            {
                StringBuilder str = new();
                for (int i = 0; i < 4 && currentIndex < stickyMetaDataCount; i++)
                {
                    int refTo = mass.StickyMetaDatas[currentIndex] >> 2;
                    int metaMode = mass.StickyMetaDatas[currentIndex] & 3;
                    str.Append($"No.{currentIndex}：{refTo} Mode：{metaMode}  ");
                    currentIndex++;
                }
                info.AppendLine(str.ToString());
            }

            int itemAdaptionsCount = mass.ItemAdaptions.Count;
            info.AppendLine($"\n数据适配项数：{itemAdaptionsCount}");
            for (int i = 0; i < itemAdaptionsCount; i++)
            {
                var item = mass.ItemAdaptions[i];
                info.AppendLine($"-- Id.{i} {item.DataJavaClz} 适配器：{item.AdapterJavaClz}");
            }

            info.AppendLine($"\n正式数据项数：{mass.IdStrPairs.Count}");
            foreach (var item in mass.IdStrPairs) info.AppendLine($"-- Id.{item.Value} Name：{item.Key}");

            return info.ToString();
        }

        public void AddMassFile(MassFile newMass, string fileName)
        {
            MassFiles.Add(fileName, newMass);
        }
    }
}