using Me.EarzuChan.Ryo.Core.Adaptations;
using Me.EarzuChan.Ryo.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Core.Utils
{
    public static class AdaptationUtils
    {
        public static Type? SearchAdaptableFormations(string clzName)
        {
            Type? baseType = null;
            var types = TypeUtils.GetAppAllTypes();
            foreach (var item in types)
            {
                var attribute = item.GetCustomAttribute<AdaptableFormationAttribute>();
                if (attribute != null)
                {
                    if (attribute.FormationName == clzName)
                    {
                        baseType = item;
                        break;
                    }
                }
            }
            return baseType;
        }
    }
}
