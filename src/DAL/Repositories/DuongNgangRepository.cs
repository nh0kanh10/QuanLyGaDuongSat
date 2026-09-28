using System;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using ET.HaTang;

namespace DAL.Repositories
{
    public class DuongNgangRepository
    {

        public DataTable LayTheoKhuGian(int maKhuGian)
        {
            string sql = @"SELECT dn.MaDuongNgang, dn.MaKhuGian, dn.LyTrinhKm, dn.LoaiDuongNgang, 
                                  dn.TenDuongBoGiaoCat, dn.LaDiemDen,
                                  CASE dn.LoaiDuongNgang
                                      WHEN 'CO_NGUOI_GAC' THEN N'Có người gác 24/7'
                                      WHEN 'TU_DONG' THEN N'Cần chắn tự động'
                                      WHEN 'BIEN_BAO' THEN N'Biển báo cảnh báo'
                                      ELSE dn.LoaiDuongNgang END AS TenLoaiHienThi,
                                  g1.TenGa AS TenGaDau, g1.LyTrinhKm AS LyTrinhGaDau,
                                  g2.TenGa AS TenGaCuoi, g2.LyTrinhKm AS LyTrinhGaCuoi
                           FROM hatang.DuongNgang dn
                           JOIN hatang.KhuGian kg ON dn.MaKhuGian = kg.MaKhuGian
                           JOIN hatang.Ga g1 ON kg.MaGaDau = g1.MaGa
                           JOIN hatang.Ga g2 ON kg.MaGaCuoi = g2.MaGa
                           WHERE dn.MaKhuGian = @maKg
                           ORDER BY dn.LyTrinhKm ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@maKg", maKhuGian) });
        }

        public int Them(DuongNgang dn)
        {
            string sql = @"INSERT INTO hatang.DuongNgang (MaKhuGian, LyTrinhKm, LoaiDuongNgang, TenDuongBoGiaoCat, LaDiemDen)
                           VALUES (@maKg, @km, @loai, @ten, @diemDen)";
            var p = new[]
            {
                new SqlParameter("@maKg", dn.MaKhuGian),
                new SqlParameter("@km", dn.LyTrinhKm),
                new SqlParameter("@loai", string.IsNullOrWhiteSpace(dn.LoaiDuongNgang) ? "BIEN_BAO" : dn.LoaiDuongNgang.Trim()),
                new SqlParameter("@ten", dn.TenDuongBoGiaoCat.Trim()),
                new SqlParameter("@diemDen", dn.LaDiemDen)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int CapNhat(DuongNgang dn)
        {
            string sql = @"UPDATE hatang.DuongNgang
                           SET LyTrinhKm = @km,
                               LoaiDuongNgang = @loai,
                               TenDuongBoGiaoCat = @ten,
                               LaDiemDen = @diemDen
                           WHERE MaDuongNgang = @id";
            var p = new[]
            {
                new SqlParameter("@id", dn.MaDuongNgang),
                new SqlParameter("@km", dn.LyTrinhKm),
                new SqlParameter("@loai", string.IsNullOrWhiteSpace(dn.LoaiDuongNgang) ? "BIEN_BAO" : dn.LoaiDuongNgang.Trim()),
                new SqlParameter("@ten", dn.TenDuongBoGiaoCat.Trim()),
                new SqlParameter("@diemDen", dn.LaDiemDen)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int Xoa(int maDuongNgang)
        {
            return DatabaseHelper.ExecuteNonQuery(
                "DELETE FROM hatang.DuongNgang WHERE MaDuongNgang = @id",
                new[] { new SqlParameter("@id", maDuongNgang) });
        }

        public bool KiemTraLyTrinhTrung(int maKhuGian, decimal lyTrinhKm, int excludeId = 0)
        {
            string sql = @"SELECT COUNT(*) FROM hatang.DuongNgang
                           WHERE MaKhuGian = @maKg AND ABS(LyTrinhKm - @km) < 0.005 AND (@exclude = 0 OR MaDuongNgang <> @exclude)";
            var p = new[]
            {
                new SqlParameter("@maKg", maKhuGian),
                new SqlParameter("@km", lyTrinhKm),
                new SqlParameter("@exclude", excludeId)
            };
            object? res = DatabaseHelper.ExecuteScalar(sql, p);
            return res != null && Convert.ToInt32(res) > 0;
        }
    }
}
