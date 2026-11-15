using System.Data;
using Microsoft.Data.SqlClient;

namespace DAL.Connection
{
    // =========================================================================
    // [13/11 Dat] CHONG GHI DE KHI HAI NGUOI CUNG SUA MOT CHUYEN TAU
    //
    // vanhanh.ChuyenTau.PhienBan (ROWVERSION, co san trong script goc) = "phien ban cua ca chuyen":
    // moi thao tac ghi tren chuyen (form chuyen, lich dung ga, doan tau, trang thai, xep tranh) deu doi no.
    // Man hinh giu PhienBan luc nap; khi ghi ma PhienBan da khac -> khong ghi, bao nguoi dung nap lai.
    //
    // Khoa ung dung "vanhanh.SuaChuyen:<ma>" tuan tu hoa cac lan ghi cung mot chuyen:
    //   - Lich trinh (cac ham ghi cu cua A Ba tu mo ket noi rieng): KhoaSuaChuyen.Mo giu khoa muc PHIEN tren mot ket
    //     noi rieng trong luc ham ghi chay -> doc PhienBan -> so -> ghi -> cham PhienBan -> nha khoa.
    //   - Lap tau (Phuong tien), doi trang thai, xep tranh: lay cung khoa trong giao dich cua minh (muc GIAO DICH)
    //     va cham PhienBan trong cung giao dich.
    // Khoa "vantai.BanVe:<ma>": dat ve (sp_DatVeTheoChang) lay Shared, dong bo toa ban ve lay Exclusive.
    // =========================================================================
    public sealed class KhoaSuaChuyen : IDisposable
    {
        public static string TenKhoa(int maChuyenTau) => $"vanhanh.SuaChuyen:{maChuyenTau}";
        public static string TenKhoaBanVe(int maChuyenTau) => $"vantai.BanVe:{maChuyenTau}";

        private readonly SqlConnection _ketNoi;
        private bool _daDong;

        public int MaChuyenTau { get; }

        private KhoaSuaChuyen(SqlConnection ketNoi, int maChuyenTau)
        {
            _ketNoi = ketNoi;
            MaChuyenTau = maChuyenTau;
        }

        public static KhoaSuaChuyen Mo(int maChuyenTau, int choToiDaMs = 10_000)
        {
            var ketNoi = new SqlConnection(DatabaseHelper.GetConnectionString());
            try
            {
                ketNoi.Open();
                using var lenh = new SqlCommand("sp_getapplock", ketNoi) { CommandType = CommandType.StoredProcedure };
                lenh.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = TenKhoa(maChuyenTau) });
                lenh.Parameters.Add(new SqlParameter("@LockMode", SqlDbType.VarChar, 32) { Value = "Exclusive" });
                lenh.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.VarChar, 32) { Value = "Session" });
                lenh.Parameters.Add(new SqlParameter("@LockTimeout", SqlDbType.Int) { Value = choToiDaMs });
                var traVe = lenh.Parameters.Add(new SqlParameter("@KetQua", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue });
                lenh.ExecuteNonQuery();
                if (traVe.Value is int kq && kq < 0)
                    throw new TimeoutException("Chuyến tàu này đang được người khác lưu thay đổi. Vui lòng thử lại sau ít giây.");
                return new KhoaSuaChuyen(ketNoi, maChuyenTau);
            }
            catch
            {
                ketNoi.Dispose();
                throw;
            }
        }

        // null = chuyen khong con trong CSDL
        public byte[]? DocPhienBan()
        {
            using var lenh = new SqlCommand("SELECT PhienBan FROM vanhanh.ChuyenTau WHERE MaChuyenTau = @id", _ketNoi);
            lenh.Parameters.AddWithValue("@id", MaChuyenTau);
            return lenh.ExecuteScalar() as byte[];
        }

        // Doi PhienBan (thao tac chi ghi bang con - LichDungGa, DoanTau - khong tu doi PhienBan cua ChuyenTau)
        public void Cham()
        {
            using var lenh = new SqlCommand(
                "UPDATE vanhanh.ChuyenTau SET SoPhutTreLuyKe = SoPhutTreLuyKe WHERE MaChuyenTau = @id", _ketNoi);
            lenh.Parameters.AddWithValue("@id", MaChuyenTau);
            lenh.ExecuteNonQuery();
        }

        public void GhiNhatKy(string hanhDong, object? duLieuMoi)
            => PhienGiaoDich.GhiNhatKy(_ketNoi, null, hanhDong, "vanhanh.ChuyenTau", MaChuyenTau.ToString(), null, duLieuMoi);

        // Lan ghi gan nhat cua chuyen trong nhat ky he thong (chuyen, doan tau, toa ban ve). null = khong co.
        public static LanSuaChuyenDTO? LayLanSuaGanNhat(int maChuyenTau)
        {
            DataTable bang = DatabaseHelper.ExecuteQuery(@"
                SELECT TOP (1) DATEADD(MINUTE, DATEDIFF(MINUTE, SYSUTCDATETIME(), SYSDATETIME()), nk.ThoiDiem) AS LucSua,
                       ISNULL(tk.TenDangNhap, N'') AS NguoiSua, nk.HanhDong, nk.DuLieuMoi
                FROM kiemtoan.NhatKyHeThong nk
                LEFT JOIN phanquyen.TaiKhoan tk ON tk.MaTaiKhoan = nk.MaTaiKhoan
                WHERE (nk.TenBang IN ('vanhanh.ChuyenTau', 'vantai.ToaXeKhach') AND nk.KhoaChinhRecord = @ma)
                   OR (nk.TenBang = 'vanhanh.DoanTau'
                       AND nk.KhoaChinhRecord IN (SELECT CAST(MaDoanTau AS VARCHAR(20)) FROM vanhanh.DoanTau WHERE MaChuyenTau = @ct))
                ORDER BY nk.MaNhatKy DESC",
                new[]
                {
                    new SqlParameter("@ma", SqlDbType.VarChar, 50) { Value = maChuyenTau.ToString() },
                    new SqlParameter("@ct", maChuyenTau)
                });
            if (bang.Rows.Count == 0) return null;
            DataRow r = bang.Rows[0];
            return new LanSuaChuyenDTO(
                Convert.ToDateTime(r["LucSua"]),
                r["NguoiSua"]?.ToString() ?? "",
                r["HanhDong"]?.ToString() ?? "",
                r["DuLieuMoi"] == DBNull.Value ? null : r["DuLieuMoi"].ToString());
        }

        public void Dispose()
        {
            if (_daDong) return;
            _daDong = true;
            try
            {
                if (_ketNoi.State == ConnectionState.Open)
                {
                    using var lenh = new SqlCommand("sp_releaseapplock", _ketNoi) { CommandType = CommandType.StoredProcedure };
                    lenh.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = TenKhoa(MaChuyenTau) });
                    lenh.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.VarChar, 32) { Value = "Session" });
                    lenh.ExecuteNonQuery();
                }
            }
            catch (SqlException)
            {
                // Mat ket noi: SQL Server tu nha khoa khi phien dong
            }
            _ketNoi.Dispose();
        }
    }

    public record LanSuaChuyenDTO(DateTime LucSua, string NguoiSua, string HanhDong, string? DuLieuMoi);
}
