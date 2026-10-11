using GUI.Helpers;
using System.Data;
using System.Windows;
using BUS.Services;
using DTO.PhuongTien;
using ET.PhuongTien;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.ViewModels.KyThuat
{
    // =========================================================================
    // Cac lop HIEN THI (view model) cua phan he Quan ly Phuong tien.
    // Chi phuc vu binding XAML: chuyen DataRow thanh thuoc tinh da dinh dang san
    // kem mau sac / bieu tuong. Khong chua nghiep vu.
    // =========================================================================

    // -------------------------------------------------------------------------
    // 1. DAU MAY
    // -------------------------------------------------------------------------
    public class DauMayHienThi
    {
        public int MaDauMay { get; set; }
        public string SoHieuDauMay { get; set; } = "";
        public int MaDongDauMay { get; set; }
        public string MaDongCode { get; set; } = "";
        public string NhaSanXuat { get; set; } = "";
        public string DonViQuanLy { get; set; } = "";
        public string TrangThai { get; set; } = "";

        public int CongSuatHP { get; set; }
        public int TocDoToiDaKmh { get; set; }
        public decimal SucKeoToiDaTan { get; set; }
        public int DungTichBonDauLit { get; set; }
        public decimal TrongLuongTan { get; set; }
        public decimal ChieuDaiM { get; set; }

        public int NamSanXuat { get; set; }
        public int TuoiKhaiThac { get; set; }
        public decimal SoKmTichLuy { get; set; }

        // So km o lan bao duong gan nhat (baotri.NhatKyBaoDuong): R1 = lan R1 hoac R2, R2 = lan R2.
        // null = chua bao duong lan nao (tinh chu ky tu 0 km, khop man hinh Bao tri).
        public decimal? KmBaoDuongR1GanNhat { get; set; }
        public decimal? KmBaoDuongR2GanNhat { get; set; }

        public string TenMoTaDong => $"{MaDongCode} — {NhaSanXuat}";

        // --- Thong tin vach tien do bao duong tren DataGrid ---
        public TienDoBaoDuongDauMay TienDoBaoDuong { get; private set; } = null!;
        public string NhanBaoDuong { get; set; } = "";
        public string MauChuBaoDuong { get; set; } = "#15803D";

        // Cap cot star-sizing dung ve thanh tien do (khong can converter)
        public GridLength PhanDaChay { get; set; } = new(0, GridUnitType.Star);
        public GridLength PhanConLai { get; set; } = new(100, GridUnitType.Star);

        public static DauMayHienThi TuDongDuLieu(DataRow r)
        {
            var dm = new DauMayHienThi
            {
                MaDauMay = LayInt(r, "MaDauMay"),
                SoHieuDauMay = LayChuoi(r, "SoHieuDauMay"),
                MaDongDauMay = LayInt(r, "MaDongDauMay"),
                MaDongCode = LayChuoi(r, "MaDongCode"),
                NhaSanXuat = LayChuoi(r, "NhaSanXuat"),
                DonViQuanLy = LayChuoi(r, "DonViQuanLy"),
                TrangThai = LayChuoi(r, "TrangThai"),
                CongSuatHP = LayInt(r, "CongSuatHP"),
                TocDoToiDaKmh = LayInt(r, "TocDoToiDaKmh"),
                SucKeoToiDaTan = LayDecimal(r, "SucKeoToiDaTan"),
                DungTichBonDauLit = LayInt(r, "DungTichBonDauLit"),
                TrongLuongTan = LayDecimal(r, "TrongLuongTan"),
                ChieuDaiM = LayDecimal(r, "ChieuDaiM"),
                NamSanXuat = LayInt(r, "NamSanXuat"),
                TuoiKhaiThac = LayInt(r, "TuoiKhaiThac"),
                SoKmTichLuy = LayDecimal(r, "SoKmTichLuy"),
                KmBaoDuongR1GanNhat = LayDecimalNull(r, "KmBaoDuongR1GanNhat"),
                KmBaoDuongR2GanNhat = LayDecimalNull(r, "KmBaoDuongR2GanNhat")
            };

            dm.TinhLaiTienDoBaoDuong();
            return dm;
        }

        // Tinh lai cac thuoc tinh hien thi phu thuoc so km
        public void TinhLaiTienDoBaoDuong()
        {
            TienDoBaoDuong = PhuongTienService.TinhTienDoBaoDuong(SoKmTichLuy, KmBaoDuongR1GanNhat, KmBaoDuongR2GanNhat);

            NhanBaoDuong = $"{TienDoBaoDuong.Cap} · {TienDoBaoDuong.TyLePhanTram:N0}%";
            MauChuBaoDuong = LayMauTheoMucCanhBao(TienDoBaoDuong.MucCanhBao);

            double phanTram = Math.Min(100.0, (double)TienDoBaoDuong.TyLePhanTram);
            PhanDaChay = new GridLength(phanTram, GridUnitType.Star);
            PhanConLai = new GridLength(100 - phanTram, GridUnitType.Star);
        }

        // Ban sao de dialog sua tren do, bam Huy thi ban goc khong bi anh huong
        public DauMayHienThi SaoChep() => (DauMayHienThi)MemberwiseClone();

        // Ban ghi CSDL (phuongtien.DauMay) tuong ung
        public DauMay ToET() => new()
        {
            MaDauMay = MaDauMay,
            SoHieuDauMay = SoHieuDauMay,
            MaDongDauMay = MaDongDauMay,
            DonViQuanLy = DonViQuanLy,
            NamSanXuat = NamSanXuat,
            SoKmTichLuy = SoKmTichLuy,
            TrangThai = TrangThai
        };

        // QUA_HAN | SAP_TOI | BINH_THUONG (PhuongTienService.TinhTienDoBaoDuong)
        public static string LayMauTheoMucCanhBao(string mucCanhBao) => mucCanhBao switch
        {
            "QUA_HAN" => "#B91C1C",
            "SAP_TOI" => "#B45309",
            _ => "#15803D"
        };

        internal static string LayChuoi(DataRow r, string cot)
            => r.Table.Columns.Contains(cot) && r[cot] != DBNull.Value ? r[cot].ToString() ?? "" : "";

        internal static int LayInt(DataRow r, string cot)
            => r.Table.Columns.Contains(cot) && r[cot] != DBNull.Value ? Convert.ToInt32(r[cot]) : 0;

        internal static decimal LayDecimal(DataRow r, string cot)
            => r.Table.Columns.Contains(cot) && r[cot] != DBNull.Value ? Convert.ToDecimal(r[cot]) : 0m;

        internal static decimal? LayDecimalNull(DataRow r, string cot)
            => r.Table.Columns.Contains(cot) && r[cot] != DBNull.Value ? Convert.ToDecimal(r[cot]) : null;
    }

    // -------------------------------------------------------------------------
    // 2. LICH SU VAN DUNG DAU MAY
    // -------------------------------------------------------------------------
    public class LichSuVanDungHienThi
    {
        public int MaChuyenTau { get; set; }
        public string SoHieuMacTau { get; set; } = "";
        public string LoaiTau { get; set; } = "";
        public DateTime NgayXuatPhat { get; set; }
        public string TrangThai { get; set; } = "";
        public string VaiTroDauMay { get; set; } = "";
        public string TenGaDi { get; set; } = "";
        public string TenGaDen { get; set; } = "";

        public string HanhTrinh => $"{TenGaDi} → {TenGaDen}";

        public static LichSuVanDungHienThi TuDongDuLieu(DataRow r) => new()
        {
            MaChuyenTau = DauMayHienThi.LayInt(r, "MaChuyenTau"),
            SoHieuMacTau = DauMayHienThi.LayChuoi(r, "SoHieuMacTau"),
            LoaiTau = DauMayHienThi.LayChuoi(r, "LoaiTau"),
            NgayXuatPhat = r["NgayXuatPhat"] != DBNull.Value ? Convert.ToDateTime(r["NgayXuatPhat"]) : DateTime.MinValue,
            TrangThai = DauMayHienThi.LayChuoi(r, "TrangThai"),
            VaiTroDauMay = DauMayHienThi.LayChuoi(r, "VaiTroDauMay"),
            TenGaDi = DauMayHienThi.LayChuoi(r, "TenGaDi"),
            TenGaDen = DauMayHienThi.LayChuoi(r, "TenGaDen")
        };
    }

    // -------------------------------------------------------------------------
    // 2b. TOA XE TRONG DOI (phuongtien.ToaXe - tai san vat ly)
    // Ten thuoc tinh trung ten cot truy van LayDanhSachToaXeDoi de giu nguyen binding.
    // -------------------------------------------------------------------------
    public class ToaXeHienThi
    {
        public int MaToaXe { get; set; }
        public string SoHieuToaXe { get; set; } = "";
        public int MaChungLoai { get; set; }
        public string MaChungLoaiCode { get; set; } = "";
        public string TenMoTa { get; set; } = "";
        public decimal ChieuDaiChuanM { get; set; }
        public int SoTruc { get; set; }
        public decimal TuTrongTan { get; set; }
        public decimal TaiTrongToiDaTan { get; set; }
        public string TrangThai { get; set; } = "SAN_SANG";

        public decimal TaiTrongTrucTan => SoTruc > 0
            ? Math.Round((TuTrongTan + TaiTrongToiDaTan) / SoTruc, 2)
            : 0m;

        public static ToaXeHienThi TuDongDuLieu(DataRow r) => new()
        {
            MaToaXe = DauMayHienThi.LayInt(r, "MaToaXe"),
            SoHieuToaXe = DauMayHienThi.LayChuoi(r, "SoHieuToaXe"),
            MaChungLoai = DauMayHienThi.LayInt(r, "MaChungLoai"),
            MaChungLoaiCode = DauMayHienThi.LayChuoi(r, "MaChungLoaiCode"),
            TenMoTa = DauMayHienThi.LayChuoi(r, "TenMoTa"),
            ChieuDaiChuanM = DauMayHienThi.LayDecimal(r, "ChieuDaiChuanM"),
            SoTruc = DauMayHienThi.LayInt(r, "SoTruc"),
            TuTrongTan = DauMayHienThi.LayDecimal(r, "TuTrongTan"),
            TaiTrongToiDaTan = DauMayHienThi.LayDecimal(r, "TaiTrongToiDaTan"),
            TrangThai = DauMayHienThi.LayChuoi(r, "TrangThai")
        };

        public ToaXeHienThi SaoChep() => (ToaXeHienThi)MemberwiseClone();

        // Ban ghi CSDL (phuongtien.ToaXe) tuong ung
        public ToaXe ToET() => new()
        {
            MaToaXe = MaToaXe,
            SoHieuToaXe = SoHieuToaXe,
            MaChungLoai = MaChungLoai,
            TuTrongTan = TuTrongTan,
            TaiTrongToiDaTan = TaiTrongToiDaTan,
            TrangThai = TrangThai
        };
    }

    // Mot the trong khung "Co cau chung loai toa"
    public record ChungLoaiToaThongKe(
        string MaChungLoaiCode,
        string TenMoTa,
        decimal ChieuDaiChuanM,
        int SoTruc,
        int SoToaTrongDoi);

    // -------------------------------------------------------------------------
    // 3. TOA XE TRONG DOAN TAU CUA MOT CHUYEN (vanhanh.ChiTietDoanTau - toa that)
    // Cot cuoi: tai trong truc khi cho day so voi nguong QuyTacLapTau.TaiTrongTrucToiDaTan.
    // Ghe da ban nam o tab So do cho ngoi (toa ban ve vantai.ToaXeKhach chua noi voi
    // toa that - hop dong §7.1 KE_HOACH_BACK_END.md).
    // -------------------------------------------------------------------------
    public class ToaBienCheHienThi
    {
        public int MaToaXe { get; set; }
        public int ThuTuToa { get; set; }
        public string NhanHieuToa { get; set; } = "";
        public string LoaiToa { get; set; } = "";
        public string TenLoaiToa { get; set; } = "";
        public decimal ChieuDaiToaM { get; set; }
        public string NhanSucChua { get; set; } = "";
        public string TrangThai { get; set; } = "";

        public string PhamViChay { get; set; } = "Suốt hành trình";
        public bool CoPhamViRieng { get; set; }

        public decimal TaiTrongTrucTan { get; set; }
        public string NhanTaiTrongTruc { get; set; } = "";
        public decimal PhanTramTruc { get; set; }
        public string MauTruc { get; set; } = "#15803D";
        public GridLength PhanTruc { get; set; } = new(0, GridUnitType.Star);
        public GridLength PhanTrucConLai { get; set; } = new(100, GridUnitType.Star);

        public static ToaBienCheHienThi TuToa(int thuTu, ToaXeLapTauDTO toa, string phamVi, bool coPhamViRieng)
        {
            var cl = DanhMucMauPhuongTien.TimChungLoai(toa.MaChungLoaiCode);
            decimal truc = QuyTacLapTau.TaiTrongTruc(toa);
            decimal nguong = QuyTacLapTau.TaiTrongTrucToiDaTan;
            decimal phanTram = nguong > 0 ? Math.Round(truc / nguong * 100m, 1) : 0m;
            double hienThi = Math.Min(100.0, (double)phanTram);

            return new ToaBienCheHienThi
            {
                MaToaXe = toa.MaToaXe,
                ThuTuToa = thuTu,
                NhanHieuToa = toa.SoHieuToaXe,
                LoaiToa = toa.MaChungLoaiCode,
                TenLoaiToa = cl.TenNgan,
                ChieuDaiToaM = toa.ChieuDaiM,
                NhanSucChua = QuyTacLapTau.LaToaHang(toa.MaChungLoaiCode) ? "Toa hàng" : $"{cl.SucChuaThamKhao} chỗ",
                TrangThai = toa.TrangThai,
                PhamViChay = phamVi,
                CoPhamViRieng = coPhamViRieng,
                TaiTrongTrucTan = truc,
                NhanTaiTrongTruc = $"{toa.TongTrongLuongTan:N1} t · {truc:N2} t/trục",
                PhanTramTruc = phanTram,
                MauTruc = truc > nguong ? "#B91C1C" : phanTram >= 90m ? "#B45309" : "#15803D",
                PhanTruc = new GridLength(hienThi, GridUnitType.Star),
                PhanTrucConLai = new GridLength(100 - hienThi, GridUnitType.Star)
            };
        }
    }

    // -------------------------------------------------------------------------
    // 4. MOT O GHE TREN SO DO
    // -------------------------------------------------------------------------
    public class GheHienThi
    {
        public int MaChoNgoi { get; set; }
        public int SoGhe { get; set; }
        public int? TangGiuong { get; set; }
        public bool LaGhePhu { get; set; }
        public string LoaiToa { get; set; } = "";

        // Mot cho ngoi co the ban cho nhieu khach tren cac chang khong giao nhau
        public int SoVeDaBan { get; set; }
        public string DanhSachChang { get; set; } = "";
        public decimal TongTienGhe { get; set; }

        public bool DaBan => SoVeDaBan > 0;
        public bool BanNhieuChang => SoVeDaBan > 1;

        public string MauNen { get; set; } = "#F0FDF4";
        public string MauVien { get; set; } = "#86EFAC";
        public string MauChu { get; set; } = "#15803D";

        // Dong chu nho duoi so ghe: tang giuong hoac trang thai
        public string NhanPhu { get; set; } = "";

        public string MoTaDayDu { get; set; } = "";

        public static GheHienThi TuDongDuLieu(DataRow r)
        {
            var ghe = new GheHienThi
            {
                MaChoNgoi = DauMayHienThi.LayInt(r, "MaChoNgoi"),
                SoGhe = DauMayHienThi.LayInt(r, "SoGhe"),
                TangGiuong = r.Table.Columns.Contains("TangGiuong") && r["TangGiuong"] != DBNull.Value
                    ? Convert.ToInt32(r["TangGiuong"])
                    : null,
                LaGhePhu = r.Table.Columns.Contains("LaGhePhu") && r["LaGhePhu"] != DBNull.Value && Convert.ToBoolean(r["LaGhePhu"]),
                LoaiToa = DauMayHienThi.LayChuoi(r, "LoaiToa"),
                SoVeDaBan = DauMayHienThi.LayInt(r, "SoVeDaBan"),
                DanhSachChang = DauMayHienThi.LayChuoi(r, "DanhSachChang"),
                TongTienGhe = DauMayHienThi.LayDecimal(r, "TongTienGhe")
            };

            string moTaViTri = ghe.TangGiuong.HasValue
                ? $"Ghế {ghe.SoGhe} · Tầng {ghe.TangGiuong}"
                : $"Ghế {ghe.SoGhe}";

            if (!ghe.DaBan)
            {
                // Còn trống: xanh lá
                ghe.MauNen = "#F0FDF4";
                ghe.MauVien = "#86EFAC";
                ghe.MauChu = "#15803D";
                ghe.NhanPhu = ghe.TangGiuong.HasValue
                    ? $"Tầng {ghe.TangGiuong}"
                    : (ghe.LaGhePhu ? "Ghế phụ" : "Trống");

                ghe.MoTaDayDu = $"{moTaViTri}\nTrạng thái: Còn trống";
            }
            else if (ghe.BanNhieuChang)
            {
                // Ban nhieu chang khong giao nhau: to mau cam de phan biet
                ghe.MauNen = "#FFFBEB";
                ghe.MauVien = "#FCD34D";
                ghe.MauChu = "#B45309";
                ghe.NhanPhu = $"{ghe.SoVeDaBan} chặng";

                ghe.MoTaDayDu =
                    $"{moTaViTri}\n" +
                    $"Đã bán {ghe.SoVeDaBan} chặng không giao nhau:\n{ghe.DanhSachChang}\n" +
                    $"Tổng thu: {ghe.TongTienGhe:N0} đ";
            }
            else
            {
                // Ban tron mot chang: do
                ghe.MauNen = "#FEE2E2";
                ghe.MauVien = "#FCA5A5";
                ghe.MauChu = "#B91C1C";
                ghe.NhanPhu = ghe.TangGiuong.HasValue ? $"Tầng {ghe.TangGiuong}" : "Đã bán";

                ghe.MoTaDayDu =
                    $"{moTaViTri}\n{ghe.DanhSachChang}\n" +
                    $"Giá vé: {ghe.TongTienGhe:N0} đ";
            }

            return ghe;
        }
    }

    // -------------------------------------------------------------------------
    // 5. HANH KHACH DANG DI TREN TOA
    // -------------------------------------------------------------------------
    public class HanhKhachTrenToaHienThi
    {
        public int SoGhe { get; set; }
        public int? TangGiuong { get; set; }
        public string TenHanhKhach { get; set; } = "";
        public string MaVeCode { get; set; } = "";
        public string Chang { get; set; } = "";
        public decimal GiaVeThucThu { get; set; }

        public string ViTri => TangGiuong.HasValue ? $"{SoGhe} (T{TangGiuong})" : SoGhe.ToString();

        public static HanhKhachTrenToaHienThi TuDongDuLieu(DataRow r) => new()
        {
            SoGhe = DauMayHienThi.LayInt(r, "SoGhe"),
            TangGiuong = r.Table.Columns.Contains("TangGiuong") && r["TangGiuong"] != DBNull.Value
                ? Convert.ToInt32(r["TangGiuong"])
                : null,
            TenHanhKhach = DauMayHienThi.LayChuoi(r, "TenHanhKhach"),
            MaVeCode = DauMayHienThi.LayChuoi(r, "MaVeCode"),
            Chang = $"{DauMayHienThi.LayChuoi(r, "TenGaDi")} → {DauMayHienThi.LayChuoi(r, "TenGaDen")}",
            GiaVeThucThu = DauMayHienThi.LayDecimal(r, "GiaVeThucThu")
        };
    }

    // -------------------------------------------------------------------------
    // 6. MOT HANG MUC TRONG BANG KIEM TRA AN TOAN
    // -------------------------------------------------------------------------
    public class HangMucAnToanHienThi
    {
        public string TenHangMuc { get; set; } = "";
        public string KetQua { get; set; } = "";
        public string MucDo { get; set; } = "DAT";      // DAT | CANH_BAO | LOI
        public SymbolRegular BieuTuong { get; set; } = SymbolRegular.CheckmarkCircle24;
        public string MauSac { get; set; } = "#15803D";

        public static HangMucAnToanHienThi Dat(string ten, string ketQua) => new()
        {
            TenHangMuc = ten,
            KetQua = ketQua,
            MucDo = "DAT",
            BieuTuong = SymbolRegular.CheckmarkCircle24,
            MauSac = "#15803D"
        };

        public static HangMucAnToanHienThi CanhBao(string ten, string ketQua) => new()
        {
            TenHangMuc = ten,
            KetQua = ketQua,
            MucDo = "CANH_BAO",
            BieuTuong = SymbolRegular.Warning24,
            MauSac = "#B45309"
        };

        public static HangMucAnToanHienThi Loi(string ten, string ketQua) => new()
        {
            TenHangMuc = ten,
            KetQua = ketQua,
            MucDo = "LOI",
            BieuTuong = SymbolRegular.DismissCircle24,
            MauSac = "#B91C1C"
        };

        // Hang muc tu bo luat BUS.Services.QuyTacLapTau
        public static HangMucAnToanHienThi TuDTO(HangMucKiemTraDTO h) => h.MucDo switch
        {
            MucDoKiemTra.Loi => Loi(h.Ten, h.NoiDung),
            MucDoKiemTra.CanhBao => CanhBao(h.Ten, h.NoiDung),
            _ => Dat(h.Ten, h.NoiDung)
        };
    }
}
