using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace Me.EarzuChan.MaterialForYou.Effects
{
    public class MaterialEffect
    {
        // 时长
        // 资源
        // 参数

        public class MaterialEffectBlob
        {
            // 参数
            public string Property { get; set; }

            // 传入元素的字段
            public string? Field { get; set; }

            // 是否覆盖所选字段
            public object? NewObject { get; set; }

            public double Delay { get; set; }

            public AnimationTimeline Animation { get; set; }

            public MaterialEffectBlob(string property, double delay, AnimationTimeline animation)
            {
                Property = property;
                Delay = delay;
                Animation = animation;
            }
        }

        public MaterialEffectBlob[] Effects { get; set; } = Array.Empty<MaterialEffectBlob>();
    }
}
