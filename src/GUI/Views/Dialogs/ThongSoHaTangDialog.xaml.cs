using System;
using System.Data;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DTO.Common;
using Wpf.Ui.Controls;

namespace GUI.Views.Dialogs
{
    public partial class ThongSoHaTangDialog : Window
    {
        public bool YeuCauChuyenSangQuanLy { get; private set; } = false;
        public string LoaiDoiTuong { get; private set; } = ""; // "RAY" hoặc "KHU_GIAN"
        public int MaDoiTuongId { get; private set; } = 0;
        public int MaGaLienQuan { get; private set; } = 0;

        public ThongSoHaTangDialog()
        {
            InitializeComponent();
        }

        public static bool HienThiDuongRay(Window? owner, DataRow rayRow, string tenGa, string maGaCode, decimal lyTrinhKm, out int maGaChon)
        {
            var dialog = new ThongSoHaTangDialog
            {
                LoaiDoiTuong = "RAY"
            };

            if (owner != null && owner.IsLoaded && owner.IsVisible)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            int maRay = rayRow.Table.Columns.Contains("MaDuongRay") && rayRow["MaDuongRay"] != DBNull.Value ? Convert.ToInt32(rayRow["MaDuongRay"]) : 0;
            int soHieu = rayRow.Table.Columns.Contains("SoHieuDuong") && rayRow["SoHieuDuong"] != DBNull.Value ? Convert.ToInt32(rayRow["SoHieuDuong"]) : 1;
            string loai = rayRow.Table.Columns.Contains("LoaiDuong") ? (rayRow["LoaiDuong"]?.ToString() ?? "CHINH_TUYEN") : "CHINH_TUYEN";
            int chieuDai = rayRow.Table.Columns.Contains("ChieuDaiHuuDungM") && rayRow["ChieuDaiHuuDungM"] != DBNull.Value ? Convert.ToInt32(rayRow["ChieuDaiHuuDungM"]) : 450;
            bool coKeGa = rayRow.Table.Columns.Contains("CoKeGa") && rayRow["CoKeGa"] != DBNull.Value && Convert.ToBoolean(rayRow["CoKeGa"]);
            string trangThai = rayRow.Table.Columns.Contains("TrangThai") ? (rayRow["TrangThai"]?.ToString() ?? "TRONG") : "TRONG";
            int maGa = rayRow.Table.Columns.Contains("MaGa") && rayRow["MaGa"] != DBNull.Value ? Convert.ToInt32(rayRow["MaGa"]) : 0;

            dialog.MaDoiTuongId = maRay;
            dialog.MaGaLienQuan = maGa;
            dialog.txtTitleHeader.Text = $"HỒ SƠ KỸ THUẬT: ĐƯỜNG RAY SỐ {soHieu} — GA {tenGa.ToUpper()}";
            dialog.iconHeader.Symbol = SymbolRegular.Branch24;
            dialog.txtTenDoiTuong.Text = $"Đường Ray Số {soHieu}";
            dialog.txtMoTaPhu.Text = $"Trực thuộc Ga {tenGa} ({maGaCode}) — Lý trình Km {FormatHelper.FormatKm(lyTrinhKm)} Tuyến Bắc - Nam";

            // Phân loại & màu sắc
            string tenLoai = "Đường tránh tàu (Dừng đỗ, tránh vượt tàu trên đường đơn)";
            if (loai == "CHINH_TUYEN") tenLoai = "Đường chính tuyến (Đón, gửi và thông qua tàu tốc độ cao)";
            else if (loai == "BOC_DO") tenLoai = "Đường xếp dỡ hóa vận (Dồn dịch toa xe, bốc dỡ hàng hóa)";
            dialog.txtChiTietPhanLoai.Text = tenLoai;

            // Thông số 2: Chiều dài
            dialog.lblThongSo2.Text = "Chiều dài hữu dụng:";
            dialog.txtThongSo2.Text = $"{chieuDai:N0} mét (sức chứa tiêu chuẩn: ~{chieuDai / 20} toa xe)";

            // Thông số 3: Ke ga
            dialog.lblThongSo3.Text = "Hạ tầng ke ga đón khách:";
            dialog.txtThongSo3.Text = coKeGa 
                ? "Có ke ga cao tiêu chuẩn (+1,100mm) phục vụ hành khách lên xuống tàu an toàn" 
                : "Không bố trí ke ga (Đường tác nghiệp kỹ thuật / dồn dịch toa xe)";

            // Thông số 4: Khổ ray
            dialog.lblThongSo4.Text = "Khổ đường sắt:";
            dialog.txtThongSo4.Text = "1,000 mm (Khổ hẹp tiêu chuẩn mạng lưới Đường Sắt Việt Nam)";

            // Thông số 5: Kết cấu ray
            dialog.lblThongSo5.Text = "Tiêu chuẩn kết cấu ray:";
            dialog.txtThongSo5.Text = loai == "CHINH_TUYEN" 
                ? "Ray hàn liền P50, tà vẹt bê tông dự ứng lực K1, đá dăm ba-lát đầm nén 35cm"
                : "Ray P43 tà vẹt bê tông, ghi cơ khí R=300m góc chéo 1/9";

            // Thông số 6: Phương thức đóng đường
            dialog.lblThongSo6.Text = "Khóa liên khóa ghi:";
            dialog.txtThongSo6.Text = "Hệ thống khóa điện rơle liên khóa tập trung trạm điều độ ga";

            // Thông số 7: Tiêu chuẩn an toàn
            dialog.lblThongSo7.Text = "Quy chuẩn kỹ thuật:";
            dialog.txtThongSo7.Text = "QCVN 08:2018/BGTVT (Quy chuẩn kỹ thuật quốc gia về khai thác đường sắt)";

            // Trạng thái badge
            dialog.bdTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            if (trangThai == "CO_TAU")
            {
                dialog.txtTrangThaiBadge.Text = "ĐANG CÓ TÀU";
                dialog.txtTrangThaiBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                dialog.dotTrangThaiBadge.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                dialog.bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
            }
            else if (trangThai == "BAO_TRI")
            {
                dialog.txtTrangThaiBadge.Text = "ĐANG BẢO TRÌ";
                dialog.txtTrangThaiBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
                dialog.dotTrangThaiBadge.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                dialog.bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
            }
            else
            {
                dialog.txtTrangThaiBadge.Text = "SẴN SÀNG";
                dialog.txtTrangThaiBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                dialog.dotTrangThaiBadge.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
                dialog.bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
            }

            // Khuyến cáo
            if (loai == "CHINH_TUYEN")
            {
                dialog.txtTieuDeKhuyenCao.Text = "QUY ĐỊNH AN TOÀN CHÍNH TUYẾN";
                dialog.txtNoiDungKhuyenCao.Text = "Đường số 1 là đường chính tuyến đón gửi và thông qua tàu. Tuyệt đối cấm dồn dịch toa xe chở hàng nguy hiểm (xăng dầu, axít, vật liệu nổ) đỗ qua đêm trên đường này.";
            }
            else
            {
                dialog.txtTieuDeKhuyenCao.Text = "LƯU Ý TÁC NGHIỆP TRÁNH VƯỢT";
                dialog.txtNoiDungKhuyenCao.Text = $"Đường tránh số {soHieu}. Đoàn tàu dừng đỗ tránh tàu phải nằm gọn hoàn toàn trong mốc giới hạn va quẹt và chiều dài hữu dụng {chieuDai} mét.";
            }

            dialog.ShowDialog();
            maGaChon = dialog.MaGaLienQuan;
            return dialog.YeuCauChuyenSangQuanLy;
        }

