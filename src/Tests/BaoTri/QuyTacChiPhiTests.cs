using BUS.Services;
using DTO.BaoTri;

namespace Tests.BaoTri
{
    // QuyTacChiPhi (tab 5 Chi phi & hieu qua, 26/10): phan bo chi phi theo mac tau, doanh thu theo ngay chay,
    // khuyen nghi theo bien loi nhuan, bat bien "tong cac mac + chua phan bo = thuc chi".
    public class QuyTacChiPhiTests
    {
        static readonly DateTime BayGio = new(2026, 10, 26, 10, 0, 0);
        static readonly DateTime TuNgay7 = new(2026, 10, 20);

        static MacTauBaoTriDTO Mac(int ma, string soHieu, string loai = "TAU_NHANH", bool khaiThac = true)
            => new() { MaMacTau = ma, SoHieuMacTau = soHieu, LoaiTau = loai, TenGaDi = "Hà Nội", TenGaDen = "Sài Gòn", DangKhaiThac = khaiThac };

        static DoanTauBaoTriDTO Doan(int ma, string mac, int dauMay, int? day = null)
            => new() { MaDoanTau = ma, MaChuyenTau = ma, SoHieuMacTau = mac, MaDauMayChinh = dauMay, MaDauMayDay = day, TrangThaiChuyen = "DA_LEN_LICH" };

        static DauMayBaoTriDTO DM(int ma, decimal km)
            => new() { MaDauMay = ma, SoHieuDauMay = $"D19E-90{ma}", MaDongCode = "D19E", TrangThai = "SAN_SANG", SoKmTichLuy = km, DinhMucSFC = 90m };

        static DoanhThuChuyenDTO Chuyen(int ma, string mac, DateTime ngay, int soGhe, decimal thucThu, decimal hoan = 0m)
            => new() { MaChuyenTau = ma, SoHieuMacTau = mac, NgayXuatPhat = ngay, GioXuatPhatKH = ngay.AddHours(6),
                       SoGhe = soGhe, SoVeHieuLuc = soGhe / 2, ThucThu = thucThu, TienHoan = hoan };

