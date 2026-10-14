using DTO.PhuongTien;

namespace BUS.Services
{
    // =========================================================================
    // BO LUAT LAP DOAN TAU (ham thuan, khong cham CSDL - de unit test duoc)
    //
    // Mot bo luat duy nhat cho:
    //   - man hinh keo - tha (chay lai sau moi thao tac, hien bang kiem tra an toan),
    //   - tab Lap doan tau (kiem tra doan tau dang luu trong CSDL),
    //   - LapTauService.LuuPhuongAn (kiem lai trong giao dich truoc khi ghi).
    //
    // Mo hinh cat / noi (CLAUDE.md §5.6): doan tau la MOT danh sach toa theo thu tu moc
    // noi; moi toa co pham vi MaGaNoi -> MaGaCat tren lich dung ga. Khu doan k = ga k -> k+1.
    // Toa co mat o khu doan k khi ViTriNoi <= k < ViTriCat. Cac khu doan lien tiep cung bo
    // toa gop thanh mot chang.
    // =========================================================================
    public static class QuyTacLapTau
    {
        // CHECK cua vanhanh.DoanTau
        public const int SoToaToiDa = 20;
        public const decimal ChieuDaiToiDaM = 450.0m;

        // Nguong GIA DINH cho kho duong 1.000 mm - chua co nguon chinh thuc (12/10).
        // Tai trong truc cho phep do doanh nghiep duong sat cong bo theo tung tuyen / khu doan trong
        // "cong lenh tai trong" (Luat Duong sat 2017, TT 27/2018/TT-BGTVT); khong tim duoc ban cong
        // khai cho tuyen Ha Noi - TP.HCM. Doi chieu: dau may D19E the he moi 13,5 t/truc (VNR, 08/2026).
        // Giao dien ghi "nguong gia dinh"; bao cao ghi vao muc gia dinh / han che.
        public const decimal TaiTrongTrucToiDaTan = 14.0m;

        // Thoi gian do toi thieu tai ga cat / noi toa (don toa, noi ong gio, thu ham lai).
        // QUY UOC, chua doi chieu quy trinh tac nghiep cua VNR.
        public const int SoPhutDoToiThieuCatNoi = 15;

        public const decimal NguongSatSucKeoPhanTram = 90m;
        public const decimal DuTruChieuDaiSatM = 20m;

        public static readonly string[] TrangThaiChuyenDuocLapTau = { "DA_LEN_LICH", "SAN_SANG" };

        public static bool LaToaHang(string maChungLoaiCode) => maChungLoaiCode is "G" or "M" or "P";

        public static bool ChuyenConHieuLuc(string trangThaiChuyen) => trangThaiChuyen is not ("HOAN_THANH" or "DA_HUY");

        public static bool ChuyenDangChay(string trangThaiChuyen) => trangThaiChuyen is "DANG_CHAY" or "TRE_GIO";

        // Dau may / toa khong duoc dua vao doan tau: dang bao duong hoac da ngung khai thac (14/10)
        public static bool KhongDuocVanDung(string trangThai) => trangThai is "BAO_DUONG" or PhuongTienService.NgungKhaiThac;

        private static string MoTaKhongVanDung(string trangThai)
            => trangThai == PhuongTienService.NgungKhaiThac ? "đã ngừng khai thác" : "đang bảo dưỡng";

        // Hai khung gio chong nhau: y nhu trigger trg_PhanCongKipLai_CheckTrungGio
        public static bool ChongGio(DateTime aTu, DateTime aDen, DateTime bTu, DateTime bDen)
            => (aTu > bTu ? aTu : bTu) < (aDen < bDen ? aDen : bDen);

        // Quyet dinh 09/10: chi lap / sua doan tau khi chuyen chua chay
        public static (bool ChoPhep, string LyDo) KiemTraChoPhepSua(string trangThaiChuyen) => trangThaiChuyen switch
        {
            "DA_LEN_LICH" or "SAN_SANG" => (true, ""),
            "DANG_CHAY" or "TRE_GIO" => (false, "Chuyến tàu đang chạy: chỉ xem đoàn tàu, không lập / sửa được."),
            "HOAN_THANH" => (false, "Chuyến tàu đã hoàn thành: chỉ xem đoàn tàu, không lập / sửa được."),
            "DA_HUY" => (false, "Chuyến tàu đã hủy: không lập đoàn tàu."),
            _ => (false, $"Trạng thái chuyến \"{trangThaiChuyen}\" không cho phép lập / sửa đoàn tàu.")
        };

