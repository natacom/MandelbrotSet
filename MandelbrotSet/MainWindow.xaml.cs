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
                    controller_vm.BeginZoomInSelection((int)arg.GetPosition(image).X, (int)arg.GetPosition(image).Y);
                else if (arg.ChangedButton == System.Windows.Input.MouseButton.Right)
                    controller_vm.CancelZoomInSelection();
            };
            image.MouseUp += (sender, arg) =>
            {
                if (arg.ChangedButton == System.Windows.Input.MouseButton.Left)
                    controller_vm.EndZoomInSelection();
                else if (arg.ChangedButton == System.Windows.Input.MouseButton.Right)
                    controller_vm.Move((int)arg.GetPosition(image).X, (int)arg.GetPosition(image).Y);
            };
            image.MouseWheel += (sender, arg) =>
            {
                if (arg.Delta > 0) {
                    controller_vm.ZoomIn();
                }
                else if (arg.Delta < 0) {
                    controller_vm.ZoomOut();
                }
            };
            image.MouseMove += (sender, arg) => controller_vm.DuringZoomInSelection((int)arg.GetPosition(image).X, (int)arg.GetPosition(image).Y);

            DataContext = new {
                Controller = controller_vm,
            };
        }
    }
}
