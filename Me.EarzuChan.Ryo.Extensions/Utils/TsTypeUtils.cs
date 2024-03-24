using Me.EarzuChan.Ryo.Core.Adaptions;
using Me.EarzuChan.Ryo.Extensions.Exceptions.TsTypeExceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Extensions.Utils
{
    // 如果要支持外挂类型，是不是也通过这玩意来生成动态类型？
    public static class TsTypeUtils
    {
        public enum NonAdaptableFormatHandling
        {
            Ignore,
            Error
        }

        public static string ParseToTsType(this Type type, NonAdaptableFormatHandling nonAdaptable = NonAdaptableFormatHandling.Ignore)
        {
            // Check for custom attribute
            var adaptableFormat = type.GetCustomAttribute<AdaptableFormat>();
            if (adaptableFormat != null) return adaptableFormat.FormatName;

            if (type.IsTsNumberType()) return "number";
            else if (type == typeof(string)) return "string";
            else if (type == typeof(bool)) return "boolean";
            else if (type.IsTsListType()) return $"{type.GetTsListElementType().ParseToTsType()}[]"; // 包含的类型
            else if (nonAdaptable == NonAdaptableFormatHandling.Ignore) return $"{{Non Adaptable Format: {type.FullName}}}";
            else throw new TsTypeParsingException($"Type {type} is not a Adaptable Format");
        }

        public static bool IsTsNumberType(this Type type) => type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(decimal) || type == typeof(float); // TODO: What's more?

        public static bool IsTsListType(this Type type) => type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

        public static Type GetTsListElementType(this Type type)
        {
            if (!type.IsTsListType()) throw new TsTypeParsingException("Not a Ts List Type");

            if (type.IsArray) return type.GetElementType()!;
            else return type.GetGenericArguments().First();
        }
    }
}
