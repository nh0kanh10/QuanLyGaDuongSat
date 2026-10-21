/* =========================================================================
   KIỂM TRA DỮ LIỆU PHƯƠNG TIỆN + NHÂN SỰ THEO LUẬT NGHIỆP VỤ (Sprint 1)
   Tác giả: Đạt · 09/10/2026
   Chỉ ĐỌC (bảng tạm #), không sửa dữ liệu. Chạy lại được bất cứ lúc nào, kể cả sau khi dùng app.
   Lệnh: sqlcmd -S . -E -I -f 65001 -d QuanLyDuongSatV2 -i <file>

   Kết quả: bảng tổng hợp theo luật + danh sách chi tiết.
     LOI     = vi phạm luật nghiệp vụ trong dữ liệu của Đạt (Phương tiện / Nhân sự) → có thì báo lỗi cuối file.
     GHI_CHU = điểm lệch trong dữ liệu của phân hệ khác (A Bá) → chỉ liệt kê để báo, không tính là lỗi.

   Luật lấy từ giao diện của Đạt (LapTauKeoThaDialog, DanhGiaKipLai) và PHAN_TICH_NHAN_SU_KIP_LAI.md (R1–R13):
   PT-01..PT-11 phương tiện / lập tàu · NS-01..NS-13 nhân sự / kíp lái · AB-* ghi chú dữ liệu A Bá.
   ========================================================================= */
SET NOCOUNT ON;

DECLARE @KetQua TABLE (Muc VARCHAR(10), Luat VARCHAR(10), MoTaLuat NVARCHAR(200), ChiTiet NVARCHAR(400));

DECLARE @SoToaToiDa INT = 20, @ChieuDaiToiDa DECIMAL(6,1) = 450.0, @TrucToiDa DECIMAL(5,2) = 14.0,
        @PhutDoCatNoi INT = 15, @GioNghiToiThieu DECIMAL(4,1) = 8.0;

-- Mốc "hôm nay" theo dữ liệu (ngày của chuyến SE19), giống file seed
DECLARE @NgayNay DATE = (
    SELECT TOP (1) ct.NgayXuatPhat FROM vanhanh.ChuyenTau ct
    JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau
    WHERE m.SoHieuMacTau = 'SE19' ORDER BY ct.NgayXuatPhat DESC);

IF OBJECT_ID('tempdb..#Chuyen') IS NOT NULL DROP TABLE #Chuyen;
IF OBJECT_ID('tempdb..#Kip') IS NOT NULL DROP TABLE #Kip;
IF OBJECT_ID('tempdb..#NguoiKip') IS NOT NULL DROP TABLE #NguoiKip;
IF OBJECT_ID('tempdb..#ToaChang') IS NOT NULL DROP TABLE #ToaChang;
IF OBJECT_ID('tempdb..#Chang') IS NOT NULL DROP TABLE #Chang;

/* ---------- Chuyến tàu ---------- */
SELECT ct.MaChuyenTau, m.SoHieuMacTau AS Mac, m.LoaiTau, ct.TrangThai, ct.NgayXuatPhat,
       ct.GioXuatPhatKH AS GioXP, ct.GioVeDichKH AS GioVD,
       CAST(CASE WHEN ct.TrangThai IN ('HOAN_THANH', 'DA_HUY') THEN 0 ELSE 1 END AS BIT) AS ConHieuLuc,
       (SELECT MAX(ld.ThuTuDung) FROM vanhanh.LichDungGa ld WHERE ld.MaChuyenTau = ct.MaChuyenTau) AS SoGa,
       ISNULL((SELECT MAX(ld.ThuTuDung) FROM vanhanh.LichDungGa ld
               WHERE ld.MaChuyenTau = ct.MaChuyenTau AND ld.GioDiThucTe IS NOT NULL), 0) AS DaQuaGa,
       dt.MaDoanTau, dt.MaDauMayChinh, dt.MaDauMayDay, dt.TongSoToa, dt.TongChieuDaiM, dt.TongTrongLuongTan,
       ddm.MaDongCode AS DongDauMay, ddm.ChieuDaiM AS ChieuDaiDauMay, ddm.SucKeoToiDaTan AS SucKeo,
       (SELECT MIN(dr.ChieuDaiHuuDungM) FROM vanhanh.LichDungGa ld
        JOIN hatang.DuongRayGa dr ON dr.MaGa = ld.MaGa AND dr.LoaiDuong = 'DUONG_TRANH'
        WHERE ld.MaChuyenTau = ct.MaChuyenTau) AS DuongTranhNganNhat
INTO #Chuyen
FROM vanhanh.ChuyenTau ct
JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau
LEFT JOIN vanhanh.DoanTau dt ON dt.MaChuyenTau = ct.MaChuyenTau
LEFT JOIN phuongtien.DauMay dm ON dm.MaDauMay = dt.MaDauMayChinh
LEFT JOIN phuongtien.DongDauMay ddm ON ddm.MaDongDauMay = dm.MaDongDauMay;

/* ---------- Toa trên từng chặng (khu đoạn ga k → k+1) ---------- */
SELECT c.MaDoanTau, c.MaChuyenTau, ld.ThuTuDung AS K
INTO #Chang
FROM #Chuyen c JOIN vanhanh.LichDungGa ld ON ld.MaChuyenTau = c.MaChuyenTau
WHERE c.MaDoanTau IS NOT NULL AND ld.ThuTuDung < c.SoGa;

SELECT ch.MaDoanTau, ch.MaChuyenTau, ch.K, ct.MaToaXe, ct.ThuTuNoi, tx.SoHieuToaXe, cl.MaChungLoaiCode AS Loai,
       CAST(CASE WHEN cl.MaChungLoaiCode IN ('G', 'M', 'P') THEN 1 ELSE 0 END AS BIT) AS LaHang,
       cl.ChieuDaiChuanM AS ChieuDai, tx.TuTrongTan + tx.TaiTrongToiDaTan AS TrongLuong,
       ROW_NUMBER() OVER (PARTITION BY ch.MaDoanTau, ch.K ORDER BY ct.ThuTuNoi) AS ViTri,
       COUNT(*) OVER (PARTITION BY ch.MaDoanTau, ch.K) AS SoToa
