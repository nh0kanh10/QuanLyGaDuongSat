using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DTO.Common;
using GUI.Helpers;
using GUI.Views.KyThuat.Models;

namespace GUI.Views.KyThuat.Dialogs
{
    // =========================================================================
    // DIALOG THEM MOI / CAP NHAT TOA XE TRONG DOI (phuongtien.ToaXe)
    //
    // Giai doan dung giao dien: chi kiem tra du lieu nhap va tra ve ToaXeHienThi.
    // Tai trong truc = (tu trong + tai trong) / so truc, doi chieu nguong tham khao.
    // =========================================================================
    public partial class ToaXeDialog : Window
    {
        private static readonly Regex MauSoHieu = new(@"^[A-Z]+-[0-9]+$");

        private readonly ToaXeHienThi? _banGoc;
        private readonly HashSet<string> _soHieuDaCo;
        private readonly bool _dangKhoiTao;

        // Ghi nho gia tri do he thong tu dien, de biet nguoi dung da tu sua hay chua
        private string _soHieuTuGoiY = "";
        private ChungLoaiToaMau? _chungLoaiTruoc;

        public ToaXeHienThi? KetQua { get; private set; }

        public ToaXeDialog(ToaXeHienThi? banGoc, IEnumerable<string> soHieuDaCo)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;
            _soHieuDaCo = new HashSet<string>(soHieuDaCo, StringComparer.OrdinalIgnoreCase);

            SmartNumberFormatHelper.AttachDecimalKm(txtTuTrong, 0m, 999.99m);
            SmartNumberFormatHelper.AttachDecimalKm(txtTaiTrong, 0m, 999.99m);

            cboChungLoai.ItemsSource = DanhMucMauPhuongTien.ChungLoaiToa;
            txtNguongTruc.Text = $"ngưỡng {DanhMucMauPhuongTien.TaiTrongTrucToiDaTan:N1} t";

            if (banGoc == null)
                KhoiTaoThemMoi();
            else
                KhoiTaoCapNhat(banGoc);

            _chungLoaiTruoc = cboChungLoai.SelectedItem as ChungLoaiToaMau;
            _dangKhoiTao = false;
            CapNhatXemTruoc();

            Loaded += (_, _) =>
            {
                if (banGoc == null)
                {
                    txtSoHieu.Focus();
                    txtSoHieu.CaretIndex = txtSoHieu.Text.Length;
                }
                else
                {
                    txtTuTrong.Focus();
                    txtTuTrong.SelectAll();
                }
            };
        }

        private void KhoiTaoThemMoi()
        {
            txtTieuDe.Text = "THÊM MỚI TOA XE";
            txtTieuDePhu.Text = "Khai báo toa xe mới vào đội phương tiện";
            txtNutLuu.Text = "Thêm Toa Xe";

            cboChungLoai.SelectedIndex = 0;
            var cl = (ChungLoaiToaMau)cboChungLoai.SelectedItem;
            DienThongSoMacDinh(cl);
            DienSoHieuGoiY(cl);
            rdoSanSang.IsChecked = true;
        }

