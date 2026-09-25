using System.Data;
using DAL.Repositories;

namespace BUS.Services
{
    // =========================================================================
    // PHAN HE QUAN LY PHUONG TIEN (Doan tau, Dau may, Toa xe & Ghe)
    // Tang nghiep vu chi doc: kiem tra tham so dau vao + cac quy tac tinh toan
    // hien thi (cap bao duong, he so su dung suc keo). Khong ghi du lieu.
    // =========================================================================
    public class PhuongTienService
    {
        private readonly PhuongTienRepository _repo = new();

        // Moc km bao duong dinh ky dau may diesel theo cap (tham so hien thi).
        public const decimal MocBaoDuongR1 = 50_000m;
        public const decimal MocBaoDuongR2 = 150_000m;
        public const decimal MocBaoDuongRo = 300_000m;

        // ---------------------------------------------------------------------
        // DAU MAY
        // ---------------------------------------------------------------------

        public DataTable LayDanhSachDauMay(string? tuKhoa = null, string? donViQuanLy = null, string? trangThai = null)
            => _repo.LayDanhSachDauMay(tuKhoa, donViQuanLy, trangThai);

        public DataTable LayDanhSachDonViQuanLy() => _repo.LayDanhSachDonViQuanLy();

        public DataTable LayLichSuVanDung(int maDauMay)
        {
            if (maDauMay <= 0) return new DataTable();
            return _repo.LayLichSuVanDung(maDauMay);
        }

        // Xac dinh cap bao duong ke tiep va ty le da chay toi moc do.
        public static (string CapKeTiep, decimal MocKm, decimal TyLePhanTram, string MucCanhBao) TinhTienDoBaoDuong(decimal soKmTichLuy)
        {
            decimal moc;
            string cap;

            if (soKmTichLuy < MocBaoDuongR1)
            {
                cap = "R1";
                moc = MocBaoDuongR1;
            }
            else if (soKmTichLuy < MocBaoDuongR2)
            {
                cap = "R2";
                moc = MocBaoDuongR2;
            }
            else if (soKmTichLuy < MocBaoDuongRo)
            {
                cap = "Ro";
                moc = MocBaoDuongRo;
            }
            else
            {
                // Da vuot moc dai tu, tinh theo chu ky Ro tiep theo
                cap = "Ro";
                moc = MocBaoDuongRo;
                return ("Ro", moc, 100m, "QUA_HAN");
            }

            decimal tyLe = moc > 0 ? Math.Round(soKmTichLuy / moc * 100m, 1) : 0m;
            if (tyLe > 100m) tyLe = 100m;

            string mucCanhBao = tyLe switch
            {
                >= 95m => "KHAN_CAP",
                >= 85m => "CANH_BAO",
                _ => "BINH_THUONG"
            };

            return (cap, moc, tyLe, mucCanhBao);
        }

        // ---------------------------------------------------------------------
        // DOI TOA XE
        // ---------------------------------------------------------------------

        public DataTable LayDanhSachToaXeDoi(string? tuKhoa = null, int? maChungLoai = null, string? trangThai = null)
            => _repo.LayDanhSachToaXeDoi(tuKhoa, maChungLoai, trangThai);

        public DataTable LayDanhSachChungLoaiToa() => _repo.LayDanhSachChungLoaiToa();

        // ---------------------------------------------------------------------
        // DOAN TAU
        // ---------------------------------------------------------------------

        public DataTable LayDanhSachChuyenTauLapTau() => _repo.LayDanhSachChuyenTauLapTau();

        public DataTable LayBienCheToaXe(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayBienCheToaXe(maChuyenTau);
        }

        // Doi chieu chieu dai doan tau voi duong tranh ngan nhat tren hanh trinh.
        public static (bool DatYeuCau, string ThongDiep) DoiChieuChieuDaiDuongTranh(decimal tongChieuDaiM, decimal? duongTranhNganNhatM)
        {
            if (duongTranhNganNhatM is null or <= 0)
                return (true, "Chưa có dữ liệu đường tránh trên hành trình để đối chiếu.");

            decimal duTru = duongTranhNganNhatM.Value - tongChieuDaiM;

            if (duTru < 0)
                return (false, $"Vượt {Math.Abs(duTru):N1} m so với đường tránh ngắn nhất ({duongTranhNganNhatM.Value:N0} m).");

            if (duTru < 20m)
                return (true, $"Sát giới hạn: chỉ còn dự trữ {duTru:N1} m so với đường tránh ngắn nhất.");

            return (true, $"Đạt yêu cầu: còn dự trữ {duTru:N1} m so với đường tránh ngắn nhất.");
        }

        // Doi chieu tong trong luong doan tau voi suc keo dinh muc cua dau may chinh.
        public static (bool DatYeuCau, decimal TyLePhanTram, string ThongDiep) DoiChieuSucKeo(decimal tongTrongLuongTan, decimal? sucKeoToiDaTan)
        {
            if (sucKeoToiDaTan is null or <= 0)
                return (true, 0m, "Chưa chỉ định đầu máy kéo chính để đối chiếu sức kéo.");

            decimal tyLe = Math.Round(tongTrongLuongTan / sucKeoToiDaTan.Value * 100m, 1);

            if (tyLe > 100m)
                return (false, tyLe, $"Quá tải {tyLe:N1}% định mức sức kéo ({sucKeoToiDaTan.Value:N0} tấn).");

            if (tyLe > 90m)
                return (true, tyLe, $"Sát định mức: đang dùng {tyLe:N1}% sức kéo khả dụng.");

            return (true, tyLe, $"Đạt yêu cầu: đang dùng {tyLe:N1}% sức kéo khả dụng.");
        }

        // ---------------------------------------------------------------------
        // SO DO CHO NGOI
        // ---------------------------------------------------------------------

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

        // ---------------------------------------------------------------------
        // TONG QUAN
        // ---------------------------------------------------------------------

        public DataTable LayThongKeTongQuan() => _repo.LayThongKeTongQuan();
    }
}
