using BUS.Services;
using DTO.BaoTri;

namespace Tests.BaoTri
{
    // Bo luat Bao tri (QuyTacBaoTri) - ham thuan, khong cham CSDL.
    public class QuyTacBaoTriTests
    {
        // ---------------------------------------------------------------- KHAM XE
        [Theory]
        [InlineData(4.8, 0.20, true, true, true)]     // dung nguong van dat
        [InlineData(4.7, 0.10, true, true, false)]
        [InlineData(5.0, 0.21, true, true, false)]
        [InlineData(5.0, 0.10, false, true, false)]
        [InlineData(5.0, 0.10, true, false, false)]
        public void DuDieuKienXuatBen_TrungCotPersisted(double apLuc, double sutAp, bool nuoc, bool veSinh, bool ketQua)
            => Assert.Equal(ketQua, QuyTacBaoTri.DuDieuKienXuatBen((decimal)apLuc, (decimal)sutAp, nuoc, veSinh));

        [Fact]
        public void KhamXe_KhongDat_BatBuocGhiChu()
        {
            var bb = new KhamXeDTO { MaDoanTau = 1, MaNguoiKham = 2, ApLucHamBar = 4.5m, DoSutApBarPhut = 0.1m, DaCapNuoc = true, DaXaVeSinh = true };
            Assert.Contains(QuyTacBaoTri.KiemTraKhamXe(bb), l => l.Truong == "GhiChu");

            bb.GhiChuKyThuat = "Áp lực yếu";
            Assert.Empty(QuyTacBaoTri.KiemTraKhamXe(bb));
        }

        [Fact]
        public void KhamXe_SoLeVaGioiHanCot()
        {
            var bb = new KhamXeDTO { MaDoanTau = 1, MaNguoiKham = 2, ApLucHamBar = 4.85m, DoSutApBarPhut = 0.125m, DaCapNuoc = true, DaXaVeSinh = true, ChiPhi = 1.5m };
            var loi = QuyTacBaoTri.KiemTraKhamXe(bb).Select(l => l.Truong).ToList();
            Assert.Contains("ApLuc", loi);
            Assert.Contains("SutAp", loi);
            Assert.Contains("ChiPhi", loi);
            Assert.DoesNotContain("GhiChu", loi);   // so do sai thi chua xet ghi chu

            bb = new KhamXeDTO { MaDoanTau = 0, MaNguoiKham = 0, ApLucHamBar = 10.1m, DoSutApBarPhut = -0.1m };
            loi = QuyTacBaoTri.KiemTraKhamXe(bb).Select(l => l.Truong).ToList();
            Assert.Equal(new[] { "DoanTau", "NguoiKham", "ApLuc", "SutAp" }, loi);
        }

        [Fact]
        public void TomTatLoi_LietKeHangMucKhongDat()
        {
            var bb = new KhamXeDTO { ApLucHamBar = 4.5m, DoSutApBarPhut = 0.28m, DaCapNuoc = true, DaXaVeSinh = false };
            Assert.Equal("Áp lực hãm 4.5 bar · Sụt áp 0.28 bar/phút · Chưa xả vệ sinh", QuyTacBaoTri.TomTatLoi(bb));
        }

        // ---------------------------------------------------------------- NHIEN LIEU
        [Theory]
        [InlineData(2800, 580, 688, 70.17)]     // phieu goc D19E-901
        [InlineData(3950, 510, 650, 119.16)]    // phieu goc D19E-902
        [InlineData(1, 1, 8000, 1.25)]          // 1.25 dung, khong lam tron
        [InlineData(1, 1, 16000, 0.63)]         // 0.625 -> 0.63 (nua tren len, khop ROUND cua SQL)
        public void Sfc_CongThucVaLamTron(double lit, double tan, double km, double sfc)
            => Assert.Equal((decimal)sfc, QuyTacBaoTri.TinhSfc((decimal)lit, (decimal)tan, (decimal)km));

        [Theory]
        [InlineData(89.9, "DAT")]
        [InlineData(90.0, "SAT")]
        [InlineData(100.0, "SAT")]
        [InlineData(100.1, "VUOT")]
        public void XepMuc_TheoPhanTramDinhMuc(double tyLe, string muc) => Assert.Equal(muc, QuyTacBaoTri.XepMuc((decimal)tyLe));

        [Fact]
        public void TinhLaiSuatTieuHao_BoQuaGiaTriGiaoDienGuiLen()
        {
            var nl = new CapNhienLieuDTO { SoLitTraNap = 3950m, TrongLuongKeoTan = 510m, CuLyChayKm = 650m, DonGiaVndLit = 21000m,
                                           SuatTieuHaoSFC = 1m, CanhBaoVuotMuc = false, ChiPhi = 5m };
            QuyTacBaoTri.TinhLaiSuatTieuHao(nl, 90m);
            Assert.Equal(119.16m, nl.SuatTieuHaoSFC);
            Assert.True(nl.CanhBaoVuotMuc);
            Assert.Equal(82_950_000m, nl.ChiPhi);
        }

