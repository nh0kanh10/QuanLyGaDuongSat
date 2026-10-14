namespace DTO.PhuongTien
{
    // =========================================================================
    // DU LIEU TRAO DOI CUA CHUC NANG LAP DOAN TAU (vanhanh.DoanTau + ChiTietDoanTau)
    //
    //   DAL  -> BoiCanhLapTauDTO   : moi thu can de kiem tra mot phuong an (chuyen,
    //                                hanh trinh, duong ray, dau may, toa xe, ai dang dung gi)
    //   GUI  -> PhuongAnLapTauDTO  : phuong an nguoi dung vua xep (dau may + danh sach toa)
    //   BUS  -> KetQuaKiemTraLapTauDTO : so lieu tung khu doan / chang + bang hang muc an toan
    // =========================================================================

    // Mot chuyen tau KHAC co dung dau may / toa xe dang xet (de doi chieu trung gio, vi tri)
    public class ChuyenSuDungDTO
    {
        public int MaChuyenTau { get; set; }
        public string SoHieuMacTau { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public DateTime GioXuatPhatKH { get; set; }
        public DateTime GioVeDichKH { get; set; }

        // KEO_CHINH | DAY (dau may) · TOA (toa xe)
        public string VaiTro { get; set; } = "";

        // Ga dau / ga cuoi thuc te cua chuyen (lich dung ga, khong co thi theo mac tau)
        public string GaDauCode { get; set; } = "";
        public string GaCuoiCode { get; set; } = "";
        public string GaCuoiTen { get; set; } = "";

        public string KhungGio => $"{GioXuatPhatKH:HH:mm dd/MM} → {GioVeDichKH:HH:mm dd/MM}";
    }

    // Mot ga dung cua chuyen (vanhanh.LichDungGa) kem du lieu duong ray de doi chieu chieu dai
    public class DiemDungHanhTrinhDTO
    {
        public int ThuTuDung { get; set; }
        public int MaGa { get; set; }
        public string MaGaCode { get; set; } = "";
        public string TenGa { get; set; } = "";
        public decimal LyTrinhKm { get; set; }
        public DateTime GioDenKeHoach { get; set; }
        public DateTime GioDiKeHoach { get; set; }

        // Duong ray dieu do da gan cho diem dung (LichDungGa.MaDuongRay), co the null
        public int? MaDuongRayGan { get; set; }
        public int? SoHieuDuongGan { get; set; }
        public decimal? ChieuDaiDuongGanM { get; set; }

        // Duong tranh dai nhat cua ga (khong tinh duong dang bao tri), co the null
        public decimal? DuongTranhDaiNhatM { get; set; }
        public int? SoHieuDuongTranhDaiNhat { get; set; }

        public int SoPhutDo => (int)Math.Round((GioDiKeHoach - GioDenKeHoach).TotalMinutes);
    }

    // Khu gian tren hanh trinh can dau may day (hatang.KhuGian.CanDauMayDay = 1)
    public class KhuGianCanDayDTO
    {
        public string TuGaCode { get; set; } = "";
        public string DenGaCode { get; set; } = "";
        public string TenTuGa { get; set; } = "";
        public string TenDenGa { get; set; } = "";
        public decimal DoDocPermil { get; set; }

        public string Ten => $"{TenTuGa} – {TenDenGa} ({DoDocPermil:0.#}‰)";
    }

    public class DauMayLapTauDTO
    {
        public int MaDauMay { get; set; }
        public string SoHieuDauMay { get; set; } = "";
        public string MaDongCode { get; set; } = "";
        public string DonViQuanLy { get; set; } = "";
        public string TrangThai { get; set; } = "SAN_SANG";
        public decimal SucKeoToiDaTan { get; set; }
        public decimal ChieuDaiM { get; set; }
        public decimal TrongLuongTan { get; set; }

        // Cac chuyen khac dung dau may nay trong khoang 7 ngay quanh chuyen dang xet
        // (ke ca chuyen da hoan thanh - de biet dau may dang nam o ga nao)
        public List<ChuyenSuDungDTO> ChuyenKhac { get; set; } = new();
    }

    public class ToaXeLapTauDTO
    {
        public int MaToaXe { get; set; }
        public string SoHieuToaXe { get; set; } = "";
        public string MaChungLoaiCode { get; set; } = "";
        public string TenMoTa { get; set; } = "";
        public string TrangThai { get; set; } = "SAN_SANG";
        public decimal ChieuDaiM { get; set; }
        public int SoTruc { get; set; }
        public decimal TuTrongTan { get; set; }
        public decimal TaiTrongToiDaTan { get; set; }

        public decimal TongTrongLuongTan => TuTrongTan + TaiTrongToiDaTan;

        // Cac chuyen KHAC con hieu luc, chong khung gio voi chuyen dang xet, co toa nay trong doan
        public List<ChuyenSuDungDTO> ChuyenKhac { get; set; } = new();
    }

    // Mot toa trong phuong an. Thu tu trong danh sach = thu tu moc noi (1 = ngay sau dau may).
    public class ToaTrongDoanDTO
    {
        public int MaToaXe { get; set; }
        public int? MaGaNoi { get; set; }
        public int? MaGaCat { get; set; }
    }

    // Moi thu can de kiem tra / luu mot phuong an lap tau cho mot chuyen
    public class BoiCanhLapTauDTO
    {
        // --- Chuyen tau ---
        public int MaChuyenTau { get; set; }
        public string SoHieuMacTau { get; set; } = "";
        public string LoaiTau { get; set; } = "";
        public string TrangThaiChuyen { get; set; } = "";
        public DateTime NgayXuatPhat { get; set; }
        public DateTime GioXuatPhatKH { get; set; }
        public DateTime GioVeDichKH { get; set; }
        public string MaGaDiCode { get; set; } = "";
        public string TenGaDi { get; set; } = "";
        public string MaGaDenCode { get; set; } = "";
        public string TenGaDen { get; set; } = "";

        // --- Doan tau dang luu trong CSDL (null = chuyen chua lap doan tau) ---
        public int? MaDoanTau { get; set; }
        public byte[]? PhienBan { get; set; }
        public int? MaDauMayChinh { get; set; }
        public int? MaDauMayDay { get; set; }
        public bool DaDuyetAnToan { get; set; }
        public int TongSoToa { get; set; }
        public decimal TongChieuDaiM { get; set; }
        public decimal TongTrongLuongTan { get; set; }
        public List<ToaTrongDoanDTO> DoanHienTai { get; set; } = new();

        // --- Hanh trinh + ha tang ---
        public List<DiemDungHanhTrinhDTO> HanhTrinh { get; set; } = new();
        public List<KhuGianCanDayDTO> KhuGianCanDay { get; set; } = new();

        // --- Phuong tien (toan doi, kem thong tin ai dang dung) ---
        public List<DauMayLapTauDTO> DauMay { get; set; } = new();
        public List<ToaXeLapTauDTO> ToaXe { get; set; } = new();

        // --- Quyen sua theo trang thai chuyen (LapTauService dien) ---
        public bool ChoPhepSua { get; set; }
        public string LyDoKhongChoSua { get; set; } = "";

        public string TenChuyen => $"{SoHieuMacTau} · {NgayXuatPhat:dd/MM/yyyy} · {TenGaDi} → {TenGaDen}";

        public DauMayLapTauDTO? TimDauMay(int? maDauMay)
            => maDauMay == null ? null : DauMay.FirstOrDefault(d => d.MaDauMay == maDauMay.Value);

        public ToaXeLapTauDTO? TimToa(int maToaXe) => ToaXe.FirstOrDefault(t => t.MaToaXe == maToaXe);
    }

    // Phuong an nguoi dung muon luu
    public class PhuongAnLapTauDTO
    {
        public int MaChuyenTau { get; set; }

        // PhienBan cua DoanTau luc mo man hinh (null = luc do chuyen chua co doan tau)
        public byte[]? PhienBan { get; set; }

        public int? MaDauMayChinh { get; set; }
        public int? MaDauMayDay { get; set; }
        public List<ToaTrongDoanDTO> DanhSachToa { get; set; } = new();
    }

    // =========================================================================
    // KET QUA KIEM TRA
    // =========================================================================

    public static class MucDoKiemTra
    {
        public const string Dat = "DAT";
        public const string CanhBao = "CANH_BAO";
        public const string Loi = "LOI";
    }

    public class HangMucKiemTraDTO
    {
        public string Ten { get; set; } = "";
        public string MucDo { get; set; } = MucDoKiemTra.Dat;
        public string NoiDung { get; set; } = "";

        public static HangMucKiemTraDTO Dat(string ten, string noiDung) => new() { Ten = ten, MucDo = MucDoKiemTra.Dat, NoiDung = noiDung };
        public static HangMucKiemTraDTO CanhBao(string ten, string noiDung) => new() { Ten = ten, MucDo = MucDoKiemTra.CanhBao, NoiDung = noiDung };
        public static HangMucKiemTraDTO Loi(string ten, string noiDung) => new() { Ten = ten, MucDo = MucDoKiemTra.Loi, NoiDung = noiDung };
    }

    // Khu doan k = tu ga dung k toi ga dung k + 1 (k tinh tu 0). Chuyen chua co lich dung ga: 1 khu doan.
    public class KhuDoanDTO
    {
        public int ViTri { get; set; }
        public string TuGaCode { get; set; } = "";
        public string DenGaCode { get; set; } = "";
        public List<int> MaToaXe { get; set; } = new();
        public int SoToa => MaToaXe.Count;
        public decimal ChieuDaiM { get; set; }       // ke ca dau may keo chinh
        public decimal TrongLuongTan { get; set; }   // toa toan tai, chua tinh dau may
        public bool CoLoi { get; set; }

        public string Ten => TuGaCode.Length > 0 ? $"{TuGaCode} → {DenGaCode}" : "Toàn hành trình";
    }

    // Chang = cac khu doan lien tiep co cung bo toa (cung thu tu)
    public class ChangDTO
    {
        public int TuViTri { get; set; }
        public int DenViTri { get; set; }
        public string Ten { get; set; } = "";
        public List<int> MaToaXe { get; set; } = new();
        public int SoToa => MaToaXe.Count;
        public decimal ChieuDaiM { get; set; }
        public decimal TrongLuongTan { get; set; }
    }

    public static class ViTriNhomToa
    {
        public const string Khong = "KHONG";
        public const string Dau = "DAU";      // ngay sau dau may
        public const string Cuoi = "CUOI";    // cuoi doan
        public const string Sai = "SAI";      // chen giua doan / khong lien khoi
    }

    // Tac nghiep tai mot ga trung gian: cat nhom toa khoi doan den, noi nhom toa vao doan roi ga
    public class TacNghiepGaDTO
    {
        public int ViTri { get; set; }
        public string MaGaCode { get; set; } = "";
        public string TenGa { get; set; } = "";
        public int SoPhutDo { get; set; }
        public List<int> Cat { get; set; } = new();
        public List<int> Noi { get; set; } = new();
        public string ViTriCat { get; set; } = ViTriNhomToa.Khong;
        public string ViTriNoi { get; set; } = ViTriNhomToa.Khong;
        public bool DuThoiGian { get; set; }

        public bool DungViTri => ViTriCat != ViTriNhomToa.Sai && ViTriNoi != ViTriNhomToa.Sai;
        public bool CoLoi => !DuThoiGian || !DungViTri;

        // "−2 +3"
        public string NhanThayDoi
        {
            get
            {
                var phan = new List<string>();
                if (Cat.Count > 0) phan.Add($"−{Cat.Count}");
                if (Noi.Count > 0) phan.Add($"+{Noi.Count}");
                return string.Join(" ", phan);
            }
        }
    }

    public class KetQuaKiemTraLapTauDTO
    {
        public List<KhuDoanDTO> KhuDoan { get; set; } = new();
        public List<ChangDTO> Chang { get; set; } = new();
        public List<TacNghiepGaDTO> TacNghiep { get; set; } = new();
        public List<HangMucKiemTraDTO> HangMuc { get; set; } = new();

        // Vi tri ga dung (0..n-1) co doan tau dai hon gioi han duong ray tai ga
        public List<int> GaVuotChieuDai { get; set; } = new();

        // Gia tri LON NHAT tren cac khu doan = DoanTau.TongSoToa / TongChieuDaiM / TongTrongLuongTan
        public int SoToaLonNhat { get; set; }
        public decimal ChieuDaiLonNhatM { get; set; }
        public decimal TrongLuongLonNhatTan { get; set; }
        public string ChangDaiNhat { get; set; } = "";
        public string ChangNangNhat { get; set; } = "";

        // "NTR +5 · DAN −2"; rong = khong cat / noi doc duong
        public string MoTaCatNoi { get; set; } = "";
        public bool CoCatNoi => MoTaCatNoi.Length > 0;

        public int SoLoi => HangMuc.Count(h => h.MucDo == MucDoKiemTra.Loi);
        public int SoCanhBao => HangMuc.Count(h => h.MucDo == MucDoKiemTra.CanhBao);
        public bool CoLoi => SoLoi > 0;
    }

    // Ket qua sau khi LapTauService.LuuPhuongAn ghi xong
    public class KetQuaLuuLapTauDTO
    {
        public int MaDoanTau { get; set; }
        public bool LaLapMoi { get; set; }
        public bool KhongThayDoi { get; set; }
        public bool DaHuyDuyetAnToan { get; set; }
        public int SoToaTrongPhuongAn { get; set; }
        public int TongSoToa { get; set; }
        public decimal TongChieuDaiM { get; set; }
        public decimal TongTrongLuongTan { get; set; }
        public string MoTaCatNoi { get; set; } = "";
    }

    // Loi nhap lieu cua mot truong tren dialog (Truong = ten truong, vd "SoHieu")
    public record LoiNhapLieuDTO(string Truong, string ThongDiep);
}
