using System;
using Xunit;
using DTO.Common;
using DAL.Repositories;

namespace Tests.VanHanh
{
    public class SoatVeRangBuocTests
    {
        [Fact]
        public void CapNhatTrangThaiSoatVe_MaVeKhongTonTai_TraVeFalseVaBaoLoi()
        {
            var repo = new VeRepository();
            bool result = repo.CapNhatTrangThaiSoatVe(-99999, "DA_LEN_TAU", 1, out string err);
            Assert.False(result);
            Assert.False(string.IsNullOrWhiteSpace(err));
            Assert.Contains("không hợp lệ", err);
        }

        [Theory]
        [InlineData(0, 0, false)]    // 0 phút trễ, hiện tại = giờ đi -> chưa quá 5p -> chưa đóng cửa
        [InlineData(0, 5, false)]    // 0 phút trễ, hiện tại = giờ đi + 5p -> vừa tròn 5p -> chưa đóng cửa
        [InlineData(0, 6, true)]     // 0 phút trễ, hiện tại = giờ đi + 6p -> quá 5p -> đã đóng cửa
        [InlineData(30, 30, false)]  // Trễ 30p, hiện tại = giờ đi + 30p -> vừa tới giờ tàu mới -> chưa đóng cửa
        [InlineData(30, 35, false)]  // Trễ 30p, hiện tại = giờ đi + 35p -> trong 5p đệm -> chưa đóng cửa
        [InlineData(30, 36, true)]   // Trễ 30p, hiện tại = giờ đi + 36p -> quá 5p đệm của giờ trễ -> đã đóng cửa
        public void KiemTraDongCuaSoatVe_TheoGioKHVaSoPhutTre(int soPhutTre, int phutTroiQua, bool mongDoiDaDongCua)
        {
            DateTime gioDiKH = new DateTime(2026, 11, 16, 8, 0, 0);
            DateTime gioDiDuKien = gioDiKH.AddMinutes(soPhutTre);
            DateTime now = gioDiKH.AddMinutes(phutTroiQua);

            bool daDongCua = now > gioDiDuKien.AddMinutes(5);
            Assert.Equal(mongDoiDaDongCua, daDongCua);
        }

        [Theory]
        [InlineData(-1, false)] // Hôm qua -> Quá hạn
        [InlineData(1, false)]  // Ngày mai -> Chưa đến ngày
        [InlineData(0, true)]   // Hôm nay -> Đúng ngày
        public void KiemTraNgayKhoiHanh_SoVoiNgayHienTai(int doLechNgay, bool hopLe)
        {
            DateTime now = new DateTime(2026, 11, 16, 10, 0, 0);
            DateTime ngayKhoiHanh = now.Date.AddDays(doLechNgay);

            bool quaHan = ngayKhoiHanh < now.Date;
            bool chuaDenNgay = ngayKhoiHanh > now.Date;
            bool dungNgay = (ngayKhoiHanh == now.Date);

            Assert.Equal(hopLe, dungNgay);
            if (doLechNgay < 0) Assert.True(quaHan);
            if (doLechNgay > 0) Assert.True(chuaDenNgay);
        }

        [Theory]
        [InlineData(-121, false)] // Đến sớm 121 phút (> 2 tiếng) -> Chưa mở cửa
        [InlineData(-120, true)]  // Đến sớm đúng 120 phút -> Đã mở cửa
        [InlineData(-60, true)]   // Đến sớm 60 phút -> Đã mở cửa
        [InlineData(0, true)]     // Đúng giờ tàu chạy -> Đã mở cửa
        [InlineData(5, true)]     // 5 phút đệm sau giờ chạy -> Vẫn còn mở
        [InlineData(6, false)]    // 6 phút sau giờ chạy -> Đã đóng cửa
        public void KiemTraKhungGioMoCuaSoatVe_Tu120PhutTruocDen5PhutSau(int phutSoVoiGioChay, bool trongKhungMoCua)
        {
            DateTime gioDi = new DateTime(2026, 11, 16, 14, 0, 0);
            DateTime now = gioDi.AddMinutes(phutSoVoiGioChay);

            bool quaSom = now < gioDi.AddMinutes(-120);
            bool quaMuon = now > gioDi.AddMinutes(5);
            bool duocVao = !quaSom && !quaMuon;

            Assert.Equal(trongKhungMoCua, duocVao);
        }

        [Fact]
        public void ThoiGianHeThongHelper_DongBoGioThanhCong()
        {
            DateTime thoiDiemGiaLap = new DateTime(2026, 11, 16, 14, 30, 0);
            ThoiGianHeThongHelper.CheDoMoPhong = true;
            ThoiGianHeThongHelper.GioMoPhong = thoiDiemGiaLap;

            DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
            Assert.Equal(2026, now.Year);
            Assert.Equal(11, now.Month);
            Assert.Equal(16, now.Day);
            Assert.Equal(14, now.Hour);
            Assert.Equal(30, now.Minute);

            // Reset ve gio that
            ThoiGianHeThongHelper.CheDoMoPhong = false;
        }

        [Fact]
        public void VeService_SoatVe_KhiQua5PhutGioChay_BiChanVaTraVeLoiCuaDong()
        {
            var repo = new VeRepository();
            repo.CapNhatTrangThaiSoatVe(1, "DA_DAT", null, out _);

            var veService = new BUS.Services.VeService();
            // Đặt thời gian mô phỏng là 07:15 (Tàu SE1 xuất phát Hà Nội lúc 06:10, đã quá 65 phút)
            ThoiGianHeThongHelper.BatMoPhong(new DateTime(2026, 11, 16, 7, 15, 0));
            try
            {
                // Vé 1: TK_SE1_HN_VIN_01 (chuyến SE1, ga đón Hà Nội)
                bool thanhCong = veService.SoatVe(1, 1, out string loi);
                Assert.False(thanhCong);
                Assert.Contains("Cửa kiểm soát đã đóng", loi);
            }
            finally
            {
                ThoiGianHeThongHelper.TatMoPhong();
                repo.CapNhatTrangThaiSoatVe(1, "DA_DAT", null, out _);
            }
        }

        [Fact]
        public void VeService_SoatVe_KhiSaiGaKiemSoat_BiChanVaTraVeLoiSaiGa()
        {
            var repo = new VeRepository();
            repo.CapNhatTrangThaiSoatVe(1, "DA_DAT", null, out _);

            var veService = new BUS.Services.VeService();
            // Đặt thời gian mô phỏng trong khung giờ mở cửa đón của Hà Nội (05:30)
            ThoiGianHeThongHelper.BatMoPhong(new DateTime(2026, 11, 16, 5, 30, 0));
            try
            {
                // Vé 1 đón ở Ga 1 (Hà Nội) nhưng lại đem soát ở Ga 3 (Phủ Lý)
                bool thanhCong = veService.SoatVe(1, 3, out string loi);
                Assert.False(thanhCong);
                Assert.Contains("không khớp ga kiểm soát", loi);
            }
            finally
            {
                ThoiGianHeThongHelper.TatMoPhong();
                repo.CapNhatTrangThaiSoatVe(1, "DA_DAT", null, out _);
            }
        }
    }
}
