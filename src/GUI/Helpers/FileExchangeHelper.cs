using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using ClosedXML.Excel;
using GUI.Views.Dialogs;
using Microsoft.Win32;

namespace GUI.Helpers
{
    /// <summary>
    /// Định nghĩa mục tiêu cột cần nhận diện thông minh từ tệp tải lên
    /// </summary>
    public class SmartColumnTarget
    {
        public string PropertyKey { get; set; } = "";
        public string[] Aliases { get; set; } = Array.Empty<string>();
        public bool IsRequired { get; set; } = false;

        public SmartColumnTarget(string propertyKey, string[] aliases, bool isRequired = false)
        {
            PropertyKey = propertyKey;
            Aliases = aliases;
            IsRequired = isRequired;
        }
    }

    /// <summary>
    /// Dòng dữ liệu sau khi được ánh xạ theo tên thuộc tính chuẩn, độc lập với thứ tự cột vật lý
    /// </summary>
    public class SmartRowData
    {
        public int SourceLineNumber { get; set; }
        public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string[] RawTokens { get; set; } = Array.Empty<string>();

        public string Get(string key, string fallback = "")
        {
            if (Fields.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                return val.Trim();
            return fallback;
        }
    }

    /// <summary>
    /// Kết quả phân tích bảng thông minh
    /// </summary>
    public class SmartTableResult
    {
        public int HeaderRowIndex { get; set; } = -1;
        public bool HeaderFound => HeaderRowIndex >= 0;
        public Dictionary<string, int> ColumnIndices { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public int MatchedColumnCount => ColumnIndices.Count;
        public List<SmartRowData> DataRows { get; set; } = new();
        public int PreambleRowsCount { get; set; } = 0;
        public int PreambleRowsSkipped => PreambleRowsCount;
        public int DiscardedFooterRowsCount { get; set; } = 0;
        public int FooterRowsSkipped => DiscardedFooterRowsCount;
    }

    /// <summary>
    /// Dịch vụ Tiện ích Xuất và Nhập tệp dữ liệu dùng chung toàn hệ thống.
    /// Hỗ trợ cả Microsoft Excel (.xlsx) định dạng cao cấp (ClosedXML) và CSV (RFC 4180 + UTF-8 BOM).
    /// </summary>
    public static class FileExchangeHelper
    {
        #region CHUYỂN ĐỔI SỐ THÔNG MINH (CẢ ĐỊNH DẠNG VI-VN VÀ INVARIANT / US)

