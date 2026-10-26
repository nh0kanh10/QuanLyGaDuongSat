/* =========================================================================
   MIGRATION: Nhân sự — ngày sinh nhân viên (ràng buộc đủ 18 tuổi)
   Tác giả  : Đạt
   Ngày     : 14/10/2026 (Sprint 2, ràng buộc dữ liệu cơ bản)
   Bảng     : nhansu.NhanVien (chủ bảng: Đạt)

   Lý do (Đạt chốt 14/10): ràng buộc "nhân viên phải trên 18 tuổi" cần ngày sinh, script gốc chưa có cột.
     - Thêm cột NgaySinh DATE.
     - Hồ sơ đang có (dữ liệu mẫu) chưa có ngày sinh → gán ngày sinh MẪU, 25–50 tuổi tại ngày lập hồ sơ
       (tính cố định theo MaNhanVien, chạy lại ra cùng kết quả). Sửa lại ngày thật trong màn hình Hồ sơ.
     - Sau đó NOT NULL + CHECK: đủ 18 tuổi tại ngày lập hồ sơ, sinh từ 1940.
   Giới hạn 18–62 tuổi tính theo NGÀY HÔM NAY kiểm ở BUS (RangBuocNhapLieu.KiemTraNgaySinh).

   Thứ tự: chạy sau script gốc + seed (seed chèn nhân viên chưa có ngày sinh). Chạy lại được nhiều lần.
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

IF OBJECT_ID(N'nhansu.NhanVien', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng nhansu.NhanVien. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Cột ngày sinh (tạm cho NULL để điền dữ liệu cũ)
IF COL_LENGTH(N'nhansu.NhanVien', N'NgaySinh') IS NULL
    ALTER TABLE nhansu.NhanVien ADD NgaySinh DATE NULL;
GO

-- 2. Ngày sinh mẫu cho hồ sơ cũ: 25 tuổi + 0..25 năm (theo MaNhanVien) tại ngày lập hồ sơ
UPDATE nhansu.NhanVien
SET NgaySinh = DATEADD(DAY, -((MaNhanVien * 2971) % 9131), DATEADD(YEAR, -25, CAST(NgayTao AS DATE)))
WHERE NgaySinh IS NULL;
GO

-- 3. Bắt buộc + CHECK đủ 18 tuổi tại ngày lập hồ sơ
IF COLUMNPROPERTY(OBJECT_ID(N'nhansu.NhanVien'), N'NgaySinh', 'AllowsNull') = 1
    ALTER TABLE nhansu.NhanVien ALTER COLUMN NgaySinh DATE NOT NULL;
GO

IF OBJECT_ID(N'nhansu.CK_NhanVien_NgaySinh', N'C') IS NULL
    ALTER TABLE nhansu.NhanVien WITH CHECK ADD CONSTRAINT CK_NhanVien_NgaySinh
        CHECK (NgaySinh >= '1940-01-01' AND NgaySinh <= DATEADD(YEAR, -18, CAST(NgayTao AS DATE)));
GO

PRINT N'V20261014_1000: xong (nhansu.NhanVien có NgaySinh, đủ 18 tuổi tại ngày lập hồ sơ).';
GO
