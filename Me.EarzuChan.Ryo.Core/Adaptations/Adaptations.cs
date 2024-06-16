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
        public bool IsCsTypeUnidentified => CsType == null; // C#未识别

        public override string ToString()
        {
            // return $"[RyoType信息：{AdaptionManager.INSTANCE.GetJavaClzByType(this)}，Java短名：{ShortName}，Java名：{Name}，自定义：{IsCustom}，是列表：{IsArray}，C#类：{BaseType}]";
            return $"{{RyoType: Java: {JavaClassName}, Java Short Name: {(JavaShortName ?? "None")}, Is Jvm Primitive: {IsJavaPrimitiveType}, Is Adaptable Custom: {IsAdaptableCustom}, Is Adapted with Constructor: {IsAdaptWithCtor}, Is Array: {IsArray}( Is List Array: {IsListInternally}), C#: {CsType}}}";
        }

        internal RyoType() { }
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