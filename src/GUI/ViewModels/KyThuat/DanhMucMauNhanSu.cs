using GUI.Helpers;
namespace GUI.ViewModels.KyThuat
{
    // =========================================================================
    // DU LIEU MAU TINH cho phan he Nhan su, To tau & Phan cong ca truc
    // (giai doan DUNG GIAO DIEN - chua co Repository cho schema nhansu).
    //
    // Bam seed trong tailieu\script_tao_csdl_v2.sql:
    //   - 13 nhan vien (MaNhanVien 1..13), 4 kip, 11 lan kiem tra
    //   - MaNVCode doi tu NV_001..NV_013 sang ma theo chuc danh (LT_/PL_/TT_...,
    //     quy uoc giao dien 29/09 - MaNhanVienTheoChucDanh); seed can sua theo
    //     khi noi CSDL (bang doi ma: CLAUDE.md muc 5.8)
    //   - Kip 3 (SE19), 4 (NA1), 5 (SE3) da thay nguoi de khong con ca vi pham
    //   - 8 chuyen tau (MaChuyenTau 1..8 theo thu tu INSERT) kem lich dung ga
    //   - Ga: MaGa theo thu tu INSERT cua hatang.Ga
    // Ngay gio tinh tuong doi theo hom nay giong @NgayNay / @HomQua / @NgayMai.
    // Ban ghi ghi "bo sung mau" khong co trong seed.
    // =========================================================================
    public static class DanhMucMauNhanSu
    {
        private static readonly DateTime LucNap = DateTime.Now;
        private static readonly DateTime HomNay = LucNap.Date;
        private static readonly DateTime HomQua = HomNay.AddDays(-1);
        private static readonly DateTime NgayMai = HomNay.AddDays(1);

        // ---------------------------------------------------------------------
        // Ga (chi cac ga co mat trong lich dung / kip lai)
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<GaNhanSu> Ga = new[]
        {
            new GaNhanSu(1, "HNI", "Hà Nội"),     new GaNhanSu(2, "GBA", "Giáp Bát"),
            new GaNhanSu(3, "PLY", "Phủ Lý"),     new GaNhanSu(4, "NDH", "Nam Định"),
            new GaNhanSu(5, "NBH", "Ninh Bình"),  new GaNhanSu(7, "THA", "Thanh Hóa"),
            new GaNhanSu(8, "VIN", "Vinh"),       new GaNhanSu(11, "DHO", "Đồng Hới"),
            new GaNhanSu(13, "HUE", "Huế"),       new GaNhanSu(14, "LCO", "Lăng Cô"),
            new GaNhanSu(15, "DAN", "Đà Nẵng"),   new GaNhanSu(18, "DTR", "Diêu Trì"),
            new GaNhanSu(20, "NTR", "Nha Trang"), new GaNhanSu(22, "BTH", "Bình Thuận"),
            new GaNhanSu(25, "STH", "Sóng Thần"), new GaNhanSu(27, "SGO", "Sài Gòn")
        };

        public static GaNhanSu? TimGa(int maGa) => Ga.FirstOrDefault(g => g.MaGa == maGa);
        private static GaNhanSu G(string maGaCode) => Ga.First(g => g.MaGaCode == maGaCode);

        // Ga dong quan cua don vi - tham khao cho R10 (CSDL chua co bang chang - depot)
        public static GaNhanSu? GaDongQuan(string? donVi)
        {
            if (string.IsNullOrWhiteSpace(donVi)) return null;
            if (donVi.Contains("Hà Nội")) return G("HNI");
            if (donVi.Contains("Vinh")) return G("VIN");
            if (donVi.Contains("Đà Nẵng")) return G("DAN");
            if (donVi.Contains("Sài Gòn") || donVi.Contains("Phương Nam")) return G("SGO");
            return null;
        }

        // ---------------------------------------------------------------------
        // Chuyen tau (vanhanh.ChuyenTau + LichDungGa + DoanTau.MaDauMayChinh)
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<ChuyenTauNhanSu> ChuyenTau = TaoChuyenTau();

        public static ChuyenTauNhanSu? TimChuyen(int maChuyenTau)
            => ChuyenTau.FirstOrDefault(c => c.MaChuyenTau == maChuyenTau);

        private static DiemDungNhanSu D(string ga, int thuTu, DateTime ngay, int phutDen, int phutDi)
            => new(G(ga), thuTu, ngay.AddMinutes(phutDen), ngay.AddMinutes(phutDi));

