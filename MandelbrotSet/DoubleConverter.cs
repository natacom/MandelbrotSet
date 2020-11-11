using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace MandelbrotSet
{
    class DoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double)) return null;

            string output = ((double)value).ToString();

            if (!output.Contains('.')) {
                output += ".0";
            }

            return output;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = value as string;

            if (!string.IsNullOrEmpty(str) && double.TryParse(str, out double newValue)) {
                return newValue;
            }
            else {
                return null;
            }
        }
    }
}
