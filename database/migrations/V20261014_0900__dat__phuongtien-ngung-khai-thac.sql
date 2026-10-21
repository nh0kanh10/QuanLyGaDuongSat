/* =========================================================================
   MIGRATION: Phương tiện — trạng thái "Ngừng khai thác" cho đầu máy và toa xe
   Tác giả  : Đạt
   Ngày     : 14/10/2026 (Sprint 2)
   Bảng     : phuongtien.DauMay, phuongtien.ToaXe (chủ bảng: Đạt)

   Lý do (Đạt chốt 14/10):
   Đầu máy / toa xe đã có lịch sử (đoàn tàu, phiếu cấp dầu, bảo dưỡng, vận đơn) không xóa được
   (khóa ngoại, và xóa thì mất hồ sơ an toàn). Trước đây CHECK chỉ có 3 trạng thái nên máy / toa
   thanh lý, hỏng hẳn vẫn nằm mãi trong danh sách như đang dùng. Thêm trạng thái NGUNG_KHAI_THAC:
     - vẫn giữ trong hồ sơ để tra cứu lịch sử;
     - không được đưa vào đoàn tàu (QuyTacLapTau báo LỖI, bãi toa / ô chọn đầu máy không hiện);
     - chỉ chuyển sang được khi không còn gán cho chuyến chưa hoàn thành (PhuongTienService);
     - đưa về "Sẵn sàng" / "Bảo dưỡng" được khi phục hồi.
   Bản ghi CHƯA có dữ liệu liên quan (nhập nhầm) thì xóa hẳn được — kiểm ở PhuongTienService.

   Ràng buộc CHECK trạng thái ở script gốc không đặt tên (SQL Server tự sinh CK__DauMay__TrangTha__...)
   nên tìm theo cột rồi bỏ, đặt lại có tên CK_DauMay_TrangThai / CK_ToaXe_TrangThai.

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

IF OBJECT_ID(N'phuongtien.DauMay', N'U') IS NULL OR OBJECT_ID(N'phuongtien.ToaXe', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng phuongtien.DauMay / phuongtien.ToaXe. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

BEGIN TRANSACTION;

DECLARE @sql NVARCHAR(MAX);

-- 1. Đầu máy: bỏ mọi CHECK đặt trên cột TrangThai (tên tự sinh hoặc tên cũ), đặt lại 4 trạng thái
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE parent_object_id = OBJECT_ID(N'phuongtien.DauMay')
                 AND name = N'CK_DauMay_TrangThai'
                 AND definition LIKE N'%NGUNG_KHAI_THAC%')
BEGIN
    SET @sql = N'';
    SELECT @sql += N'ALTER TABLE phuongtien.DauMay DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';'
    FROM sys.check_constraints cc
    JOIN sys.columns c ON c.object_id = cc.parent_object_id AND c.column_id = cc.parent_column_id
    WHERE cc.parent_object_id = OBJECT_ID(N'phuongtien.DauMay') AND c.name = N'TrangThai';
    EXEC sys.sp_executesql @sql;

    ALTER TABLE phuongtien.DauMay WITH CHECK ADD CONSTRAINT CK_DauMay_TrangThai
        CHECK (TrangThai IN ('SAN_SANG', 'DANG_CHAY', 'BAO_DUONG', 'NGUNG_KHAI_THAC'));
END;

-- 2. Toa xe: như trên
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE parent_object_id = OBJECT_ID(N'phuongtien.ToaXe')
                 AND name = N'CK_ToaXe_TrangThai'
                 AND definition LIKE N'%NGUNG_KHAI_THAC%')
BEGIN
    SET @sql = N'';
    SELECT @sql += N'ALTER TABLE phuongtien.ToaXe DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';'
    FROM sys.check_constraints cc
    JOIN sys.columns c ON c.object_id = cc.parent_object_id AND c.column_id = cc.parent_column_id
    WHERE cc.parent_object_id = OBJECT_ID(N'phuongtien.ToaXe') AND c.name = N'TrangThai';
    EXEC sys.sp_executesql @sql;

    ALTER TABLE phuongtien.ToaXe WITH CHECK ADD CONSTRAINT CK_ToaXe_TrangThai
        CHECK (TrangThai IN ('SAN_SANG', 'DA_NOI', 'BAO_DUONG', 'NGUNG_KHAI_THAC'));
END;

COMMIT TRANSACTION;
GO

-- Kiểm tra sau khi chạy
SELECT OBJECT_SCHEMA_NAME(parent_object_id) + N'.' + OBJECT_NAME(parent_object_id) AS Bang,
       name AS RangBuoc, definition AS DieuKien
FROM sys.check_constraints
WHERE name IN (N'CK_DauMay_TrangThai', N'CK_ToaXe_TrangThai');
PRINT N'V20261014_0900: xong (đầu máy / toa xe có thêm trạng thái NGUNG_KHAI_THAC).';
GO
