using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GUI.Converters
{
    /// <summary>
    /// Hiển thị TrangThai vận đơn (DA_NHAN, DA_XEP, DANG_VAN_CHUYEN, DA_DEN, DA_GIAO) 
    /// thành tiếng Việt và trả về màu sắc tương ứng cho badge/text.
    /// parameter: "Text" | "Color" | "BgColor"
    /// </summary>
    public class TrangThaiVanDonConverter : IValueConverter
    {
        private static readonly SolidColorBrush NavyText = new((Color)ColorConverter.ConvertFromString("#003B73"));
        private static readonly SolidColorBrush BlueText = new((Color)ColorConverter.ConvertFromString("#1D4ED8"));
        private static readonly SolidColorBrush OrangeText = new((Color)ColorConverter.ConvertFromString("#C2410C"));
        private static readonly SolidColorBrush TealText = new((Color)ColorConverter.ConvertFromString("#0F766E"));
        private static readonly SolidColorBrush GreenText = new((Color)ColorConverter.ConvertFromString("#15803D"));
        private static readonly SolidColorBrush GrayText = new((Color)ColorConverter.ConvertFromString("#475569"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = value?.ToString() ?? "";
            string param = parameter?.ToString() ?? "Text";

            if (param == "Color")
            {
                return val switch
                {
                    "DA_NHAN" => NavyText,
                    "DA_XEP" => BlueText,
                    "DANG_VAN_CHUYEN" => OrangeText,
                    "DA_DEN" => TealText,
                    "DA_GIAO" => GreenText,
                    _ => GrayText
                };
            }

            if (param == "BgColor")
            {
                return val switch
                {
                    "DA_NHAN" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                    "DA_XEP" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")),
                    "DANG_VAN_CHUYEN" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7ED")),
                    "DA_DEN" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7")),
                    "DA_GIAO" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
                };
            }

            if (param == "BorderColor")
            {
                return val switch
                {
                    "DA_NHAN" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                    "DA_XEP" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BFDBFE")),
                    "DANG_VAN_CHUYEN" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FED7AA")),
                    "DA_DEN" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A")),
                    "DA_GIAO" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"))
                };
            }

            // Default: Text
            return val switch
            {
                "DA_NHAN" => "Đã tiếp nhận",
                "DA_XEP" => "Đã xếp toa",
                "DANG_VAN_CHUYEN" => "Đang chạy",
                "DA_DEN" => "Đã đến ga",
                "DA_GIAO" => "Đã giao khách",
                _ => val
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
