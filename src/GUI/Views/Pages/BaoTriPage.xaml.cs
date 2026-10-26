using BUS.Services;
using DTO.BaoTri;
using DTO.NhanSu;
using GUI.ViewModels.KyThuat;
using GUI.Helpers;
using GUI.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.Pages
{
    // =========================================================================
    // PHAN HE BAO TRI & SUA CHUA
    //   Tab 1: Bien ban kham xe ky thuat (baotri.KhamXeKyThuat)   - file nay
    //   Tab 2: Cap phat nhien lieu (baotri.NhatKyCapNhienLieu)    - BaoTriPage.NhienLieu.cs
    //   Tab 3: Bao duong dinh ky (baotri.NhatKyBaoDuong)          - BaoTriPage.BaoDuong.cs
    //   Tab 4: Canh bao an toan + duyet an toan + san sang xuat ben - BaoTriPage.CanhBao.cs
    //   Tab 5: Chi phi & hieu qua theo tuyen (tong hop 3 tab tren) - BaoTriPage.ChiPhi.cs
    //
    // Tu 24/10 (back-end): du lieu doc tu CSDL qua BaoTriService.LayDuLieu (mot anh chup cho ca 5 tab);
    // moi thao tac ghi thang CSDL qua BaoTriService - Service chay lai luat trong giao dich truoc khi ghi.
    // Dialog nhan ham luu (callback): loi tu Service thi hien ThongBaoDialog va giu dialog mo.
    // Bien ban kham xe khong sua (quyet dinh 24/10): sai thi "Kham lai" = lap bien ban moi.
    // =========================================================================
    public partial class BaoTriPage : Page
    {
        private bool _dangNapDuLieu;

        private readonly BaoTriService _service = new();
        private readonly NhanSuService _nhanSuService = new();
        private bool _coKetNoiCsdl = true;

        // Anh chup o lan nap gan nhat, 5 tab va 3 dialog dung chung
        private BaoTriDuLieu _duLieu = new(DuLieuBaoTri.Rong(DateTime.Now));
        private DuLieuNhanSu? _duLieuNhanSu;

        // Loc theo doan tau khi bam the tinh trang (null = tat ca)
        private int? _maDoanTauLoc;

        public BaoTriPage()
        {
            // Chan SelectionChanged ban ra tu ComboBoxItem IsSelected="True" luc dung cay giao dien
            _dangNapDuLieu = true;
            InitializeComponent();
            _dangNapDuLieu = false;

            Loaded += BaoTriPage_Loaded;
        }

        private void BaoTriPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Chi nap mot lan dau tien
            Loaded -= BaoTriPage_Loaded;
            NapToanBoDuLieu();
        }

        // =====================================================================
        // NAP DU LIEU CHUNG
        // =====================================================================

        // Doc lai tu CSDL. Mat ket noi: anh chup rong, bao loi mot lan (khong bao lai moi lan lam moi),
        // man hinh van mo duoc; nap lai (F5) khi CSDL co lai thi tu het.
        private void NapLaiTuCsdl()
        {
            bool truocDoCoKetNoi = _coKetNoiCsdl;
            _coKetNoiCsdl = _service.LayDuLieu(out var dl, out string loi);
            _duLieu = new BaoTriDuLieu(dl);
            NapLaiNhanSu();

            if (!_coKetNoiCsdl && truocDoCoKetNoi)
                ThongBaoDialog.Loi($"Không đọc được dữ liệu bảo trì từ CSDL.\n{loi}", "Lỗi kết nối CSDL");
        }

        // Kip lai cho cot san sang xuat ben (null = khong doc duoc -> muc kip lai bi chan)
        private void NapLaiNhanSu()
            => _duLieuNhanSu = _nhanSuService.LayDuLieu(out var ns, out _) ? ns : null;

        private bool KiemTraKetNoi()
        {
            if (_coKetNoiCsdl) return true;
            ThongBaoDialog.CanhBao("Chưa kết nối được CSDL nên chưa thao tác được. Kiểm tra kết nối rồi bấm F5 để nạp lại.",
                                   "Chưa có dữ liệu");
            return false;
        }

        public void NapToanBoDuLieu()
        {
            NapLaiTuCsdl();
            LamMoiTatCa();
        }

        // Ve lai 5 tab tu anh chup hien tai (khong doc CSDL)
        private void LamMoiTatCa()
        {
            LamMoiDanhSach(giuDongChon: true);
            LamMoiNhienLieu(giuDongChon: true);
            LamMoiBaoDuong(giuDongChon: true);
            LamMoiCanhBao();
            LamMoiChiPhi();
        }

        // Moi lan mo tab Canh bao / Chi phi thi tinh lai (thoi gian, chi phi du phong... thay doi theo dong ho);
        // tab Canh bao doc lai kip lai vi ben Nhan su co the vua phan cong.
        private void TabBaoTri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SelectionChanged cua ComboBox / DataGrid ben trong cung noi bot len day
            if (!ReferenceEquals(e.OriginalSource, tabBaoTri) || _dangNapDuLieu) return;
            if (tabBaoTri.SelectedIndex == 3)
            {
                NapLaiNhanSu();
                LamMoiCanhBao();
            }
            if (tabBaoTri.SelectedIndex == 4) LamMoiChiPhi();
        }

        // Bam chi so tren dai tieu de: chuyen toi tab tuong ung kem bo loc phu hop
        private void Kpi_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement)?.Tag?.ToString())
            {
                case "KHONG_DAT":
                    tabBaoTri.SelectedIndex = 0;
                    _maDoanTauLoc = null;
                    ChonTheoTag(cboKetLuan, "KHONG_DAT");   // chan SelectionChanged, nap lai ngay duoi
                    LamMoiDanhSach(giuDongChon: false);
                    break;
                case "CHUA_KHAM":
                    tabBaoTri.SelectedIndex = 0;
                    BtnXoaLocKhamXe_Click(this, new RoutedEventArgs());
                    break;
                case "VUOT":
                    tabBaoTri.SelectedIndex = 1;
                    _maDauMayLoc = null;
                    ChonTheoTag(cboMucTieuHao, "VUOT");
                    LamMoiNhienLieu(giuDongChon: false);
                    break;
                case "DEN_HAN":
                    tabBaoTri.SelectedIndex = 2;
                    break;
            }
        }

        private void ChonTheoTag(ComboBox cbo, string tag)
        {
            _dangNapDuLieu = true;
            foreach (object muc in cbo.Items)
            {
                if (muc is ComboBoxItem cbi && cbi.Tag?.ToString() == tag)
                {
                    cbo.SelectedItem = cbi;
                    break;
                }
            }
            _dangNapDuLieu = false;
        }

        private List<KhamXeHienThi> LayToanBoKhamXe() => _duLieu.KhamXe;

        // =====================================================================
        // THE TINH TRANG XUAT BEN THEO DOAN TAU + CHI SO TONG QUAN
        // =====================================================================

        private void NapTinhTrangDoanTau(List<KhamXeHienThi> toanBo)
        {
            var dsTinhTrang = _duLieu.DoanTauDangDieuHanh.Select(dt =>
            {
                var cuaDoan = toanBo.Where(bb => bb.MaDoanTau == dt.MaDoanTau).ToList();
                return new TinhTrangDoanTauHienThi
                {
                    DoanTau = dt,
                    BienBanMoiNhat = cuaDoan.OrderByDescending(bb => bb.ThoiDiemKham).ThenByDescending(bb => bb.MaKhamXe).FirstOrDefault(),
                    SoLanKham = cuaDoan.Count,
                    DangChon = _maDoanTauLoc == dt.MaDoanTau
                };
            }).ToList();

            icTinhTrangDoanTau.ItemsSource = dsTinhTrang;

            txtKpiKhongDat.Text = dsTinhTrang.Count(t => t.TrangThai == "KHONG_DAT").ToString("N0");
            txtKpiChuaKham.Text = dsTinhTrang.Count(t => t.TrangThai == "CHUA_KHAM" && !t.DoanTau.DaXuatPhat).ToString("N0");
        }

        private void TheDoanTau_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not TinhTrangDoanTauHienThi tt) return;

            // Bam lai the dang chon thi bo loc
            _maDoanTauLoc = _maDoanTauLoc == tt.DoanTau.MaDoanTau ? null : tt.DoanTau.MaDoanTau;
            LamMoiDanhSach(giuDongChon: false);
        }

        private void TheDoanTau_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not TinhTrangDoanTauHienThi tt) return;
            e.Handled = true;

            // Hai lan Click cua cu nhap dup da bat roi tat loc: dat lai loc theo doan tau nay
            _maDoanTauLoc = tt.DoanTau.MaDoanTau;
            LamMoiDanhSach(giuDongChon: false);
            LapBienBan(tt.DoanTau.MaDoanTau, tt.BienBanMoiNhat?.MaNguoiKham);
        }

        private void BtnBoLocDoanTau_Click(object sender, RoutedEventArgs e)
        {
            _maDoanTauLoc = null;
            LamMoiDanhSach(giuDongChon: true);
        }

        // =====================================================================
        // TAB 1: DANH SACH BIEN BAN KHAM XE
        // =====================================================================

        private void NapDanhSachKhamXe(List<KhamXeHienThi> toanBo)
        {
            string tuKhoa = txtTimKhamXe.Text?.Trim() ?? string.Empty;
            string ketLuan = LayTagDangChon(cboKetLuan) ?? "ALL";

            var danhSach = toanBo.Where(bb => KhopBoLocKhamXe(bb, tuKhoa, ketLuan, _maDoanTauLoc))
                                 .OrderByDescending(bb => bb.ThoiDiemKham).ThenByDescending(bb => bb.MaKhamXe)
                                 .ToList();

            dgKhamXe.ItemsSource = danhSach;
            txtBadgeKhamXe.Text = danhSach.Count.ToString();
            txtTrongKhamXe.Visibility = danhSach.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Chip loc doan tau
            var dtLoc = _maDoanTauLoc.HasValue ? _duLieu.TimDoanTau(_maDoanTauLoc.Value) : null;
            bdLocDoanTau.Visibility = dtLoc != null ? Visibility.Visible : Visibility.Collapsed;
            txtLocDoanTau.Text = dtLoc != null ? $"Đoàn tàu: {dtLoc.SoHieuMacTau} · {dtLoc.NgayXuatPhat:dd/MM}" : "";
        }

        private static bool KhopBoLocKhamXe(KhamXeHienThi bb, string tuKhoa, string ketLuan, int? maDoanTau)
        {
            if (tuKhoa.Length > 0 &&
                !bb.MaBienBan.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !bb.SoHieuMacTau.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !bb.SoHieuDauMayChinh.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !bb.TenNguoiKham.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !bb.GhiChuKyThuat.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
                return false;

            if (ketLuan == "DAT" && !bb.DuDieuKienXuatBen) return false;
            if (ketLuan == "KHONG_DAT" && bb.DuDieuKienXuatBen) return false;
            if (maDoanTau.HasValue && bb.MaDoanTau != maDoanTau.Value) return false;
            return true;
        }

        // Ve lai the tinh trang + luoi tu anh chup; giu dong dang chon neu con trong danh sach
        private void LamMoiDanhSach(bool giuDongChon, int? maChonSau = null)
        {
            int? maDangChon = maChonSau ?? (giuDongChon ? (dgKhamXe.SelectedItem as KhamXeHienThi)?.MaKhamXe : null);

            var toanBo = LayToanBoKhamXe();
            NapTinhTrangDoanTau(toanBo);
            NapDanhSachKhamXe(toanBo);

            if (!(maDangChon.HasValue && ChonDongTheoMa(maDangChon.Value)) && dgKhamXe.Items.Count > 0)
                dgKhamXe.SelectedIndex = 0;
            else if (dgKhamXe.Items.Count == 0)
                XoaTrangChiTiet();
        }

        private bool ChonDongTheoMa(int maKhamXe)
        {
            if (dgKhamXe.ItemsSource is not IEnumerable<KhamXeHienThi> ds) return false;

            var muc = ds.FirstOrDefault(bb => bb.MaKhamXe == maKhamXe);
            if (muc == null) return false;

            dgKhamXe.SelectedItem = muc;
            dgKhamXe.ScrollIntoView(muc);
            return true;
        }

        private void TxtTimKhamXe_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            LamMoiDanhSach(giuDongChon: true);
        }

        private void BoLocKhamXe_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            LamMoiDanhSach(giuDongChon: true);
        }

        private void BtnXoaLocKhamXe_Click(object sender, RoutedEventArgs e)
        {
            _dangNapDuLieu = true;
            txtTimKhamXe.Text = string.Empty;
            cboKetLuan.SelectedIndex = 0;
            _maDoanTauLoc = null;
            _dangNapDuLieu = false;

            LamMoiDanhSach(giuDongChon: true);
        }

        // =====================================================================
        // CHI TIET BIEN BAN
        // =====================================================================

        private void DgKhamXe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var bb = dgKhamXe.SelectedItem as KhamXeHienThi;
            var dt = bb != null ? _duLieu.TimDoanTau(bb.MaDoanTau) : null;
            btnKhamLaiDoanTau.IsEnabled = dt != null && !dt.DaKetThuc;

            if (bb == null)
            {
                XoaTrangChiTiet();
                return;
            }
            HienThiChiTiet(bb);
        }

        private void XoaTrangChiTiet()
        {
            txtChiTietTieuDe.Text = "BIÊN BẢN KHÁM XE";
            txtChiTietMoTa.Text = "Chọn một biên bản để xem chi tiết";
            txtChiTietMa.Text = "—";
            pnlChiTiet.Visibility = Visibility.Collapsed;
            txtChiTietTrong.Visibility = Visibility.Visible;
        }

        private void HienThiChiTiet(KhamXeHienThi bb)
        {
            pnlChiTiet.Visibility = Visibility.Visible;
            txtChiTietTrong.Visibility = Visibility.Collapsed;

            txtChiTietTieuDe.Text = $"{bb.SoHieuMacTau} · {bb.NgayXuatPhat:dd/MM/yyyy}";
            txtChiTietMoTa.Text = $"Khám lúc {bb.ThoiDiemKham:HH:mm dd/MM/yyyy} · {bb.TenNguoiKham}";
            txtChiTietMa.Text = bb.MaBienBan;

            // --- Ket luan ---
            bool dat = bb.DuDieuKienXuatBen;
            bdChiTietKetLuan.Background = Mau(dat ? "#F0FDF4" : "#FEF2F2");
            bdChiTietKetLuan.BorderBrush = Mau(dat ? "#86EFAC" : "#FCA5A5");
            icoChiTietKetLuan.Symbol = dat ? SymbolRegular.ShieldCheckmark24 : SymbolRegular.ShieldError24;
            icoChiTietKetLuan.Foreground = Mau(dat ? "#15803D" : "#B91C1C");
            txtChiTietKetLuan.Text = dat ? "ĐỦ ĐIỀU KIỆN XUẤT BẾN" : "KHÔNG ĐỦ ĐIỀU KIỆN XUẤT BẾN";
            txtChiTietKetLuan.Foreground = Mau(dat ? "#15803D" : "#B91C1C");
            txtChiTietKetLuanPhu.Text = dat
                ? "Đạt 4/4 hạng mục kiểm tra"
                : $"Đạt {bb.SoHangMucDat}/4 hạng mục · không cho đoàn tàu xuất bến";

            icChiTietHangMuc.ItemsSource = bb.HangMuc;
            txtChiTietGhiChu.Text = string.IsNullOrWhiteSpace(bb.GhiChuKyThuat) ? "(Không có ghi chú)" : bb.GhiChuKyThuat;

            pnlChiTietChiPhi.Children.Clear();
            ThemDongThongSo(pnlChiTietChiPhi, "Chi phí khám xe / sửa chữa",
                            bb.ChiPhi > 0 ? $"{bb.ChiPhi:N0} đ" : "Không phát sinh", laDongCuoi: true);

            // --- Thong tin doan tau ---
            var pnl = pnlChiTietDoanTau;
            pnl.Children.Clear();
            var dt = _duLieu.TimDoanTau(bb.MaDoanTau);
            if (dt != null)
            {
                ThemDongThongSo(pnl, "Hành trình", dt.HanhTrinh);
                ThemDongThongSo(pnl, "Xuất phát kế hoạch", dt.GioXuatPhatKH.ToString("HH:mm dd/MM/yyyy"));
                ThemDongThongSo(pnl, "Trạng thái chuyến", dt.NhanTrangThaiChuyen);
                ThemDongThongSo(pnl, "Đầu máy", dt.NhanDauMay);
                ThemDongThongSo(pnl, "Số toa · chiều dài", $"{dt.TongSoToa} toa · {dt.TongChieuDaiM:N1} m");
                ThemDongThongSo(pnl, "Trọng lượng đoàn", $"{dt.TongTrongLuongTan:N1} tấn");
                if (dt.Goc.ThoiDiemSuaDoanTau.HasValue)
                    ThemDongThongSo(pnl, "Sửa đoàn tàu lần cuối", dt.Goc.ThoiDiemSuaDoanTau.Value.ToString("HH:mm dd/MM/yyyy"));
                ThemDongThongSo(pnl, "Duyệt an toàn", dt.DaDuyetAnToan ? "Đã duyệt" : "Chưa duyệt", laDongCuoi: true);
            }

            // --- Lich su kham cua doan tau (ke ca bien ban dang chon) ---
            var lichSu = LayToanBoKhamXe().Where(x => x.MaDoanTau == bb.MaDoanTau)
                                          .OrderByDescending(x => x.ThoiDiemKham).ThenByDescending(x => x.MaKhamXe)
                                          .ToList();
            icLichSuKham.ItemsSource = lichSu;
            txtTieuDeLichSu.Text = $"LỊCH SỬ KHÁM CỦA ĐOÀN TÀU ({lichSu.Count})";

            // --- Kham lai: chi goi y khi bien ban MOI NHAT cua doan tau khong dat (hoac lap truoc lan sua doan tau) ---
            var moiNhat = lichSu.FirstOrDefault();
            bool hetHieuLuc = moiNhat != null && dt?.Goc.ThoiDiemSuaDoanTau > moiNhat.ThoiDiemKham;
            bool canKhamLai = dt != null && !dt.DaKetThuc && moiNhat != null && (!moiNhat.DuDieuKienXuatBen || hetHieuLuc);
            btnKhamLai.Visibility = canKhamLai ? Visibility.Visible : Visibility.Collapsed;
            txtNutKhamLai.Text = hetHieuLuc && moiNhat!.DuDieuKienXuatBen
                ? $"Đoàn tàu {bb.SoHieuMacTau} đã đổi thành phần — khám lại"
                : $"Khám lại đoàn tàu {bb.SoHieuMacTau} sau khi khắc phục";
        }

        // Mot dong "nhan ...... gia tri" trong bang thong so cua panel chi tiet
        private void ThemDongThongSo(Panel pnl, string nhan, string giaTri, bool laDongCuoi = false)
        {
            var hang = new Grid { Height = 26 };
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tbNhan = new TextBlock { Text = nhan, Style = (Style)FindResource("KtSpecLabel") };
            var tbGiaTri = new TextBlock { Text = giaTri, Style = (Style)FindResource("KtSpecValue"), MaxWidth = 190 };
            Grid.SetColumn(tbGiaTri, 1);

            hang.Children.Add(tbNhan);
            hang.Children.Add(tbGiaTri);

            pnl.Children.Add(new Border
            {
                Child = hang,
                BorderBrush = Mau("#F1F5F9"),
                BorderThickness = new Thickness(0, 0, 0, laDongCuoi ? 0 : 1)
            });
        }

        private void DongLichSuKham_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not KhamXeHienThi bb) return;
            if (ChonDongTheoMa(bb.MaKhamXe)) return;

            // Bi bo loc an mat: xoa loc roi chon lai
            BtnXoaLocKhamXe_Click(this, new RoutedEventArgs());
            ChonDongTheoMa(bb.MaKhamXe);
        }

        // =====================================================================
        // LAP BIEN BAN (khong sua bien ban da lap - quyet dinh 24/10)
        // =====================================================================

        private void BtnLapBienBan_Click(object sender, RoutedEventArgs e) => LapBienBan(_maDoanTauLoc);

        // Lap bien ban moi cho doan tau cua bien ban dang chon (thay cho nut Sua truoc day)
        private void BtnKhamLai_Click(object sender, RoutedEventArgs e)
        {
            if (dgKhamXe.SelectedItem is not KhamXeHienThi bb) return;
            LapBienBan(bb.MaDoanTau, bb.MaNguoiKham);
        }

        // chuyenTab = false khi goi tu tab Canh bao: mo dialog tai cho, khong nhay tab
        private void LapBienBan(int? maDoanTau = null, int? maNguoiKham = null, bool chuyenTab = true)
        {
            if (chuyenTab) tabBaoTri.SelectedIndex = 0;
            if (!KiemTraKetNoi()) return;

            KetQuaLapBienBan? kq = null;
            string? Luu(KhamXeDTO bb) => _service.LapBienBan(bb, out string loi, out kq) ? null : loi;

            var dlg = new KhamXeDialog(_duLieu, Luu, maDoanTau, maNguoiKham) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || kq == null) return;

            NapLaiTuCsdl();
            LamMoiTatCa();
            ChonBienBanVuaLap(kq.MaKhamXe);
            BaoKetQuaLuu(kq);
        }

        private void ChonBienBanVuaLap(int maKhamXe)
        {
            if (ChonDongTheoMa(maKhamXe)) return;

            // Bo loc hien tai an mat ban ghi vua luu thi xoa loc de nguoi dung thay ngay
            var bb = _duLieu.KhamXe.FirstOrDefault(x => x.MaKhamXe == maKhamXe);
            _dangNapDuLieu = true;
            txtTimKhamXe.Text = string.Empty;
            cboKetLuan.SelectedIndex = 0;
            _dangNapDuLieu = false;
            if (_maDoanTauLoc.HasValue && _maDoanTauLoc != bb?.MaDoanTau) _maDoanTauLoc = null;
            LamMoiDanhSach(giuDongChon: false, maChonSau: maKhamXe);
        }

        private void BaoKetQuaLuu(KetQuaLapBienBan kq)
        {
            var bb = _duLieu.KhamXe.FirstOrDefault(x => x.MaKhamXe == kq.MaKhamXe);
            string tau = bb?.SoHieuMacTau ?? "";
            if (kq.DuDieuKien)
            {
                var dt = bb != null ? _duLieu.TimDoanTau(bb.MaDoanTau) : null;
                ThongBaoDialog.ThanhCong(
                    $"Đã lập biên bản BB-{kq.MaKhamXe:0000} cho đoàn tàu {tau} lúc {kq.ThoiDiemKham:HH:mm dd/MM}.\n" +
                    "Kết luận: ĐỦ ĐIỀU KIỆN XUẤT BẾN (đạt 4/4 hạng mục).\n\n" +
                    (dt != null && !dt.DaDuyetAnToan && !dt.DaXuatPhat
                        ? "Đoàn tàu chưa được duyệt an toàn: vào tab Cảnh Báo An Toàn để duyệt."
                        : "Biên bản đã ghi vào CSDL."),
                    "Lưu biên bản khám xe");
            }
            else
            {
                ThongBaoDialog.CanhBao(
                    $"Đã lập biên bản BB-{kq.MaKhamXe:0000} cho đoàn tàu {tau}.\n" +
                    $"Kết luận: KHÔNG ĐỦ ĐIỀU KIỆN XUẤT BẾN ({bb?.TomTatLoi}).\n\n" +
                    (kq.DaBoDuyet ? "Đoàn tàu đã bị bỏ cờ duyệt an toàn.\n" : "") +
                    "Đoàn tàu không được xuất bến cho tới khi có biên bản khám lại đạt yêu cầu và được duyệt lại.",
                    "Đoàn tàu không đạt");
            }
        }

        // =====================================================================
        // TIEN ICH
        // =====================================================================

        private static string? LayTagDangChon(ComboBox cbo)
            => (cbo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

        private static SolidColorBrush Mau(string ma) => (SolidColorBrush)new BrushConverter().ConvertFrom(ma)!;

        // =====================================================================
        // CAC HAM MainWindow GOI QUA (dong bo voi cac Page khac)
        // =====================================================================

        // F3 / F2 / Esc tac dong len tab dang mo
        public void FocusTimKiem()
        {
            TextBox o = tabBaoTri.SelectedIndex switch
            {
                1 => txtTimCapDau,
                2 => txtTimBaoDuong,
                3 => txtTimCanhBao,
                4 => txtTimTuyen,
                _ => txtTimKhamXe
            };
            o.Focus();
            o.SelectAll();
        }

        public void KichHoatThemMoi()
        {
            switch (tabBaoTri.SelectedIndex)
            {
                case 0: LapBienBan(_maDoanTauLoc); break;
                case 1: CapPhatNhienLieu(_maDauMayLoc); break;
                case 2: GhiNhanBaoDuong(_khoaPhuongTienLoc); break;
                // Tab Canh bao (index 3) chi tong hop, khong co ban ghi rieng de lap moi
            }
        }

        // F5: doc lai tu CSDL
        public void KichHoatNapLai() => NapToanBoDuLieu();

        public void KichHoatHuy()
        {
            switch (tabBaoTri.SelectedIndex)
            {
                case 0: BtnXoaLocKhamXe_Click(this, new RoutedEventArgs()); break;
                case 1: BtnXoaLocCapDau_Click(this, new RoutedEventArgs()); break;
                case 2: BtnXoaLocBaoDuong_Click(this, new RoutedEventArgs()); break;
                case 3: BtnXoaLocCanhBao_Click(this, new RoutedEventArgs()); break;
                case 4: BtnXoaLocTuyen_Click(this, new RoutedEventArgs()); break;
            }
        }
    }
}
