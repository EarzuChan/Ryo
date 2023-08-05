using Me.EarzuChan.Ryo.Adaptions.AdapterFactories;
using Me.EarzuChan.Ryo.Formations;
using Me.EarzuChan.Ryo.IO;
using Me.EarzuChan.Ryo.Masses;
using Me.EarzuChan.Ryo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static Me.EarzuChan.Ryo.Formations.SaraDialogueTree;

namespace Me.EarzuChan.Ryo.Adaptions
{
    public class RyoType
    {
        public string? ShortName; // Java短名
        public string? Name; // Java名
        public bool IsAdaptableCustom = false; // 可适配类
        public bool IsArray = false; // 是列表
        public Type? BaseType; // C#基类
        public bool IsAdaptWithCtor = false; // 是否用构造器来适配

        // 属性
        public bool IsJvmBaseType => ShortName != null; // JVM基本类型
        public bool IsUnidentifiedType => BaseType == null; // 未识别
        public bool HasSubarray => (Name != null && Name.StartsWith('['));

        /*public RyoType GetSubitemRyoType()
         {
             if (!IsArray) throw new NotSupportedException(this + "不是列表");
             var am = AdaptionManager.INSTANCE;
             return am.GetTypeByJavaClz(am.GetJavaClzByType(this)[1..]);
         }*/

        public override string ToString()
        {
            // return $"[RyoType信息：{AdaptionManager.INSTANCE.GetJavaClzByType(this)}，Java短名：{ShortName}，Java名：{Name}，自定义：{IsCustom}，是列表：{IsArray}，C#类：{BaseType}]";
            return $"[RyoType：Java：{Name}，短名：{(ShortName == null ? "无" : ShortName)}，可适配自定：{IsAdaptableCustom}，列表：{IsArray}，C#：{BaseType}]";
        }
    }

    public class AdaptionManager
    {
        // public static readonly IntRyoType=

        public static AdaptionManager INSTANCE { get { instance ??= new(); return instance; } }
        private static AdaptionManager? instance;
        public HashSet<RyoType> RyoTypes = new();

        public AdaptionManager() => RegDefaultRyoTypeJavaClzTable();

