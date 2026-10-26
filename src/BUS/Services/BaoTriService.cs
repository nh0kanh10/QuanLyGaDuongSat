using System.Text.Json;
using DAL.Connection;
using DAL.Repositories;
using DTO.BaoTri;
using DTO.Common;
using DTO.PhuongTien;

namespace BUS.Services
{
    // =========================================================================
    // BAO TRI & SUA CHUA - KE_HOACH_BACK_END.md §5.2 (Dat, Sprint 2)
    //
    // Doc : LayDuLieu -> anh chup (DuLieuBaoTri) cho ca trang; luat chay trong bo nho (QuyTacBaoTri,
    //       KiemTraXuatBenService).
    // Ghi : moi thao tac MOT giao dich: quyen -> kiem truong nhap -> giao dich -> (khoa) -> nap lai anh chup
    //       trong giao dich (gio may chu) -> luat -> ghi -> nhat ky -> COMMIT.
    //       Thao tac dung toi doan tau / trang thai phuong tien (lap bien ban, duyet an toan, bao duong) lay
    //       CUNG khoa voi lap tau va Phuong tien (LapTauService.TaiNguyenKhoa): duyet khong chen giua luc doi
    //       thanh phan doan tau, bao duong khong chen giua luc gan dau may vao doan tau.
    //
    // Quyet dinh 24/10 (Dat chot 11 khuyen nghi):
    //   1 SFC Service tinh, dinh muc o DongDauMay.DinhMucSFC · 2 chu ky la hang so (QuyTacBaoTri)
    //   3 chi phi 3 bang (cap dau = so lit x don gia) · 4 nguoi ky phieu = tai khoan vai tro KY_THUAT
    //   5 gio Viet Nam (SYSDATETIME) · 6 bien ban kham KHONG sua / xoa; phieu cap dau, bao duong sua duoc
    //     theo PhienBan; phieu bao duong chi sua khi la phieu moi nhat cua phuong tien; khong xoa phieu
    //   7 ghi bao duong = lam xong: phuong tien dang BAO_DUONG thi dua ve SAN_SANG (nguoi dung chon);
    //     dau may dang chay / phuong tien ngung khai thac thi khong ghi
    //   8 duyet an toan chi xet phan ky thuat (KiemTraXuatBenService.DieuKienDuyet); bien ban khong dat
    //     thi tu bo duyet · 9 xuat ben chan cung (KiemTraXuatBenService)
    // =========================================================================
    public class BaoTriService
    {
        // Noi cap dau goi y khi chua co lich su
        public static readonly string[] NoiCapDauMacDinh =
            { "Trạm dầu Giáp Bát", "Trạm dầu Vinh", "Trạm dầu Đà Nẵng", "Trạm dầu Sài Gòn" };

        private readonly BaoTriRepository _repo = new();

        // =====================================================================
        // DOC
        // =====================================================================

