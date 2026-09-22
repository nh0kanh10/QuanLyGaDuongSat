namespace DTO.HaTang
{
    // Hien thi khu gian kem ten ga dau + ga cuoi
    public class KhuGianHienThi
    {
        public int MaKhuGian { get; set; }
        public int MaGaDau { get; set; }
        public string TenGaDau { get; set; } = "";
        public int MaGaCuoi { get; set; }
        public string TenGaCuoi { get; set; } = "";
        public decimal CuLyKm { get; set; }
        public int TocDoToiDaKhach { get; set; }
        public int TocDoToiDaHang { get; set; }
        public decimal DoDocPermil { get; set; }
        public bool CanDauMayDay { get; set; }
        public string TrangThai { get; set; } = "";
    }
}
