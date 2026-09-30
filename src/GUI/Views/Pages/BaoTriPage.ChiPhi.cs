using System.Windows;
using System.Windows.Controls;
using GUI.ViewModels.KyThuat;

namespace GUI.Views.Pages
{
    // =========================================================================
    // BAO TRI - TAB 5: CHI PHI & HIEU QUA (theo tuyen duong / mac tau)
    //
    // Bang tong hop CHI DOC (khong co dialog them/sua/xoa): cong don ChiPhi da
    // ghi o 3 tab truoc (kham xe, cap nhien lieu, bao duong) TRONG KY dang chon,
    // chia theo tuyen dang phuc vu, cong voi chi phi bao duong DU PHONG (dua tren
    // chu ky o tab 3), roi so voi DOANH THU MAU cung ky de goi y lai/lo va khuyen nghi.
    // Duoc tinh lai moi lan mo tab (giong tab Canh bao).
    // =========================================================================
    public partial class BaoTriPage
    {
        private List<TuyenChiPhiHienThi> _tatCaTuyenChiPhi = new();
        private string? _maTuyenChonChiPhi;

        private int SoNgayKyChiPhi =>
            (cboKyChiPhi.SelectedItem as ComboBoxItem)?.Tag is string s && int.TryParse(s, out int n)
                ? n : ChiPhiHieuQua.SoNgayKyMacDinh;

        private void LamMoiChiPhi()
        {
            int soNgay = SoNgayKyChiPhi;
            var bayGio = DateTime.Now;
            var tuNgay = bayGio.AddDays(-soNgay);
            _tatCaTuyenChiPhi = ChiPhiHieuQua.TaoTongHop(LayToanBoKhamXe(), LayToanBoCapDau(), LayToanBoBaoDuong(),
                                                         soNgay, tuNgay);

            int soCanXemXet = _tatCaTuyenChiPhi.Count(t => t.CanXemXet);
            int soLo = _tatCaTuyenChiPhi.Count(t => t.CoDuLieuChiPhi && !t.CoLai);
            txtBadgeChiPhi.Text = soCanXemXet.ToString();
            bdBadgeChiPhi.Background = Mau(soCanXemXet > 0 ? "#DC2626" : "#E2E8F0");
            txtBadgeChiPhi.Foreground = Mau(soCanXemXet > 0 ? "#FFFFFF" : "#334155");
            txtTomTatChiPhi.Text = soCanXemXet == 0
                ? $"{_tatCaTuyenChiPhi.Count} tuyến · không tuyến nào cần xem xét thu hẹp"
                : $"{_tatCaTuyenChiPhi.Count} tuyến · {soCanXemXet} tuyến cần xem xét thu hẹp ({soLo} lỗ, " +
                  $"{soCanXemXet - soLo} biên lợi nhuận < {ChiPhiHieuQua.NguongCanNhac:N0}%)";
            txtTomTatChiPhi.Foreground = Mau(soCanXemXet > 0 ? "#B91C1C" : "#15803D");

            int soKhoan = _tatCaTuyenChiPhi.SelectMany(t => t.ChiTietDaChi).Select(c => c.MaPhieu).Distinct().Count();
            txtCapNhatChiPhi.Text = $"Kỳ {tuNgay:dd/MM} – {bayGio:dd/MM/yyyy} · {soKhoan} phiếu có chi phí trong kỳ · " +
                                    $"cập nhật lúc {bayGio:HH:mm:ss}";

            NapDanhSachChiPhi();
        }

        private void CboKyChiPhi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            LamMoiChiPhi();
        }

