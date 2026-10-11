using System.Data;
using Microsoft.Data.SqlClient;

namespace DAL.Connection
{
    // =========================================================================
    // MOT GIAO DICH CSDL DUNG CHUNG CHO NHIEU REPOSITORY (unit of work)
    //
    //   using var gd = PhienGiaoDich.Mo();
    //   gd.KhoaTaiNguyen("vanhanh.LapTau");          // tuy chon
    //   ... goi cac ham Repository nhan tham so PhienGiaoDich ...
    //   gd.XacNhan();                                 // COMMIT
    //
    // Khong goi XacNhan (return som / nem loi) thi Dispose tu ROLLBACK.
    // BUS chi cam doi tuong nay, khong dung toi SqlConnection / SqlTransaction.
    //
    // Ghi chu: KE_HOACH_BACK_END.md §2.5 du kien DatabaseHelper.ThucHienTrongGiaoDich
    // (viec nen cua A Ba, chua co). Lop nay dung doc lap, khong sua DatabaseHelper.
    // =========================================================================
    public sealed class PhienGiaoDich : IDisposable
    {
        private readonly SqlConnection _ketNoi;
        private readonly SqlTransaction _giaoDich;
        private bool _daKetThuc;

        private PhienGiaoDich(SqlConnection ketNoi, SqlTransaction giaoDich)
        {
            _ketNoi = ketNoi;
            _giaoDich = giaoDich;
        }

        public static PhienGiaoDich Mo(IsolationLevel mucCoLap = IsolationLevel.ReadCommitted)
        {
            var ketNoi = new SqlConnection(DatabaseHelper.GetConnectionString());
            try
            {
                ketNoi.Open();
                return new PhienGiaoDich(ketNoi, ketNoi.BeginTransaction(mucCoLap));
            }
            catch
            {
                ketNoi.Dispose();
                throw;
            }
        }

