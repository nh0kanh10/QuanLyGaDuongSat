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
    // DIALOG LAP / CAP NHAT PHIEU CAP PHAT NHIEN LIEU (baotri.NhatKyCapNhienLieu)
    //
    // Giai doan dung giao dien: chi kiem tra du lieu nhap va tra ve doi tuong
    // CapNhienLieuHienThi. Suat tieu hao (SFC) va co vuot dinh muc duoc TINH
    // tu so lit / tan keo / cu ly (CSDL hien de nhap tay) - xem DinhMucNhienLieu.
    // =========================================================================
    public partial class CapNhienLieuDialog : Window
    {
        private readonly CapNhienLieuHienThi? _banGoc;   // null = lap phieu moi
        private readonly bool _dangKhoiTao;

        public CapNhienLieuHienThi? KetQua { get; private set; }

        public CapNhienLieuDialog(CapNhienLieuHienThi? banGoc, int? maDauMayMacDinh = null)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;

            ONhapSoThucHelper.Gan(txtSoLit, 2, coDinhSoLe: false);
            ONhapSoThucHelper.Gan(txtTanKeo, 2, coDinhSoLe: false);
            ONhapSoThucHelper.Gan(txtCuLy, 1, coDinhSoLe: false);

            cboDauMay.ItemsSource = DanhMucMauBaoTri.DauMay;
            cboNoiCap.ItemsSource = DanhMucMauBaoTri.NoiCapDau;
            cboNguoiCap.ItemsSource = DanhMucMauBaoTri.NguoiCapDau;

            if (banGoc == null)
                KhoiTaoThemMoi(maDauMayMacDinh);
            else
                KhoiTaoCapNhat(banGoc);

            NapDoanTauCuaDauMay();
            _dangKhoiTao = false;
            CapNhatXemTruoc();

            Loaded += (_, _) =>
            {
                Control oDau = cboDauMay.SelectedItem == null ? cboDauMay : txtSoLit;
                oDau.Focus();
                if (oDau is TextBox tb) tb.SelectAll();
            };
        }

        private void KhoiTaoThemMoi(int? maDauMay)
        {
            txtTieuDe.Text = "CẤP PHÁT NHIÊN LIỆU ĐẦU MÁY";
            txtTieuDePhu.Text = "Ghi nhận lượng dầu trả nạp và tính suất tiêu hao so với định mức";
            txtNutLuu.Text = "Lưu Phiếu Cấp Phát";
            txtThoiDiem.Text = DateTime.Now.ToString("HH:mm dd/MM/yyyy");
            txtGoiYThoiDiem.Text = "Tự ghi khi lưu";

            cboDauMay.SelectedItem = maDauMay.HasValue ? DanhMucMauBaoTri.TimDauMay(maDauMay.Value) : null;
            cboNoiCap.SelectedIndex = 0;
            cboNguoiCap.SelectedIndex = 0;
        }

        private void KhoiTaoCapNhat(CapNhienLieuHienThi nl)
        {
            txtTieuDe.Text = $"CẬP NHẬT PHIẾU {nl.MaPhieu} — {nl.SoHieuDauMay}";
            txtTieuDePhu.Text = "Đầu máy và thời điểm bơm là thông tin gốc của phiếu nên không sửa";
            txtNutLuu.Text = "Lưu Thay Đổi";
            txtThoiDiem.Text = nl.ThoiDiemBomDau.ToString("HH:mm dd/MM/yyyy");
            txtGoiYThoiDiem.Text = "Giữ nguyên thời điểm gốc";

            cboDauMay.SelectedItem = DanhMucMauBaoTri.TimDauMay(nl.MaDauMay);
            cboDauMay.IsEnabled = false;
            cboNoiCap.Text = nl.NoiCapDau;
            cboNguoiCap.SelectedItem = DanhMucMauBaoTri.TimNguoiCapDau(nl.MaNguoiCap);

            txtSoLit.Text = nl.SoLitTraNap.ToString("0.##", FormatHelper.TechnicalCulture);
            txtTanKeo.Text = nl.TrongLuongKeoTan.ToString("0.##", FormatHelper.TechnicalCulture);
            txtCuLy.Text = nl.CuLyChayKm.ToString("0.#", FormatHelper.TechnicalCulture);
        }

        // =====================================================================
        // DOAN TAU CUA DAU MAY (de dien nhanh trong luong keo)
        // =====================================================================

        private void NapDoanTauCuaDauMay()
        {
            var dm = cboDauMay.SelectedItem as DauMayCapDau;
            var ds = dm == null
                ? new List<DoanTauKhamXe>()
                : DanhMucMauBaoTri.DoanTau.Where(d => d.SoHieuDauMayChinh == dm.SoHieuDauMay ||
                                                      d.SoHieuDauMayDay == dm.SoHieuDauMay).ToList();

            cboDoanTau.ItemsSource = ds;
            cboDoanTau.SelectedIndex = ds.Count > 0 ? 0 : -1;
            cboDoanTau.IsEnabled = ds.Count > 0;
            btnLayTrongLuong.IsEnabled = ds.Count > 0;

            txtGoiYDoanTau.Text = dm == null ? "Chọn đầu máy để xem các đoàn tàu đã kéo."
                                : ds.Count == 0 ? $"{dm.SoHieuDauMay} chưa được phân công cho đoàn tàu nào."
                                : $"{ds.Count} đoàn tàu có {dm.SoHieuDauMay} (chính hoặc đẩy) — chọn rồi bấm \"Lấy trọng lượng\".";
        }

        private void BtnLayTrongLuong_Click(object sender, RoutedEventArgs e)
        {
            if (cboDoanTau.SelectedItem is not DoanTauKhamXe dt) return;
            txtTanKeo.Text = dt.TongTrongLuongTan.ToString("0.##", FormatHelper.TechnicalCulture);
            txtCuLy.Focus();
            txtCuLy.SelectAll();
        }

        // =====================================================================
        // XEM TRUOC TRUC TIEP
        // =====================================================================

        private void TruongNhap_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangKhoiTao) return;

            if (sender == txtSoLit) txtLoiSoLit.Text = "";
            if (sender == txtTanKeo) txtLoiTanKeo.Text = "";
            if (sender == txtCuLy) txtLoiCuLy.Text = "";

            CapNhatXemTruoc();
        }

        private void CboDauMay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiDauMay.Text = "";
            txtLoiSoLit.Text = "";
            NapDoanTauCuaDauMay();
            CapNhatXemTruoc();
        }

        private void CboNguoiCap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiNguoiCap.Text = "";
        }

        private void CapNhatXemTruoc()
        {
            var dm = cboDauMay.SelectedItem as DauMayCapDau;
            decimal? soLit = ONhapSoThucHelper.Lay(txtSoLit);
            decimal? tan = ONhapSoThucHelper.Lay(txtTanKeo);
            decimal? km = ONhapSoThucHelper.Lay(txtCuLy);

            decimal dinhMuc = dm?.DinhMucSfc ?? DinhMucNhienLieu.LayDinhMuc(null);
            decimal? sfc = soLit.HasValue && tan.HasValue && km.HasValue
                ? DinhMucNhienLieu.TinhSfc(soLit.Value, tan.Value, km.Value)
                : null;

            txtNhanDinhMuc.Text = $"▲ định mức {dinhMuc:N0}";

            // --- Suat tieu hao + thanh so voi dinh muc ---
            if (sfc.HasValue)
            {
                decimal tyLe = DinhMucNhienLieu.TinhTyLe(sfc.Value, dinhMuc);
                string muc = DinhMucNhienLieu.XepMuc(tyLe);
                string mau = DinhMucNhienLieu.MauChu(muc);

                txtSfc.Text = sfc.Value.ToString("N2");
                txtSfc.Foreground = Mau(mau);
                txtCongThuc.Text = $"= {soLit:N0} × 10,000 ÷ ({tan:N0} × {km:N1})";
                bdSfcVach.Background = Mau(mau);
                var (daDung, conLai) = DinhMucNhienLieu.TachThanh(tyLe);
                colSfcDaDung.Width = daDung;
                colSfcConLai.Width = conLai;

                if (sfc.Value > DinhMucNhienLieu.SfcGioiHan)
                {
                    DatKetLuan("VUOT", "SỐ LIỆU BẤT THƯỜNG",
                               $"Suất tiêu hao {sfc:N2} vượt giới hạn cột DECIMAL(5,2) — kiểm tra lại cự ly / trọng lượng.",
                               SymbolRegular.ErrorCircle24);
                }
                else
                {
                    switch (muc)
                    {
                        case "VUOT":
                            DatKetLuan(muc, $"VƯỢT ĐỊNH MỨC {tyLe - 100m:N1}%",
                                       "Bật cờ cảnh báo vượt mức · nên kiểm tra kỹ thuật đầu máy và cách vận hành.",
                                       SymbolRegular.Warning24);
                            break;
                        case "SAT":
                            DatKetLuan(muc, $"SÁT ĐỊNH MỨC ({tyLe:N0}%)",
                                       "Chưa vượt nhưng cần theo dõi ở các lần cấp tiếp theo.",
                                       SymbolRegular.Info24);
                            break;
                        default:
                            DatKetLuan(muc, $"TRONG ĐỊNH MỨC ({tyLe:N0}%)",
                                       "Tiêu hao bình thường, không bật cờ cảnh báo.",
                                       SymbolRegular.CheckmarkCircle24);
                            break;
                    }
                }
            }
            else
            {
                txtSfc.Text = "—";
                txtSfc.Foreground = Mau("#475569");
                txtCongThuc.Text = "= số lít × 10,000 ÷ (tấn kéo × km)";
                colSfcDaDung.Width = new GridLength(0, GridUnitType.Star);
                colSfcConLai.Width = new GridLength((double)DinhMucNhienLieu.ThangHienThiToiDa, GridUnitType.Star);
                DatKetLuan("", "CHỜ NHẬP SỐ LIỆU", "Nhập số lít, trọng lượng kéo và cự ly đã chạy.", SymbolRegular.Info24);
            }

            // --- Muc nap so voi bon dau ---
            if (dm != null && dm.DungTichBonDauLit > 0)
            {
                decimal lit = soLit ?? 0m;
                double phanTram = (double)Math.Round(lit / dm.DungTichBonDauLit * 100m, 1);
                bool vuotBon = lit > dm.DungTichBonDauLit;

                txtBon.Text = $"{lit:N0} / {dm.DungTichBonDauLit:N0} lít";
                txtBonTyLe.Text = $"{phanTram:N1}%";
                txtBonTyLe.Foreground = Mau(vuotBon ? "#B91C1C" : "#0284C7");
                bdBonVach.Background = Mau(vuotBon ? "#DC2626" : "#0284C7");
                double ve = Math.Min(phanTram, 100);
                colBonDaNap.Width = new GridLength(ve, GridUnitType.Star);
                colBonConLai.Width = new GridLength(100 - ve, GridUnitType.Star);
            }
            else
            {
                txtBon.Text = "Chọn đầu máy để xem dung tích bồn";
                txtBonTyLe.Text = "";
                colBonDaNap.Width = new GridLength(0, GridUnitType.Star);
                colBonConLai.Width = new GridLength(100, GridUnitType.Star);
            }

            // --- Thong tin dau may ---
            pnlDauMay.Children.Clear();
            if (dm != null)
            {
                var dong = DanhMucMauPhuongTien.TimDongDauMay(dm.MaDongCode);
                ThemDongThongSo("Dòng máy", dong != null ? $"{dm.MaDongCode} · {dong.CongSuatHP:N0} HP" : dm.MaDongCode);
                ThemDongThongSo("Xí nghiệp", dm.DonViQuanLy);
                ThemDongThongSo("Dung tích bồn dầu", $"{dm.DungTichBonDauLit:N0} lít");
                ThemDongThongSo("Định mức (tham khảo)", $"{dm.DinhMucSfc:N0} L/10,000 t·km", laDongCuoi: true);
            }
            else
            {
                pnlDauMay.Children.Add(new TextBlock
                {
                    Text = "Chọn đầu máy để xem thông số.",
                    FontSize = 10,
                    Foreground = Mau("#94A3B8"),
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 6, 0, 6)
                });
            }
        }

        private void DatKetLuan(string muc, string tieuDe, string moTa, SymbolRegular bieuTuong)
        {
            string chu = muc.Length == 0 ? "#475569" : DinhMucNhienLieu.MauChu(muc);
            bdKetLuan.Background = Mau(DinhMucNhienLieu.MauNen(muc));
            bdKetLuan.BorderBrush = Mau(DinhMucNhienLieu.MauVien(muc));
            icoKetLuan.Symbol = bieuTuong;
            icoKetLuan.Foreground = Mau(muc.Length == 0 ? "#94A3B8" : chu);
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

            pnlDauMay.Children.Add(new Border
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
            if (!KiemTraHopLe(out var dauMay, out string noiCap, out var nguoiCap,
                              out decimal soLit, out decimal tanKeo, out decimal cuLy))
                return;

            var kq = _banGoc?.SaoChep() ?? new CapNhienLieuHienThi { ThoiDiemBomDau = DateTime.Now };

            kq.GanDauMay(dauMay);
            kq.NoiCapDau = noiCap;
            kq.MaNguoiCap = nguoiCap.MaTaiKhoan;
            kq.TenNguoiCap = nguoiCap.HoTenHienThi;
            kq.SoLitTraNap = soLit;
            kq.TrongLuongKeoTan = tanKeo;
            kq.CuLyChayKm = cuLy;
            kq.TinhLaiSuatTieuHao();
            kq.LaThayDoiTam = true;

            KetQua = kq;
            DialogResult = true;
        }

        private bool KiemTraHopLe(out DauMayCapDau dauMay, out string noiCap, out TaiKhoanMau nguoiCap,
                                  out decimal soLit, out decimal tanKeo, out decimal cuLy)
        {
            bool hopLe = true;
            Control? oLoiDauTien = null;

            void BaoLoi(TextBlock tbLoi, string thongDiep, Control o)
            {
                tbLoi.Text = thongDiep;
                hopLe = false;
                oLoiDauTien ??= o;
            }

            // Dau may (FK MaDauMay NOT NULL)
            dauMay = (cboDauMay.SelectedItem as DauMayCapDau)!;
            if (dauMay == null)
                BaoLoi(txtLoiDauMay, "Vui lòng chọn đầu máy được cấp dầu.", cboDauMay);

            // Noi cap dau (NVARCHAR(100) NOT NULL)
            noiCap = cboNoiCap.Text.Trim();
            if (noiCap.Length == 0)
                BaoLoi(txtLoiNoiCap, "Vui lòng chọn hoặc nhập nơi cấp dầu.", cboNoiCap);
            else if (noiCap.Length > 100)
                BaoLoi(txtLoiNoiCap, $"Tối đa 100 ký tự (đang có {noiCap.Length}).", cboNoiCap);
            else
                txtLoiNoiCap.Text = "";

            // Nguoi cap (FK TaiKhoan - CSDL cho NULL nhung giao dien yeu cau ghi ro)
            nguoiCap = (cboNguoiCap.SelectedItem as TaiKhoanMau)!;
            if (nguoiCap == null)
                BaoLoi(txtLoiNguoiCap, "Vui lòng chọn người cấp phát.", cboNguoiCap);

            // So lit: CHECK > 0, DECIMAL(8,2), khong vuot dung tich bon cua dong may
            soLit = 0m;
            decimal? l = ONhapSoThucHelper.Lay(txtSoLit);
            if (!l.HasValue)
                BaoLoi(txtLoiSoLit, "Vui lòng nhập số lít trả nạp.", txtSoLit);
            else if (l <= 0 || l > DinhMucNhienLieu.SoLitGioiHan)
                BaoLoi(txtLoiSoLit, "Số lít phải lớn hơn 0.", txtSoLit);
            else if (ONhapSoThucHelper.QuaSoChuSoLe(l.Value, 2))
                BaoLoi(txtLoiSoLit, "Tối đa 2 chữ số thập phân (DECIMAL(8,2)).", txtSoLit);
            else if (dauMay != null && dauMay.DungTichBonDauLit > 0 && l > dauMay.DungTichBonDauLit)
                BaoLoi(txtLoiSoLit, $"Vượt dung tích bồn dầu {dauMay.DungTichBonDauLit:N0} lít của dòng {dauMay.MaDongCode}.", txtSoLit);
            else
                soLit = l.Value;

            // Trong luong keo: CHECK > 0, DECIMAL(7,2)
            tanKeo = 0m;
            decimal? t = ONhapSoThucHelper.Lay(txtTanKeo);
            if (!t.HasValue)
                BaoLoi(txtLoiTanKeo, "Vui lòng nhập trọng lượng kéo.", txtTanKeo);
            else if (t <= 0 || t > DinhMucNhienLieu.TrongLuongGioiHan)
                BaoLoi(txtLoiTanKeo, $"Trọng lượng phải lớn hơn 0 và không quá {DinhMucNhienLieu.TrongLuongGioiHan:N2} tấn.", txtTanKeo);
            else if (ONhapSoThucHelper.QuaSoChuSoLe(t.Value, 2))
                BaoLoi(txtLoiTanKeo, "Tối đa 2 chữ số thập phân (DECIMAL(7,2)).", txtTanKeo);
            else
                tanKeo = t.Value;

            // Cu ly: CHECK > 0, DECIMAL(6,1)
            cuLy = 0m;
            decimal? k = ONhapSoThucHelper.Lay(txtCuLy);
            if (!k.HasValue)
                BaoLoi(txtLoiCuLy, "Vui lòng nhập cự ly đã chạy.", txtCuLy);
            else if (k <= 0 || k > DinhMucNhienLieu.CuLyGioiHan)
                BaoLoi(txtLoiCuLy, $"Cự ly phải lớn hơn 0 và không quá {DinhMucNhienLieu.CuLyGioiHan:N1} km.", txtCuLy);
            else if (ONhapSoThucHelper.QuaSoChuSoLe(k.Value, 1))
                BaoLoi(txtLoiCuLy, "Tối đa 1 chữ số thập phân (DECIMAL(6,1)).", txtCuLy);
            else
                cuLy = k.Value;

            // Suat tieu hao tinh ra phai vua cot DECIMAL(5,2)
            if (hopLe)
            {
                decimal sfc = DinhMucNhienLieu.TinhSfc(soLit, tanKeo, cuLy) ?? 0m;
                if (sfc > DinhMucNhienLieu.SfcGioiHan)
                    BaoLoi(txtLoiCuLy, $"Suất tiêu hao tính ra {sfc:N2} vượt giới hạn 999.99 — kiểm tra lại cự ly / trọng lượng.", txtCuLy);
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