INTO #ToaChang
FROM #Chang ch
JOIN vanhanh.ChiTietDoanTau ct ON ct.MaDoanTau = ch.MaDoanTau
JOIN phuongtien.ToaXe tx ON tx.MaToaXe = ct.MaToaXe
JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
LEFT JOIN vanhanh.LichDungGa lNoi ON lNoi.MaChuyenTau = ch.MaChuyenTau AND lNoi.MaGa = ct.MaGaNoi
LEFT JOIN vanhanh.LichDungGa lCat ON lCat.MaChuyenTau = ch.MaChuyenTau AND lCat.MaGa = ct.MaGaCat
WHERE ISNULL(lNoi.ThuTuDung, 1) <= ch.K AND ch.K < ISNULL(lCat.ThuTuDung, 2147483647);

/* ---------- Kíp + người trong kíp ---------- */
SELECT k.MaPhanCong, k.MaChuyenTau, c.Mac, k.TrangThai, c.TrangThai AS TrangThaiChuyen, c.ConHieuLuc,
       c.GioXP, c.GioVD, c.DaQuaGa, c.DongDauMay,
       gN.MaGaCode AS GaNhan, gG.MaGaCode AS GaGiao,
       ISNULL(lN.ThuTuDung, 1) AS ViTriNhan, ISNULL(lG.ThuTuDung, ISNULL(c.SoGa, 2)) AS ViTriGiao,
       ISNULL(lN.GioDiKeHoach, c.GioXP) AS GioNhan, ISNULL(lG.GioDenKeHoach, c.GioVD) AS GioGiao,
       k.MaLaiTau, k.MaPhuLai, k.MaTruongTau
INTO #Kip
FROM nhansu.PhanCongKipLai k
JOIN #Chuyen c ON c.MaChuyenTau = k.MaChuyenTau
JOIN hatang.Ga gN ON gN.MaGa = k.MaGaNhanBan
JOIN hatang.Ga gG ON gG.MaGa = k.MaGaBanGiao
LEFT JOIN vanhanh.LichDungGa lN ON lN.MaChuyenTau = k.MaChuyenTau AND lN.MaGa = k.MaGaNhanBan
LEFT JOIN vanhanh.LichDungGa lG ON lG.MaChuyenTau = k.MaChuyenTau AND lG.MaGa = k.MaGaBanGiao;

SELECT k.*, v.VaiTro, v.MaNhanVien, nv.MaNVCode, nv.ChucDanh, nv.HangBangLai, nv.HanKhamSucKhoe, nv.TrangThai AS TrangThaiNV
INTO #NguoiKip
FROM #Kip k
CROSS APPLY (VALUES ('LAI_TAU', k.MaLaiTau), ('PHU_LAI', k.MaPhuLai), ('TRUONG_TAU', k.MaTruongTau)) v(VaiTro, MaNhanVien)
JOIN nhansu.NhanVien nv ON nv.MaNhanVien = v.MaNhanVien;

/* =========================================================================
   PHƯƠNG TIỆN
   ========================================================================= */
-- PT-01 Số toa từng chặng 1..20, TongSoToa = lớn nhất
INSERT INTO @KetQua
SELECT 'LOI', 'PT-01', N'Số toa mỗi chặng 1..20 và TongSoToa = số toa lớn nhất trên các chặng',
       CONCAT(c.Mac, N': lớn nhất ', x.SoToaMax, N' toa, nhỏ nhất ', x.SoToaMin, N', DoanTau ghi ', c.TongSoToa)
