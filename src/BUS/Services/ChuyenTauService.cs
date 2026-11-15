using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using DAL.Repositories;
using ET.VanHanh;

namespace BUS.Services
{
    public class ChuyenTauService
    {
        // Quy chuẩn kỹ thuật quốc gia về khai thác đường sắt QCVN 08:2018/BGTVT (Chiều dài đoàn tàu tối đa không quá chiều dài hữu dụng đường tránh 450m)
        public const decimal CHIEU_DAI_TOI_DA_DOAN_TAU_M = 450.0m;

        private readonly ChuyenTauRepository _repo = new();

        public DataTable LayDanhSach(DateTime? ngay = null, int? maMacTau = null, string? trangThai = null)
        {
            return _repo.LayDanhSach(ngay, maMacTau, trangThai);
        }

        public DataTable LayDanhSachMacTau() => _repo.LayDanhSachMacTau();

        public DataTable LayLichDungGa(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayLichDungGa(maChuyenTau);
        }

        public DataTable LayLichDungGaMauTheoMac(int maMacTau, DateTime ngayXuatPhat, TimeSpan gioXuatPhat)
        {
            if (maMacTau <= 0) return new DataTable();
            return _repo.LayLichDungGaMauTheoMac(maMacTau, ngayXuatPhat, gioXuatPhat);
        }

        public DataTable LayDanhSachToaXe(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayDanhSachToaXe(maChuyenTau);
        }

        public DataTable LayDanhSachToaXeTheoDauMay(int maDauMay)
        {
            if (maDauMay <= 0) return new DataTable();
            return _repo.LayDanhSachToaXeTheoDauMay(maDauMay);
        }

        public bool KiemTraTrungLich(int maMacTau, DateTime ngayXuatPhat, int excludeMaChuyen = 0)
        {
            if (maMacTau <= 0) return false;
            return _repo.KiemTraTrungLich(maMacTau, ngayXuatPhat, excludeMaChuyen);
        }

        private static readonly Dictionary<string, HashSet<string>> HopLeChuyenTrangThai = new()
        {
            ["DA_LEN_LICH"] = new() { "DA_LEN_LICH", "SAN_SANG", "DA_HUY" },
            ["SAN_SANG"] = new() { "SAN_SANG", "DANG_CHAY", "DA_LEN_LICH", "DA_HUY" },
            ["DANG_CHAY"] = new() { "DANG_CHAY", "HOAN_THANH", "TRE_GIO", "DA_HUY" },
            ["TRE_GIO"] = new() { "TRE_GIO", "DANG_CHAY", "HOAN_THANH", "DA_HUY" },
            ["HOAN_THANH"] = new() { "HOAN_THANH" },
            ["DA_HUY"] = new() { "DA_HUY" }
        };

        public static bool KiemTraChuyenTrangThai(string trangThaiHienTai, string trangThaiMoi, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            if (string.IsNullOrEmpty(trangThaiHienTai) || trangThaiHienTai == trangThaiMoi) return true;

            if (trangThaiHienTai == "HOAN_THANH" || trangThaiHienTai == "DA_HUY")
            {
                thongBaoLoi = $"Chuyến tàu đã ở trạng thái kết thúc ({trangThaiHienTai}), không thể thay đổi trạng thái!";
                return false;
            }

            if (HopLeChuyenTrangThai.TryGetValue(trangThaiHienTai, out var allowed) && allowed.Contains(trangThaiMoi))
            {
                return true;
            }

            thongBaoLoi = $"Quy trình vận hành không cho phép chuyển trạng thái từ [{trangThaiHienTai}] sang [{trangThaiMoi}]!";
            return false;
        }

        public int Them(ChuyenTau ct, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu", out thongBaoLoi)) return 0;
            thongBaoLoi = string.Empty;
            if (ct.MaMacTau <= 0)
            {
                thongBaoLoi = "Mã mác tàu không hợp lệ!";
                return 0;
            }
            if (ct.NgayXuatPhat.Date < DateTime.Today)
            {
                thongBaoLoi = "Ngày xuất phát không được ở trong quá khứ!";
                return 0;
            }
            if (ct.GioVeDichKH <= ct.GioXuatPhatKH)
            {
                thongBaoLoi = "Giờ về đích dự kiến phải sau giờ xuất phát!";
                return 0;
            }
            if ((ct.GioVeDichKH - ct.GioXuatPhatKH).TotalMinutes < 30)
            {
                thongBaoLoi = "Thời gian chạy kế hoạch của chuyến tàu tối thiểu phải từ 30 phút trở lên!";
                return 0;
            }
            if ((ct.GioVeDichKH - ct.GioXuatPhatKH).TotalDays > 7)
            {
                thongBaoLoi = "Thời gian chạy kế hoạch của một chuyến tàu không được vượt quá 7 ngày!";
                return 0;
            }
            if (_repo.KiemTraTrungLich(ct.MaMacTau, ct.NgayXuatPhat))
            {
                thongBaoLoi = $"Mác tàu này đã được lập lịch cho ngày {ct.NgayXuatPhat:dd/MM/yyyy}!";
                return 0;
            }
            return _repo.Them(ct);
        }

        public int Them(ChuyenTau ct) => Them(ct, out _);

        public int LapChuyenTauTronGoi(ChuyenTau ct, DataTable dtLichDung, DoanTau? dt, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            if (ct.MaMacTau <= 0)
            {
                thongBaoLoi = "Mã mác tàu không hợp lệ!";
                return 0;
            }
            if (ct.NgayXuatPhat.Date < DateTime.Today)
            {
                thongBaoLoi = "Ngày xuất phát không được ở trong quá khứ!";
                return 0;
            }
            if (ct.GioVeDichKH <= ct.GioXuatPhatKH)
            {
                thongBaoLoi = "Giờ về đích dự kiến phải sau giờ xuất phát!";
                return 0;
            }
            if ((ct.GioVeDichKH - ct.GioXuatPhatKH).TotalMinutes < 30)
            {
                thongBaoLoi = "Thời gian chạy kế hoạch của chuyến tàu tối thiểu phải từ 30 phút trở lên!";
                return 0;
            }
            if (_repo.KiemTraTrungLich(ct.MaMacTau, ct.NgayXuatPhat))
            {
                thongBaoLoi = $"Mác tàu này đã được lập lịch cho ngày {ct.NgayXuatPhat:dd/MM/yyyy}!";
                return 0;
            }
            if (dtLichDung == null || dtLichDung.Rows.Count < 2)
            {
                thongBaoLoi = "Chuyến tàu phải có tối thiểu 2 điểm dừng (ga xuất phát và ga về đích)!";
                return 0;
            }

            return _repo.LapChuyenTauTronGoi(ct, dtLichDung, dt, out thongBaoLoi);
        }

