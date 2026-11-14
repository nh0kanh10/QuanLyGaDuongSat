-- ==============================================================================================
-- DỰ ÁN: HỆ THỐNG QUẢN LÝ GA & VẬN TẢI ĐƯỜNG SẮT (QUAN LY GA DUONG SAT)
-- SCRIPT: KÍCH HOẠT DỮ LIỆU BÁN VÉ TRƯỚC (ADVANCE BOOKING) CHO CÁC NGÀY TRONG TUẦN & CUỐI TUẦN
-- TÁC GIẢ: Nhóm Phát Triển Đồ Án Tốt Nghiệp TDC
-- NGÀY TẠO: 13/11/2026
-- ==============================================================================================
-- MỤC ĐÍCH:
--   1. Giải quyết bài toán "Mua vé cuối tuần từ đầu tuần" (Advance Booking).
--   2. Tự động sinh lịch chạy tàu (vanhanh.ChuyenTau) trạng thái DA_LEN_LICH cho các ngày Thứ 6,
--      Thứ 7, Chủ Nhật trong vòng 14 ngày tới tính từ ngày chạy script.
--   3. Tự động khởi tạo Toa xe khách (vantai.ToaXeKhach: AN, NML, BN) và Chỗ ngồi
--      (vantai.ChoNgoi: 134 chỗ/chuyến) sẵn sàng 100% mở bán vé.
--   4. Tự động lập Lịch dừng ga (vanhanh.LichDungGa) chi tiết qua các ga dọc tuyến Bắc - Nam.
--   5. Cung cấp Stored Procedure 'vantai.sp_LayDanhSachNgayCoChuyen' hỗ trợ giao diện hiển thị danh
--      sách các ngày có chuyến tàu khả dụng.
--
-- SỬA 15/11 (Đạt, khi đưa vào database\thu_tu_chay.txt):
--   - Bỏ "USE QuanLyDuongSatV2": tao_lai_csdl.cmd dựng CSDL tên khác (CSDL tạm) thì không ghi nhầm vào CSDL thật.
--   - KHÔNG tạo vanhanh.DoanTau: trước đây gán cùng 1 đầu máy cho mọi chuyến (chồng giờ, luật PT-08) và đặt sẵn
--     DaDuyetAnToan = 1 khi chưa khám xe. Đầu máy + toa do Phương tiện lập, duyệt an toàn do Bảo trì làm.
--     Bán vé chỉ cần ToaXeKhach + ChoNgoi nên vẫn mở bán bình thường.
--   - Bỏ đoạn xoá chuyến tương lai chưa có vé (gặp chuyến đã lập toa / có kíp / có biên bản thì lỗi khoá ngoại,
--     rollback cả file). Vẫn chạy lại được: chuyến đã có (cùng mác, cùng ngày) thì thủ tục bỏ qua.
--   - Sinh từ NGÀY MAI (chuyến hôm nay do seed khác tạo, đã có kíp lái); SET QUOTED_IDENTIFIER ON vì ToaXeKhach
--     có chỉ mục lọc (migration V20261113_1000) và thủ tục giữ thiết lập lúc tạo.
--
-- HƯỚNG DẪN DÀNH CHO DEVELOPER KHÁC:
--   - Mở file này trong SQL Server Management Studio (SSMS) hoặc Azure Data Studio.
--   - CHỌN DATABASE dự án trước khi chạy (mặc định QuanLyDuongSatV2) — file không tự USE.
--   - Bấm Execute (hoặc nhấn phím F5).
--   - Chạy lại nhiều lần không trùng dữ liệu, có bọc Transaction.
-- ==============================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT N'==============================================================================================';
PRINT N'BẮT ĐẦU CẬP NHẬT KẾ HOẠCH BÁN VÉ TRƯỚC (ADVANCE BOOKING) CHO CÁC NGÀY CUỐI TUẦN...';
PRINT N'==============================================================================================';

-- ----------------------------------------------------------------------------------------------
-- PHẦN 1: STORED PROCEDURE TỰ ĐỘNG LẬP LỊCH CHUYẾN TÀU THEO NGÀY
-- ----------------------------------------------------------------------------------------------
IF OBJECT_ID('vanhanh.sp_TuDongLapChuyenTauTheoNgay', 'P') IS NOT NULL
    DROP PROCEDURE vanhanh.sp_TuDongLapChuyenTauTheoNgay;
GO

