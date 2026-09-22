using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GUI.Converters
{
    public class BooleanToSymbolConverter : IValueConverter
    {
        private static readonly SolidColorBrush ActiveGreen = new((Color)ColorConverter.ConvertFromString("#16A34A"));
        private static readonly SolidColorBrush InactiveGray = new((Color)ColorConverter.ConvertFromString("#94A3B8"));
        private static readonly SolidColorBrush ActiveText = new((Color)ColorConverter.ConvertFromString("#14532D"));
        private static readonly SolidColorBrush InactiveText = new((Color)ColorConverter.ConvertFromString("#475569"));
        private static readonly SolidColorBrush ActiveBg = new((Color)ColorConverter.ConvertFromString("#DCFCE7"));
        private static readonly SolidColorBrush InactiveBg = new((Color)ColorConverter.ConvertFromString("#F1F5F9"));
        private static readonly SolidColorBrush ActiveBorder = new((Color)ColorConverter.ConvertFromString("#15803D"));
        private static readonly SolidColorBrush InactiveBorder = new((Color)ColorConverter.ConvertFromString("#64748B"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DBNull.Value)
            {
                string pNull = parameter?.ToString() ?? "";
                if (pNull == "VisibilityTrue") return System.Windows.Visibility.Collapsed;
                if (pNull == "VisibilityFalse") return System.Windows.Visibility.Visible;
                return "—";
            }

            try
            {
                bool b = false;
                if (value is bool boolVal)
            {
                b = boolVal;
            }
            else if (int.TryParse(value.ToString(), out int intVal))
            {
                b = intVal > 0;
            }
            else
            {
                bool.TryParse(value.ToString(), out b);
            }
                string param = parameter?.ToString() ?? "";
                if (param == "Status")
                {
                    return b ? "Đang khai thác" : "Tạm ngừng";
                }
                if (param == "StatusDotColor")
                {
                    return b ? ActiveGreen : InactiveGray;
                }
                if (param == "StatusBorderColor")
                {
                    return b ? ActiveBorder : InactiveBorder;
                }
                if (param == "StatusTextColor")
                {
                    return b ? ActiveText : InactiveText;
                }
                if (param == "StatusBgColor")
                {
                    return b ? ActiveBg : InactiveBg;
                }
                if (param == "IsTrue")
                {
                    return b;
                }
                if (param == "VisibilityTrue")
                {
                    return b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                }
                if (param == "VisibilityFalse")
                {
                    return b ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                }
                if (param == "CoKhong")
                {
                    return b ? "Có" : "—";
                }
                return b ? "✓" : "—";
            }
            catch
            {
                return "—";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
