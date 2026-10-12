using BUS.Services;
using DTO.NhanSu;
using static Tests.NhanSu.MauNhanSu;

namespace Tests.NhanSu
{
    // Dat chot 12/10: chuc danh + don vi theo danh muc (DanhMucDonVi), dieu kien doi chuc danh /
    // hang bang / don vi (NhanSuService.KiemTraDoiViTri), ma cu trong lich su khong cap lai.
    public class HoSoViTriTests
    {
        private static readonly string[] HangBang = { "D13E", "D19E" };

        private static string[] Truong(IEnumerable<DTO.PhuongTien.LoiNhapLieuDTO> loi) => loi.Select(l => l.Truong).ToArray();

        private static NhanVienHienThi HoSo(string chucDanh, string? bang, string donVi) => new()
        {
            MaNhanVien = 99, MaNVCode = "LT_099", HoTen = "Nguyễn Văn Thử", SoDienThoai = "0912345678",
            ChucDanh = chucDanh, HangBangLai = bang, HanKhamSucKhoe = DateTime.Today.AddYears(1),
            DonViChuQuan = donVi, TrangThai = NhanVienHienThi.SanSang
        };

        // Ban sao ho so trong anh chup roi sua (nhu dialog)
        private static NhanVienHienThi Sua(DuLieuNhanSu dl, int ma, Action<NhanVienHienThi> doi)
        {
            var nv = dl.TimNhanVien(ma)!.SaoChep();
            doi(nv);
            return nv;
        }

        // --- Danh muc don vi theo chuc danh ---

        [Theory]
        [InlineData("Lái tàu", "Đội lái tàu Vinh")]
        [InlineData("Phụ lái", "Đội lái tàu Sài Gòn")]
        [InlineData("Trưởng tàu", "Đoàn tiếp viên Phương Nam")]
        [InlineData("Tiếp viên", "Đoàn tiếp viên Hà Nội")]
        [InlineData("Nhân viên khám xe", "Trạm Giáp Bát")]
        [InlineData("Nhân viên bán vé", "Ga Đà Nẵng")]
        public void DonViDungLoai_HopLe(string chucDanh, string donVi)
            => Assert.True(DanhMucDonVi.HopLe(chucDanh, donVi, CacGa));

        [Theory]
        [InlineData("Lái tàu", "Đoàn tiếp viên Hà Nội")]       // cau hoi cua Dat 12/10
        [InlineData("Phụ lái", "Ga Vinh")]
        [InlineData("Trưởng tàu", "Đội lái tàu Vinh")]
        [InlineData("Tiếp viên", "Trạm Giáp Bát")]
        [InlineData("Nhân viên bán vé", "Ga Huế")]             // ga khong co trong danh muc ga
        [InlineData("Nhân viên bán vé", "Đội lái tàu Hà Nội")]
        [InlineData("Thợ hàn", "Đội lái tàu Hà Nội")]          // chuc danh ngoai danh muc
        public void DonViSaiLoai_KhongHopLe(string chucDanh, string donVi)
            => Assert.False(DanhMucDonVi.HopLe(chucDanh, donVi, CacGa));

        [Fact]
        public void BanVe_DanhSachGaTheoThuTuLyTrinh()
            => Assert.Equal(new[] { "Ga Hà Nội", "Ga Vinh", "Ga Đà Nẵng", "Ga Sài Gòn" },
                            DanhMucDonVi.DonViHopLe("Nhân viên bán vé", CacGa));

        [Fact]
        public void KiemTraNhanVien_LaiTauThuocDoanTiepVien_LoiDonVi()
        {
            var loi = NhanSuService.KiemTraNhanVien(HoSo("Lái tàu", "D19E", "Đoàn tiếp viên Hà Nội"), HangBang, CacGa);
            Assert.Equal(new[] { "DonViChuQuan" }, Truong(loi));
            Assert.Contains("đội lái tàu", loi[0].ThongDiep);
        }

        [Fact]
        public void KiemTraNhanVien_ChucDanhNgoaiDanhMuc_LoiChucDanh()
            => Assert.Contains("ChucDanh", Truong(NhanSuService.KiemTraNhanVien(HoSo("Thợ hàn", null, "Trạm Giáp Bát"), HangBang, CacGa)));

        [Fact]
        public void KiemTraNhanVien_BanVeTaiGaCoThat_HopLe()
            => Assert.Empty(NhanSuService.KiemTraNhanVien(HoSo("Nhân viên bán vé", null, "Ga Vinh"), HangBang, CacGa));

        // --- Dieu kien doi chuc danh / hang bang / don vi ---
        // Anh chup mau: LT_002 (2) lai tau kip 2 SE19 HNI -> VIN (da phan cong, dau may D19E);
        // TT_003 (22) truong tau cung kip; LT_001 (1) chua co kip.

