using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace GUI.Converters
{
    public class ChoTrongBadgeConverter : IValueConverter
    {
        // Xanh lục: Còn nhiều chỗ (> 10)
        private static readonly SolidColorBrush GreenText = new((Color)ColorConverter.ConvertFromString("#15803D"));
        private static readonly SolidColorBrush GreenBg = new((Color)ColorConverter.ConvertFromString("#DCFCE7"));
        private static readonly SolidColorBrush GreenBorder = new((Color)ColorConverter.ConvertFromString("#86EFAC"));

        // Vàng cam: Sắp hết chỗ (1 - 10)
        private static readonly SolidColorBrush AmberText = new((Color)ColorConverter.ConvertFromString("#B45309"));
        private static readonly SolidColorBrush AmberBg = new((Color)ColorConverter.ConvertFromString("#FEF3C7"));
        private static readonly SolidColorBrush AmberBorder = new((Color)ColorConverter.ConvertFromString("#FDE68A"));

        // Đỏ: Hết chỗ (0)
        private static readonly SolidColorBrush RedText = new((Color)ColorConverter.ConvertFromString("#B91C1C"));
        private static readonly SolidColorBrush RedBg = new((Color)ColorConverter.ConvertFromString("#FEE2E2"));
        private static readonly SolidColorBrush RedBorder = new((Color)ColorConverter.ConvertFromString("#FECACA"));

        // Xám: Không xác định
        private static readonly SolidColorBrush GrayText = new((Color)ColorConverter.ConvertFromString("#64748B"));
        private static readonly SolidColorBrush GrayBg = new((Color)ColorConverter.ConvertFromString("#F1F5F9"));
        private static readonly SolidColorBrush GrayBorder = new((Color)ColorConverter.ConvertFromString("#CBD5E1"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int soCho = 0;
            if (value is int iVal)
            {
                soCho = iVal;
            }
            else if (value != null && value != DBNull.Value)
            {
                string raw = value.ToString() ?? "";
                if (int.TryParse(raw, out int directVal))
                {
                    soCho = directVal;
                }
                else
                {
                    var match = System.Text.RegularExpressions.Regex.Match(raw, @"\d+");
                    if (match.Success && int.TryParse(match.Value, out int matchedVal))
                    {
                        soCho = matchedVal;
                    }
                }
            }

            string param = parameter?.ToString() ?? "Text";

            if (param == "Icon")
            {
                if (soCho == 0) return SymbolRegular.DismissCircle24;
                if (soCho <= 10) return SymbolRegular.Warning24;
                return SymbolRegular.CheckmarkCircle24;
            }

            if (param == "TextColor")
            {
                if (soCho == 0) return RedText;
                if (soCho <= 10) return AmberText;
                return GreenText;
            }

            if (param == "BgColor")
            {
                if (soCho == 0) return RedBg;
                if (soCho <= 10) return AmberBg;
                return GreenBg;
            }

            if (param == "BorderColor")
            {
                if (soCho == 0) return RedBorder;
                if (soCho <= 10) return AmberBorder;
                return GreenBorder;
            }

            // Default: Text
            if (soCho == 0) return "Hết chỗ";
            return $"{soCho} chỗ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
