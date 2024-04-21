using Me.EarzuChan.Ryo.Core.Adaptations.AdapterFactories;
using Me.EarzuChan.Ryo.Core.Exceptions.AdaptationExceptions;
using Me.EarzuChan.Ryo.Core.Formations.HelperFormations;
using Me.EarzuChan.Ryo.Core.IO;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Exceptions;
using Me.EarzuChan.Ryo.Utils;
using System.Reflection;
using System.Text;

namespace Me.EarzuChan.Ryo.Core.Adaptations
{
    public class RyoType
    {
        public string? JavaShortName; // Java短名
        public string? JavaClassName; // Java名
        public bool IsAdaptableCustom = false; // 可适配类
        public bool IsArray = false; // 是数组
        public Type? CsType; // C#基类
        public bool IsAdaptWithCtor = false; // 是否用构造器来适配

        internal bool IsListInternally = false; // 奇技淫巧

        // 属性
        public bool IsJavaPrimitiveType => JavaShortName != null; // JVM原型

        public bool IsJavaUniversalType => JavaClassName!.EndsWith("$*");

        public bool IsJvmBasicType => JavaClassName != null && JavaClassName.StartsWith("java.lang."); // JVM基本类型
        public bool IsCsTypeUnidentified => CsType == null; // 未识别
        // public bool HasSubarray => Name != null && Name.StartsWith('[') || Name == null && CsType != null && CsType.IsArray;

        public override string ToString()
        {
            // return $"[RyoType信息：{AdaptionManager.INSTANCE.GetJavaClzByType(this)}，Java短名：{ShortName}，Java名：{Name}，自定义：{IsCustom}，是列表：{IsArray}，C#类：{BaseType}]";
            return $"{{RyoType: Java: {JavaClassName}, Java Short Name: {(JavaShortName ?? "None")}, Is Jvm Primitive: {IsJavaPrimitiveType}, Is Adaptable Custom: {IsAdaptableCustom}, Is Adapted with Constructor: {IsAdaptWithCtor}, Is Array: {IsArray}( Is List Array: {IsListInternally}), C#: {CsType}}}";
        }

        internal RyoType() { }
    }

    public class AdaptationManager
    {
        public static AdaptationManager INSTANCE { get { instance ??= new(); return instance; } }
        private static AdaptationManager? instance;
        public HashSet<RyoType> RyoTypes = new();
        public HashSet<RyoType> BasicRyoTypes = new();
        private bool hasRyoTypesRegistered = false;

        public AdaptationManager() => RegDefaultRyoTypeJavaClzTable();

        // 注册默认Ryo类型
        private void RegDefaultRyoTypeJavaClzTable()
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

        // 列表取元素类型
        public RyoType GetArrayElementRyoType(RyoType ryoType)
        {
            if (ryoType.IsArray)
            {
                if (ryoType.JavaClassName != null) return GetRyoTypeByJavaClz(ryoType.JavaClassName);
                else if (ryoType.CsType != null) return GetRyoTypeByCsClz(ryoType.CsType);
                else throw new IllegalRyoTypeException("The element Ryo Type can not be resolved without a reference");
            }
            throw new IllegalRyoTypeException("The given Ryo Type is not an array Ryo Type");
        }

        // GetRyoByJava
        public RyoType GetRyoTypeByJavaClz(string clzName)
        {
            // 是否是列表
            bool isArray = clzName.StartsWith('[');

            // 先拨弄短名，只有是数组下才以短名示人
            string? shortName = isArray ? clzName[1..] : null;
            if (clzName.StartsWith("[[")) clzName = clzName[1..]; // 是嵌套Java类名，保留原始类名

            // 是否是基本类型
            if (isArray && shortName!.StartsWith('L') && clzName.EndsWith(';'))
            {
                clzName = clzName[2..^1];
                shortName = null;
            }

            // 初始化
            RyoType? ryoType = null;

            // 匹配注册的类
            foreach (var item in BasicRyoTypes.Concat(RyoTypes).Where(item => shortName != null ? item.JavaShortName == shortName : item.IsJavaUniversalType ? clzName.StartsWith(item.JavaClassName![..^2]) : clzName == item.JavaClassName!))
            {
                ryoType = item;
                break;
            }

            // 没有就创建新的
            if (ryoType == null)
            {
                // 查找可适配自定义
                Type? baseType = AdaptationUtils.SearchAdaptableFormations(clzName);
                bool isAdaptableCustom = baseType != null;

                bool isAdaptWithCtor = baseType != null && typeof(ICtorAdaptable).IsAssignableFrom(baseType);

                // 不是可适配自定义就没有BaseType：要不要加个策略然后抛出以免非法RyoType
                ryoType = new() { IsAdaptableCustom = isAdaptableCustom, JavaClassName = clzName, CsType = baseType, IsAdaptWithCtor = isAdaptWithCtor };
            }
            ryoType.IsArray = isArray;

            return ryoType;
        }

