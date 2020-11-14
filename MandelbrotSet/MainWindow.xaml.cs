using System.Windows;

namespace MandelbrotSet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly ControllerViewModel controller_vm = new ControllerViewModel();

        public MainWindow()
        {
            InitializeComponent();

            image.SizeChanged += (sender, arg) => controller_vm.SetCanvasSize((int)image.ActualWidth, (int)image.ActualHeight);
            Loaded += (sender, arg) =>
            {
                controller_vm.SetCanvasSize((int)image.ActualWidth, (int)image.ActualHeight);
                controller_vm.InitAfterLoading();
            };
            image.MouseDown += (sender, arg) =>
            {
                if (arg.ChangedButton == System.Windows.Input.MouseButton.Left)
                    controller_vm.BeginDnD((int)arg.GetPosition(image).X, (int)arg.GetPosition(image).Y);
            };
            image.MouseUp += (sender, arg) =>
            {
                if (arg.ChangedButton == System.Windows.Input.MouseButton.Left)
                    controller_vm.EndDnD();
            };
            image.MouseMove += (sender, arg) => controller_vm.DuringDnD((int)arg.GetPosition(image).X, (int)arg.GetPosition(image).Y);

            DataContext = new {
                Controller = controller_vm,
            };
        }
    }
}
