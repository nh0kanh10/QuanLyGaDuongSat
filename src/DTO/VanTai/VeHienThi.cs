namespace DTO.VanTai
{
    // Hien thi ve tau kem ten ga, so ghe, loai toa
    public class VeHienThi
    {
        public int MaVe { get; set; }
        public string MaVeCode { get; set; } = "";
        public string SoHieuMacTau { get; set; } = "";
        public string TenGaDi { get; set; } = "";
        public string TenGaDen { get; set; } = "";
        public string TenHanhKhach { get; set; } = "";
        public string CCCDHanhKhach { get; set; } = "";
        public int SoGhe { get; set; }
        public string LoaiToa { get; set; } = "";
        public string NhanHieuToa { get; set; } = "";
        public decimal GiaVeGoc { get; set; }
        public decimal SoTienGiam { get; set; }
        public decimal GiaVeThucThu { get; set; }
        public string TrangThai { get; set; } = "";
        public DateTime ThoiDiemXuatVe { get; set; }
    }
}
