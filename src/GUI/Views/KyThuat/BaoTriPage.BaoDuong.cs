using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GUI.Views.Dialogs;
using GUI.Views.KyThuat.Dialogs;
using GUI.Views.KyThuat.Models;

namespace GUI.Views.KyThuat
{
    // =========================================================================
    // BAO TRI - TAB 3: BAO DUONG DINH KY (baotri.NhatKyBaoDuong)
    //
    // Cung mau voi tab 1 - 2. Tinh trang den han tinh theo chu ky tu lan bao
    // duong gan nhat cua tung phuong tien (ChuKyBaoDuong), nen phieu vua ghi
    // nhan tren giao dien lam moi ngay moc ke tiep cua the / KPI.
    // =========================================================================
    public partial class BaoTriPage
    {
        // Khoa am = phieu ghi nhan moi, khoa duong = phieu goc da bi sua
        private readonly Dictionary<int, BaoDuongHienThi> _baoDuongTam = new();

        // Loc theo mot phuong tien (khoa "DAU_MAY:2" / "TOA_XE:4"), null = tat ca
        private string? _khoaPhuongTienLoc;

        // Du lieu goc (thay cho doc CSDL) + ban sua tam + ban ghi nhan moi
        private List<BaoDuongHienThi> LayToanBoBaoDuong()
        {
            var ds = new List<BaoDuongHienThi>();
            var maDaCo = new HashSet<int>();

            foreach (var bd in DanhMucMauBaoTri.LayNhatKyBaoDuong())
            {
                maDaCo.Add(bd.MaBaoDuong);
                ds.Add(_baoDuongTam.TryGetValue(bd.MaBaoDuong, out var banTam) ? banTam : bd);
            }
            ds.AddRange(_baoDuongTam.Values.Where(bd => !maDaCo.Contains(bd.MaBaoDuong)));
            return ds;
        }

        private void LamMoiBaoDuong(bool giuDongChon, int? maChonSau = null)
        {
            int? maDangChon = maChonSau ?? (giuDongChon ? (dgBaoDuong.SelectedItem as BaoDuongHienThi)?.MaBaoDuong : null);

            var toanBo = LayToanBoBaoDuong();
            NapTinhTrangBaoDuong(toanBo);
            NapDanhSachBaoDuong(toanBo);

            if (!(maDangChon.HasValue && ChonDongBaoDuong(maDangChon.Value)) && dgBaoDuong.Items.Count > 0)
                dgBaoDuong.SelectedIndex = 0;

            CapNhatNhanThayDoiTam();
        }

        // =====================================================================
        // THE TINH TRANG + CHI SO "DEN HAN BAO DUONG"
        // =====================================================================

        private TinhTrangBaoDuongHienThi TaoTinhTrang(PhuongTienBaoDuong pt, List<BaoDuongHienThi> toanBo)
        {
            var cuaPt = toanBo.Where(x => x.KhoaPhuongTien == pt.Khoa).ToList();
            return new TinhTrangBaoDuongHienThi
            {
                PhuongTien = pt,
                TienDo = ChuKyBaoDuong.TinhTienDo(pt, cuaPt),
                LanGanNhat = cuaPt.OrderByDescending(x => x.ThoiDiemHoanThanh).FirstOrDefault(),
                DangChon = _khoaPhuongTienLoc == pt.Khoa
            };
        }

