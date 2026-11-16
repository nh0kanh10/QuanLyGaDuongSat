using System;
using System.IO;
using System.Diagnostics;
using DAL.Connection;
using DTO.Common;

namespace GUI.Helpers
{
    // =========================================================================
    // Bao dam ung dung noi duoc toi SQL Server truoc khi mo cua so dau tien.
    //
    // Thu tu:
    //   1. Uu tien file MDF cuc bo neu co trong thu muc app (LocalDB AttachDbFilename).
    //   2. Lay chuoi ket noi tu cau hinh (appsettings.json + appsettings.Local.json).
    //   3. Khong duoc va cau hinh cho phep (TuDongDoMayChu = true) thi do lan
    //      luot cac instance quen gap (.\SQLEXPRESS, localhost, .).
    // =========================================================================
    public static class KetNoiCsdlHelper
    {
        private static readonly string[] CacMayChuUngVien =
        {
            @".\SQLEXPRESS",
            ".",
            "localhost",
            @"(localdb)\MSSQLLocalDB"
        };

        private static bool _daKhoiTao;
        private static bool _ketNoiThanhCong;
        private static string _mayChuDangDung = string.Empty;
        private static bool _doTuDong;

        // May chu thuc su dang dung (sau khi do xong), de hien trong thong bao loi.
        public static string MayChuDangDung => _mayChuDangDung;

        public static bool KetNoiThanhCong => _ketNoiThanhCong;

        // true khi phai do tu dong moi noi duoc => cau hinh dang lech, nen nhac
        // nguoi dung sua appsettings.Local.json cho dung may cua minh.
        public static bool DaPhaiDoTuDong => _doTuDong;

        // Mo ta ngan gon de ghi nhat ky / hien khi ket noi that bai.
        public static string MoTaCauHinh =>
            $"Cấu hình đọc từ {CauHinhUngDung.CacTepDaDoc} tại {CauHinhUngDung.ThuMucCauHinh}";

        public static void DatTrangThaiKetNoi(bool thanhCong, string mayChu)
        {
            _ketNoiThanhCong = thanhCong;
            _mayChuDangDung = mayChu;
        }

        // Tra ve true neu tim duoc mot instance ket noi duoc toi CSDL.
        public static bool DamBaoKetNoi()
        {
            if (_daKhoiTao) return _ketNoiThanhCong;
            _daKhoiTao = true;

            CauHinhUngDung.Nap();

            // 1. Uu tien file MDF cuc bo neu co trong thu muc app
            string thuMucApp = AppDomain.CurrentDomain.BaseDirectory;
            string fileMdf = Path.Combine(thuMucApp, "QuanLyDuongSatV2.mdf");
            if (File.Exists(fileMdf))
            {
                KichHoatLocalDbService();
                string chuoiMdf = TaoChuoiKetNoiMdf(fileMdf);
                DatabaseHelper.SetConnectionString(chuoiMdf);
                if (DatabaseHelper.TestConnection())
                {
                    _ketNoiThanhCong = true;
                    _mayChuDangDung = @"(localdb)\MSSQLLocalDB [MDF]";
                    return true;
                }
            }

            // 2. Uu tien dung chuoi trong cau hinh.
            DatabaseHelper.SetConnectionString(CauHinhUngDung.ChuoiKetNoi());
            if (DatabaseHelper.TestConnection())
            {
                _ketNoiThanhCong = true;
                _mayChuDangDung = CauHinhUngDung.MayChu;
                return true;
            }

            // 3. Do cac instance quen gap neu cau hinh cho phep.
            if (!CauHinhUngDung.TuDongDoMayChu)
            {
                _ketNoiThanhCong = false;
                _mayChuDangDung = CauHinhUngDung.MayChu;
                return false;
            }

            foreach (string mayChu in CacMayChuUngVien)
            {
                DatabaseHelper.SetConnectionString(
                    CauHinhUngDung.ChuoiKetNoiTheoMayChu(mayChu));

                if (DatabaseHelper.TestConnection())
                {
                    _ketNoiThanhCong = true;
                    _mayChuDangDung = mayChu;
                    _doTuDong = true;
                    return true;
                }
            }

            // Khong noi duoc may nao: tra chuoi ve dung cau hinh
            DatabaseHelper.SetConnectionString(CauHinhUngDung.ChuoiKetNoi());
            _ketNoiThanhCong = false;
            _mayChuDangDung = CauHinhUngDung.MayChu;
            return false;
        }

        public static string TaoChuoiKetNoiMdf(string duongDanMdf)
        {
            return $@"Server=(localdb)\MSSQLLocalDB;AttachDbFilename={duongDanMdf};Database=QuanLyDuongSatV2;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=8;";
        }

        public static void KichHoatLocalDbService()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c sqllocaldb start MSSQLLocalDB",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var process = Process.Start(startInfo);
                process?.WaitForExit(3000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Cảnh báo] Kích hoạt LocalDB: {ex.Message}");
            }
        }
    }
}
