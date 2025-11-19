using Me.EarzuChan.Ryo.Core.Adaptations;
using Me.EarzuChan.Ryo.Core.IO;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Exceptions;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Core.Masses;

public interface IMass
{
    void Load(FileStream fileStream);

    void Save(FileStream fileStream, bool allowDeflate = true);

    GetResult<T> Get<T>(int id);

    int Add<T>(T obj);

    bool Remove(int id);

    void Set<T>(int id, T obj);
}

public class GetResult<T>(T data, bool parseSuccess, string? errorMessage = null)
{
    public T Data { get; } = data;

    public bool ParseSuccess { get; } = parseSuccess;

    public string? ErrorMessage { get; } = errorMessage;
}

public abstract class Mass : IMass
{
    protected string ExtendedName = "Mass";

    private RyoBuffer _workingBuffer = new();

    private int _savedId;
    private int _currentStickyMetadataPtr; // 当前读取到的元数据绝对索引
    private int _currentStickyMetadataEnd; // 当前对象的元数据结束边界

    private Dictionary<int, object>? _savedItems;

    // [新增] 处理队列，用于解决非连续 ID 的递归处理问题
    private List<int>? _processingQueue;

    public class ItemBlob(int adaptionId, int stickyOffset, int stickyCount, byte[]? data)
    {
        public int AdaptionId = adaptionId;

        // [新增] 内存态：元数据在 StickyMetaDatas 中的起始位置
        public int StickyOffset = stickyOffset;

        // [新增] 内存态：元数据长度
        public int StickyCount = stickyCount;

        public byte[]? Data = data;

        // 构造函数调整，兼容旧逻辑的同时初始化新字段
    }

    public class ItemAdaption(string? dataJavaClz, string? adapterFactoryJavaClz)
    {
        public readonly string? DataJavaClz = dataJavaClz;
        public readonly string? AdapterJavaClz = adapterFactoryJavaClz;
    }

    public readonly List<ItemBlob> ItemBlobs = [];
    public readonly List<ItemAdaption> ItemAdaptions = [];
    public readonly List<int> StickyMetaDatas = []; // 现在这是一个允许碎片的堆

    public int FindAdaptionIdForDataRyoType(RyoType ryoType)
    {
        var javaClz = ryoType.ToJavaClass()!;
        var result = ItemAdaptions.Find(a => a.DataJavaClz == javaClz);
        if (result != null) return ItemAdaptions.IndexOf(result);

        var adapterRyoType = ryoType.DataRyoTypeFindAdapterRyoType() ?? throw new FormatException("该类型没有可用的适配器：" + ryoType);

        var itemAdaption = new ItemAdaption(javaClz, adapterRyoType.ToJavaClass()!);
        ItemAdaptions.Add(itemAdaption);
        return ItemAdaptions.IndexOf(itemAdaption);
    }

    public int Add<T>(T obj)
    {
        // 复用 Set 逻辑，ID 为 Count 即为追加
        var id = ItemBlobs.Count;

        // 占位，防止递归死循环
        ItemBlobs.Add(new ItemBlob(0, 0, 0, []));

        SetInternal(id, obj);

        return id;
    }

    public void Set<T>(int id, T obj)
    {
        if (id < 0 || id >= ItemBlobs.Count) throw new IndexOutOfRangeException(); // 不可空Set

        SetInternal(id, obj);
    }

