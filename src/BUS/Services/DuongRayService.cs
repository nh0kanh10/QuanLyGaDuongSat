using System;
using System.Data;
using DAL.Repositories;
using ET.HaTang;

namespace BUS.Services
{
    public class DuongRayService
    {
        private readonly DuongRayRepository _repo = new();

        public DataTable LayDanhSachTheoGa(int maGa) => _repo.LayDanhSachTheoGa(maGa);

        public DataTable LayTatCa() => _repo.LayTatCa();

        public bool Them(DuongRayGa ray, out string error)
        {
            error = "";
            if (ray.MaGa <= 0)
            {
                error = "Vui lòng chỉ định Ga trực thuộc.";
                return false;
            }

            if (ray.SoHieuDuong <= 0)
            {
                error = "Số hiệu đường ray phải là số nguyên dương lớn hơn 0 (ví dụ: 1, 2, 3).";
                return false;
            }

            if (ray.ChieuDaiHuuDungM < 100)
            {
                error = "Chiều dài hữu dụng của đường ray phải tối thiểu 100 mét để đảm bảo an toàn dồn dịch đoàn tàu.";
                return false;
            }

            if (_repo.KiemTraSoHieuTonTai(ray.MaGa, ray.SoHieuDuong))
            {
                error = $"Đường ray số {ray.SoHieuDuong} đã tồn tại trong ga này. Vui lòng chọn số hiệu khác.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ray.LoaiDuong))
            {
                ray.LoaiDuong = "DUONG_TRANH";
            }

            if (string.IsNullOrWhiteSpace(ray.TrangThai))
            {
                ray.TrangThai = "TRONG";
            }

            bool ok = _repo.Them(ray) > 0;
            if (!ok)
            {
                error = "Lỗi cơ sở dữ liệu khi thêm mới đường ray.";
            }
            return ok;
        }

        public bool CapNhat(DuongRayGa ray, out string error)
        {
            error = "";
            if (ray.MaDuongRay <= 0)
            {
                error = "Mã đường ray không hợp lệ.";
                return false;
            }

            if (ray.SoHieuDuong <= 0)
            {
                error = "Số hiệu đường ray phải là số nguyên dương lớn hơn 0.";
                return false;
            }

            if (ray.ChieuDaiHuuDungM < 100)
            {
                error = "Chiều dài hữu dụng của đường ray phải tối thiểu 100 mét.";
                return false;
            }

            if (_repo.KiemTraSoHieuTonTai(ray.MaGa, ray.SoHieuDuong, ray.MaDuongRay))
            {
                error = $"Số hiệu đường {ray.SoHieuDuong} đã bị trùng với đường ray khác trong cùng ga.";
                return false;
            }

            bool ok = _repo.CapNhat(ray) > 0;
            if (!ok)
            {
                error = "Lỗi cơ sở dữ liệu khi cập nhật thông tin đường ray.";
            }
            return ok;
        }

        public bool Xoa(int maDuongRay, out string error)
        {
            error = "";
            if (maDuongRay <= 0)
            {
                error = "Mã đường ray không hợp lệ.";
                return false;
            }

            bool ok = _repo.Xoa(maDuongRay) > 0;
            if (!ok)
            {
                error = "Không thể xóa đường ray (có thể do ràng buộc lịch trình chạy tàu hoặc lỗi CSDL).";
            }
            return ok;
        }
    }
}
