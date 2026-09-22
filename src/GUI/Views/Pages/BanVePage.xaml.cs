using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using BUS.Services;
using ET.VanTai;
using GUI.Views.Dialogs;
using QRCoder;

namespace GUI.Views.Pages
{
    public partial class BanVePage : Page
    {
        private readonly VeService _veService = new();
        private readonly GaService _gaService = new();
        private readonly ChuyenTauService _chuyenTauService = new();

        private DataTable? _cachedDsGa;
        private DataTable? _cachedChuyenTau;
        private DataTable? _cachedDanhSachVe;

        private readonly List<GioVeItem> _gioVe = new();

        private int _currentMaChuyenTau = 0;
        private string _currentSoHieuMacTau = "";
        private int _currentMaGaDi = 0;
        private int _currentMaGaDen = 0;
        private int _currentMaToaXe = 0;
        private string _currentNhanHieuToa = "";
        private string _currentLoaiToa = "";
        private bool _isLoaded = false;

        public BanVePage()
        {
            InitializeComponent();
            _isLoaded = true;
            KhoiTaoDuLieuBanDau();
        }

        private void KhoiTaoDuLieuBanDau()
        {
            try
            {
                _cachedDsGa = _gaService.LayDanhSach();
                cboGaDi.ItemsSource = _cachedDsGa.DefaultView;
                cboGaDen.ItemsSource = _cachedDsGa.DefaultView;

                cboGaDi.SelectedValue = 1; // Hà Nội
                cboGaDen.SelectedValue = 27; // Sài Gòn
                dpNgayDi.Language = System.Windows.Markup.XmlLanguage.GetLanguage("vi-VN");
                dpNgayDi.SelectedDate = DateTime.Today;
                dpNgayDi.Loaded += (s, e) => ChuanHoaDatePicker(dpNgayDi);
                dpNgayDi.SelectedDateChanged += (s, e) => ChuanHoaDatePicker(dpNgayDi);
                Dispatcher.BeginInvoke(new Action(() => ChuanHoaDatePicker(dpNgayDi)), System.Windows.Threading.DispatcherPriority.Loaded);

                txtNguoiMuaTen.TextChanged += (s, e) =>
                {
                    if (_gioVe.Count == 1 && txtNguoiMuaTen.Tag == null)
                    {
                        _gioVe[0].TenHanhKhach = txtNguoiMuaTen.Text.Trim();
                        if (spGioVe.Children.Count > 0 && spGioVe.Children[0] is Border b && b.Child is Grid g)
                        {
                            foreach (var row in g.Children)
                            {
                                if (row is Grid gridRow && Grid.GetRow(gridRow) == 1)
                                {
                                    foreach (var c in gridRow.Children)
                                    {
                                        if (c is TextBox tb && tb.Text != txtNguoiMuaTen.Text)
                                        {
                                            tb.Text = txtNguoiMuaTen.Text;
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                TimChuyenTau();
                LoadDanhSachVeTab2();
                RenderGioVe();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi nạp dữ liệu ban đầu: {ex.Message}", "Lỗi CSDL");
            }
        }

        // =========================================================================
        // 1. TÌM KIẾM CHẶNG, CHUYẾN TÀU & BỘ LỌC NÂNG CAO
        // =========================================================================
        private void BtnTimChuyen_Click(object sender, RoutedEventArgs e) => TimChuyenTau();

        private void TimChuyenTau()
        {
            if (cboGaDi.SelectedValue == null || cboGaDen.SelectedValue == null) return;

            int gaDi = Convert.ToInt32(cboGaDi.SelectedValue);
            int gaDen = Convert.ToInt32(cboGaDen.SelectedValue);

            if (gaDi == gaDen)
            {
                ThongBaoDialog.CanhBao("Ga đi và Ga đến không được trùng nhau!", "Trùng Ga");
                return;
            }

            _currentMaGaDi = gaDi;
            _currentMaGaDen = gaDen;
            DateTime ngayDi = dpNgayDi.SelectedDate ?? DateTime.Today;

            _cachedChuyenTau = _veService.TimChuyenTauTheoChang(gaDi, gaDen, ngayDi);

            if (!_cachedChuyenTau.Columns.Contains("LoTrinhChay"))
                _cachedChuyenTau.Columns.Add("LoTrinhChay", typeof(string));
            if (!_cachedChuyenTau.Columns.Contains("ChoConLai"))
                _cachedChuyenTau.Columns.Add("ChoConLai", typeof(string));
            if (!_cachedChuyenTau.Columns.Contains("SoChoConLai"))
                _cachedChuyenTau.Columns.Add("SoChoConLai", typeof(int));
            if (!_cachedChuyenTau.Columns.Contains("GioDiHour"))
                _cachedChuyenTau.Columns.Add("GioDiHour", typeof(int));
            if (!_cachedChuyenTau.Columns.Contains("GioDenHour"))
                _cachedChuyenTau.Columns.Add("GioDenHour", typeof(int));
            if (!_cachedChuyenTau.Columns.Contains("GiaVeCoSoNum"))
                _cachedChuyenTau.Columns.Add("GiaVeCoSoNum", typeof(decimal));

            foreach (DataRow row in _cachedChuyenTau.Rows)
            {
                row["LoTrinhChay"] = $"{row["TenGaXuatPhat"]} → {row["TenGaKetThuc"]}";
                int tong = Convert.ToInt32(row["TongSoCho"]);
                int daBan = Convert.ToInt32(row["SoChoDaBan"]);
                int conLai = Math.Max(0, tong - daBan);
                row["SoChoConLai"] = conLai;
                row["ChoConLai"] = $"{conLai} chỗ";

                if (row["GioXuatPhatKH"] != DBNull.Value)
                    row["GioDiHour"] = Convert.ToDateTime(row["GioXuatPhatKH"]).Hour;
                else
                    row["GioDiHour"] = 0;

                if (row["GioVeDichKH"] != DBNull.Value)
                    row["GioDenHour"] = Convert.ToDateTime(row["GioVeDichKH"]).Hour;
                else
                    row["GioDenHour"] = 0;

                if (row["GiaVeCoSo"] != DBNull.Value)
                    row["GiaVeCoSoNum"] = Convert.ToDecimal(row["GiaVeCoSo"]);
                else
                    row["GiaVeCoSoNum"] = 0m;
            }

            ApDungBoLocChuyenTau();
        }

        private void BoLoc_Changed(object sender, RoutedEventArgs e)
        {
            if (_cachedChuyenTau != null) ApDungBoLocChuyenTau();
        }

        private void TxtLocMacTau_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_cachedChuyenTau != null) ApDungBoLocChuyenTau();
        }

        private void BtnToggleLoc_Click(object sender, RoutedEventArgs e)
        {
            popBoLocNangCao.IsOpen = !popBoLocNangCao.IsOpen;
        }

        private void BtnDongLoc_Click(object sender, RoutedEventArgs e)
        {
            popBoLocNangCao.IsOpen = false;
        }

        private bool _chiLocConCho = false;

        private void GridSwitchCheDo_Click(object sender, MouseButtonEventArgs e)
        {
            _chiLocConCho = !_chiLocConCho;
            CapNhatGiaoDienSwitch(animate: true);
            ApDungBoLocChuyenTau();
        }

        private void TabTatCa_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (_chiLocConCho)
            {
                _chiLocConCho = false;
                CapNhatGiaoDienSwitch(animate: true);
                ApDungBoLocChuyenTau();
            }
        }

        private void TabChiConCho_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (!_chiLocConCho)
            {
                _chiLocConCho = true;
                CapNhatGiaoDienSwitch(animate: true);
                ApDungBoLocChuyenTau();
            }
        }

        private void GridSwitchCheDo_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CapNhatGiaoDienSwitch(animate: false);
        }

        private void CapNhatGiaoDienSwitch(bool animate = true)
        {
            if (ttSwitchIndicator == null || bdSwitchIndicator == null || txtSwitchTatCa == null || txtSwitchChiConCho == null) return;

            double totalWidth = gridSwitchCheDo.ActualWidth > 0 ? gridSwitchCheDo.ActualWidth : 220.0;
            double halfWidth = totalWidth / 2.0;

            // Khối indicator ôm sát 1 nửa cột, trừ 4px padding
            bdSwitchIndicator.Width = Math.Max(20, halfWidth - 4);
            double targetX = _chiLocConCho ? halfWidth : 0.0;

            if (animate)
            {
                var anim = new DoubleAnimation
                {
                    To = targetX,
                    Duration = TimeSpan.FromMilliseconds(180),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                ttSwitchIndicator.BeginAnimation(TranslateTransform.XProperty, anim);
            }
            else
            {
                ttSwitchIndicator.BeginAnimation(TranslateTransform.XProperty, null);
                ttSwitchIndicator.X = targetX;
            }

            if (_chiLocConCho)
            {
                txtSwitchTatCa.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                txtSwitchTatCa.FontWeight = FontWeights.Normal;

                txtSwitchChiConCho.Foreground = Brushes.White;
                txtSwitchChiConCho.FontWeight = FontWeights.Bold;
            }
            else
            {
                txtSwitchTatCa.Foreground = Brushes.White;
                txtSwitchTatCa.FontWeight = FontWeights.Bold;

                txtSwitchChiConCho.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                txtSwitchChiConCho.FontWeight = FontWeights.Normal;
            }
        }

        private void CboToanTuGia_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;

            if (cboToanTuGia?.SelectedItem is ComboBoxItem cbi && cbi.Tag is string tag)
            {
                if (txtGiaNhap1 != null)
                {
                    txtGiaNhap1.IsEnabled = tag != "ALL";
                    if (tag == "LTE") txtGiaNhap1.PlaceholderText = "Giá tối đa (≤)...";
                    else if (tag == "GTE") txtGiaNhap1.PlaceholderText = "Giá tối thiểu (≥)...";
                    else if (tag == "BETWEEN") txtGiaNhap1.PlaceholderText = "Mức giá từ (≥)...";
                    else txtGiaNhap1.PlaceholderText = "Nhập mức giá...";
                }

                if (gridGiaDen != null)
                {
                    gridGiaDen.Visibility = tag == "BETWEEN" ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            if (_cachedChuyenTau != null) ApDungBoLocChuyenTau();
        }

        private void TxtGiaNhap_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (_cachedChuyenTau != null) ApDungBoLocChuyenTau();
        }

        private void BtnChipGia_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            if (sender is Button btn && btn.Tag is string tagStr && decimal.TryParse(tagStr, out decimal val))
            {
                if (cboToanTuGia.SelectedIndex == 0)
                {
                    cboToanTuGia.SelectedIndex = 1; // Chuyển sang LTE (<=)
                }

                string tag = (cboToanTuGia.SelectedItem as ComboBoxItem)?.Tag as string ?? "LTE";
                if (tag == "BETWEEN" && !string.IsNullOrWhiteSpace(txtGiaNhap1.Text))
                {
                    txtGiaNhap2.Text = $"{val:N0}";
                }
                else
                {
                    txtGiaNhap1.Text = $"{val:N0}";
                }
            }
        }

        private static decimal? LaySoTien(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string raw = text.Replace(".", "").Replace(",", "").Replace("đ", "").Replace("VND", "").Replace(" ", "").Trim();
            if (decimal.TryParse(raw, out decimal val) && val > 0)
            {
                return val;
            }
            return null;
        }

        private void BtnResetLoc_Click(object sender, RoutedEventArgs e)
        {
            _chiLocConCho = false;
            CapNhatGiaoDienSwitch(animate: true);
            if (cboLocGioDi != null) cboLocGioDi.SelectedIndex = 0;
            if (cboLocGioDen != null) cboLocGioDen.SelectedIndex = 0;
            if (cboToanTuGia != null) cboToanTuGia.SelectedIndex = 0;
            if (txtGiaNhap1 != null) txtGiaNhap1.Text = "";
            if (txtGiaNhap2 != null) txtGiaNhap2.Text = "";
            if (gridGiaDen != null) gridGiaDen.Visibility = Visibility.Collapsed;
            if (txtLocMacTau != null) txtLocMacTau.Text = "";
            if (_cachedChuyenTau != null) ApDungBoLocChuyenTau();
        }

        private void ApDungBoLocChuyenTau()
        {
            if (!_isLoaded || dgChuyenTau == null) return;

            if (_cachedChuyenTau == null || _cachedChuyenTau.Rows.Count == 0)
            {
                if (dgChuyenTau != null) dgChuyenTau.ItemsSource = null;
                if (spToaXe != null) spToaXe.Children.Clear();
                if (gridNoiThatToa != null) gridNoiThatToa.Children.Clear();
                if (dgLichDungGa != null) dgLichDungGa.ItemsSource = null;
                if (txtTieuDeToaHienTai != null) txtTieuDeToaHienTai.Text = "Không có chuyến tàu phù hợp trong ngày!";
                return;
            }

            var query = _cachedChuyenTau.AsEnumerable();

            // 1. Lọc còn chỗ trống
            if (_chiLocConCho)
            {
                query = query.Where(r => r.Field<int>("SoChoConLai") > 0);
            }

            // 2. Lọc khung giờ khởi hành
            int idxGioDi = cboLocGioDi?.SelectedIndex ?? 0;
            if (idxGioDi == 1) query = query.Where(r => r.Field<int>("GioDiHour") >= 0 && r.Field<int>("GioDiHour") < 6);
            else if (idxGioDi == 2) query = query.Where(r => r.Field<int>("GioDiHour") >= 6 && r.Field<int>("GioDiHour") < 12);
            else if (idxGioDi == 3) query = query.Where(r => r.Field<int>("GioDiHour") >= 12 && r.Field<int>("GioDiHour") < 18);
            else if (idxGioDi == 4) query = query.Where(r => r.Field<int>("GioDiHour") >= 18 && r.Field<int>("GioDiHour") < 24);

            // 3. Lọc khung giờ đến
            int idxGioDen = cboLocGioDen?.SelectedIndex ?? 0;
            if (idxGioDen == 1) query = query.Where(r => r.Field<int>("GioDenHour") >= 0 && r.Field<int>("GioDenHour") < 12);
            else if (idxGioDen == 2) query = query.Where(r => r.Field<int>("GioDenHour") >= 12 && r.Field<int>("GioDenHour") < 18);
            else if (idxGioDen == 3) query = query.Where(r => r.Field<int>("GioDenHour") >= 18 && r.Field<int>("GioDenHour") < 24);

            // 4. Lọc khoảng giá theo toán tử dấu & số tiền nhập linh hoạt
            if (cboToanTuGia?.SelectedItem is ComboBoxItem cbiGia && cbiGia.Tag is string toanTu && toanTu != "ALL")
            {
                decimal? gia1 = LaySoTien(txtGiaNhap1?.Text ?? "");
                decimal? gia2 = LaySoTien(txtGiaNhap2?.Text ?? "");

                if (toanTu == "LTE" && gia1.HasValue)
                {
                    query = query.Where(r => r.Field<decimal>("GiaVeCoSoNum") <= gia1.Value);
                }
                else if (toanTu == "GTE" && gia1.HasValue)
                {
                    query = query.Where(r => r.Field<decimal>("GiaVeCoSoNum") >= gia1.Value);
                }
                else if (toanTu == "BETWEEN")
                {
                    if (gia1.HasValue) query = query.Where(r => r.Field<decimal>("GiaVeCoSoNum") >= gia1.Value);
                    if (gia2.HasValue) query = query.Where(r => r.Field<decimal>("GiaVeCoSoNum") <= gia2.Value);
                }
            }

            // 5. Lọc mác tàu
            string macTau = txtLocMacTau?.Text.Trim() ?? "";
            if (!string.IsNullOrEmpty(macTau))
            {
                query = query.Where(r => (r.Field<string>("SoHieuMacTau") ?? "").IndexOf(macTau, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var list = query.ToList();
            if (list.Count > 0)
            {
                DataTable dtFilter = list.CopyToDataTable();
                dgChuyenTau.ItemsSource = dtFilter.DefaultView;
                dgChuyenTau.SelectedIndex = 0;
            }
            else
            {
                if (dgChuyenTau != null) dgChuyenTau.ItemsSource = null;
                if (spToaXe != null) spToaXe.Children.Clear();
                if (gridNoiThatToa != null) gridNoiThatToa.Children.Clear();
                if (dgLichDungGa != null) dgLichDungGa.ItemsSource = null;
                if (txtTieuDeToaHienTai != null) txtTieuDeToaHienTai.Text = "Không tìm thấy chuyến tàu thỏa mãn bộ lọc nâng cao!";
            }
        }

        private void BtnQuickRoute_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string route)
            {
                switch (route)
                {
                    case "HN-SG":
                        cboGaDi.SelectedValue = 1;
                        cboGaDen.SelectedValue = 27;
                        break;
                    case "HN-VIN":
                        cboGaDi.SelectedValue = 1;
                        cboGaDen.SelectedValue = 7;
                        break;
                    case "HN-DNA":
                        cboGaDi.SelectedValue = 1;
                        cboGaDen.SelectedValue = 14;
                        break;
                    case "SG-NTR":
                        cboGaDi.SelectedValue = 27;
                        cboGaDen.SelectedValue = 22;
                        break;
                    case "SG-DNA":
                        cboGaDi.SelectedValue = 27;
                        cboGaDen.SelectedValue = 14;
                        break;
                }
                TimChuyenTau();
            }
        }

        private void DgChuyenTau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgChuyenTau.SelectedItem is DataRowView row)
            {
                _currentMaChuyenTau = Convert.ToInt32(row["MaChuyenTau"]);
                _currentSoHieuMacTau = row["SoHieuMacTau"].ToString() ?? "";
                LoadDanhSachToaXe();
                LoadLichDungGaChuyenTau();
            }
        }

        private void LoadLichDungGaChuyenTau()
        {
            if (_currentMaChuyenTau <= 0)
            {
                dgLichDungGa.ItemsSource = null;
                return;
            }

            try
            {
                DataTable dtLich = _chuyenTauService.LayLichDungGa(_currentMaChuyenTau);
                dgLichDungGa.ItemsSource = dtLich.DefaultView;
            }
            catch
            {
                dgLichDungGa.ItemsSource = null;
            }
        }

        // =========================================================================
        // 2. BIÊN CHẾ TOA XE & THANH CHỌN TOA
        // =========================================================================
        private void LoadDanhSachToaXe()
        {
            spToaXe.Children.Clear();
            if (_currentMaChuyenTau <= 0) return;

            DataTable dtToa = _veService.LayDanhSachToaTheoChuyen(_currentMaChuyenTau, _currentMaGaDi, _currentMaGaDen);
            if (dtToa.Rows.Count == 0)
            {
                gridNoiThatToa.Children.Clear();
                txtTieuDeToaHienTai.Text = "Chuyến tàu chưa thiết lập biên chế toa!";
                return;
            }

            Border? firstCard = null;
            int firstToaId = 0;
            string firstNhanHieu = "";
            string firstLoaiToa = "";
            string firstTenLoai = "";
            int firstTrong = 0;
            int firstTong = 0;

            foreach (DataRow row in dtToa.Rows)
            {
                int maToa = Convert.ToInt32(row["MaToaXeKhach"]);
                string nhanHieu = row["NhanHieuToa"].ToString() ?? "";
                string loaiToa = row["LoaiToa"].ToString() ?? "";
                string tenLoai = row["TenLoaiToa"].ToString() ?? "";
                int soChoTrong = Convert.ToInt32(row["SoChoTrong"]);
                int tongCho = Convert.ToInt32(row["TongSoCho"]);

                if (firstToaId == 0)
                {
                    firstToaId = maToa;
                    firstNhanHieu = nhanHieu;
                    firstLoaiToa = loaiToa;
                    firstTenLoai = tenLoai;
                    firstTrong = soChoTrong;
                    firstTong = tongCho;
                }

                var bdCard = new Border
                {
                    Tag = maToa,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(0, 0, 6, 0),
                    Cursor = Cursors.Hand
                };

                var spCardContent = new StackPanel { Orientation = Orientation.Horizontal };
                var txtToaName = new TextBlock
                {
                    Text = $"{nhanHieu} ({loaiToa})",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                var bdBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 249, 255)), // #F0F9FF Sky Blue nhạt
                    BorderBrush = new SolidColorBrush(Color.FromRgb(186, 230, 253)), // #BAE6FD
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1)
                };
                var txtBadge = new TextBlock
                {
                    Text = $"{soChoTrong} chỗ",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(2, 132, 199)) // #0284C7 Sky Blue
                };
                bdBadge.Child = txtBadge;

