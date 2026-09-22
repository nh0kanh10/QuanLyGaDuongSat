using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using ET.VanTai;

namespace DAL.Repositories
{
    public class VeRepository
    {
        public DataTable LayDanhSach()
        {
            string sql = @"SELECT v.MaVe, v.MaVeCode, mt.SoHieuMacTau, 
                                  dv.MaPNR,
                                  g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen,
                                  v.TenHanhKhach, v.CCCDHanhKhach, cn.SoGhe, 
                                  tx.LoaiToa, tx.NhanHieuToa,
                                  v.GiaVeGoc, v.SoTienGiam, v.GiaVeThucThu,
                                  v.TrangThai, v.ThoiDiemXuatVe,
                                  ct.NgayXuatPhat, ct.GioXuatPhatKH
                           FROM vantai.Ve v
                           JOIN vantai.DonDatVe dv ON v.MaDonVe = dv.MaDonVe
                           JOIN vanhanh.ChuyenTau ct ON v.MaChuyenTau = ct.MaChuyenTau
                           JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                           JOIN hatang.Ga g1 ON v.MaGaDi = g1.MaGa
                           JOIN hatang.Ga g2 ON v.MaGaDen = g2.MaGa
                           JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
                           JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                           ORDER BY v.ThoiDiemXuatVe DESC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable TimKiemVe(string keyword)
        {
            string sql = @"SELECT v.MaVe, v.MaVeCode, mt.SoHieuMacTau, 
                                  dv.MaPNR,
                                  g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen,
                                  v.TenHanhKhach, v.CCCDHanhKhach, cn.SoGhe, 
                                  tx.LoaiToa, tx.NhanHieuToa,
                                  v.GiaVeGoc, v.SoTienGiam, v.GiaVeThucThu,
                                  v.TrangThai, v.ThoiDiemXuatVe,
                                  ct.NgayXuatPhat, ct.GioXuatPhatKH
                           FROM vantai.Ve v
                           JOIN vantai.DonDatVe dv ON v.MaDonVe = dv.MaDonVe
                           JOIN vanhanh.ChuyenTau ct ON v.MaChuyenTau = ct.MaChuyenTau
                           JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                           JOIN hatang.Ga g1 ON v.MaGaDi = g1.MaGa
                           JOIN hatang.Ga g2 ON v.MaGaDen = g2.MaGa
                           JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
                           JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                           WHERE v.MaVeCode LIKE @kw 
                              OR dv.MaPNR LIKE @kw 
                              OR v.CCCDHanhKhach LIKE @kw 
                              OR v.TenHanhKhach LIKE @kw 
                              OR mt.SoHieuMacTau LIKE @kw
                           ORDER BY v.ThoiDiemXuatVe DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@kw", $"%{keyword}%") });
        }

        public DataTable TimChuyenTauTheoChang(int maGaDi, int maGaDen, DateTime ngayDi)
        {
            string sql = @"
                SELECT ct.MaChuyenTau, ct.MaMacTau, mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay,
                       ct.NgayXuatPhat, ct.GioXuatPhatKH, ct.GioVeDichKH, ct.TrangThai,
                       gDi.TenGa AS TenGaXuatPhat, gDen.TenGa AS TenGaKetThuc,
                       (SELECT COUNT(*) FROM vantai.ToaXeKhach tx WHERE tx.MaChuyenTau = ct.MaChuyenTau) AS SoToaXe,
                       (SELECT COUNT(*) FROM vantai.ChoNgoi cn 
                        JOIN vantai.ToaXeKhach tx2 ON cn.MaToaXeKhach = tx2.MaToaXeKhach 
                        WHERE tx2.MaChuyenTau = ct.MaChuyenTau) AS TongSoCho,
                       (SELECT COUNT(*) FROM vantai.Ve v 
                        WHERE v.MaChuyenTau = ct.MaChuyenTau 
                          AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')) AS SoChoDaBan,
                       ISNULL(vantai.fn_TinhGiaVe('NML', @gaDi, @gaDen, NULL), 150000) AS GiaVeCoSo
                FROM vanhanh.ChuyenTau ct
                JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                JOIN hatang.Ga gDi ON mt.MaGaDi = gDi.MaGa
                JOIN hatang.Ga gDen ON mt.MaGaDen = gDen.MaGa
                WHERE ct.TrangThai IN ('KE_HOACH', 'DANG_CHAY', 'CHUA_CHAY')
                  AND (
                      -- Logic xac dinh huong chay phu hop voi chieu di cua khach
                      (SELECT LyTrinhKm FROM hatang.Ga WHERE MaGa = @gaDi) < (SELECT LyTrinhKm FROM hatang.Ga WHERE MaGa = @gaDen)
                      AND mt.HuongChay = 'BAC_NAM'
                      OR
                      (SELECT LyTrinhKm FROM hatang.Ga WHERE MaGa = @gaDi) > (SELECT LyTrinhKm FROM hatang.Ga WHERE MaGa = @gaDen)
                      AND mt.HuongChay = 'NAM_BAC'
                  )
                ORDER BY ct.GioXuatPhatKH ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@gaDi", maGaDi),
                new SqlParameter("@gaDen", maGaDen)
            });
        }