        // Doi chieu trong luong keo voi suc keo dinh muc cua dau may chinh
        public static (bool DatYeuCau, decimal TyLePhanTram, string ThongDiep) DoiChieuSucKeo(decimal trongLuongTan, decimal? sucKeoToiDaTan)
        {
            if (sucKeoToiDaTan is null or <= 0)
                return (true, 0m, "Chưa chỉ định đầu máy kéo chính để đối chiếu sức kéo.");

            decimal tyLe = Math.Round(trongLuongTan / sucKeoToiDaTan.Value * 100m, 1);

            if (tyLe > 100m)
                return (false, tyLe, $"Quá tải {tyLe:N1}% định mức sức kéo ({sucKeoToiDaTan.Value:N0} tấn).");

            if (tyLe > NguongSatSucKeoPhanTram)
                return (true, tyLe, $"Sát định mức: đang dùng {tyLe:N1}% sức kéo khả dụng.");

            return (true, tyLe, $"Đạt yêu cầu: đang dùng {tyLe:N1}% sức kéo khả dụng.");
        }

        // Gioi han chieu dai tai mot ga dung: duong dieu do da gan, khong co thi duong tranh
        // dai nhat cua ga (chi can MOT duong du dai la tranh duoc), tran 450 m cua CHECK.
        public static (decimal GioiHanM, string Nguon) GioiHanChieuDaiTaiGa(DiemDungHanhTrinhDTO ga)
        {
            if (ga.ChieuDaiDuongGanM is > 0)
                return (Math.Min(ga.ChieuDaiDuongGanM.Value, ChieuDaiToiDaM), $"đường {ga.SoHieuDuongGan} điều độ đã gán");

            if (ga.DuongTranhDaiNhatM is > 0)
                return (Math.Min(ga.DuongTranhDaiNhatM.Value, ChieuDaiToiDaM), $"đường tránh dài nhất (đường {ga.SoHieuDuongTranhDaiNhat})");

            return (ChieuDaiToiDaM, "ga chưa có dữ liệu đường tránh, dùng giới hạn hệ thống");
        }

