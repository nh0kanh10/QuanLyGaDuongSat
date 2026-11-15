# =====================================================================
#  Sinh database\CSDL_tron_goi.sql = MOT file .sql dung lai toan bo CSDL
#  (script goc 000_tao_csdl_goc.sql + moi file trong thu_tu_chay.txt).
#  Mo file do bang SSMS -> F5 la xong, khong phai chay tung file.
#
#  Cach chay: nhap dup ghep_tron_goi.cmd (hoac: powershell -ExecutionPolicy Bypass -File ghep_tron_goi.ps1)
#  Chay lai moi khi them / sua dong trong thu_tu_chay.txt, roi commit ca CSDL_tron_goi.sql.
#
#  Thu nghiem tren CSDL khac (khong dung CSDL that):
#    ghep_tron_goi.cmd -TenCsdl QLDS_Thu        -> ghi ra %TEMP%\CSDL_tron_goi_QLDS_Thu.sql
#
#  Khac tao_lai_csdl.cmd: ngay du lieu = ngay chay file (gio may), khong truyen ngay khac duoc.
# =====================================================================
param(
    [string]$TenCsdl = 'QuanLyDuongSatV2',
    [string]$Ra = ''
)
$ErrorActionPreference = 'Stop'
$DB = $PSScriptRoot
$GocCsdl = 'QuanLyDuongSatV2'
if (-not $Ra) {
    if ($TenCsdl -eq $GocCsdl) { $Ra = Join-Path $DB 'CSDL_tron_goi.sql' }
    else { $Ra = Join-Path $env:TEMP "CSDL_tron_goi_$TenCsdl.sql" }
}

function Doc([string]$p) {
    $s = [IO.File]::ReadAllText($p, [Text.Encoding]::UTF8)
    return $s.TrimStart([char]0xFEFF).Replace("`r`n", "`n")
}

function ThayDung([string]$s, [string]$cu, [string]$moi, [int]$soLan) {
    $dem = ([regex]::Matches($s, [regex]::Escape($cu))).Count
    if ($dem -ne $soLan) {
        throw "Mong doi $soLan cho '$cu' trong script goc, thay $dem. Script goc da doi -> sua ghep_tron_goi.ps1."
    }
    return $s.Replace($cu, $moi)
}

function ChuoiSql([string]$s) { return $s.Replace("'", "''") }

# --- Danh sach file theo thu_tu_chay.txt (dung chung voi tao_lai_csdl.cmd) ---
$cacPhan = New-Object System.Collections.Generic.List[object]
foreach ($dong in [IO.File]::ReadAllLines((Join-Path $DB 'thu_tu_chay.txt'), [Text.Encoding]::UTF8)) {
    $d = $dong.Trim().TrimStart([char]0xFEFF)
    if (-not $d -or $d.StartsWith('#')) { continue }
    $tep = Join-Path $DB $d
    if (-not (Test-Path $tep)) { throw "Khong thay file '$d' (ghi trong thu_tu_chay.txt)." }
    $cacPhan.Add([pscustomobject]@{ Ten = $d; Tep = $tep })
}
$N = $cacPhan.Count + 1   # buoc 1 = script goc, buoc 2..N = cac file

