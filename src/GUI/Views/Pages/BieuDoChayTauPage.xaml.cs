using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BUS.Services;
using DTO.VanHanh;

namespace GUI.Views.Pages
{
    public partial class BieuDoChayTauPage : Page
    {
        private readonly DieuDoService _dieuDoService = new();
        private List<HanhTrinhTauDTO> _dsHanhTrinh = new();
        private List<XungDotKhuGianDTO> _dsXungDot = new();
        private DataTable _dtGa = new();

        // Tọa độ hình học và căn chỉnh biểu đồ Marey
        private const double X_LEFT = 210.0;
        private const double X_WIDTH = 2200.0;
        private const double Y_TOP = 45.0;
        private const double ROW_HEIGHT = 44.0; // Khoảng cách cố định giữa 2 ga liên tiếp: Chống đè chữ 100%

        private readonly List<(decimal Km, double Y, string TenGa, string MaCode, string HangGa, bool CoCauQuay)> _lyTrinhYList = new();
        private HanhTrinhTauDTO? _selectedTau;
        private bool _isBangChiTietThuGon = false;
        private double _savedChiTietHeight = 260.0;
        private bool _isLoadingData = false;

        #region Helpers tối ưu bộ nhớ & Zoom
        private static SolidColorBrush TaoBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static SolidColorBrush TaoBrushHex(string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return TaoBrush(color);
        }

        private void CuonToiViTri(double canvasX, double canvasY)
        {
            double scaleX = canvasScale?.ScaleX ?? 1.0;
            double scaleY = canvasScale?.ScaleY ?? 1.0;

            double targetX = canvasX * scaleX;
            double targetY = canvasY * scaleY;

            double viewW = svBieuDo.ViewportWidth > 0 ? svBieuDo.ViewportWidth : 800;
            double viewH = svBieuDo.ViewportHeight > 0 ? svBieuDo.ViewportHeight : 500;

            svBieuDo.ScrollToHorizontalOffset(Math.Max(0, targetX - viewW / 2.0));
            svBieuDo.ScrollToVerticalOffset(Math.Max(0, targetY - viewH / 2.0));
        }
        #endregion

        private class TauVisual
        {
            public HanhTrinhTauDTO Tau { get; set; } = null!;
            public List<Line> Lines { get; set; } = new();
            public List<Ellipse> Dots { get; set; } = new();
            public Border? LabelBorder { get; set; }
            public Brush BrushMau { get; set; } = null!;
        }

        private class XungDotVisual
        {
            public XungDotKhuGianDTO Xd { get; set; } = null!;
            public Ellipse RingOuter { get; set; } = null!;
            public Ellipse DotInner { get; set; } = null!;
            public Border TagBorder { get; set; } = null!;
            public Point ViTriGiao { get; set; }
        }

        private readonly List<TauVisual> _tauVisuals = new();
        private readonly List<XungDotVisual> _xungDotVisuals = new();

        public event Action? DuLieuDieuDoThayDoi;

