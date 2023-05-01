using Me.EarzuChan.Ryo.Formations;
using Me.EarzuChan.Ryo.IO;
using Me.EarzuChan.Ryo.Masses;
using Me.EarzuChan.Ryo.Utils;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Me.EarzuChan.Ryo.Adaptions.AdapterFactories.BaseArrayTypeAdapterFactory;
using System.Reflection.PortableExecutable;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Me.EarzuChan.Ryo.Adaptions.AdapterFactories
{
    public interface IAdapterFactory
    {
        IAdapter Create(RyoType type);

        RyoType FindAdapterRyoTypeForDataRyoType(RyoType ryoType);

        //string? FindAdapterForRyoType
    }

    // 说明：真的智将来的，根据写出的多少个参数（Out方法的输出长度可以内部判断）来对于多长的构造器！高！师爷！
    public class CustomFormatAdapterFactory : IAdapterFactory
    {
        public class CustomFormatAdapter : IAdapter
        {
            private readonly List<ConstructorInfo> Ctors = new();
            private readonly List<List<Type>> CtorParams = new();

            public CustomFormatAdapter(List<ConstructorInfo> constructorInfos)
            {
                Ctors = constructorInfos;

                if (Ctors == null || Ctors.Count == 0) throw new NullReferenceException("构造器不满足条件，为Null或没有、没有、没有");

                // 遍历参数
                foreach (var c in Ctors)
                {
                    List<Type> paramTypes = new();
                    foreach (var p in c.GetParameters()) paramTypes.Add(p.ParameterType);
                    CtorParams.Add(paramTypes);
                }
            }

            // string IAdapter.JavaClz => "sengine.mass.serializers.MassSerializableSerializer";

            public object From(Mass mass, RyoReader reader, RyoType ryoType)
            {
                ConstructorInfo? ctor = null;
                List<Type>? paramTypes = null;

                // 瞄准构造器且匹配参数
                if (Ctors.Count > 1)
                {
                    int ctorId = reader.ReadSignedByte();
                    ctor = Ctors[reader.ReadSignedByte()];
                    paramTypes = CtorParams[ctorId];
                }
                else
                {
                    ctor = Ctors[0];
                    paramTypes = CtorParams[0];
                }

                // 一个一个参数来读取
                object[] args = new object[paramTypes.Count];
                for (int i = 0; i < paramTypes.Count; i++)
                {
                    Type item = paramTypes[i];

                    //LogUtil.INSTANCE.PrintDebugInfo($"参数{i + 1}：{item}");
                    // TODO:读参数方法
                    if (item == typeof(int)) args[i] = reader.ReadInt();
                    else if (item == typeof(string)) args[i] = reader.ReadString();
                    else if (item == typeof(float)) args[i] = reader.ReadFloat();
                    else if (item == typeof(bool)) args[i] = reader.ReadBoolean();
                    else args[i] = mass.Read<object>();
                }

                // 使用构造器构造
                return ctor.Invoke(args);
            }

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                object[] args = ((IAdaptable)obj).GetAdaptedArray() ?? throw new NullReferenceException("给我Null我怎么写入？");

                int i = 0;
                while (i < Ctors.Count)
                {
                    if (CtorParams[i].Count == args.Length) break;
                    i++;
                }

                if (i == Ctors.Count) throw new InvalidCastException("没有符合参数数的构造器");

                if (Ctors.Count > 1) writer.WriteSignedByte((sbyte)i);

                var paramTypes = CtorParams[i];

                /*LogUtil.INSTANCE.PrintInfo("参数数：" + paramTypes.Count);
                foreach (Type tp in paramTypes) LogUtil.INSTANCE.PrintInfo("参数类型：" + tp);*/

                //mass.ItemBlobs[mass.SavedId].StickyId++;
                for (int i2 = 0; i2 < args.Length; i2++)
                {
                    Type item = paramTypes[i2];

                    //LogUtil.INSTANCE.PrintDebugInfo($"参数{i + 1}：{item}");
                    // TODO:读参数方法
                    object ob = args[i2];
                    try
                    {
                        //LogUtil.INSTANCE.PrintInfo($"写参数{i2 + 1}，预测类型：{item}，实际：{ob.GetType()}，值：{ob}");

                        if (item == typeof(int)) writer.WriteInt((int)ob);
                        else if (item == typeof(string)) writer.WrintString((string)ob);
                        else if (item == typeof(float)) writer.WriteFloat((float)ob);
                        else if (item == typeof(bool)) writer.WriteBoolean((bool)ob);
                        else mass.Write<object>(ob);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidCastException($"写入第{i2 + 1}号参数时错误，是因为" + ex.Message, ex);
                    }
                }
            }
        }

        public IAdapter Create(RyoType type)
        {
            if (!type.IsAdaptableCustom) throw new FormatException("非可适配自定义类型：" + type);

            var formatType = AdaptionManager.INSTANCE.GetCsClzByRyoType(type) ?? throw new NotSupportedException("不支持解析不了的自定义类型：" + type);

            // TODO:未来给普通类做的字段适配器

            try
            {
                var adapter = new CustomFormatAdapter(formatType.GetConstructors().Where(c => c.GetCustomAttribute<IAdaptable.AdaptableConstructor>() != null).OrderBy(c => c.GetParameters().Length).ToList());

                return adapter;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("在为类型" + type + "创建适配器时出错，因为" + ex.Message);
            }
        }

        public RyoType FindAdapterRyoTypeForDataRyoType(RyoType ryoType)
        {
            if (ryoType.IsAdaptableCustom && !ryoType.IsArray) return new() { Name = "sengine.mass.serializers.MassSerializableSerializer", BaseType = typeof(CustomFormatAdapterFactory) };
            else throw new InvalidDataException("类型不属于可适配自定义类型：" + ryoType);
        }
    }

    public class BaseTypeAdapterFactory : IAdapterFactory
    {
        public static readonly Dictionary<Type, RyoType> DataAdapterRyoTypePairs = new() {
            { typeof(int) ,new() {  Name = "sengine.mass.serializers.DefaultSerializers$IntSerializer", BaseType = typeof(IntAdapter) } },
            { typeof(string) ,new() {  Name = "sengine.mass.serializers.DefaultSerializers$StringSerializer", BaseType = typeof(StringAdapter) } },
        };

        public class IntAdapter : IAdapter
        {
            public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadInt();

            public void To(object obj, Mass mass, RyoWriter writer) => writer.WriteInt((int)obj);
        }

        public class StringAdapter : IAdapter
        {
            public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadString();

            public void To(object obj, Mass mass, RyoWriter writer) => writer.WrintString((string)obj);
        }

        public IAdapter Create(RyoType ryoType)
        {
            // 需要内联吗？
            // Type baseType = AdaptionManager.INSTANCE.GetCsClzByRyoType(ryoType) ?? throw new NullReferenceException("不能为不能解析的类型创建适配器：" + ryoType);
            if (!ryoType.IsArray && !ryoType.IsAdaptableCustom && !ryoType.IsUnidentifiedType)
            {
                foreach (var item in DataAdapterRyoTypePairs) if (ryoType.BaseType == item.Key) return (IAdapter)Activator.CreateInstance(item.Value.BaseType!)!;
            }
            throw new NotSupportedException("类型不属于基本类型或暂不支持：" + ryoType);
        }

        public RyoType FindAdapterRyoTypeForDataRyoType(RyoType ryoType)
        {
            // Type baseType = AdaptionManager.INSTANCE.GetCsClzByRyoType(ryoType) ?? throw new NullReferenceException("不能为不能解析的类型创建适配器：" + ryoType);
            if (!ryoType.IsArray && !ryoType.IsAdaptableCustom && !ryoType.IsUnidentifiedType)
            {
                foreach (var item in DataAdapterRyoTypePairs) if (ryoType.BaseType == item.Key) return item.Value;
            }
            throw new InvalidDataException("不属于基本类型或暂不支持：" + ryoType);
        }
    }

    public class BaseArrayTypeAdapterFactory : IAdapterFactory
    {
        public static readonly Dictionary<Type, RyoType> DataAdapterRyoTypePairs = new() {
            { typeof(int), new() { Name = "sengine.mass.serializers.DefaultArraySerializers$IntArraySerializer", BaseType = typeof(IntArrayAdapter) } },
            { typeof(byte), new() { Name = "sengine.mass.serializers.DefaultArraySerializers$ByteArraySerializer", BaseType = typeof(ByteArrayAdapter) } },
            { typeof(string), new() { Name = "sengine.mass.serializers.DefaultArraySerializers$StringArraySerializer", BaseType = typeof(StringArrayAdapter) } },
        };

        public class IntArrayAdapter : IAdapter
        {
            public object From(Mass mass, RyoReader reader, RyoType ryoType)
            {
                int i = reader.ReadInt();
                int[] iArr = new int[i];
                for (int i2 = 0; i2 < i; i2++)
                {
                    iArr[i2] = reader.ReadInt();
                }
                return iArr;
            }

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                int[] intArr = (int[])obj;
                writer.WriteInt(intArr.Length);
                foreach (var item in intArr) writer.WriteInt(item);
            }
        }

        public class ObjectArrayAdapter : IAdapter
        {
            public object From(Mass mass, RyoReader reader, RyoType ryoType)
            {
                // TODO:优化类型判断过程，和RyoType的GetSubitemRyoType有关

                var oriItemType = AdaptionManager.INSTANCE.GetCsClzByRyoType(ryoType)?.GetElementType();
                Type itemType = oriItemType ?? typeof(object);

                // 读个数
                Array objArr = Array.CreateInstance(itemType, reader.ReadInt());
                // LogUtil.INSTANCE.PrintInfo(ryoType + "列表类型：" + itemType + "、大小：" + objArr.Length);

                //mass.Reference(objArr);

                // 读子项
                for (int i = 0; i < objArr.Length; i++) objArr.SetValue(mass.Read<object>(), i);

                // 额外匹配类型
                if (oriItemType == null)
                {
                    if (objArr.Length > 0 && objArr.GetValue(0) != null) itemType = objArr.GetValue(0)!.GetType();
                    LogUtil.INSTANCE.PrintDebugInfo("额外重写匹配 类型为" + itemType);
                    if (itemType != typeof(object))
                    {
                        Array newArray = Array.CreateInstance(itemType, objArr.Length);

                        for (int i = 0; i < objArr.Length; i++)
                        {
                            object obj = objArr.GetValue(i)!; // 获取objArr中的当前项
                            object convertedObj = Convert.ChangeType(obj, itemType); // 将当前项转换为type类型
                            newArray.SetValue(convertedObj, i); // 将转换后的值赋值给新数组中的对应位置
                        }

                        return newArray;
                    }
                }

                return objArr;
            }

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                var objType = obj.GetType();
                if (!objType.IsArray) throw new InvalidDataException("什么！这不是数组，这是" + objType);
                //Type eleType = objType.GetElementType()!;
                Array objArr = (Array)obj;
                writer.WriteInt(objArr.Length);

                // mass.ItemBlobs[mass.SavedId].StickyId++;
                foreach (var item in objArr) mass.Write(item);
            }
        }

        public class ByteArrayAdapter : IAdapter
        {
            public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadBytes(reader.ReadInt());

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                byte[] objArr = (byte[])obj;
                writer.WriteInt(objArr.Length);
                writer.WriteBytes(objArr);
            }
        }

        public class StringArrayAdapter : IAdapter
        {
            public object From(Mass mass, RyoReader reader, RyoType ryoType)
            {
                int i = reader.ReadInt();
                var strArr = new string[i];
                for (int i2 = 0; i2 < i; i2++) strArr[i2] = reader.ReadString();
                return strArr;
            }

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                string[] strArr = (string[])obj;
                writer.WriteInt(strArr.Length);
                foreach (var item in strArr) writer.WrintString(item);
            }
        }

        public IAdapter Create(RyoType ryoType)
        {
            if (!ryoType.IsArray) throw new FormatException("不是列表：" + ryoType);

            foreach (var item in DataAdapterRyoTypePairs) if (ryoType.BaseType == item.Key) return (IAdapter)Activator.CreateInstance(item.Value.BaseType!)!;
            return new ObjectArrayAdapter();
        }

        public RyoType FindAdapterRyoTypeForDataRyoType(RyoType ryoType)
        {
            if (!ryoType.IsArray) throw new FormatException("不是列表：" + ryoType);

            foreach (var item in DataAdapterRyoTypePairs) if (ryoType.BaseType == item.Key) return item.Value;

            return new() { Name = "sengine.mass.serializers.DefaultArraySerializers$ObjectArraySerializer", BaseType = typeof(ObjectArrayAdapter) };
        }
    }

    // 待修正
    public class SpecialFormatAdapterFactory : IAdapterFactory
    {
        public static readonly Dictionary<Type, RyoType> DataAdapterRyoTypePairs = new() {
            {typeof(FragmentalImage), new() { Name = "sengine.graphics2d.texturefile.FIFormat", BaseType = typeof(FragmentalImageAdapter) } },
        };

        public class FragmentalImageAdapter : IAdapter
        {
            // public string JavaClz => "sengine.graphics2d.texturefile.FIFormat";

            // 计算分块数，按长度分割总数一共要多少块（不足的也给完整一块）
            public static int CalculateBlockCount(int fullLength, int blockLength)
            {
                int quotient = Math.DivRem(blockLength, fullLength, out int remainder);
                return (remainder != 0 ? 1 : 0) + quotient;
            }

            public object From(Mass mass, RyoReader reader, RyoType ryoType)
            {
                int jpgLength;// 保留

                int clipCount = reader.ReadInt();
                int sliceCount = reader.ReadInt();

                int[] sliceWidths = new int[sliceCount];
                int[] sliceHeights = new int[sliceCount];
                RyoPixmap[][] pixmaps = new RyoPixmap[sliceCount][];

                // 每层
                for (int i2 = 0; i2 < sliceCount; i2++)
                {
                    try
                    {
                        // 每层的宽高
                        int sliceWidth = reader.ReadInt();
                        sliceWidths[i2] = sliceWidth;
                        int sliceHeight = reader.ReadInt();
                        sliceHeights[i2] = sliceHeight;

                        //每层的格式
                        RyoPixmap.FORMAT format = (RyoPixmap.FORMAT)reader.ReadUnsignedByte();
                        bool isJPG = format == RyoPixmap.FORMAT.RGB888 || format == RyoPixmap.FORMAT.RGB565;

                        // 块数
                        int sliceBlockCount = CalculateBlockCount(clipCount, sliceWidth);
                        // 片数
                        int sliceClipCount = sliceBlockCount * CalculateBlockCount(clipCount, sliceHeight);

                        // 每层块数
                        pixmaps[i2] = new RyoPixmap[sliceClipCount];

                        for (int i3 = 0; i3 < sliceClipCount; i3++)
                        {
                            int x = (i3 % sliceBlockCount) * clipCount;
                            int y = (i3 / sliceBlockCount) * clipCount;
                            int right = x + clipCount;
                            int bottom = y + clipCount;

                            // 处理宽高
                            if (right > sliceWidth) right = sliceWidth;

                            if (bottom > sliceHeight) bottom = sliceHeight;

                            // 是否JPG
                            if (isJPG && (jpgLength = reader.ReadInt()) > 0)
                            {
                                // 是JPG
                                var buffer = reader.ReadBytes(jpgLength);
                                pixmaps[i2][i3] = new(buffer);
                            }
                            else
                            {
                                // 不是JPG
                                RyoPixmap pixmap = new(right - x, bottom - y, format);
                                pixmap.Pixels = reader.ReadBytes(pixmap.Pixels.Length);
                                pixmaps[i2][i3] = pixmap;
                            }
                        }
                    }
                    catch (Exception th)
                    {
                        //LogUtil.INSTANCE.PrintError("读取失败 将回收内存", th);

                        foreach (RyoPixmap[] v1 in pixmaps)
                        {
                            if (v1 == null) continue;

                            foreach (RyoPixmap v in v1) v?.Dispose();
                        }
                        throw new InvalidDataException("读取块时出现异常，因为" + th.Message, th);
                    }
                }
                return new FragmentalImage(clipCount, sliceWidths, sliceHeights, pixmaps);
            }

            // 实验性
            public void To(object obj, Mass mass, RyoWriter writer)
            {
                var image = (FragmentalImage)obj;
                // RyoWriter? directClipWritter = null;

                // 元数据
                writer.WriteInt(image.MaxClipSize);
                writer.WriteInt(image.RyoPixmaps.Length);

                for (int i = 0; i < image.RyoPixmaps.Length; i++)
                {
                    // 每层数据
                    RyoPixmap[] pixmapArr = image.RyoPixmaps[i];
                    writer.WriteInt(image.SliceWidths[i]);
                    writer.WriteInt(image.SliceHeights[i]);
                    RyoPixmap.FORMAT format = pixmapArr[0].Format;

                    // 写出类型序号
                    writer.WriteUnsignedByte((byte)format);
                    bool isJPG = pixmapArr[0].IsJPG;

                    // 每块数据
                    foreach (RyoPixmap pixmap in pixmapArr)
                    {
                        // JPG
                        if (isJPG)
                        {
                            // 直接长度
                            writer.WriteInt(pixmap.Pixels.Length);
                            writer.WriteBytes(pixmap.Pixels);
                        }
                        else
                        {
                            //这块原来是不括起来的，警告一下怕有问题！！
                            writer.WriteBytes(pixmap.Pixels);
                        }
                    }
                }

            }

            // 实验性
            /*public void WriteAsJpegToGivenWriter(RyoPixmap pixmap, RyoWriter anoWriter)
            {
                try
                {
                    var bmp = pixmap.ToImage() ?? throw new NullReferenceException("转换图片失败，一得阁拉米");

                    var streamHere = new MemoryStream();
                    bmp.Save(streamHere, new JpegEncoder());
                    anoWriter.WriteBytes(streamHere.ToArray());
                }
                catch (Exception th)
                {
                    LogUtil.INSTANCE.PrintError("Unable to convert image to JPEG", th);
                    anoWriter.PositionToZero();
                }
            }*/
        }

        public IAdapter Create(RyoType type)
        {
            //if (!type.IsCustom) throw new FormatException(type + "非自定义类型");

            foreach (var item in DataAdapterRyoTypePairs) if (type.BaseType == item.Key) return (IAdapter)Activator.CreateInstance(item.Value.BaseType!)!;

            throw new FormatException(type + "没有合适的特殊类型适配器");
        }

        public RyoType FindAdapterRyoTypeForDataRyoType(RyoType ryoType)
        {
            foreach (var item in DataAdapterRyoTypePairs) if (ryoType.BaseType == item.Key) return item.Value;

            throw new FormatException(ryoType + "找不到合适的特殊类型适配器");
        }
    }
}
