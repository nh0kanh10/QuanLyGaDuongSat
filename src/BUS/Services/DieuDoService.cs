using System.Data;
using DAL.Repositories;
using DTO.VanHanh;

namespace BUS.Services
{
    public class DieuDoService
    {
        private readonly DieuDoRepository _repo = new();

        /// <summary>
        /// Lấy toàn bộ dữ liệu biểu đồ chạy tàu theo ngày, gom nhóm theo từng chuyến tàu
        /// </summary>
        public List<HanhTrinhTauDTO> LayDuLieuBieuDo(DateTime ngay)
        {
            var result = new List<HanhTrinhTauDTO>();
            DataTable dt = _repo.LayDuLieuBieuDo(ngay);

            var grouped = dt.AsEnumerable().GroupBy(r => Convert.ToInt32(r["MaChuyenTau"]));

            foreach (var group in grouped)
            {
                DataRow first = group.First();
                int maChuyenTau = group.Key;
                string soHieu = first["SoHieuMacTau"].ToString() ?? "";
                string loaiTau = first["LoaiTau"].ToString() ?? "TAU_NHANH";
                string huongChay = first["HuongChay"].ToString() ?? "BAC_NAM";
                int mucUuTien = first["MucUuTien"] != DBNull.Value ? Convert.ToInt32(first["MucUuTien"]) : 3;
                decimal chieuDai = first["TongChieuDaiM"] != DBNull.Value ? Convert.ToDecimal(first["TongChieuDaiM"]) : 0m;
                string trangThai = first["TrangThai"].ToString() ?? "DA_LEN_LICH";

                var hanhTrinh = new HanhTrinhTauDTO
                {
                    MaChuyenTau = maChuyenTau,
                    SoHieuMacTau = soHieu,
                    LoaiTau = loaiTau,
                    HuongChay = huongChay,
                    MucUuTien = mucUuTien,
                    TongChieuDaiM = chieuDai,
                    TrangThai = trangThai,
                    GioXuatPhat = Convert.ToDateTime(first["GioXuatPhatKH"]),
                    GioVeDich = Convert.ToDateTime(first["GioVeDichKH"]),
                    TenGaDi = first["TenGaDi"].ToString() ?? "",
                    TenGaDen = first["TenGaDen"].ToString() ?? "",
                    MauSacHex = ChonMauSacTheoLoaiTau(soHieu, loaiTau, huongChay)
                };

                foreach (DataRow r in group)
                {
                    DateTime gioDen = Convert.ToDateTime(r["GioDenKeHoach"]);
                    DateTime gioDi = Convert.ToDateTime(r["GioDiKeHoach"]);
                    int thoiGianDung = (int)(gioDi - gioDen).TotalMinutes;

                    hanhTrinh.DanhSachDiem.Add(new DiemChayTauDTO
                    {
                        MaChuyenTau = maChuyenTau,
                        SoHieuMacTau = soHieu,
                        LoaiTau = loaiTau,
                        HuongChay = huongChay,
                        MucUuTien = mucUuTien,
                        ThuTuDung = r["ThuTuDung"] != DBNull.Value ? Convert.ToInt32(r["ThuTuDung"]) : 0,
                        MaGa = r["MaGa"] != DBNull.Value ? Convert.ToInt32(r["MaGa"]) : 0,
                        TenGa = r["TenGa"]?.ToString() ?? "",
                        MaGaCode = r["MaGaCode"]?.ToString() ?? "",
                        LyTrinhKm = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m,
                        GioDen = gioDen,
                        GioDi = gioDi,
                        ThoiGianDungPhut = Math.Max(0, thoiGianDung),
                        LaDiemTranh = r["LaDiemTranh"] != DBNull.Value && Convert.ToBoolean(r["LaDiemTranh"]),
                        MaDuongRay = r["MaDuongRay"] != DBNull.Value ? Convert.ToInt32(r["MaDuongRay"]) : null,
                        SoHieuDuongRay = r["SoHieuDuongRay"] != DBNull.Value ? Convert.ToInt32(r["SoHieuDuongRay"]) : null,
                        LoaiDuong = r["LoaiDuong"] != DBNull.Value ? r["LoaiDuong"].ToString() : null
                    });
                }

                result.Add(hanhTrinh);
            }

            return result;
        }

