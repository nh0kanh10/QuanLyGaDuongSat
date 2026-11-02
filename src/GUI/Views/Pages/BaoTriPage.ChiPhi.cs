using System.Windows;
using System.Windows.Controls;
using BUS.Services;
using DTO.BaoTri;
using GUI.ViewModels.KyThuat;

namespace GUI.Views.Pages
{
    // =========================================================================
    // BAO TRI - TAB 5: CHI PHI & HIEU QUA (theo mac tau)
    //
    // Bang tong hop CHI DOC (khong co dialog them/sua/xoa). Tu 26/10 la so lieu THAT tu anh chup CSDL:
    // QuyTacChiPhi.TongHop cong chi phi da ghi o 3 tab truoc (kham xe, cap nhien lieu, bao duong) TRONG KY,
    // chia theo mac tau, cong chi phi bao duong DU PHONG, roi so voi DOANH THU VE that (vantai.Ve) cua cac
    // chuyen chay trong ky de goi y lai / lo va khuyen nghi. Tinh lai moi lan mo tab (giong tab Canh bao).
    // =========================================================================
    public partial class BaoTriPage
    {
        private KetQuaChiPhiDTO? _ketQuaChiPhi;
        private List<MacTauChiPhiHienThi> _tatCaMacTauChiPhi = new();
        private string? _macTauChonChiPhi;

        private int SoNgayKyChiPhi =>
            (cboKyChiPhi.SelectedItem as ComboBoxItem)?.Tag is string s && int.TryParse(s, out int n)
                ? n : QuyTacChiPhi.SoNgayKyMacDinh;

        private void LamMoiChiPhi()
        {
            int soNgay = SoNgayKyChiPhi;
            var kq = QuyTacChiPhi.TongHop(_duLieu.Goc, soNgay);
            _ketQuaChiPhi = kq;
            _tatCaMacTauChiPhi = kq.MacTau.Select(h => new MacTauChiPhiHienThi(h, soNgay)).ToList();

            int soCanXemXet = _tatCaMacTauChiPhi.Count(t => t.CanXemXet);
            int soLo = _tatCaMacTauChiPhi.Count(t => t.CanXemXet && !t.CoLai);
            txtBadgeChiPhi.Text = soCanXemXet.ToString();
            bdBadgeChiPhi.Background = Mau(soCanXemXet > 0 ? "#DC2626" : "#E2E8F0");
            txtBadgeChiPhi.Foreground = Mau(soCanXemXet > 0 ? "#FFFFFF" : "#334155");
            txtTomTatChiPhi.Text = soCanXemXet == 0
                ? $"{_tatCaMacTauChiPhi.Count} mác tàu · không mác nào cần xem xét thu hẹp"
                : $"{_tatCaMacTauChiPhi.Count} mác tàu · {soCanXemXet} mác cần xem xét thu hẹp ({soLo} lỗ, " +
                  $"{soCanXemXet - soLo} biên lợi nhuận < {QuyTacChiPhi.NguongCanNhac:N0}%)";
            txtTomTatChiPhi.Foreground = Mau(soCanXemXet > 0 ? "#B91C1C" : "#15803D");

            int soPhieu = kq.MacTau.SelectMany(t => t.DaChi).Concat(kq.ChuaPhanBo).Select(c => c.MaPhieu).Distinct().Count();
            txtCapNhatChiPhi.Text = $"Kỳ {kq.TuNgay:dd/MM} – {kq.DenLuc:dd/MM/yyyy} · {soPhieu} phiếu có chi phí · " +
                                    $"số liệu lúc {kq.DenLuc:HH:mm:ss}";

            // Doi soat: thuc chi = phan da gan cho cac mac + phan chua gan duoc
            txtPhanBoChiPhi.Text = $"Thực chi {kq.ThucChi:N0} đ · gắn cho mác tàu {kq.DaPhanBo:N0} đ · " +
                                   $"chưa gắn mác {kq.TongChuaPhanBo:N0} đ";
            txtPhanBoChiPhi.ToolTip = kq.ChuaPhanBo.Count == 0
                ? "Mọi khoản chi phí trong kỳ đều đã gắn cho một mác tàu."
                : "Chi phí chưa gắn mác tàu nào:\n" + string.Join("\n",
                      kq.ChuaPhanBo.Take(10).Select(c => $"• {c.MaPhieu} {c.MoTa}: {c.SoTien:N0} đ"))
                  + (kq.ChuaPhanBo.Count > 10 ? $"\n… và {kq.ChuaPhanBo.Count - 10} khoản khác" : "");

            NapDanhSachChiPhi();
        }

