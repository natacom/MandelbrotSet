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
    public class ControllerViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName]string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #region private members

        private Size m_canvasSize = new Size(1, 1);
        private double m_X = -0.5;
        private double m_Y = 0;
        private double m_W = 3;
        private double m_H = 2.11;

        private int m_N = 1000;
        private double m_Threshold = 2;

        private BitmapImage m_image = new BitmapImage();
        private bool m_showAxes = false;
        private bool m_lockAspect = true;

        private Task m_renderingTask;
        private Task m_savingTask;
        private CancellationTokenSource m_renderingCancellation = new CancellationTokenSource();
        private CancellationTokenSource m_savingCancellation = new CancellationTokenSource();

        #endregion

        #region public properties

        public Progress Progress { get; } = new Progress();
        public Information Information { get; } = new Information();

        public double X { get => m_X; set { m_X = value; Refresh(); } }
        public double Y { get => m_Y; set { m_Y = value; Refresh(); } }
        public double W {
            get => m_W;
            set
            {
                m_W = value;
                if (m_lockAspect) {
                    m_H = (double)m_canvasSize.Height / m_canvasSize.Width * value;
                    Notify(nameof(H));
                }
                Refresh();
            }
        }
        public double H {
            get => m_H;
            set
            {
                m_H = value;
                if (m_lockAspect) {
                    m_W = (double)m_canvasSize.Width / m_canvasSize.Height * value;
                    Notify(nameof(W));
                }
                Refresh();
            }
        }
        public int N { get => m_N; set { m_N = value; Refresh(); } }
        public double Threshold { get => m_Threshold; set { m_Threshold = value; Refresh(); } }
        public BitmapImage Image { get => m_image; private set { m_image = value; Notify(); } }

        public GeneralCommand RefreshCommand { get; }
        public bool ShowAxes
        {
            get => m_showAxes;
            set
            {
                m_showAxes = value;
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
            RefreshCommand = new GeneralCommand(() => true, Refresh);
            SaveCommand = new GeneralCommand(() => true, SaveImage);
            Refresh();
        }

        public void InitAfterLoading()
        {
            FixedAspect = true;
        }

        private void UpdateHeightIfNeeded()
        {
            if (m_lockAspect) {
                m_H = (double)m_canvasSize.Height / m_canvasSize.Width * W;
                Notify(nameof(H));
                Refresh();
            }
        }

        public void SetCanvasSize(int x, int y)
        {
            m_canvasSize = new Size(x, y);
            UpdateHeightIfNeeded();
            Refresh();
        }

        #region methods related to image

        public void SaveImage()
        {
            if (m_canvasSize.Width == 0 || m_canvasSize.Height == 0) {
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
                    Bitmap bitmap = Render(false);

                    using (FileStream fs = new FileStream("output.png", FileMode.OpenOrCreate)) {
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
            if (m_canvasSize.Width == 0 || m_canvasSize.Height == 0) {
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
                m_renderingCancellation.Dispose();
                m_renderingCancellation = new CancellationTokenSource();
            }

            m_renderingTask = Task.Run(() =>
            {
                try {
                    Bitmap bitmap = Render(true);

                    using (MemoryStream ms = new MemoryStream()) {
                        bitmap.Save(ms, ImageFormat.Bmp);
                        ms.Seek(0, SeekOrigin.Begin);
                        BitmapImage tmpImg = new BitmapImage();
                        tmpImg.BeginInit();
                        tmpImg.CacheOption = BitmapCacheOption.OnLoad;
                        tmpImg.StreamSource = ms;
                        tmpImg.EndInit();
                        tmpImg.Freeze();
                        Image = tmpImg;
                    }
                }
                catch(OperationCanceledException) {
                    /* do nothing */
                }
            }, m_renderingCancellation.Token);
        }

        private Bitmap Render(bool isForRendering)
        {
            Bitmap bitmap = new Bitmap(m_canvasSize.Width, m_canvasSize.Height);
            Graphics g = Graphics.FromImage(bitmap);
            g.FillRectangle(Brushes.White, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

            // Fill each pixel with calculated colour.
            for (int x = 0; x < bitmap.Width; ++x) {
                const int maxConcurrency = 12;
                List<Task> tasks = new List<Task>();
                Color[] colors = new Color[bitmap.Height];

                // Calculate each pixel asynchronously
                using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(maxConcurrency)) {
                    for (int y = 0; y < bitmap.Height; ++y) {
                        concurrencySemaphore.Wait();
                        int _x = x;
                        int _y = y;
                        Task task = Task.Factory.StartNew(() =>
                        {
                            PointF pos = CalculatePositionFromCanvasPos(_x, _y);
                            colors[_y] = CalculateColour(pos);

                            concurrencySemaphore.Release();
                        });

                        tasks.Add(task);
                    }
                    Task.WaitAll(tasks.ToArray());
                }

                // Fill bitmap pixels with colours
                for (int y = 0; y < bitmap.Height; ++y) {
                    if (colors[y] != Color.Transparent) {
                        bitmap.SetPixel(x, y, colors[y]);
                    }
                }

                // update Progress
                Progress.UpdateProgress(100 * x / bitmap.Width, isForRendering);

                // for cancellation
                m_renderingCancellation.Token.ThrowIfCancellationRequested();
            }
            Progress.UpdateProgress(-1, isForRendering);

            if (m_showAxes) {
                // Draw axes of the coordinate
                Point org = CalculateOriginOnCanvas();
                g.DrawLine(Pens.Gray, new Point(org.X, 0), new Point(org.X, bitmap.Height));
                g.DrawLine(Pens.Gray, new Point(0, org.Y), new Point(bitmap.Width, org.Y));
            }

            g.Flush();

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bitmap;
        }

        private Point CalculateOriginOnCanvas()
        {
            double posX = W / 2;
            double orgX = posX - X;
            double ratioX = orgX / W;

            double posY = H / 2;
            double orgY = posY - Y;
            double ratioY = orgY / H;

            return new Point((int)(m_canvasSize.Width * ratioX), (int)(m_canvasSize.Height * ratioY));
        }

        private PointF CalculatePositionFromCanvasPos(int x, int y)
        {
            double ratioX = (double)x / m_canvasSize.Width;
            double posX = W * ratioX - W / 2 + X;

            double ratioY = (double)y / m_canvasSize.Height;
            double posY = H * ratioY - H / 2 + Y;

            return new PointF((float)posX, (float)posY);
        }

        private Color CalculateColour(PointF pos)
        {
            bool isOverThreshold = false;

            double X_n = 0;
            double Y_n = 0;
            int n = 0;
            while (n++ < N) {
                double nextX = X_n * X_n - Y_n * Y_n + pos.X;
                double nextY = 2 * X_n * Y_n + pos.Y;
                X_n = nextX;
                Y_n = nextY;
                if (X_n * X_n + Y_n * Y_n > Threshold * Threshold) {
                    isOverThreshold = true;
                    break;
                }
            }

            return isOverThreshold ? Color.Transparent : Color.Black;
        }

        #endregion
    }
}
