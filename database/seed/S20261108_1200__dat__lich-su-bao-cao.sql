/* =========================================================================
   SEED: Dữ liệu lịch sử 14 ngày cho trang Báo cáo thống kê (Sprint 3)
   Tác giả  : Đạt
   Ngày     : 08/11/2026
   Bảng ghi : vanhanh.ChuyenTau, LichDungGa, NhatKyChamGio; vantai.ToaXeKhach, ChoNgoi, DonDatVe, Ve, HoanHuyVe
              (bảng của A Bá — chỉ THÊM dữ liệu mẫu đánh dấu TK_LS_ / PNR_LS_, không sửa dòng có sẵn. Đạt chốt 08/11 (D8).)

   Lý do: CSDL dựng mới chỉ có vài chuyến quanh hôm nay -> doanh thu theo ngày, hệ số sử dụng ghế, tỷ lệ đúng giờ
   gần như trống. File này nhân bản các chuyến mẫu của 6 mác khách (SE1, SE2, SE3, SE4, SE19, NA1) ra 14 ngày
   trước (ngày gốc − 17 .. ngày gốc − 4), đã chạy xong (HOAN_THANH). Dừng ở ngày −4 để chuyến 32,5 giờ
   cũng về đích trước hôm qua -> dữ liệu "quanh hôm nay" (cửa sổ Nhân sự, kịch bản demo cũ) giữ nguyên:
     - lịch dừng = lịch của chuyến mẫu dời ngày; GIỜ THỰC TẾ có trễ mô phỏng: 60% chuyến trễ 0–4 phút,
       25% trễ 5–20 phút, 15% trễ 20–60 phút (trễ tăng dần dọc hành trình, đến ga cuối = trễ lớn nhất);
       chuyến trễ từ 10 phút ghi 1 dòng NhatKyChamGio;
     - toa bán vé + ghế = như chuyến mẫu;
     - vé: tỷ lệ bán theo mác (SE1 75% … SE3 45%) + cuối tuần thêm 10%; chặng con ngẫu nhiên như seed 26/10;
       ~3% vé hoàn (DA_HOAN_VE + HoanHuyVe), còn lại DA_LEN_TAU (chuyến đã chạy). Khách lấy từ vantai.KhachHang.
   Ngẫu nhiên tất định (HASHBYTES MD5) -> chạy lại trên CSDL dựng lại cho cùng kết quả.

   Thứ tự: sau S20261026_1000 (cần toa bán vé + khách mẫu). Thấy vé TK_LS_ thì bỏ qua.
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

IF EXISTS (SELECT 1 FROM vantai.Ve WHERE MaVeCode LIKE 'TK[_]LS[_]%')
BEGIN
    PRINT N'Seed S20261108_1200 đã chạy trên CSDL này (có vé TK_LS_) -> bỏ qua.';
    SET NOEXEC ON;
END;
GO

DROP TABLE IF EXISTS #Mau, #ChuyenMoi, #Dung, #Khach, #Ghe, #Ve;

DECLARE @NgayGoc DATE = (SELECT TOP (1) ct.NgayXuatPhat FROM vanhanh.ChuyenTau ct
                         JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau
                         WHERE m.SoHieuMacTau = 'SE19' ORDER BY ct.NgayXuatPhat DESC);
DECLARE @LechUtcPhut INT = DATEDIFF(MINUTE, SYSDATETIME(), SYSUTCDATETIME());
DECLARE @NguoiBan INT = ISNULL((SELECT MaTaiKhoan FROM phanquyen.TaiKhoan WHERE TenDangNhap = 'nv_mai'),
                               (SELECT MIN(MaTaiKhoan) FROM phanquyen.TaiKhoan));
IF @NgayGoc IS NULL
BEGIN
    RAISERROR(N'Không tìm thấy chuyến SE19 của dữ liệu gốc.', 16, 1);
    RETURN;
END;

-- 1. Chuyến mẫu: mỗi mác khách lấy chuyến gần nhất có cả lịch dừng lẫn toa bán vé
SELECT m.MaMacTau, m.SoHieuMacTau, m.LoaiTau, x.MaChuyenTau AS MaMau, x.NgayXuatPhat AS NgayMau,
       x.GioXuatPhatKH, x.GioVeDichKH
INTO #Mau
FROM vanhanh.MacTauMau m
CROSS APPLY (SELECT TOP (1) ct.*
             FROM vanhanh.ChuyenTau ct
             WHERE ct.MaMacTau = m.MaMacTau AND ct.TrangThai <> 'DA_HUY'
               AND EXISTS (SELECT 1 FROM vanhanh.LichDungGa l WHERE l.MaChuyenTau = ct.MaChuyenTau)
               AND EXISTS (SELECT 1 FROM vantai.ToaXeKhach t WHERE t.MaChuyenTau = ct.MaChuyenTau)
             ORDER BY ct.NgayXuatPhat DESC) x
WHERE m.SoHieuMacTau IN ('SE1', 'SE2', 'SE3', 'SE4', 'SE19', 'NA1');

BEGIN TRANSACTION;

-- 2. Chuyến lịch sử: 14 ngày (ngày gốc − 17 .. − 4), bỏ ngày mác đã có chuyến (UQ_MacTau_Ngay)
;WITH Ngay AS (SELECT TOP (14) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) + 3 AS d FROM sys.all_objects)
SELECT mu.*, n.d, DATEADD(DAY, -n.d, @NgayGoc) AS NgayMoi,
       DATEDIFF(DAY, mu.NgayMau, DATEADD(DAY, -n.d, @NgayGoc)) AS DoiNgay,
       (CAST(HASHBYTES('MD5', CONCAT(mu.SoHieuMacTau, N'|', n.d, N'|tre')) AS INT) & 2147483647) % 100 AS HTre,
       CAST(NULL AS INT) AS TreToiDa, CAST(NULL AS INT) AS MaChuyenMoi
INTO #ChuyenMoi
FROM #Mau mu CROSS JOIN Ngay n
WHERE NOT EXISTS (SELECT 1 FROM vanhanh.ChuyenTau c WHERE c.MaMacTau = mu.MaMacTau AND c.NgayXuatPhat = DATEADD(DAY, -n.d, @NgayGoc));

UPDATE #ChuyenMoi SET TreToiDa = CASE WHEN HTre < 60 THEN HTre % 5
                                      WHEN HTre < 85 THEN 5 + HTre % 16
                                      ELSE 20 + (HTre * 7) % 41 END;

INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, GioXuatPhatTT, GioVeDichTT, SoPhutTreLuyKe, TrangThai)
SELECT MaMacTau, NgayMoi, DATEADD(DAY, DoiNgay, GioXuatPhatKH), DATEADD(DAY, DoiNgay, GioVeDichKH),
       DATEADD(DAY, DoiNgay, GioXuatPhatKH), DATEADD(MINUTE, TreToiDa, DATEADD(DAY, DoiNgay, GioVeDichKH)), TreToiDa, 'HOAN_THANH'
FROM #ChuyenMoi;
DECLARE @SoChuyen INT = @@ROWCOUNT;

UPDATE cm SET MaChuyenMoi = ct.MaChuyenTau
FROM #ChuyenMoi cm JOIN vanhanh.ChuyenTau ct ON ct.MaMacTau = cm.MaMacTau AND ct.NgayXuatPhat = cm.NgayMoi;

-- 3. Lịch dừng: dời ngày; trễ tăng dần (ga đầu 0, ga cuối = TreToiDa); không gán đường ray
INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, GioDenThucTe, GioDiThucTe, MaDuongRay, LaDiemTranh)
SELECT cm.MaChuyenMoi, l.MaGa, l.ThuTuDung,
       DATEADD(DAY, cm.DoiNgay, l.GioDenKeHoach), DATEADD(DAY, cm.DoiNgay, l.GioDiKeHoach),
       DATEADD(MINUTE, ROUND(cm.TreToiDa * 1.0 * (l.ThuTuDung - 1) / NULLIF(n.SoDiem - 1, 0), 0), DATEADD(DAY, cm.DoiNgay, l.GioDenKeHoach)),
       DATEADD(MINUTE, ROUND(cm.TreToiDa * 1.0 * (l.ThuTuDung - 1) / NULLIF(n.SoDiem - 1, 0), 0), DATEADD(DAY, cm.DoiNgay, l.GioDiKeHoach)),
       NULL, l.LaDiemTranh
FROM #ChuyenMoi cm
JOIN vanhanh.LichDungGa l ON l.MaChuyenTau = cm.MaMau
JOIN (SELECT MaChuyenTau, MAX(ThuTuDung) AS SoDiem FROM vanhanh.LichDungGa GROUP BY MaChuyenTau) n ON n.MaChuyenTau = cm.MaMau;

-- 4. Nhật ký chậm giờ cho chuyến trễ từ 10 phút (ghi ở điểm dừng giữa hành trình)
INSERT INTO vanhanh.NhatKyChamGio (MaChuyenTau, MaGa, LoaiSuCo, SoPhutCham, GhiChuXuLy, ThoiDiemBaoCao)
SELECT cm.MaChuyenMoi, l.MaGa,
       CASE cm.HTre % 5 WHEN 0 THEN 'CHO_TRANH_TAU' WHEN 1 THEN 'SU_CO_TOA_XE' WHEN 2 THEN 'BAO_LU'
                        WHEN 3 THEN 'HONG_DAU_MAY' ELSE 'KHAC' END,
       cm.TreToiDa, N'Dữ liệu lịch sử mẫu (seed S20261108_1200).',
       DATEADD(MINUTE, @LechUtcPhut, l.GioDiThucTe)
FROM #ChuyenMoi cm
JOIN (SELECT MaChuyenTau, MAX(ThuTuDung) AS SoDiem FROM vanhanh.LichDungGa GROUP BY MaChuyenTau) n ON n.MaChuyenTau = cm.MaChuyenMoi
JOIN vanhanh.LichDungGa l ON l.MaChuyenTau = cm.MaChuyenMoi AND l.ThuTuDung = (n.SoDiem + 1) / 2
WHERE cm.TreToiDa >= 10;

-- 5. Toa bán vé + ghế như chuyến mẫu
INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua)
SELECT cm.MaChuyenMoi, t.NhanHieuToa, t.LoaiToa, t.ThuTuToa, t.SucChua
FROM #ChuyenMoi cm JOIN vantai.ToaXeKhach t ON t.MaChuyenTau = cm.MaMau;

INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT tMoi.MaToaXeKhach, c.SoGhe, c.TangGiuong, c.LaGhePhu
FROM #ChuyenMoi cm
JOIN vantai.ToaXeKhach tMau ON tMau.MaChuyenTau = cm.MaMau
JOIN vantai.ToaXeKhach tMoi ON tMoi.MaChuyenTau = cm.MaChuyenMoi AND tMoi.ThuTuToa = tMau.ThuTuToa
JOIN vantai.ChoNgoi c ON c.MaToaXeKhach = tMau.MaToaXeKhach;

-- 6. Vé
SELECT l.MaChuyenTau, l.MaGa, ROW_NUMBER() OVER (PARTITION BY l.MaChuyenTau ORDER BY l.ThuTuDung) AS ViTri,
       l.GioDiKeHoach AS GioDi
INTO #Dung
FROM vanhanh.LichDungGa l
WHERE l.MaChuyenTau IN (SELECT MaChuyenMoi FROM #ChuyenMoi);

SELECT ROW_NUMBER() OVER (ORDER BY MaKhachHang) AS i, MaKhachHang, HoTen, SoCCCD, LoaiKhach
INTO #Khach
FROM vantai.KhachHang;
DECLARE @SoKhach INT = (SELECT COUNT(*) FROM #Khach);

SELECT c.MaChoNgoi, cm.MaChuyenMoi AS MaChuyenTau, t.LoaiToa, c.TangGiuong, cm.LoaiTau, n.SoDiem
INTO #Ghe
FROM #ChuyenMoi cm
JOIN vantai.ToaXeKhach t ON t.MaChuyenTau = cm.MaChuyenMoi
JOIN vantai.ChoNgoi c ON c.MaToaXeKhach = t.MaToaXeKhach AND c.LaGhePhu = 0
JOIN (SELECT MaChuyenTau, COUNT(*) AS SoDiem FROM #Dung GROUP BY MaChuyenTau) n ON n.MaChuyenTau = cm.MaChuyenMoi
WHERE n.SoDiem >= 2
  AND (CAST(HASHBYTES('MD5', CONCAT(c.MaChoNgoi, N'|ls|ban')) AS INT) & 2147483647) % 100 <
      CASE cm.SoHieuMacTau WHEN 'SE1' THEN 75 WHEN 'SE2' THEN 65 WHEN 'SE3' THEN 45
                           WHEN 'SE4' THEN 60 WHEN 'SE19' THEN 55 WHEN 'NA1' THEN 70 ELSE 60 END
      + CASE WHEN DATEPART(WEEKDAY, cm.NgayMoi) IN (1, 6, 7) THEN 10 ELSE 0 END;      -- CN, T6, T7 (DATEFIRST mac dinh 7)

SELECT g.MaChoNgoi, g.MaChuyenTau, g.LoaiToa, g.TangGiuong, g.LoaiTau, a.ViTriDi, b.ViTriDen,
       k.MaKhachHang, k.HoTen, k.SoCCCD, k.LoaiKhach,
       CAST(NULL AS INT) AS MaGaDi, CAST(NULL AS INT) AS MaGaDen, CAST(NULL AS DATETIME2) AS GioDi,
       CAST(NULL AS DECIMAL(12, 2)) AS GiaVeGoc, CAST(NULL AS DECIMAL(12, 2)) AS SoTienGiam,
       CAST(NULL AS INT) AS rn, CAST(NULL AS DATETIME2) AS XuatVe, CAST(NULL AS DATETIME2) AS Hoan,
       CAST(NULL AS DECIMAL(4, 2)) AS TyLeLePhi, CAST(NULL AS VARCHAR(20)) AS TrangThai
INTO #Ve
FROM #Ghe g
CROSS APPLY (SELECT CASE WHEN (CAST(HASHBYTES('MD5', CONCAT(g.MaChoNgoi, N'|ls|chang')) AS INT) & 2147483647) % 100 < 45 THEN 1
                         ELSE 1 + (CAST(HASHBYTES('MD5', CONCAT(g.MaChoNgoi, N'|ls|di')) AS INT) & 2147483647) % (g.SoDiem - 1) END AS ViTriDi) a
CROSS APPLY (SELECT CASE WHEN (CAST(HASHBYTES('MD5', CONCAT(g.MaChoNgoi, N'|ls|chang')) AS INT) & 2147483647) % 100 < 45 THEN g.SoDiem
                         ELSE a.ViTriDi + 1 + (CAST(HASHBYTES('MD5', CONCAT(g.MaChoNgoi, N'|ls|den')) AS INT) & 2147483647) % (g.SoDiem - a.ViTriDi) END AS ViTriDen) b
JOIN #Khach k ON k.i = (CAST(HASHBYTES('MD5', CONCAT(g.MaChoNgoi, N'|ls|khach')) AS INT) & 2147483647) % @SoKhach + 1;

UPDATE v SET MaGaDi = dDi.MaGa, MaGaDen = dDen.MaGa, GioDi = dDi.GioDi
FROM #Ve v
JOIN #Dung dDi ON dDi.MaChuyenTau = v.MaChuyenTau AND dDi.ViTri = v.ViTriDi
JOIN #Dung dDen ON dDen.MaChuyenTau = v.MaChuyenTau AND dDen.ViTri = v.ViTriDen;

UPDATE #Ve SET GiaVeGoc = vantai.fn_TinhGiaVe(LoaiToa, MaGaDi, MaGaDen, TangGiuong, LoaiTau);
DELETE FROM #Ve WHERE GiaVeGoc IS NULL OR GiaVeGoc <= 0 OR MaGaDi IS NULL OR MaGaDen IS NULL;

UPDATE v SET SoTienGiam = ROUND(v.GiaVeGoc * ISNULL((SELECT TRY_CAST(dm.GhiChu AS DECIMAL(4, 2)) FROM danhmuc.DanhMuc dm
                                                    WHERE dm.NhomDanhMuc = 'TI_LE_GIAM_GIA' AND dm.MaCode = v.LoaiKhach), 0), -3)
FROM #Ve v;

;WITH Danh AS (SELECT rn, ROW_NUMBER() OVER (ORDER BY MaChuyenTau, MaChoNgoi) AS so FROM #Ve)
UPDATE Danh SET rn = so;

UPDATE #Ve SET XuatVe = DATEADD(MINUTE, -(120 + (CAST(HASHBYTES('MD5', CONCAT(MaChoNgoi, N'|ls|xuat')) AS INT) & 2147483647) % (20 * 24 * 60)), GioDi);
UPDATE #Ve SET
    TyLeLePhi = CASE WHEN (CAST(HASHBYTES('MD5', CONCAT(MaChoNgoi, N'|ls|lephi')) AS INT) & 2147483647) % 2 = 0 THEN 0.10 ELSE 0.20 END,
    Hoan = DATEADD(HOUR, CASE WHEN (CAST(HASHBYTES('MD5', CONCAT(MaChoNgoi, N'|ls|lephi')) AS INT) & 2147483647) % 2 = 0 THEN -30 ELSE -10 END, GioDi)
WHERE SoTienGiam = 0 AND (CAST(HASHBYTES('MD5', CONCAT(MaChoNgoi, N'|ls|hoan')) AS INT) & 2147483647) % 100 < 3;
UPDATE #Ve SET Hoan = NULL, TyLeLePhi = NULL WHERE Hoan IS NOT NULL AND Hoan <= XuatVe;
UPDATE #Ve SET TrangThai = CASE WHEN Hoan IS NOT NULL THEN 'DA_HOAN_VE' ELSE 'DA_LEN_TAU' END;

INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan, ThoiDiemTao)
SELECT 'PNR_LS_' + RIGHT('0000000' + CAST(rn AS VARCHAR(7)), 7), MaKhachHang, 1, GiaVeGoc - SoTienGiam,
       CASE (CAST(HASHBYTES('MD5', CONCAT(rn, N'|ls|tt')) AS INT) & 2147483647) % 4 WHEN 0 THEN 'TIEN_MAT' WHEN 1 THEN 'CHUYEN_KHOAN'
                                                WHEN 2 THEN 'THE_NGAN_HANG' ELSE 'VI_DIEN_TU' END,
       CASE WHEN TrangThai = 'DA_HOAN_VE' THEN 'DA_HOAN_TIEN' ELSE 'DA_THANH_TOAN' END,
       @NguoiBan, DATEADD(MINUTE, @LechUtcPhut, XuatVe)
FROM #Ve ORDER BY rn;

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach,
                       GiaVeGoc, SoTienGiam, TrangThai, ThoiDiemXuatVe, ThoiDiemSoatVe, MaGaSoat)
SELECT d.MaDonVe, 'TK_LS_' + RIGHT('0000000' + CAST(v.rn AS VARCHAR(7)), 7), v.MaChuyenTau, v.MaChoNgoi,
       v.MaGaDi, v.MaGaDen, v.HoTen, v.SoCCCD, v.GiaVeGoc, v.SoTienGiam, v.TrangThai,
       DATEADD(MINUTE, @LechUtcPhut, v.XuatVe),
       CASE WHEN v.TrangThai = 'DA_LEN_TAU' THEN DATEADD(MINUTE, -15, v.GioDi) END,
       CASE WHEN v.TrangThai = 'DA_LEN_TAU' THEN v.MaGaDi END
FROM #Ve v
JOIN vantai.DonDatVe d ON d.MaPNR = 'PNR_LS_' + RIGHT('0000000' + CAST(v.rn AS VARCHAR(7)), 7)
ORDER BY v.rn;
DECLARE @SoVe INT = @@ROWCOUNT;

INSERT INTO vantai.HoanHuyVe (MaVe, ThoiDiemHoan, TienVeGoc, TyLeLePhi, HinhThucHoan, LyDoHoan, MaNhanVienDuyet)
SELECT ve.MaVe, DATEADD(MINUTE, @LechUtcPhut, v.Hoan), v.GiaVeGoc, v.TyLeLePhi, 'CHUYEN_KHOAN',
       N'Thay đổi kế hoạch cá nhân', @NguoiBan
FROM #Ve v
JOIN vantai.Ve ve ON ve.MaVeCode = 'TK_LS_' + RIGHT('0000000' + CAST(v.rn AS VARCHAR(7)), 7)
WHERE v.TrangThai = 'DA_HOAN_VE';

COMMIT TRANSACTION;

PRINT N'Seed S20261108_1200: ' + CAST(@SoChuyen AS NVARCHAR(10)) + N' chuyến lịch sử, ' + CAST(@SoVe AS NVARCHAR(10)) + N' vé.';
DROP TABLE IF EXISTS #Mau, #ChuyenMoi, #Dung, #Khach, #Ghe, #Ve;
GO
SET NOEXEC OFF;
GO
