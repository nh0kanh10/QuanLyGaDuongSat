/* =========================================================================
   SEED SPRINT 1 — DỮ LIỆU MẪU PHƯƠNG TIỆN + NHÂN SỰ (phần của Đạt)
   Tác giả  : Đạt
   Ngày     : 09/10/2026

   Thứ tự chạy (trên CSDL vừa tạo từ script gốc, chưa sửa tay):
     1. script_tao_csdl_v2.sql                                  (A Bá, tạo CSDL + dữ liệu gốc)
     2. migrations\V20261009_0820__dat__chitietdoantau-ga-noi-cat.sql
     3. file này
     4. tests\T20261009_0840__dat__kiem-tra-du-lieu-sprint1.sql  (kiểm lại các luật nghiệp vụ)
   Lệnh: sqlcmd -S . -E -I -f 65001 -d QuanLyDuongSatV2 -i <file>
   (-I = QUOTED_IDENTIFIER ON như ADO.NET; thiếu -I thì câu ghi vào bảng có cột tính toán báo lỗi 1934.)

   Chạy lại: thấy đã có mã LT_001 thì bỏ qua, không ghi gì. Muốn làm lại thì tạo lại CSDL từ bước 1.
   Mọi thứ nằm trong MỘT giao dịch: lỗi ở đâu thì hoàn tác toàn bộ.

   Mốc thời gian: lấy theo ngày của chuyến SE19 trong dữ liệu gốc (= @NgayNay của script gốc), KHÔNG lấy
   giờ máy, để khớp với các chuyến tàu A Bá đã tạo dù chạy file này vào ngày khác.

   ─────────────────────────────────────────────────────────────────────────
   NỘI DUNG
   A. (ĐỤNG BẢNG CỦA A BÁ — đã thống nhất 09/10, báo A Bá khi gộp)
      SE3 Hà Nội → Sài Gòn qua Đà Nẵng: 13 ga dừng, 32,5 giờ (khớp mác tàu), đỗ Đà Nẵng 25 phút để nối toa.
      Sửa vanhanh.ChuyenTau.GioVeDichKH của SE3 (16 giờ → 32,5 giờ) + thêm vanhanh.LichDungGa.
   B. PHƯƠNG TIỆN
      - 4 đầu máy mới; gỡ 2 chỗ một đầu máy kéo 2 tàu chồng giờ (D19E-901: SE1 + SE19; D19E-902: SE2 + SE4).
      - 72 toa mới: 57 toa cho đoàn tàu, 13 toa bãi (trùng danh sách bãi trên giao diện), 2 toa đang bảo dưỡng.
      - Toa thật cho 6 đoàn tàu (62 toa khác nhau). SE3 dùng lại 10 toa của SE2 (SE2 về Hà Nội đêm nay, SE3 chạy
        19:00 tối mai) và nối thêm 3 toa tại Đà Nẵng chạy tiếp vào Sài Gòn.
      - Tổng số toa / chiều dài / trọng lượng của đoàn tàu tính lại từ toa thật (lớn nhất trên các chặng).
      - Trạng thái toa / đầu máy theo đúng việc đang làm.
   C. NHÂN SỰ
      - Đổi mã NV_001..NV_013 sang mã theo chức danh (LT_ / PL_ / TT_ / KX_ / BV_).
      - 11 nhân viên bổ sung như giao diện (MaNhanVien 14..24) + 17 người mới (lái tàu, phụ lái các depot, 1 trưởng tàu).
      - 14 kíp: các chuyến đang chạy / chạy hôm nay phủ kín hành trình, trạng thái theo tiến độ thật của tàu
        (chặng đã qua = HOAN_THANH, đang chạy = DANG_THUC_HIEN, chưa tới = DA_PHAN_CONG). SE3 (ngày mai) chỉ có kíp
        Hà Nội → Vinh; 3 chặng sau để trống cho người dùng phân kíp (demo Gợi Ý Kíp).
      - Kiểm tra lên ban: kíp đã nhận ban có đủ kết quả đạt của cả 3 người; SE19 (sắp xuất bến) đã kiểm tra.
      - Trạng thái nhân viên theo kíp (R13).

   QUY ƯỚC NGHIỆP VỤ ĐÃ ÁP (nguồn: luật trên giao diện của Đạt, PHAN_TICH_NHAN_SU_KIP_LAI.md, CLAUDE.md §5.6)
   - Lái tàu / phụ lái đổi kíp tại ga có depot; trưởng tàu đi suốt chuyến (được ở nhiều kíp của cùng chuyến).
   - Một người không ở 2 chuyến chồng khung giờ (trigger chỉ chặn lái tàu / phụ lái, ở đây áp cả trưởng tàu).
   - Nghỉ >= 8 giờ giữa 2 ca khác chuyến. Bằng lái = dòng đầu máy chính. Hạn khám còn hiệu lực.
   - Đoàn tàu <= 20 toa, dài (kể cả đầu máy chính) <= đường tránh ngắn nhất trên hành trình (400 m),
     trọng lượng toàn tải <= sức kéo đầu máy, tải trọng trục <= 14 t, toa hàng liền khối ở cuối đoàn.
   - Toa nối dọc đường xếp liền khối ở cuối đoàn, ga nối đỗ >= 15 phút.
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
IF COL_LENGTH(N'vanhanh.ChiTietDoanTau', N'MaGaNoi') IS NULL
BEGIN
    RAISERROR(N'Chưa chạy migration V20261009_0820 (ChiTietDoanTau.MaGaNoi / MaGaCat).', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM nhansu.NhanVien WHERE MaNVCode = 'LT_001')
BEGIN
    PRINT N'Seed S20261009_0830 đã chạy trên CSDL này (đã có LT_001) -> bỏ qua, không ghi gì.';
    RETURN;
END;

-- Mốc ngày theo dữ liệu gốc
DECLARE @NgayNay DATE = (
    SELECT TOP (1) ct.NgayXuatPhat
    FROM vanhanh.ChuyenTau ct
    JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau
    WHERE m.SoHieuMacTau = 'SE19'
    ORDER BY ct.NgayXuatPhat DESC);
DECLARE @HomQua  DATE = DATEADD(DAY, -1, @NgayNay);
DECLARE @NgayMai DATE = DATEADD(DAY,  1, @NgayNay);
DECLARE @T0 DATETIME2 = CAST(@NgayNay AS DATETIME2);   -- 00:00 hôm nay
DECLARE @T1 DATETIME2 = CAST(@NgayMai AS DATETIME2);   -- 00:00 ngày mai

-- Các chuyến tàu của dữ liệu gốc
DECLARE @Chuyen TABLE (Mac VARCHAR(10) PRIMARY KEY, MaChuyenTau INT NULL, MaDoanTau INT NULL);
INSERT INTO @Chuyen (Mac, MaChuyenTau)
SELECT v.Mac,
       (SELECT ct.MaChuyenTau
        FROM vanhanh.ChuyenTau ct
        JOIN vanhanh.MacTauMau m ON m.MaMacTau = ct.MaMacTau
        WHERE m.SoHieuMacTau = v.SoHieu AND ct.NgayXuatPhat = v.Ngay)
FROM (VALUES ('SE1', 'SE1', @NgayNay),
             ('SE2', 'SE2', @HomQua),
             ('SE3', 'SE3', @NgayMai),
             ('SE4', 'SE4', @NgayNay),   -- dữ liệu gốc ghi ngày xuất phát hôm nay, giờ chạy 19:00 hôm qua
             ('SE19', 'SE19', @NgayNay),
             ('NA1', 'NA1', @NgayNay)) v(Mac, SoHieu, Ngay);

UPDATE c SET MaDoanTau = dt.MaDoanTau
FROM @Chuyen c JOIN vanhanh.DoanTau dt ON dt.MaChuyenTau = c.MaChuyenTau;

DECLARE @CtSE1 INT, @CtSE2 INT, @CtSE3 INT, @CtSE4 INT, @CtSE19 INT, @CtNA1 INT;
SELECT @CtSE1 = MaChuyenTau FROM @Chuyen WHERE Mac = 'SE1';
SELECT @CtSE2 = MaChuyenTau FROM @Chuyen WHERE Mac = 'SE2';
SELECT @CtSE3 = MaChuyenTau FROM @Chuyen WHERE Mac = 'SE3';
SELECT @CtSE4 = MaChuyenTau FROM @Chuyen WHERE Mac = 'SE4';
SELECT @CtSE19 = MaChuyenTau FROM @Chuyen WHERE Mac = 'SE19';
SELECT @CtNA1 = MaChuyenTau FROM @Chuyen WHERE Mac = 'NA1';

-- CSDL phải còn ở trạng thái gốc, nếu không thì dữ liệu dưới đây có thể sai nghiệp vụ
DECLARE @LoiGoc NVARCHAR(400) = NULL;
IF @NgayNay IS NULL OR EXISTS (SELECT 1 FROM @Chuyen WHERE MaChuyenTau IS NULL OR MaDoanTau IS NULL)
    SET @LoiGoc = N'không tìm thấy đủ 6 chuyến SE1 / SE2 / SE3 / SE4 / SE19 / NA1 kèm đoàn tàu của dữ liệu gốc';
ELSE IF (SELECT COUNT(*) FROM nhansu.NhanVien) <> 13
     OR (SELECT COUNT(*) FROM nhansu.NhanVien WHERE MaNVCode LIKE 'NV[_]0%') <> 13
    SET @LoiGoc = N'bảng nhansu.NhanVien không còn đúng 13 nhân viên NV_001..NV_013';
ELSE IF (SELECT COUNT(*) FROM phuongtien.DauMay) <> 4 OR (SELECT COUNT(*) FROM phuongtien.ToaXe) <> 6
    SET @LoiGoc = N'đội đầu máy / toa xe không còn là 4 đầu máy + 6 toa của dữ liệu gốc';
ELSE IF EXISTS (SELECT 1 FROM vanhanh.ChiTietDoanTau)
    SET @LoiGoc = N'vanhanh.ChiTietDoanTau đã có dữ liệu';
ELSE IF (SELECT COUNT(*) FROM nhansu.PhanCongKipLai) <> 4
    SET @LoiGoc = N'nhansu.PhanCongKipLai không còn đúng 4 kíp của dữ liệu gốc';
ELSE IF (SELECT GioXuatPhatKH FROM vanhanh.ChuyenTau WHERE MaChuyenTau = @CtSE3) <> DATEADD(MINUTE, 1140, @T1)
    SET @LoiGoc = N'giờ xuất phát SE3 không còn là 19:00 ngày mai';

IF @LoiGoc IS NOT NULL
BEGIN
    RAISERROR(N'CSDL không còn ở trạng thái gốc: %s. Tạo lại CSDL từ script gốc, chạy migration rồi chạy lại file này.', 16, 1, @LoiGoc);
    RETURN;
END;

BEGIN TRY
BEGIN TRANSACTION;

/* =========================================================================
   A. SE3 HÀ NỘI → SÀI GÒN QUA ĐÀ NẴNG   (bảng của A Bá: vanhanh.LichDungGa, vanhanh.ChuyenTau)
   Mác SE3: HN → SG, 32,5 giờ. Xuất phát 19:00 ngày mai (giữ giờ A Bá đã đặt) → đến Sài Gòn 03:30.
   Phút tính từ 00:00 ngày mai (> 1440 = ngày kia, > 2880 = 2 ngày sau).
   Hà Nội → Đà Nẵng giữ đúng giờ đã dùng trên giao diện (theo mẫu chạy SE19).
   ========================================================================= */
