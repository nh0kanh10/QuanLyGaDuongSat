using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BUS.Services
{
    // =========================================================================
    // Bam va kiem mat khau (Q12, Dat chot 08/11: PBKDF2 co san trong .NET 8, khop chu
    // "Argon2/PBKDF2" o muc yeu cau bao mat cua bao cao, khong them goi NuGet).
    //
    // Dinh dang luu trong phanquyen.TaiKhoan.MatKhauHash (VARCHAR 255):
    //     PBKDF2$SHA256$<so vong>$<muoi base64>$<bam base64>
    // Muoi 16 byte ngau nhien rieng tung tai khoan, bam 32 byte, 100.000 vong.
    // So vong nam trong chuoi -> sau nay tang so vong khong lam hong mat khau cu.
    //
    // Chuoi bam BCrypt cu ($2a$12$...) cua script goc: KhongPhaiDinhDangMoi -> dang nhap bao
    // "nho quan tri vien dat lai" (seed S20261108_1100 da dat lai cho tai khoan demo).
    // =========================================================================
    public static class MatKhauHelper
    {
        public const int SoVongMacDinh = 100_000;
        private const int DoDaiMuoi = 16;
        private const int DoDaiBam = 32;
        private const string TienTo = "PBKDF2";
        private const string ThuatToan = "SHA256";

        public const int DoDaiToiThieu = 8;
        public const int DoDaiToiDa = 64;

        public static string TaoChuoiBam(string matKhau, int soVong = SoVongMacDinh)
        {
            if (matKhau == null) throw new ArgumentNullException(nameof(matKhau));
            byte[] muoi = RandomNumberGenerator.GetBytes(DoDaiMuoi);
            byte[] bam = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(matKhau), muoi, soVong,
                                                   HashAlgorithmName.SHA256, DoDaiBam);
            return $"{TienTo}${ThuatToan}${soVong}${Convert.ToBase64String(muoi)}${Convert.ToBase64String(bam)}";
        }

        public static bool LaDinhDangMoi(string? chuoiBam)
            => !string.IsNullOrEmpty(chuoiBam) && chuoiBam.StartsWith(TienTo + "$", StringComparison.Ordinal);

        // So sanh thoi gian hang (FixedTimeEquals). Chuoi bam hong / sai dinh dang -> false, khong nem loi.
        public static bool KiemTra(string? matKhau, string? chuoiBam)
        {
            if (string.IsNullOrEmpty(matKhau) || !LaDinhDangMoi(chuoiBam)) return false;

            string[] phan = chuoiBam!.Split('$');
            if (phan.Length != 5 || phan[1] != ThuatToan) return false;
            if (!int.TryParse(phan[2], out int soVong) || soVong < 1_000 || soVong > 10_000_000) return false;

            byte[] muoi, bamLuu;
            try
            {
                muoi = Convert.FromBase64String(phan[3]);
                bamLuu = Convert.FromBase64String(phan[4]);
            }
            catch (FormatException)
            {
                return false;
            }
            if (muoi.Length == 0 || bamLuu.Length == 0) return false;

            byte[] bam = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(matKhau), muoi, soVong,
                                                   HashAlgorithmName.SHA256, bamLuu.Length);
            return CryptographicOperations.FixedTimeEquals(bam, bamLuu);
        }

        // Quy tac mat khau moi (dat lai / doi mat khau): 8-64 ky tu, co chu cai va chu so,
        // khong co khoang trang. null = hop le.
        public static string? KiemTraDoManh(string? matKhau)
        {
            if (string.IsNullOrEmpty(matKhau)) return "Chưa nhập mật khẩu.";
            if (matKhau.Length < DoDaiToiThieu) return $"Mật khẩu phải có ít nhất {DoDaiToiThieu} ký tự.";
            if (matKhau.Length > DoDaiToiDa) return $"Mật khẩu tối đa {DoDaiToiDa} ký tự.";
            if (matKhau.Any(char.IsWhiteSpace)) return "Mật khẩu không được chứa khoảng trắng.";
            if (!matKhau.Any(char.IsLetter) || !matKhau.Any(char.IsDigit))
                return "Mật khẩu phải có cả chữ cái và chữ số.";
            return null;
        }
    }
}
