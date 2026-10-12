using BUS.Services;
using DTO.NhanSu;
using static Tests.NhanSu.MauNhanSu;

namespace Tests.NhanSu
{
    // Luat phan cong kip lai R2-R11 (BUS/Services/DanhGiaKipLai.cs, CLAUDE.md §5.8, §5.12):
    // o chon chi hien nguoi du dieu kien, thu tu goi y, trung chuyen / nghi giua ca / kiem tra len ban,
    // truong tau di tiep chang sau, chang chong nhau, va bat bien "chon tu danh sach thi khong ra ca do".
    public class DanhGiaKipLaiTests
    {
        private static DanhSachUngVien UngVienLaiTauSE1(DuLieuNhanSu dl)
            => DanhGiaKipLai.LayUngVien(dl, QuyTacKipLai.LaiTau, dl.TimChuyen(ChuyenA)!, HNI, VIN, null);

        private static string[] Ma(IEnumerable<UngVienKip> ds) => ds.Select(u => u.MaNVCode).ToArray();

        [Fact]
        public void OChonLaiTau_ChiHienNguoiDuDieuKien_XepTheoGoiY()
        {
            var uv = UngVienLaiTauSE1(DuLieu());

            // khong canh bao truoc -> cung depot dau chang truoc -> ... ; han kham sap het xep cuoi
            Assert.Equal(new[] { "LT_001", "LT_008", "LT_005" }, Ma(uv.DuDieuKien));
            Assert.Equal("LT_001", uv.GoiY!.MaNVCode);
            Assert.Equal(new[] { "LT_002", "LT_003", "LT_004", "LT_007", "LT_009" }, Ma(uv.BiAn));
            Assert.DoesNotContain("LT_006", Ma(uv.DuDieuKien).Concat(Ma(uv.BiAn)));   // da nghi viec: khong xet
        }

        [Theory]
        [InlineData("LT_002", "chỉ nghỉ")]                        // R4 nghi < 8 gio sau SE19
        [InlineData("LT_003", "hạng bằng D13E")]                  // R7
        [InlineData("LT_004", "hết hạn khám sức khỏe")]            // R8
        [InlineData("LT_007", "không đạt kiểm tra lên ban")]       // R6
        [InlineData("LT_009", "khung giờ hai chuyến chồng nhau")]  // R3
        public void NguoiBiAn_CoLyDo(string maNV, string lyDo)
        {
            var uv = UngVienLaiTauSE1(DuLieu());
            Assert.Contains(lyDo, uv.BiAn.Single(u => u.MaNVCode == maNV).LyDoKhongDuDieuKien);
        }

        [Fact]
        public void HanKhamConDuoi30Ngay_CanhBaoNhungVanChonDuoc()
        {
            var lt005 = UngVienLaiTauSE1(DuLieu()).Tim(5)!;
            Assert.True(lt005.CoCanhBao);
            Assert.False(lt005.CoLoi);
        }

        [Fact]
        public void KhacDepot_ChiLaLuuY()
        {
            var lt008 = UngVienLaiTauSE1(DuLieu()).Tim(8)!;
            Assert.False(lt008.CoLoi);
            Assert.False(lt008.CoCanhBao);
            Assert.False(lt008.CungDepot);
        }

        [Fact]
        public void R2_SaiChucDanh_Loi()
        {
            var dl = DuLieu();
            var dk = DanhGiaKipLai.DoiChieu(dl, dl.TimNhanVien(30)!, QuyTacKipLai.LaiTau, dl.TimChuyen(ChuyenA)!, HNI, VIN, null);
            Assert.True(dk.Single(d => d.TenDieuKien == DieuKienKip.ChucDanh).LaLoi);
        }

        [Fact]
        public void R7_ChuyenD13E_ChiLaiTauBangD13E()
        {
            var dl = DuLieu();
            var uv = DanhGiaKipLai.LayUngVien(dl, QuyTacKipLai.LaiTau, dl.TimChuyen(ChuyenD)!, HNI, VIN, null);
            Assert.Equal(new[] { "LT_003" }, Ma(uv.DuDieuKien));
        }

