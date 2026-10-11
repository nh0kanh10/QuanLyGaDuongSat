using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using DTO.NhanSu;

namespace DAL.Repositories
{
    // =========================================================================
    // NHAN SU: nhansu.NhanVien, PhanCongKipLai, KiemTraSucKhoe (chu bang: Dat)
    //
    // LayDuLieu : anh chup du lieu cho ca trang Nhan su (4 tab + 3 dialog) va cho
    //             NhanSuService chay lai luat trong giao dich truoc khi ghi. Doc chi doc
    //             vanhanh.ChuyenTau / LichDungGa / MacTauMau (A Ba, Sprint 1 doc thang -
    //             KE_HOACH §4.5), vanhanh.DoanTau + phuongtien.* (dong may cho R7).
    // Ghi       : chi ghi, khong kiem nghiep vu (BUS da kiem trong cung giao dich).
    //             Kip / nhan vien ghi theo PhienBan (ROWVERSION, migration V20261011_1500):
    //             tra ve so dong, 0 = nguoi khac vua sua / xoa.
    // Thoi gian : gio Viet Nam (quyet dinh 11/10 - khop gio chuyen tau cua vanhanh), ghi
    //             bang SYSDATETIME() cua may chu, khong dung mac dinh SYSUTCDATETIME().
    // =========================================================================
    public class NhanSuRepository
    {
        // Chuyen nam trong cua so [tu, den) hoac dang chay / co kip dang thuc hien
        private const string DieuKienCuaSo = @"
            (ct.GioVeDichKH >= @tu AND ct.GioXuatPhatKH < @den)
            OR ct.TrangThai IN ('DANG_CHAY', 'TRE_GIO')
            OR EXISTS (SELECT 1 FROM nhansu.PhanCongKipLai k0
                       WHERE k0.MaChuyenTau = ct.MaChuyenTau AND k0.TrangThai = 'DANG_THUC_HIEN')";

