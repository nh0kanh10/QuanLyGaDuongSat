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
    // DIALOG GHI NHAN / CAP NHAT PHIEU BAO DUONG DINH KY (baotri.NhatKyBaoDuong)
    //
    // - Loai phuong tien quyet dinh FK va cap hop le: dau may -> R1/R2,
    //   toa xe -> D1/D2 (CK_PhuongTien_Ref; ghep cap theo loai la quy uoc giao dien).
    // - Tu goi y cap can lam nhat, xem truoc moc ke tiep sau khi ghi nhan.
    // - Tu 24/10: kiem tra bang QuyTacBaoTri.KiemTraBaoDuong (cung ham Service dung) roi goi ham luu
    //   cua trang (BaoTriService.LuuBaoDuong). Phuong tien dang "Bao duong" -> o "Dua ve San sang".
    //   Phuong tien ngung khai thac khong hien trong danh sach (khong ghi bao duong).
    // =========================================================================
    public partial class BaoDuongDialog : Window
    {
        private readonly BaoDuongHienThi? _banGoc;                 // null = ghi nhan moi
        private readonly BaoTriDuLieu _duLieu;
        private readonly Func<BaoDuongDTO, bool, string?> _luu;
        private readonly IReadOnlyList<BaoDuongHienThi> _toanBoLichSu;
        private readonly DateTime _thoiDiem;
        private bool _dangKhoiTao;

        // Khi lap moi va nguoi dung chua tu go chi phi: tu dien lai gia tri de xuat moi lan doi cap
        private bool _chiPhiTuDong = true;
        private bool _dangTuDienChiPhi;

        // luu(phieu, duaVeSanSang): tra null khi ghi duoc, nguoc lai la loi de hien
        public BaoDuongDialog(BaoDuongHienThi? banGoc, BaoTriDuLieu duLieu, Func<BaoDuongDTO, bool, string?> luu,
                              string? khoaPhuongTienMacDinh = null)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _banGoc = banGoc;
            _duLieu = duLieu;
            _luu = luu;
            _toanBoLichSu = duLieu.BaoDuong;
            _thoiDiem = banGoc?.ThoiDiemHoanThanh ?? DateTime.Now;

            ONhapSoThucHelper.Gan(txtSoKm, 1, coDinhSoLe: false);
            ONhapSoThucHelper.Gan(txtChiPhi, 0, coDinhSoLe: false);

            // Nguoi thuc hien cu khong con trong danh sach chon duoc thi van hien de giu nguyen khi sua
            var dsNguoi = duLieu.NguoiKyThuat.ToList();
            if (banGoc?.MaNguoiThucHien is int maCu && dsNguoi.All(n => n.MaTaiKhoan != maCu))
                dsNguoi.Insert(0, new TaiKhoanMau(maCu, "", banGoc.TenNguoiThucHien.Length > 0 ? banGoc.TenNguoiThucHien : $"Tài khoản #{maCu}", "người thực hiện cũ"));
            cboNguoi.ItemsSource = dsNguoi;

            if (banGoc == null)
                KhoiTaoThemMoi(khoaPhuongTienMacDinh);
            else
                KhoiTaoCapNhat(banGoc);

            _dangKhoiTao = false;
            CapNhatOSoKm();
            CapNhatXemTruoc();

            Loaded += (_, _) =>
            {
                Control oDau = cboPhuongTien.SelectedItem == null ? cboPhuongTien : KmTheoDongHo ? txtChiPhi : txtSoKm;
                oDau.Focus();
                if (oDau is TextBox tb) tb.SelectAll();
            };
        }

        private void KhoiTaoThemMoi(string? khoaMacDinh)
        {
            txtTieuDe.Text = "GHI NHẬN BẢO DƯỠNG ĐỊNH KỲ";
            txtTieuDePhu.Text = "Đầu máy cấp R1 / R2 · toa xe cấp D1 / D2";
            txtNutLuu.Text = "Lưu Phiếu Bảo Dưỡng";
            txtThoiDiem.Text = "Hoàn thành: giờ máy chủ lúc lưu";
            txtTieuDeHienTai.Text = "TÌNH TRẠNG HIỆN TẠI";

            var ptMacDinh = khoaMacDinh != null ? _duLieu.TimPhuongTien(khoaMacDinh) : null;
            if (ptMacDinh?.NgungKhaiThac == true) ptMacDinh = null;
            string loai = ptMacDinh?.LoaiPhuongTien ?? "DAU_MAY";

            (loai == "DAU_MAY" ? rdoDauMay : rdoToaXe).IsChecked = true;
            NapPhuongTien(loai);
            cboPhuongTien.SelectedItem = ptMacDinh;
            cboNguoi.SelectedIndex = _duLieu.NguoiKyThuat.Count > 0 ? 0 : -1;
            if (_duLieu.NguoiKyThuat.Count == 0)
                txtLoiNguoi.Text = "Chưa có tài khoản kỹ thuật (vai trò KY_THUAT) đang hoạt động.";

            if (ptMacDinh != null) DienGoiYTheoPhuongTien();
            CapNhatODuaVeSanSang();
        }

        private void KhoiTaoCapNhat(BaoDuongHienThi bd)
        {
            txtTieuDe.Text = $"CẬP NHẬT PHIẾU {bd.MaPhieu} — {bd.SoHieuPhuongTien}";
            txtTieuDePhu.Text = "Phương tiện và thời điểm hoàn thành là thông tin gốc của phiếu nên không sửa";
            txtNutLuu.Text = "Lưu Thay Đổi";
            txtThoiDiem.Text = $"Hoàn thành: {_thoiDiem:HH:mm dd/MM/yyyy} (giữ nguyên)";
            txtTieuDeHienTai.Text = "TRƯỚC KHI SỬA";

            (bd.LaDauMay ? rdoDauMay : rdoToaXe).IsChecked = true;
            rdoDauMay.IsEnabled = false;
            rdoToaXe.IsEnabled = false;
            NapPhuongTien(bd.LoaiPhuongTien);
            var pt = _duLieu.TimPhuongTien(bd.KhoaPhuongTien);
            cboPhuongTien.ItemsSource = pt != null ? new List<PhuongTienBaoDuong> { pt } : new List<PhuongTienBaoDuong>();
            cboPhuongTien.SelectedItem = pt;
            cboPhuongTien.IsEnabled = false;

            ChonCap(bd.CapBaoDuong);
            txtSoKm.Text = bd.SoKmTaiThoiDiem.ToString("0.#", FormatHelper.TechnicalCulture);
            cboNguoi.SelectedItem = (cboNguoi.ItemsSource as IEnumerable<TaiKhoanMau>)?.FirstOrDefault(n => n.MaTaiKhoan == bd.MaNguoiThucHien);
            txtGhiChu.Text = bd.GhiChuKyThuat;
            txtChiPhi.Text = bd.ChiPhi.ToString("0", FormatHelper.TechnicalCulture);
            _chiPhiTuDong = false;      // dang sua: giu dung chi phi da luu, khong tu doi theo cap
        }

        // =====================================================================
        // LOAI PHUONG TIEN / PHUONG TIEN / CAP
        // =====================================================================

        private string LoaiDangChon => rdoToaXe.IsChecked == true ? "TOA_XE" : "DAU_MAY";

        // Phieu MOI cua dau may ghi tai km dong ho hien tai (quyet dinh 26/10, cung luat QuyTacBaoTri) -> o km
        // chi doc, tu dien theo dau may. Toa xe (CSDL khong theo doi km toa) va sua phieu cu thi van go tay.
        private bool KmTheoDongHo => _banGoc == null && LoaiDangChon == "DAU_MAY";

        private void CapNhatOSoKm()
        {
            bool khoa = KmTheoDongHo;
            txtSoKm.IsReadOnly = khoa;
            txtSoKm.IsTabStop = !khoa;
            if (khoa) txtSoKm.Background = Mau("#F1F5F9");
            else txtSoKm.ClearValue(BackgroundProperty);
            txtSoKm.ToolTip = khoa
                ? "Phiếu mới ghi tại km tích lũy hiện tại của đầu máy (tự điền, không sửa ở đây). Đồng hồ sai thì sửa ở màn Phương tiện trước."
                : "Số km tích lũy của phương tiện lúc hoàn thành bảo dưỡng, 1 chữ số thập phân";
        }

        private void NapPhuongTien(string loai)
        {
            runNhanPhuongTien.Text = loai == "DAU_MAY" ? "Đầu máy" : "Toa xe";
            // Phuong tien ngung khai thac khong ghi bao duong
            cboPhuongTien.ItemsSource = _duLieu.PhuongTien.Where(p => p.LoaiPhuongTien == loai && !p.NgungKhaiThac).ToList();
            cboPhuongTien.SelectedIndex = -1;

            string[] cap = ChuKyBaoDuong.CapCua(loai);
            rdoCap1.Content = cap[0];
            rdoCap2.Content = cap[1];
            rdoCap1.Tag = ChuKyBaoDuong.MauCap(cap[0]);
            rdoCap2.Tag = ChuKyBaoDuong.MauCap(cap[1]);
            rdoCap1.IsChecked = false;
            rdoCap2.IsChecked = false;
        }

        private string? CapDangChon
            => rdoCap1.IsChecked == true ? rdoCap1.Content?.ToString()
             : rdoCap2.IsChecked == true ? rdoCap2.Content?.ToString()
             : null;

        private void ChonCap(string cap)
        {
            if (rdoCap1.Content?.ToString() == cap) rdoCap1.IsChecked = true;
            else if (rdoCap2.Content?.ToString() == cap) rdoCap2.IsChecked = true;
        }

        private void LoaiPhuongTien_Checked(object sender, RoutedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiPhuongTien.Text = "";
            txtLoiCap.Text = "";
            NapPhuongTien(LoaiDangChon);
            txtSoKm.Text = "";
            CapNhatOSoKm();
            CapNhatODuaVeSanSang();
            CapNhatXemTruoc();
        }

        private void CboPhuongTien_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiPhuongTien.Text = "";
            DienGoiYTheoPhuongTien();
            CapNhatODuaVeSanSang();
            CapNhatXemTruoc();
        }

        // Phuong tien dang "Bao duong": phieu ghi khi da lam xong -> hoi dua ve "San sang" (mac dinh co).
        // Dang chay: Service khong cho ghi - bao truoc o day.
        private void CapNhatODuaVeSanSang()
        {
            var pt = cboPhuongTien.SelectedItem as PhuongTienBaoDuong;
            bool hien = _banGoc == null && pt?.TrangThai == "BAO_DUONG";
            chkDuaVeSanSang.Visibility = hien ? Visibility.Visible : Visibility.Collapsed;
            if (hien)
            {
                chkDuaVeSanSang.IsChecked = true;
                txtDuaVeSanSang.Text = $"{pt!.SoHieu} đang ở trạng thái Bảo dưỡng — đưa về Sẵn sàng sau khi lưu";
            }
            if (_banGoc == null && pt?.DangChay == true)
                txtLoiPhuongTien.Text = pt.LaDauMay
                    ? $"{pt.SoHieu} đang kéo tàu, chưa ghi nhận bảo dưỡng được."
                    : $"Toa {pt.SoHieu} đang chạy trong đoàn tàu, chưa ghi nhận bảo dưỡng được.";
        }

        // Lap moi: chon san cap can lam nhat + dien san so km
        private void DienGoiYTheoPhuongTien()
        {
            if (_banGoc != null || cboPhuongTien.SelectedItem is not PhuongTienBaoDuong pt) return;

            var lichSu = LichSuCua(pt, boQuaBanGoc: false);
            var chuY = ChuKyBaoDuong.CapCanChuY(ChuKyBaoDuong.TinhTienDo(pt, lichSu));
            string capGoiY = chuY?.Cap ?? ChuKyBaoDuong.CapCua(pt.LoaiPhuongTien)[0];
            ChonCap(capGoiY);

            if (_chiPhiTuDong)
            {
                _dangTuDienChiPhi = true;
                txtChiPhi.Text = ChuKyBaoDuong.ChiPhiThamKhao(capGoiY).ToString("0", FormatHelper.TechnicalCulture);
                _dangTuDienChiPhi = false;
            }

            bool cu = _dangKhoiTao;
            _dangKhoiTao = true;
            txtSoKm.Text = pt.LaDauMay
                ? pt.SoKmTichLuy.ToString("0.#", FormatHelper.TechnicalCulture)
                : lichSu.OrderByDescending(x => x.ThoiDiemHoanThanh).FirstOrDefault()?.SoKmTaiThoiDiem
                        .ToString("0.#", FormatHelper.TechnicalCulture) ?? "";
            txtLoiSoKm.Text = "";
            _dangKhoiTao = cu;
        }

        private void CapBaoDuong_Checked(object sender, RoutedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiCap.Text = "";
            if (_chiPhiTuDong && CapDangChon != null)
            {
                _dangTuDienChiPhi = true;
                txtChiPhi.Text = ChuKyBaoDuong.ChiPhiThamKhao(CapDangChon).ToString("0", FormatHelper.TechnicalCulture);
                _dangTuDienChiPhi = false;
            }
            CapNhatXemTruoc();
        }

        private void TruongNhap_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            if (sender == txtSoKm) txtLoiSoKm.Text = "";
            if (sender == txtChiPhi)
            {
                txtLoiChiPhi.Text = "";
                if (!_dangTuDienChiPhi) _chiPhiTuDong = false;    // nguoi dung da tu sua, khong tu doi theo cap nua
            }
            CapNhatXemTruoc();
        }

        private void CboNguoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            txtLoiNguoi.Text = "";
        }

        // Lich su bao duong cua phuong tien (da tron thay doi tam); khi sua thi bo phieu dang sua
        private List<BaoDuongHienThi> LichSuCua(PhuongTienBaoDuong pt, bool boQuaBanGoc)
            => _toanBoLichSu.Where(x => x.KhoaPhuongTien == pt.Khoa &&
                                        !(boQuaBanGoc && _banGoc != null && x.MaBaoDuong == _banGoc.MaBaoDuong))
                            .ToList();

        // =====================================================================
        // XEM TRUOC TRUC TIEP
        // =====================================================================

        private void CapNhatXemTruoc()
        {
            string? cap = CapDangChon;
            txtMoTaCap.Text = cap != null ? ChuKyBaoDuong.MoTaChuKy(cap) : "Chọn cấp bảo dưỡng đã thực hiện.";

            if (cboPhuongTien.SelectedItem is not PhuongTienBaoDuong pt)
            {
                icTienDoHienTai.ItemsSource = null;
                icTienDoSau.ItemsSource = null;
                bdGoiYCap.Visibility = Visibility.Collapsed;
                txtGoiYSoKm.Text = "";
                return;
            }

            // --- Tien do hien tai (khi sua: gom ca ban goc) ---
            var lichSuKhac = LichSuCua(pt, boQuaBanGoc: true);
            var lichSuHienTai = _banGoc != null ? lichSuKhac.Append(_banGoc).ToList() : lichSuKhac;
            var tienDoHienTai = ChuKyBaoDuong.TinhTienDo(pt, lichSuHienTai);
            icTienDoHienTai.ItemsSource = tienDoHienTai;

            // --- Sau khi ghi nhan: thay phieu nay bang ban xem truoc ---
            decimal? km = ONhapSoThucHelper.Lay(txtSoKm);
            if (cap != null && km.HasValue)
            {
                var banXemTruoc = new BaoDuongHienThi
                {
                    CapBaoDuong = cap,
                    SoKmTaiThoiDiem = km.Value,
                    ThoiDiemHoanThanh = _thoiDiem
                };
                banXemTruoc.GanPhuongTien(pt);
                icTienDoSau.ItemsSource = ChuKyBaoDuong.TinhTienDo(pt, lichSuKhac.Append(banXemTruoc));
            }
            else
            {
                icTienDoSau.ItemsSource = null;
            }

            // --- Goi y cap ---
            var chuY = ChuKyBaoDuong.CapCanChuY(tienDoHienTai);
            HienGoiYCap(pt, chuY, cap);

            // --- Goi y so km ---
            var lanTruoc = lichSuKhac.Where(x => x.ThoiDiemHoanThanh < _thoiDiem)
                                     .OrderByDescending(x => x.ThoiDiemHoanThanh).FirstOrDefault();
            string truoc = lanTruoc != null ? $"Lần trước: {lanTruoc.SoKmTaiThoiDiem:N0} km ({lanTruoc.CapBaoDuong})." : "Chưa có lần bảo dưỡng trước.";
            txtGoiYSoKm.Text = !pt.LaDauMay
                ? $"CSDL không theo dõi km của toa — nhập theo lý lịch toa. {truoc}"
                : KmTheoDongHo
                    ? $"Lấy theo km tích lũy hiện tại của đầu máy ({pt.SoKmTichLuy:N0} km). {truoc}"
                    : $"Km tích lũy hiện tại: {pt.SoKmTichLuy:N0} km. {truoc}";
        }

        private void HienGoiYCap(PhuongTienBaoDuong pt, TienDoCapBaoDuong? chuY, string? capChon)
        {
            // Khi sua, tinh trang "hien tai" da gom chinh phieu nay => goi y de gay hieu lam, an di
            if (_banGoc != null)
            {
                bdGoiYCap.Visibility = Visibility.Collapsed;
                return;
            }
            bdGoiYCap.Visibility = Visibility.Visible;
            bool canLam = chuY != null && chuY.TrangThai is "QUA_HAN" or "SAP_TOI";
            bool chonThapHon = canLam && capChon != null && !ChuKyBaoDuong.LaCapCao(capChon) && ChuKyBaoDuong.LaCapCao(chuY!.Cap);

            string mauChu, mauNen, mauVien;
            SymbolRegular bieuTuong;
            string noiDung;

            if (chonThapHon)
            {
                (mauChu, mauNen, mauVien, bieuTuong) = ("#B91C1C", "#FEF2F2", "#FECACA", SymbolRegular.Warning24);
                noiDung = $"{pt.SoHieu} đang {chuY!.CumTrangThai} ({chuY.ConLai}). " +
                          $"Cấp {capChon} không làm mới mốc {chuY.Cap} — nên chọn {chuY.Cap} (bao gồm {capChon}).";
            }
            else if (canLam)
            {
                (mauChu, mauNen, mauVien, bieuTuong) = chuY!.TrangThai == "QUA_HAN"
                    ? ("#B91C1C", "#FEF2F2", "#FECACA", SymbolRegular.ErrorCircle24)
                    : ("#B45309", "#FFFBEB", "#FDE68A", SymbolRegular.Warning24);
                noiDung = $"{pt.SoHieu} đang {chuY.CumTrangThai} ({chuY.ConLai}). " +
                          (capChon == chuY.Cap || (capChon != null && ChuKyBaoDuong.LaCapCao(capChon))
                              ? $"Ghi nhận {capChon} sẽ làm mới mốc này."
                              : $"Gợi ý chọn cấp {chuY.Cap}.");
            }
            else
            {
                (mauChu, mauNen, mauVien, bieuTuong) = ("#1D4ED8", "#EFF6FF", "#BFDBFE", SymbolRegular.Info24);
                noiDung = chuY?.DaDung.HasValue == true
                    ? $"{pt.SoHieu} chưa tới hạn cấp nào. Gần nhất: {chuY.Cap} ({chuY.ConLai})."
                    : $"{pt.SoHieu} chưa có lịch sử bảo dưỡng — phiếu này sẽ là mốc đầu tiên để tính chu kỳ.";
            }

            bdGoiYCap.Background = Mau(mauNen);
            bdGoiYCap.BorderBrush = Mau(mauVien);
            icoGoiYCap.Symbol = bieuTuong;
            icoGoiYCap.Foreground = Mau(mauChu);
            txtGoiYCap.Foreground = Mau(mauChu);
            txtGoiYCap.Text = noiDung;
        }

        // =====================================================================
        // LUU / HUY
        // =====================================================================

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!KiemTraHopLe(out var bd)) return;

            // Ghi CSDL qua trang (BaoTriService). Loi nghiep vu / CSDL: bao va giu dialog de sua.
            bool duaVeSanSang = chkDuaVeSanSang.Visibility == Visibility.Visible && chkDuaVeSanSang.IsChecked == true;
            string? loi = _luu(bd, duaVeSanSang);
            if (loi != null)
            {
                ThongBaoDialog.CanhBao(loi, "Chưa lưu được phiếu bảo dưỡng");
                return;
            }
            DialogResult = true;
        }

        private bool KiemTraHopLe(out BaoDuongDTO bd)
        {
            bool hopLe = true;
            Control? oLoiDauTien = null;

            void BaoLoi(TextBlock tbLoi, string thongDiep, Control o)
            {
                if (tbLoi.Text.Length == 0) tbLoi.Text = thongDiep;
                hopLe = false;
                oLoiDauTien ??= o;
            }

            foreach (var tb in new[] { txtLoiPhuongTien, txtLoiCap, txtLoiSoKm, txtLoiNguoi, txtLoiChiPhi })
                tb.Text = "";

            var pt = cboPhuongTien.SelectedItem as PhuongTienBaoDuong;
            decimal? km = ONhapSoThucHelper.Lay(txtSoKm);
            decimal? chiPhi = ONhapSoThucHelper.Lay(txtChiPhi);
            // O km tu dien theo dau may (chi doc): chua chon dau may thi chi bao o o phuong tien
            if (!km.HasValue && !(KmTheoDongHo && pt == null))
                BaoLoi(txtLoiSoKm, "Vui lòng nhập số km tại thời điểm bảo dưỡng.", txtSoKm);
            if (!chiPhi.HasValue) BaoLoi(txtLoiChiPhi, "Chi phí không hợp lệ (nhập 0 nếu không có).", txtChiPhi);
            if (_banGoc == null && pt?.DangChay == true)
                BaoLoi(txtLoiPhuongTien, pt.LaDauMay ? $"{pt.SoHieu} đang kéo tàu, chưa ghi nhận bảo dưỡng được."
                                                     : $"Toa {pt.SoHieu} đang chạy trong đoàn tàu, chưa ghi nhận bảo dưỡng được.", cboPhuongTien);

            bd = _banGoc?.ToDTO() ?? new BaoDuongDTO();
            bd.LoaiPhuongTien = pt?.LoaiPhuongTien ?? LoaiDangChon;
            bd.MaDauMay = pt != null && pt.LaDauMay ? pt.MaPhuongTien : null;
            bd.MaToaXe = pt != null && !pt.LaDauMay ? pt.MaPhuongTien : null;
            bd.CapBaoDuong = CapDangChon ?? "";
            bd.SoKmTaiThoiDiem = km ?? 0m;
            bd.MaNguoiThucHien = (cboNguoi.SelectedItem as TaiKhoanMau)?.MaTaiKhoan;
            bd.GhiChuKyThuat = txtGhiChu.Text.Trim();
            bd.ChiPhi = chiPhi ?? 0m;

            // Lich su truoc phieu nay cua cung phuong tien (khi sua: phieu dang sua la moi nhat)
            var lichSuTruoc = pt != null
                ? LichSuCua(pt, boQuaBanGoc: true).Select(x => x.ToDTO()).ToList()
                : new List<BaoDuongDTO>();

            // Cung luat voi Service (dialog chi doi ten truong sang o nhap)
            foreach (var l in QuyTacBaoTri.KiemTraBaoDuong(bd, pt?.LaDauMay == true ? pt.SoKmTichLuy : null, lichSuTruoc))
            {
                switch (l.Truong)
                {
                    case "PhuongTien": BaoLoi(txtLoiPhuongTien, l.ThongDiep, cboPhuongTien); break;
                    case "Cap": BaoLoi(txtLoiCap, l.ThongDiep, rdoCap1); break;
                    case "SoKm" when km.HasValue: BaoLoi(txtLoiSoKm, l.ThongDiep, txtSoKm); break;
                    case "NguoiThucHien": BaoLoi(txtLoiNguoi, l.ThongDiep, cboNguoi); break;
                    case "ChiPhi" when chiPhi.HasValue: BaoLoi(txtLoiChiPhi, l.ThongDiep, txtChiPhi); break;
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
