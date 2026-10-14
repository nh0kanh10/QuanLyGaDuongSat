using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using BUS.Services;
using DTO.Common;
using ET.HaTang;
using GUI.Helpers;
using GUI.Profiles;
using GUI.Views.Dialogs;
using GUI.Views.UserControls;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;
using SymbolIcon = Wpf.Ui.Controls.SymbolIcon;

namespace GUI.Views.Pages
{
    public partial class KhuGianPage : Page
    {
        private readonly KhuGianService _khuGianService = new();
        private readonly GaService _gaService = new();
        private readonly DuongNgangService _duongNgangService = new();

        private DataTable? _cachedTable;
        private DataTable? _dtGa;
        private DataTable? _dtDuongNgang;
        private int _currentMaKg = 0;
        private int _currentMaDnEdit = 0;
        private decimal _currentLyTrinhMin = 0m;
        private decimal _currentLyTrinhMax = 0m;
        private string _currentTenPhanDoan = "";
        private string _currentTenGaDau = "";
        private string _currentTenGaCuoi = "";
        private bool _isAddingNew = false;
        private bool _isDirty = false;
        private bool _isLoadingData = false;
        private string _currentViewMode = "BANG"; // "BANG" hoặc "SODO"

        public KhuGianPage()
        {
            InitializeComponent();
            Loaded += KhuGianPage_Loaded;
            SmartNumberFormatHelper.AttachDecimalKm(txtCuLyKg, min: 0.1m, max: 999.99m);
            SmartNumberFormatHelper.AttachIntegerSmall(txtVmaxKhach, min: 10, max: 200);
            SmartNumberFormatHelper.AttachIntegerSmall(txtVmaxHang, min: 10, max: 150);
            SmartNumberFormatHelper.AttachDecimal(txtDoDocKg, min: 0.0m, max: 50.0m, decimalPlaces: 1);
            SmartNumberFormatHelper.AttachDecimalKm(txtDnLyTrinh, min: 0m, max: 5000m);
        }

        private void KhuGianPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDanhSachGa();
            LoadData();
        }

        private void LoadDanhSachGa()
        {
            try
            {
                _dtGa = _gaService.LayDanhSach();
                cboGaDau.Items.Clear();
                cboGaCuoi.Items.Clear();

                if (_dtGa != null)
                {
                    foreach (DataRow r in _dtGa.Rows)
                    {
                        int maGa = Convert.ToInt32(r["MaGa"]);
                        string code = r["MaGaCode"]?.ToString() ?? "";
                        string ten = r["TenGa"]?.ToString() ?? "";
                        decimal km = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m;
                        string itemText = $"[{code}] {ten} (Km {FormatHelper.FormatKm(km)})";

                        // cboGaDau: toàn bộ danh sách ga
                        cboGaDau.Items.Add(new ComboBoxItem { Content = itemText, Tag = maGa });
                        // cboGaCuoi: sẽ được tự động dựa vào ga liền kề khi cboGaDau thay đổi
                    }
                }

                if (cboGaDau.Items.Count > 0) cboGaDau.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi("Không thể nạp danh mục ga: " + ex.Message, "Lỗi Dữ Liệu");
            }
        }

        public void LoadData(int selectMaKg = 0)
        {
            _isLoadingData = true;
            try
            {
                _cachedTable = _khuGianService.LayDanhSach();

                if (_cachedTable != null)
                {
                    if (!_cachedTable.Columns.Contains("STT"))
                    {
                        _cachedTable.Columns.Add("STT", typeof(int));
                    }
                    for (int i = 0; i < _cachedTable.Rows.Count; i++)
                    {
                        _cachedTable.Rows[i]["STT"] = i + 1;
                    }
                }

                ApplyFilter();
                CapNhatThongKeTong();
                VeSoDoTuyen();

                if (selectMaKg > 0)
                {
                    ChonKhuGianTheoId(selectMaKg);
                }
                else if (dgKhuGian.Items.Count > 0 && dgKhuGian.SelectedIndex < 0)
                {
                    dgKhuGian.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi("Không thể tải danh sách phân đoạn khu gian: " + ex.Message, "Lỗi Truy Vấn");
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void CapNhatThongKeTong()
        {
            if (_cachedTable == null) return;

            int count = _cachedTable.Rows.Count;
            decimal tongKm = 0m;
            int mayDayCount = 0;
            int tongDn = 0;

            foreach (DataRow r in _cachedTable.Rows)
            {
                if (r["CuLyKm"] != DBNull.Value)
                    tongKm += Convert.ToDecimal(r["CuLyKm"]);

                if (r["CanDauMayDay"] != DBNull.Value && Convert.ToBoolean(r["CanDauMayDay"]))
                    mayDayCount++;

                if (r.Table.Columns.Contains("SoDuongNgang") && r["SoDuongNgang"] != DBNull.Value)
                    tongDn += Convert.ToInt32(r["SoDuongNgang"]);
            }

            txtTongSoPhanDoan.Text = $"Tổng số: {count} phân đoạn";
            txtTongCuLy.Text = $"Tổng cự ly: {FormatHelper.FormatKm(tongKm)} km";
            txtTongDuongNgang.Text = $"Đường ngang: {tongDn} vị trí";
            txtMayDayCount.Text = $"Đoạn cần máy đẩy: {mayDayCount} đèo dốc";
        }

        private static string EscapeLikeFilter(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var sb = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (c == '[' || c == ']' || c == '*' || c == '%')
                {
                    sb.Append('[').Append(c).Append(']');
                }
                else if (c == '\'')
                {
                    sb.Append("''");
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void ApplyFilter()
        {
            if (_cachedTable == null) return;

            try
            {
                string rawKw = txtTimKiem?.Text?.Trim() ?? "";
                if (rawKw.Length > 100)
                {
                    rawKw = rawKw.Substring(0, 100);
                }
                string keywordSafe = EscapeLikeFilter(rawKw);
                string filterStatus = "";

                switch (cboLocTrangThai.SelectedIndex)
                {
                    case 1:
                        filterStatus = "RONG";
                        break;
                    case 2:
                        filterStatus = "CO_TAU";
                        break;
                    case 3:
                        filterStatus = "PHONG_TOA";
                        break;
                    default:
                        filterStatus = "";
                        break;
                }

                var conditions = new List<string>();

                if (!string.IsNullOrEmpty(keywordSafe))
                {
                    conditions.Add($"(TenGaDau LIKE '%{keywordSafe}%' OR TenGaCuoi LIKE '%{keywordSafe}%' OR MaGaDauCode LIKE '%{keywordSafe}%' OR MaGaCuoiCode LIKE '%{keywordSafe}%')");
                }

                if (!string.IsNullOrEmpty(filterStatus))
                {
                    conditions.Add($"TrangThai = '{filterStatus}'");
                }

                if (chkLocDiemDen != null && chkLocDiemDen.IsChecked == true)
                {
                    conditions.Add("SoDiemDen > 0");
                }

                var view = _cachedTable.DefaultView;
                view.RowFilter = conditions.Count > 0 ? string.Join(" AND ", conditions) : "";

                dgKhuGian.ItemsSource = view;

                bool coKetQua = view.Count > 0;
                pnlEmptyState.Visibility = coKetQua ? Visibility.Collapsed : Visibility.Visible;
                btnXoaTimKiem.Visibility = string.IsNullOrEmpty(rawKw) ? Visibility.Collapsed : Visibility.Visible;
                if (btnDatLaiLoc != null)
                {
                    btnDatLaiLoc.Visibility = conditions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }

                if (coKetQua && (dgKhuGian.SelectedIndex < 0 || dgKhuGian.SelectedIndex >= view.Count))
                {
                    dgKhuGian.SelectedIndex = 0;
                }

                // Đồng bộ bộ lọc sang Sơ đồ tuyến nếu đang mở chế độ này
                if (_currentViewMode == "SODO")
                {
                    VeSoDoTuyen();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KhuGianPage.ApplyFilter] Lỗi áp dụng bộ lọc: {ex.Message}");
            }
        }

        private void TxtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_isLoadingData) return;
                if (txtTimKiem != null && txtTimKiem.Text.Length > 100)
                {
                    txtTimKiem.Text = txtTimKiem.Text.Substring(0, 100);
                    txtTimKiem.CaretIndex = 100;
                    return;
                }
                ApplyFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KhuGianPage.TxtTimKiem_TextChanged] Lỗi: {ex.Message}");
            }
        }

        private void BtnXoaTimKiem_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Text = "";
            txtTimKiem.Focus();
        }

