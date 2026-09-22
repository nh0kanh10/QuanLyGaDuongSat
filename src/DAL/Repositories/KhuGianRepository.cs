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
                IF OBJECT_ID('hatang.DuongNgang', 'U') IS NOT NULL
                BEGIN
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
                    ORDER BY g1.LyTrinhKm;
                END
                ELSE
                BEGIN
                    SELECT kg.*, 
                           g1.TenGa AS TenGaDau, g1.MaGaCode AS MaGaDauCode, g1.LyTrinhKm AS LyTrinhDauKm,
                           g2.TenGa AS TenGaCuoi, g2.MaGaCode AS MaGaCuoiCode, g2.LyTrinhKm AS LyTrinhCuoiKm,
                           CASE kg.TrangThai
                               WHEN 'RONG' THEN N'Thông đường sẵn sàng'
                               WHEN 'CO_TAU' THEN N'Đang có đoàn tàu'
                               WHEN 'PHONG_TOA' THEN N'Phong tỏa thi công'
                               ELSE kg.TrangThai END AS TenTrangThai,
                           0 AS SoDuongNgang,
                           0 AS SoDiemDen
                    FROM hatang.KhuGian kg
                    JOIN hatang.Ga g1 ON kg.MaGaDau = g1.MaGa
                    JOIN hatang.Ga g2 ON kg.MaGaCuoi = g2.MaGa
                    ORDER BY g1.LyTrinhKm;
                END";
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

        public int Xoa(int maKhuGian)
        {
            return DatabaseHelper.ExecuteNonQuery("DELETE FROM hatang.KhuGian WHERE MaKhuGian=@id",
                new[] { new SqlParameter("@id", maKhuGian) });
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
