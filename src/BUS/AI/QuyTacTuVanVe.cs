using System;
using System.Collections.Generic;
using System.Linq;
using DTO.AI;

namespace BUS.AI
{
    // =========================================================================
    // LUAT CHAM DIEM TU VAN VE (MCDM) - ham thuan, unit test duoc (Tests/AI).
    //
    //   Score = Σ w_i · c_i ,  c_i ∈ [0, 10],  Σ w_i = 1          (SRS §3.2)
    //   4 tieu chi (thu tu trong mang): 0 tien nghi · 1 ngan sach · 2 khung gio / ngu dem · 3 suc khoe & doi tuong.
    //   Trong so mac dinh = bang vi du SRS: 0.35 · 0.25 · 0.20 · 0.20.
    //   Nhom cu ly: < 250 km ngan · 250–650 km trung binh · >= 650 km dai (SRS tieu chi 1).
    //
    // Bang diem c_i la DIEM CHUYEN GIA (tham khao), chon de tai tao dung bang vi du cua SRS
    // (cham Ha Noi -> Da Nang ~791 km, tau dem). Ghi vao bao cao: trong so / diem chua hoc tu du lieu.
    // =========================================================================
    public static class QuyTacTuVanVe
    {
        public const int TieuChiTienNghi = 0, TieuChiNganSach = 1, TieuChiKhungGio = 2, TieuChiSucKhoe = 3;
        public static readonly string[] TenTieuChi = { "Tiện nghi", "Ngân sách", "Khung giờ / ngủ đêm", "Sức khỏe & đối tượng" };
        public static readonly double[] TrongSoMacDinh = { 0.35, 0.25, 0.20, 0.20 };

        public const decimal NguongChangNganKm = 250m;
        public const decimal NguongChangDaiKm = 650m;
        public const double SoGioDemToiThieu = 3.0;      // >= 3 gio nam trong 22:00-06:00 thi coi la tau dem

        public enum NhomCuLy { Ngan, TrungBinh, Dai }

        // [12/11] Loai khach du kien cua doan (form chi co o danh dau, khong co so luong tung loai):
        // co nguoi cao tuoi -> 1 ve NGUOI_GIA; co tre em -> 1 ve TRE_EM (con cho); con lai SINH_VIEN neu danh dau "hoc sinh /
        // sinh vien", khong thi THUONG. Di 1 minh ma danh nhieu o: uu tien cao tuoi > sinh vien > tre em.
        // Nhan vien ban ve sua lai tung ve trong gio neu doan khac du kien.
        public static List<string> LoaiKhachDuKien(int soNguoi, bool caoTuoi, bool treEm, bool sinhVien)
        {
            int n = Math.Max(1, soNguoi);
            var ds = new List<string>();
            if (caoTuoi) ds.Add("NGUOI_GIA");
            if (treEm && ds.Count < n && !(n == 1 && sinhVien)) ds.Add("TRE_EM");
            while (ds.Count < n) ds.Add(sinhVien ? "SINH_VIEN" : "THUONG");
            return ds.Take(n).ToList();
        }

        public static string TenLoaiKhach(string ma) => ma switch
        {
            "NGUOI_GIA" => "người cao tuổi",
            "TRE_EM" => "trẻ em",
            "SINH_VIEN" => "sinh viên",
            _ => "người lớn"
        };

        // "1 người cao tuổi · 2 sinh viên"
        public static string MoTaLoaiKhach(IEnumerable<string> cacLoai)
            => string.Join(" · ", cacLoai.GroupBy(x => x).OrderBy(g => g.Key == "THUONG" ? 0 : 1)
                                          .Select(g => $"{g.Count()} {TenLoaiKhach(g.Key)}"));

        public static NhomCuLy XepNhomCuLy(decimal km)
            => km < NguongChangNganKm ? NhomCuLy.Ngan : km < NguongChangDaiKm ? NhomCuLy.TrungBinh : NhomCuLy.Dai;

        // Loai cho de xuat theo cu ly (SRS tieu chi 1) - dung de giai thich
        public static string[] LoaiChoUuTien(decimal km) => XepNhomCuLy(km) switch
        {
            NhomCuLy.Ngan => new[] { "NML", "NC" },
            NhomCuLy.TrungBinh => new[] { "NML", "BN" },
            _ => new[] { "AN", "BN" }
        };

