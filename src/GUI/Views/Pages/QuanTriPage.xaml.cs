using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BUS.Services;
using DTO.Common;
using DTO.QuanTri;
using GUI.ViewModels.KyThuat;
using GUI.Views.Dialogs;

namespace GUI.Views.Pages
{
    // =========================================================================
    // TRANG QUAN TRI HE THONG (Sprint 3, Dat) - can quyen SYS_CONFIG (tab chi hien khi co quyen).
    //   Tab 1 Tai khoan       : them / sua / gan vai tro / vo hieu hoa / dat lai mat khau / mo khoa.
    //   Tab 2 Vai tro & quyen : ma tran vai tro x quyen (ADMIN luon du quyen, khong sua).
    //   Tab 3 Lich su dang nhap, Tab 4 Nhat ky he thong: chi doc, loc theo ngay / tai khoan / bang.
    // Moi luat + ghi CSDL o QuanTriService; trang chi hien thi va goi Service.
    // =========================================================================
    public partial class QuanTriPage : Page
    {
        private readonly QuanTriService _sv = new();
        private List<TaiKhoanHienThi> _taiKhoan = new();
        private List<VaiTroDTO> _vaiTro = new();
        private List<QuyenDTO> _quyen = new();
        private HashSet<(int MaVaiTro, int MaQuyen)> _maTran = new();
        private List<MucChon> _mucQuyen = new();
        private HashSet<int> _quyenGoc = new();
        private bool _dangNap;
        private bool _daNap;
        private bool _daNapLichSu, _daNapNhatKy;
        private VaiTroHienThi? _vaiTroDangXem;

        public QuanTriPage()
        {
            _dangNap = true;
            InitializeComponent();
            dpLsTu.SelectedDate = DateTime.Today.AddDays(-6);
            dpLsDen.SelectedDate = DateTime.Today;
            dpNkTu.SelectedDate = DateTime.Today.AddDays(-6);
            dpNkDen.SelectedDate = DateTime.Today;
            _dangNap = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_daNap) return;
            _daNap = true;
            NapTatCa();
        }

        // =====================================================================
        // NAP DU LIEU
        // =====================================================================
        // giuThayDoiQuyen = true (mac dinh, nap lai tu dong sau khi luu tai khoan / dat lai MK / mo khoa): o dang tick
        // tren ma tran chua luu duoc giu lai. F5 / Nap lai hoi truoc roi goi voi false (KichHoatNapLai).
        private void NapTatCa(int? chonTaiKhoan = null, bool giuThayDoiQuyen = true)
        {
            int? vtDangSua = CoThayDoiQuyen() ? _vaiTroDangXem!.Goc.MaVaiTro : null;
            var oDangTick = vtDangSua.HasValue ? _mucQuyen.Where(m => m.DuocChon).Select(m => m.Ma).ToHashSet() : null;

            if (!_sv.LayDanhSachTaiKhoan(out var ds, out string loi) || !_sv.LayDanhMuc(out _vaiTro, out _quyen, out _maTran, out loi))
            {
                ThongBaoDialog.Loi(loi, "Không Đọc Được Dữ Liệu Quản Trị");
                return;
            }
            _taiKhoan = ds.Select(t => new TaiKhoanHienThi(t)).ToList();

            _dangNap = true;
            string? vtLoc = (cboVaiTroLoc.SelectedItem as ComboBoxItem)?.Tag as string;
            cboVaiTroLoc.Items.Clear();
            cboVaiTroLoc.Items.Add(new ComboBoxItem { Content = "Tất cả vai trò", Tag = "ALL" });
            foreach (var vt in _vaiTro) cboVaiTroLoc.Items.Add(new ComboBoxItem { Content = vt.TenVaiTro, Tag = vt.MaVaiTroCode });
            cboVaiTroLoc.SelectedItem = cboVaiTroLoc.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == vtLoc) ?? cboVaiTroLoc.Items[0];

            DatComboTaiKhoan(cboLsTaiKhoan, "Tất cả tài khoản");
            DatComboTaiKhoan(cboNkNguoi, "Tất cả người thao tác");
            _dangNap = false;

