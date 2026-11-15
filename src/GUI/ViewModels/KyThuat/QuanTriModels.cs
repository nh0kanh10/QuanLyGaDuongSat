using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;
using DTO.QuanTri;

namespace GUI.ViewModels.KyThuat
{
    // =========================================================================
    // Lop hien thi cho trang Quan tri (Sprint 3, Dat). Du lieu goc: DTO/QuanTri/QuanTriDTO.cs.
    // =========================================================================

    internal static class MauQuanTri
    {
        public static SolidColorBrush Tu(string ma)
        {
            var b = (SolidColorBrush)new BrushConverter().ConvertFrom(ma)!;
            b.Freeze();
            return b;
        }
        public static readonly SolidColorBrush Xanh = Tu("#15803D");
        public static readonly SolidColorBrush Do = Tu("#B91C1C");
        public static readonly SolidColorBrush Cam = Tu("#B45309");
        public static readonly SolidColorBrush Xam = Tu("#64748B");
        public static readonly SolidColorBrush Chu = Tu("#0F172A");
    }

    public sealed class TaiKhoanHienThi
    {
        public TaiKhoanHienThi(TaiKhoanDTO goc) => Goc = goc;
        public TaiKhoanDTO Goc { get; }

        public int MaTaiKhoan => Goc.MaTaiKhoan;
        public string TenDangNhap => Goc.TenDangNhap;
        public string HoTenHienThi => Goc.HoTenHienThi;
        public string NhanVaiTro => Goc.NhanVaiTro;
        public string? Email => Goc.Email;
        public string NhanHoSo => Goc.MaNVCode == null ? "—" : $"{Goc.MaNVCode} · {Goc.HoTenNhanVien}";
        public string NhanDangNhapCuoi => Goc.LanDangNhapCuoi?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa đăng nhập";
        public string NhanNgayTao => Goc.NgayTao.ToString("dd/MM/yyyy HH:mm");

        // Uu tien: vo hieu hoa > dang bi khoa > phai doi mat khau > hoat dong
        public string MaTrangThai => !Goc.DangHoatDong ? "VO_HIEU" : Goc.DangBiKhoa ? "BI_KHOA" : "HOAT_DONG";
        public string NhanTrangThai => !Goc.DangHoatDong ? "Vô hiệu hóa"
            : Goc.DangBiKhoa ? $"Bị khóa đến {Goc.KhoaDenLuc:HH:mm}"
            : Goc.PhaiDoiMatKhau ? "Hoạt động · phải đổi MK"
            : Goc.SoLanSaiLienTiep > 0 ? $"Hoạt động · sai {Goc.SoLanSaiLienTiep} lần"
            : "Hoạt động";
        public SolidColorBrush MauTrangThai => !Goc.DangHoatDong ? MauQuanTri.Xam
            : Goc.DangBiKhoa ? MauQuanTri.Do
            : Goc.PhaiDoiMatKhau || Goc.SoLanSaiLienTiep > 0 ? MauQuanTri.Cam
            : MauQuanTri.Xanh;
        public double DoMo => Goc.DangHoatDong ? 1.0 : 0.6;

        public bool KhopTuKhoa(string tuKhoa)
        {
            if (string.IsNullOrWhiteSpace(tuKhoa)) return true;
            string k = BoDau(tuKhoa.Trim());
            return BoDau(TenDangNhap).Contains(k) || BoDau(HoTenHienThi).Contains(k)
                || BoDau(NhanVaiTro).Contains(k) || BoDau(Goc.MaNVCode ?? "").Contains(k)
                || BoDau(Goc.Email ?? "").Contains(k);
        }

        internal static string BoDau(string s)
        {
            var d = s.Normalize(System.Text.NormalizationForm.FormD);
            var kq = new System.Text.StringBuilder(d.Length);
            foreach (char c in d)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    kq.Append(c == 'đ' ? 'd' : c == 'Đ' ? 'D' : c);
            return kq.ToString().ToLowerInvariant();
        }
    }

