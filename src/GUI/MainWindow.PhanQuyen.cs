using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BUS.Services;
using DTO.Common;
using GUI.Helpers;
using GUI.Views.Dialogs;

namespace GUI
{
    // =========================================================================
    // PHAN QUYEN TREN MAN HINH CHINH (Sprint 3, Dat - file rieng, partial cua MainWindow cua A Ba).
    //   - An tab / cum khong co quyen xem (Q10: Service van kiem lai truoc khi ghi).
    //   - Chan dieu huong toi tab khong co quyen (ca khi trang khac goi NavigateTo...).
    //   - Thanh trang thai hien nguoi dang nhap; nut "Dang xuat / Thoat" mo menu tai khoan.
    // Tab hien khi co quyen XEM cua phan he HOAC mot quyen GHI cua phan he (migration V20261108_1000).
    // =========================================================================
    public partial class MainWindow
    {
        public static readonly IReadOnlyDictionary<string, string[]> QuyenTheoTab = new Dictionary<string, string[]>
        {
            ["MangLuoiGa"] = new[] { MaQuyen.XemMangLuoi, MaQuyen.SuaHaTang, MaQuyen.DuyetLenhPhongToa },
            ["KhuGian"] = new[] { MaQuyen.XemMangLuoi, MaQuyen.SuaHaTang, MaQuyen.DuyetLenhPhongToa },
            ["MacTau"] = new[] { MaQuyen.XemMangLuoi, MaQuyen.SuaHaTang, MaQuyen.LapBieuDoChayTau },
            ["LichTrinh"] = new[] { MaQuyen.XemMangLuoi, MaQuyen.LapBieuDoChayTau, MaQuyen.GhiNhanSuCoChamGio },
            ["BieuDo"] = new[] { MaQuyen.XemMangLuoi, MaQuyen.LapBieuDoChayTau, MaQuyen.GhiNhanSuCoChamGio },
            ["DieuHanhThucTe"] = new[] { MaQuyen.XemMangLuoi, MaQuyen.LapBieuDoChayTau, MaQuyen.GhiNhanSuCoChamGio },
            ["BanVe"] = new[] { MaQuyen.XemVe, MaQuyen.BanVe, MaQuyen.HoanVe },
            ["SoatVe"] = new[] { MaQuyen.XemVe, MaQuyen.SoatVe },
            ["HangHoa"] = new[] { MaQuyen.XemHangHoa, MaQuyen.LapVanDonHang },
            ["NhanSu"] = new[] { MaQuyen.XemNhanSu, MaQuyen.QuanLyHoSoNhanVien, MaQuyen.PhanCongKipLai, MaQuyen.NhanBanKipLai },
            ["PhuongTien"] = new[] { MaQuyen.XemPhuongTien, MaQuyen.QuanLyPhuongTien, MaQuyen.LapDoanTau },
            ["BaoTri"] = new[] { MaQuyen.XemBaoTri, MaQuyen.GhiNhatKyBaoTri, MaQuyen.NghiemThuKhamXe },
            ["BaoCao"] = new[] { MaQuyen.XemBaoCao },
            ["QuanTri"] = new[] { MaQuyen.CauHinhHeThong },
        };