        // --- c0: tien nghi theo loai cho + cu ly ----------------------------------
        public static double DiemTienNghi(string loai, int? tang, decimal km)
        {
            int t = tang ?? 1;
            return (XepNhomCuLy(km), loai) switch
            {
                (NhomCuLy.Dai, "AN") => t == 1 ? 9.5 : 9.0,
                (NhomCuLy.Dai, "BN") => t == 1 ? 8.5 : t == 2 ? 8.0 : 7.0,
                (NhomCuLy.Dai, "NML") => 4.0,
                (NhomCuLy.Dai, "NC") => 1.0,
                (NhomCuLy.TrungBinh, "AN") => t == 1 ? 9.0 : 8.5,
                (NhomCuLy.TrungBinh, "BN") => t == 1 ? 8.0 : t == 2 ? 7.5 : 6.5,
                (NhomCuLy.TrungBinh, "NML") => 7.0,
                (NhomCuLy.TrungBinh, "NC") => 3.5,
                (NhomCuLy.Ngan, "AN") => t == 1 ? 7.5 : 7.0,
                (NhomCuLy.Ngan, "BN") => t == 1 ? 7.0 : t == 2 ? 6.5 : 6.0,
                (NhomCuLy.Ngan, "NML") => 8.5,
                (NhomCuLy.Ngan, "NC") => 6.5,
                _ => 5.0
            };
        }

        // --- c1: ngan sach: 10 x (gia re nhat / gia nay); vuot ngan sach toi da -> tru 5 -------------
        public static double DiemNganSach(decimal gia, decimal giaReNhat, decimal? nganSachToiDa)
        {
            if (gia <= 0 || giaReNhat <= 0) return 5.0;
            double d = Math.Min(10.0, 10.0 * (double)(giaReNhat / gia));
            if (nganSachToiDa.HasValue && gia > nganSachToiDa.Value) d = Math.Max(0, d - 5.0);
            return Math.Round(d, 2);
        }

        // --- c2: khung gio / ngu dem ----------------------------------------------
        public static double DiemKhungGio(string loai, int? tang, bool quaDem, string khungGio, DateTime gioDen)
        {
            int t = tang ?? 1;
            double d;
            if (quaDem)
            {
                d = loai switch
                {
                    "AN" => 9.5,
                    "BN" => t == 1 ? 9.0 : t == 2 ? 8.5 : 7.5,
                    "NML" => 3.0,
                    "NC" => 1.0,
                    _ => 5.0
                };
                if (khungGio == KhungGioUuTien.TranhDiDem) d = Math.Min(d, 4.0) * 0.5;
            }
            else
            {
                d = loai switch
                {
                    "NML" => 8.0,
                    "NC" => 6.0,
                    "AN" => 7.5,
                    "BN" => t == 1 ? 7.0 : t == 2 ? 6.5 : 6.0,
                    _ => 5.0
                };
                if (khungGio == KhungGioUuTien.NguTrenTau) d *= 0.6;
            }
            if (DenGiuaDem(gioDen)) d -= 1.5;      // den ga 23:00-05:00 bat tien
            return Math.Round(Math.Clamp(d, 0, 10), 2);
        }

        // --- c3: suc khoe & doi tuong ---------------------------------------------
        public static double DiemSucKhoe(string loai, int? tang, decimal km, bool caoTuoi, bool treEm)
        {
            int t = tang ?? 1;
            double d = (XepNhomCuLy(km), loai) switch
            {
                (NhomCuLy.Dai, "AN") => t == 1 ? 9.5 : 8.0,
                (NhomCuLy.Dai, "BN") => t == 1 ? 9.0 : t == 2 ? 7.5 : 5.5,
                (NhomCuLy.Dai, "NML") => 4.0,
                (NhomCuLy.Dai, "NC") => 1.0,
                (NhomCuLy.TrungBinh, "AN") => t == 1 ? 9.0 : 8.0,
                (NhomCuLy.TrungBinh, "BN") => t == 1 ? 8.5 : t == 2 ? 7.5 : 6.0,
                (NhomCuLy.TrungBinh, "NML") => 6.5,
                (NhomCuLy.TrungBinh, "NC") => 3.5,
                (NhomCuLy.Ngan, "AN") => t == 1 ? 8.5 : 8.0,
                (NhomCuLy.Ngan, "BN") => t == 1 ? 8.0 : t == 2 ? 7.5 : 6.5,
                (NhomCuLy.Ngan, "NML") => 8.0,
                (NhomCuLy.Ngan, "NC") => 6.0,
                _ => 5.0
            };
            bool laGiuong = loai is "AN" or "BN";
            if (caoTuoi)
            {
                if (laGiuong) d += t == 1 ? 0.5 : t == 2 ? -1.5 : -3.0;
                if (loai == "NC") d -= 1.0;
            }
            if (treEm)
            {
                if (laGiuong && t == 3) d -= 1.5;
                if (loai == "AN" && t == 1) d += 0.5;
            }
            return Math.Round(Math.Clamp(d, 0, 10), 2);
        }

