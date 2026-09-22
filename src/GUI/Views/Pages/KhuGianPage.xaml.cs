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
using Microsoft.Win32;
using BUS.Services;
using DTO.Common;
using ET.HaTang;
using GUI.Helpers;
using GUI.Profiles;
using GUI.Views.Dialogs;
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

        public KhuGianPage()
        {
            InitializeComponent();
            Loaded += KhuGianPage_Loaded;
            SmartNumberFormatHelper.AttachDecimalKm(txtCuLyKg, min: 0.1m, max: 2000m);
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

                        cboGaDau.Items.Add(new ComboBoxItem { Content = itemText, Tag = maGa });
                        cboGaCuoi.Items.Add(new ComboBoxItem { Content = itemText, Tag = maGa });
                    }
                }

                if (cboGaDau.Items.Count > 0) cboGaDau.SelectedIndex = 0;
                if (cboGaCuoi.Items.Count > 1) cboGaCuoi.SelectedIndex = 1;
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

        private void ApplyFilter()
        {
            if (_cachedTable == null) return;

            string keyword = txtTimKiem.Text.Trim().Replace("'", "''");
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

            if (!string.IsNullOrEmpty(keyword))
            {
                conditions.Add($"(TenGaDau LIKE '%{keyword}%' OR TenGaCuoi LIKE '%{keyword}%' OR MaGaDauCode LIKE '%{keyword}%' OR MaGaCuoiCode LIKE '%{keyword}%')");
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
            btnXoaTimKiem.Visibility = string.IsNullOrEmpty(keyword) ? Visibility.Collapsed : Visibility.Visible;

            if (coKetQua && (dgKhuGian.SelectedIndex < 0 || dgKhuGian.SelectedIndex >= view.Count))
            {
                dgKhuGian.SelectedIndex = 0;
            }
        }

        private void TxtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingData) return;
            ApplyFilter();
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
            LoadDanhSachGa();
            LoadData(_currentMaKg);
        }

        private void DgKhuGian_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingData) return;

            if (_isDirty)
            {
                _isDirty = false;
            }

            if (dgKhuGian.SelectedItem is DataRowView drv)
            {
                PopulateForm(drv);
            }
        }

        private void DgKhuGian_Sorting(object sender, DataGridSortingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (dgKhuGian.ItemsSource is DataView dv)
                {
                    for (int i = 0; i < dv.Count; i++)
                    {
                        dv[i]["STT"] = i + 1;
                    }
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
                badgeContainerKg.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                badgeContainerKg.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                txtBadgeKg.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));

                // Chuyển sang hiển thị Route Connection Card trực quan
                pnlRouteBanner.Visibility = Visibility.Visible;
                pnlRouteSelect.Visibility = Visibility.Collapsed;

                txtBannerCodeDau.Text = codeDau;
                txtBannerTenDau.Text = tenDau;
                txtBannerKmDau.Text = $"Km {FormatHelper.FormatKm(lyTrinhDau)}";

                txtBannerCodeCuoi.Text = codeCuoi;
                txtBannerTenCuoi.Text = tenCuoi;
                txtBannerKmCuoi.Text = $"Km {FormatHelper.FormatKm(lyTrinhCuoi)}";

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

                txtLuuKg.Text = "Lưu Phân Đoạn";
                btnXoaKg.IsEnabled = true;

                _currentLyTrinhMin = Math.Min(lyTrinhDau, lyTrinhCuoi);
                _currentLyTrinhMax = Math.Max(lyTrinhDau, lyTrinhCuoi);
                _currentTenPhanDoan = $"{tenDau} — {tenCuoi}";
                _currentTenGaDau = tenDau;
                _currentTenGaCuoi = tenCuoi;

                txtTrackGaDau.Text = $"{tenDau} (Km {FormatHelper.FormatKm(lyTrinhDau)})";
                txtTrackGaCuoi.Text = $"{tenCuoi} (Km {FormatHelper.FormatKm(lyTrinhCuoi)})";

                tabDuongNgangKg.IsEnabled = true;
                txtDnKhuGianTen.Text = $"Phân đoạn: {_currentTenPhanDoan}";
                txtDnPhamViKm.Text = $"Phạm vi: Km {FormatHelper.FormatKm(_currentLyTrinhMin)} — Km {FormatHelper.FormatKm(_currentLyTrinhMax)}";
                txtDnHintKm.Text = $"Phạm vi: Km {FormatHelper.FormatKm(_currentLyTrinhMin)} — Km {FormatHelper.FormatKm(_currentLyTrinhMax)}";

                TaiDanhSachDuongNgang(_currentMaKg);
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
                    if (cboGaCuoi.Items.Count > 1) cboGaCuoi.SelectedIndex = 1;

                    txtLuuKg.Text = "Tạo Mới Phân Đoạn";
                    btnXoaKg.IsEnabled = false;
                    cboGaDau.Focus();
                }
                else
                {
                    tabDuongNgangKg.IsEnabled = true;
                    pnlRouteBanner.Visibility = Visibility.Visible;
                    pnlRouteSelect.Visibility = Visibility.Collapsed;
                    txtLuuKg.Text = "Lưu Phân Đoạn";
                    btnXoaKg.IsEnabled = true;
                }

                txtCuLyKg.Text = "30.00";
                txtVmaxKhach.Text = "80";
                txtVmaxHang.Text = "50";
                txtDoDocKg.Text = "0.0";
                chkCanDauMayDay.IsChecked = false;
                cboTrangThaiKg.SelectedIndex = 0;
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void BtnHuyKg_Click(object sender, RoutedEventArgs e)
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

        private void BtnLuuKg_Click(object sender, RoutedEventArgs e)
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

            if (!FormatHelper.TryParseKm(txtCuLyKg.Text, out decimal cuLyKm) || cuLyKm <= 0)
            {
                ThongBaoDialog.CanhBao("Cự ly phân đoạn khu gian phải là số dương lớn hơn 0 (ví dụ: 30.50).", "Sai Định Dạng Cự Ly");
                txtCuLyKg.Focus();
                return;
            }

            if (!int.TryParse(txtVmaxKhach.Text.Trim(), out int vmaxKhach) || vmaxKhach <= 0)
            {
                ThongBaoDialog.CanhBao("Tốc độ tối đa tàu khách phải là số nguyên dương lớn hơn 0 km/h (tiêu chuẩn: 80 - 100 km/h).", "Lỗi Tốc Độ");
                txtVmaxKhach.Focus();
                return;
            }

            if (!int.TryParse(txtVmaxHang.Text.Trim(), out int vmaxHang) || vmaxHang <= 0)
            {
                ThongBaoDialog.CanhBao("Tốc độ tối đa tàu hàng phải là số nguyên dương lớn hơn 0 km/h (tiêu chuẩn: 50 - 60 km/h).", "Lỗi Tốc Độ");
                txtVmaxHang.Focus();
                return;
            }

            if (!decimal.TryParse(txtDoDocKg.Text.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal doDoc) || doDoc < 0)
            {
                ThongBaoDialog.CanhBao("Độ dốc lớn nhất không hợp lệ (phải >= 0 ‰).", "Lỗi Độ Dốc");
                txtDoDocKg.Focus();
                return;
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

        private void BtnXoaKg_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaKg <= 0) return;

            string tenPhanDoan = txtTitleFormKg.Text;
            bool xacNhan = ThongBaoDialog.XacNhan(
                $"Bạn có chắc chắn muốn xóa phân đoạn khu gian [{tenPhanDoan}]?\n\nLưu ý: Nếu phân đoạn này đã gắn với lịch trình đoàn tàu hoặc đường ngang, CSDL sẽ từ chối xóa để đảm bảo toàn vẹn dữ liệu.",
                "Xác Nhận Xóa Phân Đoạn");

            if (xacNhan)
            {
                bool thanhCong = _khuGianService.Xoa(_currentMaKg, out string loi);
                if (thanhCong)
                {
                    ThongBaoDialog.ThanhCong("Đã xóa phân đoạn khu gian thành công.", "Đã Xóa");
                    LoadData();
                }
                else
                {
                    ThongBaoDialog.Loi(loi, "Không Thể Xóa");
                }
            }
        }

        private void BtnNhapFile_Click(object sender, RoutedEventArgs e)
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

        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
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

        private void FormInput_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingData) return;
            _isDirty = true;
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

            double width = cvsTrackMarkers.ActualWidth - 30; // Trừ lề 15px mỗi bên
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

                // Marker: Mốc cọc trên trục ray trực quan
                var marker = new Border
                {
                    Width = laDiemDen ? 14 : 12,
                    Height = laDiemDen ? 14 : 12,
                    CornerRadius = new CornerRadius(laDiemDen ? 2 : 6),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(laDiemDen ? "#B91C1C" : (loai == "CO_NGUOI_GAC" ? "#003B73" : "#2563EB"))),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    Cursor = Cursors.Hand,
                    ToolTip = $"[Km {FormatHelper.FormatKm(lyTrinh)}] {tenDuong}\n{(laDiemDen ? "⚠️ ĐIỂM ĐEN TAI NẠN • " : "")}{(loai == "CO_NGUOI_GAC" ? "Có người gác (24/7)" : loai == "TU_DONG" ? "Cảnh báo tự động" : "Biển báo dân sinh")}"
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

            // Màu vạch Accent viền kỹ thuật mép trái
            string accentColor = laDiemDen ? "#B91C1C" : (loai switch
            {
                "CO_NGUOI_GAC" => "#003B73",
                "TU_DONG" => "#2563EB",
                _ => "#64748B"
            });

            // Khung Thẻ Trạm Hạ Tầng Kỹ Thuật (Infrastructure Crossing Card)
            var card = new Border
            {
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(4, 1, 1, 1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor)),
                Background = Brushes.White,
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 9)
            };

            // Hiệu ứng hover phẳng thanh lịch
            card.MouseEnter += (s, e) =>
            {
                card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
            };
            card.MouseLeave += (s, e) =>
            {
                card.Background = Brushes.White;
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Tầng 1: Km, Tên đường, Nút Sửa/Xóa
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Tầng 2: Thước đo vị trí tương đối
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Tầng 3: Nhãn phòng vệ & Cờ Điểm Đen

            // -------------------------------------------------------------
            // TẦNG 1: ĐỊNH DANH VỊ TRÍ & HÀNH ĐỘNG
            // -------------------------------------------------------------
            var rowTop = new Grid();
            rowTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pnlInfo = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Cột mốc Km đường sắt (Kiểu mốc bia số)
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

            // -------------------------------------------------------------
            // TẦNG 2: THƯỚC ĐO VỊ TRÍ TƯƠNG ĐỐI TRÊN KHU GIAN (PROGRESS)
            // -------------------------------------------------------------
            decimal distDau = Math.Abs(lyTrinh - _currentLyTrinhMin);
            decimal distCuoi = Math.Abs(_currentLyTrinhMax - lyTrinh);
            decimal culySegment = _currentLyTrinhMax - _currentLyTrinhMin;
            int pct = culySegment > 0 ? (int)Math.Round((distDau / culySegment) * 100) : 0;
            pct = Math.Max(0, Math.Min(100, pct));

            var pnlDistance = new StackPanel { Margin = new Thickness(0, 6, 0, 6) };

            // Text cự ly 2 đầu
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

            // Mini Track Progress Bar
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

            // -------------------------------------------------------------
            // TẦNG 3: NHÃN HẠ TẦNG & CỜ CẢNH BÁO AN TOÀN
            // -------------------------------------------------------------
            var pnlTags = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // 1. Nhãn hình thức phòng vệ
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

            // 2. Cờ Điểm Đen Kỹ Thuật (Nếu có)
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

        private void MoFormSuaDuongNgang(DataRow r)
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

        private void BtnHuyDn_Click(object sender, RoutedEventArgs e)
        {
            _currentMaDnEdit = 0;
            pnlFormDuongNgang.Visibility = Visibility.Collapsed;
        }

        private void BtnLuuDn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaKg <= 0) return;

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

        private void XoaDuongNgang(int maDn, string tenDuong, decimal lyTrinh)
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
                    txtTrackDensity.Text = "⚠️ PHONG TỎA THI CÔNG / SỰ CỐ";
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

        // =========================================================================
        // CONTEXT MENU THAO TÁC NHANH (RIGHT CLICK DATAGRID)
        // =========================================================================

        private void CtxSua_Click(object sender, RoutedEventArgs e)
        {
            tabInspectorKg.SelectedItem = tabThongSoKg;
            txtCuLyKg.Focus();
        }

        private void CtxDoiTrangThai_Click(object sender, RoutedEventArgs e)
        {
            if (dgKhuGian.SelectedItem is DataRowView drv)
            {
                int maKg = Convert.ToInt32(drv["MaKhuGian"]);
                string tenGaDau = drv["TenGaDau"]?.ToString() ?? "";
                string tenGaCuoi = drv["TenGaCuoi"]?.ToString() ?? "";
                string trangThaiHienTai = drv["TrangThai"]?.ToString() ?? "RONG";

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

        private void CtxThemDuongNgang_Click(object sender, RoutedEventArgs e)
        {
            if (dgKhuGian.SelectedItem is DataRowView)
            {
                tabInspectorKg.SelectedItem = tabDuongNgangKg;
                BtnThemDuongNgang_Click(sender, e);
            }
        }

        private void CtxSaoChep_Click(object sender, RoutedEventArgs e)
        {
            if (dgKhuGian.SelectedItem is DataRowView drv)
            {
                string info = $"PHÂN ĐOẠN KHU GIAN: {drv["TenGaDau"]} → {drv["TenGaCuoi"]}\n" +
                              $"- Cự ly: {FormatHelper.FormatKm(Convert.ToDecimal(drv["CuLyKm"]))} km\n" +
                              $"- Tốc độ tối đa: {drv["TocDoToiDaKhach"]} km/h (khách) | {drv["TocDoToiDaHang"]} km/h (hàng)\n" +
                              $"- Độ dốc: {drv["DoDocPermil"]}‰ | Máy đẩy: {(Convert.ToBoolean(drv["CanDauMayDay"]) ? "Cần" : "Không")}\n" +
                              $"- Trạng thái: {drv["TenTrangThai"]}";
                try
                {
                    Clipboard.SetText(info);
                    ThongBaoDialog.ThanhCong("Đã sao chép thông số phân đoạn khu gian vào bộ nhớ tạm (Clipboard).", "Sao Chép");
                }
                catch
                {
                    ThongBaoDialog.CanhBao(info, "Thông Tin Phân Đoạn");
                }
            }
        }

        private void CtxXoa_Click(object sender, RoutedEventArgs e)
        {
            BtnXoaKg_Click(sender, e);
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
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
            var win = Window.GetWindow(this);
            var dlg = new TongHopKhuGianGaDialog();
            if (win != null) dlg.Owner = win;
            dlg.ShowDialog();
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
    }
}
