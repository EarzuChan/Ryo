using System.Collections.Concurrent;
using Me.EarzuChan.Ryo.Core.Codecations;
using Me.EarzuChan.Ryo.Core.Codecations.CodecFactories;
using Me.EarzuChan.Ryo.Core.Exceptions.CodecationExceptions;
using Me.EarzuChan.Ryo.Core.Formations.HelperFormations;
using Me.EarzuChan.Ryo.Exceptions;
using Me.EarzuChan.Ryo.Utils;
using System.Reflection;
using System.Text;

namespace Me.EarzuChan.Ryo.Core.Utils;

public static class CodecationUtils {
    public static readonly HashSet<RyoType> RyoTypes = [];
    public static readonly HashSet<RyoType> BasicRyoTypes = [];
    private static bool _hasRyoTypesRegistered;

    private static readonly ConcurrentDictionary<string, RyoType> JavaClassToRyoTypeCache = new();
    private static readonly ConcurrentDictionary<Type, RyoType> CsTypeToRyoTypeCache = new();

    // 静态构造函数
    static CodecationUtils() => RegDefaultRyoTypes();

    // 注册默认Ryo类型
    private static void RegDefaultRyoTypes() {
        // 是否已注册
        if (_hasRyoTypesRegistered) throw new RyoException("已经注册过内置Ryo类型集合");

        // 注册基本类型
        BasicRyoTypes.Add(new() { JavaShortName = "Z", JavaClassName = "java.lang.Boolean", CsType = typeof(bool) });
        BasicRyoTypes.Add(new() { JavaShortName = "B", JavaClassName = "java.lang.Byte", CsType = typeof(byte) });
        BasicRyoTypes.Add(new() { JavaShortName = "C", JavaClassName = "java.lang.Character", CsType = typeof(char) });
        BasicRyoTypes.Add(new() { JavaShortName = "D", JavaClassName = "java.lang.Double", CsType = typeof(double) });
        BasicRyoTypes.Add(new() { JavaShortName = "F", JavaClassName = "java.lang.Float", CsType = typeof(float) });
        BasicRyoTypes.Add(new() { JavaShortName = "I", JavaClassName = "java.lang.Integer", CsType = typeof(int) });
        BasicRyoTypes.Add(new() { JavaShortName = "J", JavaClassName = "java.lang.Long", CsType = typeof(long) });
        BasicRyoTypes.Add(new() { JavaShortName = "S", JavaClassName = "java.lang.Short", CsType = typeof(short) });
        BasicRyoTypes.Add(new() { JavaShortName = "V", JavaClassName = "java.lang.Void", CsType = typeof(void) });
        BasicRyoTypes.Add(new() { JavaClassName = "java.lang.String", CsType = typeof(string) });
        BasicRyoTypes.Add(new() { JavaClassName = "java.lang.Object", CsType = typeof(object) });

        // 特定类（图片等）
        RyoTypes.Add(new() {
            JavaClassName = "sengine.graphics2d.texturefile.FIFormat$FragmentedImageData",
            CsType = typeof(FragmentalImage)
        });

        // 注册工厂类
        RyoTypes.Add(new() {
            JavaClassName = "sengine.mass.serializers.DefaultSerializers$*", CsType = typeof(NormalTypeCodecFactory)
        });
        RyoTypes.Add(new() {
            JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$*",
            CsType = typeof(BaseArrayTypeCodecFactory)
        });
        RyoTypes.Add(new() {
            JavaClassName = "sengine.mass.serializers.CollectionSerializer$*",
            CsType = typeof(BaseArrayTypeCodecFactory)
        });
        RyoTypes.Add(new() {
            JavaClassName = "sengine.mass.serializers.MapSerializer$*", CsType = typeof(BaseArrayTypeCodecFactory)
        });
        RyoTypes.Add(new() {
            JavaClassName = "sengine.mass.serializers.MassSerializableSerializer$*", CsType = typeof(CustomFormatCodecFactory)
        });
        RyoTypes.Add(new() {
            JavaClassName = "sengine.mass.serializers.FieldSerializer$*", CsType = typeof(CustomFormatCodecFactory)
        });
        RyoTypes.Add(new() {
            JavaClassName = "sengine.graphics2d.texturefile.FIFormat$*", CsType = typeof(SpecialFormatCodecFactory)
        });

        // 注册额外项目（从文件或者什么别的方式？），未来可想想看：TODO

        // 完成
        _hasRyoTypesRegistered = true;
    }

    public static Type? SearchCodecableFormationsByJavaClass(this string? clzName) {
        Type? baseType = null;
        var types = TypeUtils.GetAppAllTypes();
        foreach (var item in types) {
            var attribute = item.GetCustomAttribute<CodecableFormationAttribute>();
            if (attribute == null || attribute.FormationName != clzName) continue;

            baseType = item;
            break;
        }

        return baseType;
    }

