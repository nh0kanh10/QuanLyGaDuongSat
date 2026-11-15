using DTO.PhuongTien;

namespace BUS.Services
{
    // =========================================================================
    // DONG BO TOA BAN VE THEO DOAN TAU (ham thuan, khong cham CSDL - de unit test duoc) - 13/11
    //
    // Dat chot 13/11 (khuyen nghi):
    //   - Toa khach (NC / NML / BN / AN) thu k trong doan tau (thu tu moc noi) <-> "Toa k" ban ve.
    //     Thay toa that CUNG LOAI -> giu toa ban ve, ve giu nguyen toa / cho (chi doi MaToaXe, pham vi ga).
    //   - Phuong an lam hong ve CON HIEU LUC (DA_DAT / DA_LEN_TAU) -> chan luu, liet ke tung ve:
    //       bot toa co ve · doi loai toa co ve · cat / noi lam ve nam ngoai pham vi chay cua toa.
    //   - Toa phai bo ma chi con ve da hoan / da huy -> NGUNG_BAN (khoa ngoai Ve -> ChoNgoi khong cho xoa);
    //     chua tung co ve -> XOA.
    //   - Cung loai thi GIU suc chua dang ban (khong co lai so ghe ve da in); chi bo sung ghe con thieu.
    //   - Tau hang (LoaiTau = TAU_HANG): khong ban ve -> khong co toa ban ve nao.
    //
    // Mot ham cho ca man keo - tha (hang muc "Toa ban ve" trong QuyTacLapTau) va LapTauService (ghi).
    // =========================================================================
    public static class QuyTacToaBanVe
    {
        public static bool LaToaKhach(string maChungLoaiCode) => maChungLoaiCode is "NC" or "NML" or "BN" or "AN";

        // Suc chua khi chung loai chua khai SucChuaChuan (giong seed S20261026_1000 / man Lich trinh)
        public static int SucChuaMacDinh(string loaiToa) => loaiToa switch
        {
            "AN" => 28,
            "BN" => 42,
            "NML" => 64,
            _ => 80
        };

        public static string TenLoai(string loaiToa) => loaiToa switch
        {
            "NC" => "Ngồi cứng",
            "NML" => "Ngồi mềm điều hòa",
            "BN" => "Nằm khoang 6",
            "AN" => "Nằm khoang 4",
            _ => loaiToa
        };

        // Cung dang voi seed S20261026_1000: "Toa 3 (NML)"
        public static string NhanHieu(int thuTuToa, string loaiToa) => $"Toa {thuTuToa} ({loaiToa})";

        public static int SucChuaMoBan(ToaXeLapTauDTO toa)
            => toa.SucChuaChuan > 0 ? toa.SucChuaChuan : SucChuaMacDinh(toa.MaChungLoaiCode);

        // Ga noi = ga dau / ga cat = ga cuoi nghia la "chay suot" -> NULL (y LapTauService.ChuanHoaPhamVi)
        public static (int? Noi, int? Cat) ChuanHoaPhamVi(BoiCanhLapTauDTO bc, int? maGaNoi, int? maGaCat)
        {
            int? gaDau = bc.HanhTrinh.Count > 0 ? bc.HanhTrinh[0].MaGa : null;
            int? gaCuoi = bc.HanhTrinh.Count > 0 ? bc.HanhTrinh[^1].MaGa : null;
            return (maGaNoi == gaDau ? null : maGaNoi, maGaCat == gaCuoi ? null : maGaCat);
        }

        // Ve co nam trong pham vi chay cua toa khong (theo thu tu ga tren lich dung).
        // Khong xac dinh duoc thu tu (ga ve khong co tren lich dung) -> khong chan (du lieu cu).
        public static bool VeTrongPhamVi(BoiCanhLapTauDTO bc, VeTrenToaDTO ve, int? maGaNoi, int? maGaCat)
        {
            if (maGaNoi == null && maGaCat == null) return true;
            int iDi = ViTriGa(bc, ve.MaGaDi), iDen = ViTriGa(bc, ve.MaGaDen);
            if (iDi < 0 || iDen < 0) return true;
            if (maGaNoi != null)
            {
                int iNoi = ViTriGa(bc, maGaNoi.Value);
                if (iNoi >= 0 && iDi < iNoi) return false;
            }
            if (maGaCat != null)
            {
                int iCat = ViTriGa(bc, maGaCat.Value);
                if (iCat >= 0 && iDen > iCat) return false;
            }
            return true;
        }

        private static int ViTriGa(BoiCanhLapTauDTO bc, int maGa) => bc.HanhTrinh.FindIndex(d => d.MaGa == maGa);

