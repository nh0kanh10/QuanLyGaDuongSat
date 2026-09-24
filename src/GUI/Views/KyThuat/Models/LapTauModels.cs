using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GUI.Views.KyThuat.Models
{
    // =========================================================================
    // Cac lop phuc vu man hinh MO PHONG LAP TAU KEO - THA.
    // Chi ton tai tren giao dien, tuong ung vanhanh.DoanTau + vanhanh.ChiTietDoanTau
    // khi noi CSDL that.
    // =========================================================================

    // -------------------------------------------------------------------------
    // Mot toa xe tren man hinh lap tau (o bai toa hoac trong doan tau)
    // -------------------------------------------------------------------------
    public class ToaLapTau : INotifyPropertyChanged
    {
        public string SoHieu { get; set; } = "";
        public string LoaiCode { get; set; } = "";
        public string TenLoai { get; set; } = "";
        public decimal ChieuDaiM { get; set; }
        public int SoTruc { get; set; } = 4;
        public decimal TuTrongTan { get; set; }
        public decimal TaiTrongTan { get; set; }
        public int SucChua { get; set; }
        public bool LaToaHang { get; set; }

        public string MauNen { get; set; } = "#F1F5F9";
        public string MauVien { get; set; } = "#CBD5E1";
        public string MauChu { get; set; } = "#475569";

        // Toa lay tu danh muc mau (khong co trong CSDL)
        public bool LaDuLieuMau { get; set; }

        // Dong ghi chu nguon goc hien o cuoi tooltip
        public string GhiChu { get; set; } = "";

        private int _thuTu;
        public int ThuTu
        {
            get => _thuTu;
            set { if (_thuTu != value) { _thuTu = value; BaoThayDoi(); } }
        }

        public decimal TongTrongLuongTan => TuTrongTan + TaiTrongTan;

        public decimal TaiTrongTrucTan => SoTruc > 0 ? Math.Round(TongTrongLuongTan / SoTruc, 2) : 0m;

        public bool VuotTaiTrongTruc => TaiTrongTrucTan > DanhMucMauPhuongTien.TaiTrongTrucToiDaTan;

        public string MoTaNgan => $"{ChieuDaiM:N1} m · {TongTrongLuongTan:N1} t";

        public string NhanSucChua => LaToaHang ? "Toa hàng" : $"{SucChua} chỗ";

        public string MoTaChiTiet =>
            $"{SoHieu} — {TenLoai}\n" +
            $"Chiều dài: {ChieuDaiM:N1} m · {SoTruc} trục\n" +
            $"Tự trọng {TuTrongTan:N1} t + tải trọng {TaiTrongTan:N1} t = {TongTrongLuongTan:N1} t\n" +
            $"Tải trọng trục: {TaiTrongTrucTan:N2} t/trục" +
            (VuotTaiTrongTruc ? $" (vượt ngưỡng {DanhMucMauPhuongTien.TaiTrongTrucToiDaTan:N1} t)" : "") +
            (LaToaHang ? "" : $"\nSức chứa: {SucChua} chỗ") +
            (GhiChu.Length > 0 ? $"\n{GhiChu}" : "");

        // Tao tu dong doi toa xe (phuongtien.ToaXe)
        public static ToaLapTau TuToaXe(ToaXeHienThi tx)
        {
            var cl = DanhMucMauPhuongTien.TimChungLoai(tx.MaChungLoaiCode);
            return new ToaLapTau
            {
                SoHieu = tx.SoHieuToaXe,
                LoaiCode = tx.MaChungLoaiCode,
                TenLoai = string.IsNullOrWhiteSpace(tx.TenMoTa) ? cl.TenMoTa : tx.TenMoTa,
                ChieuDaiM = tx.ChieuDaiChuanM > 0 ? tx.ChieuDaiChuanM : cl.ChieuDaiChuanM,
                SoTruc = tx.SoTruc > 0 ? tx.SoTruc : cl.SoTruc,
                TuTrongTan = tx.TuTrongTan,
                TaiTrongTan = tx.TaiTrongToiDaTan,
                SucChua = cl.SucChuaThamKhao,
                LaToaHang = cl.LaToaHang,
                MauNen = cl.MauNen,
                MauVien = cl.MauVien,
                MauChu = cl.MauChu
            };
        }

        // Tao tu dong bien che toa khach cua chuyen (vantai.ToaXeKhach).
        // Bang nay khong co tu trong/tai trong nen lay gia tri tham khao theo chung loai.
        // Nhan "Toa 1 (AN)" se sai khi doi thu tu, nen dat so hieu theo ma bien che: AN-K01.
        public static ToaLapTau TuBienChe(ToaBienCheHienThi b)
        {
            var cl = DanhMucMauPhuongTien.TimChungLoai(b.LoaiToa);
            return new ToaLapTau
            {
                SoHieu = b.MaToaXeKhach > 0 ? $"{b.LoaiToa}-K{b.MaToaXeKhach:00}" : b.NhanHieuToa,
                GhiChu = $"Biên chế hiện tại của chuyến: {b.NhanHieuToa}",
                LoaiCode = b.LoaiToa,
                TenLoai = cl.TenMoTa,
                ChieuDaiM = b.ChieuDaiToaM > 0 ? b.ChieuDaiToaM : cl.ChieuDaiChuanM,
                SoTruc = cl.SoTruc,
                TuTrongTan = cl.TuTrongMacDinhTan,
                TaiTrongTan = cl.TaiTrongMacDinhTan,
                SucChua = b.SucChua > 0 ? b.SucChua : cl.SucChuaThamKhao,
                LaToaHang = cl.LaToaHang,
                MauNen = cl.MauNen,
                MauVien = cl.MauVien,
                MauChu = cl.MauChu
            };
        }

        // Tao tu danh muc mau
        public static ToaLapTau TuMau(string soHieu, string maCode)
        {
            var cl = DanhMucMauPhuongTien.TimChungLoai(maCode);
            return new ToaLapTau
            {
                SoHieu = soHieu,
                LoaiCode = cl.MaChungLoaiCode,
                TenLoai = cl.TenMoTa,
                ChieuDaiM = cl.ChieuDaiChuanM,
                SoTruc = cl.SoTruc,
                TuTrongTan = cl.TuTrongMacDinhTan,
                TaiTrongTan = cl.TaiTrongMacDinhTan,
                SucChua = cl.SucChuaThamKhao,
                LaToaHang = cl.LaToaHang,
                MauNen = cl.MauNen,
                MauVien = cl.MauVien,
                MauChu = cl.MauChu,
                LaDuLieuMau = true
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void BaoThayDoi([CallerMemberName] string? ten = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
    }

    // -------------------------------------------------------------------------
    // Dau may co the chon lam may keo chinh
    // -------------------------------------------------------------------------
    public class DauMayLapTau
    {
        public string SoHieu { get; set; } = "";
        public string MaDongCode { get; set; } = "";
        public string DonViQuanLy { get; set; } = "";
        public string TrangThai { get; set; } = "SAN_SANG";
        public decimal SucKeoTan { get; set; }
        public decimal ChieuDaiM { get; set; }
        public decimal TrongLuongTan { get; set; }

        public string NhanTrangThai => TrangThai switch
        {
            "SAN_SANG" => "Sẵn sàng",
            "DANG_CHAY" => "Đang chạy",
            "BAO_DUONG" => "Bảo dưỡng",
            _ => TrangThai
        };

        // So hieu da chua ma dong (D19E-901) nen khong lap lai ma dong cho gon
        public string NhanHienThi => $"{SoHieu}  ·  kéo {SucKeoTan:N0} tấn  ·  {NhanTrangThai}";

        public static DauMayLapTau TuDauMay(DauMayHienThi dm) => new()
        {
            SoHieu = dm.SoHieuDauMay,
            MaDongCode = dm.MaDongCode,
            DonViQuanLy = dm.DonViQuanLy,
            TrangThai = dm.TrangThai,
            SucKeoTan = dm.SucKeoToiDaTan,
            ChieuDaiM = dm.ChieuDaiM,
            TrongLuongTan = dm.TrongLuongTan
        };
    }

    // -------------------------------------------------------------------------
    // Du lieu dau vao cua dialog lap tau
    // -------------------------------------------------------------------------
    public class ThongTinLapTau
    {
        public int MaChuyenTau { get; set; }
        public string TenChuyen { get; set; } = "";
        public decimal? DuongTranhNganNhatM { get; set; }
        public string? SoHieuDauMayDangChon { get; set; }

        public List<DauMayLapTau> DanhSachDauMay { get; set; } = new();
        public List<ToaLapTau> DoanTauBanDau { get; set; } = new();
        public List<ToaLapTau> BaiToa { get; set; } = new();
    }

    // -------------------------------------------------------------------------
    // Ket qua tra ve khi bam "Xac nhan lap tau"
    // -------------------------------------------------------------------------
    public class KetQuaLapTau
    {
        public DauMayLapTau? DauMay { get; set; }
        public List<ToaLapTau> DanhSachToa { get; set; } = new();
        public decimal TongChieuDaiM { get; set; }
        public decimal TongTrongLuongTan { get; set; }
        public int TongSucChua { get; set; }
        public List<HangMucAnToanHienThi> HangMuc { get; set; } = new();
    }
}
