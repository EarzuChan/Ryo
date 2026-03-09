using System.Buffers.Binary;
using System.Text;

namespace Me.EarzuChan.Ryo.Core.IO;

public class RyoWriter : IDisposable
{
    public void Dispose()
    {
        Writer.Flush();
        Writer.Dispose();
    }

    private readonly BinaryWriter Writer;

    public long RestLength => Length - Position;

    public long Length => Writer.BaseStream.Length; //不应该，应该是读缓冲区，虽然我没用

    public long Position
    {
        get => Writer.BaseStream.Position; //不应该，应该是读缓冲区，虽然我没用
        set => Writer.BaseStream.Position = value;
    }

    public RyoWriter(Stream outputStream) => Writer = new(outputStream);

    public void WriteInt(int intToWrite)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, intToWrite);
        Writer.Write(buffer); // Modern .NET BinaryWriter 支持 ReadOnlySpan<byte>
    }

    public void WriteShort(short shortToWrite) {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, shortToWrite);
        Writer.Write(buffer);
    }

    public void WriteUnsignedByte(byte bt) => Writer.Write(bt);

    public void PositionToZero() => Position = 0;

    public void WriteBytes(byte[] pixels) => Writer.Write(pixels);

    public void WriteString(string? v)
    {
        if (v == null) {
            WriteInt(-1);
            return;
        }
        
        if (v.Length == 0) {
            WriteInt(0);
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(v);
        WriteInt(bytes.Length);
        WriteBytes(bytes);
    }

    public static implicit operator Stream(RyoWriter writer) => writer.Writer.BaseStream;

    public void WriteFixedString(string format) => WriteBytes(Encoding.UTF8.GetBytes(format));

    public void WriteSignedByte(sbyte i) => Writer.Write(i);

    public void WriteFloat(float f) {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleBigEndian(buffer, f);
        Writer.Write(buffer);
    }

    public void WriteBoolean(bool ob) => WriteSignedByte((sbyte)(ob ? 1 : 0));
    
    // 新增：WriteLong
    public void WriteLong(long longToWrite)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(buffer, longToWrite);
        Writer.Write(buffer);
    }

    // 新增：WriteDouble
    public void WriteDouble(double doubleToWrite)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleBigEndian(buffer, doubleToWrite);
        Writer.Write(buffer);
    }

    // 新增：WriteChar (Java char 是 2 字节无符号整数)
    public void WriteChar(char charToWrite)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, charToWrite);
        Writer.Write(buffer);
    }
}