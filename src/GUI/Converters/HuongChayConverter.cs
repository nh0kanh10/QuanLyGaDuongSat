using System;
using System.Globalization;
using System.Windows.Data;

namespace GUI.Converters
{
    public class HuongChayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = value?.ToString() ?? "";
            return val switch
            {
                "BAC_NAM" => "Bắc ➔ Nam",
                "NAM_BAC" => "Nam ➔ Bắc",
                _ => val
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
