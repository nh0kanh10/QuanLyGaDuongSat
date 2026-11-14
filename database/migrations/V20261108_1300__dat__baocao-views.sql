/* =========================================================================
   MIGRATION: Schema baocao + các VIEW chỉ đọc cho trang Báo cáo thống kê (Sprint 3)
   Tác giả  : Đạt
   Ngày     : 08/11/2026
   Đọc bảng : vantai.Ve, HoanHuyVe, ToaXeKhach, ChoNgoi, VanDonHang, LoaiHangHoa; vanhanh.ChuyenTau, MacTauMau,
              LichDungGa; hatang.Ga (bảng A Bá — CHỈ ĐỌC, §7.6 kế hoạch).

   Định nghĩa chỉ số (ghi vào báo cáo):
   - Doanh thu thuần của vé = giá thực thu (GiaVeThucThu) của vé CHƯA HỦY − tiền đã hoàn (SoTienThucHoan).
     Vé DA_HUY (vé cũ khi đổi ghế) KHÔNG tính -> không đếm trùng như báo cáo bán vé hiện tại.
     Vé hoàn còn lại đúng phần lệ phí giữ lại. Ghi nhận theo NGÀY TÀU CHẠY (ChuyenTau.NgayXuatPhat).
   - Hệ số sử dụng ghế = Σ(ghế·km đã bán) ÷ Σ(ghế·km cung ứng) theo chuyến:
       cung ứng = số ghế (không tính ghế phụ) × cự ly hành trình chuyến (ga dừng đầu -> ga dừng cuối);
       đã bán = Σ cự ly chặng của vé còn hiệu lực (DA_DAT, DA_LEN_TAU).
     Tính theo chặng vì một ghế bán được cho nhiều chặng không giao nhau.
   - Đúng giờ: so giờ đến thực tế với kế hoạch tại GA CUỐI; trễ ≤ 5 phút là đúng giờ (ngưỡng trong view,
     đổi ở đây nếu nhóm chốt khác). Chỉ tính chuyến đã có giờ đến thực tế ở ga cuối.
   - Sản lượng hàng: tấn, tấn·km (cự ly theo lý trình ga gửi – ga nhận), cước; vận đơn ghi nhận theo ngày tạo (giờ máy chủ).

   Chạy lại được (CREATE OR ALTER). Thứ tự: sau script gốc.
   Không có USE: chạy bằng  sqlcmd -S . -E -I -f 65001 -d <TênCSDL> -i <file>
   ========================================================================= */
SET NOCOUNT ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'Đang đứng ở CSDL hệ thống. Chạy lại với -d QuanLyDuongSatV2 (hoặc CSDL kiểm thử).', 16, 1);
    SET NOEXEC ON;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'baocao') EXEC (N'CREATE SCHEMA baocao');
GO

-- Cự ly hành trình từng chuyến (ga dừng đầu -> ga dừng cuối)
CREATE OR ALTER VIEW baocao.vw_HanhTrinhChuyen AS
SELECT ct.MaChuyenTau, ct.NgayXuatPhat AS NgayChay, mt.SoHieuMacTau, mt.LoaiTau, ct.TrangThai,
       gDau.MaGa AS MaGaDau, gCuoi.MaGa AS MaGaCuoi,
       ABS(gCuoi.LyTrinhKm - gDau.LyTrinhKm) AS CuLyKm
FROM vanhanh.ChuyenTau ct
JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
CROSS APPLY (SELECT TOP (1) l.MaGa FROM vanhanh.LichDungGa l WHERE l.MaChuyenTau = ct.MaChuyenTau ORDER BY l.ThuTuDung) dau
CROSS APPLY (SELECT TOP (1) l.MaGa FROM vanhanh.LichDungGa l WHERE l.MaChuyenTau = ct.MaChuyenTau ORDER BY l.ThuTuDung DESC) cuoi
JOIN hatang.Ga gDau ON gDau.MaGa = dau.MaGa
JOIN hatang.Ga gCuoi ON gCuoi.MaGa = cuoi.MaGa;
GO

-- Một dòng / vé (trừ vé đã hủy)
CREATE OR ALTER VIEW baocao.vw_DoanhThuVe AS
SELECT v.MaVe, v.MaChuyenTau, ct.NgayXuatPhat AS NgayChay, mt.SoHieuMacTau, mt.LoaiTau,
       v.MaGaDi, gDi.TenGa AS TenGaDi, v.MaGaDen, gDen.TenGa AS TenGaDen, t.LoaiToa,
       v.TrangThai, CAST(IIF(v.TrangThai = 'DA_HOAN_VE', 1, 0) AS BIT) AS LaVeHoan,
       v.GiaVeThucThu AS ThucThu, ISNULL(h.SoTienThucHoan, 0) AS TienHoan,
       v.GiaVeThucThu - ISNULL(h.SoTienThucHoan, 0) AS DoanhThuThuan,
       ABS(gDen.LyTrinhKm - gDi.LyTrinhKm) AS CuLyKm
