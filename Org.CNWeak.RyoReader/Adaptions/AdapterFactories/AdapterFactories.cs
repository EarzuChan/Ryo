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

namespace Me.EarzuChan.Ryo.Adaptions.AdapterFactories
{
    public interface IAdapterFactory
    {
        IAdapter Create(RyoType type);

        //string? FindAdapterForRyoType
    }

    public class CustomFormatAdapterFactory : IAdapterFactory
    {
        public class CustomFormatAdapter : IAdapter
        {
            private readonly List<ConstructorInfo> Ctors = new();
            private readonly List<List<Type>> CtorParams = new();

            public CustomFormatAdapter(List<ConstructorInfo> constructorInfos)
            {
                Ctors = constructorInfos;

                if (Ctors == null || Ctors.Count == 0) throw new NullReferenceException("构造器不满足条件");

                // 遍历参数
                foreach (var c in Ctors)
                {
                    List<Type> paramTypes = new();
                    foreach (var p in c.GetParameters()) paramTypes.Add(p.ParameterType);
                    CtorParams.Add(paramTypes);
                }
            }

            string IAdapter.JavaClz => "sengine.mass.serializers.MassSerializableSerializer";

            public object From(Mass mass, RyoReader reader, RyoType ryoType)
            {
                ConstructorInfo? ctor = null;
                List<Type>? paramTypes = null;
                if (Ctors.Count > 1)
                {
                    int ctorId = reader.ReadUnsignedByte();
                    ctor = Ctors[reader.ReadUnsignedByte()];
                    paramTypes = CtorParams[ctorId];
                }
                else
                {
                    ctor = Ctors[0];
                    paramTypes = CtorParams[0];
                }

                object[] args = new object[paramTypes.Count];
                for (int i = 0; i < paramTypes.Count; i++)
                {
                    Type item = paramTypes[i];

                    LogUtil.INSTANCE.PrintDebugInfo($"参数{i + 1}：{item}");
                    // TODO:读参数方法
                    if (item == typeof(int)) args[i] = reader.ReadInt();
                    else if (item == typeof(string)) args[i] = reader.ReadString();
                    else if (item == typeof(float)) args[i] = reader.ReadFloat();
                    else if (item == typeof(bool)) args[i] = reader.ReadBoolean();
                    else args[i] = mass.Read<object>();
                }

                return ctor.Invoke(args);
            }

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public IAdapter Create(RyoType type)
        {
            if (!type.IsAdaptableCustom) throw new FormatException("非自定义格式" + type);

            var formatType = AdaptionManager.INSTANCE.GetCsClzByRyoType(type);

            if (formatType == null) throw new NotSupportedException("暂不支持该自定义类型" + type);
            // TODO:未来给普通类做的字段适配器

            try
            {
                var adapter = new CustomFormatAdapter(formatType.GetConstructors().Where(c => c.GetCustomAttribute<IAdaptable.AdaptableConstructor>() != null).OrderBy(c => c.GetParameters().Length).ToList());

                return adapter;
            }
            catch (Exception ex)
            {
                throw new Exception("为类型" + type + "创建适配器时出错，因为" + ex.Message);
            }
        }
    }

    public class BaseTypeAdapterFactory : IAdapterFactory
    {
        public class IntAdapter : IAdapter
        {
            public string JavaClz => "sengine.mass.serializers.DefaultSerializers$IntSerializer";

            public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadInt();

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public class StringAdapter : IAdapter
        {
            public string JavaClz => "sengine.mass.serializers.DefaultSerializers$StringSerializer";

            public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadString();

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public IAdapter Create(RyoType type)
        {
            // 需要内联吗？
            Type? baseType = type.BaseType;

            if (baseType == typeof(int)) return new IntAdapter();
            else if (baseType == typeof(string)) return new StringAdapter();
            else throw new NotSupportedException("暂不支持" + type);
        }
    }

    public class BaseArrayTypeAdapterFactory : IAdapterFactory
    {
        public class IntArrayAdapter : IAdapter
        {
            public string JavaClz => "sengine.mass.serializers.DefaultArraySerializers$IntArraySerializer";

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
                throw new NotImplementedException();
            }
        }

        public class ObjectArrayAdapter : IAdapter
        {
            public string JavaClz => "sengine.mass.serializers.DefaultArraySerializers$ObjectArraySerializer";

            public object From(Mass mass, RyoReader reader, RyoType ryoType)
            {
                // TODO:优化类型判断过程，和RyoType的GetSubitemRyoType有关

                var oriItemType = AdaptionManager.INSTANCE.GetCsClzByRyoType(ryoType)?.GetElementType();
                Type itemType = oriItemType ?? typeof(object);

                Array objArr = Array.CreateInstance(itemType, reader.ReadInt());
                // LogUtil.INSTANCE.PrintInfo(ryoType + "列表类型：" + itemType + "、大小：" + objArr.Length);

                //mass.Reference(objArr);

                for (int i = 0; i < objArr.Length; i++) objArr.SetValue(mass.Read<object>(), i);

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
                throw new NotImplementedException();
            }
        }

        public class ByteArrayAdapter : IAdapter
        {
            public string JavaClz => "sengine.mass.serializers.DefaultArraySerializers$ByteArraySerializer";

