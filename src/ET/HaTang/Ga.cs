namespace ET.HaTang
{
    public class Ga
    {
        public int MaGa { get; set; }
        public int MaTuyen { get; set; }
        public string MaGaCode { get; set; } = "";
        public string TenGa { get; set; } = "";
        public decimal LyTrinhKm { get; set; }
        public string TinhThanh { get; set; } = "";
        public string HangGa { get; set; } = "HANG_3";
        public bool CoCauQuay { get; set; }
        public bool DangKhaiThac { get; set; } = true;
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public string NguoiTao { get; set; } = "admin";
        public DateTime? NgayCapNhat { get; set; }
        public string? NguoiCapNhat { get; set; }
    }
}
