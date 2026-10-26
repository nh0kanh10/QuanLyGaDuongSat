using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GUI.Converters
{
    public class TacNghiepGaConverter : IValueConverter
    {
        private static readonly SolidColorBrush NeutralFg = new(Color.FromRgb(100, 116, 139)); // #64748B

        private static readonly SolidColorBrush AmberBg = new(Color.FromRgb(254, 243, 199)); // #FEF3C7
        private static readonly SolidColorBrush AmberBorder = new(Color.FromRgb(252, 211, 77)); // #FCD34D
        private static readonly SolidColorBrush AmberFg = new(Color.FromRgb(180, 83, 9)); // #B45309

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isTranh = false;
            if (value is bool b) isTranh = b;
            else if (value is int i) isTranh = (i == 1);
            else if (value != null && bool.TryParse(value.ToString(), out bool parsedBool)) isTranh = parsedBool;
            else if (value != null && int.TryParse(value.ToString(), out int parsedInt)) isTranh = (parsedInt == 1);

            string param = parameter?.ToString() ?? "Text";

            return param switch
            {
                "Text" => isTranh ? "Tránh tàu" : "Đón/Trả",
                "Bg" => isTranh ? AmberBg : Brushes.Transparent,
                "Border" => isTranh ? AmberBorder : Brushes.Transparent,
                "Fg" => isTranh ? AmberFg : NeutralFg,
                _ => isTranh ? "Tránh tàu" : "Đón/Trả"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw KalNotSupported();
            static NotSupportedException KalNotSupported() => new();
        }
    }
}
