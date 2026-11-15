using System.Windows;
using System.Windows.Input;
using BUS.Services;
using DTO.Common;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // Doi mat khau cua nguoi dang dang nhap (Sprint 3, Dat).
    //   batBuoc = true : mo ngay sau dang nhap khi quan tri vien vua dat lai mat khau
    //                    (da biet mat khau hien tai -> an o nhap mat khau cu).
    //   batBuoc = false: mo tu menu tai khoan tren MainWindow.
    // Luat + ghi CSDL: PhanQuyenService.DoiMatKhau.
    // =========================================================================
    public partial class DoiMatKhauDialog : Window
    {
        private readonly bool _batBuoc;
        private readonly string? _matKhauHienTai;

        // [12/11] Nhap sai mat khau hien tai du so lan -> tai khoan vua bi khoa; cua so goi phai dang xuat.
        public bool BiKhoaTaiKhoan { get; private set; }

        public DoiMatKhauDialog(bool batBuoc = false, string? matKhauHienTai = null)
        {
            InitializeComponent();
            _batBuoc = batBuoc;
            _matKhauHienTai = matKhauHienTai;

            txtTieuDePhu.Text = $"Tài khoản: {PhienLamViec.TenDangNhap} · {PhienLamViec.HoTen}";
            if (batBuoc) bdBatBuoc.Visibility = Visibility.Visible;
            if (batBuoc && !string.IsNullOrEmpty(matKhauHienTai)) pnlMatKhauCu.Visibility = Visibility.Collapsed;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (pnlMatKhauCu.Visibility == Visibility.Visible) txtMatKhauCu.Focus();
            else txtMatKhauMoi.Focus();
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            string cu = pnlMatKhauCu.Visibility == Visibility.Visible ? txtMatKhauCu.Password : _matKhauHienTai ?? "";
            if (!PhanQuyenService.DoiMatKhau(cu, txtMatKhauMoi.Password, txtNhapLai.Password, out string loi, out bool biKhoa))
            {
                if (biKhoa)
                {
                    BiKhoaTaiKhoan = true;
                    ThongBaoDialog.CanhBao(loi, "Tài Khoản Bị Khóa", this);
                    DialogResult = false;
                    return;
                }
                // Xoa o mat khau cu TRUOC khi hien loi (PasswordChanged -> O_ThayDoi xoa dong loi)
                if (loi.StartsWith("Mật khẩu hiện tại")) txtMatKhauCu.Clear();
                txtLoi.Text = loi;
                if (pnlMatKhauCu.Visibility == Visibility.Visible && txtMatKhauCu.Password.Length == 0) txtMatKhauCu.Focus();
                return;
            }
            ThongBaoDialog.ThanhCong("Đã đổi mật khẩu. Lần đăng nhập sau dùng mật khẩu mới.", "Đổi Mật Khẩu", this);
            DialogResult = true;
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void O_ThayDoi(object sender, RoutedEventArgs e) => txtLoi.Text = "";

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
        }
    }
}