        private void NapTinhTrangBaoDuong(List<BaoDuongHienThi> toanBo)
        {
            var tatCa = DanhMucMauBaoTri.PhuongTien.Select(pt => TaoTinhTrang(pt, toanBo)).ToList();

            // 4 the dau may
            icTinhTrangBaoDuong.ItemsSource = tatCa.Where(t => t.PhuongTien.LaDauMay).ToList();

            // The tong hop toa xe
            var toa = tatCa.Where(t => !t.PhuongTien.LaDauMay).ToList();
            int toaDenHan = toa.Count(t => t.TrangThai is "QUA_HAN" or "SAP_TOI");
            int toaChuaCo = toa.Count(t => t.LanGanNhat == null);
            bool locToa = LayTagDangChon(cboLoaiPhuongTien) == "TOA_XE";

            txtTheToaXeTieuDe.Text = $"TOA XE ({toa.Count})";
            txtTheToaXeDenHan.Text = toaDenHan > 0 ? $"{toaDenHan} toa đến hạn" : "Không toa nào đến hạn";
            txtTheToaXeDenHan.Foreground = Mau(toaDenHan > 0 ? "#B45309" : "#15803D");
            txtTheToaXeMoTa.Text = toaChuaCo > 0 ? $"{toaChuaCo} toa chưa có lịch sử bảo dưỡng" : "Tất cả toa đã có lịch sử";
            bdTheToaXe.Background = Mau(toaDenHan > 0 ? "#FFFBEB" : "#F8FAFC");
            bdTheToaXe.BorderBrush = Mau(locToa ? "#003B73" : toaDenHan > 0 ? "#FDE68A" : "#CBD5E1");
            bdTheToaXe.BorderThickness = new Thickness(locToa ? 2 : 1);

            // KPI dai tieu de: tinh ca dau may lan toa xe
            int denHan = tatCa.Count(t => t.TrangThai is "QUA_HAN" or "SAP_TOI");
            int quaHan = tatCa.Count(t => t.TrangThai == "QUA_HAN");
            txtKpiDenHan.Text = denHan.ToString("N0");
            txtKpiDenHanPhu.Text = quaHan > 0 ? $"· {quaHan} quá hạn" : "phương tiện";
        }

        private void ThePhuongTienBd_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not TinhTrangBaoDuongHienThi tt) return;

