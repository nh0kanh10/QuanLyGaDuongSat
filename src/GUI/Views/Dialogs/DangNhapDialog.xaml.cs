using System.Windows;
using System.Windows.Input;
using BUS.Services;
using GUI.Helpers;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // Cua so Dang nhap (Sprint 3, Dat) - mo truoc MainWindow (PhienDangNhapHelper).
    // DialogResult = true khi dang nhap thanh cong VA (neu bi buoc) da doi mat khau.
    // Moi kiem tra / ghi lich su nam o PhanQuyenService.DangNhap; cua so chi hien ket qua.
    // =========================================================================
    public partial class DangNhapDialog : Window
    {
        private readonly string? _tenGoiY;

        public DangNhapDialog(string? tenDangNhapGoiY = null, string? thongBaoDau = null)
        {
            InitializeComponent();
            _tenGoiY = tenDangNhapGoiY;

            string mayChu = KetNoiCsdlHelper.MayChuDangDung is "." or "(local)" or "localhost"
                ? "máy này" : KetNoiCsdlHelper.MayChuDangDung;
            string csdl = $"{DTO.Common.CauHinhUngDung.TenCsdl} trên {mayChu}";
            txtKetNoi.Text = KetNoiCsdlHelper.KetNoiThanhCong
                ? $"CSDL: {csdl}"
                : $"⚠ Chưa kết nối được CSDL {csdl}. Kiểm tra SQL Server rồi mở lại ứng dụng.";
            txtKetNoi.ToolTip = KetNoiCsdlHelper.MoTaCauHinh;

            if (!string.IsNullOrEmpty(thongBaoDau)) HienThongBao(thongBaoDau, laLoi: false);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_tenGoiY))
            {
                txtTenDangNhap.Text = _tenGoiY;
                txtMatKhau.Focus();
            }
            else
            {
                txtTenDangNhap.Focus();
            }
            CapNhatCapsLock();
        }

        private void BtnDangNhap_Click(object sender, RoutedEventArgs e)
        {
            string ten = txtTenDangNhap.Text.Trim();
            if (ten.Length == 0) { HienThongBao("Vui lòng nhập tên đăng nhập."); txtTenDangNhap.Focus(); return; }
            if (txtMatKhau.Password.Length == 0) { HienThongBao("Vui lòng nhập mật khẩu."); txtMatKhau.Focus(); return; }

            btnDangNhap.IsEnabled = false;
            txtNutDangNhap.Text = "Đang kiểm tra...";
            Mouse.OverrideCursor = Cursors.Wait;
            DTO.QuanTri.KetQuaDangNhap kq;
            try
            {
                kq = PhanQuyenService.DangNhap(ten, txtMatKhau.Password);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                btnDangNhap.IsEnabled = true;
                txtNutDangNhap.Text = "Đăng Nhập";
            }

            if (!kq.ThanhCong)
            {
                // Xoa o mat khau TRUOC khi hien loi: PasswordChanged an khung loi (O_ThayDoi)
                txtMatKhau.Clear();
                HienThongBao(kq.ThongBao);
                txtMatKhau.Focus();
                return;
            }

            if (kq.PhaiDoiMatKhau)
            {
                var doi = new DoiMatKhauDialog(batBuoc: true, matKhauHienTai: txtMatKhau.Password) { Owner = this };
                if (doi.ShowDialog() != true)
                {
                    PhanQuyenService.DangXuat();
                    txtMatKhau.Clear();
                    HienThongBao("Bạn phải đổi mật khẩu trước khi vào hệ thống (quản trị viên vừa đặt lại mật khẩu).", laLoi: false);
                    txtMatKhau.Focus();
                    return;
                }
            }

            DialogResult = true;
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void O_ThayDoi(object sender, RoutedEventArgs e)
        {
            if (bdLoi.Visibility == Visibility.Visible && iconLoi.Symbol == Wpf.Ui.Controls.SymbolRegular.ErrorCircle24)
                bdLoi.Visibility = Visibility.Collapsed;
        }

        private void MatKhau_KeyUp(object sender, KeyEventArgs e) => CapNhatCapsLock();

        private void CapNhatCapsLock()
            => txtCapsLock.Visibility = Keyboard.IsKeyToggled(Key.CapsLock) ? Visibility.Visible : Visibility.Collapsed;

        private void HienThongBao(string thongBao, bool laLoi = true)
        {
            txtLoi.Text = thongBao;
            iconLoi.Symbol = laLoi ? Wpf.Ui.Controls.SymbolRegular.ErrorCircle24 : Wpf.Ui.Controls.SymbolRegular.Info24;
            iconLoi.Foreground = laLoi ? Mau("#B91C1C") : Mau("#1D4ED8");
            txtLoi.Foreground = laLoi ? Mau("#991B1B") : Mau("#1E3A8A");
            bdLoi.Background = laLoi ? Mau("#FEF2F2") : Mau("#EFF6FF");
            bdLoi.BorderBrush = laLoi ? Mau("#FCA5A5") : Mau("#93C5FD");
            bdLoi.Visibility = Visibility.Visible;
        }

        private static System.Windows.Media.SolidColorBrush Mau(string ma)
            => (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFrom(ma)!;

        private void KeoCuaSo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
            }
            else if (e.Key == Key.CapsLock)
            {
                Dispatcher.BeginInvoke(CapNhatCapsLock);
            }
        }
    }
}
