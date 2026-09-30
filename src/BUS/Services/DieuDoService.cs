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
                int mucUuTien = Convert.ToInt32(first["MucUuTien"]);
                decimal chieuDai = Convert.ToDecimal(first["TongChieuDaiM"]);
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
                    hanhTrinh.DanhSachDiem.Add(new DiemChayTauDTO
                    {
                        MaChuyenTau = maChuyenTau,
                        SoHieuMacTau = soHieu,
                        LoaiTau = loaiTau,
                        HuongChay = huongChay,
                        MucUuTien = mucUuTien,
                        ThuTuDung = Convert.ToInt32(r["ThuTuDung"]),
                        MaGa = Convert.ToInt32(r["MaGa"]),
                        TenGa = r["TenGa"].ToString() ?? "",
                        MaGaCode = r["MaGaCode"].ToString() ?? "",
                        LyTrinhKm = Convert.ToDecimal(r["LyTrinhKm"]),
                        GioDen = Convert.ToDateTime(r["GioDenKeHoach"]),
                        GioDi = Convert.ToDateTime(r["GioDiKeHoach"]),
                        ThoiGianDungPhut = (int)(Convert.ToDateTime(r["GioDiKeHoach"]) - Convert.ToDateTime(r["GioDenKeHoach"])).TotalMinutes,
                        LaDiemTranh = Convert.ToBoolean(r["LaDiemTranh"]),
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

        private XungDotKhuGianDTO TaoXungDot(int id, HanhTrinhTauDTO t1, HanhTrinhTauDTO t2,
                                             DiemChayTauDTO d1_start, DiemChayTauDTO d1_end,
                                             DiemChayTauDTO d2_start, DiemChayTauDTO d2_end,
                                             string loaiXungDot, DateTime thoiDiem, decimal kmXungDot)
        {
            // 1. Phân định quyền ưu tiên chạy tàu (Train Priority Hierarchy)
            bool t1UuTienHon = t1.MucUuTien < t2.MucUuTien;
            if (t1.MucUuTien == t2.MucUuTien)
            {
                // Cùng cấp ưu tiên: Tàu chiều Nam -> Bắc (mác chẵn SE2, SE4) quy chuẩn biểu đồ ưu tiên hơn
                if (t1.HuongChay != t2.HuongChay)
                {
                    t1UuTienHon = (t1.HuongChay == "NAM_BAC");
                }
                else
                {
                    t1UuTienHon = t1.GioXuatPhat <= t2.GioXuatPhat;
                }
            }

            var tauUuTien = t1UuTienHon ? t1 : t2;
            var tauBiTranh = t1UuTienHon ? t2 : t1;
            var d_uuTien_start = t1UuTienHon ? d1_start : d2_start;
            var d_uuTien_end = t1UuTienHon ? d1_end : d2_end;
            var d_biTranh_start = t1UuTienHon ? d2_start : d1_start;
            var d_biTranh_end = t1UuTienHon ? d2_end : d1_end;

            // 2. Xác định khu gian vật lý thực tế trên tuyến đường sắt (Physical Block Section)
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

            // 3. Thuật toán chọn ga tránh tối ưu (Nearest Feasible Siding Station)
            DataTable dtGaCoDuongTranh = _repo.LayTatCaGaCoDuongTranh(tauBiTranh.TongChieuDaiM);
            DataRow? gaTranhDuocChon = null;

            if (tauBiTranh.HuongChay == "BAC_NAM")
            {
                // Tàu Bắc -> Nam (Km tăng dần): Ga tránh phải nằm TRƯỚC điểm xung đột (LyTrinhKm <= kmXungDot)
                gaTranhDuocChon = dtGaCoDuongTranh.AsEnumerable()
                    .Where(r => Convert.ToDecimal(r["LyTrinhKm"]) <= kmXungDot)
                    .OrderByDescending(r => Convert.ToDecimal(r["LyTrinhKm"]))
                    .FirstOrDefault();
            }
            else
            {
                // Tàu Nam -> Bắc (Km giảm dần): Ga tránh phải nằm TRƯỚC điểm xung đột (LyTrinhKm >= kmXungDot)
                gaTranhDuocChon = dtGaCoDuongTranh.AsEnumerable()
                    .Where(r => Convert.ToDecimal(r["LyTrinhKm"]) >= kmXungDot)
                    .OrderBy(r => Convert.ToDecimal(r["LyTrinhKm"]))
                    .FirstOrDefault();
            }

            int maGaTranh;
            string tenGaTranh;
            decimal lyTrinhGaTranh;
            int? maRayTranh = null;
            int? soHieuRay = null;

            if (gaTranhDuocChon != null)
            {
                maGaTranh = Convert.ToInt32(gaTranhDuocChon["MaGa"]);
                tenGaTranh = gaTranhDuocChon["TenGa"].ToString() ?? "";
                lyTrinhGaTranh = Convert.ToDecimal(gaTranhDuocChon["LyTrinhKm"]);
                maRayTranh = Convert.ToInt32(gaTranhDuocChon["MaDuongRay"]);
                soHieuRay = Convert.ToInt32(gaTranhDuocChon["SoHieuDuong"]);
            }
            else
            {
                maGaTranh = d_biTranh_start.MaGa;
                tenGaTranh = d_biTranh_start.TenGa;
                lyTrinhGaTranh = d_biTranh_start.LyTrinhKm;
            }

            // 4. Tính toán thời gian dừng tránh chuẩn theo Lý Thuyết Chiếm Dụng Phân Đoạn (Blocking Time Theory)
            // Thời điểm tàu nhường đến ga tránh
            double durBiTranh = (d_biTranh_end.GioDen - d_biTranh_start.GioDi).TotalMinutes;
            double vBiTranh = durBiTranh > 0 ? (double)(d_biTranh_end.LyTrinhKm - d_biTranh_start.LyTrinhKm) / durBiTranh : 1.0;

            DateTime gioDenGaTranh;
            if (maGaTranh == d_biTranh_start.MaGa)
            {
                gioDenGaTranh = d_biTranh_start.GioDen;
            }
            else if (maGaTranh == d_biTranh_end.MaGa)
            {
                gioDenGaTranh = d_biTranh_end.GioDen;
            }
            else
            {
                double phutTuStart = Math.Abs(vBiTranh) > 1e-5 ? (double)(lyTrinhGaTranh - d_biTranh_start.LyTrinhKm) / vBiTranh : 0;
                gioDenGaTranh = d_biTranh_start.GioDi.AddMinutes(Math.Max(0, phutTuStart));
            }

            // Thời điểm tàu ưu tiên chạy qua ga tránh này
            double durUuTien = (d_uuTien_end.GioDen - d_uuTien_start.GioDi).TotalMinutes;
            double vUuTien = durUuTien > 0 ? (double)(d_uuTien_end.LyTrinhKm - d_uuTien_start.LyTrinhKm) / durUuTien : 1.0;

            DateTime gioTauUuTienQuaGaTranh;
            if (maGaTranh == d_uuTien_start.MaGa)
            {
                gioTauUuTienQuaGaTranh = d_uuTien_start.GioDi;
            }
            else if (maGaTranh == d_uuTien_end.MaGa)
            {
                gioTauUuTienQuaGaTranh = d_uuTien_end.GioDen;
            }
            else
            {
                double phutTuStartUuTien = Math.Abs(vUuTien) > 1e-5 ? (double)(lyTrinhGaTranh - d_uuTien_start.LyTrinhKm) / vUuTien : 0;
                gioTauUuTienQuaGaTranh = d_uuTien_start.GioDi.AddMinutes(Math.Max(0, phutTuStartUuTien));
            }

            // Tàu nhường xuất phát sau khi tàu ưu tiên qua ga + 5 phút đệm an toàn
            DateTime gioDiGaTranh = gioTauUuTienQuaGaTranh.AddMinutes(5);
            if ((gioDiGaTranh - gioDenGaTranh).TotalMinutes < 15)
            {
                gioDiGaTranh = gioDenGaTranh.AddMinutes(15);
            }

            int soPhutDung = (int)Math.Max(15, Math.Ceiling((gioDiGaTranh - gioDenGaTranh).TotalMinutes));

            // Tính số phút tịnh tiến cần thiết cho các ga phía sau (đảm bảo giờ đến ga sau luôn > giờ đi ga này)
            double distToNxt = Math.Abs((double)(d_biTranh_end.LyTrinhKm - lyTrinhGaTranh));
            double phutChayToNxt = Math.Abs(vBiTranh) > 1e-5 ? distToNxt / Math.Abs(vBiTranh) : 15.0;
            DateTime gioDenNxtMoi = gioDiGaTranh.AddMinutes(Math.Max(10, phutChayToNxt));
            int soPhutTang = (int)Math.Max(soPhutDung, Math.Ceiling((gioDenNxtMoi - d_biTranh_end.GioDen).TotalMinutes));
            if (soPhutTang < soPhutDung) soPhutTang = soPhutDung;

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

                SoHieuTauUuTien = tauUuTien.SoHieuMacTau,
                MucUuTienTauUuTien = tauUuTien.MucUuTien,
                MaChuyenTauBiTranh = tauBiTranh.MaChuyenTau,
                SoHieuMacTauBiTranh = tauBiTranh.SoHieuMacTau,
                SoHieuTauBiTranh = tauBiTranh.SoHieuMacTau,
                MucUuTienTauBiTranh = tauBiTranh.MucUuTien,

                MaGaDeXuatTranh = maGaTranh,
                TenGaDeXuatTranh = tenGaTranh,
                LyTrinhGaTranhKm = lyTrinhGaTranh,
                GioDenGaTranh = gioDenGaTranh,
                GioDiGaTranh = gioDiGaTranh,
                SoPhutDungDeXuat = soPhutDung,
                SoPhutTangLichTrinh = soPhutTang,
                MaDuongRayDeXuat = maRayTranh,
                SoHieuDuongRayDeXuat = soHieuRay
            };
        }

        /// <summary>
        /// Giải quyết riêng biệt một xung đột khu gian cụ thể khi người dùng bấm nút [Xếp Tránh]
        /// </summary>
        public bool GiaiQuyetMotXungDot(XungDotKhuGianDTO xd, out string baoCaoChiTiet)
        {
            string ghiChu = $"Điều độ tự động: Nhường đường cho tàu {xd.SoHieuTauUuTien}. Đỗ đường tránh số {xd.SoHieuDuongRayDeXuat ?? 2} trong {xd.SoPhutDungDeXuat} phút (Đến {xd.GioDenGaTranh:HH:mm} - Đi {xd.GioDiGaTranh:HH:mm}).";

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
                baoCaoChiTiet = $"Đã xếp tàu {xd.SoHieuTauBiTranh} dừng tránh tại Ga {xd.TenGaDeXuatTranh} (Đường tránh #{xd.SoHieuDuongRayDeXuat ?? 2}) từ {xd.GioDenGaTranh:HH:mm} đến {xd.GioDiGaTranh:HH:mm} ({xd.SoPhutDungDeXuat} phút).\n\nLịch trình các ga tiếp theo đã được tịnh tiến an toàn (+{soPhutTang} phút).";
                return true;
            }
            else
            {
                baoCaoChiTiet = $"Không thể cập nhật lịch cho tàu {xd.SoHieuTauBiTranh} tại Ga {xd.TenGaDeXuatTranh}.";
                return false;
            }
        }

        /// <summary>
        /// Thuật toán tự động giải quyết xung đột tránh tàu toàn tuyến (Auto-Conflict Resolution Engine)
        /// </summary>
        public bool TuDongGiaiQuyetTranhTau(DateTime ngay, out string baoCaoChiTiet)
        {
            var dsXungDot = KiemTraXungDotToanTuyen(ngay);
            if (dsXungDot.Count == 0)
            {
                baoCaoChiTiet = "Không phát hiện xung đột an toàn nào trong ngày được chọn. Toàn bộ lịch trình đều hợp lệ!";
                return true;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== BÁO CÁO KẾT QUẢ TỰ ĐỘNG XẾP TRÁNH TÀU NGÀY {ngay:dd/MM/yyyy} ===");
            sb.AppendLine($"Tổng số xung đột phát hiện: {dsXungDot.Count}");
            sb.AppendLine("---------------------------------------------------------------");

            int thanhCong = 0;

            foreach (var xd in dsXungDot)
            {
                if (GiaiQuyetMotXungDot(xd, out string thongBao))
                {
                    thanhCong++;
                    sb.AppendLine($"✔ Xung đột #{xd.MaXungDot}: Đã xếp tàu {xd.SoHieuTauBiTranh} dừng tránh tại Ga {xd.TenGaDeXuatTranh} (Đường #{xd.SoHieuDuongRayDeXuat ?? 2}).");
                }
                else
                {
                    sb.AppendLine($"❌ Xung đột #{xd.MaXungDot}: {thongBao}");
                }
            }

            sb.AppendLine("---------------------------------------------------------------");
            sb.AppendLine($"Hoàn thành giải quyết {thanhCong}/{dsXungDot.Count} xung đột. Biểu đồ chạy tàu đã được cập nhật mượt mà!");
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
