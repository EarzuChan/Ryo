using Me.EarzuChan.Ryo.Core.Utils;

namespace Me.EarzuChan.Ryo.Core.IO
{
    public class RyoBuffer
    {
        public byte[] Buffer;
        public int Length { get => Buffer.Length; }
        public int Limit { set; get; }
        public int Position { set; get; }

        public RyoBuffer() => Buffer = Array.Empty<byte>();

        public RyoBuffer(byte[] buffer) => Buffer = buffer;
    }
}