IF NOT EXISTS (SELECT 1 FROM vanhanh.LichDungGa WHERE MaChuyenTau = @CtSE3)
BEGIN
    INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach)
    SELECT @CtSE3, g.MaGa, v.ThuTu, DATEADD(MINUTE, v.Den, @T1), DATEADD(MINUTE, v.Di, @T1)
    FROM (VALUES
        ( 1, 'HNI', 1120, 1140),   -- 18:40 vào ga · 19:00 xuất phát
        ( 2, 'PLY', 1195, 1198),
        ( 3, 'NDH', 1228, 1233),
        ( 4, 'THA', 1315, 1320),
        ( 5, 'VIN', 1480, 1490),   -- 00:40 · đổi kíp lái
        ( 6, 'DHO', 1730, 1738),
        ( 7, 'HUE', 1920, 1930),
        ( 8, 'DAN', 2100, 2125),   -- 11:00 · đỗ 25 phút: đổi kíp + nối 3 toa (>= 15 phút)
        ( 9, 'QNG', 2260, 2265),
        (10, 'DTR', 2425, 2435),
        (11, 'NTR', 2640, 2655),   -- 20:00 · đổi kíp lái
        (12, 'BTH', 2875, 2880),
        (13, 'SGO', 3090, 3090)    -- 03:30 · ga cuối
    ) v(ThuTu, Ga, Den, Di)
    JOIN hatang.Ga g ON g.MaGaCode = v.Ga;

    UPDATE vanhanh.ChuyenTau
    SET GioVeDichKH = DATEADD(MINUTE, 3090, @T1)        -- 19:00 + 32 giờ 30 = 03:30
    WHERE MaChuyenTau = @CtSE3;
