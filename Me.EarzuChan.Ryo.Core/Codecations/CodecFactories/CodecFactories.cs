using Me.EarzuChan.Ryo.Core.Formations.HelperFormations;
using Me.EarzuChan.Ryo.Core.IO;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Core.Utils;
using System.Reflection;

namespace Me.EarzuChan.Ryo.Core.Codecations.CodecFactories;

public interface ICodecFactory {
    ICodec CreateCodecForDataRyoType(RyoType type);

    RyoType FindCodecRyoTypeForDataRyoType(RyoType ryoType);
}

// 说明：真的智将来的，根据写出的多少个参数（Out方法的输出长度可以内部判断）来对于多长的构造器！高！师爷！
public class CustomFormatCodecFactory : ICodecFactory {
    public class CustomFormatCtorCodec : ICodec {
        private readonly List<ConstructorInfo> _ctors;
        private readonly List<List<Type>> _ctorParams = [];

        public CustomFormatCtorCodec(List<ConstructorInfo> constructorInfos) {
            _ctors = constructorInfos;

            if (_ctors == null || _ctors.Count == 0) throw new NullReferenceException("构造器不满足条件，为Null或没有、没有、没有");

            // 遍历参数
            foreach (var c in _ctors) {
                List<Type> paramTypes = new();
                foreach (var p in c.GetParameters()) paramTypes.Add(p.ParameterType);
                _ctorParams.Add(paramTypes);
            }
        }

        public object From(Mass mass, RyoReader reader, RyoType ryoType) {
            ConstructorInfo? ctor = null;
            List<Type>? paramTypes = null;

            // 瞄准构造器且匹配参数
            if (_ctors.Count > 1) {
                int ctorId = reader.ReadSignedByte();
                ctor = _ctors[reader.ReadSignedByte()];
                paramTypes = _ctorParams[ctorId];
            } else {
                ctor = _ctors[0];
                paramTypes = _ctorParams[0];
            }

            // 一个一个参数来读取
            object?[] args = new object[paramTypes.Count];
            for (int i = 0; i < paramTypes.Count; i++) {
                Type itemType = paramTypes[i];
                // 读参数方法
                try {
                    args[i] = ReadWriteTypesUtils.Read(itemType, reader, mass);
                } catch (Exception ex) {
                    throw new InvalidCastException($"读取第{i + 1}号参数时错误，是因为" + ex.Message, ex);
                }
            }

            // 使用构造器构造
            return ctor.Invoke(args);
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            object[] args = ((ICtorCodecable)obj).GetCodecatedArray() ??
                            throw new NullReferenceException($"该类型{obj.GetType()}暂不能构建适配列表");

            int matchedCtorIndex;
            for (matchedCtorIndex = 0; matchedCtorIndex < _ctors.Count && _ctorParams[matchedCtorIndex].Count != args.Length; matchedCtorIndex++) ;

            if (matchedCtorIndex == _ctors.Count) throw new InvalidCastException("适配列表的参数不符合任意构造器参数");

            if (_ctors.Count > 1) writer.WriteSignedByte((sbyte)matchedCtorIndex);

            var paramTypes = _ctorParams[matchedCtorIndex];
            
            for (int i = 0; i < args.Length; i++) {
                Type itemType = paramTypes[i];

                object item = args[i];
                try {
                    ReadWriteTypesUtils.Write(itemType, item, writer, mass);
                } catch (Exception ex) {
                    throw new InvalidCastException($"写入第{i + 1}号参数（{item.GetType}）时错误，是因为" + ex.Message, ex);
                }
            }
        }
    }

    public class CustomFormatFieldCodec : ICodec {
        private readonly FieldInfo[] FieldInfos;
        private readonly Type ObjectType;

        public object? From(Mass mass, RyoReader reader, RyoType ryoType) {
            object? item = Activator.CreateInstance(ObjectType)!;
            // 所以顺序必须按照Java 紧密排序！
            for (int i = 0; i < FieldInfos.Length; i++) {
                FieldInfo field = FieldInfos[i];
                try {
                    object? value = ReadWriteTypesUtils.Read(field.FieldType, reader, mass, true);
                    field.SetValue(item, value);
                } catch (Exception ex) {
                    throw new FieldAccessException($"读取第 {i + 1} 字段 [{field.Name}] 对象 {field.FieldType} 时出错", ex);
                }
            }

            return item;
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            for (int i = 0; i < FieldInfos.Length; i++) {
                FieldInfo field = FieldInfos[i];
                try {
                    ReadWriteTypesUtils.Write(field.FieldType, field.GetValue(obj)!, writer, mass, true);
                } catch (Exception ex) {
                    throw new FieldAccessException($"写入第 {i + 1} 个字段 [{field.Name}]：{field.FieldType} 时出错，对象：{obj}", ex);
                }
            }
        }

