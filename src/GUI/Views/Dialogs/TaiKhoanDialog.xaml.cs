using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BUS.Services;
using DTO.QuanTri;
using GUI.ViewModels.KyThuat;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // Them / sua tai khoan (Sprint 3, Dat). Kiem tra nhap lieu dung chung QuanTriService.KiemTraTaiKhoan;
    // luu qua callback (Service) - loi thi giu dialog mo va hien loi.
    // Sua: khoa ten dang nhap, an o mat khau (dat lai mat khau la nut rieng tren trang).
    // =========================================================================
    public partial class TaiKhoanDialog : Window
    {
        private readonly TaiKhoanDTO? _goc;
        private readonly Func<TaiKhoanLuuDTO, string?> _luu;
        private readonly List<MucChon> _vaiTro;
        private bool _dangNap = true;

        public TaiKhoanLuuDTO? KetQua { get; private set; }

        public TaiKhoanDialog(TaiKhoanDTO? goc, List<VaiTroDTO> vaiTro,
                              List<(int Ma, string MaNV, string HoTen, string ChucDanh, int SoTaiKhoan)> nhanVien,
                              Func<TaiKhoanLuuDTO, string?> luu)
        {
            InitializeComponent();
            _goc = goc;
            _luu = luu;

            var dangCo = goc?.CacVaiTro.Select(v => v.MaVaiTro).ToHashSet() ?? new HashSet<int>();
            _vaiTro = vaiTro.Select(v => new MucChon
            {
                Ma = v.MaVaiTro, Ma2 = v.MaVaiTroCode, Ten = v.TenVaiTro + (v.KichHoat ? "" : " (đang tắt)"),
                MoTa = v.MoTa, DuocChon = dangCo.Contains(v.MaVaiTro)
            }).ToList();
            icVaiTro.ItemsSource = _vaiTro;

            cboNhanVien.Items.Add(new ComboBoxItem { Content = "(Không nối hồ sơ nhân viên)", Tag = null });
            foreach (var nv in nhanVien)
            {
                bool laCuaMinh = goc?.MaNhanVien == nv.Ma;
                string them = !laCuaMinh && nv.SoTaiKhoan > 0 ? $" · đã có {nv.SoTaiKhoan} tài khoản" : "";
                cboNhanVien.Items.Add(new ComboBoxItem { Content = $"{nv.MaNV} · {nv.HoTen} · {nv.ChucDanh}{them}", Tag = nv.Ma });
            }
            // Ho so cu da nghi viec (khong con trong danh sach) van giu de khong mat lien ket khi sua
            if (goc?.MaNhanVien is int maNv && nhanVien.All(n => n.Ma != maNv))
                cboNhanVien.Items.Add(new ComboBoxItem { Content = $"{goc.MaNVCode} · {goc.HoTenNhanVien} (đã nghỉ việc)", Tag = maNv });
            cboNhanVien.SelectedItem = cboNhanVien.Items.Cast<ComboBoxItem>().FirstOrDefault(i => Equals(i.Tag, goc?.MaNhanVien)) ?? cboNhanVien.Items[0];

            if (goc == null)
            {
                txtTenDangNhap.Focus();
            }
            else
            {
                txtTieuDe.Text = "SỬA TÀI KHOẢN";
                txtTieuDePhu.Text = $"{goc.TenDangNhap} · tạo {goc.NgayTao:dd/MM/yyyy}";
                txtTenDangNhap.Text = goc.TenDangNhap;
                txtTenDangNhap.IsReadOnly = true;
                txtTenDangNhap.Background = System.Windows.Media.Brushes.WhiteSmoke;
                txtGoiYTen.Text = "Tên đăng nhập không đổi được sau khi tạo.";
                txtHoTen.Text = goc.HoTenHienThi;
                txtEmail.Text = goc.Email ?? "";
                pnlMatKhau.Visibility = Visibility.Collapsed;
                chkHoatDong.Visibility = Visibility.Visible;
                chkHoatDong.IsChecked = goc.DangHoatDong;
                txtChanTrang.Text = "Đổi vai trò / vô hiệu hóa có hiệu lực từ lần đăng nhập sau của người dùng.";
                txtHoTen.Focus();
            }
            _dangNap = false;
        }

        private void CboNhanVien_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNap) return;
            // Chua co ten hien thi -> lay ho ten trong ho so
            if (string.IsNullOrWhiteSpace(txtHoTen.Text) && cboNhanVien.SelectedItem is ComboBoxItem { Tag: int } it)
            {
                string[] phan = (it.Content as string ?? "").Split(" · ");
                if (phan.Length >= 2) txtHoTen.Text = phan[1];
            }
            O_ThayDoi(sender, e);
        }

        private TaiKhoanLuuDTO DocForm() => new()
        {
            MaTaiKhoan = _goc?.MaTaiKhoan ?? 0,
            TenDangNhap = txtTenDangNhap.Text,
            HoTenHienThi = txtHoTen.Text,
            Email = txtEmail.Text,
            MaNhanVien = (cboNhanVien.SelectedItem as ComboBoxItem)?.Tag as int?,
            DangHoatDong = _goc == null || chkHoatDong.IsChecked == true,
            MatKhauMoi = _goc == null ? txtMatKhau.Password : null,
            CacMaVaiTro = _vaiTro.Where(v => v.DuocChon).Select(v => v.Ma).ToList(),
            PhienBan = _goc?.PhienBan
        };

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            var tk = DocForm();
            var loi = QuanTriService.KiemTraTaiKhoan(tk, _goc == null).Select(l => l.ThongDiep).ToList();
            if (_goc == null && txtMatKhau.Password != txtNhapLai.Password) loi.Add("Mật khẩu nhập lại không khớp.");
            if (loi.Count > 0)
            {
                txtLoi.Text = string.Join("\n", loi.Select(l => "• " + l));
                return;
            }

            if (_goc != null && _goc.DangHoatDong && !tk.DangHoatDong)
            {
                // [12/11] Luat "khong tu vo hieu hoa" kiem truoc khi hoi (truoc day hoi xong moi bi Service tu choi).
                if (_goc.MaTaiKhoan == PhanQuyenService.MaTaiKhoanHienTai)
                {
                    txtLoi.Text = "• Không thể tự vô hiệu hóa tài khoản đang đăng nhập.";
                    return;
                }
                // Vo hieu hoa khong xoa du lieu -> hop xac nhan thuong, khong dung tieu de "xoa du lieu khoi CSDL"
                if (!ThongBaoDialog.XacNhan($"Vô hiệu hóa tài khoản \"{_goc.TenDangNhap}\"? Người này sẽ không đăng nhập được nữa.\n" +
                                            "Lịch sử đăng nhập và nhật ký thao tác vẫn được giữ; bật lại được bất cứ lúc nào.",
                                            "Vô Hiệu Hóa Tài Khoản", "Vô hiệu hóa", "Hủy bỏ", false, this))
                    return;
            }

            string? kq = _luu(tk);
            if (kq != null)
            {
                txtLoi.Text = kq;
                return;
            }
            KetQua = tk;
            DialogResult = true;
        }

        private void O_ThayDoi(object sender, RoutedEventArgs e)
        {
            if (!_dangNap) txtLoi.Text = "";
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { e.Handled = true; DialogResult = false; }
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) { e.Handled = true; BtnLuu_Click(this, new RoutedEventArgs()); }
        }
    }
}
