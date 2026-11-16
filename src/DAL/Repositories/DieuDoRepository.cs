using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;

namespace DAL.Repositories
{
    public class DieuDoRepository
    {
        public DataTable LayDuLieuBieuDo(DateTime ngay)
        {
            if (ngay < new DateTime(1753, 1, 1))
            {
                ngay = LayNgayBieuDoMacDinh();
            }

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
                    -- [13/11 Đạt] Khóa sửa chuyến (cùng khóa với màn Lịch trình / Lập đoàn tàu): không ghi chồng lên người đang sửa
                    DECLARE @tenKhoa NVARCHAR(255) = CONCAT(N'vanhanh.SuaChuyen:', @ct);
                    DECLARE @kqKhoa INT;
                    EXEC @kqKhoa = sp_getapplock @Resource = @tenKhoa, @LockMode = 'Exclusive',
                                                 @LockOwner = 'Transaction', @LockTimeout = 10000;
                    IF @kqKhoa < 0
                        RAISERROR(N'Chuyến tàu đang được người khác sửa ở màn Lịch trình, chưa xếp tránh được. Vui lòng thử lại sau ít giây.', 16, 1);

                    -- Khóa độc quyền chuyến tàu đang cập nhật để chống xung đột điều độ đồng thời
                    IF NOT EXISTS (SELECT 1 FROM vanhanh.ChuyenTau WITH (UPDLOCK, ROWLOCK) WHERE MaChuyenTau = @ct AND TrangThai <> 'DA_HUY')
                    BEGIN
                        RAISERROR(N'Chuyến tàu không tồn tại hoặc đã bị hủy.', 16, 1);
                        ROLLBACK TRANSACTION;
                        RETURN;
                    END

                    -- Tự động lưu bản sao lịch trình gốc vào vanhanh.LichDungGaGoc nếu chưa có
                    IF OBJECT_ID('vanhanh.LichDungGaGoc', 'U') IS NOT NULL
                    BEGIN
                        IF NOT EXISTS (SELECT 1 FROM vanhanh.LichDungGaGoc WHERE MaChuyenTau = @ct)
                        BEGIN
                            INSERT INTO vanhanh.LichDungGaGoc (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, MaDuongRay)
                            SELECT ld.MaChuyenTau, ld.MaGa, ld.ThuTuDung, ld.GioDenKeHoach, ld.GioDiKeHoach, ld.MaDuongRay
                            FROM vanhanh.LichDungGa ld
                            WHERE ld.MaChuyenTau = @ct;
                        END
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

                    -- [13/11 Đạt] Đổi PhienBan của chuyến (màn Lịch trình đang mở chuyến này biết dữ liệu đã đổi) + nhật ký hệ thống
                    UPDATE vanhanh.ChuyenTau SET SoPhutTreLuyKe = SoPhutTreLuyKe WHERE MaChuyenTau = @ct;
                    DECLARE @khoaNk VARCHAR(50) = CAST(@ct AS VARCHAR(50));
                    DECLARE @nkMoi NVARCHAR(MAX) = CONCAT(N'{""ThaoTac"":""Xếp tránh tại ', (SELECT TenGa FROM hatang.Ga WHERE MaGa = @ga),
                                                          N' (+', @phutTangThucTe, N' phút)""}');
                    EXEC kiemtoan.sp_GhiNhatKyHeThong @MaTaiKhoan = @nguoi, @HanhDong = 'XEP_TRANH_TAU',
                         @TenBang = 'vanhanh.ChuyenTau', @KhoaChinhRecord = @khoaNk, @DuLieuMoi = @nkMoi;

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
                new SqlParameter("@lyDo", lyDo),
                new SqlParameter("@nguoi", SqlDbType.Int) { Value = DTO.Common.PhienLamViec.MaTaiKhoan > 0 ? (object)DTO.Common.PhienLamViec.MaTaiKhoan : DBNull.Value }
            };

            return DatabaseHelper.ExecuteNonQuery(sql, parameters) > 0;
        }

