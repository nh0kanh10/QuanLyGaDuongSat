using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;

namespace DAL.Repositories
{
    public class DieuDoRepository
    {
        public DataTable LayDuLieuBieuDo(DateTime ngay)
        {
            string sql = @"
                SELECT 
                    ct.MaChuyenTau, ct.NgayXuatPhat, ct.GioXuatPhatKH, ct.GioVeDichKH, ct.TrangThai,
                    mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay, mt.MucUuTien,
                    gDi.TenGa AS TenGaDi, gDen.TenGa AS TenGaDen,
                    ISNULL(dt.TongChieuDaiM, 0.0) AS TongChieuDaiM,
                    ld.MaDiemDung, ld.ThuTuDung, ld.MaGa, ld.GioDenKeHoach, ld.GioDiKeHoach, 
                    ld.LaDiemTranh, ld.MaDuongRay,
                    g.TenGa, g.MaGaCode, g.LyTrinhKm,
                    dr.SoHieuDuong AS SoHieuDuongRay, dr.LoaiDuong
                FROM vanhanh.ChuyenTau ct
                JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                JOIN hatang.Ga gDi ON mt.MaGaDi = gDi.MaGa
                JOIN hatang.Ga gDen ON mt.MaGaDen = gDen.MaGa
                LEFT JOIN vanhanh.DoanTau dt ON ct.MaChuyenTau = dt.MaChuyenTau
                JOIN vanhanh.LichDungGa ld ON ct.MaChuyenTau = ld.MaChuyenTau
                JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                LEFT JOIN hatang.DuongRayGa dr ON ld.MaDuongRay = dr.MaDuongRay
                WHERE ct.GioXuatPhatKH < DATEADD(DAY, 1, @ngay)
                  AND ct.GioVeDichKH >= @ngay
                  AND ct.TrangThai <> 'DA_HUY'
                ORDER BY ct.MaChuyenTau, ld.ThuTuDung ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@ngay", ngay.Date) });
        }

        public DateTime LayNgayBieuDoMacDinh()
        {
            // 1. Kiểm tra xem ngày hôm nay có chuyến tàu hoạt động không
            string sqlCheckToday = @"
                SELECT COUNT(1) 
                FROM vanhanh.ChuyenTau ct
                WHERE (CAST(ct.NgayXuatPhat AS DATE) = CAST(GETDATE() AS DATE)
                       OR CAST(GETDATE() AS DATE) BETWEEN CAST(ct.GioXuatPhatKH AS DATE) AND CAST(ct.GioVeDichKH AS DATE))
                  AND ct.TrangThai <> 'DA_HUY'";
            object? count = DatabaseHelper.ExecuteScalar(sqlCheckToday);
            if (count != null && Convert.ToInt32(count) > 0)
            {
                return DateTime.Today;
            }

            // 2. Nếu hôm nay không có tàu, tự động lấy ngày gần nhất trong CSDL có chuyến tàu khai thác
            string sqlNearest = @"
                SELECT TOP 1 CAST(ct.NgayXuatPhat AS DATE)
                FROM vanhanh.ChuyenTau ct
                WHERE ct.TrangThai <> 'DA_HUY'
                ORDER BY ABS(DATEDIFF(day, ct.NgayXuatPhat, GETDATE())) ASC";
            object? obj = DatabaseHelper.ExecuteScalar(sqlNearest);
            if (obj != null && obj != DBNull.Value)
            {
                return Convert.ToDateTime(obj);
            }

            return DateTime.Today;
        }

