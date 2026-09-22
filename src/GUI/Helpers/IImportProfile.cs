using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace GUI.Helpers
{
   // Định nghĩa cấu hình cột hiển thị trên DataGrid Preview của hộp thoại Import
    
    public class ImportColumnDefinition
    {
        public string TieuDe { get; set; } = "";
        public string TenThuocTinh { get; set; } = "";
        public int ChieuRong { get; set; } = 120;
        public TextAlignment CanLe { get; set; } = TextAlignment.Left;
        public bool IsBold { get; set; } = false;
        public bool IsConsolas { get; set; } = false;

        public ImportColumnDefinition(string tieuDe, string tenThuocTinh, int chieuRong = 120, TextAlignment canLe = TextAlignment.Left, bool isBold = false, bool isConsolas = false)
        {
            TieuDe = tieuDe;
            TenThuocTinh = tenThuocTinh;
            ChieuRong = chieuRong;
            CanLe = canLe;
            IsBold = isBold;
            IsConsolas = isConsolas;
        }
    }

   // Lớp cơ sở cho mỗi dòng dữ liệu xem trước khi Import (tuân thủ Option 4: Left-Border Accent Card)
    
    public abstract class ImportRowBase
    {
        public int Dong { get; set; }
        public bool HopLe { get; set; }
        public bool LaCapNhat { get; set; } = false;
        public string ThongBaoKiemTra { get; set; } = "";

        public Brush TinhTrangBorderBrush => HopLe 
            ? (LaCapNhat 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")) // Xanh dương cho cập nhật
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"))) // Xanh lá cho thêm mới
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));    // Đỏ cho lỗi hoặc trùng

        public Brush TinhTrangColor => HopLe 
            ? (LaCapNhat 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D4ED8"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"))) 
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));

        public SymbolRegular TinhTrangSymbol => HopLe 
            ? (LaCapNhat ? SymbolRegular.ArrowSyncCircle24 : SymbolRegular.CheckmarkCircle24)
            : SymbolRegular.DismissCircle24;
    }

   // Giao diện chuẩn cho một hồ sơ cấu hình nhập tệp
    
    public interface IImportProfile
    {
        // Tiêu đề hiển thị trên Header hộp thoại
        
        string TieuDeDialog { get; }

        // Tên tệp CSV/Excel mẫu mặc định khi người dùng tải xuống
        
        string TenTepMauMacDinh { get; }

        // Nội dung tệp CSV mẫu chuẩn (bao gồm Header và các dòng ví dụ)
        
        string NoiDungCsvMau { get; }

        // Danh sách tiêu đề cột mẫu cho tệp Excel / CSV
        
        List<string> TieuDeCotMau { get; }

        // Danh sách các dòng dữ liệu ví dụ mẫu
        
        List<string[]> CacDongDuLieuMau { get; }

        // Ghi chú giải thích cho từng cột (dùng làm Comment hướng dẫn trong Excel)
        
        List<string>? GhiChuCotMau => null;

        // Danh sách các cột thuộc tính sẽ hiển thị trên bảng Preview
        
        List<ImportColumnDefinition> CacCotPreview { get; }

        // Danh sách định nghĩa các cột cần nhận diện thông minh (hỗ trợ nhiều từ đồng nghĩa)
        
        List<SmartColumnTarget> CacCotMucTieu { get; }

        // Cấu hình cho phép cập nhật dữ liệu nếu bản ghi đã tồn tại trong CSDL
        
        bool HoTroCapNhat => true;

        // Chuẩn bị nạp trước dữ liệu đối chiếu từ CSDL (chạy 1 lần trước khi đọc file)
        
        void ChuanBiDuLieuDoiChieu();

        // Làm mới bộ đệm theo dõi bản ghi trùng lặp nội bộ trong file trước mỗi lượt duyệt đánh giá
        
        void LamMoiTrangThaiDuyetTep();

        // Phân tích dữ liệu thông minh theo từng dòng đã được ánh xạ tên cột
        
        ImportRowBase KiemTraVaPhanTichDongThongMinh(SmartRowData row, int soDong, bool choPhepCapNhat = false);

        // Phân tích mảng tokens của 1 dòng (tương thích ngược)
        
        ImportRowBase KiemTraVaPhanTichDong(string[] tokens, int soDong);

        // Lưu hàng loạt các bản ghi hợp lệ vào Cơ sở dữ liệu (hỗ trợ cả Thêm mới và Cập nhật)
        
        // Số bản ghi lưu thành công
        int LuuDuLieuVaoCSDL(IEnumerable<ImportRowBase> danhSachHopLe, bool choPhepCapNhat, out string thongBao);
    }
}
