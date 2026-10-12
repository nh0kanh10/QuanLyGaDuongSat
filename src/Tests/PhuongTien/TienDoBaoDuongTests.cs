using BUS.Services;

namespace Tests.PhuongTien
{
    // Tien do bao duong dau may theo CHU KY tu lan gan nhat (PhuongTienService.TinhTienDoBaoDuong):
    // R1 moi 50.000 km tinh tu lan R1 hoac R2 gan nhat, R2 moi 150.000 km tu lan R2 gan nhat,
    // chua co lich su thi tinh tu 0 km; >= 90% sap toi han, >= 100% qua han.
    public class TienDoBaoDuongTests
    {
        [Fact]
        public void ChuaCoLichSu_ConXa_BinhThuong()
        {
            var td = PhuongTienService.TinhTienDoBaoDuong(30_000m, null, null);

            Assert.Equal("R1", td.Cap);
            Assert.Equal(60.0m, td.TyLePhanTram);
            Assert.Equal("BINH_THUONG", td.MucCanhBao);
            Assert.False(td.CoLichSu);
            Assert.Equal(20_000m, td.ConLaiKm);
        }

        [Fact]
        public void Tu90PhanTram_SapToiHan()
        {
            var td = PhuongTienService.TinhTienDoBaoDuong(46_000m, null, null);

            Assert.Equal("R1", td.Cap);
            Assert.Equal(92.0m, td.TyLePhanTram);
            Assert.Equal("SAP_TOI", td.MucCanhBao);
        }

        [Fact]
        public void D13E701_ChuaCoLichSu_210000km_QuaHanR2()
        {
            // Vi du cua man hinh Bao tri (CLAUDE.md §5.5): qua han R2 60.000 km
            var td = PhuongTienService.TinhTienDoBaoDuong(210_000m, null, null);

            Assert.Equal("R2", td.Cap);
            Assert.Equal("QUA_HAN", td.MucCanhBao);
            Assert.Equal(-60_000m, td.ConLaiKm);
        }

        [Fact]
        public void R1TinhTuLanR2GanNhat()
        {
            // Lan R2 o 120.000 km cung la moc R1: 170.000 km -> R1 du 50.000 km = qua han
            var td = PhuongTienService.TinhTienDoBaoDuong(170_000m, 120_000m, 120_000m);

            Assert.Equal("R1", td.Cap);
            Assert.Equal(100.0m, td.TyLePhanTram);
            Assert.Equal("QUA_HAN", td.MucCanhBao);
            Assert.True(td.CoLichSu);
            Assert.Equal(170_000m, td.MocKm);
        }

        [Fact]
        public void VuaLamR1_KhongConCanhBao()
        {
            var td = PhuongTienService.TinhTienDoBaoDuong(210_000m, 200_000m, 120_000m);

            Assert.Equal("BINH_THUONG", td.MucCanhBao);
            Assert.Equal("R2", td.Cap);                 // cung muc: lay cap co ty le cao hon (R2 60% > R1 20%)
            Assert.Equal(60.0m, td.TyLePhanTram);
        }

        [Fact]
        public void CungQuaHan_UuTienR2()
        {
            var td = PhuongTienService.TinhTienDoBaoDuong(400_000m, 200_000m, 200_000m);
            Assert.Equal("R2", td.Cap);
            Assert.Equal("QUA_HAN", td.MucCanhBao);
        }

        [Fact]
        public void KmNhoHonLanTruoc_KhongAm()
        {
            var td = PhuongTienService.TinhTienDoBaoDuong(100_000m, 120_000m, 120_000m);
            Assert.True(td.DaChayKm >= 0);
            Assert.Equal("BINH_THUONG", td.MucCanhBao);
        }
    }
}
