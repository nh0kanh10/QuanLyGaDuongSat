using System.Windows;
using System.Windows.Media;
using BUS.Services;
using DTO.Common;
using GUI.Views.Dialogs;
using GUI.Views.Pages;

namespace GUI
{
    // =========================================================================
    // CUM 5 "QUAN TRI & BAO CAO" tren man hinh chinh (Sprint 3, Dat - partial cua MainWindow cua A Ba).
    //   Tab BaoCao (REPORT_VIEW) -> BaoCaoPage, Tab QuanTri (SYS_CONFIG) -> QuanTriPage.
    // MainWindow.xaml.cs chi them 1 dong goi vao moi cho: constructor, NavigateTo, CapNhatTrangThaiRibbon,
    // CapNhatThanhPhimTatTheoPhanHe, ThucHien{TraCuu,ThemMoi,NapLai,XuatTep,Huy}.
    // Trang tao khi mo lan dau (nguoi khong co quyen khong ton cong nap du lieu).
    // =========================================================================
    public partial class MainWindow
    {
        private const string CumQuanTri = "QuanTriBaoCao";
        private QuanTriPage? _quanTriPage;
        private BaoCaoPage? _baoCaoPage;

        private void ThemCumQuanTri()
        {
            TagToClusterMap.TryAdd("BaoCao", CumQuanTri);
            TagToClusterMap.TryAdd("QuanTri", CumQuanTri);
            _lastActiveTabPerCluster[CumQuanTri] = "BaoCao";

            // O be rong nho (1100 px) 5 cum + huy hieu tuyen khong du cho: an huy hieu
            SizeChanged += (_, _) =>
            {
                bdHuyHieuTuyen.Visibility = ActualWidth < 1380 ? Visibility.Collapsed : Visibility.Visible;
                // [12/11] 1100 px: ten cum bi cat "QUAN TRI & BAO C..." -> rut gon, ten day du o tooltip
                txtClusterQuanTri.Text = ActualWidth < 1300 ? "QUẢN TRỊ" : "QUẢN TRỊ & BÁO CÁO";
            };
            clusterQuanTri.ToolTip = "Quản trị & Báo cáo: báo cáo thống kê, tài khoản, phân quyền, nhật ký hệ thống";

            // [09/11] Dang xuat dong MainWindow nay roi tao cua so moi. DispatcherTimer dong ho (_timer, MainWindow.xaml.cs)
            // dang chay thi Dispatcher giu tham chieu -> ca cua so cu + moi trang (Ban ve ~34.000 ve) khong duoc giai phong,
            // +~70 MB moi lan dang xuat. Dung timer khi dong.
            Closed += (_, _) => _timer.Stop();

            // [12/11] Dong bang nut X / Alt+F4 khi con dang nhap: hoi lai + ghi DANG_XUAT nhu menu "Thoat he thong".
            // Dang xuat / Thoat tu menu da goi PhanQuyenService.DangXuat() truoc khi dong -> khong hoi lan nua.
            Closing += (_, e) =>
            {
                if (!PhienLamViec.BatBuocDangNhap || !PhienLamViec.DaDangNhapThat) return;
                if (!ThongBaoDialog.XacNhan("Bạn có muốn thoát khỏi hệ thống điều hành?\nDữ liệu đang nhập dở trên các màn hình sẽ không được lưu.",
                                            "Thoát Hệ Thống", "Thoát", "Hủy bỏ", false, this))
                {
                    e.Cancel = true;
                    return;
                }
                PhanQuyenService.DangXuat();
            };
        }

        private static bool LaTrangQuanTri(string tag) => tag is "BaoCao" or "QuanTri";

        private void DieuHuongTrangQuanTri(string tag)
        {
            if (tag == "QuanTri") MainFrame.Navigate(_quanTriPage ??= new QuanTriPage());
            else MainFrame.Navigate(_baoCaoPage ??= new BaoCaoPage());
        }

