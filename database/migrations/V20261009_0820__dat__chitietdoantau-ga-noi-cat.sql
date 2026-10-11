/* =========================================================================
   MIGRATION: Thêm phạm vi chạy cho từng toa trong đoàn tàu (cắt / nối toa dọc đường)
   Tác giả  : Đạt
   Ngày     : 09/10/2026 (Sprint 1)
   Bảng     : vanhanh.ChiTietDoanTau (chủ bảng: Đạt)

   Lý do:
   - Đoàn tàu vẫn là MỘT danh sách toa theo thứ tự móc nối (ThuTuNoi).
   - Mỗi toa thêm phạm vi chạy MaGaNoi → MaGaCat:
       MaGaNoi NULL = toa có mặt từ ga đầu,
       MaGaCat NULL = toa chạy tới ga cuối,
       cả hai NULL  = chạy suốt (dữ liệu cũ giữ nguyên ý nghĩa).
   - Toa có mặt ở khu đoạn k (ga dừng k → k+1) khi vị trí ga nối <= k < vị trí ga cắt.
   - DoanTau.TongSoToa / TongChieuDaiM / TongTrongLuongTan = giá trị LỚN NHẤT trên các chặng.
   Chi tiết nghiệp vụ: CLAUDE.md §5.6, KE_HOACH_BACK_END.md §4.3 và §7.1.

   Ràng buộc chỉ kiểm được ở tầng Service (cần đọc lịch dừng ga, CHECK không làm được):
   - ga nối / ga cắt phải nằm trên hành trình của chuyến, ga nối đứng trước ga cắt;
   - ga cắt / nối phải đỗ >= 15 phút;
   - nhóm toa cắt / nối tại một ga phải liền khối ở cuối đoàn hoặc ngay sau đầu máy.

   Chạy lại được nhiều lần. Không có USE: chạy bằng  sqlcmd -S . -E -I -d <TênCSDL> -i <file>
   ========================================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'Đang đứng ở CSDL hệ thống. Chạy lại với -d QuanLyDuongSatV2 (hoặc CSDL kiểm thử).', 16, 1);
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'vanhanh.ChiTietDoanTau', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng vanhanh.ChiTietDoanTau. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Hai cột phạm vi chạy
IF COL_LENGTH(N'vanhanh.ChiTietDoanTau', N'MaGaNoi') IS NULL
    ALTER TABLE vanhanh.ChiTietDoanTau ADD MaGaNoi INT NULL;
GO
IF COL_LENGTH(N'vanhanh.ChiTietDoanTau', N'MaGaCat') IS NULL
    ALTER TABLE vanhanh.ChiTietDoanTau ADD MaGaCat INT NULL;
GO

-- 2. Khoá ngoại tới hatang.Ga
IF OBJECT_ID(N'vanhanh.FK_CTDoanTau_GaNoi', N'F') IS NULL
    ALTER TABLE vanhanh.ChiTietDoanTau
        ADD CONSTRAINT FK_CTDoanTau_GaNoi FOREIGN KEY (MaGaNoi) REFERENCES hatang.Ga(MaGa);
GO
IF OBJECT_ID(N'vanhanh.FK_CTDoanTau_GaCat', N'F') IS NULL
    ALTER TABLE vanhanh.ChiTietDoanTau
        ADD CONSTRAINT FK_CTDoanTau_GaCat FOREIGN KEY (MaGaCat) REFERENCES hatang.Ga(MaGa);
GO

-- 3. Một toa không nối và cắt ở cùng một ga
IF OBJECT_ID(N'vanhanh.CK_CTDoanTau_GaNoiKhacGaCat', N'C') IS NULL
    ALTER TABLE vanhanh.ChiTietDoanTau
        ADD CONSTRAINT CK_CTDoanTau_GaNoiKhacGaCat
        CHECK (MaGaNoi IS NULL OR MaGaCat IS NULL OR MaGaNoi <> MaGaCat);
GO

-- 4. Chỉ mục khoá ngoại (cùng quy ước IX_FK_* của script gốc)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FK_CTDoanTau_MaGaNoi'
               AND object_id = OBJECT_ID(N'vanhanh.ChiTietDoanTau'))
    CREATE NONCLUSTERED INDEX IX_FK_CTDoanTau_MaGaNoi ON vanhanh.ChiTietDoanTau(MaGaNoi);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FK_CTDoanTau_MaGaCat'
               AND object_id = OBJECT_ID(N'vanhanh.ChiTietDoanTau'))
    CREATE NONCLUSTERED INDEX IX_FK_CTDoanTau_MaGaCat ON vanhanh.ChiTietDoanTau(MaGaCat);
GO

SET NOEXEC OFF;
GO
PRINT N'Migration V20261009_0820 (ChiTietDoanTau.MaGaNoi / MaGaCat): xong.';
GO
