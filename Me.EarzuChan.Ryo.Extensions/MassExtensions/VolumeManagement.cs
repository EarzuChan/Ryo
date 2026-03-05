using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Extensions.MassExtensions;

public class LocalVolumeManager
{
    private const string Tag = "LocalVolumeManager";

    public readonly Dictionary<LocalVolume, LocalVolumeMetaData> Volumes = [];

    public event Action<Dictionary<LocalVolume, LocalVolumeMetaData>>? VolumesChanged;

    public event Action<string, string, string>? VolumeItemRenamed;

    public event Action<string, int>? VolumeItemDeleted;

    public event Action<string, int[]>? VolumeIdsRemapped;

    public LocalVolume CreateNewVolume(string fileName)
    {
        var massFile = new MassFile();

        var volume = new LocalVolume(massFile, fileName, this);

        Volumes[volume] = new LocalVolumeMetaData(null);
        NotifyVolumesChanged();

        return volume;
    }

    public LocalVolume? GetVolumeByName(string fileName) =>
        Volumes.Keys.FirstOrDefault(vol => vol.VolumeName == fileName);

    public LocalVolume OpenLocalVolume(string localPath, string? fileName = null)
    {
        // 先检查是否已经打开，抛出
        if (Volumes.Values.Any(meta => meta.LocalPath == localPath))
        {
            var err = new FileLoadException("文件已经打开");
            LogUtils.PrintError(Tag, err);
            throw err;
        }

        var massFile = new MassFile();

        using (var fileStream = FileUtils.OpenFile(localPath)) massFile.Load(fileStream);

        var volume = new LocalVolume(massFile, fileName ?? Path.GetFileNameWithoutExtension(localPath), this);

        Volumes[volume] = new LocalVolumeMetaData(localPath);
        NotifyVolumesChanged();

        return volume;
    }

    public bool Save(LocalVolume localVolume, Func<string?>? pathProvider = null, bool mustUsePathProvider = false)
    {
        LogUtils.PrintInfo(Tag, "正在保存文件：" + localVolume.VolumeName);

        var meta = Volumes[localVolume];

        if (meta.LocalPath != null && !mustUsePathProvider) pathProvider = () => meta.LocalPath;
        else if (pathProvider == null)
        {
            LogUtils.PrintWarning(Tag, "欲保存失败！既无LocalPath，又无PathProvider");
            return false;
        }

        return InternalSave(localVolume, pathProvider);
    }

    private bool InternalSave(LocalVolume localVolume, Func<string?> localPathProvider)
    {
        var actualPath = localPathProvider();

        if (actualPath is null)
        {
            LogUtils.PrintWarning("保存失败！没有提供路径！");
            return false;
        }

        try
        {
            using var fileStream = FileUtils.OpenFile(actualPath, true, true);
            localVolume.Save(fileStream);
        }
        catch (Exception ex)
        {
            LogUtils.PrintError("另存为失败！", ex);
            return false;
        }

        Volumes[localVolume] = new LocalVolumeMetaData(actualPath);

        return true;
    }

    public void Close(LocalVolume localVolume)
    {
        Volumes.Remove(localVolume);

        NotifyVolumesChanged();
    }

    // 虽然通知卷更改，但是前端的标签页唯一可信源这一块导致不同步
    internal void NotifyVolumesChanged() => VolumesChanged?.Invoke(Volumes);

    internal void NotifyVolumeItemRenamed(string volumeName, string oldName, string newName) =>
        VolumeItemRenamed?.Invoke(volumeName, oldName, newName);

    internal void NotifyVolumeItemDeleted(string volumeName, int id) =>
        VolumeItemDeleted?.Invoke(volumeName, id);

    internal void NotifyVolumeIdsRemapped(string volumeName, int[] idMap) =>
        VolumeIdsRemapped?.Invoke(volumeName, idMap);
}

public record LocalVolumeMetaData(string? LocalPath);

public class LocalVolume
{
    private readonly MassFile _massFile;
    private readonly LocalVolumeManager _manager;

    public enum AddType
    {
        Add,
        AbandonOld
    }