        // =====================================================================
        // KIEM TRA MOT PHUONG AN
        // =====================================================================
        public static KetQuaKiemTraLapTauDTO KiemTra(BoiCanhLapTauDTO bc, PhuongAnLapTauDTO pa)
        {
            var kq = new KetQuaKiemTraLapTauDTO();
            var hanhTrinh = bc.HanhTrinh;
            int soGa = hanhTrinh.Count;
            bool coHanhTrinh = soGa >= 2;
            bool choCatNoi = soGa >= 3;
            int soKhuDoan = coHanhTrinh ? soGa - 1 : 1;

            var dauMay = bc.TimDauMay(pa.MaDauMayChinh);
            var dauMayDay = bc.TimDauMay(pa.MaDauMayDay);

            // --- 0. Cau truc phuong an + vi tri noi / cat cua tung toa ---
            var loiCauTruc = new List<string>();
            if (pa.MaDauMayChinh != null && dauMay == null) loiCauTruc.Add("Đầu máy kéo chính không tồn tại trong đội đầu máy.");
            if (pa.MaDauMayDay != null && dauMayDay == null) loiCauTruc.Add("Đầu máy đẩy không tồn tại trong đội đầu máy.");

            var cacToa = new List<ToaTrongPhuongAn>();
            var daGap = new HashSet<int>();
            foreach (var t in pa.DanhSachToa)
            {
                var toa = bc.TimToa(t.MaToaXe);
                if (toa == null)
                {
                    loiCauTruc.Add($"Toa mã #{t.MaToaXe} không có trong đội toa xe.");
                    continue;
                }
                if (!daGap.Add(t.MaToaXe))
                {
                    loiCauTruc.Add($"Toa {toa.SoHieuToaXe} xuất hiện hai lần trong đoàn tàu.");
                    continue;
                }

                int noi = 0, cat = soKhuDoan;
                if (t.MaGaNoi != null || t.MaGaCat != null)
                {
                    if (!choCatNoi)
                    {
                        loiCauTruc.Add($"{toa.SoHieuToaXe}: chuyến không có ga dừng trung gian nên không cắt / nối toa dọc đường được.");
                    }
                    else
                    {
                        if (t.MaGaNoi != null)
                        {
                            int i = hanhTrinh.FindIndex(d => d.MaGa == t.MaGaNoi);
                            if (i < 0) loiCauTruc.Add($"{toa.SoHieuToaXe}: ga nối không nằm trên hành trình của chuyến.");
                            else if (i == soGa - 1) loiCauTruc.Add($"{toa.SoHieuToaXe}: không nối toa tại ga cuối.");
                            else noi = i;
                        }
                        if (t.MaGaCat != null)
                        {
                            int j = hanhTrinh.FindIndex(d => d.MaGa == t.MaGaCat);
                            if (j < 0) loiCauTruc.Add($"{toa.SoHieuToaXe}: ga cắt không nằm trên hành trình của chuyến.");
                            else if (j == 0) loiCauTruc.Add($"{toa.SoHieuToaXe}: không cắt toa tại ga đầu.");
                            else cat = j;
                        }
                        if (noi >= cat)
                        {
                            loiCauTruc.Add($"{toa.SoHieuToaXe}: ga nối phải đứng trước ga cắt trên hành trình.");
                            noi = 0;
                            cat = soKhuDoan;
                        }
                    }
                }

                cacToa.Add(new ToaTrongPhuongAn(toa, noi, cat));
            }

            // --- Khu doan ---
            for (int k = 0; k < soKhuDoan; k++)
            {
                var toaK = cacToa.Where(x => x.Noi <= k && k < x.Cat).ToList();
                kq.KhuDoan.Add(new KhuDoanDTO
                {
                    ViTri = k,
                    TuGaCode = coHanhTrinh ? hanhTrinh[k].MaGaCode : "",
                    DenGaCode = coHanhTrinh ? hanhTrinh[k + 1].MaGaCode : "",
                    MaToaXe = toaK.Select(x => x.Toa.MaToaXe).ToList(),
                    ChieuDaiM = (dauMay?.ChieuDaiM ?? 0m) + toaK.Sum(x => x.Toa.ChieuDaiM),
                    TrongLuongTan = toaK.Sum(x => x.Toa.TongTrongLuongTan)
                });
            }

            // --- Chang: gop khu doan lien tiep cung bo toa ---
            foreach (var kd in kq.KhuDoan)
            {
                var cuoi = kq.Chang.LastOrDefault();
                if (cuoi != null && cuoi.MaToaXe.SequenceEqual(kd.MaToaXe))
                {
                    cuoi.DenViTri = kd.ViTri + 1;
                    continue;
                }
                kq.Chang.Add(new ChangDTO
                {
                    TuViTri = kd.ViTri,
                    DenViTri = kd.ViTri + 1,
                    MaToaXe = kd.MaToaXe,
                    ChieuDaiM = kd.ChieuDaiM,
                    TrongLuongTan = kd.TrongLuongTan
                });
            }
            foreach (var c in kq.Chang)
                c.Ten = coHanhTrinh ? $"{hanhTrinh[c.TuViTri].MaGaCode} → {hanhTrinh[c.DenViTri].MaGaCode}" : "Toàn hành trình";

            var changDai = kq.Chang.MaxBy(c => c.ChieuDaiM)!;
            var changNang = kq.Chang.MaxBy(c => c.TrongLuongTan)!;
            kq.SoToaLonNhat = kq.KhuDoan.Max(x => x.SoToa);
            kq.ChieuDaiLonNhatM = changDai.ChieuDaiM;
            kq.TrongLuongLonNhatTan = changNang.TrongLuongTan;
            kq.ChangDaiNhat = changDai.Ten;
            kq.ChangNangNhat = changNang.Ten;

            // --- Tac nghiep cat / noi tai ga trung gian ---
            if (choCatNoi)
            {
                for (int k = 1; k < soGa - 1; k++)
                {
                    var cat = cacToa.Where(x => x.Cat == k).Select(x => x.Toa.MaToaXe).ToList();
                    var noi = cacToa.Where(x => x.Noi == k).Select(x => x.Toa.MaToaXe).ToList();
                    if (cat.Count == 0 && noi.Count == 0) continue;

                    kq.TacNghiep.Add(new TacNghiepGaDTO
                    {
                        ViTri = k,
                        MaGaCode = hanhTrinh[k].MaGaCode,
                        TenGa = hanhTrinh[k].TenGa,
                        SoPhutDo = hanhTrinh[k].SoPhutDo,
                        Cat = cat,
                        Noi = noi,
                        ViTriCat = XacDinhViTriNhom(kq.KhuDoan[k - 1].MaToaXe, cat),
                        ViTriNoi = XacDinhViTriNhom(kq.KhuDoan[k].MaToaXe, noi),
                        DuThoiGian = hanhTrinh[k].SoPhutDo >= SoPhutDoToiThieuCatNoi
                    });
                }
            }
            kq.MoTaCatNoi = string.Join(" · ", kq.TacNghiep.Select(tn => $"{tn.MaGaCode} {tn.NhanThayDoi}"));

            // --- Chieu dai tai tung ga dung: doan den va doan roi ga deu phai lot duong ---
            var vuotTaiGa = new List<string>();
            (int ViTri, decimal ConDu, decimal GioiHan)? gaHanCheNhat = null;
            if (coHanhTrinh)
            {
                for (int s = 0; s < soGa; s++)
                {
                    decimal daiTaiGa = 0m;
                    if (s >= 1) daiTaiGa = Math.Max(daiTaiGa, kq.KhuDoan[s - 1].ChieuDaiM);
                    if (s <= soKhuDoan - 1) daiTaiGa = Math.Max(daiTaiGa, kq.KhuDoan[s].ChieuDaiM);

                    var (gioiHan, nguon) = GioiHanChieuDaiTaiGa(hanhTrinh[s]);
                    decimal conDu = gioiHan - daiTaiGa;
                    if (conDu < 0)
                    {
                        kq.GaVuotChieuDai.Add(s);
                        vuotTaiGa.Add($"{hanhTrinh[s].TenGa} ({hanhTrinh[s].MaGaCode}): đoàn tàu dài {daiTaiGa:N1} m, {nguon} chỉ {gioiHan:N0} m");
                    }
                    if (gaHanCheNhat == null || conDu < gaHanCheNhat.Value.ConDu)
                        gaHanCheNhat = (s, conDu, gioiHan);
                }
            }

            foreach (var kd in kq.KhuDoan)
            {
                bool vuotGa = coHanhTrinh && (kq.GaVuotChieuDai.Contains(kd.ViTri) || kq.GaVuotChieuDai.Contains(kd.ViTri + 1));
                kd.CoLoi = kd.SoToa == 0 || kd.SoToa > SoToaToiDa || kd.ChieuDaiM > ChieuDaiToiDaM || vuotGa ||
                           (dauMay != null && dauMay.SucKeoToiDaTan > 0 && kd.TrongLuongTan > dauMay.SucKeoToiDaTan);
            }

            // =================================================================
            // BANG HANG MUC
            // =================================================================
            var hm = kq.HangMuc;

            if (loiCauTruc.Count > 0)
                hm.Add(HangMucKiemTraDTO.Loi("Dữ liệu phương án", string.Join("\n", loiCauTruc)));

            KiemTraDauMayChinh(bc, dauMay, hanhTrinh, hm);
            KiemTraDauMayDay(bc, pa, dauMay, dauMayDay, hm);

            // 3. So toa
            const string tenSoToa = "Số toa trong đoàn";
            var khuDoanRong = kq.KhuDoan.Where(x => x.SoToa == 0).ToList();
            var khuDoanVuot = kq.KhuDoan.Where(x => x.SoToa > SoToaToiDa).ToList();
            var changDongNhat = kq.Chang.MaxBy(x => x.SoToa)!;
            if (cacToa.Count == 0)
                hm.Add(HangMucKiemTraDTO.Loi(tenSoToa, "Chưa móc nối toa nào vào đầu máy."));
            else if (khuDoanRong.Count > 0)
                hm.Add(HangMucKiemTraDTO.Loi(tenSoToa,
                    $"Đoạn {string.Join(", ", khuDoanRong.Select(x => x.Ten))} không còn toa nào. Đoàn tàu phải có ít nhất 1 toa trên mọi khu đoạn."));
            else if (khuDoanVuot.Count > 0)
                hm.Add(HangMucKiemTraDTO.Loi(tenSoToa,
                    $"Đoạn {string.Join(", ", khuDoanVuot.Select(x => $"{x.Ten} ({x.SoToa} toa)"))} vượt giới hạn {SoToaToiDa} toa của một đoàn tàu."));
            else
                hm.Add(HangMucKiemTraDTO.Dat(tenSoToa, kq.Chang.Count > 1
                    ? $"Lớn nhất {changDongNhat.SoToa} toa (đoạn {changDongNhat.Ten}), trong giới hạn 1–{SoToaToiDa} toa."
                    : $"Biên chế {changDongNhat.SoToa} toa, trong giới hạn 1–{SoToaToiDa} toa."));

            // 4. Chieu dai vs duong ray tung ga
            const string tenChieuDai = "Chiều dài đoàn tàu vs đường tránh";
            var khuDoanQuaDai = kq.KhuDoan.Where(x => x.ChieuDaiM > ChieuDaiToiDaM).ToList();
            string moDauDai = kq.Chang.Count > 1
                ? $"Dài nhất đoạn {kq.ChangDaiNhat}: {kq.ChieuDaiLonNhatM:N1} m kể cả đầu máy."
                : $"Dài {kq.ChieuDaiLonNhatM:N1} m kể cả đầu máy.";
            if (vuotTaiGa.Count > 0 || khuDoanQuaDai.Count > 0)
            {
                var dong = new List<string>(vuotTaiGa);
                if (khuDoanQuaDai.Count > 0 && !coHanhTrinh)
                    dong.Add($"Vượt giới hạn {ChieuDaiToiDaM:N0} m của một đoàn tàu.");
                hm.Add(HangMucKiemTraDTO.Loi(tenChieuDai, $"{moDauDai}\n{string.Join("\n", dong)}"));
            }
            else if (!coHanhTrinh)
            {
                hm.Add(HangMucKiemTraDTO.Dat(tenChieuDai,
                    $"{moDauDai} Chuyến chưa có lịch dừng ga: đối chiếu giới hạn {ChieuDaiToiDaM:N0} m của hệ thống."));
            }
            else if (gaHanCheNhat != null)
            {
                var (viTri, conDu, gioiHan) = gaHanCheNhat.Value;
                string gaHc = $"{hanhTrinh[viTri].TenGa} ({gioiHan:N0} m)";
                hm.Add(HangMucKiemTraDTO.Dat(tenChieuDai, conDu < DuTruChieuDaiSatM
                    ? $"{moDauDai} Sát giới hạn tại {gaHc}: chỉ còn dự trữ {conDu:N1} m."
                    : $"{moDauDai} Lọt đường ở mọi ga dừng; ga hạn chế nhất {gaHc}, còn dự trữ {conDu:N1} m."));
            }

            // 5. Trong luong keo vs suc keo (khu doan nang nhat)
            const string tenSucKeo = "Trọng lượng kéo vs sức kéo";
            var (datSucKeo, _, thongDiepSucKeo) = DoiChieuSucKeo(kq.TrongLuongLonNhatTan, dauMay?.SucKeoToiDaTan);
            string moDauSucKeo = kq.Chang.Count > 1
                ? $"Đoạn nặng nhất {kq.ChangNangNhat}: kéo {kq.TrongLuongLonNhatTan:N1} tấn toàn tải."
                : $"Kéo {kq.TrongLuongLonNhatTan:N1} tấn toàn tải.";
            if (dauMay == null)
                hm.Add(HangMucKiemTraDTO.CanhBao(tenSucKeo, thongDiepSucKeo));
            else
                hm.Add(datSucKeo
                    ? HangMucKiemTraDTO.Dat(tenSucKeo, $"{moDauSucKeo} {thongDiepSucKeo}")
                    : HangMucKiemTraDTO.Loi(tenSucKeo, $"{moDauSucKeo} {thongDiepSucKeo}"));

            if (cacToa.Count == 0) return kq;

            // 6. Tai trong truc tung toa
            const string tenTruc = "Tải trọng trục";
            var toaVuotTruc = cacToa.Where(x => TaiTrongTruc(x.Toa) > TaiTrongTrucToiDaTan).ToList();
            if (toaVuotTruc.Count > 0)
                hm.Add(HangMucKiemTraDTO.Loi(tenTruc,
                    $"{string.Join(", ", toaVuotTruc.Select(x => $"{x.Toa.SoHieuToaXe} ({TaiTrongTruc(x.Toa):N2} t)"))} vượt ngưỡng giả định {TaiTrongTrucToiDaTan:N1} t/trục khi chở đầy."));
            else
                hm.Add(HangMucKiemTraDTO.Dat(tenTruc,
                    $"Lớn nhất {cacToa.Max(x => TaiTrongTruc(x.Toa)):N2} t/trục, dưới ngưỡng giả định {TaiTrongTrucToiDaTan:N1} t/trục."));

            // 7. Thu tu xep toa hang tren tung khu doan
            const string tenThuTu = "Thứ tự xếp toa";
            var maToaHang = cacToa.Where(x => LaToaHang(x.Toa.MaChungLoaiCode)).Select(x => x.Toa.MaToaXe).ToHashSet();
            var khuDoanSaiThuTu = kq.KhuDoan
                .Select(x => (KhuDoan: x, KetQua: KiemTraToaHang(x.MaToaXe, maToaHang, bc)))
                .FirstOrDefault(x => !x.KetQua.Dat);
            if (khuDoanSaiThuTu.KhuDoan != null)
            {
                string tienTo = kq.Chang.Count > 1 ? $"Đoạn {khuDoanSaiThuTu.KhuDoan.Ten}: " : "";
                hm.Add(HangMucKiemTraDTO.Loi(tenThuTu, tienTo + khuDoanSaiThuTu.KetQua.MoTa));
            }
            else
            {
                var nhieuHangNhat = kq.KhuDoan.MaxBy(x => x.MaToaXe.Count(maToaHang.Contains))!;
                hm.Add(HangMucKiemTraDTO.Dat(tenThuTu, KiemTraToaHang(nhieuHangNhat.MaToaXe, maToaHang, bc).MoTa));
            }

            // 8. Toa xe san sang: khong bao duong, khong thuoc chuyen khac chong gio
            const string tenToaSanSang = "Toa xe sẵn sàng";
            var toaBaoDuong = cacToa.Where(x => x.Toa.TrangThai == "BAO_DUONG").Select(x => x.Toa.SoHieuToaXe).ToList();
            var toaNgung = cacToa.Where(x => x.Toa.TrangThai == PhuongTienService.NgungKhaiThac).Select(x => x.Toa.SoHieuToaXe).ToList();
            var toaTrungChuyen = cacToa
                .SelectMany(x => x.Toa.ChuyenKhac
                    .Where(c => ChuyenConHieuLuc(c.TrangThai) && ChongGio(c.GioXuatPhatKH, c.GioVeDichKH, bc.GioXuatPhatKH, bc.GioVeDichKH))
                    .Select(c => $"{x.Toa.SoHieuToaXe} đang thuộc đoàn {c.SoHieuMacTau} ({c.KhungGio})"))
                .ToList();
            if (toaBaoDuong.Count > 0 || toaNgung.Count > 0 || toaTrungChuyen.Count > 0)
            {
                var dong = new List<string>();
                if (toaBaoDuong.Count > 0) dong.Add($"Đang bảo dưỡng: {string.Join(", ", toaBaoDuong)}.");
                if (toaNgung.Count > 0) dong.Add($"Đã ngừng khai thác: {string.Join(", ", toaNgung)}.");
                dong.AddRange(toaTrungChuyen);
                hm.Add(HangMucKiemTraDTO.Loi(tenToaSanSang, string.Join("\n", dong)));
            }
            else
            {
                hm.Add(HangMucKiemTraDTO.Dat(tenToaSanSang,
                    $"{cacToa.Count} toa không bảo dưỡng, không thuộc đoàn tàu nào khác trong khung giờ chuyến."));
            }

            // 9. Tac nghiep cat / noi doc duong
            if (kq.TacNghiep.Count > 0)
            {
                const string tenCatNoi = "Cắt / nối toa dọc đường";
                var loi = new List<string>();
                var dong = new List<string>();
                foreach (var tn in kq.TacNghiep)
                {
                    var phan = new List<string>();
                    if (tn.Cat.Count > 0)
                        phan.Add($"cắt {tn.Cat.Count} toa " + (tn.ViTriCat == ViTriNhomToa.Dau ? "ngay sau đầu máy" : "ở cuối đoàn"));
                    if (tn.Noi.Count > 0)
                        phan.Add($"nối {tn.Noi.Count} toa " + (tn.ViTriNoi == ViTriNhomToa.Dau ? "ngay sau đầu máy" : "vào cuối đoàn"));
                    dong.Add($"{tn.MaGaCode} (đỗ {tn.SoPhutDo}'): {string.Join(", ", phan)}");

                    if (!tn.DuThoiGian)
                        loi.Add($"{tn.TenGa} chỉ đỗ {tn.SoPhutDo} phút, cần ít nhất {SoPhutDoToiThieuCatNoi} phút để dồn toa, nối ống gió và thử hãm.");
                    if (tn.ViTriCat == ViTriNhomToa.Sai)
                        loi.Add($"Tại {tn.MaGaCode}: toa cắt ({DanhSachSoHieu(tn.Cat, bc)}) phải liền khối ở cuối đoàn hoặc ngay sau đầu máy.");
                    if (tn.ViTriNoi == ViTriNhomToa.Sai)
                        loi.Add($"Tại {tn.MaGaCode}: toa nối ({DanhSachSoHieu(tn.Noi, bc)}) phải xếp liền khối ở cuối đoàn hoặc ngay sau đầu máy, không chen giữa đoàn.");
                }

                hm.Add(loi.Count > 0
                    ? HangMucKiemTraDTO.Loi(tenCatNoi, string.Join("\n", loi))
                    : HangMucKiemTraDTO.Dat(tenCatNoi,
                        $"{string.Join(" · ", dong)}. Toa nối dọc đường cần khám xe, thử hãm tại ga nối."));
            }

            return kq;
        }