    private void SetInternal<T>(int id, T obj)
    {
        // 递归分支
        // 这里的异常会直接抛出，由栈底的 Root Case 捕获并处理回滚
        if (_savedItems != null)
        {
            if (obj == null) throw new NullReferenceException("Object cannot be null");
            if (_savedItems.TryAdd(id, obj)) _processingQueue!.Add(id);
            return;
        }

        // 根调用分支：引入事务保护

        // [快照] 记录操作前的状态，用于回滚
        // 注意：如果是 Add 操作调用此处，id 对应的占位符此时已经存在于 ItemBlobs 中，
        // 所以 undoBlobStart 实际上包含了当前这个 root blob。
        var undoBlobStart = ItemBlobs.Count;
        var undoStickyStart = StickyMetaDatas.Count;

        try
        {
            if (obj == null) throw new NullReferenceException("Object cannot be null");

            _savedItems = new() { { id, obj } };
            _processingQueue = new() { id };

            var queueIndex = 0;
            while (queueIndex < _processingQueue.Count)
            {
                var currentId = _processingQueue[queueIndex];
                var nowObj = _savedItems[currentId];

                // 1. 准备适配器
                var dataRyoType = nowObj.GetType().ToRyoType();
                var adaptionId = FindAdaptionIdForDataRyoType(dataRyoType);
                var adaption = ItemAdaptions[adaptionId];
                var adapter = AdaptationUtils.CreateAdapter(adaption.AdapterJavaClz.JavaClassToRyoType(), dataRyoType);

                // 2. 序列化（关键点：这里会向 StickyMetaDatas 追加数据，并可能递归调用 SetInternal 向 ItemBlobs 追加子项）
                var stickyStart = StickyMetaDatas.Count;

                using var writer = new RyoWriter(new MemoryStream());
                adapter.To(nowObj, this, writer); // <--- 可能发生异常

                writer.PositionToZero();

                var stickyEnd = StickyMetaDatas.Count;
                var stickyCount = stickyEnd - stickyStart;
                var data = new RyoReader(writer).ReadAllBytes();

                // 3. 提交数据到 Blob
                // 注意：ItemBlobs[currentId] 必定已存在（要么是 root id，要么是递归时 Add 的占位符）
                ItemBlobs[currentId] = new ItemBlob(adaptionId, stickyStart, stickyCount, data);

                queueIndex++;
            }

            // 成功完成，清理上下文
            _savedItems = null;
            _processingQueue = null;
        }
        catch (Exception ex)
        {
            // 回滚逻辑

            // 1. 确保上下文清理，防止污染下一次调用
            _savedItems = null;
            _processingQueue = null;

            // 2. 回滚元数据堆 (StickyMetaDatas)
            // 移除本次操作中追加的所有元数据
            if (StickyMetaDatas.Count > undoStickyStart)
                StickyMetaDatas.RemoveRange(undoStickyStart, StickyMetaDatas.Count - undoStickyStart);

            // 3. 回滚对象表 (ItemBlobs)
            // 移除本次递归过程中产生的所有**新**子对象占位符
            // undoBlobStart 是进入方法时的数量。如果我们在处理 id=5，且它是 Add 进来的，此时 ItemBlobs.Count 可能是 6。
            // 如果递归加了 3 个子项，Count 变成 9。我们要把后面 3 个砍掉。
            if (ItemBlobs.Count > undoBlobStart) ItemBlobs.RemoveRange(undoBlobStart, ItemBlobs.Count - undoBlobStart);

            // 4. 标记根对象损坏 (Tombstone)
            // 无论是 Add 还是 Set，当前操作失败意味着 id 指向的对象数据不完整或已污染。
            // 将其 Data 置空，视为逻辑删除，避免 Read 时解析错误数据。
            if (id >= 0 && id < ItemBlobs.Count)
            {
                var blob = ItemBlobs[id];
                blob.Data = null;
                blob.StickyCount = 0;
                blob.AdaptionId = -1; // 重置类型引用
            }

            // 重新抛出异常通知上层
            throw new RyoException($"设置对象失败 (ID: {id})，已回滚并标记为损坏。错误: {ex.Message}", ex);
        }
    }

