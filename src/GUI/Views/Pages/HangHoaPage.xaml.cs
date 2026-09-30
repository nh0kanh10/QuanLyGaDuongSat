using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using BUS.Services;
using ET.VanTai;
using DTO.Common;
using SymbolIcon = Wpf.Ui.Controls.SymbolIcon;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.Pages
{
    public partial class HangHoaPage : Page
    {
        private readonly VanDonHangService _service = new();
        private readonly GaService _gaService = new();
        private DataTable? _dtGa;
        private DataTable? _dtLoaiHang;
        private DataTable? _cachedVanDon;
        private bool _isInitializing = true;
        private int? _editingMaVanDon = null; // null = lập đơn mới, có giá trị = chỉnh sửa đơn

        public HangHoaPage()
        {
            InitializeComponent();
            Loaded += HangHoaPage_Loaded;
        }

        private void HangHoaPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _isInitializing = true;

                // 1. Tải danh mục ga
                _dtGa = _gaService.LayDanhSach();
                if (_dtGa != null && _dtGa.Rows.Count > 0)
                {
                    cboGaGui.ItemsSource = _dtGa.DefaultView;
                    cboGaNhan.ItemsSource = _dtGa.DefaultView;

                    // Tạo bản sao cho bộ lọc Tab 2
                    DataTable dtLocGa = _dtGa.Copy();
                    DataRow rowAll = dtLocGa.NewRow();
                    rowAll["MaGa"] = 0;
                    rowAll["TenGa"] = "Tất cả các ga";
                    dtLocGa.Rows.InsertAt(rowAll, 0);
                    cboLocGa.ItemsSource = dtLocGa.DefaultView;
                    cboLocGa.SelectedIndex = 0;

                    // Mặc định: Ga Hà Nội (đầu) -> Ga Sài Gòn (cuối)
                    cboGaGui.SelectedIndex = 0;
                    cboGaNhan.SelectedIndex = _dtGa.Rows.Count > 1 ? _dtGa.Rows.Count - 1 : 0;
                }

                // 2. Tải danh mục loại hàng
                _dtLoaiHang = _service.LayDanhSachLoaiHangHoa();
                if (_dtLoaiHang == null || _dtLoaiHang.Rows.Count == 0)
                {
                    _dtLoaiHang = new DataTable();
                    _dtLoaiHang.Columns.Add("MaLoaiHang", typeof(int));
                    _dtLoaiHang.Columns.Add("TenHangHoa", typeof(string));
                    _dtLoaiHang.Columns.Add("NhomCuoc", typeof(int));
                    _dtLoaiHang.Columns.Add("DonGiaMoiTanKm", typeof(decimal));
                    _dtLoaiHang.Rows.Add(1, "Hàng tiêu dùng đóng gói", 1, 450.00m);
                    _dtLoaiHang.Rows.Add(2, "Nông sản đông lạnh", 2, 600.00m);
                    _dtLoaiHang.Rows.Add(3, "Xe máy nguyên chiếc", 3, 1200.00m);
                    _dtLoaiHang.Rows.Add(4, "Hàng công nghiệp nặng / Máy móc", 3, 850.00m);
                    _dtLoaiHang.Rows.Add(5, "Linh kiện điện tử / Thiết bị công nghệ", 2, 750.00m);
                    _dtLoaiHang.Rows.Add(6, "Bưu phẩm bưu chính / Chuyển phát nhanh", 1, 550.00m);
                }
                cboLoaiHang.ItemsSource = _dtLoaiHang.DefaultView;

                // 3. Khởi tạo mã vận đơn mới
                SinhMaVanDonMoi();

                // 4. Đồng bộ loại hàng theo hình thức ban đầu (XE_MAY)
                ApDungHinhThucVanChuyen();

                // 5. Tải danh sách vận đơn
                NapLaiDanhSachVanDon();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo màn hình Hàng Hóa: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isInitializing = false;
                CapNhatTinhCuoc();
            }
        }

        private void SinhMaVanDonMoi()
        {
            _editingMaVanDon = null;
            txtMaVD.Text = $"VD-{DateTime.Now:yyMMddHHmmss}";
            DatCheDoNhapLieu(isEdit: false);
        }

        // Điều chỉnh trạng thái các ô nhập liệu tùy theo chế độ (Lập mới / Sửa / Xem)
        private void DatCheDoNhapLieu(bool isEdit, bool isReadOnly = false)
        {
            if (!isEdit)
            {
                txtCheDo.Text = "LẬP ĐƠN MỚI";
                badgeCheDo.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
                badgeCheDo.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
                txtCheDo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
                txtBtnLuu.Text = "Lưu Vận Đơn (F2)";
                btnHuyDonTab1.Visibility = Visibility.Collapsed;
                btnLuu.IsEnabled = true;
                btnLuu.Opacity = 1.0;
                SetFormEnabled(true);
            }
            else if (isReadOnly)
            {
                txtCheDo.Text = "XEM CHI TIẾT (ĐÃ XẾP TOA)";
                badgeCheDo.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                badgeCheDo.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                txtCheDo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                txtBtnLuu.Text = "Đã Khóa Sửa";
                btnHuyDonTab1.Visibility = Visibility.Collapsed;
                btnLuu.IsEnabled = false;
                btnLuu.Opacity = 0.5;
                SetFormEnabled(false);
            }
            else
            {
                txtCheDo.Text = "CHẾ ĐỘ SỬA ĐƠN";
                badgeCheDo.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                badgeCheDo.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                txtCheDo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                txtBtnLuu.Text = "Cập Nhật (F2)";
                btnHuyDonTab1.Visibility = Visibility.Visible;
                btnLuu.IsEnabled = true;
                btnLuu.Opacity = 1.0;
                SetFormEnabled(true);
            }
        }

        private void SetFormEnabled(bool enabled)
        {
            txtNguoiGui.IsEnabled = enabled;
            txtSDTGui.IsEnabled = enabled;
            txtNguoiNhan.IsEnabled = enabled;
            txtSDTNhan.IsEnabled = enabled;
            txtGhiChu.IsEnabled = enabled;
            cboLoaiHang.IsEnabled = enabled && (LayLoaiVanChuyenHienTai() != "XE_MAY");
            txtTrongLuong.IsEnabled = enabled;
            txtTheTich.IsEnabled = enabled;
            cboGaGui.IsEnabled = enabled;
            cboGaNhan.IsEnabled = enabled;
            cboLoaiVC.IsEnabled = enabled;
            txtBienSo.IsEnabled = enabled;
            chkDaRutXang.IsEnabled = enabled;
            txtSoContainer.IsEnabled = enabled;
            txtSoChiHQ.IsEnabled = enabled;
        }

        /// <summary>
        /// Xử lý nghiệp vụ tách bạch giữa: XE_MAY, CONTAINER, HANG_HOA.
        /// </summary>
        private void ApDungHinhThucVanChuyen()
        {
            if (_dtLoaiHang == null || cboLoaiHang == null || pnlXeMay == null || pnlContainer == null) return;

            string loaiVC = LayLoaiVanChuyenHienTai();

            if (loaiVC == "XE_MAY")
            {
                _dtLoaiHang.DefaultView.RowFilter = "TenHangHoa LIKE '%Xe máy%'";
                cboLoaiHang.IsEnabled = false;
                if (cboLoaiHang.Items.Count > 0) cboLoaiHang.SelectedIndex = 0;

                pnlXeMay.Visibility = Visibility.Visible;
                pnlContainer.Visibility = Visibility.Collapsed;

                txtTrongLuong.Text = "0.15";
                txtTheTich.Text = "0.00";
            }
            else if (loaiVC == "CONTAINER")
            {
                _dtLoaiHang.DefaultView.RowFilter = "TenHangHoa NOT LIKE '%Xe máy%'";
                cboLoaiHang.IsEnabled = true;

                ChonMatHangTheoTuKhoa("công nghiệp");

                pnlXeMay.Visibility = Visibility.Collapsed;
                pnlContainer.Visibility = Visibility.Visible;

                if (FormatHelper.TryParseWeight(txtTrongLuong.Text, out decimal w) && w <= 1.0m)
                {
                    txtTrongLuong.Text = "20.00";
                    txtTheTich.Text = "45.00";
                }
            }
            else // HANG_HOA
            {
                _dtLoaiHang.DefaultView.RowFilter = "TenHangHoa NOT LIKE '%Xe máy%'";
                cboLoaiHang.IsEnabled = true;

                ChonMatHangTheoTuKhoa("tiêu dùng");

                pnlXeMay.Visibility = Visibility.Collapsed;
                pnlContainer.Visibility = Visibility.Collapsed;

                if (FormatHelper.TryParseWeight(txtTrongLuong.Text, out decimal w) && (w == 0.15m || w >= 20.0m))
                {
                    txtTrongLuong.Text = "1.00";
                    txtTheTich.Text = "2.00";
                }
            }
        }

        private void ChonMatHangTheoTuKhoa(string keyword)
        {
            for (int i = 0; i < cboLoaiHang.Items.Count; i++)
            {
                if (cboLoaiHang.Items[i] is DataRowView row &&
                    (row["TenHangHoa"]?.ToString() ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    cboLoaiHang.SelectedIndex = i;
                    return;
                }
            }
            if (cboLoaiHang.Items.Count > 0) cboLoaiHang.SelectedIndex = 0;
        }

        private string LayLoaiVanChuyenHienTai()
        {
            if (cboLoaiVC.SelectedItem is ComboBoxItem item)
                return item.Tag?.ToString() ?? "HANG_HOA";
            return "HANG_HOA";
        }

        private void NapLaiDanhSachVanDon()
        {
            try
            {
                _cachedVanDon = _service.LayDanhSach();
                dgDonMoiLap.ItemsSource = _cachedVanDon.DefaultView;
                dgVanDon.ItemsSource = _cachedVanDon.DefaultView;

                CapNhatThongKe();
                CapNhatKpiThongKe();

                if (dgDonMoiLap.Items.Count > 0) dgDonMoiLap.SelectedIndex = 0;
                ThucHienLocVaTimKiem();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách vận đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CapNhatThongKe()
        {
            if (_cachedVanDon == null)
            {
                txtThongKe.Text = "Tổng: 0 vận đơn";
                txtSoLuongCaTruc.Text = "0 vận đơn";
                return;
            }
            int tong = _cachedVanDon.DefaultView.Count;
            txtThongKe.Text = $"Tổng: {tong} vận đơn";
            txtSoLuongCaTruc.Text = $"{tong} vận đơn";
        }

        private void CapNhatKpiThongKe()
        {
            if (_cachedVanDon == null || txtKpiTatCa == null) return;

            int tong = _cachedVanDon.Rows.Count;
            int choXep = 0;
            int dangChay = 0;
            int daDen = 0;
            int daGiao = 0;

            foreach (DataRow row in _cachedVanDon.Rows)
            {
                string tt = row["TrangThai"]?.ToString() ?? "";
                switch (tt)
                {
                    case "DA_NHAN":
                    case "DA_XEP":
                        choXep++;
                        break;
                    case "DANG_VAN_CHUYEN":
                        dangChay++;
                        break;
                    case "DA_DEN":
                        daDen++;
                        break;
                    case "DA_GIAO":
                        daGiao++;
                        break;
                }
            }

            txtKpiTatCa.Text = tong.ToString();
            txtKpiChoXep.Text = choXep.ToString();
            txtKpiDangChay.Text = dangChay.ToString();
            txtKpiDaDen.Text = daDen.ToString();
            txtKpiDaGiao.Text = daGiao.ToString();
        }

        // 2. TÍNH CƯỚC TỰ ĐỘNG
        private void CapNhatTinhCuoc()
        {
            if (_isInitializing) return;

            // Cự ly
            decimal lyTrinhGui = 0, lyTrinhNhan = 0;
            if (cboGaGui.SelectedItem is DataRowView rowGui)
                lyTrinhGui = rowGui["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(rowGui["LyTrinhKm"]) : 0;
            if (cboGaNhan.SelectedItem is DataRowView rowNhan)
                lyTrinhNhan = rowNhan["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(rowNhan["LyTrinhKm"]) : 0;

            decimal cuLyKm = Math.Abs(lyTrinhNhan - lyTrinhGui);
            txtCuLyKm.Text = $"{cuLyKm:N1} km";

            // Trọng lượng
            if (!FormatHelper.TryParseWeight(txtTrongLuong.Text, out decimal trongLuongTan))
                trongLuongTan = 0.15m;

            decimal theTichM3 = 0;
            decimal.TryParse(txtTheTich.Text.Trim(), out theTichM3);
            if (theTichM3 < 0) theTichM3 = 0;

            decimal quyDoiTan = theTichM3 * 0.333m;
            decimal trongLuongTinhCuoc = Math.Max(trongLuongTan, quyDoiTan);
            txtTrongLuongTinhCuoc.Text = $"{trongLuongTinhCuoc:N2} tấn";

            // Loại hàng
            int nhomCuoc = 1;
            decimal donGiaTanKm = 500m;
            if (cboLoaiHang.SelectedItem is DataRowView rowHang)
            {
                nhomCuoc = rowHang["NhomCuoc"] != DBNull.Value ? Convert.ToInt32(rowHang["NhomCuoc"]) : 1;
                donGiaTanKm = rowHang["DonGiaMoiTanKm"] != DBNull.Value ? Convert.ToDecimal(rowHang["DonGiaMoiTanKm"]) : 500m;
            }

            decimal heSoNhom = nhomCuoc switch { 2 => 1.15m, 3 => 1.30m, _ => 1.00m };

            // Tính cước
            decimal cuocPhi = _service.TinhCuocPhi(trongLuongTan, theTichM3, cuLyKm, nhomCuoc, donGiaTanKm);
            txtCuocPhiText.Text = FormatHelper.FormatCurrency(cuocPhi);
            txtGiaiTrinhCuoc.Text = $"Q: {trongLuongTinhCuoc:N2}t × L: {cuLyKm:N0}km × P: {donGiaTanKm:N0}đ × H: {heSoNhom:N2}";
        }

        private void CboGa_SelectionChanged(object sender, SelectionChangedEventArgs e) => CapNhatTinhCuoc();
        private void CboLoaiHang_SelectionChanged(object sender, SelectionChangedEventArgs e) => CapNhatTinhCuoc();

        private void TxtTrongLuong_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FormatHelper.TryParseWeight(txtTrongLuong.Text, out decimal weight))
                txtTrongLuong.Text = FormatHelper.FormatWeight(weight);
            CapNhatTinhCuoc();
        }

        private void TxtTheTich_LostFocus(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtTheTich.Text.Trim(), out decimal m3))
                txtTheTich.Text = $"{m3:0.00}";
            CapNhatTinhCuoc();
        }

        private void TxtThongSoCuoc_TextChanged(object sender, TextChangedEventArgs e) => CapNhatTinhCuoc();

        private void CboLoaiVC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing)
            {
                ApDungHinhThucVanChuyen();
                CapNhatTinhCuoc();
            }
        }

        // 3. LẬP VẬN ĐƠN 
        // 3. LẬP VẬN ĐƠN & QUẢN LÝ DỮ LIỆU
        private void BtnLapDonMoi_Click(object sender, RoutedEventArgs e)
        {
            dgDonMoiLap.SelectedItem = null;
            SinhMaVanDonMoi();

            txtNguoiGui.Text = "";
            txtSDTGui.Text = "";
            txtNguoiNhan.Text = "";
            txtSDTNhan.Text = "";
            txtBienSo.Text = "";
            chkDaRutXang.IsChecked = true;
            txtSoContainer.Text = "";
            txtSoChiHQ.Text = "";
            txtGhiChu.Text = "";

            string loaiVC = LayLoaiVanChuyenHienTai();
            if (loaiVC == "XE_MAY")
            {
                txtTrongLuong.Text = "0.15";
                txtTheTich.Text = "0.00";
            }
            else if (loaiVC == "CONTAINER")
            {
                txtTrongLuong.Text = "20.00";
                txtTheTich.Text = "45.00";
            }
            else
            {
                txtTrongLuong.Text = "1.00";
                txtTheTich.Text = "2.00";
            }

            CapNhatTinhCuoc();
            txtNguoiGui.Focus();
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!btnLuu.IsEnabled) return;

            string maVD = txtMaVD.Text.Trim();
            int maGaGui = cboGaGui.SelectedValue != null ? Convert.ToInt32(cboGaGui.SelectedValue) : 0;
            int maGaNhan = cboGaNhan.SelectedValue != null ? Convert.ToInt32(cboGaNhan.SelectedValue) : 0;
            int maLoaiHang = cboLoaiHang.SelectedValue != null ? Convert.ToInt32(cboLoaiHang.SelectedValue) : 0;

            // Kiểm tra ga gửi và ga nhận
            if (maGaGui <= 0 || maGaNhan <= 0)
            {
                MessageBox.Show("Vui lòng chọn ga gửi và ga nhận hợp lệ.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (maGaGui == maGaNhan)
            {
                MessageBox.Show("Ga gửi và ga nhận không được trùng nhau.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Kiểm tra thông tin người gửi / người nhận
            if (string.IsNullOrWhiteSpace(txtNguoiGui.Text))
            {
                MessageBox.Show("Vui lòng nhập họ tên người gửi hàng.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNguoiGui.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtSDTGui.Text))
            {
                MessageBox.Show("Vui lòng nhập số điện thoại người gửi.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSDTGui.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtNguoiNhan.Text))
            {
                MessageBox.Show("Vui lòng nhập họ tên người nhận hàng.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNguoiNhan.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtSDTNhan.Text))
            {
                MessageBox.Show("Vui lòng nhập số điện thoại người nhận.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSDTNhan.Focus();
                return;
            }

            // Trọng lượng & cước phí
            if (!FormatHelper.TryParseWeight(txtTrongLuong.Text, out decimal tl) || tl <= 0)
            {
                MessageBox.Show("Trọng lượng hàng hóa phải lớn hơn 0.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTrongLuong.Focus();
                return;
            }
            if (!FormatHelper.TryParseCurrency(txtCuocPhiText.Text, out decimal cuoc) || cuoc <= 0)
            {
                MessageBox.Show("Cước phí chưa được tính hợp lệ.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string loaiVC = LayLoaiVanChuyenHienTai();

            // Kiểm tra điều kiện đặc thù xe máy
            if (loaiVC == "XE_MAY")
            {
                if (string.IsNullOrWhiteSpace(txtBienSo.Text))
                {
                    MessageBox.Show("Vui lòng nhập Biển kiểm soát của xe máy ký gửi.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtBienSo.Focus();
                    return;
                }
                if (chkDaRutXang.IsChecked != true)
                {
                    MessageBox.Show("Quy chuẩn an toàn PCCC VNR: Xe máy chưa rút sạch xăng không được phép tiếp nhận vận chuyển.",
                                    "Cảnh báo PCCC", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
            }
            // Kiểm tra điều kiện đặc thù container
            else if (loaiVC == "CONTAINER")
            {
                if (string.IsNullOrWhiteSpace(txtSoContainer.Text))
                {
                    MessageBox.Show("Vui lòng nhập Số hiệu Container đường sắt.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtSoContainer.Focus();
                    return;
                }
                if (tl < 1.0m)
                {
                    MessageBox.Show("Trọng lượng container phải từ 1.00 tấn trở lên.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtTrongLuong.Focus();
                    return;
                }
            }

            var vd = new VanDonHang
            {
                MaVanDon = _editingMaVanDon ?? 0,
                MaVanDonCode = maVD,
                TenNguoiGui = txtNguoiGui.Text.Trim(),
                SDTNguoiGui = txtSDTGui.Text.Trim(),
                TenNguoiNhan = txtNguoiNhan.Text.Trim(),
                SDTNguoiNhan = txtSDTNhan.Text.Trim(),
                MaGaGui = maGaGui,
                MaGaNhan = maGaNhan,
                MaLoaiHang = maLoaiHang,
                TrongLuongTan = tl,
                CuocPhi = cuoc,
                LoaiVanChuyen = loaiVC,
                TrangThai = "DA_NHAN",
                GhiChu = string.IsNullOrWhiteSpace(txtGhiChu.Text) ? null : txtGhiChu.Text.Trim()
            };

            if (loaiVC == "XE_MAY")
            {
                vd.BienKiemSoat = txtBienSo.Text.Trim().ToUpperInvariant();
                vd.DaRutXang = chkDaRutXang.IsChecked == true;
            }
            else if (loaiVC == "CONTAINER")
            {
                vd.SoHieuContainer = txtSoContainer.Text.Trim().ToUpperInvariant();
                vd.SoChiHaiQuan = string.IsNullOrWhiteSpace(txtSoChiHQ.Text) ? null : txtSoChiHQ.Text.Trim().ToUpperInvariant();
            }

            if (_editingMaVanDon.HasValue)
            {
                // Thực hiện cập nhật vận đơn
                if (!_service.CapNhat(vd, out string error))
                {
                    MessageBox.Show($"Không thể cập nhật vận đơn: {error}", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show($"Cập nhật thành công vận đơn #{maVD}.\nTổng cước thu: {FormatHelper.FormatCurrency(cuoc)} VNĐ.",
                                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Thực hiện thêm mới vận đơn
                if (!_service.Them(vd, out string error))
                {
                    MessageBox.Show($"Không thể lưu vận đơn: {error}", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show($"Lưu thành công vận đơn #{maVD}.\nTổng cước thu: {FormatHelper.FormatCurrency(cuoc)} VNĐ.",
                                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                SinhMaVanDonMoi();
            }

            NapLaiDanhSachVanDon();
        }

        // Hủy (xóa) vận đơn khi còn ở trạng thái tiếp nhận
        private void BtnHuyDon_Click(object sender, RoutedEventArgs e)
        {
            DataRowView? row = null;
            if (tabMainHangHoa.SelectedItem == tabLapDon)
                row = dgDonMoiLap.SelectedItem as DataRowView;
            else
                row = dgVanDon.SelectedItem as DataRowView;

            int maVanDon = 0;
            string maCode = "";
            string trangThai = "";

            if (row != null)
            {
                maVanDon = Convert.ToInt32(row["MaVanDon"]);
                maCode = row["MaVanDonCode"]?.ToString() ?? "";
                trangThai = row["TrangThai"]?.ToString() ?? "";
            }
            else if (_editingMaVanDon.HasValue)
            {
                maVanDon = _editingMaVanDon.Value;
                maCode = txtMaVD.Text.Trim();
                trangThai = "DA_NHAN";
            }

            if (maVanDon <= 0)
            {
                MessageBox.Show("Vui lòng chọn vận đơn cần hủy.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (trangThai != "DA_NHAN")
            {
                MessageBox.Show($"Vận đơn #{maCode} đã được xếp toa hoặc vận chuyển, không thể hủy bỏ.", 
                                "Không thể hủy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn hủy (xóa) vận đơn #{maCode} khỏi hệ thống không?\n" +
                $"Thao tác này sẽ xóa phiếu tiếp nhận khỏi ca trực.",
                "Xác nhận hủy vận đơn", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                if (_service.Huy(maVanDon, out string error))
                {
                    MessageBox.Show($"Đã hủy thành công vận đơn #{maCode}.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    NapLaiDanhSachVanDon();
                    SinhMaVanDonMoi();
                }
                else
                {
                    MessageBox.Show($"Hủy vận đơn thất bại: {error}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Chuyển từ Tab 2 sang Tab 1 để chỉnh sửa vận đơn
        private void BtnSuaDonTab2_Click(object sender, RoutedEventArgs e)
        {
            if (dgVanDon.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Vui lòng chọn vận đơn cần chỉnh sửa.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string trangThai = row["TrangThai"]?.ToString() ?? "";
            if (trangThai != "DA_NHAN")
            {
                MessageBox.Show($"Vận đơn #{row["MaVanDonCode"]} đã xếp toa hoặc đang chạy trên ray, không được phép sửa đổi.",
                                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            tabMainHangHoa.SelectedItem = tabLapDon;
            dgDonMoiLap.SelectedItem = row;
        }

        private void DpNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing) ThucHienLocVaTimKiem();
        }

        /// <summary>
        /// Tương tác 2 chiều: Click chọn dòng trong DataGrid -> Đổ toàn bộ dữ liệu lên Form để tra cứu / xem lại / chỉnh sửa.
        /// </summary>
        private void DgDonMoiLap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDonMoiLap.SelectedItem is not DataRowView row) return;

            _isInitializing = true;
            try
            {
                int maVD = Convert.ToInt32(row["MaVanDon"]);
                _editingMaVanDon = maVD;
                string trangThai = row["TrangThai"]?.ToString() ?? "DA_NHAN";

                // Kích hoạt chế độ sửa hoặc xem chỉ đọc tùy trạng thái
                DatCheDoNhapLieu(isEdit: true, isReadOnly: trangThai != "DA_NHAN");

                txtMaVD.Text = row["MaVanDonCode"]?.ToString() ?? "";

                string loaiVC = row["LoaiVanChuyen"]?.ToString() ?? "HANG_HOA";
                for (int i = 0; i < cboLoaiVC.Items.Count; i++)
                {
                    if (cboLoaiVC.Items[i] is ComboBoxItem item && item.Tag?.ToString() == loaiVC)
                    {
                        cboLoaiVC.SelectedIndex = i;
                        break;
                    }
                }
                ApDungHinhThucVanChuyen();

                cboGaGui.SelectedValue = row["MaGaGui"];
                cboGaNhan.SelectedValue = row["MaGaNhan"];
                txtNguoiGui.Text = row["TenNguoiGui"]?.ToString() ?? "";
                txtSDTGui.Text = row["SDTNguoiGui"]?.ToString() ?? "";
                txtNguoiNhan.Text = row["TenNguoiNhan"]?.ToString() ?? "";
                txtSDTNhan.Text = row["SDTNguoiNhan"]?.ToString() ?? "";

                cboLoaiHang.SelectedValue = row["MaLoaiHang"];

                decimal tl = row["TrongLuongTan"] != DBNull.Value ? Convert.ToDecimal(row["TrongLuongTan"]) : 0;
                txtTrongLuong.Text = FormatHelper.FormatWeight(tl);

                txtBienSo.Text = row["BienKiemSoat"]?.ToString() ?? "";
                chkDaRutXang.IsChecked = row["DaRutXang"] != DBNull.Value && Convert.ToBoolean(row["DaRutXang"]);
                txtSoContainer.Text = row["SoHieuContainer"]?.ToString() ?? "";
                txtSoChiHQ.Text = row["SoChiHaiQuan"]?.ToString() ?? "";
                txtGhiChu.Text = row["GhiChu"]?.ToString() ?? "";

                decimal cuoc = row["CuocPhi"] != DBNull.Value ? Convert.ToDecimal(row["CuocPhi"]) : 0;
                txtCuocPhiText.Text = FormatHelper.FormatCurrency(cuoc);
            }
            finally
            {
                _isInitializing = false;
                CapNhatTinhCuoc();
            }

            // Đồng bộ lựa chọn với bảng bên Tab 2
            dgVanDon.SelectedItem = row;
        }

        // =========================================================================
        // BỘ LỌC NHANH TRONG CA TRỰC (TOOLBAR TAB 1)
        // =========================================================================
        private void RadLocHinhThuc_Click(object sender, RoutedEventArgs e) => ApDungLocCaTruc();
        private void TxtTimNhanhCaTruc_TextChanged(object sender, TextChangedEventArgs e) => ApDungLocCaTruc();

        private void ApDungLocCaTruc()
        {
            if (_cachedVanDon == null) return;
            var filters = new List<string>();

            // Lọc theo hình thức
            if (radLocXeMay.IsChecked == true)
                filters.Add("LoaiVanChuyen = 'XE_MAY'");
            else if (radLocContainer.IsChecked == true)
                filters.Add("LoaiVanChuyen = 'CONTAINER'");
            else if (radLocHangRoi.IsChecked == true)
                filters.Add("LoaiVanChuyen = 'HANG_HOA'");

            // Tìm nhanh từ khóa
            string kw = txtTimNhanhCaTruc.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrEmpty(kw))
            {
                filters.Add($"(MaVanDonCode LIKE '%{kw}%' OR TenNguoiGui LIKE '%{kw}%' OR TenNguoiNhan LIKE '%{kw}%' OR ThongTinDacThu LIKE '%{kw}%')");
            }

            _cachedVanDon.DefaultView.RowFilter = string.Join(" AND ", filters);
            txtSoLuongCaTruc.Text = $"{_cachedVanDon.DefaultView.Count} vận đơn";
        }

        // =========================================================================
        // 4. TRA CỨU TIẾN ĐỘ & GIAO NHẬN (TAB 2)
        // =========================================================================
        private void TxtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (btnClearTimKiem != null)
            {
                btnClearTimKiem.Visibility = string.IsNullOrEmpty(txtTimKiem.Text) ? Visibility.Collapsed : Visibility.Visible;
            }
            ThucHienLocVaTimKiem();
        }

        private void BtnClearTimKiem_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Text = "";
            txtTimKiem.Focus();
        }

        private void TxtTimKiem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ThucHienLocVaTimKiem();
        }

        private void BtnTimKiem_Click(object sender, RoutedEventArgs e) => ThucHienLocVaTimKiem();

        private void RadKpiLoc_Click(object sender, RoutedEventArgs e) => ThucHienLocVaTimKiem();

        private void RadLocHinhThucTab2_Click(object sender, RoutedEventArgs e) => ThucHienLocVaTimKiem();

        private void CboLoc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing) ThucHienLocVaTimKiem();
        }

        private void ThucHienLocVaTimKiem()
        {
            if (_cachedVanDon == null) return;
            var filterList = new List<string>();

            // 1. Tìm kiếm Realtime theo từ khóa (Mã đơn, Người gửi, Người nhận, SĐT, BKS, Số Cont)
            string keyword = txtTimKiem.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrEmpty(keyword))
            {
                filterList.Add($"(MaVanDonCode LIKE '%{keyword}%' OR TenNguoiGui LIKE '%{keyword}%' OR TenNguoiNhan LIKE '%{keyword}%' OR SDTNguoiGui LIKE '%{keyword}%' OR SDTNguoiNhan LIKE '%{keyword}%' OR ThongTinDacThu LIKE '%{keyword}%' OR TenHangHoa LIKE '%{keyword}%')");
            }

            // 2. Bộ lọc trạng thái vận chuyển từ KPI Segment Buttons
            if (radKpiChoXep.IsChecked == true)
            {
                filterList.Add("TrangThai IN ('DA_NHAN', 'DA_XEP')");
            }
            else if (radKpiDangChay.IsChecked == true)
            {
                filterList.Add("TrangThai = 'DANG_VAN_CHUYEN'");
            }
            else if (radKpiDaDen.IsChecked == true)
            {
                filterList.Add("TrangThai = 'DA_DEN'");
            }
            else if (radKpiDaGiao.IsChecked == true)
            {
                filterList.Add("TrangThai = 'DA_GIAO'");
            }

            // 3. Bộ lọc Ga đến
            if (cboLocGa.SelectedValue != null && Convert.ToInt32(cboLocGa.SelectedValue) > 0)
            {
                filterList.Add($"MaGaNhan = {cboLocGa.SelectedValue}");
            }

            // 4. Bộ lọc Hình thức vận chuyển (Chips)
            if (radLocHinhThucXeMay.IsChecked == true)
            {
                filterList.Add("LoaiVanChuyen = 'XE_MAY'");
            }
            else if (radLocHinhThucCont.IsChecked == true)
            {
                filterList.Add("LoaiVanChuyen = 'CONTAINER'");
            }
            else if (radLocHinhThucHangRoi.IsChecked == true)
            {
                filterList.Add("LoaiVanChuyen = 'HANG_HOA'");
            }

            // 5. Bộ lọc theo khoảng ngày tạo
            if (dpTuNgay != null && dpTuNgay.SelectedDate.HasValue)
            {
                string tuNgayStr = dpTuNgay.SelectedDate.Value.ToString("yyyy-MM-dd 00:00:00");
                filterList.Add($"ThoiDiemTao >= #{tuNgayStr}#");
            }
            if (dpDenNgay != null && dpDenNgay.SelectedDate.HasValue)
            {
                string denNgayStr = dpDenNgay.SelectedDate.Value.ToString("yyyy-MM-dd 23:59:59");
                filterList.Add($"ThoiDiemTao <= #{denNgayStr}#");
            }

            _cachedVanDon.DefaultView.RowFilter = filterList.Count > 0 ? string.Join(" AND ", filterList) : "";

            int count = _cachedVanDon.DefaultView.Count;
            txtThongKe.Text = $"Tổng: {count} vận đơn";

            if (count > 0)
            {
                if (dgVanDon.SelectedItem == null || !dgVanDon.Items.Contains(dgVanDon.SelectedItem))
                {
                    dgVanDon.SelectedIndex = 0;
                }
            }
            else
            {
                XoaChiTietInspector();
            }
        }

        private void XoaChiTietInspector()
        {
            txtDetailMaVD.Text = "---";
            txtDetailTrangThai.Text = "---";
            bdDetailStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
            bdDetailStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            txtDetailTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

            txtDetailTuyen.Text = "---";
            txtDetailHangHoa.Text = "---";
            txtDetailDacThu.Text = "---";
            txtDetailNguoiGui.Text = "---";
            txtDetailNguoiNhan.Text = "---";
            txtDetailGhiChu.Text = "---";
            txtDetailCuocPhi.Text = "0 VNĐ";
            txtCurrentStepName.Text = "---";
            btnGiaoHang.IsEnabled = false;
            btnSuaDonTab2.IsEnabled = false;
            btnHuyDonTab2.IsEnabled = false;
            btnSuaDonTab2.Opacity = 0.5;
            btnHuyDonTab2.Opacity = 0.5;

            CapNhatHienThiTienTrinh("");
        }

        private void DgVanDon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVanDon.SelectedItem is not DataRowView row)
            {
                XoaChiTietInspector();
                return;
            }

            string code = row["MaVanDonCode"]?.ToString() ?? "";
            string gaGui = row["TenGaGui"]?.ToString() ?? "";
            string gaNhan = row["TenGaNhan"]?.ToString() ?? "";
            string loaiVC = row["LoaiVanChuyen"]?.ToString() ?? "";
            string tenHang = row["TenHangHoa"]?.ToString() ?? "";
            decimal cuoc = row["CuocPhi"] != DBNull.Value ? Convert.ToDecimal(row["CuocPhi"]) : 0;
            string bienKS = row["BienKiemSoat"] != DBNull.Value ? row["BienKiemSoat"].ToString()! : "";
            string cont = row["SoHieuContainer"] != DBNull.Value ? row["SoHieuContainer"].ToString()! : "";
            string ghiChu = row["GhiChu"] != DBNull.Value ? row["GhiChu"].ToString()! : "";
            string trangThai = row["TrangThai"]?.ToString() ?? "DA_NHAN";

            txtDetailMaVD.Text = code;

            txtDetailTrangThai.Text = trangThai switch
            {
                "DA_NHAN" => "Đã tiếp nhận",
                "DA_XEP" => "Đã xếp toa",
                "DANG_VAN_CHUYEN" => "Đang chạy trên ray",
                "DA_DEN" => "Đã đến ga nhận (Chờ giao)",
                "DA_GIAO" => "Đã giao khách hoàn tất",
                _ => trangThai
            };

            switch (trangThai)
            {
                case "DA_NHAN":
                    bdDetailStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    bdDetailStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                    txtDetailTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                    break;
                case "DA_XEP":
                    bdDetailStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    bdDetailStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
                    txtDetailTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
                    break;
                case "DANG_VAN_CHUYEN":
                    bdDetailStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0F2FE"));
                    bdDetailStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BAE6FD"));
                    txtDetailTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
                    break;
                case "DA_DEN":
                    bdDetailStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                    bdDetailStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));
                    txtDetailTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                    break;
                case "DA_GIAO":
                    bdDetailStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
                    bdDetailStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBF7D0"));
                    txtDetailTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                    break;
                default:
                    bdDetailStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    bdDetailStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                    txtDetailTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                    break;
            }

            txtDetailTuyen.Text = $"{gaGui} → {gaNhan}";
            txtDetailNguoiGui.Text = $"{row["TenNguoiGui"]} ({row["SDTNguoiGui"]})";
            txtDetailNguoiNhan.Text = $"{row["TenNguoiNhan"]} ({row["SDTNguoiNhan"]})";

            string loaiVCViet = loaiVC switch
            {
                "XE_MAY" => "Xe máy ký gửi",
                "CONTAINER" => "Container",
                _ => "Hàng hóa rời"
            };
            txtDetailHangHoa.Text = $"{tenHang} ({loaiVCViet})";

            txtDetailCuocPhi.Text = $"{FormatHelper.FormatCurrency(cuoc)} VNĐ";
            txtDetailGhiChu.Text = string.IsNullOrWhiteSpace(ghiChu) ? "—" : ghiChu;

            if (!string.IsNullOrEmpty(bienKS))
                txtDetailDacThu.Text = $"BKS: {bienKS}";
            else if (!string.IsNullOrEmpty(cont))
                txtDetailDacThu.Text = $"Cont: {cont}";
            else
                txtDetailDacThu.Text = "—";

            CapNhatHienThiTienTrinh(trangThai);

            // Nút giao hàng: nếu đã giao thì disabled, chưa giao thì enabled
            if (trangThai == "DA_GIAO")
            {
                btnGiaoHang.IsEnabled = false;
                btnGiaoHang.Opacity = 0.6;
            }
            else
            {
                btnGiaoHang.IsEnabled = true;
                btnGiaoHang.Opacity = 1.0;
            }

            // Nút sửa và hủy: chỉ khả dụng khi còn ở trạng thái tiếp nhận DA_NHAN
            bool choPhepSuaHuy = trangThai == "DA_NHAN";
            btnSuaDonTab2.IsEnabled = choPhepSuaHuy;
            btnHuyDonTab2.IsEnabled = choPhepSuaHuy;
            btnSuaDonTab2.Opacity = choPhepSuaHuy ? 1.0 : 0.5;
            btnHuyDonTab2.Opacity = choPhepSuaHuy ? 1.0 : 0.5;

            for (int i = 0; i < cboCapNhatTrangThai.Items.Count; i++)
            {
                if (cboCapNhatTrangThai.Items[i] is ComboBoxItem item && item.Tag?.ToString() == trangThai)
                {
                    cboCapNhatTrangThai.SelectedIndex = i;
                    break;
                }
            }
        }

        private void CapNhatHienThiTienTrinh(string trangThai)
        {
            if (bdStep1Circle == null) return;

            var steps = new (Border Circle, SymbolIcon Icon, TextBlock Txt, SymbolRegular DefaultIcon)[]
            {
                (bdStep1Circle, iconStep1, txtStep1, SymbolRegular.Box24),
                (bdStep2Circle, iconStep2, txtStep2, SymbolRegular.Cube24),
                (bdStep3Circle, iconStep3, txtStep3, SymbolRegular.VehicleSubway24),
                (bdStep4Circle, iconStep4, txtStep4, SymbolRegular.Location24),
                (bdStep5Circle, iconStep5, txtStep5, SymbolRegular.CheckmarkCircle24)
            };

            var lines = new[] { lineStep12, lineStep23, lineStep34, lineStep45 };

            int level = trangThai switch
            {
                "DA_NHAN" => 1,
                "DA_XEP" => 2,
                "DANG_VAN_CHUYEN" => 3,
                "DA_DEN" => 4,
                "DA_GIAO" => 5,
                _ => 0
            };

            txtCurrentStepName.Text = level switch
            {
                1 => "BƯỚC 1/5: TIẾP NHẬN TẠI GA GỬI",
                2 => "BƯỚC 2/5: ĐÃ XẾP LÊN TOA HÀNG",
                3 => "BƯỚC 3/5: ĐANG CHẠY TRÊN RAY",
                4 => "BƯỚC 4/5: ĐÃ VỀ GA ĐÍCH - CHỜ GIAO",
                5 => "BƯỚC 5/5: HOÀN TẤT GIAO NHẬN",
                _ => "---"
            };

            txtCurrentStepName.Foreground = level switch
            {
                3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7")),
                4 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706")),
                5 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"))
            };

            var brushCompleted = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
            var brushGreen = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
            var brushAmber = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
            var brushSky = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            var brushPendingBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
            var brushPendingFg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
            var brushLinePending = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));

            for (int i = 0; i < steps.Length; i++)
            {
                int stepNum = i + 1;
                var (circle, icon, txt, defIcon) = steps[i];

                if (stepNum < level)
                {
                    circle.Background = level == 5 ? brushGreen : brushCompleted;
                    icon.Symbol = SymbolRegular.Checkmark24;
                    icon.Foreground = Brushes.White;
                    txt.Foreground = level == 5 ? brushGreen : brushCompleted;
                    txt.FontWeight = FontWeights.SemiBold;
                }
                else if (stepNum == level)
                {
                    SolidColorBrush activeBrush = level switch
                    {
                        3 => brushSky,
                        4 => brushAmber,
                        5 => brushGreen,
                        _ => brushCompleted
                    };
                    circle.Background = activeBrush;
                    icon.Symbol = defIcon;
                    icon.Foreground = Brushes.White;
                    txt.Foreground = activeBrush;
                    txt.FontWeight = FontWeights.Bold;
                }
                else
                {
                    circle.Background = brushPendingBg;
                    icon.Symbol = defIcon;
                    icon.Foreground = brushPendingFg;
                    txt.Foreground = brushPendingFg;
                    txt.FontWeight = FontWeights.Normal;
                }
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null) continue;
                if (i + 1 < level)
                {
                    lines[i].Background = level == 5 ? brushGreen : brushCompleted;
                }
                else if (i + 1 == level && level == 4)
                {
                    lines[i].Background = brushAmber;
                }
                else
                {
                    lines[i].Background = brushLinePending;
                }
            }
        }

        private void BtnCapNhatTrangThai_Click(object sender, RoutedEventArgs e)
        {
            if (dgVanDon.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Vui lòng chọn vận đơn cần cập nhật tiến độ.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int maVD = Convert.ToInt32(row["MaVanDon"]);
            string currentTT = row["TrangThai"]?.ToString() ?? "";
            string newTT = (cboCapNhatTrangThai.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? currentTT;
            string ghiChu = txtGhiChuGiaoNhan.Text.Trim();

            if (newTT == currentTT && string.IsNullOrWhiteSpace(ghiChu))
            {
                MessageBox.Show("Trạng thái không có sự thay đổi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_service.CapNhatTrangThai(maVD, newTT, string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu))
            {
                MessageBox.Show($"Cập nhật trạng thái vận đơn #{row["MaVanDonCode"]} thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                txtGhiChuGiaoNhan.Text = "";
                NapLaiDanhSachVanDon();
            }
            else
            {
                MessageBox.Show("Cập nhật trạng thái thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGiaoHang_Click(object sender, RoutedEventArgs e)
        {
            if (dgVanDon.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Vui lòng chọn vận đơn cần bàn giao cho khách.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int maVanDon = Convert.ToInt32(row["MaVanDon"]);
            string trangThai = row["TrangThai"]?.ToString() ?? "";
            string maVDCode = row["MaVanDonCode"]?.ToString() ?? "";

            if (trangThai == "DA_GIAO")
            {
                MessageBox.Show($"Vận đơn #{maVDCode} đã được giao cho khách trước đó.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Xác nhận giao hàng cho khách nhận: {row["TenNguoiNhan"]} (SĐT: {row["SDTNguoiNhan"]})?\n" +
                $"Vận đơn: #{maVDCode}\n" +
                $"Hàng hóa: {row["TenHangHoa"]}",
                "Xác nhận bàn giao hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                string ghiChu = $"[GIAO HÀNG - {DateTime.Now:dd/MM/yyyy HH:mm}]: Đã bàn giao cho {row["TenNguoiNhan"]}.";
                if (_service.CapNhatTrangThai(maVanDon, "DA_GIAO", ghiChu))
                {
                    MessageBox.Show($"Đã xác nhận bàn giao hàng thành công cho #{maVDCode}.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    NapLaiDanhSachVanDon();
                }
                else
                {
                    MessageBox.Show("Giao hàng thất bại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnInPhieu_Click(object sender, RoutedEventArgs e)
        {
            DataRowView? row = null;
            if (tabMainHangHoa.SelectedItem == tabLapDon)
                row = dgDonMoiLap.SelectedItem as DataRowView;
            else
                row = dgVanDon.SelectedItem as DataRowView;

            if (row == null)
            {
                MessageBox.Show("Vui lòng chọn vận đơn trong danh sách để in phiếu.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string code = row["MaVanDonCode"]?.ToString() ?? "";
            decimal cuoc = row["CuocPhi"] != DBNull.Value ? Convert.ToDecimal(row["CuocPhi"]) : 0;
            string bienKS = row["BienKiemSoat"] != DBNull.Value ? row["BienKiemSoat"].ToString()! : "";
            string cont = row["SoHieuContainer"] != DBNull.Value ? row["SoHieuContainer"].ToString()! : "";
            string dacThu = !string.IsNullOrEmpty(bienKS) ? $"[BKS: {bienKS}]" : (!string.IsNullOrEmpty(cont) ? $"[Cont: {cont}]" : "");

            try
            {
                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    var doc = TaoPhieuGuiHangFlowDoc(row, code, cuoc, dacThu);
                    var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                    printDlg.PrintDocument(paginator, $"PhieuGuiHang_{code}");
                    MessageBox.Show($"Đã gửi lệnh in phiếu vận đơn #{code} đến máy in thành công.", "In hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            catch
            {
                // Bỏ qua lỗi spooler nếu máy trạm chưa kết nối dịch vụ in ấn
            }

            // Hiển thị xem trước phiếu gửi hàng nếu hủy in hoặc không có máy in
            MessageBox.Show(
                $"TỔNG CÔNG TY ĐƯỜNG SẮT VIỆT NAM (VNR)\n" +
                $"PHIẾU GỬI HÀNG HÓA & BIÊN NHẬN VẬN TẢI\n" +
                $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n\n" +
                $"Mã Vận Đơn:  {code}\n" +
                $"Hành trình:   {row["TenGaGui"]} → {row["TenGaNhan"]}\n" +
                $"Người gửi:    {row["TenNguoiGui"]} ({row["SDTNguoiGui"]})\n" +
                $"Người nhận:   {row["TenNguoiNhan"]} ({row["SDTNguoiNhan"]})\n" +
                $"Hàng hóa:     {row["TenHangHoa"]} {dacThu}\n" +
                $"Trọng lượng:  {Convert.ToDecimal(row["TrongLuongTan"]):N2} tấn\n" +
                $"Tổng cước:    {FormatHelper.FormatCurrency(cuoc)} VNĐ\n" +
                $"Trạng thái:   {row["TrangThai"]}",
                "Xem Trước Phiếu Gửi Hàng VNR", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Tạo tài liệu in ấn chuẩn mực cho vận đơn đường sắt
        private FlowDocument TaoPhieuGuiHangFlowDoc(DataRowView row, string code, decimal cuoc, string dacThu)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(40),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };

            var pHeader = new Paragraph(new Run("TỔNG CÔNG TY ĐƯỜNG SẮT VIỆT NAM (VNR)"))
            {
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };
            doc.Blocks.Add(pHeader);

            var pTitle = new Paragraph(new Run("PHIẾU GỬI HÀNG HÓA VÀ BIÊN NHẬN VẬN TẢI"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73")),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 16)
            };
            doc.Blocks.Add(pTitle);

            var pContent = new Paragraph();
            pContent.Inlines.Add(new Bold(new Run("Mã Vận Đơn: ")));
            pContent.Inlines.Add(new Run($"{code}\n"));
            pContent.Inlines.Add(new Bold(new Run("Hành trình: ")));
            pContent.Inlines.Add(new Run($"{row["TenGaGui"]} → {row["TenGaNhan"]}\n"));
            pContent.Inlines.Add(new Bold(new Run("Người gửi: ")));
            pContent.Inlines.Add(new Run($"{row["TenNguoiGui"]} - SĐT: {row["SDTNguoiGui"]}\n"));
            pContent.Inlines.Add(new Bold(new Run("Người nhận: ")));
            pContent.Inlines.Add(new Run($"{row["TenNguoiNhan"]} - SĐT: {row["SDTNguoiNhan"]}\n"));
            pContent.Inlines.Add(new Bold(new Run("Hàng hóa: ")));
            pContent.Inlines.Add(new Run($"{row["TenHangHoa"]} {dacThu}\n"));
            pContent.Inlines.Add(new Bold(new Run("Trọng lượng: ")));
            pContent.Inlines.Add(new Run($"{Convert.ToDecimal(row["TrongLuongTan"]):N2} tấn\n"));
            pContent.Inlines.Add(new Bold(new Run("Tổng cước phí: ")));
            pContent.Inlines.Add(new Run($"{FormatHelper.FormatCurrency(cuoc)} VNĐ\n"));
            pContent.Inlines.Add(new Bold(new Run("Trạng thái: ")));
            pContent.Inlines.Add(new Run($"{row["TrangThai"]}\n"));
            pContent.Inlines.Add(new Bold(new Run("Thời điểm lập: ")));
            pContent.Inlines.Add(new Run($"{DateTime.Now:dd/MM/yyyy HH:mm}\n"));
            doc.Blocks.Add(pContent);

            var pFooter = new Paragraph(new Run("\nNgười gửi hàng ký nhận                      Đại diện ga tiếp nhận ký\n(Ký và ghi rõ họ tên)                      (Ký và đóng dấu ga)"))
            {
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 30, 0, 0)
            };
            doc.Blocks.Add(pFooter);

            return doc;
        }

        private void BtnBaoCaoSuCo_Click(object sender, RoutedEventArgs e)
        {
            if (dgVanDon.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Vui lòng chọn vận đơn cần lập biên bản sự cố.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int maVD = Convert.ToInt32(row["MaVanDon"]);
            string maCode = row["MaVanDonCode"]?.ToString() ?? "";

            var confirm = MessageBox.Show(
                $"Lập biên bản ghi nhận sự cố phát sinh cho vận đơn #{maCode}?",
                "Xác nhận sự cố", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            if (confirm == MessageBoxResult.Yes)
            {
                string ghiChu = $"[SỰ CỐ - {DateTime.Now:dd/MM/yyyy HH:mm}]: Phát sinh sự cố vận chuyển tại ga. Chờ kiểm định xử lý.";
                _service.CapNhatTrangThai(maVD, row["TrangThai"]?.ToString() ?? "DA_DEN", ghiChu);
                NapLaiDanhSachVanDon();
                MessageBox.Show("Đã ghi nhận biên bản sự cố vào hệ thống điều độ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnNapLai_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem.Text = "";
            txtTimNhanhCaTruc.Text = "";
            if (dpTuNgay != null) dpTuNgay.SelectedDate = null;
            if (dpDenNgay != null) dpDenNgay.SelectedDate = null;
            radLocTatCa.IsChecked = true;
            radKpiTatCa.IsChecked = true;
            radLocHinhThucAll.IsChecked = true;
            cboLocGa.SelectedIndex = 0;
            if (_cachedVanDon != null) _cachedVanDon.DefaultView.RowFilter = "";
            NapLaiDanhSachVanDon();
        }

        private void TabMainHangHoa_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void Page_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                BtnLuu_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F3)
            {
                BtnLapDonMoi_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F4)
            {
                if (tabMainHangHoa.SelectedItem == tabTraCuu)
                {
                    BtnGiaoHang_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F5)
            {
                BtnNapLai_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        // =========================================================================
        // 5. PUBLIC METHODS CHO MAINWINDOW (F-KEYS & SHORTCUT)
        // =========================================================================
        public void FocusTimKiem()
        {
            tabMainHangHoa.SelectedItem = tabTraCuu;
            txtTimKiem?.Focus();
            txtTimKiem?.SelectAll();
        }

        public void KichHoatThemMoi()
        {
            tabMainHangHoa.SelectedItem = tabLapDon;
            BtnLapDonMoi_Click(this, new RoutedEventArgs());
        }

        public void KichHoatNapLai() => BtnNapLai_Click(this, new RoutedEventArgs());
    }
}