        // ---------------------------------------------------------------------
        // 1. Dau may keo chinh: ton tai, khong bao duong, khong chong gio, co mat o ga di
        // ---------------------------------------------------------------------
        private static void KiemTraDauMayChinh(BoiCanhLapTauDTO bc, DauMayLapTauDTO? dm,
                                               List<DiemDungHanhTrinhDTO> hanhTrinh, List<HangMucKiemTraDTO> hm)
        {
            const string ten = "Đầu máy kéo chính";
            if (dm == null)
            {
                hm.Add(HangMucKiemTraDTO.Loi(ten, "Chưa chọn đầu máy kéo cho đoàn tàu."));
                return;
            }
            if (KhongDuocVanDung(dm.TrangThai))
            {
                hm.Add(HangMucKiemTraDTO.Loi(ten, $"{dm.SoHieuDauMay} {MoTaKhongVanDung(dm.TrangThai)}, không được đưa vào vận dụng."));
                return;
            }

            var trung = ChuyenTrungGio(dm, bc);
            if (trung.Count > 0)
            {
                hm.Add(HangMucKiemTraDTO.Loi(ten, string.Join("\n", trung.Select(c =>
                    $"{dm.SoHieuDauMay} đang {(c.VaiTro == "DAY" ? "đẩy" : "kéo")} {c.SoHieuMacTau} ({c.KhungGio}), chồng giờ với chuyến này."))));
                return;
            }

            // Dau may phai co mat o ga xuat phat: chuyen truoc do ket thuc o dau?
            string gaDiCode = hanhTrinh.Count > 0 ? hanhTrinh[0].MaGaCode : bc.MaGaDiCode;
            string gaDiTen = hanhTrinh.Count > 0 ? hanhTrinh[0].TenGa : bc.TenGaDi;
            var truoc = dm.ChuyenKhac
                .Where(c => c.TrangThai != "DA_HUY" && c.GioVeDichKH <= bc.GioXuatPhatKH)
                .MaxBy(c => c.GioVeDichKH);

            string coBan = $"{dm.SoHieuDauMay} (dòng {dm.MaDongCode}) sức kéo {dm.SucKeoToiDaTan:N0} tấn";
            if (truoc != null && truoc.GaCuoiCode.Length > 0 && truoc.GaCuoiCode != gaDiCode)
            {
                hm.Add(HangMucKiemTraDTO.CanhBao(ten,
                    $"{coBan}. Chuyến trước ({truoc.SoHieuMacTau}) kết thúc tại {truoc.GaCuoiTen} lúc {truoc.GioVeDichKH:HH:mm dd/MM}, " +
                    $"không ở ga xuất phát {gaDiTen}: cần điều đầu máy chạy đơn về trước giờ tàu chạy."));
            }
            else if (truoc != null)
            {
                double gioNghi = (bc.GioXuatPhatKH - truoc.GioVeDichKH).TotalHours;
                hm.Add(HangMucKiemTraDTO.Dat(ten,
                    $"{coBan}. Về {gaDiTen} sau chuyến {truoc.SoHieuMacTau} lúc {truoc.GioVeDichKH:HH:mm dd/MM}, trước giờ xuất phát {gioNghi:N1} giờ."));
            }
            else
            {
                hm.Add(HangMucKiemTraDTO.Dat(ten, $"{coBan}, không trùng chuyến nào trong khung giờ."));
            }
        }

