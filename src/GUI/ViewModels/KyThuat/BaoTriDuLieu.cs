using DTO.BaoTri;

namespace GUI.ViewModels.KyThuat
{
    // =========================================================================
    // ANH CHUP DU LIEU BAO TRI DA DOI SANG LOP HIEN THI (24/10, thay DanhMucMauBaoTri cua giai doan
    // giao dien). Trang tao moi moi lan nap lai (F5, sau moi lan luu); dialog, tab Canh bao, tab Chi phi
    // nhan cung mot doi tuong nen 5 tab luon khop nhau.
    // =========================================================================
    public class BaoTriDuLieu
    {
        public BaoTriDuLieu(DuLieuBaoTri goc)
        {
            Goc = goc;
            var tenNguoi = goc.NguoiKyThuat.ToDictionary(n => n.MaTaiKhoan, n => n.HoTenHienThi);
            string TenNguoi(int? ma) => ma.HasValue && tenNguoi.TryGetValue(ma.Value, out var ten) ? ten : "";

            DoanTau = goc.DoanTau.Select(d => new DoanTauKhamXe(d)).ToList();
            var doanTheoMa = DoanTau.ToDictionary(d => d.MaDoanTau);

            DauMay = goc.DauMay.Select(d => new DauMayCapDau(d.MaDauMay, d.SoHieuDauMay, d.MaDongCode, d.DonViQuanLy,
                                                             d.TrangThai, d.DungTichBonDauLit, d.DinhMucSFC, d.CongSuatHP,
                                                             d.SoKmTichLuy)).ToList();
            var dauMayTheoMa = DauMay.ToDictionary(d => d.MaDauMay);

            PhuongTien = DauMay.Select(d => new PhuongTienBaoDuong("DAU_MAY", d.MaDauMay, d.SoHieuDauMay,
                                    $"{d.MaDongCode} · {d.DonViQuanLy.Replace("Xí nghiệp Đầu máy", "XN")}",
                                    d.SoKmTichLuy, d.TrangThai, d.TrangThai == "DANG_CHAY"))
                .Concat(goc.ToaXe.Select(t => new PhuongTienBaoDuong("TOA_XE", t.MaToaXe, t.SoHieuToaXe,
                                    DanhMucMauPhuongTien.TimChungLoai(t.MaChungLoaiCode).TenNgan,
                                    0m, t.TrangThai, t.DangChay)))
                .ToList();
            var ptTheoKhoa = PhuongTien.ToDictionary(p => p.Khoa);

            NguoiKyThuat = goc.NguoiKyThuat.Where(n => n.ChonDuoc)
                              .Select(n => new TaiKhoanMau(n.MaTaiKhoan, n.TenDangNhap, n.HoTenHienThi, n.DonVi))
                              .ToList();

            KhamXe = goc.KhamXe.Select(k =>
            {
                var bb = new KhamXeHienThi
                {
                    MaKhamXe = k.MaKhamXe, MaNguoiKham = k.MaNguoiKham, ApLucHamBar = k.ApLucHamBar,
                    DoSutApBarPhut = k.DoSutApBarPhut, DaCapNuoc = k.DaCapNuoc, DaXaVeSinh = k.DaXaVeSinh,
                    GhiChuKyThuat = k.GhiChuKyThuat, ThoiDiemKham = k.ThoiDiemKham, ChiPhi = k.ChiPhi,
                    TenNguoiKham = TenNguoi(k.MaNguoiKham)
                };
                if (doanTheoMa.TryGetValue(k.MaDoanTau, out var dt)) bb.GanDoanTau(dt);
                else bb.MaDoanTau = k.MaDoanTau;
                return bb;
            }).ToList();

            CapDau = goc.CapDau.Select(c =>
            {
                var nl = new CapNhienLieuHienThi
                {
                    MaNhatKyDau = c.MaNhatKyDau, NoiCapDau = c.NoiCapDau, SoLitTraNap = c.SoLitTraNap,
                    TrongLuongKeoTan = c.TrongLuongKeoTan, CuLyChayKm = c.CuLyChayKm, SuatTieuHaoSFC = c.SuatTieuHaoSFC,
                    CanhBaoVuotMuc = c.CanhBaoVuotMuc, MaNguoiCap = c.MaNguoiCap, ThoiDiemBomDau = c.ThoiDiemBomDau,
                    DonGiaVndLit = c.DonGiaVndLit, ChiPhi = c.ChiPhi, PhienBan = c.PhienBan,
                    TenNguoiCap = TenNguoi(c.MaNguoiCap)
                };
                if (dauMayTheoMa.TryGetValue(c.MaDauMay, out var dm)) nl.GanDauMay(dm);
                else nl.MaDauMay = c.MaDauMay;
                return nl;
            }).ToList();

            BaoDuong = goc.BaoDuong.Select(b =>
            {
                var bd = new BaoDuongHienThi
                {
                    MaBaoDuong = b.MaBaoDuong, LoaiPhuongTien = b.LoaiPhuongTien, MaDauMay = b.MaDauMay, MaToaXe = b.MaToaXe,
                    CapBaoDuong = b.CapBaoDuong, SoKmTaiThoiDiem = b.SoKmTaiThoiDiem, GhiChuKyThuat = b.GhiChuKyThuat,
                    MaNguoiThucHien = b.MaNguoiThucHien, ThoiDiemHoanThanh = b.ThoiDiemHoanThanh, ChiPhi = b.ChiPhi,
                    PhienBan = b.PhienBan, TenNguoiThucHien = TenNguoi(b.MaNguoiThucHien)
                };
                if (ptTheoKhoa.TryGetValue(b.KhoaPhuongTien, out var pt)) bd.GanPhuongTien(pt);
                return bd;
            }).ToList();
        }

