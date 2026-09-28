using System.Data;
using DAL.Connection;
using DAL.Repositories;
using ET.VanTai;

namespace BUS.Services
{
    public class VeService
    {
        // TECH_DEBT (SRS §2.5): Cần áp dụng Dependency Injection (IoC Container) để hỗ trợ viết Unit Test tự động thay vì khởi tạo trực tiếp
        private readonly VeRepository _repo = new();

        public DataTable LayDanhSach() => _repo.LayDanhSach();

        public DataTable TimKiemVe(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return _repo.LayDanhSach();
            return _repo.TimKiemVe(keyword.Trim());
        }

        public DataTable TimChuyenTauTheoChang(int maGaDi, int maGaDen, DateTime ngayDi)
        {
            if (maGaDi <= 0 || maGaDen <= 0 || maGaDi == maGaDen)
                return new DataTable();

            return _repo.TimChuyenTauTheoChang(maGaDi, maGaDen, ngayDi);
        }

        public DataTable LayDanhSachToaTheoChuyen(int maChuyenTau, int maGaDi, int maGaDen)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayDanhSachToaTheoChuyen(maChuyenTau, maGaDi, maGaDen);
        }

        public DataTable LaySoDoGhe(int maToaXeKhach, int maChuyenTau, int maGaDi, int maGaDen)
        {
            if (maToaXeKhach <= 0 || maChuyenTau <= 0) return new DataTable();
            return _repo.LaySoDoGhe(maToaXeKhach, maChuyenTau, maGaDi, maGaDen);
        }

        public decimal TinhGiaVe(string loaiToa, int maGaDi, int maGaDen, int? tangGiuong = null, string loaiTau = "TAU_CHO")
        {
            if (maGaDi <= 0 || maGaDen <= 0) return 0;
            return _repo.TinhGiaVe(loaiToa, maGaDi, maGaDen, tangGiuong, loaiTau);
        }

        // Tỷ lệ giảm giá mặc định theo Luật Đường sắt 2017 (Điều 27)
        private static readonly Dictionary<string, decimal> FallbackGiamGia = new(StringComparer.OrdinalIgnoreCase)
        {
            ["TRE_EM"] = 0.25m,    // Giảm 25% cho trẻ em từ 6 đến 10 tuổi
            ["NGUOI_GIA"] = 0.15m,  // Giảm 15% cho người cao tuổi từ 60 tuổi trở lên
            ["SINH_VIEN"] = 0.10m,  // Giảm 10% cho học sinh, sinh viên
            ["THUONG"] = 0.00m
        };

        private static Dictionary<string, decimal>? _cachedTyLeGiamGia;
        private static DateTime _lastCacheTime = DateTime.MinValue;

        public static decimal LayTyLeGiamGia(string loaiKhach)
        {
            if (string.IsNullOrWhiteSpace(loaiKhach)) return 0m;

            if (_cachedTyLeGiamGia == null || (DateTime.Now - _lastCacheTime).TotalMinutes > 10)
            {
                try
                {
                    string sql = "SELECT MaCode, GhiChu FROM danhmuc.DanhMuc WHERE NhomDanhMuc = 'TI_LE_GIAM_GIA' AND DangKhaiThac = 1";
                    DataTable dt = DatabaseHelper.ExecuteQuery(sql);
                    var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataRow row in dt.Rows)
                    {
                        string code = row["MaCode"].ToString() ?? "";
                        if (decimal.TryParse(row["GhiChu"]?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal tyLe))
                        {
                            dict[code] = tyLe;
                        }
                    }
                    if (dict.Count > 0)
                    {
                        _cachedTyLeGiamGia = dict;
                        _lastCacheTime = DateTime.Now;
                    }
                }
                catch
                {
                    // Dự phòng nếu lỗi kết nối danh mục
                }
            }

            if (_cachedTyLeGiamGia != null && _cachedTyLeGiamGia.TryGetValue(loaiKhach, out decimal rate))
            {
                return rate;
            }

