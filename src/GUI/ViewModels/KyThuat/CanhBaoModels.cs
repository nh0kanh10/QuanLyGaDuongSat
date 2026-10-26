using BUS.Services;
using DTO.BaoTri;
using DTO.NhanSu;
using GUI.Helpers;
using System.Windows;
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.ViewModels.KyThuat
{
    // =========================================================================
    // BAO TRI - TAB 4: CANH BAO AN TOAN
    //
    // Khong co bang rieng trong CSDL: tong hop tu anh chup cua 3 tab truoc (BaoTriDuLieu) + kip lai
    // (DuLieuNhanSu). Cot "San sang xuat ben" dung DUNG ham KiemTraXuatBenService.DanhGia ma man hinh
    // Lich trinh (A Ba) goi khi doi trang thai chuyen -> hai noi khong lech nhau (24/10).
    // Moi quy tac o day chi doc, khong ghi gi.
    // =========================================================================

    // Mot canh bao. MucDo: NGHIEM_TRONG | CANH_BAO | LUU_Y
    // Nhom: KHAM_XE | LAP_TAU | NHAN_SU | NHIEN_LIEU | BAO_DUONG | TONG_HOP
    // HanhDong: KHAM_LAI | LAP_BIEN_BAN | GHI_NHAN_BD | XEM_NHIEN_LIEU | XEM_TOA | DUYET | MO_PHUONG_TIEN | MO_NHAN_SU | ""
    public record CanhBaoAnToan(
        string MucDo, string Nhom, string DoiTuong, string TieuDe, string MoTa, string Nguon,
        string HanhDong, string NhanHanhDong, string? KhoaXuLy, IReadOnlyList<string> DoanTauLienQuan)
    {
        public int ThuTuMucDo => MucDo switch { "NGHIEM_TRONG" => 3, "CANH_BAO" => 2, _ => 1 };
        public int ThuTuNhom => Nhom switch
        {
            "TONG_HOP" => 0, "KHAM_XE" => 1, "LAP_TAU" => 2, "NHAN_SU" => 3, "BAO_DUONG" => 4, _ => 5
        };

        public string NhanMucDo => MucDo switch { "NGHIEM_TRONG" => "NGHIÊM TRỌNG", "CANH_BAO" => "CẢNH BÁO", _ => "LƯU Ý" };
        public string NhanNhom => Nhom switch
        {
            "KHAM_XE" => "Khám xe",
            "LAP_TAU" => "Lập tàu",
            "NHAN_SU" => "Kíp lái",
            "NHIEN_LIEU" => "Nhiên liệu",
            "BAO_DUONG" => "Bảo dưỡng",
            _ => "Tổng hợp"
        };

        public string MauChu => MucDo switch { "NGHIEM_TRONG" => "#B91C1C", "CANH_BAO" => "#B45309", _ => "#1D4ED8" };
        public string MauNen => MucDo switch { "NGHIEM_TRONG" => "#FEF2F2", "CANH_BAO" => "#FFFBEB", _ => "#EFF6FF" };
        public string MauVien => MucDo switch { "NGHIEM_TRONG" => "#FECACA", "CANH_BAO" => "#FDE68A", _ => "#BFDBFE" };

        public SymbolRegular BieuTuong => MucDo switch
        {
            "NGHIEM_TRONG" => SymbolRegular.ErrorCircle24,
            "CANH_BAO" => SymbolRegular.Warning24,
            _ => SymbolRegular.Info24
        };

        public SymbolRegular BieuTuongNhom => Nhom switch
        {
            "KHAM_XE" => SymbolRegular.ClipboardTaskListLtr24,
            "LAP_TAU" => SymbolRegular.VehicleSubway24,
            "NHAN_SU" => SymbolRegular.PeopleTeam24,
            "NHIEN_LIEU" => SymbolRegular.GasPump24,
            "BAO_DUONG" => SymbolRegular.WrenchScrewdriver24,
            _ => SymbolRegular.ShieldError24
        };

        public bool CoHanhDong => HanhDong.Length > 0;
        public string NhanLienQuan => DoanTauLienQuan.Count > 0 ? "Đoàn tàu liên quan: " + string.Join(", ", DoanTauLienQuan) : "";
    }

    // Mot dong trong bang kiem tra san sang xuat ben. MucDo: DAT | LUU_Y | CHAN
    public record MucKiemTraXuatBen(string Ten, string KetQua, string MucDo)
    {
        public string MauChu => MucDo switch { "CHAN" => "#B91C1C", "LUU_Y" => "#B45309", _ => "#15803D" };
        public SymbolRegular BieuTuong => MucDo switch
        {
            "CHAN" => SymbolRegular.DismissCircle24,
            "LUU_Y" => SymbolRegular.Warning24,
            _ => SymbolRegular.CheckmarkCircle24
        };
    }

    public class SanSangXuatBenHienThi
    {
        public DoanTauKhamXe DoanTau { get; init; } = null!;
        public List<MucKiemTraXuatBen> KiemTra { get; init; } = new();
        public KetQuaDuyetAnToan DieuKienDuyet { get; init; } = new();
        public string ThoiGian { get; init; } = "";
        public bool DangChon { get; set; }

        public bool DaXuatPhat => DoanTau.DaXuatPhat;

        // DA_XUAT_PHAT | CHAN | LUU_Y | DAT
        public string KetLuan => DaXuatPhat ? "DA_XUAT_PHAT"
                               : KiemTra.Any(k => k.MucDo == "CHAN") ? "CHAN"
                               : KiemTra.Any(k => k.MucDo == "LUU_Y") ? "LUU_Y"
                               : "DAT";

        public string NhanKetLuan => KetLuan switch
        {
            "DA_XUAT_PHAT" => DoanTau.NhanTrangThaiChuyen.ToUpper(),
            "CHAN" => $"CHẶN XUẤT BẾN · {KiemTra.Count(k => k.MucDo == "CHAN")} lỗi",
            "LUU_Y" => "ĐƯỢC XUẤT BẾN · CÓ LƯU Ý",
            _ => "ĐỦ ĐIỀU KIỆN XUẤT BẾN"
        };

        // Nut duyet an toan: hien khi tau chua chay ma chua duyet; bat khi phan ky thuat da sach
        public Visibility HienNutDuyet => !DaXuatPhat && !DoanTau.DaDuyetAnToan ? Visibility.Visible : Visibility.Collapsed;
        public bool CoTheDuyet => DieuKienDuyet.CoTheDuyet;
        public string NhanNutDuyet => CoTheDuyet ? $"Duyệt an toàn {DoanTau.SoHieuMacTau}" : "Chưa duyệt được";
        public string GoiYNutDuyet => CoTheDuyet
            ? "Khám xe đạt sau lần sửa đoàn tàu cuối, đầu máy trong chu kỳ bảo dưỡng — bấm để duyệt (ghi vào CSDL)."
            : "Còn thiếu trước khi duyệt:\n" + DieuKienDuyet.MoTaChan;

        public string MauChu => KetLuan switch { "CHAN" => "#B91C1C", "LUU_Y" => "#B45309", "DAT" => "#15803D", _ => "#475569" };
        public string MauNen => KetLuan switch { "CHAN" => "#FEF2F2", "LUU_Y" => "#FFFBEB", "DAT" => "#F0FDF4", _ => "#F8FAFC" };
        public string MauVien => DangChon ? "#003B73"
                               : KetLuan switch { "CHAN" => "#FECACA", "LUU_Y" => "#FDE68A", "DAT" => "#BBF7D0", _ => "#E2E8F0" };
        public double DoDayVien => DangChon ? 2 : 1;

        public SymbolRegular BieuTuong => KetLuan switch
        {
            "CHAN" => SymbolRegular.ShieldError24,
            "LUU_Y" => SymbolRegular.Warning24,
            "DAT" => SymbolRegular.ShieldCheckmark24,
            _ => SymbolRegular.VehicleSubway24
        };
    }

    // =========================================================================
    // QUY TAC TONG HOP
    // =========================================================================
    public static class TongHopCanhBao
    {
        // Doan tau chua kham ma con duoi so gio nay la toi xuat phat => nang len NGHIEM_TRONG
        public const double SoGioCanhBaoGap = 3;
        private const int SoToaLietKeToiDa = 8;

        public static string ConLai(DateTime moc, DateTime bayGio)
        {
            var t = moc - bayGio;
            if (t.TotalMinutes <= 0) return $"đã quá giờ xuất phát {(int)(-t.TotalMinutes):N0} phút";
            if (t.TotalHours < 24) return $"còn {(int)t.TotalHours} giờ {t.Minutes:00} phút";
            return $"còn {(int)t.TotalDays} ngày {t.Hours} giờ";
        }

        // Tinh trang tong hop cua mot dau may: bao duong (cap can chu y) + 2 lan cap dau gan nhat
        private sealed record TinhTrangDauMay(DauMayCapDau DauMay, PhuongTienBaoDuong? PhuongTien, TienDoCapBaoDuong? BaoDuong,
                                              CapNhienLieuHienThi? NlMoiNhat, CapNhienLieuHienThi? NlTruoc);

        private static List<TinhTrangDauMay> TinhDauMay(BaoTriDuLieu dl)
        {
            var kq = new List<TinhTrangDauMay>();
            foreach (var dm in dl.DauMayKhaiThac)
            {
                var pt = dl.TimPhuongTien($"DAU_MAY:{dm.MaDauMay}");
                var bd = pt != null
                    ? ChuKyBaoDuong.CapCanChuY(ChuKyBaoDuong.TinhTienDo(pt, dl.BaoDuong.Where(x => x.KhoaPhuongTien == pt.Khoa)))
                    : null;
                var nl = dl.CapDau.Where(x => x.MaDauMay == dm.MaDauMay)
                                  .OrderByDescending(x => x.ThoiDiemBomDau).ThenByDescending(x => x.MaNhatKyDau).ToList();
                kq.Add(new TinhTrangDauMay(dm, pt, bd, nl.ElementAtOrDefault(0), nl.ElementAtOrDefault(1)));
            }
            return kq;
        }

        // Doan tau (chua ket thuc) co dau may nay lam chinh hoac day
        private static List<DoanTauKhamXe> DoanTauCuaDauMay(BaoTriDuLieu dl, int maDauMay, bool chiSapChay)
            => dl.DoanTauDangDieuHanh
                 .Where(d => (d.MaDauMayChinh == maDauMay || d.MaDauMayDay == maDauMay) && (!chiSapChay || !d.DaXuatPhat))
                 .ToList();

        // ---------------------------------------------------------------------
        // Bang san sang xuat ben: moi doan tau mot the, chua chay xep truoc
        // ---------------------------------------------------------------------
        public static List<SanSangXuatBenHienThi> TaoSanSang(BaoTriDuLieu dl, DuLieuNhanSu? ns, DateTime bayGio)
        {
            var ds = dl.DoanTauDangDieuHanh.Select(dt => new SanSangXuatBenHienThi
            {
                DoanTau = dt,
                KiemTra = KiemTraXuatBenService.DanhGia(dl.Goc, ns, dt.MaChuyenTau).CacMuc
                                               .Select(m => new MucKiemTraXuatBen(m.Ten, m.KetQua, m.MucDo)).ToList(),
                DieuKienDuyet = KiemTraXuatBenService.DieuKienDuyet(dl.Goc, dt.Goc),
                ThoiGian = dt.DaXuatPhat
                    ? $"Xuất phát {dt.GioXuatPhatKH:HH:mm dd/MM} · {dt.NhanTrangThaiChuyen.ToLower()}"
                    : $"Xuất phát {dt.GioXuatPhatKH:HH:mm dd/MM} · {ConLai(dt.GioXuatPhatKH, bayGio)}"
            });
            return ds.OrderBy(s => s.DaXuatPhat).ThenBy(s => s.DoanTau.GioXuatPhatKH).ToList();
        }

        // ---------------------------------------------------------------------
        // Danh sach canh bao
        // ---------------------------------------------------------------------
        public static List<CanhBaoAnToan> TaoCanhBao(BaoTriDuLieu dl, DuLieuNhanSu? ns, DateTime bayGio)
        {
            var ds = new List<CanhBaoAnToan>();
            var khong = Array.Empty<string>();

            // ===== Doan tau: kham xe + duyet an toan + kip lai =====
            foreach (var dt in dl.DoanTauDangDieuHanh)
            {
                bool sapChay = !dt.DaXuatPhat;
                bool gap = sapChay && (dt.GioXuatPhatKH - bayGio).TotalHours < SoGioCanhBaoGap;
                string gio = sapChay
                    ? $"xuất phát {dt.GioXuatPhatKH:HH:mm dd/MM} ({ConLai(dt.GioXuatPhatKH, bayGio)})"
                    : $"{dt.NhanTrangThaiChuyen.ToLower()} từ {dt.GioXuatPhatKH:HH:mm dd/MM}";
                var mac = new[] { dt.SoHieuMacTau };

                var bb = dl.KhamXe.Where(x => x.MaDoanTau == dt.MaDoanTau)
                                  .OrderByDescending(x => x.ThoiDiemKham).ThenByDescending(x => x.MaKhamXe).FirstOrDefault();
                if (bb != null && !bb.DuDieuKienXuatBen)
                {
                    ds.Add(new CanhBaoAnToan("NGHIEM_TRONG", "KHAM_XE", dt.SoHieuMacTau,
                        sapChay ? $"{dt.SoHieuMacTau} không đủ điều kiện xuất bến"
                                : $"{dt.SoHieuMacTau} đang chạy với biên bản khám không đạt",
                        $"{bb.TomTatLoi}. Biên bản {bb.MaBienBan} lúc {bb.ThoiDiemKham:HH:mm dd/MM} · {gio}.",
                        "baotri.KhamXeKyThuat", "KHAM_LAI", "Khám lại", dt.MaDoanTau.ToString(), mac));
                }
                else if (bb == null && sapChay)
                {
                    ds.Add(new CanhBaoAnToan(gap ? "NGHIEM_TRONG" : "CANH_BAO", "KHAM_XE", dt.SoHieuMacTau,
                        $"{dt.SoHieuMacTau} chưa có biên bản khám xe",
                        $"Đoàn tàu {dt.TongSoToa} toa, đầu máy {dt.NhanDauMay} · {gio}.",
                        "baotri.KhamXeKyThuat", "LAP_BIEN_BAN", "Lập biên bản", dt.MaDoanTau.ToString(), mac));
                }
                else if (bb != null && sapChay && dt.Goc.ThoiDiemSuaDoanTau > bb.ThoiDiemKham)
                {
                    ds.Add(new CanhBaoAnToan(gap ? "NGHIEM_TRONG" : "CANH_BAO", "KHAM_XE", dt.SoHieuMacTau,
                        $"{dt.SoHieuMacTau} đổi thành phần sau lần khám gần nhất",
                        $"Đoàn tàu sửa lúc {dt.Goc.ThoiDiemSuaDoanTau:HH:mm dd/MM}, biên bản {bb.MaBienBan} lập lúc " +
                        $"{bb.ThoiDiemKham:HH:mm dd/MM} không còn hiệu lực · {gio}.",
                        "baotri.KhamXeKyThuat", "KHAM_LAI", "Khám lại", dt.MaDoanTau.ToString(), mac));
                }

                if (!sapChay) continue;

                if (!dt.DaDuyetAnToan)
                {
                    var dk = KiemTraXuatBenService.DieuKienDuyet(dl.Goc, dt.Goc);
                    ds.Add(new CanhBaoAnToan("CANH_BAO", "LAP_TAU", dt.SoHieuMacTau,
                        $"{dt.SoHieuMacTau} chưa được duyệt an toàn",
                        dk.CoTheDuyet
                            ? $"Khám xe và bảo dưỡng đầu máy đã đạt — có thể duyệt · {gio}."
                            : $"Còn chặn: {string.Join("; ", dk.CacMuc.Where(m => m.MucDo == "CHAN").Select(m => m.KetQua))} · {gio}.",
                        "vanhanh.DoanTau.DaDuyetAnToan", dk.CoTheDuyet ? "DUYET" : "", dk.CoTheDuyet ? "Duyệt an toàn" : "",
                        dt.MaDoanTau.ToString(), mac));
                }

                // Kip lai (Nhan su) - cung luat chan xuat ben
                var chanKip = KiemTraXuatBenService.MucKipLai(ns, dt.MaChuyenTau).Where(m => m.MucDo == "CHAN").ToList();
                if (chanKip.Count > 0)
                {
                    ds.Add(new CanhBaoAnToan(gap ? "NGHIEM_TRONG" : "CANH_BAO", "NHAN_SU", dt.SoHieuMacTau,
                        $"{dt.SoHieuMacTau} chưa đủ kíp lái để xuất bến",
                        $"{string.Join("; ", chanKip.Select(m => m.KetQua))} · {gio}.",
                        "nhansu.PhanCongKipLai", "MO_NHAN_SU", "Mở Nhân sự", null, mac));
                }
            }

            // ===== Dau may: bao duong + nhien lieu + doi chieu phan cong =====
            foreach (var tt in TinhDauMay(dl))
            {
                string soHieu = tt.DauMay.SoHieuDauMay;
                var tauSapChay = DoanTauCuaDauMay(dl, tt.DauMay.MaDauMay, chiSapChay: true);
                var tauLienQuan = DoanTauCuaDauMay(dl, tt.DauMay.MaDauMay, chiSapChay: false).Select(d => d.SoHieuMacTau).ToList();
                string khoa = tt.PhuongTien?.Khoa ?? "";

                var bd = tt.BaoDuong;
                if (bd != null && bd.TrangThai is "QUA_HAN" or "SAP_TOI")
                {
                    bool quaHan = bd.TrangThai == "QUA_HAN";
                    ds.Add(new CanhBaoAnToan(quaHan ? "NGHIEM_TRONG" : "CANH_BAO", "BAO_DUONG", soHieu,
                        quaHan ? $"{soHieu} quá hạn bảo dưỡng {bd.Cap}" : $"{soHieu} sắp tới hạn bảo dưỡng {bd.Cap}",
                        $"{char.ToUpper(bd.ConLai[0])}{bd.ConLai[1..]} · km tích lũy {tt.DauMay.SoKmTichLuy:N0} · {bd.MoTaMoc}.",
                        "baotri.NhatKyBaoDuong", "GHI_NHAN_BD", $"Ghi nhận {bd.Cap}", khoa, tauLienQuan));

                    // Doi chieu phan cong: dau may qua han van duoc gan cho doan tau sap chay
                    if (quaHan && tauSapChay.Count > 0)
                    {
                        var tau = tauSapChay[0];
                        string vaiTro = tau.MaDauMayChinh == tt.DauMay.MaDauMay ? "chính" : "đẩy";
                        ds.Add(new CanhBaoAnToan("NGHIEM_TRONG", "TONG_HOP", soHieu,
                            $"Đầu máy quá hạn bảo dưỡng được phân công kéo {string.Join(", ", tauSapChay.Select(d => d.SoHieuMacTau))}",
                            $"{soHieu} {bd.ConLai} (cấp {bd.Cap}) nhưng là đầu máy {vaiTro} của {tau.SoHieuMacTau} " +
                            $"xuất phát {tau.GioXuatPhatKH:HH:mm dd/MM} ({ConLai(tau.GioXuatPhatKH, bayGio)}). " +
                            "Đề nghị bảo dưỡng trước giờ chạy hoặc thay đầu máy.",
                            "NhatKyBaoDuong × vanhanh.DoanTau", "GHI_NHAN_BD", $"Ghi nhận {bd.Cap}", khoa,
                            tauSapChay.Select(d => d.SoHieuMacTau).ToList()));
                    }
                }

                var nl = tt.NlMoiNhat;
                if (nl != null && nl.MucDo is "VUOT" or "SAT")
                {
                    bool vuot = nl.MucDo == "VUOT";
                    string xuHuong = "";
                    if (tt.NlTruoc != null && tt.NlTruoc.SuatTieuHaoSFC > 0)
                    {
                        decimal d = Math.Round((nl.SuatTieuHaoSFC - tt.NlTruoc.SuatTieuHaoSFC) / tt.NlTruoc.SuatTieuHaoSFC * 100m, 1);
                        xuHuong = d > 0 ? $" · tăng {d:N1}% so với lần trước" : d < 0 ? $" · giảm {-d:N1}% so với lần trước" : "";
                    }
                    ds.Add(new CanhBaoAnToan(vuot ? "CANH_BAO" : "LUU_Y", "NHIEN_LIEU", soHieu,
                        vuot ? $"{soHieu} tiêu hao vượt định mức {nl.TyLeDinhMuc - 100m:N1}%"
                             : $"{soHieu} tiêu hao sát định mức ({nl.TyLeDinhMuc:N0}%)",
                        $"{nl.SuatTieuHaoSFC:N2} lít/10,000 tấn·km so với định mức {nl.DinhMucSfc:N0} (tham khảo){xuHuong}. " +
                        $"Phiếu {nl.MaPhieu} tại {nl.NoiCapDau} lúc {nl.ThoiDiemBomDau:HH:mm dd/MM}.",
                        "baotri.NhatKyCapNhienLieu", "XEM_NHIEN_LIEU", "Xem phiếu", nl.MaNhatKyDau.ToString(), tauLienQuan));
                }
            }

            // ===== Toa xe: bao duong (bo qua toa ngung khai thac) =====
            var toaChuaCo = new List<string>();
            foreach (var pt in dl.PhuongTien.Where(p => !p.LaDauMay && !p.NgungKhaiThac))
            {
                var cuaToa = dl.BaoDuong.Where(x => x.KhoaPhuongTien == pt.Khoa).ToList();
                if (cuaToa.Count == 0) { toaChuaCo.Add(pt.SoHieu); continue; }

                var bd = ChuKyBaoDuong.CapCanChuY(ChuKyBaoDuong.TinhTienDo(pt, cuaToa));
                if (bd != null && bd.TrangThai is "QUA_HAN" or "SAP_TOI")
                {
                    bool quaHan = bd.TrangThai == "QUA_HAN";
                    ds.Add(new CanhBaoAnToan(quaHan ? "NGHIEM_TRONG" : "CANH_BAO", "BAO_DUONG", pt.SoHieu,
                        quaHan ? $"Toa {pt.SoHieu} quá hạn bảo dưỡng {bd.Cap}" : $"Toa {pt.SoHieu} sắp tới hạn bảo dưỡng {bd.Cap}",
                        $"{char.ToUpper(bd.ConLai[0])}{bd.ConLai[1..]} · {pt.MoTa} · {bd.MoTaMoc}.",
                        "baotri.NhatKyBaoDuong", "GHI_NHAN_BD", $"Ghi nhận {bd.Cap}", pt.Khoa, khong));
                }
            }
            if (toaChuaCo.Count > 0)
            {
                string lietKe = string.Join(", ", toaChuaCo.Take(SoToaLietKeToiDa))
                                + (toaChuaCo.Count > SoToaLietKeToiDa ? $" và {toaChuaCo.Count - SoToaLietKeToiDa} toa khác" : "");
                ds.Add(new CanhBaoAnToan("LUU_Y", "BAO_DUONG", $"{toaChuaCo.Count} toa",
                    $"{toaChuaCo.Count} toa xe chưa có lịch sử bảo dưỡng",
                    $"{lietKe} chưa có phiếu nào nên chưa tính được mốc đến hạn. " +
                    "Nên ghi nhận lần bảo dưỡng gần nhất theo lý lịch toa.",
                    "baotri.NhatKyBaoDuong", "XEM_TOA", "Xem toa xe", null, khong));
            }

            return ds.OrderByDescending(c => c.ThuTuMucDo)
                     .ThenBy(c => c.ThuTuNhom)
                     .ThenBy(c => c.DoiTuong)
                     .ToList();
        }
    }
}
