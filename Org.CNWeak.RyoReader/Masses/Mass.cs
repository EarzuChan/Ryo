using Me.EarzuChan.Ryo.Adaptions.AdapterFactories;
using Me.EarzuChan.Ryo.Adaptions;
using Me.EarzuChan.Ryo.IO;
using Me.EarzuChan.Ryo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Masses
{
    public class Mass
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

            var dataRyoType = AdaptionManager.INSTANCE.GetTypeByJavaClz(str1);

            var adapterFactoryRyoType = AdaptionManager.INSTANCE.GetTypeByJavaClz(str2);

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
            // 检查
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

            return item;
        }

        // 构造
        public Mass(string name)
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
                    indexWriter.WrintString(Adapters[i7].JavaClz);

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
    }
}