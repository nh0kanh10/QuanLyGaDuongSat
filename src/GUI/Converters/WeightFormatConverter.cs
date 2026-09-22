using System;
using System.Globalization;
using System.Windows.Data;
using DTO.Common;

namespace GUI.Converters
{
    // Converter chuyên dụng cho tải trọng hàng hóa (tấn): "0.15" hoặc "0.15 tấn".
    public class WeightFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DBNull.Value) return "0.00";

            bool includeUnit = parameter?.ToString() == "Unit";

            if (value is decimal d)
            {
                return FormatHelper.FormatWeight(d, includeUnit);
            }

            if (FormatHelper.TryParseWeight(value.ToString(), out decimal weight))
            {
                return FormatHelper.FormatWeight(weight, includeUnit);
            }

            return "0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0m;
            return FormatHelper.TryParseWeight(value.ToString(), out decimal weight) ? weight : 0m;
        }
    }
}
