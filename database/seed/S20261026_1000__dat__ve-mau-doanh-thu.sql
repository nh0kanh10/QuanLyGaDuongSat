/* =========================================================================
   SEED SPRINT 2 — VÉ MẪU CHO TAB "CHI PHÍ & HIỆU QUẢ" (phần của Đạt, GHI VÀO BẢNG CỦA A BÁ)
   Tác giả  : Đạt
   Ngày     : 26/10/2026

   Thứ tự chạy (trên CSDL vừa dựng lại):
     1. script_tao_csdl_v2.sql
     2. migrations + seed Sprint 1 (V20261009_0820 → S20261009_0830 → V20261011_1500, V20261012_1100,
        V20261014_0900, V20261014_1000)
     3. migrations\V20261024_1000 → seed\S20261024_1100 (Bảo trì)
     4. file này
     5. tests\T20261009_0840
   Lệnh: sqlcmd -S . -E -I -f 65001 -d QuanLyDuongSatV2 -i <file>

   Chạy lại: đã có vé mã TK_MAU_ thì bỏ qua, không ghi gì. Mọi thứ nằm trong MỘT giao dịch.

   VÌ SAO CÓ FILE NÀY: CSDL gốc chỉ có 12 vé (~10 triệu đồng) và 0–3 toa bán vé / chuyến, trong khi đoàn tàu
   (lập ở Phương tiện) có 8–13 toa khách → tab Chi phí & Hiệu quả tính doanh thu thật ra mác nào cũng lỗ.
   Đạt chốt 26/10: bổ sung toa bán vé theo đoàn tàu rồi bán một phần ghế. Đây là DỮ LIỆU MẪU, đánh dấu riêng
   để A Bá nhận ra / bỏ:  vé TK_MAU_xxxxxx · đơn PNR_MAU_xxxxxx · khách có 6 số cuối CCCD từ 500001.

   ─────────────────────────────────────────────────────────────────────────
   NỘI DUNG
   A. Toa bán vé (vantai.ToaXeKhach) cho MỌI toa khách (NC / NML / BN / AN) trong đoàn tàu, theo thứ tự móc nối:
      toa khách thứ k ↔ ThuTuToa = k (đúng cách seed Sprint 1 đã khớp các toa đầu). Toa bán vé đã có thì giữ
      nguyên. Sức chứa theo phuongtien.ChungLoaiToa.SucChuaChuan nếu script gốc có cột này (bản 26/10 của A Bá:
      NC 80 · NML 64 · BN 42 · AN 28), không thì theo mặc định màn Lịch trình: AN 28 · BN 42 · NML 64 · NC 64.
      (Đây là việc "đồng bộ toa bán vé từ đoàn tàu" — KE_HOACH_BACK_END §7.1 — làm bằng dữ liệu, chưa có hàm.)
   B. Ghế (vantai.ChoNgoi) cho toa bán vé chưa có ghế, sinh y hàm SinhGheChoToa (ChuyenTauRepository):
      ghế 1..sức chứa, tầng AN 1–2, BN 1–3. Gồm cả 2 toa SE3 của script gốc đang thiếu ghế.
   C. 400 khách hàng mẫu: 75% thường, 12% sinh viên, 8% người cao tuổi, 5% trẻ em.
   D. Vé — mỗi ghế được chọn bán đúng 1 vé (không chồng chặng):
      - Tỷ lệ ghế bán (mẫu): SE1 75% · SE2 60% · SE3 40% (bán trước) · SE4 60% · SE19 55% · NA1 70% · mác khác 60%.
        Ghế đã có vé trong script gốc thì bỏ qua.
      - Chặng nằm trong phạm vi toa chạy (toa nối dọc đường chỉ bán từ ga nối, toa cắt chỉ bán tới ga cắt):
        ~45% đi hết phạm vi, còn lại một chặng ngẫu nhiên.
      - Giá = vantai.fn_TinhGiaVe (như màn bán vé). Giảm giá theo loại khách (danhmuc TI_LE_GIAM_GIA),
        làm tròn nghìn đồng.
      - Giờ xuất vé 2 giờ – 20 ngày trước giờ tàu rời ga đi của khách, không quá lúc chạy file;
        ghi giờ UTC như cột gốc (DEFAULT SYSUTCDATETIME()).
      - Tàu đã rời ga đi của khách (giờ đi kế hoạch ≤ lúc chạy) và chuyến đang chạy / đã xong → DA_LEN_TAU
        (+ ThoiDiemSoatVe giờ Việt Nam, MaGaSoat = ga đi, như màn Soát vé); còn lại DA_DAT.
      - ~3% vé không giảm giá đã hoàn: hoàn trước 30 giờ → lệ phí 10%, trước 10 giờ → 20% (CHECK của bảng);
        lúc hoàn phải sau lúc xuất vé và không quá lúc chạy file, không thì vé giữ nguyên.
   E. Mỗi vé một đơn (DonDatVe, SoLuongVe = 1), người bán nv_mai (vai trò BAN_VE).
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
IF NOT EXISTS (SELECT 1 FROM vanhanh.ChiTietDoanTau)
BEGIN
    RAISERROR(N'Đoàn tàu chưa có toa (chưa chạy seed Sprint 1 S20261009_0830).', 16, 1);
    RETURN;
END;
IF COL_LENGTH(N'vantai.Ve', N'ThoiDiemSoatVe') IS NULL OR COL_LENGTH(N'vantai.Ve', N'MaGaSoat') IS NULL
BEGIN
    RAISERROR(N'Bảng vantai.Ve chưa có cột ThoiDiemSoatVe / MaGaSoat (script gốc cũ).', 16, 1);
    RETURN;
END;
IF EXISTS (SELECT 1 FROM vantai.Ve WHERE MaVeCode LIKE 'TK[_]MAU[_]%')
BEGIN
    PRINT N'Seed S20261026_1000 đã chạy trên CSDL này (đã có vé TK_MAU_) -> bỏ qua, không ghi gì.';
    RETURN;
END;

DECLARE @BayGio DATETIME2 = SYSDATETIME();
DECLARE @LechUtcPhut INT = DATEDIFF(MINUTE, SYSDATETIME(), SYSUTCDATETIME());   -- gio may -> UTC (-420 o VN)
DECLARE @NguoiBan INT = (SELECT MaTaiKhoan FROM phanquyen.TaiKhoan WHERE TenDangNhap = 'nv_mai');
IF @NguoiBan IS NULL SET @NguoiBan = (SELECT MIN(MaTaiKhoan) FROM phanquyen.TaiKhoan);
DECLARE @SoKhach INT = 400;

-- Bang tam con sot (chay lai trong cung phien SSMS sau mot lan loi)
DROP TABLE IF EXISTS #ToaKhach, #SoKhach, #KhachMau, #Dung, #Ghe, #ChonVe, #Ve;

BEGIN TRANSACTION;

/* =========================================================================
   A. TOA BÁN VÉ THEO ĐOÀN TÀU
   ========================================================================= */