END
ELSE IF NOT EXISTS (
    SELECT 1 FROM vanhanh.LichDungGa ld JOIN hatang.Ga g ON g.MaGa = ld.MaGa
    WHERE ld.MaChuyenTau = @CtSE3 AND g.MaGaCode = 'DAN'
      AND DATEDIFF(MINUTE, ld.GioDenKeHoach, ld.GioDiKeHoach) >= 15)
    RAISERROR(N'SE3 đã có lịch dừng nhưng không đỗ Đà Nẵng >= 15 phút, không nối toa được.', 16, 1);

/* =========================================================================
   B. PHƯƠNG TIỆN
   ========================================================================= */

-- B1. Đầu máy mới (D19E-901..903 và D13E-701 giữ nguyên)
INSERT INTO phuongtien.DauMay (SoHieuDauMay, MaDongDauMay, DonViQuanLy, NamSanXuat, SoKmTichLuy, TrangThai)
SELECT v.SoHieu, d.MaDongDauMay, v.DonVi, v.Nam, v.Km, 'SAN_SANG'
FROM (VALUES
    ('D19E-904', 'D19E', N'Xí nghiệp Đầu máy Hà Nội', 2019, 41200.0),   -- kéo SE3 (ngày mai)
    ('D19E-905', 'D19E', N'Xí nghiệp Đầu máy Sài Gòn', 2018, 47600.0),  -- kéo SE4
    ('D19E-906', 'D19E', N'Xí nghiệp Đầu máy Vinh',    2020, 23900.0),  -- kéo SE19
    ('D13E-702', 'D13E', N'Xí nghiệp Đầu máy Vinh',    2011, 44800.0)   -- dự phòng
) v(SoHieu, Dong, DonVi, Nam, Km)
JOIN phuongtien.DongDauMay d ON d.MaDongCode = v.Dong;

-- B2. Toa mới. Thông số theo chủng loại (tự trọng / tải trọng tối đa như toa gốc cùng loại; G theo giao diện).
--     NC-102..103, NML-202..204, BN-302..304, AN-402..403, G-501..502, M-602 : bãi (13 toa như giao diện)
--     NC-104..113, NML-205..221, BN-305..318, AN-404..411, G-503..507, M-603..605 : trong đoàn tàu
--     NC-114, AN-412 : đang bảo dưỡng
INSERT INTO phuongtien.ToaXe (SoHieuToaXe, MaChungLoai, TuTrongTan, TaiTrongToiDaTan, TrangThai)
SELECT r.Loai + '-' + CAST(r.TuSo + n.i AS VARCHAR(5)), cl.MaChungLoai, r.TuTrong, r.TaiTrong, 'SAN_SANG'
FROM (VALUES
    ('NC',  102, 114, 35.5, 12.0),
    ('NML', 202, 221, 36.0, 10.0),
    ('BN',  302, 318, 38.0,  8.0),
    ('AN',  402, 412, 39.0,  6.0),
    ('G',   501, 507, 24.0, 30.0),
    ('M',   602, 605, 25.0, 20.0)
) r(Loai, TuSo, DenSo, TuTrong, TaiTrong)
JOIN (SELECT TOP (30) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS i FROM sys.all_objects) n
  ON r.TuSo + n.i <= r.DenSo
JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoaiCode = r.Loai;

IF (SELECT COUNT(*) FROM phuongtien.ToaXe) <> 78
    RAISERROR(N'Số toa sau khi thêm phải là 78 (6 gốc + 72 mới).', 16, 1);

-- B3. Gán lại đầu máy để không đầu máy nào kéo 2 chuyến chồng giờ
--     SE1  D19E-901 (+ D19E-903 đẩy đèo Hải Vân)   SE2 D19E-902   NA1 D13E-701   (giữ như gốc)
--     SE4  D19E-902 → D19E-905   SE19 D19E-901 → D19E-906   SE3 D19E-901 → D19E-904
UPDATE dt SET MaDauMayChinh = dm.MaDauMay
FROM vanhanh.DoanTau dt
JOIN @Chuyen c ON c.MaDoanTau = dt.MaDoanTau
JOIN (VALUES ('SE4', 'D19E-905'), ('SE19', 'D19E-906'), ('SE3', 'D19E-904')) v(Mac, SoHieu) ON v.Mac = c.Mac
JOIN phuongtien.DauMay dm ON dm.SoHieuDauMay = v.SoHieu;

