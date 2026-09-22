namespace ET.VanTai
{
    public class VanDonHang
    {
        public int MaVanDon { get; set; }
        public string MaVanDonCode { get; set; } = "";
        public string TenNguoiGui { get; set; } = "";
        public string SDTNguoiGui { get; set; } = "";
        public string TenNguoiNhan { get; set; } = "";
        public string SDTNguoiNhan { get; set; } = "";
        public int MaGaGui { get; set; }
        public int MaGaNhan { get; set; }
        public int MaLoaiHang { get; set; }
        public decimal TrongLuongTan { get; set; }
        public decimal CuocPhi { get; set; }
        public int? MaToaXeHang { get; set; }
        public string LoaiVanChuyen { get; set; } = "HANG_HOA";
        public string? BienKiemSoat { get; set; }
        public bool? DaRutXang { get; set; }
        public string? SoHieuContainer { get; set; }
        public string? SoChiHaiQuan { get; set; }
        public string? GhiChu { get; set; }
        public string TrangThai { get; set; } = "DA_NHAN";
        public DateTime ThoiDiemTao { get; set; }
    }
}
