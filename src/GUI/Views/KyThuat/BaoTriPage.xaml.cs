using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GUI.Views.Dialogs;
using GUI.Views.KyThuat.Dialogs;
using GUI.Views.KyThuat.Models;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.KyThuat
{
    // =========================================================================
    // PHAN HE BAO TRI KY THUAT & CANH BAO AN TOAN
    //   Tab 1: Bien ban kham xe ky thuat (baotri.KhamXeKyThuat)   - file nay
    //   Tab 2: Cap phat nhien lieu (baotri.NhatKyCapNhienLieu)    - BaoTriPage.NhienLieu.cs
    //   Tab 3: Bao duong dinh ky (baotri.NhatKyBaoDuong)          - BaoTriPage.BaoDuong.cs
    //   Tab 4: Canh bao an toan (tong hop 3 tab tren)              - BaoTriPage.CanhBao.cs
    //
    // Giai doan dung giao dien: chua co Repository cho schema baotri nen du
    // lieu goc lay tu DanhMucMauBaoTri (trung seed data). Lap / sua bien ban
    // duoc giu trong bo nho ("thay doi tam") va tron vao du lieu goc moi lan
    // nap lai - cung mau voi PhuongTienPage.
    // =========================================================================
    public partial class BaoTriPage : Page
    {
        private bool _dangNapDuLieu;

        // --- Thay doi tam tren giao dien (chua ghi CSDL) ---
        // Khoa am = bien ban lap moi, khoa duong = bien ban goc da bi sua
        private readonly Dictionary<int, KhamXeHienThi> _khamXeTam = new();
        private int _maTamKeTiep = -1;

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

        public void NapToanBoDuLieu()
        {
            var toanBo = LayToanBoKhamXe();
            NapTinhTrangDoanTau(toanBo);
            NapDanhSachKhamXe(toanBo);

            if (dgKhamXe.Items.Count > 0 && dgKhamXe.SelectedIndex < 0)
                dgKhamXe.SelectedIndex = 0;

            LamMoiNhienLieu(giuDongChon: true);
            LamMoiBaoDuong(giuDongChon: true);
            LamMoiCanhBao();
            CapNhatNhanThayDoiTam();
        }

        // Moi lan mo tab Canh bao thi tinh lai (thoi gian "con x gio" thay doi theo dong ho)
        private void TabBaoTri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SelectionChanged cua ComboBox / DataGrid ben trong cung noi bot len day
            if (!ReferenceEquals(e.OriginalSource, tabBaoTri) || _dangNapDuLieu) return;
            if (tabBaoTri.SelectedIndex == 3) LamMoiCanhBao();
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

        // Du lieu goc (thay cho doc CSDL) + ban sua tam + ban lap moi
        private List<KhamXeHienThi> LayToanBoKhamXe()
        {
            var ds = new List<KhamXeHienThi>();
            var maDaCo = new HashSet<int>();

            foreach (var bb in DanhMucMauBaoTri.LayBienBanKhamXe())
            {
                maDaCo.Add(bb.MaKhamXe);
                ds.Add(_khamXeTam.TryGetValue(bb.MaKhamXe, out var banTam) ? banTam : bb);
            }
            ds.AddRange(_khamXeTam.Values.Where(bb => !maDaCo.Contains(bb.MaKhamXe)));
            return ds;
        }

        // =====================================================================
        // THE TINH TRANG XUAT BEN THEO DOAN TAU + CHI SO TONG QUAN
        // =====================================================================

        private void NapTinhTrangDoanTau(List<KhamXeHienThi> toanBo)
        {
            var dsTinhTrang = DanhMucMauBaoTri.DoanTau.Select(dt =>
            {
                var cuaDoan = toanBo.Where(bb => bb.MaDoanTau == dt.MaDoanTau).ToList();
                return new TinhTrangDoanTauHienThi
                {
                    DoanTau = dt,
                    BienBanMoiNhat = cuaDoan.OrderByDescending(bb => bb.ThoiDiemKham).FirstOrDefault(),
                    SoLanKham = cuaDoan.Count,
                    DangChon = _maDoanTauLoc == dt.MaDoanTau
                };
            }).ToList();

            icTinhTrangDoanTau.ItemsSource = dsTinhTrang;

            txtKpiKhongDat.Text = dsTinhTrang.Count(t => t.TrangThai == "KHONG_DAT").ToString("N0");
            txtKpiChuaKham.Text = dsTinhTrang.Count(t => t.TrangThai == "CHUA_KHAM").ToString("N0");
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
                                 .OrderByDescending(bb => bb.ThoiDiemKham)
                                 .ToList();

            dgKhamXe.ItemsSource = danhSach;
            txtBadgeKhamXe.Text = danhSach.Count.ToString();
            txtTrongKhamXe.Visibility = danhSach.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Chip loc doan tau
            var dtLoc = _maDoanTauLoc.HasValue ? DanhMucMauBaoTri.TimDoanTau(_maDoanTauLoc.Value) : null;
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

        // Nap lai the tinh trang + luoi; giu dong dang chon neu con trong danh sach
        private void LamMoiDanhSach(bool giuDongChon, int? maChonSau = null)
        {
            int? maDangChon = maChonSau ?? (giuDongChon ? (dgKhamXe.SelectedItem as KhamXeHienThi)?.MaKhamXe : null);

            var toanBo = LayToanBoKhamXe();
            NapTinhTrangDoanTau(toanBo);
            NapDanhSachKhamXe(toanBo);

            if (!(maDangChon.HasValue && ChonDongTheoMa(maDangChon.Value)) && dgKhamXe.Items.Count > 0)
                dgKhamXe.SelectedIndex = 0;

            CapNhatNhanThayDoiTam();
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
            btnSuaBienBan.IsEnabled = bb != null;
            btnSuaChiTiet.IsEnabled = bb != null;

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
            txtChiTietMa.Text = bb.LaThayDoiTam ? $"{bb.MaBienBan} · tạm" : bb.MaBienBan;

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

            // --- Thong tin doan tau ---
            var pnl = pnlChiTietDoanTau;
            pnl.Children.Clear();
            var dt = DanhMucMauBaoTri.TimDoanTau(bb.MaDoanTau);
            if (dt != null)
            {
                ThemDongThongSo(pnl, "Hành trình", dt.HanhTrinh);
                ThemDongThongSo(pnl, "Xuất phát kế hoạch", dt.GioXuatPhatKH.ToString("HH:mm dd/MM/yyyy"));
                ThemDongThongSo(pnl, "Trạng thái chuyến", dt.NhanTrangThaiChuyen);
                ThemDongThongSo(pnl, "Đầu máy", dt.NhanDauMay);
                ThemDongThongSo(pnl, "Số toa · chiều dài", $"{dt.TongSoToa} toa · {dt.TongChieuDaiM:N1} m");
                ThemDongThongSo(pnl, "Trọng lượng đoàn", $"{dt.TongTrongLuongTan:N1} tấn");
                ThemDongThongSo(pnl, "Duyệt an toàn lập tàu", dt.DaDuyetAnToan ? "Đã duyệt" : "Chưa duyệt", laDongCuoi: true);
            }

            // --- Lich su kham cua doan tau (ke ca bien ban dang chon) ---
            var lichSu = LayToanBoKhamXe().Where(x => x.MaDoanTau == bb.MaDoanTau)
                                          .OrderByDescending(x => x.ThoiDiemKham)
                                          .ToList();
            icLichSuKham.ItemsSource = lichSu;
            txtTieuDeLichSu.Text = $"LỊCH SỬ KHÁM CỦA ĐOÀN TÀU ({lichSu.Count})";

            // --- Kham lai: chi goi y khi bien ban MOI NHAT cua doan tau khong dat ---
            var moiNhat = lichSu.FirstOrDefault();
            bool canKhamLai = moiNhat != null && !moiNhat.DuDieuKienXuatBen;
            btnKhamLai.Visibility = canKhamLai ? Visibility.Visible : Visibility.Collapsed;
            txtNutKhamLai.Text = $"Khám lại đoàn tàu {bb.SoHieuMacTau} sau khi khắc phục";
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
        // LAP / SUA BIEN BAN
        // =====================================================================

        private void BtnLapBienBan_Click(object sender, RoutedEventArgs e) => LapBienBan(_maDoanTauLoc);

        private void BtnSuaBienBan_Click(object sender, RoutedEventArgs e) => SuaBienBan();

        private void DongKhamXe_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SuaBienBan();

        private void BtnKhamLai_Click(object sender, RoutedEventArgs e)
        {
            if (dgKhamXe.SelectedItem is not KhamXeHienThi bb) return;
            LapBienBan(bb.MaDoanTau, bb.MaNguoiKham);
        }

        // chuyenTab = false khi goi tu tab Canh bao: mo dialog tai cho, khong nhay tab
        private void LapBienBan(int? maDoanTau = null, int? maNguoiKham = null, bool chuyenTab = true)
        {
            if (chuyenTab) tabBaoTri.SelectedIndex = 0;

            var dlg = new KhamXeDialog(null, maDoanTau, maNguoiKham) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var bb = dlg.KetQua;
            bb.MaKhamXe = _maTamKeTiep--;
            _khamXeTam[bb.MaKhamXe] = bb;

            LamMoiSauKhiLuu(bb);
            BaoKetQuaLuu(bb, laLapMoi: true);
        }

        private void SuaBienBan()
        {
            if (dgKhamXe.SelectedItem is not KhamXeHienThi dangChon) return;

            var dlg = new KhamXeDialog(dangChon.SaoChep()) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var bb = dlg.KetQua;
            _khamXeTam[bb.MaKhamXe] = bb;

            LamMoiSauKhiLuu(bb);
            BaoKetQuaLuu(bb, laLapMoi: false);
        }

        private void LamMoiSauKhiLuu(KhamXeHienThi bb)
        {
            LamMoiDanhSach(giuDongChon: false, maChonSau: bb.MaKhamXe);

            // Bo loc hien tai an mat ban ghi vua luu thi xoa loc de nguoi dung thay ngay
            if ((dgKhamXe.SelectedItem as KhamXeHienThi)?.MaKhamXe != bb.MaKhamXe)
            {
                _dangNapDuLieu = true;
                txtTimKhamXe.Text = string.Empty;
                cboKetLuan.SelectedIndex = 0;
                _dangNapDuLieu = false;
                if (_maDoanTauLoc.HasValue && _maDoanTauLoc != bb.MaDoanTau) _maDoanTauLoc = null;

                LamMoiDanhSach(giuDongChon: false, maChonSau: bb.MaKhamXe);
            }

            LamMoiCanhBao();
        }

        private static void BaoKetQuaLuu(KhamXeHienThi bb, bool laLapMoi)
        {
            string hanhDong = laLapMoi ? "Đã lập" : "Đã cập nhật";
            if (bb.DuDieuKienXuatBen)
            {
                ThongBaoDialog.ThanhCong(
                    $"{hanhDong} biên bản khám xe cho đoàn tàu {bb.SoHieuMacTau}.\n" +
                    "Kết luận: ĐỦ ĐIỀU KIỆN XUẤT BẾN (đạt 4/4 hạng mục).\n\n" +
                    "Biên bản đang hiển thị trên màn hình, chưa ghi vào CSDL.",
                    "Lưu biên bản khám xe");
            }
            else
            {
                ThongBaoDialog.CanhBao(
                    $"{hanhDong} biên bản khám xe cho đoàn tàu {bb.SoHieuMacTau}.\n" +
                    $"Kết luận: KHÔNG ĐỦ ĐIỀU KIỆN XUẤT BẾN ({bb.TomTatLoi}).\n\n" +
                    "Đoàn tàu không được xuất bến cho tới khi có biên bản khám lại đạt yêu cầu.\n" +
                    "Biên bản đang hiển thị trên màn hình, chưa ghi vào CSDL.",
                    "Đoàn tàu không đạt");
            }
        }

        // =====================================================================
        // THAY DOI TAM
        // =====================================================================

        private void CapNhatNhanThayDoiTam()
        {
            int soThayDoi = _khamXeTam.Count + _capDauTam.Count + _baoDuongTam.Count;
            bdThayDoiTam.Visibility = soThayDoi > 0 ? Visibility.Visible : Visibility.Collapsed;
            txtThayDoiTam.Text = $"{soThayDoi} thay đổi tạm · chưa ghi CSDL";
        }

        private void BtnHuyThayDoiTam_Click(object sender, RoutedEventArgs e)
        {
            bool dongY = ThongBaoDialog.XacNhan(
                "Bỏ toàn bộ biên bản khám xe, phiếu cấp nhiên liệu, phiếu bảo dưỡng vừa lập và các chỉnh sửa?\n" +
                "Màn hình sẽ hiển thị lại đúng dữ liệu gốc.",
                "Hoàn tác thay đổi tạm", nutDongY: "Hoàn tác", nutHuy: "Giữ lại");
            if (!dongY) return;

            _khamXeTam.Clear();
            _capDauTam.Clear();
            _baoDuongTam.Clear();
            _maTamKeTiep = -1;
            LamMoiDanhSach(giuDongChon: true);
            LamMoiNhienLieu(giuDongChon: true);
            LamMoiBaoDuong(giuDongChon: true);
            LamMoiCanhBao();
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
                // Tab thu 4 (Canh bao, index 3) chi tong hop, khong co ban ghi rieng de lap moi
            }
        }

        public void KichHoatNapLai()
        {
            LamMoiDanhSach(giuDongChon: true);
            LamMoiNhienLieu(giuDongChon: true);
            LamMoiBaoDuong(giuDongChon: true);
            LamMoiCanhBao();
        }

        public void KichHoatHuy()
        {
            switch (tabBaoTri.SelectedIndex)
            {
                case 0: BtnXoaLocKhamXe_Click(this, new RoutedEventArgs()); break;
                case 1: BtnXoaLocCapDau_Click(this, new RoutedEventArgs()); break;
                case 2: BtnXoaLocBaoDuong_Click(this, new RoutedEventArgs()); break;
                case 3: BtnXoaLocCanhBao_Click(this, new RoutedEventArgs()); break;
            }
        }
    }
}
