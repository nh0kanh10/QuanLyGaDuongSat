using System;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using ET.HaTang;

namespace DAL.Repositories
{
    public class DuongRayRepository
    {
        public DataTable LayDanhSachTheoGa(int maGa)
        {
            string sql = @"SELECT r.*, g.TenGa, g.MaGaCode, g.LyTrinhKm,
                                  CASE r.LoaiDuong 
                                      WHEN 'CHINH_TUYEN' THEN N'Chính tuyến' 
                                      WHEN 'DUONG_TRANH' THEN N'Đường tránh' 
                                      WHEN 'BOC_DO' THEN N'Bốc dỡ hàng' 
                                      ELSE r.LoaiDuong END AS TenLoaiDuong,
                                  CASE r.TrangThai 
                                      WHEN 'TRONG' THEN N'Trống sẵn sàng' 
                                      WHEN 'CO_TAU' THEN N'Đang có đoàn tàu' 
                                      WHEN 'BAO_TRI' THEN N'Bảo trì kỹ thuật' 
                                      ELSE r.TrangThai END AS TenTrangThai
                           FROM hatang.DuongRayGa r
                           JOIN hatang.Ga g ON r.MaGa = g.MaGa
                           WHERE r.MaGa = @maGa
                           ORDER BY r.SoHieuDuong";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@maGa", maGa) });
        }

        public DataTable LayTatCa()
        {
            string sql = @"SELECT r.*, g.TenGa, g.MaGaCode, g.LyTrinhKm
                           FROM hatang.DuongRayGa r
                           JOIN hatang.Ga g ON r.MaGa = g.MaGa
                           ORDER BY g.LyTrinhKm, r.SoHieuDuong";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public int Them(DuongRayGa ray)
        {
            string sql = @"INSERT INTO hatang.DuongRayGa (MaGa, SoHieuDuong, LoaiDuong, ChieuDaiHuuDungM, CoKeGa, TrangThai)
                           VALUES (@maGa, @soHieu, @loai, @chieuDai, @keGa, @trangThai)";
            var p = new[]
            {
                new SqlParameter("@maGa", ray.MaGa),
                new SqlParameter("@soHieu", ray.SoHieuDuong),
                new SqlParameter("@loai", ray.LoaiDuong),
                new SqlParameter("@chieuDai", ray.ChieuDaiHuuDungM),
                new SqlParameter("@keGa", ray.CoKeGa),
                new SqlParameter("@trangThai", ray.TrangThai)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int CapNhat(DuongRayGa ray)
        {
            string sql = @"UPDATE hatang.DuongRayGa 
                           SET SoHieuDuong = @soHieu,
                               LoaiDuong = @loai,
                               ChieuDaiHuuDungM = @chieuDai,
                               CoKeGa = @keGa,
                               TrangThai = @trangThai
                           WHERE MaDuongRay = @id";
            var p = new[]
            {
                new SqlParameter("@id", ray.MaDuongRay),
                new SqlParameter("@soHieu", ray.SoHieuDuong),
                new SqlParameter("@loai", ray.LoaiDuong),
                new SqlParameter("@chieuDai", ray.ChieuDaiHuuDungM),
                new SqlParameter("@keGa", ray.CoKeGa),
                new SqlParameter("@trangThai", ray.TrangThai)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int Xoa(int maDuongRay)
        {
            string sql = "DELETE FROM hatang.DuongRayGa WHERE MaDuongRay = @id";
            return DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@id", maDuongRay) });
        }

        public bool KiemTraSoHieuTonTai(int maGa, int soHieuDuong, int maDuongRayBoQua = 0)
        {
            string sql = @"SELECT COUNT(*) 
                           FROM hatang.DuongRayGa 
                           WHERE MaGa = @maGa AND SoHieuDuong = @soHieu AND MaDuongRay <> @boQua";
            var p = new[]
            {
                new SqlParameter("@maGa", maGa),
                new SqlParameter("@soHieu", soHieuDuong),
                new SqlParameter("@boQua", maDuongRayBoQua)
            };
            var result = DatabaseHelper.ExecuteScalar(sql, p);
            return result != null && Convert.ToInt32(result) > 0;
        }
    }
}
