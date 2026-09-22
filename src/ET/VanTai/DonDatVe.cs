namespace ET.VanTai
{
    public class DonDatVe
    {
        public int MaDonVe { get; set; }
        public string MaPNR { get; set; } = "";
        public int MaKhachHang { get; set; }
        public int SoLuongVe { get; set; }
        public decimal TongTien { get; set; }
        public string HinhThucTT { get; set; } = "TIEN_MAT";
        public string TrangThaiTT { get; set; } = "DA_THANH_TOAN";
        public int MaNhanVienBan { get; set; }
        public DateTime ThoiDiemTao { get; set; }
    }
}
