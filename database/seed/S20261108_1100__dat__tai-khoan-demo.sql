/* =========================================================================
   SEED: Tài khoản demo cho đăng nhập & phân quyền (Sprint 3)
   Tác giả  : Đạt
   Ngày     : 08/11/2026
   Bảng     : phanquyen.TaiKhoan, TaiKhoan_VaiTro (chủ: Đạt)

   Chuỗi băm BCrypt ($2a$12$...) trong script gốc không ai biết mật khẩu gốc -> đặt lại mật khẩu
   cho mọi tài khoản đang dùng chuỗi băm cũ, và thêm mỗi vai trò mới 1 tài khoản.

   MẬT KHẨU DEMO CHUNG: Demo@123   (chỉ dùng cho dữ liệu demo / kiểm thử, xem database/README.md)
   Chuỗi băm: PBKDF2-SHA256, 100.000 vòng, muối 16 byte riêng từng tài khoản
   (định dạng "PBKDF2$SHA256$<vòng>$<muối base64>$<băm base64>", BUS/Services/MatKhauHelper.cs).

   | Tài khoản | Vai trò        | Ghi chú                                   |
   |-----------|----------------|-------------------------------------------|
   | admin     | ADMIN          | toàn quyền                                 |
   | quanly    | QUAN_LY        | xem mọi phân hệ + báo cáo, không sửa       |
   | dieudo    | DIEU_DO        | lịch trình, biểu đồ, phong tỏa             |
   | truongga  | TRUONG_GA      | hạ tầng, soát vé                           |
   | nv_mai    | BAN_VE         | bán vé, hoàn vé, vận đơn (script gốc)      |
   | nv_dung   | KY_THUAT       | khám xe, bảo trì (script gốc)              |
   | nv_hung   | KY_THUAT       | (seed Bảo trì 24/10)                       |
   | nv_son    | KY_THUAT       | (seed Bảo trì 24/10)                       |
   | laptau    | LAP_TAU        | phương tiện, lập đoàn tàu                  |
   | nhansu    | NHAN_SU        | hồ sơ nhân viên                            |
   | phancong  | PHAN_CONG_KIP  | phân kíp, kiểm tra lên ban                 |
   | laitau    | LAI_TAU        | chỉ xem lịch trình, lịch ca (hồ sơ LT_001) |

   Thứ tự: sau migration V20261108_1000 (cần vai trò mới). Chạy lại được: chỉ thêm tài khoản còn thiếu,
   chỉ đặt lại mật khẩu cho tài khoản còn dùng chuỗi băm cũ ($2...).
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

IF COL_LENGTH(N'phanquyen.TaiKhoan', N'SoLanSaiLienTiep') IS NULL
   OR NOT EXISTS (SELECT 1 FROM phanquyen.VaiTro WHERE MaVaiTroCode = 'QUAN_LY')
BEGIN
    RAISERROR(N'Chưa chạy migration V20261108_1000__dat__phanquyen-dangnhap-quyen.sql.', 16, 1);
    SET NOEXEC ON;
END;
GO

BEGIN TRANSACTION;

DECLARE @TK TABLE (
    Ten VARCHAR(50) PRIMARY KEY, HoTen NVARCHAR(100), Email VARCHAR(100), MaNVCode VARCHAR(20) NULL,
    VaiTro VARCHAR(30), Bam VARCHAR(255));
INSERT INTO @TK VALUES
('admin',    N'Quản trị viên',               'admin@vr.com.vn',    NULL,     'ADMIN',         'PBKDF2$SHA256$100000$O/89bp37FZsfhrGddd6ZZg==$XKuaXz7Q8hR6fiAif4tcNihxDui8rJmPP8Ufr8OnT5o='),
('quanly',   N'Cán bộ quản lý',              'quanly@vr.com.vn',   NULL,     'QUAN_LY',       'PBKDF2$SHA256$100000$8yF6QfboSPVUW2HfEDIUgQ==$cKCnTGULCPuwpT/1vXb2yZXtlNa0hFk73RiZ+raXdGI='),
('dieudo',   N'Điều độ viên trung tâm',      'dieudo@vr.com.vn',   NULL,     'DIEU_DO',       'PBKDF2$SHA256$100000$Au/nIsvj3Y3p3nhLVYMl9w==$6uzIdMYEzVJLJVOwoSQZ7Z4Mi8u1JwaLPorvS9YlXEE='),
('truongga', N'Trực ban ga Hà Nội',          'truongga@vr.com.vn', NULL,     'TRUONG_GA',     'PBKDF2$SHA256$100000$w0Z4GQibHM04o7eXwbPaLQ==$y0Bjnb1eproYnY1gmdUntGRjeCnQCU79WXqdEH93jKY='),
('nv_mai',   N'Hoàng Thị Mai',               'mai.ht@vr.com.vn',   NULL,     'BAN_VE',        'PBKDF2$SHA256$100000$QnU+7EWjdddL3kcW4Sw6VA==$67hpIUNnl0cm4QH8aZhupYeI9x3Fpt7nTdzg/coAaio='),
('nv_dung',  N'Phạm Minh Dũng',              'dung.pm@vr.com.vn',  NULL,     'KY_THUAT',      'PBKDF2$SHA256$100000$xwHRn71aOA/lMQW9Qy86ag==$IGKRGK2h9xKIGobiftYEJkCi1tmV5WoRmNOCbqtmYrw='),
('nv_hung',  N'Trần Quốc Hưng',              'hung.tq@vr.com.vn',                NULL,     'KY_THUAT',      'PBKDF2$SHA256$100000$UT8x1p+cwQcvok85fSC2/A==$qmVWOH54K8DU0cPC/oL7jq1NyV7Gfx/4ywp/nAS7PCk='),
('nv_son',   N'Lê Thanh Sơn',                'son.lt@vr.com.vn',                 NULL,     'KY_THUAT',      'PBKDF2$SHA256$100000$mERyIKnCN3gkIUFeM6V0Qg==$ITZ3ZUVOg3jFA4QtKLVhxeIQyD6xNe7B9hzscGVIIzY='),
('laptau',   N'Quản đốc lập tàu',            'laptau@vr.com.vn',   NULL,     'LAP_TAU',       'PBKDF2$SHA256$100000$XlafquUqunG2OT+WYWhEBA==$Fq9m3vvQ/hgMsFIYwrzxx1PqqZscWSaemwF4wVSDj60='),
('nhansu',   N'Cán bộ nhân sự',              'nhansu@vr.com.vn',   NULL,     'NHAN_SU',       'PBKDF2$SHA256$100000$q4unWyeIrb3HkAfJhz6QfQ==$7aDAWgGmYYw3YaKbuV0OQoNmjOeQIZDTL6+nAeR1Q9A='),
('phancong', N'Quản đốc kíp lái',            'phancong@vr.com.vn', NULL,     'PHAN_CONG_KIP', 'PBKDF2$SHA256$100000$Xbl7wPvMohf/QL6AUa6ZHg==$kWgcnaa4hrQe6YvNSjJ2rxXBZL2z26TzKn4NxGvMZJ0='),
('laitau',   N'Nguyễn Văn An',               NULL,                 'LT_001', 'LAI_TAU',       'PBKDF2$SHA256$100000$oQjwsBVZnX1Y8vGcw49uag==$7UTeUxo/kGSGXtU8VrL+O6xwdl26p6piJt9ECYCbzsQ=');

-- 1. Tài khoản còn thiếu. KHÔNG tạo nv_hung / nv_son: seed Bảo trì S20261024_1100 thấy nv_hung đã có
--    thì bỏ qua cả file -> để seed đó tạo (kèm hồ sơ nhân viên + phiếu), file này chỉ đặt lại mật khẩu.
INSERT INTO phanquyen.TaiKhoan (TenDangNhap, MatKhauHash, HoTenHienThi, Email, MaNhanVien)
SELECT t.Ten, t.Bam, t.HoTen, t.Email, nv.MaNhanVien
FROM @TK t
LEFT JOIN nhansu.NhanVien nv ON nv.MaNVCode = t.MaNVCode
WHERE t.Ten NOT IN ('nv_hung', 'nv_son')
  AND NOT EXISTS (SELECT 1 FROM phanquyen.TaiKhoan x WHERE x.TenDangNhap = t.Ten);

-- 2. Đặt lại mật khẩu demo cho tài khoản còn dùng chuỗi băm BCrypt cũ của script gốc
UPDATE tk SET MatKhauHash = t.Bam, SoLanSaiLienTiep = 0, KhoaDenLuc = NULL, PhaiDoiMatKhau = 0
FROM phanquyen.TaiKhoan tk JOIN @TK t ON t.Ten = tk.TenDangNhap
WHERE tk.MatKhauHash LIKE '$2%';

-- 3. Vai trò còn thiếu
INSERT INTO phanquyen.TaiKhoan_VaiTro (MaTaiKhoan, MaVaiTro)
SELECT tk.MaTaiKhoan, vt.MaVaiTro
FROM @TK t
JOIN phanquyen.TaiKhoan tk ON tk.TenDangNhap = t.Ten
JOIN phanquyen.VaiTro vt ON vt.MaVaiTroCode = t.VaiTro
WHERE NOT EXISTS (SELECT 1 FROM phanquyen.TaiKhoan_VaiTro x WHERE x.MaTaiKhoan = tk.MaTaiKhoan AND x.MaVaiTro = vt.MaVaiTro);

COMMIT TRANSACTION;
GO

SELECT tk.TenDangNhap, tk.HoTenHienThi,
       STRING_AGG(vt.MaVaiTroCode, ', ') AS VaiTro,
       IIF(tk.MatKhauHash LIKE 'PBKDF2$%', 'PBKDF2', 'CU') AS KieuBam
FROM phanquyen.TaiKhoan tk
LEFT JOIN phanquyen.TaiKhoan_VaiTro tv ON tv.MaTaiKhoan = tk.MaTaiKhoan
LEFT JOIN phanquyen.VaiTro vt ON vt.MaVaiTro = tv.MaVaiTro
GROUP BY tk.MaTaiKhoan, tk.TenDangNhap, tk.HoTenHienThi, tk.MatKhauHash
ORDER BY tk.MaTaiKhoan;
GO
SET NOEXEC OFF;
GO