    // 列表取元素类型
    public static RyoType GetArrayElementRyoType(this RyoType ryoType) {
        if (!ryoType.IsArray) throw new IllegalRyoTypeException("The given Ryo Type is not an array Ryo Type");

        if (ryoType.JavaClassName != null) return ryoType.JavaClassName.JavaClassToRyoType();
        return ryoType.CsType != null ? ryoType.CsType.ToRyoType() : throw new IllegalRyoTypeException("The element Ryo Type can not be resolved without an aspect");
    }

    // 其实可以不要，
    public static RyoType MakeArrayRyoType(this RyoType oldRyoType) {
        LogUtils.PrintInfo($"Make Array Ryo Type from element: {oldRyoType}");

        var oldWasArray = oldRyoType.IsArray;

        // 处理 JavaClassName
        string? newJavaClassName = null;
        if (oldRyoType.JavaClassName != null) newJavaClassName = oldWasArray ? oldRyoType.ToJavaClass() : oldRyoType.JavaClassName; // 是数组类型？取原完整版：不需要处理

        // 处理 CsType
        Type? newCsType = null;
        if (oldRyoType.CsType != null) newCsType = oldWasArray ? oldRyoType.CsType?.MakeArrayType() : oldRyoType.CsType; // 是数组类型？包装：不需要处理

        RyoType arrayRyoType = new() {
            JavaClassName = newJavaClassName,
            JavaShortName = oldWasArray ? null : oldRyoType.JavaShortName,

            CsType = newCsType,

            // CHECK：按理，下面这个多维不继承：只对本级负责
            IsCodecableCustom = !oldWasArray && oldRyoType.IsCodecableCustom,
            IsCodecateWithCtor = !oldWasArray && oldRyoType.IsCodecateWithCtor,

            IsArray = true,
            IsListInternally = false // CHECK：新建的数组类型默认不是 List
        };

        LogUtils.PrintInfo($"Created Array Ryo Type: {arrayRyoType}");
        return arrayRyoType;
    }

    // GetRyoByJa va
    // 有新写法，忘了
    public static RyoType JavaClassToRyoType(this string? clzName) {
        if (JavaClassToRyoTypeCache.TryGetValue(clzName, out var cachedRyoType)) {
            LogUtils.PrintInfo($"Already have {cachedRyoType} for Java: {clzName}");
            return cachedRyoType;
        }

        LogUtils.PrintInfo($"Resolve Ryo Type by Java Class: {clzName}");
        var rawClzName = clzName;

        // 是否是列表
        var isArray = clzName.StartsWith('[');

        // 先拨弄短名，只有是数组下才以短名示人
        var shortName = isArray ? clzName[1..] : null;
        if (clzName.StartsWith("[[")) clzName = clzName[1..]; // 是嵌套Java类名，保留去一层的原始类名

        // 是否是基本类型
        if (isArray && !shortName!.StartsWith('[')) {
            if (shortName.StartsWith('L') && clzName.EndsWith(';')) {
                clzName = clzName[2..^1];
                shortName = null;
            } else if (shortName.Length != 1) throw new RyoTypeParsingException($"Illegal Java Class input: {clzName}"); // 但是开头没有L怎么办
        }

        // 初始化
        var ryoType = BasicRyoTypes.Concat(RyoTypes)
            .Where(item => shortName != null ? item.JavaShortName == shortName : item.IsJavaUniversalType ? clzName.StartsWith(item.JavaClassName![..^2]) : clzName == item.JavaClassName!)
            .Select(item => new RyoType { JavaShortName = item.JavaShortName, JavaClassName = item.JavaClassName, CsType = item.CsType })
            .FirstOrDefault();

        // 匹配注册的类

        // 没有就创建新的
        if (ryoType == null) {
            if (shortName?.Length == 1)
                throw new RyoTypeParsingException($"There should be no class with such a short name: {shortName}");

            // 查找可适配自定义
            var baseType = SearchCodecableFormationsByJavaClass(clzName);
            var isCodecableCustom = baseType != null;
            var isCodecateWithCtor = baseType != null && typeof(ICtorCodecable).IsAssignableFrom(baseType);

            // 不是可适配自定义就没有BaseType：加个策略然后抛出以免非法RyoType
            ryoType = new() {
                IsCodecableCustom = isCodecableCustom, JavaClassName = clzName, CsType = baseType,
                IsCodecateWithCtor = isCodecateWithCtor
            };
        }

        ryoType.IsArray = isArray;

        JavaClassToRyoTypeCache[rawClzName] = ryoType;
        LogUtils.PrintInfo($"Cache {ryoType} by Java: {rawClzName}");

        return ryoType;
    }

