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
            if (!allowCreate && !File.Exists(path)) throw new FileNotFoundException("File does not exist：" + path);

            FileStream fileStream = new(path, allowCreate ? FileMode.OpenOrCreate : FileMode.Open);

            if (!fileStream.CanRead) throw new UnauthorizedAccessException("No read permission.");
            if (needWrite && !fileStream.CanWrite) throw new UnauthorizedAccessException("No write permission.");
            if (guardZero && fileStream.Length == 0 && !allowCreate) throw new FileLoadException("File is empty.");

            return fileStream;
        }
    }
}
