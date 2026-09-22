namespace DTO.VanHanh
{
    public class MacTauHienThi
    {
        public int MaMacTau { get; set; }
        public string SoHieuMacTau { get; set; } = "";
        public string LoaiTau { get; set; } = "";
        public string HuongChay { get; set; } = "";
        public string TenGaDi { get; set; } = "";
        public string TenGaDen { get; set; } = "";
        public decimal ThoiGianChuanGio { get; set; }
        public int MucUuTien { get; set; }
        public bool DangKhaiThac { get; set; }
    }
}
