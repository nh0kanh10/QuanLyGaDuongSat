using DTO.BaoTri;
using DTO.PhuongTien;

namespace BUS.Services
{
    // =========================================================================
    // BO LUAT BAO TRI - ham thuan (unit test duoc), dialog / trang / Service dung chung.
    //   Kham xe   : nguong trung cot PERSISTED baotri.KhamXeKyThuat.DuDieuKienXuatBen.
    //   Nhien lieu: SFC = so lit x 10.000 / (tan keo x km) [lit / 10.000 tan.km] (quyet dinh 24/10),
    //               dinh muc theo dong may (phuongtien.DongDauMay.DinhMucSFC, migration V20261024_1000).
    //   Bao duong : chu ky tinh tu lan bao duong gan nhat cua chinh phuong tien (khop PhuongTienService):
    //               dau may R1 50.000 km, R2 150.000 km (R2 bao gom R1); toa D1 180 ngay, D2 360 ngay.
    // Nguong / dinh muc / chu ky / chi phi tham khao CHUA doi chieu quy dinh VNR - bao cao ghi ro la gia dinh.
    // =========================================================================
    public static class QuyTacBaoTri
    {
        // =====================================================================
        // KHAM XE
        // =====================================================================
        public const decimal ApLucHamToiThieuBar = 4.8m;
        public const decimal DoSutApToiDaBarPhut = 0.20m;
        public const decimal ApLucHamGioiHanBar = 10.0m;       // CHECK > 0 AND <= 10.0, DECIMAL(3,1)
        public const decimal DoSutApGioiHanBarPhut = 5.0m;     // CHECK >= 0 AND <= 5.0, DECIMAL(3,2)
        public const decimal ChiPhiToiDa = 10_000_000_000m;    // chan go nham (cot DECIMAL(14,0))

        public static bool DuDieuKienXuatBen(decimal apLuc, decimal sutAp, bool daCapNuoc, bool daXaVeSinh)
            => apLuc >= ApLucHamToiThieuBar && sutAp <= DoSutApToiDaBarPhut && daCapNuoc && daXaVeSinh;

        public static bool DuDieuKienXuatBen(KhamXeDTO bb)
            => DuDieuKienXuatBen(bb.ApLucHamBar, bb.DoSutApBarPhut, bb.DaCapNuoc, bb.DaXaVeSinh);

        // Cac hang muc khong dat, vd "Áp lực hãm 4.5 bar · Chưa xả vệ sinh"
        public static string TomTatLoi(KhamXeDTO bb)
        {
            var loi = new List<string>();
            if (bb.ApLucHamBar < ApLucHamToiThieuBar) loi.Add($"Áp lực hãm {bb.ApLucHamBar:0.0} bar");
            if (bb.DoSutApBarPhut > DoSutApToiDaBarPhut) loi.Add($"Sụt áp {bb.DoSutApBarPhut:0.00} bar/phút");
            if (!bb.DaCapNuoc) loi.Add("Chưa cấp nước");
            if (!bb.DaXaVeSinh) loi.Add("Chưa xả vệ sinh");
            return loi.Count == 0 ? "Đạt toàn bộ hạng mục" : string.Join(" · ", loi);
        }

