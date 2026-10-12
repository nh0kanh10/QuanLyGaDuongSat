using GUI.ViewModels.KyThuat;
using GUI.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BUS.Services;
using DTO.Common;
using DTO.PhuongTien;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // DIALOG THEM MOI / CAP NHAT TOA XE TRONG DOI (phuongtien.ToaXe)
    //
    // - Chung loai lay tu CSDL (phuongtien.ChungLoaiToa); mau the + tu trong / tai trong
    //   goi y theo DanhMucMauPhuongTien.
    // - Kiem tra bang CUNG ham voi Service (PhuongTienService.KiemTraToaXe).
    // - Bam Luu: goi ham luu cua trang; Service tu choi thi hien ly do, giu dialog.
    // - Tai trong truc = (tu trong + tai trong) / so truc, doi chieu nguong QuyTacLapTau.
    // =========================================================================
    public partial class ToaXeDialog : Window
    {
        private readonly ToaXeHienThi? _banGoc;
        private readonly HashSet<string> _soHieuDaCo;
        private readonly Func<ToaXeHienThi, string?> _luu;
        private readonly bool _dangKhoiTao;

        // Ghi nho gia tri do he thong tu dien, de biet nguoi dung da tu sua hay chua
        private string _soHieuTuGoiY = "";
        private ChungLoaiToaMau? _chungLoaiTruoc;

        public ToaXeHienThi? KetQua { get; private set; }

        // soHieuDaCo: so hieu dang co trong doi, chi de goi y so ke tiep (Service moi la noi chan trung)
        public ToaXeDialog(ToaXeHienThi? banGoc, IReadOnlyList<ChungLoaiToaMau> chungLoai,
                           IEnumerable<string> soHieuDaCo, Func<ToaXeHienThi, string?> luu)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;
            _soHieuDaCo = new HashSet<string>(soHieuDaCo, StringComparer.OrdinalIgnoreCase);
            _luu = luu;

            SmartNumberFormatHelper.AttachDecimalKm(txtTuTrong, 0m, PhuongTienService.KhoiLuongToaToiDaTan);
            SmartNumberFormatHelper.AttachDecimalKm(txtTaiTrong, 0m, PhuongTienService.KhoiLuongToaToiDaTan);

            cboChungLoai.ItemsSource = chungLoai;
            txtNguongTruc.Text = $"ngưỡng {QuyTacLapTau.TaiTrongTrucToiDaTan:N1} t";

            if (banGoc == null)
                KhoiTaoThemMoi();
            else
                KhoiTaoCapNhat(banGoc, chungLoai);

            _chungLoaiTruoc = cboChungLoai.SelectedItem as ChungLoaiToaMau;
            _dangKhoiTao = false;
            CapNhatXemTruoc();

            Loaded += (_, _) =>
            {
                if (banGoc == null)
                {
                    txtSoHieu.Focus();
                    txtSoHieu.CaretIndex = txtSoHieu.Text.Length;
                }
                else
                {
                    txtTuTrong.Focus();
                    txtTuTrong.SelectAll();
                }
            };
        }

        private void KhoiTaoThemMoi()
        {
            txtTieuDe.Text = "THÊM MỚI TOA XE";
            txtTieuDePhu.Text = "Khai báo toa xe mới vào đội phương tiện";
            txtNutLuu.Text = "Thêm Toa Xe";

            cboChungLoai.SelectedIndex = cboChungLoai.Items.Count > 0 ? 0 : -1;
            if (cboChungLoai.SelectedItem is ChungLoaiToaMau cl)
            {
                DienThongSoMacDinh(cl);
                DienSoHieuGoiY(cl);
            }
            rdoSanSang.IsChecked = true;
        }

        private void KhoiTaoCapNhat(ToaXeHienThi tx, IReadOnlyList<ChungLoaiToaMau> chungLoai)
        {
            txtTieuDe.Text = $"CẬP NHẬT TOA XE — {tx.SoHieuToaXe}";
            txtTieuDePhu.Text = "Số hiệu và chủng loại là định danh nên không sửa sau khi đã tạo";
            txtNutLuu.Text = "Lưu Thay Đổi";

            cboChungLoai.SelectedItem = chungLoai.FirstOrDefault(c => c.MaChungLoai == tx.MaChungLoai);

            txtSoHieu.Text = tx.SoHieuToaXe;
            txtSoHieu.IsReadOnly = true;
            txtSoHieu.Background = (Brush)new BrushConverter().ConvertFrom("#F1F5F9")!;
            txtSoHieu.Foreground = (Brush)new BrushConverter().ConvertFrom("#475569")!;
            btnGoiYSoHieu.Visibility = Visibility.Collapsed;

            // Doi chung loai khi sua se lam lech tien to so hieu -> khoa lai (Service cung chan)
            cboChungLoai.IsEnabled = false;
            cboChungLoai.ToolTip = "Không đổi chủng loại của toa đã có. Hãy tạo toa mới nếu cần.";

            txtTuTrong.Text = FormatHelper.FormatKm(tx.TuTrongTan);
            txtTaiTrong.Text = FormatHelper.FormatKm(tx.TaiTrongToiDaTan);

            switch (tx.TrangThai)
            {
                case "DA_NOI": rdoDaNoi.IsChecked = true; break;
                case "BAO_DUONG": rdoBaoDuong.IsChecked = true; break;
                default: rdoSanSang.IsChecked = true; break;
            }
        }

        // =====================================================================
        // GOI Y SO HIEU & THONG SO MAC DINH
        // =====================================================================

        // So hieu trong ke tiep: NC-1xx, NML-2xx, BN-3xx ... (theo quy uoc seed data)
        private string TaoSoHieuGoiY(ChungLoaiToaMau cl)
        {
            string tienTo = $"{cl.MaChungLoaiCode}-";
            int coSo = (cl.MaChungLoai > 0 ? cl.MaChungLoai : 9) * 100;
            int lonNhat = coSo;

            foreach (string sh in _soHieuDaCo)
            {
                if (!sh.StartsWith(tienTo, StringComparison.OrdinalIgnoreCase)) continue;
                if (int.TryParse(sh[tienTo.Length..], out int so) && so > lonNhat)
                    lonNhat = so;
            }

            return $"{tienTo}{lonNhat + 1}";
        }

        private void DienSoHieuGoiY(ChungLoaiToaMau cl)
        {
            _soHieuTuGoiY = TaoSoHieuGoiY(cl);
            txtSoHieu.Text = _soHieuTuGoiY;
            txtSoHieu.CaretIndex = txtSoHieu.Text.Length;
            txtLoiSoHieu.Text = "";
        }

        private void DienThongSoMacDinh(ChungLoaiToaMau cl)
        {
            txtTuTrong.Text = FormatHelper.FormatKm(cl.TuTrongMacDinhTan);
            txtTaiTrong.Text = FormatHelper.FormatKm(cl.TaiTrongMacDinhTan);
            txtLoiTuTrong.Text = "";
            txtLoiTaiTrong.Text = "";
        }

        private void BtnGoiYSoHieu_Click(object sender, RoutedEventArgs e)
        {
            if (cboChungLoai.SelectedItem is ChungLoaiToaMau cl) DienSoHieuGoiY(cl);
            txtSoHieu.Focus();
        }

        private void BtnThongSoMacDinh_Click(object sender, RoutedEventArgs e)
        {
            if (cboChungLoai.SelectedItem is ChungLoaiToaMau cl) DienThongSoMacDinh(cl);
        }

        private void CboChungLoai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiChungLoai.Text = "";

            if (cboChungLoai.SelectedItem is not ChungLoaiToaMau cl) return;

            if (_banGoc == null)
            {
                // Chi thay so hieu neu nguoi dung chua tu go khac di
                string hienTai = txtSoHieu.Text.Trim();
                if (hienTai.Length == 0 || hienTai == _soHieuTuGoiY)
                    DienSoHieuGoiY(cl);

                // Chi thay tai trong neu dang la gia tri mac dinh cua chung loai truoc
                if (_chungLoaiTruoc == null || DangLaThongSoMacDinh(_chungLoaiTruoc))
                    DienThongSoMacDinh(cl);
            }

            _chungLoaiTruoc = cl;
            CapNhatXemTruoc();
        }

        private bool DangLaThongSoMacDinh(ChungLoaiToaMau cl)
            => FormatHelper.TryParseDecimal(txtTuTrong.Text, out decimal tu) && tu == cl.TuTrongMacDinhTan
            && FormatHelper.TryParseDecimal(txtTaiTrong.Text, out decimal tai) && tai == cl.TaiTrongMacDinhTan;

        // =====================================================================
        // XEM TRUOC
        // =====================================================================

        private void TruongNhap_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            if (sender == txtSoHieu) txtLoiSoHieu.Text = "";
            if (sender == txtTuTrong) txtLoiTuTrong.Text = "";
            if (sender == txtTaiTrong) txtLoiTaiTrong.Text = "";
            CapNhatXemTruoc();
        }

        private void CapNhatXemTruoc()
        {
            var conv = new BrushConverter();
            if (cboChungLoai.SelectedItem is not ChungLoaiToaMau cl) return;

            // --- The chung loai ---
            txtMaChungLoai.Text = cl.MaChungLoaiCode;
            bdMaChungLoai.Background = (Brush)conv.ConvertFrom(cl.MauNen)!;
            bdMaChungLoai.BorderBrush = (Brush)conv.ConvertFrom(cl.MauVien)!;
            txtMaChungLoai.Foreground = (Brush)conv.ConvertFrom(cl.MauChu)!;
            txtNhomToa.Text = cl.LaToaHang ? "TOA HÀNG" : $"TOA KHÁCH · {cl.SucChuaThamKhao} chỗ (tham khảo)";
            txtMoTaChungLoai.Text = cl.TenMoTa;

            pnlThongSoChungLoai.Children.Clear();
            ThemDongThongSo("Chiều dài chuẩn", $"{cl.ChieuDaiChuanM:N1} m");
            ThemDongThongSo("Số trục", $"{cl.SoTruc} trục");
            ThemDongThongSo("Tự trọng tham khảo", $"{cl.TuTrongMacDinhTan:N1} t");
            ThemDongThongSo("Tải trọng tham khảo", $"{cl.TaiTrongMacDinhTan:N1} t", laDongCuoi: true);

            // --- Tai trong truc ---
            decimal tuTrong = FormatHelper.TryParseDecimal(txtTuTrong.Text, out decimal a) ? a : 0m;
            decimal taiTrong = FormatHelper.TryParseDecimal(txtTaiTrong.Text, out decimal b) ? b : 0m;
            decimal toanTai = tuTrong + taiTrong;
            decimal truc = PhuongTienService.TinhTaiTrongTruc(tuTrong, taiTrong, cl.SoTruc);
            decimal nguong = QuyTacLapTau.TaiTrongTrucToiDaTan;
            double tyLe = nguong > 0 ? Math.Min(100.0, (double)(truc / nguong) * 100.0) : 0.0;

            string mau = truc > nguong ? "#B91C1C" : tyLe >= 85 ? "#B45309" : "#15803D";

            txtTaiTrongTruc.Text = truc.ToString("N2");
            txtTaiTrongTruc.Foreground = (Brush)conv.ConvertFrom(mau)!;
            bdVachTruc.Background = (Brush)conv.ConvertFrom(mau)!;
            colTruc.Width = new GridLength(tyLe, GridUnitType.Star);
            colTrucConLai.Width = new GridLength(100 - tyLe, GridUnitType.Star);
            txtToanTai.Text = $"{toanTai:N1} t";

            txtMoTaTruc.Text = truc > nguong
                ? $"Vượt ngưỡng {nguong:N1} t/trục khi chở đầy. Lập tàu sẽ không cho đưa toa này vào đoàn."
                : tyLe >= 85
                    ? "Sát ngưỡng tải trọng trục cho phép."
                    : "Nằm trong ngưỡng tải trọng trục cho phép.";
        }

        private void ThemDongThongSo(string nhan, string giaTri, bool laDongCuoi = false)
        {
            var hang = new Grid { Height = 22 };
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tbNhan = new TextBlock { Text = nhan, Style = (Style)FindResource("KtNhanThongSo") };
            var tbGiaTri = new TextBlock { Text = giaTri, Style = (Style)FindResource("KtGiaTriThongSo") };
            Grid.SetColumn(tbGiaTri, 1);
            hang.Children.Add(tbNhan);
            hang.Children.Add(tbGiaTri);

            pnlThongSoChungLoai.Children.Add(new Border
            {
                Child = hang,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#E2E8F0")!,
                BorderThickness = new Thickness(0, 0, 0, laDongCuoi ? 0 : 1)
            });
        }

        // =====================================================================
        // LUU / HUY
        // =====================================================================

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            var cl = cboChungLoai.SelectedItem as ChungLoaiToaMau;
            bool docDuocTuTrong = FormatHelper.TryParseDecimal(txtTuTrong.Text, out decimal tuTrong);
            bool docDuocTaiTrong = FormatHelper.TryParseDecimal(txtTaiTrong.Text, out decimal taiTrong);

            var kq = _banGoc?.SaoChep() ?? new ToaXeHienThi();
            kq.SoHieuToaXe = _banGoc?.SoHieuToaXe ?? txtSoHieu.Text.Trim().ToUpperInvariant();
            kq.TuTrongTan = docDuocTuTrong ? tuTrong : -1m;
            kq.TaiTrongToiDaTan = docDuocTaiTrong ? taiTrong : -1m;
            kq.TrangThai = rdoDaNoi.IsChecked == true ? "DA_NOI"
                         : rdoBaoDuong.IsChecked == true ? "BAO_DUONG"
                         : "SAN_SANG";
            if (cl != null)
            {
                kq.MaChungLoai = cl.MaChungLoai;
                kq.MaChungLoaiCode = cl.MaChungLoaiCode;
                kq.TenMoTa = cl.TenMoTa;
                kq.ChieuDaiChuanM = cl.ChieuDaiChuanM;
                kq.SoTruc = cl.SoTruc;
            }

            // Cung bo kiem tra voi Service: bao loi canh tung o
            if (HienLoi(PhuongTienService.KiemTraToaXe(kq.ToET(), cl?.MaChungLoaiCode, laThemMoi: _banGoc == null)))
                return;

            string? loi = _luu(kq);
            if (loi != null)
            {
                ThongBaoDialog.CanhBao(loi, "Chưa lưu được toa xe", this);
                return;
            }

            KetQua = kq;
            DialogResult = true;
        }

        // Hien loi canh tung o; tra ve true neu co loi
        private bool HienLoi(List<LoiNhapLieuDTO> dsLoi)
        {
            txtLoiChungLoai.Text = txtLoiSoHieu.Text = txtLoiTuTrong.Text = txtLoiTaiTrong.Text = "";
            Control? oLoiDauTien = null;

            foreach (var l in dsLoi)
            {
                (TextBlock? tb, Control? o) = l.Truong switch
                {
                    "ChungLoai" => (txtLoiChungLoai, (Control)cboChungLoai),
                    "SoHieu" => (txtLoiSoHieu, txtSoHieu),
                    "TuTrong" => (txtLoiTuTrong, txtTuTrong),
                    "TaiTrong" => (txtLoiTaiTrong, txtTaiTrong),
                    _ => ((TextBlock?)null, (Control?)null)
                };

                if (tb == null)
                {
                    ThongBaoDialog.CanhBao(l.ThongDiep, "Dữ liệu chưa hợp lệ", this);
                    return true;
                }
                if (tb.Text.Length == 0) tb.Text = l.ThongDiep;
                oLoiDauTien ??= o;
            }

            oLoiDauTien?.Focus();
            return dsLoi.Count > 0;
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                BtnLuu_Click(this, new RoutedEventArgs());
            }
        }
    }
}