    public GetResult<T> Get<T>(int id)
    {
        var parseSuccess = true;
        string? errorMessage = null;

        if (id < 0 || id >= ItemBlobs.Count) throw new IndexOutOfRangeException();
        var itemBlob = ItemBlobs[id];

        // 已经被逻辑删除
        if (itemBlob.Data == null) throw new InvalidOperationException($"对象已被删除：{id}"); // 就是这个，子项删了夫项寄了

        var itemAdaption = ItemAdaptions[itemBlob.AdaptionId];
        var dataRyoType = itemAdaption.DataJavaClz.JavaClassToRyoType();

        IAdapter adapter;
        try
        {
            adapter = AdaptationUtils.CreateAdapter(itemAdaption.AdapterJavaClz.JavaClassToRyoType(), dataRyoType);
        }
        catch (Exception ex)
        {
            adapter = new ReadmRawBytesAdapter();
            parseSuccess = false;
            errorMessage = $"{id}的适配器创建失败，回退到RawBytesAdapter：" + ex.Message;
            LogUtils.PrintWarning(errorMessage);
        }

        // 保存上下文 (栈保存)
        var prevBuffer = _workingBuffer;
        var prevStickyPtr = _currentStickyMetadataPtr;
        var prevStickyEnd = _currentStickyMetadataEnd;
        var prevId = _savedId;

        // 切换上下文
        _savedId = id;
        _currentStickyMetadataPtr = itemBlob.StickyOffset; // 指向该对象在堆中的起始元数据
        _currentStickyMetadataEnd = itemBlob.StickyOffset + itemBlob.StickyCount; // 设定边界
        _workingBuffer.Buffer = itemBlob.Data;

        var item = (T)adapter.From(this, _workingBuffer.Buffer, dataRyoType);

        // 恢复上下文
        _workingBuffer = prevBuffer;
        _savedId = prevId;
        _currentStickyMetadataPtr = prevStickyPtr;
        _currentStickyMetadataEnd = prevStickyEnd;

        return new(item, parseSuccess, errorMessage);
    }

    // 适配器（序列化器）用的读子方法
    public T? Read<T>()
    {
        // 边界检查：如果读针已经到了当前对象的元数据末尾
        if (_currentStickyMetadataPtr >= _currentStickyMetadataEnd) throw new InvalidDataException("尝试读取超出对象边界的元数据");

        // 获取元数据
        var metaData = StickyMetaDatas[_currentStickyMetadataPtr];

        // 指针后移
        _currentStickyMetadataPtr++;

        var subitemId = metaData >> 2;
        var subitemType = metaData & 3;

        return (subitemType) switch
        {
            0 => default,

            3 => GetSubitemSafely<T>(subitemId),

            _ => throw new NotSupportedException($"子项目类型不支持：{subitemType}"),
        };
    }

    // 适配器（序列化器）用的写子方法
    public void Write<T>(T obj)
    {
        // 保持原样，但注意 Add 现在会使用新的内存布局
        int newStickyMetaData;

        if (obj == null) newStickyMetaData = 0;
        else if (obj.GetType().ToRyoType().IsJavaPrimitiveType) throw new NotImplementedException();
        else newStickyMetaData = (Add(obj) << 2) | 3;

        StickyMetaDatas.Add(newStickyMetaData); // 这里的 Add 是在 Adapter.To 内部调用的，对应 SetInternal 里的流程
    }

    // CHECK：虽然这些可能不是本方法的范畴，但是：如果父是数组或者别的啥，子对象（即成员）为Null，不若无此成员；父需要非空类型，可能需要给初始化的空对象。这些可能需要改适配器（序列化器）逻辑
    private T? GetSubitemSafely<T>(int id)
    {
        if (id < 0 || id >= ItemBlobs.Count) return default; // 越界当作 null，本应不会发生，防御性

        // [关键] 已删除对象返回 null，不抛异常
        var blob = ItemBlobs[id];
        if (blob.Data == null)
        {
            LogUtils.PrintWarning($"子对象已被删除：{id}");
            return default;
        }

        try
        {
            var result = Get<T>(id);
            if (result.ParseSuccess) return result.Data;

            LogUtils.PrintWarning($"子对象{id}解析失败: {result.ErrorMessage}");
            return default;
        }
        catch (Exception ex)
        {
            // 捕获所有反序列化内部错误，防止炸掉父对象
            LogUtils.PrintWarning($"读取子对象{id}发生未捕获异常: {ex.Message}");
            return default;
        }
    }