CREATE PROCEDURE vanhanh.sp_TuDongLapChuyenTauTheoNgay
    @SoHieuMacTau VARCHAR(20),
    @NgayChay DATE,
    @MaChuyenTauMoi INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaChuyenTauMoi = 0;

    DECLARE @MaMacTau INT, @MaGaDi INT, @MaGaDen INT, @HuongChay VARCHAR(20);
    SELECT @MaMacTau = MaMacTau, @MaGaDi = MaGaDi, @MaGaDen = MaGaDen, @HuongChay = HuongChay
    FROM vanhanh.MacTauMau
    WHERE SoHieuMacTau = @SoHieuMacTau;

    IF @MaMacTau IS NULL
    BEGIN
        RAISERROR(N'Không tìm thấy mác tàu mẫu: %s', 16, 1, @SoHieuMacTau);
        RETURN;
    END

    -- Kiểm tra nếu chuyến tàu ngày đó đã tồn tại thì không tạo lại (Idempotent)
    SELECT @MaChuyenTauMoi = MaChuyenTau
    FROM vanhanh.ChuyenTau
    WHERE MaMacTau = @MaMacTau AND CAST(NgayXuatPhat AS DATE) = @NgayChay;

    IF @MaChuyenTauMoi > 0
    BEGIN
        RETURN; -- Đã có sẵn chuyến
    END

    -- Xác định giờ xuất phát và giờ về đích chuẩn theo mác tàu
    DECLARE @GioXuatPhatKH DATETIME2, @GioVeDichKH DATETIME2;
    IF @SoHieuMacTau = 'SE1'
    BEGIN
        SET @GioXuatPhatKH = DATEADD(HOUR, 6, CAST(@NgayChay AS DATETIME2));       -- 06:00
        SET @GioVeDichKH   = DATEADD(MINUTE, 2305, CAST(@NgayChay AS DATETIME2));   -- ~38.5 tiếng
    END
    ELSE IF @SoHieuMacTau = 'SE2'
    BEGIN
        SET @GioXuatPhatKH = DATEADD(HOUR, 19, CAST(@NgayChay AS DATETIME2));      -- 19:00
        SET @GioVeDichKH   = DATEADD(MINUTE, 3090, CAST(@NgayChay AS DATETIME2));   -- ~51.5 tiếng
    END
    ELSE IF @SoHieuMacTau = 'SE3'
    BEGIN
        SET @GioXuatPhatKH = DATEADD(MINUTE, 1140, CAST(@NgayChay AS DATETIME2));  -- 19:00
        SET @GioVeDichKH   = DATEADD(MINUTE, 3090, CAST(@NgayChay AS DATETIME2));   -- Đến SGO sau 32.5h
    END
    ELSE IF @SoHieuMacTau = 'SE4'
    BEGIN
        SET @GioXuatPhatKH = DATEADD(MINUTE, 1155, CAST(@NgayChay AS DATETIME2));  -- 19:15
        SET @GioVeDichKH   = DATEADD(MINUTE, 3090, CAST(@NgayChay AS DATETIME2));   -- Đến HNI
    END
    ELSE
    BEGIN
        SET @GioXuatPhatKH = DATEADD(HOUR, 8, CAST(@NgayChay AS DATETIME2));
        SET @GioVeDichKH   = DATEADD(HOUR, 36, CAST(@NgayChay AS DATETIME2));
    END

    -- 1. Thêm ChuyenTau
    INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, TrangThai, SoPhutTreLuyKe)
    VALUES (@MaMacTau, @NgayChay, @GioXuatPhatKH, @GioVeDichKH, 'DA_LEN_LICH', 0);
    SET @MaChuyenTauMoi = SCOPE_IDENTITY();

    -- 2. (Sửa 15/11) Không tạo DoanTau ở đây: đầu máy + toa thật do màn hình Phương tiện lập (kiểm chồng giờ,
    --    tải trục, đường tránh), cờ duyệt an toàn do Bảo trì đặt sau khi khám xe. Chưa lập đoàn tàu thì
    --    kiểm tra xuất bến chặn chuyển "Sẵn sàng" — đúng nghiệp vụ; bán vé không cần DoanTau.

    -- 3. Thêm ToaXeKhach (3 Toa: Toa 1 AN 28 chỗ, Toa 2 NML 64 chỗ, Toa 3 BN 42 chỗ)
    DECLARE @ToaAN INT, @ToaNML INT, @ToaBN INT;

    INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua)
    VALUES (@MaChuyenTauMoi, N'Toa 1 (AN)', 'AN', 1, 28);
    SET @ToaAN = SCOPE_IDENTITY();

    INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua)
    VALUES (@MaChuyenTauMoi, N'Toa 2 (NML)', 'NML', 2, 64);
    SET @ToaNML = SCOPE_IDENTITY();

    INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua)
    VALUES (@MaChuyenTauMoi, N'Toa 3 (BN)', 'BN', 3, 42);
    SET @ToaBN = SCOPE_IDENTITY();

    -- 4. Sinh Chỗ Ngồi (ChoNgoi)
    -- 4.1 Toa 1 (AN - 28 chỗ)
    INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
    SELECT @ToaAN, n, ((n-1)%2)+1, 0
    FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),
                 (15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28)) v(n);

    -- 4.2 Toa 2 (NML - 64 chỗ)
    INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
    SELECT @ToaNML, n, NULL, 0
    FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),
                 (17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28),(29),(30),(31),(32),
                 (33),(34),(35),(36),(37),(38),(39),(40),(41),(42),(43),(44),(45),(46),(47),(48),
                 (49),(50),(51),(52),(53),(54),(55),(56),(57),(58),(59),(60),(61),(62),(63),(64)) v(n);

    -- 4.3 Toa 3 (BN - 42 chỗ)
    INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
    SELECT @ToaBN, n, ((n-1)%3)+1, 0
    FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),
                 (15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28),
                 (29),(30),(31),(32),(33),(34),(35),(36),(37),(38),(39),(40),(41),(42)) v(n);

    -- 5. Lập Lịch Dừng Ga chi tiết cho TOÀN BỘ 25 GA KHÁCH DỌC TUYẾN BẮC - NAM
    -- (Bao gồm đầy đủ: Hà Nội, Phủ Lý, Nam Định, Ninh Bình, Bỉm Sơn, Thanh Hóa, Vinh, Đồng Hới, Huế, Đà Nẵng, Quảng Ngãi, Diêu Trì, Tuy Hòa, Nha Trang, Biên Hòa, Sài Gòn...)
    IF @HuongChay = 'BAC_NAM'
    BEGIN
        INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach)
        SELECT 
            @MaChuyenTauMoi,
            g.MaGa,
            ROW_NUMBER() OVER (ORDER BY g.LyTrinhKm ASC) AS ThuTuDung,
            DATEADD(MINUTE, CAST((g.LyTrinhKm / 1726.20) * 1950.0 AS INT), @GioXuatPhatKH) AS GioDenKeHoach,
            DATEADD(MINUTE, CAST((g.LyTrinhKm / 1726.20) * 1950.0 AS INT) + 
                CASE 
                    WHEN g.LyTrinhKm = 1726.20 THEN 0 -- Ga cuối cùng (Sài Gòn)
                    WHEN g.HangGa = 'HANG_1' AND g.LyTrinhKm <> 0.00 THEN 15 
                    ELSE 5 
                END, @GioXuatPhatKH) AS GioDiKeHoach
        FROM hatang.Ga g
        WHERE g.MaGaCode NOT IN ('GBA', 'STH') -- Loại bỏ 2 ga chuyên hàng Giáp Bát và Sóng Thần
        ORDER BY g.LyTrinhKm ASC;
    END
    ELSE -- NAM_BAC
    BEGIN
        INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach)
        SELECT 
            @MaChuyenTauMoi,
            g.MaGa,
            ROW_NUMBER() OVER (ORDER BY g.LyTrinhKm DESC) AS ThuTuDung,
            DATEADD(MINUTE, CAST(((1726.20 - g.LyTrinhKm) / 1726.20) * 1950.0 AS INT), @GioXuatPhatKH) AS GioDenKeHoach,
            DATEADD(MINUTE, CAST(((1726.20 - g.LyTrinhKm) / 1726.20) * 1950.0 AS INT) + 
                CASE 
                    WHEN g.LyTrinhKm = 0.00 THEN 0 -- Ga cuối cùng (Hà Nội)
                    WHEN g.HangGa = 'HANG_1' AND g.LyTrinhKm <> 1726.20 THEN 15 
                    ELSE 5 
                END, @GioXuatPhatKH) AS GioDiKeHoach
        FROM hatang.Ga g
        WHERE g.MaGaCode NOT IN ('GBA', 'STH')
        ORDER BY g.LyTrinhKm DESC;
    END
