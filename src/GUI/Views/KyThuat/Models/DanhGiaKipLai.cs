namespace GUI.Views.KyThuat.Models
{
    // =========================================================================
    // DOI CHIEU DIEU KIEN PHAN CONG KIP LAI (R2..R12 - PHAN_TICH_NHAN_SU_KIP_LAI.md)
    // Dung chung cho NhanSuPage, PhanCongKipDialog, KiemTraLenBanDialog va
    // bieu do ca truc - moi noi cung mot ket luan.
    // =========================================================================

    // Anh chup du lieu da tron thay doi tam, truyen cho dialog de doi chieu
    public class DuLieuNhanSu
    {
        private readonly Dictionary<int, NhanVienHienThi> _nhanVien;

        public DuLieuNhanSu(IReadOnlyList<NhanVienHienThi> nhanVien, IReadOnlyList<PhanCongKipHienThi> phanCong,
                            IReadOnlyList<KiemTraLenBanHienThi> kiemTra, DateTime bayGio)
        {
            NhanVien = nhanVien;
            PhanCong = phanCong;
            KiemTra = kiemTra;
            BayGio = bayGio;
            _nhanVien = nhanVien.ToDictionary(n => n.MaNhanVien);
        }

        public IReadOnlyList<NhanVienHienThi> NhanVien { get; }
        public IReadOnlyList<PhanCongKipHienThi> PhanCong { get; }
        public IReadOnlyList<KiemTraLenBanHienThi> KiemTra { get; }
        public DateTime BayGio { get; }
        public IReadOnlyList<ChuyenTauNhanSu> ChuyenTau => DanhMucMauNhanSu.ChuyenTau;

        public NhanVienHienThi? TimNhanVien(int ma) => _nhanVien.GetValueOrDefault(ma);

        public IEnumerable<PhanCongKipHienThi> KipCuaNhanVien(int maNhanVien)
            => PhanCong.Where(p => p.CoNhanVien(maNhanVien));

        // Kip cua chuyen theo thu tu chang (ga nhan ban tu dau tuyen)
        public List<PhanCongKipHienThi> KipCuaChuyen(int maChuyenTau)
            => PhanCong.Where(p => p.MaChuyenTau == maChuyenTau)
                       .OrderBy(p => p.ViTriNhan).ThenBy(p => p.MaPhanCong)
                       .ToList();

        public int SoThuTuKip(PhanCongKipHienThi kip)
            => KipCuaChuyen(kip.MaChuyenTau).FindIndex(p => p.MaPhanCong == kip.MaPhanCong) + 1;

        public KetQuaKiemTraNguoi KetQuaKiemTra(int maNhanVien, int maChuyenTau)
        {
            var ds = KiemTra.Where(k => k.MaNhanVien == maNhanVien && k.MaChuyenTau == maChuyenTau)
                            .OrderByDescending(k => k.ThoiDiemKiemTra)
                            .ToList();
            return new KetQuaKiemTraNguoi(ds.FirstOrDefault(), ds.FirstOrDefault(k => !k.DuDieuKien));
        }

        // Ca gan nhat da ket thuc truoc thoi diem (bo qua kip dang sua, chuyen trung khung gio)
        public PhanCongKipHienThi? CaTruoc(int maNhanVien, ChuyenTauNhanSu chuyen, DateTime gioNhanBan, int? maPhanCongBoQua)
            => KipCuaNhanVien(maNhanVien)
                .Where(p => p.MaPhanCong != maPhanCongBoQua && p.MaChuyenTau != chuyen.MaChuyenTau
                            && !QuyTacKipLai.TrungKhungGio(p.Chuyen, chuyen) && p.GioBanGiao <= gioNhanBan)
                .OrderByDescending(p => p.GioBanGiao)
                .FirstOrDefault();
    }

    // R6: mot lan khong dat la khong duoc len ban chuyen do
    public record KetQuaKiemTraNguoi(KiemTraLenBanHienThi? MoiNhat, KiemTraLenBanHienThi? LanKhongDat)
    {
        public bool KhongDat => LanKhongDat != null;
        public bool DaDat => LanKhongDat == null && MoiNhat?.DuDieuKien == true;
        public bool ChuaKiemTra => MoiNhat == null;

        public string NhanNgan => KhongDat ? "Không đạt kiểm tra"
                                : DaDat ? $"Đạt · {MoiNhat!.ThoiDiemKiemTra:HH:mm}"
                                : "Chưa kiểm tra";

        public string Mau => KhongDat ? "#B91C1C" : DaDat ? "#15803D" : "#B45309";
    }

    // Ket qua doi chieu mot dieu kien. Dat: true dat / false vi pham (MucDo cho biet
    // nang nhe) / null khong ap dung hoac chua co du lieu.
    public record DieuKienUngVien(string TenDieuKien, bool? Dat, MucDoVanDe MucDo, string GiaTri, string MoTa = "")
    {
        public bool LaLoi => Dat == false && MucDo == MucDoVanDe.NghiemTrong;

        public string KyHieu => Dat switch
        {
            true => "✓",
            false when MucDo == MucDoVanDe.NghiemTrong => "✗",
            false => "!",
            _ => "–"
        };

        public string MauChu => Dat switch
        {
            true => "#15803D",
            false => MucDo == MucDoVanDe.NghiemTrong ? "#B91C1C" : MucDo == MucDoVanDe.CanhBao ? "#B45309" : "#64748B",
            _ => "#94A3B8"
        };
    }

    public class VanDeNhanSu
    {
        public MucDoVanDe MucDo { get; init; }
        public string DoiTuong { get; init; } = "";
        public string NoiDung { get; init; } = "";
        public int? MaChuyenTau { get; init; }
        public int? MaPhanCong { get; init; }
        public int? MaNhanVien { get; init; }
        public string? DieuKien { get; init; }   // ten dieu kien vi pham (DanhGiaKipLai.Dk*), null = van de cua chuyen / kip

        public string NhanMucDo => MucDoHienThi.Nhan(MucDo);
        public string MauMucDo => MucDoHienThi.Mau(MucDo);
    }

    // Mot thanh vien trong mot kip (dong luoi kip)
    public class ThanhVienKip
    {
        public string VaiTro { get; init; } = "";
        public NhanVienHienThi? NhanVien { get; init; }
        public KetQuaKiemTraNguoi KiemTra { get; init; } = new(null, null);
        public List<DieuKienUngVien> DieuKien { get; init; } = new();

        public string TenVaiTro => QuyTacKipLai.TenVaiTro(VaiTro);
        public string HoTen => NhanVien?.HoTen ?? "(chưa có)";

        // Loi phan cong nang nhat (khong tinh ket qua kiem tra - hien rieng)
        public DieuKienUngVien? LoiPhanCong => DieuKien.FirstOrDefault(d => d.LaLoi && d.TenDieuKien != DanhGiaKipLai.DkKiemTra);

        public string TrangThaiNgan => LoiPhanCong?.GiaTri ?? KiemTra.NhanNgan;
        public string MauTrangThai => LoiPhanCong != null ? "#B91C1C" : KiemTra.Mau;
    }

    // Mot dong trong luoi kip cua chuyen / hang cho nhan ban
    public class KipLaiDong
    {
        public PhanCongKipHienThi PhanCong { get; init; } = null!;
        public int SoThuTu { get; init; }
        public List<ThanhVienKip> ThanhVien { get; init; } = new();
        public List<VanDeNhanSu> VanDe { get; init; } = new();
        public bool QuaGioBanGiao { get; init; }

        public ChuyenTauNhanSu Chuyen => PhanCong.Chuyen;
        public ThanhVienKip LaiTau => ThanhVien[0];
        public ThanhVienKip PhuLai => ThanhVien[1];
        public ThanhVienKip TruongTau => ThanhVien[2];

        public string TenKip => $"Kíp {SoThuTu}";
        public string NhanChuyen => $"{Chuyen.SoHieuMacTau} · {Chuyen.NgayXuatPhat:dd/MM}";
        public bool LaThayDoiTam => PhanCong.LaThayDoiTam;

        public bool CoLoi => VanDe.Any(v => v.MucDo == MucDoVanDe.NghiemTrong);
        public int SoDaDat => ThanhVien.Count(t => t.KiemTra.DaDat);
        public bool CoNguoiKhongDat => ThanhVien.Any(t => t.KiemTra.KhongDat);
        public bool LaDaPhanCong => PhanCong.TrangThai == PhanCongKipHienThi.DaPhanCong;
        public bool CoTheNhanBan => LaDaPhanCong && !CoLoi && SoDaDat == 3;

        public string TinhTrang => PhanCong.TrangThai switch
        {
            PhanCongKipHienThi.DangThucHien => QuaGioBanGiao ? "Quá giờ bàn giao" : "Đang thực hiện",
            PhanCongKipHienThi.HoanThanh => "Đã bàn giao",
            _ => CoNguoiKhongDat ? "Cần thay người"
               : CoLoi ? "Lỗi phân công"
               : SoDaDat == 3 ? "Đủ điều kiện nhận ban"
               : $"Chờ kiểm tra ({SoDaDat}/3 đạt)"
        };

        public string MauTinhTrang => PhanCong.TrangThai switch
        {
            PhanCongKipHienThi.DangThucHien => QuaGioBanGiao ? "#B45309" : "#1D4ED8",
            PhanCongKipHienThi.HoanThanh => "#64748B",
            _ => CoLoi ? "#B91C1C" : SoDaDat == 3 ? "#15803D" : "#B45309"
        };
    }

    // Mot doan tren so do hanh trinh: tu diem dung TuViTri toi DenViTri
    public record DoanPhuKip(int TuViTri, int DenViTri, KipLaiDong? Kip, bool Chong);

    // Tinh trang phan cong cua mot chuyen (dong danh sach chuyen)
    public class TinhTrangChuyen
    {
        public ChuyenTauNhanSu Chuyen { get; init; } = null!;
        public List<KipLaiDong> Kip { get; init; } = new();
        public List<DoanPhuKip> Doan { get; init; } = new();
        public List<VanDeNhanSu> VanDe { get; init; } = new();

        public bool ConHieuLuc => Chuyen.ConHieuLuc;
        public bool ThieuKip => ConHieuLuc && Doan.Any(d => d.Kip == null);
        public int SoKipCoLoi => Kip.Count(k => k.LaDaPhanCong && k.CoLoi);

        public string NhanPhuKip
        {
            get
            {
                if (!ConHieuLuc) return Kip.Count == 0 ? "Không có kíp" : $"{Kip.Count} kíp · đã kết thúc";
                if (Kip.Count == 0) return "Chưa có kíp";
                var trong = Doan.Where(d => d.Kip == null).ToList();
                if (trong.Count == 0)
                    return SoKipCoLoi > 0 ? $"Đủ {Kip.Count} kíp · {SoKipCoLoi} cần xử lý" : $"Đủ {Kip.Count} kíp";
                var d0 = trong[0];
                string chang = $"Thiếu {Chuyen.LichDung[d0.TuViTri].TenGa} → {Chuyen.LichDung[d0.DenViTri].TenGa}";
                return trong.Count > 1 ? $"{chang} (+{trong.Count - 1})" : chang;
            }
        }

        public string MauPhuKip => !ConHieuLuc ? "#64748B"
                                 : Kip.Count == 0 || (ThieuKip && Chuyen.DaXuatPhat) ? "#B91C1C"
                                 : ThieuKip ? "#B45309"
                                 : SoKipCoLoi > 0 ? "#B91C1C"
                                 : "#15803D";
    }

    // Mot kip ma nhan vien dam nhan (panel ho so)
    public class KipCuaNhanVien
    {
        public PhanCongKipHienThi PhanCong { get; init; } = null!;
        public string VaiTro { get; init; } = "";

        public string TenVaiTro => QuyTacKipLai.TenVaiTro(VaiTro);
        public string NhanChuyen => $"{PhanCong.Chuyen.SoHieuMacTau} · {PhanCong.Chuyen.NgayXuatPhat:dd/MM}";
        public string MoTa => $"{PhanCong.Chang} · {PhanCong.KhungGio}";
        public string NhanTrangThai => PhanCong.NhanTrangThai;

        public string MauTrangThai => PhanCong.TrangThai switch
        {
            PhanCongKipHienThi.DangThucHien => "#1D4ED8",
            PhanCongKipHienThi.HoanThanh => "#64748B",
            _ => "#334155"
        };
    }

    // Muc trong ComboBox chon nguoi cho mot vi tri
    public class UngVienKip
    {
        public NhanVienHienThi NhanVien { get; init; } = null!;
        public List<DieuKienUngVien> DieuKien { get; init; } = new();

        public bool CoLoi => DieuKien.Any(d => d.LaLoi);
        public bool CoCanhBao => DieuKien.Any(d => d.Dat == false && d.MucDo == MucDoVanDe.CanhBao);

        public string MaNVCode => NhanVien.MaNVCode;
        public string HoTen => NhanVien.HoTen;
        public string PhuDe => NhanVien.LaBanLaiMay
            ? $"{NhanVien.NhanBangLai} · {NhanVien.DonViChuQuan}"
            : NhanVien.DonViChuQuan;

        public string TomTat
        {
            get
            {
                var loi = DieuKien.FirstOrDefault(d => d.LaLoi);
                if (loi != null) return loi.GiaTri;
                var canhBao = DieuKien.FirstOrDefault(d => d.Dat == false && d.MucDo == MucDoVanDe.CanhBao);
                if (canhBao != null) return canhBao.GiaTri;
                string nghi = DieuKien.FirstOrDefault(d => d.TenDieuKien == DanhGiaKipLai.DkNghi)?.GiaTri ?? "";
                return nghi.StartsWith("Nghỉ") ? $"Phù hợp · {nghi.ToLower()}" : "Phù hợp";
            }
        }

        public string MauTomTat => CoLoi ? "#B91C1C" : CoCanhBao ? "#B45309" : "#15803D";

        public override string ToString() => $"{NhanVien.MaNVCode} · {NhanVien.HoTen}";
    }

    public static class DanhGiaKipLai
    {
        public const string DkChucDanh = "Chức danh";
        public const string DkBangLai = "Bằng lái – đầu máy";
        public const string DkHanKham = "Hạn khám sức khỏe";
        public const string DkTrung = "Trùng chuyến khác";
        public const string DkNghi = "Nghỉ giữa hai ca";
        public const string DkKiemTra = "Kiểm tra lên ban";
        public const string DkDonVi = "Đơn vị – chặng";
        public const string DkTrangThai = "Trạng thái hồ sơ";

        public static readonly string[] ThuTuDieuKien =
            { DkChucDanh, DkBangLai, DkHanKham, DkTrung, DkNghi, DkKiemTra, DkDonVi, DkTrangThai };

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
            var gaDongQuan = DanhMucMauNhanSu.GaDongQuan(nv.DonViChuQuan);
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

        // Danh sach ung vien cho mot vi tri: dung chuc danh, chua nghi viec; nguoi dat len truoc
        public static List<UngVienKip> LayUngVien(DuLieuNhanSu dl, string vaiTro, ChuyenTauNhanSu chuyen,
                                                  int maGaNhan, int maGaGiao, int? maPhanCongBoQua)
        {
            string chucDanh = QuyTacKipLai.ChucDanhYeuCau(vaiTro);
            return dl.NhanVien
                .Where(n => n.TrangThai != NhanVienHienThi.DaNghiViec
                            && string.Equals(n.ChucDanh.Trim(), chucDanh, StringComparison.OrdinalIgnoreCase))
                .Select(n => new UngVienKip
                {
                    NhanVien = n,
                    DieuKien = DoiChieu(dl, n, vaiTro, chuyen, maGaNhan, maGaGiao, maPhanCongBoQua)
                })
                .OrderBy(u => u.CoLoi).ThenBy(u => u.CoCanhBao).ThenBy(u => u.NhanVien.MaNVCode)
                .ToList();
        }
    }
}
