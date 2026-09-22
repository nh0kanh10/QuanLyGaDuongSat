using System;
using System.Globalization;
using System.Windows.Data;
using DTO.Common;

namespace GUI.Converters
{
    /// <summary>
    /// XAML Value Converter chuyên dụng cho tiền tệ (VNĐ) : "50,000" hoặc "50,000 đ".
    /// </summary>
    public class CurrencyFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DBNull.Value) return "0";

            bool includeSymbol = parameter?.ToString() == "Symbol";

            if (value is decimal d)
            {
                return FormatHelper.FormatCurrency(d, includeSymbol);
            }

            if (FormatHelper.TryParseCurrency(value.ToString(), out decimal amount))
            {
                return FormatHelper.FormatCurrency(amount, includeSymbol);
            }

            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0m;
            return FormatHelper.TryParseCurrency(value.ToString(), out decimal amount) ? amount : 0m;
        }
    }
}
