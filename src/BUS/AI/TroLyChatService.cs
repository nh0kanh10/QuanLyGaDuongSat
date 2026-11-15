using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BUS.Services;
using DAL.Repositories;
using DTO.AI;
using DTO.Common;

namespace BUS.AI
{
    // =========================================================================
    // TRO CHUYEN VOI TRO LY AI (16/11, Dat chot): hoi tu do, nho hoi thoai trong cua so, mo hinh tu goi cong cu.
    //   Moi cau hoi: lan goi 1 gui lich su + 6 cong cu. Mo hinh tra loi ngay (cau hoi thuong -> 1 lan goi) HOAC
    //   tra ve lenh goi cong cu -> chay truy van that (BoCongCuTroLy) -> lan goi 2 kem ket qua -> tra loi.
    //   Toi da SoVongToiDa lan goi / cau hoi (vd tim chuyen roi xem cho trong = 3 lan).
    //   dat_ve: mo cua so xac nhan (XacNhanDatVe do GUI cap) -> nhan vien nhap ten / CCCD / SDT, xac nhan -> moi ghi CSDL.
    // Gui ra ngoai: cau hoi (da che day so >= 9 chu so), ket qua cong cu (khong co ten / CCCD / SDT hanh khach).
    // Moi cau hoi thanh cong ghi kiemtoan.NhatKyChatAI (CauSQL = cac cong cu da goi + tham so).
    // =========================================================================
    public sealed class TroLyChatService
    {
        public const int SoVongToiDa = 4;
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        private readonly IKenhMoHinhNgonNgu? _kenh;
        private readonly IBoCongCuTroLy _congCu;
        private readonly Action<string, string, string?> _ghiNhatKy;
        private readonly Func<DateTime> _bayGio;

        private readonly List<TinNhanChat> _lichSu = new();
        private readonly HashSet<int> _daDat = new();
        private KetQuaTuVanVe? _tuVan;
        private YeuCauTuVanVe? _ycTuVan;
        private IReadOnlyList<CongCuMoHinh>? _dsCongCu;
        private IReadOnlyList<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)>? _dsGa;
        private IReadOnlyDictionary<string, decimal>? _tyLeGiam;

        // GUI cap: mo cua so xac nhan dat ve, tra ket qua (da dat / nhan vien huy / loi).
        public Func<YeuCauDatVeHo, Task<KetQuaDatVeHo>>? XacNhanDatVe { get; set; }

        public TroLyChatService()
        {
            var repo = new TroLyAIRepository();
            _congCu = new BoCongCuTroLy();
            _ghiNhatKy = (hoi, traLoi, sql) => repo.GhiNhatKyChat(PhanQuyenService.MaTaiKhoanHienTai, hoi, traLoi, sql);
            _bayGio = () => DateTime.Now;

            if (!CauHinhUngDung.BatMoHinhNgonNgu)
                LyDoChuaSanSang = "Phần trò chuyện AI đang tắt (\"BatMoHinhNgonNgu\": false trong appsettings).";
            else if (!CauHinhUngDung.CoKhoaApiAI)
                LyDoChuaSanSang = "Chưa có khóa API OpenAI — ghi \"KhoaApi\" trong mục \"TroLyAI\" của appsettings.Local.json.";
            else
                _kenh = new KenhOpenAI(CauHinhUngDung.KhoaApiAI, CauHinhUngDung.MoHinhAI, TimeSpan.FromSeconds(CauHinhUngDung.ThoiGianChoAiGiay));
        }

        // Unit test: kenh gia, bo cong cu gia, khong ghi CSDL.
        public TroLyChatService(IKenhMoHinhNgonNgu kenh, IBoCongCuTroLy congCu, Action<string, string, string?> ghiNhatKy, Func<DateTime> bayGio)
        {
            _kenh = kenh;
            _congCu = congCu;
            _ghiNhatKy = ghiNhatKy;
            _bayGio = bayGio;
        }

