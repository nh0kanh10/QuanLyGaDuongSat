using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using DTO.PhuongTien;

namespace DAL.Repositories
{
    // =========================================================================
    // LAP DOAN TAU: vanhanh.DoanTau + vanhanh.ChiTietDoanTau (chu bang: Dat)
    //
    // LayBoiCanh  : gom moi du lieu can de kiem tra mot phuong an cho mot chuyen.
    //               Goi trong PhienGiaoDich (khoaDong = true) khi Service kiem lai truoc khi ghi.
    // Them / CapNhat / ThayChiTiet: chi ghi, khong kiem nghiep vu (BUS da kiem bang
    //               QuyTacLapTau trong cung giao dich).
    // =========================================================================
    public class LapTauRepository
    {
        // Cac chuyen co dau may / toa xe trong khoang nay quanh chuyen dang xet duoc nap kem
        // (de biet trung gio, va dau may vua ket thuc chuyen truoc o ga nao).
        private const int SoNgayXetTruoc = 7;

        public BoiCanhLapTauDTO? LayBoiCanh(int maChuyenTau, PhienGiaoDich? gd = null, bool khoaDong = false)
        {
            var bc = LayChuyenVaDoanTau(maChuyenTau, gd, khoaDong);
            if (bc == null) return null;

            bc.HanhTrinh = LayHanhTrinh(maChuyenTau, gd);
            bc.DoanHienTai = bc.MaDoanTau.HasValue ? LayChiTietDoanTau(bc.MaDoanTau.Value, gd) : new();

            // Pham vi ly trinh cua hanh trinh (de tim khu gian can may day)
            var (kmTu, kmDen) = LayPhamViLyTrinh(bc, gd);
            bc.KhuGianCanDay = LayKhuGianCanDay(kmTu, kmDen, gd);

            bc.DauMay = LayDauMay(bc, gd);
            bc.ToaXe = LayToaXe(bc, gd);

            // [13/11] Toa ban ve dang ban + ve tren cac toa do (dong bo toa ban ve, khong lam hong ve da ban)
            bc.ToaBanVe = LayToaBanVe(maChuyenTau, gd);
            bc.VeTrenToa = LayVeTrenToa(maChuyenTau, gd);
            return bc;
        }

        // ---------------------------------------------------------------------
        // Chuyen + doan tau hien tai
        // ---------------------------------------------------------------------
        private static BoiCanhLapTauDTO? LayChuyenVaDoanTau(int maChuyenTau, PhienGiaoDich? gd, bool khoaDong)
        {
            // Trong giao dich luu: khoa dong chuyen + doan tau toi het giao dich de khong ai
            // doi trang thai chuyen / doan tau giua luc kiem tra va luc ghi.
            string khoa = khoaDong ? "WITH (UPDLOCK, HOLDLOCK)" : "";
            string sql = $@"
                SELECT ct.MaChuyenTau, mt.SoHieuMacTau, mt.LoaiTau, ct.TrangThai, ct.NgayXuatPhat,
                       ct.GioXuatPhatKH, ct.GioVeDichKH,
                       gDi.MaGaCode AS MaGaDiCode, gDi.TenGa AS TenGaDi,
                       gDen.MaGaCode AS MaGaDenCode, gDen.TenGa AS TenGaDen,
                       dt.MaDoanTau, dt.PhienBan, dt.MaDauMayChinh, dt.MaDauMayDay, dt.DaDuyetAnToan,
                       dt.TongSoToa, dt.TongChieuDaiM, dt.TongTrongLuongTan
                FROM vanhanh.ChuyenTau ct {khoa}
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                JOIN hatang.Ga gDi ON gDi.MaGa = mt.MaGaDi
                JOIN hatang.Ga gDen ON gDen.MaGa = mt.MaGaDen
                LEFT JOIN vanhanh.DoanTau dt {khoa} ON dt.MaChuyenTau = ct.MaChuyenTau
                WHERE ct.MaChuyenTau = @ct";

            DataTable bang = PhienGiaoDich.TruyVan(gd, sql, new SqlParameter("@ct", maChuyenTau));
            if (bang.Rows.Count == 0) return null;

            DataRow r = bang.Rows[0];
            bool coDoan = r["MaDoanTau"] != DBNull.Value;
            return new BoiCanhLapTauDTO
            {
                MaChuyenTau = Convert.ToInt32(r["MaChuyenTau"]),
                SoHieuMacTau = Chuoi(r, "SoHieuMacTau"),
                LoaiTau = Chuoi(r, "LoaiTau"),
                TrangThaiChuyen = Chuoi(r, "TrangThai"),
                NgayXuatPhat = Convert.ToDateTime(r["NgayXuatPhat"]),
                GioXuatPhatKH = Convert.ToDateTime(r["GioXuatPhatKH"]),
                GioVeDichKH = Convert.ToDateTime(r["GioVeDichKH"]),
                MaGaDiCode = Chuoi(r, "MaGaDiCode"),
                TenGaDi = Chuoi(r, "TenGaDi"),
                MaGaDenCode = Chuoi(r, "MaGaDenCode"),
                TenGaDen = Chuoi(r, "TenGaDen"),
                MaDoanTau = coDoan ? Convert.ToInt32(r["MaDoanTau"]) : null,
                PhienBan = coDoan ? (byte[])r["PhienBan"] : null,
                MaDauMayChinh = coDoan ? Convert.ToInt32(r["MaDauMayChinh"]) : null,
                MaDauMayDay = r["MaDauMayDay"] != DBNull.Value ? Convert.ToInt32(r["MaDauMayDay"]) : null,
                DaDuyetAnToan = coDoan && Convert.ToBoolean(r["DaDuyetAnToan"]),
                TongSoToa = coDoan ? Convert.ToInt32(r["TongSoToa"]) : 0,
                TongChieuDaiM = coDoan ? Convert.ToDecimal(r["TongChieuDaiM"]) : 0m,
                TongTrongLuongTan = coDoan ? Convert.ToDecimal(r["TongTrongLuongTan"]) : 0m
            };
        }

