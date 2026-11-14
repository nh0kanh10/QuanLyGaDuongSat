using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using DAL.Connection;
using DAL.Repositories;
using DTO.Common;
using DTO.QuanTri;

namespace BUS.Services
{
    // =========================================================================
    // Cua ngo kiem tra quyen + dang nhap / dang xuat / doi mat khau (Sprint 3, Dat).
    //
    // Moi noi goi qua Service nay thay vi goi thang PhienLamViec.
    // Quy tac Q10 (KE_HOACH_BACK_END.md §8): khong chi an nut tren giao dien -
    // Service phai kiem lai quyen truoc khi ghi CSDL.
    //
    // Dang nhap (Dat chot 08/11):
    //   - mat khau PBKDF2 (MatKhauHelper);
    //   - sai 5 lan lien tiep -> khoa 15 phut roi tu mo; quan tri vien mo tay duoc (QuanTriService);
    //   - ghi phanquyen.LichSuDangNhap MOI lan, ke ca sai ten / dang bi khoa;
    //   - dong tai khoan khoa UPDLOCK trong giao dich -> nhieu may dang nhap sai cung luc van dem dung.
    // =========================================================================
    public static class PhanQuyenService
    {
        public const int SoLanSaiToiDa = 5;
        public const int SoPhutKhoa = 15;

        private static readonly PhanQuyenRepository _repo = new();

        public static bool CoQuyen(string maQuyenCode) => PhienLamViec.CoQuyen(maQuyenCode);

        public static bool CoBatKyQuyen(params string[] cacMaQuyen)
            => PhienLamViec.CoBatKyQuyen(cacMaQuyen);

        // Goi ngay dau moi ham ghi CSDL co rang buoc quyen.
        // Thieu quyen => nem KhongDuQuyenException, GUI bat lai hien ThongBaoDialog.
        public static void BatBuocCoQuyen(string maQuyenCode, string? moTaHanhDong = null)
            => PhienLamViec.BatBuocCoQuyen(maQuyenCode, moTaHanhDong);

        // Kieu "bool + loi" dung trong cac Service: thieu quyen -> cau tieng Viet, khong nem.
        public static bool KiemTraQuyen(string maQuyenCode, string hanhDong, out string loi)
        {
            loi = CoQuyen(maQuyenCode) ? ""
                : $"Tài khoản \"{TenDangNhapHienTai}\" không có quyền {hanhDong} (cần quyền {maQuyenCode}).";
            return loi.Length == 0;
        }

        // Co 1 trong cac quyen la du (vd mac tau: ha tang HOAC lap bieu do chay tau).
        public static bool KiemTraBatKyQuyen(string[] cacMaQuyen, string hanhDong, out string loi)
        {
            loi = CoBatKyQuyen(cacMaQuyen) ? ""
                : $"Tài khoản \"{TenDangNhapHienTai}\" không có quyền {hanhDong} (cần một trong các quyền {string.Join(", ", cacMaQuyen)}).";
            return loi.Length == 0;
        }

        // [12/11] Mac tau + dinh muc van toc la mau bieu do chay tau: dieu do (TRAIN_SCHEDULE_EDIT) sua duoc, khong chi INFRA_EDIT.
        // Tab Mac tau hien voi ca 2 quyen (MainWindow.QuyenTheoTab) -> Service phai khop.
        public static readonly string[] QuyenSuaMacTau = { MaQuyen.SuaHaTang, MaQuyen.LapBieuDoChayTau };

        // Nguoi dang thao tac - dung cho cot NguoiTao / NguoiCapNhat (NVARCHAR).
        public static string TenDangNhapHienTai => PhienLamViec.TenDangNhap;

        // Dung cho cot NguoiDuyet / NguoiLap / NguoiKham / MaNhanVienBan (INT -> MaTaiKhoan).
        public static int MaTaiKhoanHienTai => PhienLamViec.MaTaiKhoan;

        // Dung khi bang tham chieu nhansu.NhanVien (vi du PhanCongKipLai).
        public static int? MaNhanVienHienTai => PhienLamViec.MaNhanVien;

        public static IReadOnlyCollection<string> CacQuyenHienTai => PhienLamViec.CacQuyen;

