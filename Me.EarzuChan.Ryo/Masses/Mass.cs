using Me.EarzuChan.Ryo.Adaptions.AdapterFactories;
using Me.EarzuChan.Ryo.Adaptions;
using Me.EarzuChan.Ryo.IO;
using Me.EarzuChan.Ryo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Me.EarzuChan.Ryo.Formations.RyoPixmap;
using System.Reflection.PortableExecutable;
using System.Diagnostics;
using Me.EarzuChan.Ryo.Commands;

namespace Me.EarzuChan.Ryo.Masses
{
    /*public class OldMass
    {
        // 常量
        public string ExtendedName = "MASS";

        // 文件名和路径
        public string Name = "";
        public string FullPath = "";

        // 暂存读写器
        public RyoReader Reader = new(Stream.Null);
        public RyoWriter Writer = new(Stream.Null);

        // 相关成员变量
        public int ObjCount = 0;
        public int CurrentObjCount = 0;
        public List<int> DataAdapterIdArray = new();
        public List<int> EndPosition = new();
        public List<bool> IsItemDeflatedArray = new();
        public List<int> StickedDataList = new();
        public List<int> StickedMetaDataList = new();
        public byte[] RealItemDataBuffer = Array.Empty<byte>();
        public RyoBuffer WorkBuffer = new RyoBuffer();
        //public int StickedMetaDataItemCount = 0;
        public List<RegableDataAdaption> MyRegableDataAdaptionList = new();
        public List<RyoType> DataRyoTypes = new();
        public List<RyoType> AdapterFactoryRyoTypes = new();
        public List<IAdapter> Adapters = new();

        // 暂存变量
        public bool ReadyToUse = true;
        public bool IsEmpty = true;
        public int SavedStickedDataIntArrayIdMinusOne = -1;
        public int SavedStickedDataIntArrayId = -1;
        public int SavedId = -1;

        // 碎片块
        public List<byte[]> BlobList = new();

        // 可注册项
        public class RegableDataAdaption
        {
            public string DataJavaClz { get; set; }
            //public RyoType DataRyoType { get; set; }
            public string AdapterJavaClz { get; set; }
            //public RyoType AdapterFactoryRyoType { get; set; }
            //public IAdapter Adapter { get; set; }
            public int Id { get; set; }

            public RegableDataAdaption(int id, string dataType, string adapterType)
            {
                Id = id;

                DataJavaClz = dataType;
                AdapterJavaClz = adapterType;
                // LogUtil.INSTANCE.PrintInfo("Ryo：" + DataRyoType, "哈希：" + DataRyoType.GetHashCode());
            }
        }

        public RegableDataAdaption RegItem(int id, string str1, string str2)
        {
            var adaption = new RegableDataAdaption(id, str1, str2);

            var dataRyoType = AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(str1);

            var adapterFactoryRyoType = AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(str2);

            IAdapter adapter;

            // 通过工厂创建
            try
            {
                if (typeof(IAdapterFactory).IsAssignableFrom(adapterFactoryRyoType.BaseType))
                {
                    adapter = ((IAdapterFactory)Activator.CreateInstance(adapterFactoryRyoType.BaseType)!).Create(dataRyoType);
                }
                else throw new FormatException($"{adapterFactoryRyoType}不是一个适配器工厂类");
            }
            catch (Exception ex)
            {
                LogUtil.INSTANCE.PrintError($"注册适配项（ID.{id}）时出现问题", ex, false);
                adapter = new DirectByteArrayAdapter();
            }

            Adapters.Add(adapter);
            DataRyoTypes.Add(dataRyoType);
            AdapterFactoryRyoTypes.Add(adapterFactoryRyoType);

            return adaption;
        }

        // 根据ID获取项目
        public T GetItemById<T>(int id)
        {
            throw new NotImplementedException();
            *//*// 检查
            if (!ReadyToUse || IsEmpty) throw new Exception("文件未准备好或为空");

            // 获取适配项
            var dataRyoType = DataRyoTypes[DataAdapterIdArray[id]];
            var adapter = Adapters[DataAdapterIdArray[id]];

            // 获取各方面数据
            RyoBuffer workBuffer = WorkBuffer;
            int oldStickedDataIntArrayIdMinusOne = SavedStickedDataIntArrayIdMinusOne;
            int oldStickedDataIntArrayId = SavedStickedDataIntArrayId;
            int oldIdSaves = SavedId;

            // 处理暂存数据
            SavedId = id;
            SavedStickedDataIntArrayIdMinusOne = id == 0 ? 0 : StickedDataList[id - 1];
            SavedStickedDataIntArrayId = StickedDataList[id];

            // 获取Blob
            WorkBuffer.Buffer = BlobList[id];

            // LogUtil.INSTANCE.PrintDebugInfo("源长度", RealItemDataBuffer.Length.ToString(), "ID", id.ToString(), "起始", blobStartPosition.ToString(), "长度", workBuffer.Length.ToString(), "3-1", SavedStickedDataIntArrayIdMinusOne.ToString(), "3", SavedStickedDataIntArrayId.ToString());

            // 从Blob建立对象
            T item = (T)adapter.From(this, (RyoReader)WorkBuffer.Buffer, dataRyoType);

            // 还回数据
            WorkBuffer = workBuffer;
            SavedId = oldIdSaves;
            SavedStickedDataIntArrayId = oldStickedDataIntArrayId;
            SavedStickedDataIntArrayIdMinusOne = oldStickedDataIntArrayIdMinusOne;

            return item;*//*
        }

        // 构造
        public OldMass(string name)
        {
            Name = name;
        }

        // 重置
        public void Clean()
        {
            ReadyToUse = false;
            try
            {
                Reader = new(Stream.Null);
                Writer = new(Stream.Null);
                ObjCount = 0;
                CurrentObjCount = 0;
                DataAdapterIdArray = new();
                EndPosition = new();
                StickedDataList = new();
                StickedMetaDataList = new();
                //StickedMetaDataItemCount = 0;
                RealItemDataBuffer = Array.Empty<byte>();
                WorkBuffer = new RyoBuffer();

                SavedStickedDataIntArrayId = -1;
                SavedStickedDataIntArrayIdMinusOne = -1;
                SavedId = -1;
                IsEmpty = true;
                ReadyToUse = true;

                BlobList = new();
            }
            catch (Exception e)
            {
                LogUtil.INSTANCE.PrintError("清除Mass失败", e);
            }
        }

        // 加载
        public void Load(FileStream file, string format)
        {
            FullPath = file.Name;

            if (!IsEmpty) Clean();

            ReadyToUse = false;
            try
            {
                Reader = new RyoReader(file);

                var isCorrectFormat = Reader.CheckHasString(format);
                if (!isCorrectFormat) return;

                var num = Reader.ReadInt();
                var isDeflated = (num & 1) != 0;
                var deflateLen = num >> 1;
                var indexDataBlob = isDeflated ? CompressionUtil.INSTANCE.Inflate(Reader.ReadBytes(deflateLen), 0, deflateLen) : Reader.ReadBytes(deflateLen);

                if (indexDataBlob != null && indexDataBlob.Length != 0)
                {
                    using var inflatedDataReader = new RyoReader(new MemoryStream(indexDataBlob));
                    ObjCount = inflatedDataReader.ReadInt();
                    CurrentObjCount = ObjCount;

                    for (var i = 0; i < ObjCount; i++)
                    {
                        int j = inflatedDataReader.ReadInt();
                        DataAdapterIdArray.Add(j >> 1);
                        IsItemDeflatedArray.Add((j & 1) != 0);
                    }

                    for (var i = 0; i < ObjCount; i++)
                        EndPosition.Add(inflatedDataReader.ReadInt());

                    for (var i = 0; i < ObjCount; i++)
                        StickedDataList.Add(inflatedDataReader.ReadInt());

                    //StickedMetaDataItemCount = StickedDataList.Last();

                    for (var i = 0; i < StickedDataList.Last(); i++)
                        StickedMetaDataList.Add(inflatedDataReader.ReadInt());

                    var regCount = inflatedDataReader.ReadInt();
                    //lock (MyRegableDataAdaptionList)
                    for (var i = 0; i < regCount; i++)
                    {
                        var id = inflatedDataReader.ReadInt();
                        var str1 = inflatedDataReader.ReadString();
                        var str2 = inflatedDataReader.ReadString();
                        MyRegableDataAdaptionList.Add(RegItem(id, str1, str2));
                        //LogUtil.INSTANCE.PrintInfo("拨弄：" + MyRegableDataAdaptionList[i].DataRyoType);
                    }

                    AfterLoadingIndex(inflatedDataReader);
                }
                RealItemDataBuffer = Reader.ReadAllBytes();

                var blobsReader = new RyoReader(new MemoryStream(RealItemDataBuffer));
                for (var i = 0; i < ObjCount; i++)
                {
                    int blobStart = i == 0 ? 0 : EndPosition[i - 1];
                    int blobLength = EndPosition[i] - blobStart;
                    byte[] blob = blobsReader.ReadBytes(blobLength);
                    if (IsItemDeflatedArray[i]) blob = CompressionUtil.INSTANCE.Inflate(blob, 0, blobLength);
                    BlobList.Add(blob);
                }

                ReadyToUse = true;
                IsEmpty = false;
            }
            catch (Exception e)
            {
                LogUtil.INSTANCE.PrintError("加载Mass失败", e);
            }
            finally
            {
                file.Dispose();
            }
        }

        // 保存
        public void Save(FileStream file, string format)
        {
            var fileWriter = new RyoWriter(file);
            fileWriter.WriteFixedString(format);

            // 先创建索引 再压缩并写入值
            var indexWriter = new RyoWriter(new MemoryStream());
            indexWriter.WriteInt(ObjCount);
            for (int i = 0; i < ObjCount; i++)
            { // 写序列化ID
              // TODO:项目压缩问题
                indexWriter.WriteInt(DataAdapterIdArray[i] << 1);//(IsItemDeflatedArray[i] == true ? 1 : 0));
            }
            int savedLength = 0;
            for (int i2 = 0; i2 < ObjCount; i2++)
            { // 写结束乐队
                savedLength += BlobList[i2].Length;
                indexWriter.WriteInt(savedLength);
            }
            for (int i3 = 0; i3 < ObjCount; i3++)
            { // 写粘连科技
                indexWriter.WriteInt(StickedDataList[i3]);
            }
            for (int i4 = 0; i4 < StickedDataList.Last(); i4++)
            { // 写元数据
                indexWriter.WriteInt(StickedMetaDataList[i4]);
            }
            if (MyRegableDataAdaptionList == null || MyRegableDataAdaptionList.Count == 0)
            { // 写出待注册
                indexWriter.WriteInt(0);
            }
            else
            {
                indexWriter.WriteInt(MyRegableDataAdaptionList.Count);
                for (int i7 = 0; i7 < MyRegableDataAdaptionList.Count; i7++)
                {
                    indexWriter.WriteInt(i7);
                    indexWriter.WrintString(MyRegableDataAdaptionList[i7].DataJavaClz);//AdaptionManager.INSTANCE.GetJavaClzByType(MyRegableDataAdaptionList[i7].DataRyoType));
                    // indexWriter.WrintString(Adapters[i7].JavaClz);

                    // 有问题。。。LogUtil.INSTANCE.PrintInfo("ID：" + i7, "类名：" + AdaptionManager.INSTANCE.GetJavaClzByType(MyRegableDataAdaptionList[i7].DataRyoType), "序列化器类名：" + MyRegableDataAdaptionList[i7].Adapter.JavaClz);
                }
            }

            // 后处理
            AfterSavingIndex(indexWriter);

            indexWriter.PositionToZero();
            // 写入大小且压缩操作
            byte[] indexBytes = new RyoReader((Stream)indexWriter).ReadAllBytes();
            byte[] yasuoedBytes = CompressionUtil.INSTANCE.Deflate(indexBytes, 0, indexBytes.Length);
            // LogUtil.INSTANCE.PrintInfo("大小：" + indexBytes.Length, "压缩：" + yasuoedBytes.Length);
            if (indexBytes.Length / yasuoedBytes.Length >= 1.2F)
            {
                fileWriter.WriteInt((yasuoedBytes.Length << 1) | 1);
                fileWriter.WriteBytes(yasuoedBytes);
            }
            else
            {
                fileWriter.WriteInt(indexBytes.Length << 1);
                fileWriter.WriteBytes(indexBytes);
            }

            // 写真正项目
            if (BlobList != null || BlobList!.Count != 0) foreach (var blob in BlobList) fileWriter.WriteBytes(blob);

            indexWriter.Dispose();
            fileWriter.Dispose();
            file.Dispose();
        }

        public void Save() => Save(new FileStream(FullPath, FileMode.OpenOrCreate), ExtendedName);

        public void Save(FileStream file) => Save(file, ExtendedName);

        public virtual void AfterLoadingIndex(RyoReader inflatedDataReader) { }

        public virtual void AfterSavingIndex(RyoWriter inflatedDataReader) { }

        // 应该弃用
        public string DumpBuffer()
        {
            if (!ReadyToUse || IsEmpty) return "未准备好或者内容为空";
            if (string.IsNullOrWhiteSpace(FullPath)) return "保存路径为空";
            var info = "保存";
            try
            {
                using (FileStream fs = new(FullPath + ".dump", FileMode.Create, FileAccess.Write))
                {
                    fs.Write(RealItemDataBuffer, 0, RealItemDataBuffer.Length);
                }
                info += "成功，路径：" + FullPath + ".dump";
            }
            catch (Exception ex)
            {
                LogUtil.INSTANCE.PrintError("保存时错误", ex);
                info += "失败（悲";
            }
            return info;
        }

        // 读子项
        public T Read<T>()
        {
            // 子项不能等于父项
            if (SavedStickedDataIntArrayIdMinusOne == SavedStickedDataIntArrayId) throw new Exception("噬主了");

            // 获取元素据
            int metaOfIdMinusOne = StickedMetaDataList[SavedStickedDataIntArrayIdMinusOne];

            // 原来的增加
            SavedStickedDataIntArrayIdMinusOne++;

            // 打印增加后的
            LogUtil.INSTANCE.PrintDebugInfo("新粘连ID：" + SavedStickedDataIntArrayIdMinusOne);

            // 获取元数据的真意
            int subitemId = metaOfIdMinusOne >> 2;
            LogUtil.INSTANCE.PrintDebugInfo($"读子项的ID：{subitemId}");

            // 必须满足要求
            if ((metaOfIdMinusOne & 3) == 3) return GetItemById<T>(subitemId);
            throw new NotSupportedException("三大欲望");
        }

        // 引用（实际上是暂存）
        public void Reference(object objArr)
        {
            //TODO:实际上是暂存下
            //throw new NotImplementedException();
        }
    }*/

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

