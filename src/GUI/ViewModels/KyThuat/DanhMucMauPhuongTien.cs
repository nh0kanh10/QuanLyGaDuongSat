using GUI.Helpers;
using System.Data;

namespace GUI.ViewModels.KyThuat
{
    // =========================================================================
    // DANH MUC PHUONG TIEN PHIA GIAO DIEN
    //
    // Tu Sprint 1 (09/10) phan he Phuong tien doc / ghi CSDL that:
    //   - dong dau may, chung loai toa, dau may, toa xe, lich dung ga -> doc qua Service;
    //   - nguong an toan lap tau (20 toa, 450 m, 14 t/truc, 15 phut cat/noi) -> BUS.Services.QuyTacLapTau.
    // Lich dung ga mau va 13 toa bai mau da bo (toa bai nam trong seed SQL).
    //
    // Con lai o day:
    //   1. Phan TRINH BAY theo chung loai toa (mau the toa, ten ngan, suc chua tham khao,
    //      tu trong / tai trong goi y khi them toa) - CSDL khong co cac cot nay.
    //   2. DongDauMay / ChungLoaiToa tinh: thong so trinh bay (Bao tri doc dau may tu CSDL tu 24/10).
    // =========================================================================
    public static class DanhMucMauPhuongTien
    {
        // ---------------------------------------------------------------------
        // Dong dau may - trung seed data (Bao tri dung; Phuong tien doc tu CSDL)
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<DongDauMayMau> DongDauMay = new[]
        {
            new DongDauMayMau(1, "D19E", "CSR Ziyang (Trung Quốc)", 1900, 120, 1200.0m, 4000, 78.0m, 16.5m),
            new DongDauMayMau(2, "D13E", "CKD Praha (Tiệp Khắc)",   1350, 100,  900.0m, 3200, 84.0m, 15.8m)
        };

        // ---------------------------------------------------------------------
        // Chung loai toa: thong so CSDL (ma, ten, dai, so truc) + phan trinh bay
        // (tu trong / tai trong goi y, suc chua tham khao, toa hang, mau).
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

        // Dong cua phuongtien.DongDauMay (PhuongTienService.LayDanhSachDongDauMay) -> lua chon cho dialog
        public static DongDauMayMau TuDongCsdl(DataRow r) => new(
            Convert.ToInt32(r["MaDongDauMay"]),
            r["MaDongCode"].ToString() ?? "",
            r["NhaSanXuat"].ToString() ?? "",
            Convert.ToInt32(r["CongSuatHP"]),
            Convert.ToInt32(r["TocDoToiDaKmh"]),
            Convert.ToDecimal(r["SucKeoToiDaTan"]),
            Convert.ToInt32(r["DungTichBonDauLit"]),
            Convert.ToDecimal(r["TrongLuongTan"]),
            Convert.ToDecimal(r["ChieuDaiM"]));

        // Dong cua phuongtien.ChungLoaiToa: thong so lay tu CSDL, phan trinh bay ghep theo ma
        public static ChungLoaiToaMau TuChungLoaiCsdl(DataRow r)
        {
            string ma = r["MaChungLoaiCode"].ToString() ?? "";
            var trinhBay = TimChungLoai(ma);
            return trinhBay with
            {
                MaChungLoai = Convert.ToInt32(r["MaChungLoai"]),
                MaChungLoaiCode = ma,
                TenMoTa = r["TenMoTa"].ToString() ?? ma,
                ChieuDaiChuanM = Convert.ToDecimal(r["ChieuDaiChuanM"]),
                SoTruc = Convert.ToInt32(r["SoTruc"]),
                LaToaHang = BUS.Services.QuyTacLapTau.LaToaHang(ma)
            };
        }
    }

    public record DongDauMayMau(
        int MaDongDauMay,
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
