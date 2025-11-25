using Me.EarzuChan.Ryo.Core.IO;
using Me.EarzuChan.Ryo.Core.Masses;

namespace Me.EarzuChan.Ryo.Core.Codecations;

public class RyoType
{
    public string? JavaShortName; // Java短名
    public string? JavaClassName; // Java名
    public bool IsCodecableCustom = false; // 可编解码类
    public bool IsArray = false; // 是数组
    public Type? CsType; // C#基类
    public bool IsCodecateWithCtor = false; // 是否用构造器来编解码

    internal bool IsListInternally = false; // 奇技淫巧

    // 属性
    public bool IsJavaPrimitiveType => JavaShortName != null; // JVM原型

    public bool IsJavaUniversalType => JavaClassName!.EndsWith("$*");

    public bool IsJvmBasicType => JavaClassName != null && JavaClassName.StartsWith("java.lang."); // JVM基本类型
    
    public bool IsCsTypeUnidentified => CsType == null; // C#未识别

    public override string ToString() => $"{{RyoType - Java: {JavaClassName}, JavaShort: {(JavaShortName ?? "None")}, JvmPrimitive: {IsJavaPrimitiveType}, CodecableCustom: {IsCodecableCustom}, CodecatedWithCtor: {IsCodecateWithCtor}, Array: {IsArray}( ListArray: {IsListInternally}), C#: {CsType}, Uid: {GetHashCode()}}}";

    internal RyoType()
    {
    }
}

public interface ICodec
{
    object From(Mass mass, RyoReader reader, RyoType ryoType);

    void To(object obj, Mass mass, RyoWriter writer);
}

// Fallback编解码器：读取原始字节流
public class ReadRawBytesCodec : ICodec
{
    public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadAllBytes();

    public void To(object obj, Mass mass, RyoWriter writer) => throw new NotSupportedException("本编解码器为Fallback编解码器，仅能用于直读原始字节流");
}

public interface ICtorCodecable
{
    // TODO：一、我希望能动态构建类；二、如果真没多个Mass构造器，那就改成根据类公开成员的顺序进行输入输出类型的构建
    [AttributeUsage(AttributeTargets.Constructor)]
    public class CodecableConstructor : Attribute
    {
    }

    object[] GetCodecatedArray();
}

[AttributeUsage(AttributeTargets.Class)]
public class CodecableFormationAttribute(string formatName) : Attribute
{
    public readonly string FormationName = formatName;
}