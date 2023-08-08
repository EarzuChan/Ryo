using Me.EarzuChan.MaterialForYou.Colors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.MaterialForYou.Effects
{
    public static class DefaultEffectProfiles
    {
        public static readonly MaterialEffectProfile DefaultEffectProfile = new()
        {
            RippleEffect = RippleEffectFactory.Create()
        };
    }
}
