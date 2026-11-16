/* =====================================================================
   CSDL TRỌN GÓI — dựng lại toàn bộ CSDL QuanLyDuongSatV2 bằng MỘT file
   File SINH TỰ ĐỘNG bằng database\ghep_tron_goi.cmd — KHÔNG sửa tay.
   Gồm: 000_tao_csdl_goc.sql + 16 file theo thu_tu_chay.txt (migration, seed, kiểm tra dữ liệu).

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

PRINT N'--- [0/17] Sao lưu CSDL đang có ---';
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

PRINT N'--- [1/17] 000_tao_csdl_goc.sql (xoá + tạo lại CSDL, dữ liệu gốc) ---';
GO
USE master;
GO

IF DB_ID(N'QuanLyDuongSatV2') IS NOT NULL
BEGIN
    ALTER DATABASE QuanLyDuongSatV2 SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QuanLyDuongSatV2;
END;
GO

CREATE DATABASE QuanLyDuongSatV2
COLLATE Vietnamese_CI_AS;
GO

USE QuanLyDuongSatV2;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

/* =========================================================================
   1. KHỞI TẠO 9 SCHEMAS
   ========================================================================= */
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'danhmuc')    EXEC('CREATE SCHEMA danhmuc');
GO
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'hatang')     EXEC('CREATE SCHEMA hatang');
GO
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'nhansu')     EXEC('CREATE SCHEMA nhansu');
GO
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'phanquyen')  EXEC('CREATE SCHEMA phanquyen');
GO
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'phuongtien') EXEC('CREATE SCHEMA phuongtien');
GO
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'vanhanh')    EXEC('CREATE SCHEMA vanhanh');
GO
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'vantai')     EXEC('CREATE SCHEMA vantai');
GO
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'baotri')     EXEC('CREATE SCHEMA baotri');
GO
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'kiemtoan')   EXEC('CREATE SCHEMA kiemtoan');
GO

/* =========================================================================
   2. KHỞI TẠO CÁC BẢNG (40 BẢNG)
   ========================================================================= */


-- SCHEMA: danhmuc


CREATE TABLE danhmuc.DanhMuc (
    MaDanhMuc           INT IDENTITY(1,1) PRIMARY KEY,
    NhomDanhMuc         VARCHAR(50)     NOT NULL,
    MaCode              VARCHAR(50)     NOT NULL,
    TenHienThi          NVARCHAR(100)   NOT NULL,
    ThuTuSapXep         INT             NOT NULL DEFAULT 1,
    GhiChu              NVARCHAR(255)   NULL,
    DangKhaiThac        BIT             NOT NULL DEFAULT 1,
    NgayTao             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_DanhMuc_Nhom_Code UNIQUE (NhomDanhMuc, MaCode)
);
GO


-- SCHEMA: hatang


CREATE TABLE hatang.TuyenDuong (
    MaTuyen             INT IDENTITY(1,1) PRIMARY KEY,
    MaTuyenCode         VARCHAR(20)     NOT NULL UNIQUE,
    TenTuyen            NVARCHAR(100)   NOT NULL,
    TongChieuDaiKm      DECIMAL(7,2)    NOT NULL CHECK (TongChieuDaiKm > 0),
    KhoRayMm            INT             NOT NULL DEFAULT 1000,
    DangKhaiThac        BIT             NOT NULL DEFAULT 1,
    NgayTao             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE hatang.Ga (
    MaGa                INT IDENTITY(1,1) PRIMARY KEY,
    MaTuyen             INT             NOT NULL,
    MaGaCode            VARCHAR(10)     NOT NULL UNIQUE,
    TenGa               NVARCHAR(100)   NOT NULL,
    LyTrinhKm           DECIMAL(7,2)    NOT NULL CHECK (LyTrinhKm >= 0),
    TinhThanh           NVARCHAR(100)   NOT NULL,
    HangGa              VARCHAR(20)     NOT NULL DEFAULT 'HANG_3'
        CHECK (HangGa IN ('HANG_1', 'HANG_2', 'HANG_3')),
    CoCauQuay           BIT             NOT NULL DEFAULT 0,
    DangKhaiThac        BIT             NOT NULL DEFAULT 1,
    NgayTao             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    NguoiTao            NVARCHAR(50)    NOT NULL DEFAULT N'admin',
    NgayCapNhat         DATETIME2       NULL,
    NguoiCapNhat        NVARCHAR(50)    NULL,
    CONSTRAINT FK_Ga_TuyenDuong FOREIGN KEY (MaTuyen) REFERENCES hatang.TuyenDuong(MaTuyen),
    CONSTRAINT UQ_Ga_Tuyen_LyTrinh UNIQUE (MaTuyen, LyTrinhKm)
);
GO

CREATE TABLE hatang.DuongRayGa (
    MaDuongRay          INT IDENTITY(1,1) PRIMARY KEY,
    MaGa                INT             NOT NULL,
    SoHieuDuong         INT             NOT NULL,
    LoaiDuong           VARCHAR(30)     NOT NULL
        CHECK (LoaiDuong IN ('CHINH_TUYEN', 'DUONG_TRANH', 'BOC_DO')),
    ChieuDaiHuuDungM    INT             NOT NULL DEFAULT 450 CHECK (ChieuDaiHuuDungM > 0),
    CoKeGa              BIT             NOT NULL DEFAULT 1,
    TrangThai           VARCHAR(20)     NOT NULL DEFAULT 'TRONG'
        CHECK (TrangThai IN ('TRONG', 'CO_TAU', 'BAO_TRI')),
    NgayTao             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    NguoiTao            NVARCHAR(50)    NOT NULL DEFAULT N'admin',
    NgayCapNhat         DATETIME2       NULL,
    NguoiCapNhat        NVARCHAR(50)    NULL,
    CONSTRAINT FK_DuongRay_Ga FOREIGN KEY (MaGa) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT UQ_Ga_SoHieu UNIQUE (MaGa, SoHieuDuong)
);
GO

CREATE TABLE hatang.KhuGian (
    MaKhuGian           INT IDENTITY(1,1) PRIMARY KEY,
    MaGaDau             INT             NOT NULL,
    MaGaCuoi            INT             NOT NULL,
    CuLyKm              DECIMAL(5,2)    NOT NULL CHECK (CuLyKm > 0),
    TocDoToiDaKhach     INT             NOT NULL DEFAULT 80 CHECK (TocDoToiDaKhach > 0),
    TocDoToiDaHang      INT             NOT NULL DEFAULT 50 CHECK (TocDoToiDaHang > 0),
    DoDocPermil         DECIMAL(4,1)    NOT NULL DEFAULT 0.0 CHECK (DoDocPermil >= 0),
    CanDauMayDay        BIT             NOT NULL DEFAULT 0,
    TrangThai           VARCHAR(20)     NOT NULL DEFAULT 'RONG'
        CHECK (TrangThai IN ('RONG', 'CO_TAU', 'PHONG_TOA')),
    CONSTRAINT FK_KhuGian_GaDau FOREIGN KEY (MaGaDau) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT FK_KhuGian_GaCuoi FOREIGN KEY (MaGaCuoi) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT CK_KhuGian_KhacGa CHECK (MaGaDau <> MaGaCuoi),
    CONSTRAINT CK_KhuGian_ThuTuChuan CHECK (MaGaDau < MaGaCuoi),
    CONSTRAINT UQ_KhuGian UNIQUE (MaGaDau, MaGaCuoi)
);
GO

CREATE TABLE hatang.DuongNgang (
    MaDuongNgang        INT IDENTITY(1,1) PRIMARY KEY,
    MaKhuGian           INT             NOT NULL,
    LyTrinhKm           DECIMAL(7,2)    NOT NULL CHECK (LyTrinhKm >= 0),
    LoaiDuongNgang      VARCHAR(30)     NOT NULL
        CHECK (LoaiDuongNgang IN ('CO_NGUOI_GAC', 'TU_DONG', 'BIEN_BAO')),
    TenDuongBoGiaoCat   NVARCHAR(150)   NOT NULL,
    LaDiemDen           BIT             NOT NULL DEFAULT 0,
    CONSTRAINT FK_DuongNgang_KhuGian FOREIGN KEY (MaKhuGian) REFERENCES hatang.KhuGian(MaKhuGian)
);
GO


-- SCHEMA: nhansu


CREATE TABLE nhansu.NhanVien (
    MaNhanVien          INT IDENTITY(1,1) PRIMARY KEY,
    MaNVCode            VARCHAR(20)     NOT NULL UNIQUE,
    HoTen               NVARCHAR(100)   NOT NULL,
    SoDienThoai         VARCHAR(20)     NOT NULL,
    ChucDanh            NVARCHAR(50)    NOT NULL,
    HangBangLai         VARCHAR(10)     NULL,
    HanKhamSucKhoe      DATE            NOT NULL,
    DonViChuQuan        NVARCHAR(100)   NOT NULL,
    TrangThai           VARCHAR(20)     NOT NULL DEFAULT 'SAN_SANG'
        CHECK (TrangThai IN ('SAN_SANG', 'DANG_LAM', 'NGHI_NGOI', 'DA_NGHI_VIEC')),
    NgayTao             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
);
GO


-- SCHEMA: phanquyen


CREATE TABLE phanquyen.VaiTro (
    MaVaiTro            INT IDENTITY(1,1) PRIMARY KEY,
    MaVaiTroCode        VARCHAR(30)     NOT NULL UNIQUE,
    TenVaiTro           NVARCHAR(100)   NOT NULL,
    MoTa                NVARCHAR(255)   NULL,
    KichHoat            BIT             NOT NULL DEFAULT 1,
    NgayTao             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE phanquyen.Quyen (
    MaQuyen             INT IDENTITY(1,1) PRIMARY KEY,
    MaQuyenCode         VARCHAR(50)     NOT NULL UNIQUE,
    TenQuyen            NVARCHAR(100)   NOT NULL,
    NhomQuyen           VARCHAR(50)     NOT NULL,
    MoTa                NVARCHAR(255)   NULL
);
GO

CREATE TABLE phanquyen.TaiKhoan (
    MaTaiKhoan          INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap         VARCHAR(50)     NOT NULL UNIQUE,
    MatKhauHash         VARCHAR(255)    NOT NULL,
    HoTenHienThi        NVARCHAR(100)   NOT NULL,
    Email               VARCHAR(100)    NULL,
    MaNhanVien          INT             NULL,
    DangHoatDong        BIT             NOT NULL DEFAULT 1,
    LanDangNhapCuoi     DATETIME2       NULL,
    NgayTao             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_TaiKhoan_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES nhansu.NhanVien(MaNhanVien)
);
GO

CREATE TABLE phanquyen.TaiKhoan_VaiTro (
    MaTaiKhoan          INT             NOT NULL,
    MaVaiTro            INT             NOT NULL,
    NgayCap             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    NguoiCap            INT             NULL,
    CONSTRAINT PK_TaiKhoan_VaiTro PRIMARY KEY (MaTaiKhoan, MaVaiTro),
    CONSTRAINT FK_TKVT_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan) ON DELETE CASCADE,
    CONSTRAINT FK_TKVT_VaiTro FOREIGN KEY (MaVaiTro) REFERENCES phanquyen.VaiTro(MaVaiTro) ON DELETE CASCADE,
    CONSTRAINT FK_TKVT_NguoiCap FOREIGN KEY (NguoiCap) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan)
);
GO

CREATE TABLE phanquyen.VaiTro_Quyen (
    MaVaiTro            INT             NOT NULL,
    MaQuyen             INT             NOT NULL,
    CONSTRAINT PK_VaiTro_Quyen PRIMARY KEY (MaVaiTro, MaQuyen),
    CONSTRAINT FK_VTQ_VaiTro FOREIGN KEY (MaVaiTro) REFERENCES phanquyen.VaiTro(MaVaiTro) ON DELETE CASCADE,
    CONSTRAINT FK_VTQ_Quyen FOREIGN KEY (MaQuyen) REFERENCES phanquyen.Quyen(MaQuyen) ON DELETE CASCADE
);
GO

CREATE TABLE phanquyen.LichSuDangNhap (
    MaLichSu            BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaTaiKhoan          INT             NULL,
    TenDangNhap         VARCHAR(50)     NOT NULL,
    ThoiDiemDangNhap    DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    DiaChiIP            VARCHAR(45)     NULL,
    ThietBi             NVARCHAR(255)   NULL,
    ThanhCong           BIT             NOT NULL,
    LyDoThatBai         NVARCHAR(255)   NULL,
    CONSTRAINT FK_LSDN_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan)
);
GO


-- SCHEMA: phuongtien


CREATE TABLE phuongtien.DongDauMay (
    MaDongDauMay        INT IDENTITY(1,1) PRIMARY KEY,
    MaDongCode          VARCHAR(20)     NOT NULL UNIQUE,
    NhaSanXuat          NVARCHAR(100)   NOT NULL,
    CongSuatHP          INT             NOT NULL CHECK (CongSuatHP > 0),
    TocDoToiDaKmh       INT             NOT NULL CHECK (TocDoToiDaKmh > 0),
    SucKeoToiDaTan      DECIMAL(6,1)    NOT NULL CHECK (SucKeoToiDaTan > 0),
    DungTichBonDauLit   INT             NOT NULL CHECK (DungTichBonDauLit > 0),
    TrongLuongTan       DECIMAL(5,1)    NOT NULL CHECK (TrongLuongTan > 0),
    ChieuDaiM           DECIMAL(4,1)    NOT NULL CHECK (ChieuDaiM > 0)
);
GO

CREATE TABLE phuongtien.DauMay (
    MaDauMay            INT IDENTITY(1,1) PRIMARY KEY,
    SoHieuDauMay        VARCHAR(20)     NOT NULL UNIQUE,
    MaDongDauMay        INT             NOT NULL,
    DonViQuanLy         NVARCHAR(50)    NOT NULL,
    NamSanXuat          INT             NOT NULL CHECK (NamSanXuat >= 1970),
    SoKmTichLuy         DECIMAL(9,1)    NOT NULL DEFAULT 0.0 CHECK (SoKmTichLuy >= 0),
    TrangThai           VARCHAR(20)     NOT NULL DEFAULT 'SAN_SANG'
        CHECK (TrangThai IN ('SAN_SANG', 'DANG_CHAY', 'BAO_DUONG')),
    CONSTRAINT FK_DauMay_Dong FOREIGN KEY (MaDongDauMay) REFERENCES phuongtien.DongDauMay(MaDongDauMay)
);
GO

CREATE TABLE phuongtien.ChungLoaiToa (
    MaChungLoai         INT IDENTITY(1,1) PRIMARY KEY,
    MaChungLoaiCode     VARCHAR(20)     NOT NULL UNIQUE,
    TenMoTa             NVARCHAR(100)   NOT NULL,
    ChieuDaiChuanM      DECIMAL(4,1)    NOT NULL DEFAULT 20.0 CHECK (ChieuDaiChuanM > 0),
    SoTruc              INT             NOT NULL DEFAULT 4 CHECK (SoTruc IN (2, 4, 6)),
    SucChuaChuan        INT             NOT NULL DEFAULT 0 CHECK (SucChuaChuan >= 0)
);
GO

CREATE TABLE phuongtien.ToaXe (
    MaToaXe             INT IDENTITY(1,1) PRIMARY KEY,
    SoHieuToaXe         VARCHAR(20)     NOT NULL UNIQUE,
    MaChungLoai         INT             NOT NULL,
    TuTrongTan          DECIMAL(5,2)    NOT NULL CHECK (TuTrongTan > 0),
    TaiTrongToiDaTan    DECIMAL(5,2)    NOT NULL CHECK (TaiTrongToiDaTan >= 0),
    TrangThai           VARCHAR(20)     NOT NULL DEFAULT 'SAN_SANG'
        CHECK (TrangThai IN ('SAN_SANG', 'DA_NOI', 'BAO_DUONG')),
    CONSTRAINT FK_ToaXe_ChungLoai FOREIGN KEY (MaChungLoai) REFERENCES phuongtien.ChungLoaiToa(MaChungLoai)
);
GO


-- SCHEMA: vanhanh


CREATE TABLE vanhanh.DinhMucVanToc (
    MaLoaiTau           VARCHAR(20)     NOT NULL PRIMARY KEY,
    TenLoaiTau          NVARCHAR(100)   NOT NULL,
    VanTocDinhMucKmh    DECIMAL(5,2)    NOT NULL CHECK (VanTocDinhMucKmh > 0),
    MoTaQuyChuan        NVARCHAR(255)   NULL,
    MauNhanDien         VARCHAR(20)     NULL,
    ThuTuHienThi        INT             NOT NULL DEFAULT 1,
    DangApDung          BIT             NOT NULL DEFAULT 1,
    NgayTao             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    NguoiTao            NVARCHAR(50)    NOT NULL DEFAULT 'admin',
    NgayCapNhat         DATETIME2       NULL,
    NguoiCapNhat        NVARCHAR(50)    NULL
);
GO
CREATE TABLE vanhanh.MacTauMau (
    MaMacTau            INT IDENTITY(1,1) PRIMARY KEY,
    SoHieuMacTau        VARCHAR(20)     NOT NULL UNIQUE,
    LoaiTau             VARCHAR(20)     NOT NULL,
    HuongChay           VARCHAR(15)     NOT NULL
        CHECK (HuongChay IN ('BAC_NAM', 'NAM_BAC')),
    MaGaDi              INT             NOT NULL,
    MaGaDen             INT             NOT NULL,
    ThoiGianChuanGio    DECIMAL(4,1)    NOT NULL CHECK (ThoiGianChuanGio > 0),
    MucUuTien           INT             NOT NULL DEFAULT 1 CHECK (MucUuTien >= 1),
    DangKhaiThac        BIT             NOT NULL DEFAULT 1,
    NgayTao             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    NguoiTao            NVARCHAR(50)    NOT NULL DEFAULT 'admin',
    NgayCapNhat         DATETIME2,
    NguoiCapNhat        NVARCHAR(50),
    CONSTRAINT FK_MacTau_GaDi FOREIGN KEY (MaGaDi) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT FK_MacTau_GaDen FOREIGN KEY (MaGaDen) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT FK_MacTau_LoaiTau FOREIGN KEY (LoaiTau) REFERENCES vanhanh.DinhMucVanToc(MaLoaiTau),
    CONSTRAINT CK_MacTau_KhacGa CHECK (MaGaDi <> MaGaDen)
);
GO

CREATE TABLE vanhanh.ChuyenTau (
    MaChuyenTau         INT IDENTITY(1,1) PRIMARY KEY,
    MaMacTau            INT             NOT NULL,
    NgayXuatPhat        DATE            NOT NULL,
    GioXuatPhatKH       DATETIME2       NOT NULL,
    GioVeDichKH         DATETIME2       NOT NULL,
    GioXuatPhatTT       DATETIME2       NULL,
    GioVeDichTT         DATETIME2       NULL,
    SoPhutTreLuyKe      INT             NOT NULL DEFAULT 0,
    LyDoHuy             NVARCHAR(255)   NULL,
    TrangThai           VARCHAR(20)     NOT NULL DEFAULT 'DA_LEN_LICH'
        CHECK (TrangThai IN ('DA_LEN_LICH', 'SAN_SANG', 'DANG_CHAY',
                             'TRE_GIO', 'HOAN_THANH', 'DA_HUY')),
    PhienBan            ROWVERSION      NOT NULL,
    CONSTRAINT FK_ChuyenTau_MacTau FOREIGN KEY (MaMacTau) REFERENCES vanhanh.MacTauMau(MaMacTau),
    CONSTRAINT CK_ChuyenTau_ThoiGianKH CHECK (GioVeDichKH > GioXuatPhatKH),
    CONSTRAINT UQ_MacTau_Ngay UNIQUE (MaMacTau, NgayXuatPhat)
);
GO

CREATE TABLE vanhanh.LichDungGa (
    MaDiemDung          INT IDENTITY(1,1) PRIMARY KEY,
    MaChuyenTau         INT             NOT NULL,
    MaGa                INT             NOT NULL,
    ThuTuDung           INT             NOT NULL CHECK (ThuTuDung >= 1),
    GioDenKeHoach       DATETIME2       NOT NULL,
    GioDiKeHoach        DATETIME2       NOT NULL,
    GioDenThucTe        DATETIME2       NULL,
    GioDiThucTe         DATETIME2       NULL,
    MaDuongRay          INT             NULL,
    LaDiemTranh         BIT             NOT NULL DEFAULT 0,
    CONSTRAINT FK_LichDung_ChuyenTau FOREIGN KEY (MaChuyenTau) REFERENCES vanhanh.ChuyenTau(MaChuyenTau),
    CONSTRAINT FK_LichDung_Ga FOREIGN KEY (MaGa) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT FK_LichDung_DuongRay FOREIGN KEY (MaDuongRay) REFERENCES hatang.DuongRayGa(MaDuongRay),
    CONSTRAINT CK_LichDung_ThoiGian CHECK (GioDiKeHoach >= GioDenKeHoach),
    CONSTRAINT UQ_ChuyenTau_ThuTu UNIQUE (MaChuyenTau, ThuTuDung)
);
GO

CREATE TABLE vanhanh.DoanTau (
    MaDoanTau           INT IDENTITY(1,1) PRIMARY KEY,
    MaChuyenTau         INT             NOT NULL UNIQUE,
    MaDauMayChinh       INT             NOT NULL,
    MaDauMayDay         INT             NULL,
    TongSoToa           INT             NOT NULL CHECK (TongSoToa > 0 AND TongSoToa <= 20),
    TongChieuDaiM       DECIMAL(6,1)    NOT NULL CHECK (TongChieuDaiM > 0 AND TongChieuDaiM <= 450.0),
    TongTrongLuongTan   DECIMAL(7,2)    NOT NULL CHECK (TongTrongLuongTan > 0),
    DaDuyetAnToan       BIT             NOT NULL DEFAULT 0,
    PhienBan            ROWVERSION      NOT NULL,
    CONSTRAINT FK_DoanTau_ChuyenTau FOREIGN KEY (MaChuyenTau) REFERENCES vanhanh.ChuyenTau(MaChuyenTau),
    CONSTRAINT FK_DoanTau_DauMayChinh FOREIGN KEY (MaDauMayChinh) REFERENCES phuongtien.DauMay(MaDauMay),
    CONSTRAINT FK_DoanTau_DauMayDay FOREIGN KEY (MaDauMayDay) REFERENCES phuongtien.DauMay(MaDauMay)
);
GO

CREATE TABLE vanhanh.ChiTietDoanTau (
    MaChiTiet           INT IDENTITY(1,1) PRIMARY KEY,
    MaDoanTau           INT             NOT NULL,
    MaToaXe             INT             NOT NULL,
    ThuTuNoi            INT             NOT NULL CHECK (ThuTuNoi >= 1),
    CONSTRAINT FK_CTDoanTau_DoanTau FOREIGN KEY (MaDoanTau) REFERENCES vanhanh.DoanTau(MaDoanTau) ON DELETE CASCADE,
    CONSTRAINT FK_CTDoanTau_ToaXe FOREIGN KEY (MaToaXe) REFERENCES phuongtien.ToaXe(MaToaXe),
    CONSTRAINT UQ_DoanTau_ThuTuNoi UNIQUE (MaDoanTau, ThuTuNoi),
    CONSTRAINT UQ_DoanTau_ToaXe UNIQUE (MaDoanTau, MaToaXe)
);
GO

CREATE TABLE vanhanh.NhatKyChamGio (
    MaSuCo              INT IDENTITY(1,1) PRIMARY KEY,
    MaChuyenTau         INT             NOT NULL,
    MaGa                INT             NULL,
    MaKhuGian           INT             NULL,
    LoaiSuCo            VARCHAR(50)     NOT NULL
        CHECK (LoaiSuCo IN ('HONG_DAU_MAY', 'BAO_LU', 'CHO_TRANH_TAU',
                            'TAI_NAN_DUONG_NGANG', 'SU_CO_TOA_XE', 'KHAC')),
    SoPhutCham          INT             NOT NULL CHECK (SoPhutCham > 0),
    GhiChuXuLy          NVARCHAR(MAX)   NULL,
    ThoiDiemBaoCao      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ChamGio_ChuyenTau FOREIGN KEY (MaChuyenTau) REFERENCES vanhanh.ChuyenTau(MaChuyenTau),
    CONSTRAINT FK_ChamGio_Ga FOREIGN KEY (MaGa) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT FK_ChamGio_KhuGian FOREIGN KEY (MaKhuGian) REFERENCES hatang.KhuGian(MaKhuGian)
);
GO

CREATE TABLE vanhanh.LenhPhongToa (
    MaLenhPhongToa      INT IDENTITY(1,1) PRIMARY KEY,
    MaKhuGian           INT             NOT NULL,
    ThoiGianBatDau      DATETIME2       NOT NULL,
    ThoiGianKetThuc     DATETIME2       NOT NULL,
    LyDo                NVARCHAR(255)   NOT NULL,
    TocDoHanCheKmh      INT             NOT NULL DEFAULT 0 CHECK (TocDoHanCheKmh >= 0),
    MaNguoiDuyet        INT             NOT NULL,
    ThoiDiemTao         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_PhongToa_ThoiGian CHECK (ThoiGianKetThuc > ThoiGianBatDau),
    CONSTRAINT FK_PhongToa_KhuGian FOREIGN KEY (MaKhuGian) REFERENCES hatang.KhuGian(MaKhuGian),
    CONSTRAINT FK_PhongToa_NguoiDuyet FOREIGN KEY (MaNguoiDuyet) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan)
);
GO


-- CÁC BẢNG NHÂN SỰ GẮN VỚI CHUYẾN TÀU


CREATE TABLE nhansu.PhanCongKipLai (
    MaPhanCong          INT IDENTITY(1,1) PRIMARY KEY,
    MaChuyenTau         INT             NOT NULL,
    MaLaiTau            INT             NOT NULL,
    MaPhuLai            INT             NOT NULL,
    MaTruongTau         INT             NOT NULL,
    MaGaNhanBan         INT             NOT NULL,
    MaGaBanGiao         INT             NOT NULL,
    TrangThai           VARCHAR(20)     NOT NULL DEFAULT 'DA_PHAN_CONG'
        CHECK (TrangThai IN ('DA_PHAN_CONG', 'DANG_THUC_HIEN', 'HOAN_THANH')),
    CONSTRAINT FK_KipLai_ChuyenTau FOREIGN KEY (MaChuyenTau) REFERENCES vanhanh.ChuyenTau(MaChuyenTau),
    CONSTRAINT FK_KipLai_LaiTau FOREIGN KEY (MaLaiTau) REFERENCES nhansu.NhanVien(MaNhanVien),
    CONSTRAINT FK_KipLai_PhuLai FOREIGN KEY (MaPhuLai) REFERENCES nhansu.NhanVien(MaNhanVien),
    CONSTRAINT FK_KipLai_TruongTau FOREIGN KEY (MaTruongTau) REFERENCES nhansu.NhanVien(MaNhanVien),
    CONSTRAINT FK_KipLai_GaNhan FOREIGN KEY (MaGaNhanBan) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT FK_KipLai_GaBanGiao FOREIGN KEY (MaGaBanGiao) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT CK_KipLai_KhacNhanVien CHECK (MaLaiTau <> MaPhuLai AND MaLaiTau <> MaTruongTau AND MaPhuLai <> MaTruongTau)
);
GO

CREATE TABLE nhansu.KiemTraSucKhoe (
    MaKiemTra           INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien          INT             NOT NULL,
    MaChuyenTau         INT             NOT NULL,
    NongDoConMgL        DECIMAL(3,2)    NOT NULL CHECK (NongDoConMgL >= 0),
    SoGioNghiTruocCa    DECIMAL(4,1)    NOT NULL CHECK (SoGioNghiTruocCa >= 0),
    -- Tự động đánh giá: cồn = 0 và nghỉ >= 8h
    DuDieuKien          AS (
        CASE 
            WHEN NongDoConMgL = 0.00 AND SoGioNghiTruocCa >= 8.0 
            THEN CAST(1 AS BIT) 
            ELSE CAST(0 AS BIT) 
        END
    ) PERSISTED,
    ThoiDiemKiemTra     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_KiemTra_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES nhansu.NhanVien(MaNhanVien),
    CONSTRAINT FK_KiemTra_ChuyenTau FOREIGN KEY (MaChuyenTau) REFERENCES vanhanh.ChuyenTau(MaChuyenTau)
);
GO


-- SCHEMA: vantai


CREATE TABLE vantai.KhachHang (
    MaKhachHang         INT IDENTITY(1,1) PRIMARY KEY,
    HoTen               NVARCHAR(100)   NOT NULL,
    SoCCCD              VARCHAR(20)     NOT NULL UNIQUE,
    SoDienThoai         VARCHAR(20)     NOT NULL,
    Email               VARCHAR(100)    NULL,
    NgaySinh            DATE            NULL,
    LoaiKhach           VARCHAR(20)     NOT NULL DEFAULT 'THUONG'
        CHECK (LoaiKhach IN ('THUONG', 'SINH_VIEN', 'NGUOI_GIA', 'TRE_EM')),
    NgayTao             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE vantai.DonDatVe (
    MaDonVe             INT IDENTITY(1,1) PRIMARY KEY,
    MaPNR               VARCHAR(20)     NOT NULL UNIQUE,
    MaKhachHang         INT             NOT NULL,
    SoLuongVe           INT             NOT NULL CHECK (SoLuongVe > 0),
    TongTien            DECIMAL(12,2)   NOT NULL CHECK (TongTien >= 0),
    HinhThucTT          VARCHAR(30)     NOT NULL DEFAULT 'TIEN_MAT'
        CHECK (HinhThucTT IN ('TIEN_MAT', 'CHUYEN_KHOAN', 'THE_NGAN_HANG', 'VI_DIEN_TU')),
    TrangThaiTT         VARCHAR(20)     NOT NULL DEFAULT 'DA_THANH_TOAN'
        CHECK (TrangThaiTT IN ('DA_THANH_TOAN', 'DA_HOAN_TIEN')),
    MaNhanVienBan       INT             NOT NULL,
    ThoiDiemTao         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    PhienBan            ROWVERSION      NOT NULL,
    CONSTRAINT FK_DonVe_KhachHang FOREIGN KEY (MaKhachHang) REFERENCES vantai.KhachHang(MaKhachHang),
    CONSTRAINT FK_DonVe_NhanVienBan FOREIGN KEY (MaNhanVienBan) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan)
);
GO

CREATE TABLE vantai.ToaXeKhach (
    MaToaXeKhach        INT IDENTITY(1,1) PRIMARY KEY,
    MaChuyenTau         INT             NOT NULL,
    NhanHieuToa         NVARCHAR(20)    NOT NULL,
    LoaiToa             VARCHAR(20)     NOT NULL
        CHECK (LoaiToa IN ('NC', 'NML', 'BN', 'AN')),
    ThuTuToa            INT             NOT NULL CHECK (ThuTuToa >= 1),
    SucChua             INT             NOT NULL CHECK (SucChua > 0),
    CONSTRAINT FK_ToaXeKhach_ChuyenTau FOREIGN KEY (MaChuyenTau) REFERENCES vanhanh.ChuyenTau(MaChuyenTau),
    CONSTRAINT UQ_ChuyenTau_ThuTuToa UNIQUE (MaChuyenTau, ThuTuToa)
);
GO

CREATE TABLE vantai.ChoNgoi (
    MaChoNgoi           INT IDENTITY(1,1) PRIMARY KEY,
    MaToaXeKhach        INT             NOT NULL,
    SoGhe               INT             NOT NULL CHECK (SoGhe > 0),
    TangGiuong          INT             NULL CHECK (TangGiuong IN (1, 2, 3)),
    LaGhePhu            BIT             NOT NULL DEFAULT 0,
    CONSTRAINT FK_ChoNgoi_ToaXeKhach FOREIGN KEY (MaToaXeKhach) REFERENCES vantai.ToaXeKhach(MaToaXeKhach),
    CONSTRAINT UQ_Toa_SoGhe UNIQUE (MaToaXeKhach, SoGhe)
);
GO

CREATE TABLE vantai.BieuCuoc (
    MaBieuCuoc          INT IDENTITY(1,1) PRIMARY KEY,
    LoaiToa             VARCHAR(20)     NOT NULL UNIQUE
        CHECK (LoaiToa IN ('NC', 'NML', 'BN', 'AN')),
    DonGiaMoiKm         DECIMAL(8,2)    NOT NULL CHECK (DonGiaMoiKm > 0),
    GiaVeSan            DECIMAL(10,2)   NOT NULL CHECK (GiaVeSan > 0),
    PhuThuTangGiuong    DECIMAL(8,2)    NOT NULL DEFAULT 0.0 CHECK (PhuThuTangGiuong >= 0)
);
GO

-- Bảng giá cố định theo cặp ga (OD) cho tàu SE/Nhanh
-- Tàu SE: Giá vé cố định theo chặng, không tính theo km (giống vé xe bus liên tỉnh)
-- Tàu Chợ (TAU_CHO): Vẫn dùng BieuCuoc tính theo km ở trên
CREATE TABLE vantai.BangGiaCoDinh (
    MaBangGia           INT IDENTITY(1,1) PRIMARY KEY,
    LoaiToa             VARCHAR(20)     NOT NULL
        CHECK (LoaiToa IN ('NC', 'NML', 'BN', 'AN')),
    MaGaDi              INT             NOT NULL,
    MaGaDen             INT             NOT NULL,
    GiaVe               DECIMAL(12,2)   NOT NULL CHECK (GiaVe > 0),
    PhuThuTangGiuong    DECIMAL(8,2)    NOT NULL DEFAULT 0.0 CHECK (PhuThuTangGiuong >= 0),
    CONSTRAINT FK_BangGiaCD_GaDi FOREIGN KEY (MaGaDi) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT FK_BangGiaCD_GaDen FOREIGN KEY (MaGaDen) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT UQ_BangGiaCD_OD UNIQUE (LoaiToa, MaGaDi, MaGaDen)
);
GO

CREATE TABLE vantai.Ve (
    MaVe                INT IDENTITY(1,1) PRIMARY KEY,
    MaDonVe             INT             NOT NULL,
    MaVeCode            VARCHAR(30)     NOT NULL UNIQUE,
    MaChuyenTau         INT             NOT NULL,
    MaChoNgoi           INT             NOT NULL,
    MaGaDi              INT             NOT NULL,
    MaGaDen             INT             NOT NULL,
    TenHanhKhach        NVARCHAR(100)   NOT NULL,
    CCCDHanhKhach       VARCHAR(20)     NOT NULL,
    GiaVeGoc            DECIMAL(12,2)   NOT NULL CHECK (GiaVeGoc >= 0),
    SoTienGiam          DECIMAL(12,2)   NOT NULL DEFAULT 0.0 CHECK (SoTienGiam >= 0),
    GiaVeThucThu        AS (GiaVeGoc - SoTienGiam) PERSISTED,
    TrangThai           VARCHAR(20)     NOT NULL DEFAULT 'DA_DAT'
        CHECK (TrangThai IN ('DA_DAT', 'DA_LEN_TAU', 'DA_HOAN_VE', 'DA_HUY')),
    ThoiDiemXuatVe      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    PhienBan            ROWVERSION      NOT NULL,
    CONSTRAINT FK_Ve_DonVe FOREIGN KEY (MaDonVe) REFERENCES vantai.DonDatVe(MaDonVe),
    CONSTRAINT FK_Ve_ChuyenTau FOREIGN KEY (MaChuyenTau) REFERENCES vanhanh.ChuyenTau(MaChuyenTau),
    CONSTRAINT FK_Ve_ChoNgoi FOREIGN KEY (MaChoNgoi) REFERENCES vantai.ChoNgoi(MaChoNgoi),
    CONSTRAINT FK_Ve_GaDi FOREIGN KEY (MaGaDi) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT FK_Ve_GaDen FOREIGN KEY (MaGaDen) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT CK_Ve_KhacGa CHECK (MaGaDi <> MaGaDen)
);
GO

