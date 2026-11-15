using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using DTO.QuanTri;

namespace DAL.Repositories
{
    // =========================================================================
    // QUAN TRI & PHAN QUYEN: phanquyen.TaiKhoan, VaiTro, Quyen, TaiKhoan_VaiTro, VaiTro_Quyen,
    // LichSuDangNhap; doc kiemtoan.NhatKyHeThong (chu bang: Dat, Sprint 3).
    //
    // Chi doc / ghi, khong kiem nghiep vu (PhanQuyenService / QuanTriService kiem trong cung giao dich).
    // Thoi gian: cac bang nay luu gio UTC (DEFAULT SYSUTCDATETIME() cua script goc, khoa tai khoan cung UTC).
    //   Doc ra -> doi sang gio may chu (DATEADD theo do lech SYSUTCDATETIME / SYSDATETIME).
    //   Bo loc tu ngay / den ngay (gio may chu) -> doi nguoc sang UTC trong cau SQL.
    // Mat khau: chi nhan / tra chuoi bam, khong bao gio nhan mat khau tho.
    // =========================================================================
    public class PhanQuyenRepository
    {
        // Doi cot UTC sang gio may chu
        private const string DoLech = "DATEDIFF(MINUTE, SYSUTCDATETIME(), SYSDATETIME())";
        private static string GioMayChu(string cot) => $"DATEADD(MINUTE, {DoLech}, {cot})";

        // =====================================================================
        // DANG NHAP
        // =====================================================================

        // Khoa dong tai khoan toi het giao dich: 2 lan dang nhap sai cung luc dem dung so lan.
        public TaiKhoanDangNhapDTO? LayTaiKhoanDeDangNhap(string tenDangNhap, PhienGiaoDich gd)
        {
            var bang = PhienGiaoDich.TruyVan(gd, @"
                SELECT MaTaiKhoan, TenDangNhap, MatKhauHash, HoTenHienThi, MaNhanVien, DangHoatDong,
                       SoLanSaiLienTiep, KhoaDenLuc, PhaiDoiMatKhau, SYSUTCDATETIME() AS BayGioUtc
                FROM phanquyen.TaiKhoan WITH (UPDLOCK, ROWLOCK)
                WHERE TenDangNhap = @ten",
                new SqlParameter("@ten", SqlDbType.VarChar, 50) { Value = tenDangNhap });
            if (bang.Rows.Count == 0) return null;
            var r = bang.Rows[0];
            return new TaiKhoanDangNhapDTO
            {
                MaTaiKhoan = Convert.ToInt32(r["MaTaiKhoan"]),
                TenDangNhap = r["TenDangNhap"].ToString() ?? "",
                MatKhauHash = r["MatKhauHash"].ToString() ?? "",
                HoTenHienThi = r["HoTenHienThi"].ToString() ?? "",
                MaNhanVien = r["MaNhanVien"] == DBNull.Value ? null : Convert.ToInt32(r["MaNhanVien"]),
                DangHoatDong = Convert.ToBoolean(r["DangHoatDong"]),
                SoLanSaiLienTiep = Convert.ToInt32(r["SoLanSaiLienTiep"]),
                KhoaDenLucUtc = r["KhoaDenLuc"] == DBNull.Value ? null : Convert.ToDateTime(r["KhoaDenLuc"]),
                PhaiDoiMatKhau = Convert.ToBoolean(r["PhaiDoiMatKhau"]),
                BayGioUtc = Convert.ToDateTime(r["BayGioUtc"])
            };
        }

