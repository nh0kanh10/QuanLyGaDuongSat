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
            string sql = @"SELECT vd.*, g1.TenGa AS TenGaGui, g2.TenGa AS TenGaNhan, lh.TenHangHoa
                           FROM vantai.VanDonHang vd
                           JOIN hatang.Ga g1 ON vd.MaGaGui = g1.MaGa
                           JOIN hatang.Ga g2 ON vd.MaGaNhan = g2.MaGa
                           JOIN vantai.LoaiHangHoa lh ON vd.MaLoaiHang = lh.MaLoaiHang
                           ORDER BY vd.ThoiDiemTao DESC";
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
                new SqlParameter("@bienKS", (object?)vd.BienKiemSoat ?? DBNull.Value),
                new SqlParameter("@rutXang", (object?)vd.DaRutXang ?? DBNull.Value),
                new SqlParameter("@container", (object?)vd.SoHieuContainer ?? DBNull.Value),
                new SqlParameter("@chiHQ", (object?)vd.SoChiHaiQuan ?? DBNull.Value),
                new SqlParameter("@ghiChu", (object?)vd.GhiChu ?? DBNull.Value)
            };
            return DatabaseHelper.ExecuteNonQuery(sql, p);
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
    }
}