        private static string MaGa(BoiCanhLapTauDTO bc, int? maGa)
            => maGa == null ? "" : bc.HanhTrinh.FirstOrDefault(d => d.MaGa == maGa)?.MaGaCode ?? $"#{maGa}";

        private static string MoTaPhamVi(BoiCanhLapTauDTO bc, int? noi, int? cat)
        {
            string tu = noi != null ? MaGa(bc, noi) : bc.HanhTrinh.Count > 0 ? bc.HanhTrinh[0].MaGaCode : bc.MaGaDiCode;
            string den = cat != null ? MaGa(bc, cat) : bc.HanhTrinh.Count > 0 ? bc.HanhTrinh[^1].MaGaCode : bc.MaGaDenCode;
            return $"{tu} → {den}";
        }

        // =====================================================================
        public static KeHoachDongBoToaBanVeDTO LapKeHoach(BoiCanhLapTauDTO bc, PhuongAnLapTauDTO pa)
        {
            var ke = new KeHoachDongBoToaBanVeDTO { LaTauKhach = bc.LaTauKhach };

            // Toa khach trong phuong an theo thu tu moc noi (toa khong co trong doi toa: QuyTacLapTau da bao loi)
            var mongMuon = new List<(ToaXeLapTauDTO Toa, int? Noi, int? Cat)>();
            if (bc.LaTauKhach)
            {
                foreach (var t in pa.DanhSachToa)
                {
                    var toa = bc.TimToa(t.MaToaXe);
                    if (toa == null || !LaToaKhach(toa.MaChungLoaiCode)) continue;
                    var (noi, cat) = ChuanHoaPhamVi(bc, t.MaGaNoi, t.MaGaCat);
                    mongMuon.Add((toa, noi, cat));
                }
            }
            ke.SoToaKhachTrongDoan = mongMuon.Count;

            var hienCo = bc.ToaBanVe.GroupBy(t => t.ThuTuToa).ToDictionary(g => g.Key, g => g.First());
            var veTheoToa = bc.VeTrenToa.GroupBy(v => v.MaToaXeKhach).ToDictionary(g => g.Key, g => g.ToList());

            var cacSo = Enumerable.Range(1, mongMuon.Count).Union(hienCo.Keys).Distinct().OrderBy(k => k);
            foreach (int k in cacSo)
            {
                (ToaXeLapTauDTO Toa, int? Noi, int? Cat)? d = k >= 1 && k <= mongMuon.Count ? mongMuon[k - 1] : null;
                var e = hienCo.GetValueOrDefault(k);
                var ve = e != null ? veTheoToa.GetValueOrDefault(e.MaToaXeKhach) ?? new() : new List<VeTrenToaDTO>();
                var veHieuLuc = ve.Where(v => v.ConHieuLuc).ToList();

                if (e == null)
                {
                    if (d == null) continue;
                    ke.ThaoTac.Add(TaoThem(k, d.Value.Toa, d.Value.Noi, d.Value.Cat, "toa khách mới trong đoàn tàu"));
                    continue;
                }

                if (d == null)
                {
                    string lyDo = bc.LaTauKhach
                        ? $"toa {k} không còn trong đoàn tàu (đoàn chỉ còn {mongMuon.Count} toa khách)"
                        : "tàu hàng không bán vé";
                    if (veHieuLuc.Count > 0) ke.VeBiAnhHuong.AddRange(veHieuLuc.Select(v => TaoAnhHuong(v, k, lyDo)));
                    else ke.ThaoTac.Add(TaoBo(e, ve.Count > 0, lyDo));
                    continue;
                }

                var (toaMoi, noiMoi, catMoi) = d.Value;
                if (e.LoaiToa != toaMoi.MaChungLoaiCode)
                {
                    string lyDo = $"toa {k} đổi từ {TenLoai(e.LoaiToa)} sang {TenLoai(toaMoi.MaChungLoaiCode)}";
                    if (veHieuLuc.Count > 0)
                    {
                        ke.VeBiAnhHuong.AddRange(veHieuLuc.Select(v => TaoAnhHuong(v, k, lyDo)));
                        continue;
                    }
                    ke.ThaoTac.Add(TaoBo(e, ve.Count > 0, lyDo));
                    ke.ThaoTac.Add(TaoThem(k, toaMoi, noiMoi, catMoi, lyDo));
                    continue;
                }

                // Cung loai: giu toa ban ve; ve con hieu luc phai nam trong pham vi chay moi
                var ngoaiPhamVi = veHieuLuc.Where(v => !VeTrongPhamVi(bc, v, noiMoi, catMoi)).ToList();
                if (ngoaiPhamVi.Count > 0)
                {
                    string lyDo = $"toa {k} chỉ chạy {MoTaPhamVi(bc, noiMoi, catMoi)} sau khi sửa";
                    ke.VeBiAnhHuong.AddRange(ngoaiPhamVi.Select(v => TaoAnhHuong(v, k, lyDo)));
                    continue;
                }

                var (noiCu, catCu) = ChuanHoaPhamVi(bc, e.MaGaNoi, e.MaGaCat);
                bool doiToaThat = e.MaToaXe != toaMoi.MaToaXe;
                bool doiPhamVi = noiCu != noiMoi || catCu != catMoi
                                 || e.MaGaNoi != noiCu || e.MaGaCat != catCu;   // CSDL dang luu ga dau / cuoi -> chuan hoa
                bool thieuGhe = e.SoGhe < e.SucChua;
                var lyDoCapNhat = new List<string>();
                if (doiToaThat) lyDoCapNhat.Add($"toa thật {toaMoi.SoHieuToaXe}");
                if (doiPhamVi) lyDoCapNhat.Add($"phạm vi {MoTaPhamVi(bc, noiMoi, catMoi)}");
                if (thieuGhe) lyDoCapNhat.Add($"bổ sung {e.SucChua - e.SoGhe} chỗ còn thiếu");

                ke.ThaoTac.Add(new ThaoTacToaBanVeDTO
                {
                    Loai = LoaiThaoTacToaBanVe.CapNhat,
                    ThuTuToa = k,
                    MaToaXeKhach = e.MaToaXeKhach,
                    NhanHieuToa = e.NhanHieuToa,
                    LoaiToa = e.LoaiToa,
                    SucChua = e.SucChua,
                    MaToaXe = toaMoi.MaToaXe,
                    MaGaNoi = noiMoi,
                    MaGaCat = catMoi,
                    LyDo = string.Join(", ", lyDoCapNhat),
                    CoGhi = doiToaThat || doiPhamVi || thieuGhe
                });
            }
            return ke;
        }

