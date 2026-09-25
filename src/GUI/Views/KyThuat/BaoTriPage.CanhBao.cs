using System.Windows;
using System.Windows.Controls;
using GUI.Views.KyThuat.Models;

namespace GUI.Views.KyThuat
{
    // =========================================================================
    // BAO TRI - TAB 4: CANH BAO AN TOAN
    //
    // Chi tong hop (khong co ban ghi rieng): lay du lieu da tron thay doi tam
    // cua 3 tab kia, dua qua TongHopCanhBao roi hien:
    //   - danh sach canh bao (loc theo muc do / nhom / tu khoa / doan tau),
    //     moi canh bao co nut xu ly mo dialog ngay tai tab nay;
    //   - bang san sang xuat ben theo tung doan tau.
    // Duoc tinh lai sau moi lan luu o tab khac va moi lan mo tab.
    // =========================================================================
    public partial class BaoTriPage
    {
        private string? _mucDoLoc;     // NGHIEM_TRONG | CANH_BAO | LUU_Y | null = tat ca
        private string? _macTauLoc;    // SoHieuMacTau khi bam the san sang xuat ben

        private List<CanhBaoAnToan> _tatCaCanhBao = new();

        private void LamMoiCanhBao()
        {
            DateTime bayGio = DateTime.Now;
            var khamXe = LayToanBoKhamXe();
            var capDau = LayToanBoCapDau();
            var baoDuong = LayToanBoBaoDuong();

            _tatCaCanhBao = TongHopCanhBao.TaoCanhBao(khamXe, capDau, baoDuong, bayGio);

            // --- Bang san sang xuat ben ---
            var sanSang = TongHopCanhBao.TaoSanSang(khamXe, capDau, baoDuong, bayGio);
            foreach (var s in sanSang) s.DangChon = s.DoanTau.SoHieuMacTau == _macTauLoc;
            icSanSang.ItemsSource = sanSang;

            var sapChay = sanSang.Where(s => !s.DaXuatPhat).ToList();
            int biChan = sapChay.Count(s => s.KetLuan == "CHAN");
            txtTomTatXuatBen.Text = $"{sapChay.Count} đoàn tàu sắp chạy · {biChan} bị chặn xuất bến";
            txtTomTatXuatBen.Foreground = Mau(biChan > 0 ? "#B91C1C" : "#15803D");
            txtCapNhatCanhBao.Text = $"Tổng hợp từ 3 tab · cập nhật lúc {bayGio:HH:mm:ss}";

            // --- Dem theo muc do ---
            int soNghiemTrong = _tatCaCanhBao.Count(c => c.MucDo == "NGHIEM_TRONG");
            int soCanhBao = _tatCaCanhBao.Count(c => c.MucDo == "CANH_BAO");
            txtSoNghiemTrong.Text = soNghiemTrong.ToString("N0");
            txtSoCanhBao.Text = soCanhBao.ToString("N0");
            txtSoLuuY.Text = _tatCaCanhBao.Count(c => c.MucDo == "LUU_Y").ToString("N0");

            // Badge tab: do khi con canh bao nghiem trong
            txtBadgeCanhBao.Text = (soNghiemTrong + soCanhBao).ToString();
            bdBadgeCanhBao.Background = Mau(soNghiemTrong > 0 ? "#DC2626" : soCanhBao > 0 ? "#F59E0B" : "#E2E8F0");
            txtBadgeCanhBao.Foreground = Mau(soNghiemTrong + soCanhBao > 0 ? "#FFFFFF" : "#334155");

            DanhDauTheMucDo();
            NapDanhSachCanhBao();
        }

