using System.Collections;
using System.Diagnostics;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Extensions.MassExtensions;

public class LocalVolumeManager
{
    public const string Tag = "LocalVolumeManager";

    public readonly Dictionary<LocalVolume, LocalVolumeMetaData> Volumes = [];

    public event Action<Dictionary<LocalVolume, LocalVolumeMetaData>>? VolumesChanged;

    public LocalVolume CreateNewVolume(string fileName)
    {
        var massFile = new MassFile();

        var volume = new LocalVolume(massFile, fileName);

        Volumes[volume] = new LocalVolumeMetaData(null);
        VolumesChanged?.Invoke(Volumes);

        return volume;
    }

    public LocalVolume? GetVolumeByName(string fileName) =>
        Volumes.Keys.FirstOrDefault(vol => vol.VolumeName == fileName);

    public LocalVolume OpenLocalVolume(string localPath, string? fileName = null)
    {
        var massFile = new MassFile();

        // Load the mass file from the local path
        using (var fileStream = FileUtils.OpenFile(localPath)) massFile.Load(fileStream);

        var volume = new LocalVolume(massFile, fileName ?? Path.GetFileNameWithoutExtension(localPath));

        Volumes[volume] = new LocalVolumeMetaData(localPath);
        VolumesChanged?.Invoke(Volumes);

        return volume;
    }

    public bool Save(LocalVolume localVolume, Func<string?>? pathProvider = null)
    {
        var meta = Volumes[localVolume];

        if (meta.LocalPath != null) pathProvider = () => meta.LocalPath;
        else if (pathProvider == null)
        {
            LogUtils.PrintWarning(Tag, "保存失败！既无LocalPath，又无PathProvider");
            return false;
        }

        return SaveAs(localVolume, pathProvider);
    }

    public bool SaveAs(LocalVolume localVolume, Func<string?> localPathProvider)
    {
        var actualPath = localPathProvider();

        if (actualPath is null)
        {
            LogUtils.PrintWarning("另存为失败！没有提供路径！");
            return false;
        }

        try
        {
            using (var fileStream = FileUtils.OpenFile(actualPath, true, true)) localVolume.Save(fileStream);
        }
        catch (Exception ex)
        {
            LogUtils.PrintError("另存为失败！", ex);
            return false;
        }

        if (Volumes[localVolume].LocalPath == null) Volumes[localVolume] = new LocalVolumeMetaData(actualPath);

        return true;
    }

    public void Close(LocalVolume localVolume)
    {
        Volumes.Remove(localVolume);

        VolumesChanged?.Invoke(Volumes);
    }
}

public record LocalVolumeMetaData(string? LocalPath);

public class LocalVolume
{
    internal readonly MassFile MassFile;

    public enum AddType
    {
        Add,
        Replace
    }

    internal LocalVolume(MassFile massFile, string fileName)
    {
        MassFile = massFile;
        VolumeName = fileName;
    }

    public int Count => MassFile.IdStrPairs.Count;

    public Dictionary<string, int> IdStrPairs => MassFile.IdStrPairs;

    public string VolumeName { get; internal set; }

    public bool Unsaved { get; internal set; }

    public int Add(object value)
    {
        Unsaved = true;

        return MassFile.Add(value);
    }

    public AddType Add(string name, object value)
    {
        Unsaved = true;

        var val = MassFile.Add(name, value);

        return val == MassFile.AddType.Add ? AddType.Add : AddType.Replace;
    }

    public void Clear()
    {
        Unsaved = true;

        throw new NotImplementedException();
    }

    public bool Remove(string name)
    {
        Unsaved = true;

        return IdStrPairs.Remove(name);
    }

    public LocalVolumeFileModel GetWrappedFile(int id, string? name = null)
    {
        var file = MassFile.Get<object>(id);

        // TODO:检测不了是否Parse成功

        return new LocalVolumeFileModel(id, VolumeName,
            name ?? MassFile.IdStrPairs.FirstOrDefault(pair => pair.Value == id).Key,
            MassFile.ItemAdaptions[MassFile.ItemBlobs[id].AdaptionId].DataJavaClz
            , file, true);
    }

    public object this[string key] => GetWrappedFile(IdStrPairs[key], key);

    public object this[int key] => GetWrappedFile(key);

    public void Save(FileStream fs) => MassFile.Save(fs);
}

public record LocalVolumeFileModel(
    int Id,
    string FromFile,
    string? Name,
    string? Type,
    object Data,
    bool ParseSuccess
);