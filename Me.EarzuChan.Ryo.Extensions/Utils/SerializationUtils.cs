using Me.EarzuChan.Ryo.Core.IO;
using Me.EarzuChan.Ryo.Core.Adaptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using Me.EarzuChan.Ryo.Core.Formations.PipeDream;
using Me.EarzuChan.Ryo.Exceptions;
using System.Reflection;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Extensions.Utils
{
    public static class SerializationUtils
    {
        public static readonly JsonSerializerSettings DefaultJsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            //PreserveReferencesHandling = PreserveReferencesHandling.All,
            Converters = new JsonConverter[] { new ExpandoObjectConverter() }
        };



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

        public static string NewtonsoftItemToJson(object item) => JsonConvert.SerializeObject(item, DefaultJsonSerializerSettings);

        public static object? NewtonsoftJsonToItem<T>(string json) => JsonConvert.DeserializeObject<T>(json);// ?? throw new RyoException("Can't restore object from json");

        public static string MakeAdaptableFormatStructure(Type type) // 获得一个一个喵
        {
            var name = MapToTsType(type);
            var members = new List<Dictionary<string, string>>();

            foreach (var field in type.GetFields())
            {
                members.Add(new Dictionary<string, string>
                {
                    { "name", field.Name.MakeFirstCharLower() },
                    { "type", MapToTsType(field.FieldType) }
                });
            }

            var serializedObject = new
            {
                name,
                members
            };

            return JsonConvert.SerializeObject(serializedObject);
        }

        public static string MapToTsType(Type csharpType)
        {
            // Check for custom attribute
            var adaptableFormat = csharpType.GetCustomAttribute<AdaptableFormat>();
            if (adaptableFormat != null)
            {
                return adaptableFormat.FormatName;
            }

            // Directly check the type using 'is' and pattern matching
            if (csharpType == typeof(int) || csharpType == typeof(long) ||
                csharpType == typeof(double) || csharpType == typeof(decimal) ||
                csharpType == typeof(float)) // 其他的如Short、Byte等
            {
                return "number";
            }
            else if (csharpType == typeof(string))
            {
                return "string";
            }
            else if (csharpType == typeof(bool))
            {
                return "boolean";
            }
            else
            {
                // 列表、数组等等
                // For other types or non-primitive types, return "any"
                return "未解析";
            }
        }
    }
}