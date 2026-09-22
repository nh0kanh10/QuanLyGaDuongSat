namespace DTO.VanHanh
{
    public class LichDungGaHienThi
    {
        public int MaDiemDung { get; set; }
        public int MaChuyenTau { get; set; }
        public string TenGa { get; set; } = "";
        public decimal LyTrinhKm { get; set; }
        public int ThuTuDung { get; set; }
        public DateTime GioDenKeHoach { get; set; }
        public DateTime GioDiKeHoach { get; set; }
        public DateTime? GioDenThucTe { get; set; }
        public DateTime? GioDiThucTe { get; set; }
        public bool LaDiemTranh { get; set; }
    }
}