        private static ThaoTacToaBanVeDTO TaoThem(int k, ToaXeLapTauDTO toa, int? noi, int? cat, string lyDo) => new()
        {
            Loai = LoaiThaoTacToaBanVe.Them,
            ThuTuToa = k,
            NhanHieuToa = NhanHieu(k, toa.MaChungLoaiCode),
            LoaiToa = toa.MaChungLoaiCode,
            SucChua = SucChuaMoBan(toa),
            MaToaXe = toa.MaToaXe,
            MaGaNoi = noi,
            MaGaCat = cat,
            LyDo = lyDo
        };

        private static ThaoTacToaBanVeDTO TaoBo(ToaBanVeDTO e, bool coVeLichSu, string lyDo) => new()
        {
            Loai = coVeLichSu ? LoaiThaoTacToaBanVe.NgungBan : LoaiThaoTacToaBanVe.Xoa,
            ThuTuToa = e.ThuTuToa,
            MaToaXeKhach = e.MaToaXeKhach,
            NhanHieuToa = e.NhanHieuToa,
            LoaiToa = e.LoaiToa,
            SucChua = e.SucChua,
            MaToaXe = e.MaToaXe,
            MaGaNoi = e.MaGaNoi,
            MaGaCat = e.MaGaCat,
            LyDo = coVeLichSu ? lyDo + " (giữ lịch sử vé đã hoàn / đã hủy)" : lyDo
        };

        private static VeBiAnhHuongDTO TaoAnhHuong(VeTrenToaDTO v, int k, string lyDo) => new()
        {
            MaVeCode = v.MaVeCode,
            TenHanhKhach = v.TenHanhKhach,
            ThuTuToa = k,
            SoGhe = v.SoGhe,
            Chang = $"{v.MaGaDiCode} → {v.MaGaDenCode}",
            LyDo = lyDo
        };

        // Noi dung hang muc / thong bao chan luu: toi da `toiDa` dong + "... va N ve khac"
        public static string MoTaVeBiAnhHuong(KeHoachDongBoToaBanVeDTO ke, int toiDa = 8)
        {
            var dong = ke.VeBiAnhHuong
                .OrderBy(v => v.ThuTuToa).ThenBy(v => v.SoGhe)
                .Take(toiDa).Select(v => "• " + v.MoTa).ToList();
            int conLai = ke.VeBiAnhHuong.Count - dong.Count;
            if (conLai > 0) dong.Add($"… và {conLai} vé khác.");
            return string.Join("\n", dong);
        }
    }
}
