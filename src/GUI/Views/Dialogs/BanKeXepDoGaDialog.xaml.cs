using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BUS.Services;
using DTO.Common;

namespace GUI.Views.Dialogs
{
    public partial class BanKeXepDoGaDialog : Window
    {
        private readonly VanDonHangService _vdService = new();
        private readonly GaService _gaService = new();
        private DataTable? _dtGa;
        private DataTable? _cachedVanDon;
        private bool _isInitializing = true;

        public class DongBanKeItem
        {
            public int STT { get; set; }
            public int MaVanDon { get; set; }
            public string MaVanDonCode { get; set; } = "";
            public string ThongTinTauGio { get; set; } = "";
            public string TenGaGui { get; set; } = "";
            public string TenGaNhan { get; set; } = "";
            public string MatHangVaDacThu { get; set; } = "";
            public string TrongLuongHienThi { get; set; } = "";
            public decimal TrongLuongTan { get; set; }
            public string NguoiNhanVaSdt { get; set; } = "";
            public string NguoiGuiVaSdt { get; set; } = "";
            public string ViTriKhoBai { get; set; } = "Bãi dồn / Kho";
            public string ToaXeDuKien { get; set; } = "Toa G-VNR";
            public string OChecklistKiemDem { get; set; } = "[   ]";
        }

        public BanKeXepDoGaDialog(int? maGaKhoiTao = null, DateTime? ngayKhoiTao = null)
        {
            InitializeComponent();
            Loaded += (s, e) => KhoiTaoDuLieu(maGaKhoiTao, ngayKhoiTao);
        }

