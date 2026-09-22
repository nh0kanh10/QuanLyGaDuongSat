namespace DTO.VanHanh
{
    public class ChuyenTauHienThi
    {
        public int MaChuyenTau { get; set; }
        public string SoHieuMacTau { get; set; } = "";
        public string LoaiTau { get; set; } = "";
        public DateTime NgayXuatPhat { get; set; }
        public DateTime GioXuatPhatKH { get; set; }
        public DateTime GioVeDichKH { get; set; }
        public DateTime? GioXuatPhatTT { get; set; }
        public DateTime? GioVeDichTT { get; set; }
        public int SoPhutTreLuyKe { get; set; }
        public string TrangThai { get; set; } = "";
        public string TenGaDi { get; set; } = "";
        public string TenGaDen { get; set; } = "";
    }
}
