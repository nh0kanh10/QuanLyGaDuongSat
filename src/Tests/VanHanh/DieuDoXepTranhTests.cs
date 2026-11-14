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

                // Đảm bảo thời gian dừng đỗ tuân thủ quy chuẩn an toàn tối thiểu 15 phút và thời gian tịnh tiến hợp lệ
                Assert.True(xd.SoPhutDungDeXuat >= 15, $"Thời gian dừng đề xuất phải >= 15 phút (thực tế: {xd.SoPhutDungDeXuat})");
                Assert.True(xd.SoPhutTangLichTrinh >= 0, $"Thời gian tịnh tiến phải >= 0 (thực tế: {xd.SoPhutTangLichTrinh})");
                Assert.False(string.IsNullOrWhiteSpace(xd.TenGaDeXuatTranh), "Phải xác định được ga đề xuất tránh");
                // Chờ quá ngưỡng (khu gian dài không có ga tránh) thì phải báo cho trực ban, không lặng lẽ đề xuất
                if (xd.SoPhutTangLichTrinh > DieuDoService.SoPhutChoToiDaGiuUuTien)
                    Assert.Contains("CẢNH BÁO", xd.MoTaChiTiet);
            }
        }

        [Fact]
        public void ThuatToanXepTranh_GiaiQuyetXungDot_ThucThiThanhCongKhongLoiTrigger()
        {
            DateTime ngay = new DateTime(2026, 10, 26);
            var dsXungDot = _dieuDoService.KiemTraXungDotToanTuyen(ngay);
            if (dsXungDot.Count == 0) return;

            var xd = dsXungDot[0];
            _output.WriteLine($"Thử nghiệm giải quyết xung đột {xd.SoHieuMacTau1} ✕ {xd.SoHieuMacTau2} tại Ga {xd.TenGaDeXuatTranh}...");

            // Sử dụng TransactionScope không Complete để tự động rollback mọi thay đổi DB sau khi test xong
            using (var scope = new System.Transactions.TransactionScope())
            {
                bool ok = _dieuDoService.GiaiQuyetMotXungDot(xd, out string baoCao);
                _output.WriteLine($"Kết quả thực thi: {(ok ? "THÀNH CÔNG" : "THẤT BẠI")}");
                _output.WriteLine($"Báo cáo chi tiết:\n{baoCao}");
                Assert.True(ok, $"Lỗi khi xếp tránh: {baoCao}");
            }
        }
    }
}