        private static List<ToaTrongDoanDTO> LayChiTietDoanTau(int maDoanTau, PhienGiaoDich? gd)
        {
            DataTable bang = PhienGiaoDich.TruyVan(gd, @"
                SELECT MaToaXe, MaGaNoi, MaGaCat
                FROM vanhanh.ChiTietDoanTau
                WHERE MaDoanTau = @dt
                ORDER BY ThuTuNoi",
                new SqlParameter("@dt", maDoanTau));

            return bang.Rows.Cast<DataRow>().Select(r => new ToaTrongDoanDTO
            {
                MaToaXe = Convert.ToInt32(r["MaToaXe"]),
                MaGaNoi = r["MaGaNoi"] != DBNull.Value ? Convert.ToInt32(r["MaGaNoi"]) : null,
                MaGaCat = r["MaGaCat"] != DBNull.Value ? Convert.ToInt32(r["MaGaCat"]) : null
            }).ToList();
        }

        // ---------------------------------------------------------------------
        // Hanh trinh: ga dung + duong ray de doi chieu chieu dai tung ga
        // ---------------------------------------------------------------------
        private static List<DiemDungHanhTrinhDTO> LayHanhTrinh(int maChuyenTau, PhienGiaoDich? gd)
        {
            // Duong tranh dai nhat cua ga, bo duong dang bao tri. Diem dung da duoc dieu do
            // gan duong ray (MaDuongRay) thi dung dung duong do (cung la duong trigger
            // trg_LichDungGa_CheckChieuDaiTau doi chieu).
            DataTable bang = PhienGiaoDich.TruyVan(gd, @"
                SELECT ld.ThuTuDung, ld.MaGa, g.MaGaCode, g.TenGa, g.LyTrinhKm,
                       ld.GioDenKeHoach, ld.GioDiKeHoach,
                       ld.MaDuongRay, drg.SoHieuDuong AS SoHieuDuongGan, drg.ChieuDaiHuuDungM AS ChieuDaiDuongGanM,
                       tr.ChieuDaiHuuDungM AS DuongTranhDaiNhatM, tr.SoHieuDuong AS SoHieuDuongTranhDaiNhat
                FROM vanhanh.LichDungGa ld
                JOIN hatang.Ga g ON g.MaGa = ld.MaGa
                LEFT JOIN hatang.DuongRayGa drg ON drg.MaDuongRay = ld.MaDuongRay
                OUTER APPLY (SELECT TOP 1 dr.ChieuDaiHuuDungM, dr.SoHieuDuong
                             FROM hatang.DuongRayGa dr
                             WHERE dr.MaGa = ld.MaGa AND dr.LoaiDuong = 'DUONG_TRANH' AND dr.TrangThai <> 'BAO_TRI'
                             ORDER BY dr.ChieuDaiHuuDungM DESC, dr.SoHieuDuong) tr
                WHERE ld.MaChuyenTau = @ct
                ORDER BY ld.ThuTuDung",
                new SqlParameter("@ct", maChuyenTau));

            return bang.Rows.Cast<DataRow>().Select(r => new DiemDungHanhTrinhDTO
            {
                ThuTuDung = Convert.ToInt32(r["ThuTuDung"]),
                MaGa = Convert.ToInt32(r["MaGa"]),
                MaGaCode = Chuoi(r, "MaGaCode"),
                TenGa = Chuoi(r, "TenGa"),
                LyTrinhKm = Convert.ToDecimal(r["LyTrinhKm"]),
                GioDenKeHoach = Convert.ToDateTime(r["GioDenKeHoach"]),
                GioDiKeHoach = Convert.ToDateTime(r["GioDiKeHoach"]),
                MaDuongRayGan = SoNguyen(r, "MaDuongRay"),
                SoHieuDuongGan = SoNguyen(r, "SoHieuDuongGan"),
                ChieuDaiDuongGanM = SoThuc(r, "ChieuDaiDuongGanM"),
                DuongTranhDaiNhatM = SoThuc(r, "DuongTranhDaiNhatM"),
                SoHieuDuongTranhDaiNhat = SoNguyen(r, "SoHieuDuongTranhDaiNhat")
            }).ToList();
        }

