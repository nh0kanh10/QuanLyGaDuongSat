using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BUS.AI;
using DTO.AI;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // TRO LY AI TU VAN VE (Sprint 3, Dat - FR-8.1 / UC-05). Mo tu trang Ban ve (nut "Tu van AI").
    // Moi luat cham diem o BUS/AI (QuyTacTuVanVe, TuVanVeService); dialog chi nhap nhu cau va hien 3 phuong an.
    // "Chon phuong an nay" -> PhuongAnDuocChon, trang Ban ve tu chon chuyen / toa / ghe vao gio (khong giu cho).
    // =========================================================================
    public partial class TuVanVeDialog : Window
    {
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
        private readonly TuVanVeService _sv = new();
        private readonly bool _choPhepChon;
        private YeuCauTuVanVe? _ycCuoi;

        public PhuongAnDuocChon? PhuongAn { get; private set; }

        private sealed record MucGa(int MaGa, string Nhan);

        public TuVanVeDialog(int? maGaDi = null, int? maGaDen = null, DateTime? ngay = null, bool choPhepChon = true)
        {
            InitializeComponent();
            _choPhepChon = choPhepChon;
            if (_sv.LayDanhSachGa(out var dsGa, out string loiGa))
            {
                var ga = dsGa.Select(g => new MucGa(g.MaGa, $"{g.TenGa} ({g.MaGaCode})")).ToList();
                cboGaDi.ItemsSource = ga;
                cboGaDen.ItemsSource = ga;
                cboGaDi.SelectedItem = ga.FirstOrDefault(g => g.MaGa == maGaDi) ?? ga.FirstOrDefault();
                cboGaDen.SelectedItem = ga.FirstOrDefault(g => g.MaGa == maGaDen) ?? ga.LastOrDefault();
            }
            else txtLoi.Text = loiGa;
            dpNgayDi.SelectedDate = ngay.HasValue && ngay.Value.Date >= DateTime.Today ? ngay.Value.Date : DateTime.Today;
            dpNgayDi.DisplayDateStart = DateTime.Today;
            if (!choPhepChon) txtChanTrang.Text += " Mở từ trang Bán vé để chọn phương án vào giỏ.";
            HienTrongSo(QuyTacTuVanVe.TrongSoMacDinh);
            DoiTuong_Changed(this, new RoutedEventArgs());
        }

        // [12/11] Hien loai khach du kien (gio ve dat san doi tuong + gia sau giam theo dong nay)
        private void DoiTuong_Changed(object sender, RoutedEventArgs e)
        {
            if (txtLoaiKhachDuKien == null || cboSoKhach == null || chkCaoTuoi == null || chkTreEm == null || chkSinhVien == null) return;
            var ds = QuyTacTuVanVe.LoaiKhachDuKien(cboSoKhach.SelectedIndex + 1, chkCaoTuoi.IsChecked == true,
                                                  chkTreEm.IsChecked == true, chkSinhVien.IsChecked == true);
            txtLoaiKhachDuKien.Text = "Vé dự kiến: " + QuyTacTuVanVe.MoTaLoaiKhach(ds) + " (giá so ngân sách đã trừ giảm giá)";
        }

        private YeuCauTuVanVe DocYeuCau()
        {
            decimal? toiDa = decimal.TryParse(txtNganSachToiDa.Text.Trim().Replace(",", "").Replace(".", "").Replace(" ", ""), out var v) && v > 0 ? v : null;
            return new YeuCauTuVanVe
            {
                MaGaDi = (cboGaDi.SelectedItem as MucGa)?.MaGa ?? 0,
                MaGaDen = (cboGaDen.SelectedItem as MucGa)?.MaGa ?? 0,
                NgayDi = dpNgayDi.SelectedDate ?? DateTime.Today,
                SoNguoi = cboSoKhach.SelectedIndex + 1,
                CoNguoiCaoTuoi = chkCaoTuoi.IsChecked == true,
                CoTreEm = chkTreEm.IsChecked == true,
                LaSinhVien = chkSinhVien.IsChecked == true,
                MangXeMay = chkXeMay.IsChecked == true,
                MucNganSach = (cboNganSach.SelectedItem as ComboBoxItem)?.Tag as string ?? MucNganSach.TieuChuan,
                NganSachToiDaMoiNguoi = toiDa,
                KhungGio = (cboKhungGio.SelectedItem as ComboBoxItem)?.Tag as string ?? KhungGioUuTien.KhongUuTien,
                TrongSo = chkTuChinh.IsChecked == true ? new[] { sldW0.Value, sldW1.Value, sldW2.Value, sldW3.Value } : null
            };
        }

        private void BtnTuVan_Click(object sender, RoutedEventArgs e)
        {
            txtLoi.Text = "";
            // [12/11] Dan chu vao o ngan sach (PreviewTextInput khong chan dan) -> truoc day bi bo qua im lang = khong gioi han
            string ns = txtNganSachToiDa.Text.Trim().Replace(",", "").Replace(".", "").Replace(" ", "");
            if (ns.Length > 0 && (!ns.All(char.IsDigit) || !decimal.TryParse(ns, out var nsSo) || nsSo <= 0))
            {
                txtLoi.Text = "Ngân sách tối đa mỗi vé chỉ gồm chữ số, lớn hơn 0 (ví dụ 900000). Bỏ trống nếu không giới hạn.";
                txtNganSachToiDa.Focus();
                txtNganSachToiDa.SelectAll();
                return;
            }
            var yc = DocYeuCau();
            Mouse.OverrideCursor = Cursors.Wait;
            bool ok;
            KetQuaTuVanVe kq;
            string loi;
            try { ok = _sv.TuVan(yc, out kq, out loi); }
            finally { Mouse.OverrideCursor = null; }
            if (!ok) { txtLoi.Text = loi; return; }

            _ycCuoi = yc;
            txtKqTieuDe.Text = kq.CacPhuongAn.Count == 0
                ? $"Không tìm được phương án cho {kq.TenGaDi} → {kq.TenGaDen}"
                : $"{kq.CacPhuongAn.Count} phương án gợi ý · {kq.TenGaDi} → {kq.TenGaDen} · {kq.CuLyKm.ToString("N0", Vi)} km";
            txtKqPhu.Text = $"Đã xét {kq.SoPhuongAnDaXet} phương án (chuyến × loại chỗ × tầng) trên {kq.SoChuyenDaXet} chuyến ngày {yc.NgayDi:dd/MM/yyyy} · " +
                            "trọng số " + string.Join(" · ", kq.TrongSoDaDung.Select((w, i) => $"{QuyTacTuVanVe.TenTieuChi[i].ToLower(Vi)} {w:P0}"));
            icGhiChu.ItemsSource = kq.GhiChu;
            icPhuongAn.ItemsSource = kq.CacPhuongAn.Select(p => new TheHienThi(p, _choPhepChon, kq.TrongSoDaDung)).ToList();
            if (chkTuChinh.IsChecked != true) HienTrongSo(kq.TrongSoDaDung);
        }

        private void BtnChon_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not TheHienThi the || _ycCuoi == null) return;
            var p = the.Goc;
            PhuongAn = new PhuongAnDuocChon(p.MaChuyenTau, _ycCuoi.MaGaDi, _ycCuoi.MaGaDen, _ycCuoi.NgayDi,
                                            p.MaToaXeKhach, p.LoaiToa, p.CacMaChoNgoi, p.CacLoaiKhach);
            DialogResult = true;
        }

        // --- Trong so ----------------------------------------------------------
        private void ChkTuChinh_Changed(object sender, RoutedEventArgs e)
        {
            pnlTrongSo.IsEnabled = chkTuChinh.IsChecked == true;
            txtGoiYTrongSo.Text = chkTuChinh.IsChecked == true
                ? "Kéo thanh trượt — trọng số được chuẩn hóa để tổng = 100%."
                : "Để trống: AI tự gán trọng số theo hồ sơ khách (ngân sách, người cao tuổi, khung giờ).";
            CapNhatNhanTrongSo();
        }

        private void TrongSo_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => CapNhatNhanTrongSo();

        private void HienTrongSo(double[] w)
        {
            sldW0.Value = w[0] * 100; sldW1.Value = w[1] * 100; sldW2.Value = w[2] * 100; sldW3.Value = w[3] * 100;
            CapNhatNhanTrongSo();
        }

        private void CapNhatNhanTrongSo()
        {
            if (sldW0 == null || sldW1 == null || sldW2 == null || sldW3 == null || txtW3 == null) return;   // dang dung cay XAML
            var w = QuyTacTuVanVe.ChuanHoa(new[] { sldW0.Value, sldW1.Value, sldW2.Value, sldW3.Value });
            txtW0.Text = $"{w[0]:P0}"; txtW1.Text = $"{w[1]:P0}"; txtW2.Text = $"{w[2]:P0}"; txtW3.Text = $"{w[3]:P0}";
        }

        private void ChiNhapSo(object sender, TextCompositionEventArgs e) => e.Handled = !e.Text.All(char.IsDigit);

        private void BtnDong_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { e.Handled = true; DialogResult = false; }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control) { e.Handled = true; BtnTuVan_Click(this, new RoutedEventArgs()); }
        }

        // --- The ket qua ---------------------------------------------------------
        private sealed class TheHienThi
        {
            public TheHienThi(PhuongAnVe p, bool choPhepChon, double[] w)
            {
                Goc = p;
                HienNutChon = choPhepChon ? Visibility.Visible : Visibility.Collapsed;
                TieuChi = Enumerable.Range(0, 4).Select(i => new TieuChiHienThi(
                    $"{TenNgan[i]} ({w[i]:P0})", p.DiemTieuChi[i])).ToList();
            }
            public PhuongAnVe Goc { get; }
            public Visibility HienNutChon { get; }
            public List<TieuChiHienThi> TieuChi { get; }
            public List<string> LyDo => Goc.LyDo;
            public string NhanHang => Goc.XepHang == 1 ? "KHUYÊN DÙNG" : $"PHƯƠNG ÁN {Goc.XepHang}";
            public Brush MauHang => Goc.XepHang == 1 ? Mau("#15803D") : Mau("#0284C7");
            public Brush MauVien => Goc.XepHang == 1 ? Mau("#16A34A") : Mau("#CBD5E1");
            public Thickness DoDayVien => Goc.XepHang == 1 ? new Thickness(2) : new Thickness(1);
            public string TieuDe => $"{Goc.SoHieuMacTau} · {Goc.TenLoaiCho}";
            public string DongGio => $"Đi {Goc.GioDi:HH:mm dd/MM} → đến {Goc.GioDen:HH:mm dd/MM} · khoảng {Goc.SoGioChay:0.#} giờ" +
                                     (Goc.QuaDem ? " · chạy qua đêm" : "");
            public string DongGhe => Goc.NhanGhe;
            public string NhanDiem => $"{Goc.Diem:0.00}/10";
            public string NhanGia => $"{Goc.GiaMotVe.ToString("N0", Vi)} đ/vé";
            // [12/11] Tong tien da tru giam gia theo doi tuong (gio ve dat san loai khach giong the nay)
            public string NhanTongTien => Goc.CacMaChoNgoi.Count > 1
                    ? $"{Goc.CacMaChoNgoi.Count} vé: {Goc.TongTien.ToString("N0", Vi)} đ" + (Goc.CoGiamGia ? " (đã giảm)" : "") +
                      (Goc.VuotNganSach ? " · vượt ngân sách" : "")
                : Goc.CoGiamGia ? $"sau giảm {Goc.TongTien.ToString("N0", Vi)} đ" + (Goc.VuotNganSach ? " · vượt ngân sách" : "")
                : Goc.VuotNganSach ? "vượt ngân sách" : "";
        }

        private static readonly string[] TenNgan = { "Tiện nghi", "Ngân sách", "Giờ / ngủ đêm", "Sức khỏe" };

        private sealed record TieuChiHienThi(string Ten, double Diem)
        {
            public double DoRong => Math.Max(0, Diem) * 8.5;      // thanh 85 px = 10 diem
            public string NhanDiem => Diem.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static SolidColorBrush Mau(string ma) => (SolidColorBrush)new BrushConverter().ConvertFrom(ma)!;
    }
}