        static DuLieuBaoTri TaoDuLieu() => new()
        {
            BayGio = BayGio,
            MacTau =
            {
                Mac(1, "SE1"), Mac(2, "SE2"), Mac(3, "SE3"), Mac(4, "NA1", "TAU_CHO"),
                Mac(5, "HBN1", "TAU_HANG"), Mac(6, "SE20"), Mac(7, "SE99", khaiThac: false)
            },
            DoanTau = { Doan(1, "SE1", 1, day: 3), Doan(2, "SE2", 2), Doan(3, "SE3", 1), Doan(4, "NA1", 4) },
            // Dau may 2: 60.000 km, chua co lich su -> qua han R1 (du phong 8 trieu cho SE2)
            // Dau may 5: du phong, khong keo doan tau nao
            DauMay = { DM(1, 10_000m), DM(2, 60_000m), DM(3, 10_000m), DM(4, 10_000m), DM(5, 80_000m) },
            ToaXe = { new ToaXeBaoTriDTO { MaToaXe = 7, SoHieuToaXe = "NML-207", MaChungLoaiCode = "NML", TrangThai = "SAN_SANG" } },
            KhamXe =
            {
                new KhamXeDTO { MaKhamXe = 1, MaDoanTau = 1, ApLucHamBar = 5.0m, DoSutApBarPhut = 0.1m, DaCapNuoc = true, DaXaVeSinh = true,
                                ChiPhi = 300_000m, ThoiDiemKham = new DateTime(2026, 10, 25, 8, 0, 0) },
                new KhamXeDTO { MaKhamXe = 2, MaDoanTau = 3, ApLucHamBar = 4.5m, DoSutApBarPhut = 0.1m, DaCapNuoc = true, DaXaVeSinh = true,
                                ChiPhi = 3_500_000m, ThoiDiemKham = new DateTime(2026, 10, 26, 8, 0, 0) },
                new KhamXeDTO { MaKhamXe = 3, MaDoanTau = 2, ApLucHamBar = 5.0m, DoSutApBarPhut = 0.1m, DaCapNuoc = true, DaXaVeSinh = true,
                                ChiPhi = 500_000m, ThoiDiemKham = TuNgay7.AddMinutes(-1) }       // ngoai ky
            },
            CapDau =
            {
                new CapNhienLieuDTO { MaNhatKyDau = 1, MaDauMay = 1, SoLitTraNap = 1, ChiPhi = 90_000_001m, ThoiDiemBomDau = new DateTime(2026, 10, 24) },
                new CapNhienLieuDTO { MaNhatKyDau = 2, MaDauMay = 2, SoLitTraNap = 1, ChiPhi = 60_000_000m, ThoiDiemBomDau = TuNgay7 },   // dung moc dau ky
                new CapNhienLieuDTO { MaNhatKyDau = 3, MaDauMay = 5, SoLitTraNap = 1, ChiPhi = 10_000_000m, ThoiDiemBomDau = new DateTime(2026, 10, 25) },
                new CapNhienLieuDTO { MaNhatKyDau = 4, MaDauMay = 3, SoLitTraNap = 1, ChiPhi = 16_000_000m, ThoiDiemBomDau = new DateTime(2026, 10, 25) },
                new CapNhienLieuDTO { MaNhatKyDau = 5, MaDauMay = 4, SoLitTraNap = 1, ChiPhi = 40_000_000m, ThoiDiemBomDau = new DateTime(2026, 10, 25) },
                new CapNhienLieuDTO { MaNhatKyDau = 6, MaDauMay = 2, SoLitTraNap = 1, ChiPhi = 1_000_000m, ThoiDiemBomDau = new DateTime(2026, 10, 19) }   // ngoai ky
            },
            BaoDuong =
            {
                new BaoDuongDTO { MaBaoDuong = 1, LoaiPhuongTien = "DAU_MAY", MaDauMay = 4, CapBaoDuong = "R2", SoKmTaiThoiDiem = 10_000m,
                                  ChiPhi = 25_000_000m, ThoiDiemHoanThanh = new DateTime(2026, 10, 26, 9, 0, 0) },
                new BaoDuongDTO { MaBaoDuong = 2, LoaiPhuongTien = "TOA_XE", MaToaXe = 7, CapBaoDuong = "D1", SoKmTaiThoiDiem = 40_000m,
                                  ChiPhi = 5_000_000m, ThoiDiemHoanThanh = new DateTime(2026, 10, 23) }
            },
            DoanhThuChuyen =
            {
                Chuyen(1, "SE1", new DateTime(2026, 10, 26), 600, 500_000_000m),
                Chuyen(2, "SE2", new DateTime(2026, 10, 25), 500, 90_000_000m, hoan: 2_000_000m),
                Chuyen(3, "SE3", new DateTime(2026, 10, 27), 300, 70_000_000m),      // ngay mai: chua chay
                Chuyen(4, "NA1", new DateTime(2026, 10, 26), 400, 10_000_000m),
                Chuyen(5, "HBN1", new DateTime(2026, 10, 26), 0, 0m),
                Chuyen(6, "SE1", new DateTime(2026, 10, 19), 600, 999_000_000m)      // ngoai ky
            },
            VanDon = { new VanDonDoanhThuDTO(1, new DateTime(2026, 10, 25), 18_500_000m), new VanDonDoanhThuDTO(2, new DateTime(2026, 9, 1), 1m) }
        };

        static HieuQuaMacTauDTO M(KetQuaChiPhiDTO kq, string mac) => kq.MacTau.Single(m => m.MacTau.SoHieuMacTau == mac);

        [Fact]
        public void Ky7Ngay_TinhCaHomNay()
        {
            Assert.Equal(TuNgay7, QuyTacChiPhi.TuNgay(BayGio, 7));
            Assert.Equal(BayGio.Date, QuyTacChiPhi.TuNgay(BayGio, 1));
        }

        [Fact]
        public void TongCacMac_CongChuaPhanBo_BangThucChi()
        {
            var kq = QuyTacChiPhi.TongHop(TaoDuLieu(), 7);
            Assert.Equal(249_800_001m, kq.ThucChi);
            Assert.Equal(kq.ThucChi, kq.DaPhanBo + kq.TongChuaPhanBo);
        }

