using System;
using System.Data;
using System.Text.RegularExpressions;
using DAL.Repositories;
using ET.HaTang;

namespace BUS.Services
{
    public class GaService
    {
        private readonly GaRepository _repo = new();

        public DataTable LayDanhSach() => _repo.LayDanhSach();

        public DataTable TimTheoTen(string tenGa) => _repo.TimTheoTen(tenGa);

        public bool Them(Ga ga) => Them(ga, out _);

        public bool Them(Ga ga, out string error)
        {
            error = string.Empty;

            // 1. Kiểm tra Mã Ga Code (VARCHAR(10) NOT NULL UNIQUE)
            if (string.IsNullOrWhiteSpace(ga.MaGaCode))
            {
                error = "Mã Ga (Code) không được để trống (ví dụ: HN, GB, PL).";
                return false;
            }

            ga.MaGaCode = ga.MaGaCode.Trim().ToUpperInvariant();
            if (ga.MaGaCode.Length > 10)
            {
                error = "Mã Ga (Code) tối đa 10 ký tự theo quy định cơ sở dữ liệu.";
                return false;
            }

            if (!Regex.IsMatch(ga.MaGaCode, @"^[A-Z0-9_\-]+$"))
            {
                error = "Mã Ga chỉ được chứa chữ cái in hoa không dấu, chữ số hoặc gạch ngang (ví dụ: HN, SGO, DN_01).";
                return false;
            }

            if (_repo.KiemTraMaGaCodeTonTai(ga.MaGaCode))
            {
                error = $"Mã Ga '{ga.MaGaCode}' đã tồn tại trên mạng lưới đường sắt. Vui lòng chọn mã khác.";
                return false;
            }

            // 2. Kiểm tra Tên Ga (NVARCHAR(100) NOT NULL)
            if (string.IsNullOrWhiteSpace(ga.TenGa))
            {
                error = "Tên Ga không được để trống.";
                return false;
            }

            ga.TenGa = ga.TenGa.Trim();
            if (ga.TenGa.Length > 100)
            {
                error = "Tên Ga không được vượt quá 100 ký tự.";
                return false;
            }

            // 3. Kiểm tra Lý Trình Km (DECIMAL(7,2) >= 0 & UNIQUE theo tuyến)
            if (ga.LyTrinhKm < 0)
            {
                error = "Lý trình không được là số âm (phải >= 0.00 km).";
                return false;
            }

            if (ga.LyTrinhKm > 5000)
            {
                error = "Lý trình vượt quá giới hạn chiều dài mạng lưới đường sắt quốc gia (tối đa 5,000 km).";
                return false;
            }

            if (_repo.KiemTraLyTrinhTonTai(ga.MaTuyen > 0 ? ga.MaTuyen : 1, ga.LyTrinhKm))
            {
                error = $"Lý trình Km {ga.LyTrinhKm:0.00} đã trùng với một ga khác trên tuyến (vi phạm ràng buộc duy nhất UQ_Ga_Tuyen_LyTrinh).";
                return false;
            }

            // 4. Tỉnh thành
            if (string.IsNullOrWhiteSpace(ga.TinhThanh))
            {
                error = "Vui lòng chọn Tỉnh/Thành phố trực thuộc cho Ga.";
                return false;
            }

            // 5. Chuẩn hóa hạng ga
            if (ga.HangGa != "HANG_1" && ga.HangGa != "HANG_2" && ga.HangGa != "HANG_3")
            {
                ga.HangGa = "HANG_3";
            }

            bool ok = _repo.Them(ga) > 0;
            if (!ok)
            {
                error = "Lỗi cơ sở dữ liệu khi thêm mới ga vào hệ thống.";
            }
            return ok;
        }

        public bool CapNhat(Ga ga) => CapNhat(ga, out _);

        public bool CapNhat(Ga ga, out string error)
        {
            error = string.Empty;

            if (ga.MaGa <= 0)
            {
                error = "Mã Ga định danh không hợp lệ.";
                return false;
            }

            // 1. Tên Ga
            if (string.IsNullOrWhiteSpace(ga.TenGa))
            {
                error = "Tên Ga không được để trống.";
                return false;
            }

            ga.TenGa = ga.TenGa.Trim();
            if (ga.TenGa.Length > 100)
            {
                error = "Tên Ga không được vượt quá 100 ký tự.";
                return false;
            }

            // 2. Lý Trình Km
            if (ga.LyTrinhKm < 0)
            {
                error = "Lý trình không được là số âm (phải >= 0.00 km).";
                return false;
            }

            if (ga.LyTrinhKm > 5000)
            {
                error = "Lý trình vượt quá giới hạn mạng lưới đường sắt (tối đa 5,000 km).";
                return false;
            }

            if (_repo.KiemTraLyTrinhTonTai(ga.MaTuyen > 0 ? ga.MaTuyen : 1, ga.LyTrinhKm, ga.MaGa))
            {
                error = $"Lý trình Km {ga.LyTrinhKm:0.00} đã trùng với một ga khác trên tuyến (vi phạm ràng buộc UQ_Ga_Tuyen_LyTrinh).";
                return false;
            }

            // 3. Tỉnh thành
            if (string.IsNullOrWhiteSpace(ga.TinhThanh))
            {
                error = "Vui lòng chọn Tỉnh/Thành phố cho Ga.";
                return false;
            }

            // 4. Chuẩn hóa hạng ga
            if (ga.HangGa != "HANG_1" && ga.HangGa != "HANG_2" && ga.HangGa != "HANG_3")
            {
                ga.HangGa = "HANG_3";
            }

            bool ok = _repo.CapNhat(ga) > 0;
            if (!ok)
            {
                error = "Lỗi cơ sở dữ liệu khi cập nhật thông tin ga.";
            }
            return ok;
        }

        public bool Xoa(int maGa) => Xoa(maGa, out _);

        public bool Xoa(int maGa, out string error)
        {
            error = string.Empty;
            if (maGa <= 0)
            {
                error = "Mã Ga không hợp lệ.";
                return false;
            }

            try
            {
                bool ok = _repo.Xoa(maGa) > 0;
                if (!ok)
                {
                    error = "Không tìm thấy bản ghi Ga cần xóa trong CSDL.";
                }
                return ok;
            }
            catch (Exception ex)
            {
                error = $"Không thể xóa Ga này do ràng buộc toàn vẹn dữ liệu (đang có hạ tầng đường ray, phân đoạn khu gian hoặc lịch chạy tàu liên quan). Chi tiết: {ex.Message}";
                return false;
            }
        }

        public DataTable LayDuongRayTheoGa(int maGa) => _repo.LayDuongRayTheoGa(maGa);

        public DataTable LayKhuGianTheoGa(int maGa) => _repo.LayKhuGianTheoGa(maGa);
    }
}
