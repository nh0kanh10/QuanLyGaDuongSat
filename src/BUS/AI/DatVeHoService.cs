using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BUS.Services;
using DTO.AI;
using ET.VanTai;

namespace BUS.AI
{
    // =========================================================================
    // DAT VE HO (16/11): AI (cong cu dat_ve) hoac nut "Dat Ve Ngay" tren the phuong an -> cua so xac nhan (GUI) -> day.
    // Luat kiem tra Y NHU man Ban ve (BanVePage.BtnThanhToan_Click cua A Ba): <= 8 ve, ten + SDT nguoi mua (0 + 9 so),
    // moi ve ho ten + CCCD 12 so, khong trung CCCD trong don. Ghi CSDL qua VeService.DatVeTheoDon (sp_DatVeTheoChang:
    // khoa ghe, chan ghe vua bi ban, chan 1 CCCD 2 ve chong chang) -> khong co duong ghi ve thu hai.
    // Thong tin hanh khach (ten, CCCD, SDT) chi nhap o cua so xac nhan, KHONG gui cho mo hinh ngon ngu.
    // =========================================================================
    public sealed class DatVeHoService
    {
        public const int SoVeToiDa = 8;

        public static readonly (string Ma, string Ten)[] CacHinhThucTT =
        {
            ("TIEN_MAT", "Tiền mặt"), ("CHUYEN_KHOAN", "Chuyển khoản / VietQR"),
            ("THE_NGAN_HANG", "Thẻ POS ngân hàng"), ("VI_DIEN_TU", "Ví điện tử"),
        };

        // Giam gia lam tron giong gio ve (VeService.TinhMucGiamGia).
        public static decimal TinhGiam(decimal giaGoc, string loaiKhach, Func<string, decimal> tyLe)
            => giaGoc <= 0 ? 0 : Math.Round(giaGoc * tyLe(loaiKhach ?? "THUONG"), 0);

        // Mot dong / ghe cua phuong an; loai khach lay tu tu van (nhan vien sua lai o cua so xac nhan neu khac).
        public static List<HanhKhachDatVe> TaoHanhKhach(PhuongAnVe pa, Func<string, decimal> tyLe)
            => pa.CacMaChoNgoi.Select((ma, i) =>
            {
                string loai = i < pa.CacLoaiKhach.Count ? pa.CacLoaiKhach[i] : "THUONG";
                return new HanhKhachDatVe
                {
                    MaChoNgoi = ma,
                    SoGhe = i < pa.CacSoGhe.Count ? pa.CacSoGhe[i] : 0,
                    LoaiKhach = loai,
                    GiaGoc = pa.GiaMotVe,
                    SoTienGiam = TinhGiam(pa.GiaMotVe, loai, tyLe),
                };
            }).ToList();

        public static List<string> KiemTra(string tenNguoiMua, string sdtNguoiMua, IReadOnlyList<HanhKhachDatVe> ds)
        {
            var loi = new List<string>();
            if (ds.Count == 0) loi.Add("Không có vé nào để đặt.");
            if (ds.Count > SoVeToiDa) loi.Add($"Một đơn tối đa {SoVeToiDa} vé (đang có {ds.Count}).");
            if (string.IsNullOrWhiteSpace(tenNguoiMua)) loi.Add("Nhập họ tên người mua vé.");
            string sdt = (sdtNguoiMua ?? "").Trim();
            if (sdt.Length == 0) loi.Add("Nhập số điện thoại người mua.");
            else if (!Regex.IsMatch(sdt, @"^0\d{9}$")) loi.Add("Số điện thoại phải gồm 10 chữ số, bắt đầu bằng 0.");
            for (int i = 0; i < ds.Count; i++)
            {
                var hk = ds[i];
                string nhan = $"Vé {i + 1} (chỗ {hk.SoGhe})";
                if (string.IsNullOrWhiteSpace(hk.HoTen)) loi.Add($"{nhan}: nhập họ tên hành khách.");
                string cccd = (hk.Cccd ?? "").Trim();
                if (cccd.Length == 0) loi.Add($"{nhan}: nhập số CCCD.");
                else if (!Regex.IsMatch(cccd, @"^\d{12}$")) loi.Add($"{nhan}: CCCD phải gồm đúng 12 chữ số.");
            }
            var trung = ds.Select(x => (x.Cccd ?? "").Trim()).Where(x => x.Length > 0)
                          .GroupBy(x => x).FirstOrDefault(g => g.Count() > 1);
            if (trung != null) loi.Add($"CCCD {trung.Key} bị nhập cho nhiều hành khách — mỗi vé cần CCCD riêng.");
            return loi;
        }

