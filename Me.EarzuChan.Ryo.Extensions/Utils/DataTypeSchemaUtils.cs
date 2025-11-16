using Me.EarzuChan.Ryo.Core.Adaptations;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Extensions.Exceptions.DataTypeSchemaExceptions;
using Me.EarzuChan.Ryo.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Extensions.Utils;

// 如果要支持外挂类型，是不是也通过这玩意来生成动态类型？
public static class DataTypeSchemaUtils
{
    private static readonly ConcurrentDictionary<string, RyoType> DataTypeNameToRyoTypeCache = new();
    
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

        return ryoType.JavaClassName + typeNameSuffix;
    }
    
    public static RyoType DataTypeNameResolveRyoType(this string dataTypeName)
    {
        if (DataTypeNameToRyoTypeCache.TryGetValue(dataTypeName, out var cachedRyoType))
        {
            LogUtils.PrintInfo($"Already have {cachedRyoType} for DataTypeName: {dataTypeName}");
            return cachedRyoType;
        }
        
        LogUtils.PrintInfo($"Resolve Ryo Type for Data Type Name: {dataTypeName}");
    
        // 计算数组维度
        int arrayDepth = 0;
        string baseTypeName = dataTypeName;
    
        while (baseTypeName.EndsWith("[]"))
        {
            arrayDepth++;
            baseTypeName = baseTypeName[..^2];
            LogUtils.PrintInfo($"Array depth: {arrayDepth}, base type: {baseTypeName}");
        }
    
        // 从 JavaClassName 创建基础 RyoType
        var ryoType = baseTypeName.JavaClassToRyoType();
    
        // 包装数组层级
        for (int i = 0; i < arrayDepth; i++)
        {
            ryoType = ryoType.MakeArrayRyoType(); // 假设存在这样的方法
            LogUtils.PrintInfo($"Wrapped array level {i + 1}: {ryoType}");
        }

        DataTypeNameToRyoTypeCache[dataTypeName] = ryoType;
        LogUtils.PrintInfo($"Cache {ryoType} by DataTypeName: {dataTypeName}");
    
        return ryoType;
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

    public static object[] GetAllDataTypeSchemas()
    {
        // 第一步：遍历源集合，调用GetDataTypeSchema方法
        var sourceSchemas = AdaptationUtils.BasicRyoTypes.Select(ryoType => ryoType.GetDataTypeSchema());

        // 第二步：遍历当前程序集中所有带有AdaptableFormat注解的类，调用GetDataTypeSchema方法
        var adaptableSchemas = TypeUtils.GetAppAllTypes().Where(type => type.GetCustomAttributes<AdaptableFormationAttribute>().Any())
            .Select(type => type.ToRyoType().GetDataTypeSchema());

        // 第三步：合并两个列表
        return sourceSchemas.Concat(adaptableSchemas).ToArray();
    }
}