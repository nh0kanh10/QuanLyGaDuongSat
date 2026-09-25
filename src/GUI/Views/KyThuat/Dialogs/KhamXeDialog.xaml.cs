using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DTO.Common;
using GUI.Views.KyThuat.Helpers;
using GUI.Views.KyThuat.Models;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.KyThuat.Dialogs
{
    // =========================================================================
    // DIALOG LAP / CAP NHAT BIEN BAN KHAM XE KY THUAT (baotri.KhamXeKyThuat)
    //
    // Giai doan dung giao dien: chi kiem tra du lieu nhap va tra ve doi tuong
    // KhamXeHienThi, trang goi tu cap nhat danh sach tren man hinh.
    // Ket luan "du dieu kien xuat ben" tinh dung cong thuc cot PERSISTED
    // DuDieuKienXuatBen (NguongKhamXe), xem truoc ngay khi go.
    // =========================================================================
    public partial class KhamXeDialog : Window
    {
        private readonly KhamXeHienThi? _banGoc;   // null = lap bien ban moi
        private readonly bool _dangKhoiTao;

        public KhamXeHienThi? KetQua { get; private set; }

        // maDoanTauMacDinh / maNguoiKhamMacDinh: chon san khi lap tu the doan tau hoac "Kham lai"
        public KhamXeDialog(KhamXeHienThi? banGoc, int? maDoanTauMacDinh = null, int? maNguoiKhamMacDinh = null)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;

            ONhapSoThucHelper.Gan(txtApLuc, 1);
            ONhapSoThucHelper.Gan(txtSutAp, 2);

            cboDoanTau.ItemsSource = DanhMucMauBaoTri.DoanTau;
            cboNguoiKham.ItemsSource = DanhMucMauBaoTri.NguoiKham;

            if (banGoc == null)
                KhoiTaoThemMoi(maDoanTauMacDinh, maNguoiKhamMacDinh);
            else
                KhoiTaoCapNhat(banGoc);

            _dangKhoiTao = false;
            CapNhatXemTruoc();

            Loaded += (_, _) =>
            {
                Control oDau = cboDoanTau.SelectedItem == null ? cboDoanTau : txtApLuc;
                oDau.Focus();
                if (oDau is TextBox tb) tb.SelectAll();
            };
        }

        private void KhoiTaoThemMoi(int? maDoanTau, int? maNguoiKham)
        {
            txtTieuDe.Text = "LẬP BIÊN BẢN KHÁM XE KỸ THUẬT";
            txtTieuDePhu.Text = "Thử hãm gió, kiểm tra cấp nước và vệ sinh trước khi xuất bến";
            txtNutLuu.Text = "Lưu Biên Bản";
            txtThoiDiemKham.Text = DateTime.Now.ToString("HH:mm dd/MM/yyyy");
            txtGoiYThoiDiem.Text = "Tự ghi khi lưu";

            cboDoanTau.SelectedItem = maDoanTau.HasValue ? DanhMucMauBaoTri.TimDoanTau(maDoanTau.Value) : null;
            cboNguoiKham.SelectedItem = DanhMucMauBaoTri.TimNguoiKham(maNguoiKham ?? 0) ?? DanhMucMauBaoTri.NguoiKham[0];

            // Kham lai thuong da xu ly xong phan phuc vu, nhung van de nguoi kham tu tick
            chkCapNuoc.IsChecked = false;
            chkXaVeSinh.IsChecked = false;
        }

        private void KhoiTaoCapNhat(KhamXeHienThi bb)
        {
            txtTieuDe.Text = $"CẬP NHẬT BIÊN BẢN {bb.MaBienBan} — {bb.SoHieuMacTau}";
            txtTieuDePhu.Text = "Đoàn tàu và thời điểm khám là thông tin gốc của biên bản nên không sửa";
            txtNutLuu.Text = "Lưu Thay Đổi";
            txtThoiDiemKham.Text = bb.ThoiDiemKham.ToString("HH:mm dd/MM/yyyy");
            txtGoiYThoiDiem.Text = "Giữ nguyên thời điểm gốc";

            cboDoanTau.SelectedItem = DanhMucMauBaoTri.TimDoanTau(bb.MaDoanTau);
            cboDoanTau.IsEnabled = false;
            cboNguoiKham.SelectedItem = DanhMucMauBaoTri.TimNguoiKham(bb.MaNguoiKham);

            txtApLuc.Text = bb.ApLucHamBar.ToString("0.0", FormatHelper.TechnicalCulture);
            txtSutAp.Text = bb.DoSutApBarPhut.ToString("0.00", FormatHelper.TechnicalCulture);
            chkCapNuoc.IsChecked = bb.DaCapNuoc;
            chkXaVeSinh.IsChecked = bb.DaXaVeSinh;
            txtGhiChu.Text = bb.GhiChuKyThuat;
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
            if (!KiemTraHopLe(out var doanTau, out var nguoiKham, out decimal apLuc, out decimal sutAp))
                return;

            var kq = _banGoc?.SaoChep() ?? new KhamXeHienThi { ThoiDiemKham = DateTime.Now };

            kq.GanDoanTau(doanTau);
            kq.MaNguoiKham = nguoiKham.MaTaiKhoan;
            kq.TenNguoiKham = nguoiKham.HoTenHienThi;
            kq.ApLucHamBar = apLuc;
            kq.DoSutApBarPhut = sutAp;
            kq.DaCapNuoc = chkCapNuoc.IsChecked == true;
            kq.DaXaVeSinh = chkXaVeSinh.IsChecked == true;
            kq.GhiChuKyThuat = txtGhiChu.Text.Trim();
            kq.LaThayDoiTam = true;

            KetQua = kq;
            DialogResult = true;
        }

        private bool KiemTraHopLe(out DoanTauKhamXe doanTau, out TaiKhoanMau nguoiKham, out decimal apLuc, out decimal sutAp)
        {
            bool hopLe = true;
            Control? oLoiDauTien = null;

            void BaoLoi(TextBlock tbLoi, string thongDiep, Control o)
            {
                tbLoi.Text = thongDiep;
                hopLe = false;
                oLoiDauTien ??= o;
            }

            // Doan tau (FK MaDoanTau NOT NULL)
            doanTau = (cboDoanTau.SelectedItem as DoanTauKhamXe)!;
            if (doanTau == null)
                BaoLoi(txtLoiDoanTau, "Vui lòng chọn đoàn tàu được khám.", cboDoanTau);

            // Nguoi kham (FK MaNguoiKham NOT NULL)
            nguoiKham = (cboNguoiKham.SelectedItem as TaiKhoanMau)!;
            if (nguoiKham == null)
                BaoLoi(txtLoiNguoiKham, "Vui lòng chọn nhân viên khám xe.", cboNguoiKham);

            // Ap luc ham: CHECK (> 0 AND <= 10.0), DECIMAL(3,1)
            apLuc = 0m;
            decimal? a = LaySo(txtApLuc);
            if (!a.HasValue)
                BaoLoi(txtLoiApLuc, "Vui lòng nhập áp lực hãm.", txtApLuc);
            else if (a <= 0 || a > NguongKhamXe.ApLucHamGioiHanBar)
                BaoLoi(txtLoiApLuc, $"Áp lực hãm phải lớn hơn 0 và không quá {NguongKhamXe.ApLucHamGioiHanBar:0.0} bar.", txtApLuc);
            else if (decimal.Round(a.Value, 1) != a.Value)
                BaoLoi(txtLoiApLuc, "Chỉ ghi 1 chữ số thập phân (ví dụ 4.8), CSDL lưu DECIMAL(3,1).", txtApLuc);
            else
                apLuc = a.Value;

            // Do sut ap: CHECK (>= 0 AND <= 5.0), DECIMAL(3,2)
            sutAp = 0m;
            decimal? s = LaySo(txtSutAp);
            if (!s.HasValue)
                BaoLoi(txtLoiSutAp, "Vui lòng nhập độ sụt áp.", txtSutAp);
            else if (s < 0 || s > NguongKhamXe.DoSutApGioiHanBarPhut)
                BaoLoi(txtLoiSutAp, $"Độ sụt áp phải từ 0 đến {NguongKhamXe.DoSutApGioiHanBarPhut:0.00} bar/phút.", txtSutAp);
            else if (decimal.Round(s.Value, 2) != s.Value)
                BaoLoi(txtLoiSutAp, "Chỉ ghi 2 chữ số thập phân (ví dụ 0.18), CSDL lưu DECIMAL(3,2).", txtSutAp);
            else
                sutAp = s.Value;

            // Khong dat thi phai ghi nguyen nhan / bien phap (quy uoc giao dien, CSDL cho phep NULL)
            if (hopLe && txtGhiChu.Text.Trim().Length == 0 &&
                !NguongKhamXe.DuDieuKienXuatBen(apLuc, sutAp, chkCapNuoc.IsChecked == true, chkXaVeSinh.IsChecked == true))
            {
                BaoLoi(txtLoiGhiChu, "Biên bản không đạt: ghi rõ nguyên nhân và biện pháp xử lý (có thể bấm \"Tạo từ kết quả\").", txtGhiChu);
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