        // Kiem tra tung truong (Truong: DoanTau | NguoiKham | ApLuc | SutAp | GhiChu | ChiPhi)
        public static List<LoiNhapLieuDTO> KiemTraKhamXe(KhamXeDTO bb)
        {
            var ds = new List<LoiNhapLieuDTO>();
            if (bb.MaDoanTau <= 0) ds.Add(new("DoanTau", "Vui lòng chọn đoàn tàu được khám."));
            if (bb.MaNguoiKham <= 0) ds.Add(new("NguoiKham", "Vui lòng chọn nhân viên khám xe."));

            bool apLucHopLe = false, sutApHopLe = false;
            if (bb.ApLucHamBar <= 0 || bb.ApLucHamBar > ApLucHamGioiHanBar)
                ds.Add(new("ApLuc", $"Áp lực hãm phải lớn hơn 0 và không quá {ApLucHamGioiHanBar:0.0} bar."));
            else if (QuaSoChuSoLe(bb.ApLucHamBar, 1))
                ds.Add(new("ApLuc", "Chỉ ghi 1 chữ số thập phân (ví dụ 4.8), CSDL lưu DECIMAL(3,1)."));
            else apLucHopLe = true;

            if (bb.DoSutApBarPhut < 0 || bb.DoSutApBarPhut > DoSutApGioiHanBarPhut)
                ds.Add(new("SutAp", $"Độ sụt áp phải từ 0 đến {DoSutApGioiHanBarPhut:0.00} bar/phút."));
            else if (QuaSoChuSoLe(bb.DoSutApBarPhut, 2))
                ds.Add(new("SutAp", "Chỉ ghi 2 chữ số thập phân (ví dụ 0.18), CSDL lưu DECIMAL(3,2)."));
            else sutApHopLe = true;

            // Khong dat thi phai ghi nguyen nhan / bien phap (quy uoc nghiep vu, CSDL cho NULL)
            if (apLucHopLe && sutApHopLe && !DuDieuKienXuatBen(bb) && string.IsNullOrWhiteSpace(bb.GhiChuKyThuat))
                ds.Add(new("GhiChu", "Biên bản không đạt: ghi rõ nguyên nhân và biện pháp xử lý (có thể bấm \"Tạo từ kết quả\")."));

            if (KiemTraChiPhi(bb.ChiPhi) is { } loiCp) ds.Add(new("ChiPhi", loiCp));
            return ds;
        }

        public static string? KiemTraChiPhi(decimal chiPhi)
            => chiPhi < 0 ? "Chi phí không được âm."
             : chiPhi > ChiPhiToiDa ? $"Chi phí tối đa {ChiPhiToiDa:N0} đ cho một phiếu."
             : QuaSoChuSoLe(chiPhi, 0) ? "Chi phí tính theo đồng, không có số lẻ."
             : null;

        // =====================================================================
        // NHIEN LIEU
        // =====================================================================
        public const decimal HeSoQuyDoi = 10_000m;
        public const decimal NguongSatDinhMuc = 90m;            // % dinh muc: tu day tro len la "sat dinh muc"
        public const decimal DonGiaGoiY = 21_000m;              // gia dau DO tham khao (VND/lit) dien san trong dialog
        public const decimal DonGiaToiDa = 1_000_000m;          // VND/lit - chan go nham (cot DECIMAL(10,0))
        public const decimal SoLitGioiHan = 999_999.99m;        // DECIMAL(8,2), CHECK > 0
        public const decimal TrongLuongGioiHan = 99_999.99m;    // DECIMAL(7,2), CHECK > 0
        public const decimal CuLyGioiHan = 99_999.9m;           // DECIMAL(6,1), CHECK > 0
        public const decimal SfcGioiHan = 999.99m;              // DECIMAL(5,2), CHECK >= 0
        public const int DoDaiNoiCapToiDa = 100;                // NVARCHAR(100)

        // Lam tron nua tren len (khop ROUND cua SQL trong migration), khong lam tron kieu ngan hang
        public static decimal? TinhSfc(decimal soLit, decimal tanKeo, decimal cuLyKm)
            => soLit > 0 && tanKeo > 0 && cuLyKm > 0
                ? Math.Round(soLit * HeSoQuyDoi / (tanKeo * cuLyKm), 2, MidpointRounding.AwayFromZero)
                : null;

        public static decimal TinhTyLe(decimal sfc, decimal dinhMuc)
            => dinhMuc > 0 ? Math.Round(sfc / dinhMuc * 100m, 1, MidpointRounding.AwayFromZero) : 0m;

        // VUOT | SAT | DAT
        public static string XepMuc(decimal tyLe)
            => tyLe > 100m ? "VUOT" : tyLe >= NguongSatDinhMuc ? "SAT" : "DAT";

        // Service tinh lai 2 cot SFC + co vuot muc + thanh tien tu so do (khong tin gia tri giao dien gui len)
        public static void TinhLaiSuatTieuHao(CapNhienLieuDTO nl, decimal dinhMuc)
        {
            nl.SuatTieuHaoSFC = TinhSfc(nl.SoLitTraNap, nl.TrongLuongKeoTan, nl.CuLyChayKm) ?? 0m;
            nl.CanhBaoVuotMuc = nl.SuatTieuHaoSFC > dinhMuc;
            nl.ChiPhi = Math.Round(nl.SoLitTraNap * nl.DonGiaVndLit, 0, MidpointRounding.AwayFromZero);
        }

