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

            if (kg.CuLyKm <= 0)
            {
                error = "Cự ly phân đoạn khu gian phải lớn hơn 0 km.";
                return false;
            }

            if (kg.TocDoToiDaKhach <= 0 || kg.TocDoToiDaHang <= 0)
            {
                error = "Tốc độ tối đa cho phép phải là số nguyên dương lớn hơn 0 km/h.";
                return false;
            }

            if (kg.DoDocPermil < 0)
            {
                error = "Độ dốc tối đa không thể là số âm.";
                return false;
            }

            if (_repo.KiemTraTonTai(kg.MaGaDau, kg.MaGaCuoi))
            {
                error = "Phân đoạn khu gian giữa 2 ga này đã tồn tại trên mạng lưới đường sắt.";
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

            if (kg.CuLyKm <= 0)
            {
                error = "Cự ly phân đoạn khu gian phải lớn hơn 0 km.";
                return false;
            }

            if (kg.TocDoToiDaKhach <= 0 || kg.TocDoToiDaHang <= 0)
            {
                error = "Tốc độ tối đa cho phép phải lớn hơn 0 km/h.";
                return false;
            }

            if (kg.DoDocPermil < 0)
            {
                error = "Độ dốc tối đa không thể là số âm.";
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

        public bool Xoa(int maKhuGian, out string error)
        {
            error = "";
            if (maKhuGian <= 0)
            {
                error = "Mã khu gian không hợp lệ.";
                return false;
            }

            bool ok = _repo.Xoa(maKhuGian) > 0;
            if (!ok)
            {
                error = "Không thể xóa phân đoạn khu gian (đang có lịch trình chạy tàu hoặc đường ngang giao cắt liên kết).";
            }
            return ok;
        }
    }
}