        private void CboKyChiPhi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            LamMoiChiPhi();
        }

        private void NapDanhSachChiPhi()
        {
            string tuKhoa = txtTimMacTau.Text?.Trim() ?? string.Empty;

            var ds = _tatCaMacTauChiPhi.Where(t =>
                tuKhoa.Length == 0 ||
                t.SoHieuMacTau.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) ||
                t.Goc.MacTau.HanhTrinh.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) ||
                t.Goc.MacTau.NhanLoaiTau.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var t in ds) t.DangChon = t.SoHieuMacTau == _macTauChonChiPhi;
            icMacTauChiPhi.ItemsSource = ds;
            txtTrongMacTauChiPhi.Visibility = ds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Neu mac dang chon bi loc mat, hoac chua chon gi thi tu chon dong dau tien
            var dangChon = ds.FirstOrDefault(t => t.SoHieuMacTau == _macTauChonChiPhi) ?? ds.FirstOrDefault();
            HienThiChiTietMacTau(dangChon);
        }

        private void TxtTimMacTau_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            NapDanhSachChiPhi();
        }

        private void BtnXoaLocMacTau_Click(object sender, RoutedEventArgs e)
        {
            _dangNapDuLieu = true;
            txtTimMacTau.Text = string.Empty;
            _dangNapDuLieu = false;
            NapDanhSachChiPhi();
        }

        private void TheMacTauChiPhi_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not MacTauChiPhiHienThi t) return;
            _macTauChonChiPhi = t.SoHieuMacTau;
            NapDanhSachChiPhi();
        }

        // =====================================================================
        // CHI TIET MAC TAU (cot phai)
        // =====================================================================

        private void HienThiChiTietMacTau(MacTauChiPhiHienThi? t)
        {
            if (t == null)
            {
                pnlChiPhiChiTiet.Visibility = Visibility.Collapsed;
                txtChiPhiTrong.Visibility = Visibility.Visible;
                return;
            }

            _macTauChonChiPhi = t.SoHieuMacTau;
            pnlChiPhiChiTiet.Visibility = Visibility.Visible;
            txtChiPhiTrong.Visibility = Visibility.Collapsed;

            var h = t.Goc;
            txtCpTieuDe.Text = t.TieuDe;
            txtCpMoTa.Text = $"{h.MacTau.HanhTrinh} · {t.MoTaChuyen}";

            // --- Ket luan loi / lo ---
            string nen = t.ChuaDuDuLieu ? "#F8FAFC" : t.CoLai ? "#F0FDF4" : "#FEF2F2";
            string vien = t.ChuaDuDuLieu ? "#E2E8F0" : t.CoLai ? "#86EFAC" : "#FCA5A5";
            bdCpKetLuan.Background = Mau(nen);
            bdCpKetLuan.BorderBrush = Mau(vien);
            txtCpLoiNhuan.Text = $"{(t.CoLai ? "+" : "")}{t.LoiNhuan:N0} đ";
            txtCpLoiNhuan.Foreground = Mau(t.MauLoiNhuan);
            txtCpLoiNhuanPhu.Text = t.ChuaDuDuLieu
                ? h.LyDoChuaDu
                : h.BienLoiNhuan is { } bien
                    ? $"Biên lợi nhuận {bien:N1}% trong {t.SoNgayKy} ngày · doanh thu {t.DoanhThu:N0} đ − chi phí bảo trì {t.TongChiPhiBaoTri:N0} đ"
                    : $"Có {h.SoGhe:N0} ghế bán vé nhưng chưa thu được đồng doanh thu nào trong {t.SoNgayKy} ngày.";

            // --- Khuyen nghi ---
            bdCpKhuyenNghi.Background = Mau(t.NenKhuyenNghi);
            bdCpKhuyenNghi.BorderBrush = Mau(t.VienKhuyenNghi);
            icoCpKhuyenNghi.Symbol = t.BieuTuongKhuyenNghi;
            icoCpKhuyenNghi.Foreground = Mau(t.MauKhuyenNghi);
            txtCpKhuyenNghi.Text = t.NhanKhuyenNghi;
            txtCpKhuyenNghi.Foreground = Mau(t.MauKhuyenNghi);

            // --- So lieu tong hop ---
            pnlCpSoLieu.Children.Clear();
            ThemDongThongSo(pnlCpSoLieu, $"Doanh thu vé thuần ({h.SoChuyen} chuyến)", $"{t.DoanhThu:N0} đ");
            if (h.TienHoan > 0)
                ThemDongThongSo(pnlCpSoLieu, "· thực thu − đã hoàn cho khách", $"{h.ThucThu:N0} − {h.TienHoan:N0} đ");
            ThemDongThongSo(pnlCpSoLieu, "Vé đã bán / số ghế",
                h.SoGhe > 0 ? $"{h.SoVe:N0} / {h.SoGhe:N0} ({h.TyLeLapDay:N1}%)" : "—");
            ThemDongThongSo(pnlCpSoLieu, $"Chi phí đã chi trong {t.SoNgayKy} ngày", $"{t.ChiPhiDaChi:N0} đ");
            ThemDongThongSo(pnlCpSoLieu, "Chi phí dự phóng kỳ tới", $"{t.ChiPhiDuPhong:N0} đ");
            ThemDongThongSo(pnlCpSoLieu, "Tổng chi phí bảo trì", $"{t.TongChiPhiBaoTri:N0} đ", laDongCuoi: true);

            // --- Doanh thu theo chuyen ---
            icCpChuyen.ItemsSource = t.ChuyenTrongKy;
            txtCpTieuDeChuyen.Text = $"DOANH THU THEO CHUYẾN ({t.ChuyenTrongKy.Count} chuyến chạy trong kỳ)";
            txtCpTrongChuyen.Visibility = t.ChuyenTrongKy.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // --- Breakdown da chi ---
            icCpDaChi.ItemsSource = t.ChiTietDaChi;
            txtCpTieuDeDaChi.Text = $"CHI PHÍ ĐÃ CHI TRONG {t.SoNgayKy} NGÀY ({t.ChiTietDaChi.Count} khoản)";
            txtCpTrongDaChi.Visibility = t.ChiTietDaChi.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // --- Breakdown du phong ---
            icCpDuPhong.ItemsSource = t.ChiTietDuPhong;
            txtCpTieuDeDuPhong.Text = $"CHI PHÍ DỰ PHÓNG ({t.ChiTietDuPhong.Count} phương tiện)";
            txtCpTrongDuPhong.Visibility = t.ChiTietDuPhong.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            string ghiChu =
                "Doanh thu = tiền vé thực thu − tiền đã hoàn cho khách, của các chuyến có ngày chạy trong kỳ " +
                "(vé cũ khi đổi ghế đã hủy không tính). Chi phí nhiên liệu, bảo dưỡng và dự phóng của một đầu máy " +
                "được chia cho các mác theo số đoàn tàu đầu máy đó kéo; chi phí khám xe tính thẳng cho mác của " +
                "đoàn tàu; bảo dưỡng toa xe chưa gắn mác. Lợi nhuận chưa trừ lương, phí hạ tầng, khấu hao.";
            if (_ketQuaChiPhi is { SoVanDon: > 0 } kq)
                ghiChu += $"\nCước hàng hoá trong kỳ: {kq.CuocHangHoa:N0} đ ({kq.SoVanDon} vận đơn) — vận đơn chưa gắn " +
                          "chuyến tàu nên chưa chia được theo mác.";
            txtCpGhiChu.Text = ghiChu;
        }
    }
}