        // Truong: DauMay | NoiCap | NguoiCap | SoLit | TanKeo | CuLy | DonGia
        public static List<LoiNhapLieuDTO> KiemTraCapDau(CapNhienLieuDTO nl, DauMayBaoTriDTO? dm)
        {
            var ds = new List<LoiNhapLieuDTO>();
            if (dm == null) ds.Add(new("DauMay", "Vui lòng chọn đầu máy được cấp dầu."));

            string noiCap = nl.NoiCapDau?.Trim() ?? "";
            if (noiCap.Length == 0) ds.Add(new("NoiCap", "Vui lòng chọn hoặc nhập nơi cấp dầu."));
            else if (noiCap.Length > DoDaiNoiCapToiDa) ds.Add(new("NoiCap", $"Tối đa {DoDaiNoiCapToiDa} ký tự (đang có {noiCap.Length})."));

            if (nl.MaNguoiCap is null or <= 0) ds.Add(new("NguoiCap", "Vui lòng chọn người cấp phát."));

            if (nl.SoLitTraNap <= 0 || nl.SoLitTraNap > SoLitGioiHan)
                ds.Add(new("SoLit", "Số lít phải lớn hơn 0."));
            else if (QuaSoChuSoLe(nl.SoLitTraNap, 2))
                ds.Add(new("SoLit", "Tối đa 2 chữ số thập phân (DECIMAL(8,2))."));
            else if (dm != null && dm.DungTichBonDauLit > 0 && nl.SoLitTraNap > dm.DungTichBonDauLit)
                ds.Add(new("SoLit", $"Vượt dung tích bồn dầu {dm.DungTichBonDauLit:N0} lít của dòng {dm.MaDongCode}."));

            if (nl.TrongLuongKeoTan <= 0 || nl.TrongLuongKeoTan > TrongLuongGioiHan)
                ds.Add(new("TanKeo", $"Trọng lượng phải lớn hơn 0 và không quá {TrongLuongGioiHan:N2} tấn."));
            else if (QuaSoChuSoLe(nl.TrongLuongKeoTan, 2))
                ds.Add(new("TanKeo", "Tối đa 2 chữ số thập phân (DECIMAL(7,2))."));

            if (nl.CuLyChayKm <= 0 || nl.CuLyChayKm > CuLyGioiHan)
                ds.Add(new("CuLy", $"Cự ly phải lớn hơn 0 và không quá {CuLyGioiHan:N1} km."));
            else if (QuaSoChuSoLe(nl.CuLyChayKm, 1))
                ds.Add(new("CuLy", "Tối đa 1 chữ số thập phân (DECIMAL(6,1))."));
            else if (ds.All(l => l.Truong is not ("SoLit" or "TanKeo"))
                     && TinhSfc(nl.SoLitTraNap, nl.TrongLuongKeoTan, nl.CuLyChayKm) is { } sfc && sfc > SfcGioiHan)
                ds.Add(new("CuLy", $"Suất tiêu hao tính ra {sfc:N2} vượt giới hạn {SfcGioiHan:N2} — kiểm tra lại cự ly / trọng lượng."));

            if (nl.DonGiaVndLit <= 0) ds.Add(new("DonGia", "Vui lòng nhập đơn giá dầu (đ/lít)."));
            else if (nl.DonGiaVndLit > DonGiaToiDa) ds.Add(new("DonGia", $"Đơn giá tối đa {DonGiaToiDa:N0} đ/lít."));
            else if (QuaSoChuSoLe(nl.DonGiaVndLit, 0)) ds.Add(new("DonGia", "Đơn giá tính theo đồng, không có số lẻ."));

            return ds;
        }

        // =====================================================================
        // BAO DUONG DINH KY
        // =====================================================================
        public const decimal ChuKyR1Km = PhuongTienService.MocBaoDuongR1;    // 50,000
        public const decimal ChuKyR2Km = PhuongTienService.MocBaoDuongR2;    // 150,000
        public const int ChuKyD1Ngay = 180;
        public const int ChuKyD2Ngay = 360;
        public const decimal NguongSapToi = PhuongTienService.NguongSapToiHanPhanTram;   // 90%
        public const decimal SoKmGioiHan = 99_999_999.9m;   // DECIMAL(9,1), CHECK >= 0

        public static string[] CapCua(string loaiPhuongTien)
            => loaiPhuongTien == "DAU_MAY" ? new[] { "R1", "R2" } : new[] { "D1", "D2" };

