using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using DAL.Connection;
using DAL.Repositories;
using DTO.Common;
using DTO.PhuongTien;
using ET.PhuongTien;

namespace BUS.Services
{
    // =========================================================================
    // PHAN HE QUAN LY PHUONG TIEN (Dau may, Toa xe, Ghe)
    //
    // Doc: danh sach / ly lich / thong ke (DataTable cho tang giao dien).
    // Ghi: them / sua / xoa dau may va toa xe theo mau "bool Them(..., out string loi)":
    //   1. kiem quyen (PhanQuyenService),
    //   2. kiem du lieu nhap (cung ham ma dialog dung - KiemTraDauMay / KiemTraToaXe),
    //   3. trong MOT giao dich: kiem trung so hieu, luat trang thai theo chuyen dang dung,
    //      ghi, ghi nhat ky he thong (kiemtoan.NhatKyHeThong).
    // Lap doan tau: LapTauService.
    //
    // 14/10 (Dat chot): xoa han chi khi chua co du lieu nao tro toi (nhap nham); da co lich su
    // thi chuyen sang "Ngung khai thac" (NGUNG_KHAI_THAC) - giu ho so, khong van dung nua.
    // =========================================================================
    public class PhuongTienService
    {
        private readonly PhuongTienRepository _repo = new();

        // Chu ky bao duong dinh ky dau may (khop BaoTri: ChuKyBaoDuong.ChuKyR1Km / ChuKyR2Km).
        // R2 bao gom R1: lam R2 thi moc R1 cung tinh lai tu do.
        public const decimal MocBaoDuongR1 = 50_000m;
        public const decimal MocBaoDuongR2 = 150_000m;
        public const decimal NguongSapToiHanPhanTram = 90m;     // khop BaoTri: NguongSapToi

        // 14/10 rang buoc co ban (RangBuocNhapLieu): bien tren / duoi theo thuc te, chat hon kieu cot
        public const decimal SoKmToiDa = 10_000_000m;            // ~200,000 km/nam x 50 nam (cot DECIMAL(9,1))
        public const decimal KhoiLuongToaToiDaTan = 999.99m;     // DECIMAL(5,2) - gioi han o nhap
        public const decimal TuTrongToaToiThieuTan = 10m;
        public const decimal TuTrongToaToiDaTan = 100m;
        public const decimal TaiTrongToaToiDaTan = 100m;
        public const int DoDaiSoHieuToiDa = 20;                  // VARCHAR(20)
        public const int DoDaiDonViToiDa = 50;                   // NVARCHAR(50)
        public const int NamSanXuatNhoNhat = 1970;               // CHECK NamSanXuat >= 1970

        // 14/10: them NGUNG_KHAI_THAC (migration V20261014_0900) - giu ho so de tra cuu, khong van dung nua
        public const string NgungKhaiThac = "NGUNG_KHAI_THAC";
        public static readonly string[] TrangThaiDauMay = { "SAN_SANG", "DANG_CHAY", "BAO_DUONG", NgungKhaiThac };
        public static readonly string[] TrangThaiToaXe = { "SAN_SANG", "DA_NOI", "BAO_DUONG", NgungKhaiThac };

        // Goi y khi khong xoa duoc vi da co lich su
        public const string GoiYNgungKhaiThac =
            "Xóa sẽ làm mất lịch sử này. Nếu không còn dùng nữa, hãy chuyển sang \"Ngừng khai thác\": " +
            "hồ sơ vẫn giữ để tra cứu nhưng không đưa vào đoàn tàu được nữa.";


        // ---------------------------------------------------------------------
        // DAU MAY - DOC
        // ---------------------------------------------------------------------

        public DataTable LayDanhSachDauMay(string? tuKhoa = null, string? donViQuanLy = null, string? trangThai = null)
            => _repo.LayDanhSachDauMay(tuKhoa, donViQuanLy, trangThai);

        public DataTable LayDanhSachDonViQuanLy() => _repo.LayDanhSachDonViQuanLy();

        public DataTable LayDanhSachDongDauMay() => _repo.LayDanhSachDongDauMay();

        public DataTable LayLichSuVanDung(int maDauMay)
        {
            if (maDauMay <= 0) return new DataTable();
            return _repo.LayLichSuVanDung(maDauMay);
        }

