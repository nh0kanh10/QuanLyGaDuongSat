using System.Text.Json;
using DAL.Connection;
using DAL.Repositories;
using DTO.Common;
using DTO.PhuongTien;

namespace BUS.Services
{
    // =========================================================================
    // LAP DOAN TAU (vanhanh.DoanTau + ChiTietDoanTau) - KE_HOACH_BACK_END.md §4.3
    //
    // Quyet dinh 09/10:
    //   - Chi lap / sua doan tau khi chuyen "Da len lich" hoac "San sang".
    //   - Luu phuong an co doi thanh phan thi bo co DaDuyetAnToan (phai duyet lai - Bao tri).
    //   - Co dau may day (tuy chon). Chieu dai doi chieu tung ga dung.
    //
    // LuuPhuongAn chay trong MOT giao dich:
    //   khoa ung dung "vanhanh.LapDoanTau" (tuan tu hoa cac lan luu, de luat "mot dau may /
    //   toa khong thuoc 2 chuyen chong gio" khong bi 2 nguoi luu cung luc lam sai)
    //   -> nap lai boi canh (khoa dong chuyen + doan tau) -> kiem trang thai chuyen
    //   -> kiem PhienBan -> chay lai QuyTacLapTau -> ghi DoanTau + ChiTietDoanTau
    //   -> ghi nhat ky -> COMMIT.
    //
    // Chua lam (ngoai Sprint 1): dong bo vantai.ToaXeKhach theo doan tau
    // (ChuyenTauService.DongBoToaXeKhachTuDoanTau cua A Ba, hop dong §7.1, Sprint 2).
    // =========================================================================
    public class LapTauService
    {
        public const string TaiNguyenKhoa = "vanhanh.LapDoanTau";

        private readonly LapTauRepository _repo = new();

        public BoiCanhLapTauDTO? LayBoiCanh(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return null;
            var bc = _repo.LayBoiCanh(maChuyenTau);
            if (bc != null) DienQuyenSua(bc);
            return bc;
        }

        public static KetQuaKiemTraLapTauDTO KiemTra(BoiCanhLapTauDTO bc, PhuongAnLapTauDTO pa)
            => QuyTacLapTau.KiemTra(bc, pa);

        // Doan tau dang luu trong CSDL duoi dang phuong an (de kiem tra / hien thi / mo keo - tha)
        public static PhuongAnLapTauDTO PhuongAnHienTai(BoiCanhLapTauDTO bc) => new()
        {
            MaChuyenTau = bc.MaChuyenTau,
            PhienBan = bc.PhienBan,
            MaDauMayChinh = bc.MaDauMayChinh,
            MaDauMayDay = bc.MaDauMayDay,
            DanhSachToa = bc.DoanHienTai.Select(t => new ToaTrongDoanDTO
            {
                MaToaXe = t.MaToaXe,
                MaGaNoi = t.MaGaNoi,
                MaGaCat = t.MaGaCat
            }).ToList()
        };

