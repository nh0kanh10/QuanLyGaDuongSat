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

DECLARE @NgayNay DATE = CAST(SYSUTCDATETIME() AS DATE);
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
('NV_006', N'Vũ Tiến Đạt',      '0906789012', N'Lái tàu', 'D19E', DATEADD(DAY, 15, CAST(SYSUTCDATETIME() AS DATE)), N'Đội lái tàu Hà Nội',   'SAN_SANG'),
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
