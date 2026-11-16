using System;
using Xunit;
using DTO.Common;

namespace Tests.VanHanh
{
    public class BanVeRangBuocTests
    {
        [Theory]
        [InlineData(0, 06, 10, 05, 59, true)]   // Tàu chạy 06:10, lúc 05:59 (còn 11 phút) -> Cho phép lấy lên bán vé
        [InlineData(0, 06, 10, 06, 00, true)]   // Tàu chạy 06:10, lúc 06:00 (đúng 10 phút) -> Vừa tròn ngưỡng 10p -> Cho phép
        [InlineData(0, 06, 10, 06, 01, false)]  // Tàu chạy 06:10, lúc 06:01 (còn 9 phút) -> Sát giờ (< 10p) -> Không lấy lên
        [InlineData(0, 06, 10, 06, 06, false)]  // Tàu chạy 06:10, lúc 06:06 (còn 4 phút) -> Sát giờ (< 10p) -> Không lấy lên
        [InlineData(0, 06, 10, 06, 15, false)]  // Tàu chạy 06:10, lúc 06:15 (đã qua giờ chạy) -> Không lấy lên
        [InlineData(20, 06, 10, 06, 06, true)]  // Tàu 06:10 trễ 20p (chạy 06:30), lúc 06:06 (còn 24p) -> Vẫn cho phép
        [InlineData(20, 06, 10, 06, 20, true)]  // Tàu 06:10 trễ 20p (chạy 06:30), lúc 06:20 (còn 10p) -> Vừa tròn ngưỡng -> Cho phép
        [InlineData(20, 06, 10, 06, 21, false)] // Tàu 06:10 trễ 20p (chạy 06:30), lúc 06:21 (còn 9p) -> Sát giờ (< 10p) -> Không lấy lên
        public void KiemTraKhoaBanVeSatGio_TheoGioKHVaSoPhutTre(
            int soPhutTre, 
            int gioKH, int phutKH, 
            int gioXet, int phutXet, 
            bool mongDoiDuocLayLen)
        {
            DateTime ngayDi = new DateTime(2026, 11, 16);
            DateTime gioDiKH = ngayDi.AddHours(gioKH).AddMinutes(phutKH);
            DateTime gioDiDuKien = gioDiKH.AddMinutes(soPhutTre);
            DateTime thoiDiemXet = ngayDi.AddHours(gioXet).AddMinutes(phutXet);

            // Logic truy van SQL: DATEADD(MINUTE, -10, gioDiDuKien) >= thoiDiemXet
            bool duocLayLen = gioDiDuKien.AddMinutes(-10) >= thoiDiemXet;

            Assert.Equal(mongDoiDuocLayLen, duocLayLen);
        }

        [Fact]
        public void KiemTraGioMoPhong_TauSE1_Luc06h06_BiKhoaBanVe()
        {
            // Tàu SE1 xuất phát lúc 06:10 ngày 16/11/2026
            DateTime gioSE1 = new DateTime(2026, 11, 16, 6, 10, 0);

            // Đồng hồ vận hành mô phỏng đang là 06:06:00
            DateTime gioHeThong = new DateTime(2026, 11, 16, 6, 6, 0);

            // Thời điểm chốt sổ quầy vé: trước giờ chạy 10 phút (06:00:00)
            DateTime gioChotBanVe = gioSE1.AddMinutes(-10);

            bool conDuocBan = gioHeThong <= gioChotBanVe;

            // Lúc 06:06 thì đã vượt qua 06:00 -> Không được bán vé
            Assert.False(conDuocBan);
        }
    }
}