        T Read<T>(); // 需要被取缔
    }

    public class Mass : IMass
    {
        protected string ExtendedName = "Mass";
        private RyoBuffer WorkBuffer = new();//待优化
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
            var javaClz = AdaptionManager.INSTANCE.GetJavaClzByRyoType(ryoType)!;
            var result = ItemAdaptions.Find(a => a.DataJavaClz == javaClz);
            if (result != null) return ItemAdaptions.IndexOf(result);

            var adapterRyoType = AdaptionManager.INSTANCE.FindAdapterRyoTypeForDataRyoType(ryoType) ?? throw new FormatException("类型没有可用的适配器：" + ryoType);

            var adapterJavaClz = AdaptionManager.INSTANCE.GetJavaClzByRyoType(adapterRyoType)!;
            var itemAdaption = new ItemAdaption(javaClz, adapterJavaClz);
            ItemAdaptions.Add(itemAdaption);

            return ItemAdaptions.IndexOf(itemAdaption);
        }

        // TODO:实现带名字的添加很简单，添加ID于文本的Item就行了
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
                    // LogUtil.INSTANCE.PrintInfo($"ID：{id} 适配前_循环第：{nowId}");

                    RyoType dataRyoType = AdaptionManager.INSTANCE.GetRyoTypeByCsClz(nowObj.GetType());

                    // LogUtil.INSTANCE.PrintInfo("类型：" + dataRyoType);

                    var adaptionId = FindAdaptionIdForDataRyoType(dataRyoType);
                    var adaption = ItemAdaptions[adaptionId];

                    var adapter = AdaptionManager.INSTANCE.CreateAdapter(AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(adaption.AdapterJavaClz), dataRyoType);

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
                throw new Exception("不能添加对象，因为" + ex.Message, ex);
            }
        }

        public T Get<T>(int id)
        {
            if (id < 0 || id >= ItemBlobs.Count) throw new IndexOutOfRangeException("索引超界");

            var itemBlob = ItemBlobs[id];
            var itemAdaption = ItemAdaptions[itemBlob.AdaptionId];

            // 获取适配项
            var dataRyoType = AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(itemAdaption.DataJavaClz);
            // LogUtil.INSTANCE.PrintInfo("项目类型：" + dataRyoType);
            IAdapter adapter;
            try
            {
                // LogUtil.INSTANCE.PrintInfo("修正：" + dataRyoType);
                adapter = AdaptionManager.INSTANCE.CreateAdapter(AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(itemAdaption.AdapterJavaClz), dataRyoType);
            }
            catch (Exception ex)
            {
                adapter = new DirectByteArrayAdapter();
                LogUtil.INSTANCE.PrintError($"为对象（ID：{id}）创建适配器时出错", ex);
            }

            // 获取各方面数据
            RyoBuffer workBuffer = WorkBuffer;
            int savedItemBlobStickyIdIndexMinusOne = SavedItemBlobMinusOneStickyId;
            int savedItemBlobStickyIdIndex = SavedItemBlobStickyId;
            int savedId = SavedId;

            // 处理暂存数据
            SavedId = id;
            SavedItemBlobMinusOneStickyId = id == 0 ? 0 : ItemBlobs[id - 1].StickyIndex;
            SavedItemBlobStickyId = itemBlob.StickyIndex;
            LogUtil.INSTANCE.PrintInfo($"暂存ID：{SavedId}", $"暂存减一：{SavedItemBlobMinusOneStickyId}", $"暂存直接：{SavedItemBlobStickyId}");

            // 获取Blob
            WorkBuffer.Buffer = itemBlob.Data;

            // LogUtil.INSTANCE.PrintDebugInfo("源长度", RealItemDataBuffer.Length.ToString(), "ID", id.ToString(), "起始", blobStartPosition.ToString(), "长度", workBuffer.Length.ToString(), "3-1", SavedStickedDataIntArrayIdMinusOne.ToString(), "3", SavedStickedDataIntArrayId.ToString());

            // 从Blob建立对象
            T item = (T)adapter.From(this, (RyoReader)WorkBuffer.Buffer, dataRyoType);

            // 还回数据
            WorkBuffer = workBuffer;
            SavedId = savedId;
            SavedItemBlobMinusOneStickyId = savedItemBlobStickyIdIndexMinusOne;
            SavedItemBlobStickyId = savedItemBlobStickyIdIndex;

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
            LogUtil.INSTANCE.PrintInfo($"减一元数据：{metaOfIdMinusOne}", $"保存的减一：{SavedItemBlobMinusOneStickyId}", $"子项ID：{subitemId}", $"与三和：{metaOfIdMinusOne & 3}");

            // 必须满足要求
            if ((metaOfIdMinusOne & 3) == 3) return Get<T>(subitemId);
            throw new NotSupportedException($"元数据的类型（{metaOfIdMinusOne & 3}）暂不支持");
        }

        // 如果是FS，需要在ID名字表中拨弄删除
        public void Remove(int id)
        {
            throw new NotImplementedException();
        }

        // 子项怎么办
        public void Set(int id, object obj)
        {
            throw new NotSupportedException("暂不支持！");

            try
            {
                if (obj == null) throw new NullReferenceException("对象为Null");

                //int id = ItemBlobs.Count;
                bool doSthLess = !IsPutting;

                RyoType dataRyoType = AdaptionManager.INSTANCE.GetRyoTypeByCsClz(obj.GetType());
                var adaptionId = FindAdaptionIdForDataRyoType(dataRyoType);
                var adaption = ItemAdaptions[adaptionId];
                // if(dataRyoType!=AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(adaption.DataJavaClz)) 

                var adapter = AdaptionManager.INSTANCE.CreateAdapter(AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(adaption.AdapterJavaClz), dataRyoType);

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
            catch (Exception ex) { throw new Exception("不能设置对象，因为" + ex.Message, ex); }
        }

        public void Load(FileStream fileStream)
        {
            using var reader = new RyoReader(fileStream);

            var isCorrectFormat = reader.CheckHasString(ExtendedName);
            if (!isCorrectFormat) throw new FormatException("文件格式校验失败");

            var indexInfo = reader.ReadInt();
            var isDeflated = (indexInfo & 1) != 0;
            var deflateLen = indexInfo >> 1;
            var indexBlob = isDeflated ? CompressionUtil.Inflate(reader.ReadBytes(deflateLen), 0, deflateLen) : reader.ReadBytes(deflateLen);

            List<bool> isItemBlobDeflatedList = new();
            List<int> itemBlobEndPositions = new();
            List<int> itemBlobAdaptionIds = new();
            List<int> itemBlobStickyIds = new();
            int objCount = 0;

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
                for (var i = 0; i < itemBlobStickyIds.Last(); i++) StickyMetaDatas.Add(inflatedDataReader.ReadInt());

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
                if (isItemBlobDeflatedList[i]) itemBlobData = CompressionUtil.Inflate(itemBlobData, 0, blobLength);
                ItemBlobs.Add(new ItemBlob(itemBlobAdaptionIds[i], itemBlobStickyIds[i], itemBlobData));
            }
        }

        public void Save(FileStream fileStream, bool noDeflated = false)
        {
            using var fileWriter = new RyoWriter(fileStream);
            fileWriter.WriteFixedString(ExtendedName);

            // 先创建索引 再压缩并写入值
            int objCount = ItemBlobs.Count;
            using var indexWriter = new RyoWriter(new MemoryStream());
            indexWriter.WriteInt(objCount);

            // TODO:检测并压缩项目，或者添加项目时压缩，时刻准备着！
            for (int i = 0; i < objCount; i++) indexWriter.WriteInt(ItemBlobs[i].AdaptionId << 1);

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
            byte[] deflatedBytes = CompressionUtil.Deflate(indexBytes, 0, indexBytes.Length);
            if (!noDeflated && indexBytes.Length / deflatedBytes.Length >= 1.2F)
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

                if (obj == null) newStickyMetaData = 0; //Ret?
                else if (AdaptionManager.INSTANCE.GetRyoTypeByCsClz(obj.GetType()).IsJvmBaseType)
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