-- B4. Toa trong từng đoàn tàu. ThuTuNoi 1 = ngay sau đầu máy.
--     Toa khách đầu đoàn khớp loại toa bán vé của A Bá (vantai.ToaXeKhach ThuTuToa 1..3);
--     toa hàng (M xe máy, G hàng kín) liền khối ở cuối đoàn.
INSERT INTO vanhanh.ChiTietDoanTau (MaDoanTau, MaToaXe, ThuTuNoi, MaGaNoi, MaGaCat)
SELECT c.MaDoanTau, tx.MaToaXe, v.ThuTu, gNoi.MaGa, NULL
FROM (VALUES
    -- SE1 Hà Nội → Sài Gòn: 14 toa (bán vé: 1 AN · 2 NML · 3 BN)
    ('SE1', 1, 'AN-401', NULL), ('SE1', 2, 'NML-201', NULL), ('SE1', 3, 'BN-301', NULL),
    ('SE1', 4, 'AN-404', NULL), ('SE1', 5, 'AN-405', NULL), ('SE1', 6, 'BN-305', NULL),
    ('SE1', 7, 'BN-306', NULL), ('SE1', 8, 'NML-205', NULL), ('SE1', 9, 'NML-206', NULL),
    ('SE1', 10, 'NML-207', NULL), ('SE1', 11, 'NC-101', NULL), ('SE1', 12, 'NC-104', NULL),
    ('SE1', 13, 'M-601', NULL), ('SE1', 14, 'G-503', NULL),

    -- SE2 Sài Gòn → Hà Nội: 12 toa (về Hà Nội đêm nay; 10 toa khách quay vòng chạy SE3)
    ('SE2', 1, 'NML-208', NULL), ('SE2', 2, 'BN-307', NULL), ('SE2', 3, 'AN-406', NULL),
    ('SE2', 4, 'AN-407', NULL), ('SE2', 5, 'BN-308', NULL), ('SE2', 6, 'BN-309', NULL),
    ('SE2', 7, 'NML-209', NULL), ('SE2', 8, 'NML-210', NULL), ('SE2', 9, 'NC-105', NULL),
    ('SE2', 10, 'NC-106', NULL), ('SE2', 11, 'M-603', NULL), ('SE2', 12, 'G-504', NULL),

    -- SE3 Hà Nội → Sài Gòn (ngày mai): 10 toa của SE2 (bán vé: 1 NML · 2 BN · 3 AN)
    --     + 3 toa nối tại Đà Nẵng, liền khối ở cuối đoàn, chạy Đà Nẵng → Sài Gòn
    ('SE3', 1, 'NML-208', NULL), ('SE3', 2, 'BN-307', NULL), ('SE3', 3, 'AN-406', NULL),
    ('SE3', 4, 'AN-407', NULL), ('SE3', 5, 'BN-308', NULL), ('SE3', 6, 'BN-309', NULL),
    ('SE3', 7, 'NML-209', NULL), ('SE3', 8, 'NML-210', NULL), ('SE3', 9, 'NC-105', NULL),
    ('SE3', 10, 'NC-106', NULL),
    ('SE3', 11, 'NML-211', 'DAN'), ('SE3', 12, 'NML-212', 'DAN'), ('SE3', 13, 'BN-310', 'DAN'),

    -- SE4 Sài Gòn → Hà Nội: 13 toa (bán vé: 1 AN · 2 NML)
    ('SE4', 1, 'AN-408', NULL), ('SE4', 2, 'NML-213', NULL), ('SE4', 3, 'AN-409', NULL),
    ('SE4', 4, 'BN-311', NULL), ('SE4', 5, 'BN-312', NULL), ('SE4', 6, 'BN-313', NULL),
    ('SE4', 7, 'NML-214', NULL), ('SE4', 8, 'NML-215', NULL), ('SE4', 9, 'NML-216', NULL),
    ('SE4', 10, 'NC-107', NULL), ('SE4', 11, 'NC-108', NULL), ('SE4', 12, 'M-604', NULL),
    ('SE4', 13, 'G-505', NULL),

    -- SE19 Hà Nội → Đà Nẵng: 11 toa (bán vé: 1 AN · 2 BN)
    ('SE19', 1, 'AN-410', NULL), ('SE19', 2, 'BN-314', NULL), ('SE19', 3, 'AN-411', NULL),
    ('SE19', 4, 'BN-315', NULL), ('SE19', 5, 'BN-316', NULL), ('SE19', 6, 'NML-217', NULL),
    ('SE19', 7, 'NML-218', NULL), ('SE19', 8, 'NML-219', NULL), ('SE19', 9, 'NC-109', NULL),
    ('SE19', 10, 'M-605', NULL), ('SE19', 11, 'G-506', NULL),

    -- NA1 Hà Nội → Vinh (D13E): 9 toa (bán vé: 1 NML)
    ('NA1', 1, 'NML-220', NULL), ('NA1', 2, 'NML-221', NULL), ('NA1', 3, 'NC-110', NULL),
    ('NA1', 4, 'NC-111', NULL), ('NA1', 5, 'NC-112', NULL), ('NA1', 6, 'NC-113', NULL),
    ('NA1', 7, 'BN-317', NULL), ('NA1', 8, 'BN-318', NULL), ('NA1', 9, 'G-507', NULL)
) v(Mac, ThuTu, SoHieu, GaNoi)
JOIN @Chuyen c ON c.Mac = v.Mac
JOIN phuongtien.ToaXe tx ON tx.SoHieuToaXe = v.SoHieu
LEFT JOIN hatang.Ga gNoi ON gNoi.MaGaCode = v.GaNoi;

IF (SELECT COUNT(*) FROM vanhanh.ChiTietDoanTau) <> 72
    RAISERROR(N'Phải có 72 dòng ChiTietDoanTau (14 + 12 + 13 + 13 + 11 + 9).', 16, 1);

