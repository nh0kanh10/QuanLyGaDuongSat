using System;
using System.Data;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DTO.Common;

namespace GUI.Views.Dialogs
{
    public partial class TongHopKhuGianGaDialog : Window
    {
        private Action? _onMoPhanHe;

        public TongHopKhuGianGaDialog()
        {
            InitializeComponent();
        }

        public static void HienThi(Window? owner, int maGa, string tenGa, string maCode, decimal lyTrinhKm, DataTable? dtKhuGian, Action? onMoPhanHe = null)
        {
            var dlg = new TongHopKhuGianGaDialog
            {
                _onMoPhanHe = onMoPhanHe
            };

            if (owner != null && owner.IsLoaded && owner.IsVisible)
            {
                dlg.Owner = owner;
                dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dlg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            dlg.NapDuLieu(maGa, tenGa, maCode, lyTrinhKm, dtKhuGian);
            dlg.ShowDialog();
        }

        private void NapDuLieu(int maGa, string tenGa, string maCode, decimal lyTrinhKm, DataTable? dtKhuGian)
        {
            txtGaTrungTam.Text = $"GA {tenGa.Trim().ToUpper()} ({maCode.Trim().ToUpper()})";
            txtMoTaGaTrungTam.Text = $"Lý trình Km {FormatHelper.FormatKm(lyTrinhKm)} — Trục chính Đường Sắt Bắc - Nam";
            txtVisualGaHienTaiTen.Text = $"GA {tenGa.Trim().ToUpper()}";
            txtVisualGaHienTaiKm.Text = $"Km {FormatHelper.FormatKm(lyTrinhKm)}";

            DataRow? rowBac = null;
            DataRow? rowNam = null;

            if (dtKhuGian != null)
            {
                foreach (DataRow r in dtKhuGian.Rows)
                {
                    string huong = r["HuongTuyen"]?.ToString() ?? "";
                    int gaCuoi = r.Table.Columns.Contains("MaGaCuoi") && r["MaGaCuoi"] != DBNull.Value ? Convert.ToInt32(r["MaGaCuoi"]) : 0;
                    int gaDau = r.Table.Columns.Contains("MaGaDau") && r["MaGaDau"] != DBNull.Value ? Convert.ToInt32(r["MaGaDau"]) : 0;

                    if (huong.Contains("Bắc") || gaCuoi == maGa)
                    {
                        rowBac = r;
                    }
                    else if (huong.Contains("Nam") || gaDau == maGa)
                    {
                        rowNam = r;
                    }
                }
            }

            int count = (rowBac != null ? 1 : 0) + (rowNam != null ? 1 : 0);
            txtTongKhuGianBadge.Text = $"{count} KHU GIAN KẾT NỐI";

            // 1. Phía Bắc
            if (rowBac != null)
            {
                string gaDauTen = rowBac["TenGaDau"]?.ToString() ?? "Ga Đi";
                decimal culy = rowBac["CuLyKm"] != DBNull.Value ? Convert.ToDecimal(rowBac["CuLyKm"]) : 0m;
                int vk = rowBac["TocDoToiDaKhach"] != DBNull.Value ? Convert.ToInt32(rowBac["TocDoToiDaKhach"]) : 80;
                int vh = rowBac["TocDoToiDaHang"] != DBNull.Value ? Convert.ToInt32(rowBac["TocDoToiDaHang"]) : 50;
                decimal doc = rowBac["DoDocPermil"] != DBNull.Value ? Convert.ToDecimal(rowBac["DoDocPermil"]) : 0m;
                bool day = rowBac["CanDauMayDay"] != DBNull.Value && Convert.ToBoolean(rowBac["CanDauMayDay"]);
                string status = rowBac["TrangThai"]?.ToString() ?? "RONG";

                int soDnBac = rowBac.Table.Columns.Contains("SoDuongNgang") && rowBac["SoDuongNgang"] != DBNull.Value ? Convert.ToInt32(rowBac["SoDuongNgang"]) : 0;
                int soDdBac = rowBac.Table.Columns.Contains("SoDiemDen") && rowBac["SoDiemDen"] != DBNull.Value ? Convert.ToInt32(rowBac["SoDiemDen"]) : 0;

                txtVisualGaBacTen.Text = $"Ga {gaDauTen}";
                decimal kmBac = Math.Max(0, lyTrinhKm - culy);
                txtVisualGaBacKm.Text = $"Km {FormatHelper.FormatKm(kmBac)}";
                txtVisualCulyBac.Text = $"{FormatHelper.FormatKm(culy)} km";

                txtTenKhuGianBac.Text = $"{gaDauTen} ↔ {tenGa}";
                txtCulyBac.Text = $"{FormatHelper.FormatKm(culy)} km";
                txtVmaxBac.Text = $"{vk} km/h (khách) / {vh} km/h (hàng)";
                txtDoDocBac.Text = $"{doc:0.0} ‰";
                txtMayDayBac.Text = day ? "BẮT BUỘC ĐẦU MÁY ĐẨY" : "Không yêu cầu";
                txtMayDayBac.FontWeight = day ? FontWeights.Bold : FontWeights.Normal;
                txtMayDayBac.Foreground = day ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
                                              : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));

                // Hiển thị thông số đường ngang thực tế nghiệp vụ
                if (soDnBac == 0)
                {
                    txtDuongNgangBac.Text = "Không có giao cắt (An toàn)";
                    txtDuongNgangBac.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                    txtDuongNgangBac.FontWeight = FontWeights.Normal;
                }
                else if (soDdBac == 0)
                {
                    txtDuongNgangBac.Text = $"{soDnBac} vị trí đường ngang (Đạt chuẩn)";
                    txtDuongNgangBac.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
                    txtDuongNgangBac.FontWeight = FontWeights.SemiBold;
                }
                else
                {
                    txtDuongNgangBac.Text = $"{soDnBac} vị trí (⚠️ {soDdBac} điểm đen)";
                    txtDuongNgangBac.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    txtDuongNgangBac.FontWeight = FontWeights.Bold;
                }

                ApDungMauTrangThaiKhuGian(status, rectRayBac, bdRayBacBadge, txtRayBacDot, bdCardKhuGianBac, bdStatusKhuGianBac, txtStatusKhuGianBac);

                txtGhiChuBac.Text = day 
                    ? "Đoạn đèo dốc hiểm trở, yêu cầu kiểm tra kỹ thuật hãm ép và áp lực gió đoàn tàu trước khi xuất phát."
                    : "Đoạn tuyến vận hành ổn định, tốc độ thông qua tàu bình thường.";
            }
            else
            {
                txtVisualGaBacTen.Text = "Điểm Đầu Tuyến";
                txtVisualGaBacKm.Text = "Hà Nội";
                txtVisualCulyBac.Text = "—";
                bdVisualGaBac.Opacity = 0.5;

                txtTenKhuGianBac.Text = "Không có khu gian phía Bắc";
                txtCulyBac.Text = "—";
                txtVmaxBac.Text = "—";
                txtDoDocBac.Text = "—";
                txtMayDayBac.Text = "—";
                txtMayDayBac.Foreground = Brushes.Gray;
                txtDuongNgangBac.Text = "—";
                txtDuongNgangBac.Foreground = Brushes.Gray;

                ApDungTrangThaiKhongKhuGian(rectRayBac, bdRayBacBadge, txtRayBacDot, bdCardKhuGianBac, bdStatusKhuGianBac, txtStatusKhuGianBac);
                txtGhiChuBac.Text = "Ga hiện tại là ga đầu mối phía Bắc của tuyến, không có phân đoạn kết nối chạy tàu lên phía Bắc.";
            }

