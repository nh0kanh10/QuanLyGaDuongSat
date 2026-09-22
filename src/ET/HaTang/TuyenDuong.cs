namespace ET.HaTang
{
    public class TuyenDuong
    {
        public int MaTuyen { get; set; }
        public string MaTuyenCode { get; set; } = "";
        public string TenTuyen { get; set; } = "";
        public decimal TongChieuDaiKm { get; set; }
        public int KhoRayMm { get; set; } = 1000;
        public bool DangKhaiThac { get; set; } = true;
    }
}
