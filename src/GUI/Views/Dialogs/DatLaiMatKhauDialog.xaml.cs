using System;
using System.Windows;
using System.Windows.Input;

namespace GUI.Views.Dialogs
{
    // Quan tri vien dat lai mat khau cho mot tai khoan (Sprint 3, Dat). Luu qua callback QuanTriService.DatLaiMatKhau.
    public partial class DatLaiMatKhauDialog : Window
    {
        private readonly Func<string, string, string?> _luu;

        public DatLaiMatKhauDialog(string tenDangNhap, string hoTen, Func<string, string, string?> luu)
        {
            InitializeComponent();
            _luu = luu;
            txtTieuDePhu.Text = $"Tài khoản: {tenDangNhap} · {hoTen}";
            Loaded += (_, _) => txtMatKhau.Focus();
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            string? loi = _luu(txtMatKhau.Password, txtNhapLai.Password);
            if (loi != null) { txtLoi.Text = loi; return; }
            DialogResult = true;
        }

        private void O_ThayDoi(object sender, RoutedEventArgs e) => txtLoi.Text = "";
        private void BtnHuy_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { e.Handled = true; DialogResult = false; }
        }
    }
}