        [Fact]
        public void CapDau_VuotBonVaSfcQuaLon()
        {
            var dm = new DauMayBaoTriDTO { MaDauMay = 7, MaDongCode = "D13E", DungTichBonDauLit = 3200, DinhMucSFC = 100m };
            var nl = new CapNhienLieuDTO { MaDauMay = 7, NoiCapDau = "Trạm dầu Vinh", MaNguoiCap = 2, SoLitTraNap = 3300m,
                                           TrongLuongKeoTan = 380m, CuLyChayKm = 520m, DonGiaVndLit = 21000m };
            Assert.Contains(QuyTacBaoTri.KiemTraCapDau(nl, dm), l => l.Truong == "SoLit" && l.ThongDiep.Contains("dung tích bồn"));

            nl.SoLitTraNap = 3000m; nl.TrongLuongKeoTan = 1m; nl.CuLyChayKm = 1m;
            Assert.Contains(QuyTacBaoTri.KiemTraCapDau(nl, dm), l => l.Truong == "CuLy" && l.ThongDiep.Contains("vượt giới hạn"));

            nl.TrongLuongKeoTan = 380m; nl.CuLyChayKm = 520m;
            Assert.Empty(QuyTacBaoTri.KiemTraCapDau(nl, dm));

            nl.DonGiaVndLit = 0m; nl.MaNguoiCap = null; nl.NoiCapDau = " ";
            var loi = QuyTacBaoTri.KiemTraCapDau(nl, null).Select(l => l.Truong).ToList();
            Assert.Equal(new[] { "DauMay", "NoiCap", "NguoiCap", "DonGia" }, loi);
        }

        // ---------------------------------------------------------------- BAO DUONG
        static readonly DateTime BayGio = new(2026, 10, 24, 10, 0, 0);

        static BaoDuongDTO BD(string cap, decimal km, int soNgayTruoc, int ma, string loai = "DAU_MAY")
            => new() { MaBaoDuong = ma, LoaiPhuongTien = loai, MaDauMay = loai == "DAU_MAY" ? 1 : null, MaToaXe = loai == "TOA_XE" ? 1 : null,
                       CapBaoDuong = cap, SoKmTaiThoiDiem = km, ThoiDiemHoanThanh = BayGio.AddDays(-soNgayTruoc), MaNguoiThucHien = 2 };

        [Fact]
        public void DauMay_ChuaCoLichSu_TinhTu0Km()
        {
            var td = QuyTacBaoTri.TinhTienDo("DAU_MAY", 210_000m, Array.Empty<BaoDuongDTO>(), BayGio);
            Assert.Equal("QUA_HAN", td[0].TrangThai);          // R1 420%
            Assert.Equal("QUA_HAN", td[1].TrangThai);          // R2 140%
            Assert.Equal(140m, td[1].TyLe);
            Assert.Equal("R2", QuyTacBaoTri.CapCanChuY(td)!.Cap);   // cung qua han -> uu tien cap cao
        }

        [Fact]
        public void DauMay_R2LamMoiCaR1()
        {
            var ls = new[] { BD("R1", 120_000m, 40, 1), BD("R2", 140_000m, 10, 2) };
            var td = QuyTacBaoTri.TinhTienDo("DAU_MAY", 148_500m, ls, BayGio);
            Assert.Equal(2, td[0].LanGanNhat!.MaBaoDuong);   // R1 tinh tu lan R2 gan nhat (moi hon R1)
            Assert.Equal(8_500m, td[0].DaDung);
            Assert.Equal(17m, td[0].TyLe);
            Assert.Equal(290_000m, td[1].MocKeTiep);
        }

        [Fact]
        public void DauMay_R1KhongLamMoiR2()
        {
            var ls = new[] { BD("R1", 120_000m, 40, 1) };
            var td = QuyTacBaoTri.TinhTienDo("DAU_MAY", 148_500m, ls, BayGio);
            Assert.Equal(57m, td[0].TyLe);                    // R1 tu 120,000
            Assert.Equal(99m, td[1].TyLe);                    // R2 van tu 0 km
            Assert.Equal("SAP_TOI", td[1].TrangThai);
            Assert.Equal("R2", QuyTacBaoTri.CapCanChuY(td)!.Cap);
        }

        [Fact]
        public void ToaXe_ChuaCoLichSu_ChuaCoMoc_CoLichSu_TinhTheoNgay()
        {
            var td = QuyTacBaoTri.TinhTienDo("TOA_XE", 0m, Array.Empty<BaoDuongDTO>(), BayGio);
            Assert.All(td, t => Assert.Equal("CHUA_CO", t.TrangThai));

            td = QuyTacBaoTri.TinhTienDo("TOA_XE", 0m, new[] { BD("D1", 50_000m, 170, 1, "TOA_XE") }, BayGio);
            Assert.Equal("SAP_TOI", td[0].TrangThai);          // 170 / 180 ngay = 94.4%
            Assert.Equal(10m, td[0].ConLai);
            Assert.Equal("CHUA_CO", td[1].TrangThai);          // D1 khong lam moi D2
        }

