using BUS.Services;
using DTO.Common;

namespace Tests.QuanTri
{
    // Sua loi kiem thu giao dien 09/11:
    //   - SYS_CONFIG chi danh cho vai tro ADMIN (khong cap them cho vai tro khac; dang nhap bo qua neu khong co ADMIN);
    //   - nut "Chuyen Trang Thai" o Lich trinh can quyen TRAIN_SCHEDULE_EDIT.
    public class QuyenHeThongTests
    {
        [Fact]
        public void VaiTroKhac_ThemSysConfig_BiChan()
        {
            string? loi = QuanTriService.LoiCapQuyenHeThong("QUAN_LY",
                new[] { MaQuyen.XemBaoCao }, new[] { MaQuyen.XemBaoCao, MaQuyen.CauHinhHeThong });
            Assert.NotNull(loi);
            Assert.Contains("chỉ dành cho vai trò Quản trị viên", loi);
        }

        [Fact]
        public void VaiTroKhac_LoCoSanSysConfig_VanLuuDuoc_VaBoDuoc()
        {
            // co san (sua tay CSDL) -> giu nguyen hoac bo tick deu khong bi chan
            Assert.Null(QuanTriService.LoiCapQuyenHeThong("QUAN_LY",
                new[] { MaQuyen.CauHinhHeThong }, new[] { MaQuyen.CauHinhHeThong, MaQuyen.XemBaoCao }));
            Assert.Null(QuanTriService.LoiCapQuyenHeThong("QUAN_LY",
                new[] { MaQuyen.CauHinhHeThong }, new[] { MaQuyen.XemBaoCao }));
        }

        [Fact]
        public void VaiTroKhac_KhongDungSysConfig_KhongBiChan()
        {
            Assert.Null(QuanTriService.LoiCapQuyenHeThong("DIEU_DO",
                new[] { MaQuyen.XemMangLuoi }, new[] { MaQuyen.XemMangLuoi, MaQuyen.XemHangHoa }));
        }

        [Fact]
        public void VaiTroAdmin_LuonKhongBiChan()
        {
            Assert.Null(QuanTriService.LoiCapQuyenHeThong(QuanTriService.VaiTroQuanTri,
                Array.Empty<string>(), new[] { MaQuyen.CauHinhHeThong }));
        }

        [Fact]
        public void DangNhap_KhongCoVaiTroAdmin_BoSysConfig()
        {
            var q = PhanQuyenService.QuyenHieuLuc(
                new[] { MaQuyen.XemBaoCao, MaQuyen.CauHinhHeThong, MaQuyen.XemBaoCao }, new[] { "QUAN_LY" });
            Assert.DoesNotContain(MaQuyen.CauHinhHeThong, q);
            Assert.Equal(new[] { MaQuyen.XemBaoCao }, q);
        }

        [Fact]
        public void DangNhap_CoVaiTroAdmin_GiuSysConfig()
        {
            var q = PhanQuyenService.QuyenHieuLuc(
                new[] { MaQuyen.CauHinhHeThong, MaQuyen.XemBaoCao }, new[] { "QUAN_LY", QuanTriService.VaiTroQuanTri });
            Assert.Contains(MaQuyen.CauHinhHeThong, q);
            Assert.Equal(2, q.Count);
        }
    }

    [Collection("PhienLamViec")]
    public class DoiTrangThaiChuyenQuyenTests
    {
        [Fact]
        public void ChiXem_KhongDoiDuocTrangThaiChuyen()
        {
            try
            {
                PhienLamViec.BatBuocDangNhap = true;
                PhienLamViec.DangNhap(11, "quanly", "Cán bộ quản lý", null,
                    new[] { MaQuyen.XemMangLuoi, MaQuyen.XemBaoCao }, new[] { "Cán bộ quản lý" });

                // Bi chan truoc khi doc CSDL -> chay duoc khong can SQL Server
                bool ok = new ChuyenTauService().DoiTrangThai(5, "DA_LEN_LICH", out string loi);
                Assert.False(ok);
                Assert.Contains(MaQuyen.LapBieuDoChayTau, loi);
            }
            finally
            {
                PhienLamViec.DangXuat();
                PhienLamViec.BatBuocDangNhap = false;
            }
        }

        [Fact]
        public void QuyenDoiTrangThai_LaLapBieuDoChayTau()
            => Assert.Equal("TRAIN_SCHEDULE_EDIT", ChuyenTauService.MaQuyenDoiTrangThai);
    }
}
