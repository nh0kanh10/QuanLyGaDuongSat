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

            if (ray.ChieuDaiHuuDungM < 100 || ray.ChieuDaiHuuDungM > 3000)
            {
                error = "Chiều dài hữu dụng của đường ray phải từ 100 đến 3,000 mét.";
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

            if (ray.LoaiDuong == "CHINH_TUYEN" && _repo.KiemTraChinhTuyenTonTai(ray.MaGa))
            {
                error = "Mỗi ga đường sắt chỉ được phép có duy nhất 01 đường chính tuyến. Vui lòng chọn phân loại 'Đường tránh' hoặc 'Bốc dỡ'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ray.TrangThai))
            {
                ray.TrangThai = "TRONG";
            }
            else if (ray.TrangThai == "CO_TAU")
            {
                error = "Đường ray mới tạo không được thiết lập trạng thái 'Đang có tàu'. Vui lòng chọn 'Trống' hoặc 'Bảo trì'.";
                return false;
            }

            bool ok = _repo.Them(ray) > 0;
            if (!ok)
            {
                error = "Không thể lưu thông tin đường ray. Vui lòng thử lại.";
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

            if (ray.ChieuDaiHuuDungM < 100 || ray.ChieuDaiHuuDungM > 3000)
            {
                error = "Chiều dài hữu dụng của đường ray phải từ 100 đến 3,000 mét.";
                return false;
            }

            if (_repo.KiemTraSoHieuTonTai(ray.MaGa, ray.SoHieuDuong, ray.MaDuongRay))
            {
                error = $"Số hiệu đường {ray.SoHieuDuong} đã bị trùng với đường ray khác trong cùng ga.";
                return false;
            }

            if (ray.LoaiDuong == "CHINH_TUYEN" && _repo.KiemTraChinhTuyenTonTai(ray.MaGa, ray.MaDuongRay))
            {
                error = "Mỗi ga đường sắt chỉ được phép có duy nhất 01 đường chính tuyến . Vui lòng chọn phân loại 'Đường tránh' hoặc 'Bốc dỡ'.";
                return false;
            }

            bool ok = _repo.CapNhat(ray) > 0;
            if (!ok)
            {
                error = "Không thể cập nhật thông tin đường ray. Vui lòng thử lại.";
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

            if (_repo.KiemTraDangCoTau(maDuongRay))
            {
                error = "Không thể xóa đường ray này vì đang có đoàn tàu hoạt động / chiếm dụng trên đường. Vui lòng giải phóng tàu trước khi xóa hạ tầng.";
                return false;
            }

            if (_repo.KiemTraCoLichDung(maDuongRay))
            {
                error = "Không thể xóa đường ray này vì đã được xếp lịch dừng đón trả tàu trong kế hoạch chạy tàu.";
                return false;
            }

            if (_repo.KiemTraLaChinhTuyenDuyNhat(maDuongRay))
            {
                error = "Không thể xóa đường chính tuyến duy nhất của ga. Mọi ga bắt buộc phải có ít nhất 01 đường ray chính tuyến để thông tàu.";
                return false;
            }

            bool ok = _repo.Xoa(maDuongRay) > 0;
            if (!ok)
            {
                error = "Không thể xóa đường ray này vì đang có tàu hoạt động hoặc đã được xếp lịch.";
            }
            return ok;
        }
    }
}
