using System;
using System.Globalization;
using System.Windows.Data;
using DTO.Common;

namespace GUI.Converters
{
    /// <summary>
    /// XAML Value Converter chuyên dụng cho hiển thị Lý Trình đường sắt (km).
    /// Đảm bảo định dạng chuẩn "688.00" hoặc "688.00 km" đồng bộ trên toàn bộ DataGrid & Form.
    /// </summary>
    public class KmFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DBNull.Value) 
            {
                if (parameter?.ToString() == "IsSteep") return false;
                if (parameter?.ToString() == "ToaXe") return "~0 toa";
                return "0.00";
            }

            string paramStr = parameter?.ToString() ?? "";

            if (paramStr == "IsSteep")
            {
                if (decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal docVal))
                {
                    return docVal >= 15.0m;
                }
                return false;
            }

            if (paramStr == "ToaXe")
            {
                if (int.TryParse(value.ToString(), out int met) && met > 0)
                {
                    return $"~{met / 20} toa";
                }
                return "~0 toa";
            }

            bool includeUnit = paramStr == "Unit";

            if (value is decimal d)
            {
                return FormatHelper.FormatKm(d, includeUnit);
            }

            if (FormatHelper.TryParseKm(value.ToString(), out decimal km))
            {
                return FormatHelper.FormatKm(km, includeUnit);
            }

            return "0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0m;
            return FormatHelper.TryParseKm(value.ToString(), out decimal km) ? km : 0m;
        }
    }
}
