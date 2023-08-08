using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.MaterialForYou.Colors
{
    public class MaterialTonalColor
    {
        private Color KeyColor;

        public MaterialTonalColor(string colorString) => makeUp(ColorTranslator.FromHtml(colorString));

        public MaterialTonalColor(int colorHex) => makeUp(Color.FromArgb(colorHex));

        public MaterialTonalColor(int a, int r, int g, int b) => makeUp(Color.FromArgb(a, r, g, b));

        public MaterialTonalColor(Color color) => makeUp(color);

        public void makeUp(Color color) => KeyColor = color;

        public Color No0 => Color.Black;

        public Color No10 => calculateColor(10);

        public Color No20 => calculateColor(20);

        public Color No30 => calculateColor(30);

        public Color No40 => calculateColor(40);

        public Color No50 => calculateColor(50);

        public Color No60 => calculateColor(60);

        public Color No70 => calculateColor(70);

        public Color No80 => calculateColor(80);

        public Color No90 => calculateColor(90);

        public Color No95 => calculateColor(95);

        public Color No99 => calculateColor(99);

        public Color No100 => Color.White;

        private Color calculateColor(int tone)
        {
            // 计算调制颜色的Red、Green、Blue通道值
            int red = KeyColor.R + (int)((255 - KeyColor.R) * tone / 100f);
            int green = KeyColor.G + (int)((255 - KeyColor.G) * tone / 100f);
            int blue = KeyColor.B + (int)((255 - KeyColor.B) * tone / 100f);

            // 创建并返回调制颜色
            return Color.FromArgb(KeyColor.A, red, green, blue);
        }
    }
}
