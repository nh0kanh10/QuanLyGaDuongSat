using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BUS.Services;
using GUI.Helpers;
using GUI.Views.Dialogs;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.Pages
{
    public partial class SoatVePage : Page
    {
        private readonly VeService _veService = new();
        private readonly GaService _gaService = new();

        private DataTable? _cachedDsGa;
        private DataTable? _cachedManifest;
        private DataRow? _currentSoatVeRow;
        private int _currentSoatMaChuyenTau = 0;
        private int _currentSoatMaGa = 0;
        private bool _isLoaded = false;
        private int _pendingChuyenMaChuyenTau = 0;
        private DateTime? _pendingChuyenNgay = null;

        public SoatVePage()
        {
            InitializeComponent();
            _isLoaded = true;
            KhoiTaoDuLieuBanDau();
        }

        private void KhoiTaoDuLieuBanDau()
        {
            try
            {
                dpNgaySoatVe.Language = System.Windows.Markup.XmlLanguage.GetLanguage("vi-VN");
                dpNgaySoatVe.SelectedDate = DateTime.Today;
                dpNgaySoatVe.Loaded += (s, e) => ChuanHoaDatePicker(dpNgaySoatVe);
                dpNgaySoatVe.SelectedDateChanged += (s, e) => ChuanHoaDatePicker(dpNgaySoatVe);
                Dispatcher.BeginInvoke(new Action(() => ChuanHoaDatePicker(dpNgaySoatVe)), System.Windows.Threading.DispatcherPriority.Loaded);

                if (cboLocTrangThaiManifest != null && cboLocTrangThaiManifest.SelectedIndex < 0)
                {
                    cboLocTrangThaiManifest.SelectedIndex = 0;
                }

                // Tải danh mục ga kiểm soát
                _cachedDsGa = _gaService.LayDanhSach();
                cboGaKiemSoat.ItemsSource = _cachedDsGa.DefaultView;
                ChonGaMacDinhLinhHoat(cboGaKiemSoat, new[] { "Hà Nội", "HNO" }, 0);

                if (cboGaKiemSoat.SelectedValue is int mg)
                {
                    _currentSoatMaGa = mg;
                }

                // Tải danh sách chuyến tàu chạy trong ngày
                TaiDanhSachChuyenTauTheoNgay(DateTime.Today);
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi nạp dữ liệu màn hình kiểm soát soát vé: {ex.Message}", "Lỗi Khởi Tạo");
            }
        }

        private void ChonGaMacDinhLinhHoat(ComboBox cbo, string[] tuKhoaUuTien, int fallbackIndex)
        {
            if (_cachedDsGa == null || _cachedDsGa.Rows.Count == 0) return;

            foreach (DataRow row in _cachedDsGa.Rows)
            {
                string tenGa = row["TenGa"]?.ToString() ?? "";
                string maCode = row["MaGaCode"]?.ToString() ?? "";
                foreach (var tk in tuKhoaUuTien)
                {
                    if (tenGa.Contains(tk, StringComparison.OrdinalIgnoreCase) || maCode.Equals(tk, StringComparison.OrdinalIgnoreCase))
                    {
                        cbo.SelectedValue = Convert.ToInt32(row["MaGa"]);
                        return;
                    }
                }
            }

            if (fallbackIndex >= 0 && fallbackIndex < _cachedDsGa.Rows.Count)
            {
                cbo.SelectedValue = Convert.ToInt32(_cachedDsGa.Rows[fallbackIndex]["MaGa"]);
            }
        }

        private void TaiDanhSachChuyenTauTheoNgay(DateTime ngay)
        {
            DataTable dtChuyen = _veService.LayDanhSachChuyenTauHomNay(ngay);
            cboChuyenTauSoatVe.ItemsSource = dtChuyen.DefaultView;

            if (dtChuyen.Rows.Count > 0)
            {
                cboChuyenTauSoatVe.SelectedIndex = 0;
            }
            else
            {
                _currentSoatMaChuyenTau = 0;
                _cachedManifest = null;
                if (dgvManifest != null) dgvManifest.ItemsSource = null;
                ResetKpiSoatVe();
                HienThiChoQuetVe();
                if (lblManifestSubTitle != null) lblManifestSubTitle.Text = "Không có chuyến tàu nào chạy trong ngày đã chọn";
                if (lblManifestTongKet != null) lblManifestTongKet.Text = "Hiển thị 0 hành khách";
            }
        }

        private void DpNgaySoatVe_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || dpNgaySoatVe == null || cboChuyenTauSoatVe == null) return;
            DateTime ngay = dpNgaySoatVe.SelectedDate ?? DateTime.Today;
            TaiDanhSachChuyenTauTheoNgay(ngay);
        }

        private void CboChuyenTauSoatVe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || cboChuyenTauSoatVe == null) return;
            if (cboChuyenTauSoatVe.SelectedValue is int mct && mct > 0)
            {
                _currentSoatMaChuyenTau = mct;
                lblManifestSubTitle.Text = $"Chuyến tàu đang kiểm soát: {cboChuyenTauSoatVe.Text}";
                LoadManifestChuyenTau();
                HienThiChoQuetVe();
            }
        }

        private void CboGaKiemSoat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || cboGaKiemSoat == null) return;
            if (cboGaKiemSoat.SelectedValue is int mg && mg > 0)
            {
                _currentSoatMaGa = mg;
                CapNhatKpiSoatVe();
                ApDungLocManifest();
            }
        }

        private void BtnLamMoiSoatVe_Click(object sender, RoutedEventArgs e)
        {
            DateTime ngay = dpNgaySoatVe.SelectedDate ?? DateTime.Today;
            TaiDanhSachChuyenTauTheoNgay(ngay);
            LoadManifestChuyenTau();
            ThongBaoDialog.ThanhCong("Đã nạp lại dữ liệu kiểm soát vé của chuyến tàu!", "Làm Mới Dữ Liệu");
        }

        private void LoadManifestChuyenTau()
        {
            try
            {
                if (_currentSoatMaChuyenTau <= 0) return;

                _cachedManifest = _veService.LayManifestChuyenTau(_currentSoatMaChuyenTau);
                ApDungLocManifest();
                CapNhatKpiSoatVe();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi tải manifest chuyến tàu: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void CapNhatKpiSoatVe()
        {
            try
            {
                if (!_isLoaded || lblKpiTongVe == null) return;

                if (_currentSoatMaChuyenTau <= 0)
                {
                    ResetKpiSoatVe();
                    return;
                }

                DataTable dtTk = _veService.LayThongKeSoatVe(_currentSoatMaChuyenTau, _currentSoatMaGa);
                if (dtTk.Rows.Count > 0)
                {
                    DataRow r = dtTk.Rows[0];
                    int tongVe = r["TongVe"] != DBNull.Value ? Convert.ToInt32(r["TongVe"]) : 0;
                    int daLen = r["DaLenTau"] != DBNull.Value ? Convert.ToInt32(r["DaLenTau"]) : 0;
                    int chuaLen = r["ChuaLenTau"] != DBNull.Value ? Convert.ToInt32(r["ChuaLenTau"]) : 0;
                    int donTong = r["DonTaiGaTong"] != DBNull.Value ? Convert.ToInt32(r["DonTaiGaTong"]) : 0;
                    int donDaLen = r["DonTaiGaDaLen"] != DBNull.Value ? Convert.ToInt32(r["DonTaiGaDaLen"]) : 0;
                    int traTaiGa = r["TraTaiGa"] != DBNull.Value ? Convert.ToInt32(r["TraTaiGa"]) : 0;

                    if (lblKpiTongVe != null) lblKpiTongVe.Text = tongVe.ToString("N0");
                    if (lblKpiDaLenTau != null) lblKpiDaLenTau.Text = daLen.ToString("N0");
                    if (lblKpiChuaLenTau != null) lblKpiChuaLenTau.Text = chuaLen.ToString("N0");
                    if (lblKpiDonTaiGaDaLen != null) lblKpiDonTaiGaDaLen.Text = donDaLen.ToString("N0");
                    if (lblKpiDonTaiGaTong != null) lblKpiDonTaiGaTong.Text = donTong.ToString("N0");
                    if (lblKpiTraTaiGa != null) lblKpiTraTaiGa.Text = traTaiGa.ToString("N0");
                }
            }
            catch
            {
                ResetKpiSoatVe();
            }
        }

        private void ResetKpiSoatVe()
        {
            if (lblKpiTongVe != null) lblKpiTongVe.Text = "0";
            if (lblKpiDaLenTau != null) lblKpiDaLenTau.Text = "0";
            if (lblKpiChuaLenTau != null) lblKpiChuaLenTau.Text = "0";
            if (lblKpiDonTaiGaDaLen != null) lblKpiDonTaiGaDaLen.Text = "0";
            if (lblKpiDonTaiGaTong != null) lblKpiDonTaiGaTong.Text = "0";
            if (lblKpiTraTaiGa != null) lblKpiTraTaiGa.Text = "0";
        }

        private void TxtTimKiemManifest_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            ApDungLocManifest();
        }

        private void CboLocTrangThaiManifest_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            ApDungLocManifest();
        }

        private void ApDungLocManifest()
        {
            if (!_isLoaded || dgvManifest == null) return;

            if (_cachedManifest == null)
            {
                dgvManifest.ItemsSource = null;
                if (lblManifestTongKet != null)
                {
                    lblManifestTongKet.Text = "Hiển thị 0 hành khách";
                }
                return;
            }

            try
            {
                DataView dv = _cachedManifest.DefaultView;
                var filters = new List<string>();

                // Lọc theo từ khóa (Tên, CCCD, Mã vé, Số ghế)
                string kw = txtTimKiemManifest?.Text?.Trim().Replace("'", "''") ?? "";
                if (!string.IsNullOrWhiteSpace(kw))
                {
                    filters.Add($"(TenHanhKhach LIKE '%{kw}%' OR CCCDHanhKhach LIKE '%{kw}%' OR MaVeCode LIKE '%{kw}%' OR CONVERT(SoGhe, 'System.String') LIKE '%{kw}%')");
                }

                // Lọc theo combobox trạng thái
                if (cboLocTrangThaiManifest?.SelectedItem is ComboBoxItem item)
                {
                    string tag = item.Tag?.ToString() ?? "ALL";
                    switch (tag)
                    {
                        case "DON_GA_CHUA_LEN":
                            filters.Add($"MaGaDi = {_currentSoatMaGa} AND TrangThai = 'DA_DAT'");
                            break;
                        case "DON_GA_DA_LEN":
                            filters.Add($"MaGaDi = {_currentSoatMaGa} AND TrangThai = 'DA_LEN_TAU'");
                            break;
                        case "DA_LEN_TAU":
                            filters.Add("TrangThai = 'DA_LEN_TAU'");
                            break;
                        case "DA_DAT":
                            filters.Add("TrangThai = 'DA_DAT'");
                            break;
                        case "HUY_HOAN":
                            filters.Add("TrangThai IN ('DA_HUY', 'DA_HOAN_VE')");
                            break;
                    }
                }

                dv.RowFilter = filters.Count > 0 ? string.Join(" AND ", filters) : "";
                dgvManifest.ItemsSource = dv;
                if (lblManifestTongKet != null)
                {
                    lblManifestTongKet.Text = $"Hiển thị {dv.Count:N0} / {_cachedManifest.Rows.Count:N0} hành khách";
                }
            }
            catch
            {
                if (dgvManifest != null && _cachedManifest != null)
                {
                    dgvManifest.ItemsSource = _cachedManifest.DefaultView;
                }
            }
        }

        private void TxtScannerInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ThucHienKiemTraVaSoatVe();
            }
        }

        private void BtnKiemTraVaSoatVe_Click(object sender, RoutedEventArgs e)
        {
            ThucHienKiemTraVaSoatVe();
        }

        private void ThucHienKiemTraVaSoatVe()
        {
            string kw = txtScannerInput.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(kw))
            {
                ThongBaoDialog.CanhBao("Vui lòng quét mã QR/Barcode hoặc nhập mã vé / số CCCD!", "Chưa Nhập Mã");
                txtScannerInput.Focus();
                return;
            }

            try
            {
                // Tìm vé trong hệ thống
                DataTable dt = _veService.TimVeSoat(_currentSoatMaChuyenTau > 0 ? _currentSoatMaChuyenTau : null, kw);
                if (dt.Rows.Count == 0)
                {
                    // Thử tìm trên toàn bộ các chuyến nếu chuyến hiện tại không khớp
                    dt = _veService.TimVeSoat(null, kw);
                }

                if (dt.Rows.Count == 0)
                {
                    HienThiKetQuaLoi("KHÔNG TÌM THẤY VÉ", $"Không có vé nào khớp với từ khóa '{kw}' trong cơ sở dữ liệu!");
                    txtScannerInput.SelectAll();
                    txtScannerInput.Focus();
                    return;
                }

                DataRow row = dt.Rows[0];
                _currentSoatVeRow = row;
                HienThiChiTietVeSoat(row);

                // Đánh giá tính hợp lệ
                int maChuyenTauCuaVe = Convert.ToInt32(row["MaChuyenTau"]);
                string trangThaiVe = row["TrangThai"].ToString() ?? "";
                int maGaDiCuaVe = Convert.ToInt32(row["MaGaDi"]);
                string tenGaDi = row["TenGaDi"].ToString() ?? "";
                string soHieuMacTau = row["SoHieuMacTau"].ToString() ?? "";

                // Kiểm tra sai chuyến tàu
                if (_currentSoatMaChuyenTau > 0 && maChuyenTauCuaVe != _currentSoatMaChuyenTau)
                {
                    DateTime ngayXuatPhatCuaVe = row.Table.Columns.Contains("NgayXuatPhat") && row["NgayXuatPhat"] != DBNull.Value
                        ? Convert.ToDateTime(row["NgayXuatPhat"])
                        : DateTime.Today;
                    DateTime ngayDangChon = dpNgaySoatVe.SelectedDate ?? DateTime.Today;
                    string tenChuyenDangChon = cboChuyenTauSoatVe.Text;

                    if (ngayXuatPhatCuaVe.Date != ngayDangChon.Date)
                    {
                        HienThiKetQuaLoi("SAI NGÀY KHỞI HÀNH", 
                            $"Vé này thuộc tàu {soHieuMacTau} xuất phát ngày {ngayXuatPhatCuaVe:dd/MM/yyyy} (Chuyến #{maChuyenTauCuaVe}), không khớp với ngày bạn đang kiểm soát ({ngayDangChon:dd/MM/yyyy})!",
                            coTheChuyenChuyen: true,
                            maChuyenMoi: maChuyenTauCuaVe,
                            ngayChuyenMoi: ngayXuatPhatCuaVe.Date);
                    }
                    else
                    {
                        HienThiKetQuaLoi("SAI CHUYẾN TÀU", 
                            $"Vé này thuộc chuyến #{maChuyenTauCuaVe} (Tàu {soHieuMacTau}), không phải chuyến tàu đang kiểm soát ({tenChuyenDangChon})!",
                            coTheChuyenChuyen: true,
                            maChuyenMoi: maChuyenTauCuaVe,
                            ngayChuyenMoi: ngayXuatPhatCuaVe.Date);
                    }
                    txtScannerInput.SelectAll();
                    txtScannerInput.Focus();
                    return;
                }

                // Kiểm tra vé bị hủy/hoàn
                if (trangThaiVe == "DA_HUY")
                {
                    HienThiKetQuaLoi("VÉ ĐÃ BỊ HỦY", "Vé này đã bị hủy bỏ trên hệ thống, không được phép lên tàu!");
                    txtScannerInput.SelectAll();
                    txtScannerInput.Focus();
                    return;
                }

                if (trangThaiVe == "DA_HOAN_VE")
                {
                    HienThiKetQuaLoi("VÉ ĐÃ HOÀN TRẢ", "Vé này đã làm thủ tục hoàn trả tiền, không còn giá trị sử dụng!");
                    txtScannerInput.SelectAll();
                    txtScannerInput.Focus();
                    return;
                }

                // Kiểm tra vé đã lên tàu trước đó
                if (trangThaiVe == "DA_LEN_TAU")
                {
                    string thoiDiem = row["ThoiDiemSoatVe"] != DBNull.Value ? Convert.ToDateTime(row["ThoiDiemSoatVe"]).ToString("HH:mm:ss dd/MM/yyyy") : "--";
                    string gaSoat = row["TenGaSoat"]?.ToString() ?? "Cửa soát vé";
                    HienThiKetQuaCanhBao("CẢNH BÁO: VÉ ĐÃ QUA CỬA SOÁT TRƯỚC ĐÓ", 
                        $"Vé đã được soát vào lúc {thoiDiem} tại ga {gaSoat}. Vui lòng kiểm tra lại để tránh dùng vé trùng!");
                    txtScannerInput.SelectAll();
                    txtScannerInput.Focus();
                    return;
                }

                // Kiểm tra ga đi
                if (_currentSoatMaGa > 0 && maGaDiCuaVe != _currentSoatMaGa)
                {
                    HienThiKetQuaCanhBao("CHÚ Ý GA ĐÓN TÀU", 
                        $"Ga đi trên vé là [{tenGaDi}], ga kiểm soát hiện tại khác ga đi. Khách có thể đi tàu từ ga sau hoặc cần đối chiếu thông tin!");
                }
                else
                {
                    // HỢP LỆ!
                    HienThiKetQuaHopLe("HỢP LỆ - ĐƯỢC PHÉP LÊN TÀU", "Vé hoàn toàn hợp lệ và đúng lịch trình chuyến tàu.");
                }

                // Tự động cho lên tàu nếu bật toggle auto
                if (chkTuDongSoatVe.IsChecked == true && trangThaiVe == "DA_DAT")
                {
                    XacNhanLenTau(row, hienThiThongBao: false);
                }

                txtScannerInput.SelectAll();
                txtScannerInput.Focus();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi khi kiểm tra vé: {ex.Message}", "Lỗi");
            }
        }

        private void HienThiChoQuetVe()
        {
            if (pnlChoQuetVe != null) pnlChoQuetVe.Visibility = Visibility.Visible;
            if (pnlKetQuaChiTiet != null) pnlKetQuaChiTiet.Visibility = Visibility.Collapsed;
            if (btnChuyenSangChuyenTauNay != null) btnChuyenSangChuyenTauNay.Visibility = Visibility.Collapsed;
            _currentSoatVeRow = null;
        }

        private void HienThiChiTietVeSoat(DataRow row)
        {
            pnlChoQuetVe.Visibility = Visibility.Collapsed;
            pnlKetQuaChiTiet.Visibility = Visibility.Visible;

            lblSoatToaXe.Text = $"TOA {Convert.ToInt32(row["ThuTuNoiToa"]):D2}";
            lblSoatLoaiToa.Text = row["NhanHieuToa"]?.ToString() ?? "Toa xe khách";

            int soGhe = Convert.ToInt32(row["SoGhe"]);
            object tangGiuong = row["TangGiuong"];
            lblSoatSoGhe.Text = $"GHẾ {soGhe:D2}";
            lblSoatTangGiuong.Text = (tangGiuong != DBNull.Value && Convert.ToInt32(tangGiuong) > 0) ? $"Tầng {tangGiuong}" : "Ghế ngồi";

            lblSoatTenKhach.Text = row["TenHanhKhach"]?.ToString()?.ToUpper() ?? "";
            lblSoatCccdKhach.Text = row["CCCDHanhKhach"]?.ToString() ?? "";

            lblSoatGaDi.Text = row["TenGaDi"]?.ToString() ?? "";
            lblSoatGaDen.Text = row["TenGaDen"]?.ToString() ?? "";

            string macTau = row["SoHieuMacTau"]?.ToString() ?? "";
            DateTime ngayXP = row["NgayXuatPhat"] != DBNull.Value ? Convert.ToDateTime(row["NgayXuatPhat"]) : DateTime.Today;
            lblSoatMacTauNgay.Text = $"{macTau} - {ngayXP:dd/MM/yyyy}";

            lblSoatGioXuatPhat.Text = DateTimeFormatHelper.DinhDangGio(row, "GioDiKH", "GioXuatPhatKH", "--:--");

            lblSoatMaVeCode.Text = row["MaVeCode"]?.ToString() ?? "";
            decimal giaVe = row["GiaVeThucThu"] != DBNull.Value ? Convert.ToDecimal(row["GiaVeThucThu"]) : 0;
            lblSoatGiaVe.Text = giaVe.ToString("N0") + " VNĐ";

            string trangThai = row["TrangThai"]?.ToString() ?? "";
            lblSoatTrangThaiHienTai.Text = trangThai switch
            {
                "DA_LEN_TAU" => "Đã lên tàu",
                "DA_DAT" => "Chưa lên tàu",
                "DA_HOAN_VE" => "Đã hoàn vé",
                "DA_HUY" => "Đã hủy",
                _ => trangThai
            };

            lblSoatThoiDiem.Text = row["ThoiDiemSoatVe"] != DBNull.Value ? Convert.ToDateTime(row["ThoiDiemSoatVe"]).ToString("HH:mm:ss dd/MM/yyyy") : "--";
            lblSoatGaKiemSoat.Text = row["TenGaSoat"]?.ToString() ?? "--";

            // Trạng thái các nút
            btnXacNhanLenTau.IsEnabled = (trangThai == "DA_DAT");
            btnHuySoatVe.IsEnabled = (trangThai == "DA_LEN_TAU");
        }

        private void HienThiKetQuaHopLe(string tieuDe, string moTa)
        {
            bdBannerTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
            bdBannerTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
            iconBannerTrangThai.Symbol = SymbolRegular.CheckmarkCircle24;
            iconBannerTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
            lblBannerTieuDe.Text = tieuDe;
            lblBannerTieuDe.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
            lblBannerMoTa.Text = moTa;
            lblBannerMoTa.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534"));
            if (btnChuyenSangChuyenTauNay != null) btnChuyenSangChuyenTauNay.Visibility = Visibility.Collapsed;
        }

        private void HienThiKetQuaCanhBao(string tieuDe, string moTa)
        {
            bdBannerTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
            bdBannerTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
            iconBannerTrangThai.Symbol = SymbolRegular.Warning24;
            iconBannerTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
            lblBannerTieuDe.Text = tieuDe;
            lblBannerTieuDe.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
            lblBannerMoTa.Text = moTa;
            lblBannerMoTa.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E"));
            if (btnChuyenSangChuyenTauNay != null) btnChuyenSangChuyenTauNay.Visibility = Visibility.Collapsed;
        }

        private void HienThiKetQuaLoi(string tieuDe, string moTa, bool coTheChuyenChuyen = false, int maChuyenMoi = 0, DateTime? ngayChuyenMoi = null)
        {
            pnlChoQuetVe.Visibility = Visibility.Collapsed;
            pnlKetQuaChiTiet.Visibility = Visibility.Visible;

            bdBannerTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
            bdBannerTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            iconBannerTrangThai.Symbol = SymbolRegular.DismissCircle24;
            iconBannerTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            lblBannerTieuDe.Text = tieuDe;
            lblBannerTieuDe.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
            lblBannerMoTa.Text = moTa;
            lblBannerMoTa.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));

            btnXacNhanLenTau.IsEnabled = false;
            btnHuySoatVe.IsEnabled = false;

            if (btnChuyenSangChuyenTauNay != null)
            {
                if (coTheChuyenChuyen && maChuyenMoi > 0 && ngayChuyenMoi.HasValue)
                {
                    _pendingChuyenMaChuyenTau = maChuyenMoi;
                    _pendingChuyenNgay = ngayChuyenMoi;
                    btnChuyenSangChuyenTauNay.Content = $"Chuyển sang kiểm soát chuyến #{maChuyenMoi} (Ngày {ngayChuyenMoi:dd/MM/yyyy})";
                    btnChuyenSangChuyenTauNay.Visibility = Visibility.Visible;
                }
                else
                {
                    btnChuyenSangChuyenTauNay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnXacNhanLenTau_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSoatVeRow != null)
            {
                XacNhanLenTau(_currentSoatVeRow, hienThiThongBao: true);
            }
        }

        private void XacNhanLenTau(DataRow row, bool hienThiThongBao)
        {
            int maVe = Convert.ToInt32(row["MaVe"]);
            string tenKhach = row["TenHanhKhach"]?.ToString() ?? "";
            string soGhe = row["SoGhe"]?.ToString() ?? "";
            string toa = row["ThuTuNoiToa"]?.ToString() ?? "";

            if (_veService.SoatVe(maVe, _currentSoatMaGa > 0 ? _currentSoatMaGa : null, out string err))
            {
                row["TrangThai"] = "DA_LEN_TAU";
                row["ThoiDiemSoatVe"] = DateTime.Now;
                if (cboGaKiemSoat.SelectedItem is DataRowView drv)
                {
                    row["TenGaSoat"] = drv["TenGa"];
                }

                // Cập nhật lại trong manifest nếu có
                if (_cachedManifest != null)
                {
                    DataRow[] found = _cachedManifest.Select($"MaVe = {maVe}");
                    if (found.Length > 0)
                    {
                        found[0]["TrangThai"] = "DA_LEN_TAU";
                        found[0]["ThoiDiemSoatVe"] = DateTime.Now;
                        found[0]["TenGaSoat"] = row["TenGaSoat"];
                    }
                }

                HienThiChiTietVeSoat(row);
                HienThiKetQuaHopLe("ĐÃ QUA CỬA KIỂM SOÁT LÊN TÀU", $"Hành khách {tenKhach} (Toa {toa}, Ghế {soGhe}) đã được xác nhận lên tàu thành công.");
                CapNhatKpiSoatVe();
                ApDungLocManifest();

                if (hienThiThongBao)
                {
                    ThongBaoDialog.ThanhCong($"Hành khách {tenKhach} (Toa {toa}, Ghế {soGhe}) đã được xác nhận lên tàu thành công!", "Soát Vé Thành Công");
                }
            }
            else
            {
                ThongBaoDialog.Loi($"Không thể cập nhật soát vé: {err}", "Lỗi");
            }
        }

        private void BtnHuySoatVe_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSoatVeRow == null) return;
            int maVe = Convert.ToInt32(_currentSoatVeRow["MaVe"]);
            string tenKhach = _currentSoatVeRow["TenHanhKhach"]?.ToString() ?? "";

            if (!ThongBaoDialog.XacNhan($"Bạn có chắc chắn muốn hủy trạng thái lên tàu của hành khách {tenKhach}?\nVé sẽ quay về trạng thái 'Chưa lên tàu'.", "Xác Nhận Hủy Soát"))
            {
                return;
            }

            if (_veService.HuySoatVe(maVe, out string err))
            {
                _currentSoatVeRow["TrangThai"] = "DA_DAT";
                _currentSoatVeRow["ThoiDiemSoatVe"] = DBNull.Value;
                _currentSoatVeRow["TenGaSoat"] = DBNull.Value;

                if (_cachedManifest != null)
                {
                    DataRow[] found = _cachedManifest.Select($"MaVe = {maVe}");
                    if (found.Length > 0)
                    {
                        found[0]["TrangThai"] = "DA_DAT";
                        found[0]["ThoiDiemSoatVe"] = DBNull.Value;
                        found[0]["TenGaSoat"] = DBNull.Value;
                    }
                }

                HienThiChiTietVeSoat(_currentSoatVeRow);
                HienThiKetQuaHopLe("ĐÃ HOÀN TÁC SOÁT VÉ", $"Vé của {tenKhach} đã trở về trạng thái 'Chưa lên tàu'.");
                CapNhatKpiSoatVe();
                ApDungLocManifest();
                ThongBaoDialog.ThanhCong($"Đã hoàn tác trạng thái soát vé cho hành khách {tenKhach}!", "Hoàn Tác Thành Công");
            }
            else
            {
                ThongBaoDialog.Loi($"Lỗi hủy soát vé: {err}", "Lỗi");
            }
        }

        private void DgvManifest_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvManifest.SelectedItem is DataRowView drv)
            {
                _currentSoatVeRow = drv.Row;
                HienThiChiTietVeSoat(drv.Row);
                string tt = drv.Row["TrangThai"]?.ToString() ?? "";
                if (tt == "DA_LEN_TAU")
                {
                    HienThiKetQuaHopLe("HÀNH KHÁCH ĐÃ LÊN TÀU", "Vé đã được kiểm soát hợp lệ trước đó.");
                }
                else if (tt == "DA_DAT")
                {
                    HienThiKetQuaHopLe("HÀNH KHÁCH CHƯA LÊN TÀU", "Sẵn sàng thực hiện thủ tục lên tàu.");
                }
                else
                {
                    HienThiKetQuaLoi("VÉ ĐÃ HOÀN HOẶC HỦY", "Vé không còn giá trị lên tàu.");
                }
            }
        }

        private void DgvManifest_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvManifest.SelectedItem is DataRowView drv)
            {
                string tt = drv.Row["TrangThai"]?.ToString() ?? "";
                if (tt == "DA_DAT")
                {
                    XacNhanLenTau(drv.Row, hienThiThongBao: true);
                }
                else if (tt == "DA_LEN_TAU")
                {
                    BtnHuySoatVe_Click(sender, e);
                }
            }
        }

        private void BtnDongSoatVe_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView drv)
            {
                string tt = drv.Row["TrangThai"]?.ToString() ?? "";
                if (tt == "DA_DAT")
                {
                    _currentSoatVeRow = drv.Row;
                    XacNhanLenTau(drv.Row, hienThiThongBao: true);
                }
                else if (tt == "DA_LEN_TAU")
                {
                    _currentSoatVeRow = drv.Row;
                    BtnHuySoatVe_Click(sender, e);
                }
            }
        }

        private void BtnDongInThe_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView drv)
            {
                _currentSoatVeRow = drv.Row;
                BtnXemTheLenTauSoatVe_Click(sender, e);
            }
        }

        private void BtnXemTheLenTauSoatVe_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSoatVeRow == null)
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn một vé trên bảng kiểm soát để xem thẻ lên tàu!", "Chưa Chọn Vé");
                return;
            }

            try
            {
                var row = _currentSoatVeRow;
                var dialog = new TheLenTauDialog
                {
                    Owner = Window.GetWindow(this)
                };

                string maPNR = row.Table.Columns.Contains("MaPNR") ? row["MaPNR"]?.ToString() ?? "" : "";
                string maVeCode = row["MaVeCode"]?.ToString() ?? "";
                string macTau = row.Table.Columns.Contains("SoHieuMacTau") ? row["SoHieuMacTau"]?.ToString() ?? "" : (cboChuyenTauSoatVe.Text);
                string gaDi = row["TenGaDi"]?.ToString() ?? "";
                string gaDen = row["TenGaDen"]?.ToString() ?? "";
                string gioDi = DateTimeFormatHelper.DinhDangGio(row, "GioDiKH", "GioXuatPhatKH", "06:00");
                string tenKhach = row["TenHanhKhach"]?.ToString() ?? "";
                string cccd = row["CCCDHanhKhach"]?.ToString() ?? "";
                string toaXe = $"Toa {row["ThuTuNoiToa"]}";
                int soGhe = Convert.ToInt32(row["SoGhe"]);
                decimal giaVe = row["GiaVeThucThu"] != DBNull.Value ? Convert.ToDecimal(row["GiaVeThucThu"]) : 0;

                dialog.SetThongTinVe(maPNR, maVeCode, macTau, gaDi, gaDen, gioDi, tenKhach, cccd, toaXe, soGhe, giaVe);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi hiển thị thẻ lên tàu: {ex.Message}", "Lỗi");
            }
        }

        private void BtnSoatTatCaGaHienTai_Click(object sender, RoutedEventArgs e)
        {
            if (_cachedManifest == null || _cachedManifest.Rows.Count == 0)
            {
                ThongBaoDialog.ThongTin("Chưa có danh sách hành khách của chuyến tàu!", "Danh Sách Trống");
                return;
            }

            DataRow[] khachChuaLen = _cachedManifest.Select($"MaGaDi = {_currentSoatMaGa} AND TrangThai = 'DA_DAT'");
            if (khachChuaLen.Length == 0)
            {
                ThongBaoDialog.ThongTin("Tất cả hành khách đón tại ga này đã được soát vé lên tàu đầy đủ!", "Đã Hoàn Tất");
                return;
            }

            if (!ThongBaoDialog.XacNhan($"Có {khachChuaLen.Length} hành khách đón tại ga hiện tại chưa lên tàu.\nBạn có muốn xác nhận lên tàu nhanh cho toàn bộ {khachChuaLen.Length} hành khách này?", "Xác Nhận Soát Toàn Bộ"))
            {
                return;
            }

            int successCount = 0;
            foreach (var row in khachChuaLen)
            {
                int maVe = Convert.ToInt32(row["MaVe"]);
                if (_veService.SoatVe(maVe, _currentSoatMaGa, out _))
                {
                    row["TrangThai"] = "DA_LEN_TAU";
                    row["ThoiDiemSoatVe"] = DateTime.Now;
                    if (cboGaKiemSoat.SelectedItem is DataRowView drv)
                    {
                        row["TenGaSoat"] = drv["TenGa"];
                    }
                    successCount++;
                }
            }

            CapNhatKpiSoatVe();
            ApDungLocManifest();
            ThongBaoDialog.ThanhCong($"Đã xác nhận lên tàu thành công cho {successCount} hành khách đón tại ga!", "Hoàn Tất");
        }

        private void BtnInDanhSachManifest_Click(object sender, RoutedEventArgs e)
        {
            if (_cachedManifest == null || _cachedManifest.Rows.Count == 0)
            {
                ThongBaoDialog.ThongTin("Chưa có danh sách hành khách để xuất manifest!", "Thông Báo");
                return;
            }

            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Tệp CSV (Excel)|*.csv|Tệp văn bản|*.txt",
                    FileName = $"Manifest_Chuyen_{_currentSoatMaChuyenTau}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (sfd.ShowDialog() == true)
                {
                    using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                    sw.WriteLine("STT,Mã Vé,Họ Tên Hành Khách,CCCD,Toa,Số Ghế,Ga Đi,Ga Đến,Giá Vé,Trạng Thái,Thời Điểm Soát,Ga Soát");
                    int stt = 1;
                    foreach (DataRow r in _cachedManifest.Rows)
                    {
                        string tt = r["TrangThai"]?.ToString() switch
                        {
                            "DA_LEN_TAU" => "Đã lên tàu",
                            "DA_DAT" => "Chưa lên tàu",
                            "DA_HOAN_VE" => "Đã hoàn",
                            "DA_HUY" => "Đã hủy",
                            _ => ""
                        };
                        string td = r["ThoiDiemSoatVe"] != DBNull.Value ? Convert.ToDateTime(r["ThoiDiemSoatVe"]).ToString("yyyy-MM-dd HH:mm:ss") : "";
                        sw.WriteLine($"{stt++},\"{r["MaVeCode"]}\",\"{r["TenHanhKhach"]}\",\"{r["CCCDHanhKhach"]}\",Toa {r["ThuTuNoiToa"]},Số {r["SoGhe"]},\"{r["TenGaDi"]}\",\"{r["TenGaDen"]}\",{r["GiaVeThucThu"]},\"{tt}\",\"{td}\",\"{r["TenGaSoat"]}\"");
                    }
                    ThongBaoDialog.ThanhCong("Đã xuất danh sách Manifest thành công!", "Xuất Tệp");
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi xuất tệp: {ex.Message}", "Lỗi");
            }
        }

        public void FocusTimKiem()
        {
            txtTimKiemManifest?.Focus();
            txtTimKiemManifest?.SelectAll();
        }

        public void FocusScanner()
        {
            txtScannerInput?.Focus();
            txtScannerInput?.SelectAll();
        }

        public void FocusLocManifest()
        {
            txtTimKiemManifest?.Focus();
            txtTimKiemManifest?.SelectAll();
        }

        public void KichHoatSoatVe()
        {
            txtScannerInput?.Focus();
            txtScannerInput?.SelectAll();
        }

        public void KichHoatNapLai()
        {
            DateTime ngay = dpNgaySoatVe.SelectedDate ?? DateTime.Today;
            TaiDanhSachChuyenTauTheoNgay(ngay);
            LoadManifestChuyenTau();
        }

        public void KichHoatXuatTep()
        {
            BtnInDanhSachManifest_Click(this, new RoutedEventArgs());
        }

        private void BtnChuyenSangChuyenTauNay_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingChuyenMaChuyenTau > 0 && _pendingChuyenNgay.HasValue)
            {
                int targetChuyen = _pendingChuyenMaChuyenTau;
                DateTime targetNgay = _pendingChuyenNgay.Value.Date;

                dpNgaySoatVe.SelectedDate = targetNgay;
                TaiDanhSachChuyenTauTheoNgay(targetNgay);
                cboChuyenTauSoatVe.SelectedValue = targetChuyen;

                if (btnChuyenSangChuyenTauNay != null)
                {
                    btnChuyenSangChuyenTauNay.Visibility = Visibility.Collapsed;
                }

                ThucHienKiemTraVaSoatVe();
            }
        }

        private void ChuanHoaDatePicker(DatePicker dp)
        {
            try
            {
                dp.ApplyTemplate();
                var box = FindVisualChild<System.Windows.Controls.Primitives.DatePickerTextBox>(dp);
                if (box != null)
                {
                    box.Height = double.NaN;
                    box.MinHeight = 0;
                    box.Margin = new Thickness(0);
                    box.Padding = new Thickness(4, 0, 2, 0);
                    box.VerticalAlignment = VerticalAlignment.Center;
                    box.VerticalContentAlignment = VerticalAlignment.Center;
                }
            }
            catch { }
        }

        private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild) return typedChild;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