        // [12/11] Quyen GHI cua tung tab: khong co quyen nao -> F2 (them / lap moi) bi chan, an nut F2 tren dai phim tat.
        // Tab khong co trong bang (Soat ve: F2 = dua con tro vao o quet; Bao cao) thi F2 khong ghi gi.
        public static readonly IReadOnlyDictionary<string, string[]> QuyenGhiTheoTab = new Dictionary<string, string[]>
        {
            ["MangLuoiGa"] = new[] { MaQuyen.SuaHaTang },
            ["KhuGian"] = new[] { MaQuyen.SuaHaTang },
            ["MacTau"] = PhanQuyenService.QuyenSuaMacTau,
            ["LichTrinh"] = new[] { MaQuyen.LapBieuDoChayTau },
            ["BanVe"] = new[] { MaQuyen.BanVe },
            ["HangHoa"] = new[] { MaQuyen.LapVanDonHang },
            ["NhanSu"] = new[] { MaQuyen.QuanLyHoSoNhanVien, MaQuyen.PhanCongKipLai, MaQuyen.NhanBanKipLai },
            ["PhuongTien"] = new[] { MaQuyen.QuanLyPhuongTien, MaQuyen.LapDoanTau },
            ["BaoTri"] = new[] { MaQuyen.GhiNhatKyBaoTri, MaQuyen.NghiemThuKhamXe },
            ["QuanTri"] = new[] { MaQuyen.CauHinhHeThong },
        };

        public static bool CoQuyenGhiTab(string tag)
            => !QuyenGhiTheoTab.TryGetValue(tag, out var ds) || PhanQuyenService.CoBatKyQuyen(ds);

        // Thu tu tab de chon tab dau tien duoc xem
        private static readonly string[] ThuTuTab =
            { "MangLuoiGa", "KhuGian", "MacTau", "NhanSu", "LichTrinh", "BieuDo", "DieuHanhThucTe", "BanVe", "SoatVe", "HangHoa", "PhuongTien", "BaoTri", "BaoCao", "QuanTri" };

        private bool _daMoTrangDau;

        public static bool CoQuyenXemTab(string tag)
            => !QuyenTheoTab.TryGetValue(tag, out var ds) || PhanQuyenService.CoBatKyQuyen(ds);

        private IEnumerable<(string Tag, Border Tab)> CacTab() => new (string, Border)[]
        {
            ("MangLuoiGa", tabGa), ("KhuGian", tabKhuGian), ("MacTau", tabMacTau), ("NhanSu", tabNavNhanSu),
            ("LichTrinh", tabLichTrinh), ("BieuDo", tabBieuDo), ("DieuHanhThucTe", tabDieuHanhThucTe),
            ("BanVe", tabBanVe), ("SoatVe", tabSoatVe), ("HangHoa", tabHangHoa),
            ("PhuongTien", tabNavPhuongTien), ("BaoTri", tabNavBaoTri),
            ("BaoCao", tabNavBaoCao), ("QuanTri", tabNavQuanTri),
        };

        // Goi trong constructor, truoc NavigateTo dau tien.
        private void ApDungPhanQuyenGiaoDien()
        {
            foreach (var (tag, tab) in CacTab())
                tab.Visibility = CoQuyenXemTab(tag) ? Visibility.Visible : Visibility.Collapsed;

            void Cum(Border cum, string maCum)
                => cum.Visibility = TagToClusterMap.Where(x => x.Value == maCum).Any(x => CoQuyenXemTab(x.Key))
                    ? Visibility.Visible : Visibility.Collapsed;
            Cum(clusterMasterData, "MasterData");
            Cum(clusterVanHanh, "VanHanh");
            Cum(clusterKinhDoanh, "KinhDoanh");
            Cum(clusterKyThuat, "KyThuat");
            Cum(clusterQuanTri, CumQuanTri);

            // Tab nho cua tung cum: neu tab mac dinh bi an thi nho tab dau tien duoc xem trong cum
            foreach (var cum in _lastActiveTabPerCluster.Keys.ToList())
            {
                if (CoQuyenXemTab(_lastActiveTabPerCluster[cum])) continue;
                var thay = ThuTuTab.FirstOrDefault(t => TagToClusterMap.TryGetValue(t, out var c) && c == cum && CoQuyenXemTab(t));
                if (thay != null) _lastActiveTabPerCluster[cum] = thay;
            }

            txtNguoiDangNhap.Text = PhienLamViec.DaDangNhapThat
                ? $"{PhienLamViec.HoTen} ({PhienLamViec.TenDangNhap})" +
                  (PhienLamViec.MoTaVaiTro.Length > 0 ? $" · {PhienLamViec.MoTaVaiTro}" : "")
                : "Chưa đăng nhập";
            txtNguoiDangNhap.ToolTip = "Tài khoản đang đăng nhập — bấm nút ✕ góc trên bên phải để đổi mật khẩu / đăng xuất.";
        }