        public bool LuuPhuongAn(PhuongAnLapTauDTO pa, out string loi, out KetQuaLuuLapTauDTO? ketQua)
        {
            loi = "";
            ketQua = null;

            if (!PhanQuyenService.CoQuyen(MaQuyen.LapDoanTau))
            {
                loi = $"Tài khoản \"{PhanQuyenService.TenDangNhapHienTai}\" không có quyền lập đoàn tàu (cần quyền {MaQuyen.LapDoanTau}).";
                return false;
            }
            if (pa == null || pa.MaChuyenTau <= 0)
            {
                loi = "Chưa chọn chuyến tàu để lập đoàn tàu.";
                return false;
            }
            if (pa.MaDauMayChinh is not int maDauMayChinh)
            {
                loi = "Chưa chọn đầu máy kéo chính.";
                return false;
            }
            if (pa.DanhSachToa.Count == 0)
            {
                loi = "Đoàn tàu chưa có toa nào.";
                return false;
            }

            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(TaiNguyenKhoa);

                var bc = _repo.LayBoiCanh(pa.MaChuyenTau, gd, khoaDong: true);
                if (bc == null)
                {
                    loi = "Không tìm thấy chuyến tàu (có thể đã bị xóa). Hãy nạp lại danh sách chuyến.";
                    return false;
                }

                DienQuyenSua(bc);
                if (!bc.ChoPhepSua)
                {
                    loi = bc.LyDoKhongChoSua;
                    return false;
                }

                if (!CungPhienBan(pa.PhienBan, bc.PhienBan))
                {
                    loi = bc.PhienBan == null
                        ? "Đoàn tàu của chuyến này đã bị xóa sau khi bạn mở màn hình lập tàu. Hãy nạp lại rồi lập lại."
                        : "Đoàn tàu của chuyến này vừa được người khác lập / sửa sau khi bạn mở màn hình lập tàu. Hãy nạp lại rồi lập lại.";
                    return false;
                }

                pa = ChuanHoaPhamVi(bc, pa);

                var kt = QuyTacLapTau.KiemTra(bc, pa);
                if (kt.CoLoi)
                {
                    var hangMucLoi = kt.HangMuc.Where(h => h.MucDo == MucDoKiemTra.Loi).ToList();
                    loi = $"Chưa lưu được vì còn {hangMucLoi.Count} hạng mục không đạt:\n\n" +
                          string.Join("\n", hangMucLoi.Select(h => $"• {h.Ten}: {h.NoiDung.Split('\n')[0]}"));
                    return false;
                }

                bool laMoi = bc.MaDoanTau == null;
                if (!laMoi && !KhacThanhPhan(bc, pa))
                {
                    ketQua = TaoKetQua(bc.MaDoanTau!.Value, laMoi: false, khongThayDoi: true, daHuyDuyet: false, pa, kt);
                    return true;
                }

                int maDoanTau;
                if (laMoi)
                {
                    maDoanTau = _repo.ThemDoanTau(bc.MaChuyenTau, maDauMayChinh, pa.MaDauMayDay,
                                                  kt.SoToaLonNhat, kt.ChieuDaiLonNhatM, kt.TrongLuongLonNhatTan, gd);
                }
                else
                {
                    maDoanTau = bc.MaDoanTau!.Value;
                    int soDong = _repo.CapNhatDoanTau(maDoanTau, bc.PhienBan!, maDauMayChinh, pa.MaDauMayDay,
                                                      kt.SoToaLonNhat, kt.ChieuDaiLonNhatM, kt.TrongLuongLonNhatTan,
                                                      daDuyetAnToan: false, gd);
                    if (soDong == 0)
                    {
                        loi = "Đoàn tàu của chuyến này vừa được người khác lập / sửa. Hãy nạp lại rồi lập lại.";
                        return false;
                    }
                }

                _repo.ThayChiTietDoanTau(maDoanTau, pa.DanhSachToa, gd);

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai,
                                    laMoi ? "LAP_DOAN_TAU" : "SUA_DOAN_TAU", "vanhanh.DoanTau", maDoanTau.ToString(),
                                    laMoi ? null : MoTaJson(bc, bc.MaDauMayChinh, bc.MaDauMayDay, bc.DoanHienTai,
                                                            bc.TongSoToa, bc.TongChieuDaiM, bc.TongTrongLuongTan, bc.DaDuyetAnToan),
                                    MoTaJson(bc, pa.MaDauMayChinh, pa.MaDauMayDay, pa.DanhSachToa,
                                             kt.SoToaLonNhat, kt.ChieuDaiLonNhatM, kt.TrongLuongLonNhatTan, false));
                gd.XacNhan();

                ketQua = TaoKetQua(maDoanTau, laMoi, khongThayDoi: false, daHuyDuyet: !laMoi && bc.DaDuyetAnToan, pa, kt);
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // ---------------------------------------------------------------------

        private static void DienQuyenSua(BoiCanhLapTauDTO bc)
        {
            var (choPhep, lyDo) = QuyTacLapTau.KiemTraChoPhepSua(bc.TrangThaiChuyen);
            bc.ChoPhepSua = choPhep;
            bc.LyDoKhongChoSua = lyDo;
        }

        // Ga noi = ga dau / ga cat = ga cuoi cung nghia "chay suot" (NULL). Quy ve NULL de CSDL chi
        // luu mot kieu, va de so sanh "co thay doi" khong bao sai roi bo co duyet an toan oan.
        private static PhuongAnLapTauDTO ChuanHoaPhamVi(BoiCanhLapTauDTO bc, PhuongAnLapTauDTO pa) => new()
        {
            MaChuyenTau = pa.MaChuyenTau,
            PhienBan = pa.PhienBan,
            MaDauMayChinh = pa.MaDauMayChinh,
            MaDauMayDay = pa.MaDauMayDay,
            DanhSachToa = pa.DanhSachToa.Select(t => ChuanHoaPhamVi(bc, t)).ToList()
        };

