using System.Data;
using System.Windows;
using BUS.Services;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.KyThuat.Models
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

        public string TenMoTaDong => $"{MaDongCode} — {NhaSanXuat}";

        // --- Thong tin vach tien do bao duong tren DataGrid ---
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
                SoKmTichLuy = LayDecimal(r, "SoKmTichLuy")
            };

            dm.TinhLaiTienDoBaoDuong();
            return dm;
        }

        // Tinh lai cac thuoc tinh hien thi phu thuoc so km (goi sau khi them/sua tren giao dien)
        public void TinhLaiTienDoBaoDuong()
        {
            var (cap, _, tyLe, mucCanhBao) = PhuongTienService.TinhTienDoBaoDuong(SoKmTichLuy);

            NhanBaoDuong = $"{cap} · {tyLe:N0}%";
            MauChuBaoDuong = LayMauTheoMucCanhBao(mucCanhBao);

            double phanTram = (double)tyLe;
            PhanDaChay = new GridLength(phanTram, GridUnitType.Star);
            PhanConLai = new GridLength(100 - phanTram, GridUnitType.Star);
        }

        // Ban sao de dialog sua tren do, bam Huy thi ban goc khong bi anh huong
        public DauMayHienThi SaoChep() => (DauMayHienThi)MemberwiseClone();

        public static string LayMauTheoMucCanhBao(string mucCanhBao) => mucCanhBao switch
        {
            "QUA_HAN" => "#B91C1C",
            "KHAN_CAP" => "#DC2626",
            "CANH_BAO" => "#B45309",
            _ => "#15803D"
        };

        internal static string LayChuoi(DataRow r, string cot)
            => r.Table.Columns.Contains(cot) && r[cot] != DBNull.Value ? r[cot].ToString() ?? "" : "";

        internal static int LayInt(DataRow r, string cot)
            => r.Table.Columns.Contains(cot) && r[cot] != DBNull.Value ? Convert.ToInt32(r[cot]) : 0;

        internal static decimal LayDecimal(DataRow r, string cot)
            => r.Table.Columns.Contains(cot) && r[cot] != DBNull.Value ? Convert.ToDecimal(r[cot]) : 0m;
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
    }

    // Mot the trong khung "Co cau chung loai toa" (so toa co cong ca toa them tam)
    public record ChungLoaiToaThongKe(
        string MaChungLoaiCode,
        string TenMoTa,
        decimal ChieuDaiChuanM,
        int SoTruc,
        int SoToaTrongDoi);

    // -------------------------------------------------------------------------
    // 3. TOA XE TRONG BIEN CHE MOT CHUYEN
    // -------------------------------------------------------------------------
    public class ToaBienCheHienThi
    {
        public int MaToaXeKhach { get; set; }
        public string NhanHieuToa { get; set; } = "";
        public string LoaiToa { get; set; } = "";
        public string TenLoaiToa { get; set; } = "";
        public int ThuTuToa { get; set; }
        public int SucChua { get; set; }
        public decimal ChieuDaiToaM { get; set; }
        public int SoGheDaThietLap { get; set; }
        public int SoGheDaBan { get; set; }

        public decimal PhanTramLapDay { get; set; }
        public string NhanLapDay { get; set; } = "";
        public string MauLapDay { get; set; } = "#15803D";

        public GridLength PhanDaBan { get; set; } = new(0, GridUnitType.Star);
        public GridLength PhanConTrong { get; set; } = new(100, GridUnitType.Star);

        public static ToaBienCheHienThi TuDongDuLieu(DataRow r)
        {
            var toa = new ToaBienCheHienThi
            {
                MaToaXeKhach = DauMayHienThi.LayInt(r, "MaToaXeKhach"),
                NhanHieuToa = DauMayHienThi.LayChuoi(r, "NhanHieuToa"),
                LoaiToa = DauMayHienThi.LayChuoi(r, "LoaiToa"),
                TenLoaiToa = DauMayHienThi.LayChuoi(r, "TenLoaiToa"),
                ThuTuToa = DauMayHienThi.LayInt(r, "ThuTuToa"),
                SucChua = DauMayHienThi.LayInt(r, "SucChua"),
                ChieuDaiToaM = DauMayHienThi.LayDecimal(r, "ChieuDaiToaM"),
                SoGheDaThietLap = DauMayHienThi.LayInt(r, "SoGheDaThietLap"),
                SoGheDaBan = DauMayHienThi.LayInt(r, "SoGheDaBan")
            };

            // Mau so uu tien so ghe da thiet lap thuc te, neu chua co thi dung suc chua khai bao
            int mauSo = toa.SoGheDaThietLap > 0 ? toa.SoGheDaThietLap : toa.SucChua;

            toa.PhanTramLapDay = mauSo > 0
                ? Math.Round((decimal)toa.SoGheDaBan / mauSo * 100m, 1)
                : 0m;

            toa.NhanLapDay = toa.SoGheDaThietLap > 0
                ? $"{toa.SoGheDaBan}/{toa.SoGheDaThietLap} chỗ"
                : "Chưa sinh ghế";

            toa.MauLapDay = toa.PhanTramLapDay switch
            {
                >= 90m => "#B91C1C",
                >= 70m => "#B45309",
                >= 40m => "#1D4ED8",
                _ => "#15803D"
            };

            double phanTram = (double)toa.PhanTramLapDay;
            toa.PhanDaBan = new GridLength(phanTram, GridUnitType.Star);
            toa.PhanConTrong = new GridLength(100 - phanTram, GridUnitType.Star);

            return toa;
        }

        // Dong bien che lay tu phuong an lap tau keo-tha: chua sinh ghe, chua ban ve
        public static ToaBienCheHienThi TuToaLapTau(ToaLapTau t) => new()
        {
            NhanHieuToa = t.SoHieu,
            LoaiToa = t.LoaiCode,
            TenLoaiToa = DanhMucMauPhuongTien.TimChungLoai(t.LoaiCode).TenNgan,
            ThuTuToa = t.ThuTu,
            SucChua = t.SucChua,
            ChieuDaiToaM = t.ChieuDaiM,
            NhanLapDay = t.LaToaHang ? "Toa hàng" : "Phương án mô phỏng",
            MauLapDay = "#94A3B8"
        };
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
    }
}
