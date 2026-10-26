/* =========================================================================
   SEED SPRINT 2 — DỮ LIỆU MẪU BẢO TRÌ (phần của Đạt)
   Tác giả  : Đạt
   Ngày     : 24/10/2026

   Thứ tự chạy (trên CSDL vừa dựng lại):
     1. script_tao_csdl_v2.sql
     2. migrations\V20261009_0820 → seed\S20261009_0830 → migrations\V20261011_1500, V20261012_1100,
        V20261014_0900, V20261014_1000
     3. migrations\V20261024_1000__dat__baotri-chiphi-dinhmuc-giovn.sql
     4. file này
     5. tests\T20261009_0840__dat__kiem-tra-du-lieu-sprint1.sql
   Lệnh: sqlcmd -S . -E -I -f 65001 -d QuanLyDuongSatV2 -i <file>

   Chạy lại: thấy đã có tài khoản nv_hung thì bỏ qua, không ghi gì. Mọi thứ nằm trong MỘT giao dịch.
   Mốc thời gian: ngày của chuyến SE19 (như seed Sprint 1); giờ không được vượt quá giờ máy chủ lúc chạy
   (phiếu "chiều nay" mà chạy buổi sáng thì lùi về trước giờ chạy vài chục phút).

   ─────────────────────────────────────────────────────────────────────────
   NỘI DUNG (số liệu mẫu, đơn giá / chi phí là số tham khảo)
   A. 2 nhân viên khám xe KX_002, KX_003 (Trạm Giáp Bát) + tài khoản nv_hung, nv_son vai trò KY_THUAT
      (cùng mật khẩu băm với nv_dung). Người ký phiếu bảo trì = tài khoản KY_THUAT (quyết định 24/10).
   B. Cấp nhiên liệu: đơn giá 21.000 đ/lít cho 2 phiếu gốc + 8 phiếu mới. D19E-902 tiêu hao tăng dần
      82 → 87 → 119 (phiếu gốc, vượt định mức); D13E-701 sát định mức; D19E-906 sát định mức.
   C. Bảo dưỡng: 3 phiếu đầu máy cũ (D19E-901, D19E-903, D19E-902) + 4 phiếu toa. D13E-701 cố ý KHÔNG có
      lịch sử -> quá hạn R2 (đang kéo NA1: cảnh báo tổng hợp, chặn duyệt + xuất bến - demo "ghi R2").
   D. Biên bản khám xe: chi phí cho 2 biên bản gốc; thêm SE2, SE4 (đạt), SE19 (chưa xả vệ sinh -> không đạt).
      NA1 chưa khám (demo lập biên bản -> duyệt -> xuất bến).
   E. Cờ duyệt an toàn khớp luật mới: chuyến chưa xuất phát có biên bản không đạt / chưa khám thì chưa duyệt
      (SE19, NA1 -> 0; SE3 vốn 0).
   ========================================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'Đang đứng ở CSDL hệ thống. Chạy lại với -d QuanLyDuongSatV2 (hoặc CSDL kiểm thử).', 16, 1);
    SET NOEXEC ON;
END;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/* =========================================================================
   0. ĐIỀU KIỆN TRƯỚC KHI CHẠY
   ========================================================================= */
IF COL_LENGTH(N'baotri.NhatKyCapNhienLieu', N'DonGiaVndLit') IS NULL
BEGIN
    RAISERROR(N'Chưa chạy migration V20261024_1000 (chi phí, định mức SFC).', 16, 1);
    RETURN;
END;
IF NOT EXISTS (SELECT 1 FROM nhansu.NhanVien WHERE MaNVCode = 'LT_001')
   OR NOT EXISTS (SELECT 1 FROM phuongtien.DauMay WHERE SoHieuDauMay = 'D19E-906')
BEGIN
    RAISERROR(N'Chưa chạy seed Sprint 1 (S20261009_0830).', 16, 1);
    RETURN;