        private void CboLocTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingData) return;
            ApplyFilter();
        }

        private void ChkLocDiemDen_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingData) return;
            ApplyFilter();
        }

        private void BtnDatLaiLoc_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Text = "";
            cboLocTrangThai.SelectedIndex = 0;
            if (chkLocDiemDen != null) chkLocDiemDen.IsChecked = false;
            ApplyFilter();
        }

        private void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadDanhSachGa();
                LoadData(_currentMaKg);
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi làm mới dữ liệu: {ex.Message}", "Lỗi Làm Mới");
            }
        }

        private void DgKhuGian_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingData) return;

            try
            {
                if (_isDirty)
                {
                    _isDirty = false;
                }

                if (dgKhuGian.SelectedItem is DataRowView drv)
                {
                    PopulateForm(drv);
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi chọn phân đoạn: {ex.Message}", "Lỗi Lựa Chọn");
            }
        }

        private void DgKhuGian_Sorting(object sender, DataGridSortingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (dgKhuGian.ItemsSource is DataView dv)
                    {
                        for (int i = 0; i < dv.Count; i++)
                        {
                            dv[i]["STT"] = i + 1;
                        }
                    }
                }
                catch
                {
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void PopulateForm(DataRowView drv)
        {
            _isLoadingData = true;
            try
            {
                _currentMaKg = Convert.ToInt32(drv["MaKhuGian"]);
                _isAddingNew = false;
                _isDirty = false;

                int maGaDau = Convert.ToInt32(drv["MaGaDau"]);
                int maGaCuoi = Convert.ToInt32(drv["MaGaCuoi"]);
                string tenDau = drv["TenGaDau"]?.ToString() ?? "";
                string tenCuoi = drv["TenGaCuoi"]?.ToString() ?? "";
                string codeDau = drv["MaGaDauCode"]?.ToString() ?? "";
                string codeCuoi = drv["MaGaCuoiCode"]?.ToString() ?? "";
                decimal lyTrinhDau = drv["LyTrinhDauKm"] != DBNull.Value ? Convert.ToDecimal(drv["LyTrinhDauKm"]) : 0m;
                decimal lyTrinhCuoi = drv["LyTrinhCuoiKm"] != DBNull.Value ? Convert.ToDecimal(drv["LyTrinhCuoiKm"]) : 0m;

                txtTitleFormKg.Text = $"{tenDau} ↔ {tenCuoi}";
                txtSubtitleFormKg.Text = "Cấu hình thông số kỹ thuật phân đoạn";
                txtBadgeKg.Text = $"ID: #{_currentMaKg}";
                badgeContainerKg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                badgeContainerKg.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                txtBadgeKg.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));

                if (bdDirtyKg != null) bdDirtyKg.Visibility = Visibility.Collapsed;
                if (txtAuditThoiDiem != null) txtAuditThoiDiem.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                pnlRouteBanner.Visibility = Visibility.Visible;
                pnlRouteSelect.Visibility = Visibility.Collapsed;

                bool bacLaDau = lyTrinhDau <= lyTrinhCuoi;
                string bCodeDau = bacLaDau ? codeDau : codeCuoi;
                string bTenDau = bacLaDau ? tenDau : tenCuoi;
                decimal bKmDau = bacLaDau ? lyTrinhDau : lyTrinhCuoi;

                string bCodeCuoi = bacLaDau ? codeCuoi : codeDau;
                string bTenCuoi = bacLaDau ? tenCuoi : tenDau;
                decimal bKmCuoi = bacLaDau ? lyTrinhCuoi : lyTrinhDau;



                txtBannerCodeDau.Text = bCodeDau;
                txtBannerTenDau.Text = bTenDau;
                txtBannerKmDau.Text = $"Km {FormatHelper.FormatKm(bKmDau)}";

                txtBannerCodeCuoi.Text = bCodeCuoi;
                txtBannerTenCuoi.Text = bTenCuoi;
                txtBannerKmCuoi.Text = $"Km {FormatHelper.FormatKm(bKmCuoi)}";

                decimal khoangCach = Math.Abs(lyTrinhCuoi - lyTrinhDau);
                txtBannerKhoangCachGa.Text = $"Khoảng cách lý trình: {FormatHelper.FormatKm(khoangCach)} km";

                ChonComboTheoTag(cboGaDau, maGaDau);
                ChonComboTheoTag(cboGaCuoi, maGaCuoi);

                decimal culy = drv["CuLyKm"] != DBNull.Value ? Convert.ToDecimal(drv["CuLyKm"]) : 0m;
                txtCuLyKg.Text = FormatHelper.FormatKm(culy);

                txtVmaxKhach.Text = drv["TocDoToiDaKhach"]?.ToString() ?? "80";
                txtVmaxHang.Text = drv["TocDoToiDaHang"]?.ToString() ?? "50";

                decimal doDoc = drv["DoDocPermil"] != DBNull.Value ? Convert.ToDecimal(drv["DoDocPermil"]) : 0m;
                txtDoDocKg.Text = doDoc.ToString("0.0");

                chkCanDauMayDay.IsChecked = drv["CanDauMayDay"] != DBNull.Value && Convert.ToBoolean(drv["CanDauMayDay"]);

                string tt = drv["TrangThai"]?.ToString() ?? "RONG";
                cboTrangThaiKg.SelectedIndex = tt switch
                {
                    "CO_TAU" => 1,
                    "PHONG_TOA" => 2,
                    _ => 0
                };
                CapNhatVisualTrangThaiRay(tt);

                // Cập nhật tính toán vận hành phân đoạn tự động
                CapNhatTinhToanVanHanh();

                txtLuuKg.Text = "Lưu Phân Đoạn";
                if (btnXoaKg != null)
                {
                    btnXoaKg.Visibility = Visibility.Visible;
                    btnXoaKg.IsEnabled = true;
                }

                _currentLyTrinhMin = Math.Min(lyTrinhDau, lyTrinhCuoi);
                _currentLyTrinhMax = Math.Max(lyTrinhDau, lyTrinhCuoi);
                _currentTenPhanDoan = $"{bTenDau} — {bTenCuoi}";
                _currentTenGaDau = bTenDau;
                _currentTenGaCuoi = bTenCuoi;

                txtTrackGaDau.Text = $"{bTenDau} (Km {FormatHelper.FormatKm(bKmDau)})";
                txtTrackGaCuoi.Text = $"{bTenCuoi} (Km {FormatHelper.FormatKm(bKmCuoi)})";

                tabDuongNgangKg.IsEnabled = true;
                txtDnKhuGianTen.Text = $"Phân đoạn: {_currentTenPhanDoan}";
                txtDnPhamViKm.Text = $"Phạm vi: Km {FormatHelper.FormatKm(_currentLyTrinhMin)} — Km {FormatHelper.FormatKm(_currentLyTrinhMax)}";
                txtDnHintKm.Text = $"Phạm vi: Km {FormatHelper.FormatKm(_currentLyTrinhMin)} — Km {FormatHelper.FormatKm(_currentLyTrinhMax)}";

                TaiDanhSachDuongNgang(_currentMaKg);
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có sự cố khi hiển thị thông tin phân đoạn khu gian:\n{ex.Message}", "Lỗi Hiển Thị");
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void ChonComboTheoTag(ComboBox cbo, int tagId)
        {
            foreach (ComboBoxItem item in cbo.Items)
            {
                if (item.Tag is int id && id == tagId)
                {
                    cbo.SelectedItem = item;
                    return;
                }
            }
        }

        public void ChonKhuGianTheoId(int maKg)
        {
            if (dgKhuGian.ItemsSource is DataView dv)
            {
                for (int i = 0; i < dv.Count; i++)
                {
                    if (Convert.ToInt32(dv[i]["MaKhuGian"]) == maKg)
                    {
                        dgKhuGian.SelectedIndex = i;
                        dgKhuGian.ScrollIntoView(dgKhuGian.SelectedItem);
                        return;
                    }
                }
            }
        }

        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            ClearForm(isNew: true);
        }

        private void ClearForm(bool isNew)
        {
            _isLoadingData = true;
            try
            {
                _isAddingNew = isNew;
                _currentMaKg = 0;
                _isDirty = false;
                if (bdDirtyKg != null) bdDirtyKg.Visibility = Visibility.Collapsed;

                if (txtAuditThoiDiem != null) txtAuditThoiDiem.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                CapNhatTinhToanVanHanh();

                txtTitleFormKg.Text = isNew ? "THÊM PHÂN ĐOẠN KHU GIAN MỚI" : "CHI TIẾT PHÂN ĐOẠN";
                txtSubtitleFormKg.Text = isNew ? "Khai báo phân đoạn liên kết giữa hai ga" : "Cấu hình thông số kỹ thuật";
                txtBadgeKg.Text = isNew ? "TẠO MỚI" : "ID: #0";
                badgeContainerKg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                badgeContainerKg.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isNew ? "#16A34A" : "#CBD5E1"));
                txtBadgeKg.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isNew ? "#15803D" : "#475569"));

                if (isNew)
                {
                    tabDuongNgangKg.IsEnabled = false;
                    tabInspectorKg.SelectedIndex = 0;
                    pnlFormDuongNgang.Visibility = Visibility.Collapsed;
                    pnlCardsDuongNgang.Children.Clear();
                    txtTabBadgeDn.Text = "0";
                    bdTabBadgeDn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    txtTabBadgeDn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                    pnlEmptyDn.Visibility = Visibility.Visible;
                    txtDnTongKetTab.Text = "Tổng số: 0 vị trí giao cắt";

                    pnlRouteBanner.Visibility = Visibility.Collapsed;
                    pnlRouteSelect.Visibility = Visibility.Visible;
                    cboGaDau.IsEnabled = true;
                    cboGaCuoi.IsEnabled = true;

                    if (cboGaDau.Items.Count > 0) cboGaDau.SelectedIndex = 0;

                    txtLuuKg.Text = "Tạo Mới Phân Đoạn";
                    if (btnXoaKg != null) btnXoaKg.Visibility = Visibility.Collapsed;
                    cboGaDau.Focus();
                }
                else
                {
                    tabDuongNgangKg.IsEnabled = true;
                    pnlRouteBanner.Visibility = Visibility.Visible;
                    pnlRouteSelect.Visibility = Visibility.Collapsed;
                    txtLuuKg.Text = "Lưu Phân Đoạn";
                    if (btnXoaKg != null)
                    {
                        btnXoaKg.Visibility = Visibility.Visible;
                        btnXoaKg.IsEnabled = true;
                    }
                }

                txtCuLyKg.Text = "30.00";
                txtVmaxKhach.Text = "80";
                txtVmaxHang.Text = "50";
                txtDoDocKg.Text = "0.0";
                chkCanDauMayDay.IsChecked = false;
                cboTrangThaiKg.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi làm mới biểu mẫu phân đoạn:\n{ex.Message}", "Lỗi Biểu Mẫu");
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void BtnHuyKg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isAddingNew)
                {
                    if (dgKhuGian.SelectedItem is DataRowView drv)
                    {
                        PopulateForm(drv);
                    }
                    else if (dgKhuGian.Items.Count > 0)
                    {
                        dgKhuGian.SelectedIndex = 0;
                    }
                }
                else
                {
                    if (dgKhuGian.SelectedItem is DataRowView drv)
                    {
                        PopulateForm(drv);
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi hủy thao tác:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void BtnLuuKg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cboGaDau.SelectedItem is not ComboBoxItem itDau || itDau.Tag is not int maGaDau)
                {
                    ThongBaoDialog.CanhBao("Vui lòng chỉ định Ga Đầu phía Bắc của phân đoạn.", "Thiếu Dữ Liệu");
                    cboGaDau.Focus();
                    return;
                }

                if (cboGaCuoi.SelectedItem is not ComboBoxItem itCuoi || itCuoi.Tag is not int maGaCuoi)
                {
                    ThongBaoDialog.CanhBao("Vui lòng chỉ định Ga Cuối phía Nam của phân đoạn.", "Thiếu Dữ Liệu");
                    cboGaCuoi.Focus();
                    return;
                }

                if (maGaDau == maGaCuoi)
                {
                    ThongBaoDialog.CanhBao("Ga Đầu và Ga Cuối của phân đoạn không được trùng nhau.", "Lỗi Ràng Buộc Ga");
                    cboGaCuoi.Focus();
                    return;
                }

                if (!FormatHelper.TryParseKm(txtCuLyKg.Text, out decimal cuLyKm) || cuLyKm <= 0 || cuLyKm > 999.99m)
                {
                    ThongBaoDialog.CanhBao("Cự ly phân đoạn khu gian phải lớn hơn 0 và không vượt quá 999.99 km (giới hạn CSDL).", "Sai Định Dạng Cự Ly");
                    txtCuLyKg.Focus();
                    return;
                }

                if (!int.TryParse(txtVmaxKhach.Text.Trim(), out int vmaxKhach) || vmaxKhach < 10 || vmaxKhach > 200)
                {
                    ThongBaoDialog.CanhBao("Tốc độ tối đa tàu khách phải nằm trong khoảng 10 - 200 km/h (tiêu chuẩn khai thác: 80 - 100 km/h).", "Lỗi Tốc Độ");
                    txtVmaxKhach.Focus();
                    return;
                }

                if (!int.TryParse(txtVmaxHang.Text.Trim(), out int vmaxHang) || vmaxHang < 10 || vmaxHang > 150)
                {
                    ThongBaoDialog.CanhBao("Tốc độ tối đa tàu hàng phải nằm trong khoảng 10 - 150 km/h (tiêu chuẩn khai thác: 50 - 60 km/h).", "Lỗi Tốc Độ");
                    txtVmaxHang.Focus();
                    return;
                }

                if (vmaxHang > vmaxKhach)
                {
                    ThongBaoDialog.CanhBao($"Tốc độ tối đa tàu hàng ({vmaxHang} km/h) không được vượt quá tốc độ tối đa tàu khách ({vmaxKhach} km/h).", "Quy Chuẩn Tốc Độ");
                    txtVmaxHang.Focus();
                    return;
                }

                if (!FormatHelper.TryParseDecimal(txtDoDocKg.Text, out decimal doDoc) || doDoc < 0 || doDoc > 50.0m)
                {
                    ThongBaoDialog.CanhBao("Độ dốc lớn nhất phải từ 0.0‰ đến 50.0‰ (đoạn đèo dốc hiểm trở nhất ĐSVN hiện tại ~17.5‰).", "Lỗi Độ Dốc");
                    txtDoDocKg.Focus();
                    return;
                }

                // Kiểm tra cảnh báo nếu cự ly nhập tay lệch đáng kể (> 25%) so với khoảng cách lý trình giữa 2 ga
                decimal lyTrinhDelta = 0m;
                if (_isAddingNew)
                {
                    decimal kmDau = TimLyTrinhGa(maGaDau);
                    decimal kmCuoi = TimLyTrinhGa(maGaCuoi);
                    lyTrinhDelta = Math.Abs(kmCuoi - kmDau);
                }
                else
                {
                    lyTrinhDelta = Math.Abs(_currentLyTrinhMax - _currentLyTrinhMin);
                }

                if (lyTrinhDelta > 0 && Math.Abs(cuLyKm - lyTrinhDelta) / lyTrinhDelta > 0.25m)
                {
                    string thongBaoLech = $"Cự ly thực tế vừa nhập ({FormatHelper.FormatKm(cuLyKm)} km) lệch đáng kể so với khoảng cách lý trình ({FormatHelper.FormatKm(lyTrinhDelta)} km).\n\nBạn có chắc chắn muốn lưu thông số phân đoạn này không?";
                    if (!ThongBaoDialog.XacNhan(thongBaoLech, "Cảnh Báo Lệch Cự Ly"))
                    {
                        txtCuLyKg.Focus();
                        return;
                    }
                }

                // Khuyến nghị an toàn: Nếu độ dốc lớn mà chưa bố trí máy đẩy đèo dốc
                if (doDoc >= 15.0m && chkCanDauMayDay.IsChecked != true)
                {
                    if (ThongBaoDialog.XacNhan($"Phân đoạn này có độ dốc lớn ({doDoc:0.0}‰ >= 15.0‰). Bạn có muốn kích hoạt 'Bố trí máy đẩy đèo dốc' để đảm bảo an toàn kéo tàu hàng nặng không?", "Khuyến Nghị An Toàn"))
                    {
                        chkCanDauMayDay.IsChecked = true;
                    }
                }

                // Chặn gán trạng thái CO_TAU khi tạo mới danh mục
                if (_isAddingNew && cboTrangThaiKg.SelectedIndex == 1)
                {
                    ThongBaoDialog.CanhBao("Phân đoạn khu gian mới khai báo chưa có đoàn tàu hoạt động. Vui lòng chọn 'Thông đường sẵn sàng' hoặc 'Phong tỏa thi công'.", "Trạng Thái Không Hợp Lệ");
                    cboTrangThaiKg.Focus();
                    return;
                }

                // làm tròn theo đúng độ chính xác
                cuLyKm = Math.Round(cuLyKm, 2);
                doDoc = Math.Round(doDoc, 1);

                // Cảnh báo ngưỡng biên kỹ thuật đường sắt
                if (cuLyKm > 150m)
                {
                    if (!ThongBaoDialog.XacNhan($"Cự ly vừa nhập ({FormatHelper.FormatKm(cuLyKm)} km) lớn hơn thông thường đối với một phân đoạn khu gian đơn tuyến. Bạn có chắc chắn số liệu này chính xác không?", "Cảnh Báo Cự Ly Lớn"))
                    {
                        txtCuLyKg.Focus();
                        return;
                    }
                }

                if (doDoc > 20.0m)
                {
                    if (!ThongBaoDialog.XacNhan($"Độ dốc ({doDoc:0.0}‰) vượt quá độ dốc khai thác thông thường của ĐSVN (~17.5‰ đèo Hải Vân). Bạn có chắc chắn muốn lưu thông số này không?", "Cảnh Báo Độ Dốc Cao"))
                    {
                        txtDoDocKg.Focus();
                        return;
                    }
                }

                string trangThai = "RONG";
                if (cboTrangThaiKg.SelectedIndex == 1) trangThai = "CO_TAU";
                else if (cboTrangThaiKg.SelectedIndex == 2) trangThai = "PHONG_TOA";

                var kg = new KhuGian
                {
                    MaKhuGian = _currentMaKg,
                    MaGaDau = maGaDau,
                    MaGaCuoi = maGaCuoi,
                    CuLyKm = cuLyKm,
                    TocDoToiDaKhach = vmaxKhach,
                    TocDoToiDaHang = vmaxHang,
                    DoDocPermil = doDoc,
                    CanDauMayDay = chkCanDauMayDay.IsChecked == true,
                    TrangThai = trangThai
                };

                if (_isAddingNew)
                {
                    bool thanhCong = _khuGianService.Them(kg, out string loi);
                    if (thanhCong)
                    {
                        _isDirty = false;
                        if (bdDirtyKg != null) bdDirtyKg.Visibility = Visibility.Collapsed;
                        ThongBaoDialog.ThanhCong("Thêm mới phân đoạn khu gian thành công vào mạng lưới!", "Thao Tác Thành Công");
                        LoadData();
                    }
                    else
                    {
                        ThongBaoDialog.Loi(loi, "Không Thể Thêm Mới");
                    }
                }
                else
                {
                    bool thanhCong = _khuGianService.CapNhat(kg, out string loi);
                    if (thanhCong)
                    {
                        _isDirty = false;
                        if (bdDirtyKg != null) bdDirtyKg.Visibility = Visibility.Collapsed;
                        ThongBaoDialog.ThanhCong("Cập nhật thông số phân đoạn khu gian thành công!", "Lưu Thành Công");
                        int savedId = _currentMaKg;
                        LoadData(savedId);
                    }
                    else
                    {
                        ThongBaoDialog.Loi(loi, "Không Thể Cập Nhật");
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi lưu phân đoạn khu gian:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void BtnXoaKg_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaKg <= 0) return;

            // Chặn xóa nếu phân đoạn đang có đoàn tàu hoạt động
            if (cboTrangThaiKg.SelectedIndex == 1)
            {
                ThongBaoDialog.CanhBao("Phân đoạn khu gian hiện đang có đoàn tàu hoạt động bên trong. Tuyệt đối không được xóa để đảm bảo an toàn chạy tàu.", "Vi Phạm Quy Chuẩn An Toàn");
                return;
            }

            try
            {
                string tenPhanDoan = txtTitleFormKg.Text;
                int soDn = _duongNgangService.LayTheoKhuGian(_currentMaKg)?.Rows.Count ?? 0;

                string noiDungXacNhan;
                if (soDn > 0)
                {
                    noiDungXacNhan = $"Bạn có chắc chắn muốn xóa phân đoạn khu gian [{tenPhanDoan}]?\n\nPhân đoạn này đang chứa {soDn} điểm giao cắt đường ngang liên kết. Hệ thống sẽ tự động dọn sạch các điểm giao cắt này đi kèm phân đoạn.";
                }
                else
                {
                    noiDungXacNhan = $"Bạn có chắc chắn muốn xóa phân đoạn khu gian [{tenPhanDoan}]?";
                }

                bool xacNhan = ThongBaoDialog.XacNhan(noiDungXacNhan, "Xác Nhận Xóa Phân Đoạn");

                if (xacNhan)
                {
                    bool thanhCong = _khuGianService.Xoa(_currentMaKg, out string loi, xoaDuongNgangKemTheo: true);
                    if (thanhCong)
                    {
                        _isDirty = false;
                        if (bdDirtyKg != null) bdDirtyKg.Visibility = Visibility.Collapsed;
                        ThongBaoDialog.ThanhCong("Đã xóa phân đoạn khu gian thành công.", "Đã Xóa");
                        LoadData();
                    }
                    else
                    {
                        ThongBaoDialog.Loi(loi, "Không Thể Xóa");
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi thực hiện xóa phân đoạn: {ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void BtnNhapFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profile = new KhuGianImportProfile();
                var dlg = new CommonImportDialog(profile)
                {
                    Owner = Window.GetWindow(this)
                };
                if (dlg.ShowDialog() == true)
                {
                    LoadDanhSachGa();
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể khởi động tiến trình nhập tệp khu gian:\n{ex.Message}", "Lỗi Nhập Tệp");
            }
        }

        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cachedTable == null || _cachedTable.Rows.Count == 0)
                {
                    ThongBaoDialog.CanhBao("Không có dữ liệu phân đoạn khu gian để xuất.", "Thông Báo");
                    return;
                }

                var colMap = new Dictionary<string, string>
                {
                    { "STT", "STT" },
                    { "MaKhuGian", "Mã Phân Đoạn" },
                    { "TenGaDau", "Ga Đầu" },
                    { "MaGaDauCode", "Mã Ga Đầu" },
                    { "TenGaCuoi", "Ga Cuối" },
                    { "MaGaCuoiCode", "Mã Ga Cuối" },
                    { "CuLyKm", "Cự Ly (km)" },
                    { "TocDoToiDaKhach", "Vmax Khách (km/h)" },
                    { "TocDoToiDaHang", "Vmax Hàng (km/h)" },
                    { "DoDocPermil", "Độ Dốc (‰)" },
                    { "CanDauMayDay", "Cần Máy Đẩy" },
                    { "TenTrangThai", "Trạng Thái" },
                    { "SoDuongNgang", "Số Đường Ngang" },
                    { "SoDiemDen", "Điểm Đen TNGT" }
                };

                FileExchangeHelper.XuatExcel(_cachedTable, colMap, "DanhSach_PhanDoan_KhuGian.xlsx", "BÁO CÁO DANH MỤC PHÂN ĐOẠN KHU GIAN ĐƯỜNG SẮT TOÀN TUYẾN", "PhanDoanKhuGian");
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi xuất tệp Excel khu gian:\n{ex.Message}", "Lỗi Xuất Tệp");
            }
        }

        private void FormInput_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingData) return;

            try
            {
                _isDirty = true;
                if (bdDirtyKg != null) bdDirtyKg.Visibility = Visibility.Visible;

                if (sender == cboTrangThaiKg && cboTrangThaiKg.SelectedItem is ComboBoxItem)
                {
                    string tt = cboTrangThaiKg.SelectedIndex switch
                    {
                        1 => "CO_TAU",
                        2 => "PHONG_TOA",
                        _ => "RONG"
                    };
                    CapNhatVisualTrangThaiRay(tt);
                }

                CapNhatTinhToanVanHanh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KhuGianPage.FormInput_Changed] Lỗi cập nhật trường nhập liệu: {ex.Message}");
            }
        }

        private void BtnLinkSangTabDn_Click(object sender, MouseButtonEventArgs e)
        {
            if (tabInspectorKg != null && tabDuongNgangKg != null && tabDuongNgangKg.IsEnabled)
            {
                tabInspectorKg.SelectedItem = tabDuongNgangKg;
            }
        }

        private void CapNhatTinhToanVanHanh()
        {
            if (txtEstTimeKhach == null || txtEstTimeHang == null) return;

            try
            {
                decimal cuLyKm = 0m;
                int vmaxK = 80;
                int vmaxH = 50;
                decimal doDoc = 0m;

                FormatHelper.TryParseKm(txtCuLyKg?.Text ?? "", out cuLyKm);
                int.TryParse(txtVmaxKhach?.Text ?? "80", out vmaxK);
                int.TryParse(txtVmaxHang?.Text ?? "50", out vmaxH);
                decimal.TryParse(txtDoDocKg?.Text ?? "0.0", out doDoc);

                if (vmaxK <= 0) vmaxK = 80;
                if (vmaxH <= 0) vmaxH = 50;

                // 1. Tính thời gian chạy tàu tối thiểu: t = (S / V) * 60 phút
                if (cuLyKm > 0)
                {
                    double phutK = ((double)cuLyKm / vmaxK) * 60.0;
                    int mK = (int)phutK;
                    int sK = (int)Math.Round((phutK - mK) * 60.0);
                    txtEstTimeKhach.Text = (sK > 0) ? $"{mK} phút {sK:D2} giây" : $"{mK} phút";
                    txtEstSpeedKhach.Text = $"Theo Vmax: {vmaxK} km/h";

                    double phutH = ((double)cuLyKm / vmaxH) * 60.0;
                    int mH = (int)phutH;
                    int sH = (int)Math.Round((phutH - mH) * 60.0);
                    txtEstTimeHang.Text = (sH > 0) ? $"{mH} phút {sH:D2} giây" : $"{mH} phút";
                    txtEstSpeedHang.Text = $"Theo Vmax: {vmaxH} km/h";
                }
                else
                {
                    txtEstTimeKhach.Text = "—";
                    txtEstTimeHang.Text = "—";
                }

                // 2. Đánh giá trắc dọc đèo dốc & sức kéo
                if (txtEstDocLevel != null && txtEstDocNotice != null)
                {
                    if (doDoc >= 15.0m || (chkCanDauMayDay?.IsChecked == true))
                    {
                        txtEstDocLevel.Text = $"Độ dốc {doDoc:0.0}‰ (Đèo dốc nguy hiểm)";
                        txtEstDocLevel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                        txtEstDocNotice.Text = "Bắt buộc máy đẩy Banker đuôi tàu";
                        txtEstDocNotice.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    }
                    else if (doDoc >= 10.0m)
                    {
                        txtEstDocLevel.Text = $"Độ dốc {doDoc:0.0}‰ (Dốc lớn)";
                        txtEstDocLevel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
                        txtEstDocNotice.Text = "Kiểm tra áp lực hãm gió đoàn tàu";
                        txtEstDocNotice.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                    }
                    else
                    {
                        txtEstDocLevel.Text = $"Độ dốc {doDoc:0.0}‰ (Bình thường)";
                        txtEstDocLevel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
                        txtEstDocNotice.Text = "Vận hành thông suốt ổn định";
                        txtEstDocNotice.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                    }
                }

                // 3. Tóm tắt hạ tầng đường ngang thực tế
                if (txtEstDuongNgangInfo != null && txtEstMatDoInfo != null)
                {
                    int countDn = _dtDuongNgang?.Rows.Count ?? 0;
                    int countDd = 0;
                    if (_dtDuongNgang != null)
                    {
                        foreach (DataRow r in _dtDuongNgang.Rows)
                        {
                            if (r.Table.Columns.Contains("LaDiemDen") && r["LaDiemDen"] != DBNull.Value && Convert.ToBoolean(r["LaDiemDen"]))
                                countDd++;
                        }
                    }

                    if (countDn > 0)
                    {
                        txtEstDuongNgangInfo.Text = (countDd > 0) ? $"{countDn} vị trí ([!] {countDd} điểm đen)" : $"{countDn} vị trí (An toàn)";
                        decimal matDo = (cuLyKm > 0) ? Math.Round((decimal)countDn / cuLyKm, 2) : 0m;
                        txtEstMatDoInfo.Text = $"Mật độ: {matDo:0.00} vị trí/km";
                    }
                    else
                    {
                        txtEstDuongNgangInfo.Text = "0 vị trí (Không có giao cắt)";
                        txtEstMatDoInfo.Text = "Mật độ: 0.00 vị trí/km";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KhuGianPage.CapNhatTinhToanVanHanh] Lỗi tính toán thông số vận hành: {ex.Message}");
            }
        }

        private void TabInspectorKg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (tabInspectorKg.SelectedIndex == 1 && _currentMaKg > 0)
                {
                    TaiDanhSachDuongNgang(_currentMaKg);
                }
            }
        }

        private void TaiDanhSachDuongNgang(int maKg)
        {
            pnlCardsDuongNgang.Children.Clear();
            if (cvsTrackMarkers != null) cvsTrackMarkers.Children.Clear();
            _currentMaDnEdit = 0;
            pnlFormDuongNgang.Visibility = Visibility.Collapsed;

            if (maKg <= 0)
            {
                txtTabBadgeDn.Text = "0";
                txtDnTongKetTab.Text = "Tổng số: 0 vị trí giao cắt";
                txtTrackDensity.Text = "Mật độ: 0.00 vị trí/km";
                pnlEmptyDn.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                _dtDuongNgang = _duongNgangService.LayTheoKhuGian(maKg);
                int total = _dtDuongNgang.Rows.Count;
                int diemDenCount = 0;

                txtTabBadgeDn.Text = total.ToString();

                decimal culyKg = _currentLyTrinhMax - _currentLyTrinhMin;
                if (culyKg > 0)
                {
                    decimal matDo = total / culyKg;
                    txtTrackDensity.Text = $"Mật độ: {matDo:0.00} vị trí/km";
                }
                else
                {
                    txtTrackDensity.Text = $"{total} vị trí";
                }

                if (total == 0)
                {
                    pnlEmptyDn.Visibility = Visibility.Visible;
                    txtDnTongKetTab.Text = "Tổng số: 0 vị trí giao cắt";
                    bdTabBadgeDn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    txtTabBadgeDn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                    return;
                }

                pnlEmptyDn.Visibility = Visibility.Collapsed;

                foreach (DataRow r in _dtDuongNgang.Rows)
                {
                    bool laDiemDen = r["LaDiemDen"] != DBNull.Value && Convert.ToBoolean(r["LaDiemDen"]);
                    if (laDiemDen) diemDenCount++;
                    var card = TaoTheDuongNgang(r);
                    pnlCardsDuongNgang.Children.Add(card);
                }

                VeSoDoTrackStrip();

                if (diemDenCount > 0)
                {
                    txtDnTongKetTab.Text = $"Tổng số: {total} vị trí ({diemDenCount} điểm đen nguy cơ)";
                    bdTabBadgeDn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    bdTabBadgeDn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
                    txtTabBadgeDn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                }
                else
                {
                    txtDnTongKetTab.Text = $"Tổng số: {total} vị trí giao cắt đường bộ";
                    bdTabBadgeDn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    bdTabBadgeDn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                    txtTabBadgeDn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                }

                CapNhatTinhToanVanHanh();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi("Không thể tải danh sách đường ngang: " + ex.Message, "Lỗi Tải Dữ Liệu");
            }
        }

        private void CvsTrackMarkers_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            VeSoDoTrackStrip();
        }

        private void VeSoDoTrackStrip()
        {
            if (cvsTrackMarkers == null || _dtDuongNgang == null) return;
            cvsTrackMarkers.Children.Clear();

            decimal range = _currentLyTrinhMax - _currentLyTrinhMin;
            if (range <= 0 || cvsTrackMarkers.ActualWidth <= 40) return;

            double width = cvsTrackMarkers.ActualWidth - 30; 
            double offsetLeft = 15;

            foreach (DataRow r in _dtDuongNgang.Rows)
            {
                int maDn = Convert.ToInt32(r["MaDuongNgang"]);
                decimal lyTrinh = Convert.ToDecimal(r["LyTrinhKm"]);
                string tenDuong = r["TenDuongBoGiaoCat"]?.ToString() ?? "";
                string loai = r["LoaiDuongNgang"]?.ToString() ?? "BIEN_BAO";
                bool laDiemDen = r["LaDiemDen"] != DBNull.Value && Convert.ToBoolean(r["LaDiemDen"]);

                double ratio = (double)((lyTrinh - _currentLyTrinhMin) / range);
                ratio = Math.Max(0.0, Math.Min(1.0, ratio));
                double x = offsetLeft + ratio * width;

                var marker = new Border
                {
                    Width = laDiemDen ? 14 : 12,
                    Height = laDiemDen ? 14 : 12,
                    CornerRadius = new CornerRadius(laDiemDen ? 2 : 6),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(laDiemDen ? "#B91C1C" : (loai == "CO_NGUOI_GAC" ? "#003B73" : "#2563EB"))),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    Cursor = Cursors.Hand,
                    ToolTip = $"[Km {FormatHelper.FormatKm(lyTrinh)}] {tenDuong}\n{(laDiemDen ? "[!] ĐIỂM ĐEN TAI NẠN • " : "")}{(loai == "CO_NGUOI_GAC" ? "Có người gác (24/7)" : loai == "TU_DONG" ? "Cảnh báo tự động" : "Biển báo dân sinh")}"
                };

                Canvas.SetLeft(marker, x - (marker.Width / 2.0));
                Canvas.SetTop(marker, (22 - marker.Height) / 2.0);
                cvsTrackMarkers.Children.Add(marker);
            }
        }

        private Border TaoTheDuongNgang(DataRow r)
        {
            int maDn = Convert.ToInt32(r["MaDuongNgang"]);
            decimal lyTrinh = Convert.ToDecimal(r["LyTrinhKm"]);
            string tenDuong = r["TenDuongBoGiaoCat"]?.ToString() ?? "";
            string loai = r["LoaiDuongNgang"]?.ToString() ?? "BIEN_BAO";
            bool laDiemDen = r["LaDiemDen"] != DBNull.Value && Convert.ToBoolean(r["LaDiemDen"]);

            string accentColor = laDiemDen ? "#B91C1C" : (loai switch
            {
                "CO_NGUOI_GAC" => "#003B73",
                "TU_DONG" => "#2563EB",
                _ => "#64748B"
            });

            var card = new Border
            {
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(4, 1, 1, 1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor)),
                Background = Brushes.White,
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 9)
            };

            card.MouseEnter += (s, e) =>
            {
                card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            };
            card.MouseLeave += (s, e) =>
            {
                card.Background = Brushes.White;
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 

            //  ĐỊNH DANH VỊ TRÍ & HÀNH ĐỘNG
            var rowTop = new Grid();
            rowTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pnlInfo = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Cột mốc Km đường sắt 
            var kmBadge = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BFDBFE")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            kmBadge.Child = new TextBlock
            {
                Text = $"Km {FormatHelper.FormatKm(lyTrinh)}",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"))
            };
            pnlInfo.Children.Add(kmBadge);

            // Tên tuyến đường bộ giao cắt
            var txtTen = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(tenDuong) ? "Đường dân sinh" : tenDuong,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = $"Tuyến đường bộ giao cắt: {tenDuong}"
            };
            pnlInfo.Children.Add(txtTen);

            Grid.SetColumn(pnlInfo, 0);
            rowTop.Children.Add(pnlInfo);

            // Nút thao tác Sửa / Xóa
            var actionsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var btnEdit = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(0, 0, 2, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Chỉnh sửa thông số điểm giao cắt này"
            };
            var iconEdit = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = SymbolRegular.Edit24,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"))
            };
            btnEdit.Content = iconEdit;
            btnEdit.MouseEnter += (s, e) => iconEdit.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
            btnEdit.MouseLeave += (s, e) => iconEdit.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            btnEdit.Click += (s, e) => MoFormSuaDuongNgang(r);
            actionsPanel.Children.Add(btnEdit);

            var btnDelete = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 2, 4, 2),
                Cursor = Cursors.Hand,
                ToolTip = "Xóa điểm giao cắt này"
            };
            var iconDel = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = SymbolRegular.Delete24,
                FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
            };
            btnDelete.Content = iconDel;
            btnDelete.MouseEnter += (s, e) => iconDel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            btnDelete.MouseLeave += (s, e) => iconDel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            btnDelete.Click += (s, e) => XoaDuongNgang(maDn, tenDuong, lyTrinh);
            actionsPanel.Children.Add(btnDelete);

            Grid.SetColumn(actionsPanel, 1);
            rowTop.Children.Add(actionsPanel);

            Grid.SetRow(rowTop, 0);
            grid.Children.Add(rowTop);

            //  THƯỚC ĐO 
            decimal distDau = Math.Abs(lyTrinh - _currentLyTrinhMin);
            decimal distCuoi = Math.Abs(_currentLyTrinhMax - lyTrinh);
            decimal culySegment = _currentLyTrinhMax - _currentLyTrinhMin;
            int pct = culySegment > 0 ? (int)Math.Round((distDau / culySegment) * 100) : 0;
            pct = Math.Max(0, Math.Min(100, pct));

            var pnlDistance = new StackPanel { Margin = new Thickness(0, 6, 0, 6) };

            string gaDauTen = string.IsNullOrWhiteSpace(_currentTenGaDau) ? "Ga Đầu" : _currentTenGaDau;
            string gaCuoiTen = string.IsNullOrWhiteSpace(_currentTenGaCuoi) ? "Ga Cuối" : _currentTenGaCuoi;
            var txtDistance = new TextBlock
            {
                Text = $"Cách {gaDauTen}: {FormatHelper.FormatKm(distDau)} km ({pct}%)  •  Cách {gaCuoiTen}: {FormatHelper.FormatKm(distCuoi)} km",
                FontSize = 9.5,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                Margin = new Thickness(0, 0, 0, 3)
            };
            pnlDistance.Children.Add(txtDistance);

            var barContainer = new Border
            {
                Height = 3,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                CornerRadius = new CornerRadius(1.5),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var barGrid = new Grid();
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(pct, GridUnitType.Star) });
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100 - pct, GridUnitType.Star) });

            var fillBar = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(laDiemDen ? "#B91C1C" : "#003B73")),
                CornerRadius = new CornerRadius(1.5)
            };
            Grid.SetColumn(fillBar, 0);
            barGrid.Children.Add(fillBar);
            barContainer.Child = barGrid;
            pnlDistance.Children.Add(barContainer);

            Grid.SetRow(pnlDistance, 1);
            grid.Children.Add(pnlDistance);

            // NHÃN HẠ TẦNG & CỜ CẢNH BÁO AN TOÀN
            var pnlTags = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var tagLoai = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(6, 2, 6, 2),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            string tenLoai = loai switch
            {
                "CO_NGUOI_GAC" => "Có người gác (Chắn 24/7)",
                "TU_DONG" => "Cảnh báo tự động (Chuông đèn & Cần chắn)",
                _ => "Biển báo dân sinh"
            };
            tagLoai.Child = new TextBlock
            {
                Text = tenLoai,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                VerticalAlignment = VerticalAlignment.Center
            };
            pnlTags.Children.Add(tagLoai);

            // 2. Cờ Điểm Đen (Nếu có)
            if (laDiemDen)
            {
                var tagDiemDen = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = "Vị trí có mật độ giao thông cao hoặc từng xảy ra sự cố va chạm"
                };
                var ctnDiemDen = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                ctnDiemDen.Children.Add(new Wpf.Ui.Controls.SymbolIcon
                {
                    Symbol = SymbolRegular.Warning20,
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
                    Margin = new Thickness(0, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                ctnDiemDen.Children.Add(new TextBlock
                {
                    Text = "ĐIỂM ĐEN TAI NẠN",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                    VerticalAlignment = VerticalAlignment.Center
                });
                tagDiemDen.Child = ctnDiemDen;
                pnlTags.Children.Add(tagDiemDen);
            }

            Grid.SetRow(pnlTags, 2);
            grid.Children.Add(pnlTags);

            card.Child = grid;
            return card;
        }

        private void BtnThemDuongNgang_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentMaKg <= 0)
                {
                    ThongBaoDialog.CanhBao("Vui lòng chọn một phân đoạn khu gian trước khi thêm đường ngang.", "Chưa Chọn Phân Đoạn");
                    return;
                }

                _currentMaDnEdit = 0;
                txtTitleFormDn.Text = "KHAI BÁO ĐƯỜNG NGANG GIAO CẮT";
                txtBadgeFormDn.Text = "TẠO MỚI";
                txtLuuDn.Text = "Lưu Vị Trí";
                iconFormDn.Symbol = SymbolRegular.AddCircle24;

                txtDnLyTrinh.Text = "";
                txtDnTenDuong.Text = "";
                cboDnLoai.SelectedIndex = 0;
                chkDnLaDiemDen.IsChecked = false;

                pnlFormDuongNgang.Visibility = Visibility.Visible;
                txtDnLyTrinh.Focus();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi mở biểu mẫu thêm đường ngang:\n{ex.Message}", "Lỗi Biểu Mẫu");
            }
        }

        private void MoFormSuaDuongNgang(DataRow r)
        {
            try
            {
                _currentMaDnEdit = Convert.ToInt32(r["MaDuongNgang"]);
                decimal lyTrinh = Convert.ToDecimal(r["LyTrinhKm"]);
                string tenDuong = r["TenDuongBoGiaoCat"]?.ToString() ?? "";
                string loai = r["LoaiDuongNgang"]?.ToString() ?? "BIEN_BAO";
                bool laDiemDen = r["LaDiemDen"] != DBNull.Value && Convert.ToBoolean(r["LaDiemDen"]);

                txtTitleFormDn.Text = $"CẬP NHẬT ĐƯỜNG NGANG #{_currentMaDnEdit}";
                txtBadgeFormDn.Text = $"SỬA #{_currentMaDnEdit}";
                txtLuuDn.Text = "Cập Nhật";
                iconFormDn.Symbol = SymbolRegular.Edit24;

                txtDnLyTrinh.Text = FormatHelper.FormatKm(lyTrinh);
                txtDnTenDuong.Text = tenDuong;

                cboDnLoai.SelectedIndex = loai switch
                {
                    "CO_NGUOI_GAC" => 0,
                    "TU_DONG" => 1,
                    _ => 2
                };

                chkDnLaDiemDen.IsChecked = laDiemDen;
                pnlFormDuongNgang.Visibility = Visibility.Visible;
                txtDnTenDuong.Focus();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi nạp thông tin đường ngang để sửa:\n{ex.Message}", "Lỗi Hiển Thị");
            }
        }

        private void BtnHuyDn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentMaDnEdit = 0;
                pnlFormDuongNgang.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BtnHuyDn_Click] Lỗi: {ex.Message}");
            }
        }

        private void BtnLuuDn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaKg <= 0) return;

            try
            {
                if (!FormatHelper.TryParseKm(txtDnLyTrinh.Text, out decimal lyTrinhKm) || lyTrinhKm < 0)
                {
                    ThongBaoDialog.CanhBao("Vui lòng nhập vị trí lý trình Km hợp lệ (ví dụ: 12.50).", "Lý Trình Không Hợp Lệ");
                    txtDnLyTrinh.Focus();
                    return;
                }

                string tenDuong = txtDnTenDuong.Text.Trim();
                if (string.IsNullOrWhiteSpace(tenDuong))
                {
                    ThongBaoDialog.CanhBao("Vui lòng nhập tên tuyến đường bộ giao cắt (ví dụ: QL1A, Tỉnh lộ 427, Đường dân sinh...).", "Thiếu Dữ Liệu");
                    txtDnTenDuong.Focus();
                    return;
                }

                string loai = "BIEN_BAO";
                if (cboDnLoai.SelectedItem is ComboBoxItem item && item.Tag is string tag)
                {
                    loai = tag;
                }

                bool laDiemDen = chkDnLaDiemDen.IsChecked == true;

                var dn = new DuongNgang
                {
                    MaDuongNgang = _currentMaDnEdit,
                    MaKhuGian = _currentMaKg,
                    LyTrinhKm = lyTrinhKm,
                    TenDuongBoGiaoCat = tenDuong,
                    LoaiDuongNgang = loai,
                    LaDiemDen = laDiemDen
                };

                if (_currentMaDnEdit <= 0)
                {
                    bool ok = _duongNgangService.Them(dn, _currentLyTrinhMin, _currentLyTrinhMax, out string loi);
                    if (ok)
                    {
                        ThongBaoDialog.ThanhCong("Khai báo điểm giao cắt đường ngang thành công!", "Thành Công");
                        pnlFormDuongNgang.Visibility = Visibility.Collapsed;
                        TaiDanhSachDuongNgang(_currentMaKg);
                        LamMoiDataGridKhongChonLai();
                    }
                    else
                    {
                        ThongBaoDialog.Loi(loi, "Không Thể Thêm Mới");
                    }
                }
                else
                {
                    bool ok = _duongNgangService.CapNhat(dn, _currentLyTrinhMin, _currentLyTrinhMax, out string loi);
                    if (ok)
                    {
                        ThongBaoDialog.ThanhCong("Cập nhật thông số đường ngang thành công!", "Lưu Thành Công");
                        pnlFormDuongNgang.Visibility = Visibility.Collapsed;
                        TaiDanhSachDuongNgang(_currentMaKg);
                        LamMoiDataGridKhongChonLai();
                    }
                    else
                    {
                        ThongBaoDialog.Loi(loi, "Không Thể Cập Nhật");
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi lưu điểm giao cắt đường ngang:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void XoaDuongNgang(int maDn, string tenDuong, decimal lyTrinh)
        {
            try
            {
                bool xacNhan = ThongBaoDialog.XacNhan(
                    $"Bạn có chắc chắn muốn xóa điểm giao cắt [{tenDuong}] tại vị trí Km {FormatHelper.FormatKm(lyTrinh)} khỏi phân đoạn này?",
                    "Xác Nhận Xóa Đường Ngang");

                if (xacNhan)
                {
                    bool ok = _duongNgangService.Xoa(maDn, out string loi);
                    if (ok)
                    {
                        ThongBaoDialog.ThanhCong("Đã xóa điểm giao cắt đường ngang thành công.", "Đã Xóa");
                        TaiDanhSachDuongNgang(_currentMaKg);
                        LamMoiDataGridKhongChonLai();
                    }
                    else
                    {
                        ThongBaoDialog.Loi(loi, "Không Thể Xóa");
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi xóa đường ngang:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void LamMoiDataGridKhongChonLai()
        {
            int currentSelectedId = _currentMaKg;
            _isLoadingData = true;
            try
            {
                _cachedTable = _khuGianService.LayDanhSach();
                if (_cachedTable != null)
                {
                    if (!_cachedTable.Columns.Contains("STT"))
                        _cachedTable.Columns.Add("STT", typeof(int));
                    for (int i = 0; i < _cachedTable.Rows.Count; i++)
                        _cachedTable.Rows[i]["STT"] = i + 1;
                }
                ApplyFilter();
                CapNhatThongKeTong();
                if (currentSelectedId > 0)
                    ChonKhuGianTheoId(currentSelectedId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LamMoiDataGridKhongChonLai] Lỗi làm mới bảng: {ex.Message}");
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void CapNhatVisualTrangThaiRay(string trangThai)
        {
            if (brdTrackMainLine == null || txtTrackDensity == null) return;

            switch (trangThai)
            {
                case "PHONG_TOA":
                    brdTrackMainLine.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    txtTrackDensity.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    txtTrackDensity.Text = "PHONG TỎA THI CÔNG / SỰ CỐ";
                    break;
                case "CO_TAU":
                    brdTrackMainLine.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                    txtTrackDensity.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                    txtTrackDensity.Text = "ĐOÀN TÀU ĐANG CHIẾM DỤNG";
                    break;
                default:
                    brdTrackMainLine.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                    txtTrackDensity.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                    decimal range = _currentLyTrinhMax - _currentLyTrinhMin;
                    int countDn = _dtDuongNgang?.Rows.Count ?? 0;
                    decimal density = (range > 0 && countDn > 0) ? Math.Round(countDn / range, 2) : 0m;
                    txtTrackDensity.Text = $"Mật độ: {density:0.00} vị trí/km";
                    break;
            }
        }

        private void CboGaDau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingData) return;
            _isDirty = true;

            if (_isAddingNew)
            {
                CapNhatGaCuoiLienKe();
            }
        }

   
        private void CapNhatGaCuoiLienKe()
        {
            cboGaCuoi.Items.Clear();
            pnlWarningGaLienKe.Visibility = Visibility.Collapsed;

            if (cboGaDau.SelectedItem is not ComboBoxItem itDau || itDau.Tag is not int maGaDau)
                return;

            try
            {
                var dtLienKe = _khuGianService.LayGaLienKeChuaCoKhuGian(maGaDau);

                if (dtLienKe == null || dtLienKe.Rows.Count == 0)
                {
                    // Ga này đã có đủ khu gian 2 phía hoặc là ga đầu/cuối đã có khu gian
                    pnlWarningGaLienKe.Visibility = Visibility.Visible;
                    txtWarningGaLienKe.Text = "Ga này đã có đủ khu gian ở cả 2 phía liền kề. Vui lòng chọn ga khác để tạo phân đoạn mới.";
                    return;
                }

                foreach (DataRow r in dtLienKe.Rows)
                {
                    int maGa = Convert.ToInt32(r["MaGa"]);
                    string code = r["MaGaCode"]?.ToString() ?? "";
                    string ten = r["TenGa"]?.ToString() ?? "";
                    decimal km = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m;
                    string huong = r["Huong"]?.ToString() ?? "";
                    string itemText = $"[{code}] {ten} (Km {FormatHelper.FormatKm(km)}) — {huong}";

                    cboGaCuoi.Items.Add(new ComboBoxItem { Content = itemText, Tag = maGa });
                }

                if (cboGaCuoi.Items.Count > 0)
                {
                    cboGaCuoi.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                pnlWarningGaLienKe.Visibility = Visibility.Visible;
                txtWarningGaLienKe.Text = "Lỗi khi truy vấn ga liền kề: " + ex.Message;
            }
        }

        private void CboGa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingData) return;
            _isDirty = true;
            TinhCuLyTuDong();
        }

        private void TinhCuLyTuDong()
        {
            if (cboGaDau.SelectedItem is ComboBoxItem itemDau && itemDau.Tag != null &&
                cboGaCuoi.SelectedItem is ComboBoxItem itemCuoi && itemCuoi.Tag != null)
            {
                int maDau = Convert.ToInt32(itemDau.Tag);
                int maCuoi = Convert.ToInt32(itemCuoi.Tag);

                if (maDau == maCuoi) return;

                decimal kmDau = TimLyTrinhGa(maDau);
                decimal kmCuoi = TimLyTrinhGa(maCuoi);

                decimal culy = Math.Abs(kmCuoi - kmDau);
                if (culy > 0)
                {
                    txtCuLyKg.Text = FormatHelper.FormatKm(culy);
                    txtBannerKhoangCachGa.Text = $"Khoảng cách lý trình: {FormatHelper.FormatKm(culy)} km (Tự động tính)";
                }
            }
        }

        private decimal TimLyTrinhGa(int maGa)
        {
            if (_dtGa != null)
            {
                foreach (DataRow r in _dtGa.Rows)
                {
                    if (Convert.ToInt32(r["MaGa"]) == maGa && r["LyTrinhKm"] != DBNull.Value)
                    {
                        return Convert.ToDecimal(r["LyTrinhKm"]);
                    }
                }
            }
            return 0m;
        }

        private void BtnTinhLaiCuLy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                decimal khoangCach = 0m;
                if (_isAddingNew)
                {
                    if (cboGaDau.SelectedItem is ComboBoxItem itemDau && itemDau.Tag != null &&
                        cboGaCuoi.SelectedItem is ComboBoxItem itemCuoi && itemCuoi.Tag != null)
                    {
                        int maDau = Convert.ToInt32(itemDau.Tag);
                        int maCuoi = Convert.ToInt32(itemCuoi.Tag);
                        if (maDau != maCuoi)
                        {
                            decimal kmDau = TimLyTrinhGa(maDau);
                            decimal kmCuoi = TimLyTrinhGa(maCuoi);
                            khoangCach = Math.Abs(kmCuoi - kmDau);
                        }
                    }
                }
                else
                {
                    khoangCach = Math.Abs(_currentLyTrinhMax - _currentLyTrinhMin);
                }

                if (khoangCach > 0)
                {
                    txtCuLyKg.Text = FormatHelper.FormatKm(khoangCach);
                    txtBannerKhoangCachGa.Text = $"Khoảng cách lý trình: {FormatHelper.FormatKm(khoangCach)} km (Đã đồng bộ)";
                    _isDirty = true;
                }
                else
                {
                    ThongBaoDialog.CanhBao("Không xác định được khoảng cách lý trình hợp lệ giữa hai ga để tính toán.", "Tính Cự Ly Lý Trình");
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi tính lại cự ly lý trình:\n{ex.Message}", "Lỗi Tính Toán");
            }
        }

        // Context menu

        private void CtxSua_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tabInspectorKg.SelectedItem = tabThongSoKg;
                txtCuLyKg.Focus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CtxSua_Click] Lỗi: {ex.Message}");
            }
        }

        private void CtxCopyPhanDoan_Click(object sender, RoutedEventArgs e)
        {
            if (dgKhuGian.SelectedItem is DataRowView drv)
            {
                string tenDau = drv["TenGaDau"]?.ToString() ?? "";
                string tenCuoi = drv["TenGaCuoi"]?.ToString() ?? "";
                decimal culy = drv["CuLyKm"] != DBNull.Value ? Convert.ToDecimal(drv["CuLyKm"]) : 0m;
                string text = $"{tenDau} ↔ {tenCuoi} ({FormatHelper.FormatKm(culy)} km)";
                try
                {
                    Clipboard.SetText(text);
                    ThongBaoDialog.ThanhCong($"Đã sao chép: {text}", "Sao Chép Tuyến");
                }
                catch
                {
                }
            }
        }

        private void CtxCopyRow_Click(object sender, RoutedEventArgs e)
        {
            if (dgKhuGian.SelectedItem is DataRowView drv)
            {
                try
                {
                    var r = drv.Row;
                    decimal culy = r["CuLyKm"] != DBNull.Value ? Convert.ToDecimal(r["CuLyKm"]) : 0m;
                    decimal doDoc = r["DoDocPermil"] != DBNull.Value ? Convert.ToDecimal(r["DoDocPermil"]) : 0m;
                    string line = $"{r["TenGaDau"]}\t{r["TenGaCuoi"]}\t{FormatHelper.FormatKm(culy)}\t{r["TocDoToiDaKhach"]}\t{r["TocDoToiDaHang"]}\t{doDoc:0.0}\t{r["TenTrangThai"]}";
                    Clipboard.SetText(line);
                    ThongBaoDialog.ThanhCong("Đã sao chép toàn bộ dòng phân đoạn (định dạng Tab-separated để dán trực tiếp vào Excel).", "Sao Chép Bảng");
                }
                catch
                {
                }
            }
        }

        private void CtxDoiTrangThai_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgKhuGian.SelectedItem is DataRowView drv)
                {
                    int maKg = Convert.ToInt32(drv["MaKhuGian"]);
                    string tenGaDau = drv["TenGaDau"]?.ToString() ?? "";
                    string tenGaCuoi = drv["TenGaCuoi"]?.ToString() ?? "";
                    string trangThaiHienTai = drv["TrangThai"]?.ToString() ?? "RONG";

                    if (trangThaiHienTai == "CO_TAU")
                    {
                        ThongBaoDialog.CanhBao(
                            $"Phân đoạn [{tenGaDau} → {tenGaCuoi}] hiện đang có đoàn tàu chiếm dụng ray.\n\nKhông thể thực hiện phong tỏa hoặc can thiệp kỹ thuật cho đến khi đoàn tàu trả đường an toàn.",
                            "Cấm Can Thiệp An Toàn");
                        return;
                    }

                    string trangThaiMoi = (trangThaiHienTai == "PHONG_TOA") ? "RONG" : "PHONG_TOA";
                    string tenMoi = (trangThaiMoi == "PHONG_TOA") ? "Phong tỏa thi công / Sự cố" : "Thông đường sẵn sàng";

                    string thongDiep = (trangThaiMoi == "PHONG_TOA")
                        ? $"Bạn có muốn PHONG TỎA phân đoạn khu gian [{tenGaDau} → {tenGaCuoi}]?\n(Hệ thống sẽ gắn cờ cảnh báo an toàn trên toàn mạng lưới)"
                        : $"Bạn có muốn MỞ ĐƯỜNG / THÔNG TUYẾN trở lại cho phân đoạn [{tenGaDau} → {tenGaCuoi}]?";

                    if (ThongBaoDialog.XacNhan(thongDiep, "Đổi Trạng Thái Khai Thác"))
                    {
                        var kg = new KhuGian
                        {
                            MaKhuGian = maKg,
                            MaGaDau = Convert.ToInt32(drv["MaGaDau"]),
                            MaGaCuoi = Convert.ToInt32(drv["MaGaCuoi"]),
                            CuLyKm = Convert.ToDecimal(drv["CuLyKm"]),
                            TocDoToiDaKhach = Convert.ToInt32(drv["TocDoToiDaKhach"]),
                            TocDoToiDaHang = Convert.ToInt32(drv["TocDoToiDaHang"]),
                            DoDocPermil = Convert.ToDecimal(drv["DoDocPermil"]),
                            CanDauMayDay = Convert.ToBoolean(drv["CanDauMayDay"]),
                            TrangThai = trangThaiMoi
                        };

                        if (_khuGianService.CapNhat(kg, out string err))
                        {
                            ThongBaoDialog.ThanhCong($"Đã cập nhật trạng thái phân đoạn sang: {tenMoi}", "Thành Công");
                            LoadData(maKg);
                        }
                        else
                        {
                            ThongBaoDialog.Loi(err, "Lỗi Cập Nhật");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi đổi trạng thái phân đoạn:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void CtxThemDuongNgang_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgKhuGian.SelectedItem is DataRowView)
                {
                    tabInspectorKg.SelectedItem = tabDuongNgangKg;
                    BtnThemDuongNgang_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CtxThemDuongNgang_Click] Lỗi: {ex.Message}");
            }
        }

        private void CtxLamMoi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isDirty = false;
                LoadData();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi làm mới: {ex.Message}", "Lỗi Làm Mới");
            }
        }

        private void CtxXoa_Click(object sender, RoutedEventArgs e)
        {
            BtnXoaKg_Click(sender, e);
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
                {
                    e.Handled = true;
                    BtnLuuKg_Click(btnLuuKg, new RoutedEventArgs());
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
                {
                    e.Handled = true;
                    BtnThemMoi_Click(btnLuuKg, new RoutedEventArgs());
                }
                else if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    BtnHuyKg_Click(btnHuyKg, new RoutedEventArgs());
                }
                else if (e.Key == Key.F5)
                {
                    e.Handled = true;
                    BtnLamMoi_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KhuGianPage.Page_PreviewKeyDown] Lỗi phím tắt: {ex.Message}");
            }
        }

       
         public void FocusTimKiem()
        {
            if (txtTimKiem != null)
            {
                txtTimKiem.Focus();
                txtTimKiem.SelectAll();
            }
        }

        public void KichHoatThemMoi() => BtnThemMoi_Click(this, new RoutedEventArgs());
        public void KichHoatNapLai() => BtnLamMoi_Click(this, new RoutedEventArgs());
        public void KichHoatNhapTep() => BtnNhapFile_Click(this, new RoutedEventArgs());
        public void KichHoatXuatTep() => BtnXuatExcel_Click(this, new RoutedEventArgs());
        public void KichHoatBaoCao()
        {
        }

        public void KichHoatHuy()
        {
            if (!string.IsNullOrEmpty(txtTimKiem.Text))
            {
                txtTimKiem.Text = "";
            }
            else
            {
                BtnHuyKg_Click(this, new RoutedEventArgs());
            }
        }

        private void BtnViewBang_Click(object sender, RoutedEventArgs e)
        {
            _currentViewMode = "BANG";
            CapNhatGiaoDienViewMode();
        }

        private void BtnViewSoDo_Click(object sender, RoutedEventArgs e)
        {
            _currentViewMode = "SODO";
            CapNhatGiaoDienViewMode();
        }

        private void CapNhatGiaoDienViewMode()
        {
            if (_currentViewMode == "BANG")
            {
                btnViewBang.Background = Brushes.White;
                txtViewBang.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                txtViewBang.FontWeight = FontWeights.Bold;
                if (iconViewBang != null) iconViewBang.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));

                btnViewSoDo.Background = Brushes.Transparent;
                txtViewSoDo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                txtViewSoDo.FontWeight = FontWeights.Normal;
                if (iconViewSoDo != null) iconViewSoDo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));

                dgKhuGian.Visibility = Visibility.Visible;
                pnlSoDoTuyen.Visibility = Visibility.Collapsed;
                ApplyFilter();
            }
            else
            {
                btnViewSoDo.Background = Brushes.White;
                txtViewSoDo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                txtViewSoDo.FontWeight = FontWeights.Bold;
                if (iconViewSoDo != null) iconViewSoDo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));

                btnViewBang.Background = Brushes.Transparent;
                txtViewBang.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                txtViewBang.FontWeight = FontWeights.Normal;
                if (iconViewBang != null) iconViewBang.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));

                dgKhuGian.Visibility = Visibility.Collapsed;
                pnlEmptyState.Visibility = Visibility.Collapsed;
                pnlSoDoTuyen.Visibility = Visibility.Visible;
                VeSoDoTuyen();
            }
        }

        private void RadSoDoLoc_Checked(object sender, RoutedEventArgs e)
        {
            if (_isLoadingData) return;
            VeSoDoTuyen();
        }

        // Tìm bản ghi phân đoạn khu gian giữa 2 mã ga .
        private DataRow? TimKhuGianGiuaHaiGa(int maGa1, int maGa2)
        {
            if (_cachedTable == null) return null;
            foreach (DataRow r in _cachedTable.Rows)
            {
                int dau = Convert.ToInt32(r["MaGaDau"]);
                int cuoi = Convert.ToInt32(r["MaGaCuoi"]);
                if ((dau == maGa1 && cuoi == maGa2) || (dau == maGa2 && cuoi == maGa1))
                    return r;
            }
            return null;
        }

        // Dựng toàn bộ trục đường ray và các ga, phân đoạn liền kề theo chiều Bắc - Nam.
        public void VeSoDoTuyen()
        {
            if (pnlTrackCanvas == null) return;
            pnlTrackCanvas.Children.Clear();

            try
            {

            if (_dtGa == null || _dtGa.Rows.Count == 0)
            {
                pnlTrackCanvas.Children.Add(new TextBlock
                {
                    Text = "Chưa có dữ liệu danh mục ga để thiết lập sơ đồ tuyến.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                });
                return;
            }

            var dsGa = _dtGa.AsEnumerable()
                .OrderBy(r => r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m)
                .ToList();

            if (dsGa.Count < 2)
            {
                pnlTrackCanvas.Children.Add(new TextBlock
                {
                    Text = "Cần ít nhất 2 ga để hình thành sơ đồ phân đoạn khu gian.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                });
                return;
            }

            // Lấy tiêu chí tìm kiếm và bộ lọc hiện tại
            string keyword = txtTimKiem != null ? txtTimKiem.Text.Trim() : "";
            string normKeyword = !string.IsNullOrEmpty(keyword) ? SearchableComboBox.RemoveDiacritics(keyword).ToLowerInvariant() : "";

            string filterStatus = "";
            if (cboLocTrangThai != null)
            {
                filterStatus = cboLocTrangThai.SelectedIndex switch
                {
                    1 => "RONG",
                    2 => "CO_TAU",
                    3 => "PHONG_TOA",
                    _ => ""
                };
            }

            bool chiLocDiemDen = chkLocDiemDen != null && chkLocDiemDen.IsChecked == true;
            bool chiHienThiDoanThieu = radSoDoChiDoanThieu != null && radSoDoChiDoanThieu.IsChecked == true;

            int soDoanDaCo = 0;
            int soDoanThieu = 0;

            // Đếm thống kê tổng trước
            for (int i = 0; i < dsGa.Count - 1; i++)
            {
                int maGaA = Convert.ToInt32(dsGa[i]["MaGa"]);
                int maGaB = Convert.ToInt32(dsGa[i + 1]["MaGa"]);
                var kgRow = TimKhuGianGiuaHaiGa(maGaA, maGaB);
                if (kgRow != null) soDoanDaCo++;
                else soDoanThieu++;
            }

            if (txtBadgeDoanThieu != null)
            {
                txtBadgeDoanThieu.Text = soDoanThieu.ToString();
                bdBadgeDoanThieu.Visibility = soDoanThieu > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            int soDoanHienThi = 0;
            int lastDrawnMaGa = -1;

            // Tiến hành dựng từng cặp ga liền kề có áp dụng bộ lọc & tìm kiếm
            for (int i = 0; i < dsGa.Count - 1; i++)
            {
                var gaA = dsGa[i];
                var gaB = dsGa[i + 1];

                int maGaA = Convert.ToInt32(gaA["MaGa"]);
                int maGaB = Convert.ToInt32(gaB["MaGa"]);
                string codeA = gaA["MaGaCode"]?.ToString() ?? "";
                string codeB = gaB["MaGaCode"]?.ToString() ?? "";
                string tenA = gaA["TenGa"]?.ToString() ?? "";
                string tenB = gaB["TenGa"]?.ToString() ?? "";
                decimal kmA = gaA["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(gaA["LyTrinhKm"]) : 0m;
                decimal kmB = gaB["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(gaB["LyTrinhKm"]) : 0m;
                decimal khoangCachKm = Math.Abs(kmB - kmA);

                var kgRow = TimKhuGianGiuaHaiGa(maGaA, maGaB);
                bool daKetNoi = kgRow != null;

                // 1. Lọc theo RadioButton "Chưa kết nối"
                if (chiHienThiDoanThieu && daKetNoi) continue;

                // 2. Lọc theo Trạng thái khu gian
                if (!string.IsNullOrEmpty(filterStatus))
                {
                    if (!daKetNoi) continue;
                    string trangThai = kgRow!["TrangThai"]?.ToString() ?? "RONG";
                    if (trangThai != filterStatus) continue;
                }

                // 3. Lọc theo Điểm đen tai nạn
                if (chiLocDiemDen)
                {
                    if (!daKetNoi) continue;
                    int soDd = kgRow!.Table.Columns.Contains("SoDiemDen") && kgRow["SoDiemDen"] != DBNull.Value ? Convert.ToInt32(kgRow["SoDiemDen"]) : 0;
                    if (soDd <= 0) continue;
                }

                // 4. Lọc theo Từ khóa tìm kiếm (hỗ trợ gõ tiếng Việt có dấu và không dấu)
                if (!string.IsNullOrEmpty(normKeyword))
                {
                    string normTenA = SearchableComboBox.RemoveDiacritics(tenA).ToLowerInvariant();
                    string normCodeA = codeA.ToLowerInvariant();
                    string normTenB = SearchableComboBox.RemoveDiacritics(tenB).ToLowerInvariant();
                    string normCodeB = codeB.ToLowerInvariant();
                    string normPhanDoan = $"{normTenA} {normTenB}";

                    bool match = normTenA.Contains(normKeyword) 
                              || normCodeA.Contains(normKeyword)
                              || normTenB.Contains(normKeyword)
                              || normCodeB.Contains(normKeyword)
                              || normPhanDoan.Contains(normKeyword);

                    if (!match && daKetNoi && kgRow!["MaKhuGian"] != DBNull.Value && kgRow["MaKhuGian"].ToString() == keyword)
                    {
                        match = true;
                    }

                    if (!match) continue;
                }

                soDoanHienThi++;

                if (lastDrawnMaGa != -1 && maGaA != lastDrawnMaGa)
                {
                    pnlTrackCanvas.Children.Add(TaoVachNganCachKetQuaLocUI());
                }

                if (maGaA != lastDrawnMaGa)
                {
                    pnlTrackCanvas.Children.Add(TaoNodeGaUI(gaA));
                }

                pnlTrackCanvas.Children.Add(TaoDoanRayUI(maGaA, codeA, tenA, kmA, maGaB, codeB, tenB, kmB, khoangCachKm, kgRow));

                pnlTrackCanvas.Children.Add(TaoNodeGaUI(gaB));

                lastDrawnMaGa = maGaB;
            }

            // Trường hợp không có phân đoạn nào khớp bộ lọc
            if (soDoanHienThi == 0)
            {
                pnlTrackCanvas.Children.Add(TaoEmptyStateLocUI());
            }
            }
            catch (Exception ex)
            {
                pnlTrackCanvas.Children.Add(new TextBlock
                {
                    Text = $"Có sự cố khi hiển thị sơ đồ tuyến: {ex.Message}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                    Margin = new Thickness(16)
                });
            }
        }

        /// Tạo chỉ báo ngắt quãng khi các phân đoạn trung gian.
        private FrameworkElement TaoVachNganCachKetQuaLocUI()
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var spDots = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            for (int d = 0; d < 3; d++)
            {
                spDots.Children.Add(new Ellipse
                {
                    Width = 3,
                    Height = 3,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    Margin = new Thickness(0, 1.5, 0, 1.5)
                });
            }
            Grid.SetColumn(spDots, 0);
            grid.Children.Add(spDots);

            var bdNotice = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(10, 3, 10, 3),
                Margin = new Thickness(8, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            bdNotice.Child = new TextBlock
            {
                Text = "Các phân đoạn trung gian không khớp điều kiện lọc đã được ẩn",
                FontSize = 10,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"))
            };
            Grid.SetColumn(bdNotice, 1);
            grid.Children.Add(bdNotice);

            return grid;
        }

        // Tạo giao diện khi bộ lọc không có kết quả trên sơ đồ tuyến.
        private FrameworkElement TaoEmptyStateLocUI()
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(24, 36, 24, 36),
                Margin = new Thickness(0, 20, 0, 20)
            };

            var spEmpty = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconEmpty = new SymbolIcon
            {
                Symbol = SymbolRegular.Search24,
                FontSize = 32,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            spEmpty.Children.Add(iconEmpty);

            var txtEmpty = new TextBlock
            {
                Text = "Không tìm thấy phân đoạn khu gian nào khớp với điều kiện tìm kiếm hoặc lọc",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            spEmpty.Children.Add(txtEmpty);

            var txtHint = new TextBlock
            {
                Text = "Vui lòng kiểm tra lại từ khóa hoặc xóa bớt tiêu chí lọc để xem toàn bộ sơ đồ.",
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14)
            };
            spEmpty.Children.Add(txtHint);

            var btnClear = new Button
            {
                Content = "Xóa điều kiện lọc",
                Style = (Style)FindResource("DesktopButton"),
                Height = 28,
                Padding = new Thickness(16, 0, 16, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Cursor = Cursors.Hand
            };
            btnClear.Click += (s, e) =>
            {
                BtnDatLaiLoc_Click(s, e);
            };
            spEmpty.Children.Add(btnClear);

            border.Child = spEmpty;
            return border;
        }

        private FrameworkElement TaoNodeGaUI(DataRow ga)
        {
            string codeGa = ga["MaGaCode"]?.ToString() ?? "";
            string tenGa = ga["TenGa"]?.ToString() ?? "";
            decimal lyTrinhKm = ga["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(ga["LyTrinhKm"]) : 0m;
            string tinhThanh = ga.Table.Columns.Contains("TinhThanh") ? ga["TinhThanh"]?.ToString() ?? "" : "";
            string hangGa = ga.Table.Columns.Contains("HangGa") ? ga["HangGa"]?.ToString() ?? "" : "";
            int soDuongRay = ga.Table.Columns.Contains("SoDuongRay") && ga["SoDuongRay"] != DBNull.Value ? Convert.ToInt32(ga["SoDuongRay"]) : 0;

            var grid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var dotBorder = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                BorderThickness = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var innerDot = new Border
            {
                Width = 6,
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            dotBorder.Child = innerDot;
            Grid.SetColumn(dotBorder, 0);
            grid.Children.Add(dotBorder);

            var spInfo = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };

            // Badge mã code ga
            var bdCode = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BFDBFE")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(0, 0, 8, 0)
            };
            bdCode.Child = new TextBlock
            {
                Text = codeGa,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"))
            };
            spInfo.Children.Add(bdCode);

            // Tên ga
            spInfo.Children.Add(new TextBlock
            {
                Text = tenGa,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });

            // Lý trình Km
            var bdKm = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6, 1, 6, 1),
                Margin = new Thickness(0, 0, 8, 0)
            };
            bdKm.Child = new TextBlock
            {
                Text = $"Km {FormatHelper.FormatKm(lyTrinhKm)}",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"))
            };
            spInfo.Children.Add(bdKm);

            // Các thông số địa phận và quy mô ga nối tiếp nhau
            if (!string.IsNullOrWhiteSpace(tinhThanh))
            {
                spInfo.Children.Add(new TextBlock
                {
                    Text = $"•  {tinhThanh}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                });
            }

            if (!string.IsNullOrWhiteSpace(hangGa))
            {
                spInfo.Children.Add(new TextBlock
                {
                    Text = $"•  Ga {hangGa}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                });
            }

            if (soDuongRay > 0)
            {
                spInfo.Children.Add(new TextBlock
                {
                    Text = $"•  {soDuongRay} đường đón gửi",
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            Grid.SetColumn(spInfo, 1);
            grid.Children.Add(spInfo);

            return grid;
        }

        private FrameworkElement TaoDoanRayUI(int maGaA, string codeA, string tenA, decimal kmA,
                                              int maGaB, string codeB, string tenB, decimal kmB,
                                              decimal khoangCachKm, DataRow? kgRow)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            bool daKetNoi = kgRow != null;

            if (daKetNoi)
            {
                int maKg = Convert.ToInt32(kgRow!["MaKhuGian"]);
                decimal cuLy = kgRow["CuLyKm"] != DBNull.Value ? Convert.ToDecimal(kgRow["CuLyKm"]) : 0m;
                int vmaxKhach = kgRow["TocDoToiDaKhach"] != DBNull.Value ? Convert.ToInt32(kgRow["TocDoToiDaKhach"]) : 80;
                int vmaxHang = kgRow["TocDoToiDaHang"] != DBNull.Value ? Convert.ToInt32(kgRow["TocDoToiDaHang"]) : 50;
                decimal doDoc = kgRow["DoDocPermil"] != DBNull.Value ? Convert.ToDecimal(kgRow["DoDocPermil"]) : 0m;
                string trangThai = kgRow["TrangThai"]?.ToString() ?? "RONG";
                int soDn = kgRow.Table.Columns.Contains("SoDuongNgang") && kgRow["SoDuongNgang"] != DBNull.Value ? Convert.ToInt32(kgRow["SoDuongNgang"]) : 0;
                int soDd = kgRow.Table.Columns.Contains("SoDiemDen") && kgRow["SoDiemDen"] != DBNull.Value ? Convert.ToInt32(kgRow["SoDiemDen"]) : 0;
                bool canMayDay = kgRow["CanDauMayDay"] != DBNull.Value && Convert.ToBoolean(kgRow["CanDauMayDay"]);

                var railGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Center, Width = 14, Margin = new Thickness(0, -2, 0, -2) };
                railGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
                railGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                railGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });

                Color railColor = trangThai switch
                {
                    "CO_TAU" => (Color)ColorConverter.ConvertFromString("#F59E0B"),
                    "PHONG_TOA" => (Color)ColorConverter.ConvertFromString("#EF4444"),
                    _ => (Color)ColorConverter.ConvertFromString("#2563EB")
                };

                var railLeft = new Rectangle { Fill = new SolidColorBrush(railColor) };
                var railRight = new Rectangle { Fill = new SolidColorBrush(railColor) };
                Grid.SetColumn(railLeft, 0);
                Grid.SetColumn(railRight, 2);
                railGrid.Children.Add(railLeft);
                railGrid.Children.Add(railRight);

                Grid.SetColumn(railGrid, 0);
                grid.Children.Add(railGrid);

                bool isSelected = (maKg == _currentMaKg);
                var card = new Border
                {
                    Background = isSelected ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")) : Brushes.White,
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#003B73" : "#E2E8F0")),
                    BorderThickness = new Thickness(isSelected ? 2 : 1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(14, 8, 14, 8),
                    Margin = new Thickness(8, 4, 0, 4),
                    Cursor = Cursors.Hand
                };

                card.MouseEnter += (s, e) =>
                {
                    if (maKg != _currentMaKg)
                    {
                        card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
                        card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                    }
                };
                card.MouseLeave += (s, e) =>
                {
                    if (maKg != _currentMaKg)
                    {
                        card.Background = Brushes.White;
                        card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                    }
                };

                card.MouseLeftButtonUp += (s, e) =>
                {
                    ChonKhuGianTheoId(maKg);
                    VeSoDoTuyen();
                };

                var cardGrid = new Grid();
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.2, GridUnitType.Star) }); // Cột 0: Phân đoạn & Cự ly
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) }); // Cột 1: Tốc độ Vmax
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) }); // Cột 2: Độ dốc & Lực kéo
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.0, GridUnitType.Star) }); // Cột 3: Hạ tầng đường ngang
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.6, GridUnitType.Star) }); // Cột 4: Trạng thái & Thao tác

                var col0 = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                col0.Children.Add(new TextBlock
                {
                    Text = $"{tenA} — {tenB}",
                    FontSize = 12.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                    Margin = new Thickness(0, 0, 0, 3)
                });

                var spCuLyBar = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                spCuLyBar.Children.Add(new TextBlock
                {
                    Text = $"{FormatHelper.FormatKm(cuLy)} km",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                    Margin = new Thickness(0, 0, 8, 0)
                });

                double barWidth = Math.Clamp(15.0 + (double)cuLy * 0.8, 18.0, 140.0);
                Color barColor = (vmaxKhach >= 80) ? (Color)ColorConverter.ConvertFromString("#2563EB")
                               : (vmaxKhach >= 60) ? (Color)ColorConverter.ConvertFromString("#D97706")
                               : (Color)ColorConverter.ConvertFromString("#DC2626");
                spCuLyBar.Children.Add(new Border
                {
                    Width = barWidth,
                    Height = 5,
                    CornerRadius = new CornerRadius(2),
                    Background = new SolidColorBrush(barColor),
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = $"Cự ly thực tế: {FormatHelper.FormatKm(cuLy)} km | Tốc độ Vmax: {vmaxKhach} km/h (Mã màu thang tốc độ)"
                });
                spCuLyBar.Children.Add(new TextBlock
                {
                    Text = $"  •  CHẶNG #{maKg:D2}",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                    VerticalAlignment = VerticalAlignment.Center
                });
                col0.Children.Add(spCuLyBar);
                Grid.SetColumn(col0, 0);
                cardGrid.Children.Add(col0);

                var col1 = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
                col1.Children.Add(new TextBlock
                {
                    Text = $"Khách: {vmaxKhach} km/h",
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                    Margin = new Thickness(0, 0, 0, 2)
                });
                col1.Children.Add(new TextBlock
                {
                    Text = $"Hàng: {vmaxHang} km/h",
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"))
                });
                Grid.SetColumn(col1, 1);
                cardGrid.Children.Add(col1);

                var col2 = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
                col2.Children.Add(new TextBlock
                {
                    Text = $"Độ dốc: {doDoc:0.0}‰",
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                    Margin = new Thickness(0, 0, 0, 2)
                });
                col2.Children.Add(new TextBlock
                {
                    Text = canMayDay ? "● Bắt buộc máy đẩy" : "Máy đẩy: Không",
                    FontSize = 11,
                    FontWeight = canMayDay ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(canMayDay ? "#B45309" : "#64748B"))
                });
                Grid.SetColumn(col2, 2);
                cardGrid.Children.Add(col2);

                var col3 = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
                col3.Children.Add(new TextBlock
                {
                    Text = $"{soDn} đường ngang giao cắt",
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                    Margin = new Thickness(0, 0, 0, 2)
                });

                if (soDd > 0)
                {
                    var spDd = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    spDd.Children.Add(new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
                        Margin = new Thickness(0, 0, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    spDd.Children.Add(new TextBlock
                    {
                        Text = $"{soDd} điểm đen TNGT",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    col3.Children.Add(spDd);
                }
                else
                {
                    col3.Children.Add(new TextBlock
                    {
                        Text = "An toàn: 0 điểm đen",
                        FontSize = 11,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"))
                    });
                }
                Grid.SetColumn(col3, 3);
                cardGrid.Children.Add(col3);

                string textTt = trangThai switch
                {
                    "CO_TAU" => "Đang có tàu",
                    "PHONG_TOA" => "Phong tỏa",
                    _ => "Thông đường"
                };
                string bgTt = trangThai switch
                {
                    "CO_TAU" => "#FEF3C7",
                    "PHONG_TOA" => "#FEE2E2",
                    _ => "#F1F5F9"
                };
                string borderTt = trangThai switch
                {
                    "CO_TAU" => "#FDE68A",
                    "PHONG_TOA" => "#FECACA",
                    _ => "#CBD5E1"
                };
                string fgTt = trangThai switch
                {
                    "CO_TAU" => "#B45309",
                    "PHONG_TOA" => "#DC2626",
                    _ => "#475569"
                };

                var spTt = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var bdTt = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgTt)),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderTt)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(10, 4, 10, 4),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                bdTt.Child = new TextBlock
                {
                    Text = textTt,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgTt))
                };
                spTt.Children.Add(bdTt);

                Grid.SetColumn(spTt, 4);
                cardGrid.Children.Add(spTt);

                card.Child = cardGrid;
                Grid.SetColumn(card, 1);
                grid.Children.Add(card);
            }
            else
            {
                grid.MinHeight = 44;

                var dashLine = new Rectangle
                {
                    Width = 2,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                    StrokeDashArray = new DoubleCollection { 4, 3 },
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(dashLine, 0);
                grid.Children.Add(dashLine);

                var emptyCard = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFAFA")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(16, 8, 16, 8),
                    Margin = new Thickness(8, 4, 0, 4),
                    Cursor = Cursors.Hand
                };

                var spCenter = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var btnPlusCircle = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var iconPlusCenter = new SymbolIcon
                {
                    Symbol = SymbolRegular.Add24,
                    FontSize = 12,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                btnPlusCircle.Child = iconPlusCenter;
                spCenter.Children.Add(btnPlusCircle);

                var txtCenterInfo = new TextBlock
                {
                    Text = $"Thiết lập phân đoạn khu gian giữa {tenA} và {tenB}  (Cự ly ước tính: {FormatHelper.FormatKm(khoangCachKm)} km)",
                    FontSize = 11.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                    VerticalAlignment = VerticalAlignment.Center
                };
                spCenter.Children.Add(txtCenterInfo);

                emptyCard.Child = spCenter;

                emptyCard.MouseEnter += (s, e) =>
                {
                    emptyCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"));
                    emptyCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
                    btnPlusCircle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
                    txtCenterInfo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D4ED8"));
                };
                emptyCard.MouseLeave += (s, e) =>
                {
                    emptyCard.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFAFA"));
                    emptyCard.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                    btnPlusCircle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                    txtCenterInfo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                };

                emptyCard.MouseLeftButtonUp += (s, e) =>
                {
                    KichHoatThemMoiGiuaHaiGa(maGaA, maGaB, khoangCachKm);
                };

                Grid.SetColumn(emptyCard, 1);
                grid.Children.Add(emptyCard);
            }

            return grid;
        }

       
        private void KichHoatThemMoiGiuaHaiGa(int maGaA, int maGaB, decimal cuLyUocTinh)
        {
            try
            {
                // 1. Chuyển sang form tạo mới
                ClearForm(isNew: true);

                // 2. Tự động chọn Ga Đầu
                ChonComboTheoTag(cboGaDau, maGaA);

                // 3. Nạp danh sách ga liền kề cho Ga Cuối
                CapNhatGaCuoiLienKe();

                // 4. Tự động chọn Ga Cuối
                ChonComboTheoTag(cboGaCuoi, maGaB);

                // 5. Gán cự ly ước tính theo khoảng cách lý trình
                if (cuLyUocTinh > 0)
                {
                    txtCuLyKg.Text = FormatHelper.FormatKm(cuLyUocTinh);
                    txtBannerKhoangCachGa.Text = $"Khoảng cách lý trình: {FormatHelper.FormatKm(cuLyUocTinh)} km (Tự động tính)";
                }

                // 6. Đưa Inspector về Tab 1 và focus vào ô Tốc độ tàu khách
                tabInspectorKg.SelectedIndex = 0;
                txtVmaxKhach.Focus();
                txtVmaxKhach.SelectAll();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi kích hoạt biểu mẫu tạo phân đoạn:\n{ex.Message}", "Lỗi Biểu Mẫu");
            }
        }
    }
}