        [Fact]
        public void TruongTau_DuocDiTiepChangSauCuaChuyenMinh()
        {
            var dl = DuLieu(new[] { KipMoi(3, ChuyenA, HNI, VIN, lai: 1, phu: 10, truong: 20) });
            var se1 = dl.TimChuyen(ChuyenA)!;

            var tt = DanhGiaKipLai.LayUngVien(dl, QuyTacKipLai.TruongTau, se1, VIN, DAN, null);
            Assert.Contains("TT_001", Ma(tt.DuDieuKien));

            // lai tau thi khong: moi chuyen chi mot kip
            var lt = DanhGiaKipLai.LayUngVien(dl, QuyTacKipLai.LaiTau, se1, VIN, DAN, null);
            Assert.Contains("đã thuộc kíp", lt.BiAn.Single(u => u.MaNVCode == "LT_001").LyDoKhongDuDieuKien);
        }

        [Fact]
        public void TruongTau_KhongDuocChongChangCuaChinhMinh()
        {
            var dl = DuLieu(new[] { KipMoi(3, ChuyenA, HNI, VIN, lai: 1, phu: 10, truong: 20) });
            var tt = DanhGiaKipLai.LayUngVien(dl, QuyTacKipLai.TruongTau, dl.TimChuyen(ChuyenA)!, HNI, DAN, null);
            Assert.Contains("TT_001", Ma(tt.BiAn));
        }

        [Fact]
        public void SuaKip_KhongTuSoVoiChinhNo()
        {
            // Dang sua kip 3 thi LT_001 (thanh vien kip 3) van du dieu kien cho chinh chang do
            var dl = DuLieu(new[] { KipMoi(3, ChuyenA, HNI, VIN, lai: 1, phu: 10, truong: 20) });
            var lt = DanhGiaKipLai.LayUngVien(dl, QuyTacKipLai.LaiTau, dl.TimChuyen(ChuyenA)!, HNI, VIN, maPhanCongBoQua: 3);
            Assert.Contains("LT_001", Ma(lt.DuDieuKien));
        }

        [Fact]
        public void R4_DoiXung_KhongNhanCaKetThucSatGioCaSau()
        {
            // LT_001 da o SE1 19:00; SE19 ket thuc 12:00 cung ngay -> ca sau chi cach 7 gio
            var dl = DuLieu(new[] { KipMoi(3, ChuyenA, HNI, VIN, lai: 1, phu: 10, truong: 20) });
            var lt = DanhGiaKipLai.LayUngVien(dl, QuyTacKipLai.LaiTau, dl.TimChuyen(ChuyenC)!, HNI, VIN, maPhanCongBoQua: 2);
            Assert.Contains("ca sau", lt.BiAn.Single(u => u.MaNVCode == "LT_001").LyDoKhongDuDieuKien);
        }

        // --- R11 chang ---

        [Fact]
        public void KiemTraChang()
        {
            var dl = DuLieu(new[] { KipMoi(3, ChuyenA, HNI, VIN, lai: 1, phu: 10, truong: 20) });
            var se1 = dl.TimChuyen(ChuyenA)!;

            Assert.Null(DanhGiaKipLai.KiemTraChang(dl, se1, VIN, DAN, null));
            Assert.Contains("chồng lên kíp", DanhGiaKipLai.KiemTraChang(dl, se1, HNI, DAN, null));
            Assert.Contains("phải nằm sau", DanhGiaKipLai.KiemTraChang(dl, se1, DAN, VIN, null));
            Assert.Contains("ga dừng trên hành trình", DanhGiaKipLai.KiemTraChang(dl, se1, 99, VIN, null));
            Assert.Null(DanhGiaKipLai.KiemTraChang(dl, se1, HNI, VIN, maPhanCongBoQua: 3));
        }

        [Fact]
        public void DanhGiaChuyen_ChangChuaCoKip_CanhBao()
        {
            var dl = DuLieu(new[] { KipMoi(3, ChuyenA, HNI, VIN, lai: 1, phu: 10, truong: 20) });
            var tt = DanhGiaKipLai.DanhGiaChuyen(dl, dl.TimChuyen(ChuyenA)!);

            var thieu = Assert.Single(tt.VanDe, v => v.NoiDung.Contains("chưa có kíp lái"));
            Assert.Equal(MucDoVanDe.CanhBao, thieu.MucDo);
            Assert.Contains("Vinh → Sài Gòn", thieu.NoiDung);
            Assert.DoesNotContain(tt.VanDe, v => v.MucDo == MucDoVanDe.NghiemTrong);
        }

