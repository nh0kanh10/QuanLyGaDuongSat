using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DTO.Common;
using QRCoder;

namespace GUI.Views.Dialogs
{
    public partial class PhieuGuiHangDialog : Window
    {
        public PhieuGuiHangDialog()
        {
            InitializeComponent();
        }

        public PhieuGuiHangDialog(DataRowView row) : this()
        {
            if (row != null)
            {
                NapThongTinVanDon(row.Row);
            }
        }

        public PhieuGuiHangDialog(DataRow row) : this()
        {
            if (row != null)
            {
                NapThongTinVanDon(row);
            }
        }

        public void NapThongTinVanDon(DataRow row)
        {
            string code = row["MaVanDonCode"]?.ToString() ?? "VD_000000";
            txtMaVanDonCode.Text = code;

            // Ga gửi & Ga nhận
            string gaGui = row.Table.Columns.Contains("TenGaGui") && row["TenGaGui"] != DBNull.Value ? row["TenGaGui"].ToString()! : "Ga Gửi";
            string gaNhan = row.Table.Columns.Contains("TenGaNhan") && row["TenGaNhan"] != DBNull.Value ? row["TenGaNhan"].ToString()! : "Ga Nhận";
            txtTenGaGui.Text = gaGui.ToUpper();
            txtTenGaNhan.Text = gaNhan.ToUpper();

            // Cự ly
            if (row.Table.Columns.Contains("CuLyKm") && row["CuLyKm"] != DBNull.Value)
            {
                decimal cuLy = Convert.ToDecimal(row["CuLyKm"]);
                txtCuLyKm.Text = $"{cuLy:N0} Km";
            }
            else
            {
                txtCuLyKm.Text = "Toàn tuyến";
            }

            // Thời điểm tạo / gửi
            DateTime thoiDiem = row.Table.Columns.Contains("ThoiDiemTao") && row["ThoiDiemTao"] != DBNull.Value
                ? Convert.ToDateTime(row["ThoiDiemTao"])
                : ThoiGianHeThongHelper.LayGioHienTai();
            txtThoiDiemGui.Text = $"Thời điểm lập: {thoiDiem:dd/MM/yyyy HH:mm}";

            // Người gửi
            txtTenNguoiGui.Text = row.Table.Columns.Contains("TenNguoiGui") ? row["TenNguoiGui"]?.ToString() ?? "--" : "--";
            txtSdtNguoiGui.Text = row.Table.Columns.Contains("SDTNguoiGui") ? row["SDTNguoiGui"]?.ToString() ?? "--" : "--";
            txtDiaChiNguoiGui.Text = row.Table.Columns.Contains("DiaChiNguoiGui") && !string.IsNullOrWhiteSpace(row["DiaChiNguoiGui"]?.ToString())
                ? row["DiaChiNguoiGui"].ToString()!
                : $"Khu vực ga {gaGui}";

            // Người nhận
            txtTenNguoiNhan.Text = row.Table.Columns.Contains("TenNguoiNhan") ? row["TenNguoiNhan"]?.ToString() ?? "--" : "--";
            txtSdtNguoiNhan.Text = row.Table.Columns.Contains("SDTNguoiNhan") ? row["SDTNguoiNhan"]?.ToString() ?? "--" : "--";
            txtDiaChiNguoiNhan.Text = row.Table.Columns.Contains("DiaChiNguoiNhan") && !string.IsNullOrWhiteSpace(row["DiaChiNguoiNhan"]?.ToString())
                ? row["DiaChiNguoiNhan"].ToString()!
                : $"Khu vực ga {gaNhan}";

            // Hàng hóa & trọng lượng
            string tenHang = row.Table.Columns.Contains("TenHangHoa") && row["TenHangHoa"] != DBNull.Value ? row["TenHangHoa"].ToString()! : "Hàng hóa tổng hợp";
            txtTenHangHoa.Text = tenHang;

            decimal trongLuong = row.Table.Columns.Contains("TrongLuongTan") && row["TrongLuongTan"] != DBNull.Value
                ? Convert.ToDecimal(row["TrongLuongTan"])
                : 0m;
            txtTrongLuong.Text = $"{trongLuong:N2} tấn";

            // Nhóm cước & đơn giá
            if (row.Table.Columns.Contains("NhomCuoc") && row["NhomCuoc"] != DBNull.Value)
            {
                txtNhomCuoc.Text = $"Nhóm {row["NhomCuoc"]} (Theo biểu cước VNR)";
            }
            if (row.Table.Columns.Contains("DonGiaMoiTanKm") && row["DonGiaMoiTanKm"] != DBNull.Value)
            {
                decimal donGia = Convert.ToDecimal(row["DonGiaMoiTanKm"]);
                txtDonGiaTanKm.Text = $"{donGia:N0} đ/tấn.km";
            }

            // Hình thức & đặc thù
            string loaiVC = row.Table.Columns.Contains("LoaiVanChuyen") && row["LoaiVanChuyen"] != DBNull.Value ? row["LoaiVanChuyen"].ToString()! : "TIÊU CHUẨN";
            string bienKS = row.Table.Columns.Contains("BienKiemSoat") && row["BienKiemSoat"] != DBNull.Value ? row["BienKiemSoat"].ToString()! : "";
            string cont = row.Table.Columns.Contains("SoHieuContainer") && row["SoHieuContainer"] != DBNull.Value ? row["SoHieuContainer"].ToString()! : "";
            string chiHQ = row.Table.Columns.Contains("SoChiHaiQuan") && row["SoChiHaiQuan"] != DBNull.Value ? row["SoChiHaiQuan"].ToString()! : "";
            bool rutXang = row.Table.Columns.Contains("DaRutXang") && row["DaRutXang"] != DBNull.Value && Convert.ToBoolean(row["DaRutXang"]);

            txtHinhThucVC.Text = $"Hình thức: {loaiVC}";

            if (!string.IsNullOrEmpty(cont))
            {
                txtThongTinDacThu.Text = $"Số Container: {cont}" + (!string.IsNullOrEmpty(chiHQ) ? $" | Chì niêm phong: {chiHQ}" : "");
            }
            else if (!string.IsNullOrEmpty(bienKS))
            {
                txtThongTinDacThu.Text = $"Biển kiểm soát: {bienKS}" + (rutXang ? " (Đã rút xăng an toàn pccc)" : "");
            }
            else
            {
                txtThongTinDacThu.Text = "Vận chuyển toa hàng chuyên dụng (Pallet đóng kiện)";
            }

            // Ghi chú
            string ghiChu = row.Table.Columns.Contains("GhiChu") && row["GhiChu"] != DBNull.Value ? row["GhiChu"].ToString()! : "";
            txtGhiChu.Text = !string.IsNullOrWhiteSpace(ghiChu) ? ghiChu : "Bốc xếp cẩn thận, bảo quản khô ráo, che chắn bạt chuyên dụng trong suốt hành trình.";

            // Cước phí
            decimal cuoc = row.Table.Columns.Contains("CuocPhi") && row["CuocPhi"] != DBNull.Value ? Convert.ToDecimal(row["CuocPhi"]) : 0m;
            txtCuocVanChuyen.Text = $"{cuoc:N0} VNĐ";
            txtTongCuoc.Text = $"{cuoc:N0} VNĐ";
            txtBangChu.Text = $"(Viết bằng chữ: {ChuyenSoThanhChu((long)cuoc)})";

            // Trạng thái vận đơn
            string trangThai = row.Table.Columns.Contains("TrangThai") && row["TrangThai"] != DBNull.Value ? row["TrangThai"].ToString()! : "CHO_TIEP_NHAN";
            DinhDangTrangThai(trangThai);

            // Sinh mã QR tra cứu vận đơn
            TaoMaQrChoVanDon(code, gaGui, gaNhan, cuoc);
        }