        /// <summary>
        /// Khôi phục trạng thái lịch trình ban đầu của các chuyến tàu trong ngày được chọn:
        /// - Reset điểm dừng tránh LaDiemTranh = 0
        /// - Reset SoPhutTreLuyKe = 0
        /// - Xóa nhật ký chậm giờ do xếp tránh tự động CHO_TRANH_TAU
        /// </summary>
        public bool KhoiPhucLichTrinhNgay(DateTime ngay)
        {
            if (ngay < new DateTime(1753, 1, 1))
            {
                ngay = LayNgayBieuDoMacDinh();
            }

            string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                BEGIN TRY
                    DECLARE @Cts TABLE (MaChuyenTau INT);
                    INSERT INTO @Cts
                    SELECT DISTINCT ct.MaChuyenTau
                    FROM vanhanh.ChuyenTau ct
                    WHERE ct.GioXuatPhatKH < DATEADD(DAY, 1, @ngay)
                      AND ct.GioVeDichKH >= @ngay
                      AND ct.TrangThai <> 'DA_HUY';

                    -- 1. Nếu có bảng lưu gốc vanhanh.LichDungGaGoc, khôi phục lại 100% lịch chuẩn ban đầu
                    IF OBJECT_ID('vanhanh.LichDungGaGoc', 'U') IS NOT NULL 
                       AND EXISTS (SELECT 1 FROM vanhanh.LichDungGaGoc g JOIN @Cts c ON g.MaChuyenTau = c.MaChuyenTau)
                    BEGIN
                        -- Xóa toàn bộ các điểm dừng hiện tại của chuyến tàu (kể cả điểm dừng tránh chèn thêm)
                        DELETE ld
                        FROM vanhanh.LichDungGa ld
                        JOIN @Cts c ON ld.MaChuyenTau = c.MaChuyenTau
                        WHERE EXISTS (SELECT 1 FROM vanhanh.LichDungGaGoc g WHERE g.MaChuyenTau = c.MaChuyenTau);

                        -- Chèn lại nguyên vẹn các điểm dừng từ bảng gốc
                        INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, LaDiemTranh, MaDuongRay)
                        SELECT g.MaChuyenTau, g.MaGa, g.ThuTuDung, g.GioDenKeHoach, g.GioDiKeHoach, 0, g.MaDuongRay
                        FROM vanhanh.LichDungGaGoc g
                        JOIN @Cts c ON g.MaChuyenTau = c.MaChuyenTau;
                    END
                    ELSE
                    BEGIN
                        -- Fallback nếu chưa có LichDungGaGoc
                        UPDATE ld
                        SET ld.LaDiemTranh = 0,
                            ld.GioDenThucTe = NULL,
                            ld.GioDiThucTe = NULL
                        FROM vanhanh.LichDungGa ld
                        JOIN @Cts c ON ld.MaChuyenTau = c.MaChuyenTau;
                    END

                    -- 2. Reset giờ về đích, số phút trễ lũy kế và trạng thái
                    UPDATE ct
                    SET ct.GioVeDichKH = ISNULL((SELECT MAX(ld.GioDenKeHoach) FROM vanhanh.LichDungGa ld WHERE ld.MaChuyenTau = ct.MaChuyenTau), ct.GioVeDichKH),
                        ct.SoPhutTreLuyKe = 0,
                        ct.TrangThai = 'SAN_SANG'
                    FROM vanhanh.ChuyenTau ct
                    JOIN @Cts c ON ct.MaChuyenTau = c.MaChuyenTau;