        // Tien do bao duong theo CHU KY tu lan bao duong gan nhat (khop man hinh Bao tri).
        // kmR1GanNhat: km o lan R1 hoac R2 gan nhat; kmR2GanNhat: km o lan R2 gan nhat;
        // null = chua bao duong lan nao -> tinh tu 0 km.
        // Tra ve cap can chu y nhat: qua han > sap toi han > binh thuong; cung muc thi uu tien R2.
        public static TienDoBaoDuongDauMay TinhTienDoBaoDuong(decimal soKmTichLuy, decimal? kmR1GanNhat, decimal? kmR2GanNhat)
        {
            var r1 = TienDoMotCap("R1", MocBaoDuongR1, soKmTichLuy, kmR1GanNhat);
            var r2 = TienDoMotCap("R2", MocBaoDuongR2, soKmTichLuy, kmR2GanNhat);

            if (r2.MucNghiemTrong > r1.MucNghiemTrong) return r2;
            if (r1.MucNghiemTrong > r2.MucNghiemTrong) return r1;
            if (r2.MucNghiemTrong >= 2) return r2;
            return r1.TyLePhanTram >= r2.TyLePhanTram ? r1 : r2;
        }

        private static TienDoBaoDuongDauMay TienDoMotCap(string cap, decimal chuKy, decimal soKm, decimal? kmLanTruoc)
        {
            decimal moc = (kmLanTruoc ?? 0m) + chuKy;
            decimal daChay = Math.Max(0m, soKm - (kmLanTruoc ?? 0m));
            decimal tyLe = Math.Round(daChay / chuKy * 100m, 1);
            string muc = tyLe >= 100m ? "QUA_HAN" : tyLe >= NguongSapToiHanPhanTram ? "SAP_TOI" : "BINH_THUONG";
            return new TienDoBaoDuongDauMay(cap, chuKy, moc, daChay, tyLe, muc, kmLanTruoc.HasValue);
        }

        // ---------------------------------------------------------------------
        // DAU MAY - KIEM TRA NHAP LIEU (dialog va Service dung chung)
        // ---------------------------------------------------------------------

        public static void ChuanHoa(DauMay dm)
        {
            dm.SoHieuDauMay = (dm.SoHieuDauMay ?? "").Trim().ToUpperInvariant();
            dm.DonViQuanLy = RangBuocNhapLieu.ChuanHoa(dm.DonViQuanLy);
            dm.TrangThai = (dm.TrangThai ?? "").Trim().ToUpperInvariant();
        }

        // maDongCode: ma dong cua MaDongDauMay (null = chua chon / khong ton tai)
        public static List<LoiNhapLieuDTO> KiemTraDauMay(DauMay dm, string? maDongCode, bool laThemMoi)
        {
            var loi = new List<LoiNhapLieuDTO>();

            // So hieu = tien to dong may + phan so (chi chu so, 3-4 chu so) - RangBuocNhapLieu
            if (laThemMoi)
            {
                string sh = dm.SoHieuDauMay;
                if (maDongCode == null)
                {
                    if (sh.Length == 0) loi.Add(new("SoHieu", "Vui lòng nhập số hiệu đầu máy."));
                }
                else if (!sh.StartsWith(maDongCode + "-", StringComparison.Ordinal))
                    loi.Add(new("SoHieu", $"Tiền tố số hiệu phải khớp dòng máy đã chọn ({maDongCode}-...)."));
                else if (RangBuocNhapLieu.KiemTraPhanSoHieu(sh[(maDongCode.Length + 1)..], maDongCode + "-") is { } loiSo)
                    loi.Add(new("SoHieu", loiSo));
            }

            if (maDongCode == null)
                loi.Add(new("DongMay", "Vui lòng chọn dòng đầu máy."));

            if (RangBuocNhapLieu.KiemTraTenXiNghiep(dm.DonViQuanLy) is { } loiXn)
                loi.Add(new("XiNghiep", loiXn));

            int namNay = DateTime.Now.Year;
            if (dm.NamSanXuat < NamSanXuatNhoNhat || dm.NamSanXuat > namNay)
                loi.Add(new("NamSanXuat", $"Năm sản xuất phải từ {NamSanXuatNhoNhat} đến {namNay}."));

            if (dm.SoKmTichLuy < 0)
                loi.Add(new("SoKm", "Số km tích lũy không được âm."));
            else if (dm.SoKmTichLuy > SoKmToiDa)
                loi.Add(new("SoKm", $"Số km tích lũy tối đa {SoKmToiDa:N0} km."));
            else if (QuaSoChuSoLe(dm.SoKmTichLuy, 1))
                loi.Add(new("SoKm", "Số km tích lũy chỉ lấy 1 chữ số lẻ."));

            if (!TrangThaiDauMay.Contains(dm.TrangThai))
                loi.Add(new("TrangThai", "Trạng thái đầu máy không hợp lệ."));

            return loi;
        }

