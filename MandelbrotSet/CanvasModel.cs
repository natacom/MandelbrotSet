using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MandelbrotSet
{
    class CanvasModel
    {
        Bitmap m_imageCache;
        RectangleD m_rectangle = RectangleD.Empty;

        public System.Windows.Size Size { get; set; } = new System.Windows.Size(1, 1);
        public System.Windows.Point CenterPoint { get; set; } = new System.Windows.Point(-0.5, 0);
        public System.Windows.Size XYSize { get; set; } = new System.Windows.Size(3, 2.11);
        public bool ShowAxes { get; set; } = false;
        public void SetSelectionRectangle(System.Windows.Point p1, System.Windows.Point p2)
        {
            m_rectangle = new RectangleD(p1, p2);
        }
        public int N { get; set; } = 10000;
        public double Threshold { get; set; } = 2;

        Progress Progress;
        Information Information;

        public CanvasModel(Progress progress, Information information)
        {
            Progress = progress;
            Information = information;
        }

        ~CanvasModel()
        {
            m_imageCache?.Dispose();
        }

        public Bitmap DrawSelectionRectangle(bool showRectangle)
        {
            var bitmap = (Bitmap)m_imageCache.Clone();
            if (showRectangle) {
                var g = Graphics.FromImage(bitmap);
                g.DrawRectangle(Pens.Red, m_rectangle.IntRectangle);
            }
            return bitmap;
        }

        public void UpdatePosAndScale()
        {
            var bottomLeftPos = CalculatePositionFromCanvasPos(m_rectangle.Left, m_rectangle.Bottom);
            var topRightPos = CalculatePositionFromCanvasPos(m_rectangle.Right, m_rectangle.Top);
            CenterPoint = new System.Windows.Point(
                bottomLeftPos.X + (topRightPos.X - bottomLeftPos.X) / 2,
                bottomLeftPos.Y + (topRightPos.Y - bottomLeftPos.Y) / 2);
            XYSize = new System.Windows.Size(
                topRightPos.X - bottomLeftPos.X,
                topRightPos.Y - bottomLeftPos.Y);
        }

        public void UpdatePositionFromCanvasPoint(double x, double y)
        {
            CenterPoint = CalculatePositionFromCanvasPos(x, y);
        }

        public Bitmap Render(bool isForRendering, int coreNum, CancellationToken cancelingToken)
        {
            var bitmap = new Bitmap((int)Size.Width, (int)Size.Height);
            var g = Graphics.FromImage(bitmap);
            try {
                g.FillRectangle(Brushes.White, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

                // Fill each pixel with calculated colour.
                for (int x = 0; x < bitmap.Width; ++x) {
                    var tasks = new List<Task>();
                    var colours = new Color[bitmap.Height];

                    // Calculate each pixel asynchronously
                    using (var concurrencySemaphore = new SemaphoreSlim(coreNum)) {
                        System.Windows.Point pos;
                        for (int y = 0; y < bitmap.Height; ++y) {
                            concurrencySemaphore.Wait();
                            int _x = x;
                            int _y = y;
                            var task = Task.Factory.StartNew(() =>
                            {
                                pos = CalculatePositionFromCanvasPos(_x, _y);
                                colours[_y] = CalculateColour(pos);

                                concurrencySemaphore.Release();
                            });

                            tasks.Add(task);
                        }
                        Task.WaitAll(tasks.ToArray());
                        tasks.ForEach(t => t?.Dispose());
                    }

                    // Fill bitmap pixels with colours
                    for (int y = 0; y < bitmap.Height; ++y) {
                        if (colours[y] != Color.Transparent) {
                            bitmap.SetPixel(x, bitmap.Height - y - 1, colours[y]);
                        }
                    }

                    // update Progress
                    Progress?.UpdateProgress(100 * x / bitmap.Width, isForRendering);

                    // for cancellation
                    cancelingToken.ThrowIfCancellationRequested();
                }
                Progress?.UpdateProgress(-1, isForRendering);

                if (ShowAxes) {
                    // Draw axes of the coordinate
                    var org = CalculateOriginOnCanvas();
                    g.DrawLine(Pens.Gray, new Point(org.X, 0), new Point(org.X, bitmap.Height));
                    g.DrawLine(Pens.Gray, new Point(0, org.Y), new Point(bitmap.Width, org.Y));
                }

                g.Flush();

                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                m_imageCache = bitmap;
                return bitmap;
            }
            catch (Exception ex) {
                Information?.SetString(ex.Message);
                g?.Dispose();
                bitmap?.Dispose();
                return m_imageCache;
            }
        }

        private System.Windows.Point CalculatePositionFromCanvasPos(double x, double y)
        {
            double ratioX = (double)x / Size.Width;
            double posX = XYSize.Width * ratioX - XYSize.Width / 2 + CenterPoint.X;

            double ratioY = (double)(Size.Height - y) / Size.Height;
            double posY = XYSize.Height * ratioY - XYSize.Height / 2 + CenterPoint.Y;

            // The mantissa of the double type is 52 bits so that
            // the value is not accurate enough when the order of X is 1.0
            // and the order of (W * ratioX - W / 2) is 1.0E-15.
            return new System.Windows.Point(posX, posY);
        }

        private Point CalculateOriginOnCanvas()
        {
            double posX = XYSize.Width / 2;
            double orgX = posX - CenterPoint.X;
            double ratioX = orgX / XYSize.Width;

            double posY = XYSize.Height / 2;
            double orgY = posY - CenterPoint.Y;
            double ratioY = orgY / XYSize.Height;

            return new Point((int)(Size.Width * ratioX), (int)(Size.Height * ratioY));
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
    }
}