END;
GO

PRINT N'>> Đã tạo Stored Procedure vanhanh.sp_TuDongLapChuyenTauTheoNgay thành công!';
GO

-- ----------------------------------------------------------------------------------------------
-- PHẦN 2: STORED PROCEDURE TRA CỨU CÁC NGÀY CÓ CHUYẾN TÀU KHẢ DỤNG (CHO UI / DEV)
-- ----------------------------------------------------------------------------------------------
IF OBJECT_ID('vantai.sp_LayDanhSachNgayCoChuyen', 'P') IS NOT NULL
    DROP PROCEDURE vantai.sp_LayDanhSachNgayCoChuyen;
GO

CREATE PROCEDURE vantai.sp_LayDanhSachNgayCoChuyen
    @MaGaDi INT,
    @MaGaDen INT,
    @TuNgay DATE = NULL,
    @DenNgay DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @TuNgay IS NULL SET @TuNgay = CAST(GETDATE() AS DATE);
    IF @DenNgay IS NULL SET @DenNgay = DATEADD(DAY, 30, @TuNgay);

    SELECT 
        CAST(ldDi.GioDiKeHoach AS DATE) AS NgayDi,
        DATENAME(WEEKDAY, ldDi.GioDiKeHoach) AS ThuTrongTuan,
        COUNT(DISTINCT ct.MaChuyenTau) AS SoChuyenTau,
        STRING_AGG(mt.SoHieuMacTau, ', ') WITHIN GROUP (ORDER BY ldDi.GioDiKeHoach ASC) AS CacMacTau,
        MIN(ISNULL(vantai.fn_TinhGiaVe('NML', @MaGaDi, @MaGaDen, NULL, mt.LoaiTau), 150000)) AS GiaVeTu
    FROM vanhanh.ChuyenTau ct
    JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
    JOIN vanhanh.LichDungGa ldDi ON ct.MaChuyenTau = ldDi.MaChuyenTau AND ldDi.MaGa = @MaGaDi
    JOIN vanhanh.LichDungGa ldDen ON ct.MaChuyenTau = ldDen.MaChuyenTau AND ldDen.MaGa = @MaGaDen
    WHERE ct.TrangThai IN ('DA_LEN_LICH', 'SAN_SANG', 'DANG_CHAY', 'TRE_GIO')
      AND ldDi.ThuTuDung < ldDen.ThuTuDung
      AND CAST(ldDi.GioDiKeHoach AS DATE) BETWEEN @TuNgay AND @DenNgay
      AND NOT EXISTS (
          SELECT 1 FROM vanhanh.LichDungGa ldCheck 
          WHERE ldCheck.MaChuyenTau = ct.MaChuyenTau AND ldCheck.MaGa = @MaGaDi AND ldCheck.GioDiThucTe IS NOT NULL
      )
    GROUP BY CAST(ldDi.GioDiKeHoach AS DATE), DATENAME(WEEKDAY, ldDi.GioDiKeHoach)
    ORDER BY NgayDi ASC;