END;
IF COL_LENGTH(N'nhansu.NhanVien', N'NgaySinh') IS NULL
BEGIN
    RAISERROR(N'Chưa chạy migration V20261014_1000 (ngày sinh nhân viên).', 16, 1);
    RETURN;
END;
IF EXISTS (SELECT 1 FROM phanquyen.TaiKhoan WHERE TenDangNhap = 'nv_hung')
BEGIN
    PRINT N'Seed S20261024_1100 đã chạy trên CSDL này (đã có nv_hung) -> bỏ qua, không ghi gì.';
    RETURN;
END;

DECLARE @NgayNay DATE = (
    SELECT TOP (1) ct.NgayXuatPhat
    FROM vanhanh.ChuyenTau ct
    JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau
    WHERE m.SoHieuMacTau = 'SE19'
    ORDER BY ct.NgayXuatPhat DESC);
DECLARE @T0 DATETIME2 = CAST(@NgayNay AS DATETIME2);
DECLARE @BayGio DATETIME2 = SYSDATETIME();

DECLARE @DoanSE2 INT, @DoanSE4 INT, @DoanSE19 INT, @DoanNA1 INT, @DoanSE1 INT, @DoanSE3 INT;
SELECT @DoanSE1  = dt.MaDoanTau FROM vanhanh.DoanTau dt JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau WHERE m.SoHieuMacTau = 'SE1'  AND ct.NgayXuatPhat = @NgayNay;
SELECT @DoanSE2  = dt.MaDoanTau FROM vanhanh.DoanTau dt JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau WHERE m.SoHieuMacTau = 'SE2'  AND ct.NgayXuatPhat = DATEADD(DAY, -1, @NgayNay);
SELECT @DoanSE3  = dt.MaDoanTau FROM vanhanh.DoanTau dt JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau WHERE m.SoHieuMacTau = 'SE3'  AND ct.NgayXuatPhat = DATEADD(DAY, 1, @NgayNay);
SELECT @DoanSE4  = dt.MaDoanTau FROM vanhanh.DoanTau dt JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau WHERE m.SoHieuMacTau = 'SE4'  AND ct.NgayXuatPhat = @NgayNay;
SELECT @DoanSE19 = dt.MaDoanTau FROM vanhanh.DoanTau dt JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau WHERE m.SoHieuMacTau = 'SE19' AND ct.NgayXuatPhat = @NgayNay;
SELECT @DoanNA1  = dt.MaDoanTau FROM vanhanh.DoanTau dt JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau WHERE m.SoHieuMacTau = 'NA1'  AND ct.NgayXuatPhat = @NgayNay;