CREATE TABLE vantai.BienLaiPhatVe (
    MaBienLai           INT IDENTITY(1,1) PRIMARY KEY,
    MaVe                INT             NULL,
    MaChuyenTau         INT             NOT NULL,
    TenNguoiViPham      NVARCHAR(100)   NOT NULL,
    LyDoViPham          NVARCHAR(255)   NOT NULL,
    CuLyVuotKm          DECIMAL(6,2)    NOT NULL DEFAULT 0.0 CHECK (CuLyVuotKm >= 0),
    TienVeBoSung        DECIMAL(12,2)   NOT NULL CHECK (TienVeBoSung >= 0),
    TienPhat            DECIMAL(12,2)   NOT NULL CHECK (TienPhat >= 0),
    TongTienThu         AS (TienVeBoSung + TienPhat) PERSISTED,
    MaNguoiLap          INT             NOT NULL,
    ThoiDiemLap         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_BienLai_Ve FOREIGN KEY (MaVe) REFERENCES vantai.Ve(MaVe),
    CONSTRAINT FK_BienLai_ChuyenTau FOREIGN KEY (MaChuyenTau) REFERENCES vanhanh.ChuyenTau(MaChuyenTau),
    CONSTRAINT FK_BienLai_NguoiLap FOREIGN KEY (MaNguoiLap) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan)
);
GO

CREATE TABLE vantai.HoanHuyVe (
    MaHoanHuy           INT IDENTITY(1,1) PRIMARY KEY,
    MaVe                INT             NOT NULL UNIQUE,
    ThoiDiemHoan        DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    TienVeGoc           DECIMAL(12,2)   NOT NULL CHECK (TienVeGoc >= 0),
    TyLeLePhi           DECIMAL(4,2)    NOT NULL CHECK (TyLeLePhi IN (0.10, 0.20)), -- 10% (trước 24h) hoặc 20% (4h-24h) theo chuẩn VNR
    LePhiHoan           AS (CAST(TienVeGoc * TyLeLePhi AS DECIMAL(12,2))) PERSISTED,
    SoTienThucHoan      AS (CAST(TienVeGoc - (TienVeGoc * TyLeLePhi) AS DECIMAL(12,2))) PERSISTED,
    HinhThucHoan        VARCHAR(30)     NOT NULL DEFAULT 'CHUYEN_KHOAN'
        CHECK (HinhThucHoan IN ('CHUYEN_KHOAN', 'TIEN_MAT', 'VI_DIEN_TU')),
    LyDoHoan            NVARCHAR(255)   NOT NULL,
    MaNhanVienDuyet     INT             NOT NULL,
    CONSTRAINT FK_HoanHuy_Ve FOREIGN KEY (MaVe) REFERENCES vantai.Ve(MaVe),
    CONSTRAINT FK_HoanHuy_NhanVien FOREIGN KEY (MaNhanVienDuyet) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan)
);
GO

CREATE TABLE vantai.LoaiHangHoa (
    MaLoaiHang          INT IDENTITY(1,1) PRIMARY KEY,
    TenHangHoa          NVARCHAR(100)   NOT NULL UNIQUE,
    NhomCuoc            INT             NOT NULL CHECK (NhomCuoc IN (1, 2, 3)),
    DonGiaMoiTanKm      DECIMAL(8,2)    NOT NULL CHECK (DonGiaMoiTanKm > 0)
);
GO

CREATE TABLE vantai.VanDonHang (
    MaVanDon            INT IDENTITY(1,1) PRIMARY KEY,
    MaVanDonCode        VARCHAR(30)     NOT NULL UNIQUE,
    TenNguoiGui         NVARCHAR(100)   NOT NULL,
    SDTNguoiGui         VARCHAR(20)     NOT NULL,
    TenNguoiNhan        NVARCHAR(100)   NOT NULL,
    SDTNguoiNhan        VARCHAR(20)     NOT NULL,
    MaGaGui             INT             NOT NULL,
    MaGaNhan            INT             NOT NULL,
    MaLoaiHang          INT             NOT NULL,
    TrongLuongTan       DECIMAL(6,2)    NOT NULL CHECK (TrongLuongTan > 0),
    CuocPhi             DECIMAL(12,2)   NOT NULL CHECK (CuocPhi >= 0),
    MaToaXeHang         INT             NULL,
    LoaiVanChuyen       VARCHAR(20)     NOT NULL DEFAULT 'HANG_HOA'
        CHECK (LoaiVanChuyen IN ('HANG_HOA', 'XE_MAY', 'CONTAINER')),
    BienKiemSoat        VARCHAR(20)     NULL,
    DaRutXang           BIT             NULL,
    SoHieuContainer     VARCHAR(30)     NULL,
    SoChiHaiQuan        VARCHAR(30)     NULL,
    GhiChu              NVARCHAR(MAX)   NULL,
    TrangThai           VARCHAR(20)     NOT NULL DEFAULT 'DA_NHAN'
        CHECK (TrangThai IN ('DA_NHAN', 'DA_XEP', 'DANG_VAN_CHUYEN', 'DA_DEN', 'DA_GIAO')),
    ThoiDiemTao         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    PhienBan            ROWVERSION      NOT NULL,
    CONSTRAINT FK_VanDon_GaGui FOREIGN KEY (MaGaGui) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT FK_VanDon_GaNhan FOREIGN KEY (MaGaNhan) REFERENCES hatang.Ga(MaGa),
    CONSTRAINT FK_VanDon_LoaiHang FOREIGN KEY (MaLoaiHang) REFERENCES vantai.LoaiHangHoa(MaLoaiHang),
    CONSTRAINT FK_VanDon_ToaXe FOREIGN KEY (MaToaXeHang) REFERENCES phuongtien.ToaXe(MaToaXe),
    CONSTRAINT CK_VanDon_KhacGa CHECK (MaGaGui <> MaGaNhan),
    CONSTRAINT CK_VanDon_LoaiVanChuyen CHECK (
        (LoaiVanChuyen = 'XE_MAY'    AND BienKiemSoat IS NOT NULL AND DaRutXang IS NOT NULL)
     OR (LoaiVanChuyen = 'CONTAINER' AND SoHieuContainer IS NOT NULL)
     OR (LoaiVanChuyen = 'HANG_HOA'  AND BienKiemSoat IS NULL AND SoHieuContainer IS NULL)
    )
);
GO


-- SCHEMA: baotri


CREATE TABLE baotri.NhatKyCapNhienLieu (
    MaNhatKyDau         INT IDENTITY(1,1) PRIMARY KEY,
    MaDauMay            INT             NOT NULL,
    NoiCapDau           NVARCHAR(100)   NOT NULL,
    SoLitTraNap         DECIMAL(8,2)    NOT NULL CHECK (SoLitTraNap > 0),
    TrongLuongKeoTan    DECIMAL(7,2)    NOT NULL CHECK (TrongLuongKeoTan > 0),
    CuLyChayKm          DECIMAL(6,1)    NOT NULL CHECK (CuLyChayKm > 0),
    SuatTieuHaoSFC      DECIMAL(5,2)    NOT NULL CHECK (SuatTieuHaoSFC >= 0),
    CanhBaoVuotMuc      BIT             NOT NULL DEFAULT 0,
    MaNguoiCap          INT             NULL,
    ThoiDiemBomDau      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CapDau_DauMay FOREIGN KEY (MaDauMay) REFERENCES phuongtien.DauMay(MaDauMay),
    CONSTRAINT FK_CapDau_NguoiCap FOREIGN KEY (MaNguoiCap) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan)
);
GO

CREATE TABLE baotri.KhamXeKyThuat (
    MaKhamXe            INT IDENTITY(1,1) PRIMARY KEY,
    MaDoanTau           INT             NOT NULL,
    MaNguoiKham         INT             NOT NULL,
    ApLucHamBar         DECIMAL(3,1)    NOT NULL CHECK (ApLucHamBar > 0 AND ApLucHamBar <= 10.0),
    DoSutApBarPhut      DECIMAL(3,2)    NOT NULL CHECK (DoSutApBarPhut >= 0 AND DoSutApBarPhut <= 5.0),
    DaCapNuoc           BIT             NOT NULL DEFAULT 0,
    DaXaVeSinh          BIT             NOT NULL DEFAULT 0,
    -- Tự động đánh giá: áp lực hãm >= 4.8, sụt áp <= 0.20, đủ nước và vệ sinh
    DuDieuKienXuatBen   AS (
        CASE 
            WHEN ApLucHamBar >= 4.8 
             AND DoSutApBarPhut <= 0.20 
             AND DaCapNuoc = 1 
             AND DaXaVeSinh = 1 
            THEN CAST(1 AS BIT) 
            ELSE CAST(0 AS BIT) 
        END
    ) PERSISTED,
    GhiChuKyThuat       NVARCHAR(MAX)   NULL,
    ThoiDiemKham        DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_KhamXe_DoanTau FOREIGN KEY (MaDoanTau) REFERENCES vanhanh.DoanTau(MaDoanTau),
    CONSTRAINT FK_KhamXe_NguoiKham FOREIGN KEY (MaNguoiKham) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan)
);
GO

CREATE TABLE baotri.NhatKyBaoDuong (
    MaBaoDuong          INT IDENTITY(1,1) PRIMARY KEY,
    LoaiPhuongTien      VARCHAR(20)     NOT NULL
        CHECK (LoaiPhuongTien IN ('DAU_MAY', 'TOA_XE')),
    MaDauMay            INT             NULL,
    MaToaXe             INT             NULL,
    CapBaoDuong         VARCHAR(20)     NOT NULL
        CHECK (CapBaoDuong IN ('R1', 'R2', 'D1', 'D2')),
    SoKmTaiThoiDiem     DECIMAL(9,1)    NOT NULL CHECK (SoKmTaiThoiDiem >= 0),
    GhiChuKyThuat       NVARCHAR(MAX)   NULL,
    MaNguoiThucHien     INT             NULL,
    ThoiDiemHoanThanh   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_BaoDuong_DauMay FOREIGN KEY (MaDauMay) REFERENCES phuongtien.DauMay(MaDauMay),
    CONSTRAINT FK_BaoDuong_ToaXe FOREIGN KEY (MaToaXe) REFERENCES phuongtien.ToaXe(MaToaXe),
    CONSTRAINT FK_BaoDuong_NguoiThucHien FOREIGN KEY (MaNguoiThucHien) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan),
    CONSTRAINT CK_PhuongTien_Ref CHECK (
        (LoaiPhuongTien = 'DAU_MAY' AND MaDauMay IS NOT NULL AND MaToaXe IS NULL)
        OR (LoaiPhuongTien = 'TOA_XE' AND MaToaXe IS NOT NULL AND MaDauMay IS NULL)
    )
);
GO


-- SCHEMA: kiemtoan


CREATE TABLE kiemtoan.NhatKyHeThong (
    MaNhatKy            BIGINT IDENTITY(1,1) PRIMARY KEY,
    ThoiDiem            DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    MaTaiKhoan          INT             NULL,
    HanhDong            VARCHAR(50)     NOT NULL,
    TenBang             VARCHAR(50)     NOT NULL,
    KhoaChinhRecord     VARCHAR(50)     NOT NULL,
    DuLieuCu            NVARCHAR(MAX)   NULL,
    DuLieuMoi           NVARCHAR(MAX)   NULL,
    DiaChiIP            VARCHAR(45)     NULL,
    CONSTRAINT FK_NhatKy_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan)
);
GO

CREATE TABLE kiemtoan.NhatKyChatAI (
    MaChat              INT IDENTITY(1,1) PRIMARY KEY,
    MaTaiKhoan          INT             NOT NULL,
    CauHoi              NVARCHAR(MAX)   NOT NULL,
    CauSQL              NVARCHAR(MAX)   NULL,
    CauTraLoi           NVARCHAR(MAX)   NOT NULL,
    ThoiDiem            DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ChatAI_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES phanquyen.TaiKhoan(MaTaiKhoan)
);
GO

/* =========================================================================
   3. FUNCTIONS, PROCEDURES & TRIGGERS
   ========================================================================= */

-- 3.1. Hàm tính giá vé — Phân biệt 2 chế độ tính cước theo Thông tư 17/2019/TT-BGTVT:
--   • TAU_NHANH (SE): Giá cố định theo cặp ga (OD) — tra bảng BangGiaCoDinh
--   • TAU_CHO:        Giá tính theo km thực tế — công thức GiaVeSan + CuLyKm × ĐơnGiá/km
--   • Fallback:       Nếu không tìm thấy giá cố định → dùng công thức km làm dự phòng
IF OBJECT_ID('vantai.fn_TinhGiaVe', 'FN') IS NOT NULL
    DROP FUNCTION vantai.fn_TinhGiaVe;
GO

CREATE FUNCTION vantai.fn_TinhGiaVe(
    @LoaiToa VARCHAR(20),
    @MaGaDi INT,
    @MaGaDen INT,
    @TangGiuong INT = NULL,
    @LoaiTau VARCHAR(20) = 'TAU_CHO'  -- Default backward-compatible
)
RETURNS DECIMAL(12,2)
AS
BEGIN
    -- =============================
    -- NHÁNH 1: TÀU NHANH (SE) — Giá cố định theo cặp ga
    -- =============================
    IF @LoaiTau = 'TAU_NHANH'
    BEGIN
        DECLARE @GiaCoDinh DECIMAL(12,2), @PhuThuCD DECIMAL(8,2);

        SELECT 
            @GiaCoDinh = GiaVe,
            @PhuThuCD = CASE 
                WHEN @TangGiuong = 1 THEN PhuThuTangGiuong
                WHEN @TangGiuong = 2 THEN PhuThuTangGiuong * 0.5
                ELSE 0.0 
            END
        FROM vantai.BangGiaCoDinh
        WHERE LoaiToa = @LoaiToa AND MaGaDi = @MaGaDi AND MaGaDen = @MaGaDen;

        -- Thử chiều ngược (giá đi/về giống nhau)
        IF @GiaCoDinh IS NULL
            SELECT 
                @GiaCoDinh = GiaVe,
                @PhuThuCD = CASE 
                    WHEN @TangGiuong = 1 THEN PhuThuTangGiuong
                    WHEN @TangGiuong = 2 THEN PhuThuTangGiuong * 0.5
                    ELSE 0.0 
                END
            FROM vantai.BangGiaCoDinh
            WHERE LoaiToa = @LoaiToa AND MaGaDi = @MaGaDen AND MaGaDen = @MaGaDi;

        IF @GiaCoDinh IS NOT NULL
            RETURN ROUND(@GiaCoDinh + ISNULL(@PhuThuCD, 0.0), -3);

        -- Fallback: Nếu không có giá cố định cho cặp ga này → dùng công thức km bên dưới
    END;

    -- =============================
    -- NHÁNH 2: TÀU CHỢ / FALLBACK — Giá tính theo km lý trình
    -- =============================
    DECLARE @KmDi DECIMAL(7,2), @KmDen DECIMAL(7,2);
    SELECT @KmDi = LyTrinhKm FROM hatang.Ga WHERE MaGa = @MaGaDi;
    SELECT @KmDen = LyTrinhKm FROM hatang.Ga WHERE MaGa = @MaGaDen;

    IF @KmDi IS NULL OR @KmDen IS NULL RETURN NULL;

    DECLARE @CuLyKm DECIMAL(7,2) = ABS(@KmDen - @KmDi);
    DECLARE @DonGia DECIMAL(8,2), @GiaSan DECIMAL(10,2), @PhuThu DECIMAL(8,2);

    SELECT 
        @DonGia = DonGiaMoiKm,
        @GiaSan = GiaVeSan,
        @PhuThu = CASE 
            WHEN @TangGiuong = 1 THEN PhuThuTangGiuong
            WHEN @TangGiuong = 2 THEN PhuThuTangGiuong * 0.5
            ELSE 0.0 
        END
    FROM vantai.BieuCuoc
    WHERE LoaiToa = @LoaiToa;

    IF @DonGia IS NULL RETURN NULL;

    RETURN ROUND(@GiaSan + (@CuLyKm * @DonGia) + ISNULL(@PhuThu, 0.0), -3); -- Làm tròn đến 1.000 VNĐ theo chuẩn VNR
END;
GO

-- 3.2. Đặt vé an toàn tương tranh (UPDLOCK, HOLDLOCK)
IF OBJECT_ID('vantai.sp_DatVeTheoChang', 'P') IS NOT NULL
    DROP PROCEDURE vantai.sp_DatVeTheoChang;
GO

