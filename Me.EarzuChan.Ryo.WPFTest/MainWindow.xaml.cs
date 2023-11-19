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
        private event EventHandler? OnFieldChangedHandler;

        public FieldEditor(object obj)
        {
            Obj = obj;
            Orientation = Orientation.Vertical;

            FlushView();
        }

        private void FlushView()
        {
            Children.Clear();

            if (Obj == null)
            {
                Children.Add(new Label()
                {
                    Content = "Null Value"
                });
            }
            else
            {

                // 获取对象的类型信息
                Type objectType = Obj.GetType();
                FieldInfo[] fields = objectType.GetFields();

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
                        Margin = new(8)
                    };

                    IFieldEditor editor = fieldType == typeof(string) ? new StringEditor((string)fieldValue) : fieldType == typeof(int) ? new IntEditor((int)fieldValue) : new FieldEditor(fieldValue);

                    editor.AddOnValueChangedListener(value =>
                    {
                        Trace.WriteLine($"{field.Name}改变为({(value ?? "NULL")})");
                        field.SetValue(Obj, value);
                        OnFieldChangedHandler?.Invoke(this, EventArgs.Empty);
                    });

                    holder.Children.Add((UIElement)editor);

                    // 将 Label 和 TextBox 添加到 StackPanel 中
                    Children.Add(label);
                    Children.Add(holder);
                }
            }

            Button flushViewButton = new() { Content = "Flush View" };
            flushViewButton.Click += (_, _) =>
            {
                FlushView();
            };
            Children.Add(flushViewButton);
        }

        public void AddOnValueChangedListener(Action<object> listener)
        {
            OnFieldChangedHandler += (_, _) => listener(Obj);
        }
    }

    internal interface IFieldEditor
    {
        // object GetObj();

        void AddOnValueChangedListener(Action<object> listener);
    }

    public class StringEditor : StackPanel, IFieldEditor
    {
        private Button CreateEmptyStringButton = new() { Content = "Create Empty String" };
        private Button SetStringToNullButton = new() { Content = "Set String To Null" };
        private TextBox TextEditBox = new();

        private event EventHandler? OnFieldChangedHandler;

        private string? StringRef
        {
            get => StringStorage;
            set
            {
                Trace.WriteLine($"文本状态监测：{value == null} {lastStatus} {value}");

                if ((value == null) != lastStatus)
                {
                    Children.Clear();

                    lastStatus = value == null;

                    if (lastStatus.Value)
                    {
                        Children.Add(CreateEmptyStringButton);
                    }
                    else
                    {
                        Children.Add(TextEditBox);
                        Children.Add(SetStringToNullButton);
                    }
                }
                StringStorage = value;

                OnFieldChangedHandler?.Invoke(value, EventArgs.Empty);
            }
        }

        private bool? lastStatus = null;
        private string? StringStorage;

        public StringEditor(string str, bool nullable = true)
        {
            Orientation = Orientation.Horizontal;

            if (!nullable)
            {
                str ??= "";
                SetStringToNullButton.Visibility = Visibility.Collapsed;
            }

            StringRef = str;

            CreateEmptyStringButton.Click += (_, _) =>
            {
                StringRef = "";
                TextEditBox.Text = StringRef;
            };
            SetStringToNullButton.Click += (_, _) => StringRef = null;

            TextEditBox.Text = StringRef;
            TextEditBox.TextChanged += (_, _) => { if (StringRef != TextEditBox.Text) StringRef = TextEditBox.Text; };
        }

        public void AddOnValueChangedListener(Action<object> listener)
        {
            OnFieldChangedHandler += (value, _) => listener(value);
        }
    }

    public class IntEditor : StackPanel, IFieldEditor
    {
        private TextBox NumEditBox = new();
        private Label ErroringLabel = new() { Content = "Erroring", Visibility = Visibility.Collapsed };
        public IntEditor(int num)
        {
            Orientation = Orientation.Horizontal;
            Children.Add(NumEditBox);
            Children.Add(ErroringLabel);

            NumEditBox.Text = num.ToString();
        }

        public void AddOnValueChangedListener(Action<object> listener)
        {
            NumEditBox.TextChanged += (_, _) =>
            {
                try
                {
                    int value = int.Parse(NumEditBox.Text);
                    ErroringLabel.Visibility = Visibility.Collapsed;
                    listener(value);
                }
                catch (Exception)
                {
                    ErroringLabel.Visibility = Visibility.Visible;
                    Trace.WriteLine("整数解析失败");
                }
            };

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