IF @NgayNay IS NULL OR @DoanSE1 IS NULL OR @DoanSE2 IS NULL OR @DoanSE3 IS NULL OR @DoanSE4 IS NULL OR @DoanSE19 IS NULL OR @DoanNA1 IS NULL
BEGIN
    RAISERROR(N'Không tìm thấy đủ 6 đoàn tàu SE1 / SE2 / SE3 / SE4 / SE19 / NA1 của dữ liệu gốc.', 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;

/* =========================================================================
   A. NHÂN VIÊN KHÁM XE + TÀI KHOẢN KỸ THUẬT
   ========================================================================= */
DECLARE @ChucDanhKX NVARCHAR(50), @DonViKX NVARCHAR(100), @HashMau VARCHAR(255);
SELECT @ChucDanhKX = ChucDanh, @DonViKX = DonViChuQuan FROM nhansu.NhanVien WHERE MaNVCode = 'KX_001';
SELECT @HashMau = MatKhauHash FROM phanquyen.TaiKhoan WHERE TenDangNhap = 'nv_dung';

INSERT INTO nhansu.NhanVien (MaNVCode, HoTen, SoDienThoai, ChucDanh, HangBangLai, HanKhamSucKhoe, DonViChuQuan, TrangThai, NgayTao, NgaySinh)
VALUES ('KX_002', N'Trần Quốc Hưng', '0912340021', @ChucDanhKX, NULL, DATEADD(DAY, 300, @NgayNay), @DonViKX, 'SAN_SANG', SYSDATETIME(), '1988-06-14'),
       ('KX_003', N'Lê Thanh Sơn',   '0912340022', @ChucDanhKX, NULL, DATEADD(DAY, 420, @NgayNay), @DonViKX, 'SAN_SANG', SYSDATETIME(), '1992-11-03');

INSERT INTO phanquyen.TaiKhoan (TenDangNhap, MatKhauHash, HoTenHienThi, Email, MaNhanVien)
SELECT v.Ten, @HashMau, v.HoTen, v.Email, nv.MaNhanVien
FROM (VALUES ('nv_hung', N'Trần Quốc Hưng', 'hung.tq@vr.com.vn', 'KX_002'),
             ('nv_son',  N'Lê Thanh Sơn',   'son.lt@vr.com.vn',  'KX_003')) v(Ten, HoTen, Email, MaNV)
JOIN nhansu.NhanVien nv ON nv.MaNVCode = v.MaNV;

INSERT INTO phanquyen.TaiKhoan_VaiTro (MaTaiKhoan, MaVaiTro)
SELECT tk.MaTaiKhoan, vt.MaVaiTro
FROM phanquyen.TaiKhoan tk, phanquyen.VaiTro vt
WHERE tk.TenDangNhap IN ('nv_hung', 'nv_son') AND vt.MaVaiTroCode = 'KY_THUAT';

DECLARE @Dung INT = (SELECT MaTaiKhoan FROM phanquyen.TaiKhoan WHERE TenDangNhap = 'nv_dung');
DECLARE @Hung INT = (SELECT MaTaiKhoan FROM phanquyen.TaiKhoan WHERE TenDangNhap = 'nv_hung');
DECLARE @Son  INT = (SELECT MaTaiKhoan FROM phanquyen.TaiKhoan WHERE TenDangNhap = 'nv_son');

/* =========================================================================
   B. CẤP NHIÊN LIỆU (SFC + cờ vượt mức tính như BaoTriService: làm tròn 2 chữ số, nửa trên làm tròn lên)
   ========================================================================= */
UPDATE baotri.NhatKyCapNhienLieu SET DonGiaVndLit = 21000 WHERE DonGiaVndLit = 0;

DECLARE @CapDau TABLE (SoHieu VARCHAR(20), NoiCap NVARCHAR(100), Lit DECIMAL(8,2), Tan DECIMAL(7,2), Km DECIMAL(6,1),
                       NguoiCap INT, PhutTuT0 INT, DonGia DECIMAL(10,0));
INSERT INTO @CapDau VALUES
    ('D19E-902', N'Trạm dầu Giáp Bát', 3000, 530, 690, @Hung, -5 * 1440 + 340,  21000),   -- 82.03
    ('D19E-902', N'Trạm dầu Vinh',     3300, 540, 700, @Hung, -3 * 1440 + 850,  20500),   -- 87.30
    ('D19E-901', N'Trạm dầu Sài Gòn',  3100, 560, 720, @Son,  -2 * 1440 + 1100, 21000),   -- 76.88
    ('D19E-903', N'Trạm dầu Đà Nẵng',   780, 580, 180, @Dung, -1 * 1440 + 1260, 21000),   -- 74.71
    ('D13E-701', N'Trạm dầu Vinh',     1850, 380, 530, @Hung, -4 * 1440 + 540,  20500),   -- 91.86
    ('D13E-701', N'Trạm dầu Giáp Bát', 1900, 380, 520, @Hung, -1 * 1440 + 1300, 21000),   -- 96.15 sát định mức
    ('D19E-905', N'Trạm dầu Sài Gòn',  2900, 606, 650, @Son,  -1 * 1440 + 360,  21000),   -- 73.62
    ('D19E-906', N'Trạm dầu Vinh',     2200, 512, 480, @Dung, -1 * 1440 + 900,  21000);   -- 89.52 sát định mức

INSERT INTO baotri.NhatKyCapNhienLieu (MaDauMay, NoiCapDau, SoLitTraNap, TrongLuongKeoTan, CuLyChayKm,
                                       SuatTieuHaoSFC, CanhBaoVuotMuc, MaNguoiCap, ThoiDiemBomDau, DonGiaVndLit)
SELECT dm.MaDauMay, c.NoiCap, c.Lit, c.Tan, c.Km, v.Sfc,
       CASE WHEN v.Sfc > ddm.DinhMucSFC THEN 1 ELSE 0 END,
       c.NguoiCap,
       CASE WHEN DATEADD(MINUTE, c.PhutTuT0, @T0) > @BayGio
            THEN DATEADD(MINUTE, -30 - ROW_NUMBER() OVER (ORDER BY c.PhutTuT0) * 7, @BayGio)
            ELSE DATEADD(MINUTE, c.PhutTuT0, @T0) END,
       c.DonGia
FROM @CapDau c
JOIN phuongtien.DauMay dm ON dm.SoHieuDauMay = c.SoHieu
JOIN phuongtien.DongDauMay ddm ON ddm.MaDongDauMay = dm.MaDongDauMay
CROSS APPLY (SELECT CAST(ROUND(CAST(c.Lit AS DECIMAL(18,6)) * 10000 / (c.Tan * c.Km), 2) AS DECIMAL(5,2)) AS Sfc) v;

/* =========================================================================
   C. BẢO DƯỠNG ĐỊNH KỲ (đầu máy theo km, toa theo ngày - chu kỳ tham khảo)
   ========================================================================= */
DECLARE @BaoDuong TABLE (Loai VARCHAR(20), SoHieu VARCHAR(20), Cap VARCHAR(20), Km DECIMAL(9,1), Nguoi INT,
                         SoNgayTruoc INT, Gio INT, GhiChu NVARCHAR(400), ChiPhi DECIMAL(14,0));
INSERT INTO @BaoDuong VALUES
    ('DAU_MAY', 'D19E-901', 'R1',  80000.0, @Dung, 150, 15, N'Thay lọc dầu bôi trơn, kiểm tra hệ thống hãm và cát chống trượt.', 8200000),
    ('DAU_MAY', 'D19E-903', 'R1',  50500.0, @Son,  300, 10, N'Kiểm tra bộ truyền động, bôi trơn ổ trục trước mùa khai thác đèo Hải Vân.', 8900000),
    ('DAU_MAY', 'D19E-902', 'R1',  70000.0, @Dung, 400,  9, N'R1 định kỳ, thay dầu động cơ.', 7800000),
    ('TOA_XE',  'NC-101',   'D1',  38200.0, @Dung,  30, 14, N'Kiểm tra giá chuyển hướng, thay má phanh guốc.', 4800000),
    ('TOA_XE',  'NML-201',  'D2',  61500.0, @Hung, 100, 11, N'Bảo dưỡng cấp D2: điều hòa, hệ thống điện toa, toàn bộ ghế.', 16500000),
    ('TOA_XE',  'BN-301',   'D1',  45200.0, @Dung,  75,  8, N'Kiểm tra móc nối, đầu đấm; vệ sinh két nước.', 5100000),
    ('TOA_XE',  'AN-401',   'D1',  52800.0, @Hung, 170, 13, N'D1 định kỳ. Lưu ý cửa khoang 3 kẹt, đã căn chỉnh.', 5400000);

INSERT INTO baotri.NhatKyBaoDuong (LoaiPhuongTien, MaDauMay, MaToaXe, CapBaoDuong, SoKmTaiThoiDiem, GhiChuKyThuat,
                                   MaNguoiThucHien, ThoiDiemHoanThanh, ChiPhi)
SELECT b.Loai,
       CASE WHEN b.Loai = 'DAU_MAY' THEN dm.MaDauMay END,
       CASE WHEN b.Loai = 'TOA_XE' THEN tx.MaToaXe END,
       b.Cap, b.Km, b.GhiChu, b.Nguoi,
       DATEADD(HOUR, b.Gio, DATEADD(DAY, -b.SoNgayTruoc, @T0)), b.ChiPhi
FROM @BaoDuong b
LEFT JOIN phuongtien.DauMay dm ON b.Loai = 'DAU_MAY' AND dm.SoHieuDauMay = b.SoHieu
LEFT JOIN phuongtien.ToaXe tx ON b.Loai = 'TOA_XE' AND tx.SoHieuToaXe = b.SoHieu;

-- Phiếu gốc (D19E-902 R1 120.000 km): chi phí mẫu
UPDATE baotri.NhatKyBaoDuong SET ChiPhi = 7600000
WHERE ChiPhi = 0 AND SoKmTaiThoiDiem = 120000.0 AND CapBaoDuong = 'R1';

/* =========================================================================
   D. BIÊN BẢN KHÁM XE
   ========================================================================= */
UPDATE baotri.KhamXeKyThuat SET ChiPhi = 300000  WHERE MaDoanTau = @DoanSE1 AND ChiPhi = 0;
UPDATE baotri.KhamXeKyThuat SET ChiPhi = 3500000 WHERE MaDoanTau = @DoanSE3 AND ChiPhi = 0;

DECLARE @KhamXe TABLE (MaDoanTau INT, Nguoi INT, ApLuc DECIMAL(3,1), SutAp DECIMAL(3,2), Nuoc BIT, VeSinh BIT,
                       GhiChu NVARCHAR(400), PhutTuT0 INT, ChiPhi DECIMAL(14,0));
INSERT INTO @KhamXe VALUES
    (@DoanSE2,  @Hung, 5.1, 0.15, 1, 1, N'Đạt. Đã thay gioăng ống hãm nối toa 5 – 6.', -1440 + 1085, 850000),
    (@DoanSE4,  @Hung, 4.9, 0.20, 1, 1, N'Đạt ngưỡng tối thiểu. Theo dõi van hãm toa 7 ở ga dọc đường.', -1440 + 1095, 300000),
    (@DoanSE19, @Dung, 5.2, 0.10, 1, 0, N'Két vệ sinh toa 3, toa 5 chưa xả – chờ tổ vệ sinh depot.', 1050, 300000);

INSERT INTO baotri.KhamXeKyThuat (MaDoanTau, MaNguoiKham, ApLucHamBar, DoSutApBarPhut, DaCapNuoc, DaXaVeSinh,
                                  GhiChuKyThuat, ThoiDiemKham, ChiPhi)
SELECT k.MaDoanTau, k.Nguoi, k.ApLuc, k.SutAp, k.Nuoc, k.VeSinh, k.GhiChu,
       CASE WHEN DATEADD(MINUTE, k.PhutTuT0, @T0) > @BayGio
            THEN DATEADD(MINUTE, -20, @BayGio)
            ELSE DATEADD(MINUTE, k.PhutTuT0, @T0) END,
       k.ChiPhi
FROM @KhamXe k;

/* =========================================================================
   E. CỜ DUYỆT AN TOÀN KHỚP LUẬT MỚI
   ========================================================================= */
UPDATE vanhanh.DoanTau SET DaDuyetAnToan = 0 WHERE MaDoanTau IN (@DoanSE19, @DoanNA1, @DoanSE3);

COMMIT TRANSACTION;
GO

-- Kiểm tra nhanh sau khi chạy
SELECT N'Tài khoản kỹ thuật' AS Muc, COUNT(*) AS SoLuong
FROM phanquyen.TaiKhoan_VaiTro tv JOIN phanquyen.VaiTro vt ON vt.MaVaiTro = tv.MaVaiTro WHERE vt.MaVaiTroCode = 'KY_THUAT'
UNION ALL SELECT N'Biên bản khám xe', COUNT(*) FROM baotri.KhamXeKyThuat
UNION ALL SELECT N'Phiếu cấp dầu', COUNT(*) FROM baotri.NhatKyCapNhienLieu
UNION ALL SELECT N'Phiếu bảo dưỡng', COUNT(*) FROM baotri.NhatKyBaoDuong;
PRINT N'S20261024_1100: xong (dữ liệu mẫu Bảo trì Sprint 2).';
GO