        private void NapDanhSachChiPhi()
        {
            string tuKhoa = txtTimTuyen.Text?.Trim() ?? string.Empty;

            var ds = _tatCaTuyenChiPhi.Where(t =>
                tuKhoa.Length == 0 ||
                t.Tuyen.TenTuyen.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) ||
                t.Tuyen.HanhTrinh.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) ||
                t.Tuyen.NhanMacTau.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var t in ds) t.DangChon = t.Tuyen.MaTuyen == _maTuyenChonChiPhi;
            icTuyenChiPhi.ItemsSource = ds;
            txtTrongTuyenChiPhi.Visibility = ds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Neu tuyen dang chon bi loc mat, hoac chua chon gi thi tu chon dong dau tien
            var dangChon = ds.FirstOrDefault(t => t.Tuyen.MaTuyen == _maTuyenChonChiPhi) ?? ds.FirstOrDefault();
            HienThiChiTietTuyen(dangChon);
        }

        private void TxtTimTuyen_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            NapDanhSachChiPhi();
        }

        private void BtnXoaLocTuyen_Click(object sender, RoutedEventArgs e)
        {
            _dangNapDuLieu = true;
            txtTimTuyen.Text = string.Empty;
            _dangNapDuLieu = false;
            NapDanhSachChiPhi();
        }

        private void TheTuyenChiPhi_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not TuyenChiPhiHienThi t) return;
            _maTuyenChonChiPhi = t.Tuyen.MaTuyen;
            NapDanhSachChiPhi();
        }

        // =====================================================================
        // CHI TIET TUYEN (cot phai)
        // =====================================================================

        private void HienThiChiTietTuyen(TuyenChiPhiHienThi? t)
        {
            if (t == null)
            {
                pnlChiPhiChiTiet.Visibility = Visibility.Collapsed;
                txtChiPhiTrong.Visibility = Visibility.Visible;
                return;
            }

            _maTuyenChonChiPhi = t.Tuyen.MaTuyen;
            pnlChiPhiChiTiet.Visibility = Visibility.Visible;
            txtChiPhiTrong.Visibility = Visibility.Collapsed;

            txtCpTieuDe.Text = t.Tuyen.TenTuyen;
            txtCpMoTa.Text = $"{t.Tuyen.HanhTrinh} · {(t.Tuyen.MacTauPhucVu.Count > 0 ? t.Tuyen.NhanMacTau : "chưa có mác tàu")}";

            // --- Ket luan loi / lo ---
            bdCpKetLuan.Background = Mau(t.CoLai ? "#F0FDF4" : "#FEF2F2");
            bdCpKetLuan.BorderBrush = Mau(t.CoLai ? "#86EFAC" : "#FCA5A5");
            txtCpLoiNhuan.Text = $"{(t.CoLai ? "+" : "")}{t.LoiNhuan:N0} đ";
            txtCpLoiNhuan.Foreground = Mau(t.MauLoiNhuan);
            txtCpLoiNhuanPhu.Text = t.CoDuLieuChiPhi
                ? $"Biên lợi nhuận {t.BienLoiNhuan:N1}% trong {t.SoNgayKy} ngày · doanh thu {t.DoanhThu:N0} đ − chi phí bảo trì {t.TongChiPhiBaoTri:N0} đ"
                : $"Chưa có khoản chi phí nào trong {t.SoNgayKy} ngày cho tuyến này — chưa đủ cơ sở so sánh.";

            // --- Khuyen nghi ---
            bdCpKhuyenNghi.Background = Mau(t.NenKhuyenNghi);
            bdCpKhuyenNghi.BorderBrush = Mau(t.VienKhuyenNghi);
            icoCpKhuyenNghi.Symbol = t.BieuTuongKhuyenNghi;
            icoCpKhuyenNghi.Foreground = Mau(t.MauKhuyenNghi);
            txtCpKhuyenNghi.Text = t.NhanKhuyenNghi;
            txtCpKhuyenNghi.Foreground = Mau(t.MauKhuyenNghi);

            // --- So lieu tong hop ---
            pnlCpSoLieu.Children.Clear();
            ThemDongThongSo(pnlCpSoLieu, $"Doanh thu {t.SoNgayKy} ngày (mẫu)", $"{t.DoanhThu:N0} đ");
            ThemDongThongSo(pnlCpSoLieu, $"Chi phí đã chi trong {t.SoNgayKy} ngày", $"{t.ChiPhiDaChi:N0} đ");
            ThemDongThongSo(pnlCpSoLieu, "Chi phí dự phóng kỳ tới", $"{t.ChiPhiDuPhong:N0} đ");
            ThemDongThongSo(pnlCpSoLieu, "Tổng chi phí bảo trì", $"{t.TongChiPhiBaoTri:N0} đ", laDongCuoi: true);

            // --- Breakdown da chi ---
            icCpDaChi.ItemsSource = t.ChiTietDaChi.OrderByDescending(c => c.ThoiDiem).ToList();
            txtCpTieuDeDaChi.Text = $"CHI PHÍ ĐÃ CHI TRONG {t.SoNgayKy} NGÀY ({t.ChiTietDaChi.Count} khoản)";
            txtCpTrongDaChi.Visibility = t.ChiTietDaChi.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // --- Breakdown du phong ---
            icCpDuPhong.ItemsSource = t.ChiTietDuPhong.ToList();
            txtCpTieuDeDuPhong.Text = $"CHI PHÍ DỰ PHÓNG ({t.ChiTietDuPhong.Count} phương tiện)";
            txtCpTrongDuPhong.Visibility = t.ChiTietDuPhong.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            txtCpGhiChu.Text = t.Tuyen.GhiChu +
                "\nChi phí nhiên liệu, bảo dưỡng và dự phóng của một đầu máy được chia cho các tuyến theo số " +
                "đoàn tàu mà đầu máy đó kéo; chi phí khám xe tính thẳng cho tuyến của đoàn tàu.";
        }
    }
}
