using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RyoWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var position = Mouse.GetPosition(canvas);

            var ellipse = new Ellipse
            {
                Width = 0,
                Height = 0,
                StrokeThickness = 2,
                Fill = Brushes.White,
                Opacity = 0.5
            };

            var aniSize = Math.Max(button.ActualWidth, button.ActualHeight) * 3;

            canvas.Children.Add(ellipse);

            var storyboard = new Storyboard();

            var animation = new DoubleAnimation
            {
                From = 0,
                To = aniSize,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animation, ellipse);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Width"));
            storyboard.Children.Add(animation);

            animation = new DoubleAnimation
            {
                From = position.X,
                To = position.X - aniSize / 2,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animation, ellipse);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Left)"));
            storyboard.Children.Add(animation);

            animation = new DoubleAnimation
            {
                From = position.Y,
                To = position.Y - aniSize / 2,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animation, ellipse);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Top)"));
            storyboard.Children.Add(animation);

            animation = new DoubleAnimation
            {
                From = 0,
                To = aniSize,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animation, ellipse);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Height"));
            storyboard.Children.Add(animation);

            storyboard.Completed += (s, _) => canvas.Children.Remove(ellipse);
            storyboard.Begin();

            // Trace.WriteLine($"左：{ellipse.Width} 上：{ellipse.Height}");
            Canvas.SetLeft(ellipse, position.X);
            Canvas.SetTop(ellipse, position.Y);
        }
    }
}