        public void GhiLichSuDangNhap(int? maTaiKhoan, string tenDangNhap, bool thanhCong, string? lyDo,
                                      string? diaChiIP, string? thietBi, PhienGiaoDich gd)
        {
            PhienGiaoDich.ThucThi(gd, @"
                INSERT INTO phanquyen.LichSuDangNhap (MaTaiKhoan, TenDangNhap, DiaChiIP, ThietBi, ThanhCong, LyDoThatBai)
                VALUES (@tk, @ten, @ip, @tb, @ok, @lyDo)",
                new SqlParameter("@tk", SqlDbType.Int) { Value = (object?)maTaiKhoan ?? DBNull.Value },
                new SqlParameter("@ten", SqlDbType.VarChar, 50) { Value = Cat(tenDangNhap, 50) },
                new SqlParameter("@ip", SqlDbType.VarChar, 45) { Value = (object?)Cat(diaChiIP, 45) ?? DBNull.Value },
                new SqlParameter("@tb", SqlDbType.NVarChar, 255) { Value = (object?)Cat(thietBi, 255) ?? DBNull.Value },
                new SqlParameter("@ok", thanhCong),
                new SqlParameter("@lyDo", SqlDbType.NVarChar, 255) { Value = (object?)Cat(lyDo, 255) ?? DBNull.Value });
        }

        // Sai mat khau: ghi so lan sai; khoa = true thi dat KhoaDenLuc = bay gio + soPhut (UTC), dem lai tu 0.
        // Tra ve thoi diem het khoa (gio may chu) neu vua khoa.
        public DateTime? GhiSaiMatKhau(int maTaiKhoan, int soLanSai, bool khoa, int soPhutKhoa, PhienGiaoDich gd)
        {
            var bang = PhienGiaoDich.TruyVan(gd, $@"
                UPDATE phanquyen.TaiKhoan
                SET SoLanSaiLienTiep = @n,
                    KhoaDenLuc = CASE WHEN @khoa = 1 THEN DATEADD(MINUTE, @phut, SYSUTCDATETIME()) ELSE NULL END
                OUTPUT {GioMayChu("INSERTED.KhoaDenLuc")} AS KhoaDenLuc
                WHERE MaTaiKhoan = @id",
                new SqlParameter("@n", khoa ? 0 : soLanSai),
                new SqlParameter("@khoa", khoa),
                new SqlParameter("@phut", soPhutKhoa),
                new SqlParameter("@id", maTaiKhoan));
            if (bang.Rows.Count == 0 || bang.Rows[0]["KhoaDenLuc"] == DBNull.Value) return null;
            return Convert.ToDateTime(bang.Rows[0]["KhoaDenLuc"]);
        }

        public void GhiDangNhapThanhCong(int maTaiKhoan, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                UPDATE phanquyen.TaiKhoan
                SET SoLanSaiLienTiep = 0, KhoaDenLuc = NULL, LanDangNhapCuoi = SYSUTCDATETIME()
                WHERE MaTaiKhoan = @id",
                new SqlParameter("@id", maTaiKhoan));

        // Vai tro DANG KICH HOAT cua tai khoan + hop cac quyen cua chung.
        public (List<VaiTroDTO> VaiTro, List<string> Quyen) LayVaiTroVaQuyen(int maTaiKhoan, PhienGiaoDich? gd = null)
        {
            var vaiTro = PhienGiaoDich.TruyVan(gd, @"
                SELECT vt.MaVaiTro, vt.MaVaiTroCode, vt.TenVaiTro, vt.MoTa, vt.KichHoat
                FROM phanquyen.TaiKhoan_VaiTro tv
                JOIN phanquyen.VaiTro vt ON vt.MaVaiTro = tv.MaVaiTro
                WHERE tv.MaTaiKhoan = @id AND vt.KichHoat = 1
                ORDER BY vt.MaVaiTro",
                new SqlParameter("@id", maTaiKhoan)).Rows.Cast<DataRow>().Select(DocVaiTro).ToList();

            var quyen = PhienGiaoDich.TruyVan(gd, @"
                SELECT DISTINCT q.MaQuyenCode
                FROM phanquyen.TaiKhoan_VaiTro tv
                JOIN phanquyen.VaiTro vt ON vt.MaVaiTro = tv.MaVaiTro AND vt.KichHoat = 1
                JOIN phanquyen.VaiTro_Quyen vq ON vq.MaVaiTro = vt.MaVaiTro
                JOIN phanquyen.Quyen q ON q.MaQuyen = vq.MaQuyen
                WHERE tv.MaTaiKhoan = @id",
                new SqlParameter("@id", maTaiKhoan)).Rows.Cast<DataRow>()
                .Select(r => r["MaQuyenCode"].ToString() ?? "").Where(s => s.Length > 0).ToList();

            return (vaiTro, quyen);
        }

