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

        private int m_N = 10000;
        private double m_Threshold = 2;

        Bitmap m_imageCache;
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

        public double X {
            get => m_X;
            set {
                m_X = value;
                Notify();
                Refresh();
            }
        }
        public double Y {
            get => m_Y;
            set {
                m_Y = value;
                Notify();
                Refresh();
            }
        }
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

        public GeneralCommand ResetCommand { get; }
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
            ResetCommand = new GeneralCommand(() => true, () =>
            {
                X = -0.5;
                Y = 0;
                W = 3;
                FixedAspect = true;
                Refresh();
            });
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

        #region method related to mouse

        private bool m_isWhileDnD = false;
        private Point m_dndBeginPos = Point.Empty;
        private Point m_dndEndPos = Point.Empty;

        public void BeginDnD(int x, int y)
        {
            m_dndBeginPos = new Point(x, y);
            m_isWhileDnD = true;
        }

        public void DuringDnD(int x, int y)
        {
            if (m_isWhileDnD) {
                if (FixedAspect) {
                    double aspectRatio = (double)m_canvasSize.Height / m_canvasSize.Width;
                    int fixedY = (int)(aspectRatio * (x - m_dndBeginPos.X)) + m_dndBeginPos.Y;
                    m_dndEndPos = new Point(x, fixedY);
                }
                else {
                    m_dndEndPos = new Point(x, y);
                }
                RefreshSelectingRect();
            }
        }

        public void EndDnD()
        {
            if (m_isWhileDnD) {
                UpdatePosAndScale();
                m_isWhileDnD = false;
            }
        }

        public void CancelDnD()
        {
            RefreshSelectingRect(false);
            m_isWhileDnD = false;
        }

        private void RefreshSelectingRect(bool showRectangle = true)
        {
            Bitmap bitmap = (Bitmap)m_imageCache.Clone();

            if (showRectangle) {
                Graphics g = Graphics.FromImage(bitmap);

                int x1 = Math.Min(m_dndBeginPos.X, m_dndEndPos.X);
                int y1 = Math.Min(m_dndBeginPos.Y, m_dndEndPos.Y);
                int x2 = Math.Max(m_dndBeginPos.X, m_dndEndPos.X);
                int y2 = Math.Max(m_dndBeginPos.Y, m_dndEndPos.Y);
                g.DrawRectangle(Pens.Red, x1, y1, x2 - x1, y2 - y1);
            }

            BitmapToImageSource(bitmap);
        }

        private void UpdatePosAndScale()
        {
            int x1 = Math.Min(m_dndBeginPos.X, m_dndEndPos.X);
            int y1 = Math.Min(m_dndBeginPos.Y, m_dndEndPos.Y);
            int x2 = Math.Max(m_dndBeginPos.X, m_dndEndPos.X);
            int y2 = Math.Max(m_dndBeginPos.Y, m_dndEndPos.Y);
            System.Windows.Point bottomLeftPos = CalculatePositionFromCanvasPos(x1, y2);
            System.Windows.Point topRightPos = CalculatePositionFromCanvasPos(x2, y1);
            X = bottomLeftPos.X + (topRightPos.X - bottomLeftPos.X) / 2;
            Y = bottomLeftPos.Y + (topRightPos.Y - bottomLeftPos.Y) / 2;
            H = topRightPos.Y - bottomLeftPos.Y;
            W = topRightPos.X - bottomLeftPos.X;
        }

        #endregion

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
                    m_imageCache = Render(true);
                    BitmapToImageSource(m_imageCache);
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
            try {
                g.FillRectangle(Brushes.White, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

                // Fill each pixel with calculated colour.
                for (int x = 0; x < bitmap.Width; ++x) {
                    const int maxConcurrency = 12;
                    List<Task> tasks = new List<Task>();
                    Color[] colours = new Color[bitmap.Height];

                    // Calculate each pixel asynchronously
                    using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(maxConcurrency)) {
                        for (int y = 0; y < bitmap.Height; ++y) {
                            concurrencySemaphore.Wait();
                            int _x = x;
                            int _y = y;
                            Task task = Task.Factory.StartNew(() =>
                            {
                                System.Windows.Point pos = CalculatePositionFromCanvasPos(_x, _y);
                                colours[_y] = CalculateColour(pos);

                                concurrencySemaphore.Release();
                            });

                            tasks.Add(task);
                        }
                        Task.WaitAll(tasks.ToArray());
                    }

                    // Fill bitmap pixels with colours
                    for (int y = 0; y < bitmap.Height; ++y) {
                        if (colours[y] != Color.Transparent) {
                            bitmap.SetPixel(x, bitmap.Height - y - 1, colours[y]);
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
            catch(Exception ex) {
                Information.InfoString = ex.Message;
                return m_imageCache;
            }
        }

        private void BitmapToImageSource(Bitmap bitmap)
        {
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

        private System.Windows.Point CalculatePositionFromCanvasPos(int x, int y)
        {
            double ratioX = (double)x / m_canvasSize.Width;
            double posX = W * ratioX - W / 2 + X;

            double ratioY = (double)(m_canvasSize.Height - y) / m_canvasSize.Height;
            double posY = H * ratioY - H / 2 + Y;

            // The mantissa of the double type is 52 bits so that
            // the value is not accurate enough when the order of X is 1.0
            // and the order of (W * ratioX - W / 2) is 1.0E-15.
            return new System.Windows.Point(posX, posY);
        }

        private Color CalculateColour(System.Windows.Point pos)
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
