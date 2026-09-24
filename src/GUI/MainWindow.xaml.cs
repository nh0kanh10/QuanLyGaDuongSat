using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using GUI.Views.Pages;
using GUI.Views.Dialogs;
using GUI.Views.KyThuat;

namespace GUI
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly MangLuoiGaPage _mangLuoiGaPage = new();
        private readonly KhuGianPage _khuGianPage = new();
        private readonly LichTrinhPage _lichTrinhPage = new();
        private readonly BanVePage _banVePage = new();
        private readonly HangHoaPage _hangHoaPage = new();

        // Phân hệ Kỹ thuật & Quản trị
        private readonly PhuongTienPage _phuongTienPage = new();

        private string _currentTag = "MangLuoiGa";
        private readonly System.Windows.Threading.DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            NavigateTo("MangLuoiGa");
            
            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => txtClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _timer.Start();
            txtClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void NavTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                NavigateTo(tag);
            }
        }

        public void NavigateTo(string tag, int? focusId = null)
        {
            _currentTag = tag;
            ResetRibbonTabs();

            switch (tag)
            {
                case "MangLuoiGa":
                    MainFrame.Navigate(_mangLuoiGaPage);
                    SetTabActive(tabGa, txtTabGa, iconTabGa);
                    break;

                case "KhuGian":
                    MainFrame.Navigate(_khuGianPage);
                    SetTabActive(tabKhuGian, txtTabKhuGian, iconTabKhuGian);
                    if (focusId.HasValue && focusId.Value > 0)
                    {
                        _khuGianPage.ChonKhuGianTheoId(focusId.Value);
                    }
                    break;

                case "LichTrinh":
                    MainFrame.Navigate(_lichTrinhPage);
                    SetTabActive(tabLichTrinh, txtTabLichTrinh, iconTabLichTrinh);
                    break;

                case "BanVe":
                    MainFrame.Navigate(_banVePage);
                    SetTabActive(tabBanVe, txtTabBanVe, iconTabBanVe);
                    break;

                case "HangHoa":
                    MainFrame.Navigate(_hangHoaPage);
                    SetTabActive(tabHangHoa, txtTabHangHoa, iconTabHangHoa);
                    break;

                case "PhuongTien":
                    MainFrame.Navigate(_phuongTienPage);
                    SetTabActive(tabNavPhuongTien, txtTabPhuongTien, iconTabPhuongTien);
                    break;
            }

            CapNhatThanhPhimTatTheoPhanHe(tag);
        }

        private void CapNhatThanhPhimTatTheoPhanHe(string tag)
        {
            switch (tag)
            {
                case "MangLuoiGa":
                    btnScF2.Visibility = Visibility.Visible;
                    sepScF2.Visibility = Visibility.Visible;
                    txtScF2.Text = "Thêm Ga Mới";
                    btnScF2.ToolTip = "F2: Mở bảng thêm mới Ga đường sắt";

                    btnScF3.Visibility = Visibility.Visible;
                    sepScF3.Visibility = Visibility.Visible;
                    txtScF3.Text = "Tra Cứu Ga";
                    btnScF3.ToolTip = "F3: Focus con trỏ vào ô tìm kiếm ga";

                    btnScF4.Visibility = Visibility.Visible;
                    sepScF4.Visibility = Visibility.Visible;
                    txtScF4.Text = "Nhập Tệp";
                    btnScF4.ToolTip = "F4: Nhập danh sách ga từ Excel/CSV";

                    btnScF5.Visibility = Visibility.Visible;
                    sepScF5.Visibility = Visibility.Visible;
                    txtScF5.Text = "Nạp Lại";
                    btnScF5.ToolTip = "F5: Tải lại dữ liệu ga từ CSDL";

                    btnScF6.Visibility = Visibility.Visible;
                    sepScF6.Visibility = Visibility.Visible;
                    txtScF6.Text = "Xuất Excel";
                    btnScF6.ToolTip = "F6: Xuất danh sách ga ra tệp Excel";

                    btnScF9.Visibility = Visibility.Visible;
                    sepScF9.Visibility = Visibility.Visible;
                    txtScF9.Text = "Báo Cáo Ga (A4)";
                    btnScF9.ToolTip = "F9: Mở trung tâm xem trước và in báo cáo kỹ thuật ga";
                    break;

                case "KhuGian":
                    btnScF2.Visibility = Visibility.Visible;
                    sepScF2.Visibility = Visibility.Visible;
                    txtScF2.Text = "Thêm Khu Gian";
                    btnScF2.ToolTip = "F2: Mở form thêm mới khu gian";

                    btnScF3.Visibility = Visibility.Visible;
                    sepScF3.Visibility = Visibility.Visible;
                    txtScF3.Text = "Tra Cứu Khu Gian";
                    btnScF3.ToolTip = "F3: Focus con trỏ vào ô tìm kiếm khu gian";

                    btnScF4.Visibility = Visibility.Visible;
                    sepScF4.Visibility = Visibility.Visible;
                    txtScF4.Text = "Nhập Tệp";
                    btnScF4.ToolTip = "F4: Nhập danh sách khu gian từ Excel/CSV";

                    btnScF5.Visibility = Visibility.Visible;
                    sepScF5.Visibility = Visibility.Visible;
                    txtScF5.Text = "Nạp Lại";
                    btnScF5.ToolTip = "F5: Tải lại dữ liệu khu gian từ CSDL";

                    btnScF6.Visibility = Visibility.Visible;
                    sepScF6.Visibility = Visibility.Visible;
                    txtScF6.Text = "Xuất Excel";
                    btnScF6.ToolTip = "F6: Xuất danh sách khu gian ra tệp Excel";

                    btnScF9.Visibility = Visibility.Visible;
                    sepScF9.Visibility = Visibility.Visible;
                    txtScF9.Text = "Tổng Hợp Hạ Tầng";
                    btnScF9.ToolTip = "F9: Xem bảng tổng hợp kỹ thuật hạ tầng đường ray các ga";
                    break;

                case "LichTrinh":
                    btnScF2.Visibility = Visibility.Visible;
                    sepScF2.Visibility = Visibility.Visible;
                    txtScF2.Text = "Lập Chuyến Mới";
                    btnScF2.ToolTip = "F2: Lập chuyến tàu mới";

                    btnScF3.Visibility = Visibility.Visible;
                    sepScF3.Visibility = Visibility.Visible;
                    txtScF3.Text = "Chọn Mác Tàu";
                    btnScF3.ToolTip = "F3: Mở danh sách lọc mác tàu";

                    btnScF4.Visibility = Visibility.Collapsed;
                    sepScF4.Visibility = Visibility.Collapsed;

                    btnScF5.Visibility = Visibility.Visible;
                    sepScF5.Visibility = Visibility.Visible;
                    txtScF5.Text = "Nạp Lại";
                    btnScF5.ToolTip = "F5: Tải lại lịch trình chạy tàu";

                    btnScF6.Visibility = Visibility.Collapsed;
                    sepScF6.Visibility = Visibility.Collapsed;

                    btnScF9.Visibility = Visibility.Collapsed;
                    sepScF9.Visibility = Visibility.Collapsed;
                    break;

                case "BanVe":
                    btnScF2.Visibility = Visibility.Visible;
                    sepScF2.Visibility = Visibility.Visible;
                    txtScF2.Text = "Bán Vé Mới";
                    btnScF2.ToolTip = "F2: Mở quy trình bán vé mới cho hành khách";

                    btnScF3.Visibility = Visibility.Visible;
                    sepScF3.Visibility = Visibility.Visible;
                    txtScF3.Text = "Tìm Vé / CCCD";
                    btnScF3.ToolTip = "F3: Focus ô tìm kiếm mã vé hoặc số CCCD hành khách";

                    btnScF4.Visibility = Visibility.Collapsed;
                    sepScF4.Visibility = Visibility.Collapsed;

                    btnScF5.Visibility = Visibility.Visible;
                    sepScF5.Visibility = Visibility.Visible;
                    txtScF5.Text = "Nạp Lại";
                    btnScF5.ToolTip = "F5: Nạp lại danh sách vé đã bán";

                    btnScF6.Visibility = Visibility.Collapsed;
                    sepScF6.Visibility = Visibility.Collapsed;

                    btnScF9.Visibility = Visibility.Collapsed;
                    sepScF9.Visibility = Visibility.Collapsed;
                    break;

                case "HangHoa":
                    btnScF2.Visibility = Visibility.Visible;
                    sepScF2.Visibility = Visibility.Visible;
                    txtScF2.Text = "Lập Vận Đơn Mới";
                    btnScF2.ToolTip = "F2: Mở quy trình lập vận đơn hàng hóa mới";

                    btnScF3.Visibility = Visibility.Visible;
                    sepScF3.Visibility = Visibility.Visible;
                    txtScF3.Text = "Tìm Vận Đơn";
                    btnScF3.ToolTip = "F3: Focus ô tìm kiếm mã vận đơn / chủ hàng";

                    btnScF4.Visibility = Visibility.Collapsed;
                    sepScF4.Visibility = Visibility.Collapsed;

                    btnScF5.Visibility = Visibility.Visible;
                    sepScF5.Visibility = Visibility.Visible;
                    txtScF5.Text = "Nạp Lại";
                    btnScF5.ToolTip = "F5: Tải lại danh sách vận đơn hàng hóa";

                    btnScF6.Visibility = Visibility.Collapsed;
                    sepScF6.Visibility = Visibility.Collapsed;

                    btnScF9.Visibility = Visibility.Collapsed;
                    sepScF9.Visibility = Visibility.Collapsed;
                    break;

                case "PhuongTien":
                    btnScF2.Visibility = Visibility.Collapsed;
                    sepScF2.Visibility = Visibility.Collapsed;

                    btnScF3.Visibility = Visibility.Visible;
                    sepScF3.Visibility = Visibility.Visible;
                    txtScF3.Text = "Tra Cứu Đầu Máy";
                    btnScF3.ToolTip = "F3: Focus ô tìm kiếm số hiệu đầu máy / dòng máy / xí nghiệp";

                    btnScF4.Visibility = Visibility.Collapsed;
                    sepScF4.Visibility = Visibility.Collapsed;

                    btnScF5.Visibility = Visibility.Visible;
                    sepScF5.Visibility = Visibility.Visible;
                    txtScF5.Text = "Nạp Lại";
                    btnScF5.ToolTip = "F5: Nạp lại toàn bộ dữ liệu phương tiện từ CSDL";

                    btnScF6.Visibility = Visibility.Collapsed;
                    sepScF6.Visibility = Visibility.Collapsed;

                    btnScF9.Visibility = Visibility.Collapsed;
                    sepScF9.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void ResetRibbonTabs()
        {
            var inactiveBg = (Brush)new BrushConverter().ConvertFrom("#F1F5F9")!;
            var inactiveBorder = (Brush)new BrushConverter().ConvertFrom("#CBD5E1")!;
            var inactiveFg = (Brush)new BrushConverter().ConvertFrom("#334155")!;
            var inactiveIconFg = (Brush)new BrushConverter().ConvertFrom("#475569")!;

            tabGa.Background = inactiveBg;
            tabGa.BorderBrush = inactiveBorder;
            txtTabGa.Foreground = inactiveFg;
            iconTabGa.Foreground = inactiveIconFg;

            tabKhuGian.Background = inactiveBg;
            tabKhuGian.BorderBrush = inactiveBorder;
            txtTabKhuGian.Foreground = inactiveFg;
            iconTabKhuGian.Foreground = inactiveIconFg;

            tabLichTrinh.Background = inactiveBg;
            tabLichTrinh.BorderBrush = inactiveBorder;
            txtTabLichTrinh.Foreground = inactiveFg;
            iconTabLichTrinh.Foreground = inactiveIconFg;

            tabBanVe.Background = inactiveBg;
            tabBanVe.BorderBrush = inactiveBorder;
            txtTabBanVe.Foreground = inactiveFg;
            iconTabBanVe.Foreground = inactiveIconFg;

            tabHangHoa.Background = inactiveBg;
            tabHangHoa.BorderBrush = inactiveBorder;
            txtTabHangHoa.Foreground = inactiveFg;
            iconTabHangHoa.Foreground = inactiveIconFg;

            tabNavPhuongTien.Background = inactiveBg;
            tabNavPhuongTien.BorderBrush = inactiveBorder;
            txtTabPhuongTien.Foreground = inactiveFg;
            iconTabPhuongTien.Foreground = inactiveIconFg;
        }

        private void SetTabActive(Border tab, TextBlock text, Wpf.Ui.Controls.SymbolIcon icon)
        {
            tab.Background = (Brush)new BrushConverter().ConvertFrom("#003B73")!;
            tab.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#002855")!;
            text.Foreground = Brushes.White;
            icon.Foreground = Brushes.White;
        }

        // Xử lý menu
        private void MenuGa_Click(object sender, RoutedEventArgs e) => NavigateTo("MangLuoiGa");
        private void MenuKhuGian_Click(object sender, RoutedEventArgs e) => NavigateTo("KhuGian");
        private void MenuLichTrinh_Click(object sender, RoutedEventArgs e) => NavigateTo("LichTrinh");
        private void MenuBanVe_Click(object sender, RoutedEventArgs e) => NavigateTo("BanVe");
        private void MenuHangHoa_Click(object sender, RoutedEventArgs e) => NavigateTo("HangHoa");

        private void MenuKetNoi_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chuỗi kết nối CSDL hiện tại:\nServer=.\\SQLEXPRESS; Database=QuanLyDuongSatV2; Trusted_Connection=True;", "Thông tin kết nối", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuThoat_Click(object sender, RoutedEventArgs e)
        {
            var r = MessageBox.Show("Bạn có muốn thoát khỏi hệ thống điều hành?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) Application.Current.Shutdown();
        }

        private void MenuBaoCao_Click(object sender, RoutedEventArgs e) => ThucHienBaoCao();
        private void MenuTroGiup_Click(object sender, RoutedEventArgs e) => ThucHienTroGiup();

        // Xử lý phím tắt bàn phím
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    e.Handled = true;
                    ThucHienTroGiup();
                    break;
                case Key.F2:
                    if (btnScF2.Visibility != Visibility.Visible) return;
                    e.Handled = true;
                    ThucHienThemMoi();
                    break;
                case Key.F3:
                    if (btnScF3.Visibility != Visibility.Visible) return;
                    e.Handled = true;
                    ThucHienTraCuu();
                    break;
                case Key.F4:
                    if (btnScF4.Visibility != Visibility.Visible) return;
                    e.Handled = true;
                    ThucHienNhapTep();
                    break;
                case Key.F5:
                    if (btnScF5.Visibility != Visibility.Visible) return;
                    e.Handled = true;
                    ThucHienNapLai();
                    break;
                case Key.F6:
                    if (btnScF6.Visibility != Visibility.Visible) return;
                    e.Handled = true;
                    ThucHienXuatTep();
                    break;
                case Key.F9:
                    if (btnScF9.Visibility != Visibility.Visible) return;
                    e.Handled = true;
                    ThucHienBaoCao();
                    break;
                case Key.F11:
                    e.Handled = true;
                    ThucHienToanManHinh();
                    break;
                case Key.Escape:
                    ThucHienHuy();
                    break;
            }
        }

        private void BtnShortcut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                switch (tag)
                {
                    case "F1": ThucHienTroGiup(); break;
                    case "F2": if (btnScF2.Visibility == Visibility.Visible) ThucHienThemMoi(); break;
                    case "F3": if (btnScF3.Visibility == Visibility.Visible) ThucHienTraCuu(); break;
                    case "F4": if (btnScF4.Visibility == Visibility.Visible) ThucHienNhapTep(); break;
                    case "F5": if (btnScF5.Visibility == Visibility.Visible) ThucHienNapLai(); break;
                    case "F6": if (btnScF6.Visibility == Visibility.Visible) ThucHienXuatTep(); break;
                    case "F9": if (btnScF9.Visibility == Visibility.Visible) ThucHienBaoCao(); break;
                    case "F11": ThucHienToanManHinh(); break;
                    case "ESC": ThucHienHuy(); break;
                }
            }
        }

        // Xử lý các tác vụ theo phân hệ
        private void ThucHienTraCuu()
        {
            switch (_currentTag)
            {
                case "MangLuoiGa":
                    _mangLuoiGaPage.FocusTimKiem();
                    break;
                case "KhuGian":
                    _khuGianPage.FocusTimKiem();
                    break;
                case "LichTrinh":
                    _lichTrinhPage.FocusTimKiem();
                    break;
                case "BanVe":
                    _banVePage.FocusTimKiem();
                    break;
                case "HangHoa":
                    _hangHoaPage.FocusTimKiem();
                    break;
                case "PhuongTien":
                    _phuongTienPage.FocusTimKiem();
                    break;
            }
        }

        private void ThucHienThemMoi()
        {
            switch (_currentTag)
            {
                case "MangLuoiGa":
                    _mangLuoiGaPage.KichHoatThemMoi();
                    break;
                case "KhuGian":
                    _khuGianPage.KichHoatThemMoi();
                    break;
                case "LichTrinh":
                    _lichTrinhPage.KichHoatThemMoi();
                    break;
                case "BanVe":
                    _banVePage.KichHoatThemMoi();
                    break;
                case "HangHoa":
                    _hangHoaPage.KichHoatThemMoi();
                    break;
            }
        }

        private void ThucHienNapLai()
        {
            switch (_currentTag)
            {
                case "MangLuoiGa":
                    _mangLuoiGaPage.KichHoatNapLai();
                    break;
                case "KhuGian":
                    _khuGianPage.KichHoatNapLai();
                    break;
                case "LichTrinh":
                    _lichTrinhPage.KichHoatNapLai();
                    break;
                case "BanVe":
                    _banVePage.KichHoatNapLai();
                    break;
                case "HangHoa":
                    _hangHoaPage.KichHoatNapLai();
                    break;
                case "PhuongTien":
                    _phuongTienPage.KichHoatNapLai();
                    break;
            }
        }

        private void ThucHienNhapTep()
        {
            switch (_currentTag)
            {
                case "MangLuoiGa":
                    _mangLuoiGaPage.KichHoatNhapTep();
                    break;
                case "KhuGian":
                    _khuGianPage.KichHoatNhapTep();
                    break;
            }
        }

        private void ThucHienXuatTep()
        {
            switch (_currentTag)
            {
                case "MangLuoiGa":
                    _mangLuoiGaPage.KichHoatXuatTep();
                    break;
                case "KhuGian":
                    _khuGianPage.KichHoatXuatTep();
                    break;
            }
        }

        private void ThucHienBaoCao()
        {
            switch (_currentTag)
            {
                case "MangLuoiGa":
                    _mangLuoiGaPage.KichHoatBaoCao();
                    break;
                case "KhuGian":
                    _khuGianPage.KichHoatBaoCao();
                    break;
            }
        }

        private void ThucHienToanManHinh()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void ThucHienHuy()
        {
            switch (_currentTag)
            {
                case "MangLuoiGa":
                    _mangLuoiGaPage.KichHoatHuy();
                    break;
                case "KhuGian":
                    _khuGianPage.KichHoatHuy();
                    break;
                case "PhuongTien":
                    _phuongTienPage.KichHoatHuy();
                    break;
            }
        }

        private void ThucHienTroGiup()
        {
            var dialog = new SoTayPhimTatDialog(_currentTag)
            {
                Owner = this
            };
            dialog.ShowDialog();
        }
    }
}