        private void NapDanhSachCanhBao()
        {
            string tuKhoa = txtTimCanhBao.Text?.Trim() ?? string.Empty;
            string nhom = LayTagDangChon(cboNhomCanhBao) ?? "ALL";

            var ds = _tatCaCanhBao.Where(c =>
                (_mucDoLoc == null || c.MucDo == _mucDoLoc) &&
                (nhom == "ALL" || c.Nhom == nhom) &&
                (_macTauLoc == null || c.DoiTuong == _macTauLoc || c.DoanTauLienQuan.Contains(_macTauLoc)) &&
                (tuKhoa.Length == 0 ||
                 c.TieuDe.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) ||
                 c.MoTa.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) ||
                 c.DoiTuong.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) ||
                 c.NhanLienQuan.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))).ToList();

            icCanhBao.ItemsSource = ds;

            bdTrongCanhBao.Visibility = ds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtTrongCanhBao.Text = _tatCaCanhBao.Count == 0
                ? "An toàn: không có cảnh báo nào ở cả 3 nhóm khám xe, nhiên liệu, bảo dưỡng."
                : _macTauLoc != null
                    ? $"Không có cảnh báo nào liên quan tới {_macTauLoc} khớp bộ lọc."
                    : "Không có cảnh báo nào khớp bộ lọc.";

            bdLocTauCanhBao.Visibility = _macTauLoc != null ? Visibility.Visible : Visibility.Collapsed;
            txtLocTauCanhBao.Text = _macTauLoc != null ? $"Đoàn tàu: {_macTauLoc}" : "";
        }

        // Vien dam cho the muc do dang loc
        private void DanhDauTheMucDo()
        {
            void Dat(Border bd, string muc, string vienThuong)
            {
                bool chon = _mucDoLoc == muc;
                bd.BorderBrush = Mau(chon ? "#003B73" : vienThuong);
                bd.BorderThickness = new Thickness(chon ? 2 : 1);
            }
            Dat(bdMucNghiemTrong, "NGHIEM_TRONG", "#FECACA");
            Dat(bdMucCanhBao, "CANH_BAO", "#FDE68A");
            Dat(bdMucLuuY, "LUU_Y", "#BFDBFE");
        }

        // =====================================================================
        // LOC
        // =====================================================================

        private void TheMucDo_Click(object sender, RoutedEventArgs e)
        {
            string? muc = (sender as FrameworkElement)?.Tag?.ToString();
            _mucDoLoc = _mucDoLoc == muc ? null : muc;
            DanhDauTheMucDo();
            NapDanhSachCanhBao();
        }

        private void TheSanSang_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not SanSangXuatBenHienThi s) return;
            _macTauLoc = _macTauLoc == s.DoanTau.SoHieuMacTau ? null : s.DoanTau.SoHieuMacTau;
            LamMoiCanhBao();
        }

        private void BtnBoLocTauCanhBao_Click(object sender, RoutedEventArgs e)
        {
            _macTauLoc = null;
            LamMoiCanhBao();
        }

        private void TxtTimCanhBao_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            NapDanhSachCanhBao();
        }

        private void BoLocCanhBao_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            NapDanhSachCanhBao();
        }

        private void BtnXoaLocCanhBao_Click(object sender, RoutedEventArgs e)
        {
            _dangNapDuLieu = true;
            txtTimCanhBao.Text = string.Empty;
            cboNhomCanhBao.SelectedIndex = 0;
            _dangNapDuLieu = false;
            _mucDoLoc = null;
            _macTauLoc = null;
            LamMoiCanhBao();
        }

        // =====================================================================
        // XU LY CANH BAO: mo dialog / chuyen toi dung cho can xem
        // =====================================================================

        private void BtnXuLyCanhBao_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not CanhBaoAnToan cb) return;

            switch (cb.HanhDong)
            {
                case "KHAM_LAI":
                case "LAP_BIEN_BAN":
                    if (int.TryParse(cb.KhoaXuLy, out int maDoanTau))
                        LapBienBan(maDoanTau, chuyenTab: false);
                    break;

                case "GHI_NHAN_BD":
                    GhiNhanBaoDuong(cb.KhoaXuLy, chuyenTab: false);
                    break;

                case "XEM_NHIEN_LIEU":
                    if (int.TryParse(cb.KhoaXuLy, out int maPhieu))
                    {
                        var nl = LayToanBoCapDau().FirstOrDefault(x => x.MaNhatKyDau == maPhieu);
                        _dangNapDuLieu = true;
                        txtTimCapDau.Text = string.Empty;
                        cboMucTieuHao.SelectedIndex = 0;
                        _dangNapDuLieu = false;
                        _maDauMayLoc = nl?.MaDauMay;
                        tabBaoTri.SelectedIndex = 1;
                        LamMoiNhienLieu(giuDongChon: false, maChonSau: maPhieu);
                    }
                    break;

                case "XEM_TOA":
                    _khoaPhuongTienLoc = null;
                    ChonTheoTag(cboLoaiPhuongTien, "TOA_XE");
                    tabBaoTri.SelectedIndex = 2;
                    LamMoiBaoDuong(giuDongChon: false);
                    break;

                case "MO_PHUONG_TIEN":
                    // Duyet an toan lap tau thuoc phan he Phuong tien (tab Lap Doan Tau)
                    (Window.GetWindow(this) as MainWindow)?.NavigateTo("PhuongTien");
                    break;
            }
        }
    }
}