    // O chon (vai tro trong dialog tai khoan, quyen trong ma tran)
    public sealed class MucChon : INotifyPropertyChanged
    {
        private bool _duocChon;
        public int Ma { get; init; }
        public string Ma2 { get; init; } = "";          // MaVaiTroCode / MaQuyenCode
        public string Ten { get; init; } = "";
        public string? MoTa { get; init; }
        public string Nhom { get; init; } = "";
        public string TenNhom { get; init; } = "";
        public bool ChoSua { get; init; } = true;
        public bool DuocChon
        {
            get => _duocChon;
            set { if (_duocChon == value) return; _duocChon = value; PropertyChanged?.Invoke(this, new(nameof(DuocChon))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class VaiTroHienThi
    {
        public VaiTroHienThi(VaiTroDTO goc, int soTaiKhoan, int soQuyen) { Goc = goc; SoTaiKhoan = soTaiKhoan; SoQuyen = soQuyen; }
        public VaiTroDTO Goc { get; }
        public int SoTaiKhoan { get; }
        public int SoQuyen { get; }
        public string TenVaiTro => Goc.TenVaiTro;
        public string MaVaiTroCode => Goc.MaVaiTroCode;
        public string NhanPhu => $"{Goc.MaVaiTroCode} · {SoQuyen} quyền · {SoTaiKhoan} tài khoản";
    }

    public sealed class LichSuDangNhapHienThi
    {
        public LichSuDangNhapHienThi(LichSuDangNhapDTO goc) => Goc = goc;
        public LichSuDangNhapDTO Goc { get; }
        public string NhanThoiDiem => Goc.ThoiDiem.ToString("dd/MM/yyyy HH:mm:ss");
        public string TenDangNhap => Goc.TenDangNhap;
        public string HoTen => Goc.HoTenHienThi ?? "(không có tài khoản)";
        public string NhanKetQua => Goc.ThanhCong ? "Thành công" : "Thất bại";
        public SolidColorBrush MauKetQua => Goc.ThanhCong ? MauQuanTri.Xanh : MauQuanTri.Do;
        public string LyDo => Goc.LyDoThatBai ?? "";
        public string ThietBi => Goc.ThietBi ?? "";
        public string DiaChiIP => Goc.DiaChiIP ?? "";
    }

    public sealed record TruongThayDoi(string Truong, string Truoc, string Sau, bool DaDoi)
    {
        public SolidColorBrush MauDong => DaDoi ? MauQuanTri.Tu("#FEF3C7") : MauQuanTri.Tu("#FFFFFF");
        public System.Windows.FontWeight DoDam => DaDoi ? System.Windows.FontWeights.SemiBold : System.Windows.FontWeights.Normal;
    }

    public sealed class NhatKyHienThi
    {
        public NhatKyHienThi(NhatKyHeThongDTO goc) => Goc = goc;
        public NhatKyHeThongDTO Goc { get; }
        public string NhanThoiDiem => Goc.ThoiDiem.ToString("dd/MM/yyyy HH:mm:ss");
        public string NguoiThucHien => Goc.TenDangNhap ?? "(hệ thống)";
        public string HoTen => Goc.HoTenHienThi ?? "";
        public string HanhDong => Goc.HanhDong;
        public string NhanHanhDong => TenHanhDong(Goc.HanhDong);
        public string TenBang => Goc.TenBang;
        public string KhoaChinh => Goc.KhoaChinhRecord;
        public SolidColorBrush MauHanhDong => Goc.HanhDong switch
        {
            "XOA" or "HUY_KIP" or "KHOA_TAI_KHOAN" or "BO_DUYET_AN_TOAN" or "DELETE" or "HOAN_VE" or "HUY_VAN_DON" or "HUY_SOAT_VE" => MauQuanTri.Do,
            "THEM" or "INSERT" or "PHAN_CONG_KIP" or "LAP_DOAN_TAU" or "DUYET_AN_TOAN" or "BAN_VE" or "LAP_VAN_DON" => MauQuanTri.Xanh,
            "DANG_XUAT" or "DOI_MAT_KHAU" => MauQuanTri.Xam,
            _ => MauQuanTri.Chu
        };

        public static string TenHanhDong(string ma) => ma switch
        {
            "THEM" or "INSERT" => "Thêm",
            "SUA" or "UPDATE" => "Sửa",
            "XOA" or "DELETE" => "Xóa",
            "LAP_DOAN_TAU" => "Lập đoàn tàu",
            "SUA_DOAN_TAU" => "Sửa đoàn tàu",
            "PHAN_CONG_KIP" => "Phân công kíp",
            "SUA_KIP" => "Sửa kíp",
            "HUY_KIP" => "Hủy kíp",
            "GHI_KIEM_TRA" => "Ghi kiểm tra lên ban",
            "NHAN_BAN" => "Nhận ban",
            "BAN_GIAO" => "Bàn giao",
            "DUYET_AN_TOAN" => "Duyệt an toàn",
            "BO_DUYET_AN_TOAN" => "Bỏ duyệt an toàn",
            "LAP_BIEN_BAN" => "Lập biên bản khám",
            "DANG_XUAT" => "Đăng xuất",
            "DOI_MAT_KHAU" => "Đổi mật khẩu",
            "DAT_LAI_MAT_KHAU" => "Đặt lại mật khẩu",
            "KHOA_TAI_KHOAN" => "Khóa tài khoản",
            "MO_KHOA" => "Mở khóa",
            "SUA_QUYEN_VAI_TRO" => "Sửa quyền vai trò",
            "TU_VAN_VE" => "Tư vấn vé AI",
            "DOI_TRANG_THAI_CHUYEN" => "Đổi trạng thái chuyến",
            "BAN_VE" => "Bán vé",
            "HOAN_VE" => "Hoàn vé",
            "DOI_GHE" => "Đổi ghế",
            "SOAT_VE" => "Soát vé lên tàu",
            "HUY_SOAT_VE" => "Hủy soát vé",
            "SOAT_VE_THEO_LO" => "Soát vé cả ga",
            "LAP_VAN_DON" => "Lập vận đơn",
            "SUA_VAN_DON" => "Sửa vận đơn",
            "HUY_VAN_DON" => "Hủy vận đơn",
            "DOI_TRANG_THAI_VAN_DON" => "Đổi trạng thái vận đơn",
            "SUA_LICH_TRINH" => "Sửa lịch trình chuyến",
            "XEP_TRANH_TAU" => "Xếp tránh tàu",
            "DONG_BO_TOA_BAN_VE" => "Đồng bộ toa bán vé",
            _ => ma
        };

        // So sanh JSON cu / moi theo tung truong cap 1 (gia tri long nhau in nguyen JSON).
        public List<TruongThayDoi> CacTruong()
        {
            var cu = DocJson(Goc.DuLieuCu);
            var moi = DocJson(Goc.DuLieuMoi);
            var ten = cu.Keys.Concat(moi.Keys).Distinct().ToList();
            return ten.Select(t =>
            {
                cu.TryGetValue(t, out var a);
                moi.TryGetValue(t, out var b);
                return new TruongThayDoi(t, a ?? "", b ?? "", (a ?? "") != (b ?? "") && Goc.DuLieuCu != null && Goc.DuLieuMoi != null);
            }).ToList();
        }

        private static Dictionary<string, string> DocJson(string? json)
        {
            var kq = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(json)) return kq;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) { kq["(giá trị)"] = json; return kq; }
                foreach (var p in doc.RootElement.EnumerateObject())
                {
                    kq[p.Name] = p.Value.ValueKind switch
                    {
                        JsonValueKind.String => p.Value.GetString() ?? "",
                        JsonValueKind.Null => "(trống)",
                        _ => p.Value.GetRawText()
                    };
                }
            }
            catch (JsonException)
            {
                kq["(nội dung)"] = json;
            }
            return kq;
        }
    }

    public static class TenNhomQuyen
    {
        public static string Ten(string nhom) => nhom switch
        {
            "HE_THONG" => "Quản trị hệ thống",
            "BAO_CAO" => "Báo cáo thống kê",
            "AI" => "Trợ lý AI",
            "DIEU_DO" => "Mạng lưới & điều độ chạy tàu",
            "VAN_TAI" => "Kinh doanh vận tải (vé, hàng hóa)",
            "PHUONG_TIEN" => "Phương tiện & lập tàu",
            "KY_THUAT" => "Bảo trì & khám xe",
            "NHAN_SU" => "Nhân sự & kíp lái",
            _ => nhom
        };

        public static int ThuTu(string nhom) => nhom switch
        {
            "HE_THONG" => 0, "BAO_CAO" => 1, "AI" => 2, "DIEU_DO" => 3, "VAN_TAI" => 4,
            "PHUONG_TIEN" => 5, "KY_THUAT" => 6, "NHAN_SU" => 7, _ => 9
        };
    }
}
