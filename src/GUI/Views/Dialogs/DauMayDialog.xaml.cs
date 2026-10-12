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
    // DIALOG THEM MOI / CAP NHAT HO SO DAU MAY (phuongtien.DauMay)
    //
    // - Kiem tra nhap lieu bang CUNG ham voi Service (PhuongTienService.KiemTraDauMay)
    //   de bao loi ngay canh tung o.
    // - Bam Luu: goi ham luu cua trang (PhuongTienService.ThemDauMay / CapNhatDauMay).
    //   Service tu choi (trung so hieu, luat trang thai, nguoi khac vua sua...) thi hien
    //   ly do va GIU NGUYEN dialog de nguoi dung sua tiep.
    // - Khi sua: khoa so hieu va dong may (Service cung chan).
    // - So hieu = tien to theo dong may (txtTienToSoHieu, khong go duoc, chi doi khi chon
    //   dong khac) + phan so (txtSoHieu).
    // =========================================================================
    public partial class DauMayDialog : Window
    {
        private readonly DauMayHienThi? _banGoc;          // null = them moi
        private readonly Func<DauMayHienThi, string?> _luu;
        private readonly bool _dangKhoiTao;

        public DauMayHienThi? KetQua { get; private set; }

        // luu: ghi vao CSDL, tra ve null neu thanh cong, nguoc lai la thong diep loi
        public DauMayDialog(DauMayHienThi? banGoc, IReadOnlyList<DongDauMayMau> dongDauMay,
                            IEnumerable<string> danhSachXiNghiep, Func<DauMayHienThi, string?> luu)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;
            _luu = luu;

            SmartNumberFormatHelper.AttachIntegerSmall(txtNamSanXuat, PhuongTienService.NamSanXuatNhoNhat, DateTime.Now.Year);
            SmartNumberFormatHelper.AttachIntegerThousandSeparated(txtSoKm, 0, 99_999_999);

            cboDongMay.ItemsSource = dongDauMay;
            cboXiNghiep.ItemsSource = danhSachXiNghiep
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (banGoc == null)
                KhoiTaoThemMoi();
            else
                KhoiTaoCapNhat(banGoc, dongDauMay);

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

            cboDongMay.SelectedIndex = cboDongMay.Items.Count > 0 ? 0 : -1;
            cboXiNghiep.SelectedIndex = cboXiNghiep.Items.Count > 0 ? 0 : -1;
            txtNamSanXuat.Text = DateTime.Now.Year.ToString();
            txtSoKm.Text = "0";
            rdoSanSang.IsChecked = true;

            // Tien to theo dong may, nguoi dung chi go phan so
            DatTienToSoHieu(cboDongMay.SelectedItem is DongDauMayMau dong ? $"{dong.MaDongCode}-" : "");
        }

        private void KhoiTaoCapNhat(DauMayHienThi dm, IReadOnlyList<DongDauMayMau> dongDauMay)
        {
            txtTieuDe.Text = $"CẬP NHẬT HỒ SƠ ĐẦU MÁY — {dm.SoHieuDauMay}";
            txtTieuDePhu.Text = "Số hiệu và dòng máy là định danh nên không sửa sau khi đã tạo";
            txtNutLuu.Text = "Lưu Thay Đổi";

            // Tach "D19E-901" thanh tien to + phan so de giong luc them moi; so hieu khong co
            // dau gach (du lieu cu) thi hien nguyen trong o
            int gach = dm.SoHieuDauMay.IndexOf('-');
            DatTienToSoHieu(gach > 0 ? dm.SoHieuDauMay[..(gach + 1)] : "");
            txtSoHieu.Text = gach > 0 ? dm.SoHieuDauMay[(gach + 1)..] : dm.SoHieuDauMay;
            KhoaO(txtSoHieu);

            cboDongMay.SelectedItem = dongDauMay.FirstOrDefault(d => d.MaDongDauMay == dm.MaDongDauMay);
            cboDongMay.IsEnabled = false;
            cboDongMay.ToolTip = "Không đổi dòng máy của đầu máy đã có (tiền tố số hiệu, bằng lái của kíp đều theo dòng máy).";

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

        private static void KhoaO(TextBox o)
        {
            o.IsReadOnly = true;
            o.Background = (Brush)new BrushConverter().ConvertFrom("#F1F5F9")!;
            o.Foreground = (Brush)new BrushConverter().ConvertFrom("#475569")!;
        }

        private void DatTienToSoHieu(string tienTo)
        {
            txtTienToSoHieu.Text = tienTo;
            bdTienToSoHieu.Visibility = tienTo.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            txtSoHieu.MaxLength = Math.Max(1, PhuongTienService.DoDaiSoHieuToiDa - tienTo.Length);
        }

        private string GhepSoHieu()
            => txtTienToSoHieu.Text + txtSoHieu.Text.Trim().ToUpperInvariant();

        // =====================================================================
        // XEM TRUOC TRUC TIEP
        // =====================================================================

        private void TruongNhap_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangKhoiTao) return;

            // Go lai thi xoa loi cua truong do
            if (sender == txtSoHieu)
            {
                txtLoiSoHieu.Text = "";

                // Dan ca so hieu day du ("D19E-907") thi bo phan tien to da co san
                string tienTo = txtTienToSoHieu.Text;
                if (tienTo.Length > 0 && txtSoHieu.Text.StartsWith(tienTo, StringComparison.OrdinalIgnoreCase))
                {
                    txtSoHieu.Text = txtSoHieu.Text[tienTo.Length..];   // goi lai su kien nay
                    txtSoHieu.CaretIndex = txtSoHieu.Text.Length;
                    return;
                }
            }
            if (sender == txtNamSanXuat) txtLoiNamSanXuat.Text = "";
            if (sender == txtSoKm) txtLoiSoKm.Text = "";

            CapNhatXemTruoc();
        }

        private void CboDongMay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiDongMay.Text = "";

            // Them moi: tien to so hieu doi theo dong may, giu nguyen phan so da go
            if (_banGoc == null && cboDongMay.SelectedItem is DongDauMayMau dong)
            {
                DatTienToSoHieu($"{dong.MaDongCode}-");
                txtLoiSoHieu.Text = "";
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

            // --- Tien do bao duong: chu ky tu lan bao duong gan nhat (dau may moi: tu 0 km) ---
            decimal soKm = LaySoKm();
            var tienDo = PhuongTienService.TinhTienDoBaoDuong(soKm, _banGoc?.KmBaoDuongR1GanNhat, _banGoc?.KmBaoDuongR2GanNhat);
            var mau = (SolidColorBrush)new BrushConverter().ConvertFrom(DauMayHienThi.LayMauTheoMucCanhBao(tienDo.MucCanhBao))!;

            double phanTram = Math.Min(100.0, (double)tienDo.TyLePhanTram);
            txtCapKeTiep.Text = $"Cấp cần chú ý: {tienDo.Cap} · mốc {tienDo.MocKm:N0} km";
            txtTyLeBaoDuong.Text = $"{tienDo.TyLePhanTram:N1}%";
            txtTyLeBaoDuong.Foreground = mau;
            bdVachBaoDuong.Background = mau;
            colDaChay.Width = new GridLength(phanTram, GridUnitType.Star);
            colConLai.Width = new GridLength(100 - phanTram, GridUnitType.Star);

            string tuLan = tienDo.CoLichSu ? "kể từ lần bảo dưỡng gần nhất" : "chưa có lịch sử bảo dưỡng, tính từ 0 km";
            txtMoTaBaoDuong.Text = tienDo.MucCanhBao switch
            {
                "QUA_HAN" => $"Quá hạn {tienDo.Cap}: {tienDo.MoTaConLai} ({tuLan}).",
                "SAP_TOI" => $"Sắp tới hạn {tienDo.Cap}: {tienDo.MoTaConLai} ({tuLan}).",
                _ => $"Bình thường: {tienDo.MoTaConLai} tới mốc {tienDo.Cap} ({tuLan})."
            };

            // --- Tuoi khai thac ---
            txtTuoiKhaiThac.Text = int.TryParse(txtNamSanXuat.Text, out int nam) &&
                                   nam >= PhuongTienService.NamSanXuatNhoNhat && nam <= DateTime.Now.Year
                ? $"{DateTime.Now.Year - nam} năm"
                : "—";

            // --- Goi y chuyen trang thai khi sat / qua han bao duong ---
            bool canDuaVaoXuong = tienDo.MucCanhBao is "QUA_HAN" or "SAP_TOI";
            if (canDuaVaoXuong && rdoBaoDuong.IsChecked != true)
            {
                bdGoiYTrangThai.Visibility = Visibility.Visible;
                txtGoiYTrangThai.Text = tienDo.MucCanhBao == "QUA_HAN"
                    ? $"Đầu máy đã quá hạn bảo dưỡng {tienDo.Cap}. Nên chuyển sang trạng thái Bảo dưỡng trước khi vận dụng."
                    : $"Đầu máy sắp tới hạn bảo dưỡng {tienDo.Cap}. Cân nhắc đưa vào xưởng.";
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
            var dong = cboDongMay.SelectedItem as DongDauMayMau;
            bool namHopLe = int.TryParse(txtNamSanXuat.Text.Trim(), out int nam);

            var kq = _banGoc?.SaoChep() ?? new DauMayHienThi();
            kq.SoHieuDauMay = _banGoc?.SoHieuDauMay ?? GhepSoHieu();
            kq.DonViQuanLy = cboXiNghiep.Text.Trim();
            kq.NamSanXuat = namHopLe ? nam : 0;
            kq.SoKmTichLuy = LaySoKm();
            kq.TrangThai = rdoDangChay.IsChecked == true ? "DANG_CHAY"
                         : rdoBaoDuong.IsChecked == true ? "BAO_DUONG"
                         : "SAN_SANG";
            if (dong != null)
            {
                kq.MaDongDauMay = dong.MaDongDauMay;
                kq.MaDongCode = dong.MaDongCode;
                kq.NhaSanXuat = dong.NhaSanXuat;
                kq.CongSuatHP = dong.CongSuatHP;
                kq.TocDoToiDaKmh = dong.TocDoToiDaKmh;
                kq.SucKeoToiDaTan = dong.SucKeoToiDaTan;
                kq.DungTichBonDauLit = dong.DungTichBonDauLit;
                kq.TrongLuongTan = dong.TrongLuongTan;
                kq.ChieuDaiM = dong.ChieuDaiM;
            }

            // Cung bo kiem tra voi Service: bao loi canh tung o
            var dsLoi = PhuongTienService.KiemTraDauMay(kq.ToET(), dong?.MaDongCode, laThemMoi: _banGoc == null);
            if (!namHopLe)
                dsLoi.Insert(0, new LoiNhapLieuDTO("NamSanXuat", $"Năm sản xuất phải từ {PhuongTienService.NamSanXuatNhoNhat} đến {DateTime.Now.Year}."));
            if (HienLoi(dsLoi)) return;

            string? loi = _luu(kq);
            if (loi != null)
            {
                ThongBaoDialog.CanhBao(loi, "Chưa lưu được hồ sơ đầu máy", this);
                return;
            }

            kq.TuoiKhaiThac = DateTime.Now.Year - kq.NamSanXuat;
            kq.TinhLaiTienDoBaoDuong();
            KetQua = kq;
            DialogResult = true;
        }

        // Hien loi canh tung o; tra ve true neu co loi
        private bool HienLoi(List<LoiNhapLieuDTO> dsLoi)
        {
            txtLoiSoHieu.Text = txtLoiDongMay.Text = txtLoiXiNghiep.Text = txtLoiNamSanXuat.Text = txtLoiSoKm.Text = "";
            Control? oLoiDauTien = null;

            foreach (var l in dsLoi)
            {
                (TextBlock? tb, Control? o) = l.Truong switch
                {
                    "SoHieu" => (txtLoiSoHieu, (Control)txtSoHieu),
                    "DongMay" => (txtLoiDongMay, cboDongMay),
                    "XiNghiep" => (txtLoiXiNghiep, cboXiNghiep),
                    "NamSanXuat" => (txtLoiNamSanXuat, txtNamSanXuat),
                    "SoKm" => (txtLoiSoKm, txtSoKm),
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