        // Doi / dat lai mat khau. phaiDoi = true khi quan tri vien dat lai (nguoi dung phai doi o lan dang nhap sau).
        public int CapNhatMatKhau(int maTaiKhoan, string chuoiBam, bool phaiDoi, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                UPDATE phanquyen.TaiKhoan
                SET MatKhauHash = @bam, PhaiDoiMatKhau = @doi, SoLanSaiLienTiep = 0, KhoaDenLuc = NULL
                WHERE MaTaiKhoan = @id",
                new SqlParameter("@bam", SqlDbType.VarChar, 255) { Value = chuoiBam },
                new SqlParameter("@doi", phaiDoi),
                new SqlParameter("@id", maTaiKhoan));

        // =====================================================================
        // QUAN TRI TAI KHOAN
        // =====================================================================

        public List<TaiKhoanDTO> LayDanhSachTaiKhoan(PhienGiaoDich? gd = null)
        {
            var vaiTro = PhienGiaoDich.TruyVan(gd, @"
                SELECT tv.MaTaiKhoan, vt.MaVaiTro, vt.MaVaiTroCode, vt.TenVaiTro, vt.MoTa, vt.KichHoat
                FROM phanquyen.TaiKhoan_VaiTro tv JOIN phanquyen.VaiTro vt ON vt.MaVaiTro = tv.MaVaiTro
                ORDER BY vt.MaVaiTro").Rows.Cast<DataRow>()
                .GroupBy(r => Convert.ToInt32(r["MaTaiKhoan"]))
                .ToDictionary(g => g.Key, g => g.Select(DocVaiTro).ToList());

            return PhienGiaoDich.TruyVan(gd, $@"
                SELECT tk.MaTaiKhoan, tk.TenDangNhap, tk.HoTenHienThi, tk.Email, tk.MaNhanVien, nv.MaNVCode,
                       nv.HoTen AS HoTenNhanVien, tk.DangHoatDong, tk.SoLanSaiLienTiep,
                       {GioMayChu("tk.KhoaDenLuc")} AS KhoaDenLuc,
                       CAST(IIF(tk.KhoaDenLuc > SYSUTCDATETIME(), 1, 0) AS BIT) AS DangBiKhoa,
                       tk.PhaiDoiMatKhau, {GioMayChu("tk.LanDangNhapCuoi")} AS LanDangNhapCuoi,
                       {GioMayChu("tk.NgayTao")} AS NgayTao, tk.PhienBan
                FROM phanquyen.TaiKhoan tk
                LEFT JOIN nhansu.NhanVien nv ON nv.MaNhanVien = tk.MaNhanVien
                ORDER BY tk.MaTaiKhoan").Rows.Cast<DataRow>().Select(r =>
            {
                int ma = Convert.ToInt32(r["MaTaiKhoan"]);
                return new TaiKhoanDTO
                {
                    MaTaiKhoan = ma,
                    TenDangNhap = r["TenDangNhap"].ToString() ?? "",
                    HoTenHienThi = r["HoTenHienThi"].ToString() ?? "",
                    Email = ChuoiHoacNull(r["Email"]),
                    MaNhanVien = r["MaNhanVien"] == DBNull.Value ? null : Convert.ToInt32(r["MaNhanVien"]),
                    MaNVCode = ChuoiHoacNull(r["MaNVCode"]),
                    HoTenNhanVien = ChuoiHoacNull(r["HoTenNhanVien"]),
                    DangHoatDong = Convert.ToBoolean(r["DangHoatDong"]),
                    SoLanSaiLienTiep = Convert.ToInt32(r["SoLanSaiLienTiep"]),
                    KhoaDenLuc = NgayHoacNull(r["KhoaDenLuc"]),
                    DangBiKhoa = Convert.ToBoolean(r["DangBiKhoa"]),
                    PhaiDoiMatKhau = Convert.ToBoolean(r["PhaiDoiMatKhau"]),
                    LanDangNhapCuoi = NgayHoacNull(r["LanDangNhapCuoi"]),
                    NgayTao = Convert.ToDateTime(r["NgayTao"]),
                    PhienBan = (byte[])r["PhienBan"],
                    CacVaiTro = vaiTro.TryGetValue(ma, out var ds) ? ds : new List<VaiTroDTO>()
                };
            }).ToList();
        }

        public List<VaiTroDTO> LayDanhSachVaiTro(PhienGiaoDich? gd = null)
            => PhienGiaoDich.TruyVan(gd,
                "SELECT MaVaiTro, MaVaiTroCode, TenVaiTro, MoTa, KichHoat FROM phanquyen.VaiTro ORDER BY MaVaiTro")
               .Rows.Cast<DataRow>().Select(DocVaiTro).ToList();

        public List<QuyenDTO> LayDanhSachQuyen(PhienGiaoDich? gd = null)
            => PhienGiaoDich.TruyVan(gd,
                "SELECT MaQuyen, MaQuyenCode, TenQuyen, NhomQuyen, MoTa FROM phanquyen.Quyen ORDER BY NhomQuyen, MaQuyen")
               .Rows.Cast<DataRow>().Select(r => new QuyenDTO(
                   Convert.ToInt32(r["MaQuyen"]), r["MaQuyenCode"].ToString() ?? "", r["TenQuyen"].ToString() ?? "",
                   r["NhomQuyen"].ToString() ?? "", ChuoiHoacNull(r["MoTa"]))).ToList();

        public HashSet<(int MaVaiTro, int MaQuyen)> LayVaiTroQuyen(PhienGiaoDich? gd = null)
            => PhienGiaoDich.TruyVan(gd, "SELECT MaVaiTro, MaQuyen FROM phanquyen.VaiTro_Quyen")
               .Rows.Cast<DataRow>().Select(r => (Convert.ToInt32(r["MaVaiTro"]), Convert.ToInt32(r["MaQuyen"]))).ToHashSet();

        // Nhan vien de noi voi tai khoan: (ma, ma NV, ho ten, chuc danh, so tai khoan dang noi).
        public List<(int Ma, string MaNV, string HoTen, string ChucDanh, int SoTaiKhoan)> LayNhanVienDeNoi(PhienGiaoDich? gd = null)
            => PhienGiaoDich.TruyVan(gd, @"
                SELECT nv.MaNhanVien, nv.MaNVCode, nv.HoTen, nv.ChucDanh,
                       (SELECT COUNT(*) FROM phanquyen.TaiKhoan tk WHERE tk.MaNhanVien = nv.MaNhanVien AND tk.DangHoatDong = 1) AS SoTaiKhoan
                FROM nhansu.NhanVien nv
                WHERE nv.TrangThai <> 'DA_NGHI_VIEC'
                ORDER BY nv.MaNVCode").Rows.Cast<DataRow>()
               .Select(r => (Convert.ToInt32(r["MaNhanVien"]), r["MaNVCode"].ToString() ?? "", r["HoTen"].ToString() ?? "",
                             r["ChucDanh"].ToString() ?? "", Convert.ToInt32(r["SoTaiKhoan"]))).ToList();

        // [12/11] Mot ho so nhan vien chi noi voi 1 tai khoan DANG HOAT DONG (tai khoan cu da vo hieu hoa thi duoc giu).
        // Tra ve ten dang nhap cua tai khoan khac dang giu ho so, null = chua co. Khoa de 2 nguoi tao cung luc khong lot.
        public string? TaiKhoanKhacCuaNhanVien(int maNhanVien, int maTaiKhoanTru, PhienGiaoDich gd)
            => PhienGiaoDich.LayGiaTri(gd, @"
                SELECT TOP 1 TenDangNhap FROM phanquyen.TaiKhoan WITH (UPDLOCK, HOLDLOCK)
                WHERE MaNhanVien = @nv AND DangHoatDong = 1 AND MaTaiKhoan <> @id
                ORDER BY MaTaiKhoan",
                new SqlParameter("@nv", maNhanVien), new SqlParameter("@id", maTaiKhoanTru)) as string;

        public bool TonTaiTenDangNhap(string ten, PhienGiaoDich gd)
            => PhienGiaoDich.LayGiaTri(gd, "SELECT 1 FROM phanquyen.TaiKhoan WITH (UPDLOCK, HOLDLOCK) WHERE TenDangNhap = @ten",
                   new SqlParameter("@ten", SqlDbType.VarChar, 50) { Value = ten }) != null;

        public int ThemTaiKhoan(TaiKhoanLuuDTO tk, string chuoiBam, PhienGiaoDich gd)
            => Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd, @"
                INSERT INTO phanquyen.TaiKhoan (TenDangNhap, MatKhauHash, HoTenHienThi, Email, MaNhanVien, DangHoatDong, PhaiDoiMatKhau)
                OUTPUT INSERTED.MaTaiKhoan
                VALUES (@ten, @bam, @hoTen, @email, @nv, @hd, 1)",
                new SqlParameter("@ten", SqlDbType.VarChar, 50) { Value = tk.TenDangNhap },
                new SqlParameter("@bam", SqlDbType.VarChar, 255) { Value = chuoiBam },
                new SqlParameter("@hoTen", SqlDbType.NVarChar, 100) { Value = tk.HoTenHienThi },
                new SqlParameter("@email", SqlDbType.VarChar, 100) { Value = (object?)tk.Email ?? DBNull.Value },
                new SqlParameter("@nv", SqlDbType.Int) { Value = (object?)tk.MaNhanVien ?? DBNull.Value },
                new SqlParameter("@hd", tk.DangHoatDong)));

