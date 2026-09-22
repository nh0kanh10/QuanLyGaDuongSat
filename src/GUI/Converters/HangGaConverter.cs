using System;
using System.Globalization;
using System.Windows.Data;

namespace GUI.Converters
{
    public class HangGaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            string val = value.ToString() ?? "";

            return val switch
            {
                "HANG_1" => "Hạng I (Đầu mối trung tâm)",
                "HANG_2" => "Hạng II (Trung chuyển tỉnh)",
                "HANG_3" => "Hạng III (Tránh kỹ thuật)",
                _ => val
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
