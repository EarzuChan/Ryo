using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class CompressionUtils
    {
        public static byte[] Inflate(byte[] input, int offset, int length)
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
                LogUtils.INSTANCE.PrintError("解压出错", e);
                return Array.Empty<byte>();
            }
        }

        public static byte[] Deflate(byte[] indexBytes, int offset, int length)
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