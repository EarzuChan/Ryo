using Me.EarzuChan.MaterialForYou.Colors;
using Me.EarzuChan.MaterialForYou.Effects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.MaterialForYou.Styles
{
    public static class DefaultStyleProfiles
    {
        public static readonly MaterialStyleProfile DefaultStyleProfile = new()
        {
            ColorProfile = DefaultColorProfiles.DefaultColorProfile,
            EffectProfile = DefaultEffectProfiles.DefaultEffectProfile
        };
    }
}