        // Lap don giong BanVePage: nguoi mua lay CCCD cua hanh khach trung ten (khong co thi hanh khach dau),
        // loai khach cua don = ve dau, trang thai thanh toan DA_THANH_TOAN, nguoi ban = tai khoan dang nhap.
        public static (KhachHang Khach, DonDatVe Don, List<Ve> CacVe) LapDon(YeuCauDatVeHo yc, string tenNguoiMua, string sdtNguoiMua,
                                                                              string hinhThucTT, IReadOnlyList<HanhKhachDatVe> ds, int maTaiKhoan)
        {
            string ten = tenNguoiMua.Trim();
            string cccdNguoiMua = ds.FirstOrDefault(v => string.Equals(v.HoTen.Trim(), ten, StringComparison.OrdinalIgnoreCase))?.Cccd.Trim()
                                  ?? ds[0].Cccd.Trim();
            var khach = new KhachHang { HoTen = ten, SoCCCD = cccdNguoiMua, SoDienThoai = sdtNguoiMua.Trim(), LoaiKhach = ds[0].LoaiKhach };
            var don = new DonDatVe
            {
                SoLuongVe = ds.Count,
                TongTien = ds.Sum(x => x.ThucThu),
                HinhThucTT = hinhThucTT,
                TrangThaiTT = "DA_THANH_TOAN",
                MaNhanVienBan = maTaiKhoan,
            };
            var ve = ds.Select(x => new Ve
            {
                MaChuyenTau = yc.PhuongAn.MaChuyenTau,
                MaChoNgoi = x.MaChoNgoi,
                MaGaDi = yc.YeuCau.MaGaDi,
                MaGaDen = yc.YeuCau.MaGaDen,
                TenHanhKhach = x.HoTen.Trim(),
                CCCDHanhKhach = x.Cccd.Trim(),
                GiaVeGoc = x.GiaGoc,
                SoTienGiam = x.SoTienGiam,
                GiaVeThucThu = x.ThucThu,
                TrangThai = "DA_DAT",
            }).ToList();
            return (khach, don, ve);
        }

        // Ghi CSDL. Loi (ghe vua bi ban, khong du quyen, trung CCCD chong chang...) -> KetQuaDatVeHo.Loi, khong nem.
        public KetQuaDatVeHo Dat(YeuCauDatVeHo yc, string tenNguoiMua, string sdtNguoiMua, string hinhThucTT, IReadOnlyList<HanhKhachDatVe> ds)
        {
            var loi = KiemTra(tenNguoiMua, sdtNguoiMua, ds);
            if (loi.Count > 0) return new KetQuaDatVeHo { Loi = string.Join("\n", loi) };
            var (khach, don, ve) = LapDon(yc, tenNguoiMua, sdtNguoiMua, hinhThucTT, ds, PhanQuyenService.MaTaiKhoanHienTai);
            if (!new VeService().DatVeTheoDon(khach, don, ve, yc.PhuongAn.SoHieuMacTau, out string pnr, out string err))
                return new KetQuaDatVeHo { Loi = string.IsNullOrWhiteSpace(err) ? "Không đặt được vé." : err };
            return new KetQuaDatVeHo
            {
                DaDat = true,
                MaPnr = pnr,
                CacMaVe = ve.Select(v => v.MaVeCode).ToList(),
                SoVe = ve.Count,
                TongTien = don.TongTien,
                HinhThucTT = hinhThucTT,
            };
        }
    }
}
