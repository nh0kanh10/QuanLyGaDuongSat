using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BUS.Services;
using GUI.Views.Dialogs;
using GUI.Views.KyThuat.Helpers;
using GUI.Views.KyThuat.Models;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.KyThuat.Dialogs
{
    // =========================================================================
    // MO PHONG LAP TAU KEO - THA (SRS Nhom 3)
    //
    // - Keo toa tu bai vao doan tau, keo trong doan de doi thu tu moc noi,
    //   keo nguoc xuong bai (hoac nhap dup / bam X) de thao toa.
    // - Sau moi thay doi, chay lai bang kiem tra an toan: dau may, so toa,
    //   chieu dai vs duong tranh, trong luong vs suc keo, tai trong truc,
    //   thu tu xep toa hang.
    //
    // Giai doan dung giao dien: ket qua chi tra ve cho trang goi hien thi,
    // khong ghi vanhanh.DoanTau / vanhanh.ChiTietDoanTau.
    // =========================================================================
    public partial class LapTauKeoThaDialog : Window
    {
        private const string DinhDangKeo = "GUI.KyThuat.ToaLapTau";

        private readonly ThongTinLapTau _dauVao;
        private readonly ObservableCollection<ToaLapTau> _doanTau = new();
        private readonly ObservableCollection<ToaLapTau> _baiToa = new();
        private readonly ICollectionView? _boLocBai;
        private readonly DauMayLapTau? _dauMayBanDau;
        private readonly bool _dangKhoiTao;
        private bool _coThayDoi;

        // --- Trang thai keo - tha ---
        private Point _diemNhan;                 // toa do chuot luc nhan, theo bdGoc
        private ToaLapTau? _toaDuocNhan;
        private FrameworkElement? _theDuocNhan;
        private KeoThaAdorner? _bongKeo;
        private bool _dangKeo;

        public KetQuaLapTau? KetQua { get; private set; }

        public LapTauKeoThaDialog(ThongTinLapTau dauVao)
        {
            _dangKhoiTao = true;
            InitializeComponent();
            _dauVao = dauVao;

            // Thu nho cho vua man hinh laptop 1366x768
            Rect vung = SystemParameters.WorkArea;
            Width = Math.Min(Width, vung.Width - 8);
            Height = Math.Min(Height, vung.Height - 8);

            txtTieuDePhu.Text = dauVao.TenChuyen;
            if (dauVao.DuongTranhNganNhatM is > 0)
            {
                txtDuongTranh.Text = $"{dauVao.DuongTranhNganNhatM:N0} m";
            }
            else
            {
                txtDuongTranh.Text = "chưa có dữ liệu";
                bdDuongTranh.ToolTip = "Hành trình chưa có dữ liệu đường tránh tại các ga dừng. " +
                    $"Chiều dài đoàn tàu được đối chiếu với giới hạn {DanhMucMauPhuongTien.ChieuDaiDoanTauToiDaM:N0} m của hệ thống.";
            }
            txtSoToaGioiHan.Text = $" / {DanhMucMauPhuongTien.SoToaToiDa} toa";

            // Dau may: uu tien may dang chi dinh cho chuyen, neu khong thi may san sang dau tien
            cboDauMay.ItemsSource = dauVao.DanhSachDauMay;
            _dauMayBanDau = dauVao.DanhSachDauMay.FirstOrDefault(d => d.SoHieu == dauVao.SoHieuDauMayDangChon)
                         ?? dauVao.DanhSachDauMay.FirstOrDefault(d => d.TrangThai == "SAN_SANG")
                         ?? dauVao.DanhSachDauMay.FirstOrDefault();
            cboDauMay.SelectedItem = _dauMayBanDau;

            icDoanTau.ItemsSource = _doanTau;

            _boLocBai = CollectionViewSource.GetDefaultView(_baiToa);
            _boLocBai.SortDescriptions.Add(new SortDescription(nameof(ToaLapTau.LaToaHang), ListSortDirection.Ascending));
            _boLocBai.SortDescriptions.Add(new SortDescription(nameof(ToaLapTau.LoaiCode), ListSortDirection.Ascending));
            _boLocBai.SortDescriptions.Add(new SortDescription(nameof(ToaLapTau.SoHieu), ListSortDirection.Ascending));
            _boLocBai.Filter = LocToaBai;
            icBaiToa.ItemsSource = _boLocBai;

            NapPhuongAnBanDau();

            _dangKhoiTao = false;
            CapNhatSauThayDoi(danhDauThayDoi: false);
        }

        private void NapPhuongAnBanDau()
        {
            _doanTau.Clear();
            _baiToa.Clear();
            foreach (var t in _dauVao.DoanTauBanDau) _doanTau.Add(t);
            foreach (var t in _dauVao.BaiToa) _baiToa.Add(t);
        }

        // =====================================================================
        // BAT DAU KEO
        // =====================================================================

        private void The_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement the || the.DataContext is not ToaLapTau toa) return;

            // Bam vao nut X tren the thi de nut tu xu ly
            if (NamTrongNut(e.OriginalSource as DependencyObject, the)) return;

            bool dangTrongDoan = _doanTau.Contains(toa);

            // Nhap dup: thao toa (neu dang trong doan) hoac moc vao cuoi doan (neu dang o bai)
            if (e.ClickCount == 2)
            {
                if (dangTrongDoan) ThaoToa(toa);
                else MocVaoCuoiDoan(toa);

                _toaDuocNhan = null;
                e.Handled = true;
                return;
            }

            _diemNhan = e.GetPosition(bdGoc);
            _toaDuocNhan = toa;
            _theDuocNhan = the;
        }

        private void The_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dangKeo || _toaDuocNhan == null || _theDuocNhan == null) return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _toaDuocNhan = null;
                return;
            }

            Point p = e.GetPosition(bdGoc);
            if (Math.Abs(p.X - _diemNhan.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(p.Y - _diemNhan.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            BatDauKeo(_toaDuocNhan, _theDuocNhan);
        }

        private void BatDauKeo(ToaLapTau toa, FrameworkElement the)
        {
            _dangKeo = true;

            // Tao "bong" toa di theo chuot
            Point gocThe = the.TranslatePoint(new Point(0, 0), bdGoc);
            var lechTay = new Point(_diemNhan.X - gocThe.X, _diemNhan.Y - gocThe.Y);
            var lop = AdornerLayer.GetAdornerLayer(bdGoc);
            if (lop != null)
            {
                _bongKeo = new KeoThaAdorner(bdGoc, the, lechTay);
                lop.Add(_bongKeo);
                _bongKeo.CapNhatViTri(_diemNhan);
            }

            the.Opacity = 0.3;

            try
            {
                DragDrop.DoDragDrop(the, new DataObject(DinhDangKeo, toa), DragDropEffects.Move);
            }
            finally
            {
                the.Opacity = 1.0;
                if (_bongKeo != null)
                {
                    AdornerLayer.GetAdornerLayer(bdGoc)?.Remove(_bongKeo);
                    _bongKeo = null;
                }
                AnVachChen();
                HienLopThaoToa(false);

                _dangKeo = false;
                _toaDuocNhan = null;
                _theDuocNhan = null;
            }
        }

        private static bool NamTrongNut(DependencyObject? phanTu, DependencyObject dungTai)
        {
            while (phanTu != null && phanTu != dungTai)
            {
                if (phanTu is ButtonBase) return true;
                phanTu = phanTu is Visual ? VisualTreeHelper.GetParent(phanTu) : LogicalTreeHelper.GetParent(phanTu);
            }
            return false;
        }

        // =====================================================================
        // KEO QUA CUA SO: cap nhat bong toa, mac dinh khong cho tha
        // =====================================================================

        private void Goc_PreviewDragOver(object sender, DragEventArgs e)
        {
            _bongKeo?.CapNhatViTri(e.GetPosition(bdGoc));
        }

        private void Goc_DragOver(object sender, DragEventArgs e)
        {
            // Chi toi day khi chuot khong nam tren doan tau hoac bai toa
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        // =====================================================================
        // THA VAO DOAN TAU (them moi hoac doi vi tri)
        // =====================================================================

        private void DoanTau_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DinhDangKeo))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            HienVachChen(TinhViTriChen(e.GetPosition(gridDoanTau)));
            TuDongCuonDoanTau(e);

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void DoanTau_DragLeave(object sender, DragEventArgs e)
        {
            // DragLeave cung ban ra khi chuot di tu phan tu con nay sang phan tu con khac
            if (!ChuotNamTrong(bdVungDoanTau, e)) AnVachChen();
        }

        private void DoanTau_Drop(object sender, DragEventArgs e)
        {
            AnVachChen();
            e.Handled = true;

            if (e.Data.GetData(DinhDangKeo) is not ToaLapTau toa) return;

            int viTri = TinhViTriChen(e.GetPosition(gridDoanTau));
            int viTriCu = _doanTau.IndexOf(toa);

            if (viTriCu >= 0)
            {
                // Doi vi tri trong doan: bu tru vi tri cu da bi rut ra
                if (viTri > viTriCu) viTri--;
                if (viTri == viTriCu) return;
                _doanTau.Move(viTriCu, viTri);
            }
            else
            {
                _baiToa.Remove(toa);
                _doanTau.Insert(viTri, toa);
            }

            CapNhatSauThayDoi();
            CuonToiToa(toa);
        }

        // Vi tri chen = so toa co tam nam ben trai con tro
        private int TinhViTriChen(Point p)
        {
            for (int i = 0; i < _doanTau.Count; i++)
            {
                if (icDoanTau.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement c) continue;
                Point goc = c.TranslatePoint(new Point(0, 0), gridDoanTau);
                if (p.X < goc.X + c.ActualWidth / 2) return i;
            }
            return _doanTau.Count;
        }

        private void HienVachChen(int viTri)
        {
            double x = icDoanTau.TranslatePoint(new Point(0, 0), gridDoanTau).X;
            for (int i = 0; i < viTri && i < _doanTau.Count; i++)
            {
                if (icDoanTau.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement c)
                    x += c.ActualWidth;
            }

            // Dat vach vao giua khe noi giua hai toa (khe rong 6px = Margin phai cua the toa)
            Canvas.SetLeft(vachChen, x - 5);
            Canvas.SetTop(vachChen, -6);
            vachChen.Height = 88 + 8 + 8;
            vachChen.Visibility = Visibility.Visible;
        }

        private void AnVachChen() => vachChen.Visibility = Visibility.Collapsed;

        private void TuDongCuonDoanTau(DragEventArgs e)
        {
            const double vungCuon = 50;
            Point p = e.GetPosition(svDoanTau);

            if (p.X < vungCuon)
                svDoanTau.ScrollToHorizontalOffset(svDoanTau.HorizontalOffset - 14);
            else if (p.X > svDoanTau.ActualWidth - vungCuon)
                svDoanTau.ScrollToHorizontalOffset(svDoanTau.HorizontalOffset + 14);
        }

        private void CuonToiToa(ToaLapTau toa)
        {
            // Doi giao dien sinh xong the toa moi roi moi cuon toi
            Dispatcher.InvokeAsync(() =>
            {
                if (icDoanTau.ItemContainerGenerator.ContainerFromItem(toa) is FrameworkElement c)
                    c.BringIntoView();
            }, DispatcherPriority.Loaded);
        }

        // =====================================================================
        // THA XUONG BAI TOA (thao toa khoi doan)
        // =====================================================================

        private void Bai_DragOver(object sender, DragEventArgs e)
        {
            bool tuDoanTau = e.Data.GetData(DinhDangKeo) is ToaLapTau toa && _doanTau.Contains(toa);

            e.Effects = tuDoanTau ? DragDropEffects.Move : DragDropEffects.None;
            HienLopThaoToa(tuDoanTau);
            e.Handled = true;
        }

        private void Bai_DragLeave(object sender, DragEventArgs e)
        {
            if (!ChuotNamTrong(bdVungBai, e)) HienLopThaoToa(false);
        }

        private void Bai_Drop(object sender, DragEventArgs e)
        {
            HienLopThaoToa(false);
            e.Handled = true;

            if (e.Data.GetData(DinhDangKeo) is ToaLapTau toa && _doanTau.Contains(toa))
                ThaoToa(toa);
        }

        private void HienLopThaoToa(bool hien)
            => lopThaoToa.Visibility = hien ? Visibility.Visible : Visibility.Collapsed;

        private static bool ChuotNamTrong(FrameworkElement vung, DragEventArgs e)
        {
            Point p = e.GetPosition(vung);
            return p.X >= 0 && p.Y >= 0 && p.X < vung.ActualWidth && p.Y < vung.ActualHeight;
        }

        // =====================================================================
        // THAO TAC NHANH
        // =====================================================================

        private void ThaoToa(ToaLapTau toa)
        {
            _doanTau.Remove(toa);
            _baiToa.Add(toa);
            CapNhatSauThayDoi();
        }

        private void MocVaoCuoiDoan(ToaLapTau toa)
        {
            _baiToa.Remove(toa);
            _doanTau.Add(toa);
            CapNhatSauThayDoi();
            CuonToiToa(toa);
        }

        private void BtnThaoToa_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ToaLapTau toa) ThaoToa(toa);
        }

        private void BtnTuDongSapXep_Click(object sender, RoutedEventArgs e)
        {
            if (_doanTau.Count < 2) return;

            // Toa khach xep theo hang (AN -> BN -> NML -> NC), toa hang don ve cuoi doan
            var thuTuMoi = _doanTau
                .OrderBy(t => HangSapXep(t.LoaiCode))
                .ThenBy(t => t.SoHieu, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = 0; i < thuTuMoi.Count; i++)
            {
                int cu = _doanTau.IndexOf(thuTuMoi[i]);
                if (cu != i) _doanTau.Move(cu, i);
            }

            CapNhatSauThayDoi();
        }

        private static int HangSapXep(string loai) => loai switch
        {
            "AN" => 0,
            "BN" => 1,
            "NML" => 2,
            "NC" => 3,
            "G" => 10,
            "M" => 11,
            "P" => 12,
            _ => 9
        };

        private void BtnThaoHet_Click(object sender, RoutedEventArgs e)
        {
            if (_doanTau.Count == 0) return;

            foreach (var t in _doanTau.ToList())
            {
                _doanTau.Remove(t);
                _baiToa.Add(t);
            }
            CapNhatSauThayDoi();
        }

        private void BtnDatLai_Click(object sender, RoutedEventArgs e)
        {
            NapPhuongAnBanDau();
            cboDauMay.SelectedItem = _dauMayBanDau;
            _coThayDoi = false;
            CapNhatSauThayDoi(danhDauThayDoi: false);
        }

        private void CboDauMay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            CapNhatSauThayDoi();
        }

        // =====================================================================
        // LOC BAI TOA
        // =====================================================================

        private bool LocToaBai(object o)
        {
            if (o is not ToaLapTau t) return false;

            string tuKhoa = txtTimBai.Text.Trim();
            if (tuKhoa.Length > 0 &&
                !t.SoHieu.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !t.TenLoai.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
                return false;

            if (rdoLocNgoi.IsChecked == true) return t.LoaiCode is "NC" or "NML";
            if (rdoLocNam.IsChecked == true) return t.LoaiCode is "BN" or "AN";
            if (rdoLocHang.IsChecked == true) return t.LaToaHang;
            return true;
        }

        private void BoLocBai_Changed(object sender, RoutedEventArgs e)
        {
            if (_dangKhoiTao || _boLocBai == null) return;
            _boLocBai.Refresh();
            CapNhatTrangThaiBai();
        }

        private void TxtTimBai_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtGoiYTimBai.Visibility = txtTimBai.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (_dangKhoiTao || _boLocBai == null) return;
            _boLocBai.Refresh();
            CapNhatTrangThaiBai();
        }

        private void CapNhatTrangThaiBai()
        {
            txtDemBai.Text = $"{_baiToa.Count} toa";
            txtBaiTrong.Visibility = _boLocBai == null || _boLocBai.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        // =====================================================================
        // TONG HOP + KIEM TRA AN TOAN
        // =====================================================================

        private decimal GioiHanChieuDai()
        {
            decimal toiDa = DanhMucMauPhuongTien.ChieuDaiDoanTauToiDaM;
            return _dauVao.DuongTranhNganNhatM is > 0 ? Math.Min(_dauVao.DuongTranhNganNhatM.Value, toiDa) : toiDa;
        }

        private decimal TinhTongChieuDai(DauMayLapTau? dm) => (dm?.ChieuDaiM ?? 0m) + _doanTau.Sum(t => t.ChieuDaiM);

        private decimal TinhTongTrongLuong() => _doanTau.Sum(t => t.TongTrongLuongTan);

        private void CapNhatSauThayDoi(bool danhDauThayDoi = true)
        {
            if (danhDauThayDoi) _coThayDoi = true;

            for (int i = 0; i < _doanTau.Count; i++) _doanTau[i].ThuTu = i + 1;

            var dm = cboDauMay.SelectedItem as DauMayLapTau;

            // --- The dau may ---
            txtDauMaySoHieu.Text = dm?.SoHieu ?? "Chưa chọn";
            txtDauMayDong.Text = dm != null ? $"DÒNG {dm.MaDongCode}" : "";
            txtDauMayThongSo.Text = dm != null ? $"{dm.ChieuDaiM:N1} m · kéo {dm.SucKeoTan:N0} t" : "";

            txtOCho.Text = _doanTau.Count == 0 ? "Kéo toa từ bãi vào đây" : "Thả để móc vào cuối đoàn";
            txtDemToaDoan.Text = $"{_doanTau.Count} toa";
            CapNhatTrangThaiBai();

            // --- So lieu tong hop ---
            int soToa = _doanTau.Count;
            int soToaHang = _doanTau.Count(t => t.LaToaHang);
            decimal chieuDai = TinhTongChieuDai(dm);
            decimal trongLuong = TinhTongTrongLuong();
            decimal gioiHanDai = GioiHanChieuDai();

            txtSoToa.Text = soToa.ToString();
            txtSoToa.Foreground = MauChu(soToa > DanhMucMauPhuongTien.SoToaToiDa ? "#B91C1C" : "#0F172A");
            txtSoToaPhu.Text = soToa == 0 ? "Chưa móc nối toa nào" : $"{soToa - soToaHang} toa khách · {soToaHang} toa hàng";

            txtChieuDai.Text = $"{chieuDai:N1} m";
            txtChieuDaiGioiHan.Text = $"/ {gioiHanDai:N0} m";
            DatVach(colDaiDaDung, colDaiConLai, bdVachDai, gioiHanDai > 0 ? (double)(chieuDai / gioiHanDai) * 100 : 0);

            txtTrongLuong.Text = $"{trongLuong:N1} t";
            if (dm != null && dm.SucKeoTan > 0)
            {
                txtTrongLuongGioiHan.Text = $"/ {dm.SucKeoTan:N0} t sức kéo";
                DatVach(colTaiDaDung, colTaiConLai, bdVachTai, (double)(trongLuong / dm.SucKeoTan) * 100);
            }
            else
            {
                txtTrongLuongGioiHan.Text = "chưa chọn đầu máy";
                DatVach(colTaiDaDung, colTaiConLai, bdVachTai, 0);
            }

            int sucChua = _doanTau.Where(t => !t.LaToaHang).Sum(t => t.SucChua);
            txtSucChua.Text = $"{sucChua:N0} chỗ";
            txtSucChuaPhu.Text = soToa - soToaHang == 0
                ? "Chưa có toa khách"
                : string.Join("  ·  ", _doanTau.Where(t => !t.LaToaHang)
                                              .GroupBy(t => t.LoaiCode)
                                              .OrderBy(g => HangSapXep(g.Key))
                                              .Select(g => $"{g.Key} ×{g.Count()}"));

            // --- Kiem tra an toan ---
            var hangMuc = KiemTraAnToan(dm, chieuDai, trongLuong);
            icHangMucAnToan.ItemsSource = hangMuc;
            CapNhatKetLuan(hangMuc);
        }

        private List<HangMucAnToanHienThi> KiemTraAnToan(DauMayLapTau? dm, decimal chieuDai, decimal trongLuong)
        {
            var ds = new List<HangMucAnToanHienThi>();
            int n = _doanTau.Count;

            // 1. Dau may keo chinh
            const string tenDauMay = "Đầu máy kéo chính";
            if (dm == null)
                ds.Add(HangMucAnToanHienThi.Loi(tenDauMay, "Chưa chọn đầu máy kéo cho đoàn tàu."));
            else if (dm.TrangThai == "BAO_DUONG")
                ds.Add(HangMucAnToanHienThi.Loi(tenDauMay, $"{dm.SoHieu} đang bảo dưỡng, không được đưa vào vận dụng."));
            else if (dm.TrangThai == "DANG_CHAY")
                ds.Add(HangMucAnToanHienThi.CanhBao(tenDauMay, $"{dm.SoHieu} đang vận dụng ở chuyến khác. Cần đối chiếu lịch để tránh trùng giờ."));
            else
                ds.Add(HangMucAnToanHienThi.Dat(tenDauMay, $"{dm.SoHieu} (dòng {dm.MaDongCode}) sẵn sàng, sức kéo {dm.SucKeoTan:N0} tấn."));

            // 2. So toa (CK TongSoToa 1..20)
            const string tenSoToa = "Số toa trong đoàn";
            int toiDa = DanhMucMauPhuongTien.SoToaToiDa;
            if (n == 0)
                ds.Add(HangMucAnToanHienThi.Loi(tenSoToa, "Chưa móc nối toa nào vào đầu máy."));
            else if (n > toiDa)
                ds.Add(HangMucAnToanHienThi.Loi(tenSoToa, $"Biên chế {n} toa, vượt giới hạn {toiDa} toa của một đoàn tàu."));
            else
                ds.Add(HangMucAnToanHienThi.Dat(tenSoToa, $"Biên chế {n} toa, trong giới hạn 1–{toiDa} toa."));

            // 3. Chieu dai vs duong tranh ngan nhat
            const string tenChieuDai = "Chiều dài đoàn tàu vs đường tránh";
            var (datDai, thongDiepDai) = PhuongTienService.DoiChieuChieuDaiDuongTranh(chieuDai, GioiHanChieuDai());
            string ghiChuNguon = _dauVao.DuongTranhNganNhatM is > 0 ? "" : " (Hành trình chưa có dữ liệu đường tránh, đối chiếu giới hạn hệ thống.)";
            ds.Add(datDai
                ? HangMucAnToanHienThi.Dat(tenChieuDai, $"Dài {chieuDai:N1} m kể cả đầu máy. {thongDiepDai}{ghiChuNguon}")
                : HangMucAnToanHienThi.Loi(tenChieuDai, $"Dài {chieuDai:N1} m kể cả đầu máy. {thongDiepDai}{ghiChuNguon}"));

            // 4. Trong luong keo vs suc keo dau may
            const string tenSucKeo = "Trọng lượng kéo vs sức kéo";
            var (datSucKeo, _, thongDiepSucKeo) = PhuongTienService.DoiChieuSucKeo(trongLuong, dm?.SucKeoTan);
            if (dm == null)
                ds.Add(HangMucAnToanHienThi.CanhBao(tenSucKeo, thongDiepSucKeo));
            else
                ds.Add(datSucKeo
                    ? HangMucAnToanHienThi.Dat(tenSucKeo, $"Kéo {trongLuong:N1} tấn toàn tải. {thongDiepSucKeo}")
                    : HangMucAnToanHienThi.Loi(tenSucKeo, $"Kéo {trongLuong:N1} tấn toàn tải. {thongDiepSucKeo}"));

            if (n == 0) return ds;

            // 5. Tai trong truc tung toa
            const string tenTruc = "Tải trọng trục";
            decimal nguongTruc = DanhMucMauPhuongTien.TaiTrongTrucToiDaTan;
            var toaVuot = _doanTau.Where(t => t.VuotTaiTrongTruc).ToList();
            if (toaVuot.Count > 0)
                ds.Add(HangMucAnToanHienThi.Loi(tenTruc,
                    $"{string.Join(", ", toaVuot.Select(t => $"{t.SoHieu} ({t.TaiTrongTrucTan:N2} t)"))} vượt ngưỡng {nguongTruc:N1} t/trục khi chở đầy."));
            else
                ds.Add(HangMucAnToanHienThi.Dat(tenTruc,
                    $"Lớn nhất {_doanTau.Max(t => t.TaiTrongTrucTan):N2} t/trục, dưới ngưỡng {nguongTruc:N1} t/trục."));

            // 6. Thu tu xep toa: toa hang phai lien khoi o dau hoac cuoi doan
            const string tenThuTu = "Thứ tự xếp toa";
            var viTriHang = Enumerable.Range(0, n).Where(i => _doanTau[i].LaToaHang).ToList();
            if (viTriHang.Count == 0)
            {
                ds.Add(HangMucAnToanHienThi.Dat(tenThuTu, "Đoàn tàu chỉ gồm toa khách."));
            }
            else if (viTriHang.Count == n)
            {
                ds.Add(HangMucAnToanHienThi.Dat(tenThuTu, "Đoàn tàu hàng thuần, không có toa khách."));
            }
            else
            {
                bool lienKhoi = viTriHang[^1] - viTriHang[0] + 1 == viTriHang.Count;
                bool oDauHoacCuoi = viTriHang[0] == 0 || viTriHang[^1] == n - 1;

                if (lienKhoi && oDauHoacCuoi)
                {
                    string noi = viTriHang[0] == 0 ? "ngay sau đầu máy" : "ở cuối đoàn";
                    ds.Add(HangMucAnToanHienThi.Dat(tenThuTu, $"Toa hàng xếp liền khối {noi}, không xen giữa toa khách."));
                }
                else
                {
                    var toaXen = viTriHang
                        .Where(i => Enumerable.Range(0, i).Any(j => !_doanTau[j].LaToaHang) &&
                                    Enumerable.Range(i + 1, n - i - 1).Any(j => !_doanTau[j].LaToaHang))
                        .Select(i => _doanTau[i].SoHieu)
                        .ToList();

                    string moTa = toaXen.Count > 0
                        ? $"Toa hàng {string.Join(", ", toaXen)} đang xen giữa các toa khách."
                        : "Toa hàng đang bị tách thành nhiều cụm.";
                    ds.Add(HangMucAnToanHienThi.Loi(tenThuTu, $"{moTa} Cần dồn toa hàng thành một khối ở đầu hoặc cuối đoàn."));
                }
            }

            return ds;
        }

        private void CapNhatKetLuan(List<HangMucAnToanHienThi> hangMuc)
        {
            int soLoi = hangMuc.Count(h => h.MucDo == "LOI");
            int soCanhBao = hangMuc.Count(h => h.MucDo == "CANH_BAO");

            string nen, vien, chu, tieuDe, moTa;
            SymbolRegular bieuTuong;

            if (soLoi > 0)
            {
                nen = "#FEF2F2"; vien = "#FECACA"; chu = "#B91C1C";
                bieuTuong = SymbolRegular.DismissCircle24;
                tieuDe = "KHÔNG ĐỦ ĐIỀU KIỆN";
                moTa = $"Có {soLoi} hạng mục không đạt, chưa thể xác nhận lập tàu";
            }
            else if (soCanhBao > 0)
            {
                nen = "#FFFBEB"; vien = "#FDE68A"; chu = "#B45309";
                bieuTuong = SymbolRegular.Warning24;
                tieuDe = "ĐẠT CÓ LƯU Ý";
                moTa = $"Các hạng mục kỹ thuật đạt, còn {soCanhBao} lưu ý";
            }
            else
            {
                nen = "#F0FDF4"; vien = "#BBF7D0"; chu = "#15803D";
                bieuTuong = SymbolRegular.CheckmarkCircle24;
                tieuDe = "ĐỦ ĐIỀU KIỆN XUẤT GA";
                moTa = "Toàn bộ hạng mục đối chiếu đều đạt yêu cầu";
            }

            bdKetLuan.Background = MauChu(nen);
            bdKetLuan.BorderBrush = MauChu(vien);
            icoKetLuan.Symbol = bieuTuong;
            icoKetLuan.Foreground = MauChu(chu);
            txtKetLuan.Text = tieuDe;
            txtKetLuan.Foreground = MauChu(chu);
            txtKetLuanPhu.Text = moTa;
        }

        // Thanh tien do: xanh <= 85%, vang <= 100%, do khi vuot
        private static void DatVach(ColumnDefinition cotDaDung, ColumnDefinition cotConLai, Border vach, double phanTram)
        {
            vach.Background = MauChu(phanTram > 100 ? "#DC2626" : phanTram > 85 ? "#D97706" : "#15803D");
            double hienThi = Math.Clamp(phanTram, 0, 100);
            cotDaDung.Width = new GridLength(hienThi, GridUnitType.Star);
            cotConLai.Width = new GridLength(100 - hienThi, GridUnitType.Star);
        }

        private static Brush MauChu(string ma) => (Brush)new BrushConverter().ConvertFrom(ma)!;

        // =====================================================================
        // XAC NHAN / DONG
        // =====================================================================

        private void BtnXacNhan_Click(object sender, RoutedEventArgs e)
        {
            var dm = cboDauMay.SelectedItem as DauMayLapTau;

            if (_doanTau.Count == 0)
            {
                ThongBaoDialog.CanhBao("Đoàn tàu chưa có toa nào. Hãy kéo toa từ bãi vào đoàn trước khi xác nhận.",
                                       "Chưa lập tàu", this);
                return;
            }

            decimal chieuDai = TinhTongChieuDai(dm);
            decimal trongLuong = TinhTongTrongLuong();
            var hangMuc = KiemTraAnToan(dm, chieuDai, trongLuong);
            var loi = hangMuc.Where(h => h.MucDo == "LOI").ToList();

            if (loi.Count > 0)
            {
                ThongBaoDialog.CanhBao(
                    $"Chưa thể xác nhận lập tàu vì còn {loi.Count} hạng mục không đạt:\n\n" +
                    string.Join("\n", loi.Select(h => $"• {h.TenHangMuc}")) +
                    "\n\nHãy điều chỉnh đoàn tàu theo gợi ý ở bảng kiểm tra an toàn.",
                    "Đoàn tàu chưa đạt", this);
                return;
            }

            KetQua = new KetQuaLapTau
            {
                DauMay = dm,
                DanhSachToa = _doanTau.ToList(),
                TongChieuDaiM = chieuDai,
                TongTrongLuongTan = trongLuong,
                TongSucChua = _doanTau.Where(t => !t.LaToaHang).Sum(t => t.SucChua),
                HangMuc = hangMuc
            };
            DialogResult = true;
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) => DongCoXacNhan();

        private void DongCoXacNhan()
        {
            if (_coThayDoi &&
                !ThongBaoDialog.XacNhan("Phương án lập tàu đang dở sẽ bị bỏ. Bạn có chắc muốn đóng?",
                                        "Đóng màn hình lập tàu", nutDongY: "Đóng", nutHuy: "Ở lại", owner: this))
                return;

            DialogResult = false;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_dangKeo) return;

            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DongCoXacNhan();
            }
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                BtnXacNhan_Click(this, new RoutedEventArgs());
            }
        }
    }
}
