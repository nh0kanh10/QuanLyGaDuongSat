using System;
using System.Collections.Concurrent;
using System.Data;
using System.Text.RegularExpressions;
using DAL.Repositories;
using ET.VanHanh;

namespace BUS.Services
{
    public class DinhMucVanTocService
    {
        private readonly DinhMucVanTocRepository _repo;
        private static readonly ConcurrentDictionary<string, (decimal VanToc, string TenLoai, string MoTa)> _cache = new();
        private static bool _cacheLoaded = false;
        private static readonly object _lockObj = new();

        public DinhMucVanTocService()
        {
            _repo = new DinhMucVanTocRepository();
        }

        public DinhMucVanTocService(DinhMucVanTocRepository repo)
        {
            _repo = repo;
        }

        public DataTable LayDanhSach(bool? chiDangApDung = null)
        {
            return _repo.LayDanhSach(chiDangApDung);
        }

        public DinhMucVanToc? LayTheoMa(string maLoaiTau)
        {
            if (string.IsNullOrWhiteSpace(maLoaiTau)) return null;
            return _repo.LayTheoMa(maLoaiTau.Trim());
        }

        /// <summary>
        /// Nạp lại cache bộ nhớ đệm cho toàn bộ hệ thống
        /// </summary>
        public void LamMoiCache()
        {
            lock (_lockObj)
            {
                _cache.Clear();
                try
                {
                    DataTable dt = _repo.LayDanhSach(null);
                    foreach (DataRow r in dt.Rows)
                    {
                        string ma = r["MaLoaiTau"]?.ToString()?.ToUpperInvariant() ?? "";
                        if (string.IsNullOrEmpty(ma)) continue;

                        decimal vt = r["VanTocDinhMucKmh"] != DBNull.Value ? Convert.ToDecimal(r["VanTocDinhMucKmh"]) : 50.0m;
                        string ten = r["TenLoaiTau"]?.ToString() ?? ma;
                        string moTa = r["MoTaQuyChuan"]?.ToString() ?? "";

                        _cache[ma] = (vt, ten, moTa);
                    }
                    _cacheLoaded = true;
                }
                catch
                {
                    // Fallback mặc định nếu mất kết nối DB
                    _cache["TAU_NHANH"] = (53.0m, "Tàu Nhanh Thống Nhất (SE)", "Định mức VNR Thống Nhất");
                    _cache["TAU_CHO"] = (42.0m, "Tàu Khách Khu Đoạn (NA, LP)", "Định mức dừng nhiều ga");
                    _cache["TAU_HANG"] = (32.0m, "Tàu Hàng Chuyên Tuyến", "Định mức dồn toa và tránh tàu");
                }
            }
        }

        /// <summary>
        /// Tra cứu nhanh thông số định mức của loại tàu từ Cache O(1)
        /// </summary>
        public static (decimal VanToc, string TenLoai, string MoTa) LayThongSoDinhMuc(string maLoaiTau)
        {
            if (!_cacheLoaded || _cache.IsEmpty)
            {
                new DinhMucVanTocService().LamMoiCache();
            }

            string key = (maLoaiTau ?? "").Trim().ToUpperInvariant();
            if (_cache.TryGetValue(key, out var val))
            {
                return val;
            }

            // Fallback an toàn nếu chưa cấu hình
            return key switch
            {
                "TAU_NHANH" => (53.0m, "Tàu Nhanh Thống Nhất (SE)", "Chuẩn VNR"),
                "TAU_CHO" => (42.0m, "Tàu Khách Khu Đoạn (NA, LP)", "Chuẩn VNR"),
                "TAU_HANG" => (32.0m, "Tàu Hàng Chuyên Tuyến", "Chuẩn VNR"),
                _ => (50.0m, key, "Định mức mặc định")
            };
        }