        // ---------------------------------------------------------------------
        // 2. Dau may day (tuy chon) + nhac khi hanh trinh qua khu gian can may day
        // ---------------------------------------------------------------------
        private static void KiemTraDauMayDay(BoiCanhLapTauDTO bc, PhuongAnLapTauDTO pa, DauMayLapTauDTO? dmChinh,
                                             DauMayLapTauDTO? dmDay, List<HangMucKiemTraDTO> hm)
        {
            const string ten = "Đầu máy đẩy";
            string dsKhuGian = string.Join(", ", bc.KhuGianCanDay.Select(k => k.Ten));

            if (pa.MaDauMayDay == null)
            {
                if (bc.KhuGianCanDay.Count > 0)
                    hm.Add(HangMucKiemTraDTO.CanhBao(ten,
                        $"Hành trình qua khu gian cần đầu máy đẩy: {dsKhuGian}. Chưa biên chế máy đẩy, cần xác nhận đoàn tàu vượt dốc được với một đầu máy."));
                return;
            }
            if (dmDay == null) return; // loi cau truc da bao o tren

            if (dmChinh != null && dmDay.MaDauMay == dmChinh.MaDauMay)
            {
                hm.Add(HangMucKiemTraDTO.Loi(ten, "Đầu máy đẩy phải khác đầu máy kéo chính."));
                return;
            }
            if (KhongDuocVanDung(dmDay.TrangThai))
            {
                hm.Add(HangMucKiemTraDTO.Loi(ten, $"{dmDay.SoHieuDauMay} {MoTaKhongVanDung(dmDay.TrangThai)}, không được đưa vào vận dụng."));
                return;
            }

            var trung = ChuyenTrungGio(dmDay, bc);
            if (trung.Count > 0)
            {
                hm.Add(HangMucKiemTraDTO.Loi(ten, string.Join("\n", trung.Select(c =>
                    $"{dmDay.SoHieuDauMay} đang {(c.VaiTro == "DAY" ? "đẩy" : "kéo")} {c.SoHieuMacTau} ({c.KhungGio}), chồng giờ với chuyến này."))));
                return;
            }

            hm.Add(HangMucKiemTraDTO.Dat(ten, bc.KhuGianCanDay.Count > 0
                ? $"{dmDay.SoHieuDauMay} đẩy hỗ trợ qua {dsKhuGian}."
                : $"{dmDay.SoHieuDauMay} biên chế làm máy đẩy (hành trình không qua khu gian bắt buộc máy đẩy)."));
        }

