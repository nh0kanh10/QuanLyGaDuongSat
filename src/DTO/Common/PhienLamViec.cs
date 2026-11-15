using System;
using System.Collections.Generic;
using System.Linq;

namespace DTO.Common
{
    // =========================================================================
    // Nguoi dang lam viec tren ung dung.
    //
    // Man hinh Dang nhap thuoc Sprint 3, nhung tu bay gio moi cau lenh ghi CSDL
    // da can biet "ai lam": Ga.NguoiTao / NguoiCapNhat, LenhPhongToa.MaNguoiDuyet
    // (NOT NULL), kiemtoan.NhatKyHeThong, BaoCao.NguoiLap, KhamXeKyThuat.NguoiKham...
    // Truoc do ma nguon viet cung chuoi "admin" o 13 cho - de nguyen thi het
    // Sprint 2 se thanh 40-50 cho, roi Sprint 3 phai di sua tung cho.
    //
    // Vi vay: moi noi lay nguoi thao tac tu DAY. Mac dinh la tai khoan admin
    // (MaTaiKhoan 1 trong seed) nen moi thu chay nhu cu. Sprint 3 chi viec goi
    // DangNhap(...) sau khi kiem tra mat khau - khong phai sua lai cac Repository.
    //
    // Cac truong trung ten cot phanquyen.TaiKhoan (script_tao_csdl_v2.sql).
    // Dat o tang DTO vi ca DAL, BUS, GUI deu tham chieu DTO.
    // =========================================================================
    public static class PhienLamViec
    {
        // Tai khoan admin trong seed - dung cho phien mac dinh truoc khi co dang nhap that.
        private const int MaTaiKhoanMacDinh = 1;
        private const string TenDangNhapMacDinh = "admin";
        private const string HoTenMacDinh = "Quản trị viên";

        private static readonly HashSet<string> _cacQuyen =
            new(StringComparer.OrdinalIgnoreCase);

        // --- Thong tin tai khoan -------------------------------------------------

        public static int MaTaiKhoan { get; private set; } = MaTaiKhoanMacDinh;

        public static string TenDangNhap { get; private set; } = TenDangNhapMacDinh;

        // Trung cot HoTenHienThi cua phanquyen.TaiKhoan.
        public static string HoTen { get; private set; } = HoTenMacDinh;

        // Null khi tai khoan khong gan voi ho so nhan vien nao.
        public static int? MaNhanVien { get; private set; }

        // false = phien mac dinh (chua co man hinh dang nhap, hoac da dang xuat).
        // Dung de CoQuyen() biet nen mo het quyen hay kiem that.
        public static bool DaDangNhapThat { get; private set; }

        public static DateTime ThoiDiemBatDau { get; private set; } = DateTime.Now;

        public static IReadOnlyCollection<string> CacQuyen => _cacQuyen.ToList();

        // Ten cac vai tro dang kich hoat cua tai khoan (hien tren thanh trang thai).
        public static IReadOnlyList<string> CacVaiTro { get; private set; } = Array.Empty<string>();

        public static string MoTaVaiTro => CacVaiTro.Count == 0 ? "" : string.Join(", ", CacVaiTro);

        // Sprint 3 (08/11): ung dung bat buoc dang nhap -> App dat true luc khoi dong.
        // true + chua dang nhap that => CoQuyen() tra false (khong con "mo het quyen").
        // Cong cu kiem thu / unit test khong dat => giu phien mac dinh mo het quyen nhu Sprint 1-2.
        public static bool BatBuocDangNhap { get; set; }

        // --- Quyen ---------------------------------------------------------------