    public bool Remove(int id)
    {
        if (id < 0 || id >= ItemBlobs.Count) return false;

        // 关键：将 Data 置空

        // 在 CollectGarbage 的 Mark 阶段，遇到 Data == null 的对象会停止追踪其子项
        // 从而产生级联的垃圾回收效果
        ItemBlobs[id].Data = null;
        ItemBlobs[id].StickyCount = 0;
        ItemBlobs[id].AdaptionId = -1;

        OnRemove(id);

        return true;
    }

    protected virtual void OnRemove(int id)
    {
    }

    // 虚方法：获取所有根对象ID，由子类实现
    protected virtual IEnumerable<int> GetRootIds() => [];

    public void CollectGarbage()
    {
        // =================================================
        // 阶段 1: 标记活对象 (Mark Alive Objects)
        // =================================================
        HashSet<int> aliveIds = [];
        Queue<int> traceQueue = new();

        // 从根节点开始
        foreach (var rootId in GetRootIds())
        {
            if (aliveIds.Contains(rootId) || rootId >= ItemBlobs.Count || ItemBlobs[rootId].Data == null) continue;
            aliveIds.Add(rootId);
            traceQueue.Enqueue(rootId);
        }

        // 广度优先遍历
        while (traceQueue.Count > 0)
        {
            var currentId = traceQueue.Dequeue();
            var blob = ItemBlobs[currentId];

            for (var i = blob.StickyOffset; i < blob.StickyOffset + blob.StickyCount; i++)
            {
                var meta = StickyMetaDatas[i];
                if ((meta & 3) != 3) continue; // 只关心对象引用

                var childId = meta >> 2;
                if (aliveIds.Contains(childId) || childId >= ItemBlobs.Count || ItemBlobs[childId].Data == null) continue;

                aliveIds.Add(childId);
                traceQueue.Enqueue(childId);
            }
        }

        // =================================================
        // 阶段 2: [新增] 标记活适配项并构建映射 (Mark & Map Adaptions)
        // =================================================
        // 只有活对象引用的适配项才是活适配项
        HashSet<int> usedAdaptionIds = [];
        foreach (var id in aliveIds) usedAdaptionIds.Add(ItemBlobs[id].AdaptionId);

        // 构建适配项的新旧映射表 (OldAdaptionId -> NewAdaptionId)
        var adaptionIdMap = new int[ItemAdaptions.Count];
        List<ItemAdaption> newAdaptions = [];
        var newAdaptionCounter = 0;

        for (var i = 0; i < ItemAdaptions.Count; i++)
        {
            if (usedAdaptionIds.Contains(i))
            {
                adaptionIdMap[i] = newAdaptionCounter++;
                newAdaptions.Add(ItemAdaptions[i]);
            }
            else adaptionIdMap[i] = -1; // 该类型已死，将被丢弃
        }

        // =================================================
        // 阶段 3: 计划对象重映射 (Plan Object Remap)
        // =================================================
        // oldObjectId -> newObjectId
        var objectIdMap = new int[ItemBlobs.Count];
        List<ItemBlob> newBlobs = [];
        List<int> newStickies = [];
        var newIdCounter = 0;

        for (var oldId = 0; oldId < ItemBlobs.Count; oldId++)
        {
            if (aliveIds.Contains(oldId)) objectIdMap[oldId] = newIdCounter++;
            else objectIdMap[oldId] = -1; // Dead
        }

        // =================================================
        // 阶段 4: 压缩与修正 (Compact & Relocate)
        // =================================================
        for (var oldId = 0; oldId < ItemBlobs.Count; oldId++)
        {
            if (objectIdMap[oldId] == -1) continue;

            var oldBlob = ItemBlobs[oldId];

            // 修正1：处理 StickyMeta (子对象引用 ID 修正)
            var newStickyStart = newStickies.Count;
            for (var k = oldBlob.StickyOffset; k < oldBlob.StickyOffset + oldBlob.StickyCount; k++)
            {
                var oldMeta = StickyMetaDatas[k];
                var newMeta = oldMeta;

                if ((oldMeta & 3) == 3)
                {
                    var oldChildId = oldMeta >> 2;
                    if (oldChildId < objectIdMap.Length && objectIdMap[oldChildId] != -1)
                        newMeta = (objectIdMap[oldChildId] << 2) | 3;
                    else newMeta = 0; // 引用了死对象（理论不应发生），置空
                }

                newStickies.Add(newMeta);
            }

            // 修正2：[关键] 使用映射后的新 AdaptionId
            int newAdaptionId = adaptionIdMap[oldBlob.AdaptionId];

            // 防御性检查：如果 newAdaptionId 是 -1，说明逻辑有严重漏洞（活对象引用了死类型）
            if (newAdaptionId == -1) throw new InvalidOperationException("Alive object references dead adaption type.");

            newBlobs.Add(new ItemBlob(newAdaptionId, newStickyStart, oldBlob.StickyCount, oldBlob.Data));
        }

        // =================================================
        // 阶段 5: 应用变更 (Apply)
        // =================================================
        // 替换对象表
        ItemBlobs.Clear();
        ItemBlobs.AddRange(newBlobs);

        // 替换元数据堆
        StickyMetaDatas.Clear();
        StickyMetaDatas.AddRange(newStickies);

        // [新增] 替换适配项表
        ItemAdaptions.Clear();
        ItemAdaptions.AddRange(newAdaptions);

        // 通知子类更新根引用
        UpdateRoots(objectIdMap);
    }

