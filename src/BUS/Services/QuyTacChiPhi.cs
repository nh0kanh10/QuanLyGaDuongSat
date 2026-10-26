using DTO.BaoTri;

namespace BUS.Services
{
    // =========================================================================
    // CHI PHI & HIEU QUA THEO MAC TAU - tab 5 trang Bao tri (26/10). Ham thuan (unit test duoc).
    //
    // Ky N ngay = tu 00:00 cua (hom nay - N + 1) toi gio may chu luc doc (tinh ca hom nay).
    // Doanh thu : ve cua cac chuyen co NGAY CHAY (NgayXuatPhat) trong ky, chuyen khong bi huy;
    //             = thuc thu - tien da hoan (nhu bao cao ban ve cua A Ba), khong tinh ve DA_HUY.
    //             Cuoc hang hoa chua gan chuyen tau -> chi cong tong, khong chia theo mac.
    // Chi phi   : moi phieu co chi phi > 0, thoi diem trong ky.
    //             - Kham xe: tinh thang cho mac cua doan tau duoc kham.
    //             - Cap dau, bao duong dau may, du phong: CHIA cho cac mac theo so doan tau (chinh / day) ma
    //               dau may keo (giu cach chia tu 28/09); dau may khong keo doan tau nao -> "chua phan bo".
    //             - Bao duong toa xe: "chua phan bo" (doi toa dung chung, giu quy uoc 28/09).
    //             Chia tron dong, phan du cho phan cuoi => tong cac mac + chua phan bo = dung so thuc chi.
    //             Du phong: dau may (con khai thac) sap / qua han bao duong, uoc theo chi phi tham khao cua cap.
    // Khuyen nghi theo bien loi nhuan: < 10% (ke ca lo) thu hep · 10-30% duy tri · > 30% mo rong.
    // Chua du du lieu: tau hang (doanh thu la cuoc van don), khong co chuyen chay trong ky, chuyen khong co
    //             toa ban ve, hoac chua co khoan chi phi nao.
    // "Loi nhuan" o day = doanh thu - chi phi bao tri (kham xe, nhien lieu, bao duong), CHUA tru luong,
    // phi ha tang, khau hao.
    // =========================================================================
    public static class QuyTacChiPhi
    {
        public const decimal NguongCanNhac = 10m;      // % bien loi nhuan
        public const decimal NguongMoRong = 30m;
        public const int SoNgayKyMacDinh = 7;

        public static DateTime TuNgay(DateTime bayGio, int soNgay) => bayGio.Date.AddDays(-(Math.Max(1, soNgay) - 1));