# --- Script goc: ngay sinh du lieu theo GIO MAY (script goc dung UTC -> truoc 7:00 sang ra ngay hom qua) ---
$goc = Doc (Join-Path $DB '000_tao_csdl_goc.sql')
$goc = ThayDung $goc 'DECLARE @NgayNay DATE = CAST(SYSUTCDATETIME() AS DATE);' `
                     'DECLARE @NgayNay DATE = CAST(SYSDATETIME() AS DATE);   -- [tron goi] ngay theo gio may' 1
$goc = ThayDung $goc 'DATEADD(DAY, 15, CAST(SYSUTCDATETIME() AS DATE))' 'DATEADD(DAY, 15, CAST(SYSDATETIME() AS DATE))' 1

# --- Dau file (sua chu o day, dung sua CSDL_tron_goi.sql) ---
$dau = @'
/* =====================================================================
   CSDL TRỌN GÓI — dựng lại toàn bộ CSDL QuanLyDuongSatV2 bằng MỘT file
   File SINH TỰ ĐỘNG bằng database\ghep_tron_goi.cmd — KHÔNG sửa tay.
   Gồm: 000_tao_csdl_goc.sql + {SO_TEP} file theo thu_tu_chay.txt (migration, seed, kiểm tra dữ liệu).

   Cách chạy:
     1. TẮT ỨNG DỤNG (file này xoá CSDL đang có rồi tạo lại).
     2. SSMS: kết nối máy chủ → File > Open > file này → Execute (F5).
        Mất khoảng 1–2 phút; nhiều bảng kết quả hiện ra là bình thường.
        Hoặc cmd (đứng trong thư mục database): sqlcmd -S . -E -f 65001 -i CSDL_tron_goi.sql
     3. Xem tab Messages, dòng cuối phải là "=== HOÀN TẤT ===".
        Nếu là "=== CHƯA XONG ===": kéo lên tìm dòng chữ đỏ ĐẦU TIÊN.

   - CSDL cũ được tự sao lưu thành QuanLyDuongSatV2_truoc_tron_goi.bak (thư mục Backup của SQL Server).
     Lấy lại: RESTORE DATABASE QuanLyDuongSatV2 FROM DISK = N'QuanLyDuongSatV2_truoc_tron_goi.bak' WITH REPLACE;
   - Ngày dữ liệu (chuyến tàu, kíp lái, vé…) = ngày chạy file (giờ máy) → hôm demo nên chạy lại.
     Cần dựng sẵn cho một ngày khác: dùng tao_lai_csdl.cmd <yyyy-MM-dd>.
   - Đăng nhập ứng dụng: admin / Demo@123 (các tài khoản khác: database\README.md).

   Thêm migration / seed mới: thêm dòng vào thu_tu_chay.txt → chạy ghep_tron_goi.cmd → commit cả file này.
   ===================================================================== */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;   -- như ADO.NET / SSMS; sqlcmd mặc định OFF
SET ANSI_NULLS ON;
SET NOEXEC OFF;
GO
USE master;
GO
-- Mỗi bước chạy xong (không bị dừng) thì ghi 1 dòng vào đây; cuối file đếm lại.
IF OBJECT_ID(N'tempdb..#TronGoi') IS NOT NULL DROP TABLE #TronGoi;
CREATE TABLE #TronGoi (Buoc INT NOT NULL);
IF OBJECT_ID(N'tempdb..#TronGoiBatDau') IS NOT NULL DROP TABLE #TronGoiBatDau;
CREATE TABLE #TronGoiBatDau (Luc DATETIME NOT NULL);
INSERT #TronGoiBatDau (Luc) VALUES (GETDATE());
IF OBJECT_ID(N'tempdb..#TronGoiViPham') IS NOT NULL DROP TABLE #TronGoiViPham;
CREATE TABLE #TronGoiViPham (SoLoi INT NOT NULL);
GO

PRINT N'--- [0/{N}] Sao lưu CSDL đang có ---';
IF DB_ID(N'QuanLyDuongSatV2') IS NOT NULL
BEGIN
    BEGIN TRY
        BACKUP DATABASE QuanLyDuongSatV2 TO DISK = N'QuanLyDuongSatV2_truoc_tron_goi.bak' WITH COPY_ONLY, INIT;
        PRINT N'Đã sao lưu vào: ' + CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(400)) + N'\QuanLyDuongSatV2_truoc_tron_goi.bak';
    END TRY
    BEGIN CATCH
        DECLARE @LoiSaoLuu NVARCHAR(2000) = ERROR_MESSAGE();
        RAISERROR(N'Không sao lưu được CSDL cũ (%s). DỪNG — chưa xoá gì.', 16, 1, @LoiSaoLuu);
        SET NOEXEC ON;
    END CATCH
END
ELSE
    PRINT N'Chưa có CSDL QuanLyDuongSatV2, bỏ qua sao lưu.';
GO

PRINT N'--- [1/{N}] 000_tao_csdl_goc.sql (xoá + tạo lại CSDL, dữ liệu gốc) ---';
GO
'@

# Sau buoc 1: CSDL phai vua duoc TAO MOI (tao sau luc bat dau chay file). Khong thi dung ngay,
# tranh truong hop xoa that bai ma cac buoc sau ghi chong len CSDL cu.
$kiemTraBuoc1 = @'
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
   OR (SELECT create_date FROM sys.databases WHERE name = N'QuanLyDuongSatV2') < (SELECT MAX(Luc) FROM #TronGoiBatDau)
BEGIN
    RAISERROR(N'CSDL QuanLyDuongSatV2 chưa được xoá và tạo lại ở bước 1 (ứng dụng hoặc cửa sổ SSMS khác còn mở CSDL?) — dừng.', 16, 1);
    SET NOEXEC ON;
END
GO
'@

# Sau moi buoc: ghi dau "da chay", roi dung lai o CSDL du an (khong co CSDL -> dung ca file)
function SauBuoc([int]$i, [string]$kiemTra = '') {
    return @"

GO
$($kiemTra.Replace("`r`n", "`n"))INSERT #TronGoi (Buoc) VALUES ($i);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
"@
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.Append($dau.Replace('{N}', "$N").Replace('{SO_TEP}', "$($cacPhan.Count)").Replace("`r`n", "`n") + "`n")
[void]$sb.Append($goc.TrimEnd() + "`n")
[void]$sb.Append((SauBuoc 1 ($kiemTraBuoc1 + "`n")))