        [Fact]
        public void TienDo_KhopPhuongTienService()
        {
            // Hai man hinh (Phuong tien, Bao tri) phai ra cung ket luan cho cung du lieu
            var ls = new[] { BD("R1", 120_000m, 40, 1) };
            var bt = QuyTacBaoTri.CapCanChuY(QuyTacBaoTri.TinhTienDo("DAU_MAY", 148_500m, ls, BayGio))!;
            var pt = PhuongTienService.TinhTienDoBaoDuong(148_500m, 120_000m, null);
            Assert.Equal(pt.Cap, bt.Cap);
            Assert.Equal(pt.MucCanhBao, bt.TrangThai);
            Assert.Equal(pt.TyLePhanTram, bt.TyLe);
        }

        [Fact]
        public void BaoDuong_CapTheoLoai_KmKhongLui_KhongVuotKmTichLuy()
        {
            var bd = BD("D1", 100_000m, 0, 0);
            Assert.Contains(QuyTacBaoTri.KiemTraBaoDuong(bd, 150_000m, Array.Empty<BaoDuongDTO>()), l => l.Truong == "Cap");

            // Sua phieu cu (MaBaoDuong > 0): km khong vuot dong ho, khong lui so voi lan truoc
            bd = BD("R2", 160_000m, 0, 9);
            Assert.Contains(QuyTacBaoTri.KiemTraBaoDuong(bd, 150_000m, Array.Empty<BaoDuongDTO>()), l => l.Truong == "SoKm" && l.ThongDiep.Contains("km tích lũy"));

            bd = BD("R1", 110_000m, 0, 9);
            Assert.Contains(QuyTacBaoTri.KiemTraBaoDuong(bd, 150_000m, new[] { BD("R1", 120_000m, 30, 1) }),
                            l => l.Truong == "SoKm" && l.ThongDiep.Contains("lần bảo dưỡng trước"));

            bd = BD("R1", 130_000m, 0, 9);
            Assert.Empty(QuyTacBaoTri.KiemTraBaoDuong(bd, 150_000m, new[] { BD("R1", 120_000m, 30, 1) }));

            bd.MaNguoiThucHien = null; bd.ChiPhi = -1m;
            var loi = QuyTacBaoTri.KiemTraBaoDuong(bd, 150_000m, Array.Empty<BaoDuongDTO>()).Select(l => l.Truong).ToList();
            Assert.Equal(new[] { "NguoiThucHien", "ChiPhi" }, loi);
        }

        // Quyet dinh 26/10: phieu MOI cua dau may ghi dung km dong ho hien tai (vd khong ghi R2 o 1 km khi
        // dong ho 210.000 km); toa xe khong co dong ho thi van go tay
        [Theory]
        [InlineData(1, true)]
        [InlineData(209_999.9, true)]
        [InlineData(210_000.1, true)]
        [InlineData(210_000, false)]
        public void BaoDuong_PhieuMoiDauMay_PhaiDungKmDongHo(double km, bool coLoi)
        {
            var bd = BD("R2", (decimal)km, 0, 0);
            var loi = QuyTacBaoTri.KiemTraBaoDuong(bd, 210_000m, Array.Empty<BaoDuongDTO>());
            Assert.Equal(coLoi, loi.Any(l => l.Truong == "SoKm"));
            if (coLoi) Assert.Contains(loi, l => l.ThongDiep.Contains("Phương tiện"));
        }

        [Fact]
        public void BaoDuong_PhieuMoiDauMay_DungDongHoNhungLuiSoVoiLanTruoc_VanBaoLoi()
        {
            var bd = BD("R1", 100_000m, 0, 0);
            Assert.Contains(QuyTacBaoTri.KiemTraBaoDuong(bd, 100_000m, new[] { BD("R1", 120_000m, 30, 1) }),
                            l => l.Truong == "SoKm" && l.ThongDiep.Contains("lần bảo dưỡng trước"));
        }

        [Fact]
        public void BaoDuong_PhieuMoiToaXe_KmGoTay()
        {
            var bd = BD("D1", 45_000m, 0, 0, "TOA_XE");
            Assert.Empty(QuyTacBaoTri.KiemTraBaoDuong(bd, null, new[] { BD("D1", 40_000m, 200, 1, "TOA_XE") }));
        }

        [Fact]
        public void BaoDuong_KhoaPhuongTienSai_BaoLoi()
        {
            var bd = new BaoDuongDTO { LoaiPhuongTien = "DAU_MAY", MaDauMay = 1, MaToaXe = 5, CapBaoDuong = "R1", MaNguoiThucHien = 2 };
            Assert.Contains(QuyTacBaoTri.KiemTraBaoDuong(bd, 1000m, Array.Empty<BaoDuongDTO>()), l => l.Truong == "PhuongTien");
        }
    }
}
