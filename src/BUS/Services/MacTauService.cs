using System;
using System.Data;
using DAL.Repositories;
using ET.VanHanh;

namespace BUS.Services
{
    public class MacTauService
    {
        private readonly MacTauRepository _repo;

        public MacTauService()
        {
            _repo = new MacTauRepository();
        }

        public MacTauService(MacTauRepository repo)
        {
            _repo = repo;
        }

        public DataTable LayDanhSach(bool? chiDangKhaiThac = null)
        {
            return _repo.LayDanhSach(chiDangKhaiThac);
        }

        public DataTable LayChiTiet(int maMacTau)
        {
            return _repo.LayChiTiet(maMacTau);
        }

        /// <summary>
        /// Tự động xác định hướng chạy dựa trên lý trình đường sắt:
        /// - Km ga đi < Km ga đến => Chạy vào phía Nam (BAC_NAM).
        /// - Km ga đi > Km ga đến => Chạy ra phía Bắc (NAM_BAC).
        /// </summary>
        public static string XacDinhHuongChay(decimal lyTrinhGaDi, decimal lyTrinhGaDen)
        {
            if (lyTrinhGaDi < lyTrinhGaDen) return "BAC_NAM";
            if (lyTrinhGaDi > lyTrinhGaDen) return "NAM_BAC";
            return "";
        }