    // GetJavaByRyo和GetCsByRyo
    extension(RyoType ryoType) {
        public string? ToJavaClass() {
            LogUtils.PrintInfo($"Resolve Java Class for: {ryoType}");

            var typeNamePrefix = new StringBuilder("");
            while (ryoType.IsArray) {
                typeNamePrefix.Append('[');
                ryoType = ryoType.GetArrayElementRyoType();
                LogUtils.PrintInfo($"Array sublevel: {ryoType}: {ryoType.GetHashCode()}");
            }

            if (ryoType.JavaClassName == null) return null;

            return typeNamePrefix.Length != 0
                ? ryoType.JavaShortName != null
                    ? typeNamePrefix + ryoType.JavaShortName : typeNamePrefix.ToString() + 'L' + ryoType.JavaClassName + ';'
                : ryoType.JavaClassName;
        }

        public Type? ToCsType() {
            LogUtils.PrintInfo($"Resolve C# Type for: {ryoType}");

            List<int> arrayLevelAndTypes = [];
            while (ryoType.IsArray) {
                arrayLevelAndTypes.Add(ryoType.IsListInternally ? 1 : 0);
                ryoType = ryoType.GetArrayElementRyoType();
                LogUtils.PrintInfo($"Array sublevel: {ryoType}: {ryoType.GetHashCode()}");
            }

            arrayLevelAndTypes.Reverse();

            return ryoType.CsType == null
                ? null
                : arrayLevelAndTypes.Aggregate(ryoType.CsType, (current, arrayType) => arrayType == 0 ? current.MakeArrayType() : typeof(List<>).MakeGenericType(current));
        }

        public RyoType? DataRyoTypeFindCodecRyoType() {
            RyoType? result = null;
            var types = TypeUtils.GetAppAllTypes();
            foreach (var item in types) {
                if (!typeof(ICodecFactory).IsAssignableFrom(item)) continue;

                try {
                    result = ((ICodecFactory)Activator.CreateInstance(item)!).FindCodecRyoTypeForDataRyoType(ryoType);
                } catch {
                    // HACK：暂不理
                }

                if (result != null) break;
            }

            return result;
        }
    }

    // GetRyoByCs
    public static RyoType ToRyoType(this Type type) {
        if (CsTypeToRyoTypeCache.TryGetValue(type, out var cachedRyoType)) {
            LogUtils.PrintInfo($"Already have {cachedRyoType} for C#: {type}");
            return cachedRyoType;
        }

        LogUtils.PrintInfo($"Resolve Ryo Type by C# Type: {type}");
        var rawType = type;

        // 列表相关
        var isArray = type.IsArray;
        var isList = false;
        if (isArray) type = type.GetElementType()!;
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
            // List也干了
            isList = true;
            isArray = true;
            type = type.GetGenericArguments().First();
        }

        // 初始化
        var ryoType = BasicRyoTypes.Concat(RyoTypes).Where(item => item.CsType == type).Select(item => new RyoType { JavaShortName = item.JavaShortName, JavaClassName = item.JavaClassName, CsType = item.CsType }).FirstOrDefault();

        // 匹配注册的类

        if (ryoType == null) {
            // 如果为可适配自定义，那么就有attr_format_name，就不用if 然后检测字段还是构造器
            var attr = type.GetCustomAttribute<CodecableFormationAttribute>();
            var clzName = attr?.FormationName;
            bool isCodecateCodCoWithCtor = clzName != null && typeof(ICtorCodecable).IsAssignableFrom(type);
            //clzName ??= type.Name; 
            // Java不友好也不行
            // if (attr == null) throw new RyoTypeParsingException($"Unknown type, that would be a problem for us: {type}");

            // 没有就创建新的
            ryoType = new() {
                IsCodecableCustom = attr != null, IsCodecateWithCtor = isCodecateCodCoWithCtor, JavaClassName = clzName,
                CsType = type
            };
        }

        ryoType.IsArray = isArray;
        ryoType.IsListInternally = isList;

        CsTypeToRyoTypeCache[rawType] = ryoType;
        LogUtils.PrintInfo($"Cache {ryoType} for C#: {rawType}");
        return ryoType;
    }

    // CreateCodec（时机）
    public static ICodec CreateCodec(RyoType codecRyoType, RyoType dataRyoType) {
        var factoryType = ToCsType(codecRyoType);

        return typeof(ICodecFactory).IsAssignableFrom(factoryType) ? ((ICodecFactory)Activator.CreateInstance(factoryType)!).CreateCodecForDataRyoType(dataRyoType) : throw new InvalidCastException($"{codecRyoType}不是一个编解码器工厂类");
    }
}