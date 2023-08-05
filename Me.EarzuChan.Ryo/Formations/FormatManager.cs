using Me.EarzuChan.Ryo.Adaptions;
using Me.EarzuChan.Ryo.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Me.EarzuChan.Ryo.Formations
{
    public class FormatManager
    {
        public static FormatManager INSTANCE { get { instance ??= new(); return instance; } }
        private static FormatManager? instance;

        public string OldItemToString(object item, string typeName = "无类名")
        {
            // 我希望写出变量名

            Type type = item.GetType();
            if (item == null) return "项目为Null";
            else if (type == typeof(int[]))
            {
                int[] iArr = (int[])item;
                return $"{{\"整数数组:{iArr.Length}\":[{string.Join(',', iArr.Select(i => i.ToString()))}]}}";
            }
            else if (type == typeof(byte[]))
            {
                var it = (RyoReader)(byte[])item;
                return $"{{\"字节数组:{it.Length}\":\"{it.ReadBytesToHexString((int)it.Length)}\"}}";
            }
            else if (type.IsArray)
            {
                var strList = new List<string>();
                foreach (var elem in (Array)item)
                {
                    strList.Add(OldItemToString(elem));
                }
                return $"{{\"{type.GetElementType()?.Name}数组:{strList.Count}\":[{string.Join(',', strList)}]}}";

            }
            else if (typeof(ICtorAdaptable).IsAssignableFrom(type))
            {
                return OldItemToString(((ICtorAdaptable)item).GetAdaptedArray(), type.Name);
            }
            else
            {
                string? str = item.ToString()!.Replace("\n", "\\n").Replace("\"", "\\\"");
                return '"' + (str ?? "项目无内置转换，且强制转换结果为Null") + '"';
            }
        }

        public string ItemToString(object item) => JsonConvert.SerializeObject(item, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            //PreserveReferencesHandling = PreserveReferencesHandling.All,
            Converters = new JsonConverter[] { new ExpandoObjectConverter() }
        });//JsonSerializer.Serialize(item, item.GetType());
    }
}