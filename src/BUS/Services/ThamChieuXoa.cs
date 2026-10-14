using DTO.Common;

namespace BUS.Services
{
    // =========================================================================
    // XOA HAN MOT BAN GHI (Dat chot 14/10)
    //
    // Dau may, toa xe, ho so nhan vien chi xoa han duoc khi CHUA co du lieu nao tro toi
    // (nhap nham). Da co lich su thi xoa se vi pham khoa ngoai va lam mat ho so an toan
    // -> de nghi chuyen trang thai thay the:
    //   dau may / toa xe -> "Ngung khai thac" (NGUNG_KHAI_THAC, migration V20261014_0900)
    //   nhan vien        -> "Da nghi viec" (DA_NGHI_VIEC)
    // Lop nay chi viet cau mo ta du lieu dang tro toi cho nguoi dung doc.
    // =========================================================================
    public static class ThamChieuXoa
    {
        // Cac dong "• ..." (chi loai co du lieu); rong = xoa duoc
        public static List<string> MoTa(IEnumerable<SoThamChieuDTO> ds)
            => ds.Where(t => t.SoLuong > 0).Select(MoTaMot).ToList();

        private static string MoTaMot(SoThamChieuDTO t)
        {
            string viDu = t.ViDu.Length == 0 ? "" : $" ({t.ViDu}{(t.SoLuong > 3 ? ", …" : "")})";
            return t.Loai switch
            {
                "DOAN_TAU" => $"có trong đoàn tàu của {t.SoLuong} chuyến{viDu}",
                "CAP_NHIEN_LIEU" => $"{t.SoLuong} phiếu cấp nhiên liệu",
                "BAO_DUONG" => $"{t.SoLuong} phiếu bảo dưỡng",
                "VAN_DON" => $"{t.SoLuong} vận đơn hàng hóa xếp trên toa",
                "KIP_LAI" => $"có trong {t.SoLuong} kíp lái{viDu}",
                "KIEM_TRA" => $"{t.SoLuong} phiếu kiểm tra lên ban",
                "TAI_KHOAN" => $"{t.SoLuong} tài khoản đăng nhập{(t.ViDu.Length > 0 ? $" ({t.ViDu})" : "")}",
                "LICH_SU_MA" => $"lịch sử đổi mã{(t.ViDu.Length > 0 ? $" ({t.ViDu})" : "")}",
                _ => $"{t.SoLuong} bản ghi khác ({t.Loai})"
            };
        }

        // Thong diep khi Service tu choi xoa vi con du lieu tro toi
        public static string KhongXoaDuoc(string doiTuong, List<string> thamChieu, string goiY)
            => $"Không xóa được {doiTuong} vì đã có dữ liệu liên quan:\n" +
               string.Join("\n", thamChieu.Select(t => "• " + t)) +
               $"\n\n{goiY}";
    }
}