        public DuLieuNhanSu LayDuLieu(DateTime tu, DateTime den, DateTime bayGio, PhienGiaoDich? gd = null)
        {
            var thamSoCuaSo = () => new[] { new SqlParameter("@tu", tu), new SqlParameter("@den", den) };

            // --- Ga + hang bang lai ---
            var ga = PhienGiaoDich.TruyVan(gd, "SELECT MaGa, MaGaCode, TenGa FROM hatang.Ga ORDER BY LyTrinhKm, MaGa")
                .Rows.Cast<DataRow>()
                .Select(r => new GaNhanSu(Convert.ToInt32(r["MaGa"]), Chuoi(r, "MaGaCode"), Chuoi(r, "TenGa")))
                .ToList();
            var gaTheoMa = ga.ToDictionary(g => g.MaGa);

            var hangBang = LayHangBangLai(gd);

            // --- Nhan vien ---
            var nhanVien = PhienGiaoDich.TruyVan(gd, @"
                SELECT MaNhanVien, MaNVCode, HoTen, SoDienThoai, ChucDanh, HangBangLai, HanKhamSucKhoe,
                       DonViChuQuan, TrangThai, NgayTao, PhienBan
                FROM nhansu.NhanVien")
                .Rows.Cast<DataRow>().Select(DocNhanVien).ToList();

            // --- Chuyen tau trong cua so + dau may chinh (dong may) ---
            var bangChuyen = PhienGiaoDich.TruyVan(gd, $@"
                SELECT ct.MaChuyenTau, mt.SoHieuMacTau, mt.LoaiTau, ct.NgayXuatPhat, ct.GioXuatPhatKH, ct.GioVeDichKH,
                       ct.TrangThai, mt.MaGaDi, mt.MaGaDen, dm.SoHieuDauMay, ddm.MaDongCode
                FROM vanhanh.ChuyenTau ct
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau
                LEFT JOIN vanhanh.DoanTau dt ON dt.MaChuyenTau = ct.MaChuyenTau
                LEFT JOIN phuongtien.DauMay dm ON dm.MaDauMay = dt.MaDauMayChinh
                LEFT JOIN phuongtien.DongDauMay ddm ON ddm.MaDongDauMay = dm.MaDongDauMay
                WHERE {DieuKienCuaSo}
                ORDER BY ct.GioXuatPhatKH, ct.MaChuyenTau", thamSoCuaSo());

            // --- Lich dung ga cua cac chuyen do ---
            var lichDung = PhienGiaoDich.TruyVan(gd, $@"
                SELECT ld.MaChuyenTau, ld.ThuTuDung, ld.MaGa, ld.GioDenKeHoach, ld.GioDiKeHoach
                FROM vanhanh.LichDungGa ld
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = ld.MaChuyenTau
                WHERE {DieuKienCuaSo}
                ORDER BY ld.MaChuyenTau, ld.ThuTuDung", thamSoCuaSo())
                .Rows.Cast<DataRow>()
                .GroupBy(r => Convert.ToInt32(r["MaChuyenTau"]))
                .ToDictionary(g => g.Key, g => g.ToList());

            var chuyenTau = new List<ChuyenTauNhanSu>();
            foreach (DataRow r in bangChuyen.Rows)
            {
                int ma = Convert.ToInt32(r["MaChuyenTau"]);
                DateTime di = Convert.ToDateTime(r["GioXuatPhatKH"]);
                DateTime den2 = Convert.ToDateTime(r["GioVeDichKH"]);
                GaNhanSu gaDi = TimGa(gaTheoMa, Convert.ToInt32(r["MaGaDi"]));
                GaNhanSu gaDen = TimGa(gaTheoMa, Convert.ToInt32(r["MaGaDen"]));

                List<DiemDungNhanSu> diem;
                bool coLich = lichDung.TryGetValue(ma, out var dongLich) && dongLich.Count >= 2;
                if (coLich)
                {
                    diem = dongLich!.Select(d => new DiemDungNhanSu(
                        TimGa(gaTheoMa, Convert.ToInt32(d["MaGa"])),
                        Convert.ToInt32(d["ThuTuDung"]),
                        d["GioDenKeHoach"] != DBNull.Value ? Convert.ToDateTime(d["GioDenKeHoach"]) : Convert.ToDateTime(d["GioDiKeHoach"]),
                        d["GioDiKeHoach"] != DBNull.Value ? Convert.ToDateTime(d["GioDiKeHoach"]) : Convert.ToDateTime(d["GioDenKeHoach"])))
                        .ToList();
                }
                else
                {
                    // Chua co lich dung ga: 2 diem ga di / ga den theo gio cua chuyen
                    diem = new List<DiemDungNhanSu>
                    {
                        new(gaDi, 1, di, di),
                        new(gaDen, 2, den2, den2)
                    };
                }

                chuyenTau.Add(new ChuyenTauNhanSu
                {
                    MaChuyenTau = ma,
                    SoHieuMacTau = Chuoi(r, "SoHieuMacTau"),
                    LoaiTau = Chuoi(r, "LoaiTau"),
                    NgayXuatPhat = Convert.ToDateTime(r["NgayXuatPhat"]),
                    GioXuatPhatKH = di,
                    GioVeDichKH = den2,
                    TrangThai = Chuoi(r, "TrangThai"),
                    GaDi = gaDi,
                    GaDen = gaDen,
                    SoHieuDauMay = r["SoHieuDauMay"] == DBNull.Value ? null : Chuoi(r, "SoHieuDauMay"),
                    MaDongDauMay = r["MaDongCode"] == DBNull.Value ? null : Chuoi(r, "MaDongCode"),
                    CoLichDungGa = coLich,
                    LichDung = diem
                });
            }
            var chuyenTheoMa = chuyenTau.ToDictionary(c => c.MaChuyenTau);

            // --- Kip cua cac chuyen trong cua so ---
            // Doc ngoai giao dich thi cac cau doc rieng le: chuyen vua vao cua so giua 2 cau (vd vua
            // chuyen sang DANG_CHAY) khong co trong danh sach chuyen -> bo qua kip do, lan nap sau se co.
            var phanCong = PhienGiaoDich.TruyVan(gd, $@"
                SELECT k.MaPhanCong, k.MaChuyenTau, k.MaLaiTau, k.MaPhuLai, k.MaTruongTau,
                       k.MaGaNhanBan, k.MaGaBanGiao, k.TrangThai, k.PhienBan
                FROM nhansu.PhanCongKipLai k
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = k.MaChuyenTau
                WHERE {DieuKienCuaSo}", thamSoCuaSo())
                .Rows.Cast<DataRow>()
                .Where(r => chuyenTheoMa.ContainsKey(Convert.ToInt32(r["MaChuyenTau"])))
                .Select(r => new PhanCongKipHienThi
                {
                    MaPhanCong = Convert.ToInt32(r["MaPhanCong"]),
                    MaChuyenTau = Convert.ToInt32(r["MaChuyenTau"]),
                    MaLaiTau = Convert.ToInt32(r["MaLaiTau"]),
                    MaPhuLai = Convert.ToInt32(r["MaPhuLai"]),
                    MaTruongTau = Convert.ToInt32(r["MaTruongTau"]),
                    MaGaNhanBan = Convert.ToInt32(r["MaGaNhanBan"]),
                    MaGaBanGiao = Convert.ToInt32(r["MaGaBanGiao"]),
                    TrangThai = Chuoi(r, "TrangThai"),
                    PhienBan = (byte[])r["PhienBan"],
                    Chuyen = chuyenTheoMa[Convert.ToInt32(r["MaChuyenTau"])]
                })
                .ToList();

            // --- Moi phieu kiem tra (so kiem tra + lich su trong ho so), kem chuyen cua phieu ---
            var kiemTra = PhienGiaoDich.TruyVan(gd, @"
                SELECT ks.MaKiemTra, ks.MaNhanVien, ks.MaChuyenTau, ks.NongDoConMgL, ks.SoGioNghiTruocCa,
                       ks.ThoiDiemKiemTra, mt.SoHieuMacTau, ct.NgayXuatPhat
                FROM nhansu.KiemTraSucKhoe ks
                JOIN vanhanh.ChuyenTau ct ON ct.MaChuyenTau = ks.MaChuyenTau
                JOIN vanhanh.MacTauMau mt ON mt.MaMacTau = ct.MaMacTau")
                .Rows.Cast<DataRow>()
                .Select(r => new KiemTraLenBanHienThi
                {
                    MaKiemTra = Convert.ToInt32(r["MaKiemTra"]),
                    MaNhanVien = Convert.ToInt32(r["MaNhanVien"]),
                    MaChuyenTau = Convert.ToInt32(r["MaChuyenTau"]),
                    NongDoConMgL = Convert.ToDecimal(r["NongDoConMgL"]),
                    SoGioNghiTruocCa = Convert.ToDecimal(r["SoGioNghiTruocCa"]),
                    ThoiDiemKiemTra = Convert.ToDateTime(r["ThoiDiemKiemTra"]),
                    SoHieuMacTau = Chuoi(r, "SoHieuMacTau"),
                    NgayXuatPhatChuyen = Convert.ToDateTime(r["NgayXuatPhat"])
                })
                .ToList();

            var dl = new DuLieuNhanSu(nhanVien, phanCong, kiemTra, chuyenTau, ga, hangBang, bayGio);
            foreach (var kt in kiemTra) kt.GanNhanVien(dl.TimNhanVien(kt.MaNhanVien));
            return dl;
        }

        // Hang bang lai hop le = ma dong dau may (D19E, D13E ...)
        public List<string> LayHangBangLai(PhienGiaoDich? gd = null)
            => PhienGiaoDich.TruyVan(gd, "SELECT MaDongCode FROM phuongtien.DongDauMay ORDER BY MaDongCode")
               .Rows.Cast<DataRow>().Select(r => Chuoi(r, "MaDongCode")).ToList();

        // Gio hien tai cua may chu CSDL (gio Viet Nam) - moc "bay gio" khi Service kiem luat
        public DateTime LayGioMayChu(PhienGiaoDich? gd = null)
            => Convert.ToDateTime(PhienGiaoDich.LayGiaTri(gd, "SELECT SYSDATETIME()"));

        // =====================================================================
        // NHAN VIEN
        // =====================================================================

        // Ma da cap cua mot tien to. Khoa UPDLOCK + HOLDLOCK (khoa ca khoang LIKE) toi het giao
        // dich: 2 nguoi them cung tien to cung luc thi nguoi sau cho, khong cap trung so.
        public List<string> LayMaTheoTienTo(string tienTo, PhienGiaoDich gd)
            => PhienGiaoDich.TruyVan(gd, @"
                SELECT MaNVCode FROM nhansu.NhanVien WITH (UPDLOCK, HOLDLOCK)
                WHERE MaNVCode LIKE @mau",
                new SqlParameter("@mau", SqlDbType.VarChar, 20) { Value = tienTo + "[_]%" })
               .Rows.Cast<DataRow>().Select(r => Chuoi(r, "MaNVCode")).ToList();

        public int ThemNhanVien(NhanVienHienThi nv, PhienGiaoDich gd)
            => Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd, @"
                INSERT INTO nhansu.NhanVien (MaNVCode, HoTen, SoDienThoai, ChucDanh, HangBangLai, HanKhamSucKhoe,
                                             DonViChuQuan, TrangThai, NgayTao)
                OUTPUT INSERTED.MaNhanVien
                VALUES (@ma, @ten, @sdt, @cd, @bang, @han, @dv, @tt, SYSDATETIME())",
                ThamSoNhanVien(nv)));

        public int CapNhatNhanVien(NhanVienHienThi nv, byte[] phienBan, PhienGiaoDich gd)
        {
            var thamSo = ThamSoNhanVien(nv).ToList();
            thamSo.Add(new SqlParameter("@id", nv.MaNhanVien));
            thamSo.Add(new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });
            return PhienGiaoDich.ThucThi(gd, @"
                UPDATE nhansu.NhanVien
                SET MaNVCode = @ma, HoTen = @ten, SoDienThoai = @sdt, ChucDanh = @cd, HangBangLai = @bang,
                    HanKhamSucKhoe = @han, DonViChuQuan = @dv, TrangThai = @tt
                WHERE MaNhanVien = @id AND PhienBan = @pb",
                thamSo.ToArray());
        }

        // Trang thai di theo kip (R13) - goi trong cung giao dich nhan ban / ban giao
        public void DatTrangThaiNhanVien(int maNhanVien, string trangThai, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd,
                "UPDATE nhansu.NhanVien SET TrangThai = @tt WHERE MaNhanVien = @id",
                new SqlParameter("@tt", SqlDbType.VarChar, 20) { Value = trangThai },
                new SqlParameter("@id", maNhanVien));

        // =====================================================================
        // KIP LAI
        // =====================================================================

        // Bang co trigger nen khong dung OUTPUT INSERTED (loi 334) -> SCOPE_IDENTITY()
        public int ThemKip(PhanCongKipHienThi kip, PhienGiaoDich gd)
            => Convert.ToInt32(PhienGiaoDich.LayGiaTri(gd, @"
                INSERT INTO nhansu.PhanCongKipLai (MaChuyenTau, MaLaiTau, MaPhuLai, MaTruongTau, MaGaNhanBan, MaGaBanGiao, TrangThai)
                VALUES (@ct, @lt, @pl, @tt, @nhan, @giao, 'DA_PHAN_CONG');
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                ThamSoKip(kip)));

        // Chi sua kip con "Da phan cong" va dung phien ban da mo
        public int CapNhatKip(PhanCongKipHienThi kip, byte[] phienBan, PhienGiaoDich gd)
        {
            var thamSo = ThamSoKip(kip).ToList();
            thamSo.Add(new SqlParameter("@id", kip.MaPhanCong));
            thamSo.Add(new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });
            return PhienGiaoDich.ThucThi(gd, @"
                UPDATE nhansu.PhanCongKipLai
                SET MaLaiTau = @lt, MaPhuLai = @pl, MaTruongTau = @tt, MaGaNhanBan = @nhan, MaGaBanGiao = @giao
                WHERE MaPhanCong = @id AND PhienBan = @pb AND TrangThai = 'DA_PHAN_CONG' AND MaChuyenTau = @ct",
                thamSo.ToArray());
        }

        public int XoaKip(int maPhanCong, byte[] phienBan, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                DELETE FROM nhansu.PhanCongKipLai
                WHERE MaPhanCong = @id AND PhienBan = @pb AND TrangThai = 'DA_PHAN_CONG'",
                new SqlParameter("@id", maPhanCong),
                new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });

