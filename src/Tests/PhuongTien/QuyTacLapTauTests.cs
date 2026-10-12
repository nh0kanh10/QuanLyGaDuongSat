using BUS.Services;
using DTO.PhuongTien;
using static Tests.PhuongTien.MauLapTau;

namespace Tests.PhuongTien
{
    // Luat lap doan tau (BUS/Services/QuyTacLapTau.cs, CLAUDE.md §5.11): moi hang muc mot ca dat / mot ca loi,
    // chang lon nhat khi cat / noi toa doc duong.
    public class QuyTacLapTauTests
    {
        private static HangMucKiemTraDTO HangMuc(KetQuaKiemTraLapTauDTO kq, string ten)
            => kq.HangMuc.Single(h => h.Ten == ten);

        // --- Phuong an hop le ---

        [Fact]
        public void PhuongAnHopLe_KhongLoiKhongCanhBao()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, DM903, KhachSuot(10)));

            Assert.False(kq.CoLoi, string.Join("\n", kq.HangMuc.Select(h => $"{h.Ten}: {h.NoiDung}")));
            Assert.Equal(0, kq.SoCanhBao);
            Assert.Equal(10, kq.SoToaLonNhat);
            Assert.Equal(19m + 10 * 20m, kq.ChieuDaiLonNhatM);   // ke ca dau may keo chinh
            Assert.Equal(10 * 46m, kq.TrongLuongLonNhatTan);      // toa toan tai, khong tinh dau may
            Assert.Single(kq.Chang);
        }

        // --- 1. Dau may keo chinh ---

        [Fact]
        public void DauMayChinh_ChuaChon_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(null, KhachSuot(5).ToArray()));
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Đầu máy kéo chính").MucDo);
        }

        [Fact]
        public void DauMayChinh_DangBaoDuong_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM702BaoDuong, KhachSuot(5).ToArray()));
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Đầu máy kéo chính").MucDo);
        }

        [Fact]
        public void DauMayChinh_KeoChuyenKhacChongGio_Loi()
        {
            var bc = BoiCanh();
            bc.TimDauMay(DM901)!.ChuyenKhac.Add(Chuyen("SE1", "DANG_CHAY", GioDi.AddHours(-5), GioDi.AddHours(3)));

            var kq = QuyTacLapTau.KiemTra(bc, PhuongAn(DM901, KhachSuot(5).ToArray()));
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Đầu máy kéo chính").MucDo);
            Assert.Contains("SE1", HangMuc(kq, "Đầu máy kéo chính").NoiDung);
        }

        [Fact]
        public void DauMayChinh_ChuyenChongGioDaHuy_KhongTinh()
        {
            var bc = BoiCanh();
            bc.TimDauMay(DM901)!.ChuyenKhac.Add(Chuyen("SE1", "DA_HUY", GioDi.AddHours(-5), GioDi.AddHours(3)));

            var kq = QuyTacLapTau.KiemTra(bc, PhuongAn(DM901, KhachSuot(5).ToArray()));
            Assert.Equal(MucDoKiemTra.Dat, HangMuc(kq, "Đầu máy kéo chính").MucDo);
        }

        [Fact]
        public void DauMayChinh_ChuyenTruocKetThucGaKhac_CanhBaoChayDonVe()
        {
            var bc = BoiCanh();
            bc.TimDauMay(DM901)!.ChuyenKhac.Add(Chuyen("SE2", "HOAN_THANH", GioDi.AddHours(-40), GioDi.AddHours(-8), "SGO", "Sài Gòn"));

            var kq = QuyTacLapTau.KiemTra(bc, PhuongAn(DM901, KhachSuot(5).ToArray()));
            Assert.Equal(MucDoKiemTra.CanhBao, HangMuc(kq, "Đầu máy kéo chính").MucDo);
            Assert.Contains("chạy đơn về", HangMuc(kq, "Đầu máy kéo chính").NoiDung);
        }

        [Fact]
        public void DauMayChinh_ChuyenTruocVeDungGaDi_Dat()
        {
            var bc = BoiCanh();
            bc.TimDauMay(DM901)!.ChuyenKhac.Add(Chuyen("SE4", "HOAN_THANH", GioDi.AddHours(-40), GioDi.AddHours(-8), "HNI", "Hà Nội"));

            var kq = QuyTacLapTau.KiemTra(bc, PhuongAn(DM901, KhachSuot(5).ToArray()));
            Assert.Equal(MucDoKiemTra.Dat, HangMuc(kq, "Đầu máy kéo chính").MucDo);
        }

        // --- 2. Dau may day ---

        [Fact]
        public void MayDay_TrungMayChinh_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, DM901, KhachSuot(5)));
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Đầu máy đẩy").MucDo);
        }

        [Fact]
        public void MayDay_DangBaoDuong_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, DM702BaoDuong, KhachSuot(5)));
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Đầu máy đẩy").MucDo);
        }

        [Fact]
        public void MayDay_KhongCo_QuaKhuGianCanDay_CanhBao()
        {
            var bc = BoiCanh();
            bc.KhuGianCanDay.Add(new KhuGianCanDayDTO { TuGaCode = "LCO", DenGaCode = "HVA", TenTuGa = "Lăng Cô", TenDenGa = "Hải Vân", DoDocPermil = 17 });

            var kq = QuyTacLapTau.KiemTra(bc, PhuongAn(DM901, KhachSuot(5).ToArray()));
            Assert.Equal(MucDoKiemTra.CanhBao, HangMuc(kq, "Đầu máy đẩy").MucDo);
            Assert.False(kq.CoLoi);
        }

        [Fact]
        public void MayDay_KhongCo_KhongQuaKhuGianCanDay_KhongCoHangMuc()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, KhachSuot(5).ToArray()));
            Assert.DoesNotContain(kq.HangMuc, h => h.Ten == "Đầu máy đẩy");
        }

        // --- 3. So toa ---

        [Fact]
        public void SoToa_ChuaNoiToa_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901));
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Số toa trong đoàn").MucDo);
        }

        [Fact]
        public void SoToa_20Toa_Dat_21Toa_Loi()
        {
            var dat = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, KhachSuot(20).ToArray()));
            var loi = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, KhachSuot(21).ToArray()));

            Assert.Equal(MucDoKiemTra.Dat, HangMuc(dat, "Số toa trong đoàn").MucDo);
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(loi, "Số toa trong đoàn").MucDo);
        }

        [Fact]
        public void SoToa_CatHetToaTaiGaGiua_KhuDoanRong_Loi()
        {
            var toa = KhachSuot(3).Select(t => T(t.MaToaXe, cat: VIN)).ToArray();
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa));

            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Số toa trong đoàn").MucDo);
            Assert.Contains("không còn toa nào", HangMuc(kq, "Số toa trong đoàn").NoiDung);
        }

        // --- 4. Chieu dai doi chieu duong tai tung ga ---

        [Fact]
        public void ChieuDai_GaCoDuongTranhNgan_Loi()
        {
            var bc = BoiCanh();
            bc.HanhTrinh[2].DuongTranhDaiNhatM = 200m;   // Hue: 10 toa + dau may = 219 m

            var kq = QuyTacLapTau.KiemTra(bc, PhuongAn(DM901, KhachSuot(10).ToArray()));
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Chiều dài đoàn tàu vs đường tránh").MucDo);
            Assert.Contains(2, kq.GaVuotChieuDai);
            Assert.True(kq.KhuDoan[1].CoLoi);   // VIN -> HUE den ga vuot
            Assert.True(kq.KhuDoan[2].CoLoi);   // HUE -> DAN roi ga vuot
            Assert.False(kq.KhuDoan[0].CoLoi);
        }

        [Fact]
        public void ChieuDai_DoanNganHonTaiGaTruocGaNoi_KhongLoi()
        {
            // Hue chi 200 m nhung doan tau qua Hue moi 5 toa (119 m); noi 5 toa tai Da Nang (sau Hue)
            var bc = BoiCanh();
            bc.HanhTrinh[2].DuongTranhDaiNhatM = 200m;
            var toa = KhachSuot(5).Concat(Enumerable.Range(6, 5).Select(so => T(Khach(so), noi: DAN))).ToArray();

            var kq = QuyTacLapTau.KiemTra(bc, PhuongAn(DM901, toa));
            Assert.Equal(MucDoKiemTra.Dat, HangMuc(kq, "Chiều dài đoàn tàu vs đường tránh").MucDo);
        }

        [Fact]
        public void GioiHanChieuDaiTaiGa_UuTienDuongDaGan_RoiDuongTranh_TranHeThong()
        {
            var ga = new DiemDungHanhTrinhDTO { ChieuDaiDuongGanM = 300m, SoHieuDuongGan = 2, DuongTranhDaiNhatM = 500m };
            Assert.Equal(300m, QuyTacLapTau.GioiHanChieuDaiTaiGa(ga).GioiHanM);

            ga.ChieuDaiDuongGanM = null;
            Assert.Equal(QuyTacLapTau.ChieuDaiToiDaM, QuyTacLapTau.GioiHanChieuDaiTaiGa(ga).GioiHanM);   // 500 -> tran 450

            ga.DuongTranhDaiNhatM = 380m;
            Assert.Equal(380m, QuyTacLapTau.GioiHanChieuDaiTaiGa(ga).GioiHanM);

            ga.DuongTranhDaiNhatM = null;
            Assert.Equal(QuyTacLapTau.ChieuDaiToiDaM, QuyTacLapTau.GioiHanChieuDaiTaiGa(ga).GioiHanM);
        }

        // --- 5. Trong luong vs suc keo ---

        [Fact]
        public void SucKeo_QuaTai_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM904Yeu, KhachSuot(10).ToArray()));   // 460 t / 300 t
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Trọng lượng kéo vs sức kéo").MucDo);
        }

        [Theory]
        [InlineData(1001, false)]   // > 100%
        [InlineData(1000, true)]    // dung 100%: chua qua tai
        [InlineData(950, true)]     // sat dinh muc
        [InlineData(900, true)]
        public void DoiChieuSucKeo_NguongQuaTai(int trongLuong, bool dat)
            => Assert.Equal(dat, QuyTacLapTau.DoiChieuSucKeo(trongLuong, 1000m).DatYeuCau);

        [Fact]
        public void DoiChieuSucKeo_SatDinhMucTren90PhanTram()
        {
            Assert.StartsWith("Sát định mức", QuyTacLapTau.DoiChieuSucKeo(950m, 1000m).ThongDiep);
            Assert.StartsWith("Đạt yêu cầu", QuyTacLapTau.DoiChieuSucKeo(900m, 1000m).ThongDiep);
        }

        // --- 6. Tai trong truc (nguong gia dinh 14 t) ---

        [Fact]
        public void TaiTrongTruc_ToaVuot14Tan_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, KhachSuot(5).Append(T(ToaP301)).ToArray()));

            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Tải trọng trục").MucDo);
            Assert.Contains("P-301", HangMuc(kq, "Tải trọng trục").NoiDung);
            Assert.Contains("giả định", HangMuc(kq, "Tải trọng trục").NoiDung);
        }

        [Fact]
        public void TaiTrongTruc_TinhTheoToanTaiChiaSoTruc()
        {
            var bc = BoiCanh();
            Assert.Equal(11.50m, QuyTacLapTau.TaiTrongTruc(bc.TimToa(Khach(1))!));
            Assert.Equal(14.25m, QuyTacLapTau.TaiTrongTruc(bc.TimToa(ToaP301)!));
            Assert.Equal(11.50m, PhuongTienService.TinhTaiTrongTruc(36m, 10m, 4));
        }

        // --- 7. Thu tu xep toa hang ---

        [Fact]
        public void ThuTuToa_ToaHangXenGiuaToaKhach_Loi()
        {
            var toa = KhachSuot(3).Append(T(ToaHang201)).Concat(KhachSuot(3, 4)).ToArray();
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa));

            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Thứ tự xếp toa").MucDo);
            Assert.Contains("G-201", HangMuc(kq, "Thứ tự xếp toa").NoiDung);
        }

        [Fact]
        public void ThuTuToa_ToaHangLienKhoiCuoiDoan_Dat()
        {
            var toa = KhachSuot(5).Append(T(ToaHang201)).Append(T(ToaHang202)).ToArray();
            Assert.Equal(MucDoKiemTra.Dat, HangMuc(QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa)), "Thứ tự xếp toa").MucDo);
        }

        [Fact]
        public void ThuTuToa_ToaHangNgaySauDauMay_Dat()
        {
            var toa = new[] { T(ToaHang201), T(ToaHang202) }.Concat(KhachSuot(5)).ToArray();
            Assert.Equal(MucDoKiemTra.Dat, HangMuc(QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa)), "Thứ tự xếp toa").MucDo);
        }

        [Fact]
        public void ThuTuToa_ToaHangTachHaiCum_Loi()
        {
            var toa = new[] { T(ToaHang201) }.Concat(KhachSuot(5)).Append(T(ToaHang202)).ToArray();
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa)), "Thứ tự xếp toa").MucDo);
        }

        // --- 8. Toa xe san sang ---

        [Fact]
        public void ToaSanSang_ToaDangBaoDuong_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, KhachSuot(5).Append(T(ToaBaoDuong401)).ToArray()));
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Toa xe sẵn sàng").MucDo);
        }

        [Fact]
        public void ToaSanSang_ToaThuocDoanKhacChongGio_Loi()
        {
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, KhachSuot(5).Append(T(ToaBan402)).ToArray()));

            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Toa xe sẵn sàng").MucDo);
            Assert.Contains("SE1", HangMuc(kq, "Toa xe sẵn sàng").NoiDung);
        }

        // --- 9. Cat / noi toa doc duong + chang lon nhat ---

        [Fact]
        public void NoiToaTaiDaNang_ChangLonNhatLaDoanSauGaNoi()
        {
            var toa = KhachSuot(10).Concat(Enumerable.Range(11, 3).Select(so => T(Khach(so), noi: DAN))).ToArray();
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa));

            Assert.False(kq.CoLoi, string.Join("\n", kq.HangMuc.Where(h => h.MucDo == MucDoKiemTra.Loi).Select(h => h.NoiDung)));
            Assert.Equal(13, kq.SoToaLonNhat);
            Assert.Equal(2, kq.Chang.Count);
            Assert.Equal("HNI → DAN", kq.Chang[0].Ten);
            Assert.Equal("DAN → SGO", kq.Chang[1].Ten);
            Assert.Equal("DAN → SGO", kq.ChangDaiNhat);
            Assert.Equal(19m + 13 * 20m, kq.ChieuDaiLonNhatM);
            Assert.Equal(13 * 46m, kq.TrongLuongLonNhatTan);
            Assert.Equal(new[] { 10, 10, 10, 13 }, kq.KhuDoan.Select(k => k.SoToa));

            var tn = Assert.Single(kq.TacNghiep);
            Assert.Equal("DAN", tn.MaGaCode);
            Assert.Equal(3, tn.Noi.Count);
            Assert.Equal(ViTriNhomToa.Cuoi, tn.ViTriNoi);
            Assert.True(tn.DuThoiGian);
            Assert.StartsWith("DAN", kq.MoTaCatNoi);
            Assert.Equal(MucDoKiemTra.Dat, HangMuc(kq, "Cắt / nối toa dọc đường").MucDo);
        }

        [Fact]
        public void CatToaTaiVinh_ChangLonNhatLaDoanTruocGaCat()
        {
            var toa = KhachSuot(10).Concat(Enumerable.Range(11, 2).Select(so => T(Khach(so), cat: VIN))).ToArray();
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa));

            Assert.False(kq.CoLoi);
            Assert.Equal(12, kq.SoToaLonNhat);
            Assert.Equal("HNI → VIN", kq.ChangDaiNhat);
            Assert.Equal(new[] { 12, 10, 10, 10 }, kq.KhuDoan.Select(k => k.SoToa));
        }

        [Fact]
        public void NoiToaTaiGaDoDuoi15Phut_Loi()
        {
            var toa = KhachSuot(10).Append(T(Khach(11), noi: HUE)).ToArray();   // Hue do 5'
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa));

            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Cắt / nối toa dọc đường").MucDo);
            Assert.Contains($"{QuyTacLapTau.SoPhutDoToiThieuCatNoi} phút", HangMuc(kq, "Cắt / nối toa dọc đường").NoiDung);
        }

        [Fact]
        public void NoiToaChenGiuaDoan_Loi()
        {
            var noi = Enumerable.Range(11, 2).Select(so => T(Khach(so), noi: DAN));
            var toa = KhachSuot(5).Concat(noi).Concat(KhachSuot(5, 6)).ToArray();
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa));

            Assert.Equal(ViTriNhomToa.Sai, Assert.Single(kq.TacNghiep).ViTriNoi);
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Cắt / nối toa dọc đường").MucDo);
        }

        [Fact]
        public void NoiToaNgaySauDauMay_Dat()
        {
            var toa = Enumerable.Range(11, 2).Select(so => T(Khach(so), noi: DAN)).Concat(KhachSuot(5)).ToArray();
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa));

            Assert.Equal(ViTriNhomToa.Dau, Assert.Single(kq.TacNghiep).ViTriNoi);
            Assert.False(kq.CoLoi);
        }

        // --- 0. Cau truc phuong an ---

        [Fact]
        public void CauTruc_GaNoiSauGaCat_Loi()
        {
            var toa = KhachSuot(5).Append(T(Khach(6), noi: DAN, cat: VIN)).ToArray();
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa));
            Assert.Equal(MucDoKiemTra.Loi, HangMuc(kq, "Dữ liệu phương án").MucDo);
        }

        [Fact]
        public void CauTruc_ToaXuatHienHaiLan_Loi()
        {
            var toa = KhachSuot(5).Append(T(Khach(1))).ToArray();
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa));
            Assert.Contains("hai lần", HangMuc(kq, "Dữ liệu phương án").NoiDung);
        }

        [Fact]
        public void CauTruc_GaNoiNgoaiHanhTrinh_Loi()
        {
            var toa = KhachSuot(5).Append(T(Khach(6), noi: 99)).ToArray();
            var kq = QuyTacLapTau.KiemTra(BoiCanh(), PhuongAn(DM901, toa));
            Assert.Contains("không nằm trên hành trình", HangMuc(kq, "Dữ liệu phương án").NoiDung);
        }

        [Fact]
        public void CauTruc_ChuyenKhongCoGaTrungGian_KhongCatNoiDuoc()
        {
            var toa = KhachSuot(5).Append(T(Khach(6), noi: HNI)).ToArray();
            var kq = QuyTacLapTau.KiemTra(BoiCanh(soGa: 2), PhuongAn(DM901, toa));
            Assert.Contains("không có ga dừng trung gian", HangMuc(kq, "Dữ liệu phương án").NoiDung);
        }

        // --- Ham phu ---

        [Theory]
        [InlineData(new[] { 1, 2, 3, 4 }, new[] { 3, 4 }, ViTriNhomToa.Cuoi)]
        [InlineData(new[] { 1, 2, 3, 4 }, new[] { 1, 2 }, ViTriNhomToa.Dau)]
        [InlineData(new[] { 1, 2, 3, 4 }, new[] { 2, 3 }, ViTriNhomToa.Sai)]   // chen giua
        [InlineData(new[] { 1, 2, 3, 4 }, new[] { 1, 4 }, ViTriNhomToa.Sai)]   // khong lien khoi
        [InlineData(new[] { 1, 2, 3, 4 }, new[] { 9 }, ViTriNhomToa.Khong)]
        [InlineData(new[] { 1, 2, 3, 4 }, new[] { 1, 2, 3, 4 }, ViTriNhomToa.Cuoi)]
        public void XacDinhViTriNhom(int[] doan, int[] nhom, string ketQua)
            => Assert.Equal(ketQua, QuyTacLapTau.XacDinhViTriNhom(doan, nhom));

        [Theory]
        [InlineData("DA_LEN_LICH", true)]
        [InlineData("SAN_SANG", true)]
        [InlineData("DANG_CHAY", false)]
        [InlineData("TRE_GIO", false)]
        [InlineData("HOAN_THANH", false)]
        [InlineData("DA_HUY", false)]
        public void ChiLapTauKhiChuyenChuaChay(string trangThai, bool choPhep)
            => Assert.Equal(choPhep, QuyTacLapTau.KiemTraChoPhepSua(trangThai).ChoPhep);

        [Fact]
        public void ChongGio_HaiKhungChamDauMut_KhongChong()
        {
            var t = new DateTime(2026, 10, 20, 8, 0, 0);
            Assert.False(QuyTacLapTau.ChongGio(t, t.AddHours(2), t.AddHours(2), t.AddHours(4)));
            Assert.True(QuyTacLapTau.ChongGio(t, t.AddHours(2), t.AddHours(1), t.AddHours(4)));
        }
    }
}
