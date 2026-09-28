using System.Data;
using DAL.Repositories;
using ET.VanTai;

namespace BUS.Services
{
    public class VanDonHangService
    {
        private readonly VanDonHangRepository _repo = new();

        public DataTable LayDanhSach() => _repo.LayDanhSach();

        public DataTable LayDanhSachLoaiHangHoa() => _repo.LayDanhSachLoaiHangHoa();

        public decimal TinhCuocPhi(decimal trongLuongTan, decimal theTichM3, decimal cuLyKm, int nhomCuoc, decimal donGiaTanKm)
        {
            if (cuLyKm <= 0 || donGiaTanKm <= 0) return 0;

            // Quy đổi thể tích: 1m3 = 333 kg = 0.333 tấn
            decimal quyDoiTan = theTichM3 * 0.333m;
            decimal trongLuongTinhCuoc = Math.Max(trongLuongTan, quyDoiTan);
            if (trongLuongTinhCuoc <= 0) trongLuongTinhCuoc = 0.05m; // Tối thiểu 50kg

            // Hệ số nhóm hàng hóa: Nhóm 1 = 1.0, Nhóm 2 = 1.15, Nhóm 3 = 1.30
            decimal heSoNhom = nhomCuoc switch
            {
                2 => 1.15m,
                3 => 1.30m,
                _ => 1.00m
            };

            decimal cuoc = trongLuongTinhCuoc * cuLyKm * donGiaTanKm * heSoNhom;

            // Làm tròn lên hàng nghìn đồng, tối thiểu 50,000 VND
            cuoc = Math.Max(50000m, Math.Ceiling(cuoc / 1000m) * 1000m);
            return cuoc;
        }

        public bool Them(VanDonHang vd) => Them(vd, out _);

        public bool Them(VanDonHang vd, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(vd.MaVanDonCode))
            {
                error = "Mã vận đơn không được để trống.";
                return false;
            }

            if (!KiemTraHopLe(vd, out error)) return false;

            try
            {
                bool ok = _repo.Them(vd) > 0;
                if (!ok) error = "Không thể ghi nhận vận đơn vào cơ sở dữ liệu.";
                return ok;
            }
            catch (Exception ex)
            {
                error = $"Lỗi cơ sở dữ liệu: {ex.Message}";
                return false;
            }
        }

        // Cập nhật thông tin vận đơn khi chưa xếp toa
        public bool CapNhat(VanDonHang vd, out string error)
        {
            error = string.Empty;

            if (vd.MaVanDon <= 0)
            {
                error = "Không xác định được mã vận đơn cần chỉnh sửa.";
                return false;
            }

            if (!KiemTraHopLe(vd, out error)) return false;

            try
            {
                bool ok = _repo.CapNhat(vd) > 0;
                if (!ok)
                {
                    error = "Không thể cập nhật. Vận đơn đã được xếp toa, đang vận chuyển hoặc không tồn tại.";
                }
                return ok;
            }
            catch (Exception ex)
            {
                error = $"Lỗi cơ sở dữ liệu: {ex.Message}";
                return false;
            }
        }

        // Hủy vận đơn khi còn ở trạng thái tiếp nhận
        public bool Huy(int maVanDon, out string error)
        {
            error = string.Empty;
            if (maVanDon <= 0)
            {
                error = "Mã vận đơn không hợp lệ.";
                return false;
            }

            try
            {
                bool ok = _repo.Xoa(maVanDon) > 0;
                if (!ok)
                {
                    error = "Không thể hủy. Vận đơn đã được xếp lên toa, đang chạy hoặc không tồn tại.";
                }
                return ok;
            }
            catch (Exception ex)
            {
                error = $"Lỗi cơ sở dữ liệu: {ex.Message}";
                return false;
            }
        }

        public bool CapNhatTrangThai(int maVanDon, string trangThai, string? ghiChu = null)
        {
            if (maVanDon <= 0) return false;
            return _repo.CapNhatTrangThaiVaGhiChu(maVanDon, trangThai, ghiChu) > 0;
        }

        // Kiểm tra hợp lệ dữ liệu nhập cho vận đơn
        private bool KiemTraHopLe(VanDonHang vd, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(vd.TenNguoiGui))
            {
                error = "Vui lòng nhập họ tên người gửi.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(vd.SDTNguoiGui))
            {
                error = "Vui lòng nhập số điện thoại người gửi.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(vd.TenNguoiNhan))
            {
                error = "Vui lòng nhập họ tên người nhận.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(vd.SDTNguoiNhan))
            {
                error = "Vui lòng nhập số điện thoại người nhận.";
                return false;
            }
            if (vd.MaGaGui <= 0 || vd.MaGaNhan <= 0)
            {
                error = "Vui lòng chọn Ga gửi và Ga nhận hợp lệ.";
                return false;
            }
            if (vd.MaGaGui == vd.MaGaNhan)
            {
                error = "Ga gửi và Ga nhận không được trùng nhau.";
                return false;
            }
            if (vd.MaLoaiHang <= 0)
            {
                error = "Vui lòng chọn loại mặt hàng vận chuyển.";
                return false;
            }
            if (vd.TrongLuongTan <= 0)
            {
                error = "Trọng lượng hàng hóa phải lớn hơn 0 (tấn).";
                return false;
            }
            if (vd.CuocPhi <= 0)
            {
                error = "Cước phí vận chuyển phải lớn hơn 0.";
                return false;
            }

            // Ràng buộc riêng theo từng hình thức vận tải
            if (vd.LoaiVanChuyen == "XE_MAY")
            {
                if (string.IsNullOrWhiteSpace(vd.BienKiemSoat))
                {
                    error = "Vận chuyển xe máy bắt buộc phải nhập Biển kiểm soát xe.";
                    return false;
                }
                if (vd.DaRutXang != true)
                {
                    error = "Theo quy chuẩn an toàn PCCC VNR, xe máy ký gửi phải được rút sạch xăng.";
                    return false;
                }
                vd.SoHieuContainer = null;
                vd.SoChiHaiQuan = null;
            }
            else if (vd.LoaiVanChuyen == "CONTAINER")
            {
                if (string.IsNullOrWhiteSpace(vd.SoHieuContainer))
                {
                    error = "Vận chuyển Container bắt buộc phải nhập Số hiệu Container.";
                    return false;
                }
                vd.BienKiemSoat = null;
                vd.DaRutXang = null;
            }
            else // HANG_HOA
            {
                vd.LoaiVanChuyen = "HANG_HOA";
                vd.BienKiemSoat = null;
                vd.DaRutXang = null;
                vd.SoHieuContainer = null;
                vd.SoChiHaiQuan = null;
            }

            return true;
        }
    }
}