        public bool CapNhat(ChuyenTau ct, out string thongBaoLoi)
        {
            byte[]? phienBan = null;
            return CapNhat(ct, ref phienBan, out thongBaoLoi);
        }

        // [13/11 Dat] phienBan = ChuyenTau.PhienBan luc man hinh nap (null = khong kiem, chi cham)
        public bool CapNhat(ChuyenTau ct, ref byte[]? phienBan, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu", out thongBaoLoi)) return false;
            thongBaoLoi = string.Empty;
            if (ct.MaChuyenTau <= 0 || ct.MaMacTau <= 0)
            {
                thongBaoLoi = "Dữ liệu chuyến tàu không hợp lệ!";
                return false;
            }
            if (ct.GioVeDichKH <= ct.GioXuatPhatKH)
            {
                thongBaoLoi = "Giờ về đích dự kiến phải sau giờ xuất phát!";
                return false;
            }
            if ((ct.GioVeDichKH - ct.GioXuatPhatKH).TotalMinutes < 30)
            {
                thongBaoLoi = "Thời gian chạy kế hoạch của chuyến tàu tối thiểu phải từ 30 phút trở lên!";
                return false;
            }
            if ((ct.GioVeDichKH - ct.GioXuatPhatKH).TotalDays > 7)
            {
                thongBaoLoi = "Thời gian chạy kế hoạch của một chuyến tàu không được vượt quá 7 ngày!";
                return false;
            }
            // [13/11] Kiem tra doc CSDL + ghi chay trong khoa chuyen, sau khi so PhienBan
            return TrenChuyen(ct.MaChuyenTau, ref phienBan,
                $"Lưu thông tin chuyến (giờ đi {ct.GioXuatPhatKH:HH:mm dd/MM}, giờ đến {ct.GioVeDichKH:HH:mm dd/MM})", () =>
            {
                if (_repo.KiemTraTrungLich(ct.MaMacTau, ct.NgayXuatPhat, ct.MaChuyenTau))
                    return $"Mác tàu này đã được lập lịch cho ngày {ct.NgayXuatPhat:dd/MM/yyyy} trên một chuyến khác!";
                // [Đạt 30/10] Sang Sẵn sàng / Đang chạy phải qua kiểm tra xuất bến (hợp đồng §7.2)
                string? lyDoChanXuatBen = LyDoChanXuatBen(ct.MaChuyenTau, _repo.LayTrangThai(ct.MaChuyenTau), ct.TrangThai);
                if (lyDoChanXuatBen != null) return lyDoChanXuatBen;
                return _repo.CapNhat(ct) > 0 ? null : "Không thể cập nhật chuyến tàu vào CSDL.";
            }, out thongBaoLoi);
        }

        public bool CapNhat(ChuyenTau ct) => CapNhat(ct, out _);

        public bool CapNhatThongSoKeHoach(int maChuyenTau, int maMacTau, DateTime ngayXuatPhat, DateTime gioXuatPhatKH, DateTime gioVeDichKH, out string thongBaoLoi)
        {
            byte[]? phienBan = null;
            return CapNhatThongSoKeHoach(maChuyenTau, maMacTau, ngayXuatPhat, gioXuatPhatKH, gioVeDichKH, ref phienBan, out thongBaoLoi);
        }

        public bool CapNhatThongSoKeHoach(int maChuyenTau, int maMacTau, DateTime ngayXuatPhat, DateTime gioXuatPhatKH, DateTime gioVeDichKH,
                                          ref byte[]? phienBan, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu", out thongBaoLoi)) return false;
            thongBaoLoi = string.Empty;
            if (maChuyenTau <= 0 || maMacTau <= 0)
            {
                thongBaoLoi = "Dữ liệu chuyến tàu không hợp lệ!";
                return false;
            }
            if (gioVeDichKH <= gioXuatPhatKH)
            {
                thongBaoLoi = "Giờ về đích dự kiến phải sau giờ xuất phát!";
                return false;
            }
            if ((gioVeDichKH - gioXuatPhatKH).TotalMinutes < 30)
            {
                thongBaoLoi = "Thời gian chạy kế hoạch của chuyến tàu tối thiểu phải từ 30 phút trở lên!";
                return false;
            }
            return TrenChuyen(maChuyenTau, ref phienBan,
                $"Lưu thông số kế hoạch (giờ đi {gioXuatPhatKH:HH:mm dd/MM}, giờ đến {gioVeDichKH:HH:mm dd/MM})", () =>
            {
                if (_repo.KiemTraTrungLich(maMacTau, ngayXuatPhat, maChuyenTau))
                    return $"Mác tàu này đã được lập lịch cho ngày {ngayXuatPhat:dd/MM/yyyy} trên một chuyến khác!";
                return _repo.CapNhatThongSoKeHoach(maChuyenTau, maMacTau, ngayXuatPhat, gioXuatPhatKH, gioVeDichKH) > 0
                    ? null : "Không thể cập nhật thông số kế hoạch chuyến tàu vào CSDL.";
            }, out thongBaoLoi);
        }

        public bool CapNhatThongSoKeHoach(int maChuyenTau, int maMacTau, DateTime ngayXuatPhat, DateTime gioXuatPhatKH, DateTime gioVeDichKH)
            => CapNhatThongSoKeHoach(maChuyenTau, maMacTau, ngayXuatPhat, gioXuatPhatKH, gioVeDichKH, out _);

        public bool CapNhatTrangThai(int maChuyenTau, string trangThai)
        {
            PhanQuyenService.BatBuocCoQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu"); // [Sprint 3] thieu quyen -> KhongDuQuyenException
            if (maChuyenTau <= 0) return false;
            if (LyDoChanXuatBen(maChuyenTau, _repo.LayTrangThai(maChuyenTau), trangThai) != null) return false;   // [Đạt 30/10]
            return _repo.CapNhatTrangThai(maChuyenTau, trangThai) > 0;
        }