        public DataTable LayDanhSachGaTuyen() => _repo.LayDanhSachGaTuyen();

        public DataTable LayDanhSachKhuGian() => _repo.LayDanhSachKhuGian();

        public DateTime LayNgayBieuDoMacDinh() => _repo.LayNgayBieuDoMacDinh();

        public DataTable LayDanhSachNgayCoTau() => _repo.LayDanhSachNgayCoTau();

        /// <summary>
        /// Thuật toán phát hiện xung đột trên tuyến đường sắt đơn (Single-Track Conflict Detection)
        /// Quét tất cả các cặp tàu chạy trong ngày để tìm vi phạm an toàn khu gian
        /// </summary>
        public List<XungDotKhuGianDTO> KiemTraXungDotToanTuyen(DateTime ngay)
        {
            var dsXungDot = new List<XungDotKhuGianDTO>();
            List<HanhTrinhTauDTO> dsTau = LayDuLieuBieuDo(ngay);

            if (dsTau.Count < 2) return dsXungDot;

            int maXungDot = 1;

            // So sánh từng cặp tàu (T1, T2)
            for (int i = 0; i < dsTau.Count - 1; i++)
            {
                var t1 = dsTau[i];
                if (t1.DanhSachDiem.Count < 2) continue;

                for (int j = i + 1; j < dsTau.Count; j++)
                {
                    var t2 = dsTau[j];
                    if (t2.DanhSachDiem.Count < 2) continue;

                    // Kiểm tra từng chặng di chuyển giữa 2 ga liên tiếp của T1 với T2
                    for (int p1 = 0; p1 < t1.DanhSachDiem.Count - 1; p1++)
                    {
                        var d1_start = t1.DanhSachDiem[p1];
                        var d1_end = t1.DanhSachDiem[p1 + 1];

                        DateTime t1_vao = d1_start.GioDi;
                        DateTime t1_ra = d1_end.GioDen;

                        int ga1_A = Math.Min(d1_start.MaGa, d1_end.MaGa);
                        int ga1_B = Math.Max(d1_start.MaGa, d1_end.MaGa);

                        for (int p2 = 0; p2 < t2.DanhSachDiem.Count - 1; p2++)
                        {
                            var d2_start = t2.DanhSachDiem[p2];
                            var d2_end = t2.DanhSachDiem[p2 + 1];

                            DateTime t2_vao = d2_start.GioDi;
                            DateTime t2_ra = d2_end.GioDen;

                            int ga2_A = Math.Min(d2_start.MaGa, d2_end.MaGa);
                            int ga2_B = Math.Max(d2_start.MaGa, d2_end.MaGa);

                            // Kiểm tra xem 2 tàu có cùng đi qua chung 1 khu gian vật lý không
                            bool cungKhuGian = (ga1_A == ga2_A && ga1_B == ga2_B);

                            // Nếu cả 2 đều có lý trình và nằm trong khoảng không gian trùng nhau
                            decimal minKm1 = Math.Min(d1_start.LyTrinhKm, d1_end.LyTrinhKm);
                            decimal maxKm1 = Math.Max(d1_start.LyTrinhKm, d1_end.LyTrinhKm);
                            decimal minKm2 = Math.Min(d2_start.LyTrinhKm, d2_end.LyTrinhKm);
                            decimal maxKm2 = Math.Max(d2_start.LyTrinhKm, d2_end.LyTrinhKm);

                            decimal giaoMinKm = Math.Max(minKm1, minKm2);
                            decimal giaoMaxKm = Math.Min(maxKm1, maxKm2);

                            if (cungKhuGian || (giaoMaxKm - giaoMinKm > 5.0m))
                            {
                                // 1. Kiểm tra trường hợp NGƯỢC CHIỀU (Đối đầu trong khu gian đơn)
                                if (t1.HuongChay != t2.HuongChay)
                                {
                                    double dur1 = (t1_ra - t1_vao).TotalMinutes;
                                    double dur2 = (t2_ra - t2_vao).TotalMinutes;

                                    if (dur1 > 0 && dur2 > 0)
                                    {
                                        DateTime tRef = t1_vao < t2_vao ? t1_vao : t2_vao;
                                        double m1s = (t1_vao - tRef).TotalMinutes;
                                        double m1e = (t1_ra - tRef).TotalMinutes;
                                        double m2s = (t2_vao - tRef).TotalMinutes;
                                        double m2e = (t2_ra - tRef).TotalMinutes;

                                        double km1s = (double)d1_start.LyTrinhKm;
                                        double km1e = (double)d1_end.LyTrinhKm;
                                        double km2s = (double)d2_start.LyTrinhKm;
                                        double km2e = (double)d2_end.LyTrinhKm;

                                        double v1 = (km1e - km1s) / dur1;
                                        double v2 = (km2e - km2s) / dur2;

                                        if (Math.Abs(v1 - v2) > 1e-6)
                                        {
                                            // km1(m) = km1s + v1*(m - m1s)
                                            // km2(m) = km2s + v2*(m - m2s)
                                            // Giao điểm: m = ((km2s - v2*m2s) - (km1s - v1*m1s)) / (v1 - v2)
                                            double mGiao = ((km2s - v2 * m2s) - (km1s - v1 * m1s)) / (v1 - v2);

                                            // Kiểm tra giao điểm có nằm trong cả 2 phân đoạn hành trình đang xét không
                                            if (mGiao >= Math.Max(m1s, m2s) - 0.1 && mGiao <= Math.Min(m1e, m2e) + 0.1)
                                            {
                                                DateTime thoiDiemGiao = tRef.AddMinutes(mGiao);
                                                decimal kmGiao = (decimal)(km1s + v1 * (mGiao - m1s));

                                                var xd = TaoXungDot(maXungDot++, t1, t2, d1_start, d1_end, d2_start, d2_end, "DOI_DAU", thoiDiemGiao, kmGiao);
                                                dsXungDot.Add(xd);
                                            }
                                        }
                                    }
                                }
                                // 2. Kiểm tra trường hợp CÙNG CHIỀU (Giãn cách an toàn tối thiểu < 10 phút)
                                else if (cungKhuGian)
                                {
                                    double chenhLechVao = Math.Abs((t1_vao - t2_vao).TotalMinutes);
                                    double chenhLechRa = Math.Abs((t1_ra - t2_ra).TotalMinutes);

                                    if (chenhLechVao < 10 || chenhLechRa < 10)
                                    {
                                        DateTime thoiDiem = t1_vao < t2_vao ? t1_vao : t2_vao;
                                        decimal kmTrungDiem = (d1_start.LyTrinhKm + d1_end.LyTrinhKm) / 2m;
                                        var xd = TaoXungDot(maXungDot++, t1, t2, d1_start, d1_end, d2_start, d2_end, "GIAN_CACH", thoiDiem, kmTrungDiem);
                                        dsXungDot.Add(xd);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return dsXungDot;
        }

        private class PhuongAnTranh
        {
            public HanhTrinhTauDTO TauUuTien { get; set; } = null!;
            public HanhTrinhTauDTO TauBiTranh { get; set; } = null!;
            public int MaGaTranh { get; set; }
            public string TenGaTranh { get; set; } = "";
            public decimal LyTrinhGaTranh { get; set; }
            public DateTime GioDenGaTranh { get; set; }
            public DateTime GioDiGaTranh { get; set; }
            public int SoPhutDung { get; set; }
            public int SoPhutTang { get; set; }
            public int? MaRayTranh { get; set; }
            public int? SoHieuRay { get; set; }
            public double DiemChiPhi { get; set; }
        }

        private PhuongAnTranh DanhGiaPhuongAnTranh(
            HanhTrinhTauDTO tUuTien, HanhTrinhTauDTO tBiTranh,
            DiemChayTauDTO d_uuTien_start, DiemChayTauDTO d_uuTien_end,
            DiemChayTauDTO d_biTranh_start, DiemChayTauDTO d_biTranh_end,
            decimal kmXungDot, DataTable dtGaCoDuongTranh)
        {
            decimal chieuDaiYeuCau = tBiTranh.TongChieuDaiM > 0 ? tBiTranh.TongChieuDaiM : 200m;

            // Trong bài toán điều độ đường đơn (Single-Track Meet-Pass Problem):
            // Tàu bị tránh BẮT BUỘC phải dừng chờ tại ga bắt đầu phân đoạn chuẩn bị vào khu gian xung đột (d_biTranh_start).
            // Tuyệt đối không cho phép duyệt lùi về các ga trong quá khứ vì tàu đã đi qua từ trước.
            int currMaGa = d_biTranh_start.MaGa;
            string currTenGa = d_biTranh_start.TenGa;
            decimal currLyTrinh = d_biTranh_start.LyTrinhKm;

            // Thời điểm tàu bị tránh đến ga này (bảo toàn lịch đến)
            DateTime curGioDen = d_biTranh_start.GioDen;

            // Tính thời điểm tàu ưu tiên thông qua / giải phóng khu gian và đến ga này
            DateTime curGioTauUuTienQua;
            if (currMaGa == d_uuTien_end.MaGa)
            {
                curGioTauUuTienQua = d_uuTien_end.GioDen;
            }
            else if (currMaGa == d_uuTien_start.MaGa)
            {
                curGioTauUuTienQua = d_uuTien_start.GioDi;
            }
            else
            {
                double durUuTien = (d_uuTien_end.GioDen - d_uuTien_start.GioDi).TotalMinutes;
                double vUuTien = durUuTien > 0 ? (double)(d_uuTien_end.LyTrinhKm - d_uuTien_start.LyTrinhKm) / durUuTien : 1.0;
                double phutTuStartUuTien = Math.Abs(vUuTien) > 1e-5 ? (double)(currLyTrinh - d_uuTien_start.LyTrinhKm) / vUuTien : durUuTien;
                curGioTauUuTienQua = d_uuTien_start.GioDi.AddMinutes(Math.Max(0, phutTuStartUuTien));
            }

            // Tính thời điểm xuất phát mới sau khi tàu ưu tiên đã thoát khỏi khu gian + 5 phút đệm an toàn
            double diffMinutes = (curGioTauUuTienQua - curGioDen).TotalMinutes;
            DateTime curGioDi;

            if (diffMinutes >= 5 && diffMinutes <= 35)
            {
                // Khoảng thời gian tự nhiên: đỗ chờ tàu ưu tiên thông qua + 5 phút đệm
                curGioDi = curGioTauUuTienQua.AddMinutes(5);
            }
            else if (diffMinutes > 35)
            {
                // Chênh lệch tự nhiên quá lớn: khống chế giãn cách dời lịch chuẩn 20 phút kể từ giờ đi cũ
                curGioDi = d_biTranh_start.GioDi.AddMinutes(20);
            }
            else
            {
                // Tàu ưu tiên đã đi qua ga này trước khi tàu tránh đến -> lùi 15 phút an toàn
                curGioDi = d_biTranh_start.GioDi.AddMinutes(15);
            }

            // Đảm bảo giờ đi mới không sớm hơn giờ đi cũ và đỗ tối thiểu 15 phút tại ga
            if (curGioDi < d_biTranh_start.GioDi)
            {
                curGioDi = d_biTranh_start.GioDi.AddMinutes(15);
            }
            if ((curGioDi - curGioDen).TotalMinutes < 15)
            {
                curGioDi = curGioDen.AddMinutes(15);
            }

            int soPhutDung = (int)Math.Ceiling((curGioDi - curGioDen).TotalMinutes);
            int soPhutTang = Math.Max(0, (int)Math.Ceiling((curGioDi - d_biTranh_start.GioDi).TotalMinutes));
            if (soPhutTang == 0)
            {
                soPhutTang = 15;
                curGioDi = d_biTranh_start.GioDi.AddMinutes(soPhutTang);
                soPhutDung = (int)Math.Ceiling((curGioDi - curGioDen).TotalMinutes);
            }

            // Kiểm tra đường ray tránh khả dụng tại ga dừng tránh
            DataTable dtRayKhaDung = _repo.LayDuongTranhKhaDung(currMaGa, chieuDaiYeuCau, curGioDen, curGioDi, tBiTranh.MaChuyenTau);
            bool coRay = dtRayKhaDung.Rows.Count > 0;

            // Tính hàm chi phí (Cost Function) cho bài toán điều độ đường sắt đơn
            // Quy ước hệ thống: Mức 1 (Tối cao: SE1-SE4) -> Mức 4 (Tàu hàng)
            // Tàu ưu tiên càng cao (số MucUuTien càng nhỏ) mà bị trễ thì phạt càng nặng
            double trongSoPhatTre = (6 - tBiTranh.MucUuTien) * 10.0;
            double chiPhi = (soPhutTang * trongSoPhatTre);
            if (!coRay)
            {
                chiPhi += 50000.0; // Phạt cực nặng nếu ga không có đường ray tránh đủ chuẩn
            }
            if (diffMinutes > 35)
            {
                chiPhi += (diffMinutes - 35) * 5.0;
            }

            return new PhuongAnTranh
            {
                TauUuTien = tUuTien,
                TauBiTranh = tBiTranh,
                MaGaTranh = currMaGa,
                TenGaTranh = currTenGa,
                LyTrinhGaTranh = currLyTrinh,
                GioDenGaTranh = curGioDen,
                GioDiGaTranh = curGioDi,
                SoPhutDung = soPhutDung,
                SoPhutTang = soPhutTang,
                MaRayTranh = coRay ? Convert.ToInt32(dtRayKhaDung.Rows[0]["MaDuongRay"]) : null,
                SoHieuRay = coRay ? Convert.ToInt32(dtRayKhaDung.Rows[0]["SoHieuDuong"]) : null,
                DiemChiPhi = chiPhi
            };
        }

        private XungDotKhuGianDTO TaoXungDot(int id, HanhTrinhTauDTO t1, HanhTrinhTauDTO t2,
                                             DiemChayTauDTO d1_start, DiemChayTauDTO d1_end,
                                             DiemChayTauDTO d2_start, DiemChayTauDTO d2_end,
                                             string loaiXungDot, DateTime thoiDiem, decimal kmXungDot)
        {
            // Xác định khu gian vật lý thực tế trên tuyến đường sắt (Physical Block Section)
            DataTable dtTatCaGa = _repo.LayDanhSachGaTuyen();
            DataRow? gaTruoc = null;
            DataRow? gaSau = null;

            for (int k = 0; k < dtTatCaGa.Rows.Count - 1; k++)
            {
                decimal kmA = Convert.ToDecimal(dtTatCaGa.Rows[k]["LyTrinhKm"]);
                decimal kmB = Convert.ToDecimal(dtTatCaGa.Rows[k + 1]["LyTrinhKm"]);

                if (kmXungDot >= Math.Min(kmA, kmB) && kmXungDot <= Math.Max(kmA, kmB))
                {
                    gaTruoc = dtTatCaGa.Rows[k];
                    gaSau = dtTatCaGa.Rows[k + 1];
                    break;
                }
            }

            string tenGaDau = gaTruoc != null ? gaTruoc["TenGa"].ToString() ?? "" : d1_start.TenGa;
            decimal lyTrinhDau = gaTruoc != null ? Convert.ToDecimal(gaTruoc["LyTrinhKm"]) : Math.Min(d1_start.LyTrinhKm, d1_end.LyTrinhKm);
            int maGaDau = gaTruoc != null ? Convert.ToInt32(gaTruoc["MaGa"]) : Math.Min(d1_start.MaGa, d1_end.MaGa);

            string tenGaCuoi = gaSau != null ? gaSau["TenGa"].ToString() ?? "" : d1_end.TenGa;
            decimal lyTrinhCuoi = gaSau != null ? Convert.ToDecimal(gaSau["LyTrinhKm"]) : Math.Max(d1_start.LyTrinhKm, d1_end.LyTrinhKm);
            int maGaCuoi = gaSau != null ? Convert.ToInt32(gaSau["MaGa"]) : Math.Max(d1_start.MaGa, d1_end.MaGa);

            // Đánh giá 2 phương án tránh tàu đối ứng (Meet-Pass Alternative Evaluation)
            DataTable dtGaCoDuongTranh = _repo.LayTatCaGaCoDuongTranh(150m);

            var pa1 = DanhGiaPhuongAnTranh(t1, t2, d1_start, d1_end, d2_start, d2_end, kmXungDot, dtGaCoDuongTranh);
            var pa2 = DanhGiaPhuongAnTranh(t2, t1, d2_start, d2_end, d1_start, d1_end, kmXungDot, dtGaCoDuongTranh);

            PhuongAnTranh paChon;
            // Áp dụng Quy chuẩn Điều độ Đường sắt:
            // 1. Nếu 2 tàu khác cấp ưu tiên -> Tàu cấp thấp hơn (số MucUuTien lớn hơn) BẮT BUỘC phải dừng tránh nhường đường cho tàu cấp cao hơn
            if (t1.MucUuTien < t2.MucUuTien)
            {
                // t1 ưu tiên cao hơn t2 (ví dụ SE4 mức 1 vs NA1 mức 3) -> pa1 (t1 ưu tiên chạy suốt, t2 dừng nhường đường) được chọn
                paChon = (pa1.MaRayTranh.HasValue || !pa2.MaRayTranh.HasValue) ? pa1 : pa2;
            }
            else if (t2.MucUuTien < t1.MucUuTien)
            {
                // t2 ưu tiên cao hơn t1 -> pa2 (t2 ưu tiên chạy suốt, t1 dừng nhường đường) được chọn
                paChon = (pa2.MaRayTranh.HasValue || !pa1.MaRayTranh.HasValue) ? pa2 : pa1;
            }
            else
            {
                // 2. Cùng cấp ưu tiên (SE gặp SE, hoặc tàu chợ gặp tàu chợ) -> Chọn phương án có tổng chi phí / độ trễ nhỏ nhất
                paChon = (pa1.DiemChiPhi <= pa2.DiemChiPhi) ? pa1 : pa2;
            }

            string moTa = loaiXungDot == "DOI_DAU"
                ? $"Xung đột đối đầu: Tàu {t1.SoHieuMacTau} ({t1.HuongChay}) và {t2.SoHieuMacTau} ({t2.HuongChay}) đối đầu tại Km {kmXungDot:F1} (khu gian {tenGaDau} - {tenGaCuoi}) lúc {thoiDiem:HH:mm}."
                : $"Vi phạm giãn cách an toàn (<10 phút): Tàu {t1.SoHieuMacTau} và {t2.SoHieuMacTau} cùng chạy qua khu gian {tenGaDau} - {tenGaCuoi}.";

            return new XungDotKhuGianDTO
            {
                MaXungDot = id,
                MaChuyenTau1 = t1.MaChuyenTau,
                SoHieuMacTau1 = t1.SoHieuMacTau,
                LoaiTau1 = t1.LoaiTau,
                MucUuTien1 = t1.MucUuTien,

                MaChuyenTau2 = t2.MaChuyenTau,
                SoHieuMacTau2 = t2.SoHieuMacTau,
                LoaiTau2 = t2.LoaiTau,
                MucUuTien2 = t2.MucUuTien,

                LoaiXungDot = loaiXungDot,
                MaGaDau = maGaDau,
                TenGaDau = tenGaDau,
                LyTrinhDauKm = lyTrinhDau,

                MaGaCuoi = maGaCuoi,
                TenGaCuoi = tenGaCuoi,
                LyTrinhCuoiKm = lyTrinhCuoi,

                ThoiDiemXungDot = thoiDiem,
                LyTrinhUocTinhKm = kmXungDot,
                MoTaChiTiet = moTa,

                SoHieuTauUuTien = paChon.TauUuTien.SoHieuMacTau,
                MucUuTienTauUuTien = paChon.TauUuTien.MucUuTien,
                MaChuyenTauBiTranh = paChon.TauBiTranh.MaChuyenTau,
                SoHieuMacTauBiTranh = paChon.TauBiTranh.SoHieuMacTau,
                SoHieuTauBiTranh = paChon.TauBiTranh.SoHieuMacTau,
                MucUuTienTauBiTranh = paChon.TauBiTranh.MucUuTien,

                MaGaDeXuatTranh = paChon.MaGaTranh,
                TenGaDeXuatTranh = paChon.TenGaTranh,
                LyTrinhGaTranhKm = paChon.LyTrinhGaTranh,
                GioDenGaTranh = paChon.GioDenGaTranh,
                GioDiGaTranh = paChon.GioDiGaTranh,
                SoPhutDungDeXuat = paChon.SoPhutDung,
                SoPhutTangLichTrinh = paChon.SoPhutTang,
                MaDuongRayDeXuat = paChon.MaRayTranh,
                SoHieuDuongRayDeXuat = paChon.SoHieuRay
            };
        }

        /// <summary>
        /// Giải quyết riêng biệt một xung đột khu gian cụ thể khi người dùng bấm nút [Xếp Tránh]
        /// </summary>
        public bool GiaiQuyetMotXungDot(XungDotKhuGianDTO xd, out string baoCaoChiTiet)
        {
            string thongTinRay = xd.SoHieuDuongRayDeXuat.HasValue
                ? $"Đường tránh #{xd.SoHieuDuongRayDeXuat.Value}"
                : "Đường tránh chỉ định bởi Trực ban ga";

            string ghiChu = $"Điều độ tự động: Nhường đường cho tàu {xd.SoHieuTauUuTien}. Đỗ {thongTinRay} trong {xd.SoPhutDungDeXuat} phút (Đến {xd.GioDenGaTranh:HH:mm} - Đi {xd.GioDiGaTranh:HH:mm}).";

            int soPhutTang = xd.SoPhutTangLichTrinh > 0 ? xd.SoPhutTangLichTrinh : xd.SoPhutDungDeXuat;

            bool ok = _repo.CapNhatLichTranhTau(
                xd.MaChuyenTauBiTranh,
                xd.MaGaDeXuatTranh,
                xd.GioDenGaTranh,
                xd.GioDiGaTranh,
                xd.MaDuongRayDeXuat,
                soPhutTang,
                ghiChu
            );

            if (ok)
            {
                xd.DaGiaiQuyet = true;
                baoCaoChiTiet = $"Đã xếp tàu {xd.SoHieuTauBiTranh} dừng tránh tại Ga {xd.TenGaDeXuatTranh} ({thongTinRay}) từ {xd.GioDenGaTranh:HH:mm} đến {xd.GioDiGaTranh:HH:mm} ({xd.SoPhutDungDeXuat} phút).\n\nLịch trình các ga tiếp theo đã được tịnh tiến an toàn (+{soPhutTang} phút).";
                return true;
            }
            else
            {
                baoCaoChiTiet = $"Không thể cập nhật lịch cho tàu {xd.SoHieuTauBiTranh} tại Ga {xd.TenGaDeXuatTranh}.";
                return false;
            }
        }

        /// <summary>
        /// Thuật toán tự động giải quyết xung đột tránh tàu toàn tuyến theo giải thuật lặp tham lam (Iterative Greedy Meet-Pass Resolution)
        /// Mỗi vòng lặp giải quyết xung đột sớm nhất, cập nhật biểu đồ và kiểm tra lại trạng thái để chống xung đột dây chuyền.
        /// </summary>
        public bool TuDongGiaiQuyetTranhTau(DateTime ngay, out string baoCaoChiTiet)
        {
            var dsBanDau = KiemTraXungDotToanTuyen(ngay);
            if (dsBanDau.Count == 0)
            {
                baoCaoChiTiet = "Không phát hiện xung đột an toàn nào trong ngày được chọn. Toàn bộ lịch trình đều hợp lệ!";
                return true;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"    BÁO CÁO KẾT QUẢ TỰ ĐỘNG XẾP TRÁNH TÀU NGÀY {ngay:dd/MM/yyyy}    ");
            sb.AppendLine($"Số xung đột phát hiện ban đầu: {dsBanDau.Count}");
            sb.AppendLine("---------------------------------------------------------------");

            int thanhCong = 0;
            int maxVongLap = 15;
            int vong = 0;
            var daXuLy = new HashSet<string>();

            while (vong < maxVongLap)
            {
                var dsHienTai = KiemTraXungDotToanTuyen(ngay);
                if (dsHienTai.Count == 0) break;

                // Lấy xung đột xảy ra sớm nhất theo thời gian
                var xdSomNhat = dsHienTai.OrderBy(x => x.ThoiDiemXungDot).FirstOrDefault(x => !daXuLy.Contains($"{x.MaChuyenTauBiTranh}_{x.MaGaDeXuatTranh}"));
                if (xdSomNhat == null)
                {
                    xdSomNhat = dsHienTai.OrderBy(x => x.ThoiDiemXungDot).First();
                }

                string key = $"{xdSomNhat.MaChuyenTauBiTranh}_{xdSomNhat.MaGaDeXuatTranh}";
                daXuLy.Add(key);

                if (GiaiQuyetMotXungDot(xdSomNhat, out string thongBao))
                {
                    thanhCong++;
                    string thongTinRay = xdSomNhat.SoHieuDuongRayDeXuat.HasValue ? $"Đường #{xdSomNhat.SoHieuDuongRayDeXuat.Value}" : "Đường tránh phân bổ";
                    sb.AppendLine($" Bước {thanhCong}: Đã xếp tàu {xdSomNhat.SoHieuTauBiTranh} dừng tránh tại Ga {xdSomNhat.TenGaDeXuatTranh} ({thongTinRay}) đỗ {xdSomNhat.SoPhutDungDeXuat} phút (Đến {xdSomNhat.GioDenGaTranh:HH:mm} - Đi {xdSomNhat.GioDiGaTranh:HH:mm}).");
                }
                else
                {
                    sb.AppendLine($" Lỗi tại bước {thanhCong + 1}: {thongBao}");
                    break;
                }

                vong++;
            }

            sb.AppendLine("---------------------------------------------------------------");
            var dsConLai = KiemTraXungDotToanTuyen(ngay);
            if (dsConLai.Count == 0)
            {
                sb.AppendLine($" TẤT CẢ XUNG ĐỘT ĐÃ ĐƯỢC GIẢI QUYẾT TRIỆT ĐỂ! Tuyến đường sắt an toàn 100%.");
            }
            else
            {
                sb.AppendLine($" Đã giải quyết {thanhCong} điểm giao nhau. Còn {dsConLai.Count} điểm cần trực ban ga giám sát.");
            }
            baoCaoChiTiet = sb.ToString();

            return thanhCong > 0;
        }

        private static string ChonMauSacTheoLoaiTau(string soHieu, string loaiTau, string huongChay)
        {
            if (loaiTau == "TAU_HANG") return "#F59E0B"; // Màu vàng hổ phách tàu hàng
            if (loaiTau == "TAU_CHO") return "#10B981";  // Màu xanh lá tàu chợ

            // Tàu nhanh SE
            if (huongChay == "BAC_NAM")
            {
                return soHieu switch
                {
                    "SE1" => "#EF4444", // Đỏ tươi
                    "SE3" => "#F97316", // Đỏ cam
                    "SE19" => "#EC4899", // Hồng tím
                    _ => "#E11D48"
                };
            }
            else
            {
                return soHieu switch
                {
                    "SE2" => "#06B6D4", // Xanh ngọc Cyan
                    "SE4" => "#3B82F6", // Xanh dương
                    "SE20" => "#8B5CF6", // Tím
                    _ => "#0EA5E9"
                };
            }
        }
    }
}
