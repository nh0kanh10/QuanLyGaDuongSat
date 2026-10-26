using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using BUS.Services;
using ET.VanHanh;
using GUI.Views.Dialogs;

namespace GUI.Views.Pages
{
    public partial class MacTauPage : Page
    {
        private readonly MacTauService _macTauService = new();
        private readonly GaService _gaService = new();
        private readonly DinhMucVanTocService _dinhMucService = new();

        private DataTable? _cachedMacTau;
        private DataTable? _cachedGa;
        private bool _isAddingNew = false;
        private int _selectedMaMacTau = 0;
        private bool _selectedDangKhaiThacGoc = true;
        private string _currentHuongChay = "BAC_NAM";
        private bool _isInitialized = false;

        public MacTauPage()
        {
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage("vi-VN");
            InitializeComponent();
            DataObject.AddPastingHandler(txtSoHieuMac, TxtSoHieuMac_Pasting);
            KhoiTaoDuLieu();
            _isInitialized = true;
        }

        private void KhoiTaoDuLieu()
        {
            try
            {
                // 1. Nạp danh sách Ga
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

                // 2. Nạp danh mục định mức loại tàu từ CSDL
                NapDanhSachDinhMucLoaiTau();

                // 3. Nạp danh sách Mác tàu
                LoadData();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi khởi tạo dữ liệu mác tàu: {ex.Message}", "Lỗi CSDL");
            }
        }

