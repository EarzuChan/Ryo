using Me.EarzuChan.Ryo.Exceptions.FileExceptions;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class FileUtils
    {
        /*public Mass GetMass(string fileName){}*/

        public static FileStream OpenFile(string path, bool needWrite = false, bool allowCreate = false, bool guardZero = true)
        {
            if (!allowCreate && !File.Exists(path)) throw new NoSuchFileException(path);

            FileStream fileStream = new(path, allowCreate ? FileMode.OpenOrCreate : FileMode.Open);

            if (!fileStream.CanRead) throw new RestrictedFileAccessException(path, RestrictedFileAccessException.RestrictedFileAccess.Read);
            if (needWrite && !fileStream.CanWrite) throw new RestrictedFileAccessException(path, RestrictedFileAccessException.RestrictedFileAccess.Write);
            if (guardZero && fileStream.Length == 0 && !allowCreate) throw new EmptyFileException(path);

            return fileStream;
        }
    }
}