using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using GUI.Views.Pages;
using GUI.Views.Dialogs;

namespace GUI
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly MangLuoiGaPage _mangLuoiGaPage = new();
        private readonly KhuGianPage _khuGianPage = new();
        private readonly LichTrinhPage _lichTrinhPage = new();
        private readonly BieuDoChayTauPage _bieuDoPage = new();
        private readonly BanVePage _banVePage = new();
        private readonly SoatVePage _soatVePage = new();
        private readonly HangHoaPage _hangHoaPage = new();

        // Phân hệ Kỹ thuật
        private readonly PhuongTienPage _phuongTienPage = new();
        private readonly BaoTriPage _baoTriPage = new();
        private readonly NhanSuPage _nhanSuPage = new();

        private string _currentTag = "MangLuoiGa";
        private readonly System.Windows.Threading.DispatcherTimer _timer;

        private static readonly Dictionary<string, string> TagToClusterMap = new()
        {
            { "MangLuoiGa", "MasterData" },
            { "KhuGian", "MasterData" },
            { "NhanSu", "MasterData" },
            { "LichTrinh", "VanHanh" },
            { "BieuDo", "VanHanh" },
            { "BanVe", "KinhDoanh" },
            { "SoatVe", "KinhDoanh" },
            { "HangHoa", "KinhDoanh" },
            { "PhuongTien", "KyThuat" },
            { "BaoTri", "KyThuat" }
        };

        private readonly Dictionary<string, string> _lastActiveTabPerCluster = new()
        {
            { "MasterData", "MangLuoiGa" },
            { "VanHanh", "LichTrinh" },
            { "KinhDoanh", "BanVe" },
            { "KyThuat", "PhuongTien" }
        };

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

        private void NavCluster_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string clusterKey)
            {
                if (_lastActiveTabPerCluster.TryGetValue(clusterKey, out var lastSubTag))
                {
                    NavigateTo(lastSubTag);
                }
                else
                {
                    NavigateTo(GetDefaultTabForCluster(clusterKey));
                }
            }
        }

        private static string GetDefaultTabForCluster(string clusterKey) => clusterKey switch
        {
            "MasterData" => "MangLuoiGa",
            "VanHanh" => "LichTrinh",
            "KinhDoanh" => "BanVe",
            "KyThuat" => "PhuongTien",
            _ => "MangLuoiGa"
        };

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
            var clusterKey = TagToClusterMap.TryGetValue(tag, out var ck) ? ck : "MasterData";
            _lastActiveTabPerCluster[clusterKey] = tag;

            CapNhatTrangThaiRibbon(clusterKey, tag);

            switch (tag)
            {
                case "MangLuoiGa":
                    MainFrame.Navigate(_mangLuoiGaPage);
                    break;

                case "KhuGian":
                    MainFrame.Navigate(_khuGianPage);
                    if (focusId.HasValue && focusId.Value > 0)
                    {
                        _khuGianPage.ChonKhuGianTheoId(focusId.Value);
                    }
                    break;

                case "LichTrinh":
                    MainFrame.Navigate(_lichTrinhPage);
                    break;

                case "BieuDo":
                    MainFrame.Navigate(_bieuDoPage);
                    _bieuDoPage.LoadData();
                    break;

                case "BanVe":
                    MainFrame.Navigate(_banVePage);
                    break;

                case "SoatVe":
                    MainFrame.Navigate(_soatVePage);
                    break;

                case "HangHoa":
                    MainFrame.Navigate(_hangHoaPage);
                    break;

                case "PhuongTien":
                    MainFrame.Navigate(_phuongTienPage);
                    break;

                case "BaoTri":
                    MainFrame.Navigate(_baoTriPage);
                    break;

                case "NhanSu":
                    MainFrame.Navigate(_nhanSuPage);
                    break;
            }

            CapNhatThanhPhimTatTheoPhanHe(tag);
        }

        private void CapNhatThanhPhimTatTheoPhanHe(string tag)
        {
            btnScF7.Visibility = Visibility.Collapsed;
            sepScF7.Visibility = Visibility.Collapsed;

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

                case "BieuDo":
                    btnScF2.Visibility = Visibility.Collapsed;
                    sepScF2.Visibility = Visibility.Collapsed;

                    btnScF3.Visibility = Visibility.Visible;
                    sepScF3.Visibility = Visibility.Visible;
                    txtScF3.Text = "Kiểm Tra";
                    btnScF3.ToolTip = "F3: Quét kiểm tra xung đột an toàn toàn tuyến";

                    btnScF4.Visibility = Visibility.Collapsed;
                    sepScF4.Visibility = Visibility.Collapsed;

                    btnScF5.Visibility = Visibility.Visible;
                    sepScF5.Visibility = Visibility.Visible;
                    txtScF5.Text = "Nạp Lại";
                    btnScF5.ToolTip = "F5: Tải lại dữ liệu biểu đồ chạy tàu";

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

                    btnScF6.Visibility = Visibility.Visible;
                    sepScF6.Visibility = Visibility.Visible;
                    txtScF6.Text = "Xuất Excel";
                    btnScF6.ToolTip = "F6: Xuất danh sách vé ra tệp Excel (.xlsx)";

                    btnScF7.Visibility = Visibility.Visible;
                    sepScF7.Visibility = Visibility.Visible;

                    btnScF9.Visibility = Visibility.Visible;
                    sepScF9.Visibility = Visibility.Visible;
                    txtScF9.Text = "Báo Cáo";
                    btnScF9.ToolTip = "F9: Mở tổng hợp & báo cáo doanh thu bán vé";
                    break;

                case "SoatVe":
                    btnScF2.Visibility = Visibility.Visible;
                    sepScF2.Visibility = Visibility.Visible;
                    txtScF2.Text = "Quét Mã";
                    btnScF2.ToolTip = "F2: Focus vào ô quét mã vé / CCCD";

                    btnScF3.Visibility = Visibility.Visible;
                    sepScF3.Visibility = Visibility.Visible;
                    txtScF3.Text = "Lọc Khách";
                    btnScF3.ToolTip = "F3: Focus ô tìm kiếm trong danh sách khách (Manifest)";

                    btnScF4.Visibility = Visibility.Collapsed;
                    sepScF4.Visibility = Visibility.Collapsed;

                    btnScF5.Visibility = Visibility.Visible;
                    sepScF5.Visibility = Visibility.Visible;
                    txtScF5.Text = "Nạp Lại";
                    btnScF5.ToolTip = "F5: Tải lại dữ liệu chuyến tàu và danh sách khách";

                    btnScF6.Visibility = Visibility.Visible;
                    sepScF6.Visibility = Visibility.Visible;
                    txtScF6.Text = "Xuất Manifest";
                    btnScF6.ToolTip = "F6: Xuất bảng danh sách hành khách ra file CSV";

                    btnScF7.Visibility = Visibility.Collapsed;
                    sepScF7.Visibility = Visibility.Collapsed;

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

                case "BaoTri":
                    btnScF2.Visibility = Visibility.Visible;
                    sepScF2.Visibility = Visibility.Visible;
                    txtScF2.Text = "Lập Mới";
                    btnScF2.ToolTip = "F2: Lập biên bản / phiếu mới theo tab đang mở (khám xe, nhiên liệu, bảo dưỡng)";

                    btnScF3.Visibility = Visibility.Visible;
                    sepScF3.Visibility = Visibility.Visible;
                    txtScF3.Text = "Tra Cứu";
                    btnScF3.ToolTip = "F3: Focus ô tìm kiếm của tab đang mở";

                    btnScF4.Visibility = Visibility.Collapsed;
                    sepScF4.Visibility = Visibility.Collapsed;

                    btnScF5.Visibility = Visibility.Visible;
                    sepScF5.Visibility = Visibility.Visible;
                    txtScF5.Text = "Nạp Lại";
                    btnScF5.ToolTip = "F5: Nạp lại dữ liệu bảo trì kỹ thuật";

                    btnScF6.Visibility = Visibility.Collapsed;
                    sepScF6.Visibility = Visibility.Collapsed;

                    btnScF9.Visibility = Visibility.Collapsed;
                    sepScF9.Visibility = Visibility.Collapsed;
                    break;

                case "NhanSu":
                    btnScF2.Visibility = Visibility.Visible;
                    sepScF2.Visibility = Visibility.Visible;
                    txtScF2.Text = "Thêm Mới";
                    btnScF2.ToolTip = "F2: Thêm theo tab đang mở (hồ sơ nhân viên, kíp lái, kết quả kiểm tra lên ban)";

                    btnScF3.Visibility = Visibility.Visible;
                    sepScF3.Visibility = Visibility.Visible;
                    txtScF3.Text = "Tra Cứu";
                    btnScF3.ToolTip = "F3: Focus ô tìm kiếm của tab đang mở";

                    btnScF4.Visibility = Visibility.Collapsed;
                    sepScF4.Visibility = Visibility.Collapsed;

                    btnScF5.Visibility = Visibility.Visible;
                    sepScF5.Visibility = Visibility.Visible;
                    txtScF5.Text = "Nạp Lại";
                    btnScF5.ToolTip = "F5: Nạp lại và đối chiếu lại điều kiện kíp lái theo giờ hiện tại";

                    btnScF6.Visibility = Visibility.Collapsed;
                    sepScF6.Visibility = Visibility.Collapsed;

                    btnScF9.Visibility = Visibility.Collapsed;
                    sepScF9.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void CapNhatTrangThaiRibbon(string clusterKey, string activeSubTag)
        {
            // 1. Tầng 1: Cập nhật trạng thái hiển thị 4 Cụm Phân Hệ Lớn
            DatTrangThaiCluster(clusterMasterData, iconClusterMasterData, txtClusterMasterData, indicatorMasterData, clusterKey == "MasterData");
            DatTrangThaiCluster(clusterVanHanh, iconClusterVanHanh, txtClusterVanHanh, indicatorVanHanh, clusterKey == "VanHanh");
            DatTrangThaiCluster(clusterKinhDoanh, iconClusterKinhDoanh, txtClusterKinhDoanh, indicatorKinhDoanh, clusterKey == "KinhDoanh");
            DatTrangThaiCluster(clusterKyThuat, iconClusterKyThuat, txtClusterKyThuat, indicatorKyThuat, clusterKey == "KyThuat");

            // 2. Tầng 2: Hiện / Ẩn Sub-bar tương ứng theo Cụm
            subBarMasterData.Visibility = clusterKey == "MasterData" ? Visibility.Visible : Visibility.Collapsed;
            subBarVanHanh.Visibility = clusterKey == "VanHanh" ? Visibility.Visible : Visibility.Collapsed;
            subBarKinhDoanh.Visibility = clusterKey == "KinhDoanh" ? Visibility.Visible : Visibility.Collapsed;
            subBarKyThuat.Visibility = clusterKey == "KyThuat" ? Visibility.Visible : Visibility.Collapsed;

            // 3. Tầng 2: Reset và Highlight Tab Tác Nghiệp Con đang được chọn
            ResetRibbonTabs();
            switch (activeSubTag)
            {
                case "MangLuoiGa": SetTabActive(tabGa, txtTabGa, iconTabGa); break;
                case "KhuGian": SetTabActive(tabKhuGian, txtTabKhuGian, iconTabKhuGian); break;
                case "NhanSu": SetTabActive(tabNavNhanSu, txtTabNhanSu, iconTabNhanSu); break;
                case "LichTrinh": SetTabActive(tabLichTrinh, txtTabLichTrinh, iconTabLichTrinh); break;
                case "BieuDo": SetTabActive(tabBieuDo, txtTabBieuDo, iconTabBieuDo); break;
                case "BanVe": SetTabActive(tabBanVe, txtTabBanVe, iconTabBanVe); break;
                case "SoatVe": SetTabActive(tabSoatVe, txtTabSoatVe, iconTabSoatVe); break;
                case "HangHoa": SetTabActive(tabHangHoa, txtTabHangHoa, iconTabHangHoa); break;
                case "PhuongTien": SetTabActive(tabNavPhuongTien, txtTabPhuongTien, iconTabPhuongTien); break;
                case "BaoTri": SetTabActive(tabNavBaoTri, txtTabBaoTri, iconTabBaoTri); break;
            }
        }

        private static void DatTrangThaiCluster(Border clusterBorder, Wpf.Ui.Controls.SymbolIcon icon, TextBlock text, System.Windows.Shapes.Rectangle indicator, bool isActive)
        {
            if (isActive)
            {
                clusterBorder.Background = (Brush)new BrushConverter().ConvertFrom("#003B73")!;
                icon.Foreground = (Brush)new BrushConverter().ConvertFrom("#38BDF8")!;
                text.Foreground = Brushes.White;
                indicator.Fill = (Brush)new BrushConverter().ConvertFrom("#38BDF8")!;
            }
            else
            {
                clusterBorder.Background = Brushes.Transparent;
                icon.Foreground = (Brush)new BrushConverter().ConvertFrom("#94A3B8")!;
                text.Foreground = (Brush)new BrushConverter().ConvertFrom("#94A3B8")!;
                indicator.Fill = Brushes.Transparent;
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

            tabBieuDo.Background = inactiveBg;
            tabBieuDo.BorderBrush = inactiveBorder;
            txtTabBieuDo.Foreground = inactiveFg;
            iconTabBieuDo.Foreground = inactiveIconFg;

            tabBanVe.Background = inactiveBg;
            tabBanVe.BorderBrush = inactiveBorder;
            txtTabBanVe.Foreground = inactiveFg;
            iconTabBanVe.Foreground = inactiveIconFg;

            tabSoatVe.Background = inactiveBg;
            tabSoatVe.BorderBrush = inactiveBorder;
            txtTabSoatVe.Foreground = inactiveFg;
            iconTabSoatVe.Foreground = inactiveIconFg;

            tabHangHoa.Background = inactiveBg;
            tabHangHoa.BorderBrush = inactiveBorder;
            txtTabHangHoa.Foreground = inactiveFg;
            iconTabHangHoa.Foreground = inactiveIconFg;

            tabNavPhuongTien.Background = inactiveBg;
            tabNavPhuongTien.BorderBrush = inactiveBorder;
            txtTabPhuongTien.Foreground = inactiveFg;
            iconTabPhuongTien.Foreground = inactiveIconFg;

            tabNavBaoTri.Background = inactiveBg;
            tabNavBaoTri.BorderBrush = inactiveBorder;
            txtTabBaoTri.Foreground = inactiveFg;
            iconTabBaoTri.Foreground = inactiveIconFg;

            tabNavNhanSu.Background = inactiveBg;
            tabNavNhanSu.BorderBrush = inactiveBorder;
            txtTabNhanSu.Foreground = inactiveFg;
            iconTabNhanSu.Foreground = inactiveIconFg;
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
        private void MenuNhanSu_Click(object sender, RoutedEventArgs e) => NavigateTo("NhanSu");
        private void MenuLichTrinh_Click(object sender, RoutedEventArgs e) => NavigateTo("LichTrinh");
        private void MenuBieuDo_Click(object sender, RoutedEventArgs e) => NavigateTo("BieuDo");
        private void MenuBanVe_Click(object sender, RoutedEventArgs e) => NavigateTo("BanVe");
        private void MenuHangHoa_Click(object sender, RoutedEventArgs e) => NavigateTo("HangHoa");
        private void MenuPhuongTien_Click(object sender, RoutedEventArgs e) => NavigateTo("PhuongTien");
        private void MenuBaoTri_Click(object sender, RoutedEventArgs e) => NavigateTo("BaoTri");


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
                case Key.F7:
                    if (btnScF7.Visibility != Visibility.Visible) return;
                    e.Handled = true;
                    ThucHienSoatVe();
                    break;
                case Key.F8:
                    e.Handled = true;
                    NavigateTo("BieuDo");
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
                    case "F7": if (btnScF7.Visibility == Visibility.Visible) ThucHienSoatVe(); break;
                    case "F8": NavigateTo("BieuDo"); break;
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
                case "SoatVe":
                    _soatVePage.FocusTimKiem();
                    break;
                case "HangHoa":
                    _hangHoaPage.FocusTimKiem();
                    break;
                case "PhuongTien":
                    _phuongTienPage.FocusTimKiem();
                    break;
                case "BaoTri":
                    _baoTriPage.FocusTimKiem();
                    break;
                case "NhanSu":
                    _nhanSuPage.FocusTimKiem();
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
                case "SoatVe":
                    _soatVePage.FocusScanner();
                    break;
                case "HangHoa":
                    _hangHoaPage.KichHoatThemMoi();
                    break;
                case "BaoTri":
                    _baoTriPage.KichHoatThemMoi();
                    break;
                case "NhanSu":
                    _nhanSuPage.KichHoatThemMoi();
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
                case "SoatVe":
                    _soatVePage.KichHoatNapLai();
                    break;
                case "HangHoa":
                    _hangHoaPage.KichHoatNapLai();
                    break;
                case "BieuDo":
                    _bieuDoPage.LoadData();
                    break;
                case "PhuongTien":
                    _phuongTienPage.KichHoatNapLai();
                    break;
                case "BaoTri":
                    _baoTriPage.KichHoatNapLai();
                    break;
                case "NhanSu":
                    _nhanSuPage.KichHoatNapLai();
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
                case "BanVe":
                    _banVePage.KichHoatXuatTep();
                    break;
                case "SoatVe":
                    _soatVePage.KichHoatXuatTep();
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
                case "BanVe":
                    _banVePage.KichHoatBaoCao();
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
                case "BaoTri":
                    _baoTriPage.KichHoatHuy();
                    break;
                case "NhanSu":
                    _nhanSuPage.KichHoatHuy();
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

        private void ThucHienSoatVe()
        {
            NavigateTo("SoatVe");
            _soatVePage.FocusScanner();
        }

        private void MenuSoatVe_Click(object sender, RoutedEventArgs e)
        {
            ThucHienSoatVe();
        }
    }
}