        private static ChuyenTauNhanSu TaoChuyen(int ma, string mac, string loai, DateTime ngay, DateTime di, DateTime den,
                                                 string trangThai, string gaDi, string gaDen,
                                                 string? dauMay, string? dong, DiemDungNhanSu[]? lichDung)
        {
            // Chuyen chua co lich dung ga: 2 diem ga di / ga den theo gio cua chuyen
            var diem = lichDung ?? new[]
            {
                new DiemDungNhanSu(G(gaDi), 1, di, di),
                new DiemDungNhanSu(G(gaDen), 2, den, den)
            };

            return new ChuyenTauNhanSu
            {
                MaChuyenTau = ma, SoHieuMacTau = mac, LoaiTau = loai, NgayXuatPhat = ngay,
                GioXuatPhatKH = di, GioVeDichKH = den, TrangThai = trangThai,
                GaDi = G(gaDi), GaDen = G(gaDen), SoHieuDauMay = dauMay, MaDongDauMay = dong,
                CoLichDungGa = lichDung != null, LichDung = diem
            };
        }

        private static List<ChuyenTauNhanSu> TaoChuyenTau() => new()
        {
            TaoChuyen(1, "SE1", "TAU_NHANH", HomNay, HomNay.AddHours(6), HomNay.AddHours(38), "DANG_CHAY",
                      "HNI", "SGO", "D19E-901", "D19E", new[]
                      {
                          D("HNI", 1, HomNay, 360, 370),   D("PLY", 2, HomNay, 430, 435),
                          D("VIN", 3, HomNay, 660, 675),   D("HUE", 4, HomNay, 1080, 1095),
                          D("LCO", 5, HomNay, 1200, 1210), D("DAN", 6, HomNay, 1275, 1290),
                          D("SGO", 7, HomNay, 2280, 2280)
                      }),
            TaoChuyen(2, "SE2", "TAU_NHANH", HomQua, HomQua.AddHours(19), HomQua.AddHours(51), "DANG_CHAY",
                      "SGO", "HNI", "D19E-902", "D19E", new[]
                      {
                          D("SGO", 1, HomQua, 1140, 1150), D("DAN", 2, HomNay, 600, 615),
                          D("VIN", 3, HomNay, 1020, 1035), D("THA", 4, HomNay, 1200, 1210),
                          D("HNI", 5, HomNay, 1380, 1380)
                      }),
            // Seed khong co lich dung cho SE3 - bo sung mau theo mau chay cua SE19
            TaoChuyen(3, "SE3", "TAU_NHANH", NgayMai, NgayMai.AddHours(19), NgayMai.AddHours(35), "DA_LEN_LICH",
                      "HNI", "DAN", "D19E-901", "D19E", new[]
                      {
                          D("HNI", 1, NgayMai, 1120, 1140), D("PLY", 2, NgayMai, 1195, 1198),
                          D("NDH", 3, NgayMai, 1228, 1233), D("THA", 4, NgayMai, 1315, 1320),
                          D("VIN", 5, NgayMai, 1480, 1490), D("DHO", 6, NgayMai, 1730, 1738),
                          D("HUE", 7, NgayMai, 1920, 1930), D("DAN", 8, NgayMai, 2100, 2100)
                      }),
            TaoChuyen(4, "SE4", "TAU_NHANH", HomNay, HomQua.AddHours(19), HomQua.AddHours(51), "DANG_CHAY",
                      "SGO", "HNI", "D19E-902", "D19E", new[]
                      {
                          D("SGO", 1, HomQua, 1140, 1155), D("BTH", 2, HomQua, 1395, 1400),
                          D("NTR", 3, HomNay, 260, 275),   D("DTR", 4, HomNay, 520, 530),
                          D("DAN", 5, HomNay, 910, 925),   D("VIN", 6, HomNay, 1480, 1490),
                          D("HNI", 7, HomNay, 1890, 1890)
                      }),
            TaoChuyen(5, "SE19", "TAU_NHANH", HomNay, HomNay.AddMinutes(1190), HomNay.AddMinutes(2180), "SAN_SANG",
                      "HNI", "DAN", "D19E-901", "D19E", new[]
                      {
                          D("HNI", 1, HomNay, 1170, 1190), D("PLY", 2, HomNay, 1245, 1248),
                          D("NDH", 3, HomNay, 1278, 1283), D("THA", 4, HomNay, 1365, 1370),
                          D("VIN", 5, HomNay, 1550, 1560), D("DHO", 6, HomNay, 1810, 1818),
                          D("HUE", 7, HomNay, 2000, 2010), D("DAN", 8, HomNay, 2180, 2180)
                      }),
            TaoChuyen(6, "NA1", "TAU_CHO", HomNay, HomNay.AddMinutes(1335), HomNay.AddMinutes(1780), "DA_LEN_LICH",
                      "HNI", "VIN", "D13E-701", "D13E", new[]
                      {
                          D("HNI", 1, HomNay, 1315, 1335), D("PLY", 2, HomNay, 1392, 1395),
                          D("NDH", 3, HomNay, 1425, 1430), D("NBH", 4, HomNay, 1460, 1463),
                          D("THA", 5, HomNay, 1525, 1530), D("VIN", 6, HomNay, 1780, 1780)
                      }),
            TaoChuyen(7, "SE1", "TAU_NHANH", HomQua, HomQua.AddHours(6), HomQua.AddHours(38), "HOAN_THANH",
                      "HNI", "SGO", null, null, null),
            TaoChuyen(8, "HBN1", "TAU_HANG", HomNay, HomNay.AddHours(4), HomNay.AddHours(33), "DANG_CHAY",
                      "GBA", "STH", null, null, null)
        };

