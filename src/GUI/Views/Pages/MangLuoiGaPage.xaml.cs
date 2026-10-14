using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using BUS.Services;
using ET.HaTang;
using DTO.Common;
using GUI.Helpers;
using GUI.Profiles;
using GUI.Views.Dialogs;

namespace GUI.Views.Pages
{
    public partial class MangLuoiGaPage : Page
    {
        private readonly GaService _gaService;
        private readonly DuongRayService _duongRayService = new();
        private DataTable? _cachedTable;
        private int _currentMaGa = 0;
        private bool _isAddingNew = false;
        private bool _isDirty = false;
        private bool _isLoadingData = false;
        private int _lastSelectedIndex = -1;
        private decimal _originalLyTrinh = 0m;

        // Trạng thái quản lý đường ray trong ga
        private DataTable? _dtCurrentDuongRay;
        private int _currentMaRay = 0;
        private bool _isAddingRay = false;
        private bool _isLoadingRay = false;
        private bool _isTrackViewGrid = false;

        // Phân trang: Mặc định 50 dòng 
        private int _currentPage = 1;
        private int _pageSize = 50;

        private static readonly Brush ErrorBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
        private static readonly Thickness ErrorBorderThickness = new(1.5);
        private readonly Dictionary<Control, string> _activeValidationErrors = new();
        private decimal _maxLyTrinhHienTai = 1726.20m;
        private string _tenGaCuoiHienTai = "Sài Gòn";

        public MangLuoiGaPage()
        {
            InitializeComponent();
            _gaService = new GaService();
            Loaded += MangLuoiGaPage_Loaded;

            // Ràng buộc nhập liệu 
            SmartNumberFormatHelper.AttachDecimalKm(txtLyTrinh, min: 0m, max: 5000.00m);
            SmartNumberFormatHelper.AttachIntegerSmall(txtSoHieuRay, min: 1, max: 99);
            SmartNumberFormatHelper.AttachIntegerThousandSeparated(txtChieuDaiRay, min: 100, max: 10000);
        }

        private void MangLuoiGaPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            _isLoadingData = true;
            try
            {
                _cachedTable = _gaService.LayDanhSach();

                if (_cachedTable != null)
                {
                    if (!_cachedTable.Columns.Contains("STT"))
                    {
                        _cachedTable.Columns.Add("STT", typeof(int));
                    }
                    if (!_cachedTable.Columns.Contains("SearchIndex"))
                    {
                        _cachedTable.Columns.Add("SearchIndex", typeof(string));
                    }
                    for (int i = 0; i < _cachedTable.Rows.Count; i++)
                    {
                        var row = _cachedTable.Rows[i];
                        row["STT"] = i + 1;

                        string tenGa = row["TenGa"]?.ToString() ?? "";
                        string maCode = row["MaGaCode"]?.ToString() ?? "";
                        string tinh = row["TinhThanh"]?.ToString() ?? "";
                        decimal km = row["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(row["LyTrinhKm"]) : 0m;
                        string kmStr = km.ToString("0.00", CultureInfo.InvariantCulture);
                        string kmStrVn = km.ToString("0.00", new CultureInfo("vi-VN"));
                        string searchContent = $"{tenGa} {FormatHelper.RemoveDiacritics(tenGa)} {maCode} {tinh} {FormatHelper.RemoveDiacritics(tinh)} {kmStr} {kmStrVn} {Math.Round(km)}";
                        row["SearchIndex"] = searchContent.ToLowerInvariant();
                    }

                    decimal maxKm = 1726.20m;
                    string tenGaCuoi = "Sài Gòn";
                    foreach (DataRow r in _cachedTable.Rows)
                    {
                        if (r["LyTrinhKm"] != DBNull.Value)
                        {
                            decimal kmVal = Convert.ToDecimal(r["LyTrinhKm"]);
                            if (kmVal > maxKm)
                            {
                                maxKm = kmVal;
                                tenGaCuoi = r["TenGa"]?.ToString() ?? "Ga cuối";
                            }
                        }
                    }
                    _maxLyTrinhHienTai = maxKm;
                    _tenGaCuoiHienTai = tenGaCuoi;
                }

                _currentPage = 1;
                ApplyPaging();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể tải danh sách ga từ cơ sở dữ liệu:\n{ex.Message}", "Lỗi Tải Dữ Liệu");
            }
            finally
            {
                _isLoadingData = false;
            }

            dgGa.SelectedIndex = -1;
            if (dgGa.Items.Count > 0)
            {
                dgGa.SelectedIndex = 0;
                if (dgGa.SelectedItem is DataRowView drvFirst)
                {
                    PopulateFormFromRow(drvFirst);
                }
                dgGa.ScrollIntoView(dgGa.SelectedItem);
            }
            else
            {
                ClearFormForNew();
                ResetHaTangRayChoGaMoi();
            }
        }

        private void ApplyPaging()
        {
            if (_cachedTable == null) return;

            var view = _cachedTable.DefaultView;
            int totalItems = view.Count;
            int tongSoGoc = _cachedTable.Rows.Count;

            txtTongSo.Text = $"{tongSoGoc} ga";
            if (bdDemKetQuaLoc != null && txtDemKetQuaLoc != null)
            {
                if (totalItems < tongSoGoc)
                {
                    bdDemKetQuaLoc.Visibility = Visibility.Visible;
                    txtDemKetQuaLoc.Text = $"Hiển thị {totalItems}/{tongSoGoc} ga";
                }
                else
                {
                    bdDemKetQuaLoc.Visibility = Visibility.Collapsed;
                }
            }

            // Cập nhật thông tin trên  phân trang
            pagerGa.SetPagingInfo(totalItems, _currentPage, _pageSize, "ga");

            // Trích xuất các dòng thuộc trang hiện tại
            var filteredList = view.Cast<DataRowView>().ToList();
            IEnumerable<DataRowView> pageRows;

            if (_pageSize <= 0 || _pageSize >= totalItems)
            {
                pageRows = filteredList;
            }
            else
            {
                pageRows = filteredList.Skip((_currentPage - 1) * _pageSize).Take(_pageSize);
            }

            DataTable pagedTable = _cachedTable.Clone();
            foreach (var item in pageRows)
            {
                pagedTable.ImportRow(item.Row);
            }

            dgGa.ItemsSource = pagedTable.DefaultView;

            if (pnlEmptyState != null)
            {
                pnlEmptyState.Visibility = totalItems == 0 ? Visibility.Visible : Visibility.Collapsed;
                if (totalItems == 0 && txtEmptySearchMsg != null)
                {
                    string kw = txtTimKiem?.Text?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(kw))
                    {
                        txtEmptySearchMsg.Text = $"Không có ga nào khớp với từ khóa '{kw}'. Bấm nút bên dưới để đặt lại bộ lọc.";
                    }
                    else
                    {
                        txtEmptySearchMsg.Text = "Không có ga nào phù hợp với bộ lọc hiện tại. Bấm nút bên dưới để đặt lại bộ lọc.";
                    }
                }
            }

            if (dgGa.Items.Count > 0 && dgGa.SelectedIndex < 0)
            {
                dgGa.SelectedIndex = 0;
            }
        }

        private void PagerGa_PageChanged(object? sender, int newPage)
        {
            if (_isDirty)
            {
                bool dongY = ThongBaoDialog.XacNhan(
                    "Dữ liệu ga hiện tại chưa được lưu. Bạn có muốn hủy thay đổi để chuyển trang không?",
                    "Xác Nhận Chuyển Trang",
                    nutDongY: "Hủy thay đổi",
                    nutHuy: "Ở lại");

                if (!dongY) return;
                _isDirty = false;
            }

            _currentPage = newPage;
            ApplyPaging();
        }

        private void PagerGa_PageSizeChanged(object? sender, int newPageSize)
        {
            _pageSize = newPageSize;
            _currentPage = 1;
            ApplyPaging();
            
        }

        private void DgGa_LoadingRow(object sender, DataGridRowEventArgs e)
        {
        }