FROM vantai.Ve v
JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = v.MaChuyenTau
JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
JOIN hatang.Ga gDi ON gDi.MaGa = v.MaGaDi
JOIN hatang.Ga gDen ON gDen.MaGa = v.MaGaDen
JOIN vantai.ChoNgoi c ON c.MaChoNgoi = v.MaChoNgoi
JOIN vantai.ToaXeKhach t ON t.MaToaXeKhach = c.MaToaXeKhach
LEFT JOIN vantai.HoanHuyVe h ON h.MaVe = v.MaVe
WHERE v.TrangThai <> 'DA_HUY' AND ct.TrangThai <> 'DA_HUY';
GO

-- Hệ số sử dụng ghế theo chuyến (ghế·km)
CREATE OR ALTER VIEW baocao.vw_HeSoSuDungGhe AS
SELECT ht.MaChuyenTau, ht.NgayChay, ht.SoHieuMacTau, ht.LoaiTau, ht.CuLyKm,
       g.SoGhe, ISNULL(b.SoVe, 0) AS SoVe,
       CAST(g.SoGhe * ht.CuLyKm AS DECIMAL(18, 2)) AS GheKmCungUng,
       CAST(ISNULL(b.GheKmDaBan, 0) AS DECIMAL(18, 2)) AS GheKmDaBan,
       CAST(CASE WHEN g.SoGhe * ht.CuLyKm > 0 THEN ISNULL(b.GheKmDaBan, 0) / (g.SoGhe * ht.CuLyKm) ELSE 0 END AS DECIMAL(9, 4)) AS HeSo
FROM baocao.vw_HanhTrinhChuyen ht
CROSS APPLY (SELECT COUNT(*) AS SoGhe
             FROM vantai.ToaXeKhach t JOIN vantai.ChoNgoi c ON c.MaToaXeKhach = t.MaToaXeKhach
             WHERE t.MaChuyenTau = ht.MaChuyenTau AND c.LaGhePhu = 0) g
OUTER APPLY (SELECT COUNT(*) AS SoVe, SUM(ABS(gDen.LyTrinhKm - gDi.LyTrinhKm)) AS GheKmDaBan
             FROM vantai.Ve v
             JOIN hatang.Ga gDi ON gDi.MaGa = v.MaGaDi
             JOIN hatang.Ga gDen ON gDen.MaGa = v.MaGaDen
             WHERE v.MaChuyenTau = ht.MaChuyenTau AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')) b
WHERE ht.TrangThai <> 'DA_HUY' AND g.SoGhe > 0;
GO

-- Đúng giờ theo chuyến: so giờ đến ga cuối (thực tế vs kế hoạch); ngưỡng đúng giờ 5 phút
CREATE OR ALTER VIEW baocao.vw_DungGioChuyen AS
SELECT ht.MaChuyenTau, ht.NgayChay, ht.SoHieuMacTau, ht.LoaiTau,
       l.GioDenKeHoach, l.GioDenThucTe,
       DATEDIFF(MINUTE, l.GioDenKeHoach, l.GioDenThucTe) AS SoPhutTre,
       CAST(IIF(DATEDIFF(MINUTE, l.GioDenKeHoach, l.GioDenThucTe) <= 5, 1, 0) AS BIT) AS DungGio,
       (SELECT COUNT(*) FROM vanhanh.NhatKyChamGio nk WHERE nk.MaChuyenTau = ht.MaChuyenTau) AS SoSuCo
FROM baocao.vw_HanhTrinhChuyen ht
CROSS APPLY (SELECT TOP (1) x.GioDenKeHoach, x.GioDenThucTe FROM vanhanh.LichDungGa x
             WHERE x.MaChuyenTau = ht.MaChuyenTau ORDER BY x.ThuTuDung DESC) l
WHERE l.GioDenThucTe IS NOT NULL AND ht.TrangThai <> 'DA_HUY';
GO

-- Sản lượng hàng hóa: một dòng / vận đơn
CREATE OR ALTER VIEW baocao.vw_SanLuongHang AS
SELECT vd.MaVanDon, vd.MaVanDonCode,
       CAST(DATEADD(MINUTE, DATEDIFF(MINUTE, SYSUTCDATETIME(), SYSDATETIME()), vd.ThoiDiemTao) AS DATE) AS NgayTao,
       lh.TenHangHoa, vd.LoaiVanChuyen, vd.TrangThai,
       vd.MaGaGui, gG.TenGa AS TenGaGui, vd.MaGaNhan, gN.TenGa AS TenGaNhan,
       vd.TrongLuongTan, ABS(gN.LyTrinhKm - gG.LyTrinhKm) AS CuLyKm,
       CAST(vd.TrongLuongTan * ABS(gN.LyTrinhKm - gG.LyTrinhKm) AS DECIMAL(14, 2)) AS TanKm,
       vd.CuocPhi
FROM vantai.VanDonHang vd
JOIN vantai.LoaiHangHoa lh ON lh.MaLoaiHang = vd.MaLoaiHang
JOIN hatang.Ga gG ON gG.MaGa = vd.MaGaGui
JOIN hatang.Ga gN ON gN.MaGa = vd.MaGaNhan;
GO

SELECT name AS ViewBaoCao FROM sys.views WHERE schema_id = SCHEMA_ID(N'baocao') ORDER BY name;
GO
SET NOEXEC OFF;
GO
