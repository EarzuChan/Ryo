using Me.EarzuChan.Ryo.Core.Masses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Exceptions.Utils
{
    public static class MassUtils
    {
        public static string GetInfo(this MassFile mass)
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
    }
}
