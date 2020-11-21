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

            Loaded += MainWindow_Loaded;
            image.MouseDown += Image_MouseDown;
            image.MouseUp += Image_MouseUp;
            image.MouseWheel += Image_MouseWheel;
            image.MouseMove += Image_MouseMove;
            image.SizeChanged += Image_SizeChanged;

            DataContext = new {
                Controller = controller_vm,
            };
        }

        #region event handlers

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            controller_vm.SetCanvasSize((int)image.ActualWidth, (int)image.ActualHeight);
            controller_vm.InitAfterLoading();
        }

        private void Image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left) {
                controller_vm.BeginZoomInSelection(
                    (int)e.GetPosition(image).X,
                    (int)e.GetPosition(image).Y);
            }
            else if (e.ChangedButton == System.Windows.Input.MouseButton.Right) {
                controller_vm.Move(
                    e.GetPosition(image).X,
                    e.GetPosition(image).Y);
            }
        }

        private void Image_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left) {
                controller_vm.EndZoomInSelection();
            }
            else if (e.ChangedButton == System.Windows.Input.MouseButton.Right) {
                controller_vm.CancelZoomInSelection();
            }
        }

        private void Image_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0) {
                controller_vm.ZoomIn();
            }
            else if (e.Delta < 0) {
                controller_vm.ZoomOut();
            }
        }

        private void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            controller_vm.DuringZoomInSelection(
                (int)e.GetPosition(image).X,
                (int)e.GetPosition(image).Y);
        }

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            controller_vm.SetCanvasSize((int)image.ActualWidth, (int)image.ActualHeight);
        }

        #endregion
    }
}
