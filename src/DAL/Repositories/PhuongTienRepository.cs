using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using DTO.Common;
using ET.PhuongTien;

namespace DAL.Repositories
{
    // =========================================================================
    // PHAN HE QUAN LY PHUONG TIEN (Dau may, Toa xe, Doan tau & Ghe)
    // Doc: danh sach / ly lich / thong ke. Ghi: them / sua / xoa dau may va toa xe
    // (xoa chi khi chua co du lieu nao tro toi - DemThamChieu*, 14/10).
    // Lap doan tau (vanhanh.DoanTau + ChiTietDoanTau) nam o LapTauRepository.
    //
    // Cac ham ghi nhan PhienGiaoDich de Service gom kiem tra + ghi + nhat ky vao
    // MOT giao dich. SqlException duoc dich sang LoiGhiCsdlException (tieng Viet).
    // =========================================================================
    public class PhuongTienRepository
    {
        // Gia tri bo loc trang thai: moi trang thai tru NGUNG_KHAI_THAC (migration V20261014_0900)
        public const string TrangThaiDangKhaiThac = "DANG_KHAI_THAC";

        // ---------------------------------------------------------------------
        // 1. DAU MAY
        // ---------------------------------------------------------------------

        public DataTable LayDanhSachDauMay(string? tuKhoa = null, string? donViQuanLy = null, string? trangThai = null)
        {
            // KmBaoDuongR1GanNhat: km o lan R1 HOAC R2 gan nhat (R2 bao gom R1)
            // KmBaoDuongR2GanNhat: km o lan R2 gan nhat
            // -> PhuongTienService.TinhTienDoBaoDuong tinh theo chu ky tu lan gan nhat (khop Bao tri)
            string sql = @"
                SELECT dm.MaDauMay,
                       dm.SoHieuDauMay,
                       dm.MaDongDauMay,
                       dm.DonViQuanLy,
                       dm.NamSanXuat,
                       dm.SoKmTichLuy,
                       dm.TrangThai,
                       ddm.MaDongCode,
                       ddm.NhaSanXuat,
                       ddm.CongSuatHP,
                       ddm.TocDoToiDaKmh,
                       ddm.SucKeoToiDaTan,
                       ddm.DungTichBonDauLit,
                       ddm.TrongLuongTan,
                       ddm.ChieuDaiM,
                       DATEDIFF(YEAR, DATEFROMPARTS(dm.NamSanXuat, 1, 1), GETDATE()) AS TuoiKhaiThac,
                       r1.SoKmTaiThoiDiem AS KmBaoDuongR1GanNhat,
                       r2.SoKmTaiThoiDiem AS KmBaoDuongR2GanNhat,
                       ISNULL((SELECT COUNT(*)
                               FROM vanhanh.DoanTau dt
                               JOIN vanhanh.ChuyenTau ct ON dt.MaChuyenTau = ct.MaChuyenTau
                               WHERE (dt.MaDauMayChinh = dm.MaDauMay OR dt.MaDauMayDay = dm.MaDauMay)
                                 AND ct.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')), 0) AS SoChuyenDangNhan,
                       (SELECT TOP 1 mt.SoHieuMacTau
                        FROM vanhanh.DoanTau dt
                        JOIN vanhanh.ChuyenTau ct ON dt.MaChuyenTau = ct.MaChuyenTau
                        JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                        WHERE (dt.MaDauMayChinh = dm.MaDauMay OR dt.MaDauMayDay = dm.MaDauMay)
                          AND ct.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
                        ORDER BY ct.GioXuatPhatKH ASC) AS MacTauDangNhan
                FROM phuongtien.DauMay dm
                JOIN phuongtien.DongDauMay ddm ON dm.MaDongDauMay = ddm.MaDongDauMay
                OUTER APPLY (SELECT TOP 1 bd.SoKmTaiThoiDiem
                             FROM baotri.NhatKyBaoDuong bd
                             WHERE bd.MaDauMay = dm.MaDauMay AND bd.CapBaoDuong IN ('R1', 'R2')
                             ORDER BY bd.ThoiDiemHoanThanh DESC, bd.MaBaoDuong DESC) r1
                OUTER APPLY (SELECT TOP 1 bd.SoKmTaiThoiDiem
                             FROM baotri.NhatKyBaoDuong bd
                             WHERE bd.MaDauMay = dm.MaDauMay AND bd.CapBaoDuong = 'R2'
                             ORDER BY bd.ThoiDiemHoanThanh DESC, bd.MaBaoDuong DESC) r2
                WHERE 1 = 1";

            var ps = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                sql += @" AND (dm.SoHieuDauMay LIKE @kw
                            OR ddm.MaDongCode LIKE @kw
                            OR dm.DonViQuanLy LIKE @kw
                            OR ddm.NhaSanXuat LIKE @kw)";
                ps.Add(new SqlParameter("@kw", $"%{tuKhoa.Trim()}%"));
            }

            if (!string.IsNullOrWhiteSpace(donViQuanLy) && donViQuanLy != "ALL")
            {
                sql += " AND dm.DonViQuanLy = @depot";
                ps.Add(new SqlParameter("@depot", donViQuanLy));
            }

            // DANG_KHAI_THAC (mac dinh cua bo loc) = moi trang thai tru Ngung khai thac
            if (trangThai == TrangThaiDangKhaiThac)
            {
                sql += " AND dm.TrangThai <> 'NGUNG_KHAI_THAC'";
            }
            else if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "ALL")
            {
                sql += " AND dm.TrangThai = @tt";
                ps.Add(new SqlParameter("@tt", trangThai));
            }

