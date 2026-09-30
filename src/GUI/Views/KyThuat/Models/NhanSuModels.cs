<<<<<<< Updated upstream:src/GUI/Views/KyThuat/Models/NhanSuModels.cs
namespace GUI.Views.KyThuat.Models
=======
﻿using GUI.Helpers;
using System.Text.RegularExpressions;
namespace GUI.ViewModels.KyThuat
>>>>>>> Stashed changes:src/GUI/ViewModels/KyThuat/NhanSuModels.cs
{
    // =========================================================================
    // Cac lop HIEN THI cua phan he Nhan su, To tau & Phan cong ca truc.
    // Ten thuoc tinh trung ten cot schema nhansu (script_tao_csdl_v2.sql) de
    // sau nay doc DataRow that khong phai doi binding.
    // Quy tac R1..R13: PHAN_TICH_NHAN_SU_KIP_LAI.md §3.
    // =========================================================================

    public static class QuyTacKipLai
    {
        // Trung cot tinh toan nhansu.KiemTraSucKhoe.DuDieuKien (R5)
        public const decimal NongDoConChoPhep = 0.00m;
        public const decimal SoGioNghiToiThieu = 8.0m;

        // Kieu cot: NongDoConMgL DECIMAL(3,2), SoGioNghiTruocCa DECIMAL(4,1)
        public const decimal NongDoConGioiHan = 9.99m;
        public const decimal SoGioNghiGioiHan = 999.9m;

        // Quy uoc giao dien (R8)
        public const int SoNgayNhacHanKham = 30;

        public const string LaiTau = "LAI_TAU";
        public const string PhuLai = "PHU_LAI";
        public const string TruongTau = "TRUONG_TAU";
        public static readonly string[] CacVaiTro = { LaiTau, PhuLai, TruongTau };

        public static bool DuDieuKienLenBan(decimal nongDoCon, decimal soGioNghi)
            => nongDoCon == NongDoConChoPhep && soGioNghi >= SoGioNghiToiThieu;

        public static string TenVaiTro(string vaiTro) => vaiTro switch
        {
            LaiTau => "Lái tàu",
            PhuLai => "Phụ lái",
            TruongTau => "Trưởng tàu",
            _ => vaiTro
        };

        // R2: vi tri nao nhan chuc danh do (ChucDanh la chu tu do trong CSDL)
        public static string ChucDanhYeuCau(string vaiTro) => TenVaiTro(vaiTro);

        public static bool CanBangLai(string vaiTro) => vaiTro is LaiTau or PhuLai;

        public static bool LaChucDanhToTau(string? chucDanh)
            => chucDanh is "Lái tàu" or "Phụ lái" or "Trưởng tàu";

        // R3: y het trigger trg_PhanCongKipLai_CheckTrungGio (xet ca khung gio chuyen)
        public static bool TrungKhungGio(ChuyenTauNhanSu a, ChuyenTauNhanSu b)
        {
            if (!a.ConHieuLuc || !b.ConHieuLuc) return false;
            DateTime batDau = a.GioXuatPhatKH > b.GioXuatPhatKH ? a.GioXuatPhatKH : b.GioXuatPhatKH;
            DateTime ketThuc = a.GioVeDichKH < b.GioVeDichKH ? a.GioVeDichKH : b.GioVeDichKH;
            return batDau < ketThuc;
        }

        public static decimal SoGio(DateTime tu, DateTime den)
            => Math.Round((decimal)(den - tu).TotalHours, 1, MidpointRounding.AwayFromZero);
    }

    // -------------------------------------------------------------------------
    // MA NHAN VIEN THEO CHUC DANH (quy uoc giao dien 29/09 - CSDL chi rang buoc
    // MaNVCode VARCHAR(20) NOT NULL UNIQUE).
    // Dang TIENTO_SSS: LT_001 lai tau, PL_001 phu lai, TT_001 truong tau ...;
    // so thu tu dem rieng tung tien to, khong dung lai ma da cap.
    // Doi chuc danh -> cap ma moi (kip, kiem tra, tai khoan noi bang MaNhanVien
    // nen khong anh huong).
    // -------------------------------------------------------------------------
    public static class MaNhanVienTheoChucDanh
    {
        public const string TienToKhac = "NV";

        // Thu tu trong bang = thu tu hien thi (to tau truoc)
        private static readonly (string ChucDanh, string TienTo)[] Bang =
        {
            ("Lái tàu", "LT"), ("Phụ lái", "PL"), ("Trưởng tàu", "TT"), ("Tiếp viên", "TV"),
            ("Nhân viên khám xe", "KX"), ("Nhân viên bán vé", "BV")
        };

        private static readonly Regex DangMa = new(@"^([A-Z]{2})_(\d{3,9})$");

        public const string MoTaQuyUoc =
            "LT lái tàu · PL phụ lái · TT trưởng tàu · TV tiếp viên · KX khám xe · BV bán vé · NV chức danh khác";

        private static int ViTri(string? chucDanh)
        {
            string cd = chucDanh?.Trim() ?? string.Empty;
            for (int i = 0; i < Bang.Length; i++)
                if (string.Equals(Bang[i].ChucDanh, cd, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        public static string TienTo(string? chucDanh)
        {
            int i = ViTri(chucDanh);
            return i >= 0 ? Bang[i].TienTo : TienToKhac;
        }

        // Sap xep theo chuc danh: lai tau, phu lai, truong tau, tiep vien, kham xe, ban ve, khac
        public static int ThuTu(string? chucDanh)
        {
            int i = ViTri(chucDanh);
            return i >= 0 ? i : Bang.Length;
        }

        // Ma dung dang va dung tien to cua chuc danh
        public static bool KhopChucDanh(string? ma, string? chucDanh)
        {
            var m = DangMa.Match(ma ?? string.Empty);
            return m.Success && m.Groups[1].Value == TienTo(chucDanh);
        }

        // Ma ke tiep = so lon nhat da cap cua tien to + 1
        public static string MaKeTiep(string? chucDanh, IEnumerable<string> maDaCap)
        {
            string tienTo = TienTo(chucDanh);
            int lonNhat = 0;
            foreach (string ma in maDaCap)
            {
                var m = DangMa.Match(ma ?? string.Empty);
                if (m.Success && m.Groups[1].Value == tienTo && int.TryParse(m.Groups[2].Value, out int so) && so > lonNhat)
                    lonNhat = so;
            }
            return $"{tienTo}_{lonNhat + 1:000}";
        }
    }

    public enum MucDoVanDe { NghiemTrong = 0, CanhBao = 1, LuuY = 2 }

    public static class MucDoHienThi
    {
        public static string Nhan(MucDoVanDe m) => m switch
        {
            MucDoVanDe.NghiemTrong => "Nghiêm trọng",
            MucDoVanDe.CanhBao => "Cảnh báo",
            _ => "Lưu ý"
        };

        public static string Mau(MucDoVanDe m) => m switch
        {
            MucDoVanDe.NghiemTrong => "#B91C1C",
            MucDoVanDe.CanhBao => "#B45309",
            _ => "#64748B"
        };
    }

    // -------------------------------------------------------------------------
    // GA, DIEM DUNG, CHUYEN TAU (tham chieu: hatang.Ga, vanhanh.LichDungGa,
    // vanhanh.ChuyenTau JOIN MacTauMau, DoanTau, DauMay, DongDauMay)
    // -------------------------------------------------------------------------
    public record GaNhanSu(int MaGa, string MaGaCode, string TenGa)
    {
        public override string ToString() => TenGa;
    }

    public record DiemDungNhanSu(GaNhanSu Ga, int ThuTuDung, DateTime GioDenKeHoach, DateTime GioDiKeHoach)
    {
        public int MaGa => Ga.MaGa;
        public string TenGa => Ga.TenGa;
        public override string ToString() => Ga.TenGa;
    }

    public class ChuyenTauNhanSu
    {
        public int MaChuyenTau { get; init; }
        public string SoHieuMacTau { get; init; } = "";
        public string LoaiTau { get; init; } = "";
        public DateTime NgayXuatPhat { get; init; }
        public DateTime GioXuatPhatKH { get; init; }
        public DateTime GioVeDichKH { get; init; }
        public string TrangThai { get; init; } = "";
        public GaNhanSu GaDi { get; init; } = null!;
        public GaNhanSu GaDen { get; init; } = null!;

        // Dau may chinh cua doan tau; null = chuyen chua lap doan tau
        public string? SoHieuDauMay { get; init; }
        public string? MaDongDauMay { get; init; }

        // Chuyen chua co lich dung ga: chi 2 diem (ga di, ga den) lay gio cua chuyen
        public bool CoLichDungGa { get; init; }
        public IReadOnlyList<DiemDungNhanSu> LichDung { get; init; } = Array.Empty<DiemDungNhanSu>();

        public string HanhTrinh => $"{GaDi.TenGa} → {GaDen.TenGa}";
        public string TenHienThi => $"{SoHieuMacTau} · {NgayXuatPhat:dd/MM/yyyy} · {HanhTrinh}";
        public override string ToString() => TenHienThi;

        public bool ConHieuLuc => TrangThai is not ("HOAN_THANH" or "DA_HUY");
        public bool DaXuatPhat => TrangThai is "DANG_CHAY" or "TRE_GIO";

        public string NhanLoaiTau => LoaiTau switch
        {
            "TAU_NHANH" => "Tàu khách nhanh",
            "TAU_CHO" => "Tàu chợ",
            "TAU_HANG" => "Tàu hàng",
            _ => LoaiTau
        };

        public string NhanTrangThai => TrangThai switch
        {
            "DA_LEN_LICH" => "Đã lên lịch",
            "SAN_SANG" => "Sẵn sàng",
            "DANG_CHAY" => "Đang chạy",
            "TRE_GIO" => "Trễ giờ",
            "HOAN_THANH" => "Hoàn thành",
            "DA_HUY" => "Đã hủy",
            _ => TrangThai
        };

        public string MauTrangThai => TrangThai switch
        {
            "DANG_CHAY" => "#1D4ED8",
            "TRE_GIO" => "#B45309",
            "SAN_SANG" => "#15803D",
            "HOAN_THANH" or "DA_HUY" => "#64748B",
            _ => "#334155"
        };

        public string NhanDauMay => SoHieuDauMay == null ? "Chưa lập đoàn tàu" : $"{SoHieuDauMay} ({MaDongDauMay})";

        public int ViTri(int maGa)
        {
            for (int i = 0; i < LichDung.Count; i++)
                if (LichDung[i].MaGa == maGa) return i;
            return -1;
        }

        public DiemDungNhanSu? DiemDung(int maGa) => LichDung.FirstOrDefault(d => d.MaGa == maGa);

        // Gio nhan ban = gio tau chay tai ga nhan ban; gio ban giao = gio tau den ga ban giao
        public DateTime GioNhanBan(int maGa) => DiemDung(maGa)?.GioDiKeHoach ?? GioXuatPhatKH;
        public DateTime GioBanGiao(int maGa) => DiemDung(maGa)?.GioDenKeHoach ?? GioVeDichKH;
    }

    // -------------------------------------------------------------------------
    // 1. NHAN VIEN (nhansu.NhanVien)
    // -------------------------------------------------------------------------
    public class NhanVienHienThi
    {
        public const string SanSang = "SAN_SANG";
        public const string DangLam = "DANG_LAM";
        public const string NghiNgoi = "NGHI_NGOI";
        public const string DaNghiViec = "DA_NGHI_VIEC";

        public int MaNhanVien { get; set; }
        public string MaNVCode { get; set; } = "";
        public string HoTen { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public string ChucDanh { get; set; } = "";
        public string? HangBangLai { get; set; }
        public DateTime HanKhamSucKhoe { get; set; }
        public string DonViChuQuan { get; set; } = "";
        public string TrangThai { get; set; } = SanSang;
        public DateTime NgayTao { get; set; }

        public bool LaThayDoiTam { get; set; }

        public string TenHienThi => $"{MaNVCode} · {HoTen}";
        public override string ToString() => TenHienThi;

        public bool LaBanLaiMay => ChucDanh is "Lái tàu" or "Phụ lái";
        public bool LaToTau => QuyTacKipLai.LaChucDanhToTau(ChucDanh);
        public string NhanBangLai => string.IsNullOrEmpty(HangBangLai) ? "—" : HangBangLai;

        public string NhanTrangThai => NhanTrangThaiCua(TrangThai);
        public string MauTrangThai => MauTrangThaiCua(TrangThai);

        public static string NhanTrangThaiCua(string trangThai) => trangThai switch
        {
            SanSang => "Sẵn sàng",
            DangLam => "Đang làm nhiệm vụ",
            NghiNgoi => "Nghỉ ngơi",
            DaNghiViec => "Đã nghỉ việc",
            _ => trangThai
        };

        public static string MauTrangThaiCua(string trangThai) => trangThai switch
        {
            SanSang => "#15803D",
            DangLam => "#1D4ED8",
            NghiNgoi => "#B45309",
            _ => "#64748B"
        };

        // --- Han kham suc khoe (R8) ---
        public int SoNgayConHanKham => (HanKhamSucKhoe.Date - DateTime.Today).Days;

        public bool HetHanKham => SoNgayConHanKham < 0;
        public bool SapHetHanKham => SoNgayConHanKham is >= 0 and <= QuyTacKipLai.SoNgayNhacHanKham;

        public string MoTaHanKham => SoNgayConHanKham switch
        {
            < 0 => $"Quá hạn {-SoNgayConHanKham} ngày",
            0 => "Hết hạn hôm nay",
            _ => $"Còn {SoNgayConHanKham} ngày"
        };

        public string MauHanKham => HetHanKham ? "#B91C1C" : SapHetHanKham ? "#B45309" : "#334155";

        public NhanVienHienThi SaoChep() => (NhanVienHienThi)MemberwiseClone();
    }

    // -------------------------------------------------------------------------
    // 2. PHAN CONG KIP LAI (nhansu.PhanCongKipLai) - 1 kip tren 1 chang
    // -------------------------------------------------------------------------
    public class PhanCongKipHienThi
    {
        public const string DaPhanCong = "DA_PHAN_CONG";
        public const string DangThucHien = "DANG_THUC_HIEN";
        public const string HoanThanh = "HOAN_THANH";

        public int MaPhanCong { get; set; }
        public int MaChuyenTau { get; set; }
        public int MaLaiTau { get; set; }
        public int MaPhuLai { get; set; }
        public int MaTruongTau { get; set; }
        public int MaGaNhanBan { get; set; }
        public int MaGaBanGiao { get; set; }
        public string TrangThai { get; set; } = DaPhanCong;

        public bool LaThayDoiTam { get; set; }

        // Chuyen tau la du lieu tham chieu tinh (khi noi CSDL: JOIN vanhanh.ChuyenTau)
        public ChuyenTauNhanSu Chuyen => DanhMucMauNhanSu.TimChuyen(MaChuyenTau)!;

        public string TenGaNhanBan => DanhMucMauNhanSu.TimGa(MaGaNhanBan)?.TenGa ?? "?";
        public string TenGaBanGiao => DanhMucMauNhanSu.TimGa(MaGaBanGiao)?.TenGa ?? "?";
        public string Chang => $"{TenGaNhanBan} → {TenGaBanGiao}";

        public DateTime GioNhanBan => Chuyen.GioNhanBan(MaGaNhanBan);
        public DateTime GioBanGiao => Chuyen.GioBanGiao(MaGaBanGiao);
        public string KhungGio => $"{GioNhanBan:HH:mm dd/MM} → {GioBanGiao:HH:mm dd/MM}";

        public int ViTriNhan => Chuyen.ViTri(MaGaNhanBan);
        public int ViTriGiao => Chuyen.ViTri(MaGaBanGiao);

        public int MaNhanVienTheoVaiTro(string vaiTro) => vaiTro switch
        {
            QuyTacKipLai.LaiTau => MaLaiTau,
            QuyTacKipLai.PhuLai => MaPhuLai,
            _ => MaTruongTau
        };

        public void GanNhanVien(string vaiTro, int maNhanVien)
        {
            switch (vaiTro)
            {
                case QuyTacKipLai.LaiTau: MaLaiTau = maNhanVien; break;
                case QuyTacKipLai.PhuLai: MaPhuLai = maNhanVien; break;
                default: MaTruongTau = maNhanVien; break;
            }
        }

        public bool CoNhanVien(int maNhanVien)
            => MaLaiTau == maNhanVien || MaPhuLai == maNhanVien || MaTruongTau == maNhanVien;

        public string? VaiTroCua(int maNhanVien)
            => MaLaiTau == maNhanVien ? QuyTacKipLai.LaiTau
             : MaPhuLai == maNhanVien ? QuyTacKipLai.PhuLai
             : MaTruongTau == maNhanVien ? QuyTacKipLai.TruongTau
             : null;

        public string NhanTrangThai => NhanTrangThaiCua(TrangThai);

        public static string NhanTrangThaiCua(string trangThai) => trangThai switch
        {
            DaPhanCong => "Đã phân công",
            DangThucHien => "Đang thực hiện",
            HoanThanh => "Đã bàn giao",
            _ => trangThai
        };

        public PhanCongKipHienThi SaoChep() => (PhanCongKipHienThi)MemberwiseClone();
    }

    // -------------------------------------------------------------------------
    // 3. KIEM TRA LEN BAN (nhansu.KiemTraSucKhoe)
    // -------------------------------------------------------------------------
    public class KiemTraLenBanHienThi
    {
        public int MaKiemTra { get; set; }
        public int MaNhanVien { get; set; }
        public int MaChuyenTau { get; set; }
        public decimal NongDoConMgL { get; set; }
        public decimal SoGioNghiTruocCa { get; set; }
        public DateTime ThoiDiemKiemTra { get; set; }

        public bool LaThayDoiTam { get; set; }

        // Cot tinh toan PERSISTED
        public bool DuDieuKien => QuyTacKipLai.DuDieuKienLenBan(NongDoConMgL, SoGioNghiTruocCa);

        // --- Hien thi (gan tu ho so moi lan nap) ---
        public string MaNVCode { get; set; } = "";
        public string HoTen { get; set; } = "";
        public string ChucDanh { get; set; } = "";

        public void GanNhanVien(NhanVienHienThi? nv)
        {
            MaNVCode = nv?.MaNVCode ?? "?";
            HoTen = nv?.HoTen ?? "(không rõ)";
            ChucDanh = nv?.ChucDanh ?? "";
        }

        public string MaPhieu => MaKiemTra > 0 ? $"KT-{MaKiemTra:0000}" : $"KT-M{-MaKiemTra:00}";

        public string SoHieuMacTau => DanhMucMauNhanSu.TimChuyen(MaChuyenTau)?.SoHieuMacTau ?? "?";
        public string NhanChuyen
        {
            get
            {
                var ct = DanhMucMauNhanSu.TimChuyen(MaChuyenTau);
                return ct == null ? "?" : $"{ct.SoHieuMacTau} · {ct.NgayXuatPhat:dd/MM}";
            }
        }

        public bool ConDat => NongDoConMgL == QuyTacKipLai.NongDoConChoPhep;
        public bool NghiDu => SoGioNghiTruocCa >= QuyTacKipLai.SoGioNghiToiThieu;

        public string MauNongDoCon => ConDat ? "#0F172A" : "#B91C1C";
        public string MauGioNghi => NghiDu ? "#0F172A" : "#B91C1C";

        public string NhanKetLuan => DuDieuKien ? "Đủ điều kiện" : "Không đủ điều kiện";
        public string MauKetLuan => DuDieuKien ? "#15803D" : "#B91C1C";

        public string LyDoKhongDat
        {
            get
            {
                var ds = new List<string>();
                if (!ConDat) ds.Add($"cồn {NongDoConMgL:0.00} mg/L");
                if (!NghiDu) ds.Add($"nghỉ {SoGioNghiTruocCa:0.0} giờ");
                return string.Join(", ", ds);
            }
        }
    }
}
