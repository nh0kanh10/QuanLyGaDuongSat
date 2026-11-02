using BUS.Services;

namespace Tests.BaoTri
{
    // Doi trang thai chuyen o Lich trinh: luc nao phai qua kiem tra xuat ben (ChuyenTauService.CanKiemTraXuatBen)
    // va menu "Chuyen Trang Thai" duoc hien nhung trang thai nao (LayTrangThaiKeTiep). Phan goi CSDL that
    // (DoiTrangThai, CapNhat bi chan) duoc kiem o tools\kiemtra_xuatben tren CSDL tam.
    public class ChuyenTrangThaiXuatBenTests
    {
        [Theory]
        [InlineData("DA_LEN_LICH", "SAN_SANG")]
        [InlineData("DA_LEN_LICH", "DANG_CHAY")]
        [InlineData("SAN_SANG", "DANG_CHAY")]
        public void SangSanSangHoacDangChay_TuChuaChay_PhaiKiemTra(string cu, string moi)
            => Assert.True(ChuyenTauService.CanKiemTraXuatBen(cu, moi));

        [Theory]
        [InlineData("SAN_SANG", "SAN_SANG")]        // khong doi (luu form binh thuong)
        [InlineData("DA_LEN_LICH", "DA_LEN_LICH")]
        [InlineData("SAN_SANG", "DA_LEN_LICH")]     // lui ve
        [InlineData("DA_LEN_LICH", "DA_HUY")]
        [InlineData("TRE_GIO", "DANG_CHAY")]        // tau da xuat ben roi
        [InlineData("DANG_CHAY", "TRE_GIO")]
        [InlineData("DANG_CHAY", "HOAN_THANH")]
        public void ConLai_KhongKiem(string cu, string moi)
            => Assert.False(ChuyenTauService.CanKiemTraXuatBen(cu, moi));

        [Fact]
        public void KeTiep_TheoQuyTrinh_KhongGomChinhNo()
        {
            Assert.Equal(new[] { "DA_HUY", "SAN_SANG" }, ChuyenTauService.LayTrangThaiKeTiep("DA_LEN_LICH").OrderBy(x => x));
            Assert.Equal(new[] { "DA_HUY", "DA_LEN_LICH", "DANG_CHAY" }, ChuyenTauService.LayTrangThaiKeTiep("SAN_SANG").OrderBy(x => x));
            Assert.DoesNotContain("DANG_CHAY", ChuyenTauService.LayTrangThaiKeTiep("DANG_CHAY"));
        }

        [Theory]
        [InlineData("HOAN_THANH")]
        [InlineData("DA_HUY")]
        [InlineData("KHONG_CO")]
        public void DaKetThucHoacLa_KhongCoTrangThaiKeTiep(string tt)
            => Assert.Empty(ChuyenTauService.LayTrangThaiKeTiep(tt));
    }
}