        [Fact]
        public void KhongDatKiemTraSauKhiPhanCong_KipHienDo_ThayNguoiThiHet()
        {
            var phieu = new KiemTraLenBanHienThi
            {
                MaKiemTra = 2, MaNhanVien = 1, MaChuyenTau = ChuyenA, NongDoConMgL = 0.20m, SoGioNghiTruocCa = 10m,
                ThoiDiemKiemTra = NgayMai.AddHours(18.3)
            };
            var dl = DuLieu(new[] { KipMoi(3, ChuyenA, HNI, VIN, lai: 1, phu: 10, truong: 20) }, new[] { phieu });

            var vanDe = DanhGiaKipLai.DanhGiaChuyen(dl, dl.TimChuyen(ChuyenA)!).VanDe;
            var loi = Assert.Single(vanDe, v => v.MucDo == MucDoVanDe.NghiemTrong);
            Assert.Equal(1, loi.MaNhanVien);
            Assert.Equal(DieuKienKip.KiemTra, loi.DieuKien);

            // thay LT_001 bang nguoi goi y -> het ca do
            var goiY = DanhGiaKipLai.LayUngVien(dl, QuyTacKipLai.LaiTau, dl.TimChuyen(ChuyenA)!, HNI, VIN, 3).GoiY!;
            Assert.NotEqual("LT_001", goiY.MaNVCode);
            var dlSau = DuLieu(new[] { KipMoi(3, ChuyenA, HNI, VIN, lai: goiY.NhanVien.MaNhanVien, phu: 10, truong: 20) }, new[] { phieu });
            Assert.DoesNotContain(DanhGiaKipLai.DanhGiaChuyen(dlSau, dlSau.TimChuyen(ChuyenA)!).VanDe,
                                  v => v.MucDo == MucDoVanDe.NghiemTrong);
        }

        // --- Bat bien (chuyen tu tools/kiemtra_nhansu, 29/09) ---

        [Fact]
        public void DuLieuMau_KhongCoCaDo()
        {
            var dl = DuLieu();
            Assert.All(dl.ChuyenTau, c =>
                Assert.DoesNotContain(DanhGiaKipLai.DanhGiaChuyen(dl, c).VanDe, v => v.MucDo == MucDoVanDe.NghiemTrong));
        }

        [Fact]
        public void PhanNgauNhienTuDanhSachDuDieuKien_KhongBaoGioRaCaDo()
        {
            var rd = new Random(2026);
            int soKipDaThem = 0;

            for (int vong = 0; vong < 40; vong++)
            {
                var them = new List<PhanCongKipHienThi>();
                int maMoi = 100;

                for (int lan = 0; lan < 15; lan++)
                {
                    var dl = DuLieu(them);
                    var chuyen = dl.ChuyenTau[rd.Next(dl.ChuyenTau.Count)];
                    int i = rd.Next(chuyen.LichDung.Count - 1);
                    int j = rd.Next(i + 1, chuyen.LichDung.Count);
                    int nhan = chuyen.LichDung[i].MaGa, giao = chuyen.LichDung[j].MaGa;
                    if (DanhGiaKipLai.KiemTraChang(dl, chuyen, nhan, giao, null) != null) continue;

                    var chon = new Dictionary<string, int>();
                    foreach (string vaiTro in QuyTacKipLai.CacVaiTro)
                    {
                        var ds = DanhGiaKipLai.LayUngVien(dl, vaiTro, chuyen, nhan, giao, null).DuDieuKien;
                        if (ds.Count == 0) break;
                        chon[vaiTro] = ds[rd.Next(ds.Count)].NhanVien.MaNhanVien;
                    }
                    if (chon.Count < 3) continue;

                    var kip = KipMoi(maMoi++, chuyen.MaChuyenTau, nhan, giao,
                                     chon[QuyTacKipLai.LaiTau], chon[QuyTacKipLai.PhuLai], chon[QuyTacKipLai.TruongTau]);
                    them.Add(kip);
                    soKipDaThem++;

                    var sau = DuLieu(them);
                    foreach (var c in sau.ChuyenTau)
                    {
                        var caDo = DanhGiaKipLai.DanhGiaChuyen(sau, c).VanDe.Where(v => v.MucDo == MucDoVanDe.NghiemTrong).ToList();
                        Assert.True(caDo.Count == 0,
                            $"vong {vong}, them kip {kip.MaPhanCong} ({chuyen.SoHieuMacTau} {nhan}->{giao}) -> " +
                            string.Join(" | ", caDo.Select(v => v.NoiDung)));
                    }
                }
            }

            Assert.True(soKipDaThem >= 40, $"chi them duoc {soKipDaThem} kip - du lieu mau qua it, bai kiem tra khong co y nghia");
        }
    }
}
