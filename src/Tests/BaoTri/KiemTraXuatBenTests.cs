using BUS.Services;
using DTO.BaoTri;

namespace Tests.BaoTri
{
    // Luat xuat ben + dieu kien duyet an toan (KiemTraXuatBenService.DanhGia / DieuKienDuyet) tren
    // anh chup du lieu dung tay. Kip lai can du lieu Nhan su -> o day chi kiem nhanh "khong doc duoc"
    // (ns = null -> chan); phan kip duoc kiem o tools\kiemtra_baotri tren CSDL tam.
    public class KiemTraXuatBenTests
    {
        static readonly DateTime BayGio = new(2026, 10, 24, 10, 0, 0);

        static DuLieuBaoTri DuLieu(bool daDuyet = false, string trangThaiChuyen = "DA_LEN_LICH", DateTime? suaDoanTau = null,
                                   decimal kmDauMay = 30_000m, params KhamXeDTO[] khamXe)
            => new()
            {
                BayGio = BayGio,
                DoanTau =
                {
                    new DoanTauBaoTriDTO
                    {
                        MaDoanTau = 6, MaChuyenTau = 60, SoHieuMacTau = "NA1", TrangThaiChuyen = trangThaiChuyen,
                        GioXuatPhatKH = BayGio.AddHours(12), MaDauMayChinh = 1, SoHieuDauMayChinh = "D13E-701",
                        DaDuyetAnToan = daDuyet, PhienBan = new byte[8], ThoiDiemSuaDoanTau = suaDoanTau
                    }
                },
                DauMay = { new DauMayBaoTriDTO { MaDauMay = 1, SoHieuDauMay = "D13E-701", MaDongCode = "D13E", TrangThai = "SAN_SANG",
                                                 SoKmTichLuy = kmDauMay, DungTichBonDauLit = 3200, DinhMucSFC = 100m } },
                KhamXe = khamXe.ToList()
            };

        static KhamXeDTO Kham(int ma, bool dat, int phutTruoc) => new()
        {
            MaKhamXe = ma, MaDoanTau = 6, MaNguoiKham = 2, ApLucHamBar = dat ? 5.0m : 4.5m, DoSutApBarPhut = 0.1m,
            DaCapNuoc = true, DaXaVeSinh = true, ThoiDiemKham = BayGio.AddMinutes(-phutTruoc)
        };

        static string MucDo(KetQuaXuatBen kq, string nhom) => kq.CacMuc.First(m => m.Nhom == nhom).MucDo;

        [Fact]
        public void ChuaLapDoanTau_Chan()
        {
            var kq = KiemTraXuatBenService.DanhGia(new DuLieuBaoTri { BayGio = BayGio }, null, 99);
            Assert.False(kq.DuocXuatBen);
            Assert.Contains(kq.LyDoChan, l => l.Contains("chưa lập đoàn tàu"));
        }

        [Fact]
        public void ChuaKham_ChuaDuyet_KhongDocDuocNhanSu_DeuChan()
        {
            var kq = KiemTraXuatBenService.DanhGia(DuLieu(), null, 60);
            Assert.Equal("CHAN", MucDo(kq, "KHAM_XE"));
            Assert.Equal("CHAN", MucDo(kq, "DUYET"));
            Assert.Equal("CHAN", MucDo(kq, "KIP_LAI"));
            Assert.Equal("DAT", MucDo(kq, "BAO_DUONG"));
            Assert.Equal("DAT", MucDo(kq, "NHIEN_LIEU"));    // chua cap dau khong phai ly do an toan
            Assert.StartsWith("Chưa được xuất bến:", kq.MoTaChan);
        }

        [Fact]
        public void BienBanMoiNhat_QuyetDinh()
        {
            // Khong dat luc 9:00, kham lai dat luc 9:30 -> dat
            var kq = KiemTraXuatBenService.DanhGia(DuLieu(true, "DA_LEN_LICH", null, 30_000m, Kham(1, false, 60), Kham(2, true, 30)), null, 60);
            Assert.Equal("DAT", MucDo(kq, "KHAM_XE"));
            // Nguoc lai: dat truoc, khong dat sau -> chan
            kq = KiemTraXuatBenService.DanhGia(DuLieu(true, "DA_LEN_LICH", null, 30_000m, Kham(1, true, 60), Kham(2, false, 30)), null, 60);
            Assert.Equal("CHAN", MucDo(kq, "KHAM_XE"));
        }