        // --- Trong so theo ho so khach (nguoi dung chinh thi dung cua nguoi dung) ---------
        public static double[] TrongSo(YeuCauTuVanVe yc)
        {
            if (yc.TrongSo is { Length: 4 } tay && tay.All(x => x >= 0) && tay.Sum() > 0)
                return ChuanHoa(tay);

            var w = (double[])TrongSoMacDinh.Clone();
            w[TieuChiNganSach] = yc.MucNganSach switch
            {
                MucNganSach.TietKiem => 0.45,
                MucNganSach.CaoCap => 0.10,
                _ => 0.25
            };
            if (yc.LaSinhVien) w[TieuChiNganSach] += 0.10;
            if (yc.CoNguoiCaoTuoi || yc.CoTreEm) w[TieuChiSucKhoe] += 0.15;
            if (yc.KhungGio != KhungGioUuTien.KhongUuTien) w[TieuChiKhungGio] += 0.10;
            return ChuanHoa(w);
        }

        public static double[] ChuanHoa(double[] w)
        {
            double tong = w.Sum();
            if (tong <= 0) return (double[])TrongSoMacDinh.Clone();
            return w.Select(x => x / tong).ToArray();
        }

        public static double TinhDiem(double[] c, double[] w)
        {
            if (c.Length != w.Length) throw new ArgumentException("Số tiêu chí và số trọng số phải bằng nhau.");
            var wn = ChuanHoa(w);
            return Math.Round(c.Zip(wn, (a, b) => a * b).Sum(), 3);
        }

        // --- Thoi gian ----------------------------------------------------------
        // So gio cua hanh trinh nam trong khung 22:00-06:00 (cong don qua nhieu dem).
        public static double SoGioBanDem(DateTime di, DateTime den)
        {
            if (den <= di) return 0;
            double tong = 0;
            for (var ngay = di.Date.AddDays(-1); ngay <= den.Date; ngay = ngay.AddDays(1))
            {
                var bd = ngay.AddHours(22);
                var kt = ngay.AddDays(1).AddHours(6);
                var a = di > bd ? di : bd;
                var b = den < kt ? den : kt;
                if (b > a) tong += (b - a).TotalHours;
            }
            return tong;
        }

        public static bool LaTauDem(DateTime di, DateTime den) => SoGioBanDem(di, den) >= SoGioDemToiThieu;

        public static bool DenGiuaDem(DateTime gioDen) => gioDen.Hour >= 23 || gioDen.Hour < 5;

        // --- Chon ghe cu the: uu tien giua toa, cung tang; du so nguoi --------------
        // cacGheTrong: (MaChoNgoi, SoGhe). Tra ve null neu khong du.
        public static List<(int MaChoNgoi, int SoGhe)>? ChonGhe(IReadOnlyList<(int MaChoNgoi, int SoGhe)> cacGheTrong, int soNguoi, int soGheLonNhatCuaToa)
        {
            if (soNguoi < 1 || cacGheTrong.Count < soNguoi) return null;
            double giua = (soGheLonNhatCuaToa + 1) / 2.0;
            var xep = cacGheTrong.OrderBy(g => g.SoGhe).ToList();
            // Tim cua so soNguoi ghe lien nhau theo so ghe (cung khoang / cung day), gan giua toa nhat
            List<(int, int)>? tot = null;
            double diemTot = double.MaxValue;
            for (int i = 0; i + soNguoi <= xep.Count; i++)
            {
                var cuaSo = xep.Skip(i).Take(soNguoi).ToList();
                int nhay = cuaSo[^1].SoGhe - cuaSo[0].SoGhe - (soNguoi - 1);       // so ghe bi chen giua
                double d = nhay * 10 + Math.Abs(cuaSo.Average(g => g.SoGhe) - giua);
                if (d < diemTot) { diemTot = d; tot = cuaSo; }
            }
            return tot;
        }
    }
}