        // Goi dau NavigateTo. Tra ve tag duoc phep mo (null = khong mo gi).
        private string? KiemTraQuyenDieuHuong(string tag)
        {
            if (CoQuyenXemTab(tag))
            {
                _daMoTrangDau = true;
                return tag;
            }

            if (_daMoTrangDau)
            {
                ThongBaoDialog.CanhBao(
                    $"Tài khoản \"{PhienLamViec.TenDangNhap}\" không có quyền xem phân hệ này.\nLiên hệ quản trị viên nếu cần được cấp quyền.",
                    "Không Đủ Quyền", this);
                return null;
            }

            // Lan mo dau: chuyen sang tab dau tien duoc xem
            var dau = ThuTuTab.FirstOrDefault(CoQuyenXemTab);
            if (dau == null) return null;
            _daMoTrangDau = true;
            return dau;
        }

        // Nut "Dang xuat / Thoat" tren thanh tren cung -> menu tai khoan.
        private void MoMenuTaiKhoan(object sender)
        {
            var menu = new ContextMenu();
            var dau = new MenuItem
            {
                Header = $"{PhienLamViec.HoTen} ({PhienLamViec.TenDangNhap})",
                IsEnabled = false,
                FontWeight = FontWeights.SemiBold
            };
            menu.Items.Add(dau);
            if (PhienLamViec.MoTaVaiTro.Length > 0)
                menu.Items.Add(new MenuItem { Header = "Vai trò: " + PhienLamViec.MoTaVaiTro, IsEnabled = false });
            menu.Items.Add(new Separator());

            var doiMk = new MenuItem { Header = "Đổi mật khẩu..." };
            doiMk.Click += (_, _) =>
            {
                var dlg = new DoiMatKhauDialog { Owner = this };
                dlg.ShowDialog();
                // [12/11] Sai mat khau hien tai du 5 lan -> tai khoan vua bi khoa: ket thuc phien ngay
                if (dlg.BiKhoaTaiKhoan)
                    PhienDangNhapHelper.DangXuat(this, $"Tài khoản \"{PhienLamViec.TenDangNhap}\" bị khóa {PhanQuyenService.SoPhutKhoa} phút do nhập sai mật khẩu hiện tại {PhanQuyenService.SoLanSaiToiDa} lần.");
            };
            menu.Items.Add(doiMk);

            var dangXuat = new MenuItem { Header = "Đăng xuất" };
            dangXuat.Click += (_, _) =>
            {
                if (ThongBaoDialog.XacNhan($"Đăng xuất tài khoản \"{PhienLamViec.TenDangNhap}\"?\nDữ liệu đang nhập dở trên các màn hình sẽ không được lưu.",
                                           "Đăng Xuất", "Đăng xuất", "Hủy bỏ", false, this))
                    PhienDangNhapHelper.DangXuat(this);
            };
            menu.Items.Add(dangXuat);

            var thoat = new MenuItem { Header = "Thoát hệ thống" };
            thoat.Click += (_, _) =>
            {
                if (ThongBaoDialog.XacNhan("Bạn có muốn thoát khỏi hệ thống điều hành?", "Thoát Hệ Thống", "Thoát", "Hủy bỏ", false, this))
                {
                    PhanQuyenService.DangXuat();
                    Application.Current.Shutdown();
                }
            };
            menu.Items.Add(thoat);

            if (sender is FrameworkElement fe)
            {
                menu.PlacementTarget = fe;
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            }
            menu.IsOpen = true;
        }
    }
}
