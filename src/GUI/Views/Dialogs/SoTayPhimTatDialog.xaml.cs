using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GUI.Views.Dialogs
{
    public class PhimTatItemViewModel
    {
        public string Phim { get; set; } = string.Empty;
        public string TenTacNghiep { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public string MauKeycapHex { get; set; } = "#003B73";
        public string TagNhan { get; set; } = "Tác Nghiệp";

        public Brush MauKeycapBrush => (Brush)new BrushConverter().ConvertFrom(MauKeycapHex)!;
        public Brush MauKeycapBorderBrush => (Brush)new BrushConverter().ConvertFrom(LamToiMau(MauKeycapHex))!;

        private static string LamToiMau(string hex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                byte r = (byte)(color.R * 0.72);
                byte g = (byte)(color.G * 0.72);
                byte b = (byte)(color.B * 0.72);
                return $"#{r:X2}{g:X2}{b:X2}";
            }
            catch
            {
                return "#000000";
            }
        }
    }

    public partial class SoTayPhimTatDialog : Window
    {
        private readonly string _initialModuleTag;
        private string _activeViewTag;

        public SoTayPhimTatDialog(string currentModuleTag = "MangLuoiGa")
        {
            InitializeComponent();
            _initialModuleTag = string.IsNullOrEmpty(currentModuleTag) ? "MangLuoiGa" : currentModuleTag;
            _activeViewTag = _initialModuleTag;

            CapNhatBadgePhanHeHienTai();
            KichHoatTabMacDinh(_initialModuleTag);
            NapPhimTatToanCuc();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void CapNhatBadgePhanHeHienTai()
        {
            string tenPhanHe = LayTenPhanHe(_initialModuleTag);
            txtCurrentModuleBadge.Text = $"Đang tác nghiệp: {tenPhanHe}";
        }

        private void KichHoatTabMacDinh(string tag)
        {
            Button targetBtn = tag switch
            {
                "MangLuoiGa" => btnTabGa,
                "KhuGian" => btnTabKhuGian,
                "LichTrinh" => btnTabLichTrinh,
                "BanVe" => btnTabBanVe,
                "HangHoa" => btnTabHangHoa,
                _ => btnTabGa
            };

            CapNhatGiaoDienTabs(targetBtn);
            HienThiPhanHe(tag);
        }

        private string LayTenPhanHe(string tag)
        {
            return tag switch
            {
                "MangLuoiGa" => "Mạng Lưới Ga",
                "KhuGian" => "Khu Gian Đường Sắt",
                "LichTrinh" => "Lịch Trình Chạy Tàu",
                "BanVe" => "Hành Khách & Bán Vé",
                "HangHoa" => "Vận Tải Hàng Hóa",
                "ALL" => "Toàn Bộ Hệ Thống",
                _ => "Hệ Thống"
            };
        }

        private void TabFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _activeViewTag = tag;
                CapNhatGiaoDienTabs(btn);
                HienThiPhanHe(tag);
            }
        }

        private void CapNhatGiaoDienTabs(Button activeBtn)
        {
            var inactiveBg = (Brush)new BrushConverter().ConvertFrom("#E2E8F0")!;
            var inactiveFg = (Brush)new BrushConverter().ConvertFrom("#334155")!;
            var inactiveIconFg = (Brush)new BrushConverter().ConvertFrom("#475569")!;
            var activeBg = (Brush)new BrushConverter().ConvertFrom("#003B73")!;

            Button[] tabs = { btnTabGa, btnTabKhuGian, btnTabLichTrinh, btnTabBanVe, btnTabHangHoa, btnTabTatCa };
            TextBlock[] labels = { txtTabGa, txtTabKhuGian, txtTabLichTrinh, txtTabBanVe, txtTabHangHoa, txtTabTatCa };
            Wpf.Ui.Controls.SymbolIcon[] icons = { icoTabGa, icoTabKhuGian, icoTabLichTrinh, icoTabBanVe, icoTabHangHoa, icoTabTatCa };

            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == activeBtn)
                {
                    tabs[i].Background = activeBg;
                    labels[i].Foreground = Brushes.White;
                    labels[i].FontWeight = FontWeights.Bold;
                    icons[i].Foreground = Brushes.White;
                }
                else
                {
                    tabs[i].Background = inactiveBg;
                    labels[i].Foreground = inactiveFg;
                    labels[i].FontWeight = FontWeights.SemiBold;
                    icons[i].Foreground = inactiveIconFg;
                }
            }
        }

        private void HienThiPhanHe(string tag)
        {
            string tenPhanHe = LayTenPhanHe(tag);
            txtSectionTitle.Text = tag == "ALL" 
                ? "DANH MỤC TOÀN BỘ PHÍM TẮT PHÂN HỆ TÁC NGHIỆP" 
                : $"PHÍM TẮT ĐẶC THÙ PHÂN HỆ — {tenPhanHe.ToUpper()}";

            var items = new List<PhimTatItemViewModel>();

            if (tag == "ALL")
            {
                items.AddRange(LayDanhSachGa());
                items.AddRange(LayDanhSachKhuGian());
                items.AddRange(LayDanhSachLichTrinh());
                items.AddRange(LayDanhSachBanVe());
                items.AddRange(LayDanhSachHangHoa());
            }
            else
            {
                switch (tag)
                {
                    case "MangLuoiGa":
                        items = LayDanhSachGa();
                        break;
                    case "KhuGian":
                        items = LayDanhSachKhuGian();
                        break;
                    case "LichTrinh":
                        items = LayDanhSachLichTrinh();
                        break;
                    case "BanVe":
                        items = LayDanhSachBanVe();
                        break;
                    case "HangHoa":
                        items = LayDanhSachHangHoa();
                        break;
                }
            }

            icPhanHeShortcuts.ItemsSource = items;
        }

        private List<PhimTatItemViewModel> LayDanhSachGa()
        {
            return new List<PhimTatItemViewModel>
            {
                new()
                {
                    Phim = "F2",
                    TenTacNghiep = "Thêm Ga Mới",
                    MoTa = "Khai báo thông tin ga đường sắt mới, thiết lập tỉnh thành, phân cấp ga (Hạng I, II, III) và lý trình tuyến Bắc - Nam.",
                    MauKeycapHex = "#15803D",
                    TagNhan = "Mạng Lưới Ga"
                },
                new()
                {
                    Phim = "F3",
                    TenTacNghiep = "Tra Cứu Ga Nhanh",
                    MoTa = "Đặt con trỏ vào ô tìm kiếm, lọc tức thì theo tên ga, mã code hoặc phân cấp ga.",
                    MauKeycapHex = "#D97706",
                    TagNhan = "Mạng Lưới Ga"
                },
                new()
                {
                    Phim = "F4",
                    TenTacNghiep = "Nhập Danh Sách Từ Tệp",
                    MoTa = "Mở trình nhập dữ liệu danh sách ga hàng loạt từ bảng tính Excel (.xlsx) hoặc CSV.",
                    MauKeycapHex = "#6366F1",
                    TagNhan = "Mạng Lưới Ga"
                },
                new()
                {
                    Phim = "F5",
                    TenTacNghiep = "Nạp Lại Dữ Liệu Ga",
                    MoTa = "Tải lại toàn bộ dữ liệu sạch từ CSDL SQL Server, đồng bộ hóa trạng thái hạ tầng ga mới nhất.",
                    MauKeycapHex = "#059669",
                    TagNhan = "Mạng Lưới Ga"
                },
                new()
                {
                    Phim = "F6",
                    TenTacNghiep = "Xuất Danh Sách Excel",
                    MoTa = "Xuất dữ liệu toàn bộ bảng mạng lưới ga ra bảng tính Excel để lưu trữ hoặc gửi báo cáo.",
                    MauKeycapHex = "#0D9488",
                    TagNhan = "Mạng Lưới Ga"
                },
                new()
                {
                    Phim = "F9",
                    TenTacNghiep = "Báo Cáo Kỹ Thuật Ga (A4)",
                    MoTa = "Mở Trung tâm Lập, Xem trước & In Báo Cáo Kỹ Thuật Hạ Tầng Ga theo tiêu chuẩn khổ in A4 Tổng Công Ty.",
                    MauKeycapHex = "#0284C7",
                    TagNhan = "Mạng Lưới Ga"
                },
                new()
                {
                    Phim = "Esc",
                    TenTacNghiep = "Bỏ Chọn / Xóa Tìm Kiếm",
                    MoTa = "Bỏ chọn hàng ga đang chọn trong bảng hoặc xóa trắng nội dung ô tra cứu.",
                    MauKeycapHex = "#DC2626",
                    TagNhan = "Mạng Lưới Ga"
                }
            };
        }

        private List<PhimTatItemViewModel> LayDanhSachKhuGian()
        {
            return new List<PhimTatItemViewModel>
            {
                new()
                {
                    Phim = "F2",
                    TenTacNghiep = "Thêm Khu Gian Mới",
                    MoTa = "Khai báo khu gian kết nối giữa Ga đi - Ga đến, chiều dài lý trình, tốc độ cho phép và số đường ray song song.",
                    MauKeycapHex = "#15803D",
                    TagNhan = "Khu Gian"
                },
                new()
                {
                    Phim = "F3",
                    TenTacNghiep = "Tra Cứu Khu Gian",
                    MoTa = "Focus con trỏ vào ô tìm kiếm nhanh khu gian theo ga đi, ga đến hoặc chiều dài tuyến.",
                    MauKeycapHex = "#D97706",
                    TagNhan = "Khu Gian"
                },
                new()
                {
                    Phim = "F4",
                    TenTacNghiep = "Nhập Khu Gian Từ Tệp",
                    MoTa = "Nhập danh sách khu gian hàng loạt từ tệp Excel/CSV với kiểm tra ràng buộc logic tự động.",
                    MauKeycapHex = "#6366F1",
                    TagNhan = "Khu Gian"
                },
                new()
                {
                    Phim = "F5",
                    TenTacNghiep = "Nạp Lại Dữ Liệu Khu Gian",
                    MoTa = "Làm mới toàn bộ danh mục khu gian trực tiếp từ CSDL.",
                    MauKeycapHex = "#059669",
                    TagNhan = "Khu Gian"
                },
                new()
                {
                    Phim = "F6",
                    TenTacNghiep = "Xuất Dữ Liệu Excel",
                    MoTa = "Xuất bảng thống kê danh mục khu gian ra bảng tính Excel.",
                    MauKeycapHex = "#0D9488",
                    TagNhan = "Khu Gian"
                },
                new()
                {
                    Phim = "F9",
                    TenTacNghiep = "Báo Cáo Tổng Hợp Hạ Tầng",
                    MoTa = "Mở bảng tổng hợp hạ tầng kỹ thuật đường ray các ga và khu gian kết nối liên hoàn.",
                    MauKeycapHex = "#0284C7",
                    TagNhan = "Khu Gian"
                },
                new()
                {
                    Phim = "Esc",
                    TenTacNghiep = "Hủy / Bỏ Chọn",
                    MoTa = "Hủy thao tác đang sửa hoặc bỏ chọn khu gian trong bảng.",
                    MauKeycapHex = "#DC2626",
                    TagNhan = "Khu Gian"
                }
            };
        }

        private List<PhimTatItemViewModel> LayDanhSachLichTrinh()
        {
            return new List<PhimTatItemViewModel>
            {
                new()
                {
                    Phim = "F2",
                    TenTacNghiep = "Lập Chuyến Tàu Mới",
                    MoTa = "Khởi tạo chuyến tàu mới trên biểu đồ chạy tàu, nhập mác tàu, ngày xuất phát và giờ hành trình dự kiến.",
                    MauKeycapHex = "#15803D",
                    TagNhan = "Lịch Trình"
                },
                new()
                {
                    Phim = "F3",
                    TenTacNghiep = "Chọn & Lọc Mác Tàu",
                    MoTa = "Mở nhanh danh sách thả xuống chọn mác tàu (SE1, SE3, SE4, HBN...) để lọc lịch trình tương ứng.",
                    MauKeycapHex = "#D97706",
                    TagNhan = "Lịch Trình"
                },
                new()
                {
                    Phim = "F5",
                    TenTacNghiep = "Nạp Lại Biểu Đồ Chạy Tàu",
                    MoTa = "Cập nhật dữ liệu giờ chạy tàu thực tế và kế hoạch chuyến từ CSDL điều độ.",
                    MauKeycapHex = "#059669",
                    TagNhan = "Lịch Trình"
                },
                new()
                {
                    Phim = "Esc",
                    TenTacNghiep = "Đóng / Bỏ Chọn",
                    MoTa = "Bỏ chọn hành trình hoặc đặt lại bộ lọc mác tàu về 'Tất cả'.",
                    MauKeycapHex = "#DC2626",
                    TagNhan = "Lịch Trình"
                }
            };
        }

        private List<PhimTatItemViewModel> LayDanhSachBanVe()
        {
            return new List<PhimTatItemViewModel>
            {
                new()
                {
                    Phim = "F2",
                    TenTacNghiep = "Bán Vé Mới",
                    MoTa = "Mở form bán vé mới cho hành khách, chọn mác tàu, toa, chỗ ngồi và áp dụng chính sách giảm giá.",
                    MauKeycapHex = "#15803D",
                    TagNhan = "Bán Vé"
                },
                new()
                {
                    Phim = "F3",
                    TenTacNghiep = "Tìm Vé / Số CCCD",
                    MoTa = "Focus con trỏ vào ô tìm kiếm để tra cứu nhanh vé đã phát hành qua mã vé, tên hành khách hoặc số CCCD.",
                    MauKeycapHex = "#D97706",
                    TagNhan = "Bán Vé"
                },
                new()
                {
                    Phim = "F5",
                    TenTacNghiep = "Nạp Lại Danh Sách Vé",
                    MoTa = "Tải lại danh sách vé đã đặt và đã thanh toán từ hệ thống.",
                    MauKeycapHex = "#059669",
                    TagNhan = "Bán Vé"
                },
                new()
                {
                    Phim = "Esc",
                    TenTacNghiep = "Hủy Form / Bỏ Chọn",
                    MoTa = "Đóng cửa sổ bán vé hoặc hủy bỏ vé đang chọn.",
                    MauKeycapHex = "#DC2626",
                    TagNhan = "Bán Vé"
                }
            };
        }

        private List<PhimTatItemViewModel> LayDanhSachHangHoa()
        {
            return new List<PhimTatItemViewModel>
            {
                new()
                {
                    Phim = "F2",
                    TenTacNghiep = "Lập Vận Đơn Mới",
                    MoTa = "Mở cửa sổ lập vận đơn gửi hàng mới, khai báo chủ hàng, ga gửi, ga nhận, trọng lượng và tính cước.",
                    MauKeycapHex = "#15803D",
                    TagNhan = "Hàng Hóa"
                },
                new()
                {
                    Phim = "F3",
                    TenTacNghiep = "Tìm Kiếm Vận Đơn",
                    MoTa = "Focus ô tìm kiếm để tra cứu nhanh vận đơn theo mã đơn, tên chủ hàng hoặc toa xe xếp hàng.",
                    MauKeycapHex = "#D97706",
                    TagNhan = "Hàng Hóa"
                },
                new()
                {
                    Phim = "F5",
                    TenTacNghiep = "Nạp Lại Danh Sách Vận Đơn",
                    MoTa = "Cập nhật lại danh sách vận đơn và trạng thái xếp dỡ hàng hóa từ CSDL.",
                    MauKeycapHex = "#059669",
                    TagNhan = "Hàng Hóa"
                },
                new()
                {
                    Phim = "Esc",
                    TenTacNghiep = "Hủy / Đóng Form",
                    MoTa = "Hủy thao tác soạn vận đơn hoặc bỏ chọn bản ghi trong bảng.",
                    MauKeycapHex = "#DC2626",
                    TagNhan = "Hàng Hóa"
                }
            };
        }

        private void NapPhimTatToanCuc()
        {
            var toanCucItems = new List<PhimTatItemViewModel>
            {
                new()
                {
                    Phim = "F1",
                    TenTacNghiep = "Mở Sổ Tay Phím Tắt",
                    MoTa = "Mở bảng tra cứu phím tắt tác nghiệp này tại bất kỳ màn hình nào trong hệ thống.",
                    MauKeycapHex = "#003B73",
                    TagNhan = "Toàn Hệ Thống"
                },
                new()
                {
                    Phim = "F11",
                    TenTacNghiep = "Toàn Màn Hình (Full Screen)",
                    MoTa = "Chuyển đổi linh hoạt giữa chế độ phóng to toàn màn hình và kích thước cửa sổ chuẩn.",
                    MauKeycapHex = "#475569",
                    TagNhan = "Toàn Hệ Thống"
                },
                new()
                {
                    Phim = "Esc",
                    TenTacNghiep = "Hủy Thao Tác / Bỏ Chọn / Đóng Bảng",
                    MoTa = "Bỏ chọn hàng đang chọn, xóa nhanh nội dung tìm kiếm hoặc đóng cửa sổ pop-up hiện hành.",
                    MauKeycapHex = "#DC2626",
                    TagNhan = "Toàn Hệ Thống"
                }
            };

            icToanCucShortcuts.ItemsSource = toanCucItems;
        }
    }
}
