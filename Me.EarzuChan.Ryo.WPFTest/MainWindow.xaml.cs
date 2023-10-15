using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Me.EarzuChan.Ryo.WPFTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public object Obj = new TestClass() { Inner = new() };

        public MainWindow()
        {
            InitializeComponent();

            ViewHost.Children.Add(new FieldEditor(Obj));
        }
    }

    public class FieldEditor : StackPanel, IFieldEditor
    {
        private readonly object Obj;

        public FieldEditor(object obj)
        {
            Obj = obj;

            InitView();
        }

        private void InitView()
        {
            if (Obj == null)
            {
                Children.Add(new Label()
                {
                    Content = "Null Value"
                });
            }
            else
            {
                Children.Clear();

                // 获取对象的类型信息
                Type objectType = Obj.GetType();
                FieldInfo[] fields = objectType.GetFields();

                // 创建一个 StackPanel 用于容纳字段编辑器
                Orientation = Orientation.Vertical;

                Trace.WriteLine($"初始化 {objectType}有{fields.Length}个");

                // 遍历对象的属性并创建编辑器
                foreach (FieldInfo field in fields)
                {
                    Trace.WriteLine($"添加：{field.Name}");

                    // 创建 Label 显示属性名称
                    Label label = new()
                    {
                        Content = field.Name
                    };

                    // 创建 子Editor 用于编辑属性值
                    Type fieldType = field.FieldType;
                    object fieldValue = field.GetValue(Obj);

                    StackPanel holder = new()
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new(10)
                    };

                    IFieldEditor editor = fieldType == typeof(string) ? new StringEditor((string)fieldValue) : fieldType == typeof(int) ? new IntEditor((int)fieldValue) : new FieldEditor(fieldValue);
                    Button updateButton = new()
                    {
                        Content = "Update"
                    };

                    updateButton.Click += (_, _) =>
                    {
                        field.SetValue(Obj, editor.GetObj());
                        InitView();
                    };

                    holder.Children.Add((UIElement)editor);
                    holder.Children.Add(updateButton);

                    // 将 Label 和 TextBox 添加到 StackPanel 中
                    Children.Add(label);
                    Children.Add(holder);
                }
            }
        }

        public object GetObj() { return Obj; }
    }

    internal interface IFieldEditor
    {
        object GetObj();
    }

    public class StringEditor : TextBox, IFieldEditor
    {
        public StringEditor(string str)
        {
            str ??= string.Empty;
            Text = str;
        }

        public object GetObj()
        {
            return Text;
        }
    }

    public class IntEditor : TextBox, IFieldEditor
    {
        public IntEditor(int str)
        {
            Text = str.ToString();
        }

        public object GetObj()
        {
            return int.Parse(Text);
        }
    }

    public class TestClass
    {
        public string Str = "Test";

        public int Num = 1919810;

        public TestClass? Inner;

        public TestClass() { }
    }
}