using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DTO.Common
{
    /// <summary>
    /// Chuẩn hóa định dạng và xử lý số liệu toàn hệ thống Điều Hành Vận Tải Đường Sắt.
    /// Đảm bảo tính nhất quán 100% giữa View (WPF), Business (BUS) và Database (SQL Server).
    /// </summary>
    public static class FormatHelper
    {
        /// <summary>
        /// Culture chuẩn duy nhất cho các thông số kỹ thuật (lý trình, tọa độ, tải trọng, tiền tệ).
        /// Sử dụng InvariantCulture (dấu chấm thập phân '.' và dấu phẩy ngàn ',').
        /// </summary>
        public static readonly CultureInfo TechnicalCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Làm sạch chuỗi số thô: loại bỏ khoảng trắng đặc biệt (NBSP), đơn vị đo lường phổ biến.
        /// </summary>
        private static string SanitizeNumberString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // Loại bỏ khoảng trắng thông thường và Non-breaking space (\u00A0)
            string clean = input.Trim().Replace("\u00A0", "").Replace(" ", "");

            // Loại bỏ các hậu tố đơn vị đo phổ biến nếu người dùng dán vào ô nhập
            clean = Regex.Replace(clean, @"(?i)(km|tấn|tan|kg|đồng|dong|vnd|vnđ|đ)$", "");

            return clean.Trim();
        }

        /// <summary>
        /// Parse số thực an toàn đa định dạng (Defensive Tolerant Multi-Format Parsing).
        /// Chống 100% lỗi mâu thuẫn Culture (vd: người dùng gõ "688,00" không bao giờ bị parse nhầm thành 68800).
        /// Tự động thích ứng cả chuẩn quốc tế (1,095.50), chuẩn Việt Nam (1.095,50), hay gõ nhanh (688.00 / 688,00 / 688.00 km).
        /// </summary>
        public static bool TryParseDecimal(string? input, out decimal result)
        {
            result = 0;
            string clean = SanitizeNumberString(input);
            if (string.IsNullOrEmpty(clean)) return false;

            int lastDot = clean.LastIndexOf('.');
            int lastComma = clean.LastIndexOf(',');

            if (lastDot >= 0 && lastComma >= 0)
            {
                if (lastDot > lastComma)
                {
                    // Định dạng US/ERP: "1,095.50" -> bỏ dấu phẩy phân cách ngàn
                    clean = clean.Replace(",", "");
                }
                else
                {
                    // Định dạng VN/EU: "1.095,50" -> bỏ dấu chấm ngàn, đổi phẩy thành chấm thập phân
                    clean = clean.Replace(".", "").Replace(',', '.');
                }
            }
            else if (lastComma >= 0)
            {
                // Chỉ có dấu phẩy:
                // Nếu có nhiều hơn 1 dấu phẩy (vd: "1,000,000") -> là dấu phân cách ngàn -> xóa bỏ
                int commaCount = clean.Split(',').Length - 1;
                if (commaCount > 1)
                {
                    clean = clean.Replace(",", "");
                }
                else
                {
                    // Có đúng 1 dấu phẩy (vd: "688,00" hoặc "1095,50") -> là dấu thập phân -> đổi thành chấm
                    clean = clean.Replace(',', '.');
                }
            }
            else if (lastDot >= 0)
            {
                // Chỉ có dấu chấm:
                // Nếu có nhiều hơn 1 dấu chấm (vd: "1.000.000") -> là dấu phân cách ngàn -> xóa bỏ
                int dotCount = clean.Split('.').Length - 1;
                if (dotCount > 1)
                {
                    clean = clean.Replace(".", "");
                }
                // Nếu có đúng 1 dấu chấm (vd: "688.00") -> giữ nguyên chuẩn Invariant
            }

            return decimal.TryParse(clean, NumberStyles.Any, TechnicalCulture, out result);
        }

        /// <summary>
        /// Parse Lý trình đường sắt (km) an toàn: yêu cầu số thực không âm (>= 0).
        /// </summary>
        public static bool TryParseKm(string? input, out decimal km)
        {
            if (TryParseDecimal(input, out km) && km >= 0)
            {
                return true;
            }
            km = 0;
            return false;
        }

        /// <summary>
        /// Parse Tải trọng / Trọng lượng (tấn) an toàn: yêu cầu số thực dương (> 0).
        /// </summary>
        public static bool TryParseWeight(string? input, out decimal weight)
        {
            if (TryParseDecimal(input, out weight) && weight > 0)
            {
                return true;
            }
            weight = 0;
            return false;
        }

        /// <summary>
        /// Parse Cước phí / Tiền tệ (VNĐ) an toàn: yêu cầu không âm (>= 0).
        /// Tự động xử lý cả phân cách ngàn dấu phẩy ("650,000") và dấu chấm ("1.250.000").
        /// </summary>
        public static bool TryParseCurrency(string? input, out decimal amount)
        {
            amount = 0;
            string clean = SanitizeNumberString(input);
            if (string.IsNullOrEmpty(clean)) return false;

            // Nếu có cả chấm và phẩy: vd "1,250,000.00" hoặc "1.250.000,00"
            int lastDot = clean.LastIndexOf('.');
            int lastComma = clean.LastIndexOf(',');

            if (lastDot >= 0 && lastComma >= 0)
            {
                if (lastDot > lastComma)
                {
                    // "1,250,000.00" -> bỏ dấu phẩy ngàn
                    clean = clean.Replace(",", "");
                }
                else
                {
                    // "1.250.000,00" -> bỏ dấu chấm ngàn, đổi phẩy thành chấm
                    clean = clean.Replace(".", "").Replace(',', '.');
                }
                if (decimal.TryParse(clean, NumberStyles.Any, TechnicalCulture, out amount) && amount >= 0)
                {
                    amount = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
                    return true;
                }
            }

            // Với tiền VNĐ thông thường: loại bỏ toàn bộ dấu chấm và phẩy ngàn
            string digitsOnly = clean.Replace(".", "").Replace(",", "");
            if (decimal.TryParse(digitsOnly, NumberStyles.Any, TechnicalCulture, out amount) && amount >= 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parse số thực an toàn, trả về giá trị mặc định nếu không hợp lệ.
        /// </summary>
        public static decimal ParseDecimalOrDefault(string? input, decimal defaultValue = 0)
        {
            return TryParseDecimal(input, out decimal val) ? val : defaultValue;
        }

        /// <summary>
        /// Format Lý trình (km) chuẩn kỹ thuật hiển thị đồng bộ toàn hệ thống: "688.00" hoặc "688.00 km".
        /// </summary>
        public static string FormatKm(decimal km, bool includeUnit = false)
        {
            string formatted = km.ToString("F2", TechnicalCulture);
            return includeUnit ? $"{formatted} km" : formatted;
        }

        /// <summary>
        /// Format Tiền tệ / Cước phí (VNĐ) chuẩn kế toán: "50,000" hoặc "50,000 đ".
        /// </summary>
        public static string FormatCurrency(decimal amount, bool includeSymbol = false)
        {
            string formatted = amount.ToString("N0", TechnicalCulture);
            return includeSymbol ? $"{formatted} đ" : formatted;
        }

        /// <summary>
        /// Format Tải trọng (tấn) chuẩn kỹ thuật: "0.15" hoặc "0.15 tấn".
        /// </summary>
        public static string FormatWeight(decimal tons, bool includeUnit = false)
        {
            string formatted = tons.ToString("N2", TechnicalCulture);
            return includeUnit ? $"{formatted} tấn" : formatted;
        }

        /// <summary>
        /// Format số thực với số chữ số thập phân tùy chọn theo chuẩn InvariantCulture.
        /// </summary>
        public static string FormatDecimal(decimal value, int decimalPlaces = 2)
        {
            return value.ToString($"F{decimalPlaces}", TechnicalCulture);
        }

        /// <summary>
        /// Chuyển đổi timestamp UTC từ CSDL sang giờ địa phương máy trạm (UTC+7 cho Việt Nam).
        /// Chống lệch 7 múi giờ (ví dụ: cập nhật lúc 04:08 AM không bị hiển thị thành 21:08 đêm hôm trước).
        /// </summary>
        public static DateTime ToLocalDateTime(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc)
            {
                return dt.ToLocalTime();
            }
            // SQL Server DATETIME2 trả về Kind là Unspecified
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
        }

        /// <summary>
        /// Format Ngày Giờ tác nghiệp chuẩn đường sắt Việt Nam: "dd/MM/yyyy HH:mm".
        /// Mặc định tự động chuyển đổi UTC từ Database thành giờ địa phương máy trạm (UTC+7).
        /// </summary>
        public static string FormatDateTime(DateTime dt, bool fromUtc = true)
        {
            DateTime display = fromUtc ? ToLocalDateTime(dt) : dt;
            return display.ToString("dd/MM/yyyy HH:mm");
        }

        /// <summary>
        /// Format Ngày Giờ an toàn cho giá trị nullable: trả về giá trị mặc định nếu null.
        /// </summary>
        public static string FormatDateTime(DateTime? dt, bool fromUtc = true, string defaultText = "Chưa ghi nhận")
        {
            return dt.HasValue ? FormatDateTime(dt.Value, fromUtc) : defaultText;
        }

        /// <summary>
        /// Format Ngày: "dd/MM/yyyy".
        /// </summary>
        public static string FormatDate(DateTime dt, bool fromUtc = true)
        {
            DateTime display = fromUtc ? ToLocalDateTime(dt) : dt;
            return display.ToString("dd/MM/yyyy");
        }

        /// <summary>
        /// Format Giờ phút: "HH:mm".
        /// </summary>
        public static string FormatTime(DateTime dt, bool fromUtc = true)
        {
            DateTime display = fromUtc ? ToLocalDateTime(dt) : dt;
            return display.ToString("HH:mm");
        }
    }
}
