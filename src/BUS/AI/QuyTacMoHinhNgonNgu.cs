using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DTO.AI;

namespace BUS.AI
{
    // =========================================================================
    // LUAT CHO TRO CHUYEN AI (16/11) - ham thuan, unit test duoc (Tests/AI/TroLyChatTests).
    //   - Loi nhac he thong (vai tro, quy tac, danh sach ga, giam gia, ngay gio) + 6 cong cu (function calling, strict).
    //   - Doc / kiem tham so cong cu: ga phai co that, ngay khong o qua khu, 1-6 khach... (sai -> tra loi cho mo hinh, khong nem).
    //   - AnDuLieuCaNhan: che day >= 9 chu so (CCCD, SDT) truoc khi gui ra ngoai / ghi nhat ky.
    //   - KiemSoLieu: so tien / so lon trong cau tra loi phai co trong du lieu he thong da gui (khong co -> canh bao).
    //   - CatLichSu: chi gui N tin nhan gan nhat, cat theo dau cau hoi (khong de ket qua cong cu mo coi).
    // Mo hinh KHONG cham diem, KHONG chon ghe: xep hang van la QuyTacTuVanVe (MCDM).
    // =========================================================================
    public static class QuyTacMoHinhNgonNgu
    {
        public const int DoDaiCauHoiToiDa = 1000;
        public const int SoTinNhanGuiToiDa = 40;

        public static class CongCu
        {
            public const string DsMacTau = "ds_mac_tau";
            public const string TimChuyenTau = "tim_chuyen_tau";
            public const string LichTrinhChuyen = "lich_trinh_chuyen";
            public const string ChoTrongVaGia = "xem_cho_trong_va_gia";
            public const string TuVanPhuongAn = "tu_van_phuong_an";
            public const string DatVe = "dat_ve";
        }

        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        public static readonly JsonSerializerOptions JsonGon = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        // ---------------------------------------------------------------------
        // Che du lieu ca nhan: chuoi chu so (cho phep cach / cham / gach giua) co >= 9 chu so. Gia ve (<= 8 chu so) giu nguyen.
        // ---------------------------------------------------------------------
        private static readonly Regex DaySo = new(@"\d(?:[\d .\-]*\d)?", RegexOptions.Compiled);

        public static string AnDuLieuCaNhan(string? cau)
        {
            if (string.IsNullOrEmpty(cau)) return "";
            return DaySo.Replace(cau, m => m.Value.Count(char.IsDigit) >= 9 ? "[số đã ẩn]" : m.Value);
        }

