using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MandelbrotSet
{
    class RectangleD
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double Left => X;
        public double Right => X + Width;
        public double Top => Y;
        public double Bottom => Y + Height;

        public static RectangleD Empty => new RectangleD(0, 0, 0, 0);
        public Rectangle IntRectangle => new Rectangle((int)X, (int)Y, (int)Width, (int)Height);

        public RectangleD(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public RectangleD(System.Windows.Point p1, System.Windows.Point p2)
        {
            X = Math.Min(p1.X, p2.X);
            Y = Math.Min(p1.Y, p2.Y);
            Width = Math.Abs(p1.X - p2.X);
            Height = Math.Abs(p1.Y - p2.Y);
        }
    }
}