        // ---------------------------------------------------------------------
        // DAU MAY - GHI
        // ---------------------------------------------------------------------

        public bool ThemDauMay(DauMay dm, out string loi)
        {
            loi = "";
            if (!CoQuyen(MaQuyen.QuanLyPhuongTien, "thêm đầu máy", out loi)) return false;

            ChuanHoa(dm);
            try
            {
                string? maDong = _repo.LayMaDongCode(dm.MaDongDauMay);
                if (!HopLe(KiemTraDauMay(dm, maDong, laThemMoi: true), out loi)) return false;

                if (dm.TrangThai == "DANG_CHAY")
                {
                    loi = "Đầu máy mới chưa kéo chuyến nào nên không đặt trạng thái \"Đang chạy\".";
                    return false;
                }
                if (dm.TrangThai == NgungKhaiThac)
                {
                    loi = "Đầu máy mới không đặt trạng thái \"Ngừng khai thác\".";
                    return false;
                }

                using var gd = PhienGiaoDich.Mo();
                if (_repo.SoHieuDauMayDaTonTai(dm.SoHieuDauMay, gd))
                {
                    loi = $"Số hiệu đầu máy {dm.SoHieuDauMay} đã tồn tại.";
                    return false;
                }

                dm.MaDauMay = _repo.ThemDauMay(dm, gd);
                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "THEM", "phuongtien.DauMay",
                                    dm.MaDauMay.ToString(), null, Json(dm));
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // goc: ban ghi luc nguoi dung mo dialog (de phat hien nguoi khac sua chen giua)
        public bool CapNhatDauMay(DauMay moi, DauMay goc, out string loi)
        {
            loi = "";
            if (!CoQuyen(MaQuyen.QuanLyPhuongTien, "sửa đầu máy", out loi)) return false;

            ChuanHoa(moi);
            ChuanHoa(goc);
            if (moi.MaDauMay <= 0 || moi.MaDauMay != goc.MaDauMay)
            {
                loi = "Không xác định được đầu máy cần sửa.";
                return false;
            }
            if (moi.SoHieuDauMay != goc.SoHieuDauMay)
            {
                loi = "Số hiệu là khóa định danh, không sửa sau khi đã tạo.";
                return false;
            }
            if (moi.MaDongDauMay != goc.MaDongDauMay)
            {
                loi = "Không đổi dòng máy của đầu máy đã có (tiền tố số hiệu, bằng lái của kíp và thông số đoàn tàu đều theo dòng máy).";
                return false;
            }

            try
            {
                string? maDong = _repo.LayMaDongCode(moi.MaDongDauMay);
                if (!HopLe(KiemTraDauMay(moi, maDong, laThemMoi: false), out loi)) return false;

                using var gd = PhienGiaoDich.Mo();

                // Cung khoa voi LapTauService: doi trang thai (vd sang Bao duong) va gan dau may
                // vao doan tau khong duoc chen nhau, neu khong ca hai cung qua kiem tra.
                gd.KhoaTaiNguyen(LapTauService.TaiNguyenKhoa);

                var hienTai = _repo.LayDauMay(moi.MaDauMay, gd);
                if (hienTai == null)
                {
                    loi = "Đầu máy không còn trong CSDL (có thể đã bị xóa). Hãy nạp lại danh sách.";
                    return false;
                }
                if (!GiongNhau(hienTai, goc))
                {
                    loi = BaoDaBiSua(hienTai.SoHieuDauMay);
                    return false;
                }

                // So km khong thap hon so km da ghi o lan bao duong gan nhat
                decimal? kmBaoDuong = _repo.LayKmBaoDuongGanNhat(moi.MaDauMay, gd);
                if (kmBaoDuong.HasValue && moi.SoKmTichLuy < kmBaoDuong.Value)
                {
                    loi = $"Số km tích lũy ({moi.SoKmTichLuy:N1} km) không được nhỏ hơn số km ở lần bảo dưỡng gần nhất ({kmBaoDuong.Value:N1} km).";
                    return false;
                }

                if (moi.TrangThai != hienTai.TrangThai &&
                    !KiemTraDoiTrangThaiDauMay(moi, _repo.LayChuyenConHieuLucCuaDauMay(moi.MaDauMay, gd), out loi))
                    return false;

                if (_repo.CapNhatDauMay(moi, goc, gd) == 0)
                {
                    loi = BaoDaBiSua(moi.SoHieuDauMay);
                    return false;
                }

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "SUA", "phuongtien.DauMay",
                                    moi.MaDauMay.ToString(), Json(hienTai), Json(moi));
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // Luat doi trang thai (quyet dinh 09/10: nhap tay, Service chan cho mau thuan)
        private static bool KiemTraDoiTrangThaiDauMay(DauMay dm, DataTable chuyenConHieuLuc, out string loi)
        {
            loi = "";
            var chuyen = DocChuyen(chuyenConHieuLuc);
            var dangChay = chuyen.Where(c => QuyTacLapTau.ChuyenDangChay(c.TrangThai)).ToList();

            switch (dm.TrangThai)
            {
                case "BAO_DUONG" or NgungKhaiThac when chuyen.Count > 0:
                    loi = $"{dm.SoHieuDauMay} đang được gán cho {MoTaChuyen(chuyen)}. " +
                          "Hãy lập lại đoàn tàu các chuyến đó với đầu máy khác trước khi chuyển sang " +
                          $"{(dm.TrangThai == NgungKhaiThac ? "Ngừng khai thác" : "Bảo dưỡng")}.";
                    return false;
                case "DANG_CHAY" when dangChay.Count == 0:
                    loi = $"{dm.SoHieuDauMay} không kéo / đẩy chuyến nào đang chạy nên không đặt trạng thái \"Đang chạy\".";
                    return false;
                case "SAN_SANG" when dangChay.Count > 0:
                    loi = $"{dm.SoHieuDauMay} đang kéo / đẩy {MoTaChuyen(dangChay)} (đang chạy), chưa thể chuyển về \"Sẵn sàng\".";
                    return false;
            }
            return true;
        }

        // ---------------------------------------------------------------------
        // DAU MAY - XOA (14/10): chi khi chua co du lieu nao tro toi (nhap nham)
        // ---------------------------------------------------------------------

        // Doc truoc khi hoi xac nhan: cac dong mo ta du lieu dang tro toi; rong = xoa duoc
        public bool LayThamChieuDauMay(int maDauMay, out List<string> thamChieu, out string loi)
        {
            loi = "";
            thamChieu = new();
            try
            {
                thamChieu = ThamChieuXoa.MoTa(_repo.DemThamChieuDauMay(maDauMay));
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // goc: ban ghi luc nguoi dung chon (phat hien nguoi khac sua / xoa chen giua)
        public bool XoaDauMay(DauMay goc, out string loi)
        {
            loi = "";
            if (!CoQuyen(MaQuyen.QuanLyPhuongTien, "xóa đầu máy", out loi)) return false;

            ChuanHoa(goc);
            try
            {
                using var gd = PhienGiaoDich.Mo();

                // Cung khoa voi LapTauService: khong de dau may vua duoc gan vao doan tau chen giua
                gd.KhoaTaiNguyen(LapTauService.TaiNguyenKhoa);

                var hienTai = _repo.LayDauMay(goc.MaDauMay, gd);
                if (hienTai == null)
                {
                    loi = "Đầu máy không còn trong CSDL (có thể vừa bị xóa). Hãy nạp lại danh sách.";
                    return false;
                }
                if (!GiongNhau(hienTai, goc))
                {
                    loi = BaoDaBiSua(hienTai.SoHieuDauMay);
                    return false;
                }

                var thamChieu = ThamChieuXoa.MoTa(_repo.DemThamChieuDauMay(goc.MaDauMay, gd));
                if (thamChieu.Count > 0)
                {
                    loi = ThamChieuXoa.KhongXoaDuoc($"đầu máy {hienTai.SoHieuDauMay}", thamChieu, GoiYNgungKhaiThac);
                    return false;
                }

                if (_repo.XoaDauMay(goc, gd) == 0)
                {
                    loi = BaoDaBiSua(goc.SoHieuDauMay);
                    return false;
                }

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "XOA", "phuongtien.DauMay",
                                    goc.MaDauMay.ToString(), Json(hienTai), null);
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // ---------------------------------------------------------------------
        // DOI TOA XE - DOC
        // ---------------------------------------------------------------------

        public DataTable LayDanhSachToaXeDoi(string? tuKhoa = null, int? maChungLoai = null, string? trangThai = null)
            => _repo.LayDanhSachToaXeDoi(tuKhoa, maChungLoai, trangThai);

        public DataTable LayDanhSachChungLoaiToa() => _repo.LayDanhSachChungLoaiToa();

        public static decimal TinhTaiTrongTruc(decimal tuTrongTan, decimal taiTrongTan, int soTruc)
            => soTruc > 0 ? Math.Round((tuTrongTan + taiTrongTan) / soTruc, 2) : 0m;

        // ---------------------------------------------------------------------
        // DOI TOA XE - KIEM TRA NHAP LIEU (dialog va Service dung chung)
        // ---------------------------------------------------------------------

        public static void ChuanHoa(ToaXe tx)
        {
            tx.SoHieuToaXe = (tx.SoHieuToaXe ?? "").Trim().ToUpperInvariant();
            tx.TrangThai = (tx.TrangThai ?? "").Trim().ToUpperInvariant();
        }

        public static List<LoiNhapLieuDTO> KiemTraToaXe(ToaXe tx, string? maChungLoaiCode, bool laThemMoi)
        {
            var loi = new List<LoiNhapLieuDTO>();

            if (maChungLoaiCode == null)
                loi.Add(new("ChungLoai", "Vui lòng chọn chủng loại toa."));

            // So hieu = ma chung loai + "-" + phan so (chi chu so, 3-4 chu so) - RangBuocNhapLieu
            if (laThemMoi)
            {
                string sh = tx.SoHieuToaXe;
                if (sh.Length == 0)
                    loi.Add(new("SoHieu", "Vui lòng nhập số hiệu toa xe."));
                else if (maChungLoaiCode != null && !sh.StartsWith(maChungLoaiCode + "-", StringComparison.Ordinal))
                    loi.Add(new("SoHieu", $"Số hiệu phải bắt đầu bằng \"{maChungLoaiCode}-\" theo chủng loại đã chọn (ví dụ {maChungLoaiCode}-222)."));
                else if (maChungLoaiCode != null &&
                         RangBuocNhapLieu.KiemTraPhanSoHieu(sh[(maChungLoaiCode.Length + 1)..], maChungLoaiCode + "-") is { } loiSo)
                    loi.Add(new("SoHieu", loiSo));
            }

            if (tx.TuTrongTan < TuTrongToaToiThieuTan || tx.TuTrongTan > TuTrongToaToiDaTan)
                loi.Add(new("TuTrong", $"Tự trọng phải từ {TuTrongToaToiThieuTan:N0} đến {TuTrongToaToiDaTan:N0} tấn."));
            else if (QuaSoChuSoLe(tx.TuTrongTan, 2))
                loi.Add(new("TuTrong", "Tự trọng chỉ lấy 2 chữ số lẻ."));

            if (tx.TaiTrongToiDaTan < 0 || tx.TaiTrongToiDaTan > TaiTrongToaToiDaTan)
                loi.Add(new("TaiTrong", $"Tải trọng phải từ 0 đến {TaiTrongToaToiDaTan:N0} tấn."));
            else if (QuaSoChuSoLe(tx.TaiTrongToiDaTan, 2))
                loi.Add(new("TaiTrong", "Tải trọng chỉ lấy 2 chữ số lẻ."));

            if (!TrangThaiToaXe.Contains(tx.TrangThai))
                loi.Add(new("TrangThai", "Trạng thái toa xe không hợp lệ."));

            return loi;
        }

        // ---------------------------------------------------------------------
        // DOI TOA XE - GHI
        // ---------------------------------------------------------------------

        public bool ThemToaXe(ToaXe tx, out string loi)
        {
            loi = "";
            if (!CoQuyen(MaQuyen.QuanLyPhuongTien, "thêm toa xe", out loi)) return false;

            ChuanHoa(tx);
            try
            {
                string? maChungLoai = _repo.LayMaChungLoaiCode(tx.MaChungLoai);
                if (!HopLe(KiemTraToaXe(tx, maChungLoai, laThemMoi: true), out loi)) return false;

                if (tx.TrangThai == "DA_NOI")
                {
                    loi = "Toa mới chưa thuộc đoàn tàu nào nên không đặt trạng thái \"Đã nối\".";
                    return false;
                }
                if (tx.TrangThai == NgungKhaiThac)
                {
                    loi = "Toa mới không đặt trạng thái \"Ngừng khai thác\".";
                    return false;
                }

                using var gd = PhienGiaoDich.Mo();
                if (_repo.SoHieuToaXeDaTonTai(tx.SoHieuToaXe, gd))
                {
                    loi = $"Số hiệu toa xe {tx.SoHieuToaXe} đã tồn tại.";
                    return false;
                }

                tx.MaToaXe = _repo.ThemToaXe(tx, gd);
                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "THEM", "phuongtien.ToaXe",
                                    tx.MaToaXe.ToString(), null, Json(tx));
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        public bool CapNhatToaXe(ToaXe moi, ToaXe goc, out string loi)
        {
            loi = "";
            if (!CoQuyen(MaQuyen.QuanLyPhuongTien, "sửa toa xe", out loi)) return false;

            ChuanHoa(moi);
            ChuanHoa(goc);
            if (moi.MaToaXe <= 0 || moi.MaToaXe != goc.MaToaXe)
            {
                loi = "Không xác định được toa xe cần sửa.";
                return false;
            }
            if (moi.SoHieuToaXe != goc.SoHieuToaXe)
            {
                loi = "Số hiệu là khóa định danh, không sửa sau khi đã tạo.";
                return false;
            }
            if (moi.MaChungLoai != goc.MaChungLoai)
            {
                loi = "Không đổi chủng loại của toa đã có (tiền tố số hiệu theo chủng loại). Hãy tạo toa mới nếu cần.";
                return false;
            }

            try
            {
                string? maChungLoai = _repo.LayMaChungLoaiCode(moi.MaChungLoai);
                if (!HopLe(KiemTraToaXe(moi, maChungLoai, laThemMoi: false), out loi)) return false;

                using var gd = PhienGiaoDich.Mo();

                // Cung khoa voi LapTauService (xem CapNhatDauMay)
                gd.KhoaTaiNguyen(LapTauService.TaiNguyenKhoa);

                var hienTai = _repo.LayToaXe(moi.MaToaXe, gd);
                if (hienTai == null)
                {
                    loi = "Toa xe không còn trong CSDL (có thể đã bị xóa). Hãy nạp lại danh sách.";
                    return false;
                }
                if (!GiongNhau(hienTai, goc))
                {
                    loi = BaoDaBiSua(hienTai.SoHieuToaXe);
                    return false;
                }

                if (moi.TrangThai != hienTai.TrangThai &&
                    !KiemTraDoiTrangThaiToaXe(moi, _repo.LayChuyenConHieuLucCuaToaXe(moi.MaToaXe, gd), out loi))
                    return false;

                if (_repo.CapNhatToaXe(moi, goc, gd) == 0)
                {
                    loi = BaoDaBiSua(moi.SoHieuToaXe);
                    return false;
                }

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "SUA", "phuongtien.ToaXe",
                                    moi.MaToaXe.ToString(), Json(hienTai), Json(moi));
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        private static bool KiemTraDoiTrangThaiToaXe(ToaXe tx, DataTable chuyenConHieuLuc, out string loi)
        {
            loi = "";
            var chuyen = DocChuyen(chuyenConHieuLuc);
            var dangChay = chuyen.Where(c => QuyTacLapTau.ChuyenDangChay(c.TrangThai)).ToList();

            switch (tx.TrangThai)
            {
                case "BAO_DUONG" or NgungKhaiThac when chuyen.Count > 0:
                    loi = $"Toa {tx.SoHieuToaXe} đang nằm trong đoàn tàu của {MoTaChuyen(chuyen)}. " +
                          "Hãy tháo toa khỏi các đoàn tàu đó trước khi chuyển sang " +
                          $"{(tx.TrangThai == NgungKhaiThac ? "Ngừng khai thác" : "Bảo dưỡng")}.";
                    return false;
                case "DA_NOI" when chuyen.Count == 0:
                    loi = $"Toa {tx.SoHieuToaXe} chưa thuộc đoàn tàu nào của chuyến còn hiệu lực nên không đặt trạng thái \"Đã nối\".";
                    return false;
                case "SAN_SANG" when dangChay.Count > 0:
                    loi = $"Toa {tx.SoHieuToaXe} đang chạy trong đoàn {MoTaChuyen(dangChay)}, chưa thể chuyển về \"Sẵn sàng\".";
                    return false;
            }
            return true;
        }

        // ---------------------------------------------------------------------
        // DOI TOA XE - XOA (14/10): nhu dau may
        // ---------------------------------------------------------------------

        public bool LayThamChieuToaXe(int maToaXe, out List<string> thamChieu, out string loi)
        {
            loi = "";
            thamChieu = new();
            try
            {
                thamChieu = ThamChieuXoa.MoTa(_repo.DemThamChieuToaXe(maToaXe));
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        public bool XoaToaXe(ToaXe goc, out string loi)
        {
            loi = "";
            if (!CoQuyen(MaQuyen.QuanLyPhuongTien, "xóa toa xe", out loi)) return false;

            ChuanHoa(goc);
            try
            {
                using var gd = PhienGiaoDich.Mo();
                gd.KhoaTaiNguyen(LapTauService.TaiNguyenKhoa);

                var hienTai = _repo.LayToaXe(goc.MaToaXe, gd);
                if (hienTai == null)
                {
                    loi = "Toa xe không còn trong CSDL (có thể vừa bị xóa). Hãy nạp lại danh sách.";
                    return false;
                }
                if (!GiongNhau(hienTai, goc))
                {
                    loi = BaoDaBiSua(hienTai.SoHieuToaXe);
                    return false;
                }

                var thamChieu = ThamChieuXoa.MoTa(_repo.DemThamChieuToaXe(goc.MaToaXe, gd));
                if (thamChieu.Count > 0)
                {
                    loi = ThamChieuXoa.KhongXoaDuoc($"toa {hienTai.SoHieuToaXe}", thamChieu, GoiYNgungKhaiThac);
                    return false;
                }

                if (_repo.XoaToaXe(goc, gd) == 0)
                {
                    loi = BaoDaBiSua(goc.SoHieuToaXe);
                    return false;
                }

                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "XOA", "phuongtien.ToaXe",
                                    goc.MaToaXe.ToString(), Json(hienTai), null);
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // ---------------------------------------------------------------------
        // DOAN TAU / SO DO GHE / TONG QUAN (doc)
        // ---------------------------------------------------------------------

        public DataTable LayDanhSachChuyenTauLapTau() => _repo.LayDanhSachChuyenTauLapTau();

        // Toa ban ve cua chuyen (vantai.ToaXeKhach) - dung cho tab So do cho ngoi
        public DataTable LayBienCheToaXe(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayBienCheToaXe(maChuyenTau);
        }

        public static (bool DatYeuCau, decimal TyLePhanTram, string ThongDiep) DoiChieuSucKeo(decimal tongTrongLuongTan, decimal? sucKeoToiDaTan)
            => QuyTacLapTau.DoiChieuSucKeo(tongTrongLuongTan, sucKeoToiDaTan);

        public DataTable LaySoDoGhe(int maToaXeKhach, int maChuyenTau)
        {
            if (maToaXeKhach <= 0 || maChuyenTau <= 0) return new DataTable();
            return _repo.LaySoDoGhe(maToaXeKhach, maChuyenTau);
        }

        public DataTable LayHanhKhachTrenToa(int maToaXeKhach, int maChuyenTau)
        {
            if (maToaXeKhach <= 0 || maChuyenTau <= 0) return new DataTable();
            return _repo.LayHanhKhachTrenToa(maToaXeKhach, maChuyenTau);
        }

        public DataTable LayThongKeTongQuan() => _repo.LayThongKeTongQuan();

        // ---------------------------------------------------------------------
        // TIEN ICH
        // ---------------------------------------------------------------------

        public static bool QuaSoChuSoLe(decimal giaTri, int soChuSoLe) => decimal.Round(giaTri, soChuSoLe) != giaTri;

        private static bool CoQuyen(string maQuyen, string hanhDong, out string loi)
        {
            loi = PhanQuyenService.CoQuyen(maQuyen)
                ? ""
                : $"Tài khoản \"{PhanQuyenService.TenDangNhapHienTai}\" không có quyền {hanhDong} (cần quyền {maQuyen}).";
            return loi.Length == 0;
        }

        private static bool HopLe(List<LoiNhapLieuDTO> dsLoi, out string loi)
        {
            loi = dsLoi.Count > 0 ? dsLoi[0].ThongDiep : "";
            return dsLoi.Count == 0;
        }

        private static string BaoDaBiSua(string soHieu)
            => $"{soHieu} vừa được người khác thay đổi sau khi bạn mở hồ sơ. Hãy nạp lại danh sách rồi sửa lại.";

        private static bool GiongNhau(DauMay a, DauMay b)
            => a.SoHieuDauMay == b.SoHieuDauMay && a.MaDongDauMay == b.MaDongDauMay && a.DonViQuanLy == b.DonViQuanLy
            && a.NamSanXuat == b.NamSanXuat && a.SoKmTichLuy == b.SoKmTichLuy && a.TrangThai == b.TrangThai;

        private static bool GiongNhau(ToaXe a, ToaXe b)
            => a.SoHieuToaXe == b.SoHieuToaXe && a.MaChungLoai == b.MaChungLoai && a.TuTrongTan == b.TuTrongTan
            && a.TaiTrongToiDaTan == b.TaiTrongToiDaTan && a.TrangThai == b.TrangThai;

        private static List<ChuyenSuDungDTO> DocChuyen(DataTable bang) => bang.Rows.Cast<DataRow>().Select(r => new ChuyenSuDungDTO
        {
            MaChuyenTau = Convert.ToInt32(r["MaChuyenTau"]),
            SoHieuMacTau = r["SoHieuMacTau"].ToString() ?? "",
            TrangThai = r["TrangThai"].ToString() ?? "",
            GioXuatPhatKH = Convert.ToDateTime(r["GioXuatPhatKH"]),
            GioVeDichKH = Convert.ToDateTime(r["GioVeDichKH"]),
            VaiTro = r["VaiTro"].ToString() ?? ""
        }).ToList();

        private static string MoTaChuyen(IEnumerable<ChuyenSuDungDTO> ds)
            => string.Join(", ", ds.Select(c => $"{c.SoHieuMacTau} ({c.KhungGio})"));

        private static string Json(object doiTuong) => JsonSerializer.Serialize(doiTuong);
    }

    // Tien do bao duong cua mot cap (R1 / R2). MucCanhBao: QUA_HAN | SAP_TOI | BINH_THUONG
    public record TienDoBaoDuongDauMay(string Cap, decimal ChuKyKm, decimal MocKm, decimal DaChayKm,
                                       decimal TyLePhanTram, string MucCanhBao, bool CoLichSu)
    {
        public decimal ConLaiKm => ChuKyKm - DaChayKm;

        public int MucNghiemTrong => MucCanhBao switch { "QUA_HAN" => 3, "SAP_TOI" => 2, _ => 1 };

        // "còn 1,500 km" / "quá 60,000 km"
        public string MoTaConLai => ConLaiKm >= 0 ? $"còn {ConLaiKm:N0} km" : $"quá {-ConLaiKm:N0} km";
    }
}