        private static (decimal KmTu, decimal KmDen) LayPhamViLyTrinh(BoiCanhLapTauDTO bc, PhienGiaoDich? gd)
        {
            if (bc.HanhTrinh.Count >= 2)
            {
                decimal a = bc.HanhTrinh[0].LyTrinhKm, b = bc.HanhTrinh[^1].LyTrinhKm;
                return (Math.Min(a, b), Math.Max(a, b));
            }

            // Chua co lich dung ga: lay theo ga di / ga den cua mac tau
            DataTable bang = PhienGiaoDich.TruyVan(gd, @"
                SELECT gDi.LyTrinhKm AS KmDi, gDen.LyTrinhKm AS KmDen
                FROM vanhanh.ChuyenTau ct
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                JOIN hatang.Ga gDi ON gDi.MaGa = mt.MaGaDi
                JOIN hatang.Ga gDen ON gDen.MaGa = mt.MaGaDen
                WHERE ct.MaChuyenTau = @ct",
                new SqlParameter("@ct", bc.MaChuyenTau));
            if (bang.Rows.Count == 0) return (0m, 0m);

            decimal di = Convert.ToDecimal(bang.Rows[0]["KmDi"]), den = Convert.ToDecimal(bang.Rows[0]["KmDen"]);
            return (Math.Min(di, den), Math.Max(di, den));
        }

        private static List<KhuGianCanDayDTO> LayKhuGianCanDay(decimal kmTu, decimal kmDen, PhienGiaoDich? gd)
        {
            DataTable bang = PhienGiaoDich.TruyVan(gd, @"
                SELECT g1.MaGaCode AS TuGaCode, g2.MaGaCode AS DenGaCode, g1.TenGa AS TenTuGa, g2.TenGa AS TenDenGa,
                       kg.DoDocPermil
                FROM hatang.KhuGian kg
                JOIN hatang.Ga g1 ON g1.MaGa = kg.MaGaDau
                JOIN hatang.Ga g2 ON g2.MaGa = kg.MaGaCuoi
                WHERE kg.CanDauMayDay = 1
                  AND IIF(g1.LyTrinhKm < g2.LyTrinhKm, g1.LyTrinhKm, g2.LyTrinhKm) >= @kmTu
                  AND IIF(g1.LyTrinhKm > g2.LyTrinhKm, g1.LyTrinhKm, g2.LyTrinhKm) <= @kmDen
                ORDER BY IIF(g1.LyTrinhKm < g2.LyTrinhKm, g1.LyTrinhKm, g2.LyTrinhKm)",
                new SqlParameter("@kmTu", SqlDbType.Decimal) { Precision = 7, Scale = 2, Value = kmTu },
                new SqlParameter("@kmDen", SqlDbType.Decimal) { Precision = 7, Scale = 2, Value = kmDen });

            return bang.Rows.Cast<DataRow>().Select(r => new KhuGianCanDayDTO
            {
                TuGaCode = Chuoi(r, "TuGaCode"),
                DenGaCode = Chuoi(r, "DenGaCode"),
                TenTuGa = Chuoi(r, "TenTuGa"),
                TenDenGa = Chuoi(r, "TenDenGa"),
                DoDocPermil = Convert.ToDecimal(r["DoDocPermil"])
            }).ToList();
        }

