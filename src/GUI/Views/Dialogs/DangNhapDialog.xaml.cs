using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using BUS.Services;
using DAL.Connection;
using DTO.Common;
using GUI.Helpers;
using Wpf.Ui.Controls;

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

            CapNhatHienThiKetNoi();

            if (!string.IsNullOrEmpty(thongBaoDau)) HienThongBao(thongBaoDau, laLoi: false);
        }

        private void CapNhatHienThiKetNoi()
        {
            bool daKetNoi = KetNoiCsdlHelper.KetNoiThanhCong;
            string mayChu = KetNoiCsdlHelper.MayChuDangDung is "." or "(local)" or "localhost"
                ? "máy này" : KetNoiCsdlHelper.MayChuDangDung;
            string csdl = $"{CauHinhUngDung.TenCsdl} trên {mayChu}";

            // Vô hiệu hoá / Kích hoạt các nút nhập & đăng nhập theo trạng thái kết nối
            btnDangNhap.IsEnabled = daKetNoi;
            btnDangNhap.Opacity = daKetNoi ? 1.0 : 0.5;
            btnDangNhapNhanhAdmin.IsEnabled = daKetNoi;
            bdDangNhapNhanh.Opacity = daKetNoi ? 1.0 : 0.45;
            txtTenDangNhap.IsEnabled = daKetNoi;
            txtMatKhau.IsEnabled = daKetNoi;

            if (daKetNoi)
            {
                txtKetNoi.Text = $"CSDL: {csdl}";
                txtKetNoi.Foreground = Mau("#93C5FD");
                iconKetNoi.Symbol = SymbolRegular.Database24;
                iconKetNoi.Foreground = Mau("#38BDF8");

                btnDangNhap.ToolTip = null;
                btnDangNhapNhanhAdmin.ToolTip = "Đăng nhập ngay với tài khoản Quản trị viên toàn quyền (admin) - Phím tắt F2";
                if (bdLoi.Visibility == Visibility.Visible && txtLoi.Text.Contains("Chưa kết nối CSDL"))
                {
                    bdLoi.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                txtKetNoi.Text = $"⚠ Chưa kết nối CSDL {csdl}. Dùng 2 nút bên dưới để cấu hình.";
                txtKetNoi.Foreground = Mau("#FDE68A");
                iconKetNoi.Symbol = SymbolRegular.Database24;
                iconKetNoi.Foreground = Mau("#F59E0B");

                btnDangNhap.ToolTip = "Vui lòng kết nối CSDL trước khi đăng nhập";
                btnDangNhapNhanhAdmin.ToolTip = "Vui lòng kết nối CSDL trước khi đăng nhập";
                HienThongBao("Chưa kết nối CSDL. Các nút đăng nhập đã tạm khóa; vui lòng dùng 2 nút phía dưới để cấu hình.", laLoi: true);
            }
            txtKetNoi.ToolTip = KetNoiCsdlHelper.MoTaCauHinh;
        }

        private void BtnNhapChuoiKetNoi_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CauHinhKetNoiDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                CapNhatHienThiKetNoi();
                if (KetNoiCsdlHelper.KetNoiThanhCong)
                {
                    HienThongBao("Đã lưu và kết nối thành công tới CSDL!", laLoi: false);
                }
            }
        }

        private void BtnAutoMdf_Click(object sender, RoutedEventArgs e)
        {
            string thuMucApp = AppDomain.CurrentDomain.BaseDirectory;
            string? duongDanMdf = null;

            // 1. Tìm trong thư mục chạy ứng dụng
            string fileMacDinh = Path.Combine(thuMucApp, "QuanLyDuongSatV2.mdf");
            if (File.Exists(fileMacDinh))
            {
                duongDanMdf = fileMacDinh;
            }
            else
            {
                try
                {
                    var cacFileMdf = Directory.GetFiles(thuMucApp, "*.mdf", SearchOption.TopDirectoryOnly);
                    if (cacFileMdf.Length > 0)
                    {
                        duongDanMdf = cacFileMdf[0];
                    }
                }
                catch { }
            }

            // 2. Nếu không tìm thấy tự động -> Mở hộp thoại cho người dùng chọn file
            if (string.IsNullOrEmpty(duongDanMdf))
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Chọn tệp tin CSDL SQL Server (.mdf) cho hệ thống đường sắt",
                    Filter = "SQL Server Database (*.mdf)|*.mdf|Tất cả tệp (*.*)|*.*",
                    FileName = "QuanLyDuongSatV2.mdf"
                };
                if (ofd.ShowDialog() != true) return;
                duongDanMdf = ofd.FileName;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            btnAutoMdf.IsEnabled = false;

            try
            {
                // Kích hoạt dịch vụ LocalDB
                KetNoiCsdlHelper.KichHoatLocalDbService();

                // Tạo chuỗi kết nối AttachDbFilename
                string chuoiMdf = KetNoiCsdlHelper.TaoChuoiKetNoiMdf(duongDanMdf);

                // Thử kết nối
                using (var conn = new SqlConnection(chuoiMdf))
                {
                    conn.Open();
                }

                // Lưu cấu hình vào appsettings.Local.json
                CauHinhUngDung.LuuCauHinhLocal(null, "QuanLyDuongSatV2", chuoiMdf);

                // Áp dụng cho toàn bộ hệ thống
                DatabaseHelper.SetConnectionString(chuoiMdf);
                KetNoiCsdlHelper.DatTrangThaiKetNoi(true, $"(localdb)\\MSSQLLocalDB [{Path.GetFileName(duongDanMdf)}]");
                CapNhatHienThiKetNoi();

                ThongBaoDialog.ThanhCong(
                    $"Đã gắn tệp CSDL MDF thành công vào SQL Server LocalDB!\n\nTệp: {duongDanMdf}\nCấu hình đã được lưu tự động.",
                    "Kết Nối MDF Thành Công",
                    this);
            }
            catch (Exception ex)
            {
                ThongBaoDialog.CanhBao(
                    $"Không thể kết nối tệp CSDL MDF qua LocalDB:\n{ex.Message}\n\nVui lòng đảm bảo:\n1. Máy tính đã cài đặt gói SQL Server LocalDB 2022.\n2. Tệp .mdf không bị ứng dụng khác khóa quyền truy cập.",
                    "Lỗi Kết Nối MDF",
                    this);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                btnAutoMdf.IsEnabled = true;
            }
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
            if (!KetNoiCsdlHelper.KetNoiThanhCong)
            {
                HienThongBao("Hệ thống chưa kết nối CSDL. Vui lòng cấu hình kết nối trước!");
                return;
            }

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

        private void BtnDangNhapNhanhAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (!KetNoiCsdlHelper.KetNoiThanhCong)
            {
                HienThongBao("Hệ thống chưa kết nối CSDL. Vui lòng cấu hình kết nối trước!");
                return;
            }

            txtTenDangNhap.Text = "admin";
            txtMatKhau.Password = "Demo@123";
            BtnDangNhap_Click(btnDangNhap, new RoutedEventArgs());
        }

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
            else if (e.Key == Key.F2)
            {
                e.Handled = true;
                if (!KetNoiCsdlHelper.KetNoiThanhCong)
                {
                    HienThongBao("Hệ thống chưa kết nối CSDL. Vui lòng cấu hình kết nối trước!");
                    return;
                }
                BtnDangNhapNhanhAdmin_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.CapsLock)
            {
                Dispatcher.BeginInvoke(CapNhatCapsLock);
            }
        }
    }
}
