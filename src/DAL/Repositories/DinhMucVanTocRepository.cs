using System;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using ET.VanHanh;

namespace DAL.Repositories
{
    public class DinhMucVanTocRepository
    {
        public DataTable LayDanhSach(bool? chiDangApDung = null)
        {
            string sql = @"SELECT dm.MaLoaiTau, dm.TenLoaiTau, dm.VanTocDinhMucKmh, dm.MoTaQuyChuan,
                                  dm.MauNhanDien, dm.ThuTuHienThi, dm.DangApDung,
                                  dm.NgayTao, dm.NguoiTao, dm.NgayCapNhat, dm.NguoiCapNhat,
                                  ISNULL((SELECT COUNT(*) FROM vanhanh.MacTauMau m WHERE m.LoaiTau = dm.MaLoaiTau), 0) AS SoMacTauDangDung
                           FROM vanhanh.DinhMucVanToc dm
                           WHERE (@chiDangApDung IS NULL OR dm.DangApDung = @chiDangApDung)
                           ORDER BY dm.ThuTuHienThi ASC, dm.MaLoaiTau ASC";

            var parameters = new[]
            {
                new SqlParameter("@chiDangApDung", chiDangApDung.HasValue ? (object)chiDangApDung.Value : DBNull.Value)
            };

            return DatabaseHelper.ExecuteQuery(sql, parameters);
        }

        public DinhMucVanToc? LayTheoMa(string maLoaiTau)
        {
            string sql = @"SELECT MaLoaiTau, TenLoaiTau, VanTocDinhMucKmh, MoTaQuyChuan,
                                  MauNhanDien, ThuTuHienThi, DangApDung,
                                  NgayTao, NguoiTao, NgayCapNhat, NguoiCapNhat
                           FROM vanhanh.DinhMucVanToc
                           WHERE UPPER(MaLoaiTau) = UPPER(@ma)";

            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@ma", maLoaiTau.Trim()) });
            if (dt.Rows.Count == 0) return null;

            var r = dt.Rows[0];
            return new DinhMucVanToc
            {
                MaLoaiTau = r["MaLoaiTau"]?.ToString() ?? "",
                TenLoaiTau = r["TenLoaiTau"]?.ToString() ?? "",
                VanTocDinhMucKmh = Convert.ToDecimal(r["VanTocDinhMucKmh"]),
                MoTaQuyChuan = r["MoTaQuyChuan"] != DBNull.Value ? r["MoTaQuyChuan"]?.ToString() : null,
                MauNhanDien = r["MauNhanDien"] != DBNull.Value ? r["MauNhanDien"]?.ToString() : "#0284C7",
                ThuTuHienThi = Convert.ToInt32(r["ThuTuHienThi"]),
                DangApDung = Convert.ToBoolean(r["DangApDung"]),
                NgayTao = Convert.ToDateTime(r["NgayTao"]),
                NguoiTao = r["NguoiTao"]?.ToString() ?? "admin",
                NgayCapNhat = r["NgayCapNhat"] != DBNull.Value ? Convert.ToDateTime(r["NgayCapNhat"]) : null,
                NguoiCapNhat = r["NguoiCapNhat"] != DBNull.Value ? r["NguoiCapNhat"]?.ToString() : null
            };
        }

        public bool KiemTraTonTai(string maLoaiTau)
        {
            string sql = @"SELECT COUNT(*) FROM vanhanh.DinhMucVanToc WHERE UPPER(MaLoaiTau) = UPPER(@ma)";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@ma", maLoaiTau.Trim()) });
            return Convert.ToInt32(dt.Rows[0][0]) > 0;
        }

        public int DemSoMacTauDangSuDung(string maLoaiTau)
        {
            string sql = @"SELECT COUNT(*) FROM vanhanh.MacTauMau WHERE LoaiTau = @ma";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@ma", maLoaiTau.Trim()) });
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        public int Them(DinhMucVanToc dm)
        {
            string sql = @"INSERT INTO vanhanh.DinhMucVanToc 
                           (MaLoaiTau, TenLoaiTau, VanTocDinhMucKmh, MoTaQuyChuan, MauNhanDien, ThuTuHienThi, DangApDung, NgayTao, NguoiTao)
                           VALUES (@ma, @ten, @vanToc, @moTa, @mau, @thuTu, @dangApDung, SYSUTCDATETIME(), @nguoiTao)";

            var p = new[]
            {
                new SqlParameter("@ma", dm.MaLoaiTau.Trim().ToUpperInvariant()),
                new SqlParameter("@ten", dm.TenLoaiTau.Trim()),
                new SqlParameter("@vanToc", dm.VanTocDinhMucKmh),
                new SqlParameter("@moTa", string.IsNullOrWhiteSpace(dm.MoTaQuyChuan) ? DBNull.Value : dm.MoTaQuyChuan.Trim()),
                new SqlParameter("@mau", string.IsNullOrWhiteSpace(dm.MauNhanDien) ? (object)DBNull.Value : dm.MauNhanDien.Trim()),
                new SqlParameter("@thuTu", dm.ThuTuHienThi),
                new SqlParameter("@dangApDung", dm.DangApDung),
                new SqlParameter("@nguoiTao", string.IsNullOrWhiteSpace(dm.NguoiTao) ? "admin" : dm.NguoiTao)
            };

            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int CapNhat(DinhMucVanToc dm)
        {
            string sql = @"UPDATE vanhanh.DinhMucVanToc
                           SET TenLoaiTau = @ten,
                               VanTocDinhMucKmh = @vanToc,
                               MoTaQuyChuan = @moTa,
                               MauNhanDien = @mau,
                               ThuTuHienThi = @thuTu,
                               DangApDung = @dangApDung,
                               NgayCapNhat = SYSUTCDATETIME(),
                               NguoiCapNhat = @nguoiCapNhat
                           WHERE UPPER(MaLoaiTau) = UPPER(@ma)";

            var p = new[]
            {
                new SqlParameter("@ma", dm.MaLoaiTau.Trim()),
                new SqlParameter("@ten", dm.TenLoaiTau.Trim()),
                new SqlParameter("@vanToc", dm.VanTocDinhMucKmh),
                new SqlParameter("@moTa", string.IsNullOrWhiteSpace(dm.MoTaQuyChuan) ? DBNull.Value : dm.MoTaQuyChuan.Trim()),
                new SqlParameter("@mau", string.IsNullOrWhiteSpace(dm.MauNhanDien) ? (object)DBNull.Value : dm.MauNhanDien.Trim()),
                new SqlParameter("@thuTu", dm.ThuTuHienThi),
                new SqlParameter("@dangApDung", dm.DangApDung),
                new SqlParameter("@nguoiCapNhat", string.IsNullOrWhiteSpace(dm.NguoiCapNhat) ? "admin" : dm.NguoiCapNhat)
            };

            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int Xoa(string maLoaiTau)
        {
            string sql = @"DELETE FROM vanhanh.DinhMucVanToc WHERE UPPER(MaLoaiTau) = UPPER(@ma)";
            return DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@ma", maLoaiTau.Trim()) });
        }
    }
}
