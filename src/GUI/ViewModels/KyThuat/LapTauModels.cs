using GUI.Helpers;
using BUS.Services;
using DTO.PhuongTien;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GUI.ViewModels.KyThuat
{
    // =========================================================================
    // Cac lop HIEN THI cua man hinh LAP TAU KEO - THA.
    // Du lieu lay tu LapTauService.LayBoiCanh (vanhanh.DoanTau + ChiTietDoanTau,
    // LichDungGa, DauMay, ToaXe). Moi luat an toan nam o BUS.Services.QuyTacLapTau;
    // cac lop nay chi giu trang thai keo - tha va thuoc tinh de binding.
    // =========================================================================

    // -------------------------------------------------------------------------
    // Mot toa xe tren man hinh lap tau (o bai toa hoac trong doan tau)
    // -------------------------------------------------------------------------
    public class ToaLapTau : INotifyPropertyChanged
    {
        public int MaToaXe { get; set; }
        public string SoHieu { get; set; } = "";
        public string LoaiCode { get; set; } = "";
        public string TenLoai { get; set; } = "";
        public decimal ChieuDaiM { get; set; }
        public int SoTruc { get; set; } = 4;
        public decimal TuTrongTan { get; set; }
        public decimal TaiTrongTan { get; set; }
        public int SucChua { get; set; }
        public bool LaToaHang { get; set; }

        public string MauNen { get; set; } = "#F1F5F9";
        public string MauVien { get; set; } = "#CBD5E1";
        public string MauChu { get; set; } = "#475569";

        // Dong ghi chu nguon goc hien o cuoi tooltip
        public string GhiChu { get; set; } = "";

        private int _thuTu;
        public int ThuTu
        {
            get => _thuTu;
            set { if (_thuTu != value) { _thuTu = value; BaoThayDoi(); } }
        }

        public decimal TongTrongLuongTan => TuTrongTan + TaiTrongTan;

        public decimal TaiTrongTrucTan => PhuongTienService.TinhTaiTrongTruc(TuTrongTan, TaiTrongTan, SoTruc);

        public bool VuotTaiTrongTruc => TaiTrongTrucTan > QuyTacLapTau.TaiTrongTrucToiDaTan;

        public string MoTaNgan => $"{ChieuDaiM:N1} m · {TongTrongLuongTan:N1} t";

        public string NhanSucChua => LaToaHang ? "Toa hàng" : $"{SucChua} chỗ";

        public string MoTaChiTiet =>
            $"{SoHieu} — {TenLoai}\n" +
            $"Chiều dài: {ChieuDaiM:N1} m · {SoTruc} trục\n" +
            $"Tự trọng {TuTrongTan:N1} t + tải trọng {TaiTrongTan:N1} t = {TongTrongLuongTan:N1} t\n" +
            $"Tải trọng trục: {TaiTrongTrucTan:N2} t/trục" +
            (VuotTaiTrongTruc ? $" (vượt ngưỡng giả định {QuyTacLapTau.TaiTrongTrucToiDaTan:N1} t)" : "") +
            (LaToaHang ? "" : $"\nSức chứa tham khảo: {SucChua} chỗ") +
            (CoPhamViRieng ? $"\nChạy từ {GaNoiCode} đến {GaCatCode} (cắt / nối dọc đường)" : "") +
            (GhiChu.Length > 0 ? $"\n{GhiChu}" : "");

        // ---------------------------------------------------------------------
        // Pham vi chay tren hanh trinh (vanhanh.ChiTietDoanTau.MaGaNoi / MaGaCat).
        // null = noi tu ga dau / chay toi ga cuoi cua chuyen (chay suot).
        // GaNoiCode / GaCatCode chi de hien thi (ma ga, ke ca khi chay suot).
        // ---------------------------------------------------------------------
        public int? MaGaNoi { get; private set; }
        public int? MaGaCat { get; private set; }
        public string GaNoiCode { get; private set; } = "";
        public string GaCatCode { get; private set; } = "";

        public bool CoPhamViRieng => MaGaNoi != null || MaGaCat != null;

        public string NhanPhamVi => GaNoiCode.Length > 0 ? $"{GaNoiCode} → {GaCatCode}" : "Suốt hành trình";

        public void DatPhamVi(int? maGaNoi, string gaNoiCode, int? maGaCat, string gaCatCode)
        {
            MaGaNoi = maGaNoi;
            MaGaCat = maGaCat;
            GaNoiCode = gaNoiCode;
            GaCatCode = gaCatCode;
            BaoThayDoiPhamVi();
        }

        public void XoaPhamVi() => DatPhamVi(null, "", null, "");

        private void BaoThayDoiPhamVi()
        {
            BaoThayDoi(nameof(MaGaNoi));
            BaoThayDoi(nameof(MaGaCat));
            BaoThayDoi(nameof(CoPhamViRieng));
            BaoThayDoi(nameof(NhanPhamVi));
            BaoThayDoi(nameof(MoTaChiTiet));
        }

        // ---------------------------------------------------------------------
        // Trang thai hien thi tren man hinh lap tau (dialog dat lai sau moi thao tac)
        // ---------------------------------------------------------------------

        // Toa co mat trong doan tau o khu doan dang xem hay khong
        private bool _dangHoatDong = true;
        public bool DangHoatDong
        {
            get => _dangHoatDong;
            set { if (_dangHoatDong != value) { _dangHoatDong = value; BaoThayDoi(); } }
        }

        // Nhan tren the toa khi toa khong co mat o khu doan dang xem: "Nối tại DAN", "Đã cắt tại BTH"
        private string _nhanNgoaiDoan = "";
        public string NhanNgoaiDoan
        {
            get => _nhanNgoaiDoan;
            set { if (_nhanNgoaiDoan != value) { _nhanNgoaiDoan = value; BaoThayDoi(); } }
        }

        // Tooltip nut X: thao han khoi doan hoac cat tai ga dang xem
        private string _goiYNutThao = "Tháo toa khỏi đoàn";
        public string GoiYNutThao
        {
            get => _goiYNutThao;
            set { if (_goiYNutThao != value) { _goiYNutThao = value; BaoThayDoi(); } }
        }

        // Chuyen co lich dung ga trung gian thi moi hien nhan pham vi chay
        private bool _hienPhamVi;
        public bool HienPhamVi
        {
            get => _hienPhamVi;
            set { if (_hienPhamVi != value) { _hienPhamVi = value; BaoThayDoi(); } }
        }

        // Ban sao de dialog thao tac, bam Huy thi phuong an goc khong bi dung
        public ToaLapTau SaoChep()
        {
            var ban = (ToaLapTau)MemberwiseClone();
            ban.PropertyChanged = null;
            return ban;
        }

        // Tao tu toa xe trong doi (phuongtien.ToaXe + ChungLoaiToa)
        public static ToaLapTau TuToaXe(ToaXeLapTauDTO tx)
        {
            var cl = DanhMucMauPhuongTien.TimChungLoai(tx.MaChungLoaiCode);
            return new ToaLapTau
            {
                MaToaXe = tx.MaToaXe,
                SoHieu = tx.SoHieuToaXe,
                LoaiCode = tx.MaChungLoaiCode,
                TenLoai = string.IsNullOrWhiteSpace(tx.TenMoTa) ? cl.TenMoTa : tx.TenMoTa,
                ChieuDaiM = tx.ChieuDaiM,
                SoTruc = tx.SoTruc,
                TuTrongTan = tx.TuTrongTan,
                TaiTrongTan = tx.TaiTrongToiDaTan,
                SucChua = cl.SucChuaThamKhao,
                LaToaHang = QuyTacLapTau.LaToaHang(tx.MaChungLoaiCode),
                MauNen = cl.MauNen,
                MauVien = cl.MauVien,
                MauChu = cl.MauChu
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void BaoThayDoi([CallerMemberName] string? ten = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
    }

    // -------------------------------------------------------------------------
    // Dau may co the chon lam may keo chinh / may day
    // -------------------------------------------------------------------------
    public class DauMayLapTau
    {
        public int MaDauMay { get; set; }
        public string SoHieu { get; set; } = "";
        public string MaDongCode { get; set; } = "";
        public string DonViQuanLy { get; set; } = "";
        public string TrangThai { get; set; } = "SAN_SANG";
        public decimal SucKeoTan { get; set; }
        public decimal ChieuDaiM { get; set; }
        public decimal TrongLuongTan { get; set; }

        // "bận SE1" khi dau may thuoc chuyen khac chong khung gio chuyen dang lap
        public string GhiChuBan { get; set; } = "";

        public bool LaKhongDung => MaDauMay == 0;

        public string NhanTrangThai => TrangThai switch
        {
            "SAN_SANG" => "Sẵn sàng",
            "DANG_CHAY" => "Đang chạy",
            "BAO_DUONG" => "Bảo dưỡng",
            _ => TrangThai
        };

        // So hieu da chua ma dong (D19E-901) nen khong lap lai ma dong cho gon.
        // Viet ngan de o chon tren thanh cong cu (rong 245) van thay "bận SE1".
        public string NhanHienThi => LaKhongDung
            ? "— Không dùng máy đẩy —"
            : $"{SoHieu} · {SucKeoTan:N0} t · {(GhiChuBan.Length > 0 ? GhiChuBan : NhanTrangThai)}";

        // Muc dau tien cua o chon may day
        public static DauMayLapTau KhongDung() => new() { MaDauMay = 0 };

        public static DauMayLapTau TuDTO(DauMayLapTauDTO dm, BoiCanhLapTauDTO bc)
        {
            var ban = dm.ChuyenKhac
                .Where(c => QuyTacLapTau.ChuyenConHieuLuc(c.TrangThai) &&
                            QuyTacLapTau.ChongGio(c.GioXuatPhatKH, c.GioVeDichKH, bc.GioXuatPhatKH, bc.GioVeDichKH))
                .Select(c => c.SoHieuMacTau)
                .Distinct()
                .ToList();

            return new DauMayLapTau
            {
                MaDauMay = dm.MaDauMay,
                SoHieu = dm.SoHieuDauMay,
                MaDongCode = dm.MaDongCode,
                DonViQuanLy = dm.DonViQuanLy,
                TrangThai = dm.TrangThai,
                SucKeoTan = dm.SucKeoToiDaTan,
                ChieuDaiM = dm.ChieuDaiM,
                TrongLuongTan = dm.TrongLuongTan,
                GhiChuBan = ban.Count > 0 ? $"bận {string.Join(", ", ban)}" : ""
            };
        }
    }

    // -------------------------------------------------------------------------
    // Mot ga dung cua chuyen tau (vanhanh.LichDungGa JOIN hatang.Ga) tren thanh
    // hanh trinh cua man hinh lap tau. Moi diem kem so lieu cua KHU DOAN NGAY
    // SAU no (ga nay -> ga dung ke tiep) de ve doan noi giua hai ga.
    // -------------------------------------------------------------------------
    public class DiemDungLapTau : INotifyPropertyChanged
    {
        public int ViTri { get; init; }                 // 0 = ga dau
        public int MaGa { get; init; }
        public string MaGaCode { get; init; } = "";
        public string TenGa { get; init; } = "";
        public int ThuTuDung { get; init; }
        public DateTime GioDenKeHoach { get; init; }
        public DateTime GioDiKeHoach { get; init; }
        public bool LaGaDau { get; init; }
        public bool LaGaCuoi { get; init; }

        // Gioi han chieu dai doan tau tai ga (duong dieu do gan / duong tranh dai nhat)
        public decimal GioiHanChieuDaiM { get; init; }
        public string NguonGioiHan { get; init; } = "";

        public int SoPhutDo => (int)Math.Round((GioDiKeHoach - GioDenKeHoach).TotalMinutes);

        public bool DuThoiGianCatNoi => SoPhutDo >= QuyTacLapTau.SoPhutDoToiThieuCatNoi;

        public string NhanGio => LaGaDau ? $"đi {GioDiKeHoach:HH:mm}"
                               : LaGaCuoi ? $"đến {GioDenKeHoach:HH:mm}"
                               : $"{GioDenKeHoach:HH:mm} · đỗ {SoPhutDo}'";

        public string NhanMenu => LaGaDau ? $"{MaGaCode} · {TenGa} (ga đầu)"
                                : LaGaCuoi ? $"{MaGaCode} · {TenGa} (ga cuối)"
                                : $"{MaGaCode} · {TenGa} (đỗ {SoPhutDo} phút)";

        // --- Trang thai do dialog cap nhat sau moi thao tac ---
        private bool _dangChon;
        public bool DangChon { get => _dangChon; set => Dat(ref _dangChon, value); }

        // Nhan so toa noi / cat tai ga: "+5", "−2", "−2 +3"
        private string _nhanThayDoi = "";
        public string NhanThayDoi
        {
            get => _nhanThayDoi;
            set { if (Dat(ref _nhanThayDoi, value)) BaoThayDoi(nameof(CoThayDoi)); }
        }
        public bool CoThayDoi => NhanThayDoi.Length > 0;

        private bool _coLoiTaiGa;
        public bool CoLoiTaiGa { get => _coLoiTaiGa; set => Dat(ref _coLoiTaiGa, value); }

        private string _moTaChiTiet = "";
        public string MoTaChiTiet { get => _moTaChiTiet; set => Dat(ref _moTaChiTiet, value); }

        // Khu doan ngay sau ga nay
        private string _nhanDoanSau = "";
        public string NhanDoanSau { get => _nhanDoanSau; set => Dat(ref _nhanDoanSau, value); }

        private bool _doanSauCoLoi;
        public bool DoanSauCoLoi { get => _doanSauCoLoi; set => Dat(ref _doanSauCoLoi, value); }

        private bool _doanSauDangChon;
        public bool DoanSauDangChon { get => _doanSauDangChon; set => Dat(ref _doanSauDangChon, value); }

        public static List<DiemDungLapTau> TaoHanhTrinh(IReadOnlyList<DiemDungHanhTrinhDTO> lich)
            => lich.Select((d, i) =>
            {
                var (gioiHan, nguon) = QuyTacLapTau.GioiHanChieuDaiTaiGa(d);
                return new DiemDungLapTau
                {
                    ViTri = i,
                    MaGa = d.MaGa,
                    MaGaCode = d.MaGaCode,
                    TenGa = d.TenGa,
                    ThuTuDung = d.ThuTuDung,
                    GioDenKeHoach = d.GioDenKeHoach,
                    GioDiKeHoach = d.GioDiKeHoach,
                    LaGaDau = i == 0,
                    LaGaCuoi = i == lich.Count - 1,
                    GioiHanChieuDaiM = gioiHan,
                    NguonGioiHan = nguon
                };
            }).ToList();

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool Dat<T>(ref T truong, T giaTri, [CallerMemberName] string? ten = null)
        {
            if (EqualityComparer<T>.Default.Equals(truong, giaTri)) return false;
            truong = giaTri;
            BaoThayDoi(ten);
            return true;
        }

        private void BaoThayDoi(string? ten) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
    }

    // -------------------------------------------------------------------------
    // Du lieu dau vao cua dialog lap tau
    // -------------------------------------------------------------------------
    public class ThongTinLapTau
    {
        public BoiCanhLapTauDTO BoiCanh { get; init; } = null!;

        // Cac ga dung theo thu tu; it hon 3 ga (khong co ga trung gian) thi khong cat / noi duoc
        public List<DiemDungLapTau> HanhTrinh { get; init; } = new();
        public string GhiChuHanhTrinh { get; init; } = "";

        public List<DauMayLapTau> DanhSachDauMay { get; init; } = new();
        public List<ToaLapTau> DoanTauBanDau { get; init; } = new();
        public List<ToaLapTau> BaiToa { get; init; } = new();

        // Ghi phuong an vao CSDL (LapTauService.LuuPhuongAn). Tra ve null neu luu duoc,
        // nguoc lai la thong diep loi de dialog hien va giu nguyen man hinh.
        public Func<PhuongAnLapTauDTO, (KetQuaLuuLapTauDTO? KetQua, string? Loi)> Luu { get; init; } = null!;
    }
}
