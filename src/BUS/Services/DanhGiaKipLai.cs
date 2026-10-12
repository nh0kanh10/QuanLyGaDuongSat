using System.Text.RegularExpressions;
using DTO.NhanSu;

namespace BUS.Services
{
    // =========================================================================
    // DOI CHIEU DIEU KIEN PHAN CONG KIP LAI (R2..R12 - PHAN_TICH_NHAN_SU_KIP_LAI.md)
    // Ham thuan (khong cham CSDL): dung chung cho NhanSuPage, PhanCongKipDialog,
    // KiemTraLenBanDialog, bieu do ca truc VA NhanSuService (chay lai truoc khi ghi)
    // - moi noi cung mot ket luan.
    // 11/10: chuyen tu GUI/ViewModels/KyThuat/DanhGiaKipLai.cs, khong doi luat.
    // =========================================================================
    public static class DanhGiaKipLai
    {
        // Ten dieu kien (giu ten cu cho GUI)
        public const string DkChucDanh = DieuKienKip.ChucDanh;
        public const string DkBangLai = DieuKienKip.BangLai;
        public const string DkHanKham = DieuKienKip.HanKham;
        public const string DkTrung = DieuKienKip.Trung;
        public const string DkNghi = DieuKienKip.Nghi;
        public const string DkKiemTra = DieuKienKip.KiemTra;
        public const string DkDonVi = DieuKienKip.DonVi;
        public const string DkTrangThai = DieuKienKip.TrangThai;

        public static string[] ThuTuDieuKien => DieuKienKip.ThuTu;

