using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class TypeUtils
    {
        public static IEnumerable<Type> GetAppAllTypes() => AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes());
    }
}