        // ---------------------------------------------------------------------
        // Dau may + cac chuyen khac dang / da dung tung dau may
        // ---------------------------------------------------------------------
        private static List<DauMayLapTauDTO> LayDauMay(BoiCanhLapTauDTO bc, PhienGiaoDich? gd)
        {
            DataTable bangDauMay = PhienGiaoDich.TruyVan(gd, @"
                SELECT dm.MaDauMay, dm.SoHieuDauMay, dm.DonViQuanLy, dm.TrangThai,
                       ddm.MaDongCode, ddm.SucKeoToiDaTan, ddm.ChieuDaiM, ddm.TrongLuongTan
                FROM phuongtien.DauMay dm
                JOIN phuongtien.DongDauMay ddm ON ddm.MaDongDauMay = dm.MaDongDauMay
                ORDER BY dm.SoHieuDauMay");

            // Moi dong = mot lan dau may xuat hien trong doan tau cua chuyen KHAC (keo hoac day),
            // tru chuyen da huy, trong khoang [GioXP - 7 ngay, GioVD].
            DataTable bangSuDung = PhienGiaoDich.TruyVan(gd, @"
                SELECT x.MaDauMay, x.VaiTro, ct.MaChuyenTau, mt.SoHieuMacTau, ct.TrangThai,
                       ct.GioXuatPhatKH, ct.GioVeDichKH,
                       COALESCE(gDau.MaGaCode, gMacDi.MaGaCode) AS GaDauCode,
                       COALESCE(gCuoi.MaGaCode, gMacDen.MaGaCode) AS GaCuoiCode,
                       COALESCE(gCuoi.TenGa, gMacDen.TenGa) AS GaCuoiTen
                FROM vanhanh.DoanTau dt
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                JOIN hatang.Ga gMacDi ON gMacDi.MaGa = mt.MaGaDi
                JOIN hatang.Ga gMacDen ON gMacDen.MaGa = mt.MaGaDen
                CROSS APPLY (VALUES (dt.MaDauMayChinh, 'KEO_CHINH'), (dt.MaDauMayDay, 'DAY')) x(MaDauMay, VaiTro)
                OUTER APPLY (SELECT TOP 1 g.MaGaCode FROM vanhanh.LichDungGa ld JOIN hatang.Ga g ON g.MaGa = ld.MaGa
                             WHERE ld.MaChuyenTau = ct.MaChuyenTau ORDER BY ld.ThuTuDung ASC) gDau
                OUTER APPLY (SELECT TOP 1 g.MaGaCode, g.TenGa FROM vanhanh.LichDungGa ld JOIN hatang.Ga g ON g.MaGa = ld.MaGa
                             WHERE ld.MaChuyenTau = ct.MaChuyenTau ORDER BY ld.ThuTuDung DESC) gCuoi
                WHERE x.MaDauMay IS NOT NULL
                  AND ct.MaChuyenTau <> @ct
                  AND ct.TrangThai <> 'DA_HUY'
                  AND ct.GioVeDichKH > DATEADD(DAY, -@soNgay, @gioXP)
                  AND ct.GioXuatPhatKH < @gioVD",
                new SqlParameter("@ct", bc.MaChuyenTau),
                new SqlParameter("@soNgay", SoNgayXetTruoc),
                new SqlParameter("@gioXP", SqlDbType.DateTime2) { Value = bc.GioXuatPhatKH },
                new SqlParameter("@gioVD", SqlDbType.DateTime2) { Value = bc.GioVeDichKH });

            var suDungTheoDauMay = bangSuDung.Rows.Cast<DataRow>()
                .GroupBy(r => Convert.ToInt32(r["MaDauMay"]))
                .ToDictionary(g => g.Key, g => g.Select(TaoChuyenSuDung).ToList());

            return bangDauMay.Rows.Cast<DataRow>().Select(r =>
            {
                int ma = Convert.ToInt32(r["MaDauMay"]);
                return new DauMayLapTauDTO
                {
                    MaDauMay = ma,
                    SoHieuDauMay = Chuoi(r, "SoHieuDauMay"),
                    MaDongCode = Chuoi(r, "MaDongCode"),
                    DonViQuanLy = Chuoi(r, "DonViQuanLy"),
                    TrangThai = Chuoi(r, "TrangThai"),
                    SucKeoToiDaTan = Convert.ToDecimal(r["SucKeoToiDaTan"]),
                    ChieuDaiM = Convert.ToDecimal(r["ChieuDaiM"]),
                    TrongLuongTan = Convert.ToDecimal(r["TrongLuongTan"]),
                    ChuyenKhac = suDungTheoDauMay.GetValueOrDefault(ma) ?? new()
                };
            }).ToList();
        }

        // ---------------------------------------------------------------------
        // Toa xe + cac chuyen khac CON HIEU LUC, CHONG GIO co toa trong doan tau
        // ---------------------------------------------------------------------
        private static List<ToaXeLapTauDTO> LayToaXe(BoiCanhLapTauDTO bc, PhienGiaoDich? gd)
        {
            DataTable bangToa = PhienGiaoDich.TruyVan(gd, @"
                SELECT tx.MaToaXe, tx.SoHieuToaXe, tx.TrangThai, tx.TuTrongTan, tx.TaiTrongToiDaTan,
                       cl.MaChungLoaiCode, cl.TenMoTa, cl.ChieuDaiChuanM, cl.SoTruc, cl.SucChuaChuan
                FROM phuongtien.ToaXe tx
                JOIN phuongtien.ChungLoaiToa cl ON cl.MaChungLoai = tx.MaChungLoai
                ORDER BY cl.MaChungLoaiCode, tx.SoHieuToaXe");

            DataTable bangSuDung = PhienGiaoDich.TruyVan(gd, @"
                SELECT c.MaToaXe, ct.MaChuyenTau, mt.SoHieuMacTau, ct.TrangThai, ct.GioXuatPhatKH, ct.GioVeDichKH,
                       gMacDi.MaGaCode AS GaDauCode, gMacDen.MaGaCode AS GaCuoiCode, gMacDen.TenGa AS GaCuoiTen,
                       'TOA' AS VaiTro
                FROM vanhanh.ChiTietDoanTau c
                JOIN vanhanh.DoanTau dt ON dt.MaDoanTau = c.MaDoanTau
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = dt.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                JOIN hatang.Ga gMacDi ON gMacDi.MaGa = mt.MaGaDi
                JOIN hatang.Ga gMacDen ON gMacDen.MaGa = mt.MaGaDen
                WHERE ct.MaChuyenTau <> @ct
                  AND ct.TrangThai NOT IN ('HOAN_THANH', 'DA_HUY')
                  AND IIF(ct.GioXuatPhatKH > @gioXP, ct.GioXuatPhatKH, @gioXP)
                    < IIF(ct.GioVeDichKH < @gioVD, ct.GioVeDichKH, @gioVD)",
                new SqlParameter("@ct", bc.MaChuyenTau),
                new SqlParameter("@gioXP", SqlDbType.DateTime2) { Value = bc.GioXuatPhatKH },
                new SqlParameter("@gioVD", SqlDbType.DateTime2) { Value = bc.GioVeDichKH });

            var suDungTheoToa = bangSuDung.Rows.Cast<DataRow>()
                .GroupBy(r => Convert.ToInt32(r["MaToaXe"]))
                .ToDictionary(g => g.Key, g => g.Select(TaoChuyenSuDung).ToList());

            return bangToa.Rows.Cast<DataRow>().Select(r =>
            {
                int ma = Convert.ToInt32(r["MaToaXe"]);
                return new ToaXeLapTauDTO
                {
                    MaToaXe = ma,
                    SoHieuToaXe = Chuoi(r, "SoHieuToaXe"),
                    MaChungLoaiCode = Chuoi(r, "MaChungLoaiCode"),
                    TenMoTa = Chuoi(r, "TenMoTa"),
                    TrangThai = Chuoi(r, "TrangThai"),
                    ChieuDaiM = Convert.ToDecimal(r["ChieuDaiChuanM"]),
                    SoTruc = Convert.ToInt32(r["SoTruc"]),
                    TuTrongTan = Convert.ToDecimal(r["TuTrongTan"]),
                    TaiTrongToiDaTan = Convert.ToDecimal(r["TaiTrongToiDaTan"]),
                    SucChuaChuan = r["SucChuaChuan"] != DBNull.Value ? Convert.ToInt32(r["SucChuaChuan"]) : 0,
                    ChuyenKhac = suDungTheoToa.GetValueOrDefault(ma) ?? new()
                };
            }).ToList();
        }

        // ---------------------------------------------------------------------
        // [13/11] Toa ban ve (vantai.ToaXeKhach, bang cua A Ba) dang ban + ve tren cac toa do
        // ---------------------------------------------------------------------
        private static List<ToaBanVeDTO> LayToaBanVe(int maChuyenTau, PhienGiaoDich? gd)
        {
            DataTable bang = PhienGiaoDich.TruyVan(gd, @"
                SELECT t.MaToaXeKhach, t.ThuTuToa, t.NhanHieuToa, t.LoaiToa, t.SucChua, t.MaToaXe, t.MaGaNoi, t.MaGaCat,
                       (SELECT COUNT(*) FROM vantai.ChoNgoi c WHERE c.MaToaXeKhach = t.MaToaXeKhach) AS SoGhe
                FROM vantai.ToaXeKhach t
                WHERE t.MaChuyenTau = @ct AND t.NgungBan = 0
                ORDER BY t.ThuTuToa",
                new SqlParameter("@ct", maChuyenTau));

            return bang.Rows.Cast<DataRow>().Select(r => new ToaBanVeDTO
            {
                MaToaXeKhach = Convert.ToInt32(r["MaToaXeKhach"]),
                ThuTuToa = Convert.ToInt32(r["ThuTuToa"]),
                NhanHieuToa = Chuoi(r, "NhanHieuToa"),
                LoaiToa = Chuoi(r, "LoaiToa"),
                SucChua = Convert.ToInt32(r["SucChua"]),
                SoGhe = Convert.ToInt32(r["SoGhe"]),
                MaToaXe = SoNguyen(r, "MaToaXe"),
                MaGaNoi = SoNguyen(r, "MaGaNoi"),
                MaGaCat = SoNguyen(r, "MaGaCat")
            }).ToList();
        }

        private static List<VeTrenToaDTO> LayVeTrenToa(int maChuyenTau, PhienGiaoDich? gd)
        {
            DataTable bang = PhienGiaoDich.TruyVan(gd, @"
                SELECT v.MaVe, v.MaVeCode, c.MaToaXeKhach, c.SoGhe, v.MaGaDi, v.MaGaDen,
                       gDi.MaGaCode AS MaGaDiCode, gDen.MaGaCode AS MaGaDenCode, v.TrangThai, v.TenHanhKhach
                FROM vantai.Ve v
                JOIN vantai.ChoNgoi c ON c.MaChoNgoi = v.MaChoNgoi
                JOIN vantai.ToaXeKhach t ON t.MaToaXeKhach = c.MaToaXeKhach
                JOIN hatang.Ga gDi ON gDi.MaGa = v.MaGaDi
                JOIN hatang.Ga gDen ON gDen.MaGa = v.MaGaDen
                WHERE t.MaChuyenTau = @ct AND t.NgungBan = 0",
                new SqlParameter("@ct", maChuyenTau));

            return bang.Rows.Cast<DataRow>().Select(r => new VeTrenToaDTO
            {
                MaVe = Convert.ToInt32(r["MaVe"]),
                MaVeCode = Chuoi(r, "MaVeCode"),
                MaToaXeKhach = Convert.ToInt32(r["MaToaXeKhach"]),
                SoGhe = Convert.ToInt32(r["SoGhe"]),
                MaGaDi = Convert.ToInt32(r["MaGaDi"]),
                MaGaDen = Convert.ToInt32(r["MaGaDen"]),
                MaGaDiCode = Chuoi(r, "MaGaDiCode"),
                MaGaDenCode = Chuoi(r, "MaGaDenCode"),
                TrangThai = Chuoi(r, "TrangThai"),
                TenHanhKhach = Chuoi(r, "TenHanhKhach")
            }).ToList();
        }

        private static ChuyenSuDungDTO TaoChuyenSuDung(DataRow r) => new()
        {
            MaChuyenTau = Convert.ToInt32(r["MaChuyenTau"]),
            SoHieuMacTau = Chuoi(r, "SoHieuMacTau"),
            TrangThai = Chuoi(r, "TrangThai"),
            GioXuatPhatKH = Convert.ToDateTime(r["GioXuatPhatKH"]),
            GioVeDichKH = Convert.ToDateTime(r["GioVeDichKH"]),
            VaiTro = Chuoi(r, "VaiTro"),
            GaDauCode = Chuoi(r, "GaDauCode"),
            GaCuoiCode = Chuoi(r, "GaCuoiCode"),
            GaCuoiTen = Chuoi(r, "GaCuoiTen")
        };

        // ---------------------------------------------------------------------
        // GHI (chi goi trong PhienGiaoDich, sau khi BUS da kiem tra)
        // ---------------------------------------------------------------------

        public int ThemDoanTau(int maChuyenTau, int maDauMayChinh, int? maDauMayDay,
                               int tongSoToa, decimal tongChieuDaiM, decimal tongTrongLuongTan, PhienGiaoDich gd)
        {
            try
            {
                return Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd, @"
                    INSERT INTO vanhanh.DoanTau (MaChuyenTau, MaDauMayChinh, MaDauMayDay, TongSoToa, TongChieuDaiM,
                                                 TongTrongLuongTan, DaDuyetAnToan)
                    VALUES (@ct, @chinh, @day, @soToa, @dai, @nang, 0);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new SqlParameter("@ct", maChuyenTau),
                    new SqlParameter("@chinh", maDauMayChinh),
                    new SqlParameter("@day", SqlDbType.Int) { Value = (object?)maDauMayDay ?? DBNull.Value },
                    new SqlParameter("@soToa", tongSoToa),
                    new SqlParameter("@dai", SqlDbType.Decimal) { Precision = 6, Scale = 1, Value = tongChieuDaiM },
                    new SqlParameter("@nang", SqlDbType.Decimal) { Precision = 7, Scale = 2, Value = tongTrongLuongTan }));
            }
            catch (SqlException ex)
            {
                throw DichLoiSqlPhuongTien.Dich(ex);
            }
        }

