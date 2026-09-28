namespace GUI.Views.KyThuat.Models
{
    // =========================================================================
    // DU LIEU MAU TINH cho giai doan DUNG GIAO DIEN.
    //
    // Cac dialog them/sua va man hinh lap tau keo-tha chua ghi CSDL, nen can
    // mot bo danh muc tinh de hien thi. Moi gia tri bam dung ten cot, gia tri
    // enum va seed data trong tailieu\script_tao_csdl_v2.sql de sau nay thay
    // bang truy van that khong phai doi binding.
    // =========================================================================
    public static class DanhMucMauPhuongTien
    {
        // Rang buoc CK cua vanhanh.DoanTau: TongSoToa <= 20, TongChieuDaiM <= 450
        public const int SoToaToiDa = 20;
        public const decimal ChieuDaiDoanTauToiDaM = 450.0m;

        // Nguong tai trong truc tham khao cho kho duong 1.000 mm, chi dung de mo phong.
        // Can doi chieu quy chuan chinh thuc truoc khi dua vao bao cao.
        public const decimal TaiTrongTrucToiDaTan = 14.0m;

        // Thoi gian do toi thieu tai ga trung gian de cat / noi toa (don toa, noi ong gio,
        // thu ham lai). QUY UOC GIAO DIEN - chua doi chieu quy trinh tac nghiep cua VNR.
        public const int SoPhutDoToiThieuCatNoi = 15;

        // ---------------------------------------------------------------------
        // Dong dau may (phuongtien.DongDauMay) - trung seed data
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<DongDauMayMau> DongDauMay = new[]
        {
            new DongDauMayMau("D19E", "CSR Ziyang (Trung Quốc)", 1900, 120, 1200.0m, 4000, 78.0m, 16.5m),
            new DongDauMayMau("D13E", "CKD Praha (Tiệp Khắc)",   1350, 100,  900.0m, 3200, 84.0m, 15.8m)
        };

        public static readonly IReadOnlyList<string> XiNghiepDauMay = new[]
        {
            "Xí nghiệp Đầu máy Hà Nội",
            "Xí nghiệp Đầu máy Vinh",
            "Xí nghiệp Đầu máy Đà Nẵng",
            "Xí nghiệp Đầu máy Sài Gòn"
        };

        // Dau may du phong khi khong ket noi duoc CSDL (trung seed data)
        public static readonly IReadOnlyList<(string SoHieu, string MaDong, string DonVi, string TrangThai)> DauMayMau = new[]
        {
            ("D19E-901", "D19E", "Xí nghiệp Đầu máy Hà Nội",  "SAN_SANG"),
            ("D19E-902", "D19E", "Xí nghiệp Đầu máy Hà Nội",  "SAN_SANG"),
            ("D19E-903", "D19E", "Xí nghiệp Đầu máy Đà Nẵng", "SAN_SANG"),
            ("D13E-701", "D13E", "Xí nghiệp Đầu máy Sài Gòn", "SAN_SANG")
        };

        // ---------------------------------------------------------------------
        // Chung loai toa (phuongtien.ChungLoaiToa) - trung seed data,
        // kem tu trong / tai trong / suc chua tham khao va mau hien thi.
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<ChungLoaiToaMau> ChungLoaiToa = new[]
        {
            new ChungLoaiToaMau(1, "NC",  "Toa ngồi cứng thông thường",        20.0m, 4, 35.5m, 12.0m, 80, false, "#FFFBEB", "#FDE68A", "#B45309"),
            new ChungLoaiToaMau(2, "NML", "Toa ngồi mềm điều hòa không khí",   20.0m, 4, 36.0m, 10.0m, 64, false, "#EFF6FF", "#BFDBFE", "#1D4ED8"),
            new ChungLoaiToaMau(3, "BN",  "Toa nằm khoang 6 giường điều hòa",  20.0m, 4, 38.0m,  8.0m, 42, false, "#EEF2FF", "#C7D2FE", "#4338CA"),
            new ChungLoaiToaMau(4, "AN",  "Toa nằm khoang 4 giường điều hòa",  20.0m, 4, 39.0m,  6.0m, 28, false, "#F0FDF4", "#BBF7D0", "#15803D"),
            new ChungLoaiToaMau(5, "G",   "Toa xe hàng mui kín chuyên dụng",   15.5m, 4, 24.0m, 30.0m,  0, true,  "#F1F5F9", "#CBD5E1", "#475569"),
            new ChungLoaiToaMau(6, "M",   "Toa chuyên chở xe máy 2 tầng",      19.5m, 4, 25.0m, 20.0m,  0, true,  "#F1F5F9", "#CBD5E1", "#475569"),
            new ChungLoaiToaMau(7, "P",   "Toa xe mặt võng chở Container",     14.2m, 4, 22.0m, 35.0m,  0, true,  "#F1F5F9", "#CBD5E1", "#475569")
        };

        public static ChungLoaiToaMau TimChungLoai(string? maCode)
        {
            foreach (var cl in ChungLoaiToa)
            {
                if (string.Equals(cl.MaChungLoaiCode, maCode, StringComparison.OrdinalIgnoreCase))
                    return cl;
            }
            // Chung loai la: hien thi trung tinh
            return new ChungLoaiToaMau(0, maCode ?? "?", maCode ?? "Không rõ", 20.0m, 4, 35.0m, 10.0m, 0, false,
                                       "#F1F5F9", "#CBD5E1", "#475569");
        }

        public static DongDauMayMau? TimDongDauMay(string? maCode)
        {
            foreach (var d in DongDauMay)
            {
                if (string.Equals(d.MaDongCode, maCode, StringComparison.OrdinalIgnoreCase))
                    return d;
            }
            return null;
        }

        // ---------------------------------------------------------------------
        // Bai toa mau: bo sung them toa san sang de man hinh keo-tha du phong phu.
        // So hieu khong trung voi 6 toa trong seed data (NC-101, NML-201, BN-301,
        // AN-401, M-601, P-701).
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<(string SoHieu, string MaCode)> BaiToaMau = new[]
        {
            ("NC-102", "NC"), ("NC-103", "NC"),
            ("NML-202", "NML"), ("NML-203", "NML"), ("NML-204", "NML"),
            ("BN-302", "BN"), ("BN-303", "BN"), ("BN-304", "BN"),
            ("AN-402", "AN"), ("AN-403", "AN"),
            ("G-501", "G"), ("G-502", "G"),
            ("M-602", "M")
        };

        // ---------------------------------------------------------------------
        // Lich dung ga mau (vanhanh.LichDungGa) cho thanh hanh trinh cua man hinh
        // lap tau - cat / noi toa doc duong. Repository chua co truy van LichDungGa
        // nen bam seed data: MaChuyenTau 1..8 theo thu tu INSERT, MaGa theo thu tu
        // INSERT cua hatang.Ga. Phut tinh tu 00:00 ngay tau chay ga dau
        // (> 1440 = sang ngay hom sau). Chi dung de hien gio / tinh thoi gian do.
        // ---------------------------------------------------------------------
        private static readonly GaDungMau HNI = new(1, "HNI", "Hà Nội"), PLY = new(3, "PLY", "Phủ Lý"),
            NDH = new(4, "NDH", "Nam Định"), NBH = new(5, "NBH", "Ninh Bình"), THA = new(7, "THA", "Thanh Hóa"),
            VIN = new(8, "VIN", "Vinh"), DHO = new(11, "DHO", "Đồng Hới"), HUE = new(13, "HUE", "Huế"),
            LCO = new(14, "LCO", "Lăng Cô"), DAN = new(15, "DAN", "Đà Nẵng"), DTR = new(18, "DTR", "Diêu Trì"),
            NTR = new(20, "NTR", "Nha Trang"), BTH = new(22, "BTH", "Bình Thuận"), SGO = new(27, "SGO", "Sài Gòn");

        private static LichDungMau Lich(int maChuyen, string mac, bool boSung, params (GaDungMau Ga, int Den, int Di)[] diem)
            => new(maChuyen, mac, diem.Select((d, i) => new DiemDungMau(d.Ga, i + 1, d.Den, d.Di)).ToList(), boSung);

        public static readonly IReadOnlyList<LichDungMau> LichDungGa = new[]
        {
            Lich(1, "SE1", false, (HNI, 360, 370), (PLY, 430, 435), (VIN, 660, 675), (HUE, 1080, 1095),
                                  (LCO, 1200, 1210), (DAN, 1275, 1290), (SGO, 2280, 2280)),
            Lich(2, "SE2", false, (SGO, 1140, 1150), (DAN, 2040, 2055), (VIN, 2460, 2475), (THA, 2640, 2650),
                                  (HNI, 2820, 2820)),
            // Seed khong co lich dung cho SE3 - bo sung mau theo mau chay cua SE19 (trung DanhMucMauNhanSu)
            Lich(3, "SE3", true,  (HNI, 1120, 1140), (PLY, 1195, 1198), (NDH, 1228, 1233), (THA, 1315, 1320),
                                  (VIN, 1480, 1490), (DHO, 1730, 1738), (HUE, 1920, 1930), (DAN, 2100, 2100)),
            Lich(4, "SE4", false, (SGO, 1140, 1155), (BTH, 1395, 1400), (NTR, 1700, 1715), (DTR, 1960, 1970),
                                  (DAN, 2350, 2365), (VIN, 2920, 2930), (HNI, 3330, 3330)),
            Lich(5, "SE19", false, (HNI, 1170, 1190), (PLY, 1245, 1248), (NDH, 1278, 1283), (THA, 1365, 1370),
                                   (VIN, 1550, 1560), (DHO, 1810, 1818), (HUE, 2000, 2010), (DAN, 2180, 2180)),
            Lich(6, "NA1", false, (HNI, 1315, 1335), (PLY, 1392, 1395), (NDH, 1425, 1430), (NBH, 1460, 1463),
                                  (THA, 1525, 1530), (VIN, 1780, 1780))
        };

        // Khop theo MaChuyenTau + mac tau; lech (CSDL nap lai seed nhieu lan) thi lay theo
        // mac tau neu mac do chi co mot lich mau.
        public static LichDungMau? TimLichDung(int maChuyenTau, string soHieuMacTau)
        {
            var theoMa = LichDungGa.FirstOrDefault(l => l.MaChuyenTau == maChuyenTau && l.SoHieuMacTau == soHieuMacTau);
            if (theoMa != null) return theoMa;

            var theoMac = LichDungGa.Where(l => l.SoHieuMacTau == soHieuMacTau).ToList();
            return theoMac.Count == 1 ? theoMac[0] : null;
        }
    }

    public record GaDungMau(int MaGa, string MaGaCode, string TenGa);

    public record DiemDungMau(GaDungMau Ga, int ThuTuDung, int PhutDen, int PhutDi);

    public record LichDungMau(int MaChuyenTau, string SoHieuMacTau, IReadOnlyList<DiemDungMau> DiemDung, bool LaBoSungMau);

    public record DongDauMayMau(
        string MaDongCode,
        string NhaSanXuat,
        int CongSuatHP,
        int TocDoToiDaKmh,
        decimal SucKeoToiDaTan,
        int DungTichBonDauLit,
        decimal TrongLuongTan,
        decimal ChieuDaiM)
    {
        public string TenHienThi => $"{MaDongCode} — {NhaSanXuat}";
    }

    public record ChungLoaiToaMau(
        int MaChungLoai,
        string MaChungLoaiCode,
        string TenMoTa,
        decimal ChieuDaiChuanM,
        int SoTruc,
        decimal TuTrongMacDinhTan,
        decimal TaiTrongMacDinhTan,
        int SucChuaThamKhao,
        bool LaToaHang,
        string MauNen,
        string MauVien,
        string MauChu)
    {
        public string TenHienThi => $"{MaChungLoaiCode} — {TenMoTa}";

        // Ten ngan dung trong bang bien che (trung cach dat ten cua PhuongTienRepository.LayBienCheToaXe)
        public string TenNgan => MaChungLoaiCode switch
        {
            "NC" => "Ngồi cứng",
            "NML" => "Ngồi mềm điều hòa",
            "BN" => "Giường nằm khoang 6",
            "AN" => "Giường nằm khoang 4",
            "G" => "Toa hàng mui kín",
            "M" => "Toa chở xe máy",
            "P" => "Toa chở container",
            _ => TenMoTa
        };
    }
}
