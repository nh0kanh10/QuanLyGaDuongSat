using System;
using System.Collections.Generic;
using System.Linq;

namespace DTO.QuanTri
{
    // =========================================================================
    // Quan tri & phan quyen (Sprint 3, Dat): schema phanquyen + kiemtoan.NhatKyHeThong.
    // Thoi gian trong cac bang nay luu gio UTC (DEFAULT SYSUTCDATETIME() cua script goc);
    // Repository doi sang gio may chu truoc khi tra ve (thuoc tinh ...Local).
    // =========================================================================

    // Dong tai khoan doc ra luc dang nhap (khoa dong UPDLOCK trong giao dich).
    public sealed class TaiKhoanDangNhapDTO
    {
        public int MaTaiKhoan { get; init; }
        public string TenDangNhap { get; init; } = "";
        public string MatKhauHash { get; init; } = "";
        public string HoTenHienThi { get; init; } = "";
        public int? MaNhanVien { get; init; }
        public bool DangHoatDong { get; init; }
        public int SoLanSaiLienTiep { get; init; }
        public DateTime? KhoaDenLucUtc { get; init; }
        public bool PhaiDoiMatKhau { get; init; }
        public DateTime BayGioUtc { get; init; }      // SYSUTCDATETIME() cua may chu, cung cau truy van

        public bool DangBiKhoa => KhoaDenLucUtc.HasValue && KhoaDenLucUtc.Value > BayGioUtc;
    }

    public sealed class KetQuaDangNhap
    {
        public bool ThanhCong { get; init; }
        public string ThongBao { get; init; } = "";
        public bool PhaiDoiMatKhau { get; init; }
        public bool BiKhoa { get; init; }
        public int SoLanSai { get; init; }
        public IReadOnlyList<string> CacVaiTro { get; init; } = Array.Empty<string>();
    }

    public sealed record VaiTroDTO(int MaVaiTro, string MaVaiTroCode, string TenVaiTro, string? MoTa, bool KichHoat);

    public sealed record QuyenDTO(int MaQuyen, string MaQuyenCode, string TenQuyen, string NhomQuyen, string? MoTa);

    // Mot dong trong danh sach tai khoan (trang Quan tri).
    public sealed class TaiKhoanDTO
    {
        public int MaTaiKhoan { get; init; }
        public string TenDangNhap { get; init; } = "";
        public string HoTenHienThi { get; init; } = "";
        public string? Email { get; init; }
        public int? MaNhanVien { get; init; }
        public string? MaNVCode { get; init; }
        public string? HoTenNhanVien { get; init; }
        public bool DangHoatDong { get; init; }
        public int SoLanSaiLienTiep { get; init; }
        public DateTime? KhoaDenLuc { get; init; }          // gio may chu
        public bool DangBiKhoa { get; init; }
        public bool PhaiDoiMatKhau { get; init; }
        public DateTime? LanDangNhapCuoi { get; init; }     // gio may chu
        public DateTime NgayTao { get; init; }              // gio may chu
        public byte[] PhienBan { get; init; } = Array.Empty<byte>();
        public List<VaiTroDTO> CacVaiTro { get; init; } = new();

        public string NhanVaiTro => CacVaiTro.Count == 0 ? "(chưa gán)" : string.Join(", ", CacVaiTro.Select(v => v.TenVaiTro));
    }

    // Du lieu nhap tu dialog them / sua tai khoan.
    public sealed class TaiKhoanLuuDTO
    {
        public int MaTaiKhoan { get; set; }                 // 0 = them moi
        public string TenDangNhap { get; set; } = "";
        public string HoTenHienThi { get; set; } = "";
        public string? Email { get; set; }
        public int? MaNhanVien { get; set; }
        public bool DangHoatDong { get; set; } = true;
        public string? MatKhauMoi { get; set; }             // chi dung khi them moi
        public List<int> CacMaVaiTro { get; set; } = new();
        public byte[]? PhienBan { get; set; }
    }

    public sealed class LichSuDangNhapDTO
    {
        public long MaLichSu { get; init; }
        public int? MaTaiKhoan { get; init; }
        public string TenDangNhap { get; init; } = "";
        public string? HoTenHienThi { get; init; }
        public DateTime ThoiDiem { get; init; }             // gio may chu
        public string? DiaChiIP { get; init; }
        public string? ThietBi { get; init; }
        public bool ThanhCong { get; init; }
        public string? LyDoThatBai { get; init; }

        public string NhanKetQua => ThanhCong ? "Thành công" : "Thất bại";
    }

    public sealed class NhatKyHeThongDTO
    {
        public long MaNhatKy { get; init; }
        public DateTime ThoiDiem { get; init; }             // gio may chu
        public int? MaTaiKhoan { get; init; }
        public string? TenDangNhap { get; init; }
        public string? HoTenHienThi { get; init; }
        public string HanhDong { get; init; } = "";
        public string TenBang { get; init; } = "";
        public string KhoaChinhRecord { get; init; } = "";
        public string? DuLieuCu { get; init; }
        public string? DuLieuMoi { get; init; }
        public string? DiaChiIP { get; init; }

        public string NguoiThucHien => string.IsNullOrEmpty(TenDangNhap) ? "(hệ thống)" : $"{TenDangNhap}";
    }

    public sealed class BoLocNhatKy
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }               // tinh ca ngay nay
        public int? MaTaiKhoan { get; set; }
        public string? TenBang { get; set; }
        public string? HanhDong { get; set; }
        public string? TuKhoa { get; set; }
        public bool? ThanhCong { get; set; }                // chi dung cho lich su dang nhap
        public int SoDongToiDa { get; set; } = 1000;
    }
}
