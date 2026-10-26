using BUS.Services;
using DTO.BaoTri;
using GUI.Helpers;
using System.Windows;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.ViewModels.KyThuat
{
    // =========================================================================
    // Cac lop HIEN THI cua phan he Bao tri & sua chua.
    // Ten thuoc tinh trung ten cot trong schema baotri (giu tu giai doan giao dien de khong doi binding).
    // Tu 24/10 (back-end): du lieu doc tu CSDL qua BaoTriService (DTO.BaoTri) roi doi sang cac lop o day
    // (BaoTriDuLieu); nguong / cong thuc / chu ky goi BUS.Services.QuyTacBaoTri - mot nguon duy nhat cho
    // giao dien, Service va KiemTraXuatBenService. O day chi them mau, bieu tuong, chu hien thi.
    // =========================================================================

    // -------------------------------------------------------------------------
    // NGUONG KHAM XE - trung cot tinh toan baotri.KhamXeKyThuat.DuDieuKienXuatBen
    // -------------------------------------------------------------------------
    public static class NguongKhamXe
    {
        public const decimal ApLucHamToiThieuBar = QuyTacBaoTri.ApLucHamToiThieuBar;
        public const decimal DoSutApToiDaBarPhut = QuyTacBaoTri.DoSutApToiDaBarPhut;
        public const decimal ApLucHamGioiHanBar = QuyTacBaoTri.ApLucHamGioiHanBar;
        public const decimal DoSutApGioiHanBarPhut = QuyTacBaoTri.DoSutApGioiHanBarPhut;

        public static bool DuDieuKienXuatBen(decimal apLuc, decimal sutAp, bool daCapNuoc, bool daXaVeSinh)
            => QuyTacBaoTri.DuDieuKienXuatBen(apLuc, sutAp, daCapNuoc, daXaVeSinh);

        // Bon hang muc doi chieu; gia tri null = chua nhap (dung cho xem truoc trong dialog).
        // Dinh dang "0.0##" / "0.00#" de khong lam tron so dang go do (4.85 khong hien thanh 4.9).
        public static List<HangMucKhamXe> TaoHangMuc(decimal? apLuc, decimal? sutAp, bool daCapNuoc, bool daXaVeSinh)
        {
            return new List<HangMucKhamXe>
            {
                new("Áp lực hãm gió", apLuc.HasValue ? $"{apLuc:0.0##} bar" : "Chưa nhập",
                    $"≥ {ApLucHamToiThieuBar:0.0} bar", apLuc.HasValue ? apLuc >= ApLucHamToiThieuBar : null),
                new("Độ sụt áp ống hãm", sutAp.HasValue ? $"{sutAp:0.00#} bar/phút" : "Chưa nhập",
                    $"≤ {DoSutApToiDaBarPhut:0.00} bar/phút", sutAp.HasValue ? sutAp <= DoSutApToiDaBarPhut : null),
                new("Cấp nước sinh hoạt", daCapNuoc ? "Đã cấp" : "Chưa cấp", "Bắt buộc", daCapNuoc),
                new("Xả két vệ sinh", daXaVeSinh ? "Đã xả" : "Chưa xả", "Bắt buộc", daXaVeSinh)
            };
        }
    }

    // Mot dong trong bang doi chieu hang muc kham xe
    public record HangMucKhamXe(string TenHangMuc, string GiaTriDo, string YeuCau, bool? Dat)
    {
        public string MauChu => Dat switch { true => "#15803D", false => "#B91C1C", _ => "#94A3B8" };
        public string MauNen => Dat switch { true => "#F0FDF4", false => "#FEF2F2", _ => "#F8FAFC" };
        public string MauVien => Dat switch { true => "#BBF7D0", false => "#FECACA", _ => "#E2E8F0" };
        public string NhanKetQua => Dat switch { true => "Đạt", false => "Không đạt", _ => "Chờ nhập" };

        public SymbolRegular BieuTuong => Dat switch
        {
            true => SymbolRegular.CheckmarkCircle24,
            false => SymbolRegular.DismissCircle24,
            _ => SymbolRegular.Circle24
        };
    }

    // -------------------------------------------------------------------------
    // 1. DOAN TAU (vanhanh.DoanTau JOIN ChuyenTau, MacTauMau, Ga, DauMay)
    // -------------------------------------------------------------------------
    public class DoanTauKhamXe
    {
        public DoanTauKhamXe(DoanTauBaoTriDTO goc) { Goc = goc; }

        public DoanTauBaoTriDTO Goc { get; }

        public int MaDoanTau => Goc.MaDoanTau;
        public int MaChuyenTau => Goc.MaChuyenTau;
        public string SoHieuMacTau => Goc.SoHieuMacTau;
        public string LoaiTau => Goc.LoaiTau;
        public DateTime NgayXuatPhat => Goc.NgayXuatPhat;
        public DateTime GioXuatPhatKH => Goc.GioXuatPhatKH;
        public string TenGaDi => Goc.TenGaDi;
        public string TenGaDen => Goc.TenGaDen;
        public int MaDauMayChinh => Goc.MaDauMayChinh;
        public int? MaDauMayDay => Goc.MaDauMayDay;
        public string SoHieuDauMayChinh => Goc.SoHieuDauMayChinh;
        public string? SoHieuDauMayDay => Goc.SoHieuDauMayDay;
        public int TongSoToa => Goc.TongSoToa;
        public decimal TongChieuDaiM => Goc.TongChieuDaiM;
        public decimal TongTrongLuongTan => Goc.TongTrongLuongTan;
        public bool DaDuyetAnToan => Goc.DaDuyetAnToan;
        public string TrangThaiChuyen => Goc.TrangThaiChuyen;
        public bool DaXuatPhat => Goc.DaXuatPhat;
        public bool DaKetThuc => Goc.DaKetThuc;

        public string HanhTrinh => Goc.HanhTrinh;
        public string TenHienThi => $"{SoHieuMacTau} · {NgayXuatPhat:dd/MM/yyyy} · {HanhTrinh}";

        public string NhanDauMay => string.IsNullOrEmpty(SoHieuDauMayDay)
            ? SoHieuDauMayChinh
            : $"{SoHieuDauMayChinh} + {SoHieuDauMayDay} (đẩy)";

        public string NhanTrangThaiChuyen => TrangThaiChuyen switch
        {
            "DA_LEN_LICH" => "Đã lên lịch",
            "SAN_SANG" => "Sẵn sàng",
            "DANG_CHAY" => "Đang chạy",
            "TRE_GIO" => "Trễ giờ",
            "HOAN_THANH" => "Hoàn thành",
            "DA_HUY" => "Đã hủy",
            _ => TrangThaiChuyen
        };

        // Ten doc cho UI Automation / trinh doc man hinh khi la muc cua ComboBox
        public override string ToString() => TenHienThi;
    }

    // Tai khoan ky phieu (phanquyen.TaiKhoan vai tro KY_THUAT) kem don vi de hien thi
    public record TaiKhoanMau(int MaTaiKhoan, string TenDangNhap, string HoTenHienThi, string DonVi)
    {
        public string TenHienThi => DonVi.Length > 0 ? $"{HoTenHienThi} — {DonVi}" : HoTenHienThi;
        public override string ToString() => TenHienThi;
    }

    // -------------------------------------------------------------------------
    // 2. BIEN BAN KHAM XE KY THUAT (baotri.KhamXeKyThuat) - chi lap moi, khong sua (quyet dinh 24/10)
    // -------------------------------------------------------------------------
    public class KhamXeHienThi
    {
        // --- Cot cua bang ---
        public int MaKhamXe { get; set; }
        public int MaDoanTau { get; set; }
        public int MaNguoiKham { get; set; }
        public decimal ApLucHamBar { get; set; }
        public decimal DoSutApBarPhut { get; set; }
        public bool DaCapNuoc { get; set; }
        public bool DaXaVeSinh { get; set; }
        public string GhiChuKyThuat { get; set; } = "";
        public DateTime ThoiDiemKham { get; set; }
        public decimal ChiPhi { get; set; }      // VND, migration V20261024_1000

        // --- Cot JOIN de hien thi ---
        public string SoHieuMacTau { get; set; } = "";
        public DateTime NgayXuatPhat { get; set; }
        public string TenGaDi { get; set; } = "";
        public string TenGaDen { get; set; } = "";
        public string SoHieuDauMayChinh { get; set; } = "";
        public string TenNguoiKham { get; set; } = "";

        // Cot tinh toan PERSISTED trong CSDL: tinh lai dung cong thuc
        public bool DuDieuKienXuatBen
            => NguongKhamXe.DuDieuKienXuatBen(ApLucHamBar, DoSutApBarPhut, DaCapNuoc, DaXaVeSinh);

        public bool DatApLucHam => ApLucHamBar >= NguongKhamXe.ApLucHamToiThieuBar;
        public bool DatSutAp => DoSutApBarPhut <= NguongKhamXe.DoSutApToiDaBarPhut;

        public int SoHangMucDat
            => (DatApLucHam ? 1 : 0) + (DatSutAp ? 1 : 0) + (DaCapNuoc ? 1 : 0) + (DaXaVeSinh ? 1 : 0);

        public string MaBienBan => $"BB-{MaKhamXe:0000}";
        public string HanhTrinh => $"{TenGaDi} → {TenGaDen}";

        public string MauApLuc => DatApLucHam ? "#15803D" : "#B91C1C";
        public string MauSutAp => DatSutAp ? "#15803D" : "#B91C1C";

        // Tom tat cac hang muc khong dat (hien trong luoi va the doan tau)
        public string TomTatLoi => QuyTacBaoTri.TomTatLoi(ToDTO());

        public List<HangMucKhamXe> HangMuc
            => NguongKhamXe.TaoHangMuc(ApLucHamBar, DoSutApBarPhut, DaCapNuoc, DaXaVeSinh);

        public void GanDoanTau(DoanTauKhamXe dt)
        {
            MaDoanTau = dt.MaDoanTau;
            SoHieuMacTau = dt.SoHieuMacTau;
            NgayXuatPhat = dt.NgayXuatPhat;
            TenGaDi = dt.TenGaDi;
            TenGaDen = dt.TenGaDen;
            SoHieuDauMayChinh = dt.SoHieuDauMayChinh;
        }

        public KhamXeDTO ToDTO() => new()
        {
            MaKhamXe = MaKhamXe, MaDoanTau = MaDoanTau, MaNguoiKham = MaNguoiKham, ApLucHamBar = ApLucHamBar,
            DoSutApBarPhut = DoSutApBarPhut, DaCapNuoc = DaCapNuoc, DaXaVeSinh = DaXaVeSinh,
            GhiChuKyThuat = GhiChuKyThuat, ThoiDiemKham = ThoiDiemKham, ChiPhi = ChiPhi
        };
    }

    // -------------------------------------------------------------------------
    // 3. TINH TRANG XUAT BEN THEO DOAN TAU (the tom tat phia tren luoi)
    //    Dua tren bien ban MOI NHAT cua moi doan tau.
    // -------------------------------------------------------------------------
    public class TinhTrangDoanTauHienThi
    {
        public DoanTauKhamXe DoanTau { get; init; } = null!;
        public KhamXeHienThi? BienBanMoiNhat { get; init; }
        public int SoLanKham { get; init; }
        public bool DangChon { get; set; }

        // DAT | KHONG_DAT | CHUA_KHAM
        public string TrangThai => BienBanMoiNhat == null ? "CHUA_KHAM"
                                 : BienBanMoiNhat.DuDieuKienXuatBen ? "DAT" : "KHONG_DAT";

        public string NhanTrangThai => TrangThai switch
        {
            "DAT" => "Đủ ĐK xuất bến",
            "KHONG_DAT" => "Không đủ ĐK",
            _ => "Chưa khám"
        };

        public string MoTa => BienBanMoiNhat == null
            ? $"Xuất phát {DoanTau.GioXuatPhatKH:HH:mm dd/MM}"
            : BienBanMoiNhat.DuDieuKienXuatBen
                ? $"Khám lúc {BienBanMoiNhat.ThoiDiemKham:HH:mm dd/MM}"
                : BienBanMoiNhat.TomTatLoi;

        public string MauChu => TrangThai switch { "DAT" => "#15803D", "KHONG_DAT" => "#B91C1C", _ => "#B45309" };
        public string MauNen => TrangThai switch { "DAT" => "#F0FDF4", "KHONG_DAT" => "#FEF2F2", _ => "#FFFBEB" };
        public string MauVien => DangChon ? "#003B73"
                               : TrangThai switch { "DAT" => "#BBF7D0", "KHONG_DAT" => "#FECACA", _ => "#FDE68A" };
        public double DoDayVien => DangChon ? 2 : 1;

        public SymbolRegular BieuTuong => TrangThai switch
        {
            "DAT" => SymbolRegular.CheckmarkCircle24,
            "KHONG_DAT" => SymbolRegular.DismissCircle24,
            _ => SymbolRegular.Clock24
        };
    }

    // =========================================================================
    // 4. NHIEN LIEU (baotri.NhatKyCapNhienLieu)
    // SFC = so lit x 10.000 / (tan keo x km), dinh muc theo dong may (DongDauMay.DinhMucSFC, tham khao).
    // =========================================================================
    public static class DinhMucNhienLieu
    {
        public const decimal HeSoQuyDoi = QuyTacBaoTri.HeSoQuyDoi;
        public const decimal DonGiaMacDinhVndLit = QuyTacBaoTri.DonGiaGoiY;
        public const decimal NguongSatDinhMuc = QuyTacBaoTri.NguongSatDinhMuc;
        public const decimal ThangHienThiToiDa = 150m;    // thanh tien do ve tu 0 den 150% dinh muc
        public const decimal SfcGioiHan = QuyTacBaoTri.SfcGioiHan;

        public static decimal? TinhSfc(decimal soLit, decimal tanKeo, decimal cuLyKm) => QuyTacBaoTri.TinhSfc(soLit, tanKeo, cuLyKm);
        public static decimal TinhTyLe(decimal sfc, decimal dinhMuc) => QuyTacBaoTri.TinhTyLe(sfc, dinhMuc);
        public static string XepMuc(decimal tyLe) => QuyTacBaoTri.XepMuc(tyLe);

        public static string MauChu(string muc) => muc switch { "VUOT" => "#B91C1C", "SAT" => "#B45309", "DAT" => "#15803D", _ => "#64748B" };
        public static string MauNen(string muc) => muc switch { "VUOT" => "#FEF2F2", "SAT" => "#FFFBEB", "DAT" => "#F0FDF4", _ => "#F8FAFC" };
        public static string MauVien(string muc) => muc switch { "VUOT" => "#FECACA", "SAT" => "#FDE68A", "DAT" => "#BBF7D0", _ => "#E2E8F0" };

        public static string NhanMuc(string muc) => muc switch
        {
            "VUOT" => "Vượt định mức",
            "SAT" => "Sát định mức",
            "DAT" => "Trong định mức",
            _ => "Chưa có số liệu"
        };

        // Cap cot star-sizing cho thanh 0 - 150% dinh muc
        public static (GridLength DaDung, GridLength ConLai) TachThanh(decimal tyLe)
        {
            double v = (double)Math.Clamp(tyLe, 0m, ThangHienThiToiDa);
            return (new GridLength(v, GridUnitType.Star), new GridLength((double)ThangHienThiToiDa - v, GridUnitType.Star));
        }
    }

    // Dau may (phuongtien.DauMay JOIN DongDauMay) - dung chung cho cap dau, bao duong, canh bao
    public record DauMayCapDau(int MaDauMay, string SoHieuDauMay, string MaDongCode, string DonViQuanLy,
                               string TrangThai, int DungTichBonDauLit, decimal DinhMucSfc, int CongSuatHP,
                               decimal SoKmTichLuy)
    {
        public bool NgungKhaiThac => TrangThai == PhuongTienService.NgungKhaiThac;
        public string TenHienThi => $"{SoHieuDauMay} — {DonViQuanLy}";
        public override string ToString() => TenHienThi;
    }

    public class CapNhienLieuHienThi
    {
        // --- Cot cua bang ---
        public int MaNhatKyDau { get; set; }
        public int MaDauMay { get; set; }
        public string NoiCapDau { get; set; } = "";
        public decimal SoLitTraNap { get; set; }
        public decimal TrongLuongKeoTan { get; set; }
        public decimal CuLyChayKm { get; set; }
        public decimal SuatTieuHaoSFC { get; set; }
        public bool CanhBaoVuotMuc { get; set; }
        public int? MaNguoiCap { get; set; }
        public DateTime ThoiDiemBomDau { get; set; }
        public decimal DonGiaVndLit { get; set; }
        public decimal ChiPhi { get; set; }          // cot tinh = so lit x don gia
        public byte[]? PhienBan { get; set; }

        // --- Cot JOIN de hien thi ---
        public string SoHieuDauMay { get; set; } = "";
        public string MaDongCode { get; set; } = "";
        public int DungTichBonDauLit { get; set; }
        public decimal DinhMucSfc { get; set; }
        public string TenNguoiCap { get; set; } = "";

        public decimal TyLeDinhMuc => DinhMucNhienLieu.TinhTyLe(SuatTieuHaoSFC, DinhMucSfc);
        public string MucDo => DinhMucNhienLieu.XepMuc(TyLeDinhMuc);

        public string MaPhieu => $"NL-{MaNhatKyDau:0000}";
        public string NhanCongTac => $"{TrongLuongKeoTan:N0} t · {CuLyChayKm:N0} km";
        public string NhanTyLe => $"{TyLeDinhMuc:N0}% ĐM";
        public string NhanMucDo => DinhMucNhienLieu.NhanMuc(MucDo);
        public decimal TyLeBon => DungTichBonDauLit > 0 ? Math.Round(SoLitTraNap / DungTichBonDauLit * 100m, 1) : 0m;

        public string MauChu => DinhMucNhienLieu.MauChu(MucDo);
        public string MauNen => DinhMucNhienLieu.MauNen(MucDo);
        public string MauVien => DinhMucNhienLieu.MauVien(MucDo);

        public GridLength PhanDaDung => DinhMucNhienLieu.TachThanh(TyLeDinhMuc).DaDung;
        public GridLength PhanConLai => DinhMucNhienLieu.TachThanh(TyLeDinhMuc).ConLai;

        public void GanDauMay(DauMayCapDau dm)
        {
            MaDauMay = dm.MaDauMay;
            SoHieuDauMay = dm.SoHieuDauMay;
            MaDongCode = dm.MaDongCode;
            DungTichBonDauLit = dm.DungTichBonDauLit;
            DinhMucSfc = dm.DinhMucSfc;
        }

        public CapNhienLieuDTO ToDTO() => new()
        {
            MaNhatKyDau = MaNhatKyDau, MaDauMay = MaDauMay, NoiCapDau = NoiCapDau, SoLitTraNap = SoLitTraNap,
            TrongLuongKeoTan = TrongLuongKeoTan, CuLyChayKm = CuLyChayKm, SuatTieuHaoSFC = SuatTieuHaoSFC,
            CanhBaoVuotMuc = CanhBaoVuotMuc, MaNguoiCap = MaNguoiCap, ThoiDiemBomDau = ThoiDiemBomDau,
            DonGiaVndLit = DonGiaVndLit, ChiPhi = ChiPhi, PhienBan = PhienBan
        };

        public CapNhienLieuHienThi SaoChep() => (CapNhienLieuHienThi)MemberwiseClone();
    }

    // The tom tat suat tieu hao gan nhat cua moi dau may (phia tren luoi)
    public class TinhTrangNhienLieuHienThi
    {
        public DauMayCapDau DauMay { get; init; } = null!;
        public CapNhienLieuHienThi? LanMoiNhat { get; init; }
        public CapNhienLieuHienThi? LanTruoc { get; init; }
        public int SoLanCap { get; init; }
        public bool DangChon { get; set; }

        public string TrangThai => LanMoiNhat?.MucDo ?? "CHUA_CO";
        public string NhanTrangThai => DinhMucNhienLieu.NhanMuc(TrangThai);

        public string GiaTri => LanMoiNhat != null ? $"{LanMoiNhat.SuatTieuHaoSFC:N2}" : "—";
        public string MoTa => LanMoiNhat == null
            ? $"Định mức {DauMay.DinhMucSfc:N0} · chưa cấp dầu"
            : $"{LanMoiNhat.TyLeDinhMuc:N0}% định mức {DauMay.DinhMucSfc:N0}";

        // So sanh voi lan cap truoc cua cung dau may
        public string XuHuong
        {
            get
            {
                if (LanMoiNhat == null || LanTruoc == null || LanTruoc.SuatTieuHaoSFC <= 0) return "Chưa đủ dữ liệu so sánh";
                decimal thayDoi = Math.Round((LanMoiNhat.SuatTieuHaoSFC - LanTruoc.SuatTieuHaoSFC) / LanTruoc.SuatTieuHaoSFC * 100m, 1);
                return thayDoi switch
                {
                    > 0 => $"▲ tăng {thayDoi:N1}% so với lần trước",
                    < 0 => $"▼ giảm {-thayDoi:N1}% so với lần trước",
                    _ => "Không đổi so với lần trước"
                };
            }
        }

        public string MauChu => DinhMucNhienLieu.MauChu(TrangThai);
        public string MauNen => DinhMucNhienLieu.MauNen(TrangThai);
        public string MauVien => DangChon ? "#003B73" : DinhMucNhienLieu.MauVien(TrangThai);
        public double DoDayVien => DangChon ? 2 : 1;

        public GridLength PhanDaDung => DinhMucNhienLieu.TachThanh(LanMoiNhat?.TyLeDinhMuc ?? 0m).DaDung;
        public GridLength PhanConLai => DinhMucNhienLieu.TachThanh(LanMoiNhat?.TyLeDinhMuc ?? 0m).ConLai;

        public SymbolRegular BieuTuong => TrangThai switch
        {
            "VUOT" => SymbolRegular.Warning24,
            "SAT" => SymbolRegular.Info24,
            "DAT" => SymbolRegular.CheckmarkCircle24,
            _ => SymbolRegular.Clock24
        };
    }

    // =========================================================================
    // 5. BAO DUONG DINH KY (baotri.NhatKyBaoDuong) - chu ky o QuyTacBaoTri (tham khao):
    //   dau may R1 50,000 km, R2 150,000 km (R2 bao gom R1), tinh tu lan bao duong gan nhat, chua co
    //   lich su thi tinh tu 0 km; toa xe D1 180 ngay, D2 360 ngay (D2 bao gom D1).
    // =========================================================================
    public static class ChuKyBaoDuong
    {
        public const decimal ChuKyR1Km = QuyTacBaoTri.ChuKyR1Km;
        public const decimal ChuKyR2Km = QuyTacBaoTri.ChuKyR2Km;
        public const int ChuKyD1Ngay = QuyTacBaoTri.ChuKyD1Ngay;
        public const int ChuKyD2Ngay = QuyTacBaoTri.ChuKyD2Ngay;
        public const decimal NguongSapToi = QuyTacBaoTri.NguongSapToi;
        public const decimal SoKmGioiHan = QuyTacBaoTri.SoKmGioiHan;

        public static string[] CapCua(string loaiPhuongTien) => QuyTacBaoTri.CapCua(loaiPhuongTien);
        public static decimal ChiPhiThamKhao(string cap) => QuyTacBaoTri.ChiPhiThamKhao(cap);
        public static bool LaCapCao(string cap) => QuyTacBaoTri.LaCapCao(cap);

        public static string MoTaChuKy(string cap) => cap switch
        {
            "R1" => $"Chu kỳ {ChuKyR1Km:N0} km",
            "R2" => $"Chu kỳ {ChuKyR2Km:N0} km · bao gồm R1",
            "D1" => $"Chu kỳ {ChuKyD1Ngay} ngày (tham khảo)",
            "D2" => $"Chu kỳ {ChuKyD2Ngay} ngày · bao gồm D1 (tham khảo)",
            _ => ""
        };

        public static string MauCap(string cap) => cap switch
        {
            "R1" => "#1D4ED8", "R2" => "#6D28D9", "D1" => "#0F766E", "D2" => "#B45309", _ => "#475569"
        };
        public static string NenCap(string cap) => cap switch
        {
            "R1" => "#EFF6FF", "R2" => "#F5F3FF", "D1" => "#F0FDFA", "D2" => "#FFFBEB", _ => "#F1F5F9"
        };

        // Tien do tung cap cua mot phuong tien (goi QuyTacBaoTri.TinhTienDo roi gan lai phieu hien thi)
        public static List<TienDoCapBaoDuong> TinhTienDo(PhuongTienBaoDuong pt, IEnumerable<BaoDuongHienThi> lichSuCuaPhuongTien)
        {
            var theoDto = lichSuCuaPhuongTien.Select(x => (Dto: x.ToDTO(), HienThi: x)).ToList();
            var tienDo = QuyTacBaoTri.TinhTienDo(pt.LoaiPhuongTien, pt.SoKmTichLuy, theoDto.Select(x => x.Dto), DateTime.Now);
            return tienDo.Select(t => new TienDoCapBaoDuong(t, theoDto.FirstOrDefault(x => ReferenceEquals(x.Dto, t.LanGanNhat)).HienThi))
                         .ToList();
        }

        // Cap can chu y nhat: qua han > sap toi > binh thuong > chua co; cung muc thi uu tien cap cao, roi % lon
        public static TienDoCapBaoDuong? CapCanChuY(IEnumerable<TienDoCapBaoDuong> tienDo)
        {
            var ds = tienDo.ToList();
            var chon = QuyTacBaoTri.CapCanChuY(ds.Select(t => t.Goc));
            return chon == null ? null : ds.First(t => ReferenceEquals(t.Goc, chon));
        }
    }

    // Tien do mot cap bao duong (bao lop BUS TienDoBaoDuongDTO, them chu / mau / thanh %)
    public class TienDoCapBaoDuong
    {
        public TienDoCapBaoDuong(TienDoBaoDuongDTO goc, BaoDuongHienThi? lanGanNhat)
        {
            Goc = goc;
            LanGanNhat = lanGanNhat;
        }

        public TienDoBaoDuongDTO Goc { get; }
        public BaoDuongHienThi? LanGanNhat { get; }

        public string Cap => Goc.Cap;
        public bool TheoKm => Goc.TheoKm;
        public decimal ChuKy => Goc.ChuKy;
        public decimal? DaDung => Goc.DaDung;
        public decimal? MocKeTiep => Goc.MocKeTiep;
        public decimal TyLe => Goc.TyLe;
        public string TrangThai => Goc.TrangThai;
        public int MucNghiemTrong => Goc.MucNghiemTrong;

        public string NhanTrangThai => TrangThai switch
        {
            "QUA_HAN" => $"Quá hạn {Cap}",
            "SAP_TOI" => $"Sắp tới hạn {Cap}",
            "BINH_THUONG" => "Bình thường",
            _ => "Chưa có lịch sử"
        };

        // Dung giua cau: "... dang qua han R2 ..." (khong ToLower de giu nguyen ma cap)
        public string CumTrangThai => TrangThai switch
        {
            "QUA_HAN" => $"quá hạn {Cap}",
            "SAP_TOI" => $"sắp tới hạn {Cap}",
            "BINH_THUONG" => "trong chu kỳ",
            _ => "chưa có lịch sử"
        };

        // "còn 1,500 km" / "quá 60,000 km" / "còn 12 ngày"
        public string ConLai => DaDung.HasValue ? KiemTraXuatBenService.MoTaConLai(Goc) : "—";

        public string MoTaMoc => !MocKeTiep.HasValue ? $"Chưa có lần {Cap} để tính mốc · {ChuKyBaoDuong.MoTaChuKy(Cap)}"
            : TheoKm ? $"Mốc kế tiếp {MocKeTiep:N0} km · {ChuKyBaoDuong.MoTaChuKy(Cap)}"
            : $"Đến hạn {LanGanNhat!.ThoiDiemHoanThanh.AddDays((double)ChuKy):dd/MM/yyyy} · {ChuKyBaoDuong.MoTaChuKy(Cap)}";

        public string MauChu => TrangThai switch { "QUA_HAN" => "#B91C1C", "SAP_TOI" => "#B45309", "BINH_THUONG" => "#15803D", _ => "#64748B" };
        public string MauNen => TrangThai switch { "QUA_HAN" => "#FEF2F2", "SAP_TOI" => "#FFFBEB", "BINH_THUONG" => "#F0FDF4", _ => "#F8FAFC" };
        public string MauVien => TrangThai switch { "QUA_HAN" => "#FECACA", "SAP_TOI" => "#FDE68A", "BINH_THUONG" => "#BBF7D0", _ => "#E2E8F0" };
        public string MauCap => ChuKyBaoDuong.MauCap(Cap);
        public string NenCap => ChuKyBaoDuong.NenCap(Cap);

        public GridLength PhanDaDung => new((double)Math.Min(TyLe, 100m), GridUnitType.Star);
        public GridLength PhanConLai => new((double)(100m - Math.Min(TyLe, 100m)), GridUnitType.Star);
    }

    // Phuong tien co the bao duong: dau may (phuongtien.DauMay) hoac toa xe (phuongtien.ToaXe)
    public record PhuongTienBaoDuong(string LoaiPhuongTien, int MaPhuongTien, string SoHieu, string MoTa,
                                     decimal SoKmTichLuy, string TrangThai, bool DangChay)
    {
        public bool LaDauMay => LoaiPhuongTien == "DAU_MAY";
        public bool NgungKhaiThac => TrangThai == PhuongTienService.NgungKhaiThac;
        public string Khoa => $"{LoaiPhuongTien}:{MaPhuongTien}";
        public string NhanLoai => LaDauMay ? "Đầu máy" : "Toa xe";
        public string TenHienThi => $"{SoHieu} — {MoTa}";
        public override string ToString() => TenHienThi;
    }

    public class BaoDuongHienThi
    {
        // --- Cot cua bang ---
        public int MaBaoDuong { get; set; }
        public string LoaiPhuongTien { get; set; } = "DAU_MAY";
        public int? MaDauMay { get; set; }
        public int? MaToaXe { get; set; }
        public string CapBaoDuong { get; set; } = "";
        public decimal SoKmTaiThoiDiem { get; set; }
        public string GhiChuKyThuat { get; set; } = "";
        public int? MaNguoiThucHien { get; set; }
        public DateTime ThoiDiemHoanThanh { get; set; }
        public decimal ChiPhi { get; set; }      // VND, migration V20261024_1000
        public byte[]? PhienBan { get; set; }

        // --- Cot JOIN de hien thi ---
        public string SoHieuPhuongTien { get; set; } = "";
        public string MoTaPhuongTien { get; set; } = "";
        public string TenNguoiThucHien { get; set; } = "";

        public bool LaDauMay => LoaiPhuongTien == "DAU_MAY";
        public string KhoaPhuongTien => $"{LoaiPhuongTien}:{(LaDauMay ? MaDauMay : MaToaXe)}";
        public string MaPhieu => $"BD-{MaBaoDuong:0000}";
        public string NhanLoai => LaDauMay ? "Đầu máy" : "Toa xe";
        public SymbolRegular BieuTuongLoai => LaDauMay ? SymbolRegular.Engine24 : SymbolRegular.VehicleSubway24;
        public string MauCap => ChuKyBaoDuong.MauCap(CapBaoDuong);
        public string NenCap => ChuKyBaoDuong.NenCap(CapBaoDuong);

        // Gan phuong tien dung rang buoc CK_PhuongTien_Ref (chi mot trong hai FK co gia tri)
        public void GanPhuongTien(PhuongTienBaoDuong pt)
        {
            LoaiPhuongTien = pt.LoaiPhuongTien;
            MaDauMay = pt.LaDauMay ? pt.MaPhuongTien : null;
            MaToaXe = pt.LaDauMay ? null : pt.MaPhuongTien;
            SoHieuPhuongTien = pt.SoHieu;
            MoTaPhuongTien = pt.MoTa;
        }

        public BaoDuongDTO ToDTO() => new()
        {
            MaBaoDuong = MaBaoDuong, LoaiPhuongTien = LoaiPhuongTien, MaDauMay = MaDauMay, MaToaXe = MaToaXe,
            CapBaoDuong = CapBaoDuong, SoKmTaiThoiDiem = SoKmTaiThoiDiem, GhiChuKyThuat = GhiChuKyThuat,
            MaNguoiThucHien = MaNguoiThucHien, ThoiDiemHoanThanh = ThoiDiemHoanThanh, ChiPhi = ChiPhi, PhienBan = PhienBan
        };

        public BaoDuongHienThi SaoChep() => (BaoDuongHienThi)MemberwiseClone();
    }

    // The tinh trang bao duong cua mot dau may (phia tren luoi)
    public class TinhTrangBaoDuongHienThi
    {
        public PhuongTienBaoDuong PhuongTien { get; init; } = null!;
        public List<TienDoCapBaoDuong> TienDo { get; init; } = new();
        public BaoDuongHienThi? LanGanNhat { get; init; }
        public bool DangChon { get; set; }

        public TienDoCapBaoDuong? CapChuY => ChuKyBaoDuong.CapCanChuY(TienDo);
        public string TrangThai => CapChuY?.TrangThai ?? "CHUA_CO";
        public string NhanTrangThai => CapChuY?.NhanTrangThai ?? "Chưa có lịch sử";
        public string GiaTri => CapChuY?.DaDung.HasValue == true ? $"{CapChuY.Cap} · {CapChuY.ConLai}" : "—";

        public string MoTa => LanGanNhat == null
            ? "Chưa có lần bảo dưỡng nào"
            : $"Lần gần nhất: {LanGanNhat.CapBaoDuong} · " +
              (PhuongTien.LaDauMay ? $"{LanGanNhat.SoKmTaiThoiDiem:N0} km" : $"{LanGanNhat.ThoiDiemHoanThanh:dd/MM/yyyy}");

        public string MauChu => CapChuY?.MauChu ?? "#64748B";
        public string MauNen => CapChuY?.MauNen ?? "#F8FAFC";
        public string MauVien => DangChon ? "#003B73" : CapChuY?.MauVien ?? "#E2E8F0";
        public double DoDayVien => DangChon ? 2 : 1;
        public GridLength PhanDaDung => CapChuY?.PhanDaDung ?? new GridLength(0, GridUnitType.Star);
        public GridLength PhanConLai => CapChuY?.PhanConLai ?? new GridLength(100, GridUnitType.Star);

        public SymbolRegular BieuTuong => TrangThai switch
        {
            "QUA_HAN" => SymbolRegular.ErrorCircle24,
            "SAP_TOI" => SymbolRegular.Warning24,
            "BINH_THUONG" => SymbolRegular.CheckmarkCircle24,
            _ => SymbolRegular.Clock24
        };
    }
}
