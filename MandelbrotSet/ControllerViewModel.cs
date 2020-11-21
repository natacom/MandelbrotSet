using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;

namespace MandelbrotSet
{
    public class ControllerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void NotifyPosition()
        {
            Notify(nameof(X));
            Notify(nameof(Y));
        }
        private void NotifySize()
        {
            Notify(nameof(W));
            Notify(nameof(H));
        }
        private void NotifyCanvas()
        {
            Notify(nameof(X));
            Notify(nameof(Y));
            Notify(nameof(W));
            Notify(nameof(H));
        }

        #region private members

        private int m_CoreNum = 8;

        private BitmapImage m_image = new BitmapImage();
        private bool m_lockAspect = true;

        private Task m_renderingTask;
        private Task m_savingTask;
        private CancellationTokenSource m_renderingCancellation = new CancellationTokenSource();
        private CancellationTokenSource m_savingCancellation = new CancellationTokenSource();

        private CanvasModel m_canvas;

        #endregion

        #region public properties

        public Progress Progress { get; } = new Progress();
        public Information Information { get; } = new Information();

        public double X {
            get => m_canvas.CenterPoint.X;
            set {
                m_canvas.CenterPoint = new System.Windows.Point(value, Y);
                Notify();
                Refresh();
            }
        }
        public double Y {
            get => m_canvas.CenterPoint.Y;
            set {
                m_canvas.CenterPoint = new System.Windows.Point(X, value);
                Notify();
                Refresh();
            }
        }
        public double W {
            get => m_canvas.XYSize.Width;
            set
            {
                double w = value;
                double h = m_canvas.XYSize.Height;
                if (m_lockAspect) {
                    h = (double)m_canvas.Size.Height / m_canvas.Size.Width * value;
                }
                m_canvas.XYSize = new System.Windows.Size(w, h);
                NotifySize();
                Refresh();
            }
        }
        public double H {
            get => m_canvas.XYSize.Height;
            set
            {
                double h = value;
                double w = m_canvas.XYSize.Width;
                if (m_lockAspect) {
                    w = (double)m_canvas.Size.Width / m_canvas.Size.Height * value;
                }
                m_canvas.XYSize = new System.Windows.Size(w, h);
                NotifySize();
                Refresh();
            }
        }
        public int N {
            get => m_canvas.N;
            set {
                m_canvas.N = value;
                Refresh();
            }
        }
        public double Threshold {
            get => m_canvas.Threshold;
            set {
                m_canvas.Threshold = value;
                Refresh();
            }
        }
        public int CoreNum {
            get => m_CoreNum;
            set {
                m_CoreNum = value;
                Refresh();
            }
        }
        public BitmapImage Image {
            get => m_image;
            private set {
                m_image = value;
                Notify();
            }
        }

        public GeneralCommand ResetCommand { get; }
        public bool ShowAxes
        {
            get => m_canvas.ShowAxes;
            set
            {
                m_canvas.ShowAxes = value;
                Notify();
                Refresh();
            }
        }
        public bool FixedAspect {
            get => m_lockAspect;
            set
            {
                m_lockAspect = value;
                Notify();
                UpdateHeightIfNeeded();
            }
        }
        public GeneralCommand SaveCommand { get; }

        #endregion

        public ControllerViewModel()
        {
            ResetCommand = new GeneralCommand(() => true, () =>
            {
                X = -0.5;
                Y = 0;
                W = 3;
                FixedAspect = true;
                Refresh();
            });
            SaveCommand = new GeneralCommand(() => true, SaveImage);
            m_canvas = new CanvasModel(Progress, Information);
            Refresh();
        }

        public void InitAfterLoading()
        {
            // This assignment updates Height by calculating from Width
            // and update the View just after the app is loaded.
            FixedAspect = true;
        }

        private void UpdateHeightIfNeeded()
        {
            if (m_lockAspect) {
                m_canvas.XYSize = new System.Windows.Size(
                    W, (double)m_canvas.Size.Height / m_canvas.Size.Width * W);
                NotifySize();
                Refresh();
            }
        }

        public void SetCanvasSize(double x, double y)
        {
            m_canvas.Size = new System.Windows.Size(x, y);
            UpdateHeightIfNeeded();
            Refresh();
        }

        #region method related to mouse