        // -----------------------------------------------------------------
        // Doi chieu 1 nguoi cho 1 vi tri tren 1 chang (R2 R3 R4 R6 R7 R8 R9 R10)
        // maPhanCongBoQua: kip dang sua (khong tu so voi chinh no)
        // -----------------------------------------------------------------
        public static List<DieuKienUngVien> DoiChieu(DuLieuNhanSu dl, NhanVienHienThi nv, string vaiTro,
                                                     ChuyenTauNhanSu chuyen, int maGaNhan, int maGaGiao,
                                                     int? maPhanCongBoQua)
        {
            var ds = new List<DieuKienUngVien>();
            DateTime gioNhan = chuyen.GioNhanBan(maGaNhan);
            DateTime gioGiao = chuyen.GioBanGiao(maGaGiao);
            string vt = QuyTacKipLai.TenVaiTro(vaiTro);

            // R2 chuc danh
            string chucDanh = QuyTacKipLai.ChucDanhYeuCau(vaiTro);
            bool dungChucDanh = string.Equals(nv.ChucDanh.Trim(), chucDanh, StringComparison.OrdinalIgnoreCase);
            ds.Add(new(DkChucDanh, dungChucDanh, MucDoVanDe.NghiemTrong, nv.ChucDanh,
                       dungChucDanh ? "" : $"chức danh \"{nv.ChucDanh}\" không đảm nhận được vị trí {vt.ToLower()}"));

            // R7 bang lai - dong dau may
            if (!QuyTacKipLai.CanBangLai(vaiTro))
                ds.Add(new(DkBangLai, null, MucDoVanDe.LuuY, "Không yêu cầu"));
            else if (string.IsNullOrEmpty(chuyen.MaDongDauMay))
                ds.Add(new(DkBangLai, false, MucDoVanDe.LuuY, "Chưa có đầu máy",
                           "chuyến chưa lập đoàn tàu, chưa đối chiếu được hạng bằng lái"));
            else if (string.Equals(nv.HangBangLai, chuyen.MaDongDauMay, StringComparison.OrdinalIgnoreCase))
                ds.Add(new(DkBangLai, true, MucDoVanDe.NghiemTrong, $"{nv.HangBangLai} = {chuyen.MaDongDauMay}"));
            else
                ds.Add(new(DkBangLai, false, MucDoVanDe.NghiemTrong, $"Bằng {nv.NhanBangLai} ≠ {chuyen.MaDongDauMay}",
                           $"hạng bằng {nv.NhanBangLai} không lái được đầu máy {chuyen.SoHieuDauMay}"));

            // R8 han kham suc khoe
            if (nv.HanKhamSucKhoe.Date < gioNhan.Date)
                ds.Add(new(DkHanKham, false, MucDoVanDe.NghiemTrong, $"Hết hạn {nv.HanKhamSucKhoe:dd/MM/yyyy}",
                           $"hết hạn khám sức khỏe từ {nv.HanKhamSucKhoe:dd/MM/yyyy}"));
            else if (nv.SapHetHanKham)
                ds.Add(new(DkHanKham, false, MucDoVanDe.CanhBao, nv.MoTaHanKham,
                           $"hạn khám sức khỏe {nv.HanKhamSucKhoe:dd/MM/yyyy}, {nv.MoTaHanKham.ToLower()}"));
            else
                ds.Add(new(DkHanKham, true, MucDoVanDe.NghiemTrong, $"Đến {nv.HanKhamSucKhoe:dd/MM/yyyy}"));

            // R3 trung khung gio chuyen (y nhu trigger). Cung chuyen: lai tau / phu lai chi 1 kip;
            // truong tau duoc di tiep chang sau cua chuyen minh (van o vi tri truong tau, khong chong chang)
            var kipKhac = dl.KipCuaNhanVien(nv.MaNhanVien).Where(p => p.MaPhanCong != maPhanCongBoQua).ToList();
            int viTriNhan = chuyen.ViTri(maGaNhan), viTriGiao = chuyen.ViTri(maGaGiao);
            var cungChuyen = kipKhac.FirstOrDefault(p => p.MaChuyenTau == chuyen.MaChuyenTau
                && !(vaiTro == QuyTacKipLai.TruongTau && p.MaTruongTau == nv.MaNhanVien
                     && (p.ViTriGiao <= viTriNhan || viTriGiao <= p.ViTriNhan)));
            var trungChuyen = kipKhac.FirstOrDefault(p => p.MaChuyenTau != chuyen.MaChuyenTau
                                                          && QuyTacKipLai.TrungKhungGio(p.Chuyen, chuyen));
            if (cungChuyen != null)
                ds.Add(new(DkTrung, false, MucDoVanDe.NghiemTrong, "Đã ở kíp khác",
                           $"đã thuộc kíp {cungChuyen.Chang} của chuyến này"));
            else if (trungChuyen != null)
            {
                var ct = trungChuyen.Chuyen;
                ds.Add(new(DkTrung, false, MucDoVanDe.NghiemTrong, $"Trùng {ct.SoHieuMacTau}",
                           $"đang thuộc kíp chuyến {ct.SoHieuMacTau} ({ct.GioXuatPhatKH:HH:mm dd/MM} – {ct.GioVeDichKH:HH:mm dd/MM}), " +
                           "khung giờ hai chuyến chồng nhau"));
            }
            else
                ds.Add(new(DkTrung, true, MucDoVanDe.NghiemTrong, "Không trùng"));

            // R4 nghi giua hai ca (chi xet cac ca khong trung chuyen)
            var caKhac = kipKhac.Where(p => p.MaChuyenTau != chuyen.MaChuyenTau
                                            && !QuyTacKipLai.TrungKhungGio(p.Chuyen, chuyen)).ToList();
            var chong = caKhac.FirstOrDefault(p => p.GioNhanBan < gioGiao && gioNhan < p.GioBanGiao);
            var truoc = dl.CaTruoc(nv.MaNhanVien, chuyen, gioNhan, maPhanCongBoQua);
            var sau = caKhac.Where(p => p.GioNhanBan >= gioGiao).OrderBy(p => p.GioNhanBan).FirstOrDefault();
            decimal? nghiTruoc = truoc == null ? null : QuyTacKipLai.SoGio(truoc.GioBanGiao, gioNhan);
            decimal? nghiSau = sau == null ? null : QuyTacKipLai.SoGio(gioGiao, sau.GioNhanBan);

            if (chong != null)
                ds.Add(new(DkNghi, false, MucDoVanDe.NghiemTrong, $"Trùng ca {chong.Chuyen.SoHieuMacTau}",
                           $"trùng giờ với ca {chong.Chuyen.SoHieuMacTau} {chong.Chang} ({chong.KhungGio})"));
            else if (nghiTruoc < QuyTacKipLai.SoGioNghiToiThieu)
                ds.Add(new(DkNghi, false, MucDoVanDe.NghiemTrong, $"Chỉ nghỉ {nghiTruoc:0.0} giờ",
                           $"chỉ nghỉ {nghiTruoc:0.0} giờ sau ca {truoc!.Chuyen.SoHieuMacTau} (bàn giao {truoc.GioBanGiao:HH:mm dd/MM}), " +
                           $"tối thiểu {QuyTacKipLai.SoGioNghiToiThieu:0} giờ"));
            else if (nghiSau < QuyTacKipLai.SoGioNghiToiThieu)
                ds.Add(new(DkNghi, false, MucDoVanDe.NghiemTrong, $"Ca sau cách {nghiSau:0.0} giờ",
                           $"ca sau ({sau!.Chuyen.SoHieuMacTau}, nhận ban {sau.GioNhanBan:HH:mm dd/MM}) chỉ cách {nghiSau:0.0} giờ"));
            else if (nghiTruoc.HasValue)
                ds.Add(new(DkNghi, true, MucDoVanDe.NghiemTrong, $"Nghỉ {nghiTruoc:0.0} giờ"));
            else
                ds.Add(new(DkNghi, true, MucDoVanDe.NghiemTrong, "Không có ca trước"));

            // R6 ket qua kiem tra len ban cho chuyen nay
            var kq = dl.KetQuaKiemTra(nv.MaNhanVien, chuyen.MaChuyenTau);
            if (kq.KhongDat)
                ds.Add(new(DkKiemTra, false, MucDoVanDe.NghiemTrong, "Không đạt",
                           $"không đạt kiểm tra lên ban lúc {kq.LanKhongDat!.ThoiDiemKiemTra:HH:mm dd/MM} " +
                           $"({kq.LanKhongDat.LyDoKhongDat}), phải thay người"));
            else if (kq.DaDat)
                ds.Add(new(DkKiemTra, true, MucDoVanDe.NghiemTrong, $"Đạt {kq.MoiNhat!.ThoiDiemKiemTra:HH:mm}"));
            else
                ds.Add(new(DkKiemTra, null, MucDoVanDe.LuuY, "Chưa kiểm tra"));

            // R10 don vi - chang (tham khao)
            var gaDongQuan = dl.GaDongQuan(nv.DonViChuQuan);
            if (gaDongQuan == null)
                ds.Add(new(DkDonVi, null, MucDoVanDe.LuuY, "—"));
            else if (gaDongQuan.MaGa == maGaNhan || gaDongQuan.MaGa == maGaGiao)
                ds.Add(new(DkDonVi, true, MucDoVanDe.LuuY, $"Đóng quân {gaDongQuan.TenGa}"));
            else
                ds.Add(new(DkDonVi, false, MucDoVanDe.LuuY, $"Đóng quân {gaDongQuan.TenGa}",
                           $"thuộc {nv.DonViChuQuan}, chặng không đi qua ga đóng quân {gaDongQuan.TenGa}"));

            // R9 trang thai ho so
            if (nv.TrangThai == NhanVienHienThi.DaNghiViec)
                ds.Add(new(DkTrangThai, false, MucDoVanDe.NghiemTrong, "Đã nghỉ việc", "đã nghỉ việc, phải thay người"));
            else
                ds.Add(new(DkTrangThai, true, MucDoVanDe.NghiemTrong, nv.NhanTrangThai));

            return ds;
        }

