using BUS.AI;
using DTO.AI;

namespace Tests.AI
{
    // Luat cham diem AI tu van ve (MCDM, Sprint 3).
    public class QuyTacTuVanVeTests
    {
        static readonly double[] W = QuyTacTuVanVe.TrongSoMacDinh;

        // Bang vi du SRS (Nhom1.docx §3.2): Ha Noi -> Da Nang 791 km, w = 0.35 / 0.25 / 0.20 / 0.20.
        // Dong ghe cung SE7 trong SRS ghi 3,050 nhung 0.35·1 + 0.25·10 + 0.2·1 + 0.2·1 = 3,25 -> SRS can sua.
        [Theory]
        [InlineData(9.5, 6.0, 9.5, 9.5, 8.625)]
        [InlineData(8.0, 8.0, 8.5, 7.5, 8.000)]
        [InlineData(4.0, 9.5, 3.0, 4.0, 5.175)]
        [InlineData(1.0, 10.0, 1.0, 1.0, 3.250)]
        public void TinhDiem_TaiTaoBangViDuSRS(double c1, double c2, double c3, double c4, double mongDoi)
        {
            Assert.Equal(mongDoi, QuyTacTuVanVe.TinhDiem(new[] { c1, c2, c3, c4 }, W), 3);
        }

        // Diem tung tieu chi cua bang ham khop bang vi du (chang dai, tau dem, khong nguoi gia / tre em)
        [Theory]
        [InlineData("AN", 1, 9.5, 9.5, 9.5)]
        [InlineData("BN", 2, 8.0, 8.5, 7.5)]
        [InlineData("NML", null, 4.0, 3.0, 4.0)]
        [InlineData("NC", null, 1.0, 1.0, 1.0)]
        public void DiemTieuChi_ChangDaiTauDem_KhopSRS(string loai, int? tang, double tienNghi, double khungGio, double sucKhoe)
        {
            var den = new DateTime(2026, 11, 10, 11, 30, 0);
            Assert.Equal(tienNghi, QuyTacTuVanVe.DiemTienNghi(loai, tang, 791m));
            Assert.Equal(khungGio, QuyTacTuVanVe.DiemKhungGio(loai, tang, true, KhungGioUuTien.KhongUuTien, den));
            Assert.Equal(sucKhoe, QuyTacTuVanVe.DiemSucKhoe(loai, tang, 791m, false, false));
        }

        [Theory]
        [InlineData(100, QuyTacTuVanVe.NhomCuLy.Ngan)]
        [InlineData(249.99, QuyTacTuVanVe.NhomCuLy.Ngan)]
        [InlineData(250, QuyTacTuVanVe.NhomCuLy.TrungBinh)]
        [InlineData(649.99, QuyTacTuVanVe.NhomCuLy.TrungBinh)]
        [InlineData(650, QuyTacTuVanVe.NhomCuLy.Dai)]
        [InlineData(1726.2, QuyTacTuVanVe.NhomCuLy.Dai)]
        public void NguongCuLy_250_650(double km, QuyTacTuVanVe.NhomCuLy nhom)
            => Assert.Equal(nhom, QuyTacTuVanVe.XepNhomCuLy((decimal)km));

        [Fact]
        public void ChangNgan_GheNgoiDiemTienNghiCaoHonGiuong()
        {
            Assert.True(QuyTacTuVanVe.DiemTienNghi("NML", null, 120m) > QuyTacTuVanVe.DiemTienNghi("AN", 1, 120m));
            Assert.Equal(new[] { "NML", "NC" }, QuyTacTuVanVe.LoaiChoUuTien(120m));
            Assert.Equal(new[] { "AN", "BN" }, QuyTacTuVanVe.LoaiChoUuTien(791m));
        }

        [Fact]
        public void ChangDai_GiuongDiemTienNghiCaoHonGhe()
            => Assert.True(QuyTacTuVanVe.DiemTienNghi("BN", 3, 791m) > QuyTacTuVanVe.DiemTienNghi("NML", null, 791m));

        [Theory]
        [InlineData(500000, 500000, 10.0)]
        [InlineData(1000000, 500000, 5.0)]
        [InlineData(625000, 500000, 8.0)]
        public void DiemNganSach_TiLeGiaReNhat(double gia, double re, double mongDoi)
            => Assert.Equal(mongDoi, QuyTacTuVanVe.DiemNganSach((decimal)gia, (decimal)re, null), 2);

        [Fact]
        public void DiemNganSach_VuotNganSach_BiTru()
        {
            Assert.Equal(3.0, QuyTacTuVanVe.DiemNganSach(625000m, 500000m, 600000m), 2);   // 8 - 5
            Assert.Equal(0.0, QuyTacTuVanVe.DiemNganSach(5000000m, 500000m, 600000m), 2);  // khong am
            Assert.Equal(5.0, QuyTacTuVanVe.DiemNganSach(0m, 500000m, null));               // gia loi -> trung tinh
        }

        [Fact]
        public void NguoiCaoTuoi_UuTienGiuongTang1()
        {
            double t1 = QuyTacTuVanVe.DiemSucKhoe("BN", 1, 791m, true, false);
            double t2 = QuyTacTuVanVe.DiemSucKhoe("BN", 2, 791m, true, false);
            double t3 = QuyTacTuVanVe.DiemSucKhoe("BN", 3, 791m, true, false);
            Assert.True(t1 > t2 && t2 > t3, $"{t1} {t2} {t3}");
            Assert.True(QuyTacTuVanVe.DiemSucKhoe("BN", 1, 791m, true, false) > QuyTacTuVanVe.DiemSucKhoe("BN", 1, 791m, false, false));
        }