        private void KhoiTaoDuLieu(int? maGaKhoiTao, DateTime? ngayKhoiTao)
        {
            try
            {
                _isInitializing = true;

                // 1. Tải danh mục ga
                _dtGa = _gaService.LayDanhSach();
                if (_dtGa != null && _dtGa.Rows.Count > 0)
                {
                    cboGaTacNghiep.ItemsSource = _dtGa.DefaultView;

                    // Mặc định chọn ga truyền vào, nếu không thì lấy Ga Giáp Bát (ga hàng hóa trọng điểm) hoặc ga đầu tiên
                    int selectedIndex = 0;
                    if (maGaKhoiTao.HasValue && maGaKhoiTao.Value > 0)
                    {
                        for (int i = 0; i < _dtGa.Rows.Count; i++)
                        {
                            if (Convert.ToInt32(_dtGa.Rows[i]["MaGa"]) == maGaKhoiTao.Value)
                            {
                                selectedIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Tìm ga Giáp Bát hoặc Hà Nội
                        for (int i = 0; i < _dtGa.Rows.Count; i++)
                        {
                            string tenGa = _dtGa.Rows[i]["TenGa"]?.ToString() ?? "";
                            if (tenGa.Contains("Giáp Bát", StringComparison.OrdinalIgnoreCase))
                            {
                                selectedIndex = i;
                                break;
                            }
                        }
                    }

                    cboGaTacNghiep.SelectedIndex = selectedIndex;
                }

                // 2. Thiết lập ngày tác nghiệp (theo giờ hệ thống / mô phỏng)
                DateTime ngay = ngayKhoiTao ?? ThoiGianHeThongHelper.LayGioHienTai();
                dpNgayTacNghiep.SelectedDate = ngay.Date;
                txtNgayInHienTai.Text = $"Ngày tác nghiệp: {ngay:dd/MM/yyyy}";

                // 3. Tải danh sách vận đơn và nạp vào bảng
                _cachedVanDon = _vdService.LayDanhSach();

                _isInitializing = false;
                NapDuLieuBanKe();
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                ThongBaoDialog.Loi($"Không thể khởi tạo bản kê tác nghiệp: {ex.Message}", "Lỗi Tải Dữ Liệu");
            }
        }

        private void NapDuLieuBanKe()
        {
            if (_isInitializing || cboGaTacNghiep.SelectedValue == null) return;

            int maGa = Convert.ToInt32(cboGaTacNghiep.SelectedValue);
            string tenGaChon = "";
            if (cboGaTacNghiep.SelectedItem is DataRowView drv)
            {
                tenGaChon = drv["TenGa"]?.ToString() ?? "";
            }

            txtTieuDeGaHienTai.Text = $"TRỰC BAN BÃI HÀNG GA: {tenGaChon.ToUpper()}";

            DateTime ngayTacNghiep = dpNgayTacNghiep.SelectedDate ?? ThoiGianHeThongHelper.LayGioHienTai().Date;
            txtNgayInHienTai.Text = $"Ngày tác nghiệp: {ngayTacNghiep:dd/MM/yyyy}";

            if (_cachedVanDon == null)
            {
                _cachedVanDon = _vdService.LayDanhSach();
            }

            var dsDoXuong = new List<DongBanKeItem>();
            var dsXepLen = new List<DongBanKeItem>();

            int sttDo = 1;
            int sttXep = 1;
            decimal tongTanDo = 0m;
            decimal tongTanXep = 0m;

            foreach (DataRow row in _cachedVanDon.Rows)
            {
                string trangThai = row["TrangThai"]?.ToString() ?? "";
                if (trangThai == "DA_HUY") continue;

                int gaGui = row["MaGaGui"] != DBNull.Value ? Convert.ToInt32(row["MaGaGui"]) : 0;
                int gaNhan = row["MaGaNhan"] != DBNull.Value ? Convert.ToInt32(row["MaGaNhan"]) : 0;

                string code = row["MaVanDonCode"]?.ToString() ?? "";
                string tenGaGui = row.Table.Columns.Contains("TenGaGui") ? row["TenGaGui"]?.ToString() ?? "" : "";
                string tenGaNhan = row.Table.Columns.Contains("TenGaNhan") ? row["TenGaNhan"]?.ToString() ?? "" : "";
                string tenHang = row.Table.Columns.Contains("TenHangHoa") ? row["TenHangHoa"]?.ToString() ?? "Hàng hóa" : "Hàng hóa";
                string dacThu = row.Table.Columns.Contains("ThongTinDacThu") ? row["ThongTinDacThu"]?.ToString() ?? "" : "";
                decimal trongLuong = row["TrongLuongTan"] != DBNull.Value ? Convert.ToDecimal(row["TrongLuongTan"]) : 0m;

                string matHangVaDacThu = string.IsNullOrWhiteSpace(dacThu) || dacThu == "—" 
                    ? tenHang 
                    : $"{tenHang} [{dacThu}]";

                string nguoiNhan = row["TenNguoiNhan"]?.ToString() ?? "";
                string sdtNhan = row["SDTNguoiNhan"]?.ToString() ?? "";
                string nguoiNhanVaSdt = string.IsNullOrEmpty(sdtNhan) ? nguoiNhan : $"{nguoiNhan} ({sdtNhan})";

                string nguoiGui = row["TenNguoiGui"]?.ToString() ?? "";
                string sdtGui = row["SDTNguoiGui"]?.ToString() ?? "";
                string nguoiGuiVaSdt = string.IsNullOrEmpty(sdtGui) ? nguoiGui : $"{nguoiGui} ({sdtGui})";

                // Mapping mác tàu vận chuyển chuyên tuyến (HBN1 cho chiều Bắc -> Nam, HBN2 cho Nam -> Bắc)
                string macTau = gaGui < gaNhan ? "HBN1" : "HBN2";

                // 1. Kiểm tra hàng dỡ xuống tại ga này (Ga nhận trùng Ga chọn)
                if (gaNhan == maGa)
                {
                    dsDoXuong.Add(new DongBanKeItem
                    {
                        STT = sttDo++,
                        MaVanDon = Convert.ToInt32(row["MaVanDon"]),
                        MaVanDonCode = code,
                        ThongTinTauGio = $"{macTau} · Đến 14:30",
                        TenGaGui = tenGaGui,
                        TenGaNhan = tenGaNhan,
                        MatHangVaDacThu = matHangVaDacThu,
                        TrongLuongTan = trongLuong,
                        TrongLuongHienThi = $"{trongLuong:N2} tấn",
                        NguoiNhanVaSdt = nguoiNhanVaSdt,
                        NguoiGuiVaSdt = nguoiGuiVaSdt,
                        ViTriKhoBai = "Kho bãi nhận",
                        OChecklistKiemDem = "[   ]"
                    });
                    tongTanDo += trongLuong;
                }

                // 2. Kiểm tra hàng xếp lên tại ga này (Ga gửi trùng Ga chọn)
                if (gaGui == maGa)
                {
                    dsXepLen.Add(new DongBanKeItem
                    {
                        STT = sttXep++,
                        MaVanDon = Convert.ToInt32(row["MaVanDon"]),
                        MaVanDonCode = code,
                        ThongTinTauGio = $"{macTau} · Đi 06:15",
                        TenGaGui = tenGaGui,
                        TenGaNhan = tenGaNhan,
                        MatHangVaDacThu = matHangVaDacThu,
                        TrongLuongTan = trongLuong,
                        TrongLuongHienThi = $"{trongLuong:N2} tấn",
                        NguoiNhanVaSdt = nguoiNhanVaSdt,
                        NguoiGuiVaSdt = nguoiGuiVaSdt,
                        ToaXeDuKien = $"Toa {((sttXep - 1) % 4) + 1}",
                        OChecklistKiemDem = "[   ]"
                    });
                    tongTanXep += trongLuong;
                }
            }

            dgHangDoXuong.ItemsSource = dsDoXuong;
            dgHangXepLen.ItemsSource = dsXepLen;

            txtThongKeDoXuong.Text = $"Tổng số: {dsDoXuong.Count} vận đơn ({tongTanDo:N2} tấn)";
            txtThongKeXepLen.Text = $"Tổng số: {dsXepLen.Count} vận đơn ({tongTanXep:N2} tấn)";

            // Cập nhật hiển thị theo chế độ lọc
            CapNhatCheDoXem();
        }

        private void CapNhatCheDoXem()
        {
            if (radXemTatCa.IsChecked == true)
            {
                pnlPhan1DoXuong.Visibility = Visibility.Visible;
                pnlPhan2XepLen.Visibility = Visibility.Visible;
                txtPhuDeBanKe.Text = "KẾ HOẠCH BÀN GIAO & THEO DÕI HIỆN TRƯỜNG XẾP DỠ HAI CHIỀU (ĐẾN / ĐI)";
            }
            else if (radXemDoXuong.IsChecked == true)
            {
                pnlPhan1DoXuong.Visibility = Visibility.Visible;
                pnlPhan2XepLen.Visibility = Visibility.Collapsed;
                txtPhuDeBanKe.Text = "KẾ HOẠCH THEO DÕI DỠ HÀNG TẠI GA (HÀNG ĐẾN - UNLOADING)";
            }
            else if (radXemXepLen.IsChecked == true)
            {
                pnlPhan1DoXuong.Visibility = Visibility.Collapsed;
                pnlPhan2XepLen.Visibility = Visibility.Visible;
                txtPhuDeBanKe.Text = "KẾ HOẠCH THEO DÕI XẾP HÀNG LÊN TÀU (HÀNG ĐI - LOADING)";
            }
        }

        private void CboGaTacNghiep_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NapDuLieuBanKe();
        }

        private void DpNgayTacNghiep_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            NapDuLieuBanKe();
        }

        private void RadCheDoXem_Click(object sender, RoutedEventArgs e)
        {
            CapNhatCheDoXem();
        }

        private void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            _cachedVanDon = _vdService.LayDanhSach();
            NapDuLieuBanKe();
        }

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
            else if (e.Key == Key.F8 || (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                BtnInBanKe_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void BtnInBanKe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tenGa = "";
                if (cboGaTacNghiep.SelectedItem is DataRowView drv)
                {
                    tenGa = drv["TenGa"]?.ToString() ?? "Ga";
                }

                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    printDlg.PrintVisual(bdInBanKe, $"BanKeXepDo_{tenGa}_{DateTime.Now:yyyyMMdd}");
                    ThongBaoDialog.ThanhCong($"Lệnh in Bản kê tác nghiệp xếp dỡ tại Ga {tenGa} đã gửi đến máy in thành công!", "In Hoàn Tất");
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể thực hiện in bản kê: {ex.Message}", "Lỗi In Ấn");
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
