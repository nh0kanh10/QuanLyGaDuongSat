using System;
using System.Data;
using DAL.Repositories;
using ET.HaTang;

namespace BUS.Services
{
    public class KhuGianService
    {
        private readonly KhuGianRepository _repo = new();

        public DataTable LayDanhSach() => _repo.LayDanhSach();

        public DataTable LayTheoGa(int maGa) => _repo.LayTheoGa(maGa);

        /// <summary>
        /// Lấy danh sách ga liền kề (tối đa 2) chưa có khu gian nối với ga được chọn.
        /// Dùng cho Smart UX: auto-filter ComboBox Ga Cuối.
        /// </summary>
        public DataTable LayGaLienKeChuaCoKhuGian(int maGa, int maKhuGianBoQua = 0)
            => _repo.LayGaLienKeChuaCoKhuGian(maGa, maKhuGianBoQua);

        public bool Them(KhuGian kg, out string error)
        {
            error = "";
            if (kg.MaGaDau <= 0 || kg.MaGaCuoi <= 0)
            {
                error = "Vui lòng chỉ định cả Ga Đầu và Ga Cuối của phân đoạn khu gian.";
                return false;
            }

            if (kg.MaGaDau == kg.MaGaCuoi)
            {
                error = "Ga Đầu và Ga Cuối không được trùng nhau.";
                return false;
            }

            // Tự động chuẩn hóa thứ tự ga để tuân thủ ràng buộc toàn vẹn CSDL CK_KhuGian_ThuTuChuan (MaGaDau < MaGaCuoi)
            if (kg.MaGaDau > kg.MaGaCuoi)
            {
                int temp = kg.MaGaDau;
                kg.MaGaDau = kg.MaGaCuoi;
                kg.MaGaCuoi = temp;
            }

            if (kg.CuLyKm <= 0 || kg.CuLyKm > 999.99m)
            {
                error = "Cự ly phân đoạn khu gian phải lớn hơn 0 và không vượt quá 999.99 km.";
                return false;
            }

            if (kg.TocDoToiDaKhach < 10 || kg.TocDoToiDaKhach > 200)
            {
                error = "Tốc độ tối đa tàu khách không hợp lệ (cho phép từ 10 đến 200 km/h).";
                return false;
            }

            if (kg.TocDoToiDaHang < 10 || kg.TocDoToiDaHang > 150)
            {
                error = "Tốc độ tối đa tàu hàng không hợp lệ (cho phép từ 10 đến 150 km/h).";
                return false;
            }

            if (kg.TocDoToiDaHang > kg.TocDoToiDaKhach)
            {
                error = "Tốc độ tối đa tàu hàng không được lớn hơn tốc độ tối đa tàu khách.";
                return false;
            }

            if (kg.DoDocPermil < 0 || kg.DoDocPermil > 50.0m)
            {
                error = "Độ dốc tối đa phải từ 0.0‰ đến 50.0‰.";
                return false;
            }

            if (_repo.KiemTraTonTai(kg.MaGaDau, kg.MaGaCuoi))
            {
                error = "Phân đoạn khu gian giữa 2 ga này đã tồn tại trên mạng lưới đường sắt.";
                return false;
            }

            // Ràng buộc nghiệp vụ đường sắt: khu gian chỉ nối 2 ga LIỀN KỀ theo lý trình
            if (!_repo.KiemTraGaLienKe(kg.MaGaDau, kg.MaGaCuoi))
            {
                error = "Hai ga được chọn KHÔNG liền kề nhau trên tuyến (có ga khác nằm giữa). Khu gian chỉ được tạo giữa 2 ga liền kề theo lý trình.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(kg.TrangThai))
            {
                kg.TrangThai = "RONG";
            }

            bool ok = _repo.Them(kg) > 0;
            if (!ok)
            {
                error = "Lỗi cơ sở dữ liệu khi thêm mới phân đoạn khu gian.";
            }
            return ok;
        }

        public bool CapNhat(KhuGian kg, out string error)
        {
            error = "";
            if (kg.MaKhuGian <= 0)
            {
                error = "Mã khu gian không hợp lệ.";
                return false;
            }

            if (kg.CuLyKm <= 0 || kg.CuLyKm > 999.99m)
            {
                error = "Cự ly phân đoạn khu gian phải lớn hơn 0 và không vượt quá 999.99 km.";
                return false;
            }

            if (kg.TocDoToiDaKhach < 10 || kg.TocDoToiDaKhach > 200)
            {
                error = "Tốc độ tối đa tàu khách không hợp lệ (cho phép từ 10 đến 200 km/h).";
                return false;
            }

            if (kg.TocDoToiDaHang < 10 || kg.TocDoToiDaHang > 150)
            {
                error = "Tốc độ tối đa tàu hàng không hợp lệ (cho phép từ 10 đến 150 km/h).";
                return false;
            }

            if (kg.TocDoToiDaHang > kg.TocDoToiDaKhach)
            {
                error = "Tốc độ tối đa tàu hàng không được lớn hơn tốc độ tối đa tàu khách.";
                return false;
            }

            if (kg.DoDocPermil < 0 || kg.DoDocPermil > 50.0m)
            {
                error = "Độ dốc tối đa phải từ 0.0‰ đến 50.0‰.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(kg.TrangThai))
            {
                kg.TrangThai = "RONG";
            }

            bool ok = _repo.CapNhat(kg) > 0;
            if (!ok)
            {
                error = "Lỗi cơ sở dữ liệu khi cập nhật thông tin phân đoạn khu gian.";
            }
            return ok;
        }

        public bool Xoa(int maKhuGian, out string error, bool xoaDuongNgangKemTheo = true)
        {
            error = "";
            if (maKhuGian <= 0)
            {
                error = "Mã khu gian không hợp lệ.";
                return false;
            }

            try
            {
                bool ok = _repo.Xoa(maKhuGian, xoaDuongNgangKemTheo) > 0;
                if (!ok)
                {
                    error = "Không tìm thấy phân đoạn khu gian cần xóa trong CSDL.";
                }
                return ok;
            }
            catch (Exception ex)
            {
                error = $"Không thể xóa phân đoạn khu gian: {ex.Message}";
                return false;
            }
        }
    }
}