        // Tra ve so dong cap nhat: 0 = PhienBan da doi (co nguoi khac vua ghi doan tau nay)
        public int CapNhatDoanTau(int maDoanTau, byte[] phienBan, int maDauMayChinh, int? maDauMayDay,
                                  int tongSoToa, decimal tongChieuDaiM, decimal tongTrongLuongTan,
                                  bool daDuyetAnToan, PhienGiaoDich gd)
        {
            try
            {
                return PhienGiaoDich.ThucThi(gd, @"
                    UPDATE vanhanh.DoanTau
                    SET MaDauMayChinh = @chinh, MaDauMayDay = @day, TongSoToa = @soToa,
                        TongChieuDaiM = @dai, TongTrongLuongTan = @nang, DaDuyetAnToan = @duyet
                    WHERE MaDoanTau = @id AND PhienBan = @pb",
                    new SqlParameter("@id", maDoanTau),
                    new SqlParameter("@pb", SqlDbType.Binary, 8) { Value = phienBan },
                    new SqlParameter("@chinh", maDauMayChinh),
                    new SqlParameter("@day", SqlDbType.Int) { Value = (object?)maDauMayDay ?? DBNull.Value },
                    new SqlParameter("@soToa", tongSoToa),
                    new SqlParameter("@dai", SqlDbType.Decimal) { Precision = 6, Scale = 1, Value = tongChieuDaiM },
                    new SqlParameter("@nang", SqlDbType.Decimal) { Precision = 7, Scale = 2, Value = tongTrongLuongTan },
                    new SqlParameter("@duyet", daDuyetAnToan));
            }
            catch (SqlException ex)
            {
                throw DichLoiSqlPhuongTien.Dich(ex);
            }
        }