        // -----------------------------------------------------------------
        // R11 chang: ga nam tren lich dung, nhan truoc giao, khong chong chang kip khac
        // cua cung chuyen. null = hop le. (Truoc 11/10 nam trong PhanCongKipDialog.)
        // -----------------------------------------------------------------
        public static string? KiemTraChang(DuLieuNhanSu dl, ChuyenTauNhanSu chuyen, int maGaNhan, int maGaGiao,
                                           int? maPhanCongBoQua)
        {
            int i1 = chuyen.ViTri(maGaNhan), i2 = chuyen.ViTri(maGaGiao);
            if (i1 < 0 || i2 < 0) return "Ga nhận ban / bàn giao phải là ga dừng trên hành trình của chuyến.";
            if (i1 >= i2) return "Ga bàn giao phải nằm sau ga nhận ban trên hành trình.";

            var chong = dl.KipCuaChuyen(chuyen.MaChuyenTau)
                          .Where(p => p.MaPhanCong != maPhanCongBoQua)
                          .FirstOrDefault(p => p.ViTriNhan < i2 && i1 < p.ViTriGiao);
            if (chong != null)
                return $"Chặng chồng lên kíp {dl.SoThuTuKip(chong)} ({chong.Chang}). Mỗi đoạn hành trình chỉ do một kíp đảm nhận.";

            return null;
        }