        // ---------------------------------------------------------------------
        // Loi nhac he thong
        // ---------------------------------------------------------------------
        public static string LoiNhacHeThong(IReadOnlyList<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)> dsGa,
                                            IReadOnlyDictionary<string, decimal> tyLeGiam, DateTime bayGio)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là trợ lý AI tại quầy bán vé của Đường sắt Việt Nam, trò chuyện với nhân viên bán vé (họ có thể gõ lại lời khách). " +
                          "Trả lời bằng tiếng Việt, tự nhiên, ngắn gọn, thân thiện.");
            sb.AppendLine();
            sb.AppendLine("Cách làm việc:");
            sb.AppendLine("- Câu hỏi chung hoặc ngoài lề: trả lời bình thường bằng hiểu biết của bạn. Bạn không có dữ liệu thời gian thực " +
                          "(thời tiết, tin tức, giá vàng...) thì nói rõ là không có.");
            sb.AppendLine("- Câu hỏi về mác tàu, chuyến tàu, giờ chạy, ga dừng, chỗ trống, giá vé, chọn vé: luôn dùng công cụ để lấy dữ liệu thật " +
                          "của hệ thống. Không tự đoán giờ tàu, giá vé, số chỗ.");
            sb.AppendLine("- Hiểu cả tiếng Việt không dấu, viết tắt (\"sg\" = Sài Gòn, \"hn\" = Hà Nội, \"800k\" = 800.000 đồng, \"t6\" = thứ Sáu). " +
                          "Tên thành phố thì dùng ga chính của thành phố đó.");
            sb.AppendLine("- Khách cần chọn vé: gọi tu_van_phuong_an (hệ thống chấm điểm đa tiêu chí và chọn sẵn ghế). Thiếu ga đi, ga đến hoặc ngày đi " +
                          "thì hỏi lại trước khi gọi. Có kết quả thì nói phương án nên chọn và vì sao trong 2–4 câu; danh sách đầy đủ đã hiện ở khung " +
                          "bên phải màn hình, không cần chép lại hết.");
            sb.AppendLine("- Đặt vé: chỉ gọi dat_ve khi người dùng nói rõ muốn đặt, theo số thứ tự phương án của lần tư vấn gần nhất. Hệ thống tự mở " +
                          "cửa sổ xác nhận để nhân viên nhập họ tên, CCCD, số điện thoại và xác nhận — không hỏi những thông tin đó trong chat.");
            sb.AppendLine("- Số tiền chỉ lấy từ dữ liệu công cụ, viết đầy đủ dạng 1.264.000 đ (không viết \"triệu\", \"k\").");
            sb.AppendLine("- Không dùng định dạng markdown (không **, không #). Được xuống dòng và gạch đầu dòng \"-\".");
            sb.AppendLine();
            sb.AppendLine("Quy định bán vé: mỗi đơn tối đa 8 vé; mỗi vé cần họ tên và CCCD 12 số của hành khách. Giảm giá theo đối tượng: " +
                          string.Join(", ", tyLeGiam.Where(kv => kv.Value > 0).Select(kv => $"{TenDoiTuong(kv.Key)} {kv.Value * 100:0.#}%")) + ".");
            sb.AppendLine();
            sb.AppendLine("Danh sách ga đang khai thác (mã: tên, lý trình km):");
            foreach (var g in dsGa)
                sb.AppendLine($"{g.MaGaCode}: {g.TenGa} (km {g.LyTrinhKm.ToString("0.#", CultureInfo.InvariantCulture)})");
            sb.AppendLine();
            sb.Append($"Bây giờ là {bayGio:HH:mm} {TenThu(bayGio)} {bayGio:dd/MM/yyyy} ({bayGio:yyyy-MM-dd}).");
            return sb.ToString();
        }

        public static string TenDoiTuong(string ma) => ma switch
        {
            "TRE_EM" => "trẻ em",
            "NGUOI_GIA" => "người cao tuổi",
            "SINH_VIEN" => "học sinh / sinh viên",
            _ => "người lớn"
        };

        // ---------------------------------------------------------------------
        // Dinh nghia cong cu (strict: moi thuoc tinh required, additionalProperties false; "khong co" = 0 / "")
        // ---------------------------------------------------------------------
        public static IReadOnlyList<CongCuMoHinh> DanhSachCongCu(IEnumerable<string> cacMaGa)
        {
            var maGa = cacMaGa.Distinct().ToArray();
            object ga(string moTa) => new { type = "string", @enum = maGa, description = moTa };
            object ngay = new { type = "string", description = "Ngày đi dạng yyyy-MM-dd" };
            object maChuyen = new { type = "integer", description = "ma_chuyen_tau lấy từ kết quả tim_chuyen_tau" };

            return new[]
            {
                Tao(CongCu.DsMacTau, "Danh sách mác tàu đang khai thác: số hiệu, loại tàu, ga đầu – ga cuối, thời gian chạy chuẩn.",
                    new Dictionary<string, object>()),
                Tao(CongCu.TimChuyenTau, "Tìm các chuyến tàu khách dừng ở cả ga đi và ga đến trong một ngày: giờ đi tại ga đi, giờ đến tại ga đến, số chỗ trống, giá ghế mềm.",
                    new Dictionary<string, object> { ["ga_di"] = ga("Mã ga đi"), ["ga_den"] = ga("Mã ga đến"), ["ngay"] = ngay }),
                Tao(CongCu.LichTrinhChuyen, "Lịch trình một chuyến tàu: các ga dừng theo thứ tự với giờ đến, giờ đi.",
                    new Dictionary<string, object> { ["ma_chuyen_tau"] = maChuyen }),
                Tao(CongCu.ChoTrongVaGia, "Chỗ trống và giá vé theo từng loại chỗ (ghế cứng, ghế mềm, giường khoang 6, khoang 4 theo tầng) của một chuyến trên một chặng.",
                    new Dictionary<string, object> { ["ma_chuyen_tau"] = maChuyen, ["ga_di"] = ga("Mã ga đi"), ["ga_den"] = ga("Mã ga đến") }),
                Tao(CongCu.TuVanPhuongAn, "Tư vấn chọn vé: hệ thống chấm điểm đa tiêu chí mọi chuyến còn chỗ trên chặng và trả về tối đa 3 phương án tốt nhất (đã chọn sẵn ghế) kèm lý do.",
                    new Dictionary<string, object>
                    {
                        ["ga_di"] = ga("Mã ga đi"),
                        ["ga_den"] = ga("Mã ga đến"),
                        ["ngay"] = ngay,
                        ["so_nguoi"] = new { type = "integer", description = $"Tổng số khách, 1–{TuVanVeService.SoNguoiToiDa}" },
                        ["co_nguoi_cao_tuoi"] = new { type = "boolean", description = "Có người từ 60 tuổi" },
                        ["co_tre_em"] = new { type = "boolean", description = "Có trẻ em đi cùng" },
                        ["la_sinh_vien"] = new { type = "boolean", description = "Khách là học sinh / sinh viên" },
                        ["muc_ngan_sach"] = new { type = "string", @enum = new[] { MucNganSach.TietKiem, MucNganSach.TieuChuan, MucNganSach.CaoCap },
                                                  description = "TIET_KIEM: muốn rẻ; CAO_CAP: muốn thoải mái nhất; còn lại TIEU_CHUAN" },
                        ["ngan_sach_toi_da_moi_ve"] = new { type = "integer", description = "Số đồng tối đa cho một vé, 0 nếu khách không nói" },
                        ["khung_gio"] = new { type = "string", @enum = new[] { KhungGioUuTien.KhongUuTien, KhungGioUuTien.TranhDiDem, KhungGioUuTien.NguTrenTau },
                                              description = "TRANH_DEM: muốn đi / đến ban ngày; NGU_TREN_TAU: muốn đi đêm ngủ trên tàu; còn lại KHONG" },
                    }),
                Tao(CongCu.DatVe, "Đặt vé cho một phương án của lần tư vấn gần nhất. Hệ thống mở cửa sổ xác nhận để nhân viên nhập thông tin hành khách; chỉ gọi khi người dùng yêu cầu đặt.",
                    new Dictionary<string, object> { ["phuong_an"] = new { type = "integer", description = "Số thứ tự phương án (1, 2 hoặc 3)" } }),
            };
        }

        private static CongCuMoHinh Tao(string ten, string moTa, Dictionary<string, object> thuocTinh)
            => new(ten, moTa, new Dictionary<string, JsonElement>
            {
                ["type"] = JsonSerializer.SerializeToElement("object"),
                ["properties"] = JsonSerializer.SerializeToElement(thuocTinh),
                ["required"] = JsonSerializer.SerializeToElement(thuocTinh.Keys.ToArray()),
                ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
            });

        // ---------------------------------------------------------------------
        // Doc tham so. Sai -> loi (chuoi) tra cho mo hinh de no hoi lai / sua, khong nem ngoai le.
        // ---------------------------------------------------------------------
        public static bool DocJson(string thamSoJson, out JsonElement goc, out string loi)
        {
            loi = "";
            try
            {
                goc = JsonDocument.Parse(string.IsNullOrWhiteSpace(thamSoJson) ? "{}" : thamSoJson).RootElement.Clone();
                if (goc.ValueKind == JsonValueKind.Object) return true;
            }
            catch (JsonException) { goc = default; }
            loi = "Tham số không phải JSON object.";
            return false;
        }

        public static string Chuoi(JsonElement goc, string ten)
            => goc.ValueKind == JsonValueKind.Object && goc.TryGetProperty(ten, out var v) && v.ValueKind == JsonValueKind.String
                ? (v.GetString() ?? "").Trim() : "";

        public static long So(JsonElement goc, string ten)
            => goc.ValueKind == JsonValueKind.Object && goc.TryGetProperty(ten, out var v) && v.ValueKind == JsonValueKind.Number
               && v.TryGetInt64(out long n) ? n : 0;

        public static bool Bool(JsonElement goc, string ten)
            => goc.ValueKind == JsonValueKind.Object && goc.TryGetProperty(ten, out var v) && v.ValueKind == JsonValueKind.True;

        public static bool DocGa(JsonElement goc, string ten, IReadOnlyList<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)> dsGa,
                                 out (int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm) ga, out string loi)
        {
            string ma = Chuoi(goc, ten);
            ga = dsGa.FirstOrDefault(g => g.MaGaCode.Equals(ma, StringComparison.OrdinalIgnoreCase));
            loi = ga.MaGa > 0 ? "" : $"Không có ga mã \"{ma}\".";
            return ga.MaGa > 0;
        }

        public static bool DocNgay(JsonElement goc, DateTime homNay, out DateTime ngay, out string loi)
        {
            string s = Chuoi(goc, "ngay");
            if (!DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out ngay))
            {
                loi = $"Ngày \"{s}\" không đúng dạng yyyy-MM-dd.";
                return false;
            }
            if (ngay.Date < homNay.Date) { loi = $"Ngày {ngay:dd/MM/yyyy} đã qua."; return false; }
            loi = "";
            return true;
        }

        public static bool DocYeuCauTuVan(JsonElement goc, IReadOnlyList<(int MaGa, string MaGaCode, string TenGa, decimal LyTrinhKm)> dsGa,
                                          DateTime homNay, out YeuCauTuVanVe yc, out string loi)
        {
            yc = new YeuCauTuVanVe();
            if (!DocGa(goc, "ga_di", dsGa, out var di, out loi)) return false;
            if (!DocGa(goc, "ga_den", dsGa, out var den, out loi)) return false;
            if (di.MaGa == den.MaGa) { loi = "Ga đi và ga đến trùng nhau."; return false; }
            if (!DocNgay(goc, homNay, out var ngay, out loi)) return false;
            long soNguoi = So(goc, "so_nguoi");
            if (soNguoi < 1 || soNguoi > TuVanVeService.SoNguoiToiDa)
            {
                loi = $"Số khách phải từ 1 đến {TuVanVeService.SoNguoiToiDa} (đoàn lớn hơn thì tách đơn).";
                return false;
            }
            long nganSach = So(goc, "ngan_sach_toi_da_moi_ve");
            if (nganSach != 0 && (nganSach < 10_000 || nganSach > 100_000_000))
            {
                loi = "Ngân sách tối đa mỗi vé không hợp lý (tính bằng đồng, vd 800000).";
                return false;
            }
            string muc = Chuoi(goc, "muc_ngan_sach");
            string kg = Chuoi(goc, "khung_gio");
            yc = new YeuCauTuVanVe
            {
                MaGaDi = di.MaGa,
                MaGaDen = den.MaGa,
                NgayDi = ngay.Date,
                SoNguoi = (int)soNguoi,
                CoNguoiCaoTuoi = Bool(goc, "co_nguoi_cao_tuoi"),
                CoTreEm = Bool(goc, "co_tre_em"),
                LaSinhVien = Bool(goc, "la_sinh_vien"),
                MucNganSach = muc is MucNganSach.TietKiem or MucNganSach.CaoCap ? muc : MucNganSach.TieuChuan,
                NganSachToiDaMoiNguoi = nganSach > 0 ? nganSach : null,
                KhungGio = kg is KhungGioUuTien.TranhDiDem or KhungGioUuTien.NguTrenTau ? kg : KhungGioUuTien.KhongUuTien,
            };
            return true;
        }

        // ---------------------------------------------------------------------
        // Ket qua tu van -> JSON gon cho mo hinh (gia da dinh dang vi-VN de doi chieu so lieu)
        // ---------------------------------------------------------------------
        public static string KetQuaTuVanChoMoHinh(YeuCauTuVanVe yc, KetQuaTuVanVe kq)
        {
            var loaiKhach = QuyTacTuVanVe.LoaiKhachDuKien(yc.SoNguoi, yc.CoNguoiCaoTuoi, yc.CoTreEm, yc.LaSinhVien);
            var duLieu = new
            {
                hanh_trinh = $"{kq.TenGaDi} → {kq.TenGaDen}",
                cu_ly_km = kq.CuLyKm.ToString("N0", Vi),
                ngay_di = $"{TenThu(yc.NgayDi)} {yc.NgayDi:dd/MM/yyyy}",
                khach = QuyTacTuVanVe.MoTaLoaiKhach(loaiKhach),
                da_xet = $"{kq.SoPhuongAnDaXet} phương án trên {kq.SoChuyenDaXet} chuyến",
                phuong_an = kq.CacPhuongAn.Select(p => new
                {
                    so_thu_tu = p.XepHang,
                    mac_tau = p.SoHieuMacTau,
                    loai_cho = p.TenLoaiCho,
                    toa_ghe = p.NhanGhe,
                    di = $"{p.GioDi:HH:mm} ngày {p.GioDi:dd/MM}",
                    den = $"{p.GioDen:HH:mm} ngày {p.GioDen:dd/MM}",
                    so_gio_chay = p.SoGioChay.ToString("0.#", Vi),
                    chay_qua_dem = p.QuaDem,
                    gia_mot_ve = p.GiaMotVe.ToString("N0", Vi) + " đ",
                    tong_tien_ca_doan = p.TongTien.ToString("N0", Vi) + " đ",
                    da_giam_gia_doi_tuong = p.CoGiamGia,
                    vuot_ngan_sach = p.VuotNganSach,
                    chenh_lech_so_voi_phuong_an_1 = p.XepHang > 1 && kq.CacPhuongAn.Count > 0
                        ? (p.TongTien - kq.CacPhuongAn[0].TongTien).ToString("+#,##0;-#,##0;0", Vi) + " đ (cả đoàn)"
                        : null,
                    diem = p.Diem.ToString("0.00", Vi) + "/10",
                    ly_do = p.LyDo,
                }),
                ghi_chu = kq.GhiChu.Count > 0 ? kq.GhiChu : null,
            };
            return JsonSerializer.Serialize(duLieu, JsonGon);
        }

        // ---------------------------------------------------------------------
        // Doi chieu so lieu: so co dau ngan nghin (1.264.000) hoac >= 4 chu so lien trong cau tra loi phai co trong du lieu
        // he thong; viet "1,2 triệu" / "800k" thi khong doi chieu duoc -> cung bao. Gio, ngay, diem (8,35) khong bi xet.
        // ---------------------------------------------------------------------
        private static readonly Regex SoLon = new(@"(?<![\d.,])\d{1,3}(?:[.,]\d{3})+(?![\d])|(?<![\d.,])\d{4,}(?![\d])", RegexOptions.Compiled);
        private static readonly Regex SoKhongDayDu = new(@"\d+(?:[.,]\d+)?\s*(?:triệu|tr\b|nghìn|ngàn|k\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static KetQuaDoiChieuSoLieu KiemSoLieu(string traLoi, string duLieuHeThong)
        {
            string noiDung = (traLoi ?? "").Trim();
            var kq = new KetQuaDoiChieuSoLieu { NoiDung = noiDung, KhopHet = true };
            if (noiDung.Length == 0) return kq;

            var hopLe = new HashSet<long>(SoLon.Matches(duLieuHeThong ?? "").Select(m => GiaTri(m.Value)));
            kq.SoLieuLa.AddRange(SoLon.Matches(noiDung).Select(m => m.Value).Where(v => !hopLe.Contains(GiaTri(v))).Distinct());
            kq.SoLieuLa.AddRange(SoKhongDayDu.Matches(noiDung).Select(m => m.Value.Trim()).Distinct());
            kq.KhopHet = kq.SoLieuLa.Count == 0;
            return kq;
        }

        private static long GiaTri(string so)
        {
            string chiSo = new(so.Where(char.IsDigit).ToArray());
            return chiSo.Length is > 0 and <= 18 && long.TryParse(chiSo, out long v) ? v : -1;
        }

        // ---------------------------------------------------------------------
        // Lich su gui di: toi da N tin nhan gan nhat, bat dau o mot cau hoi cua nguoi dung
        // (cat giua chung thi ket qua cong cu mat lenh goi tuong ung -> API bao loi).
        // ---------------------------------------------------------------------
        public static IReadOnlyList<TinNhanChat> CatLichSu(IReadOnlyList<TinNhanChat> lichSu, int toiDa = SoTinNhanGuiToiDa)
        {
            if (lichSu.Count <= toiDa) return lichSu;
            int batDau = lichSu.Count - toiDa;
            while (batDau < lichSu.Count && lichSu[batDau].VaiTro != VaiTroTinNhan.NguoiDung) batDau++;
            if (batDau >= lichSu.Count)       // luot cuoi dai hon toiDa: lay tu cau hoi cuoi cung
                batDau = Enumerable.Range(0, lichSu.Count).Last(i => lichSu[i].VaiTro == VaiTroTinNhan.NguoiDung);
            return lichSu.Skip(batDau).ToList();
        }

        // Bo dau markdown con sot (** __ #) vi khung chat hien chu thuong.
        public static string BoMarkdown(string s)
            => Regex.Replace(Regex.Replace(s ?? "", @"\*\*|__", ""), @"(?m)^\s{0,3}#{1,6}\s+", "").Trim();

        // ---------------------------------------------------------------------
        public static string TenThu(DateTime ngay) => ngay.DayOfWeek switch
        {
            DayOfWeek.Monday => "thứ Hai",
            DayOfWeek.Tuesday => "thứ Ba",
            DayOfWeek.Wednesday => "thứ Tư",
            DayOfWeek.Thursday => "thứ Năm",
            DayOfWeek.Friday => "thứ Sáu",
            DayOfWeek.Saturday => "thứ Bảy",
            _ => "Chủ nhật"
        };

        public static string TenLoaiTau(string ma) => ma switch
        {
            "TAU_NHANH" => "tàu nhanh",
            "TAU_CHO" => "tàu chợ",
            "TAU_HANG" => "tàu hàng",
            _ => ma.ToLowerInvariant().Replace('_', ' ')
        };
    }
}
