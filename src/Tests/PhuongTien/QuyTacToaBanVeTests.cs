using BUS.Services;
using DTO.PhuongTien;
using static Tests.PhuongTien.MauLapTau;

namespace Tests.PhuongTien
{
    // Dong bo toa ban ve theo doan tau (BUS/Services/QuyTacToaBanVe.cs, 13/11):
    // toa khach thu k <-> "Toa k"; khong lam hong ve con hieu luc; toa chi con ve lich su -> ngung ban.
    public class QuyTacToaBanVeTests
    {
        private const int ToaAN501 = 501;
        private int _maVe = 1;

        private static BoiCanhLapTauDTO BoiCanhCoAN()
        {
            var bc = BoiCanh();
            bc.ToaXe.Add(new ToaXeLapTauDTO
            {
                MaToaXe = ToaAN501, SoHieuToaXe = "AN-501", MaChungLoaiCode = "AN", TrangThai = "SAN_SANG",
                ChieuDaiM = 20m, SoTruc = 4, TuTrongTan = 40m, TaiTrongToiDaTan = 6m, SucChuaChuan = 28
            });
            return bc;
        }

        // Toa ban ve dang ban k = 1..n khop toa khach 101.. (NC, 80 cho, du ghe)
        private static void CoSan(BoiCanhLapTauDTO bc, int n)
        {
            for (int k = 1; k <= n; k++)
                bc.ToaBanVe.Add(new ToaBanVeDTO
                {
                    MaToaXeKhach = 1000 + k, ThuTuToa = k, NhanHieuToa = $"Toa {k} (NC)", LoaiToa = "NC",
                    SucChua = 80, SoGhe = 80, MaToaXe = Khach(k)
                });
        }

        private void Ve(BoiCanhLapTauDTO bc, int k, int ghe, int gaDi, int gaDen, string trangThai = "DA_DAT")
        {
            string Ma(int ga) => bc.HanhTrinh.First(d => d.MaGa == ga).MaGaCode;
            bc.VeTrenToa.Add(new VeTrenToaDTO
            {
                MaVe = _maVe, MaVeCode = $"TK_{_maVe++:000}", MaToaXeKhach = 1000 + k, SoGhe = ghe,
                MaGaDi = gaDi, MaGaDen = gaDen, MaGaDiCode = Ma(gaDi), MaGaDenCode = Ma(gaDen),
                TrangThai = trangThai, TenHanhKhach = "Nguyễn Văn A"
            });
        }

        private static List<ThaoTacToaBanVeDTO> Loai(KeHoachDongBoToaBanVeDTO ke, string loai)
            => ke.ThaoTac.Where(t => t.Loai == loai).ToList();

        // --- Mo ban moi ---

        [Fact]
        public void ChuyenChuaCoToaBanVe_MoBanDuToaKhach_DanhSoTu1()
        {
            var ke = QuyTacToaBanVe.LapKeHoach(BoiCanh(), PhuongAn(DM901, KhachSuot(10).ToArray()));

            var them = Loai(ke, LoaiThaoTacToaBanVe.Them);
            Assert.Equal(10, them.Count);
            Assert.Equal(Enumerable.Range(1, 10), them.Select(t => t.ThuTuToa));
            Assert.Equal(Enumerable.Range(1, 10).Select(Khach), them.Select(t => t.MaToaXe!.Value));
            Assert.All(them, t => Assert.Equal(80, t.SucChua));          // NC chua khai SucChuaChuan -> 80
            Assert.Equal("Toa 1 (NC)", them[0].NhanHieuToa);
            Assert.False(ke.CoXungDot);
            Assert.True(ke.CoThayDoi);
        }

        [Fact]
        public void ToaHang_KhongMoBan_ToaKhachSauToaHangVanDanhSoLienTuc()
        {
            var pa = PhuongAn(DM901, KhachSuot(3).Append(T(ToaHang201)).Append(T(Khach(4))).ToArray());
            var ke = QuyTacToaBanVe.LapKeHoach(BoiCanh(), pa);

            Assert.Equal(4, ke.SoToaKhachTrongDoan);
            Assert.Equal(new[] { Khach(1), Khach(2), Khach(3), Khach(4) },
                         Loai(ke, LoaiThaoTacToaBanVe.Them).Select(t => t.MaToaXe!.Value));
        }