        // Khoa muc ung dung (sp_getapplock), giu toi het giao dich. Dung de tuan tu hoa cac
        // thao tac phai kiem tra cheo nhieu dong (vd: mot dau may khong keo 2 chuyen chong gio) -
        // khoa dong UPDLOCK khong chan duoc vi dong "xung dot" co the chua ton tai.
        public void KhoaTaiNguyen(string tenTaiNguyen, int choToiDaMs = 10_000)
        {
            using var lenh = TaoLenh("sp_getapplock");
            lenh.CommandType = CommandType.StoredProcedure;
            lenh.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = tenTaiNguyen });
            lenh.Parameters.Add(new SqlParameter("@LockMode", SqlDbType.VarChar, 32) { Value = "Exclusive" });
            lenh.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.VarChar, 32) { Value = "Transaction" });
            lenh.Parameters.Add(new SqlParameter("@LockTimeout", SqlDbType.Int) { Value = choToiDaMs });
            var traVe = lenh.Parameters.Add(new SqlParameter("@KetQua", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue });

            lenh.ExecuteNonQuery();

            // 0 / 1 = da lay khoa; -1 het thoi gian cho; -2 bi huy; -3 deadlock; -999 loi tham so
            if (traVe.Value is int kq && kq < 0)
                throw new TimeoutException(
                    $"Không lấy được khóa \"{tenTaiNguyen}\" (mã {kq}): đang có người khác lưu cùng loại dữ liệu, vui lòng thử lại.");
        }

        // Ghi kiemtoan.NhatKyHeThong trong CUNG giao dich (quyet dinh Q5: Service goi
        // sp_GhiNhatKyHeThong, biet nguoi thao tac, rollback thi nhat ky cung bien mat).
        public void GhiNhatKyHeThong(int? maTaiKhoan, string hanhDong, string tenBang, string khoaChinh,
                                     string? duLieuCu, string? duLieuMoi)
        {
            using var lenh = TaoLenh("kiemtoan.sp_GhiNhatKyHeThong");
            lenh.CommandType = CommandType.StoredProcedure;
            lenh.Parameters.Add(new SqlParameter("@MaTaiKhoan", SqlDbType.Int) { Value = (object?)maTaiKhoan ?? DBNull.Value });
            lenh.Parameters.Add(new SqlParameter("@HanhDong", SqlDbType.VarChar, 50) { Value = hanhDong });
            lenh.Parameters.Add(new SqlParameter("@TenBang", SqlDbType.VarChar, 50) { Value = tenBang });
            lenh.Parameters.Add(new SqlParameter("@KhoaChinhRecord", SqlDbType.VarChar, 50) { Value = khoaChinh });
            lenh.Parameters.Add(new SqlParameter("@DuLieuCu", SqlDbType.NVarChar, -1) { Value = (object?)duLieuCu ?? DBNull.Value });
            lenh.Parameters.Add(new SqlParameter("@DuLieuMoi", SqlDbType.NVarChar, -1) { Value = (object?)duLieuMoi ?? DBNull.Value });
            lenh.Parameters.Add(new SqlParameter("@DiaChiIP", SqlDbType.VarChar, 45) { Value = Environment.MachineName });
            lenh.ExecuteNonQuery();
        }

        public void XacNhan()
        {
            if (_daKetThuc) throw new InvalidOperationException("Giao dịch đã kết thúc.");
            _giaoDich.Commit();
            _daKetThuc = true;
        }

        public void Dispose()
        {
            if (!_daKetThuc)
            {
                _daKetThuc = true;
                try
                {
                    _giaoDich.Rollback();
                }
                catch (Exception ex) when (ex is InvalidOperationException or SqlException)
                {
                    // Giao dich da bi SQL Server huy truoc (vd trigger ROLLBACK, loi XACT_ABORT)
                    // hoac mat ket noi: khong con gi de hoan tac. Khong nem tiep de khoi che
                    // mat loi goc dang lan ra khoi khoi using.
                }
            }
            _giaoDich.Dispose();
            _ketNoi.Dispose();
        }

        // ---------------------------------------------------------------------
        // Dung cho cac Repository trong DAL
        // ---------------------------------------------------------------------

        internal SqlCommand TaoLenh(string sql, params SqlParameter[] thamSo)
        {
            if (_daKetThuc) throw new InvalidOperationException("Giao dịch đã kết thúc.");
            var lenh = new SqlCommand(sql, _ketNoi, _giaoDich);
            if (thamSo.Length > 0) lenh.Parameters.AddRange(thamSo);
            return lenh;
        }

        // gd == null: chay doc lap qua DatabaseHelper (moi lenh mot ket noi)
        internal static DataTable TruyVan(PhienGiaoDich? gd, string sql, params SqlParameter[] thamSo)
        {
            if (gd == null) return DatabaseHelper.ExecuteQuery(sql, thamSo.Length > 0 ? thamSo : null);

            // SqlDataAdapter giong DatabaseHelper.ExecuteQuery: cung kieu cot, khong suy rang buoc khoa
            var bang = new DataTable();
            using var lenh = gd.TaoLenh(sql, thamSo);
            using var boDoc = new SqlDataAdapter(lenh);
            boDoc.Fill(bang);
            return bang;
        }

        internal static int ThucThi(PhienGiaoDich? gd, string sql, params SqlParameter[] thamSo)
        {
            if (gd == null) return DatabaseHelper.ExecuteNonQuery(sql, thamSo.Length > 0 ? thamSo : null);

            using var lenh = gd.TaoLenh(sql, thamSo);
            return lenh.ExecuteNonQuery();
        }

        internal static object? LayGiaTri(PhienGiaoDich? gd, string sql, params SqlParameter[] thamSo)
        {
            if (gd == null) return DatabaseHelper.ExecuteScalar(sql, thamSo.Length > 0 ? thamSo : null);

            using var lenh = gd.TaoLenh(sql, thamSo);
            return lenh.ExecuteScalar();
        }
    }
}
