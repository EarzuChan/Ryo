using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Extensions.MassExtensions;

public class LocalVolumeManager {
    private const string Tag = "LocalVolumeManager";

    public readonly Dictionary<LocalVolume, LocalVolumeMetaData> Volumes = [];

    public event Action<Dictionary<LocalVolume, LocalVolumeMetaData>>? VolumesChanged;

    public event Action<string, string, string>? VolumeItemRenamed;

    public event Action<string, int>? VolumeItemDeleted;

    public event Action<string, int[]>? VolumeIdsRemapped;

    private static string NormalizePath(string path) {
        try {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        } catch {
            return path;
        }
    }

    private static string NewVolumeId() => Guid.NewGuid().ToString("N");

    private static bool IsSamePath(string a, string b) =>
        string.Equals(NormalizePath(a), NormalizePath(b), StringComparison.OrdinalIgnoreCase);

    public LocalVolume CreateNewVolume(string fileName) {
        var massFile = new MassFile();
        var volume = new LocalVolume(massFile, fileName, NewVolumeId(), this);

        Volumes[volume] = new LocalVolumeMetaData(null);
        NotifyVolumesChanged();

        return volume;
    }

    public LocalVolume CloneVolume(LocalVolume sourceVolume, string fileName) {
        var massFile = sourceVolume.CloneMassFile();
        var volume = new LocalVolume(massFile, fileName, NewVolumeId(), this);

        Volumes[volume] = new LocalVolumeMetaData(null);
        NotifyVolumesChanged();

        return volume;
    }

    public LocalVolume? GetVolumeById(string volumeId) => Volumes.Keys.FirstOrDefault(vol => vol.VolumeId == volumeId);

    public LocalVolume? GetVolumeByName(string fileName) => Volumes.Keys.FirstOrDefault(vol => vol.VolumeName == fileName);

    public LocalVolume? GetVolumeByLocalPath(string localPath) {
        var targetPath = NormalizePath(localPath);
        return Volumes.FirstOrDefault(entry =>
            entry.Value.LocalPath != null && IsSamePath(entry.Value.LocalPath, targetPath)
        ).Key;
    }

    public void RenameVolume(string volumeId, string newName) {
        var volume = GetVolumeById(volumeId);
        if (volume is null) return;

        // 卷改名后断开原路径绑定，后续保存必须走 SaveFileDialog。
        Volumes[volume] = new LocalVolumeMetaData(null);
        volume.VolumeName = newName;
        volume.TouchForMetadataChange();
        NotifyVolumesChanged();
    }

    public LocalVolume OpenLocalVolume(string localPath, string? fileName = null) {
        var normalizedPath = NormalizePath(localPath);
        if (GetVolumeByLocalPath(normalizedPath) != null) {
            var err = new InvalidOperationException("文件已经打开");
            LogUtils.PrintError(Tag, err);
            throw err;
        }

        var massFile = new MassFile();
        using (var fileStream = FileUtils.OpenFile(normalizedPath)) massFile.Load(fileStream);

        var volume = new LocalVolume(
            massFile,
            fileName ?? Path.GetFileNameWithoutExtension(normalizedPath),
            NewVolumeId(),
            this
        );

        Volumes[volume] = new LocalVolumeMetaData(normalizedPath);
        NotifyVolumesChanged();

        return volume;
    }

    public LocalVolume OpenLocalVolumeAsCopy(string localPath, string? fileName = null) {
        var normalizedPath = NormalizePath(localPath);
        var massFile = new MassFile();
        using (var fileStream = FileUtils.OpenFile(normalizedPath)) massFile.Load(fileStream);

        var volume = new LocalVolume(
            massFile,
            fileName ?? Path.GetFileNameWithoutExtension(normalizedPath),
            NewVolumeId(),
            this
        );

        // 副本脱离原始路径绑定，后续保存必须走 SaveFileDialog。
        Volumes[volume] = new LocalVolumeMetaData(null);
        NotifyVolumesChanged();

        return volume;
    }

