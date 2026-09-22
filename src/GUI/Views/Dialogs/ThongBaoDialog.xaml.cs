using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace GUI.Views.Dialogs
{
    public enum LoaiThongBao
    {
        Loi,
        ThanhCong,
        CanhBao,
        ThongTin,
        XacNhan
    }

    public partial class ThongBaoDialog : Window
    {
        public bool KetQuaXacNhan { get; private set; } = false;

        public ThongBaoDialog(
            string thongDiep, 
            string tieuDe = "Thông Báo", 
            LoaiThongBao loai = LoaiThongBao.ThongTin,
            string nutChinh = "Đã hiểu",
            string nutHuy = "Hủy bỏ",
            bool laHanhDongXoa = false)
        {
            InitializeComponent();
            txtTieuDe.Text = tieuDe;
            txtNoiDung.Text = thongDiep;
            txtNutChinh.Text = nutChinh;
            txtNutHuy.Text = nutHuy;

            CauHinhGiaoDien(loai, laHanhDongXoa);
        }

        private static SolidColorBrush Brush(string hex) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        private void CauHinhGiaoDien(LoaiThongBao loai, bool laHanhDongXoa)
        {
            switch (loai)
            {
                case LoaiThongBao.Loi:
                    gridHeader.Background = Brush("#B91C1C");
                    bdWindowBorder.BorderBrush = Brush("#991B1B");
                    borderIcon.Background = Brush("#F8FAFC");
                    borderIcon.BorderBrush = Brush("#FCA5A5");
                    iconThongBao.Symbol = SymbolRegular.DismissCircle24;
                    iconThongBao.Foreground = Brush("#DC2626");
                    txtTieuDeHeader.Text = "LỖI THAO TÁC / TRUY VẤN DỮ LIỆU";
                    btnChinh.Background = Brush("#475569");
                    btnChinh.BorderBrush = Brush("#334155");
                    btnHuy.Visibility = Visibility.Collapsed;
                    break;

                case LoaiThongBao.ThanhCong:
                    gridHeader.Background = Brush("#15803D");
                    bdWindowBorder.BorderBrush = Brush("#166534");
                    borderIcon.Background = Brush("#F8FAFC");
                    borderIcon.BorderBrush = Brush("#86EFAC");
                    iconThongBao.Symbol = SymbolRegular.CheckmarkCircle24;
                    iconThongBao.Foreground = Brush("#16A34A");
                    txtTieuDeHeader.Text = "THAO TÁC THÀNH CÔNG";
                    btnChinh.Background = Brush("#15803D");
                    btnChinh.BorderBrush = Brush("#166534");
                    btnHuy.Visibility = Visibility.Collapsed;
                    break;

                case LoaiThongBao.CanhBao:
                    gridHeader.Background = Brush("#C2410C");
                    bdWindowBorder.BorderBrush = Brush("#9A3412");
                    borderIcon.Background = Brush("#F8FAFC");
                    borderIcon.BorderBrush = Brush("#FCD34D");
                    iconThongBao.Symbol = SymbolRegular.Warning24;
                    iconThongBao.Foreground = Brush("#EA580C");
                    txtTieuDeHeader.Text = "CẢNH BÁO AN TOÀN HỆ THỐNG";
                    btnChinh.Background = Brush("#003B73");
                    btnChinh.BorderBrush = Brush("#002855");
                    btnHuy.Visibility = Visibility.Collapsed;
                    break;

                case LoaiThongBao.XacNhan:
                    btnHuy.Visibility = Visibility.Visible;

                    if (laHanhDongXoa)
                    {
                        gridHeader.Background = Brush("#991B1B");
                        bdWindowBorder.BorderBrush = Brush("#7F1D1D");
                        borderIcon.Background = Brush("#F8FAFC");
                        borderIcon.BorderBrush = Brush("#FCA5A5");
                        iconThongBao.Symbol = SymbolRegular.Delete24;
                        iconThongBao.Foreground = Brush("#DC2626");
                        txtTieuDeHeader.Text = "XÁC NHẬN XÓA DỮ LIỆU KHỎI CSDL";
                        btnChinh.Background = Brush("#B91C1C");
                        btnChinh.BorderBrush = Brush("#991B1B");
                    }
                    else
                    {
                        gridHeader.Background = Brush("#003B73");
                        bdWindowBorder.BorderBrush = Brush("#002855");
                        borderIcon.Background = Brush("#F8FAFC");
                        borderIcon.BorderBrush = Brush("#CBD5E1");
                        iconThongBao.Symbol = SymbolRegular.QuestionCircle24;
                        iconThongBao.Foreground = Brush("#003B73");
                        txtTieuDeHeader.Text = "XÁC NHẬN ĐIỀU ĐỘ CHẠY TÀU";
                        btnChinh.Background = Brush("#003B73");
                        btnChinh.BorderBrush = Brush("#002855");
                    }
                    break;

                case LoaiThongBao.ThongTin:
                default:
                    gridHeader.Background = Brush("#003B73");
                    bdWindowBorder.BorderBrush = Brush("#002855");
                    borderIcon.Background = Brush("#F8FAFC");
                    borderIcon.BorderBrush = Brush("#CBD5E1");
                    iconThongBao.Symbol = SymbolRegular.Info24;
                    iconThongBao.Foreground = Brush("#003B73");
                    txtTieuDeHeader.Text = "THÔNG BÁO HỆ THỐNG ĐIỀU HÀNH";
                    btnChinh.Background = Brush("#003B73");
                    btnChinh.BorderBrush = Brush("#002855");
                    btnHuy.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            KetQuaXacNhan = false;
            Close();
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            KetQuaXacNhan = false;
            Close();
        }

        private void BtnChinh_Click(object sender, RoutedEventArgs e)
        {
            KetQuaXacNhan = true;
            Close();
        }

        // Phương thức tĩnh gọi nhanh dialog
        private static Window? TimCuaSoCha(Window? owner = null)
        {
            if (owner != null && owner.IsLoaded && owner.IsVisible) return owner;
            if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsLoaded && Application.Current.MainWindow.IsVisible)
            {
                return Application.Current.MainWindow;
            }
            return null;
        }

        public static void Loi(string thongDiep, string tieuDe = "Lỗi Thao Tác", Window? owner = null)
        {
            var winOwner = TimCuaSoCha(owner);
            var dialog = new ThongBaoDialog(thongDiep, tieuDe, LoaiThongBao.Loi, "Đã hiểu")
            {
                Owner = winOwner,
                WindowStartupLocation = winOwner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };
            dialog.ShowDialog();
        }

        public static void ThanhCong(string thongDiep, string tieuDe = "Thành Công", Window? owner = null)
        {
            var winOwner = TimCuaSoCha(owner);
            var dialog = new ThongBaoDialog(thongDiep, tieuDe, LoaiThongBao.ThanhCong, "Đã hiểu")
            {
                Owner = winOwner,
                WindowStartupLocation = winOwner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };
            dialog.ShowDialog();
        }

        public static void CanhBao(string thongDiep, string tieuDe = "Cảnh Báo", Window? owner = null)
        {
            var winOwner = TimCuaSoCha(owner);
            var dialog = new ThongBaoDialog(thongDiep, tieuDe, LoaiThongBao.CanhBao, "Đã hiểu")
            {
                Owner = winOwner,
                WindowStartupLocation = winOwner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };
            dialog.ShowDialog();
        }

        public static void ThongTin(string thongDiep, string tieuDe = "Thông Báo", Window? owner = null)
        {
            var winOwner = TimCuaSoCha(owner);
            var dialog = new ThongBaoDialog(thongDiep, tieuDe, LoaiThongBao.ThongTin, "Đã hiểu")
            {
                Owner = winOwner,
                WindowStartupLocation = winOwner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };
            dialog.ShowDialog();
        }

        public static bool XacNhan(
            string thongDiep, 
            string tieuDe = "Xác Nhận Hành Động", 
            string nutDongY = "Đồng ý", 
            string nutHuy = "Hủy bỏ", 
            bool laHanhDongXoa = false, 
            Window? owner = null)
        {
            var winOwner = TimCuaSoCha(owner);
            var dialog = new ThongBaoDialog(thongDiep, tieuDe, LoaiThongBao.XacNhan, nutDongY, nutHuy, laHanhDongXoa)
            {
                Owner = winOwner,
                WindowStartupLocation = winOwner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };
            dialog.ShowDialog();
            return dialog.KetQuaXacNhan;
        }

        private string _maXacNhanCanNhap = "";

        public void YeuCauNhapMaXacNhan(string maCanNhap)
        {
            _maXacNhanCanNhap = maCanNhap?.Trim() ?? "";
            if (!string.IsNullOrEmpty(_maXacNhanCanNhap))
            {
                pnlNhapMaXacNhan.Visibility = Visibility.Visible;
                lblHuongDanNhapMa.Text = $"Vui lòng nhập đúng mã '{_maXacNhanCanNhap}' để mở khóa nút xóa:";
                btnChinh.IsEnabled = false;
                btnChinh.Opacity = 0.5;
                txtMaXacNhan.Focus();
            }
        }

        private void TxtMaXacNhan_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_maXacNhanCanNhap)) return;
            bool khop = txtMaXacNhan.Text.Trim().Equals(_maXacNhanCanNhap, StringComparison.OrdinalIgnoreCase);
            btnChinh.IsEnabled = khop;
            btnChinh.Opacity = khop ? 1.0 : 0.5;
        }

        public static bool XacNhanXoaCoMa(
            string thongDiep,
            string tieuDe,
            string maXacNhan,
            string nutXoa = "Xóa Vĩnh Viễn",
            string nutHuy = "Hủy Bỏ",
            Window? owner = null)
        {
            var winOwner = TimCuaSoCha(owner);
            var dialog = new ThongBaoDialog(thongDiep, tieuDe, LoaiThongBao.XacNhan, nutXoa, nutHuy, laHanhDongXoa: true)
            {
                Owner = winOwner,
                WindowStartupLocation = winOwner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };
            dialog.YeuCauNhapMaXacNhan(maXacNhan);
            dialog.ShowDialog();
            return dialog.KetQuaXacNhan;
        }
    }
}
