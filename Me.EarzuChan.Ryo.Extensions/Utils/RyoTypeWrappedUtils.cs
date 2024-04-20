using Me.EarzuChan.Ryo.Core.Adaptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Extensions.Utils
{
    public static class RyoTypeWrappedUtils
    {
        public static RyoType JavaClassToRyoType(this string javaClz) => AdaptionManager.INSTANCE.GetRyoTypeByJavaClz(javaClz);

        public static RyoType ToRyoType(this Type csType) => AdaptionManager.INSTANCE.GetRyoTypeByCsClz(csType);

        public static RyoType GetArrayElementRyoType(this RyoType ryoType) => AdaptionManager.INSTANCE.GetArrayElementRyoType(ryoType);

        public static Type ToType(this RyoType ryoType) => AdaptionManager.INSTANCE.GetCsClzByRyoType(ryoType)!;

        public static string ToJavaClass(this RyoType ryoType) => AdaptionManager.INSTANCE.GetJavaClzByRyoType(ryoType)!;
    }
}