        private void KhoiTaoCapNhat(ToaXeHienThi tx)
        {
            txtTieuDe.Text = $"CẬP NHẬT TOA XE — {tx.SoHieuToaXe}";
            txtTieuDePhu.Text = "Số hiệu là khóa định danh nên không sửa sau khi đã tạo";
            txtNutLuu.Text = "Lưu Thay Đổi";

            cboChungLoai.SelectedItem = DanhMucMauPhuongTien.TimChungLoai(tx.MaChungLoaiCode);
            if (cboChungLoai.SelectedIndex < 0) cboChungLoai.SelectedIndex = 0;

            txtSoHieu.Text = tx.SoHieuToaXe;
            txtSoHieu.IsReadOnly = true;
            txtSoHieu.Background = (Brush)new BrushConverter().ConvertFrom("#F1F5F9")!;
            txtSoHieu.Foreground = (Brush)new BrushConverter().ConvertFrom("#475569")!;
            btnGoiYSoHieu.Visibility = Visibility.Collapsed;

            // Doi chung loai khi sua se lam lech tien to so hieu -> khoa lai
            cboChungLoai.IsEnabled = false;
            cboChungLoai.ToolTip = "Không đổi chủng loại của toa đã có. Hãy tạo toa mới nếu cần.";

            txtTuTrong.Text = FormatHelper.FormatKm(tx.TuTrongTan);
            txtTaiTrong.Text = FormatHelper.FormatKm(tx.TaiTrongToiDaTan);

            switch (tx.TrangThai)
            {
                case "DA_NOI": rdoDaNoi.IsChecked = true; break;
                case "BAO_DUONG": rdoBaoDuong.IsChecked = true; break;
                default: rdoSanSang.IsChecked = true; break;
            }
        }

        // =====================================================================
        // GOI Y SO HIEU & THONG SO MAC DINH
        // =====================================================================

        // So hieu trong ke tiep: NC-1xx, NML-2xx, BN-3xx ... (theo quy uoc seed data)
        private string TaoSoHieuGoiY(ChungLoaiToaMau cl)
        {
            string tienTo = $"{cl.MaChungLoaiCode}-";
            int coSo = (cl.MaChungLoai > 0 ? cl.MaChungLoai : 9) * 100;
            int lonNhat = coSo;

            foreach (string sh in _soHieuDaCo)
            {
                if (!sh.StartsWith(tienTo, StringComparison.OrdinalIgnoreCase)) continue;
                if (int.TryParse(sh[tienTo.Length..], out int so) && so > lonNhat)
                    lonNhat = so;
            }

            return $"{tienTo}{lonNhat + 1}";
        }

        private void DienSoHieuGoiY(ChungLoaiToaMau cl)
        {
            _soHieuTuGoiY = TaoSoHieuGoiY(cl);
            txtSoHieu.Text = _soHieuTuGoiY;
            txtSoHieu.CaretIndex = txtSoHieu.Text.Length;
            txtLoiSoHieu.Text = "";
        }

        private void DienThongSoMacDinh(ChungLoaiToaMau cl)
        {
            txtTuTrong.Text = FormatHelper.FormatKm(cl.TuTrongMacDinhTan);
            txtTaiTrong.Text = FormatHelper.FormatKm(cl.TaiTrongMacDinhTan);
            txtLoiTuTrong.Text = "";
            txtLoiTaiTrong.Text = "";
        }

        private void BtnGoiYSoHieu_Click(object sender, RoutedEventArgs e)
        {
            if (cboChungLoai.SelectedItem is ChungLoaiToaMau cl) DienSoHieuGoiY(cl);
            txtSoHieu.Focus();
        }

        private void BtnThongSoMacDinh_Click(object sender, RoutedEventArgs e)
        {
            if (cboChungLoai.SelectedItem is ChungLoaiToaMau cl) DienThongSoMacDinh(cl);
        }

        private void CboChungLoai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiChungLoai.Text = "";

            if (cboChungLoai.SelectedItem is not ChungLoaiToaMau cl) return;

            if (_banGoc == null)
            {
                // Chi thay so hieu neu nguoi dung chua tu go khac di
                string hienTai = txtSoHieu.Text.Trim();
                if (hienTai.Length == 0 || hienTai == _soHieuTuGoiY)
                    DienSoHieuGoiY(cl);

                // Chi thay tai trong neu dang la gia tri mac dinh cua chung loai truoc
                if (_chungLoaiTruoc == null || DangLaThongSoMacDinh(_chungLoaiTruoc))
                    DienThongSoMacDinh(cl);
            }