        // ---------------------------------------------------------------------
        // Nhan vien (nhansu.NhanVien). MaNhanVien 1..13 trung seed (ma NV da
        // doi theo chuc danh); nguoi thuoc kip dang thuc hien de DANG_LAM cho
        // khop R13 (seed de SAN_SANG).
        // ---------------------------------------------------------------------
        public static List<NhanVienHienThi> LayNhanVien() => new()
        {
            NV(1,  "LT_001", "Nguyễn Văn An",     "0901234567", "Lái tàu",    "D19E", new DateTime(2027, 12, 31), "Đội lái tàu Hà Nội", NhanVienHienThi.DangLam, 400),
            NV(2,  "PL_001", "Trần Đình Bình",    "0902345678", "Phụ lái",    "D19E", new DateTime(2027, 12, 31), "Đội lái tàu Hà Nội", NhanVienHienThi.DangLam, 400),
            NV(3,  "TT_001", "Lê Văn Cường",      "0903456789", "Trưởng tàu", null,   new DateTime(2027, 12, 31), "Đoàn tiếp viên Hà Nội", NhanVienHienThi.DangLam, 400),
            NV(4,  "KX_001", "Phạm Minh Dũng",    "0904567890", "Nhân viên khám xe", null, new DateTime(2027, 12, 31), "Trạm Giáp Bát", NhanVienHienThi.SanSang, 400),
            NV(5,  "BV_001", "Hoàng Thị Mai",     "0905678901", "Nhân viên bán vé",  null, new DateTime(2027, 12, 31), "Ga Hà Nội", NhanVienHienThi.SanSang, 400),
            NV(6,  "LT_002", "Vũ Tiến Đạt",       "0906789012", "Lái tàu",    "D19E", HomNay.AddDays(15),         "Đội lái tàu Hà Nội", NhanVienHienThi.SanSang, 380),
            NV(7,  "LT_003", "Phan Văn Hùng",     "0907123456", "Lái tàu",    "D19E", new DateTime(2027, 10, 15), "Đội lái tàu Sài Gòn", NhanVienHienThi.DangLam, 380),
            NV(8,  "PL_002", "Đặng Thế Anh",      "0908234567", "Phụ lái",    "D19E", new DateTime(2027, 9, 20),  "Đội lái tàu Sài Gòn", NhanVienHienThi.DangLam, 380),
            NV(9,  "TT_002", "Bùi Xuân Khoa",     "0909345678", "Trưởng tàu", null,   new DateTime(2028, 1, 10),  "Đoàn tiếp viên Phương Nam", NhanVienHienThi.DangLam, 380),
            NV(10, "LT_004", "Lê Đình Chiến",     "0910456789", "Lái tàu",    "D19E", new DateTime(2027, 6, 30),  "Đội lái tàu Vinh", NhanVienHienThi.SanSang, 380),
            NV(11, "PL_003", "Phạm Thành Long",   "0911567890", "Phụ lái",    "D19E", new DateTime(2027, 8, 12),  "Đội lái tàu Vinh", NhanVienHienThi.SanSang, 380),
            NV(12, "LT_005", "Ngô Tất Tố",        "0912678901", "Lái tàu",    "D13E", new DateTime(2027, 11, 25), "Đội lái tàu Hà Nội", NhanVienHienThi.SanSang, 380),
            NV(13, "PL_004", "Hoàng Trọng Nghĩa", "0913789012", "Phụ lái",    "D13E", new DateTime(2027, 12, 5),  "Đội lái tàu Hà Nội", NhanVienHienThi.SanSang, 380),

            // --- Bo sung mau ---
            NV(14, "LT_006", "Đỗ Minh Tuấn",      "0914890123", "Lái tàu",    "D19E", new DateTime(2027, 3, 15),  "Đội lái tàu Đà Nẵng", NhanVienHienThi.SanSang, 300),
            NV(15, "PL_005", "Trịnh Văn Hải",     "0915901234", "Phụ lái",    "D19E", new DateTime(2027, 5, 20),  "Đội lái tàu Đà Nẵng", NhanVienHienThi.SanSang, 300),
            NV(16, "TT_003", "Nguyễn Thị Hạnh",   "0916012345", "Trưởng tàu", null,   new DateTime(2027, 4, 2),   "Đoàn tiếp viên Đà Nẵng", NhanVienHienThi.SanSang, 300),
            NV(17, "TT_004", "Lương Văn Thành",   "0917123456", "Trưởng tàu", null,   new DateTime(2027, 8, 1),   "Đoàn tiếp viên Hà Nội", NhanVienHienThi.SanSang, 260),
            NV(18, "PL_006", "Phạm Quang Huy",    "0918234567", "Phụ lái",    "D19E", HomNay.AddDays(-10),        "Đội lái tàu Hà Nội", NhanVienHienThi.SanSang, 260),
            NV(19, "LT_007", "Mai Xuân Trường",   "0919345678", "Lái tàu",    "D19E", new DateTime(2027, 2, 10),  "Đội lái tàu Sài Gòn", NhanVienHienThi.SanSang, 200),
            NV(20, "PL_007", "Cao Văn Lực",       "0920456789", "Phụ lái",    "D19E", new DateTime(2027, 7, 18),  "Đội lái tàu Sài Gòn", NhanVienHienThi.SanSang, 200),
            NV(21, "TV_001", "Trần Thị Thu",      "0921567890", "Tiếp viên",  null,   new DateTime(2027, 5, 30),  "Đoàn tiếp viên Hà Nội", NhanVienHienThi.SanSang, 150),
            NV(22, "TV_002", "Vũ Thị Hoa",        "0922678901", "Tiếp viên",  null,   new DateTime(2027, 1, 12),  "Đoàn tiếp viên Phương Nam", NhanVienHienThi.NghiNgoi, 150),
            NV(23, "LT_008", "Hồ Văn Nam",        "0923789012", "Lái tàu",    "D13E", new DateTime(2026, 12, 1),  "Đội lái tàu Vinh", NhanVienHienThi.DaNghiViec, 900),
            // Truong tau Ha Noi thay Bui Xuan Khoa o kip NA1 (seed: trung gio SE4)
            NV(24, "TT_005", "Đinh Văn Quý",      "0924890123", "Trưởng tàu", null,   new DateTime(2027, 10, 20), "Đoàn tiếp viên Hà Nội", NhanVienHienThi.SanSang, 120)
        };