        public static bool HienThiKhuGian(Window? owner, DataRow kgRow, out int maGaChon)
        {
            var dialog = new ThongSoHaTangDialog
            {
                LoaiDoiTuong = "KHU_GIAN"
            };

            if (owner != null && owner.IsLoaded && owner.IsVisible)
            {
                dialog.Owner = owner;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            int maKg = kgRow.Table.Columns.Contains("MaKhuGian") && kgRow["MaKhuGian"] != DBNull.Value ? Convert.ToInt32(kgRow["MaKhuGian"]) : 0;
            string gaDau = kgRow["TenGaDau"]?.ToString() ?? "Ga Đi";
            string gaCuoi = kgRow["TenGaCuoi"]?.ToString() ?? "Ga Đến";
            string codeDau = kgRow.Table.Columns.Contains("MaGaDauCode") ? (kgRow["MaGaDauCode"]?.ToString() ?? "") : "";
            string codeCuoi = kgRow.Table.Columns.Contains("MaGaCuoiCode") ? (kgRow["MaGaCuoiCode"]?.ToString() ?? "") : "";
            decimal culy = Convert.ToDecimal(kgRow["CuLyKm"]);
            int vk = Convert.ToInt32(kgRow["TocDoToiDaKhach"]);
            int vh = Convert.ToInt32(kgRow["TocDoToiDaHang"]);
            decimal doc = Convert.ToDecimal(kgRow["DoDocPermil"]);
            bool canDay = Convert.ToBoolean(kgRow["CanDauMayDay"]);
            string trangThai = kgRow.Table.Columns.Contains("TrangThai") ? (kgRow["TrangThai"]?.ToString() ?? "RONG") : "RONG";
            int maGaDau = kgRow.Table.Columns.Contains("MaGaDau") ? Convert.ToInt32(kgRow["MaGaDau"]) : 0;

            dialog.MaDoiTuongId = maKg;
            dialog.MaGaLienQuan = maGaDau;
            dialog.txtTitleHeader.Text = $"HỒ SƠ KỸ THUẬT: KHU GIAN {gaDau.ToUpper()} ↔ {gaCuoi.ToUpper()}";
            dialog.iconHeader.Symbol = SymbolRegular.ArrowSwap24;
            dialog.txtTenDoiTuong.Text = $"Khu Gian: {gaDau} ↔ {gaCuoi}";
            dialog.txtMoTaPhu.Text = $"Phân đoạn kết nối mạng lưới đường sắt đơn Bắc - Nam ({codeDau} — {codeCuoi})";

            dialog.txtChiTietPhanLoai.Text = "Đường đơn tuyến Thống Nhất khổ 1.000mm (1 khu gian — 1 đoàn tàu)";

            dialog.lblThongSo2.Text = "Cự ly phân đoạn:";
            dialog.txtThongSo2.Text = $"{FormatHelper.FormatKm(culy)} km (khoảng cách thực tế giữa tâm 2 ga)";

            dialog.lblThongSo3.Text = "Tốc độ tối đa thiết kế:";
            dialog.txtThongSo3.Text = $"Tàu khách: {vk} km/h  |  Tàu hàng: {vh} km/h";

            dialog.lblThongSo4.Text = "Độ dốc dọc lớn nhất:";
            dialog.txtThongSo4.Text = $"{doc:0.0} ‰ (Phần nghìn)";

            dialog.lblThongSo5.Text = "Yêu cầu máy đẩy đèo:";
            dialog.txtThongSo5.Text = canDay 
                ? "BẮT BUỘC GHÉP ĐẦU MÁY ĐẨY (Banker Engine) tại chân đèo để đảm bảo an toàn kéo nặng vượt dốc."
                : "Không yêu cầu đầu máy đẩy (Địa hình đồng bằng / trung du thông thường)";

            dialog.lblThongSo6.Text = "Phương thức đóng đường:";
            dialog.txtThongSo6.Text = "Đóng đường nửa tự động kết hợp thẻ đường ĐN-2 & máy phát thẻ bài";

            dialog.lblThongSo7.Text = "Cự ly hãm khẩn cấp:";
            dialog.txtThongSo7.Text = "≤ 800 mét ở tốc độ lớn nhất trên dốc quy định";

            // Trạng thái badge
            dialog.bdTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            if (trangThai == "CO_TAU")
            {
                dialog.txtTrangThaiBadge.Text = "ĐANG CÓ TÀU";
                dialog.txtTrangThaiBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                dialog.dotTrangThaiBadge.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                dialog.bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
            }
            else if (trangThai == "PHONG_TOA")
            {
                dialog.txtTrangThaiBadge.Text = "PHONG TỎA THI CÔNG";
                dialog.txtTrangThaiBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
                dialog.dotTrangThaiBadge.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                dialog.bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
            }
            else
            {
                dialog.txtTrangThaiBadge.Text = "THÔNG ĐƯỜNG (RỖNG)";
                dialog.txtTrangThaiBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                dialog.dotTrangThaiBadge.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
                dialog.bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
            }

            // Khuyến cáo an toàn đèo dốc (Option 4: Left-Border Accent Card)
            if (canDay || doc >= 15m)
            {
                dialog.bdKhuyenCao.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
                dialog.bdKhuyenCao.BorderThickness = new Thickness(4, 1, 1, 1);
                dialog.bdKhuyenCao.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                dialog.txtTieuDeKhuyenCao.Text = "CẢNH BÁO ĐỊA HÌNH ĐÈO DỐC HIỂM TRỞ";
                dialog.txtTieuDeKhuyenCao.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                dialog.iconKhuyenCao.Symbol = SymbolRegular.Warning24;
                dialog.iconKhuyenCao.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                dialog.txtNoiDungKhuyenCao.Text = $"Khu đoạn có độ dốc cao {doc:0.0}‰ (như Đèo Hải Vân / Đèo Khe Nét). Trực ban điều độ và lái tàu phải tuyệt đối tuân thủ quy trình thử hãm gió ép áp lực 5.0 bar và ghép đầu máy đẩy phụ trước khi lên đèo.";
                dialog.txtNoiDungKhuyenCao.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
            }
            else
            {
                dialog.txtTieuDeKhuyenCao.Text = "QUY TẮC ĐIỀU ĐỘ KHU GIAN ĐƠN TUYẾN";
                dialog.txtNoiDungKhuyenCao.Text = "Đường sắt đơn một khu gian chỉ cho phép một đoàn tàu chiếm dụng. Nghiêm cấm gửi tàu khi chưa nhận được tín hiệu xin đường và chấp nhận đường từ ga đối diện.";
            }

            dialog.ShowDialog();
            maGaChon = dialog.MaGaLienQuan;
            return dialog.YeuCauChuyenSangQuanLy;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnMoQuanLy_Click(object sender, RoutedEventArgs e)
        {
            YeuCauChuyenSangQuanLy = true;
            Close();
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
