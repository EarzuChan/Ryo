using System.Buffers.Binary;
using System.Text;

namespace Me.EarzuChan.Ryo.Core.IO;

public class RyoReader : IDisposable
{
    private readonly BinaryReader Reader;

    public long RestLength => Length - Position;

    public long Length => Reader.BaseStream.Length; //不应该，应该是读缓冲区，虽然我没用

    public long Position
    {
        get => Reader.BaseStream.Position; //不应该，应该是读缓冲区，虽然我没用
        set => Reader.BaseStream.Position = value;
    }

    public RyoReader(Stream inputStream) => Reader = new(inputStream);

    public byte[] ReadBytes(int length) => Reader.ReadBytes(length);

    public int ReadInt() {
        Span<byte> buffer = stackalloc byte[4];
        Reader.Read(buffer); // 直接读入栈内存，零分配
        return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }

    public short ReadShort() {
        Span<byte> buffer = stackalloc byte[2];
        Reader.Read(buffer);
        return BinaryPrimitives.ReadInt16BigEndian(buffer);
    }

    public string ReadBytesToHexString(int length)
    {
        byte[] bytes = Reader.ReadBytes(length);

        StringBuilder sb = new();
        foreach (byte b in bytes)
        {
            sb.AppendFormat("0x{0:X2},", b);
        }

        if (sb.Length > 0)
        {
            sb.Length--;
        }

        return sb.ToString();
    }

    public string? ReadString() {
        int length = ReadInt();
        return length switch {
            < 0 => null,
            0 => "",
            _ => ReadString(length)
        };
    }

    private string ReadString(int length) => Encoding.UTF8.GetString(Reader.ReadBytes(length));

    public bool CheckHasString(string str) => ReadString(Encoding.UTF8.GetBytes(str).Length) == str;

    public void Dispose() => Reader.Dispose();

    public byte[] ReadAllBytes() => Reader.ReadBytes((int)RestLength);

    public float ReadFloat() {
        Span<byte> buffer = stackalloc byte[4];
        Reader.Read(buffer);
        return BinaryPrimitives.ReadSingleBigEndian(buffer);
    }

    public byte ReadUnsignedByte() => Reader.ReadByte();

    public sbyte ReadSignedByte() => Reader.ReadSByte();

    public bool ReadBoolean() => ReadUnsignedByte() != 0;
    
    public long ReadLong() {
        Span<byte> buffer = stackalloc byte[8];
        Reader.Read(buffer);
        return BinaryPrimitives.ReadInt64BigEndian(buffer);
    }

    public double ReadDouble() {
        Span<byte> buffer = stackalloc byte[8];
        Reader.Read(buffer);
        return BinaryPrimitives.ReadDoubleBigEndian(buffer);
    }

    public char ReadChar() {
        Span<byte> buffer = stackalloc byte[2];
        Reader.Read(buffer);
        return (char)BinaryPrimitives.ReadUInt16BigEndian(buffer);
    }

    public static implicit operator RyoReader(byte[] buffer) => new(new MemoryStream(buffer));
}