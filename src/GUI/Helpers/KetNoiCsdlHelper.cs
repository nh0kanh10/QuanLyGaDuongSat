using DAL.Connection;
using DTO.Common;

namespace GUI.Helpers
{
    // =========================================================================
    // Bao dam ung dung noi duoc toi SQL Server truoc khi mo cua so dau tien.
    //
    // Thu tu:
    //   1. Lay chuoi ket noi tu cau hinh (CauHinhUngDung: appsettings.json +
    //      appsettings.Local.json) va dat vao DatabaseHelper.
    //   2. Thu ket noi. Duoc thi xong.
    //   3. Khong duoc va cau hinh cho phep (TuDongDoMayChu = true) thi do lan
    //      luot cac instance quen gap - de nguoi trong nhom chua kip tao
    //      appsettings.Local.json van chay duoc.
    //
    // Chay mot lan duy nhat luc khoi dong (App.OnStartup).
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

        // Tra ve true neu tim duoc mot instance ket noi duoc toi CSDL.
        public static bool DamBaoKetNoi()
        {
            if (_daKhoiTao) return _ketNoiThanhCong;
            _daKhoiTao = true;

            CauHinhUngDung.Nap();

            // 1-2. Uu tien dung chuoi trong cau hinh.
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

            // Khong noi duoc may nao: tra chuoi ve dung cau hinh de thong bao loi
            // hien ten may dung nhu nguoi dung da khai.
            DatabaseHelper.SetConnectionString(CauHinhUngDung.ChuoiKetNoi());
            _ketNoiThanhCong = false;
            _mayChuDangDung = CauHinhUngDung.MayChu;
            return false;
        }
    }
}