            return FallbackGiamGia.TryGetValue(loaiKhach, out decimal fallbackRate) ? fallbackRate : 0m;
        }

        // Tinh muc giam gia tu CSDL danhmuc.DanhMuc hoac can cu Luat Duong sat 2017
        public decimal TinhMucGiamGia(decimal giaGoc, string loaiKhach)
        {
            if (giaGoc <= 0 || string.IsNullOrWhiteSpace(loaiKhach)) return 0;
            decimal tyLe = LayTyLeGiamGia(loaiKhach);
            return Math.Round(giaGoc * tyLe, 0);
        }

        public DataTable? TimKhachHangTheoCCCD(string cccd)
        {
            if (string.IsNullOrWhiteSpace(cccd)) return null;
            return _repo.TimKhachHangTheoCCCD(cccd.Trim());
        }

        public bool DatVeTheoDon(
            KhachHang khachHang, 
            DonDatVe donVe, 
            List<Ve> danhSachVe, 
            string soHieuTau,
            out string maPNR, 
            out string thongBaoLoi)
        {
            maPNR = string.Empty;
            thongBaoLoi = string.Empty;

            if (danhSachVe == null || danhSachVe.Count == 0)
            {
                thongBaoLoi = "Giỏ vé đang trống, vui lòng chọn ghế trước khi thanh toán!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(khachHang.HoTen))
            {
                thongBaoLoi = "Vui lòng nhập họ tên người đại diện mua vé!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(khachHang.SoDienThoai))
            {
                thongBaoLoi = "Vui lòng nhập số điện thoại người đại diện mua vé!";
                return false;
            }

            foreach (var ve in danhSachVe)
            {
                if (string.IsNullOrWhiteSpace(ve.TenHanhKhach))
                {
                    thongBaoLoi = $"Vui lòng nhập họ tên hành khách cho ghế #{ve.MaChoNgoi}!";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(ve.CCCDHanhKhach))
                {
                    thongBaoLoi = $"Vui lòng nhập CCCD/Mã định danh cho hành khách {ve.TenHanhKhach}!";
                    return false;
                }
            }

            return _repo.LuuDonDatVeTransaction(khachHang, donVe, danhSachVe, soHieuTau, out maPNR, out thongBaoLoi);
        }

        public bool HoanVe(int maVe, decimal tyLeLePhi, string lyDo, string hinhThucHoan, int maNhanVien, out string thongBaoLoi)
        {
            if (maVe <= 0)
            {
                thongBaoLoi = "Mã vé không hợp lệ!";
                return false;
            }
            return _repo.HoanVe(maVe, tyLeLePhi, lyDo, hinhThucHoan, maNhanVien, out thongBaoLoi);
        }

        public DataTable LayDanhSachVeTheoCCCD(string cccd)
        {
            if (string.IsNullOrWhiteSpace(cccd)) return new DataTable();
            return _repo.LayDanhSachVeTheoCCCD(cccd.Trim());
        }

        public bool DoiGheCungChuyen(int maVeCu, int maChoNgoiMoi, int maNhanVien, out string maVeCodeMoi, out string thongBaoLoi)
        {
            if (maVeCu <= 0 || maChoNgoiMoi <= 0)
            {
                maVeCodeMoi = string.Empty;
                thongBaoLoi = "Mã vé hoặc mã chỗ ngồi mới không hợp lệ!";
                return false;
            }
            return _repo.DoiGheCungChuyen(maVeCu, maChoNgoiMoi, maNhanVien, out maVeCodeMoi, out thongBaoLoi);
        }

        #region NGHIỆP VỤ SOÁT VÉ & KIỂM SOÁT LÊN TÀU

        public bool SoatVe(int maVe, int? maGaSoat, out string thongBaoLoi)
        {
            if (maVe <= 0)
            {
                thongBaoLoi = "Mã vé không hợp lệ!";
                return false;
            }
            return _repo.CapNhatTrangThaiSoatVe(maVe, "DA_LEN_TAU", maGaSoat, out thongBaoLoi);
        }

        public bool HuySoatVe(int maVe, out string thongBaoLoi)
        {
            if (maVe <= 0)
            {
                thongBaoLoi = "Mã vé không hợp lệ!";
                return false;
            }
            return _repo.CapNhatTrangThaiSoatVe(maVe, "DA_DAT", null, out thongBaoLoi);
        }

        public DataTable LayManifestChuyenTau(int maChuyenTau, string? trangThaiLoc = null)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayManifestChuyenTau(maChuyenTau, trangThaiLoc);
        }

        public DataTable TimVeSoat(int? maChuyenTau, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new DataTable();
            return _repo.TimVeSoat(maChuyenTau, keyword.Trim());
        }

        public DataTable LayThongKeSoatVe(int maChuyenTau, int maGaHienTai)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayThongKeSoatVe(maChuyenTau, maGaHienTai);
        }

        public DataTable LayDanhSachChuyenTauHomNay(DateTime? ngay = null)
        {
            return _repo.LayDanhSachChuyenTauHomNay(ngay ?? DateTime.Today);
        }

        #endregion
    }
}