-- B5. Tổng số toa / chiều dài / trọng lượng của đoàn tàu = lớn nhất trên các chặng (khu đoạn ga k → k+1).
--     Chiều dài kể cả đầu máy chính; trọng lượng = toa toàn tải (tự trọng + tải trọng tối đa), như màn hình lập tàu.
;WITH Chang AS (
    SELECT c.MaDoanTau, ld.ThuTuDung AS K
    FROM @Chuyen c
    JOIN vanhanh.LichDungGa ld ON ld.MaChuyenTau = c.MaChuyenTau
    WHERE ld.ThuTuDung < (SELECT MAX(x.ThuTuDung) FROM vanhanh.LichDungGa x WHERE x.MaChuyenTau = c.MaChuyenTau)
),
SoLieuChang AS (
    SELECT ch.MaDoanTau, ch.K,
           COUNT(*)                                   AS SoToa,
           SUM(cl.ChieuDaiChuanM)                     AS ChieuDaiToa,
           SUM(tx.TuTrongTan + tx.TaiTrongToiDaTan)   AS TrongLuong
    FROM Chang ch
    JOIN vanhanh.DoanTau dt        ON dt.MaDoanTau = ch.MaDoanTau
    JOIN vanhanh.ChiTietDoanTau ct ON ct.MaDoanTau = ch.MaDoanTau
    JOIN phuongtien.ToaXe tx       ON tx.MaToaXe = ct.MaToaXe
    JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
    LEFT JOIN vanhanh.LichDungGa lNoi ON lNoi.MaChuyenTau = dt.MaChuyenTau AND lNoi.MaGa = ct.MaGaNoi
    LEFT JOIN vanhanh.LichDungGa lCat ON lCat.MaChuyenTau = dt.MaChuyenTau AND lCat.MaGa = ct.MaGaCat
    WHERE ISNULL(lNoi.ThuTuDung, 1) <= ch.K
      AND ch.K < ISNULL(lCat.ThuTuDung, 2147483647)
    GROUP BY ch.MaDoanTau, ch.K
),
LonNhat AS (
    SELECT MaDoanTau, MAX(SoToa) AS SoToa, MAX(ChieuDaiToa) AS ChieuDaiToa, MAX(TrongLuong) AS TrongLuong
    FROM SoLieuChang GROUP BY MaDoanTau
)
UPDATE dt
SET TongSoToa         = ln.SoToa,
    TongChieuDaiM     = ddm.ChieuDaiM + ln.ChieuDaiToa,
    TongTrongLuongTan = ln.TrongLuong
FROM vanhanh.DoanTau dt
JOIN LonNhat ln               ON ln.MaDoanTau = dt.MaDoanTau
JOIN phuongtien.DauMay dm     ON dm.MaDauMay = dt.MaDauMayChinh
JOIN phuongtien.DongDauMay ddm ON ddm.MaDongDauMay = dm.MaDongDauMay;

IF @@ROWCOUNT <> 6
    RAISERROR(N'Phải tính lại được tổng số liệu cho đủ 6 đoàn tàu.', 16, 1);

-- B6. Trạng thái toa: đang móc trong đoàn tàu của chuyến đang chạy / sẵn sàng / chạy tối nay → DA_NOI.
--     3 toa nối SE3 tại Đà Nẵng đang chờ ở bãi Đà Nẵng → SAN_SANG. 2 toa bảo dưỡng → BAO_DUONG.
UPDATE tx SET TrangThai = 'DA_NOI'
FROM phuongtien.ToaXe tx
WHERE EXISTS (SELECT 1 FROM vanhanh.ChiTietDoanTau ct JOIN @Chuyen c ON c.MaDoanTau = ct.MaDoanTau
              WHERE ct.MaToaXe = tx.MaToaXe AND c.Mac IN ('SE1', 'SE2', 'SE4', 'SE19', 'NA1'));

UPDATE phuongtien.ToaXe SET TrangThai = 'BAO_DUONG' WHERE SoHieuToaXe IN ('NC-114', 'AN-412');

-- B7. Trạng thái đầu máy: đang kéo / đẩy chuyến DANG_CHAY → DANG_CHAY (D19E-901, 902, 903, 905).
--     Đầu máy của chuyến chưa chạy (SE19, NA1, SE3) và đầu máy dự phòng → SAN_SANG.
UPDATE dm SET TrangThai = 'DANG_CHAY'
FROM phuongtien.DauMay dm
WHERE EXISTS (SELECT 1 FROM vanhanh.DoanTau dt JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau
              WHERE ct.TrangThai = 'DANG_CHAY' AND dm.MaDauMay IN (dt.MaDauMayChinh, dt.MaDauMayDay));

/* =========================================================================
   C. NHÂN SỰ
   ========================================================================= */

-- C1. Đổi mã theo chức danh (bảng đổi mã: CLAUDE.md §5.8). Kíp / kiểm tra / tài khoản nối bằng MaNhanVien nên không ảnh hưởng.
UPDATE nv SET MaNVCode = v.MaMoi
FROM nhansu.NhanVien nv
JOIN (VALUES ('NV_001', 'LT_001'), ('NV_002', 'PL_001'), ('NV_003', 'TT_001'), ('NV_004', 'KX_001'),
             ('NV_005', 'BV_001'), ('NV_006', 'LT_002'), ('NV_007', 'LT_003'), ('NV_008', 'PL_002'),
             ('NV_009', 'TT_002'), ('NV_010', 'LT_004'), ('NV_011', 'PL_003'), ('NV_012', 'LT_005'),
             ('NV_013', 'PL_004')) v(MaCu, MaMoi) ON v.MaCu = nv.MaNVCode;

IF @@ROWCOUNT <> 13
    RAISERROR(N'Phải đổi mã đủ 13 nhân viên.', 16, 1);

-- C2. Nhân viên thêm mới. 11 người đầu trùng giao diện (MaNhanVien 14..24), 17 người sau là mới.
--     NgayTruoc = số ngày trước hôm nay lúc lập hồ sơ. HanKham NULL = dùng @HanKhamTuongDoi (ngày tính theo hôm nay).
INSERT INTO nhansu.NhanVien (MaNVCode, HoTen, SoDienThoai, ChucDanh, HangBangLai, HanKhamSucKhoe, DonViChuQuan, TrangThai, NgayTao)
SELECT v.Ma, v.HoTen, v.Sdt, v.ChucDanh, v.Bang,
       ISNULL(v.HanKham, DATEADD(DAY, v.HanKhamSauSoNgay, @NgayNay)),
       v.DonVi, v.TrangThai, DATEADD(HOUR, 8, DATEADD(DAY, -v.NgayTruoc, @T0))
