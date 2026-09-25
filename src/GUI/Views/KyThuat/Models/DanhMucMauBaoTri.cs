namespace GUI.Views.KyThuat.Models
{
    // =========================================================================
    // DU LIEU MAU TINH cho phan he Bao tri ky thuat & Canh bao an toan
    // (giai doan DUNG GIAO DIEN - chua co Repository cho schema baotri).
    //
    // Bam dung seed data trong tailieu\script_tao_csdl_v2.sql:
    //   - 6 doan tau (vanhanh.DoanTau) theo thu tu INSERT => MaDoanTau 1..6
    //   - 2 bien ban kham xe goc: SE1 dat, SE3 ap luc ham 4.5 bar khong dat
    // Ngay gio tinh tuong doi theo hom nay giong @NgayNay / @HomQua / @NgayMai
    // trong script. Cac ban ghi danh dau "bo sung mau" khong co trong seed.
    // =========================================================================
    public static class DanhMucMauBaoTri
    {
        // Moc thoi gian co dinh luc nap du lieu mau lan dau (de gio kham khong troi moi lan nap lai)
        private static readonly DateTime LucNap = DateTime.Now;
        private static readonly DateTime HomNay = LucNap.Date;
        private static readonly DateTime HomQua = HomNay.AddDays(-1);
        private static readonly DateTime NgayMai = HomNay.AddDays(1);

        // ---------------------------------------------------------------------
        // Doan tau (vanhanh.DoanTau + ChuyenTau + MacTauMau)
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<DoanTauKhamXe> DoanTau = new[]
        {
            new DoanTauKhamXe(1, 1, "SE1",  "TAU_NHANH", HomNay,  HomNay.AddHours(6),
                              "Hà Nội", "Sài Gòn", "D19E-901", "D19E-903", 14, 296.5m, 580.0m, true,  "DANG_CHAY"),
            new DoanTauKhamXe(2, 2, "SE2",  "TAU_NHANH", HomQua,  HomQua.AddHours(19),
                              "Sài Gòn", "Hà Nội", "D19E-902", null,       12, 256.5m, 510.0m, true,  "DANG_CHAY"),
            new DoanTauKhamXe(3, 3, "SE3",  "TAU_NHANH", NgayMai, NgayMai.AddHours(19),
                              "Hà Nội", "Đà Nẵng", "D19E-901", null,       10, 216.5m, 430.0m, false, "DA_LEN_LICH"),
            new DoanTauKhamXe(4, 4, "SE4",  "TAU_NHANH", HomNay,  HomQua.AddHours(19),
                              "Sài Gòn", "Hà Nội", "D19E-902", null,       13, 276.5m, 540.0m, true,  "DANG_CHAY"),
            new DoanTauKhamXe(5, 5, "SE19", "TAU_NHANH", HomNay,  HomNay.AddMinutes(1190),
                              "Hà Nội", "Đà Nẵng", "D19E-901", null,       11, 236.5m, 460.0m, true,  "SAN_SANG"),
            new DoanTauKhamXe(6, 6, "NA1",  "TAU_CHO",   HomNay,  HomNay.AddMinutes(1335),
                              "Hà Nội", "Vinh",    "D13E-701", null,        9, 196.5m, 380.0m, true,  "DA_LEN_LICH")
        };

        // ---------------------------------------------------------------------
        // Nguoi kham (phanquyen.TaiKhoan). Seed chi co nv_dung (MaTaiKhoan 2) la
        // nhan vien kham xe (NV_004, DonViChuQuan "Trạm Giáp Bát"); 2 tai khoan
        // con lai la bo sung mau.
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<TaiKhoanMau> NguoiKham = new[]
        {
            new TaiKhoanMau(2, "nv_dung", "Phạm Minh Dũng", "Trạm Giáp Bát"),
            new TaiKhoanMau(4, "nv_hung", "Trần Quốc Hưng", "Trạm Sài Gòn"),   // bo sung mau
            new TaiKhoanMau(5, "nv_son",  "Lê Thanh Sơn",   "Trạm Đà Nẵng")    // bo sung mau
        };

        // ---------------------------------------------------------------------
        // Dau may (phuongtien.DauMay theo thu tu INSERT => MaDauMay 1..4) kem
        // dung tich bon dau cua dong may - lay tu danh muc mau Phuong tien.
        // ---------------------------------------------------------------------
        public static readonly IReadOnlyList<DauMayCapDau> DauMay = DanhMucMauPhuongTien.DauMayMau
            .Select((d, i) => new DauMayCapDau(i + 1, d.SoHieu, d.MaDong, d.DonVi, d.TrangThai,
                                               DanhMucMauPhuongTien.TimDongDauMay(d.MaDong)?.DungTichBonDauLit ?? 0))
            .ToList();

        // Nguoi cap dau (phanquyen.TaiKhoan). Seed ghi MaNguoiCap = 1 (admin);
        // 2 nhan vien tram dau la bo sung mau.
        public static readonly IReadOnlyList<TaiKhoanMau> NguoiCapDau = new[]
        {
            new TaiKhoanMau(1, "admin",  "Quản trị viên", "Điều hành"),
            new TaiKhoanMau(6, "nv_tai", "Đỗ Văn Tài",    "Trạm dầu Giáp Bát"),   // bo sung mau
            new TaiKhoanMau(7, "nv_lan", "Ngô Thị Lan",   "Trạm dầu Sài Gòn")     // bo sung mau
        };

        public static readonly IReadOnlyList<string> NoiCapDau = new[]
        {
            "Trạm dầu Giáp Bát", "Trạm dầu Vinh", "Trạm dầu Đà Nẵng", "Trạm dầu Sài Gòn"
        };

        public static DauMayCapDau? TimDauMay(int maDauMay)
            => DauMay.FirstOrDefault(d => d.MaDauMay == maDauMay);

        public static TaiKhoanMau? TimNguoiCapDau(int? maTaiKhoan)
            => NguoiCapDau.FirstOrDefault(n => n.MaTaiKhoan == maTaiKhoan);

        public static DoanTauKhamXe? TimDoanTau(int maDoanTau)
            => DoanTau.FirstOrDefault(d => d.MaDoanTau == maDoanTau);

        // ---------------------------------------------------------------------
        // Phuong tien de bao duong: 4 dau may (SoKmTichLuy trung seed) + 6 toa xe
        // cua doi toa trong seed (phuongtien.ToaXe, MaToaXe 1..6 theo thu tu INSERT).
        // ---------------------------------------------------------------------
        private static readonly Dictionary<string, decimal> SoKmDauMaySeed = new()
        {
            ["D19E-901"] = 125_000m, ["D19E-902"] = 148_500m, ["D19E-903"] = 92_000m, ["D13E-701"] = 210_000m
        };

        private static readonly (string SoHieu, string MaCode)[] ToaXeSeed =
        {
            ("NC-101", "NC"), ("NML-201", "NML"), ("BN-301", "BN"), ("AN-401", "AN"), ("M-601", "M"), ("P-701", "P")
        };

        public static readonly IReadOnlyList<PhuongTienBaoDuong> PhuongTien =
            DauMay.Select(d => new PhuongTienBaoDuong("DAU_MAY", d.MaDauMay, d.SoHieuDauMay,
                                                      $"{d.MaDongCode} · {d.DonViQuanLy.Replace("Xí nghiệp Đầu máy", "XN")}",
                                                      SoKmDauMaySeed.GetValueOrDefault(d.SoHieuDauMay)))
                  .Concat(ToaXeSeed.Select((t, i) => new PhuongTienBaoDuong("TOA_XE", i + 1, t.SoHieu,
                                                      DanhMucMauPhuongTien.TimChungLoai(t.MaCode).TenNgan, 0m)))
                  .ToList();

        public static PhuongTienBaoDuong? TimPhuongTien(string khoa)
            => PhuongTien.FirstOrDefault(p => p.Khoa == khoa);

        // Nguoi thuc hien bao duong: dung chung danh sach nhan vien ky thuat
        public static IReadOnlyList<TaiKhoanMau> NguoiBaoDuong => NguoiKham;

        // ---------------------------------------------------------------------
        // Nhat ky bao duong (baotri.NhatKyBaoDuong). Dong 1 trung seed (D19E-902
        // R1 tai 120,000 km); con lai bo sung mau. D13E-701 co tinh KHONG co lich
        // su => qua han R2 (khop phan tich Q4_4). M-601, P-701 chua co lich su.
        // ---------------------------------------------------------------------
        public static List<BaoDuongHienThi> LayNhatKyBaoDuong()
        {
            return new List<BaoDuongHienThi>
            {
                TaoLanBaoDuong(1, "DAU_MAY:2", "R1", 120_000m, 2, HomNay.AddDays(-40).AddHours(16),
                               "Bảo dưỡng định kỳ cấp R1 hoàn thành đạt chuẩn."),                          // seed
                TaoLanBaoDuong(2, "DAU_MAY:1", "R1",  80_000m, 2, HomNay.AddDays(-150).AddHours(15),
                               "Thay lọc dầu bôi trơn, kiểm tra hệ thống hãm và cát chống trượt."),
                TaoLanBaoDuong(3, "DAU_MAY:3", "R1",  50_500m, 5, HomNay.AddDays(-300).AddHours(10),
                               "Kiểm tra bộ truyền động, bôi trơn ổ trục trước mùa khai thác đèo Hải Vân."),
                TaoLanBaoDuong(4, "DAU_MAY:2", "R1",  70_000m, 2, HomNay.AddDays(-400).AddHours(9),
                               "R1 định kỳ, thay dầu động cơ."),
                TaoLanBaoDuong(5, "TOA_XE:1",  "D1",  38_200m, 2, HomNay.AddDays(-30).AddHours(14),
                               "Kiểm tra giá chuyển hướng, thay má phanh guốc."),
                TaoLanBaoDuong(6, "TOA_XE:2",  "D2",  61_500m, 4, HomNay.AddDays(-100).AddHours(11),
                               "Bảo dưỡng cấp D2: điều hòa, hệ thống điện toa, toàn bộ ghế."),
                TaoLanBaoDuong(7, "TOA_XE:3",  "D1",  45_200m, 2, HomNay.AddDays(-75).AddHours(8),
                               "Kiểm tra móc nối, đầu đấm; vệ sinh két nước."),
                TaoLanBaoDuong(8, "TOA_XE:4",  "D1",  52_800m, 4, HomNay.AddDays(-170).AddHours(13),
                               "D1 định kỳ. Lưu ý cửa khoang 3 kẹt, đã căn chỉnh.")
            };
        }

        private static BaoDuongHienThi TaoLanBaoDuong(int ma, string khoaPhuongTien, string cap, decimal soKm,
                                                      int maNguoi, DateTime thoiDiem, string ghiChu)
        {
            var bd = new BaoDuongHienThi
            {
                MaBaoDuong = ma,
                CapBaoDuong = cap,
                SoKmTaiThoiDiem = soKm,
                MaNguoiThucHien = maNguoi,
                TenNguoiThucHien = TimNguoiKham(maNguoi)?.HoTenHienThi ?? "",
                ThoiDiemHoanThanh = thoiDiem,
                GhiChuKyThuat = ghiChu
            };

            var pt = TimPhuongTien(khoaPhuongTien);
            if (pt != null) bd.GanPhuongTien(pt);
            return bd;
        }

        public static TaiKhoanMau? TimNguoiKham(int maTaiKhoan)
            => NguoiKham.FirstOrDefault(n => n.MaTaiKhoan == maTaiKhoan);

        // ---------------------------------------------------------------------
        // Bien ban kham xe (baotri.KhamXeKyThuat).
        // Moi lan goi tra ve ban moi - dong vai tro "doc CSDL" cua trang.
        // ---------------------------------------------------------------------
        public static List<KhamXeHienThi> LayBienBanKhamXe()
        {
            return new List<KhamXeHienThi>
            {
                // Seed: SE1 dat chuan
                TaoBienBan(1, 1, 2, 5.0m, 0.12m, true, true,
                           "Đạt chuẩn an toàn kỹ thuật xuất bến.", HomNay.AddHours(6).AddMinutes(-50)),
                // Seed: SE3 ap luc ham yeu
                TaoBienBan(2, 3, 2, 4.5m, 0.28m, true, true,
                           "Áp lực hãm yếu không đạt chuẩn 4.8 bar.", HomNay.AddHours(9)),
                // Bo sung mau
                TaoBienBan(3, 2, 4, 5.1m, 0.15m, true, true,
                           "Đạt. Đã thay gioăng ống hãm nối toa 5 – 6.", HomQua.AddHours(18).AddMinutes(5)),
                TaoBienBan(4, 4, 4, 4.9m, 0.20m, true, true,
                           "Đạt ngưỡng tối thiểu. Theo dõi van hãm toa 7 ở ga dọc đường.", HomQua.AddHours(18).AddMinutes(15)),
                TaoBienBan(5, 5, 2, 5.2m, 0.10m, true, false,
                           "Két vệ sinh toa 3, toa 5 chưa xả – chờ tổ vệ sinh depot.", HomNay.AddHours(17).AddMinutes(30))
            };
        }

        // ---------------------------------------------------------------------
        // Nhat ky cap nhien lieu (baotri.NhatKyCapNhienLieu).
        // 2 dong dau trung seed (so lit / tan / km); SFC tinh lai theo cong thuc
        // cua giao dien (70.17 va 119.16) thay cho gia tri nhap tay 2.15 / 3.25
        // trong seed - co CanhBaoVuotMuc van giu dung (901 khong vuot, 902 vuot).
        // Cac dong con lai bo sung mau: D19E-902 tieu hao tang dan qua cac lan cap.
        // ---------------------------------------------------------------------
        public static List<CapNhienLieuHienThi> LayNhatKyCapDau()
        {
            return new List<CapNhienLieuHienThi>
            {
                TaoLanCapDau(1, 1, "Trạm dầu Giáp Bát", 2800m, 580m, 688m, 1, HomNay.AddHours(5).AddMinutes(20)),   // seed
                TaoLanCapDau(2, 2, "Trạm dầu Sài Gòn",  3950m, 510m, 650m, 1, HomQua.AddHours(18).AddMinutes(30)),  // seed
                TaoLanCapDau(3, 2, "Trạm dầu Vinh",     3300m, 540m, 700m, 6, HomNay.AddDays(-3).AddHours(14).AddMinutes(10)),
                TaoLanCapDau(4, 2, "Trạm dầu Giáp Bát", 3000m, 530m, 690m, 6, HomNay.AddDays(-5).AddHours(5).AddMinutes(40)),
                TaoLanCapDau(5, 1, "Trạm dầu Sài Gòn",  3100m, 560m, 720m, 7, HomNay.AddDays(-2).AddHours(18).AddMinutes(20)),
                TaoLanCapDau(6, 3, "Trạm dầu Đà Nẵng",   780m, 580m, 180m, 1, HomQua.AddHours(21)),
                TaoLanCapDau(7, 4, "Trạm dầu Giáp Bát", 1900m, 380m, 520m, 6, HomQua.AddHours(21).AddMinutes(40)),
                TaoLanCapDau(8, 4, "Trạm dầu Vinh",     1850m, 380m, 530m, 6, HomNay.AddDays(-4).AddHours(9))
            };
        }

        private static CapNhienLieuHienThi TaoLanCapDau(int ma, int maDauMay, string noiCap, decimal soLit,
                                                        decimal tanKeo, decimal cuLy, int maNguoiCap, DateTime thoiDiem)
        {
            var nl = new CapNhienLieuHienThi
            {
                MaNhatKyDau = ma,
                NoiCapDau = noiCap,
                SoLitTraNap = soLit,
                TrongLuongKeoTan = tanKeo,
                CuLyChayKm = cuLy,
                MaNguoiCap = maNguoiCap,
                TenNguoiCap = TimNguoiCapDau(maNguoiCap)?.HoTenHienThi ?? "",
                ThoiDiemBomDau = thoiDiem > LucNap ? LucNap.AddMinutes(-20 * ma) : thoiDiem
            };

            var dm = TimDauMay(maDauMay);
            if (dm != null) nl.GanDauMay(dm);
            nl.TinhLaiSuatTieuHao();
            return nl;
        }

        private static KhamXeHienThi TaoBienBan(int ma, int maDoanTau, int maNguoiKham,
                                                decimal apLuc, decimal sutAp, bool nuoc, bool veSinh,
                                                string ghiChu, DateTime thoiDiem)
        {
            var bb = new KhamXeHienThi
            {
                MaKhamXe = ma,
                MaNguoiKham = maNguoiKham,
                ApLucHamBar = apLuc,
                DoSutApBarPhut = sutAp,
                DaCapNuoc = nuoc,
                DaXaVeSinh = veSinh,
                GhiChuKyThuat = ghiChu,
                // Khong de thoi diem kham nam o tuong lai khi mo app luc sang som
                ThoiDiemKham = thoiDiem > LucNap ? LucNap.AddMinutes(-15 * ma) : thoiDiem,
                TenNguoiKham = TimNguoiKham(maNguoiKham)?.HoTenHienThi ?? ""
            };

            var dt = TimDoanTau(maDoanTau);
            if (dt != null) bb.GanDoanTau(dt);
            return bb;
        }
    }
}
