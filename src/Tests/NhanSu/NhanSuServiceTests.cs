using BUS.Services;
using DTO.NhanSu;
using static Tests.NhanSu.MauNhanSu;

namespace Tests.NhanSu
{
    // Cac ham kiem tra tinh cua NhanSuService (dialog va Service dung chung, khong cham CSDL):
    // ho so (R7 hang bang), trang thai (Q7), moc ghi kiem tra (Q4), so lieu phieu, dieu kien len ban.
    public class NhanSuServiceTests
    {
        private static readonly string[] HangBang = { "D13E", "D19E" };

        private static NhanVienHienThi HoSo(string chucDanh = "Lái tàu", string? bang = "D19E") => new()
        {
            MaNhanVien = 1, MaNVCode = "LT_001", HoTen = "Nguyễn Văn An", SoDienThoai = "0912345678",
            ChucDanh = chucDanh, HangBangLai = bang, HanKhamSucKhoe = DateTime.Today.AddYears(1),
            DonViChuQuan = "Đội lái tàu Hà Nội", TrangThai = NhanVienHienThi.SanSang
        };

        private static string[] Truong(IEnumerable<DTO.PhuongTien.LoiNhapLieuDTO> loi) => loi.Select(l => l.Truong).ToArray();

        // --- Ho so ---

        [Fact]
        public void HoSoHopLe_KhongLoi()
            => Assert.Empty(NhanSuService.KiemTraNhanVien(HoSo(), HangBang));

        [Fact]
        public void LaiTauKhongCoHangBang_Loi()
            => Assert.Contains("HangBangLai", Truong(NhanSuService.KiemTraNhanVien(HoSo(bang: null), HangBang)));

        [Fact]
        public void HangBangKhongPhaiDongDauMayCoThat_Loi()
            => Assert.Contains("HangBangLai", Truong(NhanSuService.KiemTraNhanVien(HoSo(bang: "D20E"), HangBang)));

        [Fact]
        public void TruongTauCoHangBang_Loi()
            => Assert.Contains("HangBangLai", Truong(NhanSuService.KiemTraNhanVien(HoSo("Trưởng tàu", "D19E"), HangBang)));

        [Theory]
        [InlineData("0912345678", true)]
        [InlineData("02438123456", true)]
        [InlineData("912345678", false)]
        [InlineData("09123", false)]
        [InlineData("0912-345-678", false)]
        public void SoDienThoai(string sdt, bool hopLe)
        {
            var nv = HoSo();
            nv.SoDienThoai = sdt;
            Assert.Equal(hopLe, !Truong(NhanSuService.KiemTraNhanVien(nv, HangBang)).Contains("SoDienThoai"));
        }

        // --- Trang thai (Q7: "Dang lam nhiem vu" chi do nhan ban dat) ---

        [Fact]
        public void HoSoMoi_KhongChonTayDangLamNhiemVu_KhongDatDaNghiViec()
        {
            var dl = DuLieu();
            Assert.NotNull(NhanSuService.KiemTraTrangThai(null, NhanVienHienThi.DangLam, dl));
            Assert.NotNull(NhanSuService.KiemTraTrangThai(null, NhanVienHienThi.DaNghiViec, dl));
            Assert.Null(NhanSuService.KiemTraTrangThai(null, NhanVienHienThi.SanSang, dl));
        }

        [Fact]
        public void DangTrongCa_KhongDoiTrangThai()
        {
            var dl = DuLieu(new[] { KipMoi(3, ChuyenA, HNI, VIN, 1, 10, 20, PhanCongKipHienThi.DangThucHien) });
            var lt001 = dl.TimNhanVien(1)!;
            lt001.TrangThai = NhanVienHienThi.DangLam;

            Assert.Contains("bàn giao", NhanSuService.KiemTraTrangThai(lt001, NhanVienHienThi.NghiNgoi, dl));
            Assert.Null(NhanSuService.KiemTraTrangThai(lt001, NhanVienHienThi.DangLam, dl));
        }

        [Fact]
        public void NgoaiCa_DoiQuaLaiSanSangNghiNgoi()
        {
            var dl = DuLieu();
            Assert.Null(NhanSuService.KiemTraTrangThai(dl.TimNhanVien(1)!, NhanVienHienThi.NghiNgoi, dl));
            Assert.NotNull(NhanSuService.KiemTraTrangThai(dl.TimNhanVien(1)!, NhanVienHienThi.DangLam, dl));
        }

        // --- Kiem tra len ban ---

        [Fact]
        public void MocGhiKiemTra_3GioTruocGioNhanBan()
        {
            var dl = DuLieu(new[] { KipMoi(3, ChuyenA, VIN, DAN, 8, 13, 20) });
            var kip = dl.TimKip(3)!;

            Assert.Equal(kip.GioNhanBan.AddHours(-QuyTacKipLai.SoGioDoSomNhat), NhanSuService.GhiKiemTraTuLuc(kip));
            Assert.Equal(dl.TimChuyen(ChuyenA)!.LichDung[1].GioDiKeHoach, kip.GioNhanBan);   // gio tau chay tai ga nhan ban
        }

        [Theory]
        [InlineData("0", "8", true)]
        [InlineData("0", "7.9", false)]     // nghi duoi 8 gio
        [InlineData("0.01", "12", false)]   // con phai bang 0
        public void DieuKienLenBan(string con, string nghi, bool dat)
            => Assert.Equal(dat, QuyTacKipLai.DuDieuKienLenBan(decimal.Parse(con, System.Globalization.CultureInfo.InvariantCulture),
                                                               decimal.Parse(nghi, System.Globalization.CultureInfo.InvariantCulture)));

        [Theory]
        [InlineData("0", "8", null)]
        [InlineData("0.155", "8", "2 chữ số")]
        [InlineData("0", "8.25", "1 chữ số")]
        [InlineData("10", "8", "nồng độ cồn phải từ")]
        [InlineData("0", "-1", "giờ nghỉ phải từ")]
        public void SoLieuPhieuKiemTra(string con, string nghi, string? loiChua)
        {
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            string? loi = NhanSuService.KiemTraSoLieu(decimal.Parse(con, ic), decimal.Parse(nghi, ic));
            if (loiChua == null) Assert.Null(loi);
            else Assert.Contains(loiChua, loi);
        }

        [Fact]
        public void MaDuKien_TheoDuLieuDangCo()
        {
            var dl = DuLieu();
            Assert.Equal("LT_010", NhanSuService.MaDuKien("Lái tàu", dl));
            Assert.Equal("TT_005", NhanSuService.MaDuKien("Trưởng tàu", dl));
            Assert.Equal("TV_001", NhanSuService.MaDuKien("Tiếp viên", dl));
        }
    }
}
