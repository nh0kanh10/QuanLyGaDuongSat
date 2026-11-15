using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using BUS.Services;
using DAL.Repositories;
using DTO.AI;

namespace BUS.AI
{
    // =========================================================================
    // BO CONG CU CUA TRO CHUYEN AI (16/11): mo hinh chi chon TEN cong cu + tham so; o day chay cau truy van viet san,
    // CHI DOC (dat_ve xu ly rieng trong TroLyChatService vi can cua so xac nhan). Ket qua tra mo hinh la JSON gon.
    // Tim chuyen / toa / gia / tu van dung lai VeService (A Ba) + TuVanVeService, khong viet lai SQL ban ve.
    // =========================================================================
    public sealed class KetQuaCongCu
    {
        public string ChoMoHinh { get; init; } = "";      // noi dung gui lai mo hinh
        public string MoTa { get; init; } = "";           // dong hien cho nhan vien: "Tìm chuyến Hà Nội → Đà Nẵng 20/11: 3 chuyến"
        public bool Loi { get; init; }
        public KetQuaTuVanVe? TuVan { get; init; }
        public YeuCauTuVanVe? YeuCau { get; init; }

        public static KetQuaCongCu BaoLoi(string loi) => new() { ChoMoHinh = "LỖI: " + loi, MoTa = "Tra cứu không thành công: " + loi, Loi = true };
    }

