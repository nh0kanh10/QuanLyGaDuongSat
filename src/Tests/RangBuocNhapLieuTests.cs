using BUS.Services;

namespace Tests
{
    // Rang buoc nhap lieu co ban 14/10 (BUS/Services/RangBuocNhapLieu.cs): null = hop le, chuoi = cau loi.
    public class RangBuocNhapLieuTests
    {
        private static readonly DateTime HomNay = new(2026, 10, 14);

        [Theory]
        [InlineData("Nguyễn Văn An", null)]
        [InlineData("  Lê   Thị  Hoa ", null)]                       // khoang trang thua tu gop
        [InlineData("", "Vui lòng nhập")]
        [InlineData("Nguyễn Văn 2", "chữ số")]
        [InlineData("Nguyễn Văn @n", "ký tự đặc biệt")]
        [InlineData("Nguyễn_Văn An", "ký tự đặc biệt")]
        [InlineData("An", "ít nhất 2 từ")]
        [InlineData("Nguyễn Văn Aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "tối đa 50")]
        public void HoTen(string hoTen, string? loiChua)
            => KiemLoi(RangBuocNhapLieu.KiemTraHoTen(hoTen), loiChua);

        [Theory]
        [InlineData("0912345678", null)]
        [InlineData("", "Vui lòng nhập")]
        [InlineData("09123a5678", "chữ cái")]
        [InlineData("0912 345 678", "ký tự đặc biệt")]
        [InlineData("1912345678", "bắt đầu bằng số 0")]
        [InlineData("091234567", "đúng 10")]
        [InlineData("09123456789", "đúng 10")]
        public void SoDienThoai(string sdt, string? loiChua)
            => KiemLoi(RangBuocNhapLieu.KiemTraSoDienThoai(sdt), loiChua);

        [Theory]
        [InlineData("2008-10-14", null)]          // vua tron 18 tuoi
        [InlineData("2008-10-15", "đủ 18 tuổi")]  // con 1 ngay nua moi 18
        [InlineData("1964-10-15", null)]          // 61 tuoi
        [InlineData("1963-10-14", "quá tuổi")]    // 63 tuoi
        [InlineData("2026-10-15", "sau ngày hôm nay")]
        public void NgaySinh(string ngay, string? loiChua)
            => KiemLoi(RangBuocNhapLieu.KiemTraNgaySinh(DateTime.Parse(ngay), HomNay), loiChua);

        [Fact]
        public void NgaySinh_ChuaChon_Loi()
            => Assert.Contains("Vui lòng chọn", RangBuocNhapLieu.KiemTraNgaySinh(null, HomNay));

        [Theory]
        [InlineData("2027-10-14", true, null)]
        [InlineData("2028-10-15", false, "quá 2 năm")]
        [InlineData("2026-10-13", true, "còn hiệu lực")]   // ho so moi da het han
        [InlineData("2026-10-13", false, null)]           // ho so dang sua: het han van luu (su that)
        public void HanKham(string han, bool laHoSoMoi, string? loiChua)
            => KiemLoi(RangBuocNhapLieu.KiemTraHanKham(DateTime.Parse(han), laHoSoMoi, HomNay), loiChua);

        [Theory]
        [InlineData("907", null)]
        [InlineData("1234", null)]
        [InlineData("", "Vui lòng nhập")]
        [InlineData("90A", "chữ cái")]
        [InlineData("9 07", "khoảng trắng")]
        [InlineData("90#", "ký tự đặc biệt")]
        [InlineData("97", "3–4 chữ số")]
        [InlineData("12345", "3–4 chữ số")]
        [InlineData("000", "toàn số 0")]
        public void PhanSoHieu(string phanSo, string? loiChua)
            => KiemLoi(RangBuocNhapLieu.KiemTraPhanSoHieu(phanSo, "D19E-"), loiChua);

        [Theory]
        [InlineData("Xí nghiệp Đầu máy Hà Nội", null)]
        [InlineData("", "Vui lòng")]
        [InlineData("XN Hà Nội!", "ký tự đặc biệt")]
        [InlineData("12345", "chữ cái")]
        [InlineData("XN 1", "từ 5 đến 50")]
        public void TenXiNghiep(string ten, string? loiChua)
            => KiemLoi(RangBuocNhapLieu.KiemTraTenXiNghiep(ten), loiChua);

        private static void KiemLoi(string? loi, string? loiChua)
        {
            if (loiChua == null) Assert.Null(loi);
            else Assert.Contains(loiChua, loi);
        }
    }
}
