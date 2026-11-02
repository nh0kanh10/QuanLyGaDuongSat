namespace DTO.BaoTri
{
    // =========================================================================
    // BAO TRI & SUA CHUA (Dat, Sprint 2): du lieu doc / ghi schema baotri.
    // Ten thuoc tinh = ten cot (script_tao_csdl_v2.sql + migration V20261024_1000).
    // Lop o day chi chua du lieu + vai thuoc tinh tinh toan khong can WPF; luat nghiep vu
    // o BUS (QuyTacBaoTri, BaoTriService, KiemTraXuatBenService), mau / bieu tuong o GUI.
    // =========================================================================

    // Doan tau (vanhanh.DoanTau JOIN ChuyenTau, MacTauMau, Ga, DauMay) - chi doc, tru co DaDuyetAnToan
    public class DoanTauBaoTriDTO
    {
        public int MaDoanTau { get; init; }
        public int MaChuyenTau { get; init; }
        public string SoHieuMacTau { get; init; } = "";
        public string LoaiTau { get; init; } = "";
        public DateTime NgayXuatPhat { get; init; }
        public DateTime GioXuatPhatKH { get; init; }
        public DateTime GioVeDichKH { get; init; }
        public string TenGaDi { get; init; } = "";
        public string TenGaDen { get; init; } = "";
        public int MaDauMayChinh { get; init; }
        public string SoHieuDauMayChinh { get; init; } = "";
        public int? MaDauMayDay { get; init; }
        public string? SoHieuDauMayDay { get; init; }
        public int TongSoToa { get; init; }
        public decimal TongChieuDaiM { get; init; }
        public decimal TongTrongLuongTan { get; init; }
        public bool DaDuyetAnToan { get; init; }
        public string TrangThaiChuyen { get; init; } = "";
        public byte[] PhienBan { get; init; } = Array.Empty<byte>();

        // Lan lap / sua doan tau gan nhat (kiemtoan.NhatKyHeThong LAP_DOAN_TAU / SUA_DOAN_TAU).
        // null = doan tau co tu seed, chua sua qua ung dung. Bien ban kham truoc moc nay khong con hieu luc.
        public DateTime? ThoiDiemSuaDoanTau { get; init; }

        public bool DaXuatPhat => TrangThaiChuyen is "DANG_CHAY" or "TRE_GIO" or "HOAN_THANH" or "DA_HUY";
        public bool DaKetThuc => TrangThaiChuyen is "HOAN_THANH" or "DA_HUY";
        public string HanhTrinh => $"{TenGaDi} → {TenGaDen}";
    }

    // Dau may (phuongtien.DauMay JOIN DongDauMay)
    public class DauMayBaoTriDTO
    {
        public int MaDauMay { get; init; }
        public string SoHieuDauMay { get; init; } = "";
        public string MaDongCode { get; init; } = "";
        public string DonViQuanLy { get; init; } = "";
        public string TrangThai { get; init; } = "";
        public decimal SoKmTichLuy { get; init; }
        public int DungTichBonDauLit { get; init; }
        public decimal DinhMucSFC { get; init; }
        public int CongSuatHP { get; init; }
    }

    // Toa xe (phuongtien.ToaXe JOIN ChungLoaiToa). DangChay = dang nam trong doan tau cua chuyen dang chay.
    public class ToaXeBaoTriDTO
    {
        public int MaToaXe { get; init; }
        public string SoHieuToaXe { get; init; } = "";
        public string MaChungLoaiCode { get; init; } = "";
        public string TenChungLoai { get; init; } = "";
        public string TrangThai { get; init; } = "";
        public bool DangChay { get; init; }
    }

    // Tai khoan co the ky phieu bao tri (phanquyen.TaiKhoan co vai tro KY_THUAT). DonVi lay tu ho so nhan vien.
    public class TaiKhoanKyThuatDTO
    {
        public int MaTaiKhoan { get; init; }
        public string TenDangNhap { get; init; } = "";
        public string HoTenHienThi { get; init; } = "";
        public string DonVi { get; init; } = "";
        public bool DangHoatDong { get; init; }

        // Co vai tro KY_THUAT: duoc chon de ky phieu moi. false = chi con trong lich su phieu cu (vd admin
        // cap dau trong seed goc) - van doc de hien ten.
        public bool LaKyThuat { get; init; }

        public bool ChonDuoc => LaKyThuat && DangHoatDong;
    }

    // baotri.KhamXeKyThuat - chi them, khong sua (quyet dinh 24/10: sai thi lap bien ban moi)
    public class KhamXeDTO
    {
        public int MaKhamXe { get; set; }
        public int MaDoanTau { get; set; }
        public int MaNguoiKham { get; set; }
        public decimal ApLucHamBar { get; set; }
        public decimal DoSutApBarPhut { get; set; }
        public bool DaCapNuoc { get; set; }
        public bool DaXaVeSinh { get; set; }
        public string GhiChuKyThuat { get; set; } = "";
        public DateTime ThoiDiemKham { get; set; }
        public decimal ChiPhi { get; set; }
    }

    // baotri.NhatKyCapNhienLieu. SuatTieuHaoSFC + CanhBaoVuotMuc do Service tinh; ChiPhi la cot tinh.
    public class CapNhienLieuDTO
    {
        public int MaNhatKyDau { get; set; }
        public int MaDauMay { get; set; }
        public string NoiCapDau { get; set; } = "";
        public decimal SoLitTraNap { get; set; }
        public decimal TrongLuongKeoTan { get; set; }
        public decimal CuLyChayKm { get; set; }
        public decimal SuatTieuHaoSFC { get; set; }
        public bool CanhBaoVuotMuc { get; set; }
        public int? MaNguoiCap { get; set; }
        public DateTime ThoiDiemBomDau { get; set; }
        public decimal DonGiaVndLit { get; set; }
        public decimal ChiPhi { get; set; }
        public byte[]? PhienBan { get; set; }
    }

    // baotri.NhatKyBaoDuong. Dung mot trong hai MaDauMay / MaToaXe theo LoaiPhuongTien (CK_PhuongTien_Ref).
    public class BaoDuongDTO
    {
        public int MaBaoDuong { get; set; }
        public string LoaiPhuongTien { get; set; } = "DAU_MAY";
        public int? MaDauMay { get; set; }
        public int? MaToaXe { get; set; }
        public string CapBaoDuong { get; set; } = "";
        public decimal SoKmTaiThoiDiem { get; set; }
        public string GhiChuKyThuat { get; set; } = "";
        public int? MaNguoiThucHien { get; set; }
        public DateTime ThoiDiemHoanThanh { get; set; }
        public decimal ChiPhi { get; set; }
        public byte[]? PhienBan { get; set; }

        public bool LaDauMay => LoaiPhuongTien == "DAU_MAY";
        public int MaPhuongTien => (LaDauMay ? MaDauMay : MaToaXe) ?? 0;
        public string KhoaPhuongTien => $"{LoaiPhuongTien}:{MaPhuongTien}";
    }

    // =========================================================================
    // ANH CHUP DU LIEU cho ca trang Bao tri (5 tab + 3 dialog) va cho Service chay lai luat
    // trong giao dich truoc khi ghi. BayGio = gio may chu luc doc (gio Viet Nam).
    // =========================================================================
    public class DuLieuBaoTri
    {
        public List<DoanTauBaoTriDTO> DoanTau { get; init; } = new();
        public List<DauMayBaoTriDTO> DauMay { get; init; } = new();
        public List<ToaXeBaoTriDTO> ToaXe { get; init; } = new();
        public List<TaiKhoanKyThuatDTO> NguoiKyThuat { get; init; } = new();
        public List<KhamXeDTO> KhamXe { get; init; } = new();
        public List<CapNhienLieuDTO> CapDau { get; init; } = new();
        public List<BaoDuongDTO> BaoDuong { get; init; } = new();
        public List<string> NoiCapDau { get; init; } = new();
        public DateTime BayGio { get; init; }

        // Cho tab Chi phi (chi doc khi trang nap; Service nap lai trong giao dich thi de rong)
        public List<MacTauBaoTriDTO> MacTau { get; init; } = new();
        public List<DoanhThuChuyenDTO> DoanhThuChuyen { get; init; } = new();
        public List<VanDonDoanhThuDTO> VanDon { get; init; } = new();

        public static DuLieuBaoTri Rong(DateTime bayGio) => new() { BayGio = bayGio };

        public DoanTauBaoTriDTO? TimDoanTau(int maDoanTau) => DoanTau.FirstOrDefault(d => d.MaDoanTau == maDoanTau);
        public DoanTauBaoTriDTO? TimDoanTauTheoChuyen(int maChuyenTau) => DoanTau.FirstOrDefault(d => d.MaChuyenTau == maChuyenTau);
        public DauMayBaoTriDTO? TimDauMay(int maDauMay) => DauMay.FirstOrDefault(d => d.MaDauMay == maDauMay);
        public ToaXeBaoTriDTO? TimToaXe(int maToaXe) => ToaXe.FirstOrDefault(t => t.MaToaXe == maToaXe);
        public TaiKhoanKyThuatDTO? TimNguoiKyThuat(int? maTaiKhoan) => NguoiKyThuat.FirstOrDefault(n => n.MaTaiKhoan == maTaiKhoan);

        // Bien ban moi nhat cua doan tau (null = chua kham)
        public KhamXeDTO? BienBanMoiNhat(int maDoanTau)
            => KhamXe.Where(k => k.MaDoanTau == maDoanTau)
                     .OrderByDescending(k => k.ThoiDiemKham).ThenByDescending(k => k.MaKhamXe)
                     .FirstOrDefault();

        public IEnumerable<BaoDuongDTO> LichSuBaoDuong(string loaiPhuongTien, int maPhuongTien)
            => BaoDuong.Where(b => b.LoaiPhuongTien == loaiPhuongTien && b.MaPhuongTien == maPhuongTien);

        // Hai phieu cap dau gan nhat cua dau may (moi nhat truoc)
        public List<CapNhienLieuDTO> CapDauGanNhat(int maDauMay, int soPhieu = 2)
            => CapDau.Where(c => c.MaDauMay == maDauMay)
                     .OrderByDescending(c => c.ThoiDiemBomDau).ThenByDescending(c => c.MaNhatKyDau)
                     .Take(soPhieu).ToList();

        // Doan tau chua ket thuc co dau may nay (chinh hoac day), som nhat truoc
        public List<DoanTauBaoTriDTO> DoanTauCuaDauMay(int maDauMay, bool chiChuaXuatPhat)
            => DoanTau.Where(d => (d.MaDauMayChinh == maDauMay || d.MaDauMayDay == maDauMay) && !d.DaKetThuc
                                  && (!chiChuaXuatPhat || !d.DaXuatPhat))
                      .OrderBy(d => d.GioXuatPhatKH).ToList();
    }

    // Tien do mot cap bao duong cua mot phuong tien. Dau may tinh theo km, toa xe theo ngay.
    // DaDung = km da chay / so ngay ke tu lan gan nhat lam moi cap nay; null = chua co moc (toa chua co lich su).
    // TrangThai: QUA_HAN | SAP_TOI | BINH_THUONG | CHUA_CO
    public record TienDoBaoDuongDTO(string Cap, bool TheoKm, decimal ChuKy, decimal? DaDung, decimal? MocKeTiep,
                                    BaoDuongDTO? LanGanNhat, decimal TyLe, string TrangThai)
    {
        public int MucNghiemTrong => TrangThai switch { "QUA_HAN" => 3, "SAP_TOI" => 2, "BINH_THUONG" => 1, _ => 0 };
        public decimal? ConLai => DaDung.HasValue ? ChuKy - DaDung.Value : null;
    }

    // =========================================================================
    // XUAT BEN (hop dong KE_HOACH_BACK_END.md §7.2) va duyet an toan (UC-BT-03)
    // MucDo: DAT | LUU_Y | CHAN. Nhom: DOAN_TAU | KHAM_XE | DUYET | BAO_DUONG | NHIEN_LIEU | KIP_LAI
    // =========================================================================
    public record MucXuatBenDTO(string Nhom, string Ten, string KetQua, string MucDo);

    public class KetQuaXuatBen
    {
        public int MaChuyenTau { get; init; }
        public List<MucXuatBenDTO> CacMuc { get; init; } = new();

        public bool DuocXuatBen => CacMuc.All(m => m.MucDo != "CHAN");
        public List<string> LyDoChan => CacMuc.Where(m => m.MucDo == "CHAN").Select(m => $"{m.Ten}: {m.KetQua}").ToList();
        public List<string> LuuY => CacMuc.Where(m => m.MucDo == "LUU_Y").Select(m => $"{m.Ten}: {m.KetQua}").ToList();

        // Mot cau de hien len giao dien khi bi chan
        public string MoTaChan => DuocXuatBen ? ""
            : "Chưa được xuất bến:\n" + string.Join("\n", LyDoChan.Select(l => "• " + l));
    }

    // Dieu kien duyet an toan: chi phan KY THUAT (doan tau, kham xe sau lan sua doan tau, bao duong dau may).
    // Kip lai xet luc xuat ben (KiemTraXuatBenService), khong xet o day.
    public class KetQuaDuyetAnToan
    {
        public List<MucXuatBenDTO> CacMuc { get; init; } = new();
        public string? LyDoKhongDuyet { get; init; }   // ly do chung (chuyen da chay, da duyet roi...)

        public bool CoTheDuyet => LyDoKhongDuyet == null && CacMuc.All(m => m.MucDo != "CHAN");
        public string MoTaChan => LyDoKhongDuyet
            ?? string.Join("\n", CacMuc.Where(m => m.MucDo == "CHAN").Select(m => $"• {m.Ten}: {m.KetQua}"));
    }

    // Ket qua cac thao tac ghi (de giao dien bao lai dung dieu da xay ra)
    public record KetQuaLapBienBan(int MaKhamXe, DateTime ThoiDiemKham, bool DuDieuKien, bool DaBoDuyet);
    public record KetQuaGhiBaoDuong(int MaBaoDuong, DateTime ThoiDiemHoanThanh, string? TrangThaiCu, string? TrangThaiMoi);
}
