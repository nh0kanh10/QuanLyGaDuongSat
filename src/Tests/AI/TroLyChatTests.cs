using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BUS.AI;
using DTO.AI;

namespace Tests.AI
{
    // [16/11] Tro chuyen AI (function calling) + dat ve ho: kenh GIA, bo cong cu GIA -> khong goi mang, khong ton phi, khong can CSDL.
    public class QuyTacMoHinhNgonNguTests
    {
        internal static readonly DateTime BayGio = new(2026, 11, 16, 14, 5, 0);   // thu Hai

        internal static readonly List<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)> Ga = new()
        {
            (1, "HNI", "Hà Nội", 0m),
            (5, "VIN", "Vinh", 319m),
            (9, "DAN", "Đà Nẵng", 791m),
            (15, "SGO", "Sài Gòn", 1726m),
        };

        internal static readonly Dictionary<string, decimal> TyLe = new() { ["TRE_EM"] = 0.25m, ["NGUOI_GIA"] = 0.15m, ["SINH_VIEN"] = 0.10m };

        static JsonElement J(object o) => JsonSerializer.SerializeToElement(o);

        static object ThamSoTuVan(string gaDi = "HNI", string gaDen = "DAN", string ngay = "2026-11-20", int soNguoi = 2,
                                  int nganSach = 1_000_000, string khungGio = "NGU_TREN_TAU", string muc = "TIEU_CHUAN")
            => new
            {
                ga_di = gaDi, ga_den = gaDen, ngay, so_nguoi = soNguoi, co_nguoi_cao_tuoi = true, co_tre_em = false, la_sinh_vien = false,
                muc_ngan_sach = muc, ngan_sach_toi_da_moi_ve = nganSach, khung_gio = khungGio
            };

        // --- Che du lieu ca nhan ---------------------------------------------------
        [Theory]
        [InlineData("CCCD 012345678901 nhé", "CCCD [số đã ẩn] nhé")]
        [InlineData("sđt 0912 345 678", "sđt [số đã ẩn]")]
        [InlineData("số 0912.345.678 gọi lại", "số [số đã ẩn] gọi lại")]
        [InlineData("tối đa 1.200.000 một vé", "tối đa 1.200.000 một vé")]
        [InlineData("2 người ngày 20/11", "2 người ngày 20/11")]
        public void AnDuLieuCaNhan_CheDaySoDai_GiuGiaVe(string vao, string ra)
            => Assert.Equal(ra, QuyTacMoHinhNgonNgu.AnDuLieuCaNhan(vao));

        // --- Loi nhac + cong cu ---------------------------------------------------------
        [Fact]
        public void LoiNhacHeThong_CoGa_GiamGia_NgayGio()
        {
            string s = QuyTacMoHinhNgonNgu.LoiNhacHeThong(Ga, TyLe, BayGio);
            Assert.Contains("DAN: Đà Nẵng", s);
            Assert.Contains("trẻ em 25%", s);
            Assert.Contains("14:05 thứ Hai 16/11/2026 (2026-11-16)", s);
            Assert.Contains("dat_ve", s);
        }

        [Fact]
        public void DanhSachCongCu_Du6_SchemaStrict()
        {
            var ds = QuyTacMoHinhNgonNgu.DanhSachCongCu(Ga.Select(g => g.MaGaCode));
            Assert.Equal(new[] { "ds_mac_tau", "tim_chuyen_tau", "lich_trinh_chuyen", "xem_cho_trong_va_gia", "tu_van_phuong_an", "dat_ve" },
                         ds.Select(c => c.Ten).ToArray());
            foreach (var c in ds)
            {
                var props = c.SchemaThamSo["properties"].EnumerateObject().Select(p => p.Name).ToHashSet();
                var batBuoc = c.SchemaThamSo["required"].EnumerateArray().Select(x => x.GetString()!).ToHashSet();
                Assert.True(props.SetEquals(batBuoc), c.Ten);
                Assert.False(c.SchemaThamSo["additionalProperties"].GetBoolean());
                Assert.False(string.IsNullOrWhiteSpace(c.MoTa));
            }
            var enumGa = ds.Single(c => c.Ten == "tim_chuyen_tau").SchemaThamSo["properties"].GetProperty("ga_di").GetProperty("enum")
                           .EnumerateArray().Select(x => x.GetString()).ToList();
            Assert.Equal(4, enumGa.Count);
            Assert.Contains("SGO", enumGa);
        }

