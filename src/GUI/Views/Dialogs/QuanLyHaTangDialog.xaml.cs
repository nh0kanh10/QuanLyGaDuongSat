using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BUS.Services;
using DTO.Common;
using ET.HaTang;

namespace GUI.Views.Dialogs
{
    public partial class QuanLyHaTangDialog : Window
    {
        private readonly GaService _gaService = new();
        private readonly DuongRayService _duongRayService = new();
        private readonly KhuGianService _khuGianService = new();

        private DataTable? _dtGa;
        private DataTable? _dtRay;
        private DataTable? _dtKg;

        private int _currentMaRay = 0;
        private bool _isAddingRay = false;

        private int _currentMaKg = 0;
        private bool _isAddingKg = false;

        public bool CoThayDoiDuLieu { get; private set; } = false;

        public QuanLyHaTangDialog(int maGaKhoiTao = 0, int tabIndex = 0)
        {
            InitializeComponent();
            KhoiTaoDuLieu(maGaKhoiTao, tabIndex);
        }

        private void KhoiTaoDuLieu(int maGaKhoiTao, int tabIndex)
        {
            LoadDanhSachGa(maGaKhoiTao);
            LoadDanhSachKhuGian();

            if (tabIndex >= 0 && tabIndex < tabMain.Items.Count)
            {
                tabMain.SelectedIndex = tabIndex;
            }
        }

        private void LoadDanhSachGa(int maGaKhoiTao)
        {
            try
            {
                _dtGa = _gaService.LayDanhSach();
                cboChonGaRay.Items.Clear();
                cboGaDau.Items.Clear();
                cboGaCuoi.Items.Clear();

                int selectedIndexRay = 0;
                int idx = 0;

                foreach (DataRow r in _dtGa.Rows)
                {
                    int maGa = Convert.ToInt32(r["MaGa"]);
                    string code = r["MaGaCode"]?.ToString() ?? "";
                    string ten = r["TenGa"]?.ToString() ?? "";
                    decimal km = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m;
                    string itemText = $"[{code}] {ten} — Km {FormatHelper.FormatKm(km)}";

                    var cboItemRay = new ComboBoxItem { Content = itemText, Tag = maGa };
                    cboChonGaRay.Items.Add(cboItemRay);

                    var cboItemDau = new ComboBoxItem { Content = itemText, Tag = maGa };
                    cboGaDau.Items.Add(cboItemDau);

                    var cboItemCuoi = new ComboBoxItem { Content = itemText, Tag = maGa };
                    cboGaCuoi.Items.Add(cboItemCuoi);

                    if (maGa == maGaKhoiTao)
                    {
                        selectedIndexRay = idx;
                    }
                    idx++;
                }

                if (cboChonGaRay.Items.Count > 0)
                {
                    cboChonGaRay.SelectedIndex = selectedIndexRay;
                }
                if (cboGaDau.Items.Count > 0) cboGaDau.SelectedIndex = 0;
                if (cboGaCuoi.Items.Count > 1) cboGaCuoi.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi("Lỗi tải danh mục ga: " + ex.Message, "Lỗi Dữ Liệu");
            }
        }