        private void DinhDangTrangThai(string trangThai)
        {
            switch (trangThai)
            {
                case "DA_GIAO":
                    txtTrangThai.Text = "ĐÃ GIAO HÀNG (HOÀN TẤT)";
                    txtTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                    bdTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
                    bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                    break;

                case "DANG_VAN_CHUYEN":
                case "DANG_CHAY":
                    txtTrangThai.Text = "ĐANG VẬN CHUYỂN";
                    txtTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
                    bdTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
                    bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7DD3FC"));
                    break;

                case "DA_DEN":
                case "DA_DEN_GA":
                    txtTrangThai.Text = "ĐÃ ĐẾN GA ĐÍCH (CHỜ TRẢ HÀNG)";
                    txtTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C3AED"));
                    bdTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3E8FF"));
                    bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D8B4FE"));
                    break;

                case "CHO_XEP":
                case "CHO_TIEP_NHAN":
                case "DA_TIEP_NHAN":
                    txtTrangThai.Text = "ĐÃ TIẾP NHẬN (CHỜ XẾP TOA)";
                    txtTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                    bdTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                    bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));
                    break;

                case "DA_HUY":
                    txtTrangThai.Text = "ĐÃ HỦY VẬN ĐƠN";
                    txtTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    bdTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                    bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
                    break;

