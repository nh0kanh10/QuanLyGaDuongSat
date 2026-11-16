using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using BUS.Services;
using DTO.Common;
using DTO.VanHanh;
using GUI.Views.Dialogs;

namespace GUI.Views.Pages
{
    public partial class DieuHanhThucTePage : Page
    {
        private readonly DieuDoService _dieuDoService = new();
        private readonly DispatcherTimer _simulationTimer = new();
        private DateTime _ngayHienTai = ThoiGianHeThongHelper.LayGioHienTai().Date;
        private int _tocDoNhan = 1; // 0 = Dung, 1 = 1x, 30 = 30x, 60 = 60x
        private bool _isLoaded = false;

        // Cấu trúc dữ liệu ga vẽ trên sơ đồ Synoptic
        private class GaSynoptic
        {
            public int MaGa { get; set; }
            public string TenGa { get; set; } = "";
            public string MaGaCode { get; set; } = "";
            public decimal LyTrinhKm { get; set; }
            public double X { get; set; }
            public bool CoTauDo { get; set; }
            public string SoHieuTauDo { get; set; } = "";
        }

        // Thông tin chặng hiển thị trên bảng Bán vé
        public class ChangVeHienThi
        {
            public int MaGaDi { get; set; }
            public int MaGaDen { get; set; }
            public string TenChang { get; set; } = "";
            public string GioDiKH { get; set; } = "";
            public string GioDiThucTe { get; set; } = "";
            public string MoTaTrangThai { get; set; } = "";
            public Brush MauNenTrangThai { get; set; } = Brushes.Transparent;
            public Brush MauChuTrangThai { get; set; } = Brushes.Black;
        }

        private readonly List<GaSynoptic> _dsGaSynoptic = new();
        private List<HanhTrinhTauDTO> _dsHanhTrinh = new();

        public DieuHanhThucTePage()
        {
            InitializeComponent();

            try
            {
                _ngayHienTai = _dieuDoService.LayNgayBieuDoMacDinh();
                if (_ngayHienTai < new DateTime(1753, 1, 1))
                {
                    _ngayHienTai = ThoiGianHeThongHelper.LayGioHienTai().Date;
                }
            }
            catch
            {
                _ngayHienTai = ThoiGianHeThongHelper.LayGioHienTai().Date;
            }

            _simulationTimer.Interval = TimeSpan.FromSeconds(1);
            _simulationTimer.Tick += SimulationTimer_Tick;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _ngayHienTai = _dieuDoService.LayNgayBieuDoMacDinh();
                if (_ngayHienTai == DateTime.MinValue) _ngayHienTai = DateTime.Today;

                if (!_isLoaded)
                {
                    _isLoaded = true;
                    // Khởi động giờ mô phỏng mặc định là 07:15 sáng ngày được chọn
                    if (!ThoiGianHeThongHelper.CheDoMoPhong)
                    {
                        ThoiGianHeThongHelper.BatMoPhong(new DateTime(_ngayHienTai.Year, _ngayHienTai.Month, _ngayHienTai.Day, 7, 15, 0));
                    }
                    KhoiTaoDuLieuGaSynoptic();
                }

                // Luôn đảm bảo Timer hoạt động khi người dùng quay lại trang
                if (!_simulationTimer.IsEnabled)
                {
                    _simulationTimer.Start();
                }

                CapNhatHienThiNutTocDo();
                CapNhatToanBoHeThong();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể khởi tạo bàn điều độ: {ex.Message}", "Lỗi Khởi Tạo");
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _simulationTimer.Stop();
        }

        private void SimulationTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_tocDoNhan > 0 && ThoiGianHeThongHelper.CheDoMoPhong)
                {
                    DateTime gioHienTai = ThoiGianHeThongHelper.LayGioHienTai();
                    DateTime gioMoi = gioHienTai.AddSeconds(_tocDoNhan);
                    ThoiGianHeThongHelper.GioMoPhong = gioMoi;
                }

