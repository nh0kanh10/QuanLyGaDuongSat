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
    // DIALOG LAP / CAP NHAT PHIEU CAP PHAT NHIEN LIEU (baotri.NhatKyCapNhienLieu)
    //
    // Tu 24/10: kiem tra bang QuyTacBaoTri.KiemTraCapDau (cung ham Service dung) roi goi ham
    // luu cua trang (BaoTriService.LuuCapDau). Suat tieu hao (SFC) + co vuot dinh muc + thanh
    // tien do Service tinh lai khi ghi; o day chi xem truoc. Dinh muc + dung tich bon doc tu
    // dong may (phuongtien.DongDauMay). Sua: giu dau may + thoi diem bom, ghi theo PhienBan.
    // =========================================================================
    public partial class CapNhienLieuDialog : Window
    {
        private readonly CapNhienLieuHienThi? _banGoc;   // null = lap phieu moi
        private readonly BaoTriDuLieu _duLieu;
        private readonly Func<CapNhienLieuDTO, string?> _luu;
        private readonly bool _dangKhoiTao;

        public CapNhienLieuDialog(CapNhienLieuHienThi? banGoc, BaoTriDuLieu duLieu, Func<CapNhienLieuDTO, string?> luu,
                                  int? maDauMayMacDinh = null)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;
            _duLieu = duLieu;
            _luu = luu;

            ONhapSoThucHelper.Gan(txtSoLit, 2, coDinhSoLe: false);
            ONhapSoThucHelper.Gan(txtTanKeo, 2, coDinhSoLe: false);
            ONhapSoThucHelper.Gan(txtCuLy, 1, coDinhSoLe: false);
            ONhapSoThucHelper.Gan(txtDonGia, 0, coDinhSoLe: false);

            cboNoiCap.ItemsSource = duLieu.NoiCapDau;

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
            txtGoiYThoiDiem.Text = "Giờ máy chủ lúc lưu";

            // Dau may ngung khai thac khong cap dau nua
            var dsDauMay = _duLieu.DauMayKhaiThac;
            cboDauMay.ItemsSource = dsDauMay;
            cboNguoiCap.ItemsSource = _duLieu.NguoiKyThuat;

            cboDauMay.SelectedItem = maDauMay.HasValue ? dsDauMay.FirstOrDefault(d => d.MaDauMay == maDauMay) : null;
            cboNoiCap.SelectedIndex = 0;
            cboNguoiCap.SelectedIndex = _duLieu.NguoiKyThuat.Count > 0 ? 0 : -1;
            if (_duLieu.NguoiKyThuat.Count == 0)
                txtLoiNguoiCap.Text = "Chưa có tài khoản kỹ thuật (vai trò KY_THUAT) đang hoạt động.";
            txtDonGia.Text = DinhMucNhienLieu.DonGiaMacDinhVndLit.ToString("0", FormatHelper.TechnicalCulture);
        }

        private void KhoiTaoCapNhat(CapNhienLieuHienThi nl)
        {
            txtTieuDe.Text = $"CẬP NHẬT PHIẾU {nl.MaPhieu} — {nl.SoHieuDauMay}";
            txtTieuDePhu.Text = "Đầu máy và thời điểm bơm là thông tin gốc của phiếu nên không sửa";
            txtNutLuu.Text = "Lưu Thay Đổi";
            txtThoiDiem.Text = nl.ThoiDiemBomDau.ToString("HH:mm dd/MM/yyyy");
            txtGoiYThoiDiem.Text = "Giữ nguyên thời điểm gốc";

            var dm = _duLieu.TimDauMay(nl.MaDauMay);
            cboDauMay.ItemsSource = dm != null ? new List<DauMayCapDau> { dm } : new List<DauMayCapDau>();
            cboDauMay.SelectedItem = dm;
            cboDauMay.IsEnabled = false;
            cboNoiCap.Text = nl.NoiCapDau;

            // Nguoi cap cu khong con trong danh sach chon duoc (vd admin o seed goc) thi van hien de giu nguyen
            var dsNguoi = _duLieu.NguoiKyThuat.ToList();
            if (nl.MaNguoiCap.HasValue && dsNguoi.All(n => n.MaTaiKhoan != nl.MaNguoiCap))
                dsNguoi.Insert(0, new TaiKhoanMau(nl.MaNguoiCap.Value, "", nl.TenNguoiCap.Length > 0 ? nl.TenNguoiCap : $"Tài khoản #{nl.MaNguoiCap}", "người cấp cũ"));
            cboNguoiCap.ItemsSource = dsNguoi;
            cboNguoiCap.SelectedItem = dsNguoi.FirstOrDefault(n => n.MaTaiKhoan == nl.MaNguoiCap);

            txtSoLit.Text = nl.SoLitTraNap.ToString("0.##", FormatHelper.TechnicalCulture);
            txtTanKeo.Text = nl.TrongLuongKeoTan.ToString("0.##", FormatHelper.TechnicalCulture);
            txtCuLy.Text = nl.CuLyChayKm.ToString("0.#", FormatHelper.TechnicalCulture);
            decimal donGia = nl.DonGiaVndLit > 0 ? nl.DonGiaVndLit : DinhMucNhienLieu.DonGiaMacDinhVndLit;
            txtDonGia.Text = donGia.ToString("0", FormatHelper.TechnicalCulture);
        }

        // =====================================================================
        // DOAN TAU CUA DAU MAY (de dien nhanh trong luong keo)
        // =====================================================================

        private void NapDoanTauCuaDauMay()
        {
            var dm = cboDauMay.SelectedItem as DauMayCapDau;
            var ds = dm == null
                ? new List<DoanTauKhamXe>()
                : _duLieu.DoanTau.Where(d => d.MaDauMayChinh == dm.MaDauMay || d.MaDauMayDay == dm.MaDauMay)
                                 .OrderByDescending(d => d.GioXuatPhatKH).ToList();

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
            if (sender == txtDonGia) txtLoiDonGia.Text = "";

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

            decimal dinhMuc = dm?.DinhMucSfc ?? 0m;
            decimal? sfc = soLit.HasValue && tan.HasValue && km.HasValue
                ? DinhMucNhienLieu.TinhSfc(soLit.Value, tan.Value, km.Value)
                : null;

            txtNhanDinhMuc.Text = dm != null ? $"▲ định mức {dinhMuc:N0}" : "▲ định mức (chọn đầu máy)";

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

            // --- Thanh tien = so lit x don gia ---
            decimal? donGia = ONhapSoThucHelper.Lay(txtDonGia);
            txtThanhTien.Text = soLit.HasValue && donGia.HasValue
                ? $"{soLit.Value * donGia.Value:N0} đ"
                : "—";

            // --- Thong tin dau may ---
            pnlDauMay.Children.Clear();
            if (dm != null)
            {
                ThemDongThongSo("Dòng máy", $"{dm.MaDongCode} · {dm.CongSuatHP:N0} HP");
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
            if (!KiemTraHopLe(out var nl)) return;

            // Ghi CSDL qua trang (BaoTriService). Loi nghiep vu / CSDL: bao va giu dialog de sua.
            string? loi = _luu(nl);
            if (loi != null)
            {
                ThongBaoDialog.CanhBao(loi, "Chưa lưu được phiếu cấp phát");
                return;
            }
            DialogResult = true;
        }

        private bool KiemTraHopLe(out CapNhienLieuDTO nl)
        {
            bool hopLe = true;
            Control? oLoiDauTien = null;

            void BaoLoi(TextBlock tbLoi, string thongDiep, Control o)
            {
                if (tbLoi.Text.Length == 0) tbLoi.Text = thongDiep;
                hopLe = false;
                oLoiDauTien ??= o;
            }

            foreach (var tb in new[] { txtLoiDauMay, txtLoiNoiCap, txtLoiNguoiCap, txtLoiSoLit, txtLoiTanKeo, txtLoiCuLy, txtLoiDonGia })
                tb.Text = "";

            decimal? soLit = ONhapSoThucHelper.Lay(txtSoLit);
            decimal? tan = ONhapSoThucHelper.Lay(txtTanKeo);
            decimal? km = ONhapSoThucHelper.Lay(txtCuLy);
            decimal? donGia = ONhapSoThucHelper.Lay(txtDonGia);
            if (!soLit.HasValue) BaoLoi(txtLoiSoLit, "Vui lòng nhập số lít trả nạp.", txtSoLit);
            if (!tan.HasValue) BaoLoi(txtLoiTanKeo, "Vui lòng nhập trọng lượng kéo.", txtTanKeo);
            if (!km.HasValue) BaoLoi(txtLoiCuLy, "Vui lòng nhập cự ly đã chạy.", txtCuLy);
            if (!donGia.HasValue) BaoLoi(txtLoiDonGia, "Vui lòng nhập đơn giá dầu.", txtDonGia);

            var dm = cboDauMay.SelectedItem as DauMayCapDau;
            nl = _banGoc?.ToDTO() ?? new CapNhienLieuDTO();
            nl.MaDauMay = dm?.MaDauMay ?? 0;
            nl.NoiCapDau = cboNoiCap.Text.Trim();
            nl.MaNguoiCap = (cboNguoiCap.SelectedItem as TaiKhoanMau)?.MaTaiKhoan;
            nl.SoLitTraNap = soLit ?? 1m;
            nl.TrongLuongKeoTan = tan ?? 1m;
            nl.CuLyChayKm = km ?? 1m;
            nl.DonGiaVndLit = donGia ?? 1m;

            // Cung luat voi Service (dialog chi doi ten truong sang o nhap)
            foreach (var l in QuyTacBaoTri.KiemTraCapDau(nl, dm != null ? _duLieu.Goc.TimDauMay(dm.MaDauMay) : null))
            {
                switch (l.Truong)
                {
                    case "DauMay": BaoLoi(txtLoiDauMay, l.ThongDiep, cboDauMay); break;
                    case "NoiCap": BaoLoi(txtLoiNoiCap, l.ThongDiep, cboNoiCap); break;
                    case "NguoiCap": BaoLoi(txtLoiNguoiCap, l.ThongDiep, cboNguoiCap); break;
                    case "SoLit" when soLit.HasValue: BaoLoi(txtLoiSoLit, l.ThongDiep, txtSoLit); break;
                    case "TanKeo" when tan.HasValue: BaoLoi(txtLoiTanKeo, l.ThongDiep, txtTanKeo); break;
                    case "CuLy" when km.HasValue && soLit.HasValue && tan.HasValue: BaoLoi(txtLoiCuLy, l.ThongDiep, txtCuLy); break;
                    case "DonGia" when donGia.HasValue: BaoLoi(txtLoiDonGia, l.ThongDiep, txtDonGia); break;
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