        public bool SanSang => _kenh != null;
        public string LyDoChuaSanSang { get; } = "";
        public string TenMoHinh => _kenh?.TenMoHinh ?? CauHinhUngDung.MoHinhAI;
        public IReadOnlyList<TinNhanChat> LichSu => _lichSu;
        public KetQuaTuVanVe? TuVanGanNhat => _tuVan;
        public YeuCauTuVanVe? YeuCauTuVanGanNhat => _ycTuVan;
        public bool DaDatPhuongAn(int soThuTu) => _daDat.Contains(soThuTu);

        public void BatDauLai()
        {
            _lichSu.Clear();
            _daDat.Clear();
            _tuVan = null;
            _ycTuVan = null;
        }

        // Tu van bang che do Loc nang cao (ngoai chat) -> the phuong an doi -> bao cho AI biet de "dat phuong an 2" dung bo dang hien.
        public void GhiNhanTuVanTuForm(YeuCauTuVanVe yc, KetQuaTuVanVe kq)
        {
            _tuVan = kq.CacPhuongAn.Count > 0 ? kq : null;
            _ycTuVan = kq.CacPhuongAn.Count > 0 ? yc : null;
            _daDat.Clear();
            if (SanSang && kq.CacPhuongAn.Count > 0)
                _lichSu.Add(TinNhanChat.NguoiDung("[Ghi chú hệ thống] Nhân viên vừa tư vấn bằng chế độ Lọc nâng cao, các phương án đang hiện trên màn hình: " +
                                                  QuyTacMoHinhNgonNgu.KetQuaTuVanChoMoHinh(yc, kq)));
        }

        // Dat ve bang nut tren the phuong an (ngoai chat).
        public void GhiNhanDaDatNgoaiChat(int soThuTu, KetQuaDatVeHo kq)
        {
            if (!kq.DaDat) return;
            _daDat.Add(soThuTu);
            if (SanSang && _lichSu.Count > 0) _lichSu.Add(TinNhanChat.NguoiDung(GhiChuDaDat(soThuTu, kq)));
        }

        private static string GhiChuDaDat(int soThuTu, KetQuaDatVeHo kq)
            => $"[Ghi chú hệ thống] Nhân viên đã đặt phương án {soThuTu}: đơn {kq.MaPnr}, {kq.SoVe} vé, tổng {kq.TongTien.ToString("N0", Vi)} đ.";

