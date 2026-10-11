using BUS.Services;
using DTO.NhanSu;
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
    //
    // Tu 11/10: ma that do NhanSuService cap khi luu (o ma chi hien ma du kien); hang bang
    // lai lay tu phuongtien.DongDauMay; kiem tra tung o dung NhanSuService.KiemTraNhanVien;
    // luu goi ham _luu (Service) - loi thi bao va giu dialog mo. "Dang lam nhiem vu" do nhan
    // ban dat, khong chon tay (Q7).
    // =========================================================================
    public partial class NhanVienDialog : Window
    {
        private static readonly Regex MaHopLe = new(@"^[A-Za-z0-9_\-]+$");
        private static readonly Regex ChiSo = new(@"^[0-9]+$");

        private readonly NhanVienHienThi? _banGoc;   // null = them moi
        private readonly DuLieuNhanSu _duLieu;
        private readonly Func<NhanVienHienThi, string?> _luu;
        private readonly bool _dangKhoiTao;

        public NhanVienHienThi? KetQua { get; private set; }

        public NhanVienDialog(NhanVienHienThi? banGoc, DuLieuNhanSu duLieu, Func<NhanVienHienThi, string?> luu)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;
            _duLieu = duLieu;
            _luu = luu;

            cboChucDanh.ItemsSource = QuyTacKipLai.ChucDanhGoiY;
            cboHangBang.ItemsSource = duLieu.HangBangLai;
            cboDonVi.ItemsSource = duLieu.NhanVien.Select(n => n.DonViChuQuan)
                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                         .Distinct().OrderBy(s => s).ToList();

            // Q7: "Dang lam nhiem vu" chi hien khi ho so dang o trang thai do (do nhan ban dat)
            var mucDangLam = cboTrangThai.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == NhanVienHienThi.DangLam);
            if (mucDangLam != null && banGoc?.TrangThai != NhanVienHienThi.DangLam) cboTrangThai.Items.Remove(mucDangLam);

            if (banGoc == null) KhoiTaoThemMoi();
            else KhoiTaoCapNhat(banGoc);

            // Dang trong ca (kip dang thuc hien): trang thai doi khi xac nhan ban giao
            var dangChay = banGoc == null ? null
                : duLieu.KipCuaNhanVien(banGoc.MaNhanVien).FirstOrDefault(p => p.TrangThai == PhanCongKipHienThi.DangThucHien);
            if (dangChay != null)
            {
                cboTrangThai.IsEnabled = false;
                txtLoiTrangThai.Text = $"Đang trong ca {dangChay.Chuyen.SoHieuMacTau} ({dangChay.Chang}): trạng thái đổi khi xác nhận bàn giao.";
                txtLoiTrangThai.Foreground = Mau("#64748B");
            }

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
            cboHangBang.SelectedItem = _duLieu.HangBangLai.FirstOrDefault(h => h == nv.HangBangLai);
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
                        ? "Mã dự kiến, cấp chính thức khi lưu. Chức danh ngoài danh mục dùng tiền tố NV."
                        : $"Mã dự kiến theo chức danh ({tienTo} = {ChucDanh.ToLower()}), hệ thống cấp chính thức khi lưu.";
                else
                    ghiChu = string.Equals(_banGoc.ChucDanh.Trim(), ChucDanh, StringComparison.OrdinalIgnoreCase)
                        ? $"Mã cũ chưa theo chức danh nên cấp mã mới khi lưu: {_banGoc.MaNVCode} → {ma} (dự kiến)."
                        : $"Đổi chức danh nên cấp mã mới khi lưu: {_banGoc.MaNVCode} → {ma} (dự kiến).";
            }

            txtMa.Text = ma;
            txtLoiMa.Text = ghiChu ?? string.Empty;
            txtLoiMa.Foreground = Mau(_banGoc != null && ma != _banGoc.MaNVCode ? "#B45309" : "#64748B");
        }

        // Ma da cap theo anh chup luc mo dialog (ma that do Service cap trong giao dich khi luu)
        private IEnumerable<string> MaDaCap() => _duLieu.NhanVien.Select(n => n.MaNVCode);

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

            string? gaDongQuan = _duLieu.GaDongQuan(DonVi)?.TenGa;
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

            var kq = TaoTuForm();
            string? loi = _luu(kq);
            if (loi != null)
            {
                ThongBaoDialog.CanhBao(loi, "Chưa lưu được hồ sơ nhân viên", this);
                return;
            }

            KetQua = kq;
            DialogResult = true;
        }

        // Cung bo kiem tra voi NhanSuService (bao loi canh tung o); Service kiem lai khi luu
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

            // MaNVCode: he thong cap theo chuc danh (Service cap ma that khi luu)
            string ma = txtMa.Text.Trim();
            if (ma.Length == 0)
                BaoLoi(txtLoiMa, "Chọn chức danh để hệ thống cấp mã nhân viên.", cboChucDanh);
            else if (!MaHopLe.IsMatch(ma))
                BaoLoi(txtLoiMa, "Chỉ dùng chữ không dấu, chữ số, dấu _ hoặc - (cột VARCHAR).", txtMa);

            if (dpHanKham.SelectedDate == null)
                BaoLoi(txtLoiHanKham, "Vui lòng chọn hạn khám sức khỏe.", dpHanKham);

            foreach (var l in NhanSuService.KiemTraNhanVien(TaoTuForm(), _duLieu.HangBangLai))
            {
                switch (l.Truong)
                {
                    case "HoTen": BaoLoi(txtLoiHoTen, l.ThongDiep, txtHoTen); break;
                    case "SoDienThoai": BaoLoi(txtLoiSoDienThoai, l.ThongDiep, txtSoDienThoai); break;
                    case "HanKhamSucKhoe": if (dpHanKham.SelectedDate != null) BaoLoi(txtLoiHanKham, l.ThongDiep, dpHanKham); break;
                    case "ChucDanh": BaoLoi(txtLoiChucDanh, l.ThongDiep, cboChucDanh); break;
                    case "HangBangLai": BaoLoi(txtLoiHangBang, l.ThongDiep, cboHangBang); break;
                    case "DonViChuQuan": BaoLoi(txtLoiDonVi, l.ThongDiep, cboDonVi); break;
                    default: BaoLoi(txtLoiTrangThai, l.ThongDiep, cboTrangThai); break;
                }
            }

            // Q7 + R13: dang trong ca thi khong doi trang thai; "Dang lam" khong chon tay
            if (cboTrangThai.IsEnabled && NhanSuService.KiemTraTrangThai(_banGoc, TrangThai, _duLieu) is { } loiTrangThai)
                BaoLoi(txtLoiTrangThai, loiTrangThai, cboTrangThai);

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
