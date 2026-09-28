using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS.Services;
using ET.VanHanh;

namespace GUI.Views.Pages
{
    public partial class LichTrinhPage : Page
    {
        private readonly ChuyenTauService _chuyenTauService = new();
        private readonly GaService _gaService = new();

        private DataTable? _cachedChuyenTau;
        private DataTable? _cachedMacTau;
        private DataTable? _cachedGa;
        private bool _isAddingNew = false;
        private bool _isLoading = true;

        private int _selectedMaChuyenTau = 0;
        private int _selectedMaDiemDung = 0;

        public LichTrinhPage()
        {
            InitializeComponent();
            KhoiTaoDuLieu();
        }

        private void KhoiTaoDuLieu()
        {
            _isLoading = true;
            try
            {
                _cachedMacTau = _chuyenTauService.LayDanhSachMacTau();
                cboMacTau.ItemsSource = _cachedMacTau.DefaultView;

                var dtLocMacTau = _cachedMacTau.Copy();
                var rowAll = dtLocMacTau.NewRow();
                rowAll["MaMacTau"] = 0;
                rowAll["SoHieuMacTau"] = "Tất cả mác tàu";
                dtLocMacTau.Rows.InsertAt(rowAll, 0);
                cboLocMacTau.ItemsSource = dtLocMacTau.DefaultView;
                cboLocMacTau.SelectedIndex = 0;

                _cachedGa = _gaService.LayDanhSach();
                cboThemDungGa.ItemsSource = _cachedGa.DefaultView;
                if (_cachedGa.Rows.Count > 0)
                {
                    cboThemDungGa.SelectedIndex = 0;
                }

                dpNgayChuyen.SelectedDate = DateTime.Today;

                _isLoading = false;
                LoadData();
            }
            catch (Exception ex)
            {
                _isLoading = false;
                MessageBox.Show($"Lỗi khởi tạo dữ liệu: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            if (_isLoading || dgChuyenTau == null) return;

            try
            {
                DateTime? locNgay = dpLocNgay.SelectedDate;
                int? locMac = null;
                if (cboLocMacTau.SelectedValue != null && int.TryParse(cboLocMacTau.SelectedValue.ToString(), out int maMac) && maMac > 0)
                {
                    locMac = maMac;
                }

                string? locTrangThai = null;
                if (cboLocTrangThai.SelectedItem is ComboBoxItem item && item.Tag != null)
                {
                    locTrangThai = item.Tag.ToString();
                }

                _cachedChuyenTau = _chuyenTauService.LayDanhSach(locNgay, locMac, locTrangThai);
                dgChuyenTau.ItemsSource = _cachedChuyenTau.DefaultView;

                CapNhatThongKe(_cachedChuyenTau);

                if (dgChuyenTau.Items.Count > 0)
                {
                    // Chọn lại bản ghi cũ hoặc bản ghi đầu tiên
                    int foundIndex = -1;
                    if (_selectedMaChuyenTau > 0)
                    {
                        for (int i = 0; i < _cachedChuyenTau.Rows.Count; i++)
                        {
                            if (Convert.ToInt32(_cachedChuyenTau.Rows[i]["MaChuyenTau"]) == _selectedMaChuyenTau)
                            {
                                foundIndex = i;
                                break;
                            }
                        }
                    }

                    dgChuyenTau.SelectedIndex = foundIndex >= 0 ? foundIndex : 0;
                }
                else
                {
                    XoaThongTinChiTiet();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp danh sách lịch trình: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CapNhatThongKe(DataTable dt)
        {
            int tong = dt.Rows.Count;
            int dangChay = 0;
            int treGio = 0;
            int hoanThanh = 0;

            foreach (DataRow r in dt.Rows)
            {
                string tt = r["TrangThai"]?.ToString() ?? "";
                if (tt == "DANG_CHAY") dangChay++;
                else if (tt == "TRE_GIO") treGio++;
                else if (tt == "HOAN_THANH") hoanThanh++;
            }

            txtTongSo.Text = $"Tổng số: {tong} chuyến";
            txtThongKeChay.Text = $"Đang chạy: {dangChay}";
            txtThongKeTre.Text = $"Chậm giờ: {treGio}";
            txtThongKeHoanThanh.Text = $"Hoàn thành: {hoanThanh}";
        }

        private void DgChuyenTau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || _isAddingNew) return;

            if (dgChuyenTau.SelectedItem is DataRowView row)
            {
                _selectedMaChuyenTau = Convert.ToInt32(row["MaChuyenTau"]);

                string soHieu = row["SoHieuMacTau"]?.ToString() ?? "";
                string trangThai = row["TrangThai"]?.ToString() ?? "";
                DateTime ngayXuatPhat = row["NgayXuatPhat"] != DBNull.Value ? Convert.ToDateTime(row["NgayXuatPhat"]) : DateTime.Today;

                txtInspectorTitle.Text = $"ĐIỀU HÀNH CHUYẾN TÀU: #{_selectedMaChuyenTau} ({soHieu})";
                txtInspectorSubtitle.Text = $"Hành trình: {row["TenGaDi"]} → {row["TenGaDen"]} | Khởi hành: {ngayXuatPhat:dd/MM/yyyy}";
                txtInspectorBadge.Text = $"ID: #{_selectedMaChuyenTau} | {trangThai}";

                // Tab 1: Form Chuyến Tàu
                if (row["MaMacTau"] != DBNull.Value)
                {
                    cboMacTau.SelectedValue = Convert.ToInt32(row["MaMacTau"]);
                }

                txtLoaiTau.Text = row["LoaiTau"]?.ToString() ?? "";
                txtHuongChay.Text = row["HuongChay"]?.ToString() ?? "";
                txtGaDi.Text = row["TenGaDi"]?.ToString() ?? "";
                txtGaDen.Text = row["TenGaDen"]?.ToString() ?? "";

                dpNgayChuyen.SelectedDate = ngayXuatPhat;

                if (row["GioXuatPhatKH"] != DBNull.Value)
                    txtGioDiKH.Text = Convert.ToDateTime(row["GioXuatPhatKH"]).ToString("HH:mm");

                if (row["GioVeDichKH"] != DBNull.Value)
                    txtGioDenKH.Text = Convert.ToDateTime(row["GioVeDichKH"]).ToString("HH:mm");

                txtSoPhutTre.Text = row["SoPhutTreLuyKe"]?.ToString() ?? "0";

                if (row["GioXuatPhatTT"] != DBNull.Value)
                    txtGioDiTT.Text = Convert.ToDateTime(row["GioXuatPhatTT"]).ToString("HH:mm");
                else
                    txtGioDiTT.Text = string.Empty;

                if (row["GioVeDichTT"] != DBNull.Value)
                    txtGioDenTT.Text = Convert.ToDateTime(row["GioVeDichTT"]).ToString("HH:mm");
                else
                    txtGioDenTT.Text = string.Empty;

                txtLyDoHuy.Text = row["LyDoHuy"]?.ToString() ?? string.Empty;

                // Chọn ComboBox Trạng Thái
                for (int i = 0; i < cboTrangThai.Items.Count; i++)
                {
                    if (cboTrangThai.Items[i] is ComboBoxItem item && item.Tag?.ToString() == trangThai)
                    {
                        cboTrangThai.SelectedIndex = i;
                        break;
                    }
                }

                btnXoaChuyen.IsEnabled = true;
                txtBtnLuuChuyen.Text = "Lưu Thay Đổi";

                // Kiểm tra loại tàu hàng để hiển thị thông báo
                string loaiTau = row["LoaiTau"]?.ToString() ?? "";
                bdTauHangNotice.Visibility = (loaiTau == "TAU_HANG") ? Visibility.Visible : Visibility.Collapsed;

                // Tab 2 & 3: Tải dữ liệu con
                LoadLichDungGa(_selectedMaChuyenTau);
                LoadToaXe(_selectedMaChuyenTau);

                // Tải thông tin đoàn tàu & đầu máy kéo
                DateTime dtDiVal = row["GioXuatPhatKH"] != DBNull.Value ? Convert.ToDateTime(row["GioXuatPhatKH"]) : DateTime.Today.AddHours(6);
                DateTime dtDenVal = row["GioVeDichKH"] != DBNull.Value ? Convert.ToDateTime(row["GioVeDichKH"]) : DateTime.Today.AddDays(1).AddHours(14);
                LoadDoanTau(_selectedMaChuyenTau, dtDiVal, dtDenVal);
            }
        }

        private void CboMacTau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboMacTau.SelectedItem is DataRowView row)
            {
                string mac = row["SoHieuMacTau"]?.ToString() ?? "";
                txtLoaiTau.Text = row["LoaiTau"]?.ToString() ?? "";
                txtHuongChay.Text = row["HuongChay"]?.ToString() ?? "";
                txtGaDi.Text = $"{row["TenGaDi"]} ({row["MaGaDiCode"]})";
                txtGaDen.Text = $"{row["TenGaDen"]} ({row["MaGaDenCode"]})";

                bdTauHangNotice.Visibility = (txtLoaiTau.Text == "TAU_HANG") ? Visibility.Visible : Visibility.Collapsed;

                // Tự động gán giờ xuất phát chuẩn và tính toán giờ đến dự kiến nếu đang lập chuyến mới
                if (_isAddingNew)
                {
                    string gioDiChuan = mac switch
                    {
                        "SE1" => "06:00",
                        "SE2" => "19:00",
                        "SE3" => "19:00",
                        "SE4" => "19:00",
                        "SE19" => "19:50",
                        "SE20" => "18:05",
                        "NA1" => "22:15",
                        "HBN1" => "04:00",
                        _ => "06:00"
                    };
                    txtGioDiKH.Text = gioDiChuan;

                    if (decimal.TryParse(row["ThoiGianChuanGio"]?.ToString(), out decimal gioChuan))
                    {
                        if (TimeSpan.TryParse(gioDiChuan, out TimeSpan tDi))
                        {
                            DateTime ngayDi = (dpNgayChuyen.SelectedDate ?? DateTime.Today).Date;
                            DateTime dtDi = ngayDi.Add(tDi);
                            DateTime dtDen = dtDi.AddHours((double)gioChuan);
                            txtGioDenKH.Text = dtDen.ToString("HH:mm");
                            LoadDanhSachDauMay(dtDi, dtDen, 0);
                        }
                    }
                }
            }
        }

        private void LoadLichDungGa(int maChuyenTau)
        {
            try
            {
                var dt = _chuyenTauService.LayLichDungGa(maChuyenTau);
                dgLichDungGa.ItemsSource = dt.DefaultView;
                txtTabBadgeDungGa.Text = dt.Rows.Count.ToString();

                if (dt.Rows.Count > 0)
                {
                    txtDungGaSubtitle.Text = $"Lộ trình gồm {dt.Rows.Count} ga dừng đón trả khách và tránh tàu";
                    dgLichDungGa.SelectedIndex = 0;
                }
                else
                {
                    txtDungGaSubtitle.Text = "Hành trình chưa có điểm dừng (nhấn Lấy Mẫu Tuyến hoặc Thêm Ga Dừng)";
                    txtNhanhGaChon.Text = "Chưa chọn điểm dừng";
                    txtNhanhGioDenTT.Text = string.Empty;
                    txtNhanhGioDiTT.Text = string.Empty;
                    _selectedMaDiemDung = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch dừng ga: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadToaXe(int maChuyenTau)
        {
            try
            {
                var dt = _chuyenTauService.LayDanhSachToaXe(maChuyenTau);
                dgToaXe.ItemsSource = dt.DefaultView;
                txtTabBadgeToaXe.Text = dt.Rows.Count.ToString();

                int tongSucChua = 0;
                foreach (DataRow r in dt.Rows)
                {
                    if (r["SucChua"] != DBNull.Value)
                    {
                        tongSucChua += Convert.ToInt32(r["SucChua"]);
                    }
                }

                txtTongSoToa.Text = $"{dt.Rows.Count} toa xe";
                txtTongSucChua.Text = $"{tongSucChua:N0} chỗ ngồi / giường nằm";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải biên chế toa xe: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void XoaThongTinChiTiet()
        {
            _selectedMaChuyenTau = 0;
            _selectedMaDiemDung = 0;

            txtInspectorTitle.Text = "CHI TIẾT ĐIỀU HÀNH CHUYẾN TÀU";
            txtInspectorSubtitle.Text = "Không có chuyến tàu nào phù hợp với bộ lọc";
            txtInspectorBadge.Text = "ID: #0";

            txtLoaiTau.Text = string.Empty;
            txtHuongChay.Text = string.Empty;
            txtGaDi.Text = string.Empty;
            txtGaDen.Text = string.Empty;
            txtSoPhutTre.Text = "0";
            txtGioDiTT.Text = string.Empty;
            txtGioDenTT.Text = string.Empty;
            txtLyDoHuy.Text = string.Empty;

            cboDauMayChinh.ItemsSource = null;
            cboDauMayDay.ItemsSource = null;
            txtTongChieuDaiM.Text = "0.0";
            txtTongTrongLuongTan.Text = "0.0";
            chkDaDuyetAnToan.IsChecked = false;
            txtCanhBaoChieuDaiGa.Text = "Không có dữ liệu hành trình";
            bdTauHangNotice.Visibility = Visibility.Collapsed;
            grdFormThemToa.Visibility = Visibility.Collapsed;

            btnXoaChuyen.IsEnabled = false;

            dgLichDungGa.ItemsSource = null;
            txtTabBadgeDungGa.Text = "0";
            txtDungGaSubtitle.Text = "Không có dữ liệu";

            dgToaXe.ItemsSource = null;
            txtTabBadgeToaXe.Text = "0";
            txtTongSoToa.Text = "0 toa xe";
            txtTongSucChua.Text = "0 chỗ";
        }

        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = true;
            dgChuyenTau.SelectedItem = null;

            txtInspectorTitle.Text = "LẬP LỊCH TRÌNH CHUYẾN TÀU MỚI";
            txtInspectorSubtitle.Text = "Nhập thông tin kế hoạch và chọn mác tàu vận hành";
            txtInspectorBadge.Text = "MỚI";

            dpNgayChuyen.SelectedDate = DateTime.Today.AddDays(1);
            txtGioDiKH.Text = "06:00";
            txtGioDenKH.Text = "18:30";
            txtSoPhutTre.Text = "0";
            txtGioDiTT.Text = string.Empty;
            txtGioDenTT.Text = string.Empty;
            txtLyDoHuy.Text = string.Empty;
            cboTrangThai.SelectedIndex = 0;

            txtTongChieuDaiM.Text = "216.5";
            txtTongTrongLuongTan.Text = "478.0";
            chkDaDuyetAnToan.IsChecked = false;
            txtCanhBaoChieuDaiGa.Text = "Hành trình mới: Chiều dài đoàn tàu nằm trong giới hạn an toàn quy chuẩn.";
            bdTauHangNotice.Visibility = Visibility.Collapsed;
            grdFormThemToa.Visibility = Visibility.Collapsed;

            DateTime dtDi = DateTime.Today.AddDays(1).AddHours(6);
            DateTime dtDen = dtDi.AddHours(32.5);
            LoadDanhSachDauMay(dtDi, dtDen, 0);

            if (cboMacTau.Items.Count > 0)
                cboMacTau.SelectedIndex = 0;

            btnXoaChuyen.IsEnabled = false;
            txtBtnLuuChuyen.Text = "Lập Chuyến Tàu";

            dgLichDungGa.ItemsSource = null;
            txtTabBadgeDungGa.Text = "0";
            txtDungGaSubtitle.Text = "Sẽ cấu hình điểm dừng sau khi lưu chuyến";

            dgToaXe.ItemsSource = null;
            txtTabBadgeToaXe.Text = "0";
            txtTongSoToa.Text = "0 toa xe";
            txtTongSucChua.Text = "0 chỗ";

            tabInspector.SelectedItem = tabThongTin;
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (cboMacTau.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn mác tàu mẫu vận hành.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpNgayChuyen.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày xuất phát của chuyến tàu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(txtGioDiKH.Text.Trim(), out TimeSpan tDi) ||
                !TimeSpan.TryParse(txtGioDenKH.Text.Trim(), out TimeSpan tDen))
            {
                MessageBox.Show("Giờ xuất phát và giờ đến kế hoạch phải đúng định dạng HH:mm (ví dụ: 06:00, 22:30).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int maMac = Convert.ToInt32(cboMacTau.SelectedValue);
            DateTime ngayDi = dpNgayChuyen.SelectedDate.Value.Date;
            DateTime dtDi = ngayDi.Add(tDi);
            DateTime dtDen = ngayDi.Add(tDen);

            // Nếu giờ đến nhỏ hơn hoặc bằng giờ đi thì chuyến tàu chạy qua ngày hôm sau
            if (dtDen <= dtDi)
            {
                dtDen = dtDen.AddDays(1);
            }

            string trangThai = "DA_LEN_LICH";
            if (cboTrangThai.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                trangThai = item.Tag.ToString()!;
            }

            int.TryParse(txtSoPhutTre.Text.Trim(), out int soPhutTre);

            DateTime? dtDiTT = null;
            if (TimeSpan.TryParse(txtGioDiTT.Text.Trim(), out TimeSpan tDiTT))
            {
                dtDiTT = ngayDi.Add(tDiTT);
            }

            DateTime? dtDenTT = null;
            if (TimeSpan.TryParse(txtGioDenTT.Text.Trim(), out TimeSpan tDenTT))
            {
                dtDenTT = ngayDi.Add(tDenTT);
                if (dtDiTT.HasValue && dtDenTT <= dtDiTT)
                    dtDenTT = dtDenTT.Value.AddDays(1);
            }

            // RÀNG BUỘC CHỐNG TRANH CHẤP ĐẦU MÁY (Time-Overlap Contention)
            if (cboDauMayChinh.SelectedValue != null && int.TryParse(cboDauMayChinh.SelectedValue.ToString(), out int maDauMay) && maDauMay > 0)
            {
                if (!_chuyenTauService.KiemTraXungDotDauMay(maDauMay, dtDi, dtDen, _isAddingNew ? 0 : _selectedMaChuyenTau, out string thongBaoLoi))
                {
                    MessageBox.Show(thongBaoLoi, "Xung Đột Phương Tiện", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var ct = new ChuyenTau
            {
                MaMacTau = maMac,
                NgayXuatPhat = ngayDi,
                GioXuatPhatKH = dtDi,
                GioVeDichKH = dtDen,
                GioXuatPhatTT = dtDiTT,
                GioVeDichTT = dtDenTT,
                SoPhutTreLuyKe = soPhutTre,
                LyDoHuy = string.IsNullOrWhiteSpace(txtLyDoHuy.Text) ? null : txtLyDoHuy.Text.Trim(),
                TrangThai = trangThai
            };

            if (_isAddingNew)
            {
                int newId = _chuyenTauService.Them(ct);
                if (newId > 0)
                {
                    _isAddingNew = false;
                    _selectedMaChuyenTau = newId;

                    // Lưu cấu hình đoàn tàu & đầu máy kéo
                    LuuThongTinDoanTau(newId);

                    // Đề xuất sao chép lịch dừng từ chuyến gần nhất cùng mác
                    var hoi = MessageBox.Show($"Đã lập lịch chuyến tàu #{newId} thành công.\n\nBạn có muốn tự động sao chép các điểm dừng từ chuyến mẫu gần nhất của mác tàu này không?",
                        "Sao chép lịch trình", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (hoi == MessageBoxResult.Yes)
                    {
                        _chuyenTauService.SaoChepLichDungTuChuyenMau(maMac, newId, ngayDi);
                    }

                    LoadData();
                }
                else
                {
                    MessageBox.Show("Không thể lập chuyến tàu. Có thể mác tàu này đã được lập lịch cho cùng ngày xuất phát.", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                ct.MaChuyenTau = _selectedMaChuyenTau;
                if (_chuyenTauService.CapNhat(ct))
                {
                    // Cập nhật cấu hình đoàn tàu & đầu máy kéo
                    LuuThongTinDoanTau(_selectedMaChuyenTau);

                    MessageBox.Show("Đã cập nhật thông tin chuyến tàu thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show("Không thể cập nhật chuyến tàu. Vui lòng kiểm tra lại dữ liệu.", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LuuThongTinDoanTau(int maChuyenTau)
        {
            try
            {
                int maChinh = (cboDauMayChinh.SelectedValue != null && int.TryParse(cboDauMayChinh.SelectedValue.ToString(), out int mc) && mc > 0) ? mc : 1;
                int? maDay = (cboDauMayDay.SelectedValue != null && int.TryParse(cboDauMayDay.SelectedValue.ToString(), out int md) && md > 0) ? md : null;
                decimal.TryParse(txtTongChieuDaiM.Text, out decimal chieuDai);
                if (chieuDai <= 0) chieuDai = 16.5m;
                decimal.TryParse(txtTongTrongLuongTan.Text, out decimal trongLuong);
                if (trongLuong <= 0) trongLuong = 78.0m;
                int soToa = dgToaXe.Items.Count > 0 ? dgToaXe.Items.Count : 1;

                var doanTau = new DoanTau
                {
                    MaChuyenTau = maChuyenTau,
                    MaDauMayChinh = maChinh,
                    MaDauMayDay = maDay,
                    TongSoToa = soToa,
                    TongChieuDaiM = chieuDai,
                    TongTrongLuongTan = trongLuong,
                    DaDuyetAnToan = chkDaDuyetAnToan.IsChecked == true
                };

                _chuyenTauService.LuuDoanTau(doanTau, out _);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi lưu đoàn tàu: {ex.Message}");
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = false;
            LoadData();
        }

        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0) return;

            var r = MessageBox.Show($"Xác nhận hủy chuyến tàu #{_selectedMaChuyenTau}?\n\nToàn bộ dữ liệu điểm dừng và biên chế toa xe liên quan cũng sẽ được xử lý.",
                "Xác nhận hủy chuyến", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r == MessageBoxResult.Yes)
            {
                if (_chuyenTauService.Xoa(_selectedMaChuyenTau, out string thongBaoLoi))
                {
                    MessageBox.Show("Đã xóa chuyến tàu thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    _selectedMaChuyenTau = 0;
                    LoadData();
                }
                else
                {
                    string msg = !string.IsNullOrWhiteSpace(thongBaoLoi) ? thongBaoLoi : "Không thể xóa chuyến tàu này (có thể đã phát sinh giao dịch vé).";
                    MessageBox.Show(msg, "Cảnh báo an toàn dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }


        private void BtnNapLai_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = false;
            LoadData();
        }

        private void DpLocNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => LoadData();

        private void CboLocMacTau_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadData();

        private void CboLocTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadData();

        private void BtnHienTatCa_Click(object sender, RoutedEventArgs e)
        {
            _isLoading = true;
            dpLocNgay.SelectedDate = null;
            cboLocMacTau.SelectedIndex = 0;
            cboLocTrangThai.SelectedIndex = 0;
            _isLoading = false;
            LoadData();
        }

        // =========================================================================
        // TAB 2: LỊCH TRÌNH DỪNG GA (SUB-ENTITY)
        // =========================================================================

        private void BtnMoFormThemDung_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0)
            {
                MessageBox.Show("Vui lòng chọn một chuyến tàu trước khi thêm điểm dừng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            pnlFormDiemDung.Visibility = Visibility.Visible;

            // Tính thứ tự dừng tiếp theo
            int thuTu = 1;
            if (dgLichDungGa.ItemsSource is DataView dv)
            {
                thuTu = dv.Count + 1;
            }
            txtThemThuTu.Text = thuTu.ToString();
        }

        private void BtnDongFormThemDung_Click(object sender, RoutedEventArgs e)
        {
            pnlFormDiemDung.Visibility = Visibility.Collapsed;
        }

        private void BtnLuuThemDiemDung_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0) return;

            if (cboThemDungGa.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn ga dừng.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtThemThuTu.Text.Trim(), out int thuTu) || thuTu <= 0)
            {
                MessageBox.Show("Thứ tự dừng phải là số nguyên dương (>= 1).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TimeSpan.TryParse(txtThemGioDen.Text.Trim(), out TimeSpan tDen) ||
                !TimeSpan.TryParse(txtThemGioDi.Text.Trim(), out TimeSpan tDi))
            {
                MessageBox.Show("Giờ đến và giờ đi phải đúng định dạng HH:mm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime ngayDi = dpNgayChuyen.SelectedDate ?? DateTime.Today;
            DateTime dtDen = ngayDi.Add(tDen);
            DateTime dtDi = ngayDi.Add(tDi);

            if (dtDi < dtDen)
            {
                MessageBox.Show("Giờ đi kế hoạch phải lớn hơn hoặc bằng giờ đến.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ld = new LichDungGa
            {
                MaChuyenTau = _selectedMaChuyenTau,
                MaGa = Convert.ToInt32(cboThemDungGa.SelectedValue),
                ThuTuDung = thuTu,
                GioDenKeHoach = dtDen,
                GioDiKeHoach = dtDi,
                LaDiemTranh = chkThemLaTranh.IsChecked == true
            };

            if (_chuyenTauService.ThemLichDungGa(ld))
            {
                pnlFormDiemDung.Visibility = Visibility.Collapsed;
                LoadLichDungGa(_selectedMaChuyenTau);
            }
            else
            {
                MessageBox.Show("Không thể thêm điểm dừng (trùng thứ tự hoặc vi phạm dữ liệu).", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgLichDungGa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgLichDungGa.SelectedItem is DataRowView row)
            {
                _selectedMaDiemDung = Convert.ToInt32(row["MaDiemDung"]);
                string tenGa = row["TenGa"]?.ToString() ?? "";
                string lyTrinh = row["LyTrinhKm"] != DBNull.Value ? $"{Convert.ToDecimal(row["LyTrinhKm"]):0.00} km" : "";
                int thuTu = Convert.ToInt32(row["ThuTuDung"]);

                txtNhanhGaChon.Text = $"Ga dừng #{thuTu}: {tenGa} (Km {lyTrinh})";

                if (row["GioDenThucTe"] != DBNull.Value)
                    txtNhanhGioDenTT.Text = Convert.ToDateTime(row["GioDenThucTe"]).ToString("HH:mm");
                else
                    txtNhanhGioDenTT.Text = string.Empty;

                if (row["GioDiThucTe"] != DBNull.Value)
                    txtNhanhGioDiTT.Text = Convert.ToDateTime(row["GioDiThucTe"]).ToString("HH:mm");
                else
                    txtNhanhGioDiTT.Text = string.Empty;
            }
        }

        private void BtnLuuGioThucTe_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaDiemDung <= 0)
            {
                MessageBox.Show("Vui lòng chọn một điểm dừng trong danh sách.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DateTime ngayDi = dpNgayChuyen.SelectedDate ?? DateTime.Today;
            DateTime? dtDenTT = null;
            DateTime? dtDiTT = null;

            if (!string.IsNullOrWhiteSpace(txtNhanhGioDenTT.Text))
            {
                if (TimeSpan.TryParse(txtNhanhGioDenTT.Text.Trim(), out TimeSpan tDen))
                    dtDenTT = ngayDi.Add(tDen);
                else
                {
                    MessageBox.Show("Giờ đến thực tế không đúng định dạng HH:mm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(txtNhanhGioDiTT.Text))
            {
                if (TimeSpan.TryParse(txtNhanhGioDiTT.Text.Trim(), out TimeSpan tDi))
                    dtDiTT = ngayDi.Add(tDi);
                else
                {
                    MessageBox.Show("Giờ đi thực tế không đúng định dạng HH:mm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (_chuyenTauService.CapNhatGioThucTe(_selectedMaDiemDung, dtDenTT, dtDiTT))
            {
                LoadLichDungGa(_selectedMaChuyenTau);
            }
            else
            {
                MessageBox.Show("Lỗi cập nhật giờ thực tế điểm dừng.", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnXoaDiemDung_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                int maDiemDung = Convert.ToInt32(row["MaDiemDung"]);
                string tenGa = row["TenGa"]?.ToString() ?? "";

                var r = MessageBox.Show($"Xác nhận bỏ ga '{tenGa}' khỏi lịch trình chuyến tàu này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    if (_chuyenTauService.XoaLichDungGa(maDiemDung))
                    {
                        LoadLichDungGa(_selectedMaChuyenTau);
                    }
                    else
                    {
                        MessageBox.Show("Không thể xóa điểm dừng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnSaoChepMau_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0 || cboMacTau.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn chuyến tàu hợp lệ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int maMac = Convert.ToInt32(cboMacTau.SelectedValue);
            DateTime ngayDi = dpNgayChuyen.SelectedDate ?? DateTime.Today;

            var r = MessageBox.Show("Hệ thống sẽ sao chép danh sách ga dừng và khoảng thời gian tiêu chuẩn từ chuyến gần nhất của mác tàu này.\n\nTiếp tục?",
                "Sao chép mẫu tuyến", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r == MessageBoxResult.Yes)
            {
                if (_chuyenTauService.SaoChepLichDungTuChuyenMau(maMac, _selectedMaChuyenTau, ngayDi))
                {
                    MessageBox.Show("Đã sao chép lịch dừng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadLichDungGa(_selectedMaChuyenTau);
                }
                else
                {
                    MessageBox.Show("Chưa tìm thấy chuyến tàu mẫu nào khác có dữ liệu điểm dừng của mác tàu này để sao chép.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void TabInspector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle tab switch if needed
        }

        // =========================================================================
        // PHÂN HỆ ĐOÀN TÀU & ĐẦU MÁY KÉO (Chống Tranh Chấp & Kiểm Tra Chiều Dài Ga)
        // =========================================================================

        private void LoadDanhSachDauMay(DateTime? gioDi, DateTime? gioDen, int? maChuyenTau)
        {
            try
            {
                var dt = _chuyenTauService.LayDanhSachDauMay(gioDi, gioDen, maChuyenTau);
                cboDauMayChinh.ItemsSource = dt.DefaultView;

                var dtDay = dt.Copy();
                var rNone = dtDay.NewRow();
                rNone["MaDauMay"] = 0;
                rNone["SoHieuDauMay"] = "-- Không dùng máy đẩy --";
                dtDay.Rows.InsertAt(rNone, 0);
                cboDauMayDay.ItemsSource = dtDay.DefaultView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải danh sách đầu máy: {ex.Message}");
            }
        }

        private void LoadDoanTau(int maChuyenTau, DateTime gioDi, DateTime gioDen)
        {
            try
            {
                LoadDanhSachDauMay(gioDi, gioDen, maChuyenTau);

                var dtDoanTau = _chuyenTauService.LayThongTinDoanTau(maChuyenTau);
                if (dtDoanTau.Rows.Count > 0)
                {
                    var r = dtDoanTau.Rows[0];
                    int maChinh = Convert.ToInt32(r["MaDauMayChinh"]);
                    int? maDay = r["MaDauMayDay"] != DBNull.Value ? Convert.ToInt32(r["MaDauMayDay"]) : 0;
                    decimal chieuDai = Convert.ToDecimal(r["TongChieuDaiM"]);
                    decimal trongLuong = Convert.ToDecimal(r["TongTrongLuongTan"]);
                    bool daDuyet = Convert.ToBoolean(r["DaDuyetAnToan"]);

                    cboDauMayChinh.SelectedValue = maChinh;
                    cboDauMayDay.SelectedValue = maDay.HasValue ? maDay.Value : 0;
                    txtTongChieuDaiM.Text = chieuDai.ToString("F1");
                    txtTongTrongLuongTan.Text = trongLuong.ToString("F1");
                    chkDaDuyetAnToan.IsChecked = daDuyet;

                    KiemTraChieuDaiVoiDuongTranhGa(maChuyenTau, chieuDai);
                }
                else
                {
                    if (cboDauMayChinh.Items.Count > 0)
                        cboDauMayChinh.SelectedIndex = 0;
                    cboDauMayDay.SelectedIndex = 0;

                    int soToa = dgToaXe.Items.Count > 0 ? dgToaXe.Items.Count : 3;
                    decimal chieuDai = 16.5m + (soToa * 20.0m);
                    decimal trongLuong = 78.0m + (soToa * 40.0m);
                    txtTongChieuDaiM.Text = chieuDai.ToString("F1");
                    txtTongTrongLuongTan.Text = trongLuong.ToString("F1");
                    chkDaDuyetAnToan.IsChecked = false;

                    KiemTraChieuDaiVoiDuongTranhGa(maChuyenTau, chieuDai);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi nạp thông tin đoàn tàu: {ex.Message}");
            }
        }

        private void KiemTraChieuDaiVoiDuongTranhGa(int maChuyenTau, decimal tongChieuDai)
        {
            if (maChuyenTau <= 0)
            {
                icoCanhBaoChieuDai.Symbol = Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
                icoCanhBaoChieuDai.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(21, 128, 61));
                txtCanhBaoChieuDaiGa.Text = "Hành trình mới: Chiều dài đoàn tàu nằm trong giới hạn an toàn quy chuẩn.";
                bdCanhBaoChieuDai.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
                return;
            }

            if (_chuyenTauService.KiemTraChieuDaiVoiGaDung(maChuyenTau, tongChieuDai, out string canhBao))
            {
                icoCanhBaoChieuDai.Symbol = Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
                icoCanhBaoChieuDai.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(21, 128, 61));
                txtCanhBaoChieuDaiGa.Text = $"Đoàn tàu ({tongChieuDai:F1}m) thỏa mãn chiều dài đường tránh tại tất cả các ga dừng trên hành trình.";
                bdCanhBaoChieuDai.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225));
            }
            else
            {
                icoCanhBaoChieuDai.Symbol = Wpf.Ui.Controls.SymbolRegular.Warning24;
                icoCanhBaoChieuDai.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 83, 9));
                txtCanhBaoChieuDaiGa.Text = canhBao;
                bdCanhBaoChieuDai.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(217, 119, 6));
            }
        }

        private void CboDauMayChinh_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboDauMayChinh.SelectedItem is DataRowView row)
            {
                bool dangTrung = row["DangTrungLich"] != DBNull.Value && Convert.ToBoolean(row["DangTrungLich"]);
                if (dangTrung)
                {
                    string thongTinBan = row["ChuyenTauDangBan"]?.ToString() ?? "một chuyến khác";
                    MessageBox.Show($"Cảnh báo: Đầu máy {row["SoHieuDauMay"]} đang bận chuyến {thongTinBan} có khung giờ trùng lặp!\n\nNếu lưu chuyến, hệ thống sẽ chặn để bảo toàn an toàn khai thác.",
                        "Cảnh báo xung đột đầu máy", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // =========================================================================
        // PHÂN HỆ BIÊN CHẾ TOA XE KHÁCH (Tab 3: Thêm, Xóa, Sao Chép)
        // =========================================================================

        private void BtnMoFormThemToa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0)
            {
                MessageBox.Show("Vui lòng chọn chuyến tàu trước khi thêm toa xe biên chế.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int nextThuTu = (dgToaXe.ItemsSource is DataView dv) ? dv.Count + 1 : 1;
            txtThemThuTuToa.Text = nextThuTu.ToString();
            cboThemLoaiToa.SelectedIndex = 0;
            txtThemSucChua.Text = "28";
            txtThemNhanHieuToa.Text = $"Toa {nextThuTu} (AN)";
            grdFormThemToa.Visibility = Visibility.Visible;
        }

        private void BtnDongFormThemToa_Click(object sender, RoutedEventArgs e)
        {
            grdFormThemToa.Visibility = Visibility.Collapsed;
        }

        private void CboThemLoaiToa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtThemSucChua == null || txtThemNhanHieuToa == null || cboThemLoaiToa == null) return;

            if (cboThemLoaiToa.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string loai = item.Tag.ToString()!;
                int thuTu = int.TryParse(txtThemThuTuToa?.Text, out int tt) ? tt : 1;
                switch (loai)
                {
                    case "AN":
                        txtThemSucChua.Text = "28";
                        txtThemNhanHieuToa.Text = $"Toa {thuTu} (AN)";
                        break;
                    case "BN":
                        txtThemSucChua.Text = "42";
                        txtThemNhanHieuToa.Text = $"Toa {thuTu} (BN)";
                        break;
                    case "NML":
                        txtThemSucChua.Text = "64";
                        txtThemNhanHieuToa.Text = $"Toa {thuTu} (NML)";
                        break;
                    case "NC":
                        txtThemSucChua.Text = "64";
                        txtThemNhanHieuToa.Text = $"Toa {thuTu} (NC)";
                        break;
                }
            }
        }

        private void BtnLuuToaXe_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0) return;

            if (!int.TryParse(txtThemThuTuToa.Text.Trim(), out int thuTu) || thuTu <= 0)
            {
                MessageBox.Show("Thứ tự toa phải là số nguyên dương.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtThemNhanHieuToa.Text))
            {
                MessageBox.Show("Vui lòng nhập ký hiệu toa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtThemSucChua.Text.Trim(), out int sucChua) || sucChua <= 0)
            {
                MessageBox.Show("Sức chứa chỗ ngồi phải là số nguyên dương.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string loaiToa = "AN";
            if (cboThemLoaiToa.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                loaiToa = item.Tag.ToString()!;
            }

            var tx = new ET.VanTai.ToaXeKhach
            {
                MaChuyenTau = _selectedMaChuyenTau,
                NhanHieuToa = txtThemNhanHieuToa.Text.Trim(),
                LoaiToa = loaiToa,
                ThuTuToa = thuTu,
                SucChua = sucChua
            };

            if (_chuyenTauService.ThemToaXeKhach(tx, chkTuDongSinhGhe.IsChecked == true, out string thongBaoLoi))
            {
                grdFormThemToa.Visibility = Visibility.Collapsed;
                LoadToaXe(_selectedMaChuyenTau);
                DateTime dtDi = dpNgayChuyen.SelectedDate ?? DateTime.Today;
                LoadDoanTau(_selectedMaChuyenTau, dtDi, dtDi.AddDays(1));
            }
            else
            {
                MessageBox.Show($"Không thể thêm toa xe: {thongBaoLoi}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnXoaToaXe_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                int maToa = Convert.ToInt32(row["MaToaXeKhach"]);
                string nhanHieu = row["NhanHieuToa"]?.ToString() ?? "";

                var r = MessageBox.Show($"Xác nhận xóa '{nhanHieu}' khỏi biên chế đoàn tàu?", "Xác nhận xóa toa xe", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    if (_chuyenTauService.XoaToaXeKhach(maToa, out string thongBaoLoi))
                    {
                        LoadToaXe(_selectedMaChuyenTau);
                        DateTime dtDi = dpNgayChuyen.SelectedDate ?? DateTime.Today;
                        LoadDoanTau(_selectedMaChuyenTau, dtDi, dtDi.AddDays(1));
                    }
                    else
                    {
                        MessageBox.Show(thongBaoLoi, "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void BtnSaoChepBienCheMau_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0 || cboMacTau.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn chuyến tàu hợp lệ.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int maMac = Convert.ToInt32(cboMacTau.SelectedValue);
            var r = MessageBox.Show("Hệ thống sẽ sao chép danh sách toa xe và tự động sinh chỗ ngồi từ chuyến mẫu gần nhất của mác tàu này.\n\nTiếp tục?",
                "Sao chép biên chế", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r == MessageBoxResult.Yes)
            {
                if (_chuyenTauService.SaoChepBienCheTuChuyenMau(maMac, _selectedMaChuyenTau, out string thongBao))
                {
                    MessageBox.Show(thongBao, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadToaXe(_selectedMaChuyenTau);
                    DateTime dtDi = dpNgayChuyen.SelectedDate ?? DateTime.Today;
                    LoadDoanTau(_selectedMaChuyenTau, dtDi, dtDi.AddDays(1));
                }
                else
                {
                    MessageBox.Show(thongBao, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // =========================================================================
        // CÁC PHƯƠNG THỨC KÍCH HOẠT TÁC VỤ THỰC TẾ TỪ MAINWINDOW (F1 - F12 & SHORTCUT STRIP)
        // =========================================================================
        public void FocusTimKiem()
        {
            if (cboLocMacTau != null)
            {
                cboLocMacTau.Focus();
                cboLocMacTau.IsDropDownOpen = true;
            }
        }

        public void KichHoatThemMoi() => BtnThem_Click(this, new RoutedEventArgs());
        public void KichHoatNapLai() => BtnNapLai_Click(this, new RoutedEventArgs());
    }
}

