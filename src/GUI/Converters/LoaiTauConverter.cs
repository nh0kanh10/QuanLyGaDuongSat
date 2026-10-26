using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GUI.Converters
{
    public class LoaiTauConverter : IValueConverter
    {
        private static readonly SolidColorBrush TauNhanhColor = new((Color)ColorConverter.ConvertFromString("#003B73"));
        private static readonly SolidColorBrush TauChoColor = new((Color)ColorConverter.ConvertFromString("#15803D"));
        private static readonly SolidColorBrush TauHangColor = new((Color)ColorConverter.ConvertFromString("#B45309"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = value?.ToString() ?? "";
            string param = parameter?.ToString() ?? "Text";

            if (param == "Color")
            {
                return val switch
                {
                    "TAU_NHANH" => TauNhanhColor,
                    "TAU_CHO" => TauChoColor,
                    "TAU_HANG" => TauHangColor,
                    _ => TauNhanhColor
                };
            }

            return val switch
            {
                "TAU_NHANH" => "Khách SE",
                "TAU_CHO" => "Khách TN",
                "TAU_HANG" => "Tàu Hàng",
                "TAU_CHUYEN_DUNG" => "Chuyên dụng",
                _ => string.IsNullOrWhiteSpace(val) ? "--" : val
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
