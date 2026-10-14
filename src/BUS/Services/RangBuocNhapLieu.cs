using System.Text;
using System.Text.RegularExpressions;

namespace BUS.Services
{
    // =========================================================================
    // RANG BUOC NHAP LIEU CO BAN (Dat chot 14/10) - dung chung cho dialog va Service
    // (Service kiem lai truoc khi ghi). Moi ham tra ve CAU LOI de hien ngay duoi o nhap,
    // null = hop le.
    //
    //   Ho ten         : bat buoc; chi chu cai (co dau) + khoang trang; khong so, khong ky tu dac biet;
    //                    >= 2 tu; toi da 50 ky tu (khoang trang thua tu gop).
    //   So dien thoai  : bat buoc; chi chu so; bat dau bang 0; dung 10 chu so.
    //   Ngay sinh      : bat buoc; khong sau hom nay; tu 18 den 62 tuoi.
    //   Han kham SK    : bat buoc; khong qua 2 nam ke tu hom nay; ho so moi phai con han.
    //   Phan so hieu   : (dau may D19E-907, toa NML-222) chi chu so; 3-4 chu so; khong toan so 0.
    //   Ten xi nghiep  : bat buoc; chi chu, so, khoang trang; co chu cai; 5-50 ky tu.
    //   So km / nam SX / tu trong / tai trong: bien tren - bien duoi o PhuongTienService.
    //
    // Ham LoiKyTu*: chi xet ky tu (so / ky tu dac biet / chu cai) -> dialog bao NGAY khi go.
    // Ham KiemTra*: day du (ca do dai, so tu, bien) -> dialog bao khi roi o va khi bam Luu.
    // =========================================================================
    public static class RangBuocNhapLieu
    {
        public const int HoTenToiDa = 50;
        public const int SoDienThoaiDoDai = 10;
        public const int TuoiToiThieu = 18;
        public const int TuoiToiDa = 62;              // tuoi nghi huu toi da (BLLD 2019, lo trinh nam 62)
        public const int HanKhamToiDaNam = 2;
        public const int PhanSoToiThieu = 3;
        public const int PhanSoToiDa = 4;
        public const int TenXiNghiepToiThieu = 5;
        public const int TenXiNghiepToiDa = 50;       // NVARCHAR(50)

