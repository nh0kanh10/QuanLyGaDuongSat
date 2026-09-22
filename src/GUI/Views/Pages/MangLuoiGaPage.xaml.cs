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

        // Trạng thái quản lý đường ray trong ga
        private DataTable? _dtCurrentDuongRay;
        private int _currentMaRay = 0;
        private bool _isAddingRay = false;
        private bool _isLoadingRay = false;
        private bool _isTrackViewGrid = false;

        // Phân trang: Mặc định 50 dòng 
        private int _currentPage = 1;
        private int _pageSize = 50;

        public MangLuoiGaPage()
        {
            InitializeComponent();
            _gaService = new GaService();
            Loaded += MangLuoiGaPage_Loaded;

            // Ràng buộc nhập liệu & Tự động định dạng
            SmartNumberFormatHelper.AttachDecimalKm(txtLyTrinh, min: 0m, max: 5000m);
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
                    for (int i = 0; i < _cachedTable.Rows.Count; i++)
                    {
                        _cachedTable.Rows[i]["STT"] = i + 1;
                    }
                }

                _currentPage = 1;
                ApplyPaging();
            }
            finally
            {
                _isLoadingData = false;
            }

            if (dgGa.Items.Count > 0)
            {
                dgGa.SelectedIndex = 0;
            }
            else
            {
                ClearFormForNew();
            }
        }

        private void ApplyPaging()
        {
            if (_cachedTable == null) return;

            var view = _cachedTable.DefaultView;
            int totalItems = view.Count;
            txtTongSo.Text = $"{totalItems} ga";

            // Cập nhật thông tin trên component phân trang
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
                    "Dữ liệu ga hiện tại đang có thay đổi chưa được lưu vào CSDL.\n\nBạn có muốn hủy bỏ các thay đổi này để chuyển sang ga khác không?",
                    "Xác Nhận Chuyển Dòng",
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

            if (_isAddingNew)
            {
                _isAddingNew = false;
            }

            if (dgGa.SelectedItem is DataRowView row)
            {
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
                string tinhThanh = row["TinhThanh"]?.ToString() ?? "";

                txtMaCode.Text = maCode;
                txtTenGa.Text = tenGa;
                // Thống nhất định dạng số qua FormatHelper dùng chung hệ thống
                txtLyTrinh.Text = FormatHelper.FormatKm(lyTrinh);
                cboTinhThanh.Text = tinhThanh;

                string hang = row["HangGa"]?.ToString() ?? "";
                if (hang == "HANG_1") cboHangGa.SelectedIndex = 0;
                else if (hang == "HANG_2") cboHangGa.SelectedIndex = 1;
                else cboHangGa.SelectedIndex = 2;

                chkCoCauQuay.IsChecked = row["CoCauQuay"] != DBNull.Value && Convert.ToBoolean(row["CoCauQuay"]);
                chkDangKhaiThac.IsChecked = row["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(row["DangKhaiThac"]);
                UpdateKhaiThacLabel();

                // Cập nhật Thước Đo Mốc Lý Trình Tuyến Bắc - Nam Trực Quan
                if (pbLyTrinh != null)
                {
                    pbLyTrinh.Value = (double)lyTrinh;
                }
                if (txtThongTinLyTrinh != null)
                {
                    double pt = lyTrinh > 0 ? (double)(lyTrinh / 1726.2m * 100m) : 0;
                    txtThongTinLyTrinh.Text = $"Vị trí: Ga {tenGa} ({maCode}) — Km {FormatHelper.FormatKm(lyTrinh)} ({pt:0.0}% trục Bắc - Nam)";
                }

                // Đổ dữ liệu hạ tầng thực tế từ SQL Server
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

                // Đổ dữ liệu kiểm toán thật từ CSDL
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

                _isDirty = false;
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void ClearFormForNew()
        {
            _isAddingNew = true;
            _isLoadingData = true;
            try
            {
                _currentMaGa = 0;
                txtMaCode.Text = "";
                txtTenGa.Text = "";
                txtLyTrinh.Text = "0.00";
                cboTinhThanh.Text = "Hà Nội";
                cboHangGa.SelectedIndex = 0;
                chkCoCauQuay.IsChecked = false;
                chkDangKhaiThac.IsChecked = true;
                UpdateKhaiThacLabel();

                txtSoDuongRay.Text = "Chưa khởi tạo";
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
                ClearFormRay(isNew: true);
                txtRayCountTab.Text = "0 ray";
                txtSoDuongRay.Text = "0 đường ray";
                txtNguoiTao.Text = "admin (Mới)";
                txtNgayCapNhat.Text = "Chưa ghi nhận";

                inspectorHeader.SetCreateMode();
                crudActionBar.SetSaveMode(isAddingNew: true);
                crudActionBar.SetSaveEnabled(true);
                crudActionBar.SetCancelEnabled(true);
                crudActionBar.SetDeleteEnabled(false);

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

        private void FormInput_Changed(object sender, TextChangedEventArgs e) => MarkDirty();
        private void FormCombo_Changed(object sender, SelectionChangedEventArgs e) => MarkDirty();
        private void FormCheck_Changed(object sender, RoutedEventArgs e) => MarkDirty();

        private void ChkDangKhaiThac_Click(object sender, RoutedEventArgs e)
        {
            UpdateKhaiThacLabel();
            MarkDirty();
        }

        private void UpdateKhaiThacLabel()
        {
            if (chkDangKhaiThac != null)
            {
                chkDangKhaiThac.Content = chkDangKhaiThac.IsChecked == true 
                    ? "● Đang khai thác" 
                    : "○ Tạm ngừng khai thác";
            }
        }

        /// <summary>
        /// Tự động chuẩn hóa hiển thị về dấu chấm thập phân "688.00" chuẩn kỹ thuật khi rời ô nhập.
        /// Chống mâu thuẫn hiển thị giữa DataGrid và Form chi tiết.
        /// </summary>
        private void TxtLyTrinh_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FormatHelper.TryParseKm(txtLyTrinh.Text, out decimal km))
            {
                txtLyTrinh.Text = FormatHelper.FormatKm(km);
            }
        }

        // Xử lý nút Thêm Ga Mới từ thanh Toolbar Master (Bên trái)
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
            if (string.IsNullOrWhiteSpace(txtMaCode.Text))
            {
                ThongBaoDialog.CanhBao("Vui lòng nhập Mã Code Ga (ví dụ: HNI, SGO, DAN, HUE).", "Thiếu Trường Bắt Buộc");
                txtMaCode.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTenGa.Text))
            {
                ThongBaoDialog.CanhBao("Vui lòng nhập Tên Ga.", "Thiếu Trường Bắt Buộc");
                txtTenGa.Focus();
                return;
            }

            // Parse dung sai an toàn đa định dạng qua FormatHelper dùng chung toàn hệ thống
            if (!FormatHelper.TryParseKm(txtLyTrinh.Text, out decimal lyTrinh))
            {
                ThongBaoDialog.CanhBao("Lý trình phải là số thực không âm (ví dụ: 688.00 hoặc 1095.50 km).", "Lý Trình Không Hợp Lệ");
                txtLyTrinh.Focus();
                return;
            }

            string hangGa = "HANG_1";
            if (cboHangGa.SelectedIndex == 1) hangGa = "HANG_2";
            else if (cboHangGa.SelectedIndex == 2) hangGa = "HANG_3";

            var ga = new Ga
            {
                MaTuyen = 1,
                MaGaCode = txtMaCode.Text.Trim().ToUpper(),
                TenGa = txtTenGa.Text.Trim(),
                LyTrinhKm = lyTrinh,
                TinhThanh = cboTinhThanh.Text.Trim(),
                HangGa = hangGa,
                CoCauQuay = chkCoCauQuay.IsChecked == true,
                DangKhaiThac = chkDangKhaiThac.IsChecked == true,
                NguoiTao = "admin",
                NguoiCapNhat = "admin"
            };

            if (_isAddingNew)
            {
                if (_gaService.Them(ga, out string err))
                {
                    ThongBaoDialog.ThanhCong($"Đã thêm mới thành công Ga {ga.TenGa} ({ga.MaGaCode}) vào CSDL.", "Thêm Ga Thành Công");
                    _isDirty = false;
                    _isAddingNew = false;
                    LoadData();
                }
                else
                {
                    ThongBaoDialog.Loi(err, "Không Thể Thêm Ga");
                }
            }
            else
            {
                ga.MaGa = _currentMaGa;
                if (_gaService.CapNhat(ga, out string err))
                {
                    ThongBaoDialog.ThanhCong($"Đã cập nhật dữ liệu Ga {ga.TenGa} ({ga.MaGaCode}) thành công.", "Cập Nhật Thành Công");
                    _isDirty = false;
                    LoadData();
                }
                else
                {
                    ThongBaoDialog.Loi(err, "Không Thể Cập Nhật Ga");
                }
            }
        }

        private void CrudActionBar_CancelClicked(object sender, RoutedEventArgs e)
        {
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
                    $"Bạn đang thực hiện xóa Ga: [{maCode}] {tenGa.ToUpper()} (Mã ID: #{maGa}).\n\nĐây là hành động nguy hiểm có thể ảnh hưởng trực tiếp đến an toàn chạy tàu và toàn vẹn cơ sở dữ liệu.\n\nNếu ga này đang có lịch trình chạy tàu hoặc liên kết khu gian, hệ thống sẽ từ chối để đảm bảo an toàn.", 
                    "XÁC NHẬN XÓA GA ĐƯỜNG SẮT", 
                    maXacNhan: maCode,
                    nutXoa: "Xóa vĩnh viễn",
                    nutHuy: "Hủy bỏ");

                if (confirm)
                {
                    try
                    {
                        if (_gaService.Xoa(maGa))
                        {
                            ThongBaoDialog.ThanhCong($"Đã xóa Ga {tenGa} ({maCode}) thành công khỏi CSDL.", "Đã Xóa Thành Công");
                            _isDirty = false;
                            LoadData();
                        }
                        else
                        {
                            ThongBaoDialog.CanhBao($"Không thể xóa Ga [{maCode}] {tenGa}!\n\nLý do: Ga này hiện đang có các phân đoạn khu gian kết nối và lịch trình chạy tàu liên kết trong CSDL. Vui lòng gỡ bỏ các ràng buộc trước khi xóa ga.", "Ràng Buộc Dữ Liệu An Toàn");
                        }
                    }
                    catch (Exception ex)
                    {
                        ThongBaoDialog.Loi($"Lỗi CSDL: {ex.Message}", "Lỗi Ngoại Lệ");
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
                    pnlChiTietKhuGian.Children.Add(new TextBlock
                    {
                        Text = "Chưa có phân đoạn khu gian nào kết nối",
                        FontSize = 10,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")),
                        FontStyle = FontStyles.Italic,
                        Margin = new Thickness(0, 2, 0, 2)
                    });
                }
                else
                {
                    foreach (DataRow r in dtKg.Rows)
                    {
                        string huong = r["HuongTuyen"]?.ToString() ?? "Khu gian";
                        string gaDau = r["TenGaDau"]?.ToString() ?? "";
                        string gaCuoi = r["TenGaCuoi"]?.ToString() ?? "";
                        decimal culy = Convert.ToDecimal(r["CuLyKm"]);
                        int vk = Convert.ToInt32(r["TocDoToiDaKhach"]);
                        int vh = Convert.ToInt32(r["TocDoToiDaHang"]);
                        bool dayTau = Convert.ToBoolean(r["CanDauMayDay"]);
                        decimal doc = Convert.ToDecimal(r["DoDocPermil"]);

                        var itemBorder = new Border
                        {
                            Background = Brushes.White,
                            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(2),
                            Padding = new Thickness(6, 4, 6, 4),
                            Margin = new Thickness(0, 0, 0, 4),
                            Cursor = Cursors.Hand,
                            ToolTip = "Nhấn để xem hồ sơ thông số kỹ thuật khu gian chi tiết"
                        };

                        DataRow capturedKgRow = r;
                        itemBorder.MouseLeftButtonUp += (s, ev) =>
                        {
                            ev.Handled = true;
                            HienThiPopupTongHopKhuGian();
                        };

                        var sp = new StackPanel();
                        var topRow = new Grid();
                        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var badgeHuong = new Border
                        {
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E7FF")),
                            Padding = new Thickness(4, 1, 4, 1),
                            CornerRadius = new CornerRadius(2),
                            Margin = new Thickness(0, 0, 6, 0)
                        };
                        badgeHuong.Child = new TextBlock
                        {
                            Text = huong,
                            FontSize = 9,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"))
                        };
                        Grid.SetColumn(badgeHuong, 0);
                        topRow.Children.Add(badgeHuong);

                        var txtRoute = new TextBlock
                        {
                            Text = $"{gaDau} ↔ {gaCuoi} ({culy:0.00} km)",
                            FontSize = 11,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(txtRoute, 1);
                        topRow.Children.Add(txtRoute);

                        sp.Children.Add(topRow);

                        var txtSub = new TextBlock
                        {
                            Text = dayTau ? $"Vmax: {vk} km/h (khách), {vh} km/h (hàng) • ⚡ Cần đầu máy đẩy (Dốc {doc}‰)"
                                          : $"Vmax: {vk} km/h (khách), {vh} km/h (hàng) • Dốc {doc}‰",
                            FontSize = 9,
                            Foreground = dayTau ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"))
                                                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                            FontWeight = dayTau ? FontWeights.SemiBold : FontWeights.Normal,
                            Margin = new Thickness(0, 2, 0, 0)
                        };
                        sp.Children.Add(txtSub);

                        itemBorder.Child = sp;
                        pnlChiTietKhuGian.Children.Add(itemBorder);
                    }
                }

                // 2. Nạp chi tiết các đường ray trong ga trực tiếp vào Tab Đường Ray
                TaiDanhSachDuongRayTrongGa(maGa);
            }
            catch
            {
                // Phòng vệ UI nếu mất kết nối CSDL tạm thời
            }
        }

        private void BdBoxDuongRay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            tabInspector.SelectedIndex = 1;
        }

        private void BtnQuanLyKhuGian_Click(object sender, RoutedEventArgs e)
        {
            HienThiPopupTongHopKhuGian();
        }

        private void HienThiPopupTongHopKhuGian()
        {
            if (_currentMaGa <= 0)
            {
                ThongBaoDialog.CanhBao("Vui lòng chọn một Ga từ danh sách trước khi xem thông tin phân đoạn khu gian.", "Chưa Chọn Ga");
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

        private void TabInspector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source == tabInspector && tabInspector.SelectedIndex == 1)
            {
                txtTenGaRayTab.Text = $"HẠ TẦNG RAY GA {txtTenGa.Text.Trim().ToUpper()}";
                if (_dtCurrentDuongRay == null || _dtCurrentDuongRay.Rows.Count == 0)
                {
                    if (_currentMaGa > 0)
                        TaiDanhSachDuongRayTrongGa(_currentMaGa);
                }
            }
        }

        public void TaiDanhSachDuongRayTrongGa(int maGa)
        {
            _isLoadingRay = true;
            try
            {
                _dtCurrentDuongRay = _duongRayService.LayDanhSachTheoGa(maGa);
                dgDuongRayGa.ItemsSource = _dtCurrentDuongRay?.DefaultView;

                int count = _dtCurrentDuongRay?.Rows.Count ?? 0;
                txtRayCountTab.Text = $"{count} ray";
                txtSoDuongRay.Text = $"{count} đường ray";

                // Tính toán KPI năng lực hạ tầng ga
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

                VeSoDoDuongRay();

                if (_dtCurrentDuongRay != null && _dtCurrentDuongRay.Rows.Count > 0)
                {
                    dgDuongRayGa.SelectedIndex = 0;
                }
                else
                {
                    ClearFormRay(isNew: true);
                }
            }
            catch
            {
                // Phòng vệ UI nếu lỗi CSDL tạm thời
            }
            finally
            {
                _isLoadingRay = false;
            }
        }

        private void DgDuongRayGa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingRay) return;

            if (dgDuongRayGa.SelectedItem is DataRowView drv)
            {
                PopulateFormRay(drv);
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

                // Tính toán số hiệu kế tiếp gợi ý
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

                txtLuuRay.Text = "Tạo Mới Đường Ray";
                btnXoaRay.IsEnabled = !isNew;
                CapNhatNangLucToa();
                txtSoHieuRay.Focus();
            }
            finally
            {
                _isLoadingRay = false;
            }
        }

        private void CapNhatNangLucToa()
        {
            if (txtNangLucToa == null || txtGhiChuTacNghiep == null) return;

            int chieuDai = 450;
            if (txtChieuDaiRay != null && int.TryParse(txtChieuDaiRay.Text.Trim(), out int d) && d > 0)
            {
                chieuDai = d;
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
                    BorderThickness = new Thickness(isSelected ? 1.5 : 1),
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
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

                // Cột 1: Huy hiệu số hiệu & Tên đường
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

                // Cột 2: Thanh ray mô phỏng tỷ lệ
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

                // Nhãn mô tả dưới thanh ray
                var spSub = new StackPanel { Orientation = Orientation.Horizontal };

                // Badge loại tác nghiệp
                string loaiText = loai switch { "CHINH_TUYEN" => "Chính Tuyến", "BOC_DO" => "Bốc Dỡ", _ => "Đ.Tránh" };
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
                        Text = "🚶 Ke ga",
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

                // Cột 3: Trạng thái ray (chữ sạch tiếng Việt)
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

        private void ChonRayTheoId(int maRay)
        {
            if (dgDuongRayGa.ItemsSource is DataView dv)
            {
                for (int i = 0; i < dv.Count; i++)
                {
                    if (Convert.ToInt32(dv[i]["MaDuongRay"]) == maRay)
                    {
                        dgDuongRayGa.SelectedIndex = i;
                        dgDuongRayGa.ScrollIntoView(dgDuongRayGa.SelectedItem);
                        return;
                    }
                }
            }
        }

        private void BtnThemRayMoi_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaGa <= 0)
            {
                ThongBaoDialog.CanhBao("Vui lòng chọn một Ga từ danh sách trước khi thêm đường ray.", "Chưa Chọn Ga");
                return;
            }
            ClearFormRay(isNew: true);
        }

        private void BtnHuyRay_Click(object sender, RoutedEventArgs e)
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

        private void BtnLuuRay_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaGa <= 0)
            {
                ThongBaoDialog.CanhBao("Vui lòng chọn một Ga từ danh sách.", "Chưa Chọn Ga");
                return;
            }

            if (!int.TryParse(txtSoHieuRay.Text.Trim(), out int soHieu) || soHieu <= 0)
            {
                ThongBaoDialog.CanhBao("Số hiệu đường ray phải là số nguyên dương lớn hơn 0 (ví dụ: 1, 2, 3).", "Số Hiệu Không Hợp Lệ");
                txtSoHieuRay.Focus();
                return;
            }

            if (!int.TryParse(txtChieuDaiRay.Text.Replace(",", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int chieuDai) || chieuDai < 100)
            {
                ThongBaoDialog.CanhBao("Chiều dài hữu dụng đường ray phải tối thiểu 100m để đảm bảo an toàn dồn dịch.", "Chiều Dài Quá Ngắn");
                txtChieuDaiRay.Focus();
                return;
            }

            string loai = "CHINH_TUYEN";
            if (cboLoaiDuongRay.SelectedIndex == 1) loai = "DUONG_TRANH";
            else if (cboLoaiDuongRay.SelectedIndex == 2) loai = "BOC_DO";

            string tt = "TRONG";
            if (cboTrangThaiRay.SelectedIndex == 1) tt = "CO_TAU";
            else if (cboTrangThaiRay.SelectedIndex == 2) tt = "BAO_TRI";

            var ray = new DuongRayGa
            {
                MaDuongRay = _currentMaRay,
                MaGa = _currentMaGa,
                SoHieuDuong = soHieu,
                LoaiDuong = loai,
                ChieuDaiHuuDungM = chieuDai,
                CoKeGa = chkCoKeGaRay.IsChecked == true,
                TrangThai = tt
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

        private void BtnXoaRay_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaRay <= 0) return;

            bool xacNhan = ThongBaoDialog.XacNhan(
                $"Bạn có chắc muốn xóa {txtTitleFormRay.Text} khỏi Ga {txtTenGa.Text.Trim()}?\n\nLưu ý: Nếu đường ray đang gắn với lịch trình dừng tàu, CSDL sẽ từ chối xóa để đảm bảo toàn vẹn dữ liệu.",
                "Xác Nhận Xóa Đường Ray");

            if (xacNhan)
            {
                bool thanhCong = _duongRayService.Xoa(_currentMaRay, out string loi);
                if (thanhCong)
                {
                    ThongBaoDialog.ThanhCong("Đã xóa đường ray thành công khỏi ga.", "Đã Xóa");
                    TaiChiTietHaTangGa(_currentMaGa);
                    LoadData();
                }
                else
                {
                    ThongBaoDialog.Loi(loi, "Không Thể Xóa");
                }
            }
        }

        // Bộ lọc đa tiêu chí
        private void ApplyAdvancedFilter()
        {
            if (_cachedTable == null) return;

            var conditions = new List<string>();

            // 1. Tìm kiếm từ khóa (Tên ga, mã code, tỉnh thành)
            string kw = txtTimKiem.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrEmpty(kw))
            {
                conditions.Add($"(TenGa LIKE '%{kw}%' OR MaGaCode LIKE '%{kw}%' OR TinhThanh LIKE '%{kw}%')");
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

            // Áp dụng RowFilter
            _cachedTable.DefaultView.RowFilter = conditions.Count > 0 ? string.Join(" AND ", conditions) : "";

            // Hiển thị nút "Hủy Bộ Lọc" nếu có bất kỳ điều kiện lọc nào đang chạy
            bool isFiltering = conditions.Count > 0;
            if (btnXoaTatCaLoc != null)
            {
                btnXoaTatCaLoc.Visibility = isFiltering ? Visibility.Visible : Visibility.Collapsed;
            }

            _currentPage = 1;
            ApplyPaging();
        }

        private void TxtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool coChu = !string.IsNullOrEmpty(txtTimKiem.Text);
            if (btnXoaTimKiem != null)
            {
                btnXoaTimKiem.Visibility = coChu ? Visibility.Visible : Visibility.Collapsed;
            }
            if (txtWatermark != null)
            {
                txtWatermark.Visibility = coChu ? Visibility.Collapsed : Visibility.Visible;
            }
            ApplyAdvancedFilter();
        }

        private void BtnXoaTimKiem_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Text = "";
            btnXoaTimKiem.Visibility = Visibility.Collapsed;
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
            if (txtWatermark != null) txtWatermark.Visibility = Visibility.Visible;
            if (cboLocHangGa != null) cboLocHangGa.SelectedIndex = 0;
            if (cboLocKhaiThac != null) cboLocKhaiThac.SelectedIndex = 0;
            if (chkLocCauQuay != null) chkLocCauQuay.IsChecked = false;
            _isLoadingData = false;

            ApplyAdvancedFilter();
        }

        // Nhập file Excel / CSV hàng loạt dùng chung (CommonImportDialog)
        private void BtnNhapFile_Click(object sender, RoutedEventArgs e)
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

        // Xuất file Excel / CSV qua FileExchangeHelper dùng chung
        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
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
                    ThongBaoDialog.CanhBao("Chưa có dữ liệu danh sách ga để lập báo cáo. Vui lòng tải dữ liệu trước.", "Thông Báo", Window.GetWindow(this));
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
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể mở Trung tâm Báo cáo: {ex.Message}", "Lỗi Khởi Tạo", Window.GetWindow(this));
            }
        }

        // Menu chuột phải
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

        private void CtxDoiTrangThai_Click(object sender, RoutedEventArgs e)
        {
            if (dgGa.SelectedItem is DataRowView drv)
            {
                int maGa = Convert.ToInt32(drv["MaGa"]);
                string tenGa = drv["TenGa"]?.ToString() ?? "";
                bool hienTai = drv["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(drv["DangKhaiThac"]);
                bool moi = !hienTai;
                string actionStr = moi ? "MỞ LẠI KHAI THÁC" : "TẠM NGỪNG KHAI THÁC";

                bool ask = ThongBaoDialog.XacNhan(
                    $"Bạn có chắc chắn muốn {actionStr} cho Ga {tenGa} không?",
                    "Xác Nhận Thay Đổi Trạng Thái Nhanh",
                    nutDongY: "Xác nhận",
                    nutHuy: "Hủy bỏ");

                if (ask)
                {
                    try
                    {
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
                    catch (Exception ex)
                    {
                        ThongBaoDialog.Loi("Lỗi khi đổi trạng thái: " + ex.Message, "Lỗi Thao Tác");
                    }
                }
            }
        }

        private void CtxXemNhatKy_Click(object sender, RoutedEventArgs e)
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

        private void CtxLamMoi_Click(object sender, RoutedEventArgs e)
        {
            _isDirty = false;
            LoadData();
        }

        // Phím tắt
        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                e.Handled = true;
                if (crudActionBar.btnLuu.IsEnabled)
                {
                    CrudActionBar_SaveClicked(sender, e);
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
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

        // =========================================================================
        // CÁC PHƯƠNG THỨC KÍCH HOẠT TÁC VỤ THỰC TẾ TỪ MAINWINDOW (F1 - F12 & SHORTCUT STRIP)
        // =========================================================================
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