        // =====================================================================
        // DANG NHAP
        // =====================================================================
        public static KetQuaDangNhap DangNhap(string? tenDangNhap, string? matKhau)
        {
            string ten = (tenDangNhap ?? "").Trim();
            if (ten.Length == 0) return ThatBai("Vui lòng nhập tên đăng nhập.");
            if (string.IsNullOrEmpty(matKhau)) return ThatBai("Vui lòng nhập mật khẩu.");
            if (ten.Length > 50) return ThatBai("Tên đăng nhập hoặc mật khẩu không đúng.");

            string thietBi = MoTaThietBi();
            string? ip = DiaChiIPCucBo();

            try
            {
                using var gd = PhienGiaoDich.Mo();
                var tk = _repo.LayTaiKhoanDeDangNhap(ten, gd);

                // Sai ten: van ghi lich su (MaTaiKhoan NULL), cau bao chung chung nhu sai mat khau
                if (tk == null)
                {
                    _repo.GhiLichSuDangNhap(null, ten, false, "Không có tài khoản này", ip, thietBi, gd);
                    gd.XacNhan();
                    return ThatBai("Tên đăng nhập hoặc mật khẩu không đúng.");
                }

                if (!tk.DangHoatDong)
                {
                    // [12/11] Chi bao "vo hieu hoa" khi mat khau dung - sai mat khau thi bao chung chung nhu tai khoan
                    // khong ton tai (khong de lo tinh trang tai khoan cho nguoi doan mat khau).
                    bool dungMk = MatKhauHelper.LaDinhDangMoi(tk.MatKhauHash) && MatKhauHelper.KiemTra(matKhau, tk.MatKhauHash);
                    _repo.GhiLichSuDangNhap(tk.MaTaiKhoan, tk.TenDangNhap, false,
                        dungMk ? "Tài khoản đã bị vô hiệu hóa" : "Tài khoản đã bị vô hiệu hóa · sai mật khẩu", ip, thietBi, gd);
                    gd.XacNhan();
                    return ThatBai(dungMk ? "Tài khoản đã bị vô hiệu hóa. Liên hệ quản trị viên."
                                          : "Tên đăng nhập hoặc mật khẩu không đúng.");
                }

                if (tk.DangBiKhoa)
                {
                    DateTime denLuc = DoiSangGioMay(tk.KhoaDenLucUtc!.Value, tk.BayGioUtc);
                    int conPhut = Math.Max(1, (int)Math.Ceiling((tk.KhoaDenLucUtc.Value - tk.BayGioUtc).TotalMinutes));
                    _repo.GhiLichSuDangNhap(tk.MaTaiKhoan, tk.TenDangNhap, false, $"Đang bị khóa đến {denLuc:HH:mm}", ip, thietBi, gd);
                    gd.XacNhan();
                    return new KetQuaDangNhap
                    {
                        BiKhoa = true,
                        ThongBao = $"Tài khoản đang bị khóa do nhập sai mật khẩu {SoLanSaiToiDa} lần liên tiếp.\n" +
                                   $"Thử lại sau {denLuc:HH:mm} (còn khoảng {conPhut} phút) hoặc nhờ quản trị viên mở khóa."
                    };
                }

                if (!MatKhauHelper.LaDinhDangMoi(tk.MatKhauHash))
                {
                    _repo.GhiLichSuDangNhap(tk.MaTaiKhoan, tk.TenDangNhap, false, "Mật khẩu lưu theo định dạng cũ", ip, thietBi, gd);
                    gd.XacNhan();
                    return ThatBai("Tài khoản chưa có mật khẩu theo chuẩn mới. Nhờ quản trị viên đặt lại mật khẩu.");
                }

                if (!MatKhauHelper.KiemTra(matKhau, tk.MatKhauHash))
                {
                    // Khoa cu da het han -> dem lai tu dau
                    int soLan = (tk.KhoaDenLucUtc.HasValue ? 0 : tk.SoLanSaiLienTiep) + 1;
                    bool khoa = soLan >= SoLanSaiToiDa;
                    DateTime? denLuc = _repo.GhiSaiMatKhau(tk.MaTaiKhoan, soLan, khoa, SoPhutKhoa, gd);
                    _repo.GhiLichSuDangNhap(tk.MaTaiKhoan, tk.TenDangNhap, false,
                        khoa ? $"Sai mật khẩu lần {soLan}/{SoLanSaiToiDa} - khóa {SoPhutKhoa} phút"
                             : $"Sai mật khẩu lần {soLan}/{SoLanSaiToiDa}", ip, thietBi, gd);
                    if (khoa)
                        gd.GhiNhatKyHeThong(tk.MaTaiKhoan, "KHOA_TAI_KHOAN", "phanquyen.TaiKhoan", tk.MaTaiKhoan.ToString(),
                            null, Json(new { tk.TenDangNhap, LyDo = $"Sai mật khẩu {SoLanSaiToiDa} lần liên tiếp", KhoaDenLuc = denLuc }));
                    gd.XacNhan();

                    return khoa
                        ? new KetQuaDangNhap
                        {
                            BiKhoa = true, SoLanSai = soLan,
                            ThongBao = $"Sai mật khẩu {SoLanSaiToiDa} lần liên tiếp. Tài khoản bị khóa {SoPhutKhoa} phút" +
                                       (denLuc.HasValue ? $" (đến {denLuc:HH:mm})." : ".")
                        }
                        : new KetQuaDangNhap
                        {
                            SoLanSai = soLan,
                            ThongBao = "Tên đăng nhập hoặc mật khẩu không đúng.\n" +
                                       $"Còn {SoLanSaiToiDa - soLan} lần thử trước khi tài khoản bị khóa {SoPhutKhoa} phút."
                        };
                }

                var (vaiTro, quyenTho) = _repo.LayVaiTroVaQuyen(tk.MaTaiKhoan, gd);
                var quyen = QuyenHieuLuc(quyenTho, vaiTro.Select(v => v.MaVaiTroCode));
                if (vaiTro.Count == 0)
                {
                    _repo.GhiLichSuDangNhap(tk.MaTaiKhoan, tk.TenDangNhap, false, "Chưa được gán vai trò", ip, thietBi, gd);
                    gd.XacNhan();
                    return ThatBai("Tài khoản chưa được gán vai trò nào đang hoạt động. Liên hệ quản trị viên.");
                }
                if (quyen.Count == 0)
                {
                    string tenVt = string.Join(", ", vaiTro.Select(v => v.TenVaiTro));
                    _repo.GhiLichSuDangNhap(tk.MaTaiKhoan, tk.TenDangNhap, false, $"Vai trò chưa có quyền nào ({tenVt})", ip, thietBi, gd);
                    gd.XacNhan();
                    return ThatBai($"Vai trò \"{tenVt}\" của tài khoản chưa được cấp quyền nào. Liên hệ quản trị viên.");
                }

                if (tk.PhaiDoiMatKhau)
                {
                    // [12/11] Phai doi mat khau truoc khi vao: mat khau dung -> xoa dem sai, nhung CHUA ghi "dang nhap thanh cong".
                    // Doi xong (DoiMatKhau) moi ghi; huy (HuyDangNhapChuaDoiMatKhau) thi ghi that bai.
                    _repo.MoKhoa(tk.MaTaiKhoan, gd);
                    _choDoiMatKhau = (ip, thietBi);
                }
                else
                {
                    _repo.GhiDangNhapThanhCong(tk.MaTaiKhoan, gd);
                    _repo.GhiLichSuDangNhap(tk.MaTaiKhoan, tk.TenDangNhap, true, null, ip, thietBi, gd);
                    _choDoiMatKhau = null;
                }
                gd.XacNhan();

                PhienLamViec.DangNhap(tk.MaTaiKhoan, tk.TenDangNhap, tk.HoTenHienThi, tk.MaNhanVien,
                                      quyen, vaiTro.Select(v => v.TenVaiTro));
                return new KetQuaDangNhap
                {
                    ThanhCong = true,
                    PhaiDoiMatKhau = tk.PhaiDoiMatKhau,
                    CacVaiTro = vaiTro.Select(v => v.TenVaiTro).ToList(),
                    ThongBao = $"Xin chào {tk.HoTenHienThi}."
                };
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                if (CsdlChuaCapNhat(ex))
                    return ThatBai("CSDL chưa được cập nhật cho phần Đăng nhập (Sprint 3).\n" +
                                   @"Chạy database\tao_lai_csdl.cmd, hoặc chạy migration V20261108_1000 rồi seed S20261108_1100.");
                return ThatBai("Không đăng nhập được do lỗi kết nối CSDL:\n" + DichLoiSqlPhuongTien.ThongDiep(ex));
            }
        }

