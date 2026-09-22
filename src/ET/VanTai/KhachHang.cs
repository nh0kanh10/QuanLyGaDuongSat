namespace ET.VanTai
{
    public class KhachHang
    {
        public int MaKhachHang { get; set; }
        public string HoTen { get; set; } = "";
        public string SoCCCD { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public string? Email { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string LoaiKhach { get; set; } = "THUONG";
    }
}