        [Fact]
        public void SucChuaTheoChungLoai()
        {
            var bc = BoiCanhCoAN();
            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, T(Khach(1)), T(ToaAN501)));
            var an = Loai(ke, LoaiThaoTacToaBanVe.Them).Single(t => t.LoaiToa == "AN");
            Assert.Equal(28, an.SucChua);
            Assert.Equal(2, an.ThuTuToa);
        }

        // --- Khop san / thay toa that cung loai ---

        [Fact]
        public void DaKhop_KhongCanGhi()
        {
            var bc = BoiCanh();
            CoSan(bc, 5);
            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, KhachSuot(5).ToArray()));

            Assert.False(ke.CoThayDoi);
            Assert.False(ke.CoXungDot);
            Assert.Equal(5, ke.ThaoTac.Count(t => t.Loai == LoaiThaoTacToaBanVe.CapNhat && !t.CoGhi));
        }

        [Fact]
        public void ThayToaThatCungLoai_CoVe_GiuToaBanVe_ChiDoiMaToaXe()
        {
            var bc = BoiCanh();
            CoSan(bc, 5);
            Ve(bc, 3, 12, HNI, SGO);
            var pa = PhuongAn(DM901, T(Khach(1)), T(Khach(2)), T(Khach(9)), T(Khach(4)), T(Khach(5)));

            var ke = QuyTacToaBanVe.LapKeHoach(bc, pa);

            Assert.False(ke.CoXungDot);
            var toa3 = ke.ThaoTac.Single(t => t.ThuTuToa == 3);
            Assert.Equal(LoaiThaoTacToaBanVe.CapNhat, toa3.Loai);
            Assert.True(toa3.CoGhi);
            Assert.Equal(Khach(9), toa3.MaToaXe);
            Assert.Equal(1003, toa3.MaToaXeKhach);
            Assert.Empty(Loai(ke, LoaiThaoTacToaBanVe.Them));
        }

        [Fact]
        public void ThieuGhe_BoSung()
        {
            var bc = BoiCanh();
            CoSan(bc, 2);
            bc.ToaBanVe[1].SoGhe = 0;      // kieu 2 toa SE3 cua script goc: co toa, chua sinh ghe
            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, KhachSuot(2).ToArray()));
            var toa2 = ke.ThaoTac.Single(t => t.ThuTuToa == 2);
            Assert.True(toa2.CoGhi);
            Assert.Contains("bổ sung 80 chỗ", toa2.LyDo);
        }

        // --- Bot toa ---

        [Fact]
        public void BotToaCoVeHieuLuc_Chan_LietKeVe()
        {
            var bc = BoiCanh();
            CoSan(bc, 5);
            Ve(bc, 5, 7, HNI, VIN);
            Ve(bc, 5, 8, DAN, SGO, "DA_HOAN_VE");    // ve da hoan khong chan

            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, KhachSuot(4).ToArray()));

            Assert.True(ke.CoXungDot);
            var ve = Assert.Single(ke.VeBiAnhHuong);
            Assert.Equal(5, ve.ThuTuToa);
            Assert.Equal(7, ve.SoGhe);
            Assert.Equal("HNI → VIN", ve.Chang);
            Assert.Contains("không còn trong đoàn tàu", ve.LyDo);
            Assert.DoesNotContain(ke.ThaoTac, t => t.ThuTuToa == 5);
        }

        [Fact]
        public void BotToaChiConVeDaHoan_NgungBan_ChuaTungCoVe_Xoa()
        {
            var bc = BoiCanh();
            CoSan(bc, 6);
            Ve(bc, 5, 3, HNI, SGO, "DA_HOAN_VE");
            Ve(bc, 5, 4, HNI, SGO, "DA_HUY");

            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, KhachSuot(4).ToArray()));

            Assert.False(ke.CoXungDot);
            Assert.Equal(LoaiThaoTacToaBanVe.NgungBan, ke.ThaoTac.Single(t => t.ThuTuToa == 5).Loai);
            Assert.Equal(LoaiThaoTacToaBanVe.Xoa, ke.ThaoTac.Single(t => t.ThuTuToa == 6).Loai);
            Assert.Contains("bỏ toa 5, 6", ke.MoTaThayDoi);
        }

        // --- Doi loai toa ---

        [Fact]
        public void DoiLoaiToaCoVeHieuLuc_Chan()
        {
            var bc = BoiCanhCoAN();
            CoSan(bc, 3);
            Ve(bc, 2, 40, VIN, DAN);
            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, T(Khach(1)), T(ToaAN501), T(Khach(3))));

            Assert.True(ke.CoXungDot);
            Assert.Contains("đổi từ Ngồi cứng sang Nằm khoang 4", ke.VeBiAnhHuong.Single().LyDo);
        }

        [Fact]
        public void DoiLoaiToaKhongVe_XoaRoiMoBanLaiCungSoToa()
        {
            var bc = BoiCanhCoAN();
            CoSan(bc, 3);
            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, T(Khach(1)), T(ToaAN501), T(Khach(3))));

            Assert.False(ke.CoXungDot);
            Assert.Equal(LoaiThaoTacToaBanVe.Xoa, ke.ThaoTac.Single(t => t.ThuTuToa == 2 && t.MaToaXeKhach == 1002).Loai);
            var moi = Loai(ke, LoaiThaoTacToaBanVe.Them).Single();
            Assert.Equal((2, "AN", 28), (moi.ThuTuToa, moi.LoaiToa, moi.SucChua));
        }

        [Fact]
        public void DoiLoaiToaChiConVeLichSu_NgungBanRoiMoBanLai()
        {
            var bc = BoiCanhCoAN();
            CoSan(bc, 3);
            Ve(bc, 2, 40, VIN, DAN, "DA_HOAN_VE");
            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, T(Khach(1)), T(ToaAN501), T(Khach(3))));

            Assert.Equal(LoaiThaoTacToaBanVe.NgungBan, ke.ThaoTac.Single(t => t.MaToaXeKhach == 1002).Loai);
            Assert.Single(Loai(ke, LoaiThaoTacToaBanVe.Them));
        }

        // --- Pham vi ga (cat / noi doc duong) ---

        [Fact]
        public void NoiTaiDAN_VeTruocDAN_Chan_VeSauDAN_Duoc()
        {
            var bc = BoiCanh();
            CoSan(bc, 3);
            Ve(bc, 3, 1, HNI, VIN);
            Ve(bc, 3, 2, DAN, SGO);
            var pa = PhuongAn(DM901, T(Khach(1)), T(Khach(2)), T(Khach(3), noi: DAN));

            var ke = QuyTacToaBanVe.LapKeHoach(bc, pa);

            var ve = Assert.Single(ke.VeBiAnhHuong);
            Assert.Equal(1, ve.SoGhe);
            Assert.Contains("chỉ chạy DAN → SGO", ve.LyDo);
        }

        [Fact]
        public void NoiTaiDAN_KhongVeNgoaiPhamVi_CapNhatPhamVi()
        {
            var bc = BoiCanh();
            CoSan(bc, 3);
            Ve(bc, 3, 2, DAN, SGO);
            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, T(Khach(1)), T(Khach(2)), T(Khach(3), noi: DAN)));

            var toa3 = ke.ThaoTac.Single(t => t.ThuTuToa == 3);
            Assert.True(toa3.CoGhi);
            Assert.Equal(DAN, toa3.MaGaNoi);
            Assert.Null(toa3.MaGaCat);
        }

        [Fact]
        public void GaNoiLaGaDau_CoiNhuChaySuot()
        {
            var bc = BoiCanh();
            CoSan(bc, 2);
            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, T(Khach(1), noi: HNI), T(Khach(2), cat: SGO)));
            Assert.False(ke.CoThayDoi);
        }

        [Theory]
        [InlineData(HNI, VIN, true)]
        [InlineData(HNI, HUE, false)]
        [InlineData(VIN, HUE, false)]
        [InlineData(HUE, SGO, false)]
        public void VeTrongPhamVi_CatTaiVIN(int gaDi, int gaDen, bool trong)
        {
            var bc = BoiCanh();
            var ve = new VeTrenToaDTO { MaGaDi = gaDi, MaGaDen = gaDen, TrangThai = "DA_DAT" };
            Assert.Equal(trong, QuyTacToaBanVe.VeTrongPhamVi(bc, ve, null, VIN));
        }

        // --- Tau hang ---

        [Fact]
        public void TauHang_BoToaBanVeCu_KhongMoBan()
        {
            var bc = BoiCanh();
            bc.LoaiTau = "TAU_HANG";
            CoSan(bc, 2);
            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, KhachSuot(3).ToArray()));

            Assert.False(ke.LaTauKhach);
            Assert.Empty(Loai(ke, LoaiThaoTacToaBanVe.Them));
            Assert.Equal(2, Loai(ke, LoaiThaoTacToaBanVe.Xoa).Count);
        }

        // --- Hang muc trong bang kiem tra an toan (QuyTacLapTau) ---

        [Fact]
        public void HangMucToaBanVe_CoVeBiHong_LaLoiChanLuu()
        {
            var bc = BoiCanh();
            CoSan(bc, 5);
            Ve(bc, 5, 7, HNI, VIN);
            var kq = QuyTacLapTau.KiemTra(bc, PhuongAn(DM901, KhachSuot(4).ToArray()));

            var hm = kq.HangMuc.Single(h => h.Ten == "Toa bán vé");
            Assert.Equal(MucDoKiemTra.Loi, hm.MucDo);
            Assert.Contains("TK_001", hm.NoiDung);
            Assert.True(kq.CoLoi);
            Assert.NotNull(kq.DongBoToaBanVe);
        }

        [Fact]
        public void HangMucToaBanVe_KhongCoToaKhach_CanhBao()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, T(ToaHang201), T(ToaHang202)));
            Assert.Equal(MucDoKiemTra.CanhBao, kq.HangMuc.Single(h => h.Ten == "Toa bán vé").MucDo);
        }

        [Fact]
        public void MoTaVeBiAnhHuong_ToiDa8Dong()
        {
            var bc = BoiCanh();
            CoSan(bc, 3);
            for (int g = 1; g <= 11; g++) Ve(bc, 3, g, HNI, SGO);
            var ke = QuyTacToaBanVe.LapKeHoach(bc, PhuongAn(DM901, KhachSuot(2).ToArray()));

            string moTa = QuyTacToaBanVe.MoTaVeBiAnhHuong(ke);
            Assert.Equal(9, moTa.Split('\n').Length);
            Assert.EndsWith("… và 3 vé khác.", moTa);
        }
    }
}
