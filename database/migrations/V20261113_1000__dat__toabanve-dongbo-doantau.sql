/* =========================================================================
   MIGRATION: Toa bán vé đồng bộ theo đoàn tàu + chặn bán vé ngoài phạm vi toa
   Tác giả  : Đạt
   Ngày     : 13/11/2026 (Sprint 3)
   Bảng     : vantai.ToaXeKhach (thêm cột), vantai.sp_DatVeTheoChang (thủ tục), baocao.vw_HeSoSuDungGhe (view)
              — bảng / thủ tục của A Bá, chỉ CỘNG THÊM (cột NULL / có DEFAULT, thủ tục giữ nguyên tham số).

   Đạt chốt 13/11 (theo khuyến nghị):
   1. Toa bán vé nối với đoàn tàu THEO SỐ TOA KHÁCH: toa khách (NC / NML / BN / AN) thứ k trong đoàn tàu
      (thứ tự móc nối) ↔ "Toa k" bán vé (ThuTuToa = k). Thay toa thật cùng loại thì vé giữ nguyên toa / chỗ.
      Thêm MaToaXe (toa thật đang phục vụ) + MaGaNoi / MaGaCat (phạm vi chạy, chép từ ChiTietDoanTau).
   2. Lưu đoàn tàu làm hỏng vé còn hiệu lực → CHẶN, liệt kê vé (LapTauService). Toa chỉ còn vé đã hoàn / đã hủy
      thì không xoá được (khoá ngoại vantai.Ve → ChoNgoi) → NgungBan = 1: giữ lịch sử, Bán vé không hiện.
      UNIQUE (MaChuyenTau, ThuTuToa) đổi thành chỉ mục duy nhất CÓ LỌC (NgungBan = 0) để "Toa k" mới thay
      được toa k cũ đã ngừng bán.
   3. Bán vé chặn 2 lớp: danh sách toa chỉ hiện toa chạy qua cả chặng (fn_ToaChayQuaChang), và thủ tục đặt vé
      kiểm lại trong giao dịch (ghế thuộc toa đang bán của đúng chuyến, toa chạy qua chặng).
      Khoá ứng dụng "vantai.BanVe:<MaChuyenTau>": đặt vé lấy Shared, đồng bộ toa (lưu đoàn tàu) lấy Exclusive
      → không bán vào ghế đang bị bỏ giữa lúc đồng bộ kiểm tra vé và lúc ghi.

   Bổ sung dữ liệu: điền MaToaXe / MaGaNoi / MaGaCat cho toa bán vé hiện có khớp đoàn tàu theo số toa + loại toa.

   Lưu ý: chỉ mục có lọc đòi QUOTED_IDENTIFIER ON khi ghi bảng ToaXeKhach (ADO.NET, SSMS mặc định ON;
   sqlcmd phải có -I như các script khác).
   Chạy lại được nhiều lần. Không có USE: sqlcmd -S . -E -I -f 65001 -d <TênCSDL> -i <file>
   ========================================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'Đang đứng ở CSDL hệ thống. Chạy lại với -d QuanLyDuongSatV2 (hoặc CSDL kiểm thử).', 16, 1);
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'vantai.ToaXeKhach', N'U') IS NULL OR COL_LENGTH(N'vanhanh.ChiTietDoanTau', N'MaGaNoi') IS NULL
BEGIN
    RAISERROR(N'Chưa có vantai.ToaXeKhach hoặc ChiTietDoanTau.MaGaNoi. Chạy script gốc + migration V20261009_0820 trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Cột mới của ToaXeKhach -------------------------------------------------------------------
IF COL_LENGTH(N'vantai.ToaXeKhach', N'MaToaXe') IS NULL
    ALTER TABLE vantai.ToaXeKhach ADD MaToaXe INT NULL
        CONSTRAINT FK_ToaXeKhach_ToaXe REFERENCES phuongtien.ToaXe(MaToaXe);
GO
IF COL_LENGTH(N'vantai.ToaXeKhach', N'MaGaNoi') IS NULL
    ALTER TABLE vantai.ToaXeKhach ADD MaGaNoi INT NULL
        CONSTRAINT FK_ToaXeKhach_GaNoi REFERENCES hatang.Ga(MaGa);
GO
IF COL_LENGTH(N'vantai.ToaXeKhach', N'MaGaCat') IS NULL
    ALTER TABLE vantai.ToaXeKhach ADD MaGaCat INT NULL
        CONSTRAINT FK_ToaXeKhach_GaCat REFERENCES hatang.Ga(MaGa);
GO
IF COL_LENGTH(N'vantai.ToaXeKhach', N'NgungBan') IS NULL
    ALTER TABLE vantai.ToaXeKhach ADD NgungBan BIT NOT NULL
        CONSTRAINT DF_ToaXeKhach_NgungBan DEFAULT 0;
GO

-- 2. "Toa k" duy nhất trong các toa đang bán ---------------------------------------------------
IF OBJECT_ID(N'vantai.UQ_ChuyenTau_ThuTuToa', N'UQ') IS NOT NULL
    ALTER TABLE vantai.ToaXeKhach DROP CONSTRAINT UQ_ChuyenTau_ThuTuToa;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ToaXeKhach_ChuyenTau_ThuTu'
               AND object_id = OBJECT_ID(N'vantai.ToaXeKhach'))
    CREATE UNIQUE INDEX UX_ToaXeKhach_ChuyenTau_ThuTu ON vantai.ToaXeKhach (MaChuyenTau, ThuTuToa)
        WHERE NgungBan = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ToaXeKhach_MaToaXe'
               AND object_id = OBJECT_ID(N'vantai.ToaXeKhach'))
    CREATE INDEX IX_ToaXeKhach_MaToaXe ON vantai.ToaXeKhach (MaToaXe);
GO

-- 3. Bổ sung liên kết cho toa bán vé đã có (khớp số toa khách + loại toa) ------------------------
;WITH K AS (
    SELECT dt.MaChuyenTau, ctd.MaToaXe, cl.MaChungLoaiCode AS LoaiToa, ctd.MaGaNoi, ctd.MaGaCat,
           ROW_NUMBER() OVER (PARTITION BY dt.MaChuyenTau ORDER BY ctd.ThuTuNoi) AS ThuTuKhach
    FROM vanhanh.DoanTau dt
    JOIN vanhanh.ChiTietDoanTau ctd ON ctd.MaDoanTau = dt.MaDoanTau
    JOIN phuongtien.ToaXe tx ON tx.MaToaXe = ctd.MaToaXe
    JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
    WHERE cl.MaChungLoaiCode IN ('NC', 'NML', 'BN', 'AN'))
UPDATE t
SET MaToaXe = K.MaToaXe, MaGaNoi = K.MaGaNoi, MaGaCat = K.MaGaCat
FROM vantai.ToaXeKhach t
JOIN K ON K.MaChuyenTau = t.MaChuyenTau AND K.ThuTuKhach = t.ThuTuToa AND K.LoaiToa = t.LoaiToa
WHERE t.NgungBan = 0 AND t.MaToaXe IS NULL;
PRINT CONCAT(N'Đã nối ', @@ROWCOUNT, N' toa bán vé với toa thật trong đoàn tàu.');
GO

-- 4. Toa bán vé có chạy qua chặng ga đi → ga đến không ---------------------------------------
--    Toa chạy suốt (không ga nối / ga cắt) → 1. Toa nối / cắt dọc đường: ga đi không trước ga nối,
--    ga đến không sau ga cắt (theo thứ tự lịch dừng của chuyến). Không xác định được thứ tự → 0 (không bán).
CREATE OR ALTER FUNCTION vantai.fn_ToaChayQuaChang (@MaToaXeKhach INT, @MaGaDi INT, @MaGaDen INT)
RETURNS BIT
AS
BEGIN
    DECLARE @ct INT, @noi INT, @cat INT;
    SELECT @ct = MaChuyenTau, @noi = MaGaNoi, @cat = MaGaCat
    FROM vantai.ToaXeKhach WHERE MaToaXeKhach = @MaToaXeKhach;

    IF @ct IS NULL RETURN 0;
    IF @noi IS NULL AND @cat IS NULL RETURN 1;

    DECLARE @tDi INT = (SELECT MIN(ThuTuDung) FROM vanhanh.LichDungGa WHERE MaChuyenTau = @ct AND MaGa = @MaGaDi);
    DECLARE @tDen INT = (SELECT MAX(ThuTuDung) FROM vanhanh.LichDungGa WHERE MaChuyenTau = @ct AND MaGa = @MaGaDen);
    IF @tDi IS NULL OR @tDen IS NULL RETURN 0;

    IF @noi IS NOT NULL
       AND @tDi < ISNULL((SELECT MIN(ThuTuDung) FROM vanhanh.LichDungGa WHERE MaChuyenTau = @ct AND MaGa = @noi), 2147483647)
        RETURN 0;
    IF @cat IS NOT NULL
       AND @tDen > ISNULL((SELECT MAX(ThuTuDung) FROM vanhanh.LichDungGa WHERE MaChuyenTau = @ct AND MaGa = @cat), -1)
        RETURN 0;
    RETURN 1;
END;
GO

-- 5. Thủ tục đặt vé: giữ nguyên tham số + cách kiểm chặng giao nhau của A Bá, thêm 3 kiểm tra -------
CREATE OR ALTER PROCEDURE vantai.sp_DatVeTheoChang
    @MaDonVe            INT,
    @MaChuyenTau        INT,
    @MaChoNgoi          INT,
    @MaGaDi             INT,
    @MaGaDen            INT,
    @TenHanhKhach       NVARCHAR(100),
    @CCCDHanhKhach      VARCHAR(20),
    @GiaVeGoc           DECIMAL(12,2),
    @SoTienGiam         DECIMAL(12,2) = 0.0,
    @MaVeCodeOutput     VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @KmDi DECIMAL(7,2);
    DECLARE @KmDen DECIMAL(7,2);
    DECLARE @GiaVeThucThu DECIMAL(12,2) = @GiaVeGoc - @SoTienGiam;

    SELECT @KmDi = LyTrinhKm FROM hatang.Ga WHERE MaGa = @MaGaDi;
    SELECT @KmDen = LyTrinhKm FROM hatang.Ga WHERE MaGa = @MaGaDen;

    IF @KmDi IS NULL OR @KmDen IS NULL
    BEGIN
        RAISERROR(N'Ga đi hoặc Ga đến không hợp lệ.', 16, 1);
        RETURN -1;
    END;

    IF @MaVeCodeOutput IS NULL OR LEN(@MaVeCodeOutput) = 0
        SET @MaVeCodeOutput = 'TK_' + UPPER(SUBSTRING(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 1, 10));

    BEGIN TRANSACTION;
    BEGIN TRY
        -- [13/11 Đạt] Khoá "bán vé của chuyến" (Shared): lưu đoàn tàu đồng bộ toa bán vé lấy Exclusive.
        DECLARE @TaiNguyen NVARCHAR(255) = CONCAT(N'vantai.BanVe:', @MaChuyenTau);
        DECLARE @KqKhoa INT;
        EXEC @KqKhoa = sp_getapplock @Resource = @TaiNguyen, @LockMode = 'Shared',
                                     @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @KqKhoa < 0
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'Đoàn tàu của chuyến đang được cập nhật, chưa đặt được vé. Vui lòng thử lại sau ít giây.', 16, 1);
            RETURN -3;
        END;

        -- [13/11 Đạt] Ghế phải thuộc toa đang bán của đúng chuyến, và toa chạy qua cả chặng của khách
        DECLARE @ToaKhach INT, @ChuyenCuaToa INT, @NgungBan BIT, @NhanToa NVARCHAR(20);
        SELECT @ToaKhach = tx.MaToaXeKhach, @ChuyenCuaToa = tx.MaChuyenTau, @NgungBan = tx.NgungBan, @NhanToa = tx.NhanHieuToa
        FROM vantai.ChoNgoi cn
        JOIN vantai.ToaXeKhach tx ON tx.MaToaXeKhach = cn.MaToaXeKhach
        WHERE cn.MaChoNgoi = @MaChoNgoi;

        IF @ToaKhach IS NULL OR @ChuyenCuaToa <> @MaChuyenTau
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'Chỗ ngồi không thuộc chuyến tàu này. Hãy tải lại sơ đồ toa.', 16, 1);
            RETURN -4;
        END;
        IF @NgungBan = 1
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'%s đã ngừng bán vì đoàn tàu vừa được lập lại. Hãy tải lại sơ đồ toa rồi chọn chỗ khác.', 16, 1, @NhanToa);
            RETURN -5;
        END;
        IF vantai.fn_ToaChayQuaChang(@ToaKhach, @MaGaDi, @MaGaDen) = 0
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'%s không chạy qua cả chặng của khách (toa nối / cắt dọc đường). Hãy chọn toa khác.', 16, 1, @NhanToa);
            RETURN -6;
        END;

        -- Khóa chỗ ngồi trong chặng để loại trừ check-then-insert race condition (Chuẩn 3NF tra cứu qua bảng Ga)
        IF EXISTS (
            SELECT 1
            FROM vantai.Ve v WITH (UPDLOCK, HOLDLOCK)
            JOIN hatang.Ga gDi ON v.MaGaDi = gDi.MaGa
            JOIN hatang.Ga gDen ON v.MaGaDen = gDen.MaGa
            WHERE v.MaChuyenTau = @MaChuyenTau
              AND v.MaChoNgoi = @MaChoNgoi
              AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
              AND NOT (
                  IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) >= IIF(@KmDi < @KmDen, @KmDen, @KmDi)
                  OR IIF(gDi.LyTrinhKm > gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) <= IIF(@KmDi < @KmDen, @KmDi, @KmDen)
              )
        )
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'Chỗ ngồi đã có khách đặt trên cung chặng trùng lặp.', 16, 1);
            RETURN -2;
        END;

        INSERT INTO vantai.Ve (
            MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi,
            MaGaDi, MaGaDen,
            TenHanhKhach, CCCDHanhKhach,
            GiaVeGoc, SoTienGiam,
            TrangThai, ThoiDiemXuatVe
        ) VALUES (
            @MaDonVe, @MaVeCodeOutput, @MaChuyenTau, @MaChoNgoi,
            @MaGaDi, @MaGaDen,
            @TenHanhKhach, @CCCDHanhKhach,
            @GiaVeGoc, @SoTienGiam,
            'DA_DAT', SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 6. Hệ số sử dụng ghế: không tính toa đã ngừng bán (toa bị thay trước giờ tàu chạy) ------------
IF OBJECT_ID(N'baocao.vw_HanhTrinhChuyen', N'V') IS NOT NULL
    EXEC (N'
CREATE OR ALTER VIEW baocao.vw_HeSoSuDungGhe AS
SELECT ht.MaChuyenTau, ht.NgayChay, ht.SoHieuMacTau, ht.LoaiTau, ht.CuLyKm,
       g.SoGhe, ISNULL(b.SoVe, 0) AS SoVe,
       CAST(g.SoGhe * ht.CuLyKm AS DECIMAL(18, 2)) AS GheKmCungUng,
       CAST(ISNULL(b.GheKmDaBan, 0) AS DECIMAL(18, 2)) AS GheKmDaBan,
       CAST(CASE WHEN g.SoGhe * ht.CuLyKm > 0 THEN ISNULL(b.GheKmDaBan, 0) / (g.SoGhe * ht.CuLyKm) ELSE 0 END AS DECIMAL(9, 4)) AS HeSo
FROM baocao.vw_HanhTrinhChuyen ht
CROSS APPLY (SELECT COUNT(*) AS SoGhe
             FROM vantai.ToaXeKhach t JOIN vantai.ChoNgoi c ON c.MaToaXeKhach = t.MaToaXeKhach
             WHERE t.MaChuyenTau = ht.MaChuyenTau AND c.LaGhePhu = 0 AND t.NgungBan = 0) g
OUTER APPLY (SELECT COUNT(*) AS SoVe, SUM(ABS(gDen.LyTrinhKm - gDi.LyTrinhKm)) AS GheKmDaBan
             FROM vantai.Ve v
             JOIN hatang.Ga gDi ON gDi.MaGa = v.MaGaDi
             JOIN hatang.Ga gDen ON gDen.MaGa = v.MaGaDen
             WHERE v.MaChuyenTau = ht.MaChuyenTau AND v.TrangThai IN (''DA_DAT'', ''DA_LEN_TAU'')) b
WHERE ht.TrangThai <> ''DA_HUY'' AND g.SoGhe > 0;');
GO

-- Kết quả
SELECT COUNT(*) AS ToaBanVe,
       SUM(CASE WHEN MaToaXe IS NOT NULL THEN 1 ELSE 0 END) AS DaNoiToaThat,
       SUM(CASE WHEN MaGaNoi IS NOT NULL OR MaGaCat IS NOT NULL THEN 1 ELSE 0 END) AS CoPhamViGa,
       SUM(CASE WHEN NgungBan = 1 THEN 1 ELSE 0 END) AS NgungBan
FROM vantai.ToaXeKhach;
GO
SET NOEXEC OFF;
GO