FROM (VALUES
    -- Như giao diện
    ('LT_006', N'Đỗ Minh Tuấn',      '0914890123', N'Lái tàu',    'D19E', CAST('2027-03-15' AS DATE), NULL, N'Đội lái tàu Đà Nẵng',      'SAN_SANG',     300),
    ('PL_005', N'Trịnh Văn Hải',     '0915901234', N'Phụ lái',    'D19E', '2027-05-20', NULL, N'Đội lái tàu Đà Nẵng',        'SAN_SANG',     300),
    ('TT_003', N'Nguyễn Thị Hạnh',   '0916012345', N'Trưởng tàu', NULL,   '2027-04-02', NULL, N'Đoàn tiếp viên Đà Nẵng',     'SAN_SANG',     300),
    ('TT_004', N'Lương Văn Thành',   '0917123456', N'Trưởng tàu', NULL,   '2027-08-01', NULL, N'Đoàn tiếp viên Hà Nội',      'SAN_SANG',     260),
    ('PL_006', N'Phạm Quang Huy',    '0918234567', N'Phụ lái',    'D19E', NULL,        -10,  N'Đội lái tàu Hà Nội',          'SAN_SANG',     260),  -- hết hạn khám 10 ngày
    ('LT_007', N'Mai Xuân Trường',   '0919345678', N'Lái tàu',    'D19E', '2027-02-10', NULL, N'Đội lái tàu Sài Gòn',        'SAN_SANG',     200),
    ('PL_007', N'Cao Văn Lực',       '0920456789', N'Phụ lái',    'D19E', '2027-07-18', NULL, N'Đội lái tàu Sài Gòn',        'SAN_SANG',     200),
    ('TV_001', N'Trần Thị Thu',      '0921567890', N'Tiếp viên',  NULL,   '2027-05-30', NULL, N'Đoàn tiếp viên Hà Nội',      'SAN_SANG',     150),
    ('TV_002', N'Vũ Thị Hoa',        '0922678901', N'Tiếp viên',  NULL,   '2027-01-12', NULL, N'Đoàn tiếp viên Phương Nam',  'NGHI_NGOI',    150),
    ('LT_008', N'Hồ Văn Nam',        '0923789012', N'Lái tàu',    'D13E', '2026-12-01', NULL, N'Đội lái tàu Vinh',           'DA_NGHI_VIEC', 900),
    ('TT_005', N'Đinh Văn Quý',      '0924890123', N'Trưởng tàu', NULL,   '2027-10-20', NULL, N'Đoàn tiếp viên Hà Nội',      'SAN_SANG',     120),

    -- Mới (đủ người cho các kíp đang chạy / chạy hôm nay, mỗi depot một nhóm)
    ('LT_009', N'Lê Quốc Bảo',       '0925901234', N'Lái tàu',    'D19E', '2027-09-14', NULL, N'Đội lái tàu Vinh',           'SAN_SANG',     420),
    ('PL_008', N'Nguyễn Hữu Phước',  '0926012345', N'Phụ lái',    'D19E', '2027-06-08', NULL, N'Đội lái tàu Vinh',           'SAN_SANG',     410),
    ('LT_010', N'Trần Minh Khang',   '0927123456', N'Lái tàu',    'D19E', '2027-11-03', NULL, N'Đội lái tàu Sài Gòn',        'SAN_SANG',     400),
    ('PL_009', N'Võ Thành Nhân',     '0928234567', N'Phụ lái',    'D19E', '2027-04-22', NULL, N'Đội lái tàu Sài Gòn',        'SAN_SANG',     390),
    ('TT_006', N'Phan Thị Ngọc Lan', '0929345678', N'Trưởng tàu', NULL,   '2027-08-19', NULL, N'Đoàn tiếp viên Phương Nam',  'SAN_SANG',     380),
    ('LT_011', N'Hoàng Văn Thắng',   '0930456789', N'Lái tàu',    'D19E', '2027-02-27', NULL, N'Đội lái tàu Vinh',           'SAN_SANG',     370),
    ('PL_010', N'Đặng Văn Toàn',     '0931567890', N'Phụ lái',    'D19E', '2027-10-05', NULL, N'Đội lái tàu Vinh',           'SAN_SANG',     360),
    ('LT_012', N'Nguyễn Đức Mạnh',   '0932678901', N'Lái tàu',    'D19E', '2027-07-11', NULL, N'Đội lái tàu Hà Nội',         'SAN_SANG',     350),
    ('PL_011', N'Bùi Văn Kiên',      '0933789012', N'Phụ lái',    'D19E', '2027-03-30', NULL, N'Đội lái tàu Hà Nội',         'SAN_SANG',     340),
    ('LT_013', N'Trương Công Danh',  '0934890123', N'Lái tàu',    'D19E', '2027-05-16', NULL, N'Đội lái tàu Vinh',           'SAN_SANG',     330),
    ('PL_012', N'Lê Văn Sơn',        '0935901234', N'Phụ lái',    'D19E', '2027-12-12', NULL, N'Đội lái tàu Vinh',           'SAN_SANG',     320),
    ('LT_014', N'Vương Đình Huấn',   '0936012345', N'Lái tàu',    'D19E', '2027-09-01', NULL, N'Đội lái tàu Hà Nội',         'SAN_SANG',     310),
    ('PL_013', N'Đỗ Xuân Hòa',       '0937123456', N'Phụ lái',    'D19E', '2027-01-25', NULL, N'Đội lái tàu Hà Nội',         'SAN_SANG',     290),
    ('LT_015', N'Nguyễn Văn Phú',    '0938234567', N'Lái tàu',    'D19E', '2027-06-21', NULL, N'Đội lái tàu Đà Nẵng',        'SAN_SANG',     280),
    ('PL_014', N'Huỳnh Tấn Lộc',     '0939345678', N'Phụ lái',    'D19E', '2027-08-30', NULL, N'Đội lái tàu Đà Nẵng',        'SAN_SANG',     270),
    ('LT_016', N'Lý Thanh Tùng',     '0940456789', N'Lái tàu',    'D19E', '2027-11-18', NULL, N'Đội lái tàu Sài Gòn',        'SAN_SANG',     180),  -- dự phòng
    ('PL_015', N'Kiều Văn Quang',    '0941567890', N'Phụ lái',    'D19E', NULL,         20,   N'Đội lái tàu Vinh',           'SAN_SANG',     160)   -- dự phòng, hạn khám còn 20 ngày
) v(Ma, HoTen, Sdt, ChucDanh, Bang, HanKham, HanKhamSauSoNgay, DonVi, TrangThai, NgayTruoc);

IF (SELECT COUNT(*) FROM nhansu.NhanVien) <> 41
    RAISERROR(N'Phải có 41 nhân viên sau khi thêm.', 16, 1);