        // ===== [Đạt 30/10] Kiểm tra xuất bến — hợp đồng §7.2 KE_HOACH_BACK_END =====
        // Chuyến đang Đã lên lịch / Sẵn sàng mà chuyển sang Sẵn sàng / Đang chạy thì phải qua KiemTraXuatBenService:
        // biên bản khám xe đạt (lập sau lần sửa đoàn tàu gần nhất), đã duyệt an toàn ở Bảo trì, đầu máy không quá hạn
        // bảo dưỡng, chặng đầu có kíp lái đủ điều kiện. Chặn cứng, không có nút vượt (Q11).
        public static bool CanKiemTraXuatBen(string trangThaiCu, string trangThaiMoi)
            => trangThaiMoi != trangThaiCu
               && (trangThaiMoi is "SAN_SANG" or "DANG_CHAY")
               && (trangThaiCu is "DA_LEN_LICH" or "SAN_SANG");

        // null = được chuyển; khác null = lý do bị chặn (nhiều dòng, đọc được ngay trên hộp thông báo)
        public string? LyDoChanXuatBen(int maChuyenTau, string trangThaiCu, string trangThaiMoi)
        {
            if (!CanKiemTraXuatBen(trangThaiCu, trangThaiMoi)) return null;
            var kq = new KiemTraXuatBenService().KiemTra(maChuyenTau);
            return kq.DuocXuatBen ? null : kq.MoTaChan;
        }

        // Các trạng thái được chuyển tới từ trạng thái hiện tại (theo HopLeChuyenTrangThai), trừ chính nó
        public static List<string> LayTrangThaiKeTiep(string trangThaiHienTai)
            => HopLeChuyenTrangThai.TryGetValue(trangThaiHienTai, out var ds)
                ? ds.Where(t => t != trangThaiHienTai).ToList()
                : new List<string>();

        // Đổi riêng trạng thái (nút "Chuyển Trạng Thái" ở Lịch trình): đọc trạng thái hiện tại trong CSDL,
        // kiểm quy trình chuyển + kiểm tra xuất bến rồi mới ghi.
        // [Đạt 09/11] Chỉ người có quyền Lập biểu đồ chạy tàu (TRAIN_SCHEDULE_EDIT) mới được đổi; ghi trạng thái + nhật ký
        // hệ thống DOI_TRANG_THAI_CHUYEN trong cùng một giao dịch (trước đây ai có quyền xem lịch trình cũng đổi được, không để lại vết).
        public const string MaQuyenDoiTrangThai = DTO.Common.MaQuyen.LapBieuDoChayTau;

        public bool DoiTrangThai(int maChuyenTau, string trangThaiMoi, out string thongBaoLoi)
        {
            byte[]? phienBan = null;
            return DoiTrangThai(maChuyenTau, trangThaiMoi, ref phienBan, out thongBaoLoi);
        }

        // [13/11] Chay trong khoa chuyen + so PhienBan (chong ghi de ca chuyen)
        public bool DoiTrangThai(int maChuyenTau, string trangThaiMoi, ref byte[]? phienBan, out string thongBaoLoi)
        {
            if (!PhanQuyenService.KiemTraQuyen(MaQuyenDoiTrangThai, "đổi trạng thái chuyến tàu", out thongBaoLoi)) return false;
            return TrenChuyen(maChuyenTau, ref phienBan, "", () =>
                DoiTrangThaiTrongKhoa(maChuyenTau, trangThaiMoi, out string loi) ? null : loi,
                out thongBaoLoi, ghiNhatKy: false);
        }

        private bool DoiTrangThaiTrongKhoa(int maChuyenTau, string trangThaiMoi, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            string trangThaiCu = _repo.LayTrangThai(maChuyenTau);
            if (string.IsNullOrEmpty(trangThaiCu))
            {
                thongBaoLoi = "Không tìm thấy chuyến tàu trong CSDL!";
                return false;
            }
            if (!KiemTraChuyenTrangThai(trangThaiCu, trangThaiMoi, out thongBaoLoi)) return false;

            string? lyDo = LyDoChanXuatBen(maChuyenTau, trangThaiCu, trangThaiMoi);
            if (lyDo != null)
            {
                thongBaoLoi = lyDo;
                return false;
            }
            try
            {
                using var gd = DAL.Connection.PhienGiaoDich.Mo();
                if (new LapTauRepository().DoiTrangThaiChuyen(maChuyenTau, trangThaiCu, trangThaiMoi, gd) == 0)
                {
                    thongBaoLoi = "Trạng thái chuyến vừa được người khác thay đổi. Hãy nạp lại danh sách rồi thử lại.";
                    return false;
                }
                gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "DOI_TRANG_THAI_CHUYEN", "vanhanh.ChuyenTau",
                    maChuyenTau.ToString(),
                    PhanQuyenService.Json(new { TrangThai = trangThaiCu }),
                    PhanQuyenService.Json(new { TrangThai = trangThaiMoi }));
                gd.XacNhan();
                return true;
            }
            catch (Exception ex) when (DichLoiSqlPhuongTien.LaLoiCsdl(ex))
            {
                thongBaoLoi = "Không thể cập nhật trạng thái chuyến tàu vào CSDL: " + DichLoiSqlPhuongTien.ThongDiep(ex);
                return false;
            }
        }

        public bool CapNhatGioThucTe(int maDiemDung, DateTime? gioDenTT, DateTime? gioDiTT)
        {
            PhanQuyenService.BatBuocCoQuyen(DTO.Common.MaQuyen.GhiNhanSuCoChamGio, "ghi giờ chạy thực tế"); // [Sprint 3] thieu quyen -> KhongDuQuyenException
            if (maDiemDung <= 0) return false;
            bool ok = _repo.CapNhatGioThucTe(maDiemDung, gioDenTT, gioDiTT) > 0;
            if (ok)
            {
                // [12/11] Tre qua nguong tai ga moi nhat -> chuyen tu dat "Tre gio" (va nguoc lai khi da bat kip), ghi nhat ky
                var doi = _repo.CapNhatTrangThaiTheoGioThucTe(maDiemDung, SoPhutTreToiDaDungGio);
                if (doi is { } d)
                {
                    try
                    {
                        using var gd = DAL.Connection.PhienGiaoDich.Mo();
                        gd.GhiNhatKyHeThong(PhanQuyenService.MaTaiKhoanHienTai, "DOI_TRANG_THAI_CHUYEN", "vanhanh.ChuyenTau",
                            d.MaChuyenTau.ToString(), PhanQuyenService.Json(new { TrangThai = d.Cu }),
                            PhanQuyenService.Json(new { TrangThai = d.Moi, LyDo = "Tự động theo giờ chạy thực tế" }));
                        gd.XacNhan();
                    }
                    catch (Exception) { /* nhat ky khong chan thao tac chinh */ }
                }
            }
            return ok;
        }