        // =====================================================================
        // Kiem tra quyen theo ma quyen (phanquyen.Quyen.MaQuyenCode).
        //
        // Sprint 1-2: phien mac dinh chua dang nhap that -> tra true, moi chuc
        // nang dung duoc nhu hien nay.
        // Sprint 3: sau DangNhap(...) voi danh sach quyen that -> kiem tra that.
        // Nho vay ca hai nguoi gan CoQuyen(...) vao nut / tab ngay tu bay gio
        // ma khong phai cho man hinh dang nhap (Q10: chan ca o Service).
        // =====================================================================
        public static bool CoQuyen(string maQuyenCode)
        {
            if (!DaDangNhapThat) return !BatBuocDangNhap;
            if (string.IsNullOrWhiteSpace(maQuyenCode)) return false;
            return _cacQuyen.Contains(maQuyenCode.Trim());
        }

        public static bool CoBatKyQuyen(params string[] cacMaQuyen)
            => cacMaQuyen != null && cacMaQuyen.Any(CoQuyen);

        // Goi truoc khi ghi CSDL. Khong du quyen thi nem KhongDuQuyenException
        // de GUI bat lai va hien ThongBaoDialog.
        public static void BatBuocCoQuyen(string maQuyenCode, string? moTaHanhDong = null)
        {
            if (CoQuyen(maQuyenCode)) return;
            throw new KhongDuQuyenException(maQuyenCode, moTaHanhDong);
        }

        // --- Dang nhap / dang xuat ----------------------------------------------

        // =====================================================================
        // Sprint 3 goi ham nay sau khi kiem tra mat khau thanh cong.
        // cacQuyen: danh sach MaQuyenCode doc tu TaiKhoan_VaiTro -> VaiTro_Quyen -> Quyen.
        // =====================================================================
        public static void DangNhap(
            int maTaiKhoan,
            string tenDangNhap,
            string hoTen,
            int? maNhanVien = null,
            IEnumerable<string>? cacQuyen = null,
            IEnumerable<string>? cacVaiTro = null)
        {
            if (maTaiKhoan <= 0)
                throw new ArgumentOutOfRangeException(nameof(maTaiKhoan), "Mã tài khoản phải lớn hơn 0.");
            if (string.IsNullOrWhiteSpace(tenDangNhap))
                throw new ArgumentException("Tên đăng nhập không được để trống.", nameof(tenDangNhap));

            MaTaiKhoan = maTaiKhoan;
            TenDangNhap = tenDangNhap.Trim();
            HoTen = string.IsNullOrWhiteSpace(hoTen) ? TenDangNhap : hoTen.Trim();
            MaNhanVien = maNhanVien;
            DaDangNhapThat = true;
            ThoiDiemBatDau = DateTime.Now;
            CacVaiTro = cacVaiTro?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? new List<string>();

            _cacQuyen.Clear();
            if (cacQuyen != null)
            {
                foreach (string quyen in cacQuyen)
                {
                    if (!string.IsNullOrWhiteSpace(quyen)) _cacQuyen.Add(quyen.Trim());
                }
            }

            PhienDaDoi?.Invoke();
        }

        // Ve lai phien mac dinh (admin, mo het quyen).
        public static void DangXuat()
        {
            MaTaiKhoan = MaTaiKhoanMacDinh;
            TenDangNhap = TenDangNhapMacDinh;
            HoTen = HoTenMacDinh;
            MaNhanVien = null;
            DaDangNhapThat = false;
            ThoiDiemBatDau = DateTime.Now;
            CacVaiTro = Array.Empty<string>();
            _cacQuyen.Clear();

            PhienDaDoi?.Invoke();
        }

        // MainWindow / cac trang nghe su kien nay de an - hien tab, nut theo quyen.
        public static event Action? PhienDaDoi;
    }