        public void LoadData()
        {
            try
            {
                _cachedMacTau = _macTauService.LayDanhSach();

                if (!_cachedMacTau.Columns.Contains("STT"))
                    _cachedMacTau.Columns.Add("STT", typeof(int));
                if (!_cachedMacTau.Columns.Contains("TenLoaiTauHienThi"))
                    _cachedMacTau.Columns.Add("TenLoaiTauHienThi", typeof(string));
                if (!_cachedMacTau.Columns.Contains("TenHuongChayHienThi"))
                    _cachedMacTau.Columns.Add("TenHuongChayHienThi", typeof(string));
                if (!_cachedMacTau.Columns.Contains("TenTrangThai"))
                    _cachedMacTau.Columns.Add("TenTrangThai", typeof(string));
                if (!_cachedMacTau.Columns.Contains("DotColor"))
                    _cachedMacTau.Columns.Add("DotColor", typeof(Brush));
                if (!_cachedMacTau.Columns.Contains("FgTrangThai"))
                    _cachedMacTau.Columns.Add("FgTrangThai", typeof(Brush));

                var brushGreenDot = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                var brushGreenFg = new SolidColorBrush(Color.FromRgb(21, 128, 61));

                var brushGrayDot = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                var brushGrayFg = new SolidColorBrush(Color.FromRgb(71, 85, 105));

                int stt = 1;
                foreach (DataRow r in _cachedMacTau.Rows)
                {
                    r["STT"] = stt++;

                    string loai = r["LoaiTau"]?.ToString() ?? "";
                    r["TenLoaiTauHienThi"] = DinhMucVanTocService.LayThongSoDinhMuc(loai).TenLoai;

                    string huong = r["HuongChay"]?.ToString() ?? "";
                    r["TenHuongChayHienThi"] = huong switch
                    {
                        "BAC_NAM" => "Bắc → Nam",
                        "NAM_BAC" => "Nam → Bắc",
                        _ => huong
                    };

                    bool kt = r["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(r["DangKhaiThac"]);
                    if (kt)
                    {
                        r["TenTrangThai"] = "Đang khai thác";
                        r["DotColor"] = brushGreenDot;
                        r["FgTrangThai"] = brushGreenFg;
                    }
                    else
                    {
                        r["TenTrangThai"] = "Tạm ngừng";
                        r["DotColor"] = brushGrayDot;
                        r["FgTrangThai"] = brushGrayFg;
                    }
                }

                txtBadgeSoLuong.Text = $"{_cachedMacTau.Rows.Count} mác";
                ApDungBoLoc();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi nạp danh sách mác tàu: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void ApDungBoLoc()
        {
            if (_cachedMacTau == null) return;

            string tuKhoa = txtTimKiem?.Text.Trim().ToLower() ?? "";
            string loaiFilter = (cboLocLoaiTau?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            int indexTrangThai = cboLocTrangThai?.SelectedIndex ?? 0;

            var dv = _cachedMacTau.DefaultView;
            string filter = "1=1";

            if (!string.IsNullOrEmpty(tuKhoa))
            {
                filter += $" AND (SoHieuMacTau LIKE '%{tuKhoa}%' OR TenGaDi LIKE '%{tuKhoa}%' OR TenGaDen LIKE '%{tuKhoa}%')";
            }

            if (!string.IsNullOrEmpty(loaiFilter))
            {
                filter += $" AND LoaiTau = '{loaiFilter}'";
            }

            if (indexTrangThai == 1) // Đang khai thác
            {
                filter += " AND DangKhaiThac = 1";
            }
            else if (indexTrangThai == 2) // Tạm ngừng
            {
                filter += " AND DangKhaiThac = 0";
            }

            dv.RowFilter = filter;
            dgMacTau.ItemsSource = dv;

            int dangChay = 0, tamDung = 0;
            foreach (DataRowView r in dv)
            {
                bool kt = r["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(r["DangKhaiThac"]);
                if (kt) dangChay++; else tamDung++;
            }

            txtThongKeHienThi.Text = $"Hiển thị: {dv.Count} / {_cachedMacTau.Rows.Count} mác tàu";
            txtThongKeKhaiThac.Text = $"Đang khai thác: {dangChay}";
            txtThongKeTamDung.Text = $"Tạm ngừng: {tamDung}";

            bool coChu = !string.IsNullOrEmpty(txtTimKiem?.Text);
            if (txtPlaceholderTimKiem != null)
                txtPlaceholderTimKiem.Visibility = coChu ? Visibility.Collapsed : Visibility.Visible;
            if (bdShortcutSearch != null)
                bdShortcutSearch.Visibility = coChu ? Visibility.Collapsed : Visibility.Visible;
            if (btnXoaTimKiem != null)
                btnXoaTimKiem.Visibility = coChu ? Visibility.Visible : Visibility.Collapsed;

            if (btnDatLaiLoc != null)
            {
                bool coLoc = coChu || !string.IsNullOrEmpty(loaiFilter) || indexTrangThai > 0;
                btnDatLaiLoc.Visibility = coLoc ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_selectedMaMacTau > 0)
            {
                for (int i = 0; i < dv.Count; i++)
                {
                    if (Convert.ToInt32(dv[i]["MaMacTau"]) == _selectedMaMacTau)
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
                _selectedMaMacTau = Convert.ToInt32(row["MaMacTau"]);

                string soHieu = row["SoHieuMacTau"]?.ToString() ?? "";
                inspectorHeader.SetDetail($"MÁC TÀU: {soHieu}", "Cấu hình thông số kỹ thuật & quy chuẩn hành trình", _selectedMaMacTau, Wpf.Ui.Controls.SymbolRegular.BranchFork24);

                txtSoHieuMac.Text = soHieu;
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
                _selectedDangKhaiThacGoc = dangKt;
                if (dangKt) radDangKhaiThac.IsChecked = true; else radTamNgung.IsChecked = true;

                int soChuyen = row["SoChuyenTauDaChay"] != DBNull.Value ? Convert.ToInt32(row["SoChuyenTauDaChay"]) : 0;
                txtKpiSoChuyen.Text = $"{soChuyen} chuyến";

                // LỰA CHỌN B: Khóa cứng 100% số hiệu khi sửa mác tàu đã lưu (giống Mã Ga)
                txtSoHieuMac.IsReadOnly = true;
                txtSoHieuMac.Background = (Brush)new BrushConverter().ConvertFrom("#F1F5F9")!;
                txtSoHieuMac.ToolTip = "Số hiệu mác tàu là mã định danh cố định, không thể thay đổi sau khi đã lưu (khóa cố định giống Mã Ga).";

                // Đổ dữ liệu AUDIT chuẩn
                string nguoiTao = row["NguoiTao"]?.ToString() ?? "admin";
                string thoiGianTao = row["NgayTao"] != DBNull.Value ? Convert.ToDateTime(row["NgayTao"]).ToString("dd/MM/yyyy HH:mm") : "--/--/----";
                txtNguoiTao.Text = $"{nguoiTao} ({thoiGianTao})";

                if (row["NgayCapNhat"] != DBNull.Value)
                {
                    string nguoiSua = row["NguoiCapNhat"]?.ToString() ?? "admin";
                    string thoiGianSua = Convert.ToDateTime(row["NgayCapNhat"]).ToString("dd/MM/yyyy HH:mm");
                    txtNgayCapNhat.Text = $"{nguoiSua} ({thoiGianSua})";
                }
                else
                {
                    txtNgayCapNhat.Text = "Chưa có cập nhật";
                }

                CapNhatHuongVaHanhTrinh();
            }
        }

        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            KichHoatThemMoi();
        }

        public void KichHoatThemMoi()
        {
            _isAddingNew = true;
            _selectedMaMacTau = 0;
            dgMacTau.SelectedItem = null;

            inspectorHeader.SetCreateMode("KHAI BÁO MÁC TÀU MỚI", "Nhập thông số hành trình cho mác tàu mới trên tuyến");

            txtSoHieuMac.Text = "";
            txtSoHieuMac.IsReadOnly = false;
            txtSoHieuMac.Background = Brushes.White;
            txtSoHieuMac.ToolTip = "Nhập số hiệu định danh mác tàu (ví dụ: SE1, NA1, LP3, HBN1)";
            cboLoaiTau.SelectedIndex = 0;

            if (_cachedGa != null && _cachedGa.Rows.Count >= 2)
            {
                cboGaDi.SelectedIndex = 0;
                cboGaDen.SelectedIndex = _cachedGa.Rows.Count - 1;
            }

            cboMucUuTien.SelectedIndex = 0;
            _selectedDangKhaiThacGoc = true;
            radDangKhaiThac.IsChecked = true;
            txtKpiSoChuyen.Text = "0 chuyến";

            txtNguoiTao.Text = "admin (đang tạo mới)";
            txtNgayCapNhat.Text = "Chưa lưu";

            ApDungTinhGioChuanTuDong();
            txtSoHieuMac.Focus();
        }

        private void CboLoaiTau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (_isAddingNew)
            {
                ApDungTinhGioChuanTuDong();
            }
            else
            {
                CapNhatHuongVaHanhTrinh();
            }
        }

        private void CboGa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (_isAddingNew)
            {
                ApDungTinhGioChuanTuDong();
            }
            else
            {
                CapNhatHuongVaHanhTrinh();
            }
        }

        private void BtnTuTinhGioChuan_Click(object sender, RoutedEventArgs e)
        {
            ApDungTinhGioChuanTuDong();
        }

        private void ApDungTinhGioChuanTuDong()
        {
            if (!_isInitialized || cboGaDi == null || cboGaDen == null || cboLoaiTau == null || txtThoiGianChuan == null)
                return;

            if (cboGaDi.SelectedItem is DataRowView rDi && cboGaDen.SelectedItem is DataRowView rDen)
            {
                decimal kmDi = rDi["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(rDi["LyTrinhKm"]) : 0;
                decimal kmDen = rDen["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(rDen["LyTrinhKm"]) : 0;
                decimal cuLy = Math.Abs(kmDen - kmDi);

                string loai = (cboLoaiTau.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "TAU_NHANH";
                decimal tgGoiY = MacTauService.TinhGioChuanGoiY(cuLy, loai);

                txtThoiGianChuan.Text = tgGoiY.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

                var (vanToc, tenLoai, moTa) = DinhMucVanTocService.LayThongSoDinhMuc(loai);

                if (txtGiaiThichDinhMuc != null)
                {
                    txtGiaiThichDinhMuc.Text = $"⚡ Tự tính ({tenLoai}): {cuLy:F1} km ÷ {vanToc:F0} km/h = {tgGoiY:F1}h";
                    txtGiaiThichDinhMuc.ToolTip = $"{tenLoai} - {vanToc:F1} km/h: {moTa}";
                    txtGiaiThichDinhMuc.Foreground = new SolidColorBrush(Color.FromRgb(2, 132, 199));
                }
            }
            CapNhatHuongVaHanhTrinh();
        }

        /// <summary>
        /// RÀNG BUỘC KỸ THUẬT SỐ HIỆU MÁC TÀU:
        /// Chỉ chấp nhận chữ cái tiếng Anh (A-Z, a-z), chữ số (0-9) và dấu gạch nối (-).
        /// Chặn triệt để: Khoảng trắng, ký tự đặc biệt, emoji, ký tự tiếng Việt có dấu.
        /// </summary>
        private void TxtSoHieuMac_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsLetterOrDigit(c) && c != '-')
                {
                    e.Handled = true;
                    return;
                }
                if (c > 127) // Chặn ký tự có dấu / unicode ngoài ASCII
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void TxtSoHieuMac_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (string.IsNullOrEmpty(text) || !System.Text.RegularExpressions.Regex.IsMatch(text, @"^[A-Za-z0-9\-]+$"))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        /// <summary>
        /// RÀNG BUỘC KỸ THUẬT: Chỉ cho phép nhập số 0-9 và duy nhất 1 dấu thập phân (. hoặc ,)
        /// Chặn triệt để: Chữ cái, ký tự đặc biệt, dấu âm (-), dấu cách, số cực lớn
        /// </summary>
        private void TxtThoiGianChuan_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string currentText = tb.Text;
            string input = e.Text;

            // Nếu không phải ký tự số 0-9
            if (!char.IsDigit(input[0]))
            {
                // Cho phép duy nhất 1 dấu . hoặc ,
                if ((input == "." || input == ",") && !currentText.Contains('.') && !currentText.Contains(','))
                {
                    e.Handled = false;
                    return;
                }

                // Chặn toàn bộ chữ cái, dấu âm, dấu cách
                e.Handled = true;
                return;
            }
        }

        /// <summary>
        /// Tự động chuẩn hóa về định dạng 1 chữ số thập phân (F1) và giới hạn dải hợp lệ 0.2h - 100.0h
        /// </summary>
        private void TxtThoiGianChuan_LostFocus(object sender, RoutedEventArgs e)
        {
            string raw = txtThoiGianChuan.Text.Trim().Replace(',', '.');
            if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal val))
            {
                if (val < 0.2m) val = 0.2m;
                if (val > 100.0m) val = 100.0m;
                txtThoiGianChuan.Text = val.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                txtThoiGianChuan.Text = "1.0";
            }
            CapNhatHuongVaHanhTrinh();
        }

        private void TxtThoiGianChuan_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            CapNhatHuongVaHanhTrinh();
        }

        private void CapNhatHuongVaHanhTrinh()
        {
            if (!_isInitialized || cboGaDi == null || cboGaDen == null || txtHuongChayTuDong == null || txtKhoangCachHanhTrinh == null || txtKpiCuLy == null || txtKpiVanToc == null)
                return;

            if (cboGaDi.SelectedItem is DataRowView rDi && cboGaDen.SelectedItem is DataRowView rDen)
            {
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
                    txtHuongChayTuDong.Text = " Ga xuất phát trùng Ga về đích";
                    txtHuongChayTuDong.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                }

                decimal cuLy = Math.Abs(kmDen - kmDi);
                txtKhoangCachHanhTrinh.Text = $"{cuLy:F1}";
                txtKpiCuLy.Text = $"{cuLy:F1} km";

                string rawTg = txtThoiGianChuan?.Text.Trim().Replace(',', '.') ?? "";
                if (decimal.TryParse(rawTg, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal tg) && tg > 0)
                {
                    decimal vtb = cuLy / tg;
                    txtKpiVanToc.Text = $"~{vtb:F1} km/h";

                    if (vtb > 200m)
                    {
                        txtKpiVanToc.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)); // Đỏ cảnh báo
                        txtKpiVanToc.ToolTip = $" CẢNH BÁO AN TOÀN:\nVận tốc lữ hành ~{vtb:F1} km/h vượt quá tốc độ tối đa cho phép của hạ tầng đường sắt (ngưỡng tối đa 200 km/h)!\nVui lòng tăng thời gian chạy chuẩn cho phù hợp.";
                    }
                    else if (vtb > 120m)
                    {
                        txtKpiVanToc.Foreground = new SolidColorBrush(Color.FromRgb(194, 65, 12)); // Cam đậm
                        txtKpiVanToc.ToolTip = $" CHÚ Ý VẬN TỐC CAO:\nVận tốc lữ hành ~{vtb:F1} km/h ở mức cao (trên 120 km/h, chỉ dành cho mác tàu nhanh/cao tốc nâng cấp).";
                    }
                    else if (vtb < 10m && cuLy > 30m)
                    {
                        txtKpiVanToc.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6)); // Cam cảnh báo
                        txtKpiVanToc.ToolTip = $" CẢNH BÁO VẬN HÀNH:\nVận tốc lữ hành ~{vtb:F1} km/h quá chậm (dưới 10 km/h)!\nVui lòng giảm thời gian chạy chuẩn cho phù hợp.";
                    }
                    else
                    {
                        txtKpiVanToc.Foreground = new SolidColorBrush(Color.FromRgb(15, 118, 110)); // Xanh chuẩn
                        txtKpiVanToc.ToolTip = $"Tốc độ trung bình lữ hành: ~{vtb:F1} km/h (Hành trình {cuLy:F1} km trong {tg:F1} giờ).";
                    }
                }
                else
                {
                    txtKpiVanToc.Text = "~0 km/h";
                    txtKpiVanToc.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                    txtKpiVanToc.ToolTip = "Chưa có thông tin thời gian chạy chuẩn hợp lệ.";
                }
            }
        }

        private void CrudActionBar_SaveClicked(object sender, RoutedEventArgs e)
        {
            ThucHienLuu();
        }

        public void ThucHienLuu()
        {
            string soHieu = txtSoHieuMac.Text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(soHieu))
            {
                ThongBaoDialog.CanhBao("Vui lòng nhập số hiệu mác tàu (ví dụ: SE7, NA1, LP3).", "Thiếu Thông Tin");
                txtSoHieuMac.Focus();
                return;
            }

            if (soHieu.Length < 2)
            {
                ThongBaoDialog.CanhBao("Số hiệu mác tàu phải có độ dài tối thiểu từ 2 ký tự trở lên (ví dụ: SE, NA1, LP3).", "Cảnh Báo Độ Dài");
                txtSoHieuMac.Focus();
                return;
            }

            if (soHieu.Length > 10)
            {
                ThongBaoDialog.CanhBao("Số hiệu mác tàu không được vượt quá 10 ký tự (theo quy chuẩn số hiệu mác tàu ĐSVN).", "Cảnh Báo Độ Dài");
                txtSoHieuMac.Focus();
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(soHieu, @"^[A-Z0-9\-]+$"))
            {
                ThongBaoDialog.CanhBao("Số hiệu mác tàu chỉ được chứa chữ cái tiếng Anh (A-Z), chữ số (0-9) và dấu gạch nối (-).\nKhông được chứa khoảng trắng, emoji hoặc ký tự đặc biệt!", "Cảnh Báo Ký Tự");
                txtSoHieuMac.Focus();
                return;
            }

            if (cboGaDi.SelectedValue == null || cboGaDen.SelectedValue == null)
            {
                ThongBaoDialog.CanhBao("Vui lòng chọn đầy đủ Ga xuất phát và Ga về đích.", "Thiếu Thông Tin");
                return;
            }

            int gaDi = Convert.ToInt32(cboGaDi.SelectedValue);
            int gaDen = Convert.ToInt32(cboGaDen.SelectedValue);

            if (gaDi == gaDen)
            {
                ThongBaoDialog.CanhBao("Ga xuất phát phải khác Ga về đích!", "Lỗi Tuyến Chạy");
                return;
            }

            // RÀNG BUỘC PHÒNG VỆ VỀ THỜI GIAN CHUẨN
            string rawTg = txtThoiGianChuan.Text.Trim().Replace(',', '.');
            if (!decimal.TryParse(rawTg, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal tgChuan))
            {
                ThongBaoDialog.CanhBao("Thời gian chạy chuẩn không đúng định dạng số hợp lệ (ví dụ: 32.5 giờ).", "Lỗi Nhập Liệu");
                txtThoiGianChuan.Focus();
                return;
            }

            if (tgChuan < 0.2m)
            {
                ThongBaoDialog.CanhBao("Thời gian chạy chuẩn tối thiểu là 0.2 giờ (tương đương 12 phút).\nKhông chấp nhận số âm hoặc bằng 0!", "Giới Hạn Nghiệp Vụ");
                txtThoiGianChuan.Focus();
                return;
            }

            if (tgChuan > 100.0m)
            {
                ThongBaoDialog.CanhBao("Thời gian chạy chuẩn tối đa cho phép là 100.0 giờ (mạng lưới đường sắt Việt Nam không có hành trình nào vượt quá 100h)!\nVui lòng nhập lại số hợp lý.", "Giới Hạn Nghiệp Vụ");
                txtThoiGianChuan.Focus();
                return;
            }

            // Kiểm tra tính phi lý của Vận tốc lữ hành dự tính
            if (cboGaDi.SelectedItem is DataRowView rDi && cboGaDen.SelectedItem is DataRowView rDen)
            {
                decimal kmDi = rDi["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(rDi["LyTrinhKm"]) : 0;
                decimal kmDen = rDen["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(rDen["LyTrinhKm"]) : 0;
                decimal cuLy = Math.Abs(kmDen - kmDi);

                if (cuLy > 0)
                {
                    decimal vtb = cuLy / tgChuan;
                    if (vtb > 200m)
                    {
                        ThongBaoDialog.CanhBao($"Thời gian {tgChuan:F1} giờ tương ứng vận tốc lữ hành ~{vtb:F1} km/h, vượt quá tốc độ tối đa cho phép của hạ tầng đường sắt (ngưỡng an toàn tối đa 200 km/h)!\nVui lòng tăng thời gian chạy chuẩn cho phù hợp.", "Cảnh Báo An Toàn");
                        txtThoiGianChuan.Focus();
                        return;
                    }
                    if (vtb < 10m && cuLy > 30m)
                    {
                        ThongBaoDialog.CanhBao($"Thời gian {tgChuan:F1} giờ tương ứng vận tốc lữ hành ~{vtb:F1} km/h, quá chậm (dưới 10 km/h) đối với một mác tàu trên tuyến!\nVui lòng giảm thời gian chạy chuẩn cho phù hợp.", "Cảnh Báo Vận Hành");
                        txtThoiGianChuan.Focus();
                        return;
                    }
                }
            }

            string loai = "TAU_NHANH";
            if (cboLoaiTau.SelectedItem is ComboBoxItem itemLoai && itemLoai.Tag != null)
                loai = itemLoai.Tag.ToString()!;

            int uuTien = 1;
            if (cboMucUuTien.SelectedItem is ComboBoxItem itemUuTien && itemUuTien.Tag != null)
                uuTien = Convert.ToInt32(itemUuTien.Tag);

            bool dangKt = radDangKhaiThac.IsChecked ?? true;

            // Xử lý khi chuyển mác tàu từ Đang khai thác sang Tạm ngừng
            bool daHuyChuyenHangLoat = false;
            int soChuyenDaHuy = 0;
            int soChuyenKeHoach = 0;

            if (!_isAddingNew && _selectedMaMacTau > 0 && _selectedDangKhaiThacGoc && !dangKt)
            {
                string gaDiText = cboGaDi.Text;
                string gaDenText = cboGaDen.Text;
                string hanhTrinh = $"{gaDiText} → {gaDenText}";

                if (!XuLyTamDungMacTauVaCacChuyen(_selectedMaMacTau, soHieu, hanhTrinh, out daHuyChuyenHangLoat, out soChuyenDaHuy, out soChuyenKeHoach))
                {
                    // Người dùng hủy bỏ dialog -> Giữ nguyên trạng thái cũ
                    radDangKhaiThac.IsChecked = true;
                    return;
                }
            }

            var m = new MacTauMau
            {
                MaMacTau = _isAddingNew ? 0 : _selectedMaMacTau,
                SoHieuMacTau = soHieu,
                LoaiTau = loai,
                HuongChay = _currentHuongChay,
                MaGaDi = gaDi,
                MaGaDen = gaDen,
                ThoiGianChuanGio = tgChuan,
                MucUuTien = uuTien,
                DangKhaiThac = dangKt
            };

            if (_macTauService.Luu(m, _isAddingNew, out string loi, "admin"))
            {
                _selectedMaMacTau = m.MaMacTau;
                _selectedDangKhaiThacGoc = m.DangKhaiThac;

                if (daHuyChuyenHangLoat)
                {
                    ThongBaoDialog.ThanhCong($"Đã lưu mác tàu '{m.SoHieuMacTau}' sang trạng thái Tạm ngừng và hủy thành công {soChuyenDaHuy} chuyến tàu kế hoạch tương lai!", "Thành Công");
                }
                else if (!_isAddingNew && !m.DangKhaiThac && soChuyenKeHoach > 0)
                {
                    ThongBaoDialog.ThongTin($"Đã lưu mác tàu '{m.SoHieuMacTau}' sang trạng thái Tạm ngừng. Toàn bộ {soChuyenKeHoach} chuyến tàu kế hoạch tương lai vẫn được bảo lưu để Điều độ viên chủ động điều chỉnh.", "Đã Tạm Ngừng Mác Tàu");
                }
                else
                {
                    ThongBaoDialog.ThanhCong($"Đã lưu thành công mác tàu '{m.SoHieuMacTau}' vào hệ thống!", "Thành Công");
                }

                _isAddingNew = false;
                LoadData();
            }
            else
            {
                ThongBaoDialog.CanhBao(loi, "Không Thể Lưu Mác Tàu");
            }
        }

        private bool XuLyTamDungMacTauVaCacChuyen(int maMac, string soHieu, string hanhTrinh, out bool daHuyChuyenHangLoat, out int soChuyenDaHuy, out int soChuyenKeHoach)
        {
            daHuyChuyenHangLoat = false;
            soChuyenDaHuy = 0;
            soChuyenKeHoach = 0;

            var dtTuongLai = _macTauService.LayDanhSachChuyenTauKeHoachTuongLai(maMac);
            if (dtTuongLai == null || dtTuongLai.Rows.Count == 0)
            {
                return true;
            }

            soChuyenKeHoach = dtTuongLai.Rows.Count;
            var dlg = new XuLyTamDungMacTauDialog(maMac, soHieu, hanhTrinh, dtTuongLai)
            {
                Owner = Window.GetWindow(this)
            };

            if (dlg.ShowDialog() != true || !dlg.CoXacNhan)
            {
                return false;
            }

            if (dlg.HuyChuyenTuongLai)
            {
                if (_macTauService.TamDungMacTauVaXuLyChuyen(maMac, true, dlg.LyDoHuy, out soChuyenDaHuy, out string loiHuy, "admin"))
                {
                    daHuyChuyenHangLoat = true;
                    return true;
                }
                else
                {
                    ThongBaoDialog.CanhBao(loiHuy, "Không Thể Hủy Chuyến Tàu");
                    return false;
                }
            }
            else
            {
                daHuyChuyenHangLoat = false;
                return true;
            }
        }

        private void CrudActionBar_CancelClicked(object sender, RoutedEventArgs e)
        {
            KichHoatHuy();
        }

        public void KichHoatHuy()
        {
            _isAddingNew = false;
            LoadData();
        }

        private void CrudActionBar_DeleteClicked(object sender, RoutedEventArgs e)
        {
            ThucHienXoaMacTau();
        }

        private void InspectorHeader_DeleteClicked(object sender, RoutedEventArgs e)
        {
            ThucHienXoaMacTau();
        }

        private void ThucHienXoaMacTau()
        {
            if (_selectedMaMacTau <= 0) return;

            string soHieu = txtSoHieuMac.Text;
            bool xacNhan = ThongBaoDialog.XacNhan(
                $"Bạn có chắc chắn muốn xóa mác tàu '{soHieu}' khỏi hệ thống?\n(Nếu mác tàu đã có chuyến tàu, hệ thống sẽ ngăn chặn để bảo vệ dữ liệu).",
                "Xác Nhận Xóa Mác Tàu",
                nutDongY: "Xóa Mác Tàu",
                nutHuy: "Hủy Bỏ",
                laHanhDongXoa: true);

            if (xacNhan)
            {
                if (_macTauService.Xoa(_selectedMaMacTau, out string loi))
                {
                    ThongBaoDialog.ThanhCong($"Đã xóa vĩnh viễn mác tàu '{soHieu}'.", "Thành Công");
                    _selectedMaMacTau = 0;
                    LoadData();
                }
                else
                {
                    ThongBaoDialog.CanhBao(loi, "Không Thể Xóa");
                }
            }
        }

        public void ChonMacTauTheoId(int maMacTau)
        {
            _selectedMaMacTau = maMacTau;
            ApDungBoLoc();
        }

        public void FocusTimKiem()
        {
            txtTimKiem.Focus();
            txtTimKiem.SelectAll();
        }

        private void BtnNapLai_Click(object sender, RoutedEventArgs e)
        {
            KichHoatNapLai();
        }

        public void KichHoatNapLai()
        {
            LoadData();
        }

        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            KichHoatXuatTep();
        }

        public void KichHoatXuatTep()
        {
            if (_cachedMacTau == null || _cachedMacTau.Rows.Count == 0)
            {
                ThongBaoDialog.ThongTin("Không có dữ liệu mác tàu để xuất.", "Thông Báo");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"DanhSach_MacTau_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using var writer = new StreamWriter(dialog.FileName, false, System.Text.Encoding.UTF8);
                    writer.WriteLine("STT,Mác Tàu,Loại Tàu,Hướng Chạy,Ga Đi,Ga Đến,Thời Gian Chuẩn (Giờ),Mức Ưu Tiên,Trạng Thái,Số Chuyến Đã Chạy,Người Tạo,Ngày Tạo,Người Cập Nhật,Ngày Cập Nhật");

                    int stt = 1;
                    foreach (DataRow r in _cachedMacTau.Rows)
                    {
                        string soHieu = r["SoHieuMacTau"]?.ToString() ?? "";
                        string loai = r["TenLoaiTauHienThi"]?.ToString() ?? "";
                        string huong = r["TenHuongChayHienThi"]?.ToString() ?? "";
                        string gaDi = r["TenGaDi"]?.ToString() ?? "";
                        string gaDen = r["TenGaDen"]?.ToString() ?? "";
                        string tg = r["ThoiGianChuanGio"]?.ToString() ?? "";
                        string uuTien = r["MucUuTien"]?.ToString() ?? "";
                        string tt = r["TenTrangThai"]?.ToString() ?? "";
                        string soChuyen = r["SoChuyenTauDaChay"]?.ToString() ?? "0";
                        string nguoiTao = r["NguoiTao"]?.ToString() ?? "";
                        string ngayTao = r["NgayTao"] != DBNull.Value ? Convert.ToDateTime(r["NgayTao"]).ToString("dd/MM/yyyy HH:mm") : "";
                        string nguoiSua = r["NguoiCapNhat"]?.ToString() ?? "";
                        string ngaySua = r["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(r["NgayCapNhat"]).ToString("dd/MM/yyyy HH:mm") : "";

                        writer.WriteLine($"{stt++},\"{soHieu}\",\"{loai}\",\"{huong}\",\"{gaDi}\",\"{gaDen}\",{tg},{uuTien},\"{tt}\",{soChuyen},\"{nguoiTao}\",\"{ngayTao}\",\"{nguoiSua}\",\"{ngaySua}\"");
                    }

                    ThongBaoDialog.ThanhCong($"Đã xuất danh sách mác tàu thành công ra tệp:\n{dialog.FileName}", "Thành Công");
                }
                catch (Exception ex)
                {
                    ThongBaoDialog.Loi($"Lỗi xuất tệp CSV: {ex.Message}", "Lỗi");
                }
            }
        }

        private void TxtTimKiem_TextChanged(object sender, TextChangedEventArgs e) => ApDungBoLoc();
        private void CboLocLoaiTau_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApDungBoLoc();
        private void CboLocTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApDungBoLoc();

        private void BtnXoaTimKiem_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Text = "";
            ApDungBoLoc();
            txtTimKiem.Focus();
        }

        private void BtnDatLaiLoc_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Text = "";
            cboLocLoaiTau.SelectedIndex = 0;
            cboLocTrangThai.SelectedIndex = 0;
            ApDungBoLoc();
        }

        private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
            {
                row.IsSelected = true;
                row.Focus();
            }
        }

        private void CtxSua_Click(object sender, RoutedEventArgs e)
        {
            txtThoiGianChuan.Focus();
            txtThoiGianChuan.SelectAll();
        }

        private void CtxCopySoHieu_Click(object sender, RoutedEventArgs e)
        {
            if (dgMacTau.SelectedItem is DataRowView row)
            {
                string soHieu = row["SoHieuMacTau"]?.ToString() ?? "";
                if (!string.IsNullOrEmpty(soHieu))
                {
                    Clipboard.SetText(soHieu);
                }
            }
        }

        private void CtxCopyHanhTrinh_Click(object sender, RoutedEventArgs e)
        {
            if (dgMacTau.SelectedItem is DataRowView row)
            {
                string gaDi = row["TenGaDi"]?.ToString() ?? "";
                string gaDen = row["TenGaDen"]?.ToString() ?? "";
                Clipboard.SetText($"{gaDi} → {gaDen}");
            }
        }

        private void CtxCopyRow_Click(object sender, RoutedEventArgs e)
        {
            if (dgMacTau.SelectedItem is DataRowView row)
            {
                string soHieu = row["SoHieuMacTau"]?.ToString() ?? "";
                string loai = row["TenLoaiTauHienThi"]?.ToString() ?? "";
                string gaDi = row["TenGaDi"]?.ToString() ?? "";
                string gaDen = row["TenGaDen"]?.ToString() ?? "";
                string huong = row["TenHuongChayHienThi"]?.ToString() ?? "";
                string tg = row["ThoiGianChuanGio"]?.ToString() ?? "";
                string tt = row["TenTrangThai"]?.ToString() ?? "";
                Clipboard.SetText($"{soHieu}\t{loai}\t{gaDi} → {gaDen}\t{huong}\t{tg}h\t{tt}");
            }
        }

        private void CtxDoiTrangThai_Click(object sender, RoutedEventArgs e)
        {
            if (dgMacTau.SelectedItem is DataRowView row)
            {
                int maMac = Convert.ToInt32(row["MaMacTau"]);
                string soHieu = row["SoHieuMacTau"]?.ToString() ?? "";
                string gaDi = row["TenGaDi"]?.ToString() ?? "";
                string gaDen = row["TenGaDen"]?.ToString() ?? "";
                string hanhTrinh = $"{gaDi} → {gaDen}";
                bool dangKtHienTai = row["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(row["DangKhaiThac"]);
                bool trangThaiMoi = !dangKtHienTai;

                if (dangKtHienTai && !trangThaiMoi)
                {
                    // Chuyển từ ĐANG KHAI THÁC sang TẠM NGỪNG
                    if (!XuLyTamDungMacTauVaCacChuyen(maMac, soHieu, hanhTrinh, out bool daHuyChuyen, out int soChuyenHuy, out int soChuyenKeHoach))
                    {
                        return; // Người dùng ấn Hủy bỏ
                    }

                    if (daHuyChuyen)
                    {
                        ThongBaoDialog.ThanhCong($"Đã tạm ngừng mác tàu '{soHieu}' và hủy thành công {soChuyenHuy} chuyến tàu kế hoạch tương lai.", "Thành Công");
                        LoadData();
                    }
                    else
                    {
                        if (_macTauService.DoiTrangThaiKhaiThac(maMac, false, out string loi, "admin"))
                        {
                            if (soChuyenKeHoach > 0)
                            {
                                ThongBaoDialog.ThongTin($"Đã chuyển mác tàu '{soHieu}' sang trạng thái Tạm ngừng. Toàn bộ {soChuyenKeHoach} chuyến tàu kế hoạch tương lai vẫn được bảo lưu để Điều độ viên chủ động điều chỉnh.", "Đã Tạm Ngừng Mác Tàu");
                            }
                            else
                            {
                                ThongBaoDialog.ThanhCong($"Đã chuyển mác tàu '{soHieu}' sang trạng thái Tạm ngừng khai thác (Đã ẩn khỏi danh sách lập chuyến mới).", "Thành Công");
                            }
                            LoadData();
                        }
                        else
                        {
                            ThongBaoDialog.CanhBao(loi, "Không Thể Đổi Trạng Thái");
                        }
                    }
                }
                else
                {
                    // Kích hoạt lại mác tàu
                    if (_macTauService.DoiTrangThaiKhaiThac(maMac, true, out string loi, "admin"))
                    {
                        ThongBaoDialog.ThanhCong($"Đã kích hoạt khai thác trở lại mác tàu '{soHieu}' (Sẵn sàng để lập chuyến mới).", "Thành Công");
                        LoadData();
                    }
                    else
                    {
                        ThongBaoDialog.CanhBao(loi, "Không Thể Đổi Trạng Thái");
                    }
                }
            }
        }

        private void CtxXoa_Click(object sender, RoutedEventArgs e)
        {
            ThucHienXoaMacTau();
        }

        public void KichHoatTuTinhGioChuan()
        {
            ApDungTinhGioChuanTuDong();
        }

        public void KichHoatDinhMucVanToc()
        {
            var dlg = new GUI.Views.Dialogs.DinhMucVanTocDialog
            {
                Owner = Window.GetWindow(this)
            };
            dlg.ShowDialog();
            if (dlg.CoThayDoiDuLieu)
            {
                NapDanhSachDinhMucLoaiTau();
                LoadData();
                ApDungTinhGioChuanTuDong();
            }
        }

        private void BtnDinhMucVanToc_Click(object sender, RoutedEventArgs e)
        {
            KichHoatDinhMucVanToc();
        }

        public void NapDanhSachDinhMucLoaiTau()
        {
            try
            {
                string? currentSelected = (cboLoaiTau.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                string? currentLoc = (cboLocLoaiTau.SelectedItem as ComboBoxItem)?.Tag?.ToString();

                DataTable dt = _dinhMucService.LayDanhSach(chiDangApDung: true);

                // 1. Cbo Inspector Form
                cboLoaiTau.Items.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    string ma = r["MaLoaiTau"]?.ToString() ?? "";
                    string ten = r["TenLoaiTau"]?.ToString() ?? ma;
                    decimal vt = r["VanTocDinhMucKmh"] != DBNull.Value ? Convert.ToDecimal(r["VanTocDinhMucKmh"]) : 50m;
                    var item = new ComboBoxItem
                    {
                        Content = $"{ten} ({vt:F0} km/h)",
                        Tag = ma
                    };
                    cboLoaiTau.Items.Add(item);
                    if (ma == currentSelected)
                    {
                        cboLoaiTau.SelectedItem = item;
                    }
                }
                if (cboLoaiTau.SelectedItem == null && cboLoaiTau.Items.Count > 0)
                {
                    cboLoaiTau.SelectedIndex = 0;
                }

                // 2. Cbo Lọc Toolbar
                cboLocLoaiTau.Items.Clear();
                var itemAll = new ComboBoxItem { Content = "Tất cả loại tàu", IsSelected = true };
                cboLocLoaiTau.Items.Add(itemAll);
                foreach (DataRow r in dt.Rows)
                {
                    string ma = r["MaLoaiTau"]?.ToString() ?? "";
                    string ten = r["TenLoaiTau"]?.ToString() ?? ma;
                    var item = new ComboBoxItem
                    {
                        Content = ten,
                        Tag = ma
                    };
                    cboLocLoaiTau.Items.Add(item);
                    if (ma == currentLoc)
                    {
                        cboLocLoaiTau.SelectedItem = item;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NapDanhSachDinhMucLoaiTau] Lỗi: {ex.Message}");
            }
        }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
                {
                    e.Handled = true;
                    ThucHienLuu();
                }
                else if ((Keyboard.Modifiers == ModifierKeys.Control && (e.Key == Key.K || e.Key == Key.F)) || e.Key == Key.F3)
                {
                    e.Handled = true;
                    FocusTimKiem();
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
                {
                    e.Handled = true;
                    KichHoatThemMoi();
                }
                else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
                {
                    e.Handled = true;
                    KichHoatXuatTep();
                }
                else if (e.Key == Key.F2)
                {
                    e.Handled = true;
                    KichHoatThemMoi();
                }
                else if (e.Key == Key.F4)
                {
                    e.Handled = true;
                    KichHoatTuTinhGioChuan();
                }
                else if (e.Key == Key.F5)
                {
                    e.Handled = true;
                    KichHoatNapLai();
                }
                else if (e.Key == Key.F6)
                {
                    e.Handled = true;
                    KichHoatXuatTep();
                }
                else if (e.Key == Key.F7)
                {
                    e.Handled = true;
                    KichHoatDinhMucVanToc();
                }
                else if (e.Key == Key.Delete)
                {
                    // Tránh ăn phím Delete khi người dùng đang nhập văn bản trong TextBox
                    if (Keyboard.FocusedElement is not TextBox)
                    {
                        e.Handled = true;
                        ThucHienXoaMacTau();
                    }
                }
                else if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    if (!string.IsNullOrEmpty(txtTimKiem.Text))
                    {
                        txtTimKiem.Text = "";
                    }
                    else
                    {
                        KichHoatHuy();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MacTauPage.Page_PreviewKeyDown] Lỗi phím tắt: {ex.Message}");
            }
        }
    }
}
