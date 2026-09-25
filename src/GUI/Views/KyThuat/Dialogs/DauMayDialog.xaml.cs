using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BUS.Services;
using DTO.Common;
using GUI.Helpers;
using GUI.Views.KyThuat.Models;

namespace GUI.Views.KyThuat.Dialogs
{
    // =========================================================================
    // DIALOG THEM MOI / CAP NHAT HO SO DAU MAY
    //
    // Giai doan dung giao dien: dialog chi kiem tra du lieu nhap va tra ve
    // doi tuong DauMayHienThi, trang goi se tu cap nhat danh sach tren man hinh.
    // Rang buoc kiem tra bam theo phuongtien.DauMay trong script CSDL.
    // =========================================================================
    public partial class DauMayDialog : Window
    {
        private static readonly Regex MauSoHieu = new(@"^[A-Z0-9]+-[0-9]+$");

        private readonly DauMayHienThi? _banGoc;          // null = them moi
        private readonly HashSet<string> _soHieuDaCo;     // de bao trung khoa UNIQUE SoHieuDauMay
        private readonly bool _dangKhoiTao;

        public DauMayHienThi? KetQua { get; private set; }

        public DauMayDialog(DauMayHienThi? banGoc, IEnumerable<string> soHieuDaCo, IEnumerable<string> danhSachXiNghiep)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;
            _soHieuDaCo = new HashSet<string>(soHieuDaCo, StringComparer.OrdinalIgnoreCase);

            SmartNumberFormatHelper.AttachIntegerSmall(txtNamSanXuat, 1970, DateTime.Now.Year);
            SmartNumberFormatHelper.AttachIntegerThousandSeparated(txtSoKm, 0, 99_999_999);

            cboDongMay.ItemsSource = DanhMucMauPhuongTien.DongDauMay;
            cboXiNghiep.ItemsSource = danhSachXiNghiep
                .Concat(DanhMucMauPhuongTien.XiNghiepDauMay)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (banGoc == null)
                KhoiTaoThemMoi();
            else
                KhoiTaoCapNhat(banGoc);

            _dangKhoiTao = false;
            CapNhatXemTruoc();

            Loaded += (_, _) =>
            {
                var oDau = banGoc == null ? txtSoHieu : (Control)cboXiNghiep;
                oDau.Focus();
                if (oDau is TextBox tb) tb.CaretIndex = tb.Text.Length;
            };
        }

        private void KhoiTaoThemMoi()
        {
            txtTieuDe.Text = "THÊM MỚI ĐẦU MÁY";
            txtTieuDePhu.Text = "Khai báo hồ sơ đầu máy mới vào đội phương tiện";
            txtNutLuu.Text = "Thêm Đầu Máy";

            cboDongMay.SelectedIndex = 0;
            cboXiNghiep.SelectedIndex = cboXiNghiep.Items.Count > 0 ? 0 : -1;
            txtNamSanXuat.Text = DateTime.Now.Year.ToString();
            txtSoKm.Text = "0";
            rdoSanSang.IsChecked = true;

            // Goi y san tien to theo dong may de nguoi dung chi can go phan so
            txtSoHieu.Text = $"{DanhMucMauPhuongTien.DongDauMay[0].MaDongCode}-";
        }

        private void KhoiTaoCapNhat(DauMayHienThi dm)
        {
            txtTieuDe.Text = $"CẬP NHẬT HỒ SƠ ĐẦU MÁY — {dm.SoHieuDauMay}";
            txtTieuDePhu.Text = "Số hiệu là khóa định danh nên không sửa sau khi đã tạo";
            txtNutLuu.Text = "Lưu Thay Đổi";

            txtSoHieu.Text = dm.SoHieuDauMay;
            txtSoHieu.IsReadOnly = true;
            txtSoHieu.Background = (Brush)new BrushConverter().ConvertFrom("#F1F5F9")!;
            txtSoHieu.Foreground = (Brush)new BrushConverter().ConvertFrom("#475569")!;

            cboDongMay.SelectedItem = DanhMucMauPhuongTien.TimDongDauMay(dm.MaDongCode) ?? DanhMucMauPhuongTien.DongDauMay[0];
            cboXiNghiep.Text = dm.DonViQuanLy;
            txtNamSanXuat.Text = dm.NamSanXuat > 0 ? dm.NamSanXuat.ToString() : DateTime.Now.Year.ToString();
            txtSoKm.Text = dm.SoKmTichLuy.ToString("N0", FormatHelper.TechnicalCulture);

            switch (dm.TrangThai)
            {
                case "DANG_CHAY": rdoDangChay.IsChecked = true; break;
                case "BAO_DUONG": rdoBaoDuong.IsChecked = true; break;
                default: rdoSanSang.IsChecked = true; break;
            }
        }