    public interface IBoCongCuTroLy
    {
        IReadOnlyList<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)> DanhSachGa();
        IReadOnlyDictionary<string, decimal> TyLeGiamGia();
        KetQuaCongCu ThucThi(string ten, JsonElement thamSo, DateTime bayGio);
    }

    public sealed class BoCongCuTroLy : IBoCongCuTroLy
    {
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
        private readonly TroLyAIRepository _repo = new();
        private readonly VeService _ve = new();
        private readonly TuVanVeService _tuVan = new();
        private List<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)>? _ga;

        public IReadOnlyList<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)> DanhSachGa() => _ga ??= _repo.LayDanhSachGa();

        public IReadOnlyDictionary<string, decimal> TyLeGiamGia()
            => new[] { "TRE_EM", "NGUOI_GIA", "SINH_VIEN" }.ToDictionary(x => x, VeService.LayTyLeGiamGia);

        public KetQuaCongCu ThucThi(string ten, JsonElement ts, DateTime bayGio)
        {
            try
            {
                return ten switch
                {
                    QuyTacMoHinhNgonNgu.CongCu.DsMacTau => DsMacTau(),
                    QuyTacMoHinhNgonNgu.CongCu.TimChuyenTau => TimChuyenTau(ts, bayGio),
                    QuyTacMoHinhNgonNgu.CongCu.LichTrinhChuyen => LichTrinh(ts),
                    QuyTacMoHinhNgonNgu.CongCu.ChoTrongVaGia => ChoTrongVaGia(ts),
                    QuyTacMoHinhNgonNgu.CongCu.TuVanPhuongAn => TuVan(ts, bayGio),
                    _ => KetQuaCongCu.BaoLoi($"Không có công cụ \"{ten}\".")
                };
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                return KetQuaCongCu.BaoLoi("Không đọc được dữ liệu: " + DichLoiSqlPhuongTien.ThongDiep(ex));
            }
        }

        private static string Json(object o) => JsonSerializer.Serialize(o, QuyTacMoHinhNgonNgu.JsonGon);

        private KetQuaCongCu DsMacTau()
        {
            var ds = _repo.LayDanhSachMacTau();
            return new KetQuaCongCu
            {
                ChoMoHinh = Json(ds.Select(m => new
                {
                    mac_tau = m.SoHieu,
                    loai = QuyTacMoHinhNgonNgu.TenLoaiTau(m.LoaiTau),
                    tuyen = $"{m.TenGaDi} → {m.TenGaDen}",
                    thoi_gian_chuan_gio = m.ThoiGianChuanGio.ToString("0.#", Vi),
                })),
                MoTa = $"Danh sách mác tàu: {ds.Count} mác",
            };
        }

        private KetQuaCongCu TimChuyenTau(JsonElement ts, DateTime bayGio)
        {
            var dsGa = DanhSachGa();
            if (!QuyTacMoHinhNgonNgu.DocGa(ts, "ga_di", dsGa, out var di, out string loi)) return KetQuaCongCu.BaoLoi(loi);
            if (!QuyTacMoHinhNgonNgu.DocGa(ts, "ga_den", dsGa, out var den, out loi)) return KetQuaCongCu.BaoLoi(loi);
            if (di.MaGa == den.MaGa) return KetQuaCongCu.BaoLoi("Ga đi và ga đến trùng nhau.");
            if (!QuyTacMoHinhNgonNgu.DocNgay(ts, bayGio, out var ngay, out loi)) return KetQuaCongCu.BaoLoi(loi);

            var dong = _ve.TimChuyenTauTheoChang(di.MaGa, den.MaGa, ngay).Rows.Cast<DataRow>()
                .Where(r => (r["LoaiTau"]?.ToString() ?? "") != "TAU_HANG")
                .OrderBy(r => Convert.ToDateTime(r["GioXuatPhatKH"])).ToList();
            string chang = $"{di.TenGa} → {den.TenGa} ngày {ngay:dd/MM/yyyy}";
            if (dong.Count == 0)
                return new KetQuaCongCu
                {
                    ChoMoHinh = $"Không có chuyến tàu khách nào còn bán vé dừng ở cả {di.TenGa} và {den.TenGa} ngày {ngay:dd/MM/yyyy}.",
                    MoTa = $"Tìm chuyến {chang}: không có chuyến",
                };
            return new KetQuaCongCu
            {
                ChoMoHinh = Json(new
                {
                    chang,
                    cac_chuyen = dong.Select(r =>
                    {
                        var gioDi = Convert.ToDateTime(r["GioXuatPhatKH"]);
                        var gioDen = Convert.ToDateTime(r["GioVeDichKH"]);
                        int choTrong = Math.Max(0, SoNguyen(r["TongSoCho"]) - SoNguyen(r["SoChoDaBan"]));
                        return new
                        {
                            ma_chuyen_tau = SoNguyen(r["MaChuyenTau"]),
                            mac_tau = r["SoHieuMacTau"]?.ToString(),
                            loai = QuyTacMoHinhNgonNgu.TenLoaiTau(r["LoaiTau"]?.ToString() ?? ""),
                            di = $"{gioDi:HH:mm} ngày {gioDi:dd/MM}",
                            den = $"{gioDen:HH:mm} ngày {gioDen:dd/MM}",
                            so_gio = Math.Max(0, (gioDen - gioDi).TotalHours).ToString("0.#", Vi),
                            cho_trong = choTrong,
                            gia_ghe_mem = r["GiaVeCoSo"] == DBNull.Value ? null : Convert.ToDecimal(r["GiaVeCoSo"]).ToString("N0", Vi) + " đ",
                        };
                    }),
                }),
                MoTa = $"Tìm chuyến {di.TenGa} → {den.TenGa} ngày {ngay:dd/MM}: {dong.Count} chuyến",
            };
        }

        private KetQuaCongCu LichTrinh(JsonElement ts)
        {
            int ma = (int)QuyTacMoHinhNgonNgu.So(ts, "ma_chuyen_tau");
            var tt = _repo.LayThongTinChuyen(ma);
            if (tt == null) return KetQuaCongCu.BaoLoi($"Không có chuyến mã {ma}.");
            var ga = _repo.LayLichDung(ma);
            if (ga.Count == 0)
                return new KetQuaCongCu { ChoMoHinh = $"Chuyến {tt.Value.SoHieuMacTau} ngày {tt.Value.NgayXuatPhat:dd/MM/yyyy} chưa có lịch dừng ga.",
                                          MoTa = $"Lịch trình {tt.Value.SoHieuMacTau}: chưa có lịch dừng" };
            return new KetQuaCongCu
            {
                ChoMoHinh = Json(new
                {
                    mac_tau = tt.Value.SoHieuMacTau,
                    ngay_xuat_phat = tt.Value.NgayXuatPhat.ToString("dd/MM/yyyy"),
                    cac_ga = ga.Select(g => new
                    {
                        thu_tu = g.ThuTu,
                        ga = g.TenGa,
                        den = $"{g.GioDen:HH:mm} ngày {g.GioDen:dd/MM}",
                        di = $"{g.GioDi:HH:mm} ngày {g.GioDi:dd/MM}",
                        dung_phut = (int)Math.Round((g.GioDi - g.GioDen).TotalMinutes),
                    }),
                }),
                MoTa = $"Lịch trình {tt.Value.SoHieuMacTau} ngày {tt.Value.NgayXuatPhat:dd/MM}: {ga.Count} ga dừng",
            };
        }

        private KetQuaCongCu ChoTrongVaGia(JsonElement ts)
        {
            var dsGa = DanhSachGa();
            int ma = (int)QuyTacMoHinhNgonNgu.So(ts, "ma_chuyen_tau");
            if (!QuyTacMoHinhNgonNgu.DocGa(ts, "ga_di", dsGa, out var di, out string loi)) return KetQuaCongCu.BaoLoi(loi);
            if (!QuyTacMoHinhNgonNgu.DocGa(ts, "ga_den", dsGa, out var den, out loi)) return KetQuaCongCu.BaoLoi(loi);
            var tt = _repo.LayThongTinChuyen(ma);
            if (tt == null) return KetQuaCongCu.BaoLoi($"Không có chuyến mã {ma}.");

            var toa = _ve.LayDanhSachToaTheoChuyen(ma, di.MaGa, den.MaGa).Rows.Cast<DataRow>().ToList();
            if (toa.Count == 0)
                return new KetQuaCongCu { ChoMoHinh = $"Chuyến {tt.Value.SoHieuMacTau} không có toa bán vé chạy qua chặng {di.TenGa} → {den.TenGa}.",
                                          MoTa = $"Chỗ trống & giá {tt.Value.SoHieuMacTau}: không có toa bán vé" };
            var loai = toa.GroupBy(r => r["LoaiToa"]?.ToString() ?? "").Select(g =>
            {
                int?[] cacTang = g.Key switch { "BN" => new int?[] { 1, 2, 3 }, "AN" => new int?[] { 1, 2 }, _ => new int?[] { null } };
                return new
                {
                    loai_cho = g.First()["TenLoaiToa"]?.ToString() ?? g.Key,
                    so_toa = g.Count(),
                    cho_trong = g.Sum(r => SoNguyen(r["SoChoTrong"])),
                    tong_cho = g.Sum(r => SoNguyen(r["TongSoCho"])),
                    gia = cacTang.Select(t => new
                    {
                        tang = t,
                        gia = _ve.TinhGiaVe(g.Key, di.MaGa, den.MaGa, t, tt.Value.LoaiTau).ToString("N0", Vi) + " đ",
                    }),
                };
            }).ToList();
            return new KetQuaCongCu
            {
                ChoMoHinh = Json(new
                {
                    mac_tau = tt.Value.SoHieuMacTau,
                    chang = $"{di.TenGa} → {den.TenGa}",
                    ghi_chu = "Giá gốc một vé, chưa trừ giảm giá theo đối tượng.",
                    cac_loai_cho = loai,
                }),
                MoTa = $"Chỗ trống & giá {tt.Value.SoHieuMacTau} {di.TenGa} → {den.TenGa}: {loai.Count} loại chỗ",
            };
        }

        private KetQuaCongCu TuVan(JsonElement ts, DateTime bayGio)
        {
            if (!QuyTacMoHinhNgonNgu.DocYeuCauTuVan(ts, DanhSachGa(), bayGio, out var yc, out string loi)) return KetQuaCongCu.BaoLoi(loi);
            if (!_tuVan.TuVan(yc, out var kq, out loi)) return KetQuaCongCu.BaoLoi(loi);
            return new KetQuaCongCu
            {
                ChoMoHinh = kq.CacPhuongAn.Count == 0
                    ? "Không có phương án: " + string.Join(" ", kq.GhiChu)
                    : QuyTacMoHinhNgonNgu.KetQuaTuVanChoMoHinh(yc, kq),
                MoTa = $"Tư vấn {kq.TenGaDi} → {kq.TenGaDen} ngày {yc.NgayDi:dd/MM}, {yc.SoNguoi} khách: {kq.CacPhuongAn.Count} phương án " +
                       $"(đã xét {kq.SoPhuongAnDaXet} trên {kq.SoChuyenDaXet} chuyến)",
                TuVan = kq,
                YeuCau = yc,
            };
        }

        private static int SoNguyen(object? o) => o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
    }
}