        // -----------------------------------------------------------------
        // Danh gia mot kip (R6 R12 + doi chieu tung thanh vien)
        // -----------------------------------------------------------------
        public static KipLaiDong DanhGiaKip(DuLieuNhanSu dl, PhanCongKipHienThi kip, int soThuTu)
        {
            var thanhVien = new List<ThanhVienKip>();
            var vanDe = new List<VanDeNhanSu>();
            bool laDaPhanCong = kip.TrangThai == PhanCongKipHienThi.DaPhanCong;
            string doiTuong = $"{kip.Chuyen.SoHieuMacTau} · Kíp {soThuTu}";

            foreach (string vaiTro in QuyTacKipLai.CacVaiTro)
            {
                var nv = dl.TimNhanVien(kip.MaNhanVienTheoVaiTro(vaiTro));
                var kq = nv == null ? new KetQuaKiemTraNguoi(null, null) : dl.KetQuaKiemTra(nv.MaNhanVien, kip.MaChuyenTau);

                // Kip da nhan ban / da ban giao khong doi chieu lai dieu kien phan cong
                var dieuKien = laDaPhanCong && nv != null
                    ? DoiChieu(dl, nv, vaiTro, kip.Chuyen, kip.MaGaNhanBan, kip.MaGaBanGiao, kip.MaPhanCong)
                    : new List<DieuKienUngVien>();

                thanhVien.Add(new ThanhVienKip { VaiTro = vaiTro, NhanVien = nv, KiemTra = kq, DieuKien = dieuKien });

                foreach (var dk in dieuKien.Where(d => d.Dat == false))
                {
                    vanDe.Add(new VanDeNhanSu
                    {
                        MucDo = dk.MucDo,
                        DoiTuong = doiTuong,
                        NoiDung = $"{QuyTacKipLai.TenVaiTro(vaiTro)} {nv!.HoTen}: {dk.MoTa}",
                        MaChuyenTau = kip.MaChuyenTau,
                        MaPhanCong = kip.MaPhanCong,
                        MaNhanVien = nv.MaNhanVien,
                        DieuKien = dk.TenDieuKien
                    });
                }
            }

            if (laDaPhanCong)
            {
                var chuaKiemTra = thanhVien.Where(t => t.NhanVien != null && t.KiemTra.ChuaKiemTra).ToList();
                if (chuaKiemTra.Count > 0)
                {
                    bool quaGio = dl.BayGio >= kip.GioNhanBan;
                    vanDe.Add(new VanDeNhanSu
                    {
                        MucDo = quaGio ? MucDoVanDe.CanhBao : MucDoVanDe.LuuY,
                        DoiTuong = doiTuong,
                        NoiDung = (quaGio ? $"Đã qua giờ nhận ban {kip.GioNhanBan:HH:mm dd/MM}, còn " : "Chưa kiểm tra lên ban: ")
                                  + string.Join(", ", chuaKiemTra.Select(t => $"{t.TenVaiTro.ToLower()} {t.HoTen}")),
                        MaChuyenTau = kip.MaChuyenTau,
                        MaPhanCong = kip.MaPhanCong
                    });
                }
            }

            bool quaGioBanGiao = kip.TrangThai == PhanCongKipHienThi.DangThucHien && dl.BayGio > kip.GioBanGiao;
            if (quaGioBanGiao)
            {
                vanDe.Add(new VanDeNhanSu
                {
                    MucDo = MucDoVanDe.CanhBao,
                    DoiTuong = doiTuong,
                    NoiDung = $"Đã quá giờ bàn giao dự kiến tại {kip.TenGaBanGiao} ({kip.GioBanGiao:HH:mm dd/MM}) nhưng chưa xác nhận bàn giao",
                    MaChuyenTau = kip.MaChuyenTau,
                    MaPhanCong = kip.MaPhanCong
                });
            }

            return new KipLaiDong
            {
                PhanCong = kip,
                SoThuTu = soThuTu,
                ThanhVien = thanhVien,
                VanDe = vanDe.OrderBy(v => v.MucDo).ToList(),
                QuaGioBanGiao = quaGioBanGiao
            };
        }