        // [Dat 09/11] Nut "Chuyen Trang Thai" o Lich trinh (ChuyenTauService.DoiTrangThai): doi trong giao dich cua Service,
        // chi khi trang thai trong CSDL van la trang thai cu. 0 dong = nguoi khac vua doi truoc.
        // [13/11] phienBan khac null: con phai dung PhienBan cua chuyen luc man hinh nap (chong ghi de ca chuyen).
        public int DoiTrangThaiChuyen(int maChuyenTau, string trangThaiCu, string trangThaiMoi, PhienGiaoDich gd,
                                      byte[]? phienBan = null)
            => PhienGiaoDich.ThucThi(gd,
                @"UPDATE vanhanh.ChuyenTau SET TrangThai = @moi
                  WHERE MaChuyenTau = @id AND TrangThai = @cu AND (@pb IS NULL OR PhienBan = @pb)",
                new SqlParameter("@moi", SqlDbType.VarChar, 20) { Value = trangThaiMoi },
                new SqlParameter("@cu", SqlDbType.VarChar, 20) { Value = trangThaiCu },
                new SqlParameter("@id", maChuyenTau),
                new SqlParameter("@pb", SqlDbType.Binary, 8) { Value = (object?)phienBan ?? DBNull.Value });

        // [13/11] PhienBan (ROWVERSION) hien tai cua chuyen; null = chuyen khong con
        public byte[]? LayPhienBanChuyen(int maChuyenTau, PhienGiaoDich gd)
            => PhienGiaoDich.LayGiaTri(gd, "SELECT PhienBan FROM vanhanh.ChuyenTau WHERE MaChuyenTau = @id",
                   new SqlParameter("@id", maChuyenTau)) as byte[];

