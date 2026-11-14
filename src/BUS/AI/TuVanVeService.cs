using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using BUS.Services;
using DAL.Repositories;
using DTO.AI;
using DTO.Common;

namespace BUS.AI
{

    // AI TU VAN VE (Sprint 3, Dat - FR-8.1, UC-05). Chay offline, khong can mang / khoa API.
    //   1. Ung vien: chuyen con cho tren chang (VeService.TimChuyenTauTheoChang cua A Ba) x loai cho x tang giuong,
    //      con du cho cho ca doan tren CHANG cua khach (so do ghe theo chang cua A Ba).
    //   2. Cham 4 tieu chi (QuyTacTuVanVe) -> Score = Σ w·c; loai phuong an vuot ngan sach toi da
    //      (con thi van giu de bao "khong co phuong an trong ngan sach").
    //   3. Chon 3 phuong an tot nhat, khong trung (chuyen, loai cho); sinh ly do theo mau.
    //   4. Ghi kiemtoan.NhatKyChatAI (nguoi hoi = tai khoan dang dang nhap).
    // Chi DOC du lieu ban ve, khong giu cho / khong ghi vantai.* (§7.4).

    public class TuVanVeService
    {
        public const int SoPhuongAnToiDa = 3;
        public const int SoNguoiToiDa = 6;

        private readonly VeService _ve = new();
        private readonly TroLyAIRepository _repo = new();
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        public bool LayDanhSachGa(out List<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)> ds, out string loi)
        {
            loi = "";
            try { ds = _repo.LayDanhSachGa(); return true; }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                ds = new();
                loi = "Không đọc được danh sách ga: " + DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        public bool TuVan(YeuCauTuVanVe yc, out KetQuaTuVanVe kq, out string loi)
        {
            kq = new KetQuaTuVanVe();
            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.DungTroLyAI, "dùng trợ lý AI", out loi)) return false;
            if (yc.MaGaDi <= 0 || yc.MaGaDen <= 0) { loi = "Chọn ga đi và ga đến."; return false; }
            if (yc.MaGaDi == yc.MaGaDen) { loi = "Ga đi và ga đến phải khác nhau."; return false; }
            if (yc.SoNguoi < 1 || yc.SoNguoi > SoNguoiToiDa) { loi = $"Số hành khách từ 1 đến {SoNguoiToiDa}."; return false; }
            if (yc.NgayDi.Date < DateTime.Today) { loi = "Ngày đi không được ở quá khứ."; return false; }
            if (yc.NganSachToiDaMoiNguoi is <= 0) { loi = "Ngân sách tối đa phải lớn hơn 0 (bỏ trống nếu không giới hạn)."; return false; }

