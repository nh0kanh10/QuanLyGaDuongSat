using System.Data;
using DAL.Repositories;
using ET.VanTai;

namespace BUS.Services
{
    public class VeService
    {
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

        public decimal TinhGiaVe(string loaiToa, int maGaDi, int maGaDen, int? tangGiuong = null)
        {
            if (maGaDi <= 0 || maGaDen <= 0) return 0;
            return _repo.TinhGiaVe(loaiToa, maGaDi, maGaDen, tangGiuong);
        }

        // Tinh muc giam gia theo Luat Duong sat 2017
        public decimal TinhMucGiamGia(decimal giaGoc, string loaiKhach)
        {
            if (giaGoc <= 0) return 0;
            return loaiKhach switch
            {
                "TRE_EM" => Math.Round(giaGoc * 0.25m, 0),    // Giam 25% cho tre em
                "NGUOI_GIA" => Math.Round(giaGoc * 0.15m, 0),  // Giam 15% cho nguoi cao tuoi >= 60
                "SINH_VIEN" => Math.Round(giaGoc * 0.10m, 0),  // Giam 10% cho sinh vien
                _ => 0m
            };
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
    }
}