-- C3. Kíp lái.
--     Sửa 4 kíp gốc:
--       SE1 Hà Nội → Vinh : tàu đã qua Vinh (có giờ đi thực tế ở Lăng Cô) → HOAN_THANH
--       SE4 Sài Gòn → Nha Trang : tàu đã rời Nha Trang → HOAN_THANH
--       SE19 / NA1 : trưởng tàu gốc (TT_001 / TT_002) đang đi SE1 / SE4 cùng khung giờ → thay TT_004 / TT_005 (như giao diện)
UPDATE k SET TrangThai = 'HOAN_THANH'
FROM nhansu.PhanCongKipLai k
WHERE k.MaChuyenTau IN (@CtSE1, @CtSE4);

UPDATE k SET MaTruongTau = (SELECT MaNhanVien FROM nhansu.NhanVien WHERE MaNVCode = 'TT_004')
FROM nhansu.PhanCongKipLai k WHERE k.MaChuyenTau = @CtSE19;

UPDATE k SET MaTruongTau = (SELECT MaNhanVien FROM nhansu.NhanVien WHERE MaNVCode = 'TT_005')
FROM nhansu.PhanCongKipLai k WHERE k.MaChuyenTau = @CtNA1;

--     Thêm 10 kíp. Lái tàu / phụ lái đổi ở ga có depot; trưởng tàu đi suốt chuyến.
--     Trạng thái theo giờ đi thực tế đã ghi trong lịch dừng của A Bá:
--       SE1 đã rời Lăng Cô (đang qua đèo Hải Vân) · SE2 đã rời Vinh · SE4 đã rời Nha Trang · SE19 / NA1 / SE3 chưa chạy.
DECLARE @KipMoi TABLE (Mac VARCHAR(10), LaiTau VARCHAR(20), PhuLai VARCHAR(20), TruongTau VARCHAR(20),
                       GaNhan CHAR(3), GaGiao CHAR(3), TrangThai VARCHAR(20));
INSERT INTO @KipMoi VALUES
    ('SE1',  'LT_009', 'PL_008', 'TT_001', 'VIN', 'DAN', 'DANG_THUC_HIEN'),  -- Vinh 11:15 → Đà Nẵng 21:15
    ('SE1',  'LT_007', 'PL_007', 'TT_001', 'DAN', 'SGO', 'DA_PHAN_CONG'),    -- Đà Nẵng 21:30 → Sài Gòn 14:00
    ('SE2',  'LT_010', 'PL_009', 'TT_006', 'SGO', 'DAN', 'HOAN_THANH'),      -- Sài Gòn 19:10 hôm qua → Đà Nẵng 10:00
    ('SE2',  'LT_011', 'PL_010', 'TT_006', 'DAN', 'VIN', 'HOAN_THANH'),      -- Đà Nẵng 10:15 → Vinh 17:00
    ('SE2',  'LT_012', 'PL_011', 'TT_006', 'VIN', 'HNI', 'DANG_THUC_HIEN'),  -- Vinh 17:15 → Hà Nội 23:00
    ('SE4',  'LT_006', 'PL_005', 'TT_002', 'NTR', 'DAN', 'DANG_THUC_HIEN'),  -- Nha Trang 04:35 → Đà Nẵng 15:10
    ('SE4',  'LT_013', 'PL_012', 'TT_002', 'DAN', 'VIN', 'DA_PHAN_CONG'),    -- Đà Nẵng 15:25 → Vinh 00:40
    ('SE4',  'LT_014', 'PL_013', 'TT_002', 'VIN', 'HNI', 'DA_PHAN_CONG'),    -- Vinh 00:50 → Hà Nội 07:30
    ('SE19', 'LT_015', 'PL_014', 'TT_004', 'VIN', 'DAN', 'DA_PHAN_CONG'),    -- Vinh 02:00 → Đà Nẵng 12:20
    ('SE3',  'LT_012', 'PL_011', 'TT_006', 'HNI', 'VIN', 'DA_PHAN_CONG');    -- 19:00 ngày mai, kíp SE2 nghỉ 20 giờ ở Hà Nội rồi chạy chiều về

INSERT INTO nhansu.PhanCongKipLai (MaChuyenTau, MaLaiTau, MaPhuLai, MaTruongTau, MaGaNhanBan, MaGaBanGiao, TrangThai)
SELECT c.MaChuyenTau, lt.MaNhanVien, pl.MaNhanVien, tt.MaNhanVien, gN.MaGa, gG.MaGa, k.TrangThai
FROM @KipMoi k
JOIN @Chuyen c           ON c.Mac = k.Mac
JOIN nhansu.NhanVien lt  ON lt.MaNVCode = k.LaiTau
JOIN nhansu.NhanVien pl  ON pl.MaNVCode = k.PhuLai
JOIN nhansu.NhanVien tt  ON tt.MaNVCode = k.TruongTau
JOIN hatang.Ga gN        ON gN.MaGaCode = k.GaNhan
JOIN hatang.Ga gG        ON gG.MaGaCode = k.GaGiao;

IF (SELECT COUNT(*) FROM nhansu.PhanCongKipLai) <> 14
    RAISERROR(N'Phải có 14 kíp (4 gốc + 10 mới).', 16, 1);

