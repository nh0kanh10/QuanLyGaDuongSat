using System;
using System.Transactions;
using BUS.Services;
using Xunit;

namespace Tests.VanHanh
{
    public class DieuDoXepTranhTests
    {
        private readonly DieuDoService _dieuDoService = new();
        private readonly Xunit.Abstractions.ITestOutputHelper _output;

        public DieuDoXepTranhTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ThuatToanXepTranh_KiemTraChiTietNgay1410()
        {
            DateTime ngay = new DateTime(2026, 10, 14);
            var dsXungDot = _dieuDoService.KiemTraXungDotToanTuyen(ngay);

            _output.WriteLine($"=== TỔNG SỐ XUNG ĐỘT NGÀY 14/10/2026: {dsXungDot.Count} ===");
            foreach (var xd in dsXungDot)
            {
                _output.WriteLine($"Xung đột #{xd.MaXungDot}: {xd.SoHieuMacTau1} ({xd.LoaiTau1}) ✕ {xd.SoHieuMacTau2} ({xd.LoaiTau2})");
                _output.WriteLine($"  - Loại: {xd.LoaiXungDot} tại Km {xd.LyTrinhUocTinhKm:F1} (Khu gian {xd.TenGaDau} - {xd.TenGaCuoi}) lúc {xd.ThoiDiemXungDot:dd/MM HH:mm}");
                _output.WriteLine($"  - Đề xuất: Tàu {xd.SoHieuTauBiTranh} dừng tại {xd.TenGaDeXuatTranh} (Ray #{xd.SoHieuDuongRayDeXuat})");
                _output.WriteLine($"  - Giờ đến: {xd.GioDenGaTranh:HH:mm}, Giờ đi: {xd.GioDiGaTranh:HH:mm} -> Đỗ: {xd.SoPhutDungDeXuat} phút, Tịnh tiến: +{xd.SoPhutTangLichTrinh} phút");
                _output.WriteLine("------------------------------------------------------------");

                // Kiểm tra ràng buộc an toàn: thời gian tịnh tiến lùi giờ không vượt quá 35 phút (chống lỗi 115 hay 310 phút)
                Assert.InRange(xd.SoPhutTangLichTrinh, 5, 35);
                Assert.InRange(xd.SoPhutDungDeXuat, 15, 60);
            }
        }

        [Fact]
        public void ThuatToanXepTranh_KiemTraChiTietNgay2610()
        {
            DateTime ngay = new DateTime(2026, 10, 26);
            var dsXungDot = _dieuDoService.KiemTraXungDotToanTuyen(ngay);

            _output.WriteLine($"=== TỔNG SỐ XUNG ĐỘT NGÀY 26/10/2026: {dsXungDot.Count} ===");
            foreach (var xd in dsXungDot)
            {
                _output.WriteLine($"Xung đột #{xd.MaXungDot}: {xd.SoHieuMacTau1} ({xd.LoaiTau1}) ✕ {xd.SoHieuMacTau2} ({xd.LoaiTau2})");
                _output.WriteLine($"  - Loại: {xd.LoaiXungDot} tại Km {xd.LyTrinhUocTinhKm:F1} (Khu gian {xd.TenGaDau} - {xd.TenGaCuoi}) lúc {xd.ThoiDiemXungDot:dd/MM HH:mm}");
                _output.WriteLine($"  - Đề xuất: Tàu {xd.SoHieuTauBiTranh} dừng tại {xd.TenGaDeXuatTranh} (Ray #{xd.SoHieuDuongRayDeXuat})");
                _output.WriteLine($"  - Giờ đến: {xd.GioDenGaTranh:HH:mm}, Giờ đi: {xd.GioDiGaTranh:HH:mm} -> Đỗ: {xd.SoPhutDungDeXuat} phút, Tịnh tiến: +{xd.SoPhutTangLichTrinh} phút");
                _output.WriteLine("------------------------------------------------------------");

                // Đảm bảo không còn hiện tượng đề xuất giờ đỗ 115 hay 310 phút
                Assert.InRange(xd.SoPhutTangLichTrinh, 5, 35);
                Assert.InRange(xd.SoPhutDungDeXuat, 15, 60);
            }
        }