        private void CapNhatRibbonQuanTri(string clusterKey, string activeSubTag)
        {
            DatTrangThaiCluster(clusterQuanTri, iconClusterQuanTri, txtClusterQuanTri, indicatorQuanTri, clusterKey == CumQuanTri);
            subBarQuanTri.Visibility = clusterKey == CumQuanTri ? Visibility.Visible : Visibility.Collapsed;

            var nenTat = Brushes.Transparent;
            var chuTat = (Brush)new BrushConverter().ConvertFrom("#475569")!;
            var iconTat = (Brush)new BrushConverter().ConvertFrom("#64748B")!;
            foreach (var (tab, txt, icon) in new[] { (tabNavBaoCao, txtTabBaoCao, iconTabBaoCao), (tabNavQuanTri, txtTabQuanTri, iconTabQuanTri) })
            {
                tab.Background = nenTat;
                tab.BorderBrush = nenTat;
                txt.Foreground = chuTat;
                txt.FontWeight = FontWeights.SemiBold;
                icon.Foreground = iconTat;
            }
            if (activeSubTag == "BaoCao") SetTabActive(tabNavBaoCao, txtTabBaoCao, iconTabBaoCao);
            if (activeSubTag == "QuanTri") SetTabActive(tabNavQuanTri, txtTabQuanTri, iconTabQuanTri);
        }

        // Dai phim tat cho 2 trang moi. Tra ve true = da xu ly (trang cua cum 5).
        private bool CapNhatPhimTatQuanTri(string tag)
        {
            if (!LaTrangQuanTri(tag))
            {
                // [12/11] Trang khac: phan con lai cua CapNhatThanhPhimTatTheoPhanHe (MainWindow.xaml.cs) dat dai phim tat
                // ngay sau ham nay -> an nut F2 o luot ke tiep cua Dispatcher neu tai khoan chi duoc xem.
                if (!CoQuyenGhiTab(tag))
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (_currentTag != tag) return;
                        btnScF2.Visibility = Visibility.Collapsed;
                        sepScF2.Visibility = Visibility.Collapsed;
                    }, System.Windows.Threading.DispatcherPriority.Loaded);
                return false;
            }
            bool qt = tag == "QuanTri";

            void Dat(FrameworkElement nut, FrameworkElement vach, bool hien)
            {
                nut.Visibility = hien ? Visibility.Visible : Visibility.Collapsed;
                vach.Visibility = nut.Visibility;
            }
            Dat(btnScF2, sepScF2, qt);
            if (qt) { txtScF2.Text = "Thêm Tài Khoản"; btnScF2.ToolTip = "F2: Tạo tài khoản mới"; }
            Dat(btnScF3, sepScF3, true);
            txtScF3.Text = "Tra Cứu";
            btnScF3.ToolTip = "F3: Đưa con trỏ vào ô tìm kiếm của tab đang mở";
            Dat(btnScF4, sepScF4, false);
            Dat(btnScF5, sepScF5, true);
            txtScF5.Text = "Nạp Lại";
            btnScF5.ToolTip = qt ? "F5: Nạp lại tài khoản, vai trò, lịch sử, nhật ký" : "F5: Tính lại báo cáo theo bộ lọc";
            Dat(btnScF6, sepScF6, !qt);
            if (!qt) { txtScF6.Text = "Xuất Excel"; btnScF6.ToolTip = "F6: Xuất báo cáo đang xem ra tệp Excel"; }
            Dat(btnScF7, sepScF7, false);
            Dat(btnScF8, sepScF8, false);
            Dat(btnScF9, sepScF9, false);
            return true;
        }

        private bool PhimTatQuanTri(string phim)
        {
            // [12/11] F2 = them / lap moi: chan o moi trang khi tai khoan khong co quyen ghi cua phan he
            if (phim == "F2" && !CoQuyenGhiTab(_currentTag))
            {
                ThongBaoDialog.CanhBao($"Tài khoản \"{PhienLamViec.TenDangNhap}\" chỉ được xem phân hệ này, không thêm / lập mới được.\n" +
                                       "Liên hệ quản trị viên nếu cần được cấp quyền.", "Không Đủ Quyền", this);
                return true;
            }
            if (!LaTrangQuanTri(_currentTag)) return false;
            if (_currentTag == "QuanTri" && _quanTriPage != null)
            {
                switch (phim)
                {
                    case "F2": _quanTriPage.KichHoatThemMoi(); break;
                    case "F3": _quanTriPage.FocusTimKiem(); break;
                    case "F5": _quanTriPage.KichHoatNapLai(); break;
                    case "ESC": _quanTriPage.KichHoatHuy(); break;
                }
            }
            else if (_currentTag == "BaoCao" && _baoCaoPage != null)
            {
                switch (phim)
                {
                    case "F3": _baoCaoPage.FocusTimKiem(); break;
                    case "F5": _baoCaoPage.KichHoatNapLai(); break;
                    case "F6": _baoCaoPage.KichHoatXuatExcel(); break;
                }
            }
            return true;
        }
    }
}
