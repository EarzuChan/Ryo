using Me.EarzuChan.Ryo.Core.IO;
using Me.EarzuChan.Ryo.Core.Adaptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using Me.EarzuChan.Ryo.Core.Formations.PipeDream;
using Me.EarzuChan.Ryo.Exceptions;

namespace Me.EarzuChan.Ryo.Extensions.Utils
{
    public static class FormatUtils
    {
        // 有参不行，日嫩毛
        public static string SystemItemToXml(object item)
        {
            using StringWriter stringWriter = new();
            XmlSerializer serializer = new(item.GetType());
            serializer.Serialize(stringWriter, item);
            return stringWriter.ToString();
        }

        public static string InsidedItemToJson(object item, string typeName = "无类名")
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
                    strList.Add(InsidedItemToJson(elem));
                }
                return $"{{\"{type.GetElementType()?.Name}数组:{strList.Count}\":[{string.Join(',', strList)}]}}";

            }
            else if (typeof(ICtorAdaptable).IsAssignableFrom(type))
            {
                return InsidedItemToJson(((ICtorAdaptable)item).GetAdaptedArray(), type.Name);
            }
            else
            {
                string? str = item.ToString()!.Replace("\n", "\\n").Replace("\"", "\\\"");
                return '"' + (str ?? "项目无内置转换，且强制转换结果为Null") + '"';
            }
        }

        public static string NewtonsoftItemToJson(object item) => JsonConvert.SerializeObject(item, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            //PreserveReferencesHandling = PreserveReferencesHandling.All,
            Converters = new JsonConverter[] { new ExpandoObjectConverter() }
        });

        public static object? NewtonsoftJsonToItem<T>(string json) => JsonConvert.DeserializeObject<T>(json);// ?? throw new RyoException("Can't restore object from json");
    }
}