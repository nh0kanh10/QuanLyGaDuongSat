using BUS.Services;
using ET.VanHanh;
using Xunit;

namespace Tests.VanHanh
{
    public class DinhMucVanTocRangBuocTests
    {
        private readonly DinhMucVanTocService _service = new();

        [Fact]
        public void KiemTraHopLe_MaRong_BaoLoi()
        {
            var dm = new DinhMucVanToc
            {
                MaLoaiTau = "",
                TenLoaiTau = "Tàu Thử Nghiệm",
                VanTocDinhMucKmh = 60m
            };

            bool hopLe = _service.KiemTraHopLe(dm, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("không được để trống", loi);
        }

        [Theory]
        [InlineData("A")]                  // Quá ngắn (< 2)
        [InlineData("TAU_NHANH_QUA_DAI_1234567890")] // Quá dài (> 20)
        [InlineData("TAU NHANH")]          // Có khoảng trắng
        [InlineData("TAU@NHANH")]          // Ký tự đặc biệt
        [InlineData("TÀU_SE")]             // Tiếng Việt có dấu
        public void KiemTraHopLe_MaSaiDinhDang_BaoLoi(string maSai)
        {
            var dm = new DinhMucVanToc
            {
                MaLoaiTau = maSai,
                TenLoaiTau = "Tàu Test",
                VanTocDinhMucKmh = 60m
            };

            bool hopLe = _service.KiemTraHopLe(dm, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.NotEmpty(loi);
        }

        [Fact]
        public void KiemTraHopLe_TenRong_BaoLoi()
        {
            var dm = new DinhMucVanToc
            {
                MaLoaiTau = "TAU_TEST",
                TenLoaiTau = "   ",
                VanTocDinhMucKmh = 60m
            };

            bool hopLe = _service.KiemTraHopLe(dm, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("không được để trống", loi);
        }

        [Theory]
        [InlineData(5.0)]    // Dưới 10 km/h (quá chậm phi thực tế)
        [InlineData(250.0)]  // Trên 200 km/h (vượt quá hạ tầng đường sắt hiện tại)
        public void KiemTraHopLe_VanTocNgoaiGioiHan_BaoLoi(double vt)
        {
            var dm = new DinhMucVanToc
            {
                MaLoaiTau = "TAU_TEST",
                TenLoaiTau = "Tàu Thử Nghiệm",
                VanTocDinhMucKmh = (decimal)vt
            };

            bool hopLe = _service.KiemTraHopLe(dm, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("khoảng từ 10.0 đến 200.0", loi);
        }

        [Fact]
        public void LayThongSoDinhMuc_LoaiSanCo_TraVeDungVNR()
        {
            var (vanTocSe, tenSe, _) = DinhMucVanTocService.LayThongSoDinhMuc("TAU_NHANH");
            Assert.True(vanTocSe >= 50.0m && vanTocSe <= 80.0m);
            Assert.Contains("Thống Nhất", tenSe);

            var (vanTocCho, tenCho, _) = DinhMucVanTocService.LayThongSoDinhMuc("TAU_CHO");
            Assert.Equal(42.0m, vanTocCho);
            Assert.Contains("Khu Đoạn", tenCho);

            var (vanTocHang, tenHang, _) = DinhMucVanTocService.LayThongSoDinhMuc("TAU_HANG");
            Assert.Equal(32.0m, vanTocHang);
            Assert.Contains("Hàng", tenHang);
        }

        [Theory]
        [InlineData(1726.2, "TAU_NHANH")]
        [InlineData(319.2, "TAU_CHO")]
        [InlineData(1726.2, "TAU_HANG")]
        public void TinhGioChuanGoiY_TheoDinhMucDong_ChinhXac(double cuLy, string loaiTau)
        {
            var (vt, _, _) = DinhMucVanTocService.LayThongSoDinhMuc(loaiTau);
            decimal expected = Math.Round((decimal)cuLy / vt, 1);
            decimal tg = MacTauService.TinhGioChuanGoiY((decimal)cuLy, loaiTau);
            Assert.Equal(expected, tg);
        }
    }
}