        // Dung gio: tre <= 5 phut (cung nguong voi bao cao ty le dung gio - baocao.vw_DungGioChuyen)
        public const int SoPhutTreToiDaDungGio = 5;

        public bool ThemLichDungGa(LichDungGa ld, out string thongBaoLoi)
        {
            byte[]? phienBan = null;
            return ThemLichDungGa(ld, ref phienBan, out thongBaoLoi);
        }

        public bool ThemLichDungGa(LichDungGa ld, ref byte[]? phienBan, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu", out thongBaoLoi)) return false;
            thongBaoLoi = string.Empty;
            if (ld.MaChuyenTau <= 0 || ld.MaGa <= 0 || ld.ThuTuDung <= 0)
            {
                thongBaoLoi = "Dữ liệu điểm dừng ga không hợp lệ!";
                return false;
            }
            if (ld.GioDiKeHoach < ld.GioDenKeHoach)
            {
                thongBaoLoi = "Giờ đi kế hoạch không được trước giờ đến kế hoạch tại cùng một ga dừng!";
                return false;
            }
            if ((ld.GioDiKeHoach - ld.GioDenKeHoach).TotalHours > 12.0)
            {
                thongBaoLoi = "Thời gian dừng tác nghiệp tại một ga không được vượt quá 12 giờ!";
                return false;
            }

            return TrenChuyen(ld.MaChuyenTau, ref phienBan, $"Thêm điểm dừng thứ {ld.ThuTuDung} ({TenGa(ld.MaGa)})", () =>
                ThemLichDungGaTrongKhoa(ld, out string loi) ? null : loi, out thongBaoLoi);
        }

        private bool ThemLichDungGaTrongKhoa(LichDungGa ld, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            // Kiểm tra tính đơn điệu thời gian và không trùng thứ tự/ga với các điểm dừng đã có
            var dtLich = _repo.LayLichDungGa(ld.MaChuyenTau);
            foreach (DataRow r in dtLich.Rows)
            {
                int maGaExist = Convert.ToInt32(r["MaGa"]);
                int thuTuExist = Convert.ToInt32(r["ThuTuDung"]);
                string tenGaExist = r["TenGa"]?.ToString() ?? "";

                if (thuTuExist == ld.ThuTuDung)
                {
                    thongBaoLoi = $"Thứ tự dừng #{ld.ThuTuDung} đã tồn tại tại ga {tenGaExist}!";
                    return false;
                }
                if (maGaExist == ld.MaGa)
                {
                    thongBaoLoi = $"Ga [{tenGaExist}] đã có trong lộ trình của chuyến tàu này!";
                    return false;
                }

                DateTime denExist = Convert.ToDateTime(r["GioDenKeHoach"]);
                DateTime diExist = Convert.ToDateTime(r["GioDiKeHoach"]);

                // Ga trước phải đi trước khi ga sau đến
                if (thuTuExist < ld.ThuTuDung && diExist >= ld.GioDenKeHoach)
                {
                    thongBaoLoi = $"Giờ đến tại ga mới ({ld.GioDenKeHoach:HH:mm dd/MM}) phải sau giờ rời ga #{thuTuExist} ({tenGaExist} lúc {diExist:HH:mm dd/MM})!";
                    return false;
                }
                if (thuTuExist > ld.ThuTuDung && denExist <= ld.GioDiKeHoach)
                {
                    thongBaoLoi = $"Giờ rời ga mới ({ld.GioDiKeHoach:HH:mm dd/MM}) phải trước giờ đến của ga #{thuTuExist} ({tenGaExist} lúc {denExist:HH:mm dd/MM})!";
                    return false;
                }
            }

            try
            {
                bool kq = _repo.ThemLichDungGa(ld) > 0;
                if (!kq) thongBaoLoi = "Không thể ghi điểm dừng ga vào cơ sở dữ liệu.";
                return kq;
            }
            catch (SqlException ex)
            {
                thongBaoLoi = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                thongBaoLoi = $"Lỗi thêm điểm dừng: {ex.Message}";
                return false;
            }
        }

        public bool ThemLichDungGa(LichDungGa ld) => ThemLichDungGa(ld, out _);

        public bool CapNhatLichDungGa(LichDungGa ld, out string thongBaoLoi)
        {
            byte[]? phienBan = null;
            return CapNhatLichDungGa(ld, ref phienBan, out thongBaoLoi);
        }

        public bool CapNhatLichDungGa(LichDungGa ld, ref byte[]? phienBan, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu", out thongBaoLoi)) return false;
            thongBaoLoi = string.Empty;
            if (ld.MaDiemDung <= 0 || ld.MaChuyenTau <= 0)
            {
                thongBaoLoi = "Dữ liệu điểm dừng ga không hợp lệ!";
                return false;
            }
            if (ld.GioDiKeHoach < ld.GioDenKeHoach)
            {
                thongBaoLoi = "Giờ đi kế hoạch không được trước giờ đến kế hoạch tại cùng một ga dừng!";
                return false;
            }
            if ((ld.GioDiKeHoach - ld.GioDenKeHoach).TotalHours > 12.0)
            {
                thongBaoLoi = "Thời gian dừng tác nghiệp tại một ga không được vượt quá 12 giờ!";
                return false;
            }

            return TrenChuyen(ld.MaChuyenTau, ref phienBan,
                $"Sửa giờ điểm dừng thứ {ld.ThuTuDung} (đến {ld.GioDenKeHoach:HH:mm}, đi {ld.GioDiKeHoach:HH:mm})", () =>
                CapNhatLichDungGaTrongKhoa(ld, out string loi) ? null : loi, out thongBaoLoi);
        }