        /// <summary>
        /// Chuyển đổi chuỗi số thành decimal an toàn tuyệt đối.
        /// Tự động xử lý dấu phân cách hàng nghìn kiểu US (1,095.50) hoặc kiểu Việt Nam (1.095,50),
        /// loại bỏ các hậu tố đơn vị như "km", "km/h", "‰", "%", "m".
        /// </summary>
        public static bool ThuChuyenDoiDecimal(string? raw, out decimal result)
        {
            result = 0m;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string clean = raw.Trim()
                              .Replace("km/h", "", StringComparison.OrdinalIgnoreCase)
                              .Replace("km", "", StringComparison.OrdinalIgnoreCase)
                              .Replace("‰", "")
                              .Replace("%", "")
                              .Replace("m", "", StringComparison.OrdinalIgnoreCase)
                              .Replace(" ", "")
                              .Trim();

            if (string.IsNullOrEmpty(clean)) return false;

            // 1. Thử InvariantCulture với AllowThousands (e.g. 1,095.50 hoặc 865.00)
            if (decimal.TryParse(clean, NumberStyles.Number | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
                return true;

            // 2. Thử Culture vi-VN với AllowThousands (e.g. 1.095,50 hoặc 865,00)
            var viCulture = CultureInfo.GetCultureInfo("vi-VN");
            if (decimal.TryParse(clean, NumberStyles.Number | NumberStyles.AllowThousands, viCulture, out result))
                return true;

            // 3. Xử lý trường hợp có cả dấu chấm '.' và dấu phẩy ','
            int lastDot = clean.LastIndexOf('.');
            int lastComma = clean.LastIndexOf(',');

            if (lastDot >= 0 && lastComma >= 0)
            {
                if (lastDot > lastComma)
                {
                    // Ví dụ: 1,095.50 -> dấu phẩy là ngàn, dấu chấm là thập phân
                    string normalized = clean.Replace(",", "");
                    if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                        return true;
                }
                else
                {
                    // Ví dụ: 1.095,50 -> dấu chấm là ngàn, dấu phẩy là thập phân
                    string normalized = clean.Replace(".", "").Replace(",", ".");
                    if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                        return true;
                }
            }

            // 4. Nếu chỉ có dấu phẩy ',' (ví dụ: 1095,50)
            if (clean.Contains(',') && !clean.Contains('.'))
            {
                string normalized = clean.Replace(",", ".");
                if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    return true;
            }

            // 5. Thử ép lại bằng NumberStyles.Any Invariant
            return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// Chuyển đổi chuỗi số thành int an toàn
        /// </summary>
        public static bool ThuChuyenDoiInt(string? raw, out int result)
        {
            result = 0;
            if (ThuChuyenDoiDecimal(raw, out decimal d))
            {
                result = (int)Math.Round(d);
                return true;
            }
            return false;
        }

        #endregion

        #region CSV PARSING & ESCAPING

        /// <summary>
        /// Escape chuỗi theo chuẩn RFC 4180 CSV
        /// </summary>
        public static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        /// <summary>
        /// Phân tích 1 dòng CSV thành mảng các tokens (hỗ trợ trường có dấu ngoặc kép bọc ngoài)
        /// </summary>
        public static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var currentToken = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentToken.Append('"');
                        i++; // Bỏ qua dấu ngoặc kép kép
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentToken.ToString());
                    currentToken.Clear();
                }
                else
                {
                    currentToken.Append(c);
                }
            }

            result.Add(currentToken.ToString());
            return result.ToArray();
        }

        #endregion

        #region ĐỌC TỆP DÙNG CHUNG (HỖ TRỢ .XLSX VÀ .CSV)

        /// <summary>
        /// Đọc toàn bộ các dòng từ tệp Excel (.xlsx) hoặc CSV, trả về danh sách các mảng token chuỗi.
        /// </summary>
        public static List<string[]> DocTepDuLieu(string filePath)
        {
            var rows = new List<string[]>();
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (ext == ".xlsx" || ext == ".xlsm")
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return rows;

                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

                for (int r = 1; r <= lastRow; r++)
                {
                    var rowData = new string[lastCol];
                    bool hasData = false;
                    for (int c = 1; c <= lastCol; c++)
                    {
                        string cellVal = "";
                        try
                        {
                            var cell = worksheet.Cell(r, c);
                            cellVal = cell.GetFormattedString()?.Trim() ?? "";
                        }
                        catch
                        {
                            try { cellVal = worksheet.Cell(r, c).Value.ToString().Trim(); } catch { }
                        }

                        rowData[c - 1] = cellVal;
                        if (!string.IsNullOrEmpty(cellVal)) hasData = true;
                    }
                    if (hasData)
                    {
                        rows.Add(rowData);
                    }
                }
            }
            else
            {
                // Mặc định đọc CSV với UTF-8
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    rows.Add(ParseCsvLine(line));
                }
            }

            return rows;
        }