        [Fact]
        public void DauMayKeoHaiMac_ChiaTronDong_PhanDuChoPhanCuoi()
        {
            var kq = QuyTacChiPhi.TongHop(TaoDuLieu(), 7);
            var se1 = M(kq, "SE1").DaChi.Single(c => c.MaPhieu == "NL-0001");
            var se3 = M(kq, "SE3").DaChi.Single(c => c.MaPhieu == "NL-0001");
            Assert.Equal(45_000_001m, se1.SoTien);
            Assert.Equal(45_000_000m, se3.SoTien);
            Assert.True(se1.DuocChia && se1.SoPhan == 1 && se1.TongSoPhan == 2 && se1.SoTienGoc == 90_000_001m);
        }

        [Fact]
        public void KhamXe_TinhThang_DauMayDay_TinhChoMacDuocDay()
        {
            var kq = QuyTacChiPhi.TongHop(TaoDuLieu(), 7);
            var se1 = M(kq, "SE1");
            Assert.Contains(se1.DaChi, c => c.MaPhieu == "BB-0001" && c.SoTien == 300_000m && !c.DuocChia);
            Assert.Contains(se1.DaChi, c => c.MaPhieu == "NL-0004" && c.SoTien == 16_000_000m);   // may day D19E-903
            Assert.Equal(61_300_001m, se1.ChiPhiDaChi);
        }

        [Fact]
        public void DauMayDuPhong_VaBaoDuongToa_ChuaPhanBo()
        {
            var kq = QuyTacChiPhi.TongHop(TaoDuLieu(), 7);
            Assert.Equal(new[] { "BD-0002", "NL-0003" }, kq.ChuaPhanBo.Select(c => c.MaPhieu).OrderBy(x => x));
            Assert.Equal(15_000_000m, kq.TongChuaPhanBo);
        }

        [Fact]
        public void MocKy_PhieuTruoc0GioNgayDau_KhongTinh_DungMoc_Tinh()
        {
            var kq = QuyTacChiPhi.TongHop(TaoDuLieu(), 7);
            var se2 = M(kq, "SE2");
            Assert.Contains(se2.DaChi, c => c.MaPhieu == "NL-0002");
            Assert.DoesNotContain(se2.DaChi, c => c.MaPhieu is "NL-0006" or "BB-0003");
        }

        [Fact]
        public void DoanhThu_TheoNgayChay_ThucThuTruHoan()
        {
            var kq = QuyTacChiPhi.TongHop(TaoDuLieu(), 7);
            Assert.Equal(500_000_000m, M(kq, "SE1").DoanhThu);              // chuyen 19/10 ngoai ky
            Assert.Equal(88_000_000m, M(kq, "SE2").DoanhThu);
            Assert.Equal(0, M(kq, "SE3").SoChuyen);                          // chuyen ngay mai chua tinh
            Assert.Equal(1, kq.SoVanDon);
            Assert.Equal(18_500_000m, kq.CuocHangHoa);
        }

        [Fact]
        public void DuPhong_DauMayQuaHan_ChiaChoMacDauMayKeo()
        {
            var kq = QuyTacChiPhi.TongHop(TaoDuLieu(), 7);
            var dp = Assert.Single(M(kq, "SE2").DuPhong);
            Assert.Equal("R1", dp.Cap);
            Assert.Equal("QUA_HAN", dp.TrangThai);
            Assert.Equal(QuyTacBaoTri.ChiPhiThamKhao("R1"), dp.SoTienDuKien);
            Assert.Empty(M(kq, "NA1").DuPhong);                              // vua ghi R2
        }