        private bool CapNhatLichDungGaTrongKhoa(LichDungGa ld, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            // Kiểm tra tính đơn điệu thời gian và không xung đột với các điểm dừng liền trước/sau
            var dtLich = _repo.LayLichDungGa(ld.MaChuyenTau);
            foreach (DataRow r in dtLich.Rows)
            {
                int maDiemDungExist = Convert.ToInt32(r["MaDiemDung"]);
                if (maDiemDungExist == ld.MaDiemDung) continue;

                int thuTuExist = Convert.ToInt32(r["ThuTuDung"]);
                string tenGaExist = r["TenGa"]?.ToString() ?? "";
                DateTime denExist = Convert.ToDateTime(r["GioDenKeHoach"]);
                DateTime diExist = Convert.ToDateTime(r["GioDiKeHoach"]);

                if (thuTuExist < ld.ThuTuDung && diExist >= ld.GioDenKeHoach)
                {
                    thongBaoLoi = $"Giờ đến tại ga ({ld.GioDenKeHoach:HH:mm dd/MM}) phải sau giờ rời ga #{thuTuExist} ({tenGaExist} lúc {diExist:HH:mm dd/MM})!";
                    return false;
                }
                if (thuTuExist > ld.ThuTuDung && denExist <= ld.GioDiKeHoach)
                {
                    thongBaoLoi = $"Giờ rời ga ({ld.GioDiKeHoach:HH:mm dd/MM}) phải trước giờ đến của ga #{thuTuExist} ({tenGaExist} lúc {denExist:HH:mm dd/MM})!";
                    return false;
                }
            }

            try
            {
                bool kq = _repo.CapNhatLichDungGa(ld) > 0;
                if (!kq) thongBaoLoi = "Không thể cập nhật điểm dừng vào cơ sở dữ liệu.";
                return kq;
            }
            catch (SqlException ex)
            {
                thongBaoLoi = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                thongBaoLoi = $"Lỗi cập nhật điểm dừng: {ex.Message}";
                return false;
            }
        }

        public bool CapNhatLichDungGa(LichDungGa ld) => CapNhatLichDungGa(ld, out _);

        public bool XoaLichDungGa(int maDiemDung)
        {
            PhanQuyenService.BatBuocCoQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu"); // [Sprint 3] thieu quyen -> KhongDuQuyenException
            if (maDiemDung <= 0) return false;
            int maChuyenTau = _repo.LayMaChuyenCuaDiemDung(maDiemDung);
            if (maChuyenTau <= 0) return false;
            byte[]? phienBan = null;
            return XoaLichDungGa(maChuyenTau, maDiemDung, ref phienBan, out _);
        }

        // [13/11] Khong xoa ga co ve con hieu luc len / xuong, hoac la ga noi / cat toa cua doan tau (lam hong ve, toa ban ve)
        public bool XoaLichDungGa(int maChuyenTau, int maDiemDung, ref byte[]? phienBan, out string thongBaoLoi)
        {
            PhanQuyenService.BatBuocCoQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu"); // [Sprint 3] thieu quyen -> KhongDuQuyenException
            thongBaoLoi = string.Empty;
            if (maChuyenTau <= 0 || maDiemDung <= 0)
            {
                thongBaoLoi = "Điểm dừng không hợp lệ!";
                return false;
            }
            return TrenChuyen(maChuyenTau, ref phienBan, "Xóa một điểm dừng", () =>
            {
                var (tenGa, soVe, soToa, cuaChuyen) = _repo.DemRangBuocXoaDiemDung(maDiemDung, maChuyenTau);
                if (!cuaChuyen) return "Điểm dừng không còn thuộc chuyến này (có thể vừa bị xóa). Hãy nạp lại lịch dừng.";
                if (soVe > 0)
                    return $"Không xóa được ga {tenGa}: còn {soVe} vé hiệu lực lên / xuống tại ga này. Đổi chặng hoặc hoàn các vé đó trước.";
                if (soToa > 0)
                    return $"Không xóa được ga {tenGa}: là ga nối / cắt {soToa} toa của đoàn tàu. Sửa đoàn tàu ở phân hệ Phương tiện trước.";
                return _repo.XoaLichDungGa(maDiemDung) > 0 ? null : "Không xóa được điểm dừng (có thể đã bị xóa trước đó).";
            }, out thongBaoLoi);
        }

        public bool DayThuTuDung(int maChuyenTau, int tuThuTu)
        {
            if (maChuyenTau <= 0 || tuThuTu <= 0) return false;
            byte[]? phienBan = null;
            return DayThuTuDung(maChuyenTau, tuThuTu, ref phienBan, out _);
        }

        public bool DayThuTuDung(int maChuyenTau, int tuThuTu, ref byte[]? phienBan, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            if (maChuyenTau <= 0 || tuThuTu <= 0) return false;
            return TrenChuyen(maChuyenTau, ref phienBan, $"Dời thứ tự các ga từ #{tuThuTu}", () =>
                _repo.DayThuTuDung(maChuyenTau, tuThuTu) ? null : "Không dời được thứ tự các điểm dừng.", out thongBaoLoi);
        }

        public bool SapXepLichDungTheoKm(int maChuyenTau, bool chieuBacNam, out int soDiemDungThayDoi)
        {
            byte[]? phienBan = null;
            return SapXepLichDungTheoKm(maChuyenTau, chieuBacNam, ref phienBan, out soDiemDungThayDoi, out _);
        }

        public bool SapXepLichDungTheoKm(int maChuyenTau, bool chieuBacNam, ref byte[]? phienBan, out int soDiemDungThayDoi, out string thongBaoLoi)
        {
            soDiemDungThayDoi = 0;
            thongBaoLoi = string.Empty;
            if (maChuyenTau <= 0) return false;
            int soThayDoi = 0;
            bool ok = TrenChuyen(maChuyenTau, ref phienBan, "Sắp xếp lại lịch dừng theo lý trình", () =>
                _repo.SapXepLichDungTheoKm(maChuyenTau, chieuBacNam, out soThayDoi) ? null : "Không sắp xếp được lịch dừng.",
                out thongBaoLoi);
            soDiemDungThayDoi = soThayDoi;
            return ok;
        }

        public bool TinhTienGioCacGaPhiaSau(int maChuyenTau, int tuThuTu, int soPhut)
        {
            if (maChuyenTau <= 0 || tuThuTu <= 0 || soPhut <= 0) return false;
            byte[]? phienBan = null;
            return TinhTienGioCacGaPhiaSau(maChuyenTau, tuThuTu, soPhut, ref phienBan, out _);
        }

        public bool TinhTienGioCacGaPhiaSau(int maChuyenTau, int tuThuTu, int soPhut, ref byte[]? phienBan, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            if (maChuyenTau <= 0 || tuThuTu <= 0 || soPhut <= 0) return false;
            return TrenChuyen(maChuyenTau, ref phienBan, $"Lùi giờ các ga từ #{tuThuTu} thêm {soPhut} phút", () =>
                _repo.TinhTienGioCacGaPhiaSau(maChuyenTau, tuThuTu, soPhut) ? null : "Không lùi được giờ các ga phía sau.",
                out thongBaoLoi);
        }