        private void DgGa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingData) return;

            // Kiểm tra an toàn dữ liệu: Cảnh báo mất thay đổi dở dang
            if (_isDirty)
            {
                bool dongY = ThongBaoDialog.XacNhan(
                    "Thông tin ga đang có thay đổi chưa được lưu.\n\nBạn có muốn hủy bỏ các thay đổi này để chuyển sang ga khác không?",
                    "Xác Nhận Chuyển Ga",
                    nutDongY: "Hủy thay đổi",
                    nutHuy: "Ở lại");

                if (!dongY)
                {
                    _isLoadingData = true;
                    dgGa.SelectedIndex = _lastSelectedIndex;
                    _isLoadingData = false;
                    return;
                }
                _isDirty = false;
            }

            if (dgGa.SelectedItem is DataRowView row)
            {
                _isAddingNew = false;
                _lastSelectedIndex = dgGa.SelectedIndex;
                PopulateFormFromRow(row);
            }
        }

        private void PopulateFormFromRow(DataRowView row)
        {
            _isLoadingData = true;
            try
            {
                _currentMaGa = Convert.ToInt32(row["MaGa"]);
                string maCode = row["MaGaCode"]?.ToString() ?? "";
                string tenGa = row["TenGa"]?.ToString() ?? "";
                decimal lyTrinh = row["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(row["LyTrinhKm"]) : 0;
                _originalLyTrinh = lyTrinh;
                string tinhThanh = row["TinhThanh"]?.ToString() ?? "";

                txtMaCode.Text = maCode;
                txtMaCode.IsReadOnly = true;
                txtMaCode.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                txtMaCode.ToolTip = "Mã Ga không thể sửa sau khi đã tạo";
                txtTenGa.Text = tenGa;
                txtLyTrinh.Text = FormatHelper.FormatKm(lyTrinh);

                // Khớp chọn ComboBoxItem Tỉnh Thành
                bool matchedTinh = false;
                foreach (ComboBoxItem item in cboTinhThanh.Items)
                {
                    if (string.Equals(item.Content?.ToString(), tinhThanh, StringComparison.OrdinalIgnoreCase))
                    {
                        cboTinhThanh.SelectedItem = item;
                        matchedTinh = true;
                        break;
                    }
                }
                if (!matchedTinh && !string.IsNullOrWhiteSpace(tinhThanh))
                {
                    var newItem = new ComboBoxItem { Content = tinhThanh };
                    cboTinhThanh.Items.Add(newItem);
                    cboTinhThanh.SelectedItem = newItem;
                }

                string hang = row["HangGa"]?.ToString() ?? "";
                if (hang == "HANG_1") cboHangGa.SelectedIndex = 0;
                else if (hang == "HANG_2") cboHangGa.SelectedIndex = 1;
                else cboHangGa.SelectedIndex = 2;

                chkCoCauQuay.IsChecked = row["CoCauQuay"] != DBNull.Value && Convert.ToBoolean(row["CoCauQuay"]);
                IsDangKhaiThac = row["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(row["DangKhaiThac"]);
                CapNhatTomTatTacNghiepGa();

                // Cập nhật Thước Đo và Cự ly theo Ga cuối động
                CapNhatThuocDoLyTrinh(lyTrinh, $"{tenGa} ({maCode})");

                // Đổ dữ liệu hạ tầng thực tế 
                int rays = row.Row.Table.Columns.Contains("SoDuongRay") && row["SoDuongRay"] != DBNull.Value 
                    ? Convert.ToInt32(row["SoDuongRay"]) : 0;
                txtSoDuongRay.Text = $"{rays} đường ray";

                int sections = row.Row.Table.Columns.Contains("SoKhuGian") && row["SoKhuGian"] != DBNull.Value 
                    ? Convert.ToInt32(row["SoKhuGian"]) : 0;
                
                bool isOperating = row["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(row["DangKhaiThac"]);
                if (sections == 0 && isOperating)
                {
                    txtSoKhuGian.Text = "0 (Cảnh báo cô lập)";
                    txtSoKhuGian.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                    bdBoxKhuGian.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
                    bdBoxKhuGian.BorderThickness = new Thickness(4, 1, 1, 1);
                    bdBoxKhuGian.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                }
                else
                {
                    txtSoKhuGian.Text = $"{sections} khu gian kết nối";
                    txtSoKhuGian.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                    bdBoxKhuGian.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
                    bdBoxKhuGian.BorderThickness = new Thickness(1);
                    bdBoxKhuGian.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                }

                TaiChiTietHaTangGa(_currentMaGa);

                // Đổ dữ liệu kiểm toán 
                string creator = row.Row.Table.Columns.Contains("NguoiTao") && row["NguoiTao"] != DBNull.Value 
                    ? row["NguoiTao"].ToString()! : "admin";
                DateTime createTime = row.Row.Table.Columns.Contains("NgayTao") && row["NgayTao"] != DBNull.Value 
                    ? Convert.ToDateTime(row["NgayTao"]) : DateTime.UtcNow;
                txtNguoiTao.Text = $"{creator} ({FormatHelper.FormatDateTime(createTime)})";

                if (row.Row.Table.Columns.Contains("NgayCapNhat") && row["NgayCapNhat"] != DBNull.Value)
                {
                    DateTime updTime = Convert.ToDateTime(row["NgayCapNhat"]);
                    string updUser = row.Row.Table.Columns.Contains("NguoiCapNhat") && row["NguoiCapNhat"] != DBNull.Value 
                        ? row["NguoiCapNhat"].ToString()! : "admin";
                    txtNgayCapNhat.Text = $"{updUser} ({FormatHelper.FormatDateTime(updTime)})";
                }
                else
                {
                    txtNgayCapNhat.Text = "Chưa có cập nhật";
                }

                inspectorHeader.SetStationDetail(tenGa, maCode, _currentMaGa, "Tuyến Bắc - Nam");

                crudActionBar.SetSaveMode(isAddingNew: false);
                crudActionBar.SetSaveEnabled(false);
                crudActionBar.SetCancelEnabled(false);
                crudActionBar.SetDeleteEnabled(true);
                CapNhatTrangThaiCacNutRay();

                ClearFormValidationError();
                _isDirty = false;
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có sự cố khi hiển thị thông tin ga:\n{ex.Message}", "Lỗi Hiển Thị");
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void ClearFormForNew()
        {
            ClearFormValidationError();
            _isAddingNew = true;
            _isLoadingData = true;
            try
            {
                _currentMaGa = 0;
                _originalLyTrinh = 0m;
                txtMaCode.Text = "";
                txtMaCode.IsReadOnly = false;
                txtMaCode.Background = Brushes.White;
                txtMaCode.ToolTip = "Nhập Mã Code Ga (ví dụ: HNI, SGO, DAN)";

                txtTenGa.Text = "";
                txtLyTrinh.Text = "";
                cboTinhThanh.Text = "Hà Nội";
                cboHangGa.SelectedIndex = 0;
                chkCoCauQuay.IsChecked = false;
                IsDangKhaiThac = true;
                CapNhatTomTatTacNghiepGa();

                txtSoDuongRay.Text = "0 đường ray";
                txtSoKhuGian.Text = "Chưa kết nối";
                txtSoKhuGian.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                if (bdBoxKhuGian != null)
                {
                    bdBoxKhuGian.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
                    bdBoxKhuGian.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                }
                if (pnlChiTietKhuGian != null)
                {
                    pnlChiTietKhuGian.Children.Clear();
                    pnlChiTietKhuGian.Children.Add(new TextBlock { Text = "Ga mới chưa có phân đoạn khu gian kết nối", FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), FontStyle = FontStyles.Italic });
                }

                // Dọn sạch hoàn toàn dữ liệu của ga cũ
                ResetHaTangRayChoGaMoi();

                txtNguoiTao.Text = "admin (Mới)";
                txtNgayCapNhat.Text = "Chưa ghi nhận";

                inspectorHeader.SetCreateMode();
                crudActionBar.SetSaveMode(isAddingNew: true);
                crudActionBar.SetSaveEnabled(true);
                crudActionBar.SetCancelEnabled(true);
                crudActionBar.SetDeleteEnabled(false);

                // Tự động chuyển Inspector về Tab 0 (Thông Tin Ga)
                if (tabInspector != null)
                {
                    tabInspector.SelectedIndex = 0;
                }

                _isDirty = false;
            }
            finally
            {
                _isLoadingData = false;
            }

            txtMaCode.Focus();
        }

        private void MarkDirty()
        {
            if (!_isLoadingData && crudActionBar != null)
            {
                _isDirty = true;
                crudActionBar.SetSaveEnabled(true);
                crudActionBar.SetCancelEnabled(true);
                inspectorHeader.SetDirtyState(true);
            }
        }

        private void FormInput_Changed(object sender, TextChangedEventArgs e)
        {
            if (sender == txtMaCode && _activeValidationErrors.ContainsKey(txtMaCode))
            {
                string code = txtMaCode.Text.Trim().ToUpperInvariant();
                if (code.Length >= 2 && System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z0-9_\-]+$"))
                {
                    RemoveFieldError(txtMaCode);
                }
            }
            else if (sender == txtTenGa && _activeValidationErrors.ContainsKey(txtTenGa))
            {
                string ten = txtTenGa.Text.Trim();
                if (ten.Length >= 2)
                {
                    RemoveFieldError(txtTenGa);
                }
            }
            else if (sender == txtLyTrinh)
            {
                if (string.IsNullOrWhiteSpace(txtLyTrinh.Text))
                {
                    CapNhatThuocDoLyTrinh(0m, "mới");
                }
                else if (FormatHelper.TryParseKm(txtLyTrinh.Text, out decimal km) && km >= 0 && km <= 5000.00m)
                {
                    if (_activeValidationErrors.ContainsKey(txtLyTrinh))
                    {
                        RemoveFieldError(txtLyTrinh);
                    }

                    // Cập nhật Thước đo Lý trình động
                    CapNhatThuocDoLyTrinh(km, txtTenGa.Text);

                    // Nếu đang thêm mới ga: Tự động dự đoán và đồng bộ tỉnh/thành theo mốc km
                    if (_isAddingNew)
                    {
                        string tinhGoiY = GoiYTinhThanhTheoLyTrinh(km);
                        ChonTinhThanhTheoTen(tinhGoiY);
                    }
                }
            }

            MarkDirty();
        }

        private void FormCombo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender == cboTinhThanh && _activeValidationErrors.ContainsKey(cboTinhThanh))
            {
                string tinh = (cboTinhThanh.SelectedItem is ComboBoxItem cbi ? cbi.Content?.ToString() : cboTinhThanh.Text)?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(tinh))
                {
                    RemoveFieldError(cboTinhThanh);
                }
            }
            else if (sender == cboHangGa)
            {
                CapNhatTomTatTacNghiepGa();
            }

            MarkDirty();
        }

        private void FormCheck_Changed(object sender, RoutedEventArgs e)
        {
            CapNhatTomTatTacNghiepGa();
            MarkDirty();
        }

        /// <summary>
        /// Đồng bộ hiển thị thẻ tóm tắt vai trò vận hành và thiết bị đầu máy theo dữ liệu thực của Ga.
        /// </summary>
        private void CapNhatTomTatTacNghiepGa()
        {
            if (txtQuyChuanGa == null || txtThietBiQuay == null) return;

            int hangIdx = cboHangGa?.SelectedIndex ?? 0;
            string hangText = hangIdx switch
            {
                0 => "Hạng I · Đầu mối trung tâm",
                1 => "Hạng II · Trung chuyển khu vực",
                _ => "Hạng III · Tránh vượt kỹ thuật"
            };
            string tacNghiepText = hangIdx switch
            {
                0 => "Đón trả khách & lập giải tàu hàng lớn",
                1 => "Đón trả khách & điều tiết khu vực",
                _ => "Tác nghiệp giao tránh và vượt tàu kỹ thuật"
            };
            txtQuyChuanGa.Text = hangText;
            if (txtTacNghiepGa != null) txtTacNghiepGa.Text = tacNghiepText;

            bool coCauQuay = chkCoCauQuay?.IsChecked == true;
            txtThietBiQuay.Text = coCauQuay ? "Có cầu quay xe lửa" : "Không trang bị bàn quay";
            if (txtMoTaQuay != null) txtMoTaQuay.Text = coCauQuay ? "Đổi chiều đầu máy kỹ thuật" : "Tác nghiệp phương thức kéo đẩy 2 đầu";
        }

        private void ClearFormValidationError(bool preserveBanner = false)
        {
            if (!preserveBanner && pnlFormError != null)
            {
                pnlFormError.Visibility = Visibility.Collapsed;
            }

            foreach (var ctrl in _activeValidationErrors.Keys)
            {
                ctrl.ClearValue(Control.BorderBrushProperty);
                ctrl.ClearValue(Control.BorderThicknessProperty);
                ctrl.ClearValue(Border.BorderBrushProperty);
                ctrl.ClearValue(Border.BorderThicknessProperty);
            }
            _activeValidationErrors.Clear();
        }

        /// Xóa viền đỏ của 1 trường cụ thể khi người dùng đã sửa hợp lệ.
        private void RemoveFieldError(Control ctrl)
        {
            if (_activeValidationErrors.ContainsKey(ctrl))
            {
                ctrl.ClearValue(Control.BorderBrushProperty);
                ctrl.ClearValue(Control.BorderThicknessProperty);
                ctrl.ClearValue(Border.BorderBrushProperty);
                ctrl.ClearValue(Border.BorderThicknessProperty);
                _activeValidationErrors.Remove(ctrl);

                if (_activeValidationErrors.Count == 0)
                {
                    if (pnlFormError != null)
                    {
                        pnlFormError.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    CapNhatHienThiBannerLoi();
                }
            }
        }

        /// <summary>
        /// Cập nhật nội dung hiển thị của banner thông báo lỗi tổng hợp.
        /// </summary>
        private void CapNhatHienThiBannerLoi()
        {
            if (txtFormErrorMsg == null || pnlFormError == null) return;

            if (_activeValidationErrors.Count == 0)
            {
                pnlFormError.Visibility = Visibility.Collapsed;
                return;
            }

            if (_activeValidationErrors.Count == 1)
            {
                txtFormErrorMsg.Text = _activeValidationErrors.Values.First();
            }
            else
            {
                txtFormErrorMsg.Text = $"Vui lòng hoàn tất {_activeValidationErrors.Count} thông tin sau trước khi lưu:\n• " 
                    + string.Join("\n• ", _activeValidationErrors.Values);
            }
            pnlFormError.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hiển thị cảnh báo lỗi nhập liệu đồng loạt cho tất cả các trường bị thiếu hoặc sai .
        /// </summary>
        private void HienThiDanhSachLoiNhapLieu(Control? specificFocus = null, bool autoFocus = true)
        {
            if (_activeValidationErrors.Count == 0)
            {
                ClearFormValidationError();
                return;
            }

            foreach (var ctrl in _activeValidationErrors.Keys)
            {
                ctrl.BorderBrush = ErrorBorderBrush;
                ctrl.BorderThickness = ErrorBorderThickness;
            }

            CapNhatHienThiBannerLoi();

            if (autoFocus)
            {
                Control focusTarget = specificFocus ?? _activeValidationErrors.Keys.First();
                focusTarget.Focus();
                if (focusTarget is TextBox tb)
                {
                    tb.SelectAll();
                }

                scrollViewerForm?.ScrollToTop();
            }
        }

        private void BtnDismissFormError_Click(object sender, RoutedEventArgs e)
        {
            ClearFormValidationError(preserveBanner: false);
        }

        private bool IsDangKhaiThac
        {
            get => rbDangKhaiThac?.IsChecked == true;
            set
            {
                if (rbDangKhaiThac != null) rbDangKhaiThac.IsChecked = value;
                if (rbTamNgungKhaiThac != null) rbTamNgungKhaiThac.IsChecked = !value;
            }
        }

        private void RbTrangThaiGa_Click(object sender, RoutedEventArgs e)
        {
            MarkDirty();
        }

        /// <summary>
        /// Tự động chuẩn hóa hiển thị về dấu chấm thập phân "688.00" .
        /// Đồng thời kiểm tra nhanh xem mốc km có bị trùng với ga khác trên tuyến không.
        /// </summary>
        private void TxtLyTrinh_LostFocus(object sender, RoutedEventArgs e)
        {
            string raw = txtLyTrinh.Text.Trim();
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            if (FormatHelper.TryParseKm(raw, out decimal km))
            {
                string formatted = FormatHelper.FormatKm(km);
                if (txtLyTrinh.Text != formatted)
                {
                    txtLyTrinh.Text = formatted;
                }

                try
                {
                    // Kiểm tra nhanh trùng lý trình với ga khác 
                    string? gaTrung = _gaService.LayTenGaTheoLyTrinh(1, km, _currentMaGa);
                    if (!string.IsNullOrEmpty(gaTrung))
                    {
                        _activeValidationErrors[txtLyTrinh] = $"Lý trình Km {formatted} đã trùng với Ga {gaTrung}.";
                        HienThiDanhSachLoiNhapLieu(txtLyTrinh, autoFocus: false);
                    }
                    else
                    {
                        if (_activeValidationErrors.TryGetValue(txtLyTrinh, out string? currentErr) && currentErr.Contains("đã trùng"))
                        {
                            RemoveFieldError(txtLyTrinh);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TxtLyTrinh_LostFocus] Lỗi kiểm tra lý trình: {ex.Message}");
                }
            }
        }

        private void TxtLyTrinh_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Chặn chữ cái và ký tự lạ ngay khi gõ: chỉ chấp nhận số [0-9] và dấu chấm/phẩy
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9\.,]+$"))
            {
                e.Handled = true;
                return;
            }

            if (e.Text == "." || e.Text == ",")
            {
                string curText = txtLyTrinh.Text;
                if (txtLyTrinh.SelectionLength > 0)
                {
                    curText = curText.Remove(txtLyTrinh.SelectionStart, txtLyTrinh.SelectionLength);
                }
                if (curText.Contains(".") || curText.Contains(","))
                {
                    e.Handled = true;
                }
            }
        }

        private void TxtLyTrinh_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        // Kiểm tra nhanh trùng mã code ga ngay khi người dùng rời ô nhập (không cướp tiêu điểm).
        private void TxtMaCode_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!_isAddingNew) return;

            string code = txtMaCode.Text.Trim().ToUpperInvariant();
            if (code.Length >= 2)
            {
                try
                {
                    if (_gaService.KiemTraMaGaCodeTonTai(code, _currentMaGa))
                    {
                        _activeValidationErrors[txtMaCode] = $"Mã Ga '{code}' đã được sử dụng. Vui lòng chọn mã khác.";
                        HienThiDanhSachLoiNhapLieu(txtMaCode, autoFocus: false);
                    }
                    else
                    {
                        if (_activeValidationErrors.TryGetValue(txtMaCode, out string? currentErr) && currentErr.Contains("đã được sử dụng"))
                        {
                            RemoveFieldError(txtMaCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TxtMaCode_LostFocus] Lỗi kiểm tra mã ga: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Cập nhật hiển thị Thước đo Lý trình và cự ly đến Ga cuối động
        /// Tự động mở dãn trục nếu có ga vượt mốc 1.726,20 km (ví dụ Cần Thơ Km 1.900, Cà Mau).
        /// </summary>
        private void CapNhatThuocDoLyTrinh(decimal lyTrinh, string? tenGaHienThi = null)
        {
            decimal mocTrucTuyen = Math.Max(_maxLyTrinhHienTai, Math.Max(1726.20m, lyTrinh));
            double pt = mocTrucTuyen > 0 ? (double)(lyTrinh / mocTrucTuyen * 100m) : 0;

            if (pbLyTrinhMini != null)
            {
                pbLyTrinhMini.Maximum = (double)mocTrucTuyen;
                pbLyTrinhMini.Value = (double)Math.Min(Math.Max(lyTrinh, 0m), mocTrucTuyen);
            }

            if (txtTyLeTuyen != null)
            {
                txtTyLeTuyen.Text = $"{pt:0.0}% trục tuyến (Km {FormatHelper.FormatKm(lyTrinh)})";
            }

            if (txtKhoangCachSaiGon != null)
            {
                if (lyTrinh >= mocTrucTuyen && lyTrinh > 0)
                {
                    txtKhoangCachSaiGon.Text = $"Ga cuối tuyến (Mốc Km {FormatHelper.FormatKm(lyTrinh)})";
                }
                else
                {
                    decimal cachGaCuoi = Math.Max(0m, mocTrucTuyen - lyTrinh);
                    txtKhoangCachSaiGon.Text = $"Cách {_tenGaCuoiHienTai} {FormatHelper.FormatKm(cachGaCuoi)} km";
                }
            }

            if (txtThongTinLyTrinh != null)
            {
                string ten = string.IsNullOrWhiteSpace(tenGaHienThi) ? "mới" : tenGaHienThi.Trim();
                txtThongTinLyTrinh.Text = $"Vị trí: Ga {ten} — Km {FormatHelper.FormatKm(lyTrinh)} ({pt:0.0}% trục tuyến)";
            }
        }

        // Tự động gợi ý tỉnh / thành phố theo mốc lý trình trên toàn tuyến đường sắt.
        private static string GoiYTinhThanhTheoLyTrinh(decimal km)
        {
            if (km < 35m) return "Hà Nội";
            if (km < 57m) return "Hà Nam";
            if (km < 111m) return "Nam Định";
            if (km < 151m) return "Ninh Bình";
            if (km < 239m) return "Thanh Hóa";
            if (km < 319m) return "Nghệ An";
            if (km < 411m) return "Hà Tĩnh";
            if (km < 523m) return "Quảng Bình";
            if (km < 651m) return "Quảng Trị";
            if (km < 751m) return "Thừa Thiên Huế";
            if (km < 821m) return "Đà Nẵng";
            if (km < 901m) return "Quảng Nam";
            if (km < 1001m) return "Quảng Ngãi";
            if (km < 1151m) return "Bình Định";
            if (km < 1251m) return "Phú Yên";
            if (km < 1381m) return "Khánh Hòa";
            if (km < 1451m) return "Ninh Thuận";
            if (km < 1651m) return "Bình Thuận";
            if (km < 1701m) return "Đồng Nai";
            if (km < 1716m) return "Bình Dương";
            if (km <= 1740m) return "TP. Hồ Chí Minh";
            if (km < 1780m) return "Long An";
            if (km < 1830m) return "Tiền Giang";
            if (km < 1880m) return "Vĩnh Long";
            if (km < 1950m) return "Cần Thơ";
            if (km < 2050m) return "Hậu Giang";
            if (km < 2150m) return "Sóc Trăng";
            if (km < 2250m) return "Bạc Liêu";
            return "Cà Mau";
        }

        private void ChonTinhThanhTheoTen(string tenTinh)
        {
            if (cboTinhThanh == null || string.IsNullOrWhiteSpace(tenTinh)) return;

            foreach (ComboBoxItem item in cboTinhThanh.Items)
            {
                if (string.Equals(item.Content?.ToString(), tenTinh, StringComparison.OrdinalIgnoreCase))
                {
                    if (cboTinhThanh.SelectedItem != item)
                    {
                        cboTinhThanh.SelectedItem = item;
                    }
                    return;
                }
            }
        }

        private void BtnThemGaMoi_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                bool dongY = ThongBaoDialog.XacNhan(
                    "Dữ liệu ga hiện tại chưa được lưu. Bạn có muốn bỏ qua thay đổi để khai báo ga mới không?",
                    "Xác Nhận Tạo Mới",
                    nutDongY: "Bỏ qua thay đổi",
                    nutHuy: "Ở lại");

                if (!dongY) return;
            }

            dgGa.SelectedIndex = -1;
            ClearFormForNew();
        }

        private void CrudActionBar_SaveClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                _activeValidationErrors.Clear();

                string maCode = txtMaCode.Text.Trim().ToUpperInvariant();
                bool laThemMoi = _isAddingNew || _currentMaGa <= 0;
                if (laThemMoi)
                {
                    if (string.IsNullOrWhiteSpace(maCode))
                    {
                        _activeValidationErrors[txtMaCode] = "Mã Ga: Chưa nhập mã ga (ví dụ: HNI, SGO)";
                    }
                    else if (maCode.Length < 2)
                    {
                        _activeValidationErrors[txtMaCode] = "Mã Ga: Tối thiểu 2 ký tự (ví dụ: HN, SGO)";
                    }
                    else if (!System.Text.RegularExpressions.Regex.IsMatch(maCode, @"^[A-Z0-9_\-]+$"))
                    {
                        _activeValidationErrors[txtMaCode] = "Mã Ga: Chỉ gồm chữ in hoa, số hoặc gạch nối";
                    }
                }

                string tenGa = txtTenGa.Text.Trim();
                if (string.IsNullOrWhiteSpace(tenGa))
                {
                    _activeValidationErrors[txtTenGa] = "Tên Ga: Chưa nhập tên ga (ví dụ: Hà Nội, Huế)";
                }
                else if (tenGa.Length < 2)
                {
                    _activeValidationErrors[txtTenGa] = "Tên Ga: Tối thiểu 2 ký tự";
                }

                string rawLyTrinh = txtLyTrinh.Text.Trim();
                decimal lyTrinh = 0m;
                if (string.IsNullOrWhiteSpace(rawLyTrinh))
                {
                    _activeValidationErrors[txtLyTrinh] = "Lý Trình: Chưa nhập số km (ví dụ: 125.50)";
                }
                else if (!FormatHelper.TryParseKm(rawLyTrinh, out lyTrinh) || lyTrinh < 0 || lyTrinh > GaService.MaxLyTrinhKm)
                {
                    _activeValidationErrors[txtLyTrinh] = $"Lý Trình: Phải từ 0.00 đến {FormatHelper.FormatKm(GaService.MaxLyTrinhKm)} km";
                }

                string tinhThanh = (cboTinhThanh.SelectedItem is ComboBoxItem cbi ? cbi.Content?.ToString() : cboTinhThanh.Text)?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(tinhThanh))
                {
                    _activeValidationErrors[cboTinhThanh] = "Tỉnh / Thành: Chưa chọn tỉnh hoặc thành phố";
                }

                if (_activeValidationErrors.Count > 0)
                {
                    HienThiDanhSachLoiNhapLieu();
                    return;
                }

                ClearFormValidationError();

                // Cảnh báo xoá có ga có dữ liệu
                if (!_isAddingNew && _currentMaGa > 0 && !IsDangKhaiThac)
                {
                    if (_gaService.KiemTraGaDangCoTau(_currentMaGa))
                    {
                        ThongBaoDialog.CanhBao($"Ga {tenGa} hiện đang có đoàn tàu hoạt động trên đường ray trong ga. Không thể chuyển sang 'Tạm ngừng khai thác' khi chưa giải phóng tàu khỏi ga.", "Vi Phạm An Toàn");
                        IsDangKhaiThac = true;
                        return;
                    }

                    var dtKg = _gaService.LayKhuGianTheoGa(_currentMaGa);
                    int soKg = dtKg?.Rows.Count ?? 0;
                    if (soKg > 0)
                    {
                        bool xacNhan = ThongBaoDialog.XacNhan(
                            $"Ga {tenGa} hiện đang kết nối với {soKg} khu gian trên tuyến.\n\nTạm ngừng ga này có thể ảnh hưởng đến lịch chạy tàu.\n\nBạn có chắc muốn chuyển ga này sang TẠM NGỪNG không?",
                            "Xác Nhận Tạm Ngừng Ga",
                            nutDongY: "Tạm ngừng",
                            nutHuy: "Hủy bỏ");

                        if (!xacNhan)
                        {
                            IsDangKhaiThac = true;
                            return;
                        }
                    }
                }

                if (!_isAddingNew && _currentMaGa > 0 && Math.Abs(lyTrinh - _originalLyTrinh) > 0.001m)
                {
                    // 1. Kiểm tra đảo lộn trật tự tương quan giữa các ga trên tuyến
                    if (KiemTraDaoLonThuTuKm(_currentMaGa, lyTrinh, tenGa, out string canhBaoThuTu))
                    {
                        bool xacNhanDaoLon = ThongBaoDialog.XacNhan(
                            $"{canhBaoThuTu}\n\nBạn có chắc chắn muốn xác nhận thay đổi thứ tự km này không?",
                            "Cảnh Báo Đảo Lộn Thứ Tự Ga");

                        if (!xacNhanDaoLon)
                        {
                            txtLyTrinh.Focus();
                            return;
                        }
                    }

                    // 2. Cảnh báo lệch cự ly khu gian liên kết
                    var dtKg = _gaService.LayKhuGianTheoGa(_currentMaGa);
                    int soKg = dtKg?.Rows.Count ?? 0;
                    if (soKg > 0)
                    {
                        bool xacNhanKm = ThongBaoDialog.XacNhan(
                            $"Lý trình của Ga {tenGa} được thay đổi từ {FormatHelper.FormatKm(_originalLyTrinh)} km thành {FormatHelper.FormatKm(lyTrinh)} km.\n\nGa này hiện đang kết nối với {soKg} phân đoạn khu gian. Việc thay đổi lý trình sẽ làm lệch cự ly khoảng cách giữa các ga liên quan.\n\nBạn có chắc chắn muốn lưu mốc lý trình mới này không?",
                            "Xác Nhận Thay Đổi Lý Trình Ga");

                        if (!xacNhanKm)
                        {
                            txtLyTrinh.Focus();
                            return;
                        }
                    }
                }

                string hangGa = "HANG_1";
                if (cboHangGa.SelectedIndex == 1) hangGa = "HANG_2";
                else if (cboHangGa.SelectedIndex == 2) hangGa = "HANG_3";

                var ga = new Ga
                {
                    MaTuyen = 1,
                    MaGaCode = maCode,
                    TenGa = tenGa,
                    LyTrinhKm = lyTrinh,
                    TinhThanh = tinhThanh,
                    HangGa = hangGa,
                    CoCauQuay = chkCoCauQuay.IsChecked == true,
                    DangKhaiThac = IsDangKhaiThac,
                    NguoiTao = "admin",
                    NguoiCapNhat = "admin"
                };

                if (laThemMoi)
                {
                    if (_gaService.Them(ga, out string err))
                    {
                        ClearFormValidationError();
                        ThongBaoDialog.ThanhCong($"Đã thêm Ga {ga.TenGa} ({ga.MaGaCode}) thành công.", "Thêm Ga Thành Công");
                        _isDirty = false;
                        _isAddingNew = false;
                        LoadData();
                    }
                    else
                    {
                        _activeValidationErrors.Clear();
                        Control? target = null;
                        if (err.Contains("Mã") || err.Contains("mã") || err.Contains("Code")) target = txtMaCode;
                        else if (err.Contains("Tên") || err.Contains("tên")) target = txtTenGa;
                        else if (err.Contains("Lý trình") || err.Contains("lý trình") || err.Contains("km")) target = txtLyTrinh;

                        if (target != null)
                        {
                            _activeValidationErrors[target] = err;
                            HienThiDanhSachLoiNhapLieu(target);
                        }
                        else
                        {
                            txtFormErrorMsg.Text = err;
                            pnlFormError.Visibility = Visibility.Visible;
                            scrollViewerForm?.ScrollToTop();
                        }
                    }
                }
                else
                {
                    ga.MaGa = _currentMaGa;
                    if (_gaService.CapNhat(ga, out string err))
                    {
                        ClearFormValidationError();
                        ThongBaoDialog.ThanhCong($"Đã lưu thay đổi Ga {ga.TenGa} ({ga.MaGaCode}) thành công.", "Lưu Thành Công");
                        _isDirty = false;
                        LoadData();
                    }
                    else
                    {
                        _activeValidationErrors.Clear();
                        Control? target = null;
                        if (err.Contains("Tên") || err.Contains("tên")) target = txtTenGa;
                        else if (err.Contains("Lý trình") || err.Contains("lý trình") || err.Contains("km")) target = txtLyTrinh;

                        if (target != null)
                        {
                            _activeValidationErrors[target] = err;
                            HienThiDanhSachLoiNhapLieu(target);
                        }
                        else
                        {
                            txtFormErrorMsg.Text = err;
                            pnlFormError.Visibility = Visibility.Visible;
                            scrollViewerForm?.ScrollToTop();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi lưu thông tin ga:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        /// <summary>
        /// Kiểm tra xem mốc lý trình mới có bị nhảy cóc làm đảo lộn thứ tự địa lý với các ga lân cận không.
        /// </summary>
        private bool KiemTraDaoLonThuTuKm(int maGaHienTai, decimal lyTrinhMoi, string tenGaHienTai, out string canhBao)
        {
            canhBao = "";
            if (_cachedTable == null || _cachedTable.Rows.Count == 0) return false;

            try
            {
                var otherStations = _cachedTable.AsEnumerable()
                    .Where(r => r.RowState != DataRowState.Deleted && Convert.ToInt32(r["MaGa"]) != maGaHienTai)
                    .Select(r => new
                    {
                        MaGa = Convert.ToInt32(r["MaGa"]),
                        TenGa = r["TenGa"]?.ToString() ?? "",
                        LyTrinhKm = Convert.ToDecimal(r["LyTrinhKm"])
                    })
                    .OrderBy(s => s.LyTrinhKm)
                    .ToList();

                if (otherStations.Count == 0) return false;

                decimal kmCu = _originalLyTrinh;
                var invertedStations = new List<string>();

                if (lyTrinhMoi > kmCu)
                {
                    // Tăng km: xem có ga nào vốn đứng sau ga hiện tại nhưng lại nhỏ hơn km mới không
                    foreach (var s in otherStations)
                    {
                        if (s.LyTrinhKm > kmCu && s.LyTrinhKm < lyTrinhMoi)
                        {
                            invertedStations.Add($"{s.TenGa} (Km {FormatHelper.FormatKm(s.LyTrinhKm)})");
                        }
                    }
                }
                else if (lyTrinhMoi < kmCu)
                {
                    // Giảm km: xem có ga nào vốn đứng trước ga hiện tại nhưng lại lớn hơn km mới không
                    foreach (var s in otherStations)
                    {
                        if (s.LyTrinhKm < kmCu && s.LyTrinhKm > lyTrinhMoi)
                        {
                            invertedStations.Add($"{s.TenGa} (Km {FormatHelper.FormatKm(s.LyTrinhKm)})");
                        }
                    }
                }

                if (invertedStations.Count > 0)
                {
                    canhBao = $"Lý trình mới (Km {FormatHelper.FormatKm(lyTrinhMoi)}) làm đảo lộn thứ tự địa lý của Ga {tenGaHienTai} so với các ga: {string.Join(", ", invertedStations.Take(3))}{(invertedStations.Count > 3 ? "..." : "")}.\n\nĐiều này làm thay đổi trình tự đón gửi tàu tuần tự trên trục Bắc — Nam.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KiemTraDaoLonThuTuKm] Lỗi kiểm tra thứ tự: {ex.Message}");
            }

            return false;
        }

        private void CrudActionBar_CancelClicked(object sender, RoutedEventArgs e)
        {
            ClearFormValidationError();
            _isDirty = false;
            _isAddingNew = false;
            inspectorHeader.SetDirtyState(false);

            if (dgGa.SelectedItem is DataRowView row)
            {
                PopulateFormFromRow(row);
            }
            else if (dgGa.Items.Count > 0)
            {
                dgGa.SelectedIndex = 0;
            }
        }

        private void CrudActionBar_DeleteClicked(object sender, RoutedEventArgs e)
        {
            if (dgGa.SelectedItem is DataRowView row)
            {
                int maGa = Convert.ToInt32(row["MaGa"]);
                string tenGa = row["TenGa"]?.ToString() ?? "";
                string maCode = row["MaGaCode"]?.ToString() ?? "";

                bool confirm = ThongBaoDialog.XacNhanXoaCoMa(
                    $"Bạn có chắc chắn muốn xóa Ga {tenGa} ({maCode}) không?\n\nLưu ý: Không thể hoàn tác hành động này.", 
                    "Xác Nhận Xóa Ga", 
                    maXacNhan: maCode,
                    nutXoa: "Xác nhận xóa",
                    nutHuy: "Hủy bỏ");

                if (confirm)
                {
                    try
                    {
                        if (_gaService.Xoa(maGa, out bool daXoaVinhVien, out string thongBao))
                        {
                            _currentMaGa = 0;
                            _isDirty = false;
                            ClearFormForNew();
                            ResetHaTangRayChoGaMoi();

                            if (daXoaVinhVien)
                            {
                                ThongBaoDialog.ThanhCong($"Đã xóa hoàn toàn Ga {tenGa} ({maCode}) khỏi hệ thống.", "Đã Xóa");
                            }
                            else
                            {
                                ThongBaoDialog.ThongTin($"Ga {tenGa} ({maCode}) đã có dữ liệu hạ tầng kết nối nên hệ thống đã chuyển trạng thái sang 'Tạm ngừng khai thác' để bảo toàn dữ liệu.", "Đã Tạm Ngừng Khai Thác");
                            }
                            LoadData();
                        }
                        else
                        {
                            ThongBaoDialog.CanhBao(thongBao, "Không Thể Xóa Ga");
                        }
                    }
                    catch (Exception ex)
                    {
                        ThongBaoDialog.Loi($"Không thể xóa ga: {ex.Message}", "Lỗi");
                    }
                }
            }
            else
            {
                ThongBaoDialog.CanhBao("Vui lòng chọn một ga từ danh sách trước khi xóa.", "Chưa Chọn Dữ Liệu");
            }
        }

        private void TaiChiTietHaTangGa(int maGa)
        {
            if (pnlChiTietKhuGian == null) return;
            pnlChiTietKhuGian.Children.Clear();

            try
            {
                // 1. Nạp chi tiết các khu gian kết nối
                var dtKg = _gaService.LayKhuGianTheoGa(maGa);
                if (dtKg == null || dtKg.Rows.Count == 0)
                {
                    var emptyBox = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(12, 10, 12, 10),
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    var emptySp = new StackPanel { Orientation = Orientation.Horizontal };
                    emptySp.Children.Add(new Wpf.Ui.Controls.SymbolIcon
                    {
                        Symbol = Wpf.Ui.Controls.SymbolRegular.Info24,
                        FontSize = 14,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                        Margin = new Thickness(0, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    emptySp.Children.Add(new TextBlock
                    {
                        Text = "Ga chưa có liên kết khu gian trực tiếp trong CSDL.",
                        FontSize = 11.5,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    emptyBox.Child = emptySp;
                    pnlChiTietKhuGian.Children.Add(emptyBox);
                }
                else
                {
                    // Phân loại phân đoạn phía Bắc và phía Nam
                    DataRow? rowBac = null;
                    DataRow? rowNam = null;

                    foreach (DataRow r in dtKg.Rows)
                    {
                        int maGaCuoi = Convert.ToInt32(r["MaGaCuoi"]);
                        int maGaDau = Convert.ToInt32(r["MaGaDau"]);
                        string huong = r["HuongTuyen"]?.ToString() ?? "";

                        if (maGaCuoi == maGa || huong.Contains("Bắc"))
                        {
                            rowBac = r;
                        }
                        else if (maGaDau == maGa || huong.Contains("Nam"))
                        {
                            rowNam = r;
                        }
                    }

                    // Tên ga hiện tại và lý trình
                    string tenGaHienTai = txtTenGa.Text.Trim();
                    if (string.IsNullOrEmpty(tenGaHienTai)) tenGaHienTai = "GA ĐANG CHỌN";
                    FormatHelper.TryParseKm(txtLyTrinh.Text, out decimal lyTrinhHienTai);

                    // Sơ Đồ Ray Tuyến Tính Chuỗi Ga (Linear Railway Schematic Track)
                    var cardSoDo = TaoSoDoKhuGianTuyenTinh(tenGaHienTai, lyTrinhHienTai, rowBac, rowNam);
                    pnlChiTietKhuGian.Children.Add(cardSoDo);

                    // Dải tóm tắt thông số tốc độ & kỹ thuật phụ bên dưới sơ đồ
                    var pnlThongSo = TaoThongSoKyThuatKhuGian(rowBac, rowNam);
                    if (pnlThongSo != null)
                    {
                        pnlChiTietKhuGian.Children.Add(pnlThongSo);
                    }
                }

                // 2. Nạp chi tiết các đường ray trong ga trực tiếp vào Tab Đường Ray
                TaiDanhSachDuongRayTrongGa(maGa);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaiChiTietHaTangGa] Lỗi nạp hạ tầng ga {maGa}: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo Sơ Đồ Ray Tuyến Tính trực quan theo chuỗi liên tục:
        /// Ga Bắc ── Cự ly, Dốc ── ● GA HIỆN TẠI ── Cự ly, Dốc ── Ga Nam
        /// </summary>
        private Border TaoSoDoKhuGianTuyenTinh(string tenGaHienTai, decimal lyTrinhHienTai, DataRow? rowBac, DataRow? rowNam)
        {
            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 12, 12, 12),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand,
                ToolTip = "Nhấn để mở cửa sổ tổng hợp hồ sơ khu gian toàn tuyến"
            };

            card.MouseEnter += (s, e) =>
            {
                card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            };
            card.MouseLeave += (s, e) =>
            {
                card.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
                card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            };
            card.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                HienThiPopupTongHopKhuGian();
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90, GridUnitType.Pixel) });

            // -------------------------------------------------------------
            // CỘT 0: GA PHÍA BẮC (NÚT TRÁI)
            // -------------------------------------------------------------
            var spBac = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            if (rowBac != null)
            {
                string tenGaBac = rowBac["TenGaDau"]?.ToString() ?? "Ga Bắc";
                decimal lyTrinhBac = rowBac.Table.Columns.Contains("LyTrinhGaDau") && rowBac["LyTrinhGaDau"] != DBNull.Value 
                    ? Convert.ToDecimal(rowBac["LyTrinhGaDau"]) : 0m;

                spBac.Children.Add(new TextBlock
                {
                    Text = tenGaBac,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 85
                });
                spBac.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    StrokeThickness = 2.5,
                    Margin = new Thickness(0, 3, 0, 3),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                spBac.Children.Add(new TextBlock
                {
                    Text = FormatHelper.FormatKm(lyTrinhBac),
                    FontSize = 10,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            else
            {
                // Ga ở đầu tuyến (Ga Hà Nội)
                spBac.Children.Add(new TextBlock
                {
                    Text = "[ĐẦU TUYẾN]",
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                spBac.Children.Add(new Border
                {
                    Width = 4,
                    Height = 12,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                    CornerRadius = new CornerRadius(1),
                    Margin = new Thickness(0, 3, 0, 3),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                spBac.Children.Add(new TextBlock
                {
                    Text = "Km 0.00",
                    FontSize = 10,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            Grid.SetColumn(spBac, 0);
            grid.Children.Add(spBac);

            // -------------------------------------------------------------
            // CỘT 1: ĐOẠN RAY NỐI PHÍA BẮC (CỰ LY + ĐƯỜNG RAY + ĐỘ DỐC)
            // -------------------------------------------------------------
            var segBac = new Grid { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, 0) };
            segBac.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            segBac.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            segBac.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            if (rowBac != null)
            {
                decimal cuLyBac = Convert.ToDecimal(rowBac["CuLyKm"]);
                decimal doDocBac = Convert.ToDecimal(rowBac["DoDocPermil"]);
                bool dayTauBac = Convert.ToBoolean(rowBac["CanDauMayDay"]);

                // Cự ly (Trên ray)
                var txtCuLyBac = new TextBlock
                {
                    Text = $"{cuLyBac:0.0} km",
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 2)
                };
                Grid.SetRow(txtCuLyBac, 0);
                segBac.Children.Add(txtCuLyBac);

                // Đường ray nối ngang
                var trackBac = new Border
                {
                    Height = 3,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                    CornerRadius = new CornerRadius(1.5),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(trackBac, 1);
                segBac.Children.Add(trackBac);

                // Độ dốc & Cảnh báo (Dưới ray)
                var docPanelBac = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                if (dayTauBac)
                {
                    docPanelBac.Children.Add(new Wpf.Ui.Controls.SymbolIcon
                    {
                        Symbol = Wpf.Ui.Controls.SymbolRegular.Warning24,
                        FontSize = 10.5,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
                        Margin = new Thickness(0, 0, 3, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    docPanelBac.Children.Add(new TextBlock
                    {
                        Text = $"{doDocBac:0.0}‰",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C")),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    docPanelBac.ToolTip = $"Cần đầu máy đẩy (Độ dốc lớn {doDocBac:0.0}‰)";
                }
                else
                {
                    docPanelBac.Children.Add(new TextBlock
                    {
                        Text = doDocBac > 0 ? $"{doDocBac:0.0}‰" : "0‰",
                        FontSize = 10,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }
                Grid.SetRow(docPanelBac, 2);
                segBac.Children.Add(docPanelBac);
            }
            else
            {
                // Đoạn ray trống ở đầu tuyến
                var trackEmpty = new Border
                {
                    Height = 2,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(trackEmpty, 1);
                segBac.Children.Add(trackEmpty);
            }
            Grid.SetColumn(segBac, 1);
            grid.Children.Add(segBac);

            // -------------------------------------------------------------
            // CỘT 2: GA HIỆN TẠI (● NÚT GIỮA NỔI BẬT NHẤT)
            // -------------------------------------------------------------
            var spCurrent = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            
            // Tên ga hiện tại: IN HOA, ĐẬM, MÀU NAVY
            spCurrent.Children.Add(new TextBlock
            {
                Text = tenGaHienTai.ToUpperInvariant(),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 105
            });

            // Nút tròn to nổi bật ở giữa
            spCurrent.Children.Add(new System.Windows.Shapes.Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BFDBFE")),
                StrokeThickness = 2.5,
                Margin = new Thickness(0, 2, 0, 2),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Lý trình hiện tại
            spCurrent.Children.Add(new TextBlock
            {
                Text = FormatHelper.FormatKm(lyTrinhHienTai),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            Grid.SetColumn(spCurrent, 2);
            grid.Children.Add(spCurrent);

            // -------------------------------------------------------------
            // CỘT 3: ĐOẠN RAY NỐI PHÍA NAM (CỰ LY + ĐƯỜNG RAY + ĐỘ DỐC)
            // -------------------------------------------------------------
            var segNam = new Grid { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, 0) };
            segNam.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            segNam.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            segNam.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            if (rowNam != null)
            {
                decimal cuLyNam = Convert.ToDecimal(rowNam["CuLyKm"]);
                decimal doDocNam = Convert.ToDecimal(rowNam["DoDocPermil"]);
                bool dayTauNam = Convert.ToBoolean(rowNam["CanDauMayDay"]);

                // Cự ly (Trên ray)
                var txtCuLyNam = new TextBlock
                {
                    Text = $"{cuLyNam:0.0} km",
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 2)
                };
                Grid.SetRow(txtCuLyNam, 0);
                segNam.Children.Add(txtCuLyNam);

                // Đường ray nối ngang
                var trackNam = new Border
                {
                    Height = 3,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                    CornerRadius = new CornerRadius(1.5),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(trackNam, 1);
                segNam.Children.Add(trackNam);

                // Độ dốc & Cảnh báo (Dưới ray)
                var docPanelNam = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                if (dayTauNam)
                {
                    docPanelNam.Children.Add(new Wpf.Ui.Controls.SymbolIcon
                    {
                        Symbol = Wpf.Ui.Controls.SymbolRegular.Warning24,
                        FontSize = 10.5,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
                        Margin = new Thickness(0, 0, 3, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    docPanelNam.Children.Add(new TextBlock
                    {
                        Text = $"{doDocNam:0.0}‰",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C")),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    docPanelNam.ToolTip = $"Cần đầu máy đẩy (Độ dốc lớn {doDocNam:0.0}‰)";
                }
                else
                {
                    docPanelNam.Children.Add(new TextBlock
                    {
                        Text = doDocNam > 0 ? $"{doDocNam:0.0}‰" : "0‰",
                        FontSize = 10,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });
                }
                Grid.SetRow(docPanelNam, 2);
                segNam.Children.Add(docPanelNam);
            }
            else
            {
                // Đoạn ray trống ở cuối tuyến
                var trackEmpty = new Border
                {
                    Height = 2,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(trackEmpty, 1);
                segNam.Children.Add(trackEmpty);
            }
            Grid.SetColumn(segNam, 3);
            grid.Children.Add(segNam);

            // -------------------------------------------------------------
            // CỘT 4: GA PHÍA NAM (NÚT PHẢI)
            // -------------------------------------------------------------
            var spNam = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            if (rowNam != null)
            {
                string tenGaNam = rowNam["TenGaCuoi"]?.ToString() ?? "Ga Nam";
                decimal lyTrinhNam = rowNam.Table.Columns.Contains("LyTrinhGaCuoi") && rowNam["LyTrinhGaCuoi"] != DBNull.Value 
                    ? Convert.ToDecimal(rowNam["LyTrinhGaCuoi"]) : 0m;

                spNam.Children.Add(new TextBlock
                {
                    Text = tenGaNam,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 85
                });
                spNam.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    StrokeThickness = 2.5,
                    Margin = new Thickness(0, 3, 0, 3),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                spNam.Children.Add(new TextBlock
                {
                    Text = FormatHelper.FormatKm(lyTrinhNam),
                    FontSize = 10,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            else
            {
                // Ga ở cuối tuyến (Ga Sài Gòn)
                spNam.Children.Add(new TextBlock
                {
                    Text = "[CUỐI TUYẾN]",
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                spNam.Children.Add(new Border
                {
                    Width = 4,
                    Height = 12,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                    CornerRadius = new CornerRadius(1),
                    Margin = new Thickness(0, 3, 0, 3),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                spNam.Children.Add(new TextBlock
                {
                    Text = "Km 1,726.20",
                    FontSize = 10,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            Grid.SetColumn(spNam, 4);
            grid.Children.Add(spNam);

            card.Child = grid;
            return card;
        }

        /// <summary>
        /// Tạo dải thông số kỹ thuật phụ bên dưới sơ đồ ray (Vmax Khách/Hàng, máy đẩy).
        /// </summary>
        private UIElement? TaoThongSoKyThuatKhuGian(DataRow? rowBac, DataRow? rowNam)
        {
            if (rowBac == null && rowNam == null) return null;

            var sp = new StackPanel();

            if (rowBac != null)
            {
                string gaBac = rowBac["TenGaDau"]?.ToString() ?? "Ga Bắc";
                int vk = Convert.ToInt32(rowBac["TocDoToiDaKhach"]);
                int vh = Convert.ToInt32(rowBac["TocDoToiDaHang"]);
                bool dayTau = Convert.ToBoolean(rowBac["CanDauMayDay"]);

                var cardBac = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 5)
                };

                var spBacContent = new Grid();
                spBacContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                spBacContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var tbBac = new TextBlock { FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
                tbBac.Inlines.Add(new System.Windows.Documents.Run("Phía Bắc (↔ " + gaBac + "): ") 
                { 
                    FontWeight = FontWeights.Bold, 
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D4ED8")) 
                });
                tbBac.Inlines.Add(new System.Windows.Documents.Run($"Tốc độ Vmax {vk} km/h (khách), {vh} km/h (hàng)") 
                { 
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")) 
                });
                Grid.SetColumn(tbBac, 0);
                spBacContent.Children.Add(tbBac);

                if (dayTau)
                {
                    var bdBadge = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(6, 2, 6, 2),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    var tbBadge = new TextBlock
                    {
                        Text = "⚠ Cần máy đẩy",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"))
                    };
                    bdBadge.Child = tbBadge;
                    Grid.SetColumn(bdBadge, 1);
                    spBacContent.Children.Add(bdBadge);
                }

                cardBac.Child = spBacContent;
                sp.Children.Add(cardBac);
            }

            if (rowNam != null)
            {
                string gaNam = rowNam["TenGaCuoi"]?.ToString() ?? "Ga Nam";
                int vk = Convert.ToInt32(rowNam["TocDoToiDaKhach"]);
                int vh = Convert.ToInt32(rowNam["TocDoToiDaHang"]);
                bool dayTau = Convert.ToBoolean(rowNam["CanDauMayDay"]);

                var cardNam = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 3)
                };

                var spNamContent = new Grid();
                spNamContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                spNamContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var tbNam = new TextBlock { FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
                tbNam.Inlines.Add(new System.Windows.Documents.Run("Phía Nam (↔ " + gaNam + "): ") 
                { 
                    FontWeight = FontWeights.Bold, 
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D")) 
                });
                tbNam.Inlines.Add(new System.Windows.Documents.Run($"Tốc độ Vmax {vk} km/h (khách), {vh} km/h (hàng)") 
                { 
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")) 
                });
                Grid.SetColumn(tbNam, 0);
                spNamContent.Children.Add(tbNam);

                if (dayTau)
                {
                    var bdBadge = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(6, 2, 6, 2),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    var tbBadge = new TextBlock
                    {
                        Text = "⚠ Cần máy đẩy",
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"))
                    };
                    bdBadge.Child = tbBadge;
                    Grid.SetColumn(bdBadge, 1);
                    spNamContent.Children.Add(bdBadge);
                }

                cardNam.Child = spNamContent;
                sp.Children.Add(cardNam);
            }

            return sp;
        }

        private void BdBoxDuongRay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isAddingNew || _currentMaGa <= 0)
            {
                ThongBaoDialog.CanhBao("Ga chưa được lưu trong hệ thống. Vui lòng bấm [Lưu Ga Mới] trước khi cấu hình đường ray.", "Chưa Lưu Ga Mới");
                return;
            }
            tabInspector.SelectedIndex = 1;
        }

        private void BtnQuanLyKhuGian_Click(object sender, RoutedEventArgs e)
        {
            HienThiPopupTongHopKhuGian();
        }

        private void HienThiPopupTongHopKhuGian()
        {
            try
            {
                if (_currentMaGa <= 0)
                {
                    ThongBaoDialog.CanhBao("Vui lòng chọn một ga từ danh sách trước khi xem khu gian.", "Chưa Chọn Ga");
                    return;
                }

                FormatHelper.TryParseKm(txtLyTrinh.Text, out decimal lyTrinhKm);
                var dtKg = _gaService.LayKhuGianTheoGa(_currentMaGa);

                TongHopKhuGianGaDialog.HienThi(
                    Window.GetWindow(this),
                    _currentMaGa,
                    txtTenGa.Text.Trim(),
                    txtMaCode.Text.Trim(),
                    lyTrinhKm,
                    dtKg,
                    onMoPhanHe: () =>
                    {
                        if (Application.Current.MainWindow is MainWindow mw)
                        {
                            mw.NavigateTo("KhuGian");
                        }
                    });
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có sự cố khi mở danh sách khu gian liên kết:\n{ex.Message}", "Lỗi Hiển Thị");
            }
        }

        private void TabInspector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.Source == tabInspector && tabInspector.SelectedIndex == 1)
                {
                    if (_isAddingNew || _currentMaGa <= 0)
                    {
                        txtTenGaRayTab.Text = "HẠ TẦNG RAY GA (CHƯA LƯU)";
                        ResetHaTangRayChoGaMoi();
                    }
                    else
                    {
                        string prefixDirty = _isDirty ? " [Chưa Lưu Thông Tin Ga]" : "";
                        txtTenGaRayTab.Text = $"HẠ TẦNG RAY GA {txtTenGa.Text.Trim().ToUpper()}{prefixDirty}";
                        if (_dtCurrentDuongRay == null || _dtCurrentDuongRay.Rows.Count == 0)
                        {
                            TaiDanhSachDuongRayTrongGa(_currentMaGa);
                        }
                        else
                        {
                            CapNhatTrangThaiCacNutRay();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TabInspector_SelectionChanged] Lỗi chuyển tab: {ex.Message}");
            }
        }

        public void TaiDanhSachDuongRayTrongGa(int maGa)
        {
            if (maGa <= 0)
            {
                ResetHaTangRayChoGaMoi();
                return;
            }

            _isLoadingRay = true;
            try
            {
                _dtCurrentDuongRay = _duongRayService.LayDanhSachTheoGa(maGa);
                dgDuongRayGa.ItemsSource = _dtCurrentDuongRay?.DefaultView;

                int count = _dtCurrentDuongRay?.Rows.Count ?? 0;
                txtRayCountTab.Text = $"{count} ray";
                txtSoDuongRay.Text = $"{count} đường ray";

                int totalLength = 0;
                int passengerTracks = 0;
                if (_dtCurrentDuongRay != null)
                {
                    foreach (DataRow r in _dtCurrentDuongRay.Rows)
                    {
                        int len = r["ChieuDaiHuuDungM"] != DBNull.Value ? Convert.ToInt32(r["ChieuDaiHuuDungM"]) : 0;
                        totalLength += len;
                        bool hasKe = r["CoKeGa"] != DBNull.Value && Convert.ToBoolean(r["CoKeGa"]);
                        if (hasKe) passengerTracks++;
                    }
                }

                if (txtTongChieuDaiRay != null) txtTongChieuDaiRay.Text = $"{totalLength:N0} m";
                if (txtTongSucChuaGa != null) txtTongSucChuaGa.Text = $"~{Math.Max(0, totalLength / 25)} toa xe";
                if (txtTongKeGa != null) txtTongKeGa.Text = $"{passengerTracks} đường";

                if (txtTongSucChuaTab0 != null) txtTongSucChuaTab0.Text = $"~{Math.Max(0, totalLength / 25)} toa xe";
                if (txtTongKeGaTab0 != null) txtTongKeGaTab0.Text = $"{passengerTracks} ke ga";

                VeSoDoDuongRay();

                _isLoadingRay = false;

                if (_dtCurrentDuongRay != null && _dtCurrentDuongRay.Rows.Count > 0)
                {
                    dgDuongRayGa.SelectedIndex = -1;
                    dgDuongRayGa.SelectedIndex = 0;
                    if (dgDuongRayGa.SelectedItem is DataRowView drvFirst)
                    {
                        PopulateFormRay(drvFirst);
                    }
                    else if (_dtCurrentDuongRay.DefaultView.Count > 0)
                    {
                        PopulateFormRay(_dtCurrentDuongRay.DefaultView[0]);
                    }
                }
                else
                {
                    ClearFormRay(isNew: true);
                }

                CapNhatTrangThaiCacNutRay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TaiDanhSachDuongRayTrongGa] Lỗi nạp danh sách đường ray: {ex.Message}");
            }
            finally
            {
                _isLoadingRay = false;
            }
        }

        private void DgDuongRayGa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingRay) return;

            try
            {
                if (dgDuongRayGa.SelectedItem is DataRowView drv)
                {
                    PopulateFormRay(drv);
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi chọn đường ray:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void PopulateFormRay(DataRowView drv)
        {
            _isLoadingRay = true;
            try
            {
                _currentMaRay = Convert.ToInt32(drv["MaDuongRay"]);
                _isAddingRay = false;

                int sh = Convert.ToInt32(drv["SoHieuDuong"]);
                txtTitleFormRay.Text = $"THÔNG SỐ ĐƯỜNG RAY SỐ {sh}";
                if (txtBadgeFormRay != null) txtBadgeFormRay.Text = $"ĐƯỜNG {sh}";
                txtSoHieuRay.Text = sh.ToString();
                txtChieuDaiRay.Text = drv["ChieuDaiHuuDungM"]?.ToString() ?? "450";
                chkCoKeGaRay.IsChecked = drv["CoKeGa"] != DBNull.Value && Convert.ToBoolean(drv["CoKeGa"]);

                string loai = drv["LoaiDuong"]?.ToString() ?? "CHINH_TUYEN";
                cboLoaiDuongRay.SelectedIndex = loai switch
                {
                    "DUONG_TRANH" => 1,
                    "BOC_DO" => 2,
                    _ => 0
                };

                string tt = drv["TrangThai"]?.ToString() ?? "TRONG";
                cboTrangThaiRay.SelectedIndex = tt switch
                {
                    "CO_TAU" => 1,
                    "BAO_TRI" => 2,
                    _ => 0
                };

                txtLuuRay.Text = "Lưu Cấu Hình Ray";
                btnXoaRay.IsEnabled = true;

                // Cập nhật thông tin Audit kiểm toán của đường ray
                if (txtNguoiTaoRay != null)
                {
                    string creator = drv.Row.Table.Columns.Contains("NguoiTao") && drv["NguoiTao"] != DBNull.Value
                        ? drv["NguoiTao"].ToString()! : "admin";
                    DateTime createTime = drv.Row.Table.Columns.Contains("NgayTao") && drv["NgayTao"] != DBNull.Value
                        ? Convert.ToDateTime(drv["NgayTao"]) : DateTime.UtcNow;
                    txtNguoiTaoRay.Text = $"{creator} ({FormatHelper.FormatDateTime(createTime)})";
                }

                if (txtNgayCapNhatRay != null)
                {
                    if (drv.Row.Table.Columns.Contains("NgayCapNhat") && drv["NgayCapNhat"] != DBNull.Value)
                    {
                        string updater = drv.Row.Table.Columns.Contains("NguoiCapNhat") && drv["NguoiCapNhat"] != DBNull.Value
                            ? drv["NguoiCapNhat"].ToString()! : "admin";
                        DateTime updateTime = Convert.ToDateTime(drv["NgayCapNhat"]);
                        txtNgayCapNhatRay.Text = $"{updater} ({FormatHelper.FormatDateTime(updateTime)})";
                    }
                    else
                    {
                        txtNgayCapNhatRay.Text = "Chưa cập nhật";
                    }
                }

                CapNhatNangLucToa();
                VeSoDoDuongRay();
            }
            finally
            {
                _isLoadingRay = false;
            }
        }

        private void ClearFormRay(bool isNew)
        {
            _isLoadingRay = true;
            try
            {
                _isAddingRay = isNew;
                _currentMaRay = 0;
                txtTitleFormRay.Text = isNew ? "THÊM ĐƯỜNG RAY MỚI" : "THÔNG SỐ ĐƯỜNG RAY";
                if (txtBadgeFormRay != null) txtBadgeFormRay.Text = isNew ? "TẠO MỚI" : "ĐƯỜNG RAY";

                int maxSoHieu = 0;
                if (_dtCurrentDuongRay != null)
                {
                    foreach (DataRow r in _dtCurrentDuongRay.Rows)
                    {
                        int sh = Convert.ToInt32(r["SoHieuDuong"]);
                        if (sh > maxSoHieu) maxSoHieu = sh;
                    }
                }
                txtSoHieuRay.Text = (maxSoHieu + 1).ToString();
                txtChieuDaiRay.Text = "450";
                cboLoaiDuongRay.SelectedIndex = maxSoHieu == 0 ? 0 : 1; // Ray 1 là chính tuyến, các ray sau là đường tránh
                chkCoKeGaRay.IsChecked = true;
                cboTrangThaiRay.SelectedIndex = 0;

                if (txtNguoiTaoRay != null) txtNguoiTaoRay.Text = isNew ? "admin (Mới)" : "admin";
                if (txtNgayCapNhatRay != null) txtNgayCapNhatRay.Text = "Chưa cập nhật";

                txtLuuRay.Text = "Tạo Mới Đường Ray";
                btnXoaRay.IsEnabled = !isNew;
                CapNhatNangLucToa();
                CapNhatTrangThaiCacNutRay();
                txtSoHieuRay.Focus();
            }
            finally
            {
                _isLoadingRay = false;
            }
        }

        private void ResetHaTangRayChoGaMoi()
        {
            _isLoadingRay = true;
            try
            {
                _dtCurrentDuongRay = null;
                _currentMaRay = 0;
                _isAddingRay = false;

                if (dgDuongRayGa != null) dgDuongRayGa.ItemsSource = null;
                if (txtTongChieuDaiRay != null) txtTongChieuDaiRay.Text = "0 m";
                if (txtTongSucChuaGa != null) txtTongSucChuaGa.Text = "~0 toa xe";
                if (txtTongKeGa != null) txtTongKeGa.Text = "0 đường";
                if (txtTongSucChuaTab0 != null) txtTongSucChuaTab0.Text = "~0 toa xe";
                if (txtTongKeGaTab0 != null) txtTongKeGaTab0.Text = "0 ke ga";
                if (txtRayCountTab != null) txtRayCountTab.Text = "0 ray";
                if (txtSoDuongRay != null) txtSoDuongRay.Text = "0 đường ray";
                if (txtTenGaRayTab != null) txtTenGaRayTab.Text = "HẠ TẦNG RAY GA (MỚI)";

                if (pnlVisualTracks != null)
                {
                    pnlVisualTracks.Children.Clear();
                    pnlVisualTracks.Children.Add(new TextBlock
                    {
                        Text = "Ga này chưa được lưu. Vui lòng quay lại tab 'Thông Tin Ga' và bấm [Lưu Ga Mới] trước khi thêm đường ray.",
                        FontSize = 10,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                        FontStyle = FontStyles.Italic,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 4)
                    });
                }

                ClearFormRay(isNew: true);
                CapNhatTrangThaiCacNutRay();
            }
            finally
            {
                _isLoadingRay = false;
            }
        }

        private void CapNhatTrangThaiCacNutRay()
        {
            bool isSavedGa = (_currentMaGa > 0 && !_isAddingNew);

            if (btnThemRayMoi != null)
            {
                btnThemRayMoi.IsEnabled = isSavedGa;
                btnThemRayMoi.ToolTip = isSavedGa
                    ? "Thêm một đường ray mới vào ga đang chọn"
                    : "Vui lòng hoàn tất lưu Ga mới trước khi thêm đường ray";
            }

            if (btnLuuRay != null)
            {
                btnLuuRay.IsEnabled = isSavedGa;
                btnLuuRay.ToolTip = isSavedGa
                    ? "Lưu thông tin đường ray"
                    : "Vui lòng lưu Ga trước khi lưu đường ray";
            }

            if (btnXoaRay != null)
            {
                btnXoaRay.IsEnabled = isSavedGa && !_isAddingRay && _currentMaRay > 0;
            }

            if (txtLuuRay != null)
            {
                if (!isSavedGa)
                {
                    txtLuuRay.Text = "Cần Lưu Ga Trước";
                }
                else
                {
                    txtLuuRay.Text = _isAddingRay ? "Tạo Mới Đường Ray" : "Lưu Cấu Hình Ray";
                }
            }
        }

        private void CapNhatNangLucToa()
        {
            if (txtNangLucToa == null || txtGhiChuTacNghiep == null) return;

            int chieuDai = 450;
            if (txtChieuDaiRay != null)
            {
                string cdStr = (txtChieuDaiRay.Text ?? "").Replace(",", "").Trim();
                if (int.TryParse(cdStr, out int d) && d > 0)
                {
                    chieuDai = d;
                }
            }

            int soToa = Math.Max(1, chieuDai / 25);
            txtNangLucToa.Text = $"Sức chứa tiếp nhận: ~{soToa} toa xe tiêu chuẩn VNR (25m/toa)";

            int loaiIdx = cboLoaiDuongRay?.SelectedIndex ?? 0;
            txtGhiChuTacNghiep.Text = loaiIdx switch
            {
                0 => "Chính tuyến: Đón gửi tàu Thống Nhất SE1 - SE20 thông qua tốc độ cao",
                1 => $"Đường tránh: Phù hợp dừng tránh vượt cho đoàn tàu dài đến {soToa} toa",
                2 => "Đường bốc dỡ: Dồn dịch, dỡ hàng hóa container và toa hàng G/H",
                _ => "Tác nghiệp kỹ thuật ga"
            };
        }

        private void TxtChieuDaiRay_TextChanged(object sender, TextChangedEventArgs e)
        {
            CapNhatNangLucToa();
        }

        private void CboLoaiDuongRay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CapNhatNangLucToa();
        }

        #region Toggle Chế Độ Hiển Thị Đường Ray (Sơ đồ / Bảng)

        private void ToggleViewSoDo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isTrackViewGrid) return;
            _isTrackViewGrid = false;
            UpdateTrackViewMode();
        }

        private void ToggleViewBang_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isTrackViewGrid) return;
            _isTrackViewGrid = true;
            UpdateTrackViewMode();
        }

        private void UpdateTrackViewMode()
        {
            if (_isTrackViewGrid)
            {
                dgDuongRayGa.Visibility = Visibility.Visible;
                bdSoDoDuongRay.Visibility = Visibility.Collapsed;

                btnViewBang.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                btnViewBang.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                txtViewBang.Foreground = new SolidColorBrush(Colors.White);
                txtViewBang.FontWeight = FontWeights.Bold;

                btnViewSoDo.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                btnViewSoDo.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                txtViewSoDo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                txtViewSoDo.FontWeight = FontWeights.SemiBold;
            }
            else
            {
                dgDuongRayGa.Visibility = Visibility.Collapsed;
                bdSoDoDuongRay.Visibility = Visibility.Visible;

                btnViewSoDo.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                btnViewSoDo.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                txtViewSoDo.Foreground = new SolidColorBrush(Colors.White);
                txtViewSoDo.FontWeight = FontWeights.Bold;

                btnViewBang.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                btnViewBang.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                txtViewBang.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                txtViewBang.FontWeight = FontWeights.SemiBold;
            }
        }

        #endregion

        private void VeSoDoDuongRay()
        {
            if (pnlVisualTracks == null) return;
            pnlVisualTracks.Children.Clear();

            try
            {

            if (_isAddingNew || _currentMaGa <= 0)
            {
                pnlVisualTracks.Children.Add(new TextBlock
                {
                    Text = "Ga này chưa được lưu. Vui lòng quay lại tab 'Thông Tin Ga' và bấm [Lưu Ga Mới] trước khi thêm đường ray.",
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    FontStyle = FontStyles.Italic,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 4)
                });
                return;
            }

            if (_dtCurrentDuongRay == null || _dtCurrentDuongRay.Rows.Count == 0)
            {
                pnlVisualTracks.Children.Add(new TextBlock
                {
                    Text = "Ga chưa có dữ liệu đường ray nào",
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 4, 0, 4)
                });
                return;
            }

            int maxLen = 1;
            foreach (DataRow r in _dtCurrentDuongRay.Rows)
            {
                int len = r["ChieuDaiHuuDungM"] != DBNull.Value ? Convert.ToInt32(r["ChieuDaiHuuDungM"]) : 400;
                if (len > maxLen) maxLen = len;
            }

            foreach (DataRow r in _dtCurrentDuongRay.Rows)
            {
                int maRay = Convert.ToInt32(r["MaDuongRay"]);
                int soHieu = Convert.ToInt32(r["SoHieuDuong"]);
                string loai = r["LoaiDuong"]?.ToString() ?? "CHINH_TUYEN";
                int dai = r["ChieuDaiHuuDungM"] != DBNull.Value ? Convert.ToInt32(r["ChieuDaiHuuDungM"]) : 400;
                bool coKe = r["CoKeGa"] != DBNull.Value && Convert.ToBoolean(r["CoKeGa"]);
                string tt = r["TrangThai"]?.ToString() ?? "TRONG";

                bool isSelected = (_currentMaRay == maRay);

                var itemBorder = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    BorderThickness = isSelected ? new Thickness(3.5, 1, 1, 1) : new Thickness(1),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#003B73" : "#E2E8F0")),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#EFF6FF" : "#FFFFFF")),
                    Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 2, 0, 3),
                    Cursor = Cursors.Hand,
                    Tag = maRay
                };

                itemBorder.MouseLeftButtonUp += (s, e) =>
                {
                    ChonRayTheoId(maRay);
                };

                var itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });

                var spLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var bdNum = new Border
                {
                    Width = 18,
                    Height = 18,
                    CornerRadius = new CornerRadius(9),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#003B73" : "#F1F5F9")),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                bdNum.Child = new TextBlock
                {
                    Text = soHieu.ToString(),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#FFFFFF" : "#0F172A")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                spLeft.Children.Add(bdNum);

                spLeft.Children.Add(new TextBlock
                {
                    Text = $"Đường {soHieu}",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#003B73" : "#0F172A")),
                    VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn(spLeft, 0);
                itemGrid.Children.Add(spLeft);

                var spMid = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
                
                var trackGrid = new Grid { Height = 6, Margin = new Thickness(0, 0, 0, 2) };
                var trackBg = new Border
                {
                    Height = 4,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                    CornerRadius = new CornerRadius(2)
                };
                trackGrid.Children.Add(trackBg);

                double ratio = Math.Clamp((double)dai / maxLen, 0.35, 1.0);
                string trackColor = loai switch
                {
                    "CHINH_TUYEN" => "#003B73",
                    "BOC_DO" => "#D97706",
                    _ => "#4338CA"
                };
                var trackActive = new Border
                {
                    Height = 4,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = ratio * 135,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(trackColor)),
                    CornerRadius = new CornerRadius(2)
                };
                trackGrid.Children.Add(trackActive);
                spMid.Children.Add(trackGrid);

                var spSub = new StackPanel { Orientation = Orientation.Horizontal };

                string loaiText = loai switch { "CHINH_TUYEN" => "Chính Tuyến", "BOC_DO" => "Bốc Dỡ", _ => "Đường Tránh" };
                string loaiBgColor = loai switch { "CHINH_TUYEN" => "#F0FDF4", "BOC_DO" => "#FFFBEB", _ => "#EEF2FF" };
                string loaiBdrColor = loai switch { "CHINH_TUYEN" => "#BBF7D0", "BOC_DO" => "#FDE68A", _ => "#C7D2FE" };
                string loaiTxtColor = loai switch { "CHINH_TUYEN" => "#15803D", "BOC_DO" => "#B45309", _ => "#4338CA" };

                var bdLoaiBadge = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(loaiBgColor)),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(loaiBdrColor)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(4, 0, 4, 0),
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                bdLoaiBadge.Child = new TextBlock
                {
                    Text = loaiText,
                    FontSize = 8,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(loaiTxtColor))
                };
                spSub.Children.Add(bdLoaiBadge);

                spSub.Children.Add(new TextBlock
                {
                    Text = $"{dai}m",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    Margin = new Thickness(0, 0, 6, 0)
                });
                if (coKe)
                {
                    spSub.Children.Add(new TextBlock
                    {
                        Text = "Ke ga",
                        FontSize = 9,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D")),
                        Margin = new Thickness(0, 0, 6, 0)
                    });
                }
                spSub.Children.Add(new TextBlock
                {
                    Text = $"{Math.Max(1, dai / 25)} toa",
                    FontSize = 9,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
                });
                spMid.Children.Add(spSub);

                Grid.SetColumn(spMid, 1);
                itemGrid.Children.Add(spMid);

                var spStatus = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

                string stColor = tt switch
                {
                    "CO_TAU" => "#F59E0B",
                    "BAO_TRI" => "#EF4444",
                    _ => "#16A34A"
                };
                string stText = tt switch
                {
                    "CO_TAU" => "Có tàu",
                    "BAO_TRI" => "Bảo trì",
                    _ => "Trống"
                };
                spStatus.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(stColor)),
                    Margin = new Thickness(0, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
                spStatus.Children.Add(new TextBlock
                {
                    Text = stText,
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(stColor)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn(spStatus, 2);
                itemGrid.Children.Add(spStatus);

                itemBorder.Child = itemGrid;
                pnlVisualTracks.Children.Add(itemBorder);
            }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VeSoDoDuongRay] Lỗi vẽ sơ đồ đường ray: {ex.Message}");
            }
        }

        private void ChonRayTheoId(int maRay)
        {
            try
            {
                if (dgDuongRayGa.ItemsSource is DataView dv)
                {
                    for (int i = 0; i < dv.Count; i++)
                    {
                        if (Convert.ToInt32(dv[i]["MaDuongRay"]) == maRay)
                        {
                            if (dgDuongRayGa.SelectedIndex == i)
                            {
                                PopulateFormRay(dv[i]);
                            }
                            else
                            {
                                dgDuongRayGa.SelectedIndex = i;
                            }
                            dgDuongRayGa.ScrollIntoView(dgDuongRayGa.SelectedItem);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChonRayTheoId] Lỗi chọn ray {maRay}: {ex.Message}");
            }
        }

        private void BtnThemRayMoi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentMaGa <= 0 || _isAddingNew)
                {
                    ThongBaoDialog.CanhBao("Ga này chưa được lưu vào hệ thống. Vui lòng quay lại tab 'Thông Tin Ga' và bấm [Lưu Ga Mới] trước khi thêm đường ray.", "Chưa Lưu Ga Mới");
                    if (tabInspector != null) tabInspector.SelectedIndex = 0;
                    return;
                }
                ClearFormRay(isNew: true);
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi tạo đường ray mới:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void BtnHuyRay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgDuongRayGa.SelectedItem is DataRowView drv)
                {
                    PopulateFormRay(drv);
                }
                else
                {
                    ClearFormRay(isNew: true);
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi hủy thao tác đường ray:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void BtnLuuRay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentMaGa <= 0 || _isAddingNew)
                {
                    ThongBaoDialog.CanhBao("Ga chưa được lưu trong hệ thống. Vui lòng quay lại tab 'Thông Tin Ga' và bấm [Lưu Ga Mới] trước khi tạo đường ray.", "Chưa Lưu Ga Mới");
                    if (tabInspector != null) tabInspector.SelectedIndex = 0;
                    return;
                }

                if (!int.TryParse(txtSoHieuRay.Text.Trim(), out int soHieu) || soHieu <= 0)
                {
                    ThongBaoDialog.CanhBao("Số hiệu đường ray phải từ 1 trở lên (ví dụ: 1, 2, 3).", "Số Hiệu Không Hợp Lệ");
                    txtSoHieuRay.Focus();
                    return;
                }

                if (!int.TryParse(txtChieuDaiRay.Text.Replace(",", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int chieuDai) || chieuDai < 100 || chieuDai > 10000)
                {
                    ThongBaoDialog.CanhBao("Chiều dài đường ray phải từ 100 mét đến 10,000 mét.", "Chiều Dài Không Hợp Lệ");
                    txtChieuDaiRay.Focus();
                    return;
                }

                string loai = "CHINH_TUYEN";
                if (cboLoaiDuongRay.SelectedIndex == 1) loai = "DUONG_TRANH";
                else if (cboLoaiDuongRay.SelectedIndex == 2) loai = "BOC_DO";

                string tt = "TRONG";
                if (cboTrangThaiRay.SelectedIndex == 1) tt = "CO_TAU";
                else if (cboTrangThaiRay.SelectedIndex == 2) tt = "BAO_TRI";

                // Ràng buộc an toàn: Không cho phép tạo mới ray với trạng thái có tàu chiếm dụng
                if (_isAddingRay && tt == "CO_TAU")
                {
                    ThongBaoDialog.CanhBao("Đường ray mới tạo không được thiết lập trạng thái 'Đang có đoàn tàu chiếm dụng'. Vui lòng chọn 'Trống' hoặc 'Tạm đóng bảo trì kỹ thuật'.", "Trạng Thái Không Hợp Lệ");
                    cboTrangThaiRay.Focus();
                    return;
                }

                // Khuyến nghị an toàn: Nếu đường tránh < 250m
                if (loai == "DUONG_TRANH" && chieuDai < 250)
                {
                    bool tiepTuc = ThongBaoDialog.XacNhan(
                        $"Chiều dài đường tránh ({chieuDai} m) nhỏ hơn quy chuẩn đón gửi đoàn tàu tiêu chuẩn (>= 250 m).\n\nĐường ray này có thể không đủ chứa đoàn tàu khách/hàng thông thường.\n\nBạn có chắc chắn muốn lưu thông số này?",
                        "Cảnh Báo Dung Lượng Đoàn Tàu");
                    if (!tiepTuc)
                    {
                        txtChieuDaiRay.Focus();
                        return;
                    }
                }

                var ray = new DuongRayGa
                {
                    MaDuongRay = _currentMaRay,
                    MaGa = _currentMaGa,
                    SoHieuDuong = soHieu,
                    LoaiDuong = loai,
                    ChieuDaiHuuDungM = chieuDai,
                    CoKeGa = chkCoKeGaRay.IsChecked == true,
                    TrangThai = tt,
                    NguoiTao = "admin",
                    NguoiCapNhat = "admin"
                };

                if (_isAddingRay)
                {
                    bool thanhCong = _duongRayService.Them(ray, out string loi);
                    if (thanhCong)
                    {
                        ThongBaoDialog.ThanhCong($"Đã thêm mới Đường ray số {soHieu} vào Ga {txtTenGa.Text.Trim()}!", "Thành Công");
                        TaiChiTietHaTangGa(_currentMaGa);
                        LoadData();
                    }
                    else
                    {
                        ThongBaoDialog.Loi(loi, "Lỗi Thêm Mới Ray");
                    }
                }
                else
                {
                    bool thanhCong = _duongRayService.CapNhat(ray, out string loi);
                    if (thanhCong)
                    {
                        ThongBaoDialog.ThanhCong($"Cập nhật thông số Đường ray số {soHieu} thành công!", "Lưu Thành Công");
                        TaiChiTietHaTangGa(_currentMaGa);
                        LoadData();
                    }
                    else
                    {
                        ThongBaoDialog.Loi(loi, "Lỗi Cập Nhật Ray");
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi lưu thông số đường ray:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void BtnXoaRay_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaRay <= 0) return;

            if (cboTrangThaiRay.SelectedIndex == 1)
            {
                ThongBaoDialog.CanhBao("Không thể xóa đường ray này vì đang có đoàn tàu hoạt động / chiếm dụng trên đường. Vui lòng giải phóng tàu trước khi xóa hạ tầng.", "Vi Phạm An Toàn");
                return;
            }

            // Ràng buộc hạ tầng đường sắt: Chặn xóa đường chính tuyến duy nhất trong ga
            if (cboLoaiDuongRay.SelectedIndex == 0) // CHINH_TUYEN
            {
                int countChinhTuyen = 0;
                if (_dtCurrentDuongRay != null)
                {
                    foreach (DataRow r in _dtCurrentDuongRay.Rows)
                    {
                        if (r.RowState != DataRowState.Deleted && r["LoaiDuong"]?.ToString() == "CHINH_TUYEN")
                        {
                            countChinhTuyen++;
                        }
                    }
                }
                if (countChinhTuyen <= 1)
                {
                    ThongBaoDialog.CanhBao("Không thể xóa đường chính tuyến duy nhất của ga. Mọi ga trên mạng lưới bắt buộc phải duy trì tối thiểu 01 đường ray chính tuyến để thông tàu toàn tuyến.", "Ràng Buộc Hạ Tầng Bắt Buộc");
                    return;
                }
            }

            try
            {
                bool xacNhan = ThongBaoDialog.XacNhan(
                    $"Bạn có chắc muốn xóa {txtTitleFormRay.Text} của Ga {txtTenGa.Text.Trim()} không?\n\nLưu ý: Không thể hoàn tác hành động này.",
                    "Xác Nhận Xóa Đường Ray");

                if (xacNhan)
                {
                    bool thanhCong = _duongRayService.Xoa(_currentMaRay, out string loi);
                    if (thanhCong)
                    {
                        _currentMaRay = 0;
                        ClearFormRay(isNew: false);
                        ThongBaoDialog.ThanhCong("Đã xóa đường ray thành công khỏi ga.", "Đã Xóa");
                        TaiDanhSachDuongRayTrongGa(_currentMaGa);
                        TaiChiTietHaTangGa(_currentMaGa);
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
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi xóa đường ray:\n{ex.Message}", "Lỗi Thao Tác");
            }
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

        // Bộ lọc 
        private void ApplyAdvancedFilter()
        {
            if (_cachedTable == null) return;

            try
            {
                var conditions = new List<string>();

                // 1. Tìm kiếm từ khóa (Tên ga, mã code, tỉnh thành, lý trình Km - có dấu & không dấu, an toàn ký tự đặc biệt)
                string rawKw = txtTimKiem?.Text?.Trim() ?? "";
                if (rawKw.Length > 100)
                {
                    rawKw = rawKw.Substring(0, 100);
                }

                if (!string.IsNullOrEmpty(rawKw))
                {
                    string kwSafe = EscapeLikeFilter(rawKw.ToLowerInvariant());
                    string kwUnaccent = EscapeLikeFilter(FormatHelper.RemoveDiacritics(rawKw).ToLowerInvariant());

                    conditions.Add($"(SearchIndex LIKE '%{kwSafe}%' OR SearchIndex LIKE '%{kwUnaccent}%')");
                }

                // 2. Lọc theo Phân Cấp Ga
                if (cboLocHangGa != null && cboLocHangGa.SelectedIndex > 0)
                {
                    if (cboLocHangGa.SelectedIndex == 1) conditions.Add("HangGa = 'HANG_1'");
                    else if (cboLocHangGa.SelectedIndex == 2) conditions.Add("HangGa = 'HANG_2'");
                    else if (cboLocHangGa.SelectedIndex == 3) conditions.Add("HangGa = 'HANG_3'");
                }

                // 3. Lọc theo Khai Thác
                if (cboLocKhaiThac != null && cboLocKhaiThac.SelectedIndex > 0)
                {
                    if (cboLocKhaiThac.SelectedIndex == 1) conditions.Add("DangKhaiThac = true");
                    else if (cboLocKhaiThac.SelectedIndex == 2) conditions.Add("DangKhaiThac = false");
                }

                // 4. Lọc Cầu Quay
                if (chkLocCauQuay != null && chkLocCauQuay.IsChecked == true)
                {
                    conditions.Add("CoCauQuay = true");
                }

                // Cập nhật trạng thái thị giác Active Indicator cho các bộ lọc
                if (cboLocHangGa != null)
                {
                    if (cboLocHangGa.SelectedIndex > 0)
                    {
                        cboLocHangGa.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#0284C7")!;
                        cboLocHangGa.Background = (Brush)new BrushConverter().ConvertFrom("#F0F9FF")!;
                    }
                    else
                    {
                        cboLocHangGa.ClearValue(Control.BorderBrushProperty);
                        cboLocHangGa.ClearValue(Control.BackgroundProperty);
                    }
                }

                if (cboLocKhaiThac != null)
                {
                    if (cboLocKhaiThac.SelectedIndex > 0)
                    {
                        cboLocKhaiThac.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#0284C7")!;
                        cboLocKhaiThac.Background = (Brush)new BrushConverter().ConvertFrom("#F0F9FF")!;
                    }
                    else
                    {
                        cboLocKhaiThac.ClearValue(Control.BorderBrushProperty);
                        cboLocKhaiThac.ClearValue(Control.BackgroundProperty);
                    }
                }

                if (bdChipCauQuay != null && chkLocCauQuay != null)
                {
                    if (chkLocCauQuay.IsChecked == true)
                    {
                        bdChipCauQuay.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#0284C7")!;
                        bdChipCauQuay.Background = (Brush)new BrushConverter().ConvertFrom("#F0F9FF")!;
                        chkLocCauQuay.Foreground = (Brush)new BrushConverter().ConvertFrom("#0369A1")!;
                    }
                    else
                    {
                        bdChipCauQuay.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#CBD5E1")!;
                        bdChipCauQuay.Background = Brushes.White;
                        chkLocCauQuay.Foreground = (Brush)new BrushConverter().ConvertFrom("#334155")!;
                    }
                }

                try
                {
                    _cachedTable.DefaultView.RowFilter = conditions.Count > 0 ? string.Join(" AND ", conditions) : "";
                }
                catch
                {
                    _cachedTable.DefaultView.RowFilter = "";
                }

                // Hiển thị nút "Hủy Bộ Lọc" nếu có bất kỳ điều kiện lọc nào đang chạy
                bool isFiltering = conditions.Count > 0;
                if (btnXoaTatCaLoc != null)
                {
                    btnXoaTatCaLoc.Visibility = isFiltering ? Visibility.Visible : Visibility.Collapsed;
                }

                _currentPage = 1;
                ApplyPaging();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MangLuoiGaPage.ApplyAdvancedFilter] Lỗi: {ex.Message}");
            }
        }

        private void TxtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (txtTimKiem != null && txtTimKiem.Text.Length > 100)
                {
                    txtTimKiem.Text = txtTimKiem.Text.Substring(0, 100);
                    txtTimKiem.CaretIndex = 100;
                    return;
                }

                bool coChu = !string.IsNullOrEmpty(txtTimKiem?.Text);
                if (btnXoaTimKiem != null)
                {
                    btnXoaTimKiem.Visibility = coChu ? Visibility.Visible : Visibility.Collapsed;
                }
                if (bdShortcutSearch != null)
                {
                    bdShortcutSearch.Visibility = coChu ? Visibility.Collapsed : Visibility.Visible;
                }
                if (txtWatermark != null)
                {
                    txtWatermark.Visibility = coChu ? Visibility.Collapsed : Visibility.Visible;
                }
                ApplyAdvancedFilter();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MangLuoiGaPage.TxtTimKiem_TextChanged] Lỗi: {ex.Message}");
            }
        }

        private void BtnXoaTimKiem_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Text = "";
            btnXoaTimKiem.Visibility = Visibility.Collapsed;
            if (bdShortcutSearch != null) bdShortcutSearch.Visibility = Visibility.Visible;
            if (txtWatermark != null) txtWatermark.Visibility = Visibility.Visible;
            txtTimKiem.Focus();
        }

        private void CboLoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingData) return;
            ApplyAdvancedFilter();
        }

        private void ChkLocCauQuay_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingData) return;
            ApplyAdvancedFilter();
        }

        private void BtnXoaTatCaLoc_Click(object sender, RoutedEventArgs e)
        {
            _isLoadingData = true;
            txtTimKiem.Text = "";
            if (btnXoaTimKiem != null) btnXoaTimKiem.Visibility = Visibility.Collapsed;
            if (bdShortcutSearch != null) bdShortcutSearch.Visibility = Visibility.Visible;
            if (txtWatermark != null) txtWatermark.Visibility = Visibility.Visible;
            if (cboLocHangGa != null) cboLocHangGa.SelectedIndex = 0;
            if (cboLocKhaiThac != null) cboLocKhaiThac.SelectedIndex = 0;
            if (chkLocCauQuay != null) chkLocCauQuay.IsChecked = false;
            _isLoadingData = false;

            ApplyAdvancedFilter();
        }

        // Nhập file Excel / CSV hàng loạt dùng chung 
        private void BtnNhapFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profile = new GaImportProfile(_gaService);
                var dlg = new CommonImportDialog(profile)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dlg.ShowDialog() == true && dlg.SoBanGhiDaNapThanhCong > 0)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể khởi động tiến trình nhập tệp ga:\n{ex.Message}", "Lỗi Nhập Tệp");
            }
        }

        // Xuất file Excel / CSV qua dùng chung
        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cachedTable == null || _cachedTable.DefaultView.Count == 0)
                {
                    ThongBaoDialog.ThongTin("Không có dữ liệu ga để xuất file.", "Thông Báo");
                    return;
                }

                var colMap = new Dictionary<string, string>
                {
                    { "STT", "STT" },
                    { "MaGaCode", "Mã Ga" },
                    { "TenGa", "Tên Ga" },
                    { "LyTrinhKm", "Lý Trình (km)" },
                    { "TinhThanh", "Tỉnh / Thành Phố" },
                    { "HangGa", "Phân Cấp Ga" },
                    { "SoDuongRay", "Số Đường Ray" },
                    { "CoCauQuay", "Cầu Quay" },
                    { "DangKhaiThac", "Trạng Thái Khai Thác" }
                };

                FileExchangeHelper.XuatExcel(
                    _cachedTable.DefaultView,
                    colMap,
                    $"DanhSachGa_BacNam_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                    "BÁO CÁO DANH SÁCH GA ĐƯỜNG SẮT BẮC - NAM",
                    "DanhSachGa");
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi xuất tệp Excel danh sách ga:\n{ex.Message}", "Lỗi Xuất Tệp");
            }
        }

        private static string EscapeCsv(string? val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n") || val.Contains("\r"))
            {
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            }
            return val;
        }

        // Mở trung tâm báo cáo thống kê
        private void BtnInBaoCao_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_cachedTable == null || _cachedTable.Rows.Count == 0)
                {
                    ThongBaoDialog.CanhBao("Danh sách ga đang trống, không thể in báo cáo.", "Thông Báo", Window.GetWindow(this));
                    return;
                }

                var winOwner = Window.GetWindow(this);
                if (winOwner == null || !winOwner.IsLoaded || !winOwner.IsVisible)
                {
                    winOwner = Application.Current?.MainWindow;
                }

                var dialog = new BaoCaoGaDialog(_cachedTable);
                if (winOwner != null && winOwner.IsLoaded && winOwner.IsVisible)
                {
                    dialog.Owner = winOwner;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                var mw = winOwner as MainWindow;
                mw?.ShowModalOverlay();
                try
                {
                    dialog.ShowDialog();
                }
                finally
                {
                    mw?.HideModalOverlay();
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể mở cửa sổ báo cáo: {ex.Message}", "Lỗi Khởi Tạo", Window.GetWindow(this));
            }
        }

        private void CtxCopyCode_Click(object sender, RoutedEventArgs e)
        {
            if (dgGa.SelectedItem is DataRowView drv)
            {
                string code = drv["MaGaCode"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(code))
                {
                    Clipboard.SetText(code);
                }
            }
        }

        private void CtxCopyTenKm_Click(object sender, RoutedEventArgs e)
        {
            if (dgGa.SelectedItem is DataRowView drv)
            {
                string ten = drv["TenGa"]?.ToString() ?? "";
                decimal km = drv["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(drv["LyTrinhKm"]) : 0m;
                Clipboard.SetText($"Ga {ten} — Km {FormatHelper.FormatKm(km)}");
            }
        }

        private void CtxCopyRow_Click(object sender, RoutedEventArgs e)
        {
            if (dgGa.SelectedItem is DataRowView drv)
            {
                var r = drv.Row;
                decimal km = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m;
                string line = $"{r["MaGaCode"]}\t{r["TenGa"]}\t{FormatHelper.FormatKm(km)}\t{r["TinhThanh"]}\t{r["HangGa"]}";
                Clipboard.SetText(line);
            }
        }

        private void CtxThemDuongRay_Click(object sender, RoutedEventArgs e)
        {
            if (dgGa.SelectedItem is DataRowView)
            {
                if (tabInspector != null)
                {
                    tabInspector.SelectedIndex = 1;
                }
                ClearFormRay(isNew: true);
            }
        }

        private void CtxXemKhuGian_Click(object sender, RoutedEventArgs e)
        {
            if (dgGa.SelectedItem is DataRowView)
            {
                HienThiPopupTongHopKhuGian();
            }
        }

        private void CtxXoaGa_Click(object sender, RoutedEventArgs e)
        {
            CrudActionBar_DeleteClicked(sender, e);
        }

        private void CtxDoiTrangThai_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgGa.SelectedItem is DataRowView drv)
                {
                    int maGa = Convert.ToInt32(drv["MaGa"]);
                    string tenGa = drv["TenGa"]?.ToString() ?? "";
                    bool hienTai = drv["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(drv["DangKhaiThac"]);
                    bool moi = !hienTai;
                    string actionStr = moi ? "MỞ LẠI KHAI THÁC" : "TẠM NGỪNG KHAI THÁC";

                    // Kiểm tra an toàn trước khi chuyển sang tạm ngừng
                    if (!moi)
                    {
                        if (_gaService.KiemTraGaDangCoTau(maGa))
                        {
                            ThongBaoDialog.CanhBao($"Ga {tenGa} hiện đang có đoàn tàu hoạt động trên đường ray trong ga. Không thể chuyển sang 'Tạm ngừng khai thác' khi chưa giải phóng tàu khỏi ga.", "Vi Phạm An Toàn");
                            return;
                        }

                        var dtKg = _gaService.LayKhuGianTheoGa(maGa);
                        int soKg = dtKg?.Rows.Count ?? 0;
                        if (soKg > 0)
                        {
                            bool confirmKg = ThongBaoDialog.XacNhan(
                                $"Ga {tenGa} hiện đang kết nối với {soKg} khu gian trên tuyến.\n\nTạm ngừng ga này có thể ảnh hưởng đến lịch chạy tàu.\n\nBạn có chắc muốn chuyển ga này sang TẠM NGỪNG KHAI THÁC không?",
                                "Xác Nhận Tạm Ngừng Ga",
                                nutDongY: "Tạm ngừng",
                                nutHuy: "Hủy bỏ");
                            if (!confirmKg) return;
                        }
                    }
                    else
                    {
                        bool ask = ThongBaoDialog.XacNhan(
                            $"Bạn có chắc chắn muốn {actionStr} cho Ga {tenGa} không?",
                            "Xác Nhận Đổi Trạng Thái",
                            nutDongY: "Xác nhận",
                            nutHuy: "Hủy bỏ");
                        if (!ask) return;
                    }

                    var ga = new Ga
                    {
                        MaGa = maGa,
                        TenGa = tenGa,
                        LyTrinhKm = drv["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(drv["LyTrinhKm"]) : 0m,
                        TinhThanh = drv["TinhThanh"]?.ToString() ?? "",
                        HangGa = drv["HangGa"]?.ToString() ?? "HANG_3",
                        CoCauQuay = drv["CoCauQuay"] != DBNull.Value && Convert.ToBoolean(drv["CoCauQuay"]),
                        DangKhaiThac = moi,
                        NguoiCapNhat = "admin"
                    };
                    _gaService.CapNhat(ga);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi("Lỗi khi đổi trạng thái: " + ex.Message, "Lỗi Thao Tác");
            }
        }

        private void CtxXemNhatKy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgGa.SelectedItem is DataRowView drv)
                {
                    string code = drv["MaGaCode"]?.ToString() ?? "";
                    string ten = drv["TenGa"]?.ToString() ?? "";
                    var winOwner = Window.GetWindow(this);
                    if (winOwner == null || !winOwner.IsLoaded || !winOwner.IsVisible)
                    {
                        winOwner = Application.Current?.MainWindow;
                    }

                    var dialog = new LichSuGaDialog(code, ten);
                    if (winOwner != null && winOwner.IsLoaded && winOwner.IsVisible)
                    {
                        dialog.Owner = winOwner;
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    }
                    else
                    {
                        dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    }
                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể mở nhật ký lịch sử ga:\n{ex.Message}", "Lỗi Khởi Tạo");
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

        // Context menu
        private void CtxCopyRaySoHieu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgDuongRayGa.SelectedItem is DataRowView drv)
                {
                    string sh = drv["SoHieuDuong"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(sh)) Clipboard.SetText($"Đường {sh}");
                }
            }
            catch
            {
            }
        }

        private void CtxCopyRayInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgDuongRayGa.SelectedItem is DataRowView drv)
                {
                    string sh = drv["SoHieuDuong"]?.ToString() ?? "";
                    string loai = drv["TenLoaiDuong"]?.ToString() ?? drv["LoaiDuong"]?.ToString() ?? "";
                    string dai = drv["ChieuDaiHuuDungM"]?.ToString() ?? "";
                    string tt = drv["TenTrangThai"]?.ToString() ?? drv["TrangThai"]?.ToString() ?? "";
                    string ke = (drv["CoKeGa"] != DBNull.Value && Convert.ToBoolean(drv["CoKeGa"])) ? "Có ke ga" : "Không có ke";
                    Clipboard.SetText($"Đường ray số {sh} — Loại: {loai} — Chiều dài: {dai}m — {ke} — Trạng thái: {tt}");
                }
            }
            catch
            {
            }
        }

        private void CtxDoiTrangThaiRay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgDuongRayGa.SelectedItem is DataRowView drv)
                {
                    int maRay = Convert.ToInt32(drv["MaDuongRay"]);
                    int sh = Convert.ToInt32(drv["SoHieuDuong"]);
                    string currentTt = drv["TrangThai"]?.ToString() ?? "TRONG";

                    if (currentTt == "CO_TAU")
                    {
                        ThongBaoDialog.CanhBao($"Đường ray số {sh} đang có đoàn tàu chiếm dụng. Không thể đổi nhanh trạng thái khi chưa giải phóng tàu.", "Cảnh Báo An Toàn");
                        return;
                    }

                    string newTt = currentTt == "TRONG" ? "BAO_TRI" : "TRONG";
                    string newTtName = newTt == "TRONG" ? "Trống sẵn sàng" : "Tạm đóng bảo trì kỹ thuật";

                    bool ok = ThongBaoDialog.XacNhan(
                        $"Bạn có muốn đổi trạng thái Đường ray số {sh} sang '{newTtName}' không?",
                        "Đổi Trạng Thái Đường Ray");

                    if (ok)
                    {
                        var ray = new DuongRayGa
                        {
                            MaDuongRay = maRay,
                            MaGa = _currentMaGa,
                            SoHieuDuong = sh,
                            LoaiDuong = drv["LoaiDuong"]?.ToString() ?? "DUONG_TRANH",
                            ChieuDaiHuuDungM = Convert.ToInt32(drv["ChieuDaiHuuDungM"]),
                            CoKeGa = drv["CoKeGa"] != DBNull.Value && Convert.ToBoolean(drv["CoKeGa"]),
                            TrangThai = newTt
                        };

                        if (_duongRayService.CapNhat(ray, out string loi))
                        {
                            ThongBaoDialog.ThanhCong($"Đã cập nhật trạng thái Đường ray số {sh} thành '{newTtName}'.", "Thành Công");
                            TaiChiTietHaTangGa(_currentMaGa);
                            LoadData();
                        }
                        else
                        {
                            ThongBaoDialog.Loi(loi, "Lỗi Cập Nhật Ray");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Đã xảy ra sự cố khi đổi trạng thái đường ray:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        private void CtxLamMoiRay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentMaGa > 0)
                {
                    TaiDanhSachDuongRayTrongGa(_currentMaGa);
                    TaiChiTietHaTangGa(_currentMaGa);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CtxLamMoiRay_Click] Lỗi: {ex.Message}");
            }
        }

        private void CtxXoaRayItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgDuongRayGa.SelectedItem is DataRowView drv)
                {
                    PopulateFormRay(drv);
                    BtnXoaRay_Click(sender, e);
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Có lỗi khi xóa đường ray:\n{ex.Message}", "Lỗi Thao Tác");
            }
        }

        // Phím tắt
        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
                {
                    e.Handled = true;
                    if (crudActionBar.btnLuu.IsEnabled)
                    {
                        CrudActionBar_SaveClicked(sender, e);
                    }
                }
                else if ((Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.K || e.Key == Key.F)) || e.Key == Key.F3)
                {
                    e.Handled = true;
                    txtTimKiem.Focus();
                    txtTimKiem.SelectAll();
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
                {
                    e.Handled = true;
                    BtnThemGaMoi_Click(sender, e);
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
                {
                    e.Handled = true;
                    BtnXuatExcel_Click(sender, e);
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
                {
                    e.Handled = true;
                    BtnInBaoCao_Click(sender, e);
                }
                else if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    if (!string.IsNullOrEmpty(txtTimKiem.Text))
                    {
                        BtnXoaTatCaLoc_Click(sender, e);
                    }
                    else if (crudActionBar.btnHuy.IsEnabled)
                    {
                        CrudActionBar_CancelClicked(sender, e);
                    }
                }
                else if (e.Key == Key.F2)
                {
                    e.Handled = true;
                    BtnThemGaMoi_Click(sender, e);
                }
                else if (e.Key == Key.F5)
                {
                    e.Handled = true;
                    _isDirty = false;
                    BtnXoaTatCaLoc_Click(sender, e);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MangLuoiGaPage.Page_PreviewKeyDown] Lỗi phím tắt: {ex.Message}");
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

        public void KichHoatThemMoi() => BtnThemGaMoi_Click(this, new RoutedEventArgs());
        
        public void KichHoatNapLai()
        {
            _isDirty = false;
            BtnXoaTatCaLoc_Click(this, new RoutedEventArgs());
            LoadData();
        }

        public void KichHoatNhapTep() => BtnNhapFile_Click(this, new RoutedEventArgs());
        public void KichHoatXuatTep() => BtnXuatExcel_Click(this, new RoutedEventArgs());
        public void KichHoatBaoCao() => BtnInBaoCao_Click(this, new RoutedEventArgs());

        public void KichHoatHuy()
        {
            if (!string.IsNullOrEmpty(txtTimKiem.Text))
            {
                BtnXoaTatCaLoc_Click(this, new RoutedEventArgs());
            }
            else if (crudActionBar.btnHuy.IsEnabled)
            {
                CrudActionBar_CancelClicked(this, new RoutedEventArgs());
            }
        }
    }
}