        private static NhanVienHienThi NV(int ma, string code, string hoTen, string sdt, string chucDanh, string? bang,
                                          DateTime hanKham, string donVi, string trangThai, int soNgayTruoc)
            => new()
            {
                MaNhanVien = ma, MaNVCode = code, HoTen = hoTen, SoDienThoai = sdt, ChucDanh = chucDanh,
                HangBangLai = bang, HanKhamSucKhoe = hanKham, DonViChuQuan = donVi, TrangThai = trangThai,
                NgayTao = HomNay.AddDays(-soNgayTruoc).AddHours(8)
            };

        // Don vi / chuc danh goi y cho dialog ho so (cot la chu tu do)
        public static readonly IReadOnlyList<string> ChucDanhGoiY = new[]
        {
            "Lái tàu", "Phụ lái", "Trưởng tàu", "Tiếp viên", "Nhân viên khám xe", "Nhân viên bán vé"
        };

        public static readonly IReadOnlyList<string> HangBangLaiGoiY = new[] { "D19E", "D13E" };

        // ---------------------------------------------------------------------
        // Kip lai (nhansu.PhanCongKipLai). 4 kip dau theo seed, rieng truong tau
        // kip 3 va kip 4 da thay: seed de Le Van Cuong (TT_001) o ca SE1 lan SE19,
        // Bui Xuan Khoa (TT_002) o ca SE4 lan NA1 -> trung khung gio (bai E4).
        // Du lieu mau khong con ca vi pham; ca do chi xuat hien khi co viec xay
        // ra sau luc phan (khong dat kiem tra, sua ho so).
        // ---------------------------------------------------------------------
        public static List<PhanCongKipHienThi> LayPhanCong() => new()
        {
            Kip(1, 1, 1, 2, 3,    "HNI", "VIN", PhanCongKipHienThi.DangThucHien),
            Kip(2, 4, 7, 8, 9,    "SGO", "NTR", PhanCongKipHienThi.DangThucHien),
            Kip(3, 5, 10, 11, 17, "HNI", "VIN", PhanCongKipHienThi.DaPhanCong),   // seed: truong tau 3
            Kip(4, 6, 12, 13, 24, "HNI", "VIN", PhanCongKipHienThi.DaPhanCong),   // seed: truong tau 9
            // Bo sung mau: kip SE3. Vu Tien Dat (LT_002) khong dat kiem tra SE3 (seed)
            // nen lai tau da thay bang Nguyen Van An (LT_001); phieu khong dat van giu trong so.
            Kip(5, 3, 1, 2, 17,   "HNI", "VIN", PhanCongKipHienThi.DaPhanCong)
        };