        public bool SaoChepLichDungTuChuyenMau(int maMacTau, int maChuyenDich, DateTime ngayKhoiHanh)
        {
            byte[]? phienBan = null;
            return SaoChepLichDungTuChuyenMau(maMacTau, maChuyenDich, ngayKhoiHanh, ref phienBan, out _);
        }

        public bool SaoChepLichDungTuChuyenMau(int maMacTau, int maChuyenDich, DateTime ngayKhoiHanh, ref byte[]? phienBan, out string thongBaoLoi)
        {
            PhanQuyenService.BatBuocCoQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu"); // [Sprint 3] thieu quyen -> KhongDuQuyenException
            thongBaoLoi = string.Empty;
            if (maMacTau <= 0 || maChuyenDich <= 0) return false;
            return TrenChuyen(maChuyenDich, ref phienBan, "Sao chép lịch dừng từ chuyến mẫu", () =>
            {
                int maChuyenMau = _repo.TimChuyenMauGanNhatCungMac(maMacTau, maChuyenDich);
                if (maChuyenMau <= 0) return "Không tìm thấy chuyến mẫu cùng mác tàu để sao chép lịch dừng.";
                // Thay ca lich dung: khong duoc lam mat ga co ve / ga noi cat toa
                string? chan = _repo.LyDoKhongThayLichDung(maChuyenDich, maChuyenMau);
                if (chan != null) return chan;
                return _repo.SaoChepLichDungTuChuyenKhac(maChuyenMau, maChuyenDich, ngayKhoiHanh) > 0
                    ? null : "Chuyến mẫu không có điểm dừng nào để sao chép.";
            }, out thongBaoLoi);
        }

        // =========================================================================
        // [13/11 Dat] CHONG GHI DE KHI HAI NGUOI CUNG SUA MOT CHUYEN
        //
        // Moi thao tac ghi tren chuyen (form chuyen, lich dung ga, dau may buoc 3, xoa, doi trang thai) chay trong
        // khoa "vanhanh.SuaChuyen:<ma>" (KhoaSuaChuyen): doc PhienBan cua chuyen -> khac PhienBan man hinh dang giu
        // -> KHONG ghi, bao "vua duoc thay doi" (VuaXungDotPhienBan = true, man hinh nap lai) -> kiem tra doc CSDL
        // + ghi (ham cu cua A Ba) -> cham PhienBan + nhat ky SUA_LICH_TRINH -> tra PhienBan moi cho man hinh.
        // phienBan = null: khong so (ham cu, tool), van khoa + cham.
        // =========================================================================
        public bool VuaXungDotPhienBan { get; private set; }

        private bool TrenChuyen(int maChuyenTau, ref byte[]? phienBan, string moTa, Func<string?> thaoTac,
                                out string thongBaoLoi, bool ghiNhatKy = true)
        {
            VuaXungDotPhienBan = false;
            thongBaoLoi = string.Empty;
            if (maChuyenTau <= 0)
            {
                thongBaoLoi = "Chưa chọn chuyến tàu.";
                return false;
            }
            try
            {
                using var khoa = KhoaSuaChuyen.Mo(maChuyenTau);
                byte[]? hienTai = khoa.DocPhienBan();
                if (hienTai == null)
                {
                    VuaXungDotPhienBan = true;
                    thongBaoLoi = "Không tìm thấy chuyến tàu trong CSDL (có thể vừa bị người khác xóa). Màn hình sẽ nạp lại danh sách.";
                    return false;
                }
                if (phienBan != null && !hienTai.AsSpan().SequenceEqual(phienBan))
                {
                    VuaXungDotPhienBan = true;
                    thongBaoLoi = ThongDiepXungDot(maChuyenTau);
                    return false;
                }

                string? loi = thaoTac();
                if (loi == null)
                {
                    khoa.Cham();
                    if (ghiNhatKy) khoa.GhiNhatKy("SUA_LICH_TRINH", new { ThaoTac = moTa });
                }
                // Doc lai ca khi loi: thao tac khong ghi gi thi PhienBan khong doi; man hinh luon giu ban moi nhat
                phienBan = khoa.DocPhienBan();
                if (loi != null)
                {
                    thongBaoLoi = loi;
                    return false;
                }
                return true;
            }
            catch (TimeoutException ex)
            {
                thongBaoLoi = ex.Message;
                return false;
            }
            catch (SqlException ex)
            {
                thongBaoLoi = "Không ghi được vào CSDL: " + (ex.Errors.Count > 0 ? ex.Errors[0].Message : ex.Message);
                return false;
            }
        }

        public static string ThongDiepXungDot(int maChuyenTau)
        {
            string lanGanNhat = "";
            try
            {
                var lan = KhoaSuaChuyen.LayLanSuaGanNhat(maChuyenTau);
                if (lan != null)
                    lanGanNhat = $"\nLần ghi gần nhất trong nhật ký: {(lan.NguoiSua.Length > 0 ? lan.NguoiSua : "không rõ người")} "
                               + $"lúc {lan.LucSua:HH:mm dd/MM} — {TenThaoTac(lan)}.";
            }
            catch (SqlException) { /* chi la thong tin them */ }

            return "Chuyến tàu này vừa được thay đổi sau khi bạn mở (ở máy khác, ở phân hệ Phương tiện / Bảo trì, "
                 + "hoặc do xếp tránh tự động). Để không ghi đè, thao tác của bạn CHƯA được lưu."
                 + lanGanNhat
                 + "\nMàn hình sẽ nạp lại dữ liệu mới nhất; hãy kiểm tra rồi làm lại.";
        }

        private static string TenThaoTac(LanSuaChuyenDTO lan)
        {
            // SUA_LICH_TRINH, XEP_TRANH_TAU ghi {"ThaoTac": "..."} -> dung nguyen cau do (co ten ga, so phut)
            if (lan.DuLieuMoi != null && lan.HanhDong is "SUA_LICH_TRINH" or "XEP_TRANH_TAU")
            {
                try
                {
                    using var json = System.Text.Json.JsonDocument.Parse(lan.DuLieuMoi);
                    if (json.RootElement.TryGetProperty("ThaoTac", out var tt) && tt.GetString() is { Length: > 0 } s) return s;
                }
                catch (System.Text.Json.JsonException) { }
            }
            return lan.HanhDong switch
            {
                "SUA_LICH_TRINH" => "Sửa lịch trình chuyến",
                "DOI_TRANG_THAI_CHUYEN" => "Đổi trạng thái chuyến",
                "LAP_DOAN_TAU" => "Lập đoàn tàu (Phương tiện)",
                "SUA_DOAN_TAU" => "Sửa đoàn tàu (Phương tiện)",
                "DONG_BO_TOA_BAN_VE" => "Đồng bộ toa bán vé theo đoàn tàu",
                "XEP_TRANH_TAU" => "Xếp tránh tàu (Biểu đồ chạy tàu)",
                "DUYET_AN_TOAN" => "Duyệt an toàn đoàn tàu (Bảo trì)",
                "BO_DUYET_AN_TOAN" => "Bỏ duyệt an toàn đoàn tàu (Bảo trì)",
                _ => lan.HanhDong
            };
        }

