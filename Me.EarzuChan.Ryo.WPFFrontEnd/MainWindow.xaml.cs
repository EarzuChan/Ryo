using Me.EarzuChan.MaterialForYou;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Me.EarzuChan.Ryo.WPFFrontEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UIManager uiManager;

        public MainWindow()
        {
            InitializeComponent();

            uiManager = new UIManager();

            var color = uiManager.StyleProfile.ColorProfile.Primary;
            Trace.WriteLine($"Primary：#{color.R:X2}{color.G:X2}{color.B:X2}");

            // AB.Click
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            uiManager.UnapplyFor(this);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            uiManager.ApplyFor(this);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
