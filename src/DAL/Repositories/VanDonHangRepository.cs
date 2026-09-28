using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using ET.VanTai;

namespace DAL.Repositories
{
    public class VanDonHangRepository
    {
        public DataTable LayDanhSach()
        {
            string sql = @"SELECT vd.*, g1.TenGa AS TenGaGui, g2.TenGa AS TenGaNhan, lh.TenHangHoa, lh.NhomCuoc, lh.DonGiaMoiTanKm,
                                  ABS(ISNULL(g2.LyTrinhKm, 0) - ISNULL(g1.LyTrinhKm, 0)) AS CuLyKm,
                                  COALESCE(NULLIF(vd.BienKiemSoat, ''), NULLIF(vd.SoHieuContainer, ''), N'—') AS ThongTinDacThu
                           FROM vantai.VanDonHang vd
                           JOIN hatang.Ga g1 ON vd.MaGaGui = g1.MaGa
                           JOIN hatang.Ga g2 ON vd.MaGaNhan = g2.MaGa
                           JOIN vantai.LoaiHangHoa lh ON vd.MaLoaiHang = lh.MaLoaiHang
                           ORDER BY vd.ThoiDiemTao DESC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable LayDanhSachLoaiHangHoa()
        {
            string sql = @"SELECT MaLoaiHang, TenHangHoa, NhomCuoc, DonGiaMoiTanKm 
                           FROM vantai.LoaiHangHoa 
                           ORDER BY NhomCuoc, MaLoaiHang";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public int Them(VanDonHang vd)
        
        {
            string sql = @"INSERT INTO vantai.VanDonHang 
                           (MaVanDonCode, TenNguoiGui, SDTNguoiGui, TenNguoiNhan, SDTNguoiNhan,
                            MaGaGui, MaGaNhan, MaLoaiHang, TrongLuongTan, CuocPhi, 
                            LoaiVanChuyen, BienKiemSoat, DaRutXang, SoHieuContainer, SoChiHaiQuan, GhiChu)
                           VALUES (@code, @nguoiGui, @sdtGui, @nguoiNhan, @sdtNhan,
                                   @gaGui, @gaNhan, @loaiHang, @trongLuong, @cuoc,
                                   @loaiVC, @bienKS, @rutXang, @container, @chiHQ, @ghiChu)";
            var p = new[]
            {
                new SqlParameter("@code", vd.MaVanDonCode),
                new SqlParameter("@nguoiGui", vd.TenNguoiGui),
                new SqlParameter("@sdtGui", vd.SDTNguoiGui),
                new SqlParameter("@nguoiNhan", vd.TenNguoiNhan),
                new SqlParameter("@sdtNhan", vd.SDTNguoiNhan),
                new SqlParameter("@gaGui", vd.MaGaGui),
                new SqlParameter("@gaNhan", vd.MaGaNhan),
                new SqlParameter("@loaiHang", vd.MaLoaiHang),
                new SqlParameter("@trongLuong", vd.TrongLuongTan),
                new SqlParameter("@cuoc", vd.CuocPhi),
                new SqlParameter("@loaiVC", vd.LoaiVanChuyen),
                new SqlParameter("@bienKS", string.IsNullOrWhiteSpace(vd.BienKiemSoat) ? DBNull.Value : vd.BienKiemSoat.Trim()),
                new SqlParameter("@rutXang", vd.DaRutXang.HasValue ? (object)vd.DaRutXang.Value : DBNull.Value),
                new SqlParameter("@container", string.IsNullOrWhiteSpace(vd.SoHieuContainer) ? DBNull.Value : vd.SoHieuContainer.Trim()),
                new SqlParameter("@chiHQ", string.IsNullOrWhiteSpace(vd.SoChiHaiQuan) ? DBNull.Value : vd.SoChiHaiQuan.Trim()),
                new SqlParameter("@ghiChu", string.IsNullOrWhiteSpace(vd.GhiChu) ? DBNull.Value : vd.GhiChu.Trim())
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        // Cập nhật thông tin vận đơn khi chưa xếp toa (TrangThai = 'DA_NHAN')
        public int CapNhat(VanDonHang vd)
        {
            string sql = @"UPDATE vantai.VanDonHang 
                           SET TenNguoiGui = @nguoiGui,
                               SDTNguoiGui = @sdtGui,
                               TenNguoiNhan = @nguoiNhan,
                               SDTNguoiNhan = @sdtNhan,
                               MaGaGui = @gaGui,
                               MaGaNhan = @gaNhan,
                               MaLoaiHang = @loaiHang,
                               TrongLuongTan = @trongLuong,
                               CuocPhi = @cuoc,
                               LoaiVanChuyen = @loaiVC,
                               BienKiemSoat = @bienKS,
                               DaRutXang = @rutXang,
                               SoHieuContainer = @container,
                               SoChiHaiQuan = @chiHQ,
                               GhiChu = @ghiChu
                           WHERE MaVanDon = @id AND TrangThai = 'DA_NHAN'";
            var p = new[]
            {
                new SqlParameter("@id", vd.MaVanDon),
                new SqlParameter("@nguoiGui", vd.TenNguoiGui),
                new SqlParameter("@sdtGui", vd.SDTNguoiGui),
                new SqlParameter("@nguoiNhan", vd.TenNguoiNhan),
                new SqlParameter("@sdtNhan", vd.SDTNguoiNhan),
                new SqlParameter("@gaGui", vd.MaGaGui),
                new SqlParameter("@gaNhan", vd.MaGaNhan),
                new SqlParameter("@loaiHang", vd.MaLoaiHang),
                new SqlParameter("@trongLuong", vd.TrongLuongTan),
                new SqlParameter("@cuoc", vd.CuocPhi),
                new SqlParameter("@loaiVC", vd.LoaiVanChuyen),
                new SqlParameter("@bienKS", string.IsNullOrWhiteSpace(vd.BienKiemSoat) ? DBNull.Value : vd.BienKiemSoat.Trim()),
                new SqlParameter("@rutXang", vd.DaRutXang.HasValue ? (object)vd.DaRutXang.Value : DBNull.Value),
                new SqlParameter("@container", string.IsNullOrWhiteSpace(vd.SoHieuContainer) ? DBNull.Value : vd.SoHieuContainer.Trim()),
                new SqlParameter("@chiHQ", string.IsNullOrWhiteSpace(vd.SoChiHaiQuan) ? DBNull.Value : vd.SoChiHaiQuan.Trim()),
                new SqlParameter("@ghiChu", string.IsNullOrWhiteSpace(vd.GhiChu) ? DBNull.Value : vd.GhiChu.Trim())
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
        }

        // Hủy (xóa) vận đơn khi còn ở trạng thái tiếp nhận
        public int Xoa(int maVanDon)
        {
            string sql = "DELETE FROM vantai.VanDonHang WHERE MaVanDon = @id AND TrangThai = 'DA_NHAN'";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@id", maVanDon)
            });
        }

        public int CapNhatTrangThai(int maVanDon, string trangThai)
        {
            string sql = "UPDATE vantai.VanDonHang SET TrangThai=@tt WHERE MaVanDon=@id";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@id", maVanDon),
                new SqlParameter("@tt", trangThai)
            });
        }

        // Cập nhật trạng thái và nối thêm ghi chú vào lịch sử xử lý
        public int CapNhatTrangThaiVaGhiChu(int maVanDon, string trangThai, string? ghiChu = null)
        {
            if (string.IsNullOrWhiteSpace(ghiChu))
            {
                return CapNhatTrangThai(maVanDon, trangThai);
            }
            string sql = @"UPDATE vantai.VanDonHang 
                           SET TrangThai = @tt, 
                               GhiChu = CASE 
                                   WHEN GhiChu IS NULL OR LTRIM(RTRIM(GhiChu)) = '' THEN @ghiChu
                                   ELSE GhiChu + CHAR(13) + CHAR(10) + @ghiChu 
                               END 
                           WHERE MaVanDon = @id";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@id", maVanDon),
                new SqlParameter("@tt", trangThai),
                new SqlParameter("@ghiChu", ghiChu.Trim())
            });
        }
    }
}
