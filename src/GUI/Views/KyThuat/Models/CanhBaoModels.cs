using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.KyThuat.Models
{
    // =========================================================================
    // BAO TRI - TAB 4: CANH BAO AN TOAN
    //
    // Khong co bang rieng trong CSDL: tong hop tu 3 nguon cua 3 tab truoc
    // (baotri.KhamXeKyThuat, baotri.NhatKyCapNhienLieu, baotri.NhatKyBaoDuong)
    // cong vanhanh.DoanTau (dau may duoc gan, DaDuyetAnToan) va ChuyenTau.
    // Moi quy tac o day chi doc du lieu hien thi, khong ghi gi.
    // =========================================================================

    // Mot canh bao. MucDo: NGHIEM_TRONG | CANH_BAO | LUU_Y
    // Nhom: KHAM_XE | LAP_TAU | NHIEN_LIEU | BAO_DUONG | TONG_HOP
    // HanhDong: KHAM_LAI | LAP_BIEN_BAN | GHI_NHAN_BD | XEM_NHIEN_LIEU | XEM_TOA | MO_PHUONG_TIEN | ""
    public record CanhBaoAnToan(
        string MucDo, string Nhom, string DoiTuong, string TieuDe, string MoTa, string Nguon,
        string HanhDong, string NhanHanhDong, string? KhoaXuLy, IReadOnlyList<string> DoanTauLienQuan)
    {
        public int ThuTuMucDo => MucDo switch { "NGHIEM_TRONG" => 3, "CANH_BAO" => 2, _ => 1 };
        public int ThuTuNhom => Nhom switch { "TONG_HOP" => 0, "KHAM_XE" => 1, "LAP_TAU" => 2, "BAO_DUONG" => 3, _ => 4 };

        public string NhanMucDo => MucDo switch { "NGHIEM_TRONG" => "NGHIÊM TRỌNG", "CANH_BAO" => "CẢNH BÁO", _ => "LƯU Ý" };
        public string NhanNhom => Nhom switch
        {
            "KHAM_XE" => "Khám xe",
            "LAP_TAU" => "Lập tàu",
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
        public string ThoiGian { get; init; } = "";
        public bool DangChon { get; set; }

        public bool DaXuatPhat => TongHopCanhBao.DaXuatPhat(DoanTau);

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

        public static bool DaXuatPhat(DoanTauKhamXe dt)
            => dt.TrangThaiChuyen is "DANG_CHAY" or "TRE_GIO" or "HOAN_THANH" or "DA_HUY";

        public static string ConLai(DateTime moc, DateTime bayGio)
        {
            var t = moc - bayGio;
            if (t.TotalMinutes <= 0) return $"đã quá giờ xuất phát {(int)(-t.TotalMinutes):N0} phút";
            if (t.TotalHours < 24) return $"còn {(int)t.TotalHours} giờ {t.Minutes:00} phút";
            return $"còn {(int)t.TotalDays} ngày {t.Hours} giờ";
        }

        // Tinh trang tong hop cua mot dau may tu tab 2 + tab 3
        private sealed record TinhTrangDauMay(
            DauMayCapDau DauMay, PhuongTienBaoDuong? PhuongTien, TienDoCapBaoDuong? BaoDuong,
            CapNhienLieuHienThi? NlMoiNhat, CapNhienLieuHienThi? NlTruoc);

        private static Dictionary<string, TinhTrangDauMay> TinhDauMay(
            List<CapNhienLieuHienThi> capDau, List<BaoDuongHienThi> baoDuong)
        {
            var kq = new Dictionary<string, TinhTrangDauMay>();
            foreach (var dm in DanhMucMauBaoTri.DauMay)
            {
                var pt = DanhMucMauBaoTri.TimPhuongTien($"DAU_MAY:{dm.MaDauMay}");
                var bd = pt != null
                    ? ChuKyBaoDuong.CapCanChuY(ChuKyBaoDuong.TinhTienDo(pt, baoDuong.Where(x => x.KhoaPhuongTien == pt.Khoa)))
                    : null;
                var nl = capDau.Where(x => x.MaDauMay == dm.MaDauMay).OrderByDescending(x => x.ThoiDiemBomDau).ToList();
                kq[dm.SoHieuDauMay] = new TinhTrangDauMay(dm, pt, bd, nl.ElementAtOrDefault(0), nl.ElementAtOrDefault(1));
            }
            return kq;
        }

        // Doan tau (chua ket thuc) co dau may nay lam chinh hoac day
        private static List<DoanTauKhamXe> DoanTauCuaDauMay(string soHieu, bool chiSapChay)
            => DanhMucMauBaoTri.DoanTau
                .Where(d => (d.SoHieuDauMayChinh == soHieu || d.SoHieuDauMayDay == soHieu) &&
                            d.TrangThaiChuyen is not ("HOAN_THANH" or "DA_HUY") &&
                            (!chiSapChay || !DaXuatPhat(d)))
                .OrderBy(d => d.GioXuatPhatKH)
                .ToList();

        // ---------------------------------------------------------------------
        // Bang san sang xuat ben: moi doan tau mot the, chua chay xep truoc
        // ---------------------------------------------------------------------
        public static List<SanSangXuatBenHienThi> TaoSanSang(
            List<KhamXeHienThi> khamXe, List<CapNhienLieuHienThi> capDau, List<BaoDuongHienThi> baoDuong, DateTime bayGio)
        {
            var dauMay = TinhDauMay(capDau, baoDuong);
            var ds = new List<SanSangXuatBenHienThi>();

            foreach (var dt in DanhMucMauBaoTri.DoanTau.Where(d => d.TrangThaiChuyen is not ("HOAN_THANH" or "DA_HUY")))
            {
                var kiemTra = new List<MucKiemTraXuatBen>();

                // 1. Kham xe: bien ban moi nhat
                var bb = khamXe.Where(x => x.MaDoanTau == dt.MaDoanTau).OrderByDescending(x => x.ThoiDiemKham).FirstOrDefault();
                kiemTra.Add(bb == null
                    ? new MucKiemTraXuatBen("Khám xe kỹ thuật", "Chưa có biên bản khám xe", "CHAN")
                    : bb.DuDieuKienXuatBen
                        ? new MucKiemTraXuatBen("Khám xe kỹ thuật", $"Đạt 4/4 · {bb.MaBienBan} lúc {bb.ThoiDiemKham:HH:mm dd/MM}", "DAT")
                        : new MucKiemTraXuatBen("Khám xe kỹ thuật", $"Không đạt · {bb.TomTatLoi}", "CHAN"));

                // 2. Duyet an toan lap tau (vanhanh.DoanTau.DaDuyetAnToan)
                kiemTra.Add(dt.DaDuyetAnToan
                    ? new MucKiemTraXuatBen("Duyệt an toàn lập tàu", "Đã duyệt", "DAT")
                    : new MucKiemTraXuatBen("Duyệt an toàn lập tàu", "Chưa duyệt", "CHAN"));

                // 3 - 4. Bao duong + nhien lieu cua tung dau may (chinh, day)
                foreach (var (soHieu, vaiTro) in new[] { (dt.SoHieuDauMayChinh, "chính"), (dt.SoHieuDauMayDay, "đẩy") })
                {
                    if (string.IsNullOrEmpty(soHieu) || !dauMay.TryGetValue(soHieu, out var tt)) continue;

                    var bd = tt.BaoDuong;
                    kiemTra.Add(bd == null
                        ? new MucKiemTraXuatBen($"Bảo dưỡng {soHieu} ({vaiTro})", "Chưa có dữ liệu", "LUU_Y")
                        : bd.TrangThai switch
                        {
                            "QUA_HAN" => new MucKiemTraXuatBen($"Bảo dưỡng {soHieu} ({vaiTro})", $"Quá hạn {bd.Cap} · {bd.ConLai}", "CHAN"),
                            "SAP_TOI" => new MucKiemTraXuatBen($"Bảo dưỡng {soHieu} ({vaiTro})", $"Sắp tới hạn {bd.Cap} · {bd.ConLai}", "LUU_Y"),
                            _ => new MucKiemTraXuatBen($"Bảo dưỡng {soHieu} ({vaiTro})", $"Trong chu kỳ · {bd.Cap} {bd.ConLai}", "DAT")
                        });

                    var nl = tt.NlMoiNhat;
                    kiemTra.Add(nl == null
                        ? new MucKiemTraXuatBen($"Nhiên liệu {soHieu}", "Chưa có số liệu cấp dầu", "LUU_Y")
                        : nl.MucDo == "VUOT"
                            ? new MucKiemTraXuatBen($"Nhiên liệu {soHieu}", $"Lần cấp gần nhất vượt định mức {nl.TyLeDinhMuc - 100m:N1}%", "LUU_Y")
                            : new MucKiemTraXuatBen($"Nhiên liệu {soHieu}", $"{nl.TyLeDinhMuc:N0}% định mức", "DAT"));
                }

                ds.Add(new SanSangXuatBenHienThi
                {
                    DoanTau = dt,
                    KiemTra = kiemTra,
                    ThoiGian = DaXuatPhat(dt)
                        ? $"Xuất phát {dt.GioXuatPhatKH:HH:mm dd/MM} · {dt.NhanTrangThaiChuyen.ToLower()}"
                        : $"Xuất phát {dt.GioXuatPhatKH:HH:mm dd/MM} · {ConLai(dt.GioXuatPhatKH, bayGio)}"
                });
            }

            return ds.OrderBy(s => s.DaXuatPhat).ThenBy(s => s.DoanTau.GioXuatPhatKH).ToList();
        }

        // ---------------------------------------------------------------------
        // Danh sach canh bao
        // ---------------------------------------------------------------------
        public static List<CanhBaoAnToan> TaoCanhBao(
            List<KhamXeHienThi> khamXe, List<CapNhienLieuHienThi> capDau, List<BaoDuongHienThi> baoDuong, DateTime bayGio)
        {
            var ds = new List<CanhBaoAnToan>();
            var dauMay = TinhDauMay(capDau, baoDuong);
            var khong = Array.Empty<string>();

            // ===== Doan tau: kham xe + duyet lap tau =====
            foreach (var dt in DanhMucMauBaoTri.DoanTau.Where(d => d.TrangThaiChuyen is not ("HOAN_THANH" or "DA_HUY")))
            {
                bool sapChay = !DaXuatPhat(dt);
                string gio = sapChay
                    ? $"xuất phát {dt.GioXuatPhatKH:HH:mm dd/MM} ({ConLai(dt.GioXuatPhatKH, bayGio)})"
                    : $"{dt.NhanTrangThaiChuyen.ToLower()} từ {dt.GioXuatPhatKH:HH:mm dd/MM}";
                var mac = new[] { dt.SoHieuMacTau };

                var bb = khamXe.Where(x => x.MaDoanTau == dt.MaDoanTau).OrderByDescending(x => x.ThoiDiemKham).FirstOrDefault();
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
                    bool gap = (dt.GioXuatPhatKH - bayGio).TotalHours < SoGioCanhBaoGap;
                    ds.Add(new CanhBaoAnToan(gap ? "NGHIEM_TRONG" : "CANH_BAO", "KHAM_XE", dt.SoHieuMacTau,
                        $"{dt.SoHieuMacTau} chưa có biên bản khám xe",
                        $"Đoàn tàu {dt.TongSoToa} toa, đầu máy {dt.NhanDauMay} · {gio}.",
                        "baotri.KhamXeKyThuat", "LAP_BIEN_BAN", "Lập biên bản", dt.MaDoanTau.ToString(), mac));
                }

                if (!dt.DaDuyetAnToan && sapChay)
                {
                    ds.Add(new CanhBaoAnToan("CANH_BAO", "LAP_TAU", dt.SoHieuMacTau,
                        $"{dt.SoHieuMacTau} chưa được duyệt an toàn lập tàu",
                        $"Đoàn tàu {dt.TongSoToa} toa · {dt.TongChieuDaiM:N1} m · {dt.TongTrongLuongTan:N0} tấn · {gio}.",
                        "vanhanh.DoanTau.DaDuyetAnToan", "MO_PHUONG_TIEN", "Mở Phương tiện", null, mac));
                }
            }

            // ===== Dau may: bao duong + nhien lieu + doi chieu phan cong =====
            foreach (var tt in dauMay.Values)
            {
                string soHieu = tt.DauMay.SoHieuDauMay;
                var tauSapChay = DoanTauCuaDauMay(soHieu, chiSapChay: true);
                var tauLienQuan = DoanTauCuaDauMay(soHieu, chiSapChay: false).Select(d => d.SoHieuMacTau).ToList();
                string khoa = tt.PhuongTien?.Khoa ?? "";

                var bd = tt.BaoDuong;
                if (bd != null && bd.TrangThai is "QUA_HAN" or "SAP_TOI")
                {
                    bool quaHan = bd.TrangThai == "QUA_HAN";
                    ds.Add(new CanhBaoAnToan(quaHan ? "NGHIEM_TRONG" : "CANH_BAO", "BAO_DUONG", soHieu,
                        quaHan ? $"{soHieu} quá hạn bảo dưỡng {bd.Cap}" : $"{soHieu} sắp tới hạn bảo dưỡng {bd.Cap}",
                        $"{char.ToUpper(bd.ConLai[0])}{bd.ConLai[1..]} · km tích lũy {tt.PhuongTien?.SoKmTichLuy:N0} · {bd.MoTaMoc}.",
                        "baotri.NhatKyBaoDuong", "GHI_NHAN_BD", $"Ghi nhận {bd.Cap}", khoa, tauLienQuan));

                    // Doi chieu phan cong: dau may qua han van duoc gan cho doan tau sap chay
                    if (quaHan && tauSapChay.Count > 0)
                    {
                        var tau = tauSapChay[0];
                        string vaiTro = tau.SoHieuDauMayChinh == soHieu ? "chính" : "đẩy";
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

            // ===== Toa xe: bao duong =====
            var toaChuaCo = new List<string>();
            foreach (var pt in DanhMucMauBaoTri.PhuongTien.Where(p => !p.LaDauMay))
            {
                var cuaToa = baoDuong.Where(x => x.KhoaPhuongTien == pt.Khoa).ToList();
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
                ds.Add(new CanhBaoAnToan("LUU_Y", "BAO_DUONG", string.Join(", ", toaChuaCo),
                    $"{toaChuaCo.Count} toa xe chưa có lịch sử bảo dưỡng",
                    $"{string.Join(", ", toaChuaCo)} chưa có phiếu nào nên chưa tính được mốc đến hạn. " +
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