        public static KetQuaChiPhiDTO TongHop(DuLieuBaoTri dl, int soNgay)
        {
            DateTime tu = TuNgay(dl.BayGio, soNgay), den = dl.BayGio;
            bool TrongKy(DateTime t) => t >= tu && t <= den;

            // Moi mac dang khai thac + mac ngung khai thac nhung con du lieu trong ky
            var theoMac = new Dictionary<string, HieuQuaMacTauDTO>();
            foreach (var mt in dl.MacTau)
                theoMac[mt.SoHieuMacTau] = new HieuQuaMacTauDTO { MacTau = mt };

            var chuaPhanBo = new List<KhoanChiPhiDTO>();
            decimal thucChi = 0m;

            // Ghi mot khoan theo danh sach phan (mac, so phan); khong co phan nao -> chua phan bo
            void GhiKhoan(string nguon, string maPhieu, string moTa, decimal soTien, DateTime thoiDiem,
                          List<(string Mac, int SoPhan)> phan, string lyDoChuaPhanBo)
            {
                thucChi += soTien;
                var phanHopLe = phan.Where(p => theoMac.ContainsKey(p.Mac)).ToList();
                if (phanHopLe.Count == 0)
                {
                    chuaPhanBo.Add(new KhoanChiPhiDTO(nguon, maPhieu, $"{moTa} · {lyDoChuaPhanBo}", soTien, soTien, 1, 1, thoiDiem));
                    return;
                }
                int tong = phanHopLe.Sum(p => p.SoPhan);
                foreach (var (mac, soPhan, tien) in ChiaTronDong(soTien, phanHopLe, tong))
                    theoMac[mac].DaChi.Add(new KhoanChiPhiDTO(nguon, maPhieu, moTa, tien, soTien, soPhan, tong, thoiDiem));
            }

            // 1) Kham xe -> mac cua doan tau duoc kham
            foreach (var kx in dl.KhamXe.Where(k => k.ChiPhi > 0 && TrongKy(k.ThoiDiemKham)))
            {
                var dt = dl.TimDoanTau(kx.MaDoanTau);
                var phan = dt == null ? new List<(string, int)>() : new List<(string, int)> { (dt.SoHieuMacTau, 1) };
                GhiKhoan("KHAM_XE", $"BB-{kx.MaKhamXe:0000}",
                    $"Khám xe {dt?.SoHieuMacTau ?? "?"} · {(QuyTacBaoTri.DuDieuKienXuatBen(kx) ? "đạt" : "không đạt")}",
                    kx.ChiPhi, kx.ThoiDiemKham, phan, "đoàn tàu không còn trong dữ liệu");
            }

            // 2) Cap dau -> chia theo doan tau dau may keo
            foreach (var nl in dl.CapDau.Where(c => c.ChiPhi > 0 && TrongKy(c.ThoiDiemBomDau)))
            {
                string soHieu = dl.TimDauMay(nl.MaDauMay)?.SoHieuDauMay ?? $"#{nl.MaDauMay}";
                GhiKhoan("NHIEN_LIEU", $"NL-{nl.MaNhatKyDau:0000}", $"Cấp dầu {soHieu} · {nl.SoLitTraNap:N0} lít",
                    nl.ChiPhi, nl.ThoiDiemBomDau, PhanTheoDauMay(dl, nl.MaDauMay), "đầu máy chưa kéo đoàn tàu nào");
            }

            // 3) Bao duong: dau may chia theo doan tau; toa xe chua phan bo
            foreach (var bd in dl.BaoDuong.Where(b => b.ChiPhi > 0 && TrongKy(b.ThoiDiemHoanThanh)))
            {
                if (bd.LaDauMay)
                {
                    string soHieu = dl.TimDauMay(bd.MaPhuongTien)?.SoHieuDauMay ?? $"#{bd.MaPhuongTien}";
                    GhiKhoan("BAO_DUONG", $"BD-{bd.MaBaoDuong:0000}", $"Bảo dưỡng {bd.CapBaoDuong} · {soHieu}",
                        bd.ChiPhi, bd.ThoiDiemHoanThanh, PhanTheoDauMay(dl, bd.MaPhuongTien), "đầu máy chưa kéo đoàn tàu nào");
                }
                else
                {
                    string soHieu = dl.TimToaXe(bd.MaPhuongTien)?.SoHieuToaXe ?? $"#{bd.MaPhuongTien}";
                    GhiKhoan("BAO_DUONG", $"BD-{bd.MaBaoDuong:0000}", $"Bảo dưỡng {bd.CapBaoDuong} · toa {soHieu}",
                        bd.ChiPhi, bd.ThoiDiemHoanThanh, new List<(string, int)>(), "toa xe dùng chung, chưa gắn mác");
                }
            }

            // 4) Du phong: dau may con khai thac dang sap / qua han -> uoc theo chi phi tham khao, chia nhu nhien lieu
            foreach (var dm in dl.DauMay.Where(d => d.TrangThai != "NGUNG_KHAI_THAC"))
            {
                var chuY = QuyTacBaoTri.CapCanChuY(QuyTacBaoTri.TienDoDauMay(dl, dm));
                if (chuY == null || chuY.TrangThai is not ("QUA_HAN" or "SAP_TOI")) continue;

                decimal uocTinh = QuyTacBaoTri.ChiPhiThamKhao(chuY.Cap);
                var phan = PhanTheoDauMay(dl, dm.MaDauMay).Where(p => theoMac.ContainsKey(p.Mac)).ToList();
                if (phan.Count == 0 || uocTinh <= 0) continue;

                int tong = phan.Sum(p => p.SoPhan);
                string tinhTrang = chuY.TrangThai == "QUA_HAN" ? $"quá hạn {chuY.Cap}" : $"sắp tới hạn {chuY.Cap}";
                foreach (var (mac, soPhan, tien) in ChiaTronDong(uocTinh, phan, tong))
                {
                    string chia = soPhan == tong ? "" : $" · phân bổ {soPhan}/{tong} của {uocTinh:N0} đ";
                    theoMac[mac].DuPhong.Add(new KhoanDuPhongDTO(dm.SoHieuDauMay, chuY.Cap, chuY.TrangThai,
                        $"{dm.SoHieuDauMay} {tinhTrang} ({KiemTraXuatBenService.MoTaConLai(chuY)}) · ước theo chi phí " +
                        $"{chuY.Cap} tham khảo{chia}", tien));
                }
            }

            // 5) Doanh thu: chuyen co ngay chay trong ky
            foreach (var c in dl.DoanhThuChuyen.Where(c => c.NgayXuatPhat.Date >= tu.Date && c.NgayXuatPhat.Date <= den.Date))
            {
                if (!theoMac.TryGetValue(c.SoHieuMacTau, out var h)) continue;
                h.ChuyenTrongKy.Add(c);
            }

            // 6) Khuyen nghi; bo mac ngung khai thac khong co du lieu nao trong ky
            var ds = theoMac.Values
                .Where(h => h.MacTau.DangKhaiThac || h.SoChuyen > 0 || h.CoDuLieuChiPhi)
                .ToList();
            foreach (var h in ds)
                (h.MaKhuyenNghi, h.LyDoChuaDu) = KhuyenNghi(h);

            var vanDon = dl.VanDon.Where(v => TrongKy(v.ThoiDiemTao)).ToList();
            return new KetQuaChiPhiDTO
            {
                TuNgay = tu,
                DenLuc = den,
                SoNgay = soNgay,
                MacTau = ds.OrderBy(h => ThuTuSapXep(h.MaKhuyenNghi))
                           .ThenBy(h => h.BienLoiNhuan ?? decimal.MinValue)
                           .ThenBy(h => h.MacTau.MaMacTau)
                           .ToList(),
                ChuaPhanBo = chuaPhanBo.OrderByDescending(c => c.ThoiDiem).ToList(),
                ThucChi = thucChi,
                SoVanDon = vanDon.Count,
                CuocHangHoa = vanDon.Sum(v => v.CuocPhi)
            };
        }