        public int DoiTrangThaiKip(int maPhanCong, byte[] phienBan, string tuTrangThai, string sangTrangThai, PhienGiaoDich gd)
            => PhienGiaoDich.ThucThi(gd, @"
                UPDATE nhansu.PhanCongKipLai SET TrangThai = @moi
                WHERE MaPhanCong = @id AND PhienBan = @pb AND TrangThai = @cu",
                new SqlParameter("@moi", SqlDbType.VarChar, 20) { Value = sangTrangThai },
                new SqlParameter("@cu", SqlDbType.VarChar, 20) { Value = tuTrangThai },
                new SqlParameter("@id", maPhanCong),
                new SqlParameter("@pb", SqlDbType.Timestamp) { Value = phienBan });

        // =====================================================================
        // KIEM TRA LEN BAN
        // =====================================================================

        public (int MaKiemTra, DateTime ThoiDiem) ThemKiemTra(int maNhanVien, int maChuyenTau, decimal nongDoCon,
                                                             decimal soGioNghi, PhienGiaoDich gd)
        {
            var bang = PhienGiaoDich.TruyVan(gd, @"
                INSERT INTO nhansu.KiemTraSucKhoe (MaNhanVien, MaChuyenTau, NongDoConMgL, SoGioNghiTruocCa, ThoiDiemKiemTra)
                OUTPUT INSERTED.MaKiemTra, INSERTED.ThoiDiemKiemTra
                VALUES (@nv, @ct, @con, @nghi, SYSDATETIME())",
                new SqlParameter("@nv", maNhanVien),
                new SqlParameter("@ct", maChuyenTau),
                new SqlParameter("@con", SqlDbType.Decimal) { Precision = 3, Scale = 2, Value = nongDoCon },
                new SqlParameter("@nghi", SqlDbType.Decimal) { Precision = 4, Scale = 1, Value = soGioNghi });
            var r = bang.Rows[0];
            return (Convert.ToInt32(r["MaKiemTra"]), Convert.ToDateTime(r["ThoiDiemKiemTra"]));
        }

