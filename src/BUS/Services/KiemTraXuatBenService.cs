using DAL.Repositories;
using DTO.BaoTri;
using DTO.NhanSu;

namespace BUS.Services
{
    // =========================================================================
    // KIEM TRA XUAT BEN - hop dong KE_HOACH_BACK_END.md §7.2 (Dat giao, A Ba goi).
    //
    //   var kq = new KiemTraXuatBenService().KiemTra(maChuyenTau);
    //   if (!kq.DuocXuatBen) { ThongBaoDialog.CanhBao(kq.MoTaChan, ...); return; }
    //
    // A Ba goi khi doi trang thai chuyen sang SAN_SANG / DANG_CHAY. Q11 (Dat chot 24/10): Sprint 2
    // CHAN CUNG, khong co nut vuot (chua co dang nhap nen chua phan biet duoc quan ly).
    //
    // Chan  : chua co doan tau; bien ban moi nhat chua co / khong dat / lap truoc lan sua doan tau;
    //         chua duyet an toan; dau may qua han bao duong; kip chua phan cong / chang dau chua co kip /
    //         kip co nguoi khong du dieu kien (khong dat kiem tra len ban, het han kham, trung gio...).
    // Luu y : tieu hao lan cap gan nhat vuot dinh muc; sap toi han bao duong; chang sau chua co kip;
    //         han kham suc khoe con <= 30 ngay, chua kiem tra len ban.
    //
    // DanhGia la ham thuan: tab Canh bao cua trang Bao tri dung chung de hien "san sang xuat ben" -
    // man hinh va luat chan cua A Ba khong the lech nhau.
    // =========================================================================
    public class KiemTraXuatBenService
    {
        private readonly BaoTriRepository _baoTri = new();
        private readonly NhanSuService _nhanSu = new();

        public KetQuaXuatBen KiemTra(int maChuyenTau)
        {
            DuLieuBaoTri bt;
            try
            {
                bt = _baoTri.LayDuLieu();
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                // An toan truoc: khong doc duoc thi khong cho xuat ben, noi ro ly do
                return new KetQuaXuatBen
                {
                    MaChuyenTau = maChuyenTau,
                    CacMuc = { new("DOAN_TAU", "Dữ liệu bảo trì", $"Không đọc được CSDL ({DichLoiSqlPhuongTien.ThongDiep(ex)})", "CHAN") }
                };
            }

            DuLieuNhanSu? ns = _nhanSu.LayDuLieu(out var dl, out _) ? dl : null;
            return DanhGia(bt, ns, maChuyenTau);
        }

        // ns == null: khong doc duoc du lieu nhan su -> muc kip lai bi chan
        public static KetQuaXuatBen DanhGia(DuLieuBaoTri bt, DuLieuNhanSu? ns, int maChuyenTau)
        {
            var muc = new List<MucXuatBenDTO>();
            var dt = bt.TimDoanTauTheoChuyen(maChuyenTau);

            if (dt == null)
            {
                muc.Add(new("DOAN_TAU", "Đoàn tàu", "Chuyến chưa lập đoàn tàu", "CHAN"));
            }
            else
            {
                muc.Add(MucKhamXe(bt, dt));
                muc.Add(dt.DaDuyetAnToan
                    ? new MucXuatBenDTO("DUYET", "Duyệt an toàn", "Đã duyệt", "DAT")
                    : new MucXuatBenDTO("DUYET", "Duyệt an toàn", "Chưa duyệt", "CHAN"));

                foreach (var (ma, vaiTro) in new[] { ((int?)dt.MaDauMayChinh, "chính"), (dt.MaDauMayDay, "đẩy") })
                {
                    var dm = ma.HasValue ? bt.TimDauMay(ma.Value) : null;
                    if (dm == null) continue;
                    muc.Add(MucBaoDuong(bt, dm, vaiTro));
                    muc.Add(MucNhienLieu(bt, dm));
                }
            }

            muc.AddRange(MucKipLai(ns, maChuyenTau));
            return new KetQuaXuatBen { MaChuyenTau = maChuyenTau, CacMuc = muc };
        }

