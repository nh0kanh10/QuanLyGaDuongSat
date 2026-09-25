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
    // DIALOG GHI NHAN / CAP NHAT PHIEU BAO DUONG DINH KY (baotri.NhatKyBaoDuong)
    //
    // - Loai phuong tien quyet dinh FK va cap hop le: dau may -> R1/R2,
    //   toa xe -> D1/D2 (CK_PhuongTien_Ref; ghep cap theo loai la quy uoc giao dien).
    // - Tu goi y cap can lam nhat, xem truoc moc ke tiep sau khi ghi nhan.
    // - Chi tra ve BaoDuongHienThi, trang goi tu cap nhat danh sach (chua ghi CSDL).
    // =========================================================================
    public partial class BaoDuongDialog : Window
    {
        private readonly BaoDuongHienThi? _banGoc;                 // null = ghi nhan moi
        private readonly IReadOnlyList<BaoDuongHienThi> _toanBoLichSu;
        private readonly DateTime _thoiDiem;
        private bool _dangKhoiTao;

        public BaoDuongHienThi? KetQua { get; private set; }

        public BaoDuongDialog(BaoDuongHienThi? banGoc, IReadOnlyList<BaoDuongHienThi> toanBoLichSu,
                              string? khoaPhuongTienMacDinh = null)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;
            _toanBoLichSu = toanBoLichSu;
            _thoiDiem = banGoc?.ThoiDiemHoanThanh ?? DateTime.Now;

            ONhapSoThucHelper.Gan(txtSoKm, 1, coDinhSoLe: false);
            cboNguoi.ItemsSource = DanhMucMauBaoTri.NguoiBaoDuong;

            if (banGoc == null)
                KhoiTaoThemMoi(khoaPhuongTienMacDinh);
            else
                KhoiTaoCapNhat(banGoc);

            _dangKhoiTao = false;
            CapNhatXemTruoc();

            Loaded += (_, _) =>
            {
                Control oDau = cboPhuongTien.SelectedItem == null ? cboPhuongTien : txtSoKm;
                oDau.Focus();
                if (oDau is TextBox tb) tb.SelectAll();
            };
        }

        private void KhoiTaoThemMoi(string? khoaMacDinh)
        {
            txtTieuDe.Text = "GHI NHẬN BẢO DƯỠNG ĐỊNH KỲ";
            txtTieuDePhu.Text = "Đầu máy cấp R1 / R2 · toa xe cấp D1 / D2";
            txtNutLuu.Text = "Lưu Phiếu Bảo Dưỡng";
            txtThoiDiem.Text = $"Hoàn thành: {_thoiDiem:HH:mm dd/MM/yyyy} (tự ghi)";
            txtTieuDeHienTai.Text = "TÌNH TRẠNG HIỆN TẠI";

            var ptMacDinh = khoaMacDinh != null ? DanhMucMauBaoTri.TimPhuongTien(khoaMacDinh) : null;
            string loai = ptMacDinh?.LoaiPhuongTien ?? "DAU_MAY";

            (loai == "DAU_MAY" ? rdoDauMay : rdoToaXe).IsChecked = true;
            NapPhuongTien(loai);
            cboPhuongTien.SelectedItem = ptMacDinh;
            cboNguoi.SelectedIndex = 0;

            if (ptMacDinh != null) DienGoiYTheoPhuongTien();
        }

        private void KhoiTaoCapNhat(BaoDuongHienThi bd)
        {
            txtTieuDe.Text = $"CẬP NHẬT PHIẾU {bd.MaPhieu} — {bd.SoHieuPhuongTien}";
            txtTieuDePhu.Text = "Phương tiện và thời điểm hoàn thành là thông tin gốc của phiếu nên không sửa";
            txtNutLuu.Text = "Lưu Thay Đổi";
            txtThoiDiem.Text = $"Hoàn thành: {_thoiDiem:HH:mm dd/MM/yyyy} (giữ nguyên)";
            txtTieuDeHienTai.Text = "TRƯỚC KHI SỬA";

            (bd.LaDauMay ? rdoDauMay : rdoToaXe).IsChecked = true;
            rdoDauMay.IsEnabled = false;
            rdoToaXe.IsEnabled = false;
            NapPhuongTien(bd.LoaiPhuongTien);
            cboPhuongTien.SelectedItem = DanhMucMauBaoTri.TimPhuongTien(bd.KhoaPhuongTien);
            cboPhuongTien.IsEnabled = false;

            ChonCap(bd.CapBaoDuong);
            txtSoKm.Text = bd.SoKmTaiThoiDiem.ToString("0.#", FormatHelper.TechnicalCulture);
            cboNguoi.SelectedItem = DanhMucMauBaoTri.TimNguoiKham(bd.MaNguoiThucHien ?? 0);
            txtGhiChu.Text = bd.GhiChuKyThuat;
        }

        // =====================================================================
        // LOAI PHUONG TIEN / PHUONG TIEN / CAP
        // =====================================================================

        private string LoaiDangChon => rdoToaXe.IsChecked == true ? "TOA_XE" : "DAU_MAY";

        private void NapPhuongTien(string loai)
        {
            runNhanPhuongTien.Text = loai == "DAU_MAY" ? "Đầu máy" : "Toa xe";
            cboPhuongTien.ItemsSource = DanhMucMauBaoTri.PhuongTien.Where(p => p.LoaiPhuongTien == loai).ToList();
            cboPhuongTien.SelectedIndex = -1;

            string[] cap = ChuKyBaoDuong.CapCua(loai);
            rdoCap1.Content = cap[0];
            rdoCap2.Content = cap[1];
            rdoCap1.Tag = ChuKyBaoDuong.MauCap(cap[0]);
            rdoCap2.Tag = ChuKyBaoDuong.MauCap(cap[1]);
            rdoCap1.IsChecked = false;
            rdoCap2.IsChecked = false;
        }

        private string? CapDangChon
            => rdoCap1.IsChecked == true ? rdoCap1.Content?.ToString()
             : rdoCap2.IsChecked == true ? rdoCap2.Content?.ToString()
             : null;

        private void ChonCap(string cap)
        {
            if (rdoCap1.Content?.ToString() == cap) rdoCap1.IsChecked = true;
            else if (rdoCap2.Content?.ToString() == cap) rdoCap2.IsChecked = true;
        }

        private void LoaiPhuongTien_Checked(object sender, RoutedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiPhuongTien.Text = "";
            txtLoiCap.Text = "";
            NapPhuongTien(LoaiDangChon);
            txtSoKm.Text = "";
            CapNhatXemTruoc();
        }

        private void CboPhuongTien_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiPhuongTien.Text = "";
            DienGoiYTheoPhuongTien();
            CapNhatXemTruoc();
        }

        // Lap moi: chon san cap can lam nhat + dien san so km
        private void DienGoiYTheoPhuongTien()
        {
            if (_banGoc != null || cboPhuongTien.SelectedItem is not PhuongTienBaoDuong pt) return;

            var lichSu = LichSuCua(pt, boQuaBanGoc: false);
            var chuY = ChuKyBaoDuong.CapCanChuY(ChuKyBaoDuong.TinhTienDo(pt, lichSu));
            ChonCap(chuY?.Cap ?? ChuKyBaoDuong.CapCua(pt.LoaiPhuongTien)[0]);

            bool cu = _dangKhoiTao;
            _dangKhoiTao = true;
            txtSoKm.Text = pt.LaDauMay
                ? pt.SoKmTichLuy.ToString("0.#", FormatHelper.TechnicalCulture)
                : lichSu.OrderByDescending(x => x.ThoiDiemHoanThanh).FirstOrDefault()?.SoKmTaiThoiDiem
                        .ToString("0.#", FormatHelper.TechnicalCulture) ?? "";
            txtLoiSoKm.Text = "";
            _dangKhoiTao = cu;
        }

        private void CapBaoDuong_Checked(object sender, RoutedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiCap.Text = "";
            CapNhatXemTruoc();
        }

        private void TruongNhap_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiSoKm.Text = "";
            CapNhatXemTruoc();
        }

        private void CboNguoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiNguoi.Text = "";
        }

        // Lich su bao duong cua phuong tien (da tron thay doi tam); khi sua thi bo phieu dang sua
        private List<BaoDuongHienThi> LichSuCua(PhuongTienBaoDuong pt, bool boQuaBanGoc)
            => _toanBoLichSu.Where(x => x.KhoaPhuongTien == pt.Khoa &&
                                        !(boQuaBanGoc && _banGoc != null && x.MaBaoDuong == _banGoc.MaBaoDuong))
                            .ToList();

        // =====================================================================
        // XEM TRUOC TRUC TIEP
        // =====================================================================

        private void CapNhatXemTruoc()
        {
            string? cap = CapDangChon;
            txtMoTaCap.Text = cap != null ? ChuKyBaoDuong.MoTaChuKy(cap) : "Chọn cấp bảo dưỡng đã thực hiện.";

            if (cboPhuongTien.SelectedItem is not PhuongTienBaoDuong pt)
            {
                icTienDoHienTai.ItemsSource = null;
                icTienDoSau.ItemsSource = null;
                bdGoiYCap.Visibility = Visibility.Collapsed;
                txtGoiYSoKm.Text = "";
                return;
            }

            // --- Tien do hien tai (khi sua: gom ca ban goc) ---
            var lichSuKhac = LichSuCua(pt, boQuaBanGoc: true);
            var lichSuHienTai = _banGoc != null ? lichSuKhac.Append(_banGoc).ToList() : lichSuKhac;
            var tienDoHienTai = ChuKyBaoDuong.TinhTienDo(pt, lichSuHienTai);
            icTienDoHienTai.ItemsSource = tienDoHienTai;

            // --- Sau khi ghi nhan: thay phieu nay bang ban xem truoc ---
            decimal? km = ONhapSoThucHelper.Lay(txtSoKm);
            if (cap != null && km.HasValue)
            {
                var banXemTruoc = new BaoDuongHienThi
                {
                    CapBaoDuong = cap,
                    SoKmTaiThoiDiem = km.Value,
                    ThoiDiemHoanThanh = _thoiDiem
                };
                banXemTruoc.GanPhuongTien(pt);
                icTienDoSau.ItemsSource = ChuKyBaoDuong.TinhTienDo(pt, lichSuKhac.Append(banXemTruoc));
            }
            else
            {
                icTienDoSau.ItemsSource = null;
            }

            // --- Goi y cap ---
            var chuY = ChuKyBaoDuong.CapCanChuY(tienDoHienTai);
            HienGoiYCap(pt, chuY, cap);

            // --- Goi y so km ---
            var lanTruoc = lichSuKhac.Where(x => x.ThoiDiemHoanThanh < _thoiDiem)
                                     .OrderByDescending(x => x.ThoiDiemHoanThanh).FirstOrDefault();
            string truoc = lanTruoc != null ? $"Lần trước: {lanTruoc.SoKmTaiThoiDiem:N0} km ({lanTruoc.CapBaoDuong})." : "Chưa có lần bảo dưỡng trước.";
            txtGoiYSoKm.Text = pt.LaDauMay
                ? $"Km tích lũy hiện tại: {pt.SoKmTichLuy:N0} km. {truoc}"
                : $"CSDL không theo dõi km của toa — nhập theo lý lịch toa. {truoc}";
        }

        private void HienGoiYCap(PhuongTienBaoDuong pt, TienDoCapBaoDuong? chuY, string? capChon)
        {
            // Khi sua, tinh trang "hien tai" da gom chinh phieu nay => goi y de gay hieu lam, an di
            if (_banGoc != null)
            {
                bdGoiYCap.Visibility = Visibility.Collapsed;
                return;
            }
            bdGoiYCap.Visibility = Visibility.Visible;
            bool canLam = chuY != null && chuY.TrangThai is "QUA_HAN" or "SAP_TOI";
            bool chonThapHon = canLam && capChon != null && !ChuKyBaoDuong.LaCapCao(capChon) && ChuKyBaoDuong.LaCapCao(chuY!.Cap);

            string mauChu, mauNen, mauVien;
            SymbolRegular bieuTuong;
            string noiDung;

            if (chonThapHon)
            {
                (mauChu, mauNen, mauVien, bieuTuong) = ("#B91C1C", "#FEF2F2", "#FECACA", SymbolRegular.Warning24);
                noiDung = $"{pt.SoHieu} đang {chuY!.CumTrangThai} ({chuY.ConLai}). " +
                          $"Cấp {capChon} không làm mới mốc {chuY.Cap} — nên chọn {chuY.Cap} (bao gồm {capChon}).";
            }
            else if (canLam)
            {
                (mauChu, mauNen, mauVien, bieuTuong) = chuY!.TrangThai == "QUA_HAN"
                    ? ("#B91C1C", "#FEF2F2", "#FECACA", SymbolRegular.ErrorCircle24)
                    : ("#B45309", "#FFFBEB", "#FDE68A", SymbolRegular.Warning24);
                noiDung = $"{pt.SoHieu} đang {chuY.CumTrangThai} ({chuY.ConLai}). " +
                          (capChon == chuY.Cap || (capChon != null && ChuKyBaoDuong.LaCapCao(capChon))
                              ? $"Ghi nhận {capChon} sẽ làm mới mốc này."
                              : $"Gợi ý chọn cấp {chuY.Cap}.");
            }
            else
            {
                (mauChu, mauNen, mauVien, bieuTuong) = ("#1D4ED8", "#EFF6FF", "#BFDBFE", SymbolRegular.Info24);
                noiDung = chuY?.DaDung.HasValue == true
                    ? $"{pt.SoHieu} chưa tới hạn cấp nào. Gần nhất: {chuY.Cap} ({chuY.ConLai})."
                    : $"{pt.SoHieu} chưa có lịch sử bảo dưỡng — phiếu này sẽ là mốc đầu tiên để tính chu kỳ.";
            }

            bdGoiYCap.Background = Mau(mauNen);
            bdGoiYCap.BorderBrush = Mau(mauVien);
            icoGoiYCap.Symbol = bieuTuong;
            icoGoiYCap.Foreground = Mau(mauChu);
            txtGoiYCap.Foreground = Mau(mauChu);
            txtGoiYCap.Text = noiDung;
        }

        // =====================================================================
        // LUU / HUY
        // =====================================================================

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!KiemTraHopLe(out var pt, out string cap, out decimal soKm, out var nguoi)) return;

            var kq = _banGoc?.SaoChep() ?? new BaoDuongHienThi { ThoiDiemHoanThanh = _thoiDiem };
            kq.GanPhuongTien(pt);
            kq.CapBaoDuong = cap;
            kq.SoKmTaiThoiDiem = soKm;
            kq.MaNguoiThucHien = nguoi.MaTaiKhoan;
            kq.TenNguoiThucHien = nguoi.HoTenHienThi;
            kq.GhiChuKyThuat = txtGhiChu.Text.Trim();
            kq.LaThayDoiTam = true;

            KetQua = kq;
            DialogResult = true;
        }

        private bool KiemTraHopLe(out PhuongTienBaoDuong pt, out string cap, out decimal soKm, out TaiKhoanMau nguoi)
        {
            bool hopLe = true;
            Control? oLoiDauTien = null;

            void BaoLoi(TextBlock tbLoi, string thongDiep, Control o)
            {
                tbLoi.Text = thongDiep;
                hopLe = false;
                oLoiDauTien ??= o;
            }

            // Phuong tien (CK_PhuongTien_Ref: dung mot FK theo loai)
            pt = (cboPhuongTien.SelectedItem as PhuongTienBaoDuong)!;
            if (pt == null)
                BaoLoi(txtLoiPhuongTien, $"Vui lòng chọn {(LoaiDangChon == "DAU_MAY" ? "đầu máy" : "toa xe")}.", cboPhuongTien);

            // Cap (CHECK R1/R2/D1/D2)
            cap = CapDangChon ?? "";
            if (cap.Length == 0)
                BaoLoi(txtLoiCap, "Vui lòng chọn cấp bảo dưỡng.", rdoCap1);

            // So km: CHECK >= 0, DECIMAL(9,1)
            soKm = 0m;
            decimal? k = ONhapSoThucHelper.Lay(txtSoKm);
            if (!k.HasValue)
                BaoLoi(txtLoiSoKm, "Vui lòng nhập số km tại thời điểm bảo dưỡng.", txtSoKm);
            else if (k < 0 || k > ChuKyBaoDuong.SoKmGioiHan)
                BaoLoi(txtLoiSoKm, "Số km không hợp lệ.", txtSoKm);
            else if (ONhapSoThucHelper.QuaSoChuSoLe(k.Value, 1))
                BaoLoi(txtLoiSoKm, "Tối đa 1 chữ số thập phân (DECIMAL(9,1)).", txtSoKm);
            else if (pt != null && pt.LaDauMay && k > pt.SoKmTichLuy)
                BaoLoi(txtLoiSoKm, $"Lớn hơn km tích lũy hiện tại của đầu máy ({pt.SoKmTichLuy:N0} km).", txtSoKm);
            else if (pt != null)
            {
                // Km khong duoc lui so voi lan bao duong truoc do cua cung phuong tien
                var lanTruoc = LichSuCua(pt, boQuaBanGoc: true)
                    .Where(x => x.ThoiDiemHoanThanh < _thoiDiem)
                    .OrderByDescending(x => x.SoKmTaiThoiDiem).FirstOrDefault();
                if (lanTruoc != null && k < lanTruoc.SoKmTaiThoiDiem)
                    BaoLoi(txtLoiSoKm, $"Nhỏ hơn số km ở lần bảo dưỡng trước ({lanTruoc.SoKmTaiThoiDiem:N0} km, " +
                                       $"{lanTruoc.CapBaoDuong} ngày {lanTruoc.ThoiDiemHoanThanh:dd/MM/yyyy}).", txtSoKm);
                else
                    soKm = k.Value;
            }

            // Nguoi thuc hien (CSDL cho NULL, giao dien yeu cau ghi ro)
            nguoi = (cboNguoi.SelectedItem as TaiKhoanMau)!;
            if (nguoi == null)
                BaoLoi(txtLoiNguoi, "Vui lòng chọn người thực hiện.", cboNguoi);

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
