using BUS.Services;
using ET.VanHanh;
using Xunit;

namespace Tests.VanHanh
{
    public class MacTauRangBuocTests
    {
        [Theory]
        [InlineData(0.0, 1726.2, "BAC_NAM")]     // Hà Nội (0.0) -> Sài Gòn (1726.2)
        [InlineData(0.0, 319.2, "BAC_NAM")]      // Hà Nội (0.0) -> Vinh (319.2)
        [InlineData(1726.2, 0.0, "NAM_BAC")]     // Sài Gòn (1726.2) -> Hà Nội (0.0)
        [InlineData(791.4, 0.0, "NAM_BAC")]      // Đà Nẵng (791.4) -> Hà Nội (0.0)
        [InlineData(100.0, 100.0, "")]           // Trùng lý trình
        public void XacDinhHuongChay_TuLyTrinh_DungQuyChuanVNR(double kmDi, double kmDen, string huongMongDoi)
        {
            string huong = MacTauService.XacDinhHuongChay((decimal)kmDi, (decimal)kmDen);
            Assert.Equal(huongMongDoi, huong);
        }

        [Fact]
        public void KiemTraHopLe_SoHieuRong_BaoLoi()
        {
            var service = new MacTauService();
            var m = new MacTauMau
            {
                SoHieuMacTau = "   ",
                LoaiTau = "TAU_NHANH",
                HuongChay = "BAC_NAM",
                MaGaDi = 1,
                MaGaDen = 2,
                ThoiGianChuanGio = 10.0m,
                MucUuTien = 1
            };

            bool hopLe = service.KiemTraHopLe(m, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("không được để trống", loi);
        }

        [Fact]
        public void KiemTraHopLe_GaDiTrungGaDen_BaoLoi()
        {
            var service = new MacTauService();
            var m = new MacTauMau
            {
                SoHieuMacTau = "TEST99",
                LoaiTau = "TAU_NHANH",
                HuongChay = "BAC_NAM",
                MaGaDi = 5,
                MaGaDen = 5,
                ThoiGianChuanGio = 10.0m,
                MucUuTien = 1
            };

            bool hopLe = service.KiemTraHopLe(m, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("Ga xuất phát phải khác Ga về đích", loi);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5.5)]
        [InlineData(150)] // > 100 giờ
        public void KiemTraHopLe_ThoiGianChuanKhongHopLe_BaoLoi(double tgGio)
        {
            var service = new MacTauService();
            var m = new MacTauMau
            {
                SoHieuMacTau = "TEST99",
                LoaiTau = "TAU_NHANH",
                HuongChay = "BAC_NAM",
                MaGaDi = 1,
                MaGaDen = 2,
                ThoiGianChuanGio = (decimal)tgGio,
                MucUuTien = 1
            };

            bool hopLe = service.KiemTraHopLe(m, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("Thời gian chạy chuẩn", loi);
        }

        [Fact]
        public void KiemTraHopLe_LoaiTauKhongHopLe_BaoLoi()
        {
            var service = new MacTauService();
            var m = new MacTauMau
            {
                SoHieuMacTau = "TEST99",
                LoaiTau = "TAU_BAY_VU_TRU",
                HuongChay = "BAC_NAM",
                MaGaDi = 1,
                MaGaDen = 2,
                ThoiGianChuanGio = 10.0m,
                MucUuTien = 1
            };

            bool hopLe = service.KiemTraHopLe(m, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("Loại tàu không hợp lệ", loi);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-1)]
        public void KiemTraHopLe_MucUuTienNgoaiPhamVi_BaoLoi(int uuTien)
        {
            var service = new MacTauService();
            var m = new MacTauMau
            {
                SoHieuMacTau = "TEST99",
                LoaiTau = "TAU_NHANH",
                HuongChay = "BAC_NAM",
                MaGaDi = 1,
                MaGaDen = 2,
                ThoiGianChuanGio = 10.0m,
                MucUuTien = uuTien
            };

            bool hopLe = service.KiemTraHopLe(m, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("Mức ưu tiên", loi);
        }

        [Fact]
        public void MacTauMau_AuditFields_LuuGiuThongTinNguoiDungVaThoiGian()
        {
            var m = new MacTauMau
            {
                NguoiTao = "admin",
                NgayTao = new System.DateTime(2026, 10, 23, 10, 0, 0),
                NguoiCapNhat = "dieu_do_vien",
                NgayCapNhat = new System.DateTime(2026, 10, 23, 15, 30, 0)
            };

            Assert.Equal("admin", m.NguoiTao);
            Assert.Equal("dieu_do_vien", m.NguoiCapNhat);
            Assert.NotEqual(default(System.DateTime), m.NgayTao);
            Assert.True(m.NgayCapNhat.HasValue);
        }

        [Theory]
        [InlineData("A")]                  // 1 ký tự (quá ngắn)
        [InlineData("S")]                  // 1 ký tự
        public void KiemTraHopLe_SoHieuQuaNgan_BaoLoi(string soHieu)
        {
            var service = new MacTauService();
            var m = new MacTauMau
            {
                SoHieuMacTau = soHieu,
                LoaiTau = "TAU_NHANH",
                HuongChay = "BAC_NAM",
                MaGaDi = 1,
                MaGaDen = 2,
                ThoiGianChuanGio = 10.0m,
                MucUuTien = 1
            };

            bool hopLe = service.KiemTraHopLe(m, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("tối thiểu từ 2 ký tự", loi);
        }

        [Fact]
        public void KiemTraHopLe_SoHieuQuaDai_BaoLoi()
        {
            var service = new MacTauService();
            var m = new MacTauMau
            {
                SoHieuMacTau = "2222222222222222222222222222222222222222222333", // 45 ký tự
                LoaiTau = "TAU_NHANH",
                HuongChay = "BAC_NAM",
                MaGaDi = 1,
                MaGaDen = 2,
                ThoiGianChuanGio = 10.0m,
                MucUuTien = 1
            };

            bool hopLe = service.KiemTraHopLe(m, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("không được vượt quá 10 ký tự", loi);
        }

        [Theory]
        [InlineData("SE 1")]               // Chứa khoảng trắng
        [InlineData("SE@1")]               // Chứa ký tự đặc biệt
        [InlineData("TÀU1")]               // Chứa tiếng Việt có dấu
        [InlineData("SE#99")]              // Ký tự thăng
        public void KiemTraHopLe_SoHieuChuaKyTuDacBiet_BaoLoi(string soHieu)
        {
            var service = new MacTauService();
            var m = new MacTauMau
            {
                SoHieuMacTau = soHieu,
                LoaiTau = "TAU_NHANH",
                HuongChay = "BAC_NAM",
                MaGaDi = 1,
                MaGaDen = 2,
                ThoiGianChuanGio = 10.0m,
                MucUuTien = 1
            };

            bool hopLe = service.KiemTraHopLe(m, laThemMoi: true, out string loi);
            Assert.False(hopLe);
            Assert.Contains("chỉ được chứa chữ cái tiếng Anh", loi);
        }

        [Theory]
        [InlineData("VN01")]               // 4 ký tự chuẩn VNR
        [InlineData("VN99")]               // 4 ký tự
        [InlineData("NA99")]               // Tàu khu đoạn mã mới
        [InlineData("HBN-01")]             // Tàu hàng có dấu gạch nối
        [InlineData("HP99")]               // Tàu Hải Phòng mã mới
        public void KiemTraHopLe_SoHieuChuanVNR_HopLe(string soHieu)
        {
            var service = new MacTauService();
            var m = new MacTauMau
            {
                SoHieuMacTau = soHieu,
                LoaiTau = "TAU_NHANH",
                HuongChay = "BAC_NAM",
                MaGaDi = 1,
                MaGaDen = 2,
                ThoiGianChuanGio = 10.0m,
                MucUuTien = 1
            };

            bool hopLe = service.KiemTraHopLe(m, laThemMoi: true, out string loi);
            Assert.True(hopLe, $"Lỗi không mong muốn: {loi}");
            Assert.Empty(loi);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TamDungMacTauVaXuLyChuyen_MaMacKhongHopLe_BaoLoi(int maMac)
        {
            var service = new MacTauService();
            bool kq = service.TamDungMacTauVaXuLyChuyen(maMac, huyChuyenTuongLai: true, lyDoHuy: "Test", out int soChuyenHuy, out string loi);
            Assert.False(kq);
            Assert.Equal(0, soChuyenHuy);
            Assert.Contains("không hợp lệ", loi);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void LayDanhSachChuyenTauKeHoachTuongLai_MaMacKhongHopLe_TraVeBangRong(int maMac)
        {
            var service = new MacTauService();
            var dt = service.LayDanhSachChuyenTauKeHoachTuongLai(maMac);
            Assert.NotNull(dt);
            Assert.Empty(dt.Rows);
        }
    }
}