                default:
                    txtTrangThai.Text = trangThai;
                    txtTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                    bdTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    bdTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                    break;
            }
        }

        private void TaoMaQrChoVanDon(string maCode, string gaGui, string gaNhan, decimal cuoc)
        {
            try
            {
                string payload = $"https://dsvn.vn/track?code={maCode}&from={Uri.EscapeDataString(gaGui)}&to={Uri.EscapeDataString(gaNhan)}&fee={cuoc:F0}";
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(10);

                var bitmap = new BitmapImage();
                using (var ms = new MemoryStream(qrBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                }
                bitmap.Freeze();
                imgQrCode.Source = bitmap;
            }
            catch
            {
                // Phòng vệ không crash nếu lỗi QR
            }
        }

        #region HÀM ĐỔI SỐ THÀNH CHỮ TIẾNG VIỆT
        private static readonly string[] ChuSo = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        private static readonly string[] Tien = { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };

        public static string ChuyenSoThanhChu(long soTien)
        {
            if (soTien == 0) return "Không đồng";
            if (soTien < 0) return "Âm " + ChuyenSoThanhChu(-soTien);

            string chuoi = "";
            int vitri = 0;
            long n = soTien;

            while (n > 0)
            {
                int nhom = (int)(n % 1000);
                if (nhom > 0)
                {
                    string chuoiNhom = DocNhomBaChuSo(nhom, n >= 1000);
                    chuoi = $"{chuoiNhom} {Tien[vitri]} {chuoi}".Trim();
                }
                vitri++;
                n /= 1000;
            }

            chuoi = chuoi.Trim();
            if (string.IsNullOrEmpty(chuoi)) return "Không đồng";

            chuoi = char.ToUpper(chuoi[0]) + chuoi[1..] + " đồng chẵn";
            return chuoi;
        }

        private static string DocNhomBaChuSo(int so, bool dayDu)
        {
            int tram = so / 100;
            int chuc = (so % 100) / 10;
            int donVi = so % 10;
            string ketQua = "";

            if (tram > 0 || dayDu)
            {
                ketQua += $"{ChuSo[tram]} trăm ";
            }

            if (chuc > 1)
            {
                ketQua += $"{ChuSo[chuc]} mươi ";
                if (donVi == 1) ketQua += "mốt";
                else if (donVi == 5) ketQua += "lăm";
                else if (donVi > 0) ketQua += ChuSo[donVi];
            }
            else if (chuc == 1)
            {
                ketQua += "mười ";
                if (donVi == 5) ketQua += "lăm";
                else if (donVi > 0) ketQua += ChuSo[donVi];
            }
            else if (donVi > 0)
            {
                if (tram > 0 || dayDu) ketQua += "lẻ ";
                ketQua += ChuSo[donVi];
            }

            return ketQua.Trim();
        }
        #endregion

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.F8)
            {
                BtnInPhieu_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void BtnInPhieu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    printDlg.PrintVisual(bdInPhieu, $"PhieuGuiHang_{txtMaVanDonCode.Text}");
                    ThongBaoDialog.ThanhCong($"Lệnh in phiếu vận đơn #{txtMaVanDonCode.Text} đã gửi đến máy in thành công!", "In Hoàn Tất");
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể thực hiện in phiếu: {ex.Message}", "Lỗi In Ấn");
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
