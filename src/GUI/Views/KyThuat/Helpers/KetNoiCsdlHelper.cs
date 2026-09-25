using DAL.Connection;

namespace GUI.Views.KyThuat.Helpers
{
    // =========================================================================
    // Tu do tim instance SQL Server dang chay tren may tram.
    //
    // Ly do: moi may cua nhom cai mot kieu instance khac nhau (.\SQLEXPRESS,
    // instance mac dinh '.', hoac localhost). Chuoi ket noi mac dinh trong
    // DatabaseHelper chi tro toi .\SQLEXPRESS nen se hong tren may dung
    // instance mac dinh. Helper nay thu lan luot cac ung vien va goi
    // DatabaseHelper.SetConnectionString cho ung vien dau tien ket noi duoc.
    //
    // Chay mot lan duy nhat, khong sua file dung chung nao.
    // =========================================================================
    public static class KetNoiCsdlHelper
    {
        private const string TenCsdl = "QuanLyDuongSatV2";

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

        public static string MayChuDangDung => _mayChuDangDung;

        public static bool KetNoiThanhCong => _ketNoiThanhCong;

        // Tra ve true neu tim duoc mot instance ket noi duoc toi CSDL.
        public static bool DamBaoKetNoi()
        {
            if (_daKhoiTao) return _ketNoiThanhCong;
            _daKhoiTao = true;

            // Uu tien chuoi ket noi dang duoc cau hinh san (neu no da chay duoc).
            if (DatabaseHelper.TestConnection())
            {
                _ketNoiThanhCong = true;
                _mayChuDangDung = @".\SQLEXPRESS";
                return true;
            }

            foreach (string mayChu in CacMayChuUngVien)
            {
                string chuoi = $@"Server={mayChu};Database={TenCsdl};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3;";
                DatabaseHelper.SetConnectionString(chuoi);

                if (DatabaseHelper.TestConnection())
                {
                    _ketNoiThanhCong = true;
                    _mayChuDangDung = mayChu;
                    return true;
                }
            }

            _ketNoiThanhCong = false;
            _mayChuDangDung = string.Empty;
            return false;
        }
    }
}
