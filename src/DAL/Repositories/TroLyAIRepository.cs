using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using DAL.Connection;

namespace DAL.Repositories
{
    // =========================================================================
    // TRO LY AI (Sprint 3, Dat): chi DOC bang cua A Ba (hatang.Ga, vanhanh.LichDungGa) cho phan
    // con thieu o VeService (gio di / den tai ga cua khach, cu ly); GHI kiemtoan.NhatKyChatAI (chu: Dat).
    // Tim chuyen / toa / so do ghe / gia: goi VeService cua A Ba (hop dong §7.4), khong viet lai.
    // =========================================================================
    public class TroLyAIRepository
    {
        public List<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)> LayDanhSachGa()
            => DatabaseHelper.ExecuteQuery(@"
                SELECT MaGa, MaGaCode, TenGa, LyTrinhKm FROM hatang.Ga
                WHERE DangKhaiThac = 1 ORDER BY LyTrinhKm").Rows.Cast<DataRow>()
               .Select(r => (Convert.ToInt32(r["MaGa"]), r["MaGaCode"].ToString() ?? "", r["TenGa"].ToString() ?? "",
                             Convert.ToDecimal(r["LyTrinhKm"]))).ToList();

        // Gio di ke hoach tai ga di va gio den ke hoach tai ga den cua mot chuyen (null = chuyen chua co lich dung).
        public (DateTime GioDi, DateTime GioDen)? LayGioTaiGa(int maChuyenTau, int maGaDi, int maGaDen)
        {
            var bang = DatabaseHelper.ExecuteQuery(@"
                SELECT di.GioDiKeHoach AS GioDi, den.GioDenKeHoach AS GioDen
                FROM vanhanh.LichDungGa di
                JOIN vanhanh.LichDungGa den ON den.MaChuyenTau = di.MaChuyenTau AND den.ThuTuDung > di.ThuTuDung
                WHERE di.MaChuyenTau = @ct AND di.MaGa = @gaDi AND den.MaGa = @gaDen",
                new[] { new SqlParameter("@ct", maChuyenTau), new SqlParameter("@gaDi", maGaDi), new SqlParameter("@gaDen", maGaDen) });
            if (bang.Rows.Count == 0) return null;
            return (Convert.ToDateTime(bang.Rows[0]["GioDi"]), Convert.ToDateTime(bang.Rows[0]["GioDen"]));
        }

        // [16/11] Cong cu cho tro chuyen AI (chi doc).
        public List<(string SoHieu, string LoaiTau, string HuongChay, string TenGaDi, string TenGaDen, decimal ThoiGianChuanGio)> LayDanhSachMacTau()
            => DatabaseHelper.ExecuteQuery(@"
                SELECT mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay, gDi.TenGa AS TenGaDi, gDen.TenGa AS TenGaDen, mt.ThoiGianChuanGio
                FROM vanhanh.MacTauMau mt
                JOIN hatang.Ga gDi ON gDi.MaGa = mt.MaGaDi
                JOIN hatang.Ga gDen ON gDen.MaGa = mt.MaGaDen
                WHERE mt.DangKhaiThac = 1
                ORDER BY mt.LoaiTau, mt.SoHieuMacTau").Rows.Cast<DataRow>()
               .Select(r => (r["SoHieuMacTau"].ToString() ?? "", r["LoaiTau"].ToString() ?? "", r["HuongChay"].ToString() ?? "",
                             r["TenGaDi"].ToString() ?? "", r["TenGaDen"].ToString() ?? "", Convert.ToDecimal(r["ThoiGianChuanGio"]))).ToList();

        public (string SoHieuMacTau, string LoaiTau, DateTime NgayXuatPhat, string TrangThai)? LayThongTinChuyen(int maChuyenTau)
        {
            var bang = DatabaseHelper.ExecuteQuery(@"
                SELECT mt.SoHieuMacTau, mt.LoaiTau, ct.NgayXuatPhat, ct.TrangThai
                FROM vanhanh.ChuyenTau ct JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                WHERE ct.MaChuyenTau = @ct", new[] { new SqlParameter("@ct", maChuyenTau) });
            if (bang.Rows.Count == 0) return null;
            var r = bang.Rows[0];
            return (r["SoHieuMacTau"].ToString() ?? "", r["LoaiTau"].ToString() ?? "", Convert.ToDateTime(r["NgayXuatPhat"]),
                    r["TrangThai"].ToString() ?? "");
        }

        public List<(int ThuTu, string MaGaCode, string TenGa, DateTime GioDen, DateTime GioDi)> LayLichDung(int maChuyenTau)
            => DatabaseHelper.ExecuteQuery(@"
                SELECT ld.ThuTuDung, g.MaGaCode, g.TenGa, ld.GioDenKeHoach, ld.GioDiKeHoach
                FROM vanhanh.LichDungGa ld JOIN hatang.Ga g ON g.MaGa = ld.MaGa
                WHERE ld.MaChuyenTau = @ct ORDER BY ld.ThuTuDung",
                new[] { new SqlParameter("@ct", maChuyenTau) }).Rows.Cast<DataRow>()
               .Select(r => (Convert.ToInt32(r["ThuTuDung"]), r["MaGaCode"].ToString() ?? "", r["TenGa"].ToString() ?? "",
                             Convert.ToDateTime(r["GioDenKeHoach"]), Convert.ToDateTime(r["GioDiKeHoach"]))).ToList();

        public int GhiNhatKyChat(int maTaiKhoan, string cauHoi, string cauTraLoi, string? cauSql = null)
            => Convert.ToInt32(DatabaseHelper.ExecuteScalar(@"
                INSERT INTO kiemtoan.NhatKyChatAI (MaTaiKhoan, CauHoi, CauSQL, CauTraLoi)
                OUTPUT INSERTED.MaChat
                VALUES (@tk, @hoi, @sql, @tl)",
                new[]
                {
                    new SqlParameter("@tk", maTaiKhoan),
                    new SqlParameter("@hoi", SqlDbType.NVarChar, -1) { Value = cauHoi },
                    new SqlParameter("@sql", SqlDbType.NVarChar, -1) { Value = (object?)cauSql ?? DBNull.Value },
                    new SqlParameter("@tl", SqlDbType.NVarChar, -1) { Value = cauTraLoi }
                }));
    }
}