        public BieuDoChayTauPage()
        {
            InitializeComponent();
            try
            {
                dpNgay.SelectedDate = _dieuDoService.LayNgayBieuDoMacDinh();
            }
            catch
            {
                dpNgay.SelectedDate = DateTime.Today;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            NapDanhSachNgayCoTau();
            LoadData();
        }

        private void NapDanhSachNgayCoTau()
        {
            try
            {
                pnlQuickDates.Children.Clear();
                pnlQuickDates.Children.Add(new TextBlock
                {
                    Text = "Các ngày có tàu:",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TaoBrush(Color.FromRgb(71, 85, 105)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                });

                DataTable dtNgay = _dieuDoService.LayDanhSachNgayCoTau();
                if (dtNgay == null || dtNgay.Rows.Count == 0) return;

                // Nếu ngày hiện tại chưa chọn hoặc không có tàu, tự động chọn ngày có tàu gần nhất
                DateTime ngayHienTai = dpNgay.SelectedDate ?? DateTime.Today;
                bool ngayHienTaiCoTau = false;
                foreach (DataRow row in dtNgay.Rows)
                {
                    if (Convert.ToDateTime(row["Ngay"]).Date == ngayHienTai.Date)
                    {
                        ngayHienTaiCoTau = true;
                        break;
                    }
                }
                if (!ngayHienTaiCoTau && dtNgay.Rows.Count > 0)
                {
                    dpNgay.SelectedDateChanged -= DpNgay_SelectedDateChanged;
                    dpNgay.SelectedDate = Convert.ToDateTime(dtNgay.Rows[0]["Ngay"]);
                    dpNgay.SelectedDateChanged += DpNgay_SelectedDateChanged;
                    ngayHienTai = dpNgay.SelectedDate.Value;
                }

                foreach (DataRow row in dtNgay.Rows)
                {
                    DateTime d = Convert.ToDateTime(row["Ngay"]);
                    int soTau = Convert.ToInt32(row["SoChuyenTau"]);
                    bool isSelected = (d.Date == ngayHienTai.Date);

                    var btn = new Button
                    {
                        Content = $"{d:dd/MM} ({soTau} tàu)",
                        ToolTip = $"Xem biểu đồ ngày {d:dd/MM/yyyy} ({soTau} chuyến tàu)",
                        Height = 24,
                        Padding = new Thickness(9, 0, 9, 0),
                        FontSize = 11,
                        FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold,
                        Background = isSelected ? TaoBrush(Color.FromRgb(0, 59, 115)) : TaoBrush(Color.FromRgb(241, 245, 249)),
                        Foreground = isSelected ? Brushes.White : TaoBrush(Color.FromRgb(51, 65, 85)),
                        BorderBrush = isSelected ? TaoBrush(Color.FromRgb(0, 40, 80)) : TaoBrush(Color.FromRgb(203, 213, 225)),
                        BorderThickness = new Thickness(1),
                        Cursor = Cursors.Hand,
                        Margin = new Thickness(0, 0, 4, 0),
                        Tag = d
                    };

                    btn.Click += (s, args) =>
                    {
                        if (s is Button b && b.Tag is DateTime dt)
                        {
                            dpNgay.SelectedDate = dt;
                            NapDanhSachNgayCoTau();
                        }
                    };

                    pnlQuickDates.Children.Add(btn);
                }
            }
            catch
            {
            }
        }

        public void LoadData()
        {
            if (_isLoadingData) return;
            _isLoadingData = true;

            try
            {
                DateTime ngay = dpNgay.SelectedDate ?? _dieuDoService.LayNgayBieuDoMacDinh();
                _dtGa = _dieuDoService.LayDanhSachGaTuyen();
                KhoiTaoBangToaDoGa();

                _dsHanhTrinh = _dieuDoService.LayDuLieuBieuDo(ngay);
                _dsXungDot = _dieuDoService.KiemTraXungDotToanTuyen(ngay);

                CapNhatThongKe();
                VeBiểuĐồ();
                HienThiDanhSachXungDot();
                HienThiDanhSachTau();

                if (_selectedTau != null)
                {
                    var updated = _dsHanhTrinh.FirstOrDefault(t => t.MaChuyenTau == _selectedTau.MaChuyenTau);
                    if (updated != null)
                    {
                        ChonChuyenTau(updated);
                    }
                    else
                    {
                        BtnDongChiTiet_Click(this, new RoutedEventArgs());
                    }
                }
                else
                {
                    bdChiTietLichDung.Visibility = Visibility.Collapsed;
                    splChiTiet.Visibility = Visibility.Collapsed;
                    rowChiTiet.Height = GridLength.Auto;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu biểu đồ chạy tàu:\n{ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void KhoiTaoBangToaDoGa()
        {
            _lyTrinhYList.Clear();
            if (_dtGa.Rows.Count == 0) return;

            // Sort theo lý trình tăng dần
            DataView dv = _dtGa.DefaultView;
            dv.Sort = "LyTrinhKm ASC";
            DataTable sorted = dv.ToTable();

            for (int i = 0; i < sorted.Rows.Count; i++)
            {
                DataRow r = sorted.Rows[i];
                decimal km = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m;
                string tenGa = r["TenGa"]?.ToString() ?? "";
                string maCode = r["MaGaCode"]?.ToString() ?? "";
                string hangGa = r["HangGa"]?.ToString() ?? "";
                bool coCauQuay = r.Table.Columns.Contains("CoCauQuay") && r["CoCauQuay"] != DBNull.Value && Convert.ToBoolean(r["CoCauQuay"]);

                double y = Y_TOP + i * ROW_HEIGHT;
                _lyTrinhYList.Add((km, y, tenGa, maCode, hangGa, coCauQuay));
            }

            // Cập nhật chiều cao Canvas phù hợp
            double totalHeight = Math.Max(1350.0, Y_TOP + (_lyTrinhYList.Count + 1) * ROW_HEIGHT + 60.0);
            cvsBieuDo.Height = totalHeight;
            cvsBieuDo.Width = X_LEFT + X_WIDTH + 80.0;
        }

        private void CapNhatThongKe()
        {
            txtTongChuyenTau.Text = $"{_dsHanhTrinh.Count} Chuyến";
            txtBadgeXungDot.Text = $"{_dsXungDot.Count} Xung Đột";
            txtSoXungDotSide.Text = $"{_dsXungDot.Count} Xung Đột";

            if (_dsXungDot.Count > 0)
            {
                bdBadgeXungDot.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Đỏ công nghiệp #DC2626
                bdBadgeXungDot.BorderThickness = new Thickness(0);
                txtBadgeXungDot.Foreground = Brushes.White;

                bdBadgeSideLoi.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Đỏ công nghiệp #DC2626
                bdBadgeSideLoi.BorderThickness = new Thickness(0);
                txtSoXungDotSide.Foreground = Brushes.White;

                bdKhongXungDotNotice.Visibility = Visibility.Collapsed;
            }
            else
            {
                bdBadgeXungDot.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74)); // Xanh lá an toàn #16A34A
                bdBadgeXungDot.BorderThickness = new Thickness(0);
                txtBadgeXungDot.Foreground = Brushes.White;
                txtBadgeXungDot.Text = "0 Xung Đột (An Toàn)";

                bdBadgeSideLoi.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74)); // Xanh lá an toàn #16A34A
                bdBadgeSideLoi.BorderThickness = new Thickness(0);
                txtSoXungDotSide.Foreground = Brushes.White;
                txtSoXungDotSide.Text = "AN TOÀN";

                bdKhongXungDotNotice.Visibility = Visibility.Visible;
            }
        }

        // =========================================================================
        // THUẬT TOÁN RENDER CANVAS BIỂU ĐỒ MAREY (LIGHT THEME VNR ENTERPRISE)
        // =========================================================================

        private void VeBiểuĐồ()
        {
            cvsBieuDo.Children.Clear();
            _tauVisuals.Clear();
            _xungDotVisuals.Clear();

            double maxY = _lyTrinhYList.Count > 0 ? _lyTrinhYList[^1].Y + 30.0 : 1200.0;

            // 1. Vẽ nền cột thước đo ga (Station Ruler background)
            var rulerBg = new Rectangle
            {
                Width = X_LEFT - 10,
                Height = cvsBieuDo.Height,
                Fill = new SolidColorBrush(Color.FromRgb(248, 250, 252))
            };
            Canvas.SetLeft(rulerBg, 0);
            Canvas.SetTop(rulerBg, 0);
            cvsBieuDo.Children.Add(rulerBg);

            // Đường phân cách giữa cột ga và vùng biểu đồ
            var rulerSep = new Line
            {
                X1 = X_LEFT,
                Y1 = 0,
                X2 = X_LEFT,
                Y2 = cvsBieuDo.Height,
                Stroke = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                StrokeThickness = 1.5
            };
            cvsBieuDo.Children.Add(rulerSep);

            // 2. Vẽ lưới trục thời gian (X-axis: 00:00 -> 24:00)
            VeLuoiThoiGian();

            // 3. Vẽ lưới trục các ga đường sắt (Y-axis: Bắc -> Nam theo hàng chuẩn)
            VeLuoiGaDuongSat();

            // 4. Lọc mác tàu theo combobox
            string locLoai = (cboLocLoaiTau.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ALL";
            var dsHienThi = _dsHanhTrinh;
            if (locLoai != "ALL")
            {
                dsHienThi = _dsHanhTrinh.Where(t => t.LoaiTau == locLoai).ToList();
            }

            // 5. Vẽ các đường chạy tàu (Train Trajectories)
            foreach (var tau in dsHienThi)
            {
                VeDuongChayTau(tau);
            }

            // 6. Tính toán giao điểm hình học chuẩn xác 100% trên Canvas và vẽ đốm đỏ
            VeCacDiemXungDotChuanXac();

            // 7. Vẽ vạch thời gian thực "Bây giờ" (Now Line) nếu xem ngày hôm nay
            VeDuongThoiGianThuc(maxY);

            // Thiết lập trạng thái bình thường (Default: Rõ nét, không mờ nhạt)
            ApDungTrangThaiMacDinh();
        }

        private void VeLuoiThoiGian()
        {
            double maxY = _lyTrinhYList.Count > 0 ? _lyTrinhYList[^1].Y + 30.0 : 1200.0;

            // Nền thanh header hiển thị giờ trên đỉnh (HOÀN TOÀN THÔNG THOÁNG TỪ 00:00 ĐẾN 24:00)
            var headerGioBg = new Rectangle
            {
                Width = X_WIDTH + 50,
                Height = 32,
                Fill = new SolidColorBrush(Color.FromRgb(241, 245, 249))
            };
            Canvas.SetLeft(headerGioBg, X_LEFT);
            Canvas.SetTop(headerGioBg, Y_TOP - 35);
            cvsBieuDo.Children.Add(headerGioBg);

            for (int h = 0; h <= 24; h++)
            {
                double x = X_LEFT + (h / 24.0) * X_WIDTH;

                // Đường kẻ dọc mốc giờ (Tăng tương phản để dễ dò giờ)
                bool isMajorHour = (h % 6 == 0);
                var lineGio = new Line
                {
                    X1 = x,
                    Y1 = Y_TOP - 35,
                    X2 = x,
                    Y2 = maxY,
                    Stroke = new SolidColorBrush(isMajorHour ? Color.FromRgb(100, 116, 139) : Color.FromRgb(203, 213, 225)),
                    StrokeThickness = isMajorHour ? 1.8 : 1.0
                };
                cvsBieuDo.Children.Add(lineGio);

                // Nhãn giờ trên đầu (Đầy đủ từ 00:00, 01:00, 02:00 ... đến 24:00)
                var txtGio = new TextBlock
                {
                    Text = $"{h:D2}:00",
                    FontSize = 11,
                    FontWeight = isMajorHour ? FontWeights.Bold : FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(isMajorHour ? Color.FromRgb(0, 59, 115) : Color.FromRgb(71, 85, 105)),
                    FontFamily = new FontFamily("Consolas")
                };
                Canvas.SetLeft(txtGio, x - 17);
                Canvas.SetTop(txtGio, Y_TOP - 26);
                cvsBieuDo.Children.Add(txtGio);

                // Vạch chia 30 phút (nét đứt rõ ràng hơn)
                if (h < 24)
                {
                    double x30 = X_LEFT + ((h + 0.5) / 24.0) * X_WIDTH;
                    var line30 = new Line
                    {
                        X1 = x30,
                        Y1 = Y_TOP,
                        X2 = x30,
                        Y2 = maxY,
                        Stroke = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                        StrokeThickness = 1.0,
                        StrokeDashArray = new DoubleCollection { 3, 3 }
                    };
                    cvsBieuDo.Children.Add(line30);
                }
            }
        }

        private void VeLuoiGaDuongSat()
        {
            if (_lyTrinhYList.Count == 0) return;

            foreach (var item in _lyTrinhYList)
            {
                double y = item.Y;
                bool isDauMoi = item.Km == 0 ||
                                item.Km == _lyTrinhYList[^1].Km ||
                                item.CoCauQuay ||
                                string.Equals(item.HangGa, "HANG_1", StringComparison.OrdinalIgnoreCase) ||
                                item.HangGa == "1";
                bool isTrungGian = string.Equals(item.HangGa, "HANG_2", StringComparison.OrdinalIgnoreCase) || item.HangGa == "2";

                // Đường ray ngang qua ga (Phân cấp động 3 tầng theo CSDL: Cấp 1 đậm, Cấp 2 vừa, Cấp 3 mờ)
                var lineGa = new Line
                {
                    X1 = X_LEFT,
                    Y1 = y,
                    X2 = X_LEFT + X_WIDTH,
                    Y2 = y,
                    Stroke = new SolidColorBrush(isDauMoi ? Color.FromRgb(148, 163, 184) : (isTrungGian ? Color.FromRgb(226, 232, 240) : Color.FromRgb(241, 245, 249))),
                    StrokeThickness = isDauMoi ? 1.4 : 1.0,
                    StrokeDashArray = (isDauMoi || isTrungGian) ? null : new DoubleCollection { 2, 4 }
                };
                cvsBieuDo.Children.Add(lineGa);

                // Nhãn Tên Ga (Bên trái thước đo: Phân cấp kích thước & màu sắc theo Hạng Ga CSDL)
                var txtGa = new TextBlock
                {
                    Text = item.TenGa,
                    FontSize = isDauMoi ? 11.5 : (isTrungGian ? 10.5 : 9.5),
                    FontWeight = isDauMoi ? FontWeights.Bold : (isTrungGian ? FontWeights.SemiBold : FontWeights.Normal),
                    Foreground = new SolidColorBrush(isDauMoi ? Color.FromRgb(0, 59, 115) : (isTrungGian ? Color.FromRgb(51, 65, 85) : Color.FromRgb(148, 163, 184))),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Width = 115,
                    TextAlignment = TextAlignment.Right
                };
                Canvas.SetLeft(txtGa, 10);
                Canvas.SetTop(txtGa, y - (isDauMoi ? 8 : 7));
                cvsBieuDo.Children.Add(txtGa);

                // Nhãn Lý Trình Km
                var txtKm = new TextBlock
                {
                    Text = $"{item.Km:F0}km",
                    FontSize = isDauMoi ? 10 : 9,
                    Foreground = new SolidColorBrush(isDauMoi ? Color.FromRgb(71, 85, 105) : Color.FromRgb(148, 163, 184)),
                    FontFamily = new FontFamily("Consolas")
                };
                Canvas.SetLeft(txtKm, 135);
                Canvas.SetTop(txtKm, y - 7);
                cvsBieuDo.Children.Add(txtKm);

                // Chấm mốc ga tại trục tọa độ
                double dotSize = isDauMoi ? 7 : (isTrungGian ? 5 : 3.5);
                var dotGa = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = new SolidColorBrush(isDauMoi ? Color.FromRgb(0, 59, 115) : (isTrungGian ? Color.FromRgb(148, 163, 184) : Color.FromRgb(226, 232, 240))),
                    Stroke = isDauMoi ? Brushes.White : null,
                    StrokeThickness = isDauMoi ? 1.0 : 0
                };
                Canvas.SetLeft(dotGa, X_LEFT - dotSize / 2.0);
                Canvas.SetTop(dotGa, y - dotSize / 2.0);
                cvsBieuDo.Children.Add(dotGa);
            }
        }

        private void VeDuongThoiGianThuc(double maxY)
        {
            DateTime ngayXem = dpNgay.SelectedDate ?? DateTime.Today;
            if (ngayXem.Date != DateTime.Today) return;

            DateTime now = DateTime.Now;
            double phut = now.Hour * 60 + now.Minute;
            double xNow = X_LEFT + (phut / 1440.0) * X_WIDTH;

            var nowLine = new Line
            {
                X1 = xNow,
                Y1 = Y_TOP - 35,
                X2 = xNow,
                Y2 = maxY,
                Stroke = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                StrokeThickness = 2.0,
                StrokeDashArray = new DoubleCollection { 4, 3 }
            };
            cvsBieuDo.Children.Add(nowLine);

            var nowBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(5, 2, 5, 2),
                ToolTip = $"Thời gian hiện tại thực tế: {now:HH:mm:ss}"
            };
            var txtNow = new TextBlock
            {
                Text = $"● {now:HH:mm}",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas")
            };
            nowBadge.Child = txtNow;
            Canvas.SetLeft(nowBadge, xNow - 25);
            Canvas.SetTop(nowBadge, Y_TOP - 32);
            cvsBieuDo.Children.Add(nowBadge);
        }

        /// <summary>
        /// Vẽ đường chạy tàu với kỹ thuật Line Clipping qua cửa sổ 24h.
        /// Hỗ trợ lưu trữ các đối tượng trực quan để điều khiển hiệu ứng UX (Mờ mảnh -> Rõ sáng khi Hover).
        /// </summary>
        private void VeDuongChayTau(HanhTrinhTauDTO tau)
        {
            if (tau.DanhSachDiem.Count < 2) return;

            DateTime ngayGoc = dpNgay.SelectedDate ?? DateTime.Today;
            DateTime windowStart = ngayGoc.Date;
            DateTime windowEnd = ngayGoc.Date.AddDays(1);

            // Phân giải màu đường chạy tàu
            Color colorMau;
            try
            {
                colorMau = (Color)ColorConverter.ConvertFromString(tau.MauSacHex);
            }
            catch
            {
                colorMau = tau.SoHieuMacTau.StartsWith("SE") ? Color.FromRgb(220, 38, 38) : Color.FromRgb(2, 132, 199);
            }
            var brush = new SolidColorBrush(colorMau);

            var visual = new TauVisual
            {
                Tau = tau,
                BrushMau = brush
            };

            Point? firstVisiblePoint = null;
            bool isCrossDayStart = false;

            // Duyệt từng phân đoạn di chuyển & đỗ ga
            for (int i = 0; i < tau.DanhSachDiem.Count; i++)
            {
                var diemHienTai = tau.DanhSachDiem[i];
                double yHienTai = TinhToaDoY(diemHienTai.LyTrinhKm);

                // 1. Phân đoạn ĐỖ TẠI GA (Horizontal segment: GioDen -> GioDi tại cùng 1 ga)
                if (diemHienTai.GioDi > diemHienTai.GioDen)
                {
                    VePhanDoan(diemHienTai.GioDen, yHienTai, diemHienTai.GioDi, yHienTai, windowStart, windowEnd, brush, tau, visual, ref firstVisiblePoint, ref isCrossDayStart);

                    // Vẽ điểm tròn đỗ ga nếu thời điểm nằm trong ngày
                    if (diemHienTai.GioDen >= windowStart && diemHienTai.GioDen <= windowEnd)
                    {
                        double xDen = TinhToaDoXKhoangNgay(diemHienTai.GioDen, windowStart);
                        var dotDung = new Ellipse
                        {
                            Width = diemHienTai.LaDiemTranh ? 8 : 5,
                            Height = diemHienTai.LaDiemTranh ? 8 : 5,
                            Fill = diemHienTai.LaDiemTranh ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : brush,
                            Stroke = Brushes.White,
                            StrokeThickness = 1.0,
                            ToolTip = $"{tau.SoHieuMacTau} dừng tại {diemHienTai.TenGa} ({diemHienTai.ThoiGianDungPhut} phút)" +
                                      (diemHienTai.LaDiemTranh ? " [ĐIỂM TRÁNH TÀU]" : ""),
                            Tag = tau,
                            Cursor = Cursors.Hand
                        };

                        dotDung.MouseEnter += (s, e) => FocusTau(tau);
                        dotDung.MouseLeave += (s, e) => UnfocusTau();
                        dotDung.MouseDown += (s, e) => ChonChuyenTau(tau);

                        Canvas.SetLeft(dotDung, xDen - (diemHienTai.LaDiemTranh ? 4 : 2.5));
                        Canvas.SetTop(dotDung, yHienTai - (diemHienTai.LaDiemTranh ? 4 : 2.5));
                        cvsBieuDo.Children.Add(dotDung);
                        visual.Dots.Add(dotDung);
                    }
                }

                // 2. Phân đoạn CHẠY KHU GIAN (Diagonal segment: ga i GioDi -> ga i+1 GioDen)
                if (i < tau.DanhSachDiem.Count - 1)
                {
                    var diemKeTiep = tau.DanhSachDiem[i + 1];
                    double yKeTiep = TinhToaDoY(diemKeTiep.LyTrinhKm);

                    VePhanDoan(diemHienTai.GioDi, yHienTai, diemKeTiep.GioDen, yKeTiep, windowStart, windowEnd, brush, tau, visual, ref firstVisiblePoint, ref isCrossDayStart);
                }
            }

            // Gắn nhãn tên mác tàu
            if (firstVisiblePoint.HasValue)
            {
                Point p = firstVisiblePoint.Value;

                string nhanHienThi = isCrossDayStart
                    ? $"{tau.SoHieuMacTau} (Tiếp từ {tau.GioXuatPhat:dd/MM})"
                    : $"{tau.SoHieuMacTau} ({tau.GioXuatPhat:HH:mm})";

                var bdTag = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(240, 255, 255, 255)),
                    BorderBrush = brush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(4, 1, 4, 1),
                    Cursor = Cursors.Hand,
                    ToolTip = $"{tau.SoHieuMacTau}: {tau.TenGaDi} ➔ {tau.TenGaDen}\nKhởi hành: {tau.GioXuatPhat:dd/MM/yyyy HH:mm}\nĐến đích: {tau.GioVeDich:dd/MM/yyyy HH:mm}",
                    Tag = tau
                };
                var txtMac = new TextBlock
                {
                    Text = nhanHienThi,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = brush
                };
                bdTag.Child = txtMac;

                bdTag.MouseEnter += (s, e) => FocusTau(tau);
                bdTag.MouseLeave += (s, e) => UnfocusTau();
                bdTag.MouseDown += (s, e) => ChonChuyenTau(tau);

                Canvas.SetLeft(bdTag, Math.Min(p.X + 4, X_LEFT + X_WIDTH - 120));
                Canvas.SetTop(bdTag, Math.Max(Y_TOP - 20, p.Y - 18));
                cvsBieuDo.Children.Add(bdTag);
                visual.LabelBorder = bdTag;
            }

