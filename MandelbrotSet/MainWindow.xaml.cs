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

            SizeChanged += (sender, arg) => controller_vm.SetCanvasSize((int)image.ActualWidth, (int)image.ActualHeight);
            Loaded += (sender, arg) =>
            {
                controller_vm.SetCanvasSize((int)image.ActualWidth, (int)image.ActualHeight);
                controller_vm.InitAfterLoading();
            };

            DataContext = new {
                Controller = controller_vm,
            };
        }
    }
}