    internal LocalVolume(MassFile massFile, string fileName, LocalVolumeManager manager)
    {
        _massFile = massFile;
        _manager = manager;
        VolumeName = fileName;

        massFile.OnItemIdsRemap += idMap => _manager.NotifyVolumeIdsRemapped(VolumeName, idMap);
    }

    public int Count => _massFile.IdStrPairs.Count;

    public Dictionary<string, int> IdStrPairs => _massFile.IdStrPairs;

    public string VolumeName { get; internal set; }

    public bool Unsaved { get; internal set; }

    public int Revision { get; private set; }

    private void Touch()
    {
        Unsaved = true;
        Revision++;
    }

    public int Add(object value)
    {
        var ret = _massFile.Add(value);
        Touch();

        _manager.NotifyVolumesChanged();
        return ret;
    }

    public void GiveName(int id, string name)
    {
        _massFile.GiveName(id, name);
        Touch();

        _manager.NotifyVolumesChanged();
    }

    public void Set(int id, object value, bool gc = false)
    {
        _massFile.Set(id, value);
        Touch();

        if (gc) _massFile.CollectGarbage();

        _manager.NotifyVolumesChanged();
    }

    public AddType Add(string name, object value, bool gc = false)
    {
        var val = _massFile.Add(name, value) == MassFile.AddType.Add ? AddType.Add : AddType.AbandonOld;
        Touch();

        if (gc) _massFile.CollectGarbage();

        _manager.NotifyVolumesChanged();
        return val;
    }


    // 感觉这是没必要的，你要清空你大可以新建一个卷
    public void Clear()
    {
        throw new NotImplementedException();

        // Perform

        Touch();

        _manager.NotifyVolumesChanged();
    }

    public void CollectGarbage()
    {
        _massFile.CollectGarbage();
        Touch();

        _manager.NotifyVolumesChanged();
    }

    public bool Remove(string name, bool gc = false)
    {
        var id = IdStrPairs.GetValueOrDefault(name, -1);

        if (id == -1) return false;

        var state = _massFile.Remove(id);
        if (state) Touch();

        if (state) _manager.NotifyVolumeItemDeleted(VolumeName, id);

        if (gc) _massFile.CollectGarbage();

        _manager.NotifyVolumesChanged();
        return state;
    }

    public bool Remove(int id, bool gc = false)
    {
        var state = _massFile.Remove(id);
        if (state) Touch();

        if (state) _manager.NotifyVolumeItemDeleted(VolumeName, id);

        if (gc) _massFile.CollectGarbage();

        _manager.NotifyVolumesChanged();
        return state;
    }

    public LocalVolumeItemModel GetWrappedItem(int id, string? name = null)
    {
        var result = _massFile.Get<object>(id);

        var jwc = _massFile.CodecBindings[_massFile.ItemBlobs[id].CodecBindingId].DataJavaClz;

        return new LocalVolumeItemModel(id, VolumeName,
            name ?? _massFile.IdStrPairs.FirstOrDefault(pair => pair.Value == id).Key,
            jwc.JavaClassToRyoType()
                .ResolveDataTypeName()
            , result.Data, result.ParseSuccess, Revision);
    }

    public LocalVolumeItemModel this[string key] => GetWrappedItem(IdStrPairs[key], key);

    public LocalVolumeItemModel this[int key] => GetWrappedItem(key);

    public void Save(FileStream fs, bool gc = true)
    {
        if (gc) _massFile.CollectGarbage();
        Unsaved = true;

        _massFile.Save(fs);
        Unsaved = false;

        _manager.NotifyVolumesChanged();
    }

    // 重命名不需要GC
    public void Rename(string oldName, string newName)
    {
        _massFile.Rename(oldName, newName);
        Touch();

        _manager.NotifyVolumeItemRenamed(VolumeName, oldName, newName);
        _manager.NotifyVolumesChanged();
    }
}

public record LocalVolumeItemModel(
    int Id,
    string FromFile,
    string? Name,
    string? DataTypeName,
    object Data,
    bool ParseSuccess,
    int VolumeRevision
);