        #region TAB 1: QUẢN LÝ ĐƯỜNG RAY TRONG GA
        private void CboChonGaRay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboChonGaRay.SelectedItem is ComboBoxItem item && item.Tag is int maGa)
            {
                CapNhatThongTinGaTomTat(maGa);
                LoadDanhSachRayTheoGa(maGa);
            }
        }

        private void CapNhatThongTinGaTomTat(int maGa)
        {
            if (_dtGa == null) return;
            foreach (DataRow r in _dtGa.Rows)
            {
                if (Convert.ToInt32(r["MaGa"]) == maGa)
                {
                    string hang = r["HangGa"]?.ToString() ?? "";
                    string tenHang = hang == "HANG_1" ? "Ga Hạng I" : (hang == "HANG_2" ? "Ga Hạng II" : "Ga Hạng III");
                    bool cauQuay = r["CoCauQuay"] != DBNull.Value && Convert.ToBoolean(r["CoCauQuay"]);
                    string tinh = r["TinhThanh"]?.ToString() ?? "";
                    txtThongTinGaTomTat.Text = $"{tinh} • {tenHang} • Cầu quay: {(cauQuay ? "Có ✓" : "Không —")}";
                    break;
                }
            }
        }

        private void LoadDanhSachRayTheoGa(int maGa)
        {
            try
            {
                _dtRay = _duongRayService.LayDanhSachTheoGa(maGa);
                dgDuongRay.ItemsSource = _dtRay?.DefaultView;

                int count = _dtRay?.Rows.Count ?? 0;
                txtTabRayCount.Text = $"{count} ray";

                // Cập nhật thanh tóm tắt chân bảng
                int maxLen = 0;
                int totalLen = 0;
                int coKeGaCount = 0;
                if (_dtRay != null)
                {
                    foreach (DataRow r in _dtRay.Rows)
                    {
                        int cd = r["ChieuDaiHuuDungM"] != DBNull.Value ? Convert.ToInt32(r["ChieuDaiHuuDungM"]) : 0;
                        bool ke = r["CoKeGa"] != DBNull.Value && Convert.ToBoolean(r["CoKeGa"]);
                        if (cd > maxLen) maxLen = cd;
                        totalLen += cd;
                        if (ke) coKeGaCount++;
                    }
                }
                txtStatusBarRay.Text = $"Tổng số: {count} đường ray • Ray dài nhất: {maxLen:N0} m • Tổng sức chứa: ~{totalLen / 20:N0} toa xe ({coKeGaCount}/{count} ray có ke ga)";

                if (_dtRay != null && _dtRay.Rows.Count > 0)
                {
                    dgDuongRay.SelectedIndex = 0;
                }
                else
                {
                    ClearFormRay(isNew: true);
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi("Lỗi tải danh sách đường ray: " + ex.Message, "Lỗi CSDL");
            }
        }

        private void DgDuongRay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isAddingRay) return;

            if (dgDuongRay.SelectedItem is DataRowView drv)
            {
                PopulateFormRay(drv);
            }
        }

        private void PopulateFormRay(DataRowView drv)
        {
            _currentMaRay = Convert.ToInt32(drv["MaDuongRay"]);
            _isAddingRay = false;
            int soHieu = Convert.ToInt32(drv["SoHieuDuong"]);
            txtTitleFormRay.Text = $"CHI TIẾT: ĐƯỜNG SỐ {soHieu}";
            txtSoHieuRay.Text = soHieu.ToString();

            int cd = Convert.ToInt32(drv["ChieuDaiHuuDungM"]);
            txtChieuDaiRay.Text = cd.ToString();
            txtUocTinhToa.Text = $"~{cd / 20} toa xe tiêu chuẩn";

            chkCoKeGa.IsChecked = drv["CoKeGa"] != DBNull.Value && Convert.ToBoolean(drv["CoKeGa"]);

            string loai = drv["LoaiDuong"]?.ToString() ?? "CHINH_TUYEN";
            if (loai == "CHINH_TUYEN") cboLoaiDuongRay.SelectedIndex = 0;
            else if (loai == "DUONG_TRANH") cboLoaiDuongRay.SelectedIndex = 1;
            else cboLoaiDuongRay.SelectedIndex = 2;

            string tt = drv["TrangThai"]?.ToString() ?? "TRONG";
            if (tt == "TRONG") cboTrangThaiRay.SelectedIndex = 0;
            else if (tt == "CO_TAU") cboTrangThaiRay.SelectedIndex = 1;
            else cboTrangThaiRay.SelectedIndex = 2;

            btnXoaRay.IsEnabled = true;
        }

        private void ClearFormRay(bool isNew)
        {
            _isAddingRay = isNew;
            _currentMaRay = 0;
            txtTitleFormRay.Text = isNew ? "THÊM ĐƯỜNG RAY MỚI" : "CHI TIẾT ĐƯỜNG RAY";
            txtSoHieuRay.Text = (_dtRay != null ? (_dtRay.Rows.Count + 1).ToString() : "1");
            txtChieuDaiRay.Text = "450";
            txtUocTinhToa.Text = "~22 toa xe tiêu chuẩn";
            chkCoKeGa.IsChecked = true;
            cboLoaiDuongRay.SelectedIndex = 1; // Mặc định đường tránh
            cboTrangThaiRay.SelectedIndex = 0;
            btnXoaRay.IsEnabled = !isNew;
            txtSoHieuRay.Focus();
        }

        private void TxtChieuDaiRay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtUocTinhToa == null) return;
            if (int.TryParse(txtChieuDaiRay.Text.Trim(), out int cd) && cd > 0)
            {
                txtUocTinhToa.Text = $"~{cd / 20} toa xe tiêu chuẩn ({cd * 50 / 1000:0.0} tấn tải trọng)";
            }
            else
            {
                txtUocTinhToa.Text = "Chiều dài không hợp lệ";
            }
        }

        private void BtnThemRayMoi_Click(object sender, RoutedEventArgs e)
        {
            ClearFormRay(isNew: true);
        }

        private void BtnHuyRay_Click(object sender, RoutedEventArgs e)
        {
            if (dgDuongRay.SelectedItem is DataRowView drv)
            {
                PopulateFormRay(drv);
            }
            else if (_dtRay != null && _dtRay.Rows.Count > 0)
            {
                dgDuongRay.SelectedIndex = 0;
            }
        }

        private void BtnLuuRay_Click(object sender, RoutedEventArgs e)
        {
            if (cboChonGaRay.SelectedItem is not ComboBoxItem item || item.Tag is not int maGa)
            {
                ThongBaoDialog.CanhBao("Vui lòng chọn ga trước khi lưu đường ray.", "Chưa Chọn Ga");
                return;
            }

            if (!int.TryParse(txtSoHieuRay.Text.Trim(), out int soHieu) || soHieu <= 0)
            {
                ThongBaoDialog.CanhBao("Số hiệu đường ray phải là số nguyên dương lớn hơn 0 (ví dụ: 1, 2, 3).", "Số Hiệu Không Hợp Lệ");
                txtSoHieuRay.Focus();
                return;
            }

            if (!int.TryParse(txtChieuDaiRay.Text.Trim(), out int chieuDai) || chieuDai < 100)
            {
                ThongBaoDialog.CanhBao("Chiều dài hữu dụng của đường ray phải tối thiểu 100 mét để đảm bảo an toàn dồn dịch.", "Chiều Dài Không Hợp Lệ");
                txtChieuDaiRay.Focus();
                return;
            }

            string loai = "DUONG_TRANH";
            if (cboLoaiDuongRay.SelectedIndex == 0) loai = "CHINH_TUYEN";
            else if (cboLoaiDuongRay.SelectedIndex == 2) loai = "BOC_DO";

            string trangThai = "TRONG";
            if (cboTrangThaiRay.SelectedIndex == 1) trangThai = "CO_TAU";
            else if (cboTrangThaiRay.SelectedIndex == 2) trangThai = "BAO_TRI";

            var ray = new DuongRayGa
            {
                MaDuongRay = _currentMaRay,
                MaGa = maGa,
                SoHieuDuong = soHieu,
                LoaiDuong = loai,
                ChieuDaiHuuDungM = chieuDai,
                CoKeGa = chkCoKeGa.IsChecked == true,
                TrangThai = trangThai
            };

            bool ok;
            string error;

            if (_isAddingRay || _currentMaRay == 0)
            {
                ok = _duongRayService.Them(ray, out error);
                if (ok)
                {
                    ThongBaoDialog.ThanhCong($"Đã thêm mới thành công Đường ray số {soHieu} ({chieuDai}m) vào ga.", "Thêm Ray Thành Công");
                    CoThayDoiDuLieu = true;
                    _isAddingRay = false;
                    LoadDanhSachRayTheoGa(maGa);
                }
                else
                {
                    ThongBaoDialog.CanhBao(error, "Lỗi Thêm Mới");
                }
            }
            else
            {
                ok = _duongRayService.CapNhat(ray, out error);
                if (ok)
                {
                    ThongBaoDialog.ThanhCong($"Đã cập nhật thông số kỹ thuật Đường ray số {soHieu} thành công.", "Cập Nhật Thành Công");
                    CoThayDoiDuLieu = true;
                    LoadDanhSachRayTheoGa(maGa);
                }
                else
                {
                    ThongBaoDialog.CanhBao(error, "Lỗi Cập Nhật");
                }
            }
        }

        private void BtnXoaRay_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaRay <= 0) return;

            string soHieu = txtSoHieuRay.Text.Trim();
            string codeXacNhan = $"RAY{soHieu}";

            bool confirm = ThongBaoDialog.XacNhanXoaCoMa(
                $"Bạn đang yêu cầu xóa Đường ray số {soHieu} khỏi ga này.\n\nĐây là thao tác ảnh hưởng đến năng lực đón gửi và tránh tàu của ga. Vui lòng kiểm tra chắc chắn đường này không có đoàn tàu đang đỗ.",
                "XÁC NHẬN XÓA ĐƯỜNG RAY",
                codeXacNhan,
                nutXoa: "Xóa đường ray",
                nutHuy: "Hủy bỏ");

            if (confirm)
            {
                if (cboChonGaRay.SelectedItem is ComboBoxItem item && item.Tag is int maGa)
                {
                    if (_duongRayService.Xoa(_currentMaRay, out string error))
                    {
                        ThongBaoDialog.ThanhCong($"Đã xóa Đường ray số {soHieu} thành công.", "Đã Xóa");
                        CoThayDoiDuLieu = true;
                        LoadDanhSachRayTheoGa(maGa);
                    }
                    else
                    {
                        ThongBaoDialog.Loi(error, "Không Thể Xóa");
                    }
                }
            }
        }
        #endregion

        #region TAB 2: QUẢN LÝ KHU GIAN
        private void LoadDanhSachKhuGian()
        {
            try
            {
                _dtKg = _khuGianService.LayDanhSach();
                dgKhuGian.ItemsSource = _dtKg?.DefaultView;

                int count = _dtKg?.Rows.Count ?? 0;
                txtTabKgCount.Text = $"{count} phân đoạn";

                // Cập nhật thanh tóm tắt chân bảng khu gian
                decimal tongKm = 0m;
                int deoCount = 0;
                if (_dtKg != null)
                {
                    foreach (DataRow r in _dtKg.Rows)
                    {
                        decimal km = r["CuLyKm"] != DBNull.Value ? Convert.ToDecimal(r["CuLyKm"]) : 0m;
                        decimal doc = r["DoDocPermil"] != DBNull.Value ? Convert.ToDecimal(r["DoDocPermil"]) : 0m;
                        bool day = r["CanDauMayDay"] != DBNull.Value && Convert.ToBoolean(r["CanDauMayDay"]);

                        tongKm += km;
                        if (doc >= 15m || day) deoCount++;
                    }
                }
                txtStatusBarKg.Text = $"Mạng lưới: {count} phân đoạn liên hoàn • Tổng cự ly: {FormatHelper.FormatKm(tongKm)} km • {deoCount} phân đoạn đèo dốc hiểm trở";

                if (_dtKg != null && _dtKg.Rows.Count > 0)
                {
                    dgKhuGian.SelectedIndex = 0;
                }
                else
                {
                    ClearFormKg(isNew: true);
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi("Lỗi tải mạng lưới khu gian: " + ex.Message, "Lỗi CSDL");
            }
        }

        private void DgKhuGian_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isAddingKg) return;

            if (dgKhuGian.SelectedItem is DataRowView drv)
            {
                PopulateFormKg(drv);
            }
        }

        private void PopulateFormKg(DataRowView drv)
        {
            _currentMaKg = Convert.ToInt32(drv["MaKhuGian"]);
            _isAddingKg = false;

            int maGaDau = Convert.ToInt32(drv["MaGaDau"]);
            int maGaCuoi = Convert.ToInt32(drv["MaGaCuoi"]);
            string tenDau = drv["TenGaDau"]?.ToString() ?? "";
            string tenCuoi = drv["TenGaCuoi"]?.ToString() ?? "";

            txtTitleFormKg.Text = $"PHÂN ĐOẠN: {tenDau} ↔ {tenCuoi}";

            ChonItemTrongCombo(cboGaDau, maGaDau);
            ChonItemTrongCombo(cboGaCuoi, maGaCuoi);

            decimal culy = Convert.ToDecimal(drv["CuLyKm"]);
            txtCuLyKg.Text = FormatHelper.FormatKm(culy);
            txtVmaxKhach.Text = drv["TocDoToiDaKhach"]?.ToString() ?? "80";
            txtVmaxHang.Text = drv["TocDoToiDaHang"]?.ToString() ?? "50";
            txtDoDocKg.Text = drv["DoDocPermil"] != DBNull.Value ? Convert.ToDecimal(drv["DoDocPermil"]).ToString("0.0") : "0.0";
            chkCanDauMayDay.IsChecked = drv["CanDauMayDay"] != DBNull.Value && Convert.ToBoolean(drv["CanDauMayDay"]);

            string tt = drv["TrangThai"]?.ToString() ?? "RONG";
            if (tt == "RONG") cboTrangThaiKg.SelectedIndex = 0;
            else if (tt == "CO_TAU") cboTrangThaiKg.SelectedIndex = 1;
            else cboTrangThaiKg.SelectedIndex = 2;

            cboGaDau.IsEnabled = false;
            cboGaCuoi.IsEnabled = false;
            btnXoaKg.IsEnabled = true;
        }

        private void ChonItemTrongCombo(ComboBox cbo, int maGa)
        {
            foreach (ComboBoxItem it in cbo.Items)
            {
                if (it.Tag is int id && id == maGa)
                {
                    cbo.SelectedItem = it;
                    break;
                }
            }
        }

        private void ClearFormKg(bool isNew)
        {
            _isAddingKg = isNew;
            _currentMaKg = 0;
            txtTitleFormKg.Text = isNew ? "THÊM PHÂN ĐOẠN KHU GIAN MỚI" : "CHI TIẾT PHÂN ĐOẠN KHU GIAN";
            cboGaDau.IsEnabled = true;
            cboGaCuoi.IsEnabled = true;
            txtCuLyKg.Text = "30.00";
            txtVmaxKhach.Text = "80";
            txtVmaxHang.Text = "50";
            txtDoDocKg.Text = "0.0";
            chkCanDauMayDay.IsChecked = false;
            cboTrangThaiKg.SelectedIndex = 0;
            btnXoaKg.IsEnabled = !isNew;
            cboGaDau.Focus();
        }

        private void BtnThemKhuGianMoi_Click(object sender, RoutedEventArgs e)
        {
            ClearFormKg(isNew: true);
        }

        private void BtnHuyKg_Click(object sender, RoutedEventArgs e)
        {
            if (dgKhuGian.SelectedItem is DataRowView drv)
            {
                PopulateFormKg(drv);
            }
            else if (_dtKg != null && _dtKg.Rows.Count > 0)
            {
                dgKhuGian.SelectedIndex = 0;
            }
        }

        private void BtnLuuKg_Click(object sender, RoutedEventArgs e)
        {
            if (cboGaDau.SelectedItem is not ComboBoxItem itDau || itDau.Tag is not int maGaDau)
            {
                ThongBaoDialog.CanhBao("Vui lòng chọn Ga Đầu phân đoạn.", "Thiếu Dữ Liệu");
                return;
            }

            if (cboGaCuoi.SelectedItem is not ComboBoxItem itCuoi || itCuoi.Tag is not int maGaCuoi)
            {
                ThongBaoDialog.CanhBao("Vui lòng chọn Ga Cuối phân đoạn.", "Thiếu Dữ Liệu");
                return;
            }

            if (maGaDau == maGaCuoi)
            {
                ThongBaoDialog.CanhBao("Ga Đầu và Ga Cuối của phân đoạn khu gian không được trùng nhau.", "Lỗi Phân Đoạn");
                return;
            }

            if (!FormatHelper.TryParseKm(txtCuLyKg.Text, out decimal culy) || culy <= 0)
            {
                ThongBaoDialog.CanhBao("Cự ly phân đoạn phải là số thực lớn hơn 0 km.", "Cự Ly Không Hợp Lệ");
                txtCuLyKg.Focus();
                return;
            }

            if (!int.TryParse(txtVmaxKhach.Text.Trim(), out int vk) || vk <= 0)
            {
                ThongBaoDialog.CanhBao("Vận tốc tàu khách tối đa phải là số nguyên dương lớn hơn 0 km/h.", "Tốc Độ Không Hợp Lệ");
                txtVmaxKhach.Focus();
                return;
            }

            if (!int.TryParse(txtVmaxHang.Text.Trim(), out int vh) || vh <= 0)
            {
                ThongBaoDialog.CanhBao("Vận tốc tàu hàng tối đa phải là số nguyên dương lớn hơn 0 km/h.", "Tốc Độ Không Hợp Lệ");
                txtVmaxHang.Focus();
                return;
            }

            decimal doc = 0.0m;
            string docStr = txtDoDocKg.Text.Trim().Replace(',', '.');
            if (!decimal.TryParse(docStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out doc) || doc < 0)
            {
                ThongBaoDialog.CanhBao("Độ dốc dọc phải là số thực không âm (đơn vị ‰).", "Độ Dốc Không Hợp Lệ");
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
                CuLyKm = culy,
                TocDoToiDaKhach = vk,
                TocDoToiDaHang = vh,
                DoDocPermil = doc,
                CanDauMayDay = chkCanDauMayDay.IsChecked == true,
                TrangThai = trangThai
            };

            bool ok;
            string error;

            if (_isAddingKg || _currentMaKg == 0)
            {
                ok = _khuGianService.Them(kg, out error);
                if (ok)
                {
                    ThongBaoDialog.ThanhCong($"Đã thêm mới thành công phân đoạn khu gian ({FormatHelper.FormatKm(culy)} km) vào mạng lưới.", "Thêm Khu Gian Thành Công");
                    CoThayDoiDuLieu = true;
                    _isAddingKg = false;
                    LoadDanhSachKhuGian();
                }
                else
                {
                    ThongBaoDialog.CanhBao(error, "Lỗi Thêm Mới");
                }
            }
            else
            {
                ok = _khuGianService.CapNhat(kg, out error);
                if (ok)
                {
                    ThongBaoDialog.ThanhCong("Đã cập nhật thông số kỹ thuật phân đoạn khu gian thành công.", "Cập Nhật Thành Công");
                    CoThayDoiDuLieu = true;
                    LoadDanhSachKhuGian();
                }
                else
                {
                    ThongBaoDialog.CanhBao(error, "Lỗi Cập Nhật");
                }
            }
        }

        private void BtnXoaKg_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMaKg <= 0) return;

            string codeXacNhan = $"KG{_currentMaKg}";

            bool confirm = ThongBaoDialog.XacNhanXoaCoMa(
                $"Bạn đang yêu cầu xóa phân đoạn khu gian này khỏi mạng lưới đường sắt.\n\nĐây là hành động nguy hiểm có thể làm ngắt quãng hành trình chạy tàu toàn tuyến Bắc - Nam.",
                "XÁC NHẬN XÓA PHÂN ĐOẠN KHU GIAN",
                codeXacNhan,
                nutXoa: "Xóa khu gian",
                nutHuy: "Hủy bỏ");

            if (confirm)
            {
                if (_khuGianService.Xoa(_currentMaKg, out string error))
                {
                    ThongBaoDialog.ThanhCong("Đã xóa phân đoạn khu gian thành công.", "Đã Xóa");
                    CoThayDoiDuLieu = true;
                    LoadDanhSachKhuGian();
                }
                else
                {
                    ThongBaoDialog.Loi(error, "Không Thể Xóa");
                }
            }
        }
        #endregion

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
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