            try
            {
                var ga = _repo.LayDanhSachGa();
                var gaDi = ga.FirstOrDefault(g => g.MaGa == yc.MaGaDi);
                var gaDen = ga.FirstOrDefault(g => g.MaGa == yc.MaGaDen);
                if (gaDi.MaGa == 0 || gaDen.MaGa == 0) { loi = "Ga không còn khai thác. Hãy nạp lại danh sách ga."; return false; }

                kq.TenGaDi = gaDi.TenGa;
                kq.TenGaDen = gaDen.TenGa;
                kq.CuLyKm = Math.Abs(gaDen.LyTrinhKm - gaDi.LyTrinhKm);
                kq.TrongSoDaDung = QuyTacTuVanVe.TrongSo(yc);

                var ungVien = TimUngVien(yc, kq);
                kq.SoPhuongAnDaXet = ungVien.Count;
                if (yc.MangXeMay)
                    kq.GhiChu.Add("Xe máy đi kèm gửi theo vận đơn hàng hóa (phân hệ Hàng hóa) — ghi chú, không dùng để lọc chuyến.");

                if (ungVien.Count == 0)
                {
                    kq.GhiChu.Add(kq.SoChuyenDaXet == 0
                        ? $"Không có chuyến nào dừng ở cả {gaDi.TenGa} và {gaDen.TenGa} ngày {yc.NgayDi:dd/MM/yyyy}."
                        : $"Các chuyến ngày {yc.NgayDi:dd/MM/yyyy} không còn đủ {yc.SoNguoi} chỗ cùng loại trên chặng này.");
                    kq.CauTraLoi = string.Join(" ", kq.GhiChu);
                    GhiNhatKy(yc, kq);
                    return true;
                }

                ChamDiem(yc, kq, ungVien);
                kq.CacPhuongAn = ChonTop(ungVien);
                for (int i = 0; i < kq.CacPhuongAn.Count; i++) kq.CacPhuongAn[i].XepHang = i + 1;
                foreach (var pa in kq.CacPhuongAn) pa.LyDo = SinhLyDo(yc, kq, pa);

                if (kq.CacPhuongAn.All(p => p.VuotNganSach) && yc.NganSachToiDaMoiNguoi.HasValue)
                    kq.GhiChu.Insert(0, $"Không có phương án nào trong ngân sách {yc.NganSachToiDaMoiNguoi.Value.ToString("N0", Vi)} đ/người — " +
                                        "dưới đây là các phương án gần nhất (rẻ nhất vẫn vượt).");
                else if (kq.CacPhuongAn.Count > 0 && kq.CacPhuongAn[0].VuotNganSach)
                    kq.GhiChu.Insert(0, "Phương án điểm cao nhất vượt ngân sách — cân nhắc phương án trong ngân sách bên cạnh.");

                kq.CauTraLoi = TomTat(yc, kq);
                GhiNhatKy(yc, kq);
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                loi = "Không đọc được dữ liệu chuyến tàu: " + DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        // =====================================================================
        private List<PhuongAnVe> TimUngVien(YeuCauTuVanVe yc, KetQuaTuVanVe kq)
        {
            var ds = new List<PhuongAnVe>();
            var chuyen = _ve.TimChuyenTauTheoChang(yc.MaGaDi, yc.MaGaDen, yc.NgayDi).Rows.Cast<DataRow>()
                .Where(r => (r["LoaiTau"]?.ToString() ?? "") != "TAU_HANG").ToList();
            kq.SoChuyenDaXet = chuyen.Count;
            var giaCache = new Dictionary<(string, string, int?), decimal>();

            foreach (var ct in chuyen)
            {
                int maCt = Convert.ToInt32(ct["MaChuyenTau"]);
                string loaiTau = ct["LoaiTau"]?.ToString() ?? "TAU_NHANH";
                var gio = _repo.LayGioTaiGa(maCt, yc.MaGaDi, yc.MaGaDen)
                          ?? (Convert.ToDateTime(ct["GioXuatPhatKH"]), Convert.ToDateTime(ct["GioVeDichKH"]));
                if (gio.GioDi < DateTime.Now) continue;           // tau da qua ga di cua khach

                var cacToa = _ve.LayDanhSachToaTheoChuyen(maCt, yc.MaGaDi, yc.MaGaDen).Rows.Cast<DataRow>()
                    .Where(r => r["SoChoTrong"] != DBNull.Value && Convert.ToInt32(r["SoChoTrong"]) >= 1).ToList();

                // Gom ghe trong theo (loai, tang) tren moi toa
                var theoNhom = new Dictionary<(string Loai, int? Tang), List<(int MaToa, string Nhan, List<(int MaCho, int SoGhe)> Trong, int SoGheMax)>>();
                foreach (var toa in cacToa)
                {
                    int maToa = Convert.ToInt32(toa["MaToaXeKhach"]);
                    string loai = toa["LoaiToa"]?.ToString() ?? "";
                    string nhan = toa["NhanHieuToa"]?.ToString() ?? "";
                    var soDo = _ve.LaySoDoGhe(maToa, maCt, yc.MaGaDi, yc.MaGaDen).Rows.Cast<DataRow>()
                        .GroupBy(r => Convert.ToInt32(r["MaChoNgoi"]))                // mot ghe co the ra nhieu dong
                        .Select(g => g.OrderByDescending(r => Convert.ToInt32(r["DaDat"])).First()).ToList();
                    if (soDo.Count == 0) continue;
                    int soGheMax = soDo.Max(r => Convert.ToInt32(r["SoGhe"]));
                    foreach (var g in soDo.Where(r => Convert.ToInt32(r["DaDat"]) == 0 && !Convert.ToBoolean(r["LaGhePhu"]))
                                           .GroupBy(r => r["TangGiuong"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["TangGiuong"])))
                    {
                        var key = (loai, g.Key);
                        if (!theoNhom.TryGetValue(key, out var lst)) theoNhom[key] = lst = new();
                        lst.Add((maToa, nhan, g.Select(r => (Convert.ToInt32(r["MaChoNgoi"]), Convert.ToInt32(r["SoGhe"]))).ToList(), soGheMax));
                    }
                }

                foreach (var (key, toaTrong) in theoNhom)
                {
                    int tongTrong = toaTrong.Sum(t => t.Trong.Count);
                    // Toa co nhieu cho trong nhat va du cho ca doan
                    var chon = toaTrong.Where(t => t.Trong.Count >= yc.SoNguoi).OrderByDescending(t => t.Trong.Count).FirstOrDefault();
                    if (chon.Trong == null) continue;
                    var ghe = QuyTacTuVanVe.ChonGhe(chon.Trong, yc.SoNguoi, chon.SoGheMax);
                    if (ghe == null) continue;

                    var khoaGia = (loaiTau, key.Loai, key.Tang);
                    if (!giaCache.TryGetValue(khoaGia, out decimal gia))
                        giaCache[khoaGia] = gia = _ve.TinhGiaVe(key.Loai, yc.MaGaDi, yc.MaGaDen, key.Tang, loaiTau);

                    ds.Add(new PhuongAnVe
                    {
                        MaChuyenTau = maCt,
                        SoHieuMacTau = ct["SoHieuMacTau"]?.ToString() ?? "",
                        LoaiTau = loaiTau,
                        MaToaXeKhach = chon.MaToa,
                        NhanHieuToa = chon.Nhan,
                        LoaiToa = key.Loai,
                        Tang = key.Tang,
                        CacMaChoNgoi = ghe.Select(g => g.MaChoNgoi).ToList(),
                        CacSoGhe = ghe.Select(g => g.SoGhe).ToList(),
                        SoChoTrongCungLoai = tongTrong,
                        GiaMotVe = gia,
                        GioDi = gio.GioDi,
                        GioDen = gio.GioDen,
                        CuLyKm = kq.CuLyKm,
                        QuaDem = QuyTacTuVanVe.LaTauDem(gio.GioDi, gio.GioDen)
                    });
                }
            }
            return ds;
        }

        private static void ChamDiem(YeuCauTuVanVe yc, KetQuaTuVanVe kq, List<PhuongAnVe> ds)
        {
            // [12/11] Gia sau giam theo loai khach du kien (cung ty le giam va cach lam tron voi gio ve - VeService)
            var loaiKhach = QuyTacTuVanVe.LoaiKhachDuKien(yc.SoNguoi, yc.CoNguoiCaoTuoi, yc.CoTreEm, yc.LaSinhVien);
            foreach (var p in ds)
            {
                p.CacLoaiKhach = loaiKhach.Take(p.CacMaChoNgoi.Count).ToList();
                p.CacGiaSauGiam = p.CacLoaiKhach
                    .Select(l => p.GiaMotVe - Math.Round(p.GiaMotVe * VeService.LayTyLeGiamGia(l), 0)).ToList();
            }

            decimal giaReNhat = ds.Where(p => p.GiaCaoNhatSauGiam > 0).Select(p => p.GiaCaoNhatSauGiam).DefaultIfEmpty(0).Min();
            foreach (var p in ds)
            {
                // Ngan sach "toi da moi ve": ve dat nhat cua doan sau giam gia khong duoc vuot
                p.VuotNganSach = yc.NganSachToiDaMoiNguoi.HasValue && p.GiaCaoNhatSauGiam > yc.NganSachToiDaMoiNguoi.Value;
                p.DiemTieuChi = new[]
                {
                    QuyTacTuVanVe.DiemTienNghi(p.LoaiToa, p.Tang, p.CuLyKm),
                    QuyTacTuVanVe.DiemNganSach(p.GiaCaoNhatSauGiam, giaReNhat, yc.NganSachToiDaMoiNguoi),
                    QuyTacTuVanVe.DiemKhungGio(p.LoaiToa, p.Tang, p.QuaDem, yc.KhungGio, p.GioDen),
                    QuyTacTuVanVe.DiemSucKhoe(p.LoaiToa, p.Tang, p.CuLyKm, yc.CoNguoiCaoTuoi, yc.CoTreEm)
                };
                p.Diem = QuyTacTuVanVe.TinhDiem(p.DiemTieuChi, kq.TrongSoDaDung);
            }
        }

        // Diem cao truoc; trong ngan sach truoc vuot; khong trung (chuyen, loai cho); hoa thi re hon, di som hon
        private static List<PhuongAnVe> ChonTop(List<PhuongAnVe> ds)
        {
            var kq = new List<PhuongAnVe>();
            // Moi phuong an deu vuot ngan sach: ngan sach la rang buoc cung -> uu tien phuong an gan ngan sach nhat (re nhat)
            var thuTu = ds.All(p => p.VuotNganSach)
                ? ds.OrderBy(p => p.GiaMotVe).ThenByDescending(p => p.Diem).ThenBy(p => p.GioDi)
                : ds.OrderBy(p => p.VuotNganSach).ThenByDescending(p => p.Diem).ThenBy(p => p.GiaMotVe).ThenBy(p => p.GioDi);
            foreach (var p in thuTu)
            {
                if (kq.Any(x => x.MaChuyenTau == p.MaChuyenTau && x.LoaiToa == p.LoaiToa)) continue;
                kq.Add(p);
                if (kq.Count == SoPhuongAnToiDa) break;
            }
            return kq;
        }

        // =====================================================================
        // CAU TU VAN THEO MAU
        // =====================================================================
        internal static List<string> SinhLyDo(YeuCauTuVanVe yc, KetQuaTuVanVe kq, PhuongAnVe p)
        {
            var ly = new List<string>();
            bool giuong = p.LoaiToa is "AN" or "BN";
            var nhom = QuyTacTuVanVe.XepNhomCuLy(p.CuLyKm);
            string km = p.CuLyKm.ToString("N0", Vi);

            if (nhom == QuyTacTuVanVe.NhomCuLy.Dai)
                ly.Add(giuong ? $"Chặng dài {km} km, khoảng {p.SoGioChay:0.#} giờ — giường nằm giúp nghỉ ngơi, đỡ mệt."
                              : $"Ngồi khoảng {p.SoGioChay:0.#} giờ trên chặng dài {km} km sẽ mệt — chỉ hợp nếu ưu tiên tiết kiệm.");
            else if (nhom == QuyTacTuVanVe.NhomCuLy.Ngan)
                ly.Add(giuong ? $"Chặng ngắn {km} km — giường nằm thoải mái nhưng không thật cần thiết."
                              : $"Chặng ngắn {km} km (khoảng {p.SoGioChay:0.#} giờ) — ghế ngồi là đủ.");
            else
                ly.Add($"Chặng trung bình {km} km, khoảng {p.SoGioChay:0.#} giờ.");

            if (p.QuaDem)
                ly.Add(yc.KhungGio == KhungGioUuTien.TranhDiDem
                    ? $"Tàu chạy qua đêm ({p.GioDi:HH:mm} → {p.GioDen:HH:mm}) — chưa đúng mong muốn đi ban ngày."
                    : giuong ? $"Tàu đêm ({p.GioDi:HH:mm} → {p.GioDen:HH:mm} ngày {p.GioDen:dd/MM}): ngủ trọn giấc, tiết kiệm một đêm khách sạn."
                             : $"Tàu chạy qua đêm ({p.GioDi:HH:mm} → {p.GioDen:HH:mm}) mà ngồi ghế — khó ngủ.");
            else
                ly.Add($"Đi ban ngày: khởi hành {p.GioDi:HH:mm}, đến {kq.TenGaDen} lúc {p.GioDen:HH:mm}.");
            if (QuyTacTuVanVe.DenGiuaDem(p.GioDen))
                ly.Add($"Đến ga lúc {p.GioDen:HH:mm} (giữa đêm) — cần chuẩn bị phương tiện về.");

            if (yc.CoNguoiCaoTuoi && giuong)
                ly.Add(p.Tang == 1 ? "Giường tầng 1, không phải leo thang — phù hợp người cao tuổi."
                                   : $"Giường tầng {p.Tang}: người cao tuổi phải leo thang.");
            if (yc.CoTreEm && p.LoaiToa == "AN")
                ly.Add("Khoang 4 giường riêng tư, an toàn cho trẻ nhỏ.");

            if (p.VuotNganSach)
                ly.Add($"Vượt ngân sách {(p.GiaCaoNhatSauGiam - yc.NganSachToiDaMoiNguoi!.Value).ToString("N0", Vi)} đ/người" +
                       (p.CoGiamGia ? " (đã tính giảm giá theo đối tượng)." : "."));
            else if (p.XepHang > 1 && kq.CacPhuongAn.Count > 0 && kq.CacPhuongAn[0].GiaMotVe > 0)
            {
                decimal chenh = (p.GiaMotVe - kq.CacPhuongAn[0].GiaMotVe) / kq.CacPhuongAn[0].GiaMotVe * 100m;
                if (Math.Abs(chenh) >= 3)
                    ly.Add(chenh < 0 ? $"Rẻ hơn phương án 1 khoảng {-chenh:0}%." : $"Đắt hơn phương án 1 khoảng {chenh:0}%.");
            }

            int toiDa = Enumerable.Range(0, 4).OrderByDescending(i => p.DiemTieuChi[i] * kq.TrongSoDaDung[i]).First();
            ly.Add($"Điểm mạnh nhất: {QuyTacTuVanVe.TenTieuChi[toiDa].ToLower(Vi)} ({p.DiemTieuChi[toiDa]:0.0}/10). " +
                   $"Còn {p.SoChoTrongCungLoai} chỗ cùng loại trên chặng.");
            return ly;
        }

        private static string MoTaYeuCau(YeuCauTuVanVe yc, KetQuaTuVanVe kq)
        {
            var sb = new StringBuilder($"Tư vấn vé {kq.TenGaDi} → {kq.TenGaDen} ngày {yc.NgayDi:dd/MM/yyyy}, {yc.SoNguoi} khách");
            if (yc.CoNguoiCaoTuoi) sb.Append(", có người cao tuổi");
            if (yc.CoTreEm) sb.Append(", có trẻ em");
            if (yc.LaSinhVien) sb.Append(", sinh viên");
            sb.Append($" (dự kiến {QuyTacTuVanVe.MoTaLoaiKhach(QuyTacTuVanVe.LoaiKhachDuKien(yc.SoNguoi, yc.CoNguoiCaoTuoi, yc.CoTreEm, yc.LaSinhVien))})");
            sb.Append($"; ngân sách {yc.MucNganSach}");
            if (yc.NganSachToiDaMoiNguoi.HasValue) sb.Append($" (tối đa {yc.NganSachToiDaMoiNguoi.Value.ToString("N0", Vi)} đ/người)");
            sb.Append($"; khung giờ {yc.KhungGio}");
            if (yc.MangXeMay) sb.Append("; mang xe máy");
            sb.Append($"; trọng số {string.Join("/", kq.TrongSoDaDung.Select(w => w.ToString("0.00", CultureInfo.InvariantCulture)))}");
            return sb.ToString();
        }

        private static string TomTat(YeuCauTuVanVe yc, KetQuaTuVanVe kq)
        {
            var sb = new StringBuilder();
            foreach (var p in kq.CacPhuongAn)
                sb.AppendLine($"{p.XepHang}) {p.SoHieuMacTau} {p.GioDi:dd/MM HH:mm} · {p.TenLoaiCho} · {p.NhanGhe} · " +
                              $"{p.GiaMotVe.ToString("N0", Vi)} đ/vé" +
                              (p.CoGiamGia ? $" (cả đoàn sau giảm {p.TongTien.ToString("N0", Vi)} đ)" : "") + $" · {p.Diem:0.000} điểm");
            foreach (var g in kq.GhiChu) sb.AppendLine(g);
            sb.Append($"(Đã xét {kq.SoPhuongAnDaXet} phương án trên {kq.SoChuyenDaXet} chuyến.)");
            return sb.ToString();
        }

        private void GhiNhatKy(YeuCauTuVanVe yc, KetQuaTuVanVe kq)
        {
            try
            {
                _repo.GhiNhatKyChat(PhanQuyenService.MaTaiKhoanHienTai, MoTaYeuCau(yc, kq), kq.CauTraLoi);
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                kq.GhiChu.Add("(Không ghi được nhật ký hỏi – đáp AI: " + DichLoiSqlPhuongTien.ThongDiep(ex) + ")");
            }
        }
    }
}