    protected virtual void UpdateRoots(int[] idMap)
    {
    }
    
    public int Copy(int oriId)
    {
        if (oriId < 0 || oriId >= ItemBlobs.Count) throw new ArgumentOutOfRangeException(nameof(oriId));
        if (ItemBlobs[oriId].Data == null) throw new InvalidOperationException("无法复制已删除的对象");

        // 事务保护
        var undoBlobStart = ItemBlobs.Count;
        var undoStickyStart = StickyMetaDatas.Count;

        // 1. 准备队列和映射表 (BFS 核心)
        // 映射表: OldId -> NewId
        var idMap = new Dictionary<int, int>();
        var queue = new Queue<int>();

        try
        {
            // 2. 初始化根对象
            // 先创建占位符，确立 NewId
            var newRootId = ItemBlobs.Count;
            ItemBlobs.Add(new ItemBlob(0, 0, 0, [])); // 占位
            
            idMap[oriId] = newRootId;
            queue.Enqueue(oriId);

            // 3. 开始广度优先遍历
            // 这种方式保证了父对象先处理，子对象后处理
            // 从而使得 StickyOffset 随着 ID 的增加而增加
            while (queue.Count > 0)
            {
                var currentOldId = queue.Dequeue();
                var currentNewId = idMap[currentOldId];
                var oldBlob = ItemBlobs[currentOldId];

                // 记录当前新对象的元数据起始位置
                // 因为是 BFS，现在的 Count 一定是堆的最末尾，绝对不会比之前的 offset 小
                var newStickyStart = StickyMetaDatas.Count;

                // 4. 处理元数据
                for (var i = 0; i < oldBlob.StickyCount; i++)
                {
                    var oldMeta = StickyMetaDatas[oldBlob.StickyOffset + i];
                    var type = oldMeta & 3;
                    var newMeta = oldMeta;

                    if (type == 3) // 对象引用
                    {
                        var oldChildId = oldMeta >> 2;

                        // 关键：如果引用的是已删除对象或无效对象，视为 null
                        if (oldChildId < 0 || oldChildId >= ItemBlobs.Count || ItemBlobs[oldChildId].Data == null) newMeta = 0;
                        else
                        {
                            // 检查子对象是否已存在映射
                            if (!idMap.TryGetValue(oldChildId, out var newChildId))
                            {
                                // 新发现的子对象：立即占位并入队
                                newChildId = ItemBlobs.Count;
                                ItemBlobs.Add(new ItemBlob(0, 0, 0, [])); // 占位
                                idMap[oldChildId] = newChildId;
                                queue.Enqueue(oldChildId);
                            }
                            
                            // 更新元数据指向新 ID
                            newMeta = (newChildId << 2) | 3;
                        }
                    }

                    // 立即写入全局堆
                    // BFS 保证了我们此刻写入的一定是当前这一层的数据
                    StickyMetaDatas.Add(newMeta);
                }

                var newStickyEnd = StickyMetaDatas.Count;

                // 5. 复制实体数据
                byte[]? newData = null;
                if (oldBlob.Data != null)
                {
                    newData = new byte[oldBlob.Data.Length];
                    Array.Copy(oldBlob.Data, newData, oldBlob.Data.Length);
                }

                // 6. 提交 Blob (替换占位符)
                ItemBlobs[currentNewId] = new ItemBlob(oldBlob.AdaptionId, newStickyStart, newStickyEnd - newStickyStart, newData);
            }

            return newRootId;
        }
        catch (Exception ex)
        {
            // 回滚逻辑
            if (StickyMetaDatas.Count > undoStickyStart)
                StickyMetaDatas.RemoveRange(undoStickyStart, StickyMetaDatas.Count - undoStickyStart);

            if (ItemBlobs.Count > undoBlobStart) ItemBlobs.RemoveRange(undoBlobStart, ItemBlobs.Count - undoBlobStart);

            throw new RyoException($"复制对象失败 (Source ID: {oriId})，已回滚。错误: {ex.Message}", ex);
        }
    }