        // -----------------------------------------------------------------
        // Danh gia mot chuyen: cac kip + do phu hanh trinh (R11)
        // -----------------------------------------------------------------
        public static TinhTrangChuyen DanhGiaChuyen(DuLieuNhanSu dl, ChuyenTauNhanSu chuyen)
        {
            var kip = dl.KipCuaChuyen(chuyen.MaChuyenTau)
                        .Select((p, i) => DanhGiaKip(dl, p, i + 1))
                        .ToList();

            var doan = TinhDoanPhu(chuyen, kip);
            var vanDe = new List<VanDeNhanSu>();

            if (chuyen.ConHieuLuc)
            {
                var mucThieu = chuyen.DaXuatPhat ? MucDoVanDe.NghiemTrong : MucDoVanDe.CanhBao;

                if (kip.Count == 0)
                {
                    vanDe.Add(TaoVanDeChuyen(chuyen, mucThieu, chuyen.DaXuatPhat
                        ? "Tàu đang chạy nhưng chưa phân công kíp lái nào"
                        : "Chưa phân công kíp lái nào cho chuyến"));
                }
                else
                {
                    foreach (var d in doan.Where(d => d.Kip == null))
                    {
                        string chang = $"{chuyen.LichDung[d.TuViTri].TenGa} → {chuyen.LichDung[d.DenViTri].TenGa}";
                        vanDe.Add(TaoVanDeChuyen(chuyen, mucThieu, chuyen.DaXuatPhat
                            ? $"Tàu đang chạy: chặng {chang} chưa có kíp lái"
                            : $"Chặng {chang} chưa có kíp lái"));
                    }
                }

                foreach (var d in doan.Where(d => d.Chong))
                {
                    string chang = $"{chuyen.LichDung[d.TuViTri].TenGa} → {chuyen.LichDung[d.DenViTri].TenGa}";
                    vanDe.Add(TaoVanDeChuyen(chuyen, MucDoVanDe.NghiemTrong, $"Nhiều kíp cùng chạy chặng {chang}"));
                }

                if (!chuyen.CoLichDungGa)
                    vanDe.Add(TaoVanDeChuyen(chuyen, MucDoVanDe.LuuY,
                        $"Chuyến chưa có lịch dừng ga, chỉ phân công được cả hành trình {chuyen.HanhTrinh}"));

                if (chuyen.SoHieuDauMay == null)
                    vanDe.Add(TaoVanDeChuyen(chuyen, MucDoVanDe.LuuY, "Chuyến chưa lập đoàn tàu, chưa đối chiếu được bằng lái"));

                vanDe.AddRange(kip.SelectMany(k => k.VanDe));
            }

            return new TinhTrangChuyen
            {
                Chuyen = chuyen,
                Kip = kip,
                Doan = doan,
                VanDe = vanDe.OrderBy(v => v.MucDo).ToList()
            };
        }

