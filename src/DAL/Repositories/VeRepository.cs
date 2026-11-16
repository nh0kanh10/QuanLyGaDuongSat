using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using ET.VanTai;
using DTO.Common;

namespace DAL.Repositories
{
    public class VeRepository
    {
        public DataTable LayDanhSach()
        {
            DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
            string sql = @"SELECT v.MaVe, v.MaVeCode, mt.SoHieuMacTau, mt.LoaiTau,
                                  dv.MaPNR, dv.MaDonVe, DATEADD(HOUR, 7, dv.ThoiDiemTao) AS ThoiDiemTaoDon, dv.HinhThucTT, dv.TrangThaiTT,
                                  kh.HoTen AS TenNguoiMua, kh.SoDienThoai AS SdtNguoiMua, kh.SoCCCD AS CccdNguoiMua,
                                  g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen,
                                  v.TenHanhKhach, v.CCCDHanhKhach, cn.SoGhe, cn.TangGiuong, cn.MaChoNgoi,
                                  tx.LoaiToa, tx.NhanHieuToa, tx.MaToaXeKhach,
                                  v.GiaVeGoc, v.SoTienGiam, v.GiaVeThucThu,
                                  CASE 
                                      WHEN v.TrangThai = 'DA_DAT' AND (
                                          ct.TrangThai IN ('HOAN_THANH', 'DA_HUY') 
                                          OR CAST(ct.NgayXuatPhat AS DATE) < CAST(@now AS DATE)
                                          OR (ct.TrangThai = 'DANG_CHAY' AND (
                                              EXISTS (
                                                  SELECT 1 FROM vanhanh.LichDungGa ld 
                                                  WHERE ld.MaChuyenTau = ct.MaChuyenTau 
                                                    AND ld.MaGa = v.MaGaDi 
                                                    AND (ld.GioDiThucTe IS NOT NULL OR ld.GioDiKeHoach < @now)
                                              )
                                          ))
                                      ) THEN 'HET_HAN'
                                      ELSE v.TrangThai
                                  END AS TrangThai,
                                  v.TrangThai AS TrangThaiGoc,
                                  DATEADD(HOUR, 7, v.ThoiDiemXuatVe) AS ThoiDiemXuatVe,
                                  v.MaChuyenTau, v.MaGaDi, v.MaGaDen,
                                  ct.NgayXuatPhat, ct.GioXuatPhatKH,
                                  DATEADD(HOUR, 7, hh.ThoiDiemHoan) AS ThoiDiemHoan, hh.TyLeLePhi, hh.LePhiHoan, hh.SoTienThucHoan, hh.HinhThucHoan, hh.LyDoHoan
                           FROM vantai.Ve v
                           JOIN vantai.DonDatVe dv ON v.MaDonVe = dv.MaDonVe
                           LEFT JOIN vantai.KhachHang kh ON dv.MaKhachHang = kh.MaKhachHang
                           JOIN vanhanh.ChuyenTau ct ON v.MaChuyenTau = ct.MaChuyenTau
                           JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                           JOIN hatang.Ga g1 ON v.MaGaDi = g1.MaGa
                           JOIN hatang.Ga g2 ON v.MaGaDen = g2.MaGa
                           JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
                           JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                           LEFT JOIN vantai.HoanHuyVe hh ON v.MaVe = hh.MaVe
                           ORDER BY v.ThoiDiemXuatVe DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@now", now) });
        }

        public DataTable TimKiemVe(string keyword)
        {
            DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
            string sql = @"SELECT v.MaVe, v.MaVeCode, mt.SoHieuMacTau, mt.LoaiTau,
                                  dv.MaPNR, dv.MaDonVe, DATEADD(HOUR, 7, dv.ThoiDiemTao) AS ThoiDiemTaoDon, dv.HinhThucTT, dv.TrangThaiTT,
                                  kh.HoTen AS TenNguoiMua, kh.SoDienThoai AS SdtNguoiMua, kh.SoCCCD AS CccdNguoiMua,
                                  g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen,
                                  v.TenHanhKhach, v.CCCDHanhKhach, cn.SoGhe, cn.TangGiuong, cn.MaChoNgoi,
                                  tx.LoaiToa, tx.NhanHieuToa, tx.MaToaXeKhach,
                                  v.GiaVeGoc, v.SoTienGiam, v.GiaVeThucThu,
                                  CASE 
                                      WHEN v.TrangThai = 'DA_DAT' AND (
                                          ct.TrangThai IN ('HOAN_THANH', 'DA_HUY') 
                                          OR CAST(ct.NgayXuatPhat AS DATE) < CAST(@now AS DATE)
                                          OR (ct.TrangThai = 'DANG_CHAY' AND (
                                              EXISTS (
                                                  SELECT 1 FROM vanhanh.LichDungGa ld 
                                                  WHERE ld.MaChuyenTau = ct.MaChuyenTau 
                                                    AND ld.MaGa = v.MaGaDi 
                                                    AND (ld.GioDiThucTe IS NOT NULL OR ld.GioDiKeHoach < @now)
                                              )
                                          ))
                                      ) THEN 'HET_HAN'
                                      ELSE v.TrangThai
                                  END AS TrangThai,
                                  v.TrangThai AS TrangThaiGoc,
                                  DATEADD(HOUR, 7, v.ThoiDiemXuatVe) AS ThoiDiemXuatVe,
                                  v.MaChuyenTau, v.MaGaDi, v.MaGaDen,
                                  ct.NgayXuatPhat, ct.GioXuatPhatKH,
                                  DATEADD(HOUR, 7, hh.ThoiDiemHoan) AS ThoiDiemHoan, hh.TyLeLePhi, hh.LePhiHoan, hh.SoTienThucHoan, hh.HinhThucHoan, hh.LyDoHoan
                           FROM vantai.Ve v
                           JOIN vantai.DonDatVe dv ON v.MaDonVe = dv.MaDonVe
                           LEFT JOIN vantai.KhachHang kh ON dv.MaKhachHang = kh.MaKhachHang
                           JOIN vanhanh.ChuyenTau ct ON v.MaChuyenTau = ct.MaChuyenTau
                           JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                           JOIN hatang.Ga g1 ON v.MaGaDi = g1.MaGa
                           JOIN hatang.Ga g2 ON v.MaGaDen = g2.MaGa
                           JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
                           JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                           LEFT JOIN vantai.HoanHuyVe hh ON v.MaVe = hh.MaVe
                           WHERE v.MaVeCode LIKE @kw 
                              OR dv.MaPNR LIKE @kw 
                              OR v.CCCDHanhKhach LIKE @kw 
                              OR v.TenHanhKhach LIKE @kw 
                              OR mt.SoHieuMacTau LIKE @kw
                              OR kh.HoTen LIKE @kw
                              OR kh.SoCCCD LIKE @kw
                              OR kh.SoDienThoai LIKE @kw
                           ORDER BY v.ThoiDiemXuatVe DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[] {
                new SqlParameter("@kw", $"%{keyword}%"),
                new SqlParameter("@now", now)
            });
        }