                    -- 3. Xóa nhật ký chậm giờ do xếp tránh hoặc demo
                    DELETE nk
                    FROM vanhanh.NhatKyChamGio nk
                    JOIN @Cts c ON nk.MaChuyenTau = c.MaChuyenTau
                    WHERE nk.LoaiSuCo = 'CHO_TRANH_TAU' OR nk.GhiChuXuLy LIKE '%[DEMO]%';

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@ngay", ngay.Date) }) >= 0;
        }

        /// <summary>
        /// Ghi nhận sự cố chậm giờ thực tế cho một đoàn tàu:
        /// - Cộng dồn SoPhutTreLuyKe vào vanhanh.ChuyenTau
        /// - Cập nhật TrangThai = 'TRE_GIO'
        /// - Ghi vào vanhanh.NhatKyChamGio
        /// </summary>
        public bool GhiNhanSuCoChamGioThucTe(int maChuyenTau, int? maGa, int soPhutCham, string loaiSuCo, string ghiChu)
        {
            string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                BEGIN TRY
                    -- 1. Cộng dồn phút trễ vào ChuyenTau
                    UPDATE vanhanh.ChuyenTau
                    SET SoPhutTreLuyKe = ISNULL(SoPhutTreLuyKe, 0) + @soPhut,
                        TrangThai = 'TRE_GIO'
                    WHERE MaChuyenTau = @maChuyenTau;

                    -- 2. Ghi nhật ký sự cố chậm giờ
                    INSERT INTO vanhanh.NhatKyChamGio (MaChuyenTau, MaGa, LoaiSuCo, SoPhutCham, GhiChuXuLy, ThoiDiemBaoCao)
                    VALUES (@maChuyenTau, @maGa, @loaiSuCo, @soPhut, @ghiChu, SYSUTCDATETIME());

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@maChuyenTau", maChuyenTau),
                new SqlParameter("@maGa", (object?)maGa ?? DBNull.Value),
                new SqlParameter("@soPhut", soPhutCham),
                new SqlParameter("@loaiSuCo", loaiSuCo),
                new SqlParameter("@ghiChu", (object?)ghiChu ?? DBNull.Value)
            }) > 0;
        }

        /// <summary>
        /// Cập nhật tiến độ chạy tàu thực tế tại một ga (giờ đến / giờ đi thực tế):
        /// - Cập nhật GioDenThucTe, GioDiThucTe của ga đó
        /// - Tự động điền GioDiThucTe cho các ga trước nếu còn NULL
        /// - Cập nhật TrangThai ChuyenTau = 'DANG_CHAY'
        /// </summary>
        public bool CapNhatTienDoGaThucTe(int maChuyenTau, int maGa, DateTime? gioDenTT, DateTime? gioDiTT)
        {
            string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                BEGIN TRY
                    DECLARE @ThuTu INT;
                    SELECT @ThuTu = ThuTuDung 
                    FROM vanhanh.LichDungGa 
                    WHERE MaChuyenTau = @maChuyenTau AND MaGa = @maGa;

                    IF @ThuTu IS NOT NULL
                    BEGIN
                        -- Cập nhật ga hiện tại
                        UPDATE vanhanh.LichDungGa
                        SET GioDenThucTe = ISNULL(@gioDen, GioDenThucTe),
                            GioDiThucTe = ISNULL(@gioDi, GioDiThucTe)
                        WHERE MaChuyenTau = @maChuyenTau AND MaGa = @maGa;

                        -- Các ga trước đó: nếu chưa có giờ đi thực tế, điền giờ kế hoạch để khóa chặng bán vé
                        UPDATE vanhanh.LichDungGa
                        SET GioDenThucTe = ISNULL(GioDenThucTe, GioDenKeHoach),
                            GioDiThucTe = ISNULL(GioDiThucTe, GioDiKeHoach)
                        WHERE MaChuyenTau = @maChuyenTau AND ThuTuDung < @ThuTu;

                        -- Chuyển trạng thái chuyến tàu sang Đang Chạy nếu chưa hoàn thành
                        UPDATE vanhanh.ChuyenTau
                        SET TrangThai = 'DANG_CHAY'
                        WHERE MaChuyenTau = @maChuyenTau AND TrangThai NOT IN ('HOAN_THANH', 'HUY_BO');
                    END

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@maChuyenTau", maChuyenTau),
                new SqlParameter("@maGa", maGa),
                new SqlParameter("@gioDen", (object?)gioDenTT ?? DBNull.Value),
                new SqlParameter("@gioDi", (object?)gioDiTT ?? DBNull.Value)
            }) > 0;
        }

        public int LaySoPhutTreLuyKe(int maChuyenTau)
        {
            string sql = "SELECT ISNULL(SoPhutTreLuyKe, 0) FROM vanhanh.ChuyenTau WHERE MaChuyenTau = @id";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@id", maChuyenTau) });
            return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
        }
    }
}
