using BUS.Services;
using DTO.Common;
using DTO.PhuongTien;
using ET.PhuongTien;
using static Tests.PhuongTien.MauLapTau;

namespace Tests.PhuongTien
{
    // 14/10: trang thai "Ngung khai thac" (dau may / toa xe) va cau mo ta du lieu lien quan khi xoa
    // (BUS/Services/QuyTacLapTau.cs, PhuongTienService.cs, ThamChieuXoa.cs).
    public class NgungKhaiThacTests
    {
        private static HangMucKiemTraDTO HangMuc(KetQuaKiemTraLapTauDTO kq, string ten)
            => kq.HangMuc.Single(h => h.Ten == ten);

        private static BoiCanhLapTauDTO BoiCanhCo904Ngung()
        {
            var bc = BoiCanh();
            bc.TimDauMay(DM904Yeu)!.TrangThai = PhuongTienService.NgungKhaiThac;
            return bc;
        }

        // --- Lap doan tau ---

        [Fact]
        public void DauMayChinh_NgungKhaiThac_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanhCo904Ngung(), PhuongAn(DM904Yeu, KhachSuot(5).ToArray()));

            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Đầu máy kéo chính").MucDo);
            Assert.Contains("đã ngừng khai thác", HangMuc(kq, "Đầu máy kéo chính").NoiDung);
        }

        [Fact]
        public void MayDay_NgungKhaiThac_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanhCo904Ngung(), PhuongAn(DM901, DM904Yeu, KhachSuot(5)));

            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Đầu máy đẩy").MucDo);
            Assert.Contains("đã ngừng khai thác", HangMuc(kq, "Đầu máy đẩy").NoiDung);
        }

        [Fact]
        public void DauMayChinh_BaoDuong_VanGhiDangBaoDuong()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM702BaoDuong, KhachSuot(5).ToArray()));
            Assert.Contains("đang bảo dưỡng", HangMuc(kq, "Đầu máy kéo chính").NoiDung);
        }

        [Fact]
        public void Toa_NgungKhaiThac_LoiTachRiengVoiBaoDuong()
        {
            var bc = BoiCanh();
            bc.ToaXe.Single(t => t.MaToaXe == Khach(6)).TrangThai = PhuongTienService.NgungKhaiThac;

            var kq = QuyTacLapTau.KiemTra(bc, PhuongAn(DM901, KhachSuot(6).Append(T(ToaBaoDuong401)).ToArray()));
            var hm = HangMuc(kq, "Toa xe sẵn sàng");

            Assert.Equal(MucDoKiemTra.Loi, hm.MucDo);
            Assert.Contains("Đang bảo dưỡng: NC-401.", hm.NoiDung);
            Assert.Contains("Đã ngừng khai thác: NC-106.", hm.NoiDung);
        }

        [Theory]
        [InlineData("BAO_DUONG", true)]
        [InlineData("NGUNG_KHAI_THAC", true)]
        [InlineData("SAN_SANG", false)]
        [InlineData("DANG_CHAY", false)]
        [InlineData("DA_NOI", false)]
        public void KhongDuocVanDung(string trangThai, bool khongDuoc)
            => Assert.Equal(khongDuoc, QuyTacLapTau.KhongDuocVanDung(trangThai));

        // --- Kiem tra nhap lieu: trang thai moi hop le, trang thai la van loi ---

        [Fact]
        public void KiemTraDauMay_NgungKhaiThac_HopLe_TrangThaiLa_Loi()
        {
            var dm = new DauMay { SoHieuDauMay = "D19E-901", MaDongDauMay = 1, DonViQuanLy = "XN Hà Nội",
                                  NamSanXuat = 2015, SoKmTichLuy = 1000m, TrangThai = PhuongTienService.NgungKhaiThac };
            Assert.DoesNotContain(PhuongTienService.KiemTraDauMay(dm, "D19E", laThemMoi: false), l => l.Truong == "TrangThai");

            dm.TrangThai = "HONG_HAN";
            Assert.Contains(PhuongTienService.KiemTraDauMay(dm, "D19E", laThemMoi: false), l => l.Truong == "TrangThai");
        }

        [Fact]
        public void KiemTraToaXe_NgungKhaiThac_HopLe()
        {
            var tx = new ToaXe { SoHieuToaXe = "NML-201", MaChungLoai = 1, TuTrongTan = 42m, TaiTrongToiDaTan = 6m,
                                 TrangThai = PhuongTienService.NgungKhaiThac };
            Assert.DoesNotContain(PhuongTienService.KiemTraToaXe(tx, "NML", laThemMoi: false), l => l.Truong == "TrangThai");
        }

        // --- Cau mo ta du lieu lien quan khi xoa ---

        [Fact]
        public void ThamChieu_TatCaBangKhong_XoaDuoc()
        {
            var ds = new[] { new SoThamChieuDTO("DOAN_TAU", 0, ""), new SoThamChieuDTO("CAP_NHIEN_LIEU", 0, ""),
                             new SoThamChieuDTO("BAO_DUONG", 0, "") };
            Assert.Empty(ThamChieuXoa.MoTa(ds));
        }

        [Fact]
        public void ThamChieu_ChiLietKeLoaiCoDuLieu_ViDuThemDauBaChamKhiNhieuHon3()
        {
            var ds = ThamChieuXoa.MoTa(new[]
            {
                new SoThamChieuDTO("DOAN_TAU", 5, "SE1 14/10, SE3 13/10, SE19 12/10"),
                new SoThamChieuDTO("CAP_NHIEN_LIEU", 0, ""),
                new SoThamChieuDTO("BAO_DUONG", 2, "")
            });

            Assert.Equal(new[] { "có trong đoàn tàu của 5 chuyến (SE1 14/10, SE3 13/10, SE19 12/10, …)", "2 phiếu bảo dưỡng" }, ds);
        }

        [Fact]
        public void ThamChieu_NhanVien_KipTaiKhoanLichSuMa()
        {
            var ds = ThamChieuXoa.MoTa(new[]
            {
                new SoThamChieuDTO("KIP_LAI", 2, "SE1 14/10, NA1 13/10"),
                new SoThamChieuDTO("KIEM_TRA", 3, ""),
                new SoThamChieuDTO("TAI_KHOAN", 1, "nv_dung"),
                new SoThamChieuDTO("LICH_SU_MA", 1, "PL_016")
            });

            Assert.Equal(new[]
            {
                "có trong 2 kíp lái (SE1 14/10, NA1 13/10)", "3 phiếu kiểm tra lên ban",
                "1 tài khoản đăng nhập (nv_dung)", "lịch sử đổi mã (PL_016)"
            }, ds);
        }

        [Fact]
        public void ThongDiepKhongXoaDuoc_CoDauDongVaGoiY()
        {
            string tin = ThamChieuXoa.KhongXoaDuoc("đầu máy D19E-901", new() { "1 phiếu bảo dưỡng" }, PhuongTienService.GoiYNgungKhaiThac);

            Assert.StartsWith("Không xóa được đầu máy D19E-901 vì đã có dữ liệu liên quan:\n• 1 phiếu bảo dưỡng\n\n", tin);
            Assert.Contains("Ngừng khai thác", tin);
        }
    }
}