            // 2. Phía Nam
            if (rowNam != null)
            {
                string gaCuoiTen = rowNam["TenGaCuoi"]?.ToString() ?? "Ga Đến";
                decimal culy = rowNam["CuLyKm"] != DBNull.Value ? Convert.ToDecimal(rowNam["CuLyKm"]) : 0m;
                int vk = rowNam["TocDoToiDaKhach"] != DBNull.Value ? Convert.ToInt32(rowNam["TocDoToiDaKhach"]) : 80;
                int vh = rowNam["TocDoToiDaHang"] != DBNull.Value ? Convert.ToInt32(rowNam["TocDoToiDaHang"]) : 50;
                decimal doc = rowNam["DoDocPermil"] != DBNull.Value ? Convert.ToDecimal(rowNam["DoDocPermil"]) : 0m;
                bool day = rowNam["CanDauMayDay"] != DBNull.Value && Convert.ToBoolean(rowNam["CanDauMayDay"]);
                string status = rowNam["TrangThai"]?.ToString() ?? "RONG";
                int soDnNam = rowNam.Table.Columns.Contains("SoDuongNgang") && rowNam["SoDuongNgang"] != DBNull.Value ? Convert.ToInt32(rowNam["SoDuongNgang"]) : 0;
                int soDdNam = rowNam.Table.Columns.Contains("SoDiemDen") && rowNam["SoDiemDen"] != DBNull.Value ? Convert.ToInt32(rowNam["SoDiemDen"]) : 0;

                txtVisualGaNamTen.Text = $"Ga {gaCuoiTen}";
                decimal kmNam = lyTrinhKm + culy;
                txtVisualGaNamKm.Text = $"Km {FormatHelper.FormatKm(kmNam)}";
                txtVisualCulyNam.Text = $"{FormatHelper.FormatKm(culy)} km";

                txtTenKhuGianNam.Text = $"{tenGa} ↔ {gaCuoiTen}";
                txtCulyNam.Text = $"{FormatHelper.FormatKm(culy)} km";
                txtVmaxNam.Text = $"{vk} km/h (khách) / {vh} km/h (hàng)";
                txtDoDocNam.Text = $"{doc:0.0} ‰";
                txtMayDayNam.Text = day ? "BẮT BUỘC ĐẦU MÁY ĐẨY" : "Không yêu cầu";
                txtMayDayNam.FontWeight = day ? FontWeights.Bold : FontWeights.Normal;
                txtMayDayNam.Foreground = day ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"))
                                              : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));

