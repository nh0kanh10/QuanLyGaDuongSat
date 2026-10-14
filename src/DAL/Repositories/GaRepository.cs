using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using ET.HaTang;

namespace DAL.Repositories
{
    public class GaRepository
    {
        public DataTable LayDanhSach()
        {
            string sql = @"SELECT g.*, 
                                  ISNULL((SELECT COUNT(*) FROM hatang.DuongRayGa d WHERE d.MaGa = g.MaGa), 0) AS SoDuongRay,
                                  ISNULL((SELECT COUNT(*) FROM hatang.KhuGian k WHERE k.MaGaDau = g.MaGa OR k.MaGaCuoi = g.MaGa), 0) AS SoKhuGian,
                                  ISNULL(t.TenTuyen, N'Tuyến Bắc - Nam') AS TenTuyen
                           FROM hatang.Ga g
                           LEFT JOIN hatang.TuyenDuong t ON g.MaTuyen = t.MaTuyen
                           ORDER BY g.LyTrinhKm";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable TimTheoTen(string tenGa)
        {
            string sql = @"SELECT g.*, 
                                  ISNULL((SELECT COUNT(*) FROM hatang.DuongRayGa d WHERE d.MaGa = g.MaGa), 0) AS SoDuongRay,
                                  ISNULL((SELECT COUNT(*) FROM hatang.KhuGian k WHERE k.MaGaDau = g.MaGa OR k.MaGaCuoi = g.MaGa), 0) AS SoKhuGian,
                                  ISNULL(t.TenTuyen, N'Tuyến Bắc - Nam') AS TenTuyen
                           FROM hatang.Ga g
                           LEFT JOIN hatang.TuyenDuong t ON g.MaTuyen = t.MaTuyen
                           WHERE g.TenGa LIKE @ten OR g.MaGaCode LIKE @ten OR g.TinhThanh LIKE @ten
                           ORDER BY g.LyTrinhKm";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@ten", "%" + tenGa + "%") });
        }

        public int Them(Ga ga)
        {
            string sql = @"INSERT INTO hatang.Ga (MaTuyen, MaGaCode, TenGa, LyTrinhKm, TinhThanh, HangGa, CoCauQuay, DangKhaiThac, NguoiTao)
                           VALUES (@maTuyen, @code, @ten, @km, @tinh, @hang, @quay, @kt, @nguoiTao)";
            var p = new[]
            {
                new SqlParameter("@maTuyen", ga.MaTuyen),
                new SqlParameter("@code", ga.MaGaCode),
                new SqlParameter("@ten", ga.TenGa),
                new SqlParameter("@km", ga.LyTrinhKm),
                new SqlParameter("@tinh", ga.TinhThanh),
                new SqlParameter("@hang", ga.HangGa),
                new SqlParameter("@quay", ga.CoCauQuay),
                new SqlParameter("@kt", ga.DangKhaiThac),
                new SqlParameter("@nguoiTao", string.IsNullOrWhiteSpace(ga.NguoiTao) ? "admin" : ga.NguoiTao)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int CapNhat(Ga ga)
        {
            string sql = @"UPDATE hatang.Ga SET TenGa=@ten, LyTrinhKm=@km, TinhThanh=@tinh, 
                           HangGa=@hang, CoCauQuay=@quay, DangKhaiThac=@kt, 
                           NgayCapNhat=SYSUTCDATETIME(), NguoiCapNhat=@nguoiCapNhat 
                           WHERE MaGa=@id";
            var p = new[]
            {
                new SqlParameter("@id", ga.MaGa),
                new SqlParameter("@ten", ga.TenGa),
                new SqlParameter("@km", ga.LyTrinhKm),
                new SqlParameter("@tinh", ga.TinhThanh),
                new SqlParameter("@hang", ga.HangGa),
                new SqlParameter("@quay", ga.CoCauQuay),
                new SqlParameter("@kt", ga.DangKhaiThac),
                new SqlParameter("@nguoiCapNhat", string.IsNullOrWhiteSpace(ga.NguoiCapNhat) ? "admin" : ga.NguoiCapNhat)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int Xoa(int maGa)
        {
            string sql = @"UPDATE hatang.Ga 
                           SET DangKhaiThac = 0, 
                               NgayCapNhat = SYSUTCDATETIME(), 
                               NguoiCapNhat = 'admin' 
                           WHERE MaGa = @id";
            return DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@id", maGa) });
        }

        /// <summary>
        /// Xóa ga thông minh: Nếu ga chưa có bất kỳ dữ liệu liên kết nào (đường ray, khu gian, lịch trình...) thì xóa hẳn (Hard Delete).
        /// Nếu đã có liên kết hạ tầng hoặc vận hành thì tự động chuyển sang Tạm ngừng khai thác (Soft Delete).
        /// </summary>
        public bool XoaThongMinh(int maGa, out bool daXoaVinhVien, out string thongBao)
        {
            daXoaVinhVien = false;
            thongBao = string.Empty;

            try
            {
                // 1. Thử xóa hoàn toàn (Hard Delete) nếu là ga mới chưa có ràng buộc khóa ngoại
                string sqlDelete = "DELETE FROM hatang.Ga WHERE MaGa = @id";
                int rows = DatabaseHelper.ExecuteNonQuery(sqlDelete, new[] { new SqlParameter("@id", maGa) });
                if (rows > 0)
                {
                    daXoaVinhVien = true;
                    thongBao = "Đã xóa hoàn toàn ga khỏi hệ thống.";
                    return true;
                }
                thongBao = "Không tìm thấy dữ liệu ga cần xóa.";
                return false;
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // 2. Vi phạm khóa ngoại FK (có dữ liệu đường ray, khu gian, lịch trình...) -> Chuyển sang Tạm ngừng khai thác
                string sqlSoft = @"UPDATE hatang.Ga 
                                   SET DangKhaiThac = 0, 
                                       NgayCapNhat = SYSUTCDATETIME(), 
                                       NguoiCapNhat = 'admin' 
                                   WHERE MaGa = @id";
                int rows = DatabaseHelper.ExecuteNonQuery(sqlSoft, new[] { new SqlParameter("@id", maGa) });
                if (rows > 0)
                {
                    daXoaVinhVien = false;
                    thongBao = "Ga đã có dữ liệu hạ tầng hoặc vận hành nên đã được chuyển sang trạng thái 'Tạm ngừng khai thác' để bảo toàn dữ liệu.";
                    return true;
                }
                thongBao = "Không thể cập nhật trạng thái ga.";
                return false;
            }
            catch (Exception ex)
            {
                thongBao = ex.Message;
                return false;
            }
        }

        public DataTable LayDuongRayTheoGa(int maGa)
        {
            string sql = @"SELECT MaDuongRay, SoHieuDuong, LoaiDuong, ChieuDaiHuuDungM, CoKeGa, TrangThai
                           FROM hatang.DuongRayGa
                           WHERE MaGa = @maGa
                           ORDER BY SoHieuDuong";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@maGa", maGa) });
        }

        public DataTable LayKhuGianTheoGa(int maGa)
        {
            string sql = @"
                SELECT kg.MaKhuGian, kg.MaGaDau, kg.MaGaCuoi, kg.CuLyKm, kg.TocDoToiDaKhach, 
                       kg.TocDoToiDaHang, kg.DoDocPermil, kg.CanDauMayDay, kg.TrangThai,
                       g1.TenGa AS TenGaDau, g2.TenGa AS TenGaCuoi, 
                       g1.MaGaCode AS MaGaDauCode, g2.MaGaCode AS MaGaCuoiCode,
                       g1.LyTrinhKm AS LyTrinhGaDau, g2.LyTrinhKm AS LyTrinhGaCuoi,
                       CASE WHEN kg.MaGaCuoi = @maGa THEN N'Khu gian phía Bắc' ELSE N'Khu gian phía Nam' END AS HuongTuyen,
                       ISNULL((SELECT COUNT(*) FROM hatang.DuongNgang dn WHERE dn.MaKhuGian = kg.MaKhuGian), 0) AS SoDuongNgang,
                       ISNULL((SELECT COUNT(*) FROM hatang.DuongNgang dn WHERE dn.MaKhuGian = kg.MaKhuGian AND dn.LaDiemDen = 1), 0) AS SoDiemDen
                FROM hatang.KhuGian kg
                JOIN hatang.Ga g1 ON kg.MaGaDau = g1.MaGa
                JOIN hatang.Ga g2 ON kg.MaGaCuoi = g2.MaGa
                WHERE kg.MaGaDau = @maGa OR kg.MaGaCuoi = @maGa
                ORDER BY kg.MaGaDau";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@maGa", maGa) });
        }

        public bool KiemTraMaGaCodeTonTai(string maGaCode, int excludeMaGa = 0)
        {
            string sql = @"SELECT COUNT(*) FROM hatang.Ga 
                           WHERE UPPER(MaGaCode) = UPPER(@code) AND (@exclude = 0 OR MaGa <> @exclude)";
            var p = new[]
            {
                new SqlParameter("@code", maGaCode.Trim()),
                new SqlParameter("@exclude", excludeMaGa)
            };
            object? res = DatabaseHelper.ExecuteScalar(sql, p);
            return res != null && Convert.ToInt32(res) > 0;
        }

        public bool KiemTraLyTrinhTonTai(int maTuyen, decimal lyTrinhKm, int excludeMaGa = 0)
        {
            string sql = @"SELECT COUNT(*) FROM hatang.Ga 
                           WHERE MaTuyen = @maTuyen AND ABS(LyTrinhKm - @km) < 0.001 AND (@exclude = 0 OR MaGa <> @exclude)";
            var p = new[]
            {
                new SqlParameter("@maTuyen", maTuyen),
                new SqlParameter("@km", lyTrinhKm),
                new SqlParameter("@exclude", excludeMaGa)
            };
            object? res = DatabaseHelper.ExecuteScalar(sql, p);
            return res != null && Convert.ToInt32(res) > 0;
        }

        public string? LayTenGaTheoLyTrinh(int maTuyen, decimal lyTrinhKm, int excludeMaGa = 0)
        {
            string sql = @"SELECT TOP 1 CONCAT(TenGa, N' (', MaGaCode, N')') FROM hatang.Ga 
                           WHERE MaTuyen = @maTuyen AND ABS(LyTrinhKm - @km) < 0.001 AND (@exclude = 0 OR MaGa <> @exclude)";
            var p = new[]
            {
                new SqlParameter("@maTuyen", maTuyen),
                new SqlParameter("@km", lyTrinhKm),
                new SqlParameter("@exclude", excludeMaGa)
            };
            object? res = DatabaseHelper.ExecuteScalar(sql, p);
            return res?.ToString();
        }

        public bool KiemTraTenGaTonTai(int maTuyen, string tenGa, int excludeMaGa = 0)
        {
            string sql = @"SELECT COUNT(*) FROM hatang.Ga 
                           WHERE MaTuyen = @maTuyen AND LOWER(LTRIM(RTRIM(TenGa))) = LOWER(@ten) AND (@exclude = 0 OR MaGa <> @exclude)";
            var p = new[]
            {
                new SqlParameter("@maTuyen", maTuyen),
                new SqlParameter("@ten", tenGa.Trim().ToLower()),
                new SqlParameter("@exclude", excludeMaGa)
            };
            object? res = DatabaseHelper.ExecuteScalar(sql, p);
            return res != null && Convert.ToInt32(res) > 0;
        }

        public bool KiemTraGaDangCoTau(int maGa)
        {
            string sql = @"SELECT COUNT(*) FROM hatang.DuongRayGa WHERE MaGa = @maGa AND TrangThai = 'CO_TAU'";
            var res = DatabaseHelper.ExecuteScalar(sql, new[] { new SqlParameter("@maGa", maGa) });
            return res != null && Convert.ToInt32(res) > 0;
        }
    }
}
