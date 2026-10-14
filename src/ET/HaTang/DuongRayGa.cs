namespace ET.HaTang
{
    public class DuongRayGa
    {
        public int MaDuongRay { get; set; }
        public int MaGa { get; set; }
        public int SoHieuDuong { get; set; }
        public string LoaiDuong { get; set; } = "CHINH_TUYEN";
        public int ChieuDaiHuuDungM { get; set; } = 450;
        public bool CoKeGa { get; set; } = true;
        public string TrangThai { get; set; } = "TRONG";
        public System.DateTime NgayTao { get; set; } = System.DateTime.UtcNow;
        public string NguoiTao { get; set; } = "admin";
        public System.DateTime? NgayCapNhat { get; set; }
        public string? NguoiCapNhat { get; set; }
    }
}
