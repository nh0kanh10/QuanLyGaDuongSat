using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;

namespace DAL.Repositories
{
    public class NhatKyRepository
    {
        public DataTable LayNhatKyMoiNhat(int top = 50, string? tenBang = null)
        {
            string sql;
            if (string.IsNullOrWhiteSpace(tenBang))
            {
                sql = $"SELECT TOP (@top) * FROM kiemtoan.NhatKyHeThong ORDER BY ThoiDiem DESC";
                return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@top", top) });
            }
            else
            {
                sql = $"SELECT TOP (@top) * FROM kiemtoan.NhatKyHeThong WHERE TenBang = @bang ORDER BY ThoiDiem DESC";
                return DatabaseHelper.ExecuteQuery(sql, new[]
                {
                    new SqlParameter("@top", top),
                    new SqlParameter("@bang", tenBang)
                });
            }
        }

        public int GhiNhatKy(string hanhDong, string tenBang, string khoaChinh, string? duLieuCu = null, string? duLieuMoi = null, int? maTaiKhoan = null)
        {
            var p = new[]
            {
                new SqlParameter("@MaTaiKhoan", (object?)maTaiKhoan ?? DBNull.Value),
                new SqlParameter("@HanhDong", hanhDong),
                new SqlParameter("@TenBang", tenBang),
                new SqlParameter("@KhoaChinhRecord", khoaChinh),
                new SqlParameter("@DuLieuCu", (object?)duLieuCu ?? DBNull.Value),
                new SqlParameter("@DuLieuMoi", (object?)duLieuMoi ?? DBNull.Value),
                new SqlParameter("@DiaChiIP", Environment.MachineName)
            };
            return DatabaseHelper.ExecuteStoredProcedure("kiemtoan.sp_GhiNhatKyHeThong", p).Rows.Count;
        }
    }
}