CREATE PROCEDURE vantai.sp_DatVeTheoChang
    @MaDonVe            INT,
    @MaChuyenTau        INT,
    @MaChoNgoi          INT,
    @MaGaDi             INT,
    @MaGaDen            INT,
    @TenHanhKhach       NVARCHAR(100),
    @CCCDHanhKhach      VARCHAR(20),
    @GiaVeGoc           DECIMAL(12,2),
    @SoTienGiam         DECIMAL(12,2) = 0.0,
    @MaVeCodeOutput     VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @KmDi DECIMAL(7,2);
    DECLARE @KmDen DECIMAL(7,2);
    DECLARE @GiaVeThucThu DECIMAL(12,2) = @GiaVeGoc - @SoTienGiam;

    SELECT @KmDi = LyTrinhKm FROM hatang.Ga WHERE MaGa = @MaGaDi;
    SELECT @KmDen = LyTrinhKm FROM hatang.Ga WHERE MaGa = @MaGaDen;

    IF @KmDi IS NULL OR @KmDen IS NULL
    BEGIN
        RAISERROR(N'Ga đi hoặc Ga đến không hợp lệ.', 16, 1);
        RETURN -1;
    END;

    IF @MaVeCodeOutput IS NULL OR LEN(@MaVeCodeOutput) = 0
        SET @MaVeCodeOutput = 'TK_' + UPPER(SUBSTRING(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 1, 10));

    BEGIN TRANSACTION;
    BEGIN TRY
        -- Khóa chỗ ngồi trong chặng để loại trừ check-then-insert race condition (Chuẩn 3NF tra cứu qua bảng Ga)
        IF EXISTS (
            SELECT 1 
            FROM vantai.Ve v WITH (UPDLOCK, HOLDLOCK)
            JOIN hatang.Ga gDi ON v.MaGaDi = gDi.MaGa
            JOIN hatang.Ga gDen ON v.MaGaDen = gDen.MaGa
            WHERE v.MaChuyenTau = @MaChuyenTau
              AND v.MaChoNgoi = @MaChoNgoi
              AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
              AND NOT (
                  IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) >= IIF(@KmDi < @KmDen, @KmDen, @KmDi)
                  OR IIF(gDi.LyTrinhKm > gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) <= IIF(@KmDi < @KmDen, @KmDi, @KmDen)
              )
        )
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'Chỗ ngồi đã có khách đặt trên cung chặng trùng lặp.', 16, 1);
            RETURN -2;
        END;

        INSERT INTO vantai.Ve (
            MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi,
            MaGaDi, MaGaDen,
            TenHanhKhach, CCCDHanhKhach,
            GiaVeGoc, SoTienGiam,
            TrangThai, ThoiDiemXuatVe
        ) VALUES (
            @MaDonVe, @MaVeCodeOutput, @MaChuyenTau, @MaChoNgoi,
            @MaGaDi, @MaGaDen,
            @TenHanhKhach, @CCCDHanhKhach,
            @GiaVeGoc, @SoTienGiam,
            'DA_DAT', SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Ghi nhật ký kiểm toán hệ thống dùng chung
IF OBJECT_ID('kiemtoan.sp_GhiNhatKyHeThong', 'P') IS NOT NULL
    DROP PROCEDURE kiemtoan.sp_GhiNhatKyHeThong;
GO

CREATE PROCEDURE kiemtoan.sp_GhiNhatKyHeThong
    @MaTaiKhoan          INT = NULL,
    @HanhDong            VARCHAR(50),
    @TenBang             VARCHAR(50),
    @KhoaChinhRecord     VARCHAR(50),
    @DuLieuCu            NVARCHAR(MAX) = NULL,
    @DuLieuMoi           NVARCHAR(MAX) = NULL,
    @DiaChiIP            VARCHAR(45) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO kiemtoan.NhatKyHeThong (
        ThoiDiem, MaTaiKhoan, HanhDong, TenBang, KhoaChinhRecord, DuLieuCu, DuLieuMoi, DiaChiIP
    )
    VALUES (
        SYSUTCDATETIME(), @MaTaiKhoan, @HanhDong, @TenBang, @KhoaChinhRecord, @DuLieuCu, @DuLieuMoi, ISNULL(@DiaChiIP, HOST_NAME())
    );
END;
GO

-- Trigger tự động kiểm toán bảng Ga (Snapshot JSON cũ & mới)
IF OBJECT_ID('hatang.trg_Ga_AuditLog', 'TR') IS NOT NULL
    DROP TRIGGER hatang.trg_Ga_AuditLog;
GO

CREATE TRIGGER hatang.trg_Ga_AuditLog
ON hatang.Ga
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- INSERT
    IF EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO kiemtoan.NhatKyHeThong (ThoiDiem, MaTaiKhoan, HanhDong, TenBang, KhoaChinhRecord, DuLieuCu, DuLieuMoi, DiaChiIP)
        SELECT 
            SYSUTCDATETIME(),
            NULL,
            'THEM_GA',
            'hatang.Ga',
            CAST(i.MaGa AS VARCHAR(50)),
            NULL,
            (SELECT i.MaGa, i.MaTuyen, i.MaGaCode, i.TenGa, i.LyTrinhKm, i.TinhThanh, i.HangGa, i.CoCauQuay, i.DangKhaiThac, i.NguoiTao, i.NgayTao FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            HOST_NAME()
        FROM inserted i;
    END

    -- DELETE
    IF EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted)
    BEGIN
        INSERT INTO kiemtoan.NhatKyHeThong (ThoiDiem, MaTaiKhoan, HanhDong, TenBang, KhoaChinhRecord, DuLieuCu, DuLieuMoi, DiaChiIP)
        SELECT 
            SYSUTCDATETIME(),
            NULL,
            'XOA_GA',
            'hatang.Ga',
            CAST(d.MaGa AS VARCHAR(50)),
            (SELECT d.MaGa, d.MaTuyen, d.MaGaCode, d.TenGa, d.LyTrinhKm, d.TinhThanh, d.HangGa, d.CoCauQuay, d.DangKhaiThac, d.NguoiTao, d.NgayTao FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            NULL,
            HOST_NAME()
        FROM deleted d;
    END

    -- UPDATE
    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
    BEGIN
        IF NOT UPDATE(NgayCapNhat)
        BEGIN
            UPDATE g
            SET NgayCapNhat = SYSUTCDATETIME()
            FROM hatang.Ga g
            INNER JOIN inserted i ON g.MaGa = i.MaGa;
        END

        INSERT INTO kiemtoan.NhatKyHeThong (ThoiDiem, MaTaiKhoan, HanhDong, TenBang, KhoaChinhRecord, DuLieuCu, DuLieuMoi, DiaChiIP)
        SELECT 
            SYSUTCDATETIME(),
            NULL,
            'SUA_GA',
            'hatang.Ga',
            CAST(i.MaGa AS VARCHAR(50)),
            (SELECT d.MaGa, d.MaTuyen, d.MaGaCode, d.TenGa, d.LyTrinhKm, d.TinhThanh, d.HangGa, d.CoCauQuay, d.DangKhaiThac, d.NguoiTao, d.NgayTao FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            (SELECT i.MaGa, i.MaTuyen, i.MaGaCode, i.TenGa, i.LyTrinhKm, i.TinhThanh, i.HangGa, i.CoCauQuay, i.DangKhaiThac, i.NguoiTao, i.NgayTao FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            HOST_NAME()
        FROM inserted i
        INNER JOIN deleted d ON i.MaGa = d.MaGa;
    END
END;
GO

-- 3.4. Trigger đảm bảo thứ tự thời gian tăng dần nghiêm ngặt giữa các điểm dừng của chuyến tàu
IF OBJECT_ID('vanhanh.trg_LichDungGa_CheckThoiGian', 'TR') IS NOT NULL
    DROP TRIGGER vanhanh.trg_LichDungGa_CheckThoiGian;
GO

CREATE TRIGGER vanhanh.trg_LichDungGa_CheckThoiGian
ON vanhanh.LichDungGa
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Điểm dừng sau phải có GioDenKeHoach > GioDiKeHoach của điểm dừng liền trước
    IF EXISTS (
        SELECT 1
        FROM vanhanh.LichDungGa curr
        INNER JOIN inserted i ON curr.MaChuyenTau = i.MaChuyenTau AND curr.ThuTuDung = i.ThuTuDung
        INNER JOIN vanhanh.LichDungGa prev ON curr.MaChuyenTau = prev.MaChuyenTau AND prev.ThuTuDung = curr.ThuTuDung - 1
        WHERE curr.GioDenKeHoach <= prev.GioDiKeHoach
    )
    BEGIN
        RAISERROR(N'Lỗi xung đột thời gian: Điểm dừng sau phải có Giờ Đến lớn hơn Giờ Đi của điểm dừng liền trước.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- Giờ Đi của điểm dừng này không được vượt quá Giờ Đến của điểm dừng kế tiếp
    IF EXISTS (
        SELECT 1
        FROM vanhanh.LichDungGa curr
        INNER JOIN inserted i ON curr.MaChuyenTau = i.MaChuyenTau AND curr.ThuTuDung = i.ThuTuDung
        INNER JOIN vanhanh.LichDungGa nxt ON curr.MaChuyenTau = nxt.MaChuyenTau AND nxt.ThuTuDung = curr.ThuTuDung + 1
        WHERE nxt.GioDenKeHoach <= curr.GioDiKeHoach
    )
    BEGIN
        RAISERROR(N'Lỗi xung đột thời gian: Giờ Đi của điểm dừng này không được vượt quá Giờ Đến của điểm dừng kế tiếp.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 3.5. Trigger tự động phát hiện xung đột chiều dài đoàn tàu vượt quá chiều dài đường ray ga
IF OBJECT_ID('vanhanh.trg_LichDungGa_CheckChieuDaiTau', 'TR') IS NOT NULL
    DROP TRIGGER vanhanh.trg_LichDungGa_CheckChieuDaiTau;
GO

CREATE TRIGGER vanhanh.trg_LichDungGa_CheckChieuDaiTau
ON vanhanh.LichDungGa
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN vanhanh.DoanTau dt ON i.MaChuyenTau = dt.MaChuyenTau
        INNER JOIN hatang.DuongRayGa dr ON i.MaDuongRay = dr.MaDuongRay
        WHERE dt.TongChieuDaiM > dr.ChieuDaiHuuDungM
    )
    BEGIN
        RAISERROR(N'Cảnh báo an toàn: Chiều dài đoàn tàu vượt quá chiều dài hữu dụng của đường ray tại ga chỉ định.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 3.6. Trigger ngăn chặn phân công cùng 1 nhân viên lái tàu cho 2 chuyến tàu có khung giờ chồng lấn
IF OBJECT_ID('nhansu.trg_PhanCongKipLai_CheckTrungGio', 'TR') IS NOT NULL
    DROP TRIGGER nhansu.trg_PhanCongKipLai_CheckTrungGio;
GO

CREATE TRIGGER nhansu.trg_PhanCongKipLai_CheckTrungGio
ON nhansu.PhanCongKipLai
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN vanhanh.ChuyenTau ct1 ON i.MaChuyenTau = ct1.MaChuyenTau
        JOIN nhansu.PhanCongKipLai pk2 ON (pk2.MaLaiTau = i.MaLaiTau OR pk2.MaPhuLai = i.MaPhuLai OR pk2.MaLaiTau = i.MaPhuLai OR pk2.MaPhuLai = i.MaLaiTau)
                                      AND pk2.MaPhanCong <> i.MaPhanCong
        JOIN vanhanh.ChuyenTau ct2 ON pk2.MaChuyenTau = ct2.MaChuyenTau
        WHERE ct1.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
          AND ct2.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
          AND (CASE WHEN ct1.GioXuatPhatKH > ct2.GioXuatPhatKH THEN ct1.GioXuatPhatKH ELSE ct2.GioXuatPhatKH END)
            < (CASE WHEN ct1.GioVeDichKH < ct2.GioVeDichKH THEN ct1.GioVeDichKH ELSE ct2.GioVeDichKH END)
    )
    BEGIN
        RAISERROR(N'Xung đột nhân sự: Lái tàu hoặc Phụ lái đã được phân công cho một chuyến tàu khác có khung giờ chồng lấn.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

/* =========================================================================
   4. CHỈ MỤC (INDEXES)
   ========================================================================= */

-- 4.1. Chỉ mục nghiệp vụ tra cứu
CREATE NONCLUSTERED INDEX IX_Ve_TraCuuGhe 
    ON vantai.Ve (MaChuyenTau, MaChoNgoi, TrangThai, MaGaDi, MaGaDen)
    INCLUDE (GiaVeThucThu, TenHanhKhach);

CREATE NONCLUSTERED INDEX IX_LichDungGa_ThoiGian 
    ON vanhanh.LichDungGa (MaChuyenTau, ThuTuDung, GioDenKeHoach, GioDiKeHoach)
    INCLUDE (MaGa, MaDuongRay, LaDiemTranh);

CREATE NONCLUSTERED INDEX IX_ChuyenTau_NgayTrangThai 
    ON vanhanh.ChuyenTau (NgayXuatPhat, TrangThai)
    INCLUDE (MaMacTau, SoPhutTreLuyKe);

CREATE NONCLUSTERED INDEX IX_VanDon_CodeTrangThai 
    ON vantai.VanDonHang (MaVanDonCode, TrangThai)
    INCLUDE (MaGaGui, MaGaNhan, CuocPhi);

CREATE NONCLUSTERED INDEX IX_TaiKhoanVaiTro_Lookup 
    ON phanquyen.TaiKhoan_VaiTro (MaTaiKhoan, MaVaiTro);

CREATE NONCLUSTERED INDEX IX_NhatKyHeThong_ThoiDiem 
    ON kiemtoan.NhatKyHeThong (ThoiDiem, TenBang);

-- 4.2. Chỉ mục khóa ngoại (Foreign Key Indexes)
CREATE NONCLUSTERED INDEX IX_FK_Ga_MaTuyen ON hatang.Ga(MaTuyen);
CREATE NONCLUSTERED INDEX IX_FK_DuongRayGa_MaGa ON hatang.DuongRayGa(MaGa);
CREATE NONCLUSTERED INDEX IX_FK_KhuGian_MaGaDau ON hatang.KhuGian(MaGaDau);
CREATE NONCLUSTERED INDEX IX_FK_KhuGian_MaGaCuoi ON hatang.KhuGian(MaGaCuoi);
CREATE NONCLUSTERED INDEX IX_FK_DuongNgang_MaKhuGian ON hatang.DuongNgang(MaKhuGian);

CREATE NONCLUSTERED INDEX IX_FK_TaiKhoan_MaNhanVien ON phanquyen.TaiKhoan(MaNhanVien);
CREATE NONCLUSTERED INDEX IX_FK_LSDN_MaTaiKhoan ON phanquyen.LichSuDangNhap(MaTaiKhoan);

CREATE NONCLUSTERED INDEX IX_FK_DauMay_MaDongDauMay ON phuongtien.DauMay(MaDongDauMay);
CREATE NONCLUSTERED INDEX IX_FK_ToaXe_MaChungLoai ON phuongtien.ToaXe(MaChungLoai);

CREATE NONCLUSTERED INDEX IX_FK_ChuyenTau_MaMacTau ON vanhanh.ChuyenTau(MaMacTau);
CREATE NONCLUSTERED INDEX IX_FK_LichDung_MaGa ON vanhanh.LichDungGa(MaGa);
CREATE NONCLUSTERED INDEX IX_FK_LichDung_MaDuongRay ON vanhanh.LichDungGa(MaDuongRay);
CREATE NONCLUSTERED INDEX IX_FK_DoanTau_MaChuyenTau ON vanhanh.DoanTau(MaChuyenTau);
CREATE NONCLUSTERED INDEX IX_FK_DoanTau_MaDauMayChinh ON vanhanh.DoanTau(MaDauMayChinh);
CREATE NONCLUSTERED INDEX IX_FK_CTDoanTau_MaDoanTau ON vanhanh.ChiTietDoanTau(MaDoanTau);
CREATE NONCLUSTERED INDEX IX_FK_CTDoanTau_MaToaXe ON vanhanh.ChiTietDoanTau(MaToaXe);
CREATE NONCLUSTERED INDEX IX_FK_ChamGio_MaChuyenTau ON vanhanh.NhatKyChamGio(MaChuyenTau);
CREATE NONCLUSTERED INDEX IX_FK_PhongToa_MaKhuGian ON vanhanh.LenhPhongToa(MaKhuGian);

CREATE NONCLUSTERED INDEX IX_FK_KipLai_MaChuyenTau ON nhansu.PhanCongKipLai(MaChuyenTau);
CREATE NONCLUSTERED INDEX IX_FK_KipLai_MaLaiTau ON nhansu.PhanCongKipLai(MaLaiTau);
CREATE NONCLUSTERED INDEX IX_FK_KiemTra_MaChuyenTau ON nhansu.KiemTraSucKhoe(MaChuyenTau);
CREATE NONCLUSTERED INDEX IX_FK_KiemTra_MaNhanVien ON nhansu.KiemTraSucKhoe(MaNhanVien);

CREATE NONCLUSTERED INDEX IX_FK_DonVe_MaKhachHang ON vantai.DonDatVe(MaKhachHang);
CREATE NONCLUSTERED INDEX IX_FK_ToaXeKhach_MaChuyenTau ON vantai.ToaXeKhach(MaChuyenTau);
CREATE NONCLUSTERED INDEX IX_FK_ChoNgoi_MaToaXeKhach ON vantai.ChoNgoi(MaToaXeKhach);
CREATE NONCLUSTERED INDEX IX_FK_Ve_MaDonVe ON vantai.Ve(MaDonVe);
CREATE NONCLUSTERED INDEX IX_FK_Ve_MaChoNgoi ON vantai.Ve(MaChoNgoi);
CREATE NONCLUSTERED INDEX IX_FK_Ve_MaGaDi ON vantai.Ve(MaGaDi);
CREATE NONCLUSTERED INDEX IX_FK_Ve_MaGaDen ON vantai.Ve(MaGaDen);
CREATE NONCLUSTERED INDEX IX_FK_VanDon_MaGaGui ON vantai.VanDonHang(MaGaGui);
CREATE NONCLUSTERED INDEX IX_FK_VanDon_MaGaNhan ON vantai.VanDonHang(MaGaNhan);
CREATE NONCLUSTERED INDEX IX_FK_VanDon_LoaiHang ON vantai.VanDonHang(MaLoaiHang);

CREATE NONCLUSTERED INDEX IX_FK_CapDau_MaDauMay ON baotri.NhatKyCapNhienLieu(MaDauMay);
CREATE NONCLUSTERED INDEX IX_FK_KhamXe_MaDoanTau ON baotri.KhamXeKyThuat(MaDoanTau);
CREATE NONCLUSTERED INDEX IX_FK_BaoDuong_MaDauMay ON baotri.NhatKyBaoDuong(MaDauMay);
CREATE NONCLUSTERED INDEX IX_FK_BaoDuong_MaToaXe ON baotri.NhatKyBaoDuong(MaToaXe);
CREATE NONCLUSTERED INDEX IX_FK_NhatKy_MaTaiKhoan ON kiemtoan.NhatKyHeThong(MaTaiKhoan);
CREATE NONCLUSTERED INDEX IX_FK_ChatAI_MaTaiKhoan ON kiemtoan.NhatKyChatAI(MaTaiKhoan);
GO

/* =========================================================================
   5. DỮ LIỆU MẪU (SEED DATA)
   ========================================================================= */

-- 5.1. Danh mục tra cứu
INSERT INTO danhmuc.DanhMuc (NhomDanhMuc, MaCode, TenHienThi, ThuTuSapXep, GhiChu) VALUES
('HANG_GA', 'HANG_1', N'Hạng I (Ga đầu mối trung tâm)', 1, NULL),
('HANG_GA', 'HANG_2', N'Hạng II (Ga trung chuyển tỉnh)', 2, NULL),
('HANG_GA', 'HANG_3', N'Hạng III (Ga tránh kỹ thuật)', 3, NULL),
('LOAI_DUONG', 'CHINH_TUYEN', N'Chính tuyến', 1, NULL),
('LOAI_DUONG', 'DUONG_TRANH', N'Đường tránh', 2, NULL),
('LOAI_DUONG', 'BOC_DO', N'Đường bốc dỡ', 3, NULL),
('TRANG_THAI_KHU_GIAN', 'RONG', N'Rỗng', 1, NULL),
('TRANG_THAI_KHU_GIAN', 'CO_TAU', N'Có tàu chiếm dụng', 2, NULL),
('TRANG_THAI_KHU_GIAN', 'PHONG_TOA', N'Phong tỏa', 3, NULL),
('LOAI_TOA', 'NC', N'Ngồi cứng', 1, NULL),
('LOAI_TOA', 'NML', N'Ngồi mềm điều hòa', 2, NULL),
('LOAI_TOA', 'BN', N'Nằm khoang 6 điều hòa', 3, NULL),
('LOAI_TOA', 'AN', N'Nằm khoang 4 điều hòa', 4, NULL),
('TRANG_THAI_CHUYEN_TAU', 'DA_LEN_LICH', N'Đã lên lịch', 1, NULL),
('TRANG_THAI_CHUYEN_TAU', 'SAN_SANG', N'Sẵn sàng xuất bến', 2, NULL),
('TRANG_THAI_CHUYEN_TAU', 'DANG_CHAY', N'Đang chạy trên tuyến', 3, NULL),
('TRANG_THAI_CHUYEN_TAU', 'TRE_GIO', N'Chậm giờ', 4, NULL),
('TRANG_THAI_CHUYEN_TAU', 'HOAN_THANH', N'Hoàn thành hành trình', 5, NULL),
('TRANG_THAI_CHUYEN_TAU', 'DA_HUY', N'Đã hủy chuyến', 6, NULL),
('HINH_THUC_TT', 'TIEN_MAT', N'Tiền mặt', 1, NULL),
('HINH_THUC_TT', 'CHUYEN_KHOAN', N'Chuyển khoản ngân hàng', 2, NULL),
('HINH_THUC_TT', 'THE_NGAN_HANG', N'Thẻ thanh toán (POS)', 3, NULL),
('HINH_THUC_TT', 'VI_DIEN_TU', N'Ví điện tử', 4, NULL),
('TI_LE_GIAM_GIA', 'TRE_EM', N'Trẻ em 6-10 tuổi (Giảm 25%)', 1, '0.25'),
('TI_LE_GIAM_GIA', 'NGUOI_GIA', N'Người cao tuổi >= 60 (Giảm 15%)', 2, '0.15'),
('TI_LE_GIAM_GIA', 'SINH_VIEN', N'Học sinh - Sinh viên (Giảm 10%)', 3, '0.10');
GO

-- 5.2. Vai trò, Quyền & Phân quyền
INSERT INTO phanquyen.VaiTro (MaVaiTroCode, TenVaiTro, MoTa) VALUES
('ADMIN', N'Quản trị viên hệ thống', N'Toàn quyền cấu hình và quản trị CSDL'),
('DIEU_DO', N'Trực ban điều độ chạy tàu', N'Lập biểu đồ chạy tàu, duyệt lệnh phong tỏa'),
('TRUONG_GA', N'Trực ban ga / Trưởng ga', N'Đón tiễn tàu, điều động dồn dịch đường ray'),
('BAN_VE', N'Nhân viên bán vé & thủ kho vận đơn', N'Đặt vé, xuất vé, hoàn vé, lập vận đơn'),
('LAI_TAU', N'Lái tàu & Kíp lái', N'Xem lịch trình chạy tàu, nhật ký cấp nhiên liệu'),
('KY_THUAT', N'Nhân viên khám xe kỹ thuật', N'Kiểm tra an toàn hãm, duyệt xuất bến');

INSERT INTO phanquyen.Quyen (MaQuyenCode, TenQuyen, NhomQuyen, MoTa) VALUES
('SYS_CONFIG', N'Cấu hình hệ thống', 'HE_THONG', N'Quản lý danh mục và người dùng'),
('TRAIN_SCHEDULE_EDIT', N'Lập biểu đồ chạy tàu', 'DIEU_DO', N'Thay đổi giờ chạy tàu'),
('BLOCK_ORDER_APPROVE', N'Duyệt lệnh phong tỏa', 'DIEU_DO', N'Phát lệnh cấm hoặc hạn chế tốc độ'),
('DISPATCH_INCIDENT_REPORT', N'Ghi nhận sự cố chậm giờ', 'DIEU_DO', N'Cập nhật lý do và số phút chậm'),
('TICKET_ISSUE', N'Bán và in vé tàu', 'VAN_TAI', N'Giao dịch bán vé'),
('TICKET_REFUND', N'Hoàn và hủy vé tàu', 'VAN_TAI', N'Trả vé và hoàn tiền'),
('CARGO_DISPATCH', N'Lập vận đơn hàng hóa', 'VAN_TAI', N'Nhận hàng hóa, xe máy, container'),
('TECH_SAFETY_APPROVE', N'Nghiệm thu khám xe', 'KY_THUAT', N'Ký duyệt an toàn kỹ thuật');

INSERT INTO phanquyen.VaiTro_Quyen (MaVaiTro, MaQuyen)
SELECT v.MaVaiTro, q.MaQuyen FROM phanquyen.VaiTro v CROSS JOIN phanquyen.Quyen q WHERE v.MaVaiTroCode = 'ADMIN';

INSERT INTO phanquyen.VaiTro_Quyen (MaVaiTro, MaQuyen)
SELECT v.MaVaiTro, q.MaQuyen FROM phanquyen.VaiTro v, phanquyen.Quyen q
WHERE v.MaVaiTroCode = 'DIEU_DO' AND q.MaQuyenCode IN ('TRAIN_SCHEDULE_EDIT', 'BLOCK_ORDER_APPROVE', 'DISPATCH_INCIDENT_REPORT');

INSERT INTO phanquyen.VaiTro_Quyen (MaVaiTro, MaQuyen)
SELECT v.MaVaiTro, q.MaQuyen FROM phanquyen.VaiTro v, phanquyen.Quyen q
WHERE v.MaVaiTroCode = 'BAN_VE' AND q.MaQuyenCode IN ('TICKET_ISSUE', 'TICKET_REFUND', 'CARGO_DISPATCH');

INSERT INTO phanquyen.VaiTro_Quyen (MaVaiTro, MaQuyen)
SELECT v.MaVaiTro, q.MaQuyen FROM phanquyen.VaiTro v, phanquyen.Quyen q
WHERE v.MaVaiTroCode = 'KY_THUAT' AND q.MaQuyenCode = 'TECH_SAFETY_APPROVE';
GO

-- 5.3. Nhân viên & Tài khoản
INSERT INTO nhansu.NhanVien (MaNVCode, HoTen, SoDienThoai, ChucDanh, HangBangLai, HanKhamSucKhoe, DonViChuQuan, TrangThai) VALUES
('NV_001', N'Nguyễn Văn An', '0901234567', N'Lái tàu', 'D19E', '2027-12-31', N'Đội lái tàu Hà Nội', 'SAN_SANG'),
('NV_002', N'Trần Đình Bình', '0902345678', N'Phụ lái', 'D19E', '2027-12-31', N'Đội lái tàu Hà Nội', 'SAN_SANG'),
('NV_003', N'Lê Văn Cường', '0903456789', N'Trưởng tàu', NULL, '2027-12-31', N'Đoàn tiếp viên Hà Nội', 'SAN_SANG'),
('NV_004', N'Phạm Minh Dũng', '0904567890', N'Nhân viên khám xe', NULL, '2027-12-31', N'Trạm Giáp Bát', 'SAN_SANG'),
('NV_005', N'Hoàng Thị Mai', '0905678901', N'Nhân viên bán vé', NULL, '2027-12-31', N'Ga Hà Nội', 'SAN_SANG');

INSERT INTO phanquyen.TaiKhoan (TenDangNhap, MatKhauHash, HoTenHienThi, Email, MaNhanVien) VALUES
('admin', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewR5I72o3jquP6hK', N'Quản trị viên', 'admin@vr.com.vn', NULL),
('nv_dung', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewR5I72o3jquP6hK', N'Phạm Minh Dũng', 'dung.pm@vr.com.vn', 4),
('nv_mai', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewR5I72o3jquP6hK', N'Hoàng Thị Mai', 'mai.ht@vr.com.vn', 5);

INSERT INTO phanquyen.TaiKhoan_VaiTro (MaTaiKhoan, MaVaiTro)
SELECT tk.MaTaiKhoan, vt.MaVaiTro FROM phanquyen.TaiKhoan tk, phanquyen.VaiTro vt
WHERE (tk.TenDangNhap = 'admin' AND vt.MaVaiTroCode = 'ADMIN')
   OR (tk.TenDangNhap = 'nv_dung' AND vt.MaVaiTroCode = 'KY_THUAT')
   OR (tk.TenDangNhap = 'nv_mai' AND vt.MaVaiTroCode = 'BAN_VE');
GO

-- 5.4. Tuyến, Ga, Đường ray & Khu gian (Dữ liệu thực tế Tuyến Thống Nhất Bắc - Nam 1.726,20 km)
INSERT INTO hatang.TuyenDuong (MaTuyenCode, TenTuyen, TongChieuDaiKm, KhoRayMm)
VALUES ('HN_HCM', N'Tuyến Đường sắt Thống Nhất Bắc - Nam', 1726.20, 1000);

DECLARE @MaTuyen INT = SCOPE_IDENTITY();

-- 27 Ga chiến lược trải đều toàn tuyến Bắc - Trung - Nam
INSERT INTO hatang.Ga (MaTuyen, MaGaCode, TenGa, LyTrinhKm, TinhThanh, HangGa, CoCauQuay) VALUES
(@MaTuyen, 'HNI', N'Hà Nội',        0.00,    N'Hà Nội',             'HANG_1', 1), -- Đầu mối phía Bắc (Có cầu quay)
(@MaTuyen, 'GBA', N'Giáp Bát',      5.00,    N'Hà Nội',             'HANG_1', 0), -- Ga hàng hóa lớn nhất miền Bắc
(@MaTuyen, 'PLY', N'Phủ Lý',        56.00,   N'Hà Nam',             'HANG_2', 0),
(@MaTuyen, 'NDH', N'Nam Định',      87.00,   N'Nam Định',           'HANG_2', 0),
(@MaTuyen, 'NBH', N'Ninh Bình',     115.00,  N'Ninh Bình',          'HANG_2', 0),
(@MaTuyen, 'BSO', N'Bỉm Sơn',       141.00,  N'Thanh Hóa',          'HANG_2', 0),
(@MaTuyen, 'THA', N'Thanh Hóa',     175.20,  N'Thanh Hóa',          'HANG_2', 0),
(@MaTuyen, 'VIN', N'Vinh',          319.20,  N'Nghệ An',            'HANG_1', 1), -- Ga trung tâm miền Trung (Có cầu quay)
(@MaTuyen, 'YTR', N'Yên Trung',     340.00,  N'Hà Tĩnh',            'HANG_3', 0),
(@MaTuyen, 'HPH', N'Hương Phố',     387.00,  N'Hà Tĩnh',            'HANG_2', 0), -- Cửa ngõ đèo Khe Nét
(@MaTuyen, 'DHO', N'Đồng Hới',      522.00,  N'Quảng Bình',         'HANG_1', 0),
(@MaTuyen, 'DHA', N'Đông Hà',       622.00,  N'Quảng Trị',          'HANG_2', 0),
(@MaTuyen, 'HUE', N'Huế',           688.00,  N'Thừa Thiên Huế',     'HANG_1', 0),
(@MaTuyen, 'LCO', N'Lăng Cô',       757.00,  N'Thừa Thiên Huế',     'HANG_2', 0), -- Cửa ngõ đèo Hải Vân (Có trạm máy đẩy)
(@MaTuyen, 'DAN', N'Đà Nẵng',       791.40,  N'Đà Nẵng',            'HANG_1', 1), -- Ga lớn nhất miền Trung (Có cầu quay)
(@MaTuyen, 'TKY', N'Tam Kỳ',        865.00,  N'Quảng Nam',          'HANG_2', 0),
(@MaTuyen, 'QNG', N'Quảng Ngãi',    928.00,  N'Quảng Ngãi',         'HANG_2', 0),
(@MaTuyen, 'DTR', N'Diêu Trì',      1095.50, N'Bình Định',          'HANG_1', 1), -- Ga ngã ba Quy Nhơn (Có cầu quay)
(@MaTuyen, 'THO', N'Tuy Hòa',       1198.00, N'Phú Yên',            'HANG_2', 0),
(@MaTuyen, 'NTR', N'Nha Trang',     1315.00, N'Khánh Hòa',          'HANG_1', 1), -- Ga du lịch lớn (Có cầu quay)
(@MaTuyen, 'TCA', N'Tháp Chàm',     1408.00, N'Ninh Thuận',         'HANG_2', 0),
(@MaTuyen, 'BTH', N'Bình Thuận',    1551.00, N'Bình Thuận',         'HANG_2', 0), -- Ga Mương Mán cũ
(@MaTuyen, 'BHO', N'Biên Hòa',      1697.50, N'Đồng Nai',           'HANG_2', 0),
(@MaTuyen, 'DIA', N'Dĩ An',         1707.00, N'Bình Dương',         'HANG_2', 0), -- Nhà máy Xe lửa Dĩ An
(@MaTuyen, 'STH', N'Sóng Thần',     1711.00, N'Bình Dương',         'HANG_1', 0), -- Ga logistics hàng hóa lớn nhất miền Nam
(@MaTuyen, 'BTR', N'Bình Triệu',    1718.00, N'TP. Hồ Chí Minh',    'HANG_2', 0),
(@MaTuyen, 'SGO', N'Sài Gòn',       1726.20, N'TP. Hồ Chí Minh',    'HANG_1', 1); -- Đầu mối phía Nam (Có cầu quay)

-- Đường ray tại các ga
INSERT INTO hatang.DuongRayGa (MaGa, SoHieuDuong, LoaiDuong, ChieuDaiHuuDungM, CoKeGa)
SELECT g.MaGa, d.SoHieuDuong, d.LoaiDuong, d.ChieuDaiHuuDungM, d.CoKeGa
FROM hatang.Ga g
CROSS APPLY (
    SELECT 1 AS SoHieuDuong, 'CHINH_TUYEN' AS LoaiDuong, 500 AS ChieuDaiHuuDungM, 1 AS CoKeGa
    UNION ALL SELECT 2, 'DUONG_TRANH', 450, 1
    UNION ALL SELECT 3, 'DUONG_TRANH', 400, 0
    UNION ALL SELECT 4, 'BOC_DO', 350, 0 WHERE g.HangGa = 'HANG_1' OR g.MaGaCode IN ('GBA', 'STH')
    UNION ALL SELECT 5, 'BOC_DO', 380, 0 WHERE g.MaGaCode IN ('HNI', 'GBA', 'STH', 'SGO')
) d;

-- 26 Khu gian liên hoàn nối tiếp chuẩn hóa (MaGaDau < MaGaCuoi) bao phủ trọn vẹn 1.726,20 km
INSERT INTO hatang.KhuGian (MaGaDau, MaGaCuoi, CuLyKm, TocDoToiDaKhach, TocDoToiDaHang, DoDocPermil, CanDauMayDay, TrangThai)
SELECT g1.MaGa, g2.MaGa, r.CuLyKm, r.VmaxKhach, r.VmaxHang, r.DoDoc, r.MayDay, 'RONG'
FROM (VALUES
    ('HNI', 'GBA', 5.00,   60, 40, 0.0, 0),
    ('GBA', 'PLY', 51.00,  80, 50, 0.0, 0),
    ('PLY', 'NDH', 31.00,  80, 50, 0.0, 0),
    ('NDH', 'NBH', 28.00,  80, 50, 0.0, 0),
    ('NBH', 'BSO', 26.00,  80, 50, 0.0, 0),
    ('BSO', 'THA', 34.20,  80, 50, 0.0, 0),
    ('THA', 'VIN', 144.00, 80, 50, 0.0, 0),
    ('VIN', 'YTR', 20.80,  80, 50, 0.0, 0),
    ('YTR', 'HPH', 47.00,  80, 50, 0.0, 0),
    ('HPH', 'DHO', 135.00, 70, 45, 15.0, 1), -- Đèo Khe Nét (Dốc 15‰ - Cần đầu máy đẩy)
    ('DHO', 'DHA', 100.00, 80, 50, 0.0, 0),
    ('DHA', 'HUE', 66.00,  80, 50, 0.0, 0),
    ('HUE', 'LCO', 69.00,  80, 50, 0.0, 0),
    ('LCO', 'DAN', 34.40,  60, 40, 17.0, 1), -- Đèo Hải Vân (Dốc 17‰ - Cần đầu máy đẩy)
    ('DAN', 'TKY', 73.60,  80, 50, 0.0, 0),
    ('TKY', 'QNG', 63.00,  80, 50, 0.0, 0),
    ('QNG', 'DTR', 167.50, 80, 50, 5.0, 0),
    ('DTR', 'THO', 102.50, 80, 50, 5.0, 0),
    ('THO', 'NTR', 117.00, 80, 50, 5.0, 0),
    ('NTR', 'TCA', 93.00,  80, 50, 5.0, 0),
    ('TCA', 'BTH', 143.00, 80, 50, 5.0, 0),
    ('BTH', 'BHO', 146.50, 80, 50, 0.0, 0),
    ('BHO', 'DIA', 9.50,   80, 50, 0.0, 0),
    ('DIA', 'STH', 4.00,   60, 40, 0.0, 0),
    ('STH', 'BTR', 7.00,   60, 40, 0.0, 0),
    ('BTR', 'SGO', 8.20,   50, 30, 0.0, 0)
) r(MaDau, MaCuoi, CuLyKm, VmaxKhach, VmaxHang, DoDoc, MayDay)
JOIN hatang.Ga g1 ON r.MaDau = g1.MaGaCode
JOIN hatang.Ga g2 ON r.MaCuoi = g2.MaGaCode;
GO

-- 5.5. Biểu cước chuẩn VNR & Loại hàng hóa
-- BieuCuoc: Dùng cho TÀU CHỢ (tính theo km) + fallback cho tàu SE khi không có giá cố định
INSERT INTO vantai.BieuCuoc (LoaiToa, DonGiaMoiKm, GiaVeSan, PhuThuTangGiuong) VALUES
('NC',  700.00,  50000.00, 0.00),     -- Ngồi cứng
('NML', 950.00,  70000.00, 0.00),     -- Ngồi mềm điều hòa
('BN',  1200.00, 100000.00, 30000.00), -- Nằm khoang 6 (Phụ thu tầng 1: 30k, tầng 2: 15k)
('AN',  1500.00, 150000.00, 50000.00); -- Nằm khoang 4 (Phụ thu tầng 1: 50k, tầng 2: 25k)

-- BangGiaCoDinh: Giá cố định cho TÀU NHANH (SE) theo cặp ga — Tham khảo bảng giá VNR 2024
-- Nguyên tắc: Giá giống nhau cả 2 chiều, Function tự tra cả chiều ngược
INSERT INTO vantai.BangGiaCoDinh (LoaiToa, MaGaDi, MaGaDen, GiaVe, PhuThuTangGiuong)
SELECT r.LoaiToa, gDi.MaGa, gDen.MaGa, r.GiaVe, r.PhuThu
FROM (VALUES
    -- NML (Ngồi mềm điều hòa) — Các chặng chính tàu SE
    ('NML', 'HNI', 'VIN',   362000,  0),     -- Hà Nội → Vinh (319 km)
    ('NML', 'HNI', 'HUE',   616000,  0),     -- Hà Nội → Huế (688 km)
    ('NML', 'HNI', 'DAN',   683000,  0),     -- Hà Nội → Đà Nẵng (791 km)
    ('NML', 'HNI', 'NTR',  1042000,  0),     -- Hà Nội → Nha Trang (1315 km)
    ('NML', 'HNI', 'SGO',  1264000,  0),     -- Hà Nội → Sài Gòn (1726 km)
    ('NML', 'VIN', 'HUE',   375000,  0),     -- Vinh → Huế (369 km)
    ('NML', 'VIN', 'DAN',   448000,  0),     -- Vinh → Đà Nẵng (472 km)
    ('NML', 'HUE', 'DAN',   138000,  0),     -- Huế → Đà Nẵng (103 km)
    ('NML', 'HUE', 'SGO',   796000,  0),     -- Huế → Sài Gòn (1038 km)
    ('NML', 'DAN', 'NTR',   486000,  0),     -- Đà Nẵng → Nha Trang (524 km)
    ('NML', 'DAN', 'SGO',   738000,  0),     -- Đà Nẵng → Sài Gòn (935 km)
    ('NML', 'NTR', 'SGO',   392000,  0),     -- Nha Trang → Sài Gòn (411 km)

    -- NC (Ngồi cứng) — Rẻ hơn NML khoảng 25-30%
    ('NC', 'HNI', 'VIN',    253000,  0),
    ('NC', 'HNI', 'HUE',    431000,  0),
    ('NC', 'HNI', 'DAN',    478000,  0),
    ('NC', 'HNI', 'NTR',    729000,  0),
    ('NC', 'HNI', 'SGO',    885000,  0),
    ('NC', 'VIN', 'HUE',    263000,  0),
    ('NC', 'VIN', 'DAN',    314000,  0),
    ('NC', 'HUE', 'DAN',     97000,  0),
    ('NC', 'HUE', 'SGO',    557000,  0),
    ('NC', 'DAN', 'NTR',    340000,  0),
    ('NC', 'DAN', 'SGO',    516000,  0),
    ('NC', 'NTR', 'SGO',    274000,  0),

    -- BN (Nằm khoang 6 giường) — Đắt hơn NML khoảng 30%, phụ thu giường tầng 1
    ('BN', 'HNI', 'VIN',    471000, 30000),
    ('BN', 'HNI', 'HUE',    801000, 30000),
    ('BN', 'HNI', 'DAN',    888000, 30000),
    ('BN', 'HNI', 'NTR',   1355000, 30000),
    ('BN', 'HNI', 'SGO',   1643000, 30000),
    ('BN', 'VIN', 'HUE',    488000, 30000),
    ('BN', 'VIN', 'DAN',    582000, 30000),
    ('BN', 'HUE', 'DAN',    179000, 30000),
    ('BN', 'HUE', 'SGO',   1035000, 30000),
    ('BN', 'DAN', 'NTR',    632000, 30000),
    ('BN', 'DAN', 'SGO',    959000, 30000),
    ('BN', 'NTR', 'SGO',    510000, 30000),

    -- AN (Nằm VIP khoang 4) — Đắt nhất, phụ thu giường tầng 1
    ('AN', 'HNI', 'VIN',    598000, 50000),
    ('AN', 'HNI', 'HUE',   1017000, 50000),
    ('AN', 'HNI', 'DAN',   1128000, 50000),
    ('AN', 'HNI', 'NTR',   1721000, 50000),
    ('AN', 'HNI', 'SGO',   2087000, 50000),
    ('AN', 'VIN', 'HUE',    619000, 50000),
    ('AN', 'VIN', 'DAN',    740000, 50000),
    ('AN', 'HUE', 'DAN',    228000, 50000),
    ('AN', 'HUE', 'SGO',   1314000, 50000),
    ('AN', 'DAN', 'NTR',    803000, 50000),
    ('AN', 'DAN', 'SGO',   1218000, 50000),
    ('AN', 'NTR', 'SGO',    647000, 50000)
) r(LoaiToa, MaDi, MaCuoi, GiaVe, PhuThu)
JOIN hatang.Ga gDi ON r.MaDi = gDi.MaGaCode
JOIN hatang.Ga gDen ON r.MaCuoi = gDen.MaGaCode;

INSERT INTO vantai.LoaiHangHoa (TenHangHoa, NhomCuoc, DonGiaMoiTanKm) VALUES
(N'Hàng tiêu dùng đóng gói', 1, 450.00),
(N'Nông sản đông lạnh', 2, 600.00),
(N'Xe máy nguyên chiếc', 3, 1200.00),
(N'Hàng công nghiệp nặng / Máy móc', 3, 850.00),
(N'Linh kiện điện tử / Thiết bị công nghệ', 2, 750.00),
(N'Bưu phẩm bưu chính / Chuyển phát nhanh', 1, 550.00);
GO

-- 5.6. Phương tiện: Đầu máy & Toa xe thực tế VNR
INSERT INTO phuongtien.DongDauMay (MaDongCode, NhaSanXuat, CongSuatHP, TocDoToiDaKmh, SucKeoToiDaTan, DungTichBonDauLit, TrongLuongTan, ChieuDaiM) VALUES
('D19E', N'CSR Ziyang (Trung Quốc)', 1900, 120, 1200.0, 4000, 78.0, 16.5),
('D13E', N'CKD Praha (Tiệp Khắc)', 1350, 100, 900.0, 3200, 84.0, 15.8);

INSERT INTO phuongtien.DauMay (SoHieuDauMay, MaDongDauMay, DonViQuanLy, NamSanXuat, SoKmTichLuy, TrangThai)
SELECT 'D19E-901', MaDongDauMay, N'Xí nghiệp Đầu máy Hà Nội', 2015, 125000.0, 'SAN_SANG'
FROM phuongtien.DongDauMay WHERE MaDongCode = 'D19E'
UNION ALL
SELECT 'D19E-902', MaDongDauMay, N'Xí nghiệp Đầu máy Hà Nội', 2012, 148500.0, 'SAN_SANG' -- Sắp chạm mốc 150.000 km cần bảo dưỡng R2
FROM phuongtien.DongDauMay WHERE MaDongCode = 'D19E'
UNION ALL
SELECT 'D19E-903', MaDongDauMay, N'Xí nghiệp Đầu máy Đà Nẵng', 2016, 92000.0, 'SAN_SANG' -- Đầu máy đẩy đèo Hải Vân
FROM phuongtien.DongDauMay WHERE MaDongCode = 'D19E'
UNION ALL
SELECT 'D13E-701', MaDongDauMay, N'Xí nghiệp Đầu máy Sài Gòn', 2008, 210000.0, 'SAN_SANG'
FROM phuongtien.DongDauMay WHERE MaDongCode = 'D13E';

INSERT INTO phuongtien.ChungLoaiToa (MaChungLoaiCode, TenMoTa, ChieuDaiChuanM, SoTruc, SucChuaChuan) VALUES
('NC',  N'Toa ngồi cứng thông thường', 20.0, 4, 80),
('NML', N'Toa ngồi mềm điều hòa không khí', 20.0, 4, 64),
('BN',  N'Toa nằm khoang 6 giường điều hòa', 20.0, 4, 42),
('AN',  N'Toa nằm khoang 4 giường điều hòa', 20.0, 4, 28),
('G',   N'Toa xe hàng mui kín chuyên dụng', 15.5, 4, 0),
('M',   N'Toa chuyên chở xe máy 2 tầng', 19.5, 4, 0),
('P',   N'Toa xe mặt võng chở Container', 14.2, 4, 0);

INSERT INTO phuongtien.ToaXe (SoHieuToaXe, MaChungLoai, TuTrongTan, TaiTrongToiDaTan, TrangThai)
SELECT 'NC-101', MaChungLoai, 35.5, 12.0, 'SAN_SANG' FROM phuongtien.ChungLoaiToa WHERE MaChungLoaiCode = 'NC'
UNION ALL
SELECT 'NML-201', MaChungLoai, 36.0, 10.0, 'SAN_SANG' FROM phuongtien.ChungLoaiToa WHERE MaChungLoaiCode = 'NML'
UNION ALL
SELECT 'BN-301', MaChungLoai, 38.0, 8.0, 'SAN_SANG' FROM phuongtien.ChungLoaiToa WHERE MaChungLoaiCode = 'BN'
UNION ALL
SELECT 'AN-401', MaChungLoai, 39.0, 6.0, 'SAN_SANG' FROM phuongtien.ChungLoaiToa WHERE MaChungLoaiCode = 'AN'
UNION ALL
SELECT 'M-601', MaChungLoai, 25.0, 20.0, 'SAN_SANG' FROM phuongtien.ChungLoaiToa WHERE MaChungLoaiCode = 'M'
UNION ALL
SELECT 'P-701', MaChungLoai, 22.0, 35.0, 'SAN_SANG' FROM phuongtien.ChungLoaiToa WHERE MaChungLoaiCode = 'P';
GO

-- 5.7. Mác tàu, Chuyến tàu mẫu (SE1-SE4, SE19, SE20, NA1, HBN1)
DECLARE @GaHN INT, @GaSG INT, @GaDAN INT, @GaVIN INT, @GaGBA INT, @GaSTH INT, @GaBTH INT, @GaNTR INT, @GaDHO INT, @GaHUE INT, @GaPLY INT, @GaTHA INT, @GaNDH INT, @GaNBH INT, @GaDTR INT, @GaBHO INT, @GaLCO INT;
SELECT @GaHN  = MaGa FROM hatang.Ga WHERE MaGaCode = 'HNI';
SELECT @GaGBA = MaGa FROM hatang.Ga WHERE MaGaCode = 'GBA';
SELECT @GaPLY = MaGa FROM hatang.Ga WHERE MaGaCode = 'PLY';
SELECT @GaNDH = MaGa FROM hatang.Ga WHERE MaGaCode = 'NDH';
SELECT @GaNBH = MaGa FROM hatang.Ga WHERE MaGaCode = 'NBH';
SELECT @GaTHA = MaGa FROM hatang.Ga WHERE MaGaCode = 'THA';
SELECT @GaVIN = MaGa FROM hatang.Ga WHERE MaGaCode = 'VIN';
SELECT @GaDHO = MaGa FROM hatang.Ga WHERE MaGaCode = 'DHO';
SELECT @GaHUE = MaGa FROM hatang.Ga WHERE MaGaCode = 'HUE';
SELECT @GaLCO = MaGa FROM hatang.Ga WHERE MaGaCode = 'LCO';
SELECT @GaDAN = MaGa FROM hatang.Ga WHERE MaGaCode = 'DAN';
SELECT @GaDTR = MaGa FROM hatang.Ga WHERE MaGaCode = 'DTR';
SELECT @GaNTR = MaGa FROM hatang.Ga WHERE MaGaCode = 'NTR';
SELECT @GaBTH = MaGa FROM hatang.Ga WHERE MaGaCode = 'BTH';
SELECT @GaBHO = MaGa FROM hatang.Ga WHERE MaGaCode = 'BHO';
SELECT @GaSTH = MaGa FROM hatang.Ga WHERE MaGaCode = 'STH';
SELECT @GaSG  = MaGa FROM hatang.Ga WHERE MaGaCode = 'SGO';

INSERT INTO vanhanh.DinhMucVanToc (MaLoaiTau, TenLoaiTau, VanTocDinhMucKmh, MoTaQuyChuan, MauNhanDien, ThuTuHienThi, DangApDung) VALUES
('TAU_NHANH', N'Tàu Nhanh Thống Nhất (SE)', 53.00, N'Tốc độ lữ hành định mức VNR Thống Nhất (31-33h Bắc-Nam)', '#0284C7', 1, 1),
('TAU_CHO',   N'Tàu Khách Khu Đoạn (NA, LP)', 42.00, N'Tốc độ lữ hành đỗ đón khách nhiều ga địa phương (40-42h Bắc-Nam)', '#D97706', 2, 1),
('TAU_HANG',  N'Tàu Hàng Chuyên Tuyến',       32.00, N'Tốc độ lữ hành bao gồm thời gian dồn toa và dừng tránh tàu khách', '#475569', 3, 1);
INSERT INTO vanhanh.MacTauMau (SoHieuMacTau, LoaiTau, HuongChay, MaGaDi, MaGaDen, ThoiGianChuanGio, MucUuTien) VALUES
('SE1',  'TAU_NHANH',  'BAC_NAM', @GaHN,  @GaSG,  32.5, 1),
('SE2',  'TAU_NHANH',  'NAM_BAC', @GaSG,  @GaHN,  32.5, 1),
('SE3',  'TAU_NHANH',  'BAC_NAM', @GaHN,  @GaSG,  32.5, 2),
('SE4',  'TAU_NHANH',  'NAM_BAC', @GaSG,  @GaHN,  32.5, 1),
('SE19', 'TAU_NHANH',  'BAC_NAM', @GaHN,  @GaDAN, 15.5, 2),
('SE20', 'TAU_NHANH',  'NAM_BAC', @GaDAN, @GaHN,  15.5, 2),
('NA1',  'TAU_CHO',   'BAC_NAM', @GaHN,  @GaVIN, 6.5,  3),
('HBN1', 'TAU_HANG',   'BAC_NAM', @GaGBA, @GaSTH, 29.0, 4);

DECLARE @MaSE1 INT, @MaSE2 INT, @MaSE3 INT, @MaSE4 INT, @MaSE19 INT, @MaNA1 INT, @MaHBN1 INT;
SELECT @MaSE1  = MaMacTau FROM vanhanh.MacTauMau WHERE SoHieuMacTau = 'SE1';
SELECT @MaSE2  = MaMacTau FROM vanhanh.MacTauMau WHERE SoHieuMacTau = 'SE2';
SELECT @MaSE3  = MaMacTau FROM vanhanh.MacTauMau WHERE SoHieuMacTau = 'SE3';
SELECT @MaSE4  = MaMacTau FROM vanhanh.MacTauMau WHERE SoHieuMacTau = 'SE4';
SELECT @MaSE19 = MaMacTau FROM vanhanh.MacTauMau WHERE SoHieuMacTau = 'SE19';
SELECT @MaNA1  = MaMacTau FROM vanhanh.MacTauMau WHERE SoHieuMacTau = 'NA1';
SELECT @MaHBN1 = MaMacTau FROM vanhanh.MacTauMau WHERE SoHieuMacTau = 'HBN1';

DECLARE @NgayNay DATE = CAST(SYSDATETIME() AS DATE);   -- [tron goi] ngay theo gio may
DECLARE @NgayMai DATE = DATEADD(DAY, 1, @NgayNay);
DECLARE @HomQua DATE = DATEADD(DAY, -1, @NgayNay);

-- Chuyến 1: SE1 hôm nay - Đang chạy (Hiện ở khu gian Lăng Cô - Đà Nẵng)
INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, GioXuatPhatTT, SoPhutTreLuyKe, TrangThai)
VALUES (@MaSE1, @NgayNay, 
        DATEADD(HOUR, 6, CAST(@NgayNay AS DATETIME2)), 
        DATEADD(HOUR, 38, CAST(@NgayNay AS DATETIME2)),
        DATEADD(HOUR, 6, CAST(@NgayNay AS DATETIME2)),
        10, 'DANG_CHAY');
DECLARE @CtSE1 INT = SCOPE_IDENTITY();

-- Chuyến 2: SE2 hôm qua - Đang chạy ngược chiều Nam - Bắc (Hiện ở khu gian Vinh - Thanh Hóa)
INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, GioXuatPhatTT, SoPhutTreLuyKe, TrangThai)
VALUES (@MaSE2, @HomQua, 
        DATEADD(HOUR, 19, CAST(@HomQua AS DATETIME2)), 
        DATEADD(HOUR, 51, CAST(@HomQua AS DATETIME2)),
        DATEADD(HOUR, 19, CAST(@HomQua AS DATETIME2)),
        45, 'DANG_CHAY');
DECLARE @CtSE2 INT = SCOPE_IDENTITY();

-- Chuyến 3: SE3 ngày mai - Đã lên lịch
INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, TrangThai)
VALUES (@MaSE3, @NgayMai, 
        DATEADD(HOUR, 19, CAST(@NgayMai AS DATETIME2)), 
        DATEADD(HOUR, 35, CAST(@NgayMai AS DATETIME2)),
        'DA_LEN_LICH');
DECLARE @CtSE3 INT = SCOPE_IDENTITY();

-- Chuyến 4: SE4 hôm nay - Đang chạy Nam - Bắc (Hiện ở khu gian Nha Trang - Diêu Trì)
INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, GioXuatPhatTT, SoPhutTreLuyKe, TrangThai)
VALUES (@MaSE4, @NgayNay,
        DATEADD(HOUR, 19, CAST(@HomQua AS DATETIME2)),
        DATEADD(HOUR, 51, CAST(@HomQua AS DATETIME2)),
        DATEADD(HOUR, 19, CAST(@HomQua AS DATETIME2)),
        15, 'DANG_CHAY');
DECLARE @CtSE4 INT = SCOPE_IDENTITY();

-- Chuyến 5: SE19 tối nay - Sẵn sàng xuất bến tại ga Hà Nội
INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, SoPhutTreLuyKe, TrangThai)
VALUES (@MaSE19, @NgayNay,
        DATEADD(MINUTE, 1190, CAST(@NgayNay AS DATETIME2)),
        DATEADD(MINUTE, 2180, CAST(@NgayNay AS DATETIME2)),
        0, 'SAN_SANG');
DECLARE @CtSE19 INT = SCOPE_IDENTITY();

-- Chuyến 6: NA1 đêm nay - Đã lên lịch (Hà Nội -> Vinh)
INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, TrangThai)
VALUES (@MaNA1, @NgayNay,
        DATEADD(MINUTE, 1335, CAST(@NgayNay AS DATETIME2)),
        DATEADD(MINUTE, 1780, CAST(@NgayNay AS DATETIME2)),
        'DA_LEN_LICH');
DECLARE @CtNA1 INT = SCOPE_IDENTITY();

-- Chuyến 7: SE1 hôm qua - Đã hoàn thành toàn bộ hành trình
INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, GioXuatPhatTT, GioVeDichTT, SoPhutTreLuyKe, TrangThai)
VALUES (@MaSE1, @HomQua,
        DATEADD(HOUR, 6, CAST(@HomQua AS DATETIME2)),
        DATEADD(HOUR, 38, CAST(@HomQua AS DATETIME2)),
        DATEADD(HOUR, 6, CAST(@HomQua AS DATETIME2)),
        DATEADD(MINUTE, 2305, CAST(@HomQua AS DATETIME2)),
        25, 'HOAN_THANH');

-- Chuyến 8: HBN1 hôm nay - Tàu hàng chuyên tuyến Bắc - Nam (Giáp Bát -> Sóng Thần)
INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, GioXuatPhatTT, SoPhutTreLuyKe, TrangThai)
VALUES (@MaHBN1, @NgayNay,
        DATEADD(HOUR, 4, CAST(@NgayNay AS DATETIME2)),
        DATEADD(HOUR, 33, CAST(@NgayNay AS DATETIME2)),
        DATEADD(HOUR, 4, CAST(@NgayNay AS DATETIME2)),
        0, 'DANG_CHAY');
DECLARE @CtHBN1 INT = SCOPE_IDENTITY();

-- Lịch dừng ga cho SE1 (Đã qua Huế và Lăng Cô, đang chạy sang Đà Nẵng)
INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, GioDenThucTe, GioDiThucTe) VALUES
(@CtSE1, @GaHN,  1, DATEADD(HOUR, 6, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 370, CAST(@NgayNay AS DATETIME2)), DATEADD(HOUR, 6, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 370, CAST(@NgayNay AS DATETIME2))),
(@CtSE1, @GaPLY, 2, DATEADD(MINUTE, 430, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 435, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 430, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 435, CAST(@NgayNay AS DATETIME2))),
(@CtSE1, @GaVIN, 3, DATEADD(HOUR, 11, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 675, CAST(@NgayNay AS DATETIME2)), DATEADD(HOUR, 11, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 675, CAST(@NgayNay AS DATETIME2))),
(@CtSE1, @GaHUE, 4, DATEADD(HOUR, 18, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1095, CAST(@NgayNay AS DATETIME2)), DATEADD(HOUR, 18, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1095, CAST(@NgayNay AS DATETIME2))),
(@CtSE1, @GaLCO, 5, DATEADD(HOUR, 20, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1210, CAST(@NgayNay AS DATETIME2)), DATEADD(HOUR, 20, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1210, CAST(@NgayNay AS DATETIME2))),
(@CtSE1, @GaDAN, 6, DATEADD(MINUTE, 1275, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1290, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE1, @GaSG,  7, DATEADD(HOUR, 38, CAST(@NgayNay AS DATETIME2)), DATEADD(HOUR, 38, CAST(@NgayNay AS DATETIME2)), NULL, NULL);

-- Lịch dừng ga cho SE2 (Ngược chiều Nam - Bắc: Đã rời Vinh, đang trên đường đến Thanh Hóa)
INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, GioDenThucTe, GioDiThucTe) VALUES
(@CtSE2, @GaSG,  1, DATEADD(HOUR, 19, CAST(@HomQua AS DATETIME2)), DATEADD(MINUTE, 1150, CAST(@HomQua AS DATETIME2)), DATEADD(HOUR, 19, CAST(@HomQua AS DATETIME2)), DATEADD(MINUTE, 1150, CAST(@HomQua AS DATETIME2))),
(@CtSE2, @GaDAN, 2, DATEADD(HOUR, 10, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 615, CAST(@NgayNay AS DATETIME2)), DATEADD(HOUR, 10, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 615, CAST(@NgayNay AS DATETIME2))),
(@CtSE2, @GaVIN, 3, DATEADD(HOUR, 17, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1035, CAST(@NgayNay AS DATETIME2)), DATEADD(HOUR, 17, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1035, CAST(@NgayNay AS DATETIME2))),
(@CtSE2, @GaTHA, 4, DATEADD(HOUR, 20, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1210, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE2, @GaHN,  5, DATEADD(HOUR, 23, CAST(@NgayNay AS DATETIME2)), DATEADD(HOUR, 23, CAST(@NgayNay AS DATETIME2)), NULL, NULL);

-- Lịch dừng ga cho SE4 (Đã qua Nha Trang lúc sáng sớm, đang trên đường đến Diêu Trì)
INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, GioDenThucTe, GioDiThucTe) VALUES
(@CtSE4, @GaSG,  1, DATEADD(HOUR, 19, CAST(@HomQua AS DATETIME2)), DATEADD(MINUTE, 1155, CAST(@HomQua AS DATETIME2)), DATEADD(HOUR, 19, CAST(@HomQua AS DATETIME2)), DATEADD(MINUTE, 1155, CAST(@HomQua AS DATETIME2))),
(@CtSE4, @GaBTH, 2, DATEADD(MINUTE, 1395, CAST(@HomQua AS DATETIME2)), DATEADD(MINUTE, 1400, CAST(@HomQua AS DATETIME2)), DATEADD(MINUTE, 1405, CAST(@HomQua AS DATETIME2)), DATEADD(MINUTE, 1410, CAST(@HomQua AS DATETIME2))),
(@CtSE4, @GaNTR, 3, DATEADD(MINUTE, 260, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 275, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 275, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 290, CAST(@NgayNay AS DATETIME2))),
(@CtSE4, @GaDTR, 4, DATEADD(MINUTE, 520, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 530, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE4, @GaDAN, 5, DATEADD(MINUTE, 910, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 925, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE4, @GaVIN, 6, DATEADD(MINUTE, 1480, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1490, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE4, @GaHN,  7, DATEADD(MINUTE, 1890, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1890, CAST(@NgayNay AS DATETIME2)), NULL, NULL);

-- Lịch dừng ga cho SE19 (Tối nay xuất phát từ Hà Nội đi Đà Nẵng)
INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, GioDenThucTe, GioDiThucTe) VALUES
(@CtSE19, @GaHN,  1, DATEADD(MINUTE, 1170, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1190, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE19, @GaPLY, 2, DATEADD(MINUTE, 1245, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1248, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE19, @GaNDH, 3, DATEADD(MINUTE, 1278, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1283, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE19, @GaTHA, 4, DATEADD(MINUTE, 1365, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1370, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE19, @GaVIN, 5, DATEADD(MINUTE, 1550, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1560, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE19, @GaDHO, 6, DATEADD(MINUTE, 1810, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1818, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE19, @GaHUE, 7, DATEADD(MINUTE, 2000, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 2010, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtSE19, @GaDAN, 8, DATEADD(MINUTE, 2180, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 2180, CAST(@NgayNay AS DATETIME2)), NULL, NULL);

-- Lịch dừng ga cho NA1 (Tàu khu đoạn Hà Nội - Vinh đêm nay)
INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, GioDenThucTe, GioDiThucTe) VALUES
(@CtNA1, @GaHN,  1, DATEADD(MINUTE, 1315, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1335, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtNA1, @GaPLY, 2, DATEADD(MINUTE, 1392, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1395, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtNA1, @GaNDH, 3, DATEADD(MINUTE, 1425, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1430, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtNA1, @GaNBH, 4, DATEADD(MINUTE, 1460, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1463, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtNA1, @GaTHA, 5, DATEADD(MINUTE, 1525, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1530, CAST(@NgayNay AS DATETIME2)), NULL, NULL),
(@CtNA1, @GaVIN, 6, DATEADD(MINUTE, 1780, CAST(@NgayNay AS DATETIME2)), DATEADD(MINUTE, 1780, CAST(@NgayNay AS DATETIME2)), NULL, NULL);

-- Đoàn tàu
DECLARE @DauChinh INT, @DauDay INT, @Dau902 INT, @Dau701 INT;
SELECT @DauChinh = MaDauMay FROM phuongtien.DauMay WHERE SoHieuDauMay = 'D19E-901';
SELECT @DauDay   = MaDauMay FROM phuongtien.DauMay WHERE SoHieuDauMay = 'D19E-903';
SELECT @Dau902   = MaDauMay FROM phuongtien.DauMay WHERE SoHieuDauMay = 'D19E-902';
SELECT @Dau701   = MaDauMay FROM phuongtien.DauMay WHERE SoHieuDauMay = 'D13E-701';

INSERT INTO vanhanh.DoanTau (MaChuyenTau, MaDauMayChinh, MaDauMayDay, TongSoToa, TongChieuDaiM, TongTrongLuongTan, DaDuyetAnToan) VALUES
(@CtSE1,  @DauChinh, @DauDay, 14, 296.5, 580.0, 1),
(@CtSE2,  @Dau902,   NULL,    12, 256.5, 510.0, 1),
(@CtSE3,  @DauChinh, NULL,    10, 216.5, 430.0, 0), -- Chưa duyệt an toàn hãm
(@CtSE4,  @Dau902,   NULL,    13, 276.5, 540.0, 1),
(@CtSE19, @DauChinh, NULL,    11, 236.5, 460.0, 1),
(@CtNA1,  @Dau701,   NULL,     9, 196.5, 380.0, 1);
DECLARE @DoanSE1 INT, @DoanSE3 INT;
SELECT @DoanSE1 = MaDoanTau FROM vanhanh.DoanTau WHERE MaChuyenTau = @CtSE1;
SELECT @DoanSE3 = MaDoanTau FROM vanhanh.DoanTau WHERE MaChuyenTau = @CtSE3;

-- Cấu hình Toa xe khách & Chỗ ngồi
-- SE1: Toa 1 (AN), Toa 2 (NML), Toa 3 (BN)
INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua) VALUES
(@CtSE1, N'Toa 1 (AN)',  'AN',  1, 28),
(@CtSE1, N'Toa 2 (NML)', 'NML', 2, 64),
(@CtSE1, N'Toa 3 (BN)',  'BN',  3, 42);

DECLARE @ToaAN_SE1 INT, @ToaNML_SE1 INT, @ToaBN_SE1 INT;
SELECT @ToaAN_SE1  = MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @CtSE1 AND ThuTuToa = 1;
SELECT @ToaNML_SE1 = MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @CtSE1 AND ThuTuToa = 2;
SELECT @ToaBN_SE1  = MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @CtSE1 AND ThuTuToa = 3;

-- Sinh 28 chỗ cho Toa 1 AN (SE1)
INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT @ToaAN_SE1, n, ((n-1)%2)+1, 0
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28)) v(n);

-- Sinh 64 chỗ cho Toa 2 NML (SE1)
INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT @ToaNML_SE1, n, NULL, 0
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28),(29),(30),(31),(32),(33),(34),(35),(36),(37),(38),(39),(40),(41),(42),(43),(44),(45),(46),(47),(48),(49),(50),(51),(52),(53),(54),(55),(56),(57),(58),(59),(60),(61),(62),(63),(64)) v(n);

-- Sinh 42 chỗ cho Toa 3 BN (SE1)
INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT @ToaBN_SE1, n, ((n-1)%3)+1, 0
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28),(29),(30),(31),(32),(33),(34),(35),(36),(37),(38),(39),(40),(41),(42)) v(n);

-- SE3: Toa 1 NML, Toa 2 BN, Toa 3 AN
INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua) VALUES
(@CtSE3, N'Toa 1 (NML)', 'NML', 1, 64),
(@CtSE3, N'Toa 2 (BN)',  'BN',  2, 42),
(@CtSE3, N'Toa 3 (AN)',  'AN',  3, 28);
DECLARE @ToaBN_SE3 INT;
SELECT @ToaBN_SE3 = MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @CtSE3 AND LoaiToa = 'BN';

INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT @ToaBN_SE3, n, ((n-1)%3)+1, 0
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28),(29),(30),(31),(32),(33),(34),(35),(36),(37),(38),(39),(40),(41),(42)) v(n);

-- SE4: Toa 1 (AN), Toa 2 (NML)
INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua) VALUES
(@CtSE4, N'Toa 1 (AN)',  'AN',  1, 28),
(@CtSE4, N'Toa 2 (NML)', 'NML', 2, 64);
DECLARE @ToaAN_SE4 INT, @ToaNML_SE4 INT;
SELECT @ToaAN_SE4  = MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @CtSE4 AND ThuTuToa = 1;
SELECT @ToaNML_SE4 = MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @CtSE4 AND ThuTuToa = 2;

INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT @ToaAN_SE4, n, ((n-1)%2)+1, 0
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28)) v(n);

INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT @ToaNML_SE4, n, NULL, 0
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28),(29),(30),(31),(32),(33),(34),(35),(36),(37),(38),(39),(40),(41),(42),(43),(44),(45),(46),(47),(48),(49),(50),(51),(52),(53),(54),(55),(56),(57),(58),(59),(60),(61),(62),(63),(64)) v(n);

-- SE19: Toa 1 (AN), Toa 2 (BN)
INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua) VALUES
(@CtSE19, N'Toa 1 (AN)', 'AN', 1, 28),
(@CtSE19, N'Toa 2 (BN)', 'BN', 2, 42);
DECLARE @ToaAN_SE19 INT, @ToaBN_SE19 INT;
SELECT @ToaAN_SE19 = MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @CtSE19 AND ThuTuToa = 1;
SELECT @ToaBN_SE19 = MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @CtSE19 AND ThuTuToa = 2;

INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT @ToaAN_SE19, n, ((n-1)%2)+1, 0
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28)) v(n);

INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT @ToaBN_SE19, n, ((n-1)%3)+1, 0
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28),(29),(30),(31),(32),(33),(34),(35),(36),(37),(38),(39),(40),(41),(42)) v(n);

-- NA1: Toa 1 (NML)
INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua)
VALUES (@CtNA1, N'Toa 1 (NML)', 'NML', 1, 64);
DECLARE @ToaNML_NA1 INT = SCOPE_IDENTITY();

INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
SELECT @ToaNML_NA1, n, NULL, 0
FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28),(29),(30),(31),(32),(33),(34),(35),(36),(37),(38),(39),(40),(41),(42),(43),(44),(45),(46),(47),(48),(49),(50),(51),(52),(53),(54),(55),(56),(57),(58),(59),(60),(61),(62),(63),(64)) v(n);

-- Nhân viên bổ sung: NV_006 đến NV_013
INSERT INTO nhansu.NhanVien (MaNVCode, HoTen, SoDienThoai, ChucDanh, HangBangLai, HanKhamSucKhoe, DonViChuQuan, TrangThai) VALUES
('NV_006', N'Vũ Tiến Đạt',      '0906789012', N'Lái tàu', 'D19E', DATEADD(DAY, 15, CAST(SYSDATETIME() AS DATE)), N'Đội lái tàu Hà Nội',   'SAN_SANG'),
('NV_007', N'Phan Văn Hùng',     '0907123456', N'Lái tàu', 'D19E', '2027-10-15', N'Đội lái tàu Sài Gòn',  'SAN_SANG'),
('NV_008', N'Đặng Thế Anh',      '0908234567', N'Phụ lái', 'D19E', '2027-09-20', N'Đội lái tàu Sài Gòn',  'SAN_SANG'),
('NV_009', N'Bùi Xuân Khoa',     '0909345678', N'Trưởng tàu', NULL, '2028-01-10', N'Đoàn tiếp viên Phương Nam', 'SAN_SANG'),
('NV_010', N'Lê Đình Chiến',     '0910456789', N'Lái tàu', 'D19E', '2027-06-30', N'Đội lái tàu Vinh',     'SAN_SANG'),
('NV_011', N'Phạm Thành Long',   '0911567890', N'Phụ lái', 'D19E', '2027-08-12', N'Đội lái tàu Vinh',     'SAN_SANG'),
('NV_012', N'Ngô Tất Tố',        '0912678901', N'Lái tàu', 'D13E', '2027-11-25', N'Đội lái tàu Hà Nội',   'SAN_SANG'),
('NV_013', N'Hoàng Trọng Nghĩa', '0913789012', N'Phụ lái', 'D13E', '2027-12-05', N'Đội lái tàu Hà Nội',   'SAN_SANG');

-- Kíp lái các chuyến tàu
INSERT INTO nhansu.PhanCongKipLai (MaChuyenTau, MaLaiTau, MaPhuLai, MaTruongTau, MaGaNhanBan, MaGaBanGiao, TrangThai) VALUES
(@CtSE1,  1,  2, 3, @GaHN, @GaVIN, 'DANG_THUC_HIEN'),
(@CtSE4,  7,  8, 9, @GaSG, @GaNTR, 'DANG_THUC_HIEN'),
(@CtSE19, 10, 11, 3, @GaHN, @GaVIN, 'DA_PHAN_CONG'),
(@CtNA1,  12, 13, 9, @GaHN, @GaVIN, 'DA_PHAN_CONG');

-- Kiểm tra sức khỏe lái tàu trước ban
INSERT INTO nhansu.KiemTraSucKhoe (MaNhanVien, MaChuyenTau, NongDoConMgL, SoGioNghiTruocCa) VALUES
(1,  @CtSE1,  0.00, 10.0),
(2,  @CtSE1,  0.00, 9.5),
(3,  @CtSE1,  0.00, 12.0),
(6,  @CtSE3,  0.15, 6.0),  -- Vi phạm nồng độ cồn > 0 và nghỉ < 8 tiếng
(7,  @CtSE4,  0.00, 10.0),
(8,  @CtSE4,  0.00, 9.5),
(9,  @CtSE4,  0.00, 11.0),
(10, @CtSE19, 0.00, 10.5),
(11, @CtSE19, 0.00, 9.0),
(12, @CtNA1,  0.00, 11.5),
(13, @CtNA1,  0.00, 10.0);

-- Khám xe kỹ thuật
INSERT INTO baotri.KhamXeKyThuat (MaDoanTau, MaNguoiKham, ApLucHamBar, DoSutApBarPhut, DaCapNuoc, DaXaVeSinh, GhiChuKyThuat) VALUES
(@DoanSE1, 2, 5.0, 0.12, 1, 1, N'Đạt chuẩn an toàn kỹ thuật xuất bến.'),
(@DoanSE3, 2, 4.5, 0.28, 1, 1, N'Áp lực hãm yếu không đạt chuẩn 4.8 bar.');

-- Nhật ký cấp dầu
INSERT INTO baotri.NhatKyCapNhienLieu (MaDauMay, NoiCapDau, SoLitTraNap, TrongLuongKeoTan, CuLyChayKm, SuatTieuHaoSFC, CanhBaoVuotMuc, MaNguoiCap) VALUES
(@DauChinh, N'Trạm dầu Giáp Bát', 2800.0, 580.0, 688.0, 2.15, 0, 1),
(@Dau902,   N'Trạm dầu Sài Gòn',   3950.0, 510.0, 650.0, 3.25, 1, 1);

-- Nhật ký bảo dưỡng
INSERT INTO baotri.NhatKyBaoDuong (LoaiPhuongTien, MaDauMay, CapBaoDuong, SoKmTaiThoiDiem, GhiChuKyThuat, MaNguoiThucHien)
VALUES ('DAU_MAY', @Dau902, 'R1', 120000.0, N'Bảo dưỡng định kỳ cấp R1 hoàn thành đạt chuẩn.', 2);

-- Lệnh phong tỏa đèo Hải Vân
DECLARE @KgHaiVan INT;
SELECT @KgHaiVan = MaKhuGian FROM hatang.KhuGian WHERE MaGaDau = @GaLCO AND MaGaCuoi = @GaDAN;

INSERT INTO vanhanh.LenhPhongToa (MaKhuGian, ThoiGianBatDau, ThoiGianKetThuc, LyDo, TocDoHanCheKmh, MaNguoiDuyet)
VALUES (@KgHaiVan, 
        DATEADD(HOUR, 2, CAST(@NgayNay AS DATETIME2)), 
        DATEADD(HOUR, 5, CAST(@NgayNay AS DATETIME2)), 
        N'Thi công kích hầm gia cố chống sạt lở đèo Hải Vân', 0, 1);

-- Nhật ký chậm giờ SE2
INSERT INTO vanhanh.NhatKyChamGio (MaChuyenTau, MaGa, LoaiSuCo, SoPhutCham, GhiChuXuLy)
VALUES (@CtSE2, @GaVIN, 'CHO_TRANH_TAU', 45, N'Chờ tàu khách ưu tiên SE1 vượt tại ga tránh.');

-- Khách hàng
INSERT INTO vantai.KhachHang (HoTen, SoCCCD, SoDienThoai, Email, LoaiKhach) VALUES
(N'Trần Văn Nam',   '001200009999', '0988776655', 'nam.tv@gmail.com',       'THUONG'),
(N'Lê Hoàng Phúc',  '079201008888', '0912334455', 'phuc.lh@gmail.com',      'THUONG'),
(N'Đoàn Thầy Cô',   '001085007777', '0933221100', 'doan.gv@edu.vn',         'THUONG'),
(N'Nguyễn Thị Hằng', '036195006666', '0977889900', 'hang.nt@yahoo.com',     'THUONG'),
(N'Hoàng Minh Tuấn', '024201001111', '0915123456', 'tuan.hm@gmail.com',      'THUONG'),
(N'Đặng Thùy Trang', '031199002222', '0984234567', 'trang.dt@gmail.com',     'SINH_VIEN'),
(N'Nguyễn Hữu Thắng', '038080003333', '0973345678', 'thang.nh@gmail.com',    'NGUOI_GIA'),
(N'Vũ Mai Phương',   '001195004444', '0962456789', 'phuong.vm@fpt.com',     'THUONG'),
(N'Bùi Quang Huy',   '079092005555', '0908567890', 'huy.bq@vnr.vn',         'THUONG'),
(N'Lê Thanh Tùng',   '040188006666', '0937678901', 'tung.lt@yahoo.com',     'THUONG'),
(N'Phan Mỹ Linh',    '048202007777', '0946789012', 'linh.pm@student.edu.vn', 'SINH_VIEN'),
(N'Trần Đức Cường',  '001090008888', '0989890123', 'cuong.td@gmail.com',     'THUONG');

DECLARE @KhNam INT, @KhPhuc INT, @KhDoan INT, @KhHang INT, @KhTuan INT, @KhTrang INT, @KhThang INT, @KhPhuong INT, @KhHuy INT, @KhTung INT, @KhLinh INT, @KhCuong INT;
SELECT @KhNam    = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '001200009999';
SELECT @KhPhuc   = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '079201008888';
SELECT @KhDoan   = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '001085007777';
SELECT @KhHang   = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '036195006666';
SELECT @KhTuan   = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '024201001111';
SELECT @KhTrang  = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '031199002222';
SELECT @KhThang  = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '038080003333';
SELECT @KhPhuong = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '001195004444';
SELECT @KhHuy    = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '079092005555';
SELECT @KhTung   = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '040188006666';
SELECT @KhLinh   = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '048202007777';
SELECT @KhCuong  = MaKhachHang FROM vantai.KhachHang WHERE SoCCCD = '001090008888';

-- Đơn 1: Khách Nam (SE1: Hà Nội -> Vinh, Giường 1 Toa 1)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE1_HN_VIN', @KhNam, 1, 629000.00, 'CHUYEN_KHOAN', 'DA_THANH_TOAN', 3);
DECLARE @DonNam INT = SCOPE_IDENTITY();

DECLARE @Ghe1_AN_SE1 INT, @Ghe3_AN_SE1 INT;
SELECT @Ghe1_AN_SE1 = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaAN_SE1 AND SoGhe = 1;
SELECT @Ghe3_AN_SE1 = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaAN_SE1 AND SoGhe = 3;

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
VALUES (@DonNam, 'TK_SE1_HN_VIN_01', @CtSE1, @Ghe1_AN_SE1, @GaHN, @GaVIN, N'Trần Văn Nam', '001200009999', 629000.00, 0.00, 'DA_DAT');

-- Đơn 2: Khách Phúc (SE1: Vinh -> Sài Gòn, cùng Giường 1 Toa 1 - Chặng nối tiếp)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE1_VIN_SG', @KhPhuc, 1, 2261000.00, 'VI_DIEN_TU', 'DA_THANH_TOAN', 3);
DECLARE @DonPhuc INT = SCOPE_IDENTITY();

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
VALUES (@DonPhuc, 'TK_SE1_VIN_SG_01', @CtSE1, @Ghe1_AN_SE1, @GaVIN, @GaSG, N'Lê Hoàng Phúc', '079201008888', 2261000.00, 0.00, 'DA_DAT');

-- Đơn 3: Đoàn Thầy Cô (5 vé chuyến SE3)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE3_GROUP_05', @KhDoan, 5, 5250000.00, 'THE_NGAN_HANG', 'DA_THANH_TOAN', 3);

-- Đơn 4: Khách Hằng (SE3 - Đã hoàn vé)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE3_REFUND_01', @KhHang, 1, 850000.00, 'CHUYEN_KHOAN', 'DA_HOAN_TIEN', 3);
DECLARE @DonHang INT = SCOPE_IDENTITY();

DECLARE @Ghe2_BN_SE3 INT;
SELECT @Ghe2_BN_SE3 = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaBN_SE3 AND SoGhe = 2;

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
VALUES (@DonHang, 'TK_SE3_REFUND_01', @CtSE3, @Ghe2_BN_SE3, @GaHN, @GaDAN, N'Nguyễn Thị Hằng', '036195006666', 850000.00, 0.00, 'DA_HOAN_VE');
DECLARE @VeHoan INT = SCOPE_IDENTITY();

INSERT INTO vantai.HoanHuyVe (MaVe, ThoiDiemHoan, TienVeGoc, TyLeLePhi, HinhThucHoan, LyDoHoan, MaNhanVienDuyet)
VALUES (@VeHoan, DATEADD(DAY, -2, SYSUTCDATETIME()), 850000.00, 0.20, 'CHUYEN_KHOAN', N'Khách có lịch công tác đột xuất đổi vé máy bay', 3);

-- Đơn 5: Khách Tuấn (SE1: Hà Nội -> Đà Nẵng, Ghế 10 Toa 2 NML)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE1_TUAN_DAN', @KhTuan, 1, 820000.00, 'CHUYEN_KHOAN', 'DA_THANH_TOAN', 3);
DECLARE @DonTuan INT = SCOPE_IDENTITY();

DECLARE @Ghe10_NML_SE1 INT, @Ghe12_NML_SE1 INT;
SELECT @Ghe10_NML_SE1 = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaNML_SE1 AND SoGhe = 10;
SELECT @Ghe12_NML_SE1 = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaNML_SE1 AND SoGhe = 12;

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
VALUES (@DonTuan, 'TK_SE1_HN_DAN_02', @CtSE1, @Ghe10_NML_SE1, @GaHN, @GaDAN, N'Hoàng Minh Tuấn', '024201001111', 820000.00, 0.00, 'DA_DAT');

-- Đơn 6: Khách Trang (Sinh viên, SE1: Nam Định -> Vinh, Ghế 12 Toa 2 NML, Giảm 50.000, Đã lên tàu)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE1_TRANG_VIN', @KhTrang, 1, 240000.00, 'VI_DIEN_TU', 'DA_THANH_TOAN', 3);
DECLARE @DonTrang INT = SCOPE_IDENTITY();

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
VALUES (@DonTrang, 'TK_SE1_ND_VIN_03', @CtSE1, @Ghe12_NML_SE1, @GaNDH, @GaVIN, N'Đặng Thùy Trang', '031199002222', 290000.00, 50000.00, 'DA_LEN_TAU');

-- Đơn 7: Khách Thắng (Người già, SE1: Hà Nội -> Sài Gòn, Giường 3 Toa 1 AN, Giảm 150.000, Đã lên tàu)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE1_THANG_SG', @KhThang, 1, 2590000.00, 'TIEN_MAT', 'DA_THANH_TOAN', 3);
DECLARE @DonThang INT = SCOPE_IDENTITY();

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
VALUES (@DonThang, 'TK_SE1_HN_SG_04', @CtSE1, @Ghe3_AN_SE1, @GaHN, @GaSG, N'Nguyễn Hữu Thắng', '038080003333', 2740000.00, 150000.00, 'DA_LEN_TAU');

-- Đơn 8: Khách Phương (SE4: Sài Gòn -> Nha Trang, 2 vé Giường 1 & 2 Toa 1 AN)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE4_PHUONG_NTR', @KhPhuong, 2, 1420000.00, 'CHUYEN_KHOAN', 'DA_THANH_TOAN', 3);
DECLARE @DonPhuong INT = SCOPE_IDENTITY();

DECLARE @Ghe1_AN_SE4 INT, @Ghe2_AN_SE4 INT, @Ghe15_NML_SE4 INT;
SELECT @Ghe1_AN_SE4  = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaAN_SE4 AND SoGhe = 1;
SELECT @Ghe2_AN_SE4  = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaAN_SE4 AND SoGhe = 2;
SELECT @Ghe15_NML_SE4 = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaNML_SE4 AND SoGhe = 15;

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai) VALUES
(@DonPhuong, 'TK_SE4_SG_NTR_01', @CtSE4, @Ghe1_AN_SE4, @GaSG, @GaNTR, N'Vũ Mai Phương', '001195004444', 710000.00, 0.00, 'DA_DAT'),
(@DonPhuong, 'TK_SE4_SG_NTR_02', @CtSE4, @Ghe2_AN_SE4, @GaSG, @GaNTR, N'Nguyễn Thu Hà',  '001196005555', 710000.00, 0.00, 'DA_DAT');

-- Đơn 9: Khách Huy (SE19: Hà Nội -> Đồng Hới, Giường 5 Toa 1 AN)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE19_HUY_DHO', @KhHuy, 1, 930000.00, 'VI_DIEN_TU', 'DA_THANH_TOAN', 3);
DECLARE @DonHuy INT = SCOPE_IDENTITY();

DECLARE @Ghe5_AN_SE19 INT, @Ghe1_BN_SE19 INT;
SELECT @Ghe5_AN_SE19 = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaAN_SE19 AND SoGhe = 5;
SELECT @Ghe1_BN_SE19 = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaBN_SE19 AND SoGhe = 1;

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
VALUES (@DonHuy, 'TK_SE19_HN_DHO_01', @CtSE19, @Ghe5_AN_SE19, @GaHN, @GaDHO, N'Bùi Quang Huy', '079092005555', 930000.00, 0.00, 'DA_DAT');

-- Đơn 10: Khách Tùng (NA1: Hà Nội -> Thanh Hóa, Ghế 20 Toa 1 NML)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_NA1_TUNG_THA', @KhTung, 1, 235000.00, 'TIEN_MAT', 'DA_THANH_TOAN', 3);
DECLARE @DonTung INT = SCOPE_IDENTITY();

