using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using DTO.BaoTri;

namespace DAL.Repositories
{
    // =========================================================================
    // BAO TRI: baotri.KhamXeKyThuat, NhatKyCapNhienLieu, NhatKyBaoDuong; co vanhanh.DoanTau.DaDuyetAnToan
    // (chu bang: Dat).
    //
    // LayDuLieu : anh chup cho ca trang Bao tri (5 tab + 3 dialog) va cho BaoTriService chay lai luat
    //             trong giao dich truoc khi ghi. Doc chi doc vanhanh.ChuyenTau / MacTauMau (A Ba),
    //             phuongtien.* + nhansu.NhanVien + phanquyen.* (ten / vai tro nguoi ky phieu).
    //             Lich su phieu doc TOAN bang (du lieu do an nho); doan tau doc moi doan.
    // Ghi       : chi ghi, khong kiem nghiep vu (BUS da kiem trong cung giao dich).
    //             Phieu cap dau / bao duong sua theo PhienBan (ROWVERSION, migration V20261024_1000):
    //             tra ve so dong, 0 = nguoi khac vua sua.
    // Thoi gian : gio Viet Nam - ghi bang SYSDATETIME() cua may chu (quyet dinh 24/10, nhu Nhan su).
    //             kiemtoan.NhatKyHeThong ghi gio UTC (thu tuc cua A Ba) -> doi sang gio may chu khi doc.
    // =========================================================================
    public class BaoTriRepository
    {
        public DuLieuBaoTri LayDuLieu(PhienGiaoDich? gd = null)
        {
            DateTime bayGio = LayGioMayChu(gd);

            var doanTau = PhienGiaoDich.TruyVan(gd, @"
                SELECT dt.MaDoanTau, dt.MaChuyenTau, mt.SoHieuMacTau, mt.LoaiTau, ct.NgayXuatPhat, ct.GioXuatPhatKH,
                       ct.GioVeDichKH, gdi.TenGa AS TenGaDi, gden.TenGa AS TenGaDen,
                       dt.MaDauMayChinh, dmc.SoHieuDauMay AS SoHieuDauMayChinh,
                       dt.MaDauMayDay, dmd.SoHieuDauMay AS SoHieuDauMayDay,
                       dt.TongSoToa, dt.TongChieuDaiM, dt.TongTrongLuongTan, dt.DaDuyetAnToan, ct.TrangThai, dt.PhienBan,
                       DATEADD(MINUTE, DATEDIFF(MINUTE, SYSUTCDATETIME(), SYSDATETIME()), nk.ThoiDiem) AS ThoiDiemSuaDoanTau
                FROM vanhanh.DoanTau dt
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                JOIN hatang.Ga gdi ON gdi.MaGa = mt.MaGaDi
                JOIN hatang.Ga gden ON gden.MaGa = mt.MaGaDen
                JOIN phuongtien.DauMay dmc ON dmc.MaDauMay = dt.MaDauMayChinh
                LEFT JOIN phuongtien.DauMay dmd ON dmd.MaDauMay = dt.MaDauMayDay
                OUTER APPLY (SELECT MAX(n.ThoiDiem) AS ThoiDiem
                             FROM kiemtoan.NhatKyHeThong n
                             WHERE n.TenBang = 'vanhanh.DoanTau' AND n.HanhDong IN ('LAP_DOAN_TAU', 'SUA_DOAN_TAU')
                               AND n.KhoaChinhRecord = CAST(dt.MaDoanTau AS VARCHAR(50))) nk
                ORDER BY ct.GioXuatPhatKH, dt.MaDoanTau")
                .Rows.Cast<DataRow>().Select(r => new DoanTauBaoTriDTO
                {
                    MaDoanTau = Convert.ToInt32(r["MaDoanTau"]),
                    MaChuyenTau = Convert.ToInt32(r["MaChuyenTau"]),
                    SoHieuMacTau = Chuoi(r, "SoHieuMacTau"),
                    LoaiTau = Chuoi(r, "LoaiTau"),
                    NgayXuatPhat = Convert.ToDateTime(r["NgayXuatPhat"]),
                    GioXuatPhatKH = Convert.ToDateTime(r["GioXuatPhatKH"]),
                    GioVeDichKH = Convert.ToDateTime(r["GioVeDichKH"]),
                    TenGaDi = Chuoi(r, "TenGaDi"),
                    TenGaDen = Chuoi(r, "TenGaDen"),
                    MaDauMayChinh = Convert.ToInt32(r["MaDauMayChinh"]),
                    SoHieuDauMayChinh = Chuoi(r, "SoHieuDauMayChinh"),
                    MaDauMayDay = r["MaDauMayDay"] == DBNull.Value ? null : Convert.ToInt32(r["MaDauMayDay"]),
                    SoHieuDauMayDay = r["SoHieuDauMayDay"] == DBNull.Value ? null : Chuoi(r, "SoHieuDauMayDay"),
                    TongSoToa = Convert.ToInt32(r["TongSoToa"]),
                    TongChieuDaiM = Convert.ToDecimal(r["TongChieuDaiM"]),
                    TongTrongLuongTan = Convert.ToDecimal(r["TongTrongLuongTan"]),
                    DaDuyetAnToan = Convert.ToBoolean(r["DaDuyetAnToan"]),
                    TrangThaiChuyen = Chuoi(r, "TrangThai"),
                    PhienBan = (byte[])r["PhienBan"],
                    ThoiDiemSuaDoanTau = r["ThoiDiemSuaDoanTau"] == DBNull.Value ? null : Convert.ToDateTime(r["ThoiDiemSuaDoanTau"])
                }).ToList();

            var dauMay = PhienGiaoDich.TruyVan(gd, @"
                SELECT dm.MaDauMay, dm.SoHieuDauMay, ddm.MaDongCode, dm.DonViQuanLy, dm.TrangThai, dm.SoKmTichLuy,
                       ddm.DungTichBonDauLit, ddm.DinhMucSFC, ddm.CongSuatHP
                FROM phuongtien.DauMay dm
                JOIN phuongtien.DongDauMay ddm ON ddm.MaDongDauMay = dm.MaDongDauMay
                ORDER BY dm.SoHieuDauMay")
                .Rows.Cast<DataRow>().Select(r => new DauMayBaoTriDTO
                {
                    MaDauMay = Convert.ToInt32(r["MaDauMay"]),
                    SoHieuDauMay = Chuoi(r, "SoHieuDauMay"),
                    MaDongCode = Chuoi(r, "MaDongCode"),
                    DonViQuanLy = Chuoi(r, "DonViQuanLy"),
                    TrangThai = Chuoi(r, "TrangThai"),
                    SoKmTichLuy = Convert.ToDecimal(r["SoKmTichLuy"]),
                    DungTichBonDauLit = Convert.ToInt32(r["DungTichBonDauLit"]),
                    DinhMucSFC = Convert.ToDecimal(r["DinhMucSFC"]),
                    CongSuatHP = Convert.ToInt32(r["CongSuatHP"])
                }).ToList();

            var toaXe = PhienGiaoDich.TruyVan(gd, @"
                SELECT tx.MaToaXe, tx.SoHieuToaXe, cl.MaChungLoaiCode, cl.TenMoTa, tx.TrangThai,
                       CAST(CASE WHEN EXISTS (SELECT 1
                                              FROM vanhanh.ChiTietDoanTau c
                                              JOIN vanhanh.DoanTau d ON d.MaDoanTau = c.MaDoanTau
                                              JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = d.MaChuyenTau
                                              WHERE c.MaToaXe = tx.MaToaXe AND ct.TrangThai IN ('DANG_CHAY', 'TRE_GIO'))
                                 THEN 1 ELSE 0 END AS BIT) AS DangChay
                FROM phuongtien.ToaXe tx
                JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
                ORDER BY tx.SoHieuToaXe")
                .Rows.Cast<DataRow>().Select(r => new ToaXeBaoTriDTO
                {
                    MaToaXe = Convert.ToInt32(r["MaToaXe"]),
                    SoHieuToaXe = Chuoi(r, "SoHieuToaXe"),
                    MaChungLoaiCode = Chuoi(r, "MaChungLoaiCode"),
                    TenChungLoai = Chuoi(r, "TenMoTa"),
                    TrangThai = Chuoi(r, "TrangThai"),
                    DangChay = Convert.ToBoolean(r["DangChay"])
                }).ToList();

            // Nguoi ky phieu: tai khoan co vai tro KY_THUAT + moi tai khoan da ky phieu cu (de hien ten)
            var nguoi = PhienGiaoDich.TruyVan(gd, @"
                SELECT tk.MaTaiKhoan, tk.TenDangNhap, tk.HoTenHienThi, tk.DangHoatDong,
                       ISNULL(nv.DonViChuQuan, N'') AS DonVi,
                       CAST(CASE WHEN EXISTS (SELECT 1 FROM phanquyen.TaiKhoan_VaiTro tv
                                              JOIN phanquyen.VaiTro vt ON vt.MaVaiTro = tv.MaVaiTro
                                              WHERE tv.MaTaiKhoan = tk.MaTaiKhoan AND vt.MaVaiTroCode = 'KY_THUAT')
                                 THEN 1 ELSE 0 END AS BIT) AS LaKyThuat
                FROM phanquyen.TaiKhoan tk
                LEFT JOIN nhansu.NhanVien nv ON nv.MaNhanVien = tk.MaNhanVien
                WHERE EXISTS (SELECT 1 FROM phanquyen.TaiKhoan_VaiTro tv
                              JOIN phanquyen.VaiTro vt ON vt.MaVaiTro = tv.MaVaiTro
                              WHERE tv.MaTaiKhoan = tk.MaTaiKhoan AND vt.MaVaiTroCode = 'KY_THUAT')
                   OR tk.MaTaiKhoan IN (SELECT MaNguoiKham FROM baotri.KhamXeKyThuat
                                        UNION SELECT MaNguoiCap FROM baotri.NhatKyCapNhienLieu WHERE MaNguoiCap IS NOT NULL
                                        UNION SELECT MaNguoiThucHien FROM baotri.NhatKyBaoDuong WHERE MaNguoiThucHien IS NOT NULL)
                ORDER BY tk.HoTenHienThi, tk.MaTaiKhoan")
                .Rows.Cast<DataRow>().Select(r => new TaiKhoanKyThuatDTO
                {
                    MaTaiKhoan = Convert.ToInt32(r["MaTaiKhoan"]),
                    TenDangNhap = Chuoi(r, "TenDangNhap"),
                    HoTenHienThi = Chuoi(r, "HoTenHienThi"),
                    DonVi = Chuoi(r, "DonVi"),
                    DangHoatDong = Convert.ToBoolean(r["DangHoatDong"]),
                    LaKyThuat = Convert.ToBoolean(r["LaKyThuat"])
                }).ToList();

            var khamXe = PhienGiaoDich.TruyVan(gd, @"
                SELECT MaKhamXe, MaDoanTau, MaNguoiKham, ApLucHamBar, DoSutApBarPhut, DaCapNuoc, DaXaVeSinh,
                       GhiChuKyThuat, ThoiDiemKham, ChiPhi
                FROM baotri.KhamXeKyThuat")
                .Rows.Cast<DataRow>().Select(r => new KhamXeDTO
                {
                    MaKhamXe = Convert.ToInt32(r["MaKhamXe"]),
                    MaDoanTau = Convert.ToInt32(r["MaDoanTau"]),
                    MaNguoiKham = Convert.ToInt32(r["MaNguoiKham"]),
                    ApLucHamBar = Convert.ToDecimal(r["ApLucHamBar"]),
                    DoSutApBarPhut = Convert.ToDecimal(r["DoSutApBarPhut"]),
                    DaCapNuoc = Convert.ToBoolean(r["DaCapNuoc"]),
                    DaXaVeSinh = Convert.ToBoolean(r["DaXaVeSinh"]),
                    GhiChuKyThuat = Chuoi(r, "GhiChuKyThuat"),
                    ThoiDiemKham = Convert.ToDateTime(r["ThoiDiemKham"]),
                    ChiPhi = Convert.ToDecimal(r["ChiPhi"])
                }).ToList();

            var capDau = PhienGiaoDich.TruyVan(gd, @"
                SELECT MaNhatKyDau, MaDauMay, NoiCapDau, SoLitTraNap, TrongLuongKeoTan, CuLyChayKm, SuatTieuHaoSFC,
                       CanhBaoVuotMuc, MaNguoiCap, ThoiDiemBomDau, DonGiaVndLit, ChiPhi, PhienBan
                FROM baotri.NhatKyCapNhienLieu")
                .Rows.Cast<DataRow>().Select(r => new CapNhienLieuDTO
                {
                    MaNhatKyDau = Convert.ToInt32(r["MaNhatKyDau"]),
                    MaDauMay = Convert.ToInt32(r["MaDauMay"]),
                    NoiCapDau = Chuoi(r, "NoiCapDau"),
                    SoLitTraNap = Convert.ToDecimal(r["SoLitTraNap"]),
                    TrongLuongKeoTan = Convert.ToDecimal(r["TrongLuongKeoTan"]),
                    CuLyChayKm = Convert.ToDecimal(r["CuLyChayKm"]),
                    SuatTieuHaoSFC = Convert.ToDecimal(r["SuatTieuHaoSFC"]),
                    CanhBaoVuotMuc = Convert.ToBoolean(r["CanhBaoVuotMuc"]),
                    MaNguoiCap = r["MaNguoiCap"] == DBNull.Value ? null : Convert.ToInt32(r["MaNguoiCap"]),
                    ThoiDiemBomDau = Convert.ToDateTime(r["ThoiDiemBomDau"]),
                    DonGiaVndLit = Convert.ToDecimal(r["DonGiaVndLit"]),
                    ChiPhi = Convert.ToDecimal(r["ChiPhi"]),
                    PhienBan = (byte[])r["PhienBan"]
                }).ToList();

            var baoDuong = PhienGiaoDich.TruyVan(gd, @"
                SELECT MaBaoDuong, LoaiPhuongTien, MaDauMay, MaToaXe, CapBaoDuong, SoKmTaiThoiDiem, GhiChuKyThuat,
                       MaNguoiThucHien, ThoiDiemHoanThanh, ChiPhi, PhienBan
                FROM baotri.NhatKyBaoDuong")
                .Rows.Cast<DataRow>().Select(r => new BaoDuongDTO
                {
                    MaBaoDuong = Convert.ToInt32(r["MaBaoDuong"]),
                    LoaiPhuongTien = Chuoi(r, "LoaiPhuongTien"),
                    MaDauMay = r["MaDauMay"] == DBNull.Value ? null : Convert.ToInt32(r["MaDauMay"]),
                    MaToaXe = r["MaToaXe"] == DBNull.Value ? null : Convert.ToInt32(r["MaToaXe"]),
                    CapBaoDuong = Chuoi(r, "CapBaoDuong"),
                    SoKmTaiThoiDiem = Convert.ToDecimal(r["SoKmTaiThoiDiem"]),
                    GhiChuKyThuat = Chuoi(r, "GhiChuKyThuat"),
                    MaNguoiThucHien = r["MaNguoiThucHien"] == DBNull.Value ? null : Convert.ToInt32(r["MaNguoiThucHien"]),
                    ThoiDiemHoanThanh = Convert.ToDateTime(r["ThoiDiemHoanThanh"]),
                    ChiPhi = Convert.ToDecimal(r["ChiPhi"]),
                    PhienBan = (byte[])r["PhienBan"]
                }).ToList();

            var noiCap = PhienGiaoDich.TruyVan(gd, @"
                SELECT NoiCapDau FROM baotri.NhatKyCapNhienLieu GROUP BY NoiCapDau ORDER BY COUNT(*) DESC, NoiCapDau")
                .Rows.Cast<DataRow>().Select(r => Chuoi(r, "NoiCapDau")).ToList();

            return new DuLieuBaoTri
            {
                DoanTau = doanTau,
                DauMay = dauMay,
                ToaXe = toaXe,
                NguoiKyThuat = nguoi,
                KhamXe = khamXe,
                CapDau = capDau,
                BaoDuong = baoDuong,
                NoiCapDau = noiCap,
                BayGio = bayGio
            };
        }

        public DateTime LayGioMayChu(PhienGiaoDich? gd = null)
            => Convert.ToDateTime(PhienGiaoDich.LayGiaTri(gd, "SELECT SYSDATETIME()"));

        // =====================================================================
        // KHAM XE + DUYET AN TOAN
        // =====================================================================

        // Bang khong co trigger nen dung OUTPUT INSERTED duoc
        public (int MaKhamXe, DateTime ThoiDiem) ThemKhamXe(KhamXeDTO bb, PhienGiaoDich gd)
        {
            var r = PhienGiaoDich.TruyVan(gd, @"
                INSERT INTO baotri.KhamXeKyThuat (MaDoanTau, MaNguoiKham, ApLucHamBar, DoSutApBarPhut, DaCapNuoc, DaXaVeSinh,
                                                  GhiChuKyThuat, ThoiDiemKham, ChiPhi)
                OUTPUT INSERTED.MaKhamXe, INSERTED.ThoiDiemKham
                VALUES (@dt, @nguoi, @apLuc, @sutAp, @nuoc, @veSinh, @ghiChu, SYSDATETIME(), @chiPhi)",
                new SqlParameter("@dt", bb.MaDoanTau),
                new SqlParameter("@nguoi", bb.MaNguoiKham),
                new SqlParameter("@apLuc", SqlDbType.Decimal) { Precision = 3, Scale = 1, Value = bb.ApLucHamBar },
                new SqlParameter("@sutAp", SqlDbType.Decimal) { Precision = 3, Scale = 2, Value = bb.DoSutApBarPhut },
                new SqlParameter("@nuoc", bb.DaCapNuoc),
                new SqlParameter("@veSinh", bb.DaXaVeSinh),
                new SqlParameter("@ghiChu", SqlDbType.NVarChar, -1) { Value = string.IsNullOrWhiteSpace(bb.GhiChuKyThuat) ? DBNull.Value : (object)bb.GhiChuKyThuat },
                new SqlParameter("@chiPhi", SqlDbType.Decimal) { Precision = 14, Scale = 0, Value = bb.ChiPhi }).Rows[0];
            return (Convert.ToInt32(r["MaKhamXe"]), Convert.ToDateTime(r["ThoiDiemKham"]));
        }

        // Duyet dung phien ban doan tau da xem. 0 dong = doan tau vua bi sua / da duyet roi.
        public int DuyetAnToan(int maDoanTau, byte[] phienBan, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                UPDATE vanhanh.DoanTau SET DaDuyetAnToan = 1
                WHERE MaDoanTau = @id AND PhienBan = @pb AND DaDuyetAnToan = 0",
                new SqlParameter("@id", maDoanTau),
                new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });

        // Bien ban khong dat -> bo co duyet (trong cung giao dich lap bien ban)
        public int BoDuyetAnToan(int maDoanTau, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd,
                "UPDATE vanhanh.DoanTau SET DaDuyetAnToan = 0 WHERE MaDoanTau = @id AND DaDuyetAnToan = 1",
                new SqlParameter("@id", maDoanTau));

        // =====================================================================
        // NHIEN LIEU
        // =====================================================================

        public (int Ma, DateTime ThoiDiem, decimal ChiPhi) ThemCapDau(CapNhienLieuDTO nl, PhienGiaoDich gd)
        {
            var r = PhienGiaoDich.TruyVan(gd, @"
                INSERT INTO baotri.NhatKyCapNhienLieu (MaDauMay, NoiCapDau, SoLitTraNap, TrongLuongKeoTan, CuLyChayKm,
                                                       SuatTieuHaoSFC, CanhBaoVuotMuc, MaNguoiCap, ThoiDiemBomDau, DonGiaVndLit)
                OUTPUT INSERTED.MaNhatKyDau, INSERTED.ThoiDiemBomDau, INSERTED.ChiPhi
                VALUES (@dm, @noi, @lit, @tan, @km, @sfc, @vuot, @nguoi, SYSDATETIME(), @gia)",
                ThamSoCapDau(nl)).Rows[0];
            return (Convert.ToInt32(r["MaNhatKyDau"]), Convert.ToDateTime(r["ThoiDiemBomDau"]), Convert.ToDecimal(r["ChiPhi"]));
        }

        // Giu dau may + thoi diem bom goc (thong tin goc cua phieu)
        public int SuaCapDau(CapNhienLieuDTO nl, byte[] phienBan, PhienGiaoDich gd)
        {
            var thamSo = ThamSoCapDau(nl).ToList();
            thamSo.Add(new SqlParameter("@id", nl.MaNhatKyDau));
            thamSo.Add(new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });
            return PhienGiaoDich.ThucThi(gd, @"
                UPDATE baotri.NhatKyCapNhienLieu
                SET NoiCapDau = @noi, SoLitTraNap = @lit, TrongLuongKeoTan = @tan, CuLyChayKm = @km,
                    SuatTieuHaoSFC = @sfc, CanhBaoVuotMuc = @vuot, MaNguoiCap = @nguoi, DonGiaVndLit = @gia
                WHERE MaNhatKyDau = @id AND PhienBan = @pb AND MaDauMay = @dm",
                thamSo.ToArray());
        }

        // =====================================================================
        // BAO DUONG
        // =====================================================================

        public (int Ma, DateTime ThoiDiem) ThemBaoDuong(BaoDuongDTO bd, PhienGiaoDich gd)
        {
            var r = PhienGiaoDich.TruyVan(gd, @"
                INSERT INTO baotri.NhatKyBaoDuong (LoaiPhuongTien, MaDauMay, MaToaXe, CapBaoDuong, SoKmTaiThoiDiem,
                                                   GhiChuKyThuat, MaNguoiThucHien, ThoiDiemHoanThanh, ChiPhi)
                OUTPUT INSERTED.MaBaoDuong, INSERTED.ThoiDiemHoanThanh
                VALUES (@loai, @dm, @tx, @cap, @km, @ghiChu, @nguoi, SYSDATETIME(), @chiPhi)",
                ThamSoBaoDuong(bd)).Rows[0];
            return (Convert.ToInt32(r["MaBaoDuong"]), Convert.ToDateTime(r["ThoiDiemHoanThanh"]));
        }

        // Giu loai + phuong tien + thoi diem hoan thanh goc
        public int SuaBaoDuong(BaoDuongDTO bd, byte[] phienBan, PhienGiaoDich gd)
        {
            var thamSo = ThamSoBaoDuong(bd).ToList();
            thamSo.Add(new SqlParameter("@id", bd.MaBaoDuong));
            thamSo.Add(new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });
            return PhienGiaoDich.ThucThi(gd, @"
                UPDATE baotri.NhatKyBaoDuong
                SET CapBaoDuong = @cap, SoKmTaiThoiDiem = @km, GhiChuKyThuat = @ghiChu,
                    MaNguoiThucHien = @nguoi, ChiPhi = @chiPhi
                WHERE MaBaoDuong = @id AND PhienBan = @pb AND LoaiPhuongTien = @loai
                  AND ISNULL(MaDauMay, 0) = ISNULL(@dm, 0) AND ISNULL(MaToaXe, 0) = ISNULL(@tx, 0)",
                thamSo.ToArray());
        }

        // Doi trang thai dau may / toa sau bao duong (chi khi con dung trang thai cu)
        public int DoiTrangThaiDauMay(int maDauMay, string tuTrangThai, string sangTrangThai, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd,
                "UPDATE phuongtien.DauMay SET TrangThai = @moi WHERE MaDauMay = @id AND TrangThai = @cu",
                new SqlParameter("@moi", SqlDbType.VarChar, 20) { Value = sangTrangThai },
                new SqlParameter("@cu", SqlDbType.VarChar, 20) { Value = tuTrangThai },
                new SqlParameter("@id", maDauMay));

        public int DoiTrangThaiToaXe(int maToaXe, string tuTrangThai, string sangTrangThai, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd,
                "UPDATE phuongtien.ToaXe SET TrangThai = @moi WHERE MaToaXe = @id AND TrangThai = @cu",
                new SqlParameter("@moi", SqlDbType.VarChar, 20) { Value = sangTrangThai },
                new SqlParameter("@cu", SqlDbType.VarChar, 20) { Value = tuTrangThai },
                new SqlParameter("@id", maToaXe));

        // =====================================================================
        // TIEN ICH
        // =====================================================================

        private static SqlParameter[] ThamSoCapDau(CapNhienLieuDTO nl) => new[]
        {
            new SqlParameter("@dm", nl.MaDauMay),
            new SqlParameter("@noi", SqlDbType.NVarChar, 100) { Value = nl.NoiCapDau },
            new SqlParameter("@lit", SqlDbType.Decimal) { Precision = 8, Scale = 2, Value = nl.SoLitTraNap },
            new SqlParameter("@tan", SqlDbType.Decimal) { Precision = 7, Scale = 2, Value = nl.TrongLuongKeoTan },
            new SqlParameter("@km", SqlDbType.Decimal) { Precision = 6, Scale = 1, Value = nl.CuLyChayKm },
            new SqlParameter("@sfc", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = nl.SuatTieuHaoSFC },
            new SqlParameter("@vuot", nl.CanhBaoVuotMuc),
            new SqlParameter("@nguoi", SqlDbType.Int) { Value = (object?)nl.MaNguoiCap ?? DBNull.Value },
            new SqlParameter("@gia", SqlDbType.Decimal) { Precision = 10, Scale = 0, Value = nl.DonGiaVndLit }
        };

        private static SqlParameter[] ThamSoBaoDuong(BaoDuongDTO bd) => new[]
        {
            new SqlParameter("@loai", SqlDbType.VarChar, 20) { Value = bd.LoaiPhuongTien },
            new SqlParameter("@dm", SqlDbType.Int) { Value = (object?)bd.MaDauMay ?? DBNull.Value },
            new SqlParameter("@tx", SqlDbType.Int) { Value = (object?)bd.MaToaXe ?? DBNull.Value },
            new SqlParameter("@cap", SqlDbType.VarChar, 20) { Value = bd.CapBaoDuong },
            new SqlParameter("@km", SqlDbType.Decimal) { Precision = 9, Scale = 1, Value = bd.SoKmTaiThoiDiem },
            new SqlParameter("@ghiChu", SqlDbType.NVarChar, -1) { Value = string.IsNullOrWhiteSpace(bd.GhiChuKyThuat) ? DBNull.Value : (object)bd.GhiChuKyThuat },
            new SqlParameter("@nguoi", SqlDbType.Int) { Value = (object?)bd.MaNguoiThucHien ?? DBNull.Value },
            new SqlParameter("@chiPhi", SqlDbType.Decimal) { Precision = 14, Scale = 0, Value = bd.ChiPhi }
        };

        private static string Chuoi(DataRow r, string cot) => r[cot] == DBNull.Value ? "" : r[cot].ToString() ?? "";
    }
}