        private static List<ChuyenSuDungDTO> ChuyenTrungGio(DauMayLapTauDTO dm, BoiCanhLapTauDTO bc)
            => dm.ChuyenKhac
                 .Where(c => ChuyenConHieuLuc(c.TrangThai) && ChongGio(c.GioXuatPhatKH, c.GioVeDichKH, bc.GioXuatPhatKH, bc.GioVeDichKH))
                 .OrderBy(c => c.GioXuatPhatKH)
                 .ToList();

        public static decimal TaiTrongTruc(ToaXeLapTauDTO toa)
            => toa.SoTruc > 0 ? Math.Round(toa.TongTrongLuongTan / toa.SoTruc, 2) : 0m;

        // Nhom toa cat / noi phai lien khoi va nam o cuoi doan hoac ngay sau dau may
        public static string XacDinhViTriNhom(IReadOnlyList<int> doan, IReadOnlyCollection<int> nhom)
        {
            var viTri = nhom.Select(m => IndexOf(doan, m)).Where(i => i >= 0).OrderBy(i => i).ToList();
            if (viTri.Count == 0) return ViTriNhomToa.Khong;
            if (viTri[^1] - viTri[0] + 1 != viTri.Count) return ViTriNhomToa.Sai;
            if (viTri[^1] == doan.Count - 1) return ViTriNhomToa.Cuoi;
            if (viTri[0] == 0) return ViTriNhomToa.Dau;
            return ViTriNhomToa.Sai;
        }

