using BUS.Services;
using ET.VanHanh;
using Xunit;

namespace Tests.VanHanh
{
    public class ChuyenTauRangBuocTests
    {
        [Theory]
        [InlineData("DA_LEN_LICH", "SAN_SANG", true)]
        [InlineData("DA_LEN_LICH", "DA_HUY", true)]
        [InlineData("SAN_SANG", "DANG_CHAY", true)]
        [InlineData("SAN_SANG", "DA_LEN_LICH", true)]
        [InlineData("DANG_CHAY", "HOAN_THANH", true)]
        [InlineData("DANG_CHAY", "TRE_GIO", true)]
        [InlineData("TRE_GIO", "HOAN_THANH", true)]
        [InlineData("TRE_GIO", "DANG_CHAY", true)]
        [InlineData("HOAN_THANH", "DA_LEN_LICH", false)]
        [InlineData("HOAN_THANH", "DANG_CHAY", false)]
        [InlineData("DA_HUY", "DANG_CHAY", false)]
        [InlineData("DA_HUY", "SAN_SANG", false)]
        [InlineData("DA_LEN_LICH", "HOAN_THANH", false)]
        public void KiemTraChuyenTrangThai_DungQuyTrinh(string cu, string moi, bool hopLeMongDoi)
        {
            bool hopLe = ChuyenTauService.KiemTraChuyenTrangThai(cu, moi, out string loi);
            Assert.Equal(hopLeMongDoi, hopLe);
            if (!hopLeMongDoi)
            {
                Assert.False(string.IsNullOrWhiteSpace(loi));
            }
        }

        [Fact]
        public void LuuDoanTau_DauMayChinhTrungDauMayDay_BaoLoi()
        {
            var service = new ChuyenTauService();
            var dt = new DoanTau
            {
                MaChuyenTau = 1,
                MaDauMayChinh = 5,
                MaDauMayDay = 5, // Cùng đầu máy
                TongSoToa = 10,
                TongChieuDaiM = 220,
                TongTrongLuongTan = 450
            };

            bool kq = service.LuuDoanTau(dt, out string loi);

            Assert.False(kq);
            Assert.Contains("trùng nhau", loi);
        }

        [Fact]
        public void LuuDoanTau_ThieuDauMayChinh_BaoLoi()
        {
            var service = new ChuyenTauService();
            var dt = new DoanTau
            {
                MaChuyenTau = 1,
                MaDauMayChinh = 0, // Không có đầu máy chính
                MaDauMayDay = null,
                TongSoToa = 10,
                TongChieuDaiM = 220,
                TongTrongLuongTan = 450
            };

            bool kq = service.LuuDoanTau(dt, out string loi);

            Assert.False(kq);
            Assert.Contains("Đầu Máy Kéo Chính", loi);
        }

        [Fact]
        public void LuuDoanTau_VuotChieuDaiQuyChuan_BaoLoi()
        {
            var service = new ChuyenTauService();
            var dt = new DoanTau
            {
                MaChuyenTau = 1,
                MaDauMayChinh = 1,
                TongSoToa = 15,
                TongChieuDaiM = 480.0m, // Vượt 450m
                TongTrongLuongTan = 500
            };

            bool kq = service.LuuDoanTau(dt, out string loi);

            Assert.False(kq);
            Assert.Contains("vượt quá trần quy chuẩn", loi);
        }

        [Fact]
        public void LuuDoanTau_SoToaVuotQua20_BaoLoi()
        {
            var service = new ChuyenTauService();
            var dt = new DoanTau
            {
                MaChuyenTau = 1,
                MaDauMayChinh = 1,
                TongSoToa = 25, // > 20 toa
                TongChieuDaiM = 400.0m,
                TongTrongLuongTan = 500
            };

            bool kq = service.LuuDoanTau(dt, out string loi);

            Assert.False(kq);
            Assert.Contains("từ 1 đến 20 toa", loi);
        }

