@echo off
setlocal
REM =====================================================================
REM  SINH LAI CSDL_tron_goi.sql (1 file .sql dung lai toan bo CSDL: mo bang SSMS -> F5)
REM  = script goc 000_tao_csdl_goc.sql + moi file trong thu_tu_chay.txt.
REM
REM  Nhap dup file nay, hoac (cmd): database\ghep_tron_goi.cmd
REM  Chay lai MOI KHI them / sua dong trong thu_tu_chay.txt, roi commit ca CSDL_tron_goi.sql.
REM  Thu tren CSDL khac: ghep_tron_goi.cmd -TenCsdl QLDS_Thu   (ghi ra %%TEMP%%, khong dung file trong repo)
REM =====================================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ghep_tron_goi.ps1" %*
set "KQ=%errorlevel%"
if not "%KQ%"=="0" echo LOI: chua sinh duoc file (xem dong bao loi phia tren).
REM Nhap dup (cua so tu mo) thi dung lai cho doc ket qua
echo %cmdcmdline% | "%SystemRoot%\System32\find.exe" /i "%~f0" >nul && pause
exit /b %KQ%