        public static bool LaCapCao(string cap) => cap is "R2" or "D2";

        public static bool CapHopLe(string loaiPhuongTien, string cap) => CapCua(loaiPhuongTien).Contains(cap);

        // Chi phi binh quan THAM KHAO (VND): goi y trong dialog + du phong o tab Chi phi.
        public static decimal ChiPhiThamKhao(string cap) => cap switch
        {
            "R1" => 8_000_000m,
            "R2" => 25_000_000m,
            "D1" => 5_000_000m,
            "D2" => 15_000_000m,
            _ => 0m
        };

        // Tien do tung cap cua mot phuong tien, tu lich su bao duong cua chinh no.
        // Cap thap duoc "lam moi" boi ca cap cao (R2 bao gom R1, D2 bao gom D1).
        public static List<TienDoBaoDuongDTO> TinhTienDo(string loaiPhuongTien, decimal soKmTichLuy,
                                                         IEnumerable<BaoDuongDTO> lichSu, DateTime bayGio)
        {
            var ds = lichSu.OrderByDescending(x => x.ThoiDiemHoanThanh).ThenByDescending(x => x.MaBaoDuong).ToList();
            var ketQua = new List<TienDoBaoDuongDTO>();

            foreach (string cap in CapCua(loaiPhuongTien))
            {
                var lan = ds.FirstOrDefault(x => x.CapBaoDuong == cap || (!LaCapCao(cap) && LaCapCao(x.CapBaoDuong)));

                if (loaiPhuongTien == "DAU_MAY")
                {
                    decimal chuKy = cap == "R1" ? ChuKyR1Km : ChuKyR2Km;
                    decimal kmMoc = lan?.SoKmTaiThoiDiem ?? 0m;
                    decimal daChay = Math.Max(0m, soKmTichLuy - kmMoc);
                    ketQua.Add(TaoTienDo(cap, true, chuKy, daChay, kmMoc + chuKy, lan));
                }
                else
                {
                    int chuKy = cap == "D1" ? ChuKyD1Ngay : ChuKyD2Ngay;
                    if (lan == null)
                    {
                        // Toa khong co ngay dua vao khai thac => khong co moc de tinh
                        ketQua.Add(new TienDoBaoDuongDTO(cap, false, chuKy, null, null, null, 0m, "CHUA_CO"));
                    }
                    else
                    {
                        decimal soNgay = Math.Max(0m, Math.Floor((decimal)(bayGio - lan.ThoiDiemHoanThanh).TotalDays));
                        ketQua.Add(TaoTienDo(cap, false, chuKy, soNgay, chuKy, lan));
                    }
                }
            }
            return ketQua;
        }

        private static TienDoBaoDuongDTO TaoTienDo(string cap, bool theoKm, decimal chuKy, decimal daDung, decimal moc, BaoDuongDTO? lan)
        {
            decimal tyLe = Math.Round(daDung / chuKy * 100m, 1, MidpointRounding.AwayFromZero);
            string trangThai = tyLe >= 100m ? "QUA_HAN" : tyLe >= NguongSapToi ? "SAP_TOI" : "BINH_THUONG";
            return new TienDoBaoDuongDTO(cap, theoKm, chuKy, daDung, moc, lan, tyLe, trangThai);
        }

        // Cap can chu y nhat: qua han > sap toi > binh thuong > chua co; cung muc (tu sap toi tro len)
        // thi uu tien cap cao, roi % lon
        public static TienDoBaoDuongDTO? CapCanChuY(IEnumerable<TienDoBaoDuongDTO> tienDo)
            => tienDo.OrderByDescending(t => t.MucNghiemTrong)
                     .ThenByDescending(t => LaCapCao(t.Cap) && t.MucNghiemTrong >= 2)
                     .ThenByDescending(t => t.TyLe)
                     .FirstOrDefault();

        // Tien do cua mot dau may trong anh chup (km tich luy hien tai + lich su cua no)
        public static List<TienDoBaoDuongDTO> TienDoDauMay(DuLieuBaoTri dl, DauMayBaoTriDTO dm)
            => TinhTienDo("DAU_MAY", dm.SoKmTichLuy, dl.LichSuBaoDuong("DAU_MAY", dm.MaDauMay), dl.BayGio);