        private static VanDeNhanSu TaoVanDeChuyen(ChuyenTauNhanSu chuyen, MucDoVanDe muc, string noiDung)
            => new() { MucDo = muc, DoiTuong = chuyen.SoHieuMacTau, NoiDung = noiDung, MaChuyenTau = chuyen.MaChuyenTau };

        // Chia hanh trinh thanh cac doan lien tiep co cung kip phu (null = chua co kip)
        public static List<DoanPhuKip> TinhDoanPhu(ChuyenTauNhanSu chuyen, List<KipLaiDong> kip)
        {
            var ketQua = new List<DoanPhuKip>();
            int soKhoang = chuyen.LichDung.Count - 1;
            if (soKhoang <= 0) return ketQua;

            List<KipLaiDong> PhuKhoang(int k) => kip
                .Where(x => x.PhanCong.ViTriNhan >= 0 && x.PhanCong.ViTriNhan <= k && x.PhanCong.ViTriGiao >= k + 1)
                .ToList();

            int batDau = 0;
            var truoc = PhuKhoang(0);
            for (int k = 1; k <= soKhoang; k++)
            {
                var hienTai = k < soKhoang ? PhuKhoang(k) : null;
                bool giongNhau = hienTai != null && hienTai.Count == truoc.Count
                                 && hienTai.Select(x => x.PhanCong.MaPhanCong).SequenceEqual(truoc.Select(x => x.PhanCong.MaPhanCong));
                if (giongNhau) continue;

                ketQua.Add(new DoanPhuKip(batDau, k, truoc.FirstOrDefault(), truoc.Count > 1));
                batDau = k;
                if (hienTai != null) truoc = hienTai;
            }
            return ketQua;
        }

        // -----------------------------------------------------------------
        // Danh sach chon nguoi cho mot vi tri tren mot chang (29/09):
        //   - chi gom nguoi dung chuc danh, chua nghi viec va KHONG vi pham dieu
        //     kien nao (R3 R4 R6 R7 R8) -> phan cong xong khong the ra ca vi pham;
        //   - xep: khong canh bao truoc -> thuoc depot o dau chang -> nghi lau
        //     nhat (khong co ca truoc = nghi du) -> ma NV;
        //   - nguoi khong du dieu kien bi an, giu kem ly do ("Da an N nguoi").
        // -----------------------------------------------------------------
        public static DanhSachUngVien LayUngVien(DuLieuNhanSu dl, string vaiTro, ChuyenTauNhanSu chuyen,
                                                 int maGaNhan, int maGaGiao, int? maPhanCongBoQua)
        {
            string chucDanh = QuyTacKipLai.ChucDanhYeuCau(vaiTro);
            var tatCa = dl.NhanVien
                .Where(n => n.TrangThai != NhanVienHienThi.DaNghiViec
                            && string.Equals(n.ChucDanh.Trim(), chucDanh, StringComparison.OrdinalIgnoreCase))
                .Select(n => TaoUngVien(dl, n, vaiTro, chuyen, maGaNhan, maGaGiao, maPhanCongBoQua))
                .ToList();

            return new DanhSachUngVien
            {
                DuDieuKien = tatCa.Where(u => !u.CoLoi)
                                  .OrderBy(u => u.CoCanhBao)
                                  .ThenBy(u => u.CungDepot switch { true => 0, null => 1, _ => 2 })
                                  .ThenByDescending(u => u.SoGioNghi ?? decimal.MaxValue)
                                  .ThenBy(u => u.MaNVCode, StringComparer.Ordinal)
                                  .ToList(),
                BiAn = tatCa.Where(u => u.CoLoi)
                            .OrderBy(u => u.MaNVCode, StringComparer.Ordinal)
                            .ToList()
            };
        }

