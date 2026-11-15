@echo off
setlocal EnableExtensions
REM =====================================================================
REM  TAO LAI CSDL TU DAU: script goc -> cac file trong thu_tu_chay.txt
REM
REM  Cach chay (cmd, dung o thu muc nao cung duoc):
REM    tao_lai_csdl.cmd                         CSDL QuanLyDuongSatV2, ngay du lieu = HOM NAY (gio may)
REM    tao_lai_csdl.cmd 2026-11-10              du lieu tinh theo ngay 10/11 (dung truoc cho ngay demo)
REM    tao_lai_csdl.cmd 2026-11-10 QLDS_Thu     dung CSDL khac (thu nghiem, kiem thu)
REM    tao_lai_csdl.cmd 2026-11-10 QLDS_Thu .\SQLEXPRESS   may chu khac (mac dinh ".")
REM
REM  - PHAI TAT UNG DUNG truoc khi chay: script goc DROP DATABASE.
REM  - CSDL dang co duoc sao luu truoc (thu muc Backup cua SQL Server).
REM  - Script goc lay ngay theo gio UTC (truoc 7:00 sang ra ngay hom qua):
REM    file nay thay bang ngay truyen vao / ngay theo gio may.
REM  - Tai khoan dang nhap + mat khau demo: xem README.md.
REM =====================================================================

set "DB_DIR=%~dp0"
set "GOC=%DB_DIR%000_tao_csdl_goc.sql"
set "NGAY=%~1"
set "CSDL=%~2"
set "MAYCHU=%~3"
if "%CSDL%"=="" set "CSDL=QuanLyDuongSatV2"
if "%MAYCHU%"=="" set "MAYCHU=."
if "%NGAY%"=="" for /f %%d in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd"') do set "NGAY=%%d"
for /f %%t in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmm"') do set "LUC=%%t"
set "TAM=%TEMP%\qlds_tao_lai_%CSDL%"
if not exist "%TAM%" mkdir "%TAM%"
set "SQL=sqlcmd -S %MAYCHU% -E -I -f 65001 -b"

echo.
echo  May chu          : %MAYCHU%
echo  CSDL             : %CSDL%
echo  Ngay goc du lieu : %NGAY%
echo  Log tung buoc    : %TAM%
echo.

REM --- 0. Sao luu CSDL dang co ---
echo [0] Sao luu CSDL dang co (neu co)...
sqlcmd -S %MAYCHU% -E -b -Q "IF DB_ID('%CSDL%') IS NOT NULL BACKUP DATABASE [%CSDL%] TO DISK = N'%CSDL%_truoc_tao_lai_%LUC%.bak' WITH COPY_ONLY, INIT" > "%TAM%\0_saoluu.log"
if errorlevel 1 (
    echo LOI: khong sao luu duoc CSDL dang co. Xem %TAM%\0_saoluu.log
    exit /b 1
)

REM --- 1. Chep script goc, thay ngay + ten CSDL ---
echo [1] Chuan bi script goc...
powershell -NoProfile -Command "$s = [IO.File]::ReadAllText('%GOC%', [Text.Encoding]::UTF8); if (-not $s.Contains('CAST(SYSUTCDATETIME() AS DATE)')) { exit 3 }; $s = $s.Replace('CAST(SYSUTCDATETIME() AS DATE)', 'CAST(''%NGAY%'' AS DATE)').Replace('QuanLyDuongSatV2', '%CSDL%'); [IO.File]::WriteAllText('%TAM%\goc.sql', $s, (New-Object Text.UTF8Encoding $true))"
if errorlevel 1 (
    echo LOI: khong tim thay dong lay ngay trong script goc. Khong chay tiep.
    exit /b 1
)

REM --- 2. Script goc (DROP + CREATE + du lieu goc) ---
echo [2] Chay script goc (khoang 10-20 giay)...
pushd "%TAM%"
%SQL% -i goc.sql -o 2_goc.log
if errorlevel 1 (
    popd
    echo LOI o script goc. Ung dung con mo? Xem %TAM%\2_goc.log
    exit /b 1
)
popd

REM --- 3. Migration + seed + kiem tra theo thu_tu_chay.txt ---
echo [3] Migration + seed theo thu_tu_chay.txt...
set "CANH_BAO="
for /f "usebackq eol=# tokens=1,2 delims=\" %%a in ("%DB_DIR%thu_tu_chay.txt") do (
    call :chay "%%a" "%%b"
    if errorlevel 1 exit /b 1
)

echo.
echo ============ XONG ============
sqlcmd -S %MAYCHU% -E -W -h -1 -d %CSDL% -Q "SET NOCOUNT ON; SELECT '  Nhan vien : ' + CAST(COUNT(*) AS varchar) FROM nhansu.NhanVien; SELECT '  Dau may   : ' + CAST(COUNT(*) AS varchar) FROM phuongtien.DauMay; SELECT '  Ve        : ' + CAST(COUNT(*) AS varchar) FROM vantai.Ve; SELECT '  Tai khoan : ' + CAST(COUNT(*) AS varchar) FROM phanquyen.TaiKhoan; SELECT '  Quyen     : ' + CAST(COUNT(*) AS varchar) FROM phanquyen.Quyen"
if defined CANH_BAO echo  CANH BAO: script kiem tra bao vi pham - xem log trong %TAM%
echo.
echo  Mo ung dung, dang nhap bang tai khoan demo (README.md).
exit /b 0

REM ---------------------------------------------------------------------
REM  :chay <thu muc> <ten file>   (sqlcmd hieu "/" la dau tham so -> cd vao thu muc, truyen ten file tran)
REM ---------------------------------------------------------------------
:chay
set "THU_MUC=%~1"
set "TEP=%~2"
if not exist "%DB_DIR%%THU_MUC%\%TEP%" (
    echo LOI: khong thay %THU_MUC%\%TEP%
    exit /b 1
)
echo     %THU_MUC%\%TEP%
pushd "%DB_DIR%%THU_MUC%"
%SQL% -d %CSDL% -i "%TEP%" > "%TAM%\%TEP%.log"
set "KQ=%errorlevel%"
popd
if "%KQ%"=="0" exit /b 0
if /i "%THU_MUC%"=="tests" (
    echo     ^> canh bao: co vi pham, xem %TAM%\%TEP%.log
    set "CANH_BAO=1"
    exit /b 0
)
echo LOI o %THU_MUC%\%TEP%. Xem %TAM%\%TEP%.log
exit /b 1
