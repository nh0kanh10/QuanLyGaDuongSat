using System.Linq;
using System.Windows;
using BUS.Services;
using DTO.Common;
using GUI.Views.Dialogs;

namespace GUI.Helpers
{
    // =========================================================================
    // An / chan thao tac ghi theo quyen (Sprint 3, Q10).
    //   AnNeuKhongCoQuyen: an nut ghi khi tai khoan chi co quyen xem (goi tu ma).
    //   CanQuyen (gan trong XAML): helpers:QuyenGiaoDienHelper.CanQuyen="INFRA_EDIT"
    //                      -> phan tu tu an khi khong co quyen; nhieu ma cach nhau dau phay = co 1 trong cac quyen.
    //                      Dung cho nut / muc menu ghi tren trang cua A Ba (12/11) - chi them thuoc tinh, khong sua ma trang.
    //   KiemTra          : goi dau ham mo dialog / ghi (nhap dup dong, phim tat, nut trong the...)
    //                      -> bao ngay, khong de nguoi dung nhap xong moi bi Service tu choi.
    // Service van kiem lai truoc khi ghi CSDL (lop bao ve cuoi cung).
    // =========================================================================
    public static class QuyenGiaoDienHelper
    {
        public static void AnNeuKhongCoQuyen(string maQuyen, params FrameworkElement?[] cacPhanTu)
        {
            if (PhanQuyenService.CoQuyen(maQuyen)) return;
            foreach (var pt in cacPhanTu)
                if (pt != null) pt.Visibility = Visibility.Collapsed;
        }

        public static bool KiemTra(string maQuyen, string hanhDong)
        {
            if (PhanQuyenService.CoQuyen(maQuyen)) return true;
            ThongBaoDialog.CanhBao(
                $"Tài khoản \"{PhienLamViec.TenDangNhap}\" chỉ được xem, không có quyền {hanhDong}.\n" +
                "Liên hệ quản trị viên nếu cần được cấp quyền.",
                "Không Đủ Quyền");
            return false;
        }

        // ---------------------------------------------------------------------
        // Thuoc tinh gan CanQuyen
        // ---------------------------------------------------------------------
        public static readonly DependencyProperty CanQuyenProperty = DependencyProperty.RegisterAttached(
            "CanQuyen", typeof(string), typeof(QuyenGiaoDienHelper), new PropertyMetadata(null, CanQuyen_Changed));

        public static string? GetCanQuyen(DependencyObject d) => (string?)d.GetValue(CanQuyenProperty);
        public static void SetCanQuyen(DependencyObject d, string? giaTri) => d.SetValue(CanQuyenProperty, giaTri);

        // "A,B" -> co A hoac B
        public static bool CoQuyenTheoChuoi(string? cacMa)
        {
            if (string.IsNullOrWhiteSpace(cacMa)) return true;
            var ds = cacMa.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
            return ds.Length == 0 || PhanQuyenService.CoBatKyQuyen(ds);
        }

        private static void CanQuyen_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement el || CoQuyenTheoChuoi(e.NewValue as string)) return;
            el.Visibility = Visibility.Collapsed;
            // Ma cua trang co the dat lai Visibility = Visible sau do (vd hien nut Xoa khi chon dong, mo form sua)
            // -> an lai ngay khi phan tu hien ra. Lambda tinh, khong giu tham chieu ngoai -> khong ro bo nho.
            el.IsVisibleChanged -= GiuAn;
            el.IsVisibleChanged += GiuAn;
        }

        private static void GiuAn(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement el && e.NewValue is true && !CoQuyenTheoChuoi(GetCanQuyen(el)))
                el.Visibility = Visibility.Collapsed;
        }
    }
}