        /// <summary>
        /// Bộ phân tích bảng thông minh: Tự động định vị dòng tiêu đề Header (bỏ qua mọi dòng Banner/Title),
        /// loại bỏ dòng tổng kết / rác ở cuối bảng, và ánh xạ các cột dữ liệu theo tên chuẩn xác 100%.
        /// </summary>
        public static SmartTableResult PhanTichBangThongMinh(List<string[]> allRows, List<SmartColumnTarget> targets)
        {
            var result = new SmartTableResult();
            if (allRows == null || allRows.Count == 0 || targets == null || targets.Count == 0)
                return result;

            // 1. TÌM DÒNG HEADER BẰNG GIẢI THUẬT MATCHING ĐIỂM SỐ
            int bestHeaderRow = -1;
            int bestScore = 0;
            Dictionary<string, int> bestColumnIndices = new(StringComparer.OrdinalIgnoreCase);

            int maxScanRows = Math.Min(25, allRows.Count);

            for (int r = 0; r < maxScanRows; r++)
            {
                var row = allRows[r];
                if (row == null || row.Length == 0) continue;

                int score = 0;
                var currentIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var usedCols = new HashSet<int>();

                // Bước 1: Quét khớp CHÍNH XÁC (Exact Match - Điểm cao nhất)
                foreach (var target in targets)
                {
                    for (int c = 0; c < row.Length; c++)
                    {
                        if (usedCols.Contains(c)) continue;
                        string cellClean = ChuanHoaSoSanh(row[c]);
                        if (string.IsNullOrEmpty(cellClean)) continue;

                        bool isExact = false;
                        foreach (var alias in target.Aliases)
                        {
                            string aliasClean = ChuanHoaSoSanh(alias);
                            if (cellClean == aliasClean)
                            {
                                isExact = true;
                                break;
                            }
                        }

                        if (isExact)
                        {
                            currentIndices[target.PropertyKey] = c;
                            usedCols.Add(c);
                            score += target.IsRequired ? 5 : 3;
                            break;
                        }
                    }
                }

                // Bước 2: Quét khớp PHỤ (Substring Match) cho các cột chưa được gán
                foreach (var target in targets)
                {
                    if (currentIndices.ContainsKey(target.PropertyKey)) continue;

                    for (int c = 0; c < row.Length; c++)
                    {
                        if (usedCols.Contains(c)) continue;
                        string cellClean = ChuanHoaSoSanh(row[c]);
                        if (string.IsNullOrEmpty(cellClean)) continue;

                        bool isSubMatch = false;
                        foreach (var alias in target.Aliases)
                        {
                            string aliasClean = ChuanHoaSoSanh(alias);
                            if (string.IsNullOrEmpty(aliasClean)) continue;

                            if (cellClean.Contains(aliasClean) || (cellClean.Length >= 4 && aliasClean.Contains(cellClean)))
                            {
                                isSubMatch = true;
                                break;
                            }
                        }

                        if (isSubMatch)
                        {
                            currentIndices[target.PropertyKey] = c;
                            usedCols.Add(c);
                            score += target.IsRequired ? 3 : 1;
                            break;
                        }
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestHeaderRow = r;
                    bestColumnIndices = currentIndices;
                }
            }

            // Nếu tìm được dòng header thỏa mãn tối thiểu 2 thuộc tính (hoặc score >= 3)
            if (bestHeaderRow >= 0 && bestScore >= 2)
            {
                result.HeaderRowIndex = bestHeaderRow;
                result.ColumnIndices = bestColumnIndices;
                result.PreambleRowsCount = bestHeaderRow;
            }
            else
            {
                // Trường hợp file CSV thô không có header: Gán mặc định theo thứ tự 0, 1, 2...
                result.HeaderRowIndex = -1;
                result.PreambleRowsCount = 0;
                for (int i = 0; i < targets.Count; i++)
                {
                    result.ColumnIndices[targets[i].PropertyKey] = i;
                }
            }

            int startRow = result.HeaderRowIndex >= 0 ? result.HeaderRowIndex + 1 : 0;
            int virtualLineNo = 1;

            // 2. TRÍCH XUẤT DỮ LIỆU TỪNG DÒNG & BỎ QUA RÁC / FOOTER
            for (int r = startRow; r < allRows.Count; r++)
            {
                var row = allRows[r];
                if (row == null || row.Length == 0 || row.All(string.IsNullOrWhiteSpace))
                    continue;

                // Kiểm tra xem dòng này có phải Footer / Tổng kết không
                string combinedFirstCells = string.Join(" ", row.Take(Math.Min(4, row.Length))).ToLowerInvariant();
                if (combinedFirstCells.Contains("tổng cộng") || combinedFirstCells.Contains("tong cong") ||
                    combinedFirstCells.Contains("tổng số") || combinedFirstCells.Contains("total") ||
                    combinedFirstCells.Contains("quy mô dữ liệu") || combinedFirstCells.Contains("mục dữ liệu") ||
                    combinedFirstCells.Contains("bản ghi"))
                {
                    result.DiscardedFooterRowsCount++;
                    continue;
                }

                // Tạo SmartRowData
                var smartRow = new SmartRowData
                {
                    SourceLineNumber = virtualLineNo++,
                    RawTokens = row
                };

                foreach (var target in targets)
                {
                    if (result.ColumnIndices.TryGetValue(target.PropertyKey, out int colIdx) && colIdx < row.Length)
                    {
                        smartRow.Fields[target.PropertyKey] = row[colIdx]?.Trim() ?? "";
                    }
                    else
                    {
                        smartRow.Fields[target.PropertyKey] = "";
                    }
                }

                // Bỏ qua dòng nếu TẤT CẢ các cột bắt buộc đều rỗng (ví dụ: dòng trống lọt giữa file)
                bool coDuLieuBatBuoc = targets.Where(t => t.IsRequired).Any(t => !string.IsNullOrWhiteSpace(smartRow.Get(t.PropertyKey)));
                if (!coDuLieuBatBuoc && targets.Any(t => t.IsRequired))
                {
                    continue;
                }

                result.DataRows.Add(smartRow);
            }

            return result;
        }

        private static string BoDauTiengViet(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    if (c == 'đ') sb.Append('d');
                    else if (c == 'Đ') sb.Append('D');
                    else sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string ChuanHoaSoSanh(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string noDau = BoDauTiengViet(input).ToLowerInvariant();
            var sb = new StringBuilder();
            foreach (char c in noDau)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        #endregion

        #region XUẤT EXCEL & CSV CAO CẤP (FORMAT CĂN CHỈNH ĐẸP CHUẨN DOANH NGHIỆP)

        /// <summary>
        /// Xuất dữ liệu từ DataTable ra tệp Excel (.xlsx) hoặc CSV với giao diện căn chỉnh chuyên nghiệp.
        /// Tự động định dạng số (Km, Tốc độ, Độ dốc), kẻ viền, căn lề, tiêu đề báo cáo Bộ/ĐSVN, và Freeze Pane.
        /// </summary>
        public static bool XuatExcel(
            DataTable? table,
            Dictionary<string, string> columnMapping,
            string defaultFileName,
            string tieuDeBaoCao = "BÁO CÁO DỮ LIỆU ĐƯỜNG SẮT",
            string? tenSheet = "DuLieu")
        {
            if (table == null || table.Rows.Count == 0)
            {
                ThongBaoDialog.ThongTin("Không có bản ghi nào để xuất dữ liệu.", "Thông Báo");
                return false;
            }

            return XuatExcel(table.DefaultView, columnMapping, defaultFileName, tieuDeBaoCao, tenSheet);
        }

        /// <summary>
        /// Xuất dữ liệu từ DataView ra tệp Excel (.xlsx) hoặc CSV với giao diện căn chỉnh chuyên nghiệp.
        /// </summary>
        public static bool XuatExcel(
            DataView view,
            Dictionary<string, string> columnMapping,
            string defaultFileName,
            string tieuDeBaoCao = "BÁO CÁO DỮ LIỆU ĐƯỜNG SẮT",
            string? tenSheet = "DuLieu")
        {
            if (view == null || view.Count == 0)
            {
                ThongBaoDialog.ThongTin("Không có bản ghi nào để xuất dữ liệu.", "Thông Báo");
                return false;
            }

            if (!defaultFileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) && 
                !defaultFileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                defaultFileName += ".xlsx";
            }

            var sfd = new SaveFileDialog
            {
                Title = "Xuất Dữ Liệu Ra Tệp Báo Cáo",
                FileName = defaultFileName,
                Filter = "Tệp Microsoft Excel (*.xlsx)|*.xlsx|Tệp CSV (Excel) (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
                DefaultExt = ".xlsx"
            };

            if (sfd.ShowDialog() != true) return false;

            string filePath = sfd.FileName;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                if (ext == ".csv")
                {
                    GhiCsvThuong(view, columnMapping, filePath);
                }
                else
                {
                    GhiExcelXlsx(view, columnMapping, filePath, tieuDeBaoCao, tenSheet ?? "DuLieu");
                }

                bool xemNgay = ThongBaoDialog.XacNhan(
                    $"Đã xuất thành công {view.Count} bản ghi ra tệp:\n{filePath}\n\nBạn có muốn mở tệp này ngay bây giờ không?",
                    "Xuất Tệp Thành Công",
                    nutDongY: "Mở tệp ngay",
                    nutHuy: "Đóng");

                if (xemNgay)
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }

                return true;
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi xuất tệp dữ liệu:\n{ex.Message}", "Lỗi Xuất Tệp");
                return false;
            }
        }

