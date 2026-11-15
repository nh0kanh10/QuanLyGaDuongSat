using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using DTO.BaoCao;

namespace DAL.Repositories
{
    // =========================================================================
    // BAO CAO THONG KE (Sprint 3, Dat): chi DOC cac view baocao.vw_* (migration V20261108_1300).
    // Moi chi so tinh trong SQL (GROUP BY tren view) -> so tren man hinh = so chay tay tren view.
    // =========================================================================
    public class BaoCaoRepository
    {
        private static SqlParameter[] ThamSo(BoLocBaoCao loc) => new[]
        {
            new SqlParameter("@tu", SqlDbType.Date) { Value = loc.TuNgay.Date },
            new SqlParameter("@den", SqlDbType.Date) { Value = loc.DenNgay.Date },
            new SqlParameter("@mac", SqlDbType.VarChar, 20) { Value = (object?)loc.SoHieuMacTau ?? DBNull.Value }
        };

        private const string LocChuyen = "NgayChay BETWEEN @tu AND @den AND (@mac IS NULL OR SoHieuMacTau = @mac)";

        public List<string> LayDanhSachMacTau()
            => DatabaseHelper.ExecuteQuery(@"SELECT SoHieuMacTau FROM vanhanh.MacTauMau
                                             WHERE LoaiTau <> 'TAU_HANG' ORDER BY SoHieuMacTau")
               .Rows.Cast<DataRow>().Select(r => r[0].ToString() ?? "").ToList();

        public KetQuaBaoCao LayBaoCao(BoLocBaoCao loc)
        {
            var kq = new KetQuaBaoCao { BoLoc = loc };

            // --- Theo mac tau (gom 3 view) ---
            var mac = new Dictionary<string, DongMacTau>();
            DongMacTau M(string s) => mac.TryGetValue(s, out var d) ? d : mac[s] = new DongMacTau { SoHieuMacTau = s };

            foreach (DataRow r in Q($@"
                SELECT SoHieuMacTau, COUNT(*) AS SoVe, SUM(DoanhThuThuan) AS DoanhThu
                FROM baocao.vw_DoanhThuVe WHERE {LocChuyen} GROUP BY SoHieuMacTau", loc).Rows)
            {
                var d = M(Chuoi(r, "SoHieuMacTau"));
                d.SoVe = Convert.ToInt32(r["SoVe"]);
                d.DoanhThu = So(r, "DoanhThu");
            }

            foreach (DataRow r in Q($@"
                SELECT SoHieuMacTau, COUNT(*) AS SoChuyen,
                       CASE WHEN SUM(GheKmCungUng) > 0 THEN SUM(GheKmDaBan) / SUM(GheKmCungUng) ELSE 0 END AS HeSo
                FROM baocao.vw_HeSoSuDungGhe WHERE {LocChuyen} GROUP BY SoHieuMacTau", loc).Rows)
            {
                var d = M(Chuoi(r, "SoHieuMacTau"));
                d.SoChuyen = Convert.ToInt32(r["SoChuyen"]);
                d.HeSoGhe = Convert.ToDouble(r["HeSo"]);
            }

            foreach (DataRow r in Q($@"
                SELECT SoHieuMacTau, COUNT(*) AS N, SUM(CAST(DungGio AS INT)) AS Dung, AVG(CAST(SoPhutTre AS FLOAT)) AS TreTB, MAX(SoPhutTre) AS TreMax
                FROM baocao.vw_DungGioChuyen WHERE {LocChuyen} GROUP BY SoHieuMacTau", loc).Rows)
            {
                var d = M(Chuoi(r, "SoHieuMacTau"));
                d.SoChuyenCoGio = Convert.ToInt32(r["N"]);
                d.SoChuyenDungGio = Convert.ToInt32(r["Dung"]);
                d.TreTrungBinh = r["TreTB"] == DBNull.Value ? 0 : Convert.ToDouble(r["TreTB"]);
                d.TreLonNhat = r["TreMax"] == DBNull.Value ? 0 : Convert.ToInt32(r["TreMax"]);
            }
            kq.TheoMacTau = mac.Values.OrderByDescending(x => x.DoanhThu).ToList();

            // --- Tong quan ---
            var t = Q($@"
                SELECT COUNT(*) AS SoVe, SUM(CAST(LaVeHoan AS INT)) AS SoHoan, ISNULL(SUM(DoanhThuThuan), 0) AS DoanhThu
                FROM baocao.vw_DoanhThuVe WHERE {LocChuyen}", loc).Rows[0];
            var g = Q($@"
                SELECT COUNT(*) AS SoChuyen, CASE WHEN SUM(GheKmCungUng) > 0 THEN SUM(GheKmDaBan) / SUM(GheKmCungUng) ELSE 0 END AS HeSo
                FROM baocao.vw_HeSoSuDungGhe WHERE {LocChuyen}", loc).Rows[0];
            var dg = Q($@"
                SELECT COUNT(*) AS N, ISNULL(SUM(CAST(DungGio AS INT)), 0) AS Dung, AVG(CAST(SoPhutTre AS FLOAT)) AS TreTB
                FROM baocao.vw_DungGioChuyen WHERE {LocChuyen}", loc).Rows[0];
            var h = Q(@"
                SELECT COUNT(*) AS N, ISNULL(SUM(TrongLuongTan), 0) AS Tan, ISNULL(SUM(TanKm), 0) AS TanKm, ISNULL(SUM(CuocPhi), 0) AS Cuoc
                FROM baocao.vw_SanLuongHang WHERE NgayTao BETWEEN @tu AND @den", loc).Rows[0];
            kq.TongQuan = new TongQuanBaoCao
            {
                SoVe = Convert.ToInt32(t["SoVe"]),
                SoVeHoan = t["SoHoan"] == DBNull.Value ? 0 : Convert.ToInt32(t["SoHoan"]),
                DoanhThuThuan = So(t, "DoanhThu"),
                SoChuyen = Convert.ToInt32(g["SoChuyen"]),
                HeSoGhe = Convert.ToDouble(g["HeSo"]),
                SoChuyenCoGio = Convert.ToInt32(dg["N"]),
                SoChuyenDungGio = Convert.ToInt32(dg["Dung"]),
                TreTrungBinh = dg["TreTB"] == DBNull.Value ? 0 : Convert.ToDouble(dg["TreTB"]),
                SoVanDon = Convert.ToInt32(h["N"]),
                SanLuongTan = So(h, "Tan"),
                TanKm = So(h, "TanKm"),
                CuocHang = So(h, "Cuoc")
            };

            // --- Theo ngay ---
            kq.TheoNgay = Q($@"
                SELECT NgayChay, COUNT(*) AS SoVe, SUM(DoanhThuThuan) AS DoanhThu
                FROM baocao.vw_DoanhThuVe WHERE {LocChuyen} GROUP BY NgayChay ORDER BY NgayChay", loc).Rows.Cast<DataRow>()
                .Select(r => new DongNgay { Ngay = Convert.ToDateTime(r["NgayChay"]), SoVe = Convert.ToInt32(r["SoVe"]), DoanhThu = So(r, "DoanhThu") }).ToList();

            // --- Theo ga di ---
            kq.TheoGa = Q($@"
                SELECT TenGaDi, COUNT(*) AS SoVe, SUM(DoanhThuThuan) AS DoanhThu
                FROM baocao.vw_DoanhThuVe WHERE {LocChuyen} GROUP BY TenGaDi ORDER BY SUM(DoanhThuThuan) DESC", loc).Rows.Cast<DataRow>()
                .Select(r => new DongGa { TenGa = Chuoi(r, "TenGaDi"), SoVe = Convert.ToInt32(r["SoVe"]), DoanhThu = So(r, "DoanhThu") }).ToList();

            // --- Theo chuyen ---
            kq.TheoChuyen = Q($@"
                SELECT h.MaChuyenTau, h.NgayChay, h.SoHieuMacTau, h.CuLyKm, h.SoGhe, h.SoVe, h.HeSo, d.SoPhutTre, ISNULL(d.SoSuCo, 0) AS SoSuCo
                FROM baocao.vw_HeSoSuDungGhe h
                LEFT JOIN baocao.vw_DungGioChuyen d ON d.MaChuyenTau = h.MaChuyenTau
                WHERE h.NgayChay BETWEEN @tu AND @den AND (@mac IS NULL OR h.SoHieuMacTau = @mac)
                ORDER BY h.NgayChay DESC, h.SoHieuMacTau", loc).Rows.Cast<DataRow>()
                .Select(r => new DongChuyen
                {
                    MaChuyenTau = Convert.ToInt32(r["MaChuyenTau"]),
                    NgayChay = Convert.ToDateTime(r["NgayChay"]),
                    SoHieuMacTau = Chuoi(r, "SoHieuMacTau"),
                    CuLyKm = So(r, "CuLyKm"),
                    SoGhe = Convert.ToInt32(r["SoGhe"]),
                    SoVe = Convert.ToInt32(r["SoVe"]),
                    HeSo = Convert.ToDouble(r["HeSo"]),
                    SoPhutTre = r["SoPhutTre"] == DBNull.Value ? null : Convert.ToInt32(r["SoPhutTre"]),
                    SoSuCo = Convert.ToInt32(r["SoSuCo"])
                }).ToList();

            // --- Hang hoa ---
            kq.HangHoa = Q(@"
                SELECT TenHangHoa, COUNT(*) AS N, SUM(TrongLuongTan) AS Tan, SUM(TanKm) AS TanKm, SUM(CuocPhi) AS Cuoc
                FROM baocao.vw_SanLuongHang WHERE NgayTao BETWEEN @tu AND @den
                GROUP BY TenHangHoa ORDER BY SUM(TanKm) DESC", loc).Rows.Cast<DataRow>()
                .Select(r => new DongHangHoa { TenHangHoa = Chuoi(r, "TenHangHoa"), SoVanDon = Convert.ToInt32(r["N"]),
                                               Tan = So(r, "Tan"), TanKm = So(r, "TanKm"), Cuoc = So(r, "Cuoc") }).ToList();
            return kq;
        }

        private static DataTable Q(string sql, BoLocBaoCao loc) => DatabaseHelper.ExecuteQuery(sql, ThamSo(loc));
        private static string Chuoi(DataRow r, string c) => r[c] == DBNull.Value ? "" : r[c].ToString() ?? "";
        private static decimal So(DataRow r, string c) => r[c] == DBNull.Value ? 0m : Convert.ToDecimal(r[c]);
    }
}