            _chungLoaiTruoc = cl;
            CapNhatXemTruoc();
        }

        private bool DangLaThongSoMacDinh(ChungLoaiToaMau cl)
            => FormatHelper.TryParseDecimal(txtTuTrong.Text, out decimal tu) && tu == cl.TuTrongMacDinhTan
            && FormatHelper.TryParseDecimal(txtTaiTrong.Text, out decimal tai) && tai == cl.TaiTrongMacDinhTan;

        // =====================================================================
        // XEM TRUOC
        // =====================================================================

        private void TruongNhap_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            if (sender == txtSoHieu) txtLoiSoHieu.Text = "";
            if (sender == txtTuTrong) txtLoiTuTrong.Text = "";
            if (sender == txtTaiTrong) txtLoiTaiTrong.Text = "";
            CapNhatXemTruoc();
        }

        private void CapNhatXemTruoc()
        {
            var conv = new BrushConverter();
            if (cboChungLoai.SelectedItem is not ChungLoaiToaMau cl) return;

            // --- The chung loai ---
            txtMaChungLoai.Text = cl.MaChungLoaiCode;
            bdMaChungLoai.Background = (Brush)conv.ConvertFrom(cl.MauNen)!;
            bdMaChungLoai.BorderBrush = (Brush)conv.ConvertFrom(cl.MauVien)!;
            txtMaChungLoai.Foreground = (Brush)conv.ConvertFrom(cl.MauChu)!;
            txtNhomToa.Text = cl.LaToaHang ? "TOA HÀNG" : $"TOA KHÁCH · {cl.SucChuaThamKhao} chỗ";
            txtMoTaChungLoai.Text = cl.TenMoTa;

            pnlThongSoChungLoai.Children.Clear();
            ThemDongThongSo("Chiều dài chuẩn", $"{cl.ChieuDaiChuanM:N1} m");
            ThemDongThongSo("Số trục", $"{cl.SoTruc} trục");
            ThemDongThongSo("Tự trọng tham khảo", $"{cl.TuTrongMacDinhTan:N1} t");
            ThemDongThongSo("Tải trọng tham khảo", $"{cl.TaiTrongMacDinhTan:N1} t", laDongCuoi: true);

            // --- Tai trong truc ---
            decimal tuTrong = FormatHelper.TryParseDecimal(txtTuTrong.Text, out decimal a) ? a : 0m;
            decimal taiTrong = FormatHelper.TryParseDecimal(txtTaiTrong.Text, out decimal b) ? b : 0m;
            decimal toanTai = tuTrong + taiTrong;
            decimal truc = cl.SoTruc > 0 ? Math.Round(toanTai / cl.SoTruc, 2) : 0m;
            decimal nguong = DanhMucMauPhuongTien.TaiTrongTrucToiDaTan;
            double tyLe = nguong > 0 ? Math.Min(100.0, (double)(truc / nguong) * 100.0) : 0.0;

            string mau = truc > nguong ? "#B91C1C" : tyLe >= 85 ? "#B45309" : "#15803D";

            txtTaiTrongTruc.Text = truc.ToString("N2");
            txtTaiTrongTruc.Foreground = (Brush)conv.ConvertFrom(mau)!;
            bdVachTruc.Background = (Brush)conv.ConvertFrom(mau)!;
            colTruc.Width = new GridLength(tyLe, GridUnitType.Star);
            colTrucConLai.Width = new GridLength(100 - tyLe, GridUnitType.Star);
            txtToanTai.Text = $"{toanTai:N1} t";

            txtMoTaTruc.Text = truc > nguong
                ? $"Vượt ngưỡng {nguong:N1} t/trục khi chở đầy. Toa sẽ bị cảnh báo lúc lập tàu."
                : tyLe >= 85
                    ? "Sát ngưỡng tải trọng trục cho phép."
                    : "Nằm trong ngưỡng tải trọng trục cho phép.";
        }

        private void ThemDongThongSo(string nhan, string giaTri, bool laDongCuoi = false)
        {
            var hang = new Grid { Height = 22 };
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tbNhan = new TextBlock { Text = nhan, Style = (Style)FindResource("KtNhanThongSo") };
            var tbGiaTri = new TextBlock { Text = giaTri, Style = (Style)FindResource("KtGiaTriThongSo") };
            Grid.SetColumn(tbGiaTri, 1);
            hang.Children.Add(tbNhan);
            hang.Children.Add(tbGiaTri);

            pnlThongSoChungLoai.Children.Add(new Border
            {
                Child = hang,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#E2E8F0")!,
                BorderThickness = new Thickness(0, 0, 0, laDongCuoi ? 0 : 1)
            });
        }

        // =====================================================================
        // LUU / HUY
        // =====================================================================

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            bool hopLe = true;
            Control? oLoiDauTien = null;

            void BaoLoi(TextBlock tbLoi, string thongDiep, Control o)
            {
                tbLoi.Text = thongDiep;
                hopLe = false;
                oLoiDauTien ??= o;
            }

            var cl = cboChungLoai.SelectedItem as ChungLoaiToaMau;
            if (cl == null)
                BaoLoi(txtLoiChungLoai, "Vui lòng chọn chủng loại toa.", cboChungLoai);

            string soHieu = txtSoHieu.Text.Trim().ToUpperInvariant();
            if (_banGoc == null)
            {
                if (soHieu.Length == 0)
                    BaoLoi(txtLoiSoHieu, "Vui lòng nhập số hiệu toa xe.", txtSoHieu);
                else if (!MauSoHieu.IsMatch(soHieu))
                    BaoLoi(txtLoiSoHieu, "Số hiệu phải có dạng LOẠI-SỐ, ví dụ NML-205.", txtSoHieu);
                else if (cl != null && !soHieu.StartsWith(cl.MaChungLoaiCode + "-", StringComparison.OrdinalIgnoreCase))
                    BaoLoi(txtLoiSoHieu, $"Tiền tố phải khớp chủng loại đã chọn ({cl.MaChungLoaiCode}-...).", txtSoHieu);
                else if (_soHieuDaCo.Contains(soHieu))
                    BaoLoi(txtLoiSoHieu, $"Số hiệu {soHieu} đã tồn tại (trùng khóa UNIQUE SoHieuToaXe).", txtSoHieu);
            }

            // CHECK TuTrongTan > 0, DECIMAL(5,2)
            if (!FormatHelper.TryParseDecimal(txtTuTrong.Text, out decimal tuTrong) || tuTrong <= 0 || tuTrong > 999.99m)
                BaoLoi(txtLoiTuTrong, "Tự trọng phải lớn hơn 0 và không quá 999.99 tấn.", txtTuTrong);

            // CHECK TaiTrongToiDaTan >= 0
            if (!FormatHelper.TryParseDecimal(txtTaiTrong.Text, out decimal taiTrong) || taiTrong < 0 || taiTrong > 999.99m)
                BaoLoi(txtLoiTaiTrong, "Tải trọng phải từ 0 đến 999.99 tấn.", txtTaiTrong);

            if (!hopLe || cl == null)
            {
                oLoiDauTien?.Focus();
                return;
            }

            var kq = _banGoc?.SaoChep() ?? new ToaXeHienThi();
            kq.SoHieuToaXe = _banGoc?.SoHieuToaXe ?? soHieu;
            kq.MaChungLoai = cl.MaChungLoai;
            kq.MaChungLoaiCode = cl.MaChungLoaiCode;
            kq.TenMoTa = cl.TenMoTa;
            kq.ChieuDaiChuanM = cl.ChieuDaiChuanM;
            kq.SoTruc = cl.SoTruc;
            kq.TuTrongTan = tuTrong;
            kq.TaiTrongToiDaTan = taiTrong;
            kq.TrangThai = rdoDaNoi.IsChecked == true ? "DA_NOI"
                         : rdoBaoDuong.IsChecked == true ? "BAO_DUONG"
                         : "SAN_SANG";

            KetQua = kq;
            DialogResult = true;
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                BtnLuu_Click(this, new RoutedEventArgs());
            }
        }
    }
}