            // Bam lai the dang chon thi bo loc
            _khoaPhuongTienLoc = _khoaPhuongTienLoc == tt.PhuongTien.Khoa ? null : tt.PhuongTien.Khoa;
            if (_khoaPhuongTienLoc != null) ChonTheoTag(cboLoaiPhuongTien, "ALL");
            LamMoiBaoDuong(giuDongChon: false);
        }

        private void ThePhuongTienBd_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not TinhTrangBaoDuongHienThi tt) return;
            e.Handled = true;

            // Hai lan Click cua cu nhap dup da bat roi tat loc: dat lai loc theo phuong tien nay
            _khoaPhuongTienLoc = tt.PhuongTien.Khoa;
            LamMoiBaoDuong(giuDongChon: false);
            GhiNhanBaoDuong(tt.PhuongTien.Khoa);
        }

        private void TheToaXeBd_Click(object sender, RoutedEventArgs e)
        {
            bool dangLocToa = LayTagDangChon(cboLoaiPhuongTien) == "TOA_XE";
            _khoaPhuongTienLoc = null;
            ChonTheoTag(cboLoaiPhuongTien, dangLocToa ? "ALL" : "TOA_XE");
            LamMoiBaoDuong(giuDongChon: false);
        }

        private void BtnBoLocPhuongTien_Click(object sender, RoutedEventArgs e)
        {
            _khoaPhuongTienLoc = null;
            LamMoiBaoDuong(giuDongChon: true);
        }

        // =====================================================================
        // DANH SACH PHIEU BAO DUONG
        // =====================================================================

        private void NapDanhSachBaoDuong(List<BaoDuongHienThi> toanBo)
        {
            string tuKhoa = txtTimBaoDuong.Text?.Trim() ?? string.Empty;
            string loai = LayTagDangChon(cboLoaiPhuongTien) ?? "ALL";
            string cap = LayTagDangChon(cboCapBaoDuong) ?? "ALL";

            var danhSach = toanBo.Where(bd => KhopBoLocBaoDuong(bd, tuKhoa, loai, cap, _khoaPhuongTienLoc))
                                 .OrderByDescending(bd => bd.ThoiDiemHoanThanh)
                                 .ToList();

            dgBaoDuong.ItemsSource = danhSach;
            txtBadgeBaoDuong.Text = danhSach.Count.ToString();
            txtTrongBaoDuong.Visibility = danhSach.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtTrongBaoDuong.Text = _khoaPhuongTienLoc != null && danhSach.Count == 0
                ? "Phương tiện này chưa có phiếu bảo dưỡng nào — nhấp đúp vào thẻ để ghi nhận."
                : "Không có phiếu bảo dưỡng nào khớp điều kiện lọc.";

            var ptLoc = _khoaPhuongTienLoc != null ? DanhMucMauBaoTri.TimPhuongTien(_khoaPhuongTienLoc) : null;
            bdLocPhuongTien.Visibility = ptLoc != null ? Visibility.Visible : Visibility.Collapsed;
            txtLocPhuongTien.Text = ptLoc != null ? $"{ptLoc.NhanLoai}: {ptLoc.SoHieu}" : "";
        }

        private static bool KhopBoLocBaoDuong(BaoDuongHienThi bd, string tuKhoa, string loai, string cap, string? khoa)
        {
            if (tuKhoa.Length > 0 &&
                !bd.MaPhieu.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !bd.SoHieuPhuongTien.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !bd.TenNguoiThucHien.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !bd.GhiChuKyThuat.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
                return false;

            if (loai != "ALL" && bd.LoaiPhuongTien != loai) return false;
            if (cap != "ALL" && bd.CapBaoDuong != cap) return false;
            if (khoa != null && bd.KhoaPhuongTien != khoa) return false;
            return true;
        }

        private bool ChonDongBaoDuong(int maBaoDuong)
        {
            if (dgBaoDuong.ItemsSource is not IEnumerable<BaoDuongHienThi> ds) return false;

            var muc = ds.FirstOrDefault(bd => bd.MaBaoDuong == maBaoDuong);
            if (muc == null) return false;

            dgBaoDuong.SelectedItem = muc;
            dgBaoDuong.ScrollIntoView(muc);
            return true;
        }

        private void TxtTimBaoDuong_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            LamMoiBaoDuong(giuDongChon: true);
        }

        private void BoLocBaoDuong_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            LamMoiBaoDuong(giuDongChon: true);
        }

        private void BtnXoaLocBaoDuong_Click(object sender, RoutedEventArgs e)
        {
            _dangNapDuLieu = true;
            txtTimBaoDuong.Text = string.Empty;
            cboLoaiPhuongTien.SelectedIndex = 0;
            cboCapBaoDuong.SelectedIndex = 0;
            _khoaPhuongTienLoc = null;
            _dangNapDuLieu = false;

            LamMoiBaoDuong(giuDongChon: true);
        }

        // =====================================================================
        // CHI TIET PHIEU
        // =====================================================================

        private void DgBaoDuong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var bd = dgBaoDuong.SelectedItem as BaoDuongHienThi;
            btnSuaBaoDuong.IsEnabled = bd != null;
            btnSuaBaoDuongChiTiet.IsEnabled = bd != null;

            if (bd == null)
            {
                txtBdTieuDe.Text = "PHIẾU BẢO DƯỠNG";
                txtBdMoTa.Text = "Chọn một phiếu để xem chi tiết";
                txtBdMa.Text = "—";
                pnlBdChiTiet.Visibility = Visibility.Collapsed;
                txtBdTrong.Visibility = Visibility.Visible;
                return;
            }
            HienThiChiTietBaoDuong(bd);
        }

        private void HienThiChiTietBaoDuong(BaoDuongHienThi bd)
        {
            pnlBdChiTiet.Visibility = Visibility.Visible;
            txtBdTrong.Visibility = Visibility.Collapsed;

            txtBdTieuDe.Text = $"{bd.SoHieuPhuongTien} · Bảo dưỡng {bd.CapBaoDuong}";
            txtBdMoTa.Text = $"Hoàn thành {bd.ThoiDiemHoanThanh:HH:mm dd/MM/yyyy} · {bd.TenNguoiThucHien}";
            txtBdMa.Text = bd.LaThayDoiTam ? $"{bd.MaPhieu} · tạm" : bd.MaPhieu;

            // --- Cap da lam ---
            bdBdCapNhan.Background = Mau(bd.NenCap);
            txtBdCap.Text = bd.CapBaoDuong;
            txtBdCap.Foreground = Mau(bd.MauCap);
            txtBdCapTieuDe.Text = $"Bảo dưỡng cấp {bd.CapBaoDuong} · {bd.NhanLoai.ToLower()} {bd.SoHieuPhuongTien}";
            txtBdCapMoTa.Text = bd.LaDauMay
                ? $"Tại {bd.SoKmTaiThoiDiem:N0} km · {ChuKyBaoDuong.MoTaChuKy(bd.CapBaoDuong)}"
                : $"Ngày {bd.ThoiDiemHoanThanh:dd/MM/yyyy} · {ChuKyBaoDuong.MoTaChuKy(bd.CapBaoDuong)}";

            // --- Tinh trang hien tai cua phuong tien ---
            var pt = DanhMucMauBaoTri.TimPhuongTien(bd.KhoaPhuongTien);
            var lichSu = LayToanBoBaoDuong().Where(x => x.KhoaPhuongTien == bd.KhoaPhuongTien)
                                            .OrderByDescending(x => x.ThoiDiemHoanThanh)
                                            .ToList();
            var tienDo = pt != null ? ChuKyBaoDuong.TinhTienDo(pt, lichSu) : new List<TienDoCapBaoDuong>();
            icBdTienDo.ItemsSource = tienDo;
            txtBdTieuDeTinhTrang.Text = $"TÌNH TRẠNG HIỆN TẠI CỦA {bd.SoHieuPhuongTien}";

            var chuY = ChuKyBaoDuong.CapCanChuY(tienDo);
            bool canLam = chuY != null && chuY.TrangThai is "QUA_HAN" or "SAP_TOI";
            btnBdGhiNhanTiep.Visibility = canLam ? Visibility.Visible : Visibility.Collapsed;
            if (canLam) txtBdGhiNhanTiep.Text = $"Ghi nhận {chuY!.Cap} cho {bd.SoHieuPhuongTien} ({chuY.ConLai})";

            // --- Thong tin phieu ---
            var pnl = pnlBdThongTin;
            pnl.Children.Clear();
            ThemDongThongSo(pnl, "Loại phương tiện", bd.NhanLoai);
            ThemDongThongSo(pnl, "Phương tiện", $"{bd.SoHieuPhuongTien} · {bd.MoTaPhuongTien}");
            ThemDongThongSo(pnl, "Km tại thời điểm", $"{bd.SoKmTaiThoiDiem:N1} km");
            if (bd.LaDauMay && pt != null)
                ThemDongThongSo(pnl, "Đã chạy kể từ phiếu này", $"{Math.Max(0, pt.SoKmTichLuy - bd.SoKmTaiThoiDiem):N0} km");
            ThemDongThongSo(pnl, "Số ngày kể từ phiếu này", $"{Math.Max(0, (DateTime.Now - bd.ThoiDiemHoanThanh).Days):N0} ngày");
            ThemDongThongSo(pnl, "Người thực hiện", string.IsNullOrEmpty(bd.TenNguoiThucHien) ? "(không ghi)" : bd.TenNguoiThucHien);
            ThemDongThongSo(pnl, "Khóa tham chiếu", bd.LaDauMay ? $"MaDauMay #{bd.MaDauMay} · MaToaXe trống"
                                                                : $"MaToaXe #{bd.MaToaXe} · MaDauMay trống", laDongCuoi: true);

            txtBdGhiChu.Text = string.IsNullOrWhiteSpace(bd.GhiChuKyThuat) ? "(Không có ghi chú)" : bd.GhiChuKyThuat;

            // --- Lich su ---
            icBdLichSu.ItemsSource = lichSu;
            txtBdTieuDeLichSu.Text = $"LỊCH SỬ BẢO DƯỠNG CỦA {bd.SoHieuPhuongTien} ({lichSu.Count})";
        }

        private void DongLichSuBaoDuong_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not BaoDuongHienThi bd) return;
            if (ChonDongBaoDuong(bd.MaBaoDuong)) return;

            // Bi bo loc an mat: xoa loc roi chon lai
            BtnXoaLocBaoDuong_Click(this, new RoutedEventArgs());
            ChonDongBaoDuong(bd.MaBaoDuong);
        }

        private void BtnBdGhiNhanTiep_Click(object sender, RoutedEventArgs e)
        {
            if (dgBaoDuong.SelectedItem is BaoDuongHienThi bd) GhiNhanBaoDuong(bd.KhoaPhuongTien);
        }

        // =====================================================================
        // GHI NHAN / SUA PHIEU BAO DUONG
        // =====================================================================

        private void BtnGhiNhanBaoDuong_Click(object sender, RoutedEventArgs e) => GhiNhanBaoDuong(_khoaPhuongTienLoc);

        private void BtnSuaBaoDuong_Click(object sender, RoutedEventArgs e) => SuaBaoDuong();

        private void DongBaoDuong_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SuaBaoDuong();

        // chuyenTab = false khi goi tu tab Canh bao: mo dialog tai cho, khong nhay tab
        private void GhiNhanBaoDuong(string? khoaPhuongTien = null, bool chuyenTab = true)
        {
            if (chuyenTab) tabBaoTri.SelectedIndex = 2;

            var dlg = new BaoDuongDialog(null, LayToanBoBaoDuong(), khoaPhuongTien) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var bd = dlg.KetQua;
            bd.MaBaoDuong = _maTamKeTiep--;
            _baoDuongTam[bd.MaBaoDuong] = bd;

            LamMoiSauKhiLuuBaoDuong(bd);
            BaoKetQuaLuuBaoDuong(bd, laLapMoi: true);
        }

        private void SuaBaoDuong()
        {
            if (dgBaoDuong.SelectedItem is not BaoDuongHienThi dangChon) return;

            var dlg = new BaoDuongDialog(dangChon.SaoChep(), LayToanBoBaoDuong()) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var bd = dlg.KetQua;
            _baoDuongTam[bd.MaBaoDuong] = bd;

            LamMoiSauKhiLuuBaoDuong(bd);
            BaoKetQuaLuuBaoDuong(bd, laLapMoi: false);
        }

        private void LamMoiSauKhiLuuBaoDuong(BaoDuongHienThi bd)
        {
            LamMoiBaoDuong(giuDongChon: false, maChonSau: bd.MaBaoDuong);

            // Bo loc hien tai an mat phieu vua luu thi xoa loc de nguoi dung thay ngay
            if ((dgBaoDuong.SelectedItem as BaoDuongHienThi)?.MaBaoDuong != bd.MaBaoDuong)
            {
                _dangNapDuLieu = true;
                txtTimBaoDuong.Text = string.Empty;
                cboLoaiPhuongTien.SelectedIndex = 0;
                cboCapBaoDuong.SelectedIndex = 0;
                _dangNapDuLieu = false;
                if (_khoaPhuongTienLoc != null && _khoaPhuongTienLoc != bd.KhoaPhuongTien) _khoaPhuongTienLoc = null;

                LamMoiBaoDuong(giuDongChon: false, maChonSau: bd.MaBaoDuong);
            }

            LamMoiCanhBao();
        }

        private void BaoKetQuaLuuBaoDuong(BaoDuongHienThi bd, bool laLapMoi)
        {
            // Moc ke tiep sau khi ghi nhan, de nguoi dung thay ngay tac dung cua phieu
            var pt = DanhMucMauBaoTri.TimPhuongTien(bd.KhoaPhuongTien);
            string mocKeTiep = "";
            if (pt != null)
            {
                var tienDo = ChuKyBaoDuong.TinhTienDo(pt, LayToanBoBaoDuong().Where(x => x.KhoaPhuongTien == pt.Khoa));
                mocKeTiep = string.Join("\n", tienDo.Select(t => $"   • {t.Cap}: {t.NhanTrangThai} — {t.ConLai}"));
            }

            ThongBaoDialog.ThanhCong(
                $"{(laLapMoi ? "Đã ghi nhận" : "Đã cập nhật")} bảo dưỡng cấp {bd.CapBaoDuong} cho " +
                $"{bd.NhanLoai.ToLower()} {bd.SoHieuPhuongTien} tại {bd.SoKmTaiThoiDiem:N0} km.\n\n" +
                (mocKeTiep.Length > 0 ? $"Tình trạng sau khi ghi nhận:\n{mocKeTiep}\n\n" : "") +
                "Phiếu đang hiển thị trên màn hình, chưa ghi vào CSDL.",
                "Lưu phiếu bảo dưỡng");
        }
    }
}
