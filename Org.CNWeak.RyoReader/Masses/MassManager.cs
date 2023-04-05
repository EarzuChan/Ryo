using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Masses
{
    public class MassManager
    {
        public static MassManager INSTANCE { get { instance ??= new(); return instance; } }
        private static MassManager? instance;

        public MassManager() => PreloadProfiles();

        private void PreloadProfiles()
        {
            // 加载本地Format等
        }

        public List<MassFile> MassList = new();

        public MassFile? GetMassFileByFileName(string fileName) => MassList.Find((MassFile m) => m.Name.ToLower() == fileName.ToLower());
    }
}
