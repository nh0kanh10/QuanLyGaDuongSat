namespace ET.VanHanh
{
    public class ChuyenTau
    {
        public int MaChuyenTau { get; set; }
        public int MaMacTau { get; set; }
        public DateTime NgayXuatPhat { get; set; }
        public DateTime GioXuatPhatKH { get; set; }
        public DateTime GioVeDichKH { get; set; }
        public DateTime? GioXuatPhatTT { get; set; }
        public DateTime? GioVeDichTT { get; set; }
        public int SoPhutTreLuyKe { get; set; }
        public string? LyDoHuy { get; set; }
        public string TrangThai { get; set; } = "DA_LEN_LICH";
    }
}
