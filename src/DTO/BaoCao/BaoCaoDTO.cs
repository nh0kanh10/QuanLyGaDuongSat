using System;
using System.Collections.Generic;

namespace DTO.BaoCao
{
    // =========================================================================
    // Bao cao thong ke tong hop (Sprint 3, Dat). Nguon: cac view baocao.vw_* (migration V20261108_1300).
    // Doanh thu / ghe / dung gio ghi nhan theo NGAY TAU CHAY; van don theo ngay tao.
    // =========================================================================
    public sealed class BoLocBaoCao
    {
        public DateTime TuNgay { get; set; } = DateTime.Today.AddDays(-13);
        public DateTime DenNgay { get; set; } = DateTime.Today;
        public string? SoHieuMacTau { get; set; }          // null = moi mac
    }

    public sealed class TongQuanBaoCao
    {
        public int SoChuyen { get; set; }
        public decimal DoanhThuThuan { get; set; }
        public int SoVe { get; set; }
        public int SoVeHoan { get; set; }
        public double TyLeHoan => SoVe == 0 ? 0 : (double)SoVeHoan / SoVe;
        public double HeSoGhe { get; set; }
        public int SoChuyenCoGio { get; set; }
        public int SoChuyenDungGio { get; set; }
        public double TyLeDungGio => SoChuyenCoGio == 0 ? 0 : (double)SoChuyenDungGio / SoChuyenCoGio;
        public double TreTrungBinh { get; set; }
        public int SoVanDon { get; set; }
        public decimal SanLuongTan { get; set; }
        public decimal TanKm { get; set; }
        public decimal CuocHang { get; set; }
    }

    public sealed class DongMacTau
    {
        public string SoHieuMacTau { get; set; } = "";
        public int SoChuyen { get; set; }
        public int SoVe { get; set; }
        public decimal DoanhThu { get; set; }
        public double HeSoGhe { get; set; }
        public int SoChuyenCoGio { get; set; }
        public int SoChuyenDungGio { get; set; }
        public double TyLeDungGio => SoChuyenCoGio == 0 ? 0 : (double)SoChuyenDungGio / SoChuyenCoGio;
        public double TreTrungBinh { get; set; }
        public int TreLonNhat { get; set; }
    }

    public sealed class DongNgay
    {
        public DateTime Ngay { get; set; }
        public decimal DoanhThu { get; set; }
        public int SoVe { get; set; }
    }

    public sealed class DongGa
    {
        public string TenGa { get; set; } = "";
        public int SoVe { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public sealed class DongChuyen
    {
        public int MaChuyenTau { get; set; }
        public DateTime NgayChay { get; set; }
        public string SoHieuMacTau { get; set; } = "";
        public decimal CuLyKm { get; set; }
        public int SoGhe { get; set; }
        public int SoVe { get; set; }
        public double HeSo { get; set; }
        public int? SoPhutTre { get; set; }
        public int SoSuCo { get; set; }
    }

    public sealed class DongHangHoa
    {
        public string TenHangHoa { get; set; } = "";
        public int SoVanDon { get; set; }
        public decimal Tan { get; set; }
        public decimal TanKm { get; set; }
        public decimal Cuoc { get; set; }
    }

    public sealed class KetQuaBaoCao
    {
        public BoLocBaoCao BoLoc { get; set; } = new();
        public TongQuanBaoCao TongQuan { get; set; } = new();
        public List<DongMacTau> TheoMacTau { get; set; } = new();
        public List<DongNgay> TheoNgay { get; set; } = new();
        public List<DongGa> TheoGa { get; set; } = new();
        public List<DongChuyen> TheoChuyen { get; set; } = new();
        public List<DongHangHoa> HangHoa { get; set; } = new();
    }
}
