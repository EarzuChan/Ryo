using Me.EarzuChan.Ryo.Core.IO;
using Me.EarzuChan.Ryo.Core.Adaptations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using Me.EarzuChan.Ryo.Core.Formations.DataFormations.PipeDream;
using Me.EarzuChan.Ryo.Exceptions;
using System.Reflection;
using Me.EarzuChan.Ryo.Utils;
using Newtonsoft.Json.Serialization;

namespace Me.EarzuChan.Ryo.Extensions.Utils
{
    public static class SerializationUtils
    {
        private static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.None, // suan处理类名：Auto时如给哺乳动物类字段分配狗的实例时标注一个元数据 而哥们有类型系统，这这不采用
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            // A类的成员a类型是A时采取的操作
            // PreserveReferencesHandling = PreserveReferencesHandling.All,
            // TODO: Null管不管
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new JsonConverter[] { new ExpandoObjectConverter() }
        };

        [Obsolete("老哥不建议使用这个")]
        public static string ToJsonWithInternalAlgorithm(this object item, string typeName = "无类名")
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
                    strList.Add(ToJsonWithInternalAlgorithm(elem));
                }
                return $"{{\"{type.GetElementType()?.Name}数组:{strList.Count}\":[{string.Join(',', strList)}]}}";

            }
            else if (typeof(ICtorAdaptable).IsAssignableFrom(type))
                return ToJsonWithInternalAlgorithm(((ICtorAdaptable)item).GetAdaptedArray(), type.Name);
            else
            {
                string? str = item.ToString()!.Replace("\n", "\\n").Replace("\"", "\\\"");
                return '"' + (str ?? "项目无内置转换，且强制转换结果为Null") + '"';
            }
        }

        public static string ToJson(this object item) => JsonConvert.SerializeObject(item, DefaultJsonSerializerSettings);

        public static object? JsonToObject<T>(this string json) => JsonConvert.DeserializeObject<T>(json); // 要不要设置？
    }
}