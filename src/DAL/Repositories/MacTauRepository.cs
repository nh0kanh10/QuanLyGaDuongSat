using System;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using ET.VanHanh;

namespace DAL.Repositories
{
    public class MacTauRepository
    {
        public DataTable LayDanhSach(bool? chiDangKhaiThac = null)
        {
            string sql = @"SELECT mt.MaMacTau, mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay, 
                                  mt.MaGaDi, mt.MaGaDen, mt.ThoiGianChuanGio, mt.MucUuTien, mt.DangKhaiThac,
                                  mt.NgayTao, mt.NguoiTao, mt.NgayCapNhat, mt.NguoiCapNhat,
                                  g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen,
                                  g1.MaGaCode AS MaGaDiCode, g2.MaGaCode AS MaGaDenCode,
                                  g1.LyTrinhKm AS LyTrinhKmGaDi, g2.LyTrinhKm AS LyTrinhKmGaDen,
                                  ISNULL((SELECT COUNT(*) FROM vanhanh.ChuyenTau ct WHERE ct.MaMacTau = mt.MaMacTau), 0) AS SoChuyenTauDaChay
                           FROM vanhanh.MacTauMau mt
                           JOIN hatang.Ga g1 ON mt.MaGaDi = g1.MaGa
                           JOIN hatang.Ga g2 ON mt.MaGaDen = g2.MaGa
                           WHERE (@chiDangKhaiThac IS NULL OR mt.DangKhaiThac = @chiDangKhaiThac)
                           ORDER BY mt.DangKhaiThac DESC, mt.SoHieuMacTau ASC";

            var parameters = new[]
            {
                new SqlParameter("@chiDangKhaiThac", chiDangKhaiThac.HasValue ? (object)chiDangKhaiThac.Value : DBNull.Value)
            };

            return DatabaseHelper.ExecuteQuery(sql, parameters);
        }

        public DataTable LayChiTiet(int maMacTau)
        {
            string sql = @"SELECT mt.MaMacTau, mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay, 
                                  mt.MaGaDi, mt.MaGaDen, mt.ThoiGianChuanGio, mt.MucUuTien, mt.DangKhaiThac,
                                  mt.NgayTao, mt.NguoiTao, mt.NgayCapNhat, mt.NguoiCapNhat,
                                  g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen,
                                  g1.MaGaCode AS MaGaDiCode, g2.MaGaCode AS MaGaDenCode,
                                  g1.LyTrinhKm AS LyTrinhKmGaDi, g2.LyTrinhKm AS LyTrinhKmGaDen,
                                  ISNULL((SELECT COUNT(*) FROM vanhanh.ChuyenTau ct WHERE ct.MaMacTau = mt.MaMacTau), 0) AS SoChuyenTauDaChay
                           FROM vanhanh.MacTauMau mt
                           JOIN hatang.Ga g1 ON mt.MaGaDi = g1.MaGa
                           JOIN hatang.Ga g2 ON mt.MaGaDen = g2.MaGa
                           WHERE mt.MaMacTau = @maMacTau";

            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@maMacTau", maMacTau) });
        }

        public bool KiemTraTrungSoHieu(string soHieuMacTau, int maMacTauHienTai = 0)
        {
            string sql = @"SELECT COUNT(*) FROM vanhanh.MacTauMau 
                           WHERE UPPER(SoHieuMacTau) = UPPER(@soHieu) AND MaMacTau <> @maHienTai";

            var p = new[]
            {
                new SqlParameter("@soHieu", soHieuMacTau.Trim()),
                new SqlParameter("@maHienTai", maMacTauHienTai)
            };

            object? res = DatabaseHelper.ExecuteScalar(sql, p);
            return res != null && Convert.ToInt32(res) > 0;
        }

        public int Them(MacTauMau m)
        {
            string sql = @"INSERT INTO vanhanh.MacTauMau (SoHieuMacTau, LoaiTau, HuongChay, MaGaDi, MaGaDen, ThoiGianChuanGio, MucUuTien, DangKhaiThac, NgayTao, NguoiTao)
                           VALUES (@soHieu, @loai, @huong, @gaDi, @gaDen, @tg, @uuTien, @kt, SYSUTCDATETIME(), @nguoiTao);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var p = new[]
            {
                new SqlParameter("@soHieu", m.SoHieuMacTau.Trim().ToUpper()),
                new SqlParameter("@loai", m.LoaiTau),
                new SqlParameter("@huong", m.HuongChay),
                new SqlParameter("@gaDi", m.MaGaDi),
                new SqlParameter("@gaDen", m.MaGaDen),
                new SqlParameter("@tg", m.ThoiGianChuanGio),
                new SqlParameter("@uuTien", m.MucUuTien),
                new SqlParameter("@kt", m.DangKhaiThac),
                new SqlParameter("@nguoiTao", string.IsNullOrWhiteSpace(m.NguoiTao) ? "admin" : m.NguoiTao)
            };

            object? res = DatabaseHelper.ExecuteScalar(sql, p);
            return res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;
        }

