using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using ET.HaTang;

namespace DAL.Repositories
{
    public class KhuGianRepository
    {
        public DataTable LayDanhSach()
        {
            string sql = @"
                SELECT kg.*, 
                       g1.TenGa AS TenGaDau, g1.MaGaCode AS MaGaDauCode, g1.LyTrinhKm AS LyTrinhDauKm,
                       g2.TenGa AS TenGaCuoi, g2.MaGaCode AS MaGaCuoiCode, g2.LyTrinhKm AS LyTrinhCuoiKm,
                       CASE kg.TrangThai
                           WHEN 'RONG' THEN N'Thông đường sẵn sàng'
                           WHEN 'CO_TAU' THEN N'Đang có đoàn tàu'
                           WHEN 'PHONG_TOA' THEN N'Phong tỏa thi công'
                           ELSE kg.TrangThai END AS TenTrangThai,
                       ISNULL((SELECT COUNT(*) FROM hatang.DuongNgang dn WHERE dn.MaKhuGian = kg.MaKhuGian), 0) AS SoDuongNgang,
                       ISNULL((SELECT COUNT(*) FROM hatang.DuongNgang dn WHERE dn.MaKhuGian = kg.MaKhuGian AND dn.LaDiemDen = 1), 0) AS SoDiemDen
                FROM hatang.KhuGian kg
                JOIN hatang.Ga g1 ON kg.MaGaDau = g1.MaGa
                JOIN hatang.Ga g2 ON kg.MaGaCuoi = g2.MaGa
                ORDER BY g1.LyTrinhKm";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable LayTheoGa(int maGa)
        {
            string sql = @"SELECT kg.*, 
                                  g1.TenGa AS TenGaDau, g1.MaGaCode AS MaGaDauCode, g1.LyTrinhKm AS LyTrinhDauKm,
                                  g2.TenGa AS TenGaCuoi, g2.MaGaCode AS MaGaCuoiCode, g2.LyTrinhKm AS LyTrinhCuoiKm,
                                  CASE WHEN kg.MaGaCuoi = @maGa THEN N'Khu gian phía Bắc' ELSE N'Khu gian phía Nam' END AS HuongTuyen
                           FROM hatang.KhuGian kg
                           JOIN hatang.Ga g1 ON kg.MaGaDau = g1.MaGa
                           JOIN hatang.Ga g2 ON kg.MaGaCuoi = g2.MaGa
                           WHERE kg.MaGaDau = @maGa OR kg.MaGaCuoi = @maGa
                           ORDER BY g1.LyTrinhKm";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@maGa", maGa) });
        }

        public int Them(KhuGian kg)
        {
            string sql = @"INSERT INTO hatang.KhuGian (MaGaDau, MaGaCuoi, CuLyKm, TocDoToiDaKhach, TocDoToiDaHang, DoDocPermil, CanDauMayDay, TrangThai)
                           VALUES (@gaDau, @gaCuoi, @culy, @vk, @vh, @doc, @banker, @trangThai)";
            var p = new[]
            {
                new SqlParameter("@gaDau", kg.MaGaDau),
                new SqlParameter("@gaCuoi", kg.MaGaCuoi),
                new SqlParameter("@culy", kg.CuLyKm),
                new SqlParameter("@vk", kg.TocDoToiDaKhach),
                new SqlParameter("@vh", kg.TocDoToiDaHang),
                new SqlParameter("@doc", kg.DoDocPermil),
                new SqlParameter("@banker", kg.CanDauMayDay),
                new SqlParameter("@trangThai", string.IsNullOrWhiteSpace(kg.TrangThai) ? "RONG" : kg.TrangThai)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int CapNhat(KhuGian kg)
        {
            string sql = @"UPDATE hatang.KhuGian 
                           SET CuLyKm = @culy, 
                               TocDoToiDaKhach = @vk, 
                               TocDoToiDaHang = @vh, 
                               DoDocPermil = @doc, 
                               CanDauMayDay = @banker,
                               TrangThai = @trangThai 
                           WHERE MaKhuGian = @id";
            var p = new[]
            {
                new SqlParameter("@id", kg.MaKhuGian),
                new SqlParameter("@culy", kg.CuLyKm),
                new SqlParameter("@vk", kg.TocDoToiDaKhach),
                new SqlParameter("@vh", kg.TocDoToiDaHang),
                new SqlParameter("@doc", kg.DoDocPermil),
                new SqlParameter("@banker", kg.CanDauMayDay),
                new SqlParameter("@trangThai", string.IsNullOrWhiteSpace(kg.TrangThai) ? "RONG" : kg.TrangThai)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int Xoa(int maKhuGian, bool xoaDuongNgangKemTheo = true)
        {
            if (xoaDuongNgangKemTheo)
            {
                string sql = @"
                    BEGIN TRANSACTION;
                    DELETE FROM hatang.DuongNgang WHERE MaKhuGian = @id;
                    DELETE FROM hatang.KhuGian WHERE MaKhuGian = @id;
                    COMMIT TRANSACTION;";
                return DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@id", maKhuGian) });
            }
            else
            {
                return DatabaseHelper.ExecuteNonQuery("DELETE FROM hatang.KhuGian WHERE MaKhuGian=@id",
                    new[] { new SqlParameter("@id", maKhuGian) });
            }
        }

        /// <summary>
        /// Lấy danh sách ga liền kề (tối đa 2: 1 phía Bắc + 1 phía Nam theo lý trình)
        /// mà chưa có khu gian nối với ga được chọn.
        /// </summary>
        public DataTable LayGaLienKeChuaCoKhuGian(int maGa, int maKhuGianBoQua = 0)
        {
            // CTE tìm ga ngay phía Bắc (lý trình nhỏ hơn gần nhất) và phía Nam (lý trình lớn hơn gần nhất)
            // Sau đó loại ga đã tồn tại khu gian (trừ khu gian đang chỉnh sửa)
            string sql = @"
                WITH GaHienTai AS (
                    SELECT MaGa, LyTrinhKm FROM hatang.Ga WHERE MaGa = @maGa
                ),
                GaLienKe AS (
                    -- Ga liền kề phía Bắc (lý trình nhỏ hơn, gần nhất)
                    SELECT TOP 1 g.MaGa, g.MaGaCode, g.TenGa, g.LyTrinhKm, N'Phía Bắc' AS Huong
                    FROM hatang.Ga g, GaHienTai ht
                    WHERE g.LyTrinhKm < ht.LyTrinhKm AND g.DangKhaiThac = 1
                    ORDER BY g.LyTrinhKm DESC

                    UNION ALL

                    -- Ga liền kề phía Nam (lý trình lớn hơn, gần nhất)
                    SELECT TOP 1 g.MaGa, g.MaGaCode, g.TenGa, g.LyTrinhKm, N'Phía Nam' AS Huong
                    FROM hatang.Ga g, GaHienTai ht
                    WHERE g.LyTrinhKm > ht.LyTrinhKm AND g.DangKhaiThac = 1
                    ORDER BY g.LyTrinhKm ASC
                )
                SELECT lk.*
                FROM GaLienKe lk
                WHERE NOT EXISTS (
                    SELECT 1 FROM hatang.KhuGian kg
                    WHERE ((kg.MaGaDau = @maGa AND kg.MaGaCuoi = lk.MaGa) 
                        OR (kg.MaGaDau = lk.MaGa AND kg.MaGaCuoi = @maGa))
                      AND kg.MaKhuGian <> @boQua
                )
                ORDER BY lk.LyTrinhKm";
            var p = new[]
            {
                new SqlParameter("@maGa", maGa),
                new SqlParameter("@boQua", maKhuGianBoQua)
            };
            return DatabaseHelper.ExecuteQuery(sql, p);
        }

        /// <summary>
        /// Kiểm tra 2 ga có liền kề nhau theo lý trình không (không có ga nào nằm giữa).
        /// </summary>
        public bool KiemTraGaLienKe(int maGa1, int maGa2)
        {
            string sql = @"
                DECLARE @km1 DECIMAL(10,2), @km2 DECIMAL(10,2);
                SELECT @km1 = LyTrinhKm FROM hatang.Ga WHERE MaGa = @g1;
                SELECT @km2 = LyTrinhKm FROM hatang.Ga WHERE MaGa = @g2;

                -- Đếm số ga nằm giữa 2 ga (theo lý trình), nếu = 0 thì liền kề
                SELECT COUNT(*) FROM hatang.Ga
                WHERE DangKhaiThac = 1
                  AND LyTrinhKm > CASE WHEN @km1 < @km2 THEN @km1 ELSE @km2 END
                  AND LyTrinhKm < CASE WHEN @km1 < @km2 THEN @km2 ELSE @km1 END";
            var p = new[]
            {
                new SqlParameter("@g1", maGa1),
                new SqlParameter("@g2", maGa2)
            };
            var result = DatabaseHelper.ExecuteScalar(sql, p);
            return result != null && Convert.ToInt32(result) == 0;
        }

        public bool KiemTraTonTai(int maGaDau, int maGaCuoi, int maKhuGianBoQua = 0)
        {
            string sql = @"SELECT COUNT(*) 
                           FROM hatang.KhuGian 
                           WHERE ((MaGaDau = @g1 AND MaGaCuoi = @g2) OR (MaGaDau = @g2 AND MaGaCuoi = @g1))
                             AND MaKhuGian <> @boQua";
            var p = new[]
            {
                new SqlParameter("@g1", maGaDau),
                new SqlParameter("@g2", maGaCuoi),
                new SqlParameter("@boQua", maKhuGianBoQua)
            };
            var result = DatabaseHelper.ExecuteScalar(sql, p);
            return result != null && Convert.ToInt32(result) > 0;
        }
    }
}
