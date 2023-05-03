using Me.EarzuChan.Ryo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.IO
{
    public class RyoBuffer
    {
        public byte[] Buffer;
        public int Length { get => Buffer.Length; }
        public int Limit { set; get; }
        public int Position { set; get; }

        public RyoBuffer() => Buffer = Array.Empty<byte>();

        public RyoBuffer(byte[] buffer) => Buffer = buffer;

        public string Dump(string path)
        {
            //if (!ReadyToUse || IsEmpty) return "未准备好或者内容为空";
            //if (string.IsNullOrWhiteSpace(FullPath)) return "保存路径为空";
            var info = "保存";
            try
            {
                using (FileStream fs = new(path, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(Buffer, 0, Buffer.Length);
                }
                info += path;
            }
            catch (Exception ex)
            {
                LogUtils.INSTANCE.PrintError("保存时错误", ex);
                info += "失败（悲";
            }
            return info;
        }
    }
}
