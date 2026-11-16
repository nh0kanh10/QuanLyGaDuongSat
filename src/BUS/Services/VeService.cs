using System.Data;
using DAL.Connection;
using DAL.Repositories;
using ET.VanTai;
using DTO.Common;

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

        public DataTable TimChuyenTauTheoChang(int maGaDi, int maGaDen, DateTime ngayDi, DateTime? gioHienTai = null)
        {
            if (maGaDi <= 0 || maGaDen <= 0 || maGaDi == maGaDen)
                return new DataTable();

            return _repo.TimChuyenTauTheoChang(maGaDi, maGaDen, ngayDi, gioHienTai);
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
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            maPNR = string.Empty;
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.BanVe, "bán / đổi vé", out thongBaoLoi)) return false;
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
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.HoanVe, "hoàn vé", out thongBaoLoi)) return false;
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

        /// <summary>Gia ve moi + chenh lech khi doi ghe (giu ty le giam cua ve cu) - dung cho hop thoai.</summary>
        public static (decimal GiamMoi, decimal ThucThuMoi, decimal ChenhLech) TinhGiaDoiGhe(
            decimal giaGocCu, decimal giamCu, decimal giaGocMoi)
            => VeRepository.TinhGiaDoiGhe(giaGocCu, giamCu, giaGocMoi);

        public bool DoiGheCungChuyen(int maVeCu, int maChoNgoiMoi, decimal giaVeGocMoi, int maNhanVien, out string maVeCodeMoi, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            maVeCodeMoi = string.Empty;
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.BanVe, "bán / đổi vé", out thongBaoLoi)) return false;
            if (maVeCu <= 0 || maChoNgoiMoi <= 0)
            {
                maVeCodeMoi = string.Empty;
                thongBaoLoi = "Mã vé hoặc mã chỗ ngồi mới không hợp lệ!";
                return false;
            }
            return _repo.DoiGheCungChuyen(maVeCu, maChoNgoiMoi, giaVeGocMoi, maNhanVien, out maVeCodeMoi, out thongBaoLoi);
        }

        #region NGHIỆP VỤ SOÁT VÉ & KIỂM SOÁT LÊN TÀU

        public bool SoatVe(int maVe, int? maGaSoat, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.SoatVe, "soát vé", out thongBaoLoi)) return false;
            if (maVe <= 0)
            {
                thongBaoLoi = "Mã vé không hợp lệ!";
                return false;
            }

            // Kiểm tra tính hợp lệ và giờ tàu chạy (Quy chuẩn ĐSVN: Cửa soát vé mở trước 120p, đóng sau 5p giờ chạy)
            DataTable dtVe = _repo.TimVeSoat(null, maVe.ToString());
            if (dtVe == null || dtVe.Rows.Count == 0)
            {
                thongBaoLoi = "Không tìm thấy vé trong hệ thống!";
                return false;
            }

            DataRow row = dtVe.Rows[0];
            string trangThaiVe = row["TrangThai"]?.ToString() ?? "";
            if (trangThaiVe != "DA_DAT")
            {
                thongBaoLoi = trangThaiVe == "DA_LEN_TAU"
                    ? "Vé này đã được soát và hành khách đã lên tàu trước đó!"
                    : $"Vé không hợp lệ để lên tàu (Trạng thái hiện tại: {trangThaiVe})!";
                return false;
            }

            // Kiểm tra ga đón
            int maGaDi = row["MaGaDi"] != DBNull.Value ? Convert.ToInt32(row["MaGaDi"]) : 0;
            string tenGaDi = row["TenGaDi"]?.ToString() ?? "";
            string soHieuMacTau = row["SoHieuMacTau"]?.ToString() ?? "";

            if (maGaSoat.HasValue && maGaSoat.Value > 0 && maGaDi > 0 && maGaDi != maGaSoat.Value)
            {
                thongBaoLoi = $"Cửa kiểm soát từ chối: Vé đón tại ga {tenGaDi}, không khớp ga kiểm soát hiện tại!";
                return false;
            }

            // Kiểm tra tàu đã xuất phát thực tế rời ga đón
            if (row.Table.Columns.Contains("GioDiThucTe") && row["GioDiThucTe"] != DBNull.Value)
            {
                DateTime gioDiTT = Convert.ToDateTime(row["GioDiThucTe"]);
                thongBaoLoi = $"Tàu {soHieuMacTau} đã khởi hành rời ga đón {tenGaDi} lúc {gioDiTT:HH:mm}. Cửa kiểm soát đã đóng!";
                return false;
            }

            // Kiểm tra giờ chạy dự kiến (bao gồm trễ lũy kế)
            DateTime? gioDiKH = null;
            if (row.Table.Columns.Contains("GioDiKH") && row["GioDiKH"] != DBNull.Value)
            {
                gioDiKH = Convert.ToDateTime(row["GioDiKH"]);
            }
            else if (row.Table.Columns.Contains("GioXuatPhatKH") && row["GioXuatPhatKH"] != DBNull.Value)
            {
                DateTime ngayXP = row.Table.Columns.Contains("NgayXuatPhat") && row["NgayXuatPhat"] != DBNull.Value
                    ? Convert.ToDateTime(row["NgayXuatPhat"])
                    : ThoiGianHeThongHelper.LayGioHienTai().Date;
                object gVal = row["GioXuatPhatKH"];
                TimeSpan ts = gVal is TimeSpan t ? t : (gVal is DateTime d ? d.TimeOfDay : TimeSpan.Zero);
                gioDiKH = ngayXP.Date.Add(ts);
            }

            if (gioDiKH.HasValue)
            {
                DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
                int soPhutTre = row.Table.Columns.Contains("SoPhutTreLuyKe") && row["SoPhutTreLuyKe"] != DBNull.Value
                    ? Convert.ToInt32(row["SoPhutTreLuyKe"])
                    : 0;
                DateTime gioDiDuKien = gioDiKH.Value.AddMinutes(soPhutTre);

                if (gioDiKH.Value.Date < now.Date)
                {
                    thongBaoLoi = $"Vé đã quá hạn sử dụng (Ngày khởi hành: {gioDiKH.Value:dd/MM/yyyy})!";
                    return false;
                }

                if (gioDiKH.Value.Date > now.Date)
                {
                    thongBaoLoi = $"Vé chưa đến ngày khởi hành (Ngày khởi hành: {gioDiKH.Value:dd/MM/yyyy})!";
                    return false;
                }

                if (now < gioDiDuKien.AddMinutes(-120))
                {
                    thongBaoLoi = $"Chưa đến giờ mở cửa soát vé! Cửa chỉ mở trước giờ tàu chạy tối đa 120 phút (từ {gioDiDuKien.AddMinutes(-120):HH:mm}).";
                    return false;
                }

                if (now > gioDiDuKien.AddMinutes(5))
                {
                    string thongTinGio = soPhutTre > 0
                        ? $"{gioDiDuKien:HH:mm} (kế hoạch {gioDiKH.Value:HH:mm} + trễ {soPhutTre}p)"
                        : $"{gioDiKH.Value:HH:mm}";
                    thongBaoLoi = $"Tàu {soHieuMacTau} đã khởi hành lúc {thongTinGio} tại ga {tenGaDi}. Cửa kiểm soát đã đóng sau 5 phút kể từ giờ tàu chạy!";
                    return false;
                }
            }

            return _repo.CapNhatTrangThaiSoatVe(maVe, "DA_LEN_TAU", maGaSoat, out thongBaoLoi);
        }

        public bool SoatVeTheoLo(int maChuyenTau, int maGaSoat, out int soVeDaSoat, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            soVeDaSoat = 0;
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.SoatVe, "soát vé", out thongBaoLoi)) return false;
            if (maChuyenTau <= 0 || maGaSoat <= 0)
            {
                soVeDaSoat = 0;
                thongBaoLoi = "Mã chuyến tàu hoặc ga kiểm soát không hợp lệ!";
                return false;
            }

            // Kiểm tra xem tàu tại ga này đã khởi hành hay chưa
            DataTable dtManifest = _repo.LayManifestChuyenTau(maChuyenTau, "DA_DAT");
            if (dtManifest != null && dtManifest.Rows.Count > 0)
            {
                DataRow firstRow = dtManifest.Rows[0];
                if (firstRow.Table.Columns.Contains("GioDiThucTe") && firstRow["GioDiThucTe"] != DBNull.Value)
                {
                    thongBaoLoi = "Tàu đã khởi hành rời ga. Cửa kiểm soát đã đóng, không thể soát vé!";
                    return false;
                }

                DateTime? gioDiKH = null;
                if (firstRow.Table.Columns.Contains("GioDiKH") && firstRow["GioDiKH"] != DBNull.Value)
                {
                    gioDiKH = Convert.ToDateTime(firstRow["GioDiKH"]);
                }

                if (gioDiKH.HasValue)
                {
                    DateTime now = ThoiGianHeThongHelper.LayGioHienTai();
                    int soPhutTre = firstRow.Table.Columns.Contains("SoPhutTreLuyKe") && firstRow["SoPhutTreLuyKe"] != DBNull.Value
                        ? Convert.ToInt32(firstRow["SoPhutTreLuyKe"])
                        : 0;
                    DateTime gioDiDuKien = gioDiKH.Value.AddMinutes(soPhutTre);

                    if (now > gioDiDuKien.AddMinutes(5))
                    {
                        thongBaoLoi = $"Tàu đã chạy lúc {gioDiDuKien:HH:mm}. Cửa kiểm soát đã đóng sau 5 phút kể từ giờ tàu chạy!";
                        return false;
                    }
                }
            }

            return _repo.SoatVeTheoLo(maChuyenTau, maGaSoat, out soVeDaSoat, out thongBaoLoi);
        }

        public bool HuySoatVe(int maVe, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.SoatVe, "soát vé", out thongBaoLoi)) return false;
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
            return _repo.LayDanhSachChuyenTauHomNay(ngay ?? ThoiGianHeThongHelper.LayGioHienTai().Date);
        }

        #endregion
    }
}
