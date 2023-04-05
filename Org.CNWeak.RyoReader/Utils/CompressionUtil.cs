using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Me.EarzuChan.Ryo.Utils
{
    public class CompressionUtil
    {
        public static CompressionUtil INSTANCE { get { instance ??= new(); return instance; } }
        private static CompressionUtil? instance;

        public byte[] Inflate(byte[] input, int offset, int length)
        {
            try
            {
                var outStream = new MemoryStream();
                var inflater = new Inflater();
                inflater.SetInput(input, offset, length);
                var buffer = new byte[1024];
                while (!inflater.IsFinished)
                {
                    int count = inflater.Inflate(buffer);
                    outStream.Write(buffer, 0, count);
                }

                return outStream.ToArray();
            }
            catch (Exception e)
            {
                LogUtil.INSTANCE.PrintError("解压出错", e);
                return Array.Empty<byte>();
            }
        }

        public byte[] Deflate(byte[] indexBytes, int offset, int length)
        {
            Deflater deflater = new Deflater();
            deflater.SetInput(indexBytes, offset, length);
            deflater.Finish();

            byte[] buffer = new byte[1024];
            List<byte> outputList = new List<byte>();

            while (!deflater.IsFinished)
            {
                int count = deflater.Deflate(buffer);
                outputList.AddRange(buffer.Take(count));
            }

            return outputList.ToArray();
        }
    }
}