        // Dang nhap dung mat khau nhung bi buoc doi mat khau (quan tri vien vua dat lai): giu dia chi may de ghi lich su
        // khi nguoi dung doi xong / huy. null = khong co lan dang nhap nao dang cho.
        private static (string? Ip, string ThietBi)? _choDoiMatKhau;

        // Nguoi dung huy hop "doi mat khau bat buoc": phien chua tung mo -> ghi lich su that bai, khong ghi DANG_XUAT.
        public static void HuyDangNhapChuaDoiMatKhau()
        {
            if (_choDoiMatKhau is { } cho && PhienLamViec.DaDangNhapThat)
            {
                try
                {
                    using var gd = PhienGiaoDich.Mo();
                    _repo.GhiLichSuDangNhap(PhienLamViec.MaTaiKhoan, PhienLamViec.TenDangNhap, false,
                        "Không đổi mật khẩu bắt buộc", cho.Ip, cho.ThietBi, gd);
                    gd.XacNhan();
                }
                catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
                {
                    // mat ket noi: van huy phien
                }
            }
            _choDoiMatKhau = null;
            PhienLamViec.DangXuat();
        }

        // Ghi nhat ky roi ve phien chua dang nhap. Loi CSDL khi ghi nhat ky khong chan viec dang xuat.
        public static void DangXuat()
        {
            if (_choDoiMatKhau != null)
            {
                HuyDangNhapChuaDoiMatKhau();
                return;
            }
            if (PhienLamViec.DaDangNhapThat)
            {
                try
                {
                    using var gd = PhienGiaoDich.Mo();
                    gd.GhiNhatKyHeThong(PhienLamViec.MaTaiKhoan, "DANG_XUAT", "phanquyen.TaiKhoan",
                        PhienLamViec.MaTaiKhoan.ToString(), null,
                        Json(new { PhienLamViec.TenDangNhap, BatDau = PhienLamViec.ThoiDiemBatDau, KetThuc = DateTime.Now }));
                    gd.XacNhan();
                }
                catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
                {
                    // mat ket noi luc dang xuat: van dang xuat
                }
            }
            PhienLamViec.DangXuat();
        }