DECLARE @Ghe20_NML_NA1 INT;
SELECT @Ghe20_NML_NA1 = MaChoNgoi FROM vantai.ChoNgoi WHERE MaToaXeKhach = @ToaNML_NA1 AND SoGhe = 20;

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
VALUES (@DonTung, 'TK_NA1_HN_THA_01', @CtNA1, @Ghe20_NML_NA1, @GaHN, @GaTHA, N'Lê Thanh Tùng', '040188006666', 235000.00, 0.00, 'DA_DAT');

-- Đơn 11: Khách Linh (Sinh viên, SE19: Hà Nội -> Huế, Giường 1 Toa 2 BN, Giảm 80.000)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE19_LINH_HUE', @KhLinh, 1, 845000.00, 'VI_DIEN_TU', 'DA_THANH_TOAN', 3);
DECLARE @DonLinh INT = SCOPE_IDENTITY();

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
VALUES (@DonLinh, 'TK_SE19_HN_HUE_01', @CtSE19, @Ghe1_BN_SE19, @GaHN, @GaHUE, N'Phan Mỹ Linh', '048202007777', 925000.00, 80000.00, 'DA_DAT');

-- Đơn 12: Khách Cường (SE4: Đà Nẵng -> Hà Nội, Ghế 15 Toa 2 NML - Đã hoàn vé)
INSERT INTO vantai.DonDatVe (MaPNR, MaKhachHang, SoLuongVe, TongTien, HinhThucTT, TrangThaiTT, MaNhanVienBan)
VALUES ('PNR_SE4_CUONG_REFUND', @KhCuong, 1, 650000.00, 'CHUYEN_KHOAN', 'DA_HOAN_TIEN', 3);
DECLARE @DonCuong INT = SCOPE_IDENTITY();

INSERT INTO vantai.Ve (MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi, MaGaDi, MaGaDen, TenHanhKhach, CCCDHanhKhach, GiaVeGoc, SoTienGiam, TrangThai)
VALUES (@DonCuong, 'TK_SE4_DAN_HN_01', @CtSE4, @Ghe15_NML_SE4, @GaDAN, @GaHN, N'Trần Đức Cường', '001090008888', 650000.00, 0.00, 'DA_HOAN_VE');
DECLARE @VeHoan2 INT = SCOPE_IDENTITY();

INSERT INTO vantai.HoanHuyVe (MaVe, ThoiDiemHoan, TienVeGoc, TyLeLePhi, HinhThucHoan, LyDoHoan, MaNhanVienDuyet)
VALUES (@VeHoan2, DATEADD(HOUR, -6, SYSUTCDATETIME()), 650000.00, 0.10, 'CHUYEN_KHOAN', N'Thay đổi lịch trình công tác sớm hơn', 3);

-- Biên lai phạt vé trên tàu
INSERT INTO vantai.BienLaiPhatVe (MaVe, MaChuyenTau, TenNguoiViPham, LyDoViPham, CuLyVuotKm, TienVeBoSung, TienPhat, MaNguoiLap) VALUES
(1,    @CtSE1, N'Nguyễn Văn Tèo', N'Đi quá chặng (Mua vé HN-Phủ Lý nhưng ngồi đến Nam Định)', 31.00, 50000.00, 100000.00, 1),
(NULL, @CtSE1, N'Trần Thị Nụ',    N'Không vé trốn lên tàu tại ga Phủ Lý',                    0.00, 120000.00, 200000.00, 1),
(NULL, @CtSE4, N'Đỗ Văn Toàn',    N'Đi quá chặng (Ngủ quên vượt từ ga Đồng Hới đến ga Hà Nội)', 522.00, 450000.00, 300000.00, 1);

-- Vận đơn hàng hóa thực tế
DECLARE @LoaiTieuDung INT, @LoaiNongSan INT, @LoaiXeMay INT, @LoaiCongNghiep INT, @LoaiDienTu INT, @LoaiBuuChinh INT;
SELECT @LoaiTieuDung   = MaLoaiHang FROM vantai.LoaiHangHoa WHERE TenHangHoa LIKE N'%tiêu dùng%';
SELECT @LoaiNongSan    = MaLoaiHang FROM vantai.LoaiHangHoa WHERE TenHangHoa LIKE N'%Nông sản%';
SELECT @LoaiXeMay      = MaLoaiHang FROM vantai.LoaiHangHoa WHERE TenHangHoa = N'Xe máy nguyên chiếc';
SELECT @LoaiCongNghiep = MaLoaiHang FROM vantai.LoaiHangHoa WHERE NhomCuoc = 3 AND TenHangHoa LIKE N'%công nghiệp%';
SELECT @LoaiDienTu     = MaLoaiHang FROM vantai.LoaiHangHoa WHERE TenHangHoa LIKE N'%điện tử%';
SELECT @LoaiBuuChinh   = MaLoaiHang FROM vantai.LoaiHangHoa WHERE TenHangHoa LIKE N'%Bưu phẩm%';

INSERT INTO vantai.VanDonHang (MaVanDonCode, TenNguoiGui, SDTNguoiGui, TenNguoiNhan, SDTNguoiNhan, MaGaGui, MaGaNhan, MaLoaiHang, TrongLuongTan, CuocPhi, LoaiVanChuyen, BienKiemSoat, DaRutXang, SoHieuContainer, SoChiHaiQuan, TrangThai) VALUES
-- Xe máy
('VD_XM_CHUA_RUT_XANG', N'Phạm Văn Hùng', '0911223344', N'Lê Thị Lan',   '0922334455', @GaHN,  @GaDAN, @LoaiXeMay, 0.15,   450000.00, 'XE_MAY', '29B1-888.88', 0, NULL, NULL, 'DA_XEP'),
('VD_XM_DA_RUT_XANG',   N'Ngô Văn Tuấn',  '0933445566', N'Ngô Gia Bảo',  '0944556677', @GaHN,  @GaSG,  @LoaiXeMay, 0.18,   950000.00, 'XE_MAY', '43D1-999.99', 1, NULL, NULL, 'DANG_VAN_CHUYEN'),
('VD_XM_SH_HN_SG',      N'Đỗ Quốc Anh',   '0978112233', N'Đỗ Minh Châu', '0989223344', @GaHN,  @GaSG,  @LoaiXeMay, 0.16,  1250000.00, 'XE_MAY', '29G1-777.99', 1, NULL, NULL, 'DA_DEN'),
('VD_XM_EXCITER_CHO',   N'Vũ Hải Đăng',   '0905334455', N'Trần Văn An',  '0916445566', @GaGBA, @GaNTR, @LoaiXeMay, 0.13,   750000.00, 'XE_MAY', '29E2-666.88', 0, NULL, NULL, 'DA_XEP'),
('VD_XM_PKL_SG_DAN',    N'Lâm Gia Huy',   '0932556677', N'Hoàng Nam',    '0943667788', @GaSG,  @GaDAN, @LoaiXeMay, 0.28,  2800000.00, 'XE_MAY', '59A3-999.99', 1, NULL, NULL, 'DANG_VAN_CHUYEN'),

-- Container
('VD_CONT_Y_8899',       N'Công ty CP Vận tải Ratraco', '0243888999', N'Tiếp vận Sóng Thần',   '0283777888', @GaGBA, @GaSTH, @LoaiCongNghiep, 24.50, 18500000.00, 'CONTAINER', NULL, NULL, 'CONT-VNR-8899',   'HQ-VN-998877', 'DANG_VAN_CHUYEN'),
('VD_CONT_THANH_LONG',   N'Hợp tác xã Nông sản Hàm Thuận', '02523888777', N'Tổng kho Nông sản Bắc Ninh', '0243888666', @GaBTH, @GaGBA, @LoaiNongSan, 26.00, 28500000.00, 'CONTAINER', NULL, NULL, 'CONT-COLD-5566',  'HQ-VN-334455', 'DANG_VAN_CHUYEN'),
('VD_CONT_SAMSUNG',      N'Tập đoàn Điện tử Samsung VN', '02223888999', N'Trung tâm Phân phối SEHC Sài Gòn', '0283888555', @GaGBA, @GaSTH, @LoaiDienTu, 22.00, 21000000.00, 'CONTAINER', NULL, NULL, 'CONT-VNR-7722',   'HQ-VN-112233', 'DA_GIAO'),

-- Hàng hóa thông thường
('VD_NHOM3_DAN_SGO',     N'Tập đoàn Thép Hòa Phát',     '02363555666', N'Tổng kho Tân Cảng Sài Gòn', '0283888777', @GaDAN, @GaSG,  @LoaiCongNghiep, 55.00, 43700000.00, 'HANG_HOA', NULL, NULL, NULL, NULL, 'DA_GIAO'),
('VD_BUU_CHINH_VIETTEL', N'Tổng Công ty Bưu chính Viettel', '02462660000', N'Trung tâm Khai thác Bưu chính Nam Bộ', '02862660000', @GaGBA, @GaSTH, @LoaiBuuChinh, 3.50, 4200000.00, 'HANG_HOA', NULL, NULL, NULL, NULL, 'DA_XEP'),
('VD_GAO_ST25_SG_HN',    N'Công ty TNHH Lúa Vàng Phương Nam', '0283777999', N'Hệ thống Siêu thị Hà Nội Mart', '0243999888', @GaSG,  @GaHN,  @LoaiNongSan, 12.00, 9800000.00, 'HANG_HOA', NULL, NULL, NULL, NULL, 'DA_GIAO');
GO


/* =========================================================================
   6. NGÂN HÀNG TRUY VẤN NGHIỆP VỤ & BẢO VỆ HỆ THỐNG (SQL QUERY BANK)
   (Đáp ứng 100% các câu hỏi kiểm tra đồ án của Thầy giáo / Hội đồng)
   ========================================================================= */

-- -------------------------------------------------------------------------
-- NHÓM 1: VÉ & GIÁ VÉ
-- -------------------------------------------------------------------------

-- Q1_1: Giá vé loại toa NML đi từ ga Vinh đến ga Huế là bao nhiêu?
-- Đáp án: Tàu SE dùng giá cố định từ bảng BangGiaCoDinh; Tàu Chợ dùng công thức km
SELECT 
    'NML' AS LoaiToa,
    gDi.TenGa AS GaDi, gDen.TenGa AS GaDen,
    ABS(gDen.LyTrinhKm - gDi.LyTrinhKm) AS CuLyKm,
    vantai.fn_TinhGiaVe('NML', gDi.MaGa, gDen.MaGa, NULL, 'TAU_NHANH') AS GiaVeSE,
    vantai.fn_TinhGiaVe('NML', gDi.MaGa, gDen.MaGa, NULL, 'TAU_CHO') AS GiaVeTauCho
FROM hatang.Ga gDi, hatang.Ga gDen
WHERE gDi.MaGaCode = 'VIN' AND gDen.MaGaCode = 'HUE';

-- Q1_2: Toa giường nằm khoang 6 (BN) của chuyến SE3 ngày mai còn bao nhiêu chỗ trống đoạn Hà Nội – Đà Nẵng?
DECLARE @ChuyenSE3 INT, @ToaBN INT, @KmHN DECIMAL(7,2), @KmDAN DECIMAL(7,2);
SELECT @ChuyenSE3 = ct.MaChuyenTau FROM vanhanh.ChuyenTau ct JOIN vanhanh.MacTauMau mm ON ct.MaMacTau = mm.MaMacTau WHERE mm.SoHieuMacTau = 'SE3';
SELECT @ToaBN = MaToaXeKhach FROM vantai.ToaXeKhach WHERE MaChuyenTau = @ChuyenSE3 AND LoaiToa = 'BN';
SELECT @KmHN = LyTrinhKm FROM hatang.Ga WHERE MaGaCode = 'HNI';
SELECT @KmDAN = LyTrinhKm FROM hatang.Ga WHERE MaGaCode = 'DAN';

SELECT 
    COUNT(cn.MaChoNgoi) AS TongSoChoKhoang6,
    COUNT(CASE WHEN v.MaVe IS NULL THEN 1 END) AS SoChoTrongDoanHN_DAN
FROM vantai.ChoNgoi cn
LEFT JOIN vantai.Ve v ON cn.MaChoNgoi = v.MaChoNgoi 
    AND v.MaChuyenTau = @ChuyenSE3
    AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
LEFT JOIN hatang.Ga gDi ON v.MaGaDi = gDi.MaGa
LEFT JOIN hatang.Ga gDen ON v.MaGaDen = gDen.MaGa
WHERE cn.MaToaXeKhach = @ToaBN
  AND (v.MaVe IS NULL OR NOT (gDen.LyTrinhKm <= @KmHN OR gDi.LyTrinhKm >= @KmDAN));

-- Q1_3: Khách đã mua giường tầng 1 toa AN đoạn Hà Nội – Vinh; chỗ đó có bán tiếp được cho khách khác đi đoạn Vinh – Sài Gòn không?
-- Đáp án: Hoàn toàn được vì KmGaDen(HN-VIN) = 319.20 <= KmGaDi(VIN-SGO) = 319.20 (không chồng chặng).
SELECT 
    v.MaVeCode, v.TenHanhKhach, cn.SoGhe, cn.TangGiuong,
    gDi.TenGa AS GaDi, gDen.TenGa AS GaDen, 
    gDi.LyTrinhKm AS KmGaDi, gDen.LyTrinhKm AS KmGaDen, 
    v.TrangThai
FROM vantai.Ve v
JOIN vantai.ChoNgoi cn ON v.MaChoNgoi = cn.MaChoNgoi
JOIN hatang.Ga gDi ON v.MaGaDi = gDi.MaGa
JOIN hatang.Ga gDen ON v.MaGaDen = gDen.MaGa
WHERE cn.SoGhe = 1 AND v.MaChuyenTau = 1;

-- Q1_4: Một khách hàng cụ thể (theo CCCD) đã mua bao nhiêu vé, tổng chi bao nhiêu tiền, đi những chặng nào?
SELECT 
    kh.SoCCCD, kh.HoTen,
    COUNT(v.MaVe) AS TongSoVeDaMua,
    SUM(v.GiaVeThucThu) AS TongTienChiVND,
    STRING_AGG(CONCAT(gDi.TenGa, ' -> ', gDen.TenGa, ' (', v.MaVeCode, ')'), '; ') AS DanhSachCacChangDi
FROM vantai.KhachHang kh
JOIN vantai.DonDatVe don ON kh.MaKhachHang = don.MaKhachHang
JOIN vantai.Ve v ON don.MaDonVe = v.MaDonVe
JOIN hatang.Ga gDi ON v.MaGaDi = gDi.MaGa
JOIN hatang.Ga gDen ON v.MaGaDen = gDen.MaGa
WHERE kh.SoCCCD = '001200009999'
GROUP BY kh.SoCCCD, kh.HoTen;

-- Q1_5: Đơn đặt vé nào có số lượng vé nhiều nhất trong tháng?
SELECT TOP 1 
    don.MaPNR, kh.HoTen AS NguoiDat, don.SoLuongVe, don.TongTien, don.HinhThucTT, don.ThoiDiemTao
FROM vantai.DonDatVe don
JOIN vantai.KhachHang kh ON don.MaKhachHang = kh.MaKhachHang
WHERE MONTH(don.ThoiDiemTao) = MONTH(SYSUTCDATETIME())
ORDER BY don.SoLuongVe DESC;

-- Q1_6: Vé nào đã bị hoàn/hủy trong tuần qua, số tiền hoàn tương ứng là bao nhiêu?
-- Đáp án: Truy vấn trực tiếp từ bảng riêng vantai.HoanHuyVe (Sparse data)
SELECT 
    v.MaVeCode, v.TenHanhKhach, hh.TienVeGoc, hh.TyLeLePhi * 100 AS PhanTramPhi, 
    hh.LePhiHoan, hh.SoTienThucHoan, hh.HinhThucHoan, hh.LyDoHoan, hh.ThoiDiemHoan
FROM vantai.HoanHuyVe hh
JOIN vantai.Ve v ON hh.MaVe = v.MaVe
WHERE hh.ThoiDiemHoan >= DATEADD(DAY, -7, SYSUTCDATETIME());

-- Q1_7: Có khách nào bị lập biên lai phạt vì đi quá chặng hoặc không vé trên chuyến SE1 không?
SELECT 
    bl.MaBienLai, mm.SoHieuMacTau, bl.TenNguoiViPham, bl.LyDoViPham,
    bl.CuLyVuotKm, bl.TienVeBoSung, bl.TienPhat, bl.TongTienThu, bl.ThoiDiemLap
FROM vantai.BienLaiPhatVe bl
JOIN vanhanh.ChuyenTau ct ON bl.MaChuyenTau = ct.MaChuyenTau
JOIN vanhanh.MacTauMau mm ON ct.MaMacTau = mm.MaMacTau
WHERE mm.SoHieuMacTau = 'SE1';


-- -------------------------------------------------------------------------
-- NHÓM 2: LỊCH TRÌNH & ĐIỀU ĐỘ CHẠY TÀU
-- -------------------------------------------------------------------------

-- Q2_1: Chuyến SE2 dừng những ga nào, theo thứ tự, giờ đến/đi kế hoạch từng ga?
SELECT 
    mm.SoHieuMacTau, ld.ThuTuDung, g.TenGa, g.LyTrinhKm,
    FORMAT(ld.GioDenKeHoach, 'dd/MM/yyyy HH:mm') AS GioDenKH,
    FORMAT(ld.GioDiKeHoach, 'dd/MM/yyyy HH:mm') AS GioDiKH
FROM vanhanh.LichDungGa ld
JOIN vanhanh.ChuyenTau ct ON ld.MaChuyenTau = ct.MaChuyenTau
JOIN vanhanh.MacTauMau mm ON ct.MaMacTau = mm.MaMacTau
JOIN hatang.Ga g ON ld.MaGa = g.MaGa
WHERE mm.SoHieuMacTau = 'SE2'
ORDER BY ld.ThuTuDung ASC;

-- Q2_2: Hiện tại có những chuyến nào đang chạy trên tuyến, đang ở khu gian nào?
-- Đáp án: Suy diễn chuẩn nghiệp vụ đóng đường VNR qua 2 điểm dừng thực tế của LichDungGa (Không cần GPS!)
SELECT 
    ct.MaChuyenTau, mm.SoHieuMacTau, mm.HuongChay,
    g1.TenGa AS GaVuaRoi, FORMAT(ld1.GioDiThucTe, 'HH:mm') AS GioKhoiHanh,
    g2.TenGa AS GaSapDen, FORMAT(ld2.GioDenKeHoach, 'HH:mm') AS GioDuKienDen,
    CONCAT(g1.TenGa, ' ---> ', g2.TenGa) AS KhuGianDangChiemDung,
    kg.CuLyKm AS CuLyKhuGianKm, kg.DoDocPermil
FROM vanhanh.ChuyenTau ct
JOIN vanhanh.MacTauMau mm ON ct.MaMacTau = mm.MaMacTau
JOIN vanhanh.LichDungGa ld1 ON ct.MaChuyenTau = ld1.MaChuyenTau
JOIN vanhanh.LichDungGa ld2 ON ct.MaChuyenTau = ld2.MaChuyenTau AND ld2.ThuTuDung = ld1.ThuTuDung + 1
JOIN hatang.Ga g1 ON ld1.MaGa = g1.MaGa
JOIN hatang.Ga g2 ON ld2.MaGa = g2.MaGa
JOIN hatang.KhuGian kg ON (kg.MaGaDau = g1.MaGa AND kg.MaGaCuoi = g2.MaGa) 
                       OR (kg.MaGaDau = g2.MaGa AND kg.MaGaCuoi = g1.MaGa)
WHERE ct.TrangThai = 'DANG_CHAY'
  AND ld1.GioDiThucTe IS NOT NULL 
  AND ld2.GioDenThucTe IS NULL;

-- Q2_3: Khu gian nào đang bị phong tỏa, lý do gì, ảnh hưởng đến những chuyến nào hôm nay?
SELECT 
    pt.MaLenhPhongToa,
    CONCAT(gDau.TenGa, ' - ', gCuoi.TenGa) AS KhuGianPhongToa,
    pt.LyDo, FORMAT(pt.ThoiGianBatDau, 'HH:mm') AS TuGio, FORMAT(pt.ThoiGianKetThuc, 'HH:mm') AS DenGio,
    mm.SoHieuMacTau AS ChuyenTauBiAnhHuong,
    ct.TrangThai AS TrangThaiChuyenTau
FROM vanhanh.LenhPhongToa pt
JOIN hatang.KhuGian kg ON pt.MaKhuGian = kg.MaKhuGian
JOIN hatang.Ga gDau ON kg.MaGaDau = gDau.MaGa
JOIN hatang.Ga gCuoi ON kg.MaGaCuoi = gCuoi.MaGa
JOIN vanhanh.LichDungGa ld ON (ld.MaGa = kg.MaGaDau OR ld.MaGa = kg.MaGaCuoi)
JOIN vanhanh.ChuyenTau ct ON ld.MaChuyenTau = ct.MaChuyenTau
JOIN vanhanh.MacTauMau mm ON ct.MaMacTau = mm.MaMacTau
WHERE CAST(pt.ThoiGianBatDau AS DATE) = CAST(SYSUTCDATETIME() AS DATE);

-- Q2_4: Chuyến tàu nào có số phút trễ lũy kế cao nhất tháng này, nguyên nhân chủ yếu là gì?
SELECT TOP 1
    mm.SoHieuMacTau, ct.NgayXuatPhat, ct.SoPhutTreLuyKe,
    cg.LoaiSuCo AS NguyenNhanChinh, cg.GhiChuXuLy
FROM vanhanh.ChuyenTau ct
JOIN vanhanh.MacTauMau mm ON ct.MaMacTau = mm.MaMacTau
LEFT JOIN vanhanh.NhatKyChamGio cg ON ct.MaChuyenTau = cg.MaChuyenTau
WHERE MONTH(ct.NgayXuatPhat) = MONTH(SYSUTCDATETIME())
ORDER BY ct.SoPhutTreLuyKe DESC;

-- Q2_5: Ga nào có cầu quay đầu máy? Khu gian nào cần đầu máy đẩy phụ?
-- 1. Ga có cầu quay
SELECT MaGaCode, TenGa, LyTrinhKm, TinhThanh FROM hatang.Ga WHERE CoCauQuay = 1;
-- 2. Khu gian đèo dốc cần đầu máy đẩy
SELECT 
    CONCAT(g1.TenGa, ' - ', g2.TenGa) AS PhanDoanDeoDoc,
    kg.CuLyKm, kg.DoDocPermil, kg.TocDoToiDaKhach, N'Yêu cầu đầu máy đẩy Banker' AS ChiThiKyThuat
FROM hatang.KhuGian kg
JOIN hatang.Ga g1 ON kg.MaGaDau = g1.MaGa
JOIN hatang.Ga g2 ON kg.MaGaCuoi = g2.MaGa
WHERE kg.CanDauMayDay = 1;

-- Q2_6: Đoàn tàu SE1 hôm nay gồm đầu máy nào, có đầu máy đẩy phụ không, tổng bao nhiêu toa và bao nhiêu mét?
SELECT 
    mm.SoHieuMacTau,
    dmChinh.SoHieuDauMay AS DauMayChinh,
    ISNULL(dmDay.SoHieuDauMay, N'Không có') AS DauMayDayPhu,
    dt.TongSoToa,
    dt.TongChieuDaiM AS TongChieuDaiMet,
    dt.TongTrongLuongTan,
    dt.DaDuyetAnToan
FROM vanhanh.DoanTau dt
JOIN vanhanh.ChuyenTau ct ON dt.MaChuyenTau = ct.MaChuyenTau
JOIN vanhanh.MacTauMau mm ON ct.MaMacTau = mm.MaMacTau
JOIN phuongtien.DauMay dmChinh ON dt.MaDauMayChinh = dmChinh.MaDauMay
LEFT JOIN phuongtien.DauMay dmDay ON dt.MaDauMayDay = dmDay.MaDauMay
WHERE mm.SoHieuMacTau = 'SE1';


-- -------------------------------------------------------------------------
-- NHÓM 3: VẬN TẢI HÀNG HÓA
-- -------------------------------------------------------------------------

-- Q3_1: Vận đơn xe máy nào đã xếp lên toa mà chưa đánh dấu "đã rút xăng"?
SELECT 
    vd.MaVanDonCode, vd.BienKiemSoat, vd.TenNguoiGui, vd.SDTNguoiGui,
    gGui.TenGa AS GaGui, gNhan.TenGa AS GaNhan, vd.TrangThai, vd.DaRutXang,
    N'CẢNH BÁO: CHƯA RÚT XĂNG KHI XẾP TOA' AS CanhBaoChayNo
FROM vantai.VanDonHang vd
JOIN hatang.Ga gGui ON vd.MaGaGui = gGui.MaGa
JOIN hatang.Ga gNhan ON vd.MaGaNhan = gNhan.MaGa
WHERE vd.LoaiVanChuyen = 'XE_MAY' 
  AND vd.TrangThai = 'DA_XEP' 
  AND ISNULL(vd.DaRutXang, 0) = 0;

-- Q3_2: Container mã số Y (CONT-VNR-8899) hiện đang ở trạng thái nào?
SELECT 
    vd.SoHieuContainer, vd.MaVanDonCode, vd.SoChiHaiQuan,
    gGui.TenGa AS GaGui, gNhan.TenGa AS GaNhan,
    vd.TrongLuongTan, vd.CuocPhi, vd.TrangThai, vd.ThoiDiemTao
FROM vantai.VanDonHang vd
JOIN hatang.Ga gGui ON vd.MaGaGui = gGui.MaGa
JOIN hatang.Ga gNhan ON vd.MaGaNhan = gNhan.MaGa
WHERE vd.SoHieuContainer = 'CONT-VNR-8899';

-- Q3_3: Tổng cước phí vận đơn nhóm cước 3 (hàng cồng kềnh/giá trị cao) thu được trong quý?
SELECT 
    DATEPART(QUARTER, vd.ThoiDiemTao) AS Quy,
    COUNT(vd.MaVanDon) AS TongSoVanDon,
    SUM(vd.TrongLuongTan) AS TongKhoiLuongTan,
    SUM(vd.CuocPhi) AS TongCuocPhiThuDuocVND
FROM vantai.VanDonHang vd
JOIN vantai.LoaiHangHoa lh ON vd.MaLoaiHang = lh.MaLoaiHang
WHERE lh.NhomCuoc = 3
GROUP BY DATEPART(QUARTER, vd.ThoiDiemTao);

-- Q3_4: Giữa ga Đà Nẵng và Sài Gòn, tháng này vận chuyển tổng cộng bao nhiêu tấn hàng?
SELECT 
    gGui.TenGa AS GaGui, gNhan.TenGa AS GaNhan,
    COUNT(vd.MaVanDon) AS SoChuyenHang,
    SUM(vd.TrongLuongTan) AS TongTrongLuongTan,
    SUM(vd.CuocPhi) AS TongCuocPhiVND
FROM vantai.VanDonHang vd
JOIN hatang.Ga gGui ON vd.MaGaGui = gGui.MaGa
JOIN hatang.Ga gNhan ON vd.MaGaNhan = gNhan.MaGa
WHERE gGui.MaGaCode = 'DAN' AND gNhan.MaGaCode = 'SGO'
  AND MONTH(vd.ThoiDiemTao) = MONTH(SYSUTCDATETIME())
GROUP BY gGui.TenGa, gNhan.TenGa;


-- -------------------------------------------------------------------------
-- NHÓM 4: NHÂN SỰ, AN TOÀN & BẢO TRÌ
-- -------------------------------------------------------------------------

-- Q4_1: Kíp lái của chuyến SE1 hôm nay gồm những ai, nhận bàn giao tại ga nào?
SELECT 
    mm.SoHieuMacTau,
    nvLai.HoTen AS LaiTauChinh, nvLai.HangBangLai,
    nvPhu.HoTen AS PhuLaiTau,
    nvTruong.HoTen AS TruongTau,
    gNhan.TenGa AS GaNhanBanGiao,
    gGiao.TenGa AS GaTraBanGiao,
    pk.TrangThai
FROM nhansu.PhanCongKipLai pk
JOIN vanhanh.ChuyenTau ct ON pk.MaChuyenTau = ct.MaChuyenTau
JOIN vanhanh.MacTauMau mm ON ct.MaMacTau = mm.MaMacTau
JOIN nhansu.NhanVien nvLai ON pk.MaLaiTau = nvLai.MaNhanVien
JOIN nhansu.NhanVien nvPhu ON pk.MaPhuLai = nvPhu.MaNhanVien
JOIN nhansu.NhanVien nvTruong ON pk.MaTruongTau = nvTruong.MaNhanVien
JOIN hatang.Ga gNhan ON pk.MaGaNhanBan = gNhan.MaGa
JOIN hatang.Ga gGiao ON pk.MaGaBanGiao = gGiao.MaGa
WHERE mm.SoHieuMacTau = 'SE1';

-- Q4_2: Có nhân viên nào nồng độ cồn > 0 hoặc nghỉ chưa đủ 8 tiếng mà vẫn được ghi nhận không đủ điều kiện lên ban?
SELECT 
    nv.MaNVCode, nv.HoTen, nv.ChucDanh,
    ksk.NongDoConMgL, ksk.SoGioNghiTruocCa,
    ksk.DuDieuKien AS KetLuanHeThong,
    ksk.ThoiDiemKiemTra,
    CASE WHEN ksk.DuDieuKien = 0 THEN N'ĐÌNH CHỈ LÊN BAN (VI PHẠM NỒNG ĐỘ CỒN HOẶC THỜI GIAN NGHỈ)' ELSE N'ĐẠT' END AS CanhBaoKyLuat
FROM nhansu.KiemTraSucKhoe ksk
JOIN nhansu.NhanVien nv ON ksk.MaNhanVien = nv.MaNhanVien
WHERE ksk.DuDieuKien = 0 OR ksk.NongDoConMgL > 0 OR ksk.SoGioNghiTruocCa < 8.0;

-- Q4_3: Đoàn tàu của chuyến SE1 hôm nay đã khám xe kỹ thuật đạt chuẩn chưa?
SELECT 
    mm.SoHieuMacTau,
    kx.ApLucHamBar, kx.DoSutApBarPhut, kx.DaCapNuoc, kx.DaXaVeSinh,
    kx.DuDieuKienXuatBen,
    kx.GhiChuKyThuat, kx.ThoiDiemKham
FROM baotri.KhamXeKyThuat kx
JOIN vanhanh.DoanTau dt ON kx.MaDoanTau = dt.MaDoanTau
JOIN vanhanh.ChuyenTau ct ON dt.MaChuyenTau = ct.MaChuyenTau
JOIN vanhanh.MacTauMau mm ON ct.MaMacTau = mm.MaMacTau
WHERE mm.SoHieuMacTau = 'SE1';

-- Q4_4: Đầu máy nào sắp đến hạn bảo dưỡng cấp R2 theo km tích lũy? (Định mức R2: 150.000 km)
SELECT 
    dm.SoHieuDauMay, ddm.MaDongCode, dm.SoKmTichLuy,
    150000.0 - dm.SoKmTichLuy AS SoKmConLaiTruocR2,
    CASE 
        WHEN dm.SoKmTichLuy >= 145000.0 THEN N'CẢNH BÁO: SẮP ĐẾN HẠN BẢO DƯỠNG CẤP R2'
        ELSE N'Bình thường'
    END AS TinhTrangBaoDuong
FROM phuongtien.DauMay dm
JOIN phuongtien.DongDauMay ddm ON dm.MaDongDauMay = ddm.MaDongDauMay
WHERE dm.SoKmTichLuy >= 140000.0;

-- Q4_5: Nhân viên nào sắp hết hạn khám sức khỏe trong 30 ngày mà vẫn đang được phân công?
SELECT 
    nv.MaNVCode, nv.HoTen, nv.ChucDanh, nv.HanKhamSucKhoe,
    DATEDIFF(DAY, CAST(SYSUTCDATETIME() AS DATE), nv.HanKhamSucKhoe) AS SoNgayConLai,
    N'YÊU CẦU ĐI KHÁM SỨC KHỎE ĐỊNH KỲ LÁI TÀU' AS ChiThiYTe
FROM nhansu.NhanVien nv
WHERE nv.HanKhamSucKhoe <= DATEADD(DAY, 30, CAST(SYSUTCDATETIME() AS DATE));

-- Q4_6: Đầu máy nào có suất tiêu hao nhiên liệu vượt định mức ở lần cấp dầu gần nhất?
SELECT 
    dm.SoHieuDauMay, nk.NoiCapDau, nk.SoLitTraNap, nk.CuLyChayKm,
    nk.SuatTieuHaoSFC, nk.CanhBaoVuotMuc, nk.ThoiDiemBomDau
FROM baotri.NhatKyCapNhienLieu nk
JOIN phuongtien.DauMay dm ON nk.MaDauMay = dm.MaDauMay
WHERE nk.CanhBaoVuotMuc = 1;


-- -------------------------------------------------------------------------
-- NHÓM 5: BÁO CÁO / KPI TỔNG HỢP
-- -------------------------------------------------------------------------

-- Q5_1: Doanh thu bán vé hôm nay, chia theo từng hình thức thanh toán?
SELECT 
    don.HinhThucTT,
    COUNT(don.MaDonVe) AS SoGiaoDich,
    SUM(don.SoLuongVe) AS TongVeBan,
    SUM(don.TongTien) AS DoanhThuVND
FROM vantai.DonDatVe don
WHERE CAST(don.ThoiDiemTao AS DATE) = CAST(SYSUTCDATETIME() AS DATE)
  AND don.TrangThaiTT = 'DA_THANH_TOAN'
GROUP BY don.HinhThucTT;

-- Q5_2: Tỷ lệ chạy tàu đúng giờ (trễ <= 15 phút) toàn tuyến trong tháng?
SELECT 
    COUNT(*) AS TongSoChuyenTau,
    SUM(CASE WHEN SoPhutTreLuyKe <= 15 THEN 1 ELSE 0 END) AS SoChuyenDungGio,
    ROUND(CAST(SUM(CASE WHEN SoPhutTreLuyKe <= 15 THEN 1 ELSE 0 END) AS DECIMAL(5,2)) / COUNT(*) * 100.0, 1) AS TyLeDungGioOTP_PhanTram
FROM vanhanh.ChuyenTau
WHERE MONTH(NgayXuatPhat) = MONTH(SYSUTCDATETIME());

-- Q5_3: Ga nào bán được nhiều vé nhất tháng này?
SELECT TOP 3
    g.TenGa,
    COUNT(v.MaVe) AS TongSoVeBan,
    SUM(v.GiaVeThucThu) AS TongDoanhThuVeVND
FROM vantai.Ve v
JOIN hatang.Ga g ON v.MaGaDi = g.MaGa
WHERE MONTH(v.ThoiDiemXuatVe) = MONTH(SYSUTCDATETIME())
GROUP BY g.TenGa
ORDER BY TongSoVeBan DESC;

