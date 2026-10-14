using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using DTO.Common;
using DTO.NhanSu;

namespace DAL.Repositories
{
    // =========================================================================
    // NHAN SU: nhansu.NhanVien, PhanCongKipLai, KiemTraSucKhoe (chu bang: Dat)
    //
    // LayDuLieu : anh chup du lieu cho ca trang Nhan su (4 tab + 3 dialog) va cho
    //             NhanSuService chay lai luat trong giao dich truoc khi ghi. Doc chi doc
    //             vanhanh.ChuyenTau / LichDungGa / MacTauMau (A Ba, Sprint 1 doc thang -
    //             KE_HOACH §4.5), vanhanh.DoanTau + phuongtien.* (dong may cho R7).
    // Ghi       : chi ghi, khong kiem nghiep vu (BUS da kiem trong cung giao dich).
    //             Xoa ho so (14/10) chi khi chua co du lieu tro toi (DemThamChieuNhanVien).
    //             Kip / nhan vien ghi theo PhienBan (ROWVERSION, migration V20261011_1500):
    //             tra ve so dong, 0 = nguoi khac vua sua / xoa.
    // Thoi gian : gio Viet Nam (quyet dinh 11/10 - khop gio chuyen tau cua vanhanh), ghi
    //             bang SYSDATETIME() cua may chu, khong dung mac dinh SYSUTCDATETIME().
    // =========================================================================
    public class NhanSuRepository
    {
        // Chuyen nam trong cua so [tu, den) hoac dang chay / co kip dang thuc hien.
        // 12/10: them chuyen CHUA KET THUC ma ke hoach da ve dich truoc cua so (dieu do chua ghi
        // hoan thanh / huy) - trigger trung gio van tinh kip cua chuyen do, bo qua thi luat R3 / R4
        // bao "khong trung" roi trigger chan luc luu. Chuyen bat dau sau @den ma chong gio voi
        // chuyen trong cua so van co the sot -> Service doi chieu lai bang TimKipTrungGio truoc khi ghi.
        private const string DieuKienCuaSo = @"
            (ct.GioVeDichKH >= @tu AND ct.GioXuatPhatKH < @den)
            OR (ct.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY') AND ct.GioVeDichKH < @tu)
            OR ct.TrangThai IN ('DANG_CHAY', 'TRE_GIO')
            OR EXISTS (SELECT 1 FROM nhansu.PhanCongKipLai k0
                       WHERE k0.MaChuyenTau = ct.MaChuyenTau AND k0.TrangThai = 'DANG_THUC_HIEN')";

        public DuLieuNhanSu LayDuLieu(DateTime tu, DateTime den, DateTime bayGio, PhienGiaoDich? gd = null)
        {
            var thamSoCuaSo = () => new[] { new SqlParameter("@tu", tu), new SqlParameter("@den", den) };

            // --- Ga + hang bang lai ---
            var ga = LayGa(gd);
            var gaTheoMa = ga.ToDictionary(g => g.MaGa);

            var hangBang = LayHangBangLai(gd);

            // --- Nhan vien + tai khoan dang nhap (neu co) ---
            var nhanVien = PhienGiaoDich.TruyVan(gd, @"
                SELECT nv.MaNhanVien, nv.MaNVCode, nv.HoTen, nv.SoDienThoai, nv.ChucDanh, nv.HangBangLai, nv.HanKhamSucKhoe,
                       nv.DonViChuQuan, nv.TrangThai, nv.NgayTao, nv.PhienBan, nv.NgaySinh,
                       tk.TenDangNhap, tk.DangHoatDong, tk.LanDangNhapCuoi, ISNULL(tk.SoTaiKhoan, 0) AS SoTaiKhoan
                FROM nhansu.NhanVien nv
                OUTER APPLY (SELECT TOP 1 t.TenDangNhap, t.DangHoatDong, t.LanDangNhapCuoi, COUNT(*) OVER () AS SoTaiKhoan
                             FROM phanquyen.TaiKhoan t
                             WHERE t.MaNhanVien = nv.MaNhanVien
                             ORDER BY t.DangHoatDong DESC, t.LanDangNhapCuoi DESC) tk")
                .Rows.Cast<DataRow>().Select(DocNhanVien).ToList();

            // --- Lich su doi ma (migration V20261012_1100) ---
            var lichSuMa = PhienGiaoDich.TruyVan(gd, @"
                SELECT MaNhanVien, MaNVCodeCu, MaNVCodeMoi, ChucDanhCu, ChucDanhMoi, DonViCu, DonViMoi, ThoiDiem
                FROM nhansu.LichSuMaNhanVien ORDER BY ThoiDiem, MaLichSu")
                .Rows.Cast<DataRow>()
                .GroupBy(r => Convert.ToInt32(r["MaNhanVien"]))
                .ToDictionary(g => g.Key, g => (IReadOnlyList<LichSuMaNhanVienDTO>)g.Select(r => new LichSuMaNhanVienDTO(
                    Chuoi(r, "MaNVCodeCu"), Chuoi(r, "MaNVCodeMoi"), Chuoi(r, "ChucDanhCu"), Chuoi(r, "ChucDanhMoi"),
                    Chuoi(r, "DonViCu"), Chuoi(r, "DonViMoi"), Convert.ToDateTime(r["ThoiDiem"]))).ToList());
            foreach (var nv in nhanVien)
                if (lichSuMa.TryGetValue(nv.MaNhanVien, out var ls)) nv.LichSuMa = ls;

            // --- Chuyen tau trong cua so + dau may chinh (dong may) ---
            var bangChuyen = PhienGiaoDich.TruyVan(gd, $@"
                SELECT ct.MaChuyenTau, mt.SoHieuMacTau, mt.LoaiTau, ct.NgayXuatPhat, ct.GioXuatPhatKH, ct.GioVeDichKH,
                       ct.TrangThai, mt.MaGaDi, mt.MaGaDen, dm.SoHieuDauMay, ddm.MaDongCode
                FROM vanhanh.ChuyenTau ct
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                LEFT JOIN vanhanh.DoanTau dt ON dt.MaChuyenTau = ct.MaChuyenTau
                LEFT JOIN phuongtien.DauMay dm ON dm.MaDauMay = dt.MaDauMayChinh
                LEFT JOIN phuongtien.DongDauMay ddm ON ddm.MaDongDauMay = dm.MaDongDauMay
                WHERE {DieuKienCuaSo}
                ORDER BY ct.GioXuatPhatKH, ct.MaChuyenTau", thamSoCuaSo());

            // --- Lich dung ga cua cac chuyen do ---
            var lichDung = PhienGiaoDich.TruyVan(gd, $@"
                SELECT ld.MaChuyenTau, ld.ThuTuDung, ld.MaGa, ld.GioDenKeHoach, ld.GioDiKeHoach
                FROM vanhanh.LichDungGa ld
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = ld.MaChuyenTau
                WHERE {DieuKienCuaSo}
                ORDER BY ld.MaChuyenTau, ld.ThuTuDung", thamSoCuaSo())
                .Rows.Cast<DataRow>()
                .GroupBy(r => Convert.ToInt32(r["MaChuyenTau"]))
                .ToDictionary(g => g.Key, g => g.ToList());

            var chuyenTau = new List<ChuyenTauNhanSu>();
            foreach (DataRow r in bangChuyen.Rows)
            {
                int ma = Convert.ToInt32(r["MaChuyenTau"]);
                DateTime di = Convert.ToDateTime(r["GioXuatPhatKH"]);
                DateTime den2 = Convert.ToDateTime(r["GioVeDichKH"]);
                GaNhanSu gaDi = TimGa(gaTheoMa, Convert.ToInt32(r["MaGaDi"]));
                GaNhanSu gaDen = TimGa(gaTheoMa, Convert.ToInt32(r["MaGaDen"]));

                List<DiemDungNhanSu> diem;
                bool coLich = lichDung.TryGetValue(ma, out var dongLich) && dongLich.Count >= 2;
                if (coLich)
                {
                    diem = dongLich!.Select(d => new DiemDungNhanSu(
                        TimGa(gaTheoMa, Convert.ToInt32(d["MaGa"])),
                        Convert.ToInt32(d["ThuTuDung"]),
                        d["GioDenKeHoach"] != DBNull.Value ? Convert.ToDateTime(d["GioDenKeHoach"]) : Convert.ToDateTime(d["GioDiKeHoach"]),
                        d["GioDiKeHoach"] != DBNull.Value ? Convert.ToDateTime(d["GioDiKeHoach"]) : Convert.ToDateTime(d["GioDenKeHoach"])))
                        .ToList();
                }
                else
                {
                    // Chua co lich dung ga: 2 diem ga di / ga den theo gio cua chuyen
                    diem = new List<DiemDungNhanSu>
                    {
                        new(gaDi, 1, di, di),
                        new(gaDen, 2, den2, den2)
                    };
                }

                string trangThai = Chuoi(r, "TrangThai");
                chuyenTau.Add(new ChuyenTauNhanSu
                {
                    MaChuyenTau = ma,
                    SoHieuMacTau = Chuoi(r, "SoHieuMacTau"),
                    LoaiTau = Chuoi(r, "LoaiTau"),
                    NgayXuatPhat = Convert.ToDateTime(r["NgayXuatPhat"]),
                    GioXuatPhatKH = di,
                    GioVeDichKH = den2,
                    TrangThai = trangThai,
                    QuaGioVeDich = trangThai is not ("HOAN_THANH" or "DA_HUY") && den2 < bayGio,
                    GaDi = gaDi,
                    GaDen = gaDen,
                    SoHieuDauMay = r["SoHieuDauMay"] == DBNull.Value ? null : Chuoi(r, "SoHieuDauMay"),
                    MaDongDauMay = r["MaDongCode"] == DBNull.Value ? null : Chuoi(r, "MaDongCode"),
                    CoLichDungGa = coLich,
                    LichDung = diem
                });
            }
            var chuyenTheoMa = chuyenTau.ToDictionary(c => c.MaChuyenTau);

            // --- Kip cua cac chuyen trong cua so ---
            // Doc ngoai giao dich thi cac cau doc rieng le: chuyen vua vao cua so giua 2 cau (vd vua
            // chuyen sang DANG_CHAY) khong co trong danh sach chuyen -> bo qua kip do, lan nap sau se co.
            var phanCong = PhienGiaoDich.TruyVan(gd, $@"
                SELECT k.MaPhanCong, k.MaChuyenTau, k.MaLaiTau, k.MaPhuLai, k.MaTruongTau,
                       k.MaGaNhanBan, k.MaGaBanGiao, k.TrangThai, k.PhienBan
                FROM nhansu.PhanCongKipLai k
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = k.MaChuyenTau
                WHERE {DieuKienCuaSo}", thamSoCuaSo())
                .Rows.Cast<DataRow>()
                .Where(r => chuyenTheoMa.ContainsKey(Convert.ToInt32(r["MaChuyenTau"])))
                .Select(r => new PhanCongKipHienThi
                {
                    MaPhanCong = Convert.ToInt32(r["MaPhanCong"]),
                    MaChuyenTau = Convert.ToInt32(r["MaChuyenTau"]),
                    MaLaiTau = Convert.ToInt32(r["MaLaiTau"]),
                    MaPhuLai = Convert.ToInt32(r["MaPhuLai"]),
                    MaTruongTau = Convert.ToInt32(r["MaTruongTau"]),
                    MaGaNhanBan = Convert.ToInt32(r["MaGaNhanBan"]),
                    MaGaBanGiao = Convert.ToInt32(r["MaGaBanGiao"]),
                    TrangThai = Chuoi(r, "TrangThai"),
                    PhienBan = (byte[])r["PhienBan"],
                    Chuyen = chuyenTheoMa[Convert.ToInt32(r["MaChuyenTau"])]
                })
                .ToList();

            // --- Moi phieu kiem tra (so kiem tra + lich su trong ho so), kem chuyen cua phieu ---
            var kiemTra = PhienGiaoDich.TruyVan(gd, @"
                SELECT ks.MaKiemTra, ks.MaNhanVien, ks.MaChuyenTau, ks.NongDoConMgL, ks.SoGioNghiTruocCa,
                       ks.ThoiDiemKiemTra, mt.SoHieuMacTau, ct.NgayXuatPhat
                FROM nhansu.KiemTraSucKhoe ks
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = ks.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau")
                .Rows.Cast<DataRow>()
                .Select(r => new KiemTraLenBanHienThi
                {
                    MaKiemTra = Convert.ToInt32(r["MaKiemTra"]),
                    MaNhanVien = Convert.ToInt32(r["MaNhanVien"]),
                    MaChuyenTau = Convert.ToInt32(r["MaChuyenTau"]),
                    NongDoConMgL = Convert.ToDecimal(r["NongDoConMgL"]),
                    SoGioNghiTruocCa = Convert.ToDecimal(r["SoGioNghiTruocCa"]),
                    ThoiDiemKiemTra = Convert.ToDateTime(r["ThoiDiemKiemTra"]),
                    SoHieuMacTau = Chuoi(r, "SoHieuMacTau"),
                    NgayXuatPhatChuyen = Convert.ToDateTime(r["NgayXuatPhat"])
                })
                .ToList();

            var dl = new DuLieuNhanSu(nhanVien, phanCong, kiemTra, chuyenTau, ga, hangBang, bayGio);
            foreach (var kt in kiemTra) kt.GanNhanVien(dl.TimNhanVien(kt.MaNhanVien));
            return dl;
        }

        // Danh muc ga theo ly trinh (ga dong quan R10, don vi "Ga ..." cua nhan vien ban ve)
        public List<GaNhanSu> LayGa(PhienGiaoDich? gd = null)
            => PhienGiaoDich.TruyVan(gd, "SELECT MaGa, MaGaCode, TenGa FROM hatang.Ga ORDER BY LyTrinhKm, MaGa")
               .Rows.Cast<DataRow>()
               .Select(r => new GaNhanSu(Convert.ToInt32(r["MaGa"]), Chuoi(r, "MaGaCode"), Chuoi(r, "TenGa")))
               .ToList();

        // Hang bang lai hop le = ma dong dau may (D19E, D13E ...)
        public List<string> LayHangBangLai(PhienGiaoDich? gd = null)
            => PhienGiaoDich.TruyVan(gd, "SELECT MaDongCode FROM phuongtien.DongDauMay ORDER BY MaDongCode")
               .Rows.Cast<DataRow>().Select(r => Chuoi(r, "MaDongCode")).ToList();

        // Gio hien tai cua may chu CSDL (gio Viet Nam) - moc "bay gio" khi Service kiem luat
        public DateTime LayGioMayChu(PhienGiaoDich? gd = null)
            => Convert.ToDateTime(PhienGiaoDich.LayGiaTri(gd, "SELECT SYSDATETIME()"));

        // =====================================================================
        // NHAN VIEN
        // =====================================================================

        // Ma da cap cua mot tien to, ke ca ma cu trong lich su doi ma (khong cap lai). Khoa UPDLOCK +
        // HOLDLOCK (khoa ca khoang LIKE) toi het giao dich: 2 nguoi them cung tien to cung luc thi
        // nguoi sau cho, khong cap trung so.
        public List<string> LayMaTheoTienTo(string tienTo, PhienGiaoDich gd)
            => PhienGiaoDich.TruyVan(gd, @"
                SELECT MaNVCode FROM nhansu.NhanVien WITH (UPDLOCK, HOLDLOCK)
                WHERE MaNVCode LIKE @mau
                UNION ALL
                SELECT MaNVCodeCu FROM nhansu.LichSuMaNhanVien WITH (UPDLOCK, HOLDLOCK)
                WHERE MaNVCodeCu LIKE @mau",
                new SqlParameter("@mau", SqlDbType.VarChar, 20) { Value = tienTo + "[_]%" })
               .Rows.Cast<DataRow>().Select(r => Chuoi(r, "MaNVCode")).ToList();

        // Ghi mot lan doi ma trong cung giao dich voi UPDATE ho so (goc = ban truoc khi sua)
        public void ThemLichSuMa(NhanVienHienThi goc, NhanVienHienThi moi, int? maTaiKhoan, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                INSERT INTO nhansu.LichSuMaNhanVien (MaNhanVien, MaNVCodeCu, MaNVCodeMoi, ChucDanhCu, ChucDanhMoi,
                                                     DonViCu, DonViMoi, ThoiDiem, MaTaiKhoan)
                VALUES (@id, @cu, @moi, @cdCu, @cdMoi, @dvCu, @dvMoi, SYSDATETIME(), @tk)",
                new SqlParameter("@id", goc.MaNhanVien),
                new SqlParameter("@cu", SqlDbType.VarChar, 20) { Value = goc.MaNVCode },
                new SqlParameter("@moi", SqlDbType.VarChar, 20) { Value = moi.MaNVCode },
                new SqlParameter("@cdCu", SqlDbType.NVarChar, 50) { Value = goc.ChucDanh },
                new SqlParameter("@cdMoi", SqlDbType.NVarChar, 50) { Value = moi.ChucDanh },
                new SqlParameter("@dvCu", SqlDbType.NVarChar, 100) { Value = goc.DonViChuQuan },
                new SqlParameter("@dvMoi", SqlDbType.NVarChar, 100) { Value = moi.DonViChuQuan },
                new SqlParameter("@tk", SqlDbType.Int) { Value = (object?)maTaiKhoan ?? DBNull.Value });

        // Bang co trigger (trg_NhanVien_KhongDungLaiMaCu, 12/10) nen khong dung OUTPUT INSERTED (loi 334)
        public int ThemNhanVien(NhanVienHienThi nv, PhienGiaoDich gd)
            => Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd, @"
                INSERT INTO nhansu.NhanVien (MaNVCode, HoTen, SoDienThoai, ChucDanh, HangBangLai, HanKhamSucKhoe,
                                             DonViChuQuan, TrangThai, NgayTao, NgaySinh)
                VALUES (@ma, @ten, @sdt, @cd, @bang, @han, @dv, @tt, SYSDATETIME(), @ns);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                ThamSoNhanVien(nv)));

        public int CapNhatNhanVien(NhanVienHienThi nv, byte[] phienBan, PhienGiaoDich gd)
        {
            var thamSo = ThamSoNhanVien(nv).ToList();
            thamSo.Add(new SqlParameter("@id", nv.MaNhanVien));
            thamSo.Add(new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });
            return PhienGiaoDich.ThucThi(gd, @"
                UPDATE nhansu.NhanVien
                SET MaNVCode = @ma, HoTen = @ten, SoDienThoai = @sdt, ChucDanh = @cd, HangBangLai = @bang,
                    HanKhamSucKhoe = @han, DonViChuQuan = @dv, TrangThai = @tt, NgaySinh = @ns
                WHERE MaNhanVien = @id AND PhienBan = @pb",
                thamSo.ToArray());
        }

        // 14/10: moi du lieu tro toi ho so (TOAN bang, khong theo cua so ngay cua anh chup).
        // Con dong nao thi khong xoa duoc - Service de nghi chuyen sang "Da nghi viec".
        public List<SoThamChieuDTO> DemThamChieuNhanVien(int maNhanVien, PhienGiaoDich? gd = null)
            => PhuongTienRepository.DocThamChieu(PhienGiaoDich.TruyVan(gd, @"
                SELECT 'KIP_LAI' AS Loai, COUNT(*) AS SoLuong,
                       ISNULL((SELECT STRING_AGG(x.Nhan, ', ') WITHIN GROUP (ORDER BY x.Gio DESC)
                               FROM (SELECT TOP 3 CONCAT(mt.SoHieuMacTau, ' ', FORMAT(ct.NgayXuatPhat, 'dd/MM')) AS Nhan,
                                            ct.GioXuatPhatKH AS Gio
                                     FROM nhansu.PhanCongKipLai k
                                     JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = k.MaChuyenTau
                                     JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                                     WHERE @id IN (k.MaLaiTau, k.MaPhuLai, k.MaTruongTau)
                                     ORDER BY ct.GioXuatPhatKH DESC) x), '') AS ViDu
                FROM nhansu.PhanCongKipLai WHERE @id IN (MaLaiTau, MaPhuLai, MaTruongTau)
                UNION ALL
                SELECT 'KIEM_TRA', COUNT(*), '' FROM nhansu.KiemTraSucKhoe WHERE MaNhanVien = @id
                UNION ALL
                SELECT 'TAI_KHOAN', COUNT(*), ISNULL(STRING_AGG(TenDangNhap, ', '), '')
                FROM phanquyen.TaiKhoan WHERE MaNhanVien = @id
                UNION ALL
                SELECT 'LICH_SU_MA', COUNT(*), ISNULL(STRING_AGG(MaNVCodeCu, ', '), '')
                FROM nhansu.LichSuMaNhanVien WHERE MaNhanVien = @id",
                new SqlParameter("@id", maNhanVien)));

        // Xoa dung phien ban da mo. 0 dong = nguoi khac vua sua / xoa / kip vua doi trang thai ho so.
        public int XoaNhanVien(int maNhanVien, byte[] phienBan, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                DELETE FROM nhansu.NhanVien WHERE MaNhanVien = @id AND PhienBan = @pb",
                new SqlParameter("@id", maNhanVien),
                new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });

        // Trang thai di theo kip (R13) - goi trong cung giao dich nhan ban / ban giao
        public void DatTrangThaiNhanVien(int maNhanVien, string trangThai, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd,
                "UPDATE nhansu.NhanVien SET TrangThai = @tt WHERE MaNhanVien = @id",
                new SqlParameter("@tt", SqlDbType.VarChar, 20) { Value = trangThai },
                new SqlParameter("@id", maNhanVien));

        // =====================================================================
        // KIP LAI
        // =====================================================================

        // Kip trong CSDL trung gio voi kip sap ghi (them / sua / doi trang thai). CUNG dieu kien voi
        // trigger trg_PhanCongKipLai_CheckTrungGio (migration V20261011_1500) nhung doc tren toan bang,
        // khong theo cua so anh chup: Service goi ngay truoc khi ghi de bao ro ten nguoi + chuyen
        // thay vi cau chung cua trigger. Sua trigger thi sua ca cau nay.
        public List<KipTrungGioDTO> TimKipTrungGio(PhanCongKipHienThi kip, PhienGiaoDich gd)
            => PhienGiaoDich.TruyVan(gd, @"
                SELECT n.VaiTro, nv.HoTen,
                       CASE n.MaNhanVien WHEN k.MaLaiTau THEN 'LAI_TAU' WHEN k.MaPhuLai THEN 'PHU_LAI' ELSE 'TRUONG_TAU' END AS VaiTroKipKia,
                       CASE WHEN k.MaChuyenTau = ct1.MaChuyenTau THEN 1 ELSE 0 END AS CungChuyen,
                       mt.SoHieuMacTau, ct2.GioXuatPhatKH, ct2.GioVeDichKH,
                       gn.TenGa AS TenGaNhanBan, gg.TenGa AS TenGaBanGiao
                FROM (VALUES (@lt, 'LAI_TAU', 1), (@pl, 'PHU_LAI', 2), (@tt, 'TRUONG_TAU', 3)) n(MaNhanVien, VaiTro, ThuTu)
                JOIN nhansu.PhanCongKipLai k ON k.MaPhanCong <> @id
                                            AND n.MaNhanVien IN (k.MaLaiTau, k.MaPhuLai, k.MaTruongTau)
                JOIN vanhanh.ChuyenTau ct1 ON ct1.MaChuyenTau = @ct
                JOIN vanhanh.ChuyenTau ct2 ON ct2.MaChuyenTau = k.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct2.MaMacTau
                JOIN nhansu.NhanVien nv ON nv.MaNhanVien = n.MaNhanVien
                JOIN hatang.Ga gn ON gn.MaGa = k.MaGaNhanBan
                JOIN hatang.Ga gg ON gg.MaGa = k.MaGaBanGiao
                WHERE ct1.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
                  AND ct2.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
                  AND (
                        (k.MaChuyenTau <> ct1.MaChuyenTau
                         AND (CASE WHEN ct1.GioXuatPhatKH > ct2.GioXuatPhatKH THEN ct1.GioXuatPhatKH ELSE ct2.GioXuatPhatKH END)
                           < (CASE WHEN ct1.GioVeDichKH < ct2.GioVeDichKH THEN ct1.GioVeDichKH ELSE ct2.GioVeDichKH END))
                     OR (k.MaChuyenTau = ct1.MaChuyenTau
                         AND NOT (n.VaiTro = 'TRUONG_TAU' AND k.MaTruongTau = n.MaNhanVien))
                      )
                ORDER BY n.ThuTu, ct2.GioXuatPhatKH, k.MaPhanCong",
                new SqlParameter("@id", kip.MaPhanCong),
                new SqlParameter("@ct", kip.MaChuyenTau),
                new SqlParameter("@lt", kip.MaLaiTau),
                new SqlParameter("@pl", kip.MaPhuLai),
                new SqlParameter("@tt", kip.MaTruongTau))
               .Rows.Cast<DataRow>()
               .Select(r => new KipTrungGioDTO(
                   Chuoi(r, "VaiTro"), Chuoi(r, "HoTen"), Chuoi(r, "VaiTroKipKia"), Convert.ToInt32(r["CungChuyen"]) == 1,
                   Chuoi(r, "SoHieuMacTau"), Convert.ToDateTime(r["GioXuatPhatKH"]), Convert.ToDateTime(r["GioVeDichKH"]),
                   Chuoi(r, "TenGaNhanBan"), Chuoi(r, "TenGaBanGiao")))
               .ToList();

        // Bang co trigger nen khong dung OUTPUT INSERTED (loi 334) -> SCOPE_IDENTITY()
        public int ThemKip(PhanCongKipHienThi kip, PhienGiaoDich gd)
            => Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd, @"
                INSERT INTO nhansu.PhanCongKipLai (MaChuyenTau, MaLaiTau, MaPhuLai, MaTruongTau, MaGaNhanBan, MaGaBanGiao, TrangThai)
                VALUES (@ct, @lt, @pl, @tt, @nhan, @giao, 'DA_PHAN_CONG');
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                ThamSoKip(kip)));

        // Chi sua kip con "Da phan cong" va dung phien ban da mo
        public int CapNhatKip(PhanCongKipHienThi kip, byte[] phienBan, PhienGiaoDich gd)
        {
            var thamSo = ThamSoKip(kip).ToList();
            thamSo.Add(new SqlParameter("@id", kip.MaPhanCong));
            thamSo.Add(new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });
            return PhienGiaoDich.ThucThi(gd, @"
                UPDATE nhansu.PhanCongKipLai
                SET MaLaiTau = @lt, MaPhuLai = @pl, MaTruongTau = @tt, MaGaNhanBan = @nhan, MaGaBanGiao = @giao
                WHERE MaPhanCong = @id AND PhienBan = @pb AND TrangThai = 'DA_PHAN_CONG' AND MaChuyenTau = @ct",
                thamSo.ToArray());
        }

