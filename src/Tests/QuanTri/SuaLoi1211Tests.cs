using BUS.AI;
using BUS.Services;
using DTO.Common;

namespace Tests.QuanTri
{
    // Sua loi 12/11 (sau kiem giao dien Sprint 3):
    //   - Mac tau / dinh muc van toc: dieu do (TRAIN_SCHEDULE_EDIT) sua duoc, khong chi INFRA_EDIT;
    //   - AI tu van ve: loai khach du kien cua doan -> gia sau giam, gio ve dat san doi tuong;
    //   - Service cua A Ba chan nguoi chi xem truoc khi doc CSDL.
    [Collection("PhienLamViec")]
    public class QuyenMacTauTests
    {
        private static void DangNhap(params string[] quyen)
        {
            PhienLamViec.BatBuocDangNhap = true;
            PhienLamViec.DangNhap(99, "kiemthu", "Kiểm thử", null, quyen, new[] { "Kiểm thử" });
        }

        private static void DangXuat()
        {
            PhienLamViec.DangXuat();
            PhienLamViec.BatBuocDangNhap = false;
        }

        [Fact]
        public void QuyenSuaMacTau_GomHaTangVaLapBieuDo()
        {
            Assert.Contains(MaQuyen.SuaHaTang, PhanQuyenService.QuyenSuaMacTau);
            Assert.Contains(MaQuyen.LapBieuDoChayTau, PhanQuyenService.QuyenSuaMacTau);
        }

        [Theory]
        [InlineData(MaQuyen.SuaHaTang, true)]
        [InlineData(MaQuyen.LapBieuDoChayTau, true)]
        [InlineData(MaQuyen.XemMangLuoi, false)]
        public void KiemTraBatKyQuyen_MotTrongCacQuyenLaDu(string quyen, bool duoc)
        {
            try
            {
                DangNhap(quyen);
                bool ok = PhanQuyenService.KiemTraBatKyQuyen(PhanQuyenService.QuyenSuaMacTau, "sửa mác tàu", out string loi);
                Assert.Equal(duoc, ok);
                Assert.Equal(duoc, loi.Length == 0);
                if (!duoc) Assert.Contains(MaQuyen.LapBieuDoChayTau, loi);
            }
            finally { DangXuat(); }
        }

        [Fact]
        public void ChiXem_KhongBanVeHoanVeSoatVe()
        {
            try
            {
                DangNhap(MaQuyen.XemVe);
                var sv = new VeService();
                Assert.False(sv.HoanVe(1, 0.1m, "thử", "TIEN_MAT", 1, out string loiHoan));
                Assert.Contains(MaQuyen.HoanVe, loiHoan);
                Assert.False(sv.SoatVe(1, 1, out string loiSoat));
                Assert.Contains(MaQuyen.SoatVe, loiSoat);
            }
            finally { DangXuat(); }
        }

        [Fact]
        public void ChiXem_KhongLapVanDon()
        {
            try
            {
                DangNhap(MaQuyen.XemHangHoa);
                Assert.False(new VanDonHangService().Huy(1, out string loi));
                Assert.Contains(MaQuyen.LapVanDonHang, loi);
            }
            finally { DangXuat(); }
        }
    }

    public class LoaiKhachTuVanTests
    {
        [Fact]
        public void MotNguoi_KhongDanhDau_LaNguoiLon()
            => Assert.Equal(new[] { "THUONG" }, QuyTacTuVanVe.LoaiKhachDuKien(1, false, false, false));

        [Fact]
        public void MotNguoi_SinhVien()
            => Assert.Equal(new[] { "SINH_VIEN" }, QuyTacTuVanVe.LoaiKhachDuKien(1, false, false, true));

        [Fact]
        public void MotNguoi_NhieuO_UuTienCaoTuoi()
            => Assert.Equal(new[] { "NGUOI_GIA" }, QuyTacTuVanVe.LoaiKhachDuKien(1, true, true, true));

        [Fact]
        public void MotNguoi_SinhVienVaTreEm_LaSinhVien()
            => Assert.Equal(new[] { "SINH_VIEN" }, QuyTacTuVanVe.LoaiKhachDuKien(1, false, true, true));

        [Fact]
        public void BonNguoi_CaoTuoiTreEmSinhVien()
            => Assert.Equal(new[] { "NGUOI_GIA", "TRE_EM", "SINH_VIEN", "SINH_VIEN" },
                            QuyTacTuVanVe.LoaiKhachDuKien(4, true, true, true));

        [Fact]
        public void HaiNguoi_CoTreEm_ConLaiNguoiLon()
            => Assert.Equal(new[] { "TRE_EM", "THUONG" }, QuyTacTuVanVe.LoaiKhachDuKien(2, false, true, false));

        [Fact]
        public void SoLuongLuonDungSoNguoi()
        {
            for (int n = 1; n <= 6; n++)
                foreach (bool a in new[] { false, true })
                    foreach (bool b in new[] { false, true })
                        foreach (bool c in new[] { false, true })
                            Assert.Equal(n, QuyTacTuVanVe.LoaiKhachDuKien(n, a, b, c).Count);
        }

        [Fact]
        public void MoTa_GomNhomTheoLoai()
            => Assert.Equal("1 người cao tuổi · 2 sinh viên",
                            QuyTacTuVanVe.MoTaLoaiKhach(new[] { "NGUOI_GIA", "SINH_VIEN", "SINH_VIEN" }));
    }
}
