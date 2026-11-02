using System.Data;
using Microsoft.Data.SqlClient;
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
            if (_repo.KiemTraTrungLich(ct.MaMacTau, ct.NgayXuatPhat, ct.MaChuyenTau))
            {
                thongBaoLoi = $"Mác tàu này đã được lập lịch cho ngày {ct.NgayXuatPhat:dd/MM/yyyy} trên một chuyến khác!";
                return false;
            }
            // [Đạt 30/10] Sang Sẵn sàng / Đang chạy phải qua kiểm tra xuất bến (hợp đồng §7.2)
            string? lyDoChanXuatBen = LyDoChanXuatBen(ct.MaChuyenTau, _repo.LayTrangThai(ct.MaChuyenTau), ct.TrangThai);
            if (lyDoChanXuatBen != null)
            {
                thongBaoLoi = lyDoChanXuatBen;
                return false;
            }
            bool kq = _repo.CapNhat(ct) > 0;
            if (!kq) thongBaoLoi = "Không thể cập nhật chuyến tàu vào CSDL.";
            return kq;
        }

        public bool CapNhat(ChuyenTau ct) => CapNhat(ct, out _);

        public bool CapNhatThongSoKeHoach(int maChuyenTau, int maMacTau, DateTime ngayXuatPhat, DateTime gioXuatPhatKH, DateTime gioVeDichKH, out string thongBaoLoi)
        {
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
            if (_repo.KiemTraTrungLich(maMacTau, ngayXuatPhat, maChuyenTau))
            {
                thongBaoLoi = $"Mác tàu này đã được lập lịch cho ngày {ngayXuatPhat:dd/MM/yyyy} trên một chuyến khác!";
                return false;
            }
            bool kq = _repo.CapNhatThongSoKeHoach(maChuyenTau, maMacTau, ngayXuatPhat, gioXuatPhatKH, gioVeDichKH) > 0;
            if (!kq) thongBaoLoi = "Không thể cập nhật thông số kế hoạch chuyến tàu vào CSDL.";
            return kq;
        }

        public bool CapNhatThongSoKeHoach(int maChuyenTau, int maMacTau, DateTime ngayXuatPhat, DateTime gioXuatPhatKH, DateTime gioVeDichKH)
            => CapNhatThongSoKeHoach(maChuyenTau, maMacTau, ngayXuatPhat, gioXuatPhatKH, gioVeDichKH, out _);

        public bool CapNhatTrangThai(int maChuyenTau, string trangThai)
        {
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
        public bool DoiTrangThai(int maChuyenTau, string trangThaiMoi, out string thongBaoLoi)
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
            bool kq = _repo.CapNhatTrangThai(maChuyenTau, trangThaiMoi) > 0;
            if (!kq) thongBaoLoi = "Không thể cập nhật trạng thái chuyến tàu vào CSDL.";
            return kq;
        }

        public bool CapNhatGioThucTe(int maDiemDung, DateTime? gioDenTT, DateTime? gioDiTT)
        {
            if (maDiemDung <= 0) return false;
            return _repo.CapNhatGioThucTe(maDiemDung, gioDenTT, gioDiTT) > 0;
        }

        public bool ThemLichDungGa(LichDungGa ld, out string thongBaoLoi)
        {
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
            if (maDiemDung <= 0) return false;
            return _repo.XoaLichDungGa(maDiemDung) > 0;
        }

        public bool DayThuTuDung(int maChuyenTau, int tuThuTu)
        {
            if (maChuyenTau <= 0 || tuThuTu <= 0) return false;
            return _repo.DayThuTuDung(maChuyenTau, tuThuTu);
        }

        public bool SapXepLichDungTheoKm(int maChuyenTau, bool chieuBacNam, out int soDiemDungThayDoi)
        {
            soDiemDungThayDoi = 0;
            if (maChuyenTau <= 0) return false;
            return _repo.SapXepLichDungTheoKm(maChuyenTau, chieuBacNam, out soDiemDungThayDoi);
        }

        public bool TinhTienGioCacGaPhiaSau(int maChuyenTau, int tuThuTu, int soPhut)
        {
            if (maChuyenTau <= 0 || tuThuTu <= 0 || soPhut <= 0) return false;
            return _repo.TinhTienGioCacGaPhiaSau(maChuyenTau, tuThuTu, soPhut);
        }

        public bool SaoChepLichDungTuChuyenMau(int maMacTau, int maChuyenDich, DateTime ngayKhoiHanh)
        {
            if (maMacTau <= 0 || maChuyenDich <= 0) return false;
            int maChuyenMau = _repo.TimChuyenMauGanNhatCungMac(maMacTau, maChuyenDich);
            if (maChuyenMau <= 0) return false;
            return _repo.SaoChepLichDungTuChuyenKhac(maChuyenMau, maChuyenDich, ngayKhoiHanh) > 0;
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
            if (maChuyenTau <= 0)
            {
                thongBaoLoi = "Mã chuyến tàu không hợp lệ!";
                return false;
            }
            string trangThai = _repo.LayTrangThai(maChuyenTau);
            if (!KiemTraCoTheXoaChuyen(trangThai, out thongBaoLoi))
            {
                return false;
            }
            return _repo.Xoa(maChuyenTau, out thongBaoLoi);
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

            return _repo.LuuDoanTau(dt) > 0;
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

            int maToa = _repo.ThemToaXeKhach(tx, tuDongSinhGhe);
            return maToa > 0;
        }

        public bool XoaToaXeKhach(int maToaXeKhach, out string thongBaoLoi)
        {
            if (maToaXeKhach <= 0)
            {
                thongBaoLoi = "Mã toa xe không hợp lệ!";
                return false;
            }
            return _repo.XoaToaXeKhach(maToaXeKhach, out thongBaoLoi);
        }

        public bool SaoChepBienCheTuChuyenMau(int maMacTau, int maChuyenDich, out string thongBao)
        {
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

            int soToaSaoChep = _repo.SaoChepBienCheToaXe(maChuyenMau, maChuyenDich);
            thongBao = $"Đã sao chép thành công {soToaSaoChep} toa xe từ chuyến mẫu (Mã #{maChuyenMau})!";
            return soToaSaoChep > 0;
        }
    }
}
