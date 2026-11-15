using System;
using System.Collections.Generic;
using System.Linq;

namespace DTO.AI
{
    // =========================================================================
    // AI tu van ve (Sprint 3, Dat) - cham diem da tieu chi (MCDM), chay offline.
    // Dat chot 08/11 (Q9): 4 tieu chi cham diem nhu bang vi du cua SRS (tien nghi, ngan sach,
    // khung gio / ngu dem, suc khoe & doi tuong); vi tri ghe dung de chon ghe cu the;
    // xe may chi ghi chu (CSDL khong co toa hanh ly). Cau tu van sinh theo mau, khong dung mo hinh ngon ngu.
    // =========================================================================

    public static class MucNganSach
    {
        public const string TietKiem = "TIET_KIEM";
        public const string TieuChuan = "TIEU_CHUAN";
        public const string CaoCap = "CAO_CAP";
    }

    public static class KhungGioUuTien
    {
        public const string KhongUuTien = "KHONG";
        public const string TranhDiDem = "TRANH_DEM";      // muon di / den ban ngay
        public const string NguTrenTau = "NGU_TREN_TAU";   // muon di dem, ngu tren tau
    }

    public sealed class YeuCauTuVanVe
    {
        public int MaGaDi { get; set; }
        public int MaGaDen { get; set; }
        public DateTime NgayDi { get; set; }
        public int SoNguoi { get; set; } = 1;
        public bool CoNguoiCaoTuoi { get; set; }
        public bool CoTreEm { get; set; }
        public bool LaSinhVien { get; set; }
        public string MucNganSach { get; set; } = AI.MucNganSach.TieuChuan;
        public decimal? NganSachToiDaMoiNguoi { get; set; }
        public string KhungGio { get; set; } = KhungGioUuTien.KhongUuTien;
        public bool MangXeMay { get; set; }
        // Trong so nguoi dung chinh (null = tu dong theo ho so khach). Thu tu: tien nghi, ngan sach, khung gio, suc khoe.
        public double[]? TrongSo { get; set; }
    }

    // Mot phuong an = (chuyen, loai cho, tang giuong) con du cho cho ca doan tren chang.
    public sealed class PhuongAnVe
    {
        public int MaChuyenTau { get; set; }
        public string SoHieuMacTau { get; set; } = "";
        public string LoaiTau { get; set; } = "";
        public int MaToaXeKhach { get; set; }
        public string NhanHieuToa { get; set; } = "";
        public string LoaiToa { get; set; } = "";       // NC / NML / BN / AN
        public int? Tang { get; set; }                  // null = ghe ngoi
        public List<int> CacMaChoNgoi { get; set; } = new();
        public List<int> CacSoGhe { get; set; } = new();
        public int SoChoTrongCungLoai { get; set; }
        public decimal GiaMotVe { get; set; }                     // gia goc (chua giam) 1 ve
        // [12/11] Loai khach du kien cho tung cho (THUONG / SINH_VIEN / NGUOI_GIA / TRE_EM) va gia sau giam tuong ung:
        // ngan sach "toi da moi ve" so voi gia SAU GIAM, gio ve nhan dung loai khach (truoc day de "Nguoi lon 100%").
        public List<string> CacLoaiKhach { get; set; } = new();
        public List<decimal> CacGiaSauGiam { get; set; } = new();
        public decimal GiaCaoNhatSauGiam => CacGiaSauGiam.Count > 0 ? CacGiaSauGiam.Max() : GiaMotVe;
        public decimal TongTien => CacGiaSauGiam.Count > 0 ? CacGiaSauGiam.Sum() : GiaMotVe * CacMaChoNgoi.Count;
        public bool CoGiamGia => TongTien < GiaMotVe * CacMaChoNgoi.Count;
        public DateTime GioDi { get; set; }
        public DateTime GioDen { get; set; }
        public double SoGioChay => Math.Max(0, (GioDen - GioDi).TotalHours);
        public decimal CuLyKm { get; set; }
        public bool QuaDem { get; set; }
        public bool VuotNganSach { get; set; }

        public double[] DiemTieuChi { get; set; } = new double[4];
        public double Diem { get; set; }
        public int XepHang { get; set; }
        public List<string> LyDo { get; set; } = new();

        public string TenLoaiCho => LoaiToa switch
        {
            "AN" => "Giường nằm khoang 4",
            "BN" => "Giường nằm khoang 6",
            "NML" => "Ghế ngồi mềm điều hòa",
            "NC" => "Ghế ngồi cứng",
            _ => LoaiToa
        } + (Tang.HasValue ? $" · tầng {Tang}" : "");
        public string NhanGhe => (NhanHieuToa.StartsWith("Toa", StringComparison.OrdinalIgnoreCase) ? NhanHieuToa : $"Toa {NhanHieuToa}") + " · "
            + (Tang.HasValue ? "giường " : "ghế ") + string.Join(", ", CacSoGhe);
    }

    public sealed class KetQuaTuVanVe
    {
        public List<PhuongAnVe> CacPhuongAn { get; set; } = new();   // toi da 3, da xep hang
        public int SoPhuongAnDaXet { get; set; }
        public int SoChuyenDaXet { get; set; }
        public double[] TrongSoDaDung { get; set; } = new double[4];
        public List<string> GhiChu { get; set; } = new();
        public string TenGaDi { get; set; } = "";
        public string TenGaDen { get; set; } = "";
        public decimal CuLyKm { get; set; }
        public string CauTraLoi { get; set; } = "";      // ban van tom tat (ghi NhatKyChatAI)
    }

    // Ket qua tra ve cho trang Ban ve khi bam "Chon phuong an nay"
    public sealed record PhuongAnDuocChon(int MaChuyenTau, int MaGaDi, int MaGaDen, DateTime NgayDi,
                                          int MaToaXeKhach, string LoaiToa, IReadOnlyList<int> CacMaChoNgoi,
                                          IReadOnlyList<string>? CacLoaiKhach = null);
}