        public int XoaKip(int maPhanCong, byte[] phienBan, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                DELETE FROM nhansu.PhanCongKipLai
                WHERE MaPhanCong = @id AND PhienBan = @pb AND TrangThai = 'DA_PHAN_CONG'",
                new SqlParameter("@id", maPhanCong),
                new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });

        public int DoiTrangThaiKip(int maPhanCong, byte[] phienBan, string tuTrangThai, string sangTrangThai, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                UPDATE nhansu.PhanCongKipLai SET TrangThai = @moi
                WHERE MaPhanCong = @id AND PhienBan = @pb AND TrangThai = @cu",
                new SqlParameter("@moi", SqlDbType.VarChar, 20) { Value = sangTrangThai },
                new SqlParameter("@cu", SqlDbType.VarChar, 20) { Value = tuTrangThai },
                new SqlParameter("@id", maPhanCong),
                new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });

        // =====================================================================
        // KIEM TRA LEN BAN
        // =====================================================================

        public (int MaKiemTra, DateTime ThoiDiem) ThemKiemTra(int maNhanVien, int maChuyenTau, decimal nongDoCon,
                                                             decimal soGioNghi, PhienGiaoDich gd)
        {
            var bang = PhienGiaoDich.TruyVan(gd, @"
                INSERT INTO nhansu.KiemTraSucKhoe (MaNhanVien, MaChuyenTau, NongDoConMgL, SoGioNghiTruocCa, ThoiDiemKiemTra)
                OUTPUT INSERTED.MaKiemTra, INSERTED.ThoiDiemKiemTra
                VALUES (@nv, @ct, @con, @nghi, SYSDATETIME())",
                new SqlParameter("@nv", maNhanVien),
                new SqlParameter("@ct", maChuyenTau),
                new SqlParameter("@con", SqlDbType.Decimal) { Precision = 3, Scale = 2, Value = nongDoCon },
                new SqlParameter("@nghi", SqlDbType.Decimal) { Precision = 4, Scale = 1, Value = soGioNghi });
            var r = bang.Rows[0];
            return (Convert.ToInt32(r["MaKiemTra"]), Convert.ToDateTime(r["ThoiDiemKiemTra"]));
        }

        // =====================================================================
        // TIEN ICH
        // =====================================================================

        private static NhanVienHienThi DocNhanVien(DataRow r) => new()
        {
            MaNhanVien = Convert.ToInt32(r["MaNhanVien"]),
            MaNVCode = Chuoi(r, "MaNVCode"),
            HoTen = Chuoi(r, "HoTen"),
            SoDienThoai = Chuoi(r, "SoDienThoai"),
            ChucDanh = Chuoi(r, "ChucDanh"),
            HangBangLai = r["HangBangLai"] == DBNull.Value ? null : Chuoi(r, "HangBangLai"),
            HanKhamSucKhoe = Convert.ToDateTime(r["HanKhamSucKhoe"]),
            NgaySinh = r["NgaySinh"] == DBNull.Value ? null : Convert.ToDateTime(r["NgaySinh"]),
            DonViChuQuan = Chuoi(r, "DonViChuQuan"),
            TrangThai = Chuoi(r, "TrangThai"),
            NgayTao = Convert.ToDateTime(r["NgayTao"]),
            PhienBan = (byte[])r["PhienBan"],
            TenDangNhap = r["TenDangNhap"] == DBNull.Value ? null : Chuoi(r, "TenDangNhap"),
            TaiKhoanHoatDong = r["DangHoatDong"] != DBNull.Value && Convert.ToBoolean(r["DangHoatDong"]),
            LanDangNhapCuoi = r["LanDangNhapCuoi"] == DBNull.Value ? null : Convert.ToDateTime(r["LanDangNhapCuoi"]),
            SoTaiKhoan = Convert.ToInt32(r["SoTaiKhoan"])
        };

        private static SqlParameter[] ThamSoNhanVien(NhanVienHienThi nv) => new[]
        {
            new SqlParameter("@ma", SqlDbType.VarChar, 20) { Value = nv.MaNVCode },
            new SqlParameter("@ten", SqlDbType.NVarChar, 100) { Value = nv.HoTen },
            new SqlParameter("@sdt", SqlDbType.VarChar, 20) { Value = nv.SoDienThoai },
            new SqlParameter("@cd", SqlDbType.NVarChar, 50) { Value = nv.ChucDanh },
            new SqlParameter("@bang", SqlDbType.VarChar, 10) { Value = (object?)nv.HangBangLai ?? DBNull.Value },
            new SqlParameter("@han", SqlDbType.Date) { Value = nv.HanKhamSucKhoe.Date },
            new SqlParameter("@ns", SqlDbType.Date) { Value = (object?)nv.NgaySinh?.Date ?? DBNull.Value },
            new SqlParameter("@dv", SqlDbType.NVarChar, 100) { Value = nv.DonViChuQuan },
            new SqlParameter("@tt", SqlDbType.VarChar, 20) { Value = nv.TrangThai }
        };

        private static SqlParameter[] ThamSoKip(PhanCongKipHienThi kip) => new[]
        {
            new SqlParameter("@ct", kip.MaChuyenTau),
            new SqlParameter("@lt", kip.MaLaiTau),
            new SqlParameter("@pl", kip.MaPhuLai),
            new SqlParameter("@tt", kip.MaTruongTau),
            new SqlParameter("@nhan", kip.MaGaNhanBan),
            new SqlParameter("@giao", kip.MaGaBanGiao)
        };

        private static GaNhanSu TimGa(Dictionary<int, GaNhanSu> gaTheoMa, int maGa)
            => gaTheoMa.TryGetValue(maGa, out var g) ? g : new GaNhanSu(maGa, $"#{maGa}", $"Ga #{maGa}");

        private static string Chuoi(DataRow r, string cot) => r[cot] == DBNull.Value ? "" : r[cot].ToString() ?? "";
    }
}
