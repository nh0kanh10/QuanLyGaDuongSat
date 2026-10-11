namespace ET.PhuongTien
{
    // Trung cot phuongtien.ToaXe (toa xe vat ly trong doi)
    public class ToaXe
    {
        public int MaToaXe { get; set; }
        public string SoHieuToaXe { get; set; } = string.Empty;
        public int MaChungLoai { get; set; }
        public decimal TuTrongTan { get; set; }
        public decimal TaiTrongToiDaTan { get; set; }
        public string TrangThai { get; set; } = "SAN_SANG";
    }
}