        // [13/11] "Cham" dong chuyen: doi PhienBan (moi thay doi doan tau / lich dung / trang thai deu la thay doi cua chuyen)
        // -> man Lich trinh dang mo chuyen nay biet du lieu da cu, khong ghi de.
        public void ChamPhienBanChuyen(int maChuyenTau, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, "UPDATE vanhanh.ChuyenTau SET SoPhutTreLuyKe = SoPhutTreLuyKe WHERE MaChuyenTau = @id",
                   new SqlParameter("@id", maChuyenTau));

        // [13/11] Ghi ke hoach dong bo toa ban ve (QuyTacToaBanVe.LapKeHoach) trong giao dich luu doan tau.
        // Thu tu: bo toa cu (xoa / ngung ban) truoc de "Toa k" moi khong dung chi muc duy nhat, roi cap nhat, roi them.
        public void ApDungDongBoToaBanVe(int maChuyenTau, KeHoachDongBoToaBanVeDTO keHoach, PhienGiaoDich gd)
        {
            try
            {
                foreach (var t in keHoach.ThaoTac.Where(x => x.Loai == LoaiThaoTacToaBanVe.Xoa))
                {
                    PhienGiaoDich.ThucThi(gd, @"
                        DELETE FROM vantai.ChoNgoi WHERE MaToaXeKhach = @toa;
                        DELETE FROM vantai.ToaXeKhach WHERE MaToaXeKhach = @toa AND MaChuyenTau = @ct;",
                        new SqlParameter("@toa", t.MaToaXeKhach!.Value),
                        new SqlParameter("@ct", maChuyenTau));
                }
                foreach (var t in keHoach.ThaoTac.Where(x => x.Loai == LoaiThaoTacToaBanVe.NgungBan))
                {
                    PhienGiaoDich.ThucThi(gd,
                        "UPDATE vantai.ToaXeKhach SET NgungBan = 1 WHERE MaToaXeKhach = @toa AND MaChuyenTau = @ct",
                        new SqlParameter("@toa", t.MaToaXeKhach!.Value),
                        new SqlParameter("@ct", maChuyenTau));
                }
                foreach (var t in keHoach.ThaoTac.Where(x => x.Loai == LoaiThaoTacToaBanVe.CapNhat && x.CoGhi))
                {
                    PhienGiaoDich.ThucThi(gd, @"
                        UPDATE vantai.ToaXeKhach SET MaToaXe = @toaThat, MaGaNoi = @noi, MaGaCat = @cat
                        WHERE MaToaXeKhach = @toa AND MaChuyenTau = @ct;",
                        new SqlParameter("@toa", t.MaToaXeKhach!.Value),
                        new SqlParameter("@ct", maChuyenTau),
                        new SqlParameter("@toaThat", SqlDbType.Int) { Value = (object?)t.MaToaXe ?? DBNull.Value },
                        new SqlParameter("@noi", SqlDbType.Int) { Value = (object?)t.MaGaNoi ?? DBNull.Value },
                        new SqlParameter("@cat", SqlDbType.Int) { Value = (object?)t.MaGaCat ?? DBNull.Value });
                    SinhGheConThieu(t.MaToaXeKhach!.Value, gd);
                }
                foreach (var t in keHoach.ThaoTac.Where(x => x.Loai == LoaiThaoTacToaBanVe.Them))
                {
                    int maToa = Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd, @"
                        INSERT INTO vantai.ToaXeKhach (MaChuyenTau, NhanHieuToa, LoaiToa, ThuTuToa, SucChua, MaToaXe, MaGaNoi, MaGaCat)
                        VALUES (@ct, @nhan, @loai, @thuTu, @sucChua, @toaThat, @noi, @cat);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new SqlParameter("@ct", maChuyenTau),
                        new SqlParameter("@nhan", SqlDbType.NVarChar, 20) { Value = t.NhanHieuToa },
                        new SqlParameter("@loai", SqlDbType.VarChar, 20) { Value = t.LoaiToa },
                        new SqlParameter("@thuTu", t.ThuTuToa),
                        new SqlParameter("@sucChua", t.SucChua),
                        new SqlParameter("@toaThat", SqlDbType.Int) { Value = (object?)t.MaToaXe ?? DBNull.Value },
                        new SqlParameter("@noi", SqlDbType.Int) { Value = (object?)t.MaGaNoi ?? DBNull.Value },
                        new SqlParameter("@cat", SqlDbType.Int) { Value = (object?)t.MaGaCat ?? DBNull.Value }));
                    SinhGheConThieu(maToa, gd);
                }
            }
            catch (SqlException ex)
            {
                throw DichLoiSqlPhuongTien.Dich(ex);
            }
        }

        // Ghe 1..SucChua con thieu cua mot toa ban ve (y ChuyenTauRepository.SinhGheChoToa cua A Ba:
        // AN tang 1-2, BN tang 1-3, NC / NML khong tang)
        private static void SinhGheConThieu(int maToaXeKhach, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                ;WITH So AS (SELECT TOP (200) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_objects)
                INSERT INTO vantai.ChoNgoi (MaToaXeKhach, SoGhe, TangGiuong, LaGhePhu)
                SELECT t.MaToaXeKhach, s.n,
                       CASE t.LoaiToa WHEN 'AN' THEN ((s.n - 1) % 2) + 1 WHEN 'BN' THEN ((s.n - 1) % 3) + 1 END, 0
                FROM vantai.ToaXeKhach t
                JOIN So s ON s.n <= t.SucChua
                WHERE t.MaToaXeKhach = @toa
                  AND NOT EXISTS (SELECT 1 FROM vantai.ChoNgoi c WHERE c.MaToaXeKhach = t.MaToaXeKhach AND c.SoGhe = s.n);",
                new SqlParameter("@toa", maToaXeKhach));

        // Xoa het toa cu roi chen lai theo thu tu moi (ThuTuNoi 1..n) trong cung giao dich
        public void ThayChiTietDoanTau(int maDoanTau, IReadOnlyList<ToaTrongDoanDTO> danhSachToa, PhienGiaoDich gd)
        {
            try
            {
                PhienGiaoDich.ThucThi(gd, "DELETE FROM vanhanh.ChiTietDoanTau WHERE MaDoanTau = @dt",
                    new SqlParameter("@dt", maDoanTau));

                if (danhSachToa.Count == 0) return;

                // Mot lenh INSERT nhieu dong (toi da vai chuc toa), tham so danh so theo dong
                var sql = new StringBuilder(
                    "INSERT INTO vanhanh.ChiTietDoanTau (MaDoanTau, MaToaXe, ThuTuNoi, MaGaNoi, MaGaCat) VALUES ");
                var thamSo = new List<SqlParameter> { new("@dt", maDoanTau) };
                for (int i = 0; i < danhSachToa.Count; i++)
                {
                    var t = danhSachToa[i];
                    if (i > 0) sql.Append(", ");
                    sql.Append($"(@dt, @toa{i}, @tt{i}, @noi{i}, @cat{i})");
                    thamSo.Add(new SqlParameter($"@toa{i}", t.MaToaXe));
                    thamSo.Add(new SqlParameter($"@tt{i}", i + 1));
                    thamSo.Add(new SqlParameter($"@noi{i}", SqlDbType.Int) { Value = (object?)t.MaGaNoi ?? DBNull.Value });
                    thamSo.Add(new SqlParameter($"@cat{i}", SqlDbType.Int) { Value = (object?)t.MaGaCat ?? DBNull.Value });
                }

                PhienGiaoDich.ThucThi(gd, sql.ToString(), thamSo.ToArray());
            }
            catch (SqlException ex)
            {
                throw DichLoiSqlPhuongTien.Dich(ex);
            }
        }

        // ---------------------------------------------------------------------
        private static string Chuoi(DataRow r, string cot)
            => r[cot] != DBNull.Value ? r[cot].ToString() ?? "" : "";

        private static int? SoNguyen(DataRow r, string cot)
            => r[cot] != DBNull.Value ? Convert.ToInt32(r[cot]) : null;

        private static decimal? SoThuc(DataRow r, string cot)
            => r[cot] != DBNull.Value ? Convert.ToDecimal(r[cot]) : null;
    }
}
