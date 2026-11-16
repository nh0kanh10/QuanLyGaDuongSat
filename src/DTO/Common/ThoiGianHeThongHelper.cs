using System;

namespace DTO.Common
{
    // =========================================================================
    // HELPER THỜI GIAN TOÀN HỆ THỐNG & ĐIỀU HÀNH MÔ PHỎNG (SIMULATION CLOCK)
    //
    // Giúp đồng bộ thời gian giữa:
    //  - Phân hệ Điều hành chạy tàu thực tế (CTC Console)
    //  - Phân hệ Lịch trình & Biểu đồ chạy tàu
    //  - Phân hệ Bán vé & Soát vé hành khách
    //
    // Khi CheDoMoPhong = false: LayGioHienTai() trả về DateTime.Now máy tính thật.
    // Khi CheDoMoPhong = true: LayGioHienTai() trả về GioMoPhong (chạy theo tốc độ x1, x30, x60 hoặc nhảy mốc kịch bản).
    // =========================================================================
    public static class ThoiGianHeThongHelper
    {
        private static bool _cheDoMoPhong = false;
        private static DateTime _gioMoPhong = DateTime.Now;

        /// <summary>
        /// Trạng thái bật/tắt chế độ mô phỏng vận hành
        /// </summary>
        public static bool CheDoMoPhong
        {
            get => _cheDoMoPhong;
            set
            {
                if (_cheDoMoPhong != value)
                {
                    _cheDoMoPhong = value;
                    OnThoiGianThayDoi(LayGioHienTai());
                }
            }
        }

        /// <summary>
        /// Mốc thời gian giả lập khi ở chế độ mô phỏng
        /// </summary>
        public static DateTime GioMoPhong
        {
            get => _gioMoPhong;
            set
            {
                _gioMoPhong = value;
                if (_cheDoMoPhong)
                {
                    OnThoiGianThayDoi(_gioMoPhong);
                }
            }
        }

        /// <summary>
        /// Lấy thời điểm hiện tại của hệ thống (có tính đến chế độ mô phỏng)
        /// </summary>
        public static DateTime LayGioHienTai()
        {
            return _cheDoMoPhong ? _gioMoPhong : DateTime.Now;
        }

        /// <summary>
        /// Sự kiện phát ra khi thời gian hệ thống hoặc mốc giả lập thay đổi
        /// </summary>
        public static event Action<DateTime>? ThoiGianThayDoi;

        private static void OnThoiGianThayDoi(DateTime thoiGianMoi)
        {
            try
            {
                ThoiGianThayDoi?.Invoke(thoiGianMoi);
            }
            catch
            {
                // Phòng vệ tránh crash nếu một listener gặp exception
            }
        }

        /// <summary>
        /// Kích hoạt chế độ mô phỏng với thời điểm bắt đầu chỉ định
        /// </summary>
        public static void BatMoPhong(DateTime thoiDiemKhoiTao)
        {
            _gioMoPhong = thoiDiemKhoiTao;
            _cheDoMoPhong = true;
            OnThoiGianThayDoi(_gioMoPhong);
        }

        /// <summary>
        /// Tắt chế độ mô phỏng, quay trở về giờ máy tính thật
        /// </summary>
        public static void TatMoPhong()
        {
            _cheDoMoPhong = false;
            OnThoiGianThayDoi(DateTime.Now);
        }
    }
}
