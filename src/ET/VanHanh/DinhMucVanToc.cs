using System;

namespace ET.VanHanh
{
    /// <summary>
    /// Thực thể quản lý Định Mức Vận Tốc Lữ Hành theo Loại Hình Vận Tải (Chuẩn UIC 406 & VNR)
    /// </summary>
    public class DinhMucVanToc
    {
        public string MaLoaiTau { get; set; } = "";
        public string TenLoaiTau { get; set; } = "";
        public decimal VanTocDinhMucKmh { get; set; } = 50.0m;
        public string? MoTaQuyChuan { get; set; }
        public string? MauNhanDien { get; set; } = "#0284C7";
        public int ThuTuHienThi { get; set; } = 1;
        public bool DangApDung { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;
        public string NguoiTao { get; set; } = "admin";
        public DateTime? NgayCapNhat { get; set; }
        public string? NguoiCapNhat { get; set; }
    }
}