        public DataTable LayDanhSachNgayCoTau()
        {
            string sql = @"
                SELECT TOP 4 
                    CAST(ct.NgayXuatPhat AS DATE) AS Ngay,
                    COUNT(ct.MaChuyenTau) AS SoChuyenTau
                FROM vanhanh.ChuyenTau ct
                WHERE ct.TrangThai <> 'DA_HUY'
                GROUP BY CAST(ct.NgayXuatPhat AS DATE)
                ORDER BY Ngay DESC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable LayDanhSachGaTuyen()
        {
            string sql = @"SELECT g.MaGa, g.MaGaCode, g.TenGa, g.LyTrinhKm, g.HangGa, g.CoCauQuay
                           FROM hatang.Ga g
                           WHERE g.DangKhaiThac = 1
                           ORDER BY g.LyTrinhKm ASC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable LayDanhSachKhuGian()
        {
            string sql = @"
                SELECT kg.MaKhuGian, kg.MaGaDau, kg.MaGaCuoi, kg.CuLyKm, kg.TocDoToiDaKhach, kg.TocDoToiDaHang,
                       g1.TenGa AS TenGaDau, g1.MaGaCode AS MaGaDauCode, g1.LyTrinhKm AS LyTrinhDauKm,
                       g2.TenGa AS TenGaCuoi, g2.MaGaCode AS MaGaCuoiCode, g2.LyTrinhKm AS LyTrinhCuoiKm
                FROM hatang.KhuGian kg
                JOIN hatang.Ga g1 ON kg.MaGaDau = g1.MaGa
                JOIN hatang.Ga g2 ON kg.MaGaCuoi = g2.MaGa
                ORDER BY g1.LyTrinhKm ASC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable LayTatCaGaCoDuongTranh(decimal chieuDaiM)
        {
            string sql = @"
                SELECT g.MaGa, g.MaGaCode, g.TenGa, g.LyTrinhKm,
                       dr.MaDuongRay, dr.SoHieuDuong, dr.ChieuDaiHuuDungM
                FROM hatang.Ga g
                JOIN hatang.DuongRayGa dr ON g.MaGa = dr.MaGa
                WHERE g.DangKhaiThac = 1
                  AND dr.LoaiDuong = 'DUONG_TRANH'
                  AND dr.ChieuDaiHuuDungM >= @chieuDai
                ORDER BY g.LyTrinhKm ASC, dr.ChieuDaiHuuDungM ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@chieuDai", chieuDaiM) });
        }

        public DataTable LayDuongTranhKhaDung(int maGa, decimal chieuDaiM, DateTime? gioDen = null, DateTime? gioDi = null, int? excludeMaChuyen = null)
        {
            string sql = @"
                SELECT TOP 1 dr.MaDuongRay, dr.SoHieuDuong, dr.ChieuDaiHuuDungM, dr.LoaiDuong
                FROM hatang.DuongRayGa dr
                WHERE dr.MaGa = @maGa
                  AND dr.LoaiDuong = 'DUONG_TRANH'
                  AND dr.TrangThai <> 'BAO_TRI'
                  AND dr.ChieuDaiHuuDungM >= @chieuDai
                  AND (@gioDen IS NULL OR @gioDi IS NULL OR NOT EXISTS (
                      -- Kiểm tra trùng lặp thời gian chiếm dụng với bất kỳ chuyến tàu nào khác
                      SELECT 1 
                      FROM vanhanh.LichDungGa ld
                      JOIN vanhanh.ChuyenTau ct ON ld.MaChuyenTau = ct.MaChuyenTau
                      WHERE ld.MaDuongRay = dr.MaDuongRay
                        AND ct.TrangThai <> 'DA_HUY'
                        AND (@excludeCt IS NULL OR ld.MaChuyenTau <> @excludeCt)
                        AND ld.GioDenKeHoach < @gioDi
                        AND ld.GioDiKeHoach > @gioDen
                  ))
                ORDER BY dr.ChieuDaiHuuDungM ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@maGa", maGa),
                new SqlParameter("@chieuDai", chieuDaiM),
                new SqlParameter("@gioDen", (object?)gioDen ?? DBNull.Value),
                new SqlParameter("@gioDi", (object?)gioDi ?? DBNull.Value),
                new SqlParameter("@excludeCt", (object?)excludeMaChuyen ?? DBNull.Value)
            });
        }

        public bool CapNhatLichTranhTau(int maChuyenTau, int maGa, DateTime gioDenMoi, DateTime gioDiMoi, int? maDuongRay, int soPhutTang, string lyDo)
        {
            string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                BEGIN TRY
                    -- Khóa độc quyền chuyến tàu đang cập nhật để chống xung đột điều độ đồng thời
                    IF NOT EXISTS (SELECT 1 FROM vanhanh.ChuyenTau WITH (UPDLOCK, ROWLOCK) WHERE MaChuyenTau = @ct AND TrangThai <> 'DA_HUY')
                    BEGIN
                        RAISERROR(N'Chuyến tàu không tồn tại hoặc đã bị hủy.', 16, 1);
                        ROLLBACK TRANSACTION;
                        RETURN;
                    END

                    DECLARE @thuTuHienTai INT = NULL;
                    SELECT @thuTuHienTai = ThuTuDung 
                    FROM vanhanh.LichDungGa 
                    WHERE MaChuyenTau = @ct AND MaGa = @ga;

                    -- Tính số phút tịnh tiến thực tế dựa trên độ chênh lệch giữa giờ đi mới và giờ đi cũ
                    DECLARE @phutTangThucTe INT = @soPhutTang;

                    IF @thuTuHienTai IS NOT NULL
                    BEGIN
                        DECLARE @gioDiCu DATETIME = NULL;
                        SELECT @gioDiCu = GioDiKeHoach 
                        FROM vanhanh.LichDungGa 
                        WHERE MaChuyenTau = @ct AND ThuTuDung = @thuTuHienTai;

                        IF @gioDiCu IS NOT NULL
                        BEGIN
                            DECLARE @diff INT = DATEDIFF(MINUTE, @gioDiCu, @gioDi);
                            IF @diff > @phutTangThucTe SET @phutTangThucTe = @diff;
                            IF @phutTangThucTe < 0 SET @phutTangThucTe = 0;
                        END
                    END

                    IF @thuTuHienTai IS NOT NULL
                    BEGIN
                        -- Ga này đã có sẵn trong lịch -> Cập nhật đồng bộ tại ga dừng tránh VÀ tất cả các ga phía sau trong CÙNG MỘT LỆNH UPDATE NGUYÊN TỬ
                        -- Giữ nguyên khoảng cách chạy giữa các ga (running time), trigger trg_LichDungGa_CheckThoiGian kiểm tra sau lệnh UPDATE sẽ thỏa mãn 100%
                        UPDATE vanhanh.LichDungGa
                        SET GioDenKeHoach = CASE 
                                WHEN ThuTuDung = @thuTuHienTai THEN @gioDen
                                ELSE DATEADD(MINUTE, @phutTangThucTe, GioDenKeHoach)
                            END,
                            GioDiKeHoach = CASE 
                                WHEN ThuTuDung = @thuTuHienTai THEN @gioDi
                                ELSE DATEADD(MINUTE, @phutTangThucTe, GioDiKeHoach)
                            END,
                            LaDiemTranh = CASE 
                                WHEN ThuTuDung = @thuTuHienTai THEN 1 
                                ELSE LaDiemTranh 
                            END,
                            MaDuongRay = CASE 
                                WHEN ThuTuDung = @thuTuHienTai THEN ISNULL(@ray, MaDuongRay) 
                                ELSE MaDuongRay 
                            END
                        WHERE MaChuyenTau = @ct AND ThuTuDung >= @thuTuHienTai;
                    END
                    ELSE
                    BEGIN
                        -- Ga này chưa có trong lịch (ga kỹ thuật/ga đường tránh mới trên tuyến) -> Chèn mới vào đúng thứ tự lý trình
                        DECLARE @lyTrinhGa DECIMAL(7,2);
                        SELECT @lyTrinhGa = LyTrinhKm FROM hatang.Ga WHERE MaGa = @ga;

                        DECLARE @huongChay VARCHAR(20);
                        SELECT @huongChay = mt.HuongChay 
                        FROM vanhanh.ChuyenTau ct 
                        JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau 
                        WHERE ct.MaChuyenTau = @ct;

                        IF @huongChay = 'BAC_NAM'
                        BEGIN
                            SELECT TOP 1 @thuTuHienTai = ld.ThuTuDung + 1
                            FROM vanhanh.LichDungGa ld
                            JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                            WHERE ld.MaChuyenTau = @ct AND g.LyTrinhKm < @lyTrinhGa
                            ORDER BY g.LyTrinhKm DESC;
                        END
                        ELSE
                        BEGIN
                            SELECT TOP 1 @thuTuHienTai = ld.ThuTuDung + 1
                            FROM vanhanh.LichDungGa ld
                            JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                            WHERE ld.MaChuyenTau = @ct AND g.LyTrinhKm > @lyTrinhGa
                            ORDER BY g.LyTrinhKm ASC;
                        END

                        IF @thuTuHienTai IS NULL SET @thuTuHienTai = 1;

                        -- 1. Tịnh tiến giờ các ga sau và tăng thứ tự +1
                        IF @phutTangThucTe > 0
                        BEGIN
                            UPDATE vanhanh.LichDungGa
                            SET GioDenKeHoach = DATEADD(MINUTE, @phutTangThucTe, GioDenKeHoach),
                                GioDiKeHoach = DATEADD(MINUTE, @phutTangThucTe, GioDiKeHoach),
                                ThuTuDung = ThuTuDung + 1
                            WHERE MaChuyenTau = @ct AND ThuTuDung >= @thuTuHienTai;
                        END
                        ELSE
                        BEGIN
                            UPDATE vanhanh.LichDungGa
                            SET ThuTuDung = ThuTuDung + 1
                            WHERE MaChuyenTau = @ct AND ThuTuDung >= @thuTuHienTai;
                        END

                        -- 2. Chèn điểm dừng tránh mới
                        INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, LaDiemTranh, MaDuongRay)
                        VALUES (@ct, @ga, @thuTuHienTai, @gioDen, @gioDi, 1, @ray);
                    END

                    -- Cập nhật giờ về đích và số phút trễ lũy kế của chuyến tàu
                    IF @phutTangThucTe > 0
                    BEGIN
                        UPDATE vanhanh.ChuyenTau
                        SET GioVeDichKH = (SELECT MAX(GioDenKeHoach) FROM vanhanh.LichDungGa WHERE MaChuyenTau = @ct),
                            SoPhutTreLuyKe = SoPhutTreLuyKe + @phutTangThucTe
                        WHERE MaChuyenTau = @ct;
                    END

                    -- Ghi nhật ký vận hành
                    INSERT INTO vanhanh.NhatKyChamGio (MaChuyenTau, MaGa, LoaiSuCo, SoPhutCham, GhiChuXuLy, ThoiDiemBaoCao)
                    VALUES (@ct, @ga, 'CHO_TRANH_TAU', @phutTangThucTe, @lyDo, SYSDATETIME());

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            var parameters = new[]
            {
                new SqlParameter("@ct", maChuyenTau),
                new SqlParameter("@ga", maGa),
                new SqlParameter("@gioDen", gioDenMoi),
                new SqlParameter("@gioDi", gioDiMoi),
                new SqlParameter("@ray", (object?)maDuongRay ?? DBNull.Value),
                new SqlParameter("@soPhutTang", soPhutTang),
                new SqlParameter("@lyDo", lyDo)
            };

            return DatabaseHelper.ExecuteNonQuery(sql, parameters) > 0;
        }
    }
}
