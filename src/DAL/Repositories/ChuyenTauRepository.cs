using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using ET.VanHanh;
using ET.VanTai;

namespace DAL.Repositories
{
    public class ChuyenTauRepository
    {
        public DataTable LayDanhSach(DateTime? ngay = null, int? maMacTau = null, string? trangThai = null)
        {
            string sql = @"SELECT ct.MaChuyenTau, ct.MaMacTau, ct.NgayXuatPhat, ct.GioXuatPhatKH, ct.GioVeDichKH,
                                  ct.GioXuatPhatTT, ct.GioVeDichTT, ct.SoPhutTreLuyKe, ct.LyDoHuy, ct.TrangThai,
                                  mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay,
                                  g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen,
                                  ISNULL((SELECT COUNT(*) FROM vanhanh.LichDungGa ld WHERE ld.MaChuyenTau = ct.MaChuyenTau), 0) AS SoDiemDung,
                                  ISNULL((SELECT COUNT(*) FROM vantai.ToaXeKhach tx WHERE tx.MaChuyenTau = ct.MaChuyenTau), 0) AS SoToaXe,
                                  dm1.SoHieuDauMay AS SoHieuDauMayChinh,
                                  dt.TongChieuDaiM, dt.TongTrongLuongTan, dt.DaDuyetAnToan
                           FROM vanhanh.ChuyenTau ct
                           JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                           JOIN hatang.Ga g1 ON mt.MaGaDi = g1.MaGa
                           JOIN hatang.Ga g2 ON mt.MaGaDen = g2.MaGa
                           LEFT JOIN vanhanh.DoanTau dt ON ct.MaChuyenTau = dt.MaChuyenTau
                           LEFT JOIN phuongtien.DauMay dm1 ON dt.MaDauMayChinh = dm1.MaDauMay
                           WHERE 1=1";

            var parameters = new List<SqlParameter>();

            if (ngay.HasValue)
            {
                sql += " AND ct.NgayXuatPhat = @ngay";
                parameters.Add(new SqlParameter("@ngay", ngay.Value.Date));
            }

            if (maMacTau.HasValue && maMacTau.Value > 0)
            {
                sql += " AND ct.MaMacTau = @maMacTau";
                parameters.Add(new SqlParameter("@maMacTau", maMacTau.Value));
            }

            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "ALL")
            {
                sql += " AND ct.TrangThai = @trangThai";
                parameters.Add(new SqlParameter("@trangThai", trangThai));
            }

            sql += " ORDER BY ct.NgayXuatPhat DESC, ct.GioXuatPhatKH ASC";

            return DatabaseHelper.ExecuteQuery(sql, parameters.Count > 0 ? parameters.ToArray() : null);
        }

