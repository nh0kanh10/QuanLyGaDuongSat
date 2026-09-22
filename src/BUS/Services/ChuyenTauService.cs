using System.Data;
using DAL.Repositories;
using ET.VanHanh;

namespace BUS.Services
{
    public class ChuyenTauService
    {
        private readonly ChuyenTauRepository _repo = new();

        public DataTable LayDanhSach(DateTime? ngay = null, int? maMacTau = null, string? trangThai = null)
        {
            return _repo.LayDanhSach(ngay, maMacTau, trangThai);
        }

        public DataTable LayDanhSachMacTau() => _repo.LayDanhSachMacTau();

        public DataTable LayLichDungGa(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayLichDungGa(maChuyenTau);
        }

        public DataTable LayDanhSachToaXe(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayDanhSachToaXe(maChuyenTau);
        }

        public int Them(ChuyenTau ct)
        {
            if (ct.MaMacTau <= 0) return 0;
            if (ct.GioVeDichKH <= ct.GioXuatPhatKH) return 0;
            return _repo.Them(ct);
        }

        public bool CapNhat(ChuyenTau ct)
        {
            if (ct.MaChuyenTau <= 0 || ct.MaMacTau <= 0) return false;
            if (ct.GioVeDichKH <= ct.GioXuatPhatKH) return false;
            return _repo.CapNhat(ct) > 0;
        }

        public bool CapNhatTrangThai(int maChuyenTau, string trangThai)
        {
            if (maChuyenTau <= 0) return false;
            return _repo.CapNhatTrangThai(maChuyenTau, trangThai) > 0;
        }

        public bool CapNhatGioThucTe(int maDiemDung, DateTime? gioDenTT, DateTime? gioDiTT)
        {
            if (maDiemDung <= 0) return false;
            return _repo.CapNhatGioThucTe(maDiemDung, gioDenTT, gioDiTT) > 0;
        }

        public bool ThemLichDungGa(LichDungGa ld)
        {
            if (ld.MaChuyenTau <= 0 || ld.MaGa <= 0 || ld.ThuTuDung <= 0) return false;
            if (ld.GioDiKeHoach < ld.GioDenKeHoach) return false;
            return _repo.ThemLichDungGa(ld) > 0;
        }

        public bool XoaLichDungGa(int maDiemDung)
        {
            if (maDiemDung <= 0) return false;
            return _repo.XoaLichDungGa(maDiemDung) > 0;
        }

        public bool SaoChepLichDungTuChuyenMau(int maMacTau, int maChuyenDich, DateTime ngayKhoiHanh)
        {
            if (maMacTau <= 0 || maChuyenDich <= 0) return false;
            int maChuyenMau = _repo.TimChuyenMauGanNhatCungMac(maMacTau, maChuyenDich);
            if (maChuyenMau <= 0) return false;
            return _repo.SaoChepLichDungTuChuyenKhac(maChuyenMau, maChuyenDich, ngayKhoiHanh) > 0;
        }

        public bool Xoa(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return false;
            return _repo.Xoa(maChuyenTau) > 0;
        }

        // =========================================================================
        // NGHIỆP VỤ ĐOÀN TÀU & ĐẦU MÁY (DoanTau, DauMay)
        // =========================================================================

        public DataTable LayThongTinDoanTau(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayThongTinDoanTau(maChuyenTau);
        }

        public DataTable LayDanhSachDauMay(DateTime? gioDi = null, DateTime? gioDen = null, int? maChuyenTauHienTai = null)
        {
            return _repo.LayDanhSachDauMay(gioDi, gioDen, maChuyenTauHienTai);
        }

        public bool KiemTraXungDotDauMay(int maDauMay, DateTime gioDi, DateTime gioDen, int maChuyenHienTai, out string thongBaoLoi)
        {
            if (maDauMay <= 0)
            {
                thongBaoLoi = "Chưa chọn đầu máy!";
                return false;
            }
            if (gioDen <= gioDi)
            {
                thongBaoLoi = "Giờ đến dự kiến phải lớn hơn giờ xuất phát!";
                return false;
            }
            return _repo.KiemTraXungDotDauMay(maDauMay, gioDi, gioDen, maChuyenHienTai, out thongBaoLoi);
        }