        public DuLieuBaoTri Goc { get; }
        public DateTime BayGio => Goc.BayGio;

        public List<DoanTauKhamXe> DoanTau { get; }
        public List<DauMayCapDau> DauMay { get; }
        public List<PhuongTienBaoDuong> PhuongTien { get; }
        public List<TaiKhoanMau> NguoiKyThuat { get; }        // chi tai khoan chon duoc (KY_THUAT, dang hoat dong)
        public List<KhamXeHienThi> KhamXe { get; }
        public List<CapNhienLieuHienThi> CapDau { get; }
        public List<BaoDuongHienThi> BaoDuong { get; }
        public IReadOnlyList<string> NoiCapDau => Goc.NoiCapDau;

        // Doan tau chua ket thuc (the tinh trang, danh sach lap bien ban, san sang xuat ben)
        public List<DoanTauKhamXe> DoanTauDangDieuHanh
            => DoanTau.Where(d => !d.DaKetThuc).OrderBy(d => d.GioXuatPhatKH).ThenBy(d => d.MaDoanTau).ToList();

        // Dau may con khai thac (the nhien lieu / bao duong, danh sach cap dau)
        public List<DauMayCapDau> DauMayKhaiThac => DauMay.Where(d => !d.NgungKhaiThac).ToList();

        public DoanTauKhamXe? TimDoanTau(int maDoanTau) => DoanTau.FirstOrDefault(d => d.MaDoanTau == maDoanTau);
        public DauMayCapDau? TimDauMay(int maDauMay) => DauMay.FirstOrDefault(d => d.MaDauMay == maDauMay);
        public PhuongTienBaoDuong? TimPhuongTien(string khoa) => PhuongTien.FirstOrDefault(p => p.Khoa == khoa);
        public TaiKhoanMau? TimNguoiKyThuat(int? maTaiKhoan) => NguoiKyThuat.FirstOrDefault(n => n.MaTaiKhoan == maTaiKhoan);
    }
}
