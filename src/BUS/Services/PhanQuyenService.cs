using System.Collections.Generic;
using DTO.Common;

namespace BUS.Services
{
    // =========================================================================
    // Cua ngo kiem tra quyen cho tang BUS / GUI.
    //
    // Hien tai chi goi lai PhienLamViec (phien mac dinh => tra true), nhung moi
    // noi nen goi qua Service nay thay vi goi thang PhienLamViec: Sprint 3 noi
    // dang nhap that thi chi sua trong file nay.
    //
    // Quy tac Q10 (KE_HOACH_BACK_END.md §8): khong chi an nut tren giao dien -
    // Service phai kiem lai quyen truoc khi ghi CSDL.
    // =========================================================================
    public static class PhanQuyenService
    {
        public static bool CoQuyen(string maQuyenCode) => PhienLamViec.CoQuyen(maQuyenCode);

        public static bool CoBatKyQuyen(params string[] cacMaQuyen)
            => PhienLamViec.CoBatKyQuyen(cacMaQuyen);

        // Goi ngay dau moi ham ghi CSDL co rang buoc quyen.
        // Thieu quyen => nem KhongDuQuyenException, GUI bat lai hien ThongBaoDialog.
        public static void BatBuocCoQuyen(string maQuyenCode, string? moTaHanhDong = null)
            => PhienLamViec.BatBuocCoQuyen(maQuyenCode, moTaHanhDong);

        // Nguoi dang thao tac - dung cho cot NguoiTao / NguoiCapNhat (NVARCHAR).
        public static string TenDangNhapHienTai => PhienLamViec.TenDangNhap;

        // Dung cho cot NguoiDuyet / NguoiLap / NguoiKham (INT -> MaTaiKhoan).
        public static int MaTaiKhoanHienTai => PhienLamViec.MaTaiKhoan;

        // Dung khi bang tham chieu nhansu.NhanVien (vi du PhanCongKipLai).
        public static int? MaNhanVienHienTai => PhienLamViec.MaNhanVien;

        public static IReadOnlyCollection<string> CacQuyenHienTai => PhienLamViec.CacQuyen;

        // =====================================================================
        // Sprint 3 (Quan tri & phan quyen) se bo sung tai day:
        //   - DangNhap(tenDangNhap, matKhau): doc phanquyen.TaiKhoan, kiem
        //     MatKhauHash (Q12: BCrypt hay PBKDF2), kiem DangHoatDong, ghi
        //     LichSuDangNhap, cap nhat LanDangNhapCuoi, roi goi
        //     PhienLamViec.DangNhap(...) kem danh sach quyen.
        //   - LayQuyenCuaTaiKhoan(maTaiKhoan): TaiKhoan_VaiTro -> VaiTro_Quyen -> Quyen.
        // Chua viet o day vi chua kiem thu duoc tren CSDL that.
        // =====================================================================
    }
}
