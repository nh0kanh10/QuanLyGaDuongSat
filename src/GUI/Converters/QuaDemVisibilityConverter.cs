using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GUI.Converters
{
    public class QuaDemVisibilityConverter : IMultiValueConverter, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int soNgay = 0;
            if (value is int i) soNgay = i;
            else if (value is short s) soNgay = s;
            else if (value is long l) soNgay = (int)l;
            else if (value != null && int.TryParse(value.ToString(), out int parsed)) soNgay = parsed;

            string param = parameter?.ToString() ?? "";
            if (param == "Text")
            {
                return soNgay > 0 ? $"+{soNgay}N" : string.Empty;
            }

            return soNgay > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Visibility.Collapsed;

            if (values[0] is DateTime dtDi && values[1] is DateTime dtDen)
            {
                // Nếu giờ đến nhỏ hơn giờ đi trong cùng ngày (hoặc ngày đến sau ngày đi)
                if (dtDen.Date > dtDi.Date || (dtDen.Date == dtDi.Date && dtDen.TimeOfDay < dtDi.TimeOfDay))
                {
                    return Visibility.Visible;
                }
            }
            else if (values[0] is TimeSpan tsDi && values[1] is TimeSpan tsDen)
            {
                if (tsDen < tsDi)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
