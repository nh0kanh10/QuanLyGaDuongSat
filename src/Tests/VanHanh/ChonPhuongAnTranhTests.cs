using BUS.Services;
using Xunit;

namespace Tests.VanHanh
{
    /// <summary>
    /// Luật chọn tàu nhường đường (DieuDoService.ChonPhuongAnTau1ChaySuot) - hàm thuần, không cần CSDL.
    /// Chi phí tính như DanhGiaPhuongAnTranh: phút trễ × (6 − cấp tàu bị tránh) × 10 (+ khoảng cách, bỏ qua ở đây).
    /// </summary>
    public class ChonPhuongAnTranhTests
    {
        private static double ChiPhi(int phutTre, int capTauBiTranh) => phutTre * (6 - capTauBiTranh) * 10.0;

        [Fact]
        public void KhacCap_TauCapThapChoNgan_TauCapThapNhuong()
        {
            // SE1 (cấp 1) gặp NA1 (cấp 3): NA1 chờ 20' -> NA1 nhường dù để SE1 chờ chỉ 10' thì rẻ hơn
            bool chonPa1 = DieuDoService.ChonPhuongAnTau1ChaySuot(1, 3,
                20, true, ChiPhi(20, 3),
                10, true, ChiPhi(10, 1));
            Assert.True(chonPa1);
        }

        [Fact]
        public void KhacCap_TauCapThapChoQuaLau_DaoLaiKhiReHon()
        {
            // Dữ liệu thật 12/11: SE4 (cấp 1) x SE19 (cấp 2) ở khu gian Thanh Hóa - Vinh.
            // SE19 nhường: chờ +306' ở Thanh Hóa; SE4 nhường: chờ +65' ở Vinh -> SE4 chờ.
            bool chonPa1 = DieuDoService.ChonPhuongAnTau1ChaySuot(1, 2,
                306, true, ChiPhi(306, 2),
                65, true, ChiPhi(65, 1));
            Assert.False(chonPa1);

            // Đổi thứ tự 2 tàu -> cùng kết luận
            bool chonPa1DaoThuTu = DieuDoService.ChonPhuongAnTau1ChaySuot(2, 1,
                65, true, ChiPhi(65, 1),
                306, true, ChiPhi(306, 2));
            Assert.True(chonPa1DaoThuTu);
        }

        [Fact]
        public void KhacCap_TauCapThapChoQuaLau_NhungDaoLaiConTeHon_GiuUuTien()
        {
            // NA1 (cấp 3) chờ +146' ở Thanh Hóa; nếu SE4 (cấp 1) nhường thì chờ +295' -> NA1 vẫn nhường
            bool chonPa1 = DieuDoService.ChonPhuongAnTau1ChaySuot(1, 3,
                146, true, ChiPhi(146, 3),
                295, true, ChiPhi(295, 1));
            Assert.True(chonPa1);
        }

        [Fact]
        public void ChoDungNguong60Phut_VanGiuUuTien()
        {
            int nguong = DieuDoService.SoPhutChoToiDaGiuUuTien;
            bool chonPa1 = DieuDoService.ChonPhuongAnTau1ChaySuot(1, 2,
                nguong, true, ChiPhi(nguong, 2),
                5, true, ChiPhi(5, 1));
            Assert.True(chonPa1);
        }

        [Fact]
        public void PhuongAnCoDuongTranh_LuonThangPhuongAnKhongCo()
        {
            // Tàu cấp thấp không có đường tránh trống -> tàu cấp cao dừng tránh
            bool chonPa1 = DieuDoService.ChonPhuongAnTau1ChaySuot(1, 3,
                20, false, ChiPhi(20, 3) + 50000,
                30, true, ChiPhi(30, 1));
            Assert.False(chonPa1);
        }

        [Fact]
        public void CungCap_ChonChiPhiNhoHon()
        {
            Assert.False(DieuDoService.ChonPhuongAnTau1ChaySuot(1, 1, 57, true, ChiPhi(57, 1), 39, true, ChiPhi(39, 1)));
            Assert.True(DieuDoService.ChonPhuongAnTau1ChaySuot(1, 1, 39, true, ChiPhi(39, 1), 57, true, ChiPhi(57, 1)));
        }
    }
}