            // Gắn nhãn ga cuối / kết thúc hành trình (neo kết thúc, tránh cảm giác nét vẽ bị đứt cụt)
            var diemCuoi = tau.DanhSachDiem[^1];
            if (diemCuoi.GioDen >= windowStart && diemCuoi.GioDen <= windowEnd)
            {
                double xCuoi = TinhToaDoXKhoangNgay(diemCuoi.GioDen, windowStart);
                double yCuoi = TinhToaDoY(diemCuoi.LyTrinhKm);

                var dotCuoi = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = brush,
                    Stroke = Brushes.White,
                    StrokeThickness = 1.5,
                    ToolTip = $"{tau.SoHieuMacTau} kết thúc hành trình tại ga {diemCuoi.TenGa} lúc {diemCuoi.GioDen:HH:mm (dd/MM)}",
                    Cursor = Cursors.Hand
                };
                Canvas.SetLeft(dotCuoi, xCuoi - 4);
                Canvas.SetTop(dotCuoi, yCuoi - 4);
                cvsBieuDo.Children.Add(dotCuoi);
                visual.Dots.Add(dotCuoi);

                var bdEndTag = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                    BorderBrush = brush,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(3, 1, 3, 1),
                    ToolTip = $"{tau.SoHieuMacTau} kết thúc hành trình tại {diemCuoi.TenGa} ({diemCuoi.GioDen:HH:mm})",
                    Cursor = Cursors.Hand
                };
                var txtEnd = new TextBlock
                {
                    Text = $"🏁 {diemCuoi.TenGa} ({diemCuoi.GioDen:HH:mm})",
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = brush
                };
                bdEndTag.Child = txtEnd;
                bdEndTag.MouseEnter += (s, e) => FocusTau(tau);
                bdEndTag.MouseLeave += (s, e) => UnfocusTau();
                bdEndTag.MouseDown += (s, e) => ChonChuyenTau(tau);