END;
GO

PRINT N'>> Đã tạo Stored Procedure vantai.sp_LayDanhSachNgayCoChuyen thành công!';
GO

-- ----------------------------------------------------------------------------------------------
-- PHẦN 3: KÍCH HOẠT DỮ LIỆU CHUYẾN TÀU CHO CÁC NGÀY TRONG TUẦN & CUỐI TUẦN (14 NGÀY TỚI)
-- ----------------------------------------------------------------------------------------------
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @NgayHienTai DATE = CAST(GETDATE() AS DATE);
    DECLARE @i INT = 1;   -- (Sửa 15/11) từ ngày mai; chuyến hôm nay do seed khác tạo (đã có kíp lái)
    DECLARE @NgayLoop DATE;
    DECLARE @MaChuyen INT;
    DECLARE @DayOfWeekName NVARCHAR(20);

    -- (Sửa 15/11) Bỏ đoạn xoá chuyến tương lai chưa có vé: xoá DoanTau khi còn ChiTietDoanTau / kíp lái / biên bản
    -- khám xe trỏ tới thì lỗi khoá ngoại và rollback cả file. Thủ tục đã tự bỏ qua chuyến có sẵn (cùng mác, cùng ngày).

    PRINT N'>> Đang tự động quét và sinh chuyến tàu cho 14 ngày tới (từ ' + CONVERT(NVARCHAR(10), DATEADD(DAY, 1, @NgayHienTai), 103) + N')...';

    WHILE @i <= 14
    BEGIN
        SET @NgayLoop = DATEADD(DAY, @i, @NgayHienTai);
        SET @DayOfWeekName = DATENAME(WEEKDAY, @NgayLoop);

        -- Mỗi ngày chạy ít nhất 1 cặp tàu nhanh SE1 (Bắc - Nam) và SE2 (Nam - Bắc)
        EXEC vanhanh.sp_TuDongLapChuyenTauTheoNgay @SoHieuMacTau = 'SE1', @NgayChay = @NgayLoop, @MaChuyenTauMoi = @MaChuyen OUTPUT;
        EXEC vanhanh.sp_TuDongLapChuyenTauTheoNgay @SoHieuMacTau = 'SE2', @NgayChay = @NgayLoop, @MaChuyenTauMoi = @MaChuyen OUTPUT;

        -- Riêng các ngày Cuối Tuần (Thứ Sáu, Thứ Bảy, Chủ Nhật) tăng cường thêm cặp tàu SE3 và SE4
        IF DATEPART(WEEKDAY, @NgayLoop) IN (1, 6, 7) -- 1: Chủ Nhật, 6: Thứ Sáu, 7: Thứ Bảy (theo chuẩn SQL US_ENGLISH)
        BEGIN
            EXEC vanhanh.sp_TuDongLapChuyenTauTheoNgay @SoHieuMacTau = 'SE3', @NgayChay = @NgayLoop, @MaChuyenTauMoi = @MaChuyen OUTPUT;
            EXEC vanhanh.sp_TuDongLapChuyenTauTheoNgay @SoHieuMacTau = 'SE4', @NgayChay = @NgayLoop, @MaChuyenTauMoi = @MaChuyen OUTPUT;
        END

        SET @i = @i + 1;
    END

    COMMIT TRANSACTION;
    PRINT N'==============================================================================================';
    PRINT N'HOÀN TẤT THÀNH CÔNG! ĐÃ SINH ĐỦ LỊCH CHẠY TÀU & VÉ MỞ BÁN CHO TOÀN BỘ 14 NGÀY TỚI!';
    PRINT N'BÂY GIỜ BẠN CÓ THỂ MỞ MÀN HÌNH BÁN VÉ, CHỌN BẤT KỲ NGÀY CUỐI TUẦN NÀO ĐỂ MUA VÉ TRƯỚC.';
    PRINT N'==============================================================================================';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'Lỗi khi sinh dữ liệu bán vé trước: %s', 16, 1, @ErrMsg);
