/* =========================================================================
   MIGRATION: Quản trị & phân quyền — đăng nhập, khoá sau 5 lần sai, danh mục quyền / vai trò
   Tác giả  : Đạt
   Ngày     : 08/11/2026 (Sprint 3)
   Bảng     : phanquyen.TaiKhoan, Quyen, VaiTro, VaiTro_Quyen, LichSuDangNhap; kiemtoan.NhatKyHeThong (chủ: Đạt)

   Lý do (Đạt chốt 08/11, theo khuyến nghị):
   1. Khoá tài khoản sau 5 lần sai liên tiếp (báo cáo FR-7.1): thêm SoLanSaiLienTiep + KhoaDenLuc (giờ UTC,
      như các cột thời gian khác của phanquyen). Khoá 15 phút rồi tự mở; quản trị viên mở tay được.
   2. PhaiDoiMatKhau: quản trị viên đặt lại mật khẩu -> người dùng phải đổi ở lần đăng nhập kế tiếp.
   3. PhienBan ROWVERSION: hai quản trị viên cùng sửa một tài khoản thì người lưu sau được báo.
   4. Danh mục quyền: mỗi phân hệ 1 quyền XEM + các quyền GHI -> khớp ma trận F / R / - của báo cáo
      (R = có quyền xem, không có quyền ghi). Tab hiện khi có quyền xem HOẶC một quyền ghi của phân hệ.
      Gồm 8 quyền gốc + 8 quyền ghi đã dùng trong mã (MaQuyen trong DTO/Common/PhienLamViec.cs)
      + 8 quyền xem mới = 24 quyền.
   5. Vai trò mới: LAP_TAU, NHAN_SU, PHAN_CONG_KIP, QUAN_LY (SRS chức năng của Đạt). Gán quyền cho cả
      10 vai trò; ADMIN có mọi quyền.
   6. Chỉ mục cho màn Lịch sử đăng nhập / Nhật ký hệ thống (lọc theo thời gian, tài khoản, bảng).

   Mật khẩu: chuỗi băm PBKDF2-SHA256 (BUS/Services/MatKhauHelper.cs). Mật khẩu demo đặt ở seed
   S20261108_1100 (chuỗi băm BCrypt cũ trong script gốc không ai biết mật khẩu gốc).

   Thứ tự: chạy sau script gốc (không phụ thuộc seed). Chạy lại được nhiều lần: chỉ thêm cái còn thiếu,
   không xoá quyền quản trị viên đã gán tay.
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

IF OBJECT_ID(N'phanquyen.TaiKhoan', N'U') IS NULL OR OBJECT_ID(N'phanquyen.Quyen', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có schema phanquyen. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Cột mới của TaiKhoan -------------------------------------------------------------------
IF COL_LENGTH(N'phanquyen.TaiKhoan', N'SoLanSaiLienTiep') IS NULL
    ALTER TABLE phanquyen.TaiKhoan ADD SoLanSaiLienTiep INT NOT NULL
        CONSTRAINT DF_TaiKhoan_SoLanSai DEFAULT 0
        CONSTRAINT CK_TaiKhoan_SoLanSai CHECK (SoLanSaiLienTiep >= 0);
GO
IF COL_LENGTH(N'phanquyen.TaiKhoan', N'KhoaDenLuc') IS NULL
    ALTER TABLE phanquyen.TaiKhoan ADD KhoaDenLuc DATETIME2 NULL;      -- giờ UTC; NULL = không khoá
GO
IF COL_LENGTH(N'phanquyen.TaiKhoan', N'PhaiDoiMatKhau') IS NULL
    ALTER TABLE phanquyen.TaiKhoan ADD PhaiDoiMatKhau BIT NOT NULL
        CONSTRAINT DF_TaiKhoan_PhaiDoiMatKhau DEFAULT 0;
GO
IF COL_LENGTH(N'phanquyen.TaiKhoan', N'PhienBan') IS NULL
    ALTER TABLE phanquyen.TaiKhoan ADD PhienBan ROWVERSION;
GO

-- 2. Chỉ mục ---------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichSuDangNhap_ThoiDiem'
                 AND object_id = OBJECT_ID(N'phanquyen.LichSuDangNhap'))
    CREATE INDEX IX_LichSuDangNhap_ThoiDiem ON phanquyen.LichSuDangNhap (ThoiDiemDangNhap DESC)
        INCLUDE (MaTaiKhoan, TenDangNhap, ThanhCong);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichSuDangNhap_TaiKhoan'
                 AND object_id = OBJECT_ID(N'phanquyen.LichSuDangNhap'))
    CREATE INDEX IX_LichSuDangNhap_TaiKhoan ON phanquyen.LichSuDangNhap (MaTaiKhoan, ThoiDiemDangNhap DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhatKyHeThong_ThoiDiem'
                 AND object_id = OBJECT_ID(N'kiemtoan.NhatKyHeThong'))
    CREATE INDEX IX_NhatKyHeThong_ThoiDiem ON kiemtoan.NhatKyHeThong (ThoiDiem DESC)
        INCLUDE (MaTaiKhoan, HanhDong, TenBang, KhoaChinhRecord);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhatKyHeThong_TaiKhoan'
                 AND object_id = OBJECT_ID(N'kiemtoan.NhatKyHeThong'))
    CREATE INDEX IX_NhatKyHeThong_TaiKhoan ON kiemtoan.NhatKyHeThong (MaTaiKhoan, ThoiDiem DESC);
GO

BEGIN TRANSACTION;

-- 3. Danh mục quyền (thêm cái còn thiếu, cập nhật tên / nhóm / mô tả cho thống nhất) -----------
DECLARE @Quyen TABLE (Ma VARCHAR(50) PRIMARY KEY, Ten NVARCHAR(100), Nhom VARCHAR(50), MoTa NVARCHAR(255));
INSERT INTO @Quyen VALUES
-- Quản trị
('SYS_CONFIG',               N'Quản trị hệ thống',                 'HE_THONG',    N'Tài khoản, vai trò & quyền, lịch sử đăng nhập, nhật ký hệ thống'),
('REPORT_VIEW',              N'Xem báo cáo thống kê',              'BAO_CAO',     N'Trang báo cáo tổng hợp: doanh thu, hệ số sử dụng ghế, tỷ lệ đúng giờ'),
('AI_ADVISOR',               N'Dùng trợ lý AI',                    'AI',          N'Tư vấn vé, trợ lý điều độ'),
-- Mạng lưới & lịch trình (A Bá)
('NETWORK_VIEW',             N'Xem mạng lưới & lịch trình',        'DIEU_DO',     N'Xem ga, khu gian, mác tàu, lịch trình, biểu đồ chạy tàu'),
('INFRA_EDIT',               N'Sửa hạ tầng & mác tàu',             'DIEU_DO',     N'Thêm / sửa ga, đường ray, khu gian, đường ngang, mác tàu mẫu'),
('TRAIN_SCHEDULE_EDIT',      N'Lập biểu đồ chạy tàu',              'DIEU_DO',     N'Lập / sửa chuyến tàu, lịch dừng ga, xếp tránh'),
('BLOCK_ORDER_APPROVE',      N'Duyệt lệnh phong tỏa',              'DIEU_DO',     N'Phát lệnh cấm hoặc hạn chế tốc độ khu gian'),
('DISPATCH_INCIDENT_REPORT', N'Ghi nhận sự cố chậm giờ',           'DIEU_DO',     N'Ghi giờ thực tế, lý do và số phút chậm'),
-- Kinh doanh vận tải (A Bá)
('TICKET_VIEW',              N'Xem bán vé & soát vé',              'VAN_TAI',     N'Tra cứu vé, sơ đồ ghế, danh sách hành khách'),
('TICKET_ISSUE',             N'Bán và in vé tàu',                  'VAN_TAI',     N'Đặt vé, đổi ghế, in thẻ lên tàu'),
('TICKET_REFUND',            N'Hoàn và hủy vé tàu',                'VAN_TAI',     N'Trả vé và hoàn tiền'),
('TICKET_CHECK',             N'Soát vé lên tàu',                   'VAN_TAI',     N'Xác nhận / hủy soát vé tại ga'),
('CARGO_VIEW',               N'Xem vận đơn hàng hóa',              'VAN_TAI',     N'Tra cứu vận đơn, tiến độ vận chuyển'),
('CARGO_DISPATCH',           N'Lập vận đơn hàng hóa',              'VAN_TAI',     N'Nhận hàng hóa, xe máy, container; cập nhật trạng thái'),
-- Kỹ thuật & đoàn tàu (Đạt)
('VEHICLE_VIEW',             N'Xem phương tiện',                   'PHUONG_TIEN', N'Xem đầu máy, toa xe, đoàn tàu, sơ đồ chỗ ngồi'),
('VEHICLE_EDIT',             N'Quản lý đầu máy & toa xe',          'PHUONG_TIEN', N'Thêm / sửa / xóa / ngừng khai thác đầu máy, toa xe'),
('TRAIN_FORMATION_EDIT',     N'Lập đoàn tàu',                      'PHUONG_TIEN', N'Lập / sửa đoàn tàu, cắt / nối toa dọc đường'),
('MAINT_VIEW',               N'Xem bảo trì & cảnh báo',            'KY_THUAT',    N'Xem biên bản khám xe, nhiên liệu, bảo dưỡng, cảnh báo, chi phí'),
('TECH_SAFETY_APPROVE',      N'Nghiệm thu khám xe',                'KY_THUAT',    N'Lập biên bản khám xe, duyệt an toàn đoàn tàu'),
('MAINTENANCE_LOG',          N'Ghi nhật ký bảo trì',               'KY_THUAT',    N'Cấp phát nhiên liệu, ghi nhận bảo dưỡng định kỳ'),
-- Nhân sự (Đạt)
('STAFF_VIEW',               N'Xem nhân sự & lịch ca',             'NHAN_SU',     N'Xem hồ sơ nhân viên, kíp lái, lịch ca'),
('STAFF_EDIT',               N'Quản lý hồ sơ nhân viên',           'NHAN_SU',     N'Thêm / sửa / xóa hồ sơ, cho nghỉ việc'),
('CREW_ASSIGN',              N'Phân công kíp lái',                 'NHAN_SU',     N'Lập / sửa / hủy kíp lái theo chặng'),
('CREW_CHECKIN',             N'Kiểm tra lên ban & giao nhận ca',   'NHAN_SU',     N'Ghi kiểm tra lên ban, nhận ban, bàn giao');

MERGE phanquyen.Quyen AS q
USING @Quyen AS s ON q.MaQuyenCode = s.Ma
WHEN MATCHED AND (q.TenQuyen <> s.Ten OR q.NhomQuyen <> s.Nhom OR ISNULL(q.MoTa, N'') <> s.MoTa)
    THEN UPDATE SET TenQuyen = s.Ten, NhomQuyen = s.Nhom, MoTa = s.MoTa
WHEN NOT MATCHED BY TARGET
    THEN INSERT (MaQuyenCode, TenQuyen, NhomQuyen, MoTa) VALUES (s.Ma, s.Ten, s.Nhom, s.MoTa);

-- 4. Vai trò mới ---------------------------------------------------------------------------------
DECLARE @VaiTro TABLE (Ma VARCHAR(30) PRIMARY KEY, Ten NVARCHAR(100), MoTa NVARCHAR(255));
INSERT INTO @VaiTro VALUES
('LAP_TAU',       N'Quản đốc lập tàu',          N'Quản lý đầu máy, toa xe; lập đoàn tàu, cắt / nối toa'),
('NHAN_SU',       N'Cán bộ nhân sự',            N'Quản lý hồ sơ nhân viên vận hành'),
('PHAN_CONG_KIP', N'Quản đốc kíp lái',          N'Phân công kíp lái, kiểm tra lên ban, giao nhận ca'),
('QUAN_LY',       N'Cán bộ quản lý',            N'Xem toàn bộ hệ thống và báo cáo thống kê, không sửa dữ liệu nghiệp vụ');

INSERT INTO phanquyen.VaiTro (MaVaiTroCode, TenVaiTro, MoTa)
SELECT s.Ma, s.Ten, s.MoTa FROM @VaiTro s
WHERE NOT EXISTS (SELECT 1 FROM phanquyen.VaiTro v WHERE v.MaVaiTroCode = s.Ma);

-- 5. Gán quyền cho vai trò (chỉ thêm cặp còn thiếu) ------------------------------------------------
DECLARE @Gan TABLE (VaiTro VARCHAR(30), Quyen VARCHAR(50), PRIMARY KEY (VaiTro, Quyen));
INSERT INTO @Gan VALUES
('DIEU_DO', 'NETWORK_VIEW'), ('DIEU_DO', 'TRAIN_SCHEDULE_EDIT'), ('DIEU_DO', 'BLOCK_ORDER_APPROVE'),
('DIEU_DO', 'DISPATCH_INCIDENT_REPORT'), ('DIEU_DO', 'VEHICLE_VIEW'), ('DIEU_DO', 'MAINT_VIEW'),
('DIEU_DO', 'STAFF_VIEW'), ('DIEU_DO', 'TICKET_VIEW'), ('DIEU_DO', 'REPORT_VIEW'), ('DIEU_DO', 'AI_ADVISOR'),

('TRUONG_GA', 'NETWORK_VIEW'), ('TRUONG_GA', 'INFRA_EDIT'), ('TRUONG_GA', 'TICKET_VIEW'),
('TRUONG_GA', 'TICKET_CHECK'), ('TRUONG_GA', 'CARGO_VIEW'),

('BAN_VE', 'NETWORK_VIEW'), ('BAN_VE', 'TICKET_VIEW'), ('BAN_VE', 'TICKET_ISSUE'), ('BAN_VE', 'TICKET_REFUND'),
('BAN_VE', 'TICKET_CHECK'), ('BAN_VE', 'CARGO_VIEW'), ('BAN_VE', 'CARGO_DISPATCH'), ('BAN_VE', 'AI_ADVISOR'),

('LAI_TAU', 'NETWORK_VIEW'), ('LAI_TAU', 'STAFF_VIEW'), ('LAI_TAU', 'MAINT_VIEW'),

('KY_THUAT', 'NETWORK_VIEW'), ('KY_THUAT', 'VEHICLE_VIEW'), ('KY_THUAT', 'MAINT_VIEW'),
('KY_THUAT', 'TECH_SAFETY_APPROVE'), ('KY_THUAT', 'MAINTENANCE_LOG'),

('LAP_TAU', 'NETWORK_VIEW'), ('LAP_TAU', 'VEHICLE_VIEW'), ('LAP_TAU', 'VEHICLE_EDIT'),
('LAP_TAU', 'TRAIN_FORMATION_EDIT'), ('LAP_TAU', 'MAINT_VIEW'),

('NHAN_SU', 'NETWORK_VIEW'), ('NHAN_SU', 'STAFF_VIEW'), ('NHAN_SU', 'STAFF_EDIT'),

('PHAN_CONG_KIP', 'NETWORK_VIEW'), ('PHAN_CONG_KIP', 'STAFF_VIEW'), ('PHAN_CONG_KIP', 'CREW_ASSIGN'),
('PHAN_CONG_KIP', 'CREW_CHECKIN'), ('PHAN_CONG_KIP', 'VEHICLE_VIEW'),

('QUAN_LY', 'NETWORK_VIEW'), ('QUAN_LY', 'TICKET_VIEW'), ('QUAN_LY', 'CARGO_VIEW'), ('QUAN_LY', 'VEHICLE_VIEW'),
('QUAN_LY', 'MAINT_VIEW'), ('QUAN_LY', 'STAFF_VIEW'), ('QUAN_LY', 'REPORT_VIEW'), ('QUAN_LY', 'AI_ADVISOR');

INSERT INTO phanquyen.VaiTro_Quyen (MaVaiTro, MaQuyen)
SELECT v.MaVaiTro, q.MaQuyen
FROM @Gan g
JOIN phanquyen.VaiTro v ON v.MaVaiTroCode = g.VaiTro
JOIN phanquyen.Quyen q ON q.MaQuyenCode = g.Quyen
WHERE NOT EXISTS (SELECT 1 FROM phanquyen.VaiTro_Quyen x WHERE x.MaVaiTro = v.MaVaiTro AND x.MaQuyen = q.MaQuyen);

-- ADMIN: mọi quyền
INSERT INTO phanquyen.VaiTro_Quyen (MaVaiTro, MaQuyen)
SELECT v.MaVaiTro, q.MaQuyen
FROM phanquyen.VaiTro v CROSS JOIN phanquyen.Quyen q
WHERE v.MaVaiTroCode = 'ADMIN'
  AND NOT EXISTS (SELECT 1 FROM phanquyen.VaiTro_Quyen x WHERE x.MaVaiTro = v.MaVaiTro AND x.MaQuyen = q.MaQuyen);

COMMIT TRANSACTION;
GO

-- Kết quả
SELECT v.MaVaiTroCode AS VaiTro, COUNT(vq.MaQuyen) AS SoQuyen
FROM phanquyen.VaiTro v LEFT JOIN phanquyen.VaiTro_Quyen vq ON vq.MaVaiTro = v.MaVaiTro
GROUP BY v.MaVaiTroCode ORDER BY v.MaVaiTroCode;
GO
SET NOEXEC OFF;
GO
