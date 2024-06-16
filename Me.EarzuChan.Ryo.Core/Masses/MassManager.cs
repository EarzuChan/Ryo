using Me.EarzuChan.Ryo.Exceptions.FileExceptions;
using Me.EarzuChan.Ryo.Core.Utils;
using System.Text;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Core.Masses
{
    public class MassManager
    {
        public readonly Dictionary<string, MassFile> MassFiles = new();

        public MassFile GetMassFileOrThrow(string fileName)
        {
            fileName = fileName.Trim().ToLower();

            var massFile = MassFiles.FirstOrDefault(item => item.Key.ToLower() == fileName).Value;

            return massFile ?? throw new NoSuchFileException(fileName);
        }

        public bool ExistsMass(string fileName) =>MassFiles.ContainsKey(fileName);

        // TODO:改写
        public MassFile LoadMassFile(string filePath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("文件名不该为空");
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException("文件路径不该为空");
            fileName = fileName.Trim().ToLower();
            filePath = filePath.Trim();

            using var fileStream = FileUtils.OpenFile(filePath);

            var massFile = new MassFile();
            try
            {
                massFile.Load(fileStream);
            }
            catch (Exception ex)
            {
                throw new FileLoadException("文件加载出错，" + ex.Message, ex);
            }
            MassFiles.Add(fileName, massFile);

            return massFile;
        }

        public void UnloadMassFile(string fileName) => MassFiles.Remove(fileName.Trim().ToLower());

        public void AddMassFile(MassFile newMass, string fileName)
        {
            MassFiles.Add(fileName, newMass);
        }
    }
}