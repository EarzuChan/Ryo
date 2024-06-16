using Me.EarzuChan.Ryo.Core.Adaptations;
using Me.EarzuChan.Ryo.Core.Exceptions;
using Me.EarzuChan.Ryo.Core.IO;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Exceptions;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Core.Masses
{
    public interface IMass
    {
        void Load(FileStream fileStream);

        void Save(FileStream fileStream, bool noDeflated = false);

        /*void AfterLoadingIndex(RyoReader reader);

        void AfterSavingIndex(RyoWriter writer);*/

        T Get<T>(int id);

        int Add(object obj);

        void Remove(int id);

        void Set(int id, object obj);

        /* 加载时绑定数据块的适配项
         * 单独注册项
         * 获取时获取适配器（不影响没适配的类）
         * 删除时如果适配了才删除子项
         * 实现数据块压缩与刷新适配项、元数据
         */

        // T Read<T>(); // 需要被取缔
    }

    public class Mass : IMass
    {
        protected string ExtendedName = "Mass";

        private RyoBuffer WorkingBuffer = new(); // TODO:待优化

        private int SavedId;

        private int SavedItemBlobMinusOneStickyId;

        private int SavedItemBlobStickyId;

        private bool IsPutting;

        private Dictionary<int, object>? SavedItems = null;

        public class ItemBlob
        {
            public int AdaptionId;

            public int StickyIndex;

            public byte[] Data;

            public ItemBlob(int adaptionId, int stickyId, byte[] data)
            {
                AdaptionId = adaptionId;
                StickyIndex = stickyId;
                Data = data;
            }
        }

        public class ItemAdaption
        {
            //public int AdaptionId;

            public string DataJavaClz;

            public string AdapterJavaClz;

            public ItemAdaption(string dataJavaClz, string adapterFactoryJavaClz)
            {
                //AdaptionId = adaptionId;
                DataJavaClz = dataJavaClz;
                AdapterJavaClz = adapterFactoryJavaClz;
            }
        }

        public readonly List<ItemBlob> ItemBlobs = new();

        public readonly List<ItemAdaption> ItemAdaptions = new();

        public readonly List<int> StickyMetaDatas = new();

        // 修正Adaptions命名空间，让工厂或者别的什么玩意能寻找适配器。先遍历表，没有就获取然后加新。反正返回适配项ID
        public int FindAdaptionIdForDataRyoType(RyoType ryoType)
        {
            // 查找一手好活
            var javaClz = ryoType.ToJavaClass()!;
            var result = ItemAdaptions.Find(a => a.DataJavaClz == javaClz);
            if (result != null) return ItemAdaptions.IndexOf(result);

            var adapterRyoType = ryoType.DataRyoTypeFindAdapterRyoType() ?? throw new FormatException("该类型没有可用的适配器：" + ryoType);

            var adapterJavaClz = adapterRyoType.ToJavaClass()!;
            var itemAdaption = new ItemAdaption(javaClz, adapterJavaClz);
            ItemAdaptions.Add(itemAdaption);

            return ItemAdaptions.IndexOf(itemAdaption);
        }

        public int Add(object obj)
        {
            try
            {
                if (obj == null) throw new NullReferenceException("对象为Null，我测你们妈");

                int id = ItemBlobs.Count;
                // LogUtil.INSTANCE.PrintInfo("当前ID：" + id);

                // if (id > 10) Environment.Exit(1919810);

                ItemBlobs.Add(new ItemBlob(114514, 1919810, Array.Empty<byte>()));// 占位耳

                // 代表正在序列化，托管给原主
                // 收集单
                if (SavedItems != null)
                {
                    SavedItems.Add(id, obj);
                    // LogUtil.INSTANCE.PrintInfo($"直接返回：{id}");
                    return id;
                }
                SavedItems = new() { { id, obj } };

                int nowId = id;
                while (nowId < id + SavedItems.Count)
                {
                    object nowObj = SavedItems[nowId] ?? throw new NullReferenceException("噗叽啪");
                    // LogUtils.PrintInfo($"主Id：{id} 本Id：{nowId}");

                    RyoType dataRyoType = nowObj.GetType().ToRyoType();

                    var adaptionId = FindAdaptionIdForDataRyoType(dataRyoType);
                    var adaption = ItemAdaptions[adaptionId];

                    // LogUtils.PrintInfo($"Id：{adaptionId} 类型：{dataRyoType} {adaption.AdapterJavaClz}");

                    var adapter = AdaptationUtils.CreateAdapter(adaption.AdapterJavaClz.JavaClassToRyoType(), dataRyoType);

                    using var writer = new RyoWriter(new MemoryStream());
                    adapter.To(nowObj, this, writer);
                    writer.PositionToZero();

                    ItemBlobs[nowId] = new ItemBlob(adaptionId, StickyMetaDatas.Count, new RyoReader(writer).ReadAllBytes());
                    //LogUtil.INSTANCE.PrintInfo($"ID：{id} 适配后_循环第：{nowId} 粘连索引：{StickyMetaDatas.Count}");

                    nowId++;
                    /*LogUtil.INSTANCE.PrintInfo("No." + id + "老索引：" + oldSticky, "总StickyIndex：" + (oldSticky + oldStickyAdd + SavedItemBlobStickyId));
                    ItemBlobs[id].StickyIndex = ;
                    ItemBlobs[id].Data = ;*/
                }

                SavedItems = null;

                return id;
            }
            catch (Exception ex)
            {
                throw new RyoException("不能添加对象，因为" + ex.Message, ex);
            }
        }

        public T Get<T>(int id)
        {
            if (id < 0 || id >= ItemBlobs.Count) throw new IndexOutOfRangeException("索引超界");

            var itemBlob = ItemBlobs[id];
            var itemAdaption = ItemAdaptions[itemBlob.AdaptionId];

            // 获取适配项
            var dataRyoType = itemAdaption.DataJavaClz.JavaClassToRyoType();
            // LogUtils.INSTANCE.PrintInfo("项目类型：" + dataRyoType);
            IAdapter adapter;
            try
            {
                // LogUtil.INSTANCE.PrintInfo("修正：" + dataRyoType);
                adapter = AdaptationUtils.CreateAdapter(itemAdaption.AdapterJavaClz.JavaClassToRyoType(), dataRyoType);
            }
            catch (Exception ex)
            {
                adapter = new DirectReadRawBytesAdapter();
                LogUtils.PrintError($"为对象（ID：{id}）创建适配器时出错", ex);
            }

            // 获取各方面数据
            RyoBuffer workBuffer = WorkingBuffer;
            int savedItemBlobStickyIdIndexMinusOne = SavedItemBlobMinusOneStickyId;
            int savedItemBlobStickyIdIndex = SavedItemBlobStickyId;
            int savedId = SavedId;

            // 处理暂存数据
            SavedId = id;
            SavedItemBlobMinusOneStickyId = id == 0 ? 0 : ItemBlobs[id - 1].StickyIndex;
            SavedItemBlobStickyId = itemBlob.StickyIndex;
            // LogUtils.INSTANCE.PrintInfo($"暂存ID：{SavedId}", $"暂存减一：{SavedItemBlobMinusOneStickyId}", $"暂存直接：{SavedItemBlobStickyId}");

            // 获取Blob
            WorkingBuffer.Buffer = itemBlob.Data;

            // LogUtils.INSTANCE.PrintInfo("长度", workBuffer.Length.ToString());

            // 从Blob建立对象
            T item = (T)adapter.From(this, (RyoReader)WorkingBuffer.Buffer, dataRyoType);

            // 还回数据
            WorkingBuffer = workBuffer;
            SavedId = savedId;
            SavedItemBlobMinusOneStickyId = savedItemBlobStickyIdIndexMinusOne;
            SavedItemBlobStickyId = savedItemBlobStickyIdIndex;

            // LogUtils.INSTANCE.PrintInfo("读取终了");

            return item;
        }

        public T Read<T>()
        {
            // 子项不能等于父项
            if (SavedItemBlobMinusOneStickyId == SavedItemBlobStickyId) throw new InvalidDataException("无子项的项目不能被当作父项读");

            // 获取元素据
            int metaOfIdMinusOne = StickyMetaDatas[SavedItemBlobMinusOneStickyId];

            // 原来的增加
            // 导致适配器再读可正确读好吧
            SavedItemBlobMinusOneStickyId++;

            // 打印增加后的
            //LogUtil.INSTANCE.PrintDebugInfo("新粘连ID：" + SavedItemBlobMinusOneStickyId);

            // 获取元数据的真意
            int subitemId = metaOfIdMinusOne >> 2;
            // LogUtils.INSTANCE.PrintInfo($"减一元数据：{metaOfIdMinusOne}", $"保存的减一：{SavedItemBlobMinusOneStickyId}", $"子项ID：{subitemId}", $"与三和：{metaOfIdMinusOne & 3}");

            // 必须满足要求
            return (metaOfIdMinusOne & 3) switch
            {
                0 => default,// 本该返回NULL
                3 => Get<T>(subitemId),
                _ => throw new NotSupportedException($"元数据的类型（{metaOfIdMinusOne & 3}）暂不支持"), // 1抛NULL、2内联还不支持
            };
        }

        // 如果是FS，需要在ID名字表中拨弄删除
        public void Remove(int id)
        {
            throw new NotImplementedException("暂不支持！");
        }

        // 子项怎么办
        public void Set(int id, object obj)
        {
            throw new NotSupportedException("暂不支持！");

            // 以前的可能可以不要了，我的评价是新增，然后把原来的空荡图图了，然后改粘连数据
            try
            {
                if (obj == null) throw new NullReferenceException("对象为Null");

                //int id = ItemBlobs.Count;
                bool doSthLess = !IsPutting;

                RyoType dataRyoType = obj.GetType().ToRyoType();
                var adaptionId = FindAdaptionIdForDataRyoType(dataRyoType);
                var adaption = ItemAdaptions[adaptionId];
                // if(dataRyoType!=AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(adaption.DataJavaClz)) 

                var adapter = AdaptationUtils.CreateAdapter(adaption.AdapterJavaClz.JavaClassToRyoType(), dataRyoType);

                int stickyId = ItemBlobs[id].StickyIndex;
                SavedItemBlobStickyId = stickyId;

                var writer = new RyoWriter(new MemoryStream());
                adapter.To(obj, this, writer);
                writer.PositionToZero();

                var itemBlob = new ItemBlob(adaptionId, stickyId, new RyoReader(writer).ReadAllBytes());

                ItemBlobs[id] = itemBlob;

                if (!doSthLess) IsPutting = false;
                // 写写手写到对应Blob和压缩（待办）
                // 记得解析原文的对于粘连数据干了什么？我觉得是大小，因为没读（写）不刷新，写元数据自然是最新

                // throw new NotImplementedException("暂未完成");

                //return id;
            }
            catch (Exception ex) { throw new RyoException("不能设置对象，因为" + ex.Message, ex); }
        }

        public void Load(FileStream fileStream)
        {
            using var reader = new RyoReader(fileStream);

            var isCorrectFormat = reader.CheckHasString(ExtendedName);
            if (!isCorrectFormat) throw new FormatException("文件格式校验失败");

            var indexInfo = reader.ReadInt();
            var isDeflated = (indexInfo & 1) != 0;
            var deflateLen = indexInfo >> 1;
            var indexBlob = isDeflated ? CompressionUtils.Inflate(reader.ReadBytes(deflateLen), 0, deflateLen) : reader.ReadBytes(deflateLen);

            List<bool> isItemBlobDeflatedList = new();
            List<int> itemBlobEndPositions = new();
            List<int> itemBlobAdaptionIds = new();
            List<int> itemBlobStickyIds = new();
            int objCount = 0;

            // LogUtils.INSTANCE.PrintInfo("索引长度：" + indexBlob.Length);
            if (indexBlob != null && indexBlob.Length != 0)
            {
                using var inflatedDataReader = new RyoReader(new MemoryStream(indexBlob));
                objCount = inflatedDataReader.ReadInt();

                // 读适配项与压缩情况
                for (var i = 0; i < objCount; i++)
                {
                    // TODO:之所以适配项有ID，就是因为索引并不是一一对应适配项的，还需斟酌
                    int itemBlobInfo = inflatedDataReader.ReadInt();
                    itemBlobAdaptionIds.Add(itemBlobInfo >> 1);
                    isItemBlobDeflatedList.Add((itemBlobInfo & 1) != 0);
                }

                // 读末尾点
                for (var i = 0; i < objCount; i++) itemBlobEndPositions.Add(inflatedDataReader.ReadInt());

                // 读粘连ID
                for (var i = 0; i < objCount; i++) itemBlobStickyIds.Add(inflatedDataReader.ReadInt());

                // 读粘连元数据
                int stickyMetaDataCount = objCount == 0 ? 0 : itemBlobStickyIds.Last();
                for (var i = 0; i < stickyMetaDataCount; i++) StickyMetaDatas.Add(inflatedDataReader.ReadInt());

                // 读适配项
                int regCount = inflatedDataReader.ReadInt();
                for (var i = 0; i < regCount; i++)
                {
                    inflatedDataReader.ReadInt();
                    var str1 = inflatedDataReader.ReadString();
                    var str2 = inflatedDataReader.ReadString();
                    ItemAdaptions.Add(new ItemAdaption(str1, str2));
                }

                AfterLoadingIndex(inflatedDataReader);
            }

            byte[] itemBlobsBuffer = reader.ReadAllBytes();

            var blobsReader = new RyoReader(new MemoryStream(itemBlobsBuffer));
            for (var i = 0; i < objCount; i++)
            {
                int blobStart = i == 0 ? 0 : itemBlobEndPositions[i - 1];
                int blobLength = itemBlobEndPositions[i] - blobStart;
                byte[] itemBlobData = blobsReader.ReadBytes(blobLength);
                if (isItemBlobDeflatedList[i]) itemBlobData = CompressionUtils.Inflate(itemBlobData, 0, blobLength);
                ItemBlobs.Add(new ItemBlob(itemBlobAdaptionIds[i], itemBlobStickyIds[i], itemBlobData));
            }
        }

        // TODO:实现真正项目压缩
        public void Save(FileStream fileStream, bool deflated = true)
        {
            using var fileWriter = new RyoWriter(fileStream);
            fileWriter.WriteFixedString(ExtendedName);

            // 先创建索引 再压缩并写入值
            int objCount = ItemBlobs.Count;
            using var indexWriter = new RyoWriter(new MemoryStream());
            indexWriter.WriteInt(objCount);

            // TODO:检测并压缩项目！
            for (int i = 0; i < objCount; i++) indexWriter.WriteInt(ItemBlobs[i].AdaptionId << 1); // <<1则都没有压缩，全恼

            // 写结束乐队
            int savedLength = 0;
            for (int i = 0; i < objCount; i++)
            {
                savedLength += ItemBlobs[i].Data.Length;
                indexWriter.WriteInt(savedLength);
            }

            // 写粘连科技
            for (int i = 0; i < objCount; i++) indexWriter.WriteInt(ItemBlobs[i].StickyIndex);

            // 元数据
            for (int i4 = 0; i4 < StickyMetaDatas.Count; i4++) indexWriter.WriteInt(StickyMetaDatas[i4]);

            // 写出适配项
            if (ItemAdaptions == null || ItemAdaptions.Count == 0) indexWriter.WriteInt(0);
            else
            {
                indexWriter.WriteInt(ItemAdaptions.Count);
                for (int i7 = 0; i7 < ItemAdaptions.Count; i7++)
                {
                    // 添加、Put、Del完项目 项目块和适配项要刷新

                    var itemAdaption = ItemAdaptions[i7];
                    indexWriter.WriteInt(i7);
                    indexWriter.WrintString(itemAdaption.DataJavaClz);
                    indexWriter.WrintString(itemAdaption.AdapterJavaClz);
                }
            }

            // 后处理
            AfterSavingIndex(indexWriter);

            // 写入索引
            indexWriter.PositionToZero();
            byte[] indexBytes = new RyoReader((Stream)indexWriter).ReadAllBytes();
            byte[] deflatedBytes = CompressionUtils.Deflate(indexBytes, 0, indexBytes.Length);
            if (deflated && indexBytes.Length / deflatedBytes.Length >= 1.2F)
            {
                fileWriter.WriteInt((deflatedBytes.Length << 1) | 1);
                fileWriter.WriteBytes(deflatedBytes);
            }
            else
            {
                fileWriter.WriteInt(indexBytes.Length << 1);
                fileWriter.WriteBytes(indexBytes);
            }

            // 写真正项目
            foreach (var blob in ItemBlobs) fileWriter.WriteBytes(blob.Data);
        }

        protected virtual void AfterLoadingIndex(RyoReader reader) { }

        protected virtual void AfterSavingIndex(RyoWriter writer) { }

        // TODO:依托答辩
        public void Write<T>(T obj)
        {
            // LogUtil.INSTANCE.PrintInfo("覆写：" + IsPutting, "对象：" + obj);

            // 暂不写覆盖
            if (IsPutting)
            {
                throw new NotImplementedException();
                if (obj == null) throw new NullReferenceException("怕覆写空对象");

                int stickyMetaData = StickyMetaDatas[SavedItemBlobStickyId];
                if ((stickyMetaData & 3) != 3) throw new InvalidOperationException("暂不支持非3类型覆写");
                int subitemId = stickyMetaData >> 2;

                /*if (obj == null) stickyMetaData = 0;//Ret?
                else if (AdaptionManager.INSTANCE.GetRyoTypeByCsClz(obj.GetType()).IsJvmBaseType)
                {
                    throw new NotImplementedException();
                }
                else stickyMetaData = (SavedId << 2) | 3;*/ // Switch3

                Set(subitemId, obj);
                //读到的MetaIndex的Meta作为Set的Id，在Set中要更新MetaIndex。

            }
            else
            {
                int newStickyMetaData;

                if (obj == null) newStickyMetaData = 0; // 给SUB是NULL预留的
                else if (obj.GetType().ToRyoType().IsJavaPrimitiveType)
                {
                    throw new NotImplementedException();
                    // 序列化基本类型，然后写注册ID+2给4
                    // 那边业没实现这个
                    /*Class <?> cls = obj.getClass();
                    接口_序列化器 <?> xlhq = 方法_取序列化器(cls);
                    newStickyMetaData = (方法_取已序列化类的ID(cls) << 2) | 2;
                    try
                    { // 直接写入父项捏
                        xlhq.方法_写(this, this.读者_输出_暂存拨弄缓冲区, obj); // 直接写入父项捏
                    }
                    catch (Throwable th)
                    {
                        throw new 错误类_马斯("Cannot serialize inlined object: " + obj + ", type: " + cls, th);
                    }*/
                }
                else
                {
                    newStickyMetaData = (Add(obj) << 2) | 3;
                    //ItemBlobs[newItemId].StickyId++;
                } // Switch3
                StickyMetaDatas.Add(newStickyMetaData);
            }
        }
    }
}