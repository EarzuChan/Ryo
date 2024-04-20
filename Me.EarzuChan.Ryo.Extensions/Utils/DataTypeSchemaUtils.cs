using Me.EarzuChan.Ryo.Core.Adaptions;
using Me.EarzuChan.Ryo.Extensions.Exceptions.DataTypeSchemaExceptions;
using Me.EarzuChan.Ryo.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Extensions.Utils
{
    // 如果要支持外挂类型，是不是也通过这玩意来生成动态类型？
    public static class DataTypeSchemaUtils
    {
        /*public enum NonDataTypeHandling
        {
            Ignore,
            Error
        }*/

        public static string ParseDataTypeName(this RyoType type/*, NonDataTypeHandling nonAdaptable = NonDataTypeHandling.Ignore*/)
        {
            // Core那边要相应修改DataType吗
            LogUtils.PrintInfo($"解析：{type}");

            var typeNameSuffix = new StringBuilder();
            while (type.IsArray)
            {
                typeNameSuffix.Append("[]");
                type = type.GetArrayElementRyoType();
                LogUtils.PrintInfo($"爷爷，下一层：{type}");
            }

            if (type.Name == null) throw new DataTypeSchemaParsingException($"{type}怎么没名呢");

            /*if (type.IsAdaptableCustom || type.IsJvmPrimitive)*/
            return type.Name + typeNameSuffix.ToString();

            /*else if (nonAdaptable == NonDataTypeHandling.Ignore) return $"{{Non Adaptable Format: {type}}}";
            else throw new TsTypeParsingException($"Type {type} is not a Adaptable Format");*/
            // Byte、Char怎么办
        }

        public static object GetDataTypeSchema(this RyoType ryoType/*, NonDataTypeHandling notParsable = NonDataTypeHandling.Ignore*/) // 获得一个一个喵
        {
            var csType = ryoType.ToType();
            if (csType == null) LogUtils.PrintWarning("获取数据类型架构时发现一个没有C#类型的Ryo类型");

            var type = ryoType.ParseDataTypeName();
            var members = new List<Dictionary<string, string>>();

            if (ryoType.IsBasicType || csType == null || ryoType.IsArray) return new { type };
            else
            {
                foreach (var field in csType.GetFields())
                {
                    members.Add(new Dictionary<string, string>
                {
                    { "name", field.Name.MakeFirstCharLower() },
                    { "type", field.FieldType.ToRyoType().ParseDataTypeName() }
                });
                }

                return new
                {
                    type,
                    members
                };
            }
        }
    }
}