        [Fact]
        public void ThemChuyenTau_GioVeDichTruocGioXuatPhat_BaoLoi()
        {
            var service = new ChuyenTauService();
            var ct = new ChuyenTau
            {
                MaMacTau = 1,
                NgayXuatPhat = DateTime.Today.AddDays(1),
                GioXuatPhatKH = DateTime.Today.AddDays(1).AddHours(10),
                GioVeDichKH = DateTime.Today.AddDays(1).AddHours(9), // Giờ đến trước giờ đi
                TrangThai = "DA_LEN_LICH"
            };

            int id = service.Them(ct, out string loi);

            Assert.Equal(0, id);
            Assert.Contains("sau giờ xuất phát", loi);
        }

        [Fact]
        public void ThemChuyenTau_ThoiGianChayDuoi30Phut_BaoLoi()
        {
            var service = new ChuyenTauService();
            var ct = new ChuyenTau
            {
                MaMacTau = 1,
                NgayXuatPhat = DateTime.Today.AddDays(1),
                GioXuatPhatKH = DateTime.Today.AddDays(1).AddHours(6),
                GioVeDichKH = DateTime.Today.AddDays(1).AddHours(6).AddMinutes(15), // Chạy 15 phút
                TrangThai = "DA_LEN_LICH"
            };

            int id = service.Them(ct, out string loi);

            Assert.Equal(0, id);
            Assert.Contains("30 phút trở lên", loi);
        }

        [Fact]
        public void ThemLichDungGa_GioDiTruocGioDen_BaoLoi()
        {
            var service = new ChuyenTauService();
            var ld = new LichDungGa
            {
                MaChuyenTau = 1,
                MaGa = 2,
                ThuTuDung = 2,
                GioDenKeHoach = DateTime.Today.AddHours(8),
                GioDiKeHoach = DateTime.Today.AddHours(7).AddMinutes(50) // Rời ga trước khi đến
            };

            bool kq = service.ThemLichDungGa(ld, out string loi);

            Assert.False(kq);
            Assert.Contains("không được trước giờ đến", loi);
        }

        [Fact]
        public void ThemLichDungGa_ThoiGianDungVuotQua12Gio_BaoLoi()
        {
            var service = new ChuyenTauService();
            var ld = new LichDungGa
            {
                MaChuyenTau = 1,
                MaGa = 2,
                ThuTuDung = 2,
                GioDenKeHoach = DateTime.Today.AddHours(8),
                GioDiKeHoach = DateTime.Today.AddHours(22) // Dừng 14 tiếng tại ga
            };

            bool kq = service.ThemLichDungGa(ld, out string loi);

            Assert.False(kq);
            Assert.Contains("vượt quá 12 giờ", loi);
        }

        [Fact]
        public void DayThuTuDung_ThamSoKhongHopLe_TraVeFalse()
        {
            var service = new ChuyenTauService();
            Assert.False(service.DayThuTuDung(0, 1));
            Assert.False(service.DayThuTuDung(1, 0));
            Assert.False(service.DayThuTuDung(-1, -1));
        }

        [Theory]
        [InlineData("DANG_CHAY", false)]
        [InlineData("TRE_GIO", false)]
        [InlineData("HOAN_THANH", false)]
        [InlineData("DA_LEN_LICH", true)]
        [InlineData("SAN_SANG", true)]
        [InlineData("DA_HUY", true)]
        public void KiemTraCoTheXoaChuyen_TrangThaiDangChayHoacHoanThanh_BaoLoi(string trangThai, bool choPhepXoaMongDoi)
        {
            bool coTheXoa = ChuyenTauService.KiemTraCoTheXoaChuyen(trangThai, out string loi);
            Assert.Equal(choPhepXoaMongDoi, coTheXoa);
            if (!choPhepXoaMongDoi)
            {
                Assert.Contains("không được phép xóa", loi);
            }
        }