        private bool m_isWhileDnD = false;
        private System.Windows.Point m_dndBeginPos = new System.Windows.Point();
        private System.Windows.Point m_dndEndPos = new System.Windows.Point();

        public void BeginZoomInSelection(double x, double y)
        {
            m_dndBeginPos = new System.Windows.Point(x, y);
            m_isWhileDnD = true;
        }

        public void DuringZoomInSelection(double x, double y)
        {
            if (m_isWhileDnD) {
                if (FixedAspect) {
                    double aspectRatio = (double)m_canvas.Size.Height / m_canvas.Size.Width;
                    double fixedY = aspectRatio * (x - m_dndBeginPos.X) + m_dndBeginPos.Y;
                    m_dndEndPos = new System.Windows.Point(x, fixedY);
                }
                else {
                    m_dndEndPos = new System.Windows.Point(x, y);
                }
                m_canvas.SetSelectionRectangle(m_dndBeginPos, m_dndEndPos);
                RefreshSelectingRect();
            }
        }

        public void EndZoomInSelection()
        {
            if (m_isWhileDnD) {
                m_canvas.UpdatePosAndScale();
                NotifyCanvas();
                Refresh();
                m_isWhileDnD = false;
            }
        }

        public void CancelZoomInSelection()
        {
            RefreshSelectingRect(false);
            m_isWhileDnD = false;
        }

        public void ZoomIn(int n = 2)
        {
            W /= n;
            H /= n;
        }

        public void ZoomOut(int n = 2)
        {
            W *= n;
            H *= n;
        }

        public void Move(double x, double y)
        {
            if (!m_isWhileDnD) {
                m_canvas.UpdatePositionFromCanvasPoint(x, y);
                NotifyPosition();
                Refresh();
            }
        }

        private void RefreshSelectingRect(bool showRectangle = true)
        {
            var bitmap = m_canvas.DrawSelectionRectangle(showRectangle);
            BitmapToImageSource(bitmap);
        }

        #endregion

        public void SaveImage()
        {
            if (m_canvas.Size.Width == 0 || m_canvas.Size.Height == 0) {
                return;
            }

            if (!m_savingTask?.IsCompleted ?? false) {
                m_savingCancellation.Cancel();
                try {
                    m_savingTask.Wait();
                }
                catch (Exception) {
                    // The Task.Wait() throw an exception when the thread is cancelled.
                }
                m_savingCancellation.Dispose();
                m_savingCancellation = new CancellationTokenSource();
            }

            m_savingTask = Task.Run(() =>
            {
                try {
                    var bitmap = m_canvas.Render(false, m_CoreNum, m_renderingCancellation.Token);

                    using (var fs = new FileStream("output.png", FileMode.OpenOrCreate)) {
                        bitmap.Save(fs, ImageFormat.Png);
                    }
                }
                catch (OperationCanceledException) {
                    /* do nothing */
                }
            }, m_renderingCancellation.Token);
        }

        public void Refresh()
        {
            if (m_canvas.Size.Width == 0 || m_canvas.Size.Height == 0) {
                return;
            }

            if (!m_renderingTask?.IsCompleted ?? false) {
                m_renderingCancellation.Cancel();
                try {
                    m_renderingTask.Wait();
                }
                catch (Exception) {
                    // The Task.Wait() throw an exception when the thread is cancelled.
                }
                m_renderingTask.Dispose();
                m_renderingCancellation.Dispose();
                m_renderingCancellation = new CancellationTokenSource();
            }

            m_renderingTask = Task.Run(() =>
            {
                try {
                    var bitmap = m_canvas.Render(true, m_CoreNum, m_renderingCancellation.Token);
                    BitmapToImageSource(bitmap);
                }
                catch(OperationCanceledException) {
                    /* do nothing */
                }
            }, m_renderingCancellation.Token);
        }

        private void BitmapToImageSource(Bitmap bitmap)
        {
            using (var ms = new MemoryStream()) {
                bitmap.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                var tmpImg = new BitmapImage();
                tmpImg.BeginInit();
                tmpImg.CacheOption = BitmapCacheOption.OnLoad;
                tmpImg.StreamSource = ms;
                tmpImg.EndInit();
                tmpImg.Freeze();
                Image = tmpImg;
            }
        }
    }
}