        // --- Doc tham so tu van ------------------------------------------------------------
        [Fact]
        public void DocYeuCauTuVan_HopLe()
        {
            Assert.True(QuyTacMoHinhNgonNgu.DocYeuCauTuVan(J(ThamSoTuVan()), Ga, BayGio, out var yc, out string loi), loi);
            Assert.Equal(1, yc.MaGaDi);
            Assert.Equal(9, yc.MaGaDen);
            Assert.Equal(new DateTime(2026, 11, 20), yc.NgayDi);
            Assert.Equal(2, yc.SoNguoi);
            Assert.True(yc.CoNguoiCaoTuoi);
            Assert.Equal(1_000_000m, yc.NganSachToiDaMoiNguoi);
            Assert.Equal(KhungGioUuTien.NguTrenTau, yc.KhungGio);
        }

        [Theory]
        [InlineData("XYZ", "DAN", "2026-11-20", 2, 0, "Không có ga mã")]
        [InlineData("DAN", "DAN", "2026-11-20", 2, 0, "trùng nhau")]
        [InlineData("HNI", "DAN", "2026-11-10", 2, 0, "đã qua")]
        [InlineData("HNI", "DAN", "20/11/2026", 2, 0, "không đúng dạng")]
        [InlineData("HNI", "DAN", "2026-11-20", 9, 0, "tách đơn")]
        [InlineData("HNI", "DAN", "2026-11-20", 0, 0, "Số khách")]
        [InlineData("HNI", "DAN", "2026-11-20", 2, 500, "Ngân sách")]
        public void DocYeuCauTuVan_Sai_BaoLoiChoMoHinh(string gaDi, string gaDen, string ngay, int soNguoi, int nganSach, string loiMongDoi)
        {
            Assert.False(QuyTacMoHinhNgonNgu.DocYeuCauTuVan(J(ThamSoTuVan(gaDi, gaDen, ngay, soNguoi, nganSach)), Ga, BayGio, out _, out string loi));
            Assert.Contains(loiMongDoi, loi);
        }

        [Fact]
        public void DocYeuCauTuVan_NgayHomNay_NganSach0_EnumLa()
        {
            Assert.True(QuyTacMoHinhNgonNgu.DocYeuCauTuVan(J(ThamSoTuVan(ngay: "2026-11-16", nganSach: 0, khungGio: "BUOI_SANG", muc: "XYZ")),
                                                            Ga, BayGio, out var yc, out _));
            Assert.Null(yc.NganSachToiDaMoiNguoi);
            Assert.Equal(KhungGioUuTien.KhongUuTien, yc.KhungGio);
            Assert.Equal(MucNganSach.TieuChuan, yc.MucNganSach);
        }

        [Theory]
        [InlineData("không phải json")]
        [InlineData("[1,2]")]
        public void DocJson_Hong_TraLoi(string s)
        {
            Assert.False(QuyTacMoHinhNgonNgu.DocJson(s, out _, out string loi));
            Assert.NotEmpty(loi);
        }

        // --- Doi chieu so lieu ----------------------------------------------------------------
        const string DuLieu = "{ \"gia_mot_ve\": \"1.264.000 đ\", \"tong_tien_ca_doan\": \"2.528.000 đ\", \"cu_ly_km\": \"1.726\", " +
                              "\"ngay_di\": \"thứ Sáu 20/11/2026\", \"di\": \"19:00 ngày 20/11\", \"so_gio_chay\": \"32,5\" }";