            txtBadgeTaiKhoan.Text = _taiKhoan.Count.ToString();
            txtDemHoatDong.Text = _taiKhoan.Count(t => t.MaTrangThai == "HOAT_DONG").ToString();
            txtDemBiKhoa.Text = _taiKhoan.Count(t => t.MaTrangThai == "BI_KHOA").ToString();
            txtDemSaiHomNay.Text = _sv.LayLichSuDangNhap(new BoLocNhatKy { TuNgay = DateTime.Today, DenNgay = DateTime.Today, ThanhCong = false, SoDongToiDa = 5000 },
                                                         out var sai, out _) ? sai.Count.ToString() : "?";

            ApDungLocTaiKhoan(chonTaiKhoan);
            NapDanhSachVaiTro(_vaiTroDangXem?.Goc.MaVaiTro);
            if (giuThayDoiQuyen && vtDangSua.HasValue && oDangTick != null && _vaiTroDangXem?.Goc.MaVaiTro == vtDangSua)
            {
                foreach (var m in _mucQuyen.Where(m => m.ChoSua)) m.DuocChon = oDangTick.Contains(m.Ma);
                CapNhatNutQuyen();
            }
            if (_daNapLichSu) NapLichSu();
            if (_daNapNhatKy) NapNhatKy(giuBoLoc: true);
        }

        private void DatComboTaiKhoan(ComboBox cbo, string tatCa)
        {
            object? cu = (cbo.SelectedItem as ComboBoxItem)?.Tag;
            cbo.Items.Clear();
            cbo.Items.Add(new ComboBoxItem { Content = tatCa, Tag = 0 });
            foreach (var t in _taiKhoan.OrderBy(t => t.TenDangNhap))
                cbo.Items.Add(new ComboBoxItem { Content = $"{t.TenDangNhap} · {t.HoTenHienThi}", Tag = t.MaTaiKhoan });
            cbo.SelectedItem = cbo.Items.Cast<ComboBoxItem>().FirstOrDefault(i => Equals(i.Tag, cu)) ?? cbo.Items[0];
        }

        // =====================================================================
        // TAB 1: TAI KHOAN
        // =====================================================================
        private void ApDungLocTaiKhoan(int? chon = null)
        {
            int? giu = chon ?? (dgTaiKhoan.SelectedItem as TaiKhoanHienThi)?.MaTaiKhoan;
            string vt = (cboVaiTroLoc.SelectedItem as ComboBoxItem)?.Tag as string ?? "ALL";
            string tt = (cboTrangThaiLoc.SelectedItem as ComboBoxItem)?.Tag as string ?? "ALL";
            var loc = _taiKhoan.Where(t => t.KhopTuKhoa(txtTimTaiKhoan.Text)
                                        && (vt == "ALL" || t.Goc.CacVaiTro.Any(v => v.MaVaiTroCode == vt))
                                        && (tt == "ALL" || t.MaTrangThai == tt)).ToList();
            dgTaiKhoan.ItemsSource = loc;
            dgTaiKhoan.SelectedItem = loc.FirstOrDefault(t => t.MaTaiKhoan == giu);
            if (dgTaiKhoan.SelectedItem == null) HienChiTiet(null);
        }

        private void LocTaiKhoan_Changed(object sender, RoutedEventArgs e)
        {
            if (_dangNap || !IsLoaded) return;
            ApDungLocTaiKhoan();
        }

        private void DgTaiKhoan_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => HienChiTiet(dgTaiKhoan.SelectedItem as TaiKhoanHienThi);

        private void HienChiTiet(TaiKhoanHienThi? tk)
        {
            btnSuaTaiKhoan.IsEnabled = btnDatLaiMatKhau.IsEnabled = tk != null;
            btnMoKhoa.IsEnabled = tk != null && (tk.Goc.DangBiKhoa || tk.Goc.SoLanSaiLienTiep > 0);
            if (tk == null)
            {
                txtCtTen.Text = "Chọn một tài khoản";
                txtCtPhu.Text = "Nhấp đúp vào dòng để sửa.";
                pnlChiTiet.Visibility = Visibility.Collapsed;
                return;
            }
            txtCtTen.Text = tk.HoTenHienThi;
            txtCtPhu.Text = $"Tên đăng nhập: {tk.TenDangNhap}";
            txtCtTrangThai.Text = tk.NhanTrangThai;
            txtCtTrangThai.Foreground = tk.MauTrangThai;
            txtCtThongTin.Text =
                $"Vai trò: {tk.NhanVaiTro}\n" +
                $"Hồ sơ nhân viên: {tk.NhanHoSo}\n" +
                $"Email: {tk.Email ?? "—"}\n" +
                $"Đăng nhập gần nhất: {tk.NhanDangNhapCuoi}\n" +
                $"Ngày tạo: {tk.NhanNgayTao}";
            var vtKichHoat = tk.Goc.CacVaiTro.Where(v => v.KichHoat).ToList();
            var maVt = vtKichHoat.Select(v => v.MaVaiTro).ToHashSet();
            // Cung luat voi luc dang nhap (PhanQuyenService.QuyenHieuLuc): SYS_CONFIG chi tinh khi co vai tro ADMIN
            var hieuLuc = PhanQuyenService.QuyenHieuLuc(
                _quyen.Where(q => _maTran.Any(x => maVt.Contains(x.MaVaiTro) && x.MaQuyen == q.MaQuyen)).Select(q => q.MaQuyenCode),
                vtKichHoat.Select(v => v.MaVaiTroCode)).ToHashSet();
            icCtQuyen.ItemsSource = _quyen.Where(q => hieuLuc.Contains(q.MaQuyenCode))
                                          .OrderBy(q => TenNhomQuyen.ThuTu(q.NhomQuyen)).Select(q => q.TenQuyen).ToList();
            pnlChiTiet.Visibility = Visibility.Visible;
        }

        private void DongTaiKhoan_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SuaTaiKhoan();
        private void BtnThemTaiKhoan_Click(object sender, RoutedEventArgs e) => ThemTaiKhoan();
        private void BtnSuaTaiKhoan_Click(object sender, RoutedEventArgs e) => SuaTaiKhoan();

        private void ThemTaiKhoan()
        {
            if (!LayNhanVien(out var nv)) return;
            int maMoi = 0;
            var dlg = new TaiKhoanDialog(null, _vaiTro, nv, tk => _sv.ThemTaiKhoan(tk, out maMoi, out string loi) ? null : loi)
            { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            NapTatCa(maMoi);
            ThongBaoDialog.ThanhCong($"Đã tạo tài khoản \"{dlg.KetQua?.TenDangNhap}\".\nNgười dùng phải đổi mật khẩu ở lần đăng nhập đầu tiên.", "Thêm Tài Khoản");
        }

        private void SuaTaiKhoan()
        {
            if (dgTaiKhoan.SelectedItem is not TaiKhoanHienThi tk) return;
            if (!LayNhanVien(out var nv)) return;
            var dlg = new TaiKhoanDialog(tk.Goc, _vaiTro, nv, moi => _sv.CapNhatTaiKhoan(moi, out string loi) ? null : loi)
            { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            NapTatCa(tk.MaTaiKhoan);
        }

        private bool LayNhanVien(out List<(int Ma, string MaNV, string HoTen, string ChucDanh, int SoTaiKhoan)> nv)
        {
            if (_sv.LayNhanVienDeNoi(out nv, out string loi)) return true;
            ThongBaoDialog.Loi(loi);
            return false;
        }

        private void BtnDatLaiMatKhau_Click(object sender, RoutedEventArgs e)
        {
            if (dgTaiKhoan.SelectedItem is not TaiKhoanHienThi tk) return;
            var dlg = new DatLaiMatKhauDialog(tk.TenDangNhap, tk.HoTenHienThi,
                (mk, nhapLai) => _sv.DatLaiMatKhau(tk.MaTaiKhoan, mk, nhapLai, out string loi) ? null : loi)
            { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            NapTatCa(tk.MaTaiKhoan);
            ThongBaoDialog.ThanhCong($"Đã đặt lại mật khẩu cho \"{tk.TenDangNhap}\" (đồng thời mở khóa nếu đang bị khóa).\n" +
                                     "Người dùng phải đổi mật khẩu ở lần đăng nhập kế tiếp.", "Đặt Lại Mật Khẩu");
        }

        private void BtnMoKhoa_Click(object sender, RoutedEventArgs e)
        {
            if (dgTaiKhoan.SelectedItem is not TaiKhoanHienThi tk) return;
            if (!ThongBaoDialog.XacNhan($"Mở khóa tài khoản \"{tk.TenDangNhap}\" và xóa số lần đăng nhập sai?", "Mở Khóa Tài Khoản", "Mở khóa"))
                return;
            if (!_sv.MoKhoa(tk.MaTaiKhoan, out string loi)) { ThongBaoDialog.Loi(loi); return; }
            NapTatCa(tk.MaTaiKhoan);
        }

        private void BtnNapLai_Click(object sender, RoutedEventArgs e) => KichHoatNapLai();

        // =====================================================================
        // TAB 2: VAI TRO & QUYEN
        // =====================================================================
        private void NapDanhSachVaiTro(int? chon)
        {
            var soTk = _taiKhoan.SelectMany(t => t.Goc.CacVaiTro.Select(v => v.MaVaiTro)).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
            var ds = _vaiTro.Select(v => new VaiTroHienThi(v, soTk.GetValueOrDefault(v.MaVaiTro), _maTran.Count(x => x.MaVaiTro == v.MaVaiTro))).ToList();
            _dangNap = true;
            lstVaiTro.ItemsSource = ds;
            lstVaiTro.SelectedItem = ds.FirstOrDefault(v => v.Goc.MaVaiTro == chon) ?? ds.FirstOrDefault();
            _dangNap = false;
            HienQuyenCuaVaiTro(lstVaiTro.SelectedItem as VaiTroHienThi);
        }

        private void LstVaiTro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNap) return;
            var moi = lstVaiTro.SelectedItem as VaiTroHienThi;
            if (CoThayDoiQuyen() && moi != _vaiTroDangXem)
            {
                if (!ThongBaoDialog.XacNhan($"Bỏ các thay đổi chưa lưu của vai trò \"{_vaiTroDangXem?.TenVaiTro}\"?", "Thay Đổi Chưa Lưu", "Bỏ thay đổi"))
                {
                    _dangNap = true;
                    lstVaiTro.SelectedItem = _vaiTroDangXem;
                    _dangNap = false;
                    return;
                }
            }
            HienQuyenCuaVaiTro(moi);
        }

        private void HienQuyenCuaVaiTro(VaiTroHienThi? vt)
        {
            _vaiTroDangXem = vt;
            if (vt == null)
            {
                icNhomQuyen.ItemsSource = null;
                txtVtTen.Text = "Chọn một vai trò";
                txtVtMoTa.Text = "";
                CapNhatNutQuyen();
                return;
            }
            bool laAdmin = vt.MaVaiTroCode == QuanTriService.VaiTroQuanTri;
            _quyenGoc = _maTran.Where(x => x.MaVaiTro == vt.Goc.MaVaiTro).Select(x => x.MaQuyen).ToHashSet();
            _mucQuyen = _quyen.Select(q =>
            {
                // SYS_CONFIG chi cua ADMIN (09/11): vai tro khac khong tick them duoc; lo co san thi van bo tick duoc
                bool laQuyenHeThong = q.MaQuyenCode == MaQuyen.CauHinhHeThong;
                bool dangCo = _quyenGoc.Contains(q.MaQuyen);
                return new MucChon
                {
                    Ma = q.MaQuyen, Ma2 = q.MaQuyenCode, Ten = q.TenQuyen, Nhom = q.NhomQuyen, TenNhom = TenNhomQuyen.Ten(q.NhomQuyen),
                    MoTa = laQuyenHeThong && !laAdmin
                        ? "Chỉ vai trò Quản trị viên (ADMIN) có quyền này. Muốn ai quản trị hệ thống thì gán vai trò Quản trị viên cho tài khoản đó."
                        : q.MoTa,
                    ChoSua = !laAdmin && (!laQuyenHeThong || dangCo),
                    DuocChon = laAdmin || dangCo
                };
            }).ToList();
            icNhomQuyen.ItemsSource = _mucQuyen.OrderBy(m => TenNhomQuyen.ThuTu(m.Nhom)).GroupBy(m => m.TenNhom).ToList();
            txtVtTen.Text = $"{vt.TenVaiTro} ({vt.MaVaiTroCode})";
            txtVtMoTa.Text = (vt.Goc.MoTa ?? "") + (laAdmin ? "\nVai trò Quản trị viên luôn có đủ mọi quyền — không sửa được." :
                             $"\n{vt.SoTaiKhoan} tài khoản đang giữ vai trò này. Quyền mới áp dụng từ lần đăng nhập sau của họ.");
            CapNhatNutQuyen();
        }

        private bool CoThayDoiQuyen()
            => _vaiTroDangXem != null && _vaiTroDangXem.MaVaiTroCode != QuanTriService.VaiTroQuanTri
               && !_mucQuyen.Where(m => m.DuocChon).Select(m => m.Ma).ToHashSet().SetEquals(_quyenGoc);

        private void QuyenThayDoi(object sender, RoutedEventArgs e) => CapNhatNutQuyen();

        private void CapNhatNutQuyen()
        {
            bool doi = CoThayDoiQuyen();
            btnLuuQuyen.IsEnabled = btnHoanTacQuyen.IsEnabled = doi;
        }

        private void BtnHoanTacQuyen_Click(object sender, RoutedEventArgs e) => HienQuyenCuaVaiTro(_vaiTroDangXem);

        private void BtnLuuQuyen_Click(object sender, RoutedEventArgs e)
        {
            if (_vaiTroDangXem == null) return;
            var chon = _mucQuyen.Where(m => m.DuocChon).Select(m => m.Ma).ToList();
            if (chon.Count == 0 && !ThongBaoDialog.XacNhan($"Vai trò \"{_vaiTroDangXem.TenVaiTro}\" sẽ không còn quyền nào — người giữ vai trò này không đăng nhập được. Tiếp tục?",
                                                          "Bỏ Hết Quyền", "Lưu", "Hủy bỏ", true))
                return;
            int ma = _vaiTroDangXem.Goc.MaVaiTro;
            if (!_sv.LuuQuyenCuaVaiTro(ma, chon, out string loi)) { ThongBaoDialog.Loi(loi); return; }
            _vaiTroDangXem = null;
            NapTatCa();
            NapDanhSachVaiTro(ma);
            ThongBaoDialog.ThanhCong("Đã lưu quyền của vai trò. Người dùng đang đăng nhập nhận quyền mới ở lần đăng nhập sau.", "Lưu Quyền");
        }

        // =====================================================================
        // TAB 3: LICH SU DANG NHAP
        // =====================================================================
        private void NapLichSu()
        {
            var loc = new BoLocNhatKy
            {
                TuNgay = dpLsTu.SelectedDate ?? DateTime.Today,
                DenNgay = dpLsDen.SelectedDate ?? DateTime.Today,
                MaTaiKhoan = (cboLsTaiKhoan.SelectedItem as ComboBoxItem)?.Tag is int ma && ma > 0 ? ma : null,
                ThanhCong = ((cboLsKetQua.SelectedItem as ComboBoxItem)?.Tag as string) switch { "OK" => true, "LOI" => false, _ => null },
                TuKhoa = txtLsTim.Text
            };
            if (!_sv.LayLichSuDangNhap(loc, out var ds, out string loi)) { txtLsDem.Text = loi; dgLichSu.ItemsSource = null; return; }
            dgLichSu.ItemsSource = ds.Select(x => new LichSuDangNhapHienThi(x)).ToList();
            txtLsDem.Text = $"{ds.Count} lần · {ds.Count(x => !x.ThanhCong)} thất bại" + (ds.Count >= loc.SoDongToiDa ? $" (hiện {loc.SoDongToiDa} dòng mới nhất)" : "");
            _daNapLichSu = true;
        }

        private void LocLichSu_Changed(object sender, RoutedEventArgs e)
        {
            if (_dangNap || !IsLoaded || !_daNapLichSu) return;
            NapLichSu();
        }

        private void TxtLsTim_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) NapLichSu();
        }

        // =====================================================================
        // TAB 4: NHAT KY HE THONG
        // =====================================================================
        private void NapNhatKy(bool giuBoLoc = false)
        {
            if (!giuBoLoc || cboNkBang.Items.Count == 0)
            {
                if (_sv.LayGiaTriLocNhatKy(out var bang, out var hd, out _))
                {
                    _dangNap = true;
                    DatComboChuoi(cboNkBang, "Tất cả bảng", bang, s => s);
                    DatComboChuoi(cboNkHanhDong, "Tất cả hành động", hd, NhatKyHienThi.TenHanhDong);
                    _dangNap = false;
                }
            }
            var loc = new BoLocNhatKy
            {
                TuNgay = dpNkTu.SelectedDate ?? DateTime.Today,
                DenNgay = dpNkDen.SelectedDate ?? DateTime.Today,
                MaTaiKhoan = (cboNkNguoi.SelectedItem as ComboBoxItem)?.Tag is int ma && ma > 0 ? ma : null,
                TenBang = (cboNkBang.SelectedItem as ComboBoxItem)?.Tag as string,
                HanhDong = (cboNkHanhDong.SelectedItem as ComboBoxItem)?.Tag as string,
                TuKhoa = txtNkTim.Text
            };
            if (!_sv.LayNhatKyHeThong(loc, out var ds, out string loi)) { txtNkDem.Text = loi; dgNhatKy.ItemsSource = null; return; }
            dgNhatKy.ItemsSource = ds.Select(x => new NhatKyHienThi(x)).ToList();
            txtNkDem.Text = $"{ds.Count} thao tác" + (ds.Count >= loc.SoDongToiDa ? $" (hiện {loc.SoDongToiDa} dòng mới nhất)" : "");
            _daNapNhatKy = true;
            HienChiTietNhatKy(null);
        }

        private static void DatComboChuoi(ComboBox cbo, string tatCa, List<string> giaTri, Func<string, string> nhan)
        {
            string? cu = (cbo.SelectedItem as ComboBoxItem)?.Tag as string;
            cbo.Items.Clear();
            cbo.Items.Add(new ComboBoxItem { Content = tatCa, Tag = null });
            foreach (var g in giaTri) cbo.Items.Add(new ComboBoxItem { Content = nhan(g), Tag = g });
            cbo.SelectedItem = cbo.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (i.Tag as string) == cu) ?? cbo.Items[0];
        }

        private void LocNhatKy_Changed(object sender, RoutedEventArgs e)
        {
            if (_dangNap || !IsLoaded || !_daNapNhatKy) return;
            NapNhatKy(giuBoLoc: true);
        }

        private void TxtNkTim_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) NapNhatKy(giuBoLoc: true);
        }

        private void DgNhatKy_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => HienChiTietNhatKy(dgNhatKy.SelectedItem as NhatKyHienThi);

        private void HienChiTietNhatKy(NhatKyHienThi? nk)
        {
            if (nk == null)
            {
                txtNkTieuDe.Text = "Chọn một dòng nhật ký";
                txtNkPhu.Text = "Trường có nền vàng là trường đã thay đổi.";
                dgNkTruong.ItemsSource = null;
                return;
            }
            txtNkTieuDe.Text = $"{nk.NhanHanhDong} · {nk.TenBang} #{nk.KhoaChinh}";
            txtNkPhu.Text = $"{nk.NhanThoiDiem} · {nk.NguoiThucHien}" + (nk.HoTen.Length > 0 ? $" ({nk.HoTen})" : "") +
                            (string.IsNullOrEmpty(nk.Goc.DiaChiIP) ? "" : $" · máy {nk.Goc.DiaChiIP}");
            dgNkTruong.ItemsSource = nk.CacTruong();
        }

        // =====================================================================
        // CHUYEN TAB + PHIM TAT (MainWindow goi)
        // =====================================================================
        private void TabQuanTri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != tabQuanTri || !IsLoaded) return;
            if (tabQuanTri.SelectedIndex == 2 && !_daNapLichSu) NapLichSu();
            if (tabQuanTri.SelectedIndex == 3 && !_daNapNhatKy) NapNhatKy();
        }

        public void KichHoatThemMoi()
        {
            tabQuanTri.SelectedIndex = 0;
            ThemTaiKhoan();
        }

        public void KichHoatNapLai()
        {
            if (CoThayDoiQuyen() && !ThongBaoDialog.XacNhan(
                    $"Vai trò \"{_vaiTroDangXem?.TenVaiTro}\" có thay đổi quyền chưa lưu.\nNạp lại sẽ bỏ các thay đổi này. Tiếp tục?",
                    "Thay Đổi Chưa Lưu", "Bỏ thay đổi & nạp lại", "Hủy bỏ"))
                return;
            NapTatCa((dgTaiKhoan.SelectedItem as TaiKhoanHienThi)?.MaTaiKhoan, giuThayDoiQuyen: false);
            if (tabQuanTri.SelectedIndex == 2) NapLichSu();
            if (tabQuanTri.SelectedIndex == 3) NapNhatKy();
        }

        public void FocusTimKiem()
        {
            switch (tabQuanTri.SelectedIndex)
            {
                case 2: txtLsTim.Focus(); break;
                case 3: txtNkTim.Focus(); break;
                default: tabQuanTri.SelectedIndex = 0; txtTimTaiKhoan.Focus(); break;
            }
        }

        public void KichHoatHuy()
        {
            if (tabQuanTri.SelectedIndex == 0)
            {
                _dangNap = true;
                txtTimTaiKhoan.Text = "";
                cboVaiTroLoc.SelectedIndex = 0;
                cboTrangThaiLoc.SelectedIndex = 0;
                _dangNap = false;
                ApDungLocTaiKhoan();
            }
        }
    }
}
