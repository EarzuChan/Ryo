using Me.EarzuChan.Ryo.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Masses
{
    /*public class OldTextureFile : OldMass
    {
        // 成员
        public int[][] ImageIDsArray = Array.Empty<int[]>();

        public OldTextureFile(string name) : base(name)
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

    public class OldMassFile : OldMass
    {
        // 成员变量
        public Dictionary<string, int> MyIdStrMap = new();

        public OldMassFile(string name) : base(name)
        {
            ExtendedName = "FileSystem";
            OldMassManager.INSTANCE.MassList.Add(this);
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
    }*/

    public class MassFile : Mass
    {
        public readonly Dictionary<string, int> IdStrPairs = new();

        public MassFile() => ExtendedName = "FileSystem";

        public enum ADDTYPE
        {
            Add, Replace
        }

        protected override void AfterLoadingIndex(RyoReader inflatedDataReader)
        {
            var idStrPairCount = inflatedDataReader.ReadInt();
            for (var i = 0; i < idStrPairCount; i++)
            {
                var str = inflatedDataReader.ReadString();
                var id = inflatedDataReader.ReadInt();
                IdStrPairs.Add(str, id);
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

        public ADDTYPE Add(string token, object obj)
        {
            int objId = Add(obj);
            if (IdStrPairs.ContainsKey(token))
            {
                IdStrPairs[token] = objId;
                return ADDTYPE.Replace;
            }
            IdStrPairs.Add(token, objId);
            return ADDTYPE.Add;
        }

        // 看明白了，原版是抛弃原有，添加同名即替换而不删除原来的块，指向新块，所以原来的Put可以不Put
    }

    public class TextureFile : Mass
    {
        // 成员
        public readonly List<List<int>> ImageIDsArray = new();

        public TextureFile() : base() => ExtendedName = "TextureFile";

        protected override void AfterLoadingIndex(RyoReader reader)
        {
            // 从读者类输入流中读取对象个数信息，并构建表
            int imageCount = reader.ReadInt();
            //ImageIDsArray = new int[inflatedDataReader.ReadInt()][];

            // 构建表
            for (int i = 0; i < imageCount; i++)
            {
                // ImageIDsArray[i] = new int[reader.ReadInt()];
                var subList = new List<int>();
                int clipCount = reader.ReadInt();

                // 读取每个对象的个数信息
                for (int j = 0; j < clipCount; j++) subList.Add(reader.ReadInt());

                ImageIDsArray.Add(subList);
            }
        }

        protected override void AfterSavingIndex(RyoWriter writer)
        {
            writer.WriteInt(ImageIDsArray.Count);
            foreach (List<int> sublist in ImageIDsArray)
            {
                writer.WriteInt(sublist.Count);
                foreach (int item in sublist) writer.WriteInt(item);
            }

        }
    }
}