        /// <summary>
        /// Alias tương thích ngược cho các trang đang gọi XuatCsv
        /// </summary>
        public static bool XuatCsv(
            DataTable? table,
            Dictionary<string, string> columnMapping,
            string defaultFileName,
            string dialogTitle = "Xuất Dữ Liệu Ra Tệp Báo Cáo")
        {
            return XuatExcel(table, columnMapping, defaultFileName, dialogTitle);
        }

        public static bool XuatCsv(
            DataView view,
            Dictionary<string, string> columnMapping,
            string defaultFileName,
            string dialogTitle = "Xuất Dữ Liệu Ra Tệp Báo Cáo",
            Func<string, object, string>? customFormatter = null)
        {
            return XuatExcel(view, columnMapping, defaultFileName, dialogTitle);
        }

        private static void GhiExcelXlsx(
            DataView view,
            Dictionary<string, string> columnMapping,
            string filePath,
            string tieuDeBaoCao,
            string tenSheet)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(tenSheet);

            int colCount = columnMapping.Count;

            // 1. HEADER BANNER DOANH NGHIỆP
            // Dòng 1: Tên Tổng Công Ty
            ws.Cell(1, 1).Value = "TỔNG CÔNG TY ĐƯỜNG SẮT VIỆT NAM (VNR)";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 10;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#003B73");

