using DTO.NhanSu;

namespace Tests.NhanSu
{
    // =========================================================================
    // Anh chup du lieu nhan su trong bo nho (thay NhanSuRepository.LayDuLieu).
    // Moc gio tinh tu hom nay de han kham "sap het" / "het han" dung nghia khi chay bat ky ngay nao.
    //
    //   C SE19 (3): HNI 06:00 -> VIN 12:00 ngay mai           kip 2: LT_002 / PL_003 / TT_003
    //   A SE1  (1): HNI 19:00 ngay mai -> VIN -> DAN -> SGO    chua co kip
    //   B SE3  (2): HNI 20:00 ngay mai -> VIN -> SGO           kip 1: LT_009 / PL_002 / TT_002 (chong gio A)
    //   D NA1  (4): HNI 22:00 +3 ngay -> VIN, dau may D13E    chua co kip
    //
    // Lai tau cho chuyen A, chang HNI -> VIN:
    //   LT_001 du dieu kien (Ha Noi)          LT_002 chi nghi 7 gio sau SE19 (R4)
    //   LT_003 bang D13E != D19E (R7)         LT_004 het han kham (R8)
    //   LT_005 han kham con 10 ngay (canh bao, van chon duoc)
    //   LT_006 da nghi viec (R9, khong vao danh sach)
    //   LT_007 khong dat kiem tra SE1 (R6)    LT_008 doi Sai Gon (khac depot, van chon duoc)
    //   LT_009 dang o kip SE3 chong gio (R3)
    // =========================================================================
    internal static class MauNhanSu
    {
        public const int ChuyenA = 1, ChuyenB = 2, ChuyenC = 3, ChuyenD = 4;
        public const int HNI = 1, VIN = 2, DAN = 3, SGO = 4;

        public static readonly GaNhanSu GaHNI = new(HNI, "HNI", "Hà Nội");
        public static readonly GaNhanSu GaVIN = new(VIN, "VIN", "Vinh");
        public static readonly GaNhanSu GaDAN = new(DAN, "DAN", "Đà Nẵng");
        public static readonly GaNhanSu GaSGO = new(SGO, "SGO", "Sài Gòn");
        public static readonly GaNhanSu[] CacGa = { GaHNI, GaVIN, GaDAN, GaSGO };

        public static DateTime HomNay => DateTime.Today;
        public static DateTime NgayMai => DateTime.Today.AddDays(1);
        public static DateTime BayGio => DateTime.Today.AddHours(8);

        public static List<ChuyenTauNhanSu> CacChuyen()
        {
            DateTime n = NgayMai;
            return new()
            {
                Chuyen(ChuyenA, "SE1", "D19E-901", "D19E",
                    Diem(GaHNI, 1, n.AddHours(19), n.AddHours(19)),
                    Diem(GaVIN, 2, n.AddHours(24 + 0.67), n.AddHours(24 + 0.92)),
                    Diem(GaDAN, 3, n.AddHours(24 + 11), n.AddHours(24 + 11.42)),
                    Diem(GaSGO, 4, n.AddHours(48 + 4), n.AddHours(48 + 4))),
                Chuyen(ChuyenB, "SE3", "D19E-904", "D19E",
                    Diem(GaHNI, 1, n.AddHours(20), n.AddHours(20)),
                    Diem(GaVIN, 2, n.AddHours(24 + 1.67), n.AddHours(24 + 1.92)),
                    Diem(GaSGO, 3, n.AddHours(48 + 6), n.AddHours(48 + 6))),
                Chuyen(ChuyenC, "SE19", "D19E-906", "D19E",
                    Diem(GaHNI, 1, n.AddHours(6), n.AddHours(6)),
                    Diem(GaVIN, 2, n.AddHours(12), n.AddHours(12))),
                Chuyen(ChuyenD, "NA1", "D13E-701", "D13E",
                    Diem(GaHNI, 1, n.AddDays(2).AddHours(22), n.AddDays(2).AddHours(22)),
                    Diem(GaVIN, 2, n.AddDays(3).AddHours(6), n.AddDays(3).AddHours(6))),
            };
        }

        public static List<NhanVienHienThi> CacNhanVien() => new()
        {
            NV(1, "LT_001", "Lái tàu", "D19E", 400, "Đội lái tàu Hà Nội"),
            NV(2, "LT_002", "Lái tàu", "D19E", 400, "Đội lái tàu Hà Nội"),
            NV(3, "LT_003", "Lái tàu", "D13E", 400, "Đội lái tàu Hà Nội"),
            NV(4, "LT_004", "Lái tàu", "D19E", -1, "Đội lái tàu Hà Nội"),
            NV(5, "LT_005", "Lái tàu", "D19E", 10, "Đội lái tàu Hà Nội"),
            NV(6, "LT_006", "Lái tàu", "D19E", 400, "Đội lái tàu Hà Nội", NhanVienHienThi.DaNghiViec),
            NV(7, "LT_007", "Lái tàu", "D19E", 400, "Đội lái tàu Hà Nội"),
            NV(8, "LT_008", "Lái tàu", "D19E", 400, "Đội lái tàu Sài Gòn"),
            NV(9, "LT_009", "Lái tàu", "D19E", 400, "Đội lái tàu Hà Nội"),
            NV(10, "PL_001", "Phụ lái", "D19E", 400, "Đội lái tàu Hà Nội"),
            NV(11, "PL_002", "Phụ lái", "D19E", 400, "Đội lái tàu Hà Nội"),
            NV(12, "PL_003", "Phụ lái", "D19E", 400, "Đội lái tàu Vinh"),
            NV(13, "PL_004", "Phụ lái", "D19E", 400, "Đội lái tàu Đà Nẵng"),
            NV(14, "PL_005", "Phụ lái", "D13E", 400, "Đội lái tàu Hà Nội"),
            NV(20, "TT_001", "Trưởng tàu", null, 400, "Đoàn tiếp viên Hà Nội"),
            NV(21, "TT_002", "Trưởng tàu", null, 400, "Đoàn tiếp viên Hà Nội"),
            NV(22, "TT_003", "Trưởng tàu", null, 400, "Đoàn tiếp viên Hà Nội"),
            NV(23, "TT_004", "Trưởng tàu", null, 400, "Đoàn tiếp viên Phương Nam"),
            NV(30, "KX_001", "Nhân viên khám xe", null, 400, "Trạm khám xe Hà Nội"),
        };

