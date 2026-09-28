using System;
using System.Data;
using System.Globalization;

namespace GUI.Helpers
{
    /// <summary>
    /// Tiện ích chuyển đổi và định dạng an toàn cho Ngày & Giờ (DateTime, TimeSpan, Object từ CSDL)
    /// Tránh hoàn toàn ngoại lệ InvalidCastException khi đọc dữ liệu hỗn hợp từ SQL Server.
    /// </summary>
    public static class DateTimeFormatHelper
    {
        // Định dạng giờ từ DataRow
        public static string DinhDangGio(DataRow? row, string primaryCol, string? fallbackCol = null, string defaultVal = "--:--")
        {
            if (row == null) return defaultVal;

            object? val = null;
            if (row.Table.Columns.Contains(primaryCol) && row[primaryCol] != DBNull.Value)
            {
                val = row[primaryCol];
            }
            else if (!string.IsNullOrEmpty(fallbackCol) && row.Table.Columns.Contains(fallbackCol) && row[fallbackCol] != DBNull.Value)
            {
                val = row[fallbackCol];
            }

            return DinhDangGio(val, defaultVal);
        }

        // Định dạng giờ an toàn từ object bất kỳ (DateTime, TimeSpan, string, DBNull)
        public static string DinhDangGio(object? value, string defaultVal = "--:--")
        {
            if (value == null || value == DBNull.Value) return defaultVal;

            if (value is DateTime dt)
                return dt.ToString("HH:mm");

            if (value is TimeSpan ts)
                return ts.ToString(@"hh\:mm");

            string str = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(str)) return defaultVal;

            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDt) ||
                DateTime.TryParse(str, out parsedDt))
            {
                return parsedDt.ToString("HH:mm");
            }

            if (TimeSpan.TryParse(str, CultureInfo.InvariantCulture, out TimeSpan parsedTs) ||
                TimeSpan.TryParse(str, out parsedTs))
            {
                return parsedTs.ToString(@"hh\:mm");
            }

            return defaultVal;
        }

        // Chuyển đổi an toàn sang TimeSpan từ object (hỗ trợ DateTime, TimeSpan, string)
        public static TimeSpan ChuyenSangTimeSpan(object? value, TimeSpan? defaultValue = null)
        {
            if (value == null || value == DBNull.Value) 
                return defaultValue ?? TimeSpan.Zero;

            if (value is TimeSpan ts) 
                return ts;

            if (value is DateTime dt) 
                return dt.TimeOfDay;

            string str = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(str)) 
                return defaultValue ?? TimeSpan.Zero;

            if (TimeSpan.TryParse(str, out TimeSpan parsedTs)) 
                return parsedTs;

            if (DateTime.TryParse(str, out DateTime parsedDt)) 
                return parsedDt.TimeOfDay;

            return defaultValue ?? TimeSpan.Zero;
        }

        // Chuyển đổi an toàn sang DateTime từ object
        public static DateTime ChuyenSangDateTime(object? value, DateTime? defaultValue = null)
        {
            if (value == null || value == DBNull.Value)
                return defaultValue ?? DateTime.MinValue;

            if (value is DateTime dt)
                return dt;

            string str = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrEmpty(str))
                return defaultValue ?? DateTime.MinValue;

            if (DateTime.TryParse(str, out DateTime parsedDt))
                return parsedDt;

            return defaultValue ?? DateTime.MinValue;
        }
    }
}
