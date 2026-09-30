namespace DTO.VanHanh
{
    /// <summary>
    /// Điểm trạm dừng/thông qua của chuyến tàu trên Biểu đồ chạy tàu
    /// </summary>
    public class DiemChayTauDTO
    {
        public int MaChuyenTau { get; set; }
        public string SoHieuMacTau { get; set; } = "";
        public string LoaiTau { get; set; } = "TAU_NHANH";
        public string HuongChay { get; set; } = "BAC_NAM";
        public int MucUuTien { get; set; } = 1;
        public int ThuTuDung { get; set; }
        public int MaGa { get; set; }
        public string TenGa { get; set; } = "";
        public string MaGaCode { get; set; } = "";
        public decimal LyTrinhKm { get; set; }
        public DateTime GioDen { get; set; }
        public DateTime GioDi { get; set; }
        public int ThoiGianDungPhut { get; set; }
        public bool LaDiemTranh { get; set; }
        public int? MaDuongRay { get; set; }
        public int? SoHieuDuongRay { get; set; }
        public string? LoaiDuong { get; set; }
    }

    /// <summary>
    /// Toàn bộ hành trình của một chuyến tàu trong ngày trên Biểu đồ Marey
    /// </summary>
    public class HanhTrinhTauDTO
    {
        public int MaChuyenTau { get; set; }
        public string SoHieuMacTau { get; set; } = "";
        public string LoaiTau { get; set; } = "TAU_NHANH";
        public string HuongChay { get; set; } = "BAC_NAM";
        public int MucUuTien { get; set; } = 1;
        public decimal TongChieuDaiM { get; set; } = 250.0m;
        public string TrangThai { get; set; } = "DA_LEN_LICH";
        public string MauSacHex { get; set; } = "#EF4444";
        public DateTime GioXuatPhat { get; set; }
        public DateTime GioVeDich { get; set; }
        public string TenGaDi { get; set; } = "";
        public string TenGaDen { get; set; } = "";
        public List<DiemChayTauDTO> DanhSachDiem { get; set; } = new();
    }

    /// <summary>
    /// Bản ghi cảnh báo xung đột an toàn khu gian trên đường sắt đơn
    /// </summary>
    public class XungDotKhuGianDTO
    {
        public int MaXungDot { get; set; }
        public int MaChuyenTau1 { get; set; }
        public string SoHieuMacTau1 { get; set; } = "";
        public string LoaiTau1 { get; set; } = "";
        public int MucUuTien1 { get; set; } = 1;

        public int MaChuyenTau2 { get; set; }
        public string SoHieuMacTau2 { get; set; } = "";
        public string LoaiTau2 { get; set; } = "";
        public int MucUuTien2 { get; set; } = 2;

        /// <summary>
        /// DOI_DAU (Head-on - ngược chiều) hoặc GIAN_CACH (Headway - cùng chiều đuổi nhau)
        /// </summary>
        public string LoaiXungDot { get; set; } = "DOI_DAU";

        public int MaGaDau { get; set; }
        public string TenGaDau { get; set; } = "";
        public decimal LyTrinhDauKm { get; set; }

        public int MaGaCuoi { get; set; }
        public string TenGaCuoi { get; set; } = "";
        public decimal LyTrinhCuoiKm { get; set; }

        public DateTime ThoiDiemXungDot { get; set; }
        public decimal LyTrinhUocTinhKm { get; set; }
        public string MoTaChiTiet { get; set; } = "";

        // Thông tin gợi ý tự động giải quyết tránh tàu
        public string SoHieuTauUuTien { get; set; } = "";
        public int MucUuTienTauUuTien { get; set; } = 1;
        public int MaChuyenTauBiTranh { get; set; }
        public string SoHieuMacTauBiTranh { get; set; } = "";
        public string SoHieuTauBiTranh { get; set; } = "";
        public int MucUuTienTauBiTranh { get; set; } = 2;
        public int MaGaDeXuatTranh { get; set; }
        public string TenGaDeXuatTranh { get; set; } = "";
        public decimal LyTrinhGaTranhKm { get; set; }
        public DateTime GioDenGaTranh { get; set; }
        public DateTime GioDiGaTranh { get; set; }
        public int SoPhutDungDeXuat { get; set; } = 15;
        public int SoPhutTangLichTrinh { get; set; } = 15;
        public int? MaDuongRayDeXuat { get; set; }
        public int? SoHieuDuongRayDeXuat { get; set; }
        public bool DaGiaiQuyet { get; set; } = false;
    }
}
