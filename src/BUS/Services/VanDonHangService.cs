using System.Data;
using DAL.Repositories;
using ET.VanTai;

namespace BUS.Services
{
    public class VanDonHangService
    {
        private readonly VanDonHangRepository _repo = new();

        public DataTable LayDanhSach() => _repo.LayDanhSach();

        public bool Them(VanDonHang vd)
        {
            if (string.IsNullOrWhiteSpace(vd.MaVanDonCode)) return false;
            if (vd.MaGaGui <= 0 || vd.MaGaNhan <= 0) return false;
            if (vd.MaGaGui == vd.MaGaNhan) return false;
            if (vd.TrongLuongTan <= 0) return false;

            // xe may bat buoc phai rut xang
            if (vd.LoaiVanChuyen == "XE_MAY")
            {
                if (string.IsNullOrWhiteSpace(vd.BienKiemSoat)) return false;
                if (vd.DaRutXang != true) return false;
            }

            return _repo.Them(vd) > 0;
        }

        public bool CapNhatTrangThai(int maVanDon, string trangThai)
        {
            if (maVanDon <= 0) return false;
            return _repo.CapNhatTrangThai(maVanDon, trangThai) > 0;
        }
    }
}