        // Dieu kien duyet an toan (UC-BT-03): chi phan ky thuat. Kip lai xet luc xuat ben.
        public static KetQuaDuyetAnToan DieuKienDuyet(DuLieuBaoTri bt, DoanTauBaoTriDTO dt)
        {
            string? lyDo = dt.DaXuatPhat ? $"Chuyến {dt.SoHieuMacTau} đã {(dt.DaKetThuc ? "kết thúc" : "xuất phát")}, không duyệt nữa."
                         : dt.DaDuyetAnToan ? $"Đoàn tàu {dt.SoHieuMacTau} đã được duyệt an toàn."
                         : null;

            var muc = new List<MucXuatBenDTO> { MucKhamXe(bt, dt) };
            foreach (var (ma, vaiTro) in new[] { ((int?)dt.MaDauMayChinh, "chính"), (dt.MaDauMayDay, "đẩy") })
            {
                var dm = ma.HasValue ? bt.TimDauMay(ma.Value) : null;
                if (dm != null) muc.Add(MucBaoDuong(bt, dm, vaiTro));
            }
            return new KetQuaDuyetAnToan { CacMuc = muc, LyDoKhongDuyet = lyDo };
        }

        // ---------------------------------------------------------------------
        // Tung muc
        // ---------------------------------------------------------------------

        public static MucXuatBenDTO MucKhamXe(DuLieuBaoTri bt, DoanTauBaoTriDTO dt)
        {
            const string ten = "Khám xe kỹ thuật";
            var bb = bt.BienBanMoiNhat(dt.MaDoanTau);
            if (bb == null) return new("KHAM_XE", ten, "Chưa có biên bản khám xe", "CHAN");
            if (!QuyTacBaoTri.DuDieuKienXuatBen(bb))
                return new("KHAM_XE", ten, $"Không đạt · {QuyTacBaoTri.TomTatLoi(bb)}", "CHAN");
            if (dt.ThoiDiemSuaDoanTau.HasValue && dt.ThoiDiemSuaDoanTau.Value > bb.ThoiDiemKham)
                return new("KHAM_XE", ten,
                    $"BB-{bb.MaKhamXe:0000} lập trước lần sửa đoàn tàu lúc {dt.ThoiDiemSuaDoanTau:HH:mm dd/MM} — cần khám lại", "CHAN");
            return new("KHAM_XE", ten, $"Đạt 4/4 · BB-{bb.MaKhamXe:0000} lúc {bb.ThoiDiemKham:HH:mm dd/MM}", "DAT");
        }

        public static MucXuatBenDTO MucBaoDuong(DuLieuBaoTri bt, DauMayBaoTriDTO dm, string vaiTro)
        {
            string ten = $"Bảo dưỡng {dm.SoHieuDauMay} ({vaiTro})";
            if (dm.TrangThai is "BAO_DUONG" or PhuongTienService.NgungKhaiThac)
                return new("BAO_DUONG", ten, dm.TrangThai == "BAO_DUONG" ? "Đầu máy đang bảo dưỡng" : "Đầu máy đã ngừng khai thác", "CHAN");

            var t = QuyTacBaoTri.CapCanChuY(QuyTacBaoTri.TienDoDauMay(bt, dm));
            if (t == null) return new("BAO_DUONG", ten, "Chưa có dữ liệu", "LUU_Y");
            return t.TrangThai switch
            {
                "QUA_HAN" => new("BAO_DUONG", ten, $"Quá hạn {t.Cap} · {MoTaConLai(t)}", "CHAN"),
                "SAP_TOI" => new("BAO_DUONG", ten, $"Sắp tới hạn {t.Cap} · {MoTaConLai(t)}", "LUU_Y"),
                _ => new("BAO_DUONG", ten, $"Trong chu kỳ · {t.Cap} {MoTaConLai(t)}", "DAT")
            };
        }

