/* =========================================================================
   MIGRATION: Bảo trì — chi phí, định mức suất tiêu hao, giờ Việt Nam, phiên bản phiếu
   Tác giả  : Đạt
   Ngày     : 24/10/2026 (Sprint 2)
   Bảng     : baotri.KhamXeKyThuat, baotri.NhatKyCapNhienLieu, baotri.NhatKyBaoDuong,
              phuongtien.DongDauMay (chủ bảng: Đạt)

   Quyết định 24/10 (Đạt chốt theo khuyến nghị):
     1. Suất tiêu hao SFC = số lít × 10.000 ÷ (tấn kéo × km) [lít / 10.000 tấn·km], Service tính rồi
        ghi vào SuatTieuHaoSFC + CanhBaoVuotMuc (trước đây nhập tay, seed 2.15 / 3.25 không suy ra được).
        Định mức theo dòng máy: cột mới DongDauMay.DinhMucSFC — D19E 90, D13E 100. ĐỊNH MỨC THAM KHẢO,
        chưa đối chiếu định mức chính thức của VNR.
     3. Chi phí (VNĐ):
          KhamXeKyThuat.ChiPhi, NhatKyBaoDuong.ChiPhi       nhập tay, mặc định 0;
          NhatKyCapNhienLieu.DonGiaVndLit                   đơn giá dầu nhập tay;
          NhatKyCapNhienLieu.ChiPhi                         cột tính = số lít × đơn giá (PERSISTED).
        Phiếu cũ không có số liệu chi phí -> 0 (không bịa số).
     5. Giờ Việt Nam như Nhân sự (11/10): đổi mặc định 3 cột thời điểm từ SYSUTCDATETIME() sang
        SYSDATETIME(). Các dòng có sẵn trước migration đều được ghi bằng mặc định giờ UTC (ứng dụng chưa
        từng ghi bảng baotri) -> cộng 7 giờ, làm ĐÚNG MỘT LẦN cùng lúc đổi mặc định.
     6. Phiếu cấp dầu / bảo dưỡng sửa được -> thêm PhienBan ROWVERSION (không ghi đè nhau).
        Biên bản khám xe không sửa (sai thì lập biên bản mới) nên không cần.
   Ngoài ra: tính lại SFC + cờ vượt mức của MỌI phiếu cấp dầu theo công thức trên.

   Thứ tự: chạy sau script gốc (không phụ thuộc seed). Chạy lại được nhiều lần.
   Không có USE: chạy bằng  sqlcmd -S . -E -I -f 65001 -d <TênCSDL> -i <file>
   ========================================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'Đang đứng ở CSDL hệ thống. Chạy lại với -d QuanLyDuongSatV2 (hoặc CSDL kiểm thử).', 16, 1);
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'baotri.KhamXeKyThuat', N'U') IS NULL OR OBJECT_ID(N'phuongtien.DongDauMay', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng baotri.* / phuongtien.DongDauMay. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

BEGIN TRANSACTION;

DECLARE @sql NVARCHAR(MAX);

-- 1. Định mức suất tiêu hao theo dòng máy (tham khảo)
IF COL_LENGTH(N'phuongtien.DongDauMay', N'DinhMucSFC') IS NULL
BEGIN
    EXEC (N'ALTER TABLE phuongtien.DongDauMay ADD DinhMucSFC DECIMAL(5,2) NOT NULL
              CONSTRAINT DF_DongDauMay_DinhMucSFC DEFAULT (95.00)
              CONSTRAINT CK_DongDauMay_DinhMucSFC CHECK (DinhMucSFC > 0);');
    EXEC (N'UPDATE phuongtien.DongDauMay SET DinhMucSFC = 90.00 WHERE MaDongCode = ''D19E'';
            UPDATE phuongtien.DongDauMay SET DinhMucSFC = 100.00 WHERE MaDongCode = ''D13E'';');
END;

-- 2. Chi phí khám xe
IF COL_LENGTH(N'baotri.KhamXeKyThuat', N'ChiPhi') IS NULL
    EXEC (N'ALTER TABLE baotri.KhamXeKyThuat ADD ChiPhi DECIMAL(14,0) NOT NULL
              CONSTRAINT DF_KhamXe_ChiPhi DEFAULT (0)
              CONSTRAINT CK_KhamXe_ChiPhi CHECK (ChiPhi >= 0);');

-- 3. Chi phí bảo dưỡng + phiên bản
IF COL_LENGTH(N'baotri.NhatKyBaoDuong', N'ChiPhi') IS NULL
    EXEC (N'ALTER TABLE baotri.NhatKyBaoDuong ADD ChiPhi DECIMAL(14,0) NOT NULL
              CONSTRAINT DF_BaoDuong_ChiPhi DEFAULT (0)
              CONSTRAINT CK_BaoDuong_ChiPhi CHECK (ChiPhi >= 0);');
IF COL_LENGTH(N'baotri.NhatKyBaoDuong', N'PhienBan') IS NULL
    EXEC (N'ALTER TABLE baotri.NhatKyBaoDuong ADD PhienBan ROWVERSION;');

-- 4. Đơn giá dầu + thành tiền (cột tính) + phiên bản
IF COL_LENGTH(N'baotri.NhatKyCapNhienLieu', N'DonGiaVndLit') IS NULL
    EXEC (N'ALTER TABLE baotri.NhatKyCapNhienLieu ADD DonGiaVndLit DECIMAL(10,0) NOT NULL
              CONSTRAINT DF_CapDau_DonGia DEFAULT (0)
              CONSTRAINT CK_CapDau_DonGia CHECK (DonGiaVndLit >= 0);');
IF COL_LENGTH(N'baotri.NhatKyCapNhienLieu', N'ChiPhi') IS NULL
    EXEC (N'ALTER TABLE baotri.NhatKyCapNhienLieu ADD ChiPhi AS
              CAST(ROUND(SoLitTraNap * DonGiaVndLit, 0) AS DECIMAL(16,0)) PERSISTED;');
IF COL_LENGTH(N'baotri.NhatKyCapNhienLieu', N'PhienBan') IS NULL
    EXEC (N'ALTER TABLE baotri.NhatKyCapNhienLieu ADD PhienBan ROWVERSION;');

-- 5. Giờ Việt Nam: bỏ mặc định giờ UTC (tên tự sinh), cộng 7 giờ cho dòng cũ, đặt mặc định có tên.
--    Đã có mặc định có tên = đã chạy -> bỏ qua (không cộng giờ lần nữa).
DECLARE @CotGio TABLE (Bang SYSNAME, Cot SYSNAME, TenMacDinh SYSNAME);
INSERT INTO @CotGio VALUES
    (N'baotri.KhamXeKyThuat',      N'ThoiDiemKham',      N'DF_KhamXe_ThoiDiemKham'),
    (N'baotri.NhatKyCapNhienLieu', N'ThoiDiemBomDau',    N'DF_CapDau_ThoiDiemBomDau'),
    (N'baotri.NhatKyBaoDuong',     N'ThoiDiemHoanThanh', N'DF_BaoDuong_ThoiDiemHoanThanh');

DECLARE @Bang SYSNAME, @Cot SYSNAME, @Ten SYSNAME;
DECLARE dong CURSOR LOCAL FAST_FORWARD FOR SELECT Bang, Cot, TenMacDinh FROM @CotGio;
OPEN dong;
FETCH NEXT FROM dong INTO @Bang, @Cot, @Ten;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints
                   WHERE parent_object_id = OBJECT_ID(@Bang) AND name = @Ten)
    BEGIN
        SET @sql = N'';
        SELECT @sql += N'ALTER TABLE ' + @Bang + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(@Bang) AND c.name = @Cot;

        SET @sql += N'UPDATE ' + @Bang + N' SET ' + QUOTENAME(@Cot) + N' = DATEADD(HOUR, 7, ' + QUOTENAME(@Cot) + N');'
                  + N'ALTER TABLE ' + @Bang + N' ADD CONSTRAINT ' + QUOTENAME(@Ten)
                  + N' DEFAULT (SYSDATETIME()) FOR ' + QUOTENAME(@Cot) + N';';
        EXEC sys.sp_executesql @sql;
    END;
    FETCH NEXT FROM dong INTO @Bang, @Cot, @Ten;
END;
CLOSE dong;
DEALLOCATE dong;

-- 6. Tính lại suất tiêu hao + cờ vượt định mức của mọi phiếu theo công thức (làm tròn 2 chữ số,
--    nửa trên làm tròn lên - BUS dùng MidpointRounding.AwayFromZero cho khớp).
EXEC (N'
UPDATE nl
SET SuatTieuHaoSFC = v.Sfc,
    CanhBaoVuotMuc = CASE WHEN v.Sfc > ddm.DinhMucSFC THEN 1 ELSE 0 END
FROM baotri.NhatKyCapNhienLieu nl
JOIN phuongtien.DauMay dm ON dm.MaDauMay = nl.MaDauMay
JOIN phuongtien.DongDauMay ddm ON ddm.MaDongDauMay = dm.MaDongDauMay
CROSS APPLY (SELECT CAST(ROUND(CAST(nl.SoLitTraNap AS DECIMAL(18,6)) * 10000
                               / (nl.TrongLuongKeoTan * nl.CuLyChayKm), 2) AS DECIMAL(18,2)) AS Sfc) v
WHERE v.Sfc <= 999.99
  AND (nl.SuatTieuHaoSFC <> v.Sfc OR nl.CanhBaoVuotMuc <> CASE WHEN v.Sfc > ddm.DinhMucSFC THEN 1 ELSE 0 END);');

COMMIT TRANSACTION;
GO

-- Kiểm tra sau khi chạy
SELECT MaDongCode, DinhMucSFC FROM phuongtien.DongDauMay ORDER BY MaDongCode;
SELECT nl.MaNhatKyDau, dm.SoHieuDauMay, nl.SuatTieuHaoSFC, nl.CanhBaoVuotMuc, nl.DonGiaVndLit, nl.ChiPhi, nl.ThoiDiemBomDau
FROM baotri.NhatKyCapNhienLieu nl JOIN phuongtien.DauMay dm ON dm.MaDauMay = nl.MaDauMay
ORDER BY nl.MaNhatKyDau;
PRINT N'V20261024_1000: xong (chi phí, định mức SFC, giờ Việt Nam, PhienBan phiếu cấp dầu / bảo dưỡng).';
GO