-- C4. Kiểm tra lên ban (cồn 0.00 mg/L và nghỉ >= 8 giờ thì đạt — cột DuDieuKien tự tính).
--     Thời điểm kiểm tra: 30–55 phút trước giờ nhận ban. Kíp chưa tới giờ nhận ban thì chưa kiểm tra.
--     - Giữ 6 phiếu gốc của kíp SE1 Hà Nội → Vinh và SE4 Sài Gòn → Nha Trang, ghi lại thời điểm cho đúng.
--     - Giữ 2 phiếu gốc của LT_004 / PL_003 cho SE19 (tàu SAN_SANG, kíp đã kiểm tra).
--     - Phiếu KHÔNG ĐẠT của Vũ Tiến Đạt (LT_002, cồn 0.15, nghỉ 6 giờ): gốc ghi cho SE3 ngày mai, không thể đo trước
--       hơn một ngày → chuyển sang SE19 tối nay: anh được xếp lái SE19, đo không đạt nên đã thay bằng LT_004.
--       Phiếu vẫn giữ trong sổ; ô chọn lái tàu SE19 sẽ ẩn anh kèm lý do.
--     - Xoá 2 phiếu gốc của NA1 (22:15 tối nay, chưa tới giờ nhận ban) → NA1 nằm trong hàng chờ kiểm tra.
DECLARE @PhieuGoc TABLE (Ma VARCHAR(20), Mac VARCHAR(10), MacMoi VARCHAR(10), Phut INT);
INSERT INTO @PhieuGoc VALUES
    ('LT_001', 'SE1',  'SE1',   325),   -- 05:25, nhận ban Hà Nội 06:10
    ('PL_001', 'SE1',  'SE1',   330),
    ('TT_001', 'SE1',  'SE1',   335),
    ('LT_003', 'SE4',  'SE4',  -330),   -- 18:30 hôm qua, nhận ban Sài Gòn 19:15
    ('PL_002', 'SE4',  'SE4',  -325),
    ('TT_002', 'SE4',  'SE4',  -320),
    ('LT_002', 'SE3',  'SE19', 1135),   -- 18:55, không đạt
    ('LT_004', 'SE19', 'SE19', 1145),   -- 19:05, nhận ban Hà Nội 19:50
    ('PL_003', 'SE19', 'SE19', 1150);

UPDATE ks
SET MaChuyenTau     = cMoi.MaChuyenTau,
    ThoiDiemKiemTra = DATEADD(MINUTE, p.Phut, @T0)
FROM nhansu.KiemTraSucKhoe ks
JOIN nhansu.NhanVien nv ON nv.MaNhanVien = ks.MaNhanVien
JOIN @PhieuGoc p        ON p.Ma = nv.MaNVCode
JOIN @Chuyen cCu        ON cCu.Mac = p.Mac AND cCu.MaChuyenTau = ks.MaChuyenTau
JOIN @Chuyen cMoi       ON cMoi.Mac = p.MacMoi;

IF @@ROWCOUNT <> 9
    RAISERROR(N'Phải cập nhật đủ 9 phiếu kiểm tra gốc.', 16, 1);

DELETE FROM nhansu.KiemTraSucKhoe WHERE MaChuyenTau = @CtNA1;

INSERT INTO nhansu.KiemTraSucKhoe (MaNhanVien, MaChuyenTau, NongDoConMgL, SoGioNghiTruocCa, ThoiDiemKiemTra)
SELECT nv.MaNhanVien, c.MaChuyenTau, v.Con, v.GioNghi, DATEADD(MINUTE, v.Phut, @T0)
FROM (VALUES
    -- SE1 Vinh → Đà Nẵng (nhận ban 11:15)
    ('LT_009', 'SE1',  0.00, 11.0,  630),
    ('PL_008', 'SE1',  0.00, 10.5,  635),
    -- SE2 Sài Gòn → Đà Nẵng (nhận ban 19:10 hôm qua)
    ('LT_010', 'SE2',  0.00, 12.5, -340),
    ('PL_009', 'SE2',  0.00, 13.0, -335),
    ('TT_006', 'SE2',  0.00, 15.0, -330),
    -- SE2 Đà Nẵng → Vinh (nhận ban 10:15)
    ('LT_011', 'SE2',  0.00,  9.5,  570),
    ('PL_010', 'SE2',  0.00, 10.0,  575),
    -- SE2 Vinh → Hà Nội (nhận ban 17:15)
    ('LT_012', 'SE2',  0.00, 11.5,  990),
    ('PL_011', 'SE2',  0.00, 12.0,  995),
    -- SE4 Nha Trang → Đà Nẵng (nhận ban 04:35)
    ('LT_006', 'SE4',  0.00, 10.0,  230),
    ('PL_005', 'SE4',  0.00,  9.0,  235),
    -- SE19 Hà Nội → Vinh: trưởng tàu thay mới (nhận ban 19:50)
    ('TT_004', 'SE19', 0.00, 14.0, 1152)
) v(Ma, Mac, Con, GioNghi, Phut)
JOIN nhansu.NhanVien nv ON nv.MaNVCode = v.Ma
JOIN @Chuyen c          ON c.Mac = v.Mac;

IF (SELECT COUNT(*) FROM nhansu.KiemTraSucKhoe) <> 21
    RAISERROR(N'Phải có 21 phiếu kiểm tra lên ban.', 16, 1);

-- C5. Trạng thái nhân viên theo kíp (R13): đang ở kíp DANG_THUC_HIEN → DANG_LAM;
--     vừa bàn giao (kíp HOAN_THANH của chuyến chưa kết thúc) → NGHI_NGOI; còn lại giữ nguyên.
UPDATE nv
SET TrangThai = CASE
        WHEN EXISTS (SELECT 1 FROM nhansu.PhanCongKipLai k
                     WHERE k.TrangThai = 'DANG_THUC_HIEN'
                       AND nv.MaNhanVien IN (k.MaLaiTau, k.MaPhuLai, k.MaTruongTau)) THEN 'DANG_LAM'
        WHEN EXISTS (SELECT 1 FROM nhansu.PhanCongKipLai k
                     JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = k.MaChuyenTau
                     WHERE k.TrangThai = 'HOAN_THANH' AND ct.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
                       AND nv.MaNhanVien IN (k.MaLaiTau, k.MaPhuLai, k.MaTruongTau)) THEN 'NGHI_NGOI'
        ELSE nv.TrangThai END
FROM nhansu.NhanVien nv
WHERE nv.TrangThai <> 'DA_NGHI_VIEC';

COMMIT TRANSACTION;

PRINT N'Seed S20261009_0830 (Phương tiện + Nhân sự Sprint 1): xong.';
PRINT N'  Đầu máy 8 · toa 78 (62 trong đoàn tàu, 14 ở bãi, 2 bảo dưỡng) · 72 dòng ChiTietDoanTau';
PRINT N'  Nhân viên 41 · kíp 14 · phiếu kiểm tra lên ban 21 · SE3 13 ga dừng (32,5 giờ)';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SET NOEXEC OFF;
GO