        public CustomFormatFieldCodec(Type type) {
            ObjectType = type;
            // FieldInfo[] declaredFields;
            if (type.IsInterface) {
                FieldInfos = Array.Empty<FieldInfo>();
                return;
            }

            try {
                type.GetConstructor(Type.EmptyTypes);
                List<FieldInfo> fullList = new();
                List<FieldInfo> loopingList = new();

                // 整理每个层级字段们
                while (type != typeof(object)) {
                    foreach (FieldInfo field in type.GetFields()) {
                        var modifiers = field.Attributes;
                        if ((modifiers & FieldAttributes.NotSerialized) == 0 &&
                            (modifiers & FieldAttributes.Static) == 0 &&
                            !field.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))) {
                            if (!field.IsPublic) {
                                // 也没有关系，可以读取
                            }

                            loopingList.Add(field);
                        }
                    }

                    fullList.AddRange(loopingList.OrderBy(field => field.Name, StringComparer.Ordinal));
                    loopingList.Clear();

                    if (type.BaseType == null) break;
                    type = type.BaseType;
                }

                FieldInfos = fullList.ToArray();
            } catch (Exception) {
                throw new MissingFieldException($"{type}：没有可访问的无参数构造函数 不能嗯造字段填充适配器");
            }
        }
    }

    public ICodec CreateCodecForDataRyoType(RyoType type) {
        if (!type.IsCodecableCustom) throw new FormatException("非可适配自定义类型：" + type);

        var formatType = type.ToCsType() ?? throw new NotSupportedException("不支持解析不了的自定义类型：" + type);

        try {
            ICodec codec = type.IsCodecateWithCtor
                ? new CustomFormatCtorCodec(formatType.GetConstructors()
                    .Where(c => c.GetCustomAttribute<ICtorCodecable.CodecableConstructor>() != null)
                    .OrderBy(c => c.GetParameters().Length).ToList())
                : new CustomFormatFieldCodec(formatType);

            return codec;
        } catch (Exception ex) {
            throw new InvalidDataException("在为类型" + type + "创建适配器时出错，因为" + ex.Message);
        }
    }

    public RyoType FindCodecRyoTypeForDataRyoType(RyoType ryoType) {
        if (ryoType is { IsCodecableCustom: true, IsArray: false })
            return new() {
                JavaClassName = ryoType.IsCodecateWithCtor ? "sengine.mass.serializers.MassSerializableSerializer" : "sengine.mass.serializers.FieldSerializer",
                CsType = typeof(CustomFormatCodecFactory)
            };
        else throw new InvalidDataException("类型不属于可适配自定义类型：" + ryoType);
    }
}

public class NormalTypeCodecFactory : ICodecFactory {
    public static readonly Dictionary<Type, RyoType> DataCodecRyoTypePairs = new() {
        {
            typeof(int),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultSerializers$IntSerializer", CsType = typeof(IntCodec)
            }
        }, {
            typeof(bool),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultSerializers$IntSerializer",
                CsType = typeof(BooleanCodec)
            }
        }, {
            typeof(string),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultSerializers$StringSerializer",
                CsType = typeof(StringCodec)
            }
        }, {
            typeof(Enum),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultSerializers$EnumSerializer",
                CsType = typeof(EnumCodec)
            }
        }, // TODO：这里也还能有活，但我依旧发扬伟大偷懒精神
    };

    public class BooleanCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadBoolean();

        public void To(object obj, Mass mass, RyoWriter writer) => writer.WriteBoolean((bool)obj);
    }

    public class IntCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadInt();

        public void To(object obj, Mass mass, RyoWriter writer) => writer.WriteInt((int)obj);
    }

    public class StringCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadString();

        public void To(object obj, Mass mass, RyoWriter writer) => writer.WriteString((string)obj);
    }

    public class EnumCodec : ICodec {
        public object? From(Mass mass, RyoReader reader, RyoType type) {
            bool isEnum = type.CsType!.IsEnum;
            Type enumType = type.CsType!;
            if (!isEnum) enumType = type.CsType;
            return ((Enum?[])Enum.GetValues(enumType))[reader.ReadInt()];
        }


        public void To(object balue, Mass r2, RyoWriter writer) {
            writer.WriteInt((int)balue);
        }
    }

    public ICodec CreateCodecForDataRyoType(RyoType ryoType) {
        // 需要内联吗？
        if (ryoType.IsArray || ryoType.IsCodecableCustom || ryoType.IsCsTypeUnidentified)
            throw new NotSupportedException("类型不属于基本类型或暂不支持：" + ryoType);

        foreach (var item in DataCodecRyoTypePairs.Where(item => ryoType.CsType == item.Key))
            return (ICodec)Activator.CreateInstance(item.Value.CsType!)!;

        throw new NotSupportedException("类型不属于基本类型或暂不支持：" + ryoType);
    }

    public RyoType FindCodecRyoTypeForDataRyoType(RyoType ryoType) {
        if (ryoType.IsArray || ryoType.IsCodecableCustom || ryoType.IsCsTypeUnidentified)
            throw new InvalidDataException("不属于基本类型或暂不支持：" + ryoType);

        foreach (var item in DataCodecRyoTypePairs.Where(item => ryoType.CsType == item.Key))
            return item.Value;

        throw new InvalidDataException("不属于基本类型或暂不支持：" + ryoType);
    }
}