        [Fact]
        public void KhuyenNghi_TheoTungMac()
        {
            var kq = QuyTacChiPhi.TongHop(TaoDuLieu(), 7);
            Assert.Equal("MO_RONG", M(kq, "SE1").MaKhuyenNghi);
            Assert.Equal("DUY_TRI", M(kq, "SE2").MaKhuyenNghi);             // (88 - 68) / 88 = 22,7%
            Assert.Equal(22.7m, M(kq, "SE2").BienLoiNhuan);
            Assert.Equal("THU_HEP", M(kq, "NA1").MaKhuyenNghi);
            Assert.Equal("CHUA_DU", M(kq, "SE3").MaKhuyenNghi);             // co chi phi nhung chua chay
            Assert.Contains("Chưa có chuyến", M(kq, "SE3").LyDoChuaDu);
            Assert.Equal("CHUA_DU", M(kq, "HBN1").MaKhuyenNghi);
            Assert.Contains("Tàu hàng", M(kq, "HBN1").LyDoChuaDu);
            Assert.Equal("CHUA_DU", M(kq, "SE20").MaKhuyenNghi);
            Assert.DoesNotContain(kq.MacTau, m => m.MacTau.SoHieuMacTau == "SE99");   // ngung khai thac, khong co du lieu
        }

        [Fact]
        public void ThuTu_ThuHepTruoc_ChuaDuSauCung()
        {
            var kq = QuyTacChiPhi.TongHop(TaoDuLieu(), 7);
            Assert.Equal(new[] { "NA1", "SE2", "SE1", "SE3", "HBN1", "SE20" }, kq.MacTau.Select(m => m.MacTau.SoHieuMacTau));
        }

        [Theory]
        [InlineData(100, 90.1, "THU_HEP")]     // 9,9%
        [InlineData(100, 90, "DUY_TRI")]       // dung 10%
        [InlineData(100, 70, "DUY_TRI")]       // dung 30%
        [InlineData(100, 69.9, "MO_RONG")]     // 30,1%
        [InlineData(100, 150, "THU_HEP")]      // lo
        public void KhuyenNghi_NguongBienLoiNhuan(double doanhThuTrieu, double chiPhiTrieu, string ma)
        {
            var h = new HieuQuaMacTauDTO
            {
                MacTau = Mac(1, "SE1"),
                ChuyenTrongKy = { Chuyen(1, "SE1", BayGio.Date, 100, (decimal)doanhThuTrieu * 1_000_000m) },
                DaChi = { new KhoanChiPhiDTO("NHIEN_LIEU", "NL-0001", "", (decimal)chiPhiTrieu * 1_000_000m, 0m, 1, 1, BayGio) }
            };
            Assert.Equal(ma, QuyTacChiPhi.KhuyenNghi(h).Ma);
        }

        [Fact]
        public void CoGheMaKhongThuDongNao_ThuHep_BienNull_KhongCoToaBanVe_ChuaDu()
        {
            var khongBan = new HieuQuaMacTauDTO
            {
                MacTau = Mac(1, "SE1"),
                ChuyenTrongKy = { Chuyen(1, "SE1", BayGio.Date, 100, 0m) },
                DaChi = { new KhoanChiPhiDTO("KHAM_XE", "BB-0001", "", 300_000m, 300_000m, 1, 1, BayGio) }
            };
            Assert.Null(khongBan.BienLoiNhuan);
            Assert.Equal("THU_HEP", QuyTacChiPhi.KhuyenNghi(khongBan).Ma);

            var khongToa = new HieuQuaMacTauDTO
            {
                MacTau = Mac(1, "SE1"),
                ChuyenTrongKy = { Chuyen(1, "SE1", BayGio.Date, 0, 0m) },
                DaChi = { new KhoanChiPhiDTO("KHAM_XE", "BB-0001", "", 300_000m, 300_000m, 1, 1, BayGio) }
            };
            Assert.Equal("CHUA_DU", QuyTacChiPhi.KhuyenNghi(khongToa).Ma);
        }

        [Fact]
        public void ChiaTronDong_BaPhan_TongDung()
        {
            var kq = QuyTacChiPhi.ChiaTronDong(100m, new List<(string, int)> { ("SE1", 1), ("SE19", 1), ("SE3", 1) }, 3);
            Assert.Equal(new[] { 33m, 33m, 34m }, kq.Select(x => x.Tien));
            Assert.Equal(100m, kq.Sum(x => x.Tien));
        }
    }
}
