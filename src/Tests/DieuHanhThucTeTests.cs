using System;
using BUS.Services;
using DTO.Common;
using Xunit;

namespace Tests
{
    [Collection("DieuDoDbCollection")]
    public class DieuHanhThucTeTests
    {
        [Fact]
        public void ThoiGianHeThongHelper_BatMoPhong_TraVeGioMoPhongChinhXac()
        {
            // Arrange
            DateTime gioTest = new DateTime(2026, 11, 13, 7, 15, 0);

            // Act
            ThoiGianHeThongHelper.BatMoPhong(gioTest);

            // Assert
            Assert.True(ThoiGianHeThongHelper.CheDoMoPhong);
            Assert.Equal(gioTest, ThoiGianHeThongHelper.LayGioHienTai());

            // Cleanup
            ThoiGianHeThongHelper.TatMoPhong();
            Assert.False(ThoiGianHeThongHelper.CheDoMoPhong);
        }

        [Fact]
        public void ThoiGianHeThongHelper_SuKienThayDoiThoiGian_PhatTinHieu()
        {
            // Arrange
            DateTime gioTest1 = new DateTime(2026, 11, 13, 7, 15, 0);
            DateTime gioTest2 = new DateTime(2026, 11, 13, 7, 45, 0);
            DateTime? gioNhanDuoc = null;

            Action<DateTime> handler = (dt) => gioNhanDuoc = dt;
            ThoiGianHeThongHelper.ThoiGianThayDoi += handler;

            // Act
            ThoiGianHeThongHelper.BatMoPhong(gioTest1);
            Assert.Equal(gioTest1, gioNhanDuoc);

            ThoiGianHeThongHelper.GioMoPhong = gioTest2;
            Assert.Equal(gioTest2, gioNhanDuoc);

            // Cleanup
            ThoiGianHeThongHelper.ThoiGianThayDoi -= handler;
            ThoiGianHeThongHelper.TatMoPhong();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void DieuDoService_KichHoatKichBanDemo_ThanhCongVaCoThongBao(int maKichBan)
        {
            // Arrange
            var service = new DieuDoService();
            DateTime ngay = new DateTime(2026, 11, 13);

            // Act
            bool ketQua = service.KichHoatKichBanDemo(maKichBan, ngay, out string thongBao);

            // Assert
            Assert.True(ketQua);
            Assert.NotEmpty(thongBao);
            Assert.Contains(maKichBan.ToString(), thongBao);

            // Cleanup
            ThoiGianHeThongHelper.TatMoPhong();
        }

        [Fact]
        public void DieuDoService_KiemTraXungDot_PhatHienXungDotSE1VaSE2()
        {
            var service = new DieuDoService();
            var dsXungDot = service.KiemTraXungDotToanTuyen(new DateTime(2026, 11, 16));
            Assert.NotEmpty(dsXungDot);
        }

        [Fact]
        public void DieuDoService_KichBan4_RoiXepTranh_RoiKichBan4Lai_VanPhatHienXungDot()
        {
            var service = new DieuDoService();
            DateTime ngay = new DateTime(2026, 11, 16);

            // 1. Kích hoạt Kịch bản 4 lần 1
            bool kb1 = service.KichHoatKichBanDemo(4, ngay, out _);
            Assert.True(kb1);

            // Xung đột phải được phát hiện (SE1 vs SE2)
            var dsXungDot1 = service.KiemTraXungDotToanTuyen(ngay);
            Assert.NotEmpty(dsXungDot1);
            Assert.Contains(dsXungDot1, x => x.SoHieuMacTau1.Contains("SE1") || x.SoHieuMacTau2.Contains("SE1"));

            // 2. Tự động xếp tránh để giải quyết xung đột
            bool okXepTranh = service.TuDongGiaiQuyetTranhTau(ngay, out _);
            Assert.True(okXepTranh);

            // Sau khi xếp tránh, toàn tuyến an toàn (0 xung đột)
            var dsSauXepTranh = service.KiemTraXungDotToanTuyen(ngay);
            Assert.Empty(dsSauXepTranh);

            // 3. Kích hoạt lại Kịch bản 4 lần 2 (Mô phỏng hành vi của Giảng viên / User demo lại lần nữa)
            bool kb2 = service.KichHoatKichBanDemo(4, ngay, out _);
            Assert.True(kb2);

            // Xung đột PHẢI XUẤT HIỆN LẠI ĐẦY ĐỦ, không bị kẹt ở trạng thái 0 xung đột!
            var dsXungDot2 = service.KiemTraXungDotToanTuyen(ngay);
            Assert.NotEmpty(dsXungDot2);
            Assert.Contains(dsXungDot2, x => x.SoHieuMacTau1.Contains("SE1") || x.SoHieuMacTau2.Contains("SE1"));

            // 4. Khôi phục kịch bản 5 để dọn sạch
            service.KichHoatKichBanDemo(5, ngay, out _);
            ThoiGianHeThongHelper.TatMoPhong();
        }
    }
}
