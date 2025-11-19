using Me.EarzuChan.Ryo.Exceptions.FileExceptions;

namespace Me.EarzuChan.Ryo.Utils;

public static class FileUtils
{
    /*public Mass GetMass(string fileName){}*/

    public static FileStream OpenFile(string path, bool needWrite = false, bool allowCreate = false, bool guardZero = true)
    {
        if (!allowCreate && !File.Exists(path)) throw new NoSuchFileException(path);

        FileStream fileStream = new(path, allowCreate ? FileMode.OpenOrCreate : FileMode.Open);

        if (!fileStream.CanRead) throw new RestrictedFileAccessException(path, RestrictedFileAccessException.RestrictedFileAccess.Read);

        switch (needWrite)
        {
            case true when !fileStream.CanWrite:
                throw new RestrictedFileAccessException(path, RestrictedFileAccessException.RestrictedFileAccess.Write);

            // 【核心修改在这里】
            // 如果是写入模式，且目的是为了“保存/覆盖”，则清空原有内容
            case true:
                fileStream.SetLength(0);
                break;

            // 注意：如果你把文件清空了，下面的 guardZero 可能会误报，需要根据业务逻辑调整
            // 通常“保存”操作不需要防卫 0 长度，只有“读取”才需要
            // 建议修改判断逻辑，只在 !needWrite (读取模式) 时检查 guardZero
            case false when guardZero && fileStream.Length == 0 && !allowCreate:
                throw new EmptyFileException(path);
        }

        return fileStream;
    }
}