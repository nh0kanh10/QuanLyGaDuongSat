using System;
using System.Data;
using System.Text.RegularExpressions;
using DAL.Repositories;
using DTO.Common;
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
            if (ga.MaGaCode.Length < 2)
            {
                error = "Mã Ga phải có ít nhất 2 ký tự (ví dụ: HN, SGO).";
                return false;
            }

            if (ga.MaGaCode.Length > 10)
            {
                error = "Mã Ga tối đa 10 ký tự.";
                return false;
            }

            if (!Regex.IsMatch(ga.MaGaCode, @"^[A-Z0-9_\-]+$"))
            {
                error = "Mã Ga chỉ gồm chữ in hoa, số hoặc gạch nối (ví dụ: HN, SGO, DN-01).";
                return false;
            }

            if (_repo.KiemTraMaGaCodeTonTai(ga.MaGaCode))
            {
                error = $"Mã Ga '{ga.MaGaCode}' đã được sử dụng. Vui lòng chọn mã khác.";
                return false;
            }

            // 2. Kiểm tra & chuẩn hóa Tên Ga (NVARCHAR(100) NOT NULL)
            ga.TenGa = FormatHelper.ChuanHoaTenGa(ga.TenGa);
            if (string.IsNullOrWhiteSpace(ga.TenGa))
            {
                error = "Tên Ga không được để trống.";
                return false;
            }

            if (ga.TenGa.Length < 2)
            {
                error = "Tên Ga phải có ít nhất 2 ký tự (ví dụ: Hà Nội, Huế).";
                return false;
            }

            if (ga.TenGa.Length > 100)
            {
                error = "Tên Ga không được vượt quá 100 ký tự.";
                return false;
            }

            if (Regex.IsMatch(ga.TenGa, @"[@#$%^&*+=\\<>\[\]{}|~`!?;:]"))
            {
                error = "Tên Ga không được chứa ký tự đặc biệt.";
                return false;
            }

            if (!Regex.IsMatch(ga.TenGa, @"\p{L}"))
            {
                error = "Tên Ga phải có chữ cái, không thể chỉ nhập toàn số.";
                return false;
            }

            if (!char.IsLetter(ga.TenGa[0]))
            {
                error = "Tên Ga phải bắt đầu bằng chữ cái.";
                return false;
            }

            if (_repo.KiemTraTenGaTonTai(ga.MaTuyen > 0 ? ga.MaTuyen : 1, ga.TenGa))
            {
                error = $"Tên Ga '{ga.TenGa}' đã có trên tuyến này.";
                return false;
            }

            // 3. Kiểm tra Lý Trình Km (DECIMAL(7,2) >= 0 & UNIQUE theo tuyến)
            if (ga.LyTrinhKm < 0)
            {
                error = "Lý trình không được là số âm.";
                return false;
            }

            if (ga.LyTrinhKm > 5000)
            {
                error = "Lý trình không được vượt quá 5,000 km.";
                return false;
            }

            string? gaTrungKm = _repo.LayTenGaTheoLyTrinh(ga.MaTuyen > 0 ? ga.MaTuyen : 1, ga.LyTrinhKm);
            if (!string.IsNullOrEmpty(gaTrungKm))
            {
                error = $"Lý trình Km {ga.LyTrinhKm:0.00} đã trùng với Ga {gaTrungKm}.";
                return false;
            }

            // 4. Tỉnh thành
            if (string.IsNullOrWhiteSpace(ga.TinhThanh))
            {
                error = "Vui lòng chọn Tỉnh / Thành phố.";
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
                error = "Không thể lưu dữ liệu ga. Vui lòng thử lại.";
            }
            return ok;
        }

        public bool CapNhat(Ga ga) => CapNhat(ga, out _);

        public bool CapNhat(Ga ga, out string error)
        {
            error = string.Empty;

            if (ga.MaGa <= 0)
            {
                error = "Mã Ga không hợp lệ.";
                return false;
            }

            // 1. Kiểm tra & chuẩn hóa Tên Ga
            ga.TenGa = FormatHelper.ChuanHoaTenGa(ga.TenGa);
            if (string.IsNullOrWhiteSpace(ga.TenGa))
            {
                error = "Tên Ga không được để trống.";
                return false;
            }

            if (ga.TenGa.Length < 2)
            {
                error = "Tên Ga phải có ít nhất 2 ký tự.";
                return false;
            }

            if (ga.TenGa.Length > 100)
            {
                error = "Tên Ga không được vượt quá 100 ký tự.";
                return false;
            }

            if (Regex.IsMatch(ga.TenGa, @"[@#$%^&*+=\\<>\[\]{}|~`!?;:]"))
            {
                error = "Tên Ga không được chứa ký tự đặc biệt.";
                return false;
            }

            if (!Regex.IsMatch(ga.TenGa, @"\p{L}"))
            {
                error = "Tên Ga phải có chữ cái, không thể chỉ nhập toàn số.";
                return false;
            }

            if (!char.IsLetter(ga.TenGa[0]))
            {
                error = "Tên Ga phải bắt đầu bằng chữ cái.";
                return false;
            }

            if (_repo.KiemTraTenGaTonTai(ga.MaTuyen > 0 ? ga.MaTuyen : 1, ga.TenGa, ga.MaGa))
            {
                error = $"Tên Ga '{ga.TenGa}' đã có trên tuyến này.";
                return false;
            }

            // 2. Lý Trình Km
            if (ga.LyTrinhKm < 0)
            {
                error = "Lý trình không được là số âm.";
                return false;
            }

            if (ga.LyTrinhKm > 5000)
            {
                error = "Lý trình không được vượt quá 5,000 km.";
                return false;
            }

            string? gaTrungKm = _repo.LayTenGaTheoLyTrinh(ga.MaTuyen > 0 ? ga.MaTuyen : 1, ga.LyTrinhKm, ga.MaGa);
            if (!string.IsNullOrEmpty(gaTrungKm))
            {
                error = $"Lý trình Km {ga.LyTrinhKm:0.00} đã trùng với Ga {gaTrungKm}.";
                return false;
            }

            // 3. Tỉnh thành
            if (string.IsNullOrWhiteSpace(ga.TinhThanh))
            {
                error = "Vui lòng chọn Tỉnh / Thành phố.";
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
                error = "Không thể cập nhật thông tin ga. Vui lòng thử lại.";
            }
            return ok;
        }

        public bool Xoa(int maGa) => Xoa(maGa, out _, out _);

        public bool Xoa(int maGa, out string error) => Xoa(maGa, out _, out error);

        /// <summary>
        /// Xóa ga thông minh: Xóa hẳn nếu ga mới chưa có liên kết, hoặc chuyển Tạm ngừng nếu đã có hạ tầng/vận hành.
        /// </summary>
        public bool Xoa(int maGa, out bool daXoaVinhVien, out string message)
        {
            daXoaVinhVien = false;
            message = string.Empty;
            if (maGa <= 0)
            {
                message = "Mã Ga không hợp lệ.";
                return false;
            }

            if (_repo.KiemTraGaDangCoTau(maGa))
            {
                message = "Không thể xóa hoặc tạm ngừng ga này vì đang có đoàn tàu hoạt động trên đường ray trong ga. Vui lòng giải phóng tàu trước.";
                return false;
            }

            return _repo.XoaThongMinh(maGa, out daXoaVinhVien, out message);
        }

        public bool KiemTraGaDangCoTau(int maGa) => _repo.KiemTraGaDangCoTau(maGa);

        public DataTable LayDuongRayTheoGa(int maGa) => _repo.LayDuongRayTheoGa(maGa);

        public DataTable LayKhuGianTheoGa(int maGa) => _repo.LayKhuGianTheoGa(maGa);

        public bool KiemTraMaGaCodeTonTai(string maGaCode, int excludeMaGa = 0) 
            => _repo.KiemTraMaGaCodeTonTai(maGaCode, excludeMaGa);

        public string? LayTenGaTheoLyTrinh(int maTuyen, decimal lyTrinhKm, int excludeMaGa = 0) 
            => _repo.LayTenGaTheoLyTrinh(maTuyen, lyTrinhKm, excludeMaGa);
    }
}