        public DataTable LayDanhSachMacTau()
        {
            string sql = @"SELECT mt.MaMacTau, mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay, 
                                  mt.MaGaDi, mt.MaGaDen, mt.ThoiGianChuanGio, mt.MucUuTien, mt.DangKhaiThac,
                                  g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen,
                                  g1.MaGaCode AS MaGaDiCode, g2.MaGaCode AS MaGaDenCode
                           FROM vanhanh.MacTauMau mt
                           JOIN hatang.Ga g1 ON mt.MaGaDi = g1.MaGa
                           JOIN hatang.Ga g2 ON mt.MaGaDen = g2.MaGa
                           WHERE mt.DangKhaiThac = 1
                           ORDER BY mt.SoHieuMacTau";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable LayLichDungGa(int maChuyenTau)
        {
            string sql = @"SELECT ld.MaDiemDung, ld.MaChuyenTau, ld.MaGa, ld.ThuTuDung,
                                  ld.GioDenKeHoach, ld.GioDiKeHoach, ld.GioDenThucTe, ld.GioDiThucTe,
                                  ld.MaDuongRay, ld.LaDiemTranh,
                                  g.TenGa, g.MaGaCode, g.LyTrinhKm,
                                  DATEDIFF(MINUTE, ld.GioDenKeHoach, ld.GioDiKeHoach) AS ThoiGianDungPhut
                           FROM vanhanh.LichDungGa ld
                           JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                           WHERE ld.MaChuyenTau = @maChuyenTau
                           ORDER BY ld.ThuTuDung ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@maChuyenTau", maChuyenTau) });
        }

        public DataTable LayDanhSachToaXe(int maChuyenTau)
        {
            string sql = @"SELECT tx.MaToaXeKhach, tx.MaChuyenTau, tx.NhanHieuToa, tx.LoaiToa, tx.ThuTuToa, tx.SucChua,
                                  CASE tx.LoaiToa
                                      WHEN 'NC' THEN N'Ngồi Cứng'
                                      WHEN 'NML' THEN N'Ngồi Mềm Lạnh'
                                      WHEN 'BN' THEN N'Giường Nằm K6'
                                      WHEN 'AN' THEN N'Giường Nằm K4'
                                      ELSE tx.LoaiToa
                                  END AS TenLoaiToa,
                                  ISNULL((SELECT COUNT(*) FROM vantai.ChoNgoi cn WHERE cn.MaToaXeKhach = tx.MaToaXeKhach), 0) AS SoChoDaThietLap
                           FROM vantai.ToaXeKhach tx
                           WHERE tx.MaChuyenTau = @maChuyenTau
                           ORDER BY tx.ThuTuToa ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@maChuyenTau", maChuyenTau) });
        }

        public int Them(ChuyenTau ct)
        {
            string sql = @"INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, TrangThai, SoPhutTreLuyKe, LyDoHuy)
                           VALUES (@mac, @ngay, @gioDi, @gioDen, @tt, @tre, @lyDo);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var p = new[]
            {
                new SqlParameter("@mac", ct.MaMacTau),
                new SqlParameter("@ngay", ct.NgayXuatPhat.Date),
                new SqlParameter("@gioDi", ct.GioXuatPhatKH),
                new SqlParameter("@gioDen", ct.GioVeDichKH),
                new SqlParameter("@tt", ct.TrangThai),
                new SqlParameter("@tre", ct.SoPhutTreLuyKe),
                new SqlParameter("@lyDo", (object?)ct.LyDoHuy ?? DBNull.Value)
            };
            object? result = DatabaseHelper.ExecuteScalar(sql, p);
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        public int CapNhat(ChuyenTau ct)
        {
            string sql = @"UPDATE vanhanh.ChuyenTau 
                           SET MaMacTau = @mac,
                               NgayXuatPhat = @ngay,
                               GioXuatPhatKH = @gioDi,
                               GioVeDichKH = @gioDen,
                               GioXuatPhatTT = @gioDiTT,
                               GioVeDichTT = @gioDenTT,
                               SoPhutTreLuyKe = @tre,
                               LyDoHuy = @lyDo,
                               TrangThai = @tt
                           WHERE MaChuyenTau = @id";
            var p = new[]
            {
                new SqlParameter("@id", ct.MaChuyenTau),
                new SqlParameter("@mac", ct.MaMacTau),
                new SqlParameter("@ngay", ct.NgayXuatPhat.Date),
                new SqlParameter("@gioDi", ct.GioXuatPhatKH),
                new SqlParameter("@gioDen", ct.GioVeDichKH),
                new SqlParameter("@gioDiTT", (object?)ct.GioXuatPhatTT ?? DBNull.Value),
                new SqlParameter("@gioDenTT", (object?)ct.GioVeDichTT ?? DBNull.Value),
                new SqlParameter("@tre", ct.SoPhutTreLuyKe),
                new SqlParameter("@lyDo", (object?)ct.LyDoHuy ?? DBNull.Value),
                new SqlParameter("@tt", ct.TrangThai)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int CapNhatTrangThai(int maChuyenTau, string trangThai)
        {
            string sql = "UPDATE vanhanh.ChuyenTau SET TrangThai=@tt WHERE MaChuyenTau=@id";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@id", maChuyenTau),
                new SqlParameter("@tt", trangThai)
            });
        }

        public int CapNhatGioThucTe(int maDiemDung, DateTime? gioDenTT, DateTime? gioDiTT)
        {
            string sql = @"UPDATE vanhanh.LichDungGa 
                           SET GioDenThucTe = @denTT, GioDiThucTe = @diTT 
                           WHERE MaDiemDung = @id";
            var p = new[]
            {
                new SqlParameter("@id", maDiemDung),
                new SqlParameter("@denTT", (object?)gioDenTT ?? DBNull.Value),
                new SqlParameter("@diTT", (object?)gioDiTT ?? DBNull.Value)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int ThemLichDungGa(LichDungGa ld)
        {
            string sql = @"INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, GioDenThucTe, GioDiThucTe, MaDuongRay, LaDiemTranh)
                           VALUES (@ct, @ga, @tt, @denKH, @diKH, @denTT, @diTT, @ray, @tranh)";
            var p = new[]
            {
                new SqlParameter("@ct", ld.MaChuyenTau),
                new SqlParameter("@ga", ld.MaGa),
                new SqlParameter("@tt", ld.ThuTuDung),
                new SqlParameter("@denKH", ld.GioDenKeHoach),
                new SqlParameter("@diKH", ld.GioDiKeHoach),
                new SqlParameter("@denTT", (object?)ld.GioDenThucTe ?? DBNull.Value),
                new SqlParameter("@diTT", (object?)ld.GioDiThucTe ?? DBNull.Value),
                new SqlParameter("@ray", (object?)ld.MaDuongRay ?? DBNull.Value),
                new SqlParameter("@tranh", ld.LaDiemTranh)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public int XoaLichDungGa(int maDiemDung)
        {
            string sql = "DELETE FROM vanhanh.LichDungGa WHERE MaDiemDung = @id";
            return DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@id", maDiemDung) });
        }

        public int SaoChepLichDungTuChuyenKhac(int maChuyenNguon, int maChuyenDich, DateTime ngayKhoiHanhMoi)
        {
            string sql = @"INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, LaDiemTranh)
                           SELECT @dich, ld.MaGa, ld.ThuTuDung,
                                  DATEADD(DAY, DATEDIFF(DAY, ct.NgayXuatPhat, @ngayMoi), ld.GioDenKeHoach),
                                  DATEADD(DAY, DATEDIFF(DAY, ct.NgayXuatPhat, @ngayMoi), ld.GioDiKeHoach),
                                  ld.LaDiemTranh
                           FROM vanhanh.LichDungGa ld
                           JOIN vanhanh.ChuyenTau ct ON ld.MaChuyenTau = ct.MaChuyenTau
                           WHERE ld.MaChuyenTau = @nguon
                           ORDER BY ld.ThuTuDung";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@nguon", maChuyenNguon),
                new SqlParameter("@dich", maChuyenDich),
                new SqlParameter("@ngayMoi", ngayKhoiHanhMoi.Date)
            });
        }

        public int TimChuyenMauGanNhatCungMac(int maMacTau, int excludeMaChuyen)
        {
            string sql = @"SELECT TOP 1 ld.MaChuyenTau
                           FROM vanhanh.LichDungGa ld
                           JOIN vanhanh.ChuyenTau ct ON ld.MaChuyenTau = ct.MaChuyenTau
                           WHERE ct.MaMacTau = @mac AND ct.MaChuyenTau <> @exclude
                           GROUP BY ld.MaChuyenTau, ct.NgayXuatPhat
                           ORDER BY ct.NgayXuatPhat DESC";
            object? res = DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@mac", maMacTau),
                new SqlParameter("@exclude", excludeMaChuyen)
            });
            return res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;
        }

        // =========================================================================
        // PHÂN HỆ QUẢN LÝ ĐOÀN TÀU & RÀNG BUỘC CHỐNG TRANH CHẤP ĐẦU MÁY (DoanTau, DauMay)
        // =========================================================================

        public DataTable LayThongTinDoanTau(int maChuyenTau)
        {
            string sql = @"SELECT dt.MaDoanTau, dt.MaChuyenTau, dt.MaDauMayChinh, dt.MaDauMayDay,
                                  dt.TongSoToa, dt.TongChieuDaiM, dt.TongTrongLuongTan, dt.DaDuyetAnToan,
                                  dm1.SoHieuDauMay AS SoHieuDauMayChinh, ddm1.MaDongCode AS DongMayChinh,
                                  dm2.SoHieuDauMay AS SoHieuDauMayDay, ddm2.MaDongCode AS DongMayDay
                           FROM vanhanh.DoanTau dt
                           LEFT JOIN phuongtien.DauMay dm1 ON dt.MaDauMayChinh = dm1.MaDauMay
                           LEFT JOIN phuongtien.DongDauMay ddm1 ON dm1.MaDongDauMay = ddm1.MaDongDauMay
                           LEFT JOIN phuongtien.DauMay dm2 ON dt.MaDauMayDay = dm2.MaDauMay
                           LEFT JOIN phuongtien.DongDauMay ddm2 ON dm2.MaDongDauMay = ddm2.MaDongDauMay
                           WHERE dt.MaChuyenTau = @maChuyenTau";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@maChuyenTau", maChuyenTau) });
        }

        public DataTable LayDanhSachDauMay(DateTime? gioDi = null, DateTime? gioDen = null, int? maChuyenTauHienTai = null)
        {
            string sql = @"SELECT dm.MaDauMay, dm.SoHieuDauMay, dm.MaDongDauMay, dm.DonViQuanLy, dm.TrangThai,
                                  ddm.MaDongCode, ddm.CongSuatHP, ddm.TocDoToiDaKmh, ddm.SucKeoToiDaTan,
                                  ddm.TrongLuongTan, ddm.ChieuDaiM,
                                  CASE 
                                      WHEN @gioDi IS NOT NULL AND @gioDen IS NOT NULL AND EXISTS (
                                          SELECT 1 FROM vanhanh.DoanTau dt2
                                          JOIN vanhanh.ChuyenTau ct2 ON dt2.MaChuyenTau = ct2.MaChuyenTau
                                          WHERE (dt2.MaDauMayChinh = dm.MaDauMay OR dt2.MaDauMayDay = dm.MaDauMay)
                                            AND ct2.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
                                            AND (@maChuyen IS NULL OR ct2.MaChuyenTau <> @maChuyen)
                                            AND (CASE WHEN ct2.GioXuatPhatKH > @gioDi THEN ct2.GioXuatPhatKH ELSE @gioDi END)
                                              < (CASE WHEN ct2.GioVeDichKH < @gioDen THEN ct2.GioVeDichKH ELSE @gioDen END)
                                      ) THEN CAST(1 AS BIT)
                                      ELSE CAST(0 AS BIT)
                                  END AS DangTrungLich,
                                  (
                                      SELECT TOP 1 mt2.SoHieuMacTau + ' (' + FORMAT(ct2.GioXuatPhatKH, 'HH:mm dd/MM') + ' - ' + FORMAT(ct2.GioVeDichKH, 'HH:mm dd/MM') + ')'
                                      FROM vanhanh.DoanTau dt2
                                      JOIN vanhanh.ChuyenTau ct2 ON dt2.MaChuyenTau = ct2.MaChuyenTau
                                      JOIN vanhanh.MacTauMau mt2 ON ct2.MaMacTau = mt2.MaMacTau
                                      WHERE (dt2.MaDauMayChinh = dm.MaDauMay OR dt2.MaDauMayDay = dm.MaDauMay)
                                        AND ct2.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
                                        AND (@maChuyen IS NULL OR ct2.MaChuyenTau <> @maChuyen)
                                        AND (CASE WHEN ct2.GioXuatPhatKH > @gioDi THEN ct2.GioXuatPhatKH ELSE @gioDi END)
                                          < (CASE WHEN ct2.GioVeDichKH < @gioDen THEN ct2.GioVeDichKH ELSE @gioDen END)
                                  ) AS ChuyenTauDangBan
                           FROM phuongtien.DauMay dm
                           JOIN phuongtien.DongDauMay ddm ON dm.MaDongDauMay = ddm.MaDongDauMay
                           ORDER BY dm.SoHieuDauMay ASC";
            var p = new List<SqlParameter>
            {
                new SqlParameter("@gioDi", (object?)gioDi ?? DBNull.Value),
                new SqlParameter("@gioDen", (object?)gioDen ?? DBNull.Value),
                new SqlParameter("@maChuyen", (object?)maChuyenTauHienTai ?? DBNull.Value)
            };
            return DatabaseHelper.ExecuteQuery(sql, p.ToArray());
        }

        public bool KiemTraXungDotDauMay(int maDauMay, DateTime gioDi, DateTime gioDen, int maChuyenHienTai, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            string sql = @"SELECT TOP 1 dm.SoHieuDauMay, mt.SoHieuMacTau, ct.GioXuatPhatKH, ct.GioVeDichKH
                           FROM vanhanh.DoanTau dt
                           JOIN vanhanh.ChuyenTau ct ON dt.MaChuyenTau = ct.MaChuyenTau
                           JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                           JOIN phuongtien.DauMay dm ON (dt.MaDauMayChinh = dm.MaDauMay OR dt.MaDauMayDay = dm.MaDauMay)
                           WHERE dm.MaDauMay = @maDauMay
                             AND ct.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
                             AND (@maChuyen <= 0 OR ct.MaChuyenTau <> @maChuyen)
                             AND (CASE WHEN ct.GioXuatPhatKH > @gioDi THEN ct.GioXuatPhatKH ELSE @gioDi END)
                               < (CASE WHEN ct.GioVeDichKH < @gioDen THEN ct.GioVeDichKH ELSE @gioDen END)";
            var p = new[]
            {
                new SqlParameter("@maDauMay", maDauMay),
                new SqlParameter("@gioDi", gioDi),
                new SqlParameter("@gioDen", gioDen),
                new SqlParameter("@maChuyen", maChuyenHienTai)
            };
            DataTable dt = DatabaseHelper.ExecuteQuery(sql, p);
            if (dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];
                string soHieuDM = r["SoHieuDauMay"].ToString() ?? "";
                string macTau = r["SoHieuMacTau"].ToString() ?? "";
                DateTime veDich = Convert.ToDateTime(r["GioVeDichKH"]);
                thongBaoLoi = $"Lỗi xung đột: Đầu máy {soHieuDM} đang thực hiện chuyến {macTau} (dự kiến về đích lúc {veDich:HH:mm dd/MM/yyyy}). Không thể gán cho chuyến xuất phát lúc {gioDi:HH:mm dd/MM/yyyy}.";
                return false;
            }
            return true;
        }

        public int LuuDoanTau(DoanTau dt)
        {
            string checkSql = "SELECT COUNT(*) FROM vanhanh.DoanTau WHERE MaChuyenTau = @ct";
            object? existsObj = DatabaseHelper.ExecuteScalar(checkSql, new[] { new SqlParameter("@ct", dt.MaChuyenTau) });
            bool exists = existsObj != null && Convert.ToInt32(existsObj) > 0;

            if (exists)
            {
                string sqlUpdate = @"UPDATE vanhanh.DoanTau
                                     SET MaDauMayChinh = @chinh,
                                         MaDauMayDay = @day,
                                         TongSoToa = @soToa,
                                         TongChieuDaiM = @dai,
                                         TongTrongLuongTan = @trongLuong,
                                         DaDuyetAnToan = @duyet
                                     WHERE MaChuyenTau = @ct";
                return DatabaseHelper.ExecuteNonQuery(sqlUpdate, new[]
                {
                    new SqlParameter("@ct", dt.MaChuyenTau),
                    new SqlParameter("@chinh", dt.MaDauMayChinh),
                    new SqlParameter("@day", (object?)dt.MaDauMayDay ?? DBNull.Value),
                    new SqlParameter("@soToa", dt.TongSoToa),
                    new SqlParameter("@dai", dt.TongChieuDaiM),
                    new SqlParameter("@trongLuong", dt.TongTrongLuongTan),
                    new SqlParameter("@duyet", dt.DaDuyetAnToan)
                });
            }
            else
            {
                string sqlInsert = @"INSERT INTO vanhanh.DoanTau (MaChuyenTau, MaDauMayChinh, MaDauMayDay, TongSoToa, TongChieuDaiM, TongTrongLuongTan, DaDuyetAnToan)
                                     VALUES (@ct, @chinh, @day, @soToa, @dai, @trongLuong, @duyet)";
                return DatabaseHelper.ExecuteNonQuery(sqlInsert, new[]
                {
                    new SqlParameter("@ct", dt.MaChuyenTau),
                    new SqlParameter("@chinh", dt.MaDauMayChinh),
                    new SqlParameter("@day", (object?)dt.MaDauMayDay ?? DBNull.Value),
                    new SqlParameter("@soToa", dt.TongSoToa),
                    new SqlParameter("@dai", dt.TongChieuDaiM),
                    new SqlParameter("@trongLuong", dt.TongTrongLuongTan),
                    new SqlParameter("@duyet", dt.DaDuyetAnToan)
                });
            }
        }

        public bool KiemTraChieuDaiVoiGaDung(int maChuyenTau, decimal tongChieuDaiM, out string canhBao)
        {
            canhBao = string.Empty;
            string sql = @"SELECT TOP 1 g.TenGa, MIN(dr.ChieuDaiHuuDungM) AS MinDuongTranh
                           FROM vanhanh.LichDungGa ld
                           JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                           JOIN hatang.DuongRayGa dr ON ld.MaGa = dr.MaGa AND dr.LoaiDuong = 'DUONG_TRANH'
                           WHERE ld.MaChuyenTau = @ct
                           GROUP BY g.TenGa, g.LyTrinhKm
                           HAVING MIN(dr.ChieuDaiHuuDungM) < @chieuDai
                           ORDER BY MinDuongTranh ASC";
            DataTable dt = DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@ct", maChuyenTau),
                new SqlParameter("@chieuDai", tongChieuDaiM)
            });
            if (dt.Rows.Count > 0)
            {
                string tenGa = dt.Rows[0]["TenGa"].ToString() ?? "";
                int minM = Convert.ToInt32(dt.Rows[0]["MinDuongTranh"]);
                canhBao = $"Cảnh báo an toàn: Chiều dài đoàn tàu ({tongChieuDaiM:F1}m) vượt quá chiều dài đường tránh tại Ga {tenGa} ({minM}m)!";
                return false;
            }
            return true;
        }

        // =========================================================================
        // PHÂN HỆ QUẢN LÝ BIÊN CHẾ TOA XE KHÁCH (ToaXeKhach, ChoNgoi)
        // =========================================================================

        public int ThemToaXeKhach(ToaXeKhach tx, bool tuDongSinhGhe)
        {
            string sql = @"INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua)
                           VALUES (@ct, @nhanHieu, @loai, @thuTu, @sucChua);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var p = new[]
            {
                new SqlParameter("@ct", tx.MaChuyenTau),
                new SqlParameter("@nhanHieu", tx.NhanHieuToa),
                new SqlParameter("@loai", tx.LoaiToa),
                new SqlParameter("@thuTu", tx.ThuTuToa),
                new SqlParameter("@sucChua", tx.SucChua)
            };
            object? res = DatabaseHelper.ExecuteScalar(sql, p);
            int maToa = res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;

            if (maToa > 0 && tuDongSinhGhe && tx.SucChua > 0)
            {
                SinhGheChoToa(maToa, tx.LoaiToa, tx.SucChua);
            }

            CapNhatThongSoDoanTauTuDong(tx.MaChuyenTau);
            return maToa;
        }

        private void SinhGheChoToa(int maToa, string loaiToa, int sucChua)
        {
            for (int i = 1; i <= sucChua; i++)
            {
                int? tangGiuong = null;
                if (loaiToa == "AN")
                {
                    tangGiuong = ((i - 1) % 2) + 1;
                }
                else if (loaiToa == "BN")
                {
                    tangGiuong = ((i - 1) % 3) + 1;
                }

                string sqlGhe = @"INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
                                  VALUES (@toa, @soGhe, @tang, 0)";
                DatabaseHelper.ExecuteNonQuery(sqlGhe, new[]
                {
                    new SqlParameter("@toa", maToa),
                    new SqlParameter("@soGhe", i),
                    new SqlParameter("@tang", (object?)tangGiuong ?? DBNull.Value)
                });
            }
        }

        public bool XoaToaXeKhach(int maToaXeKhach, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            string checkVeSql = @"SELECT COUNT(*) 
                                  FROM vantai.Ve v
                                  JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
                                  WHERE cn.MaToaXeKhach = @toa";
            object? veCount = DatabaseHelper.ExecuteScalar(checkVeSql, new[] { new SqlParameter("@toa", maToaXeKhach) });
            if (veCount != null && Convert.ToInt32(veCount) > 0)
            {
                thongBaoLoi = $"Không thể xóa toa xe này vì đã có {Convert.ToInt32(veCount)} vé được bán cho hành khách!";
                return false;
            }

            string getCtSql = "SELECT MaChuyenTau FROM vantai.ToaXeKhach WHERE MaToaXeKhach = @toa";
            object? ctObj = DatabaseHelper.ExecuteScalar(getCtSql, new[] { new SqlParameter("@toa", maToaXeKhach) });
            int maChuyenTau = ctObj != null && ctObj != DBNull.Value ? Convert.ToInt32(ctObj) : 0;

            string delSql = @"DELETE FROM vantai.ChoNgoi WHERE MaToaXeKhach = @toa;
                              DELETE FROM vantai.ToaXeKhach WHERE MaToaXeKhach = @toa;";
            DatabaseHelper.ExecuteNonQuery(delSql, new[] { new SqlParameter("@toa", maToaXeKhach) });

            if (maChuyenTau > 0)
            {
                CapNhatThongSoDoanTauTuDong(maChuyenTau);
            }
            return true;
        }

        public void CapNhatThongSoDoanTauTuDong(int maChuyenTau)
        {
            string checkSql = "SELECT COUNT(*) FROM vanhanh.DoanTau WHERE MaChuyenTau = @ct";
            object? res = DatabaseHelper.ExecuteScalar(checkSql, new[] { new SqlParameter("@ct", maChuyenTau) });
            if (res == null || Convert.ToInt32(res) == 0) return;

            string calcSql = @"SELECT COUNT(*) AS SoToa FROM vantai.ToaXeKhach WHERE MaChuyenTau = @ct";
            DataTable dt = DatabaseHelper.ExecuteQuery(calcSql, new[] { new SqlParameter("@ct", maChuyenTau) });
            int soToa = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["SoToa"]) : 0;
            if (soToa == 0) soToa = 1;

            decimal chieuDai = 16.5m + (soToa * 20.0m);
            decimal trongLuong = 78.0m + (soToa * 40.0m);

            string updateSql = @"UPDATE vanhanh.DoanTau
                                 SET TongSoToa = @soToa,
                                     TongChieuDaiM = @dai,
                                     TongTrongLuongTan = @trongLuong
                                 WHERE MaChuyenTau = @ct";
            DatabaseHelper.ExecuteNonQuery(updateSql, new[]
            {
                new SqlParameter("@ct", maChuyenTau),
                new SqlParameter("@soToa", soToa),
                new SqlParameter("@dai", chieuDai),
                new SqlParameter("@trongLuong", trongLuong)
            });
        }

        public int SaoChepBienCheToaXe(int maChuyenNguon, int maChuyenDich)
        {
            string sqlSrc = @"SELECT NhanHieuToa, LoaiToa, ThuTuToa, SucChua 
                              FROM vantai.ToaXeKhach 
                              WHERE MaChuyenTau = @nguon
                              ORDER BY ThuTuToa";
            DataTable dt = DatabaseHelper.ExecuteQuery(sqlSrc, new[] { new SqlParameter("@nguon", maChuyenNguon) });
            int count = 0;
            foreach (DataRow r in dt.Rows)
            {
                var tx = new ToaXeKhach
                {
                    MaChuyenTau = maChuyenDich,
                    NhanHieuToa = r["NhanHieuToa"].ToString() ?? "",
                    LoaiToa = r["LoaiToa"].ToString() ?? "NC",
                    ThuTuToa = Convert.ToInt32(r["ThuTuToa"]),
                    SucChua = Convert.ToInt32(r["SucChua"])
                };
                ThemToaXeKhach(tx, true);
                count++;
            }
            return count;
        }

        public int Xoa(int maChuyenTau)
        {
            string sql = @"DELETE FROM vanhanh.ChiTietDoanTau WHERE MaDoanTau IN (SELECT MaDoanTau FROM vanhanh.DoanTau WHERE MaChuyenTau = @id);
                           DELETE FROM vanhanh.DoanTau WHERE MaChuyenTau = @id;
                           DELETE FROM vanhanh.NhatKyChamGio WHERE MaChuyenTau = @id;
                           DELETE FROM vantai.ChoNgoi WHERE MaToaXeKhach IN (SELECT MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @id);
                           DELETE FROM vantai.ToaXeKhach WHERE MaChuyenTau = @id;
                           DELETE FROM vanhanh.LichDungGa WHERE MaChuyenTau = @id;
                           DELETE FROM vanhanh.ChuyenTau WHERE MaChuyenTau = @id;";
            return DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@id", maChuyenTau) });
        }
    }
}
