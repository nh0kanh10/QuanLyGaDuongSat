namespace ET.VanTai
{
    public class Ve
    {
        public int MaVe { get; set; }
        public int MaDonVe { get; set; }
        public string MaVeCode { get; set; } = "";
        public int MaChuyenTau { get; set; }
        public int MaChoNgoi { get; set; }
        public int MaGaDi { get; set; }
        public int MaGaDen { get; set; }
        public string TenHanhKhach { get; set; } = "";
        public string CCCDHanhKhach { get; set; } = "";
        public decimal GiaVeGoc { get; set; }
        public decimal SoTienGiam { get; set; }
        public decimal GiaVeThucThu { get; set; }
        public string TrangThai { get; set; } = "DA_DAT";
        public DateTime ThoiDiemXuatVe { get; set; }
    }
}
