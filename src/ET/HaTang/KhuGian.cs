namespace ET.HaTang
{
    public class KhuGian
    {
        public int MaKhuGian { get; set; }
        public int MaGaDau { get; set; }
        public int MaGaCuoi { get; set; }
        public decimal CuLyKm { get; set; }
        public int TocDoToiDaKhach { get; set; } = 80;
        public int TocDoToiDaHang { get; set; } = 50;
        public decimal DoDocPermil { get; set; }
        public bool CanDauMayDay { get; set; }
        public string TrangThai { get; set; } = "RONG";
    }
}