        // Sua thong tin (khong doi ten dang nhap, mat khau). 0 dong = nguoi khac vua sua.
        public int CapNhatTaiKhoan(TaiKhoanLuuDTO tk, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                UPDATE phanquyen.TaiKhoan
                SET HoTenHienThi = @hoTen, Email = @email, MaNhanVien = @nv, DangHoatDong = @hd
                WHERE MaTaiKhoan = @id AND PhienBan = @pb",
                new SqlParameter("@hoTen", SqlDbType.NVarChar, 100) { Value = tk.HoTenHienThi },
                new SqlParameter("@email", SqlDbType.VarChar, 100) { Value = (object?)tk.Email ?? DBNull.Value },
                new SqlParameter("@nv", SqlDbType.Int) { Value = (object?)tk.MaNhanVien ?? DBNull.Value },
                new SqlParameter("@hd", tk.DangHoatDong),
                new SqlParameter("@id", tk.MaTaiKhoan),
                new SqlParameter("@pb", SqlDbType.Timestamp) { Value = tk.PhienBan ?? Array.Empty<byte>() });

        // Thay tap vai tro cua tai khoan bang danh sach moi (xoa cai bi bo, them cai moi kem nguoi cap).
        public void ThayVaiTro(int maTaiKhoan, IEnumerable<int> cacMaVaiTro, int nguoiCap, PhienGiaoDich gd)
        {
            string ds = string.Join(",", cacMaVaiTro.Distinct());
            PhienGiaoDich.ThucThi(gd, @"
                DELETE FROM phanquyen.TaiKhoan_VaiTro
                WHERE MaTaiKhoan = @id AND MaVaiTro NOT IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@ds, ','));
                INSERT INTO phanquyen.TaiKhoan_VaiTro (MaTaiKhoan, MaVaiTro, NguoiCap)
                SELECT @id, vt.MaVaiTro, @nguoi
                FROM phanquyen.VaiTro vt
                WHERE vt.MaVaiTro IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@ds, ','))
                  AND NOT EXISTS (SELECT 1 FROM phanquyen.TaiKhoan_VaiTro x WHERE x.MaTaiKhoan = @id AND x.MaVaiTro = vt.MaVaiTro);",
                new SqlParameter("@id", maTaiKhoan),
                new SqlParameter("@ds", SqlDbType.VarChar, 1000) { Value = ds },
                new SqlParameter("@nguoi", nguoiCap));
        }

        public int MoKhoa(int maTaiKhoan, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd,
                "UPDATE phanquyen.TaiKhoan SET SoLanSaiLienTiep = 0, KhoaDenLuc = NULL WHERE MaTaiKhoan = @id",
                new SqlParameter("@id", maTaiKhoan));

        // So tai khoan quan tri (vai tro ADMIN dang kich hoat) con hoat dong, tru tai khoan dang xet.
        public int DemQuanTriVienKhac(int maTaiKhoanTru, PhienGiaoDich gd)
            => Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd, @"
                SELECT COUNT(DISTINCT tk.MaTaiKhoan)
                FROM phanquyen.TaiKhoan tk WITH (HOLDLOCK)
                JOIN phanquyen.TaiKhoan_VaiTro tv ON tv.MaTaiKhoan = tk.MaTaiKhoan
                JOIN phanquyen.VaiTro vt ON vt.MaVaiTro = tv.MaVaiTro AND vt.KichHoat = 1 AND vt.MaVaiTroCode = 'ADMIN'
                WHERE tk.DangHoatDong = 1 AND tk.MaTaiKhoan <> @id",
                new SqlParameter("@id", maTaiKhoanTru)));

        // Ma tran vai tro x quyen: thay tap quyen cua mot vai tro.
        public void ThayQuyenCuaVaiTro(int maVaiTro, IEnumerable<int> cacMaQuyen, PhienGiaoDich gd)
        {
            string ds = string.Join(",", cacMaQuyen.Distinct());
            PhienGiaoDich.ThucThi(gd, @"
                DELETE FROM phanquyen.VaiTro_Quyen
                WHERE MaVaiTro = @vt AND MaQuyen NOT IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@ds, ','));
                INSERT INTO phanquyen.VaiTro_Quyen (MaVaiTro, MaQuyen)
                SELECT @vt, q.MaQuyen FROM phanquyen.Quyen q
                WHERE q.MaQuyen IN (SELECT TRY_CAST(value AS INT) FROM STRING_SPLIT(@ds, ','))
                  AND NOT EXISTS (SELECT 1 FROM phanquyen.VaiTro_Quyen x WHERE x.MaVaiTro = @vt AND x.MaQuyen = q.MaQuyen);",
                new SqlParameter("@vt", maVaiTro),
                new SqlParameter("@ds", SqlDbType.VarChar, 2000) { Value = ds });
        }

        // =====================================================================
        // LICH SU DANG NHAP + NHAT KY HE THONG (chi doc)
        // =====================================================================

        public List<LichSuDangNhapDTO> LayLichSuDangNhap(BoLocNhatKy loc)
        {
            var ts = ThamSoKhoangNgay(loc);
            string dk = $"ls.ThoiDiemDangNhap >= DATEADD(MINUTE, -{DoLech}, @tu) AND ls.ThoiDiemDangNhap < DATEADD(MINUTE, -{DoLech}, @den)";
            if (loc.MaTaiKhoan.HasValue) { dk += " AND ls.MaTaiKhoan = @tk"; ts.Add(new SqlParameter("@tk", loc.MaTaiKhoan.Value)); }
            if (loc.ThanhCong.HasValue) { dk += " AND ls.ThanhCong = @ok"; ts.Add(new SqlParameter("@ok", loc.ThanhCong.Value)); }
            if (!string.IsNullOrWhiteSpace(loc.TuKhoa))
            {
                dk += " AND (ls.TenDangNhap LIKE @kw OR ls.ThietBi LIKE @kw OR ls.LyDoThatBai LIKE @kw)";
                ts.Add(new SqlParameter("@kw", SqlDbType.NVarChar, 120) { Value = "%" + loc.TuKhoa.Trim() + "%" });
            }

            return PhienGiaoDich.TruyVan(null, $@"
                SELECT TOP (@top) ls.MaLichSu, ls.MaTaiKhoan, ls.TenDangNhap, tk.HoTenHienThi,
                       {GioMayChu("ls.ThoiDiemDangNhap")} AS ThoiDiem, ls.DiaChiIP, ls.ThietBi, ls.ThanhCong, ls.LyDoThatBai
                FROM phanquyen.LichSuDangNhap ls
                LEFT JOIN phanquyen.TaiKhoan tk ON tk.MaTaiKhoan = ls.MaTaiKhoan
                WHERE {dk}
                ORDER BY ls.ThoiDiemDangNhap DESC, ls.MaLichSu DESC", ts.ToArray()).Rows.Cast<DataRow>()
                .Select(r => new LichSuDangNhapDTO
                {
                    MaLichSu = Convert.ToInt64(r["MaLichSu"]),
                    MaTaiKhoan = r["MaTaiKhoan"] == DBNull.Value ? null : Convert.ToInt32(r["MaTaiKhoan"]),
                    TenDangNhap = r["TenDangNhap"].ToString() ?? "",
                    HoTenHienThi = ChuoiHoacNull(r["HoTenHienThi"]),
                    ThoiDiem = Convert.ToDateTime(r["ThoiDiem"]),
                    DiaChiIP = ChuoiHoacNull(r["DiaChiIP"]),
                    ThietBi = ChuoiHoacNull(r["ThietBi"]),
                    ThanhCong = Convert.ToBoolean(r["ThanhCong"]),
                    LyDoThatBai = ChuoiHoacNull(r["LyDoThatBai"])
                }).ToList();
        }

        public List<NhatKyHeThongDTO> LayNhatKyHeThong(BoLocNhatKy loc)
        {
            var ts = ThamSoKhoangNgay(loc);
            string dk = $"nk.ThoiDiem >= DATEADD(MINUTE, -{DoLech}, @tu) AND nk.ThoiDiem < DATEADD(MINUTE, -{DoLech}, @den)";
            if (loc.MaTaiKhoan.HasValue) { dk += " AND nk.MaTaiKhoan = @tk"; ts.Add(new SqlParameter("@tk", loc.MaTaiKhoan.Value)); }
            if (!string.IsNullOrWhiteSpace(loc.TenBang)) { dk += " AND nk.TenBang = @bang"; ts.Add(new SqlParameter("@bang", SqlDbType.VarChar, 50) { Value = loc.TenBang }); }
            if (!string.IsNullOrWhiteSpace(loc.HanhDong)) { dk += " AND nk.HanhDong = @hd"; ts.Add(new SqlParameter("@hd", SqlDbType.VarChar, 50) { Value = loc.HanhDong }); }
            if (!string.IsNullOrWhiteSpace(loc.TuKhoa))
            {
                dk += " AND (nk.KhoaChinhRecord LIKE @kw OR nk.DuLieuCu LIKE @kw OR nk.DuLieuMoi LIKE @kw)";
                ts.Add(new SqlParameter("@kw", SqlDbType.NVarChar, 120) { Value = "%" + loc.TuKhoa.Trim() + "%" });
            }

            return PhienGiaoDich.TruyVan(null, $@"
                SELECT TOP (@top) nk.MaNhatKy, {GioMayChu("nk.ThoiDiem")} AS ThoiDiem, nk.MaTaiKhoan, tk.TenDangNhap,
                       tk.HoTenHienThi, nk.HanhDong, nk.TenBang, nk.KhoaChinhRecord, nk.DuLieuCu, nk.DuLieuMoi, nk.DiaChiIP
                FROM kiemtoan.NhatKyHeThong nk
                LEFT JOIN phanquyen.TaiKhoan tk ON tk.MaTaiKhoan = nk.MaTaiKhoan
                WHERE {dk}
                ORDER BY nk.ThoiDiem DESC, nk.MaNhatKy DESC", ts.ToArray()).Rows.Cast<DataRow>()
                .Select(r => new NhatKyHeThongDTO
                {
                    MaNhatKy = Convert.ToInt64(r["MaNhatKy"]),
                    ThoiDiem = Convert.ToDateTime(r["ThoiDiem"]),
                    MaTaiKhoan = r["MaTaiKhoan"] == DBNull.Value ? null : Convert.ToInt32(r["MaTaiKhoan"]),
                    TenDangNhap = ChuoiHoacNull(r["TenDangNhap"]),
                    HoTenHienThi = ChuoiHoacNull(r["HoTenHienThi"]),
                    HanhDong = r["HanhDong"].ToString() ?? "",
                    TenBang = r["TenBang"].ToString() ?? "",
                    KhoaChinhRecord = r["KhoaChinhRecord"].ToString() ?? "",
                    DuLieuCu = ChuoiHoacNull(r["DuLieuCu"]),
                    DuLieuMoi = ChuoiHoacNull(r["DuLieuMoi"]),
                    DiaChiIP = ChuoiHoacNull(r["DiaChiIP"])
                }).ToList();
        }

        // Gia tri dang co trong nhat ky (cho o loc bang / hanh dong)
        public (List<string> Bang, List<string> HanhDong) LayGiaTriLocNhatKy()
        {
            var bang = PhienGiaoDich.TruyVan(null, "SELECT DISTINCT TenBang FROM kiemtoan.NhatKyHeThong ORDER BY TenBang")
                .Rows.Cast<DataRow>().Select(r => r[0].ToString() ?? "").ToList();
            var hd = PhienGiaoDich.TruyVan(null, "SELECT DISTINCT HanhDong FROM kiemtoan.NhatKyHeThong ORDER BY HanhDong")
                .Rows.Cast<DataRow>().Select(r => r[0].ToString() ?? "").ToList();
            return (bang, hd);
        }

        // =====================================================================

        private static List<SqlParameter> ThamSoKhoangNgay(BoLocNhatKy loc) => new()
        {
            // tu / den la ngay theo gio may chu -> doi sang UTC; den ngay tinh tron ngay
            new SqlParameter("@tu", SqlDbType.DateTime2) { Value = loc.TuNgay.Date },
            new SqlParameter("@den", SqlDbType.DateTime2) { Value = loc.DenNgay.Date.AddDays(1) },
            new SqlParameter("@top", Math.Clamp(loc.SoDongToiDa, 1, 20_000))
        };

        private static VaiTroDTO DocVaiTro(DataRow r) => new(
            Convert.ToInt32(r["MaVaiTro"]), r["MaVaiTroCode"].ToString() ?? "", r["TenVaiTro"].ToString() ?? "",
            ChuoiHoacNull(r["MoTa"]), Convert.ToBoolean(r["KichHoat"]));

        private static string? ChuoiHoacNull(object o) => o == DBNull.Value ? null : o.ToString();
        private static DateTime? NgayHoacNull(object o) => o == DBNull.Value ? null : Convert.ToDateTime(o);
        private static string? Cat(string? s, int n) => s == null ? null : s.Length <= n ? s : s[..n];
    }
}
