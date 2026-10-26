using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BUS.Services;
using ET.VanHanh;

namespace GUI.Views.Dialogs
{
    public partial class QuanLyMacTauDialog : Window
    {
        private readonly MacTauService _macTauService = new();
        private readonly GaService _gaService = new();

        private DataTable? _cachedMacTau;
        private DataTable? _cachedGa;
        private bool _isAddingNew = false;
        private int _currentMaMacTau = 0;
        private string _currentHuongChay = "BAC_NAM";

        public int? NewlySelectedOrCreatedMaMacTau { get; private set; }

        public QuanLyMacTauDialog(int? focusMaMacTau = null)
        {
            InitializeComponent();
            _currentMaMacTau = focusMaMacTau ?? 0;
            KhoiTaoDuLieu();
        }

        private void KhoiTaoDuLieu()
        {
            try
            {
                // 1. Tải danh sách Ga
                _cachedGa = _gaService.LayDanhSach();
                if (!_cachedGa.Columns.Contains("TenHienThi"))
                {
                    _cachedGa.Columns.Add("TenHienThi", typeof(string));
                }

                foreach (DataRow r in _cachedGa.Rows)
                {
                    string ten = r["TenGa"]?.ToString() ?? "";
                    string code = r["MaGaCode"]?.ToString() ?? "";
                    decimal km = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0;
                    r["TenHienThi"] = $"{ten} ({code} - Km {km:F1})";
                }

                cboGaDi.ItemsSource = _cachedGa.DefaultView;
                cboGaDen.ItemsSource = _cachedGa.DefaultView;

                // 2. Tải danh sách Mác tàu
                NapDanhSachMacTau();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo dữ liệu mác tàu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NapDanhSachMacTau()
        {
            try
            {
                _cachedMacTau = _macTauService.LayDanhSach();

                if (!_cachedMacTau.Columns.Contains("TenLoaiTauHienThi"))
                    _cachedMacTau.Columns.Add("TenLoaiTauHienThi", typeof(string));
                if (!_cachedMacTau.Columns.Contains("TenTrangThai"))
                    _cachedMacTau.Columns.Add("TenTrangThai", typeof(string));
                if (!_cachedMacTau.Columns.Contains("BgTrangThai"))
                    _cachedMacTau.Columns.Add("BgTrangThai", typeof(Brush));
                if (!_cachedMacTau.Columns.Contains("FgTrangThai"))
                    _cachedMacTau.Columns.Add("FgTrangThai", typeof(Brush));
                if (!_cachedMacTau.Columns.Contains("BorderTrangThai"))
                    _cachedMacTau.Columns.Add("BorderTrangThai", typeof(Brush));

                var brushGreenBg = new SolidColorBrush(Color.FromRgb(240, 253, 244));
                var brushGreenFg = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                var brushGreenBorder = new SolidColorBrush(Color.FromRgb(187, 247, 208));

                var brushGrayBg = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                var brushGrayFg = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                var brushGrayBorder = new SolidColorBrush(Color.FromRgb(203, 213, 225));

                foreach (DataRow r in _cachedMacTau.Rows)
                {
                    string loai = r["LoaiTau"]?.ToString() ?? "";
                    r["TenLoaiTauHienThi"] = loai switch
                    {
                        "TAU_NHANH" => "Tàu Nhanh SE",
                        "TAU_CHO" => "Tàu Chợ NA",
                        "TAU_HANG" => "Tàu Hàng",
                        _ => loai
                    };

                    bool kt = r["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(r["DangKhaiThac"]);
                    if (kt)
                    {
                        r["TenTrangThai"] = "Đang chạy";
                        r["BgTrangThai"] = brushGreenBg;
                        r["FgTrangThai"] = brushGreenFg;
                        r["BorderTrangThai"] = brushGreenBorder;
                    }
                    else
                    {
                        r["TenTrangThai"] = "Tạm dừng";
                        r["BgTrangThai"] = brushGrayBg;
                        r["FgTrangThai"] = brushGrayFg;
                        r["BorderTrangThai"] = brushGrayBorder;
                    }
                }

                ApDungBoLoc();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp danh sách mác tàu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApDungBoLoc()
        {
            if (_cachedMacTau == null) return;

            string tuKhoa = txtTimKiem?.Text.Trim().ToLower() ?? "";
            int indexLoc = cboLocTrangThai?.SelectedIndex ?? 0;

            var dv = _cachedMacTau.DefaultView;
            string filter = "1=1";

            if (!string.IsNullOrEmpty(tuKhoa))
            {
                filter += $" AND (SoHieuMacTau LIKE '%{tuKhoa}%' OR TenGaDi LIKE '%{tuKhoa}%' OR TenGaDen LIKE '%{tuKhoa}%')";
            }

            if (indexLoc == 1) // Đang khai thác
            {
                filter += " AND DangKhaiThac = 1";
            }
            else if (indexLoc == 2) // Tạm dừng
            {
                filter += " AND DangKhaiThac = 0";
            }

            dv.RowFilter = filter;
            dgMacTau.ItemsSource = dv;

            txtTongSoMac.Text = $"Hiển thị: {dv.Count} / {_cachedMacTau.Rows.Count} mác tàu";

            // Chọn lại dòng đang focus nếu có
            if (_currentMaMacTau > 0)
            {
                for (int i = 0; i < dv.Count; i++)
                {
                    if (Convert.ToInt32(dv[i]["MaMacTau"]) == _currentMaMacTau)
                    {
                        dgMacTau.SelectedIndex = i;
                        dgMacTau.ScrollIntoView(dgMacTau.SelectedItem);
                        return;
                    }
                }
            }

            if (dv.Count > 0 && dgMacTau.SelectedIndex < 0)
            {
                dgMacTau.SelectedIndex = 0;
            }
        }

        private void DgMacTau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgMacTau.SelectedItem is DataRowView row)
            {
                _isAddingNew = false;
                _currentMaMacTau = Convert.ToInt32(row["MaMacTau"]);
                NewlySelectedOrCreatedMaMacTau = _currentMaMacTau;

                txtFormTitle.Text = $"MÁC TÀU: {row["SoHieuMacTau"]}";
                txtFormBadge.Text = "CHẾ ĐỘ SỬA";
                bdFormBadge.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                txtFormBadge.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105));

                txtSoHieuMac.Text = row["SoHieuMacTau"]?.ToString() ?? "";
                txtSoHieuMac.IsReadOnly = false;

                string loai = row["LoaiTau"]?.ToString() ?? "TAU_NHANH";
                foreach (ComboBoxItem item in cboLoaiTau.Items)
                {
                    if (item.Tag?.ToString() == loai)
                    {
                        cboLoaiTau.SelectedItem = item;
                        break;
                    }
                }

                cboGaDi.SelectedValue = Convert.ToInt32(row["MaGaDi"]);
                cboGaDen.SelectedValue = Convert.ToInt32(row["MaGaDen"]);

                txtThoiGianChuan.Text = row["ThoiGianChuanGio"] != DBNull.Value ? Convert.ToDecimal(row["ThoiGianChuanGio"]).ToString("F1") : "12.0";

                int uuTien = row["MucUuTien"] != DBNull.Value ? Convert.ToInt32(row["MucUuTien"]) : 1;
                foreach (ComboBoxItem item in cboMucUuTien.Items)
                {
                    if (item.Tag?.ToString() == uuTien.ToString())
                    {
                        cboMucUuTien.SelectedItem = item;
                        break;
                    }
                }

                bool dangKt = row["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(row["DangKhaiThac"]);
                chkDangKhaiThac.IsChecked = dangKt;

                CapNhatNutTrangThai(dangKt);
                CapNhatHuongVaHanhTrinh();

                int soChuyen = row["SoChuyenTauDaChay"] != DBNull.Value ? Convert.ToInt32(row["SoChuyenTauDaChay"]) : 0;
                btnXoa.IsEnabled = (soChuyen == 0);
                btnXoa.ToolTip = soChuyen > 0 ? $"Mác tàu đã có {soChuyen} chuyến chạy, không thể xóa cứng." : "Xóa vĩnh viễn mác tàu này";
            }
        }

        private void CapNhatNutTrangThai(bool dangKhaiThac)
        {
            if (dangKhaiThac)
            {
                txtDoiTrangThai.Text = "Ngừng Khai Thác";
                iconDoiTrangThai.Symbol = Wpf.Ui.Controls.SymbolRegular.Pause24;
            }
            else
            {
                txtDoiTrangThai.Text = "Kích Hoạt Lại";
                iconDoiTrangThai.Symbol = Wpf.Ui.Controls.SymbolRegular.Play24;
            }
        }

        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = true;
            _currentMaMacTau = 0;
            dgMacTau.SelectedItem = null;

            txtFormTitle.Text = "KHAI BÁO MÁC TÀU MỚI";
            txtFormBadge.Text = "TẠO MỚI";
            bdFormBadge.Background = new SolidColorBrush(Color.FromRgb(239, 246, 255));
            txtFormBadge.Foreground = new SolidColorBrush(Color.FromRgb(29, 78, 216));

            txtSoHieuMac.Text = "";
            txtSoHieuMac.IsReadOnly = false;
            cboLoaiTau.SelectedIndex = 0;

            if (_cachedGa != null && _cachedGa.Rows.Count >= 2)
            {
                cboGaDi.SelectedIndex = 0;
                cboGaDen.SelectedIndex = _cachedGa.Rows.Count - 1;
            }

            txtThoiGianChuan.Text = "32.5";
            cboMucUuTien.SelectedIndex = 0;
            chkDangKhaiThac.IsChecked = true;

            btnXoa.IsEnabled = false;
            CapNhatNutTrangThai(true);
            CapNhatHuongVaHanhTrinh();

            txtSoHieuMac.Focus();
        }

        private void CboGa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CapNhatHuongVaHanhTrinh();
        }