        // Doi chieu mot nguoi cho mot vi tri tren mot chang (ca nguoi dang trong kip dang sua)
        public static UngVienKip TaoUngVien(DuLieuNhanSu dl, NhanVienHienThi nv, string vaiTro, ChuyenTauNhanSu chuyen,
                                            int maGaNhan, int maGaGiao, int? maPhanCongBoQua)
        {
            DateTime gioNhan = chuyen.GioNhanBan(maGaNhan);
            var truoc = dl.CaTruoc(nv.MaNhanVien, chuyen, gioNhan, maPhanCongBoQua);
            return new UngVienKip
            {
                NhanVien = nv,
                DieuKien = DoiChieu(dl, nv, vaiTro, chuyen, maGaNhan, maGaGiao, maPhanCongBoQua),
                SoGioNghi = truoc == null ? null : QuyTacKipLai.SoGio(truoc.GioBanGiao, gioNhan)
            };
        }
    }

    // -------------------------------------------------------------------------
    // MA NHAN VIEN THEO CHUC DANH (quy uoc 29/09 - CSDL chi rang buoc
    // MaNVCode VARCHAR(20) NOT NULL UNIQUE).
    // Dang TIENTO_SSS: LT_001 lai tau, PL_001 phu lai, TT_001 truong tau ...;
    // so thu tu dem rieng tung tien to. Doi chuc danh -> cap ma moi (kip, kiem tra,
    // tai khoan noi bang MaNhanVien nen khong anh huong).
    // Tu 11/10 ma that do NhanSuService cap trong giao dich (MAX + 1 cua tien to,
    // khoa UPDLOCK / HOLDLOCK); dialog chi hien ma du kien.
    // -------------------------------------------------------------------------
    public static class MaNhanVienTheoChucDanh
    {
        public const string TienToKhac = "NV";

        // Thu tu trong bang = thu tu hien thi (to tau truoc)
        private static readonly (string ChucDanh, string TienTo)[] Bang =
        {
            ("Lái tàu", "LT"), ("Phụ lái", "PL"), ("Trưởng tàu", "TT"), ("Tiếp viên", "TV"),
            ("Nhân viên khám xe", "KX"), ("Nhân viên bán vé", "BV")
        };

        private static readonly Regex DangMa = new(@"^([A-Z]{2})_(\d{3,9})$");

        public const string MoTaQuyUoc =
            "LT lái tàu · PL phụ lái · TT trưởng tàu · TV tiếp viên · KX khám xe · BV bán vé · NV chức danh khác";

        private static int ViTri(string? chucDanh)
        {
            string cd = chucDanh?.Trim() ?? string.Empty;
            for (int i = 0; i < Bang.Length; i++)
                if (string.Equals(Bang[i].ChucDanh, cd, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        public static string TienTo(string? chucDanh)
        {
            int i = ViTri(chucDanh);
            return i >= 0 ? Bang[i].TienTo : TienToKhac;
        }

        // Sap xep theo chuc danh: lai tau, phu lai, truong tau, tiep vien, kham xe, ban ve, khac
        public static int ThuTu(string? chucDanh)
        {
            int i = ViTri(chucDanh);
            return i >= 0 ? i : Bang.Length;
        }

        // Ma dung dang va dung tien to cua chuc danh
        public static bool KhopChucDanh(string? ma, string? chucDanh)
        {
            var m = DangMa.Match(ma ?? string.Empty);
            return m.Success && m.Groups[1].Value == TienTo(chucDanh);
        }

        // Ma ke tiep = so lon nhat da cap cua tien to + 1
        public static string MaKeTiep(string? chucDanh, IEnumerable<string> maDaCap)
        {
            string tienTo = TienTo(chucDanh);
            int lonNhat = 0;
            foreach (string ma in maDaCap)
            {
                var m = DangMa.Match(ma ?? string.Empty);
                if (m.Success && m.Groups[1].Value == tienTo && int.TryParse(m.Groups[2].Value, out int so) && so > lonNhat)
                    lonNhat = so;
            }
            return $"{tienTo}_{lonNhat + 1:000}";
        }
    }
}
