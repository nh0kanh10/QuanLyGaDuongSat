namespace DTO.NhanSu
{
    public class DuLieuNhanSu
    {
        private readonly Dictionary<int, NhanVienHienThi> _nhanVien;
        private readonly Dictionary<int, ChuyenTauNhanSu> _chuyen;
        private readonly List<GaNhanSu> _gaTheoDoDaiTen;

        public DuLieuNhanSu(IReadOnlyList<NhanVienHienThi> nhanVien, IReadOnlyList<PhanCongKipHienThi> phanCong,
                            IReadOnlyList<KiemTraLenBanHienThi> kiemTra, IReadOnlyList<ChuyenTauNhanSu> chuyenTau,
                            IReadOnlyList<GaNhanSu> ga, IReadOnlyList<string> hangBangLai, DateTime bayGio)
        {
            NhanVien = nhanVien;
            PhanCong = phanCong;
            KiemTra = kiemTra;
            ChuyenTau = chuyenTau;
            Ga = ga;
            HangBangLai = hangBangLai;
            BayGio = bayGio;
            _nhanVien = nhanVien.ToDictionary(n => n.MaNhanVien);
            _chuyen = chuyenTau.ToDictionary(c => c.MaChuyenTau);
            _gaTheoDoDaiTen = ga.OrderByDescending(g => g.TenGa.Length).ToList();
        }

        // Anh chup rong (khong ket noi duoc CSDL): man hinh van mo duoc
        public static DuLieuNhanSu Rong(DateTime bayGio) => new(
            Array.Empty<NhanVienHienThi>(), Array.Empty<PhanCongKipHienThi>(), Array.Empty<KiemTraLenBanHienThi>(),
            Array.Empty<ChuyenTauNhanSu>(), Array.Empty<GaNhanSu>(), Array.Empty<string>(), bayGio);

        public IReadOnlyList<NhanVienHienThi> NhanVien { get; }
        public IReadOnlyList<PhanCongKipHienThi> PhanCong { get; }
        public IReadOnlyList<KiemTraLenBanHienThi> KiemTra { get; }
        public IReadOnlyList<ChuyenTauNhanSu> ChuyenTau { get; }
        public IReadOnlyList<GaNhanSu> Ga { get; }

        // Hang bang lai hop le = ma dong dau may (phuongtien.DongDauMay.MaDongCode)
        public IReadOnlyList<string> HangBangLai { get; }

        public DateTime BayGio { get; }

        // Moi ma da tung cap: ma hien tai + ma cu trong lich su doi ma (khong cap lai ma cu)
        public IEnumerable<string> MaDaCap => NhanVien.SelectMany(n => n.CacMaCu.Prepend(n.MaNVCode));

        public NhanVienHienThi? TimNhanVien(int ma) => _nhanVien.GetValueOrDefault(ma);
        public ChuyenTauNhanSu? TimChuyen(int ma) => _chuyen.GetValueOrDefault(ma);
        public PhanCongKipHienThi? TimKip(int maPhanCong) => PhanCong.FirstOrDefault(p => p.MaPhanCong == maPhanCong);

        // R10 (tham khao): ga dong quan cua don vi = ga co ten nam trong ten don vi
        // ("Đội lái tàu Vinh" -> Vinh, "Trạm Giáp Bát" -> Giáp Bát). 
        // chang - depot. "Phương Nam" = khu vuc phia Nam -> Sai Gon.
        public GaNhanSu? GaDongQuan(string? donVi)
        {
            if (string.IsNullOrWhiteSpace(donVi)) return null;
            if (donVi.Contains("Phương Nam", StringComparison.OrdinalIgnoreCase))
                return Ga.FirstOrDefault(g => g.MaGaCode == "SGO");
            return _gaTheoDoDaiTen.FirstOrDefault(g => donVi.Contains(g.TenGa, StringComparison.OrdinalIgnoreCase));
        }

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
        public string? DieuKien { get; init; }   // ten dieu kien vi pham (DieuKienKip.*), null = van de cua chuyen / kip

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
        public DieuKienUngVien? LoiPhanCong => DieuKien.FirstOrDefault(d => d.LaLoi && d.TenDieuKien != DieuKienKip.KiemTra);

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

    // Mot nguoi doi chieu cho mot vi tri tren mot chang: muc trong ComboBox chon
    // nguoi (du dieu kien) hoac dong trong danh sach "Da an" (khong du dieu kien)
    public class UngVienKip
    {
        public NhanVienHienThi NhanVien { get; init; } = null!;
        public List<DieuKienUngVien> DieuKien { get; init; } = new();

        // So gio nghi tu ca truoc toi gio nhan ban chang nay; null = khong co ca truoc
        public decimal? SoGioNghi { get; init; }

        // R10: thuoc depot o mot dau chang (true) / khong (false) / khong ro ga dong quan (null)
        public bool? CungDepot => DieuKien.FirstOrDefault(d => d.TenDieuKien == DieuKienKip.DonVi)?.Dat;

        public bool CoLoi => DieuKien.Any(d => d.LaLoi);
        public bool CoCanhBao => DieuKien.Any(d => d.Dat == false && d.MucDo == MucDoVanDe.CanhBao);

        // Ly do khong du dieu kien (cac dieu kien vi pham)
        public string LyDoKhongDuDieuKien => string.Join("; ", DieuKien.Where(d => d.LaLoi).Select(d => d.MoTa));

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
                if (canhBao != null)
                    return canhBao.TenDieuKien == DieuKienKip.HanKham ? $"Hạn khám {canhBao.GiaTri.ToLower()}" : canhBao.GiaTri;
                string nghi = SoGioNghi.HasValue ? $"nghỉ {SoGioNghi:0.0} giờ" : "không có ca trước";
                return CungDepot == false ? $"Khác depot · {nghi}" : $"Phù hợp · {nghi}";
            }
        }

        public string MauTomTat => CoLoi ? "#B91C1C" : CoCanhBao ? "#B45309" : CungDepot == false ? "#64748B" : "#15803D";

        public override string ToString() => $"{NhanVien.MaNVCode} · {NhanVien.HoTen}";
    }

    // Danh sach chon nguoi cho mot vi tri tren mot chang: chi nguoi du dieu kien
    // duoc chon; nguoi khong du dieu kien bi an, giu kem ly do de xem
    public class DanhSachUngVien
    {
        public List<UngVienKip> DuDieuKien { get; init; } = new();
        public List<UngVienKip> BiAn { get; init; } = new();

        // Nguoi dung dau danh sach da xep - dung cho "Goi y kip"
        public UngVienKip? GoiY => DuDieuKien.FirstOrDefault();

        public UngVienKip? Tim(int maNhanVien) => DuDieuKien.FirstOrDefault(u => u.NhanVien.MaNhanVien == maNhanVien);
    }
}
