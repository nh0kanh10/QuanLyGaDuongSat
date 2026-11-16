using System.Text.Json.Nodes;
using DTO.Common;

namespace Tests
{
    // CauHinhUngDung.LuuCauHinhLocal / GopCauHinhKetNoi (A Ba them 16/11, Dat sua 16/11):
    // luu cau hinh ket noi CHI doi muc "KetNoiCsdl", moi thu khac trong appsettings.Local.json giu nguyen.
    public class CauHinhGopKetNoiTests
    {
        internal const string TepCu = """
            {
              // may rieng
              "KetNoiCsdl": { "MayChu": "DESKTOP-ABC", "TenCsdlKiemThu": "QLDS_Test", },
              "TroLyAI": { "KhoaApi": "sk-gia-de-test", "MoHinh": "gpt-4.1-mini", "ThoiGianChoGiay": 60, "BatMoHinhNgonNgu": false },
            }
            """;

        private static JsonObject Doc(string json) => (JsonObject)JsonNode.Parse(json)!;

        [Fact]
        public void DoiMayChu_GiuNguyenMucTroLyAI_VaKhoaKhac()
        {
            var kq = Doc(CauHinhUngDung.GopCauHinhKetNoi(TepCu, @".\SQLEXPRESS", " QLDS_Moi ", null));

            var ai = kq["TroLyAI"]!.AsObject();
            Assert.Equal("sk-gia-de-test", (string?)ai["KhoaApi"]);
            Assert.Equal("gpt-4.1-mini", (string?)ai["MoHinh"]);
            Assert.Equal(60, (int)ai["ThoiGianChoGiay"]!);
            Assert.False((bool)ai["BatMoHinhNgonNgu"]!);

            var kn = kq["KetNoiCsdl"]!.AsObject();
            Assert.Equal(@".\SQLEXPRESS", (string?)kn["MayChu"]);
            Assert.Equal("QLDS_Moi", (string?)kn["TenCsdl"]);
            Assert.Equal("QLDS_Test", (string?)kn["TenCsdlKiemThu"]);
            Assert.Equal("", (string?)kn["ChuoiKetNoi"]);
            Assert.True((bool)kn["TuDongDoMayChu"]!);
        }

        [Fact]
        public void CheDoChuoiKetNoi_GhiChuoi_GiuMayChuCu()
        {
            const string chuoi = @"Server=(localdb)\MSSQLLocalDB;AttachDbFilename=D:\app\QuanLyDuongSatV2.mdf;Database=QuanLyDuongSatV2;";
            var kn = Doc(CauHinhUngDung.GopCauHinhKetNoi(TepCu, null, null, "  " + chuoi + "  "))["KetNoiCsdl"]!.AsObject();

            Assert.Equal(chuoi, (string?)kn["ChuoiKetNoi"]);
            Assert.Equal("DESKTOP-ABC", (string?)kn["MayChu"]);
        }

        [Fact]
        public void ChuaCoTep_ChiTaoMucKetNoi()
        {
            var kq = Doc(CauHinhUngDung.GopCauHinhKetNoi(null, "MAY-MOI", "QLDS", null));

            Assert.Single(kq);
            Assert.Equal("MAY-MOI", (string?)kq["KetNoiCsdl"]!["MayChu"]);
        }

        [Theory]
        [InlineData(@"{ ""KetNoiCsdl"": { ""MayChu"": "".\SQLEXPRESS"" } }")]   // loi 08/10: gach cheo nguoc don
        [InlineData(@"{ ""TroLyAI"": { ""KhoaApi"": ""sk-x"" ")]                  // thieu dau dong
        [InlineData("[1, 2]")]                                                      // khong phai doi tuong
        public void TepSaiCuPhap_BaoLoi_KhongGhiDe(string tepHong)
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => CauHinhUngDung.GopCauHinhKetNoi(tepHong, "MAY", null, null));
            Assert.Contains(CauHinhUngDung.TenTepRieng, ex.Message);
        }

        [Fact]
        public void GachCheoNguoc_GhiDung_DocLaiDuoc_KhongEscapeChuViet()
        {
            string json = CauHinhUngDung.GopCauHinhKetNoi(null, @"DESKTOP\SQLEXPRESS", "Đường_Sắt", null);

            Assert.Contains(@"DESKTOP\\SQLEXPRESS", json);
            Assert.Contains("Đường_Sắt", json);
            Assert.Equal(@"DESKTOP\SQLEXPRESS", (string?)Doc(json)["KetNoiCsdl"]!["MayChu"]);
        }
    }

    // Test ghi tep that: doi trang thai tinh cua CauHinhUngDung -> chay rieng, khong song song.
    [Collection("CauHinhUngDung")]
    public class CauHinhLuuTepTests
    {
        [Fact]
        public void LuuCauHinhLocal_GhiDungThuMucDaNap_GiuThietLapAI_VaKhongGhiDeTepHong()
        {
            string thuMucGoc = CauHinhUngDung.ThuMucCauHinh;
            string tam = Path.Combine(Path.GetTempPath(), "qlds_cauhinh_" + Guid.NewGuid().ToString("N"));
            string tep = Path.Combine(tam, CauHinhUngDung.TenTepRieng);
            Directory.CreateDirectory(tam);
            try
            {
                File.WriteAllText(tep, CauHinhGopKetNoiTests.TepCu);
                CauHinhUngDung.NapLai(tam);
                Assert.Equal(60, CauHinhUngDung.ThoiGianChoAiGiay);

                Assert.True(CauHinhUngDung.LuuCauHinhLocal("MAY-MOI", "QLDS_Moi", null));
                Assert.Equal("", CauHinhUngDung.LoiLuuCauHinh);
                Assert.Equal(tam, CauHinhUngDung.ThuMucCauHinh);          // nap lai dung thu muc, khong nhay ve thu muc exe
                Assert.Equal("MAY-MOI", CauHinhUngDung.MayChu);
                Assert.Equal("QLDS_Moi", CauHinhUngDung.TenCsdl);
                Assert.Equal("QLDS_Test", CauHinhUngDung.TenCsdlKiemThu);
                Assert.Equal("sk-gia-de-test", CauHinhUngDung.KhoaApiAI);
                Assert.Equal(60, CauHinhUngDung.ThoiGianChoAiGiay);
                Assert.False(CauHinhUngDung.BatMoHinhNgonNgu);
                Assert.False(File.Exists(tep + ".tmp"));

                const string hong = "{ \"TroLyAI\": { \"KhoaApi\": \"sk-x\" ";
                File.WriteAllText(tep, hong);
                Assert.False(CauHinhUngDung.LuuCauHinhLocal("MAY-KHAC", null, null));
                Assert.Contains("sai cú pháp", CauHinhUngDung.LoiLuuCauHinh);
                Assert.Equal(hong, File.ReadAllText(tep));
                Assert.False(File.Exists(tep + ".tmp"));
            }
            finally
            {
                CauHinhUngDung.NapLai(string.IsNullOrEmpty(thuMucGoc) ? null : thuMucGoc);
                try { Directory.Delete(tam, true); } catch { }
            }
        }
    }

    [CollectionDefinition("CauHinhUngDung", DisableParallelization = true)]
    public class CauHinhUngDungCollection { }
}