$cacBuoc = New-Object System.Collections.Generic.List[string]
[void]$cacBuoc.Add("(1, N'000_tao_csdl_goc.sql')")
$i = 1
foreach ($p in $cacPhan) {
    $i++
    $s = Doc $p.Tep
    # Bo "SET NOEXEC OFF;" cuoi moi file: buoc truoc bi dung (NOEXEC ON) thi cac buoc sau cung khong chay.
    $s = [regex]::Replace($s, '(?m)^SET NOEXEC OFF;[ \t]*$', '-- [tron goi] bo SET NOEXEC OFF: buoc nay loi thi dung ca file')
    if ($p.Ten.StartsWith('tests\')) {
        # File kiem tra chi co 1 lo (khong GO) va de @SoLoi -> ghi lai so vi pham cho cuoi file bao.
        if ($s -match 'DECLARE @SoLoi INT' -and $s -notmatch '(?m)^\s*GO\s*$') {
            $s = $s.TrimEnd() + "`nIF @SoLoi > 0 INSERT #TronGoiViPham (SoLoi) VALUES (@SoLoi);   -- [tron goi]`n"
        }
    }
    $ten = ChuoiSql $p.Ten
    [void]$sb.Append("`nGO`nPRINT N'--- [$i/$N] $ten ---';`nGO`n")
    [void]$sb.Append($s.TrimEnd() + "`n")
    [void]$sb.Append((SauBuoc $i))
    [void]$cacBuoc.Add("($i, N'$ten')")
}

# --- Cuoi file: dem buoc da chay + dau hieu du lieu cua tung seed (seed loi thi rollback ca file -> mat dau hieu) ---
# Them seed moi: nen them 1 dau hieu vao @DauHieu ben duoi (khong bat buoc; thieu thi chi dua vao dem buoc).
$cuoi = @'

GO
SET NOEXEC OFF;
GO
USE master;
GO
PRINT N'';
DECLARE @Buoc TABLE (Buoc INT PRIMARY KEY, Ten NVARCHAR(300));
INSERT @Buoc (Buoc, Ten) VALUES
{CAC_BUOC};
DECLARE @DungO INT = (SELECT MIN(b.Buoc) FROM @Buoc b WHERE NOT EXISTS (SELECT 1 FROM #TronGoi t WHERE t.Buoc = b.Buoc));
DECLARE @TenDungO NVARCHAR(300) = (SELECT Ten FROM @Buoc WHERE Buoc = @DungO);
DECLARE @SoLoi INT = (SELECT MAX(SoLoi) FROM #TronGoiViPham);
DECLARE @Thieu NVARCHAR(MAX) = N'';

IF DB_ID(N'QuanLyDuongSatV2') IS NULL
    SET @Thieu = N'không có CSDL QuanLyDuongSatV2';
ELSE
BEGIN
    DECLARE @Sql NVARCHAR(MAX) = N'
        USE QuanLyDuongSatV2;
        DECLARE @DauHieu TABLE (Ten NVARCHAR(200), Co BIT);
        INSERT @DauHieu VALUES
          (N''nhân viên theo chức danh (seed Sprint 1)'', CASE WHEN EXISTS (SELECT 1 FROM nhansu.NhanVien WHERE MaNVCode LIKE ''LT[_]%'') THEN 1 ELSE 0 END),
          (N''đoàn tàu đã lập toa (seed Sprint 1)'',      CASE WHEN EXISTS (SELECT 1 FROM vanhanh.ChiTietDoanTau) THEN 1 ELSE 0 END),
          (N''phiếu bảo dưỡng mẫu (seed Bảo trì)'',      CASE WHEN (SELECT COUNT(*) FROM baotri.NhatKyBaoDuong) > 1 THEN 1 ELSE 0 END),
          (N''vé mẫu TK_MAU_ (seed doanh thu)'',          CASE WHEN EXISTS (SELECT 1 FROM vantai.Ve WHERE MaVeCode LIKE ''TK[_]MAU[_]%'') THEN 1 ELSE 0 END),
          (N''tài khoản demo mật khẩu Demo@123'',         CASE WHEN EXISTS (SELECT 1 FROM phanquyen.TaiKhoan WHERE TenDangNhap = ''quanly'')
                                                                 AND NOT EXISTS (SELECT 1 FROM phanquyen.TaiKhoan WHERE MatKhauHash NOT LIKE ''PBKDF2$%'') THEN 1 ELSE 0 END),
          (N''vé lịch sử TK_LS_ (seed báo cáo)'',         CASE WHEN EXISTS (SELECT 1 FROM vantai.Ve WHERE MaVeCode LIKE ''TK[_]LS[_]%'') THEN 1 ELSE 0 END),
          (N''view báo cáo baocao.vw_HeSoSuDungGhe'',     CASE WHEN OBJECT_ID(''baocao.vw_HeSoSuDungGhe'', ''V'') IS NOT NULL THEN 1 ELSE 0 END),
          (N''cột ToaXeKhach.NgungBan (toa bán vé)'',     CASE WHEN COL_LENGTH(''vantai.ToaXeKhach'', ''NgungBan'') IS NOT NULL THEN 1 ELSE 0 END),
          (N''thủ tục vantai.sp_LayDanhSachNgayCoChuyen'', CASE WHEN OBJECT_ID(''vantai.sp_LayDanhSachNgayCoChuyen'', ''P'') IS NOT NULL THEN 1 ELSE 0 END);
        SELECT @ThieuRa = STUFF((SELECT N''; '' + Ten FROM @DauHieu WHERE Co = 0 FOR XML PATH(''''), TYPE).value(''.'', ''NVARCHAR(MAX)''), 1, 2, N'''');
        DECLARE @NV INT = (SELECT COUNT(*) FROM nhansu.NhanVien), @DM INT = (SELECT COUNT(*) FROM phuongtien.DauMay),
                @Toa INT = (SELECT COUNT(*) FROM phuongtien.ToaXe), @Chuyen INT = (SELECT COUNT(*) FROM vanhanh.ChuyenTau),
                @Ve INT = (SELECT COUNT(*) FROM vantai.Ve), @TK INT = (SELECT COUNT(*) FROM phanquyen.TaiKhoan);
        IF @ThieuRa IS NULL
            PRINT CONCAT(N''Nhân viên '', @NV, N'' · đầu máy '', @DM, N'' · toa xe '', @Toa,
                         N'' · chuyến tàu '', @Chuyen, N'' · vé '', @Ve, N'' · tài khoản '', @TK);';
    DECLARE @ThieuRa NVARCHAR(MAX);
    BEGIN TRY
        EXEC sp_executesql @Sql, N'@ThieuRa NVARCHAR(MAX) OUTPUT', @ThieuRa = @ThieuRa OUTPUT;
        SET @Thieu = ISNULL(@ThieuRa, N'');
    END TRY
    BEGIN CATCH
        SET @Thieu = N'không đọc được dữ liệu (' + ERROR_MESSAGE() + N')';
    END CATCH
END

DECLARE @ThieuNgan NVARCHAR(1500) = LEFT(@Thieu, 1500);
IF @DungO IS NOT NULL
    RAISERROR(N'=== CHƯA XONG: dừng ở bước %d (%s). Kéo lên tìm dòng chữ đỏ ĐẦU TIÊN ở tab Messages. ===', 16, 1, @DungO, @TenDungO);
ELSE IF @Thieu <> N''
    RAISERROR(N'=== CHƯA XONG: thiếu %s. Kéo lên tìm dòng chữ đỏ ĐẦU TIÊN ở tab Messages. ===', 16, 1, @ThieuNgan);
ELSE IF @SoLoi IS NOT NULL
    RAISERROR(N'=== HOÀN TẤT nhưng kiểm tra dữ liệu báo %d vi phạm (xem bảng CHI TIẾT phía trên). ===', 16, 1, @SoLoi);
ELSE
    PRINT N'=== HOÀN TẤT ===';
GO
'@
[void]$sb.Append($cuoi.Replace('{CAC_BUOC}', ($cacBuoc -join ",`n")).Replace("`r`n", "`n"))

$noiDung = $sb.ToString()
if ($TenCsdl -ne $GocCsdl) { $noiDung = $noiDung.Replace($GocCsdl, $TenCsdl) }
[IO.File]::WriteAllText($Ra, $noiDung.Replace("`n", "`r`n"), (New-Object System.Text.UTF8Encoding $true))
$soDong = ([regex]::Matches($noiDung, "`n")).Count
Write-Host ("Da ghi {0} ({1} KB, {2} dong, {3} buoc)" -f $Ra, [int]((Get-Item $Ra).Length / 1024), $soDong, $N)