        // =====================================================================
        // XEM TRUOC TRUC TIEP
        // =====================================================================

        private void TruongNhap_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangKhoiTao) return;

            // Go lai thi xoa loi cua truong do
            if (sender == txtSoHieu) txtLoiSoHieu.Text = "";
            if (sender == txtNamSanXuat) txtLoiNamSanXuat.Text = "";
            if (sender == txtSoKm) txtLoiSoKm.Text = "";

            CapNhatXemTruoc();
        }

        private void CboDongMay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiDongMay.Text = "";

            // Them moi: doi tien to so hieu theo dong may neu nguoi dung chua go phan so
            if (_banGoc == null && cboDongMay.SelectedItem is DongDauMayMau dong)
            {
                string hienTai = txtSoHieu.Text.Trim();
                bool chiCoTienTo = hienTai.Length == 0 || hienTai.EndsWith("-");
                if (chiCoTienTo)
                {
                    txtSoHieu.Text = $"{dong.MaDongCode}-";
                    txtSoHieu.CaretIndex = txtSoHieu.Text.Length;
                }
            }

            CapNhatXemTruoc();
        }

        private void TrangThai_Checked(object sender, RoutedEventArgs e)
        {
            if (_dangKhoiTao) return;
            CapNhatXemTruoc();
        }

        private void CapNhatXemTruoc()
        {
            // --- Thong so dong may ---
            pnlThongSoDong.Children.Clear();
            if (cboDongMay.SelectedItem is DongDauMayMau dong)
            {
                ThemDongThongSo("Nhà sản xuất", dong.NhaSanXuat);
                ThemDongThongSo("Công suất", $"{dong.CongSuatHP:N0} HP");
                ThemDongThongSo("Tốc độ cấu tạo", $"{dong.TocDoToiDaKmh:N0} km/h");
                ThemDongThongSo("Sức kéo tối đa", $"{dong.SucKeoToiDaTan:N0} tấn");
                ThemDongThongSo("Bồn dầu", $"{dong.DungTichBonDauLit:N0} lít");
                ThemDongThongSo("Trọng lượng", $"{dong.TrongLuongTan:N1} tấn");
                ThemDongThongSo("Chiều dài", $"{dong.ChieuDaiM:N1} m", laDongCuoi: true);
            }

            // --- Tien do bao duong ---
            decimal soKm = LaySoKm();
            var (cap, moc, tyLe, mucCanhBao) = PhuongTienService.TinhTienDoBaoDuong(soKm);
            var mau = (SolidColorBrush)new BrushConverter().ConvertFrom(DauMayHienThi.LayMauTheoMucCanhBao(mucCanhBao))!;

            txtCapKeTiep.Text = $"Cấp kế tiếp: {cap} · mốc {moc:N0} km";
            txtTyLeBaoDuong.Text = $"{tyLe:N1}%";
            txtTyLeBaoDuong.Foreground = mau;
            bdVachBaoDuong.Background = mau;
            colDaChay.Width = new GridLength((double)tyLe, GridUnitType.Star);
            colConLai.Width = new GridLength(100 - (double)tyLe, GridUnitType.Star);

            decimal conLai = moc - soKm;
            txtMoTaBaoDuong.Text = mucCanhBao switch
            {
                "QUA_HAN" => "Đã vượt mốc đại tu Ro (300,000 km).",
                "KHAN_CAP" => $"Khẩn cấp: chỉ còn {conLai:N0} km tới mốc {cap}.",
                "CANH_BAO" => $"Cảnh báo: còn {conLai:N0} km tới mốc {cap}.",
                _ => $"Bình thường: còn {conLai:N0} km tới mốc {cap}."
            };

            // --- Tuoi khai thac ---
            txtTuoiKhaiThac.Text = int.TryParse(txtNamSanXuat.Text, out int nam) && nam >= 1970 && nam <= DateTime.Now.Year
                ? $"{DateTime.Now.Year - nam} năm"
                : "—";

            // --- Goi y chuyen trang thai khi sat / qua moc bao duong ---
            bool canDuaVaoXuong = mucCanhBao is "QUA_HAN" or "KHAN_CAP";
            if (canDuaVaoXuong && rdoBaoDuong.IsChecked != true)
            {
                bdGoiYTrangThai.Visibility = Visibility.Visible;
                txtGoiYTrangThai.Text = mucCanhBao == "QUA_HAN"
                    ? "Đầu máy đã vượt mốc đại tu. Nên chuyển sang trạng thái Bảo dưỡng trước khi vận dụng."
                    : $"Đầu máy sắp tới mốc bảo dưỡng {cap}. Cân nhắc đưa vào xưởng.";
            }
            else
            {
                bdGoiYTrangThai.Visibility = Visibility.Collapsed;
            }
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

            pnlThongSoDong.Children.Add(new Border
            {
                Child = hang,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#E2E8F0")!,
                BorderThickness = new Thickness(0, 0, 0, laDongCuoi ? 0 : 1)
            });
        }

        private decimal LaySoKm()
            => FormatHelper.TryParseCurrency(txtSoKm.Text, out decimal km) ? km : 0m;

        private void BtnChuyenBaoDuong_Click(object sender, RoutedEventArgs e)
        {
            rdoBaoDuong.IsChecked = true;
        }

        // =====================================================================
        // LUU / HUY
        // =====================================================================

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!KiemTraHopLe(out var dong, out string soHieu, out string xiNghiep, out int nam, out decimal soKm))
                return;

            var kq = _banGoc?.SaoChep() ?? new DauMayHienThi();

            kq.SoHieuDauMay = soHieu;
            kq.MaDongCode = dong.MaDongCode;
            kq.NhaSanXuat = dong.NhaSanXuat;
            kq.CongSuatHP = dong.CongSuatHP;
            kq.TocDoToiDaKmh = dong.TocDoToiDaKmh;
            kq.SucKeoToiDaTan = dong.SucKeoToiDaTan;
            kq.DungTichBonDauLit = dong.DungTichBonDauLit;
            kq.TrongLuongTan = dong.TrongLuongTan;
            kq.ChieuDaiM = dong.ChieuDaiM;
            kq.DonViQuanLy = xiNghiep;
            kq.NamSanXuat = nam;
            kq.TuoiKhaiThac = DateTime.Now.Year - nam;
            kq.SoKmTichLuy = soKm;
            kq.TrangThai = rdoDangChay.IsChecked == true ? "DANG_CHAY"
                         : rdoBaoDuong.IsChecked == true ? "BAO_DUONG"
                         : "SAN_SANG";
            kq.TinhLaiTienDoBaoDuong();

            KetQua = kq;
            DialogResult = true;
        }

        private bool KiemTraHopLe(out DongDauMayMau dong, out string soHieu, out string xiNghiep, out int nam, out decimal soKm)
        {
            bool hopLe = true;
            Control? oLoiDauTien = null;

            void BaoLoi(TextBlock tbLoi, string thongDiep, Control o)
            {
                tbLoi.Text = thongDiep;
                hopLe = false;
                oLoiDauTien ??= o;
            }

            // So hieu (UNIQUE, VARCHAR(20))
            soHieu = txtSoHieu.Text.Trim().ToUpperInvariant();
            if (_banGoc == null)
            {
                if (soHieu.Length == 0 || soHieu.EndsWith("-"))
                    BaoLoi(txtLoiSoHieu, "Vui lòng nhập đầy đủ số hiệu đầu máy.", txtSoHieu);
                else if (!MauSoHieu.IsMatch(soHieu))
                    BaoLoi(txtLoiSoHieu, "Số hiệu phải có dạng DÒNG-SỐ, ví dụ D19E-904.", txtSoHieu);
                else if (_soHieuDaCo.Contains(soHieu))
                    BaoLoi(txtLoiSoHieu, $"Số hiệu {soHieu} đã tồn tại (trùng khóa UNIQUE SoHieuDauMay).", txtSoHieu);
            }

            // Dong dau may (khoa ngoai MaDongDauMay)
            dong = (cboDongMay.SelectedItem as DongDauMayMau)!;
            if (dong == null)
                BaoLoi(txtLoiDongMay, "Vui lòng chọn dòng đầu máy.", cboDongMay);

            // Xi nghiep (NVARCHAR(50) NOT NULL)
            xiNghiep = cboXiNghiep.Text.Trim();
            if (xiNghiep.Length == 0)
                BaoLoi(txtLoiXiNghiep, "Vui lòng chọn hoặc nhập xí nghiệp quản lý.", cboXiNghiep);
            else if (xiNghiep.Length > 50)
                BaoLoi(txtLoiXiNghiep, $"Tên xí nghiệp tối đa 50 ký tự (đang có {xiNghiep.Length}).", cboXiNghiep);
            else
                txtLoiXiNghiep.Text = "";

            // Nam san xuat (CHECK NamSanXuat >= 1970)
            if (!int.TryParse(txtNamSanXuat.Text.Trim(), out nam) || nam < 1970 || nam > DateTime.Now.Year)
                BaoLoi(txtLoiNamSanXuat, $"Năm sản xuất phải từ 1970 đến {DateTime.Now.Year}.", txtNamSanXuat);

            // So km (DECIMAL(9,1), >= 0)
            soKm = LaySoKm();
            if (soKm < 0 || soKm > 99_999_999m)
                BaoLoi(txtLoiSoKm, "Số km không hợp lệ.", txtSoKm);

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
    }
}
