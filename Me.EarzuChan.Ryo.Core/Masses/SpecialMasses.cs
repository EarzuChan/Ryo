using System.Data;
using Me.EarzuChan.Ryo.Core.IO;

namespace Me.EarzuChan.Ryo.Core.Masses;

public class MassFile : Mass
{
    public readonly Dictionary<string, int> IdStrPairs = new();

    public MassFile() => ExtendedName = "FileSystem";

    public enum AddType
    {
        Add,
        AbandonOld
    }

    // TIPS：不建议直接使用，可能会有本对象作为人家的子对象时删除本对象造成人家的连带问题。建议是赐名复制（Copy）？
    public void GiveName(int id, string name)
    {
        // 检查 id 是否合法（是否在有效范围内）
        if (id < 0 || id >= ItemBlobs.Count) throw new ArgumentOutOfRangeException(nameof(id), "无效的ID");

        // 检查该 id 是否已经有名字
        if (IdStrPairs.ContainsKey(name) || IdStrPairs.ContainsValue(id)) throw new InvalidOperationException("已有重名项目或该项目已被命名");

        // 给没有名字的项目赐名
        IdStrPairs[name] = id;
    }

    public AddType Add(string token, object obj)
    {
        // 不要直接Set老Id为新内容
        
        // 1. 解绑旧名字
        var isAbandonOld = IdStrPairs.Remove(token);

        // 2. 添加新对象（生成新ID）
        var newId = Add(obj);

        // 3. 至于 oldId，如果没有其他对象引用它，下次 GC 就会被回收。
        
        // 新增映射
        IdStrPairs.Add(token, newId);
        
        return isAbandonOld ? AddType.AbandonOld : AddType.Add;
    }

    public void Rename(string oldToken, string newToken)
    {
        // 0. 基础参数校验
        if (string.IsNullOrEmpty(oldToken)) throw new ArgumentNullException(nameof(oldToken));
        if (string.IsNullOrEmpty(newToken)) throw new ArgumentNullException(nameof(newToken));

        // 如果新旧名字完全一样，无需执行任何操作，直接返回
        if (oldToken == newToken) return;

        // 1. 检查旧名字是否存在，并获取对应的 ID
        // 使用 TryGetValue 比 ContainsKey + 索引器更高效且线程安全（针对读取）
        if (!IdStrPairs.TryGetValue(oldToken, out var currentId)) throw new KeyNotFoundException($"重命名失败：找不到名为 '{oldToken}' 的项。");

        // 2. 检查新名字是否冲突
        if (IdStrPairs.ContainsKey(newToken)) throw new ArgumentException($"重命名失败：名称 '{newToken}' 已经存在，无法重复使用。");

        // 3. 执行重命名
        //先移除旧的 Key
        IdStrPairs.Remove(oldToken);

        // 再添加新的 Key，并保留原来的 ID
        IdStrPairs.Add(newToken, currentId);
    }

    public bool Remove(string token)
    {
        if (!IdStrPairs.TryGetValue(token, out var id)) return false;

        // 标记垃圾
        Remove(id);

        return true;
    }

    protected override void OnRemove(int id)
    {
        // 警告：这是 O(N) 操作，大文件慎用
        var targetKey = IdStrPairs.FirstOrDefault(x => x.Value == id).Key;

        if (targetKey != null) IdStrPairs.Remove(targetKey);
    }

    // 返回活对象给GC
    protected override IEnumerable<int> GetRootIds() => IdStrPairs.Values;

    // GC整理的回调
    protected override void UpdateRoots(int[] idMap)
    {
        // 必须转为 List 遍历，因为要在遍历中修改字典的值
        var keys = IdStrPairs.Keys.ToList();

        foreach (var key in keys)
        {
            var oldId = IdStrPairs[key];

            // 查表获取新 ID
            // 这里的逻辑是：因为 ID 对应的对象被移动并重组了，ID 值变小了
            var newId = idMap[oldId];

            // 理论上 GC 算法保证 Root 引用的对象即便移动也一定存活（newId != -1）
            if (newId != -1) IdStrPairs[key] = newId;
            // 防御性编程：如果不幸由于某种逻辑错误导致 Root 死了，移除条目
            else IdStrPairs.Remove(key);
        }
    }

    public void Copy(string sourceToken, string destToken, bool allowOverride = false)
    {
        // 1. 基础校验
        if (string.IsNullOrEmpty(sourceToken)) throw new ArgumentNullException(nameof(sourceToken));
        if (string.IsNullOrEmpty(destToken)) throw new ArgumentNullException(nameof(destToken));
        if (sourceToken == destToken) return; // 原地复制，无需操作

        if (!IdStrPairs.TryGetValue(sourceToken, out var sourceId)) throw new KeyNotFoundException($"找不到源项目：{sourceToken}");

        // 2. 目标检测
        if (IdStrPairs.ContainsKey(destToken))
        {
            if (!allowOverride) throw new DuplicateNameException($"同名项目已存在: {destToken}");
            
            // 如果覆盖，先删除旧的目标（逻辑删除）
            Remove(destToken);
        }

        // 3. 执行核心复制逻辑 (Deep Copy)
        // 这会在堆内存中产生一棵全新的对象树
        var newId = base.Copy(sourceId);

        // 4. 绑定新名字
        // 注意：如果是 overwrite 模式，Remove(destToken) 已经移除了 Key，这里是安全的 Add
        // 如果 Remove 只是标记 ID 删除但没移除 Key（取决于你的 Remove 实现），这里应该用索引器
        IdStrPairs[destToken] = newId; 
    }