    public void Load(FileStream fileStream)
    {
        using var reader = new RyoReader(fileStream);

        // 1. 校验头
        var isCorrectFormat = reader.CheckHasString(ExtendedName);
        if (!isCorrectFormat) throw new FormatException("文件格式校验失败");

        // 2. 读取并解压索引块
        var indexInfo = reader.ReadInt();
        var isDeflated = (indexInfo & 1) != 0;
        var deflateLen = indexInfo >> 1;
        var indexBlob = isDeflated
            ? CompressionUtils.Inflate(reader.ReadBytes(deflateLen), 0, deflateLen)
            : reader.ReadBytes(deflateLen);

        // 临时容器
        List<bool> isItemBlobDeflatedList = [];
        List<int> itemBlobEndPositions = [];
        List<int> itemBlobAdaptionIds = [];
        List<int> fileStickyEndPositions = [];
        var objCount = 0;

        if (indexBlob.Length != 0)
        {
            using var inflatedDataReader = new RyoReader(new MemoryStream(indexBlob));
            objCount = inflatedDataReader.ReadInt();

            // 2.1 读适配项ID与压缩标志
            for (var i = 0; i < objCount; i++)
            {
                var itemBlobInfo = inflatedDataReader.ReadInt();

                // [修正] 检查墓碑标记
                if (itemBlobInfo == -1)
                {
                    itemBlobAdaptionIds.Add(-1); // 标记为已删除
                    isItemBlobDeflatedList.Add(false); // 既然删除了，压不压缩无所谓
                }
                else
                {
                    itemBlobAdaptionIds.Add(itemBlobInfo >> 1);
                    isItemBlobDeflatedList.Add((itemBlobInfo & 1) != 0);
                }
            }

            // 2.2 读数据区结束点
            for (var i = 0; i < objCount; i++) itemBlobEndPositions.Add(inflatedDataReader.ReadInt());

            // 2.3 读粘连索引 (Cumulative Sticky Index)
            for (var i = 0; i < objCount; i++) fileStickyEndPositions.Add(inflatedDataReader.ReadInt());

            // 2.4 读所有元数据 (按文件顺序读入内存，此时内存是连续的)
            StickyMetaDatas.Clear();
            var stickyMetaDataCount = objCount == 0 ? 0 : fileStickyEndPositions.Last();
            for (var i = 0; i < stickyMetaDataCount; i++) StickyMetaDatas.Add(inflatedDataReader.ReadInt());

            // 2.5 读适配器定义
            int regCount = inflatedDataReader.ReadInt();
            ItemAdaptions.Clear();
            for (var i = 0; i < regCount; i++)
            {
                inflatedDataReader.ReadInt(); // ID (冗余，按顺序即可)
                var str1 = inflatedDataReader.ReadString();
                var str2 = inflatedDataReader.ReadString();
                ItemAdaptions.Add(new ItemAdaption(str1, str2));
            }

            // 钩子
            AfterLoadingIndex(inflatedDataReader);
        }

        // 3. 读取实体数据块
        var itemBlobsBuffer = reader.ReadAllBytes();
        var blobsReader = new RyoReader(new MemoryStream(itemBlobsBuffer));
        ItemBlobs.Clear();

        var prevStickyEnd = 0;

        for (var i = 0; i < objCount; i++)
        {
            // 计算实体数据
            int blobStart = i == 0 ? 0 : itemBlobEndPositions[i - 1];
            int blobLength = itemBlobEndPositions[i] - blobStart;

            // [修正] 根据标记决定 Data 是 null 还是 byte[]
            byte[]? itemBlobData;

            // 还原为“逻辑删除”状态
            if (itemBlobAdaptionIds[i] == -1) itemBlobData = null;
            else
            {
                var rawData = blobsReader.ReadBytes(blobLength); // 即使长度为0，这里也会返回 byte[0]

                itemBlobData = isItemBlobDeflatedList[i] ? CompressionUtils.Inflate(rawData, 0, blobLength) : rawData;
            }

            // [关键转换]：将文件的"结束位置"转换为内存的"Offset/Count"
            // 此时 StickyMetaDatas 是刚刚连续读入的，所以 Offset = prevStickyEnd
            int currentStickyEnd = fileStickyEndPositions[i];
            int stickyCount = currentStickyEnd - prevStickyEnd;
            int stickyOffset = prevStickyEnd;

            prevStickyEnd = currentStickyEnd;

            ItemBlobs.Add(new ItemBlob(itemBlobAdaptionIds[i], stickyOffset, stickyCount, itemBlobData));
        }
    }

