using Me.EarzuChan.MaterialForYou.Effects;
using Me.EarzuChan.MaterialForYou.Styles;
using Me.EarzuChan.MaterialForYou.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Me.EarzuChan.MaterialForYou
{
    public class UIManager
    {
        // 配置项
        private readonly ManagedClickablesList Clickables;
        private readonly List<UIElement> AppliedElements = new();

        public MaterialStyleProfile StyleProfile
        {
            set
            {
                NotifyStyleProfileChanged();
                _styleProfile = value;
            }
            get => _styleProfile!;
        }

        private void NotifyStyleProfileChanged()
        {
            Trace.WriteLine(this + "风格配置项被更改");
        }

        private MaterialStyleProfile? _styleProfile;

        // 初始化
        public UIManager()
        {
            Clickables = new(this);

            StyleProfile = DefaultStyleProfiles.DefaultStyleProfile;
        }

        // 为所给控件设置样式
        public void ApplyFor(UIElement element)
        {
            if (element == null) throw new ArgumentNullException("不能为Null应用");
            else if (AppliedElements.Contains(element)) throw new NotSupportedException(element + "已被应用");
            Trace.WriteLine(element + "正在被应用");

            if (element is Button)
            {
                Trace.WriteLine(element + "为按钮");

                Clickables.Add(element);
            }
            else if (element is Panel container)
            {
                foreach (UIElement subElement in container.Children)
                {
                    if (subElement == null)
                    {
                        Trace.WriteLine(subElement + "为Null，已被跳过");
                        continue;
                    }

                    ApplyFor(subElement);
                }
            }
            else if (element is ContentControl contentControl)
            {
                Trace.WriteLine(element + "为容器，将应用于其子项");

                ApplyFor((UIElement)contentControl.Content);
            }

            AppliedElements.Add(element);
            Trace.WriteLine(element + "已应用");
        }

        // 写一个方法卸载（取消）指定控件，取消应用的逻辑如上
        public void UnapplyFor(UIElement element)
        {
            if (element == null) throw new ArgumentNullException("不能为Null取消应用");
            else if (!AppliedElements.Contains(element)) throw new NotSupportedException(element + "未被应用");
            Trace.WriteLine(element + "正在被取消应用");

            if (element is Button)
            {
                Trace.WriteLine(element + "为按钮");

                Clickables.Remove(element);
            }
            else if (element is Panel container)
            {
                foreach (UIElement subElement in container.Children)
                {
                    if (subElement == null)
                    {
                        Trace.WriteLine(subElement + "为Null，已被跳过");
                        continue;
                    }

                    UnapplyFor(subElement);
                }
            }
            else if (element is ContentControl contentControl)
            {
                Trace.WriteLine(element + "为容器，将取消应用于其子项");

                UnapplyFor((UIElement)contentControl.Content);
            }

            AppliedElements.Remove(element);
            Trace.WriteLine(element + "已取消应用");
        }

        public void BindClickable(UIElement clickable)
        {
            if (clickable is Button button)
            {
                button.MouseEnter += OnButtonElementMouseHover;
                button.PreviewMouseUp += OnButtonElementMouseUp;
                button.PreviewMouseDown += OnButtonElementMouseDown;

                Trace.WriteLine(button + "已绑定按钮单击事件");
            }
        }

        public void UnbindClickable(UIElement clickable)
        {
            if (clickable is Button button)
            {
                button.MouseEnter -= OnButtonElementMouseHover;
                button.Click -= OnButtonElementMouseUp;
                button.PreviewMouseDown -= OnButtonElementMouseDown;

                Trace.WriteLine(button + "已取消绑定按钮单击事件");
            }
        }

        // 按钮按下事件
        private void OnButtonElementMouseUp(object clickable, RoutedEventArgs eventArgs)
        {
            var button = clickable is Button but ? but : throw new ArrayTypeMismatchException(clickable + "不是可点击项");

            Trace.WriteLine(button + "鼠标松开");

            /*Storyboard storyboard = new();

            // 创建一个 DoubleAnimation，以便将按钮从当前位置向右移动 100 个单位
            DoubleAnimation animationX = new DoubleAnimation(0, 100, new Duration(TimeSpan.FromSeconds(1)));
            Storyboard.SetTargetProperty(animationX, new PropertyPath(Canvas.LeftProperty));
            Storyboard.SetTarget(animationX, button);

            // 创建一个 DoubleAnimation，以便将按钮从当前位置向下移动 100 个单位
            DoubleAnimation animationY = new DoubleAnimation(0, 100, new Duration(TimeSpan.FromSeconds(1)));
            Storyboard.SetTargetProperty(animationY, new PropertyPath(Canvas.TopProperty));
            Storyboard.SetTarget(animationY, button);

            // 将两个动画添加到 Storyboard 中
            storyboard.Children.Add(animationX);
            storyboard.Children.Add(animationY);

            // 在按钮上播放 Storyboard
            storyboard.Begin(button);*/
        }

        // 按钮松开事件
        private void OnButtonElementMouseDown(object clickable, RoutedEventArgs eventArgs)
        {
            var button = clickable is Button but ? but : throw new ArrayTypeMismatchException(clickable + "不是可点击项");

            Trace.WriteLine(button + "鼠标按下");

            EffectsHandler.PlayEffectFor(button, StyleProfile.EffectProfile.RippleEffect);
        }

        // 按钮悬停事件
        private void OnButtonElementMouseHover(object clickable, RoutedEventArgs eventArgs)
        {
            var button = clickable is Button but ? but : throw new ArrayTypeMismatchException(clickable + "不是可点击项");

            Trace.WriteLine(button + "鼠标悬停");
        }

        // 写一个方法卸载（取消）指定控件，取消应用的逻辑如上
    }

    // 本来是想实现打上注解自动附加UI管理器
    /*[AttributeUsage(AttributeTargets.Class)]
    public class UseMaterialForYou : Attribute
    { }*/
}
