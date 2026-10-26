using DTO.BaoTri;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.ViewModels.KyThuat
{
    // =========================================================================
    // BAO TRI - TAB 5: CHI PHI & HIEU QUA THEO MAC TAU
    //
    // Tu 26/10 la so lieu THAT: BUS.Services.QuyTacChiPhi tong hop chi phi 3 bang baotri + doanh thu ve
    // (vantai.Ve, bang cua A Ba) trong ky; lop o day chi them nhan / mau / bieu tuong cho XAML.
    // (Truoc 26/10: 5 tuyen + doanh thu mau tinh trong ma - da bo theo quyet dinh gom theo mac tau.)
    // =========================================================================

    // Mot khoan chi phi da chi (phieu kham xe / cap dau / bao duong), SoTien = phan cua mac tau nay
    public class KhoanChiPhiHienThi
    {
        public KhoanChiPhiHienThi(KhoanChiPhiDTO goc) => Goc = goc;

        public KhoanChiPhiDTO Goc { get; }
        public string MaPhieu => Goc.MaPhieu;
        public string MoTa => Goc.MoTa;
        public decimal SoTien => Goc.SoTien;
        public DateTime ThoiDiem => Goc.ThoiDiem;

        public SymbolRegular BieuTuong => Goc.Nguon switch
        {
            "KHAM_XE" => SymbolRegular.ClipboardTaskListLtr24,
            "NHIEN_LIEU" => SymbolRegular.GasPump24,
            _ => SymbolRegular.WrenchScrewdriver24
        };

        public string NhanPhanBo => Goc.DuocChia ? $" · phân bổ {Goc.SoPhan}/{Goc.TongSoPhan} của {Goc.SoTienGoc:N0} đ" : "";
    }

    // Mot khoan chi phi du phong (dau may sap / da den han bao duong)
    public class KhoanDuPhongHienThi
    {
        public KhoanDuPhongHienThi(KhoanDuPhongDTO goc) => Goc = goc;

        public KhoanDuPhongDTO Goc { get; }
        public string SoHieuPhuongTien => Goc.SoHieuPhuongTien;
        public string Cap => Goc.Cap;
        public string MoTa => Goc.MoTa;
        public decimal SoTienDuKien => Goc.SoTienDuKien;
        public string MauChu => Goc.TrangThai == "QUA_HAN" ? "#B91C1C" : "#B45309";
    }

    // Doanh thu ve cua mot chuyen trong ky
    public class ChuyenDoanhThuHienThi
    {
        public ChuyenDoanhThuHienThi(DoanhThuChuyenDTO goc) => Goc = goc;

        public DoanhThuChuyenDTO Goc { get; }
        public decimal DoanhThu => Goc.DoanhThuThuan;

        public string TieuDe => $"{Goc.SoHieuMacTau} · chạy {Goc.NgayXuatPhat:dd/MM} · xuất phát {Goc.GioXuatPhatKH:HH:mm dd/MM}";

        public string MoTa
        {
            get
            {
                if (Goc.SoGhe == 0) return "Chưa có toa bán vé";
                string moTa = $"{Goc.SoVeHieuLuc:N0} vé / {Goc.SoGhe:N0} ghế ({Goc.TyLeLapDay:N0}%)";
                if (Goc.SoVeHoan > 0) moTa += $" · {Goc.SoVeHoan} vé hoàn (giữ lệ phí)";
                if (Goc.SoVeHuy > 0) moTa += $" · {Goc.SoVeHuy} vé đổi ghế không tính";
                return moTa;
            }
        }
    }

    public class MacTauChiPhiHienThi
    {
        public MacTauChiPhiHienThi(HieuQuaMacTauDTO goc, int soNgayKy)
        {
            Goc = goc;
            SoNgayKy = soNgayKy;
            ChiTietDaChi = goc.DaChi.OrderByDescending(c => c.ThoiDiem).Select(c => new KhoanChiPhiHienThi(c)).ToList();
            ChiTietDuPhong = goc.DuPhong.Select(c => new KhoanDuPhongHienThi(c)).ToList();
            ChuyenTrongKy = goc.ChuyenTrongKy.OrderByDescending(c => c.GioXuatPhatKH).Select(c => new ChuyenDoanhThuHienThi(c)).ToList();
        }

        public HieuQuaMacTauDTO Goc { get; }
        public int SoNgayKy { get; }
        public List<KhoanChiPhiHienThi> ChiTietDaChi { get; }
        public List<KhoanDuPhongHienThi> ChiTietDuPhong { get; }
        public List<ChuyenDoanhThuHienThi> ChuyenTrongKy { get; }
        public bool DangChon { get; set; }

        public string SoHieuMacTau => Goc.MacTau.SoHieuMacTau;
        public string TieuDe => $"{SoHieuMacTau} · {Goc.MacTau.NhanLoaiTau}";
        public string MoTaNgan => $"{Goc.MacTau.HanhTrinh} · {MoTaChuyen}";
        public string MoTaChuyen => Goc.SoChuyen == 0 ? "chưa có chuyến trong kỳ"
            : $"{Goc.SoChuyen} chuyến · {Goc.SoVe:N0} vé" + (Goc.TyLeLapDay is { } t ? $" ({t:N0}% ghế)" : "");

        public decimal DoanhThu => Goc.DoanhThu;
        public decimal ChiPhiDaChi => Goc.ChiPhiDaChi;
        public decimal ChiPhiDuPhong => Goc.ChiPhiDuPhong;
        public decimal TongChiPhiBaoTri => Goc.TongChiPhi;
        public decimal LoiNhuan => Goc.LoiNhuan;
        public bool CoLai => LoiNhuan >= 0;

        public string MaKhuyenNghi => Goc.MaKhuyenNghi;
        public bool ChuaDuDuLieu => MaKhuyenNghi == "CHUA_DU";
        public bool CanXemXet => MaKhuyenNghi == "THU_HEP";

        public string NhanKhuyenNghi => MaKhuyenNghi switch
        {
            "THU_HEP" => "ĐỀ XUẤT: CÂN NHẮC DỪNG / THU HẸP MÁC TÀU",
            "MO_RONG" => "ĐỀ XUẤT: MỞ RỘNG THÊM CHUYẾN",
            "CHUA_DU" => "CHƯA ĐỦ DỮ LIỆU TRONG KỲ ĐỂ ĐỀ XUẤT",
            _ => "DUY TRÌ, THEO DÕI THÊM"
        };

        // Nhan ngan tren the: bien loi nhuan; "—" khi chua du du lieu; "lỗ" khi co ghe ma chua thu dong nao
        public string NhanBien => ChuaDuDuLieu ? "—" : Goc.BienLoiNhuan is { } b ? $"{b:N1}%" : "lỗ";

        public string MauKhuyenNghi => MaKhuyenNghi switch
        {
            "THU_HEP" => "#B91C1C",
            "MO_RONG" => "#15803D",
            "CHUA_DU" => "#475569",
            _ => "#B45309"
        };
        public string NenKhuyenNghi => MaKhuyenNghi switch
        {
            "THU_HEP" => "#FEF2F2",
            "MO_RONG" => "#F0FDF4",
            "CHUA_DU" => "#F8FAFC",
            _ => "#FFFBEB"
        };
        public string VienKhuyenNghi => MaKhuyenNghi switch
        {
            "THU_HEP" => "#FECACA",
            "MO_RONG" => "#BBF7D0",
            "CHUA_DU" => "#E2E8F0",
            _ => "#FDE68A"
        };

        // Chua du du lieu: so loi nhuan chi de tham khao -> mau xam, khong bao dong do
        public string MauLoiNhuan => ChuaDuDuLieu ? "#475569" : CoLai ? "#15803D" : "#B91C1C";
        public string MauVien => DangChon ? "#003B73" : "#E2E8F0";
        public double DoDayVien => DangChon ? 2 : 1;

        public SymbolRegular BieuTuongKhuyenNghi => MaKhuyenNghi switch
        {
            "THU_HEP" => SymbolRegular.Warning24,
            "MO_RONG" => SymbolRegular.ArrowTrendingLines24,
            "CHUA_DU" => SymbolRegular.QuestionCircle24,
            _ => SymbolRegular.Info24
        };
    }
}