    // =========================================================================
    // Ma quyen - trung phanquyen.Quyen.MaQuyenCode trong script CSDL.
    // Dung hang so thay vi go chuoi de hai nguoi khong lech ten quyen.
    // =========================================================================
    public static class MaQuyen
    {
        // --- 8 quyen da co trong seed -------------------------------------------
        public const string CauHinhHeThong = "SYS_CONFIG";
        public const string LapBieuDoChayTau = "TRAIN_SCHEDULE_EDIT";
        public const string DuyetLenhPhongToa = "BLOCK_ORDER_APPROVE";
        public const string GhiNhanSuCoChamGio = "DISPATCH_INCIDENT_REPORT";
        public const string BanVe = "TICKET_ISSUE";
        public const string HoanVe = "TICKET_REFUND";
        public const string LapVanDonHang = "CARGO_DISPATCH";
        public const string NghiemThuKhamXe = "TECH_SAFETY_APPROVE";

        // --- Quyen GHI them o Sprint 1-2, co trong CSDL tu migration V20261108_1000 ---
        // (danh muc day du 24 quyen + bang vai tro: database/README.md)
        public const string SuaHaTang = "INFRA_EDIT";
        public const string SoatVe = "TICKET_CHECK";

        // Phan he Phuong tien (Dat, Sprint 1). Sprint 3 them vao phanquyen.Quyen + gan vai tro
        // (de xuat SRS: LAP_TAU, QUAN_LY). Truoc do phien mac dinh mo het quyen nen van dung duoc.
        public const string QuanLyPhuongTien = "VEHICLE_EDIT";      // them / sua dau may, toa xe
        public const string LapDoanTau = "TRAIN_FORMATION_EDIT";    // lap / sua doan tau

        // Phan he Nhan su (Dat, Sprint 1) - de xuat, cung lo trinh nhu tren (SRS: NHAN_SU, PHAN_CONG_KIP)
        public const string QuanLyHoSoNhanVien = "STAFF_EDIT";      // them / sua ho so nhan vien
        public const string PhanCongKipLai = "CREW_ASSIGN";         // lap / sua / huy kip lai
        public const string NhanBanKipLai = "CREW_CHECKIN";         // kiem tra len ban, nhan ban, ban giao

        // Phan he Bao tri (Dat, Sprint 2). Kham xe + duyet an toan dung NghiemThuKhamXe (da co trong seed);
        // ma duoi de xuat, cung lo trinh nhu tren (SRS: KY_THUAT, QUAN_LY)
        public const string GhiNhatKyBaoTri = "MAINTENANCE_LOG";    // cap nhien lieu, bao duong dinh ky

        // --- Quyen XEM (Sprint 3, migration V20261108_1000) ----------------------
        // Moi phan he 1 quyen xem -> khop ma tran F / R / - cua bao cao (R = xem, khong ghi).
        // Tab hien khi co quyen xem HOAC mot quyen ghi cua phan he (QuyenTheoTab trong GUI).
        public const string XemMangLuoi = "NETWORK_VIEW";           // ga, khu gian, mac tau, lich trinh, bieu do
        public const string XemVe = "TICKET_VIEW";                  // ban ve, soat ve
        public const string XemHangHoa = "CARGO_VIEW";
        public const string XemPhuongTien = "VEHICLE_VIEW";
        public const string XemBaoTri = "MAINT_VIEW";
        public const string XemNhanSu = "STAFF_VIEW";
        public const string XemBaoCao = "REPORT_VIEW";
        public const string DungTroLyAI = "AI_ADVISOR";
    }

    // =========================================================================
    // Nem ra khi Service chan hanh dong vi thieu quyen (Q10).
    // =========================================================================
    public class KhongDuQuyenException : Exception
    {
        public string MaQuyenCode { get; }

        public KhongDuQuyenException(string maQuyenCode, string? moTaHanhDong = null)
            : base(string.IsNullOrWhiteSpace(moTaHanhDong)
                ? $"Tài khoản \"{PhienLamViec.TenDangNhap}\" không có quyền thực hiện thao tác này (cần quyền {maQuyenCode})."
                : $"Tài khoản \"{PhienLamViec.TenDangNhap}\" không có quyền {moTaHanhDong} (cần quyền {maQuyenCode}).")
        {
            MaQuyenCode = maQuyenCode;
        }
    }
}
