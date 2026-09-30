using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BUS.Services;
using ET.VanTai;
using GUI.Views.Dialogs;
using GUI.Helpers;
using GUI.Views.UserControls;
using QRCoder;
using Button = System.Windows.Controls.Button;
using SymbolIcon = Wpf.Ui.Controls.SymbolIcon;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

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
        private int _currentPageVe = 1;
        private int _pageSizeVe = 50;
        private DataTable? _cachedTableVePaged;
        private Border? _currentSelectedToaCard;
        private DispatcherTimer? _toastTimer;

        private readonly List<GioVeItem> _gioVe = new();
        private readonly Dictionary<int, Action> _seatVisualUpdateMap = new();

        private int _currentMaChuyenTau = 0;
        private string _currentSoHieuMacTau = "";
        private string _currentLoaiTau = "TAU_CHO";
        private int _currentMaGaDi = 0;
        private int _currentMaGaDen = 0;
        private int _currentMaToaXe = 0;
        private string _currentNhanHieuToa = "";
        private string _currentLoaiToa = "";
        private DataTable? _cachedDsToa;
        private string _filterLoaiToaPopup = "ALL";
        private readonly Dictionary<int, Border> _mapCardToa = new();
        private DateTime _lastClosedPopupToaTime = DateTime.MinValue;
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

                ChonGaMacDinhLinhHoat(cboGaDi, new[] { "Hà Nội", "HNO" }, 0);
                ChonGaMacDinhLinhHoat(cboGaDen, new[] { "Sài Gòn", "SGO", "Hồ Chí Minh" }, _cachedDsGa.Rows.Count - 1);
                dpNgayDi.Language = System.Windows.Markup.XmlLanguage.GetLanguage("vi-VN");
                dpNgayDi.SelectedDate = DateTime.Today;
                dpNgayDi.Loaded += (s, e) => ChuanHoaDatePicker(dpNgayDi);
                dpNgayDi.SelectedDateChanged += (s, e) => ChuanHoaDatePicker(dpNgayDi);
                Dispatcher.BeginInvoke(new Action(() => ChuanHoaDatePicker(dpNgayDi)), System.Windows.Threading.DispatcherPriority.Loaded);

                txtNguoiMuaTen.LostFocus += (s, e) => ValidateNguoiMuaTen();
                txtNguoiMuaSdt.LostFocus += (s, e) => ValidateNguoiMuaSdt();
                txtNguoiMuaSdt.TextChanged += (s, e) =>
                {
                    ClearNguoiMuaSdtError();
                    if (_gioVe.Count > 0 && !string.IsNullOrWhiteSpace(txtNguoiMuaTen.Text) && !string.IsNullOrWhiteSpace(txtNguoiMuaSdt.Text))
                    {
                        UpdateStepIndicator(5);
                    }
                };

                txtNguoiMuaTen.TextChanged += (s, e) =>
                {
                    ClearNguoiMuaTenError();
                    if (_gioVe.Count == 1 && txtNguoiMuaTen.Tag == null)
                    {
                        _gioVe[0].TenHanhKhach = txtNguoiMuaTen.Text.Trim();
                        if (spGioVe.Children.Count > 0 && spGioVe.Children[0] is Border b)
                        {
                            Grid? targetGrid = null;
                            if (b.Child is Grid g) targetGrid = g;
                            else if (b.Child is StackPanel spOuter && spOuter.Children.Count > 1 && spOuter.Children[1] is Border bdBody && bdBody.Child is Grid gBody) targetGrid = gBody;

                            if (targetGrid != null)
                            {
                                foreach (var row in targetGrid.Children)
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
                    }

                    if (_gioVe.Count > 0 && !string.IsNullOrWhiteSpace(txtNguoiMuaTen.Text) && !string.IsNullOrWhiteSpace(txtNguoiMuaSdt.Text))
                    {
                        UpdateStepIndicator(5);
                    }
                };

                UpdateStepIndicator(1);
                if (toastNotification != null)
                {
                    toastNotification.MouseLeftButtonDown += (s, e) => toastNotification.Visibility = Visibility.Collapsed;
                }

                TimChuyenTau();
                LoadDanhSachVeTab2();
                RenderGioVe();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi nạp dữ liệu ban đầu: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void ChonGaMacDinhLinhHoat(SearchableComboBox cbo, string[] tuKhoaUuTien, int fallbackIndex)
        {
            if (_cachedDsGa == null || _cachedDsGa.Rows.Count == 0) return;

            foreach (DataRow row in _cachedDsGa.Rows)
            {
                string tenGa = row["TenGa"]?.ToString() ?? "";
                string maCode = _cachedDsGa.Columns.Contains("MaGaCode") ? (row["MaGaCode"]?.ToString() ?? "") : "";

                foreach (var kw in tuKhoaUuTien)
                {
                    if (tenGa.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (!string.IsNullOrEmpty(maCode) && maCode.Equals(kw, StringComparison.OrdinalIgnoreCase)))
                    {
                        cbo.SelectedValue = row["MaGa"];
                        return;
                    }
                }
            }

            int safeIndex = Math.Clamp(fallbackIndex, 0, _cachedDsGa.Rows.Count - 1);
            cbo.SelectedValue = _cachedDsGa.Rows[safeIndex]["MaGa"];
        }

        private void CboGa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            TimChuyenTau();
        }

        private void UpdateStepIndicator(int activeStep)
        {
            if (bdStep1Circle == null) return;

            var steps = new (Border Circle, SymbolIcon Icon, TextBlock Txt, SymbolRegular DefaultIcon)[]
            {
                (bdStep1Circle, iconStep1, txtStep1, SymbolRegular.Search24),
                (bdStep2Circle, iconStep2, txtStep2, SymbolRegular.VehicleSubway24),
                (bdStep3Circle, iconStep3, txtStep3, SymbolRegular.TicketDiagonal24),
                (bdStep4Circle, iconStep4, txtStep4, SymbolRegular.Person24),
                (bdStep5Circle, iconStep5, txtStep5, SymbolRegular.Payment24)
            };

            var lines = new[] { lineStep12, lineStep23, lineStep34, lineStep45 };

            for (int i = 0; i < steps.Length; i++)
            {
                int stepNum = i + 1;
                var (circle, icon, txt, defIcon) = steps[i];

                if (stepNum < activeStep)
                {
                    circle.Background = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                    icon.Symbol = SymbolRegular.Checkmark24;
                    icon.Foreground = Brushes.White;
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                    txt.FontWeight = FontWeights.Bold;
                }
                else if (stepNum == activeStep)
                {
                    circle.Background = new SolidColorBrush(Color.FromRgb(0, 59, 115));
                    icon.Symbol = defIcon;
                    icon.Foreground = Brushes.White;
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(0, 59, 115));
                    txt.FontWeight = FontWeights.Bold;
                }
                else
                {
                    circle.Background = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                    icon.Symbol = defIcon;
                    icon.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                    txt.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                    txt.FontWeight = FontWeights.SemiBold;
                }
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null) continue;
                if (i + 1 < activeStep)
                {
                    lines[i].Background = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                }
                else
                {
                    lines[i].Background = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                }
            }
        }

        private void ShowToast(string message, bool isSuccess = true)
        {
            if (toastNotification == null || toastMessage == null || toastIcon == null) return;

            toastMessage.Text = message;
            if (isSuccess)
            {
                toastIcon.Symbol = SymbolRegular.CheckmarkCircle24;
                toastIcon.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                toastNotification.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                toastNotification.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85));
            }
            else
            {
                toastIcon.Symbol = SymbolRegular.Warning24;
                toastIcon.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                toastNotification.Background = new SolidColorBrush(Color.FromRgb(69, 26, 3));
                toastNotification.BorderBrush = new SolidColorBrush(Color.FromRgb(146, 64, 14));
            }

            toastNotification.Visibility = Visibility.Visible;
            toastNotification.Opacity = 0;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            toastNotification.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            _toastTimer?.Stop();
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            _toastTimer.Tick += (s, ev) =>
            {
                _toastTimer.Stop();
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
                fadeOut.Completed += (s2, e2) => toastNotification.Visibility = Visibility.Collapsed;
                toastNotification.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            };
            _toastTimer.Start();
        }

        private async void BtnTimChuyen_Click(object sender, RoutedEventArgs e) => await TimChuyenTauAsync();

        private async void TimChuyenTau() => await TimChuyenTauAsync();

        private async Task TimChuyenTauAsync()
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

            if (loadingOverlay != null)
            {
                txtLoadingText.Text = "Đang tìm kiếm chuyến tàu khả dụng...";
                loadingOverlay.Visibility = Visibility.Visible;
            }

            try
            {
                DataTable dt = await Task.Run(() =>
                {
                    DataTable res = _veService.TimChuyenTauTheoChang(gaDi, gaDen, ngayDi);

                    if (!res.Columns.Contains("LoTrinhChay"))
                        res.Columns.Add("LoTrinhChay", typeof(string));
                    if (!res.Columns.Contains("TenLoaiTau"))
                        res.Columns.Add("TenLoaiTau", typeof(string));
                    if (!res.Columns.Contains("ThoiGianChay"))
                        res.Columns.Add("ThoiGianChay", typeof(string));
                    if (!res.Columns.Contains("ChoConLai"))
                        res.Columns.Add("ChoConLai", typeof(string));
                    if (!res.Columns.Contains("SoChoConLai"))
                        res.Columns.Add("SoChoConLai", typeof(int));
                    if (!res.Columns.Contains("GioDiHour"))
                        res.Columns.Add("GioDiHour", typeof(int));
                    if (!res.Columns.Contains("GioDenHour"))
                        res.Columns.Add("GioDenHour", typeof(int));
                    if (!res.Columns.Contains("GiaVeCoSoNum"))
                        res.Columns.Add("GiaVeCoSoNum", typeof(decimal));

                    foreach (DataRow row in res.Rows)
                    {
                        row["LoTrinhChay"] = $"{row["TenGaXuatPhat"]} → {row["TenGaKetThuc"]}";

                        string lt = row.Table.Columns.Contains("LoaiTau") ? (row["LoaiTau"]?.ToString() ?? "") : "";
                        row["TenLoaiTau"] = lt switch
                        {
                            "TAU_NHANH" => "Tàu nhanh SE",
                            "TAU_CHO" => "Tàu địa phương",
                            "TAU_HANG" => "Tàu hàng",
                            _ => "Tàu khách"
                        };

                        if (row["GioXuatPhatKH"] != DBNull.Value && row["GioVeDichKH"] != DBNull.Value)
                        {
                            DateTime dtDi = Convert.ToDateTime(row["GioXuatPhatKH"]);
                            DateTime dtDen = Convert.ToDateTime(row["GioVeDichKH"]);
                            TimeSpan dur = dtDen - dtDi;
                            if (dur.TotalMinutes > 0)
                            {
                                int hours = (int)dur.TotalHours;
                                int mins = dur.Minutes;
                                row["ThoiGianChay"] = mins > 0 ? $"{hours}h{mins:D2}p" : $"{hours} giờ";
                            }
                            else
                            {
                                row["ThoiGianChay"] = "Toàn tuyến";
                            }
                        }
                        else
                        {
                            row["ThoiGianChay"] = "Toàn tuyến";
                        }

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

                    return res;
                });

                _cachedChuyenTau = dt;
                ApDungBoLocChuyenTau();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi nạp danh sách chuyến tàu: {ex.Message}", "Lỗi CSDL");
            }
            finally
            {
                if (loadingOverlay != null)
                {
                    loadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
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
                    cboToanTuGia.SelectedIndex = 1;
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

            if (_chiLocConCho)
            {
                query = query.Where(r => r.Field<int>("SoChoConLai") > 0);
            }

            int idxGioDi = cboLocGioDi?.SelectedIndex ?? 0;
            if (idxGioDi == 1) query = query.Where(r => r.Field<int>("GioDiHour") >= 0 && r.Field<int>("GioDiHour") < 6);
            else if (idxGioDi == 2) query = query.Where(r => r.Field<int>("GioDiHour") >= 6 && r.Field<int>("GioDiHour") < 12);
            else if (idxGioDi == 3) query = query.Where(r => r.Field<int>("GioDiHour") >= 12 && r.Field<int>("GioDiHour") < 18);
            else if (idxGioDi == 4) query = query.Where(r => r.Field<int>("GioDiHour") >= 18 && r.Field<int>("GioDiHour") < 24);

            int idxGioDen = cboLocGioDen?.SelectedIndex ?? 0;
            if (idxGioDen == 1) query = query.Where(r => r.Field<int>("GioDenHour") >= 0 && r.Field<int>("GioDenHour") < 12);
            else if (idxGioDen == 2) query = query.Where(r => r.Field<int>("GioDenHour") >= 12 && r.Field<int>("GioDenHour") < 18);
            else if (idxGioDen == 3) query = query.Where(r => r.Field<int>("GioDenHour") >= 18 && r.Field<int>("GioDenHour") < 24);

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
                UpdateStepIndicator(2);
                dgChuyenTau.SelectedIndex = 0;
            }
            else
            {
                if (dgChuyenTau != null) dgChuyenTau.ItemsSource = null;
                if (spToaXe != null) spToaXe.Children.Clear();
                if (gridNoiThatToa != null) gridNoiThatToa.Children.Clear();
                if (bdEmptySoDo != null) bdEmptySoDo.Visibility = Visibility.Visible;
                if (svSoDoToa != null) svSoDoToa.Visibility = Visibility.Collapsed;
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
                _currentLoaiTau = row.Row.Table.Columns.Contains("LoaiTau") ? (row["LoaiTau"]?.ToString() ?? "TAU_CHO") : "TAU_CHO";
                UpdateStepIndicator(3);
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

        private void LoadDanhSachToaXe()
        {
            spToaXe.Children.Clear();
            _currentSelectedToaCard = null;
            _mapCardToa.Clear();

            if (_currentMaChuyenTau <= 0)
            {
                _cachedDsToa = null;
                if (bdEmptySoDo != null) bdEmptySoDo.Visibility = Visibility.Visible;
                if (svSoDoToa != null) svSoDoToa.Visibility = Visibility.Collapsed;
                if (txtNhanNutTimToa != null) txtNhanNutTimToa.Text = "Toa (+)";
                return;
            }

            DataTable dtToa = _veService.LayDanhSachToaTheoChuyen(_currentMaChuyenTau, _currentMaGaDi, _currentMaGaDen);
            _cachedDsToa = dtToa;

            if (txtNhanNutTimToa != null) txtNhanNutTimToa.Text = $"Toa ({dtToa.Rows.Count})";
            if (txtPopupTongSoToa != null) txtPopupTongSoToa.Text = $"Đoàn tàu: {dtToa.Rows.Count} toa xe";
            if (btnMoKhungTimToa != null) btnMoKhungTimToa.ToolTip = $"Danh sách toàn bộ {dtToa.Rows.Count} toa xe";

            if (dtToa.Rows.Count == 0)
            {
                gridNoiThatToa.Children.Clear();
                txtTieuDeToaHienTai.Text = "Chuyến tàu chưa thiết lập biên chế toa!";
                if (bdEmptySoDo != null) bdEmptySoDo.Visibility = Visibility.Visible;
                if (svSoDoToa != null) svSoDoToa.Visibility = Visibility.Collapsed;
                return;
            }

            Border? firstCard = null;
            int firstToaId = 0;
            string firstNhanHieu = "";
            string firstLoaiToa = "";
            string firstTenLoai = "";
            int firstTrong = 0;
            int firstTong = 0;

            // 1. Thêm biểu tượng Đầu Máy Xe Lửa (Locomotive) chỉ hướng chạy đoàn tàu
            var bdDauMay = new Border
            {
                Background = VnrWpfBrushes.Navy,
                BorderBrush = VnrWpfBrushes.NavyDark,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(9, 4, 9, 4),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var spDauMay = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            spDauMay.Children.Add(new SymbolIcon
            {
                Symbol = SymbolRegular.VehicleSubway24,
                FontSize = 13,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 5, 0)
            });
            spDauMay.Children.Add(new TextBlock
            {
                Text = "ĐẦU MÁY ➔",
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            bdDauMay.Child = spDauMay;
            spToaXe.Children.Add(bdDauMay);

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
                    BorderBrush = VnrWpfBrushes.SlateBorder,
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(9, 4, 9, 4),
                    Margin = new Thickness(0, 0, 6, 0),
                    Cursor = Cursors.Hand
                };

                // Làm sạch nhãn hiệu toa, loại bỏ trường hợp lặp nhãn "(AN) (AN)"
                string soToaText = nhanHieu;
                int parenIdx = nhanHieu.IndexOf('(');
                if (parenIdx > 0)
                {
                    soToaText = nhanHieu.Substring(0, parenIdx).Trim();
                }
                string tenLoaiMieuTa = loaiToa switch
                {
                    "AN" => "Nằm VIP K4",
                    "BN" => "Nằm K6",
                    "NML" => "Ngồi mềm ĐH",
                    "NC" => "Ngồi cứng",
                    _ => tenLoai
                };

                var spCardContent = new StackPanel { Orientation = Orientation.Horizontal };
                var txtToaName = new TextBlock
                {
                    Text = $"{soToaText} • {tenLoaiMieuTa}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = VnrWpfBrushes.TextDark,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };

                var bdBadge = new Border
                {
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(5, 1, 5, 1),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var spBadgeInner = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var iconBadge = new SymbolIcon { FontSize = 9, Margin = new Thickness(0, 0, 3, 0) };
                var txtBadge = new TextBlock { FontSize = 9.5, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center };

                if (soChoTrong == 0)
                {
                    bdBadge.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
                    bdBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202));
                    iconBadge.Symbol = SymbolRegular.DismissCircle24;
                    iconBadge.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                    txtBadge.Text = "Hết";
                    txtBadge.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                }
                else if (soChoTrong <= 10)
                {
                    bdBadge.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                    bdBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(253, 230, 138));
                    iconBadge.Symbol = SymbolRegular.Warning24;
                    iconBadge.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                    txtBadge.Text = $"{soChoTrong} chỗ";
                    txtBadge.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                }
                else
                {
                    bdBadge.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                    bdBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(134, 239, 172));
                    iconBadge.Symbol = SymbolRegular.CheckmarkCircle24;
                    iconBadge.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                    txtBadge.Text = $"{soChoTrong} chỗ";
                    txtBadge.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                }

                spBadgeInner.Children.Add(iconBadge);
                spBadgeInner.Children.Add(txtBadge);
                bdBadge.Child = spBadgeInner;

                spCardContent.Children.Add(txtToaName);
                spCardContent.Children.Add(bdBadge);
                bdCard.Child = spCardContent;

                bdCard.MouseEnter += (s, ev) =>
                {
                    if (bdCard != _currentSelectedToaCard)
                    {
                        bdCard.Background = VnrWpfBrushes.SlateCanvas;
                        bdCard.BorderBrush = VnrWpfBrushes.TextLightMuted;
                    }
                };
                bdCard.MouseLeave += (s, ev) =>
                {
                    if (bdCard != _currentSelectedToaCard)
                    {
                        bdCard.Background = Brushes.White;
                        bdCard.BorderBrush = VnrWpfBrushes.SlateBorder;
                    }
                };

                bdCard.MouseLeftButtonDown += (s, ev) =>
                {
                    HighlightCardToa(bdCard);
                    ChonToaXe(maToa, nhanHieu, loaiToa, tenLoai, soChoTrong, tongCho);
                };

                _mapCardToa[maToa] = bdCard;
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
            _currentSelectedToaCard = selected;
            foreach (var child in spToaXe.Children)
            {
                if (child is Border b && b.Tag != null)
                {
                    b.Background = Brushes.White;
                    b.BorderBrush = VnrWpfBrushes.SlateBorder;
                    if (b.Child is StackPanel sp && sp.Children.Count > 0 && sp.Children[0] is TextBlock tb)
                    {
                        tb.Foreground = VnrWpfBrushes.TextDark;
                    }
                }
            }

            selected.Background = VnrWpfBrushes.Navy;
            selected.BorderBrush = VnrWpfBrushes.NavyDark;
            if (selected.Child is StackPanel spSel && spSel.Children.Count > 0 && spSel.Children[0] is TextBlock tbSel)
            {
                tbSel.Foreground = Brushes.White;
            }
        }

        private void CuonToaVaoTamMat(Border card)
        {
            if (svToaXe == null || card == null) return;
            try
            {
                GeneralTransform transform = card.TransformToAncestor(spToaXe);
                Point pt = transform.Transform(new Point(0, 0));
                double cardLeft = pt.X;
                double cardRight = cardLeft + card.ActualWidth;

                double currentOffset = svToaXe.HorizontalOffset;
                double viewportWidth = svToaXe.ViewportWidth;

                if (cardLeft < currentOffset)
                {
                    svToaXe.ScrollToHorizontalOffset(Math.Max(0, cardLeft - 20));
                }
                else if (viewportWidth > 0 && cardRight > currentOffset + viewportWidth)
                {
                    svToaXe.ScrollToHorizontalOffset(cardRight - viewportWidth + 20);
                }
            }
            catch { }
        }

        private void BtnCuonToaTrai_Click(object sender, RoutedEventArgs e)
        {
            if (svToaXe != null)
                svToaXe.ScrollToHorizontalOffset(Math.Max(0, svToaXe.HorizontalOffset - 180));
        }

        private void BtnCuonToaPhai_Click(object sender, RoutedEventArgs e)
        {
            if (svToaXe != null)
                svToaXe.ScrollToHorizontalOffset(svToaXe.HorizontalOffset + 180);
        }

        private void SvToaXe_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (svToaXe != null && e.Delta != 0)
            {
                svToaXe.ScrollToHorizontalOffset(svToaXe.HorizontalOffset - (e.Delta > 0 ? 80 : -80));
                e.Handled = true;
            }
        }

        private void BtnMoKhungTimToa_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            // Chống hiện tượng chớp tắt khi vừa đóng do click ngoài cách đây dưới 250ms
            if ((DateTime.UtcNow - _lastClosedPopupToaTime).TotalMilliseconds < 250)
            {
                return;
            }

            if (_cachedDsToa == null || _cachedDsToa.Rows.Count == 0)
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn chuyến tàu để xem danh sách biên chế toa!", "Chưa Chọn Chuyến Tàu");
                return;
            }

            if (popTimChonToa.IsOpen)
            {
                popTimChonToa.IsOpen = false;
                return;
            }

            RenderDanhSachToaPopup();
            popTimChonToa.IsOpen = true;
        }

        private void PopTimChonToa_Opened(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtTimToa?.Focus();
                if (txtTimToa != null) Keyboard.Focus(txtTimToa);
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void PopTimChonToa_Closed(object sender, EventArgs e)
        {
            _lastClosedPopupToaTime = DateTime.UtcNow;
        }

        private void BtnDongPopupToa_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            popTimChonToa.IsOpen = false;
        }

        private void TxtTimToa_TextChanged(object sender, TextChangedEventArgs e)
        {
            string kw = txtTimToa?.Text ?? "";
            bool hasText = !string.IsNullOrEmpty(kw);
            if (txtTimToaPlaceholder != null) txtTimToaPlaceholder.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
            if (btnClearTimToa != null) btnClearTimToa.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

            RenderDanhSachToaPopup();
        }

        private void BtnClearTimToa_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (txtTimToa != null)
            {
                txtTimToa.Text = "";
                txtTimToa.Focus();
            }
        }

        private void ChipLoc_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (sender is Border b && b.Tag is string tag)
            {
                _filterLoaiToaPopup = tag;

                var allChips = new[] { chipLocTatCa, chipLocK4, chipLocK6, chipLocNM, chipLocConCho };
                foreach (var chip in allChips)
                {
                    if (chip == null) continue;
                    bool isActive = (chip.Tag as string) == tag;
                    chip.Background = isActive ? VnrWpfBrushes.Navy : new SolidColorBrush(Color.FromRgb(241, 245, 249));
                    chip.BorderBrush = isActive ? VnrWpfBrushes.Navy : new SolidColorBrush(Color.FromRgb(203, 213, 225));
                    if (chip.Child is TextBlock tb)
                    {
                        tb.Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(71, 85, 105));
                    }
                }

                RenderDanhSachToaPopup();
            }
        }

        private void RenderDanhSachToaPopup()
        {
            if (spDanhSachToaPopup == null) return;
            spDanhSachToaPopup.Children.Clear();

            if (_cachedDsToa == null || _cachedDsToa.Rows.Count == 0)
            {
                var txtEmpty = new TextBlock
                {
                    Text = "Không có thông tin toa xe!",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 11,
                    Margin = new Thickness(10)
                };
                spDanhSachToaPopup.Children.Add(txtEmpty);
                return;
            }

            string keyword = txtTimToa?.Text?.Trim() ?? "";
            string normKw = SearchableComboBox.RemoveDiacritics(keyword).ToLowerInvariant();

            int matchedCount = 0;
            foreach (DataRow row in _cachedDsToa.Rows)
            {
                int maToa = Convert.ToInt32(row["MaToaXeKhach"]);
                string nhanHieu = row["NhanHieuToa"].ToString() ?? "";
                string loaiToa = row["LoaiToa"].ToString() ?? "";
                string tenLoai = row["TenLoaiToa"].ToString() ?? "";
                int soChoTrong = Convert.ToInt32(row["SoChoTrong"]);
                int tongCho = Convert.ToInt32(row["TongSoCho"]);

                // Lọc theo Quick Chip
                if (_filterLoaiToaPopup == "AN" && loaiToa != "AN") continue;
                if (_filterLoaiToaPopup == "BN" && loaiToa != "BN") continue;
                if (_filterLoaiToaPopup == "NML" && loaiToa != "NML" && loaiToa != "NC") continue;
                if (_filterLoaiToaPopup == "CON_CHO" && soChoTrong == 0) continue;

                // Lọc theo Text Search
                if (!string.IsNullOrEmpty(keyword))
                {
                    string searchableText = $"{nhanHieu} {loaiToa} {tenLoai} {soChoTrong} chỗ";
                    string normText = SearchableComboBox.RemoveDiacritics(searchableText).ToLowerInvariant();
                    if (!normText.Contains(normKw)) continue;
                }

                matchedCount++;

                bool isSelected = (_currentMaToaXe == maToa);

                var bdItem = new Border
                {
                    Background = isSelected ? new SolidColorBrush(Color.FromRgb(238, 242, 255)) : Brushes.White,
                    BorderBrush = isSelected ? VnrWpfBrushes.Navy : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(isSelected ? 1.5 : 1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 6, 8, 6),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand
                };

                var gridItem = new Grid();
                gridItem.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
                gridItem.ColumnDefinitions.Add(new GridLength(1, GridUnitType.Star) is var w1 ? new ColumnDefinition { Width = w1 } : null!);
                gridItem.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Cột 0: Khối số toa
                var bdNum = new Border
                {
                    Background = isSelected ? VnrWpfBrushes.Navy : new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(4, 2, 4, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                string soToaRong = nhanHieu;
                int paren = nhanHieu.IndexOf('(');
                if (paren > 0) soToaRong = nhanHieu.Substring(0, paren).Trim();

                var txtNum = new TextBlock
                {
                    Text = soToaRong,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = isSelected ? Brushes.White : VnrWpfBrushes.Navy,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                bdNum.Child = txtNum;
                Grid.SetColumn(bdNum, 0);
                gridItem.Children.Add(bdNum);

                // Cột 1: Thông tin loại toa
                var spInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
                string tenMieuTa = loaiToa switch
                {
                    "AN" => "Nằm VIP K4 (Điều hòa)",
                    "BN" => "Nằm K6 (Điều hòa)",
                    "NML" => "Ngồi mềm ĐH",
                    "NC" => "Ngồi cứng",
                    _ => tenLoai
                };
                var txtTenLoai = new TextBlock
                {
                    Text = tenMieuTa,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11,
                    Foreground = isSelected ? VnrWpfBrushes.Navy : VnrWpfBrushes.TextDark
                };
                var txtSub = new TextBlock
                {
                    Text = $"Biên chế chính • {tongCho} chỗ tổng cộng",
                    FontSize = 9.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
                };
                spInfo.Children.Add(txtTenLoai);
                spInfo.Children.Add(txtSub);
                Grid.SetColumn(spInfo, 1);
                gridItem.Children.Add(spInfo);

                // Cột 2: Badge số chỗ
                var bdBadge = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var txtBadge = new TextBlock { FontSize = 10, FontWeight = FontWeights.Bold };
                if (soChoTrong == 0)
                {
                    bdBadge.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
                    txtBadge.Text = "Hết chỗ";
                    txtBadge.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                }
                else if (soChoTrong <= 5)
                {
                    bdBadge.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
                    txtBadge.Text = $"Còn {soChoTrong} chỗ";
                    txtBadge.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                }
                else
                {
                    bdBadge.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                    txtBadge.Text = $"Còn {soChoTrong}/{tongCho}";
                    txtBadge.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                }
                bdBadge.Child = txtBadge;
                Grid.SetColumn(bdBadge, 2);
                gridItem.Children.Add(bdBadge);

                bdItem.Child = gridItem;

                bdItem.MouseEnter += (s, e) =>
                {
                    if (_currentMaToaXe != maToa)
                    {
                        bdItem.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                        bdItem.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225));
                    }
                };
                bdItem.MouseLeave += (s, e) =>
                {
                    if (_currentMaToaXe != maToa)
                    {
                        bdItem.Background = Brushes.White;
                        bdItem.BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                    }
                };

                bdItem.PreviewMouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    popTimChonToa.IsOpen = false;
                    ChonToaXe(maToa, nhanHieu, loaiToa, tenLoai, soChoTrong, tongCho);
                    if (_mapCardToa.TryGetValue(maToa, out var cardOnBar))
                    {
                        HighlightCardToa(cardOnBar);
                        CuonToaVaoTamMat(cardOnBar);
                    }
                };

                spDanhSachToaPopup.Children.Add(bdItem);
            }

            if (matchedCount == 0)
            {
                var txtNoMatch = new TextBlock
                {
                    Text = "Không tìm thấy toa nào phù hợp tiêu chí!",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    FontSize = 11,
                    Margin = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                spDanhSachToaPopup.Children.Add(txtNoMatch);
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

        private void RenderToaTauThucTe()
        {
            _seatVisualUpdateMap.Clear();
            gridNoiThatToa.Children.Clear();
            gridNoiThatToa.ColumnDefinitions.Clear();
            gridNoiThatToa.RowDefinitions.Clear();

            if (_currentMaToaXe <= 0)
            {
                if (bdEmptySoDo != null) bdEmptySoDo.Visibility = Visibility.Visible;
                if (svSoDoToa != null) svSoDoToa.Visibility = Visibility.Collapsed;
                return;
            }

            DataTable dtGhe = _veService.LaySoDoGhe(_currentMaToaXe, _currentMaChuyenTau, _currentMaGaDi, _currentMaGaDen);
            if (dtGhe == null || dtGhe.Rows.Count == 0)
            {
                if (bdEmptySoDo != null) bdEmptySoDo.Visibility = Visibility.Visible;
                if (svSoDoToa != null) svSoDoToa.Visibility = Visibility.Collapsed;
                return;
            }

            if (bdEmptySoDo != null) bdEmptySoDo.Visibility = Visibility.Collapsed;
            if (svSoDoToa != null) svSoDoToa.Visibility = Visibility.Visible;

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
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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

                var spT2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
                spT2.Children.Add(TaoButtonGiuong(dictGhe, gTraiT2, "T2"));
                spT2.Children.Add(new Border { Width = 8 });
                spT2.Children.Add(TaoButtonGiuong(dictGhe, gPhaiT2, "T2"));
                Grid.SetRow(spT2, 1);
                gridKhoang.Children.Add(spT2);

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
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridKhoang.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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

                var spT3 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };
                spT3.Children.Add(TaoButtonGiuong(dictGhe, gTraiT3, "T3"));
                spT3.Children.Add(new Border { Width = 6 });
                spT3.Children.Add(TaoButtonGiuong(dictGhe, gPhaiT3, "T3"));
                Grid.SetRow(spT3, 1);
                gridKhoang.Children.Add(spT3);

                var spT2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };
                spT2.Children.Add(TaoButtonGiuong(dictGhe, gTraiT2, "T2"));
                spT2.Children.Add(new Border { Width = 6 });
                spT2.Children.Add(TaoButtonGiuong(dictGhe, gPhaiT2, "T2"));
                Grid.SetRow(spT2, 2);
                gridKhoang.Children.Add(spT2);

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

        private FrameworkElement TaoToaGheNgoi64Cho(Dictionary<int, DataRow> dictGhe)
        {
            var gridToa = new Grid { Margin = new Thickness(2), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            gridToa.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            gridToa.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            gridToa.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });
            gridToa.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            gridToa.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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

                spRowA.Children.Add(TaoButtonGheNgoi(dictGhe, gA, "Cửa sổ"));
                spRowB.Children.Add(TaoButtonGheNgoi(dictGhe, gB, "Lối đi"));
                spRowC.Children.Add(TaoButtonGheNgoi(dictGhe, gC, "Lối đi"));
                spRowD.Children.Add(TaoButtonGheNgoi(dictGhe, gD, "Cửa sổ"));
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
                Height = 36,
                Margin = new Thickness(2),
                Padding = new Thickness(0),
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
                Height = 36,
                Margin = new Thickness(1),
                Padding = new Thickness(0),
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

            var sp = new StackPanel 
            { 
                VerticalAlignment = VerticalAlignment.Center, 
                HorizontalAlignment = HorizontalAlignment.Center 
            };

            var headrest = new Border
            {
                Width = 24,
                Height = 3,
                CornerRadius = new CornerRadius(1.5),
                Background = VnrWpfBrushes.HeadrestDefault,
                Margin = new Thickness(0, 0, 0, 2),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var txtSo = new TextBlock
            {
                Text = soGhe < 10 ? $"0{soGhe}" : $"{soGhe}",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var txtSub = new TextBlock
            {
                Text = phuDe,
                FontSize = 8,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            sp.Children.Add(headrest);
            sp.Children.Add(txtSo);
            sp.Children.Add(txtSub);
            btn.Content = sp;

            void UpdateSeatVisual()
            {
                bool dangChon = _gioVe.Any(x => x.MaChoNgoi == maChoNgoi);
                if (daDat)
                {
                    btn.Background = VnrWpfBrushes.SlateCanvas;
                    btn.BorderBrush = VnrWpfBrushes.SlateLight;
                    btn.BorderThickness = new Thickness(1);
                    headrest.Background = VnrWpfBrushes.SlateLight;
                    txtSo.Foreground = VnrWpfBrushes.TextDisabled;
                    txtSub.Foreground = VnrWpfBrushes.TextDisabled;
                    btn.IsEnabled = false;
                    btn.ToolTip = $"Ghế #{soGhe}: Đã có khách đặt trên chặng này";
                }
                else if (dangChon)
                {
                    btn.Background = VnrWpfBrushes.Orange;
                    btn.BorderBrush = VnrWpfBrushes.OrangeBorder;
                    btn.BorderThickness = new Thickness(1.5);
                    headrest.Background = VnrWpfBrushes.OrangeHeadrest;
                    txtSo.Foreground = Brushes.White;
                    txtSub.Foreground = VnrWpfBrushes.OrangeHeadrest;
                    btn.ToolTip = $"Ghế #{soGhe}: Đang chọn trong giỏ vé";
                }
                else
                {
                    btn.Background = Brushes.White;
                    btn.BorderBrush = VnrWpfBrushes.SlateBorder;
                    btn.BorderThickness = new Thickness(1);
                    headrest.Background = VnrWpfBrushes.HeadrestDefault;
                    txtSo.Foreground = VnrWpfBrushes.TextDark;
                    txtSub.Foreground = VnrWpfBrushes.TextMuted;
                    btn.ToolTip = $"Ghế #{soGhe} ({phuDe}): Còn trống (Click chọn đặt)";
                }
            }

            _seatVisualUpdateMap[maChoNgoi] = UpdateSeatVisual;
            UpdateSeatVisual();

            if (!daDat)
            {
                btn.MouseEnter += (s, ev) =>
                {
                    bool isSelected = _gioVe.Any(x => x.MaChoNgoi == maChoNgoi);
                    if (!isSelected)
                    {
                        btn.Background = VnrWpfBrushes.SkyLight;
                        btn.BorderBrush = VnrWpfBrushes.Sky;
                    }
                };
                btn.MouseLeave += (s, ev) =>
                {
                    bool isSelected = _gioVe.Any(x => x.MaChoNgoi == maChoNgoi);
                    if (!isSelected)
                    {
                        btn.Background = Brushes.White;
                        btn.BorderBrush = VnrWpfBrushes.SlateBorder;
                    }
                };
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
                decimal giaGoc = _veService.TinhGiaVe(_currentLoaiToa, _currentMaGaDi, _currentMaGaDen, tangGiuong, _currentLoaiTau);
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

            if (_gioVe.Count > 0)
            {
                UpdateStepIndicator(4);
            }
            else
            {
                UpdateStepIndicator(3);
            }

            // Cập nhật cực nhanh chỉ riêng ghế được click, không hủy và vẽ lại toàn bộ 64 ghế
            if (_seatVisualUpdateMap.TryGetValue(maChoNgoi, out var updateVisual))
            {
                updateVisual();
            }
            else
            {
                RenderToaTauThucTe();
            }

            RenderGioVe();
            CapNhatTongTien();
        }

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
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)), 
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Margin = new Thickness(0, 0, 0, 8),
                    SnapsToDevicePixels = true,
                    UseLayoutRounding = true
                };

                var spOuter = new StackPanel();

                var bdHeader = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0, 59, 115)),
                    CornerRadius = new CornerRadius(5, 5, 0, 0),
                    Padding = new Thickness(10, 6, 8, 6)
                };

                var gridHeader = new Grid();
                gridHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                gridHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var spTitle = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                spTitle.Children.Add(new SymbolIcon { Symbol = SymbolRegular.TicketDiagonal24, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(186, 230, 253)), Margin = new Thickness(0, 0, 6, 0) });
                spTitle.Children.Add(new TextBlock
                {
                    Text = $"VÉ #{i + 1}: TÀU {item.SoHieuMacTau} - {item.NhanHieuToa}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11.5,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn(spTitle, 0);

                var btnXoa = new Button
                {
                    Width = 22,
                    Height = 22,
                    Padding = new Thickness(0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Xóa vé này khỏi giỏ",
                    Content = new SymbolIcon { Symbol = SymbolRegular.Dismiss24, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)) }
                };
                btnXoa.MouseEnter += (s, ev) =>
                {
                    btnXoa.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                    if (btnXoa.Content is SymbolIcon ico) ico.Foreground = Brushes.White;
                };
                btnXoa.MouseLeave += (s, ev) =>
                {
                    btnXoa.Background = Brushes.Transparent;
                    if (btnXoa.Content is SymbolIcon ico) ico.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                };
                btnXoa.Click += (s, ev) =>
                {
                    int maChoXoa = item.MaChoNgoi;
                    _gioVe.Remove(item);
                    if (_gioVe.Count == 0) UpdateStepIndicator(3);

                    if (_seatVisualUpdateMap.TryGetValue(maChoXoa, out var updateVisual))
                    {
                        updateVisual();
                    }
                    else
                    {
                        RenderToaTauThucTe();
                    }

                    RenderGioVe();
                    CapNhatTongTien();
                };
                Grid.SetColumn(btnXoa, 1);

                gridHeader.Children.Add(spTitle);
                gridHeader.Children.Add(btnXoa);
                bdHeader.Child = gridHeader;
                spOuter.Children.Add(bdHeader);

                var bdBody = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(0, 0, 5, 5),
                    Padding = new Thickness(10, 8, 10, 8)
                };

                var gridBody = new Grid();
                gridBody.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridBody.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridBody.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                gridBody.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var bdSeatBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(240, 249, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(186, 230, 253)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 3, 6, 3),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                var spSeatInfo = new StackPanel { Orientation = Orientation.Horizontal };
                spSeatInfo.Children.Add(new SymbolIcon { Symbol = SymbolRegular.Grid24, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(2, 132, 199)), Margin = new Thickness(0, 0, 6, 0) });
                spSeatInfo.Children.Add(new TextBlock
                {
                    Text = $"Ghế #{item.SoGhe} • {item.LoaiToaMoTa}",
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(3, 105, 161)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                bdSeatBadge.Child = spSeatInfo;
                Grid.SetRow(bdSeatBadge, 0);
                gridBody.Children.Add(bdSeatBadge);

                var rowTen = new Grid { Margin = new Thickness(0, 0, 0, 5) };
                rowTen.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
                rowTen.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lblTen = new TextBlock { Text = "Họ Tên:", Style = (Style)FindResource("FieldLabel"), VerticalAlignment = VerticalAlignment.Center };
                if (i == 0 && string.IsNullOrWhiteSpace(item.TenHanhKhach) && !string.IsNullOrWhiteSpace(txtNguoiMuaTen.Text))
                {
                    item.TenHanhKhach = txtNguoiMuaTen.Text.Trim();
                }
                var txtTen = new TextBox { Text = item.TenHanhKhach, Height = 28, FontSize = 12, VerticalContentAlignment = VerticalAlignment.Center };
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
                gridBody.Children.Add(rowTen);

                var rowCccd = new Grid { Margin = new Thickness(0, 0, 0, 5) };
                rowCccd.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
                rowCccd.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var lblCccd = new TextBlock { Text = "CCCD:", Style = (Style)FindResource("FieldLabel"), VerticalAlignment = VerticalAlignment.Center };
                var txtCccd = new TextBox { Text = item.CCCDHanhKhach, Height = 28, FontSize = 12, FontFamily = new FontFamily("Consolas"), VerticalContentAlignment = VerticalAlignment.Center };
                txtCccd.TextChanged += (s, ev) => item.CCCDHanhKhach = txtCccd.Text.Trim();

                Grid.SetColumn(lblCccd, 0);
                Grid.SetColumn(txtCccd, 1);
                rowCccd.Children.Add(lblCccd);
                rowCccd.Children.Add(txtCccd);
                Grid.SetRow(rowCccd, 2);
                gridBody.Children.Add(rowCccd);

                var rowGia = new Grid();
                rowGia.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
                rowGia.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGia.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var lblDoiTuong = new TextBlock { Text = "Đối tượng:", Style = (Style)FindResource("FieldLabel"), VerticalAlignment = VerticalAlignment.Center };
                var cboDoiTuong = new ComboBox { Height = 28, FontSize = 11, VerticalContentAlignment = VerticalAlignment.Center };
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
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(234, 88, 12)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
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
                gridBody.Children.Add(rowGia);

                bdBody.Child = gridBody;
                spOuter.Children.Add(bdBody);
                card.Child = spOuter;
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
            }
        }

        private void BtnXoaGio_Click(object sender, RoutedEventArgs e)
        {
            if (_gioVe.Count == 0) return;
            if (ThongBaoDialog.XacNhan("Bạn có chắc chắn muốn hủy toàn bộ ghế đang chọn trong giỏ vé?", "Xác Nhận Hủy Giỏ", "Hủy Toàn Bộ", "Giữ Lại", laHanhDongXoa: true))
            {
                _gioVe.Clear();
                ClearNguoiMuaTenError();
                ClearNguoiMuaSdtError();
                UpdateStepIndicator(3);
                RenderToaTauThucTe();
                RenderGioVe();
                CapNhatTongTien();
                ShowToast("Đã hủy toàn bộ ghế trong giỏ vé.", isSuccess: false);
            }
        }

        private bool ValidateNguoiMuaTen()
        {
            string ten = txtNguoiMuaTen.Text.Trim();
            if (string.IsNullOrWhiteSpace(ten))
            {
                if (_gioVe.Count > 0 && !string.IsNullOrWhiteSpace(_gioVe[0].TenHanhKhach))
                {
                    txtNguoiMuaTen.Text = _gioVe[0].TenHanhKhach;
                    ClearNguoiMuaTenError();
                    return true;
                }
                lblErrNguoiMuaTen.Text = "Vui lòng nhập họ tên người mua vé";
                lblErrNguoiMuaTen.Visibility = Visibility.Visible;
                txtNguoiMuaTen.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                return false;
            }
            ClearNguoiMuaTenError();
            return true;
        }

        private void ClearNguoiMuaTenError()
        {
            lblErrNguoiMuaTen.Text = "";
            lblErrNguoiMuaTen.Visibility = Visibility.Collapsed;
            txtNguoiMuaTen.ClearValue(Control.BorderBrushProperty);
        }

        private bool ValidateNguoiMuaSdt()
        {
            string sdt = txtNguoiMuaSdt.Text.Trim();
            if (string.IsNullOrWhiteSpace(sdt))
            {
                lblErrNguoiMuaSdt.Text = "Vui lòng nhập số điện thoại";
                lblErrNguoiMuaSdt.Visibility = Visibility.Visible;
                txtNguoiMuaSdt.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                return false;
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(sdt, @"^0\d{9}$"))
            {
                lblErrNguoiMuaSdt.Text = "SĐT phải gồm 10 chữ số (bắt đầu bằng 0)";
                lblErrNguoiMuaSdt.Visibility = Visibility.Visible;
                txtNguoiMuaSdt.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                return false;
            }
            ClearNguoiMuaSdtError();
            return true;
        }

        private void ClearNguoiMuaSdtError()
        {
            lblErrNguoiMuaSdt.Text = "";
            lblErrNguoiMuaSdt.Visibility = Visibility.Collapsed;
            txtNguoiMuaSdt.ClearValue(Control.BorderBrushProperty);
        }

        private void BtnThanhToan_Click(object sender, RoutedEventArgs e)
        {
            if (_gioVe.Count == 0)
            {
                ThongBaoDialog.CanhBao("Giỏ vé đang trống. Vui lòng chọn ghế trước khi thanh toán!", "Giỏ Vé Trống");
                return;
            }

            bool hopLeTen = ValidateNguoiMuaTen();
            bool hopLeSdt = ValidateNguoiMuaSdt();

            if (!hopLeTen)
            {
                txtNguoiMuaTen.Focus();
                return;
            }

            if (!hopLeSdt)
            {
                txtNguoiMuaSdt.Focus();
                return;
            }

            string tenNguoiMua = txtNguoiMuaTen.Text.Trim();
            string sdtNguoiMua = txtNguoiMuaSdt.Text.Trim();

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
                ShowToast($"Thanh toán thành công đơn vé #{maPNR}! Đã xuất {_gioVe.Count} vé điện tử hợp lệ.", isSuccess: true);

                if (listVe.Count == 1)
                {
                    var dlg = new TheLenTauDialog { Owner = Window.GetWindow(this) };
                    string tenGaDi = (cboGaDi.SelectedItem as DataRowView)?["TenGa"]?.ToString() ?? cboGaDi.Text;
                    string tenGaDen = (cboGaDen.SelectedItem as DataRowView)?["TenGa"]?.ToString() ?? cboGaDen.Text;

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
                        string tenGaDi = (cboGaDi.SelectedItem as DataRowView)?["TenGa"]?.ToString() ?? cboGaDi.Text;
                        string tenGaDen = (cboGaDen.SelectedItem as DataRowView)?["TenGa"]?.ToString() ?? cboGaDen.Text;

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
                ClearNguoiMuaTenError();
                ClearNguoiMuaSdtError();
                RenderGioVe();
                CapNhatTongTien();

                RenderToaTauThucTe();
                LoadDanhSachVeTab2();
                UpdateStepIndicator(1);
            }
            else
            {
                ThongBaoDialog.Loi($"Thanh toán thất bại: {err}", "Lỗi Thanh Toán");
            }
        }

        private async void LoadDanhSachVeTab2() => await LoadDanhSachVeTab2Async();

        private async Task LoadDanhSachVeTab2Async()
        {
            try
            {
                DataTable dt = await Task.Run(() => _veService.LayDanhSach());
                _cachedDanhSachVe = dt;
                ApplyFilterVe();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi tải danh sách vé: {ex.Message}", "Lỗi CSDL");
            }
        }

        #region PHÂN TRANG & BỘ LỌC NÂNG CAO TAB 2

        private void ApplyPagingVe()
        {
            if (_cachedDanhSachVe == null) return;

            var view = _cachedDanhSachVe.DefaultView;
            int totalItems = view.Count;
            txtTongSoVePhatHanh.Text = $"Tổng số: {totalItems} vé";

            pagerVe.SetPagingInfo(totalItems, _currentPageVe, _pageSizeVe, "vé");

            var filteredList = view.Cast<DataRowView>().ToList();
            IEnumerable<DataRowView> pageRows;

            if (_pageSizeVe <= 0 || _pageSizeVe >= totalItems)
            {
                pageRows = filteredList;
            }
            else
            {
                pageRows = filteredList.Skip((_currentPageVe - 1) * _pageSizeVe).Take(_pageSizeVe);
            }

            DataTable pagedTable = _cachedDanhSachVe.Clone();
            foreach (var item in pageRows)
            {
                pagedTable.ImportRow(item.Row);
            }

            _cachedTableVePaged = pagedTable;
            dgVe.ItemsSource = _cachedTableVePaged.DefaultView;

            if (dgVe.Items.Count > 0 && dgVe.SelectedIndex < 0)
            {
                dgVe.SelectedIndex = 0;
            }
            else if (dgVe.Items.Count == 0)
            {
                HienThiChiTietVe(null);
            }
        }

        private void PagerVe_PageChanged(object? sender, int newPage)
        {
            _currentPageVe = newPage;
            ApplyPagingVe();
        }

        private void PagerVe_PageSizeChanged(object? sender, int newPageSize)
        {
            _pageSizeVe = newPageSize;
            _currentPageVe = 1;
            ApplyPagingVe();
        }

        private void ApplyFilterVe()
        {
            if (_cachedDanhSachVe == null) return;

            var filters = new List<string>();

            // 1. Lọc theo trạng thái
            if (cboLocTrangThaiVe != null && cboLocTrangThaiVe.SelectedIndex > 0)
            {
                string ttText = ((ComboBoxItem)cboLocTrangThaiVe.SelectedItem).Content.ToString() ?? "";
                string ttCode = ttText switch
                {
                    "Đã Đặt" => "DA_DAT",
                    "Đã Lên Tàu" => "DA_LEN_TAU",
                    "Đã Hoàn Vé" => "DA_HOAN_VE",
                    "Đã Hủy" => "DA_HUY",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(ttCode))
                {
                    filters.Add($"TrangThai = '{ttCode}'");
                }
            }

            // 2. Lọc theo ngày mua
            if (dpLocTuNgay?.SelectedDate != null)
            {
                DateTime tuNgay = dpLocTuNgay.SelectedDate.Value.Date;
                filters.Add($"ThoiDiemXuatVe >= #{tuNgay:yyyy-MM-dd 00:00:00}#");
            }
            if (dpLocDenNgay?.SelectedDate != null)
            {
                DateTime denNgay = dpLocDenNgay.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
                filters.Add($"ThoiDiemXuatVe <= #{denNgay:yyyy-MM-dd 23:59:59}#");
            }

            _cachedDanhSachVe.DefaultView.RowFilter = string.Join(" AND ", filters);
            _currentPageVe = 1;
            ApplyPagingVe();
        }

        private void CboLocTrangThaiVe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilterVe();
        }

        private void DpLocNgay_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            ApplyFilterVe();
        }

        private void BtnXoaBoLoc_Click(object sender, RoutedEventArgs e)
        {
            if (cboLocTrangThaiVe != null) cboLocTrangThaiVe.SelectedIndex = 0;
            if (dpLocTuNgay != null) dpLocTuNgay.SelectedDate = null;
            if (dpLocDenNgay != null) dpLocDenNgay.SelectedDate = null;
            if (_cachedDanhSachVe != null)
            {
                _cachedDanhSachVe.DefaultView.RowFilter = "";
            }
            _currentPageVe = 1;
            ApplyPagingVe();
        }

        #endregion

        #region TÌM KIẾM, TRA CỨU KHÁCH & XUẤT EXCEL TAB 2

        private void TxtTimKiemVe_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnTimKiemVe_Click(sender, e);
            }
        }

        private async void BtnTimKiemVe_Click(object sender, RoutedEventArgs e)
        {
            string kw = txtTimKiemVe.Text.Trim();
            try
            {
                DataTable dt = await Task.Run(() => _veService.TimKiemVe(kw));
                _cachedDanhSachVe = dt;
                ApplyFilterVe();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi tìm kiếm vé: {ex.Message}", "Lỗi CSDL");
            }
        }

        private async void BtnTraCuuTheoCccd_Click(object sender, RoutedEventArgs e)
        {
            string cccd = txtTimKiemVe.Text.Trim();
            if (string.IsNullOrWhiteSpace(cccd))
            {
                if (dgVe.SelectedItem is DataRowView row)
                {
                    cccd = row["CCCDHanhKhach"]?.ToString() ?? "";
                    txtTimKiemVe.Text = cccd;
                }
            }

            if (string.IsNullOrWhiteSpace(cccd))
            {
                ThongBaoDialog.ThongTin("Vui lòng nhập số CCCD/Định danh vào ô tìm kiếm hoặc chọn một vé trên danh sách!", "Tra Cứu Khách Hàng");
                txtTimKiemVe.Focus();
                return;
            }

            try
            {
                DataTable dt = await Task.Run(() => _veService.LayDanhSachVeTheoCCCD(cccd));
                if (dt.Rows.Count == 0)
                {
                    ThongBaoDialog.ThongTin($"Không tìm thấy vé nào gắn với CCCD '{cccd}'!", "Không Có Dữ Liệu");
                    return;
                }

                _cachedDanhSachVe = dt;
                ApplyFilterVe();
                ThongBaoDialog.ThanhCong($"Tìm thấy {dt.Rows.Count} vé liên quan đến CCCD: {cccd}", "Lịch Sử Mua Vé");
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi tra cứu vé theo CCCD: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void BtnNapLaiDanhSach_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiemVe.Text = "";
            if (cboLocTrangThaiVe != null) cboLocTrangThaiVe.SelectedIndex = 0;
            if (dpLocTuNgay != null) dpLocTuNgay.SelectedDate = null;
            if (dpLocDenNgay != null) dpLocDenNgay.SelectedDate = null;
            LoadDanhSachVeTab2();
        }

        private void BtnXuatExcelVe_Click(object sender, RoutedEventArgs e)
        {
            if (_cachedDanhSachVe == null || _cachedDanhSachVe.Rows.Count == 0)
            {
                ThongBaoDialog.ThongTin("Không có vé nào trên danh sách để xuất tệp Excel!", "Thông Báo");
                return;
            }

            var mapping = new Dictionary<string, string>
            {
                ["MaVeCode"] = "Mã Vé Điện Tử",
                ["MaPNR"] = "Mã Đơn PNR",
                ["SoHieuMacTau"] = "Mác Tàu",
                ["TenGaDi"] = "Ga Xuất Phát",
                ["TenGaDen"] = "Ga Đến",
                ["TenHanhKhach"] = "Họ Tên Hành Khách",
                ["CCCDHanhKhach"] = "CCCD/Định Danh",
                ["NhanHieuToa"] = "Toa Xe",
                ["SoGhe"] = "Số Ghế",
                ["LoaiToa"] = "Hạng Chỗ",
                ["GiaVeGoc"] = "Giá Gốc (VNĐ)",
                ["SoTienGiam"] = "Giảm Giá (VNĐ)",
                ["GiaVeThucThu"] = "Thực Thu (VNĐ)",
                ["TrangThai"] = "Trạng Thái Vé",
                ["ThoiDiemXuatVe"] = "Thời Gian Mua",
                ["TenNguoiMua"] = "Người Đặt Vé",
                ["SdtNguoiMua"] = "SĐT Người Đặt"
            };

            FileExchangeHelper.XuatExcel(
                _cachedDanhSachVe.DefaultView, 
                mapping, 
                $"DanhSachVe_{DateTime.Now:yyyyMMdd_HHmmss}", 
                "BÁO CÁO DANH SÁCH VÉ TÀU ĐÃ PHÁT HÀNH - ĐƯỜNG SẮT VIỆT NAM", 
                "SoTraCuuVe");
        }

        private void BtnBaoCaoDoanhThu_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new BaoCaoDoanhThuBanVeDialog { Owner = Window.GetWindow(this) };
            dlg.ShowDialog();
        }

        #endregion

        #region MASTER-DETAIL PANEL & QR CODE ĐIỆN TỬ

        private void DgVe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVe.SelectedItem is DataRowView row)
            {
                HienThiChiTietVe(row);
            }
            else
            {
                HienThiChiTietVe(null);
            }
        }

        private void HienThiChiTietVe(DataRowView? row)
        {
            if (row == null)
            {
                pnlChiTietVeTrong.Visibility = Visibility.Visible;
                scrollChiTietVe.Visibility = Visibility.Collapsed;
                return;
            }

            pnlChiTietVeTrong.Visibility = Visibility.Collapsed;
            scrollChiTietVe.Visibility = Visibility.Visible;

            string maVeCode = row["MaVeCode"].ToString() ?? "";
            string maPNR = row["MaPNR"].ToString() ?? "";
            string macTau = row["SoHieuMacTau"].ToString() ?? "";
            string gaDi = row["TenGaDi"].ToString() ?? "";
            string gaDen = row["TenGaDen"].ToString() ?? "";
            string tenHK = row["TenHanhKhach"].ToString() ?? "";
            string cccdHK = row["CCCDHanhKhach"].ToString() ?? "";
            string toa = row["NhanHieuToa"].ToString() ?? "";
            string loaiToa = row["LoaiToa"].ToString() ?? "";
            int soGhe = Convert.ToInt32(row["SoGhe"]);
            string trangThai = row["TrangThai"].ToString() ?? "";

            lblChiTietMaVeCode.Text = maVeCode;
            lblChiTietMaPNR.Text = $"PNR: {maPNR}";
            lblChiTietTrangThai.Text = trangThai switch
            {
                "DA_DAT" => "ĐÃ ĐẶT",
                "DA_LEN_TAU" => "ĐÃ LÊN TÀU",
                "DA_HOAN_VE" => "ĐÃ HOÀN VÉ",
                "DA_HUY" => "ĐÃ HỦY",
                _ => trangThai
            };

            switch (trangThai)
            {
                case "DA_DAT":
                    brdChiTietTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
                    brdChiTietTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                    lblChiTietTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                    break;
                case "DA_HOAN_VE":
                    brdChiTietTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                    brdChiTietTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));
                    lblChiTietTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                    break;
                case "DA_HUY":
                    brdChiTietTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                    brdChiTietTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FECACA"));
                    lblChiTietTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
                    break;
                default:
                    brdChiTietTrangThai.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    brdChiTietTrangThai.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                    lblChiTietTrangThai.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
                    break;
            }

            lblChiTietGaDi.Text = gaDi;
            lblChiTietGaDen.Text = gaDen;
            lblChiTietMacTau.Text = macTau;

            if (row.Row.Table.Columns.Contains("GioXuatPhatKH") && row["GioXuatPhatKH"] != DBNull.Value)
            {
                lblChiTietGioChay.Text = Convert.ToDateTime(row["GioXuatPhatKH"]).ToString("HH:mm - dd/MM/yyyy");
            }
            else
            {
                lblChiTietGioChay.Text = "--";
            }

            lblChiTietToa.Text = toa;
            lblChiTietLoaiToa.Text = loaiToa;
            lblChiTietSoGhe.Text = soGhe.ToString();

            if (row.Row.Table.Columns.Contains("TangGiuong") && row["TangGiuong"] != DBNull.Value)
            {
                lblChiTietTangGiuong.Text = $"Tầng {row["TangGiuong"]}";
            }
            else
            {
                lblChiTietTangGiuong.Text = "Ghế ngồi";
            }

            if (row.Row.Table.Columns.Contains("MaChoNgoi") && row["MaChoNgoi"] != DBNull.Value)
            {
                lblChiTietMaCho.Text = $"#{row["MaChoNgoi"]}";
            }
            else
            {
                lblChiTietMaCho.Text = "--";
            }

            lblChiTietTenHK.Text = tenHK;
            lblChiTietCccdHK.Text = cccdHK;

            decimal giaGoc = Convert.ToDecimal(row["GiaVeGoc"]);
            decimal giamGia = Convert.ToDecimal(row["SoTienGiam"]);
            decimal thucThu = Convert.ToDecimal(row["GiaVeThucThu"]);

            lblChiTietGiaGoc.Text = $"{giaGoc:N0} VNĐ";
            lblChiTietGiamGia.Text = $"-{giamGia:N0} VNĐ";
            lblChiTietThucThu.Text = $"{thucThu:N0} VNĐ";

            string nguoiMua = (row.Row.Table.Columns.Contains("TenNguoiMua") && row["TenNguoiMua"] != DBNull.Value) ? row["TenNguoiMua"].ToString()! : tenHK;
            string sdt = (row.Row.Table.Columns.Contains("SdtNguoiMua") && row["SdtNguoiMua"] != DBNull.Value) ? row["SdtNguoiMua"].ToString()! : "--";

            lblChiTietNguoiMua.Text = nguoiMua;
            lblChiTietSdtNguoiMua.Text = sdt;
            lblChiTietThoiDiemXuat.Text = Convert.ToDateTime(row["ThoiDiemXuatVe"]).ToString("HH:mm - dd/MM/yyyy");

            // Khối thông tin hoàn trả (nếu vé đã hoàn)
            if (trangThai == "DA_HOAN_VE")
            {
                brdChiTietHoanHuy.Visibility = Visibility.Visible;
                lblChiTietThoiDiemHoan.Text = (row.Row.Table.Columns.Contains("ThoiDiemHoan") && row["ThoiDiemHoan"] != DBNull.Value)
                    ? Convert.ToDateTime(row["ThoiDiemHoan"]).ToString("HH:mm - dd/MM/yyyy") : "--";

                decimal tyLe = (row.Row.Table.Columns.Contains("TyLeLePhi") && row["TyLeLePhi"] != DBNull.Value)
                    ? Convert.ToDecimal(row["TyLeLePhi"]) : 0.10m;
                lblChiTietTyLeHoan.Text = $"{(int)(tyLe * 100)}%";

                decimal lePhi = (row.Row.Table.Columns.Contains("LePhiHoan") && row["LePhiHoan"] != DBNull.Value)
                    ? Convert.ToDecimal(row["LePhiHoan"]) : Math.Round(giaGoc * tyLe, 0);
                lblChiTietLePhiHoan.Text = $"{lePhi:N0} VNĐ";

                decimal thucHoan = (row.Row.Table.Columns.Contains("SoTienThucHoan") && row["SoTienThucHoan"] != DBNull.Value)
                    ? Convert.ToDecimal(row["SoTienThucHoan"]) : (giaGoc - lePhi);
                lblChiTietThucHoan.Text = $"{thucHoan:N0} VNĐ";

                lblChiTietLyDoHoan.Text = (row.Row.Table.Columns.Contains("LyDoHoan") && row["LyDoHoan"] != DBNull.Value)
                    ? row["LyDoHoan"].ToString() : "--";
            }
            else
            {
                brdChiTietHoanHuy.Visibility = Visibility.Collapsed;
            }

            // Sinh mã QR điện tử
            TaoMaQrChoChiTietVe(maVeCode, cccdHK, macTau, soGhe);
        }

        private void TaoMaQrChoChiTietVe(string maVe, string cccd, string tau, int ghe)
        {
            try
            {
                string payload = $"VNR|TICKET|{maVe}|{tau}|GHE:{ghe}|CCCD:{cccd}";
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(10);

                var bitmap = new BitmapImage();
                using (var ms = new System.IO.MemoryStream(qrBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                imgChiTietQRCode.Source = bitmap;
            }
            catch
            {
                imgChiTietQRCode.Source = null;
            }
        }

        #endregion

        #region DOUBLE-CLICK & CONTEXT MENU TAB 2

        private void DgVe_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgVe.SelectedItem is DataRowView)
            {
                BtnInTheLenTauTab2_Click(sender, e);
            }
        }

        private void CtxXemChiTiet_Click(object sender, RoutedEventArgs e)
        {
            if (dgVe.SelectedItem is DataRowView row)
            {
                HienThiChiTietVe(row);
                scrollChiTietVe.ScrollToTop();
            }
        }

        private void CtxSaoChepPNR_Click(object sender, RoutedEventArgs e)
        {
            if (dgVe.SelectedItem is DataRowView row)
            {
                string pnr = row["MaPNR"].ToString() ?? "";
                if (!string.IsNullOrEmpty(pnr))
                {
                    Clipboard.SetText(pnr);
                    ThongBaoDialog.ThanhCong($"Đã sao chép mã PNR '{pnr}' vào bộ nhớ tạm!", "Sao Chép Thành Công");
                }
            }
        }

        private void CtxSaoChepMaVe_Click(object sender, RoutedEventArgs e)
        {
            if (dgVe.SelectedItem is DataRowView row)
            {
                string maCode = row["MaVeCode"].ToString() ?? "";
                if (!string.IsNullOrEmpty(maCode))
                {
                    Clipboard.SetText(maCode);
                    ThongBaoDialog.ThanhCong($"Đã sao chép mã vé '{maCode}' vào bộ nhớ tạm!", "Sao Chép Thành Công");
                }
            }
        }

        private void CtxXemVeCungKhach_Click(object sender, RoutedEventArgs e)
        {
            if (dgVe.SelectedItem is DataRowView row)
            {
                string cccd = row["CCCDHanhKhach"].ToString() ?? "";
                if (!string.IsNullOrEmpty(cccd))
                {
                    txtTimKiemVe.Text = cccd;
                    BtnTraCuuTheoCccd_Click(sender, e);
                }
            }
        }

        #endregion

        #region IN VÉ, ĐỔI GHẾ & HOÀN VÉ CHUẨN VNR TAB 2

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

        private void BtnDoiGheTab2_Click(object sender, RoutedEventArgs e)
        {
            if (dgVe.SelectedItem is DataRowView row)
            {
                string tt = row["TrangThai"].ToString() ?? "";
                if (tt != "DA_DAT")
                {
                    ThongBaoDialog.CanhBao($"Vé này đang ở trạng thái '{tt}', chỉ vé 'Đã Đặt' mới được phép đổi ghế!", "Không Thể Đổi Ghế");
                    return;
                }

                int maVe = Convert.ToInt32(row["MaVe"]);
                string maVeCode = row["MaVeCode"].ToString() ?? "";
                string maPNR = row["MaPNR"].ToString() ?? "";
                int maChuyenTau = Convert.ToInt32(row["MaChuyenTau"]);
                int maGaDi = Convert.ToInt32(row["MaGaDi"]);
                int maGaDen = Convert.ToInt32(row["MaGaDen"]);
                string tenGaDi = row["TenGaDi"].ToString() ?? "";
                string tenGaDen = row["TenGaDen"].ToString() ?? "";
                string macTau = row["SoHieuMacTau"].ToString() ?? "";
                string tenHK = row["TenHanhKhach"].ToString() ?? "";
                string toaHienTai = row["NhanHieuToa"].ToString() ?? "";
                int gheHienTai = Convert.ToInt32(row["SoGhe"]);
                decimal giaHienTai = Convert.ToDecimal(row["GiaVeThucThu"]);
                string loaiTau = row.Row.Table.Columns.Contains("LoaiTau") ? (row["LoaiTau"]?.ToString() ?? "TAU_CHO") : "TAU_CHO";

                var dlg = new DoiGheDialog { Owner = Window.GetWindow(this) };
                dlg.KhoiTao(
                    maVe, maVeCode, maPNR, maChuyenTau, 
                    maGaDi, maGaDen, tenGaDi, tenGaDen, 
                    macTau, tenHK, toaHienTai, gheHienTai, giaHienTai, loaiTau);

                if (dlg.ShowDialog() == true)
                {
                    LoadDanhSachVeTab2();
                    RenderToaTauThucTe();
                }
            }
            else
            {
                ThongBaoDialog.ThongTin("Vui lòng chọn một vé trên danh sách để thực hiện đổi chỗ ngồi!", "Chưa Chọn Vé");
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
                if (tt == "DA_HUY")
                {
                    ThongBaoDialog.CanhBao("Vé này đã bị hủy trước đó!", "Vé Đã Hủy");
                    return;
                }

                // Tính toán lệ phí theo Quy chuẩn VNR dựa trên giờ khởi hành của chuyến tàu
                decimal tyLeLePhi = 0.10m;
                string quyDinhVnr = "";

                if (row.Row.Table.Columns.Contains("GioXuatPhatKH") && row["GioXuatPhatKH"] != DBNull.Value)
                {
                    DateTime gioXuatPhat = Convert.ToDateTime(row["GioXuatPhatKH"]);
                    TimeSpan conLai = gioXuatPhat - DateTime.Now;

                    if (conLai.TotalHours < 4)
                    {
                        string thoiGianMsg = conLai.TotalMinutes > 0 
                            ? $"còn {(int)conLai.TotalMinutes} phút" 
                            : "đã xuất phát";

                        ThongBaoDialog.CanhBao(
                            $"KHÔNG THỂ HOÀN VÉ THEO QUY CHUẨN VNR!\n\n" +
                            $"- Chuyến tàu: {row["SoHieuMacTau"]} khởi hành lúc: {gioXuatPhat:HH:mm - dd/MM/yyyy}\n" +
                            $"- Thời gian trước giờ chạy: {thoiGianMsg}\n\n" +
                            $"Theo Quy định VNR (Điều 15 Thông tư 14/2024/TT-BGTVT), vé chỉ được phép hoàn trả trước giờ tàu xuất phát tối thiểu 4 giờ.\n" +
                            $"Vé #{maCode} đã quá thời hạn cho phép hoàn trả!",
                            "Quá Hạn Hoàn Vé VNR");
                        return;
                    }
                    else if (conLai.TotalHours < 24)
                    {
                        tyLeLePhi = 0.20m;
                        quyDinhVnr = "Hoàn trước giờ tàu chạy từ 4h đến 24h: Khấu trừ lệ phí 20% theo quy chuẩn VNR.";
                    }
                    else
                    {
                        tyLeLePhi = 0.10m;
                        quyDinhVnr = "Hoàn trước giờ tàu chạy > 24h: Khấu trừ lệ phí 10% theo quy chuẩn VNR.";
                    }
                }
                else
                {
                    tyLeLePhi = 0.10m;
                    quyDinhVnr = "Khấu trừ lệ phí tiêu chuẩn 10% theo quy chuẩn VNR.";
                }

                decimal giaGoc = Convert.ToDecimal(row["GiaVeGoc"]);
                decimal lePhi = Math.Round(giaGoc * tyLeLePhi, 0);
                decimal thucHoan = giaGoc - lePhi;

                bool xacNhan = ThongBaoDialog.XacNhan(
                    $"XÁC NHẬN LÀM THỦ TỤC HOÀN TRẢ VÉ #{maCode}?\n\n" +
                    $"- Khách hàng: {row["TenHanhKhach"]} (CCCD: {row["CCCDHanhKhach"]})\n" +
                    $"- Mác tàu & Chặng: {row["SoHieuMacTau"]} ({row["TenGaDi"]} ➔ {row["TenGaDen"]})\n" +
                    $"- Tiền vé gốc: {giaGoc:N0} VNĐ\n" +
                    $"- Lệ phí khấu trừ ({(int)(tyLeLePhi * 100)}%): {lePhi:N0} VNĐ\n" +
                    $"- Số tiền thực hoàn lại khách: {thucHoan:N0} VNĐ\n\n" +
                    $"Ghi chú VNR: {quyDinhVnr}\n\n" +
                    $"Chỗ ngồi sẽ được giải phóng ngay lập tức trên hệ thống!",
                    "Thủ Tục Hoàn Trả Vé Chuẩn VNR",
                    "Hoàn Trả Vé",
                    "Đóng",
                    laHanhDongXoa: true);

                if (xacNhan)
                {
                    if (_veService.HoanVe(maVe, tyLeLePhi, $"Khách hoàn vé tại ga ({quyDinhVnr})", "TIEN_MAT", 3, out string err))
                    {
                        ThongBaoDialog.ThanhCong(
                            $"ĐÃ HOÀN TRẢ VÉ #{maCode} THÀNH CÔNG!\n\n" +
                            $"- Tiền hoàn trả khách: {thucHoan:N0} VNĐ\n" +
                            $"- Lệ phí thu hồi: {lePhi:N0} VNĐ\n\n" +
                            $"Chỗ ngồi đã được giải phóng trên hệ thống.", 
                            "Hoàn Vé Thành Công");

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

        #endregion

        private void TabMainBanVe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

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

        public void KichHoatXuatTep()
        {
            tabMainBanVe.SelectedIndex = 1;
            BtnXuatExcelVe_Click(this, new RoutedEventArgs());
        }

        public void KichHoatBaoCao()
        {
            BtnBaoCaoDoanhThu_Click(this, new RoutedEventArgs());
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
