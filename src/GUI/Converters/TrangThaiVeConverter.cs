using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace GUI.Converters
{
    public class TrangThaiVeConverter : IValueConverter
    {
        private static readonly SolidColorBrush GreenText = new((Color)ColorConverter.ConvertFromString("#15803D"));
        private static readonly SolidColorBrush GreenBg = new((Color)ColorConverter.ConvertFromString("#DCFCE7"));
        private static readonly SolidColorBrush GreenBorder = new((Color)ColorConverter.ConvertFromString("#86EFAC"));

        private static readonly SolidColorBrush AmberText = new((Color)ColorConverter.ConvertFromString("#B45309"));
        private static readonly SolidColorBrush AmberBg = new((Color)ColorConverter.ConvertFromString("#FEF3C7"));
        private static readonly SolidColorBrush AmberBorder = new((Color)ColorConverter.ConvertFromString("#FDE68A"));

        private static readonly SolidColorBrush RedText = new((Color)ColorConverter.ConvertFromString("#B91C1C"));
        private static readonly SolidColorBrush RedBg = new((Color)ColorConverter.ConvertFromString("#FEE2E2"));
        private static readonly SolidColorBrush RedBorder = new((Color)ColorConverter.ConvertFromString("#FECACA"));

        private static readonly SolidColorBrush GrayText = new((Color)ColorConverter.ConvertFromString("#475569"));
        private static readonly SolidColorBrush GrayBg = new((Color)ColorConverter.ConvertFromString("#F1F5F9"));
        private static readonly SolidColorBrush GrayBorder = new((Color)ColorConverter.ConvertFromString("#CBD5E1"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = value?.ToString() ?? "";
            string param = parameter?.ToString() ?? "Text";

            if (param == "Icon")
            {
                return val switch
                {
                    "DA_DAT" => SymbolRegular.CheckmarkCircle24,
                    "DA_HOAN_VE" => SymbolRegular.ArrowUndo24,
                    "DA_HUY" => SymbolRegular.DismissCircle24,
                    _ => SymbolRegular.TicketDiagonal24
                };
            }

            if (param == "Color")
            {
                return val switch
                {
                    "DA_DAT" => GreenText,
                    "DA_HOAN_VE" => AmberText,
                    "DA_HUY" => RedText,
                    _ => GrayText
                };
            }

            if (param == "BgColor")
            {
                return val switch
                {
                    "DA_DAT" => GreenBg,
                    "DA_HOAN_VE" => AmberBg,
                    "DA_HUY" => RedBg,
                    _ => GrayBg
                };
            }

            if (param == "BorderColor")
            {
                return val switch
                {
                    "DA_DAT" => GreenBorder,
                    "DA_HOAN_VE" => AmberBorder,
                    "DA_HUY" => RedBorder,
                    _ => GrayBorder
                };
            }

            return val switch
            {
                "DA_DAT" => "Đã Đặt",
                "DA_HOAN_VE" => "Đã Hoàn",
                "DA_HUY" => "Đã Hủy",
                _ => val
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
