using BUS.Services;
using DTO.BaoTri;
using GUI.ViewModels.KyThuat;
using GUI.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DTO.Common;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // DIALOG LAP BIEN BAN KHAM XE KY THUAT (baotri.KhamXeKyThuat)
    //
    // Tu 24/10: chi LAP MOI (bien ban da lap khong sua - sai thi lap bien ban moi). Kiem tra
    // truong nhap bang QuyTacBaoTri.KiemTraKhamXe (cung ham Service dung), roi goi ham luu
    // cua trang (BaoTriService.LapBienBan): loi tu Service thi bao va giu dialog mo.
    // Ket luan "du dieu kien xuat ben" tinh dung cong thuc cot PERSISTED
    // DuDieuKienXuatBen (NguongKhamXe), xem truoc ngay khi go.
    // =========================================================================
    public partial class KhamXeDialog : Window
    {
        private readonly BaoTriDuLieu _duLieu;
        private readonly Func<KhamXeDTO, string?> _luu;
        private readonly bool _dangKhoiTao;

        // maDoanTauMacDinh / maNguoiKhamMacDinh: chon san khi lap tu the doan tau hoac "Kham lai"
        public KhamXeDialog(BaoTriDuLieu duLieu, Func<KhamXeDTO, string?> luu,
                            int? maDoanTauMacDinh = null, int? maNguoiKhamMacDinh = null)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _duLieu = duLieu;
            _luu = luu;

            ONhapSoThucHelper.Gan(txtApLuc, 1);
            ONhapSoThucHelper.Gan(txtSutAp, 2);
            ONhapSoThucHelper.Gan(txtChiPhi, 0, coDinhSoLe: false);

            var dsDoanTau = duLieu.DoanTauDangDieuHanh;
            cboDoanTau.ItemsSource = dsDoanTau;
            cboNguoiKham.ItemsSource = duLieu.NguoiKyThuat;

            txtTieuDe.Text = "LẬP BIÊN BẢN KHÁM XE KỸ THUẬT";
            txtTieuDePhu.Text = "Thử hãm gió, kiểm tra cấp nước và vệ sinh trước khi xuất bến";
            txtNutLuu.Text = "Lưu Biên Bản";
            txtThoiDiemKham.Text = DateTime.Now.ToString("HH:mm dd/MM/yyyy");
            txtGoiYThoiDiem.Text = "Giờ máy chủ lúc lưu";

            cboDoanTau.SelectedItem = maDoanTauMacDinh.HasValue ? dsDoanTau.FirstOrDefault(d => d.MaDoanTau == maDoanTauMacDinh) : null;
            cboNguoiKham.SelectedItem = duLieu.TimNguoiKyThuat(maNguoiKhamMacDinh) ?? duLieu.NguoiKyThuat.FirstOrDefault();
            if (duLieu.NguoiKyThuat.Count == 0)
                txtLoiNguoiKham.Text = "Chưa có tài khoản kỹ thuật (vai trò KY_THUAT) đang hoạt động để ký biên bản.";

            // Kham lai thuong da xu ly xong phan phuc vu, nhung van de nguoi kham tu tick
            chkCapNuoc.IsChecked = false;
            chkXaVeSinh.IsChecked = false;
            txtChiPhi.Text = "0";

            _dangKhoiTao = false;
            CapNhatXemTruoc();

            Loaded += (_, _) =>
            {
                Control oDau = cboDoanTau.SelectedItem == null ? cboDoanTau : txtApLuc;
                oDau.Focus();
                if (oDau is TextBox tb) tb.SelectAll();
            };
        }

        private static decimal? LaySo(TextBox tb) => ONhapSoThucHelper.Lay(tb);

        // =====================================================================
        // XEM TRUOC TRUC TIEP
        // =====================================================================

        private void TruongNhap_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangKhoiTao) return;

            if (sender == txtApLuc) txtLoiApLuc.Text = "";
            if (sender == txtSutAp) txtLoiSutAp.Text = "";
            if (sender == txtGhiChu) txtLoiGhiChu.Text = "";
            if (sender == txtChiPhi) txtLoiChiPhi.Text = "";

            CapNhatXemTruoc();
        }

        private void HangMuc_Changed(object sender, RoutedEventArgs e)
        {
            if (_dangKhoiTao) return;
            CapNhatXemTruoc();
        }

        private void CboDoanTau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiDoanTau.Text = "";
            CapNhatXemTruoc();
        }

        private void CboNguoiKham_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiNguoiKham.Text = "";
        }

        private void CapNhatXemTruoc()
        {
            decimal? apLuc = LaySo(txtApLuc);
            decimal? sutAp = LaySo(txtSutAp);
            bool nuoc = chkCapNuoc.IsChecked == true;
            bool veSinh = chkXaVeSinh.IsChecked == true;

            // --- Doi chieu hang muc ---
            var hangMuc = NguongKhamXe.TaoHangMuc(apLuc, sutAp, nuoc, veSinh);
            icHangMuc.ItemsSource = hangMuc;

            // --- Ket luan ---
            int soDat = hangMuc.Count(h => h.Dat == true);
            if (!apLuc.HasValue || !sutAp.HasValue)
            {
                DatKetLuan(null, "CHỜ NHẬP KẾT QUẢ", $"Đã đạt {soDat}/4 hạng mục · còn thiếu số đo thử hãm");
            }
            else if (NguongKhamXe.DuDieuKienXuatBen(apLuc.Value, sutAp.Value, nuoc, veSinh))
            {
                DatKetLuan(true, "ĐỦ ĐIỀU KIỆN XUẤT BẾN", "Đạt 4/4 hạng mục kiểm tra");
            }
            else
            {
                DatKetLuan(false, "KHÔNG ĐỦ ĐIỀU KIỆN XUẤT BẾN", $"Đạt {soDat}/4 hạng mục · cần xử lý trước khi cho tàu chạy");
            }

            // --- Thang ap luc ---
            double phanTram = apLuc.HasValue ? (double)Math.Min(apLuc.Value, NguongKhamXe.ApLucHamGioiHanBar) * 10.0 : 0;
            colApLucDat.Width = new GridLength(phanTram, GridUnitType.Star);
            colApLucCon.Width = new GridLength(100 - phanTram, GridUnitType.Star);
            string mauVach = !apLuc.HasValue ? "#94A3B8" : apLuc >= NguongKhamXe.ApLucHamToiThieuBar ? "#16A34A" : "#DC2626";
            bdApLucVach.Background = Mau(mauVach);
            txtApLucThang.Text = apLuc.HasValue ? $"{apLuc:0.0##} bar" : "—";
            txtApLucThang.Foreground = Mau(mauVach);

            // --- Nhan ghi chu: bat buoc khi khong dat ---
            bool batBuocGhiChu = apLuc.HasValue && sutAp.HasValue &&
                                 !NguongKhamXe.DuDieuKienXuatBen(apLuc.Value, sutAp.Value, nuoc, veSinh);
            txtNhanGhiChu.Inlines.Clear();
            txtNhanGhiChu.Inlines.Add(new System.Windows.Documents.Run(
                batBuocGhiChu ? "Ghi chú kỹ thuật (nguyên nhân & biện pháp)" : "Ghi chú kỹ thuật"));
            if (batBuocGhiChu)
                txtNhanGhiChu.Inlines.Add(new System.Windows.Documents.Run(" *") { Foreground = Mau("#DC2626") });

            // --- Thong tin doan tau ---
            pnlDoanTau.Children.Clear();
            if (cboDoanTau.SelectedItem is DoanTauKhamXe dt)
            {
                ThemDongThongSo("Hành trình", dt.HanhTrinh);
                ThemDongThongSo("Xuất phát KH", dt.GioXuatPhatKH.ToString("HH:mm dd/MM/yyyy"));
                ThemDongThongSo("Đầu máy", dt.NhanDauMay);
                ThemDongThongSo("Số toa · chiều dài", $"{dt.TongSoToa} toa · {dt.TongChieuDaiM:N1} m");
                ThemDongThongSo("Trọng lượng đoàn", $"{dt.TongTrongLuongTan:N1} tấn", laDongCuoi: true);
            }
            else
            {
                pnlDoanTau.Children.Add(new TextBlock
                {
                    Text = "Chọn đoàn tàu để xem thông số.",
                    FontSize = 10,
                    Foreground = Mau("#94A3B8"),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 6, 0, 6)
                });
            }
        }

        private void DatKetLuan(bool? dat, string tieuDe, string moTa)
        {
            var (nen, vien, chu, bieuTuong) = dat switch
            {
                true => ("#F0FDF4", "#86EFAC", "#15803D", SymbolRegular.ShieldCheckmark24),
                false => ("#FEF2F2", "#FCA5A5", "#B91C1C", SymbolRegular.ShieldError24),
                _ => ("#F8FAFC", "#E2E8F0", "#475569", SymbolRegular.ShieldCheckmark24)
            };

            bdKetLuan.Background = Mau(nen);
            bdKetLuan.BorderBrush = Mau(vien);
            icoKetLuan.Symbol = bieuTuong;
            icoKetLuan.Foreground = Mau(dat == null ? "#94A3B8" : chu);
            txtKetLuan.Text = tieuDe;
            txtKetLuan.Foreground = Mau(chu);
            txtKetLuanPhu.Text = moTa;
        }

        private void ThemDongThongSo(string nhan, string giaTri, bool laDongCuoi = false)
        {
            var hang = new Grid { Height = 24 };
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tbNhan = new TextBlock { Text = nhan, Style = (Style)FindResource("KtNhanThongSo") };
            var tbGiaTri = new TextBlock { Text = giaTri, Style = (Style)FindResource("KtGiaTriThongSo") };
            Grid.SetColumn(tbGiaTri, 1);

            hang.Children.Add(tbNhan);
            hang.Children.Add(tbGiaTri);

            pnlDoanTau.Children.Add(new Border
            {
                Child = hang,
                BorderBrush = Mau("#E2E8F0"),
                BorderThickness = new Thickness(0, 0, 0, laDongCuoi ? 0 : 1)
            });
        }

        // Dien ghi chu tu cac hang muc khong dat (giu lai phan nguoi dung da go)
        private void BtnTaoGhiChu_Click(object sender, RoutedEventArgs e)
        {
            decimal? apLuc = LaySo(txtApLuc);
            decimal? sutAp = LaySo(txtSutAp);

            var dong = new List<string>();
            if (apLuc.HasValue && apLuc < NguongKhamXe.ApLucHamToiThieuBar)
                dong.Add($"Áp lực hãm {apLuc:0.0} bar thấp hơn chuẩn {NguongKhamXe.ApLucHamToiThieuBar:0.0} bar – kiểm tra máy nén gió, van hãm.");
            if (sutAp.HasValue && sutAp > NguongKhamXe.DoSutApToiDaBarPhut)
                dong.Add($"Sụt áp {sutAp:0.00} bar/phút vượt mức {NguongKhamXe.DoSutApToiDaBarPhut:0.00} – dò rò rỉ ống hãm, khớp nối giữa các toa.");
            if (chkCapNuoc.IsChecked != true)
                dong.Add("Chưa cấp nước sinh hoạt – yêu cầu tổ cấp nước bổ sung.");
            if (chkXaVeSinh.IsChecked != true)
                dong.Add("Chưa xả két vệ sinh – yêu cầu tổ vệ sinh depot xử lý.");

            if (dong.Count == 0)
                dong.Add("Đạt chuẩn an toàn kỹ thuật xuất bến.");

            string hienTai = txtGhiChu.Text.Trim();
            txtGhiChu.Text = hienTai.Length == 0 ? string.Join(Environment.NewLine, dong)
                                                 : hienTai + Environment.NewLine + string.Join(Environment.NewLine, dong);
            txtGhiChu.Focus();
            txtGhiChu.CaretIndex = txtGhiChu.Text.Length;
        }

        // =====================================================================
        // LUU / HUY
        // =====================================================================

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!KiemTraHopLe(out var bb)) return;

            // Ghi CSDL qua trang (BaoTriService). Loi nghiep vu / CSDL: bao va giu dialog de sua.
            string? loi = _luu(bb);
            if (loi != null)
            {
                ThongBaoDialog.CanhBao(loi, "Chưa lưu được biên bản");
                return;
            }
            DialogResult = true;
        }

        private bool KiemTraHopLe(out KhamXeDTO bb)
        {
            bool hopLe = true;
            Control? oLoiDauTien = null;

            void BaoLoi(TextBlock tbLoi, string thongDiep, Control o)
            {
                if (tbLoi.Text.Length == 0) tbLoi.Text = thongDiep;
                hopLe = false;
                oLoiDauTien ??= o;
            }

            foreach (var tb in new[] { txtLoiDoanTau, txtLoiNguoiKham, txtLoiApLuc, txtLoiSutAp, txtLoiGhiChu, txtLoiChiPhi })
                tb.Text = "";

            decimal? apLuc = LaySo(txtApLuc);
            decimal? sutAp = LaySo(txtSutAp);
            decimal? chiPhi = LaySo(txtChiPhi);
            if (!apLuc.HasValue) BaoLoi(txtLoiApLuc, "Vui lòng nhập áp lực hãm.", txtApLuc);
            if (!sutAp.HasValue) BaoLoi(txtLoiSutAp, "Vui lòng nhập độ sụt áp.", txtSutAp);
            if (!chiPhi.HasValue) BaoLoi(txtLoiChiPhi, "Chi phí không hợp lệ (nhập 0 nếu không phát sinh).", txtChiPhi);

            bb = new KhamXeDTO
            {
                MaDoanTau = (cboDoanTau.SelectedItem as DoanTauKhamXe)?.MaDoanTau ?? 0,
                MaNguoiKham = (cboNguoiKham.SelectedItem as TaiKhoanMau)?.MaTaiKhoan ?? 0,
                ApLucHamBar = apLuc ?? NguongKhamXe.ApLucHamToiThieuBar,
                DoSutApBarPhut = sutAp ?? 0m,
                DaCapNuoc = chkCapNuoc.IsChecked == true,
                DaXaVeSinh = chkXaVeSinh.IsChecked == true,
                GhiChuKyThuat = txtGhiChu.Text.Trim(),
                ChiPhi = chiPhi ?? 0m
            };

            // Cung luat voi Service (dialog chi doi ten truong sang o nhap)
            foreach (var l in QuyTacBaoTri.KiemTraKhamXe(bb))
            {
                switch (l.Truong)
                {
                    case "DoanTau": BaoLoi(txtLoiDoanTau, l.ThongDiep, cboDoanTau); break;
                    case "NguoiKham": BaoLoi(txtLoiNguoiKham, l.ThongDiep, cboNguoiKham); break;
                    case "ApLuc" when apLuc.HasValue: BaoLoi(txtLoiApLuc, l.ThongDiep, txtApLuc); break;
                    case "SutAp" when sutAp.HasValue: BaoLoi(txtLoiSutAp, l.ThongDiep, txtSutAp); break;
                    case "GhiChu" when apLuc.HasValue && sutAp.HasValue: BaoLoi(txtLoiGhiChu, l.ThongDiep, txtGhiChu); break;
                    case "ChiPhi" when chiPhi.HasValue: BaoLoi(txtLoiChiPhi, l.ThongDiep, txtChiPhi); break;
                }
            }

            if (!hopLe) oLoiDauTien?.Focus();
            return hopLe;
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

        private static SolidColorBrush Mau(string ma) => (SolidColorBrush)new BrushConverter().ConvertFrom(ma)!;
    }
}