        public bool LuuDoanTau(DoanTau dt, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            if (dt.MaChuyenTau <= 0)
            {
                thongBaoLoi = "Mã chuyến tàu không hợp lệ!";
                return false;
            }
            if (dt.MaDauMayChinh <= 0)
            {
                thongBaoLoi = "Vui lòng chỉ định Đầu Máy Kéo Chính!";
                return false;
            }
            if (dt.TongChieuDaiM > 450.0m)
            {
                thongBaoLoi = $"Chiều dài đoàn tàu ({dt.TongChieuDaiM:F1}m) vượt quá trần quy chuẩn an toàn tối đa (450m)!";
                return false;
            }
            if (dt.TongSoToa <= 0 || dt.TongSoToa > 20)
            {
                thongBaoLoi = "Tổng số toa đoàn tàu phải từ 1 đến 20 toa!";
                return false;
            }

            return _repo.LuuDoanTau(dt) > 0;
        }

        public bool KiemTraChieuDaiVoiGaDung(int maChuyenTau, decimal tongChieuDaiM, out string canhBao)
        {
            if (maChuyenTau <= 0)
            {
                canhBao = string.Empty;
                return true;
            }
            return _repo.KiemTraChieuDaiVoiGaDung(maChuyenTau, tongChieuDaiM, out canhBao);
        }

        // =========================================================================
        // NGHIỆP VỤ BIÊN CHẾ TOA XE KHÁCH (ToaXeKhach)
        // =========================================================================

        public bool ThemToaXeKhach(ET.VanTai.ToaXeKhach tx, bool tuDongSinhGhe, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            if (tx.MaChuyenTau <= 0)
            {
                thongBaoLoi = "Chưa chọn chuyến tàu!";
                return false;
            }
            if (string.IsNullOrWhiteSpace(tx.NhanHieuToa))
            {
                thongBaoLoi = "Ký hiệu toa không được để trống!";
                return false;
            }
            if (tx.ThuTuToa <= 0)
            {
                thongBaoLoi = "Thứ tự toa phải lớn hơn 0!";
                return false;
            }
            if (tx.SucChua <= 0)
            {
                thongBaoLoi = "Sức chứa toa xe phải lớn hơn 0!";
                return false;
            }

            int maToa = _repo.ThemToaXeKhach(tx, tuDongSinhGhe);
            return maToa > 0;
        }

        public bool XoaToaXeKhach(int maToaXeKhach, out string thongBaoLoi)
        {
            if (maToaXeKhach <= 0)
            {
                thongBaoLoi = "Mã toa xe không hợp lệ!";
                return false;
            }
            return _repo.XoaToaXeKhach(maToaXeKhach, out thongBaoLoi);
        }

        public bool SaoChepBienCheTuChuyenMau(int maMacTau, int maChuyenDich, out string thongBao)
        {
            thongBao = string.Empty;
            if (maMacTau <= 0 || maChuyenDich <= 0)
            {
                thongBao = "Dữ liệu mác tàu hoặc chuyến tàu không hợp lệ!";
                return false;
            }

            int maChuyenMau = _repo.TimChuyenMauGanNhatCungMac(maMacTau, maChuyenDich);
            if (maChuyenMau <= 0)
            {
                thongBao = "Không tìm thấy chuyến tàu mẫu nào cùng mác tàu để sao chép biên chế!";
                return false;
            }

            int soToaSaoChep = _repo.SaoChepBienCheToaXe(maChuyenMau, maChuyenDich);
            thongBao = $"Đã sao chép thành công {soToaSaoChep} toa xe từ chuyến mẫu (Mã #{maChuyenMau})!";
            return soToaSaoChep > 0;
        }
    }
}
