using Me.EarzuChan.Ryo.Core.Adaptations;
using Me.EarzuChan.Ryo.Core.Adaptations.AdapterFactories;
using Me.EarzuChan.Ryo.Core.Exceptions.AdaptationExceptions;
using Me.EarzuChan.Ryo.Core.Formations.HelperFormations;
using Me.EarzuChan.Ryo.Exceptions;
using Me.EarzuChan.Ryo.Utils;
using System.Reflection;
using System.Text;

namespace Me.EarzuChan.Ryo.Core.Utils
{
    public static class AdaptationUtils
    {
        public static HashSet<RyoType> RyoTypes = new();
        public static HashSet<RyoType> BasicRyoTypes = new();
        private static bool hasRyoTypesRegistered = false;

        // 静态构造函数
        static AdaptationUtils() => RegDefaultRyoTypes();

        // 注册默认Ryo类型
        private static void RegDefaultRyoTypes()
        {
            // 是否已注册
            if (hasRyoTypesRegistered) throw new RyoException("已经注册过内置Ryo类型集合");

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
            RyoTypes.Add(new() { JavaClassName = "sengine.graphics2d.texturefile.FIFormat$FragmentedImageData", CsType = typeof(FragmentalImage) });

            // 注册工厂类
            RyoTypes.Add(new() { JavaClassName = "sengine.mass.serializers.DefaultSerializers$*", CsType = typeof(NormalTypeAdapterFactory) });
            RyoTypes.Add(new() { JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$*", CsType = typeof(BaseArrayTypeAdapterFactory) });
            RyoTypes.Add(new() { JavaClassName = "sengine.mass.serializers.CollectionSerializer$*", CsType = typeof(BaseArrayTypeAdapterFactory) });
            RyoTypes.Add(new() { JavaClassName = "sengine.mass.serializers.MapSerializer$*", CsType = typeof(BaseArrayTypeAdapterFactory) });
            RyoTypes.Add(new() { JavaClassName = "sengine.mass.serializers.MassSerializableSerializer$*", CsType = typeof(CustomFormatAdapterFactory) });
            RyoTypes.Add(new() { JavaClassName = "sengine.mass.serializers.FieldSerializer$*", CsType = typeof(CustomFormatAdapterFactory) });
            RyoTypes.Add(new() { JavaClassName = "sengine.graphics2d.texturefile.FIFormat$*", CsType = typeof(SpecialFormatAdapterFactory) });

            // 注册额外项目（从文件）：TODO

            // 完成
            hasRyoTypesRegistered = true;
        }

        public static Type? SearchAdaptableFormationsByJavaClass(this string clzName)
        {
            Type? baseType = null;
            var types = TypeUtils.GetAppAllTypes();
            foreach (var item in types)
            {
                var attribute = item.GetCustomAttribute<AdaptableFormationAttribute>();
                if (attribute != null)
                {
                    if (attribute.FormationName == clzName)
                    {
                        baseType = item;
                        break;
                    }
                }
            }
            return baseType;
        }

        // 列表取元素类型
        public static RyoType GetArrayElementRyoType(this RyoType ryoType)
        {
            if (ryoType.IsArray)
            {
                if (ryoType.JavaClassName != null) return ryoType.JavaClassName.JavaClassToRyoType();
                else if (ryoType.CsType != null) return ryoType.CsType.ToRyoType();
                else throw new IllegalRyoTypeException("The element Ryo Type can not be resolved without an aspect");
            }
            throw new IllegalRyoTypeException("The given Ryo Type is not an array Ryo Type");
        }

        // GetRyoByJava
        // FIXME:貌似非法输入如“[[Ljava.lang.Byte”，没有“;”，会还原出时死循环
        // 貌似解决了
        public static RyoType JavaClassToRyoType(this string clzName)
        {
            LogUtils.PrintInfo($"Resolve Ryo Type by Java Class: {clzName}");


            // 是否是列表
            bool isArray = clzName.StartsWith('[');

            // 先拨弄短名，只有是数组下才以短名示人
            string? shortName = isArray ? clzName[1..] : null;
            if (clzName.StartsWith("[[")) clzName = clzName[1..]; // 是嵌套Java类名，保留去一层的原始类名

            // 是否是基本类型
            if (isArray && !shortName!.StartsWith('['))
            {
                if (shortName.StartsWith('L') && clzName.EndsWith(';'))
                {
                    clzName = clzName[2..^1];
                    shortName = null;
                }
                else if (shortName.Length != 1) throw new RyoTypeParsingException($"Illegal Java Class input: {clzName}"); // 但是开头没有L怎么办
            }

            // 初始化
            RyoType? ryoType = null;

            // 匹配注册的类
            foreach (var item in BasicRyoTypes.Concat(RyoTypes).Where(item => shortName != null ? item.JavaShortName == shortName : item.IsJavaUniversalType ? clzName.StartsWith(item.JavaClassName![..^2]) : clzName == item.JavaClassName!))
            {
                ryoType = new RyoType { JavaShortName = item.JavaShortName, JavaClassName = item.JavaClassName, CsType = item.CsType };
                break;
            }

            // 没有就创建新的
            if (ryoType == null)
            {
                if (shortName?.Length == 1) throw new RyoTypeParsingException($"There should be no class with such a short name: {shortName}");

                // 查找可适配自定义
                Type? baseType = SearchAdaptableFormationsByJavaClass(clzName);
                bool isAdaptableCustom = baseType != null;

                bool isAdaptWithCtor = baseType != null && typeof(ICtorAdaptable).IsAssignableFrom(baseType);

                // 不是可适配自定义就没有BaseType：加个策略然后抛出以免非法RyoType
                // if (!isAdaptableCustom) throw new RyoTypeParsingException($"Unknown class, that would be a problem for us: {clzName}");
                ryoType = new() { IsAdaptableCustom = isAdaptableCustom, JavaClassName = clzName, CsType = baseType, IsAdaptWithCtor = isAdaptWithCtor };
            }
            ryoType.IsArray = isArray;

            return ryoType;
        }

        // GetJavaByRyo
        // 会有问题，影响外面的实例
        public static string? ToJavaClass(this RyoType ryoType)
        {
            LogUtils.PrintInfo($"Resolve Java Class for: {ryoType}: {ryoType.GetHashCode()}");

            var typeNamePrefix = new StringBuilder("");
            while (ryoType.IsArray)
            {
                typeNamePrefix.Append('[');
                ryoType = ryoType.GetArrayElementRyoType();
                LogUtils.PrintInfo($"Array sublevel: {ryoType}: {ryoType.GetHashCode()}");
            }

            if (ryoType.JavaClassName == null) return null;

            return typeNamePrefix.Length != 0 ? ryoType.JavaShortName != null ? typeNamePrefix.ToString() + ryoType.JavaShortName : typeNamePrefix.ToString() + 'L' + ryoType.JavaClassName + ';' : ryoType.JavaClassName;
        }

        // GetCsByRyo
        // 会有问题，影响外面的实例
        public static Type? ToCsType(this RyoType ryoType)
        {
            LogUtils.PrintInfo($"Resolve C# Type for: {ryoType}: {ryoType.GetHashCode()}");

            List<int> arrayLevelAndTypes = new();
            while (ryoType.IsArray)
            {
                arrayLevelAndTypes.Add(ryoType.IsListInternally ? 1 : 0);
                ryoType = ryoType.GetArrayElementRyoType();
                LogUtils.PrintInfo($"Array sublevel: {ryoType}: {ryoType.GetHashCode()}");
            }
            arrayLevelAndTypes.Reverse();

            if (ryoType.CsType == null) return null;
            Type baseType = ryoType.CsType;

            foreach (int arrayType in arrayLevelAndTypes)
            {
                if (arrayType == 0)
                {
                    baseType = baseType.MakeArrayType();
                }
                else
                {
                    baseType = typeof(List<>).MakeGenericType(baseType);
                }
            }

            return baseType;
        }

        // GetRyoByCs
        public static RyoType ToRyoType(this Type type)
        {
            LogUtils.PrintInfo($"Resolve Ryo Type by C# Type: {type}");

            // 列表相关
            bool isArray = type.IsArray;
            bool isList = false;
            if (isArray) type = type.GetElementType()!;
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                // List也干了
                isList = true;
                isArray = true;
                type = type.GetGenericArguments().First();
            }

            // 初始化
            RyoType? ryoType = null;

            // 匹配注册的类
            foreach (var item in BasicRyoTypes.Concat(RyoTypes).Where(item => item.CsType == type))
            {
                ryoType = new RyoType { JavaShortName = item.JavaShortName, JavaClassName = item.JavaClassName, CsType = item.CsType };
                break;
            }

            if (ryoType == null)
            {
                // 如果为可适配自定义，那么就有attr_format_name，就不用if 然后检测字段还是构造器
                var attr = type.GetCustomAttribute<AdaptableFormationAttribute>();
                var clzName = attr?.FormationName;
                bool isAdaptWithCtor = clzName != null && typeof(ICtorAdaptable).IsAssignableFrom(type);
                //clzName ??= type.Name; 
                // Java不友好也不行
                // if (attr == null) throw new RyoTypeParsingException($"Unknown type, that would be a problem for us: {type}");

                // 没有就创建新的
                ryoType = new() { IsAdaptableCustom = attr != null, IsAdaptWithCtor = isAdaptWithCtor, JavaClassName = clzName, CsType = type };
            }
            ryoType.IsArray = isArray;
            ryoType.IsListInternally = isList;

            return ryoType;
        }

        // CreateAdapter（时机）
        public static IAdapter CreateAdapter(RyoType adapterFactoryRyoType, RyoType dataRyoType)
        {
            var factoryType = ToCsType(adapterFactoryRyoType);

            if (typeof(IAdapterFactory).IsAssignableFrom(factoryType))
                return ((IAdapterFactory)Activator.CreateInstance(factoryType)!).CreateAdapterForDataRyoType(dataRyoType);
            else throw new InvalidCastException($"{adapterFactoryRyoType}不是一个适配器工厂类");
        }

        public static RyoType? DataRyoTypeFindAdapterRyoType(this RyoType ryoType)
        {
            RyoType? result = null;
            var types = TypeUtils.GetAppAllTypes();
            foreach (var item in types)
            {
                if (typeof(IAdapterFactory).IsAssignableFrom(item))
                {
                    try
                    {
                        result = ((IAdapterFactory)Activator.CreateInstance(item)!).FindAdapterRyoTypeForDataRyoType(ryoType);
                    }
                    catch (Exception) { }

                    if (result != null) break;
                }
            }
            return result;
        }
    }
}
