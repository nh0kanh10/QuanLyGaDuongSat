using System.Text.Json;
using System.Text.RegularExpressions;
using DAL.Connection;
using DAL.Repositories;
using DTO.Common;
using DTO.NhanSu;
using DTO.PhuongTien;

namespace BUS.Services
{
    // =========================================================================
    // NHAN SU, TO TAU & PHAN CONG CA TRUC - KE_HOACH_BACK_END.md §4.4
    //
    // Doc : LayDuLieu -> anh chup du lieu (DuLieuNhanSu) cho ca trang, luat chay trong bo nho
    //       (DanhGiaKipLai) nhu giai doan giao dien.
    // Ghi : moi thao tac MOT giao dich:
    //         quyen -> mo giao dich -> khoa ung dung -> nap lai anh chup (gio may chu)
    //         -> kiem phien ban (ROWVERSION) -> chay lai luat -> ghi -> nhat ky -> COMMIT.
    //       Ghi kip: truoc khi ghi doi chieu trung gio tren toan bang (KiemTraTrungGioToanBang,
    //       cung dieu kien trigger) vi anh chup chi gom chuyen trong cua so ngay.
    //       Khoa "nhansu.PhanCong" tuan tu hoa moi thao tac lien quan kip (phan cong, kiem tra,
    //       nhan ban, ban giao, sua ho so): R4 nghi 8 gio, R6, R7, R8 khong co trong CSDL nen
    //       2 nguoi ghi cung luc se lot neu khong khoa. Thu tu lay khoa: PhanCong -> MaNhanVien.
    //
    // Quyet dinh 11/10 (Dat chot theo khuyen nghi):
    //   Q1 PhienBan ROWVERSION · Q2 trigger xet ca truong tau · Q3 gio Viet Nam (SYSDATETIME)
    //   Q4 phieu kiem tra chi ghi tu 3 gio truoc gio nhan ban · Q5 ma NV = MAX + 1 cua tien to
    //   Q7 "Dang lam nhiem vu" do nhan ban dat, khong chon tay.
    // 12/10: chuc danh + don vi chon theo danh muc (DanhMucDonVi); doi chuc danh / hang bang / don vi
    //   co dieu kien (KiemTraDoiViTri); doi ma thi ghi nhansu.LichSuMaNhanVien, ma cu khong cap lai.
    // =========================================================================
    public record KetQuaLuuNhanVien(int MaNhanVien, string MaNVCode, string? MaCu, bool KhongThayDoi);

    public class NhanSuService
    {
        public const string KhoaPhanCong = "nhansu.PhanCong";
        public const string KhoaMaNhanVien = "nhansu.MaNhanVien";

        // Cua so du lieu: tu 00:00 hom qua toi het ngay thu 7 sau hom nay
        public const int SoNgayXemTruoc = 1;
        public const int SoNgayXemSau = 8;

        private static readonly Regex SoDienThoaiHopLe = new(@"^0\d{9,10}$");

        // Q7: trang thai chon tay trong ho so
        public static readonly string[] TrangThaiChonTay =
            { NhanVienHienThi.SanSang, NhanVienHienThi.NghiNgoi, NhanVienHienThi.DaNghiViec };

        private readonly NhanSuRepository _repo = new();

        public static (DateTime Tu, DateTime Den) CuaSo(DateTime bayGio)
            => (bayGio.Date.AddDays(-SoNgayXemTruoc), bayGio.Date.AddDays(SoNgayXemSau));

        // =====================================================================
        // DOC
        // =====================================================================

