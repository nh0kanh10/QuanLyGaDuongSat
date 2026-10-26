using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Input;
using BUS.Services;
using ET.VanHanh;
using GUI.Views.Dialogs;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.Pages
{
    public partial class LichTrinhPage : Page
    {
        private readonly ChuyenTauService _chuyenTauService = new();
        private readonly GaService _gaService = new();

        private DataTable? _cachedChuyenTau;
        private DataTable? _cachedMacTau;
        private DataTable? _cachedGa;
        private DataTable? _dtLichDungInMem;
        private int _tempIdDiemDung = -1;
        private bool _isAddingNew = false;
        private bool _isLoading = true;
        private string _currentTrangThai = "";
        private int _currentSoPhutTre = 0;
        private DateTime? _currentGioDiTT = null;
        private DateTime? _currentGioDenTT = null;
        private string? _currentLyDoHuy = null;

        private static readonly System.Collections.Generic.Dictionary<string, string> GioXuatPhatChuanTheoMac = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SE1"] = "06:00",
            ["SE2"] = "19:00",
            ["SE3"] = "19:00",
            ["SE4"] = "19:00",
            ["SE5"] = "08:50",
            ["SE6"] = "08:45",
            ["SE7"] = "06:10",
            ["SE8"] = "06:00",
            ["SE19"] = "19:50",
            ["SE20"] = "18:05",
            ["NA1"] = "22:15",
            ["NA2"] = "21:30",
            ["HBN1"] = "04:00"
        };

        private int _selectedMaChuyenTau = 0;
        private int _editingMaDiemDung = 0;

        private int _currentStep = 1;
        private bool _isUpdatingStep = false;

        private static readonly SolidColorBrush ActiveBgBrush = new(Color.FromRgb(0, 59, 115));
        private static readonly SolidColorBrush ActiveFgBrush = new(Color.FromRgb(0, 59, 115));
        private static readonly SolidColorBrush CompletedBgBrush = new(Color.FromRgb(21, 128, 61));
        private static readonly SolidColorBrush CompletedLineBrush = new(Color.FromRgb(21, 128, 61));
        private static readonly SolidColorBrush InactiveBgBrush = new(Color.FromRgb(226, 232, 240));
        private static readonly SolidColorBrush InactiveFgBrush = new(Color.FromRgb(100, 116, 139));
        private static readonly SolidColorBrush InactiveLineBrush = new(Color.FromRgb(203, 213, 225));

        public static bool TryParseDecimalInvariant(string? text, out decimal result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string cleaned = text.Trim().Replace(',', '.');
            return decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result);
        }

        private static DataTable TaoDataTableLichDungRong()
        {
            var dt = new DataTable();
            dt.Columns.Add("MaDiemDung", typeof(int));
            dt.Columns.Add("MaChuyenTau", typeof(int));
            dt.Columns.Add("MaGa", typeof(int));
            dt.Columns.Add("ThuTuDung", typeof(int));
            dt.Columns.Add("GioDenKeHoach", typeof(DateTime));
            dt.Columns.Add("GioDiKeHoach", typeof(DateTime));
            dt.Columns.Add("GioDenThucTe", typeof(DateTime));
            dt.Columns.Add("GioDiThucTe", typeof(DateTime));
            dt.Columns.Add("MaDuongRay", typeof(int));
            dt.Columns.Add("LaDiemTranh", typeof(bool));
            dt.Columns.Add("TenGa", typeof(string));
            dt.Columns.Add("MaGaCode", typeof(string));
            dt.Columns.Add("LyTrinhKm", typeof(decimal));
            dt.Columns.Add("ThoiGianDungPhut", typeof(int));
            dt.Columns.Add("SoNgayQuaDemDen", typeof(int));
            dt.Columns.Add("SoNgayQuaDemDi", typeof(int));
            return dt;
        }

        private void CapNhatThongKeLichDungInMem()
        {
            if (_dtLichDungInMem == null) return;

            dgLichDungGa.ItemsSource = _dtLichDungInMem.DefaultView;
            txtTabBadgeDungGa.Text = _dtLichDungInMem.Rows.Count.ToString();

            if (_dtLichDungInMem.Rows.Count > 0)
            {
                txtDungGaSubtitle.Text = $"Lộ trình gồm {_dtLichDungInMem.Rows.Count} ga dừng đón trả khách và tránh tàu";
                if (dgLichDungGa.SelectedIndex < 0) dgLichDungGa.SelectedIndex = 0;

                decimal kmDau = _dtLichDungInMem.Rows[0]["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(_dtLichDungInMem.Rows[0]["LyTrinhKm"]) : 0m;
                decimal kmCuoi = _dtLichDungInMem.Rows[^1]["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(_dtLichDungInMem.Rows[^1]["LyTrinhKm"]) : 0m;
                decimal cuLy = Math.Abs(kmCuoi - kmDau);

                DateTime dtDi = _dtLichDungInMem.Rows[0]["GioDiKeHoach"] != DBNull.Value ? Convert.ToDateTime(_dtLichDungInMem.Rows[0]["GioDiKeHoach"]) : DateTime.Today;
                DateTime dtDen = _dtLichDungInMem.Rows[^1]["GioDenKeHoach"] != DBNull.Value ? Convert.ToDateTime(_dtLichDungInMem.Rows[^1]["GioDenKeHoach"]) : dtDi;
                TimeSpan tgChay = dtDen - dtDi;
                if (tgChay.TotalMinutes < 0) tgChay = tgChay.Add(TimeSpan.FromHours(24));

                double tongGio = tgChay.TotalHours;
                double vtb = (tongGio > 0 && cuLy > 0) ? (double)cuLy / tongGio : 0;

                int soGaTranh = 0;
                foreach (DataRow r in _dtLichDungInMem.Rows)
                {
                    if (r["LaDiemTranh"] != DBNull.Value && (Convert.ToBoolean(r["LaDiemTranh"]) || r["LaDiemTranh"].ToString() == "1"))
                        soGaTranh++;
                }

                txtTongKetLichTrinh.Text = $"Hành trình: {cuLy:N1} km | Thời gian: {(int)tgChay.TotalHours}h {tgChay.Minutes}p | Vận tốc TB: {vtb:N1} km/h | {_dtLichDungInMem.Rows.Count} ga ({soGaTranh} ga tránh)";
            }
            else
            {
                txtDungGaSubtitle.Text = "Hành trình chưa có điểm dừng (nhấn Lấy Mẫu Tuyến hoặc Thêm Ga Dừng)";
                txtTongKetLichTrinh.Text = "Chưa có dữ liệu hành trình điểm dừng.";
            }
        }

        private bool NapMauTuyenChoInMem()
        {
            if (cboMacTau.SelectedValue == null) return false;
            int maMac = Convert.ToInt32(cboMacTau.SelectedValue);
            DateTime ngayDi = (dpNgayChuyen.SelectedDate ?? DateTime.Today).Date;
            var tDi = ParseTimeSpan(txtGioDiKH.Text) ?? TimeSpan.FromHours(6);

            var dtMau = _chuyenTauService.LayLichDungGaMauTheoMac(maMac, ngayDi, tDi);
            if (dtMau != null && dtMau.Rows.Count > 0)
            {
                _dtLichDungInMem = dtMau.Copy();
                _tempIdDiemDung = -1;
                foreach (DataRow r in _dtLichDungInMem.Rows)
                {
                    r["MaDiemDung"] = _tempIdDiemDung--;
                    r["MaChuyenTau"] = 0;
                }
                CapNhatThongKeLichDungInMem();
                return true;
            }
            return false;
        }

        public LichTrinhPage()
        {
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage("vi-VN");
            InitializeComponent();
            KhoiTaoDuLieu();
        }

        private void KhoiTaoDuLieu()
        {
            _isLoading = true;
            try
            {
                NapLaiDanhSachMacTau();

                _cachedGa = _gaService.LayDanhSach();
                cboThemDungGa.ItemsSource = _cachedGa.DefaultView;
                if (_cachedGa.Rows.Count > 0)
                {
                    cboThemDungGa.SelectedIndex = 0;
                }

                dpNgayChuyen.SelectedDate = DateTime.Today;

                _isLoading = false;
                LoadData();
                CapNhatQuyTrinh(1);
            }
            catch (Exception ex)
            {
                _isLoading = false;
                ThongBaoDialog.Loi($"Lỗi khởi tạo dữ liệu: {ex.Message}", "Lỗi CSDL");
            }
        }

        public void LoadData()
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
                ThongBaoDialog.Loi($"Lỗi nạp danh sách lịch trình: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void CapNhatThongKe(DataTable dt)
        {
            int tong = dt.Rows.Count;
            int daLenLich = 0;
            int sanSang = 0;
            int dangChay = 0;
            int treGio = 0;
            int hoanThanh = 0;

            foreach (DataRow r in dt.Rows)
            {
                string tt = r["TrangThai"]?.ToString() ?? "";
                if (tt == "DA_LEN_LICH") daLenLich++;
                else if (tt == "SAN_SANG") sanSang++;
                else if (tt == "DANG_CHAY") dangChay++;
                else if (tt == "TRE_GIO") treGio++;
                else if (tt == "HOAN_THANH") hoanThanh++;
            }

            txtTongSo.Text = $"Tổng số: {tong} chuyến";
            txtThongKeLenLich.Text = $"Đã lên lịch: {daLenLich}";
            txtThongKeSanSang.Text = $"Sẵn sàng: {sanSang}";
            txtThongKeChay.Text = $"Đang chạy: {dangChay}";
            txtThongKeTre.Text = $"Chậm giờ: {treGio}";
            txtThongKeHoanThanh.Text = $"Hoàn thành: {hoanThanh}";
        }

        private void ChipThongKe_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string tag)
            {
                for (int i = 0; i < cboLocTrangThai.Items.Count; i++)
                {
                    if (cboLocTrangThai.Items[i] is ComboBoxItem item && item.Tag?.ToString() == tag)
                    {
                        cboLocTrangThai.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void DgChuyenTau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || _isAddingNew) return;

            // Cô lập form điểm dừng: Đổi chuyến tàu lập tức đóng form và hủy chế độ sửa của chuyến cũ
            if (pnlFormDiemDung != null) pnlFormDiemDung.Visibility = Visibility.Collapsed;
            _editingMaDiemDung = 0;

            if (dgChuyenTau.SelectedItem is DataRowView row)
            {
                _selectedMaChuyenTau = Convert.ToInt32(row["MaChuyenTau"]);

                string soHieu = row["SoHieuMacTau"]?.ToString() ?? "";
                string trangThai = row["TrangThai"]?.ToString() ?? "";
                DateTime ngayXuatPhat = row["NgayXuatPhat"] != DBNull.Value ? Convert.ToDateTime(row["NgayXuatPhat"]) : DateTime.Today;

                string tieuDeTheoTrangThai = trangThai switch
                {
                    "DA_LEN_LICH" => $"LẬP KẾ HOẠCH CHUYẾN TÀU: {soHieu}",
                    "SAN_SANG" => $"KẾ HOẠCH SẴN SÀNG: {soHieu}",
                    "DANG_CHAY" => $"ĐIỀU HÀNH THỜI GIAN THỰC: {soHieu}",
                    "TRE_GIO" => $"ĐIỀU HÀNH THỜI GIAN THỰC (CHẬM): {soHieu}",
                    "HOAN_THANH" => $"HỒ SƠ HOÀN THÀNH: {soHieu}",
                    "DA_HUY" => $"HỒ SƠ ĐÃ HỦY: {soHieu}",
                    _ => $"CHI TIẾT CHUYẾN TÀU: {soHieu}"
                };
                txtInspectorTitle.Text = tieuDeTheoTrangThai;
                txtInspectorSubtitle.Text = $"Hành trình: {row["TenGaDi"]} → {row["TenGaDen"]} | Khởi hành: {ngayXuatPhat:dd/MM/yyyy}";
                txtInspectorBadge.Text = $"Mã #{_selectedMaChuyenTau} • {DinhDangTrangThai(trangThai)}";

                // Tab 1: Form Chuyến Tàu
                if (row["MaMacTau"] != DBNull.Value)
                {
                    cboMacTau.SelectedValue = Convert.ToInt32(row["MaMacTau"]);
                }

                txtLoaiTau.Text = DinhDangLoaiTau(row["LoaiTau"]?.ToString());
                txtHuongChay.Text = DinhDangHuongChay(row["HuongChay"]?.ToString());
                txtGaDi.Text = row["TenGaDi"]?.ToString() ?? "";
                txtGaDen.Text = row["TenGaDen"]?.ToString() ?? "";

                // Khóa form kế hoạch và các thao tác thay đổi nếu tàu đang chạy, trễ giờ, đã hoàn thành hoặc đã hủy
                bool isKhoaChinhSua = (trangThai == "DANG_CHAY" || trangThai == "TRE_GIO" || trangThai == "HOAN_THANH" || trangThai == "DA_HUY");
                cboMacTau.IsEnabled = !isKhoaChinhSua;
                dpNgayChuyen.IsEnabled = !isKhoaChinhSua;
                txtGioDiKH.IsEnabled = !isKhoaChinhSua;
                txtGioDenKH.IsEnabled = !isKhoaChinhSua;
                cboDauMayChinh.IsEnabled = !isKhoaChinhSua;
                cboDauMayDay.IsEnabled = !isKhoaChinhSua;

                btnLuuChuyen.IsEnabled = !isKhoaChinhSua;
                if (btnSapXepTheoKm != null) btnSapXepTheoKm.IsEnabled = !isKhoaChinhSua;
                if (btnSaoChepMau != null) btnSaoChepMau.IsEnabled = !isKhoaChinhSua;
                if (btnMoFormThemDung != null) btnMoFormThemDung.IsEnabled = !isKhoaChinhSua;

                _currentTrangThai = trangThai;
                _currentSoPhutTre = row["SoPhutTreLuyKe"] != DBNull.Value ? Convert.ToInt32(row["SoPhutTreLuyKe"]) : 0;
                _currentGioDiTT = row["GioXuatPhatTT"] != DBNull.Value ? Convert.ToDateTime(row["GioXuatPhatTT"]) : null;
                _currentGioDenTT = row["GioVeDichTT"] != DBNull.Value ? Convert.ToDateTime(row["GioVeDichTT"]) : null;
                _currentLyDoHuy = row["LyDoHuy"]?.ToString();

                dpNgayChuyen.SelectedDate = ngayXuatPhat;

                if (row["GioXuatPhatKH"] != DBNull.Value)
                    txtGioDiKH.Text = Convert.ToDateTime(row["GioXuatPhatKH"]).ToString("HH:mm");
                else
                    txtGioDiKH.Text = "06:00";

                if (row["GioVeDichKH"] != DBNull.Value)
                    txtGioDenKH.Text = Convert.ToDateTime(row["GioVeDichKH"]).ToString("HH:mm");
                else
                    txtGioDenKH.Text = "18:30";

                CapNhatDuKienVeDich();

                btnXoaChuyen.IsEnabled = true;
                btnNextStep.IsEnabled = true;
                btnPrevStep.IsEnabled = true;
                txtBtnLuuChuyen.Text = "Lưu Thay Đổi";

                // Cập nhật giao diện biên chế phương tiện theo loại tàu (Tàu Hàng vs Tàu Khách)
                string loaiTau = row["LoaiTau"]?.ToString() ?? "";
                CapNhatHienThiTheoLoaiTau(loaiTau);

                // Tab 2 & 3: Tải dữ liệu con
                LoadLichDungGa(_selectedMaChuyenTau);
                LoadToaXe(_selectedMaChuyenTau);

                // Tải thông tin đoàn tàu & đầu máy kéo
                DateTime dtDiVal = row["GioXuatPhatKH"] != DBNull.Value ? Convert.ToDateTime(row["GioXuatPhatKH"]) : DateTime.Today.AddHours(6);
                DateTime dtDenVal = row["GioVeDichKH"] != DBNull.Value ? Convert.ToDateTime(row["GioVeDichKH"]) : DateTime.Today.AddDays(1).AddHours(14);
                LoadDoanTau(_selectedMaChuyenTau, dtDiVal, dtDenVal);
            }
            else
            {
                XoaThongTinChiTiet();
            }
        }

        private void CboMacTau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboMacTau.SelectedItem is DataRowView row)
            {
                string mac = row["SoHieuMacTau"]?.ToString() ?? "";
                txtLoaiTau.Text = DinhDangLoaiTau(row["LoaiTau"]?.ToString());
                txtHuongChay.Text = DinhDangHuongChay(row["HuongChay"]?.ToString());
                txtGaDi.Text = $"{row["TenGaDi"]} ({row["MaGaDiCode"]})";
                txtGaDen.Text = $"{row["TenGaDen"]} ({row["MaGaDenCode"]})";

                string rawLoai = row["LoaiTau"]?.ToString() ?? "";
                CapNhatHienThiTheoLoaiTau(rawLoai);

                // Tự động gán giờ xuất phát chuẩn và tính toán giờ đến dự kiến nếu đang lập chuyến mới
                if (_isAddingNew)
                {
                    string gioDiChuan = GioXuatPhatChuanTheoMac.TryGetValue(mac, out string? g) ? g : "06:00";
                    txtGioDiKH.Text = gioDiChuan;

                    if (decimal.TryParse(row["ThoiGianChuanGio"]?.ToString(), out decimal gioChuan))
                    {
                        var tDi = ParseTimeSpan(txtGioDiKH.Text);
                        if (tDi.HasValue)
                        {
                            DateTime ngayDi = (dpNgayChuyen.SelectedDate ?? DateTime.Today).Date;
                            DateTime dtDi = ngayDi.Add(tDi.Value);
                            DateTime dtDen = dtDi.AddHours((double)gioChuan);
                            txtGioDenKH.Text = dtDen.ToString("HH:mm");
                            LoadDanhSachDauMay(dtDi, dtDen, 0);
                            if (cboDauMayChinh.SelectedIndex < 0)
                            {
                                ChonDauMaySanSangMacDinh();
                            }
                        }
                    }

                    // Tự động nạp lộ trình điểm dừng mẫu cho mác tàu này trên RAM
                    NapMauTuyenChoInMem();
                }

                CapNhatDuKienVeDich();
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

                    // Tính toán tóm tắt thông số kỹ thuật hành trình toàn tuyến
                    decimal kmDau = dt.Rows[0]["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[0]["LyTrinhKm"]) : 0m;
                    decimal kmCuoi = dt.Rows[^1]["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(dt.Rows[^1]["LyTrinhKm"]) : 0m;
                    decimal cuLy = Math.Abs(kmCuoi - kmDau);

                    DateTime dtDi = dt.Rows[0]["GioDiKeHoach"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[0]["GioDiKeHoach"]) : DateTime.Today;
                    DateTime dtDen = dt.Rows[^1]["GioDenKeHoach"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[^1]["GioDenKeHoach"]) : dtDi;
                    TimeSpan tgChay = dtDen - dtDi;
                    if (tgChay.TotalMinutes < 0) tgChay = tgChay.Add(TimeSpan.FromHours(24));

                    double tongGio = tgChay.TotalHours;
                    double vtb = (tongGio > 0 && cuLy > 0) ? (double)cuLy / tongGio : 0;

                    int soGaTranh = 0;
                    foreach (DataRow r in dt.Rows)
                    {
                        if (r["LaDiemTranh"] != DBNull.Value && (Convert.ToBoolean(r["LaDiemTranh"]) || r["LaDiemTranh"].ToString() == "1"))
                            soGaTranh++;
                    }

                    txtTongKetLichTrinh.Text = $"Hành trình: {cuLy:N1} km | Thời gian: {(int)tgChay.TotalHours}h {tgChay.Minutes}p | Vận tốc TB: {vtb:N1} km/h | {dt.Rows.Count} ga ({soGaTranh} ga tránh)";
                }
                else
                {
                    txtDungGaSubtitle.Text = "Hành trình chưa có điểm dừng (nhấn Lấy Mẫu Tuyến hoặc Thêm Ga Dừng)";
                    txtTongKetLichTrinh.Text = "Chưa có dữ liệu hành trình điểm dừng.";
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi tải lịch dừng ga: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void LoadToaXe(int maChuyenTau)
        {
            try
            {
                DataTable? dt = null;
                if (maChuyenTau > 0)
                {
                    dt = _chuyenTauService.LayDanhSachToaXe(maChuyenTau);
                }

                // Nếu chuyến tàu chưa có toa xe riêng (hoặc đang tạo mới), lấy theo đầu máy/đoàn tàu đang chọn
                if ((dt == null || dt.Rows.Count == 0) && cboDauMayChinh?.SelectedValue != null)
                {
                    if (int.TryParse(cboDauMayChinh.SelectedValue.ToString(), out int maDm) && maDm > 0)
                    {
                        dt = _chuyenTauService.LayDanhSachToaXeTheoDauMay(maDm);
                    }
                }

                HienThiDanhSachToaXe(dt ?? new DataTable());
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi tải biên chế toa xe: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void HienThiDanhSachToaXe(DataTable dt)
        {
            if (dgToaXe == null) return;

            dgToaXe.ItemsSource = dt.DefaultView;
            if (txtTabBadgeToaXe != null) txtTabBadgeToaXe.Text = dt.Rows.Count.ToString();

            int tongSucChua = 0;
            foreach (DataRow r in dt.Rows)
            {
                if (r["SucChua"] != DBNull.Value && int.TryParse(r["SucChua"].ToString(), out int sc))
                {
                    tongSucChua += sc;
                }
            }

            if (txtTongSoToa != null) txtTongSoToa.Text = $"{dt.Rows.Count} toa xe";
            if (txtTongSucChua != null) txtTongSucChua.Text = $"{tongSucChua:N0} chỗ ngồi / giường nằm";
        }

        private void XoaThongTinChiTiet()
        {
            _selectedMaChuyenTau = 0;

            txtInspectorTitle.Text = "CHI TIẾT ĐIỀU HÀNH CHUYẾN TÀU";
            txtInspectorSubtitle.Text = "Không có chuyến tàu nào phù hợp với bộ lọc";
            txtInspectorBadge.Text = "ID: #0";

            txtLoaiTau.Text = string.Empty;
            txtHuongChay.Text = string.Empty;
            txtGaDi.Text = string.Empty;
            txtGaDen.Text = string.Empty;
            txtGioDiKH.Text = string.Empty;
            txtGioDenKH.Text = string.Empty;
            _currentTrangThai = "";
            _currentSoPhutTre = 0;
            _currentGioDiTT = null;
            _currentGioDenTT = null;
            _currentLyDoHuy = null;

            cboDauMayChinh.ItemsSource = null;
            cboDauMayDay.ItemsSource = null;
            txtTongChieuDaiM.Text = "0.0";
            txtTongTrongLuongTan.Text = "0.0";
            chkDaDuyetAnToan.IsChecked = false;
            txtCanhBaoChieuDaiGa.Text = "Không có dữ liệu hành trình";
            CapNhatHienThiTheoLoaiTau("");
            if (bdCanhBaoDauMay != null) bdCanhBaoDauMay.Visibility = Visibility.Collapsed;

            // Vô hiệu hóa các nút hành động và điều khiển form khi danh sách rỗng
            btnXoaChuyen.IsEnabled = false;
            btnLuuChuyen.IsEnabled = false;
            btnNextStep.IsEnabled = false;
            btnPrevStep.IsEnabled = false;
            cboMacTau.IsEnabled = false;
            dpNgayChuyen.IsEnabled = false;
            txtGioDiKH.IsEnabled = false;
            txtGioDenKH.IsEnabled = false;
            cboDauMayChinh.IsEnabled = false;
            cboDauMayDay.IsEnabled = false;

            dgLichDungGa.ItemsSource = null;
            txtTabBadgeDungGa.Text = "0";
            txtDungGaSubtitle.Text = "Không có dữ liệu";
            txtTongKetLichTrinh.Text = "Không có dữ liệu hành trình";

            dgToaXe.ItemsSource = null;
            txtTabBadgeToaXe.Text = "0";
            txtTongSoToa.Text = "0 toa xe";
            txtTongSucChua.Text = "0 chỗ";
        }

        private void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = true;
            _selectedMaChuyenTau = 0;
            _currentTrangThai = "DA_LEN_LICH";
            _currentSoPhutTre = 0;
            _currentGioDiTT = null;
            _currentGioDenTT = null;
            _currentLyDoHuy = null;
            dgChuyenTau.SelectedItem = null;

            if (pnlFormDiemDung != null) pnlFormDiemDung.Visibility = Visibility.Collapsed;
            _editingMaDiemDung = 0;

            txtInspectorTitle.Text = "LẬP LỊCH TRÌNH CHUYẾN TÀU MỚI";
            txtInspectorSubtitle.Text = "Nhập thông tin kế hoạch và chọn mác tàu vận hành";
            txtInspectorBadge.Text = "MỚI";

            cboMacTau.IsEnabled = true;
            dpNgayChuyen.IsEnabled = true;
            txtGioDiKH.IsEnabled = true;
            txtGioDenKH.IsEnabled = true;
            cboDauMayChinh.IsEnabled = true;
            cboDauMayDay.IsEnabled = true;
            btnLuuChuyen.IsEnabled = true;
            if (btnSapXepTheoKm != null) btnSapXepTheoKm.IsEnabled = true;
            if (btnSaoChepMau != null) btnSaoChepMau.IsEnabled = true;
            if (btnMoFormThemDung != null) btnMoFormThemDung.IsEnabled = true;

            dpNgayChuyen.SelectedDate = DateTime.Today.AddDays(1);
            txtGioDiKH.Text = "06:00";
            txtGioDenKH.Text = "18:30";
            CapNhatDuKienVeDich();

            txtTongChieuDaiM.Text = "216.5";
            txtTongTrongLuongTan.Text = "478.0";
            chkDaDuyetAnToan.IsChecked = false;
            txtCanhBaoChieuDaiGa.Text = "Hành trình mới: Chiều dài đoàn tàu nằm trong giới hạn an toàn quy chuẩn.";
            bdTauHangNotice.Visibility = Visibility.Collapsed;

            DateTime dtDi = DateTime.Today.AddDays(1).AddHours(6);
            DateTime dtDen = dtDi.AddHours(32.5);
            LoadDanhSachDauMay(dtDi, dtDen, 0);
            ChonDauMaySanSangMacDinh();

            // Khởi tạo bảng lịch trình dừng trên bộ nhớ RAM (In-Memory)
            _dtLichDungInMem = TaoDataTableLichDungRong();

            if (cboMacTau.Items.Count > 0)
            {
                cboMacTau.SelectedIndex = 0;
                NapMauTuyenChoInMem();
            }
            else
            {
                CapNhatThongKeLichDungInMem();
            }

            btnXoaChuyen.IsEnabled = false;
            txtBtnLuuChuyen.Text = "Lập Chuyến Tàu";

            dgToaXe.ItemsSource = null;
            txtTabBadgeToaXe.Text = "0";
            txtTongSoToa.Text = "0 toa xe";
            txtTongSucChua.Text = "0 chỗ";

            CapNhatQuyTrinh(1);
        }

        private void NapLaiDanhSachMacTau(int? focusMaMacTau = null)
        {
            try
            {
                int oldLocIndex = cboLocMacTau.SelectedIndex;
                int oldSelectedMac = cboMacTau.SelectedValue != null && int.TryParse(cboMacTau.SelectedValue.ToString(), out int s) ? s : 0;

                _cachedMacTau = _chuyenTauService.LayDanhSachMacTau();
                cboMacTau.ItemsSource = _cachedMacTau.DefaultView;

                var dtLocMacTau = _cachedMacTau.Copy();
                var rowAll = dtLocMacTau.NewRow();
                rowAll["MaMacTau"] = 0;
                rowAll["SoHieuMacTau"] = "Tất cả mác tàu";
                dtLocMacTau.Rows.InsertAt(rowAll, 0);
                cboLocMacTau.ItemsSource = dtLocMacTau.DefaultView;

                if (oldLocIndex >= 0 && oldLocIndex < dtLocMacTau.Rows.Count)
                    cboLocMacTau.SelectedIndex = oldLocIndex;
                else
                    cboLocMacTau.SelectedIndex = 0;

                if (focusMaMacTau.HasValue && focusMaMacTau.Value > 0)
                {
                    cboMacTau.SelectedValue = focusMaMacTau.Value;
                }
                else if (oldSelectedMac > 0)
                {
                    cboMacTau.SelectedValue = oldSelectedMac;
                }
                else if (_cachedMacTau.Rows.Count > 0)
                {
                    cboMacTau.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi nạp lại danh sách mác tàu: {ex.Message}");
            }
        }

        private void BtnQuanLyMacTau_Click(object sender, RoutedEventArgs e)
        {
            int currentMac = 0;
            if (cboMacTau.SelectedValue != null && int.TryParse(cboMacTau.SelectedValue.ToString(), out int m))
            {
                currentMac = m;
            }

            var dialog = new QuanLyMacTauDialog(currentMac > 0 ? currentMac : null)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                NapLaiDanhSachMacTau(dialog.NewlySelectedOrCreatedMaMacTau);
                LoadData();
            }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            ThucHienLuuChuyenTau();
        }

        private bool KiemTraHopLeBuoc1(out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;

            if (cboMacTau.SelectedValue == null)
            {
                thongBaoLoi = "Vui lòng chọn mác tàu mẫu vận hành.";
                return false;
            }

            if (dpNgayChuyen.SelectedDate == null)
            {
                thongBaoLoi = "Vui lòng chọn ngày xuất phát của chuyến tàu.";
                return false;
            }

            DateTime ngayDi = dpNgayChuyen.SelectedDate.Value.Date;
            if (_isAddingNew && ngayDi < DateTime.Today)
            {
                thongBaoLoi = "Không thể lập lịch chuyến tàu cho ngày trong quá khứ!";
                return false;
            }

            var tDi = ParseTimeSpan(txtGioDiKH.Text);
            var tDen = ParseTimeSpan(txtGioDenKH.Text);

            if (!tDi.HasValue || !tDen.HasValue)
            {
                thongBaoLoi = "Vui lòng nhập đầy đủ Giờ xuất phát và Giờ đến kế hoạch hợp lệ (định dạng HH:mm, ví dụ: 06:00).";
                return false;
            }

            int maMac = Convert.ToInt32(cboMacTau.SelectedValue);
            DateTime dtDi = ngayDi.Add(tDi.Value);

            decimal tgChuanGio = 12.0m;
            if (cboMacTau.SelectedItem is DataRowView rMac && decimal.TryParse(rMac["ThoiGianChuanGio"]?.ToString(), out decimal tg) && tg > 0)
            {
                tgChuanGio = tg;
            }

            DateTime dtDen = ChuyenTauService.TinhToanGioVeDich(ngayDi, tDi.Value, tDen.Value, tgChuanGio);

            if (!ChuyenTauService.KiemTraThoiGianChayHopLy(dtDen - dtDi, tgChuanGio, out string canhBaoTg))
            {
                var xnHopLy = ThongBaoDialog.XacNhan($"{canhBaoTg}\n\nBạn có muốn tiếp tục áp dụng kế hoạch thời gian này không?",
                    "Cảnh Báo Kế Hoạch Chạy Tàu", nutDongY: "Tiếp Tục Áp Dụng", nutHuy: "Chỉnh Lại Giờ");
                if (!xnHopLy)
                {
                    thongBaoLoi = "Người dùng chọn chỉnh lại thời gian chạy.";
                    return false;
                }
            }

            // Kiểm tra trùng lịch mác tàu cùng ngày (loại trừ chính chuyến đang xem/sửa)
            int excludeId = _selectedMaChuyenTau > 0 ? _selectedMaChuyenTau : 0;
            if (_chuyenTauService.KiemTraTrungLich(maMac, ngayDi, excludeId))
            {
                thongBaoLoi = $"Mác tàu đã được lập lịch cho ngày {ngayDi:dd/MM/yyyy}. Không thể tạo chuyến trùng lặp!";
                return false;
            }

            return true;
        }

        private bool KiemTraHopLeBuoc2(out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;

            if (!_isAddingNew && _selectedMaChuyenTau <= 0)
            {
                thongBaoLoi = "Chưa chọn chuyến tàu hợp lệ!";
                return false;
            }

            int soGa = (dgLichDungGa.ItemsSource is DataView dv) ? dv.Count : 0;
            if (soGa < 2)
            {
                thongBaoLoi = "Hành trình chuyến tàu chưa có đủ điểm dừng (tối thiểu 2 ga: Ga xuất phát và Ga về đích)!\n\nVui lòng bấm [Lấy Mẫu Tuyến] để nạp lộ trình chuẩn hoặc bấm [+ Thêm Ga Dừng] để khai báo điểm dừng.";
                return false;
            }

            // Kiểm tra tính đơn điệu thời gian, thời gian dừng đỗ theo tác nghiệp và tính khả thi vận tốc khu gian
            if (dgLichDungGa.ItemsSource is DataView dvLich && dvLich.Count >= 2)
            {
                for (int i = 0; i < dvLich.Count; i++)
                {
                    var rCur = dvLich[i];

                    // 1. Ràng buộc thời gian dừng đỗ theo Tác Nghiệp (Dwell Time) tại các ga trung gian
                    if (i > 0 && i < dvLich.Count - 1)
                    {
                        if (rCur["GioDenKeHoach"] != DBNull.Value && rCur["GioDiKeHoach"] != DBNull.Value)
                        {
                            DateTime tDen = Convert.ToDateTime(rCur["GioDenKeHoach"]);
                            DateTime tDi = Convert.ToDateTime(rCur["GioDiKeHoach"]);
                            double phutDo = (tDi - tDen).TotalMinutes;

                            if (phutDo < 0)
                            {
                                thongBaoLoi = $"Lỗi thời gian đỗ ga: Ga #{rCur["ThuTuDung"]} ({rCur["TenGa"]}) có giờ rời ga ({tDi:HH:mm dd/MM}) trước giờ đến ({tDen:HH:mm dd/MM})!";
                                return false;
                            }

                            bool laDiemTranh = rCur["LaDiemTranh"] != DBNull.Value && Convert.ToBoolean(rCur["LaDiemTranh"]);
                            if (laDiemTranh)
                            {
                                if (phutDo < 5)
                                {
                                    thongBaoLoi = $"Ràng buộc tác nghiệp: Ga #{rCur["ThuTuDung"]} ({rCur["TenGa"]}) là Điểm Tránh Tàu nhưng thời gian đỗ chỉ có {phutDo:N0} phút (yêu cầu tối thiểu 5 phút để bảo đảm an toàn tránh vượt đường đơn)!";
                                    return false;
                                }
                            }
                            else
                            {
                                if (phutDo < 2)
                                {
                                    thongBaoLoi = $"Ràng buộc tác nghiệp: Ga #{rCur["ThuTuDung"]} ({rCur["TenGa"]}) đón/trả khách yêu cầu thời gian dừng đỗ tối thiểu 2 phút (hiện tại: {phutDo:N0} phút)!";
                                    return false;
                                }
                            }
                        }
                    }

                    // 2. Ràng buộc Đơn điệu thời gian & Tính khả thi Vận tốc Khu gian giữa 2 ga liên tiếp
                    if (i < dvLich.Count - 1)
                    {
                        var rNext = dvLich[i + 1];
                        if (rCur["GioDiKeHoach"] != DBNull.Value && rNext["GioDenKeHoach"] != DBNull.Value)
                        {
                            DateTime tDiCur = Convert.ToDateTime(rCur["GioDiKeHoach"]);
                            DateTime tDenNext = Convert.ToDateTime(rNext["GioDenKeHoach"]);

                            if (tDenNext <= tDiCur)
                            {
                                thongBaoLoi = $"Xung đột thời gian: Ga #{rNext["ThuTuDung"]} ({rNext["TenGa"]} lúc {tDenNext:HH:mm dd/MM}) có giờ đến không sau giờ rời ga #{rCur["ThuTuDung"]} ({rCur["TenGa"]} lúc {tDiCur:HH:mm dd/MM})!";
                                return false;
                            }

                            // Tính vận tốc khu gian
                            if (rCur["LyTrinhKm"] != DBNull.Value && rNext["LyTrinhKm"] != DBNull.Value)
                            {
                                decimal kmCur = Convert.ToDecimal(rCur["LyTrinhKm"]);
                                decimal kmNext = Convert.ToDecimal(rNext["LyTrinhKm"]);
                                decimal quangDuong = Math.Abs(kmNext - kmCur);
                                double thoiGianGio = (tDenNext - tDiCur).TotalHours;

                                if (quangDuong > 0 && thoiGianGio > 0)
                                {
                                    double vanToc = (double)quangDuong / thoiGianGio;
                                    if (vanToc > 120.0)
                                    {
                                        double thoiGianPhut = thoiGianGio * 60.0;
                                        thongBaoLoi = $"Cảnh báo vận tốc kỹ thuật không khả thi: Khu gian giữa ga #{rCur["ThuTuDung"]} ({rCur["TenGa"]}) và ga #{rNext["ThuTuDung"]} ({rNext["TenGa"]}) dài {quangDuong:F1} km nhưng chạy trong {thoiGianPhut:F0} phút (vận tốc ~{vanToc:F0} km/h, vượt quá giới hạn an toàn tối đa 120 km/h của đường sắt)!\n\nVui lòng điều chỉnh lại giờ đến của ga sau.";
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool KiemTraHopLeBuoc3(out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;

            int maDauMayChinh = 0;
            if (cboDauMayChinh.SelectedValue != null && int.TryParse(cboDauMayChinh.SelectedValue.ToString(), out int parsedDm) && parsedDm > 0)
            {
                maDauMayChinh = parsedDm;
            }

            if (maDauMayChinh <= 0)
            {
                thongBaoLoi = "Vui lòng chọn Đầu Máy Kéo Chính cho chuyến tàu (Bước 3: Phương Tiện)!";
                return false;
            }

            int? maDauMayDay = null;
            if (cboDauMayDay.SelectedValue != null && int.TryParse(cboDauMayDay.SelectedValue.ToString(), out int md) && md > 0)
            {
                maDauMayDay = md;
            }

            if (maDauMayDay.HasValue && maDauMayDay.Value == maDauMayChinh)
            {
                thongBaoLoi = "Đầu máy kéo chính và đầu máy đẩy không được trùng nhau!";
                return false;
            }

            DateTime ngayDi = (dpNgayChuyen.SelectedDate ?? DateTime.Today).Date;
            var tDi = ParseTimeSpan(txtGioDiKH.Text) ?? TimeSpan.FromHours(6);
            var tDen = ParseTimeSpan(txtGioDenKH.Text) ?? TimeSpan.FromHours(18);

            decimal tgChuanGio = 12.0m;
            if (cboMacTau.SelectedItem is DataRowView rMac && decimal.TryParse(rMac["ThoiGianChuanGio"]?.ToString(), out decimal tg) && tg > 0)
            {
                tgChuanGio = tg;
            }

            DateTime dtDi = ngayDi.Add(tDi);
            DateTime dtDen = ChuyenTauService.TinhToanGioVeDich(ngayDi, tDi, tDen, tgChuanGio);

            // Kiểm tra xung đột đầu máy chính
            if (!_chuyenTauService.KiemTraXungDotDauMay(maDauMayChinh, dtDi, dtDen, _isAddingNew ? 0 : _selectedMaChuyenTau, out string errChinh))
            {
                thongBaoLoi = $"Đầu máy chính: {errChinh}";
                return false;
            }

            // Kiểm tra xung đột đầu máy đẩy (nếu có)
            if (maDauMayDay.HasValue)
            {
                if (!_chuyenTauService.KiemTraXungDotDauMay(maDauMayDay.Value, dtDi, dtDen, _isAddingNew ? 0 : _selectedMaChuyenTau, out string errDay))
                {
                    thongBaoLoi = $"Đầu máy đẩy: {errDay}";
                    return false;
                }
            }

            return true;
        }


        private void CapNhatThongSoBuoc1()
        {
            if (_selectedMaChuyenTau <= 0) return;

            // Khóa an toàn: Tuyệt đối không can thiệp kế hoạch nếu chuyến đang vận hành thực tế hoặc đã kết thúc/hủy
            if (_currentTrangThai == "DANG_CHAY" || _currentTrangThai == "TRE_GIO" || _currentTrangThai == "HOAN_THANH" || _currentTrangThai == "DA_HUY")
            {
                return;
            }

            try
            {
                int maMac = Convert.ToInt32(cboMacTau.SelectedValue);
                DateTime ngayDi = dpNgayChuyen.SelectedDate!.Value.Date;
                var tDi = ParseTimeSpan(txtGioDiKH.Text)!.Value;
                var tDen = ParseTimeSpan(txtGioDenKH.Text)!.Value;

                decimal tgChuanGio = 12.0m;
                if (cboMacTau.SelectedItem is DataRowView rMac && decimal.TryParse(rMac["ThoiGianChuanGio"]?.ToString(), out decimal tg) && tg > 0)
                {
                    tgChuanGio = tg;
                }

                DateTime dtDi = ngayDi.Add(tDi);
                DateTime dtDen = ChuyenTauService.TinhToanGioVeDich(ngayDi, tDi, tDen, tgChuanGio);

                // Chỉ cập nhật các thông số kế hoạch, bảo toàn 100% dữ liệu thực tế (giờ thực tế, trễ lũy kế, lý do hủy)
                _chuyenTauService.CapNhatThongSoKeHoach(_selectedMaChuyenTau, maMac, ngayDi, dtDi, dtDen);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi cập nhật Bước 1: {ex.Message}");
            }
        }

        private bool ThucHienLuuChuyenTau()
        {
            // Kiểm tra tuần tự từng bước:
            if (!KiemTraHopLeBuoc1(out string err1))
            {
                if (!string.IsNullOrWhiteSpace(err1) && err1 != "Người dùng chọn chỉnh lại thời gian chạy.")
                {
                    ThongBaoDialog.CanhBao(err1, "Thông Số Hành Trình (Bước 1)");
                }
                CapNhatQuyTrinh(1);
                return false;
            }

            if (!KiemTraHopLeBuoc2(out string err2))
            {
                ThongBaoDialog.CanhBao(err2, "Lịch Trình Điểm Dừng (Bước 2)");
                CapNhatQuyTrinh(2);
                return false;
            }

            if (!KiemTraHopLeBuoc3(out string err3))
            {
                ThongBaoDialog.CanhBao(err3, "Phương Tiện Vận Hành (Bước 3)");
                CapNhatQuyTrinh(3);
                return false;
            }

            int maMac = Convert.ToInt32(cboMacTau.SelectedValue);
            DateTime ngayDi = dpNgayChuyen.SelectedDate!.Value.Date;
            var tDi = ParseTimeSpan(txtGioDiKH.Text)!.Value;
            var tDen = ParseTimeSpan(txtGioDenKH.Text)!.Value;

            decimal tgChuanGio = 12.0m;
            if (cboMacTau.SelectedItem is DataRowView rMac && decimal.TryParse(rMac["ThoiGianChuanGio"]?.ToString(), out decimal tg) && tg > 0)
            {
                tgChuanGio = tg;
            }

            DateTime dtDi = ngayDi.Add(tDi);
            DateTime dtDen = ChuyenTauService.TinhToanGioVeDich(ngayDi, tDi, tDen, tgChuanGio);

            string trangThai = string.IsNullOrEmpty(_currentTrangThai) ? "DA_LEN_LICH" : _currentTrangThai;

            int maDauMayChinh = Convert.ToInt32(cboDauMayChinh.SelectedValue);
            int? maDauMayDay = null;
            if (cboDauMayDay.SelectedValue != null && int.TryParse(cboDauMayDay.SelectedValue.ToString(), out int md) && md > 0)
            {
                maDauMayDay = md;
            }

            var ct = new ChuyenTau
            {
                MaMacTau = maMac,
                NgayXuatPhat = ngayDi,
                GioXuatPhatKH = dtDi,
                GioVeDichKH = dtDen,
                GioXuatPhatTT = _currentGioDiTT,
                GioVeDichTT = _currentGioDenTT,
                SoPhutTreLuyKe = _currentSoPhutTre,
                LyDoHuy = _currentLyDoHuy,
                TrangThai = trangThai
            };

            if (_isAddingNew)
            {
                TryParseDecimalInvariant(txtTongChieuDaiM.Text, out decimal chieuDai);
                if (chieuDai <= 0) chieuDai = 216.5m;
                TryParseDecimalInvariant(txtTongTrongLuongTan.Text, out decimal trongLuong);
                if (trongLuong <= 0) trongLuong = 478.0m;
                int soToa = dgToaXe.Items.Count > 0 ? dgToaXe.Items.Count : 1;

                var doanTau = new DoanTau
                {
                    MaChuyenTau = 0,
                    MaDauMayChinh = maDauMayChinh,
                    MaDauMayDay = maDauMayDay,
                    TongSoToa = soToa,
                    TongChieuDaiM = chieuDai,
                    TongTrongLuongTan = trongLuong,
                    DaDuyetAnToan = chkDaDuyetAnToan.IsChecked == true
                };

                int newId = _chuyenTauService.LapChuyenTauTronGoi(ct, _dtLichDungInMem ?? TaoDataTableLichDungRong(), doanTau, out string loiLapChuyen);
                if (newId > 0)
                {
                    _selectedMaChuyenTau = newId;
                    _isAddingNew = false;
                    _dtLichDungInMem = null;
                    _currentTrangThai = trangThai;

                    ThongBaoDialog.ThanhCong($"Đã lập lịch chuyến tàu #{newId} ({cboMacTau.Text}) thành công!", "Lập Chuyến Thành Công");
                    LoadData();
                    return true;
                }
                else
                {
                    string err = !string.IsNullOrWhiteSpace(loiLapChuyen) ? loiLapChuyen : "Không thể lập chuyến tàu trọn gói.";
                    ThongBaoDialog.Loi(err, "Lỗi CSDL");
                    return false;
                }
            }
            else
            {
                ct.MaChuyenTau = _selectedMaChuyenTau;
                _chuyenTauService.CapNhat(ct, out _);

                // Lưu cấu hình đoàn tàu & đầu máy kéo
                LuuThongTinDoanTau(_selectedMaChuyenTau, maDauMayChinh, maDauMayDay, out string loiDoanTau);

                _currentTrangThai = trangThai;
                ThongBaoDialog.ThanhCong("Đã cập nhật thông tin chuyến tàu thành công.", "Thông Báo");
                LoadData();
                return true;
            }
        }

        private bool LuuThongTinDoanTau(int maChuyenTau, int maDauMayChinh, int? maDauMayDay, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            try
            {
                TryParseDecimalInvariant(txtTongChieuDaiM.Text, out decimal chieuDai);
                if (chieuDai <= 0) chieuDai = 16.5m;
                TryParseDecimalInvariant(txtTongTrongLuongTan.Text, out decimal trongLuong);
                if (trongLuong <= 0) trongLuong = 78.0m;
                int soToa = dgToaXe.Items.Count > 0 ? dgToaXe.Items.Count : 1;

                var doanTau = new DoanTau
                {
                    MaChuyenTau = maChuyenTau,
                    MaDauMayChinh = maDauMayChinh,
                    MaDauMayDay = maDauMayDay,
                    TongSoToa = soToa,
                    TongChieuDaiM = chieuDai,
                    TongTrongLuongTan = trongLuong,
                    DaDuyetAnToan = chkDaDuyetAnToan.IsChecked == true
                };

                return _chuyenTauService.LuuDoanTau(doanTau, out thongBaoLoi);
            }
            catch (Exception ex)
            {
                thongBaoLoi = $"Ngoại lệ khi lưu đoàn tàu: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(thongBaoLoi);
                return false;
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = false;
            _dtLichDungInMem = null;
            if (pnlFormDiemDung != null) pnlFormDiemDung.Visibility = Visibility.Collapsed;
            _editingMaDiemDung = 0;
            LoadData();
        }

        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0) return;

            if (!ChuyenTauService.KiemTraCoTheXoaChuyen(_currentTrangThai, out string errXoa))
            {
                ThongBaoDialog.CanhBao(errXoa, "Ràng Buộc Vận Hành");
                return;
            }

            var r = ThongBaoDialog.XacNhan($"Xác nhận hủy chuyến tàu #{_selectedMaChuyenTau}?\n\nToàn bộ dữ liệu điểm dừng và biên chế toa xe liên quan cũng sẽ được xử lý.",
                "Xác Nhận Hủy Chuyến", nutDongY: "Hủy Chuyến Tàu", nutHuy: "Quay Lại", laHanhDongXoa: true);

            if (r)
            {
                if (_chuyenTauService.Xoa(_selectedMaChuyenTau, out string thongBaoLoi))
                {
                    ThongBaoDialog.ThanhCong("Đã xóa chuyến tàu thành công.", "Thông Báo");
                    _selectedMaChuyenTau = 0;
                    LoadData();
                }
                else
                {
                    string msg = !string.IsNullOrWhiteSpace(thongBaoLoi) ? thongBaoLoi : "Không thể xóa chuyến tàu này (có thể đã phát sinh giao dịch vé).";
                    ThongBaoDialog.CanhBao(msg, "Cảnh Báo An Toàn Dữ Liệu");
                }
            }
        }


        private void BtnNapLai_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = false;
            _dtLichDungInMem = null;
            if (pnlFormDiemDung != null) pnlFormDiemDung.Visibility = Visibility.Collapsed;
            _editingMaDiemDung = 0;
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

        // lich trình dừng ga

        private void BtnMoFormThemDung_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAddingNew && _selectedMaChuyenTau <= 0)
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn một chuyến tàu trước khi thêm điểm dừng.", "Thông Báo");
                return;
            }

            if (_currentTrangThai == "DANG_CHAY" || _currentTrangThai == "TRE_GIO" || _currentTrangThai == "HOAN_THANH" || _currentTrangThai == "DA_HUY")
            {
                ThongBaoDialog.CanhBao("Chuyến tàu đang vận hành thời gian thực hoặc đã kết thúc/hủy, không thể thêm điểm dừng mới!", "Khóa Chỉnh Sửa");
                return;
            }

            // Nạp danh sách các ga nằm giữa hành trình mác tàu và chưa có trong lịch trình
            bool coGaKhaDung = NapDanhSachGaChoDropdown();
            if (!coGaKhaDung)
            {
                ThongBaoDialog.ThongTin("Toàn bộ các ga trên hành trình của mác tàu này đều đã có trong danh sách điểm dừng!", "Không Còn Ga Mới");
                return;
            }

            _editingMaDiemDung = 0;
            pnlFormDiemDung.Visibility = Visibility.Visible;
            txtTieuDeFormDiemDung.Text = "KHAI BÁO THÊM ĐIỂM DỪNG MỚI";
            txtBtnLuuDiemDung.Text = "Lưu Điểm Dừng";
            cboThemDungGa.IsEnabled = true;

            txtThemGioDen.Text = "08:00";
            txtThemGioDi.Text = "08:05";
            chkThemLaTranh.IsChecked = false;
            CapNhatGoiYTheoGaChon();
        }

        private bool NapDanhSachGaChoDropdown()
        {
            if (_cachedGa == null || _cachedGa.Rows.Count == 0) return false;

            // 1. Thu thập các MaGa đã có trong lịch trình điểm dừng hiện tại của chuyến tàu
            var cacGaDaCo = new HashSet<int>();
            if (dgLichDungGa.ItemsSource is DataView dvLich)
            {
                foreach (DataRowView r in dvLich)
                {
                    if (r["MaGa"] != DBNull.Value && int.TryParse(r["MaGa"].ToString(), out int mg))
                    {
                        cacGaDaCo.Add(mg);
                    }
                }
            }

            // 2. Xác định phạm vi lý trình của mác tàu
            decimal kmMin = 0m;
            decimal kmMax = 2000m;
            bool chieuBacNam = true;
            bool coThongTinMac = false;

            if (cboMacTau.SelectedItem is DataRowView rMac)
            {
                int maGaDi = rMac["MaGaDi"] != DBNull.Value ? Convert.ToInt32(rMac["MaGaDi"]) : 0;
                int maGaDen = rMac["MaGaDen"] != DBNull.Value ? Convert.ToInt32(rMac["MaGaDen"]) : 0;

                decimal? kmDi = null;
                decimal? kmDen = null;

                if (rMac.Row.Table.Columns.Contains("KmGaDi") && rMac["KmGaDi"] != DBNull.Value)
                    kmDi = Convert.ToDecimal(rMac["KmGaDi"]);
                if (rMac.Row.Table.Columns.Contains("KmGaDen") && rMac["KmGaDen"] != DBNull.Value)
                    kmDen = Convert.ToDecimal(rMac["KmGaDen"]);

                if (!kmDi.HasValue || !kmDen.HasValue)
                {
                    foreach (DataRow r in _cachedGa.Rows)
                    {
                        int mg = Convert.ToInt32(r["MaGa"]);
                        if (mg == maGaDi && r["LyTrinhKm"] != DBNull.Value) kmDi = Convert.ToDecimal(r["LyTrinhKm"]);
                        if (mg == maGaDen && r["LyTrinhKm"] != DBNull.Value) kmDen = Convert.ToDecimal(r["LyTrinhKm"]);
                    }
                }

                if (kmDi.HasValue && kmDen.HasValue)
                {
                    coThongTinMac = true;
                    chieuBacNam = kmDen.Value >= kmDi.Value;
                    kmMin = Math.Min(kmDi.Value, kmDen.Value);
                    kmMax = Math.Max(kmDi.Value, kmDen.Value);
                }
            }

            // 3. Lọc danh sách ga
            var dtFiltered = _cachedGa.Clone();
            if (!dtFiltered.Columns.Contains("TenGaHienThi"))
            {
                dtFiltered.Columns.Add("TenGaHienThi", typeof(string));
            }

            var rows = _cachedGa.AsEnumerable()
                .Where(r =>
                {
                    int maGa = r.Field<int>("MaGa");
                    if (cacGaDaCo.Contains(maGa)) return false; // Đã có trên lịch trình chuyến này

                    if (coThongTinMac)
                    {
                        if (r["LyTrinhKm"] == DBNull.Value) return false;
                        decimal km = Convert.ToDecimal(r["LyTrinhKm"]);
                        if (km < kmMin || km > kmMax) return false; // Nằm ngoài hành trình mác tàu
                    }

                    return true;
                });

            // Sắp xếp theo đúng hướng chạy của tàu
            var sortedRows = chieuBacNam
                ? rows.OrderBy(r => r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m)
                : rows.OrderByDescending(r => r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m);

            foreach (var r in sortedRows)
            {
                var newRow = dtFiltered.NewRow();
                newRow.ItemArray = (object[])r.ItemArray.Clone();
                decimal km = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m;
                newRow["TenGaHienThi"] = $"{r["TenGa"]} (Km {km:F1})";
                dtFiltered.Rows.Add(newRow);
            }

            cboThemDungGa.DisplayMemberPath = "TenGaHienThi";
            cboThemDungGa.SelectedValuePath = "MaGa";
            cboThemDungGa.ItemsSource = dtFiltered.DefaultView;

            if (dtFiltered.Rows.Count > 0)
            {
                cboThemDungGa.SelectedIndex = 0;
                return true;
            }
            else
            {
                cboThemDungGa.ItemsSource = null;
                return false;
            }
        }

        private void CboThemDungGa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CapNhatGoiYTheoGaChon();
        }

        private void CapNhatGoiYTheoGaChon()
        {
            if (_editingMaDiemDung > 0) return;
            if (pnlFormDiemDung == null || pnlFormDiemDung.Visibility != Visibility.Visible) return;
            if (cboThemDungGa.SelectedItem is not DataRowView rGaChon) return;
            if (rGaChon["LyTrinhKm"] == DBNull.Value) return;

            decimal kmChon = Convert.ToDecimal(rGaChon["LyTrinhKm"]);
            if (dgLichDungGa.ItemsSource is not DataView dv || dv.Count == 0)
            {
                txtThemThuTu.Text = "1";
                if (txtThemThuTuHienThi != null) txtThemThuTuHienThi.Text = "#1 (Điểm đầu)";
                CapNhatTinhToanGioVaKhungHopLe();
                return;
            }

            // 1. Xác định chiều chạy tàu chuẩn từ mác tàu hoặc từ các ga hiện tại
            bool chieuBacNam = true;
            if (cboMacTau.SelectedItem is DataRowView rMac)
            {
                decimal? kmDi = rMac.Row.Table.Columns.Contains("KmGaDi") && rMac["KmGaDi"] != DBNull.Value ? Convert.ToDecimal(rMac["KmGaDi"]) : null;
                decimal? kmDen = rMac.Row.Table.Columns.Contains("KmGaDen") && rMac["KmGaDen"] != DBNull.Value ? Convert.ToDecimal(rMac["KmGaDen"]) : null;
                if (kmDi.HasValue && kmDen.HasValue)
                {
                    chieuBacNam = kmDen.Value >= kmDi.Value;
                }
            }
            else
            {
                decimal kmDau = dv[0]["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(dv[0]["LyTrinhKm"]) : 0m;
                decimal kmCuoi = dv[^1]["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(dv[^1]["LyTrinhKm"]) : kmDau;
                chieuBacNam = kmCuoi >= kmDau;
            }

            // 2. Tìm vị trí chèn lý trình phù hợp
            int suggestedThuTu = dv.Count + 1;
            DataRowView? rPrev = null;
            DataRowView? rNext = null;

            for (int i = 0; i < dv.Count; i++)
            {
                DataRowView r = dv[i];
                if (r["LyTrinhKm"] == DBNull.Value) continue;
                decimal km = Convert.ToDecimal(r["LyTrinhKm"]);

                bool isChenVaoDay = chieuBacNam ? (km > kmChon) : (km < kmChon);
                if (isChenVaoDay)
                {
                    suggestedThuTu = Convert.ToInt32(r["ThuTuDung"]);
                    rNext = r;
                    if (i > 0) rPrev = dv[i - 1];
                    break;
                }
                rPrev = r;
            }

            txtThemThuTu.Text = suggestedThuTu.ToString();
            if (txtThemThuTuHienThi != null)
            {
                if (rPrev != null && rNext != null)
                    txtThemThuTuHienThi.Text = $"#{suggestedThuTu} ({rPrev["TenGa"]} → {rNext["TenGa"]})";
                else if (rPrev == null && rNext != null)
                    txtThemThuTuHienThi.Text = $"#{suggestedThuTu} (Trước {rNext["TenGa"]})";
                else if (rNext == null && rPrev != null)
                    txtThemThuTuHienThi.Text = $"#{suggestedThuTu} (Sau {rPrev["TenGa"]})";
                else
                    txtThemThuTuHienThi.Text = $"#{suggestedThuTu} (Tự động)";
            }

            // 3. Tự động gợi ý giờ đến dựa vào ga liền trước và cự ly Km
            if (rPrev != null && rPrev["GioDiKeHoach"] != DBNull.Value && rPrev["LyTrinhKm"] != DBNull.Value)
            {
                DateTime dtDiPrev = Convert.ToDateTime(rPrev["GioDiKeHoach"]);
                decimal kmPrev = Convert.ToDecimal(rPrev["LyTrinhKm"]);
                decimal quangDuong = Math.Abs(kmChon - kmPrev);

                // Ước lượng vận tốc chạy tàu trung bình 60 km/h (tối thiểu 8 phút chạy)
                double phutChay = Math.Max(8.0, (double)quangDuong / 60.0 * 60.0);
                DateTime dtDenGoiY = dtDiPrev.AddMinutes(phutChay);

                txtThemGioDen.Text = dtDenGoiY.ToString("HH:mm");
            }
            else
            {
                var tXp = ParseTimeSpan(txtGioDiKH.Text);
                if (tXp.HasValue)
                {
                    txtThemGioDen.Text = $"{(int)tXp.Value.TotalHours % 24:D2}:{tXp.Value.Minutes:D2}";
                }
            }

            CapNhatTinhToanGioVaKhungHopLe();
        }

        private void CapNhatTinhToanGioVaKhungHopLe()
        {
            if (pnlFormDiemDung == null || pnlFormDiemDung.Visibility != Visibility.Visible) return;
            if (cboThemDungGa.SelectedItem is not DataRowView rGaChon) return;

            // 1. Phút đỗ
            int dwell = 5;
            if (int.TryParse(txtThemDwellMin?.Text?.Trim(), out int d) && d > 0)
            {
                dwell = d;
            }
            else
            {
                dwell = 5;
            }

            // 2. Tìm ga liền trước và ga liền sau dựa vào thứ tự hiện tại
            int thuTu = 1;
            int.TryParse(txtThemThuTu?.Text?.Trim(), out thuTu);

            DataRowView? rPrev = null;
            DataRowView? rNext = null;
            if (dgLichDungGa.ItemsSource is DataView dv)
            {
                foreach (DataRowView r in dv)
                {
                    int mdd = Convert.ToInt32(r["MaDiemDung"]);
                    if (_editingMaDiemDung > 0 && mdd == _editingMaDiemDung)
                        continue;

                    int tt = Convert.ToInt32(r["ThuTuDung"]);
                    if (tt < thuTu && (rPrev == null || tt > Convert.ToInt32(rPrev["ThuTuDung"])))
                        rPrev = r;
                    if (tt >= thuTu && (rNext == null || tt < Convert.ToInt32(rNext["ThuTuDung"])))
                        rNext = r;
                }
            }

            DateTime ngayDi = dpNgayChuyen.SelectedDate ?? DateTime.Today;
            var tXuatPhat = ParseTimeSpan(txtGioDiKH.Text);
            DateTime dtChuyenDi = tXuatPhat.HasValue ? ngayDi.Add(tXuatPhat.Value) : ngayDi.AddHours(6);

            DateTime dtMocThamChieu = rPrev != null ? Convert.ToDateTime(rPrev["GioDiKeHoach"]) : dtChuyenDi;

            // 3. Tính giờ đến và ngày đến cụ thể
            var tDen = ParseTimeSpan(txtThemGioDen?.Text);
            if (tDen.HasValue)
            {
                DateTime dtDen = dtMocThamChieu.Date.Add(tDen.Value);
                if (rPrev != null && dtDen < dtMocThamChieu)
                {
                    dtDen = dtDen.AddDays(1); // Chạy qua đêm
                }

                int soNgayQuaDem = (dtDen.Date - ngayDi.Date).Days;
                string quaDemStr = soNgayQuaDem > 0 ? $"+{soNgayQuaDem}N" : "Cùng ngày";
                if (txtThemNgayDenGoiY != null)
                {
                    txtThemNgayDenGoiY.Text = $"{dtDen:dd/MM} ({quaDemStr})";
                }

                // 4. Giờ đi = Giờ đến + Phút đỗ
                DateTime dtDi = dtDen.AddMinutes(dwell);

                if (txtThemGioDi != null)
                {
                    txtThemGioDi.Text = dtDi.ToString("HH:mm");
                }
                if (txtThemGioDiHienThi != null)
                {
                    txtThemGioDiHienThi.Text = $"{dtDi:HH:mm} ({dtDi:dd/MM})";
                }

                // 5. Tính khung giờ hợp lệ [Min - Max]
                DateTime dtMin = dtMocThamChieu.AddMinutes(5);
                DateTime? dtMax = null;

                if (rNext != null && rNext["GioDenKeHoach"] != DBNull.Value && rGaChon["LyTrinhKm"] != DBNull.Value && rNext["LyTrinhKm"] != DBNull.Value)
                {
                    DateTime dtNextDen = Convert.ToDateTime(rNext["GioDenKeHoach"]);
                    decimal kmCur = Convert.ToDecimal(rGaChon["LyTrinhKm"]);
                    decimal kmNext = Convert.ToDecimal(rNext["LyTrinhKm"]);
                    decimal qdNext = Math.Abs(kmNext - kmCur);
                    double phutChayNext = Math.Max(5.0, (double)qdNext / 80.0 * 60.0);
                    dtMax = dtNextDen.AddMinutes(-phutChayNext - dwell);
                }

                if (txtKhungGioHopLe != null && bdKhungGioHopLe != null)
                {
                    bool hopLe = dtDen >= dtMin && (!dtMax.HasValue || dtDen <= dtMax.Value);
                    string minStr = $"{dtMin:HH:mm (dd/MM)}";
                    string maxStr = dtMax.HasValue ? $"{dtMax.Value:HH:mm (dd/MM)}" : "Không giới hạn";

                    string tenGaTruoc = rPrev != null ? (rPrev["TenGa"]?.ToString() ?? "Ga trước") : "Ga đầu";
                    string tenGaSau = rNext != null ? (rNext["TenGa"]?.ToString() ?? "Ga sau") : "";

                    if (hopLe)
                    {
                        bdKhungGioHopLe.Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
                        bdKhungGioHopLe.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
                        txtKhungGioHopLe.Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x40, 0xAF));
                        if (rNext != null && dtMax.HasValue)
                        {
                            txtKhungGioHopLe.Text = $"Khung giờ an toàn (để không trễ {tenGaSau}): {minStr} — {maxStr}";
                        }
                        else
                        {
                            txtKhungGioHopLe.Text = $"Khung giờ hợp lệ sau {tenGaTruoc}: từ {minStr} trở đi";
                        }
                    }
                    else
                    {
                        bdKhungGioHopLe.Background = new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));
                        bdKhungGioHopLe.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFE, 0xCA, 0xCA));
                        txtKhungGioHopLe.Foreground = new SolidColorBrush(Color.FromRgb(0xB9, 0x1C, 0x1C));
                        if (dtDen < dtMin)
                            txtKhungGioHopLe.Text = $"Cảnh báo: Giờ đến ({dtDen:HH:mm dd/MM}) quá sớm so với ga trước ({tenGaTruoc} lúc {dtMocThamChieu:HH:mm dd/MM}).";
                        else
                            txtKhungGioHopLe.Text = $"Cảnh báo: Giờ đến ({dtDen:HH:mm dd/MM}) quá muộn để kịp ga kế tiếp ({tenGaSau} lúc {maxStr}).";
                    }
                }
            }
        }

        private void TxtThemGioDen_TextChanged(object sender, TextChangedEventArgs e)
        {
            CapNhatTinhToanGioVaKhungHopLe();
        }

        private void TxtThemDwellMin_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Chỉ cho phép nhập ký tự số (0-9)
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void TxtThemDwellMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtThemDwellMin == null) return;
            string raw = txtThemDwellMin.Text;
            string digitsOnly = Regex.Replace(raw, @"[^\d]", "");
            if (digitsOnly.Length > 3)
            {
                digitsOnly = digitsOnly.Substring(0, 3);
            }
            if (raw != digitsOnly)
            {
                txtThemDwellMin.Text = digitsOnly;
                txtThemDwellMin.CaretIndex = digitsOnly.Length;
                return;
            }
            CapNhatTinhToanGioVaKhungHopLe();
        }

        private void TxtThemDwellMin_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtThemDwellMin.Text.Trim(), out int val))
            {
                if (val < 0) val = 0;
                if (val > 999) val = 999;
                if (chkThemLaTranh.IsChecked == true && val < 5) val = 5;
                txtThemDwellMin.Text = val.ToString();
            }
            else
            {
                txtThemDwellMin.Text = chkThemLaTranh.IsChecked == true ? "5" : "2";
            }
            CapNhatTinhToanGioVaKhungHopLe();
        }

        private void ChkThemLaTranh_Changed(object sender, RoutedEventArgs e)
        {
            if (chkThemLaTranh.IsChecked == true)
            {
                if (int.TryParse(txtThemDwellMin.Text.Trim(), out int d) && d < 5)
                {
                    txtThemDwellMin.Text = "5";
                }
            }
            CapNhatTinhToanGioVaKhungHopLe();
        }

        private void BtnChonDwell_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                txtThemDwellMin.Text = btn.Tag.ToString();
            }
        }

        private void BtnSapXepTheoKm_Click(object sender, RoutedEventArgs e)
        {
            if (_isAddingNew)
            {
                if (_dtLichDungInMem == null || _dtLichDungInMem.Rows.Count <= 1)
                {
                    ThongBaoDialog.ThongTin("Lộ trình chưa có đủ điểm dừng để thực hiện sắp xếp.", "Thông Báo");
                    return;
                }

                bool chieuBacNamInMem = true;
                if (cboMacTau.SelectedItem is DataRowView rMacInMem)
                {
                    decimal? kmDi = rMacInMem.Row.Table.Columns.Contains("KmGaDi") && rMacInMem["KmGaDi"] != DBNull.Value ? Convert.ToDecimal(rMacInMem["KmGaDi"]) : null;
                    decimal? kmDen = rMacInMem.Row.Table.Columns.Contains("KmGaDen") && rMacInMem["KmGaDen"] != DBNull.Value ? Convert.ToDecimal(rMacInMem["KmGaDen"]) : null;
                    if (kmDi.HasValue && kmDen.HasValue) chieuBacNamInMem = kmDen.Value >= kmDi.Value;
                }

                var rowsInMem = _dtLichDungInMem.AsEnumerable();
                var sortedRowsInMem = (chieuBacNamInMem
                    ? rowsInMem.OrderBy(r => Convert.ToDecimal(r["LyTrinhKm"]))
                    : rowsInMem.OrderByDescending(r => Convert.ToDecimal(r["LyTrinhKm"]))).ToList();

                var dtMoi = _dtLichDungInMem.Clone();
                int newThuTu = 1;
                foreach (var row in sortedRowsInMem)
                {
                    row["ThuTuDung"] = newThuTu++;
                    dtMoi.ImportRow(row);
                }
                _dtLichDungInMem = dtMoi;
                CapNhatThongKeLichDungInMem();
                ThongBaoDialog.ThanhCong($"Đã sắp xếp lại các điểm dừng theo Lý trình Km (hướng {(chieuBacNamInMem ? "Bắc -> Nam" : "Nam -> Bắc")})!", "Sắp Xếp Thành Công");
                return;
            }

            if (_selectedMaChuyenTau <= 0)
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn một chuyến tàu trước khi thực hiện sắp xếp điểm dừng.", "Chưa Chọn Chuyến Tàu");
                return;
            }

            if (_currentTrangThai == "DANG_CHAY" || _currentTrangThai == "TRE_GIO" || _currentTrangThai == "HOAN_THANH" || _currentTrangThai == "DA_HUY")
            {
                ThongBaoDialog.CanhBao("Chuyến tàu đang vận hành thời gian thực hoặc đã kết thúc/hủy, không thể thay đổi thứ tự và lịch ga!", "Khóa Chỉnh Sửa");
                return;
            }

            // Xác định chiều chạy chuẩn của mác tàu
            bool chieuBacNam = true;
            if (cboMacTau.SelectedItem is DataRowView rMac)
            {
                decimal? kmDi = rMac.Row.Table.Columns.Contains("KmGaDi") && rMac["KmGaDi"] != DBNull.Value ? Convert.ToDecimal(rMac["KmGaDi"]) : null;
                decimal? kmDen = rMac.Row.Table.Columns.Contains("KmGaDen") && rMac["KmGaDen"] != DBNull.Value ? Convert.ToDecimal(rMac["KmGaDen"]) : null;
                if (kmDi.HasValue && kmDen.HasValue)
                {
                    chieuBacNam = kmDen.Value >= kmDi.Value;
                }
            }
            else if (dgLichDungGa.ItemsSource is DataView dv && dv.Count >= 2)
            {
                decimal kmDau = dv[0]["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(dv[0]["LyTrinhKm"]) : 0m;
                decimal kmCuoi = dv[^1]["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(dv[^1]["LyTrinhKm"]) : kmDau;
                chieuBacNam = kmCuoi >= kmDau;
            }

            try
            {
                bool thanhCong = _chuyenTauService.SapXepLichDungTheoKm(_selectedMaChuyenTau, chieuBacNam, out int soDiemDungThayDoi);
                if (thanhCong)
                {
                    if (soDiemDungThayDoi > 0)
                    {
                        LoadLichDungGa(_selectedMaChuyenTau);
                        LoadData();
                        ThongBaoDialog.ThanhCong(
                            $"Đã chuẩn hóa và sắp xếp lại {soDiemDungThayDoi} điểm dừng theo đúng Lý trình Km (hướng {(chieuBacNam ? "Bắc -> Nam (Km tăng dần)" : "Nam -> Bắc (Km giảm dần)")})!\n\nVui lòng kiểm tra lại giờ đến và giờ đi của các ga được điều chỉnh vị trí.",
                            "Sắp Xếp Thành Công");
                    }
                    else
                    {
                        ThongBaoDialog.ThongTin(
                            "Toàn bộ các điểm dừng hiện tại đã đúng thứ tự theo Lý trình Km của hành trình, không cần điều chỉnh.",
                            "Lộ Trình Đã Chuẩn");
                    }
                }
                else
                {
                    ThongBaoDialog.CanhBao("Không thể sắp xếp lại điểm dừng. Vui lòng thử lại sau.", "Lỗi Sắp Xếp");
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi khi sắp xếp lại điểm dừng: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void BtnDongFormThemDung_Click(object sender, RoutedEventArgs e)
        {
            pnlFormDiemDung.Visibility = Visibility.Collapsed;
            _editingMaDiemDung = 0;
        }

        private void MoFormSuaDiemDung(DataRowView row)
        {
            if (_currentTrangThai == "DANG_CHAY" || _currentTrangThai == "TRE_GIO" || _currentTrangThai == "HOAN_THANH" || _currentTrangThai == "DA_HUY")
            {
                ThongBaoDialog.CanhBao("Chuyến tàu đang vận hành thời gian thực hoặc đã kết thúc/hủy, không thể chỉnh sửa điểm dừng!", "Khóa Chỉnh Sửa");
                return;
            }

            _editingMaDiemDung = Convert.ToInt32(row["MaDiemDung"]);
            pnlFormDiemDung.Visibility = Visibility.Visible;
            string tenGa = row["TenGa"]?.ToString() ?? "";
            string thuTu = row["ThuTuDung"]?.ToString() ?? "1";

            txtTieuDeFormDiemDung.Text = $"CHỈNH SỬA ĐIỂM DỪNG: {tenGa} (#{thuTu})";
            txtBtnLuuDiemDung.Text = "Cập Nhật Điểm Dừng";

            // Đảm bảo cboThemDungGa có item để hiển thị tên ga
            cboThemDungGa.DisplayMemberPath = "TenGa";
            cboThemDungGa.SelectedValuePath = "MaGa";
            if (_cachedGa != null) cboThemDungGa.ItemsSource = _cachedGa.DefaultView;
            cboThemDungGa.SelectedValue = Convert.ToInt32(row["MaGa"]);
            cboThemDungGa.IsEnabled = false; // Khóa không cho đổi ga, chỉ sửa giờ/tác nghiệp

            txtThemThuTu.Text = thuTu;
            if (txtThemThuTuHienThi != null)
            {
                txtThemThuTuHienThi.Text = $"#{thuTu} (Cố định)";
            }

            if (row["GioDenKeHoach"] != DBNull.Value)
                txtThemGioDen.Text = Convert.ToDateTime(row["GioDenKeHoach"]).ToString("HH:mm");
            if (row["GioDiKeHoach"] != DBNull.Value)
                txtThemGioDi.Text = Convert.ToDateTime(row["GioDiKeHoach"]).ToString("HH:mm");

            if (row["GioDenKeHoach"] != DBNull.Value && row["GioDiKeHoach"] != DBNull.Value)
            {
                var dtDenRow = Convert.ToDateTime(row["GioDenKeHoach"]);
                var dtDiRow = Convert.ToDateTime(row["GioDiKeHoach"]);
                int dwellRow = (int)Math.Max(1, (dtDiRow - dtDenRow).TotalMinutes);
                txtThemDwellMin.Text = dwellRow.ToString();
            }
            else
            {
                txtThemDwellMin.Text = "5";
            }

            chkThemLaTranh.IsChecked = row["LaDiemTranh"] != DBNull.Value && (Convert.ToBoolean(row["LaDiemTranh"]) || row["LaDiemTranh"].ToString() == "1");
            CapNhatTinhToanGioVaKhungHopLe();
            txtThemGioDen.Focus();
        }

        private void BtnSuaDiemDung_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                MoFormSuaDiemDung(row);
            }
        }

        private void DgLichDungGa_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgLichDungGa.SelectedItem is DataRowView row)
            {
                MoFormSuaDiemDung(row);
            }
        }

        private void BtnTangDwell5P_Click(object sender, RoutedEventArgs e)
        {
            int dwell = int.TryParse(txtThemDwellMin.Text.Trim(), out int d) ? d : 5;
            int newVal = Math.Min(999, dwell + 5);
            txtThemDwellMin.Text = newVal.ToString();
        }

        private void BtnGiamDwell5P_Click(object sender, RoutedEventArgs e)
        {
            int dwell = int.TryParse(txtThemDwellMin.Text.Trim(), out int d) ? d : 5;
            int minDwell = chkThemLaTranh.IsChecked == true ? 5 : 0;
            txtThemDwellMin.Text = Math.Max(minDwell, dwell - 5).ToString();
        }

        private void BtnLuuThemDiemDung_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAddingNew && _selectedMaChuyenTau <= 0) return;

            try
            {
                if (cboThemDungGa.SelectedValue == null)
                {
                    ThongBaoDialog.CanhBao("Vui lòng chọn ga dừng.", "Thiếu Thông Tin");
                    return;
                }

                if (!int.TryParse(txtThemThuTu.Text.Trim(), out int thuTu) || thuTu <= 0)
                {
                    ThongBaoDialog.CanhBao("Thứ tự dừng phải là số nguyên dương (>= 1).", "Cảnh Báo Nhập Liệu");
                    return;
                }

                if (!int.TryParse(txtThemDwellMin.Text.Trim(), out int dwellMin) || dwellMin < 0 || dwellMin > 999)
                {
                    ThongBaoDialog.CanhBao("Thời gian đỗ ga phải là số nguyên dương từ 0 đến 999 phút (tối đa 3 chữ số).", "Cảnh Báo Nhập Liệu");
                    txtThemDwellMin.Focus();
                    return;
                }

                int maGaChon = Convert.ToInt32(cboThemDungGa.SelectedValue);
                bool canDayThuTu = false;
                if (_editingMaDiemDung <= 0 && dgLichDungGa.ItemsSource is DataView dvLich)
                {
                    foreach (DataRowView r in dvLich)
                    {
                        if (Convert.ToInt32(r["MaGa"]) == maGaChon)
                        {
                            ThongBaoDialog.CanhBao($"Ga [{r["TenGa"]}] đã có trong lịch trình điểm dừng của chuyến tàu này!", "Trùng Ga Dừng");
                            return;
                        }
                        if (Convert.ToInt32(r["ThuTuDung"]) == thuTu)
                        {
                            // Đánh dấu cần đẩy thứ tự dừng, nhưng CHƯA commit vào DB cho tới khi validate hoàn tất
                            canDayThuTu = true;
                        }
                    }
                }

                var tDen = ParseTimeSpan(txtThemGioDen.Text);
                var tDi = ParseTimeSpan(txtThemGioDi.Text);

                if (!tDen.HasValue || !tDi.HasValue)
                {
                    ThongBaoDialog.CanhBao("Vui lòng nhập đầy đủ Giờ đến và Giờ đi hợp lệ cho điểm dừng ga (định dạng HH:mm, ví dụ: 08:00 và 08:05).", "Thiếu Thông Tin");
                    return;
                }

                DateTime ngayDi = dpNgayChuyen.SelectedDate ?? DateTime.Today;
                var tXuatPhat = ParseTimeSpan(txtGioDiKH.Text);
                DateTime dtChuyenDi = tXuatPhat.HasValue ? ngayDi.Add(tXuatPhat.Value) : ngayDi.AddHours(6);

                // Tìm ga liền trước (thứ tự lớn nhất < thuTu) và ga liền sau (thứ tự nhỏ nhất > thuTu), bỏ qua điểm dừng đang sửa
                DataRowView? rPrev = null;
                DataRowView? rNext = null;
                if (dgLichDungGa.ItemsSource is DataView dv)
                {
                    foreach (DataRowView r in dv)
                    {
                        int mdd = Convert.ToInt32(r["MaDiemDung"]);
                        if (_editingMaDiemDung > 0 && mdd == _editingMaDiemDung)
                            continue;

                        int tt = Convert.ToInt32(r["ThuTuDung"]);
                        if (tt < thuTu && (rPrev == null || tt > Convert.ToInt32(rPrev["ThuTuDung"])))
                            rPrev = r;
                        if (tt > thuTu && (rNext == null || tt < Convert.ToInt32(rNext["ThuTuDung"])))
                            rNext = r;
                    }
                }

                // Mốc tham chiếu thời gian là giờ rời ga liền trước (nếu có), hoặc giờ xuất phát của chuyến
                DateTime dtMocThamChieu = rPrev != null ? Convert.ToDateTime(rPrev["GioDiKeHoach"]) : dtChuyenDi;

                DateTime dtDen = dtMocThamChieu.Date.Add(tDen.Value);
                if (rPrev != null)
                {
                    // Nếu tDen nhỏ hơn giờ rời ga trước (vd: ga trước đi 22:00, ga này đến 00:40): tàu chạy qua nửa đêm
                    if (dtDen < dtMocThamChieu)
                    {
                        dtDen = dtDen.AddDays(1);
                    }

                    // Kiểm tra nghiêm ngặt: Giờ đến phải lớn hơn giờ rời ga trước
                    if (dtDen <= dtMocThamChieu)
                    {
                        ThongBaoDialog.CanhBao($"Giờ đến tại ga ({dtDen:HH:mm dd/MM}) phải sau giờ rời ga #{rPrev["ThuTuDung"]} ({rPrev["TenGa"]} lúc {dtMocThamChieu:HH:mm dd/MM})!\n\nCần có thời gian chạy tàu hợp lý giữa hai ga liên tiếp.",
                            "Xung Đột Thứ Tự Thời Gian");
                        return;
                    }
                }

                DateTime dtDi = dtDen.Date.Add(tDi.Value);
                if (dtDi < dtDen)
                {
                    dtDi = dtDi.AddDays(1); // Tàu dừng đỗ qua nửa đêm (vd: đến 23:55, đi 00:05)
                }

                if (dtDi < dtDen)
                {
                    ThongBaoDialog.CanhBao($"Giờ rời ga ({dtDi:HH:mm dd/MM}) không được trước giờ đến ga ({dtDen:HH:mm dd/MM})!",
                        "Lỗi Thời Gian Đỗ Ga");
                    return;
                }

                // Kiểm tra tính đơn điệu và thời gian chạy đến ga kế tiếp (nếu có)
                int soPhutTreCanDoi = 0;
                if (rNext != null && cboThemDungGa.SelectedItem is DataRowView rGaLuu)
                {
                    DateTime dtNextDen = Convert.ToDateTime(rNext["GioDenKeHoach"]);
                    decimal kmCur = Convert.ToDecimal(rGaLuu["LyTrinhKm"]);
                    decimal kmNext = Convert.ToDecimal(rNext["LyTrinhKm"]);
                    decimal qdNext = Math.Abs(kmNext - kmCur);
                    double phutChayToiThieu = Math.Max(5.0, (double)qdNext / 80.0 * 60.0);
                    DateTime dtSomNhatDenGaSau = dtDi.AddMinutes(phutChayToiThieu);

                    if (dtSomNhatDenGaSau > dtNextDen)
                    {
                        int soPhutTre = (int)Math.Ceiling((dtSomNhatDenGaSau - dtNextDen).TotalMinutes);
                        bool dongYDoi = ThongBaoDialog.XacNhan(
                            $"Ga [{rGaLuu["TenGa"]}] khởi hành lúc {dtDi:HH:mm dd/MM} làm tàu không thể đến ga kế tiếp [{rNext["TenGa"]}] lúc {dtNextDen:HH:mm dd/MM} theo lịch cũ (cần ít nhất {phutChayToiThieu:F0} phút chạy tàu, bị trễ khoảng {soPhutTre} phút).\n\nBạn có muốn hệ thống TỰ ĐỘNG DỜI LỊCH (tịnh tiến thêm +{soPhutTre} phút) cho toàn bộ các ga từ [{rNext["TenGa"]}] về sau không?",
                            "Xác Nhận Điều Chỉnh Lịch Trình Sau",
                            nutDongY: "Tự Động Dời Giờ",
                            nutHuy: "Chỉnh Lại Giờ");

                        if (dongYDoi)
                        {
                            soPhutTreCanDoi = soPhutTre;
                        }
                        else
                        {
                            return;
                        }
                    }
                }

                // Kiểm tra cảnh báo vận tốc kỹ thuật tối đa giữa 2 ga liên tiếp (ngưỡng 120 km/h)
                if (rPrev != null && cboThemDungGa.SelectedItem is DataRowView rGaChon)
                {
                    if (rPrev["LyTrinhKm"] != DBNull.Value && rGaChon["LyTrinhKm"] != DBNull.Value)
                    {
                        decimal kmPrev = Convert.ToDecimal(rPrev["LyTrinhKm"]);
                        decimal kmCur = Convert.ToDecimal(rGaChon["LyTrinhKm"]);
                        decimal quangDuong = Math.Abs(kmCur - kmPrev);
                        double thoiGianPhut = (dtDen - dtMocThamChieu).TotalMinutes;

                        if (quangDuong > 0 && thoiGianPhut > 0)
                        {
                            double vanToc = (double)quangDuong / (thoiGianPhut / 60.0);
                            if (vanToc > 120.0)
                            {
                                bool tiepTuc = ThongBaoDialog.XacNhan(
                                    $"Cảnh báo vận tốc: Tàu chạy từ ga trước ({rPrev["TenGa"]}) đến ga này ({rGaChon["TenGa"]}) mất {thoiGianPhut:F0} phút cho {quangDuong:F1} km (vận tốc trung bình ~{vanToc:F0} km/h, vượt quá giới hạn an toàn tối đa 120 km/h).\n\nBạn có muốn tiếp tục lưu điểm dừng này?",
                                    "Cảnh Báo Vận Tốc Kỹ Thuật",
                                    nutDongY: "Vẫn Lưu",
                                    nutHuy: "Chỉnh Lại");
                                if (!tiepTuc) return;
                            }
                        }
                    }
                }

                // === TẤT CẢ VALIDATION ĐÃ ĐẠT ===
                if (_isAddingNew)
                {
                    if (_dtLichDungInMem == null) _dtLichDungInMem = TaoDataTableLichDungRong();

                    // 1. Tịnh tiến giờ các ga phía sau (nếu có yêu cầu dời giờ)
                    if (soPhutTreCanDoi > 0)
                    {
                        foreach (DataRow r in _dtLichDungInMem.Rows)
                        {
                            int tt = Convert.ToInt32(r["ThuTuDung"]);
                            if (tt >= thuTu)
                            {
                                if (r["GioDenKeHoach"] != DBNull.Value)
                                    r["GioDenKeHoach"] = Convert.ToDateTime(r["GioDenKeHoach"]).AddMinutes(soPhutTreCanDoi);
                                if (r["GioDiKeHoach"] != DBNull.Value)
                                    r["GioDiKeHoach"] = Convert.ToDateTime(r["GioDiKeHoach"]).AddMinutes(soPhutTreCanDoi);
                            }
                        }
                    }

                    // 2. Nếu cần đẩy thứ tự
                    if (canDayThuTu)
                    {
                        foreach (DataRow r in _dtLichDungInMem.Rows)
                        {
                            int tt = Convert.ToInt32(r["ThuTuDung"]);
                            if (tt >= thuTu)
                            {
                                r["ThuTuDung"] = tt + 1;
                            }
                        }
                    }

                    // 3. Sửa hoặc Thêm dòng
                    if (_editingMaDiemDung != 0)
                    {
                        DataRow? rowEdit = null;
                        foreach (DataRow r in _dtLichDungInMem.Rows)
                        {
                            if (Convert.ToInt32(r["MaDiemDung"]) == _editingMaDiemDung)
                            {
                                rowEdit = r;
                                break;
                            }
                        }
                        if (rowEdit != null)
                        {
                            rowEdit["GioDenKeHoach"] = dtDen;
                            rowEdit["GioDiKeHoach"] = dtDi;
                            rowEdit["LaDiemTranh"] = chkThemLaTranh.IsChecked == true;
                            rowEdit["ThoiGianDungPhut"] = (int)Math.Max(0, (dtDi - dtDen).TotalMinutes);
                        }
                    }
                    else
                    {
                        DataRow rNew = _dtLichDungInMem.NewRow();
                        rNew["MaDiemDung"] = --_tempIdDiemDung;
                        rNew["MaChuyenTau"] = 0;
                        rNew["MaGa"] = maGaChon;
                        rNew["ThuTuDung"] = thuTu;
                        rNew["GioDenKeHoach"] = dtDen;
                        rNew["GioDiKeHoach"] = dtDi;
                        rNew["LaDiemTranh"] = chkThemLaTranh.IsChecked == true;
                        rNew["ThoiGianDungPhut"] = (int)Math.Max(0, (dtDi - dtDen).TotalMinutes);

                        if (cboThemDungGa.SelectedItem is DataRowView rG)
                        {
                            rNew["TenGa"] = rG["TenGa"]?.ToString() ?? "";
                            rNew["MaGaCode"] = rG.Row.Table.Columns.Contains("MaGaCode") ? rG["MaGaCode"]?.ToString() ?? "" : "";
                            rNew["LyTrinhKm"] = rG["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(rG["LyTrinhKm"]) : 0m;
                        }

                        _dtLichDungInMem.Rows.Add(rNew);
                    }

                    // Sắp xếp lại _dtLichDungInMem theo ThuTuDung và chuẩn hóa thứ tự
                    var sorted = _dtLichDungInMem.AsEnumerable().OrderBy(r => Convert.ToInt32(r["ThuTuDung"])).ToList();
                    var dtSorted = _dtLichDungInMem.Clone();
                    int sttMoi = 1;
                    foreach (var r in sorted)
                    {
                        r["ThuTuDung"] = sttMoi++;
                        dtSorted.ImportRow(r);
                    }
                    _dtLichDungInMem = dtSorted;

                    pnlFormDiemDung.Visibility = Visibility.Collapsed;
                    _editingMaDiemDung = 0;
                    CapNhatThongKeLichDungInMem();
                    return;
                }

                // === CHẾ ĐỘ XEM/SỬA: GHI TRỰC TIẾP VÀO CSDL ===
                if (soPhutTreCanDoi > 0)
                {
                    _chuyenTauService.TinhTienGioCacGaPhiaSau(_selectedMaChuyenTau, thuTu, soPhutTreCanDoi);
                }

                if (canDayThuTu)
                {
                    _chuyenTauService.DayThuTuDung(_selectedMaChuyenTau, thuTu);
                }

                var ld = new LichDungGa
                {
                    MaDiemDung = _editingMaDiemDung,
                    MaChuyenTau = _selectedMaChuyenTau,
                    MaGa = maGaChon,
                    ThuTuDung = thuTu,
                    GioDenKeHoach = dtDen,
                    GioDiKeHoach = dtDi,
                    LaDiemTranh = chkThemLaTranh.IsChecked == true
                };

                bool thanhCong;
                string thongBaoLoi;

                if (_editingMaDiemDung > 0)
                {
                    thanhCong = _chuyenTauService.CapNhatLichDungGa(ld, out thongBaoLoi);
                }
                else
                {
                    thanhCong = _chuyenTauService.ThemLichDungGa(ld, out thongBaoLoi);
                }

                if (thanhCong)
                {
                    pnlFormDiemDung.Visibility = Visibility.Collapsed;
                    _editingMaDiemDung = 0;
                    LoadLichDungGa(_selectedMaChuyenTau);
                    if (TryParseDecimalInvariant(txtTongChieuDaiM.Text, out decimal cd))
                    {
                        KiemTraChieuDaiVoiDuongTranhGa(_selectedMaChuyenTau, cd);
                    }
                }
                else
                {
                    string msg = !string.IsNullOrWhiteSpace(thongBaoLoi) ? thongBaoLoi : "Không thể lưu điểm dừng (trùng thứ tự hoặc vi phạm dữ liệu).";
                    ThongBaoDialog.CanhBao(msg, "Lỗi Ràng Buộc Điểm Dừng");
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi trong quá trình xử lý điểm dừng: {ex.Message}", "Lỗi Hệ Thống");
            }
        }

        private void DgLichDungGa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void BtnXoaDiemDung_Click(object sender, RoutedEventArgs e)
        {
            if (_isAddingNew)
            {
                if (sender is Button btnInMem && btnInMem.DataContext is DataRowView rowInMem)
                {
                    int maDiemDung = Convert.ToInt32(rowInMem["MaDiemDung"]);
                    string tenGa = rowInMem["TenGa"]?.ToString() ?? "";

                    var r = ThongBaoDialog.XacNhan($"Bạn có chắc chắn muốn xóa ga '{tenGa}' khỏi lịch trình dừng của chuyến tàu này không?", 
                                            "Xác Nhận Xóa Điểm Dừng", nutDongY: "Xóa Điểm Dừng", nutHuy: "Quay Lại", laHanhDongXoa: true);
                    if (r && _dtLichDungInMem != null)
                    {
                        DataRow? toRemove = null;
                        foreach (DataRow dr in _dtLichDungInMem.Rows)
                        {
                            if (Convert.ToInt32(dr["MaDiemDung"]) == maDiemDung)
                            {
                                toRemove = dr;
                                break;
                            }
                        }
                        if (toRemove != null)
                        {
                            _dtLichDungInMem.Rows.Remove(toRemove);
                            int stt = 1;
                            foreach (DataRow dr in _dtLichDungInMem.Rows)
                            {
                                dr["ThuTuDung"] = stt++;
                            }
                            CapNhatThongKeLichDungInMem();
                        }
                    }
                }
                return;
            }

            if (_currentTrangThai == "DANG_CHAY" || _currentTrangThai == "TRE_GIO" || _currentTrangThai == "HOAN_THANH" || _currentTrangThai == "DA_HUY")
            {
                ThongBaoDialog.CanhBao("Chuyến tàu đang vận hành thời gian thực hoặc đã kết thúc/hủy, không thể xóa điểm dừng!", "Khóa Chỉnh Sửa");
                return;
            }

            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                int maDiemDung = Convert.ToInt32(row["MaDiemDung"]);
                string tenGa = row["TenGa"]?.ToString() ?? "";

                var r = ThongBaoDialog.XacNhan($"Bạn có chắc chắn muốn xóa ga '{tenGa}' khỏi lịch trình dừng của chuyến tàu này không?\n\nThao tác này sẽ hủy điểm dừng và cập nhật lại tiến trình chạy tàu.", 
                                        "Xác Nhận Xóa Điểm Dừng", nutDongY: "Xóa Điểm Dừng", nutHuy: "Quay Lại", laHanhDongXoa: true);
                if (r)
                {
                    if (_chuyenTauService.XoaLichDungGa(maDiemDung))
                    {
                        LoadLichDungGa(_selectedMaChuyenTau);
                        if (TryParseDecimalInvariant(txtTongChieuDaiM.Text, out decimal cd))
                        {
                            KiemTraChieuDaiVoiDuongTranhGa(_selectedMaChuyenTau, cd);
                        }
                    }
                    else
                    {
                        ThongBaoDialog.Loi("Không thể xóa điểm dừng.", "Lỗi CSDL");
                    }
                }
            }
        }

        private void BtnSaoChepMau_Click(object sender, RoutedEventArgs e)
        {
            if (_isAddingNew)
            {
                if (cboMacTau.SelectedValue == null)
                {
                    ThongBaoDialog.ThongTin("Vui lòng chọn mác tàu trước khi sao chép mẫu tuyến.", "Thông Báo");
                    return;
                }

                int soDiemDungHienTaiInMem = _dtLichDungInMem?.Rows.Count ?? 0;
                if (soDiemDungHienTaiInMem > 0)
                {
                    bool xn = ThongBaoDialog.XacNhan($"Hành trình hiện tại đã có {soDiemDungHienTaiInMem} ga dừng.\nBạn có muốn THAY THẾ bằng lộ trình mẫu chuẩn của mác tàu này không?",
                        "Xác Nhận Sao Chép Mẫu", nutDongY: "Đồng Ý Thay Thế", nutHuy: "Bỏ Qua");
                    if (!xn) return;
                }

                if (NapMauTuyenChoInMem())
                {
                    ThongBaoDialog.ThanhCong($"Đã nạp thành công {_dtLichDungInMem?.Rows.Count} ga dừng từ chuyến mẫu!", "Sao Chép Mẫu Tuyến");
                }
                else
                {
                    ThongBaoDialog.ThongTin("Chưa tìm thấy chuyến tàu mẫu nào có dữ liệu điểm dừng của mác tàu này.", "Thông Báo");
                }
                return;
            }

            if (_selectedMaChuyenTau <= 0 || cboMacTau.SelectedValue == null)
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn chuyến tàu hợp lệ.", "Thông Báo");
                return;
            }

            if (_currentTrangThai == "DANG_CHAY" || _currentTrangThai == "TRE_GIO" || _currentTrangThai == "HOAN_THANH" || _currentTrangThai == "DA_HUY")
            {
                ThongBaoDialog.CanhBao("Chuyến tàu đang vận hành thời gian thực hoặc đã kết thúc/hủy, không thể sao chép đè lộ trình!", "Khóa Chỉnh Sửa");
                return;
            }

            int maMac = Convert.ToInt32(cboMacTau.SelectedValue);
            DateTime ngayDi = dpNgayChuyen.SelectedDate ?? DateTime.Today;

            int soDiemDungHienTai = (dgLichDungGa.ItemsSource is DataView dv) ? dv.Count : dgLichDungGa.Items.Count;
            string thongBao = soDiemDungHienTai > 0
                ? $"Chuyến tàu này hiện đã có {soDiemDungHienTai} điểm dừng.\nViệc sao chép từ chuyến mẫu sẽ THAY THẾ toàn bộ các điểm dừng hiện tại bằng dữ liệu chuẩn của mác tàu.\n\nBạn có chắc chắn muốn thực hiện?"
                : "Hệ thống sẽ sao chép danh sách ga dừng và khoảng thời gian tiêu chuẩn từ chuyến gần nhất của mác tàu này.\n\nTiếp tục?";

            var r = ThongBaoDialog.XacNhan(thongBao, "Sao Chép Mẫu Tuyến", nutDongY: "Đồng Ý Sao Chép", nutHuy: "Bỏ Qua");

            if (r)
            {
                if (_chuyenTauService.SaoChepLichDungTuChuyenMau(maMac, _selectedMaChuyenTau, ngayDi))
                {
                    ThongBaoDialog.ThanhCong("Đã sao chép lịch dừng thành công!", "Thông Báo");
                    LoadLichDungGa(_selectedMaChuyenTau);
                    if (TryParseDecimalInvariant(txtTongChieuDaiM.Text, out decimal cd))
                    {
                        KiemTraChieuDaiVoiDuongTranhGa(_selectedMaChuyenTau, cd);
                    }
                }
                else
                {
                    ThongBaoDialog.ThongTin("Chưa tìm thấy chuyến tàu mẫu nào khác có dữ liệu điểm dừng của mác tàu này để sao chép.", "Thông Báo");
                }
            }
        }


        // PHÂN HỆ ĐOÀN TÀU & ĐẦU MÁY KÉO

        private void LoadDanhSachDauMay(DateTime? gioDi, DateTime? gioDen, int? maChuyenTau)
        {
            try
            {
                var dt = _chuyenTauService.LayDanhSachDauMay(gioDi, gioDen, maChuyenTau);

                if (!dt.Columns.Contains("HienThiDropdown"))
                {
                    dt.Columns.Add("HienThiDropdown", typeof(string));
                }

                foreach (DataRow r in dt.Rows)
                {
                    string soHieu = r["SoHieuDauMay"]?.ToString() ?? "";
                    string dong = r["MaDongCode"]?.ToString() ?? "";
                    bool trung = r["DangTrungLich"] != DBNull.Value && Convert.ToBoolean(r["DangTrungLich"]);
                    string ban = r["ChuyenTauDangBan"]?.ToString() ?? "";

                    if (trung && !string.IsNullOrEmpty(ban))
                    {
                        r["HienThiDropdown"] = $"{soHieu} ({dong}) — Bận: {ban}";
                    }
                    else
                    {
                        r["HienThiDropdown"] = $"{soHieu} ({dong}) — Sẵn sàng";
                    }
                }

                cboDauMayChinh.ItemsSource = dt.DefaultView;

                var dtDay = dt.Copy();
                var rNone = dtDay.NewRow();
                rNone["MaDauMay"] = 0;
                rNone["SoHieuDauMay"] = "-- Không dùng máy đẩy --";
                rNone["HienThiDropdown"] = "-- Không dùng máy đẩy --";
                dtDay.Rows.InsertAt(rNone, 0);
                cboDauMayDay.ItemsSource = dtDay.DefaultView;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải danh sách đầu máy: {ex.Message}");
            }
        }

        private void ChonDauMaySanSangMacDinh()
        {
            if (cboDauMayChinh.ItemsSource is DataView dv)
            {
                // Ưu tiên 1: Đầu máy không trùng lịch và trạng thái SAN_SANG
                for (int i = 0; i < dv.Count; i++)
                {
                    var row = dv[i];
                    bool trung = row["DangTrungLich"] != DBNull.Value && Convert.ToBoolean(row["DangTrungLich"]);
                    string trangThai = row["TrangThai"]?.ToString() ?? "";
                    if (!trung && trangThai == "SAN_SANG")
                    {
                        cboDauMayChinh.SelectedIndex = i;
                        return;
                    }
                }

                // Ưu tiên 2: Đầu máy không trùng lịch và không ở trạng thái bảo dưỡng/sửa chữa
                for (int i = 0; i < dv.Count; i++)
                {
                    var row = dv[i];
                    bool trung = row["DangTrungLich"] != DBNull.Value && Convert.ToBoolean(row["DangTrungLich"]);
                    string trangThai = row["TrangThai"]?.ToString() ?? "";
                    if (!trung && trangThai != "BAO_DUONG" && trangThai != "SUA_CHUA")
                    {
                        cboDauMayChinh.SelectedIndex = i;
                        return;
                    }
                }

                // Nếu tất cả đầu máy đều bận: Để trống (-1) để người dùng chủ động chọn tại Bước 3
                cboDauMayChinh.SelectedIndex = -1;
            }
        }

        private void LoadDoanTau(int maChuyenTau, DateTime gioDi, DateTime gioDen)
        {
            try
            {
                LoadDanhSachDauMay(gioDi, gioDen, maChuyenTau);

                var dtDoanTau = _chuyenTauService.LayThongTinDoanTau(maChuyenTau);
                bool daCoDoanTau = dtDoanTau.Rows.Count > 0 && dtDoanTau.Rows[0]["MaDauMayChinh"] != DBNull.Value && Convert.ToInt32(dtDoanTau.Rows[0]["MaDauMayChinh"]) > 0;

                if (daCoDoanTau)
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

                    cboDauMayChinh.IsEnabled = false;
                    cboDauMayDay.IsEnabled = false;
                    cboDauMayChinh.ToolTip = "Đầu máy kéo chính được quản lý theo biên chế đoàn tàu kỹ thuật tại phân hệ Phương Tiện. Bấm [Sửa Đoàn Tàu] để thay đổi.";
                    cboDauMayDay.ToolTip = "Đầu máy đẩy được quản lý theo biên chế đoàn tàu kỹ thuật tại phân hệ Phương Tiện.";

                    if (bdTrangThaiDoanTau != null) bdTrangThaiDoanTau.Visibility = Visibility.Visible;
                    if (txtTrangThaiDoanTau != null) txtTrangThaiDoanTau.Text = "Đã liên kết Đoàn Tàu Kỹ Thuật (Đầu máy & Toa xe đã khóa đồng bộ từ phân hệ Phương Tiện).";
                    if (txtBtnMoLapDoanTau != null) txtBtnMoLapDoanTau.Text = "Sửa Đoàn Tàu";
                    btnMoLapDoanTau.ToolTip = "Mở phân hệ Phương Tiện để sửa đầu máy hoặc kéo-thả lại toa xe";

                    KiemTraChieuDaiVoiDuongTranhGa(maChuyenTau, chieuDai);
                    CapNhatCanhBaoDauMay();
                }
                else
                {
                    bool isKhoaChinhSua = (_currentTrangThai == "DANG_CHAY" || _currentTrangThai == "TRE_GIO" || _currentTrangThai == "HOAN_THANH" || _currentTrangThai == "DA_HUY");
                    cboDauMayChinh.IsEnabled = !isKhoaChinhSua;
                    cboDauMayDay.IsEnabled = !isKhoaChinhSua;
                    cboDauMayChinh.ToolTip = "Chọn đầu máy kéo chính sẵn sàng trong khung giờ";
                    cboDauMayDay.ToolTip = "Chọn đầu máy đẩy hỗ trợ (nếu có)";

                    if (bdTrangThaiDoanTau != null) bdTrangThaiDoanTau.Visibility = Visibility.Collapsed;
                    if (txtBtnMoLapDoanTau != null) txtBtnMoLapDoanTau.Text = "Mở Lập Đoàn Tàu";
                    btnMoLapDoanTau.ToolTip = "Mở tab Lập Đoàn Tàu bên Quản Lý Phương Tiện để biên chế toa xe";

                    if (cboDauMayChinh.Items.Count > 0 && cboDauMayChinh.SelectedIndex < 0)
                        ChonDauMaySanSangMacDinh();
                    cboDauMayDay.SelectedIndex = 0;

                    int soToa = dgToaXe.Items.Count > 0 ? dgToaXe.Items.Count : 3;
                    decimal chieuDai = 16.5m + (soToa * 20.0m);
                    decimal trongLuong = 78.0m + (soToa * 40.0m);
                    txtTongChieuDaiM.Text = chieuDai.ToString("F1");
                    txtTongTrongLuongTan.Text = trongLuong.ToString("F1");
                    chkDaDuyetAnToan.IsChecked = false;

                    KiemTraChieuDaiVoiDuongTranhGa(maChuyenTau, chieuDai);
                    CapNhatCanhBaoDauMay();
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
            CapNhatCanhBaoDauMay();

            // Tự động đổ danh sách các toa xe vào DataGrid phía dưới khi chọn đoàn tàu
            if (cboDauMayChinh.SelectedValue != null && int.TryParse(cboDauMayChinh.SelectedValue.ToString(), out int maDm) && maDm > 0)
            {
                DataTable? dt = null;
                if (_selectedMaChuyenTau > 0)
                {
                    dt = _chuyenTauService.LayDanhSachToaXe(_selectedMaChuyenTau);
                }

                if (dt == null || dt.Rows.Count == 0)
                {
                    dt = _chuyenTauService.LayDanhSachToaXeTheoDauMay(maDm);
                }

                if (dt != null && dt.Rows.Count > 0)
                {
                    HienThiDanhSachToaXe(dt);

                    // Tự động đồng bộ chiều dài và tải trọng tương ứng với đoàn tàu này
                    if (cboDauMayChinh.IsEnabled)
                    {
                        decimal cd = 16.5m + (dt.Rows.Count * 20.0m);
                        decimal tl = 78.0m + (dt.Rows.Count * 40.0m);
                        txtTongChieuDaiM.Text = cd.ToString("F1");
                        txtTongTrongLuongTan.Text = tl.ToString("F1");
                        KiemTraChieuDaiVoiDuongTranhGa(_selectedMaChuyenTau, cd);
                    }
                }
            }
        }

        private void CboDauMayDay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CapNhatCanhBaoDauMay();
        }

        private void CapNhatCanhBaoDauMay()
        {
            if (bdCanhBaoDauMay == null) return;

            var dsCanhBao = new List<string>();

            int maChinh = 0;
            if (cboDauMayChinh.SelectedItem is DataRowView rChinh)
            {
                if (rChinh["MaDauMay"] != DBNull.Value && int.TryParse(rChinh["MaDauMay"].ToString(), out int mc) && mc > 0)
                {
                    maChinh = mc;
                    bool dangTrungChinh = rChinh["DangTrungLich"] != DBNull.Value && Convert.ToBoolean(rChinh["DangTrungLich"]);
                    if (dangTrungChinh)
                    {
                        string thongTinBan = rChinh["ChuyenTauDangBan"]?.ToString() ?? "chuyến khác";
                        dsCanhBao.Add($"Đầu máy kéo chính [{rChinh["SoHieuDauMay"]}] đang bận {thongTinBan} có khung giờ trùng lặp");
                    }
                }
            }

            if (cboDauMayDay.SelectedItem is DataRowView rDay)
            {
                if (rDay["MaDauMay"] != DBNull.Value && int.TryParse(rDay["MaDauMay"].ToString(), out int md) && md > 0)
                {
                    if (maChinh > 0 && md == maChinh)
                    {
                        dsCanhBao.Add("Đầu máy kéo chính không được trùng với đầu máy đẩy");
                    }

                    bool dangTrungDay = rDay["DangTrungLich"] != DBNull.Value && Convert.ToBoolean(rDay["DangTrungLich"]);
                    if (dangTrungDay)
                    {
                        string thongTinBan = rDay["ChuyenTauDangBan"]?.ToString() ?? "chuyến khác";
                        dsCanhBao.Add($"Đầu máy đẩy [{rDay["SoHieuDauMay"]}] đang bận {thongTinBan} có khung giờ trùng lặp");
                    }
                }
            }

            if (dsCanhBao.Count > 0)
            {
                bdCanhBaoDauMay.Visibility = Visibility.Visible;
                txtCanhBaoDauMay.Text = "Cảnh báo phương tiện: " + string.Join("; ", dsCanhBao) + ". (Hệ thống sẽ chặn khi Lưu Lịch Trình nếu chưa xử lý trùng lặp).";
            }
            else
            {
                bdCanhBaoDauMay.Visibility = Visibility.Collapsed;
            }
        }

        // =========================================================================
        // PHÂN HỆ BIÊN CHẾ TOA XE & LIÊN KẾT LẬP ĐOÀN TÀU (Bước 3)
        // =========================================================================

        private void BtnMoLapDoanTau_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0)
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn hoặc lưu chuyến tàu trước khi sang phân hệ Lập Đoàn Tàu.", "Chưa Chọn Chuyến");
                return;
            }

            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.NavigateToLapDoanTau(_selectedMaChuyenTau);
            }
        }

        private void BtnXoaToaXe_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                int maToa = Convert.ToInt32(row["MaToaXeKhach"]);
                string nhanHieu = row["NhanHieuToa"]?.ToString() ?? "";

                var r = ThongBaoDialog.XacNhan($"Xác nhận xóa '{nhanHieu}' khỏi biên chế đoàn tàu?", "Xác Nhận Xóa Toa Xe", nutDongY: "Xóa Toa Xe", nutHuy: "Quay Lại", laHanhDongXoa: true);
                if (r)
                {
                    if (_chuyenTauService.XoaToaXeKhach(maToa, out string thongBaoLoi))
                    {
                        LoadToaXe(_selectedMaChuyenTau);
                        DateTime dtDi = dpNgayChuyen.SelectedDate ?? DateTime.Today;
                        LoadDoanTau(_selectedMaChuyenTau, dtDi, dtDi.AddDays(1));
                    }
                    else
                    {
                        ThongBaoDialog.CanhBao(thongBaoLoi, "Không Thể Xóa");
                    }
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

        public void ChonChuyenTauTheoId(int maChuyenTau, DateTime? ngay = null)
        {
            if (ngay.HasValue && dpLocNgay.SelectedDate != ngay.Value.Date)
            {
                dpLocNgay.SelectedDate = ngay.Value.Date;
            }
            _selectedMaChuyenTau = maChuyenTau;
            LoadData();

            if (dgChuyenTau.Items.Count > 0 && _cachedChuyenTau != null)
            {
                for (int i = 0; i < _cachedChuyenTau.Rows.Count; i++)
                {
                    if (Convert.ToInt32(_cachedChuyenTau.Rows[i]["MaChuyenTau"]) == maChuyenTau)
                    {
                        dgChuyenTau.SelectedIndex = i;
                        dgChuyenTau.ScrollIntoView(dgChuyenTau.SelectedItem);
                        break;
                    }
                }
            }
        }

        private void BtnXemBieuDo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int maChuyenTau))
            {
                DateTime? ngayChay = dpLocNgay.SelectedDate;
                var row = (btn.DataContext as DataRowView) ?? (dgChuyenTau.SelectedItem as DataRowView);
                if (row != null && row["NgayXuatPhat"] != DBNull.Value)
                {
                    ngayChay = Convert.ToDateTime(row["NgayXuatPhat"]).Date;
                }

                if (Application.Current.MainWindow is MainWindow mw)
                {
                    mw.NavigateToBieuDo(maChuyenTau, ngayChay);
                }
            }
        }

        private static string DinhDangTrangThai(string? tt) => tt switch
        {
            "DA_LEN_LICH" => "Đã lên lịch",
            "SAN_SANG" => "Sẵn sàng",
            "DANG_CHAY" => "Đang chạy",
            "TRE_GIO" => "Trễ giờ",
            "HOAN_THANH" => "Hoàn thành",
            "DA_HUY" => "Đã hủy",
            _ => tt ?? "--"
        };

        private static string DinhDangLoaiTau(string? lt) => lt switch
        {
            "TAU_NHANH" => "Khách SE",
            "TAU_CHO" => "Khách TN",
            "TAU_HANG" => "Tàu Hàng",
            "TAU_CHUYEN_DUNG" => "Chuyên dụng",
            _ => lt ?? "--"
        };

        private static string DinhDangHuongChay(string? hc) => hc switch
        {
            "BAC_NAM" => "Bắc ➔ Nam",
            "NAM_BAC" => "Nam ➔ Bắc",
            _ => hc ?? "--"
        };

        // =========================================================================
        // TIỆN ÍCH XỬ LÝ & ĐỊNH DẠNG THỜI GIAN CHUẨN ĐƯỜNG SẮT (HH:mm)
        // =========================================================================

        public static TimeSpan? ParseTimeSpan(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            string s = input.Trim().Replace("h", ":").Replace("H", ":").Replace(".", ":");

            // Xử lý khi người dùng gõ số liền: 0600 -> 06:00, 600 -> 06:00, 1930 -> 19:30
            if (!s.Contains(':'))
            {
                if (s.Length == 4 && int.TryParse(s, out _))
                {
                    s = s.Insert(2, ":");
                }
                else if (s.Length == 3 && int.TryParse(s, out _))
                {
                    s = "0" + s.Insert(1, ":");
                }
                else if ((s.Length == 1 || s.Length == 2) && int.TryParse(s, out int h) && h >= 0 && h < 24)
                {
                    s = $"{h:D2}:00";
                }
            }

            if (TimeSpan.TryParse(s, out TimeSpan ts))
            {
                if (ts.TotalHours < 24) return ts;
            }

            if (DateTime.TryParseExact(s, new[] { "H:m", "HH:mm", "H:mm", "HH:m" },
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dt))
            {
                return dt.TimeOfDay;
            }

            return null;
        }

        public static string FormatTimeSpan(TimeSpan? ts)
        {
            if (!ts.HasValue) return string.Empty;
            return ts.Value.ToString(@"hh\:mm");
        }

        private void TxtGio_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && !string.IsNullOrWhiteSpace(tb.Text))
            {
                var ts = ParseTimeSpan(tb.Text);
                if (ts.HasValue)
                {
                    tb.Text = FormatTimeSpan(ts);
                }
            }
        }

        private void TxtGioKH_LostFocus(object sender, RoutedEventArgs e)
        {
            TxtGio_LostFocus(sender, e);
            CapNhatDuKienVeDich();
        }

        private void DpNgayChuyen_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CapNhatDuKienVeDich();
        }


        private void CapNhatDuKienVeDich()
        {
            if (txtNgayVeDichDuKien == null || txtThoiGianChayDuKien == null) return;
            if (dpNgayChuyen.SelectedDate == null)
            {
                txtNgayVeDichDuKien.Text = "--/--/----";
                txtThoiGianChayDuKien.Text = "Hành trình: --";
                return;
            }

            var tDi = ParseTimeSpan(txtGioDiKH.Text);
            var tDen = ParseTimeSpan(txtGioDenKH.Text);
            if (!tDi.HasValue || !tDen.HasValue)
            {
                txtNgayVeDichDuKien.Text = "--/--/----";
                txtThoiGianChayDuKien.Text = "Hành trình: --";
                return;
            }

            decimal tgChuanGio = 12.0m;
            if (cboMacTau.SelectedItem is DataRowView rMac && decimal.TryParse(rMac["ThoiGianChuanGio"]?.ToString(), out decimal tg) && tg > 0)
            {
                tgChuanGio = tg;
            }

            DateTime ngayDi = dpNgayChuyen.SelectedDate.Value.Date;
            DateTime dtDi = ngayDi.Add(tDi.Value);
            DateTime dtDen = ChuyenTauService.TinhToanGioVeDich(ngayDi, tDi.Value, tDen.Value, tgChuanGio);

            int soNgayChenh = (dtDen.Date - dtDi.Date).Days;
            string quaDemText = soNgayChenh switch
            {
                0 => " (Cùng ngày)",
                1 => " (+1 ngày)",
                2 => " (+2 ngày)",
                _ => $" (+{soNgayChenh} ngày)"
            };

            txtNgayVeDichDuKien.Text = $"{dtDen:dd/MM/yyyy HH:mm}{quaDemText}";
            TimeSpan tongThoiGian = dtDen - dtDi;
            txtThoiGianChayDuKien.Text = $"Hành trình: {(int)tongThoiGian.TotalHours}h {tongThoiGian.Minutes:D2}p";
        }

        // =========================================================================
        // QUY TRÌNH ĐIỀU HÀNH 4 BƯỚC (STEP INDICATOR LOGIC)
        // =========================================================================

        public void CapNhatQuyTrinh(int step)
        {
            if (_isUpdatingStep) return;
            _isUpdatingStep = true;
            try
            {
                _currentStep = Math.Clamp(step, 1, 3);

                if (tabInspector != null && tabInspector.SelectedIndex != _currentStep - 1)
                {
                    tabInspector.SelectedIndex = _currentStep - 1;
                }

                if (bdStep1Circle == null) return;

                // --- BƯỚC 1: THÔNG SỐ ---
                if (_currentStep == 1)
                {
                    bdStep1Circle.Background = ActiveBgBrush;
                    iconStep1.Symbol = SymbolRegular.Navigation24;
                    iconStep1.Foreground = Brushes.White;
                    txtStep1.Foreground = ActiveFgBrush;
                    txtStep1.FontWeight = FontWeights.Bold;
                }
                else
                {
                    bdStep1Circle.Background = CompletedBgBrush;
                    iconStep1.Symbol = SymbolRegular.Checkmark24;
                    iconStep1.Foreground = Brushes.White;
                    txtStep1.Foreground = CompletedBgBrush;
                    txtStep1.FontWeight = FontWeights.SemiBold;
                }

                // --- CONNECTOR 1-2 ---
                lineStep12.Background = (_currentStep >= 2) ? CompletedLineBrush : InactiveLineBrush;

                // --- BƯỚC 2: GA DỪNG ---
                if (_currentStep < 2)
                {
                    bdStep2Circle.Background = InactiveBgBrush;
                    iconStep2.Symbol = SymbolRegular.Timer24;
                    iconStep2.Foreground = InactiveFgBrush;
                    txtStep2.Foreground = InactiveFgBrush;
                    txtStep2.FontWeight = FontWeights.SemiBold;
                }
                else if (_currentStep == 2)
                {
                    bdStep2Circle.Background = ActiveBgBrush;
                    iconStep2.Symbol = SymbolRegular.Timer24;
                    iconStep2.Foreground = Brushes.White;
                    txtStep2.Foreground = ActiveFgBrush;
                    txtStep2.FontWeight = FontWeights.Bold;
                }
                else
                {
                    bdStep2Circle.Background = CompletedBgBrush;
                    iconStep2.Symbol = SymbolRegular.Checkmark24;
                    iconStep2.Foreground = Brushes.White;
                    txtStep2.Foreground = CompletedBgBrush;
                    txtStep2.FontWeight = FontWeights.SemiBold;
                }

                // --- CONNECTOR 2-3 ---
                lineStep23.Background = (_currentStep >= 3) ? CompletedLineBrush : InactiveLineBrush;

                // --- BƯỚC 3: PHƯƠNG TIỆN & VẬN HÀNH ---
                if (_currentStep < 3)
                {
                    bdStep3Circle.Background = InactiveBgBrush;
                    iconStep3.Symbol = SymbolRegular.VehicleSubway24;
                    iconStep3.Foreground = InactiveFgBrush;
                    txtStep3.Foreground = InactiveFgBrush;
                    txtStep3.FontWeight = FontWeights.SemiBold;
                }
                else
                {
                    bdStep3Circle.Background = ActiveBgBrush;
                    iconStep3.Symbol = SymbolRegular.VehicleSubway24;
                    iconStep3.Foreground = Brushes.White;
                    txtStep3.Foreground = ActiveFgBrush;
                    txtStep3.FontWeight = FontWeights.Bold;
                }

                // --- FOOTER BUTTONS ---
                btnPrevStep.Visibility = (_currentStep > 1) ? Visibility.Visible : Visibility.Collapsed;
                btnNextStep.Visibility = (_currentStep < 3) ? Visibility.Visible : Visibility.Collapsed;

                txtBtnNextStep.Text = "Tiếp Theo";

                if (_isAddingNew)
                {
                    // Khi đang thêm mới: Ẩn nút Lưu ở bước 1 và 2 để người dùng chuyển bước tự nhiên qua 'Tiếp Theo'
                    if (_currentStep < 3)
                    {
                        btnLuuChuyen.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        btnLuuChuyen.Visibility = Visibility.Visible;
                        txtBtnLuuChuyen.Text = "Lập Chuyến Tàu";
                        if (TryFindResource("PrimaryDesktopButton") is Style priStyle)
                        {
                            btnLuuChuyen.Style = priStyle;
                        }
                    }
                }
                else
                {
                    btnLuuChuyen.Visibility = Visibility.Visible;
                    txtBtnLuuChuyen.Text = (_currentStep == 3) ? "Lưu Lịch Trình" : "Lưu";
                    if (TryFindResource("PrimaryDesktopButton") is Style priStyle)
                    {
                        btnLuuChuyen.Style = priStyle;
                    }
                }
            }
            finally
            {
                _isUpdatingStep = false;
            }
        }

        private void TabInspector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingStep) return;
            if (e.Source is TabControl && tabInspector != null)
            {
                int targetStep = tabInspector.SelectedIndex + 1;
                if (targetStep >= 1 && targetStep <= 3 && targetStep != _currentStep)
                {
                    CapNhatQuyTrinh(targetStep);
                }
            }
        }

        private void BtnStep1_Click(object sender, RoutedEventArgs e) => CapNhatQuyTrinh(1);

        private void BtnStep2_Click(object sender, RoutedEventArgs e)
        {
            if (_isAddingNew && (_dtLichDungInMem == null || _dtLichDungInMem.Rows.Count == 0))
            {
                NapMauTuyenChoInMem();
            }
            CapNhatQuyTrinh(2);
        }

        private void BtnStep3_Click(object sender, RoutedEventArgs e)
        {
            CapNhatQuyTrinh(3);
        }

        private void BtnPrevStep_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 1)
            {
                CapNhatQuyTrinh(_currentStep - 1);
            }
        }

        private void BtnNextStep_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep == 1)
            {
                // BƯỚC 1: Chỉ kiểm tra thông số hành trình & thời gian của Bước 1
                if (!KiemTraHopLeBuoc1(out string loi1))
                {
                    if (!string.IsNullOrWhiteSpace(loi1) && loi1 != "Người dùng chọn chỉnh lại thời gian chạy.")
                    {
                        ThongBaoDialog.CanhBao(loi1, "Thông Số Hành Trình (Bước 1)");
                    }
                    return;
                }

                if (_isAddingNew)
                {
                    // Lập mới: Hoạt động In-Memory trên RAM, không ghi bản nháp CSDL
                    if (_dtLichDungInMem == null || _dtLichDungInMem.Rows.Count == 0)
                    {
                        NapMauTuyenChoInMem();
                    }
                }
                else
                {
                    CapNhatThongSoBuoc1();
                }

                CapNhatQuyTrinh(2);
                return;
            }
            else if (_currentStep == 2)
            {
                // BƯỚC 2: Chỉ kiểm tra lịch trình điểm dừng của Bước 2
                if (!KiemTraHopLeBuoc2(out string loi2))
                {
                    ThongBaoDialog.CanhBao(loi2, "Lịch Trình Điểm Dừng (Bước 2)");
                    return;
                }

                CapNhatQuyTrinh(3);
                return;
            }
            else if (_currentStep < 3)
            {
                CapNhatQuyTrinh(_currentStep + 1);
            }
        }

        private void BtnXemBieuDoHienTai_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0)
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn một chuyến tàu trước khi xem trên Biểu đồ.", "Thông Báo");
                return;
            }

            DateTime? ngayChay = dpNgayChuyen.SelectedDate ?? dpLocNgay.SelectedDate ?? DateTime.Today;
            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.NavigateToBieuDo(_selectedMaChuyenTau, ngayChay);
            }
        }

        private void BtnKiemTraXungDot_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMaChuyenTau <= 0)
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn một chuyến tàu để kiểm tra an toàn xung đột.", "Thông Báo");
                return;
            }

            try
            {
                DateTime ngayChay = dpNgayChuyen.SelectedDate ?? DateTime.Today;
                var dieuDoService = new DieuDoService();
                var dsXungDotToanTuyen = dieuDoService.KiemTraXungDotToanTuyen(ngayChay);

                var xungDotCuaChuyen = dsXungDotToanTuyen
                    .Where(x => x.MaChuyenTau1 == _selectedMaChuyenTau || x.MaChuyenTau2 == _selectedMaChuyenTau)
                    .ToList();

                if (xungDotCuaChuyen.Count == 0)
                {
                    string macTau = cboMacTau.Text;
                    ThongBaoDialog.ThanhCong(
                        $"Chuyến tàu #{_selectedMaChuyenTau} ({macTau}) không có bất kỳ nguy cơ xung đột đối đầu hoặc vi phạm giãn cách an toàn nào trên toàn bộ hành trình ngày {ngayChay:dd/MM/yyyy}!",
                        "An Toàn Tuyệt Đối",
                        Window.GetWindow(this));
                }
                else
                {
                    string macTau = cboMacTau.Text;
                    string hanhTrinh = $"{txtGaDi.Text} → {txtGaDen.Text}";
                    var dialog = new CanhBaoXungDotDialog(xungDotCuaChuyen, _selectedMaChuyenTau, macTau, hanhTrinh, ngayChay)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    dialog.ShowDialog();

                    if (dialog.YeuCauMoBieuDo)
                    {
                        BtnXemBieuDoHienTai_Click(sender, e);
                    }
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi kiểm tra xung đột: {ex.Message}", "Lỗi Điều Độ", Window.GetWindow(this));
            }
        }


        private void CapNhatHienThiTheoLoaiTau(string loaiTau)
        {
            bool laTauHang = (loaiTau == "TAU_HANG");
            if (bdTauHangNotice != null)
            {
                bdTauHangNotice.Visibility = laTauHang ? Visibility.Visible : Visibility.Collapsed;
            }
            if (pnlToaXeKhach != null)
            {
                pnlToaXeKhach.Visibility = laTauHang ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void BtnChuyenPhanHeHangHoa_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.NavigateTo("HangHoa");
            }
        }
    }
}