FROM #Chuyen c
JOIN (SELECT ch.MaDoanTau, MAX(ISNULL(t.SoToa, 0)) AS SoToaMax, MIN(ISNULL(t.SoToa, 0)) AS SoToaMin
      FROM #Chang ch
      LEFT JOIN (SELECT DISTINCT MaDoanTau, K, SoToa FROM #ToaChang) t ON t.MaDoanTau = ch.MaDoanTau AND t.K = ch.K
      GROUP BY ch.MaDoanTau) x ON x.MaDoanTau = c.MaDoanTau
WHERE EXISTS (SELECT 1 FROM vanhanh.ChiTietDoanTau d WHERE d.MaDoanTau = c.MaDoanTau)
  AND (x.SoToaMin < 1 OR x.SoToaMax > @SoToaToiDa OR x.SoToaMax <> c.TongSoToa);

-- PT-02 Chiều dài (đầu máy chính + toa) <= đường tránh ngắn nhất trên hành trình và <= 450 m
-- PT-03 Trọng lượng toàn tải <= sức kéo đầu máy chính
;WITH SoLieu AS (
    SELECT MaDoanTau, K, SUM(ChieuDai) AS ChieuDaiToa, SUM(TrongLuong) AS TrongLuong
    FROM #ToaChang GROUP BY MaDoanTau, K
), LonNhat AS (
    SELECT MaDoanTau, MAX(ChieuDaiToa) AS ChieuDaiToa, MAX(TrongLuong) AS TrongLuong FROM SoLieu GROUP BY MaDoanTau
)
INSERT INTO @KetQua
SELECT 'LOI', 'PT-02', N'Chiều dài đoàn tàu <= đường tránh ngắn nhất (và <= 450 m), khớp TongChieuDaiM',
       CONCAT(c.Mac, N': dài ', c.ChieuDaiDauMay + ln.ChieuDaiToa, N' m, đường tránh ', c.DuongTranhNganNhat,
              N' m, DoanTau ghi ', c.TongChieuDaiM)
FROM #Chuyen c JOIN LonNhat ln ON ln.MaDoanTau = c.MaDoanTau
WHERE c.ChieuDaiDauMay + ln.ChieuDaiToa > ISNULL(c.DuongTranhNganNhat, @ChieuDaiToiDa)
   OR c.ChieuDaiDauMay + ln.ChieuDaiToa > @ChieuDaiToiDa
   OR c.ChieuDaiDauMay + ln.ChieuDaiToa <> c.TongChieuDaiM
UNION ALL
SELECT 'LOI', 'PT-03', N'Trọng lượng toàn tải <= sức kéo đầu máy chính, khớp TongTrongLuongTan',
       CONCAT(c.Mac, N': ', ln.TrongLuong, N' t / sức kéo ', c.SucKeo, N' t, DoanTau ghi ', c.TongTrongLuongTan)
FROM #Chuyen c JOIN LonNhat ln ON ln.MaDoanTau = c.MaDoanTau
WHERE ln.TrongLuong > c.SucKeo OR ln.TrongLuong <> c.TongTrongLuongTan;

-- PT-04 Tải trọng trục mỗi toa trong đoàn tàu <= 14 t (chở đầy)
INSERT INTO @KetQua
SELECT DISTINCT 'LOI', 'PT-04', N'Tải trọng trục <= 14 t/trục khi chở đầy',
       CONCAT(c.Mac, N': ', tx.SoHieuToaXe, N' ', CAST((tx.TuTrongTan + tx.TaiTrongToiDaTan) / cl.SoTruc AS DECIMAL(5,2)), N' t/trục')
FROM vanhanh.ChiTietDoanTau ct
JOIN #Chuyen c ON c.MaDoanTau = ct.MaDoanTau
JOIN phuongtien.ToaXe tx ON tx.MaToaXe = ct.MaToaXe
JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
WHERE (tx.TuTrongTan + tx.TaiTrongToiDaTan) / cl.SoTruc > @TrucToiDa;

-- PT-05 Toa hàng liền khối ở đầu hoặc cuối đoàn trên từng chặng
INSERT INTO @KetQua
SELECT 'LOI', 'PT-05', N'Toa hàng (G / M / P) liền khối, ngay sau đầu máy hoặc ở cuối đoàn (từng chặng)',
       CONCAT(c.Mac, N' chặng ', h.K, N': toa hàng ở vị trí ', h.ViTriDau, N'..', h.ViTriCuoi, N' / ', h.SoToa)
FROM (SELECT MaDoanTau, K, MIN(SoToa) AS SoToa, COUNT(*) AS SoHang, MIN(ViTri) AS ViTriDau, MAX(ViTri) AS ViTriCuoi
      FROM #ToaChang WHERE LaHang = 1 GROUP BY MaDoanTau, K) h
JOIN #Chuyen c ON c.MaDoanTau = h.MaDoanTau
WHERE h.SoHang > 0 AND h.SoHang < h.SoToa
  AND NOT (h.ViTriCuoi - h.ViTriDau + 1 = h.SoHang AND (h.ViTriDau = 1 OR h.ViTriCuoi = h.SoToa));

-- PT-06 Ga nối / ga cắt: là ga trung gian trên hành trình, nối trước cắt, đỗ >= 15 phút
INSERT INTO @KetQua
SELECT 'LOI', 'PT-06', N'Ga nối / cắt là ga trung gian của hành trình, nối trước cắt, đỗ >= 15 phút',
       CONCAT(c.Mac, N': ', tx.SoHieuToaXe, N' nối ', ISNULL(gN.MaGaCode, '-'), N' cắt ', ISNULL(gC.MaGaCode, '-'),
              N' (đỗ ', ISNULL(DATEDIFF(MINUTE, lN.GioDenKeHoach, lN.GioDiKeHoach), -1), N' / ',
              ISNULL(DATEDIFF(MINUTE, lC.GioDenKeHoach, lC.GioDiKeHoach), -1), N' phút)')
FROM vanhanh.ChiTietDoanTau ct
JOIN #Chuyen c ON c.MaDoanTau = ct.MaDoanTau
JOIN phuongtien.ToaXe tx ON tx.MaToaXe = ct.MaToaXe
LEFT JOIN hatang.Ga gN ON gN.MaGa = ct.MaGaNoi
LEFT JOIN hatang.Ga gC ON gC.MaGa = ct.MaGaCat
LEFT JOIN vanhanh.LichDungGa lN ON lN.MaChuyenTau = c.MaChuyenTau AND lN.MaGa = ct.MaGaNoi
LEFT JOIN vanhanh.LichDungGa lC ON lC.MaChuyenTau = c.MaChuyenTau AND lC.MaGa = ct.MaGaCat
WHERE (ct.MaGaNoi IS NOT NULL OR ct.MaGaCat IS NOT NULL)
  AND (   (ct.MaGaNoi IS NOT NULL AND (lN.ThuTuDung IS NULL OR lN.ThuTuDung IN (1, c.SoGa)
                                       OR DATEDIFF(MINUTE, lN.GioDenKeHoach, lN.GioDiKeHoach) < @PhutDoCatNoi))
       OR (ct.MaGaCat IS NOT NULL AND (lC.ThuTuDung IS NULL OR lC.ThuTuDung IN (1, c.SoGa)
                                       OR DATEDIFF(MINUTE, lC.GioDenKeHoach, lC.GioDiKeHoach) < @PhutDoCatNoi))
       OR (lN.ThuTuDung IS NOT NULL AND lC.ThuTuDung IS NOT NULL AND lN.ThuTuDung >= lC.ThuTuDung));

-- PT-07 Nhóm toa nối / cắt tại một ga liền khối ở cuối đoàn hoặc ngay sau đầu máy
;WITH Nhom AS (
    SELECT ct.MaDoanTau, x.Loai, x.MaGa, MIN(ct.ThuTuNoi) AS Dau, MAX(ct.ThuTuNoi) AS Cuoi, COUNT(*) AS So
    FROM vanhanh.ChiTietDoanTau ct
    CROSS APPLY (VALUES (N'nối', ct.MaGaNoi), (N'cắt', ct.MaGaCat)) x(Loai, MaGa)
    WHERE x.MaGa IS NOT NULL
    GROUP BY ct.MaDoanTau, x.Loai, x.MaGa
)
INSERT INTO @KetQua
SELECT 'LOI', 'PT-07', N'Nhóm toa nối / cắt tại một ga liền khối ở cuối đoàn hoặc ngay sau đầu máy',
       CONCAT(c.Mac, N': ', n.Loai, N' tại ', g.MaGaCode, N' vị trí ', n.Dau, N'..', n.Cuoi)
FROM Nhom n
JOIN #Chuyen c ON c.MaDoanTau = n.MaDoanTau
JOIN hatang.Ga g ON g.MaGa = n.MaGa
WHERE NOT (n.Cuoi - n.Dau + 1 = n.So
           AND (n.Dau = 1 OR n.Cuoi = (SELECT MAX(d.ThuTuNoi) FROM vanhanh.ChiTietDoanTau d WHERE d.MaDoanTau = n.MaDoanTau)));

-- PT-08 Đầu máy không kéo / đẩy 2 chuyến chồng giờ; đầu máy chính khác đầu máy đẩy
;WITH DauMayChuyen AS (
    SELECT MaChuyenTau, Mac, GioXP, GioVD, MaDauMayChinh AS MaDauMay FROM #Chuyen WHERE ConHieuLuc = 1 AND MaDoanTau IS NOT NULL
    UNION ALL
    SELECT MaChuyenTau, Mac, GioXP, GioVD, MaDauMayDay FROM #Chuyen WHERE ConHieuLuc = 1 AND MaDauMayDay IS NOT NULL
)
INSERT INTO @KetQua
SELECT 'LOI', 'PT-08', N'Một đầu máy không kéo / đẩy 2 chuyến chồng khung giờ',
       CONCAT(dm.SoHieuDauMay, N': ', a.Mac, N' và ', b.Mac)
FROM DauMayChuyen a
JOIN DauMayChuyen b ON b.MaDauMay = a.MaDauMay AND a.MaChuyenTau < b.MaChuyenTau
JOIN phuongtien.DauMay dm ON dm.MaDauMay = a.MaDauMay
WHERE IIF(a.GioXP > b.GioXP, a.GioXP, b.GioXP) < IIF(a.GioVD < b.GioVD, a.GioVD, b.GioVD)
UNION ALL
SELECT 'LOI', 'PT-08', N'Một đầu máy không kéo / đẩy 2 chuyến chồng khung giờ', CONCAT(c.Mac, N': đầu máy chính trùng đầu máy đẩy')
FROM #Chuyen c WHERE c.MaDauMayChinh = c.MaDauMayDay;

-- PT-09 Một toa không nằm trong đoàn tàu của 2 chuyến chồng giờ
INSERT INTO @KetQua
SELECT 'LOI', 'PT-09', N'Một toa không thuộc đoàn tàu của 2 chuyến chồng khung giờ',
       CONCAT(tx.SoHieuToaXe, N': ', a.Mac, N' và ', b.Mac)
FROM vanhanh.ChiTietDoanTau x
JOIN #Chuyen a ON a.MaDoanTau = x.MaDoanTau AND a.ConHieuLuc = 1
JOIN vanhanh.ChiTietDoanTau y ON y.MaToaXe = x.MaToaXe AND y.MaDoanTau <> x.MaDoanTau
JOIN #Chuyen b ON b.MaDoanTau = y.MaDoanTau AND b.ConHieuLuc = 1 AND a.MaChuyenTau < b.MaChuyenTau
JOIN phuongtien.ToaXe tx ON tx.MaToaXe = x.MaToaXe
WHERE IIF(a.GioXP > b.GioXP, a.GioXP, b.GioXP) < IIF(a.GioVD < b.GioVD, a.GioVD, b.GioVD);

-- PT-10 Đầu máy / toa đang bảo dưỡng hoặc đã ngừng khai thác (14/10) không được đưa vào chuyến còn hiệu lực
INSERT INTO @KetQua
SELECT 'LOI', 'PT-10', N'Đầu máy / toa BAO_DUONG hoặc NGUNG_KHAI_THAC không nằm trong đoàn tàu của chuyến còn hiệu lực',
       CONCAT(c.Mac, N': ', dm.SoHieuDauMay, N' (', dm.TrangThai, N')')
FROM #Chuyen c JOIN phuongtien.DauMay dm ON dm.MaDauMay IN (c.MaDauMayChinh, c.MaDauMayDay)
WHERE c.ConHieuLuc = 1 AND dm.TrangThai IN ('BAO_DUONG', 'NGUNG_KHAI_THAC')
UNION ALL
SELECT 'LOI', 'PT-10', N'Đầu máy / toa BAO_DUONG hoặc NGUNG_KHAI_THAC không nằm trong đoàn tàu của chuyến còn hiệu lực',
       CONCAT(c.Mac, N': ', tx.SoHieuToaXe, N' (', tx.TrangThai, N')')
FROM vanhanh.ChiTietDoanTau ct JOIN #Chuyen c ON c.MaDoanTau = ct.MaDoanTau
JOIN phuongtien.ToaXe tx ON tx.MaToaXe = ct.MaToaXe
WHERE c.ConHieuLuc = 1 AND tx.TrangThai IN ('BAO_DUONG', 'NGUNG_KHAI_THAC');

-- PT-11 Trạng thái khớp việc đang làm:
--   toa chạy suốt trong chuyến DANG_CHAY → DA_NOI; toa DA_NOI → phải thuộc đoàn tàu của chuyến còn hiệu lực;
--   đầu máy kéo / đẩy chuyến DANG_CHAY → DANG_CHAY; đầu máy DANG_CHAY → phải đang kéo / đẩy chuyến DANG_CHAY.
INSERT INTO @KetQua
SELECT 'LOI', 'PT-11', N'Trạng thái toa / đầu máy khớp chuyến đang chạy', CONCAT(c.Mac, N': ', tx.SoHieuToaXe, N' đang ', tx.TrangThai)
FROM vanhanh.ChiTietDoanTau ct JOIN #Chuyen c ON c.MaDoanTau = ct.MaDoanTau
JOIN phuongtien.ToaXe tx ON tx.MaToaXe = ct.MaToaXe
WHERE c.TrangThai = 'DANG_CHAY' AND ct.MaGaNoi IS NULL AND ct.MaGaCat IS NULL AND tx.TrangThai <> 'DA_NOI'
UNION ALL
SELECT 'LOI', 'PT-11', N'Trạng thái toa / đầu máy khớp chuyến đang chạy', CONCAT(tx.SoHieuToaXe, N' DA_NOI nhưng không thuộc đoàn tàu nào còn hiệu lực')
FROM phuongtien.ToaXe tx
WHERE tx.TrangThai = 'DA_NOI'
  AND NOT EXISTS (SELECT 1 FROM vanhanh.ChiTietDoanTau ct JOIN #Chuyen c ON c.MaDoanTau = ct.MaDoanTau
                  WHERE ct.MaToaXe = tx.MaToaXe AND c.ConHieuLuc = 1)
UNION ALL
SELECT 'LOI', 'PT-11', N'Trạng thái toa / đầu máy khớp chuyến đang chạy', CONCAT(c.Mac, N': ', dm.SoHieuDauMay, N' đang ', dm.TrangThai)
FROM #Chuyen c JOIN phuongtien.DauMay dm ON dm.MaDauMay IN (c.MaDauMayChinh, c.MaDauMayDay)
WHERE c.TrangThai = 'DANG_CHAY' AND dm.TrangThai <> 'DANG_CHAY'
UNION ALL
SELECT 'LOI', 'PT-11', N'Trạng thái toa / đầu máy khớp chuyến đang chạy', CONCAT(dm.SoHieuDauMay, N' DANG_CHAY nhưng không kéo / đẩy chuyến nào đang chạy')
FROM phuongtien.DauMay dm
WHERE dm.TrangThai = 'DANG_CHAY'
  AND NOT EXISTS (SELECT 1 FROM #Chuyen c WHERE c.TrangThai = 'DANG_CHAY' AND dm.MaDauMay IN (c.MaDauMayChinh, c.MaDauMayDay));

/* =========================================================================
   NHÂN SỰ
   ========================================================================= */
-- NS-01 (R2) Đúng chức danh từng vị trí
INSERT INTO @KetQua
SELECT 'LOI', 'NS-01', N'R2 · mỗi vị trí đúng chức danh',
       CONCAT(n.Mac, N' ', n.GaNhan, N'→', n.GaGiao, N': ', n.MaNVCode, N' (', n.ChucDanh, N') ở vị trí ', n.VaiTro)
FROM #NguoiKip n
WHERE n.ChucDanh <> CASE n.VaiTro WHEN 'LAI_TAU' THEN N'Lái tàu' WHEN 'PHU_LAI' THEN N'Phụ lái' ELSE N'Trưởng tàu' END;

-- NS-02 (R3) Một người không ở kíp của 2 chuyến chồng khung giờ (cả trưởng tàu)
INSERT INTO @KetQua
SELECT DISTINCT 'LOI', 'NS-02', N'R3 · một người không ở 2 chuyến chồng khung giờ',
       CONCAT(a.MaNVCode, N': ', a.Mac, N' và ', b.Mac)
FROM #NguoiKip a
JOIN #NguoiKip b ON b.MaNhanVien = a.MaNhanVien AND a.MaChuyenTau < b.MaChuyenTau
WHERE a.ConHieuLuc = 1 AND b.ConHieuLuc = 1
  AND IIF(a.GioXP > b.GioXP, a.GioXP, b.GioXP) < IIF(a.GioVD < b.GioVD, a.GioVD, b.GioVD);

-- NS-03 (R3 cùng chuyến) Lái tàu / phụ lái chỉ 1 kíp mỗi chuyến; trưởng tàu nhiều kíp thì không chồng chặng
INSERT INTO @KetQua
SELECT 'LOI', 'NS-03', N'R3 · cùng chuyến: lái tàu / phụ lái 1 kíp, trưởng tàu không chồng chặng',
       CONCAT(a.MaNVCode, N' ', a.Mac, N': ', a.GaNhan, N'→', a.GaGiao, N' và ', b.GaNhan, N'→', b.GaGiao)
FROM #NguoiKip a
JOIN #NguoiKip b ON b.MaNhanVien = a.MaNhanVien AND b.MaChuyenTau = a.MaChuyenTau AND a.MaPhanCong < b.MaPhanCong
WHERE NOT (a.VaiTro = 'TRUONG_TAU' AND b.VaiTro = 'TRUONG_TAU'
           AND (a.ViTriGiao <= b.ViTriNhan OR b.ViTriGiao <= a.ViTriNhan));

-- NS-04 (R4) Nghỉ >= 8 giờ giữa 2 ca thuộc 2 chuyến khác nhau
--       (2 chuyến chồng khung giờ thì NS-02 đã báo, ở đây bỏ qua để không báo trùng)
INSERT INTO @KetQua
SELECT DISTINCT 'LOI', 'NS-04', N'R4 · nghỉ >= 8 giờ giữa hai ca khác chuyến',
       CONCAT(a.MaNVCode, N': giao ', a.Mac, N' ', FORMAT(a.GioGiao, 'HH:mm dd/MM'), N' → nhận ', b.Mac, N' ',
              FORMAT(b.GioNhan, 'HH:mm dd/MM'), N' (', CAST(DATEDIFF(MINUTE, a.GioGiao, b.GioNhan) / 60.0 AS DECIMAL(5,1)), N' giờ)')
FROM #NguoiKip a
JOIN #NguoiKip b ON b.MaNhanVien = a.MaNhanVien AND b.MaChuyenTau <> a.MaChuyenTau AND b.GioNhan >= a.GioNhan
WHERE DATEDIFF(MINUTE, a.GioGiao, b.GioNhan) < @GioNghiToiThieu * 60
  AND NOT (a.ConHieuLuc = 1 AND b.ConHieuLuc = 1
           AND IIF(a.GioXP > b.GioXP, a.GioXP, b.GioXP) < IIF(a.GioVD < b.GioVD, a.GioVD, b.GioVD));

-- NS-05 (R5 R6) Kíp đã nhận ban (DANG_THUC_HIEN / HOAN_THANH): cả 3 người có phiếu đạt cho chuyến đó, đo trước giờ bàn giao
--               kế hoạch của kíp (11/10: nới từ "trước giờ nhận ban" - tàu trễ thì kíp đo muộn hơn giờ nhận ban kế hoạch);
--               không ai trong kíp của chuyến có phiếu KHÔNG đạt cho chính chuyến đó
INSERT INTO @KetQua
SELECT 'LOI', 'NS-05', N'R6 · kíp đã nhận ban có kiểm tra đạt của cả 3 người, đo trước giờ bàn giao',
       CONCAT(n.Mac, N' ', n.GaNhan, N'→', n.GaGiao, N': ', n.MaNVCode, N' chưa có phiếu đạt trước ', FORMAT(n.GioGiao, 'HH:mm dd/MM'))
FROM #NguoiKip n
WHERE n.TrangThai IN ('DANG_THUC_HIEN', 'HOAN_THANH')
  AND NOT EXISTS (
      SELECT 1 FROM nhansu.KiemTraSucKhoe ks
      WHERE ks.MaNhanVien = n.MaNhanVien AND ks.MaChuyenTau = n.MaChuyenTau
        AND ks.DuDieuKien = 1 AND ks.ThoiDiemKiemTra <= n.GioGiao)
UNION ALL
SELECT 'LOI', 'NS-05', N'R6 · người đã kiểm tra không đạt cho chuyến thì không được ở kíp của chuyến đó',
       CONCAT(n.Mac, N': ', n.MaNVCode, N' có phiếu không đạt cho chuyến này')
FROM #NguoiKip n
WHERE EXISTS (SELECT 1 FROM nhansu.KiemTraSucKhoe ks
              WHERE ks.MaNhanVien = n.MaNhanVien AND ks.MaChuyenTau = n.MaChuyenTau AND ks.DuDieuKien = 0);

-- NS-06 (R7) Bằng lái của lái tàu / phụ lái = dòng đầu máy chính
INSERT INTO @KetQua
SELECT 'LOI', 'NS-06', N'R7 · hạng bằng lái = dòng đầu máy chính',
       CONCAT(n.Mac, N': ', n.MaNVCode, N' bằng ', ISNULL(n.HangBangLai, '-'), N', đầu máy ', n.DongDauMay)
FROM #NguoiKip n
WHERE n.VaiTro IN ('LAI_TAU', 'PHU_LAI') AND n.DongDauMay IS NOT NULL
  AND ISNULL(n.HangBangLai, '') <> n.DongDauMay;

-- NS-07 (R8) Hạn khám sức khoẻ còn tới ngày nhận ban
INSERT INTO @KetQua
SELECT 'LOI', 'NS-07', N'R8 · hạn khám sức khỏe còn hiệu lực tới ngày nhận ban',
       CONCAT(n.Mac, N': ', n.MaNVCode, N' hết hạn ', FORMAT(n.HanKhamSucKhoe, 'dd/MM/yyyy'))
FROM #NguoiKip n
WHERE n.HanKhamSucKhoe < CAST(n.GioNhan AS DATE);

-- NS-08 (R9) Không phân công người đã nghỉ việc
INSERT INTO @KetQua
SELECT 'LOI', 'NS-08', N'R9 · không phân công người đã nghỉ việc', CONCAT(n.Mac, N': ', n.MaNVCode)
FROM #NguoiKip n WHERE n.TrangThaiNV = 'DA_NGHI_VIEC';

-- NS-09 (R11) Chuyến khách đang chạy / chạy hôm nay: kíp nối liền, phủ trọn hành trình
;WITH ChuyenXet AS (
    SELECT * FROM #Chuyen
    WHERE ConHieuLuc = 1 AND LoaiTau <> 'TAU_HANG' AND SoGa IS NOT NULL
      AND (TrangThai IN ('DANG_CHAY', 'SAN_SANG', 'TRE_GIO') OR CAST(GioXP AS DATE) <= @NgayNay)
), KipSapXep AS (
    SELECT k.MaChuyenTau, k.ViTriNhan, k.ViTriGiao,
           LAG(k.ViTriGiao) OVER (PARTITION BY k.MaChuyenTau ORDER BY k.ViTriNhan) AS GiaoTruoc
    FROM #Kip k
), Phu AS (
    SELECT c.MaChuyenTau, c.Mac, c.SoGa,
           (SELECT MIN(ViTriNhan) FROM KipSapXep x WHERE x.MaChuyenTau = c.MaChuyenTau) AS NhanDau,
           (SELECT MAX(ViTriGiao) FROM KipSapXep x WHERE x.MaChuyenTau = c.MaChuyenTau) AS GiaoCuoi,
           (SELECT COUNT(*) FROM KipSapXep x WHERE x.MaChuyenTau = c.MaChuyenTau
                AND x.GiaoTruoc IS NOT NULL AND x.GiaoTruoc <> x.ViTriNhan) AS SoCho
    FROM ChuyenXet c
)
INSERT INTO @KetQua
SELECT 'LOI', 'NS-09', N'R11 · chuyến đang chạy / chạy hôm nay có kíp nối liền, phủ trọn hành trình',
       CONCAT(p.Mac, N': kíp phủ ga ', ISNULL(p.NhanDau, 0), N'..', ISNULL(p.GiaoCuoi, 0), N' / ', p.SoGa, N' ga, ', p.SoCho, N' chỗ hở / chồng')
FROM Phu p
WHERE p.NhanDau IS NULL OR p.NhanDau <> 1 OR p.GiaoCuoi <> p.SoGa OR p.SoCho > 0;

-- NS-10 (R12) Trạng thái kíp không CHẬM hơn tiến độ của tàu (ga cuối cùng đã có giờ đi thực tế):
--       tàu đã rời ga nhận ban mà kíp chưa nhận ban, tàu đã rời ga bàn giao mà kíp chưa bàn giao.
--       11/10: không bắt kíp "đi trước" - kíp nhận ban / bàn giao khi tàu còn đỗ ở ga, trước khi điều độ ghi giờ đi thực tế.
INSERT INTO @KetQua
SELECT 'LOI', 'NS-10', N'R12 · trạng thái kíp không chậm hơn tiến độ chuyến tàu',
       CONCAT(k.Mac, N' ', k.GaNhan, N'→', k.GaGiao, N': ', k.TrangThai, N', đúng ra ', x.DungRa,
              N' (tàu đã rời ga thứ ', k.DaQuaGa, N')')
FROM #Kip k
CROSS APPLY (SELECT CASE
        WHEN k.TrangThaiChuyen = 'HOAN_THANH' OR k.DaQuaGa >= k.ViTriGiao THEN 'HOAN_THANH'
        WHEN k.DaQuaGa >= k.ViTriNhan THEN 'DANG_THUC_HIEN'
        ELSE 'DA_PHAN_CONG' END AS DungRa) x
WHERE (CASE k.TrangThai WHEN 'DA_PHAN_CONG' THEN 0 WHEN 'DANG_THUC_HIEN' THEN 1 ELSE 2 END)
    < (CASE x.DungRa    WHEN 'DA_PHAN_CONG' THEN 0 WHEN 'DANG_THUC_HIEN' THEN 1 ELSE 2 END);

-- NS-11 (R13) Trạng thái nhân viên: đang ở kíp DANG_THUC_HIEN ⇔ DANG_LAM
INSERT INTO @KetQua
SELECT 'LOI', 'NS-11', N'R13 · đang ở kíp DANG_THUC_HIEN ⇔ trạng thái DANG_LAM', CONCAT(nv.MaNVCode, N': ', nv.TrangThai)
FROM nhansu.NhanVien nv
CROSS APPLY (SELECT CASE WHEN EXISTS (SELECT 1 FROM #NguoiKip n
                                      WHERE n.MaNhanVien = nv.MaNhanVien AND n.TrangThai = 'DANG_THUC_HIEN')
                         THEN 1 ELSE 0 END AS DangCa) x
WHERE x.DangCa <> CASE WHEN nv.TrangThai = 'DANG_LAM' THEN 1 ELSE 0 END;

-- NS-12 Phiếu kiểm tra lên ban: người thuộc tổ tàu (lái tàu / phụ lái / trưởng tàu); phiếu ĐẠT phải của người trong kíp
--       chuyến đó và đo trước giờ bàn giao của họ (phiếu không đạt của người đã bị thay thì được giữ)
INSERT INTO @KetQua
SELECT 'LOI', 'NS-12', N'Phiếu kiểm tra: đúng người của tổ tàu, phiếu đạt thuộc người trong kíp và đo trước giờ bàn giao',
       CONCAT(nv.MaNVCode, N' · chuyến ', c.Mac, N' · ', FORMAT(ks.ThoiDiemKiemTra, 'HH:mm dd/MM'),
              CASE WHEN ks.DuDieuKien = 1 THEN N' (đạt)' ELSE N' (không đạt)' END)
FROM nhansu.KiemTraSucKhoe ks
JOIN nhansu.NhanVien nv ON nv.MaNhanVien = ks.MaNhanVien
JOIN #Chuyen c ON c.MaChuyenTau = ks.MaChuyenTau
WHERE nv.ChucDanh NOT IN (N'Lái tàu', N'Phụ lái', N'Trưởng tàu')
   OR (ks.DuDieuKien = 1 AND NOT EXISTS (
          SELECT 1 FROM #NguoiKip n
          WHERE n.MaNhanVien = ks.MaNhanVien AND n.MaChuyenTau = ks.MaChuyenTau AND ks.ThoiDiemKiemTra <= n.GioGiao));

-- NS-13 Mã nhân viên theo chức danh: TIỀNTỐ_SSS, đúng tiền tố, không trùng (UNIQUE đã chặn trùng)
INSERT INTO @KetQua
SELECT 'LOI', 'NS-13', N'Mã nhân viên dạng TIỀNTỐ_SSS, tiền tố đúng chức danh', CONCAT(nv.MaNVCode, N' (', nv.ChucDanh, N')')
FROM nhansu.NhanVien nv
WHERE nv.MaNVCode NOT LIKE '[A-Z][A-Z][_][0-9][0-9][0-9]%'
   OR LEFT(nv.MaNVCode, 2) <> CASE nv.ChucDanh
        WHEN N'Lái tàu' THEN 'LT' WHEN N'Phụ lái' THEN 'PL' WHEN N'Trưởng tàu' THEN 'TT'
        WHEN N'Tiếp viên' THEN 'TV' WHEN N'Nhân viên khám xe' THEN 'KX' WHEN N'Nhân viên bán vé' THEN 'BV'
        ELSE 'NV' END;

/* =========================================================================
   GHI CHÚ DỮ LIỆU CỦA PHÂN HỆ KHÁC (A Bá) — không tính là lỗi của Đạt
   ========================================================================= */
-- AB-01 Chuyến đang chạy / sắp chạy chưa có lịch dừng hoặc đoàn tàu (vd tàu hàng HBN1) → không lập tàu, không phân kíp được
INSERT INTO @KetQua
SELECT 'GHI_CHU', 'AB-01', N'Chuyến còn hiệu lực chưa có lịch dừng ga / đoàn tàu / kíp lái',
       CONCAT(c.Mac, N' (', c.TrangThai, N'): ',
              CASE WHEN c.SoGa IS NULL THEN N'không có lịch dừng; ' ELSE N'' END,
              CASE WHEN c.MaDoanTau IS NULL THEN N'chưa lập đoàn tàu; ' ELSE N'' END,
              CASE WHEN NOT EXISTS (SELECT 1 FROM #Kip k WHERE k.MaChuyenTau = c.MaChuyenTau) THEN N'chưa có kíp' ELSE N'' END)
FROM #Chuyen c
WHERE c.ConHieuLuc = 1 AND (c.SoGa IS NULL OR c.MaDoanTau IS NULL);

-- AB-02 Giờ ở ga đầu / ga cuối của lịch dừng lệch giờ xuất phát / về đích của chuyến
INSERT INTO @KetQua
SELECT 'GHI_CHU', 'AB-02', N'Lịch dừng ga đầu / ga cuối lệch giờ kế hoạch của chuyến',
       CONCAT(c.Mac, N': chuyến ', FORMAT(c.GioXP, 'HH:mm dd/MM'), N' → ', FORMAT(c.GioVD, 'HH:mm dd/MM'),
              N'; lịch dừng ', FORMAT(lD.GioDiKeHoach, 'HH:mm dd/MM'), N' → ', FORMAT(lC.GioDenKeHoach, 'HH:mm dd/MM'))
FROM #Chuyen c
JOIN vanhanh.LichDungGa lD ON lD.MaChuyenTau = c.MaChuyenTau AND lD.ThuTuDung = 1
JOIN vanhanh.LichDungGa lC ON lC.MaChuyenTau = c.MaChuyenTau AND lC.ThuTuDung = c.SoGa
WHERE lD.GioDiKeHoach <> c.GioXP OR lC.GioDenKeHoach <> c.GioVD;

-- AB-03 Ngày xuất phát khác ngày của giờ xuất phát kế hoạch
INSERT INTO @KetQua
SELECT 'GHI_CHU', 'AB-03', N'NgayXuatPhat khác ngày của GioXuatPhatKH',
       CONCAT(c.Mac, N': ngày ', FORMAT(c.NgayXuatPhat, 'dd/MM'), N', giờ xuất phát ', FORMAT(c.GioXP, 'HH:mm dd/MM'))
FROM #Chuyen c WHERE c.NgayXuatPhat <> CAST(c.GioXP AS DATE);

-- AB-04 Ga đổi kíp lái cách nhau quá xa (lịch dừng thiếu ga trung gian): ca lái tàu > 12 giờ
INSERT INTO @KetQua
SELECT 'GHI_CHU', 'AB-04', N'Ca lái tàu dài > 12 giờ vì lịch dừng thiếu ga đổi kíp ở giữa',
       CONCAT(k.Mac, N' ', k.GaNhan, N'→', k.GaGiao, N': ', CAST(DATEDIFF(MINUTE, k.GioNhan, k.GioGiao) / 60.0 AS DECIMAL(5,1)), N' giờ')
FROM #Kip k WHERE DATEDIFF(MINUTE, k.GioNhan, k.GioGiao) > 12 * 60;

/* =========================================================================
   KẾT QUẢ
   ========================================================================= */
PRINT N'=== TỔNG HỢP ===';
SELECT x.Luat, x.MoTa,
       ISNULL(r.SoDong, 0) AS SoViPham,
       CASE WHEN ISNULL(r.SoDong, 0) = 0 THEN N'ĐẠT' WHEN x.Luat LIKE 'AB-%' THEN N'GHI CHÚ' ELSE N'LỖI' END AS KetLuan
FROM (VALUES
    ('PT-01', N'Số toa mỗi chặng 1..20, khớp TongSoToa'),
    ('PT-02', N'Chiều dài <= đường tránh ngắn nhất, khớp TongChieuDaiM'),
    ('PT-03', N'Trọng lượng <= sức kéo, khớp TongTrongLuongTan'),
    ('PT-04', N'Tải trọng trục <= 14 t'),
    ('PT-05', N'Toa hàng liền khối ở đầu / cuối đoàn'),
    ('PT-06', N'Ga nối / cắt hợp lệ, đỗ >= 15 phút'),
    ('PT-07', N'Nhóm toa nối / cắt liền khối ở đầu / cuối'),
    ('PT-08', N'Đầu máy không kéo 2 chuyến chồng giờ'),
    ('PT-09', N'Toa không thuộc 2 chuyến chồng giờ'),
    ('PT-10', N'Không dùng đầu máy / toa đang bảo dưỡng / ngừng khai thác'),
    ('PT-11', N'Trạng thái toa / đầu máy khớp chuyến đang chạy'),
    ('NS-01', N'R2 đúng chức danh'),
    ('NS-02', N'R3 không ở 2 chuyến chồng giờ'),
    ('NS-03', N'R3 cùng chuyến'),
    ('NS-04', N'R4 nghỉ >= 8 giờ'),
    ('NS-05', N'R6 kiểm tra lên ban'),
    ('NS-06', N'R7 bằng lái = dòng đầu máy'),
    ('NS-07', N'R8 hạn khám'),
    ('NS-08', N'R9 không phân công người nghỉ việc'),
    ('NS-09', N'R11 kíp phủ trọn hành trình'),
    ('NS-10', N'R12 trạng thái kíp theo tiến độ'),
    ('NS-11', N'R13 trạng thái nhân viên'),
    ('NS-12', N'Phiếu kiểm tra hợp lệ'),
    ('NS-13', N'Mã nhân viên theo chức danh'),
    ('AB-01', N'[A Bá] chuyến thiếu lịch dừng / đoàn tàu'),
    ('AB-02', N'[A Bá] lịch dừng lệch giờ chuyến'),
    ('AB-03', N'[A Bá] ngày xuất phát lệch giờ xuất phát'),
    ('AB-04', N'[A Bá] ca lái > 12 giờ do thiếu ga đổi kíp')
) x(Luat, MoTa)
LEFT JOIN (SELECT Luat, COUNT(*) AS SoDong FROM @KetQua GROUP BY Luat) r ON r.Luat = x.Luat
ORDER BY x.Luat;

PRINT N'=== CHI TIẾT ===';
SELECT Muc, Luat, ChiTiet FROM @KetQua ORDER BY Muc DESC, Luat, ChiTiet;

DECLARE @SoLoi INT = (SELECT COUNT(*) FROM @KetQua WHERE Muc = 'LOI');
DECLARE @SoGhiChu INT = (SELECT COUNT(*) FROM @KetQua WHERE Muc = 'GHI_CHU');
IF @SoLoi > 0
    RAISERROR(N'KIỂM TRA KHÔNG ĐẠT: %d vi phạm luật nghiệp vụ (xem bảng chi tiết).', 16, 1, @SoLoi);
ELSE
    PRINT CONCAT(N'KIỂM TRA ĐẠT: 0 vi phạm. ', @SoGhiChu, N' ghi chú về dữ liệu phân hệ khác.');
