using System.Collections;
using System.Collections.Concurrent;
using Me.EarzuChan.Ryo.Exceptions.FileExceptions;
using Me.EarzuChan.Ryo.Core.Utils;
using System.Text;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Core.Masses;

public class MassManager
{
    public readonly ConcurrentDictionary<string, MassFile> MassFiles = new();

    public event Action<ConcurrentDictionary<string, MassFile>>? MassFilesChanged;

    public MassFile GetMassFileOrThrow(string fileName)
    {
        fileName = fileName.Trim().ToLower();

        var massFile = MassFiles.FirstOrDefault(item => item.Key.ToLower() == fileName).Value;

        return massFile ?? throw new NoSuchFileException(fileName);
    }

    public bool ExistsMass(string fileName) => MassFiles.ContainsKey(fileName);

    // TODO：改写
    public MassFile LoadMassFile(string filePath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("文件名", "不该为空");
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException("文件路径", "不该为空");
        fileName = fileName.Trim().ToLower();
        filePath = filePath.Trim();

        using var fileStream = FileUtils.OpenFile(filePath);

        var massFile = new MassFile();
        try
        {
            massFile.Load(fileStream);
        }
        catch (Exception ex)
        {
            throw new FileLoadException("加载文件时出错，" + ex.Message, ex);
        }

        try
        {
            MassFiles.TryAdd(fileName, massFile);
        }
        catch (Exception ex)
        {
            throw new FileLoadException("集成文件时出错，" + ex.Message, ex);
        }

        // MassFile里弄个事件，在这里监听以触发FilesChanged事件

        MassFilesChanged?.Invoke(MassFiles);

        return massFile;
    }

    public void UnloadMassFile(string fileName)
    {
        MassFiles.TryRemove(fileName.Trim().ToLower(), out _);

        MassFilesChanged?.Invoke(MassFiles);
    }

    public void AddMassFile(MassFile newMass, string fileName)
    {
        MassFiles.TryAdd(fileName, newMass);

        MassFilesChanged?.Invoke(MassFiles);
    }
}