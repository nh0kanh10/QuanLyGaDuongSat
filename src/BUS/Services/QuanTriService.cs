using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DAL.Connection;
using DAL.Repositories;
using DTO.Common;
using DTO.PhuongTien;
using DTO.QuanTri;

namespace BUS.Services
{
    // =========================================================================
    // QUAN TRI HE THONG (Sprint 3, Dat): tai khoan, vai tro & quyen, lich su dang nhap, nhat ky.
    // Moi thao tac can quyen SYS_CONFIG (doc lan ghi). Ghi: giao dich + khoa ung dung "phanquyen.QuanTri"
    // (tuan tu hoa cac thao tac doi tai khoan / vai tro) + nhat ky he thong (khong bao gio ghi mat khau).
    //
    // Luat:
    //   - ten dang nhap 3-50 ky tu, chu thuong khong dau / so / . _ -, khong trung (khong phan biet hoa thuong);
    //   - tai khoan moi phai co mat khau ban dau du manh + it nhat 1 vai tro; PhaiDoiMatKhau = 1;
    //   - khong tu vo hieu hoa / tu bo vai tro ADMIN cua chinh minh;
    //   - he thong luon con it nhat 1 tai khoan ADMIN dang hoat dong;
    //   - vai tro ADMIN luon co du moi quyen (khong sua tren ma tran).
    // =========================================================================
    public class QuanTriService
    {
        public const string TaiNguyenKhoa = "phanquyen.QuanTri";
        public const string VaiTroQuanTri = "ADMIN";

        private static readonly Regex MauTenDangNhap = new("^[a-z0-9._-]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex MauEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly PhanQuyenRepository _repo = new();

        // =====================================================================
        // DOC
        // =====================================================================

        public bool LayDanhSachTaiKhoan(out List<TaiKhoanDTO> ds, out string loi)
        {
            ds = new();
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "xem tài khoản", out loi)) return false;
            List<TaiKhoanDTO> kq = new();
            bool ok = Thu(() => kq = _repo.LayDanhSachTaiKhoan(), out loi);
            ds = kq;
            return ok;
        }