    public void Save(FileStream fileStream, bool allowDeflate = true)
    {
        using var fileWriter = new RyoWriter(fileStream);
        fileWriter.WriteFixedString(ExtendedName);

        var objCount = ItemBlobs.Count;

        // -------------------------------------------------------
        // 步骤 0：预处理数据 (Pre-calculation)
        // -------------------------------------------------------
        // 我们需要先确定哪些项目要压缩，以及压缩后的长度，才能正确写入索引
        var finalDataChunks = new byte[objCount][];
        var isChunkDeflated = new bool[objCount];

        for (var i = 0; i < objCount; i++)
        {
            var blob = ItemBlobs[i];
            var rawData = blob.Data;

            switch (rawData?.Length ?? 0)
            {
                // 逻辑删除或空数据
                case 0:
                    finalDataChunks[i] = [];
                    isChunkDeflated[i] = false;
                    continue;

                // 尝试压缩的阈值：太小的数据压缩不仅没效果，解压还费CPU
                // 假设只有大于 64 字节的数据才尝试压缩
                case > 64:
                {
                    var compressedData = CompressionUtils.Deflate(rawData!, 0, rawData!.Length);

                    // 策略：只有当压缩后的体积小于原体积的 90% 时才采用压缩
                    // 这样避免为了省几个字节而增加 Runtime 解压负担
                    if (compressedData.Length < rawData.Length * 0.9 && allowDeflate)
                    {
                        finalDataChunks[i] = compressedData;
                        isChunkDeflated[i] = true;
                    }
                    else
                    {
                        finalDataChunks[i] = rawData;
                        isChunkDeflated[i] = false;
                    }

                    break;
                }

                default:
                    finalDataChunks[i] = rawData!;
                    isChunkDeflated[i] = false;
                    break;
            }
        }

        // -------------------------------------------------------
        // 步骤 1：构建索引 (Index Building)
        // -------------------------------------------------------
        using var indexWriter = new RyoWriter(new MemoryStream());
        indexWriter.WriteInt(objCount);

        // 1.1 写入适配项ID与压缩标志
        for (var i = 0; i < objCount; i++)
        {
            // [修正] 显式区分 Null(已删除) 和 Empty(空对象)

            // 墓碑标记：正常的 (Id << 1) 永远是非负数，所以 -1 是安全的特殊值
            if (ItemBlobs[i].Data == null) indexWriter.WriteInt(-1);
            else
            {
                var info = ItemBlobs[i].AdaptionId << 1;
                if (isChunkDeflated[i]) info |= 1;
                indexWriter.WriteInt(info);
            }
        }

        // 1.2 写入数据区结束点 (使用 finalDataChunks 的长度)
        var currentSavedLength = 0;
        for (var i = 0; i < objCount; i++)
        {
            currentSavedLength += finalDataChunks[i].Length;
            indexWriter.WriteInt(currentSavedLength);
        }

        // 1.3 写入粘连索引 (Cumulative Sticky Index - 虚拟连续化)
        var currentVirtualStickyIndex = 0;
        for (var i = 0; i < objCount; i++)
        {
            currentVirtualStickyIndex += ItemBlobs[i].StickyCount;
            indexWriter.WriteInt(currentVirtualStickyIndex);
        }

        // 1.4 写入元数据 (碎片整理 - Defragmentation)
        foreach (var blob in ItemBlobs)
            for (var k = 0; k < blob.StickyCount; k++)
                indexWriter.WriteInt(StickyMetaDatas[blob.StickyOffset + k]);

        // 1.5 写入适配器定义
        if (ItemAdaptions.Count == 0) indexWriter.WriteInt(0);
        else
        {
            indexWriter.WriteInt(ItemAdaptions.Count);

            for (var i = 0; i < ItemAdaptions.Count; i++)
            {
                var itemAdaption = ItemAdaptions[i];
                indexWriter.WriteInt(i);
                indexWriter.WrintString(itemAdaption.DataJavaClz);
                indexWriter.WrintString(itemAdaption.AdapterJavaClz);
            }
        }

        // 1.6 后处理钩子
        AfterSavingIndex(indexWriter);

        // -------------------------------------------------------
        // 步骤 2：写入索引块 (Index Write)
        // -------------------------------------------------------
        indexWriter.PositionToZero();
        var indexBytes = new RyoReader(indexWriter).ReadAllBytes();

        // 索引本身的压缩处理
        var deflatedIndexBytes = CompressionUtils.Deflate(indexBytes, 0, indexBytes.Length);

        if (allowDeflate && indexBytes.Length > 0 && (float)indexBytes.Length / deflatedIndexBytes.Length >= 1.1F)
        {
            fileWriter.WriteInt((deflatedIndexBytes.Length << 1) | 1); // 标记索引已压缩
            fileWriter.WriteBytes(deflatedIndexBytes);
        }
        else
        {
            fileWriter.WriteInt(indexBytes.Length << 1); // 标记索引未压缩
            fileWriter.WriteBytes(indexBytes);
        }

        // -------------------------------------------------------
        // 步骤 3：写入实体数据 (Body Write)
        // -------------------------------------------------------
        // 直接写入我们预处理好的 finalDataChunks
        for (var i = 0; i < objCount; i++)
        {
            var chunk = finalDataChunks[i];

            if (chunk.Length > 0) fileWriter.WriteBytes(chunk);
        }
    }

    // 必须保留的抽象方法/虚方法钩子
    protected virtual void AfterLoadingIndex(RyoReader reader)
    {
    }

    protected virtual void AfterSavingIndex(RyoWriter writer)
    {
    }
}