END CATCH;
GO

-- ----------------------------------------------------------------------------------------------
-- PHẦN 4: TRUY VẤN KIỂM CHỨNG NHANH (VERIFICATION QUERY)
-- ----------------------------------------------------------------------------------------------
PRINT N'';
PRINT N'DANH SÁCH CÁC CHUYẾN TÀU ĐÃ LÊN LỊCH SẴN SÀNG BÁN VÉ TRONG 14 NGÀY TỚI:';
SELECT 
    ct.MaChuyenTau,
    mt.SoHieuMacTau,
    mt.LoaiTau,
    gDi.TenGa AS GaXuatPhat,
    gDen.TenGa AS GaDich,
    CONVERT(VARCHAR(10), ct.NgayXuatPhat, 103) AS NgayXuatPhat,
    DATENAME(WEEKDAY, ct.NgayXuatPhat) AS ThuTrongTuan,
    CONVERT(VARCHAR(5), ct.GioXuatPhatKH, 108) AS GioDi,
    ct.TrangThai,
    (SELECT COUNT(*) FROM vantai.ToaXeKhach tx WHERE tx.MaChuyenTau = ct.MaChuyenTau) AS SoToaXe,
    (SELECT COUNT(*) FROM vantai.ChoNgoi cn JOIN vantai.ToaXeKhach tx2 ON cn.MaToaXeKhach = tx2.MaToaXeKhach WHERE tx2.MaChuyenTau = ct.MaChuyenTau) AS TongSoChoTrong
FROM vanhanh.ChuyenTau ct
JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
JOIN hatang.Ga gDi ON mt.MaGaDi = gDi.MaGa
JOIN hatang.Ga gDen ON mt.MaGaDen = gDen.MaGa
WHERE ct.TrangThai = 'DA_LEN_LICH'
ORDER BY ct.NgayXuatPhat ASC, ct.GioXuatPhatKH ASC;
GO