        public bool CapNhat(MacTauMau m)
        {
            string sql = @"UPDATE vanhanh.MacTauMau
                           SET SoHieuMacTau = @soHieu,
                               LoaiTau = @loai,
                               HuongChay = @huong,
                               MaGaDi = @gaDi,
                               MaGaDen = @gaDen,
                               ThoiGianChuanGio = @tg,
                               MucUuTien = @uuTien,
                               DangKhaiThac = @kt,
                               NgayCapNhat = SYSUTCDATETIME(),
                               NguoiCapNhat = @nguoiCapNhat
                           WHERE MaMacTau = @id";

            var p = new[]
            {
                new SqlParameter("@id", m.MaMacTau),
                new SqlParameter("@soHieu", m.SoHieuMacTau.Trim().ToUpper()),
                new SqlParameter("@loai", m.LoaiTau),
                new SqlParameter("@huong", m.HuongChay),
                new SqlParameter("@gaDi", m.MaGaDi),
                new SqlParameter("@gaDen", m.MaGaDen),
                new SqlParameter("@tg", m.ThoiGianChuanGio),
                new SqlParameter("@uuTien", m.MucUuTien),
                new SqlParameter("@kt", m.DangKhaiThac),
                new SqlParameter("@nguoiCapNhat", string.IsNullOrWhiteSpace(m.NguoiCapNhat) ? "admin" : m.NguoiCapNhat)
            };

            return DatabaseHelper.ExecuteNonQuery(sql, p) > 0;
        }

        public int DemSoChuyenTauLienQuan(int maMacTau)
        {
            string sql = "SELECT COUNT(*) FROM vanhanh.ChuyenTau WHERE MaMacTau = @id";
            object? res = DatabaseHelper.ExecuteScalar(sql, new[] { new SqlParameter("@id", maMacTau) });
            return res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;
        }

        public bool Xoa(int maMacTau)
        {
            string sql = "DELETE FROM vanhanh.MacTauMau WHERE MaMacTau = @id";
            return DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@id", maMacTau) }) > 0;
        }

        public bool DoiTrangThaiKhaiThac(int maMacTau, bool dangKhaiThac, string nguoiCapNhat = "admin")
        {
            string sql = @"UPDATE vanhanh.MacTauMau 
                           SET DangKhaiThac = @kt, 
                               NgayCapNhat = SYSUTCDATETIME(), 
                               NguoiCapNhat = @nguoiCapNhat 
                           WHERE MaMacTau = @id";

            var p = new[]
            {
                new SqlParameter("@id", maMacTau),
                new SqlParameter("@kt", dangKhaiThac),
                new SqlParameter("@nguoiCapNhat", string.IsNullOrWhiteSpace(nguoiCapNhat) ? "admin" : nguoiCapNhat)
            };

            return DatabaseHelper.ExecuteNonQuery(sql, p) > 0;
        }

        public DataTable LayDanhSachChuyenTauKeHoachTuongLai(int maMacTau)
        {
            string sql = @"SELECT ct.MaChuyenTau, ct.NgayXuatPhat, ct.GioXuatPhatKH, ct.GioVeDichKH, ct.TrangThai,
                                  mt.SoHieuMacTau, g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen
                           FROM vanhanh.ChuyenTau ct
                           JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                           JOIN hatang.Ga g1 ON mt.MaGaDi = g1.MaGa
                           JOIN hatang.Ga g2 ON mt.MaGaDen = g2.MaGa
                           WHERE ct.MaMacTau = @id
                             AND ct.NgayXuatPhat >= CAST(GETDATE() AS DATE)
                             AND ct.TrangThai IN ('DA_LEN_LICH', 'SAN_SANG')
                           ORDER BY ct.NgayXuatPhat ASC, ct.GioXuatPhatKH ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@id", maMacTau) });
        }

        public bool TamDungMacTauVaXuLyChuyen(int maMacTau, bool huyChuyenTuongLai, string lyDoHuy, string nguoiCapNhat, out int soChuyenDaHuy)
        {
            soChuyenDaHuy = 0;
            using var conn = new SqlConnection(DatabaseHelper.GetConnectionString());
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Cập nhật mác tàu sang Tạm ngừng khai thác (DangKhaiThac = 0)
                string sqlMac = @"UPDATE vanhanh.MacTauMau 
                                  SET DangKhaiThac = 0, 
                                      NgayCapNhat = SYSUTCDATETIME(), 
                                      NguoiCapNhat = @nguoi 
                                  WHERE MaMacTau = @id";
                using (var cmdMac = new SqlCommand(sqlMac, conn, trans))
                {
                    cmdMac.Parameters.AddWithValue("@id", maMacTau);
                    cmdMac.Parameters.AddWithValue("@nguoi", string.IsNullOrWhiteSpace(nguoiCapNhat) ? "admin" : nguoiCapNhat);
                    cmdMac.ExecuteNonQuery();
                }

                // 2. Nếu người dùng chọn Phương án B: Hủy toàn bộ các chuyến kế hoạch tương lai
                if (huyChuyenTuongLai)
                {
                    string sqlChuyen = @"UPDATE vanhanh.ChuyenTau 
                                         SET TrangThai = 'DA_HUY', 
                                             LyDoHuy = @lyDo,
                                             NgayCapNhat = SYSUTCDATETIME(),
                                             NguoiCapNhat = @nguoi
                                         WHERE MaMacTau = @id 
                                           AND NgayXuatPhat >= CAST(GETDATE() AS DATE)
                                           AND TrangThai IN ('DA_LEN_LICH', 'SAN_SANG')";
                    using (var cmdChuyen = new SqlCommand(sqlChuyen, conn, trans))
                    {
                        cmdChuyen.Parameters.AddWithValue("@id", maMacTau);
                        cmdChuyen.Parameters.AddWithValue("@lyDo", string.IsNullOrWhiteSpace(lyDoHuy) ? "Tạm ngừng khai thác mác tàu" : lyDoHuy);
                        cmdChuyen.Parameters.AddWithValue("@nguoi", string.IsNullOrWhiteSpace(nguoiCapNhat) ? "admin" : nguoiCapNhat);
                        soChuyenDaHuy = cmdChuyen.ExecuteNonQuery();
                    }
                }

                trans.Commit();
                return true;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
    }
}