        // Bo khoang trang dau / cuoi, gop nhieu khoang trang lien nhau, chuan Unicode dung san (go bang
        // bo go tieng Viet co the ra chu to hop -> char.IsLetter tra false voi dau rieng)
        public static string ChuanHoa(string? s)
            => Regex.Replace((s ?? "").Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();

        // ---------------------------------------------------------------------
        // HO TEN
        // ---------------------------------------------------------------------

        public static string? LoiKyTuHoTen(string? hoTen)
        {
            string s = ChuanHoa(hoTen);
            if (s.Any(char.IsDigit)) return "Họ tên không được chứa chữ số.";
            if (s.Any(c => !char.IsLetter(c) && c != ' '))
                return "Họ tên không được chứa ký tự đặc biệt (chỉ gồm chữ cái và khoảng trắng).";
            return null;
        }

        public static string? KiemTraHoTen(string? hoTen)
        {
            string s = ChuanHoa(hoTen);
            if (s.Length == 0) return "Vui lòng nhập họ và tên.";
            if (LoiKyTuHoTen(s) is { } loi) return loi;
            if (s.Length > HoTenToiDa) return $"Họ tên tối đa {HoTenToiDa} ký tự (đang có {s.Length}).";
            if (s.Split(' ').Length < 2) return "Họ tên phải có ít nhất 2 từ (họ và tên), ví dụ: Nguyễn Văn An.";
            return null;
        }

        // ---------------------------------------------------------------------
        // SO DIEN THOAI
        // ---------------------------------------------------------------------

        public static string? LoiKyTuSoDienThoai(string? sdt)
        {
            string s = sdt?.Trim() ?? "";
            if (s.Any(char.IsLetter)) return "Số điện thoại không được chứa chữ cái.";
            if (s.Any(c => !char.IsAsciiDigit(c)))
                return "Số điện thoại không được chứa khoảng trắng hay ký tự đặc biệt (chỉ gồm chữ số).";
            return null;
        }

        public static string? KiemTraSoDienThoai(string? sdt)
        {
            string s = sdt?.Trim() ?? "";
            if (s.Length == 0) return "Vui lòng nhập số điện thoại.";
            if (LoiKyTuSoDienThoai(s) is { } loi) return loi;
            if (s[0] != '0') return "Số điện thoại phải bắt đầu bằng số 0.";
            if (s.Length != SoDienThoaiDoDai) return $"Số điện thoại phải gồm đúng {SoDienThoaiDoDai} chữ số (đang có {s.Length}).";
            return null;
        }

        // ---------------------------------------------------------------------
        // NGAY SINH / HAN KHAM
        // ---------------------------------------------------------------------

        public static int TinhTuoi(DateTime ngaySinh, DateTime homNay)
        {
            int tuoi = homNay.Year - ngaySinh.Year;
            if (ngaySinh.Date > homNay.Date.AddYears(-tuoi)) tuoi--;
            return tuoi;
        }

        public static string? KiemTraNgaySinh(DateTime? ngaySinh, DateTime homNay)
        {
            if (ngaySinh is null) return "Vui lòng chọn ngày sinh.";
            DateTime ns = ngaySinh.Value.Date;
            if (ns > homNay.Date) return "Ngày sinh không được sau ngày hôm nay.";

            int tuoi = TinhTuoi(ns, homNay);
            if (tuoi < TuoiToiThieu)
                return $"Nhân viên phải đủ {TuoiToiThieu} tuổi (sinh từ ngày {homNay.Date.AddYears(-TuoiToiThieu):dd/MM/yyyy} trở về trước), " +
                       $"hiện mới {tuoi} tuổi.";
            if (tuoi > TuoiToiDa)
                return $"Nhân viên đã {tuoi} tuổi, quá tuổi làm việc tối đa {TuoiToiDa}. Kiểm tra lại ngày sinh.";
            return null;
        }

        // Ho so dang sua duoc phep da het han (su that - van luu, Lich ca bao do); ho so moi thi khong
        public static string? KiemTraHanKham(DateTime? hanKham, bool laHoSoMoi, DateTime homNay)
        {
            if (hanKham is null) return "Vui lòng chọn hạn khám sức khỏe.";
            DateTime han = hanKham.Value.Date;
            DateTime toiDa = homNay.Date.AddYears(HanKhamToiDaNam);
            if (han > toiDa)
                return $"Hạn khám không được quá {HanKhamToiDaNam} năm kể từ hôm nay (muộn nhất {toiDa:dd/MM/yyyy}).";
            if (laHoSoMoi && han < homNay.Date)
                return "Hồ sơ mới phải có hạn khám còn hiệu lực (từ hôm nay trở đi).";
            return null;
        }

        // ---------------------------------------------------------------------
        // PHAN SO CUA SO HIEU PHUONG TIEN (sau tien to "D19E-", "NML-" ...)
        // ---------------------------------------------------------------------

        public static string? LoiKyTuPhanSo(string? phanSo)
        {
            string s = phanSo ?? "";
            if (s.Any(char.IsLetter)) return "Phần số hiệu chỉ được chứa chữ số, không được chứa chữ cái.";
            if (s.Any(char.IsWhiteSpace)) return "Phần số hiệu không được chứa khoảng trắng.";
            if (s.Any(c => !char.IsAsciiDigit(c))) return "Phần số hiệu không được chứa ký tự đặc biệt (chỉ gồm chữ số).";
            return null;
        }

        public static string? KiemTraPhanSoHieu(string? phanSo, string tienTo)
        {
            string s = phanSo ?? "";
            if (s.Length == 0) return $"Vui lòng nhập phần số sau \"{tienTo}\" (ví dụ {tienTo}907).";
            if (LoiKyTuPhanSo(s) is { } loi) return loi;
            if (s.Length < PhanSoToiThieu || s.Length > PhanSoToiDa)
                return $"Phần số hiệu phải có {PhanSoToiThieu}–{PhanSoToiDa} chữ số (đang có {s.Length}).";
            if (s.All(c => c == '0')) return "Phần số hiệu không được toàn số 0.";
            return null;
        }

        // ---------------------------------------------------------------------
        // TEN XI NGHIEP (don vi quan ly dau may)
        // ---------------------------------------------------------------------

        public static string? LoiKyTuTenXiNghiep(string? ten)
        {
            string s = ChuanHoa(ten);
            if (s.Any(c => !char.IsLetterOrDigit(c) && c != ' '))
                return "Tên xí nghiệp không được chứa ký tự đặc biệt (chỉ gồm chữ, số và khoảng trắng).";
            return null;
        }

        public static string? KiemTraTenXiNghiep(string? ten)
        {
            string s = ChuanHoa(ten);
            if (s.Length == 0) return "Vui lòng chọn hoặc nhập xí nghiệp quản lý.";
            if (LoiKyTuTenXiNghiep(s) is { } loi) return loi;
            if (!s.Any(char.IsLetter)) return "Tên xí nghiệp phải có chữ cái, không được chỉ gồm chữ số.";
            if (s.Length < TenXiNghiepToiThieu || s.Length > TenXiNghiepToiDa)
                return $"Tên xí nghiệp phải từ {TenXiNghiepToiThieu} đến {TenXiNghiepToiDa} ký tự (đang có {s.Length}).";
            return null;
        }
    }
}