            // Dòng 2: Tiêu đề Báo Cáo Lớn
            ws.Cell(2, 1).Value = tieuDeBaoCao.ToUpperInvariant();
            ws.Range(2, 1, 2, colCount).Merge();
            ws.Cell(2, 1).Style.Font.Bold = true;
            ws.Cell(2, 1).Style.Font.FontSize = 14;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#003B73");
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(2).Height = 28;

            // Dòng 3: Metadata thông tin xuất
            ws.Cell(3, 1).Value = $"Thời điểm lập báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Quy mô dữ liệu: {view.Count} bản ghi | Phân hệ: Hạ tầng kỹ thuật";
            ws.Range(3, 1, 3, colCount).Merge();
            ws.Cell(3, 1).Style.Font.Italic = true;
            ws.Cell(3, 1).Style.Font.FontSize = 9;
            ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
            ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Dòng 4: Cách dòng trống
            ws.Row(4).Height = 8;

            // 2. HEADER BẢNG DỮ LIỆU (Dòng 5)
            int headerRow = 5;
            ws.Row(headerRow).Height = 26;

            var colKeys = columnMapping.Keys.ToList();
            for (int i = 0; i < colCount; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = columnMapping[colKeys[i]];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003B73");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#00274D");
            }

            // 3. DÒNG DỮ LIỆU (Bắt đầu dòng 6)
            int currentRow = 6;
            int stt = 1;

            decimal tongCuLy = 0m;
            bool coCotCuLy = false;
            int colCuLyIdx = -1;

            for (int rIdx = 0; rIdx < view.Count; rIdx++)
            {
                var row = view[rIdx].Row;
                ws.Row(currentRow).Height = 20;
                bool isAlternate = (rIdx % 2 == 1);
                var rowBgColor = isAlternate ? XLColor.FromHtml("#F8FAFC") : XLColor.White;

                for (int cIdx = 0; cIdx < colCount; cIdx++)
                {
                    string colName = colKeys[cIdx];
                    var cell = ws.Cell(currentRow, cIdx + 1);
                    cell.Style.Fill.BackgroundColor = rowBgColor;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    if (colName.Equals("STT", StringComparison.OrdinalIgnoreCase))
                    {
                        cell.Value = stt++;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        continue;
                    }

                    object rawVal = (row.Table.Columns.Contains(colName) && row[colName] != DBNull.Value)
                        ? row[colName]
                        : "";

                    // Căn chỉnh và định dạng số thông minh dựa trên ngữ nghĩa cột
                    string lowerCol = colName.ToLowerInvariant();

                    if (lowerCol.Contains("lytrinh") || lowerCol.Contains("culy"))
                    {
                        if (decimal.TryParse(rawVal.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal numVal))
                        {
                            cell.Value = numVal;
                            cell.Style.NumberFormat.Format = "#,##0.00";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                            if (lowerCol.Contains("culy"))
                            {
                                tongCuLy += numVal;
                                coCotCuLy = true;
                                colCuLyIdx = cIdx + 1;
                            }
                        }
                        else
                        {
                            cell.Value = rawVal.ToString();
                        }
                    }
                    else if (lowerCol.Contains("tocdo") || lowerCol.Contains("vmax"))
                    {
                        if (int.TryParse(rawVal.ToString(), out int speedVal))
                        {
                            cell.Value = speedVal;
                            cell.Style.NumberFormat.Format = "#,##0";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                        else
                        {
                            cell.Value = rawVal.ToString();
                        }
                    }
                    else if (lowerCol.Contains("dopermil") || lowerCol.Contains("doodoc") || lowerCol.Contains("dodoc"))
                    {
                        if (decimal.TryParse(rawVal.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal slopeVal))
                        {
                            cell.Value = slopeVal;
                            cell.Style.NumberFormat.Format = "0.0";
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                        }
                        else
                        {
                            cell.Value = rawVal.ToString();
                        }
                    }
                    else if (lowerCol.Contains("code") || lowerCol.Contains("ma") || lowerCol.Contains("hang") || lowerCol.Contains("soduongngang") || lowerCol.Contains("sodiemden"))
                    {
                        cell.Value = rawVal.ToString();
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else if (rawVal is bool bVal)
                    {
                        cell.Value = bVal ? "Có" : "Không";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    else
                    {
                        cell.Value = rawVal.ToString();
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    }
                }

                currentRow++;
            }

            // 4. DÒNG TỔNG KẾT / FOOTER BẢNG (Accounting Double Border)
            ws.Row(currentRow).Height = 24;
            ws.Cell(currentRow, 1).Value = "TỔNG CỘNG";
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(currentRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            for (int c = 1; c <= colCount; c++)
            {
                var cell = ws.Cell(currentRow, c);
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Double; // Chuẩn kế toán đôi
                cell.Style.Border.TopBorderColor = XLColor.FromHtml("#94A3B8");
                cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#003B73");
            }

            // Ghi tổng cự ly nếu có
            if (coCotCuLy && colCuLyIdx > 0)
            {
                var sumCell = ws.Cell(currentRow, colCuLyIdx);
                sumCell.Value = tongCuLy;
                sumCell.Style.NumberFormat.Format = "#,##0.00";
                sumCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            // Dòng text số lượng
            ws.Cell(currentRow, 2).Value = $"{view.Count} mục dữ liệu";
            ws.Cell(currentRow, 2).Style.Font.Italic = true;
            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            // 5. CĂN CHỈNH ĐỘ RỘNG CỘT TỰ ĐỘNG (Auto-fit Columns)
            ws.Columns(1, colCount).AdjustToContents(10.0, 45.0);

            // Cố định dòng tiêu đề khi cuộn chuột (Freeze Panes)
            ws.SheetView.FreezeRows(headerRow);

            wb.SaveAs(filePath);
        }

        private static void GhiCsvThuong(
            DataView view,
            Dictionary<string, string> columnMapping,
            string filePath)
        {
            using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
            var headers = string.Join(",", columnMapping.Values.Select(EscapeCsv));
            writer.WriteLine(headers);

            int stt = 1;
            foreach (DataRowView drv in view)
            {
                var row = drv.Row;
                var rowVals = new List<string>();

                foreach (var kvp in columnMapping)
                {
                    string colName = kvp.Key;
                    if (colName.Equals("STT", StringComparison.OrdinalIgnoreCase))
                    {
                        rowVals.Add((stt++).ToString());
                        continue;
                    }

                    object rawVal = (row.Table.Columns.Contains(colName) && row[colName] != DBNull.Value)
                        ? row[colName]
                        : "";

                    rowVals.Add(EscapeCsv(rawVal.ToString()));
                }

                writer.WriteLine(string.Join(",", rowVals));
            }
        }

        #endregion

        #region TẠO TỆP MẪU NHẬP EXCEL & CSV CAO CẤP

        /// <summary>
        /// Tạo tệp mẫu nhập hàng loạt (.xlsx hoặc .csv) có màu sắc hướng dẫn, kiểu cột, ghi chú và dòng mẫu.
        /// </summary>
        public static bool TaoTepMau(
            string defaultFileName,
            string tieuDeMau,
            List<string> headers,
            List<string[]> sampleRows,
            List<string>? comments = null)
        {
            var sfd = new SaveFileDialog
            {
                Title = "Tải Tệp Mẫu Nhập Dữ Liệu Hàng Loạt",
                FileName = defaultFileName,
                Filter = "Tệp Microsoft Excel (*.xlsx)|*.xlsx|Tệp CSV (Excel) (*.csv)|*.csv",
                DefaultExt = Path.GetExtension(defaultFileName)
            };

            if (sfd.ShowDialog() != true) return false;

            string filePath = sfd.FileName;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            try
            {
                if (ext == ".xlsx")
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("DuLieuNhapMau");

                    // Title
                    ws.Cell(1, 1).Value = tieuDeMau.ToUpperInvariant();
                    ws.Range(1, 1, 1, headers.Count).Merge();
                    ws.Cell(1, 1).Style.Font.Bold = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 12;
                    ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#003B73");
                    ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Row(1).Height = 24;

                    // Instructions
                    ws.Cell(2, 1).Value = "Hướng dẫn: Nhập đúng dữ liệu vào các cột bên dưới. Không thay đổi tên dòng tiêu đề (Dòng 3).";
                    ws.Range(2, 1, 2, headers.Count).Merge();
                    ws.Cell(2, 1).Style.Font.Italic = true;
                    ws.Cell(2, 1).Style.Font.FontSize = 9;
                    ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");

                    // Headers (Row 3)
                    ws.Row(3).Height = 24;
                    for (int i = 0; i < headers.Count; i++)
                    {
                        var cell = ws.Cell(3, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontSize = 10;
                        cell.Style.Font.FontColor = XLColor.White;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003B73");
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#00274D");

                        if (comments != null && i < comments.Count && !string.IsNullOrWhiteSpace(comments[i]))
                        {
                            cell.CreateComment().AddText(comments[i]);
                        }
                    }

                    // Sample Rows (Row 4 onwards)
                    int curRow = 4;
                    foreach (var sRow in sampleRows)
                    {
                        ws.Row(curRow).Height = 20;
                        for (int c = 0; c < sRow.Length && c < headers.Count; c++)
                        {
                            var cell = ws.Cell(curRow, c + 1);
                            cell.Value = sRow[c];
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                        curRow++;
                    }

                    ws.Columns(1, headers.Count).AdjustToContents(12.0, 40.0);
                    ws.SheetView.FreezeRows(3);
                    wb.SaveAs(filePath);
                }
                else
                {
                    using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
                    writer.WriteLine(string.Join(",", headers.Select(EscapeCsv)));
                    foreach (var sRow in sampleRows)
                    {
                        writer.WriteLine(string.Join(",", sRow.Select(EscapeCsv)));
                    }
                }

                bool xemNgay = ThongBaoDialog.XacNhan(
                    $"Đã tạo thành công tệp mẫu tại:\n{filePath}\n\nBạn có muốn mở tệp mẫu để xem hoặc nhập dữ liệu ngay không?",
                    "Tạo Tệp Mẫu Thành Công",
                    nutDongY: "Mở tệp ngay",
                    nutHuy: "Đóng");

                if (xemNgay)
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }

                return true;
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi tạo tệp mẫu:\n{ex.Message}", "Lỗi Tạo Tệp");
                return false;
            }
        }

        #endregion
    }
}