public class BaseArrayTypeCodecFactory : ICodecFactory {
    public static readonly Dictionary<Type, RyoType> DataCodecRyoTypePairs = new() {
        {
            typeof(int),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$IntArraySerializer",
                CsType = typeof(IntArrayCodec)
            }
        }, {
            typeof(float),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$FloatArraySerializer",
                CsType = typeof(FloatArrayCodec)
            }
        }, {
            typeof(short),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$ShortArraySerializer",
                CsType = typeof(ShortArrayCodec)
            }
        }, {
            typeof(byte),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$ByteArraySerializer",
                CsType = typeof(ByteArrayCodec)
            }
        }, {
            typeof(bool),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$BooleanArraySerializer",
                CsType = typeof(BooleanArrayCodec)
            }
        }, {
            typeof(string),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$StringArraySerializer",
                CsType = typeof(StringArrayCodec)
            }
        },{ // 新增：Long
            typeof(long),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$LongArraySerializer",
                CsType = typeof(LongArrayCodec)
            }
        }, { // 新增：Double
            typeof(double),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$DoubleArraySerializer",
                CsType = typeof(DoubleArrayCodec)
            }
        }, { // 新增：Char
            typeof(char),
            new() {
                JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$CharArraySerializer",
                CsType = typeof(CharArrayCodec)
            }
        }
    };

    public class IntArrayCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) {
            int length = reader.ReadInt();
            int[] array = new int[length];
            for (int index = 0; index < length; index++) array[index] = reader.ReadInt();
            return array;
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            int[] array = (int[])obj;
            writer.WriteInt(array.Length);
            foreach (var item in array) writer.WriteInt(item);
        }
    }

    public class ShortArrayCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) {
            int length = reader.ReadInt();
            short[] array = new short[length];
            for (int index = 0; index < length; index++) array[index] = reader.ReadShort();
            return array;
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            short[] array = (short[])obj;
            writer.WriteInt(array.Length);
            foreach (var item in array) writer.WriteShort(item);
        }
    }

    public class FloatArrayCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) {
            int length = reader.ReadInt();
            float[] array = new float[length];
            for (int index = 0; index < length; index++) array[index] = reader.ReadFloat();
            return array;
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            float[] array = (float[])obj;
            writer.WriteInt(array.Length);
            foreach (var item in array) writer.WriteFloat(item);
        }
    }

    // 新增：LongArrayCodec
    public class LongArrayCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) {
            int length = reader.ReadInt();
            long[] array = new long[length];
            for (int index = 0; index < length; index++) array[index] = reader.ReadLong();
            return array;
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            long[] array = (long[])obj;
            writer.WriteInt(array.Length);
            foreach (var item in array) writer.WriteLong(item);
        }
    }

    // 新增：DoubleArrayCodec
    public class DoubleArrayCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) {
            int length = reader.ReadInt();
            double[] array = new double[length];
            for (int index = 0; index < length; index++) array[index] = reader.ReadDouble();
            return array;
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            double[] array = (double[])obj;
            writer.WriteInt(array.Length);
            foreach (var item in array) writer.WriteDouble(item);
        }
    }

    // 新增：CharArrayCodec
    public class CharArrayCodec : ICodec {
        public object? From(Mass mass, RyoReader reader, RyoType ryoType) {
            int length = reader.ReadInt();
            char[] array = new char[length];
            for (int index = 0; index < length; index++) array[index] = reader.ReadChar();
            return array;
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            char[] array = (char[])obj;
            writer.WriteInt(array.Length);
            foreach (var item in array) writer.WriteChar(item);
        }
    }

    public class ByteArrayCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadBytes(reader.ReadInt());

        public void To(object obj, Mass mass, RyoWriter writer) {
            byte[] array = (byte[])obj;
            writer.WriteInt(array.Length);
            writer.WriteBytes(array);
        }
    }

    public class BooleanArrayCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) {
            int length = reader.ReadInt();
            bool[] array = new bool[length];
            for (int index = 0; index < length; index++) array[index] = reader.ReadBoolean();
            return array;
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            bool[] array = (bool[])obj;
            writer.WriteInt(array.Length);
            foreach (var item in array) writer.WriteBoolean(item);
        }
    }

    public class StringArrayCodec : ICodec {
        public object? From(Mass mass, RyoReader reader, RyoType ryoType) {
            int length = reader.ReadInt();
            string?[] array = new string[length];
            for (int index = 0; index < length; index++) array[index] = reader.ReadString();
            return array;
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            string?[] array = (string?[])obj;
            writer.WriteInt(array.Length);
            foreach (var item in array) writer.WriteString(item); 
        }
    }

    public class ObjectArrayCodec : ICodec {
        public object From(Mass mass, RyoReader reader, RyoType ryoType) {
            var oriItemType = ryoType.GetArrayElementRyoType().CsType; // OLD：ryoType.ToCsType()?.GetElementType();
            var itemType = oriItemType ?? typeof(object);

            int length = reader.ReadInt();
            var objArray = Array.CreateInstance(itemType, length);

            for (var index = 0; index < length; index++) objArray.SetValue(mass.Read<object>(), index);

            // 额外匹配类型并重写数组
            if (oriItemType == null) {
                if (length > 0 && objArray.GetValue(0) != null) itemType = objArray.GetValue(0)!.GetType();
                
                if (itemType == typeof(object)) return objArray;
                
                var stronglyTypedArray = Array.CreateInstance(itemType, length);
                for (var index = 0; index < length; index++) {
                    var item = objArray.GetValue(index);
                    // 增加对 null 的安全处理
                    var convertedItem = item == null ? null : Convert.ChangeType(item, itemType);
                    stronglyTypedArray.SetValue(convertedItem, index);
                }

                return stronglyTypedArray; // CHECK：如果是啥都能放的ArrayList不炸了？还是只有具类型的数组？
            }

            return objArray;
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            var objType = obj.GetType();
            if (!objType.IsArray) throw new InvalidDataException($"什么！这不是数组，这是：{objType}");
            
            Array objArray = (Array)obj;
            writer.WriteInt(objArray.Length);

            foreach (var item in objArray) mass.Write(item);
        }
    }

    public ICodec CreateCodecForDataRyoType(RyoType ryoType) {
        if (!ryoType.IsArray) throw new FormatException($"不是列表：{ryoType}");

        // 优化：使用 TryGetValue 替代 foreach 遍历，时间复杂度从 O(N) 降到 O(1)
        if (ryoType.CsType != null && DataCodecRyoTypePairs.TryGetValue(ryoType.CsType, out var codecData)) return (ICodec)Activator.CreateInstance(codecData.CsType!)!;
        
        return new ObjectArrayCodec();
    }

    public RyoType FindCodecRyoTypeForDataRyoType(RyoType ryoType) {
        if (!ryoType.IsArray) throw new FormatException($"不是列表：{ryoType}");

        // 优化：使用 TryGetValue 替代 foreach 遍历
        if (ryoType.CsType != null && DataCodecRyoTypePairs.TryGetValue(ryoType.CsType, out var codecData)) return codecData;

        return new() {
            JavaClassName = "sengine.mass.serializers.DefaultArraySerializers$ObjectArraySerializer",
            CsType = typeof(ObjectArrayCodec)
        };
    }
}