                CapNhatDongHoLCD();
                VeSoDoSynoptic();
                CapNhatBangBanVe();
            }
            catch
            {
                // Phòng vệ để timer luôn tiếp tục chạy, không bị gián đoạn
            }
        }

        private void CapNhatDongHoLCD()
        {
            DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
            txtGioMoPhong.Text = now.ToString("HH:mm:ss");
            txtNgayMoPhong.Text = now.ToString("dd/MM/yyyy");

            // Chớp tắt đèn LED chỉ báo
            ledStatus.Fill = _tocDoNhan > 0 ? (ledStatus.Fill == Brushes.LimeGreen ? (Brush)new BrushConverter().ConvertFrom("#059669")! : Brushes.LimeGreen) : (Brush)new BrushConverter().ConvertFrom("#F59E0B")!;
        }

        private void KhoiTaoDuLieuGaSynoptic()
        {
            _dsGaSynoptic.Clear();
            var dtGa = _dieuDoService.LayDanhSachGaTuyen();

            // Lọc các ga tiêu biểu trên trục chính Hà Nội - Vinh cho sơ đồ Synoptic
            string[] gaUuTien = { "HNO", "GBA", "PLY", "NDI", "NBH", "BSO", "THO", "VIN" };

            var dsGaRaw = new List<(int MaGa, string TenGa, string Code, decimal Km)>();
            if (dtGa.Rows.Count > 0)
            {
                foreach (DataRow row in dtGa.Rows)
                {
                    string code = row["MaGaCode"]?.ToString() ?? "";
                    decimal km = row["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(row["LyTrinhKm"]) : 0;
                    if (km > 350) continue;

                    if (gaUuTien.Contains(code) || km == 0)
                    {
                        dsGaRaw.Add((Convert.ToInt32(row["MaGa"]), row["TenGa"]?.ToString() ?? "", code, km));
                    }
                }
            }

            // Phòng vệ nếu DB trống
            if (dsGaRaw.Count == 0)
            {
                dsGaRaw.Add((1, "Hà Nội", "HNO", 0));
                dsGaRaw.Add((2, "Giáp Bát", "GBA", 5.2m));
                dsGaRaw.Add((3, "Phủ Lý", "PLY", 56));
                dsGaRaw.Add((4, "Nam Định", "NDI", 87));
                dsGaRaw.Add((5, "Ninh Bình", "NBH", 115));
                dsGaRaw.Add((6, "Bỉm Sơn", "BSO", 141));
                dsGaRaw.Add((7, "Thanh Hóa", "THO", 175));
                dsGaRaw.Add((8, "Vinh", "VIN", 319));
            }

            // Sắp xếp tăng dần theo lý trình Km
            dsGaRaw = dsGaRaw.OrderBy(g => g.Km).ToList();

            // Thuật toán phân bổ cự ly sơ họa kỹ thuật (Schematic Alignment):
            // Đảm bảo cự ly giữa 2 ga liên tiếp luôn >= 200px, chấm dứt hoàn toàn lỗi đè chữ "HÀ N GIÁP BÁT"
            double currentX = 120;
            for (int i = 0; i < dsGaRaw.Count; i++)
            {
                var g = dsGaRaw[i];
                if (i > 0)
                {
                    double deltaKm = (double)(g.Km - dsGaRaw[i - 1].Km);
                    double gap = Math.Max(200, Math.Min(450, deltaKm * 5.5));
                    currentX += gap;
                }

                _dsGaSynoptic.Add(new GaSynoptic
                {
                    MaGa = g.MaGa,
                    TenGa = g.TenGa,
                    MaGaCode = g.Code,
                    LyTrinhKm = g.Km,
                    X = currentX
                });
            }
        }

        public void CapNhatToanBoHeThong()
        {
            if (_ngayHienTai < new DateTime(1753, 1, 1))
            {
                try
                {
                    _ngayHienTai = _dieuDoService.LayNgayBieuDoMacDinh();
                }
                catch
                {
                    _ngayHienTai = ThoiGianHeThongHelper.LayGioHienTai().Date;
                }

                if (_ngayHienTai < new DateTime(1753, 1, 1))
                {
                    _ngayHienTai = ThoiGianHeThongHelper.LayGioHienTai().Date;
                }
            }

            if (_dsGaSynoptic.Count == 0)
            {
                KhoiTaoDuLieuGaSynoptic();
            }

            _dsHanhTrinh = _dieuDoService.LayDuLieuBieuDo(_ngayHienTai);
            CapNhatDongHoLCD();
            VeSoDoSynoptic();
            CapNhatBangDieuDo();
            CapNhatBangBanVe();
        }

        #region VẼ SƠ ĐỒ SYNOPTIC (CANVAS)
        private void VeSoDoSynoptic()
        {
            cvsSynoptic.Children.Clear();
            if (_dsGaSynoptic.Count < 2) return;

            DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
            double yMainTrack = 180;
            double ySidingTrack = 130;

            // 1. Tính toán vị trí các đoàn tàu thời gian thực
            var dsTauThucTe = new List<(HanhTrinhTauDTO Tau, decimal Km, double V, bool DangDungGa, string TenGaDung, double X)>();
            int tauDangChayCount = 0;

            foreach (var tau in _dsHanhTrinh)
            {
                if (tau.DanhSachDiem.Count < 2) continue;
                var diemDau = tau.DanhSachDiem.First();
                var diemCuoi = tau.DanhSachDiem.Last();

                if (now < diemDau.GioDi)
                {
                    // Tàu chưa chạy, nằm ở ga xuất phát
                    dsTauThucTe.Add((tau, diemDau.LyTrinhKm, 0, true, diemDau.TenGa, LayXTheoKm(diemDau.LyTrinhKm)));
                }
                else if (now >= diemCuoi.GioDen)
                {
                    // Tàu đã về đích
                    dsTauThucTe.Add((tau, diemCuoi.LyTrinhKm, 0, true, diemCuoi.TenGa, LayXTheoKm(diemCuoi.LyTrinhKm)));
                }
                else
                {
                    // Tàu đang trong hành trình
                    tauDangChayCount++;
                    bool dangDung = false;
                    string gaDung = "";
                    decimal kmHienTai = diemDau.LyTrinhKm;
                    double vanToc = 70;

                    for (int i = 0; i < tau.DanhSachDiem.Count; i++)
                    {
                        var diem = tau.DanhSachDiem[i];
                        if (now >= diem.GioDen && now <= diem.GioDi)
                        {
                            dangDung = true;
                            gaDung = diem.TenGa;
                            kmHienTai = diem.LyTrinhKm;
                            vanToc = 0;
                            break;
                        }

                        if (i < tau.DanhSachDiem.Count - 1)
                        {
                            var diemTiep = tau.DanhSachDiem[i + 1];
                            if (now > diem.GioDi && now < diemTiep.GioDen)
                            {
                                double tongThoiGian = (diemTiep.GioDen - diem.GioDi).TotalMinutes;
                                double daChay = (now - diem.GioDi).TotalMinutes;
                                double phanTram = tongThoiGian > 0 ? Math.Clamp(daChay / tongThoiGian, 0.0, 1.0) : 0;
                                kmHienTai = diem.LyTrinhKm + (decimal)phanTram * (diemTiep.LyTrinhKm - diem.LyTrinhKm);
                                vanToc = Math.Abs((double)(diemTiep.LyTrinhKm - diem.LyTrinhKm)) / (tongThoiGian / 60.0);
                                break;
                            }
                        }
                    }

                    dsTauThucTe.Add((tau, kmHienTai, vanToc, dangDung, gaDung, LayXTheoKm(kmHienTai)));
                }
            }

            txtSoTauDangChay.Text = $"Đang chạy: {tauDangChayCount} đoàn tàu";

            // 2. Vẽ các đoạn khu gian đường sắt & Kiểm tra chiếm dụng ray (Track Circuit)
            for (int i = 0; i < _dsGaSynoptic.Count - 1; i++)
            {
                var gaA = _dsGaSynoptic[i];
                var gaB = _dsGaSynoptic[i + 1];

                decimal kmMin = Math.Min(gaA.LyTrinhKm, gaB.LyTrinhKm);
                decimal kmMax = Math.Max(gaA.LyTrinhKm, gaB.LyTrinhKm);

                // Kiểm tra có tàu nào đang ở giữa khu gian A-B không
                var tauTrongKhuGian = dsTauThucTe.Where(t => !t.DangDungGa && t.Km > kmMin && t.Km < kmMax).ToList();
                bool isOccupied = tauTrongKhuGian.Count > 0;

                // Tuyến ray khu gian (Ray xám chì kỹ thuật #475569; Chiếm dụng chuyển sang đỏ cảnh báo #DC2626)
                var lineTrack = new Line
                {
                    X1 = gaA.X + 28,
                    Y1 = yMainTrack,
                    X2 = gaB.X - 28,
                    Y2 = yMainTrack,
                    Stroke = isOccupied ? (Brush)new BrushConverter().ConvertFrom("#DC2626")! : (Brush)new BrushConverter().ConvertFrom("#475569")!,
                    StrokeThickness = isOccupied ? 4.5 : 3.0,
                    StrokeDashArray = isOccupied ? null : new DoubleCollection { 8, 2 }
                };
                cvsSynoptic.Children.Add(lineTrack);

                // Đèn cảm biến chiếm dụng mạch ray ở giữa khu gian
                double midX = (gaA.X + gaB.X) / 2;
                var elpTrackSensor = new Ellipse
                {
                    Width = 9,
                    Height = 9,
                    Fill = isOccupied ? (Brush)new BrushConverter().ConvertFrom("#DC2626")! : (Brush)new BrushConverter().ConvertFrom("#16A34A")!,
                    Stroke = Brushes.White,
                    StrokeThickness = 1.2
                };
                Canvas.SetLeft(elpTrackSensor, midX - 4.5);
                Canvas.SetTop(elpTrackSensor, yMainTrack - 4.5);
                cvsSynoptic.Children.Add(elpTrackSensor);

                // Nhãn lý trình cự ly khu gian
                var txtKhuGianKm = new TextBlock
                {
                    Text = $"{Math.Abs(gaB.LyTrinhKm - gaA.LyTrinhKm):0.#} km",
                    FontSize = 9.5,
                    Foreground = isOccupied ? (Brush)new BrushConverter().ConvertFrom("#DC2626")! : (Brush)new BrushConverter().ConvertFrom("#64748B")!,
                    FontFamily = new FontFamily("Consolas")
                };
                Canvas.SetLeft(txtKhuGianKm, midX - 18);
                Canvas.SetTop(txtKhuGianKm, yMainTrack + 12);
                cvsSynoptic.Children.Add(txtKhuGianKm);
            }

            // 3. Vẽ cấu trúc từng Ga (Đường chính + Đường tránh + Đèn tín hiệu + Tên ga)
            foreach (var ga in _dsGaSynoptic)
            {
                // Kiểm tra có tàu nào đang đỗ tại ga này không
                var tauDo = dsTauThucTe.FirstOrDefault(t => t.DangDungGa && Math.Abs(t.Km - ga.LyTrinhKm) < 1.0m);
                bool coTauDo = tauDo.Tau != null;

                // Đường chính tại Ga (Main Track)
                var mainLine = new Line
                {
                    X1 = ga.X - 28,
                    Y1 = yMainTrack,
                    X2 = ga.X + 28,
                    Y2 = yMainTrack,
                    Stroke = coTauDo ? (Brush)new BrushConverter().ConvertFrom("#D97706")! : (Brush)new BrushConverter().ConvertFrom("#003B73")!,
                    StrokeThickness = 4.5
                };
                cvsSynoptic.Children.Add(mainLine);

                // Đường tránh tàu (Siding Loop Track)
                var sidingPath = new Path
                {
                    Stroke = (Brush)new BrushConverter().ConvertFrom("#94A3B8")!,
                    StrokeThickness = 2,
                    Data = Geometry.Parse(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"M {ga.X - 32},{yMainTrack} C {ga.X - 16},{ySidingTrack} {ga.X + 16},{ySidingTrack} {ga.X + 32},{yMainTrack}"))
                };
                cvsSynoptic.Children.Add(sidingPath);

                // Đèn tín hiệu vào ga & ra ga (Home & Starter Signals)
                ThemDenTinHieu(ga.X - 30, yMainTrack - 16, coTauDo ? "#DC2626" : "#16A34A");
                ThemDenTinHieu(ga.X + 24, yMainTrack - 16, coTauDo ? "#D97706" : "#16A34A");

                // Thẻ Nhà Ga (Nền trắng, viền thanh lịch, chữ xanh navy chuẩn ĐSVN)
                var bdGa = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFrom(coTauDo ? "#EFF6FF" : "#FFFFFF")!,
                    BorderBrush = (Brush)new BrushConverter().ConvertFrom(coTauDo ? "#0284C7" : "#CBD5E1")!,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(8, 3, 8, 3),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 4,
                        ShadowDepth = 1,
                        Opacity = 0.08,
                        Color = Colors.Black
                    }
                };
                var spGa = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                spGa.Children.Add(new TextBlock
                {
                    Text = ga.TenGa.ToUpper(),
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#003B73")!,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                spGa.Children.Add(new TextBlock
                {
                    Text = $"Km {ga.LyTrinhKm:0.#}",
                    FontSize = 9,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#64748B")!,
                    FontFamily = new FontFamily("Consolas"),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                bdGa.Child = spGa;

                Canvas.SetLeft(bdGa, ga.X - 38);
                Canvas.SetTop(bdGa, yMainTrack + 26);
                cvsSynoptic.Children.Add(bdGa);
            }

            // 4. Vẽ các Đoàn Tàu Di Chuyển (Train Engines)
            foreach (var item in dsTauThucTe)
            {
                // Chỉ vẽ các đoàn tàu trong phạm vi đoạn tuyến này (Km 0 đến 350)
                if (item.Km < -5 || item.Km > 330) continue;

                double trainX = item.X;
                double trainY = item.DangDungGa ? ySidingTrack : yMainTrack;

                bool isBacNam = item.Tau.HuongChay == "BAC_NAM";
                string colorHex = isBacNam ? "#003B73" : "#0284C7";

                var bdTrain = new Border
                {
                    Background = (Brush)new BrushConverter().ConvertFrom(colorHex)!,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(7, 3, 7, 3),
                    Cursor = Cursors.Hand,
                    Tag = item.Tau,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 6,
                        ShadowDepth = 2,
                        Opacity = 0.2,
                        Color = Colors.Black
                    }
                };

                var spTrain = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                string arrow = isBacNam ? "➔" : "⬅";
                spTrain.Children.Add(new TextBlock
                {
                    Text = $"🚅 {item.Tau.SoHieuMacTau} {arrow}",
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 4, 0)
                });
                spTrain.Children.Add(new TextBlock
                {
                    Text = item.DangDungGa ? "[ĐỖ GA]" : $"{item.V:0} km/h",
                    FontSize = 9.5,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#FEF08A")!
                });
                bdTrain.Child = spTrain;

                Canvas.SetLeft(bdTrain, trainX - 42);
                Canvas.SetTop(bdTrain, trainY - 26);
                cvsSynoptic.Children.Add(bdTrain);
            }
        }

        private void ThemDenTinHieu(double x, double y, string colorHex)
        {
            var elp = new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = (Brush)new BrushConverter().ConvertFrom(colorHex)!,
                Stroke = (Brush)new BrushConverter().ConvertFrom("#475569")!,
                StrokeThickness = 0.8
            };
            Canvas.SetLeft(elp, x);
            Canvas.SetTop(elp, y);
            cvsSynoptic.Children.Add(elp);
        }

        private double LayXTheoKm(decimal km)
        {
            if (_dsGaSynoptic.Count == 0) return 100;
            if (km <= _dsGaSynoptic.First().LyTrinhKm) return _dsGaSynoptic.First().X;
            if (km >= _dsGaSynoptic.Last().LyTrinhKm) return _dsGaSynoptic.Last().X;

            for (int i = 0; i < _dsGaSynoptic.Count - 1; i++)
            {
                var g1 = _dsGaSynoptic[i];
                var g2 = _dsGaSynoptic[i + 1];
                if (km >= g1.LyTrinhKm && km <= g2.LyTrinhKm)
                {
                    double rangeKm = (double)(g2.LyTrinhKm - g1.LyTrinhKm);
                    double ratio = rangeKm > 0 ? (double)(km - g1.LyTrinhKm) / rangeKm : 0;
                    return g1.X + ratio * (g2.X - g1.X);
                }
            }
            return 100;
        }

        private void CvsSynoptic_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(cvsSynoptic);
            DateTime now = ThoiGianHeThongHelper.LayGioHienTai();

            // Tìm xem chuột có đang trỏ gần đoàn tàu nào không
            foreach (var tau in _dsHanhTrinh)
            {
                if (tau.DanhSachDiem.Count < 2) continue;
                decimal km = LayKmCuaTau(tau, now);
                double x = LayXTheoKm(km);

                if (Math.Abs(p.X - x) < 40 && Math.Abs(p.Y - 170) < 50)
                {
                    txtTooltipTieuDe.Text = $"ĐOÀN TÀU {tau.SoHieuMacTau} ({tau.TenGaDi} ➔ {tau.TenGaDen})";
                    txtTooltipChiTiet.Text = $"Lý trình: Km {km:0.#} | Trạng thái: {tau.TrangThai}";
                    txtTooltipTrangThai.Text = $"Loại tàu: {tau.LoaiTau} | Mức ưu tiên: #{tau.MucUuTien}";

                    Canvas.SetLeft(bdTrainTooltip, Math.Clamp(p.X + 15, 10, cvsSynoptic.ActualWidth - 250));
                    Canvas.SetTop(bdTrainTooltip, Math.Clamp(p.Y + 15, 10, cvsSynoptic.ActualHeight - 90));
                    bdTrainTooltip.Visibility = Visibility.Visible;
                    return;
                }
            }

            bdTrainTooltip.Visibility = Visibility.Collapsed;
        }

        private void CvsSynoptic_MouseLeave(object sender, MouseEventArgs e)
        {
            bdTrainTooltip.Visibility = Visibility.Collapsed;
        }

        private decimal LayKmCuaTau(HanhTrinhTauDTO tau, DateTime now)
        {
            if (tau.DanhSachDiem.Count == 0) return 0;
            var diemDau = tau.DanhSachDiem.First();
            var diemCuoi = tau.DanhSachDiem.Last();

            if (now <= diemDau.GioDi) return diemDau.LyTrinhKm;
            if (now >= diemCuoi.GioDen) return diemCuoi.LyTrinhKm;

            for (int i = 0; i < tau.DanhSachDiem.Count - 1; i++)
            {
                var d1 = tau.DanhSachDiem[i];
                var d2 = tau.DanhSachDiem[i + 1];
                if (now >= d1.GioDi && now <= d2.GioDen)
                {
                    double total = (d2.GioDen - d1.GioDi).TotalMinutes;
                    double curr = (now - d1.GioDi).TotalMinutes;
                    double r = total > 0 ? curr / total : 0;
                    return d1.LyTrinhKm + (decimal)r * (d2.LyTrinhKm - d1.LyTrinhKm);
                }
                else if (now >= d1.GioDen && now <= d1.GioDi)
                {
                    return d1.LyTrinhKm;
                }
            }
            return diemDau.LyTrinhKm;
        }
        #endregion

        #region BẢNG ĐIỀU ĐỘ & BÁN VÉ
        private void CapNhatBangDieuDo()
        {
            var dsXungDot = _dieuDoService.KiemTraXungDotToanTuyen(_ngayHienTai);
            if (dsXungDot.Count == 0)
            {
                txtBadgeXungDot.Text = "0 XUNG ĐỘT";
                txtBadgeXungDot.Foreground = (Brush)new BrushConverter().ConvertFrom("#15803D")!;
                bdBadgeXungDot.Background = (Brush)new BrushConverter().ConvertFrom("#F1F5F9")!;
                bdBadgeXungDot.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#CBD5E1")!;
                bdBadgeXungDot.BorderThickness = new Thickness(1);
                bdAnToanNotice.Visibility = Visibility.Visible;
                bdCanhBaoNotice.Visibility = Visibility.Collapsed;
                txtTrangThaiCanhBaoNhanh.Text = "Tuyến an toàn (0 Xung đột)";
                txtTrangThaiCanhBaoNhanh.Foreground = (Brush)new BrushConverter().ConvertFrom("#15803D")!;
            }
            else
            {
                txtBadgeXungDot.Text = $"{dsXungDot.Count} XUNG ĐỘT";
                txtBadgeXungDot.Foreground = Brushes.White;
                bdBadgeXungDot.Background = (Brush)new BrushConverter().ConvertFrom("#B91C1C")!;
                bdBadgeXungDot.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#991B1B")!;
                bdBadgeXungDot.BorderThickness = new Thickness(0);
                bdAnToanNotice.Visibility = Visibility.Collapsed;
                bdCanhBaoNotice.Visibility = Visibility.Visible;

                var xd = dsXungDot.First();
                txtTieuDeCanhBao.Text = $"CẢNH BÁO XUNG ĐỘT ĐỐI ĐẦU: {xd.SoHieuMacTau1} - {xd.SoHieuMacTau2}";
                txtChiTietCanhBao.Text = $"Dự báo xung đột tại khu gian {xd.TenGaDau} - {xd.TenGaCuoi} (Km {xd.LyTrinhUocTinhKm:0.#}) lúc {xd.ThoiDiemXungDot:HH:mm}.";
                txtDeXuatDieuDo.Text = $"Cho tàu {xd.SoHieuTauBiTranh} dừng tránh tại Ga {xd.TenGaDeXuatTranh} (Đường #{xd.SoHieuDuongRayDeXuat ?? 2}) trong {xd.SoPhutDungDeXuat} phút.";

                txtTrangThaiCanhBaoNhanh.Text = $"Cảnh báo: {dsXungDot.Count} xung đột phát sinh";
                txtTrangThaiCanhBaoNhanh.Foreground = (Brush)new BrushConverter().ConvertFrom("#DC2626")!;
            }
        }

        private void CapNhatBangBanVe()
        {
            DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
            var dsChang = new List<ChangVeHienThi>();

            var tauSE1 = _dsHanhTrinh.FirstOrDefault(t => t.SoHieuMacTau.Equals("SE1", StringComparison.OrdinalIgnoreCase)) ?? _dsHanhTrinh.FirstOrDefault();

            if (tauSE1 != null && tauSE1.DanhSachDiem.Count >= 2)
            {
                int soPhutTre = _dieuDoService.LaySoPhutTreLuyKe(tauSE1.MaChuyenTau);

                // 1. Tạo các chặng vé liên thông thực tế theo đúng thứ tự các điểm dừng của tàu
                for (int i = 0; i < tauSE1.DanhSachDiem.Count - 1; i++)
                {
                    var gDi = tauSE1.DanhSachDiem[i];
                    var gDen = tauSE1.DanhSachDiem[i + 1];

                    // Chốt chặn an toàn: Tuyệt đối không sinh chặng nếu ga đi trùng ga đến
                    if (gDi.MaGa == gDen.MaGa) continue;

                    DateTime gioDiKH = gDi.GioDi;
                    DateTime gioDiTT = i == 0 ? gioDiKH : gioDiKH.AddMinutes(soPhutTre);

                    ThemDongChang(dsChang, gDi.MaGa, gDen.MaGa, $"{gDi.TenGa} ➔ {gDen.TenGa}", gioDiKH, gioDiTT, now, soPhutTre);
                }

                // 2. Bổ sung các chặng dài chiến lược tiêu biểu (nếu có các ga này trong hành trình)
                var gDau = tauSE1.DanhSachDiem.First();
                var gCuoi = tauSE1.DanhSachDiem.Last();

                var gVinh = tauSE1.DanhSachDiem.FirstOrDefault(d => d.MaGaCode == "VIN");
                if (gVinh != null && gDau.MaGa != gVinh.MaGa)
                {
                    DateTime gioDiTT = gDau.GioDi;
                    ThemDongChang(dsChang, gDau.MaGa, gVinh.MaGa, $"{gDau.TenGa} ➔ {gVinh.TenGa}", gDau.GioDi, gioDiTT, now, 0);
                }

                if (gDau.MaGa != gCuoi.MaGa)
                {
                    ThemDongChang(dsChang, gDau.MaGa, gCuoi.MaGa, $"{gDau.TenGa} ➔ {gCuoi.TenGa}", gDau.GioDi, gDau.GioDi, now, 0);
                }
            }

            dgChangVeBan.ItemsSource = dsChang;
        }

        private void ThemDongChang(List<ChangVeHienThi> list, int gaDi, int gaDen, string tenChang, DateTime gioKH, DateTime gioTT, DateTime now, int soPhutTre = 0)
        {
            // Chốt chặn tuyệt đối: Không sinh chặng nếu ga đi trùng ga đến hoặc chặng đã tồn tại trong danh sách
            if (gaDi <= 0 || gaDen <= 0 || gaDi == gaDen) return;
            if (list.Any(c => c.MaGaDi == gaDi && c.MaGaDen == gaDen)) return;

            string trangThaiText;
            Brush bg;
            Brush fg;

            if (now >= gioTT)
            {
                trangThaiText = "ĐÃ ĐÓNG (Tàu đã rời ga)";
                bg = (Brush)new BrushConverter().ConvertFrom("#F1F5F9")!;
                fg = (Brush)new BrushConverter().ConvertFrom("#475569")!;
            }
            else if (soPhutTre > 0)
            {
                trangThaiText = $"GIA HẠN +{soPhutTre}P (MỞ BÁN)";
                bg = (Brush)new BrushConverter().ConvertFrom("#EFF6FF")!;
                fg = (Brush)new BrushConverter().ConvertFrom("#0369A1")!;
            }
            else if ((gioTT - now).TotalMinutes <= 20)
            {
                trangThaiText = "SẮP ĐÓNG (< 20 phút)";
                bg = (Brush)new BrushConverter().ConvertFrom("#FFFBEB")!;
                fg = (Brush)new BrushConverter().ConvertFrom("#B45309")!;
            }
            else
            {
                trangThaiText = "MỞ BÁN BÌNH THƯỜNG";
                bg = (Brush)new BrushConverter().ConvertFrom("#F0FDF4")!;
                fg = (Brush)new BrushConverter().ConvertFrom("#15803D")!;
            }

            list.Add(new ChangVeHienThi
            {
                MaGaDi = gaDi,
                MaGaDen = gaDen,
                TenChang = tenChang,
                GioDiKH = gioKH.ToString("HH:mm"),
                GioDiThucTe = gioTT.ToString("HH:mm"),
                MoTaTrangThai = trangThaiText,
                MauNenTrangThai = bg,
                MauChuTrangThai = fg
            });
        }
        #endregion

        #region KỊCH BẢN DEMO 1-CLICK & TỐC ĐỘ
        private void CapNhatHienThiNutTocDo()
        {
            var btnNormal = (Style)FindResource("DesktopButton");
            var btnActive = (Style)FindResource("PrimaryDesktopButton");

            btnDung.Style = _tocDoNhan == 0 ? btnActive : btnNormal;
            btnX1.Style = _tocDoNhan == 1 ? btnActive : btnNormal;
            btnX30.Style = _tocDoNhan == 30 ? btnActive : btnNormal;
            btnX60.Style = _tocDoNhan == 60 ? btnActive : btnNormal;
            btnX300.Style = _tocDoNhan == 300 ? btnActive : btnNormal;
        }

        private void DamBaoDongHoChayTiep()
        {
            if (_tocDoNhan <= 0)
            {
                _tocDoNhan = 60;
                txtTrangThaiTocDo.Text = "Tốc độ: 60x (Nhanh)";
            }
            if (!_simulationTimer.IsEnabled)
            {
                _simulationTimer.Start();
            }
            CapNhatHienThiNutTocDo();
        }

        private void BtnKichBan1_Click(object sender, RoutedEventArgs e)
        {
            if (_dieuDoService.KichHoatKichBanDemo(1, _ngayHienTai, out string tb))
            {
                DamBaoDongHoChayTiep();
                CapNhatToanBoHeThong();
                ThongBaoDialog.ThanhCong(tb, "Kịch Bản 1: Chuẩn Bị Xuất Phát");
            }
        }

        private void BtnKichBan2_Click(object sender, RoutedEventArgs e)
        {
            if (_dieuDoService.KichHoatKichBanDemo(2, _ngayHienTai, out string tb))
            {
                DamBaoDongHoChayTiep();
                CapNhatToanBoHeThong();
                ThongBaoDialog.ThanhCong(tb, "Kịch Bản 2: Đang Chạy Khu Gian");
            }
        }

        private void BtnKichBan3_Click(object sender, RoutedEventArgs e)
        {
            if (_dieuDoService.KichHoatKichBanDemo(3, _ngayHienTai, out string tb))
            {
                DamBaoDongHoChayTiep();
                CapNhatToanBoHeThong();
                ThongBaoDialog.ThanhCong(tb, "Kịch Bản 3: Đỗ Ga Nam Định");
            }
        }

        private void BtnKichBan4_Click(object sender, RoutedEventArgs e)
        {
            if (_dieuDoService.KichHoatKichBanDemo(4, _ngayHienTai, out string tb))
            {
                DamBaoDongHoChayTiep();
                CapNhatToanBoHeThong();
                ThongBaoDialog.CanhBao(tb, "Kịch Bản 4: Kích Hoạt Tàu Trễ +45P");
            }
        }

        private void BtnKichBan5_Click(object sender, RoutedEventArgs e)
        {
            if (_dieuDoService.KichHoatKichBanDemo(5, _ngayHienTai, out string tb))
            {
                DamBaoDongHoChayTiep();
                CapNhatToanBoHeThong();
                ThongBaoDialog.ThanhCong(tb, "Kịch Bản 5: Khôi Phục Gốc");
            }
        }

        private void BtnDung_Click(object sender, RoutedEventArgs e)
        {
            _tocDoNhan = 0;
            txtTrangThaiTocDo.Text = "Tốc độ: Tạm dừng";
            CapNhatHienThiNutTocDo();
        }

        private void BtnX1_Click(object sender, RoutedEventArgs e)
        {
            _tocDoNhan = 1;
            txtTrangThaiTocDo.Text = "Tốc độ: 1x (Bình thường)";
            if (!_simulationTimer.IsEnabled) _simulationTimer.Start();
            CapNhatHienThiNutTocDo();
        }

        private void BtnX30_Click(object sender, RoutedEventArgs e)
        {
            _tocDoNhan = 30;
            txtTrangThaiTocDo.Text = "Tốc độ: 30x (Nhanh)";
            if (!_simulationTimer.IsEnabled) _simulationTimer.Start();
            CapNhatHienThiNutTocDo();
        }

        private void BtnX60_Click(object sender, RoutedEventArgs e)
        {
            _tocDoNhan = 60;
            txtTrangThaiTocDo.Text = "Tốc độ: 60x (Nhanh)";
            if (!_simulationTimer.IsEnabled) _simulationTimer.Start();
            CapNhatHienThiNutTocDo();
        }

        private void BtnX300_Click(object sender, RoutedEventArgs e)
        {
            _tocDoNhan = 300;
            txtTrangThaiTocDo.Text = "Tốc độ: 300x (Siêu tốc)";
            if (!_simulationTimer.IsEnabled) _simulationTimer.Start();
            CapNhatHienThiNutTocDo();
        }

        private void BtnCong15P_Click(object sender, RoutedEventArgs e) => NhayThoiGian(15);
        private void BtnCong30P_Click(object sender, RoutedEventArgs e) => NhayThoiGian(30);
        private void BtnCong1H_Click(object sender, RoutedEventArgs e) => NhayThoiGian(60);

        private void NhayThoiGian(int soPhut)
        {
            if (!ThoiGianHeThongHelper.CheDoMoPhong)
            {
                ThoiGianHeThongHelper.BatMoPhong(DateTime.Now);
            }
            ThoiGianHeThongHelper.GioMoPhong = ThoiGianHeThongHelper.GioMoPhong.AddMinutes(soPhut);
            DamBaoDongHoChayTiep();
            CapNhatToanBoHeThong();
        }

        private void BtnVeGioThat_Click(object sender, RoutedEventArgs e)
        {
            ThoiGianHeThongHelper.TatMoPhong();
            _tocDoNhan = 1;
            txtTrangThaiTocDo.Text = "Đang lấy giờ máy tính thật";
            CapNhatHienThiNutTocDo();
            CapNhatToanBoHeThong();
            ThongBaoDialog.ThongTin("Đã tắt chế độ mô phỏng và đồng bộ về giờ máy tính thật.", "Đồng Hồ Hệ Thống");
        }
        #endregion

        #region HÀNH ĐỘNG ĐIỀU ĐỘ & CHUYỂN BÁN VÉ
        private void BtnTuDongXepTranh_Click(object sender, RoutedEventArgs e)
        {
            if (_dieuDoService.TuDongGiaiQuyetTranhTau(_ngayHienTai, out string baoCao))
            {
                CapNhatToanBoHeThong();
                ThongBaoDialog.ThanhCong(baoCao, "Kết Quả Xếp Tránh Tàu");
            }
            else
            {
                ThongBaoDialog.CanhBao(baoCao, "Thông Báo Điều Độ");
            }
        }

        private void BtnThemSuCo_Click(object sender, RoutedEventArgs e)
        {
            var tauSE1 = _dsHanhTrinh.FirstOrDefault(t => t.SoHieuMacTau.Equals("SE1", StringComparison.OrdinalIgnoreCase)) ?? _dsHanhTrinh.FirstOrDefault();
            if (tauSE1 == null)
            {
                ThongBaoDialog.CanhBao("Không tìm thấy chuyến tàu khả dụng trong ngày.", "Thông Báo");
                return;
            }

            int maGa = tauSE1.DanhSachDiem.Count > 1 ? tauSE1.DanhSachDiem[1].MaGa : 3;
            if (_dieuDoService.GhiNhanSuCoChamGio(tauSE1.MaChuyenTau, maGa, 30, "SU_CO_TOA_XE", "[DEMO] Sự cố kỹ thuật toa xe, trễ 30 phút"))
            {
                CapNhatToanBoHeThong();
                ThongBaoDialog.ThanhCong($"Đã ghi nhận sự cố chậm giờ +30 phút cho tàu {tauSE1.SoHieuMacTau}!", "Ghi Nhận Thành Công");
            }
        }

        private void BtnChuyenSangBanVe_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem dòng nào đang được chọn, nếu không thì lấy chặng Hà Nội -> Vinh
            var changChon = dgChangVeBan.SelectedItem as ChangVeHienThi;
            int gaDi = changChon?.MaGaDi ?? 1; // Hà Nội
            int gaDen = changChon?.MaGaDen ?? 8; // Vinh

            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                mainWin.NavigateToBanVe(gaDi, gaDen, _ngayHienTai);
            }
        }

        private void BtnMoBieuDo24H_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWin)
            {
                mainWin.NavigateTo("BieuDo");
            }
        }
        #endregion
    }
}
