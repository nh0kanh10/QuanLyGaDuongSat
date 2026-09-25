using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GUI.Views.Dialogs;
using GUI.Views.KyThuat.Dialogs;
using GUI.Views.KyThuat.Models;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.KyThuat
{
    // =========================================================================
    // BAO TRI - TAB 2: CAP PHAT NHIEN LIEU (baotri.NhatKyCapNhienLieu)
    //
    // Cung mau voi tab 1: the tom tat theo dau may (bam = loc, nhap dup = cap
    // phat), luoi phieu + panel chi tiet, lap / sua giu trong "thay doi tam".
    // Suat tieu hao va co vuot muc tinh theo DinhMucNhienLieu (dinh muc tham khao).
    // =========================================================================
    public partial class BaoTriPage
    {
        // Khoa am = phieu lap moi, khoa duong = phieu goc da bi sua
        private readonly Dictionary<int, CapNhienLieuHienThi> _capDauTam = new();

        // Loc theo dau may khi bam the (null = tat ca)
        private int? _maDauMayLoc;

        // Du lieu goc (thay cho doc CSDL) + ban sua tam + ban lap moi
        private List<CapNhienLieuHienThi> LayToanBoCapDau()
        {
            var ds = new List<CapNhienLieuHienThi>();
            var maDaCo = new HashSet<int>();

            foreach (var nl in DanhMucMauBaoTri.LayNhatKyCapDau())
            {
                maDaCo.Add(nl.MaNhatKyDau);
                ds.Add(_capDauTam.TryGetValue(nl.MaNhatKyDau, out var banTam) ? banTam : nl);
            }
            ds.AddRange(_capDauTam.Values.Where(nl => !maDaCo.Contains(nl.MaNhatKyDau)));
            return ds;
        }

        // Nap lai the + luoi; giu dong dang chon neu con trong danh sach
        private void LamMoiNhienLieu(bool giuDongChon, int? maChonSau = null)
        {
            int? maDangChon = maChonSau ?? (giuDongChon ? (dgCapDau.SelectedItem as CapNhienLieuHienThi)?.MaNhatKyDau : null);

            var toanBo = LayToanBoCapDau();
            NapTinhTrangNhienLieu(toanBo);
            NapDanhSachCapDau(toanBo);

            if (!(maDangChon.HasValue && ChonDongCapDau(maDangChon.Value)) && dgCapDau.Items.Count > 0)
                dgCapDau.SelectedIndex = 0;

            CapNhatNhanThayDoiTam();
        }

        // =====================================================================
        // THE SUAT TIEU HAO THEO DAU MAY + CHI SO "VUOT DINH MUC"
        // =====================================================================

        private void NapTinhTrangNhienLieu(List<CapNhienLieuHienThi> toanBo)
        {
            var ds = DanhMucMauBaoTri.DauMay.Select(dm =>
            {
                var cuaDauMay = toanBo.Where(nl => nl.MaDauMay == dm.MaDauMay)
                                      .OrderByDescending(nl => nl.ThoiDiemBomDau)
                                      .ToList();
                return new TinhTrangNhienLieuHienThi
                {
                    DauMay = dm,
                    LanMoiNhat = cuaDauMay.ElementAtOrDefault(0),
                    LanTruoc = cuaDauMay.ElementAtOrDefault(1),
                    SoLanCap = cuaDauMay.Count,
                    DangChon = _maDauMayLoc == dm.MaDauMay
                };
            }).ToList();

            icTinhTrangNhienLieu.ItemsSource = ds;
            txtKpiVuotDinhMuc.Text = ds.Count(t => t.TrangThai == "VUOT").ToString("N0");
        }

        private void TheDauMay_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not TinhTrangNhienLieuHienThi tt) return;

            // Bam lai the dang chon thi bo loc
            _maDauMayLoc = _maDauMayLoc == tt.DauMay.MaDauMay ? null : tt.DauMay.MaDauMay;
            LamMoiNhienLieu(giuDongChon: false);
        }

        private void TheDauMay_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not TinhTrangNhienLieuHienThi tt) return;
            e.Handled = true;

            // Hai lan Click cua cu nhap dup da bat roi tat loc: dat lai loc theo dau may nay
            _maDauMayLoc = tt.DauMay.MaDauMay;
            LamMoiNhienLieu(giuDongChon: false);
            CapPhatNhienLieu(tt.DauMay.MaDauMay);
        }

        private void BtnBoLocDauMay_Click(object sender, RoutedEventArgs e)
        {
            _maDauMayLoc = null;
            LamMoiNhienLieu(giuDongChon: true);
        }

        // =====================================================================
        // DANH SACH PHIEU CAP PHAT
        // =====================================================================

        private void NapDanhSachCapDau(List<CapNhienLieuHienThi> toanBo)
        {
            string tuKhoa = txtTimCapDau.Text?.Trim() ?? string.Empty;
            string muc = LayTagDangChon(cboMucTieuHao) ?? "ALL";

            var danhSach = toanBo.Where(nl => KhopBoLocCapDau(nl, tuKhoa, muc, _maDauMayLoc))
                                 .OrderByDescending(nl => nl.ThoiDiemBomDau)
                                 .ToList();

            dgCapDau.ItemsSource = danhSach;
            txtBadgeCapDau.Text = danhSach.Count.ToString();
            txtTrongCapDau.Visibility = danhSach.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var dmLoc = _maDauMayLoc.HasValue ? DanhMucMauBaoTri.TimDauMay(_maDauMayLoc.Value) : null;
            bdLocDauMay.Visibility = dmLoc != null ? Visibility.Visible : Visibility.Collapsed;
            txtLocDauMay.Text = dmLoc != null ? $"Đầu máy: {dmLoc.SoHieuDauMay}" : "";
        }

        private static bool KhopBoLocCapDau(CapNhienLieuHienThi nl, string tuKhoa, string muc, int? maDauMay)
        {
            if (tuKhoa.Length > 0 &&
                !nl.MaPhieu.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !nl.SoHieuDauMay.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !nl.NoiCapDau.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !nl.TenNguoiCap.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
                return false;

            if (muc != "ALL" && nl.MucDo != muc) return false;
            if (maDauMay.HasValue && nl.MaDauMay != maDauMay.Value) return false;
            return true;
        }

        private bool ChonDongCapDau(int maNhatKyDau)
        {
            if (dgCapDau.ItemsSource is not IEnumerable<CapNhienLieuHienThi> ds) return false;

            var muc = ds.FirstOrDefault(nl => nl.MaNhatKyDau == maNhatKyDau);
            if (muc == null) return false;

            dgCapDau.SelectedItem = muc;
            dgCapDau.ScrollIntoView(muc);
            return true;
        }

        private void TxtTimCapDau_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            LamMoiNhienLieu(giuDongChon: true);
        }

        private void BoLocCapDau_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            LamMoiNhienLieu(giuDongChon: true);
        }

        private void BtnXoaLocCapDau_Click(object sender, RoutedEventArgs e)
        {
            _dangNapDuLieu = true;
            txtTimCapDau.Text = string.Empty;
            cboMucTieuHao.SelectedIndex = 0;
            _maDauMayLoc = null;
            _dangNapDuLieu = false;

            LamMoiNhienLieu(giuDongChon: true);
        }

        // =====================================================================
        // CHI TIET PHIEU
        // =====================================================================

        private void DgCapDau_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var nl = dgCapDau.SelectedItem as CapNhienLieuHienThi;
            btnSuaCapDau.IsEnabled = nl != null;
            btnSuaCapDauChiTiet.IsEnabled = nl != null;

            if (nl == null)
            {
                txtNlTieuDe.Text = "PHIẾU CẤP PHÁT NHIÊN LIỆU";
                txtNlMoTa.Text = "Chọn một phiếu để xem chi tiết";
                txtNlMa.Text = "—";
                pnlNlChiTiet.Visibility = Visibility.Collapsed;
                txtNlTrong.Visibility = Visibility.Visible;
                return;
            }
            HienThiChiTietCapDau(nl);
        }

        private void HienThiChiTietCapDau(CapNhienLieuHienThi nl)
        {
            pnlNlChiTiet.Visibility = Visibility.Visible;
            txtNlTrong.Visibility = Visibility.Collapsed;

            txtNlTieuDe.Text = $"{nl.SoHieuDauMay} · {nl.NoiCapDau}";
            txtNlMoTa.Text = $"Bơm lúc {nl.ThoiDiemBomDau:HH:mm dd/MM/yyyy} · {nl.TenNguoiCap}";
            txtNlMa.Text = nl.LaThayDoiTam ? $"{nl.MaPhieu} · tạm" : nl.MaPhieu;

            // --- Ket luan ---
            string muc = nl.MucDo;
            bdNlKetLuan.Background = Mau(nl.MauNen);
            bdNlKetLuan.BorderBrush = Mau(nl.MauVien);
            icoNlKetLuan.Foreground = Mau(nl.MauChu);
            txtNlKetLuan.Foreground = Mau(nl.MauChu);
            switch (muc)
            {
                case "VUOT":
                    icoNlKetLuan.Symbol = SymbolRegular.Warning24;
                    txtNlKetLuan.Text = $"VƯỢT ĐỊNH MỨC {nl.TyLeDinhMuc - 100m:N1}%";
                    txtNlKetLuanPhu.Text = "Cờ cảnh báo vượt mức: BẬT";
                    break;
                case "SAT":
                    icoNlKetLuan.Symbol = SymbolRegular.Info24;
                    txtNlKetLuan.Text = $"SÁT ĐỊNH MỨC ({nl.TyLeDinhMuc:N0}%)";
                    txtNlKetLuanPhu.Text = "Chưa vượt, cờ cảnh báo tắt · cần theo dõi";
                    break;
                default:
                    icoNlKetLuan.Symbol = SymbolRegular.CheckmarkCircle24;
                    txtNlKetLuan.Text = $"TRONG ĐỊNH MỨC ({nl.TyLeDinhMuc:N0}%)";
                    txtNlKetLuanPhu.Text = "Cờ cảnh báo vượt mức: tắt";
                    break;
            }

            // --- Suat tieu hao ---
            txtNlSfc.Text = nl.SuatTieuHaoSFC.ToString("N2");
            txtNlSfc.Foreground = Mau(nl.MauChu);
            txtNlCongThuc.Text = $"= {nl.SoLitTraNap:N0} × 10,000 ÷ ({nl.TrongLuongKeoTan:N0} × {nl.CuLyChayKm:N1})";
            txtNlNhanDinhMuc.Text = $"Vạch đen: định mức {nl.DinhMucSfc:N0} của dòng {nl.MaDongCode} (tham khảo) · thanh vẽ tới 150%";
            bdNlVach.Background = Mau(nl.MauChu);
            colNlDaDung.Width = nl.PhanDaDung;
            colNlConLai.Width = nl.PhanConLai;

            // --- Lich su cua dau may (moi nhat truoc) ---
            var lichSu = LayToanBoCapDau().Where(x => x.MaDauMay == nl.MaDauMay)
                                          .OrderByDescending(x => x.ThoiDiemBomDau)
                                          .ToList();
            icNlLichSu.ItemsSource = lichSu;
            txtNlTieuDeLichSu.Text = $"LỊCH SỬ TIÊU HAO CỦA {nl.SoHieuDauMay} ({lichSu.Count})";

            // --- Goi y khi vuot dinh muc: kem xu huong so voi lan cap lien truoc ---
            if (muc == "VUOT")
            {
                int viTri = lichSu.FindIndex(x => x.MaNhatKyDau == nl.MaNhatKyDau);
                var lanTruoc = viTri >= 0 ? lichSu.ElementAtOrDefault(viTri + 1) : null;
                string xuHuong = lanTruoc != null && lanTruoc.SuatTieuHaoSFC > 0
                    ? $"Tăng {Math.Round((nl.SuatTieuHaoSFC - lanTruoc.SuatTieuHaoSFC) / lanTruoc.SuatTieuHaoSFC * 100m, 1):N1}% so với lần cấp trước ({lanTruoc.SuatTieuHaoSFC:N2}). "
                    : "";
                bdNlGoiY.Visibility = Visibility.Visible;
                txtNlGoiY.Text = xuHuong +
                    "Đề xuất: đưa đầu máy kiểm tra hệ thống nhiên liệu và rà soát chế độ vận hành của kíp lái.";
            }
            else
            {
                bdNlGoiY.Visibility = Visibility.Collapsed;
            }

            // --- So lieu cap phat ---
            var pnl = pnlNlSoLieu;
            pnl.Children.Clear();
            ThemDongThongSo(pnl, "Số lít trả nạp", $"{nl.SoLitTraNap:N2} lít");
            ThemDongThongSo(pnl, "Mức nạp so với bồn", nl.DungTichBonDauLit > 0
                ? $"{nl.TyLeBon:N1}% của {nl.DungTichBonDauLit:N0} lít"
                : "—");
            ThemDongThongSo(pnl, "Trọng lượng kéo", $"{nl.TrongLuongKeoTan:N2} tấn");
            ThemDongThongSo(pnl, "Cự ly đã chạy", $"{nl.CuLyChayKm:N1} km");
            ThemDongThongSo(pnl, "Nơi cấp dầu", nl.NoiCapDau);
            ThemDongThongSo(pnl, "Người cấp phát", string.IsNullOrEmpty(nl.TenNguoiCap) ? "(không ghi)" : nl.TenNguoiCap);
            ThemDongThongSo(pnl, "Cờ vượt mức (CanhBaoVuotMuc)", nl.CanhBaoVuotMuc ? "Bật" : "Tắt", laDongCuoi: true);
        }

        private void DongLichSuCapDau_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not CapNhienLieuHienThi nl) return;
            if (ChonDongCapDau(nl.MaNhatKyDau)) return;

            // Bi bo loc an mat: xoa loc roi chon lai
            BtnXoaLocCapDau_Click(this, new RoutedEventArgs());
            ChonDongCapDau(nl.MaNhatKyDau);
        }

        // =====================================================================
        // LAP / SUA PHIEU CAP PHAT
        // =====================================================================

        private void BtnCapPhat_Click(object sender, RoutedEventArgs e) => CapPhatNhienLieu(_maDauMayLoc);

        private void BtnSuaCapDau_Click(object sender, RoutedEventArgs e) => SuaCapDau();

        private void DongCapDau_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SuaCapDau();

        private void CapPhatNhienLieu(int? maDauMay = null)
        {
            tabBaoTri.SelectedIndex = 1;

            var dlg = new CapNhienLieuDialog(null, maDauMay) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var nl = dlg.KetQua;
            nl.MaNhatKyDau = _maTamKeTiep--;
            _capDauTam[nl.MaNhatKyDau] = nl;

            LamMoiSauKhiLuuCapDau(nl);
            BaoKetQuaLuuCapDau(nl, laLapMoi: true);
        }

        private void SuaCapDau()
        {
            if (dgCapDau.SelectedItem is not CapNhienLieuHienThi dangChon) return;

            var dlg = new CapNhienLieuDialog(dangChon.SaoChep()) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var nl = dlg.KetQua;
            _capDauTam[nl.MaNhatKyDau] = nl;

            LamMoiSauKhiLuuCapDau(nl);
            BaoKetQuaLuuCapDau(nl, laLapMoi: false);
        }

        private void LamMoiSauKhiLuuCapDau(CapNhienLieuHienThi nl)
        {
            LamMoiNhienLieu(giuDongChon: false, maChonSau: nl.MaNhatKyDau);

            // Bo loc hien tai an mat phieu vua luu thi xoa loc de nguoi dung thay ngay
            if ((dgCapDau.SelectedItem as CapNhienLieuHienThi)?.MaNhatKyDau != nl.MaNhatKyDau)
            {
                _dangNapDuLieu = true;
                txtTimCapDau.Text = string.Empty;
                cboMucTieuHao.SelectedIndex = 0;
                _dangNapDuLieu = false;
                if (_maDauMayLoc.HasValue && _maDauMayLoc != nl.MaDauMay) _maDauMayLoc = null;

                LamMoiNhienLieu(giuDongChon: false, maChonSau: nl.MaNhatKyDau);
            }

            LamMoiCanhBao();
        }

        private static void BaoKetQuaLuuCapDau(CapNhienLieuHienThi nl, bool laLapMoi)
        {
            string hanhDong = laLapMoi ? "Đã lập" : "Đã cập nhật";
            string soLieu = $"{nl.SoLitTraNap:N0} lít · suất tiêu hao {nl.SuatTieuHaoSFC:N2} lít/10,000 tấn·km " +
                            $"({nl.TyLeDinhMuc:N0}% định mức {nl.DinhMucSfc:N0}).";

            if (nl.CanhBaoVuotMuc)
            {
                ThongBaoDialog.CanhBao(
                    $"{hanhDong} phiếu cấp phát cho đầu máy {nl.SoHieuDauMay}: {soLieu}\n\n" +
                    $"Tiêu hao VƯỢT ĐỊNH MỨC {nl.TyLeDinhMuc - 100m:N1}% — đã bật cờ cảnh báo vượt mức.\n" +
                    "Phiếu đang hiển thị trên màn hình, chưa ghi vào CSDL.",
                    "Tiêu hao vượt định mức");
            }
            else
            {
                ThongBaoDialog.ThanhCong(
                    $"{hanhDong} phiếu cấp phát cho đầu máy {nl.SoHieuDauMay}: {soLieu}\n\n" +
                    "Phiếu đang hiển thị trên màn hình, chưa ghi vào CSDL.",
                    "Lưu phiếu cấp phát");
            }
        }
    }
}
