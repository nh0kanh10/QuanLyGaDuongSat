using System;
using System.Globalization;
using System.Windows.Data;

namespace GUI.Converters
{
    /// <summary>
    /// Hiển thị LoaiVanChuyen (XE_MAY, HANG_HOA, CONTAINER) thành tiếng Việt dễ đọc.
    /// </summary>
    public class LoaiVanChuyenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = value?.ToString() ?? "";
            return val switch
            {
                "XE_MAY" => "Xe máy ký gửi",
                "HANG_HOA" => "Hàng hóa rời",
                "CONTAINER" => "Container",
                _ => val
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