        public bool LayDanhMuc(out List<VaiTroDTO> vaiTro, out List<QuyenDTO> quyen,
                               out HashSet<(int MaVaiTro, int MaQuyen)> maTran, out string loi)
        {
            vaiTro = new(); quyen = new(); maTran = new();
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "xem vai trò & quyền", out loi)) return false;
            List<VaiTroDTO> v = new(); List<QuyenDTO> q = new(); HashSet<(int, int)> m = new();
            bool ok = Thu(() => { v = _repo.LayDanhSachVaiTro(); q = _repo.LayDanhSachQuyen(); m = _repo.LayVaiTroQuyen(); }, out loi);
            vaiTro = v; quyen = q; maTran = m;
            return ok;
        }

        public bool LayNhanVienDeNoi(out List<(int Ma, string MaNV, string HoTen, string ChucDanh, int SoTaiKhoan)> ds, out string loi)
        {
            ds = new();
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "xem hồ sơ nhân viên", out loi)) return false;
            List<(int, string, string, string, int)> kq = new();
            bool ok = Thu(() => kq = _repo.LayNhanVienDeNoi(), out loi);
            ds = kq;
            return ok;
        }

        public bool LayLichSuDangNhap(BoLocNhatKy loc, out List<LichSuDangNhapDTO> ds, out string loi)
        {
            ds = new();
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "xem lịch sử đăng nhập", out loi)) return false;
            if (!KiemTraKhoangNgay(loc, out loi)) return false;
            List<LichSuDangNhapDTO> kq = new();
            bool ok = Thu(() => kq = _repo.LayLichSuDangNhap(loc), out loi);
            ds = kq;
            return ok;
        }

        public bool LayNhatKyHeThong(BoLocNhatKy loc, out List<NhatKyHeThongDTO> ds, out string loi)
        {
            ds = new();
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "xem nhật ký hệ thống", out loi)) return false;
            if (!KiemTraKhoangNgay(loc, out loi)) return false;
            List<NhatKyHeThongDTO> kq = new();
            bool ok = Thu(() => kq = _repo.LayNhatKyHeThong(loc), out loi);
            ds = kq;
            return ok;
        }

        public bool LayGiaTriLocNhatKy(out List<string> bang, out List<string> hanhDong, out string loi)
        {
            bang = new(); hanhDong = new();
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "xem nhật ký hệ thống", out loi)) return false;
            List<string> b = new(), h = new();
            bool ok = Thu(() => (b, h) = _repo.LayGiaTriLocNhatKy(), out loi);
            bang = b; hanhDong = h;
            return ok;
        }

        // =====================================================================
        // KIEM TRA NHAP LIEU (dialog + Service dung chung)
        // =====================================================================
        public static List<LoiNhapLieuDTO> KiemTraTaiKhoan(TaiKhoanLuuDTO tk, bool laThemMoi)
        {
            var loi = new List<LoiNhapLieuDTO>();
            if (laThemMoi)
            {
                string ten = (tk.TenDangNhap ?? "").Trim();
                if (ten.Length == 0) loi.Add(new("TenDangNhap", "Chưa nhập tên đăng nhập."));
                else if (!MauTenDangNhap.IsMatch(ten))
                    loi.Add(new("TenDangNhap", "Tên đăng nhập 3–50 ký tự, chỉ gồm chữ thường không dấu, chữ số và . _ -"));

                if (MatKhauHelper.KiemTraDoManh(tk.MatKhauMoi) is { } yeu) loi.Add(new("MatKhauMoi", yeu));
            }

            string hoTen = (tk.HoTenHienThi ?? "").Trim();
            if (hoTen.Length < 2) loi.Add(new("HoTenHienThi", "Tên hiển thị phải có ít nhất 2 ký tự."));
            else if (hoTen.Length > 100) loi.Add(new("HoTenHienThi", "Tên hiển thị tối đa 100 ký tự."));
            else if (hoTen.Any(char.IsControl)) loi.Add(new("HoTenHienThi", "Tên hiển thị có ký tự không hợp lệ."));

            string email = (tk.Email ?? "").Trim();
            if (email.Length > 0)
            {
                if (email.Length > 100) loi.Add(new("Email", "Email tối đa 100 ký tự."));
                else if (!MauEmail.IsMatch(email) || email.Any(c => c > 127)) loi.Add(new("Email", "Email không đúng định dạng (ví dụ ten@vr.com.vn)."));
            }

            if (tk.CacMaVaiTro == null || tk.CacMaVaiTro.Count == 0)
                loi.Add(new("VaiTro", "Phải gán ít nhất 1 vai trò."));
            return loi;
        }

        // =====================================================================
        // GHI
        // =====================================================================

        public bool ThemTaiKhoan(TaiKhoanLuuDTO tk, out int maMoi, out string loi)
        {
            maMoi = 0;
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "thêm tài khoản", out loi)) return false;
            ChuanHoa(tk);
            if (!HopLe(KiemTraTaiKhoan(tk, true), out loi)) return false;

            int ma = 0;
            bool ok = TrongGiaoDich(gd =>
            {
                if (_repo.TonTaiTenDangNhap(tk.TenDangNhap, gd))
                    return $"Tên đăng nhập \"{tk.TenDangNhap}\" đã có người dùng.";
                if (LoiNoiNhanVien(tk, gd) is { } loiNv) return loiNv;
                if (KiemTraVaiTro(tk.CacMaVaiTro, gd) is { } loiVt) return loiVt;

                ma = _repo.ThemTaiKhoan(tk, MatKhauHelper.TaoChuoiBam(tk.MatKhauMoi!), gd);
                _repo.ThayVaiTro(ma, tk.CacMaVaiTro, PhanQuyenService.MaTaiKhoanHienTai, gd);
                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "THEM", "phanquyen.TaiKhoan", ma.ToString(), null,
                    PhanQuyenService.Json(MoTa(tk, TenVaiTro(tk.CacMaVaiTro, gd))));
                return null;
            }, out loi);
            maMoi = ma;
            return ok;
        }

        public bool CapNhatTaiKhoan(TaiKhoanLuuDTO tk, out string loi)
        {
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "sửa tài khoản", out loi)) return false;
            ChuanHoa(tk);
            if (!HopLe(KiemTraTaiKhoan(tk, false), out loi)) return false;

            return TrongGiaoDich(gd =>
            {
                var goc = _repo.LayDanhSachTaiKhoan(gd).FirstOrDefault(x => x.MaTaiKhoan == tk.MaTaiKhoan);
                if (goc == null) return "Tài khoản không còn trong CSDL. Hãy nạp lại.";
                if (tk.PhienBan == null || !goc.PhienBan.AsSpan().SequenceEqual(tk.PhienBan))
                    return $"Tài khoản \"{goc.TenDangNhap}\" vừa được người khác thay đổi. Hãy nạp lại rồi sửa lại.";
                if (LoiNoiNhanVien(tk, gd) is { } loiNv) return loiNv;
                if (KiemTraVaiTro(tk.CacMaVaiTro, gd) is { } loiVt) return loiVt;

                var vaiTroMoi = _repo.LayDanhSachVaiTro(gd).Where(v => tk.CacMaVaiTro.Contains(v.MaVaiTro)).ToList();
                bool moiLaQuanTri = tk.DangHoatDong && vaiTroMoi.Any(v => v.MaVaiTroCode == VaiTroQuanTri && v.KichHoat);
                bool gocLaQuanTri = goc.DangHoatDong && goc.CacVaiTro.Any(v => v.MaVaiTroCode == VaiTroQuanTri && v.KichHoat);

                if (tk.MaTaiKhoan == PhanQuyenService.MaTaiKhoanHienTai)
                {
                    if (!tk.DangHoatDong) return "Không thể tự vô hiệu hóa tài khoản đang đăng nhập.";
                    if (gocLaQuanTri && !moiLaQuanTri) return "Không thể tự bỏ vai trò Quản trị viên của chính mình.";
                }
                if (gocLaQuanTri && !moiLaQuanTri && _repo.DemQuanTriVienKhac(tk.MaTaiKhoan, gd) == 0)
                    return "Đây là tài khoản Quản trị viên cuối cùng đang hoạt động — không thể vô hiệu hóa / bỏ vai trò.";

                if (_repo.CapNhatTaiKhoan(tk, gd) == 0)
                    return $"Tài khoản \"{goc.TenDangNhap}\" vừa được người khác thay đổi. Hãy nạp lại rồi sửa lại.";
                _repo.ThayVaiTro(tk.MaTaiKhoan, tk.CacMaVaiTro, PhanQuyenService.MaTaiKhoanHienTai, gd);

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "SUA", "phanquyen.TaiKhoan", tk.MaTaiKhoan.ToString(),
                    PhanQuyenService.Json(new { goc.TenDangNhap, goc.HoTenHienThi, goc.Email, goc.MaNhanVien, goc.DangHoatDong,
                                                VaiTro = goc.CacVaiTro.Select(v => v.MaVaiTroCode) }),
                    PhanQuyenService.Json(new { goc.TenDangNhap, tk.HoTenHienThi, tk.Email, tk.MaNhanVien, tk.DangHoatDong,
                                                VaiTro = vaiTroMoi.Select(v => v.MaVaiTroCode) }));
                return null;
            }, out loi);
        }

        // [12/11] Mot ho so nhan vien - mot tai khoan dang hoat dong (truoc day noi duoc 2 tai khoan cung 1 nguoi:
        // nhat ky / nguoi ban ve khong con biet ai lam). Tai khoan da vo hieu hoa khong tinh.
        private string? LoiNoiNhanVien(TaiKhoanLuuDTO tk, PhienGiaoDich gd)
        {
            if (tk.MaNhanVien is not int maNv || !tk.DangHoatDong) return null;
            string? khac = _repo.TaiKhoanKhacCuaNhanVien(maNv, tk.MaTaiKhoan, gd);
            return khac == null ? null
                : $"Hồ sơ nhân viên này đã nối với tài khoản \"{khac}\" đang hoạt động. " +
                  "Mỗi nhân viên chỉ có 1 tài khoản đang hoạt động — vô hiệu hóa tài khoản cũ trước, hoặc chọn hồ sơ khác.";
        }

        // Quan tri vien dat lai mat khau: nguoi dung phai doi o lan dang nhap ke tiep; mo khoa luon.
        public bool DatLaiMatKhau(int maTaiKhoan, string? matKhauMoi, string? nhapLai, out string loi)
        {
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "đặt lại mật khẩu", out loi)) return false;
            if (MatKhauHelper.KiemTraDoManh(matKhauMoi) is { } yeu) { loi = yeu; return false; }
            if (matKhauMoi != nhapLai) { loi = "Mật khẩu nhập lại không khớp."; return false; }

            return TrongGiaoDich(gd =>
            {
                var goc = _repo.LayDanhSachTaiKhoan(gd).FirstOrDefault(x => x.MaTaiKhoan == maTaiKhoan);
                if (goc == null) return "Tài khoản không còn trong CSDL. Hãy nạp lại.";
                _repo.CapNhatMatKhau(maTaiKhoan, MatKhauHelper.TaoChuoiBam(matKhauMoi!), true, gd);
                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "DAT_LAI_MAT_KHAU", "phanquyen.TaiKhoan", maTaiKhoan.ToString(),
                    PhanQuyenService.Json(new { goc.TenDangNhap, goc.DangBiKhoa }),
                    PhanQuyenService.Json(new { goc.TenDangNhap, PhaiDoiMatKhau = true, DangBiKhoa = false }));
                return null;
            }, out loi);
        }

        public bool MoKhoa(int maTaiKhoan, out string loi)
        {
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "mở khóa tài khoản", out loi)) return false;
            return TrongGiaoDich(gd =>
            {
                var goc = _repo.LayDanhSachTaiKhoan(gd).FirstOrDefault(x => x.MaTaiKhoan == maTaiKhoan);
                if (goc == null) return "Tài khoản không còn trong CSDL. Hãy nạp lại.";
                if (!goc.DangBiKhoa && goc.SoLanSaiLienTiep == 0) return $"Tài khoản \"{goc.TenDangNhap}\" không bị khóa.";
                _repo.MoKhoa(maTaiKhoan, gd);
                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "MO_KHOA", "phanquyen.TaiKhoan", maTaiKhoan.ToString(),
                    PhanQuyenService.Json(new { goc.TenDangNhap, goc.SoLanSaiLienTiep, goc.KhoaDenLuc }),
                    PhanQuyenService.Json(new { goc.TenDangNhap, SoLanSaiLienTiep = 0, KhoaDenLuc = (DateTime?)null }));
                return null;
            }, out loi);
        }

        // Ma tran vai tro x quyen: luu tap quyen moi cua mot vai tro. ADMIN luon du quyen -> khong sua.
        public bool LuuQuyenCuaVaiTro(int maVaiTro, IReadOnlyCollection<int> cacMaQuyen, out string loi)
        {
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.CauHinhHeThong, "sửa quyền của vai trò", out loi)) return false;
            return TrongGiaoDich(gd =>
            {
                var vt = _repo.LayDanhSachVaiTro(gd).FirstOrDefault(v => v.MaVaiTro == maVaiTro);
                if (vt == null) return "Vai trò không còn trong CSDL. Hãy nạp lại.";
                if (vt.MaVaiTroCode == VaiTroQuanTri) return "Vai trò Quản trị viên luôn có đủ mọi quyền, không sửa được.";

                var tatCa = _repo.LayDanhSachQuyen(gd);
                if (cacMaQuyen.Any(q => tatCa.All(x => x.MaQuyen != q))) return "Có quyền không còn trong CSDL. Hãy nạp lại.";
                var cu = _repo.LayVaiTroQuyen(gd).Where(x => x.MaVaiTro == maVaiTro).Select(x => x.MaQuyen).ToHashSet();
                if (cu.SetEquals(cacMaQuyen)) return "Không có thay đổi nào để lưu.";
                if (LoiCapQuyenHeThong(vt.MaVaiTroCode,
                        tatCa.Where(q => cu.Contains(q.MaQuyen)).Select(q => q.MaQuyenCode),
                        tatCa.Where(q => cacMaQuyen.Contains(q.MaQuyen)).Select(q => q.MaQuyenCode)) is { } loiHeThong)
                    return loiHeThong;

                _repo.ThayQuyenCuaVaiTro(maVaiTro, cacMaQuyen, gd);
                string Ma(IEnumerable<int> ds) => string.Join(", ", tatCa.Where(q => ds.Contains(q.MaQuyen)).Select(q => q.MaQuyenCode));
                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "SUA_QUYEN_VAI_TRO", "phanquyen.VaiTro_Quyen", maVaiTro.ToString(),
                    PhanQuyenService.Json(new { vt.MaVaiTroCode, Quyen = Ma(cu) }),
                    PhanQuyenService.Json(new { vt.MaVaiTroCode, Quyen = Ma(cacMaQuyen) }));
                return null;
            }, out loi);
        }

        // Quyen Quan tri he thong (SYS_CONFIG) chi danh cho vai tro ADMIN (Dat chot 09/11). Vai tro khac co SYS_CONFIG thi
        // tu gan ADMIN cho minh, dat lai mat khau admin, sua quyen vai tro cua chinh minh duoc -> khong cho cap them.
        // Vai tro khac lo co san (sua tay CSDL) van bo tick duoc; dang nhap cung bo qua (PhanQuyenService.QuyenHieuLuc).
        public static string? LoiCapQuyenHeThong(string maVaiTroCode, IEnumerable<string> maQuyenCu, IEnumerable<string> maQuyenMoi)
        {
            if (maVaiTroCode == VaiTroQuanTri) return null;
            bool coMoi = maQuyenMoi.Contains(MaQuyen.CauHinhHeThong);
            bool coCu = maQuyenCu.Contains(MaQuyen.CauHinhHeThong);
            return coMoi && !coCu
                ? "Quyền Quản trị hệ thống (SYS_CONFIG) chỉ dành cho vai trò Quản trị viên.\n" +
                  "Muốn ai quản trị hệ thống thì gán vai trò Quản trị viên cho tài khoản đó ở tab Tài khoản."
                : null;
        }

        // =====================================================================

        private static void ChuanHoa(TaiKhoanLuuDTO tk)
        {
            tk.TenDangNhap = (tk.TenDangNhap ?? "").Trim().ToLowerInvariant();
            tk.HoTenHienThi = Regex.Replace((tk.HoTenHienThi ?? "").Trim(), @"\s+", " ");
            tk.Email = string.IsNullOrWhiteSpace(tk.Email) ? null : tk.Email.Trim();
            tk.CacMaVaiTro = (tk.CacMaVaiTro ?? new()).Distinct().ToList();
        }

        private string? KiemTraVaiTro(List<int> cacMa, PhienGiaoDich gd)
        {
            var tatCa = _repo.LayDanhSachVaiTro(gd);
            if (cacMa.Any(m => tatCa.All(v => v.MaVaiTro != m))) return "Có vai trò không còn trong CSDL. Hãy nạp lại.";
            return null;
        }

        private List<string> TenVaiTro(List<int> cacMa, PhienGiaoDich gd)
            => _repo.LayDanhSachVaiTro(gd).Where(v => cacMa.Contains(v.MaVaiTro)).Select(v => v.MaVaiTroCode).ToList();

        private static object MoTa(TaiKhoanLuuDTO tk, List<string> vaiTro)
            => new { tk.TenDangNhap, tk.HoTenHienThi, tk.Email, tk.MaNhanVien, tk.DangHoatDong, PhaiDoiMatKhau = true, VaiTro = vaiTro };

        private static bool KiemTraKhoangNgay(BoLocNhatKy loc, out string loi)
        {
            loi = loc.DenNgay.Date < loc.TuNgay.Date ? "\"Đến ngày\" phải sau hoặc bằng \"Từ ngày\"." : "";
            return loi.Length == 0;
        }

        private static bool HopLe(List<LoiNhapLieuDTO> dsLoi, out string loi)
        {
            loi = dsLoi.Count == 0 ? "" : dsLoi.Count == 1 ? dsLoi[0].ThongDiep
                : string.Join("\n", dsLoi.Select(l => "• " + l.ThongDiep));
            return dsLoi.Count == 0;
        }

        // Chay ham ghi trong giao dich co khoa; ham tra ve cau loi (null = thanh cong -> COMMIT).
        private static bool TrongGiaoDich(Func<PhienGiaoDich, string?> lam, out string loi)
        {
            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(TaiNguyenKhoa);
                string? kq = lam(gd);
                if (kq != null) { loi = kq; return false; }
                gd.XacNhan();
                loi = "";
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        private static bool Thu(Action lam, out string loi)
        {
            try
            {
                lam();
                loi = "";
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }
    }
}
