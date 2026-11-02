namespace DTO.BaoTri
{
    // =========================================================================
    // CHI PHI & HIEU QUA THEO MAC TAU (tab 5 trang Bao tri, 26/10).
    // Doanh thu doc tu bang cua A Ba (vantai.Ve, HoanHuyVe, ToaXeKhach, VanDonHang - CHI DOC), chi phi
    // tu 3 bang baotri. Tong hop + khuyen nghi o BUS (QuyTacChiPhi), mau / bieu tuong o GUI.
    // Quyet dinh 26/10 (Dat chot): gom theo MAC TAU · doanh thu ghi nhan theo NGAY TAU CHAY ·
    // doanh thu = thuc thu - tien da hoan (nhu bao cao ban ve cua A Ba), KHONG tinh ve DA_HUY (ve cu
    // khi doi ghe, da thay bang ve moi cung gia).
    // =========================================================================

    // Mac tau (vanhanh.MacTauMau JOIN hatang.Ga)
    public class MacTauBaoTriDTO
    {
        public int MaMacTau { get; init; }
        public string SoHieuMacTau { get; init; } = "";
        public string LoaiTau { get; init; } = "";          // TAU_NHANH | TAU_CHO | TAU_HANG
        public string TenGaDi { get; init; } = "";
        public string TenGaDen { get; init; } = "";
        public bool DangKhaiThac { get; init; }

        public string HanhTrinh => $"{TenGaDi} – {TenGaDen}";
        public bool LaTauHang => LoaiTau == "TAU_HANG";
        public string NhanLoaiTau => LoaiTau switch
        {
            "TAU_NHANH" => "Tàu nhanh",
            "TAU_CHO" => "Tàu chợ",
            "TAU_HANG" => "Tàu hàng",
            _ => LoaiTau
        };
    }

    // Doanh thu ve cua mot chuyen tau (tong hop vantai.Ve + HoanHuyVe, toa ban ve ToaXeKhach)
    public class DoanhThuChuyenDTO
    {
        public int MaChuyenTau { get; init; }
        public string SoHieuMacTau { get; init; } = "";
        public DateTime NgayXuatPhat { get; init; }
        public DateTime GioXuatPhatKH { get; init; }
        public string TrangThaiChuyen { get; init; } = "";
        public int SoGhe { get; init; }             // tong suc chua toa ban ve cua chuyen
        public int SoVeHieuLuc { get; init; }       // DA_DAT + DA_LEN_TAU
        public int SoVeHoan { get; init; }          // DA_HOAN_VE
        public int SoVeHuy { get; init; }           // DA_HUY - khong tinh doanh thu
        public decimal ThucThu { get; init; }       // gia thuc thu cua ve hieu luc + ve da hoan
        public decimal TienHoan { get; init; }      // tien da tra lai khach (HoanHuyVe.SoTienThucHoan)

        public decimal DoanhThuThuan => ThucThu - TienHoan;
        public decimal? TyLeLapDay => SoGhe > 0 ? Math.Round((decimal)SoVeHieuLuc / SoGhe * 100m, 1) : null;
    }

    // Van don hang hoa (chi doc). Chua gan chuyen / toa nen chi cong tong theo ky, khong chia theo mac.
    public record VanDonDoanhThuDTO(int MaVanDon, DateTime ThoiDiemTao, decimal CuocPhi);

    // Mot khoan chi phi da chi trong ky. SoTien = phan gan cho mac nay (SoPhan / TongSoPhan cua SoTienGoc).
    // Nguon: KHAM_XE | NHIEN_LIEU | BAO_DUONG
    public record KhoanChiPhiDTO(string Nguon, string MaPhieu, string MoTa, decimal SoTien, decimal SoTienGoc,
                                 int SoPhan, int TongSoPhan, DateTime ThoiDiem)
    {
        public bool DuocChia => TongSoPhan > 1 && SoPhan < TongSoPhan;
    }

    // Chi phi du phong: dau may dang sap / da den han bao duong, uoc theo chi phi tham khao cua cap do
    public record KhoanDuPhongDTO(string SoHieuPhuongTien, string Cap, string TrangThai, string MoTa,
                                  decimal SoTienDuKien);

    // Hieu qua cua mot mac tau trong ky. MaKhuyenNghi: THU_HEP | DUY_TRI | MO_RONG | CHUA_DU
    public class HieuQuaMacTauDTO
    {
        public MacTauBaoTriDTO MacTau { get; init; } = null!;
        public List<DoanhThuChuyenDTO> ChuyenTrongKy { get; init; } = new();
        public List<KhoanChiPhiDTO> DaChi { get; init; } = new();
        public List<KhoanDuPhongDTO> DuPhong { get; init; } = new();
        public string MaKhuyenNghi { get; set; } = "";
        public string LyDoChuaDu { get; set; } = "";

        public int SoChuyen => ChuyenTrongKy.Count;
        public int SoGhe => ChuyenTrongKy.Sum(c => c.SoGhe);
        public int SoVe => ChuyenTrongKy.Sum(c => c.SoVeHieuLuc);
        public int SoVeHoan => ChuyenTrongKy.Sum(c => c.SoVeHoan);
        public decimal ThucThu => ChuyenTrongKy.Sum(c => c.ThucThu);
        public decimal TienHoan => ChuyenTrongKy.Sum(c => c.TienHoan);
        public decimal DoanhThu => ChuyenTrongKy.Sum(c => c.DoanhThuThuan);
        public decimal? TyLeLapDay => SoGhe > 0 ? Math.Round((decimal)SoVe / SoGhe * 100m, 1) : null;

        public decimal ChiPhiDaChi => DaChi.Sum(c => c.SoTien);
        public decimal ChiPhiDuPhong => DuPhong.Sum(c => c.SoTienDuKien);
        public decimal TongChiPhi => ChiPhiDaChi + ChiPhiDuPhong;
        public decimal LoiNhuan => DoanhThu - TongChiPhi;

        // null khi chua co doanh thu (khong chia duoc)
        public decimal? BienLoiNhuan => DoanhThu > 0 ? Math.Round(LoiNhuan / DoanhThu * 100m, 1) : null;
        public bool CoDuLieuChiPhi => DaChi.Count + DuPhong.Count > 0;
    }

    // Ket qua tong hop ca ky: TuNgay = 00:00 ngay dau ky, DenLuc = gio may chu luc doc
    public class KetQuaChiPhiDTO
    {
        public DateTime TuNgay { get; init; }
        public DateTime DenLuc { get; init; }
        public int SoNgay { get; init; }
        public List<HieuQuaMacTauDTO> MacTau { get; init; } = new();

        // Chi phi trong ky khong gan duoc cho mac nao (dau may khong keo doan tau nao, bao duong toa xe)
        public List<KhoanChiPhiDTO> ChuaPhanBo { get; init; } = new();

        // Tong chi phi tren MOI phieu trong ky = phan da gan cho cac mac + ChuaPhanBo
        public decimal ThucChi { get; init; }
        public int SoVanDon { get; init; }
        public decimal CuocHangHoa { get; init; }

        public decimal DaPhanBo => MacTau.Sum(m => m.ChiPhiDaChi);
        public decimal TongChuaPhanBo => ChuaPhanBo.Sum(c => c.SoTien);
    }
}