            // Dau may ngung khai thac xuong cuoi danh sach
            sql += " ORDER BY CASE WHEN dm.TrangThai = 'NGUNG_KHAI_THAC' THEN 1 ELSE 0 END, dm.SoHieuDauMay ASC";

            return DatabaseHelper.ExecuteQuery(sql, ps.Count > 0 ? ps.ToArray() : null);
        }

        public DataTable LayDanhSachDonViQuanLy()
        {
            string sql = @"SELECT DISTINCT DonViQuanLy
                           FROM phuongtien.DauMay
                           WHERE DonViQuanLy IS NOT NULL AND LTRIM(RTRIM(DonViQuanLy)) <> ''
                           ORDER BY DonViQuanLy";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        // Dong co nhieu dau may nhat dung dau: dialog them moi chon san dong pho bien nhat
        public DataTable LayDanhSachDongDauMay()
        {
            string sql = @"SELECT ddm.MaDongDauMay, ddm.MaDongCode, ddm.NhaSanXuat, ddm.CongSuatHP, ddm.TocDoToiDaKmh,
                                  ddm.SucKeoToiDaTan, ddm.DungTichBonDauLit, ddm.TrongLuongTan, ddm.ChieuDaiM
                           FROM phuongtien.DongDauMay ddm
                           ORDER BY (SELECT COUNT(*) FROM phuongtien.DauMay dm WHERE dm.MaDongDauMay = ddm.MaDongDauMay) DESC,
                                    ddm.MaDongCode";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        // Lich su van dung cua mot dau may (cac chuyen da/dang nhan keo)
        public DataTable LayLichSuVanDung(int maDauMay)
        {
            string sql = @"
                SELECT ct.MaChuyenTau,
                       mt.SoHieuMacTau,
                       mt.LoaiTau,
                       ct.NgayXuatPhat,
                       ct.GioXuatPhatKH,
                       ct.GioVeDichKH,
                       ct.TrangThai,
                       ct.SoPhutTreLuyKe,
                       CASE WHEN dt.MaDauMayChinh = @id THEN N'Kéo chính' ELSE N'Đẩy phụ' END AS VaiTroDauMay,
                       g1.TenGa AS TenGaDi,
                       g2.TenGa AS TenGaDen
                FROM vanhanh.DoanTau dt
                JOIN vanhanh.ChuyenTau ct ON dt.MaChuyenTau = ct.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                JOIN hatang.Ga g1 ON mt.MaGaDi = g1.MaGa
                JOIN hatang.Ga g2 ON mt.MaGaDen = g2.MaGa
                WHERE dt.MaDauMayChinh = @id OR dt.MaDauMayDay = @id
                ORDER BY ct.NgayXuatPhat DESC, ct.GioXuatPhatKH DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@id", maDauMay) });
        }

        public DauMay? LayDauMay(int maDauMay, PhienGiaoDich? gd = null)
        {
            DataTable dt = PhienGiaoDich.TruyVan(gd,
                @"SELECT MaDauMay, SoHieuDauMay, MaDongDauMay, DonViQuanLy, NamSanXuat, SoKmTichLuy, TrangThai
                  FROM phuongtien.DauMay WHERE MaDauMay = @id",
                new SqlParameter("@id", maDauMay));
            if (dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            return new DauMay
            {
                MaDauMay = Convert.ToInt32(r["MaDauMay"]),
                SoHieuDauMay = r["SoHieuDauMay"].ToString() ?? "",
                MaDongDauMay = Convert.ToInt32(r["MaDongDauMay"]),
                DonViQuanLy = r["DonViQuanLy"].ToString() ?? "",
                NamSanXuat = Convert.ToInt32(r["NamSanXuat"]),
                SoKmTichLuy = Convert.ToDecimal(r["SoKmTichLuy"]),
                TrangThai = r["TrangThai"].ToString() ?? ""
            };
        }

        public bool SoHieuDauMayDaTonTai(string soHieu, PhienGiaoDich? gd = null)
            => Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd,
                   "SELECT COUNT(*) FROM phuongtien.DauMay WHERE SoHieuDauMay = @sh",
                   new SqlParameter("@sh", SqlDbType.VarChar, 20) { Value = soHieu })) > 0;

        public string? LayMaDongCode(int maDongDauMay, PhienGiaoDich? gd = null)
            => PhienGiaoDich.LayGiaTri(gd, "SELECT MaDongCode FROM phuongtien.DongDauMay WHERE MaDongDauMay = @id",
                   new SqlParameter("@id", maDongDauMay)) as string;

        // So km o lan bao duong R1 / R2 gan nhat (null = chua bao duong lan nao)
        public decimal? LayKmBaoDuongGanNhat(int maDauMay, PhienGiaoDich? gd = null)
        {
            object? kq = PhienGiaoDich.LayGiaTri(gd,
                @"SELECT MAX(SoKmTaiThoiDiem) FROM baotri.NhatKyBaoDuong
                  WHERE MaDauMay = @id AND CapBaoDuong IN ('R1', 'R2')",
                new SqlParameter("@id", maDauMay));
            return kq is null or DBNull ? null : Convert.ToDecimal(kq);
        }

        // Cac chuyen CON HIEU LUC (chua hoan thanh / huy) dang dung dau may lam may keo hoac may day
        public DataTable LayChuyenConHieuLucCuaDauMay(int maDauMay, PhienGiaoDich? gd = null)
            => PhienGiaoDich.TruyVan(gd, @"
                SELECT ct.MaChuyenTau, mt.SoHieuMacTau, ct.TrangThai, ct.GioXuatPhatKH, ct.GioVeDichKH,
                       CASE WHEN dt.MaDauMayChinh = @id THEN 'KEO_CHINH' ELSE 'DAY' END AS VaiTro
                FROM vanhanh.DoanTau dt
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                WHERE (dt.MaDauMayChinh = @id OR dt.MaDauMayDay = @id)
                  AND ct.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
                ORDER BY ct.GioXuatPhatKH",
                new SqlParameter("@id", maDauMay));

        public int ThemDauMay(DauMay dm, PhienGiaoDich gd)
        {
            try
            {
                return Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd, @"
                    INSERT INTO phuongtien.DauMay (SoHieuDauMay, MaDongDauMay, DonViQuanLy, NamSanXuat, SoKmTichLuy, TrangThai)
                    VALUES (@sh, @dong, @dv, @nam, @km, @tt);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new SqlParameter("@sh", SqlDbType.VarChar, 20) { Value = dm.SoHieuDauMay },
                    new SqlParameter("@dong", dm.MaDongDauMay),
                    new SqlParameter("@dv", SqlDbType.NVarChar, 50) { Value = dm.DonViQuanLy },
                    new SqlParameter("@nam", dm.NamSanXuat),
                    new SqlParameter("@km", SqlDbType.Decimal) { Precision = 9, Scale = 1, Value = dm.SoKmTichLuy },
                    new SqlParameter("@tt", SqlDbType.VarChar, 20) { Value = dm.TrangThai }));
            }
            catch (SqlException ex)
            {
                throw DichLoiSqlPhuongTien.Dich(ex);
            }
        }

        // Ghi de co kiem tra: chi cap nhat khi ban ghi trong CSDL van y nhu luc nguoi dung mo
        // dialog (goc). 0 dong = da co nguoi khac sua truoc -> Service bao "nap lai".
        // So hieu va dong may khong doi khi sua (Service da chan).
        public int CapNhatDauMay(DauMay moi, DauMay goc, PhienGiaoDich gd)
        {
            try
            {
                return PhienGiaoDich.ThucThi(gd, @"
                    UPDATE phuongtien.DauMay
                    SET DonViQuanLy = @dv, NamSanXuat = @nam, SoKmTichLuy = @km, TrangThai = @tt
                    WHERE MaDauMay = @id
                      AND SoHieuDauMay = @shGoc AND MaDongDauMay = @dongGoc AND DonViQuanLy = @dvGoc
                      AND NamSanXuat = @namGoc AND SoKmTichLuy = @kmGoc AND TrangThai = @ttGoc",
                    new SqlParameter("@id", moi.MaDauMay),
                    new SqlParameter("@dv", SqlDbType.NVarChar, 50) { Value = moi.DonViQuanLy },
                    new SqlParameter("@nam", moi.NamSanXuat),
                    new SqlParameter("@km", SqlDbType.Decimal) { Precision = 9, Scale = 1, Value = moi.SoKmTichLuy },
                    new SqlParameter("@tt", SqlDbType.VarChar, 20) { Value = moi.TrangThai },
                    new SqlParameter("@shGoc", SqlDbType.VarChar, 20) { Value = goc.SoHieuDauMay },
                    new SqlParameter("@dongGoc", goc.MaDongDauMay),
                    new SqlParameter("@dvGoc", SqlDbType.NVarChar, 50) { Value = goc.DonViQuanLy },
                    new SqlParameter("@namGoc", goc.NamSanXuat),
                    new SqlParameter("@kmGoc", SqlDbType.Decimal) { Precision = 9, Scale = 1, Value = goc.SoKmTichLuy },
                    new SqlParameter("@ttGoc", SqlDbType.VarChar, 20) { Value = goc.TrangThai });
            }
            catch (SqlException ex)
            {
                throw DichLoiSqlPhuongTien.Dich(ex);
            }
        }

        // Moi du lieu tro toi dau may (ca chuyen da xong / da huy): con dong nao thi khong xoa
        // duoc (khoa ngoai + mat lich su), Service de nghi chuyen sang Ngung khai thac.
        public List<SoThamChieuDTO> DemThamChieuDauMay(int maDauMay, PhienGiaoDich? gd = null)
            => DocThamChieu(PhienGiaoDich.TruyVan(gd, @"
                SELECT 'DOAN_TAU' AS Loai, COUNT(*) AS SoLuong,
                       ISNULL((SELECT STRING_AGG(x.Nhan, ', ') WITHIN GROUP (ORDER BY x.Gio DESC)
                               FROM (SELECT TOP 3 CONCAT(mt.SoHieuMacTau, ' ', FORMAT(ct.NgayXuatPhat, 'dd/MM')) AS Nhan,
                                            ct.GioXuatPhatKH AS Gio
                                     FROM vanhanh.DoanTau d
                                     JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = d.MaChuyenTau
                                     JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                                     WHERE d.MaDauMayChinh = @id OR d.MaDauMayDay = @id
                                     ORDER BY ct.GioXuatPhatKH DESC) x), '') AS ViDu
                FROM vanhanh.DoanTau WHERE MaDauMayChinh = @id OR MaDauMayDay = @id
                UNION ALL
                SELECT 'CAP_NHIEN_LIEU', COUNT(*), '' FROM baotri.NhatKyCapNhienLieu WHERE MaDauMay = @id
                UNION ALL
                SELECT 'BAO_DUONG', COUNT(*), '' FROM baotri.NhatKyBaoDuong WHERE MaDauMay = @id",
                new SqlParameter("@id", maDauMay)));

        // Xoa co kiem tra nhu CapNhatDauMay: chi xoa khi ban ghi van y nhu luc nguoi dung chon.
        // 0 dong = nguoi khac vua sua / xoa. Con du lieu tro toi -> loi 547 (Service da dem truoc).
        public int XoaDauMay(DauMay goc, PhienGiaoDich gd)
        {
            try
            {
                return PhienGiaoDich.ThucThi(gd, @"
                    DELETE FROM phuongtien.DauMay
                    WHERE MaDauMay = @id
                      AND SoHieuDauMay = @shGoc AND MaDongDauMay = @dongGoc AND DonViQuanLy = @dvGoc
                      AND NamSanXuat = @namGoc AND SoKmTichLuy = @kmGoc AND TrangThai = @ttGoc",
                    new SqlParameter("@id", goc.MaDauMay),
                    new SqlParameter("@shGoc", SqlDbType.VarChar, 20) { Value = goc.SoHieuDauMay },
                    new SqlParameter("@dongGoc", goc.MaDongDauMay),
                    new SqlParameter("@dvGoc", SqlDbType.NVarChar, 50) { Value = goc.DonViQuanLy },
                    new SqlParameter("@namGoc", goc.NamSanXuat),
                    new SqlParameter("@kmGoc", SqlDbType.Decimal) { Precision = 9, Scale = 1, Value = goc.SoKmTichLuy },
                    new SqlParameter("@ttGoc", SqlDbType.VarChar, 20) { Value = goc.TrangThai });
            }
            catch (SqlException ex)
            {
                throw DichLoiSqlPhuongTien.Dich(ex);
            }
        }

        // ---------------------------------------------------------------------
        // 2. DOI TOA XE (phuongtien.ToaXe - tai san vat ly)
        // ---------------------------------------------------------------------

        public DataTable LayDanhSachToaXeDoi(string? tuKhoa = null, int? maChungLoai = null, string? trangThai = null)
        {
            string sql = @"
                SELECT tx.MaToaXe,
                       tx.SoHieuToaXe,
                       tx.MaChungLoai,
                       tx.TuTrongTan,
                       tx.TaiTrongToiDaTan,
                       tx.TrangThai,
                       cl.MaChungLoaiCode,
                       cl.TenMoTa,
                       cl.ChieuDaiChuanM,
                       cl.SoTruc,
                       CAST((tx.TuTrongTan + tx.TaiTrongToiDaTan) / NULLIF(cl.SoTruc, 0) AS DECIMAL(6,2)) AS TaiTrongTrucTan
                FROM phuongtien.ToaXe tx
                JOIN phuongtien.ChungLoaiToa cl ON tx.MaChungLoai = cl.MaChungLoai
                WHERE 1 = 1";

            var ps = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                sql += " AND (tx.SoHieuToaXe LIKE @kw OR cl.MaChungLoaiCode LIKE @kw OR cl.TenMoTa LIKE @kw)";
                ps.Add(new SqlParameter("@kw", $"%{tuKhoa.Trim()}%"));
            }

            if (maChungLoai.HasValue && maChungLoai.Value > 0)
            {
                sql += " AND tx.MaChungLoai = @cl";
                ps.Add(new SqlParameter("@cl", maChungLoai.Value));
            }

            if (trangThai == TrangThaiDangKhaiThac)
            {
                sql += " AND tx.TrangThai <> 'NGUNG_KHAI_THAC'";
            }
            else if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "ALL")
            {
                sql += " AND tx.TrangThai = @tt";
                ps.Add(new SqlParameter("@tt", trangThai));
            }

            sql += " ORDER BY CASE WHEN tx.TrangThai = 'NGUNG_KHAI_THAC' THEN 1 ELSE 0 END, cl.MaChungLoaiCode, tx.SoHieuToaXe";

            return DatabaseHelper.ExecuteQuery(sql, ps.Count > 0 ? ps.ToArray() : null);
        }

        public DataTable LayDanhSachChungLoaiToa()
        {
            string sql = @"
                SELECT cl.MaChungLoai,
                       cl.MaChungLoaiCode,
                       cl.TenMoTa,
                       cl.ChieuDaiChuanM,
                       cl.SoTruc,
                       ISNULL((SELECT COUNT(*) FROM phuongtien.ToaXe tx
                               WHERE tx.MaChungLoai = cl.MaChungLoai AND tx.TrangThai <> 'NGUNG_KHAI_THAC'), 0) AS SoToaTrongDoi
                FROM phuongtien.ChungLoaiToa cl
                ORDER BY cl.MaChungLoaiCode";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public ToaXe? LayToaXe(int maToaXe, PhienGiaoDich? gd = null)
        {
            DataTable dt = PhienGiaoDich.TruyVan(gd,
                @"SELECT MaToaXe, SoHieuToaXe, MaChungLoai, TuTrongTan, TaiTrongToiDaTan, TrangThai
                  FROM phuongtien.ToaXe WHERE MaToaXe = @id",
                new SqlParameter("@id", maToaXe));
            if (dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            return new ToaXe
            {
                MaToaXe = Convert.ToInt32(r["MaToaXe"]),
                SoHieuToaXe = r["SoHieuToaXe"].ToString() ?? "",
                MaChungLoai = Convert.ToInt32(r["MaChungLoai"]),
                TuTrongTan = Convert.ToDecimal(r["TuTrongTan"]),
                TaiTrongToiDaTan = Convert.ToDecimal(r["TaiTrongToiDaTan"]),
                TrangThai = r["TrangThai"].ToString() ?? ""
            };
        }

        public bool SoHieuToaXeDaTonTai(string soHieu, PhienGiaoDich? gd = null)
            => Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd,
                   "SELECT COUNT(*) FROM phuongtien.ToaXe WHERE SoHieuToaXe = @sh",
                   new SqlParameter("@sh", SqlDbType.VarChar, 20) { Value = soHieu })) > 0;

        public string? LayMaChungLoaiCode(int maChungLoai, PhienGiaoDich? gd = null)
            => PhienGiaoDich.LayGiaTri(gd, "SELECT MaChungLoaiCode FROM phuongtien.ChungLoaiToa WHERE MaChungLoai = @id",
                   new SqlParameter("@id", maChungLoai)) as string;

        // Cac chuyen CON HIEU LUC co toa nay trong doan tau
        public DataTable LayChuyenConHieuLucCuaToaXe(int maToaXe, PhienGiaoDich? gd = null)
            => PhienGiaoDich.TruyVan(gd, @"
                SELECT ct.MaChuyenTau, mt.SoHieuMacTau, ct.TrangThai, ct.GioXuatPhatKH, ct.GioVeDichKH, 'TOA' AS VaiTro
                FROM vanhanh.ChiTietDoanTau ctdt
                JOIN vanhanh.DoanTau dt ON dt.MaDoanTau = ctdt.MaDoanTau
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                WHERE ctdt.MaToaXe = @id
                  AND ct.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
                ORDER BY ct.GioXuatPhatKH",
                new SqlParameter("@id", maToaXe));

        public int ThemToaXe(ToaXe tx, PhienGiaoDich gd)
        {
            try
            {
                return Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd, @"
                    INSERT INTO phuongtien.ToaXe (SoHieuToaXe, MaChungLoai, TuTrongTan, TaiTrongToiDaTan, TrangThai)
                    VALUES (@sh, @cl, @tu, @tai, @tt);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new SqlParameter("@sh", SqlDbType.VarChar, 20) { Value = tx.SoHieuToaXe },
                    new SqlParameter("@cl", tx.MaChungLoai),
                    new SqlParameter("@tu", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = tx.TuTrongTan },
                    new SqlParameter("@tai", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = tx.TaiTrongToiDaTan },
                    new SqlParameter("@tt", SqlDbType.VarChar, 20) { Value = tx.TrangThai }));
            }
            catch (SqlException ex)
            {
                throw DichLoiSqlPhuongTien.Dich(ex);
            }
        }

        // Ghi de co kiem tra nhu CapNhatDauMay. So hieu va chung loai khong doi khi sua.
        public int CapNhatToaXe(ToaXe moi, ToaXe goc, PhienGiaoDich gd)
        {
            try
            {
                return PhienGiaoDich.ThucThi(gd, @"
                    UPDATE phuongtien.ToaXe
                    SET TuTrongTan = @tu, TaiTrongToiDaTan = @tai, TrangThai = @tt
                    WHERE MaToaXe = @id
                      AND SoHieuToaXe = @shGoc AND MaChungLoai = @clGoc AND TuTrongTan = @tuGoc
                      AND TaiTrongToiDaTan = @taiGoc AND TrangThai = @ttGoc",
                    new SqlParameter("@id", moi.MaToaXe),
                    new SqlParameter("@tu", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = moi.TuTrongTan },
                    new SqlParameter("@tai", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = moi.TaiTrongToiDaTan },
                    new SqlParameter("@tt", SqlDbType.VarChar, 20) { Value = moi.TrangThai },
                    new SqlParameter("@shGoc", SqlDbType.VarChar, 20) { Value = goc.SoHieuToaXe },
                    new SqlParameter("@clGoc", goc.MaChungLoai),
                    new SqlParameter("@tuGoc", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = goc.TuTrongTan },
                    new SqlParameter("@taiGoc", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = goc.TaiTrongToiDaTan },
                    new SqlParameter("@ttGoc", SqlDbType.VarChar, 20) { Value = goc.TrangThai });
            }
            catch (SqlException ex)
            {
                throw DichLoiSqlPhuongTien.Dich(ex);
            }
        }

        // Nhu DemThamChieuDauMay: doan tau (moi chuyen), van don hang, phieu bao duong
        public List<SoThamChieuDTO> DemThamChieuToaXe(int maToaXe, PhienGiaoDich? gd = null)
            => DocThamChieu(PhienGiaoDich.TruyVan(gd, @"
                SELECT 'DOAN_TAU' AS Loai, COUNT(*) AS SoLuong,
                       ISNULL((SELECT STRING_AGG(x.Nhan, ', ') WITHIN GROUP (ORDER BY x.Gio DESC)
                               FROM (SELECT TOP 3 CONCAT(mt.SoHieuMacTau, ' ', FORMAT(ct.NgayXuatPhat, 'dd/MM')) AS Nhan,
                                            ct.GioXuatPhatKH AS Gio
                                     FROM vanhanh.ChiTietDoanTau c
                                     JOIN vanhanh.DoanTau d ON d.MaDoanTau = c.MaDoanTau
                                     JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = d.MaChuyenTau
                                     JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                                     WHERE c.MaToaXe = @id
                                     ORDER BY ct.GioXuatPhatKH DESC) x), '') AS ViDu
                FROM vanhanh.ChiTietDoanTau WHERE MaToaXe = @id
                UNION ALL
                SELECT 'VAN_DON', COUNT(*), '' FROM vantai.VanDonHang WHERE MaToaXeHang = @id
                UNION ALL
                SELECT 'BAO_DUONG', COUNT(*), '' FROM baotri.NhatKyBaoDuong WHERE MaToaXe = @id",
                new SqlParameter("@id", maToaXe)));

        public int XoaToaXe(ToaXe goc, PhienGiaoDich gd)
        {
            try
            {
                return PhienGiaoDich.ThucThi(gd, @"
                    DELETE FROM phuongtien.ToaXe
                    WHERE MaToaXe = @id
                      AND SoHieuToaXe = @shGoc AND MaChungLoai = @clGoc AND TuTrongTan = @tuGoc
                      AND TaiTrongToiDaTan = @taiGoc AND TrangThai = @ttGoc",
                    new SqlParameter("@id", goc.MaToaXe),
                    new SqlParameter("@shGoc", SqlDbType.VarChar, 20) { Value = goc.SoHieuToaXe },
                    new SqlParameter("@clGoc", goc.MaChungLoai),
                    new SqlParameter("@tuGoc", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = goc.TuTrongTan },
                    new SqlParameter("@taiGoc", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = goc.TaiTrongToiDaTan },
                    new SqlParameter("@ttGoc", SqlDbType.VarChar, 20) { Value = goc.TrangThai });
            }
            catch (SqlException ex)
            {
                throw DichLoiSqlPhuongTien.Dich(ex);
            }
        }

        internal static List<SoThamChieuDTO> DocThamChieu(DataTable bang)
            => bang.Rows.Cast<DataRow>()
                   .Select(r => new SoThamChieuDTO(r["Loai"].ToString() ?? "", Convert.ToInt32(r["SoLuong"]),
                                                   r["ViDu"] == DBNull.Value ? "" : r["ViDu"].ToString() ?? ""))
                   .ToList();

        // ---------------------------------------------------------------------
        // 3. DANH SACH CHUYEN TAU (o chon chuyen cua tab Lap doan tau va So do cho ngoi)
        // ---------------------------------------------------------------------

        public DataTable LayDanhSachChuyenTauLapTau()
        {
            string sql = @"
                SELECT ct.MaChuyenTau,
                       mt.SoHieuMacTau,
                       mt.LoaiTau,
                       ct.NgayXuatPhat,
                       ct.GioXuatPhatKH,
                       ct.GioVeDichKH,
                       ct.TrangThai,
                       g1.TenGa AS TenGaDi,
                       g2.TenGa AS TenGaDen,
                       dt.MaDoanTau,
                       dt.DaDuyetAnToan,
                       ISNULL((SELECT COUNT(*) FROM vanhanh.ChiTietDoanTau c WHERE c.MaDoanTau = dt.MaDoanTau), 0) AS SoToaTrongDoan
                FROM vanhanh.ChuyenTau ct
                JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                JOIN hatang.Ga g1 ON mt.MaGaDi = g1.MaGa
                JOIN hatang.Ga g2 ON mt.MaGaDen = g2.MaGa
                LEFT JOIN vanhanh.DoanTau dt ON ct.MaChuyenTau = dt.MaChuyenTau
                ORDER BY ct.NgayXuatPhat DESC, ct.GioXuatPhatKH ASC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        // ---------------------------------------------------------------------
        // 4. BIEN CHE TOA XE KHACH CUA MOT CHUYEN (vantai.ToaXeKhach - toa ban ve) + SO DO GHE
        // ---------------------------------------------------------------------

        public DataTable LayBienCheToaXe(int maChuyenTau)
        {
            string sql = @"
                SELECT tx.MaToaXeKhach,
                       tx.MaChuyenTau,
                       tx.NhanHieuToa,
                       tx.LoaiToa,
                       tx.ThuTuToa,
                       tx.SucChua,
                       ISNULL(cl.TenMoTa, tx.LoaiToa) AS TenLoaiToa,
                       ISNULL(cl.ChieuDaiChuanM, 20.0) AS ChieuDaiToaM,
                       ISNULL((SELECT COUNT(*) FROM vantai.ChoNgoi cn WHERE cn.MaToaXeKhach = tx.MaToaXeKhach), 0) AS SoGheDaThietLap,
                       -- Dem GHE (khong dem ve): mot ghe ban nhieu chang khong giao nhau van la 1 ghe
                       ISNULL((SELECT COUNT(DISTINCT cn.MaChoNgoi)
                               FROM vantai.ChoNgoi cn
                               JOIN vantai.Ve v ON v.MaChoNgoi = cn.MaChoNgoi
                                                AND v.MaChuyenTau = tx.MaChuyenTau
                                                AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
                               WHERE cn.MaToaXeKhach = tx.MaToaXeKhach), 0) AS SoGheDaBan
                FROM vantai.ToaXeKhach tx
                LEFT JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoaiCode = tx.LoaiToa
                WHERE tx.MaChuyenTau = @ct
                ORDER BY tx.ThuTuToa ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@ct", maChuyenTau) });
        }

        public DataTable LaySoDoGhe(int maToaXeKhach, int maChuyenTau)
        {
            string sql = @"
                SELECT cn.MaChoNgoi,
                       cn.MaToaXeKhach,
                       cn.SoGhe,
                       cn.TangGiuong,
                       cn.LaGhePhu,
                       tx.LoaiToa,
                       tx.NhanHieuToa,
                       ISNULL(ve.SoVeDaBan, 0)      AS SoVeDaBan,
                       ve.DanhSachChang,
                       ve.TongTienGhe
                FROM vantai.ChoNgoi cn
                JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                OUTER APPLY (
                    SELECT COUNT(*) AS SoVeDaBan,
                           SUM(v.GiaVeThucThu) AS TongTienGhe,
                           STRING_AGG(
                               CONCAT(v.TenHanhKhach, N' · ', g1.TenGa, N' → ', g2.TenGa, N' · ', v.MaVeCode),
                               CHAR(10)) WITHIN GROUP (ORDER BY g1.LyTrinhKm) AS DanhSachChang
                    FROM vantai.Ve v
                    JOIN hatang.Ga g1 ON v.MaGaDi = g1.MaGa
                    JOIN hatang.Ga g2 ON v.MaGaDen = g2.MaGa
                    WHERE v.MaChoNgoi = cn.MaChoNgoi
                      AND v.MaChuyenTau = @ct
                      AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
                ) ve
                WHERE cn.MaToaXeKhach = @toa
                ORDER BY cn.SoGhe ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@toa", maToaXeKhach),
                new SqlParameter("@ct", maChuyenTau)
            });
        }

        // Danh sach tung VE da ban tren mot toa (moi ve mot dong, ke ca cung ghe khac chang)
        public DataTable LayHanhKhachTrenToa(int maToaXeKhach, int maChuyenTau)
        {
            string sql = @"
                SELECT cn.SoGhe,
                       cn.TangGiuong,
                       v.MaVe,
                       v.MaVeCode,
                       v.TenHanhKhach,
                       v.GiaVeThucThu,
                       v.TrangThai,
                       g1.TenGa AS TenGaDi,
                       g2.TenGa AS TenGaDen,
                       g1.LyTrinhKm AS KmDi
                FROM vantai.Ve v
                JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
                JOIN hatang.Ga g1 ON v.MaGaDi = g1.MaGa
                JOIN hatang.Ga g2 ON v.MaGaDen = g2.MaGa
                WHERE cn.MaToaXeKhach = @toa
                  AND v.MaChuyenTau = @ct
                  AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
                ORDER BY cn.SoGhe ASC, g1.LyTrinhKm ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@toa", maToaXeKhach),
                new SqlParameter("@ct", maChuyenTau)
            });
        }

        // ---------------------------------------------------------------------
        // 5. SO LIEU TONG HOP CHO DAI KPI DAU TRANG
        // ---------------------------------------------------------------------

        public DataTable LayThongKeTongQuan()
        {
            string sql = @"
                SELECT
                    -- Ngung khai thac khong tinh vao doi phuong tien dang dung
                    (SELECT COUNT(*) FROM phuongtien.DauMay WHERE TrangThai <> 'NGUNG_KHAI_THAC') AS TongDauMay,
                    (SELECT COUNT(*) FROM phuongtien.DauMay WHERE TrangThai = 'SAN_SANG')      AS DauMaySanSang,
                    (SELECT COUNT(*) FROM phuongtien.ToaXe WHERE TrangThai <> 'NGUNG_KHAI_THAC') AS TongToaXe,
                    (SELECT COUNT(*) FROM vanhanh.DoanTau)                                     AS TongDoanTau,
                    (SELECT COUNT(*) FROM vanhanh.DoanTau WHERE DaDuyetAnToan = 1)             AS DoanTauDaDuyet,
                    (SELECT COUNT(*) FROM vantai.ChoNgoi)                                      AS TongChoNgoi";
            return DatabaseHelper.ExecuteQuery(sql);
        }
    }
}
