using GUI.ViewModels.KyThuat;
using GUI.Helpers;
using GUI.Views.Dialogs;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GUI.Views.Pages
{
    // =========================================================================
    // PHAN HE NHAN SU, TO TAU & PHAN CONG CA TRUC (Nhom 6 SRS)
    //   Tab 1: Ho so nhan vien (nhansu.NhanVien)                - file nay
    //   Tab 2: Phan cong kip lai theo chang (PhanCongKipLai)    - NhanSuPage.PhanCong.cs
    //   Tab 3: Kiem tra len ban (KiemTraSucKhoe)                - NhanSuPage.KiemTra.cs
    //   Tab 4: Lich ca truc (tong hop tu PhanCongKipLai)        - NhanSuPage.LichCa.cs
    // Nghiep vu, quy tac R1..R13: PHAN_TICH_NHAN_SU_KIP_LAI.md.
    //
    // Giai doan dung giao dien: du lieu goc lay tu DanhMucMauNhanSu; them / sua
    // giu trong bo nho ("thay doi tam") va tron vao du lieu goc moi lan nap lai.
    // =========================================================================
    public partial class NhanSuPage : Page
    {
        private bool _dangNapDuLieu;

        // --- Thay doi tam (chua ghi CSDL). Khoa am = ban ghi moi, khoa duong = ban ghi goc da sua ---
        private readonly Dictionary<int, NhanVienHienThi> _nhanVienTam = new();
        private readonly Dictionary<int, PhanCongKipHienThi> _phanCongTam = new();
        private readonly HashSet<int> _phanCongDaHuy = new();
        private readonly Dictionary<int, KiemTraLenBanHienThi> _kiemTraTam = new();
        private int _maTamKeTiep = -1;

        // Du lieu da tron + danh gia o lan nap gan nhat, 4 tab dung chung
        private DuLieuNhanSu? _duLieu;
        private List<TinhTrangChuyen> _tinhTrangChuyen = new();

        public NhanSuPage()
        {
            // Chan SelectionChanged ban ra tu ComboBoxItem IsSelected="True" luc dung cay giao dien
            _dangNapDuLieu = true;
            InitializeComponent();
            _dangNapDuLieu = false;

            Loaded += NhanSuPage_Loaded;
        }

        private void NhanSuPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= NhanSuPage_Loaded;
            _duLieu = TaoDuLieu();
            NapDanhSachDonVi();
            LamMoiTatCa();
        }

        private bool ChuaSanSang => _dangNapDuLieu || _duLieu == null;

        private DuLieuNhanSu DuLieu => _duLieu!;

        // =====================================================================
        // DU LIEU CHUNG
        // =====================================================================

        // Du lieu goc (thay cho doc CSDL) + ban sua tam + ban them moi
        private DuLieuNhanSu TaoDuLieu()
        {
            var nhanVien = Tron(DanhMucMauNhanSu.LayNhanVien(), _nhanVienTam, x => x.MaNhanVien);
            var phanCong = Tron(DanhMucMauNhanSu.LayPhanCong(), _phanCongTam, x => x.MaPhanCong)
                           .Where(p => !_phanCongDaHuy.Contains(p.MaPhanCong))
                           .ToList();
            var kiemTra = Tron(DanhMucMauNhanSu.LayKiemTra(), _kiemTraTam, x => x.MaKiemTra);

            var dl = new DuLieuNhanSu(nhanVien, phanCong, kiemTra, DateTime.Now);
            foreach (var kt in kiemTra) kt.GanNhanVien(dl.TimNhanVien(kt.MaNhanVien));
            return dl;
        }

        private static List<T> Tron<T>(List<T> goc, Dictionary<int, T> tam, Func<T, int> khoa)
        {
            var ds = new List<T>();
            var daCo = new HashSet<int>();
            foreach (var x in goc)
            {
                int k = khoa(x);
                daCo.Add(k);
                ds.Add(tam.TryGetValue(k, out var banTam) ? banTam : x);
            }
            ds.AddRange(tam.Values.Where(x => !daCo.Contains(khoa(x))));
            return ds;
        }

        // Nap lai toan bo va danh gia lai (gio "bay gio" doi theo dong ho)
        private void LamMoiTatCa(int? maNhanVienChon = null, int? maChuyenChon = null, int? maPhanCongChon = null)
        {
            _duLieu = TaoDuLieu();
            _tinhTrangChuyen = DuLieu.ChuyenTau.Select(c => DanhGiaKipLai.DanhGiaChuyen(DuLieu, c)).ToList();

            LamMoiHoSo(giuDongChon: true, maNhanVienChon);
            LamMoiPhanCong(giuDongChon: true, maChuyenChon, maPhanCongChon);
            LamMoiKiemTra(giuDongChon: true, maPhanCongChon);
            VeLichCa();
            CapNhatSoDem();
            CapNhatNhanThayDoiTam();
        }

        private void NapDanhSachDonVi()
        {
            var tatCa = DuLieu.NhanVien.Select(n => n.DonViChuQuan)
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Distinct().OrderBy(s => s).ToList();
            var toTau = DuLieu.NhanVien.Where(n => n.LaToTau).Select(n => n.DonViChuQuan)
                              .Distinct().OrderBy(s => s).ToList();

            _dangNapDuLieu = true;
            DoDonVi(cboDonViLoc, tatCa);
            DoDonVi(cboDonViLich, toTau);
            _dangNapDuLieu = false;
        }

        private static void DoDonVi(ComboBox cbo, List<string> ds)
        {
            string? dangChon = LayTag(cbo);
            cbo.Items.Clear();
            cbo.Items.Add(new ComboBoxItem { Content = "Tất cả đơn vị", Tag = "ALL" });
            foreach (string d in ds) cbo.Items.Add(new ComboBoxItem { Content = d, Tag = d });
            cbo.SelectedItem = cbo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == dangChon) ?? cbo.Items[0];
        }

        private void TabNhanSu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // SelectionChanged cua ComboBox / DataGrid ben trong cung noi bot len day
            if (!ReferenceEquals(e.OriginalSource, tabNhanSu) || ChuaSanSang) return;

            // Canvas chi co kich thuoc that khi tab hien ra
            if (tabNhanSu.SelectedIndex == 1) VeHanhTrinh();
            if (tabNhanSu.SelectedIndex == 3) VeLichCa();
        }

        // =====================================================================
        // DAI TIEU DE: VIEC CAN XU LY
        // =====================================================================

        private void CapNhatSoDem()
        {
            var conHieuLuc = _tinhTrangChuyen.Where(t => t.ConHieuLuc).ToList();
            int thieuKip = conHieuLuc.Count(t => t.ThieuKip);
            int kipLoi = conHieuLuc.Sum(t => t.SoKipCoLoi);
            int choKiemTra = DemNguoiChoKiemTra();
            int hanKham = DuLieu.NhanVien.Count(n => n.TrangThai != NhanVienHienThi.DaNghiViec
                                                     && (n.HetHanKham || n.SapHetHanKham));

            DatSoDem(txtDemThieuKip, thieuKip, "#B91C1C");
            DatSoDem(txtDemKipLoi, kipLoi, "#B91C1C");
            DatSoDem(txtDemChoKiemTra, choKiemTra, "#B45309");
            DatSoDem(txtDemHanKham, hanKham, "#B45309");
        }

        private int DemNguoiChoKiemTra()
            => _tinhTrangChuyen.Where(t => t.ConHieuLuc)
                               .SelectMany(t => t.Kip).Where(k => k.LaDaPhanCong)
                               .SelectMany(k => k.ThanhVien)
                               .Count(tv => tv.NhanVien != null && tv.KiemTra.ChuaKiemTra);

        private static void DatSoDem(TextBlock tb, int so, string mau)
        {
            tb.Text = so.ToString("N0");
            tb.Foreground = Mau(so > 0 ? mau : "#334155");
        }

        private void Dem_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as FrameworkElement)?.Tag?.ToString())
            {
                case "THIEU_KIP":
                    tabNhanSu.SelectedIndex = 1;
                    ChonTheoTag(cboLocChuyen, "THIEU");
                    LamMoiPhanCong(giuDongChon: false);
                    break;
                case "KIP_LOI":
                    tabNhanSu.SelectedIndex = 1;
                    ChonTheoTag(cboLocChuyen, "LOI");
                    LamMoiPhanCong(giuDongChon: false);
                    break;
                case "CHO_KIEM_TRA":
                    tabNhanSu.SelectedIndex = 2;
                    ChonHangChoCanKiemTra();
                    break;
                case "HAN_KHAM":
                    tabNhanSu.SelectedIndex = 0;
                    _dangNapDuLieu = true;
                    txtTimNhanVien.Text = string.Empty;
                    cboChucDanhLoc.SelectedIndex = 0;
                    cboDonViLoc.SelectedIndex = 0;
                    cboTrangThaiLoc.SelectedIndex = 0;
                    chkHanKhamLoc.IsChecked = true;
                    _dangNapDuLieu = false;
                    LamMoiHoSo(giuDongChon: false);
                    break;
            }
        }

        // =====================================================================
        // TAB 1: HO SO NHAN VIEN
        // =====================================================================

        private void LamMoiHoSo(bool giuDongChon, int? maChonSau = null)
        {
            int? maDangChon = maChonSau ?? (giuDongChon ? (dgNhanVien.SelectedItem as NhanVienHienThi)?.MaNhanVien : null);

            string tuKhoa = BoDau(txtTimNhanVien.Text?.Trim() ?? string.Empty);
            string chucDanh = LayTag(cboChucDanhLoc) ?? "ALL";
            string donVi = LayTag(cboDonViLoc) ?? "ALL";
            string trangThai = LayTag(cboTrangThaiLoc) ?? "DANG_CONG_TAC";
            bool hanKham = chkHanKhamLoc.IsChecked == true;

            var ds = DuLieu.NhanVien
                .Where(nv => KhopBoLocNhanVien(nv, tuKhoa, chucDanh, donVi, trangThai, hanKham))
                .OrderBy(nv => nv.MaNVCode)
                .ToList();

            dgNhanVien.ItemsSource = ds;
            txtBadgeNhanVien.Text = ds.Count.ToString();
            txtTrongNhanVien.Visibility = ds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (!(maDangChon.HasValue && ChonNhanVien(maDangChon.Value)) && ds.Count > 0)
                dgNhanVien.SelectedIndex = 0;
            if (ds.Count == 0) XoaTrangHoSo();
        }

        private static bool KhopBoLocNhanVien(NhanVienHienThi nv, string tuKhoa, string chucDanh, string donVi,
                                              string trangThai, bool hanKham)
        {
            if (tuKhoa.Length > 0 &&
                !BoDau(nv.MaNVCode).Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !BoDau(nv.HoTen).Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !nv.SoDienThoai.Contains(tuKhoa) &&
                !BoDau(nv.DonViChuQuan).Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
                return false;

            bool laChucDanhChinh = nv.ChucDanh is "Lái tàu" or "Phụ lái" or "Trưởng tàu" or "Tiếp viên";
            if (chucDanh == "KHAC" && laChucDanhChinh) return false;
            if (chucDanh is not ("ALL" or "KHAC") && nv.ChucDanh != chucDanh) return false;

            if (donVi != "ALL" && nv.DonViChuQuan != donVi) return false;

            if (trangThai == "DANG_CONG_TAC" && nv.TrangThai == NhanVienHienThi.DaNghiViec) return false;
            if (trangThai is not ("ALL" or "DANG_CONG_TAC") && nv.TrangThai != trangThai) return false;

            if (hanKham && !(nv.HetHanKham || nv.SapHetHanKham)) return false;
            return true;
        }

        private bool ChonNhanVien(int maNhanVien)
        {
            if (dgNhanVien.ItemsSource is not IEnumerable<NhanVienHienThi> ds) return false;
            var muc = ds.FirstOrDefault(n => n.MaNhanVien == maNhanVien);
            if (muc == null) return false;

            dgNhanVien.SelectedItem = muc;
            dgNhanVien.ScrollIntoView(muc);
            return true;
        }

        private void BoLocNhanVien_Changed(object sender, RoutedEventArgs e)
        {
            if (ChuaSanSang) return;
            LamMoiHoSo(giuDongChon: true);
        }

        private void BtnXoaLocNhanVien_Click(object sender, RoutedEventArgs e)
        {
            _dangNapDuLieu = true;
            txtTimNhanVien.Text = string.Empty;
            cboChucDanhLoc.SelectedIndex = 0;
            cboDonViLoc.SelectedIndex = 0;
            cboTrangThaiLoc.SelectedIndex = 0;
            chkHanKhamLoc.IsChecked = false;
            _dangNapDuLieu = false;
            LamMoiHoSo(giuDongChon: true);
        }

        private void DgNhanVien_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var nv = dgNhanVien.SelectedItem as NhanVienHienThi;
            btnSuaNhanVien.IsEnabled = nv != null;
            btnSuaHoSo.IsEnabled = nv != null;

            if (nv == null) XoaTrangHoSo();
            else HienThiHoSo(nv);
        }

        private void XoaTrangHoSo()
        {
            txtHsTen.Text = "HỒ SƠ NHÂN VIÊN";
            txtHsPhuDe.Text = "Chọn một nhân viên để xem hồ sơ";
            txtHsMa.Text = string.Empty;
            pnlHoSo.Visibility = Visibility.Collapsed;
            txtHsTrong.Visibility = Visibility.Visible;
        }

        private void HienThiHoSo(NhanVienHienThi nv)
        {
            pnlHoSo.Visibility = Visibility.Visible;
            txtHsTrong.Visibility = Visibility.Collapsed;

            txtHsTen.Text = nv.HoTen;
            txtHsPhuDe.Text = $"{nv.ChucDanh} · {nv.DonViChuQuan}";
            txtHsMa.Text = nv.LaThayDoiTam ? $"{nv.MaNVCode} · tạm" : nv.MaNVCode;

            // --- Canh bao ve dieu kien hanh nghe / kip dang dam nhan ---
            pnlHsCanhBao.Children.Clear();
            if (nv.TrangThai == NhanVienHienThi.DaNghiViec)
            {
                ThemCanhBaoHoSo(MucDoVanDe.LuuY, "Đã nghỉ việc. Hồ sơ được giữ lại, không xuất hiện khi phân công kíp.");
            }
            else
            {
                if (nv.HetHanKham)
                    ThemCanhBaoHoSo(MucDoVanDe.NghiemTrong,
                        $"Hết hạn khám sức khỏe từ {nv.HanKhamSucKhoe:dd/MM/yyyy}. Không được phân công cho tới khi khám lại.");
                else if (nv.SapHetHanKham)
                    ThemCanhBaoHoSo(MucDoVanDe.CanhBao,
                        $"Hạn khám sức khỏe {nv.HanKhamSucKhoe:dd/MM/yyyy} ({nv.MoTaHanKham.ToLower()}). Cần xếp lịch khám định kỳ.");

                var vanDeKip = _tinhTrangChuyen.SelectMany(t => t.Kip).SelectMany(k => k.VanDe)
                    .Where(v => v.MaNhanVien == nv.MaNhanVien && v.MucDo != MucDoVanDe.LuuY
                                && v.DieuKien != DanhGiaKipLai.DkHanKham)
                    .Take(3);
                foreach (var vd in vanDeKip)
                    ThemCanhBaoHoSo(vd.MucDo, $"{vd.DoiTuong}: {vd.NoiDung}");
            }

            // --- Thong tin chung ---
            pnlHsThongTin.Children.Clear();
            ThemDongThongSo(pnlHsThongTin, "Mã nhân viên", nv.MaNVCode);
            ThemDongThongSo(pnlHsThongTin, "Số điện thoại", nv.SoDienThoai);
            ThemDongThongSo(pnlHsThongTin, "Chức danh", nv.ChucDanh);
            ThemDongThongSo(pnlHsThongTin, "Đơn vị chủ quản", nv.DonViChuQuan);
            ThemDongThongSo(pnlHsThongTin, "Trạng thái", nv.NhanTrangThai, nv.MauTrangThai);
            ThemDongThongSo(pnlHsThongTin, "Ngày lập hồ sơ", nv.NgayTao.ToString("dd/MM/yyyy"), laDongCuoi: true);

            // --- Dieu kien hanh nghe ---
            pnlHsDieuKien.Children.Clear();
            ThemDongThongSo(pnlHsDieuKien, "Hạng bằng lái",
                            nv.LaBanLaiMay ? nv.NhanBangLai : "Không yêu cầu");
            ThemDongThongSo(pnlHsDieuKien, "Hạn khám sức khỏe",
                            $"{nv.HanKhamSucKhoe:dd/MM/yyyy} · {nv.MoTaHanKham.ToLower()}", nv.MauHanKham);

            if (nv.LaToTau)
            {
                var caGanNhat = DuLieu.KipCuaNhanVien(nv.MaNhanVien)
                    .Where(p => p.GioBanGiao <= DuLieu.BayGio)
                    .OrderByDescending(p => p.GioBanGiao)
                    .FirstOrDefault();
                ThemDongThongSo(pnlHsDieuKien, "Ca gần nhất", caGanNhat == null
                    ? "—"
                    : $"{caGanNhat.Chuyen.SoHieuMacTau} · giao {caGanNhat.GioBanGiao:HH:mm dd/MM} tại {caGanNhat.TenGaBanGiao}");

                if (caGanNhat != null)
                {
                    DateTime duocNhanLai = caGanNhat.GioBanGiao.AddHours((double)QuyTacKipLai.SoGioNghiToiThieu);
                    bool dangNghi = duocNhanLai > DuLieu.BayGio;
                    ThemDongThongSo(pnlHsDieuKien, "Nhận ban lại từ",
                                    dangNghi ? $"{duocNhanLai:HH:mm dd/MM} (đang nghỉ)" : "Đã đủ 8 giờ nghỉ",
                                    dangNghi ? "#B45309" : null);
                }
            }

            var kiemTra = DuLieu.KiemTra.Where(k => k.MaNhanVien == nv.MaNhanVien)
                                        .OrderByDescending(k => k.ThoiDiemKiemTra)
                                        .ToList();
            var moiNhat = kiemTra.FirstOrDefault();
            ThemDongThongSo(pnlHsDieuKien, "Kiểm tra lên ban gần nhất",
                            moiNhat == null ? "Chưa có" : $"{moiNhat.ThoiDiemKiemTra:HH:mm dd/MM} · {moiNhat.NhanKetLuan.ToLower()}",
                            moiNhat?.MauKetLuan, laDongCuoi: true);

            // --- Kip dam nhan ---
            var kip = DuLieu.KipCuaNhanVien(nv.MaNhanVien)
                .OrderByDescending(p => p.GioNhanBan)
                .Select(p => new KipCuaNhanVien { PhanCong = p, VaiTro = p.VaiTroCua(nv.MaNhanVien)! })
                .ToList();
            icHsKip.ItemsSource = kip;
            txtHsTieuDeKip.Text = $"KÍP ĐẢM NHẬN ({kip.Count})";
            txtHsKipTrong.Visibility = kip.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtHsKipTrong.Text = nv.LaToTau ? "Chưa được phân công kíp nào." : "Chức danh này không thuộc kíp lái.";

            // --- Kiem tra len ban ---
            icHsKiemTra.ItemsSource = kiemTra;
            txtHsTieuDeKiemTra.Text = $"KIỂM TRA LÊN BAN ({kiemTra.Count})";
            txtHsKiemTraTrong.Visibility = kiemTra.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ThemCanhBaoHoSo(MucDoVanDe muc, string noiDung)
        {
            string nen = muc switch
            {
                MucDoVanDe.NghiemTrong => "#FEF2F2",
                MucDoVanDe.CanhBao => "#FFFBEB",
                _ => "#F8FAFC"
            };

            pnlHsCanhBao.Children.Add(new Border
            {
                Background = Mau(nen),
                BorderBrush = Mau(MucDoHienThi.Mau(muc)),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, 5),
                Child = new TextBlock
                {
                    Text = noiDung,
                    FontSize = 11,
                    Foreground = Mau("#0F172A"),
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }

        // Mot dong "nhan ...... gia tri" trong panel ho so
        private void ThemDongThongSo(Panel pnl, string nhan, string giaTri, string? mauGiaTri = null, bool laDongCuoi = false)
        {
            var hang = new Grid { MinHeight = 26 };
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var tbNhan = new TextBlock { Text = nhan, Style = (Style)FindResource("KtSpecLabel"), Margin = new Thickness(0, 0, 10, 0) };
            var tbGiaTri = new TextBlock
            {
                Text = giaTri,
                Style = (Style)FindResource("KtSpecValue"),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.None,
                Margin = new Thickness(0, 4, 0, 4)
            };
            if (mauGiaTri != null) tbGiaTri.Foreground = Mau(mauGiaTri);
            Grid.SetColumn(tbGiaTri, 1);

            hang.Children.Add(tbNhan);
            hang.Children.Add(tbGiaTri);

            pnl.Children.Add(new Border
            {
                Child = hang,
                BorderBrush = Mau("#F1F5F9"),
                BorderThickness = new Thickness(0, 0, 0, laDongCuoi ? 0 : 1)
            });
        }

        private void HsKip_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not KipCuaNhanVien k) return;
            MoKip(k.PhanCong.MaChuyenTau, k.PhanCong.MaPhanCong);
        }

        // --- Them / sua ho so ---

        private void BtnThemNhanVien_Click(object sender, RoutedEventArgs e) => ThemNhanVien();

        private void BtnSuaNhanVien_Click(object sender, RoutedEventArgs e) => SuaNhanVien();

        private void DongNhanVien_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SuaNhanVien();

        private void ThemNhanVien()
        {
            tabNhanSu.SelectedIndex = 0;

            var dlg = new NhanVienDialog(null, DuLieu) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var nv = dlg.KetQua;
            nv.MaNhanVien = _maTamKeTiep--;
            nv.NgayTao = DateTime.Now;
            nv.LaThayDoiTam = true;
            _nhanVienTam[nv.MaNhanVien] = nv;

            SauKhiLuuNhanVien(nv);
            ThongBaoDialog.ThanhCong(
                $"Đã thêm hồ sơ {nv.MaNVCode} — {nv.HoTen} ({nv.ChucDanh}, {nv.DonViChuQuan}).\n\n" +
                "Hồ sơ đang hiển thị trên màn hình, chưa ghi vào CSDL.",
                "Thêm hồ sơ nhân viên");
        }

        private void SuaNhanVien()
        {
            if (dgNhanVien.SelectedItem is not NhanVienHienThi dangChon) return;

            var dlg = new NhanVienDialog(dangChon.SaoChep(), DuLieu) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var nv = dlg.KetQua;
            nv.LaThayDoiTam = true;
            _nhanVienTam[nv.MaNhanVien] = nv;
            SauKhiLuuNhanVien(nv);

            // Thay doi ho so lam nguoi nay khong con du dieu kien o kip chua nhan ban
            var kipAnhHuong = _tinhTrangChuyen.SelectMany(t => t.Kip)
                .Where(k => k.LaDaPhanCong && k.VanDe.Any(v => v.MaNhanVien == nv.MaNhanVien && v.MucDo == MucDoVanDe.NghiemTrong))
                .ToList();

            if (kipAnhHuong.Count > 0)
            {
                ThongBaoDialog.CanhBao(
                    $"Đã cập nhật hồ sơ {nv.MaNVCode} — {nv.HoTen}.\n\n" +
                    $"Với hồ sơ mới, {nv.HoTen} không đủ điều kiện ở {kipAnhHuong.Count} kíp chưa nhận ban:\n" +
                    string.Join("\n", kipAnhHuong.Select(k => $"• {k.NhanChuyen} · {k.TenKip} ({k.PhanCong.Chang})")) +
                    "\n\nVào tab Phân công kíp lái để thay người. Hồ sơ chưa ghi vào CSDL.",
                    "Cần thay người trong kíp");
            }
            else
            {
                ThongBaoDialog.ThanhCong(
                    $"Đã cập nhật hồ sơ {nv.MaNVCode} — {nv.HoTen}.\n\nThay đổi đang hiển thị trên màn hình, chưa ghi vào CSDL.",
                    "Cập nhật hồ sơ nhân viên");
            }
        }

        private void SauKhiLuuNhanVien(NhanVienHienThi nv)
        {
            LamMoiTatCa(maNhanVienChon: nv.MaNhanVien);
            NapDanhSachDonVi();

            // Bo loc hien tai an mat ho so vua luu thi xoa loc de nguoi dung thay ngay
            if ((dgNhanVien.SelectedItem as NhanVienHienThi)?.MaNhanVien != nv.MaNhanVien)
            {
                _dangNapDuLieu = true;
                txtTimNhanVien.Text = string.Empty;
                cboChucDanhLoc.SelectedIndex = 0;
                cboDonViLoc.SelectedIndex = 0;
                cboTrangThaiLoc.SelectedIndex = nv.TrangThai == NhanVienHienThi.DaNghiViec ? cboTrangThaiLoc.Items.Count - 1 : 0;
                chkHanKhamLoc.IsChecked = false;
                _dangNapDuLieu = false;
                LamMoiHoSo(giuDongChon: false, nv.MaNhanVien);
            }
        }

        // =====================================================================
        // THAY DOI TAM
        // =====================================================================

        private void CapNhatNhanThayDoiTam()
        {
            int soThayDoi = _nhanVienTam.Count + _phanCongTam.Count + _phanCongDaHuy.Count + _kiemTraTam.Count;
            bdThayDoiTam.Visibility = soThayDoi > 0 ? Visibility.Visible : Visibility.Collapsed;
            txtThayDoiTam.Text = $"{soThayDoi} thay đổi tạm · chưa ghi CSDL";
        }

        private void BtnHuyThayDoiTam_Click(object sender, RoutedEventArgs e)
        {
            bool dongY = ThongBaoDialog.XacNhan(
                "Bỏ toàn bộ hồ sơ, kíp, kết quả kiểm tra lên ban vừa lập / sửa / hủy?\n" +
                "Màn hình sẽ hiển thị lại đúng dữ liệu gốc.",
                "Hoàn tác thay đổi tạm", nutDongY: "Hoàn tác", nutHuy: "Giữ lại");
            if (!dongY) return;

            _nhanVienTam.Clear();
            _phanCongTam.Clear();
            _phanCongDaHuy.Clear();
            _kiemTraTam.Clear();
            _maTamKeTiep = -1;

            _duLieu = TaoDuLieu();
            NapDanhSachDonVi();
            LamMoiTatCa();
        }

        // =====================================================================
        // TIEN ICH
        // =====================================================================

        private static string? LayTag(ComboBox cbo) => (cbo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

        private void ChonTheoTag(ComboBox cbo, string tag)
        {
            _dangNapDuLieu = true;
            cbo.SelectedItem = cbo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag?.ToString() == tag) ?? cbo.SelectedItem;
            _dangNapDuLieu = false;
        }

        // Bo dau tieng Viet de tra cuu "cuong" tim ra "Cường"
        private static string BoDau(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length);
            foreach (char c in s.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                sb.Append(c switch { 'đ' => 'd', 'Đ' => 'D', _ => c });
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static SolidColorBrush Mau(string ma) => (SolidColorBrush)new BrushConverter().ConvertFrom(ma)!;

        // =====================================================================
        // CAC HAM MainWindow GOI QUA (dong bo voi cac Page khac)
        // =====================================================================

        public void FocusTimKiem()
        {
            switch (tabNhanSu.SelectedIndex)
            {
                case 1:
                    dgChuyen.Focus();
                    break;
                case 2:
                    txtTimKiemTra.Focus();
                    txtTimKiemTra.SelectAll();
                    break;
                case 3:
                    cboDonViLich.Focus();
                    break;
                default:
                    txtTimNhanVien.Focus();
                    txtTimNhanVien.SelectAll();
                    break;
            }
        }

        public void KichHoatThemMoi()
        {
            if (ChuaSanSang) return;
            switch (tabNhanSu.SelectedIndex)
            {
                case 0: ThemNhanVien(); break;
                case 1: ThemKip(); break;
                case 2: GhiKiemTraHangCho(); break;
                // Tab Lich ca truc chi de xem, lap kip o tab Phan cong
            }
        }

        public void KichHoatNapLai()
        {
            if (ChuaSanSang) return;
            LamMoiTatCa();
        }

        public void KichHoatHuy()
        {
            if (ChuaSanSang) return;
            switch (tabNhanSu.SelectedIndex)
            {
                case 0: BtnXoaLocNhanVien_Click(this, new RoutedEventArgs()); break;
                case 1:
                    ChonTheoTag(cboLocChuyen, "ACTIVE");
                    LamMoiPhanCong(giuDongChon: true);
                    break;
                case 2: BtnXoaLocKiemTra_Click(this, new RoutedEventArgs()); break;
                case 3:
                    _dangNapDuLieu = true;
                    cboDonViLich.SelectedIndex = 0;
                    cboChucDanhLich.SelectedIndex = 0;
                    chkChiCoCa.IsChecked = false;
                    _dangNapDuLieu = false;
                    VeLichCa();
                    break;
            }
        }
    }
}