        /// <summary>
        /// Kiểm tra toàn bộ ràng buộc nghiệp vụ của mác tàu mẫu
        /// </summary>
        public bool KiemTraHopLe(MacTauMau m, bool laThemMoi, out string loi)
        {
            loi = string.Empty;

            if (m == null)
            {
                loi = "Thông tin mác tàu không hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(m.SoHieuMacTau))
            {
                loi = "Số hiệu mác tàu không được để trống (ví dụ: SE1, NA1, LP3).";
                return false;
            }

            string soHieu = m.SoHieuMacTau.Trim().ToUpper();
            if (soHieu.Length < 2)
            {
                loi = "Số hiệu mác tàu phải có độ dài tối thiểu từ 2 ký tự trở lên (ví dụ: SE, NA1, LP3).";
                return false;
            }

            if (soHieu.Length > 10)
            {
                loi = "Số hiệu mác tàu không được vượt quá 10 ký tự (theo quy chuẩn mác tàu ĐSVN).";
                return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(soHieu, @"^[A-Z0-9\-]+$"))
            {
                loi = "Số hiệu mác tàu chỉ được chứa chữ cái tiếng Anh (A-Z), chữ số (0-9) và dấu gạch nối (-).\nKhông được chứa khoảng trắng hoặc ký tự đặc biệt!";
                return false;
            }

            // Kiểm tra trùng lặp số hiệu mác tàu
            if (_repo.KiemTraTrungSoHieu(soHieu, laThemMoi ? 0 : m.MaMacTau))
            {
                loi = $"Số hiệu mác tàu '{soHieu}' đã tồn tại trong hệ thống. Vui lòng chọn số hiệu khác.";
                return false;
            }

            // Kiểm tra loại tàu hợp lệ
            if (m.LoaiTau != "TAU_NHANH" && m.LoaiTau != "TAU_CHO" && m.LoaiTau != "TAU_HANG")
            {
                loi = "Loại tàu không hợp lệ (chỉ chấp nhận: Tàu Nhanh, Tàu Chợ, Tàu Hàng).";
                return false;
            }

            // Kiểm tra ga đi và ga đến
            if (m.MaGaDi <= 0 || m.MaGaDen <= 0)
            {
                loi = "Vui lòng chọn đầy đủ Ga xuất phát và Ga về đích.";
                return false;
            }

            if (m.MaGaDi == m.MaGaDen)
            {
                loi = "Ga xuất phát phải khác Ga về đích.";
                return false;
            }

            // Kiểm tra hướng chạy
            if (m.HuongChay != "BAC_NAM" && m.HuongChay != "NAM_BAC")
            {
                loi = "Hướng chạy không hợp lệ (chỉ chấp nhận: Bắc - Nam hoặc Nam - Bắc).";
                return false;
            }

            // Kiểm tra thời gian chuẩn: tối thiểu 0.2h (12 phút), tối đa 100.0h
            if (m.ThoiGianChuanGio <= 0)
            {
                loi = "Thời gian chạy chuẩn phải lớn hơn 0 giờ.";
                return false;
            }

            if (m.ThoiGianChuanGio < 0.2m)
            {
                loi = "Thời gian chạy chuẩn phải từ 0.2 giờ (12 phút) trở lên.";
                return false;
            }

            if (m.ThoiGianChuanGio > 100m)
            {
                loi = "Thời gian chạy chuẩn không vượt quá 100 giờ.";
                return false;
            }

            // Làm tròn chuẩn 1 chữ số thập phân
            m.ThoiGianChuanGio = Math.Round(m.ThoiGianChuanGio, 1, MidpointRounding.AwayFromZero);

            // Kiểm tra mức ưu tiên
            if (m.MucUuTien < 1 || m.MucUuTien > 5)
            {
                loi = "Mức ưu tiên đường ray phải từ 1 đến 5 (1 là ưu tiên cao nhất).";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tính toán thời gian chạy chuẩn gợi ý khoa học căn cứ vào Cự ly (km) và Loại hình đoàn tàu
        /// <summary>
        /// Gợi ý thời gian chuẩn (giờ) dựa trên cự ly (km) và vận tốc định mức lữ hành (km/h)
        /// được cấu hình linh hoạt trong hệ thống (bảng vanhanh.DinhMucVanToc theo chuẩn UIC/VNR).
        /// </summary>
        public static decimal TinhGioChuanGoiY(decimal cuLyKm, string loaiTau)
        {
            if (cuLyKm <= 0) return 1.0m;

            var (vanTocDinhMuc, _, _) = DinhMucVanTocService.LayThongSoDinhMuc(loaiTau);
            if (vanTocDinhMuc <= 0) vanTocDinhMuc = 50.0m;

            decimal tg = cuLyKm / vanTocDinhMuc;
            decimal rounded = Math.Round(tg, 1, MidpointRounding.AwayFromZero);
            if (rounded < 0.2m) rounded = 0.2m;
            if (rounded > 100.0m) rounded = 100.0m;

            return rounded;
        }

        /// <summary>
        /// Xác định tiền tố mác tàu (Prefix) hoàn toàn tự động dựa trên dữ liệu CSDL (Data-Driven),
        /// tuyệt đối KHÔNG hardcode tên địa phương hay tên ga trong mã nguồn.
        /// - Ưu tiên 1: Lấy tiền tố từ mác tàu đã có trên chính tuyến (Ga Đi - Ga Đến) trong CSDL.
        /// - Ưu tiên 2: Lấy tiền tố phổ biến nhất của các mác tàu cùng Loại Tàu trong CSDL.
        /// - Fallback: Trích xuất mã quy chuẩn trong ngoặc đơn từ bảng danh mục định mức loại tàu.
        /// </summary>
        public string LayTienToMacTauTuCSDL(DataTable? dtMacTau, string loaiTau, int maGaDi = 0, int maGaDen = 0)
        {
            if (dtMacTau != null && dtMacTau.Rows.Count > 0)
            {
                // 1. Nếu cùng tuyến (Ga Đi - Ga Đến) đã có mác tàu trong CSDL, ưu tiên tái sử dụng tiền tố đó
                if (maGaDi > 0 && maGaDen > 0)
                {
                    foreach (DataRow r in dtMacTau.Rows)
                    {
                        int gDi = r["MaGaDi"] != DBNull.Value ? Convert.ToInt32(r["MaGaDi"]) : 0;
                        int gDen = r["MaGaDen"] != DBNull.Value ? Convert.ToInt32(r["MaGaDen"]) : 0;
                        if ((gDi == maGaDi && gDen == maGaDen) || (gDi == maGaDen && gDen == maGaDi))
                        {
                            string s = r["SoHieuMacTau"]?.ToString()?.Trim() ?? "";
                            var m = System.Text.RegularExpressions.Regex.Match(s, @"^([A-Za-z]+)");
                            if (m.Success) return m.Groups[1].Value.ToUpperInvariant();
                        }
                    }
                }

                // 2. Tìm tiền tố xuất hiện phổ biến nhất của cùng Loại Tàu (loaiTau) trong CSDL
                var demTienTo = new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (DataRow r in dtMacTau.Rows)
                {
                    string loai = r["LoaiTau"]?.ToString() ?? "";
                    if (string.Equals(loai, loaiTau, StringComparison.OrdinalIgnoreCase))
                    {
                        string s = r["SoHieuMacTau"]?.ToString()?.Trim() ?? "";
                        var m = System.Text.RegularExpressions.Regex.Match(s, @"^([A-Za-z]+)");
                        if (m.Success)
                        {
                            string p = m.Groups[1].Value.ToUpperInvariant();
                            demTienTo[p] = demTienTo.TryGetValue(p, out int count) ? count + 1 : 1;
                        }
                    }
                }

                if (demTienTo.Count > 0)
                {
                    return demTienTo.OrderByDescending(x => x.Value).First().Key;
                }
            }

            // 3. Fallback: Trích xuất mã viết tắt trong ngoặc đơn từ bảng danh mục định mức loại tàu
            var (_, tenLoai, _) = DinhMucVanTocService.LayThongSoDinhMuc(loaiTau);
            var matchNgoac = System.Text.RegularExpressions.Regex.Match(tenLoai, @"\(([A-Za-z]+)");
            if (matchNgoac.Success)
            {
                return matchNgoac.Groups[1].Value.ToUpperInvariant();
            }

            return "SE";
        }

        /// <summary>
        /// Tự động sinh số hiệu mác tàu gợi ý chuẩn quy cách ĐSVN:
        /// - Tiền tố (Prefix) được lấy 100% tự động từ CSDL theo tuyến và loại tàu.
        /// - Hướng Bắc -> Nam: Chiều Lẻ (1, 3, 5, 7, 9...)
        /// - Hướng Nam -> Bắc: Chiều Chẵn (2, 4, 6, 8, 10...)
        /// Tự động tìm số nhỏ nhất chưa tồn tại trong hệ thống.
        /// </summary>
        public string SinhSoHieuMacTauGoiY(string loaiTau, string huongChay, int maGaDi = 0, int maGaDen = 0)
        {
            DataTable dt = _repo.LayDanhSach(null);
            var dsSoHieu = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    string s = r["SoHieuMacTau"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(s))
                    {
                        dsSoHieu.Add(s);
                    }
                }
            }

            // Lấy tiền tố hoàn toàn từ dữ liệu CSDL (Data-Driven), không hardcode tên địa danh
            string prefix = LayTienToMacTauTuCSDL(dt, loaiTau, maGaDi, maGaDen);

            // Hướng Bắc -> Nam: Chiều Lẻ (1, 3, 5, 7, 9...)
            // Hướng Nam -> Bắc: Chiều Chẵn (2, 4, 6, 8, 10...)
            bool laChieuLe = huongChay != "NAM_BAC";
            int num = laChieuLe ? 1 : 2;

            while (num < 1000)
            {
                string candidate = $"{prefix}{num}";
                if (!dsSoHieu.Contains(candidate))
                {
                    return candidate;
                }
                num += 2;
            }

            return $"{prefix}{(laChieuLe ? 99 : 98)}";
        }

        public bool Luu(MacTauMau m, bool laThemMoi, out string loi, string nguoiThucHien = "admin")
        {
            if (!KiemTraHopLe(m, laThemMoi, out loi))
            {
                return false;
            }

            try
            {
                if (laThemMoi)
                {
                    m.NguoiTao = string.IsNullOrWhiteSpace(nguoiThucHien) ? "admin" : nguoiThucHien;
                    int id = _repo.Them(m);
                    if (id > 0)
                    {
                        m.MaMacTau = id;
                        return true;
                    }
                    loi = "Không thể thêm mác tàu vào CSDL.";
                    return false;
                }
                else
                {
                    m.NguoiCapNhat = string.IsNullOrWhiteSpace(nguoiThucHien) ? "admin" : nguoiThucHien;
                    if (_repo.CapNhat(m))
                    {
                        return true;
                    }
                    loi = "Không thể cập nhật thông tin mác tàu.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                loi = $"Lỗi xử lý CSDL: {ex.Message}";
                return false;
            }
        }

        public bool Xoa(int maMacTau, out string loi)
        {
            loi = string.Empty;
            if (maMacTau <= 0)
            {
                loi = "Mã mác tàu không hợp lệ.";
                return false;
            }

            try
            {
                int soChuyen = _repo.DemSoChuyenTauLienQuan(maMacTau);
                if (soChuyen > 0)
                {
                    loi = $"Mác tàu này đã có {soChuyen} chuyến tàu được lập trong hệ thống.\n" +
                          "Không thể xóa cứng để bảo vệ toàn vẹn lịch sử chạy tàu và vé khách.\n" +
                          "Hệ thống khuyến nghị: Chuyển mác tàu sang trạng thái 'Ngừng Khai Thác'.";
                    return false;
                }

                if (_repo.Xoa(maMacTau))
                {
                    return true;
                }

                loi = "Không thể xóa mác tàu khỏi CSDL.";
                return false;
            }
            catch (Exception ex)
            {
                loi = $"Lỗi xử lý xóa mác tàu: {ex.Message}";
                return false;
            }
        }

        public bool DoiTrangThaiKhaiThac(int maMacTau, bool dangKhaiThac, out string loi, string nguoiThucHien = "admin")
        {
            loi = string.Empty;
            if (maMacTau <= 0)
            {
                loi = "Mã mác tàu không hợp lệ.";
                return false;
            }

            try
            {
                if (_repo.DoiTrangThaiKhaiThac(maMacTau, dangKhaiThac, nguoiThucHien))
                {
                    return true;
                }

                loi = "Không thể cập nhật trạng thái khai thác mác tàu.";
                return false;
            }
            catch (Exception ex)
            {
                loi = $"Lỗi cập nhật trạng thái: {ex.Message}";
                return false;
            }
        }

        public DataTable LayDanhSachChuyenTauKeHoachTuongLai(int maMacTau)
        {
            if (maMacTau <= 0) return new DataTable();
            return _repo.LayDanhSachChuyenTauKeHoachTuongLai(maMacTau);
        }

        public bool TamDungMacTauVaXuLyChuyen(int maMacTau, bool huyChuyenTuongLai, string lyDoHuy, out int soChuyenDaHuy, out string loi, string nguoiThucHien = "admin")
        {
            loi = string.Empty;
            soChuyenDaHuy = 0;
            if (maMacTau <= 0)
            {
                loi = "Mã mác tàu không hợp lệ.";
                return false;
            }

            try
            {
                return _repo.TamDungMacTauVaXuLyChuyen(maMacTau, huyChuyenTuongLai, lyDoHuy, nguoiThucHien, out soChuyenDaHuy);
            }
            catch (Exception ex)
            {
                loi = $"Lỗi xử lý tạm dừng mác tàu: {ex.Message}";
                return false;
            }
        }
    }
}
