using Me.EarzuChan.Ryo.Adaptions.AdapterFactories;
using Me.EarzuChan.Ryo.IO;
using Me.EarzuChan.Ryo.Masses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Adaptions
{
    public class RyoType
    {
        public string? ShortName; // Java短名
        public string? Name; // Java名
        public bool IsCustom = false; // 自定义类
        public bool IsArray = false; // 是列表
        public bool ArrayWithL = false; // 列表用L
        public Type? BaseType; // C#基类

        public RyoType GetSubitemRyoType()
        {
            if (!IsArray) throw new NotSupportedException(this + "不是列表");
            var am = AdaptionManager.INSTANCE;
            return am.GetTypeByJavaClz(am.GetJavaClzByType(this)[1..]);
        }

        public override string ToString()
        {
            // return $"[RyoType信息：{AdaptionManager.INSTANCE.GetJavaClzByType(this)}，Java短名：{ShortName}，Java名：{Name}，自定义：{IsCustom}，是列表：{IsArray}，C#类：{BaseType}]";
            return $"[RyoType：Java：{Name}，自定：{IsCustom}，列表：{IsArray}，C#：{BaseType}]";
        }
    }

    public class AdaptionManager
    {
        public static AdaptionManager INSTANCE { get { instance ??= new(); return instance; } }
        private static AdaptionManager? instance;
        public HashSet<RyoType> RyoTypes = new();

        public AdaptionManager() => RegDefaultTypeJavaClzTable();

        public void RegDefaultTypeJavaClzTable()
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

            // 注册工厂类
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.DefaultSerializers", BaseType = typeof(BaseTypeAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.DefaultArraySerializers", BaseType = typeof(BaseArrayTypeAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.CollectionSerializer", BaseType = typeof(BaseArrayTypeAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.MapSerializer", BaseType = typeof(BaseArrayTypeAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.MassSerializableSerializer", BaseType = typeof(CustomFormatAdapterFactory) });
            RyoTypes.Add(new() { Name = "sengine.mass.serializers.FieldSerializer", BaseType = typeof(CustomFormatAdapterFactory) });

            // 特定类（图片等）
            RyoTypes.Add(new() { Name = "sengine.graphics2d.texturefile.FIFormat", BaseType = typeof(SpecialFormatAdapterFactory) });

            // 注册额外项目（从文件）：TODO
        }

        public RyoType GetTypeByJavaClz(string clzName)
        {
            /*bool isFullArray = clzName.StartsWith("[L");
            if (isFullArray) clzName = clzName[2..];*/

            bool isArray = clzName.StartsWith('[');
            if (isArray) clzName = clzName[1..];

            bool arrayWithL = false;
            if (isArray & clzName.StartsWith("L") && clzName.EndsWith(";"))
            {
                //isCustom = true;
                arrayWithL = true;
                clzName = clzName[1..^1];
            }

            RyoType? type = null;
            // 匹配基类
            foreach (var item in RyoTypes.Where(item => item.ShortName == clzName || clzName.StartsWith(item.Name!)))
            {
                type = item;
                break;
            }

            // 没有就创建新的
            type ??= new() { IsCustom = true, Name = clzName, ArrayWithL = arrayWithL };
            type.IsArray = isArray;

            return type;
        }

        public string GetJavaClzByType(RyoType type)
        {
            string clzName = type.IsArray ? "[" : "";

            if (type.IsCustom) clzName += type.ArrayWithL ? "L" + type.Name + ";" : type.Name; //自定义
            else // 非自定义
            {
                foreach (var item in RyoTypes) // 遍历
                {
                    if (type.BaseType == item.BaseType) // 相等
                    {
                        if (item.ShortName != null && type.IsArray) clzName += item.ShortName; // 是列表且有短名
                        else
                        {
                            if (type.IsArray) clzName += "L";
                            clzName += item.Name;
                            if (type.IsArray) clzName += ";";
                        }
                        break;
                    }
                }
            }

            return clzName;
        }

        public Type? TryParseCustomFormatType(RyoType ryoType)
        {
            if (!ryoType.IsCustom) throw new FormatException("非自定义格式" + ryoType);

            //LogUtil.INSTANCE.PrintInfo("Parsing Format：" + ryoType);

            Type? formatType = null;
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
            foreach (var item in types)
            {
                var attribute = item.GetCustomAttribute<IAdaptable.AdaptableFormat>();
                if (attribute != null && typeof(IAdaptable).IsAssignableFrom(item))
                {
                    if (attribute.FormatName == ryoType.Name)
                    {
                        formatType = item;
                        break;
                    }
                }
            }

            if (ryoType.IsArray) formatType = formatType?.MakeArrayType();
            return formatType;
        }
    }

    public interface IAdapter
    {
        object From(Mass mass, RyoReader reader, RyoType ryoType);

        void To(object obj, Mass mass, RyoWriter writer);

        string JavaClz { get; }
    }

    public class DirectByteArrayAdapter : IAdapter
    {
        public string JavaClz => "sengine.mass.serializers.FailedSerializer";

        public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadAllBytes();

        public void To(object obj, Mass mass, RyoWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public interface IAdaptable
    {
        // TODO:一、我希望能动态构建类；二、如果真没多个Mass构造器，那就改成根据类公开成员的顺序进行输入输出类型的构建

        [AttributeUsage(AttributeTargets.Constructor)]
        public class AdaptableConstructor : Attribute { }

        [AttributeUsage(AttributeTargets.Class)]
        public class AdaptableFormat : Attribute
        {
            public string FormatName;
            public AdaptableFormat(string formatName) => FormatName = formatName;
        }

        object[] GetAdaptedArray();
    }
}