            public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadBytes(reader.ReadInt());

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public class StringArrayAdapter : IAdapter
        {
            public string JavaClz => "sengine.mass.serializers.DefaultArraySerializers$StringArraySerializer";

            public object From(Mass mass, RyoReader reader, RyoType ryoType)
            {
                int i = reader.ReadInt();
                var strArr = new string[i];
                for (int i2 = 0; i2 < i; i2++) strArr[i2] = reader.ReadString();
                return strArr;
            }

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public IAdapter Create(RyoType type)
        {
            if (!type.IsArray) throw new FormatException(type + "不是列表");
            Type? baseType = type.BaseType;

            if (baseType == typeof(int)) return new IntArrayAdapter();
            else if (baseType == typeof(byte)) return new ByteArrayAdapter();
            else if (baseType == typeof(string)) return new StringArrayAdapter();
            else return new ObjectArrayAdapter();
        }
    }

    public class SpecialFormatAdapterFactory : IAdapterFactory
    {
        public class FragmentalImageAdapter : IAdapter
        {
            public string JavaClz => "sengine.graphics2d.texturefile.FIFormat";

            // 计算分块数，按长度分割总数一共要多少块（不足的也给完整一块）
            public static int CalculateBlockCount(int fullLength, int blockLength)
            {
                int quotient = Math.DivRem(blockLength, fullLength, out int remainder);
                return (remainder != 0 ? 1 : 0) + quotient;
            }

            public object From(Mass mass, RyoReader reader, RyoType ryoType)
            {
                int directLength;// 保留

                int clipCount = reader.ReadInt();
                int sliceCount = reader.ReadInt();

                int[] sliceWidths = new int[sliceCount];
                int[] sliceHeights = new int[sliceCount];
                RyoPixmap[][] pixmaps = new RyoPixmap[sliceCount][];

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
                        bool isUnshaped = format == RyoPixmap.FORMAT.RGB888 || format == RyoPixmap.FORMAT.RGB565;

                        // 块数
                        int sliceBlockCount = CalculateBlockCount(clipCount, sliceWidths[i2]);
                        // 片数
                        int sliceClipCount = sliceBlockCount * CalculateBlockCount(clipCount, sliceHeights[i2]);

                        // 每层块数
                        pixmaps[i2] = new RyoPixmap[sliceClipCount];

                        for (int i3 = 0; i3 < sliceClipCount; i3++)
                        {
                            int x = (i3 % sliceBlockCount) * clipCount;
                            int y = (i3 / sliceBlockCount) * clipCount;
                            int right = x + clipCount;
                            int bottom = y + clipCount;

                            // 处理宽高
                            if (right > sliceWidth)
                            {
                                right = sliceWidth;
                            }
                            if (bottom > sliceHeight)
                            {
                                bottom = sliceHeight;
                            }

                            // 归不规则的读
                            if (!isUnshaped || (directLength = reader.ReadInt()) <= 0)
                            {
                                // 规则的不需要指定大小
                                RyoPixmap pixmap = new(right - x, bottom - y, format);
                                pixmap.Pixels = reader.ReadBytes(pixmap.Pixels.Length);
                                pixmaps[i2][i3] = pixmap;
                            }
                            else
                            {
                                // 需要指定大小
                                pixmaps[i2][i3] = new(reader.ReadBytes(directLength)) { Format = format, Height = sliceHeight, Width = sliceWidth };
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
                        throw new InvalidDataException("读取块时出现异常", th);
                    }
                }
                return new FragmentalImage(clipCount, sliceWidths, sliceHeights, pixmaps);
            }

            // 实验性
            public void To(object obj, Mass mass, RyoWriter writer)
            {
                var image = (FragmentalImage)obj;

                // 不知道是什么
                /*if (r12.f2426a > 0 || r12.f2427b > 0)
                {
                    throw new IllegalStateException("Cannot serialize partially loaded image data");
                }*/

                RyoWriter? directClipWritter = null;

                // 元数据
                writer.WriteInt(image.ClipCount);
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
                    bool isUnshaped = format == RyoPixmap.FORMAT.RGB888 || format == RyoPixmap.FORMAT.RGB565;

                    // 每块数据
                    foreach (RyoPixmap pixmap in pixmapArr)
                    {
                        // 感觉逻辑有问题

                        // 不规则
                        if (isUnshaped)
                        {
                            // 图片大于32*32
                            if (pixmap.Width * pixmap.Height >= 1024)
                            {
                                directClipWritter ??= new RyoWriter(new MemoryStream(65536));
                                directClipWritter.PositionToZero();
                                WriteAsJpegToGivenWriter(pixmap, directClipWritter);

                                if (directClipWritter.Position > 0)// 写出的有效
                                {
                                    // 直接长度
                                    writer.WriteInt((int)directClipWritter.Position);
                                    writer.WriteAnotherWriter(directClipWritter);

                                    // 问题是没退出？
                                }
                            }

                            // 写大小-1 -> 当作规则的来读取
                            writer.WriteInt(-1);
                        }

                        // 规则，就不指定大小
                        var pixels = pixmap.Pixels;
                        writer.WriteBytes(pixels);
                    }
                }

            }

            // 实验性
            public void WriteAsJpegToGivenWriter(RyoPixmap pixmap, RyoWriter anoWriter)
            {
                try
                {
                    var bmp = (Bitmap)pixmap;

                    var streamHere = new MemoryStream();
                    bmp!.Save(streamHere, ImageFormat.Jpeg);
                    anoWriter.WriteBytes(streamHere.ToArray());
                }
                catch (Exception th)
                {
                    LogUtil.INSTANCE.PrintError("Unable to convert image to JPEG", th);
                    anoWriter.PositionToZero();
                }
            }
        }

        public IAdapter Create(RyoType type)
        {
            //if (!type.IsCustom) throw new FormatException(type + "非自定义类型");

            return type.Name switch
            {
                "sengine.graphics2d.texturefile.FIFormat" => new FragmentalImageAdapter(),
                _ => throw new FormatException(type + "没有合适的适配器"),
            };
        }
    }
}
