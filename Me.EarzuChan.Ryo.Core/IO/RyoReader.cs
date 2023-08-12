using System.Text;

namespace Me.EarzuChan.Ryo.Core.IO
{
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

        public int ReadInt() => BitConverter.ToInt32(Reader.ReadBytes(4).Reverse().ToArray(), 0);

        public short ReadShort() => BitConverter.ToInt16(Reader.ReadBytes(2).Reverse().ToArray(), 0);

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

        public string ReadString()
        {
            int length = ReadInt();
            return ReadString(length);
        }

        public string ReadString(int length)
        {
            byte[] bytes = Reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        public bool CheckHasString(string str) => ReadString(Encoding.UTF8.GetBytes(str).Length) == str;

        public void Dispose() => Reader.Dispose();

        public byte[] ReadAllBytes() => Reader.ReadBytes((int)RestLength);

        public float ReadFloat() => BitConverter.ToSingle(ReadBytes(4).Reverse().ToArray(), 0);

        public byte ReadUnsignedByte() => Reader.ReadByte();

        public sbyte ReadSignedByte() => Reader.ReadSByte();

        public bool ReadBoolean() => ReadUnsignedByte() != 0;

        public static implicit operator RyoReader(byte[] buffer) => new RyoReader(new MemoryStream(buffer));
    }
}
