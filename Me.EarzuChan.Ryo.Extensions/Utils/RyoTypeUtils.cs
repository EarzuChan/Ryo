using Me.EarzuChan.Ryo.Core.Adaptations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Extensions.Utils
{
    public static class RyoTypeUtils
    {
        public static RyoType JavaClassToRyoType(this string javaClz) => AdaptationManager.INSTANCE.GetRyoTypeByJavaClz(javaClz);

        public static RyoType ToRyoType(this Type csType) => AdaptationManager.INSTANCE.GetRyoTypeByCsClz(csType);

        public static RyoType GetArrayElementRyoType(this RyoType ryoType) => AdaptationManager.INSTANCE.GetArrayElementRyoType(ryoType);

        public static Type ToCsType(this RyoType ryoType) => AdaptationManager.INSTANCE.GetCsClzByRyoType(ryoType)!;

        public static string ToJavaClass(this RyoType ryoType) => AdaptationManager.INSTANCE.GetJavaClzByRyoType(ryoType)!;
    }
}