        public static List<PhanCongKipHienThi> KipGoc(IReadOnlyList<ChuyenTauNhanSu> chuyen) => new()
        {
            Kip(1, chuyen, ChuyenB, HNI, VIN, lai: 9, phu: 11, truong: 21),
            Kip(2, chuyen, ChuyenC, HNI, VIN, lai: 2, phu: 12, truong: 22),
        };

        public static List<KiemTraLenBanHienThi> KiemTraGoc() => new()
        {
            // LT_007 do con 0,15 mg/L truoc SE1 -> khong dat
            new() { MaKiemTra = 1, MaNhanVien = 7, MaChuyenTau = ChuyenA, NongDoConMgL = 0.15m, SoGioNghiTruocCa = 12m,
                    ThoiDiemKiemTra = NgayMai.AddHours(18.2), MaNVCode = "LT_007", SoHieuMacTau = "SE1" },
        };

        public static DuLieuNhanSu DuLieu(IEnumerable<PhanCongKipHienThi>? kipThem = null,
                                          IEnumerable<KiemTraLenBanHienThi>? kiemTraThem = null)
        {
            var chuyen = CacChuyen();
            var kip = KipGoc(chuyen);
            if (kipThem != null)
                foreach (var k in kipThem)
                {
                    k.Chuyen = chuyen.Single(c => c.MaChuyenTau == k.MaChuyenTau);
                    kip.Add(k);
                }
            var kiemTra = KiemTraGoc();
            if (kiemTraThem != null) kiemTra.AddRange(kiemTraThem);

            return new DuLieuNhanSu(CacNhanVien(), kip, kiemTra, chuyen, CacGa, new[] { "D13E", "D19E" }, BayGio);
        }

        public static PhanCongKipHienThi Kip(int ma, IReadOnlyList<ChuyenTauNhanSu> chuyen, int maChuyen, int nhan, int giao,
                                             int lai, int phu, int truong, string trangThai = PhanCongKipHienThi.DaPhanCong)
            => new()
            {
                MaPhanCong = ma, MaChuyenTau = maChuyen, MaGaNhanBan = nhan, MaGaBanGiao = giao,
                MaLaiTau = lai, MaPhuLai = phu, MaTruongTau = truong, TrangThai = trangThai,
                Chuyen = chuyen.Single(c => c.MaChuyenTau == maChuyen)
            };

        // Kip them vao sau (Chuyen gan khi dung DuLieu)
        public static PhanCongKipHienThi KipMoi(int ma, int maChuyen, int nhan, int giao, int lai, int phu, int truong,
                                                string trangThai = PhanCongKipHienThi.DaPhanCong)
            => new()
            {
                MaPhanCong = ma, MaChuyenTau = maChuyen, MaGaNhanBan = nhan, MaGaBanGiao = giao,
                MaLaiTau = lai, MaPhuLai = phu, MaTruongTau = truong, TrangThai = trangThai
            };

        private static ChuyenTauNhanSu Chuyen(int ma, string mac, string dauMay, string dong, params DiemDungNhanSu[] lich)
            => new()
            {
                MaChuyenTau = ma, SoHieuMacTau = mac, LoaiTau = "TAU_NHANH", TrangThai = "DA_LEN_LICH",
                NgayXuatPhat = lich[0].GioDiKeHoach.Date, GioXuatPhatKH = lich[0].GioDiKeHoach, GioVeDichKH = lich[^1].GioDenKeHoach,
                GaDi = lich[0].Ga, GaDen = lich[^1].Ga, SoHieuDauMay = dauMay, MaDongDauMay = dong,
                CoLichDungGa = true, LichDung = lich
            };

        private static DiemDungNhanSu Diem(GaNhanSu ga, int thuTu, DateTime den, DateTime di) => new(ga, thuTu, den, di);

        private static NhanVienHienThi NV(int ma, string code, string chucDanh, string? bang, int hanKhamConNgay, string donVi,
                                          string trangThai = NhanVienHienThi.SanSang)
            => new()
            {
                MaNhanVien = ma, MaNVCode = code, HoTen = $"Nhân viên {code}", SoDienThoai = "0912345678",
                ChucDanh = chucDanh, HangBangLai = bang, HanKhamSucKhoe = DateTime.Today.AddDays(hanKhamConNgay),
                DonViChuQuan = donVi, TrangThai = trangThai
            };
    }
}