        // Khong ket noi duoc: tra anh chup rong + thong diep (man hinh van mo duoc)
        public bool LayDuLieu(out DuLieuNhanSu duLieu, out string loi)
        {
            loi = "";
            DateTime bayGio = DateTime.Now;
            try
            {
                var (tu, den) = CuaSo(bayGio);
                duLieu = _repo.LayDuLieu(tu, den, bayGio);
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                duLieu = DuLieuNhanSu.Rong(bayGio);
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // Ma du kien hien trong dialog (ma that cap khi luu - co the khac neu nguoi khac vua them)
        public static string MaDuKien(string chucDanh, DuLieuNhanSu dl)
            => MaNhanVienTheoChucDanh.MaKeTiep(chucDanh, dl.MaDaCap);

        // =====================================================================
        // HO SO NHAN VIEN
        // =====================================================================

        // Kiem tra tung truong (dialog dung chung de bao loi canh o nhap). Truong = ten cot.
        // ga: danh muc ga (don vi "Ga ..." cua nhan vien ban ve).
        public static List<LoiNhapLieuDTO> KiemTraNhanVien(NhanVienHienThi nv, IReadOnlyCollection<string> hangBangHopLe,
                                                          IReadOnlyList<GaNhanSu> ga)
        {
            var ds = new List<LoiNhapLieuDTO>();

            string hoTen = nv.HoTen?.Trim() ?? "";
            if (hoTen.Length == 0) ds.Add(new("HoTen", "Vui lòng nhập họ và tên."));
            else if (hoTen.Length > 100) ds.Add(new("HoTen", "Họ và tên tối đa 100 ký tự."));

            string sdt = nv.SoDienThoai?.Trim() ?? "";
            if (sdt.Length == 0) ds.Add(new("SoDienThoai", "Vui lòng nhập số điện thoại."));
            else if (!SoDienThoaiHopLe.IsMatch(sdt)) ds.Add(new("SoDienThoai", "Số điện thoại gồm 10–11 chữ số, bắt đầu bằng 0."));

            if (nv.HanKhamSucKhoe.Year is < 2000 or > 2100)
                ds.Add(new("HanKhamSucKhoe", "Vui lòng chọn hạn khám sức khỏe."));

            string chucDanh = nv.ChucDanh?.Trim() ?? "";
            // 12/10: chuc danh chon trong danh muc (khong go tu do) - quy tac kip, ma NV, don vi deu theo danh muc
            if (chucDanh.Length == 0) ds.Add(new("ChucDanh", "Vui lòng chọn chức danh."));
            else if (!QuyTacKipLai.LaChucDanhTrongDanhMuc(chucDanh))
                ds.Add(new("ChucDanh", $"Chức danh \"{chucDanh}\" không có trong danh mục, hãy chọn lại."));

            // HangBangLai: bat buoc voi ban lai may (CSDL cho NULL), phai la dong dau may co that
            if (QuyTacKipLai.LaChucDanhBanLai(chucDanh))
            {
                if (string.IsNullOrEmpty(nv.HangBangLai))
                    ds.Add(new("HangBangLai", $"{chucDanh} phải có hạng bằng lái (dòng đầu máy được lái)."));
                else if (hangBangHopLe.Count > 0 && !hangBangHopLe.Contains(nv.HangBangLai))
                    ds.Add(new("HangBangLai", $"Hạng bằng {nv.HangBangLai} không phải dòng đầu máy có trong danh mục."));
            }
            else if (!string.IsNullOrEmpty(nv.HangBangLai))
            {
                ds.Add(new("HangBangLai", "Chỉ lái tàu và phụ lái mới có hạng bằng lái."));
            }

            // 12/10: don vi phai dung loai cua chuc danh (DanhMucDonVi)
            string donVi = nv.DonViChuQuan?.Trim() ?? "";
            if (donVi.Length == 0) ds.Add(new("DonViChuQuan", "Vui lòng chọn đơn vị chủ quản."));
            else if (QuyTacKipLai.LaChucDanhTrongDanhMuc(chucDanh) && !DanhMucDonVi.HopLe(chucDanh, donVi, ga))
                ds.Add(new("DonViChuQuan", $"{chucDanh} phải thuộc {DanhMucDonVi.LoaiDonVi(chucDanh)}, " +
                                           $"không thuộc \"{donVi}\". Hãy chọn lại đơn vị."));

            if (nv.TrangThai is not (NhanVienHienThi.SanSang or NhanVienHienThi.DangLam
                                     or NhanVienHienThi.NghiNgoi or NhanVienHienThi.DaNghiViec))
                ds.Add(new("TrangThai", "Trạng thái không hợp lệ."));

            return ds;
        }

        // Q7 + R13: "Dang lam nhiem vu" chi do nhan ban dat; dang trong ca thi khong doi trang thai.
        // goc == null: them moi. null = hop le.
        public static string? KiemTraTrangThai(NhanVienHienThi? goc, string trangThaiMoi, DuLieuNhanSu dl)
        {
            var dangChay = goc == null ? null
                : dl.KipCuaNhanVien(goc.MaNhanVien).FirstOrDefault(p => p.TrangThai == PhanCongKipHienThi.DangThucHien);

            if (dangChay != null)
                return trangThaiMoi == NhanVienHienThi.DangLam ? null
                    : $"Đang trong ca {dangChay.Chuyen.SoHieuMacTau} ({dangChay.Chang}). Xác nhận bàn giao trước khi đổi trạng thái.";

            if (trangThaiMoi == NhanVienHienThi.DangLam && goc?.TrangThai != NhanVienHienThi.DangLam)
                return "\"Đang làm nhiệm vụ\" do xác nhận nhận ban đặt, không chọn tay.";

            if (goc == null && trangThaiMoi == NhanVienHienThi.DaNghiViec)
                return "Hồ sơ mới không đặt trạng thái \"Đã nghỉ việc\".";

            return null;
        }

        // Dat chot 12/10 - doi "vi tri" (chuc danh, hang bang, don vi la QUYET DINH cua quan ly):
        //   - dang trong ca (kip dang thuc hien) -> khong doi gi trong 3 truong nay, doi sau khi ban giao;
        //   - doi chuc danh / hang bang lam nguoi nay sai vi tri (R2) hoac sai dong may (R7) o kip chua
        //     nhan ban -> chan, phai thay nguoi o cac kip do truoc (khong de ca do tu sinh ra).
        // Han kham, nghi viec la SU THAT -> van cho luu + canh bao kip anh huong (dialog). Rong = hop le.
        public static List<LoiNhapLieuDTO> KiemTraDoiViTri(NhanVienHienThi goc, NhanVienHienThi moi, DuLieuNhanSu dl)
        {
            var ds = new List<LoiNhapLieuDTO>();
            bool doiChucDanh = !string.Equals(goc.ChucDanh?.Trim(), moi.ChucDanh?.Trim(), StringComparison.Ordinal);
            bool doiBang = !string.Equals(goc.HangBangLai ?? "", moi.HangBangLai ?? "", StringComparison.Ordinal);
            bool doiDonVi = !string.Equals(goc.DonViChuQuan?.Trim(), moi.DonViChuQuan?.Trim(), StringComparison.Ordinal);
            if (!doiChucDanh && !doiBang && !doiDonVi) return ds;

            var dangChay = dl.KipCuaNhanVien(goc.MaNhanVien).FirstOrDefault(p => p.TrangThai == PhanCongKipHienThi.DangThucHien);
            if (dangChay != null)
            {
                string ca = $"Đang trong ca {dangChay.Chuyen.SoHieuMacTau} ({dangChay.Chang})";
                if (doiChucDanh) ds.Add(new("ChucDanh", $"{ca}: đổi chức danh sau khi bàn giao."));
                if (doiBang) ds.Add(new("HangBangLai", $"{ca}: đổi hạng bằng sau khi bàn giao."));
                if (doiDonVi) ds.Add(new("DonViChuQuan", $"{ca}: đổi đơn vị sau khi bàn giao."));
                return ds;
            }

            if (!doiChucDanh && !doiBang) return ds;
            var hong = new List<string>();
            foreach (var p in dl.KipCuaNhanVien(goc.MaNhanVien)
                                .Where(p => p.TrangThai == PhanCongKipHienThi.DaPhanCong && p.Chuyen.ConHieuLuc)
                                .OrderBy(p => p.GioNhanBan))
            {
                string vaiTro = p.VaiTroCua(goc.MaNhanVien)!;
                bool LoiViTri(NhanVienHienThi nv, string dieuKien) =>
                    DanhGiaKipLai.DoiChieu(dl, nv, vaiTro, p.Chuyen, p.MaGaNhanBan, p.MaGaBanGiao, p.MaPhanCong)
                                 .Any(d => d.TenDieuKien == dieuKien && d.LaLoi);
                bool moiHong = new[] { DieuKienKip.ChucDanh, DieuKienKip.BangLai }
                    .Any(dk => LoiViTri(moi, dk) && !LoiViTri(goc, dk));
                if (moiHong)
                    hong.Add($"{p.Chuyen.SoHieuMacTau} {p.Chuyen.NgayXuatPhat:dd/MM} {p.Chang} ({QuyTacKipLai.TenVaiTro(vaiTro).ToLower()})");
            }
            if (hong.Count > 0)
                ds.Add(new(doiChucDanh ? "ChucDanh" : "HangBangLai",
                           $"Còn {hong.Count} kíp chưa nhận ban sẽ không còn đúng vị trí: {string.Join("; ", hong)}. " +
                           $"Thay người ở các kíp này (tab Phân công kíp lái) rồi mới đổi {(doiChucDanh ? "chức danh" : "hạng bằng")}."));
            return ds;
        }

        public bool ThemNhanVien(NhanVienHienThi nv, out string loi, out KetQuaLuuNhanVien? ketQua)
        {
            loi = "";
            ketQua = null;
            if (!CoQuyen(MaQuyen.QuanLyHoSoNhanVien, "thêm hồ sơ nhân viên", out loi)) return false;

            nv = ChuanHoa(nv);
            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(KhoaMaNhanVien);

                var dsLoi = KiemTraNhanVien(nv, _repo.LayHangBangLai(gd), _repo.LayGa(gd));
                string? loiTrangThai = KiemTraTrangThai(null, nv.TrangThai, DuLieuNhanSu.Rong(DateTime.Now));
                if (dsLoi.Count > 0 || loiTrangThai != null)
                {
                    loi = GopLoi(dsLoi.Select(l => l.ThongDiep).Append(loiTrangThai));
                    return false;
                }

                nv.MaNVCode = CapMa(nv.ChucDanh, gd);
                nv.MaNhanVien = _repo.ThemNhanVien(nv, gd);

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "THEM", "nhansu.NhanVien",
                                    nv.MaNhanVien.ToString(), null, MoTaJson(nv));
                gd.XacNhan();

                ketQua = new KetQuaLuuNhanVien(nv.MaNhanVien, nv.MaNVCode, null, false);
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // moi.PhienBan = phien ban luc mo dialog. Doi chuc danh -> cap ma moi theo tien to.
        public bool CapNhatNhanVien(NhanVienHienThi moi, out string loi, out KetQuaLuuNhanVien? ketQua)
        {
            loi = "";
            ketQua = null;
            if (!CoQuyen(MaQuyen.QuanLyHoSoNhanVien, "sửa hồ sơ nhân viên", out loi)) return false;

            moi = ChuanHoa(moi);
            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(KhoaPhanCong);
                gd.KhoaTaiNguyen(KhoaMaNhanVien);
                var dl = NapTrongGiaoDich(gd);

                var goc = dl.TimNhanVien(moi.MaNhanVien);
                if (goc == null)
                {
                    loi = "Không tìm thấy hồ sơ nhân viên (có thể đã bị xoá). Hãy nạp lại danh sách.";
                    return false;
                }
                if (!CungPhienBan(moi.PhienBan, goc.PhienBan))
                {
                    loi = $"Hồ sơ {goc.MaNVCode} — {goc.HoTen} vừa được thay đổi sau khi bạn mở (người khác sửa, " +
                          "hoặc kíp vừa nhận ban / bàn giao). Hãy nạp lại rồi sửa lại.";
                    return false;
                }

                var dsLoi = KiemTraNhanVien(moi, dl.HangBangLai, dl.Ga);
                dsLoi.AddRange(KiemTraDoiViTri(goc, moi, dl));
                string? loiTrangThai = KiemTraTrangThai(goc, moi.TrangThai, dl);
                if (dsLoi.Count > 0 || loiTrangThai != null)
                {
                    loi = GopLoi(dsLoi.Select(l => l.ThongDiep).Append(loiTrangThai));
                    return false;
                }

                // Ma: giu neu con dung tien to cua chuc danh, khong thi cap ma moi
                moi.MaNVCode = MaNhanVienTheoChucDanh.KhopChucDanh(goc.MaNVCode, moi.ChucDanh)
                    ? goc.MaNVCode
                    : CapMa(moi.ChucDanh, gd);

                if (GiongNhau(goc, moi))
                {
                    ketQua = new KetQuaLuuNhanVien(goc.MaNhanVien, goc.MaNVCode, null, true);
                    return true;
                }

                if (_repo.CapNhatNhanVien(moi, goc.PhienBan!, gd) == 0)
                {
                    loi = $"Hồ sơ {goc.MaNVCode} vừa được người khác sửa. Hãy nạp lại rồi sửa lại.";
                    return false;
                }
                // Doi ma -> giu ma cu de tra cuu, khong cap lai (migration V20261012_1100)
                if (moi.MaNVCode != goc.MaNVCode)
                    _repo.ThemLichSuMa(goc, moi, PhanQuyenService.MaTaiKhoanHienTai, gd);

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "SUA", "nhansu.NhanVien",
                                    goc.MaNhanVien.ToString(), MoTaJson(goc), MoTaJson(moi));
                gd.XacNhan();

                ketQua = new KetQuaLuuNhanVien(goc.MaNhanVien, moi.MaNVCode,
                                               moi.MaNVCode != goc.MaNVCode ? goc.MaNVCode : null, false);
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // =====================================================================
        // PHAN CONG KIP LAI
        // =====================================================================

        // Them (MaPhanCong <= 0) hoac sua kip. pc.PhienBan = phien ban luc mo dialog (khi sua).
        public bool LuuKip(PhanCongKipHienThi pc, out string loi, out int maPhanCong)
        {
            loi = "";
            maPhanCong = pc.MaPhanCong;
            if (!CoQuyen(MaQuyen.PhanCongKipLai, "phân công kíp lái", out loi)) return false;

            if (pc.MaLaiTau <= 0 || pc.MaPhuLai <= 0 || pc.MaTruongTau <= 0)
            {
                loi = "Kíp phải đủ lái tàu, phụ lái và trưởng tàu.";
                return false;
            }
            if (pc.MaLaiTau == pc.MaPhuLai || pc.MaLaiTau == pc.MaTruongTau || pc.MaPhuLai == pc.MaTruongTau)
            {
                loi = "Lái tàu, phụ lái và trưởng tàu phải là ba người khác nhau.";
                return false;
            }

            bool laMoi = pc.MaPhanCong <= 0;
            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(KhoaPhanCong);
                var dl = NapTrongGiaoDich(gd);

                var chuyen = dl.TimChuyen(pc.MaChuyenTau);
                if (chuyen == null)
                {
                    loi = "Chuyến tàu không còn trong danh sách đang điều hành. Hãy nạp lại.";
                    return false;
                }
                if (!chuyen.ConHieuLuc)
                {
                    loi = $"Chuyến {chuyen.SoHieuMacTau} ngày {chuyen.NgayXuatPhat:dd/MM/yyyy} đã {chuyen.NhanTrangThai.ToLower()}, không phân công kíp.";
                    return false;
                }

                PhanCongKipHienThi? goc = null;
                if (!laMoi)
                {
                    goc = dl.TimKip(pc.MaPhanCong);
                    string? loiGoc = KiemTraKipDangMo(goc, pc.PhienBan, PhanCongKipHienThi.DaPhanCong, "sửa");
                    if (loiGoc != null) { loi = loiGoc; return false; }
                    if (goc!.MaChuyenTau != pc.MaChuyenTau)
                    {
                        loi = "Không đổi chuyến tàu của kíp đã phân công.";
                        return false;
                    }
                }

                // Luat: chang (R11) + tung vi tri (R2 R3 R4 R6 R7 R8 R9) - cung ham voi o chon nguoi
                var dsLoi = new List<string>();
                string? loiChang = DanhGiaKipLai.KiemTraChang(dl, chuyen, pc.MaGaNhanBan, pc.MaGaBanGiao, goc?.MaPhanCong);
                if (loiChang != null) dsLoi.Add(loiChang);

                foreach (string vaiTro in QuyTacKipLai.CacVaiTro)
                {
                    var nv = dl.TimNhanVien(pc.MaNhanVienTheoVaiTro(vaiTro));
                    if (nv == null)
                    {
                        dsLoi.Add($"{QuyTacKipLai.TenVaiTro(vaiTro)}: không tìm thấy nhân viên.");
                        continue;
                    }
                    if (loiChang != null) continue;   // chang sai thi doi chieu gio khong co nghia

                    foreach (var dk in DanhGiaKipLai.DoiChieu(dl, nv, vaiTro, chuyen, pc.MaGaNhanBan, pc.MaGaBanGiao, goc?.MaPhanCong)
                                                    .Where(d => d.LaLoi))
                        dsLoi.Add($"{QuyTacKipLai.TenVaiTro(vaiTro)} {nv.HoTen}: {dk.MoTa}");
                }

                if (dsLoi.Count > 0)
                {
                    loi = "Chưa lưu được kíp:\n\n" + string.Join("\n", dsLoi.Select(l => "• " + l));
                    return false;
                }

                if (goc != null && goc.MaLaiTau == pc.MaLaiTau && goc.MaPhuLai == pc.MaPhuLai && goc.MaTruongTau == pc.MaTruongTau
                    && goc.MaGaNhanBan == pc.MaGaNhanBan && goc.MaGaBanGiao == pc.MaGaBanGiao)
                    return true;   // khong doi gi

                string? trungGio = KiemTraTrungGioToanBang(pc, gd);
                if (trungGio != null)
                {
                    loi = "Chưa lưu được kíp:\n\n" + trungGio;
                    return false;
                }

                if (laMoi)
                {
                    maPhanCong = _repo.ThemKip(pc, gd);
                    pc.MaPhanCong = maPhanCong;
                }
                else if (_repo.CapNhatKip(pc, goc!.PhienBan!, gd) == 0)
                {
                    loi = "Kíp vừa được người khác sửa / nhận ban. Hãy nạp lại rồi sửa lại.";
                    return false;
                }

                pc.Chuyen = chuyen;
                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, laMoi ? "PHAN_CONG_KIP" : "SUA_KIP",
                                    "nhansu.PhanCongKipLai", maPhanCong.ToString(),
                                    goc == null ? null : MoTaJson(goc, dl), MoTaJson(pc, dl));
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        public bool HuyKip(int maPhanCong, byte[]? phienBan, out string loi)
        {
            loi = "";
            if (!CoQuyen(MaQuyen.PhanCongKipLai, "hủy phân công kíp lái", out loi)) return false;

            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(KhoaPhanCong);
                var dl = NapTrongGiaoDich(gd);

                var goc = dl.TimKip(maPhanCong);
                string? loiGoc = KiemTraKipDangMo(goc, phienBan, PhanCongKipHienThi.DaPhanCong, "hủy");
                if (loiGoc != null) { loi = loiGoc; return false; }

                if (_repo.XoaKip(maPhanCong, goc!.PhienBan!, gd) == 0)
                {
                    loi = "Kíp vừa được người khác sửa / nhận ban. Hãy nạp lại.";
                    return false;
                }

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "HUY_KIP", "nhansu.PhanCongKipLai",
                                    maPhanCong.ToString(), MoTaJson(goc, dl), null);
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // =====================================================================
        // KIEM TRA LEN BAN -> NHAN BAN -> BAN GIAO
        // =====================================================================

        // Moc som nhat duoc ghi phieu cho kip (Q4)
        public static DateTime GhiKiemTraTuLuc(PhanCongKipHienThi kip)
            => kip.GioNhanBan.AddHours(-QuyTacKipLai.SoGioDoSomNhat);

        // Kiem tra so lieu mot phieu theo kieu cot. null = hop le.
        public static string? KiemTraSoLieu(decimal nongDoCon, decimal soGioNghi)
        {
            if (nongDoCon < 0 || nongDoCon > QuyTacKipLai.NongDoConGioiHan)
                return $"nồng độ cồn phải từ 0 đến {QuyTacKipLai.NongDoConGioiHan:0.00}";
            if (decimal.Round(nongDoCon, 2) != nongDoCon) return "nồng độ cồn chỉ ghi 2 chữ số thập phân";
            if (soGioNghi < 0 || soGioNghi > QuyTacKipLai.SoGioNghiGioiHan)
                return $"giờ nghỉ phải từ 0 đến {QuyTacKipLai.SoGioNghiGioiHan:0.0}";
            if (decimal.Round(soGioNghi, 1) != soGioNghi) return "giờ nghỉ chỉ ghi 1 chữ số thập phân";
            return null;
        }

        // Ghi ket qua kiem tra cho cac thanh vien cua kip (MaNhanVien, NongDoConMgL, SoGioNghiTruocCa).
        // Thoi diem kiem tra = gio may chu luc ghi.
        public bool GhiKiemTra(int maPhanCong, IReadOnlyList<KiemTraLenBanHienThi> ketQua, out string loi)
        {
            loi = "";
            if (!CoQuyen(MaQuyen.NhanBanKipLai, "ghi kết quả kiểm tra lên ban", out loi)) return false;
            if (ketQua.Count == 0)
            {
                loi = "Chưa có kết quả nào để ghi.";
                return false;
            }

            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(KhoaPhanCong);
                var dl = NapTrongGiaoDich(gd);

                var kip = dl.TimKip(maPhanCong);
                if (kip == null)
                {
                    loi = "Không tìm thấy kíp (có thể vừa bị hủy). Hãy nạp lại.";
                    return false;
                }
                if (kip.TrangThai != PhanCongKipHienThi.DaPhanCong)
                {
                    loi = $"Kíp đã {kip.NhanTrangThai.ToLower()}, không ghi thêm kết quả kiểm tra lên ban.";
                    return false;
                }

                DateTime tuLuc = GhiKiemTraTuLuc(kip);
                if (dl.BayGio < tuLuc)
                {
                    loi = $"Chỉ ghi kết quả kiểm tra trong vòng {QuyTacKipLai.SoGioDoSomNhat} giờ trước giờ nhận ban. " +
                          $"Kíp nhận ban {kip.GioNhanBan:HH:mm dd/MM} tại {kip.TenGaNhanBan}, ghi được từ {tuLuc:HH:mm dd/MM}.";
                    return false;
                }

                var dsLoi = new List<string>();
                var daGap = new HashSet<int>();
                foreach (var kt in ketQua)
                {
                    var nv = dl.TimNhanVien(kt.MaNhanVien);
                    string ten = nv?.HoTen ?? $"nhân viên #{kt.MaNhanVien}";
                    if (nv == null || !kip.CoNhanVien(kt.MaNhanVien))
                        dsLoi.Add($"{ten}: không thuộc kíp này");
                    else if (!daGap.Add(kt.MaNhanVien))
                        dsLoi.Add($"{ten}: ghi hai lần trong cùng một lượt");
                    else if (!dl.KetQuaKiemTra(kt.MaNhanVien, kip.MaChuyenTau).ChuaKiemTra)
                        dsLoi.Add($"{ten}: đã có kết quả kiểm tra cho chuyến này (người không đạt phải thay, không đo lại để lên ban)");
                    else if (KiemTraSoLieu(kt.NongDoConMgL, kt.SoGioNghiTruocCa) is { } loiSo)
                        dsLoi.Add($"{ten}: {loiSo}");
                }
                if (dsLoi.Count > 0)
                {
                    loi = "Chưa ghi được kết quả kiểm tra:\n\n" + string.Join("\n", dsLoi.Select(l => "• " + l));
                    return false;
                }

                foreach (var kt in ketQua)
                {
                    var (ma, thoiDiem) = _repo.ThemKiemTra(kt.MaNhanVien, kip.MaChuyenTau, kt.NongDoConMgL, kt.SoGioNghiTruocCa, gd);
                    kt.MaKiemTra = ma;
                    kt.ThoiDiemKiemTra = thoiDiem;
                    kt.MaChuyenTau = kip.MaChuyenTau;

                    var nv = dl.TimNhanVien(kt.MaNhanVien)!;
                    gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "GHI_KIEM_TRA", "nhansu.KiemTraSucKhoe", ma.ToString(), null,
                        JsonSerializer.Serialize(new
                        {
                            MaKiemTra = ma, nv.MaNVCode, nv.HoTen, MacTau = kip.Chuyen.SoHieuMacTau, kip.MaChuyenTau, kip.MaPhanCong,
                            kt.NongDoConMgL, kt.SoGioNghiTruocCa, kt.DuDieuKien, ThoiDiem = thoiDiem
                        }));
                }

                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // R6 R12 R13: kip du 3 nguoi dat, khong con loi phan cong, kip truoc cua chuyen da nhan ban
        // -> kip DANG_THUC_HIEN, 3 nguoi DANG_LAM
        public bool NhanBan(int maPhanCong, byte[]? phienBan, out string loi)
        {
            loi = "";
            if (!CoQuyen(MaQuyen.NhanBanKipLai, "xác nhận nhận ban", out loi)) return false;

            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(KhoaPhanCong);
                var dl = NapTrongGiaoDich(gd);

                var kip = dl.TimKip(maPhanCong);
                string? loiGoc = KiemTraKipDangMo(kip, phienBan, PhanCongKipHienThi.DaPhanCong, "nhận ban");
                if (loiGoc != null) { loi = loiGoc; return false; }

                var dong = DanhGiaKipLai.DanhGiaKip(dl, kip!, dl.SoThuTuKip(kip!));
                if (!dong.CoTheNhanBan)
                {
                    var lyDo = dong.VanDe.Where(v => v.MucDo == MucDoVanDe.NghiemTrong).Select(v => v.NoiDung)
                        .Concat(dong.ThanhVien.Where(t => t.KiemTra.ChuaKiemTra).Select(t => $"{t.TenVaiTro} {t.HoTen}: chưa kiểm tra lên ban"))
                        .Concat(dong.ThanhVien.Where(t => t.KiemTra.KhongDat).Select(t => $"{t.TenVaiTro} {t.HoTen}: không đạt kiểm tra lên ban"))
                        .Distinct();
                    loi = $"{dong.NhanChuyen} · {dong.TenKip} ({kip!.Chang}) chưa đủ điều kiện nhận ban:\n\n" +
                          string.Join("\n", lyDo.Select(l => "• " + l));
                    return false;
                }

                var kipTruoc = dl.KipCuaChuyen(kip!.MaChuyenTau)
                                 .FirstOrDefault(p => p.ViTriNhan < kip.ViTriNhan && p.TrangThai == PhanCongKipHienThi.DaPhanCong);
                if (kipTruoc != null)
                {
                    loi = $"Kíp {dl.SoThuTuKip(kipTruoc)} ({kipTruoc.Chang}) của chuyến này chưa nhận ban. " +
                          "Các kíp nhận ban theo thứ tự chặng trên hành trình.";
                    return false;
                }

                // Trigger trung gio chay ca khi chi doi trang thai
                string? trungGio = KiemTraTrungGioToanBang(kip, gd);
                if (trungGio != null)
                {
                    loi = $"{dong.NhanChuyen} · {dong.TenKip} ({kip.Chang}) chưa nhận ban được, kíp đang trùng giờ:\n\n" +
                          $"{trungGio}\n\nHãy thay người rồi nhận ban.";
                    return false;
                }

                if (_repo.DoiTrangThaiKip(maPhanCong, kip.PhienBan!, PhanCongKipHienThi.DaPhanCong,
                                          PhanCongKipHienThi.DangThucHien, gd) == 0)
                {
                    loi = "Kíp vừa được người khác sửa / nhận ban. Hãy nạp lại.";
                    return false;
                }
                foreach (int ma in kip.CacThanhVien.Distinct())
                    _repo.DatTrangThaiNhanVien(ma, NhanVienHienThi.DangLam, gd);

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "NHAN_BAN", "nhansu.PhanCongKipLai",
                                    maPhanCong.ToString(), MoTaJson(kip, dl),
                                    MoTaJson(kip, dl, PhanCongKipHienThi.DangThucHien, dl.BayGio));
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // Kip HOAN_THANH; thanh vien khong con o kip dang thuc hien nao -> NGHI_NGOI
        // (truong tau di suot chuyen da nhan ban kip sau thi giu DANG_LAM)
        public bool BanGiao(int maPhanCong, byte[]? phienBan, out string loi)
        {
            loi = "";
            if (!CoQuyen(MaQuyen.NhanBanKipLai, "xác nhận bàn giao", out loi)) return false;

            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(KhoaPhanCong);
                var dl = NapTrongGiaoDich(gd);

                var kip = dl.TimKip(maPhanCong);
                string? loiGoc = KiemTraKipDangMo(kip, phienBan, PhanCongKipHienThi.DangThucHien, "bàn giao");
                if (loiGoc != null) { loi = loiGoc; return false; }

                // Kip dang thuc hien khong sua nguoi duoc; trung gio o day chi xay ra khi gio chuyen tau
                // bi doi sau luc phan cong -> trigger se chan ca viec doi trang thai, bao ro de xu ly
                string? trungGio = KiemTraTrungGioToanBang(kip!, gd);
                if (trungGio != null)
                {
                    loi = $"Chưa bàn giao được: CSDL ghi nhận kíp này trùng giờ với kíp khác:\n\n{trungGio}\n\n" +
                          "Giờ chuyến tàu có thể đã bị đổi sau khi phân công. Báo điều độ kiểm tra lại giờ chuyến.";
                    return false;
                }

                if (_repo.DoiTrangThaiKip(maPhanCong, kip!.PhienBan!, PhanCongKipHienThi.DangThucHien,
                                          PhanCongKipHienThi.HoanThanh, gd) == 0)
                {
                    loi = "Kíp vừa được người khác cập nhật. Hãy nạp lại.";
                    return false;
                }
                foreach (int ma in kip.CacThanhVien.Distinct())
                {
                    bool conCaKhac = dl.KipCuaNhanVien(ma).Any(p => p.MaPhanCong != maPhanCong
                                                                   && p.TrangThai == PhanCongKipHienThi.DangThucHien);
                    if (!conCaKhac) _repo.DatTrangThaiNhanVien(ma, NhanVienHienThi.NghiNgoi, gd);
                }

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "BAN_GIAO", "nhansu.PhanCongKipLai",
                                    maPhanCong.ToString(), MoTaJson(kip, dl),
                                    MoTaJson(kip, dl, PhanCongKipHienThi.HoanThanh, dl.BayGio));
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // =====================================================================
        // TIEN ICH
        // =====================================================================

        // Anh chup moi nhat trong giao dich, "bay gio" = gio may chu
        private DuLieuNhanSu NapTrongGiaoDich(PhienGiaoDich gd)
        {
            DateTime bayGio = _repo.LayGioMayChu(gd);
            var (tu, den) = CuaSo(bayGio);
            return _repo.LayDuLieu(tu, den, bayGio, gd);
        }

        // Doi chieu lan cuoi tren toan bang, cung dieu kien voi trigger trung gio, trong giao dich da
        // khoa nhansu.PhanCong: anh chup chi gom chuyen trong cua so nen luat R3 co the sot kip o chuyen
        // ngoai cua so. Co trung -> danh sach "• ..." ghi ro nguoi + chuyen; null = khong trung.
        private string? KiemTraTrungGioToanBang(PhanCongKipHienThi kip, PhienGiaoDich gd)
        {
            var ds = _repo.TimKipTrungGio(kip, gd);
            return ds.Count == 0 ? null : string.Join("\n", ds.Select(t => "• " + t.MoTa));
        }

        private string CapMa(string chucDanh, PhienGiaoDich gd)
            => MaNhanVienTheoChucDanh.MaKeTiep(chucDanh, _repo.LayMaTheoTienTo(MaNhanVienTheoChucDanh.TienTo(chucDanh), gd));

        private static bool CoQuyen(string maQuyen, string hanhDong, out string loi)
        {
            loi = PhanQuyenService.CoQuyen(maQuyen) ? ""
                : $"Tài khoản \"{PhanQuyenService.TenDangNhapHienTai}\" không có quyền {hanhDong} (cần quyền {maQuyen}).";
            return loi.Length == 0;
        }

        // Kip phai con, dung trang thai, dung phien ban da mo
        private static string? KiemTraKipDangMo(PhanCongKipHienThi? kip, byte[]? phienBan, string trangThaiCan, string hanhDong)
        {
            if (kip == null) return "Không tìm thấy kíp (có thể vừa bị hủy). Hãy nạp lại.";
            if (!CungPhienBan(phienBan, kip.PhienBan))
                return "Kíp vừa được người khác cập nhật sau khi bạn mở. Hãy nạp lại rồi thao tác lại.";
            if (kip.TrangThai != trangThaiCan)
                return $"Kíp đang ở trạng thái \"{kip.NhanTrangThai}\", không {hanhDong} được.";
            return null;
        }

        private static bool CungPhienBan(byte[]? a, byte[]? b)
            => a != null && b != null && a.AsSpan().SequenceEqual(b);

        private static NhanVienHienThi ChuanHoa(NhanVienHienThi nv)
        {
            var x = nv.SaoChep();
            x.HoTen = x.HoTen?.Trim() ?? "";
            x.SoDienThoai = x.SoDienThoai?.Trim() ?? "";
            x.ChucDanh = x.ChucDanh?.Trim() ?? "";
            x.DonViChuQuan = x.DonViChuQuan?.Trim() ?? "";
            x.HangBangLai = string.IsNullOrWhiteSpace(x.HangBangLai) ? null : x.HangBangLai.Trim();
            return x;
        }

        private static bool GiongNhau(NhanVienHienThi a, NhanVienHienThi b)
            => a.MaNVCode == b.MaNVCode && a.HoTen == b.HoTen && a.SoDienThoai == b.SoDienThoai && a.ChucDanh == b.ChucDanh
               && a.HangBangLai == b.HangBangLai && a.HanKhamSucKhoe.Date == b.HanKhamSucKhoe.Date
               && a.DonViChuQuan == b.DonViChuQuan && a.TrangThai == b.TrangThai;

        private static string GopLoi(IEnumerable<string?> ds)
        {
            var co = ds.Where(s => !string.IsNullOrEmpty(s)).ToList();
            return co.Count == 1 ? co[0]! : string.Join("\n", co.Select(s => "• " + s));
        }

        private static string MoTaJson(NhanVienHienThi nv) => JsonSerializer.Serialize(new
        {
            nv.MaNhanVien, nv.MaNVCode, nv.HoTen, nv.SoDienThoai, nv.ChucDanh, nv.HangBangLai,
            HanKhamSucKhoe = nv.HanKhamSucKhoe.ToString("yyyy-MM-dd"), nv.DonViChuQuan, nv.TrangThai
        });

        // Anh chup kip cho nhat ky (ma NV + ma ga de doc duoc khi tra cuu)
        private static string MoTaJson(PhanCongKipHienThi k, DuLieuNhanSu dl, string? trangThai = null, DateTime? luc = null)
        {
            string Ma(int maNv) => dl.TimNhanVien(maNv)?.MaNVCode ?? $"#{maNv}";
            string Ga(int maGa) => dl.Ga.FirstOrDefault(g => g.MaGa == maGa)?.MaGaCode ?? $"#{maGa}";
            return JsonSerializer.Serialize(new
            {
                k.MaPhanCong, k.MaChuyenTau, MacTau = k.Chuyen?.SoHieuMacTau,
                LaiTau = Ma(k.MaLaiTau), PhuLai = Ma(k.MaPhuLai), TruongTau = Ma(k.MaTruongTau),
                GaNhanBan = Ga(k.MaGaNhanBan), GaBanGiao = Ga(k.MaGaBanGiao),
                TrangThai = trangThai ?? k.TrangThai,
                ThoiDiem = luc
            });
        }
    }
}
