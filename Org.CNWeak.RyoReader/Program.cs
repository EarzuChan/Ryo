using ICSharpCode.SharpZipLib.Zip.Compression;
using Me.Earzu.Ryo.Adaptions;
using Me.Earzu.Ryo.Adaptions.AdapterFactories;
using Me.Earzu.Ryo.Commands;
using Me.Earzu.Ryo.Formations;
using Me.Earzu.Ryo.IO;
using Me.Earzu.Ryo.Masses;
using Me.Earzu.Ryo.Utils;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Me.Earzu.Ryo
{
    public class ProgramMainBlob
    {
        static void Main(string[] args)
        {
            LogUtil.INSTANCE.SetLogger(str => Console.WriteLine(str));

#if DEBUG
            CommandManager.INSTANCE.IsDev = true;
#endif
            //LogUtil.INSTANCE.PrintInfo($"当前Dev状态：{CommandManager.INSTANCE.IsDev}");
            if (args.Length > 0)
            {
                CommandManager.INSTANCE.RunningWithArgs = true;
                CommandManager.INSTANCE.ParseCommand(args);
                return;
            }

            Console.WriteLine($"Ryo Console[Version {Assembly.GetExecutingAssembly().GetName().Version}]\nCopyright (C) Earzu Organization. All rights reserved.");
            while (true)
            {
                Console.Write("\nE:\\Ryo\\User>");
                string input = Console.ReadLine()!.Trim();
                if (input.ToLower() == "exit") break;

                CommandManager.INSTANCE.ParseCommandLine(input);
            }
        }
    }

    namespace Commands
    {
        public class CommandManager
        {
            public static CommandManager INSTANCE { get { instance ??= new(); return instance; } }
            private static CommandManager? instance;

            public readonly Dictionary<CommandAttribute, Type> commands = new();
            public bool RunningWithArgs = false;
            public bool IsDev
            {
                get => isDev;
                set
                {
                    if (isDev != value)
                    {
                        isDev = value;
                        // LogUtil.INSTANCE.PrintInfo($"注册为{value}");
                        RegCmds();
                    }
                }
            }
            private bool isDev = false;

            public void RegCmds()
            {
                commands.Clear();

                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
                foreach (var type in types)
                {
                    var attribute = type.GetCustomAttribute<CommandAttribute>();
                    if (attribute != null && typeof(ICommand).IsAssignableFrom(type))
                    {
                        if (attribute.IsDev && !isDev) continue;
                        // DEBUG逻辑
                        // UsefulUtils.INSTANCE.PrintInfo("标识符", (attribute == null ? "就是" : "不是"), "类型", (type == null ? "就是" : "不是"));
                        commands.Add(attribute, type);
                    }
                }
            }

            public CommandManager() => RegCmds();

            public void ParseCommandLine(string input)
            {
                // 解析命令行参数
                string pattern = @"([^""]\S*|"".+?"")\s*";
                MatchCollection matches = Regex.Matches(input, pattern);
                string[] cmdArgs = new string[matches.Count];
                for (int i = 0; i < matches.Count; i++)
                {
                    cmdArgs[i] = matches[i].Value.Trim();
                    if (cmdArgs[i].StartsWith("\"") && cmdArgs[i].EndsWith("\""))
                    {
                        cmdArgs[i] = cmdArgs[i][1..^1];
                    }
                }
                ParseCommand(cmdArgs);
            }

            public void ParseCommand(params string[] args)
            {
                // 解析命令并执行相关操作
                //Console.WriteLine("解析命令：");
                if (args == null || args.Length == 0)
                {
                    LogUtil.INSTANCE.PrintInfo("命令为空，输入Help查看支持的命令。");
                    return;
                }

                var givenCmdName = args[0].ToLower();
                var cmdArgs = args.Skip(1).ToArray();
                foreach (var cmd in commands)
                {
                    if (givenCmdName == cmd.Key.Name.ToLower())
                    {
                        var constructors = cmd.Value.GetConstructors();
                        foreach (var constructor in constructors)
                        {
                            var parameters = constructor.GetParameters();
                            if (cmdArgs.Length == parameters.Length)
                            {
                                var command = (ICommand)constructor.Invoke(cmdArgs);
                                command.Execute();
                                return;
                            }
                        }
                        LogUtil.INSTANCE.PrintInfo($"The parameter is incorrect. Usage of {cmd.Key.Name}: {cmd.Key.Help}");
                        return;
                    }
                }
                LogUtil.INSTANCE.PrintInfo($"'{givenCmdName}' is not recognized as an available command, enter 'Help' for more information.");
            }
        }

        [AttributeUsage(AttributeTargets.Class)]
        public class CommandAttribute : Attribute
        {
            public string Name { get; set; }
            public string Help { get; set; }
            public bool IsDev { get; set; }

            public CommandAttribute(string name, string help)
            {
                Name = name;
                Help = help;
            }
            public CommandAttribute(string name, string help, bool isDev)
            {
                Name = name;
                Help = help;
                IsDev = isDev;
            }
        }

        public interface ICommand
        {
            void Execute();
        }

        [Command("Load", "Load file from path")]
        public class LoadCommand : ICommand
        {
            public string PathString;
            public LoadCommand(string path)
            {
                PathString = path;
            }

            public void Execute()
            {
                try
                {
                    var stream = new FileStream(PathString, FileMode.Open);
                    if (stream.Length == 0) throw new Exception("文件长度为零");

                    var fileName = Path.GetFileNameWithoutExtension(stream.Name);
                    //LogListener("正在加载：" + fileName);

                    var mass = new MassFile(fileName);

                    mass.Load(stream);

                    if (mass.ReadyToUse)
                    {
                        LogUtil.INSTANCE.PrintInfo("已载入，索引信息如下：\n");
                        CommandManager.INSTANCE.ParseCommand("Info", fileName);
                        LogUtil.INSTANCE.PrintInfo($"\n档案已载入，名称：{fileName.ToUpper()}，稍后可凭借该名称Dump文件、查看索引信息、进行增删改查等操作。");
                    }
                    else
                    {
                        LogUtil.INSTANCE.PrintInfo("载入失败，文件无效，请检查您指定的文件是否存在、正确、未损坏。");
                        CommandManager.INSTANCE.ParseCommand("Unload", fileName);
                    }
                }
                catch (Exception e)
                {
                    LogUtil.INSTANCE.PrintError("载入文件失败", e);
                }
            }
        }

        [Command("Info", "Show the info of a file")]
        public class InfoCommand : ICommand
        {
            public string FileName;
            public InfoCommand(string fileName)
            {
                FileName = fileName;
            }
            public void Execute()
            {
                var mass = MassFileManager.INSTANCE.MassList.Find((MassFile m) => m.Name.ToLower() == FileName.ToLower());
                if (mass != null && mass.ReadyToUse)
                {
                    LogUtil.INSTANCE.PrintInfo(FileName.ToUpper() + "的索引信息：\n");
                    LogUtil.INSTANCE.PrintInfo($"碎片数据数：{mass.ObjCount}");
                    for (var i = 0; i < mass.ObjCount; i++) LogUtil.INSTANCE.PrintInfo($"-- Id.{i} 适配项ID：{mass.DataAdapterIdArray[i]} 已压缩：{(mass.IsItemDeflatedArray[i] ? "是" : "否")} 起始索引：{(i == 0 ? 0 : mass.EndPosition[i - 1])} 长度：{mass.EndPosition[i] - (i == 0 ? 0 : mass.EndPosition[i - 1])} 粘连数据：{mass.StickedDataList[i]}");
                    LogUtil.INSTANCE.PrintInfo($"\n粘连元数据数：{mass.StickedMetaDataItemCount}，以下为粘连元数据");
                    int currentIndex = 0;
                    while (currentIndex < mass.StickedMetaDataItemCount)
                    {
                        List<string> str = new();
                        for (int i = 0; i < 4 && currentIndex < mass.StickedMetaDataItemCount; i++)
                        {
                            str.Add("No." + (currentIndex + 1) + "：" + mass.StickedMetaDataList[currentIndex]);
                            currentIndex++;
                        }
                        LogUtil.INSTANCE.PrintInfo(str.ToArray());
                    }
                    LogUtil.INSTANCE.PrintInfo($"\n数据适配项数：{mass.MyRegableDataAdaptionList.Count}");
                    //foreach (var item in mass.MyRegableDataAdaptionList) LogUtil.INSTANCE.PrintInfo($"-- Id.{item.Id} 数据类型：{item.DataJavaClz} Ryo数据类型：{item.DataRyoType} 适配器：{item.AdapterJavaClz} Ryo适配器工厂类型：{item.AdapterFactoryRyoType}");
                    foreach (var item in mass.MyRegableDataAdaptionList) LogUtil.INSTANCE.PrintInfo($"-- Id.{item.Id} {item.DataRyoType} 适配器：{item.Adapter}");
                    LogUtil.INSTANCE.PrintInfo($"\n正式数据项数：{mass.MyIdStrMap.Count}");
                    foreach (var item in mass.MyIdStrMap) LogUtil.INSTANCE.PrintInfo($"-- Id.{item.Value} Name：{item.Key}");
                }
            }
        }

        [Command("Unload", "Unload a file by its name")]
        public class UnloadCommand : ICommand
        {
            public string FileName;
            public UnloadCommand(string fileName)
            {
                FileName = fileName;
            }
            public void Execute()
            {
                MassFileManager.INSTANCE.MassList.Remove(MassFileManager.INSTANCE.MassList.Find(a => a.Name == FileName)!);
            }
        }

        [Command("Dump", "Dump the objects blob of a file")]
        public class DumpCommand : ICommand
        {
            public string FileName;
            public DumpCommand(string str)
            {
                FileName = str;
            }
            public void Execute()
            {
                var mass = MassFileManager.INSTANCE.GetMassFileByFileName(FileName);
                if (mass != null && mass.ReadyToUse)
                {
                    LogUtil.INSTANCE.PrintInfo($"找到文件：{FileName.ToUpper()}");
                    LogUtil.INSTANCE.PrintInfo(mass.DumpBuffer());
                }
                else LogUtil.INSTANCE.PrintInfo($"找不到文件：{FileName.ToUpper()}，请查看拼写是否正确，文件是否已正确加载？");
            }
        }

        [Command("Help", "Get the help infomations of this application")]
        public class HelpCommand : ICommand
        {
            public void Execute()
            {
                LogUtil.INSTANCE.PrintInfo($"如今支持{CommandManager.INSTANCE.commands.Count}个命令，使用方法如下：");

                int i = 1;
                foreach (var cmd in CommandManager.INSTANCE.commands)
                {
                    LogUtil.INSTANCE.PrintInfo($"No.{i++} {cmd.Key.Name} 用法：{cmd.Key.Help}");
                }

                if (!CommandManager.INSTANCE.RunningWithArgs) LogUtil.INSTANCE.PrintInfo("\n如需退出，键入Exit。");
            }
        }

        [Command("Get", "Get the item infomation of your given id in a file")]
        public class GetCommand : ICommand
        {
            public string FileName;
            public int Id;
            public GetCommand(string fileName, string id)
            {
                FileName = fileName;
                Id = int.Parse(id);
            }
            public void Execute()
            {
                var mass = MassFileManager.INSTANCE.GetMassFileByFileName(FileName);
                try
                {
                    if (mass == null) throw new Exception("请求的文件不存在，请检查是否载入成功、文件名拼写是否正确？");
                    var typename = mass.MyRegableDataAdaptionList[mass.DataAdapterIdArray[Id]].DataRyoType;
                    var item = mass.GetItemById<object>(Id);
                    /*if (buffer == null || buffer.Length == 0) throw new Exception("请求的对象为空");
                    if (string.IsNullOrWhiteSpace(mass.FullPath)) throw new Exception("保存路径为空");*/

                    LogUtil.INSTANCE.PrintInfo($"数据类型：{typename}\n数据：{FormatManager.INSTANCE.ItemToString(item)}");

                    /*using (FileStream fs = new(mass.FullPath + $".{Id}.dump", FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(buffer, 0, buffer.Length);
                    }
                    LogUtil.INSTANCE.PrintInfo("保存成功，路径：" + mass.FullPath + $".{Id}.dump");*/
                }
                catch (Exception ex)
                {
                    LogUtil.INSTANCE.PrintError($"不能获取对象", ex);
                }
            }
        }

        [Command("Fuqi", "For dev only", true)]
        public class FuqiCommand : ICommand
        {
            public void Execute()
            {
                CommandManager.INSTANCE.ParseCommand("Load", "D:\\A Sources\\WeakPipeRecovery\\assets\\fuqi.fs");
            }
        }

        [Command("Seek", "Seek the images from a texture file.", true)]
        public class SeekCommand : ICommand
        {
            public string FileName;

            public SeekCommand(string fileName)
            {
                FileName = fileName;
            }

            public void Execute()
            {
                try
                {
                    var stream = new FileStream(FileName, FileMode.Open);
                    if (stream.Length == 0) throw new Exception("文件长度为零");

                    var fileName = Path.GetFileNameWithoutExtension(stream.Name);

                    var textureFile = new TextureFile(fileName);

                    textureFile.Load(stream);

                    if (textureFile.ReadyToUse)
                    {
                        LogUtil.INSTANCE.PrintInfo($"名称：{fileName.ToUpper()}\n\n纹样信息如下：\n");

                        LogUtil.INSTANCE.PrintInfo(FileName.ToUpper() + "的索引信息：\n");
                        LogUtil.INSTANCE.PrintInfo($"图片碎片数：{textureFile.ObjCount}");
                        for (var i = 0; i < textureFile.ObjCount; i++) LogUtil.INSTANCE.PrintInfo($"-- Id.{i} 适配项ID：{textureFile.DataAdapterIdArray[i]} 已压缩：{(textureFile.IsItemDeflatedArray[i] ? "是" : "否")} 起始索引：{(i == 0 ? 0 : textureFile.EndPosition[i - 1])} 长度：{textureFile.EndPosition[i] - (i == 0 ? 0 : textureFile.EndPosition[i - 1])}");

                        LogUtil.INSTANCE.PrintInfo($"\n图片模式适配项数：{textureFile.MyRegableDataAdaptionList.Count}");
                        foreach (var item in textureFile.MyRegableDataAdaptionList) LogUtil.INSTANCE.PrintInfo($"-- Id.{item.Id} 数据类型：{item.DataJavaClz} 适配器：{item.AdapterJavaClz}");

                        LogUtil.INSTANCE.PrintInfo($"\n真正图片数：{textureFile.ImageIDsArray.Length}");
                        for (var i = 0; i < textureFile.ImageIDsArray.Length; i++) LogUtil.INSTANCE.PrintInfo($"-- No.{i + 1} 图片的各种格式对应的ID：[{FormatManager.INSTANCE.ItemToString(textureFile.ImageIDsArray[i])}]");

                        //LogUtil.INSTANCE.PrintInfo("将写出DUMP数据");
                        if (textureFile.ImageIDsArray.Length == 0) return;

                        LogUtil.INSTANCE.PrintInfo("\n解析各项图片：");

                        int piece = 1;
                        for (int i = 0; i < textureFile.ImageIDsArray.Length; i++)
                        {
                            int[] items = textureFile.ImageIDsArray[i];

                            foreach (int id in items)
                            {
                                var imageBlob = textureFile.GetItemById<FragmentalImage>(id);

                                //LogUtil.INSTANCE.PrintInfo($"\n总片数：{imageBlob.ClipCount}，总层数：{imageBlob.SliceWidths.Length}，解析各项图片：");

                                if (imageBlob != null && imageBlob.ClipCount != 0)
                                {
                                    LogUtil.INSTANCE.PrintInfo($"-- No.{piece} 属于第{i + 1}张 碎片数：[{imageBlob.ClipCount}] 碎片宽：[{FormatManager.INSTANCE.ItemToString(imageBlob.SliceHeights)}] 碎片高：[{FormatManager.INSTANCE.ItemToString(imageBlob.SliceWidths)}] 碎片集层数：{imageBlob.Pixmaps.Length}");
                                    for (int level = 0; level < imageBlob.Pixmaps.Length; level++)
                                    {
                                        Pixmap[] pixs = imageBlob.Pixmaps[level];
                                        LogUtil.INSTANCE.PrintInfo($"---- 第{level + 1}层 有{pixs.Length}个");
                                        for (int no = 0; no < pixs.Length; no++)
                                        {
                                            Pixmap pix = pixs[no];
                                            LogUtil.INSTANCE.PrintInfo($"------ 第{no + 1}个 类型：{pix.Format} 像素数：{pix.GetPixelsCount()}");
                                        }
                                    }
                                    piece++;
                                }
                            }
                        }
                    }
                    else
                    {
                        LogUtil.INSTANCE.PrintInfo("查看失败，文件无效，请检查您指定的文件是否存在、正确、未损坏。");
                        //CommandManager.INSTANCE.ParseCommand("Unload", fileName);
                    }
                }
                catch (Exception e)
                {
                    LogUtil.INSTANCE.PrintError("载入文件失败", e);
                }
            }
        }

        [Command("TestConv", "For Dev only", true)]
        public class TestConvCommand : ICommand
        {
            public void Execute()
            {
                string[] types = { "B", "I", "[I", "java.lang.Integer", "cust0m", "[Lcust0m;", "java.lang.String", "[Ljava.lang.String;", "[[B", "[B" };
                int i = 1;
                foreach (var item in types)
                {
                    var ryo = AdaptionManager.INSTANCE.GetTypeByJavaClz(item);
                    var re = AdaptionManager.INSTANCE.GetJavaClzByType(ryo);
                    LogUtil.INSTANCE.PrintInfo($"No.{i}  原：{item}  结果：{re}  Java短名：{ryo.ShortName}  Java名：{ryo.Name}  自定义：{ryo.IsCustom}  是列表：{ryo.IsArray}  C#类：{ryo.BaseType}");
                    i++;
                }
            }
        }
    }

    namespace Adaptions
    {
        public class RyoType
        {
            public string? ShortName; // Java短名
            public string? Name; // Java名
            public bool IsCustom = false; // 自定义类
            public bool IsArray = false; // 是列表
            public bool ArrayWithL = false; // 列表用L
            public Type? BaseType; // C#基类

            public RyoType GetSubitemRyoType()
            {
                if (!IsArray) throw new NotSupportedException(this + "不是列表");
                var am = AdaptionManager.INSTANCE;
                return am.GetTypeByJavaClz(am.GetJavaClzByType(this)[1..]);
            }

            public override string ToString()
            {
                // return $"[RyoType信息：{AdaptionManager.INSTANCE.GetJavaClzByType(this)}，Java短名：{ShortName}，Java名：{Name}，自定义：{IsCustom}，是列表：{IsArray}，C#类：{BaseType}]";
                return $"[RyoType：Java：{Name}，自定：{IsCustom}，列表：{IsArray}，C#：{BaseType}]";
            }
        }

        public class AdaptionManager
        {
            public static AdaptionManager INSTANCE { get { instance ??= new(); return instance; } }
            private static AdaptionManager? instance;
            public HashSet<RyoType> RyoTypes = new();

            public AdaptionManager() => RegDefaultTypeJavaClzTable();

            public void RegDefaultTypeJavaClzTable()
            {
                // 注册基本类型
                RyoTypes.Add(new() { ShortName = "Z", Name = "java.lang.Boolean", BaseType = typeof(bool) });
                RyoTypes.Add(new() { ShortName = "B", Name = "java.lang.Byte", BaseType = typeof(byte) });
                RyoTypes.Add(new() { ShortName = "C", Name = "java.lang.Character", BaseType = typeof(char) });
                RyoTypes.Add(new() { ShortName = "D", Name = "java.lang.Double", BaseType = typeof(double) });
                RyoTypes.Add(new() { ShortName = "F", Name = "java.lang.Float", BaseType = typeof(float) });
                RyoTypes.Add(new() { ShortName = "I", Name = "java.lang.Integer", BaseType = typeof(int) });
                RyoTypes.Add(new() { ShortName = "J", Name = "java.lang.Long", BaseType = typeof(long) });
                RyoTypes.Add(new() { ShortName = "S", Name = "java.lang.Short", BaseType = typeof(short) });
                RyoTypes.Add(new() { ShortName = "V", Name = "java.lang.Void", BaseType = typeof(void) });
                RyoTypes.Add(new() { Name = "java.lang.String", BaseType = typeof(string) });

                // 注册工厂类
                RyoTypes.Add(new() { Name = "sengine.mass.serializers.DefaultSerializers", BaseType = typeof(BaseTypeAdapterFactory) });
                RyoTypes.Add(new() { Name = "sengine.mass.serializers.DefaultArraySerializers", BaseType = typeof(BaseArrayTypeAdapterFactory) });
                RyoTypes.Add(new() { Name = "sengine.mass.serializers.CollectionSerializer", BaseType = typeof(BaseArrayTypeAdapterFactory) });
                RyoTypes.Add(new() { Name = "sengine.mass.serializers.MapSerializer", BaseType = typeof(BaseArrayTypeAdapterFactory) });
                RyoTypes.Add(new() { Name = "sengine.mass.serializers.MassSerializableSerializer", BaseType = typeof(CustomFormatAdapterFactory) });
                RyoTypes.Add(new() { Name = "sengine.mass.serializers.FieldSerializer", BaseType = typeof(CustomFormatAdapterFactory) });

                // 注册额外项目（从文件）：TODO
            }

            public RyoType GetTypeByJavaClz(string clzName)
            {
                /*bool isFullArray = clzName.StartsWith("[L");
                if (isFullArray) clzName = clzName[2..];*/

                bool isArray = clzName.StartsWith('[');
                if (isArray) clzName = clzName[1..];

                bool arrayWithL = false;
                if (isArray & clzName.StartsWith("L") && clzName.EndsWith(";"))
                {
                    //isCustom = true;
                    arrayWithL = true;
                    clzName = clzName[1..^1];
                }

                RyoType? type = null;
                // 匹配基类
                foreach (var item in RyoTypes.Where(item => item.ShortName == clzName || clzName.StartsWith(item.Name!)))
                {
                    type = item;
                    break;
                }

                // 没有就创建新的
                type ??= new() { IsCustom = true, Name = clzName, ArrayWithL = arrayWithL };
                type.IsArray = isArray;

                return type;
            }

            public string GetJavaClzByType(RyoType type)
            {
                string clzName = type.IsArray ? "[" : "";

                if (type.IsCustom) clzName += type.ArrayWithL ? "L" + type.Name + ";" : type.Name; //自定义
                else // 非自定义
                {
                    foreach (var item in RyoTypes) // 遍历
                    {
                        if (type.BaseType == item.BaseType) // 相等
                        {
                            if (item.ShortName != null && type.IsArray) clzName += item.ShortName; // 是列表且有短名
                            else
                            {
                                if (type.IsArray) clzName += "L";
                                clzName += item.Name;
                                if (type.IsArray) clzName += ";";
                            }
                            break;
                        }
                    }
                }

                return clzName;
            }

            public Type? TryParseCustomFormatType(RyoType ryoType)
            {
                if (!ryoType.IsCustom) throw new FormatException("非自定义格式" + ryoType);

                //LogUtil.INSTANCE.PrintInfo("Parsing Format：" + ryoType);

                Type? formatType = null;
                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
                foreach (var item in types)
                {
                    var attribute = item.GetCustomAttribute<IAdaptable.AdaptableFormat>();
                    if (attribute != null && typeof(IAdaptable).IsAssignableFrom(item))
                    {
                        if (attribute.FormatName == ryoType.Name)
                        {
                            formatType = item;
                            break;
                        }
                    }
                }

                if (ryoType.IsArray) formatType = formatType?.MakeArrayType();
                return formatType;
            }
        }

        namespace AdapterFactories
        {
            public interface IAdapterFactory
            {
                IAdapter Create(RyoType type);
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
                    var formatType = AdaptionManager.INSTANCE.TryParseCustomFormatType(type);

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
                    public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadInt();

                    public void To(object obj, Mass mass, RyoWriter writer)
                    {
                        throw new NotImplementedException();
                    }
                }

                public class StringAdapter : IAdapter
                {
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
                    public object From(Mass mass, RyoReader reader, RyoType ryoType)
                    {
                        // TODO:优化类型判断过程，和RyoType的GetSubitemRyoType有关

                        var oriItemType = AdaptionManager.INSTANCE.TryParseCustomFormatType(ryoType)?.GetElementType();
                        Type itemType = oriItemType ?? typeof(object);

                        Array objArr = Array.CreateInstance(itemType, reader.ReadInt());
                        // LogUtil.INSTANCE.PrintInfo(ryoType + "列表类型：" + itemType + "、大小：" + objArr.Length);

                        mass.Reference(objArr);

                        for (int i = 0; i < objArr.Length; i++) objArr.SetValue(mass.Read<object>(), i);

                        if (oriItemType == null)
                        {
                            if (objArr.Length > 0 && objArr.GetValue(0) != null) itemType = objArr.GetValue(0)!.GetType();
                            LogUtil.INSTANCE.PrintInfo("类型为" + itemType);
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
                    public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadBytes(reader.ReadInt());

                    public void To(object obj, Mass mass, RyoWriter writer)
                    {
                        throw new NotImplementedException();
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
        }

        public interface IAdapter
        {
            object From(Mass mass, RyoReader reader, RyoType ryoType);
            void To(object obj, Mass mass, RyoWriter writer);
        }

        public class DirectByteArrayAdapter : IAdapter
        {
            public object From(Mass mass, RyoReader reader, RyoType ryoType) => reader.ReadAllBytes();

            public void To(object obj, Mass mass, RyoWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public interface IAdaptable
        {
            [AttributeUsage(AttributeTargets.Constructor)]
            public class AdaptableConstructor : Attribute { }

            [AttributeUsage(AttributeTargets.Class)]
            public class AdaptableFormat : Attribute
            {
                public string FormatName;
                public AdaptableFormat(string formatName) => FormatName = formatName;
            }

            object[] GetAdaptedArray();
        }
    }

    namespace Formations
    {
        public class FormatManager
        {
            public static FormatManager INSTANCE { get { instance ??= new(); return instance; } }
            private static FormatManager? instance;

            /*public Dictionary<OldAdapterAttribute, Type> oldAdapters = new();

            public FormatManager() => RegOldAdapterProxies();

            public void RegOldAdapterProxies()
            {
                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
                foreach (var type in types)
                {
                    var attribute = type.GetCustomAttribute<OldAdapterAttribute>();
                    if (attribute != null && typeof(IOldAdapter).IsAssignableFrom(type)) oldAdapters.Add(attribute, type);
                }
            }*/

            /*public IOldAdapter GetConverterByName(string name)
            {
                foreach (var item in oldAdapters) if (name == item.Key.Name && item.Key.AllMatches) return (IOldAdapter)Activator.CreateInstance(item.Value)!;
                LogUtil.INSTANCE.PrintInfo($"没有对于{name}的精确匹配");
                foreach (var item in oldAdapters) if (name.StartsWith(item.Key.Name) && !item.Key.AllMatches) return (IOldAdapter)Activator.CreateInstance(item.Value, name[1..])!;
                throw new NotSupportedException($"没有精确匹配也没有初略匹配，故格式{name}暂不支持");
            }*/

            /*public object ParseItem(Mass mass, RyoReader reader, string type)
            {
                try
                {
                    return GetConverterByName(type).CovertToObject(mass, reader);
                }
                catch (Exception e)
                {
                    LogUtil.INSTANCE.PrintError("怎么回事呢：", e);
                }

                return reader.ReadAllBytes();
            }*/

            public string ItemToString(object item)
            {
                Type type = item.GetType();
                if (item == null) return "项目为Null";
                else if (type == typeof(int[]))
                {
                    int[] iArr = (int[])item;
                    return $"[整数数组 长度：{iArr.Length} 内容：{{{string.Join(",", iArr.Select(i => i.ToString()))}}}]";
                }
                else if (type == typeof(byte[]))
                {
                    var it = (RyoReader)(byte[])item;
                    return $"[字节数组 长度：{it.Length} 内容：{{{it.ReadBytesToHexString((int)it.Length)}}}]";
                }
                else if (type.IsArray)
                {
                    var strList = new List<string>();
                    foreach (var elem in (Array)item)
                    {
                        strList.Add(ItemToString(elem));
                    }
                    return $"[{type.Name} 长度：{(item as Array)?.Length ?? 0} 内容：{{{string.Join(",", strList)}}}]";

                }
                else if (typeof(IAdaptable).IsAssignableFrom(type))
                {
                    return ItemToString(((IAdaptable)item).GetAdaptedArray());
                }
                else
                {
                    string? str = item.ToString();
                    return str == null ? "项目无内置转换，且强制转换结果为Null" : str!;
                }
            }
        }

        /*namespace OldAdapters
        {
            [AttributeUsage(AttributeTargets.Class)]
            public class OldAdapterAttribute : Attribute
            {
                public string Name { get; set; }
                public bool AllMatches { get; set; }

                public OldAdapterAttribute(string name)
                {
                    Name = name;
                    AllMatches = true;
                }

                public OldAdapterAttribute(string name, bool allMatches)
                {
                    Name = name;
                    AllMatches = allMatches;
                }
            }

            public interface IOldAdapter
            {
                object CovertToObject(Mass mass, RyoReader reader);
                byte[] CovertToBytes(object item);
            }

            [OldAdapter("java.lang.Integer")]
            public class IntConverter : IOldAdapter
            {
                public byte[] CovertToBytes(object item)
                {
                    throw new NotImplementedException();
                }

                public object CovertToObject(Mass mass, RyoReader reader) => reader.ReadInt();
            }

            [OldAdapter("java.lang.String")]
            public class StringConverter : IOldAdapter
            {
                public byte[] CovertToBytes(object item)
                {
                    throw new NotImplementedException();
                }

                public object CovertToObject(Mass mass, RyoReader reader) => reader.ReadString();
            }

            [OldAdapter("[I")]
            public class IntArrayConverter : IOldAdapter
            {
                public byte[] CovertToBytes(object item)
                {
                    throw new NotImplementedException();
                }

                public object CovertToObject(Mass mass, RyoReader reader)
                {
                    int i = reader.ReadInt();
                    int[] iArr = new int[i];
                    for (int i2 = 0; i2 < i; i2++)
                    {
                        iArr[i2] = reader.ReadInt();
                    }
                    return iArr;
                }
            }

            [OldAdapter("[B")]
            public class ByteArrayConverter : IOldAdapter
            {
                public byte[] CovertToBytes(object item)
                {
                    throw new NotImplementedException();
                }

                public object CovertToObject(Mass mass, RyoReader reader)
                {
                    return reader.ReadBytes(reader.ReadInt());
                }
            }

            [OldAdapter("sengine.graphics2d.FontSprites")]
            public class FuqiFormatConverter : IOldAdapter
            {
                public byte[] CovertToBytes(object item)
                {
                    throw new NotImplementedException();
                }

                public object CovertToObject(Mass mass, RyoReader reader)
                {
                    // 不要硬写
                    var iA = mass.Read<int[]>();
                    var baa = mass.Read<byte[][]>();
                    float f = reader.ReadFloat();
                    int i = reader.ReadInt();
                    return $"内容如下：\n\n整数数组：\n{FormatManager.INSTANCE.ItemToString(iA)}\n\n比特数组数组：\n{{\n{string.Join(", ", baa.Select(sub => FormatManager.INSTANCE.ItemToString(sub)))}\n}}\n\n浮点值：{f}\n\n整数值：{i}";
                }
            }

            [OldAdapter("sengine.graphics2d.texturefile.FIFormat$FragmentedImageData")]
            public class FragmentalImageConverter : IOldAdapter
            {
                // 计算分块数，按长度分割总数一共要多少块（不足的也给完整一块）
                public static int CalculateBlockCount(int fullLength, int blockLength)
                {
                    int quotient = Math.DivRem(blockLength, fullLength, out int remainder);
                    return (remainder != 0 ? 1 : 0) + quotient;
                }


                public byte[] CovertToBytes(object item)
                {
                    throw new NotImplementedException();
                }

                public object CovertToObject(Mass mass, RyoReader reader)
                {
                    // 过程有问题
                    // LogUtil.INSTANCE.PrintInfo("开始解析图片...");

                    int directLength;// 保留

                    int clipCount = reader.ReadInt();
                    int sliceCount = reader.ReadInt();

                    // LogUtil.INSTANCE.PrintInfo($"总数：{clipCount} 层级：{sliceCount}");

                    int[] sliceWidths = new int[sliceCount];
                    int[] sliceHeights = new int[sliceCount];
                    Pixmap[][] pixmaps = new Pixmap[sliceCount][];

                    for (int i2 = 0; i2 < sliceCount; i2++)
                    {
                        try
                        {
                            // 每层的宽高
                            sliceWidths[i2] = reader.ReadInt();
                            sliceHeights[i2] = reader.ReadInt();

                            //每层的格式
                            Pixmap.FORMAT format = (Pixmap.FORMAT)reader.ReadPositiveByte();
                            bool isSupport = format == Pixmap.FORMAT.RGB888 || format == Pixmap.FORMAT.RGB565;

                            int sliceBlockCount = CalculateBlockCount(clipCount, sliceWidths[i2]);
                            int sliceClipCount = sliceBlockCount * CalculateBlockCount(clipCount, sliceHeights[i2]);

                            // 每层块数
                            pixmaps[i2] = new Pixmap[sliceClipCount];

                            for (int i3 = 0; i3 < sliceClipCount; i3++)
                            {
                                int x = (i3 % sliceBlockCount) * clipCount;
                                int y = (i3 / sliceBlockCount) * clipCount;
                                int right = x + clipCount;
                                int bottom = y + clipCount;

                                if (right > sliceWidths[i2])
                                {
                                    right = sliceWidths[i2];
                                }
                                if (bottom > sliceHeights[i2])
                                {
                                    bottom = sliceHeights[i2];
                                }

                                if (!isSupport || (directLength = reader.ReadInt()) <= 0)
                                {
                                    Pixmap pixmap = new(right - x, bottom - y, format);
                                    pixmap.Pixels = reader.ReadBytes(pixmap.Pixels.Length);
                                    pixmaps[i2][i3] = pixmap;
                                }
                                else
                                {
                                    pixmaps[i2][i3] = new(reader.ReadBytes(directLength));// 宽高怎么办
                                }
                            }
                        }
                        catch (Exception th)
                        {
                            //LogUtil.INSTANCE.PrintError("读取失败 将回收内存", th);

                            foreach (Pixmap[] v1 in pixmaps)
                            {
                                if (v1 == null) continue;

                                foreach (Pixmap v in v1) v?.Dispose();
                            }
                            throw new InvalidDataException("读取块时出现异常", th);
                        }
                    }
                    return new FragmentalImage(clipCount, sliceWidths, sliceHeights, pixmaps);
                }
            }

            [OldAdapter("[", false)]
            public class ObjectArrayConverter : IOldAdapter
            {
                string SubObjType = "object";

                public ObjectArrayConverter(string subObjType)
                {
                    SubObjType = subObjType;

                    // LogUtil.INSTANCE.PrintInfo($"创建对于{SubObjType}的列表适配器");
                }
                public byte[] CovertToBytes(object item)
                {
                    throw new NotImplementedException();
                }

                public object CovertToObject(Mass mass, RyoReader reader)
                {
                    object[] objArr = new object[reader.ReadInt()];
                    // LogUtil.INSTANCE.PrintInfo("对象列表大小：" + objArr.Length);
                    mass.Reference(objArr);
                    for (int i = 0; i < objArr.Length; i++) objArr[i] = mass.Read<object>();

                    Type type = typeof(object);
                    if (objArr.Length > 0 && objArr[0] != null) type = objArr[0].GetType();
                    // LogUtil.INSTANCE.PrintInfo("类型为" + type);
                    if (type != typeof(object))
                    {
                        Array newArray = Array.CreateInstance(type, objArr.Length);

                        for (int i = 0; i < objArr.Length; i++)
                        {
                            object obj = objArr.GetValue(i)!; // 获取objArr中的当前项
                            object convertedObj = Convert.ChangeType(obj, type); // 将当前项转换为type类型
                            newArray.SetValue(convertedObj, i); // 将转换后的值赋值给新数组中的对应位置
                        }

                        return newArray;
                    }

                    return objArr;
                }
            }
        }*/

        public class Pixmap : IDisposable
        {
            public enum FORMAT
            {
                Alpha,
                Intensity,
                LuminanceAlpha,
                RGB565,
                RGBA4444,
                RGB888,
                RGBA8888
            }

            private int Width;
            private int Height;
            public byte[] Pixels { get; set; }
            public FORMAT Format = FORMAT.RGB888;

            public Pixmap(int width, int height, FORMAT format)
            {
                Width = width;
                Height = height;
                Format = format;
                Pixels = new byte[width * height * GetPixelSize(format)];
            }

            public Pixmap(byte[] buffer) => Pixels = buffer;

            public int GetPixel(int x, int y)
            {
                int pixelSize = GetPixelSize(Format);
                int index = (x + y * Width) * pixelSize;
                int pixel = 0;

                switch (Format)
                {
                    case FORMAT.RGBA8888:
                        pixel |= (Pixels[index++] & 0xff) << 24;
                        pixel |= (Pixels[index++] & 0xff) << 16;
                        pixel |= (Pixels[index++] & 0xff) << 8;
                        pixel |= (Pixels[index] & 0xff);
                        break;

                    case FORMAT.RGB888:
                        pixel |= (Pixels[index++] & 0xff) << 16;
                        pixel |= (Pixels[index++] & 0xff) << 8;
                        pixel |= (Pixels[index] & 0xff);
                        break;

                    case FORMAT.RGB565:
                        int r = (Pixels[index++] & 0xff);
                        int g = (Pixels[index] & 0xff);
                        pixel |= ((r & 0xf8) << 8);
                        pixel |= ((g & 0xfc) << 3);
                        break;

                    case FORMAT.Alpha:
                        pixel = Pixels[index] & 0xff;
                        break;
                }

                return pixel;
            }

            public void SetPixel(int x, int y, int pixel)
            {
                int pixelSize = GetPixelSize(Format);
                int index = (x + y * Width) * pixelSize;

                switch (Format)
                {
                    case FORMAT.RGBA8888:
                        Pixels[index++] = (byte)((pixel >> 24) & 0xff);
                        Pixels[index++] = (byte)((pixel >> 16) & 0xff);
                        Pixels[index++] = (byte)((pixel >> 8) & 0xff);
                        Pixels[index] = (byte)(pixel & 0xff);
                        break;

                    case FORMAT.RGB888:
                        Pixels[index++] = (byte)((pixel >> 16) & 0xff);
                        Pixels[index++] = (byte)((pixel >> 8) & 0xff);
                        Pixels[index] = (byte)(pixel & 0xff);
                        break;

                    case FORMAT.RGB565:
                        Pixels[index++] = (byte)(((pixel >> 8) & 0xf8) | ((pixel >> 13) & 0x7));
                        Pixels[index] = (byte)(((pixel >> 3) & 0xfc) | ((pixel >> 9) & 0x3));
                        break;

                    case FORMAT.Alpha:
                        Pixels[index] = (byte)(pixel & 0xff);
                        break;
                }
            }

            public int GetPixelsCount() => Pixels.Length / GetPixelSize(Format);

            public static int GetPixelSize(FORMAT format)
            {
                switch (format)
                {
                    case FORMAT.RGBA8888:
                        return 4;

                    case FORMAT.RGB888:
                        return 3;

                    case FORMAT.RGB565:
                    case FORMAT.Alpha:
                        return 2;
                }

                return 0;
            }

            public void Dispose()
            {
                Pixels = Array.Empty<byte>();
            }
        }

        public class FragmentalImage
        {
            public int ClipCount;
            public int[] SliceWidths;
            public int[] SliceHeights;
            public Pixmap[][] Pixmaps;

            public FragmentalImage(int clipCount, int[] sliceWidths, int[] sliceHeights, Pixmap[][] pixmaps)
            {
                ClipCount = clipCount;
                SliceWidths = sliceWidths;
                SliceHeights = sliceHeights;
                Pixmaps = pixmaps;
            }

            public static explicit operator object[](FragmentalImage image) => new object[] { image.ClipCount, image.SliceWidths, image.SliceHeights, image.Pixmaps };
        }

        [IAdaptable.AdaptableFormat("sengine.graphics2d.FontSprites")]
        public class FontSprites : IAdaptable
        {
            public int[] IArr;
            public byte[][] BArr;
            public float F;
            public int I;

            [IAdaptable.AdaptableConstructor]
            public FontSprites(int[] iArr, byte[][] bArr, float f, int i)
            {
                IArr = iArr;
                BArr = bArr;
                F = f;
                I = i;
            }

            object[] IAdaptable.GetAdaptedArray() => new object[] { IArr, BArr, F, I };
        }

        [IAdaptable.AdaptableFormat("game31.DialogueTree$DialogueTreeDescriptor")]
        public class Conversations : IAdaptable
        {
            private string DialogueNameSpace;
            private Conversation[] 数组_对话;

            [IAdaptable.AdaptableConstructor]
            public Conversations(string str, Conversation[] r2)
            {
                DialogueNameSpace = str;
                数组_对话 = r2;
            }

            public object[] GetAdaptedArray() => new object[] { DialogueNameSpace, 数组_对话 };
        }

        [IAdaptable.AdaptableFormat("game31.DialogueTree$Conversation")]
        public class Conversation : IAdaptable
        {
            private string[] Tags;
            private string Status;
            private UserMessage[] UserMessages;
            private bool StateOfDiswatch;
            private SenderMessage[] SenderMessagers;
            private string[] TagsToUnlock;
            private string[] TagsToLock;
            private string Trigger;

            [IAdaptable.AdaptableConstructor]
            public Conversation(string[] tags, string status, UserMessage[] userMessages, bool stateOfDiswatch, SenderMessage[] senderMessagers, string[] tagsToUnlock, string[] tagsToLock, string trigger)
            {
                Tags = tags;
                Status = status;
                UserMessages = userMessages;
                StateOfDiswatch = stateOfDiswatch;
                SenderMessagers = senderMessagers;
                TagsToUnlock = tagsToUnlock;
                TagsToLock = tagsToLock;
                Trigger = trigger;
            }

            public object[] GetAdaptedArray() => new object[] { Tags, Status, UserMessages, StateOfDiswatch, SenderMessagers, TagsToUnlock, TagsToLock, Trigger };

        }

        [IAdaptable.AdaptableFormat("game31.DialogueTree$UserMessage")]
        public class UserMessage : IAdaptable
        {
            public bool IsHidden;
            public string Message;

            [IAdaptable.AdaptableConstructor]
            public UserMessage(string str, bool z)
            {
                Message = str;
                IsHidden = z;
            }

            public object[] GetAdaptedArray() => new object[] { Message, IsHidden };
        }

        [IAdaptable.AdaptableFormat("game31.DialogueTree$SenderMessage")]
        public class SenderMessage : IAdaptable
        {
            public string DateText;
            public string Message;
            public string Origin;
            public float IdleTime;
            public float TriggerTime;
            public float TypingTime;
            public string TimeText;
            public string Trigger;

            [IAdaptable.AdaptableConstructor]
            public SenderMessage(string message, string origin, string dataText, string timeText, float idleTime, float typingTime, string trigger, float triggerTime)
            {
                Message = message;
                Origin = origin;
                DateText = dataText;
                TimeText = timeText;
                IdleTime = idleTime;
                TypingTime = typingTime;
                Trigger = trigger;
                TriggerTime = triggerTime;
            }

            public object[] GetAdaptedArray() => new object[] { Message, Origin, DateText, TimeText, IdleTime, TypingTime, Trigger, TriggerTime };
        }
    }
    namespace Utils
    {
        public class LogUtil
        {
            public static LogUtil INSTANCE { get { instance ??= new(); return instance; } }
            private static LogUtil? instance;
            private Action<string> Logger = str => Trace.WriteLine(str);
            public bool AllowPrintDebugInfo = false;

            public void SetLogger(Action<string> logger) => Logger = logger;

            public void PrintError(String info, Exception e, bool printStack = true)
            {
                Logger("错误：" + info + "，因为：" + e.Message);
                if (e.StackTrace != null && printStack) Logger(e.StackTrace);
            }

            public void PrintInfo(params string[] args)
            {
                Logger(string.Join(' ', args));
            }

            public void PrintDebugInfo(params string[] args)
            {
                if (AllowPrintDebugInfo) PrintInfo("额外调试信息：" + args);
            }
        }

        public class CompressionUtil
        {
            public static CompressionUtil INSTANCE { get { instance ??= new(); return instance; } }
            private static CompressionUtil? instance;

            public byte[] Inflate(byte[] input, int offset, int length)
            {
                try
                {
                    var outStream = new MemoryStream();
                    var inflater = new Inflater();
                    inflater.SetInput(input, offset, length);
                    var buffer = new byte[1024];
                    while (!inflater.IsFinished)
                    {
                        int count = inflater.Inflate(buffer);
                        outStream.Write(buffer, 0, count);
                    }

                    return outStream.ToArray();
                }
                catch (Exception e)
                {
                    LogUtil.INSTANCE.PrintError("解压出错", e);
                    return Array.Empty<byte>();
                }
            }
        }
    }

    namespace IO
    {
        public class RyoReader : IDisposable
        {
            private BinaryReader Reader;
            public long RestLength
            {
                get => Length - Position;
            }
            public long Length
            {
                get => Reader.BaseStream.Length; //不应该，应该是读缓冲区，虽然我没用
            }
            public long Position
            {
                get => Reader.BaseStream.Position; //不应该，应该是读缓冲区，虽然我没用
                set => Reader.BaseStream.Position = value;
            }

            public RyoReader(Stream inputStream)
            {
                Reader = new BinaryReader(inputStream);
            }

            public byte[] ReadBytes(int length)
            {
                return Reader.ReadBytes(length);
            }

            public int ReadInt() => BitConverter.ToInt32(Reader.ReadBytes(4).Reverse().ToArray(), 0);

            public string ReadBytesToHexString(int length)
            {
                byte[] bytes = Reader.ReadBytes(length);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(string.Format("{0:X2}", bytes[i]));
                    if (i < bytes.Length - 1)
                    {
                        sb.Append(',');
                    }
                }

                return sb.ToString();
            }

            public string ReadString()
            {
                int length = ReadInt();
                return ReadString(length);
            }

            public string ReadString(int length)
            {
                byte[] bytes = Reader.ReadBytes(length);
                return Encoding.UTF8.GetString(bytes);
            }

            public bool CheckHasString(string str) => ReadString(Encoding.UTF8.GetBytes(str).Length) == str;

            public void Dispose() => Reader.Dispose();

            public byte[] ReadAllBytes() => Reader.ReadBytes((int)RestLength);

            public float ReadFloat() => BitConverter.ToSingle(ReadBytes(4).Reverse().ToArray(), 0);

            public byte ReadUnsignedByte() => Reader.ReadByte();

            public sbyte ReadSignedByte() => Reader.ReadSByte();

            public bool ReadBoolean() => ReadSignedByte() != 0;

            public static implicit operator RyoReader(byte[] buffer) => new RyoReader(new MemoryStream(buffer));
        }

        public class RyoWriter { }

        public class RyoBuffer
        {
            public byte[] Buffer;
            public int Length { get => Buffer.Length; }
            public int Limit { set; get; }
            public int Position { set; get; }

            public RyoBuffer() => Buffer = Array.Empty<byte>();

            public RyoBuffer(byte[] buffer) => Buffer = buffer;

            public string Dump(string path)
            {
                //if (!ReadyToUse || IsEmpty) return "未准备好或者内容为空";
                //if (string.IsNullOrWhiteSpace(FullPath)) return "保存路径为空";
                var info = "保存";
                try
                {
                    using (FileStream fs = new(path, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(Buffer, 0, Buffer.Length);
                    }
                    info += path;
                }
                catch (Exception ex)
                {
                    LogUtil.INSTANCE.PrintError("保存时错误", ex);
                    info += "失败（悲";
                }
                return info;
            }
        }
    }

    namespace Masses
    {
        public class MassFileManager
        {
            public static MassFileManager INSTANCE { get { instance ??= new(); return instance; } }
            private static MassFileManager? instance;

            public List<MassFile> MassList = new();

            public MassFile? GetMassFileByFileName(string fileName) => MassList.Find((MassFile m) => m.Name.ToLower() == fileName.ToLower());
        }

        public class TextureFile : Mass
        {
            // 常量
            public new const string EXTENDED_NAME = "TextureFile";

            // 成员
            public int[][] ImageIDsArray = Array.Empty<int[]>();

            public TextureFile(string name) : base(name) { }

            public void Load(FileStream file)
            {
                Load(file, EXTENDED_NAME);
            }

            public override void AfterLoadingIndex(RyoReader inflatedDataReader)
            {
                // 从读者类输入流中读取对象个数信息，并构建表
                ImageIDsArray = new int[inflatedDataReader.ReadInt()][];

                // 构建表
                for (int i = 0; i < ImageIDsArray.Length; i++)
                {
                    ImageIDsArray[i] = new int[inflatedDataReader.ReadInt()];

                    // 读取每个对象的个数信息
                    for (int j = 0; j < ImageIDsArray[i].Length; j++)
                    {
                        ImageIDsArray[i][j] = inflatedDataReader.ReadInt();
                    }
                }
            }
        }

        public class MassFile : Mass
        {
            // 常量
            public new const string EXTENDED_NAME = "FileSystem";

            // 成员变量
            public Dictionary<string, int> MyIdStrMap = new();

            public MassFile(string name) : base(name)
            {
                MassFileManager.INSTANCE.MassList.Add(this);
            }

            public override void AfterLoadingIndex(RyoReader inflatedDataReader)
            {
                // LogUtil.INSTANCE.PrintInfo("芝士");

                var idStrMapCount = inflatedDataReader.ReadInt();
                for (var i = 0; i < idStrMapCount; i++)
                {
                    var str = inflatedDataReader.ReadString();
                    var id = inflatedDataReader.ReadInt();
                    MyIdStrMap.Add(str, id);
                }
            }

            public void Load(FileStream file)
            {
                Load(file, EXTENDED_NAME);
            }
        }

        public class Mass
        {
            // 常量
            public const string EXTENDED_NAME = "MASS";

            // 文件名和路径
            public string Name = "";
            public string FullPath = "";

            // 暂存读写器
            public RyoReader Reader = new(Stream.Null);
            public RyoWriter Writer = new();

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
            public int StickedMetaDataItemCount = 0;
            public List<RegableDataAdaption> MyRegableDataAdaptionList = new();

            // 暂存变量
            public bool ReadyToUse = true;
            public bool IsEmpty = true;
            public int SavedStickedDataIntArrayIdMinusOne = -1;
            public int SavedStickedDataIntArrayId = -1;
            public int SavedId = -1;

            // 可注册项
            public class RegableDataAdaption
            {
                public string DataJavaClz { get; set; }
                public RyoType DataRyoType { get; set; }
                public string AdapterJavaClz { get; set; }
                public RyoType AdapterFactoryRyoType { get; set; }
                public IAdapter Adapter { get; set; }
                public int Id { get; set; }

                public RegableDataAdaption(int id, string dataType, string adapterType)
                {
                    Id = id;

                    DataJavaClz = dataType;
                    DataRyoType = AdaptionManager.INSTANCE.GetTypeByJavaClz(dataType);

                    AdapterJavaClz = adapterType;
                    AdapterFactoryRyoType = AdaptionManager.INSTANCE.GetTypeByJavaClz(adapterType);

                    // 通过工厂创建
                    try
                    {
                        if (typeof(IAdapterFactory).IsAssignableFrom(AdapterFactoryRyoType.BaseType))
                        {
                            Adapter = ((IAdapterFactory)Activator.CreateInstance(AdapterFactoryRyoType.BaseType)!).Create(DataRyoType);
                        }
                        else throw new FormatException($"{AdapterFactoryRyoType}不是一个适配器工厂类");
                    }
                    catch (Exception ex)
                    {
                        LogUtil.INSTANCE.PrintError($"注册适配项（ID.{id}）时出现问题", ex, false);
                        Adapter = new DirectByteArrayAdapter();
                    }
                }
            }

            // 根据ID获取项目
            public T GetItemById<T>(int id)
            {
                if (!ReadyToUse || IsEmpty) throw new Exception("文件未准备好或为空");

                bool isItemDeflated = IsItemDeflatedArray[id];
                var dataAdaption = MyRegableDataAdaptionList[DataAdapterIdArray[id]];

                //Class <?> theObjTypeShouldBe = 方法_取已序列化的类(idOfXuLieHuaQi);
                //接口_序列化器 <?> xuliehuaqi = 方法_取序列化器(idOfXuLieHuaQi);

                /*byte[] oldWorkBuffer = Reader.Buffer;
                int inputReaderPosition = (int)Reader.Position;
                int inputReaderLimit = (int)Reader.Length;*/
                RyoBuffer workBuffer = WorkBuffer;
                int oldStickedDataIntArrayIdMinusOne = SavedStickedDataIntArrayIdMinusOne;
                int oldStickedDataIntArrayId = SavedStickedDataIntArrayId;
                int oldIdSaves = SavedId;

                int blobStartPosition = id == 0 ? 0 : EndPosition[id - 1];
                int blobLength = EndPosition[id] - blobStartPosition;

                SavedId = id;
                SavedStickedDataIntArrayIdMinusOne = id == 0 ? 0 : StickedDataList[id - 1];
                SavedStickedDataIntArrayId = StickedDataList[id];

                WorkBuffer.Buffer = isItemDeflated ? CompressionUtil.INSTANCE.Inflate(RealItemDataBuffer.Skip(blobStartPosition).Take(blobLength).ToArray(), 0, blobLength) : RealItemDataBuffer.Skip(blobStartPosition).Take(blobLength).ToArray();

                LogUtil.INSTANCE.PrintDebugInfo("源长度", RealItemDataBuffer.Length.ToString(), "ID", id.ToString(), "起始", blobStartPosition.ToString(), "长度", workBuffer.Length.ToString(), "3-1", SavedStickedDataIntArrayIdMinusOne.ToString(), "3", SavedStickedDataIntArrayId.ToString());

                // T item = (T)FormatManager.INSTANCE.ParseItem(this, (RyoReader)WorkBuffer.Buffer, MyRegableDataAdaptionList[DataAdapterIdArray[id]].DataJavaClz);
                T item = (T)dataAdaption.Adapter.From(this, (RyoReader)WorkBuffer.Buffer, dataAdaption.DataRyoType);

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
                    Writer = new();
                    ObjCount = 0;
                    CurrentObjCount = 0;
                    DataAdapterIdArray = new();
                    EndPosition = new();
                    StickedDataList = new();
                    StickedMetaDataList = new();
                    StickedMetaDataItemCount = 0;
                    RealItemDataBuffer = Array.Empty<byte>();
                    WorkBuffer = new RyoBuffer();

                    SavedStickedDataIntArrayId = -1;
                    SavedStickedDataIntArrayIdMinusOne = -1;
                    SavedId = -1;
                    IsEmpty = true;
                    ReadyToUse = true;
                }
                catch (Exception e)
                {
                    LogUtil.INSTANCE.PrintError("加载Mass失败", e);
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

                        StickedMetaDataItemCount = StickedDataList.Last();

                        for (var i = 0; i < StickedMetaDataItemCount; i++)
                            StickedMetaDataList.Add(inflatedDataReader.ReadInt());

                        var regCount = inflatedDataReader.ReadInt();
                        for (var i = 0; i < regCount; i++)
                        {
                            var id = inflatedDataReader.ReadInt();
                            var str1 = inflatedDataReader.ReadString();
                            var str2 = inflatedDataReader.ReadString();
                            MyRegableDataAdaptionList.Add(new RegableDataAdaption(id, str1, str2));
                        }

                        AfterLoadingIndex(inflatedDataReader);
                    }
                    RealItemDataBuffer = Reader.ReadAllBytes();

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

            public virtual void AfterLoadingIndex(RyoReader inflatedDataReader)
            {
                //LogUtil.INSTANCE.PrintInfo("怎么回事");
            }

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
                if (SavedStickedDataIntArrayIdMinusOne == SavedStickedDataIntArrayId) throw new Exception("噬主了");

                int metaOfIdMinusOne = StickedMetaDataList[SavedStickedDataIntArrayIdMinusOne];
                SavedStickedDataIntArrayIdMinusOne++;
                LogUtil.INSTANCE.PrintDebugInfo("新粘连ID：" + SavedStickedDataIntArrayIdMinusOne);
                int subitemId = metaOfIdMinusOne >> 2;

                LogUtil.INSTANCE.PrintDebugInfo($"读子项的ID：{subitemId}");
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
}