                // Hiển thị thông số đường ngang thực tế nghiệp vụ
                if (soDnNam == 0)
                {
                    txtDuongNgangNam.Text = "Không có giao cắt (An toàn)";
                    txtDuongNgangNam.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                    txtDuongNgangNam.FontWeight = FontWeights.Normal;
                }
                else if (soDdNam == 0)
                {
                    txtDuongNgangNam.Text = $"{soDnNam} vị trí đường ngang (Đạt chuẩn)";
                    txtDuongNgangNam.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
                    txtDuongNgangNam.FontWeight = FontWeights.SemiBold;
                }
                else
                {
                    txtDuongNgangNam.Text = $"{soDnNam} vị trí (⚠️ {soDdNam} điểm đen)";
                    txtDuongNgangNam.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    txtDuongNgangNam.FontWeight = FontWeights.Bold;
                }

                ApDungMauTrangThaiKhuGian(status, rectRayNam, bdRayNamBadge, txtRayNamDot, bdCardKhuGianNam, bdStatusKhuGianNam, txtStatusKhuGianNam);

                txtGhiChuNam.Text = day 
                    ? "Đoạn đèo dốc hiểm trở, yêu cầu kiểm tra kỹ thuật hãm ép và áp lực gió đoàn tàu trước khi xuất phát."
                    : "Đoạn tuyến vận hành ổn định, tốc độ thông qua tàu bình thường.";
            }
            else
            {
                txtVisualGaNamTen.Text = "Điểm Cuối Tuyến";
                txtVisualGaNamKm.Text = "Sài Gòn";
                txtVisualCulyNam.Text = "—";
                bdVisualGaNam.Opacity = 0.5;

                txtTenKhuGianNam.Text = "Không có khu gian phía Nam";
                txtCulyNam.Text = "—";
                txtVmaxNam.Text = "—";
                txtDoDocNam.Text = "—";
                txtMayDayNam.Text = "—";
                txtMayDayNam.Foreground = Brushes.Gray;
                txtDuongNgangNam.Text = "—";
                txtDuongNgangNam.Foreground = Brushes.Gray;

                ApDungTrangThaiKhongKhuGian(rectRayNam, bdRayNamBadge, txtRayNamDot, bdCardKhuGianNam, bdStatusKhuGianNam, txtStatusKhuGianNam);
                txtGhiChuNam.Text = "Ga hiện tại là ga đầu mối phía Nam của tuyến, không có phân đoạn kết nối chạy tàu về phía Nam.";
            }
        }

        private static void ApDungMauTrangThaiKhuGian(
            string status,
            System.Windows.Shapes.Rectangle rectRay,
            System.Windows.Controls.Border bdRayBadge,
            System.Windows.Controls.TextBlock txtRayDot,
            System.Windows.Controls.Border bdCard,
            System.Windows.Controls.Border bdStatusBadge,
            System.Windows.Controls.TextBlock txtStatusBadge)
        {
            if (status == "CO_TAU")
            {
                // Màu Hổ Phách / Cam Đậm cho trạng thái CÓ TÀU
                var brushRay = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                var brushBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));

                rectRay.Fill = brushRay;
                bdRayBadge.BorderBrush = brushRay;
                txtRayDot.Text = "🚆";
                txtRayDot.FontSize = 8;
                txtRayDot.Foreground = brushRay;

                bdCard.BorderBrush = brushRay;
                bdCard.BorderThickness = new Thickness(3, 1, 1, 1);

                bdStatusBadge.Background = brushRay;
                bdStatusBadge.BorderBrush = brushBorder;
                txtStatusBadge.Text = "ĐANG CÓ TÀU";
                txtStatusBadge.Foreground = Brushes.White;
            }
            else if (status == "PHONG_TOA")
            {
                // Màu Đỏ Cảnh Báo cho trạng thái PHONG TỎA
                var brushRay = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                var brushBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));

                rectRay.Fill = brushRay;
                bdRayBadge.BorderBrush = brushRay;
                txtRayDot.Text = "⛔";
                txtRayDot.FontSize = 8;
                txtRayDot.Foreground = brushRay;

                bdCard.BorderBrush = brushRay;
                bdCard.BorderThickness = new Thickness(3, 1, 1, 1);

                bdStatusBadge.Background = brushRay;
                bdStatusBadge.BorderBrush = brushBorder;
                txtStatusBadge.Text = "PHONG TỎA";
                txtStatusBadge.Foreground = Brushes.White;
            }
            else
            {
                // Màu Xanh Lục Đậm Solid cho trạng thái THÔNG ĐƯỜNG (không dùng pastel)
                var brushRay = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#047857"));
                var brushBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46"));

                rectRay.Fill = brushRay;
                bdRayBadge.BorderBrush = brushRay;
                txtRayDot.Text = "●";
                txtRayDot.FontSize = 7;
                txtRayDot.Foreground = brushRay;

                bdCard.BorderBrush = brushRay;
                bdCard.BorderThickness = new Thickness(3, 1, 1, 1);

                bdStatusBadge.Background = brushRay;
                bdStatusBadge.BorderBrush = brushBorder;
                txtStatusBadge.Text = "THÔNG ĐƯỜNG";
                txtStatusBadge.Foreground = Brushes.White;
            }
        }

        private static void ApDungTrangThaiKhongKhuGian(
            System.Windows.Shapes.Rectangle rectRay,
            System.Windows.Controls.Border bdRayBadge,
            System.Windows.Controls.TextBlock txtRayDot,
            System.Windows.Controls.Border bdCard,
            System.Windows.Controls.Border bdStatusBadge,
            System.Windows.Controls.TextBlock txtStatusBadge)
        {
            var brushMuted = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            var brushTextMuted = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

            rectRay.Fill = brushMuted;
            bdRayBadge.BorderBrush = brushMuted;
            txtRayDot.Text = "—";
            txtRayDot.Foreground = brushTextMuted;

            bdCard.BorderBrush = brushMuted;
            bdCard.BorderThickness = new Thickness(1);

            bdStatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
            bdStatusBadge.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
            txtStatusBadge.Text = "ĐIỂM ĐẦU MỐI";
            txtStatusBadge.Foreground = Brushes.White;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnMoPhanHeKhuGian_Click(object sender, RoutedEventArgs e)
        {
            Close();
            _onMoPhanHe?.Invoke();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        }
    }
}
