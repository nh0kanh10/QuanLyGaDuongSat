/* =========================================================================
   MIGRATION: Nhân sự — cột phiên bản chống ghi đè + trigger trùng giờ xét cả trưởng tàu
   Tác giả  : Đạt
   Ngày     : 11/10/2026 (Sprint 1, back-end Nhân sự)
   Bảng     : nhansu.NhanVien, nhansu.PhanCongKipLai (chủ bảng: Đạt)

   Lý do (Đạt chốt 11/10):
   1. PhienBan ROWVERSION cho NhanVien và PhanCongKipLai: Service ghi bằng
      UPDATE ... WHERE PhienBan = @pb; 0 dòng = người khác vừa sửa (KE_HOACH_BACK_END.md §2.5 mục 8).
   2. trg_PhanCongKipLai_CheckTrungGio bản gốc chỉ xét lái tàu / phụ lái. Bản này xét MỌI vị trí:
      - một người không thuộc kíp của hai chuyến KHÁC NHAU có khung giờ chồng nhau (cả trưởng tàu);
      - trong CÙNG một chuyến, một người chỉ ở một kíp — riêng trưởng tàu được ở nhiều kíp
        (trưởng tàu đi suốt chuyến, lái tàu / phụ lái đổi ở ga depot). Kíp của trưởng tàu
        không được chồng chặng: Service kiểm (cần lịch dừng ga, trigger không làm).
      Bỏ qua chuyến HOAN_THANH / DA_HUY như bản gốc. Vẫn RAISERROR + ROLLBACK như bản gốc
      để mã lỗi (50000) và cách xử lý ở tầng ứng dụng không đổi.

   Thứ tự: chạy sau script gốc. Seed S20261009_0830 đã đổi thứ tự (thay trưởng tàu SE19 / NA1
   trước khi đổi trạng thái kíp SE1 / SE4) nên chạy được cả trước lẫn sau migration này.

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

IF OBJECT_ID(N'nhansu.PhanCongKipLai', N'U') IS NULL OR OBJECT_ID(N'nhansu.NhanVien', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng nhansu.NhanVien / nhansu.PhanCongKipLai. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Cột phiên bản (SQL Server tự tăng mỗi lần dòng bị ghi)
IF COL_LENGTH(N'nhansu.NhanVien', N'PhienBan') IS NULL
    ALTER TABLE nhansu.NhanVien ADD PhienBan ROWVERSION;
GO
IF COL_LENGTH(N'nhansu.PhanCongKipLai', N'PhienBan') IS NULL
    ALTER TABLE nhansu.PhanCongKipLai ADD PhienBan ROWVERSION;
GO

-- 2. Trigger trùng giờ xét cả 3 vị trí
CREATE OR ALTER TRIGGER nhansu.trg_PhanCongKipLai_CheckTrungGio
ON nhansu.PhanCongKipLai
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        CROSS APPLY (VALUES (i.MaLaiTau, 'LAI_TAU'), (i.MaPhuLai, 'PHU_LAI'), (i.MaTruongTau, 'TRUONG_TAU'))
                    n(MaNhanVien, VaiTro)
        JOIN nhansu.PhanCongKipLai k ON k.MaPhanCong <> i.MaPhanCong
                                    AND n.MaNhanVien IN (k.MaLaiTau, k.MaPhuLai, k.MaTruongTau)
        JOIN vanhanh.ChuyenTau ct1 ON ct1.MaChuyenTau = i.MaChuyenTau
        JOIN vanhanh.ChuyenTau ct2 ON ct2.MaChuyenTau = k.MaChuyenTau
        WHERE ct1.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
          AND ct2.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
          AND (
                -- Hai chuyến khác nhau có khung giờ chồng nhau
                (i.MaChuyenTau <> k.MaChuyenTau
                 AND (CASE WHEN ct1.GioXuatPhatKH > ct2.GioXuatPhatKH THEN ct1.GioXuatPhatKH ELSE ct2.GioXuatPhatKH END)
                   < (CASE WHEN ct1.GioVeDichKH < ct2.GioVeDichKH THEN ct1.GioVeDichKH ELSE ct2.GioVeDichKH END))
                -- Cùng chuyến: chỉ trưởng tàu (ở cả hai kíp) mới được đi nhiều kíp
             OR (i.MaChuyenTau = k.MaChuyenTau
                 AND NOT (n.VaiTro = 'TRUONG_TAU' AND k.MaTruongTau = n.MaNhanVien))
              )
    )
    BEGIN
        RAISERROR(N'Xung đột nhân sự: một thành viên kíp đã được phân công cho chuyến tàu khác có khung giờ chồng lấn, hoặc đã ở kíp khác của cùng chuyến (chỉ trưởng tàu được đi nhiều kíp liên tiếp của một chuyến).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 3. Chỉ mục cho các cột người còn thiếu (script gốc mới có IX_FK_KipLai_MaLaiTau)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FK_KipLai_MaPhuLai'
               AND object_id = OBJECT_ID(N'nhansu.PhanCongKipLai'))
    CREATE NONCLUSTERED INDEX IX_FK_KipLai_MaPhuLai ON nhansu.PhanCongKipLai(MaPhuLai);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FK_KipLai_MaTruongTau'
               AND object_id = OBJECT_ID(N'nhansu.PhanCongKipLai'))
    CREATE NONCLUSTERED INDEX IX_FK_KipLai_MaTruongTau ON nhansu.PhanCongKipLai(MaTruongTau);
GO

SET NOEXEC OFF;
GO
PRINT N'Migration V20261011_1500 (Nhân sự: PhienBan + trigger trùng giờ cả trưởng tàu): xong.';
GO
