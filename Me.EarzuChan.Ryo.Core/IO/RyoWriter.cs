using System.Text;

namespace Me.EarzuChan.Ryo.Core.IO
{
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

        public void WriteInt(int intToWrite) => Writer.Write(BitConverter.GetBytes(intToWrite).Reverse().ToArray());

        public void WriteShort(short shortToWrite) => Writer.Write(BitConverter.GetBytes(shortToWrite).Reverse().ToArray());

        public void WriteUnsignedByte(byte bt) => Writer.Write(bt);

        public void PositionToZero() => Position = 0;

        public void WriteAnotherWriter(RyoWriter anoWriter) => Writer.Write(new RyoReader(anoWriter.Writer.BaseStream).ReadAllBytes());

        public void WriteBytes(byte[] pixels) => Writer.Write(pixels);

        public void WrintString(string v)
        {
            byte[] vytes = Encoding.UTF8.GetBytes(v);
            WriteInt(vytes.Length);
            WriteBytes(vytes);
        }

        public static implicit operator Stream(RyoWriter writer) => writer.Writer.BaseStream;

        public void WriteFixedString(string format) => WriteBytes(Encoding.UTF8.GetBytes(format));

        public void WriteSignedByte(sbyte i) => Writer.Write(i);

        public void WriteFloat(float ob) => Writer.Write(BitConverter.GetBytes(ob).Reverse().ToArray());

        public void WriteBoolean(bool ob) => WriteSignedByte((sbyte)(ob ? 1 : 0));
    }
}