        // Nguoi dang dang nhap tu doi mat khau cua minh.
        public static bool DoiMatKhau(string? matKhauCu, string? matKhauMoi, string? nhapLai, out string loi)
            => DoiMatKhau(matKhauCu, matKhauMoi, nhapLai, out loi, out _);

        // biKhoa = true: nhap sai mat khau hien tai lan thu SoLanSaiToiDa -> tai khoan vua bi khoa, giao dien phai dang xuat.
        public static bool DoiMatKhau(string? matKhauCu, string? matKhauMoi, string? nhapLai, out string loi, out bool biKhoa)
        {
            loi = "";
            biKhoa = false;
            if (!PhienLamViec.DaDangNhapThat) { loi = "Chưa đăng nhập."; return false; }
            if (string.IsNullOrEmpty(matKhauCu)) { loi = "Chưa nhập mật khẩu hiện tại."; return false; }
            if (MatKhauHelper.KiemTraDoManh(matKhauMoi) is { } yeu) { loi = yeu; return false; }
            if (matKhauMoi != nhapLai) { loi = "Mật khẩu nhập lại không khớp."; return false; }
            if (matKhauMoi == matKhauCu) { loi = "Mật khẩu mới phải khác mật khẩu hiện tại."; return false; }

            try
            {
                using var gd = PhienGiaoDich.Mo();
                var tk = _repo.LayTaiKhoanDeDangNhap(PhienLamViec.TenDangNhap, gd);
                if (tk == null || tk.MaTaiKhoan != PhienLamViec.MaTaiKhoan) { loi = "Không tìm thấy tài khoản đang đăng nhập."; return false; }
                if (!MatKhauHelper.KiemTra(matKhauCu, tk.MatKhauHash))
                {
                    // [12/11] Sai mat khau hien tai cung tinh vao so lan sai nhu khi dang nhap (khong thi doan mat khau
                    // tu hop "Doi mat khau" cua phien dang mo ma khong bao gio bi khoa).
                    int soLan = (tk.KhoaDenLucUtc.HasValue ? 0 : tk.SoLanSaiLienTiep) + 1;
                    bool khoa = soLan >= SoLanSaiToiDa;
                    DateTime? denLuc = _repo.GhiSaiMatKhau(tk.MaTaiKhoan, soLan, khoa, SoPhutKhoa, gd);
                    _repo.GhiLichSuDangNhap(tk.MaTaiKhoan, tk.TenDangNhap, false,
                        $"Đổi mật khẩu: sai mật khẩu hiện tại lần {soLan}/{SoLanSaiToiDa}" + (khoa ? $" - khóa {SoPhutKhoa} phút" : ""),
                        DiaChiIPCucBo(), MoTaThietBi(), gd);
                    if (khoa)
                        gd.GhiNhatKyHeThong(tk.MaTaiKhoan, "KHOA_TAI_KHOAN", "phanquyen.TaiKhoan", tk.MaTaiKhoan.ToString(),
                            null, Json(new { tk.TenDangNhap, LyDo = $"Sai mật khẩu hiện tại {SoLanSaiToiDa} lần liên tiếp khi đổi mật khẩu", KhoaDenLuc = denLuc }));
                    gd.XacNhan();
                    biKhoa = khoa;
                    loi = khoa
                        ? $"Sai mật khẩu hiện tại {SoLanSaiToiDa} lần liên tiếp. Tài khoản bị khóa {SoPhutKhoa} phút" +
                          (denLuc.HasValue ? $" (đến {denLuc:HH:mm})" : "") + " — hệ thống sẽ đăng xuất."
                        : $"Mật khẩu hiện tại không đúng. Còn {SoLanSaiToiDa - soLan} lần thử trước khi tài khoản bị khóa {SoPhutKhoa} phút.";
                    return false;
                }

                _repo.CapNhatMatKhau(tk.MaTaiKhoan, MatKhauHelper.TaoChuoiBam(matKhauMoi!), false, gd);
                gd.GhiNhatKyHeThong(tk.MaTaiKhoan, "DOI_MAT_KHAU", "phanquyen.TaiKhoan", tk.MaTaiKhoan.ToString(),
                    null, Json(new { tk.TenDangNhap, PhaiDoiMatKhau = false }));
                if (_choDoiMatKhau is { } cho)
                {
                    // Lan dang nhap dang cho doi mat khau bat buoc: gio moi tinh la dang nhap thanh cong
                    _repo.GhiDangNhapThanhCong(tk.MaTaiKhoan, gd);
                    _repo.GhiLichSuDangNhap(tk.MaTaiKhoan, tk.TenDangNhap, true, null, cho.Ip, cho.ThietBi, gd);
                }
                gd.XacNhan();
                _choDoiMatKhau = null;
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // =====================================================================

        // Quyen thuc su cua phien: SYS_CONFIG chi co hieu luc khi tai khoan giu vai tro ADMIN dang kich hoat
        // (QuanTriService.LoiCapQuyenHeThong) - vai tro khac lo co SYS_CONFIG trong CSDL cung khong vao duoc trang Quan tri.
        public static List<string> QuyenHieuLuc(IEnumerable<string> cacQuyen, IEnumerable<string> cacMaVaiTro)
        {
            bool laQuanTri = cacMaVaiTro.Contains(QuanTriService.VaiTroQuanTri);
            return cacQuyen.Where(q => laQuanTri || q != MaQuyen.CauHinhHeThong).Distinct().ToList();
        }

        private static KetQuaDangNhap ThatBai(string thongBao) => new() { ThongBao = thongBao };

        // Loi 207 / 208 (thieu cot / bang): CSDL cu chua chay migration Sprint 3
        private static bool CsdlChuaCapNhat(Exception ex)
            => ex is System.Data.Common.DbException
               && (ex.Message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase)
                   || ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase));

        private static DateTime DoiSangGioMay(DateTime utc, DateTime bayGioUtc)
            => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

        private static string MoTaThietBi()
        {
            try { return $"{Environment.MachineName} / {Environment.UserName} / {Environment.OSVersion.VersionString}"; }
            catch { return Environment.MachineName; }
        }

        private static string? DiaChiIPCucBo()
        {
            try
            {
                return Dns.GetHostAddresses(Dns.GetHostName())
                    .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))?.ToString();
            }
            catch (SocketException)
            {
                return null;
            }
        }

        internal static string Json(object o) => JsonSerializer.Serialize(o, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}