                Canvas.SetLeft(bdEndTag, Math.Min(xCuoi + 5, X_LEFT + X_WIDTH - 120));
                Canvas.SetTop(bdEndTag, yCuoi - 9);
                cvsBieuDo.Children.Add(bdEndTag);
            }

            _tauVisuals.Add(visual);
        }

        private void VePhanDoan(DateTime dt1, double y1, DateTime dt2, double y2,
                                DateTime windowStart, DateTime windowEnd,
                                Brush brush, HanhTrinhTauDTO tau, TauVisual visual,
                                ref Point? firstVisiblePoint, ref bool isCrossDayStart)
        {
            if (dt2 < windowStart || dt1 > windowEnd) return;
            if (dt2 <= dt1) return;

            double t1 = (dt1 - windowStart).TotalMinutes;
            double t2 = (dt2 - windowStart).TotalMinutes;
            double cy1 = y1;
            double cy2 = y2;

            // Cắt mép trái (00:00)
            if (t1 < 0)
            {
                double ratio = (0 - t1) / (t2 - t1);
                cy1 = y1 + ratio * (y2 - y1);
                t1 = 0;
                if (!firstVisiblePoint.HasValue) isCrossDayStart = true;
            }

            // Cắt mép phải (24:00)
            if (t2 > 1440)
            {
                double ratio = (1440 - t1) / (t2 - t1);
                cy2 = cy1 + ratio * (y2 - cy1);
                t2 = 1440;
            }

            double x1 = X_LEFT + (t1 / 1440.0) * X_WIDTH;
            double x2 = X_LEFT + (t2 / 1440.0) * X_WIDTH;

            var line = new Line
            {
                X1 = x1,
                Y1 = cy1,
                X2 = x2,
                Y2 = cy2,
                Stroke = brush,
                StrokeThickness = 1.8,
                Opacity = 0.55,
                Cursor = Cursors.Hand,
                Tag = tau
            };

            // Sự kiện tương tác UX: Hover vào sáng rõ, mờ các tàu khác
            line.MouseEnter += (s, e) => FocusTau(tau);
            line.MouseLeave += (s, e) => UnfocusTau();
            line.MouseDown += (s, e) => ChonChuyenTau(tau);

            cvsBieuDo.Children.Add(line);
            visual.Lines.Add(line);

            if (!firstVisiblePoint.HasValue)
            {
                firstVisiblePoint = new Point(x1, cy1);
            }
        }

        // =========================================================================
        // THUẬT TOÁN ĐỊNH VỊ VÀ RENDER ĐIỂM XUNG ĐỘT TRÊN BIỂU ĐỒ MAREY
        // (KINEMATIC-ALIGNED TRAIN CONFLICT MARKERS)
        // =========================================================================

        private void VeCacDiemXungDotChuanXac()
        {
            if (_dsXungDot.Count == 0) return;
            DateTime ngayGoc = dpNgay.SelectedDate ?? DateTime.Today;

            foreach (var xd in _dsXungDot)
            {
                // Định vị marker trực tiếp từ mô hình động học chính xác (Thời điểm xung đột & Lý trình Km)
                // Đảm bảo Marker, Sidebar và Tooltip đồng bộ 100%, không bị ảnh hưởng bởi line clipping hay bộ lọc
                double x = TinhToaDoXKhoangNgay(xd.ThoiDiemXungDot, ngayGoc.Date);
                double y = TinhToaDoY(xd.LyTrinhUocTinhKm);

                VeMarkerXungDot(xd, new Point(x, y));
            }
        }

        private void VeMarkerXungDot(XungDotKhuGianDTO xd, Point pt)
        {
            double x = pt.X;
            double y = pt.Y;

            // Vòng tròn cảnh báo đỏ (tâm đặt chính xác tại x, y)
            var ringOuter = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = TaoBrush(Color.FromArgb(45, 239, 68, 68)),
                Stroke = TaoBrush(Color.FromRgb(220, 38, 38)),
                StrokeThickness = 1.5,
                Cursor = Cursors.Hand,
                ToolTip = xd.MoTaChiTiet,
                Tag = xd
            };
            Canvas.SetLeft(ringOuter, x - 10);
            Canvas.SetTop(ringOuter, y - 10);

            var dotInner = new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = TaoBrush(Color.FromRgb(220, 38, 38)),
                Stroke = Brushes.White,
                StrokeThickness = 1.2,
                Cursor = Cursors.Hand,
                ToolTip = xd.MoTaChiTiet,
                Tag = xd
            };
            Canvas.SetLeft(dotInner, x - 3.5);
            Canvas.SetTop(dotInner, y - 3.5);

            // Nhãn cảnh báo nổi bên cạnh
            var borderTag = new Border
            {
                Background = TaoBrush(Color.FromRgb(254, 242, 242)),
                BorderBrush = TaoBrush(Color.FromRgb(248, 113, 113)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(5, 1, 5, 1),
                Cursor = Cursors.Hand,
                ToolTip = xd.MoTaChiTiet,
                Tag = xd
            };
            var txtTag = new TextBlock
            {
                Text = $"⚠️ {xd.SoHieuMacTau1} x {xd.SoHieuMacTau2}",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = TaoBrush(Color.FromRgb(153, 27, 27))
            };
            borderTag.Child = txtTag;

            Canvas.SetLeft(borderTag, x + 12);
            Canvas.SetTop(borderTag, y - 9);

            MouseButtonEventHandler clickHandler = (s, e) =>
            {
                string thongTinRay = xd.SoHieuDuongRayDeXuat.HasValue 
                    ? $"Đường tránh #{xd.SoHieuDuongRayDeXuat.Value}" 
                    : "Đường tránh chỉ định bởi Trực ban ga";

                string thongTinDung = xd.SoPhutTangLichTrinh > 0 && xd.SoPhutTangLichTrinh != xd.SoPhutDungDeXuat
                    ? $"chờ tránh +{xd.SoPhutTangLichTrinh} phút, tổng đỗ {xd.SoPhutDungDeXuat} phút"
                    : $"đỗ {xd.SoPhutDungDeXuat} phút";

                string thongBao = $"{xd.MoTaChiTiet}\n\n" +
                                  $"• Tàu ưu tiên cao hơn: {xd.SoHieuTauUuTien} (Cấp ưu tiên {xd.MucUuTienTauUuTien} — Giữ nguyên lịch chạy)\n" +
                                  $"• Tàu phải dừng tránh: {xd.SoHieuTauBiTranh} (Cấp ưu tiên {xd.MucUuTienTauBiTranh} — Dừng nhường đường)\n" +
                                  $"• Ga đề xuất dừng tránh: Ga {xd.TenGaDeXuatTranh} (Lý trình Km {xd.LyTrinhGaTranhKm:F1})\n" +
                                  $"• Kế hoạch dừng tránh: Đến {xd.GioDenGaTranh:HH:mm} — Đi {xd.GioDiGaTranh:HH:mm} ({thongTinDung} trên {thongTinRay})\n\n" +
                                  "Bạn có muốn hệ thống tự động dời giờ và xếp tránh an toàn cho đoàn tàu này ngay không?";

                var r = MessageBox.Show(thongBao, "Chi Tiết Xung Đột Khu Gian", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r == MessageBoxResult.Yes)
                {
                    ApDungGiaiQuyetXungDot(xd);
                }
            };

            ringOuter.MouseDown += clickHandler;
            dotInner.MouseDown += clickHandler;
            borderTag.MouseDown += clickHandler;

            cvsBieuDo.Children.Add(ringOuter);
            cvsBieuDo.Children.Add(dotInner);
            cvsBieuDo.Children.Add(borderTag);

            _xungDotVisuals.Add(new XungDotVisual
            {
                Xd = xd,
                RingOuter = ringOuter,
                DotInner = dotInner,
                TagBorder = borderTag,
                ViTriGiao = pt
            });
        }

        // =========================================================================
        // HỆ THỐNG UX TƯƠNG TÁC (DEFAULT DIMMED -> HOVER SÁNG RỰC & FOCUS)
        // =========================================================================

        private void ApDungTrangThaiMacDinh()
        {
            foreach (var v in _tauVisuals)
            {
                bool isSelected = (_selectedTau != null && _selectedTau.MaChuyenTau == v.Tau.MaChuyenTau);

                foreach (var line in v.Lines)
                {
                    line.StrokeThickness = isSelected ? 4.0 : 2.2;
                    line.Opacity = isSelected ? 1.0 : (_selectedTau != null ? 0.25 : 0.85);
                }

                foreach (var dot in v.Dots)
                {
                    dot.Opacity = isSelected ? 1.0 : (_selectedTau != null ? 0.35 : 0.85);
                    dot.Width = isSelected ? 8 : 5;
                    dot.Height = isSelected ? 8 : 5;
                }

                if (v.LabelBorder != null)
                {
                    v.LabelBorder.Opacity = isSelected ? 1.0 : (_selectedTau != null ? 0.40 : 0.85);
                }
            }

            foreach (var xd in _xungDotVisuals)
            {
                xd.RingOuter.Opacity = 0.85;
                xd.DotInner.Opacity = 0.85;
                xd.TagBorder.Opacity = 0.90;
                xd.RingOuter.StrokeThickness = 1.5;
            }
        }

        private void FocusTau(HanhTrinhTauDTO tau)
        {
            // Làm sáng rõ tàu được hover, làm mờ các tàu khác (Dimming Effect)
            foreach (var v in _tauVisuals)
            {
                bool isTarget = (v.Tau.MaChuyenTau == tau.MaChuyenTau);

                foreach (var line in v.Lines)
                {
                    line.StrokeThickness = isTarget ? 3.8 : 1.2;
                    line.Opacity = isTarget ? 1.0 : 0.18; // Mờ hẳn các tàu khác để làm nổi bật tàu đang xem
                }

                foreach (var dot in v.Dots)
                {
                    dot.Opacity = isTarget ? 1.0 : 0.15;
                    dot.Width = isTarget ? 8 : 4;
                    dot.Height = isTarget ? 8 : 4;
                }

                if (v.LabelBorder != null)
                {
                    v.LabelBorder.Opacity = isTarget ? 1.0 : 0.20;
                }
            }

            // Làm sáng rực các điểm xung đột liên quan đến tàu này
            foreach (var xd in _xungDotVisuals)
            {
                bool coLienQuan = (xd.Xd.MaChuyenTau1 == tau.MaChuyenTau || xd.Xd.MaChuyenTau2 == tau.MaChuyenTau);
                xd.RingOuter.Opacity = coLienQuan ? 1.0 : 0.20;
                xd.DotInner.Opacity = coLienQuan ? 1.0 : 0.20;
                xd.TagBorder.Opacity = coLienQuan ? 1.0 : 0.20;
                xd.RingOuter.StrokeThickness = coLienQuan ? 2.5 : 1.0;
            }

            HienThiTooltip(tau);
        }

        private void UnfocusTau()
        {
            bdHoverTooltip.Visibility = Visibility.Collapsed;
            ApDungTrangThaiMacDinh();
        }

        private void ChonChuyenTau(HanhTrinhTauDTO tau)
        {
            _selectedTau = tau;
            txtChiTietTauTitle.Text = $"CHI TIẾT LỘ TRÌNH DỪNG GA: TÀU {tau.SoHieuMacTau} ({tau.TenGaDi} ➔ {tau.TenGaDen}) • {tau.DanhSachDiem.Count} Ga Dừng";
            dgChiTietLichDung.ItemsSource = tau.DanhSachDiem;
            bdChiTietLichDung.Visibility = Visibility.Visible;
            splChiTiet.Visibility = Visibility.Visible;
            if (_isBangChiTietThuGon)
            {
                rowChiTiet.Height = GridLength.Auto;
                dgChiTietLichDung.Visibility = Visibility.Collapsed;
            }
            else
            {
                rowChiTiet.Height = new GridLength(Math.Max(200, _savedChiTietHeight), GridUnitType.Pixel);
                dgChiTietLichDung.Visibility = Visibility.Visible;
            }
            ApDungTrangThaiMacDinh();
        }

        private void HienThiTooltip(HanhTrinhTauDTO tau)
        {
            txtHoverTitle.Text = $"Mác Tàu: {tau.SoHieuMacTau} ({tau.LoaiTau})";
            txtHoverDetails.Text = $"Hành trình: {tau.TenGaDi} ➔ {tau.TenGaDen} | Đi: {tau.GioXuatPhat:HH:mm} - Đến: {tau.GioVeDich:HH:mm}";
            txtHoverSub.Text = $"Ưu tiên: Cấp {tau.MucUuTien} | Chiều dài: {tau.TongChieuDaiM:F1}m | Điểm dừng: {tau.DanhSachDiem.Count} ga";
            bdHoverTooltip.Visibility = Visibility.Visible;
        }

        private void CvsBieuDo_MouseMove(object sender, MouseEventArgs e)
        {
            if (bdHoverTooltip.Visibility == Visibility.Visible)
            {
                Point pos = e.GetPosition(grdBieuDoContainer);
                double left = pos.X + 15;
                double top = pos.Y + 15;
                double containerWidth = grdBieuDoContainer.ActualWidth;
                double containerHeight = grdBieuDoContainer.ActualHeight;

                if (containerWidth > 0 && bdHoverTooltip.ActualWidth > 0 && left + bdHoverTooltip.ActualWidth > containerWidth - 10)
                {
                    left = Math.Max(10, pos.X - bdHoverTooltip.ActualWidth - 15);
                }
                if (containerHeight > 0 && bdHoverTooltip.ActualHeight > 0 && top + bdHoverTooltip.ActualHeight > containerHeight - 10)
                {
                    top = Math.Max(10, pos.Y - bdHoverTooltip.ActualHeight - 15);
                }
                bdHoverTooltip.Margin = new Thickness(left, top, 0, 0);
            }
        }

        private void CvsBieuDo_MouseLeave(object sender, MouseEventArgs e)
        {
            bdHoverTooltip.Visibility = Visibility.Collapsed;
        }

        // =========================================================================
        // TỌA ĐỘ HỌC
        // =========================================================================

        private double TinhToaDoY(decimal km)
        {
            if (_lyTrinhYList.Count == 0) return Y_TOP;
            if (km <= _lyTrinhYList[0].Km) return _lyTrinhYList[0].Y;
            if (km >= _lyTrinhYList[^1].Km) return _lyTrinhYList[^1].Y;

            // Tìm 2 ga liền kề
            for (int i = 0; i < _lyTrinhYList.Count - 1; i++)
            {
                var g1 = _lyTrinhYList[i];
                var g2 = _lyTrinhYList[i + 1];

                if (km >= g1.Km && km <= g2.Km)
                {
                    if (g2.Km == g1.Km) return g1.Y;
                    double ratio = (double)((km - g1.Km) / (g2.Km - g1.Km));
                    return g1.Y + ratio * (g2.Y - g1.Y);
                }
            }

            return _lyTrinhYList[^1].Y;
        }

        private double TinhToaDoXKhoangNgay(DateTime dt, DateTime windowStart)
        {
            double totalMinutes = (dt - windowStart).TotalMinutes;
            if (totalMinutes < 0) totalMinutes = 0;
            if (totalMinutes > 1440) totalMinutes = 1440;
            return X_LEFT + (totalMinutes / 1440.0) * X_WIDTH;
        }

        // =========================================================================
        // SIDEBAR & DANH SÁCH XUNG ĐỘT
        // =========================================================================

        private void HienThiDanhSachXungDot()
        {
            pnlDanhSachXungDot.Children.Clear();

            foreach (var xd in _dsXungDot)
            {
                // Thẻ card thiết kế phẳng chuẩn Desktop Enterprise: Nền trắng sạch, viền xám kỹ thuật, dải nhấn màu đỏ cảnh báo sắc nét
                var card = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // #CBD5E1
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(0, 0, 0, 7),
                    SnapsToDevicePixels = true
                };

                var gridCard = new Grid();
                gridCard.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) }); // Dải nhấn cạnh trái
                gridCard.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Dải màu nhấn mép trái (Đỏ cho đối đầu, Cam cho giãn cách)
                Color colorAccent = xd.LoaiXungDot == "DOI_DAU" ? Color.FromRgb(220, 38, 38) : Color.FromRgb(217, 119, 6);
                var leftStrip = new Border
                {
                    Background = new SolidColorBrush(colorAccent),
                    CornerRadius = new CornerRadius(2, 0, 0, 2)
                };
                Grid.SetColumn(leftStrip, 0);
                gridCard.Children.Add(leftStrip);

                var contentPanel = new StackPanel { Margin = new Thickness(9, 7, 9, 7) };
                Grid.SetColumn(contentPanel, 1);

                // Dòng 1: Tiêu đề xung đột + Badge cảnh báo công nghiệp
                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var txtTitle = new TextBlock
                {
                    Text = $"{xd.SoHieuMacTau1} ✕ {xd.SoHieuMacTau2}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)) // Slate đậm rõ ràng
                };
                Grid.SetColumn(txtTitle, 0);

                var badgeLoai = new Border
                {
                    Background = new SolidColorBrush(colorAccent),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(6, 1, 6, 1),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var txtLoai = new TextBlock
                {
                    Text = xd.LoaiXungDot == "DOI_DAU" ? "ĐỐI ĐẦU" : "GIÃN CÁCH",
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                badgeLoai.Child = txtLoai;
                Grid.SetColumn(badgeLoai, 1);

                headerGrid.Children.Add(txtTitle);
                headerGrid.Children.Add(badgeLoai);
                contentPanel.Children.Add(headerGrid);

                // Dòng 2: Khu gian chuẩn theo hạ tầng đường sắt & thời điểm
                var txtKhuGian = new TextBlock
                {
                    Text = $"{xd.TenGaDau} — {xd.TenGaCuoi} (Km {xd.LyTrinhUocTinhKm:F1}) | {xd.ThoiDiemXungDot:HH:mm}",
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    Margin = new Thickness(0, 3, 0, 4),
                    TextWrapping = TextWrapping.Wrap
                };
                contentPanel.Children.Add(txtKhuGian);

                // Dòng 3: Khung đề xuất giải quyết điều độ kỹ thuật
                var boxDeXuat = new Border
                {
                    Background = TaoBrush(Color.FromRgb(241, 245, 249)), // #F1F5F9
                    BorderBrush = TaoBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                string rayDeXuat = xd.SoHieuDuongRayDeXuat.HasValue ? $"đường #{xd.SoHieuDuongRayDeXuat.Value}" : "đường tránh phân bổ";
                string thoiGianTheHien = xd.SoPhutTangLichTrinh > 0 && xd.SoPhutTangLichTrinh != xd.SoPhutDungDeXuat
                    ? $"chờ +{xd.SoPhutTangLichTrinh}p, tổng đỗ {xd.SoPhutDungDeXuat}p"
                    : $"{xd.SoPhutDungDeXuat} phút";
                var txtDeXuat = new TextBlock
                {
                    Text = $"Đề xuất: Tàu {xd.SoHieuTauBiTranh} dừng Ga {xd.TenGaDeXuatTranh} ({thoiGianTheHien}, {rayDeXuat})",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TaoBrush(Color.FromRgb(0, 59, 115)), // Xanh VNR
                    TextWrapping = TextWrapping.Wrap
                };
                boxDeXuat.Child = txtDeXuat;
                contentPanel.Children.Add(boxDeXuat);

                // Dòng 4: Nút Xem Vị Trí & Nút Xếp Tránh
                var btnGrid = new Grid();
                btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var btnXem = new Button
                {
                    Content = "Xem Vị Trí",
                    Height = 24,
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Background = Brushes.White,
                    BorderBrush = TaoBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1),
                    Foreground = TaoBrush(Color.FromRgb(51, 65, 85)),
                    Cursor = Cursors.Hand
                };
                Action focusAction = () =>
                {
                    var visual = _xungDotVisuals.FirstOrDefault(v => v.Xd.MaXungDot == xd.MaXungDot);
                    double xFocus = visual != null ? visual.ViTriGiao.X : TinhToaDoXKhoangNgay(xd.ThoiDiemXungDot, dpNgay.SelectedDate ?? DateTime.Today);
                    double yFocus = visual != null ? visual.ViTriGiao.Y : TinhToaDoY(xd.LyTrinhUocTinhKm);

                    CuonToiViTri(xFocus, yFocus);

                    var v1 = _tauVisuals.FirstOrDefault(v => v.Tau.MaChuyenTau == xd.MaChuyenTau1);
                    if (v1 != null) FocusTau(v1.Tau);
                };
                btnXem.Click += (s, e) => focusAction();
                card.Cursor = Cursors.Hand;
                card.MouseDown += (s, e) => focusAction();
                Grid.SetColumn(btnXem, 0);

                var btnApDung = new Button
                {
                    Content = "Xếp Tránh",
                    Height = 24,
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Background = TaoBrush(Color.FromRgb(0, 59, 115)), // Xanh VNR
                    BorderThickness = new Thickness(0),
                    Foreground = Brushes.White,
                    Cursor = Cursors.Hand,
                    ToolTip = $"Áp dụng phương án: Tàu {xd.SoHieuTauBiTranh} dừng tránh tại {xd.TenGaDeXuatTranh}"
                };
                btnApDung.Click += (s, e) =>
                {
                    e.Handled = true;
                    ApDungGiaiQuyetXungDot(xd);
                };
                Grid.SetColumn(btnApDung, 2);

                btnGrid.Children.Add(btnXem);
                btnGrid.Children.Add(btnApDung);
                contentPanel.Children.Add(btnGrid);

                gridCard.Children.Add(contentPanel);
                card.Child = gridCard;
                pnlDanhSachXungDot.Children.Add(card);
            }
        }

        private void HienThiDanhSachTau()
        {
            pnlDanhSachTau.Children.Clear();

            foreach (var tau in _dsHanhTrinh)
            {
                var border = new Border
                {
                    Background = TaoBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = TaoBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 0, 0, 4),
                    Cursor = Cursors.Hand
                };

                var sp = new StackPanel { Orientation = Orientation.Horizontal };

                Color c;
                try { c = (Color)ColorConverter.ConvertFromString(tau.MauSacHex); }
                catch { c = Color.FromRgb(0, 59, 115); }

                var rectMau = new Rectangle
                {
                    Width = 8,
                    Height = 8,
                    Fill = TaoBrush(c),
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var txtSoHieu = new TextBlock
                {
                    Text = tau.SoHieuMacTau,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = TaoBrush(Color.FromRgb(0, 59, 115)),
                    Width = 50,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var txtHuong = new TextBlock
                {
                    Text = $"{tau.TenGaDi} ➔ {tau.TenGaDen}",
                    FontSize = 10,
                    Foreground = TaoBrush(Color.FromRgb(71, 85, 105)),
                    VerticalAlignment = VerticalAlignment.Center
                };

                sp.Children.Add(rectMau);
                sp.Children.Add(txtSoHieu);
                sp.Children.Add(txtHuong);
                border.Child = sp;

                border.MouseEnter += (s, e) => FocusTau(tau);
                border.MouseLeave += (s, e) => UnfocusTau();
                border.MouseDown += (s, e) => ChonChuyenTau(tau);

                pnlDanhSachTau.Children.Add(border);
            }
        }

        private void ApDungGiaiQuyetXungDot(XungDotKhuGianDTO xd)
        {
            string rayApDung = xd.SoHieuDuongRayDeXuat.HasValue ? $"đường #{xd.SoHieuDuongRayDeXuat.Value}" : "đường tránh phân bổ";
            string thoiGianChiTiet = xd.SoPhutTangLichTrinh > 0 && xd.SoPhutTangLichTrinh != xd.SoPhutDungDeXuat
                ? $"chờ tránh +{xd.SoPhutTangLichTrinh} phút (tổng đỗ {xd.SoPhutDungDeXuat} phút tại ga, gồm thời gian tác nghiệp sẵn có)"
                : $"{xd.SoPhutDungDeXuat} phút";

            var r = MessageBox.Show(
                $"XÁC NHẬN ĐIỀU ĐỘ XẾP TRÁNH:\n\n" +
                $"• Cặp xung đột: {xd.SoHieuMacTau1} ✕ {xd.SoHieuMacTau2}\n" +
                $"• Khu gian: {xd.TenGaDau} — {xd.TenGaCuoi} (Km {xd.LyTrinhUocTinhKm:F1})\n" +
                $"• Phương án: Tàu {xd.SoHieuTauBiTranh} sẽ dừng tránh tại Ga {xd.TenGaDeXuatTranh} ({thoiGianChiTiet}, {rayApDung})\n" +
                $"• Tác động: Cập nhật giờ chạy và lịch dừng trong CSDL vận hành.\n\n" +
                $"Bạn có chắc chắn muốn áp dụng phương án xếp tránh này?",
                "Xác Nhận Điều Độ Xếp Tránh",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (r != MessageBoxResult.Yes) return;

            try
            {
                bool ok = _dieuDoService.GiaiQuyetMotXungDot(xd, out string baoCao);
                if (ok)
                {
                    MessageBox.Show(baoCao, "Điều Độ Xếp Tránh Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                    DuLieuDieuDoThayDoi?.Invoke();
                }
                else
                {
                    MessageBox.Show(baoCao, "Thông Báo Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xếp tránh tàu:\n{ex.Message}", "Lỗi Cơ Sở Dữ Liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ChonChuyenTauTheoId(int maChuyenTau, DateTime? ngay = null)
        {
            if (ngay.HasValue && dpNgay.SelectedDate != ngay.Value.Date)
            {
                dpNgay.SelectedDate = ngay.Value.Date;
            }
            else
            {
                LoadData();
            }

            var tau = _dsHanhTrinh.FirstOrDefault(t => t.MaChuyenTau == maChuyenTau);
            if (tau != null)
            {
                ChonChuyenTau(tau);
                FocusTau(tau);

                var v = _tauVisuals.FirstOrDefault(tv => tv.Tau.MaChuyenTau == maChuyenTau);
                if (v != null && v.Lines.Count > 0)
                {
                    double x = v.Lines[0].X1;
                    double y = v.Lines[0].Y1;
                    CuonToiViTri(x, y);
                }
            }
        }

        private void BtnMoLichTrinh_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTau != null && Application.Current.MainWindow is MainWindow mw)
            {
                mw.NavigateToLichTrinh(_selectedTau.MaChuyenTau, dpNgay.SelectedDate);
            }
        }

        // =========================================================================
        // CÁC SỰ KIỆN NÚT BẤM (BUTTON HANDLERS)
        // =========================================================================

        private void BtnChonNgayMau_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string dateStr && DateTime.TryParse(dateStr, out DateTime targetDate))
            {
                dpNgay.SelectedDate = targetDate;
            }
        }

        private void BtnTuDongXepTranh_Click(object sender, RoutedEventArgs e)
        {
            DateTime ngay = dpNgay.SelectedDate ?? DateTime.Today;
            var r = MessageBox.Show($"Xác nhận chạy Thuật toán Tự động Xếp Tránh Tàu cho ngày {ngay:dd/MM/yyyy}?\n\n" +
                                    "Hệ thống sẽ:\n" +
                                    "1. So sánh cấp ưu tiên giữa các đoàn tàu ngược chiều (SE > Tàu Chợ > Tàu Hàng).\n" +
                                    "2. Phân bổ đường ray tránh tại ga có chiều dài đường tránh đủ chứa đoàn tàu.\n" +
                                    "3. Tự động dời giờ đỗ và tịnh tiến lịch trình các ga sau mà không làm thay đổi tốc độ chuẩn.\n" +
                                    "4. Ghi nhận nhật ký chậm giờ 'CHO_TRANH_TAU' phục vụ kiểm toán.",
                                    "Tự Động Xếp Tránh Tàu", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (r == MessageBoxResult.Yes)
            {
                try
                {
                    bool ok = _dieuDoService.TuDongGiaiQuyetTranhTau(ngay, out string baoCao);
                    MessageBox.Show(baoCao, ok ? "Xếp Tránh Tàu Thành Công" : "Thông Báo", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
                    LoadData();
                    if (ok)
                    {
                        DuLieuDieuDoThayDoi?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi trong quá trình tự động xếp tránh tàu:\n{ex.Message}", "Lỗi Thực Thi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnKiemTraXungDot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime ngay = dpNgay.SelectedDate ?? DateTime.Today;
                _dsXungDot = _dieuDoService.KiemTraXungDotToanTuyen(ngay);
                CapNhatThongKe();
                VeBiểuĐồ();
                HienThiDanhSachXungDot();

                if (_dsXungDot.Count == 0)
                {
                    MessageBox.Show($"Kiểm tra an toàn toàn tuyến ngày {ngay:dd/MM/yyyy}: Không phát hiện xung đột nào!", "An Toàn Tuyệt Đối", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Phát hiện {_dsXungDot.Count} xung đột khu gian cần giải quyết!\nVui lòng xem danh sách cảnh báo ở cột bên phải.", "Cảnh Báo Xung Đột", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi kiểm tra xung đột an toàn:\n{ex.Message}", "Lỗi Kiểm Tra", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNapLai_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void DpNgay_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadData();
        }

        private void CboLocLoaiTau_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) VeBiểuĐồ();
        }

        private void SldZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (canvasScale != null && txtZoomPercent != null)
            {
                canvasScale.ScaleX = e.NewValue;
                canvasScale.ScaleY = e.NewValue;
                txtZoomPercent.Text = $"{(int)(e.NewValue * 100)}%";
            }
        }

        private void BtnThuGonChiTiet_Click(object sender, RoutedEventArgs e)
        {
            _isBangChiTietThuGon = !_isBangChiTietThuGon;
            if (_isBangChiTietThuGon)
            {
                if (rowChiTiet.ActualHeight > 80)
                {
                    _savedChiTietHeight = rowChiTiet.ActualHeight;
                }
                dgChiTietLichDung.Visibility = Visibility.Collapsed;
                splChiTiet.Visibility = Visibility.Collapsed;
                rowChiTiet.Height = GridLength.Auto;
                txtThuGon.Text = "Mở Rộng";
                iconThuGon.Symbol = Wpf.Ui.Controls.SymbolRegular.ChevronUp24;
            }
            else
            {
                dgChiTietLichDung.Visibility = Visibility.Visible;
                splChiTiet.Visibility = Visibility.Visible;
                rowChiTiet.Height = new GridLength(Math.Max(180, _savedChiTietHeight), GridUnitType.Pixel);
                txtThuGon.Text = "Thu Gọn";
                iconThuGon.Symbol = Wpf.Ui.Controls.SymbolRegular.ChevronDown24;
            }
        }

        private void BtnDongChiTiet_Click(object sender, RoutedEventArgs e)
        {
            _selectedTau = null;
            txtChiTietTauTitle.Text = "CHI TIẾT LỘ TRÌNH DỪNG GA: Vui lòng click chọn một đường tàu trên biểu đồ";
            dgChiTietLichDung.ItemsSource = null;
            bdChiTietLichDung.Visibility = Visibility.Collapsed;
            splChiTiet.Visibility = Visibility.Collapsed;
            rowChiTiet.Height = GridLength.Auto;
            ApDungTrangThaiMacDinh();
        }
    }
}