        [Fact]
        public void KiemSoLieu_SoCoTrongDuLieu_Khop()
        {
            var kq = QuyTacMoHinhNgonNgu.KiemSoLieu(
                "Anh/chị nên chọn giường tầng 1 tàu SE3, giá 1.264.000 đ một vé, cả đoàn 2.528.000 đ, chạy 1.726 km khoảng 32,5 giờ, " +
                "khởi hành 19:00 ngày 20/11/2026. Đoàn có 2 trẻ em.", DuLieu);
            Assert.True(kq.KhopHet, string.Join(",", kq.SoLieuLa));
        }

        [Theory]
        [InlineData("Giá chỉ 1.100.000 đ thôi.", "1.100.000")]
        [InlineData("Khoảng 1,2 triệu một vé.", "1,2 triệu")]
        [InlineData("Chỉ 800k.", "800k")]
        public void KiemSoLieu_SoLa_BaoRa(string traLoi, string soLa)
        {
            var kq = QuyTacMoHinhNgonNgu.KiemSoLieu(traLoi, DuLieu);
            Assert.False(kq.KhopHet);
            Assert.Contains(soLa, kq.SoLieuLa);
        }

        [Fact]
        public void KiemSoLieu_NhacLaiCumTuKhachNoi_KhongBao()
        {
            // Lan chay that 16/11: AI nhac lai "ngân sách 1 triệu" cua khach -> truoc day bi bao "chua doi chieu".
            var kq = QuyTacMoHinhNgonNgu.KiemSoLieu("Phương án 1 không vượt ngân sách 1 triệu.",
                                                    "Hai ông bà đi Đà Nẵng, tối đa 1 TRIỆU một vé. " + DuLieu);
            Assert.True(kq.KhopHet, string.Join(",", kq.SoLieuLa));
            Assert.False(QuyTacMoHinhNgonNgu.KiemSoLieu("Khoảng 2 triệu cả đoàn.", "tối đa 1 triệu một vé").KhopHet);
        }

        // --- Cat lich su -----------------------------------------------------------------------
        static List<TinNhanChat> LichSuMau(int soLuot)
        {
            var ds = new List<TinNhanChat>();
            for (int i = 0; i < soLuot; i++)
            {
                ds.Add(TinNhanChat.NguoiDung($"hỏi {i}"));
                ds.Add(TinNhanChat.TroLy("", new[] { new LenhGoiCongCu($"c{i}", "tim_chuyen_tau", "{}") }));
                ds.Add(TinNhanChat.KetQua($"c{i}", "kết quả"));
                ds.Add(TinNhanChat.TroLy($"đáp {i}"));
            }
            return ds;
        }

        [Fact]
        public void CatLichSu_Ngan_GiuNguyen()
            => Assert.Equal(8, QuyTacMoHinhNgonNgu.CatLichSu(LichSuMau(2), 40).Count);

        [Fact]
        public void CatLichSu_Dai_BatDauOCauHoi_KhongMoCoi()
        {
            var cat = QuyTacMoHinhNgonNgu.CatLichSu(LichSuMau(15), 10);
            Assert.True(cat.Count <= 10);
            Assert.Equal(VaiTroTinNhan.NguoiDung, cat[0].VaiTro);
            Assert.Equal("đáp 14", cat[^1].NoiDung);
            var maLenh = cat.SelectMany(t => t.CacLenh).Select(l => l.Ma).ToHashSet();
            Assert.All(cat.Where(t => t.VaiTro == VaiTroTinNhan.KetQuaCongCu), t => Assert.Contains(t.MaLenh, maLenh));
        }

        [Fact]
        public void BoMarkdown_BoDauSao_TieuDe()
            => Assert.Equal("Nên chọn SE3\nGiá rẻ", QuyTacMoHinhNgonNgu.BoMarkdown("## Nên chọn **SE3**\nGiá rẻ"));