        private static PhanCongKipHienThi Kip(int ma, int maChuyen, int lai, int phu, int truong,
                                              string gaNhan, string gaGiao, string trangThai)
            => new()
            {
                MaPhanCong = ma, MaChuyenTau = maChuyen, MaLaiTau = lai, MaPhuLai = phu, MaTruongTau = truong,
                MaGaNhanBan = G(gaNhan).MaGa, MaGaBanGiao = G(gaGiao).MaGa, TrangThai = trangThai
            };

        // ---------------------------------------------------------------------
        // Kiem tra len ban (nhansu.KiemTraSucKhoe) - 11 dong trung seed.
        // Thoi diem kiem tra dat truoc gio nhan ban; khong de nam o tuong lai.
        // ---------------------------------------------------------------------
        public static List<KiemTraLenBanHienThi> LayKiemTra() => new()
        {
            KT(1,  1,  1, 0.00m, 10.0m, HomNay.AddMinutes(305)),
            KT(2,  2,  1, 0.00m, 9.5m,  HomNay.AddMinutes(310)),
            KT(3,  3,  1, 0.00m, 12.0m, HomNay.AddMinutes(315)),
            KT(4,  6,  3, 0.15m, 6.0m,  HomNay.AddMinutes(450)),     // vi pham: con > 0, nghi < 8 gio
            KT(5,  7,  4, 0.00m, 10.0m, HomQua.AddMinutes(1085)),
            KT(6,  8,  4, 0.00m, 9.5m,  HomQua.AddMinutes(1090)),
            KT(7,  9,  4, 0.00m, 11.0m, HomQua.AddMinutes(1095)),
            KT(8,  10, 5, 0.00m, 10.5m, HomNay.AddMinutes(1120)),
            KT(9,  11, 5, 0.00m, 9.0m,  HomNay.AddMinutes(1125)),
            KT(10, 12, 6, 0.00m, 11.5m, HomNay.AddMinutes(1265)),
            KT(11, 13, 6, 0.00m, 10.0m, HomNay.AddMinutes(1270))
        };

        private static KiemTraLenBanHienThi KT(int ma, int maNhanVien, int maChuyen, decimal con, decimal gioNghi, DateTime thoiDiem)
            => new()
            {
                MaKiemTra = ma, MaNhanVien = maNhanVien, MaChuyenTau = maChuyen,
                NongDoConMgL = con, SoGioNghiTruocCa = gioNghi,
                ThoiDiemKiemTra = thoiDiem > LucNap ? LucNap.AddMinutes(-4 * (12 - ma)) : thoiDiem
            };
    }
}