        private string TenGa(int maGa)
        {
            try { return _repo.LayTenGa(maGa) ?? $"ga #{maGa}"; }
            catch (SqlException) { return $"ga #{maGa}"; }
        }

        public static bool KiemTraCoTheXoaChuyen(string trangThai, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            if (trangThai == "DANG_CHAY" || trangThai == "TRE_GIO" || trangThai == "HOAN_THANH")
            {
                thongBaoLoi = $"Chuyến tàu đang vận hành hoặc đã hoàn thành ({trangThai}), không được phép xóa khỏi cơ sở dữ liệu! Vui lòng chuyển sang [Đã Hủy] nếu muốn ngừng phục vụ.";
                return false;
            }
            return true;
        }

        public static DateTime TinhToanGioVeDich(DateTime ngayXuatPhat, TimeSpan gioDi, TimeSpan gioDen, decimal thoiGianChuanGio)
        {
            DateTime dtDi = ngayXuatPhat.Date.Add(gioDi);
            if (thoiGianChuanGio <= 0) thoiGianChuanGio = 12.0m;

            int soNgayDuKien = (int)Math.Floor((double)thoiGianChuanGio / 24.0);
            DateTime dtDen = ngayXuatPhat.Date.AddDays(soNgayDuKien).Add(gioDen);

            if (dtDen <= dtDi)
            {
                dtDen = dtDen.AddDays(1);
            }
            while ((dtDen - dtDi).TotalHours < (double)thoiGianChuanGio - 12.0)
            {
                dtDen = dtDen.AddDays(1);
            }
            return dtDen;
        }

        public static bool KiemTraThoiGianChayHopLy(TimeSpan thoiGianChayThucTe, decimal thoiGianChuanGio, out string canhBao)
        {
            canhBao = string.Empty;
            if (thoiGianChuanGio <= 0) return true;

            double ratio = thoiGianChayThucTe.TotalHours / (double)thoiGianChuanGio;
            if (ratio < 0.4)
            {
                canhBao = $"Thời gian chạy ({thoiGianChayThucTe.TotalHours:F1}h) ngắn bất thường so với chuẩn ({thoiGianChuanGio:F1}h - dưới 40%)!";
                return false;
            }
            if (ratio > 3.0)
            {
                canhBao = $"Thời gian chạy ({thoiGianChayThucTe.TotalHours:F1}h) dài bất thường so với chuẩn ({thoiGianChuanGio:F1}h - trên 300%)!";
                return false;
            }
            return true;
        }

        public static int TinhSoPhutTre(DateTime gioKH, DateTime gioTT)
        {
            int diff = (int)Math.Round((gioTT - gioKH).TotalMinutes);
            return Math.Max(0, diff);
        }

        public static bool KiemTraGioThucTeHopLe(DateTime? gioDiTT, DateTime? gioDenTT, out string thongBaoLoi)
        {
            thongBaoLoi = string.Empty;
            DateTime maxAllowed = DateTime.Now.AddHours(2); // Dung sai tối đa 2 giờ đề phòng sai lệch đồng hồ client

            if (gioDiTT.HasValue && gioDiTT.Value > maxAllowed)
            {
                thongBaoLoi = "Giờ xuất phát thực tế không được ở trong tương lai!";
                return false;
            }

            if (gioDenTT.HasValue)
            {
                if (gioDenTT.Value > maxAllowed)
                {
                    thongBaoLoi = "Giờ về đích thực tế không được ở trong tương lai!";
                    return false;
                }
                if (gioDiTT.HasValue && gioDenTT.Value < gioDiTT.Value)
                {
                    thongBaoLoi = "Giờ về đích thực tế không được trước giờ xuất phát thực tế!";
                    return false;
                }
            }
            return true;
        }

        public bool Xoa(int maChuyenTau, out string thongBaoLoi)
        {
            byte[]? phienBan = null;
            return Xoa(maChuyenTau, ref phienBan, out thongBaoLoi);
        }

        public bool Xoa(int maChuyenTau, ref byte[]? phienBan, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu", out thongBaoLoi)) return false;
            if (maChuyenTau <= 0)
            {
                thongBaoLoi = "Mã chuyến tàu không hợp lệ!";
                return false;
            }
            return TrenChuyen(maChuyenTau, ref phienBan, "Xóa chuyến tàu", () =>
            {
                string trangThai = _repo.LayTrangThai(maChuyenTau);
                if (!KiemTraCoTheXoaChuyen(trangThai, out string loi)) return loi;
                return _repo.Xoa(maChuyenTau, out loi) ? null : loi;
            }, out thongBaoLoi);
        }

        public bool Xoa(int maChuyenTau) => Xoa(maChuyenTau, out _);


        // =========================================================================
        // NGHIỆP VỤ ĐOÀN TÀU & ĐẦU MÁY (DoanTau, DauMay)
        // =========================================================================

        public DataTable LayThongTinDoanTau(int maChuyenTau)
        {
            if (maChuyenTau <= 0) return new DataTable();
            return _repo.LayThongTinDoanTau(maChuyenTau);
        }

        public DataTable LayDanhSachDauMay(DateTime? gioDi = null, DateTime? gioDen = null, int? maChuyenTauHienTai = null)
        {
            return _repo.LayDanhSachDauMay(gioDi, gioDen, maChuyenTauHienTai);
        }

        public bool KiemTraXungDotDauMay(int maDauMay, DateTime gioDi, DateTime gioDen, int maChuyenHienTai, out string thongBaoLoi)
        {
            if (maDauMay <= 0)
            {
                thongBaoLoi = "Chưa chọn đầu máy!";
                return false;
            }
            if (gioDen <= gioDi)
            {
                thongBaoLoi = "Giờ đến dự kiến phải lớn hơn giờ xuất phát!";
                return false;
            }
            return _repo.KiemTraXungDotDauMay(maDauMay, gioDi, gioDen, maChuyenHienTai, out thongBaoLoi);
        }

