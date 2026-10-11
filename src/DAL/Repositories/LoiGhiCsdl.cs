using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace DAL.Repositories
{
    // =========================================================================
    // Loi khi ghi CSDL da duoc dich sang cau tieng Viet de hien thang cho nguoi dung.
    // Repository bat SqlException -> nem LoiGhiCsdlException; Service bat lai tra ve
    // qua "out string loi" (khong de lo SqlException ra man hinh - KE_HOACH §2.2).
    // =========================================================================
    public class LoiGhiCsdlException : Exception
    {
        public int MaLoiSql { get; }

        public LoiGhiCsdlException(string thongDiep, SqlException goc) : base(thongDiep, goc)
        {
            MaLoiSql = goc.Number;
        }
    }

    // =========================================================================
    // Bo dich loi SQL cho cac bang cua Dat: Phuong tien (phuongtien.*, vanhanh.DoanTau,
    // vanhanh.ChiTietDoanTau) va Nhan su (nhansu.*, tu 11/10). UNIQUE / CHECK khong dat ten trong script goc nen ten rang
    // buoc do SQL Server tu sinh (UQ__DauMay__...) -> nhan dien theo ten bang + ten cot.
    //
    // KE_HOACH §2.5 du kien SqlLoiHelper dung chung (viec nen cua A Ba, chua co). Khi co thi
    // chuyen cac anh xa duoi day sang do.
    //
    // Service dung:  catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
    //                { loi = DichLoiSqlPhuongTien.ThongDiep(ex); return false; }
    // de moi loi CSDL (ca mat ket noi luc mo giao dich, luc doc, luc COMMIT) deu thanh
    // "out string loi" tieng Viet, khong lan len giao dien.
    // =========================================================================
    public static class DichLoiSqlPhuongTien
    {
        private static readonly Regex MauBang = new(@"(?:object|table) '?""?(?<bang>[\w\.]+)""?'?", RegexOptions.IgnoreCase);
        private static readonly Regex MauRangBuoc = new(@"constraint ""?'?(?<ten>[\w\.]+)""?'?", RegexOptions.IgnoreCase);
        private static readonly Regex MauCot = new(@"column '(?<cot>\w+)'", RegexOptions.IgnoreCase);
        private static readonly Regex MauGiaTriTrung = new(@"duplicate key value is \((?<gt>[^)]*)\)", RegexOptions.IgnoreCase);

        internal static LoiGhiCsdlException Dich(SqlException ex) => new(DichThongDiep(ex), ex);

        // Loi phat sinh khi lam viec voi CSDL: da dich (Repository), loi ADO.NET chua dich,
        // het thoi gian cho khoa ung dung (PhienGiaoDich.KhoaTaiNguyen)
        public static bool LaLoiCsdl(Exception ex) => ex is LoiGhiCsdlException or DbException or TimeoutException;

        public static string ThongDiep(Exception ex) => ex switch
        {
            SqlException sql => DichThongDiep(sql),
            DbException db => $"Lỗi CSDL: {db.Message}",
            _ => ex.Message
        };

        private static string DichThongDiep(SqlException ex)
        {
            string tin = ex.Message;
            string bang = MauBang.Match(tin).Groups["bang"].Value;
            string rangBuoc = MauRangBuoc.Match(tin).Groups["ten"].Value;
            string cot = MauCot.Match(tin).Groups["cot"].Value;
            string giaTri = MauGiaTriTrung.Match(tin).Groups["gt"].Value.Trim();

            switch (ex.Number)
            {
                // --- Trung khoa UNIQUE ---
                case 2627:
                case 2601:
                    if (bang.EndsWith("DauMay", StringComparison.OrdinalIgnoreCase))
                        return $"Số hiệu đầu máy {giaTri} đã tồn tại.";
                    if (bang.EndsWith("ToaXe", StringComparison.OrdinalIgnoreCase))
                        return $"Số hiệu toa xe {giaTri} đã tồn tại.";
                    if (rangBuoc.Equals("UQ_DoanTau_ToaXe", StringComparison.OrdinalIgnoreCase))
                        return "Một toa xe xuất hiện hai lần trong cùng đoàn tàu.";
                    if (rangBuoc.Equals("UQ_DoanTau_ThuTuNoi", StringComparison.OrdinalIgnoreCase))
                        return "Hai toa trùng thứ tự móc nối trong đoàn tàu.";
                    if (bang.EndsWith("DoanTau", StringComparison.OrdinalIgnoreCase))
                        return "Chuyến tàu này vừa được người khác lập đoàn tàu. Hãy nạp lại rồi lập lại.";
                    if (bang.EndsWith("NhanVien", StringComparison.OrdinalIgnoreCase))
                        return $"Mã nhân viên {giaTri} đã được cấp cho người khác. Vui lòng lưu lại để hệ thống cấp mã mới.";
                    return $"Dữ liệu bị trùng ({giaTri}).";

                // --- CHECK / FOREIGN KEY ---
                case 547:
                    return DichRangBuoc(rangBuoc, cot, tin);

                // --- Trigger RAISERROR / THROW: thong diep da la tieng Viet. Trigger co ROLLBACK thi
                //     SQL Server noi them loi 3609 ("The transaction ended in the trigger") -> chi lay loi dau.
                case 50000:
                    return ex.Errors.Count > 0 ? ex.Errors[0].Message : tin;

                case 1205:
                case 1222:
                    return "Thao tác bị xung đột với một người dùng khác đang ghi cùng dữ liệu. Vui lòng thử lại.";
                case -2:
                    return "Máy chủ CSDL phản hồi quá lâu. Vui lòng thử lại.";

                // --- Khong ket noi duoc / mat ket noi ---
                case -1:
                case 2:
                case 40:
                case 53:
                case 121:
                case 233:
                case 10053:
                case 10054:
                case 10060:
                case 11001:
                    return "Không kết nối được máy chủ CSDL. Kiểm tra SQL Server đang chạy và tên máy chủ trong appsettings.Local.json rồi thử lại.";
                case 4060:
                    return "Không mở được CSDL đã cấu hình (sai tên CSDL hoặc CSDL chưa được tạo). Kiểm tra appsettings.json.";
                case 18456:
                    return "Máy chủ CSDL từ chối đăng nhập. Kiểm tra chuỗi kết nối / quyền truy cập SQL Server.";
                case 2628:
                case 8152:
                    return "Có giá trị dài hơn độ dài cho phép của cột trong CSDL.";
                case 8115:
                    return "Có giá trị số vượt quá giới hạn của cột trong CSDL.";
                case 515:
                    return $"Thiếu dữ liệu bắt buộc{(cot.Length > 0 ? $" (cột {cot})" : "")}.";
                default:
                    return $"Lỗi CSDL (mã {ex.Number}): {tin}";
            }
        }

        private static string DichRangBuoc(string rangBuoc, string cot, string tin)
        {
            // Khoa ngoai co dat ten trong script
            if (rangBuoc.StartsWith("FK_", StringComparison.OrdinalIgnoreCase))
            {
                bool laXoa = tin.Contains("DELETE", StringComparison.OrdinalIgnoreCase);
                return rangBuoc switch
                {
                    "FK_DauMay_Dong" => "Dòng đầu máy không tồn tại.",
                    "FK_ToaXe_ChungLoai" => "Chủng loại toa không tồn tại.",
                    "FK_DoanTau_ChuyenTau" => "Chuyến tàu không tồn tại.",
                    "FK_DoanTau_DauMayChinh" => laXoa ? "Đầu máy đang được gán cho đoàn tàu." : "Đầu máy kéo chính không tồn tại.",
                    "FK_DoanTau_DauMayDay" => laXoa ? "Đầu máy đang được gán cho đoàn tàu." : "Đầu máy đẩy không tồn tại.",
                    "FK_CTDoanTau_ToaXe" => laXoa ? "Toa xe đang nằm trong một đoàn tàu." : "Toa xe không tồn tại.",
                    "FK_CTDoanTau_DoanTau" => "Đoàn tàu không tồn tại.",
                    "FK_CTDoanTau_GaNoi" => "Ga nối toa không tồn tại.",
                    "FK_CTDoanTau_GaCat" => "Ga cắt toa không tồn tại.",
                    "FK_KipLai_ChuyenTau" => "Chuyến tàu của kíp không tồn tại.",
                    "FK_KipLai_LaiTau" or "FK_KipLai_PhuLai" or "FK_KipLai_TruongTau"
                        => laXoa ? "Nhân viên đang có trong kíp lái, không xoá được (chuyển sang \"Đã nghỉ việc\")."
                                 : "Nhân viên trong kíp không tồn tại.",
                    "FK_KipLai_GaNhan" or "FK_KipLai_GaBanGiao" => "Ga nhận ban / bàn giao không tồn tại.",
                    "FK_KiemTra_NhanVien" => laXoa ? "Nhân viên đã có phiếu kiểm tra lên ban, không xoá được." : "Nhân viên không tồn tại.",
                    "FK_KiemTra_ChuyenTau" => "Chuyến tàu của phiếu kiểm tra không tồn tại.",
                    _ => laXoa ? "Bản ghi đang được dữ liệu khác tham chiếu, không xoá được."
                               : "Dữ liệu tham chiếu không tồn tại."
                };
            }

            if (rangBuoc.Equals("CK_CTDoanTau_GaNoiKhacGaCat", StringComparison.OrdinalIgnoreCase))
                return "Một toa không thể nối và cắt ở cùng một ga.";
            if (rangBuoc.Equals("CK_KipLai_KhacNhanVien", StringComparison.OrdinalIgnoreCase))
                return "Lái tàu, phụ lái và trưởng tàu phải là ba người khác nhau.";

            // CHECK do SQL Server tu dat ten: nhan dien theo cot
            return cot switch
            {
                "NamSanXuat" => "Năm sản xuất phải từ 1970 trở đi.",
                "SoKmTichLuy" => "Số km tích lũy không được âm.",
                "TrangThai" => "Trạng thái không hợp lệ.",
                "TuTrongTan" => "Tự trọng toa phải lớn hơn 0.",
                "TaiTrongToiDaTan" => "Tải trọng toa không được âm.",
                "TongSoToa" => "Đoàn tàu phải có từ 1 đến 20 toa.",
                "TongChieuDaiM" => "Đoàn tàu dài tối đa 450 m.",
                "TongTrongLuongTan" => "Trọng lượng đoàn tàu phải lớn hơn 0.",
                "ThuTuNoi" => "Thứ tự móc nối phải từ 1 trở lên.",
                "NongDoConMgL" => "Nồng độ cồn không được âm.",
                "SoGioNghiTruocCa" => "Số giờ nghỉ trước ca không được âm.",
                _ => $"Dữ liệu vi phạm ràng buộc {(rangBuoc.Length > 0 ? rangBuoc : "của CSDL")}."
            };
        }
    }
}