        public static List<TienDoBaoDuongDTO> TienDoToaXe(DuLieuBaoTri dl, ToaXeBaoTriDTO tx)
            => TinhTienDo("TOA_XE", 0m, dl.LichSuBaoDuong("TOA_XE", tx.MaToaXe), dl.BayGio);

        // Truong: PhuongTien | Cap | SoKm | NguoiThucHien | ChiPhi
        // soKmTichLuy: km hien tai cua dau may (null voi toa xe - CSDL khong theo doi km toa).
        // lichSuTruoc: cac phieu KHAC cua cung phuong tien hoan thanh truoc phieu nay.
        public static List<LoiNhapLieuDTO> KiemTraBaoDuong(BaoDuongDTO bd, decimal? soKmTichLuy, IEnumerable<BaoDuongDTO> lichSuTruoc)
        {
            var ds = new List<LoiNhapLieuDTO>();
            if (bd.LoaiPhuongTien is not ("DAU_MAY" or "TOA_XE") || bd.MaPhuongTien <= 0
                || (bd.LaDauMay ? bd.MaToaXe != null : bd.MaDauMay != null))
                ds.Add(new("PhuongTien", bd.LoaiPhuongTien == "TOA_XE" ? "Vui lòng chọn toa xe." : "Vui lòng chọn đầu máy."));

            if (string.IsNullOrEmpty(bd.CapBaoDuong)) ds.Add(new("Cap", "Vui lòng chọn cấp bảo dưỡng."));
            else if (!CapHopLe(bd.LoaiPhuongTien, bd.CapBaoDuong))
                ds.Add(new("Cap", bd.LaDauMay ? "Đầu máy chỉ bảo dưỡng cấp R1 / R2." : "Toa xe chỉ bảo dưỡng cấp D1 / D2."));

            if (bd.SoKmTaiThoiDiem < 0 || bd.SoKmTaiThoiDiem > SoKmGioiHan)
                ds.Add(new("SoKm", "Số km không hợp lệ."));
            else if (QuaSoChuSoLe(bd.SoKmTaiThoiDiem, 1))
                ds.Add(new("SoKm", "Tối đa 1 chữ số thập phân (DECIMAL(9,1))."));
            // Phieu MOI ghi thoi diem hoan thanh = bay gio => km phai la dong ho hien tai cua dau may
            // (quyet dinh 26/10; dong ho sai thi sua o Phuong tien truoc). Sua phieu cu giu luat cu.
            else if (bd.LaDauMay && soKmTichLuy.HasValue && bd.MaBaoDuong <= 0 && bd.SoKmTaiThoiDiem != soKmTichLuy.Value)
                ds.Add(new("SoKm", $"Phiếu mới ghi tại km tích lũy hiện tại của đầu máy ({soKmTichLuy.Value:N1} km). " +
                                   "Đồng hồ sai thì sửa ở màn Phương tiện trước."));
            else if (bd.LaDauMay && soKmTichLuy.HasValue && bd.SoKmTaiThoiDiem > soKmTichLuy.Value)
                ds.Add(new("SoKm", $"Lớn hơn km tích lũy hiện tại của đầu máy ({soKmTichLuy.Value:N0} km)."));
            else
            {
                // Km khong lui so voi lan bao duong truoc do cua cung phuong tien
                var lanTruoc = lichSuTruoc.OrderByDescending(x => x.SoKmTaiThoiDiem).FirstOrDefault();
                if (lanTruoc != null && bd.SoKmTaiThoiDiem < lanTruoc.SoKmTaiThoiDiem)
                    ds.Add(new("SoKm", $"Nhỏ hơn số km ở lần bảo dưỡng trước ({lanTruoc.SoKmTaiThoiDiem:N0} km, " +
                                       $"{lanTruoc.CapBaoDuong} ngày {lanTruoc.ThoiDiemHoanThanh:dd/MM/yyyy})."));
            }

            if (bd.MaNguoiThucHien is null or <= 0) ds.Add(new("NguoiThucHien", "Vui lòng chọn người thực hiện."));
            if (KiemTraChiPhi(bd.ChiPhi) is { } loiCp) ds.Add(new("ChiPhi", loiCp));
            return ds;
        }

        // =====================================================================
        // TIEN ICH
        // =====================================================================
        public static bool QuaSoChuSoLe(decimal giaTri, int soChuSoLe) => decimal.Round(giaTri, soChuSoLe) != giaTri;
    }
}
