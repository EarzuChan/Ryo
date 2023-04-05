using Me.EarzuChan.Ryo.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Masses
{
    public class TextureFile : Mass
    {
        // 成员
        public int[][] ImageIDsArray = Array.Empty<int[]>();

        public TextureFile(string name) : base(name)
        {
            ExtendedName = "TextureFile";
        }

        public void Load(FileStream file)
        {
            Load(file, ExtendedName);
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
        // 成员变量
        public Dictionary<string, int> MyIdStrMap = new();

        public MassFile(string name) : base(name)
        {
            ExtendedName = "FileSystem";
            MassManager.INSTANCE.MassList.Add(this);
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

        public override void AfterSavingIndex(RyoWriter writer)
        {
            writer.WriteInt(MyIdStrMap.Count);
            //var keys = MyIdStrMap.Keys;
            foreach (var i in MyIdStrMap)
            {
                writer.WrintString(i.Key);
                writer.WriteInt(i.Value);
            }
        }

        public void Load(FileStream file)
        {
            Load(file, ExtendedName);
        }
    }
}