        // Toa hang phai lien khoi o dau hoac cuoi doan, khong xen giua toa khach
        private static (bool Dat, string MoTa) KiemTraToaHang(IReadOnlyList<int> doan, HashSet<int> maToaHang, BoiCanhLapTauDTO bc)
        {
            int n = doan.Count;
            var viTriHang = Enumerable.Range(0, n).Where(i => maToaHang.Contains(doan[i])).ToList();

            if (viTriHang.Count == 0) return (true, "Đoàn tàu chỉ gồm toa khách.");
            if (viTriHang.Count == n) return (true, "Đoàn tàu hàng thuần, không có toa khách.");

            bool lienKhoi = viTriHang[^1] - viTriHang[0] + 1 == viTriHang.Count;
            bool oDauHoacCuoi = viTriHang[0] == 0 || viTriHang[^1] == n - 1;
            if (lienKhoi && oDauHoacCuoi)
                return (true, $"Toa hàng xếp liền khối {(viTriHang[0] == 0 ? "ngay sau đầu máy" : "ở cuối đoàn")}, không xen giữa toa khách.");

            var toaXen = viTriHang
                .Where(i => Enumerable.Range(0, i).Any(j => !maToaHang.Contains(doan[j])) &&
                            Enumerable.Range(i + 1, n - i - 1).Any(j => !maToaHang.Contains(doan[j])))
                .Select(i => bc.TimToa(doan[i])?.SoHieuToaXe ?? $"#{doan[i]}")
                .ToList();

            string moTa = toaXen.Count > 0
                ? $"Toa hàng {string.Join(", ", toaXen)} đang xen giữa các toa khách."
                : "Toa hàng đang bị tách thành nhiều cụm.";
            return (false, $"{moTa} Cần dồn toa hàng thành một khối ở đầu hoặc cuối đoàn.");
        }

        private static string DanhSachSoHieu(IEnumerable<int> maToa, BoiCanhLapTauDTO bc)
            => string.Join(", ", maToa.Select(m => bc.TimToa(m)?.SoHieuToaXe ?? $"#{m}"));

        private static int IndexOf(IReadOnlyList<int> ds, int giaTri)
        {
            for (int i = 0; i < ds.Count; i++)
                if (ds[i] == giaTri) return i;
            return -1;
        }

        private sealed record ToaTrongPhuongAn(ToaXeLapTauDTO Toa, int Noi, int Cat);
    }
}
