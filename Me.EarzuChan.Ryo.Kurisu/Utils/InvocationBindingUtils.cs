using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Me.EarzuChan.Ryo.Kurisu.Utils;

public static class InvocationBindingUtils {
    public static bool TryCreateInstance<TContract>(Type implementationType, object[] rawArgs, out TContract? instance, out string? error) where TContract : class {
        instance = null;
        error = null;

        var constructors = implementationType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        if (constructors.Length == 0) {
            error = $"类型 {implementationType.FullName} 没有可用的 public 构造函数";
            return false;
        }

        var sameArgCountConstructors = constructors.Where(it => it.GetParameters().Length == rawArgs.Length).ToArray();
        if (sameArgCountConstructors.Length == 0) {
            error = $"类型 {implementationType.FullName} 没有参数数量为 {rawArgs.Length} 的构造函数";
            return false;
        }

        string? lastError = null;
        foreach (var constructor in sameArgCountConstructors) {
            var parameters = constructor.GetParameters();
            var convertedArgs = new object?[parameters.Length];
            var failed = false;

            for (var i = 0; i < parameters.Length; i++) {
                if (TryConvertArgument(rawArgs[i], parameters[i].ParameterType, out var converted, out var convertError)) {
                    convertedArgs[i] = converted;
                    continue;
                }

                failed = true;
                lastError =
                    $"参数绑定失败：{implementationType.Name}.{constructor.Name} 第{i + 1}个参数 `{parameters[i].Name}` 需要 `{parameters[i].ParameterType.Name}`，但收到 `{rawArgs[i]?.GetType().Name ?? "null"}`。原因：{convertError}";
               
                break;
            }

            if (failed) continue;

            try {
                instance = constructor.Invoke(convertedArgs) as TContract;
                if (instance == null) {
                    lastError = $"类型 {implementationType.FullName} 并未实现 {typeof(TContract).FullName}";
                    continue;
                }

                error = null;
                return true;
            } catch (Exception e) {
                lastError = $"调用构造函数失败：{implementationType.FullName}，{e.Message}";
            }
        }

        error = lastError ?? $"无法实例化 {implementationType.FullName}";
        return false;
    }

    public static bool TryConvertArgument(object? rawValue, Type targetType, out object? convertedValue,
        out string? error) {
        var nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
        var effectiveTargetType = nullableUnderlyingType ?? targetType;
        var canBeNull = !effectiveTargetType.IsValueType || nullableUnderlyingType != null;

        switch (rawValue) {
            case null when canBeNull:
                convertedValue = null;
                error = null;
                return true;
            
            case null:
                convertedValue = null;
                error = "目标类型不允许 null";
                return false;
            
            case JToken token: return TryConvertFromJToken(token, targetType, out convertedValue, out error);
        }

        if (effectiveTargetType.IsInstanceOfType(rawValue)) {
            convertedValue = rawValue;
            error = null;
            return true;
        }

        if (effectiveTargetType.IsEnum) return TryConvertToEnum(rawValue, effectiveTargetType, out convertedValue, out error);

        if (TryConvertByBuiltinParser(rawValue, effectiveTargetType, out convertedValue)) {
            error = null;
            return true;
        }

        try {
            convertedValue = Convert.ChangeType(rawValue, effectiveTargetType, CultureInfo.InvariantCulture);
            error = null;
            return true;
        } catch (Exception) {
            // ignored
        }

        try {
            var serialized = JsonConvert.SerializeObject(rawValue);
            convertedValue = JsonConvert.DeserializeObject(serialized, effectiveTargetType);
            if (convertedValue == null && effectiveTargetType.IsValueType && nullableUnderlyingType == null) {
                error = "反序列化得到 null，目标类型不允许 null";
                return false;
            }

            error = null;
            return true;
        } catch (Exception e) {
            convertedValue = null;
            error = e.Message;
            return false;
        }
    }

    private static bool TryConvertFromJToken(JToken token, Type targetType, out object? convertedValue,
        out string? error) {
        var nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
        var effectiveTargetType = nullableUnderlyingType ?? targetType;
        var canBeNull = !effectiveTargetType.IsValueType || nullableUnderlyingType != null;

        if (token.Type is JTokenType.Null or JTokenType.Undefined) {
            if (canBeNull) {
                convertedValue = null;
                error = null;
                return true;
            }

            convertedValue = null;
            error = "目标类型不允许 null";
            return false;
        }

        if (effectiveTargetType == typeof(object)) {
            convertedValue = token.ToObject<object>();
            error = null;
            return true;
        }

        if (effectiveTargetType.IsEnum)
            return TryConvertToEnum(token, effectiveTargetType, out convertedValue, out error);

        try {
            convertedValue = token.ToObject(effectiveTargetType);
            if (convertedValue == null && effectiveTargetType.IsValueType && nullableUnderlyingType == null) {
                error = "反序列化得到 null，目标类型不允许 null";
                return false;
            }

            error = null;
            return true;
        } catch (Exception e) {
            convertedValue = null;
            error = e.Message;
            return false;
        }
    }

    private static bool TryConvertToEnum(object rawValue, Type enumType, out object? convertedValue, out string? error) {
        if (rawValue is JToken token) {
            if (token.Type == JTokenType.String) {
                var enumText = token.Value<string>() ?? string.Empty;
                return TryConvertEnumFromString(enumText, enumType, out convertedValue, out error);
            }

            if (token is JValue jValue)
                rawValue = jValue.Value!;
        }

        if (rawValue is string enumString) return TryConvertEnumFromString(enumString, enumType, out convertedValue, out error);

        try {
            var underlyingType = Enum.GetUnderlyingType(enumType);
            var enumNumericValue = Convert.ChangeType(rawValue, underlyingType, CultureInfo.InvariantCulture);
            convertedValue = Enum.ToObject(enumType, enumNumericValue!);
            error = null;
            return true;
        } catch (Exception e) {
            convertedValue = null;
            error = e.Message;
            return false;
        }
    }

    private static bool TryConvertEnumFromString(string enumString, Type enumType, out object? convertedValue,
        out string? error) {
        if (Enum.TryParse(enumType, enumString, true, out convertedValue)) {
            error = null;
            return true;
        }

        convertedValue = null;
        error = $"无法把 `{enumString}` 解析为 `{enumType.Name}`";
        return false;
    }

    private static bool TryConvertByBuiltinParser(object rawValue, Type targetType, out object? convertedValue) {
        convertedValue = null;

        if (targetType == typeof(Guid) && rawValue is string guidString &&
            Guid.TryParse(guidString, out var parsedGuid)) {
            convertedValue = parsedGuid;
            return true;
        }

        if (targetType != typeof(Uri) || rawValue is not string uriString || !Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out var parsedUri)) return false;
        
        convertedValue = parsedUri;
        return true;

    }
}