        // Khong ket noi duoc: tra anh chup rong + thong diep (man hinh van mo duoc)
        public bool LayDuLieu(out DuLieuBaoTri duLieu, out string loi)
        {
            loi = "";
            try
            {
                var dl = _repo.LayDuLieu();
                dl.NoiCapDau.AddRange(NoiCapDauMacDinh.Where(n => !dl.NoiCapDau.Contains(n)).ToList());
                duLieu = dl;
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                duLieu = DuLieuBaoTri.Rong(DateTime.Now);
                duLieu.NoiCapDau.AddRange(NoiCapDauMacDinh);
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // =====================================================================
        // KHAM XE (chi lap moi)
        // =====================================================================

        public bool LapBienBan(KhamXeDTO bb, out string loi, out KetQuaLapBienBan? ketQua)
        {
            ketQua = null;
            if (!CoQuyen(MaQuyen.NghiemThuKhamXe, "lập biên bản khám xe", out loi)) return false;

            bb.GhiChuKyThuat = bb.GhiChuKyThuat?.Trim() ?? "";
            if (!HopLe(QuyTacBaoTri.KiemTraKhamXe(bb), out loi)) return false;

            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(LapTauService.TaiNguyenKhoa);
                var dl = _repo.LayDuLieu(gd);

                var dt = dl.TimDoanTau(bb.MaDoanTau);
                if (dt == null)
                {
                    loi = "Đoàn tàu không còn trong CSDL. Hãy nạp lại.";
                    return false;
                }
                if (dt.DaKetThuc)
                {
                    loi = $"Chuyến {dt.SoHieuMacTau} ngày {dt.NgayXuatPhat:dd/MM/yyyy} đã kết thúc, không lập biên bản khám xe nữa.";
                    return false;
                }
                if (KiemTraNguoiKy(dl, bb.MaNguoiKham, "khám xe") is { } loiNguoi)
                {
                    loi = loiNguoi;
                    return false;
                }

                var (ma, luc) = _repo.ThemKhamXe(bb, gd);
                bb.MaKhamXe = ma;
                bb.ThoiDiemKham = luc;
                bool dat = QuyTacBaoTri.DuDieuKienXuatBen(bb);
                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "THEM", "baotri.KhamXeKyThuat", ma.ToString(),
                                    null, Json(new { bb.MaKhamXe, dt.SoHieuMacTau, bb.MaDoanTau, bb.MaNguoiKham, bb.ApLucHamBar,
                                                     bb.DoSutApBarPhut, bb.DaCapNuoc, bb.DaXaVeSinh, DuDieuKienXuatBen = dat,
                                                     bb.GhiChuKyThuat, bb.ChiPhi, bb.ThoiDiemKham }));

                // Bien ban moi nhat khong dat -> doan tau mat duyet (phai kham lai dat roi duyet lai)
                bool boDuyet = !dat && _repo.BoDuyetAnToan(dt.MaDoanTau, gd) > 0;
                if (boDuyet)
                    gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "BO_DUYET_AN_TOAN", "vanhanh.DoanTau",
                                        dt.MaDoanTau.ToString(), Json(new { dt.SoHieuMacTau, DaDuyetAnToan = true }),
                                        Json(new { dt.SoHieuMacTau, DaDuyetAnToan = false, LyDo = $"Biên bản BB-{ma:0000} không đạt" }));

                gd.XacNhan();
                ketQua = new KetQuaLapBienBan(ma, luc, dat, boDuyet);
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // =====================================================================
        // DUYET AN TOAN (UC-BT-03)
        // =====================================================================

        // phienBan: PhienBan cua doan tau luc nguoi dung xem (doan tau doi thanh phan sau do thi khong duyet)
        public bool DuyetAnToan(int maDoanTau, byte[]? phienBan, out string loi)
        {
            if (!CoQuyen(MaQuyen.NghiemThuKhamXe, "duyệt an toàn đoàn tàu", out loi)) return false;

            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(LapTauService.TaiNguyenKhoa);
                var dl = _repo.LayDuLieu(gd);

                var dt = dl.TimDoanTau(maDoanTau);
                if (dt == null)
                {
                    loi = "Đoàn tàu không còn trong CSDL. Hãy nạp lại.";
                    return false;
                }
                if (phienBan == null || !dt.PhienBan.AsSpan().SequenceEqual(phienBan))
                {
                    loi = $"Đoàn tàu {dt.SoHieuMacTau} vừa được thay đổi (lập lại / duyệt / bỏ duyệt) sau khi bạn xem. Hãy nạp lại rồi kiểm tra lại.";
                    return false;
                }

                var dk = KiemTraXuatBenService.DieuKienDuyet(dl, dt);
                if (!dk.CoTheDuyet)
                {
                    loi = dk.LyDoKhongDuyet ?? $"Chưa duyệt được đoàn tàu {dt.SoHieuMacTau}:\n\n{dk.MoTaChan}";
                    return false;
                }

                if (_repo.DuyetAnToan(maDoanTau, phienBan, gd) == 0)
                {
                    loi = $"Đoàn tàu {dt.SoHieuMacTau} vừa được người khác thay đổi. Hãy nạp lại rồi kiểm tra lại.";
                    return false;
                }

                var bb = dl.BienBanMoiNhat(maDoanTau)!;
                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "DUYET_AN_TOAN", "vanhanh.DoanTau", maDoanTau.ToString(),
                                    Json(new { dt.SoHieuMacTau, DaDuyetAnToan = false }),
                                    Json(new { dt.SoHieuMacTau, dt.MaChuyenTau, DaDuyetAnToan = true, BienBan = $"BB-{bb.MaKhamXe:0000}",
                                               NguoiDuyet = PhanQuyenService.TenDangNhapHienTai,
                                               KiemTra = dk.CacMuc.Select(m => $"{m.Ten}: {m.KetQua}") }));
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // =====================================================================
        // NHIEN LIEU
        // =====================================================================

        // Them (MaNhatKyDau <= 0) hoac sua (theo PhienBan). Service tu tinh SFC + co vuot muc + thanh tien.
        public bool LuuCapDau(CapNhienLieuDTO nl, out string loi)
        {
            if (!CoQuyen(MaQuyen.GhiNhatKyBaoTri, "ghi phiếu cấp nhiên liệu", out loi)) return false;

            nl.NoiCapDau = nl.NoiCapDau?.Trim() ?? "";
            bool laMoi = nl.MaNhatKyDau <= 0;
            try
            {
                using var gd = PhienGiaoDich.Mo();
                var dl = _repo.LayDuLieu(gd);

                CapNhienLieuDTO? goc = null;
                if (!laMoi)
                {
                    goc = dl.CapDau.FirstOrDefault(c => c.MaNhatKyDau == nl.MaNhatKyDau);
                    if (goc == null)
                    {
                        loi = "Phiếu cấp dầu không còn trong CSDL. Hãy nạp lại.";
                        return false;
                    }
                    if (!CungPhienBan(nl.PhienBan, goc.PhienBan))
                    {
                        loi = $"Phiếu NL-{goc.MaNhatKyDau:0000} vừa được người khác sửa sau khi bạn mở. Hãy nạp lại rồi sửa lại.";
                        return false;
                    }
                    if (goc.MaDauMay != nl.MaDauMay)
                    {
                        loi = "Không đổi đầu máy của phiếu đã lập (đầu máy và thời điểm bơm là thông tin gốc).";
                        return false;
                    }
                }

                var dm = dl.TimDauMay(nl.MaDauMay);
                if (!HopLe(QuyTacBaoTri.KiemTraCapDau(nl, dm), out loi)) return false;
                if (laMoi && dm!.TrangThai == PhuongTienService.NgungKhaiThac)
                {
                    loi = $"Đầu máy {dm.SoHieuDauMay} đã ngừng khai thác, không cấp nhiên liệu.";
                    return false;
                }
                if ((laMoi || nl.MaNguoiCap != goc!.MaNguoiCap) && KiemTraNguoiKy(dl, nl.MaNguoiCap, "cấp phát nhiên liệu") is { } loiNguoi)
                {
                    loi = loiNguoi;
                    return false;
                }

                QuyTacBaoTri.TinhLaiSuatTieuHao(nl, dm!.DinhMucSFC);

                if (laMoi)
                {
                    var (ma, luc, chiPhi) = _repo.ThemCapDau(nl, gd);
                    nl.MaNhatKyDau = ma;
                    nl.ThoiDiemBomDau = luc;
                    nl.ChiPhi = chiPhi;
                }
                else
                {
                    if (GiongNhau(goc!, nl)) return true;   // khong doi gi
                    if (_repo.SuaCapDau(nl, goc!.PhienBan!, gd) == 0)
                    {
                        loi = $"Phiếu NL-{nl.MaNhatKyDau:0000} vừa được người khác sửa. Hãy nạp lại rồi sửa lại.";
                        return false;
                    }
                    nl.ThoiDiemBomDau = goc.ThoiDiemBomDau;
                }

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, laMoi ? "THEM" : "SUA", "baotri.NhatKyCapNhienLieu",
                                    nl.MaNhatKyDau.ToString(), goc == null ? null : MoTaJson(goc, dm), MoTaJson(nl, dm));
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // =====================================================================
        // BAO DUONG DINH KY
        // =====================================================================

        // Them (MaBaoDuong <= 0) hoac sua (theo PhienBan, chi phieu moi nhat cua phuong tien).
        // duaVeSanSang: phuong tien dang BAO_DUONG thi chuyen ve SAN_SANG trong cung giao dich (chi khi them).
        public bool LuuBaoDuong(BaoDuongDTO bd, bool duaVeSanSang, out string loi, out KetQuaGhiBaoDuong? ketQua)
        {
            ketQua = null;
            if (!CoQuyen(MaQuyen.GhiNhatKyBaoTri, "ghi phiếu bảo dưỡng", out loi)) return false;

            bd.GhiChuKyThuat = bd.GhiChuKyThuat?.Trim() ?? "";
            bool laMoi = bd.MaBaoDuong <= 0;
            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(LapTauService.TaiNguyenKhoa);
                var dl = _repo.LayDuLieu(gd);

                BaoDuongDTO? goc = null;
                if (!laMoi)
                {
                    goc = dl.BaoDuong.FirstOrDefault(b => b.MaBaoDuong == bd.MaBaoDuong);
                    if (goc == null)
                    {
                        loi = "Phiếu bảo dưỡng không còn trong CSDL. Hãy nạp lại.";
                        return false;
                    }
                    if (!CungPhienBan(bd.PhienBan, goc.PhienBan))
                    {
                        loi = $"Phiếu BD-{goc.MaBaoDuong:0000} vừa được người khác sửa sau khi bạn mở. Hãy nạp lại rồi sửa lại.";
                        return false;
                    }
                    if (goc.KhoaPhuongTien != bd.KhoaPhuongTien)
                    {
                        loi = "Không đổi phương tiện của phiếu đã ghi (phương tiện và thời điểm hoàn thành là thông tin gốc).";
                        return false;
                    }
                    bd.ThoiDiemHoanThanh = goc.ThoiDiemHoanThanh;
                }

                // Phuong tien + trang thai
                string tenPt;
                string trangThai;
                decimal? kmTichLuy = null;
                if (bd.LaDauMay)
                {
                    var dm = dl.TimDauMay(bd.MaDauMay ?? 0);
                    if (dm == null) { loi = "Đầu máy không còn trong CSDL. Hãy nạp lại."; return false; }
                    tenPt = $"đầu máy {dm.SoHieuDauMay}";
                    trangThai = dm.TrangThai;
                    kmTichLuy = dm.SoKmTichLuy;
                    if (laMoi && dm.TrangThai == "DANG_CHAY")
                    {
                        var chay = dl.DoanTauCuaDauMay(dm.MaDauMay, chiChuaXuatPhat: false).FirstOrDefault(d => d.DaXuatPhat);
                        loi = $"{dm.SoHieuDauMay} đang kéo {(chay != null ? $"chuyến {chay.SoHieuMacTau}" : "tàu")}, " +
                              "chưa ghi nhận bảo dưỡng được. Ghi sau khi đầu máy về depot.";
                        return false;
                    }
                }
                else
                {
                    var tx = dl.TimToaXe(bd.MaToaXe ?? 0);
                    if (tx == null) { loi = "Toa xe không còn trong CSDL. Hãy nạp lại."; return false; }
                    tenPt = $"toa {tx.SoHieuToaXe}";
                    trangThai = tx.TrangThai;
                    if (laMoi && tx.DangChay)
                    {
                        loi = $"Toa {tx.SoHieuToaXe} đang chạy trong đoàn tàu, chưa ghi nhận bảo dưỡng được.";
                        return false;
                    }
                }
                if (laMoi && trangThai == PhuongTienService.NgungKhaiThac)
                {
                    loi = $"{char.ToUpper(tenPt[0])}{tenPt[1..]} đã ngừng khai thác, không ghi bảo dưỡng.";
                    return false;
                }

                // Lich su cua phuong tien (tru phieu dang sua). Sua: phai la phieu moi nhat.
                var lichSuKhac = dl.LichSuBaoDuong(bd.LoaiPhuongTien, bd.MaPhuongTien)
                                   .Where(b => b.MaBaoDuong != bd.MaBaoDuong).ToList();
                if (!laMoi)
                {
                    var moiHon = lichSuKhac.FirstOrDefault(b => b.ThoiDiemHoanThanh > goc!.ThoiDiemHoanThanh
                        || (b.ThoiDiemHoanThanh == goc.ThoiDiemHoanThanh && b.MaBaoDuong > goc.MaBaoDuong));
                    if (moiHon != null)
                    {
                        loi = $"Chỉ sửa được phiếu bảo dưỡng mới nhất của {tenPt}: đã có BD-{moiHon.MaBaoDuong:0000} " +
                              $"({moiHon.CapBaoDuong}, {moiHon.ThoiDiemHoanThanh:dd/MM/yyyy}) ghi sau phiếu này.";
                        return false;
                    }
                }

                if (!HopLe(QuyTacBaoTri.KiemTraBaoDuong(bd, kmTichLuy, lichSuKhac), out loi)) return false;
                if ((laMoi || bd.MaNguoiThucHien != goc!.MaNguoiThucHien)
                    && KiemTraNguoiKy(dl, bd.MaNguoiThucHien, "bảo dưỡng") is { } loiNguoi)
                {
                    loi = loiNguoi;
                    return false;
                }

                string? ttMoi = null;
                if (laMoi)
                {
                    var (ma, luc) = _repo.ThemBaoDuong(bd, gd);
                    bd.MaBaoDuong = ma;
                    bd.ThoiDiemHoanThanh = luc;
                }
                else
                {
                    if (GiongNhau(goc!, bd))
                    {
                        ketQua = new KetQuaGhiBaoDuong(bd.MaBaoDuong, bd.ThoiDiemHoanThanh, null, null);
                        return true;
                    }
                    if (_repo.SuaBaoDuong(bd, goc!.PhienBan!, gd) == 0)
                    {
                        loi = $"Phiếu BD-{bd.MaBaoDuong:0000} vừa được người khác sửa. Hãy nạp lại rồi sửa lại.";
                        return false;
                    }
                }

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, laMoi ? "THEM" : "SUA", "baotri.NhatKyBaoDuong",
                                    bd.MaBaoDuong.ToString(), goc == null ? null : MoTaJson(goc, tenPt), MoTaJson(bd, tenPt));

                // Lam xong bao duong -> dua phuong tien dang "Bao duong" ve "San sang" (nguoi dung chon)
                if (laMoi && duaVeSanSang && trangThai == "BAO_DUONG")
                {
                    int soDong = bd.LaDauMay
                        ? _repo.DoiTrangThaiDauMay(bd.MaPhuongTien, "BAO_DUONG", "SAN_SANG", gd)
                        : _repo.DoiTrangThaiToaXe(bd.MaPhuongTien, "BAO_DUONG", "SAN_SANG", gd);
                    if (soDong > 0)
                    {
                        ttMoi = "SAN_SANG";
                        gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "SUA",
                                            bd.LaDauMay ? "phuongtien.DauMay" : "phuongtien.ToaXe", bd.MaPhuongTien.ToString(),
                                            Json(new { PhuongTien = tenPt, TrangThai = "BAO_DUONG" }),
                                            Json(new { PhuongTien = tenPt, TrangThai = "SAN_SANG", LyDo = $"Hoàn thành BD-{bd.MaBaoDuong:0000}" }));
                    }
                }

                gd.XacNhan();
                ketQua = new KetQuaGhiBaoDuong(bd.MaBaoDuong, bd.ThoiDiemHoanThanh, ttMoi != null ? trangThai : null, ttMoi);
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // =====================================================================
        // TIEN ICH
        // =====================================================================

        // Nguoi ky phieu phai la tai khoan vai tro KY_THUAT dang hoat dong (quyet dinh 4)
        private static string? KiemTraNguoiKy(DuLieuBaoTri dl, int? maTaiKhoan, string viec)
        {
            var tk = dl.TimNguoiKyThuat(maTaiKhoan);
            if (tk == null || !tk.LaKyThuat)
                return $"Người {viec} phải là tài khoản có vai trò kỹ thuật (KY_THUAT). Hãy chọn lại.";
            if (!tk.DangHoatDong)
                return $"Tài khoản {tk.TenDangNhap} ({tk.HoTenHienThi}) đang bị khóa, không ký phiếu được.";
            return null;
        }

        private static bool CoQuyen(string maQuyen, string hanhDong, out string loi)
        {
            loi = PhanQuyenService.CoQuyen(maQuyen) ? ""
                : $"Tài khoản \"{PhanQuyenService.TenDangNhapHienTai}\" không có quyền {hanhDong} (cần quyền {maQuyen}).";
            return loi.Length == 0;
        }

        private static bool HopLe(List<LoiNhapLieuDTO> dsLoi, out string loi)
        {
            loi = dsLoi.Count == 0 ? "" : dsLoi.Count == 1 ? dsLoi[0].ThongDiep
                : string.Join("\n", dsLoi.Select(l => "• " + l.ThongDiep));
            return dsLoi.Count == 0;
        }

        private static bool CungPhienBan(byte[]? a, byte[]? b)
            => a != null && b != null && a.AsSpan().SequenceEqual(b);

        private static bool GiongNhau(CapNhienLieuDTO a, CapNhienLieuDTO b)
            => a.NoiCapDau == b.NoiCapDau && a.SoLitTraNap == b.SoLitTraNap && a.TrongLuongKeoTan == b.TrongLuongKeoTan
               && a.CuLyChayKm == b.CuLyChayKm && a.MaNguoiCap == b.MaNguoiCap && a.DonGiaVndLit == b.DonGiaVndLit
               && a.SuatTieuHaoSFC == b.SuatTieuHaoSFC && a.CanhBaoVuotMuc == b.CanhBaoVuotMuc;

        private static bool GiongNhau(BaoDuongDTO a, BaoDuongDTO b)
            => a.CapBaoDuong == b.CapBaoDuong && a.SoKmTaiThoiDiem == b.SoKmTaiThoiDiem
               && (a.GhiChuKyThuat ?? "") == (b.GhiChuKyThuat ?? "") && a.MaNguoiThucHien == b.MaNguoiThucHien
               && a.ChiPhi == b.ChiPhi;

        private static string MoTaJson(CapNhienLieuDTO nl, DauMayBaoTriDTO? dm) => Json(new
        {
            nl.MaNhatKyDau, DauMay = dm?.SoHieuDauMay ?? $"#{nl.MaDauMay}", nl.NoiCapDau, nl.SoLitTraNap, nl.TrongLuongKeoTan,
            nl.CuLyChayKm, nl.SuatTieuHaoSFC, nl.CanhBaoVuotMuc, DinhMuc = dm?.DinhMucSFC, nl.DonGiaVndLit, nl.ChiPhi,
            nl.MaNguoiCap, nl.ThoiDiemBomDau
        });

        private static string MoTaJson(BaoDuongDTO bd, string phuongTien) => Json(new
        {
            bd.MaBaoDuong, bd.LoaiPhuongTien, PhuongTien = phuongTien, bd.CapBaoDuong, bd.SoKmTaiThoiDiem,
            bd.GhiChuKyThuat, bd.MaNguoiThucHien, bd.ChiPhi, bd.ThoiDiemHoanThanh
        });

        private static string Json(object doiTuong) => JsonSerializer.Serialize(doiTuong);
    }
}