SELECT dt.MaChuyenTau, ctd.MaToaXe, cl.MaChungLoaiCode AS LoaiToa, ctd.MaGaNoi, ctd.MaGaCat,
       ROW_NUMBER() OVER (PARTITION BY dt.MaChuyenTau ORDER BY ctd.ThuTuNoi) AS ThuTuKhach,
       CASE cl.MaChungLoaiCode WHEN 'AN' THEN 28 WHEN 'BN' THEN 42 ELSE 64 END AS SucChua
INTO #ToaKhach
FROM vanhanh.DoanTau dt
JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau
JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
JOIN vanhanh.ChiTietDoanTau ctd ON ctd.MaDoanTau = dt.MaDoanTau
JOIN phuongtien.ToaXe tx ON tx.MaToaXe = ctd.MaToaXe
JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
WHERE ct.TrangThai <> 'DA_HUY'
  AND mt.LoaiTau IN ('TAU_NHANH', 'TAU_CHO')
  AND cl.MaChungLoaiCode IN ('NC', 'NML', 'BN', 'AN');

-- Script goc ban 26/10 cua A Ba co suc chua chuan theo chung loai (ChungLoaiToa.SucChuaChuan, NC 80) va man
-- Lich trinh doc theo cot do -> dung no cho khop. Script goc cu chua co cot thi giu mac dinh o tren.
IF COL_LENGTH(N'phuongtien.ChungLoaiToa', N'SucChuaChuan') IS NOT NULL
    EXEC (N'UPDATE k SET SucChua = cl.SucChuaChuan
            FROM #ToaKhach k
            JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoaiCode = k.LoaiToa
            WHERE cl.SucChuaChuan > 0;');

INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua)
SELECT k.MaChuyenTau,
       N'Toa ' + CAST(k.ThuTuKhach AS NVARCHAR(3)) + N' (' + k.LoaiToa + N')',
       k.LoaiToa, k.ThuTuKhach, k.SucChua
