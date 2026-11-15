using BUS.Services;
using DTO.Common;

namespace Tests.QuanTri
{
    // Bam / kiem mat khau PBKDF2 (Sprint 3) + phien bat buoc dang nhap.
    public class MatKhauHelperTests
    {
        // Chuoi bam cua tai khoan admin trong seed S20261108_1100 (sinh bang Python hashlib.pbkdf2_hmac)
        const string BamSeedAdmin = "PBKDF2$SHA256$100000$O/89bp37FZsfhrGddd6ZZg==$XKuaXz7Q8hR6fiAif4tcNihxDui8rJmPP8Ufr8OnT5o=";
        const string BamBCryptCu = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewR5I72o3jquP6hK";

        [Fact]
        public void TaoRoiKiemTra_DungMatKhau_Dat()
        {
            string bam = MatKhauHelper.TaoChuoiBam("Abc12345", 2_000);
            Assert.True(MatKhauHelper.KiemTra("Abc12345", bam));
        }

        [Theory]
        [InlineData("abc12345")]       // khac hoa / thuong
        [InlineData("Abc1234")]
        [InlineData("Abc12345 ")]
        [InlineData("")]
        [InlineData(null)]
        public void KiemTra_SaiMatKhau_KhongDat(string? sai)
        {
            string bam = MatKhauHelper.TaoChuoiBam("Abc12345", 2_000);
            Assert.False(MatKhauHelper.KiemTra(sai, bam));
        }

        [Fact]
        public void HaiLanBamCungMatKhau_KhacNhau_DoMuoiNgauNhien()
        {
            Assert.NotEqual(MatKhauHelper.TaoChuoiBam("Abc12345", 2_000), MatKhauHelper.TaoChuoiBam("Abc12345", 2_000));
        }

        [Fact]
        public void ChuoiBamSeed_KhopMatKhauDemo()
        {
            Assert.True(MatKhauHelper.KiemTra("Demo@123", BamSeedAdmin));
            Assert.False(MatKhauHelper.KiemTra("demo@123", BamSeedAdmin));
        }

        [Fact]
        public void DinhDang_SoVongNamTrongChuoi()
        {
            string bam = MatKhauHelper.TaoChuoiBam("Abc12345");
            Assert.StartsWith($"PBKDF2$SHA256${MatKhauHelper.SoVongMacDinh}$", bam);
            Assert.Equal(5, bam.Split('$').Length);
            Assert.True(bam.Length <= 255);    // cot MatKhauHash VARCHAR(255)
        }

        [Theory]
        [InlineData(BamBCryptCu)]
        [InlineData("PBKDF2$SHA256$abc$xx$yy")]
        [InlineData("PBKDF2$SHA256$100000$khong-phai-base64$!!")]
        [InlineData("PBKDF2$MD5$100000$O/89bp37FZsfhrGddd6ZZg==$XKuaXz7Q8hR6fiAif4tcNihxDui8rJmPP8Ufr8OnT5o=")]
        [InlineData("")]
        [InlineData(null)]
        public void ChuoiBamCuHoacHong_KhongNemLoi_TraFalse(string? bam)
        {
            Assert.False(MatKhauHelper.KiemTra("Demo@123", bam));
        }

        [Fact]
        public void LaDinhDangMoi_PhanBietBCryptCu()
        {
            Assert.True(MatKhauHelper.LaDinhDangMoi(BamSeedAdmin));
            Assert.False(MatKhauHelper.LaDinhDangMoi(BamBCryptCu));
        }

        [Theory]
        [InlineData("Demo@123", true)]
        [InlineData("abc12345", true)]
        [InlineData("abc1234", false)]       // 7 ky tu
        [InlineData("abcdefgh", false)]      // khong co so
        [InlineData("12345678", false)]      // khong co chu
        [InlineData("abc 12345", false)]     // khoang trang
        [InlineData("", false)]
        public void KiemTraDoManh(string mk, bool hopLe)
        {
            Assert.Equal(hopLe, MatKhauHelper.KiemTraDoManh(mk) == null);
        }

        [Fact]
        public void KiemTraDoManh_QuaDai_Loi()
        {
            Assert.NotNull(MatKhauHelper.KiemTraDoManh("a1" + new string('x', 63)));
        }
    }

    // PhienLamViec la static -> chay tuan tu voi cac test khac dung phien (Collection rieng).
    [Collection("PhienLamViec")]
    public class PhienBatBuocDangNhapTests
    {
        [Fact]
        public void BatBuocDangNhap_ChuaDangNhap_KhongCoQuyenNao()
        {
            PhienLamViec.DangXuat();
            try
            {
                PhienLamViec.BatBuocDangNhap = false;
                Assert.True(PhienLamViec.CoQuyen(MaQuyen.BanVe));        // phien mac dinh cua cong cu kiem thu

                PhienLamViec.BatBuocDangNhap = true;
                Assert.False(PhienLamViec.CoQuyen(MaQuyen.BanVe));
                Assert.Throws<KhongDuQuyenException>(() => PhienLamViec.BatBuocCoQuyen(MaQuyen.BanVe));
            }
            finally
            {
                PhienLamViec.BatBuocDangNhap = false;
            }
        }

        [Fact]
        public void DangNhapThat_ChiCoQuyenDuocCap()
        {
            try
            {
                PhienLamViec.DangNhap(5, "nv_mai", "Hoàng Thị Mai", null,
                    new[] { MaQuyen.BanVe, MaQuyen.XemVe }, new[] { "Nhân viên bán vé" });
                Assert.True(PhienLamViec.CoQuyen("ticket_issue"));      // khong phan biet hoa / thuong
                Assert.False(PhienLamViec.CoQuyen(MaQuyen.CauHinhHeThong));
                Assert.Equal("Nhân viên bán vé", PhienLamViec.MoTaVaiTro);
            }
            finally
            {
                PhienLamViec.DangXuat();
            }
            Assert.Empty(PhienLamViec.CacVaiTro);
        }
    }
}

namespace Tests.QuanTri
{
    // PhienLamViec la static: test doi phien chay rieng, khong song song voi test khac.
    [CollectionDefinition("PhienLamViec", DisableParallelization = true)]
    public class PhienLamViecCollection { }
}