    public bool Save(LocalVolume localVolume, Func<string?>? pathProvider = null, bool mustUsePathProvider = false) {
        LogUtils.PrintInfo(Tag, "正在保存文件：" + localVolume.VolumeName);

        var meta = Volumes[localVolume];

        if (meta.LocalPath != null && !mustUsePathProvider) pathProvider = () => meta.LocalPath;
        else if (pathProvider == null) {
            LogUtils.PrintWarning(Tag, "欲保存失败！既无LocalPath，又无PathProvider");
            return false;
        }

        return InternalSave(localVolume, pathProvider);
    }

    private bool InternalSave(LocalVolume localVolume, Func<string?> localPathProvider) {
        var actualPath = localPathProvider();

        if (actualPath is null) {
            LogUtils.PrintWarning("保存失败！没有提供路径！");
            return false;
        }

        try {
            using var fileStream = FileUtils.OpenFile(actualPath, true, true);
            localVolume.Save(fileStream);
        } catch (Exception ex) {
            LogUtils.PrintError("另存为失败！", ex);
            return false;
        }

        var normalizedPath = NormalizePath(actualPath);
        Volumes[localVolume] = new LocalVolumeMetaData(normalizedPath);
        localVolume.VolumeName = Path.GetFileNameWithoutExtension(normalizedPath);
        NotifyVolumesChanged();

        return true;
    }

    public void Close(LocalVolume localVolume) {
        Volumes.Remove(localVolume);

        NotifyVolumesChanged();
    }

    // 虽然通知卷更改，但是前端的标签页唯一可信源这一块导致不同步
    internal void NotifyVolumesChanged() => VolumesChanged?.Invoke(Volumes);

    internal void NotifyVolumeItemRenamed(string volumeId, string oldName, string newName) => VolumeItemRenamed?.Invoke(volumeId, oldName, newName);

    internal void NotifyVolumeItemDeleted(string volumeId, int id) => VolumeItemDeleted?.Invoke(volumeId, id);

    internal void NotifyVolumeIdsRemapped(string volumeId, int[] idMap) => VolumeIdsRemapped?.Invoke(volumeId, idMap);
}

public record LocalVolumeMetaData(string? LocalPath);

public class LocalVolume {
    private readonly MassFile _massFile;
    private readonly LocalVolumeManager _manager;
    private readonly HashSet<int> _dirtyItemIds = [];

    public enum AddType {
        Add,
        AbandonOld
    }

    internal LocalVolume(MassFile massFile, string fileName, string volumeId, LocalVolumeManager manager) {
        _massFile = massFile;
        _manager = manager;
        VolumeName = fileName;
        VolumeId = volumeId;

        massFile.OnItemIdsRemap += idMap => {
            RemapDirtyItemIds(idMap);
            _manager.NotifyVolumeIdsRemapped(VolumeId, idMap);
        };
    }

    public int Count => _massFile.IdStrPairs.Count;

    public Dictionary<string, int> IdStrPairs => _massFile.IdStrPairs;

    public string VolumeId { get; }

    public string VolumeName { get; internal set; }

    public bool Unsaved { get; internal set; }

    public int Revision { get; private set; }

    public bool IsItemUnsaved(int id) => _dirtyItemIds.Contains(id);

    private void MarkItemDirty(int id) {
        if (id >= 0) _dirtyItemIds.Add(id);
    }

    private void RemapDirtyItemIds(int[] idMap) {
        if (_dirtyItemIds.Count == 0) return;

        var remapped = new HashSet<int>();
        foreach (var oldId in _dirtyItemIds) {
            if (oldId < 0 || oldId >= idMap.Length) continue;
            var newId = idMap[oldId];
            if (newId >= 0) remapped.Add(newId);
        }

        _dirtyItemIds.Clear();
        foreach (var id in remapped) _dirtyItemIds.Add(id);
    }

    private void Touch() {
        Unsaved = true;
        Revision++;
    }

    internal void TouchForMetadataChange() => Touch();

    public int Add(object value) {
        var ret = _massFile.Add(value);
        MarkItemDirty(ret);
        Touch();

        _manager.NotifyVolumesChanged();
        return ret;
    }

    public void GiveName(int id, string name) {
        _massFile.GiveName(id, name);
        MarkItemDirty(id);
        Touch();

        _manager.NotifyVolumesChanged();
    }

    public void Set(int id, object value, bool gc = false) {
        _massFile.Set(id, value);
        MarkItemDirty(id);
        Touch();

        if (gc) _massFile.CollectGarbage();

        _manager.NotifyVolumesChanged();
    }