        [Fact]
        public void DoiChucDanh_KhiKipChuaNhanBanSeSaiViTri_Chan()
        {
            var dl = DuLieu();
            var loi = NhanSuService.KiemTraDoiViTri(dl.TimNhanVien(2)!, Sua(dl, 2, n => n.ChucDanh = "Phụ lái"), dl);
            Assert.Equal(new[] { "ChucDanh" }, Truong(loi));
            Assert.Contains("SE19", loi[0].ThongDiep);
        }

        [Fact]
        public void DoiHangBang_KhiKipChuaNhanBanSaiDongMay_Chan()
        {
            var dl = DuLieu();
            var loi = NhanSuService.KiemTraDoiViTri(dl.TimNhanVien(2)!, Sua(dl, 2, n => n.HangBangLai = "D13E"), dl);
            Assert.Equal(new[] { "HangBangLai" }, Truong(loi));
        }

        [Fact]
        public void TruongTauDoiSangTiepVien_KhiConKip_Chan()
        {
            var dl = DuLieu();
            Assert.NotEmpty(NhanSuService.KiemTraDoiViTri(dl.TimNhanVien(22)!, Sua(dl, 22, n => n.ChucDanh = "Tiếp viên"), dl));
        }

        [Fact]
        public void DoiDonVi_KhiChiCoKipChuaNhanBan_DuocDoi()
        {
            // Doi depot chi anh huong R10 (luu y), khong lam hong kip
            var dl = DuLieu();
            Assert.Empty(NhanSuService.KiemTraDoiViTri(dl.TimNhanVien(2)!, Sua(dl, 2, n => n.DonViChuQuan = "Đội lái tàu Vinh"), dl));
        }

        [Fact]
        public void DoiChucDanh_KhiChuaCoKip_DuocDoi()
        {
            var dl = DuLieu();
            Assert.Empty(NhanSuService.KiemTraDoiViTri(dl.TimNhanVien(1)!, Sua(dl, 1, n => n.ChucDanh = "Phụ lái"), dl));
        }

        [Fact]
        public void HetHanKham_LaSuThat_KhongChan()
        {
            // Han kham / nghi viec van luu duoc (dialog canh bao thay nguoi), chi chuc danh / bang / don vi bi chan
            var dl = DuLieu();
            Assert.Empty(NhanSuService.KiemTraDoiViTri(dl.TimNhanVien(2)!,
                Sua(dl, 2, n => { n.HanKhamSucKhoe = DateTime.Today.AddDays(-1); n.TrangThai = NhanVienHienThi.DaNghiViec; }), dl));
        }

        [Fact]
        public void DangTrongCa_KhongDoiChucDanhHangBangDonVi()
        {
            var dl = DuLieu(new[] { KipMoi(3, ChuyenA, HNI, VIN, lai: 1, phu: 10, truong: 20, PhanCongKipHienThi.DangThucHien) });
            var goc = dl.TimNhanVien(1)!;
            var loi = NhanSuService.KiemTraDoiViTri(goc,
                Sua(dl, 1, n => { n.ChucDanh = "Phụ lái"; n.HangBangLai = "D13E"; n.DonViChuQuan = "Đội lái tàu Vinh"; }), dl);
            Assert.Equal(new[] { "ChucDanh", "HangBangLai", "DonViChuQuan" }, Truong(loi));
            Assert.All(loi, l => Assert.Contains("Đang trong ca SE1", l.ThongDiep));

            // Sua so dien thoai / han kham khi dang trong ca van duoc
            Assert.Empty(NhanSuService.KiemTraDoiViTri(goc, Sua(dl, 1, n => n.SoDienThoai = "0987654321"), dl));
        }

        // --- Ma cu khong cap lai ---

        [Fact]
        public void MaCuTrongLichSu_KhongCapLai()
        {
            var dl = DuLieu();
            // LT_001 (1) truoc la phu lai PL_009, len lai tau -> PL_009 "nghi huu"; phu lai lon nhat dang co
            // la PL_005 nhung phu lai moi phai la PL_010 (khong cap lai PL_009 cho nguoi khac)
            dl.TimNhanVien(1)!.LichSuMa = new[]
            {
                new LichSuMaNhanVienDTO("PL_009", "LT_001", "Phụ lái", "Lái tàu", "Đội lái tàu Hà Nội", "Đội lái tàu Hà Nội", DateTime.Today)
            };
            Assert.Contains("PL_009", dl.MaDaCap);
            Assert.Equal("PL_010", NhanSuService.MaDuKien("Phụ lái", dl));
        }
    }
}
