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

        // Bảng ánh xạ tọa độ Y của các ga (sorted by LyTrinhKm)
        private readonly List<(decimal Km, double Y, string TenGa, string MaCode, string HangGa)> _lyTrinhYList = new();
        private HanhTrinhTauDTO? _selectedTau;

        // Quản lý các phần tử trực quan để phục vụ hiệu ứng Hover / Focus / Dimming
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

        public BieuDoChayTauPage()
        {
            InitializeComponent();
            try
            {
                // Tự động xác định ngày mặc định từ CSDL: nếu hôm nay có tàu thì lấy hôm nay, nếu không thì lấy ngày gần nhất có tàu
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

        /// <summary>
        /// Tải động danh sách các ngày thực tế có chuyến tàu hoạt động trong CSDL lên thanh công cụ
        /// </summary>
        private void NapDanhSachNgayCoTau()
        {
            try
            {
                pnlQuickDates.Children.Clear();
                pnlQuickDates.Children.Add(new TextBlock
                {
                    Text = "Các ngày có tàu:",
                    FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                });

                DataTable dtNgay = _dieuDoService.LayDanhSachNgayCoTau();
                if (dtNgay == null || dtNgay.Rows.Count == 0) return;

                foreach (DataRow row in dtNgay.Rows)
                {
                    DateTime d = Convert.ToDateTime(row["Ngay"]);
                    int soTau = Convert.ToInt32(row["SoChuyenTau"]);

                    var btn = new Button
                    {
                        Content = $"{d:dd/MM} ({soTau} tàu)",
                        ToolTip = $"Xem biểu đồ ngày {d:dd/MM/yyyy} ({soTau} chuyến tàu)",
                        Style = (Style)FindResource("DateQuickButton"),
                        Tag = d
                    };

                    btn.Click += (s, args) =>
                    {
                        if (s is Button b && b.Tag is DateTime dt)
                        {
                            dpNgay.SelectedDate = dt;
                        }
                    };

                    pnlQuickDates.Children.Add(btn);
                }
            }
            catch
            {
                // Không chặn giao diện nếu gặp sự cố kết nối
            }
        }

        public void LoadData()
        {
            DateTime ngay = dpNgay.SelectedDate ?? _dieuDoService.LayNgayBieuDoMacDinh();

            try
            {
                _dtGa = _dieuDoService.LayDanhSachGaTuyen();
                KhoiTaoBangToaDoGa();

                _dsHanhTrinh = _dieuDoService.LayDuLieuBieuDo(ngay);
                _dsXungDot = _dieuDoService.KiemTraXungDotToanTuyen(ngay);

                CapNhatThongKe();
                VeBiểuĐồ();
                HienThiDanhSachXungDot();
                HienThiDanhSachTau();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu biểu đồ chạy tàu:\n{ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Khởi tạo bảng tọa độ Y cho từng ga đường sắt theo thứ tự lý trình Bắc -> Nam.
        /// Mỗi ga có một hàng riêng cách đều 44px, triệt tiêu hoàn toàn hiện tượng dính/chồng đè chữ.
        /// </summary>
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
                decimal km = Convert.ToDecimal(r["LyTrinhKm"]);
                string tenGa = r["TenGa"]?.ToString() ?? "";
                string maCode = r["MaGaCode"]?.ToString() ?? "";
                string hangGa = r["HangGa"]?.ToString() ?? "";

                double y = Y_TOP + i * ROW_HEIGHT;
                _lyTrinhYList.Add((km, y, tenGa, maCode, hangGa));
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
            txtSoXungDotSide.Text = $"{_dsXungDot.Count} LỖI";

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

            // Thiết lập trạng thái bình thường (Default: Mảnh mờ dịu mắt)
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

                // Đường kẻ dọc mốc giờ
                bool isMajorHour = (h % 6 == 0);
                var lineGio = new Line
                {
                    X1 = x,
                    Y1 = Y_TOP - 35,
                    X2 = x,
                    Y2 = maxY,
                    Stroke = new SolidColorBrush(isMajorHour ? Color.FromRgb(148, 163, 184) : Color.FromRgb(226, 232, 240)),
                    StrokeThickness = isMajorHour ? 1.5 : 1.0
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

                // Vạch chia 30 phút (nét đứt)
                if (h < 24)
                {
                    double x30 = X_LEFT + ((h + 0.5) / 24.0) * X_WIDTH;
                    var line30 = new Line
                    {
                        X1 = x30,
                        Y1 = Y_TOP,
                        X2 = x30,
                        Y2 = maxY,
                        Stroke = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                        StrokeThickness = 0.8,
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
                bool isDauMoi = item.Km == 0 || item.TenGa == "Sài Gòn" || item.TenGa == "Vinh" || item.TenGa == "Đà Nẵng" || item.TenGa == "Nha Trang";

                // Đường ray ngang qua ga
                var lineGa = new Line
                {
                    X1 = X_LEFT,
                    Y1 = y,
                    X2 = X_LEFT + X_WIDTH,
                    Y2 = y,
                    Stroke = new SolidColorBrush(isDauMoi ? Color.FromRgb(203, 213, 225) : Color.FromRgb(241, 245, 249)),
                    StrokeThickness = isDauMoi ? 1.2 : 0.8
                };
                cvsBieuDo.Children.Add(lineGa);

                // Nhãn Tên Ga (Bên trái thước đo)
                var txtGa = new TextBlock
                {
                    Text = item.TenGa,
                    FontSize = isDauMoi ? 11.5 : 11,
                    FontWeight = isDauMoi ? FontWeights.Bold : FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(isDauMoi ? Color.FromRgb(0, 59, 115) : Color.FromRgb(51, 65, 85)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Width = 115,
                    TextAlignment = TextAlignment.Right
                };
                Canvas.SetLeft(txtGa, 10);
                Canvas.SetTop(txtGa, y - 8);
                cvsBieuDo.Children.Add(txtGa);

                // Nhãn Lý Trình Km
                var txtKm = new TextBlock
                {
                    Text = $"{item.Km:F0}km",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontFamily = new FontFamily("Consolas")
                };
                Canvas.SetLeft(txtKm, 135);
                Canvas.SetTop(txtKm, y - 7);
                cvsBieuDo.Children.Add(txtKm);

                // Chấm mốc ga tại trục tọa độ
                var dotGa = new Ellipse
                {
                    Width = isDauMoi ? 7 : 5,
                    Height = isDauMoi ? 7 : 5,
                    Fill = new SolidColorBrush(isDauMoi ? Color.FromRgb(0, 59, 115) : Color.FromRgb(148, 163, 184))
                };
                Canvas.SetLeft(dotGa, X_LEFT - (isDauMoi ? 3.5 : 2.5));
                Canvas.SetTop(dotGa, y - (isDauMoi ? 3.5 : 2.5));
                cvsBieuDo.Children.Add(dotGa);
            }
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
        // THUẬT TOÁN TÍNH GIAO ĐIỂM HÌNH HỌC CHUẨN XÁC 100% TRÊN CANVAS
        // (SCREEN-SPACE TRUE LINE INTERSECTION)
        // =========================================================================

        private void VeCacDiemXungDotChuanXac()
        {
            if (_dsXungDot.Count == 0 || _tauVisuals.Count < 2) return;

            foreach (var xd in _dsXungDot)
            {
                // Tìm visual của 2 đoàn tàu có liên quan đến xung đột này
                var v1 = _tauVisuals.FirstOrDefault(v => v.Tau.MaChuyenTau == xd.MaChuyenTau1);
                var v2 = _tauVisuals.FirstOrDefault(v => v.Tau.MaChuyenTau == xd.MaChuyenTau2);

                Point? diemGiaoCanvas = null;

                if (v1 != null && v2 != null)
                {
                    // Quét từng cặp đoạn thẳng Line thực tế được vẽ trên Canvas giữa 2 tàu
                    foreach (var l1 in v1.Lines)
                    {
                        Point p1A = new(l1.X1, l1.Y1);
                        Point p1B = new(l1.X2, l1.Y2);

                        foreach (var l2 in v2.Lines)
                        {
                            Point p2A = new(l2.X1, l2.Y1);
                            Point p2B = new(l2.X2, l2.Y2);

                            Point? pt = TinhGiaoDiem2DoanThang(p1A, p1B, p2A, p2B);
                            if (pt.HasValue)
                            {
                                diemGiaoCanvas = pt.Value;
                                break;
                            }
                        }

                        if (diemGiaoCanvas.HasValue) break;
                    }
                }

                // Nếu tìm được giao điểm chính xác trên Canvas: Vẽ đốm đỏ NGAY TẠI TÂM GIAO ĐIỂM ĐÓ!
                if (diemGiaoCanvas.HasValue)
                {
                    VeMarkerXungDot(xd, diemGiaoCanvas.Value);
                }
                else
                {
                    // Fallback theo thời điểm tính toán nếu đoạn cắt nằm gần mép
                    DateTime ngayGoc = dpNgay.SelectedDate ?? DateTime.Today;
                    double x = TinhToaDoXKhoangNgay(xd.ThoiDiemXungDot, ngayGoc.Date);
                    double y = TinhToaDoY(xd.LyTrinhUocTinhKm);
                    VeMarkerXungDot(xd, new Point(x, y));
                }
            }
        }

        /// <summary>
        /// Tính toán giao điểm toán học 2D giữa 2 đoạn thẳng [A1, B1] và [A2, B2] trên Canvas.
        /// Chuẩn xác 100%, trả về null nếu không cắt nhau hoặc song song.
        /// </summary>
        private static Point? TinhGiaoDiem2DoanThang(Point a1, Point a2, Point b1, Point b2)
        {
            double dX1 = a2.X - a1.X;
            double dY1 = a2.Y - a1.Y;
            double dX2 = b2.X - b1.X;
            double dY2 = b2.Y - b1.Y;

            double det = dX1 * dY2 - dY1 * dX2;
            if (Math.Abs(det) < 1e-5) return null; // Song song hoặc trùng nhau

            double t = ((b1.X - a1.X) * dY2 - (b1.Y - a1.Y) * dX2) / det;
            double u = ((b1.X - a1.X) * dY1 - (b1.Y - a1.Y) * dX1) / det;

            if (t >= -0.001 && t <= 1.001 && u >= -0.001 && u <= 1.001)
            {
                return new Point(a1.X + t * dX1, a1.Y + t * dY1);
            }
            return null;
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
                Fill = new SolidColorBrush(Color.FromArgb(45, 239, 68, 68)),
                Stroke = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
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
                Fill = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
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
                Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(248, 113, 113)),
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
                Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27))
            };
            borderTag.Child = txtTag;

            Canvas.SetLeft(borderTag, x + 12);
            Canvas.SetTop(borderTag, y - 9);

            MouseButtonEventHandler clickHandler = (s, e) =>
            {
                string thongBao = $"{xd.MoTaChiTiet}\n\n" +
                                  $"• Tàu ưu tiên cao hơn: {xd.SoHieuTauUuTien} (Cấp ưu tiên {xd.MucUuTienTauUuTien} — Giữ nguyên lịch chạy)\n" +
                                  $"• Tàu phải dừng tránh: {xd.SoHieuTauBiTranh} (Cấp ưu tiên {xd.MucUuTienTauBiTranh} — Dừng nhường đường)\n" +
                                  $"• Ga đề xuất dừng tránh: Ga {xd.TenGaDeXuatTranh} (Lý trình Km {xd.LyTrinhGaTranhKm:F1})\n" +
                                  $"• Kế hoạch dừng tránh: Đến {xd.GioDenGaTranh:HH:mm} — Đi {xd.GioDiGaTranh:HH:mm} ({xd.SoPhutDungDeXuat} phút trên Đường tránh #{xd.SoHieuDuongRayDeXuat ?? 2})\n\n" +
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
                    line.StrokeThickness = isSelected ? 3.5 : 1.8;
                    line.Opacity = isSelected ? 1.0 : (_selectedTau != null ? 0.30 : 0.55);
                }

                foreach (var dot in v.Dots)
                {
                    dot.Opacity = isSelected ? 1.0 : (_selectedTau != null ? 0.35 : 0.65);
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
                Point pos = e.GetPosition(this);
                bdHoverTooltip.Margin = new Thickness(pos.X + 15, pos.Y + 15, 0, 0);
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
                    Text = $"⚠ {xd.SoHieuMacTau1} ✕ {xd.SoHieuMacTau2}",
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
                    Margin = new Thickness(0, 3, 0, 4)
                };
                contentPanel.Children.Add(txtKhuGian);

                // Dòng 3: Khung đề xuất giải quyết điều độ kỹ thuật
                var boxDeXuat = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)), // #F1F5F9
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                var txtDeXuat = new TextBlock
                {
                    Text = $"🎯 Đề xuất: Tàu {xd.SoHieuTauBiTranh} dừng Ga {xd.TenGaDeXuatTranh} ({xd.SoPhutDungDeXuat} phút, đường #{xd.SoHieuDuongRayDeXuat ?? 2})",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 59, 115)) // Xanh VNR
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
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1),
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    Cursor = Cursors.Hand
                };
                btnXem.Click += (s, e) =>
                {
                    var visual = _xungDotVisuals.FirstOrDefault(v => v.Xd.MaXungDot == xd.MaXungDot);
                    double xFocus = visual != null ? visual.ViTriGiao.X : TinhToaDoXKhoangNgay(xd.ThoiDiemXungDot, dpNgay.SelectedDate ?? DateTime.Today);
                    double yFocus = visual != null ? visual.ViTriGiao.Y : TinhToaDoY(xd.LyTrinhUocTinhKm);

                    svBieuDo.ScrollToHorizontalOffset(Math.Max(0, xFocus - 350));
                    svBieuDo.ScrollToVerticalOffset(Math.Max(0, yFocus - 200));

                    var v1 = _tauVisuals.FirstOrDefault(v => v.Tau.MaChuyenTau == xd.MaChuyenTau1);
                    if (v1 != null) FocusTau(v1.Tau);
                };
                Grid.SetColumn(btnXem, 0);

                var btnApDung = new Button
                {
                    Content = "Xếp Tránh",
                    Height = 24,
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromRgb(0, 59, 115)), // Xanh VNR
                    BorderThickness = new Thickness(0),
                    Foreground = Brushes.White,
                    Cursor = Cursors.Hand
                };
                btnApDung.Click += (s, e) => ApDungGiaiQuyetXungDot(xd);
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
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
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
                    Fill = new SolidColorBrush(c),
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var txtSoHieu = new TextBlock
                {
                    Text = tau.SoHieuMacTau,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 59, 115)),
                    Width = 50,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var txtHuong = new TextBlock
                {
                    Text = $"{tau.TenGaDi} ➔ {tau.TenGaDen}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
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
            try
            {
                bool ok = _dieuDoService.GiaiQuyetMotXungDot(xd, out string baoCao);
                if (ok)
                {
                    MessageBox.Show(baoCao, "Điều Độ Xếp Tránh Thành Công", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
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
                bool ok = _dieuDoService.TuDongGiaiQuyetTranhTau(ngay, out string baoCao);
                MessageBox.Show(baoCao, ok ? "Xếp Tránh Tàu Thành Công" : "Thông Báo", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
                LoadData();
            }
        }

        private void BtnKiemTraXungDot_Click(object sender, RoutedEventArgs e)
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

        private void BtnNapLai_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void DpNgay_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) LoadData();
        }

        private void CboLocLoaiTau_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void BtnDongChiTiet_Click(object sender, RoutedEventArgs e)
        {
            _selectedTau = null;
            txtChiTietTauTitle.Text = "CHI TIẾT LỘ TRÌNH DỪNG GA: Vui lòng click chọn một đường tàu trên biểu đồ";
            dgChiTietLichDung.ItemsSource = null;
            ApDungTrangThaiMacDinh();
        }
    }
}