        private static ToaTrongDoanDTO ChuanHoaPhamVi(BoiCanhLapTauDTO bc, ToaTrongDoanDTO t)
        {
            int? gaDau = bc.HanhTrinh.Count > 0 ? bc.HanhTrinh[0].MaGa : null;
            int? gaCuoi = bc.HanhTrinh.Count > 0 ? bc.HanhTrinh[^1].MaGa : null;
            return new ToaTrongDoanDTO
            {
                MaToaXe = t.MaToaXe,
                MaGaNoi = t.MaGaNoi == gaDau ? null : t.MaGaNoi,
                MaGaCat = t.MaGaCat == gaCuoi ? null : t.MaGaCat
            };
        }

        private static bool CungPhienBan(byte[]? a, byte[]? b)
            => a == null ? b == null : b != null && a.AsSpan().SequenceEqual(b);

        // Dau may, danh sach toa (thu tu + pham vi) co khac doan tau dang luu khong
        private static bool KhacThanhPhan(BoiCanhLapTauDTO bc, PhuongAnLapTauDTO pa)
        {
            if (bc.MaDauMayChinh != pa.MaDauMayChinh || bc.MaDauMayDay != pa.MaDauMayDay) return true;
            if (bc.DoanHienTai.Count != pa.DanhSachToa.Count) return true;

            for (int i = 0; i < pa.DanhSachToa.Count; i++)
            {
                var cu = ChuanHoaPhamVi(bc, bc.DoanHienTai[i]);
                var moi = pa.DanhSachToa[i];
                if (cu.MaToaXe != moi.MaToaXe || cu.MaGaNoi != moi.MaGaNoi || cu.MaGaCat != moi.MaGaCat) return true;
            }
            return false;
        }

        private static KetQuaLuuLapTauDTO TaoKetQua(int maDoanTau, bool laMoi, bool khongThayDoi, bool daHuyDuyet,
                                                   PhuongAnLapTauDTO pa, KetQuaKiemTraLapTauDTO kt) => new()
        {
            MaDoanTau = maDoanTau,
            LaLapMoi = laMoi,
            KhongThayDoi = khongThayDoi,
            DaHuyDuyetAnToan = daHuyDuyet,
            SoToaTrongPhuongAn = pa.DanhSachToa.Count,
            TongSoToa = kt.SoToaLonNhat,
            TongChieuDaiM = kt.ChieuDaiLonNhatM,
            TongTrongLuongTan = kt.TrongLuongLonNhatTan,
            MoTaCatNoi = kt.MoTaCatNoi
        };

        // Anh chup doan tau cho nhat ky he thong (so hieu de doc duoc khi tra cuu nhat ky)
        private static string MoTaJson(BoiCanhLapTauDTO bc, int? maDauMayChinh, int? maDauMayDay,
                                       IEnumerable<ToaTrongDoanDTO> toa, int tongSoToa, decimal tongDai, decimal tongNang,
                                       bool daDuyet)
        {
            string? MaGa(int? maGa) => maGa == null ? null : bc.HanhTrinh.FirstOrDefault(d => d.MaGa == maGa)?.MaGaCode ?? $"#{maGa}";

            var anhChup = new
            {
                bc.MaChuyenTau,
                MacTau = bc.SoHieuMacTau,
                DauMayChinh = bc.TimDauMay(maDauMayChinh)?.SoHieuDauMay,
                DauMayDay = bc.TimDauMay(maDauMayDay)?.SoHieuDauMay,
                TongSoToa = tongSoToa,
                TongChieuDaiM = tongDai,
                TongTrongLuongTan = tongNang,
                DaDuyetAnToan = daDuyet,
                Toa = toa.Select((t, i) => new
                {
                    ThuTu = i + 1,
                    t.MaToaXe,
                    SoHieu = bc.TimToa(t.MaToaXe)?.SoHieuToaXe,
                    GaNoi = MaGa(t.MaGaNoi),
                    GaCat = MaGa(t.MaGaCat)
                })
            };
            return JsonSerializer.Serialize(anhChup);
        }
    }
}
