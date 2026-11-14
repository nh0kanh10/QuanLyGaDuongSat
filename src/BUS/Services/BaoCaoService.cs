using System;
using System.Collections.Generic;
using DAL.Repositories;
using DTO.BaoCao;
using DTO.Common;

namespace BUS.Services
{
    // =========================================================================
    // BAO CAO THONG KE TONG HOP (Sprint 3, Dat - FR-7.3). Can quyen REPORT_VIEW. Chi doc.
    // Ranh gioi Q8: bao cao trong phan he (doanh thu ban ve, bao cao ga) van o trang cua A Ba;
    // day la trang tong hop lien phan he: doanh thu, he so su dung ghe, ty le dung gio, san luong hang.
    // =========================================================================
    public class BaoCaoService
    {
        public const int SoNgayToiDa = 400;
        private readonly BaoCaoRepository _repo = new();

        public bool LayDanhSachMacTau(out List<string> ds, out string loi)
        {
            ds = new();
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.XemBaoCao, "xem báo cáo thống kê", out loi)) return false;
            try { ds = _repo.LayDanhSachMacTau(); return true; }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex)) { loi = DichLoiSqlPhuongTien.ThongDiep(ex); return false; }
        }

        public static string? KiemTraBoLoc(BoLocBaoCao loc)
        {
            if (loc.DenNgay.Date < loc.TuNgay.Date) return "\"Đến ngày\" phải sau hoặc bằng \"Từ ngày\".";
            if ((loc.DenNgay.Date - loc.TuNgay.Date).TotalDays > SoNgayToiDa) return $"Khoảng thời gian tối đa {SoNgayToiDa} ngày.";
            return null;
        }

        public bool LayBaoCao(BoLocBaoCao loc, out KetQuaBaoCao kq, out string loi)
        {
            kq = new KetQuaBaoCao { BoLoc = loc };
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.XemBaoCao, "xem báo cáo thống kê", out loi)) return false;
            if (KiemTraBoLoc(loc) is { } l) { loi = l; return false; }
            try
            {
                kq = _repo.LayBaoCao(loc);
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
                    ? "CSDL chưa có các view báo cáo (schema baocao). Chạy migration V20261108_1300 hoặc database\\tao_lai_csdl.cmd."
                    : DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }
    }
}