        // =====================================================================
        // TIEN ICH
        // =====================================================================

        private static NhanVienHienThi DocNhanVien(DataRow r) => new()
        {
            MaNhanVien = Convert.ToInt32(r["MaNhanVien"]),
            MaNVCode = Chuoi(r, "MaNVCode"),
            HoTen = Chuoi(r, "HoTen"),
            SoDienThoai = Chuoi(r, "SoDienThoai"),
            ChucDanh = Chuoi(r, "ChucDanh"),
            HangBangLai = r["HangBangLai"] == DBNull.Value ? null : Chuoi(r, "HangBangLai"),
            HanKhamSucKhoe = Convert.ToDateTime(r["HanKhamSucKhoe"]),
            DonViChuQuan = Chuoi(r, "DonViChuQuan"),
            TrangThai = Chuoi(r, "TrangThai"),
            NgayTao = Convert.ToDateTime(r["NgayTao"]),
            PhienBan = (byte[])r["PhienBan"]
        };

        private static SqlParameter[] ThamSoNhanVien(NhanVienHienThi nv) => new[]
        {
            new SqlParameter("@ma", SqlDbType.VarChar, 20) { Value = nv.MaNVCode },
            new SqlParameter("@ten", SqlDbType.NVarChar, 100) { Value = nv.HoTen },
            new SqlParameter("@sdt", SqlDbType.VarChar, 20) { Value = nv.SoDienThoai },
            new SqlParameter("@cd", SqlDbType.NVarChar, 50) { Value = nv.ChucDanh },
            new SqlParameter("@bang", SqlDbType.VarChar, 10) { Value = (object?)nv.HangBangLai ?? DBNull.Value },
            new SqlParameter("@han", SqlDbType.Date) { Value = nv.HanKhamSucKhoe.Date },
            new SqlParameter("@dv", SqlDbType.NVarChar, 100) { Value = nv.DonViChuQuan },
            new SqlParameter("@tt", SqlDbType.VarChar, 20) { Value = nv.TrangThai }
        };

        private static SqlParameter[] ThamSoKip(PhanCongKipHienThi kip) => new[]
        {
            new SqlParameter("@ct", kip.MaChuyenTau),
            new SqlParameter("@lt", kip.MaLaiTau),
            new SqlParameter("@pl", kip.MaPhuLai),
            new SqlParameter("@tt", kip.MaTruongTau),
            new SqlParameter("@nhan", kip.MaGaNhanBan),
            new SqlParameter("@giao", kip.MaGaBanGiao)
        };

        private static GaNhanSu TimGa(Dictionary<int, GaNhanSu> gaTheoMa, int maGa)
            => gaTheoMa.TryGetValue(maGa, out var g) ? g : new GaNhanSu(maGa, $"#{maGa}", $"Ga #{maGa}");

        private static string Chuoi(DataRow r, string cot) => r[cot] == DBNull.Value ? "" : r[cot].ToString() ?? "";
    }
}
