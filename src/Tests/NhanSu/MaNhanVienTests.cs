using BUS.Services;

namespace Tests.NhanSu
{
    // Ma nhan vien theo chuc danh (quy uoc 29/09, CLAUDE.md §5.8): TIENTO_SSS, so dem rieng tung tien to.
    public class MaNhanVienTests
    {
        [Theory]
        [InlineData("Lái tàu", "LT")]
        [InlineData("Phụ lái", "PL")]
        [InlineData("Trưởng tàu", "TT")]
        [InlineData("tiếp viên", "TV")]            // khong phan biet hoa / thuong
        [InlineData(" Nhân viên khám xe ", "KX")]  // bo khoang trang
        [InlineData("Nhân viên bán vé", "BV")]
        [InlineData("Kế toán", "NV")]              // chuc danh ngoai danh muc
        [InlineData(null, "NV")]
        public void TienToTheoChucDanh(string? chucDanh, string tienTo)
            => Assert.Equal(tienTo, MaNhanVienTheoChucDanh.TienTo(chucDanh));

        [Fact]
        public void MaKeTiep_LaSoLonNhatCuaTienToCong1()
        {
            var daCap = new[] { "LT_001", "LT_007", "LT_003", "PL_009", "NV_020", "LT_abc", "" };
            Assert.Equal("LT_008", MaNhanVienTheoChucDanh.MaKeTiep("Lái tàu", daCap));
            Assert.Equal("PL_010", MaNhanVienTheoChucDanh.MaKeTiep("Phụ lái", daCap));
            Assert.Equal("NV_021", MaNhanVienTheoChucDanh.MaKeTiep("Kế toán", daCap));
        }

        [Fact]
        public void MaKeTiep_TienToChuaCo_BatDauTu001()
            => Assert.Equal("TT_001", MaNhanVienTheoChucDanh.MaKeTiep("Trưởng tàu", new[] { "LT_001", "PL_002" }));

        [Fact]
        public void MaKeTiep_Qua999_VanTangTiep()
            => Assert.Equal("LT_1000", MaNhanVienTheoChucDanh.MaKeTiep("Lái tàu", new[] { "LT_999" }));

        [Fact]
        public void MaKeTiep_MaDaTraLaiKhongCapLai_NeuConMaLonHon()
            // Q5 (11/10): MAX + 1 - ma LT_002 bi xoa giua chung thi khong cap lai
            => Assert.Equal("LT_004", MaNhanVienTheoChucDanh.MaKeTiep("Lái tàu", new[] { "LT_001", "LT_003" }));

        [Theory]
        [InlineData("LT_003", "Lái tàu", true)]
        [InlineData("PL_003", "Lái tàu", false)]
        [InlineData("LT003", "Lái tàu", false)]
        [InlineData("NV_001", "Kế toán", true)]
        [InlineData(null, "Lái tàu", false)]
        public void KhopChucDanh(string? ma, string chucDanh, bool khop)
            => Assert.Equal(khop, MaNhanVienTheoChucDanh.KhopChucDanh(ma, chucDanh));

        [Fact]
        public void ThuTuHienThi_ToTauTruoc()
        {
            var chucDanh = new[] { "Kế toán", "Nhân viên bán vé", "Trưởng tàu", "Lái tàu", "Phụ lái" };
            var xep = chucDanh.OrderBy(MaNhanVienTheoChucDanh.ThuTu).ToArray();
            Assert.Equal(new[] { "Lái tàu", "Phụ lái", "Trưởng tàu", "Nhân viên bán vé", "Kế toán" }, xep);
        }
    }
}
