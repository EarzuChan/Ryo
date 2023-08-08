using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace Me.EarzuChan.MaterialForYou.Effects
{
    public static class EffectsHandler
    {
        public static async void PlayEffectFor(UIElement element, MaterialEffect effect)
        {
            Trace.WriteLine(element + "播放动画");

            // 获取资源然后拨弄
            // 资源是直接获取时长、故事版、在这里就直接Run和等待就行了

            /*// 创建并启动故事板
            Storyboard storyboard = new();
            storyboard.Children.Add(opacityAnimation);
            Storyboard.SetTarget(opacityAnimation, element);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Begin();

            await Task.Delay(200);

            opacityAnimation = new()
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(0.1)
            };

            storyboard = new();
            storyboard.Children.Add(opacityAnimation);
            Storyboard.SetTarget(opacityAnimation, element);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
            storyboard.Begin();*/
        }
    }
}