    public AddType Add(string name, object value, bool gc = false) {
        var idBefore = IdStrPairs.GetValueOrDefault(name, -1);
        var val = _massFile.Add(name, value) == MassFile.AddType.Add ? AddType.Add : AddType.AbandonOld;
        var idAfter = IdStrPairs.GetValueOrDefault(name, idBefore);
        if (idAfter >= 0) MarkItemDirty(idAfter);
        else if (idBefore >= 0) MarkItemDirty(idBefore);
        Touch();

        if (gc) _massFile.CollectGarbage();

        _manager.NotifyVolumesChanged();
        return val;
    }


    // 感觉这是没必要的，你要清空你大可以新建一个卷
    public void Clear() {
        throw new NotImplementedException();

        // Perform

        Touch();

        _manager.NotifyVolumesChanged();
    }

    public void CollectGarbage() {
        _massFile.CollectGarbage();
        
        // GC或会带来改动，标记卷脏
        Touch();

        _manager.NotifyVolumesChanged();
    }

    public bool Remove(string name, bool gc = false) {
        var id = IdStrPairs.GetValueOrDefault(name, -1);

        if (id == -1) return false;

        var state = _massFile.Remove(id);
        if (state) Touch();
        if (state) _dirtyItemIds.Remove(id);

        if (state) _manager.NotifyVolumeItemDeleted(VolumeId, id);

        if (gc) _massFile.CollectGarbage();

        _manager.NotifyVolumesChanged();
        return state;
    }

    public bool Remove(int id, bool gc = false) {
        var state = _massFile.Remove(id);
        if (state) Touch();
        if (state) _dirtyItemIds.Remove(id);

        if (state) _manager.NotifyVolumeItemDeleted(VolumeId, id);

        if (gc) _massFile.CollectGarbage();

        _manager.NotifyVolumesChanged();
        return state;
    }

    public LocalVolumeItemModel GetWrappedItem(int id, string? name = null) {
        var result = _massFile.Get<object>(id);

        var jwc = _massFile.CodecBindings[_massFile.ItemBlobs[id].CodecBindingId].DataJavaClz;

        return new LocalVolumeItemModel(id, VolumeId, VolumeName, name ?? _massFile.IdStrPairs.FirstOrDefault(pair => pair.Value == id).Key,
            jwc.JavaClassToRyoType().ResolveDataTypeName(), result.Data, result.ParseSuccess, Revision
        );
    }

    public LocalVolumeItemModel this[string key] => GetWrappedItem(IdStrPairs[key], key);

    public LocalVolumeItemModel this[int key] => GetWrappedItem(key);

    public void Save(FileStream fs, bool gc = true) {
        if (gc) _massFile.CollectGarbage();
        Unsaved = true;

        _massFile.Save(fs);
        Unsaved = false;
        _dirtyItemIds.Clear();
    }

    // 重命名不需要GC
    public void Rename(string oldName, string newName, bool allowOverwrite = false) {
        if (allowOverwrite && oldName != newName) {
            var conflictId = IdStrPairs.GetValueOrDefault(newName, -1);
            if (conflictId >= 0) Remove(conflictId);
        }

        var id = IdStrPairs.GetValueOrDefault(oldName, -1);
        _massFile.Rename(oldName, newName);
        if (id >= 0) MarkItemDirty(id);
        Touch();

        _manager.NotifyVolumeItemRenamed(VolumeId, oldName, newName);
        _manager.NotifyVolumesChanged();
    }

    internal MassFile CloneMassFile() {
        // Mass.Save 会关闭传入流，故需写入与读取使用不同流。
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.fs");
        try {
            using (var saveStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) {
                _massFile.Save(saveStream);
            }

            using var loadStream = new FileStream(
                tempPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete,
                4096,
                FileOptions.DeleteOnClose
            );
            var clone = new MassFile();
            clone.Load(loadStream);
            return clone;
        } finally {
            try {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            } catch {
                // temp cleanup is best-effort
            }
        }
    }
}

public record LocalVolumeItemModel(
    int Id,
    string FromVolumeId,
    string FromFile,
    string? Name,
    string? DataTypeName,
    object Data,
    bool ParseSuccess,
    int VolumeRevision
);
