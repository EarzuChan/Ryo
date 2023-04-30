using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.MaterialForYou.Colors
{
    public class MaterialColorProfile
    {
        // 各种Key
        public MaterialTonalColor? PrimaryTonal { get; set; }

        public MaterialTonalColor? SecondaryTonal { get; set; }

        public MaterialTonalColor? TertiaryTonal { get; set; }

        public MaterialTonalColor? ErrorTonal { get; set; }

        public MaterialTonalColor? NeutralTonal { get; set; }

        public MaterialTonalColor? NeutralVariantTonal { get; set; }

        public Color Primary { get => PrimaryTonal!.No40; }
    }
}
