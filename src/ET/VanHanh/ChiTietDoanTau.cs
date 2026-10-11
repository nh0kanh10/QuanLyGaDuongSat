namespace ET.VanHanh
{
    // Trung cot vanhanh.ChiTietDoanTau (kem 2 cot cua migration V20261009_0820).
    // MaGaNoi NULL = toa co mat tu ga dau; MaGaCat NULL = toa chay toi ga cuoi.
    public class ChiTietDoanTau
    {
        public int MaChiTiet { get; set; }
        public int MaDoanTau { get; set; }
        public int MaToaXe { get; set; }
        public int ThuTuNoi { get; set; }
        public int? MaGaNoi { get; set; }
        public int? MaGaCat { get; set; }
    }
}
