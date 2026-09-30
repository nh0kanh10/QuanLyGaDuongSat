using GUI.ViewModels.KyThuat;
using GUI.Helpers;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // DIALOG THEM / SUA HO SO NHAN VIEN (nhansu.NhanVien)
    //
    // Kiem tra theo kieu cot + UNIQUE MaNVCode; hang bang lai bat buoc voi lai tau /
    // phu lai (quy uoc giao dien). Ma NV do he thong cap theo chuc danh (LT_001,
    // PL_001 ... - MaNhanVienTheoChucDanh, 29/09): them moi lay so ke tiep, sua ma
    // doi chuc danh thi cap ma moi. Khi sua: bao truoc kip chua nhan ban se hong neu
    // doi chuc danh / bang lai / han kham / cho nghi viec.
    // =========================================================================
    public partial class NhanVienDialog : Window
    {
        private static readonly Regex MaHopLe = new(@"^[A-Za-z0-9_\-]+$");
        private static readonly Regex ChiSo = new(@"^[0-9]+$");

        private readonly NhanVienHienThi? _banGoc;   // null = them moi
        private readonly DuLieuNhanSu _duLieu;
        private readonly bool _dangKhoiTao;

        public NhanVienHienThi? KetQua { get; private set; }

        public NhanVienDialog(NhanVienHienThi? banGoc, DuLieuNhanSu duLieu)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;
            _duLieu = duLieu;

            cboChucDanh.ItemsSource = DanhMucMauNhanSu.ChucDanhGoiY;
            cboHangBang.ItemsSource = DanhMucMauNhanSu.HangBangLaiGoiY;
            cboDonVi.ItemsSource = duLieu.NhanVien.Select(n => n.DonViChuQuan)
                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                         .Distinct().OrderBy(s => s).ToList();

            if (banGoc == null) KhoiTaoThemMoi();
            else KhoiTaoCapNhat(banGoc);

            _dangKhoiTao = false;
            CapNhatTheoChucDanh();
            CapNhatMa();
            CapNhatTomTat();

            Loaded += (_, _) =>
            {
                txtHoTen.Focus();
                txtHoTen.SelectAll();
            };
        }

        private void KhoiTaoThemMoi()
        {
            txtTieuDe.Text = "THÊM HỒ SƠ NHÂN VIÊN";
            txtTieuDePhu.Text = "Ban lái máy, tổ tàu và nhân viên phục vụ trên tàu";
            icoTieuDe.Symbol = SymbolRegular.PersonAdd24;
            txtNutLuu.Text = "Lưu Hồ Sơ";

            ChonTrangThai(NhanVienHienThi.SanSang);
        }

        private void KhoiTaoCapNhat(NhanVienHienThi nv)
        {
            txtTieuDe.Text = $"CẬP NHẬT HỒ SƠ {nv.MaNVCode} — {nv.HoTen}";
            txtTieuDePhu.Text = "Mã nhân viên theo chức danh; đổi chức danh sẽ được cấp mã mới";
            icoTieuDe.Symbol = SymbolRegular.PersonEdit24;
            txtNutLuu.Text = "Lưu Thay Đổi";

            txtMa.Text = nv.MaNVCode;
            txtHoTen.Text = nv.HoTen;
            txtSoDienThoai.Text = nv.SoDienThoai;
            dpHanKham.SelectedDate = nv.HanKhamSucKhoe;
            cboChucDanh.Text = nv.ChucDanh;
            cboHangBang.SelectedItem = DanhMucMauNhanSu.HangBangLaiGoiY.FirstOrDefault(h => h == nv.HangBangLai);
            cboDonVi.Text = nv.DonViChuQuan;
            ChonTrangThai(nv.TrangThai);
        }

        // Ma nhan vien cap theo chuc danh. Them moi: so ke tiep cua tien to. Sua: giu ma cu
        // neu con dung tien to cua chuc danh, khong thi cap ma moi (ma cu khong dung lai).
        private void CapNhatMa()
        {
            string ma;
            string? ghiChu = null;
            string tienTo = MaNhanVienTheoChucDanh.TienTo(ChucDanh);

            if (ChucDanh.Length == 0)
            {
                ma = _banGoc?.MaNVCode ?? string.Empty;
                if (_banGoc == null) ghiChu = "Chọn chức danh để hệ thống cấp mã.";
            }
            else if (_banGoc != null && MaNhanVienTheoChucDanh.KhopChucDanh(_banGoc.MaNVCode, ChucDanh))
            {
                ma = _banGoc.MaNVCode;
            }
            else
            {
                ma = MaNhanVienTheoChucDanh.MaKeTiep(ChucDanh, MaDaCap());
                if (_banGoc == null)
                    ghiChu = tienTo == MaNhanVienTheoChucDanh.TienToKhac
                        ? "Cấp tự động. Chức danh ngoài danh mục dùng tiền tố NV."
                        : $"Cấp tự động theo chức danh: {tienTo} = {ChucDanh.ToLower()}.";
                else
                    ghiChu = string.Equals(_banGoc.ChucDanh.Trim(), ChucDanh, StringComparison.OrdinalIgnoreCase)
                        ? $"Mã cũ chưa theo chức danh nên cấp mã mới: {_banGoc.MaNVCode} → {ma}."
                        : $"Đổi chức danh nên cấp mã mới: {_banGoc.MaNVCode} → {ma}. Mã cũ không dùng lại.";
            }

            txtMa.Text = ma;
            txtLoiMa.Text = ghiChu ?? string.Empty;
            txtLoiMa.Foreground = Mau(_banGoc != null && ma != _banGoc.MaNVCode ? "#B45309" : "#64748B");
        }

        // Ma da cap: du lieu dang hien (ke ca ho so them / sua tam) + du lieu goc, de ma cu
        // cua nguoi vua doi chuc danh khong bi cap lai cho nguoi khac
        private IEnumerable<string> MaDaCap()
            => _duLieu.NhanVien.Select(n => n.MaNVCode)
                      .Concat(DanhMucMauNhanSu.LayNhanVien().Select(n => n.MaNVCode));

        private void ChonTrangThai(string trangThai)
        {
            cboTrangThai.SelectedItem = cboTrangThai.Items.OfType<ComboBoxItem>()
                                                    .FirstOrDefault(i => (string)i.Tag == trangThai)
                                        ?? cboTrangThai.Items[0];
        }

        private string ChucDanh => cboChucDanh.Text?.Trim() ?? string.Empty;
        private string DonVi => cboDonVi.Text?.Trim() ?? string.Empty;
        private string TrangThai => (cboTrangThai.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? NhanVienHienThi.SanSang;
        private bool CanBangLai => ChucDanh is "Lái tàu" or "Phụ lái";

        // =====================================================================
        // CAP NHAT TRUC TIEP
        // =====================================================================

        private void TruongNhap_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            if (sender == txtMa) txtLoiMa.Text = "";
            if (sender == txtHoTen) txtLoiHoTen.Text = "";
            if (sender == txtSoDienThoai) txtLoiSoDienThoai.Text = "";
            CapNhatTomTat();
        }

        private void TxtSoDienThoai_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!ChiSo.IsMatch(e.Text)) e.Handled = true;
        }

        private void DpHanKham_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiHanKham.Text = "";
            CapNhatTomTat();
        }

        private void CboChucDanh_Changed(object sender, RoutedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiChucDanh.Text = "";
            CapNhatTheoChucDanh();
            CapNhatMa();
            CapNhatTomTat();
        }

        private void CboHangBang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiHangBang.Text = "";
            CapNhatTomTat();
        }

        private void CboDonVi_Changed(object sender, RoutedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiDonVi.Text = "";
            CapNhatTomTat();
        }

        private void CboTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiTrangThai.Text = "";
            CapNhatTomTat();
        }

        // Hang bang lai chi dung cho ban lai may
        private void CapNhatTheoChucDanh()
        {
            bool can = CanBangLai;
            cboHangBang.IsEnabled = can;
            if (!can) cboHangBang.SelectedItem = null;

            txtNhanHangBang.Inlines.Clear();
            txtNhanHangBang.Inlines.Add(new System.Windows.Documents.Run(can ? "Hạng bằng lái" : "Hạng bằng lái (không áp dụng)"));
            if (can) txtNhanHangBang.Inlines.Add(new System.Windows.Documents.Run(" *") { Foreground = Mau("#DC2626") });
        }

        private NhanVienHienThi TaoTuForm()
        {
            var nv = _banGoc?.SaoChep() ?? new NhanVienHienThi();
            nv.MaNVCode = txtMa.Text.Trim();
            nv.HoTen = txtHoTen.Text.Trim();
            nv.SoDienThoai = txtSoDienThoai.Text.Trim();
            nv.ChucDanh = ChucDanh;
            nv.HangBangLai = CanBangLai ? cboHangBang.SelectedItem as string : null;
            nv.HanKhamSucKhoe = dpHanKham.SelectedDate ?? DateTime.Today;
            nv.DonViChuQuan = DonVi;
            nv.TrangThai = TrangThai;
            return nv;
        }

        private void CapNhatTomTat()
        {
            pnlTomTat.Children.Clear();

            string viTri = ChucDanh switch
            {
                "Lái tàu" or "Phụ lái" => $"{ChucDanh} — đầu máy {(cboHangBang.SelectedItem as string) ?? "(chưa chọn hạng bằng)"}",
                "Trưởng tàu" => "Trưởng tàu",
                "" => "(chưa chọn chức danh)",
                _ => "Không thuộc kíp lái — chỉ quản lý hồ sơ"
            };
            ThemDong("Vị trí trong kíp", viTri, null);

            if (dpHanKham.SelectedDate is DateTime han)
            {
                int soNgay = (han.Date - DateTime.Today).Days;
                string moTa = soNgay < 0 ? $"Đã hết hạn {-soNgay} ngày — không được phân công"
                            : soNgay <= QuyTacKipLai.SoNgayNhacHanKham ? $"Còn {soNgay} ngày — cần xếp lịch khám"
                            : $"Còn {soNgay} ngày";
                string mau = soNgay < 0 ? "#B91C1C" : soNgay <= QuyTacKipLai.SoNgayNhacHanKham ? "#B45309" : "#15803D";
                ThemDong("Hạn khám sức khỏe", moTa, mau);
            }
            else
            {
                ThemDong("Hạn khám sức khỏe", "Chưa nhập", "#94A3B8");
            }

            string? gaDongQuan = DanhMucMauNhanSu.GaDongQuan(DonVi)?.TenGa;
            ThemDong("Ga đóng quân (tham khảo)", gaDongQuan ?? "—", null, laDongCuoi: true);

            CapNhatAnhHuongKip();
        }

        // Khi sua: doi chieu lai cac kip chua nhan ban voi ho so moi
        private void CapNhatAnhHuongKip()
        {
            bdAnhHuong.Visibility = Visibility.Collapsed;
            if (_banGoc == null) return;

            var nvMoi = TaoTuForm();
            var kip = _duLieu.KipCuaNhanVien(_banGoc.MaNhanVien)
                             .Where(p => p.TrangThai == PhanCongKipHienThi.DaPhanCong && p.Chuyen.ConHieuLuc)
                             .ToList();

            var dong = new List<string>();
            foreach (var p in kip)
            {
                string vaiTro = p.VaiTroCua(_banGoc.MaNhanVien)!;
                var loiCu = DanhGiaKipLai.DoiChieu(_duLieu, _banGoc, vaiTro, p.Chuyen, p.MaGaNhanBan, p.MaGaBanGiao, p.MaPhanCong)
                                         .Where(d => d.LaLoi).Select(d => d.TenDieuKien).ToHashSet();
                var loiMoi = DanhGiaKipLai.DoiChieu(_duLieu, nvMoi, vaiTro, p.Chuyen, p.MaGaNhanBan, p.MaGaBanGiao, p.MaPhanCong)
                                          .Where(d => d.LaLoi && !loiCu.Contains(d.TenDieuKien))
                                          .ToList();
                if (loiMoi.Count > 0)
                    dong.Add($"• {p.Chuyen.SoHieuMacTau} {p.Chang} ({QuyTacKipLai.TenVaiTro(vaiTro).ToLower()}): {loiMoi[0].MoTa}");
            }

            if (dong.Count == 0) return;
            txtAnhHuong.Text = $"Với thông tin mới, {nvMoi.HoTen} sẽ không đủ điều kiện ở kíp chưa nhận ban:\n" +
                               string.Join("\n", dong) + "\nVẫn lưu được, nhưng phải thay người trong kíp.";
            bdAnhHuong.Visibility = Visibility.Visible;
        }

        private void ThemDong(string nhan, string giaTri, string? mau, bool laDongCuoi = false)
        {
            var hang = new Grid { MinHeight = 24 };
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var tbNhan = new TextBlock { Text = nhan, Style = (Style)FindResource("KtNhanThongSo") };
            var tbGiaTri = new TextBlock
            {
                Text = giaTri, Style = (Style)FindResource("KtGiaTriThongSo"),
                TextAlignment = TextAlignment.Left, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 4)
            };
            if (mau != null) tbGiaTri.Foreground = Mau(mau);
            Grid.SetColumn(tbGiaTri, 1);
            hang.Children.Add(tbNhan);
            hang.Children.Add(tbGiaTri);

            pnlTomTat.Children.Add(new Border
            {
                Child = hang,
                BorderBrush = Mau("#E2E8F0"),
                BorderThickness = new Thickness(0, 0, 0, laDongCuoi ? 0 : 1)
            });
        }

        // =====================================================================
        // LUU / HUY
        // =====================================================================

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!KiemTraHopLe()) return;
            KetQua = TaoTuForm();
            DialogResult = true;
        }

        private bool KiemTraHopLe()
        {
            bool hopLe = true;
            Control? oLoiDauTien = null;

            void BaoLoi(TextBlock tbLoi, string thongDiep, Control o)
            {
                tbLoi.Text = thongDiep;
                tbLoi.Foreground = Mau("#B91C1C");
                hopLe = false;
                oLoiDauTien ??= o;
            }

            // MaNVCode VARCHAR(20) NOT NULL UNIQUE - he thong cap theo chuc danh
            string ma = txtMa.Text.Trim();
            if (ma.Length == 0)
                BaoLoi(txtLoiMa, "Chọn chức danh để hệ thống cấp mã nhân viên.", cboChucDanh);
            else if (!MaHopLe.IsMatch(ma))
                BaoLoi(txtLoiMa, "Chỉ dùng chữ không dấu, chữ số, dấu _ hoặc - (cột VARCHAR).", txtMa);
            else if (_duLieu.NhanVien.Any(n => n.MaNhanVien != _banGoc?.MaNhanVien &&
                                                string.Equals(n.MaNVCode, ma, StringComparison.OrdinalIgnoreCase)))
                BaoLoi(txtLoiMa, $"Mã {ma} đã được dùng cho nhân viên khác (ràng buộc UNIQUE).", txtMa);

            // HoTen NVARCHAR(100) NOT NULL
            if (txtHoTen.Text.Trim().Length == 0)
                BaoLoi(txtLoiHoTen, "Vui lòng nhập họ và tên.", txtHoTen);

            // SoDienThoai VARCHAR(20) NOT NULL
            string sdt = txtSoDienThoai.Text.Trim();
            if (sdt.Length == 0)
                BaoLoi(txtLoiSoDienThoai, "Vui lòng nhập số điện thoại.", txtSoDienThoai);
            else if (!ChiSo.IsMatch(sdt) || !sdt.StartsWith('0') || sdt.Length is < 10 or > 11)
                BaoLoi(txtLoiSoDienThoai, "Số điện thoại gồm 10–11 chữ số, bắt đầu bằng 0.", txtSoDienThoai);

            // HanKhamSucKhoe DATE NOT NULL
            if (dpHanKham.SelectedDate == null)
                BaoLoi(txtLoiHanKham, "Vui lòng chọn hạn khám sức khỏe.", dpHanKham);

            // ChucDanh NVARCHAR(50) NOT NULL
            if (ChucDanh.Length == 0)
                BaoLoi(txtLoiChucDanh, "Vui lòng chọn hoặc nhập chức danh.", cboChucDanh);
            else if (ChucDanh.Length > 50)
                BaoLoi(txtLoiChucDanh, "Chức danh tối đa 50 ký tự.", cboChucDanh);

            // HangBangLai: bat buoc voi ban lai may (quy uoc giao dien, CSDL cho NULL)
            if (CanBangLai && cboHangBang.SelectedItem == null)
                BaoLoi(txtLoiHangBang, $"{ChucDanh} phải có hạng bằng lái (dòng đầu máy được lái).", cboHangBang);

            // DonViChuQuan NVARCHAR(100) NOT NULL
            if (DonVi.Length == 0)
                BaoLoi(txtLoiDonVi, "Vui lòng chọn hoặc nhập đơn vị chủ quản.", cboDonVi);
            else if (DonVi.Length > 100)
                BaoLoi(txtLoiDonVi, "Đơn vị chủ quản tối đa 100 ký tự.", cboDonVi);

            // Dang trong ca thi phai ban giao truoc khi cho nghi viec
            if (_banGoc != null && TrangThai == NhanVienHienThi.DaNghiViec)
            {
                var dangChay = _duLieu.KipCuaNhanVien(_banGoc.MaNhanVien)
                                      .FirstOrDefault(p => p.TrangThai == PhanCongKipHienThi.DangThucHien);
                if (dangChay != null)
                    BaoLoi(txtLoiTrangThai, $"Đang trong ca {dangChay.Chuyen.SoHieuMacTau} ({dangChay.Chang}). " +
                                            "Xác nhận bàn giao trước khi cho nghỉ việc.", cboTrangThai);
            }

            if (!hopLe) oLoiDauTien?.Focus();
            return hopLe;
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) => DialogResult = false;

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

        private static SolidColorBrush Mau(string ma) => (SolidColorBrush)new BrushConverter().ConvertFrom(ma)!;
    }
}
