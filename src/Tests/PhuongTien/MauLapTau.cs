using DTO.PhuongTien;

namespace Tests.PhuongTien
{
    // =========================================================================
    // Du lieu mau trong bo nho cho luat lap tau (khong cham CSDL).
    // Chuyen HNI -> VIN -> HUE -> DAN -> SGO, kieu SE3 sau seed 09/10:
    //   VIN do 15', HUE do 5' (khong du cat / noi), DAN do 25'.
    // Toa khach 46 t toan tai / 4 truc = 11,5 t/truc; toa hang G 50 t = 12,5 t/truc;
    // P-301 57 t = 14,25 t/truc (vuot nguong 14 t).
    // =========================================================================
    internal static class MauLapTau
    {
        public const int HNI = 1, VIN = 2, HUE = 3, DAN = 4, SGO = 5;
        public const int DM901 = 901, DM903 = 903, DM904Yeu = 904, DM702BaoDuong = 702;
        public const int ToaHang201 = 201, ToaHang202 = 202, ToaP301 = 301, ToaBaoDuong401 = 401, ToaBan402 = 402;

        public static readonly DateTime GioDi = new(2026, 10, 20, 19, 0, 0);
        public static readonly DateTime GioDen = GioDi.AddHours(32.5);

        // Toa khach so 1..25 -> ma 101..125
        public static int Khach(int so) => 100 + so;

        public static BoiCanhLapTauDTO BoiCanh(int soGa = 5)
        {
            var hanhTrinh = new List<DiemDungHanhTrinhDTO>
            {
                Ga(1, HNI, "HNI", "Hà Nội", 0, GioDi, GioDi),
                Ga(2, VIN, "VIN", "Vinh", 319, GioDi.AddMinutes(340), GioDi.AddMinutes(355)),
                Ga(3, HUE, "HUE", "Huế", 688, GioDi.AddMinutes(780), GioDi.AddMinutes(785)),
                Ga(4, DAN, "DAN", "Đà Nẵng", 791, GioDi.AddMinutes(960), GioDi.AddMinutes(985)),
                Ga(5, SGO, "SGO", "Sài Gòn", 1726, GioDen, GioDen),
            };
            if (soGa == 2) hanhTrinh = new() { hanhTrinh[0], hanhTrinh[4] };

            var bc = new BoiCanhLapTauDTO
            {
                MaChuyenTau = 7,
                SoHieuMacTau = "SE3",
                LoaiTau = "TAU_NHANH",
                TrangThaiChuyen = "DA_LEN_LICH",
                NgayXuatPhat = GioDi.Date,
                GioXuatPhatKH = GioDi,
                GioVeDichKH = GioDen,
                MaGaDiCode = "HNI", TenGaDi = "Hà Nội",
                MaGaDenCode = "SGO", TenGaDen = "Sài Gòn",
                HanhTrinh = hanhTrinh,
                ChoPhepSua = true
            };

            bc.DauMay.Add(DauMay(DM901, "D19E-901", "SAN_SANG", 1000m));
            bc.DauMay.Add(DauMay(DM903, "D19E-903", "SAN_SANG", 1000m));
            bc.DauMay.Add(DauMay(DM904Yeu, "D19E-904", "SAN_SANG", 300m));
            bc.DauMay.Add(DauMay(DM702BaoDuong, "D13E-702", "BAO_DUONG", 800m));

            for (int so = 1; so <= 25; so++)
                bc.ToaXe.Add(Toa(Khach(so), $"NC-{100 + so}", "NC", 20.0m, 36m, 10m));
            bc.ToaXe.Add(Toa(ToaHang201, "G-201", "G", 14.0m, 20m, 30m));
            bc.ToaXe.Add(Toa(ToaHang202, "G-202", "G", 14.0m, 20m, 30m));
            bc.ToaXe.Add(Toa(ToaP301, "P-301", "P", 14.0m, 22m, 35m));
            bc.ToaXe.Add(Toa(ToaBaoDuong401, "NC-401", "NC", 20.0m, 36m, 10m, "BAO_DUONG"));

            var toaBan = Toa(ToaBan402, "NC-402", "NC", 20.0m, 36m, 10m, "DA_NOI");
            toaBan.ChuyenKhac.Add(Chuyen("SE1", "DANG_CHAY", GioDi.AddHours(-10), GioDi.AddHours(20), "SGO", "Sài Gòn"));
            bc.ToaXe.Add(toaBan);
            return bc;
        }

        public static PhuongAnLapTauDTO PhuongAn(int? chinh, int? day, IEnumerable<ToaTrongDoanDTO> toa)
            => new() { MaChuyenTau = 7, MaDauMayChinh = chinh, MaDauMayDay = day, DanhSachToa = toa.ToList() };

        public static PhuongAnLapTauDTO PhuongAn(int? chinh, params ToaTrongDoanDTO[] toa) => PhuongAn(chinh, null, toa);

        public static ToaTrongDoanDTO T(int maToa, int? noi = null, int? cat = null)
            => new() { MaToaXe = maToa, MaGaNoi = noi, MaGaCat = cat };

        // n toa khach chay suot, bat dau tu toa so `tu`
        public static IEnumerable<ToaTrongDoanDTO> KhachSuot(int n, int tu = 1)
            => Enumerable.Range(tu, n).Select(so => T(Khach(so)));

        public static ChuyenSuDungDTO Chuyen(string mac, string trangThai, DateTime tu, DateTime den,
                                            string gaCuoiCode = "", string gaCuoiTen = "", string vaiTro = "CHINH")
            => new()
            {
                MaChuyenTau = 99, SoHieuMacTau = mac, TrangThai = trangThai, VaiTro = vaiTro,
                GioXuatPhatKH = tu, GioVeDichKH = den, GaDauCode = "HNI", GaCuoiCode = gaCuoiCode, GaCuoiTen = gaCuoiTen
            };

        private static DiemDungHanhTrinhDTO Ga(int thuTu, int maGa, string code, string ten, decimal km, DateTime den, DateTime di)
            => new() { ThuTuDung = thuTu, MaGa = maGa, MaGaCode = code, TenGa = ten, LyTrinhKm = km, GioDenKeHoach = den, GioDiKeHoach = di };

        private static DauMayLapTauDTO DauMay(int ma, string soHieu, string trangThai, decimal sucKeo)
            => new()
            {
                MaDauMay = ma, SoHieuDauMay = soHieu, MaDongCode = soHieu[..4], TrangThai = trangThai,
                SucKeoToiDaTan = sucKeo, ChieuDaiM = 19.0m, TrongLuongTan = 84m
            };

        private static ToaXeLapTauDTO Toa(int ma, string soHieu, string loai, decimal dai, decimal tuTrong, decimal taiTrong,
                                          string trangThai = "SAN_SANG")
            => new()
            {
                MaToaXe = ma, SoHieuToaXe = soHieu, MaChungLoaiCode = loai, TrangThai = trangThai,
                ChieuDaiM = dai, SoTruc = 4, TuTrongTan = tuTrong, TaiTrongToiDaTan = taiTrong
            };
    }
}