        private void CapNhatHuongVaHanhTrinh()
        {
            if (cboGaDi.SelectedItem is DataRowView rDi && cboGaDen.SelectedItem is DataRowView rDen)
            {
                string tenDi = rDi["TenGa"]?.ToString() ?? "";
                string tenDen = rDen["TenGa"]?.ToString() ?? "";
                decimal kmDi = rDi["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(rDi["LyTrinhKm"]) : 0;
                decimal kmDen = rDen["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(rDen["LyTrinhKm"]) : 0;

                string huong = MacTauService.XacDinhHuongChay(kmDi, kmDen);
                _currentHuongChay = huong;

                if (huong == "BAC_NAM")
                {
                    txtHuongChayTuDong.Text = "Bắc → Nam (Chiều Lẻ • Xuôi vào Sài Gòn)";
                    txtHuongChayTuDong.Foreground = new SolidColorBrush(Color.FromRgb(0, 59, 115));
                }
                else if (huong == "NAM_BAC")
                {
                    txtHuongChayTuDong.Text = "Nam → Bắc (Chiều Chẵn • Ngược ra Hà Nội)";
                    txtHuongChayTuDong.Foreground = new SolidColorBrush(Color.FromRgb(15, 118, 110));
                }
                else
                {
                    txtHuongChayTuDong.Text = "⚠️ Ga xuất phát trùng Ga về đích";
                    txtHuongChayTuDong.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                }

                decimal cuLy = Math.Abs(kmDen - kmDi);
                txtThongTinHanhTrinh.Text = $"Hành trình: {tenDi} (Km {kmDi:F1}) → {tenDen} (Km {kmDen:F1}) | Cự ly: {cuLy:F1} km ray.";
            }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            string soHieu = txtSoHieuMac.Text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(soHieu))
            {
                MessageBox.Show("Vui lòng nhập số hiệu mác tàu (ví dụ: SE7, NA1, LP3).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSoHieuMac.Focus();
                return;
            }

            if (cboGaDi.SelectedValue == null || cboGaDen.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn đầy đủ Ga xuất phát và Ga về đích.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int gaDi = Convert.ToInt32(cboGaDi.SelectedValue);
            int gaDen = Convert.ToInt32(cboGaDen.SelectedValue);

            if (gaDi == gaDen)
            {
                MessageBox.Show("Ga xuất phát phải khác Ga về đích!", "Lỗi Tuyến Chạy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtThoiGianChuan.Text.Trim(), out decimal tgChuan) || tgChuan <= 0)
            {
                MessageBox.Show("Thời gian chạy chuẩn phải là một số dương hợp lệ (ví dụ: 32.5 giờ).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtThoiGianChuan.Focus();
                return;
            }

            string loai = "TAU_NHANH";
            if (cboLoaiTau.SelectedItem is ComboBoxItem itemLoai && itemLoai.Tag != null)
                loai = itemLoai.Tag.ToString()!;

            int uuTien = 1;
            if (cboMucUuTien.SelectedItem is ComboBoxItem itemUuTien && itemUuTien.Tag != null)
                uuTien = Convert.ToInt32(itemUuTien.Tag);

            bool dangKt = chkDangKhaiThac.IsChecked ?? true;

            var m = new MacTauMau
            {
                MaMacTau = _isAddingNew ? 0 : _currentMaMacTau,
                SoHieuMacTau = soHieu,
                LoaiTau = loai,
                HuongChay = _currentHuongChay,
                MaGaDi = gaDi,
                MaGaDen = gaDen,
                ThoiGianChuanGio = tgChuan,
                MucUuTien = uuTien,
                DangKhaiThac = dangKt
            };

            if (_macTauService.Luu(m, _isAddingNew, out string loi))
            {
                _currentMaMacTau = m.MaMacTau;
                NewlySelectedOrCreatedMaMacTau = m.MaMacTau;
                MessageBox.Show($"Đã lưu thành công mác tàu '{m.SoHieuMacTau}' vào hệ thống!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                _isAddingNew = false;
                NapDanhSachMacTau();
            }
            else
            {
                MessageBox.Show(loi, "Không thể lưu mác tàu", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDoiTrangThai_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaMacTau <= 0) return;

            bool hienTai = chkDangKhaiThac.IsChecked ?? true;
            bool trangThaiMoi = !hienTai;

            string hanhDong = trangThaiMoi ? "kích hoạt lại" : "tạm ngừng khai thác";
            var r = MessageBox.Show($"Xác nhận {hanhDong} mác tàu '{txtSoHieuMac.Text}'?", "Xác nhận trạng thái", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
            {
                if (_macTauService.DoiTrangThaiKhaiThac(_currentMaMacTau, trangThaiMoi, out string loi))
                {
                    chkDangKhaiThac.IsChecked = trangThaiMoi;
                    CapNhatNutTrangThai(trangThaiMoi);
                    NapDanhSachMacTau();
                }
                else
                {
                    MessageBox.Show(loi, "Lỗi cập nhật", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaMacTau <= 0) return;

            string soHieu = txtSoHieuMac.Text;
            var r = MessageBox.Show($"Bạn có chắc chắn muốn xóa vĩnh viễn mác tàu '{soHieu}' khỏi hệ thống?\n(Hành động này không thể hoàn tác).",
                "Xác nhận xóa mác tàu", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (r == MessageBoxResult.Yes)
            {
                if (_macTauService.Xoa(_currentMaMacTau, out string loi))
                {
                    MessageBox.Show($"Đã xóa vĩnh viễn mác tàu '{soHieu}'.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    _currentMaMacTau = 0;
                    NewlySelectedOrCreatedMaMacTau = null;
                    NapDanhSachMacTau();
                }
                else
                {
                    MessageBox.Show(loi, "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void TxtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApDungBoLoc();
        }

        private void CboLocTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApDungBoLoc();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