        public DataTable LayDanhSachToaTheoChuyen(int maChuyenTau, int maGaDi, int maGaDen)
        {
            string sql = @"
                SELECT tx.MaToaXeKhach, tx.MaChuyenTau, tx.NhanHieuToa, tx.LoaiToa, tx.ThuTuToa, tx.SucChua,
                       CASE tx.LoaiToa
                           WHEN 'NC' THEN N'Ngồi Cứng'
                           WHEN 'NML' THEN N'Ngồi Mềm Lạnh'
                           WHEN 'BN' THEN N'Giường Nằm K6'
                           WHEN 'AN' THEN N'Giường Nằm K4 VIP'
                           ELSE tx.LoaiToa
                       END AS TenLoaiToa,
                       COUNT(cn.MaChoNgoi) AS TongSoCho,
                       COUNT(cn.MaChoNgoi) - ISNULL(SUM(CASE WHEN v.MaVe IS NOT NULL THEN 1 ELSE 0 END), 0) AS SoChoTrong
                FROM vantai.ToaXeKhach tx
                LEFT JOIN vantai.ChoNgoi cn ON tx.MaToaXeKhach = cn.MaToaXeKhach
                LEFT JOIN vantai.Ve v ON cn.MaChoNgoi = v.MaChoNgoi 
                                     AND v.MaChuyenTau = tx.MaChuyenTau 
                                     AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
                WHERE tx.MaChuyenTau = @chuyenTau
                GROUP BY tx.MaToaXeKhach, tx.MaChuyenTau, tx.NhanHieuToa, tx.LoaiToa, tx.ThuTuToa, tx.SucChua
                ORDER BY tx.ThuTuToa ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@chuyenTau", maChuyenTau) });
        }