public class SpecialFormatCodecFactory : ICodecFactory {
    public static readonly Dictionary<Type, RyoType> DataCodecRyoTypePairs = new() {
        {
            typeof(FragmentalImage),
            new() { JavaClassName = "sengine.graphics2d.texturefile.FIFormat", CsType = typeof(FragmentalImageCodec) }
        },
    };

    public class FragmentalImageCodec : ICodec {
        public object? From(Mass mass, RyoReader reader, RyoType ryoType) {
            int jpgLength; // 保留

            int clipSize = reader.ReadInt();
            int levelCount = reader.ReadInt();

            int[] levelWidths = new int[levelCount];
            int[] levelHeights = new int[levelCount];
            RyoPixmap[][] pixmaps = new RyoPixmap[levelCount][];

            // 每层
            for (int levelIndex = 0; levelIndex < levelCount; levelIndex++) {
                try {
                    // 每层的宽高
                    int levelWidth = reader.ReadInt();
                    levelWidths[levelIndex] = levelWidth;
                    int levelHeight = reader.ReadInt();
                    levelHeights[levelIndex] = levelHeight;

                    //每层的格式
                    RyoPixmap.FORMAT format = (RyoPixmap.FORMAT)reader.ReadUnsignedByte();
                    bool isJPG = format is RyoPixmap.FORMAT.RGB888 or RyoPixmap.FORMAT.RGB565;

                    // 横切数
                    int levelWidthClipCount = TextureUtils.CalculateClipCount(clipSize, levelWidth);

                    // 纵切数
                    int levelHeightClipCount = TextureUtils.CalculateClipCount(clipSize, levelHeight);

                    // 每层单元格数
                    int levelClipBlobCount = levelWidthClipCount * levelHeightClipCount;

                    // 每层单元格图片承载者
                    pixmaps[levelIndex] = new RyoPixmap[levelClipBlobCount];

                    for (int levelClipBlobIndex = 0; levelClipBlobIndex < levelClipBlobCount; levelClipBlobIndex++) {
                        // XXYY计算不正确导致的黑屏？
                        int x = (levelClipBlobIndex % levelWidthClipCount) * clipSize;
                        int y = (levelClipBlobIndex / levelWidthClipCount) * clipSize;
                        int right = x + clipSize;
                        int bottom = y + clipSize;

                        // 处理宽高
                        if (right > levelWidth) right = levelWidth;

                        if (bottom > levelHeight) bottom = levelHeight;

                        // 是否JPG
                        if (isJPG && (jpgLength = reader.ReadInt()) > 0) {
                            // 是JPG
                            var buffer = reader.ReadBytes(jpgLength);
                            pixmaps[levelIndex][levelClipBlobIndex] = new(buffer);
                        } else {
                            // 不是JPG
                            RyoPixmap pixmap = new(right - x, bottom - y, format);
                            pixmap.Pixels = reader.ReadBytes(pixmap.Pixels.Length);
                            pixmaps[levelIndex][levelClipBlobIndex] = pixmap;
                        }
                    }
                } catch (Exception th) {
                    //LogUtil.INSTANCE.PrintError("读取失败 将回收内存", th);

                    foreach (RyoPixmap[] v1 in pixmaps) {
                        if (v1 == null) continue;

                        foreach (RyoPixmap v in v1) v?.Dispose();
                    }

                    throw new InvalidDataException("读取块时出现异常，因为" + th.Message, th);
                }
            }

