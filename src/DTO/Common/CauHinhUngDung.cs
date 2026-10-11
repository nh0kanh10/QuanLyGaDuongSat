using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DTO.Common
{
    // =========================================================================
    // Doc cau hinh ket noi CSDL tu appsettings.json + appsettings.Local.json.
    //
    // Ly do co lop nay: truoc day chuoi ket noi viet cung trong
    // DAL.Connection.DatabaseHelper nen may nao lech instance SQL Server la phai
    // sua ma nguon -> file luon ban, khong commit duoc (ban tren Git tro
    // .\SQLEXPRESS, may khac tro ten may). Gio tach ra tep cau hinh:
    //   - appsettings.json       : dung chung, len Git
    //   - appsettings.Local.json : rieng tung may, bi .gitignore loai
    //
    // Thu tu uu tien: Local > chung > mac dinh trong ma.
    // Dat o tang DTO vi ca DAL, BUS, GUI deu tham chieu DTO.
    // =========================================================================
    public static class CauHinhUngDung
    {
        public const string TenTepChung = "appsettings.json";
        public const string TenTepRieng = "appsettings.Local.json";

        // Gia tri mac dinh khi khong co tep cau hinh nao (trung ban tren Git).
        private const string MayChuMacDinh = @".\SQLEXPRESS";
        private const string TenCsdlMacDinh = "QuanLyDuongSatV2";
        private const string TenCsdlKiemThuMacDinh = "QuanLyDuongSatV2_Test";

        private static bool _daNap;

        public static string MayChu { get; private set; } = MayChuMacDinh;

        public static string TenCsdl { get; private set; } = TenCsdlMacDinh;

        public static string TenCsdlKiemThu { get; private set; } = TenCsdlKiemThuMacDinh;

        // Chuoi ket noi ghi thang trong cau hinh (rong = ghep tu MayChu + TenCsdl).
        public static string ChuoiKetNoiTuyChon { get; private set; } = string.Empty;

        public static bool TuDongDoMayChu { get; private set; } = true;

        // Thu muc da doc cau hinh - phuc vu bao loi khi app khong noi duoc CSDL.
        public static string ThuMucCauHinh { get; private set; } = string.Empty;

        // Nhung tep thuc su doc duoc, vi du "appsettings.json + appsettings.Local.json".
        public static string CacTepDaDoc { get; private set; } = "(chua nap)";

        // Tep cau hinh sai cu phap JSON thi ghi loi vao day thay vi im lang bo qua
        // (da gap 08/10: ".\SQLEXPRESS" thieu dau gach cheo doi -> ca tep bi bo,
        // app lang le chay bang gia tri mac dinh, rat kho doan ra).
        public static string LoiCauHinh { get; private set; } = string.Empty;

        public static bool CoLoiCauHinh => !string.IsNullOrEmpty(LoiCauHinh);

        // =====================================================================
        // Nap cau hinh. Goi mot lan luc khoi dong (App.OnStartup).
        // thuMuc rong => lay thu muc chua tep thuc thi.
        // =====================================================================
        public static void Nap(string? thuMuc = null)
        {
            if (_daNap) return;
            _daNap = true;

            ThuMucCauHinh = string.IsNullOrWhiteSpace(thuMuc)
                ? AppContext.BaseDirectory
                : thuMuc!;

            var daDoc = new List<string>();

            // Doc tep chung truoc, roi tep rieng ghi de len.
            foreach (string tenTep in new[] { TenTepChung, TenTepRieng })
            {
                if (DocMotTep(Path.Combine(ThuMucCauHinh, tenTep)))
                {
                    daDoc.Add(tenTep);
                }
            }

            CacTepDaDoc = daDoc.Count == 0 ? "(khong co)" : string.Join(" + ", daDoc);
        }

        // Nap lai tu dau - dung cho chuong trinh kiem thu khi can doi thu muc.
        public static void NapLai(string? thuMuc = null)
        {
            _daNap = false;
            MayChu = MayChuMacDinh;
            TenCsdl = TenCsdlMacDinh;
            TenCsdlKiemThu = TenCsdlKiemThuMacDinh;
            ChuoiKetNoiTuyChon = string.Empty;
            TuDongDoMayChu = true;
            LoiCauHinh = string.Empty;
            Nap(thuMuc);
        }

        // Doc 1 tep JSON; tra ve true neu tep ton tai va doc duoc.
        // Khoa nao khong co trong tep thi giu nguyen gia tri dang co.
        private static bool DocMotTep(string duongDan)
        {
            if (!File.Exists(duongDan)) return false;

            try
            {
                // File.ReadAllText tu bo BOM neu co.
                string noiDung = File.ReadAllText(duongDan);
                using JsonDocument tep = JsonDocument.Parse(noiDung, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                if (!tep.RootElement.TryGetProperty("KetNoiCsdl", out JsonElement muc)) return true;

                MayChu = LayChuoi(muc, "MayChu", MayChu);
                TenCsdl = LayChuoi(muc, "TenCsdl", TenCsdl);
                TenCsdlKiemThu = LayChuoi(muc, "TenCsdlKiemThu", TenCsdlKiemThu);
                ChuoiKetNoiTuyChon = LayChuoi(muc, "ChuoiKetNoi", ChuoiKetNoiTuyChon);
                TuDongDoMayChu = LayBool(muc, "TuDongDoMayChu", TuDongDoMayChu);
                return true;
            }
            catch (Exception ex)
            {
                // Cau hinh sai cu phap thi bo qua, dung gia tri dang co - app van
                // mo duoc de nguoi dung doc thong bao, khong crash luc khoi dong.
                // Nhung phai ghi lai loi, neu khong thi khong ai biet tep bi bo.
                string tenTep = Path.GetFileName(duongDan);
                LoiCauHinh = string.IsNullOrEmpty(LoiCauHinh)
                    ? $"{tenTep}: {ex.Message}"
                    : $"{LoiCauHinh} | {tenTep}: {ex.Message}";
                return false;
            }
        }

        private static string LayChuoi(JsonElement muc, string khoa, string macDinh)
        {
            if (!muc.TryGetProperty(khoa, out JsonElement gt)) return macDinh;
            if (gt.ValueKind != JsonValueKind.String) return macDinh;
            string? giaTri = gt.GetString();
            return string.IsNullOrWhiteSpace(giaTri) ? macDinh : giaTri.Trim();
        }

        private static bool LayBool(JsonElement muc, string khoa, bool macDinh)
        {
            if (!muc.TryGetProperty(khoa, out JsonElement gt)) return macDinh;
            return gt.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => macDinh
            };
        }

        // =====================================================================
        // Chuoi ket noi hoan chinh. tenCsdl rong => dung CSDL chinh.
        // =====================================================================
        public static string ChuoiKetNoi(string? tenCsdl = null)
        {
            if (!_daNap) Nap();

            string csdl = string.IsNullOrWhiteSpace(tenCsdl) ? TenCsdl : tenCsdl!.Trim();

            // Chuoi ghi thang trong cau hinh chi ap cho CSDL chinh.
            if (!string.IsNullOrWhiteSpace(ChuoiKetNoiTuyChon)
                && string.Equals(csdl, TenCsdl, StringComparison.OrdinalIgnoreCase))
            {
                return ChuoiKetNoiTuyChon;
            }

            return ChuoiKetNoiTheoMayChu(MayChu, csdl);
        }

        // Chuoi ket noi toi CSDL kiem thu (dung cho project Tests sau nay).
        public static string ChuoiKetNoiKiemThu() => ChuoiKetNoi(TenCsdlKiemThu);

        // Ghep chuoi ket noi cho mot may chu bat ky - de KetNoiCsdlHelper va lop
        // nay ghep giong nhau mot cho.
        public static string ChuoiKetNoiTheoMayChu(string mayChu, string? tenCsdl = null)
        {
            if (!_daNap) Nap();
            string csdl = string.IsNullOrWhiteSpace(tenCsdl) ? TenCsdl : tenCsdl!.Trim();
            return $"Server={mayChu};Database={csdl};Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5;";
        }
    }
}