FROM #ToaKhach k
WHERE NOT EXISTS (SELECT 1 FROM vantai.ToaXeKhach t
                  WHERE t.MaChuyenTau = k.MaChuyenTau AND t.ThuTuToa = k.ThuTuKhach);
DECLARE @SoToaMoi INT = @@ROWCOUNT;

/* =========================================================================
   B. GHẾ CHO TOA BÁN VÉ CHƯA CÓ GHẾ
   ========================================================================= */
;WITH So AS (
    SELECT TOP (200) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_objects)
INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT t.MaToaXeKhach, s.n,
       CASE t.LoaiToa WHEN 'AN' THEN ((s.n - 1) % 2) + 1 WHEN 'BN' THEN ((s.n - 1) % 3) + 1 END,
       0
FROM vantai.ToaXeKhach t
JOIN So s ON s.n <= t.SucChua
WHERE t.MaChuyenTau IN (SELECT MaChuyenTau FROM #ToaKhach)
  AND NOT EXISTS (SELECT 1 FROM vantai.ChoNgoi c WHERE c.MaToaXeKhach = t.MaToaXeKhach);
DECLARE @SoGheMoi INT = @@ROWCOUNT;

/* =========================================================================
   C. KHÁCH HÀNG MẪU
   ========================================================================= */
DECLARE @Ho TABLE (i INT PRIMARY KEY, Ten NVARCHAR(10));
INSERT INTO @Ho VALUES (0, N'Nguyễn'), (1, N'Trần'), (2, N'Lê'), (3, N'Phạm'), (4, N'Hoàng'), (5, N'Huỳnh'),
                       (6, N'Phan'), (7, N'Vũ'), (8, N'Võ'), (9, N'Đặng'), (10, N'Bùi'), (11, N'Đỗ'),
                       (12, N'Hồ'), (13, N'Ngô'), (14, N'Dương'), (15, N'Lý');
DECLARE @Dem TABLE (Nu BIT, i INT, Ten NVARCHAR(10), PRIMARY KEY (Nu, i));
INSERT INTO @Dem VALUES (0, 0, N'Văn'), (0, 1, N'Minh'), (0, 2, N'Quốc'), (0, 3, N'Đức'),
                        (0, 4, N'Hoàng'), (0, 5, N'Thanh'), (0, 6, N'Gia'), (0, 7, N'Hữu'),
                        (1, 0, N'Thị'), (1, 1, N'Ngọc'), (1, 2, N'Thu'), (1, 3, N'Thanh'),
                        (1, 4, N'Mai'), (1, 5, N'Kim'), (1, 6, N'Phương'), (1, 7, N'Bảo');
DECLARE @Ten TABLE (Nu BIT, i INT, Ten NVARCHAR(10), PRIMARY KEY (Nu, i));
INSERT INTO @Ten VALUES (0, 0, N'An'), (0, 1, N'Bình'), (0, 2, N'Cường'), (0, 3, N'Dũng'), (0, 4, N'Hải'),
                        (0, 5, N'Hiếu'), (0, 6, N'Hùng'), (0, 7, N'Khang'), (0, 8, N'Khánh'), (0, 9, N'Long'),
                        (0, 10, N'Minh'), (0, 11, N'Nam'), (0, 12, N'Phong'), (0, 13, N'Phúc'), (0, 14, N'Quân'),
                        (0, 15, N'Sơn'), (0, 16, N'Tài'), (0, 17, N'Thắng'), (0, 18, N'Tuấn'), (0, 19, N'Việt'),
                        (1, 0, N'Anh'), (1, 1, N'Châu'), (1, 2, N'Giang'), (1, 3, N'Hà'), (1, 4, N'Hạnh'),
                        (1, 5, N'Hương'), (1, 6, N'Lan'), (1, 7, N'Linh'), (1, 8, N'Mai'), (1, 9, N'My'),
                        (1, 10, N'Ngân'), (1, 11, N'Nhung'), (1, 12, N'Oanh'), (1, 13, N'Phương'), (1, 14, N'Quỳnh'),
                        (1, 15, N'Tâm'), (1, 16, N'Thảo'), (1, 17, N'Trang'), (1, 18, N'Vy'), (1, 19, N'Yến');
DECLARE @Tinh TABLE (i INT PRIMARY KEY, Ma CHAR(3));
INSERT INTO @Tinh VALUES (0, '001'), (1, '079'), (2, '048'), (3, '046'), (4, '040'), (5, '038'), (6, '056'), (7, '036');

;WITH So AS (
    SELECT TOP (@SoKhach) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
    FROM sys.all_objects a CROSS JOIN sys.all_objects b)
SELECT s.i,
       CAST(s.i % 2 AS BIT) AS Nu,
       CASE WHEN s.i % 100 < 75 THEN 'THUONG' WHEN s.i % 100 < 87 THEN 'SINH_VIEN'
            WHEN s.i % 100 < 95 THEN 'NGUOI_GIA' ELSE 'TRE_EM' END AS LoaiKhach
INTO #SoKhach
FROM So s;

-- Ngay sinh theo loai khach: tre em 6-10 tuoi, sinh vien 18-23, nguoi cao tuoi 60-80, thuong 24-59
SELECT k.i, k.Nu, k.LoaiKhach,
       DATEADD(DAY, -(CASE k.LoaiKhach WHEN 'TRE_EM'    THEN 6 * 365 + (k.i * 37) % (4 * 365)
                                       WHEN 'SINH_VIEN' THEN 18 * 365 + (k.i * 37) % (5 * 365)
                                       WHEN 'NGUOI_GIA' THEN 61 * 365 + (k.i * 37) % (19 * 365)
                                       ELSE 24 * 365 + (k.i * 37) % (35 * 365) END),
               CAST(@BayGio AS DATE)) AS NgaySinh,
       CAST(NULL AS NVARCHAR(100)) AS HoTen, CAST(NULL AS VARCHAR(20)) AS SoCCCD,
       CAST(NULL AS VARCHAR(20)) AS SoDienThoai, CAST(NULL AS INT) AS MaKhachHang
INTO #KhachMau
FROM #SoKhach k;

UPDATE km SET
    HoTen = h.Ten + N' ' + d.Ten + N' ' + t.Ten,
    -- CCCD 12 so: tinh (3) + gioi tinh / the ky (1) + nam sinh (2) + 6 so (500001.. de khong trung du lieu goc)
    SoCCCD = tn.Ma
           + CAST((CASE WHEN YEAR(km.NgaySinh) >= 2000 THEN 2 ELSE 0 END) + CAST(km.Nu AS INT) AS CHAR(1))
           + RIGHT('0' + CAST(YEAR(km.NgaySinh) % 100 AS VARCHAR(2)), 2)
           + RIGHT('000000' + CAST(500000 + km.i AS VARCHAR(6)), 6),
    SoDienThoai = '09' + RIGHT('00000000' + CAST(10000000 + (km.i * 7919) % 89999999 AS VARCHAR(8)), 8)
FROM #KhachMau km
JOIN @Ho h ON h.i = (km.i * 7) % 16
JOIN @Dem d ON d.Nu = km.Nu AND d.i = (km.i * 3) % 8
JOIN @Ten t ON t.Nu = km.Nu AND t.i = (km.i * 11) % 20
JOIN @Tinh tn ON tn.i = km.i % 8;

IF EXISTS (SELECT 1 FROM vantai.KhachHang kh JOIN #KhachMau km ON km.SoCCCD = kh.SoCCCD)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR(N'CCCD khách mẫu trùng khách đã có trong CSDL -> không ghi gì.', 16, 1);
    RETURN;
END;

INSERT INTO vantai.KhachHang (HoTen, SoCCCD, SoDienThoai, Email, NgaySinh, LoaiKhach, NgayTao)
SELECT HoTen, SoCCCD, SoDienThoai, NULL, NgaySinh, LoaiKhach, DATEADD(MINUTE, @LechUtcPhut, DATEADD(DAY, -30, @BayGio))
FROM #KhachMau
ORDER BY i;

UPDATE km SET MaKhachHang = kh.MaKhachHang
FROM #KhachMau km JOIN vantai.KhachHang kh ON kh.SoCCCD = km.SoCCCD;

/* =========================================================================
   D. VÉ
   ========================================================================= */
-- Diem dung cua tung chuyen, danh so lai 1..n theo thu tu dung
SELECT l.MaChuyenTau, l.MaGa,
       ROW_NUMBER() OVER (PARTITION BY l.MaChuyenTau ORDER BY l.ThuTuDung) AS ViTri,
       COALESCE(l.GioDiKeHoach, l.GioDenKeHoach) AS GioDi
INTO #Dung
FROM vanhanh.LichDungGa l
WHERE l.MaChuyenTau IN (SELECT MaChuyenTau FROM #ToaKhach);

-- Ghe duoc chon ban + pham vi chay cua toa (vi tri diem dung tu .. den)
SELECT c.MaChoNgoi, t.MaChuyenTau, t.LoaiToa, c.TangGiuong, mt.LoaiTau, ct.TrangThai AS TrangThaiChuyen,
       ISNULL(dTu.ViTri, 1) AS ViTriTu, ISNULL(dDen.ViTri, n.SoDiem) AS ViTriDen
INTO #Ghe
FROM vantai.ChoNgoi c
JOIN vantai.ToaXeKhach t ON t.MaToaXeKhach = c.MaToaXeKhach
JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = t.MaChuyenTau
JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
JOIN (SELECT MaChuyenTau, COUNT(*) AS SoDiem FROM #Dung GROUP BY MaChuyenTau) n ON n.MaChuyenTau = t.MaChuyenTau
LEFT JOIN #ToaKhach k ON k.MaChuyenTau = t.MaChuyenTau AND k.ThuTuKhach = t.ThuTuToa
LEFT JOIN #Dung dTu ON dTu.MaChuyenTau = t.MaChuyenTau AND dTu.MaGa = k.MaGaNoi
LEFT JOIN #Dung dDen ON dDen.MaChuyenTau = t.MaChuyenTau AND dDen.MaGa = k.MaGaCat
WHERE t.MaChuyenTau IN (SELECT MaChuyenTau FROM #ToaKhach)
  AND NOT EXISTS (SELECT 1 FROM vantai.Ve v WHERE v.MaChuyenTau = t.MaChuyenTau AND v.MaChoNgoi = c.MaChoNgoi)
  AND (CAST(HASHBYTES('MD5', CONCAT(c.MaChoNgoi, N'|', N'ban')) AS INT) & 2147483647) % 100 <
      CASE mt.SoHieuMacTau WHEN 'SE1' THEN 75 WHEN 'SE2' THEN 60 WHEN 'SE3' THEN 40
                           WHEN 'SE4' THEN 60 WHEN 'SE19' THEN 55 WHEN 'NA1' THEN 70 ELSE 60 END;

DELETE FROM #Ghe WHERE ViTriDen <= ViTriTu;

SELECT g.MaChoNgoi, g.MaChuyenTau, g.LoaiToa, g.TangGiuong, g.LoaiTau, g.TrangThaiChuyen, a.ViTriDi, b.ViTriDen,
       km.MaKhachHang, km.HoTen, km.SoCCCD, km.LoaiKhach
INTO #ChonVe
FROM #Ghe g
CROSS APPLY (SELECT CASE WHEN (CAST(HASHBYTES('MD5', CONCAT(g.MaChoNgoi, N'|', N'chang')) AS INT) & 2147483647) % 100 < 45 THEN g.ViTriTu
                         ELSE g.ViTriTu + (CAST(HASHBYTES('MD5', CONCAT(g.MaChoNgoi, N'|', N'di')) AS INT) & 2147483647) % (g.ViTriDen - g.ViTriTu) END AS ViTriDi) a
CROSS APPLY (SELECT CASE WHEN (CAST(HASHBYTES('MD5', CONCAT(g.MaChoNgoi, N'|', N'chang')) AS INT) & 2147483647) % 100 < 45 THEN g.ViTriDen
                         ELSE a.ViTriDi + 1 + (CAST(HASHBYTES('MD5', CONCAT(g.MaChoNgoi, N'|', N'den')) AS INT) & 2147483647) % (g.ViTriDen - a.ViTriDi) END AS ViTriDen) b
JOIN #KhachMau km ON km.i = (CAST(HASHBYTES('MD5', CONCAT(g.MaChoNgoi, N'|', g.MaChuyenTau, N'|', N'khach')) AS INT) & 2147483647) % @SoKhach + 1;

SELECT cv.*, dDi.MaGa AS MaGaDi, dDen.MaGa AS MaGaDen, dDi.GioDi,
       vantai.fn_TinhGiaVe(cv.LoaiToa, dDi.MaGa, dDen.MaGa, cv.TangGiuong, cv.LoaiTau) AS GiaVeGoc,
       ISNULL((SELECT TRY_CAST(dm.GhiChu AS DECIMAL(4, 2)) FROM danhmuc.DanhMuc dm
               WHERE dm.NhomDanhMuc = 'TI_LE_GIAM_GIA' AND dm.MaCode = cv.LoaiKhach), 0) AS TyLeGiam,
       CAST(NULL AS INT) AS rn, CAST(NULL AS DECIMAL(12, 2)) AS SoTienGiam, CAST(NULL AS DATETIME2) AS XuatVe,
       CAST(NULL AS DATETIME2) AS Hoan, CAST(NULL AS DECIMAL(4, 2)) AS TyLeLePhi,
       CAST(NULL AS VARCHAR(20)) AS TrangThai, CAST(NULL AS DATETIME2) AS SoatVe
INTO #Ve
FROM #ChonVe cv
JOIN #Dung dDi ON dDi.MaChuyenTau = cv.MaChuyenTau AND dDi.ViTri = cv.ViTriDi
JOIN #Dung dDen ON dDen.MaChuyenTau = cv.MaChuyenTau AND dDen.ViTri = cv.ViTriDen;

DELETE FROM #Ve WHERE GiaVeGoc IS NULL OR GiaVeGoc <= 0;

;WITH Danh AS (SELECT rn, ROW_NUMBER() OVER (ORDER BY MaChuyenTau, MaChoNgoi) AS so FROM #Ve)
UPDATE Danh SET rn = so;

UPDATE #Ve SET SoTienGiam = ROUND(GiaVeGoc * TyLeGiam, -3);

-- Xuat ve 2 gio - 20 ngay truoc gio di; ve "ban sau luc chay file" (chuyen con xa) lui ve trong 10 gio gan nhat
UPDATE #Ve SET XuatVe = DATEADD(MINUTE, -(120 + (CAST(HASHBYTES('MD5', CONCAT(MaChoNgoi, N'|', N'xuatve')) AS INT) & 2147483647) % (20 * 24 * 60)), GioDi);
UPDATE #Ve SET XuatVe = DATEADD(MINUTE, -(1 + (CAST(HASHBYTES('MD5', CONCAT(MaChoNgoi, N'|', N'gan')) AS INT) & 2147483647) % 600), @BayGio) WHERE XuatVe > @BayGio;

-- Hoan ve: ~3% ve khong giam gia
UPDATE #Ve SET
    TyLeLePhi = CASE WHEN (CAST(HASHBYTES('MD5', CONCAT(MaChoNgoi, N'|', N'lephi')) AS INT) & 2147483647) % 2 = 0 THEN 0.10 ELSE 0.20 END,
    Hoan = DATEADD(HOUR, CASE WHEN (CAST(HASHBYTES('MD5', CONCAT(MaChoNgoi, N'|', N'lephi')) AS INT) & 2147483647) % 2 = 0 THEN -30 ELSE -10 END, GioDi)
WHERE SoTienGiam = 0 AND (CAST(HASHBYTES('MD5', CONCAT(MaChoNgoi, N'|', N'hoan')) AS INT) & 2147483647) % 100 < 3;
UPDATE #Ve SET Hoan = NULL, TyLeLePhi = NULL WHERE Hoan IS NOT NULL AND (Hoan <= XuatVe OR Hoan > @BayGio);

UPDATE #Ve SET TrangThai =
    CASE WHEN Hoan IS NOT NULL THEN 'DA_HOAN_VE'
         WHEN GioDi <= @BayGio AND TrangThaiChuyen IN ('DANG_CHAY', 'TRE_GIO', 'HOAN_THANH') THEN 'DA_LEN_TAU'
         ELSE 'DA_DAT' END;
UPDATE #Ve SET SoatVe = DATEADD(MINUTE, -(5 + (CAST(HASHBYTES('MD5', CONCAT(MaChoNgoi, N'|', N'soat')) AS INT) & 2147483647) % 25), GioDi) WHERE TrangThai = 'DA_LEN_TAU';

/* =========================================================================
   E. ĐƠN ĐẶT VÉ + VÉ + HOÀN VÉ
   ========================================================================= */
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan, ThoiDiemTao)
SELECT 'PNR_MAU_' + RIGHT('000000' + CAST(rn AS VARCHAR(6)), 6), MaKhachHang, 1, GiaVeGoc - SoTienGiam,
       CASE (CAST(HASHBYTES('MD5', CONCAT(rn, N'|', N'thanhtoan')) AS INT) & 2147483647) % 4 WHEN 0 THEN 'TIEN_MAT' WHEN 1 THEN 'CHUYEN_KHOAN'
                                                WHEN 2 THEN 'THE_NGAN_HANG' ELSE 'VI_DIEN_TU' END,
       CASE WHEN TrangThai = 'DA_HOAN_VE' THEN 'DA_HOAN_TIEN' ELSE 'DA_THANH_TOAN' END,
       @NguoiBan, DATEADD(MINUTE, @LechUtcPhut, XuatVe)
FROM #Ve
ORDER BY rn;

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach,
                       GiaVeGoc, SoTienGiam, TrangThai, ThoiDiemXuatVe, ThoiDiemSoatVe, MaGaSoat)
SELECT d.MaDonVe, 'TK_MAU_' + RIGHT('000000' + CAST(v.rn AS VARCHAR(6)), 6), v.MaChuyenTau, v.MaChoNgoi,
       v.MaGaDi, v.MaGaDen, v.HoTen, v.SoCCCD, v.GiaVeGoc, v.SoTienGiam, v.TrangThai,
       DATEADD(MINUTE, @LechUtcPhut, v.XuatVe), v.SoatVe,
       CASE WHEN v.TrangThai = 'DA_LEN_TAU' THEN v.MaGaDi END
FROM #Ve v
JOIN vantai.DonDatVe d ON d.MaPNR = 'PNR_MAU_' + RIGHT('000000' + CAST(v.rn AS VARCHAR(6)), 6)
ORDER BY v.rn;
DECLARE @SoVe INT = @@ROWCOUNT;

INSERT INTO vantai.HoanHuyVe (MaVe, ThoiDiemHoan, TienVeGoc, TyLeLePhi, HinhThucHoan, LyDoHoan, MaNhanVienDuyet)
SELECT ve.MaVe, DATEADD(MINUTE, @LechUtcPhut, v.Hoan), v.GiaVeGoc, v.TyLeLePhi, 'CHUYEN_KHOAN',
       CASE (CAST(HASHBYTES('MD5', CONCAT(v.rn, N'|', N'lydo')) AS INT) & 2147483647) % 4 WHEN 0 THEN N'Thay đổi kế hoạch cá nhân'
                                             WHEN 1 THEN N'Bận công việc đột xuất'
                                             WHEN 2 THEN N'Gia đình có việc gấp'
                                             ELSE N'Đổi sang chuyến khác' END,
       @NguoiBan
FROM #Ve v
JOIN vantai.Ve ve ON ve.MaVeCode = 'TK_MAU_' + RIGHT('000000' + CAST(v.rn AS VARCHAR(6)), 6)
WHERE v.TrangThai = 'DA_HOAN_VE';

COMMIT TRANSACTION;

/* =========================================================================
   TÓM TẮT
   ========================================================================= */
PRINT N'Seed S20261026_1000: thêm ' + CAST(@SoToaMoi AS NVARCHAR(10)) + N' toa bán vé, '
    + CAST(@SoGheMoi AS NVARCHAR(10)) + N' ghế, ' + CAST(@SoKhach AS NVARCHAR(10)) + N' khách, '
    + CAST(@SoVe AS NVARCHAR(10)) + N' vé mẫu.';

SELECT mt.SoHieuMacTau AS MacTau, ct.NgayXuatPhat,
       (SELECT COUNT(*) FROM vantai.ToaXeKhach t WHERE t.MaChuyenTau = ct.MaChuyenTau) AS SoToaBanVe,
       (SELECT ISNULL(SUM(t.SucChua), 0) FROM vantai.ToaXeKhach t WHERE t.MaChuyenTau = ct.MaChuyenTau) AS SoGhe,
       SUM(CASE WHEN v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU') THEN 1 ELSE 0 END) AS VeHieuLuc,
       SUM(CASE WHEN v.TrangThai = 'DA_HOAN_VE' THEN 1 ELSE 0 END) AS VeHoan,
       SUM(CASE WHEN v.TrangThai <> 'DA_HUY' THEN v.GiaVeThucThu ELSE 0 END) - ISNULL(SUM(h.SoTienThucHoan), 0) AS DoanhThuThuan
FROM vanhanh.ChuyenTau ct
JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
LEFT JOIN vantai.Ve v ON v.MaChuyenTau = ct.MaChuyenTau
LEFT JOIN vantai.HoanHuyVe h ON h.MaVe = v.MaVe
GROUP BY mt.SoHieuMacTau, ct.MaChuyenTau, ct.NgayXuatPhat, ct.GioXuatPhatKH
ORDER BY ct.GioXuatPhatKH;

-- File demo tron goi chay moi phan trong CUNG mot phien: don bang tam cho phan sau
DROP TABLE IF EXISTS #ToaKhach, #SoKhach, #KhachMau, #Dung, #Ghe, #ChonVe, #Ve;
GO
