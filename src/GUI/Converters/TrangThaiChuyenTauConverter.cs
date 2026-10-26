using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace GUI.Converters
{
    public class TrangThaiChuyenTauConverter : IValueConverter
    {
        private static readonly SolidColorBrush GreenText = new((Color)ColorConverter.ConvertFromString("#15803D"));
        private static readonly SolidColorBrush GreenBg = new((Color)ColorConverter.ConvertFromString("#DCFCE7"));
        private static readonly SolidColorBrush GreenBorder = new((Color)ColorConverter.ConvertFromString("#86EFAC"));

        private static readonly SolidColorBrush BlueText = new((Color)ColorConverter.ConvertFromString("#0369A1"));
        private static readonly SolidColorBrush BlueBg = new((Color)ColorConverter.ConvertFromString("#E0F2FE"));
        private static readonly SolidColorBrush BlueBorder = new((Color)ColorConverter.ConvertFromString("#BAE6FD"));

        private static readonly SolidColorBrush AmberText = new((Color)ColorConverter.ConvertFromString("#B45309"));
        private static readonly SolidColorBrush AmberBg = new((Color)ColorConverter.ConvertFromString("#FEF3C7"));
        private static readonly SolidColorBrush AmberBorder = new((Color)ColorConverter.ConvertFromString("#FDE68A"));

        private static readonly SolidColorBrush RedText = new((Color)ColorConverter.ConvertFromString("#B91C1C"));
        private static readonly SolidColorBrush RedBg = new((Color)ColorConverter.ConvertFromString("#FEE2E2"));
        private static readonly SolidColorBrush RedBorder = new((Color)ColorConverter.ConvertFromString("#FECACA"));

        private static readonly SolidColorBrush GrayText = new((Color)ColorConverter.ConvertFromString("#475569"));
        private static readonly SolidColorBrush GrayBg = new((Color)ColorConverter.ConvertFromString("#F1F5F9"));
        private static readonly SolidColorBrush GrayBorder = new((Color)ColorConverter.ConvertFromString("#CBD5E1"));

        private static readonly SolidColorBrush CyanText = new((Color)ColorConverter.ConvertFromString("#0F766E"));
        private static readonly SolidColorBrush CyanBg = new((Color)ColorConverter.ConvertFromString("#CCFBF1"));
        private static readonly SolidColorBrush CyanBorder = new((Color)ColorConverter.ConvertFromString("#5EEAD4"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = value?.ToString() ?? "";
            string param = parameter?.ToString() ?? "Text";

            if (param == "Color")
            {
                return val switch
                {
                    "HOAN_THANH" => GreenText,
                    "DANG_CHAY" => BlueText,
                    "SAN_SANG" => CyanText,
                    "TRE_GIO" => AmberText,
                    "DA_HUY" => RedText,
                    "DA_LEN_LICH" => GrayText,
                    _ => GrayText
                };
            }

            if (param == "BgColor")
            {
                return val switch
                {
                    "HOAN_THANH" => GreenBg,
                    "DANG_CHAY" => BlueBg,
                    "SAN_SANG" => CyanBg,
                    "TRE_GIO" => AmberBg,
                    "DA_HUY" => RedBg,
                    "DA_LEN_LICH" => GrayBg,
                    _ => GrayBg
                };
            }

            if (param == "BorderColor")
            {
                return val switch
                {
                    "HOAN_THANH" => GreenBorder,
                    "DANG_CHAY" => BlueBorder,
                    "SAN_SANG" => CyanBorder,
                    "TRE_GIO" => AmberBorder,
                    "DA_HUY" => RedBorder,
                    "DA_LEN_LICH" => GrayBorder,
                    _ => GrayBorder
                };
            }

            if (param == "Icon")
            {
                return val switch
                {
                    "HOAN_THANH" => SymbolRegular.CheckmarkCircle24,
                    "DANG_CHAY" => SymbolRegular.PlayCircle24,
                    "SAN_SANG" => SymbolRegular.Clock24,
                    "TRE_GIO" => SymbolRegular.Warning24,
                    "DA_HUY" => SymbolRegular.DismissCircle24,
                    "DA_LEN_LICH" => SymbolRegular.CalendarClock24,
                    _ => SymbolRegular.Calendar24
                };
            }

            return val switch
            {
                "HOAN_THANH" => "Hoàn thành",
                "DANG_CHAY" => "Đang chạy",
                "SAN_SANG" => "Sẵn sàng",
                "TRE_GIO" => "Trễ giờ",
                "DA_HUY" => "Đã hủy",
                "DA_LEN_LICH" => "Đã lên lịch",
                _ => string.IsNullOrWhiteSpace(val) ? "Chưa xác định" : val
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
