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

            sql += " ORDER BY ct.NgayXuatPhat DESC, CAST(ct.GioXuatPhatKH AS TIME) ASC, ct.MaChuyenTau ASC";

            return DatabaseHelper.ExecuteQuery(sql, parameters.Count > 0 ? parameters.ToArray() : null);
        }

        public DataTable LayDanhSachMacTau()
        {
            string sql = @"SELECT mt.MaMacTau, mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay, 
                                  mt.MaGaDi, mt.MaGaDen, mt.ThoiGianChuanGio, mt.MucUuTien, mt.DangKhaiThac,
                                  g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen,
                                  g1.MaGaCode AS MaGaDiCode, g2.MaGaCode AS MaGaDenCode,
                                  g1.LyTrinhKm AS KmGaDi, g2.LyTrinhKm AS KmGaDen
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
                                  DATEDIFF(MINUTE, ld.GioDenKeHoach, ld.GioDiKeHoach) AS ThoiGianDungPhut,
                                  ISNULL(DATEDIFF(DAY, ct.NgayXuatPhat, CAST(ld.GioDenKeHoach AS DATE)), 0) AS SoNgayQuaDemDen,
                                  ISNULL(DATEDIFF(DAY, ct.NgayXuatPhat, CAST(ld.GioDiKeHoach AS DATE)), 0) AS SoNgayQuaDemDi
                           FROM vanhanh.LichDungGa ld
                           JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                           LEFT JOIN vanhanh.ChuyenTau ct ON ld.MaChuyenTau = ct.MaChuyenTau
                           WHERE ld.MaChuyenTau = @maChuyenTau
                           ORDER BY ld.ThuTuDung ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@maChuyenTau", maChuyenTau) });
        }

        public DataTable LayLichDungGaMauTheoMac(int maMacTau, DateTime ngayXuatPhat, TimeSpan gioXuatPhat)
        {
            DateTime dtXuatPhatMoi = ngayXuatPhat.Date.Add(gioXuatPhat);
            string sql = @"
                SELECT 0 AS MaDiemDung, 0 AS MaChuyenTau, ld.MaGa, ld.ThuTuDung,
                       DATEADD(MINUTE, DATEDIFF(MINUTE, ctNguon.GioXuatPhatKH, @gioXuatPhatMoi), ld.GioDenKeHoach) AS GioDenKeHoach,
                       DATEADD(MINUTE, DATEDIFF(MINUTE, ctNguon.GioXuatPhatKH, @gioXuatPhatMoi), ld.GioDiKeHoach) AS GioDiKeHoach,
                       ld.GioDenThucTe, ld.GioDiThucTe,
                       ld.MaDuongRay, ld.LaDiemTranh,
                       g.TenGa, g.MaGaCode, g.LyTrinhKm,
                       DATEDIFF(MINUTE, ld.GioDenKeHoach, ld.GioDiKeHoach) AS ThoiGianDungPhut,
                       ISNULL(DATEDIFF(DAY, @ngayXuatPhat, CAST(DATEADD(MINUTE, DATEDIFF(MINUTE, ctNguon.GioXuatPhatKH, @gioXuatPhatMoi), ld.GioDenKeHoach) AS DATE)), 0) AS SoNgayQuaDemDen,
                       ISNULL(DATEDIFF(DAY, @ngayXuatPhat, CAST(DATEADD(MINUTE, DATEDIFF(MINUTE, ctNguon.GioXuatPhatKH, @gioXuatPhatMoi), ld.GioDiKeHoach) AS DATE)), 0) AS SoNgayQuaDemDi
                FROM vanhanh.LichDungGa ld
                JOIN vanhanh.ChuyenTau ctNguon ON ld.MaChuyenTau = ctNguon.MaChuyenTau
                JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                WHERE ld.MaChuyenTau = (
                    SELECT TOP 1 ld2.MaChuyenTau
                    FROM vanhanh.LichDungGa ld2
                    JOIN vanhanh.ChuyenTau ct2 ON ld2.MaChuyenTau = ct2.MaChuyenTau
                    WHERE ct2.MaMacTau = @mac
                    GROUP BY ld2.MaChuyenTau, ct2.NgayXuatPhat
                    ORDER BY ct2.NgayXuatPhat DESC
                )
                ORDER BY ld.ThuTuDung ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@mac", maMacTau),
                new SqlParameter("@ngayXuatPhat", ngayXuatPhat.Date),
                new SqlParameter("@gioXuatPhatMoi", dtXuatPhatMoi)
            });
        }

        public DataTable LayDanhSachToaXe(int maChuyenTau)
        {
            // Ưu tiên 1: Lấy từ chi tiết đoàn tàu kỹ thuật (vanhanh.ChiTietDoanTau + phuongtien.ToaXe + phuongtien.ChungLoaiToa)
            string sqlDoanTau = @"SELECT c.ThuTuNoi AS ThuTuToa,
                                         tx.MaToaXe AS MaToaXeKhach,
                                         dt.MaChuyenTau,
                                         tx.SoHieuToaXe AS NhanHieuToa,
                                         cl.MaChungLoaiCode AS LoaiToa,
                                         cl.TenMoTa AS TenLoaiToa,
                                         ISNULL(cl.SucChuaChuan, 0) AS SucChua,
                                         tx.TuTrongTan,
                                         tx.TaiTrongToiDaTan,
                                         cl.ChieuDaiChuanM
                                  FROM vanhanh.ChiTietDoanTau c
                                  JOIN vanhanh.DoanTau dt ON dt.MaDoanTau = c.MaDoanTau
                                  JOIN phuongtien.ToaXe tx ON tx.MaToaXe = c.MaToaXe
                                  JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
                                  WHERE dt.MaChuyenTau = @maChuyenTau
                                  ORDER BY c.ThuTuNoi ASC";

            var dt = DatabaseHelper.ExecuteQuery(sqlDoanTau, new[] { new SqlParameter("@maChuyenTau", maChuyenTau) });
            if (dt.Rows.Count > 0)
            {
                return dt;
            }

            // Ưu tiên 2: Fallback sang bảng thương mại vé vantai.ToaXeKhach (nếu có dữ liệu)
            string sqlVe = @"SELECT tx.MaToaXeKhach, tx.MaChuyenTau, tx.NhanHieuToa, tx.LoaiToa, tx.ThuTuToa, tx.SucChua,
                                   ISNULL(cl.TenMoTa, tx.LoaiToa) AS TenLoaiToa,
                                   ISNULL((SELECT COUNT(*) FROM vantai.ChoNgoi cn WHERE cn.MaToaXeKhach = tx.MaToaXeKhach), 0) AS SoChoDaThietLap
                            FROM vantai.ToaXeKhach tx
                            LEFT JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoaiCode = tx.LoaiToa
                            WHERE tx.MaChuyenTau = @maChuyenTau
                            ORDER BY tx.ThuTuToa ASC";
            return DatabaseHelper.ExecuteQuery(sqlVe, new[] { new SqlParameter("@maChuyenTau", maChuyenTau) });
        }

        public DataTable LayDanhSachToaXeTheoDauMay(int maDauMay)
        {
            if (maDauMay <= 0) return new DataTable();

            string sql = @"SELECT TOP 20 c.ThuTuNoi AS ThuTuToa,
                                  tx.MaToaXe AS MaToaXeKhach,
                                  dt.MaChuyenTau,
                                  tx.SoHieuToaXe AS NhanHieuToa,
                                  cl.MaChungLoaiCode AS LoaiToa,
                                  cl.TenMoTa AS TenLoaiToa,
                                  ISNULL(cl.SucChuaChuan, 0) AS SucChua,
                                  tx.TuTrongTan,
                                  tx.TaiTrongToiDaTan,
                                  cl.ChieuDaiChuanM
                           FROM vanhanh.ChiTietDoanTau c
                           JOIN vanhanh.DoanTau dt ON dt.MaDoanTau = c.MaDoanTau
                           JOIN phuongtien.ToaXe tx ON tx.MaToaXe = c.MaToaXe
                           JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
                           WHERE dt.MaDauMayChinh = @dm
                           ORDER BY dt.MaDoanTau DESC, c.ThuTuNoi ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@dm", maDauMay) });
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

        public int LapChuyenTauTronGoi(ChuyenTau ct, DataTable dtLichDung, DoanTau? dt, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            using var conn = new SqlConnection(DatabaseHelper.GetConnectionString());
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Chèn Chuyến Tàu
                string sqlTrip = @"INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, TrangThai, SoPhutTreLuyKe, LyDoHuy)
                                   VALUES (@mac, @ngay, @gioDi, @gioDen, @tt, @tre, @lyDo);
                                   SELECT CAST(SCOPE_IDENTITY() AS INT);";
                using var cmdTrip = new SqlCommand(sqlTrip, conn, trans);
                cmdTrip.Parameters.AddWithValue("@mac", ct.MaMacTau);
                cmdTrip.Parameters.AddWithValue("@ngay", ct.NgayXuatPhat.Date);
                cmdTrip.Parameters.AddWithValue("@gioDi", ct.GioXuatPhatKH);
                cmdTrip.Parameters.AddWithValue("@gioDen", ct.GioVeDichKH);
                cmdTrip.Parameters.AddWithValue("@tt", string.IsNullOrEmpty(ct.TrangThai) ? "DA_LEN_LICH" : ct.TrangThai);
                cmdTrip.Parameters.AddWithValue("@tre", ct.SoPhutTreLuyKe);
                cmdTrip.Parameters.AddWithValue("@lyDo", (object?)ct.LyDoHuy ?? DBNull.Value);

                object? resId = cmdTrip.ExecuteScalar();
                int newMaChuyen = resId != null && resId != DBNull.Value ? Convert.ToInt32(resId) : 0;
                if (newMaChuyen <= 0)
                {
                    trans.Rollback();
                    thongBaoLoi = "Không thể tạo bản ghi chuyến tàu.";
                    return 0;
                }

                // 2. Chèn Lịch Dừng Ga (nếu có các điểm dừng trên RAM)
                if (dtLichDung != null && dtLichDung.Rows.Count > 0)
                {
                    string sqlStop = @"INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, GioDenThucTe, GioDiThucTe, MaDuongRay, LaDiemTranh)
                                       VALUES (@ct, @ga, @tt, @denKH, @diKH, @denTT, @diTT, @ray, @tranh)";
                    int stt = 1;
                    foreach (DataRow r in dtLichDung.Rows)
                    {
                        using var cmdStop = new SqlCommand(sqlStop, conn, trans);
                        cmdStop.Parameters.AddWithValue("@ct", newMaChuyen);
                        cmdStop.Parameters.AddWithValue("@ga", Convert.ToInt32(r["MaGa"]));
                        
                        int thuTu = r.Table.Columns.Contains("ThuTuDung") && r["ThuTuDung"] != DBNull.Value 
                            ? Convert.ToInt32(r["ThuTuDung"]) 
                            : stt;
                        cmdStop.Parameters.AddWithValue("@tt", thuTu);

                        cmdStop.Parameters.AddWithValue("@denKH", Convert.ToDateTime(r["GioDenKeHoach"]));
                        cmdStop.Parameters.AddWithValue("@diKH", Convert.ToDateTime(r["GioDiKeHoach"]));
                        cmdStop.Parameters.AddWithValue("@denTT", (r.Table.Columns.Contains("GioDenThucTe") && r["GioDenThucTe"] != DBNull.Value) ? r["GioDenThucTe"] : DBNull.Value);
                        cmdStop.Parameters.AddWithValue("@diTT", (r.Table.Columns.Contains("GioDiThucTe") && r["GioDiThucTe"] != DBNull.Value) ? r["GioDiThucTe"] : DBNull.Value);
                        cmdStop.Parameters.AddWithValue("@ray", (r.Table.Columns.Contains("MaDuongRay") && r["MaDuongRay"] != DBNull.Value) ? r["MaDuongRay"] : DBNull.Value);
                        
                        bool laTranh = r.Table.Columns.Contains("LaDiemTranh") && r["LaDiemTranh"] != DBNull.Value && Convert.ToBoolean(r["LaDiemTranh"]);
                        cmdStop.Parameters.AddWithValue("@tranh", laTranh);

                        cmdStop.ExecuteNonQuery();
                        stt++;
                    }
                }

                // 3. Chèn Đoàn Tàu & Đầu Máy (nếu có)
                if (dt != null && dt.MaDauMayChinh > 0)
                {
                    string sqlDoanTau = @"INSERT INTO vanhanh.DoanTau (MaChuyenTau, MaDauMayChinh, MaDauMayDay, TongSoToa, TongChieuDaiM, TongTrongLuongTan, DaDuyetAnToan)
                                          VALUES (@ct, @chinh, @day, @soToa, @dai, @trongLuong, @duyet)";
                    using var cmdDT = new SqlCommand(sqlDoanTau, conn, trans);
                    cmdDT.Parameters.AddWithValue("@ct", newMaChuyen);
                    cmdDT.Parameters.AddWithValue("@chinh", dt.MaDauMayChinh);
                    cmdDT.Parameters.AddWithValue("@day", (object?)dt.MaDauMayDay ?? DBNull.Value);
                    cmdDT.Parameters.AddWithValue("@soToa", dt.TongSoToa > 0 ? dt.TongSoToa : 1);
                    cmdDT.Parameters.AddWithValue("@dai", dt.TongChieuDaiM > 0 ? dt.TongChieuDaiM : 216.5m);
                    cmdDT.Parameters.AddWithValue("@trongLuong", dt.TongTrongLuongTan > 0 ? dt.TongTrongLuongTan : 478.0m);
                    cmdDT.Parameters.AddWithValue("@duyet", dt.DaDuyetAnToan);

                    cmdDT.ExecuteNonQuery();
                }

                trans.Commit();
                return newMaChuyen;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                thongBaoLoi = $"Lỗi giao dịch lập chuyến tàu: {ex.Message}";
                return 0;
            }
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

        public int CapNhatThongSoKeHoach(int maChuyenTau, int maMacTau, DateTime ngayXuatPhat, DateTime gioXuatPhatKH, DateTime gioVeDichKH)
        {
            string sql = @"UPDATE vanhanh.ChuyenTau 
                           SET MaMacTau = @mac,
                               NgayXuatPhat = @ngay,
                               GioXuatPhatKH = @gioDi,
                               GioVeDichKH = @gioDen
                           WHERE MaChuyenTau = @id";
            var p = new[]
            {
                new SqlParameter("@id", maChuyenTau),
                new SqlParameter("@mac", maMacTau),
                new SqlParameter("@ngay", ngayXuatPhat.Date),
                new SqlParameter("@gioDi", gioXuatPhatKH),
                new SqlParameter("@gioDen", gioVeDichKH)
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

        public int CapNhatLichDungGa(LichDungGa ld)
        {
            string sql = @"UPDATE vanhanh.LichDungGa 
                           SET GioDenKeHoach = @denKH,
                               GioDiKeHoach = @diKH,
                               LaDiemTranh = @tranh
                           WHERE MaDiemDung = @id";
            var p = new[]
            {
                new SqlParameter("@id", ld.MaDiemDung),
                new SqlParameter("@denKH", ld.GioDenKeHoach),
                new SqlParameter("@diKH", ld.GioDiKeHoach),
                new SqlParameter("@tranh", ld.LaDiemTranh)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        public bool DayThuTuDung(int maChuyenTau, int tuThuTu)
        {
            string sql = @"UPDATE vanhanh.LichDungGa 
                           SET ThuTuDung = ThuTuDung + 10000 + 1 
                           WHERE MaChuyenTau = @ct AND ThuTuDung >= @tu;
                           
                           UPDATE vanhanh.LichDungGa 
                           SET ThuTuDung = ThuTuDung - 10000 
                           WHERE MaChuyenTau = @ct AND ThuTuDung > 10000;";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@ct", maChuyenTau),
                new SqlParameter("@tu", tuThuTu)
            }) > 0;
        }

        public bool SapXepLichDungTheoKm(int maChuyenTau, bool chieuBacNam, out int soDiemDungThayDoi)
        {
            soDiemDungThayDoi = 0;
            string sqlGet = @"SELECT ld.MaDiemDung, ld.ThuTuDung, ISNULL(g.LyTrinhKm, 0) AS LyTrinhKm,
                                     ld.GioDenKeHoach, ld.GioDiKeHoach, g.TenGa
                              FROM vanhanh.LichDungGa ld
                              JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                              WHERE ld.MaChuyenTau = @ct";
            var dt = DatabaseHelper.ExecuteQuery(sqlGet, new[] { new SqlParameter("@ct", maChuyenTau) });
            if (dt.Rows.Count <= 1) return true;

            var rows = dt.AsEnumerable();
            var sortedRows = (chieuBacNam
                ? rows.OrderBy(r => Convert.ToDecimal(r["LyTrinhKm"]))
                : rows.OrderByDescending(r => Convert.ToDecimal(r["LyTrinhKm"]))).ToList();

            var listNodes = new List<(int MaDiemDung, int NewThuTu, decimal Km, DateTime GioDen, DateTime GioDi)>();

            for (int i = 0; i < sortedRows.Count; i++)
            {
                var r = sortedRows[i];
                int mdd = Convert.ToInt32(r["MaDiemDung"]);
                decimal km = Convert.ToDecimal(r["LyTrinhKm"]);
                DateTime dtDen = Convert.ToDateTime(r["GioDenKeHoach"]);
                DateTime dtDi = Convert.ToDateTime(r["GioDiKeHoach"]);
                listNodes.Add((mdd, i + 1, km, dtDen, dtDi));
            }

            // Tự động nội suy thời gian cho các ga bị ngược giờ sau khi đảo vị trí theo Km
            for (int i = 0; i < listNodes.Count; i++)
            {
                var cur = listNodes[i];
                bool thoiGianKhongHopLe = false;

                if (i > 0 && cur.GioDen <= listNodes[i - 1].GioDi)
                {
                    thoiGianKhongHopLe = true;
                }
                if (i < listNodes.Count - 1 && cur.GioDi >= listNodes[i + 1].GioDen)
                {
                    thoiGianKhongHopLe = true;
                }

                if (thoiGianKhongHopLe)
                {
                    if (i > 0 && i < listNodes.Count - 1)
                    {
                        var prev = listNodes[i - 1];
                        int nextIdx = i + 1;
                        while (nextIdx < listNodes.Count && listNodes[nextIdx].GioDen <= prev.GioDi)
                        {
                            nextIdx++;
                        }

                        if (nextIdx < listNodes.Count)
                        {
                            var nextValid = listNodes[nextIdx];
                            decimal distTotal = Math.Abs(nextValid.Km - prev.Km);
                            decimal distCur = Math.Abs(cur.Km - prev.Km);

                            TimeSpan totalSpan = nextValid.GioDen - prev.GioDi;
                            double ratio = distTotal > 0 ? (double)(distCur / distTotal) : 0.5;
                            double minutesChay = Math.Round(totalSpan.TotalMinutes * ratio);
                            if (minutesChay < 5) minutesChay = 5;

                            DateTime rawDen = prev.GioDi.AddMinutes(minutesChay);
                            DateTime newDen = new DateTime(rawDen.Year, rawDen.Month, rawDen.Day, rawDen.Hour, rawDen.Minute, 0);

                            int dwellMinutes = (int)Math.Max(2, Math.Round((cur.GioDi - cur.GioDen).TotalMinutes));
                            if (dwellMinutes > 30) dwellMinutes = 5;

                            DateTime newDi = newDen.AddMinutes(dwellMinutes);
                            if (newDi >= nextValid.GioDen)
                            {
                                newDi = nextValid.GioDen.AddMinutes(-5);
                                newDen = newDi.AddMinutes(-dwellMinutes);
                            }

                            listNodes[i] = (cur.MaDiemDung, cur.NewThuTu, cur.Km, newDen, newDi);
                        }
                    }
                    else if (i == listNodes.Count - 1 && i > 0)
                    {
                        // Ga cuối cùng bị trễ/ngược: dự kiến vận tốc trung bình ~55 km/h
                        var prev = listNodes[i - 1];
                        decimal dist = Math.Abs(cur.Km - prev.Km);
                        int runMins = (int)Math.Max(10, Math.Round((double)dist * 1.1));
                        DateTime rawDen = prev.GioDi.AddMinutes(runMins);
                        DateTime newDen = new DateTime(rawDen.Year, rawDen.Month, rawDen.Day, rawDen.Hour, rawDen.Minute, 0);
                        listNodes[i] = (cur.MaDiemDung, cur.NewThuTu, cur.Km, newDen, newDen);
                    }
                }
            }

            for (int i = 0; i < sortedRows.Count; i++)
            {
                int currentThuTu = Convert.ToInt32(sortedRows[i]["ThuTuDung"]);
                if (currentThuTu != (i + 1) || listNodes[i].GioDen != Convert.ToDateTime(sortedRows[i]["GioDenKeHoach"]))
                {
                    soDiemDungThayDoi++;
                }
            }

            if (soDiemDungThayDoi == 0) return true;

            using var conn = new SqlConnection(DatabaseHelper.GetConnectionString());
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                // Vô hiệu hóa trigger kiểm tra thời gian trong lúc cập nhật hàng loạt
                using (var cmdDis = new SqlCommand("ALTER TABLE vanhanh.LichDungGa DISABLE TRIGGER trg_LichDungGa_CheckThoiGian;", conn, trans))
                {
                    cmdDis.ExecuteNonQuery();
                }

                // B1: Đánh số tạm thời với offset 10000 để tránh vi phạm CHECK constraint hoặc xung đột UNIQUE
                for (int i = 0; i < listNodes.Count; i++)
                {
                    var node = listNodes[i];
                    string sqlUp = @"UPDATE vanhanh.LichDungGa 
                                     SET ThuTuDung = @tt, GioDenKeHoach = @den, GioDiKeHoach = @di 
                                     WHERE MaDiemDung = @mdd";
                    using var cmd = new SqlCommand(sqlUp, conn, trans);
                    cmd.Parameters.AddWithValue("@tt", node.NewThuTu + 10000);
                    cmd.Parameters.AddWithValue("@den", node.GioDen);
                    cmd.Parameters.AddWithValue("@di", node.GioDi);
                    cmd.Parameters.AddWithValue("@mdd", node.MaDiemDung);
                    cmd.ExecuteNonQuery();
                }

                // B2: Giảm offset 10000 về đúng dải 1..N
                string sqlFinal = "UPDATE vanhanh.LichDungGa SET ThuTuDung = ThuTuDung - 10000 WHERE MaChuyenTau = @ct AND ThuTuDung > 10000";
                using (var cmdFinal = new SqlCommand(sqlFinal, conn, trans))
                {
                    cmdFinal.Parameters.AddWithValue("@ct", maChuyenTau);
                    cmdFinal.ExecuteNonQuery();
                }

                // B3: Cập nhật lại GioXuatPhatKH và GioVeDichKH của chuyến tàu
                if (listNodes.Count >= 2)
                {
                    string sqlTrip = @"UPDATE vanhanh.ChuyenTau 
                                       SET GioXuatPhatKH = @xp, GioVeDichKH = @vd 
                                       WHERE MaChuyenTau = @ct";
                    using var cmdTrip = new SqlCommand(sqlTrip, conn, trans);
                    cmdTrip.Parameters.AddWithValue("@xp", listNodes[0].GioDi);
                    cmdTrip.Parameters.AddWithValue("@vd", listNodes[^1].GioDen);
                    cmdTrip.Parameters.AddWithValue("@ct", maChuyenTau);
                    cmdTrip.ExecuteNonQuery();
                }

                // Kích hoạt lại trigger sau khi hoàn tất sắp xếp chuẩn
                using (var cmdEn = new SqlCommand("ALTER TABLE vanhanh.LichDungGa ENABLE TRIGGER trg_LichDungGa_CheckThoiGian;", conn, trans))
                {
                    cmdEn.ExecuteNonQuery();
                }

                trans.Commit();
                return true;
            }
            catch
            {
                trans.Rollback();
                try
                {
                    using var connEn = new SqlConnection(DatabaseHelper.GetConnectionString());
                    connEn.Open();
                    using var cmdEn = new SqlCommand("ALTER TABLE vanhanh.LichDungGa ENABLE TRIGGER trg_LichDungGa_CheckThoiGian;", connEn);
                    cmdEn.ExecuteNonQuery();
                }
                catch { }
                throw;
            }
        }

        public bool TinhTienGioCacGaPhiaSau(int maChuyenTau, int tuThuTu, int soPhut)
        {
            string sql = @"UPDATE vanhanh.LichDungGa
                           SET GioDenKeHoach = DATEADD(MINUTE, @phut, GioDenKeHoach),
                               GioDiKeHoach = DATEADD(MINUTE, @phut, GioDiKeHoach)
                           WHERE MaChuyenTau = @ct AND ThuTuDung >= @tu;

                           UPDATE vanhanh.ChuyenTau
                           SET GioVeDichKH = (SELECT MAX(GioDenKeHoach) FROM vanhanh.LichDungGa WHERE MaChuyenTau = @ct)
                           WHERE MaChuyenTau = @ct;";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@ct", maChuyenTau),
                new SqlParameter("@tu", tuThuTu),
                new SqlParameter("@phut", soPhut)
            }) > 0;
        }

        public int SaoChepLichDungTuChuyenKhac(int maChuyenNguon, int maChuyenDich, DateTime ngayKhoiHanhMoi)
        {
            // Bọc DELETE và INSERT trong cùng một transaction nguyên tử (atomic)
            string sql = @"SET XACT_ABORT ON;
                           BEGIN TRANSACTION;

                           DELETE FROM vanhanh.LichDungGa WHERE MaChuyenTau = @dich;

                           INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, LaDiemTranh)
                           SELECT @dich, ld.MaGa, ld.ThuTuDung,
                                  DATEADD(MINUTE, DATEDIFF(MINUTE, ctNguon.GioXuatPhatKH, ctDich.GioXuatPhatKH), ld.GioDenKeHoach),
                                  DATEADD(MINUTE, DATEDIFF(MINUTE, ctNguon.GioXuatPhatKH, ctDich.GioXuatPhatKH), ld.GioDiKeHoach),
                                  ld.LaDiemTranh
                           FROM vanhanh.LichDungGa ld
                           JOIN vanhanh.ChuyenTau ctNguon ON ld.MaChuyenTau = ctNguon.MaChuyenTau
                           CROSS JOIN (SELECT GioXuatPhatKH FROM vanhanh.ChuyenTau WHERE MaChuyenTau = @dich) ctDich
                           WHERE ld.MaChuyenTau = @nguon
                           ORDER BY ld.ThuTuDung;

                           COMMIT TRANSACTION;";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@nguon", maChuyenNguon),
                new SqlParameter("@dich", maChuyenDich)
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

        // PHÂN HỆ QUẢN LÝ ĐOÀN TÀU & RÀNG BUỘC CHỐNG TRANH CHẤP ĐẦU MÁY (DoanTau, DauMay)

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
            string sql = @"SELECT TOP 1 g.TenGa, ISNULL(MAX(dr.ChieuDaiHuuDungM), 0) AS MaxDuongTranh
                           FROM vanhanh.LichDungGa ld
                           JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                           LEFT JOIN hatang.DuongRayGa dr ON ld.MaGa = dr.MaGa AND dr.LoaiDuong = 'DUONG_TRANH'
                           WHERE ld.MaChuyenTau = @ct AND ld.LaDiemTranh = 1
                           GROUP BY g.TenGa, g.LyTrinhKm
                           HAVING ISNULL(MAX(dr.ChieuDaiHuuDungM), 0) < @chieuDai
                           ORDER BY MaxDuongTranh ASC";
            DataTable dt = DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@ct", maChuyenTau),
                new SqlParameter("@chieuDai", tongChieuDaiM)
            });
            if (dt.Rows.Count > 0)
            {
                string tenGa = dt.Rows[0]["TenGa"].ToString() ?? "";
                int maxM = Convert.ToInt32(dt.Rows[0]["MaxDuongTranh"]);
                if (maxM <= 0)
                {
                    canhBao = $"Cảnh báo an toàn kỹ thuật: Ga {tenGa} được chỉ định là Điểm Tránh Tàu nhưng không có đường tránh quy chuẩn trong cơ sở dữ liệu!";
                }
                else
                {
                    canhBao = $"Cảnh báo an toàn: Chiều dài đoàn tàu ({tongChieuDaiM:F1}m) vượt quá chiều dài đường tránh lớn nhất tại Ga {tenGa} ({maxM}m)! Ga này không đủ điều kiện đón tránh tàu.";
                }
                return false;
            }
            return true;
        }

        // PHÂN HỆ QUẢN LÝ BIÊN CHẾ TOA XE KHÁCH (ToaXeKhach, ChoNgoi)
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
            if (sucChua <= 0) return;

            var sb = new System.Text.StringBuilder();
            sb.Append("INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu) VALUES ");
            var pList = new List<SqlParameter> { new("@toa", maToa) };

            for (int i = 1; i <= sucChua; i++)
            {
                int? tangGiuong = loaiToa switch
                {
                    "AN" => ((i - 1) % 2) + 1,
                    "BN" => ((i - 1) % 3) + 1,
                    _ => null
                };

                if (i > 1) sb.Append(", ");
                sb.Append($"(@toa, @soGhe{i}, @tang{i}, 0)");
                pList.Add(new SqlParameter($"@soGhe{i}", i));
                pList.Add(new SqlParameter($"@tang{i}", (object?)tangGiuong ?? DBNull.Value));
            }

            DatabaseHelper.ExecuteNonQuery(sb.ToString(), pList.ToArray());
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

            // Truy vấn thông số thực tế từ phuongtien.DongDauMay và phuongtien.ChungLoaiToa
            string calcSql = @"
                SELECT 
                    ISNULL(ddmChinh.ChieuDaiM, 16.5) + ISNULL(ddmDay.ChieuDaiM, 0.0) AS ChieuDaiDauMay,
                    ISNULL(ddmChinh.TrongLuongTan, 78.0) + ISNULL(ddmDay.TrongLuongTan, 0.0) AS TrongLuongDauMay,
                    ISNULL(txInfo.SoToa, 0) AS SoToa,
                    ISNULL(txInfo.TongChieuDaiToa, 0.0) AS TongChieuDaiToa,
                    ISNULL(txInfo.TongTrongLuongToa, 0.0) AS TongTrongLuongToa
                FROM vanhanh.DoanTau dt
                LEFT JOIN phuongtien.DauMay dmChinh ON dt.MaDauMayChinh = dmChinh.MaDauMay
                LEFT JOIN phuongtien.DongDauMay ddmChinh ON dmChinh.MaDongDauMay = ddmChinh.MaDongDauMay
                LEFT JOIN phuongtien.DauMay dmDay ON dt.MaDauMayDay = dmDay.MaDauMay
                LEFT JOIN phuongtien.DongDauMay ddmDay ON dmDay.MaDongDauMay = ddmDay.MaDongDauMay
                OUTER APPLY (
                    SELECT 
                        COUNT(tx.MaToaXeKhach) AS SoToa,
                        ISNULL(SUM(cl.ChieuDaiChuanM), COUNT(tx.MaToaXeKhach) * 20.0) AS TongChieuDaiToa,
                        ISNULL(SUM(CASE tx.LoaiToa 
                            WHEN 'NC' THEN 38.0 
                            WHEN 'NML' THEN 40.0 
                            WHEN 'BN' THEN 42.0 
                            WHEN 'AN' THEN 44.0 
                            ELSE 40.0 END), COUNT(tx.MaToaXeKhach) * 40.0) AS TongTrongLuongToa
                    FROM vantai.ToaXeKhach tx
                    LEFT JOIN phuongtien.ChungLoaiToa cl ON tx.LoaiToa = cl.MaChungLoaiCode
                    WHERE tx.MaChuyenTau = dt.MaChuyenTau
                ) txInfo
                WHERE dt.MaChuyenTau = @ct";

            DataTable dt = DatabaseHelper.ExecuteQuery(calcSql, new[] { new SqlParameter("@ct", maChuyenTau) });
            if (dt.Rows.Count == 0) return;

            DataRow r = dt.Rows[0];
            int soToa = Convert.ToInt32(r["SoToa"]);
            if (soToa == 0) soToa = 1;

            decimal chieuDai = Convert.ToDecimal(r["ChieuDaiDauMay"]) + Convert.ToDecimal(r["TongChieuDaiToa"]);
            decimal trongLuong = Convert.ToDecimal(r["TrongLuongDauMay"]) + Convert.ToDecimal(r["TongTrongLuongToa"]);

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

        public bool KiemTraTrungLich(int maMacTau, DateTime ngayXuatPhat, int excludeMaChuyen = 0)
        {
            string sql = @"SELECT COUNT(*) FROM vanhanh.ChuyenTau 
                           WHERE MaMacTau = @mac AND NgayXuatPhat = @ngay
                           AND TrangThai <> 'DA_HUY'
                           AND (@exclude <= 0 OR MaChuyenTau <> @exclude)";
            object? res = DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@mac", maMacTau),
                new SqlParameter("@ngay", ngayXuatPhat.Date),
                new SqlParameter("@exclude", excludeMaChuyen)
            });
            return res != null && Convert.ToInt32(res) > 0;
        }

        public string LayTrangThai(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return string.Empty;
            string sql = "SELECT TrangThai FROM vanhanh.ChuyenTau WHERE MaChuyenTau = @id";
            object? res = DatabaseHelper.ExecuteScalar(sql, new[] { new SqlParameter("@id", maChuyenTau) });
            return res?.ToString() ?? string.Empty;
        }

        public bool Xoa(int maChuyenTau, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;

            string checkTrangThaiSql = @"SELECT TrangThai FROM vanhanh.ChuyenTau WHERE MaChuyenTau = @id";
            object? ttObj = DatabaseHelper.ExecuteScalar(checkTrangThaiSql, new[] { new SqlParameter("@id", maChuyenTau) });
            string tt = ttObj?.ToString() ?? "";
            if (tt == "DANG_CHAY" || tt == "TRE_GIO" || tt == "HOAN_THANH")
            {
                thongBaoLoi = $"Chuyến tàu đang vận hành hoặc đã hoàn thành ({tt}), không được phép xóa khỏi cơ sở dữ liệu! Vui lòng chuyển sang [Đã Hủy] nếu muốn ngừng phục vụ.";
                return false;
            }

            string checkVeSql = @"SELECT COUNT(*) 
                                  FROM vantai.Ve 
                                  WHERE MaChuyenTau = @id";
            object? veCount = DatabaseHelper.ExecuteScalar(checkVeSql, new[] { new SqlParameter("@id", maChuyenTau) });
            if (veCount != null && Convert.ToInt32(veCount) > 0)
            {
                thongBaoLoi = $"Không thể xóa vật lý chuyến tàu này vì đã có {Convert.ToInt32(veCount)} bản ghi vé được phát hành (chứng từ kế toán)! Vui lòng chuyển trạng thái chuyến tàu sang [Đã Hủy].";
                return false;
            }

            try
            {
                string sql = @"SET XACT_ABORT ON;
                               BEGIN TRANSACTION;

                               DELETE FROM baotri.KhamXeKyThuat WHERE MaDoanTau IN (SELECT MaDoanTau FROM vanhanh.DoanTau WHERE MaChuyenTau = @id);
                               DELETE FROM vanhanh.ChiTietDoanTau WHERE MaDoanTau IN (SELECT MaDoanTau FROM vanhanh.DoanTau WHERE MaChuyenTau = @id);
                               DELETE FROM vanhanh.DoanTau WHERE MaChuyenTau = @id;
                               DELETE FROM vanhanh.NhatKyChamGio WHERE MaChuyenTau = @id;
                               DELETE FROM nhansu.PhanCongKipLai WHERE MaChuyenTau = @id;
                               DELETE FROM nhansu.KiemTraSucKhoe WHERE MaChuyenTau = @id;
                               DELETE FROM vantai.ChoNgoi WHERE MaToaXeKhach IN (SELECT MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @id);
                               DELETE FROM vantai.ToaXeKhach WHERE MaChuyenTau = @id;
                               DELETE FROM vanhanh.LichDungGa WHERE MaChuyenTau = @id;
                               DELETE FROM vanhanh.ChuyenTau WHERE MaChuyenTau = @id;

                               COMMIT TRANSACTION;";
                int affected = DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@id", maChuyenTau) });
                return affected > 0;
            }
            catch (Exception ex)
            {
                thongBaoLoi = $"Lỗi khi xóa chuyến tàu: {ex.Message}";
                return false;
            }
        }

        public int Xoa(int maChuyenTau)
        {
            return Xoa(maChuyenTau, out _) ? 1 : 0;
        }
    }
}