-- Q5_4: So sánh doanh thu hành khách và hàng hóa theo tháng
WITH DoanhThuKhach AS (
    SELECT MONTH(ThoiDiemTao) AS Thang, SUM(TongTien) AS DoanhThuHanhKhachVND
    FROM vantai.DonDatVe WHERE TrangThaiTT = 'DA_THANH_TOAN' GROUP BY MONTH(ThoiDiemTao)
),
DoanhThuHang AS (
    SELECT MONTH(ThoiDiemTao) AS Thang, SUM(CuocPhi) AS DoanhThuHangHoaVND
    FROM vantai.VanDonHang GROUP BY MONTH(ThoiDiemTao)
)
SELECT 
    ISNULL(k.Thang, h.Thang) AS Thang,
    ISNULL(k.DoanhThuHanhKhachVND, 0) AS DoanhThuKhach,
    ISNULL(h.DoanhThuHangHoaVND, 0) AS DoanhThuHang,
    ISNULL(k.DoanhThuHanhKhachVND, 0) + ISNULL(h.DoanhThuHangHoaVND, 0) AS TongDoanhThuToanTuyen
FROM DoanhThuKhach k
FULL OUTER JOIN DoanhThuHang h ON k.Thang = h.Thang;

-- Q5_5: Loại toa nào đóng góp doanh thu nhiều nhất?
SELECT 
    bc.LoaiToa,
    CASE bc.LoaiToa 
        WHEN 'NC' THEN N'Ngồi cứng' 
        WHEN 'NML' THEN N'Ngồi mềm điều hòa' 
        WHEN 'BN' THEN N'Nằm khoang 6' 
        WHEN 'AN' THEN N'Nằm khoang 4' 
    END AS TenLoaiToa,
    COUNT(v.MaVe) AS SoVeDaBan,
    SUM(v.GiaVeThucThu) AS DoanhThuThuDuocVND
FROM vantai.ToaXeKhach tk
JOIN vantai.ChoNgoi cn ON tk.MaToaXeKhach = cn.MaToaXeKhach
JOIN vantai.Ve v ON cn.MaChoNgoi = v.MaChoNgoi
JOIN vantai.BieuCuoc bc ON tk.LoaiToa = bc.LoaiToa
WHERE v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
GROUP BY bc.LoaiToa;


IF COL_LENGTH(N'vantai.Ve', N'ThoiDiemSoatVe') IS NULL
    ALTER TABLE vantai.Ve ADD ThoiDiemSoatVe DATETIME2 NULL;
GO
IF COL_LENGTH(N'vantai.Ve', N'MaGaSoat') IS NULL
    ALTER TABLE vantai.Ve ADD MaGaSoat INT NULL
        CONSTRAINT FK_Ve_GaSoat FOREIGN KEY REFERENCES hatang.Ga(MaGa);
GO

GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
   OR (SELECT create_date FROM sys.databases WHERE name = N'QuanLyDuongSatV2') < (SELECT MAX(Luc) FROM #TronGoiBatDau)
BEGIN
    RAISERROR(N'CSDL QuanLyDuongSatV2 chưa được xoá và tạo lại ở bước 1 (ứng dụng hoặc cửa sổ SSMS khác còn mở CSDL?) — dừng.', 16, 1);
    SET NOEXEC ON;
END
GO
INSERT #TronGoi (Buoc) VALUES (1);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [2/17] migrations\V20261009_0820__dat__chitietdoantau-ga-noi-cat.sql ---';
GO
/* =========================================================================
   MIGRATION: Thêm phạm vi chạy cho từng toa trong đoàn tàu (cắt / nối toa dọc đường)
   Tác giả  : Đạt
   Ngày     : 09/10/2026 (Sprint 1)
   Bảng     : vanhanh.ChiTietDoanTau (chủ bảng: Đạt)

   Lý do:
   - Đoàn tàu vẫn là MỘT danh sách toa theo thứ tự móc nối (ThuTuNoi).
   - Mỗi toa thêm phạm vi chạy MaGaNoi → MaGaCat:
       MaGaNoi NULL = toa có mặt từ ga đầu,
       MaGaCat NULL = toa chạy tới ga cuối,
       cả hai NULL  = chạy suốt (dữ liệu cũ giữ nguyên ý nghĩa).
   - Toa có mặt ở khu đoạn k (ga dừng k → k+1) khi vị trí ga nối <= k < vị trí ga cắt.
   - DoanTau.TongSoToa / TongChieuDaiM / TongTrongLuongTan = giá trị LỚN NHẤT trên các chặng.
   Chi tiết nghiệp vụ: CLAUDE.md §5.6, KE_HOACH_BACK_END.md §4.3 và §7.1.

   Ràng buộc chỉ kiểm được ở tầng Service (cần đọc lịch dừng ga, CHECK không làm được):
   - ga nối / ga cắt phải nằm trên hành trình của chuyến, ga nối đứng trước ga cắt;
   - ga cắt / nối phải đỗ >= 15 phút;
   - nhóm toa cắt / nối tại một ga phải liền khối ở cuối đoàn hoặc ngay sau đầu máy.

   Chạy lại được nhiều lần. Không có USE: chạy bằng  sqlcmd -S . -E -I -d <TênCSDL> -i <file>
   ========================================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'Đang đứng ở CSDL hệ thống. Chạy lại với -d QuanLyDuongSatV2 (hoặc CSDL kiểm thử).', 16, 1);
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'vanhanh.ChiTietDoanTau', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng vanhanh.ChiTietDoanTau. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Hai cột phạm vi chạy
IF COL_LENGTH(N'vanhanh.ChiTietDoanTau', N'MaGaNoi') IS NULL
    ALTER TABLE vanhanh.ChiTietDoanTau ADD MaGaNoi INT NULL;
GO
IF COL_LENGTH(N'vanhanh.ChiTietDoanTau', N'MaGaCat') IS NULL
    ALTER TABLE vanhanh.ChiTietDoanTau ADD MaGaCat INT NULL;
GO

-- 2. Khoá ngoại tới hatang.Ga
IF OBJECT_ID(N'vanhanh.FK_CTDoanTau_GaNoi', N'F') IS NULL
    ALTER TABLE vanhanh.ChiTietDoanTau
        ADD CONSTRAINT FK_CTDoanTau_GaNoi FOREIGN KEY (MaGaNoi) REFERENCES hatang.Ga(MaGa);
GO
IF OBJECT_ID(N'vanhanh.FK_CTDoanTau_GaCat', N'F') IS NULL
    ALTER TABLE vanhanh.ChiTietDoanTau
        ADD CONSTRAINT FK_CTDoanTau_GaCat FOREIGN KEY (MaGaCat) REFERENCES hatang.Ga(MaGa);
GO

-- 3. Một toa không nối và cắt ở cùng một ga
IF OBJECT_ID(N'vanhanh.CK_CTDoanTau_GaNoiKhacGaCat', N'C') IS NULL
    ALTER TABLE vanhanh.ChiTietDoanTau
        ADD CONSTRAINT CK_CTDoanTau_GaNoiKhacGaCat
        CHECK (MaGaNoi IS NULL OR MaGaCat IS NULL OR MaGaNoi <> MaGaCat);
GO

-- 4. Chỉ mục khoá ngoại (cùng quy ước IX_FK_* của script gốc)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FK_CTDoanTau_MaGaNoi'
               AND object_id = OBJECT_ID(N'vanhanh.ChiTietDoanTau'))
    CREATE NONCLUSTERED INDEX IX_FK_CTDoanTau_MaGaNoi ON vanhanh.ChiTietDoanTau(MaGaNoi);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FK_CTDoanTau_MaGaCat'
               AND object_id = OBJECT_ID(N'vanhanh.ChiTietDoanTau'))
    CREATE NONCLUSTERED INDEX IX_FK_CTDoanTau_MaGaCat ON vanhanh.ChiTietDoanTau(MaGaCat);
GO

-- [tron goi] bo SET NOEXEC OFF: buoc nay loi thi dung ca file
GO
PRINT N'Migration V20261009_0820 (ChiTietDoanTau.MaGaNoi / MaGaCat): xong.';
GO

GO
INSERT #TronGoi (Buoc) VALUES (2);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [3/17] seed\S20261009_0830__dat__phuong-tien-nhan-su-sprint1.sql ---';
GO
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
--     Thay trưởng tàu TRƯỚC khi đổi trạng thái kíp SE1 / SE4: từ migration V20261011_1500 trigger trùng giờ xét cả
--     trưởng tàu, đổi trạng thái kíp SE1 lúc TT_001 còn ở SE19 sẽ bị trigger chặn.
UPDATE k SET MaTruongTau = (SELECT MaNhanVien FROM nhansu.NhanVien WHERE MaNVCode = 'TT_004')
FROM nhansu.PhanCongKipLai k WHERE k.MaChuyenTau = @CtSE19;

UPDATE k SET MaTruongTau = (SELECT MaNhanVien FROM nhansu.NhanVien WHERE MaNVCode = 'TT_005')
FROM nhansu.PhanCongKipLai k WHERE k.MaChuyenTau = @CtNA1;

UPDATE k SET TrangThai = 'HOAN_THANH'
FROM nhansu.PhanCongKipLai k
WHERE k.MaChuyenTau IN (@CtSE1, @CtSE4);

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

-- [tron goi] bo SET NOEXEC OFF: buoc nay loi thi dung ca file
GO

GO
INSERT #TronGoi (Buoc) VALUES (3);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [4/17] migrations\V20261011_1500__dat__nhansu-phienban-trigger-truongtau.sql ---';
GO
/* =========================================================================
   MIGRATION: Nhân sự — cột phiên bản chống ghi đè + trigger trùng giờ xét cả trưởng tàu
   Tác giả  : Đạt
   Ngày     : 11/10/2026 (Sprint 1, back-end Nhân sự)
   Bảng     : nhansu.NhanVien, nhansu.PhanCongKipLai (chủ bảng: Đạt)

   Lý do (Đạt chốt 11/10):
   1. PhienBan ROWVERSION cho NhanVien và PhanCongKipLai: Service ghi bằng
      UPDATE ... WHERE PhienBan = @pb; 0 dòng = người khác vừa sửa (KE_HOACH_BACK_END.md §2.5 mục 8).
   2. trg_PhanCongKipLai_CheckTrungGio bản gốc chỉ xét lái tàu / phụ lái. Bản này xét MỌI vị trí:
      - một người không thuộc kíp của hai chuyến KHÁC NHAU có khung giờ chồng nhau (cả trưởng tàu);
      - trong CÙNG một chuyến, một người chỉ ở một kíp — riêng trưởng tàu được ở nhiều kíp
        (trưởng tàu đi suốt chuyến, lái tàu / phụ lái đổi ở ga depot). Kíp của trưởng tàu
        không được chồng chặng: Service kiểm (cần lịch dừng ga, trigger không làm).
      Bỏ qua chuyến HOAN_THANH / DA_HUY như bản gốc. Vẫn RAISERROR + ROLLBACK như bản gốc
      để mã lỗi (50000) và cách xử lý ở tầng ứng dụng không đổi.

   Thứ tự: chạy sau script gốc. Seed S20261009_0830 đã đổi thứ tự (thay trưởng tàu SE19 / NA1
   trước khi đổi trạng thái kíp SE1 / SE4) nên chạy được cả trước lẫn sau migration này.

   Chạy lại được nhiều lần. Không có USE: chạy bằng  sqlcmd -S . -E -I -d <TênCSDL> -i <file>
   ========================================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'Đang đứng ở CSDL hệ thống. Chạy lại với -d QuanLyDuongSatV2 (hoặc CSDL kiểm thử).', 16, 1);
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'nhansu.PhanCongKipLai', N'U') IS NULL OR OBJECT_ID(N'nhansu.NhanVien', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng nhansu.NhanVien / nhansu.PhanCongKipLai. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Cột phiên bản (SQL Server tự tăng mỗi lần dòng bị ghi)
IF COL_LENGTH(N'nhansu.NhanVien', N'PhienBan') IS NULL
    ALTER TABLE nhansu.NhanVien ADD PhienBan ROWVERSION;
GO
IF COL_LENGTH(N'nhansu.PhanCongKipLai', N'PhienBan') IS NULL
    ALTER TABLE nhansu.PhanCongKipLai ADD PhienBan ROWVERSION;
GO

-- 2. Trigger trùng giờ xét cả 3 vị trí
CREATE OR ALTER TRIGGER nhansu.trg_PhanCongKipLai_CheckTrungGio
ON nhansu.PhanCongKipLai
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        CROSS APPLY (VALUES (i.MaLaiTau, 'LAI_TAU'), (i.MaPhuLai, 'PHU_LAI'), (i.MaTruongTau, 'TRUONG_TAU'))
                    n(MaNhanVien, VaiTro)
        JOIN nhansu.PhanCongKipLai k ON k.MaPhanCong <> i.MaPhanCong
                                    AND n.MaNhanVien IN (k.MaLaiTau, k.MaPhuLai, k.MaTruongTau)
        JOIN vanhanh.ChuyenTau ct1 ON ct1.MaChuyenTau = i.MaChuyenTau
        JOIN vanhanh.ChuyenTau ct2 ON ct2.MaChuyenTau = k.MaChuyenTau
        WHERE ct1.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
          AND ct2.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
          AND (
                -- Hai chuyến khác nhau có khung giờ chồng nhau
                (i.MaChuyenTau <> k.MaChuyenTau
                 AND (CASE WHEN ct1.GioXuatPhatKH > ct2.GioXuatPhatKH THEN ct1.GioXuatPhatKH ELSE ct2.GioXuatPhatKH END)
                   < (CASE WHEN ct1.GioVeDichKH < ct2.GioVeDichKH THEN ct1.GioVeDichKH ELSE ct2.GioVeDichKH END))
                -- Cùng chuyến: chỉ trưởng tàu (ở cả hai kíp) mới được đi nhiều kíp
             OR (i.MaChuyenTau = k.MaChuyenTau
                 AND NOT (n.VaiTro = 'TRUONG_TAU' AND k.MaTruongTau = n.MaNhanVien))
              )
    )
    BEGIN
        RAISERROR(N'Xung đột nhân sự: một thành viên kíp đã được phân công cho chuyến tàu khác có khung giờ chồng lấn, hoặc đã ở kíp khác của cùng chuyến (chỉ trưởng tàu được đi nhiều kíp liên tiếp của một chuyến).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 3. Chỉ mục cho các cột người còn thiếu (script gốc mới có IX_FK_KipLai_MaLaiTau)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FK_KipLai_MaPhuLai'
               AND object_id = OBJECT_ID(N'nhansu.PhanCongKipLai'))
    CREATE NONCLUSTERED INDEX IX_FK_KipLai_MaPhuLai ON nhansu.PhanCongKipLai(MaPhuLai);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FK_KipLai_MaTruongTau'
               AND object_id = OBJECT_ID(N'nhansu.PhanCongKipLai'))
    CREATE NONCLUSTERED INDEX IX_FK_KipLai_MaTruongTau ON nhansu.PhanCongKipLai(MaTruongTau);
GO

-- [tron goi] bo SET NOEXEC OFF: buoc nay loi thi dung ca file
GO
PRINT N'Migration V20261011_1500 (Nhân sự: PhienBan + trigger trùng giờ cả trưởng tàu): xong.';
GO

GO
INSERT #TronGoi (Buoc) VALUES (4);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [5/17] migrations\V20261012_1100__dat__nhansu-lichsu-ma-nhanvien.sql ---';
GO
/* =========================================================================
   MIGRATION: Nhân sự — lịch sử mã nhân viên (đổi chức danh → cấp mã mới)
   Tác giả  : Đạt
   Ngày     : 12/10/2026 (Sprint 1, back-end Nhân sự)
   Bảng     : nhansu.LichSuMaNhanVien (mới), trigger trên nhansu.NhanVien (chủ bảng: Đạt)

   Lý do (Đạt chốt 12/10):
   Mã nhân viên theo chức danh (LT_ / PL_ / TT_ ...) nên đổi chức danh thì được cấp mã mới
   (PL_015 → LT_018). Trước đây mã cũ chỉ còn trong JSON của kiemtoan.NhatKyHeThong:
     - tra cứu theo mã cũ (sổ sách, biên bản, nhật ký phân kíp ghi mã dạng chữ) không ra người;
     - mã mới = số lớn nhất + 1 nên mã cũ lớn nhất của một tiền tố bị CẤP LẠI cho người khác.
   Bảng này giữ mọi lần đổi mã. Mã cũ không bao giờ cấp lại:
     - UNIQUE(MaNVCodeCu): một mã chỉ "nghỉ hưu" một lần;
     - trigger trg_NhanVien_KhongDungLaiMaCu: không gán cho hồ sơ nào một mã đã nằm trong lịch sử;
     - NhanSuService cấp mã kế tiếp tính cả mã trong bảng này.
   Chỉ ghi khi MÃ đổi; đổi đơn vị / hạng bằng mà giữ mã thì chỉ có trong NhatKyHeThong.

   Thứ tự: chạy sau script gốc (không phụ thuộc seed). Chạy lại được nhiều lần.
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

IF OBJECT_ID(N'nhansu.NhanVien', N'U') IS NULL OR OBJECT_ID(N'phanquyen.TaiKhoan', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng nhansu.NhanVien / phanquyen.TaiKhoan. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Bảng lịch sử đổi mã
IF OBJECT_ID(N'nhansu.LichSuMaNhanVien', N'U') IS NULL
BEGIN
    CREATE TABLE nhansu.LichSuMaNhanVien (
        MaLichSu        INT IDENTITY(1,1) PRIMARY KEY,
        MaNhanVien      INT             NOT NULL
            CONSTRAINT FK_LichSuMa_NhanVien REFERENCES nhansu.NhanVien(MaNhanVien),
        MaNVCodeCu      VARCHAR(20)     NOT NULL
            CONSTRAINT UQ_LichSuMa_MaCu UNIQUE,
        MaNVCodeMoi     VARCHAR(20)     NOT NULL,
        ChucDanhCu      NVARCHAR(50)    NOT NULL,
        ChucDanhMoi     NVARCHAR(50)    NOT NULL,
        DonViCu         NVARCHAR(100)   NOT NULL,
        DonViMoi        NVARCHAR(100)   NOT NULL,
        ThoiDiem        DATETIME2       NOT NULL
            CONSTRAINT DF_LichSuMa_ThoiDiem DEFAULT SYSDATETIME(),   -- giờ Việt Nam (quyết định Q3 11/10)
        MaTaiKhoan      INT             NULL
            CONSTRAINT FK_LichSuMa_TaiKhoan REFERENCES phanquyen.TaiKhoan(MaTaiKhoan),
        CONSTRAINT CK_LichSuMa_KhacMa CHECK (MaNVCodeCu <> MaNVCodeMoi)
    );
    CREATE NONCLUSTERED INDEX IX_LichSuMa_MaNhanVien ON nhansu.LichSuMaNhanVien(MaNhanVien);
END;
GO

-- 2. Không gán cho hồ sơ một mã đã "nghỉ hưu" (có trong lịch sử)
CREATE OR ALTER TRIGGER nhansu.trg_NhanVien_KhongDungLaiMaCu
ON nhansu.NhanVien
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(MaNVCode) RETURN;   -- đổi trạng thái lúc nhận ban / bàn giao không xét

    IF EXISTS (SELECT 1 FROM inserted i JOIN nhansu.LichSuMaNhanVien ls ON ls.MaNVCodeCu = i.MaNVCode)
    BEGIN
        RAISERROR(N'Mã nhân viên này đã từng được dùng (có trong lịch sử đổi mã) nên không cấp lại. Hãy nạp lại rồi lưu lại để hệ thống cấp mã khác.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- [tron goi] bo SET NOEXEC OFF: buoc nay loi thi dung ca file
GO
PRINT N'Migration V20261012_1100 (Nhân sự: lịch sử mã nhân viên): xong.';
GO

GO
INSERT #TronGoi (Buoc) VALUES (5);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [6/17] migrations\V20261014_0900__dat__phuongtien-ngung-khai-thac.sql ---';
GO
/* =========================================================================
   MIGRATION: Phương tiện — trạng thái "Ngừng khai thác" cho đầu máy và toa xe
   Tác giả  : Đạt
   Ngày     : 14/10/2026 (Sprint 2)
   Bảng     : phuongtien.DauMay, phuongtien.ToaXe (chủ bảng: Đạt)

   Lý do (Đạt chốt 14/10):
   Đầu máy / toa xe đã có lịch sử (đoàn tàu, phiếu cấp dầu, bảo dưỡng, vận đơn) không xóa được
   (khóa ngoại, và xóa thì mất hồ sơ an toàn). Trước đây CHECK chỉ có 3 trạng thái nên máy / toa
   thanh lý, hỏng hẳn vẫn nằm mãi trong danh sách như đang dùng. Thêm trạng thái NGUNG_KHAI_THAC:
     - vẫn giữ trong hồ sơ để tra cứu lịch sử;
     - không được đưa vào đoàn tàu (QuyTacLapTau báo LỖI, bãi toa / ô chọn đầu máy không hiện);
     - chỉ chuyển sang được khi không còn gán cho chuyến chưa hoàn thành (PhuongTienService);
     - đưa về "Sẵn sàng" / "Bảo dưỡng" được khi phục hồi.
   Bản ghi CHƯA có dữ liệu liên quan (nhập nhầm) thì xóa hẳn được — kiểm ở PhuongTienService.

   Ràng buộc CHECK trạng thái ở script gốc không đặt tên (SQL Server tự sinh CK__DauMay__TrangTha__...)
   nên tìm theo cột rồi bỏ, đặt lại có tên CK_DauMay_TrangThai / CK_ToaXe_TrangThai.

   Thứ tự: chạy sau script gốc (không phụ thuộc seed). Chạy lại được nhiều lần.
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

IF OBJECT_ID(N'phuongtien.DauMay', N'U') IS NULL OR OBJECT_ID(N'phuongtien.ToaXe', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng phuongtien.DauMay / phuongtien.ToaXe. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

BEGIN TRANSACTION;

DECLARE @sql NVARCHAR(MAX);

-- 1. Đầu máy: bỏ mọi CHECK đặt trên cột TrangThai (tên tự sinh hoặc tên cũ), đặt lại 4 trạng thái
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE parent_object_id = OBJECT_ID(N'phuongtien.DauMay')
                 AND name = N'CK_DauMay_TrangThai'
                 AND definition LIKE N'%NGUNG_KHAI_THAC%')
BEGIN
    SET @sql = N'';
    SELECT @sql += N'ALTER TABLE phuongtien.DauMay DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';'
    FROM sys.check_constraints cc
    JOIN sys.columns c ON c.object_id = cc.parent_object_id AND c.column_id = cc.parent_column_id
    WHERE cc.parent_object_id = OBJECT_ID(N'phuongtien.DauMay') AND c.name = N'TrangThai';
    EXEC sys.sp_executesql @sql;

    ALTER TABLE phuongtien.DauMay WITH CHECK ADD CONSTRAINT CK_DauMay_TrangThai
        CHECK (TrangThai IN ('SAN_SANG', 'DANG_CHAY', 'BAO_DUONG', 'NGUNG_KHAI_THAC'));
END;

-- 2. Toa xe: như trên
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE parent_object_id = OBJECT_ID(N'phuongtien.ToaXe')
                 AND name = N'CK_ToaXe_TrangThai'
                 AND definition LIKE N'%NGUNG_KHAI_THAC%')
BEGIN
    SET @sql = N'';
    SELECT @sql += N'ALTER TABLE phuongtien.ToaXe DROP CONSTRAINT ' + QUOTENAME(cc.name) + N';'
    FROM sys.check_constraints cc
    JOIN sys.columns c ON c.object_id = cc.parent_object_id AND c.column_id = cc.parent_column_id
    WHERE cc.parent_object_id = OBJECT_ID(N'phuongtien.ToaXe') AND c.name = N'TrangThai';
    EXEC sys.sp_executesql @sql;

    ALTER TABLE phuongtien.ToaXe WITH CHECK ADD CONSTRAINT CK_ToaXe_TrangThai
        CHECK (TrangThai IN ('SAN_SANG', 'DA_NOI', 'BAO_DUONG', 'NGUNG_KHAI_THAC'));
END;

COMMIT TRANSACTION;
GO

-- Kiểm tra sau khi chạy
SELECT OBJECT_SCHEMA_NAME(parent_object_id) + N'.' + OBJECT_NAME(parent_object_id) AS Bang,
       name AS RangBuoc, definition AS DieuKien
FROM sys.check_constraints
WHERE name IN (N'CK_DauMay_TrangThai', N'CK_ToaXe_TrangThai');
PRINT N'V20261014_0900: xong (đầu máy / toa xe có thêm trạng thái NGUNG_KHAI_THAC).';
GO

GO
INSERT #TronGoi (Buoc) VALUES (6);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [7/17] migrations\V20261014_1000__dat__nhansu-ngay-sinh.sql ---';
GO
/* =========================================================================
   MIGRATION: Nhân sự — ngày sinh nhân viên (ràng buộc đủ 18 tuổi)
   Tác giả  : Đạt
   Ngày     : 14/10/2026 (Sprint 2, ràng buộc dữ liệu cơ bản)
   Bảng     : nhansu.NhanVien (chủ bảng: Đạt)

   Lý do (Đạt chốt 14/10): ràng buộc "nhân viên phải trên 18 tuổi" cần ngày sinh, script gốc chưa có cột.
     - Thêm cột NgaySinh DATE.
     - Hồ sơ đang có (dữ liệu mẫu) chưa có ngày sinh → gán ngày sinh MẪU, 25–50 tuổi tại ngày lập hồ sơ
       (tính cố định theo MaNhanVien, chạy lại ra cùng kết quả). Sửa lại ngày thật trong màn hình Hồ sơ.
     - Sau đó NOT NULL + CHECK: đủ 18 tuổi tại ngày lập hồ sơ, sinh từ 1940.
   Giới hạn 18–62 tuổi tính theo NGÀY HÔM NAY kiểm ở BUS (RangBuocNhapLieu.KiemTraNgaySinh).

   Thứ tự: chạy sau script gốc + seed (seed chèn nhân viên chưa có ngày sinh). Chạy lại được nhiều lần.
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

IF OBJECT_ID(N'nhansu.NhanVien', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng nhansu.NhanVien. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Cột ngày sinh (tạm cho NULL để điền dữ liệu cũ)
IF COL_LENGTH(N'nhansu.NhanVien', N'NgaySinh') IS NULL
    ALTER TABLE nhansu.NhanVien ADD NgaySinh DATE NULL;
GO

-- 2. Ngày sinh mẫu cho hồ sơ cũ: 25 tuổi + 0..25 năm (theo MaNhanVien) tại ngày lập hồ sơ
UPDATE nhansu.NhanVien
SET NgaySinh = DATEADD(DAY, -((MaNhanVien * 2971) % 9131), DATEADD(YEAR, -25, CAST(NgayTao AS DATE)))
WHERE NgaySinh IS NULL;
GO

-- 3. Bắt buộc + CHECK đủ 18 tuổi tại ngày lập hồ sơ
IF COLUMNPROPERTY(OBJECT_ID(N'nhansu.NhanVien'), N'NgaySinh', 'AllowsNull') = 1
    ALTER TABLE nhansu.NhanVien ALTER COLUMN NgaySinh DATE NOT NULL;
GO

IF OBJECT_ID(N'nhansu.CK_NhanVien_NgaySinh', N'C') IS NULL
    ALTER TABLE nhansu.NhanVien WITH CHECK ADD CONSTRAINT CK_NhanVien_NgaySinh
        CHECK (NgaySinh >= '1940-01-01' AND NgaySinh <= DATEADD(YEAR, -18, CAST(NgayTao AS DATE)));
GO

PRINT N'V20261014_1000: xong (nhansu.NhanVien có NgaySinh, đủ 18 tuổi tại ngày lập hồ sơ).';
GO

GO
INSERT #TronGoi (Buoc) VALUES (7);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [8/17] migrations\V20261024_1000__dat__baotri-chiphi-dinhmuc-giovn.sql ---';
GO
/* =========================================================================
   MIGRATION: Bảo trì — chi phí, định mức suất tiêu hao, giờ Việt Nam, phiên bản phiếu
   Tác giả  : Đạt
   Ngày     : 24/10/2026 (Sprint 2)
   Bảng     : baotri.KhamXeKyThuat, baotri.NhatKyCapNhienLieu, baotri.NhatKyBaoDuong,
              phuongtien.DongDauMay (chủ bảng: Đạt)

   Quyết định 24/10 (Đạt chốt theo khuyến nghị):
     1. Suất tiêu hao SFC = số lít × 10.000 ÷ (tấn kéo × km) [lít / 10.000 tấn·km], Service tính rồi
        ghi vào SuatTieuHaoSFC + CanhBaoVuotMuc (trước đây nhập tay, seed 2.15 / 3.25 không suy ra được).
        Định mức theo dòng máy: cột mới DongDauMay.DinhMucSFC — D19E 90, D13E 100. ĐỊNH MỨC THAM KHẢO,
        chưa đối chiếu định mức chính thức của VNR.
     3. Chi phí (VNĐ):
          KhamXeKyThuat.ChiPhi, NhatKyBaoDuong.ChiPhi       nhập tay, mặc định 0;
          NhatKyCapNhienLieu.DonGiaVndLit                   đơn giá dầu nhập tay;
          NhatKyCapNhienLieu.ChiPhi                         cột tính = số lít × đơn giá (PERSISTED).
        Phiếu cũ không có số liệu chi phí -> 0 (không bịa số).
     5. Giờ Việt Nam như Nhân sự (11/10): đổi mặc định 3 cột thời điểm từ SYSUTCDATETIME() sang
        SYSDATETIME(). Các dòng có sẵn trước migration đều được ghi bằng mặc định giờ UTC (ứng dụng chưa
        từng ghi bảng baotri) -> cộng 7 giờ, làm ĐÚNG MỘT LẦN cùng lúc đổi mặc định.
     6. Phiếu cấp dầu / bảo dưỡng sửa được -> thêm PhienBan ROWVERSION (không ghi đè nhau).
        Biên bản khám xe không sửa (sai thì lập biên bản mới) nên không cần.
   Ngoài ra: tính lại SFC + cờ vượt mức của MỌI phiếu cấp dầu theo công thức trên.

   Thứ tự: chạy sau script gốc (không phụ thuộc seed). Chạy lại được nhiều lần.
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

IF OBJECT_ID(N'baotri.KhamXeKyThuat', N'U') IS NULL OR OBJECT_ID(N'phuongtien.DongDauMay', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có bảng baotri.* / phuongtien.DongDauMay. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

BEGIN TRANSACTION;

DECLARE @sql NVARCHAR(MAX);

-- 1. Định mức suất tiêu hao theo dòng máy (tham khảo)
IF COL_LENGTH(N'phuongtien.DongDauMay', N'DinhMucSFC') IS NULL
BEGIN
    EXEC (N'ALTER TABLE phuongtien.DongDauMay ADD DinhMucSFC DECIMAL(5,2) NOT NULL
              CONSTRAINT DF_DongDauMay_DinhMucSFC DEFAULT (95.00)
              CONSTRAINT CK_DongDauMay_DinhMucSFC CHECK (DinhMucSFC > 0);');
    EXEC (N'UPDATE phuongtien.DongDauMay SET DinhMucSFC = 90.00 WHERE MaDongCode = ''D19E'';
            UPDATE phuongtien.DongDauMay SET DinhMucSFC = 100.00 WHERE MaDongCode = ''D13E'';');
END;

-- 2. Chi phí khám xe
IF COL_LENGTH(N'baotri.KhamXeKyThuat', N'ChiPhi') IS NULL
    EXEC (N'ALTER TABLE baotri.KhamXeKyThuat ADD ChiPhi DECIMAL(14,0) NOT NULL
              CONSTRAINT DF_KhamXe_ChiPhi DEFAULT (0)
              CONSTRAINT CK_KhamXe_ChiPhi CHECK (ChiPhi >= 0);');

-- 3. Chi phí bảo dưỡng + phiên bản
IF COL_LENGTH(N'baotri.NhatKyBaoDuong', N'ChiPhi') IS NULL
    EXEC (N'ALTER TABLE baotri.NhatKyBaoDuong ADD ChiPhi DECIMAL(14,0) NOT NULL
              CONSTRAINT DF_BaoDuong_ChiPhi DEFAULT (0)
              CONSTRAINT CK_BaoDuong_ChiPhi CHECK (ChiPhi >= 0);');
IF COL_LENGTH(N'baotri.NhatKyBaoDuong', N'PhienBan') IS NULL
    EXEC (N'ALTER TABLE baotri.NhatKyBaoDuong ADD PhienBan ROWVERSION;');

-- 4. Đơn giá dầu + thành tiền (cột tính) + phiên bản
IF COL_LENGTH(N'baotri.NhatKyCapNhienLieu', N'DonGiaVndLit') IS NULL
    EXEC (N'ALTER TABLE baotri.NhatKyCapNhienLieu ADD DonGiaVndLit DECIMAL(10,0) NOT NULL
              CONSTRAINT DF_CapDau_DonGia DEFAULT (0)
              CONSTRAINT CK_CapDau_DonGia CHECK (DonGiaVndLit >= 0);');
IF COL_LENGTH(N'baotri.NhatKyCapNhienLieu', N'ChiPhi') IS NULL
    EXEC (N'ALTER TABLE baotri.NhatKyCapNhienLieu ADD ChiPhi AS
              CAST(ROUND(SoLitTraNap * DonGiaVndLit, 0) AS DECIMAL(16,0)) PERSISTED;');
IF COL_LENGTH(N'baotri.NhatKyCapNhienLieu', N'PhienBan') IS NULL
    EXEC (N'ALTER TABLE baotri.NhatKyCapNhienLieu ADD PhienBan ROWVERSION;');

-- 5. Giờ Việt Nam: bỏ mặc định giờ UTC (tên tự sinh), cộng 7 giờ cho dòng cũ, đặt mặc định có tên.
--    Đã có mặc định có tên = đã chạy -> bỏ qua (không cộng giờ lần nữa).
DECLARE @CotGio TABLE (Bang SYSNAME, Cot SYSNAME, TenMacDinh SYSNAME);
INSERT INTO @CotGio VALUES
    (N'baotri.KhamXeKyThuat',      N'ThoiDiemKham',      N'DF_KhamXe_ThoiDiemKham'),
    (N'baotri.NhatKyCapNhienLieu', N'ThoiDiemBomDau',    N'DF_CapDau_ThoiDiemBomDau'),
    (N'baotri.NhatKyBaoDuong',     N'ThoiDiemHoanThanh', N'DF_BaoDuong_ThoiDiemHoanThanh');

DECLARE @Bang SYSNAME, @Cot SYSNAME, @Ten SYSNAME;
DECLARE dong CURSOR LOCAL FAST_FORWARD FOR SELECT Bang, Cot, TenMacDinh FROM @CotGio;
OPEN dong;
FETCH NEXT FROM dong INTO @Bang, @Cot, @Ten;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints
                   WHERE parent_object_id = OBJECT_ID(@Bang) AND name = @Ten)
    BEGIN
        SET @sql = N'';
        SELECT @sql += N'ALTER TABLE ' + @Bang + N' DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(@Bang) AND c.name = @Cot;

        SET @sql += N'UPDATE ' + @Bang + N' SET ' + QUOTENAME(@Cot) + N' = DATEADD(HOUR, 7, ' + QUOTENAME(@Cot) + N');'
                  + N'ALTER TABLE ' + @Bang + N' ADD CONSTRAINT ' + QUOTENAME(@Ten)
                  + N' DEFAULT (SYSDATETIME()) FOR ' + QUOTENAME(@Cot) + N';';
        EXEC sys.sp_executesql @sql;
    END;
    FETCH NEXT FROM dong INTO @Bang, @Cot, @Ten;
END;
CLOSE dong;
DEALLOCATE dong;

-- 6. Tính lại suất tiêu hao + cờ vượt định mức của mọi phiếu theo công thức (làm tròn 2 chữ số,
--    nửa trên làm tròn lên - BUS dùng MidpointRounding.AwayFromZero cho khớp).
EXEC (N'
UPDATE nl
SET SuatTieuHaoSFC = v.Sfc,
    CanhBaoVuotMuc = CASE WHEN v.Sfc > ddm.DinhMucSFC THEN 1 ELSE 0 END
FROM baotri.NhatKyCapNhienLieu nl
JOIN phuongtien.DauMay dm ON dm.MaDauMay = nl.MaDauMay
JOIN phuongtien.DongDauMay ddm ON ddm.MaDongDauMay = dm.MaDongDauMay
CROSS APPLY (SELECT CAST(ROUND(CAST(nl.SoLitTraNap AS DECIMAL(18,6)) * 10000
                               / (nl.TrongLuongKeoTan * nl.CuLyChayKm), 2) AS DECIMAL(18,2)) AS Sfc) v
WHERE v.Sfc <= 999.99
  AND (nl.SuatTieuHaoSFC <> v.Sfc OR nl.CanhBaoVuotMuc <> CASE WHEN v.Sfc > ddm.DinhMucSFC THEN 1 ELSE 0 END);');

COMMIT TRANSACTION;
GO

-- Kiểm tra sau khi chạy
SELECT MaDongCode, DinhMucSFC FROM phuongtien.DongDauMay ORDER BY MaDongCode;
SELECT nl.MaNhatKyDau, dm.SoHieuDauMay, nl.SuatTieuHaoSFC, nl.CanhBaoVuotMuc, nl.DonGiaVndLit, nl.ChiPhi, nl.ThoiDiemBomDau
FROM baotri.NhatKyCapNhienLieu nl JOIN phuongtien.DauMay dm ON dm.MaDauMay = nl.MaDauMay
ORDER BY nl.MaNhatKyDau;
PRINT N'V20261024_1000: xong (chi phí, định mức SFC, giờ Việt Nam, PhienBan phiếu cấp dầu / bảo dưỡng).';
GO

GO
INSERT #TronGoi (Buoc) VALUES (8);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [9/17] seed\S20261024_1100__dat__baotri-sprint2.sql ---';
GO
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

GO
INSERT #TronGoi (Buoc) VALUES (9);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [10/17] seed\S20261026_1000__dat__ve-mau-doanh-thu.sql ---';
GO
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

GO
INSERT #TronGoi (Buoc) VALUES (10);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [11/17] migrations\V20261108_1000__dat__phanquyen-dangnhap-quyen.sql ---';
GO
/* =========================================================================
   MIGRATION: Quản trị & phân quyền — đăng nhập, khoá sau 5 lần sai, danh mục quyền / vai trò
   Tác giả  : Đạt
   Ngày     : 08/11/2026 (Sprint 3)
   Bảng     : phanquyen.TaiKhoan, Quyen, VaiTro, VaiTro_Quyen, LichSuDangNhap; kiemtoan.NhatKyHeThong (chủ: Đạt)

   Lý do (Đạt chốt 08/11, theo khuyến nghị):
   1. Khoá tài khoản sau 5 lần sai liên tiếp (báo cáo FR-7.1): thêm SoLanSaiLienTiep + KhoaDenLuc (giờ UTC,
      như các cột thời gian khác của phanquyen). Khoá 15 phút rồi tự mở; quản trị viên mở tay được.
   2. PhaiDoiMatKhau: quản trị viên đặt lại mật khẩu -> người dùng phải đổi ở lần đăng nhập kế tiếp.
   3. PhienBan ROWVERSION: hai quản trị viên cùng sửa một tài khoản thì người lưu sau được báo.
   4. Danh mục quyền: mỗi phân hệ 1 quyền XEM + các quyền GHI -> khớp ma trận F / R / - của báo cáo
      (R = có quyền xem, không có quyền ghi). Tab hiện khi có quyền xem HOẶC một quyền ghi của phân hệ.
      Gồm 8 quyền gốc + 8 quyền ghi đã dùng trong mã (MaQuyen trong DTO/Common/PhienLamViec.cs)
      + 8 quyền xem mới = 24 quyền.
   5. Vai trò mới: LAP_TAU, NHAN_SU, PHAN_CONG_KIP, QUAN_LY (SRS chức năng của Đạt). Gán quyền cho cả
      10 vai trò; ADMIN có mọi quyền.
   6. Chỉ mục cho màn Lịch sử đăng nhập / Nhật ký hệ thống (lọc theo thời gian, tài khoản, bảng).

   Mật khẩu: chuỗi băm PBKDF2-SHA256 (BUS/Services/MatKhauHelper.cs). Mật khẩu demo đặt ở seed
   S20261108_1100 (chuỗi băm BCrypt cũ trong script gốc không ai biết mật khẩu gốc).

   Thứ tự: chạy sau script gốc (không phụ thuộc seed). Chạy lại được nhiều lần: chỉ thêm cái còn thiếu,
   không xoá quyền quản trị viên đã gán tay.
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

IF OBJECT_ID(N'phanquyen.TaiKhoan', N'U') IS NULL OR OBJECT_ID(N'phanquyen.Quyen', N'U') IS NULL
BEGIN
    RAISERROR(N'Chưa có schema phanquyen. Chạy script tạo CSDL gốc trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Cột mới của TaiKhoan -------------------------------------------------------------------
IF COL_LENGTH(N'phanquyen.TaiKhoan', N'SoLanSaiLienTiep') IS NULL
    ALTER TABLE phanquyen.TaiKhoan ADD SoLanSaiLienTiep INT NOT NULL
        CONSTRAINT DF_TaiKhoan_SoLanSai DEFAULT 0
        CONSTRAINT CK_TaiKhoan_SoLanSai CHECK (SoLanSaiLienTiep >= 0);
GO
IF COL_LENGTH(N'phanquyen.TaiKhoan', N'KhoaDenLuc') IS NULL
    ALTER TABLE phanquyen.TaiKhoan ADD KhoaDenLuc DATETIME2 NULL;      -- giờ UTC; NULL = không khoá
GO
IF COL_LENGTH(N'phanquyen.TaiKhoan', N'PhaiDoiMatKhau') IS NULL
    ALTER TABLE phanquyen.TaiKhoan ADD PhaiDoiMatKhau BIT NOT NULL
        CONSTRAINT DF_TaiKhoan_PhaiDoiMatKhau DEFAULT 0;
GO
IF COL_LENGTH(N'phanquyen.TaiKhoan', N'PhienBan') IS NULL
    ALTER TABLE phanquyen.TaiKhoan ADD PhienBan ROWVERSION;
GO

-- 2. Chỉ mục ---------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichSuDangNhap_ThoiDiem'
                 AND object_id = OBJECT_ID(N'phanquyen.LichSuDangNhap'))
    CREATE INDEX IX_LichSuDangNhap_ThoiDiem ON phanquyen.LichSuDangNhap (ThoiDiemDangNhap DESC)
        INCLUDE (MaTaiKhoan, TenDangNhap, ThanhCong);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichSuDangNhap_TaiKhoan'
                 AND object_id = OBJECT_ID(N'phanquyen.LichSuDangNhap'))
    CREATE INDEX IX_LichSuDangNhap_TaiKhoan ON phanquyen.LichSuDangNhap (MaTaiKhoan, ThoiDiemDangNhap DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhatKyHeThong_ThoiDiem'
                 AND object_id = OBJECT_ID(N'kiemtoan.NhatKyHeThong'))
    CREATE INDEX IX_NhatKyHeThong_ThoiDiem ON kiemtoan.NhatKyHeThong (ThoiDiem DESC)
        INCLUDE (MaTaiKhoan, HanhDong, TenBang, KhoaChinhRecord);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhatKyHeThong_TaiKhoan'
                 AND object_id = OBJECT_ID(N'kiemtoan.NhatKyHeThong'))
    CREATE INDEX IX_NhatKyHeThong_TaiKhoan ON kiemtoan.NhatKyHeThong (MaTaiKhoan, ThoiDiem DESC);
GO

BEGIN TRANSACTION;

-- 3. Danh mục quyền (thêm cái còn thiếu, cập nhật tên / nhóm / mô tả cho thống nhất) -----------
DECLARE @Quyen TABLE (Ma VARCHAR(50) PRIMARY KEY, Ten NVARCHAR(100), Nhom VARCHAR(50), MoTa NVARCHAR(255));
INSERT INTO @Quyen VALUES
-- Quản trị
('SYS_CONFIG',               N'Quản trị hệ thống',                 'HE_THONG',    N'Tài khoản, vai trò & quyền, lịch sử đăng nhập, nhật ký hệ thống'),
('REPORT_VIEW',              N'Xem báo cáo thống kê',              'BAO_CAO',     N'Trang báo cáo tổng hợp: doanh thu, hệ số sử dụng ghế, tỷ lệ đúng giờ'),
('AI_ADVISOR',               N'Dùng trợ lý AI',                    'AI',          N'Tư vấn vé, trợ lý điều độ'),
-- Mạng lưới & lịch trình (A Bá)
('NETWORK_VIEW',             N'Xem mạng lưới & lịch trình',        'DIEU_DO',     N'Xem ga, khu gian, mác tàu, lịch trình, biểu đồ chạy tàu'),
('INFRA_EDIT',               N'Sửa hạ tầng & mác tàu',             'DIEU_DO',     N'Thêm / sửa ga, đường ray, khu gian, đường ngang, mác tàu mẫu'),
('TRAIN_SCHEDULE_EDIT',      N'Lập biểu đồ chạy tàu',              'DIEU_DO',     N'Lập / sửa chuyến tàu, lịch dừng ga, xếp tránh'),
('BLOCK_ORDER_APPROVE',      N'Duyệt lệnh phong tỏa',              'DIEU_DO',     N'Phát lệnh cấm hoặc hạn chế tốc độ khu gian'),
('DISPATCH_INCIDENT_REPORT', N'Ghi nhận sự cố chậm giờ',           'DIEU_DO',     N'Ghi giờ thực tế, lý do và số phút chậm'),
-- Kinh doanh vận tải (A Bá)
('TICKET_VIEW',              N'Xem bán vé & soát vé',              'VAN_TAI',     N'Tra cứu vé, sơ đồ ghế, danh sách hành khách'),
('TICKET_ISSUE',             N'Bán và in vé tàu',                  'VAN_TAI',     N'Đặt vé, đổi ghế, in thẻ lên tàu'),
('TICKET_REFUND',            N'Hoàn và hủy vé tàu',                'VAN_TAI',     N'Trả vé và hoàn tiền'),
('TICKET_CHECK',             N'Soát vé lên tàu',                   'VAN_TAI',     N'Xác nhận / hủy soát vé tại ga'),
('CARGO_VIEW',               N'Xem vận đơn hàng hóa',              'VAN_TAI',     N'Tra cứu vận đơn, tiến độ vận chuyển'),
('CARGO_DISPATCH',           N'Lập vận đơn hàng hóa',              'VAN_TAI',     N'Nhận hàng hóa, xe máy, container; cập nhật trạng thái'),
-- Kỹ thuật & đoàn tàu (Đạt)
('VEHICLE_VIEW',             N'Xem phương tiện',                   'PHUONG_TIEN', N'Xem đầu máy, toa xe, đoàn tàu, sơ đồ chỗ ngồi'),
('VEHICLE_EDIT',             N'Quản lý đầu máy & toa xe',          'PHUONG_TIEN', N'Thêm / sửa / xóa / ngừng khai thác đầu máy, toa xe'),
('TRAIN_FORMATION_EDIT',     N'Lập đoàn tàu',                      'PHUONG_TIEN', N'Lập / sửa đoàn tàu, cắt / nối toa dọc đường'),
('MAINT_VIEW',               N'Xem bảo trì & cảnh báo',            'KY_THUAT',    N'Xem biên bản khám xe, nhiên liệu, bảo dưỡng, cảnh báo, chi phí'),
('TECH_SAFETY_APPROVE',      N'Nghiệm thu khám xe',                'KY_THUAT',    N'Lập biên bản khám xe, duyệt an toàn đoàn tàu'),
('MAINTENANCE_LOG',          N'Ghi nhật ký bảo trì',               'KY_THUAT',    N'Cấp phát nhiên liệu, ghi nhận bảo dưỡng định kỳ'),
-- Nhân sự (Đạt)
('STAFF_VIEW',               N'Xem nhân sự & lịch ca',             'NHAN_SU',     N'Xem hồ sơ nhân viên, kíp lái, lịch ca'),
('STAFF_EDIT',               N'Quản lý hồ sơ nhân viên',           'NHAN_SU',     N'Thêm / sửa / xóa hồ sơ, cho nghỉ việc'),
('CREW_ASSIGN',              N'Phân công kíp lái',                 'NHAN_SU',     N'Lập / sửa / hủy kíp lái theo chặng'),
('CREW_CHECKIN',             N'Kiểm tra lên ban & giao nhận ca',   'NHAN_SU',     N'Ghi kiểm tra lên ban, nhận ban, bàn giao');

MERGE phanquyen.Quyen AS q
USING @Quyen AS s ON q.MaQuyenCode = s.Ma
WHEN MATCHED AND (q.TenQuyen <> s.Ten OR q.NhomQuyen <> s.Nhom OR ISNULL(q.MoTa, N'') <> s.MoTa)
    THEN UPDATE SET TenQuyen = s.Ten, NhomQuyen = s.Nhom, MoTa = s.MoTa
WHEN NOT MATCHED BY TARGET
    THEN INSERT (MaQuyenCode, TenQuyen, NhomQuyen, MoTa) VALUES (s.Ma, s.Ten, s.Nhom, s.MoTa);

-- 4. Vai trò mới ---------------------------------------------------------------------------------
DECLARE @VaiTro TABLE (Ma VARCHAR(30) PRIMARY KEY, Ten NVARCHAR(100), MoTa NVARCHAR(255));
INSERT INTO @VaiTro VALUES
('LAP_TAU',       N'Quản đốc lập tàu',          N'Quản lý đầu máy, toa xe; lập đoàn tàu, cắt / nối toa'),
('NHAN_SU',       N'Cán bộ nhân sự',            N'Quản lý hồ sơ nhân viên vận hành'),
('PHAN_CONG_KIP', N'Quản đốc kíp lái',          N'Phân công kíp lái, kiểm tra lên ban, giao nhận ca'),
('QUAN_LY',       N'Cán bộ quản lý',            N'Xem toàn bộ hệ thống và báo cáo thống kê, không sửa dữ liệu nghiệp vụ');

INSERT INTO phanquyen.VaiTro (MaVaiTroCode, TenVaiTro, MoTa)
SELECT s.Ma, s.Ten, s.MoTa FROM @VaiTro s
WHERE NOT EXISTS (SELECT 1 FROM phanquyen.VaiTro v WHERE v.MaVaiTroCode = s.Ma);

-- 5. Gán quyền cho vai trò (chỉ thêm cặp còn thiếu) ------------------------------------------------
DECLARE @Gan TABLE (VaiTro VARCHAR(30), Quyen VARCHAR(50), PRIMARY KEY (VaiTro, Quyen));
INSERT INTO @Gan VALUES
('DIEU_DO', 'NETWORK_VIEW'), ('DIEU_DO', 'TRAIN_SCHEDULE_EDIT'), ('DIEU_DO', 'BLOCK_ORDER_APPROVE'),
('DIEU_DO', 'DISPATCH_INCIDENT_REPORT'), ('DIEU_DO', 'VEHICLE_VIEW'), ('DIEU_DO', 'MAINT_VIEW'),
('DIEU_DO', 'STAFF_VIEW'), ('DIEU_DO', 'TICKET_VIEW'), ('DIEU_DO', 'REPORT_VIEW'), ('DIEU_DO', 'AI_ADVISOR'),

('TRUONG_GA', 'NETWORK_VIEW'), ('TRUONG_GA', 'INFRA_EDIT'), ('TRUONG_GA', 'TICKET_VIEW'),
('TRUONG_GA', 'TICKET_CHECK'), ('TRUONG_GA', 'CARGO_VIEW'),

('BAN_VE', 'NETWORK_VIEW'), ('BAN_VE', 'TICKET_VIEW'), ('BAN_VE', 'TICKET_ISSUE'), ('BAN_VE', 'TICKET_REFUND'),
('BAN_VE', 'TICKET_CHECK'), ('BAN_VE', 'CARGO_VIEW'), ('BAN_VE', 'CARGO_DISPATCH'), ('BAN_VE', 'AI_ADVISOR'),

('LAI_TAU', 'NETWORK_VIEW'), ('LAI_TAU', 'STAFF_VIEW'), ('LAI_TAU', 'MAINT_VIEW'),

('KY_THUAT', 'NETWORK_VIEW'), ('KY_THUAT', 'VEHICLE_VIEW'), ('KY_THUAT', 'MAINT_VIEW'),
('KY_THUAT', 'TECH_SAFETY_APPROVE'), ('KY_THUAT', 'MAINTENANCE_LOG'),

('LAP_TAU', 'NETWORK_VIEW'), ('LAP_TAU', 'VEHICLE_VIEW'), ('LAP_TAU', 'VEHICLE_EDIT'),
('LAP_TAU', 'TRAIN_FORMATION_EDIT'), ('LAP_TAU', 'MAINT_VIEW'),

('NHAN_SU', 'NETWORK_VIEW'), ('NHAN_SU', 'STAFF_VIEW'), ('NHAN_SU', 'STAFF_EDIT'),

('PHAN_CONG_KIP', 'NETWORK_VIEW'), ('PHAN_CONG_KIP', 'STAFF_VIEW'), ('PHAN_CONG_KIP', 'CREW_ASSIGN'),
('PHAN_CONG_KIP', 'CREW_CHECKIN'), ('PHAN_CONG_KIP', 'VEHICLE_VIEW'),

('QUAN_LY', 'NETWORK_VIEW'), ('QUAN_LY', 'TICKET_VIEW'), ('QUAN_LY', 'CARGO_VIEW'), ('QUAN_LY', 'VEHICLE_VIEW'),
('QUAN_LY', 'MAINT_VIEW'), ('QUAN_LY', 'STAFF_VIEW'), ('QUAN_LY', 'REPORT_VIEW'), ('QUAN_LY', 'AI_ADVISOR');

INSERT INTO phanquyen.VaiTro_Quyen (MaVaiTro, MaQuyen)
SELECT v.MaVaiTro, q.MaQuyen
FROM @Gan g
JOIN phanquyen.VaiTro v ON v.MaVaiTroCode = g.VaiTro
JOIN phanquyen.Quyen q ON q.MaQuyenCode = g.Quyen
WHERE NOT EXISTS (SELECT 1 FROM phanquyen.VaiTro_Quyen x WHERE x.MaVaiTro = v.MaVaiTro AND x.MaQuyen = q.MaQuyen);

-- ADMIN: mọi quyền
INSERT INTO phanquyen.VaiTro_Quyen (MaVaiTro, MaQuyen)
SELECT v.MaVaiTro, q.MaQuyen
FROM phanquyen.VaiTro v CROSS JOIN phanquyen.Quyen q
WHERE v.MaVaiTroCode = 'ADMIN'
  AND NOT EXISTS (SELECT 1 FROM phanquyen.VaiTro_Quyen x WHERE x.MaVaiTro = v.MaVaiTro AND x.MaQuyen = q.MaQuyen);

COMMIT TRANSACTION;
GO

-- Kết quả
SELECT v.MaVaiTroCode AS VaiTro, COUNT(vq.MaQuyen) AS SoQuyen
FROM phanquyen.VaiTro v LEFT JOIN phanquyen.VaiTro_Quyen vq ON vq.MaVaiTro = v.MaVaiTro
GROUP BY v.MaVaiTroCode ORDER BY v.MaVaiTroCode;
GO
-- [tron goi] bo SET NOEXEC OFF: buoc nay loi thi dung ca file
GO

GO
INSERT #TronGoi (Buoc) VALUES (11);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [12/17] seed\S20261108_1100__dat__tai-khoan-demo.sql ---';
GO
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
-- [tron goi] bo SET NOEXEC OFF: buoc nay loi thi dung ca file
GO

GO
INSERT #TronGoi (Buoc) VALUES (12);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [13/17] seed\S20261108_1200__dat__lich-su-bao-cao.sql ---';
GO
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
-- [tron goi] bo SET NOEXEC OFF: buoc nay loi thi dung ca file
GO

GO
INSERT #TronGoi (Buoc) VALUES (13);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
-- Lưu bản sao lịch dừng ga chuẩn ban đầu vào vanhanh.LichDungGaGoc phục vụ tác nghiệp điều độ
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'vanhanh' AND TABLE_NAME = 'LichDungGaGoc')
BEGIN
    CREATE TABLE vanhanh.LichDungGaGoc (
        MaChuyenTau INT NOT NULL,
        MaGa INT NOT NULL,
        ThuTuDung INT NOT NULL,
        GioDenKeHoach DATETIME2 NOT NULL,
        GioDiKeHoach DATETIME2 NOT NULL,
        MaDuongRay INT NULL,
        CONSTRAINT PK_LichDungGaGoc PRIMARY KEY (MaChuyenTau, ThuTuDung)
    );
END
GO

INSERT INTO vanhanh.LichDungGaGoc (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, MaDuongRay)
SELECT ld.MaChuyenTau, ld.MaGa, ld.ThuTuDung, ld.GioDenKeHoach, ld.GioDiKeHoach, ld.MaDuongRay
FROM vanhanh.LichDungGa ld
WHERE NOT EXISTS (SELECT 1 FROM vanhanh.LichDungGaGoc g WHERE g.MaChuyenTau = ld.MaChuyenTau AND g.ThuTuDung = ld.ThuTuDung);
GO

PRINT N'--- [14/17] migrations\V20261108_1300__dat__baocao-views.sql ---';
GO
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
-- [tron goi] bo SET NOEXEC OFF: buoc nay loi thi dung ca file
GO

GO
INSERT #TronGoi (Buoc) VALUES (14);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [15/17] migrations\V20261113_1000__dat__toabanve-dongbo-doantau.sql ---';
GO
/* =========================================================================
   MIGRATION: Toa bán vé đồng bộ theo đoàn tàu + chặn bán vé ngoài phạm vi toa
   Tác giả  : Đạt
   Ngày     : 13/11/2026 (Sprint 3)
   Bảng     : vantai.ToaXeKhach (thêm cột), vantai.sp_DatVeTheoChang (thủ tục), baocao.vw_HeSoSuDungGhe (view)
              — bảng / thủ tục của A Bá, chỉ CỘNG THÊM (cột NULL / có DEFAULT, thủ tục giữ nguyên tham số).

   Đạt chốt 13/11 (theo khuyến nghị):
   1. Toa bán vé nối với đoàn tàu THEO SỐ TOA KHÁCH: toa khách (NC / NML / BN / AN) thứ k trong đoàn tàu
      (thứ tự móc nối) ↔ "Toa k" bán vé (ThuTuToa = k). Thay toa thật cùng loại thì vé giữ nguyên toa / chỗ.
      Thêm MaToaXe (toa thật đang phục vụ) + MaGaNoi / MaGaCat (phạm vi chạy, chép từ ChiTietDoanTau).
   2. Lưu đoàn tàu làm hỏng vé còn hiệu lực → CHẶN, liệt kê vé (LapTauService). Toa chỉ còn vé đã hoàn / đã hủy
      thì không xoá được (khoá ngoại vantai.Ve → ChoNgoi) → NgungBan = 1: giữ lịch sử, Bán vé không hiện.
      UNIQUE (MaChuyenTau, ThuTuToa) đổi thành chỉ mục duy nhất CÓ LỌC (NgungBan = 0) để "Toa k" mới thay
      được toa k cũ đã ngừng bán.
   3. Bán vé chặn 2 lớp: danh sách toa chỉ hiện toa chạy qua cả chặng (fn_ToaChayQuaChang), và thủ tục đặt vé
      kiểm lại trong giao dịch (ghế thuộc toa đang bán của đúng chuyến, toa chạy qua chặng).
      Khoá ứng dụng "vantai.BanVe:<MaChuyenTau>": đặt vé lấy Shared, đồng bộ toa (lưu đoàn tàu) lấy Exclusive
      → không bán vào ghế đang bị bỏ giữa lúc đồng bộ kiểm tra vé và lúc ghi.

   Bổ sung dữ liệu: điền MaToaXe / MaGaNoi / MaGaCat cho toa bán vé hiện có khớp đoàn tàu theo số toa + loại toa.

   Lưu ý: chỉ mục có lọc đòi QUOTED_IDENTIFIER ON khi ghi bảng ToaXeKhach (ADO.NET, SSMS mặc định ON;
   sqlcmd phải có -I như các script khác).
   Chạy lại được nhiều lần. Không có USE: sqlcmd -S . -E -I -f 65001 -d <TênCSDL> -i <file>
   ========================================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb')
BEGIN
    RAISERROR(N'Đang đứng ở CSDL hệ thống. Chạy lại với -d QuanLyDuongSatV2 (hoặc CSDL kiểm thử).', 16, 1);
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'vantai.ToaXeKhach', N'U') IS NULL OR COL_LENGTH(N'vanhanh.ChiTietDoanTau', N'MaGaNoi') IS NULL
BEGIN
    RAISERROR(N'Chưa có vantai.ToaXeKhach hoặc ChiTietDoanTau.MaGaNoi. Chạy script gốc + migration V20261009_0820 trước.', 16, 1);
    SET NOEXEC ON;
END;
GO

-- 1. Cột mới của ToaXeKhach -------------------------------------------------------------------
IF COL_LENGTH(N'vantai.ToaXeKhach', N'MaToaXe') IS NULL
    ALTER TABLE vantai.ToaXeKhach ADD MaToaXe INT NULL
        CONSTRAINT FK_ToaXeKhach_ToaXe REFERENCES phuongtien.ToaXe(MaToaXe);
GO
IF COL_LENGTH(N'vantai.ToaXeKhach', N'MaGaNoi') IS NULL
    ALTER TABLE vantai.ToaXeKhach ADD MaGaNoi INT NULL
        CONSTRAINT FK_ToaXeKhach_GaNoi REFERENCES hatang.Ga(MaGa);
GO
IF COL_LENGTH(N'vantai.ToaXeKhach', N'MaGaCat') IS NULL
    ALTER TABLE vantai.ToaXeKhach ADD MaGaCat INT NULL
        CONSTRAINT FK_ToaXeKhach_GaCat REFERENCES hatang.Ga(MaGa);
GO
IF COL_LENGTH(N'vantai.ToaXeKhach', N'NgungBan') IS NULL
    ALTER TABLE vantai.ToaXeKhach ADD NgungBan BIT NOT NULL
        CONSTRAINT DF_ToaXeKhach_NgungBan DEFAULT 0;
GO

-- 2. "Toa k" duy nhất trong các toa đang bán ---------------------------------------------------
IF OBJECT_ID(N'vantai.UQ_ChuyenTau_ThuTuToa', N'UQ') IS NOT NULL
    ALTER TABLE vantai.ToaXeKhach DROP CONSTRAINT UQ_ChuyenTau_ThuTuToa;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ToaXeKhach_ChuyenTau_ThuTu'
               AND object_id = OBJECT_ID(N'vantai.ToaXeKhach'))
    CREATE UNIQUE INDEX UX_ToaXeKhach_ChuyenTau_ThuTu ON vantai.ToaXeKhach (MaChuyenTau, ThuTuToa)
        WHERE NgungBan = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ToaXeKhach_MaToaXe'
               AND object_id = OBJECT_ID(N'vantai.ToaXeKhach'))
    CREATE INDEX IX_ToaXeKhach_MaToaXe ON vantai.ToaXeKhach (MaToaXe);
GO

-- 3. Bổ sung liên kết cho toa bán vé đã có (khớp số toa khách + loại toa) ------------------------
;WITH K AS (
    SELECT dt.MaChuyenTau, ctd.MaToaXe, cl.MaChungLoaiCode AS LoaiToa, ctd.MaGaNoi, ctd.MaGaCat,
           ROW_NUMBER() OVER (PARTITION BY dt.MaChuyenTau ORDER BY ctd.ThuTuNoi) AS ThuTuKhach
    FROM vanhanh.DoanTau dt
    JOIN vanhanh.ChiTietDoanTau ctd ON ctd.MaDoanTau = dt.MaDoanTau
    JOIN phuongtien.ToaXe tx ON tx.MaToaXe = ctd.MaToaXe
    JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
    WHERE cl.MaChungLoaiCode IN ('NC', 'NML', 'BN', 'AN'))
UPDATE t
SET MaToaXe = K.MaToaXe, MaGaNoi = K.MaGaNoi, MaGaCat = K.MaGaCat
FROM vantai.ToaXeKhach t
JOIN K ON K.MaChuyenTau = t.MaChuyenTau AND K.ThuTuKhach = t.ThuTuToa AND K.LoaiToa = t.LoaiToa
WHERE t.NgungBan = 0 AND t.MaToaXe IS NULL;
PRINT CONCAT(N'Đã nối ', @@ROWCOUNT, N' toa bán vé với toa thật trong đoàn tàu.');
GO

-- 4. Toa bán vé có chạy qua chặng ga đi → ga đến không ---------------------------------------
--    Toa chạy suốt (không ga nối / ga cắt) → 1. Toa nối / cắt dọc đường: ga đi không trước ga nối,
--    ga đến không sau ga cắt (theo thứ tự lịch dừng của chuyến). Không xác định được thứ tự → 0 (không bán).
CREATE OR ALTER FUNCTION vantai.fn_ToaChayQuaChang (@MaToaXeKhach INT, @MaGaDi INT, @MaGaDen INT)
RETURNS BIT
AS
BEGIN
    DECLARE @ct INT, @noi INT, @cat INT;
    SELECT @ct = MaChuyenTau, @noi = MaGaNoi, @cat = MaGaCat
    FROM vantai.ToaXeKhach WHERE MaToaXeKhach = @MaToaXeKhach;

    IF @ct IS NULL RETURN 0;
    IF @noi IS NULL AND @cat IS NULL RETURN 1;

    DECLARE @tDi INT = (SELECT MIN(ThuTuDung) FROM vanhanh.LichDungGa WHERE MaChuyenTau = @ct AND MaGa = @MaGaDi);
    DECLARE @tDen INT = (SELECT MAX(ThuTuDung) FROM vanhanh.LichDungGa WHERE MaChuyenTau = @ct AND MaGa = @MaGaDen);
    IF @tDi IS NULL OR @tDen IS NULL RETURN 0;

    IF @noi IS NOT NULL
       AND @tDi < ISNULL((SELECT MIN(ThuTuDung) FROM vanhanh.LichDungGa WHERE MaChuyenTau = @ct AND MaGa = @noi), 2147483647)
        RETURN 0;
    IF @cat IS NOT NULL
       AND @tDen > ISNULL((SELECT MAX(ThuTuDung) FROM vanhanh.LichDungGa WHERE MaChuyenTau = @ct AND MaGa = @cat), -1)
        RETURN 0;
    RETURN 1;
END;
GO

-- 5. Thủ tục đặt vé: giữ nguyên tham số + cách kiểm chặng giao nhau của A Bá, thêm 3 kiểm tra -------
CREATE OR ALTER PROCEDURE vantai.sp_DatVeTheoChang
    @MaDonVe            INT,
    @MaChuyenTau        INT,
    @MaChoNgoi          INT,
    @MaGaDi             INT,
    @MaGaDen            INT,
    @TenHanhKhach       NVARCHAR(100),
    @CCCDHanhKhach      VARCHAR(20),
    @GiaVeGoc           DECIMAL(12,2),
    @SoTienGiam         DECIMAL(12,2) = 0.0,
    @MaVeCodeOutput     VARCHAR(30) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @KmDi DECIMAL(7,2);
    DECLARE @KmDen DECIMAL(7,2);
    DECLARE @GiaVeThucThu DECIMAL(12,2) = @GiaVeGoc - @SoTienGiam;

    SELECT @KmDi = LyTrinhKm FROM hatang.Ga WHERE MaGa = @MaGaDi;
    SELECT @KmDen = LyTrinhKm FROM hatang.Ga WHERE MaGa = @MaGaDen;

    IF @KmDi IS NULL OR @KmDen IS NULL
    BEGIN
        RAISERROR(N'Ga đi hoặc Ga đến không hợp lệ.', 16, 1);
        RETURN -1;
    END;

    IF @MaVeCodeOutput IS NULL OR LEN(@MaVeCodeOutput) = 0
        SET @MaVeCodeOutput = 'TK_' + UPPER(SUBSTRING(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 1, 10));

    BEGIN TRANSACTION;
    BEGIN TRY
        -- [13/11 Đạt] Khoá "bán vé của chuyến" (Shared): lưu đoàn tàu đồng bộ toa bán vé lấy Exclusive.
        DECLARE @TaiNguyen NVARCHAR(255) = CONCAT(N'vantai.BanVe:', @MaChuyenTau);
        DECLARE @KqKhoa INT;
        EXEC @KqKhoa = sp_getapplock @Resource = @TaiNguyen, @LockMode = 'Shared',
                                     @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @KqKhoa < 0
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'Đoàn tàu của chuyến đang được cập nhật, chưa đặt được vé. Vui lòng thử lại sau ít giây.', 16, 1);
            RETURN -3;
        END;

        -- [13/11 Đạt] Ghế phải thuộc toa đang bán của đúng chuyến, và toa chạy qua cả chặng của khách
        DECLARE @ToaKhach INT, @ChuyenCuaToa INT, @NgungBan BIT, @NhanToa NVARCHAR(20);
        SELECT @ToaKhach = tx.MaToaXeKhach, @ChuyenCuaToa = tx.MaChuyenTau, @NgungBan = tx.NgungBan, @NhanToa = tx.NhanHieuToa
        FROM vantai.ChoNgoi cn
        JOIN vantai.ToaXeKhach tx ON tx.MaToaXeKhach = cn.MaToaXeKhach
        WHERE cn.MaChoNgoi = @MaChoNgoi;

        IF @ToaKhach IS NULL OR @ChuyenCuaToa <> @MaChuyenTau
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'Chỗ ngồi không thuộc chuyến tàu này. Hãy tải lại sơ đồ toa.', 16, 1);
            RETURN -4;
        END;
        IF @NgungBan = 1
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'%s đã ngừng bán vì đoàn tàu vừa được lập lại. Hãy tải lại sơ đồ toa rồi chọn chỗ khác.', 16, 1, @NhanToa);
            RETURN -5;
        END;
        IF vantai.fn_ToaChayQuaChang(@ToaKhach, @MaGaDi, @MaGaDen) = 0
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'%s không chạy qua cả chặng của khách (toa nối / cắt dọc đường). Hãy chọn toa khác.', 16, 1, @NhanToa);
            RETURN -6;
        END;

        -- Khóa chỗ ngồi trong chặng để loại trừ check-then-insert race condition (Chuẩn 3NF tra cứu qua bảng Ga)
        IF EXISTS (
            SELECT 1
            FROM vantai.Ve v WITH (UPDLOCK, HOLDLOCK)
            JOIN hatang.Ga gDi ON v.MaGaDi = gDi.MaGa
            JOIN hatang.Ga gDen ON v.MaGaDen = gDen.MaGa
            WHERE v.MaChuyenTau = @MaChuyenTau
              AND v.MaChoNgoi = @MaChoNgoi
              AND v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU')
              AND NOT (
                  IIF(gDi.LyTrinhKm < gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) >= IIF(@KmDi < @KmDen, @KmDen, @KmDi)
                  OR IIF(gDi.LyTrinhKm > gDen.LyTrinhKm, gDi.LyTrinhKm, gDen.LyTrinhKm) <= IIF(@KmDi < @KmDen, @KmDi, @KmDen)
              )
        )
        BEGIN
            ROLLBACK TRANSACTION;
            RAISERROR(N'Chỗ ngồi đã có khách đặt trên cung chặng trùng lặp.', 16, 1);
            RETURN -2;
        END;

        INSERT INTO vantai.Ve (
            MaDonVe, MaVeCode, MaChuyenTau, MaChoNgoi,
            MaGaDi, MaGaDen,
            TenHanhKhach, CCCDHanhKhach,
            GiaVeGoc, SoTienGiam,
            TrangThai, ThoiDiemXuatVe
        ) VALUES (
            @MaDonVe, @MaVeCodeOutput, @MaChuyenTau, @MaChoNgoi,
            @MaGaDi, @MaGaDen,
            @TenHanhKhach, @CCCDHanhKhach,
            @GiaVeGoc, @SoTienGiam,
            'DA_DAT', SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 6. Hệ số sử dụng ghế: không tính toa đã ngừng bán (toa bị thay trước giờ tàu chạy) ------------
IF OBJECT_ID(N'baocao.vw_HanhTrinhChuyen', N'V') IS NOT NULL
    EXEC (N'
CREATE OR ALTER VIEW baocao.vw_HeSoSuDungGhe AS
SELECT ht.MaChuyenTau, ht.NgayChay, ht.SoHieuMacTau, ht.LoaiTau, ht.CuLyKm,
       g.SoGhe, ISNULL(b.SoVe, 0) AS SoVe,
       CAST(g.SoGhe * ht.CuLyKm AS DECIMAL(18, 2)) AS GheKmCungUng,
       CAST(ISNULL(b.GheKmDaBan, 0) AS DECIMAL(18, 2)) AS GheKmDaBan,
       CAST(CASE WHEN g.SoGhe * ht.CuLyKm > 0 THEN ISNULL(b.GheKmDaBan, 0) / (g.SoGhe * ht.CuLyKm) ELSE 0 END AS DECIMAL(9, 4)) AS HeSo
FROM baocao.vw_HanhTrinhChuyen ht
CROSS APPLY (SELECT COUNT(*) AS SoGhe
             FROM vantai.ToaXeKhach t JOIN vantai.ChoNgoi c ON c.MaToaXeKhach = t.MaToaXeKhach
             WHERE t.MaChuyenTau = ht.MaChuyenTau AND c.LaGhePhu = 0 AND t.NgungBan = 0) g
OUTER APPLY (SELECT COUNT(*) AS SoVe, SUM(ABS(gDen.LyTrinhKm - gDi.LyTrinhKm)) AS GheKmDaBan
             FROM vantai.Ve v
             JOIN hatang.Ga gDi ON gDi.MaGa = v.MaGaDi
             JOIN hatang.Ga gDen ON gDen.MaGa = v.MaGaDen
             WHERE v.MaChuyenTau = ht.MaChuyenTau AND v.TrangThai IN (''DA_DAT'', ''DA_LEN_TAU'')) b
WHERE ht.TrangThai <> ''DA_HUY'' AND g.SoGhe > 0;');
GO

-- Kết quả
SELECT COUNT(*) AS ToaBanVe,
       SUM(CASE WHEN MaToaXe IS NOT NULL THEN 1 ELSE 0 END) AS DaNoiToaThat,
       SUM(CASE WHEN MaGaNoi IS NOT NULL OR MaGaCat IS NOT NULL THEN 1 ELSE 0 END) AS CoPhamViGa,
       SUM(CASE WHEN NgungBan = 1 THEN 1 ELSE 0 END) AS NgungBan
FROM vantai.ToaXeKhach;
GO
-- [tron goi] bo SET NOEXEC OFF: buoc nay loi thi dung ca file
GO

GO
INSERT #TronGoi (Buoc) VALUES (15);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [16/17] seed\S20261113_1200__dat__kich-hoat-ban-ve-truoc-cuoi-tuan.sql ---';
GO
-- ==============================================================================================
-- DỰ ÁN: HỆ THỐNG QUẢN LÝ GA & VẬN TẢI ĐƯỜNG SẮT (QUAN LY GA DUONG SAT)
-- SCRIPT: KÍCH HOẠT DỮ LIỆU BÁN VÉ TRƯỚC (ADVANCE BOOKING) CHO CÁC NGÀY TRONG TUẦN & CUỐI TUẦN
-- TÁC GIẢ: Nhóm Phát Triển Đồ Án Tốt Nghiệp TDC
-- NGÀY TẠO: 13/11/2026
-- ==============================================================================================
-- MỤC ĐÍCH:
--   1. Giải quyết bài toán "Mua vé cuối tuần từ đầu tuần" (Advance Booking).
--   2. Tự động sinh lịch chạy tàu (vanhanh.ChuyenTau) trạng thái DA_LEN_LICH cho các ngày Thứ 6,
--      Thứ 7, Chủ Nhật trong vòng 14 ngày tới tính từ ngày chạy script.
--   3. Tự động khởi tạo Toa xe khách (vantai.ToaXeKhach: AN, NML, BN) và Chỗ ngồi
--      (vantai.ChoNgoi: 134 chỗ/chuyến) sẵn sàng 100% mở bán vé.
--   4. Tự động lập Lịch dừng ga (vanhanh.LichDungGa) chi tiết qua các ga dọc tuyến Bắc - Nam.
--   5. Cung cấp Stored Procedure 'vantai.sp_LayDanhSachNgayCoChuyen' hỗ trợ giao diện hiển thị danh
--      sách các ngày có chuyến tàu khả dụng.
--
-- SỬA 15/11 (Đạt, khi đưa vào database\thu_tu_chay.txt):
--   - Bỏ "USE QuanLyDuongSatV2": tao_lai_csdl.cmd dựng CSDL tên khác (CSDL tạm) thì không ghi nhầm vào CSDL thật.
--   - KHÔNG tạo vanhanh.DoanTau: trước đây gán cùng 1 đầu máy cho mọi chuyến (chồng giờ, luật PT-08) và đặt sẵn
--     DaDuyetAnToan = 1 khi chưa khám xe. Đầu máy + toa do Phương tiện lập, duyệt an toàn do Bảo trì làm.
--     Bán vé chỉ cần ToaXeKhach + ChoNgoi nên vẫn mở bán bình thường.
--   - Bỏ đoạn xoá chuyến tương lai chưa có vé (gặp chuyến đã lập toa / có kíp / có biên bản thì lỗi khoá ngoại,
--     rollback cả file). Vẫn chạy lại được: chuyến đã có (cùng mác, cùng ngày) thì thủ tục bỏ qua.
--   - Sinh từ NGÀY MAI (chuyến hôm nay do seed khác tạo, đã có kíp lái); SET QUOTED_IDENTIFIER ON vì ToaXeKhach
--     có chỉ mục lọc (migration V20261113_1000) và thủ tục giữ thiết lập lúc tạo.
--
-- HƯỚNG DẪN DÀNH CHO DEVELOPER KHÁC:
--   - Mở file này trong SQL Server Management Studio (SSMS) hoặc Azure Data Studio.
--   - CHỌN DATABASE dự án trước khi chạy (mặc định QuanLyDuongSatV2) — file không tự USE.
--   - Bấm Execute (hoặc nhấn phím F5).
--   - Chạy lại nhiều lần không trùng dữ liệu, có bọc Transaction.
-- ==============================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT N'==============================================================================================';
PRINT N'BẮT ĐẦU CẬP NHẬT KẾ HOẠCH BÁN VÉ TRƯỚC (ADVANCE BOOKING) CHO CÁC NGÀY CUỐI TUẦN...';
PRINT N'==============================================================================================';

-- ----------------------------------------------------------------------------------------------
-- PHẦN 1: STORED PROCEDURE TỰ ĐỘNG LẬP LỊCH CHUYẾN TÀU THEO NGÀY
-- ----------------------------------------------------------------------------------------------
IF OBJECT_ID('vanhanh.sp_TuDongLapChuyenTauTheoNgay', 'P') IS NOT NULL
    DROP PROCEDURE vanhanh.sp_TuDongLapChuyenTauTheoNgay;
GO

CREATE PROCEDURE vanhanh.sp_TuDongLapChuyenTauTheoNgay
    @SoHieuMacTau VARCHAR(20),
    @NgayChay DATE,
    @MaChuyenTauMoi INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaChuyenTauMoi = 0;

    DECLARE @MaMacTau INT, @MaGaDi INT, @MaGaDen INT, @HuongChay VARCHAR(20);
    SELECT @MaMacTau = MaMacTau, @MaGaDi = MaGaDi, @MaGaDen = MaGaDen, @HuongChay = HuongChay
    FROM vanhanh.MacTauMau
    WHERE SoHieuMacTau = @SoHieuMacTau;

    IF @MaMacTau IS NULL
    BEGIN
        RAISERROR(N'Không tìm thấy mác tàu mẫu: %s', 16, 1, @SoHieuMacTau);
        RETURN;
    END

    -- Kiểm tra nếu chuyến tàu ngày đó đã tồn tại thì không tạo lại (Idempotent)
    SELECT @MaChuyenTauMoi = MaChuyenTau
    FROM vanhanh.ChuyenTau
    WHERE MaMacTau = @MaMacTau AND CAST(NgayXuatPhat AS DATE) = @NgayChay;

    IF @MaChuyenTauMoi > 0
    BEGIN
        RETURN; -- Đã có sẵn chuyến
    END

    -- Xác định giờ xuất phát và giờ về đích chuẩn theo mác tàu
    DECLARE @GioXuatPhatKH DATETIME2, @GioVeDichKH DATETIME2;
    IF @SoHieuMacTau = 'SE1'
    BEGIN
        SET @GioXuatPhatKH = DATEADD(HOUR, 6, CAST(@NgayChay AS DATETIME2));       -- 06:00
        SET @GioVeDichKH   = DATEADD(MINUTE, 2305, CAST(@NgayChay AS DATETIME2));   -- ~38.5 tiếng
    END
    ELSE IF @SoHieuMacTau = 'SE2'
    BEGIN
        SET @GioXuatPhatKH = DATEADD(HOUR, 19, CAST(@NgayChay AS DATETIME2));      -- 19:00
        SET @GioVeDichKH   = DATEADD(MINUTE, 3090, CAST(@NgayChay AS DATETIME2));   -- ~51.5 tiếng
    END
    ELSE IF @SoHieuMacTau = 'SE3'
    BEGIN
        SET @GioXuatPhatKH = DATEADD(MINUTE, 1140, CAST(@NgayChay AS DATETIME2));  -- 19:00
        SET @GioVeDichKH   = DATEADD(MINUTE, 3090, CAST(@NgayChay AS DATETIME2));   -- Đến SGO sau 32.5h
    END
    ELSE IF @SoHieuMacTau = 'SE4'
    BEGIN
        SET @GioXuatPhatKH = DATEADD(MINUTE, 1155, CAST(@NgayChay AS DATETIME2));  -- 19:15
        SET @GioVeDichKH   = DATEADD(MINUTE, 3090, CAST(@NgayChay AS DATETIME2));   -- Đến HNI
    END
    ELSE
    BEGIN
        SET @GioXuatPhatKH = DATEADD(HOUR, 8, CAST(@NgayChay AS DATETIME2));
        SET @GioVeDichKH   = DATEADD(HOUR, 36, CAST(@NgayChay AS DATETIME2));
    END

    -- 1. Thêm ChuyenTau
    INSERT INTO vanhanh.ChuyenTau (MaMacTau, NgayXuatPhat, GioXuatPhatKH, GioVeDichKH, TrangThai, SoPhutTreLuyKe)
    VALUES (@MaMacTau, @NgayChay, @GioXuatPhatKH, @GioVeDichKH, 'DA_LEN_LICH', 0);
    SET @MaChuyenTauMoi = SCOPE_IDENTITY();

    -- 2. (Sửa 15/11) Không tạo DoanTau ở đây: đầu máy + toa thật do màn hình Phương tiện lập (kiểm chồng giờ,
    --    tải trục, đường tránh), cờ duyệt an toàn do Bảo trì đặt sau khi khám xe. Chưa lập đoàn tàu thì
    --    kiểm tra xuất bến chặn chuyển "Sẵn sàng" — đúng nghiệp vụ; bán vé không cần DoanTau.

    -- 3. Thêm ToaXeKhach (3 Toa: Toa 1 AN 28 chỗ, Toa 2 NML 64 chỗ, Toa 3 BN 42 chỗ)
    DECLARE @ToaAN INT, @ToaNML INT, @ToaBN INT;

    INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua)
    VALUES (@MaChuyenTauMoi, N'Toa 1 (AN)', 'AN', 1, 28);
    SET @ToaAN = SCOPE_IDENTITY();

    INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua)
    VALUES (@MaChuyenTauMoi, N'Toa 2 (NML)', 'NML', 2, 64);
    SET @ToaNML = SCOPE_IDENTITY();

    INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua)
    VALUES (@MaChuyenTauMoi, N'Toa 3 (BN)', 'BN', 3, 42);
    SET @ToaBN = SCOPE_IDENTITY();

    -- 4. Sinh Chỗ Ngồi (ChoNgoi)
    -- 4.1 Toa 1 (AN - 28 chỗ)
    INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
    SELECT @ToaAN, n, ((n-1)%2)+1, 0
    FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),
                 (15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28)) v(n);

    -- 4.2 Toa 2 (NML - 64 chỗ)
    INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
    SELECT @ToaNML, n, NULL, 0
    FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),
                 (17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28),(29),(30),(31),(32),
                 (33),(34),(35),(36),(37),(38),(39),(40),(41),(42),(43),(44),(45),(46),(47),(48),
                 (49),(50),(51),(52),(53),(54),(55),(56),(57),(58),(59),(60),(61),(62),(63),(64)) v(n);

    -- 4.3 Toa 3 (BN - 42 chỗ)
    INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
    SELECT @ToaBN, n, ((n-1)%3)+1, 0
    FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),
                 (15),(16),(17),(18),(19),(20),(21),(22),(23),(24),(25),(26),(27),(28),
                 (29),(30),(31),(32),(33),(34),(35),(36),(37),(38),(39),(40),(41),(42)) v(n);

    -- 5. Lập Lịch Dừng Ga chi tiết cho TOÀN BỘ 25 GA KHÁCH DỌC TUYẾN BẮC - NAM
    -- (Bao gồm đầy đủ: Hà Nội, Phủ Lý, Nam Định, Ninh Bình, Bỉm Sơn, Thanh Hóa, Vinh, Đồng Hới, Huế, Đà Nẵng, Quảng Ngãi, Diêu Trì, Tuy Hòa, Nha Trang, Biên Hòa, Sài Gòn...)
    IF @HuongChay = 'BAC_NAM'
    BEGIN
        INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach)
        SELECT 
            @MaChuyenTauMoi,
            g.MaGa,
            ROW_NUMBER() OVER (ORDER BY g.LyTrinhKm ASC) AS ThuTuDung,
            DATEADD(MINUTE, CAST((g.LyTrinhKm / 1726.20) * 1950.0 AS INT), @GioXuatPhatKH) AS GioDenKeHoach,
            DATEADD(MINUTE, CAST((g.LyTrinhKm / 1726.20) * 1950.0 AS INT) + 
                CASE 
                    WHEN g.LyTrinhKm = 1726.20 THEN 0 -- Ga cuối cùng (Sài Gòn)
                    WHEN g.HangGa = 'HANG_1' AND g.LyTrinhKm <> 0.00 THEN 15 
                    ELSE 5 
                END, @GioXuatPhatKH) AS GioDiKeHoach
        FROM hatang.Ga g
        WHERE g.MaGaCode NOT IN ('GBA', 'STH') -- Loại bỏ 2 ga chuyên hàng Giáp Bát và Sóng Thần
        ORDER BY g.LyTrinhKm ASC;
    END
    ELSE -- NAM_BAC
    BEGIN
        INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach)
        SELECT 
            @MaChuyenTauMoi,
            g.MaGa,
            ROW_NUMBER() OVER (ORDER BY g.LyTrinhKm DESC) AS ThuTuDung,
            DATEADD(MINUTE, CAST(((1726.20 - g.LyTrinhKm) / 1726.20) * 1950.0 AS INT), @GioXuatPhatKH) AS GioDenKeHoach,
            DATEADD(MINUTE, CAST(((1726.20 - g.LyTrinhKm) / 1726.20) * 1950.0 AS INT) + 
                CASE 
                    WHEN g.LyTrinhKm = 0.00 THEN 0 -- Ga cuối cùng (Hà Nội)
                    WHEN g.HangGa = 'HANG_1' AND g.LyTrinhKm <> 1726.20 THEN 15 
                    ELSE 5 
                END, @GioXuatPhatKH) AS GioDiKeHoach
        FROM hatang.Ga g
        WHERE g.MaGaCode NOT IN ('GBA', 'STH')
        ORDER BY g.LyTrinhKm DESC;
    END
END;
GO

PRINT N'>> Đã tạo Stored Procedure vanhanh.sp_TuDongLapChuyenTauTheoNgay thành công!';
GO

-- ----------------------------------------------------------------------------------------------
-- PHẦN 2: STORED PROCEDURE TRA CỨU CÁC NGÀY CÓ CHUYẾN TÀU KHẢ DỤNG (CHO UI / DEV)
-- ----------------------------------------------------------------------------------------------
IF OBJECT_ID('vantai.sp_LayDanhSachNgayCoChuyen', 'P') IS NOT NULL
    DROP PROCEDURE vantai.sp_LayDanhSachNgayCoChuyen;
GO

CREATE PROCEDURE vantai.sp_LayDanhSachNgayCoChuyen
    @MaGaDi INT,
    @MaGaDen INT,
    @TuNgay DATE = NULL,
    @DenNgay DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @TuNgay IS NULL SET @TuNgay = CAST(GETDATE() AS DATE);
    IF @DenNgay IS NULL SET @DenNgay = DATEADD(DAY, 30, @TuNgay);

    SELECT 
        CAST(ldDi.GioDiKeHoach AS DATE) AS NgayDi,
        DATENAME(WEEKDAY, ldDi.GioDiKeHoach) AS ThuTrongTuan,
        COUNT(DISTINCT ct.MaChuyenTau) AS SoChuyenTau,
        STRING_AGG(mt.SoHieuMacTau, ', ') WITHIN GROUP (ORDER BY ldDi.GioDiKeHoach ASC) AS CacMacTau,
        MIN(ISNULL(vantai.fn_TinhGiaVe('NML', @MaGaDi, @MaGaDen, NULL, mt.LoaiTau), 150000)) AS GiaVeTu
    FROM vanhanh.ChuyenTau ct
    JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
    JOIN vanhanh.LichDungGa ldDi ON ct.MaChuyenTau = ldDi.MaChuyenTau AND ldDi.MaGa = @MaGaDi
    JOIN vanhanh.LichDungGa ldDen ON ct.MaChuyenTau = ldDen.MaChuyenTau AND ldDen.MaGa = @MaGaDen
    WHERE ct.TrangThai IN ('DA_LEN_LICH', 'SAN_SANG', 'DANG_CHAY', 'TRE_GIO')
      AND ldDi.ThuTuDung < ldDen.ThuTuDung
      AND CAST(ldDi.GioDiKeHoach AS DATE) BETWEEN @TuNgay AND @DenNgay
      AND NOT EXISTS (
          SELECT 1 FROM vanhanh.LichDungGa ldCheck 
          WHERE ldCheck.MaChuyenTau = ct.MaChuyenTau AND ldCheck.MaGa = @MaGaDi AND ldCheck.GioDiThucTe IS NOT NULL
      )
    GROUP BY CAST(ldDi.GioDiKeHoach AS DATE), DATENAME(WEEKDAY, ldDi.GioDiKeHoach)
    ORDER BY NgayDi ASC;
END;
GO

PRINT N'>> Đã tạo Stored Procedure vantai.sp_LayDanhSachNgayCoChuyen thành công!';
GO

-- ----------------------------------------------------------------------------------------------
-- PHẦN 3: KÍCH HOẠT DỮ LIỆU CHUYẾN TÀU CHO CÁC NGÀY TRONG TUẦN & CUỐI TUẦN (14 NGÀY TỚI)
-- ----------------------------------------------------------------------------------------------
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @NgayHienTai DATE = CAST(GETDATE() AS DATE);
    DECLARE @i INT = 1;   -- (Sửa 15/11) từ ngày mai; chuyến hôm nay do seed khác tạo (đã có kíp lái)
    DECLARE @NgayLoop DATE;
    DECLARE @MaChuyen INT;
    DECLARE @DayOfWeekName NVARCHAR(20);

    -- (Sửa 15/11) Bỏ đoạn xoá chuyến tương lai chưa có vé: xoá DoanTau khi còn ChiTietDoanTau / kíp lái / biên bản
    -- khám xe trỏ tới thì lỗi khoá ngoại và rollback cả file. Thủ tục đã tự bỏ qua chuyến có sẵn (cùng mác, cùng ngày).

    PRINT N'>> Đang tự động quét và sinh chuyến tàu cho 14 ngày tới (từ ' + CONVERT(NVARCHAR(10), DATEADD(DAY, 1, @NgayHienTai), 103) + N')...';

    WHILE @i <= 14
    BEGIN
        SET @NgayLoop = DATEADD(DAY, @i, @NgayHienTai);
        SET @DayOfWeekName = DATENAME(WEEKDAY, @NgayLoop);

        -- Mỗi ngày chạy ít nhất 1 cặp tàu nhanh SE1 (Bắc - Nam) và SE2 (Nam - Bắc)
        EXEC vanhanh.sp_TuDongLapChuyenTauTheoNgay @SoHieuMacTau = 'SE1', @NgayChay = @NgayLoop, @MaChuyenTauMoi = @MaChuyen OUTPUT;
        EXEC vanhanh.sp_TuDongLapChuyenTauTheoNgay @SoHieuMacTau = 'SE2', @NgayChay = @NgayLoop, @MaChuyenTauMoi = @MaChuyen OUTPUT;

        -- Riêng các ngày Cuối Tuần (Thứ Sáu, Thứ Bảy, Chủ Nhật) tăng cường thêm cặp tàu SE3 và SE4
        IF DATEPART(WEEKDAY, @NgayLoop) IN (1, 6, 7) -- 1: Chủ Nhật, 6: Thứ Sáu, 7: Thứ Bảy (theo chuẩn SQL US_ENGLISH)
        BEGIN
            EXEC vanhanh.sp_TuDongLapChuyenTauTheoNgay @SoHieuMacTau = 'SE3', @NgayChay = @NgayLoop, @MaChuyenTauMoi = @MaChuyen OUTPUT;
            EXEC vanhanh.sp_TuDongLapChuyenTauTheoNgay @SoHieuMacTau = 'SE4', @NgayChay = @NgayLoop, @MaChuyenTauMoi = @MaChuyen OUTPUT;
        END

        SET @i = @i + 1;
    END

    COMMIT TRANSACTION;
    PRINT N'==============================================================================================';
    PRINT N'HOÀN TẤT THÀNH CÔNG! ĐÃ SINH ĐỦ LỊCH CHẠY TÀU & VÉ MỞ BÁN CHO TOÀN BỘ 14 NGÀY TỚI!';
    PRINT N'BÂY GIỜ BẠN CÓ THỂ MỞ MÀN HÌNH BÁN VÉ, CHỌN BẤT KỲ NGÀY CUỐI TUẦN NÀO ĐỂ MUA VÉ TRƯỚC.';
    PRINT N'==============================================================================================';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'Lỗi khi sinh dữ liệu bán vé trước: %s', 16, 1, @ErrMsg);
END CATCH;
GO

-- ----------------------------------------------------------------------------------------------
-- PHẦN 4: TRUY VẤN KIỂM CHỨNG NHANH (VERIFICATION QUERY)
-- ----------------------------------------------------------------------------------------------
PRINT N'';
PRINT N'DANH SÁCH CÁC CHUYẾN TÀU ĐÃ LÊN LỊCH SẴN SÀNG BÁN VÉ TRONG 14 NGÀY TỚI:';
SELECT 
    ct.MaChuyenTau,
    mt.SoHieuMacTau,
    mt.LoaiTau,
    gDi.TenGa AS GaXuatPhat,
    gDen.TenGa AS GaDich,
    CONVERT(VARCHAR(10), ct.NgayXuatPhat, 103) AS NgayXuatPhat,
    DATENAME(WEEKDAY, ct.NgayXuatPhat) AS ThuTrongTuan,
    CONVERT(VARCHAR(5), ct.GioXuatPhatKH, 108) AS GioDi,
    ct.TrangThai,
    (SELECT COUNT(*) FROM vantai.ToaXeKhach tx WHERE tx.MaChuyenTau = ct.MaChuyenTau) AS SoToaXe,
    (SELECT COUNT(*) FROM vantai.ChoNgoi cn JOIN vantai.ToaXeKhach tx2 ON cn.MaToaXeKhach = tx2.MaToaXeKhach WHERE tx2.MaChuyenTau = ct.MaChuyenTau) AS TongSoChoTrong
FROM vanhanh.ChuyenTau ct
JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
JOIN hatang.Ga gDi ON mt.MaGaDi = gDi.MaGa
JOIN hatang.Ga gDen ON mt.MaGaDen = gDen.MaGa
WHERE ct.TrangThai = 'DA_LEN_LICH'
ORDER BY ct.NgayXuatPhat ASC, ct.GioXuatPhatKH ASC;
GO

GO
INSERT #TronGoi (Buoc) VALUES (16);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
PRINT N'--- [17/17] tests\T20261009_0840__dat__kiem-tra-du-lieu-sprint1.sql ---';
GO
/* =========================================================================
   KIỂM TRA DỮ LIỆU PHƯƠNG TIỆN + NHÂN SỰ THEO LUẬT NGHIỆP VỤ (Sprint 1)
   Tác giả: Đạt · 09/10/2026
   Chỉ ĐỌC (bảng tạm #), không sửa dữ liệu. Chạy lại được bất cứ lúc nào, kể cả sau khi dùng app.
   Lệnh: sqlcmd -S . -E -I -f 65001 -d QuanLyDuongSatV2 -i <file>

   Kết quả: bảng tổng hợp theo luật + danh sách chi tiết.
     LOI     = vi phạm luật nghiệp vụ trong dữ liệu của Đạt (Phương tiện / Nhân sự) → có thì báo lỗi cuối file.
     GHI_CHU = điểm lệch trong dữ liệu của phân hệ khác (A Bá) → chỉ liệt kê để báo, không tính là lỗi.

   Luật lấy từ giao diện của Đạt (LapTauKeoThaDialog, DanhGiaKipLai) và PHAN_TICH_NHAN_SU_KIP_LAI.md (R1–R13):
   PT-01..PT-12 phương tiện / lập tàu (PT-12: toa bán vé, 13/11) · NS-01..NS-13 nhân sự / kíp lái · AB-* ghi chú dữ liệu A Bá.
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

-- PT-12 (13/11) Toa bán vé khớp đoàn tàu (chuyến còn hiệu lực đã lập toa): toa khách thứ k ↔ "Toa k" đang bán
--       cùng loại, đúng toa thật, đúng phạm vi ga; không thừa toa bán vé; vé còn hiệu lực không nằm trên toa đã ngừng bán
--       và nằm trong phạm vi chạy của toa. Chỉ kiểm khi CSDL đã chạy migration V20261113_1000 (có cột NgungBan).
IF OBJECT_ID('tempdb..#PT12') IS NOT NULL DROP TABLE #PT12;
CREATE TABLE #PT12 (ChiTiet NVARCHAR(400));
IF COL_LENGTH(N'vantai.ToaXeKhach', N'NgungBan') IS NOT NULL
    EXEC (N'
    ;WITH K AS (
        SELECT c.MaChuyenTau, c.Mac, ct.MaToaXe, tx.SoHieuToaXe, cl.MaChungLoaiCode AS Loai, ct.MaGaNoi, ct.MaGaCat,
               ROW_NUMBER() OVER (PARTITION BY c.MaChuyenTau ORDER BY ct.ThuTuNoi) AS k
        FROM #Chuyen c
        JOIN vanhanh.ChiTietDoanTau ct ON ct.MaDoanTau = c.MaDoanTau
        JOIN phuongtien.ToaXe tx ON tx.MaToaXe = ct.MaToaXe
        JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
        WHERE c.ConHieuLuc = 1 AND c.LoaiTau <> ''TAU_HANG'' AND cl.MaChungLoaiCode IN (''NC'', ''NML'', ''BN'', ''AN''))
    INSERT INTO #PT12
    SELECT CONCAT(K.Mac, N'': toa khách thứ '', K.k, N'' ('', K.SoHieuToaXe, N'') '',
                  CASE WHEN b.MaToaXeKhach IS NULL THEN N''chưa có toa bán vé''
                       WHEN b.LoaiToa <> K.Loai THEN CONCAT(N''nhưng toa bán vé là loại '', b.LoaiToa)
                       WHEN ISNULL(b.MaToaXe, 0) <> K.MaToaXe THEN N''nhưng toa bán vé trỏ toa thật khác''
                       ELSE N''nhưng phạm vi ga của toa bán vé lệch đoàn tàu'' END)
    FROM K
    LEFT JOIN vantai.ToaXeKhach b ON b.MaChuyenTau = K.MaChuyenTau AND b.ThuTuToa = K.k AND b.NgungBan = 0
    WHERE b.MaToaXeKhach IS NULL OR b.LoaiToa <> K.Loai OR ISNULL(b.MaToaXe, 0) <> K.MaToaXe
       OR ISNULL(b.MaGaNoi, 0) <> ISNULL(K.MaGaNoi, 0) OR ISNULL(b.MaGaCat, 0) <> ISNULL(K.MaGaCat, 0)
    UNION ALL
    SELECT CONCAT(c.Mac, N'': toa bán vé '', b.ThuTuToa, N'' đang bán nhưng đoàn tàu chỉ có '',
                  (SELECT COUNT(*) FROM K WHERE K.MaChuyenTau = c.MaChuyenTau), N'' toa khách'')
    FROM #Chuyen c
    JOIN vantai.ToaXeKhach b ON b.MaChuyenTau = c.MaChuyenTau AND b.NgungBan = 0
    WHERE c.ConHieuLuc = 1
      AND EXISTS (SELECT 1 FROM vanhanh.ChiTietDoanTau x WHERE x.MaDoanTau = c.MaDoanTau)
      AND b.ThuTuToa > (SELECT COUNT(*) FROM K WHERE K.MaChuyenTau = c.MaChuyenTau)
    UNION ALL
    SELECT CONCAT(c.Mac, N'': vé '', v.MaVeCode, N'' còn hiệu lực ở '', b.NhanHieuToa,
                  CASE WHEN b.NgungBan = 1 THEN N'' đã ngừng bán'' ELSE N'' nhưng ngoài phạm vi chạy của toa'' END)
    FROM #Chuyen c
    JOIN vantai.ToaXeKhach b ON b.MaChuyenTau = c.MaChuyenTau
    JOIN vantai.ChoNgoi cn ON cn.MaToaXeKhach = b.MaToaXeKhach
    JOIN vantai.Ve v ON v.MaChoNgoi = cn.MaChoNgoi AND v.TrangThai IN (''DA_DAT'', ''DA_LEN_TAU'')
    WHERE c.ConHieuLuc = 1
      AND (b.NgungBan = 1 OR vantai.fn_ToaChayQuaChang(b.MaToaXeKhach, v.MaGaDi, v.MaGaDen) = 0);');
INSERT INTO @KetQua SELECT 'LOI', 'PT-12', N'Toa bán vé khớp đoàn tàu, vé trong phạm vi toa', ChiTiet FROM #PT12;

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
    ('PT-12', N'Toa bán vé khớp đoàn tàu, vé trong phạm vi toa'),
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
IF @SoLoi > 0 INSERT #TronGoiViPham (SoLoi) VALUES (@SoLoi);   -- [tron goi]

GO
INSERT #TronGoi (Buoc) VALUES (17);
GO
IF DB_ID(N'QuanLyDuongSatV2') IS NULL
BEGIN
    RAISERROR(N'Không có CSDL QuanLyDuongSatV2 — dừng (xem lỗi ở bước 1).', 16, 1);
    SET NOEXEC ON;
END
GO
USE QuanLyDuongSatV2;
GO
GO
SET NOEXEC OFF;
GO
USE master;
GO
PRINT N'';
DECLARE @Buoc TABLE (Buoc INT PRIMARY KEY, Ten NVARCHAR(300));
INSERT @Buoc (Buoc, Ten) VALUES
(1, N'000_tao_csdl_goc.sql'),
(2, N'migrations\V20261009_0820__dat__chitietdoantau-ga-noi-cat.sql'),
(3, N'seed\S20261009_0830__dat__phuong-tien-nhan-su-sprint1.sql'),
(4, N'migrations\V20261011_1500__dat__nhansu-phienban-trigger-truongtau.sql'),
(5, N'migrations\V20261012_1100__dat__nhansu-lichsu-ma-nhanvien.sql'),
(6, N'migrations\V20261014_0900__dat__phuongtien-ngung-khai-thac.sql'),
(7, N'migrations\V20261014_1000__dat__nhansu-ngay-sinh.sql'),
(8, N'migrations\V20261024_1000__dat__baotri-chiphi-dinhmuc-giovn.sql'),
(9, N'seed\S20261024_1100__dat__baotri-sprint2.sql'),
(10, N'seed\S20261026_1000__dat__ve-mau-doanh-thu.sql'),
(11, N'migrations\V20261108_1000__dat__phanquyen-dangnhap-quyen.sql'),
(12, N'seed\S20261108_1100__dat__tai-khoan-demo.sql'),
(13, N'seed\S20261108_1200__dat__lich-su-bao-cao.sql'),
(14, N'migrations\V20261108_1300__dat__baocao-views.sql'),
(15, N'migrations\V20261113_1000__dat__toabanve-dongbo-doantau.sql'),
(16, N'seed\S20261113_1200__dat__kich-hoat-ban-ve-truoc-cuoi-tuan.sql'),
(17, N'tests\T20261009_0840__dat__kiem-tra-du-lieu-sprint1.sql');
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