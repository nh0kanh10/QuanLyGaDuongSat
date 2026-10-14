namespace DTO.NhanSu
{

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

        // Quyet dinh 11/10 (Q4): phieu kiem tra len ban chi ghi tu 3 gio truoc gio nhan ban
        public const int SoGioDoSomNhat = 3;

        public const string LaiTau = "LAI_TAU";
        public const string PhuLai = "PHU_LAI";
        public const string TruongTau = "TRUONG_TAU";
        public static readonly string[] CacVaiTro = { LaiTau, PhuLai, TruongTau };

        // Danh muc chuc danh (cot ChucDanh la chu tu do trong CSDL; tu 12/10 ho so chi chon trong
        // danh muc nay - DanhMucDonVi). Ho so cu ngoai danh muc (ma NV_) van doc / hien duoc.
        public static readonly IReadOnlyList<string> ChucDanhGoiY = new[]
        {
            "Lái tàu", "Phụ lái", "Trưởng tàu", "Tiếp viên", "Nhân viên khám xe", "Nhân viên bán vé"
        };

        public static bool LaChucDanhTrongDanhMuc(string? chucDanh) => chucDanh != null && ChucDanhGoiY.Contains(chucDanh);

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

        public static bool LaChucDanhBanLai(string? chucDanh) => chucDanh is "Lái tàu" or "Phụ lái";

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
    public static class DanhMucDonVi
    {
        public static readonly IReadOnlyList<string> DoiLaiTau = new[]
        {
            "Đội lái tàu Hà Nội", "Đội lái tàu Vinh", "Đội lái tàu Đà Nẵng", "Đội lái tàu Sài Gòn"
        };

        public static readonly IReadOnlyList<string> DoanTiepVien = new[]
        {
            "Đoàn tiếp viên Hà Nội", "Đoàn tiếp viên Đà Nẵng", "Đoàn tiếp viên Phương Nam"
        };

        public static readonly IReadOnlyList<string> TramKhamXe = new[] { "Trạm Giáp Bát" };

        // Ten loai don vi cho thong bao; null = chuc danh ngoai danh muc
        public static string? LoaiDonVi(string? chucDanh) => chucDanh switch
        {
            "Lái tàu" or "Phụ lái" => "đội lái tàu",
            "Trưởng tàu" or "Tiếp viên" => "đoàn tiếp viên",
            "Nhân viên khám xe" => "trạm khám xe",
            "Nhân viên bán vé" => "ga",
            _ => null
        };

        // Cac don vi chon duoc cho chuc danh. Ban ve: "Ga <ten ga>" theo danh muc ga (thu tu ly trinh).
        public static IReadOnlyList<string> DonViHopLe(string? chucDanh, IEnumerable<GaNhanSu> ga) => chucDanh switch
        {
            "Lái tàu" or "Phụ lái" => DoiLaiTau,
            "Trưởng tàu" or "Tiếp viên" => DoanTiepVien,
            "Nhân viên khám xe" => TramKhamXe,
            "Nhân viên bán vé" => ga.Select(g => "Ga " + g.TenGa).ToList(),
            _ => Array.Empty<string>()
        };

        public static bool HopLe(string? chucDanh, string? donVi, IEnumerable<GaNhanSu> ga)
            => donVi != null && DonViHopLe(chucDanh, ga).Contains(donVi);
    }

    // Ten cac dieu kien doi chieu khi phan cong (hang cua bang doi chieu, thu tu hien thi)
    public static class DieuKienKip
    {
        public const string ChucDanh = "Chức danh";
        public const string BangLai = "Bằng lái – đầu máy";
        public const string HanKham = "Hạn khám sức khỏe";
        public const string Trung = "Trùng chuyến khác";
        public const string Nghi = "Nghỉ giữa hai ca";
        public const string KiemTra = "Kiểm tra lên ban";
        public const string DonVi = "Đơn vị – chặng";
        public const string TrangThai = "Trạng thái hồ sơ";

        public static readonly string[] ThuTu = { ChucDanh, BangLai, HanKham, Trung, Nghi, KiemTra, DonVi, TrangThai };
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
    // vanhanh.ChuyenTau JOIN MacTauMau, DoanTau, DauMay, DongDauMay) - chi doc
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

        // Chua ket thuc ma gio ve dich ke hoach da qua luc nap (dieu do chua ghi hoan thanh / huy):
        // trigger trung gio van tinh kip cua chuyen nay nen trang van nap va danh dau
        public bool QuaGioVeDich { get; init; }

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

        // Nhan trang thai kem dau "qua gio ve dich" (o luoi chuyen chi du cho nhan ngan)
        public string NhanTrangThaiDayDu => QuaGioVeDich ? $"{NhanTrangThai} · quá giờ về đích" : NhanTrangThai;
        public string NhanTrangThaiNgan => QuaGioVeDich ? "Quá giờ về đích" : NhanTrangThai;
        public string MauTrangThaiHienThi => QuaGioVeDich ? "#B45309" : MauTrangThai;

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

        // 14/10 (migration V20261014_1000): bat buoc, du 18 tuoi. null chi khi dialog chua chon.
        public DateTime? NgaySinh { get; set; }

        // ROWVERSION (migration V20261011_1500): ban ghi luc mo dialog, null = them moi
        public byte[]? PhienBan { get; set; }

        // Tai khoan dang nhap (phanquyen.TaiKhoan.MaNhanVien, chi doc - chuan bi Quan tri Sprint 3).
        // MaNhanVien khong UNIQUE ben TaiKhoan -> lay tai khoan dang hoat dong, dang nhap gan nhat.
        public string? TenDangNhap { get; set; }
        public bool TaiKhoanHoatDong { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }
        public int SoTaiKhoan { get; set; }

        public bool CoTaiKhoan => TenDangNhap != null;

        // Cac lan doi ma (nhansu.LichSuMaNhanVien, migration V20261012_1100), cu nhat truoc
        public IReadOnlyList<LichSuMaNhanVienDTO> LichSuMa { get; set; } = Array.Empty<LichSuMaNhanVienDTO>();
        public IEnumerable<string> CacMaCu => LichSuMa.Select(l => l.MaNVCodeCu);
        public string NhanMaCu => string.Join(", ", LichSuMa.Select(l => $"{l.MaNVCodeCu} ({l.ChucDanhCu.ToLower()}, đến {l.ThoiDiem:dd/MM/yyyy})"));

        public string NhanTaiKhoan
        {
            get
            {
                if (!CoTaiKhoan) return "Chưa có tài khoản";
                string nhan = $"{TenDangNhap} · {(TaiKhoanHoatDong ? "đang hoạt động" : "đã khóa")}";
                nhan += LanDangNhapCuoi is DateTime lan ? $" · đăng nhập {lan:HH:mm dd/MM/yyyy}" : " · chưa đăng nhập";
                if (SoTaiKhoan > 1) nhan += $" (+{SoTaiKhoan - 1} tài khoản khác)";
                return nhan;
            }
        }

        public string MauTaiKhoan => !CoTaiKhoan ? "#94A3B8" : TaiKhoanHoatDong ? "#15803D" : "#B91C1C";

        public string TenHienThi => $"{MaNVCode} · {HoTen}";
        public override string ToString() => TenHienThi;

        public bool LaBanLaiMay => QuyTacKipLai.LaChucDanhBanLai(ChucDanh);
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

    // Mot lan doi ma (doi chuc danh -> ma theo tien to moi). Ma cu khong bao gio cap lai.
    public record LichSuMaNhanVienDTO(string MaNVCodeCu, string MaNVCodeMoi, string ChucDanhCu, string ChucDanhMoi,
                                      string DonViCu, string DonViMoi, DateTime ThoiDiem);

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

        // ROWVERSION (migration V20261011_1500), null = kip moi
        public byte[]? PhienBan { get; set; }

        // Chuyen tau cua kip - gan khi dung anh chup du lieu (DuLieuNhanSu) hoac khi
        // dialog tao kip moi tren chuyen dang chon
        public ChuyenTauNhanSu Chuyen { get; set; } = null!;

        public string TenGaNhanBan => Chuyen?.DiemDung(MaGaNhanBan)?.TenGa ?? "?";
        public string TenGaBanGiao => Chuyen?.DiemDung(MaGaBanGiao)?.TenGa ?? "?";
        public string Chang => $"{TenGaNhanBan} → {TenGaBanGiao}";

        public DateTime GioNhanBan => Chuyen.GioNhanBan(MaGaNhanBan);
        public DateTime GioBanGiao => Chuyen.GioBanGiao(MaGaBanGiao);
        public string KhungGio => $"{GioNhanBan:HH:mm dd/MM} → {GioBanGiao:HH:mm dd/MM}";

        public int ViTriNhan => Chuyen.ViTri(MaGaNhanBan);
        public int ViTriGiao => Chuyen.ViTri(MaGaBanGiao);

        public IEnumerable<int> CacThanhVien => new[] { MaLaiTau, MaPhuLai, MaTruongTau };

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

    // Kip trong CSDL trung gio voi kip sap ghi - doc tren TOAN BANG, cung dieu kien voi trigger
    // trg_PhanCongKipLai_CheckTrungGio (NhanSuRepository.TimKipTrungGio). VaiTro = vi tri o kip
    // sap ghi, VaiTroKipKia = vi tri o kip dang co.
    public record KipTrungGioDTO(string VaiTro, string HoTen, string VaiTroKipKia, bool CungChuyen,
                                 string SoHieuMacTau, DateTime GioXuatPhatKH, DateTime GioVeDichKH,
                                 string TenGaNhanBan, string TenGaBanGiao)
    {
        public string MoTa => CungChuyen
            ? $"{QuyTacKipLai.TenVaiTro(VaiTro)} {HoTen}: đã thuộc kíp {TenGaNhanBan} → {TenGaBanGiao} của chuyến này"
            : $"{QuyTacKipLai.TenVaiTro(VaiTro)} {HoTen}: đang là {QuyTacKipLai.TenVaiTro(VaiTroKipKia).ToLower()} " +
              $"chuyến {SoHieuMacTau} ({GioXuatPhatKH:HH:mm dd/MM} – {GioVeDichKH:HH:mm dd/MM}), khung giờ hai chuyến chồng nhau";
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

        // Chuyen cua phieu (JOIN vanhanh.ChuyenTau + MacTauMau luc doc)
        public string SoHieuMacTau { get; set; } = "?";
        public DateTime NgayXuatPhatChuyen { get; set; }

        public string MaPhieu => $"KT-{MaKiemTra:0000}";
        public string NhanChuyen => SoHieuMacTau == "?" ? "?" : $"{SoHieuMacTau} · {NgayXuatPhatChuyen:dd/MM}";

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
