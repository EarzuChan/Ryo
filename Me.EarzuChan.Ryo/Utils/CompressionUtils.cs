using ICSharpCode.SharpZipLib.Zip.Compression;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class CompressionUtils
    {
        // 是否抛出要看策略

        public static byte[] Inflate(byte[] input, int offset, int length)
        {
            try
            {
                var inflater = new Inflater();
                inflater.SetInput(input, offset, length);

                var buffer = new byte[1024];

                List<byte> outputList = [];

                while (!inflater.IsFinished)
                {
                    var count = inflater.Inflate(buffer);
                    outputList.AddRange(buffer.Take(count));
                }

                return outputList.ToArray();
            }
            catch (Exception e)
            {
                LogUtils.PrintError("解压出错", e);
                return [];
            }
        }

        public static byte[] Deflate(byte[] indexBytes, int offset, int length)
        {
            try
            {
                Deflater deflater = new();
                deflater.SetInput(indexBytes, offset, length);
                deflater.Finish();

                var buffer = new byte[1024];

                List<byte> outputList = [];

                while (!deflater.IsFinished)
                {
                    var count = deflater.Deflate(buffer);
                    outputList.AddRange(buffer.Take(count));
                }

                return outputList.ToArray();
            }
            catch (Exception e)
            {
                LogUtils.PrintError("压缩出错", e);
                return [];
            }
        }
    }
}