    protected override void AfterLoadingIndex(RyoReader reader)
    {
        var idStrPairCount = reader.ReadInt();
        IdStrPairs.Clear();
        for (var i = 0; i < idStrPairCount; i++)
        {
            var str = reader.ReadString();
            var id = reader.ReadInt();
            IdStrPairs[str] = id;
        }
    }

    protected override void AfterSavingIndex(RyoWriter writer)
    {
        writer.WriteInt(IdStrPairs.Count);
        foreach (var i in IdStrPairs)
        {
            writer.WrintString(i.Key);
            writer.WriteInt(i.Value);
        }
    }
}

public class TextureFile : Mass
{
    // 成员：外层是贴图组，内层是具体的帧ID
    public readonly List<List<int>> ImageIDsArray = [];

    public TextureFile() => ExtendedName = "TextureFile";

    // 核心适配：GC 联动

    // 1. 告诉 GC 哪些对象是活的 (Roots)
    // 必须遍历所有列表中的所有 ID
    protected override IEnumerable<int> GetRootIds() => ImageIDsArray.SelectMany(subList => subList);

    // 2. GC 整理内存后，修正这里持有的 ID
    protected override void UpdateRoots(int[] idMap)
    {
        // 遍历每一组
        foreach (var subList in ImageIDsArray)
        {
            // 倒序遍历是为了方便移除死对象（如果有的话，理论上Root不会死）
            for (var i = subList.Count - 1; i >= 0; i--)
            {
                var oldId = subList[i];

                // 查表换新ID
                // 这里的逻辑严密性在于：idMap 覆盖了所有旧 ID 范围
                if (oldId < 0 || oldId >= idMap.Length) continue;

                var newId = idMap[oldId];

                if (newId != -1) subList[i] = newId; // 活对象，更新 ID
                else subList.RemoveAt(i); // 死对象（防御性编程），移除引用
            }
        }
    }

    protected override void AfterLoadingIndex(RyoReader reader)
    {
        var imageCount = reader.ReadInt();
        ImageIDsArray.Clear(); // 清空，防止重读叠加

        for (var i = 0; i < imageCount; i++)
        {
            var subList = new List<int>();
            var clipCount = reader.ReadInt();

            for (var j = 0; j < clipCount; j++) subList.Add(reader.ReadInt());

            ImageIDsArray.Add(subList);
        }
    }

    protected override void AfterSavingIndex(RyoWriter writer)
    {
        writer.WriteInt(ImageIDsArray.Count);
        foreach (var sublist in ImageIDsArray)
        {
            writer.WriteInt(sublist.Count);

            foreach (var item in sublist) writer.WriteInt(item);
        }
    }

    protected override void OnRemove(int id)
    {
        // 遍历外层列表

        // 使用 RemoveAll 是最高效的做法
        // 它在底层只会进行一次数组内存搬运（Array.Copy），避免了 for 循环 remove 的多次搬运开销
        foreach (var subList in ImageIDsArray) subList.RemoveAll(imgId => imgId == id);
    }

    // 业务操作封装 (利用新特性的语法糖)

    /// <summary>
    /// 添加一个新的贴图组，包含若干帧对象
    /// </summary>
    public void AddTextureGroup(params object[] frames)
    {
        var list = frames.Select(Add).ToList();
        ImageIDsArray.Add(list);
    }

    /// <summary>
    /// 向指定贴图组追加一帧
    /// </summary>
    public void AppendFrame(int groupIndex, object frameObj)
    {
        if (groupIndex < 0 || groupIndex >= ImageIDsArray.Count) throw new IndexOutOfRangeException();

        // 新增对象
        var newId = Add(frameObj);

        // 引用挂载
        ImageIDsArray[groupIndex].Add(newId);
    }

    /// <summary>
    /// 替换某一帧（COW 模式）
    /// </summary>
    public void ReplaceFrame(int groupIndex, int frameIndexInGroup, object newFrameObj)
    {
        var subList = ImageIDsArray[groupIndex];
        var oldId = subList[frameIndexInGroup];

        // 1. 标记旧对象逻辑移除 (待 GC)
        Remove(oldId);

        // 2. 添加新对象
        var newId = Add(newFrameObj);

        // 3. 更新引用
        subList[frameIndexInGroup] = newId;
    }

    /// <summary>
    /// 移除整个贴图组
    /// </summary>
    public void RemoveTextureGroup(int groupIndex)
    {
        var subList = ImageIDsArray[groupIndex];

        // 显式移除所有帧的逻辑引用，加速 GC 识别
        foreach (var id in subList) Remove(id);

        // 移除列表引用
        ImageIDsArray.RemoveAt(groupIndex);
    }
}