        [Fact]
        public void BienBanLapTruocLanSuaDoanTau_HetHieuLuc()
        {
            var dl = DuLieu(false, "DA_LEN_LICH", BayGio.AddMinutes(-10), 30_000m, Kham(1, true, 30));
            var muc = KiemTraXuatBenService.MucKhamXe(dl, dl.DoanTau[0]);
            Assert.Equal("CHAN", muc.MucDo);
            Assert.Contains("khám lại", muc.KetQua);

            dl = DuLieu(false, "DA_LEN_LICH", BayGio.AddMinutes(-60), 30_000m, Kham(1, true, 30));
            Assert.Equal("DAT", KiemTraXuatBenService.MucKhamXe(dl, dl.DoanTau[0]).MucDo);
        }

        [Fact]
        public void DauMayQuaHanBaoDuong_ChanCaXuatBenLanDuyet()
        {
            var dl = DuLieu(false, "DA_LEN_LICH", null, 210_000m, Kham(1, true, 30));
            var kq = KiemTraXuatBenService.DanhGia(dl, null, 60);
            Assert.Contains(kq.LyDoChan, l => l.Contains("Quá hạn R2"));

            var dk = KiemTraXuatBenService.DieuKienDuyet(dl, dl.DoanTau[0]);
            Assert.False(dk.CoTheDuyet);
            Assert.Contains("Quá hạn R2", dk.MoTaChan);
        }

        [Fact]
        public void SapToiHanBaoDuong_ChiLuuY()
        {
            var dl = DuLieu(true, "DA_LEN_LICH", null, 46_000m, Kham(1, true, 30));   // R1 92%
            Assert.Equal("LUU_Y", MucDo(KiemTraXuatBenService.DanhGia(dl, null, 60), "BAO_DUONG"));
            Assert.True(KiemTraXuatBenService.DieuKienDuyet(dl, dl.DoanTau[0]).LyDoKhongDuyet != null);   // da duyet roi
        }

        [Fact]
        public void DieuKienDuyet_KhamDat_DauMayTrongChuKy_DuocDuyet_KhongXetKip()
        {
            var dl = DuLieu(false, "DA_LEN_LICH", null, 30_000m, Kham(1, true, 30));
            var dk = KiemTraXuatBenService.DieuKienDuyet(dl, dl.DoanTau[0]);
            Assert.True(dk.CoTheDuyet);
            Assert.DoesNotContain(dk.CacMuc, m => m.Nhom == "KIP_LAI");
        }

        [Theory]
        [InlineData("DANG_CHAY")]
        [InlineData("HOAN_THANH")]
        public void DieuKienDuyet_ChuyenDaChay_KhongDuyet(string trangThai)
        {
            var dl = DuLieu(false, trangThai, null, 30_000m, Kham(1, true, 30));
            Assert.False(KiemTraXuatBenService.DieuKienDuyet(dl, dl.DoanTau[0]).CoTheDuyet);
        }

        [Fact]
        public void NhienLieuVuotDinhMuc_LuuY_SatDinhMuc_Dat()
        {
            var dl = DuLieu(true, "DA_LEN_LICH", null, 30_000m, Kham(1, true, 30));
            dl.CapDau.Add(new CapNhienLieuDTO { MaNhatKyDau = 1, MaDauMay = 1, SuatTieuHaoSFC = 101m, ThoiDiemBomDau = BayGio.AddHours(-2) });
            Assert.Equal("LUU_Y", MucDo(KiemTraXuatBenService.DanhGia(dl, null, 60), "NHIEN_LIEU"));

            dl.CapDau.Add(new CapNhienLieuDTO { MaNhatKyDau = 2, MaDauMay = 1, SuatTieuHaoSFC = 99.5m, ThoiDiemBomDau = BayGio.AddHours(-1) });
            var muc = KiemTraXuatBenService.DanhGia(dl, null, 60).CacMuc.First(m => m.Nhom == "NHIEN_LIEU");
            Assert.Equal("DAT", muc.MucDo);
            Assert.Contains("Sát định mức", muc.KetQua);
        }
    }
}