        public bool LuuDoanTau(DoanTau dt, out string thongBaoLoi)
        {
            byte[]? phienBan = null;
            return LuuDoanTau(dt, ref phienBan, out thongBaoLoi);
        }

        public bool LuuDoanTau(DoanTau dt, ref byte[]? phienBan, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu", out thongBaoLoi)) return false;
            thongBaoLoi = string.Empty;
            if (dt.MaChuyenTau <= 0)
            {
                thongBaoLoi = "Mã chuyến tàu không hợp lệ!";
                return false;
            }
            if (dt.MaDauMayChinh <= 0)
            {
                thongBaoLoi = "Vui lòng chỉ định Đầu Máy Kéo Chính!";
                return false;
            }
            if (dt.MaDauMayDay.HasValue && dt.MaDauMayDay.Value > 0 && dt.MaDauMayDay.Value == dt.MaDauMayChinh)
            {
                thongBaoLoi = "Đầu máy kéo chính và đầu máy đẩy không được trùng nhau!";
                return false;
            }
            if (dt.TongChieuDaiM > CHIEU_DAI_TOI_DA_DOAN_TAU_M)
            {
                thongBaoLoi = $"Chiều dài đoàn tàu ({dt.TongChieuDaiM:F1}m) vượt quá trần quy chuẩn an toàn tối đa ({CHIEU_DAI_TOI_DA_DOAN_TAU_M:F0}m)!";
                return false;
            }
            if (dt.TongSoToa <= 0 || dt.TongSoToa > 20)
            {
                thongBaoLoi = "Tổng số toa đoàn tàu phải từ 1 đến 20 toa!";
                return false;
            }

            return TrenChuyen(dt.MaChuyenTau, ref phienBan, "Lưu đầu máy kéo / đẩy (bước 3 Lịch trình)", () =>
                _repo.LuuDoanTau(dt) > 0 ? null : "Không lưu được đoàn tàu vào CSDL.", out thongBaoLoi);
        }

        public bool KiemTraChieuDaiVoiGaDung(int maChuyenTau, decimal tongChieuDaiM, out string canhBao)
        {
            if (maChuyenTau <= 0)
            {
                canhBao = string.Empty;
                return true;
            }
            return _repo.KiemTraChieuDaiVoiGaDung(maChuyenTau, tongChieuDaiM, out canhBao);
        }

        // =========================================================================
        // NGHIỆP VỤ BIÊN CHẾ TOA XE KHÁCH (ToaXeKhach)
        // =========================================================================

        public bool ThemToaXeKhach(ET.VanTai.ToaXeKhach tx, bool tuDongSinhGhe, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu", out thongBaoLoi)) return false;
            thongBaoLoi = string.Empty;
            if (tx.MaChuyenTau <= 0)
            {
                thongBaoLoi = "Chưa chọn chuyến tàu!";
                return false;
            }
            if (string.IsNullOrWhiteSpace(tx.NhanHieuToa))
            {
                thongBaoLoi = "Ký hiệu toa không được để trống!";
                return false;
            }
            if (tx.ThuTuToa <= 0)
            {
                thongBaoLoi = "Thứ tự toa phải lớn hơn 0!";
                return false;
            }
            if (tx.SucChua <= 0)
            {
                thongBaoLoi = "Sức chứa toa xe phải lớn hơn 0!";
                return false;
            }

            // [13/11] Chuyen da lap doan tau o Phuong tien: toa ban ve di theo doan tau (dong bo khi luu doan tau)
            if (_repo.CoChiTietDoanTau(tx.MaChuyenTau))
            {
                thongBaoLoi = "Chuyến đã lập đoàn tàu: toa bán vé tự đồng bộ theo đoàn tàu. Thêm toa ở phân hệ Phương tiện (Lập đoàn tàu).";
                return false;
            }
            int maToa = _repo.ThemToaXeKhach(tx, tuDongSinhGhe);
            return maToa > 0;
        }

        public bool XoaToaXeKhach(int maToaXeKhach, out string thongBaoLoi)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu", out thongBaoLoi)) return false;
            if (maToaXeKhach <= 0)
            {
                thongBaoLoi = "Mã toa xe không hợp lệ!";
                return false;
            }
            // [13/11] Toa ban ve cua chuyen da lap doan tau chi doi qua Lap doan tau (Phuong tien)
            if (_repo.ToaBanVeThuocChuyenDaLapDoan(maToaXeKhach))
            {
                thongBaoLoi = "Toa bán vé này đi theo đoàn tàu đã lập. Bỏ toa ở phân hệ Phương tiện (Lập đoàn tàu) — hệ thống kiểm vé đã bán khi lưu.";
                return false;
            }
            return _repo.XoaToaXeKhach(maToaXeKhach, out thongBaoLoi);
        }

        public bool SaoChepBienCheTuChuyenMau(int maMacTau, int maChuyenDich, out string thongBao)
        {
            // [Sprint 3] Service tu chan khi tai khoan khong co quyen ghi (giao dien chi an tab).
            if (!PhanQuyenService.KiemTraQuyen(DTO.Common.MaQuyen.LapBieuDoChayTau, "lập / sửa lịch trình chạy tàu", out thongBao)) return false;
            thongBao = string.Empty;
            if (maMacTau <= 0 || maChuyenDich <= 0)
            {
                thongBao = "Dữ liệu mác tàu hoặc chuyến tàu không hợp lệ!";
                return false;
            }

            int maChuyenMau = _repo.TimChuyenMauGanNhatCungMac(maMacTau, maChuyenDich);
            if (maChuyenMau <= 0)
            {
                thongBao = "Không tìm thấy chuyến tàu mẫu nào cùng mác tàu để sao chép biên chế!";
                return false;
            }

            if (_repo.CoChiTietDoanTau(maChuyenDich))
            {
                thongBao = "Chuyến đã lập đoàn tàu: toa bán vé tự đồng bộ theo đoàn tàu, không sao chép biên chế mẫu.";
                return false;
            }
            int soToaSaoChep = _repo.SaoChepBienCheToaXe(maChuyenMau, maChuyenDich);
            thongBao = $"Đã sao chép thành công {soToaSaoChep} toa xe từ chuyến mẫu (Mã #{maChuyenMau})!";
            return soToaSaoChep > 0;
        }
    }
}
