using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.ViewModels.KyThuat
{
    // =========================================================================
    // BAO TRI - TAB 5: CHI PHI & HIEU QUA (theo tuyen duong / mac tau)
    //
    // Khong co bang rieng trong CSDL: tong hop ChiPhi da them o 3 tab truoc
    // (KhamXeHienThi, CapNhienLieuHienThi, BaoDuongHienThi) TRONG KY dang chon,
    // roi phan bo theo TUYEN ma phuong tien dang phuc vu (tra qua
    // vanhanh.DoanTau.SoHieuMacTau).
    // Doanh thu la DU LIEU MAU TINH minh hoa, CHUA noi du lieu ban ve / hang hoa
    // that (phan hanh khach / hang hoa cua A Ba chua lam xong).
    // =========================================================================

    // Mot tuyen duong / nhom mac tau mau. Mot mac tau chi thuoc dung 1 tuyen.
    // DoanhThuThangMauVnd: doanh thu mau cho 30 ngay, quy doi tuyen tinh theo so ngay cua ky.
    public record TuyenDuongMau(string MaTuyen, string TenTuyen, string HanhTrinh,
                                IReadOnlyList<string> MacTauPhucVu, decimal DoanhThuThangMauVnd, string GhiChu)
    {
        public string NhanMacTau => string.Join(" / ", MacTauPhucVu);
    }

    // Mot khoan chi phi da chi, gan voi 1 phieu cu the (kham xe / cap dau / bao duong).
    // SoTien = phan da phan bo cho tuyen; SoTienGoc = toan bo so tien tren phieu.
    public record KhoanChiPhiDaChi(string Nguon, string MaPhieu, string MoTa, decimal SoTien, decimal SoTienGoc,
                                   string PhanBo, DateTime ThoiDiem)
    {
        public SymbolRegular BieuTuong => Nguon switch
        {
            "KHAM_XE" => SymbolRegular.ClipboardTaskListLtr24,
            "NHIEN_LIEU" => SymbolRegular.GasPump24,
            _ => SymbolRegular.WrenchScrewdriver24
        };

        public string NhanPhanBo => PhanBo.Length == 0 ? "" : $" · phân bổ {PhanBo} của {SoTienGoc:N0} đ";
    }

    // Mot khoan chi phi du phong (phuong tien sap / da den han bao duong)
    public record KhoanChiPhiDuPhong(string SoHieuPhuongTien, string Cap, string TrangThai, string MoTa,
                                     decimal SoTienDuKien)
    {
        public string MauChu => TrangThai == "QUA_HAN" ? "#B91C1C" : "#B45309";
    }

    public class TuyenChiPhiHienThi
    {
        public TuyenDuongMau Tuyen { get; init; } = null!;
        public int SoNgayKy { get; init; }
        public List<KhoanChiPhiDaChi> ChiTietDaChi { get; init; } = new();
        public List<KhoanChiPhiDuPhong> ChiTietDuPhong { get; init; } = new();
        public bool DangChon { get; set; }

        public decimal ChiPhiDaChi => ChiTietDaChi.Sum(c => c.SoTien);
        public decimal ChiPhiDuPhong => ChiTietDuPhong.Sum(c => c.SoTienDuKien);
        public decimal TongChiPhiBaoTri => ChiPhiDaChi + ChiPhiDuPhong;
        public decimal DoanhThu => Math.Round(Tuyen.DoanhThuThangMauVnd * SoNgayKy / 30m, 0);
        public decimal LoiNhuan => DoanhThu - TongChiPhiBaoTri;
        public decimal BienLoiNhuan => DoanhThu > 0 ? Math.Round(LoiNhuan / DoanhThu * 100m, 1) : 0m;

        public bool CoLai => LoiNhuan >= 0;

        // Chua co khoan chi phi nao trong ky => khong du co so de ket luan
        public bool CoDuLieuChiPhi => ChiTietDaChi.Count + ChiTietDuPhong.Count > 0;

        // < 10% (ke ca am) => can nhac dung/thu hep; > 30% => mo rong; con lai => duy tri
        public string MaKhuyenNghi => !CoDuLieuChiPhi ? "CHUA_DU"
                                     : BienLoiNhuan < ChiPhiHieuQua.NguongCanNhac ? "THU_HEP"
                                     : BienLoiNhuan > ChiPhiHieuQua.NguongMoRong ? "MO_RONG"
                                     : "DUY_TRI";

        public bool CanXemXet => MaKhuyenNghi == "THU_HEP";

        public string NhanKhuyenNghi => MaKhuyenNghi switch
        {
            "THU_HEP" => "ĐỀ XUẤT: CÂN NHẮC DỪNG / THU HẸP TUYẾN",
            "MO_RONG" => "ĐỀ XUẤT: MỞ RỘNG THÊM CHUYẾN",
            "CHUA_DU" => "CHƯA ĐỦ DỮ LIỆU CHI PHÍ TRONG KỲ ĐỂ ĐỀ XUẤT",
            _ => "DUY TRÌ, THEO DÕI THÊM"
        };

        // Nhan ngan tren the tuyen: bien loi nhuan, hoac "—" khi chua du du lieu
        public string NhanBien => CoDuLieuChiPhi ? $"{BienLoiNhuan:N1}%" : "—";

        public string MauKhuyenNghi => MaKhuyenNghi switch
        {
            "THU_HEP" => "#B91C1C",
            "MO_RONG" => "#15803D",
            "CHUA_DU" => "#475569",
            _ => "#B45309"
        };
        public string NenKhuyenNghi => MaKhuyenNghi switch
        {
            "THU_HEP" => "#FEF2F2",
            "MO_RONG" => "#F0FDF4",
            "CHUA_DU" => "#F8FAFC",
            _ => "#FFFBEB"
        };
        public string VienKhuyenNghi => MaKhuyenNghi switch
        {
            "THU_HEP" => "#FECACA",
            "MO_RONG" => "#BBF7D0",
            "CHUA_DU" => "#E2E8F0",
            _ => "#FDE68A"
        };

        public string MauLoiNhuan => CoLai ? "#15803D" : "#B91C1C";
        public string MauVien => DangChon ? "#003B73" : "#E2E8F0";
        public double DoDayVien => DangChon ? 2 : 1;

        public SymbolRegular BieuTuongKhuyenNghi => MaKhuyenNghi switch
        {
            "THU_HEP" => SymbolRegular.Warning24,
            "MO_RONG" => SymbolRegular.ArrowTrendingLines24,
            "CHUA_DU" => SymbolRegular.QuestionCircle24,
            _ => SymbolRegular.Info24
        };

        // Thu tu tren danh sach: can xem xet truoc, chua du du lieu sau cung
        public int ThuTuSapXep => MaKhuyenNghi switch { "THU_HEP" => 0, "DUY_TRI" => 1, "MO_RONG" => 2, _ => 3 };
    }

    // Phan cua mot tuyen trong mot dau may: SoDoan / TongSoDoan doan tau ma dau may keo
    public record PhanBoTuyen(TuyenDuongMau Tuyen, int SoDoan, int TongSoDoan)
    {
        public decimal TyLe => (decimal)SoDoan / TongSoDoan;
        public string Nhan => SoDoan == TongSoDoan ? "" : $"{SoDoan}/{TongSoDoan}";
    }

    // =========================================================================
    // DU LIEU MAU + TONG HOP
    // =========================================================================
    public static class ChiPhiHieuQua
    {
        public const decimal NguongCanNhac = 10m;   // % bien loi nhuan
        public const decimal NguongMoRong = 30m;
        public const int SoNgayKyMacDinh = 7;

        // 5 tuyen mau (mac tau -> tuyen). Doanh thu 30 ngay la DU LIEU MAU MINH HOA, CHUA
        // noi du lieu ban ve / van tai hang hoa that (phan cua A Ba chua xong). Chon so de
        // ky 7 ngay mac dinh ra du 4 muc: mo rong / duy tri / lai mong / lo.
        public static readonly IReadOnlyList<TuyenDuongMau> Tuyen = new[]
        {
            new TuyenDuongMau("T1", "Thống Nhất SE1/SE2", "Hà Nội – Sài Gòn",
                new[] { "SE1", "SE2" }, 1_800_000_000m,
                "Tuyến trục chính, tần suất chạy dày — doanh thu mẫu minh họa."),
            new TuyenDuongMau("T2", "SE3/SE4 (HN–ĐN/SG)", "Hà Nội – Đà Nẵng / Sài Gòn",
                new[] { "SE3", "SE4" }, 920_000_000m,
                "Doanh thu mẫu minh họa, chưa nối dữ liệu bán vé/hàng hoá thật."),
            new TuyenDuongMau("T3", "SE19 (HN–ĐN nhanh)", "Hà Nội – Đà Nẵng",
                new[] { "SE19" }, 205_000_000m,
                "Tuyến ngắn, tần suất thấp — doanh thu mẫu minh họa."),
            new TuyenDuongMau("T4", "NA1 (Hàng hoá HN–Vinh)", "Hà Nội – Vinh",
                new[] { "NA1" }, 360_000_000m,
                "Tàu hàng, doanh thu mẫu thấp hơn tàu khách cùng cự ly."),
            new TuyenDuongMau("T5", "SE7/SE8 (HN–Nha Trang)", "Hà Nội – Nha Trang",
                Array.Empty<string>(), 1_100_000_000m,
                "Tuyến mẫu bổ sung, hiện chưa có đoàn tàu nào trong dữ liệu mẫu Bảo trì " +
                "đang phục vụ tuyến này nên chưa có chi phí bảo trì ghi nhận / dự phóng.")
        };

        public static TuyenDuongMau? TimTuyenTheoMacTau(string soHieuMacTau)
            => Tuyen.FirstOrDefault(t => t.MacTauPhucVu.Contains(soHieuMacTau));

        // Chi phi cua mot dau may duoc CHIA cho cac tuyen theo so doan tau (chinh / day) ma no
        // keo tren moi tuyen, de tong chi phi cac tuyen cong lai dung bang so tien thuc chi.
        // Vd D19E-901 keo SE1, SE3, SE19 => moi tuyen T1, T2, T3 chiu 1/3.
        public static List<PhanBoTuyen> PhanBoDauMay(string soHieuDauMay)
        {
            var tuyenCuaDoan = DanhMucMauBaoTri.DoanTau
                .Where(d => d.SoHieuDauMayChinh == soHieuDauMay || d.SoHieuDauMayDay == soHieuDauMay)
                .Select(d => TimTuyenTheoMacTau(d.SoHieuMacTau))
                .Where(t => t != null)
                .Select(t => t!)
                .ToList();

            return tuyenCuaDoan.GroupBy(t => t)
                               .Select(g => new PhanBoTuyen(g.Key, g.Count(), tuyenCuaDoan.Count))
                               .ToList();
        }

        // ---------------------------------------------------------------------
        // Tong hop chi phi theo tuyen tu du lieu 3 tab truoc (da tron thay doi tam).
        // Chi phi da chi: chi lay phieu co thoi diem trong [tuNgay, bay gio].
        // Du phong: trang thai bao duong HIEN TAI, khong phu thuoc ky.
        // ---------------------------------------------------------------------
        public static List<TuyenChiPhiHienThi> TaoTongHop(
            List<KhamXeHienThi> khamXe, List<CapNhienLieuHienThi> capDau, List<BaoDuongHienThi> baoDuong,
            int soNgayKy, DateTime tuNgay)
        {
            var theoTuyen = Tuyen.ToDictionary(t => t.MaTuyen, t => new TuyenChiPhiHienThi { Tuyen = t, SoNgayKy = soNgayKy });

            void ThemDaChi(PhanBoTuyen pb, string nguon, string maPhieu, string moTa, decimal soTien, DateTime thoiDiem)
            {
                if (soTien <= 0 || thoiDiem < tuNgay) return;
                theoTuyen[pb.Tuyen.MaTuyen].ChiTietDaChi.Add(
                    new KhoanChiPhiDaChi(nguon, maPhieu, moTa, soTien * pb.TyLe, soTien, pb.Nhan, thoiDiem));
            }

            // 1) Kham xe: gan truc tiep theo mac tau cua doan tau (khong chia)
            foreach (var bb in khamXe)
            {
                var t = TimTuyenTheoMacTau(bb.SoHieuMacTau);
                if (t == null) continue;
                ThemDaChi(new PhanBoTuyen(t, 1, 1), "KHAM_XE", bb.MaBienBan,
                    $"Khám xe {bb.SoHieuMacTau} · {(bb.DuDieuKienXuatBen ? "đạt" : "không đạt")}",
                    bb.ChiPhi, bb.ThoiDiemKham);
            }

            // 2) Nhien lieu: chia theo so doan tau dau may keo tren tung tuyen
            foreach (var nl in capDau)
            {
                foreach (var pb in PhanBoDauMay(nl.SoHieuDauMay))
                    ThemDaChi(pb, "NHIEN_LIEU", nl.MaPhieu,
                        $"Cấp dầu {nl.SoHieuDauMay} · {nl.SoLitTraNap:N0} lít",
                        nl.ChiPhi, nl.ThoiDiemBomDau);
            }

            // 3) Bao duong: chi tinh cho DAU MAY (toa xe la doi toa dung chung, chua gan
            //    rieng cho tuyen nao trong du lieu mau hien tai)
            foreach (var bd in baoDuong.Where(x => x.LaDauMay))
            {
                foreach (var pb in PhanBoDauMay(bd.SoHieuPhuongTien))
                    ThemDaChi(pb, "BAO_DUONG", bd.MaPhieu,
                        $"Bảo dưỡng {bd.CapBaoDuong} · {bd.SoHieuPhuongTien}",
                        bd.ChiPhi, bd.ThoiDiemHoanThanh);
            }

            // 4) Du phong: dau may nao dang SAP_TOI / QUA_HAN cap ke tiep -> uoc tinh theo
            //    chi phi tham khao cua cap do, chia cho cac tuyen nhu nhien lieu
            foreach (var dm in DanhMucMauBaoTri.DauMay)
            {
                var pt = DanhMucMauBaoTri.TimPhuongTien($"DAU_MAY:{dm.MaDauMay}");
                if (pt == null) continue;

                var lichSu = baoDuong.Where(x => x.KhoaPhuongTien == pt.Khoa);
                var chuY = ChuKyBaoDuong.CapCanChuY(ChuKyBaoDuong.TinhTienDo(pt, lichSu));
                if (chuY == null || chuY.TrangThai is not ("QUA_HAN" or "SAP_TOI")) continue;

                decimal uocTinh = ChuKyBaoDuong.ChiPhiThamKhao(chuY.Cap);
                foreach (var pb in PhanBoDauMay(dm.SoHieuDauMay))
                {
                    string phanBo = pb.Nhan.Length == 0 ? "" : $" · phân bổ {pb.Nhan} của {uocTinh:N0} đ";
                    theoTuyen[pb.Tuyen.MaTuyen].ChiTietDuPhong.Add(new KhoanChiPhiDuPhong(
                        dm.SoHieuDauMay, chuY.Cap, chuY.TrangThai,
                        $"{dm.SoHieuDauMay} {chuY.CumTrangThai} ({chuY.ConLai}) · ước theo chi phí {chuY.Cap} tham khảo{phanBo}",
                        uocTinh * pb.TyLe));
                }
            }

            return theoTuyen.Values
                .OrderBy(t => t.ThuTuSapXep)
                .ThenBy(t => t.BienLoiNhuan)
                .ToList();
        }
    }
}
