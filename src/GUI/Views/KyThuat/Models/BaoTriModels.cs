using System.Windows;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.KyThuat.Models
{
    // =========================================================================
    // Cac lop HIEN THI cua phan he Bao tri ky thuat & Canh bao an toan.
    // Ten thuoc tinh trung ten cot trong schema baotri (script_tao_csdl_v2.sql)
    // de sau nay doc DataRow that khong phai doi binding.
    // =========================================================================

    // -------------------------------------------------------------------------
    // NGUONG KHAM XE - trung cot tinh toan baotri.KhamXeKyThuat.DuDieuKienXuatBen:
    //   ApLucHamBar >= 4.8 AND DoSutApBarPhut <= 0.20 AND DaCapNuoc = 1 AND DaXaVeSinh = 1
    // -------------------------------------------------------------------------
    public static class NguongKhamXe
    {
        public const decimal ApLucHamToiThieuBar = 4.8m;
        public const decimal DoSutApToiDaBarPhut = 0.20m;

        // Rang buoc CHECK cua cot (khong phai nguong dat / khong dat)
        public const decimal ApLucHamGioiHanBar = 10.0m;      // ApLucHamBar > 0 AND <= 10.0, DECIMAL(3,1)
        public const decimal DoSutApGioiHanBarPhut = 5.0m;    // DoSutApBarPhut >= 0 AND <= 5.0, DECIMAL(3,2)

        public static bool DuDieuKienXuatBen(decimal apLuc, decimal sutAp, bool daCapNuoc, bool daXaVeSinh)
            => apLuc >= ApLucHamToiThieuBar && sutAp <= DoSutApToiDaBarPhut && daCapNuoc && daXaVeSinh;

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
    // 1. DOAN TAU CAN KHAM (vanhanh.DoanTau JOIN ChuyenTau, MacTauMau, Ga, DauMay)
    // -------------------------------------------------------------------------
    public record DoanTauKhamXe(
        int MaDoanTau,
        int MaChuyenTau,
        string SoHieuMacTau,
        string LoaiTau,
        DateTime NgayXuatPhat,
        DateTime GioXuatPhatKH,
        string TenGaDi,
        string TenGaDen,
        string SoHieuDauMayChinh,
        string? SoHieuDauMayDay,
        int TongSoToa,
        decimal TongChieuDaiM,
        decimal TongTrongLuongTan,
        bool DaDuyetAnToan,
        string TrangThaiChuyen)
    {
        public string HanhTrinh => $"{TenGaDi} → {TenGaDen}";
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

    // Tai khoan (phanquyen.TaiKhoan) kem don vi de hien thi: nguoi kham xe, nguoi cap dau
    public record TaiKhoanMau(int MaTaiKhoan, string TenDangNhap, string HoTenHienThi, string DonVi)
    {
        public string TenHienThi => $"{HoTenHienThi} — {DonVi}";
        public override string ToString() => TenHienThi;
    }

    // -------------------------------------------------------------------------
    // 2. BIEN BAN KHAM XE KY THUAT (baotri.KhamXeKyThuat)
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

        // --- Cot JOIN de hien thi ---
        public string SoHieuMacTau { get; set; } = "";
        public DateTime NgayXuatPhat { get; set; }
        public string TenGaDi { get; set; } = "";
        public string TenGaDen { get; set; } = "";
        public string SoHieuDauMayChinh { get; set; } = "";
        public string TenNguoiKham { get; set; } = "";

        // Trang goi dat co nay cho ban ghi them / sua tren giao dien (chua ghi CSDL)
        public bool LaThayDoiTam { get; set; }

        // Cot tinh toan PERSISTED trong CSDL: tinh lai dung cong thuc
        public bool DuDieuKienXuatBen
            => NguongKhamXe.DuDieuKienXuatBen(ApLucHamBar, DoSutApBarPhut, DaCapNuoc, DaXaVeSinh);

        public bool DatApLucHam => ApLucHamBar >= NguongKhamXe.ApLucHamToiThieuBar;
        public bool DatSutAp => DoSutApBarPhut <= NguongKhamXe.DoSutApToiDaBarPhut;

        public int SoHangMucDat
            => (DatApLucHam ? 1 : 0) + (DatSutAp ? 1 : 0) + (DaCapNuoc ? 1 : 0) + (DaXaVeSinh ? 1 : 0);

        public string MaBienBan => MaKhamXe > 0 ? $"BB-{MaKhamXe:0000}" : $"MỚI-{-MaKhamXe:00}";
        public string HanhTrinh => $"{TenGaDi} → {TenGaDen}";

        public string MauApLuc => DatApLucHam ? "#15803D" : "#B91C1C";
        public string MauSutAp => DatSutAp ? "#15803D" : "#B91C1C";

        // Tom tat cac hang muc khong dat (hien trong luoi va the doan tau)
        public string TomTatLoi
        {
            get
            {
                var loi = new List<string>();
                if (!DatApLucHam) loi.Add($"Áp lực hãm {ApLucHamBar:0.0} bar");
                if (!DatSutAp) loi.Add($"Sụt áp {DoSutApBarPhut:0.00} bar/phút");
                if (!DaCapNuoc) loi.Add("Chưa cấp nước");
                if (!DaXaVeSinh) loi.Add("Chưa xả vệ sinh");
                return loi.Count == 0 ? "Đạt toàn bộ hạng mục" : string.Join(" · ", loi);
            }
        }

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

        // Ban sao de dialog sua tren do, bam Huy thi ban goc khong bi anh huong
        public KhamXeHienThi SaoChep() => (KhamXeHienThi)MemberwiseClone();
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
    //
    // CSDL chi luu SuatTieuHaoSFC NHAP TAY va khong co cot dinh muc
    // (xem Phan_Tich_CSDL_QuanLyDuongSatV2.md muc F7 va de xuat them DongDauMay.DinhMucSFC).
    // Giao dien tinh: SFC = so lit x 10.000 / (tan keo x km)  [lit / 10.000 tan-km]
    // roi so voi dinh muc THAM KHAO theo dong may de dat CanhBaoVuotMuc.
    // =========================================================================
    public static class DinhMucNhienLieu
    {
        public const decimal HeSoQuyDoi = 10_000m;
        public const decimal NguongSatDinhMuc = 90m;      // % dinh muc: tu day tro len coi la "sat dinh muc"
        public const decimal ThangHienThiToiDa = 150m;    // thanh tien do ve tu 0 den 150% dinh muc

        // Gioi han theo kieu cot
        public const decimal SoLitGioiHan = 999_999.99m;     // DECIMAL(8,2), CHECK > 0
        public const decimal TrongLuongGioiHan = 99_999.99m; // DECIMAL(7,2), CHECK > 0
        public const decimal CuLyGioiHan = 99_999.9m;        // DECIMAL(6,1), CHECK > 0
        public const decimal SfcGioiHan = 999.99m;           // DECIMAL(5,2), CHECK >= 0

        // Dinh muc THAM KHAO de mo phong - chua doi chieu dinh muc chinh thuc,
        // can kiem lai truoc khi dua vao bao cao (giong TaiTrongTrucToiDaTan).
        public static decimal LayDinhMuc(string? maDongCode) => maDongCode?.ToUpperInvariant() switch
        {
            "D19E" => 90m,
            "D13E" => 100m,
            _ => 95m
        };

        public static decimal? TinhSfc(decimal soLit, decimal tanKeo, decimal cuLyKm)
            => soLit > 0 && tanKeo > 0 && cuLyKm > 0
                ? Math.Round(soLit * HeSoQuyDoi / (tanKeo * cuLyKm), 2)
                : null;

        public static decimal TinhTyLe(decimal sfc, decimal dinhMuc)
            => dinhMuc > 0 ? Math.Round(sfc / dinhMuc * 100m, 1) : 0m;

        // VUOT | SAT | DAT
        public static string XepMuc(decimal tyLe)
            => tyLe > 100m ? "VUOT" : tyLe >= NguongSatDinhMuc ? "SAT" : "DAT";

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

    // Dau may kem dung tich bon dau cua dong may (phuongtien.DauMay JOIN DongDauMay)
    public record DauMayCapDau(int MaDauMay, string SoHieuDauMay, string MaDongCode, string DonViQuanLy,
                               string TrangThai, int DungTichBonDauLit)
    {
        public decimal DinhMucSfc => DinhMucNhienLieu.LayDinhMuc(MaDongCode);
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

        // --- Cot JOIN de hien thi ---
        public string SoHieuDauMay { get; set; } = "";
        public string MaDongCode { get; set; } = "";
        public int DungTichBonDauLit { get; set; }
        public string TenNguoiCap { get; set; } = "";

        public bool LaThayDoiTam { get; set; }

        public decimal DinhMucSfc => DinhMucNhienLieu.LayDinhMuc(MaDongCode);
        public decimal TyLeDinhMuc => DinhMucNhienLieu.TinhTyLe(SuatTieuHaoSFC, DinhMucSfc);
        public string MucDo => DinhMucNhienLieu.XepMuc(TyLeDinhMuc);

        public string MaPhieu => MaNhatKyDau > 0 ? $"NL-{MaNhatKyDau:0000}" : $"MỚI-{-MaNhatKyDau:00}";
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
        }

        // Tinh lai 2 cot SuatTieuHaoSFC + CanhBaoVuotMuc tu so do (CSDL de nhap tay)
        public void TinhLaiSuatTieuHao()
        {
            SuatTieuHaoSFC = DinhMucNhienLieu.TinhSfc(SoLitTraNap, TrongLuongKeoTan, CuLyChayKm) ?? 0m;
            CanhBaoVuotMuc = SuatTieuHaoSFC > DinhMucSfc;
        }

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
    // 5. BAO DUONG DINH KY (baotri.NhatKyBaoDuong)
    //
    // CSDL chi ghi tung lan bao duong (cap R1/R2/D1/D2) va khong co bang chu ky
    // (Phan_Tich_CSDL de xuat bang DinhMucBaoDuong). Quy uoc giao dien:
    //   - Dau may: R1 moi 50,000 km, R2 moi 150,000 km (dung hang so cua
    //     PhuongTienService); R2 bao gom R1. Tinh tu lan bao duong gan nhat,
    //     chua co lich su thi tinh tu 0 km (=> D13E-701 qua han R2, khop Q4_4).
    //   - Toa xe (CSDL khong co dong ho km cho toa): D1 moi 180 ngay, D2 moi 360
    //     ngay, D2 bao gom D1 - CHU KY THAM KHAO, chua doi chieu quy trinh VNR.
    // =========================================================================
    public static class ChuKyBaoDuong
    {
        public const decimal ChuKyR1Km = BUS.Services.PhuongTienService.MocBaoDuongR1;   // 50,000
        public const decimal ChuKyR2Km = BUS.Services.PhuongTienService.MocBaoDuongR2;   // 150,000
        public const int ChuKyD1Ngay = 180;
        public const int ChuKyD2Ngay = 360;
        public const decimal NguongSapToi = 90m;            // % chu ky: tu day tro len la "sap toi han"
        public const decimal SoKmGioiHan = 99_999_999.9m;   // DECIMAL(9,1), CHECK >= 0

        public static string[] CapCua(string loaiPhuongTien)
            => loaiPhuongTien == "DAU_MAY" ? new[] { "R1", "R2" } : new[] { "D1", "D2" };

        public static bool LaCapCao(string cap) => cap is "R2" or "D2";

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

        // Tien do tung cap cua mot phuong tien, dua tren lich su bao duong cua chinh no
        public static List<TienDoCapBaoDuong> TinhTienDo(PhuongTienBaoDuong pt, IEnumerable<BaoDuongHienThi> lichSuCuaPhuongTien)
        {
            var lichSu = lichSuCuaPhuongTien.OrderByDescending(x => x.ThoiDiemHoanThanh).ToList();
            var ketQua = new List<TienDoCapBaoDuong>();

            foreach (string cap in CapCua(pt.LoaiPhuongTien))
            {
                // Cap thap duoc "lam moi" boi ca cap cao (R2 bao gom R1, D2 bao gom D1)
                var lanGanNhat = lichSu.FirstOrDefault(x => x.CapBaoDuong == cap ||
                                                            (!LaCapCao(cap) && LaCapCao(x.CapBaoDuong)));

                if (pt.LoaiPhuongTien == "DAU_MAY")
                {
                    decimal chuKy = cap == "R1" ? ChuKyR1Km : ChuKyR2Km;
                    decimal moc = (lanGanNhat?.SoKmTaiThoiDiem ?? 0m) + chuKy;
                    decimal daChay = pt.SoKmTichLuy - (lanGanNhat?.SoKmTaiThoiDiem ?? 0m);
                    ketQua.Add(new TienDoCapBaoDuong(cap, lanGanNhat, true, chuKy, daChay, moc));
                }
                else
                {
                    int chuKy = cap == "D1" ? ChuKyD1Ngay : ChuKyD2Ngay;
                    if (lanGanNhat == null)
                    {
                        // Toa khong co ngay dua vao khai thac => khong co moc de tinh
                        ketQua.Add(new TienDoCapBaoDuong(cap, null, false, chuKy, null, null));
                    }
                    else
                    {
                        decimal soNgay = (decimal)(DateTime.Now - lanGanNhat.ThoiDiemHoanThanh).TotalDays;
                        ketQua.Add(new TienDoCapBaoDuong(cap, lanGanNhat, false, chuKy, Math.Floor(soNgay), chuKy));
                    }
                }
            }
            return ketQua;
        }

        // Cap can chu y nhat: qua han > sap toi > binh thuong > chua co; cung muc thi uu tien cap cao, roi % lon
        public static TienDoCapBaoDuong? CapCanChuY(IEnumerable<TienDoCapBaoDuong> tienDo)
            => tienDo.OrderByDescending(t => t.MucNghiemTrong)
                     .ThenByDescending(t => LaCapCao(t.Cap) && t.MucNghiemTrong >= 2)
                     .ThenByDescending(t => t.TyLe)
                     .FirstOrDefault();
    }

    // Tien do mot cap bao duong. Dau may tinh theo km, toa xe theo ngay.
    // DaDung: km da chay / so ngay ke tu lan bao duong gan nhat; null = chua co moc.
    public record TienDoCapBaoDuong(string Cap, BaoDuongHienThi? LanGanNhat, bool TheoKm,
                                    decimal ChuKy, decimal? DaDung, decimal? MocKeTiep)
    {
        public decimal TyLe => DaDung.HasValue && ChuKy > 0 ? Math.Round(DaDung.Value / ChuKy * 100m, 1) : 0m;

        // QUA_HAN | SAP_TOI | BINH_THUONG | CHUA_CO
        public string TrangThai => !DaDung.HasValue ? "CHUA_CO"
                                 : TyLe >= 100m ? "QUA_HAN"
                                 : TyLe >= ChuKyBaoDuong.NguongSapToi ? "SAP_TOI"
                                 : "BINH_THUONG";

        public int MucNghiemTrong => TrangThai switch { "QUA_HAN" => 3, "SAP_TOI" => 2, "BINH_THUONG" => 1, _ => 0 };

        private string DonVi => TheoKm ? "km" : "ngày";

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
        public string ConLai
        {
            get
            {
                if (!DaDung.HasValue) return "—";
                decimal con = ChuKy - DaDung.Value;
                return con >= 0 ? $"còn {con:N0} {DonVi}" : $"quá {-con:N0} {DonVi}";
            }
        }

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
                                     decimal SoKmTichLuy)
    {
        public bool LaDauMay => LoaiPhuongTien == "DAU_MAY";
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

        // --- Cot JOIN de hien thi ---
        public string SoHieuPhuongTien { get; set; } = "";
        public string MoTaPhuongTien { get; set; } = "";
        public string TenNguoiThucHien { get; set; } = "";

        public bool LaThayDoiTam { get; set; }

        public bool LaDauMay => LoaiPhuongTien == "DAU_MAY";
        public string KhoaPhuongTien => $"{LoaiPhuongTien}:{(LaDauMay ? MaDauMay : MaToaXe)}";
        public string MaPhieu => MaBaoDuong > 0 ? $"BD-{MaBaoDuong:0000}" : $"MỚI-{-MaBaoDuong:00}";
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