        [Fact]
        public void TinhToanGioVeDich_TuyenBacNamXuyenDem_TinhDungNgay()
        {
            DateTime ngayDi = new DateTime(2026, 10, 24);
            TimeSpan gioDi = new TimeSpan(4, 0, 0); // 04:00
            TimeSpan gioDen = new TimeSpan(9, 0, 0); // 09:00 hôm sau
            decimal thoiGianChuan = 29.0m; // 29 tiếng chạy chuẩn

            DateTime dtDen = ChuyenTauService.TinhToanGioVeDich(ngayDi, gioDi, gioDen, thoiGianChuan);

            // Dự kiến về đích vào ngày 25/10 lúc 09:00 (chạy 29 tiếng)
            Assert.Equal(new DateTime(2026, 10, 25, 9, 0, 0), dtDen);
            Assert.Equal(29.0, (dtDen - ngayDi.Add(gioDi)).TotalHours);
        }

        [Fact]
        public void TinhSoPhutTre_GioXuatPhatTre_TinhDungPhut()
        {
            DateTime gioKH = new DateTime(2026, 10, 24, 6, 0, 0);
            DateTime gioTT = new DateTime(2026, 10, 24, 6, 45, 0);

            int phutTre = ChuyenTauService.TinhSoPhutTre(gioKH, gioTT);
            Assert.Equal(45, phutTre);

            // Nếu chạy đúng giờ hoặc sớm hơn thì trễ = 0
            DateTime gioTTSom = new DateTime(2026, 10, 24, 5, 50, 0);
            Assert.Equal(0, ChuyenTauService.TinhSoPhutTre(gioKH, gioTTSom));
        }

        [Fact]
        public void KiemTraGioThucTeHopLe_GioTrongTuongLai_BaoLoi()
        {
            DateTime gioTuongLai = DateTime.Now.AddDays(2);
            bool hopLe = ChuyenTauService.KiemTraGioThucTeHopLe(gioTuongLai, null, out string loi);

            Assert.False(hopLe);
            Assert.Contains("tương lai", loi);
        }

        [Fact]
        public void KiemTraThoiGianChayHopLy_QuaNgan_BaoCanhBao()
        {
            TimeSpan thoiGianChay = TimeSpan.FromHours(5); // Chạy 5 tiếng
            decimal thoiGianChuan = 32.0m; // Chuẩn 32 tiếng (quá ngắn < 40%)

            bool hopLe = ChuyenTauService.KiemTraThoiGianChayHopLy(thoiGianChay, thoiGianChuan, out string canhBao);

            Assert.False(hopLe);
            Assert.Contains("ngắn bất thường", canhBao);
        }

        [Fact]
        public void CapNhatLichDungGa_GioDiTruocGioDen_BaoLoi()
        {
            var service = new ChuyenTauService();
            var ld = new LichDungGa
            {
                MaDiemDung = 1,
                MaChuyenTau = 1,
                MaGa = 1,
                ThuTuDung = 2,
                GioDenKeHoach = new DateTime(2026, 10, 24, 10, 0, 0),
                GioDiKeHoach = new DateTime(2026, 10, 24, 9, 30, 0) // Giờ đi trước giờ đến
            };

            bool kq = service.CapNhatLichDungGa(ld, out string loi);

            Assert.False(kq);
            Assert.Contains("không được trước giờ đến", loi);
        }

        [Fact]
        public void CapNhatLichDungGa_DoGaQua12Gio_BaoLoi()
        {
            var service = new ChuyenTauService();
            var ld = new LichDungGa
            {
                MaDiemDung = 1,
                MaChuyenTau = 1,
                MaGa = 1,
                ThuTuDung = 2,
                GioDenKeHoach = new DateTime(2026, 10, 24, 6, 0, 0),
                GioDiKeHoach = new DateTime(2026, 10, 24, 19, 0, 0) // Đỗ 13 tiếng > 12h
            };

            bool kq = service.CapNhatLichDungGa(ld, out string loi);

            Assert.False(kq);
            Assert.Contains("vượt quá 12 giờ", loi);
        }

        [Fact]
        public void CapNhatLichDungGa_MaKhongHopLe_BaoLoi()
        {
            var service = new ChuyenTauService();
            var ld = new LichDungGa
            {
                MaDiemDung = 0,
                MaChuyenTau = 0
            };

            bool kq = service.CapNhatLichDungGa(ld, out string loi);

            Assert.False(kq);
            Assert.Contains("không hợp lệ", loi);
        }
    }
}

