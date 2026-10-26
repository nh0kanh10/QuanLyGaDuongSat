using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BUS.Services;
using ET.VanHanh;

namespace GUI.Views.Dialogs
{
    public partial class DinhMucVanTocDialog : Window
    {
        private readonly DinhMucVanTocService _service = new();
        private DataTable? _cachedTable;
        private bool _isAddingNew = false;
        private string _currentMaLoaiTau = "";

        public bool CoThayDoiDuLieu { get; private set; } = false;

        public DinhMucVanTocDialog()
        {
            InitializeComponent();
            NapDanhSach();
        }

        private void NapDanhSach(string? selectMaLoai = null)
        {
            try
            {
                _cachedTable = _service.LayDanhSach(null);
                dgDinhMuc.ItemsSource = _cachedTable.DefaultView;

                if (txtBadgeSoLoai != null)
                {
                    txtBadgeSoLoai.Text = $"{_cachedTable.Rows.Count} loại";
                }

                if (_cachedTable.Rows.Count > 0)
                {
                    if (!string.IsNullOrEmpty(selectMaLoai))
                    {
                        for (int i = 0; i < dgDinhMuc.Items.Count; i++)
                        {
                            if (dgDinhMuc.Items[i] is DataRowView drv &&
                                string.Equals(drv["MaLoaiTau"]?.ToString(), selectMaLoai, StringComparison.OrdinalIgnoreCase))
                            {
                                dgDinhMuc.SelectedIndex = i;
                                dgDinhMuc.ScrollIntoView(dgDinhMuc.SelectedItem);
                                return;
                            }
                        }
                    }

                    dgDinhMuc.SelectedIndex = 0;
                }
                else
                {
                    ChuyenCheDoThemMoi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh mục định mức vận tốc: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgDinhMuc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDinhMuc.SelectedItem is DataRowView drv)
            {
                _isAddingNew = false;
                _currentMaLoaiTau = drv["MaLoaiTau"]?.ToString() ?? "";

                txtTieuDeForm.Text = $"SỬA ĐỊNH MỨC: {_currentMaLoaiTau}";
                txtMaLoaiTau.Text = _currentMaLoaiTau;
                txtMaLoaiTau.IsReadOnly = true;
                txtMaLoaiTau.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#F1F5F9")!;

                txtTenLoaiTau.Text = drv["TenLoaiTau"]?.ToString() ?? "";

                decimal vt = drv["VanTocDinhMucKmh"] != DBNull.Value ? Convert.ToDecimal(drv["VanTocDinhMucKmh"]) : 50.0m;
                txtVanTocDinhMuc.Text = vt.ToString("F1", CultureInfo.InvariantCulture);

                txtMoTaQuyChuan.Text = drv["MoTaQuyChuan"]?.ToString() ?? "";
                txtThuTu.Text = drv["ThuTuHienThi"]?.ToString() ?? "1";

                bool dangApDung = drv["DangApDung"] != DBNull.Value && Convert.ToBoolean(drv["DangApDung"]);
                if (dangApDung) radDangApDung.IsChecked = true; else radTamNgung.IsChecked = true;

                int soMac = drv["SoMacTauDangDung"] != DBNull.Value ? Convert.ToInt32(drv["SoMacTauDangDung"]) : 0;
                if (txtThongKeMacDung != null)
                {
                    txtThongKeMacDung.Text = $"Đang áp dụng cho {soMac} mác tàu trên tuyến";
                }
            }
        }

        private void ChuyenCheDoThemMoi()
        {
            _isAddingNew = true;
            _currentMaLoaiTau = "";

            txtTieuDeForm.Text = "THÊM MỚI ĐỊNH MỨC VẬN TỐC";
            txtMaLoaiTau.Text = "";
            txtMaLoaiTau.IsReadOnly = false;
            txtMaLoaiTau.Background = System.Windows.Media.Brushes.White;

            txtTenLoaiTau.Text = "";
            txtVanTocDinhMuc.Text = "50.0";
            txtMoTaQuyChuan.Text = "";
            txtThuTu.Text = (_cachedTable != null ? (_cachedTable.Rows.Count + 1).ToString() : "1");
            radDangApDung.IsChecked = true;

            if (txtThongKeMacDung != null)
            {
                txtThongKeMacDung.Text = "Khai báo định mức loại tàu mới vào hệ thống";
            }

            txtMaLoaiTau.Focus();
        }

        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            ChuyenCheDoThemMoi();
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            ThucHienLuu();
        }