        // ---------------------------------------------------------------------
        public async Task<KetQuaLuotChat> GuiAsync(string cauHoi, IProgress<string>? tienDo = null, CancellationToken ct = default)
        {
            var kq = new KetQuaLuotChat();
            KetQuaLuotChat Loi(string s) { kq.Loi = s; return kq; }

            if (!PhanQuyenService.KiemTraQuyen(MaQuyen.DungTroLyAI, "dùng trợ lý AI", out string loi)) return Loi(loi);
            if (_kenh == null) return Loi(LyDoChuaSanSang);
            string cau = (cauHoi ?? "").Trim();
            if (cau.Length == 0) return Loi("Nhập câu hỏi trước.");
            if (cau.Length > QuyTacMoHinhNgonNgu.DoDaiCauHoiToiDa) return Loi($"Câu hỏi dài quá {QuyTacMoHinhNgonNgu.DoDaiCauHoiToiDa} ký tự.");
            try
            {
                _dsGa ??= _congCu.DanhSachGa();
                _tyLeGiam ??= _congCu.TyLeGiamGia();
                _dsCongCu ??= QuyTacMoHinhNgonNgu.DanhSachCongCu(_dsGa.Select(g => g.MaGaCode));
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                return Loi("Không đọc được danh sách ga: " + DichLoiSqlPhuongTien.ThongDiep(ex));
            }

            int moc = _lichSu.Count;
            _soThuTuVuaDat = null;
            string cauGui = QuyTacMoHinhNgonNgu.AnDuLieuCaNhan(cau);
            _lichSu.Add(TinNhanChat.NguoiDung(cauGui));
            var cacLenh = new List<string>();
            try
            {
                for (int vong = 1; ; vong++)
                {
                    if (vong > SoVongToiDa)
                    {
                        kq.TraLoi = "Câu này cần tra cứu quá nhiều bước — anh/chị hỏi cụ thể hơn giúp em (ga đi, ga đến, ngày).";
                        _lichSu.Add(TinNhanChat.TroLy(kq.TraLoi));
                        break;
                    }
                    tienDo?.Report(vong == 1 ? "AI đang đọc câu hỏi…" : "AI đang trả lời dựa trên dữ liệu vừa tra…");
                    string heThong = QuyTacMoHinhNgonNgu.LoiNhacHeThong(_dsGa!, _tyLeGiam!, _bayGio());
                    var ph = await _kenh.TroChuyenAsync(heThong, QuyTacMoHinhNgonNgu.CatLichSu(_lichSu), _dsCongCu!, ct);
                    kq.SoLanGoiMoHinh++;
                    if (ph.CacLenh.Count == 0)
                    {
                        kq.TraLoi = QuyTacMoHinhNgonNgu.BoMarkdown(ph.NoiDung);
                        _lichSu.Add(TinNhanChat.TroLy(kq.TraLoi));
                        break;
                    }
                    _lichSu.Add(TinNhanChat.TroLy(ph.NoiDung, ph.CacLenh));
                    foreach (var lenh in ph.CacLenh)
                    {
                        var r = await ChayLenhAsync(lenh, kq, tienDo, ct);
                        _lichSu.Add(TinNhanChat.KetQua(lenh.Ma, r.ChoMoHinh));
                        kq.CacBuocTraCuu.Add(r.MoTa);
                        cacLenh.Add($"{lenh.Ten} {lenh.ThamSoJson}");
                    }
                }
            }
            catch (LoiMoHinhNgonNguException ex)
            {
                // Bo ca luot (khong de lenh goi cong cu treo trong lich su); da dat ve thi van ghi lai cho AI biet.
                _lichSu.RemoveRange(moc, _lichSu.Count - moc);
                if (kq.DatVe?.DaDat == true && TimSoThuTuVuaDat() is int n) _lichSu.Add(TinNhanChat.NguoiDung(GhiChuDaDat(n, kq.DatVe)));
                return Loi(ex.Message);
            }
            catch (OperationCanceledException)
            {
                _lichSu.RemoveRange(moc, _lichSu.Count - moc);
                throw;
            }

            // Doi chieu so lieu voi du lieu he thong da co trong hoi thoai (ket qua cong cu + cau hoi + loi nhac).
            string duLieu = string.Join("\n", _lichSu.Where(t => t.VaiTro != VaiTroTinNhan.TroLy).Select(t => t.NoiDung))
                            + "\n" + QuyTacMoHinhNgonNgu.LoiNhacHeThong(_dsGa!, _tyLeGiam!, _bayGio());
            kq.SoLieuChuaDoiChieu = QuyTacMoHinhNgonNgu.KiemSoLieu(kq.TraLoi, duLieu).SoLieuLa;

            try { _ghiNhatKy($"[Chat · {_kenh.TenMoHinh}] {cauGui}", kq.TraLoi, cacLenh.Count > 0 ? string.Join("\n", cacLenh) : null); }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex)) { /* khong ghi duoc nhat ky thi van tra loi */ }

            kq.ThanhCong = true;
            return kq;
        }

        private int? _soThuTuVuaDat;
        private int? TimSoThuTuVuaDat() => _soThuTuVuaDat;

        private async Task<KetQuaCongCu> ChayLenhAsync(LenhGoiCongCu lenh, KetQuaLuotChat kq, IProgress<string>? tienDo, CancellationToken ct)
        {
            if (!QuyTacMoHinhNgonNgu.DocJson(lenh.ThamSoJson, out var ts, out string loi)) return KetQuaCongCu.BaoLoi(loi);
            if (lenh.Ten == QuyTacMoHinhNgonNgu.CongCu.DatVe) return await DatVeAsync(ts, kq, tienDo);

            tienDo?.Report("Đang tra cứu dữ liệu hệ thống…");
            DateTime bayGio = _bayGio();
            var r = await Task.Run(() => _congCu.ThucThi(lenh.Ten, ts, bayGio), ct);
            if (r.TuVan != null && r.YeuCau != null && r.TuVan.CacPhuongAn.Count > 0)
            {
                _tuVan = r.TuVan;
                _ycTuVan = r.YeuCau;
                _daDat.Clear();
                kq.TuVan = r.TuVan;
                kq.YeuCauTuVan = r.YeuCau;
            }
            return r;
        }

        private async Task<KetQuaCongCu> DatVeAsync(System.Text.Json.JsonElement ts, KetQuaLuotChat kq, IProgress<string>? tienDo)
        {
            int n = (int)QuyTacMoHinhNgonNgu.So(ts, "phuong_an");
            if (_tuVan == null || _ycTuVan == null)
                return KetQuaCongCu.BaoLoi("Chưa có lần tư vấn nào — cần gọi tu_van_phuong_an trước khi đặt.");
            if (n < 1 || n > _tuVan.CacPhuongAn.Count)
                return KetQuaCongCu.BaoLoi($"Lần tư vấn gần nhất chỉ có {_tuVan.CacPhuongAn.Count} phương án.");
            if (_daDat.Contains(n))
                return KetQuaCongCu.BaoLoi($"Phương án {n} đã đặt rồi (các ghế đó đã bán) — tư vấn lại nếu khách cần thêm vé.");
            if (!PhanQuyenService.CoQuyen(MaQuyen.BanVe))
                return KetQuaCongCu.BaoLoi("Tài khoản đang đăng nhập không có quyền bán vé.");
            if (XacNhanDatVe == null)
                return KetQuaCongCu.BaoLoi("Màn hình này không hỗ trợ đặt vé.");

            var pa = _tuVan.CacPhuongAn[n - 1];
            tienDo?.Report($"Đang chờ nhân viên xác nhận đặt phương án {n}…");
            var r = await XacNhanDatVe(new YeuCauDatVeHo { PhuongAn = pa, YeuCau = _ycTuVan, TenGaDi = _tuVan.TenGaDi, TenGaDen = _tuVan.TenGaDen });
            kq.DatVe = r;
            if (r.DaDat)
            {
                _daDat.Add(n);
                _soThuTuVuaDat = n;
                string ht = DatVeHoService.CacHinhThucTT.FirstOrDefault(x => x.Ma == r.HinhThucTT).Ten ?? r.HinhThucTT;
                return new KetQuaCongCu
                {
                    ChoMoHinh = $"ĐÃ ĐẶT: đơn {r.MaPnr}, {r.SoVe} vé ({string.Join(", ", r.CacMaVe)}), tổng {r.TongTien.ToString("N0", Vi)} đ, " +
                                $"thanh toán {ht}. Tàu {pa.SoHieuMacTau}, {pa.NhanGhe}.",
                    MoTa = $"Đặt vé phương án {n}: đơn {r.MaPnr}, {r.SoVe} vé, {r.TongTien.ToString("N0", Vi)} đ",
                };
            }
            if (r.NguoiDungHuy)
                return new KetQuaCongCu
                {
                    ChoMoHinh = "NHÂN VIÊN ĐÃ HỦY: nhân viên chủ động đóng cửa sổ xác nhận, chưa đặt vé nào (không phải lỗi hệ thống). " +
                                "Chỉ cần xác nhận ngắn gọn là chưa đặt; đặt lại khi người dùng yêu cầu.",
                    MoTa = $"Đặt vé phương án {n}: nhân viên hủy",
                };
            return KetQuaCongCu.BaoLoi("Không đặt được: " + r.Loi);
        }
    }
}