                spCardContent.Children.Add(txtToaName);
                spCardContent.Children.Add(bdBadge);
                bdCard.Child = spCardContent;

                bdCard.MouseLeftButtonDown += (s, ev) =>
                {
                    HighlightCardToa(bdCard);
                    ChonToaXe(maToa, nhanHieu, loaiToa, tenLoai, soChoTrong, tongCho);
                };

                spToaXe.Children.Add(bdCard);
                if (firstCard == null) firstCard = bdCard;
            }

            if (firstCard != null)
            {
                HighlightCardToa(firstCard);
                ChonToaXe(firstToaId, firstNhanHieu, firstLoaiToa, firstTenLoai, firstTrong, firstTong);
            }
        }

        private void HighlightCardToa(Border selected)
        {
            foreach (var child in spToaXe.Children)
            {
                if (child is Border b)
                {
                    b.Background = Brushes.White;
                    b.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                    if (b.Child is StackPanel sp && sp.Children[0] is TextBlock tb)
                    {
                        tb.Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                    }
                }
            }

            selected.Background = new SolidColorBrush(Color.FromRgb(0, 59, 115));
            selected.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 40, 85));
            if (selected.Child is StackPanel spSel && spSel.Children[0] is TextBlock tbSel)
            {
                tbSel.Foreground = Brushes.White;
            }
        }

        private void ChonToaXe(int maToa, string nhanHieu, string loaiToa, string tenLoai, int soTrong, int tongCho)
        {
            _currentMaToaXe = maToa;
            _currentNhanHieuToa = nhanHieu;
            _currentLoaiToa = loaiToa;

            txtTieuDeToaHienTai.Text = $"{nhanHieu} ({tenLoai}) - Còn {soTrong}/{tongCho} chỗ";
            RenderToaTauThucTe();
        }

        // =========================================================================
        // 3. VẼ SƠ ĐỒ CHỖ NGỒI CHUẨN DSVN (KHÔNG VẼ GIẢ LẬP WC / CỬA)
        // =========================================================================
        private void RenderToaTauThucTe()
        {
            gridNoiThatToa.Children.Clear();
            gridNoiThatToa.ColumnDefinitions.Clear();
            gridNoiThatToa.RowDefinitions.Clear();

            if (_currentMaToaXe <= 0) return;

            DataTable dtGhe = _veService.LaySoDoGhe(_currentMaToaXe, _currentMaChuyenTau, _currentMaGaDi, _currentMaGaDen);
            var dictGhe = new Dictionary<int, DataRow>();
            foreach (DataRow r in dtGhe.Rows)
            {
                dictGhe[Convert.ToInt32(r["SoGhe"])] = r;
            }

            FrameworkElement visualNoiThat;
            if (_currentLoaiToa == "AN")
            {
                visualNoiThat = TaoToaGiuongNamKhoang4(dictGhe);
            }
            else if (_currentLoaiToa == "BN")
            {
                visualNoiThat = TaoToaGiuongNamKhoang6(dictGhe);
            }
            else
            {
                visualNoiThat = TaoToaGheNgoi64Cho(dictGhe);
            }

            gridNoiThatToa.Children.Add(visualNoiThat);
        }

        // 3.1. Toa Giường Nằm VIP Khoang 4 (AN - 28 chỗ)
        private FrameworkElement TaoToaGiuongNamKhoang4(Dictionary<int, DataRow> dictGhe)
        {
            var spKhoangList = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Margin = new Thickness(4),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            for (int k = 1; k <= 7; k++)
            {
                int gTraiT1 = (k - 1) * 4 + 1;
                int gTraiT2 = (k - 1) * 4 + 2;
                int gPhaiT1 = (k - 1) * 4 + 3;
                int gPhaiT2 = (k - 1) * 4 + 4;

                var bdKhoang = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(3, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var gridKhoang = new Grid();
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Tiêu đề
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // T2
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // T1

                var txtKhoang = new TextBlock
                {
                    Text = $"KHOANG {k} (VIP)",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 59, 115)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                Grid.SetRow(txtKhoang, 0);
                gridKhoang.Children.Add(txtKhoang);

                // Tầng 2
                var spT2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                spT2.Children.Add(TaoButtonGiuong(dictGhe, gTraiT2, "T2"));
                spT2.Children.Add(new Border { Width = 8 });
                spT2.Children.Add(TaoButtonGiuong(dictGhe, gPhaiT2, "T2"));
                Grid.SetRow(spT2, 1);
                gridKhoang.Children.Add(spT2);

                // Tầng 1
                var spT1 = new StackPanel { Orientation = Orientation.Horizontal };
                spT1.Children.Add(TaoButtonGiuong(dictGhe, gTraiT1, "T1"));
                spT1.Children.Add(new Border { Width = 8 });
                spT1.Children.Add(TaoButtonGiuong(dictGhe, gPhaiT1, "T1"));
                Grid.SetRow(spT1, 2);
                gridKhoang.Children.Add(spT1);

                bdKhoang.Child = gridKhoang;
                spKhoangList.Children.Add(bdKhoang);
            }

            return spKhoangList;
        }

        // 3.2. Toa Giường Nằm Khoang 6 (BN - 42 chỗ)
        private FrameworkElement TaoToaGiuongNamKhoang6(Dictionary<int, DataRow> dictGhe)
        {
            var spKhoangList = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Margin = new Thickness(4),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            for (int k = 1; k <= 7; k++)
            {
                int gTraiT1 = (k - 1) * 6 + 1;
                int gTraiT2 = (k - 1) * 6 + 2;
                int gTraiT3 = (k - 1) * 6 + 3;
                int gPhaiT1 = (k - 1) * 6 + 4;
                int gPhaiT2 = (k - 1) * 6 + 5;
                int gPhaiT3 = (k - 1) * 6 + 6;

                var bdKhoang = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(3, 0, 3, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var gridKhoang = new Grid();
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // T3
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // T2
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // T1

                var txtKhoang = new TextBlock
                {
                    Text = $"KHOANG {k}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 59, 115)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 3)
                };
                Grid.SetRow(txtKhoang, 0);
                gridKhoang.Children.Add(txtKhoang);

                // T3
                var spT3 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };
                spT3.Children.Add(TaoButtonGiuong(dictGhe, gTraiT3, "T3"));
                spT3.Children.Add(new Border { Width = 6 });
                spT3.Children.Add(TaoButtonGiuong(dictGhe, gPhaiT3, "T3"));
                Grid.SetRow(spT3, 1);
                gridKhoang.Children.Add(spT3);

                // T2
                var spT2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };
                spT2.Children.Add(TaoButtonGiuong(dictGhe, gTraiT2, "T2"));
                spT2.Children.Add(new Border { Width = 6 });
                spT2.Children.Add(TaoButtonGiuong(dictGhe, gPhaiT2, "T2"));
                Grid.SetRow(spT2, 2);
                gridKhoang.Children.Add(spT2);

                // T1
                var spT1 = new StackPanel { Orientation = Orientation.Horizontal };
                spT1.Children.Add(TaoButtonGiuong(dictGhe, gTraiT1, "T1"));
                spT1.Children.Add(new Border { Width = 6 });
                spT1.Children.Add(TaoButtonGiuong(dictGhe, gPhaiT1, "T1"));
                Grid.SetRow(spT1, 3);
                gridKhoang.Children.Add(spT1);

                bdKhoang.Child = gridKhoang;
                spKhoangList.Children.Add(bdKhoang);
            }

            return spKhoangList;
        }

        // 3.3. Toa Ngồi Mềm Điều Hòa (NML - 64 chỗ, 16 hàng x 4 ghế)
        private FrameworkElement TaoToaGheNgoi64Cho(Dictionary<int, DataRow> dictGhe)
        {
            var gridToa = new Grid { Margin = new Thickness(2), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            gridToa.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Hàng A (Cửa sổ trên)
            gridToa.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Hàng B (Lối đi trên)
            gridToa.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) }); // Lối đi trung tâm
            gridToa.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Hàng C (Lối đi dưới)
            gridToa.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Hàng D (Cửa sổ dưới)

            var spRowA = new StackPanel { Orientation = Orientation.Horizontal };
            var spRowB = new StackPanel { Orientation = Orientation.Horizontal };
            var spRowC = new StackPanel { Orientation = Orientation.Horizontal };
            var spRowD = new StackPanel { Orientation = Orientation.Horizontal };

            for (int col = 1; col <= 16; col++)
            {
                int gA = (col - 1) * 4 + 1;
                int gB = (col - 1) * 4 + 2;
                int gC = (col - 1) * 4 + 3;
                int gD = (col - 1) * 4 + 4;

                spRowA.Children.Add(TaoButtonGheNgoi(dictGhe, gA, "CS"));
                spRowB.Children.Add(TaoButtonGheNgoi(dictGhe, gB, "LĐ"));
                spRowC.Children.Add(TaoButtonGheNgoi(dictGhe, gC, "LĐ"));
                spRowD.Children.Add(TaoButtonGheNgoi(dictGhe, gD, "CS"));
            }

            Grid.SetRow(spRowA, 0);
            Grid.SetRow(spRowB, 1);

            var bdAisle = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Margin = new Thickness(0, 1, 0, 1),
                Height = 16
            };
            var txtAisle = new TextBlock
            {
                Text = "◄════════════════════════ LỐI ĐI CHÍNH TRUNG TÂM TOA XE ════════════════════════►",
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            bdAisle.Child = txtAisle;
            Grid.SetRow(bdAisle, 2);

            Grid.SetRow(spRowC, 3);
            Grid.SetRow(spRowD, 4);

            gridToa.Children.Add(spRowA);
            gridToa.Children.Add(spRowB);
            gridToa.Children.Add(bdAisle);
            gridToa.Children.Add(spRowC);
            gridToa.Children.Add(spRowD);

            return gridToa;
        }

        private Button TaoButtonGiuong(Dictionary<int, DataRow> dictGhe, int soGhe, string tenTang)
        {
            var btn = new Button
            {
                Width = 62,
                Height = 35,
                Margin = new Thickness(2),
                Cursor = Cursors.Hand
            };

            DinhDangGiaoDienCho(btn, dictGhe, soGhe, tenTang);
            return btn;
        }

        private Button TaoButtonGheNgoi(Dictionary<int, DataRow> dictGhe, int soGhe, string viTri)
        {
            var btn = new Button
            {
                Width = 46,
                Height = 34,
                Margin = new Thickness(1),
                Cursor = Cursors.Hand
            };

            DinhDangGiaoDienCho(btn, dictGhe, soGhe, viTri);
            return btn;
        }

        private void DinhDangGiaoDienCho(Button btn, Dictionary<int, DataRow> dictGhe, int soGhe, string phuDe)
        {
            if (!dictGhe.ContainsKey(soGhe))
            {
                btn.IsEnabled = false;
                btn.Visibility = Visibility.Hidden;
                return;
            }

            var r = dictGhe[soGhe];
            int maChoNgoi = Convert.ToInt32(r["MaChoNgoi"]);
            int? tangGiuong = r["TangGiuong"] != DBNull.Value ? Convert.ToInt32(r["TangGiuong"]) : null;
            bool daDat = Convert.ToInt32(r["DaDat"]) == 1;
            bool dangChon = _gioVe.Any(x => x.MaChoNgoi == maChoNgoi);

            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            var txtSo = new TextBlock
            {
                Text = soGhe < 10 ? $"0{soGhe}" : $"{soGhe}",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var txtSub = new TextBlock
            {
                Text = phuDe,
                FontSize = 8,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0)
            };

            sp.Children.Add(txtSo);
            sp.Children.Add(txtSub);
            btn.Content = sp;

            if (daDat)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                btn.BorderThickness = new Thickness(1);
                txtSo.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                txtSub.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                btn.IsEnabled = false;
                btn.ToolTip = $"Ghế #{soGhe}: Đã có khách đặt trên chặng này";
            }
            else if (dangChon)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(234, 88, 12)); // #EA580C Cam Đậm VNR
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(194, 65, 12)); // #C2410C
                btn.BorderThickness = new Thickness(1.5);
                txtSo.Foreground = Brushes.White;
                txtSub.Foreground = Brushes.White;
                btn.ToolTip = $"Ghế #{soGhe}: Đang chọn trong giỏ vé";
            }
            else
            {
                btn.Background = Brushes.White;
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199)); // #0284C7 Sky Blue thanh lịch
                btn.BorderThickness = new Thickness(1.5);
                txtSo.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)); // #0F172A Đen than rõ nét
                txtSub.Foreground = new SolidColorBrush(Color.FromRgb(2, 132, 199));
                btn.ToolTip = $"Ghế #{soGhe}: Trống (Click chọn đặt)";
            }

            btn.Click += (s, ev) =>
            {
                ToggleChonGhe(maChoNgoi, soGhe, tangGiuong);
            };
        }

        private void ToggleChonGhe(int maChoNgoi, int soGhe, int? tangGiuong)
        {
            var existing = _gioVe.FirstOrDefault(x => x.MaChoNgoi == maChoNgoi);
            if (existing != null)
            {
                _gioVe.Remove(existing);
            }
            else
            {
                decimal giaGoc = _veService.TinhGiaVe(_currentLoaiToa, _currentMaGaDi, _currentMaGaDen, tangGiuong);
                string loaiToaDesc = _currentLoaiToa switch
                {
                    "NC" => "Ngồi cứng",
                    "NML" => "Ngồi mềm ĐH",
                    "BN" => $"Nằm K6 (T{tangGiuong})",
                    "AN" => $"Nằm VIP K4 (T{tangGiuong})",
                    _ => _currentLoaiToa
                };

                _gioVe.Add(new GioVeItem
                {
                    MaChoNgoi = maChoNgoi,
                    SoGhe = soGhe,
                    TangGiuong = tangGiuong,
                    LoaiToa = _currentLoaiToa,
                    NhanHieuToa = _currentNhanHieuToa,
                    LoaiToaMoTa = loaiToaDesc,
                    MaToaXeKhach = _currentMaToaXe,
                    MaChuyenTau = _currentMaChuyenTau,
                    SoHieuMacTau = _currentSoHieuMacTau,
                    MaGaDi = _currentMaGaDi,
                    MaGaDen = _currentMaGaDen,
                    GiaGoc = giaGoc,
                    SoTienGiam = 0,
                    GiaThucThu = giaGoc,
                    TenHanhKhach = "",
                    CCCDHanhKhach = "",
                    LoaiKhach = "THUONG"
                });
            }

            RenderToaTauThucTe();
            RenderGioVe();
            CapNhatTongTien();
        }

        // =========================================================================
        // 4. GIỎ VÉ ĐANG ĐẶT & HÀNH KHÁCH
        // =========================================================================
        private void RenderGioVe()
        {
            spGioVe.Children.Clear();
            txtSoLuongGioVe.Text = $"{_gioVe.Count} vé";

            if (_gioVe.Count == 0)
            {
                bdEmptyGioVe.Visibility = Visibility.Visible;
                svGioVeList.Visibility = Visibility.Collapsed;
                return;
            }

            bdEmptyGioVe.Visibility = Visibility.Collapsed;
            svGioVeList.Visibility = Visibility.Visible;

            for (int i = 0; i < _gioVe.Count; i++)
            {
                var item = _gioVe[i];
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                var gridCard = new Grid();
                gridCard.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridCard.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridCard.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridCard.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var rowHeader = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                rowHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var txtTieuDe = new TextBlock
                {
                    Text = $"Tàu {item.SoHieuMacTau} - {item.NhanHieuToa} - Ghế {item.SoGhe} ({item.LoaiToaMoTa})",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 59, 115)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtTieuDe, 0);

                var btnXoa = new Button
                {
                    Width = 22,
                    Height = 22,
                    Padding = new Thickness(0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Content = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Dismiss24, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)) }
                };
                btnXoa.Click += (s, ev) =>
                {
                    _gioVe.Remove(item);
                    RenderToaTauThucTe();
                    RenderGioVe();
                    CapNhatTongTien();
                };
                Grid.SetColumn(btnXoa, 1);

                rowHeader.Children.Add(txtTieuDe);
                rowHeader.Children.Add(btnXoa);
                Grid.SetRow(rowHeader, 0);

                var rowTen = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                rowTen.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
                rowTen.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lblTen = new TextBlock { Text = "Họ Tên:", Style = (Style)FindResource("FieldLabel") };
                if (i == 0 && string.IsNullOrWhiteSpace(item.TenHanhKhach) && !string.IsNullOrWhiteSpace(txtNguoiMuaTen.Text))
                {
                    item.TenHanhKhach = txtNguoiMuaTen.Text.Trim();
                }
                var txtTen = new TextBox { Text = item.TenHanhKhach, Height = 24, FontSize = 11 };
                txtTen.TextChanged += (s, ev) =>
                {
                    item.TenHanhKhach = txtTen.Text.Trim();
                    if (_gioVe.Count == 1 && (string.IsNullOrWhiteSpace(txtNguoiMuaTen.Text) || txtNguoiMuaTen.Tag?.ToString() == "AutoSync" || txtNguoiMuaTen.Text != item.TenHanhKhach))
                    {
                        txtNguoiMuaTen.Tag = "AutoSync";
                        txtNguoiMuaTen.Text = item.TenHanhKhach;
                        txtNguoiMuaTen.Tag = null;
                    }
                };

                Grid.SetColumn(lblTen, 0);
                Grid.SetColumn(txtTen, 1);
                rowTen.Children.Add(lblTen);
                rowTen.Children.Add(txtTen);
                Grid.SetRow(rowTen, 1);

                var rowCccd = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                rowCccd.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
                rowCccd.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lblCccd = new TextBlock { Text = "CCCD:", Style = (Style)FindResource("FieldLabel") };
                var txtCccd = new TextBox { Text = item.CCCDHanhKhach, Height = 24, FontSize = 11, FontFamily = new FontFamily("Consolas") };
                txtCccd.TextChanged += (s, ev) => item.CCCDHanhKhach = txtCccd.Text.Trim();

                Grid.SetColumn(lblCccd, 0);
                Grid.SetColumn(txtCccd, 1);
                rowCccd.Children.Add(lblCccd);
                rowCccd.Children.Add(txtCccd);
                Grid.SetRow(rowCccd, 2);

                var rowGia = new Grid();
                rowGia.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
                rowGia.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGia.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var lblDoiTuong = new TextBlock { Text = "Đối tượng:", Style = (Style)FindResource("FieldLabel") };
                var cboDoiTuong = new ComboBox { Height = 24, FontSize = 10 };
                cboDoiTuong.Items.Add(new ComboBoxItem { Content = "Người lớn (100%)", Tag = "THUONG" });
                cboDoiTuong.Items.Add(new ComboBoxItem { Content = "Sinh viên (-10%)", Tag = "SINH_VIEN" });
                cboDoiTuong.Items.Add(new ComboBoxItem { Content = "Người già (-15%)", Tag = "NGUOI_GIA" });
                cboDoiTuong.Items.Add(new ComboBoxItem { Content = "Trẻ em (-25%)", Tag = "TRE_EM" });
                cboDoiTuong.SelectedIndex = item.LoaiKhach switch
                {
                    "SINH_VIEN" => 1,
                    "NGUOI_GIA" => 2,
                    "TRE_EM" => 3,
                    _ => 0
                };

                var txtTienVe = new TextBlock
                {
                    Text = $"{item.GiaThucThu:N0} đ",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(234, 88, 12)), // #EA580C Cam VNR
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0)
                };

                cboDoiTuong.SelectionChanged += (s, ev) =>
                {
                    if (cboDoiTuong.SelectedItem is ComboBoxItem cbi && cbi.Tag is string tag)
                    {
                        item.LoaiKhach = tag;
                        item.SoTienGiam = _veService.TinhMucGiamGia(item.GiaGoc, tag);
                        item.GiaThucThu = Math.Max(0, item.GiaGoc - item.SoTienGiam);
                        txtTienVe.Text = $"{item.GiaThucThu:N0} đ";
                        CapNhatTongTien();
                    }
                };

                Grid.SetColumn(lblDoiTuong, 0);
                Grid.SetColumn(cboDoiTuong, 1);
                Grid.SetColumn(txtTienVe, 2);
                rowGia.Children.Add(lblDoiTuong);
                rowGia.Children.Add(cboDoiTuong);
                rowGia.Children.Add(txtTienVe);
                Grid.SetRow(rowGia, 3);

                gridCard.Children.Add(rowHeader);
                gridCard.Children.Add(rowTen);
                gridCard.Children.Add(rowCccd);
                gridCard.Children.Add(rowGia);

                card.Child = gridCard;
                spGioVe.Children.Add(card);
            }
        }

        private void CapNhatTongTien()
        {
            decimal tongGoc = _gioVe.Sum(x => x.GiaGoc);
            decimal tongGiam = _gioVe.Sum(x => x.SoTienGiam);
            decimal thucThu = Math.Max(0, tongGoc - tongGiam);

            txtTongTienGoc.Text = $"{tongGoc:N0} VNĐ";
            txtTongGiamGia.Text = tongGiam > 0 ? $"- {tongGiam:N0} VNĐ" : "0 VNĐ";
            txtThucThu.Text = $"{thucThu:N0} VNĐ";

            CapNhatVietQr(thucThu);
        }

        private void CboHinhThucTT_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboHinhThucTT.SelectedItem is ComboBoxItem item && 
                (item.Tag?.ToString() == "CHUYEN_KHOAN" || item.Content.ToString()!.Contains("Chuyển Khoản")))
            {
                bdVietQrBox.Visibility = Visibility.Visible;
                decimal thucThu = _gioVe.Sum(x => x.GiaThucThu);
                CapNhatVietQr(thucThu);
            }
            else
            {
                if (bdVietQrBox != null) bdVietQrBox.Visibility = Visibility.Collapsed;
            }
        }

        private void CapNhatVietQr(decimal tongTien)
        {
            if (bdVietQrBox == null || bdVietQrBox.Visibility != Visibility.Visible || tongTien <= 0) return;

            try
            {
                string noiDung = $"VNR VETAU {DateTime.Now:HHmm}";
                txtVietQrNoiDung.Text = $"Nội dung: {noiDung}";

                string qrPayload = $"2|99|00020101021238540010A00000072701240006970415011001234567890208QRIBFTTA5303704540{tongTien:0}5802VN62180814{noiDung}6304";
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrPayload, QRCodeGenerator.ECCLevel.M);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(8);

                var bitmap = new BitmapImage();
                using (var ms = new MemoryStream(qrBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                }
                bitmap.Freeze();
                imgVietQrPos.Source = bitmap;
            }
            catch
            {
                // Bo qua loi render QR
            }
        }

        private void BtnXoaGio_Click(object sender, RoutedEventArgs e)
        {
            if (_gioVe.Count == 0) return;
            if (ThongBaoDialog.XacNhan("Bạn có chắc chắn muốn hủy toàn bộ ghế đang chọn trong giỏ vé?", "Xác Nhận Hủy Giỏ", "Hủy Toàn Bộ", "Giữ Lại", laHanhDongXoa: true))
            {
                _gioVe.Clear();
                RenderToaTauThucTe();
                RenderGioVe();
                CapNhatTongTien();
            }
        }

        // =========================================================================
        // 5. XÁC NHẬN THANH TOÁN & XUẤT VÉ (F12)
        // =========================================================================
        private void BtnThanhToan_Click(object sender, RoutedEventArgs e)
        {
            if (_gioVe.Count == 0)
            {
                ThongBaoDialog.CanhBao("Giỏ vé đang trống. Vui lòng chọn ghế trước khi thanh toán!", "Giỏ Vé Trống");
                return;
            }

            string tenNguoiMua = txtNguoiMuaTen.Text.Trim();
            string sdtNguoiMua = txtNguoiMuaSdt.Text.Trim();

            if (string.IsNullOrWhiteSpace(tenNguoiMua))
            {
                if (!string.IsNullOrWhiteSpace(_gioVe[0].TenHanhKhach))
                {
                    tenNguoiMua = _gioVe[0].TenHanhKhach;
                    txtNguoiMuaTen.Text = tenNguoiMua;
                }
                else
                {
                    ThongBaoDialog.CanhBao("Vui lòng nhập họ tên người đại diện mua vé!", "Thiếu Thông Tin");
                    txtNguoiMuaTen.Focus();
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(sdtNguoiMua))
            {
                ThongBaoDialog.CanhBao("Vui lòng nhập số điện thoại người đại diện mua vé để nhận mã vé / PNR!", "Thiếu Số Điện Thoại");
                txtNguoiMuaSdt.Focus();
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(sdtNguoiMua, @"^0\d{9}$"))
            {
                ThongBaoDialog.CanhBao("Số điện thoại không hợp lệ! Vui lòng nhập đúng định dạng 10 chữ số (bắt đầu bằng số 0).", "SĐT Không Hợp Lệ");
                txtNguoiMuaSdt.Focus();
                return;
            }

            for (int i = 0; i < _gioVe.Count; i++)
            {
                var g = _gioVe[i];
                if (string.IsNullOrWhiteSpace(g.TenHanhKhach))
                {
                    ThongBaoDialog.CanhBao($"Vui lòng nhập Họ tên hành khách cho vé Toa {g.NhanHieuToa} Ghế {g.SoGhe}!", "Thiếu Thông Tin");
                    return;
                }
                if (string.IsNullOrWhiteSpace(g.CCCDHanhKhach))
                {
                    ThongBaoDialog.CanhBao($"Vui lòng nhập số CCCD cho hành khách {g.TenHanhKhach} theo quy định Luật ĐS 2017!", "Thiếu Thông Tin");
                    return;
                }
                if (!System.Text.RegularExpressions.Regex.IsMatch(g.CCCDHanhKhach, @"^\d{12}$"))
                {
                    ThongBaoDialog.CanhBao($"Số CCCD của hành khách {g.TenHanhKhach} (Toa {g.NhanHieuToa} Ghế {g.SoGhe}) không hợp lệ!\nCCCD phải gồm đúng 12 chữ số.", "CCCD Không Hợp Lệ");
                    return;
                }
            }

            var khachHang = new KhachHang
            {
                HoTen = tenNguoiMua,
                SoCCCD = _gioVe[0].CCCDHanhKhach,
                SoDienThoai = sdtNguoiMua,
                LoaiKhach = _gioVe[0].LoaiKhach
            };

            string httt = "TIEN_MAT";
            if (cboHinhThucTT.SelectedItem is ComboBoxItem cbi)
            {
                httt = cbi.Tag?.ToString() ?? cbi.Content?.ToString()?.Split('-')[0].Trim() ?? "TIEN_MAT";
            }

            decimal tongTien = _gioVe.Sum(x => x.GiaThucThu);

            var donVe = new DonDatVe
            {
                SoLuongVe = _gioVe.Count,
                TongTien = tongTien,
                HinhThucTT = httt,
                TrangThaiTT = "DA_THANH_TOAN",
                MaNhanVienBan = 3
            };

            var listVe = new List<Ve>();
            foreach (var item in _gioVe)
            {
                listVe.Add(new Ve
                {
                    MaChuyenTau = item.MaChuyenTau,
                    MaChoNgoi = item.MaChoNgoi,
                    MaGaDi = item.MaGaDi,
                    MaGaDen = item.MaGaDen,
                    TenHanhKhach = item.TenHanhKhach,
                    CCCDHanhKhach = item.CCCDHanhKhach,
                    GiaVeGoc = item.GiaGoc,
                    SoTienGiam = item.SoTienGiam,
                    GiaVeThucThu = item.GiaThucThu,
                    TrangThai = "DA_DAT"
                });
            }

            bool thanhCong = _veService.DatVeTheoDon(khachHang, donVe, listVe, _currentSoHieuMacTau, out string maPNR, out string err);

            if (thanhCong)
            {
                ThongBaoDialog.ThanhCong($"Thanh toán thành công đơn đặt vé #{maPNR}!\nĐã xuất {_gioVe.Count} vé điện tử hợp lệ.", "Xuất Vé Thành Công");

                if (listVe.Count == 1)
                {
                    var dlg = new TheLenTauDialog { Owner = Window.GetWindow(this) };
                    string tenGaDi = (cboGaDi.SelectedItem as DataRowView)?["TenGa"]?.ToString() ?? "Hà Nội";
                    string tenGaDen = (cboGaDen.SelectedItem as DataRowView)?["TenGa"]?.ToString() ?? "Sài Gòn";

                    dlg.SetThongTinVe(
                        maPNR,
                        listVe[0].MaVeCode,
                        _currentSoHieuMacTau,
                        tenGaDi,
                        tenGaDen,
                        DateTime.Now.ToString("HH:mm - dd/MM/yyyy"),
                        listVe[0].TenHanhKhach,
                        listVe[0].CCCDHanhKhach,
                        $"{_gioVe[0].NhanHieuToa} ({_gioVe[0].LoaiToaMoTa})",
                        _gioVe[0].SoGhe,
                        listVe[0].GiaVeThucThu);

                    dlg.ShowDialog();
                }
                else if (listVe.Count > 1)
                {
                    bool inTatCa = ThongBaoDialog.XacNhan(
                        $"Đơn vé #{maPNR} có {listVe.Count} vé hành khách.\nBạn có muốn xem và in lần lượt Thẻ lên tàu cho tất cả {listVe.Count} hành khách ngay bây giờ không?",
                        "In Thẻ Lên Tàu Điện Tử",
                        "In Toàn Bộ",
                        "Để Sau");

                    if (inTatCa)
                    {
                        string tenGaDi = (cboGaDi.SelectedItem as DataRowView)?["TenGa"]?.ToString() ?? "Hà Nội";
                        string tenGaDen = (cboGaDen.SelectedItem as DataRowView)?["TenGa"]?.ToString() ?? "Sài Gòn";

                        for (int i = 0; i < listVe.Count; i++)
                        {
                            var dlg = new TheLenTauDialog { Owner = Window.GetWindow(this) };
                            dlg.SetThongTinVe(
                                maPNR,
                                listVe[i].MaVeCode,
                                _currentSoHieuMacTau,
                                tenGaDi,
                                tenGaDen,
                                DateTime.Now.ToString("HH:mm - dd/MM/yyyy"),
                                listVe[i].TenHanhKhach,
                                listVe[i].CCCDHanhKhach,
                                $"{_gioVe[i].NhanHieuToa} ({_gioVe[i].LoaiToaMoTa})",
                                _gioVe[i].SoGhe,
                                listVe[i].GiaVeThucThu);

                            dlg.ShowDialog();
                        }
                    }
                }

                _gioVe.Clear();
                txtNguoiMuaTen.Text = "";
                txtNguoiMuaSdt.Text = "";
                RenderGioVe();
                CapNhatTongTien();

                RenderToaTauThucTe();
                LoadDanhSachVeTab2();
            }
            else
            {
                ThongBaoDialog.Loi($"Thanh toán thất bại: {err}", "Lỗi Thanh Toán");
            }
        }

        // =========================================================================
        // 6. SỔ TRA CỨU & HOÀN TRẢ VÉ (TAB 2)
        // =========================================================================
        private void LoadDanhSachVeTab2()
        {
            try
            {
                _cachedDanhSachVe = _veService.LayDanhSach();
                dgVe.ItemsSource = _cachedDanhSachVe.DefaultView;
                txtTongSoVePhatHanh.Text = $"Tổng số: {_cachedDanhSachVe.Rows.Count} vé đã phát hành";

                if (dgVe.Items.Count > 0 && dgVe.SelectedIndex < 0)
                {
                    dgVe.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi tải danh sách vé: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void BtnTimKiemVe_Click(object sender, RoutedEventArgs e)
        {
            string kw = txtTimKiemVe.Text.Trim();
            _cachedDanhSachVe = _veService.TimKiemVe(kw);
            dgVe.ItemsSource = _cachedDanhSachVe.DefaultView;
            txtTongSoVePhatHanh.Text = $"Kết quả: {_cachedDanhSachVe.Rows.Count} vé";
        }

        private void BtnNapLaiDanhSach_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiemVe.Text = "";
            LoadDanhSachVeTab2();
        }

        private void DgVe_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void BtnInTheLenTauTab2_Click(object sender, RoutedEventArgs e)
        {
            if (dgVe.SelectedItem is DataRowView row)
            {
                var dlg = new TheLenTauDialog { Owner = Window.GetWindow(this) };
                decimal thu = row["GiaVeThucThu"] != DBNull.Value ? Convert.ToDecimal(row["GiaVeThucThu"]) : 0;

                dlg.SetThongTinVe(
                    row["MaPNR"].ToString() ?? "PNR-000",
                    row["MaVeCode"].ToString() ?? "TK-000",
                    row["SoHieuMacTau"].ToString() ?? "",
                    row["TenGaDi"].ToString() ?? "",
                    row["TenGaDen"].ToString() ?? "",
                    Convert.ToDateTime(row["ThoiDiemXuatVe"]).ToString("HH:mm - dd/MM/yyyy"),
                    row["TenHanhKhach"].ToString() ?? "",
                    row["CCCDHanhKhach"].ToString() ?? "",
                    $"{row["NhanHieuToa"]} ({row["LoaiToa"]})",
                    Convert.ToInt32(row["SoGhe"]),
                    thu);

                dlg.ShowDialog();
            }
            else
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn một vé trên danh sách để in thẻ lên tàu!", "Chưa Chọn Vé");
            }
        }

        private void BtnHoanVeTab2_Click(object sender, RoutedEventArgs e)
        {
            if (dgVe.SelectedItem is DataRowView row)
            {
                int maVe = Convert.ToInt32(row["MaVe"]);
                string maCode = row["MaVeCode"].ToString() ?? "";
                string tt = row["TrangThai"].ToString() ?? "";

                if (tt == "DA_HOAN_VE")
                {
                    ThongBaoDialog.CanhBao("Vé này đã làm thủ tục hoàn trả trước đó!", "Vé Đã Hoàn Trả");
                    return;
                }

                decimal giaGoc = Convert.ToDecimal(row["GiaVeGoc"]);
                decimal lePhi = giaGoc * 0.10m;
                decimal thucHoan = giaGoc - lePhi;

                bool xacNhan = ThongBaoDialog.XacNhan(
                    $"XÁC NHẬN LÀM THỦ TỤC HOÀN TRẢ VÉ #{maCode}?\n\n" +
                    $"- Khách hàng: {row["TenHanhKhach"]}\n" +
                    $"- Tiền vé gốc: {giaGoc:N0} VNĐ\n" +
                    $"- Lệ phí khấu trừ (10%): {lePhi:N0} VNĐ\n" +
                    $"- Số tiền thực hoàn lại: {thucHoan:N0} VNĐ\n\n" +
                    $"Chỗ ngồi sẽ được giải phóng ngay lập tức trên hệ thống!",
                    "Thủ Tục Hoàn Trả Vé Chuẩn VNR",
                    "Hoàn Trả Vé",
                    "Đóng",
                    laHanhDongXoa: true);

                if (xacNhan)
                {
                    if (_veService.HoanVe(maVe, 0.10m, "Khách hàng yêu cầu hoàn vé tại quầy ga", "TIEN_MAT", 3, out string err))
                    {
                        ThongBaoDialog.ThanhCong($"Đã hoàn trả vé #{maCode} thành công!\nĐã giải phóng chỗ ngồi.", "Hoàn Vé Thành Công");
                        LoadDanhSachVeTab2();
                        RenderToaTauThucTe();
                    }
                    else
                    {
                        ThongBaoDialog.Loi($"Lỗi hoàn vé: {err}", "Thất Bại");
                    }
                }
            }
            else
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn một vé trên danh sách để hoàn vé!", "Chưa Chọn Vé");
            }
        }

        private void TabMainBanVe_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        public void KichHoatThemMoi()
        {
            tabMainBanVe.SelectedIndex = 0;
            txtNguoiMuaTen.Focus();
        }

        public void FocusTimKiem()
        {
            tabMainBanVe.SelectedIndex = 1;
            txtTimKiemVe.Focus();
            txtTimKiemVe.SelectAll();
        }

        public void KichHoatNapLai()
        {
            TimChuyenTau();
            LoadDanhSachVeTab2();
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
                    box.Padding = new Thickness(4, 1, 2, 1);
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

    public class GioVeItem
    {
        public int MaChoNgoi { get; set; }
        public int SoGhe { get; set; }
        public int? TangGiuong { get; set; }
        public string LoaiToa { get; set; } = "";
        public string NhanHieuToa { get; set; } = "";
        public string LoaiToaMoTa { get; set; } = "";
        public int MaToaXeKhach { get; set; }
        public int MaChuyenTau { get; set; }
        public string SoHieuMacTau { get; set; } = "";
        public int MaGaDi { get; set; }
        public int MaGaDen { get; set; }
        public decimal GiaGoc { get; set; }
        public decimal SoTienGiam { get; set; }
        public decimal GiaThucThu { get; set; }
        public string TenHanhKhach { get; set; } = "";
        public string CCCDHanhKhach { get; set; } = "";
        public string LoaiKhach { get; set; } = "THUONG";
    }
}
