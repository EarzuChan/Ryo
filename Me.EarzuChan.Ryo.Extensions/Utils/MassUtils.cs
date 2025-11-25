using Me.EarzuChan.Ryo.Core.Masses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Extensions.Utils;

public static class MassUtils
{
    public static string GetInfo(this MassFile mass)
    {
        var itemBlobCount = mass.ItemBlobs.Count;
        StringBuilder info = new();
        info.AppendLine($"总项目数：{itemBlobCount} 主项目数：{mass.IdStrPairs.Count}\n\n项目数据：");

        for (var i = 0; i < itemBlobCount; i++)
        {
            var itemBlob = mass.ItemBlobs[i];
            info.AppendLine(
                $"-- Id.{i}：编解码绑定ID：{itemBlob.CodecBindingId}、长度：{itemBlob.Data?.Length ?? -1}、粘连数目：{itemBlob.MetaHeapCount}、粘连偏移：{itemBlob.MetaHeapOffset}");
        }

        var stickyMetaDataCount = mass.MetaHeap.Count;
        info.AppendLine($"\n粘连元数据数：{stickyMetaDataCount}\n\n粘连元数据：");

        var currentIndex = 0;
        while (currentIndex < stickyMetaDataCount)
        {
            StringBuilder str = new();
            for (var i = 0; i < 4 && currentIndex < stickyMetaDataCount; i++)
            {
                var refTo = mass.MetaHeap[currentIndex] >> 2;
                var metaMode = mass.MetaHeap[currentIndex] & 3;
                str.Append($"No.{currentIndex}：{refTo} Mode：{metaMode}  ");
                currentIndex++;
            }

            info.AppendLine(str.ToString());
        }

        var codecBindingCount = mass.CodecBindings.Count;
        info.AppendLine($"\n编解码绑定数：{codecBindingCount}");
        for (var i = 0; i < codecBindingCount; i++)
        {
            var item = mass.CodecBindings[i];
            info.AppendLine($"-- Id.{i} {item.DataJavaClz} 适配器：{item.CodecJavaClz}");
        }

        info.AppendLine($"\n正式数据项数：{mass.IdStrPairs.Count}");
        foreach (var item in mass.IdStrPairs) info.AppendLine($"-- Id.{item.Value} Name：{item.Key}");

        return info.ToString();
    }
}