        public DataTable LaySoDoGhe(int maToaXeKhach, int maChuyenTau, int maGaDi, int maGaDen)
        {
            string sql = @"
                SELECT cn.MaChoNgoi, cn.MaToaXeKhach, cn.SoGhe, cn.TangGiuong, cn.LaGhePhu,
                       tx.LoaiToa, tx.NhanHieuToa,
                       CASE 
                           WHEN v.MaVe IS NOT NULL THEN 1 
                           ELSE 0 
                       END AS DaDat,
                       v.TenHanhKhach AS KhachHienTai,
                       v.MaVeCode
                FROM vantai.ChoNgoi cn
                JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                LEFT JOIN vantai.Ve v ON cn.MaChoNgoi = v.MaChoNgoi 
                                     AND v.MaChuyenTau = @chuyenTau 
                                     AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
                WHERE cn.MaToaXeKhach = @toa
                ORDER BY cn.SoGhe ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@toa", maToaXeKhach),
                new SqlParameter("@chuyenTau", maChuyenTau)
            });
        }

        public decimal TinhGiaVe(string loaiToa, int maGaDi, int maGaDen, int? tangGiuong = null)
        {
            string sql = "SELECT vantai.fn_TinhGiaVe(@loaiToa, @gaDi, @gaDen, @tang)";
            var p = new[]
            {
                new SqlParameter("@loaiToa", loaiToa),
                new SqlParameter("@gaDi", maGaDi),
                new SqlParameter("@gaDen", maGaDen),
                new SqlParameter("@tang", (object?)tangGiuong ?? DBNull.Value)
            };
            object? res = DatabaseHelper.ExecuteScalar(sql, p);
            return res != null && res != DBNull.Value ? Convert.ToDecimal(res) : 100000m;
        }

        public DataTable? TimKhachHangTheoCCCD(string cccd)
        {
            string sql = "SELECT TOP 1 * FROM vantai.KhachHang WHERE SoCCCD = @cccd";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@cccd", cccd) });
        }

        public bool LuuDonDatVeTransaction(
            KhachHang khachHang, 
            DonDatVe donVe, 
            List<Ve> danhSachVe, 
            string soHieuTau,
            out string maPNROut, 
            out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            maPNROut = string.Empty;

            string connStr = @"Server=.\SQLEXPRESS;Database=QuanLyDuongSatV2;Trusted_Connection=True;TrustServerCertificate=True;";
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var trans = conn.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                // 1. Kiem tra hoac tao moi KhachHang
                int maKhachHang = 0;
                string checkKhSql = "SELECT MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = @cccd";
                using (var cmdCheckKh = new SqlCommand(checkKhSql, conn, trans))
                {
                    cmdCheckKh.Parameters.AddWithValue("@cccd", khachHang.SoCCCD);
                    object? res = cmdCheckKh.ExecuteScalar();
                    if (res != null && res != DBNull.Value)
                    {
                        maKhachHang = Convert.ToInt32(res);
                    }
                }

                if (maKhachHang == 0)
                {
                    string insertKhSql = @"INSERT INTO vantai.KhachHang (HoTen, SoCCCD, SoDienThoai, Email, LoaiKhach)
                                           VALUES (@ten, @cccd, @sdt, @email, @loai);
                                           SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    using var cmdInsertKh = new SqlCommand(insertKhSql, conn, trans);
                    cmdInsertKh.Parameters.AddWithValue("@ten", khachHang.HoTen);
                    cmdInsertKh.Parameters.AddWithValue("@cccd", khachHang.SoCCCD);
                    cmdInsertKh.Parameters.AddWithValue("@sdt", khachHang.SoDienThoai);
                    cmdInsertKh.Parameters.AddWithValue("@email", (object?)khachHang.Email ?? DBNull.Value);
                    cmdInsertKh.Parameters.AddWithValue("@loai", khachHang.LoaiKhach);
                    maKhachHang = Convert.ToInt32(cmdInsertKh.ExecuteScalar());
                }

                // 2. Tao MaPNR va luu DonDatVe
                string pnrCode = $"PNR{DateTime.Now:yyMMdd}{new Random().Next(1000, 9999)}";
                string insertDonSql = @"INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
                                        VALUES (@pnr, @kh, @soLuong, @tongTien, @httt, 'DA_THANH_TOAN', @nv);
                                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                int maDonVe;
                using (var cmdDon = new SqlCommand(insertDonSql, conn, trans))
                {
                    cmdDon.Parameters.AddWithValue("@pnr", pnrCode);
                    cmdDon.Parameters.AddWithValue("@kh", maKhachHang);
                    cmdDon.Parameters.AddWithValue("@soLuong", danhSachVe.Count);
                    cmdDon.Parameters.AddWithValue("@tongTien", donVe.TongTien);
                    cmdDon.Parameters.AddWithValue("@httt", donVe.HinhThucTT);
                    cmdDon.Parameters.AddWithValue("@nv", donVe.MaNhanVienBan > 0 ? donVe.MaNhanVienBan : 3);
                    maDonVe = Convert.ToInt32(cmdDon.ExecuteScalar());
                }

                // 3. Kiem tra ghe trong va luu tung Ve
                int ticketIndex = 1;
                foreach (var ve in danhSachVe)
                {
                    string checkSeatSql = @"SELECT COUNT(*) FROM vantai.Ve 
                                            WHERE MaChuyenTau = @ct 
                                              AND MaChoNgoi = @cho 
                                              AND TrangThai IN ('DA_DAT', 'DA_LEN_TAU')";
                    using (var cmdCheckSeat = new SqlCommand(checkSeatSql, conn, trans))
                    {
                        cmdCheckSeat.Parameters.AddWithValue("@ct", ve.MaChuyenTau);
                        cmdCheckSeat.Parameters.AddWithValue("@cho", ve.MaChoNgoi);
                        int seatCount = Convert.ToInt32(cmdCheckSeat.ExecuteScalar());
                        if (seatCount > 0)
                        {
                            throw new Exception($"Ghế mã #{ve.MaChoNgoi} vừa được đặt bởi giao dịch khác. Vui lòng chọn ghế khác!");
                        }
                    }

                    string veCode = $"TK_{soHieuTau}_{DateTime.Now:yyMMdd}_{ticketIndex++:D2}{new Random().Next(10, 99)}";
                    ve.MaVeCode = veCode;

                    string insertVeSql = @"INSERT INTO vantai.Ve 
                                           (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen,
                                            TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
                                           VALUES (@donVe, @code, @ct, @cho, @gaDi, @gaDen,
                                                   @ten, @cccd, @goc, @giam, 'DA_DAT');
                                           SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    using (var cmdVe = new SqlCommand(insertVeSql, conn, trans))
                    {
                        cmdVe.Parameters.AddWithValue("@donVe", maDonVe);
                        cmdVe.Parameters.AddWithValue("@code", veCode);
                        cmdVe.Parameters.AddWithValue("@ct", ve.MaChuyenTau);
                        cmdVe.Parameters.AddWithValue("@cho", ve.MaChoNgoi);
                        cmdVe.Parameters.AddWithValue("@gaDi", ve.MaGaDi);
                        cmdVe.Parameters.AddWithValue("@gaDen", ve.MaGaDen);
                        cmdVe.Parameters.AddWithValue("@ten", ve.TenHanhKhach);
                        cmdVe.Parameters.AddWithValue("@cccd", ve.CCCDHanhKhach);
                        cmdVe.Parameters.AddWithValue("@goc", ve.GiaVeGoc);
                        cmdVe.Parameters.AddWithValue("@giam", ve.SoTienGiam);
                        ve.MaVe = Convert.ToInt32(cmdVe.ExecuteScalar());
                    }
                }

                trans.Commit();
                maPNROut = pnrCode;
                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                thongBaoLoi = ex.Message;
                return false;
            }
        }

        public bool HoanVe(int maVe, decimal tyLeLePhi, string lyDo, string hinhThucHoan, int maNhanVien, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            string connStr = @"Server=.\SQLEXPRESS;Database=QuanLyDuongSatV2;Trusted_Connection=True;TrustServerCertificate=True;";
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // Lay thong tin ve goc
                string getVeSql = "SELECT GiaVeGoc, TrangThai FROM vantai.Ve WHERE MaVe = @id";
                decimal giaGoc = 0;
                string trangThai = "";

                using (var cmdGet = new SqlCommand(getVeSql, conn, trans))
                {
                    cmdGet.Parameters.AddWithValue("@id", maVe);
                    using var reader = cmdGet.ExecuteReader();
                    if (reader.Read())
                    {
                        giaGoc = Convert.ToDecimal(reader["GiaVeGoc"]);
                        trangThai = reader["TrangThai"].ToString() ?? "";
                    }
                }

                if (trangThai == "DA_HOAN_VE")
                {
                    thongBaoLoi = "Vé này đã làm thủ tục hoàn trả trước đó!";
                    trans.Rollback();
                    return false;
                }

                // Cap nhat trang thai ve
                string updateVeSql = "UPDATE vantai.Ve SET TrangThai = 'DA_HOAN_VE' WHERE MaVe = @id";
                using (var cmdUp = new SqlCommand(updateVeSql, conn, trans))
                {
                    cmdUp.Parameters.AddWithValue("@id", maVe);
                    cmdUp.ExecuteNonQuery();
                }

                // Ghi nhan vao bang HoanHuyVe
                string insertHhSql = @"INSERT INTO vantai.HoanHuyVe (MaVe, TienVeGoc, TyLeLePhi, HinhThucHoan, LyDoHoan, MaNhanVienDuyet)
                                       VALUES (@maVe, @goc, @tyLe, @ht, @lyDo, @nv)";
                using (var cmdHh = new SqlCommand(insertHhSql, conn, trans))
                {
                    cmdHh.Parameters.AddWithValue("@maVe", maVe);
                    cmdHh.Parameters.AddWithValue("@goc", giaGoc);
                    cmdHh.Parameters.AddWithValue("@tyLe", tyLeLePhi);
                    cmdHh.Parameters.AddWithValue("@ht", hinhThucHoan);
                    cmdHh.Parameters.AddWithValue("@lyDo", lyDo);
                    cmdHh.Parameters.AddWithValue("@nv", maNhanVien > 0 ? maNhanVien : 3);
                    cmdHh.ExecuteNonQuery();
                }

                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                thongBaoLoi = ex.Message;
                return false;
            }
        }
    }
}
