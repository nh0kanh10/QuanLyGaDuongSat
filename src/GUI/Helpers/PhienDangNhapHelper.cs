using System.Windows;
using BUS.Services;
using DTO.Common;
using GUI.Views.Dialogs;

namespace GUI.Helpers
{
    // =========================================================================
    // Vong doi phien dang nhap cua ung dung (Sprint 3, Dat):
    //   khoi dong -> DangNhapDialog -> MainWindow moi;  Dang xuat -> dong MainWindow -> DangNhapDialog.
    // MainWindow tao lai moi lan dang nhap -> cac trang tao moi, khong giu du lieu cua nguoi truoc.
    // Trong luc chi co cua so dang nhap: ShutdownMode = OnExplicitShutdown (khong thi dong dialog la thoat app).
    // =========================================================================
    public static class PhienDangNhapHelper
    {
        public static bool MoDangNhapVaManHinhChinh(string? tenGoiY = null, string? thongBao = null)
        {
            var app = Application.Current;
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var dn = new DangNhapDialog(tenGoiY, thongBao);
            if (dn.ShowDialog() != true)
            {
                app.Shutdown();
                return false;
            }

            var mw = new MainWindow();
            app.MainWindow = mw;
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            mw.Show();
            mw.Activate();
            return true;
        }

        public static void DangXuat(Window manHinhChinh, string? thongBao = null)
        {
            string ten = PhienLamViec.TenDangNhap;
            PhanQuyenService.DangXuat();

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            manHinhChinh.Close();
            MoDangNhapVaManHinhChinh(ten, thongBao ?? $"Tài khoản \"{ten}\" đã đăng xuất.");
        }
    }
}