        [Fact]
        public void KetQuaTuVanChoMoHinh_KhongCoMaGhe_SoQuaDuocDoiChieu()
        {
            var (yc, kq) = TroLyChatServiceTests.TuVanMau();
            string s = QuyTacMoHinhNgonNgu.KetQuaTuVanChoMoHinh(yc, kq);
            Assert.Contains("1.264.000 đ", s);
            Assert.Contains("so_thu_tu", s);
            Assert.DoesNotContain("501", s);    // MaChoNgoi noi bo khong gui
            Assert.True(QuyTacMoHinhNgonNgu.KiemSoLieu("Nên chọn SE3 giá 1.264.000 đ, rẻ hơn thì ghế mềm 812.000 đ.", s).KhopHet);
        }
    }

    public class DatVeHoServiceTests
    {
        static decimal TyLe(string loai) => QuyTacMoHinhNgonNguTests.TyLe.TryGetValue(loai, out var t) ? t : 0m;

        static List<HanhKhachDatVe> DsHopLe() => new()
        {
            new() { MaChoNgoi = 501, SoGhe = 1, HoTen = "Nguyễn Văn An", Cccd = "001080000001", LoaiKhach = "NGUOI_GIA", GiaGoc = 1_264_000m, SoTienGiam = 189_600m },
            new() { MaChoNgoi = 502, SoGhe = 2, HoTen = "Trần Thị Bình", Cccd = "001080000002", GiaGoc = 1_264_000m },
        };

        [Fact]
        public void TaoHanhKhach_TheoGheVaDoiTuong_GiamLamTron()
        {
            var (_, kq) = TroLyChatServiceTests.TuVanMau();
            var pa = kq.CacPhuongAn[0];
            pa.CacMaChoNgoi = new() { 501, 502 };
            pa.CacSoGhe = new() { 1, 2 };
            pa.CacLoaiKhach = new() { "NGUOI_GIA", "THUONG" };
            var ds = DatVeHoService.TaoHanhKhach(pa, TyLe);
            Assert.Equal(2, ds.Count);
            Assert.Equal(189_600m, ds[0].SoTienGiam);       // 1.264.000 x 15%
            Assert.Equal(1_074_400m, ds[0].ThucThu);
            Assert.Equal(0m, ds[1].SoTienGiam);
            Assert.Equal(2, ds[1].SoGhe);
        }

        [Fact]
        public void KiemTra_HopLe_KhongLoi()
            => Assert.Empty(DatVeHoService.KiemTra("Nguyễn Văn An", "0912345678", DsHopLe()));

        [Theory]
        [InlineData("", "0912345678", "họ tên người mua")]
        [InlineData("An", "", "số điện thoại")]
        [InlineData("An", "912345678", "10 chữ số")]
        public void KiemTra_NguoiMuaSai(string ten, string sdt, string loi)
            => Assert.Contains(DatVeHoService.KiemTra(ten, sdt, DsHopLe()), x => x.Contains(loi));

        [Fact]
        public void KiemTra_HanhKhachSai()
        {
            var ds = DsHopLe();
            ds[0].HoTen = " ";
            ds[1].Cccd = "00108000002";
            var loi = DatVeHoService.KiemTra("An", "0912345678", ds);
            Assert.Contains(loi, x => x.Contains("Vé 1") && x.Contains("họ tên"));
            Assert.Contains(loi, x => x.Contains("Vé 2") && x.Contains("12 chữ số"));
        }

        [Fact]
        public void KiemTra_TrungCccd_QuaSoVe()
        {
            var ds = DsHopLe();
            ds[1].Cccd = ds[0].Cccd;
            Assert.Contains(DatVeHoService.KiemTra("An", "0912345678", ds), x => x.Contains("nhiều hành khách"));
            var nhieu = Enumerable.Range(1, 9).Select(i => new HanhKhachDatVe { SoGhe = i, HoTen = "A B", Cccd = $"0010800000{i:00}" }).ToList();
            Assert.Contains(DatVeHoService.KiemTra("An", "0912345678", nhieu), x => x.Contains("tối đa 8 vé"));
        }

        [Fact]
        public void LapDon_GiongManBanVe()
        {
            var (yc, kq) = TroLyChatServiceTests.TuVanMau();
            var dv = new YeuCauDatVeHo { PhuongAn = kq.CacPhuongAn[0], YeuCau = yc, TenGaDi = "Hà Nội", TenGaDen = "Sài Gòn" };
            var (khach, don, ve) = DatVeHoService.LapDon(dv, " Trần Thị Bình ", "0912345678", "CHUYEN_KHOAN", DsHopLe(), 7);
            Assert.Equal("Trần Thị Bình", khach.HoTen);
            Assert.Equal("001080000002", khach.SoCCCD);             // CCCD cua hanh khach trung ten nguoi mua
            Assert.Equal(2, don.SoLuongVe);
            Assert.Equal(1_074_400m + 1_264_000m, don.TongTien);
            Assert.Equal("DA_THANH_TOAN", don.TrangThaiTT);
            Assert.Equal("CHUYEN_KHOAN", don.HinhThucTT);
            Assert.Equal(7, don.MaNhanVienBan);
            Assert.All(ve, v => { Assert.Equal(1, v.MaGaDi); Assert.Equal(15, v.MaGaDen); Assert.Equal(kq.CacPhuongAn[0].MaChuyenTau, v.MaChuyenTau); });
            Assert.Equal(1_074_400m, ve[0].GiaVeThucThu);
        }
    }

    [Collection("PhienLamViec")]
    public class TroLyChatServiceTests
    {
        // Kenh gia: moi lan goi lay mot "kich ban" theo thu tu, ghi lai lich su da nhan.
        sealed class KenhGia : IKenhMoHinhNgonNgu
        {
            private readonly Queue<Func<IReadOnlyList<TinNhanChat>, PhanHoiMoHinh>> _kichBan;
            public KenhGia(params Func<IReadOnlyList<TinNhanChat>, PhanHoiMoHinh>[] kichBan) => _kichBan = new(kichBan);
            public Func<IReadOnlyList<TinNhanChat>, PhanHoiMoHinh>? MacDinh { get; set; }
            public string TenMoHinh => "mo-hinh-gia";
            public List<IReadOnlyList<TinNhanChat>> CacLanGoi { get; } = new();
            public Task<PhanHoiMoHinh> TroChuyenAsync(string heThong, IReadOnlyList<TinNhanChat> lichSu, IReadOnlyList<CongCuMoHinh> congCu, CancellationToken ct)
            {
                CacLanGoi.Add(lichSu.ToList());
                var f = _kichBan.Count > 0 ? _kichBan.Dequeue() : MacDinh ?? throw new InvalidOperationException("Hết kịch bản");
                return Task.FromResult(f(lichSu));
            }
        }

        sealed class CongCuGia : IBoCongCuTroLy
        {
            public List<string> DaGoi { get; } = new();
            public IReadOnlyList<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)> DanhSachGa() => QuyTacMoHinhNgonNguTests.Ga;
            public IReadOnlyDictionary<string, decimal> TyLeGiamGia() => QuyTacMoHinhNgonNguTests.TyLe;
            public KetQuaCongCu ThucThi(string ten, JsonElement thamSo, DateTime bayGio)
            {
                DaGoi.Add(ten);
                if (ten == "tu_van_phuong_an")
                {
                    var (yc, kq) = TuVanMau();
                    return new KetQuaCongCu { ChoMoHinh = QuyTacMoHinhNgonNgu.KetQuaTuVanChoMoHinh(yc, kq), MoTa = "Tư vấn: 2 phương án", TuVan = kq, YeuCau = yc };
                }
                return new KetQuaCongCu { ChoMoHinh = "{\"cac_chuyen\":[{\"mac_tau\":\"SE3\",\"gia_ghe_mem\":\"812.000 đ\"}]}", MoTa = "Tìm chuyến: 1 chuyến" };
            }
        }

        static PhanHoiMoHinh Chu(string s) => new(s, Array.Empty<LenhGoiCongCu>());
        static PhanHoiMoHinh Goi(string ten, string thamSo = "{}", string ma = "call_1") => new("", new[] { new LenhGoiCongCu(ma, ten, thamSo) });

        static (TroLyChatService Sv, KenhGia Kenh, CongCuGia CongCu, List<(string Hoi, string TraLoi, string? Sql)> NhatKy) Tao(KenhGia kenh)
        {
            var cc = new CongCuGia();
            var nk = new List<(string, string, string?)>();
            var sv = new TroLyChatService(kenh, cc, (h, t, s) => nk.Add((h, t, s)), () => QuyTacMoHinhNgonNguTests.BayGio);
            return (sv, kenh, cc, nk);
        }

        internal static (YeuCauTuVanVe, KetQuaTuVanVe) TuVanMau()
        {
            var yc = new YeuCauTuVanVe { MaGaDi = 1, MaGaDen = 15, NgayDi = new DateTime(2026, 11, 20), SoNguoi = 1 };
            var kq = new KetQuaTuVanVe { TenGaDi = "Hà Nội", TenGaDen = "Sài Gòn", CuLyKm = 1726m };
            kq.CacPhuongAn.Add(new PhuongAnVe
            {
                XepHang = 1, MaChuyenTau = 77, SoHieuMacTau = "SE3", LoaiTau = "TAU_NHANH", LoaiToa = "BN", Tang = 1, NhanHieuToa = "Toa 7",
                CacMaChoNgoi = { 501 }, CacSoGhe = { 1 }, GiaMotVe = 1_264_000m,
                GioDi = new DateTime(2026, 11, 20, 19, 0, 0), GioDen = new DateTime(2026, 11, 22, 3, 30, 0),
                CuLyKm = 1726m, QuaDem = true, Diem = 8.35, LyDo = { "Chặng dài 1.726 km." }
            });
            kq.CacPhuongAn.Add(new PhuongAnVe
            {
                XepHang = 2, MaChuyenTau = 77, SoHieuMacTau = "SE3", LoaiTau = "TAU_NHANH", LoaiToa = "NML", NhanHieuToa = "Toa 2",
                CacMaChoNgoi = { 777 }, CacSoGhe = { 30 }, GiaMotVe = 812_000m,
                GioDi = new DateTime(2026, 11, 20, 19, 0, 0), GioDen = new DateTime(2026, 11, 22, 3, 30, 0), CuLyKm = 1726m, QuaDem = true, Diem = 5.10
            });
            return (yc, kq);
        }

        [Fact]
        public async Task CauHoiThuong_1LanGoi_KhongTraDuLieu()
        {
            var (sv, kenh, cc, nk) = Tao(new KenhGia(_ => Chu("Em không có dữ liệu thời tiết thời gian thực ạ.")));
            var kq = await sv.GuiAsync("hôm nay có mưa không");
            Assert.True(kq.ThanhCong, kq.Loi);
            Assert.Equal(1, kq.SoLanGoiMoHinh);
            Assert.Empty(kq.CacBuocTraCuu);
            Assert.Empty(cc.DaGoi);
            Assert.Equal(2, sv.LichSu.Count);
            Assert.Null(nk.Single().Sql);
        }

        [Fact]
        public async Task CanDuLieu_GoiCongCu_RoiLan2KemKetQua()
        {
            var (sv, kenh, cc, nk) = Tao(new KenhGia(
                _ => Goi("tim_chuyen_tau", "{\"ga_di\":\"HNI\",\"ga_den\":\"DAN\",\"ngay\":\"2026-11-20\"}"),
                ls => Chu("Có tàu SE3, ghế mềm 812.000 đ.")));
            var kq = await sv.GuiAsync("mai có tàu đi Đà Nẵng không");
            Assert.True(kq.ThanhCong, kq.Loi);
            Assert.Equal(2, kq.SoLanGoiMoHinh);
            Assert.Equal(new[] { "tim_chuyen_tau" }, cc.DaGoi);
            Assert.Equal("Tìm chuyến: 1 chuyến", kq.CacBuocTraCuu.Single());
            var lan2 = kenh.CacLanGoi[1];
            Assert.Equal(VaiTroTinNhan.KetQuaCongCu, lan2[^1].VaiTro);
            Assert.Equal("call_1", lan2[^1].MaLenh);
            Assert.Empty(kq.SoLieuChuaDoiChieu);
            Assert.Contains("tim_chuyen_tau", nk.Single().Sql);
            Assert.Equal(4, sv.LichSu.Count);
        }

        [Fact]
        public async Task NhoHoiThoai_LanSauGuiKemCauTruoc()
        {
            var (sv, kenh, _, _) = Tao(new KenhGia(_ => Chu("Dạ Hà Nội đi Vinh ạ."), _ => Chu("Dạ vẫn là ngày mai ạ.")));
            await sv.GuiAsync("tôi muốn đi Vinh");
            await sv.GuiAsync("vẫn ngày đó nhé");
            var lan2 = kenh.CacLanGoi[1];
            Assert.Equal(3, lan2.Count);
            Assert.Equal("tôi muốn đi Vinh", lan2[0].NoiDung);
        }

        [Fact]
        public async Task CheSo_TrongLichSu_VaNhatKy()
        {
            var (sv, kenh, _, nk) = Tao(new KenhGia(_ => Chu("Dạ.")));
            await sv.GuiAsync("khách sđt 0912345678 đi Vinh");
            Assert.DoesNotContain("0912345678", kenh.CacLanGoi[0][0].NoiDung);
            Assert.DoesNotContain("0912345678", nk.Single().Hoi);
        }

        [Fact]
        public async Task TuVan_CapNhatPhuongAn_DatVe_XacNhan()
        {
            var (sv, kenh, _, _) = Tao(new KenhGia(
                _ => Goi("tu_van_phuong_an", JsonSerializer.Serialize(new { ga_di = "HNI" })),
                _ => Chu("Nên chọn phương án 1 giá 1.264.000 đ."),
                _ => Goi("dat_ve", "{\"phuong_an\":1}", "call_2"),
                _ => Chu("Đã đặt xong ạ.")));
            YeuCauDatVeHo? daMo = null;
            sv.XacNhanDatVe = yc =>
            {
                daMo = yc;
                return Task.FromResult(new KetQuaDatVeHo { DaDat = true, MaPnr = "PNR001", SoVe = 1, TongTien = 1_264_000m, CacMaVe = { "VE001" }, HinhThucTT = "TIEN_MAT" });
            };
            var kq1 = await sv.GuiAsync("hn đi sg ngày 20/11 một người");
            Assert.NotNull(kq1.TuVan);
            Assert.Same(kq1.TuVan, sv.TuVanGanNhat);

            var kq2 = await sv.GuiAsync("đặt phương án 1 giúp tôi");
            Assert.True(kq2.ThanhCong, kq2.Loi);
            Assert.Equal(501, daMo!.PhuongAn.CacMaChoNgoi.Single());
            Assert.Equal(15, daMo.YeuCau.MaGaDen);
            Assert.True(kq2.DatVe!.DaDat);
            Assert.True(sv.DaDatPhuongAn(1));
            Assert.StartsWith("ĐÃ ĐẶT: đơn PNR001", kenh.CacLanGoi[3][^1].NoiDung);
        }

        [Fact]
        public async Task DatVe_ChuaTuVan_HoacDaDat_HoacHuy()
        {
            var (sv, kenh, _, _) = Tao(new KenhGia(
                _ => Goi("dat_ve", "{\"phuong_an\":1}"), _ => Chu("Cần tư vấn trước ạ.")));
            await sv.GuiAsync("đặt vé");
            Assert.StartsWith("LỖI: Chưa có lần tư vấn", kenh.CacLanGoi[1][^1].NoiDung);

            var (yc, kq) = TuVanMau();
            var (sv2, kenh2, _, _) = Tao(new KenhGia(
                _ => Goi("dat_ve", "{\"phuong_an\":2}"), _ => Chu("Nhân viên đã hủy ạ."),
                _ => Goi("dat_ve", "{\"phuong_an\":5}"), _ => Chu("Không có phương án 5.")));
            sv2.GhiNhanTuVanTuForm(yc, kq);
            sv2.XacNhanDatVe = _ => Task.FromResult(new KetQuaDatVeHo { NguoiDungHuy = true });
            await sv2.GuiAsync("đặt phương án 2");
            Assert.StartsWith("NHÂN VIÊN ĐÃ HỦY", kenh2.CacLanGoi[1][^1].NoiDung);
            Assert.Contains(kenh2.CacLanGoi[0], t => t.NoiDung.StartsWith("[Ghi chú hệ thống] Nhân viên vừa tư vấn bằng chế độ Lọc nâng cao"));
            await sv2.GuiAsync("đặt phương án 5");
            Assert.Contains("chỉ có 2 phương án", kenh2.CacLanGoi[3][^1].NoiDung);

            sv2.GhiNhanDaDatNgoaiChat(1, new KetQuaDatVeHo { DaDat = true, MaPnr = "PNR9", SoVe = 1 });
            Assert.True(sv2.DaDatPhuongAn(1));
        }

        [Fact]
        public async Task LoiKenh_GiuaChung_BoCaLuot()
        {
            var kenh = new KenhGia(_ => Chu("Chào anh."), _ => Goi("tim_chuyen_tau"),
                                   _ => throw new LoiMoHinhNgonNguException("Không kết nối được tới OpenAI — kiểm tra mạng."));
            var (sv, _, _, nk) = Tao(kenh);
            await sv.GuiAsync("chào");
            var kq = await sv.GuiAsync("có tàu đi Vinh không");
            Assert.False(kq.ThanhCong);
            Assert.Contains("kiểm tra mạng", kq.Loi);
            Assert.Equal(2, sv.LichSu.Count);          // chi con luot "chao"
            Assert.Single(nk);
        }

        [Fact]
        public async Task GoiCongCuMai_DungSauSoVongToiDa()
        {
            var kenh = new KenhGia { MacDinh = _ => Goi("tim_chuyen_tau") };
            var (sv, _, cc, _) = Tao(kenh);
            var kq = await sv.GuiAsync("tìm giúp");
            Assert.True(kq.ThanhCong);
            Assert.Equal(TroLyChatService.SoVongToiDa, kq.SoLanGoiMoHinh);
            Assert.Contains("quá nhiều bước", kq.TraLoi);
            Assert.Equal(VaiTroTinNhan.TroLy, sv.LichSu[^1].VaiTro);
        }

        [Fact]
        public async Task SoBia_TrongCauTraLoi_BaoChuaDoiChieu()
        {
            var (sv, _, _, _) = Tao(new KenhGia(_ => Goi("tim_chuyen_tau"), _ => Chu("Ghế mềm chỉ 799.000 đ thôi ạ.")));
            var kq = await sv.GuiAsync("giá ghế mềm bao nhiêu");
            Assert.Contains("799.000", kq.SoLieuChuaDoiChieu);
        }

        [Fact]
        public async Task ThamSoHong_TraLoiChoMoHinh_KhongSap()
        {
            var (sv, kenh, cc, _) = Tao(new KenhGia(_ => Goi("tim_chuyen_tau", "không phải json"), _ => Chu("Xin lỗi.")));
            var kq = await sv.GuiAsync("tìm tàu");
            Assert.True(kq.ThanhCong);
            Assert.Empty(cc.DaGoi);
            Assert.StartsWith("LỖI:", kenh.CacLanGoi[1][^1].NoiDung);
        }

        [Fact]
        public async Task CauRong_KhongGoi()
        {
            var (sv, kenh, _, _) = Tao(new KenhGia());
            var kq = await sv.GuiAsync("   ");
            Assert.False(kq.ThanhCong);
            Assert.Empty(kenh.CacLanGoi);
        }
    }
}