        public void RegDefaultRyoTypeJavaClzTable()
        {
            // 注册基本类型
            RyoTypes.Add(new() { ShortName = "Z", Name = "java.lang.Boolean", BaseType = typeof(bool) });
            RyoTypes.Add(new() { ShortName = "B", Name = "java.lang.Byte", BaseType = typeof(byte) });
            RyoTypes.Add(new() { ShortName = "C", Name = "java.lang.Character", BaseType = typeof(char) });
            RyoTypes.Add(new() { ShortName = "D", Name = "java.lang.Double", BaseType = typeof(double) });
            RyoTypes.Add(new() { ShortName = "F", Name = "java.lang.Float", BaseType = typeof(float) });
            RyoTypes.Add(new() { ShortName = "I", Name = "java.lang.Integer", BaseType = typeof(int) });
            RyoTypes.Add(new() { ShortName = "J", Name = "java.lang.Long", BaseType = typeof(long) });
            RyoTypes.Add(new() { ShortName = "S", Name = "java.lang.Short", BaseType = typeof(short) });
            RyoTypes.Add(new() { ShortName = "V", Name = "java.lang.Void", BaseType = typeof(void) });
            RyoTypes.Add(new() { Name = "java.lang.String", BaseType = typeof(string) });

            // 特定类（图片等）
            RyoTypes.Add(new() { Name = "sengine.graphics2d.texturefile.FIFormat$FragmentedImageData", BaseType = typeof(FragmentalImage) });

            // 注册工厂类
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.DefaultSerializers", BaseType = typeof(BaseTypeAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.DefaultArraySerializers", BaseType = typeof(BaseArrayTypeAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.CollectionSerializer", BaseType = typeof(BaseArrayTypeAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.MapSerializer", BaseType = typeof(BaseArrayTypeAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.MassSerializableSerializer", BaseType = typeof(CustomFormatAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.FieldSerializer", BaseType = typeof(CustomFormatAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.graphics2d.texturefile.FIFormat", BaseType = typeof(SpecialFormatAdapterFactory) });

            // 注册额外项目（从文件）：TODO
        }

        // GetRyoByJava
        public RyoType GetRyoTypeByJavaClz(string clzName)
        {
            // 是否是列表
            bool isArray = clzName.StartsWith('[');

            // 先拨弄短名
            string? shortName = isArray ? clzName[1..] : null;
            if (clzName.StartsWith("[[")) clzName = clzName[1..]; // 是那个嵌套
            // LogUtil.INSTANCE.PrintInfo("isArray：" + isArray, "shortName：" + shortName, "clzName：" + clzName);

            // 是否是基本类型
            if (isArray && shortName!.StartsWith('L') && clzName.EndsWith(';'))
            {
                clzName = clzName[2..^1];
                shortName = null;
            }

            // 初始化
            RyoType? ryoType = null;

            // 匹配注册的类
            foreach (var item in RyoTypes.Where(item => shortName != null ? item.ShortName == shortName : clzName.StartsWith(item.Name!)))
            {
                ryoType = item;
                break;
            }

            // 没有就创建新的
            if (ryoType == null)
            {
                // 是可适配自定义
                Type? baseType = SearchBaseType(clzName);
                bool isAdaptableCustom = baseType != null;

                bool isAdaptWithCtor = false;
                if (baseType != null) isAdaptWithCtor = typeof(ICtorAdaptable).IsAssignableFrom(baseType);

                // if (!isAdaptableCustom) throw new InvalidDataException($"给定的类名“{clzName}”不合法：" );

                // 不是可适配自定义就没有BaseType
                ryoType = new() { IsAdaptableCustom = isAdaptableCustom, Name = clzName, BaseType = baseType, IsAdaptWithCtor = isAdaptWithCtor };

                // 还套数组怎么办？不关它事，用一个属性怎么样？
                // if (clzName.StartsWith('[')) ryoType.HasSubitem = true;

                //尝试直接forName否？不吧
            }
            ryoType.IsArray = isArray;

            return ryoType;
        }

        private Type? SearchBaseType(string clzName)
        {
            Type? baseType = null;
            // string[] splitedClzName = clzName.Split('$');
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
            foreach (var item in types)
            {
                var attribute = item.GetCustomAttribute<AdaptableFormat>();
                if (attribute != null)
                {
                    if (attribute.FormatName == clzName) // splitedClzName[0])
                    {
                        // Type theItem = item;
                        /*if (splitedClzName.Length == 2) // 需要同名子类则置换
                        {
                            Type[] nestedTypes = item.GetNestedTypes();
                            if (nestedTypes.Length == 0) break; // 没有嵌套类
                                                                // foreach (var typ in nestedTypes) LogUtils.INSTANCE.PrintInfo($"类名：{typ} 需要：{splitedClzName[1]}");
                            Type? matchingNestedType = nestedTypes.FirstOrDefault(nestedType => nestedType.Name.EndsWith(splitedClzName[1])); // TODO:优化
                            if (matchingNestedType == null) continue; // 没有符合
                            theItem = matchingNestedType;
                            LogUtils.INSTANCE.PrintInfo("找到符合：" + theItem);
                        }*/
                        baseType = item; // theItem;
                        break;
                    }
                }
            }
            return baseType;
        }

        // GetJavaByRyo
        public string? GetJavaClzByRyoType(RyoType ryoType)
        {
            if (ryoType.ShortName == null && ryoType.Name == null) return null;

            string clzName = ryoType.IsArray ? "[" : "";

            // LogUtil.INSTANCE.PrintInfo("数组：" + ryoType.IsArray, "有子项数组：" + ryoType.HasSubarray, "名称：" + ryoType.Name);
            //因为Ryo已经含有JavaClz相关，所以：
            clzName += ryoType.IsArray ? ryoType.ShortName == null ? !ryoType.HasSubarray ? "L" + ryoType.Name + ";" : ryoType.Name : ryoType.ShortName : ryoType.Name;

            return clzName;
        }

        // 故意无法直转（[[B）怎么办？
        // GetCsByRyo
        public Type? GetCsClzByRyoType(RyoType ryoType)
        {
            // if (!ryoType.IsCustom) throw new FormatException("非自定义格式" + ryoType);

            //LogUtil.INSTANCE.PrintInfo("Parsing Format：" + ryoType);

            // 匹配一手自定义
            Type? baseType = ryoType.BaseType;

            // 额外查询
            if (baseType == null && ryoType.IsAdaptableCustom) baseType = SearchBaseType(ryoType.Name!);

            // baseType ??= Type.GetType(ryoType.Name!); JAVA不友好
            //formatType ??= typeof(object);
            // if (formatType == null) throw new InvalidCastException(ryoType + "不可转换为有效C#类");

            if (ryoType.IsArray) baseType = baseType?.MakeArrayType();

            return baseType;
        }

        // GetRyoByCs
        public RyoType GetRyoTypeByCsClz(Type type)
        {
            // 列表相关
            bool isArray = type.IsArray;
            if (isArray) type = type.GetElementType()!;

            // 初始化
            RyoType? ryoType = null;

            // 匹配基本类型
            foreach (var item in RyoTypes.Where(item => item.BaseType == type))
            {
                ryoType = item;
                break;
            }

            // 如果为可适配自定义，那么就有attr_format_name，就不用if
            var attr = type.GetCustomAttribute<AdaptableFormat>();
            var clzName = attr?.FormatName;
            //clzName ??= type.Name; // Java不友好

            // 没有就创建新的
            ryoType ??= new() { IsAdaptableCustom = clzName != null, Name = clzName, BaseType = type };
            ryoType.IsArray = isArray;

            return ryoType;
        }

        // CreateAdapter（时机）
        public IAdapter CreateAdapter(RyoType adapterFactoryRyoType, RyoType dataRyoType)
        {
            var factoryType = GetCsClzByRyoType(adapterFactoryRyoType);

            if (typeof(IAdapterFactory).IsAssignableFrom(factoryType))
                return ((IAdapterFactory)Activator.CreateInstance(factoryType)!).Create(dataRyoType);
            else throw new InvalidCastException($"{adapterFactoryRyoType}不是一个适配器工厂类");
        }

        public RyoType? FindAdapterRyoTypeForDataRyoType(RyoType ryoType)
        {
            RyoType? result = null;
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
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

        // 根据RyoType匹配适配器RyoType在哪里？
    }

    public interface IAdapter
    {
        object From(Mass mass, RyoReader reader, RyoType ryoType);

        void To(object obj, Mass mass, RyoWriter writer);

        // string JavaClz { get; }
    }

    public class DirectByteArrayAdapter : IAdapter
    {
        public string JavaClz => "sengine.mass.serializers.FailedSerializer";

        public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadAllBytes();

        public void To(object obj, Mass mass, RyoWriter writer) => writer.WriteBytes((byte[])obj);
    }

    public interface ICtorAdaptable
    {
        // TODO:一、我希望能动态构建类；二、如果真没多个Mass构造器，那就改成根据类公开成员的顺序进行输入输出类型的构建

        [AttributeUsage(AttributeTargets.Constructor)]
        public class AdaptableConstructor : Attribute { }

        object[] GetAdaptedArray();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class AdaptableFormat : Attribute
    {
        public readonly string FormatName;
        // private readonly bool IsSubSameNameType = false;

        public AdaptableFormat(string formatName) => FormatName = formatName;
        // public AdaptableFormat() => IsSubSameNameType = true;
    }
}
