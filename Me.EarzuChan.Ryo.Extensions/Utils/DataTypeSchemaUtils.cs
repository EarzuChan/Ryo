using Me.EarzuChan.Ryo.Core.Adaptations;
using Me.EarzuChan.Ryo.Core.Utils;
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
        public static string ResolveDataTypeName(this RyoType ryoType)
        {
            LogUtils.PrintInfo($"Resolve Data Type Name for: {ryoType}");

            var typeNameSuffix = new StringBuilder();
            while (ryoType.IsArray)
            {
                typeNameSuffix.Append("[]");
                ryoType = ryoType.GetArrayElementRyoType();
                LogUtils.PrintInfo($"Array sublevel: {ryoType}");
            }

            // if (ryoType.JavaClassName == null) throw new DataTypeSchemaParsingException($"When resolving Data Type Name, a Ryo Type without Java Class Name was found: {ryoType}");

            return ryoType.JavaClassName + typeNameSuffix.ToString();
        }

        public static object GetDataTypeSchema(this RyoType ryoType)
        {
            if (ryoType.IsArray) throw new DataTypeSchemaParsingException($"Data Type Schema should not be generated for Array Type: {ryoType}");

            var csType = ryoType.ToCsType();
            if (csType == null) LogUtils.PrintWarning($"When getting Data Type Schema, a Ryo Type without C# Type was found: {ryoType}");

            var type = ryoType.ResolveDataTypeName();

            if (ryoType.IsJvmBasicType || csType == null || csType.GetFields().Length == 0 || ryoType.IsArray) return new { type };
            else
            {
                var members = new List<Dictionary<string, string>>();

                foreach (var field in csType.GetFields())
                {
                    members.Add(new Dictionary<string, string>
                {
                    { "name", field.Name.MakeFirstCharLower() },
                    { "type", field.FieldType.ToRyoType().ResolveDataTypeName() }
                });
                }

                return new
                {
                    type,
                    members
                };
            }
        }

        public static object GetAllDataTypeSchemas()
        {
            // 第一步：遍历源集合，调用GetDataTypeSchema方法
            var sourceSchemas = AdaptationUtils.BasicRyoTypes.Select(ryoType => ryoType.GetDataTypeSchema()).ToArray();

            // 第二步：遍历当前程序集中所有带有AdaptableFormat注解的类，调用GetDataTypeSchema方法
            var adaptableSchemas = TypeUtils.GetAppAllTypes().Where(type => type.GetCustomAttributes<AdaptableFormationAttribute>().Any())
                                          .Select(type => type.ToRyoType().GetDataTypeSchema()).ToArray();

            // 第三步：合并两个列表
            return sourceSchemas.Concat(adaptableSchemas);
        }
    }
}