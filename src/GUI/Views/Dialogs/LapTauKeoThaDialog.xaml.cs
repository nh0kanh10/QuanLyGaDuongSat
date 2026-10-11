using GUI.ViewModels.KyThuat;
using GUI.Helpers;
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
using DTO.PhuongTien;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // LAP DOAN TAU KEO - THA (SRS Nhom 3) - vanhanh.DoanTau + ChiTietDoanTau
    //
    // - Keo toa tu bai vao doan tau, keo trong doan de doi thu tu moc noi,
    //   keo nguoc xuong bai (hoac nhap dup / bam X) de thao toa.
    // - CAT / NOI TOA DOC DUONG: chuyen co lich dung ga trung gian (vanhanh.LichDungGa)
    //   thi hien thanh hanh trinh. Chon mot ga = xem doan tau khi roi ga do; keo toa
    //   vao = noi tai ga do, keo toa ra = cat tai ga do. Moi toa mang pham vi chay
    //   (MaGaNoi -> MaGaCat), null = chay suot.
    // - Dau may keo chinh + dau may day (tuy chon).
    // - Sau moi thay doi, chay lai BO LUAT BUS.Services.QuyTacLapTau (cung bo Service
    //   kiem lai khi luu): dau may, so toa, chieu dai tung ga dung, suc keo, tai trong
    //   truc, toa hang, toa ban / bao duong, tac nghiep cat / noi.
    // - Xac nhan: goi ham luu cua trang (LapTauService.LuuPhuongAn). Service tu choi
    //   (nguoi khac vua sua, chuyen da chay...) thi hien ly do va giu nguyen man hinh.
    // =========================================================================
    public partial class LapTauKeoThaDialog : Window
    {
        private const string DinhDangKeo = "GUI.KyThuat.ToaLapTau";
        private const double ChieuCaoThe = 104;

        private readonly ThongTinLapTau _dauVao;
        private readonly ObservableCollection<ToaLapTau> _doanTau = new();
        private readonly ObservableCollection<ToaLapTau> _baiToa = new();
        private readonly ICollectionView? _boLocBai;
        private readonly DauMayLapTau? _dauMayBanDau;
        private readonly List<DauMayLapTau> _dsDauMayDay;
        private readonly DauMayLapTau _dauMayDayBanDau;
        private readonly decimal _gioiHanChieuDaiChung;   // nho nhat tren cac ga dung (khong co lich dung: 450 m)
        private readonly bool _dangKhoiTao;
        private bool _coThayDoi;

        // --- Hanh trinh: cac ga dung; rong = khong cat / noi doc duong ---
        private readonly List<DiemDungLapTau> _hanhTrinh;
        private int _viTriXem;                   // xem doan tau khi roi ga thu _viTriXem

        // --- Trang thai keo - tha ---
        private Point _diemNhan;                 // toa do chuot luc nhan, theo bdGoc
        private ToaLapTau? _toaDuocNhan;
        private FrameworkElement? _theDuocNhan;
        private KeoThaAdorner? _bongKeo;
        private bool _dangKeo;

        public KetQuaLuuLapTauDTO? KetQua { get; private set; }

        public LapTauKeoThaDialog(ThongTinLapTau dauVao)
        {
            _dangKhoiTao = true;
            InitializeComponent();
            _dauVao = dauVao;

            // Thu nho cho vua man hinh laptop 1366x768
            Rect vung = SystemParameters.WorkArea;
            Width = Math.Min(Width, vung.Width - 8);
            Height = Math.Min(Height, vung.Height - 8);

            var bc = dauVao.BoiCanh;
            txtTieuDePhu.Text = bc.TenChuyen;

            // Gioi han chieu dai nho nhat tren cac ga dung (QuyTacLapTau kiem tung ga)
            if (bc.HanhTrinh.Count > 0)
            {
                var gioiHanTungGa = bc.HanhTrinh
                    .Select(d => (Ga: d, GioiHan: QuyTacLapTau.GioiHanChieuDaiTaiGa(d).GioiHanM))
                    .ToList();
                var gaHanChe = gioiHanTungGa.MinBy(x => x.GioiHan);
                _gioiHanChieuDaiChung = gaHanChe.GioiHan;
                bool moiGaNhuNhau = gioiHanTungGa.All(x => x.GioiHan == _gioiHanChieuDaiChung);
                txtDuongTranh.Text = $"{_gioiHanChieuDaiChung:N0} m · {(moiGaNhuNhau ? "mọi ga" : gaHanChe.Ga.MaGaCode)}";
            }
            else
            {
                _gioiHanChieuDaiChung = QuyTacLapTau.ChieuDaiToiDaM;
                txtDuongTranh.Text = $"{_gioiHanChieuDaiChung:N0} m";
                bdDuongTranh.ToolTip = "Chuyến chưa có lịch dừng ga: chiều dài đoàn tàu đối chiếu với giới hạn " +
                    $"{QuyTacLapTau.ChieuDaiToiDaM:N0} m của hệ thống.";
            }
            txtSoToaGioiHan.Text = $" / {QuyTacLapTau.SoToaToiDa} toa";

            // Hanh trinh: can it nhat 1 ga trung gian moi co cho cat / noi
            _hanhTrinh = dauVao.HanhTrinh.Count >= 3 ? dauVao.HanhTrinh : new List<DiemDungLapTau>();
            icHanhTrinh.ItemsSource = _hanhTrinh;
            if (!CoHanhTrinh)
            {
                svHanhTrinh.Visibility = Visibility.Collapsed;
                spKhongCoHanhTrinh.Visibility = Visibility.Visible;
                txtGoiYHanhTrinh.Text = "Không cắt / nối";
                txtKhongCoHanhTrinh.Text = dauVao.GhiChuHanhTrinh.Length > 0
                    ? dauVao.GhiChuHanhTrinh
                    : "Chuyến không có ga dừng trung gian: đoàn tàu chạy suốt, không cắt / nối toa dọc đường.";
            }

            // Dau may: uu tien may dang luu cho chuyen, neu khong thi may san sang, khong ban chuyen khac
            cboDauMay.ItemsSource = dauVao.DanhSachDauMay;
            _dauMayBanDau = dauVao.DanhSachDauMay.FirstOrDefault(d => d.MaDauMay == bc.MaDauMayChinh)
                         ?? dauVao.DanhSachDauMay.FirstOrDefault(d => d.TrangThai == "SAN_SANG" && d.GhiChuBan.Length == 0)
                         ?? dauVao.DanhSachDauMay.FirstOrDefault();
            cboDauMay.SelectedItem = _dauMayBanDau;

            // May day (tuy chon): muc dau = khong dung
            _dsDauMayDay = new List<DauMayLapTau> { DauMayLapTau.KhongDung() };
            _dsDauMayDay.AddRange(dauVao.DanhSachDauMay);
            cboDauMayDay.ItemsSource = _dsDauMayDay;
            _dauMayDayBanDau = _dsDauMayDay.FirstOrDefault(d => !d.LaKhongDung && d.MaDauMay == bc.MaDauMayDay) ?? _dsDauMayDay[0];
            cboDauMayDay.SelectedItem = _dauMayDayBanDau;

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

        // Dialog thao tac tren ban sao: bam Huy / Dat lai thi phuong an goc khong bi dung
        private void NapPhuongAnBanDau()
        {
            _doanTau.Clear();
            _baiToa.Clear();
            foreach (var t in _dauVao.DoanTauBanDau) _doanTau.Add(t.SaoChep());
            foreach (var t in _dauVao.BaiToa)
            {
                var ban = t.SaoChep();
                ban.XoaPhamVi();
                ban.DangHoatDong = true;
                _baiToa.Add(ban);
            }

            // Chuan hoa pham vi theo hanh trinh cua chuyen (ga khong con trong lich dung -> chay suot)
            foreach (var t in _doanTau)
            {
                int noi = ViTriNoi(t), cat = ViTriCat(t);
                if (noi >= cat) { noi = 0; cat = SoDoan; }
                DatPhamVi(t, noi, cat);
            }

            _viTriXem = 0;
        }

        // =====================================================================
        // HANH TRINH: PHAM VI CHAY CUA TOA + KHU DOAN DANG XEM
        // Khu doan k = tu ga k toi ga k + 1. Toa co mat tren khu doan k khi
        // ViTriNoi <= k < ViTriCat.
        // =====================================================================

        private bool CoHanhTrinh => _hanhTrinh.Count >= 3;

        private int SoDoan => CoHanhTrinh ? _hanhTrinh.Count - 1 : 1;

        private DiemDungLapTau? GaDangXem => CoHanhTrinh ? _hanhTrinh[_viTriXem] : null;

        private int ViTriGa(int? maGa, int macDinh)
        {
            if (maGa == null) return macDinh;
            int i = _hanhTrinh.FindIndex(d => d.MaGa == maGa);
            return i >= 0 ? i : macDinh;
        }

        private int ViTriNoi(ToaLapTau t) => CoHanhTrinh ? ViTriGa(t.MaGaNoi, 0) : 0;

        private int ViTriCat(ToaLapTau t) => CoHanhTrinh ? ViTriGa(t.MaGaCat, SoDoan) : SoDoan;

        private bool CoMatTrongDoan(ToaLapTau t, int k) => ViTriNoi(t) <= k && k < ViTriCat(t);

        private List<ToaLapTau> ToaTrongDoan(int k) => _doanTau.Where(t => CoMatTrongDoan(t, k)).ToList();

        // Toa da co mat tu truoc ga dang xem: thao ra nghia la cat tai ga nay
        private bool SeCatTaiGaXem(ToaLapTau t)
            => CoHanhTrinh && _viTriXem > 0 && CoMatTrongDoan(t, _viTriXem) && ViTriNoi(t) < _viTriXem;

        private void DatPhamVi(ToaLapTau t, int noi, int cat)
        {
            if (!CoHanhTrinh)
            {
                t.XoaPhamVi();
                return;
            }

            noi = Math.Clamp(noi, 0, SoDoan - 1);
            cat = Math.Clamp(cat, noi + 1, SoDoan);
            t.DatPhamVi(noi > 0 ? _hanhTrinh[noi].MaGa : null, _hanhTrinh[noi].MaGaCode,
                        cat < SoDoan ? _hanhTrinh[cat].MaGa : null, _hanhTrinh[cat].MaGaCode);
        }

        private string TenDoan(int tu, int den)
            => CoHanhTrinh ? $"{_hanhTrinh[tu].MaGaCode} → {_hanhTrinh[den].MaGaCode}" : "";

        private void Ga_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not DiemDungLapTau ga || ga.LaGaCuoi) return;

            e.Handled = true;
            if (ga.ViTri == _viTriXem) return;

            _viTriXem = ga.ViTri;
            CapNhatSauThayDoi(danhDauThayDoi: false);
        }

        // --- Menu doi ga noi / ga cat cua mot toa ---

        private void BtnPhamVi_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ToaLapTau toa) MoMenuPhamVi(toa, fe);
        }

        private void MoMenuPhamVi(ToaLapTau toa, FrameworkElement viTriMo)
        {
            if (!CoHanhTrinh || !_doanTau.Contains(toa)) return;

            int noi = ViTriNoi(toa), cat = ViTriCat(toa);
            var menu = new ContextMenu { PlacementTarget = viTriMo, Placement = PlacementMode.Bottom };

            menu.Items.Add(new MenuItem { Header = $"{toa.SoHieu} · đang chạy {toa.NhanPhamVi}", IsEnabled = false });
            menu.Items.Add(new Separator());

            var mucNoi = new MenuItem { Header = "Nối vào đoàn tại" };
            for (int i = 0; i < cat; i++)
            {
                int v = i;
                mucNoi.Items.Add(TaoMucGa(_hanhTrinh[v], v == noi, () => DoiPhamVi(toa, v, ViTriCat(toa))));
            }
            menu.Items.Add(mucNoi);

            var mucCat = new MenuItem { Header = "Cắt khỏi đoàn tại" };
            for (int i = noi + 1; i <= SoDoan; i++)
            {
                int v = i;
                mucCat.Items.Add(TaoMucGa(_hanhTrinh[v], v == cat, () => DoiPhamVi(toa, ViTriNoi(toa), v)));
            }
            menu.Items.Add(mucCat);

            var mucSuot = new MenuItem { Header = "Chạy suốt hành trình", IsEnabled = toa.CoPhamViRieng };
            mucSuot.Click += (_, _) => DoiPhamVi(toa, 0, SoDoan);
            menu.Items.Add(mucSuot);

            menu.Items.Add(new Separator());
            var mucThao = new MenuItem { Header = "Tháo khỏi đoàn (trả về bãi)" };
            mucThao.Click += (_, _) => ThaoToa(toa);
            menu.Items.Add(mucThao);

            menu.IsOpen = true;
        }

        private static MenuItem TaoMucGa(DiemDungLapTau ga, bool dangChon, Action khiChon)
        {
            var muc = new MenuItem
            {
                Header = ga.NhanMenu,
                IsChecked = dangChon,
                FontWeight = dangChon ? FontWeights.SemiBold : FontWeights.Normal
            };
            muc.Click += (_, _) => khiChon();
            return muc;
        }

        private void DoiPhamVi(ToaLapTau toa, int noi, int cat)
        {
            if (noi >= cat) return;
            DatPhamVi(toa, noi, cat);
            CapNhatSauThayDoi();
        }

        // =====================================================================
        // BAT DAU KEO
        // =====================================================================

        private void The_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement the || the.DataContext is not ToaLapTau toa) return;

            // Bam vao nut X / nhan pham vi tren the thi de nut tu xu ly
            if (NamTrongNut(e.OriginalSource as DependencyObject, the)) return;

            bool dangTrongDoan = _doanTau.Contains(toa);

            // Nhap dup: thao / cat toa (neu dang trong doan) hoac moc vao doan (neu dang o bai).
            // Toa khong co mat o khu doan dang xem thi mo menu pham vi chay.
            if (e.ClickCount == 2)
            {
                if (!dangTrongDoan) MocVaoCuoiDoan(toa);
                else if (CoMatTrongDoan(toa, _viTriXem)) ThaoHoacCatToa(toa);
                else MoMenuPhamVi(toa, the);

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
                // Bo gia tri cuc bo de trigger cua template (toa ngoai doan hien mo) tiep tuc dieu khien
                the.ClearValue(OpacityProperty);

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
                // Toa moi: noi tai ga dang xem, chay toi ga cuoi
                _baiToa.Remove(toa);
                _doanTau.Insert(viTri, toa);
                DatPhamVi(toa, _viTriXem, SoDoan);
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
            vachChen.Height = ChieuCaoThe + 8 + 8;
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
        // THA XUONG BAI TOA (thao toa khoi doan / cat toa tai ga dang xem)
        // =====================================================================

        private void Bai_DragOver(object sender, DragEventArgs e)
        {
            var toa = e.Data.GetData(DinhDangKeo) as ToaLapTau;
            bool tuDoanTau = toa != null && _doanTau.Contains(toa);

            if (tuDoanTau)
            {
                txtLopThaoToa.Text = SeCatTaiGaXem(toa!)
                    ? $"Thả vào đây để cắt toa tại {GaDangXem!.TenGa}"
                    : "Thả vào đây để tháo toa khỏi đoàn";
            }

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
                ThaoHoacCatToa(toa);
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

        // Toa da chay tu truoc ga dang xem -> cat tai ga nay (van thuoc doan tau o cac
        // doan truoc). Con lai (xem ga dau, toa noi tai chinh ga nay) -> thao han ve bai.
        private void ThaoHoacCatToa(ToaLapTau toa)
        {
            if (SeCatTaiGaXem(toa))
            {
                DatPhamVi(toa, ViTriNoi(toa), _viTriXem);
                CapNhatSauThayDoi();
                return;
            }
            ThaoToa(toa);
        }

        private void ThaoToa(ToaLapTau toa)
        {
            TraVeBai(toa);
            CapNhatSauThayDoi();
        }

        private void TraVeBai(ToaLapTau toa)
        {
            _doanTau.Remove(toa);
            toa.XoaPhamVi();
            toa.DangHoatDong = true;
            toa.NhanNgoaiDoan = "";
            _baiToa.Add(toa);
        }

        private void MocVaoCuoiDoan(ToaLapTau toa)
        {
            _baiToa.Remove(toa);
            _doanTau.Add(toa);
            DatPhamVi(toa, _viTriXem, SoDoan);
            CapNhatSauThayDoi();
            CuonToiToa(toa);
        }

        private void BtnThaoToa_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ToaLapTau toa) ThaoHoacCatToa(toa);
        }

        private void BtnTuDongSapXep_Click(object sender, RoutedEventArgs e)
        {
            if (_doanTau.Count < 2) return;

            // Toa khach xep theo hang (AN -> BN -> NML -> NC), toa hang don ve cuoi doan.
            // Co cat / noi doc duong: toa chay suot dung truoc, cac nhom cat / noi xep o cuoi
            // theo kieu "ngan xep" (noi som dung truoc, cat som dung sau) de nhom nao cung
            // lien khoi o cuoi doan khi cat / noi. Khi do toa hang chay suot don len ngay
            // sau dau may de khong ket giua toa khach va nhom cat / noi.
            bool coCatNoi = _doanTau.Any(t => t.CoPhamViRieng);
            var thuTuMoi = _doanTau
                .OrderBy(t => coCatNoi && t.LaToaHang && !t.CoPhamViRieng ? 0 : 1)
                .ThenBy(ViTriNoi)
                .ThenByDescending(ViTriCat)
                .ThenBy(t => HangSapXep(t.LoaiCode))
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

            foreach (var t in _doanTau.ToList()) TraVeBai(t);
            CapNhatSauThayDoi();
        }

        private void BtnDatLai_Click(object sender, RoutedEventArgs e)
        {
            NapPhuongAnBanDau();
            cboDauMay.SelectedItem = _dauMayBanDau;
            cboDauMayDay.SelectedItem = _dauMayDayBanDau;
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
        // KIEM TRA PHUONG AN: bo luat BUS.Services.QuyTacLapTau (cung bo luat
        // LapTauService chay lai trong giao dich truoc khi ghi CSDL)
        // =====================================================================

        private PhuongAnLapTauDTO TaoPhuongAn() => new()
        {
            MaChuyenTau = _dauVao.BoiCanh.MaChuyenTau,
            PhienBan = _dauVao.BoiCanh.PhienBan,
            MaDauMayChinh = (cboDauMay.SelectedItem as DauMayLapTau)?.MaDauMay,
            MaDauMayDay = cboDauMayDay.SelectedItem is DauMayLapTau day && !day.LaKhongDung ? day.MaDauMay : null,
            DanhSachToa = _doanTau.Select(t => new ToaTrongDoanDTO
            {
                MaToaXe = t.MaToaXe,
                MaGaNoi = t.MaGaNoi,
                MaGaCat = t.MaGaCat
            }).ToList()
        };

        private KetQuaKiemTraLapTauDTO KiemTraHienTai() => QuyTacLapTau.KiemTra(_dauVao.BoiCanh, TaoPhuongAn());

        // Gioi han chieu dai cua khu doan k: nho hon cua ga dau va ga cuoi khu doan
        private decimal GioiHanChieuDaiKhuDoan(int k)
            => CoHanhTrinh ? Math.Min(_hanhTrinh[k].GioiHanChieuDaiM, _hanhTrinh[k + 1].GioiHanChieuDaiM) : _gioiHanChieuDaiChung;

        private string DanhSachSoHieu(IEnumerable<int> maToa)
            => string.Join(", ", maToa.Select(m => _doanTau.FirstOrDefault(t => t.MaToaXe == m)?.SoHieu ?? $"#{m}"));

        private void CapNhatSauThayDoi(bool danhDauThayDoi = true)
        {
            if (danhDauThayDoi) _coThayDoi = true;

            for (int i = 0; i < _doanTau.Count; i++) _doanTau[i].ThuTu = i + 1;

            var dm = cboDauMay.SelectedItem as DauMayLapTau;

            // --- The dau may (may day hien o o chon tren thanh cong cu) ---
            txtDauMaySoHieu.Text = dm?.SoHieu ?? "Chưa chọn";
            txtDauMayDong.Text = dm != null ? $"DÒNG {dm.MaDongCode}" : "";
            txtDauMayThongSo.Text = dm != null ? $"{dm.ChieuDaiM:N1} m · kéo {dm.SucKeoTan:N0} t" : "";

            var kq = KiemTraHienTai();
            var khuDoanXem = kq.KhuDoan[Math.Min(_viTriXem, kq.KhuDoan.Count - 1)];
            var toaXem = ToaTrongDoan(_viTriXem);

            CapNhatTheToa();
            CapNhatHanhTrinh(kq);
            CapNhatTieuDe(toaXem.Count);
            CapNhatTrangThaiBai();

            // --- So lieu cua khu doan dang xem ---
            int soToa = khuDoanXem.SoToa;
            int soToaHang = toaXem.Count(t => t.LaToaHang);
            decimal chieuDai = khuDoanXem.ChieuDaiM;
            decimal trongLuong = khuDoanXem.TrongLuongTan;
            decimal gioiHanDai = GioiHanChieuDaiKhuDoan(_viTriXem);

            txtSoToa.Text = soToa.ToString();
            txtSoToa.Foreground = MauChu(soToa > QuyTacLapTau.SoToaToiDa ? "#B91C1C" : "#0F172A");
            txtSoToaPhu.Text = soToa == 0
                ? (_doanTau.Count == 0 ? "Chưa móc nối toa nào" : "Không còn toa nào trên đoạn này")
                : $"{soToa - soToaHang} toa khách · {soToaHang} toa hàng" +
                  (kq.Chang.Count > 1 ? $" · lớn nhất {kq.SoToaLonNhat}" : "");

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

            int sucChua = toaXem.Where(t => !t.LaToaHang).Sum(t => t.SucChua);
            txtSucChua.Text = $"{sucChua:N0} chỗ";
            txtSucChuaPhu.Text = soToa - soToaHang == 0
                ? "Chưa có toa khách"
                : string.Join("  ·  ", toaXem.Where(t => !t.LaToaHang)
                                             .GroupBy(t => t.LoaiCode)
                                             .OrderBy(g => HangSapXep(g.Key))
                                             .Select(g => $"{g.Key} ×{g.Count()}"));

            // --- Kiem tra an toan ---
            var hangMuc = kq.HangMuc.Select(HangMucAnToanHienThi.TuDTO).ToList();
            icHangMucAnToan.ItemsSource = hangMuc;
            CapNhatKetLuan(hangMuc);
        }

        // Trang thai tung the toa theo khu doan dang xem
        private void CapNhatTheToa()
        {
            foreach (var t in _doanTau)
            {
                t.HienPhamVi = CoHanhTrinh;

                bool coMat = CoMatTrongDoan(t, _viTriXem);
                t.DangHoatDong = coMat;
                if (!coMat)
                {
                    int noi = ViTriNoi(t);
                    t.NhanNgoaiDoan = noi > _viTriXem
                        ? $"Nối tại {_hanhTrinh[noi].MaGaCode}"
                        : $"Đã cắt tại {_hanhTrinh[ViTriCat(t)].MaGaCode}";
                }

                t.GoiYNutThao = SeCatTaiGaXem(t)
                    ? $"Cắt toa tại {GaDangXem!.MaGaCode} (vẫn chạy các đoạn trước)"
                    : "Tháo toa khỏi đoàn";
            }
        }

        // Thanh hanh trinh: ga dang chon, so toa cat / noi tai ga, so toa tung khu doan
        private void CapNhatHanhTrinh(KetQuaKiemTraLapTauDTO kq)
        {
            if (!CoHanhTrinh) return;

            int phutToiThieu = QuyTacLapTau.SoPhutDoToiThieuCatNoi;

            foreach (var ga in _hanhTrinh)
            {
                int k = ga.ViTri;
                var tn = kq.TacNghiep.FirstOrDefault(x => x.ViTri == k);
                bool vuotDai = kq.GaVuotChieuDai.Contains(k);

                ga.DangChon = k == _viTriXem;
                ga.NhanThayDoi = tn?.NhanThayDoi ?? "";
                ga.CoLoiTaiGa = tn?.CoLoi == true || vuotDai;

                var dong = new List<string> { $"{ga.TenGa} ({ga.MaGaCode}) — ga dừng {k + 1}/{_hanhTrinh.Count}" };
                dong.Add(ga.LaGaDau ? $"Xuất phát {ga.GioDiKeHoach:HH:mm dd/MM}"
                       : ga.LaGaCuoi ? $"Về đích {ga.GioDenKeHoach:HH:mm dd/MM}"
                       : $"Đến {ga.GioDenKeHoach:HH:mm dd/MM} · đi {ga.GioDiKeHoach:HH:mm} · đỗ {ga.SoPhutDo} phút");
                dong.Add($"Giới hạn dài tại ga: {ga.GioiHanChieuDaiM:N0} m ({ga.NguonGioiHan})");
                if (vuotDai) dong.Add("⚠ Đoàn tàu dài hơn đường ray tại ga này");

                if (tn != null)
                {
                    if (tn.Cat.Count > 0) dong.Add($"Cắt {tn.Cat.Count} toa: {DanhSachSoHieu(tn.Cat)}");
                    if (tn.Noi.Count > 0) dong.Add($"Nối {tn.Noi.Count} toa: {DanhSachSoHieu(tn.Noi)}");
                    if (!tn.DuThoiGian) dong.Add($"⚠ Đỗ {ga.SoPhutDo} phút, dưới {phutToiThieu} phút cần cho cắt / nối");
                    if (!tn.DungViTri) dong.Add("⚠ Nhóm toa cắt / nối phải liền khối ở cuối đoàn hoặc ngay sau đầu máy");
                }

                if (ga.LaGaCuoi)
                {
                    dong.Add("Ga cuối: không có khu đoạn sau");
                }
                else
                {
                    var kd = kq.KhuDoan[k];
                    ga.NhanDoanSau = $"{kd.SoToa} toa";
                    ga.DoanSauDangChon = k == _viTriXem;
                    ga.DoanSauCoLoi = kd.CoLoi;

                    dong.Add($"Đoạn {kd.Ten}: {kd.SoToa} toa · {kd.ChieuDaiM:N1} m · {kd.TrongLuongTan:N1} t");
                    dong.Add("Bấm để xem đoàn tàu khi rời ga này");
                }

                ga.MoTaChiTiet = string.Join("\n", dong);
            }
        }

        private void CapNhatTieuDe(int soToaXem)
        {
            if (!CoHanhTrinh)
            {
                txtDemToaDoan.Text = $"{soToaXem} toa";
                txtGoiYDoanTau.Text = "Kéo để đổi vị trí  ·  nhấp đúp hoặc kéo xuống bãi để tháo toa";
                txtOCho.Text = _doanTau.Count == 0 ? "Kéo toa từ bãi vào đây" : "Thả để móc vào cuối đoàn";
                txtTieuDeSoToa.Text = "SỐ TOA TRONG ĐOÀN";
                txtTieuDeChieuDai.Text = "CHIỀU DÀI ĐOÀN TÀU";
                txtTieuDeTrongLuong.Text = "TRỌNG LƯỢNG KÉO (TOÀN TẢI)";
                txtTieuDeSucChua.Text = "SỨC CHỨA HÀNH KHÁCH";
                return;
            }

            var ga = GaDangXem!;
            string doan = TenDoan(_viTriXem, _viTriXem + 1);
            int ngoaiDoan = _doanTau.Count - soToaXem;

            txtDemToaDoan.Text = ngoaiDoan > 0 ? $"{soToaXem} toa · {ngoaiDoan} ngoài đoạn" : $"{soToaXem} toa";
            txtGoiYDoanTau.Text = _viTriXem == 0
                ? $"Đang xem đoạn {doan} (rời ga đầu)  ·  nhấp đúp hoặc kéo xuống bãi để tháo toa"
                : $"Đang xem đoạn {doan}  ·  kéo toa từ bãi vào = nối tại {ga.MaGaCode}  ·  kéo toa ra = cắt tại {ga.MaGaCode}";
            txtOCho.Text = _viTriXem > 0
                ? $"Thả để nối tại {ga.MaGaCode}"
                : _doanTau.Count == 0 ? "Kéo toa từ bãi vào đây" : "Thả để móc vào cuối đoàn";

            txtTieuDeSoToa.Text = $"SỐ TOA · {doan}";
            txtTieuDeChieuDai.Text = $"CHIỀU DÀI · {doan}";
            txtTieuDeTrongLuong.Text = $"TRỌNG LƯỢNG KÉO · {doan}";
            txtTieuDeSucChua.Text = $"SỨC CHỨA · {doan}";
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
            if (_doanTau.Count == 0)
            {
                ThongBaoDialog.CanhBao("Đoàn tàu chưa có toa nào. Hãy kéo toa từ bãi vào đoàn trước khi xác nhận.",
                                       "Chưa lập tàu", this);
                return;
            }

            var kq = KiemTraHienTai();
            var loi = kq.HangMuc.Where(h => h.MucDo == MucDoKiemTra.Loi).ToList();
            if (loi.Count > 0)
            {
                ThongBaoDialog.CanhBao(
                    $"Chưa thể xác nhận lập tàu vì còn {loi.Count} hạng mục không đạt:\n\n" +
                    string.Join("\n", loi.Select(h => $"• {h.Ten}")) +
                    "\n\nHãy điều chỉnh đoàn tàu theo gợi ý ở bảng kiểm tra an toàn.",
                    "Đoàn tàu chưa đạt", this);
                return;
            }

            // Ghi CSDL: Service kiem lai toan bo trong giao dich (nguoi khac vua sua, chuyen vua chay...)
            (KetQuaLuuLapTauDTO? KetQua, string? Loi) ketQuaLuu;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                ketQuaLuu = _dauVao.Luu(TaoPhuongAn());
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

            if (ketQuaLuu.Loi != null)
            {
                ThongBaoDialog.CanhBao(ketQuaLuu.Loi, "Chưa lưu được phương án lập tàu", this);
                return;
            }

            KetQua = ketQuaLuu.KetQua;
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