        // GetJavaByRyo
        public string? GetJavaClzByRyoType(RyoType ryoType)
        {
            LogUtils.PrintInfo($"Resolve Java Class for: {ryoType}");

            var typeNamePrefix = new StringBuilder("");
            while (ryoType.IsArray)
            {
                typeNamePrefix.Append('[');
                ryoType = GetArrayElementRyoType(ryoType);
                LogUtils.PrintInfo($"Array sublevel: {ryoType}");
            }

            if (ryoType.JavaClassName == null) return null;

            return typeNamePrefix.Length != 0 ? ryoType.JavaShortName ?? ryoType.JavaClassName : ryoType.JavaClassName;
        }

        // GetCsByRyo
        public Type? GetCsClzByRyoType(RyoType ryoType)
        {
            LogUtils.PrintInfo($"Resolve C# Type for: {ryoType}");

            List<int> arrayLevelAndTypes = new();
            while (ryoType.IsArray)
            {
                arrayLevelAndTypes.Add(ryoType.IsListInternally ? 1 : 0);
                ryoType = GetArrayElementRyoType(ryoType);
                LogUtils.PrintInfo($"Array sublevel: {ryoType}");
            }

            // 额外查询，这个对于合法的RyoType不可能
            // if (baseType == null && ryoType.IsAdaptableCustom) baseType = AdaptationUtils.SearchAdaptableFormations(ryoType.JavaClzName!);

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
        public RyoType GetRyoTypeByCsClz(Type type)
        {
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
                ryoType = item;
                break;
            }

            if (ryoType == null)
            {
                // 如果为可适配自定义，那么就有attr_format_name，就不用if 然后检测字段还是构造器
                var attr = type.GetCustomAttribute<AdaptableFormationAttribute>();
                var clzName = attr?.FormationName;
                bool isAdaptWithCtor = clzName != null && typeof(ICtorAdaptable).IsAssignableFrom(type);
                //clzName ??= type.Name; // Java不友好

                // 没有就创建新的
                ryoType = new() { IsAdaptableCustom = attr != null, IsAdaptWithCtor = isAdaptWithCtor, JavaClassName = clzName, CsType = type };
            }
            ryoType.IsArray = isArray;
            ryoType.IsListInternally = isList;

            return ryoType;
        }

        // CreateAdapter（时机）
        public IAdapter CreateAdapter(RyoType adapterFactoryRyoType, RyoType dataRyoType)
        {
            var factoryType = GetCsClzByRyoType(adapterFactoryRyoType);

            if (typeof(IAdapterFactory).IsAssignableFrom(factoryType))
                return ((IAdapterFactory)Activator.CreateInstance(factoryType)!).CreateAdapterForDataRyoType(dataRyoType);
            else throw new InvalidCastException($"{adapterFactoryRyoType}不是一个适配器工厂类");
        }

        public RyoType? FindAdapterRyoTypeForDataRyoType(RyoType ryoType)
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

    public interface IAdapter
    {
        object From(Mass mass, RyoReader reader, RyoType ryoType);

        void To(object obj, Mass mass, RyoWriter writer);

        // string JavaClz { get; }
    }

    // 无奈之举
    public class DirectReadRawBytesAdapter : IAdapter
    {
        // public string JavaClz => "sengine.mass.serializers.FailedSerializer";

        public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadAllBytes();

        public void To(object obj, Mass mass, RyoWriter writer) => writer.WriteBytes((byte[])obj);
    }

    public interface ICtorAdaptable
    {
        // TODO:一、我希望能动态构建类；二、如果真没多个Mass构造器，那就改成根据类公开成员的顺序进行输入输出类型的构建

        [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
        public class AdaptableConstructor : Attribute { }

        object[] GetAdaptedArray();
    }

    [Obsolete]
    public interface IDumpable
    {
        Dictionary<string, byte[]> GetDumpableObjects();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AdaptableFormationAttribute : Attribute
    {
        public readonly string FormationName;

        public AdaptableFormationAttribute(string formatName) => FormationName = formatName;
    }
}