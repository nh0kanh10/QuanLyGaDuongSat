namespace DTO.VanTai
{
    // Hien thi van don hang hoa kem ten ga, ten loai hang
    public class VanDonHienThi
    {
        public int MaVanDon { get; set; }
        public string MaVanDonCode { get; set; } = "";
        public string TenNguoiGui { get; set; } = "";
        public string TenNguoiNhan { get; set; } = "";
        public string TenGaGui { get; set; } = "";
        public string TenGaNhan { get; set; } = "";
        public string TenLoaiHang { get; set; } = "";
        public int NhomCuoc { get; set; }
        public decimal TrongLuongTan { get; set; }
        public decimal CuocPhi { get; set; }
        public string LoaiVanChuyen { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public DateTime ThoiDiemTao { get; set; }
    }
}