        private void ThucHienLuu()
        {
            string ma = txtMaLoaiTau.Text.Trim().ToUpperInvariant();
            string ten = txtTenLoaiTau.Text.Trim();
            string rawVt = txtVanTocDinhMuc.Text.Trim().Replace(',', '.');
            string moTa = txtMoTaQuyChuan.Text.Trim();
            string rawThuTu = txtThuTu.Text.Trim();
            bool dangApDung = radDangApDung.IsChecked == true;

            if (string.IsNullOrWhiteSpace(ma))
            {
                MessageBox.Show("Vui lòng nhập Mã loại tàu (ví dụ: TAU_NHANH, TAU_CHO).", "Thiếu Dữ Liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMaLoaiTau.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ten))
            {
                MessageBox.Show("Vui lòng nhập Tên loại tàu.", "Thiếu Dữ Liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTenLoaiTau.Focus();
                return;
            }

            if (!decimal.TryParse(rawVt, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal vanToc) || vanToc < 10.0m || vanToc > 200.0m)
            {
                MessageBox.Show("Vận tốc định mức lữ hành phải là số thực từ 10.0 đến 200.0 km/h (ví dụ: 53.0).", "Sai Dữ Liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtVanTocDinhMuc.Focus();
                txtVanTocDinhMuc.SelectAll();
                return;
            }

            if (!int.TryParse(rawThuTu, out int thuTu) || thuTu < 1)
            {
                thuTu = 1;
            }

            var dm = new DinhMucVanToc
            {
                MaLoaiTau = ma,
                TenLoaiTau = ten,
                VanTocDinhMucKmh = vanToc,
                MoTaQuyChuan = string.IsNullOrWhiteSpace(moTa) ? null : moTa,
                MauNhanDien = ma.Contains("NHANH") || ma.Contains("SE") ? "#0284C7" : (ma.Contains("HANG") ? "#475569" : "#D97706"),
                ThuTuHienThi = thuTu,
                DangApDung = dangApDung
            };

            bool ok;
            string loi;

            if (_isAddingNew)
            {
                ok = _service.Them(dm, out loi);
            }
            else
            {
                ok = _service.CapNhat(dm, out loi);
            }

            if (ok)
            {
                CoThayDoiDuLieu = true;
                MessageBox.Show($"Đã lưu định mức loại tàu '{ma}' ({vanToc:F1} km/h) thành công!", "Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
                NapDanhSach(ma);
            }
            else
            {
                MessageBox.Show($"Không thể lưu định mức:\n{loi}", "Lỗi Nghiệp Vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_isAddingNew || string.IsNullOrWhiteSpace(_currentMaLoaiTau))
            {
                MessageBox.Show("Vui lòng chọn một loại tàu từ danh sách để xóa.", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var xn = MessageBox.Show($"Bạn có chắc chắn muốn xóa định mức loại tàu '{_currentMaLoaiTau}' không?\n\nLưu ý: Chỉ xóa được nếu không có mác tàu nào đang dùng loại này.",
                "Xác Nhận Xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (xn != MessageBoxResult.Yes) return;

            if (_service.Xoa(_currentMaLoaiTau, out string loi))
            {
                CoThayDoiDuLieu = true;
                MessageBox.Show($"Đã xóa định mức loại tàu '{_currentMaLoaiTau}' thành công!", "Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
                NapDanhSach();
            }
            else
            {
                MessageBox.Show(loi, "Không Thể Xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnHuyBo_Click(object sender, RoutedEventArgs e)
        {
            if (_isAddingNew)
            {
                if (dgDinhMuc.Items.Count > 0)
                {
                    dgDinhMuc.SelectedIndex = 0;
                }
            }
            else
            {
                DgDinhMuc_SelectionChanged(dgDinhMuc, null!);
            }
        }

        private void BtnNapLai_Click(object sender, RoutedEventArgs e)
        {
            NapDanhSach(_currentMaLoaiTau);
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ThucHienLuu();
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                ChuyenCheDoThemMoi();
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                NapDanhSach(_currentMaLoaiTau);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                if (Keyboard.FocusedElement is not TextBox)
                {
                    BtnXoa_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
        }

        private void TxtVanTocDinhMuc_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Cho phép nhập số và dấu chấm hoặc phẩy thập phân
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9\.\,]$");
        }

        private void TxtThuTu_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");
        }
    }
}
