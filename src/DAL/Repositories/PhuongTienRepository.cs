using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;

namespace DAL.Repositories
{
    // =========================================================================
    // PHAN HE QUAN LY PHUONG TIEN (Doan tau, Dau may, Toa xe & Ghe)
    // Truy van CHI DOC phuc vu tang giao dien - khong chua nghiep vu ghi du lieu.
    // =========================================================================
    public class PhuongTienRepository
    {
        // ---------------------------------------------------------------------
        // 1. DAU MAY
        // ---------------------------------------------------------------------

        public DataTable LayDanhSachDauMay(string? tuKhoa = null, string? donViQuanLy = null, string? trangThai = null)
        {
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

            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "ALL")
            {
                sql += " AND dm.TrangThai = @tt";
                ps.Add(new SqlParameter("@tt", trangThai));
            }

            sql += " ORDER BY dm.SoHieuDauMay ASC";

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

            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "ALL")
            {
                sql += " AND tx.TrangThai = @tt";
                ps.Add(new SqlParameter("@tt", trangThai));
            }

            sql += " ORDER BY cl.MaChungLoaiCode, tx.SoHieuToaXe";

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
                       ISNULL((SELECT COUNT(*) FROM phuongtien.ToaXe tx WHERE tx.MaChungLoai = cl.MaChungLoai), 0) AS SoToaTrongDoi
                FROM phuongtien.ChungLoaiToa cl
                ORDER BY cl.MaChungLoaiCode";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        // ---------------------------------------------------------------------
        // 3. DOAN TAU (lap tau theo chuyen)
        // ---------------------------------------------------------------------

        // Danh sach chuyen tau kem thong so doan tau, dung cho ComboBox chon chuyen
        public DataTable LayDanhSachChuyenTauLapTau()
        {
            string sql = @"
                SELECT ct.MaChuyenTau,
                       mt.SoHieuMacTau,
                       mt.LoaiTau,
                       mt.MucUuTien,
                       ct.NgayXuatPhat,
                       ct.GioXuatPhatKH,
                       ct.GioVeDichKH,
                       ct.TrangThai,
                       g1.TenGa AS TenGaDi,
                       g2.TenGa AS TenGaDen,
                       dt.MaDoanTau,
                       dt.MaDauMayChinh,
                       dt.MaDauMayDay,
                       dt.TongSoToa,
                       dt.TongChieuDaiM,
                       dt.TongTrongLuongTan,
                       dt.DaDuyetAnToan,
                       dmc.SoHieuDauMay AS SoHieuDauMayChinh,
                       ddmc.MaDongCode  AS DongDauMayChinh,
                       ddmc.SucKeoToiDaTan AS SucKeoChinhTan,
                       dmd.SoHieuDauMay AS SoHieuDauMayDay,
                       ddmd.MaDongCode  AS DongDauMayDay,
                       ISNULL((SELECT COUNT(*) FROM vantai.ToaXeKhach t WHERE t.MaChuyenTau = ct.MaChuyenTau), 0) AS SoToaThucTe,
                       (SELECT MIN(dr.ChieuDaiHuuDungM)
                        FROM vanhanh.LichDungGa ld
                        JOIN hatang.DuongRayGa dr ON ld.MaGa = dr.MaGa AND dr.LoaiDuong = 'DUONG_TRANH'
                        WHERE ld.MaChuyenTau = ct.MaChuyenTau) AS DuongTranhNganNhatM
                FROM vanhanh.ChuyenTau ct
                JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                JOIN hatang.Ga g1 ON mt.MaGaDi = g1.MaGa
                JOIN hatang.Ga g2 ON mt.MaGaDen = g2.MaGa
                LEFT JOIN vanhanh.DoanTau dt ON ct.MaChuyenTau = dt.MaChuyenTau
                LEFT JOIN phuongtien.DauMay dmc ON dt.MaDauMayChinh = dmc.MaDauMay
                LEFT JOIN phuongtien.DongDauMay ddmc ON dmc.MaDongDauMay = ddmc.MaDongDauMay
                LEFT JOIN phuongtien.DauMay dmd ON dt.MaDauMayDay = dmd.MaDauMay
                LEFT JOIN phuongtien.DongDauMay ddmd ON dmd.MaDongDauMay = ddmd.MaDongDauMay
                ORDER BY ct.NgayXuatPhat DESC, ct.GioXuatPhatKH ASC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        // ---------------------------------------------------------------------
        // 4. BIEN CHE TOA XE KHACH CUA MOT CHUYEN + SO DO GHE
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
                       CASE tx.LoaiToa
                           WHEN 'NC'  THEN N'Ngồi cứng'
                           WHEN 'NML' THEN N'Ngồi mềm điều hòa'
                           WHEN 'BN'  THEN N'Giường nằm khoang 6'
                           WHEN 'AN'  THEN N'Giường nằm khoang 4'
                           ELSE tx.LoaiToa
                       END AS TenLoaiToa,
                       ISNULL(cl.ChieuDaiChuanM, 20.0) AS ChieuDaiToaM,
                       ISNULL((SELECT COUNT(*) FROM vantai.ChoNgoi cn WHERE cn.MaToaXeKhach = tx.MaToaXeKhach), 0) AS SoGheDaThietLap,
                       ISNULL((SELECT COUNT(*)
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

        // So do cho ngoi cua mot toa: DUNG MOT DONG CHO MOI GHE.
        //
        // Luu y nghiep vu: mot cho ngoi co the duoc ban cho NHIEU hanh khach tren cac
        // chang khong giao nhau (vi du Ha Noi-Vinh va Vinh-Sai Gon). Vi vay phai gom
        // nhom theo MaChoNgoi, neu khong so do se bi nhan doi o nhung ghe ban nhieu chang.
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
                    (SELECT COUNT(*) FROM phuongtien.DauMay)                                   AS TongDauMay,
                    (SELECT COUNT(*) FROM phuongtien.DauMay WHERE TrangThai = 'SAN_SANG')      AS DauMaySanSang,
                    (SELECT COUNT(*) FROM phuongtien.ToaXe)                                    AS TongToaXe,
                    (SELECT COUNT(*) FROM vanhanh.DoanTau)                                     AS TongDoanTau,
                    (SELECT COUNT(*) FROM vanhanh.DoanTau WHERE DaDuyetAnToan = 1)             AS DoanTauDaDuyet,
                    (SELECT COUNT(*) FROM vantai.ChoNgoi)                                      AS TongChoNgoi";
            return DatabaseHelper.ExecuteQuery(sql);
        }
    }
}