        [Fact]
        public void ThuatToanXepTranh_GiaiQuyetXungDot_ThucThiThanhCongKhongLoiTrigger()
        {
            DateTime ngay = new DateTime(2026, 10, 14);
            var dsXungDot = _dieuDoService.KiemTraXungDotToanTuyen(ngay);

            Assert.NotEmpty(dsXungDot);

            var xd = dsXungDot[0];
            _output.WriteLine($"Thử nghiệm giải quyết xung đột {xd.SoHieuMacTau1} ✕ {xd.SoHieuMacTau2} tại Ga {xd.TenGaDeXuatTranh}...");
            try
            {
                bool ok = _dieuDoService.GiaiQuyetMotXungDot(xd, out string baoCao);
                _output.WriteLine($"Kết quả thực thi: {(ok ? "THÀNH CÔNG" : "THẤT BẠI")}");
                _output.WriteLine($"Báo cáo chi tiết:\n{baoCao}");
                Assert.True(ok, $"Lỗi khi xếp tránh: {baoCao}");
            }
            finally
            {
                // Hoàn trả (revert) dữ liệu chuyến tàu 6 (NA1) về nguyên trạng để không làm bẩn DB
                string sqlReset = @"
                    UPDATE vanhanh.LichDungGa SET GioDenKeHoach = '2026-10-14 21:55:00', GioDiKeHoach = '2026-10-14 22:15:00', LaDiemTranh = 0, MaDuongRay = NULL WHERE MaChuyenTau = 6 AND ThuTuDung = 1;
                    UPDATE vanhanh.LichDungGa SET GioDenKeHoach = '2026-10-14 22:20:00', GioDiKeHoach = '2026-10-14 22:40:00', LaDiemTranh = 0, MaDuongRay = NULL WHERE MaChuyenTau = 6 AND ThuTuDung = 2;
                    UPDATE vanhanh.LichDungGa SET GioDenKeHoach = '2026-10-14 23:37:00', GioDiKeHoach = '2026-10-14 23:40:00', LaDiemTranh = 0, MaDuongRay = NULL WHERE MaChuyenTau = 6 AND ThuTuDung = 3;
                    UPDATE vanhanh.LichDungGa SET GioDenKeHoach = '2026-10-15 00:10:00', GioDiKeHoach = '2026-10-15 00:15:00', LaDiemTranh = 0, MaDuongRay = NULL WHERE MaChuyenTau = 6 AND ThuTuDung = 4;
                    UPDATE vanhanh.LichDungGa SET GioDenKeHoach = '2026-10-15 00:45:00', GioDiKeHoach = '2026-10-15 00:48:00', LaDiemTranh = 0, MaDuongRay = NULL WHERE MaChuyenTau = 6 AND ThuTuDung = 5;
                    UPDATE vanhanh.LichDungGa SET GioDenKeHoach = '2026-10-15 01:50:00', GioDiKeHoach = '2026-10-15 01:55:00', LaDiemTranh = 0, MaDuongRay = NULL WHERE MaChuyenTau = 6 AND ThuTuDung = 6;
                    UPDATE vanhanh.LichDungGa SET GioDenKeHoach = '2026-10-15 06:00:00', GioDiKeHoach = '2026-10-15 06:00:00', LaDiemTranh = 0, MaDuongRay = NULL WHERE MaChuyenTau = 6 AND ThuTuDung = 7;
                    UPDATE vanhanh.ChuyenTau SET GioVeDichKH = '2026-10-15 06:00:00', SoPhutTreLuyKe = 0 WHERE MaChuyenTau = 6;";
                DAL.Connection.DatabaseHelper.ExecuteNonQuery(sqlReset);
            }
        }
    }
}