        public static MucXuatBenDTO MucNhienLieu(DuLieuBaoTri bt, DauMayBaoTriDTO dm)
        {
            string ten = $"Nhiên liệu {dm.SoHieuDauMay}";
            var nl = bt.CapDauGanNhat(dm.MaDauMay, 1).FirstOrDefault();
            if (nl == null) return new("NHIEN_LIEU", ten, "Chưa có phiếu cấp dầu", "DAT");

            decimal tyLe = QuyTacBaoTri.TinhTyLe(nl.SuatTieuHaoSFC, dm.DinhMucSFC);
            return QuyTacBaoTri.XepMuc(tyLe) switch
            {
                "VUOT" => new("NHIEN_LIEU", ten, $"Lần cấp gần nhất vượt định mức {tyLe - 100m:N1}%", "LUU_Y"),
                "SAT" => new("NHIEN_LIEU", ten, $"Sát định mức · {tyLe:N1}%", "DAT"),
                _ => new("NHIEN_LIEU", ten, $"{tyLe:N0}% định mức", "DAT")
            };
        }

        public static List<MucXuatBenDTO> MucKipLai(DuLieuNhanSu? ns, int maChuyenTau)
        {
            const string ten = "Kíp lái";
            if (ns == null) return new() { new("KIP_LAI", ten, "Không đọc được dữ liệu nhân sự", "CHAN") };

            var chuyen = ns.TimChuyen(maChuyenTau);
            if (chuyen == null) return new() { new("KIP_LAI", ten, "Chưa phân công kíp lái", "CHAN") };

            var tt = DanhGiaKipLai.DanhGiaChuyen(ns, chuyen);
            if (tt.Kip.Count == 0) return new() { new("KIP_LAI", ten, "Chưa phân công kíp lái", "CHAN") };

            var ds = new List<MucXuatBenDTO>();
            string Chang(DoanPhuKip d) => $"{chuyen.LichDung[d.TuViTri].TenGa} → {chuyen.LichDung[d.DenViTri].TenGa}";

            foreach (var d in tt.Doan.Where(d => d.Kip == null))
                ds.Add(d.TuViTri == 0
                    ? new MucXuatBenDTO("KIP_LAI", ten, $"Chặng đầu {Chang(d)} chưa có kíp", "CHAN")
                    : new MucXuatBenDTO("KIP_LAI", ten, $"Chặng {Chang(d)} chưa có kíp", "LUU_Y"));
            foreach (var d in tt.Doan.Where(d => d.Chong))
                ds.Add(new("KIP_LAI", ten, $"Nhiều kíp cùng chạy chặng {Chang(d)}", "CHAN"));

            foreach (var k in tt.Kip.Where(k => k.PhanCong.TrangThai != PhanCongKipHienThi.HoanThanh))
            {
                foreach (var v in k.VanDe.Where(v => v.MucDo == MucDoVanDe.NghiemTrong))
                    ds.Add(new("KIP_LAI", $"{ten} · {k.TenKip}", v.NoiDung, "CHAN"));
                foreach (var v in k.VanDe.Where(v => v.MucDo == MucDoVanDe.CanhBao))
                    ds.Add(new("KIP_LAI", $"{ten} · {k.TenKip}", v.NoiDung, "LUU_Y"));
            }

            if (ds.Count == 0)
                ds.Add(new("KIP_LAI", ten, $"Đủ {tt.Kip.Count} kíp phủ hành trình", "DAT"));
            return ds;
        }

        // "còn 1,500 km" / "quá 60,000 km" / "còn 12 ngày"
        public static string MoTaConLai(TienDoBaoDuongDTO t)
        {
            if (!t.ConLai.HasValue) return "chưa có mốc";
            string donVi = t.TheoKm ? "km" : "ngày";
            return t.ConLai.Value >= 0 ? $"còn {t.ConLai.Value:N0} {donVi}" : $"quá {-t.ConLai.Value:N0} {donVi}";
        }
    }
}
