using System;
using System.Data;
using DAL.Repositories;
using DTO.Common;
using ET.HaTang;

namespace BUS.Services
{
    public class DuongNgangService
    {
        private readonly DuongNgangRepository _repo = new();

        public DataTable LayTheoKhuGian(int maKhuGian)
        {
            if (maKhuGian <= 0) return new DataTable();
            return _repo.LayTheoKhuGian(maKhuGian);
        }

        public bool Them(DuongNgang dn, decimal minKm, decimal maxKm, out string error)
        {
            error = string.Empty;

            if (dn.MaKhuGian <= 0)
            {
                error = "Khu gian định danh không hợp lệ.";
                return false;
            }

            // 1. Kiểm tra Tên đường bộ giao cắt
            if (string.IsNullOrWhiteSpace(dn.TenDuongBoGiaoCat))
            {
                error = "Vui lòng nhập tên đường bộ giao cắt (ví dụ: QL1A, Đường dân sinh Văn Điển, Tỉnh lộ 427...).";
                return false;
            }

            dn.TenDuongBoGiaoCat = dn.TenDuongBoGiaoCat.Trim();
            if (dn.TenDuongBoGiaoCat.Length > 150)
            {
                error = "Tên đường bộ giao cắt tối đa 150 ký tự.";
                return false;
            }

            // 2. Kiểm tra Lý trình Km nằm trong phạm vi khu gian
            if (dn.LyTrinhKm < 0)
            {
                error = "Lý trình đường ngang không được là số âm.";
                return false;
            }

            decimal startKm = Math.Min(minKm, maxKm);
            decimal endKm = Math.Max(minKm, maxKm);

            if (startKm > 0 || endKm > 0)
            {
                if (dn.LyTrinhKm < startKm || dn.LyTrinhKm > endKm)
                {
                    error = $"Lý trình Km {FormatHelper.FormatKm(dn.LyTrinhKm)} nằm ngoài phạm vi lý trình của phân đoạn khu gian này (từ Km {FormatHelper.FormatKm(startKm)} đến Km {FormatHelper.FormatKm(endKm)}).";
                    return false;
                }
            }

            // 3. Kiểm tra trùng lý trình trong cùng khu gian
            if (_repo.KiemTraLyTrinhTrung(dn.MaKhuGian, dn.LyTrinhKm))
            {
                error = $"Đã có một điểm giao cắt khác được khai báo tại vị trí Km {FormatHelper.FormatKm(dn.LyTrinhKm)} trong khu gian này.";
                return false;
            }

            // 4. Chuẩn hóa loại đường ngang
            if (dn.LoaiDuongNgang != "CO_NGUOI_GAC" && dn.LoaiDuongNgang != "TU_DONG" && dn.LoaiDuongNgang != "BIEN_BAO")
            {
                dn.LoaiDuongNgang = "BIEN_BAO";
            }

            bool ok = _repo.Them(dn) > 0;
            if (!ok)
            {
                error = "Lỗi cơ sở dữ liệu khi thêm mới đường ngang.";
            }
            return ok;
        }

        public bool CapNhat(DuongNgang dn, decimal minKm, decimal maxKm, out string error)
        {
            error = string.Empty;

            if (dn.MaDuongNgang <= 0 || dn.MaKhuGian <= 0)
            {
                error = "Định danh đường ngang hoặc khu gian không hợp lệ.";
                return false;
            }

            // 1. Kiểm tra Tên đường bộ giao cắt
            if (string.IsNullOrWhiteSpace(dn.TenDuongBoGiaoCat))
            {
                error = "Tên đường bộ giao cắt không được để trống.";
                return false;
            }

            dn.TenDuongBoGiaoCat = dn.TenDuongBoGiaoCat.Trim();
            if (dn.TenDuongBoGiaoCat.Length > 150)
            {
                error = "Tên đường bộ giao cắt tối đa 150 ký tự.";
                return false;
            }

            // 2. Khóa biên lý trình
            if (dn.LyTrinhKm < 0)
            {
                error = "Lý trình đường ngang không được là số âm.";
                return false;
            }

            decimal startKm = Math.Min(minKm, maxKm);
            decimal endKm = Math.Max(minKm, maxKm);

            if (startKm > 0 || endKm > 0)
            {
                if (dn.LyTrinhKm < startKm || dn.LyTrinhKm > endKm)
                {
                    error = $"Lý trình Km {FormatHelper.FormatKm(dn.LyTrinhKm)} nằm ngoài phạm vi khu gian (Km {FormatHelper.FormatKm(startKm)} — Km {FormatHelper.FormatKm(endKm)}).";
                    return false;
                }
            }

            // 3. Trùng lý trình loại trừ chính nó
            if (_repo.KiemTraLyTrinhTrung(dn.MaKhuGian, dn.LyTrinhKm, dn.MaDuongNgang))
            {
                error = $"Lý trình Km {FormatHelper.FormatKm(dn.LyTrinhKm)} đã trùng với một điểm giao cắt khác trong cùng khu gian.";
                return false;
            }

            if (dn.LoaiDuongNgang != "CO_NGUOI_GAC" && dn.LoaiDuongNgang != "TU_DONG" && dn.LoaiDuongNgang != "BIEN_BAO")
            {
                dn.LoaiDuongNgang = "BIEN_BAO";
            }

            bool ok = _repo.CapNhat(dn) > 0;
            if (!ok)
            {
                error = "Lỗi cơ sở dữ liệu khi cập nhật thông tin đường ngang.";
            }
            return ok;
        }

        public bool Xoa(int maDuongNgang, out string error)
        {
            error = string.Empty;
            if (maDuongNgang <= 0)
            {
                error = "Mã đường ngang không hợp lệ.";
                return false;
            }

            try
            {
                bool ok = _repo.Xoa(maDuongNgang) > 0;
                if (!ok)
                {
                    error = "Không tìm thấy bản ghi đường ngang cần xóa trong CSDL.";
                }
                return ok;
            }
            catch (Exception ex)
            {
                error = $"Không thể xóa đường ngang: {ex.Message}";
                return false;
            }
        }
    }
}