        [Fact]
        public void TreEm_TruGiuongTang3()
            => Assert.True(QuyTacTuVanVe.DiemSucKhoe("BN", 3, 791m, false, true) < QuyTacTuVanVe.DiemSucKhoe("BN", 3, 791m, false, false));

        [Fact]
        public void TranhDiDem_TauDemBiHaDiem_NguTrenTau_TauNgayBiHaDiem()
        {
            var den = new DateTime(2026, 11, 10, 9, 0, 0);
            Assert.True(QuyTacTuVanVe.DiemKhungGio("AN", 1, true, KhungGioUuTien.TranhDiDem, den)
                        < QuyTacTuVanVe.DiemKhungGio("NML", null, false, KhungGioUuTien.TranhDiDem, den));
            Assert.True(QuyTacTuVanVe.DiemKhungGio("AN", 1, false, KhungGioUuTien.NguTrenTau, den)
                        < QuyTacTuVanVe.DiemKhungGio("AN", 1, true, KhungGioUuTien.NguTrenTau, den));
        }

        [Fact]
        public void DenGaGiuaDem_BiTru()
        {
            var ngay = new DateTime(2026, 11, 10, 14, 0, 0);
            var dem = new DateTime(2026, 11, 10, 2, 30, 0);
            Assert.Equal(QuyTacTuVanVe.DiemKhungGio("NML", null, false, KhungGioUuTien.KhongUuTien, ngay) - 1.5,
                         QuyTacTuVanVe.DiemKhungGio("NML", null, false, KhungGioUuTien.KhongUuTien, dem), 2);
        }

        [Fact]
        public void SoGioBanDem_TauDemVaTauNgay()
        {
            var di = new DateTime(2026, 11, 9, 19, 0, 0);
            Assert.Equal(8.0, QuyTacTuVanVe.SoGioBanDem(di, new DateTime(2026, 11, 10, 11, 30, 0)), 3);   // 22h -> 06h
            Assert.True(QuyTacTuVanVe.LaTauDem(di, new DateTime(2026, 11, 10, 11, 30, 0)));
            Assert.Equal(0.0, QuyTacTuVanVe.SoGioBanDem(new DateTime(2026, 11, 10, 6, 0, 0), new DateTime(2026, 11, 10, 18, 0, 0)));
            Assert.False(QuyTacTuVanVe.LaTauDem(new DateTime(2026, 11, 10, 20, 0, 0), new DateTime(2026, 11, 10, 23, 30, 0)));   // 1.5 h
            Assert.Equal(16.0, QuyTacTuVanVe.SoGioBanDem(new DateTime(2026, 11, 9, 19, 0, 0), new DateTime(2026, 11, 11, 7, 0, 0)), 3); // 2 dem
        }

        [Fact]
        public void TrongSo_MacDinhVaTheoHoSo()
        {
            var macDinh = QuyTacTuVanVe.TrongSo(new YeuCauTuVanVe());
            Assert.Equal(1.0, macDinh.Sum(), 6);
            Assert.Equal(0.35, macDinh[0], 6);

            var tietKiem = QuyTacTuVanVe.TrongSo(new YeuCauTuVanVe { MucNganSach = MucNganSach.TietKiem, LaSinhVien = true });
            Assert.True(tietKiem[QuyTacTuVanVe.TieuChiNganSach] > macDinh[QuyTacTuVanVe.TieuChiNganSach]);
            Assert.Equal(1.0, tietKiem.Sum(), 6);

            var caoTuoi = QuyTacTuVanVe.TrongSo(new YeuCauTuVanVe { CoNguoiCaoTuoi = true, MucNganSach = MucNganSach.CaoCap });
            Assert.True(caoTuoi[QuyTacTuVanVe.TieuChiSucKhoe] > macDinh[QuyTacTuVanVe.TieuChiSucKhoe]);
            Assert.True(caoTuoi[QuyTacTuVanVe.TieuChiNganSach] < macDinh[QuyTacTuVanVe.TieuChiNganSach]);
        }

        [Fact]
        public void TrongSo_NguoiDungChinh_DuocChuanHoa_TatCa0VeMacDinh()
        {
            var w = QuyTacTuVanVe.TrongSo(new YeuCauTuVanVe { TrongSo = new[] { 2.0, 1.0, 1.0, 0.0 } });
            Assert.Equal(new[] { 0.5, 0.25, 0.25, 0.0 }, w);
            Assert.Equal(QuyTacTuVanVe.TrongSoMacDinh, QuyTacTuVanVe.TrongSo(new YeuCauTuVanVe { TrongSo = new double[4] }));
        }

        [Fact]
        public void TinhDiem_KhongPhuThuocTongTrongSo()
            => Assert.Equal(8.625, QuyTacTuVanVe.TinhDiem(new[] { 9.5, 6.0, 9.5, 9.5 }, new[] { 35.0, 25, 20, 20 }), 3);

        [Fact]
        public void ChonGhe_LienNhauGanGiuaToa()
        {
            var trong = Enumerable.Range(1, 28).Select(i => (100 + i, i)).ToList();
            var ghe = QuyTacTuVanVe.ChonGhe(trong, 2, 28)!;
            Assert.Equal(2, ghe.Count);
            Assert.Equal(1, ghe[1].SoGhe - ghe[0].SoGhe);
            Assert.InRange(ghe.Average(g => g.SoGhe), 13, 16);
        }

        [Fact]
        public void ChonGhe_KhongDuCho_TraNull()
        {
            var trong = new List<(int, int)> { (1, 3), (2, 9) };
            Assert.Null(QuyTacTuVanVe.ChonGhe(trong, 3, 28));
            Assert.NotNull(QuyTacTuVanVe.ChonGhe(trong, 2, 28));   // khong lien nhau van chon duoc
        }
    }
}
