namespace DTO.PhuongTien
{
    // =========================================================================
    // DONG BO TOA BAN VE (vantai.ToaXeKhach) THEO DOAN TAU (vanhanh.ChiTietDoanTau) - 13/11
    //
    // Dat chot 13/11: toa khach (NC / NML / BN / AN) thu k trong doan tau (thu tu moc noi) <-> "Toa k" ban ve
    // (ThuTuToa = k). Thay toa that cung loai thi ve giu nguyen toa / cho. Luu doan tau lam hong ve con hieu luc
    // (DA_DAT / DA_LEN_TAU) -> CHAN, liet ke ve. Toa chi con ve da hoan / da huy -> "ngung ban" (giu lich su).
    // =========================================================================

    // Mot toa ban ve DANG BAN (NgungBan = 0) cua chuyen
    public class ToaBanVeDTO
    {
        public int MaToaXeKhach { get; set; }
        public int ThuTuToa { get; set; }
        public string NhanHieuToa { get; set; } = "";
        public string LoaiToa { get; set; } = "";
        public int SucChua { get; set; }
        public int SoGhe { get; set; }            // so dong vantai.ChoNgoi da sinh
        public int? MaToaXe { get; set; }
        public int? MaGaNoi { get; set; }
        public int? MaGaCat { get; set; }
    }

    // Mot ve tren toa ban ve dang ban cua chuyen (moi trang thai)
    public class VeTrenToaDTO
    {
        public int MaVe { get; set; }
        public string MaVeCode { get; set; } = "";
        public int MaToaXeKhach { get; set; }
        public int SoGhe { get; set; }
        public int MaGaDi { get; set; }
        public int MaGaDen { get; set; }
        public string MaGaDiCode { get; set; } = "";
        public string MaGaDenCode { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public string TenHanhKhach { get; set; } = "";

        public bool ConHieuLuc => TrangThai is "DA_DAT" or "DA_LEN_TAU";
    }

    public static class LoaiThaoTacToaBanVe
    {
        public const string Them = "THEM";            // mo ban toa k moi (+ sinh ghe 1..SucChua)
        public const string CapNhat = "CAP_NHAT";     // giu toa k (cung loai): doi toa that / pham vi ga, bo sung ghe con thieu
        public const string Xoa = "XOA";              // toa k chua tung co ve: xoa ghe + toa
        public const string NgungBan = "NGUNG_BAN";   // toa k chi con ve da hoan / da huy: an khoi ban ve, giu lich su
    }

    public class ThaoTacToaBanVeDTO
    {
        public string Loai { get; set; } = "";
        public int ThuTuToa { get; set; }
        public int? MaToaXeKhach { get; set; }        // toa ban ve dang co (CAP_NHAT / XOA / NGUNG_BAN)
        public string NhanHieuToa { get; set; } = "";
        public string LoaiToa { get; set; } = "";
        public int SucChua { get; set; }
        public int? MaToaXe { get; set; }
        public int? MaGaNoi { get; set; }
        public int? MaGaCat { get; set; }
        public string LyDo { get; set; } = "";        // vd "đổi từ Ngồi mềm sang Nằm khoang 6"

        // CAP_NHAT: co thay doi gi trong CSDL khong (toa that / pham vi / ghe thieu)
        public bool CoGhi { get; set; } = true;
    }

    // Ve con hieu luc bi phuong an lam hong -> chan luu
    public class VeBiAnhHuongDTO
    {
        public string MaVeCode { get; set; } = "";
        public string TenHanhKhach { get; set; } = "";
        public int ThuTuToa { get; set; }
        public int SoGhe { get; set; }
        public string Chang { get; set; } = "";
        public string LyDo { get; set; } = "";

        public string MoTa => $"{MaVeCode} · Toa {ThuTuToa} chỗ {SoGhe} · {Chang} · {TenHanhKhach} — {LyDo}";
    }

    public class KeHoachDongBoToaBanVeDTO
    {
        // false = tau hang: khong ban ve
        public bool LaTauKhach { get; set; }
        public int SoToaKhachTrongDoan { get; set; }
        public List<ThaoTacToaBanVeDTO> ThaoTac { get; set; } = new();
        public List<VeBiAnhHuongDTO> VeBiAnhHuong { get; set; } = new();

        public bool CoXungDot => VeBiAnhHuong.Count > 0;
        public bool CoThayDoi => ThaoTac.Any(t => t.CoGhi);

        public int SoMoBanMoi => ThaoTac.Count(t => t.Loai == LoaiThaoTacToaBanVe.Them);
        public int SoBo => ThaoTac.Count(t => t.Loai is LoaiThaoTacToaBanVe.Xoa or LoaiThaoTacToaBanVe.NgungBan);

        // "mở bán thêm toa 11, 12 · ngừng bán toa 5 (đổi loại toa)"; rong = khong doi
        public string MoTaThayDoi
        {
            get
            {
                var phan = new List<string>();
                var them = ThaoTac.Where(t => t.Loai == LoaiThaoTacToaBanVe.Them).Select(t => t.ThuTuToa).ToList();
                var bo = ThaoTac.Where(t => t.Loai is LoaiThaoTacToaBanVe.Xoa or LoaiThaoTacToaBanVe.NgungBan)
                                .Select(t => t.ThuTuToa).Distinct().ToList();
                var thayToaThat = ThaoTac.Count(t => t.Loai == LoaiThaoTacToaBanVe.CapNhat && t.CoGhi);
                if (them.Count > 0) phan.Add($"mở bán toa {string.Join(", ", them)}");
                if (bo.Count > 0) phan.Add($"bỏ toa {string.Join(", ", bo)} cũ");
                if (thayToaThat > 0) phan.Add($"cập nhật {thayToaThat} toa (toa thật / phạm vi ga)");
                return string.Join(" · ", phan);
            }
        }
    }
}