            return new FragmentalImage(clipSize, levelWidths, levelHeights, pixmaps);
        }

        public void To(object obj, Mass mass, RyoWriter writer) {
            var image = (FragmentalImage)obj;
            // RyoWriter? directClipWritter = null;

            // 元数据
            writer.WriteInt(image.ClipSize);
            writer.WriteInt(image.RyoPixmaps.Length);

            for (int i = 0; i < image.RyoPixmaps.Length; i++) {
                // 每层数据
                // XXYY计算不正确导致的黑屏？
                RyoPixmap[] pixmapArr = image.RyoPixmaps[i];
                writer.WriteInt(image.LevelWidths[i]);
                writer.WriteInt(image.LevelHeights[i]);
                RyoPixmap.FORMAT format = pixmapArr[0].Format;

                // 写出类型序号
                writer.WriteUnsignedByte((byte)format);
                bool isJPG = pixmapArr[0].IsJPG;

                // 每块数据
                foreach (RyoPixmap pixmap in pixmapArr) {
                    // JPG
                    if (isJPG) {
                        // 直接长度
                        writer.WriteInt(pixmap.Pixels.Length);
                        writer.WriteBytes(pixmap.Pixels);
                    } else {
                        //这块原来是不括起来的，警告一下怕有问题！！
                        writer.WriteBytes(pixmap.Pixels);
                    }
                }
            }
        }
    }

    public ICodec CreateCodecForDataRyoType(RyoType type) {
        //if (!type.IsCustom) throw new FormatException(type + "非自定义类型");

        foreach (var item in DataCodecRyoTypePairs) if (type.CsType == item.Key) return (ICodec)Activator.CreateInstance(item.Value.CsType!)!;

        throw new FormatException(type + "没有合适的特殊类型适配器");
    }

    public RyoType FindCodecRyoTypeForDataRyoType(RyoType ryoType) {
        foreach (var item in DataCodecRyoTypePairs) if (ryoType.CsType == item.Key) return item.Value;

        throw new FormatException(ryoType + "找不到合适的特殊类型适配器");
    }
}