        public static (string Ma, string LyDo) KhuyenNghi(HieuQuaMacTauDTO h)
        {
            if (h.MacTau.LaTauHang)
                return ("CHUA_DU", "Tàu hàng: doanh thu là cước vận đơn, mà vận đơn chưa gắn chuyến tàu nên chưa tính được theo mác.");
            if (h.SoChuyen == 0)
                return ("CHUA_DU", "Chưa có chuyến nào chạy trong kỳ.");
            if (h.SoGhe == 0)
                return ("CHUA_DU", "Chuyến trong kỳ chưa có toa bán vé nên chưa có doanh thu vé.");
            if (!h.CoDuLieuChiPhi)
                return ("CHUA_DU", "Chưa có khoản chi phí bảo trì nào trong kỳ.");

            // Co ghe ma khong ban duoc dong nao: lo tron chi phi
            if (h.BienLoiNhuan is not { } bien) return ("THU_HEP", "");
            return (bien < NguongCanNhac ? "THU_HEP" : bien > NguongMoRong ? "MO_RONG" : "DUY_TRI", "");
        }

        // Thu tu tren danh sach: can xem xet truoc, chua du du lieu sau cung
        public static int ThuTuSapXep(string maKhuyenNghi)
            => maKhuyenNghi switch { "THU_HEP" => 0, "DUY_TRI" => 1, "MO_RONG" => 2, _ => 3 };

        // So doan tau (chinh hoac day) dau may keo, theo mac. Moi doan tau tinh 1 phan.
        public static List<(string Mac, int SoPhan)> PhanTheoDauMay(DuLieuBaoTri dl, int maDauMay)
            => dl.DoanTau.Where(d => d.MaDauMayChinh == maDauMay || d.MaDauMayDay == maDauMay)
                         .GroupBy(d => d.SoHieuMacTau)
                         .Select(g => (g.Key, g.Count()))
                         .OrderBy(p => p.Key, StringComparer.Ordinal)
                         .ToList();

        // Chia soTien theo ty le so phan, tron dong; phan du (do lam tron) don vao phan cuoi
        public static List<(string Mac, int SoPhan, decimal Tien)> ChiaTronDong(decimal soTien,
                                                                              List<(string Mac, int SoPhan)> phan, int tong)
        {
            var kq = new List<(string, int, decimal)>();
            decimal daChia = 0m;
            for (int i = 0; i < phan.Count; i++)
            {
                decimal tien = i == phan.Count - 1
                    ? soTien - daChia
                    : Math.Round(soTien * phan[i].SoPhan / tong, 0, MidpointRounding.AwayFromZero);
                daChia += tien;
                kq.Add((phan[i].Mac, phan[i].SoPhan, tien));
            }
            return kq;
        }
    }
}
