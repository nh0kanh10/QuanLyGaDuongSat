/* =========================================================================
   MIGRATION: Nhân sự — lịch sử mã nhân viên (đổi chức danh → cấp mã mới)
   Tác giả  : Đạt
   Ngày     : 12/10/2026 (Sprint 1, back-end Nhân sự)
   Bảng     : nhansu.LichSuMaNhanVien (mới), trigger trên nhansu.NhanVien (chủ bảng: Đạt)

   Lý do (Đạt chốt 12/10):
   Mã nhân viên theo chức danh (LT_ / PL_ / TT_ ...) nên đổi chức danh thì được cấp mã mới
   (PL_015 → LT_018). Trước đây mã cũ chỉ còn trong JSON của kiemtoan.NhatKyHeThong:
     - tra cứu theo mã cũ (sổ sách, biên bản, nhật ký phân kíp ghi mã dạng chữ) không ra người;
     - mã mới = số lớn nhất + 1 nên mã cũ lớn nhất của một tiền tố bị CẤP LẠI cho người khác.
   Bảng này giữ mọi lần đổi mã. Mã cũ không bao giờ cấp lại:
     - UNIQUE(MaNVCodeCu): một mã chỉ "nghỉ hưu" một lần;
     - trigger trg_NhanVien_KhongDungLaiMaCu: không gán cho hồ sơ nào một mã đã nằm trong lịch sử;
     - NhanSuService cấp mã kế tiếp tính cả mã trong bảng này.
   Chỉ ghi khi MÃ đổi; đổi đơn vị / hạng bằng mà giữ mã thì chỉ có trong NhatKyHeThong.

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

IF OBJECT_ID(N'nhansu.NhanVien', N'U') IS NULL OR OBJECT_ID(N'phanquyen.TaiKhoan', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng nhansu.NhanVien / phanquyen.TaiKhoan. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Bảng lịch sử đổi mã
IF OBJECT_ID(N'nhansu.LichSuMaNhanVien', N'U') IS NULL
BEGIN
    CREATE TABLE nhansu.LichSuMaNhanVien (
        MaLichSu        INT IDENTITY(1,1) PRIMARY KEY,
        MaNhanVien      INT             NOT NULL
            CONSTRAINT FK_LichSuMa_NhanVien REFERENCES nhansu.NhanVien(MaNhanVien),
        MaNVCodeCu      VARCHAR(20)     NOT NULL
            CONSTRAINT UQ_LichSuMa_MaCu UNIQUE,
        MaNVCodeMoi     VARCHAR(20)     NOT NULL,
        ChucDanhCu      NVARCHAR(50)    NOT NULL,
        ChucDanhMoi     NVARCHAR(50)    NOT NULL,
        DonViCu         NVARCHAR(100)   NOT NULL,
        DonViMoi        NVARCHAR(100)   NOT NULL,
        ThoiDiem        DATETIME2       NOT NULL
            CONSTRAINT DF_LichSuMa_ThoiDiem DEFAULT SYSDATETIME(),   -- giờ Việt Nam (quyết định Q3 11/10)
        MaTaiKhoan      INT             NULL
            CONSTRAINT FK_LichSuMa_TaiKhoan REFERENCES phanquyen.TaiKhoan(MaTaiKhoan),
        CONSTRAINT CK_LichSuMa_KhacMa CHECK (MaNVCodeCu <> MaNVCodeMoi)
    );
    CREATE NONCLUSTERED INDEX IX_LichSuMa_MaNhanVien ON nhansu.LichSuMaNhanVien(MaNhanVien);
END;
GO

-- 2. Không gán cho hồ sơ một mã đã "nghỉ hưu" (có trong lịch sử)
CREATE OR ALTER TRIGGER nhansu.trg_NhanVien_KhongDungLaiMaCu
ON nhansu.NhanVien
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(MaNVCode) RETURN;   -- đổi trạng thái lúc nhận ban / bàn giao không xét

    IF EXISTS (SELECT 1 FROM inserted i JOIN nhansu.LichSuMaNhanVien ls ON ls.MaNVCodeCu = i.MaNVCode)
    BEGIN
        RAISERROR(N'Mã nhân viên này đã từng được dùng (có trong lịch sử đổi mã) nên không cấp lại. Hãy nạp lại rồi lưu lại để hệ thống cấp mã khác.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

SET NOEXEC OFF;
GO
PRINT N'Migration V20261012_1100 (Nhân sự: lịch sử mã nhân viên): xong.';
GO
