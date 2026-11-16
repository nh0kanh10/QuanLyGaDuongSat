using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using DTO.Common;
using GUI.Helpers;
using Wpf.Ui.Controls;

namespace GUI.Views.Dialogs
{
    public partial class CauHinhKetNoiDialog : Window
    {
        public bool DaLuuThanhCong { get; private set; }

        public CauHinhKetNoiDialog()
        {
            InitializeComponent();
            NapGiaTriHienTai();
        }

        private void NapGiaTriHienTai()
        {
            txtMayChu.Text = string.IsNullOrEmpty(CauHinhUngDung.MayChu) ? @".\SQLEXPRESS" : CauHinhUngDung.MayChu;
            txtTenCsdl.Text = string.IsNullOrEmpty(CauHinhUngDung.TenCsdl) ? "QuanLyDuongSatV2" : CauHinhUngDung.TenCsdl;

            if (!string.IsNullOrEmpty(CauHinhUngDung.ChuoiKetNoiTuyChon))
            {
                txtChuoiKetNoi.Text = CauHinhUngDung.ChuoiKetNoiTuyChon;
                rbChuoiKetNoi.IsChecked = true;
            }
            else
            {
                txtChuoiKetNoi.Text = CauHinhUngDung.ChuoiKetNoi();
                rbMayChu.IsChecked = true;
            }
        }

        private void Rb_Checked(object sender, RoutedEventArgs e)
        {
            if (pnlMayChu == null || pnlChuoiKetNoi == null) return;
            bool dungMayChu = rbMayChu.IsChecked == true;
            pnlMayChu.Visibility = dungMayChu ? Visibility.Visible : Visibility.Collapsed;
            pnlChuoiKetNoi.Visibility = dungMayChu ? Visibility.Collapsed : Visibility.Visible;
            bdKetQuaTest.Visibility = Visibility.Collapsed;
        }

        private void BtnChonNhanh_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag != null)
            {
                txtMayChu.Text = elem.Tag.ToString();
            }
        }

        private string LayChuoiKetNoiHienTai()
        {
            if (rbMayChu.IsChecked == true)
            {
                string mayChu = txtMayChu.Text.Trim();
                string csdl = txtTenCsdl.Text.Trim();
                return $"Server={mayChu};Database={csdl};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;";
            }
            return txtChuoiKetNoi.Text.Trim();
        }

        private void BtnKiemTra_Click(object sender, RoutedEventArgs e)
        {
            string chuoi = LayChuoiKetNoiHienTai();
            if (string.IsNullOrWhiteSpace(chuoi))
            {
                HienKetQua(false, "Vui lòng nhập thông tin kết nối!");
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            btnKiemTra.IsEnabled = false;

            try
            {
                using var conn = new SqlConnection(chuoi);
                conn.Open();
                HienKetQua(true, "Kết nối thành công tới SQL Server!");
            }
            catch (Exception ex)
            {
                HienKetQua(false, $"Kết nối thất bại: {ex.Message}");
            }
            finally
            {
                Mouse.OverrideCursor = null;
                btnKiemTra.IsEnabled = true;
            }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            string? mayChu = rbMayChu.IsChecked == true ? txtMayChu.Text.Trim() : null;
            string? tenCsdl = rbMayChu.IsChecked == true ? txtTenCsdl.Text.Trim() : null;
            string? chuoiKetNoi = rbChuoiKetNoi.IsChecked == true ? txtChuoiKetNoi.Text.Trim() : null;

            bool ok = CauHinhUngDung.LuuCauHinhLocal(mayChu, tenCsdl, chuoiKetNoi);
            if (!ok)
            {
                HienKetQua(false, "Không thể ghi tệp cấu hình appsettings.Local.json!");
                return;
            }

            // Áp dụng chuỗi mới cho toàn hệ thống
            string chuoiMoi = CauHinhUngDung.ChuoiKetNoi();
            DatabaseHelper.SetConnectionString(chuoiMoi);

            bool ketNoiOk = DatabaseHelper.TestConnection();
            KetNoiCsdlHelper.DatTrangThaiKetNoi(ketNoiOk, mayChu ?? "Tùy chỉnh");

            DaLuuThanhCong = true;
            DialogResult = true;
            Close();
        }

        private void HienKetQua(bool thanhCong, string thongDiep)
        {
            bdKetQuaTest.Visibility = Visibility.Visible;
            if (thanhCong)
            {
                bdKetQuaTest.Background = Mau("#F0FDF4");
                bdKetQuaTest.BorderBrush = Mau("#86EFAC");
                iconKetQuaTest.Symbol = SymbolRegular.CheckmarkCircle24;
                iconKetQuaTest.Foreground = Mau("#16A34A");
                txtKetQuaTest.Foreground = Mau("#15803D");
            }
            else
            {
                bdKetQuaTest.Background = Mau("#FEF2F2");
                bdKetQuaTest.BorderBrush = Mau("#FCA5A5");
                iconKetQuaTest.Symbol = SymbolRegular.DismissCircle24;
                iconKetQuaTest.Foreground = Mau("#DC2626");
                txtKetQuaTest.Foreground = Mau("#B91C1C");
            }
            txtKetQuaTest.Text = thongDiep;
        }

        private static SolidColorBrush Mau(string hex) =>
            (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
