using Me.EarzuChan.Ryo.Formations;
using Me.EarzuChan.Ryo.Masses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class FileUtils
    {
        /*public Mass GetMass(string fileName){}*/

        public static FileStream OpenFile(string path, bool needWrite = false, bool allowCreate = false, bool guardZero = true)
        {
            if (!allowCreate && !File.Exists(path)) throw new FileNotFoundException("文件不存在：" + path);

            FileStream fileStream = new(path, allowCreate ? FileMode.OpenOrCreate : FileMode.Open);

            if (!fileStream.CanRead) throw new UnauthorizedAccessException("没有读取权限");
            if (needWrite && !fileStream.CanWrite) throw new UnauthorizedAccessException("没有写入权限");
            if (guardZero && fileStream.Length == 0) throw new FileLoadException("文件长度为零");

            return fileStream;
        }
    }
}
