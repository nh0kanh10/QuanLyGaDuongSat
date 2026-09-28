using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GUI.Views.Dialogs;
using GUI.Views.KyThuat.Models;

namespace GUI.Views.KyThuat
{
    // =========================================================================
    // TAB 3: KIEM TRA LEN BAN (nhansu.KiemTraSucKhoe)
    // Hang cho = kip DA_PHAN_CONG cua chuyen con hieu luc, xep theo gio nhan ban.
    // So kiem tra = moi lan do da ghi (khong sua, do lai thi ghi lan moi).
    // =========================================================================
    public partial class NhanSuPage
    {
        private void LamMoiKiemTra(bool giuDongChon, int? maPhanCongChon = null)
        {
            int? maChon = maPhanCongChon ?? (giuDongChon ? (dgHangCho.SelectedItem as KipLaiDong)?.PhanCong.MaPhanCong : null);

            var hangCho = _tinhTrangChuyen.Where(t => t.ConHieuLuc)
                                          .SelectMany(t => t.Kip)
                                          .Where(k => k.LaDaPhanCong)
                                          .OrderBy(k => k.PhanCong.GioNhanBan)
                                          .ToList();

            dgHangCho.ItemsSource = hangCho;
            var chon = hangCho.FirstOrDefault(k => k.PhanCong.MaPhanCong == maChon) ?? hangCho.FirstOrDefault();
            dgHangCho.SelectedItem = chon;
            if (chon != null) dgHangCho.ScrollIntoView(chon);
            txtTrongHangCho.Visibility = hangCho.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            int choKiemTra = DemNguoiChoKiemTra();
            txtBadgeKiemTra.Text = choKiemTra.ToString();
            bdBadgeKiemTra.Background = Mau(choKiemTra > 0 ? "#FEF3C7" : "#E2E8F0");
            txtBadgeKiemTra.Foreground = Mau(choKiemTra > 0 ? "#92400E" : "#334155");

            NapSoKiemTra();
            CapNhatNutHangCho();
        }

        private void NapSoKiemTra()
        {
            string tuKhoa = BoDau(txtTimKiemTra.Text?.Trim() ?? string.Empty);
            string ketLuan = LayTag(cboKetLuanKiemTra) ?? "ALL";

            var ds = DuLieu.KiemTra
                .Where(k => tuKhoa.Length == 0
                            || k.MaPhieu.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase)
                            || k.MaNVCode.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase)
                            || BoDau(k.HoTen).Contains(tuKhoa, StringComparison.OrdinalIgnoreCase)
                            || k.SoHieuMacTau.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
                .Where(k => ketLuan == "ALL" || (ketLuan == "DAT") == k.DuDieuKien)
                .OrderByDescending(k => k.ThoiDiemKiemTra)
                .ToList();

            dgKiemTra.ItemsSource = ds;
            txtTrongKiemTra.Visibility = ds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DgHangCho_SelectionChanged(object sender, SelectionChangedEventArgs e) => CapNhatNutHangCho();

        private void CapNhatNutHangCho()
        {
            var k = dgHangCho.SelectedItem as KipLaiDong;
            btnGhiKiemTra.IsEnabled = k != null && k.ThanhVien.Any(t => t.NhanVien != null && t.KiemTra.ChuaKiemTra);
            btnThayNguoi.IsEnabled = k != null && k.ThanhVien.Any(CanThayNguoi);
            btnNhanBanHangCho.IsEnabled = k != null;
        }

        // Nguoi khong dat kiem tra hoac vuong loi phan cong (trung chuyen, het han kham...)
        private static bool CanThayNguoi(ThanhVienKip t) => t.KiemTra.KhongDat || t.LoiPhanCong != null;

        private void ChonHangChoCanKiemTra()
        {
            if (dgHangCho.ItemsSource is not IEnumerable<KipLaiDong> ds) return;
            var k = ds.FirstOrDefault(x => x.ThanhVien.Any(t => t.NhanVien != null && t.KiemTra.ChuaKiemTra));
            if (k == null) return;
            dgHangCho.SelectedItem = k;
            dgHangCho.ScrollIntoView(k);
        }

        private void DongHangCho_MouseDoubleClick(object sender, MouseButtonEventArgs e) => GhiKiemTraHangCho();

        private void BtnGhiKiemTra_Click(object sender, RoutedEventArgs e) => GhiKiemTraHangCho();

        private void GhiKiemTraHangCho()
        {
            tabNhanSu.SelectedIndex = 2;
            if (dgHangCho.SelectedItem is not KipLaiDong k) return;

            if (!k.ThanhVien.Any(t => t.KiemTra.ChuaKiemTra))
            {
                ThongBaoDialog.ThongTin(
                    $"Cả 3 thành viên {k.TenKip.ToLower()} chuyến {k.Chuyen.SoHieuMacTau} đã có kết quả kiểm tra cho chuyến này.\n" +
                    "Người không đạt phải được thay (nút \"Thay Người\"), không đo lại để lên ban.",
                    "Kiểm tra lên ban");
                return;
            }
            KiemTraKip(k);
        }

        private void BtnThayNguoi_Click(object sender, RoutedEventArgs e)
        {
            if (dgHangCho.SelectedItem is not KipLaiDong k) return;
            var tv = k.ThanhVien.FirstOrDefault(CanThayNguoi);
            SuaKip(k, tv?.VaiTro);
        }

        private void BtnNhanBanHangCho_Click(object sender, RoutedEventArgs e)
        {
            if (dgHangCho.SelectedItem is KipLaiDong k) NhanBan(k);
        }

        private void BoLocKiemTra_Changed(object sender, RoutedEventArgs e)
        {
            if (ChuaSanSang) return;
            NapSoKiemTra();
        }

        private void BtnXoaLocKiemTra_Click(object sender, RoutedEventArgs e)
        {
            _dangNapDuLieu = true;
            txtTimKiemTra.Text = string.Empty;
            cboKetLuanKiemTra.SelectedIndex = 0;
            _dangNapDuLieu = false;
            NapSoKiemTra();
        }
    }
}