        public bool KiemTraHopLe(DinhMucVanToc dm, bool laThemMoi, out string loi)
        {
            loi = string.Empty;
            if (dm == null)
            {
                loi = "Thông tin định mức không hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dm.MaLoaiTau))
            {
                loi = "Mã loại tàu không được để trống (ví dụ: TAU_NHANH, TAU_CHO, TAU_HANG).";
                return false;
            }

            dm.MaLoaiTau = dm.MaLoaiTau.Trim().ToUpperInvariant();
            if (dm.MaLoaiTau.Length < 2 || dm.MaLoaiTau.Length > 20)
            {
                loi = "Mã loại tàu phải có độ dài từ 2 đến 20 ký tự.";
                return false;
            }

            if (!Regex.IsMatch(dm.MaLoaiTau, @"^[A-Z0-9_]+$"))
            {
                loi = "Mã loại tàu chỉ được chứa chữ cái in hoa (A-Z), chữ số (0-9) và dấu gạch dưới (_).";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dm.TenLoaiTau))
            {
                loi = "Tên loại tàu không được để trống (ví dụ: Tàu Nhanh Thống Nhất).";
                return false;
            }

            if (dm.TenLoaiTau.Trim().Length > 100)
            {
                loi = "Tên loại tàu tối đa 100 ký tự.";
                return false;
            }

            if (dm.VanTocDinhMucKmh < 10.0m || dm.VanTocDinhMucKmh > 200.0m)
            {
                loi = "Vận tốc định mức lữ hành phải nằm trong khoảng từ 10.0 đến 200.0 km/h.";
                return false;
            }

            if (dm.ThuTuHienThi < 1)
            {
                dm.ThuTuHienThi = 1;
            }

            if (laThemMoi)
            {
                if (_repo.KiemTraTonTai(dm.MaLoaiTau))
                {
                    loi = $"Mã loại tàu '{dm.MaLoaiTau}' đã tồn tại trong hệ thống. Vui lòng chọn mã khác.";
                    return false;
                }
            }
            else
            {
                if (!_repo.KiemTraTonTai(dm.MaLoaiTau))
                {
                    loi = $"Không tìm thấy loại tàu '{dm.MaLoaiTau}' để cập nhật.";
                    return false;
                }
            }

            return true;
        }

        public bool Them(DinhMucVanToc dm, out string loi, string nguoiThucHien = "admin")
        {
            if (!KiemTraHopLe(dm, true, out loi)) return false;

            dm.NguoiTao = nguoiThucHien;
            int row = _repo.Them(dm);
            if (row > 0)
            {
                LamMoiCache();
                return true;
            }

            loi = "Lỗi khi ghi dữ liệu định mức vào CSDL.";
            return false;
        }

        public bool CapNhat(DinhMucVanToc dm, out string loi, string nguoiThucHien = "admin")
        {
            if (!KiemTraHopLe(dm, false, out loi)) return false;

            dm.NguoiCapNhat = nguoiThucHien;
            int row = _repo.CapNhat(dm);
            if (row > 0)
            {
                LamMoiCache();
                return true;
            }

            loi = "Lỗi khi cập nhật dữ liệu định mức vào CSDL.";
            return false;
        }

        public bool Xoa(string maLoaiTau, out string loi)
        {
            loi = string.Empty;
            if (string.IsNullOrWhiteSpace(maLoaiTau))
            {
                loi = "Mã loại tàu không hợp lệ.";
                return false;
            }

            string ma = maLoaiTau.Trim().ToUpperInvariant();
            int soMacDung = _repo.DemSoMacTauDangSuDung(ma);
            if (soMacDung > 0)
            {
                loi = $"Không thể xóa loại tàu '{ma}' vì đang có {soMacDung} mác tàu thuộc loại này trong CSDL.\n\nVui lòng chuyển các mác tàu sang loại khác hoặc chuyển trạng thái sang 'Tạm ngừng áp dụng'.";
                return false;
            }

            int row = _repo.Xoa(ma);
            if (row > 0)
            {
                LamMoiCache();
                return true;
            }

            loi = "Không thể xóa định mức loại tàu khỏi CSDL.";
            return false;
        }
    }
}