        public DataTable TimChuyenTauTheoChang(int maGaDi, int maGaDen, DateTime ngayDi, DateTime? gioHienTai = null)
        {
            DateTime thoiDiemXet = gioHienTai ?? DTO.Common.ThoiGianHeThongHelper.LayGioHienTai();
            string sql = @"
                WITH UserChang AS (
                    SELECT 
                        IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) AS UserKmMin,
                        IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDen.LyTrinhKm, gDi.LyTrinhKm) AS UserKmMax
                    FROM hatang.Ga gDi, hatang.Ga gDen
                    WHERE gDi.MaGa = @gaDi AND gDen.MaGa = @gaDen
                )
                SELECT ct.MaChuyenTau, ct.MaMacTau, mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay,
                       ct.NgayXuatPhat,
                       ISNULL((SELECT TOP 1 ldDi.GioDiKeHoach FROM vanhanh.LichDungGa ldDi WHERE ldDi.MaChuyenTau = ct.MaChuyenTau AND ldDi.MaGa = @gaDi), ct.GioXuatPhatKH) AS GioXuatPhatKH,
                       ISNULL((SELECT TOP 1 ldDen.GioDenKeHoach FROM vanhanh.LichDungGa ldDen WHERE ldDen.MaChuyenTau = ct.MaChuyenTau AND ldDen.MaGa = @gaDen), ct.GioVeDichKH) AS GioVeDichKH,
                       ct.TrangThai,
                       gDi.TenGa AS TenGaXuatPhat, gDen.TenGa AS TenGaKetThuc,
                       -- [13/11] Chi toa dang ban (khong ngung ban) va chay qua ca chang cua khach (toa noi / cat doc duong)
                       (SELECT COUNT(*) FROM vantai.ToaXeKhach tx WHERE tx.MaChuyenTau = ct.MaChuyenTau AND tx.NgungBan = 0
                          AND vantai.fn_ToaChayQuaChang(tx.MaToaXeKhach, @gaDi, @gaDen) = 1) AS SoToaXe,
                       ISNULL((SELECT SUM(dem.SoCho) FROM vantai.ToaXeKhach tx2
                        CROSS APPLY (SELECT COUNT(*) AS SoCho FROM vantai.ChoNgoi cn WHERE cn.MaToaXeKhach = tx2.MaToaXeKhach) dem
                        WHERE tx2.MaChuyenTau = ct.MaChuyenTau AND tx2.NgungBan = 0
                          AND vantai.fn_ToaChayQuaChang(tx2.MaToaXeKhach, @gaDi, @gaDen) = 1), 0) AS TongSoCho,
                       (SELECT COUNT(DISTINCT v.MaChoNgoi) 
                        FROM vantai.Ve v 
                        JOIN hatang.Ga gvDi ON v.MaGaDi = gvDi.MaGa
                        JOIN hatang.Ga gvDen ON v.MaGaDen = gvDen.MaGa
                        CROSS JOIN UserChang uc
                        WHERE v.MaChuyenTau = ct.MaChuyenTau 
                          AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
                          AND IIF(gvDi.LyTrinhKm < gvDen.LyTrinhKm, gvDi.LyTrinhKm, gvDen.LyTrinhKm) < uc.UserKmMax
                          AND IIF(gvDi.LyTrinhKm > gvDen.LyTrinhKm, gvDi.LyTrinhKm, gvDen.LyTrinhKm) > uc.UserKmMin
                       ) AS SoChoDaBan,
                       ISNULL(vantai.fn_TinhGiaVe('NML', @gaDi, @gaDen, NULL, mt.LoaiTau), 150000) AS GiaVeCoSo
                FROM vanhanh.ChuyenTau ct
                JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                JOIN hatang.Ga gDi ON mt.MaGaDi = gDi.MaGa
                JOIN hatang.Ga gDen ON mt.MaGaDen = gDen.MaGa
                WHERE ct.TrangThai IN ('DA_LEN_LICH', 'SAN_SANG', 'DANG_CHAY', 'TRE_GIO')
                  AND (
                      -- Ưu tiên xét theo ngày đón khách thực tế tại ga đi (hỗ trợ tàu chạy xuyên ngày / xuyên đêm)
                      EXISTS (
                          SELECT 1 FROM vanhanh.LichDungGa ldDi
                          WHERE ldDi.MaChuyenTau = ct.MaChuyenTau 
                            AND ldDi.MaGa = @gaDi 
                            AND CAST(ldDi.GioDiKeHoach AS DATE) = CAST(@ngayDi AS DATE)
                            -- Nếu đi cùng ngày xét: Khóa bán vé trước giờ chạy 10 phút (sát giờ không lấy lên như qua giờ)
                            AND (CAST(@ngayDi AS DATE) > CAST(@thoiDiemXet AS DATE) OR DATEADD(MINUTE, -10, DATEADD(MINUTE, ISNULL(ct.SoPhutTreLuyKe, 0), ldDi.GioDiKeHoach)) >= @thoiDiemXet)
                      )
                      -- Dự phòng: Nếu chuyến tàu chưa lập bảng lịch dừng chi tiết, đối chiếu theo ngày xuất phát của tàu
                      OR (
                          NOT EXISTS (SELECT 1 FROM vanhanh.LichDungGa ld WHERE ld.MaChuyenTau = ct.MaChuyenTau)
                          AND CAST(ct.NgayXuatPhat AS DATE) = CAST(@ngayDi AS DATE)
                          AND (CAST(@ngayDi AS DATE) > CAST(@thoiDiemXet AS DATE) OR DATEADD(MINUTE, -10, DATEADD(MINUTE, ISNULL(ct.SoPhutTreLuyKe, 0), ct.GioXuatPhatKH)) >= @thoiDiemXet)
                      )
                  )
                  -- Ngăn bán vé khi chuyến tàu đã rời ga đi của hành khách (hoặc có GioDiThucTe <= thời điểm xét)
                  AND NOT EXISTS (
                      SELECT 1 
                      FROM vanhanh.LichDungGa ld 
                      WHERE ld.MaChuyenTau = ct.MaChuyenTau 
                        AND ld.MaGa = @gaDi 
                        AND ld.GioDiThucTe IS NOT NULL
                        AND ld.GioDiThucTe <= @thoiDiemXet
                  )
                  AND (
                      -- Kiểm tra tàu thực sự dừng ở cả 2 ga theo đúng thứ tự hành trình
                      EXISTS (
                          SELECT 1 
                          FROM vanhanh.LichDungGa ldDi
                          JOIN vanhanh.LichDungGa ldDen ON ldDi.MaChuyenTau = ldDen.MaChuyenTau
                          WHERE ldDi.MaChuyenTau = ct.MaChuyenTau
                            AND ldDi.MaGa = @gaDi
                            AND ldDen.MaGa = @gaDen
                            AND ldDi.ThuTuDung < ldDen.ThuTuDung
                      )
                      OR (
                          -- Phòng vệ: Nếu chuyến tàu chưa kịp tạo LichDungGa chi tiết, đối chiếu qua hướng chạy mác tàu
                          NOT EXISTS (SELECT 1 FROM vanhanh.LichDungGa ld WHERE ld.MaChuyenTau = ct.MaChuyenTau)
                          AND (
                              (
                                  (SELECT LyTrinhKm FROM hatang.Ga WHERE MaGa = @gaDi) < (SELECT LyTrinhKm FROM hatang.Ga WHERE MaGa = @gaDen)
                                  AND mt.HuongChay = 'BAC_NAM'
                              )
                              OR
                              (
                                  (SELECT LyTrinhKm FROM hatang.Ga WHERE MaGa = @gaDi) > (SELECT LyTrinhKm FROM hatang.Ga WHERE MaGa = @gaDen)
                                  AND mt.HuongChay = 'NAM_BAC'
                              )
                          )
                      )
                  )
                ORDER BY ct.GioXuatPhatKH ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@gaDi", maGaDi),
                new SqlParameter("@gaDen", maGaDen),
                new SqlParameter("@ngayDi", ngayDi.Date),
                new SqlParameter("@thoiDiemXet", thoiDiemXet)
            });
        }

        public DataTable LayDanhSachToaTheoChuyen(int maChuyenTau, int maGaDi, int maGaDen)
        {
            string sql = @"
                WITH UserChang AS (
                    SELECT 
                        IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) AS UserKmMin,
                        IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDen.LyTrinhKm, gDi.LyTrinhKm) AS UserKmMax
                    FROM hatang.Ga gDi, hatang.Ga gDen
                    WHERE gDi.MaGa = @gaDi AND gDen.MaGa = @gaDen
                )
                SELECT tx.MaToaXeKhach, tx.MaChuyenTau, tx.NhanHieuToa, tx.LoaiToa, tx.ThuTuToa, tx.SucChua,
                       CASE tx.LoaiToa
                           WHEN 'NC' THEN N'Ngồi Cứng'
                           WHEN 'NML' THEN N'Ngồi Mềm Lạnh'
                           WHEN 'BN' THEN N'Giường Nằm K6'
                           WHEN 'AN' THEN N'Giường Nằm K4 VIP'
                           ELSE tx.LoaiToa
                       END AS TenLoaiToa,
                       COUNT(DISTINCT cn.MaChoNgoi) AS TongSoCho,
                       COUNT(DISTINCT cn.MaChoNgoi) - COUNT(DISTINCT v.MaChoNgoi) AS SoChoTrong
                FROM vantai.ToaXeKhach tx
                CROSS JOIN UserChang uc
                LEFT JOIN vantai.ChoNgoi cn ON tx.MaToaXeKhach = cn.MaToaXeKhach
                LEFT JOIN (
                    SELECT vSub.MaChoNgoi, vSub.MaChuyenTau,
                           IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) AS VeKmMin,
                           IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDen.LyTrinhKm, gDi.LyTrinhKm) AS VeKmMax
                    FROM vantai.Ve vSub
                    JOIN hatang.Ga gDi ON vSub.MaGaDi = gDi.MaGa
                    JOIN hatang.Ga gDen ON vSub.MaGaDen = gDen.MaGa
                    WHERE vSub.MaChuyenTau = @chuyenTau
                      AND vSub.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
                ) v ON cn.MaChoNgoi = v.MaChoNgoi 
                    AND v.VeKmMin < uc.UserKmMax 
                    AND v.VeKmMax > uc.UserKmMin
                WHERE tx.MaChuyenTau = @chuyenTau
                  AND tx.NgungBan = 0                                                      -- [13/11] toa da ngung ban
                  AND vantai.fn_ToaChayQuaChang(tx.MaToaXeKhach, @gaDi, @gaDen) = 1      -- [13/11] toa noi / cat doc duong
                GROUP BY tx.MaToaXeKhach, tx.MaChuyenTau, tx.NhanHieuToa, tx.LoaiToa, tx.ThuTuToa, tx.SucChua
                ORDER BY tx.ThuTuToa ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@chuyenTau", maChuyenTau),
                new SqlParameter("@gaDi", maGaDi),
                new SqlParameter("@gaDen", maGaDen)
            });
        }

        public DataTable LaySoDoGhe(int maToaXeKhach, int maChuyenTau, int maGaDi, int maGaDen)
        {
            string sql = @"
                WITH UserChang AS (
                    SELECT 
                        IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) AS UserKmMin,
                        IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDen.LyTrinhKm, gDi.LyTrinhKm) AS UserKmMax
                    FROM hatang.Ga gDi, hatang.Ga gDen
                    WHERE gDi.MaGa = @gaDi AND gDen.MaGa = @gaDen
                )
                SELECT cn.MaChoNgoi, cn.MaToaXeKhach, cn.SoGhe, cn.TangGiuong, cn.LaGhePhu,
                       tx.LoaiToa, tx.NhanHieuToa,
                       CASE 
                           WHEN v.MaChoNgoi IS NOT NULL THEN 1 
                           ELSE 0 
                       END AS DaDat,
                       CASE 
                           WHEN v.MaChoNgoi IS NOT NULL THEN 'DA_DAT' 
                           ELSE 'CON_TRONG' 
                       END AS TrangThai,
                       v.TenHanhKhach AS KhachHienTai,
                       v.MaVeCode,
                       v.TenGaDiVe,
                       v.TenGaDenVe
                FROM vantai.ChoNgoi cn
                JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                CROSS JOIN UserChang uc
                OUTER APPLY (
                    SELECT TOP 1 vSub.MaChoNgoi, vSub.TenHanhKhach, vSub.MaVeCode,
                           gDi.TenGa AS TenGaDiVe, gDen.TenGa AS TenGaDenVe
                    FROM vantai.Ve vSub
                    JOIN hatang.Ga gDi ON vSub.MaGaDi = gDi.MaGa
                    JOIN hatang.Ga gDen ON vSub.MaGaDen = gDen.MaGa
                    WHERE vSub.MaChuyenTau = @chuyenTau
                      AND vSub.MaChoNgoi = cn.MaChoNgoi
                      AND vSub.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
                      AND IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) < uc.UserKmMax
                      AND IIF(gDi.LyTrinhKm > gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) > uc.UserKmMin
                    ORDER BY vSub.MaVe ASC
                ) v
                WHERE cn.MaToaXeKhach = @toa
                ORDER BY cn.SoGhe ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@toa", maToaXeKhach),
                new SqlParameter("@chuyenTau", maChuyenTau),
                new SqlParameter("@gaDi", maGaDi),
                new SqlParameter("@gaDen", maGaDen)
            });
        }

        public decimal TinhGiaVe(string loaiToa, int maGaDi, int maGaDen, int? tangGiuong = null, string loaiTau = "TAU_CHO")
        {
            string sql = "SELECT vantai.fn_TinhGiaVe(@loaiToa, @gaDi, @gaDen, @tang, @loaiTau)";
            var p = new[]
            {
                new SqlParameter("@loaiToa", loaiToa),
                new SqlParameter("@gaDi", maGaDi),
                new SqlParameter("@gaDen", maGaDen),
                new SqlParameter("@tang", (object?)tangGiuong ?? DBNull.Value),
                new SqlParameter("@loaiTau", loaiTau)
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

            string connStr = DatabaseHelper.GetConnectionString();
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var trans = conn.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                // 1. Kiem tra hoac tao moi KhachHang.
                //    Co CCCD thi chi tim theo CCCD (SoCCCD UNIQUE NOT NULL: nguoi khac trung SDT - vd nguoi nha -
                //    la khach khac, khong duoc gan don vao ho). Chi khi khong co CCCD moi tim theo SDT.
                int maKhachHang = 0;
                string checkKhSql = @"SELECT TOP 1 MaKhachHang FROM vantai.KhachHang
                                      WHERE (@cccd <> '' AND SoCCCD = @cccd)
                                         OR (@cccd = '' AND @sdt <> '' AND SoDienThoai = @sdt)
                                      ORDER BY MaKhachHang DESC";
                using (var cmdCheckKh = new SqlCommand(checkKhSql, conn, trans))
                {
                    cmdCheckKh.Parameters.AddWithValue("@cccd", khachHang.SoCCCD ?? "");
                    cmdCheckKh.Parameters.AddWithValue("@sdt", khachHang.SoDienThoai ?? "");
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
                string pnrCode = $"PNR{DateTime.Now:yyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
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

                // [13/11] Khoa "ban ve cua chuyen" (Shared) cho moi chuyen trong gio, theo thu tu ma chuyen:
                //         luu doan tau (dong bo toa ban ve) lay Exclusive -> khong ban vao toa dang bi bo / doi
                foreach (int maCt in danhSachVe.Select(v => v.MaChuyenTau).Distinct().OrderBy(m => m))
                    KhoaBanVe(conn, trans, maCt);

                // 3. Dat tung ve qua Stored Procedure vantai.sp_DatVeTheoChang (kiem tra chang giao nhau, UPDLOCK, HOLDLOCK)
                foreach (var ve in danhSachVe)
                {
                    // 3.0. Kiem tra hanh khach (CCCD) da co ve tren chuyen tau nay trong cung chang chua (chong mua trung ve)
                    string checkTrungVeSql = @"
                        WITH UserChang AS (
                            SELECT 
                                IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) AS UserKmMin,
                                IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDen.LyTrinhKm, gDi.LyTrinhKm) AS UserKmMax
                            FROM hatang.Ga gDi, hatang.Ga gDen
                            WHERE gDi.MaGa = @gaDi AND gDen.MaGa = @gaDen
                        )
                        SELECT TOP 1 v.MaVeCode, v.TenHanhKhach, cn.SoGhe, tx.NhanHieuToa
                        FROM vantai.Ve v
                        JOIN hatang.Ga gvDi ON v.MaGaDi = gvDi.MaGa
                        JOIN hatang.Ga gvDen ON v.MaGaDen = gvDen.MaGa
                        JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
                        JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                        CROSS JOIN UserChang uc
                        WHERE v.MaChuyenTau = @chuyenTau
                          AND v.CCCDHanhKhach = @cccd
                          AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
                          AND IIF(gvDi.LyTrinhKm < gvDen.LyTrinhKm, gvDi.LyTrinhKm, gvDen.LyTrinhKm) < uc.UserKmMax
                          AND IIF(gvDi.LyTrinhKm > gvDen.LyTrinhKm, gvDi.LyTrinhKm, gvDen.LyTrinhKm) > uc.UserKmMin";

                    // [15/11 Dat] Khong co CCCD thi bo qua (tranh chan nham 2 ve cung de trong);
                    //             doc xong dong reader roi moi Rollback (reader con mo thi Rollback nem loi tieng Anh).
                    string? veTrungCccd = null;
                    if (!string.IsNullOrWhiteSpace(ve.CCCDHanhKhach))
                    {
                        using var cmdCheckCccd = new SqlCommand(checkTrungVeSql, conn, trans);
                        cmdCheckCccd.Parameters.AddWithValue("@chuyenTau", ve.MaChuyenTau);
                        cmdCheckCccd.Parameters.AddWithValue("@gaDi", ve.MaGaDi);
                        cmdCheckCccd.Parameters.AddWithValue("@gaDen", ve.MaGaDen);
                        cmdCheckCccd.Parameters.AddWithValue("@cccd", ve.CCCDHanhKhach.Trim());

                        using var rCccd = cmdCheckCccd.ExecuteReader();
                        if (rCccd.Read())
                        {
                            string maTrung = rCccd["MaVeCode"]?.ToString() ?? "";
                            string tenTrung = rCccd["TenHanhKhach"]?.ToString() ?? "";
                            string toaTrung = rCccd["NhanHieuToa"]?.ToString() ?? "";
                            string gheTrung = rCccd["SoGhe"]?.ToString() ?? "";
                            veTrungCccd = $"Hành khách '{tenTrung}' (CCCD: {ve.CCCDHanhKhach}) đã có vé [{maTrung}] ({toaTrung}, Ghế {gheTrung}) trên chuyến tàu này, chặng bị trùng với chặng đang mua!";
                        }
                    }
                    if (veTrungCccd != null)
                    {
                        thongBaoLoi = veTrungCccd;
                        trans.Rollback();
                        return false;
                    }

                    using var cmdSP = new SqlCommand("vantai.sp_DatVeTheoChang", conn, trans);
                    cmdSP.CommandType = CommandType.StoredProcedure;
                    cmdSP.Parameters.AddWithValue("@MaDonVe", maDonVe);
                    cmdSP.Parameters.AddWithValue("@MaChuyenTau", ve.MaChuyenTau);
                    cmdSP.Parameters.AddWithValue("@MaChoNgoi", ve.MaChoNgoi);
                    cmdSP.Parameters.AddWithValue("@MaGaDi", ve.MaGaDi);
                    cmdSP.Parameters.AddWithValue("@MaGaDen", ve.MaGaDen);
                    cmdSP.Parameters.AddWithValue("@TenHanhKhach", ve.TenHanhKhach);
                    cmdSP.Parameters.AddWithValue("@CCCDHanhKhach", ve.CCCDHanhKhach);
                    cmdSP.Parameters.AddWithValue("@GiaVeGoc", ve.GiaVeGoc);
                    cmdSP.Parameters.AddWithValue("@SoTienGiam", ve.SoTienGiam);

                    var pCodeOut = cmdSP.Parameters.Add("@MaVeCodeOutput", SqlDbType.VarChar, 30);
                    pCodeOut.Direction = ParameterDirection.InputOutput;
                    pCodeOut.Value = DBNull.Value;

                    cmdSP.ExecuteNonQuery();

                    ve.MaVeCode = pCodeOut.Value?.ToString() ?? "";
                }

                // [12/11] Nhat ky he thong (trang Quan tri > Nhat ky) - cung giao dich voi don ve
                DAL.Connection.PhienGiaoDich.GhiNhatKy(conn, trans, "BAN_VE", "vantai.DonDatVe", maDonVe.ToString(), null, new
                {
                    MaPNR = pnrCode, MaKhachHang = maKhachHang, MacTau = soHieuTau, SoVe = danhSachVe.Count,
                    donVe.TongTien, donVe.HinhThucTT, Ve = danhSachVe.Select(v => v.MaVeCode).ToList()
                });

                trans.Commit();
                maPNROut = pnrCode;
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans.Connection != null)
                    {
                        trans.Rollback();
                    }
                }
                catch
                {
                    // Bo qua neu transaction da bi rollback boi SQL Server
                }
                // [13/11] Loi tu thu tuc (RAISERROR) dung dau; SQL Server noi them loi 266 tieng Anh khi thu tuc ROLLBACK
                thongBaoLoi = ex is SqlException se && se.Errors.Count > 0 ? se.Errors[0].Message : ex.Message;
                return false;
            }
        }

        public bool HoanVe(int maVe, decimal tyLeLePhi, string lyDo, string hinhThucHoan, int maNhanVien, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            string connStr = DatabaseHelper.GetConnectionString();
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // Lay thong tin ve goc va kiem tra dieu kien hoan tra theo quy dinh DSVN
                string getVeSql = @"SELECT v.GiaVeGoc, v.TrangThai, v.MaChuyenTau, v.MaGaDi,
                                           ct.NgayXuatPhat, ct.GioXuatPhatKH, ct.TrangThai AS TrangThaiChuyen,
                                           ld.GioDiKeHoach AS GioDiKH, ld.GioDiThucTe
                                    FROM vantai.Ve v
                                    JOIN vanhanh.ChuyenTau ct ON v.MaChuyenTau = ct.MaChuyenTau
                                    LEFT JOIN vanhanh.LichDungGa ld ON ld.MaChuyenTau = v.MaChuyenTau AND ld.MaGa = v.MaGaDi
                                    WHERE v.MaVe = @id";
                decimal giaGoc = 0;
                string trangThai = "";
                string trangThaiChuyen = "";
                DateTime ngayXuatPhat = DateTime.MinValue;
                TimeSpan gioXuatPhatKH = TimeSpan.Zero;
                TimeSpan? gioDiKH = null;
                object? gioDiThucTeObj = null;

                using (var cmdGet = new SqlCommand(getVeSql, conn, trans))
                {
                    cmdGet.Parameters.AddWithValue("@id", maVe);
                    using var reader = cmdGet.ExecuteReader();
                    if (reader.Read())
                    {
                        giaGoc = Convert.ToDecimal(reader["GiaVeGoc"]);
                        trangThai = reader["TrangThai"].ToString() ?? "";
                        trangThaiChuyen = reader["TrangThaiChuyen"].ToString() ?? "";
                        ngayXuatPhat = Convert.ToDateTime(reader["NgayXuatPhat"]);
                        if (reader["GioXuatPhatKH"] != DBNull.Value)
                        {
                            object val = reader["GioXuatPhatKH"];
                            gioXuatPhatKH = val is DateTime dt ? dt.TimeOfDay : (val is TimeSpan ts ? ts : TimeSpan.Zero);
                        }
                        if (reader["GioDiKH"] != DBNull.Value)
                        {
                            object val = reader["GioDiKH"];
                            gioDiKH = val is DateTime dt ? dt.TimeOfDay : (val is TimeSpan ts ? ts : null);
                        }
                        gioDiThucTeObj = reader["GioDiThucTe"] != DBNull.Value ? reader["GioDiThucTe"] : null;
                    }
                }

                if (trangThai == "DA_HOAN_VE")
                {
                    thongBaoLoi = "Vé này đã làm thủ tục hoàn trả trước đó!";
                    trans.Rollback();
                    return false;
                }

                if (trangThai == "DA_LEN_TAU")
                {
                    thongBaoLoi = "Hành khách đã qua cửa kiểm soát / đã lên tàu, không thể hoàn vé!";
                    trans.Rollback();
                    return false;
                }

                if (trangThai == "DA_HUY")
                {
                    thongBaoLoi = "Vé này đã bị hủy bỏ, không thể làm thủ tục hoàn vé!";
                    trans.Rollback();
                    return false;
                }

                if (trangThaiChuyen == "HOAN_THANH")
                {
                    thongBaoLoi = "Chuyến tàu đã hoàn thành hành trình, không thể hoàn vé!";
                    trans.Rollback();
                    return false;
                }

                if (gioDiThucTeObj != null)
                {
                    thongBaoLoi = "Tàu đã khởi hành rời ga đi của hành khách, vé không còn giá trị hoàn trả!";
                    trans.Rollback();
                    return false;
                }

                // Kiểm tra thời gian: hoàn vé cá nhân phải trước giờ khởi hành ít nhất 4 tiếng (Quy chuẩn ĐSVN)
                TimeSpan gioKhoiHanh = gioDiKH ?? gioXuatPhatKH;
                DateTime thoiDiemKhoiHanh = ngayXuatPhat.Date.Add(gioKhoiHanh);
                DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
                if (thoiDiemKhoiHanh <= now)
                {
                    thongBaoLoi = "Tàu đã quá giờ khởi hành tại ga đi, vé không còn giá trị hoàn trả!";
                    trans.Rollback();
                    return false;
                }

                if ((thoiDiemKhoiHanh - now).TotalHours < 4)
                {
                    thongBaoLoi = "Theo quy định ĐSVN, không được hoàn trả vé trước giờ tàu chạy dưới 4 tiếng!";
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

                // [12/11] Nhat ky he thong - cung giao dich
                DAL.Connection.PhienGiaoDich.GhiNhatKy(conn, trans, "HOAN_VE", "vantai.Ve", maVe.ToString(),
                    new { TrangThai = trangThai },
                    new { TrangThai = "DA_HOAN_VE", TienVeGoc = giaGoc, TyLeLePhi = tyLeLePhi, HinhThucHoan = hinhThucHoan, LyDo = lyDo });

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

        public DataTable LayDanhSachVeTheoCCCD(string cccd)
        {
            DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
            string sql = @"SELECT v.MaVe, v.MaVeCode, mt.SoHieuMacTau, 
                                  dv.MaPNR, dv.MaDonVe, DATEADD(HOUR, 7, dv.ThoiDiemTao) AS ThoiDiemTaoDon, dv.HinhThucTT, dv.TrangThaiTT,
                                  kh.HoTen AS TenNguoiMua, kh.SoDienThoai AS SdtNguoiMua, kh.SoCCCD AS CccdNguoiMua,
                                  g1.TenGa AS TenGaDi, g2.TenGa AS TenGaDen,
                                  v.TenHanhKhach, v.CCCDHanhKhach, cn.SoGhe, cn.TangGiuong, cn.MaChoNgoi,
                                  tx.LoaiToa, tx.NhanHieuToa, tx.MaToaXeKhach,
                                  v.GiaVeGoc, v.SoTienGiam, v.GiaVeThucThu,
                                  CASE 
                                      WHEN v.TrangThai = 'DA_DAT' AND (
                                          ct.TrangThai IN ('HOAN_THANH', 'DA_HUY') 
                                          OR CAST(ct.NgayXuatPhat AS DATE) < CAST(@now AS DATE)
                                          OR (ct.TrangThai = 'DANG_CHAY' AND (
                                              EXISTS (
                                                  SELECT 1 FROM vanhanh.LichDungGa ld 
                                                  WHERE ld.MaChuyenTau = ct.MaChuyenTau 
                                                    AND ld.MaGa = v.MaGaDi 
                                                    AND (ld.GioDiThucTe IS NOT NULL OR ld.GioDiKeHoach < @now)
                                              )
                                          ))
                                      ) THEN 'HET_HAN'
                                      ELSE v.TrangThai
                                  END AS TrangThai,
                                  v.TrangThai AS TrangThaiGoc,
                                  DATEADD(HOUR, 7, v.ThoiDiemXuatVe) AS ThoiDiemXuatVe,
                                  v.MaChuyenTau, v.MaGaDi, v.MaGaDen,
                                  ct.NgayXuatPhat, ct.GioXuatPhatKH,
                                  DATEADD(HOUR, 7, hh.ThoiDiemHoan) AS ThoiDiemHoan, hh.TyLeLePhi, hh.LePhiHoan, hh.SoTienThucHoan, hh.HinhThucHoan, hh.LyDoHoan
                           FROM vantai.Ve v
                           JOIN vantai.DonDatVe dv ON v.MaDonVe = dv.MaDonVe
                           LEFT JOIN vantai.KhachHang kh ON dv.MaKhachHang = kh.MaKhachHang
                           JOIN vanhanh.ChuyenTau ct ON v.MaChuyenTau = ct.MaChuyenTau
                           JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                           JOIN hatang.Ga g1 ON v.MaGaDi = g1.MaGa
                           JOIN hatang.Ga g2 ON v.MaGaDen = g2.MaGa
                           JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
                           JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                           LEFT JOIN vantai.HoanHuyVe hh ON v.MaVe = hh.MaVe
                           WHERE v.CCCDHanhKhach = @cccd OR kh.SoCCCD = @cccd
                           ORDER BY v.ThoiDiemXuatVe DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[] {
                new SqlParameter("@cccd", cccd),
                new SqlParameter("@now", now)
            });
        }

        /// <summary>
        /// Gia ve moi khi doi ghe: giu nguyen ty le giam cua ve cu (tre em, sinh vien...).
        /// Hop thoai doi ghe va giao dich ghi dung chung ham nay -> so bao khach = so ghi CSDL.
        /// </summary>
        public static (decimal GiamMoi, decimal ThucThuMoi, decimal ChenhLech) TinhGiaDoiGhe(
            decimal giaGocCu, decimal giamCu, decimal giaGocMoi)
        {
            decimal tyLeGiam = giaGocCu > 0 ? giamCu / giaGocCu : 0m;
            decimal giamMoi = Math.Round(giaGocMoi * tyLeGiam, 0);
            decimal thucThuMoi = giaGocMoi - giamMoi;
            return (giamMoi, thucThuMoi, thucThuMoi - (giaGocCu - giamCu));
        }

        // [13/11 Dat] Khoa ung dung "vantai.BanVe:<ma chuyen>" muc Shared, giu toi het giao dich.
        // sp_DatVeTheoChang cung lay (lay lai trong cung giao dich khong sao); LapTauService lay Exclusive khi dong bo toa ban ve.
        private static void KhoaBanVe(SqlConnection conn, SqlTransaction trans, int maChuyenTau)
        {
            using var cmd = new SqlCommand("sp_getapplock", conn, trans) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = DAL.Connection.KhoaSuaChuyen.TenKhoaBanVe(maChuyenTau) });
            cmd.Parameters.Add(new SqlParameter("@LockMode", SqlDbType.VarChar, 32) { Value = "Shared" });
            cmd.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.VarChar, 32) { Value = "Transaction" });
            cmd.Parameters.Add(new SqlParameter("@LockTimeout", SqlDbType.Int) { Value = 10000 });
            var kq = cmd.Parameters.Add(new SqlParameter("@KetQua", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue });
            cmd.ExecuteNonQuery();
            if (kq.Value is int k && k < 0)
                throw new InvalidOperationException("Đoàn tàu của chuyến đang được cập nhật, chưa đặt được vé. Vui lòng thử lại sau ít giây.");
        }

        public bool DoiGheCungChuyen(int maVeCu, int maChoNgoiMoi, decimal giaVeGocMoi, int maNhanVien, out string maVeCodeMoi, out string thongBaoLoi)
        {
            maVeCodeMoi = string.Empty;
            thongBaoLoi = string.Empty;

            string connStr = DatabaseHelper.GetConnectionString();
            using var conn = new SqlConnection(connStr);
            conn.Open();
            using var trans = conn.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                // 1. Doc thong tin ve cu
                string queryVeCu = @"SELECT MaDonVe, MaChuyenTau, MaGaDi, MaGaDen, 
                                            TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai
                                     FROM vantai.Ve WHERE MaVe = @id";
                int maDonVe = 0, maChuyenTau = 0, maGaDi = 0, maGaDen = 0;
                string tenHk = "", cccdHk = "", trangThai = "";
                decimal giaGoc = 0, giamGia = 0;

                using (var cmd = new SqlCommand(queryVeCu, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", maVeCu);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read())
                    {
                        thongBaoLoi = "Không tìm thấy vé cần đổi!";
                        trans.Rollback();
                        return false;
                    }
                    trangThai = r["TrangThai"].ToString() ?? "";
                    if (trangThai != "DA_DAT")
                    {
                        thongBaoLoi = $"Vé đang ở trạng thái '{trangThai}', không thể thực hiện đổi ghế!";
                        trans.Rollback();
                        return false;
                    }
                    maDonVe = Convert.ToInt32(r["MaDonVe"]);
                    maChuyenTau = Convert.ToInt32(r["MaChuyenTau"]);
                    maGaDi = Convert.ToInt32(r["MaGaDi"]);
                    maGaDen = Convert.ToInt32(r["MaGaDen"]);
                    tenHk = r["TenHanhKhach"].ToString() ?? "";
                    cccdHk = r["CCCDHanhKhach"].ToString() ?? "";
                    giaGoc = Convert.ToDecimal(r["GiaVeGoc"]);
                    giamGia = Convert.ToDecimal(r["SoTienGiam"]);
                }

                // [13/11] Khoa "ban ve cua chuyen" (Shared) truoc khi giai phong ghe cu (xem LuuDonDatVeTransaction)
                KhoaBanVe(conn, trans, maChuyenTau);

                // Tinh toan gia ve moi va chenh lech tai chinh (cung ham voi hop thoai doi ghe)
                var (giamGiaMoi, _, chenhLech) = TinhGiaDoiGhe(giaGoc, giamGia, giaVeGocMoi);

                // 2. Chuyen trang thai ve cu sang DA_HUY de giai phong ghe cu
                string updateCu = "UPDATE vantai.Ve SET TrangThai = 'DA_HUY' WHERE MaVe = @id";
                using (var cmdUp = new SqlCommand(updateCu, conn, trans))
                {
                    cmdUp.Parameters.AddWithValue("@id", maVeCu);
                    cmdUp.ExecuteNonQuery();
                }

                // 3. Dat ve tren ghe moi thong qua sp_DatVeTheoChang (khoa va kiem tra chặng giao nhau)
                using (var cmdSP = new SqlCommand("vantai.sp_DatVeTheoChang", conn, trans))
                {
                    cmdSP.CommandType = CommandType.StoredProcedure;
                    cmdSP.Parameters.AddWithValue("@MaDonVe", maDonVe);
                    cmdSP.Parameters.AddWithValue("@MaChuyenTau", maChuyenTau);
                    cmdSP.Parameters.AddWithValue("@MaChoNgoi", maChoNgoiMoi);
                    cmdSP.Parameters.AddWithValue("@MaGaDi", maGaDi);
                    cmdSP.Parameters.AddWithValue("@MaGaDen", maGaDen);
                    cmdSP.Parameters.AddWithValue("@TenHanhKhach", tenHk);
                    cmdSP.Parameters.AddWithValue("@CCCDHanhKhach", cccdHk);
                    cmdSP.Parameters.AddWithValue("@GiaVeGoc", giaVeGocMoi);
                    cmdSP.Parameters.AddWithValue("@SoTienGiam", giamGiaMoi);

                    var pOut = cmdSP.Parameters.Add("@MaVeCodeOutput", SqlDbType.VarChar, 30);
                    pOut.Direction = ParameterDirection.InputOutput;
                    pOut.Value = DBNull.Value;

                    cmdSP.ExecuteNonQuery();
                    maVeCodeMoi = pOut.Value?.ToString() ?? "";
                }

                // 4. Cap nhat chenh lech tai chinh vao DonDatVe
                if (chenhLech != 0)
                {
                    string updateDon = @"UPDATE vantai.DonDatVe 
                                         SET TongTien = CASE WHEN TongTien + @diff < 0 THEN 0 ELSE TongTien + @diff END 
                                         WHERE MaDonVe = @maDonVe";
                    using (var cmdDon = new SqlCommand(updateDon, conn, trans))
                    {
                        cmdDon.Parameters.AddWithValue("@diff", chenhLech);
                        cmdDon.Parameters.AddWithValue("@maDonVe", maDonVe);
                        cmdDon.ExecuteNonQuery();
                    }
                }

                // [12/11] Nhat ky he thong - cung giao dich
                DAL.Connection.PhienGiaoDich.GhiNhatKy(conn, trans, "DOI_GHE", "vantai.Ve", maVeCu.ToString(),
                    new { TrangThai = trangThai, GiaVeGoc = giaGoc, SoTienGiam = giamGia },
                    new { VeMoi = maVeCodeMoi, MaChoNgoi = maChoNgoiMoi, GiaVeGoc = giaVeGocMoi, SoTienGiam = giamGiaMoi, ChenhLech = chenhLech });

                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    if (trans.Connection != null) trans.Rollback();
                }
                catch { }
                // [13/11] Loi tu thu tuc (RAISERROR) dung dau; SQL Server noi them loi 266 tieng Anh khi thu tuc ROLLBACK
                thongBaoLoi = ex is SqlException se && se.Errors.Count > 0 ? se.Errors[0].Message : ex.Message;
                return false;
            }
        }

        #region PHƯƠNG THỨC KIỂM SOÁT VÉ & SOÁT VÉ LÊN TÀU

        public bool CapNhatTrangThaiSoatVe(int maVe, string trangThai, int? maGaSoat, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            try
            {
                DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
                string sql = @"UPDATE vantai.Ve 
                               SET TrangThai = @trangThai,
                                   ThoiDiemSoatVe = CASE WHEN @trangThai = 'DA_LEN_TAU' THEN @thoiDiemSoat ELSE NULL END,
                                   MaGaSoat = CASE WHEN @trangThai = 'DA_LEN_TAU' THEN @maGaSoat ELSE NULL END
                               OUTPUT DELETED.TrangThai, DELETED.MaVeCode
                               WHERE MaVe = @id
                                 AND (
                                     (@trangThai = 'DA_LEN_TAU' AND TrangThai = 'DA_DAT')
                                     OR (@trangThai = 'DA_DAT' AND TrangThai = 'DA_LEN_TAU')
                                 )";
                // [12/11] Cap nhat + nhat ky he thong trong mot giao dich
                using var conn = new SqlConnection(DatabaseHelper.GetConnectionString());
                conn.Open();
                using var trans = conn.BeginTransaction();
                string? cu = null, maVeCode = null;
                using (var cmd = new SqlCommand(sql, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@id", maVe);
                    cmd.Parameters.AddWithValue("@trangThai", trangThai);
                    cmd.Parameters.AddWithValue("@thoiDiemSoat", now);
                    cmd.Parameters.AddWithValue("@maGaSoat", (object?)maGaSoat ?? DBNull.Value);
                    using var r = cmd.ExecuteReader();
                    if (r.Read()) { cu = r["TrangThai"].ToString(); maVeCode = r["MaVeCode"].ToString(); }
                }
                if (cu == null)
                {
                    trans.Rollback();
                    thongBaoLoi = "Không thể cập nhật: Trạng thái vé không hợp lệ để soát vé hoặc vé đã bị hủy/hoàn trả!";
                    return false;
                }
                DAL.Connection.PhienGiaoDich.GhiNhatKy(conn, trans, trangThai == "DA_LEN_TAU" ? "SOAT_VE" : "HUY_SOAT_VE", "vantai.Ve",
                    maVe.ToString(), new { MaVeCode = maVeCode, TrangThai = cu }, new { MaVeCode = maVeCode, TrangThai = trangThai, MaGaSoat = maGaSoat });
                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                thongBaoLoi = ex.Message;
                return false;
            }
        }

        public bool SoatVeTheoLo(int maChuyenTau, int maGaSoat, out int soVeDaSoat, out string thongBaoLoi)
        {
            soVeDaSoat = 0;
            thongBaoLoi = string.Empty;
            try
            {
                DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
                string sql = @"UPDATE vantai.Ve 
                               SET TrangThai = 'DA_LEN_TAU',
                                   ThoiDiemSoatVe = @thoiDiemSoat,
                                   MaGaSoat = @maGaSoat
                               WHERE MaChuyenTau = @maChuyenTau
                                 AND MaGaDi = @maGaSoat
                                 AND TrangThai = 'DA_DAT'";
                // [12/11] Cap nhat + nhat ky he thong trong mot giao dich
                using var conn = new SqlConnection(DatabaseHelper.GetConnectionString());
                conn.Open();
                using var trans = conn.BeginTransaction();
                using (var cmd = new SqlCommand(sql, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@maChuyenTau", maChuyenTau);
                    cmd.Parameters.AddWithValue("@maGaSoat", maGaSoat);
                    cmd.Parameters.AddWithValue("@thoiDiemSoat", now);
                    soVeDaSoat = cmd.ExecuteNonQuery();
                }
                if (soVeDaSoat > 0)
                    DAL.Connection.PhienGiaoDich.GhiNhatKy(conn, trans, "SOAT_VE_THEO_LO", "vanhanh.ChuyenTau", maChuyenTau.ToString(),
                        null, new { MaGaSoat = maGaSoat, SoVeDaSoat = soVeDaSoat });
                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                thongBaoLoi = ex.Message;
                return false;
            }
        }

        public DataTable LayManifestChuyenTau(int maChuyenTau, string? trangThaiLoc = null)
        {
            string filterTrangThai = string.IsNullOrWhiteSpace(trangThaiLoc) || trangThaiLoc == "ALL"
                ? ""
                : "AND v.TrangThai = @trangThaiLoc";

            string sql = $@"
                SELECT v.MaVe, v.MaVeCode, v.TenHanhKhach, v.CCCDHanhKhach,
                       tx.LoaiToa, tx.NhanHieuToa, tx.ThuTuToa AS ThuTuNoiToa,
                       cn.SoGhe, cn.TangGiuong, tx.LoaiToa AS LoaiCho,
                       gDi.TenGa AS TenGaDi, gDen.TenGa AS TenGaDen,
                       gDi.LyTrinhKm AS KmDi, gDen.LyTrinhKm AS KmDen,
                       v.GiaVeThucThu, v.TrangThai, v.ThoiDiemXuatVe,
                       v.ThoiDiemSoatVe, gSoat.TenGa AS TenGaSoat,
                       v.MaGaDi, v.MaGaDen, v.MaChoNgoi, tx.MaToaXeKhach,
                       v.MaChuyenTau, mt.SoHieuMacTau, ct.NgayXuatPhat, ct.GioXuatPhatKH,
                       ct.TrangThai AS TrangThaiChuyenTau,
                       ISNULL(ct.SoPhutTreLuyKe, 0) AS SoPhutTreLuyKe,
                       ldDi.GioDiKeHoach AS GioDiKH,
                       ldDi.GioDiThucTe
                FROM vantai.Ve v
                JOIN vanhanh.ChuyenTau ct ON v.MaChuyenTau = ct.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
                JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                JOIN hatang.Ga gDi ON v.MaGaDi = gDi.MaGa
                JOIN hatang.Ga gDen ON v.MaGaDen = gDen.MaGa
                LEFT JOIN hatang.Ga gSoat ON v.MaGaSoat = gSoat.MaGa
                LEFT JOIN vanhanh.LichDungGa ldDi ON ldDi.MaChuyenTau = v.MaChuyenTau AND ldDi.MaGa = v.MaGaDi
                WHERE v.MaChuyenTau = @maChuyenTau
                  {filterTrangThai}
                ORDER BY tx.ThuTuToa ASC, cn.SoGhe ASC";

            var parameters = new List<SqlParameter> { new SqlParameter("@maChuyenTau", maChuyenTau) };
            if (!string.IsNullOrWhiteSpace(filterTrangThai))
            {
                parameters.Add(new SqlParameter("@trangThaiLoc", trangThaiLoc));
            }
            return DatabaseHelper.ExecuteQuery(sql, parameters.ToArray());
        }

        public DataTable TimVeSoat(int? maChuyenTau, string keyword)
        {
            string filterChuyen = maChuyenTau.HasValue && maChuyenTau.Value > 0
                ? "AND v.MaChuyenTau = @maChuyenTau"
                : "";

            string sql = $@"
                SELECT v.MaVe, v.MaVeCode, v.TenHanhKhach, v.CCCDHanhKhach,
                       tx.LoaiToa, tx.NhanHieuToa, tx.ThuTuToa AS ThuTuNoiToa,
                       cn.SoGhe, cn.TangGiuong, tx.LoaiToa AS LoaiCho,
                       gDi.TenGa AS TenGaDi, gDen.TenGa AS TenGaDen,
                       gDi.LyTrinhKm AS KmDi, gDen.LyTrinhKm AS KmDen,
                       v.GiaVeThucThu, v.TrangThai, v.ThoiDiemXuatVe,
                       v.ThoiDiemSoatVe, gSoat.TenGa AS TenGaSoat,
                       v.MaGaDi, v.MaGaDen, v.MaChoNgoi, tx.MaToaXeKhach,
                       v.MaChuyenTau, mt.SoHieuMacTau, mt.LoaiTau,
                       ct.NgayXuatPhat, ct.GioXuatPhatKH, ct.TrangThai AS TrangThaiChuyenTau,
                       ISNULL(ct.SoPhutTreLuyKe, 0) AS SoPhutTreLuyKe,
                       ldDi.GioDiKeHoach AS GioDiKH, ldDi.GioDiThucTe,
                       ldDen.GioDenKeHoach AS GioDenKH, ldDen.GioDenThucTe
                FROM vantai.Ve v
                JOIN vanhanh.ChuyenTau ct ON v.MaChuyenTau = ct.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
                JOIN vantai.ToaXeKhach tx ON cn.MaToaXeKhach = tx.MaToaXeKhach
                JOIN hatang.Ga gDi ON v.MaGaDi = gDi.MaGa
                JOIN hatang.Ga gDen ON v.MaGaDen = gDen.MaGa
                LEFT JOIN hatang.Ga gSoat ON v.MaGaSoat = gSoat.MaGa
                LEFT JOIN vanhanh.LichDungGa ldDi ON ldDi.MaChuyenTau = v.MaChuyenTau AND ldDi.MaGa = v.MaGaDi
                LEFT JOIN vanhanh.LichDungGa ldDen ON ldDen.MaChuyenTau = v.MaChuyenTau AND ldDen.MaGa = v.MaGaDen
                WHERE (v.MaVeCode = @kw 
                       OR v.CCCDHanhKhach = @kw 
                       OR v.TenHanhKhach LIKE @kwLike
                       OR CAST(v.MaVe AS VARCHAR) = @kw)
                  {filterChuyen}
                ORDER BY v.ThoiDiemXuatVe DESC";

            var pList = new List<SqlParameter>
            {
                new SqlParameter("@kw", keyword.Trim()),
                new SqlParameter("@kwLike", $"%{keyword.Trim()}%")
            };
            if (maChuyenTau.HasValue && maChuyenTau.Value > 0)
            {
                pList.Add(new SqlParameter("@maChuyenTau", maChuyenTau.Value));
            }
            return DatabaseHelper.ExecuteQuery(sql, pList.ToArray());
        }

        public DataTable LayThongKeSoatVe(int maChuyenTau, int maGaHienTai)
        {
            string sql = @"
                SELECT 
                    COUNT(*) AS TongVe,
                    SUM(CASE WHEN v.TrangThai = 'DA_LEN_TAU' THEN 1 ELSE 0 END) AS DaLenTau,
                    SUM(CASE WHEN v.TrangThai = 'DA_DAT' THEN 1 ELSE 0 END) AS ChuaLenTau,
                    SUM(CASE WHEN v.TrangThai IN ('DA_HUY', 'DA_HOAN_VE') THEN 1 ELSE 0 END) AS DaHuyHoan,
                    SUM(CASE WHEN v.MaGaDi = @maGa THEN 1 ELSE 0 END) AS DonTaiGaTong,
                    SUM(CASE WHEN v.MaGaDi = @maGa AND v.TrangThai = 'DA_LEN_TAU' THEN 1 ELSE 0 END) AS DonTaiGaDaLen,
                    SUM(CASE WHEN v.MaGaDi = @maGa AND v.TrangThai = 'DA_DAT' THEN 1 ELSE 0 END) AS DonTaiGaChuaLen,
                    SUM(CASE WHEN v.MaGaDen = @maGa AND v.TrangThai = 'DA_LEN_TAU' THEN 1 ELSE 0 END) AS TraTaiGa
                FROM vantai.Ve v
                WHERE v.MaChuyenTau = @maChuyenTau";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@maChuyenTau", maChuyenTau),
                new SqlParameter("@maGa", maGaHienTai)
            });
        }

        public DataTable LayDanhSachChuyenTauHomNay(DateTime ngay)
        {
            string sql = @"
                SELECT ct.MaChuyenTau, mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay,
                       ct.NgayXuatPhat, ct.GioXuatPhatKH, ct.GioVeDichKH, ct.TrangThai,
                       gDi.TenGa AS TenGaXuatPhat, gDen.TenGa AS TenGaKetThuc,
                       CONCAT(mt.SoHieuMacTau, ' (', FORMAT(ct.GioXuatPhatKH, 'HH:mm'), ' ', gDi.TenGa, ' -> ', gDen.TenGa, ')') AS TenHienThi
                FROM vanhanh.ChuyenTau ct
                JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                JOIN hatang.Ga gDi ON mt.MaGaDi = gDi.MaGa
                JOIN hatang.Ga gDen ON mt.MaGaDen = gDen.MaGa
                WHERE CAST(ct.NgayXuatPhat AS DATE) = CAST(@ngay AS DATE)
                ORDER BY ct.GioXuatPhatKH ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@ngay", ngay.Date) });
        }

        #endregion
    }
}
