using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DTO.VanHanh;

namespace GUI.Views.Dialogs
{
    public partial class CanhBaoXungDotDialog : Window
    {
        public bool YeuCauMoBieuDo { get; private set; } = false;

        public CanhBaoXungDotDialog(
            List<XungDotKhuGianDTO> dsXungDot, 
            int maChuyenTauHienTai, 
            string soHieuMacTau, 
            string hanhTrinh, 
            DateTime ngayChay)
        {
            InitializeComponent();

            txtThongTinChuyenTau.Text = $"Chuyến tàu #{maChuyenTauHienTai}: {soHieuMacTau} ({hanhTrinh}) | Ngày chạy: {ngayChay:dd/MM/yyyy}";
            txtTongSoXungDot.Text = $"Phát hiện: {dsXungDot.Count} nguy cơ xung đột khu gian";

            int soDoiDau = dsXungDot.Count(x => x.LoaiXungDot == "DOI_DAU");
            int soGianCach = dsXungDot.Count(x => x.LoaiXungDot != "DOI_DAU");
            txtChiTietPhanLoai.Text = $"Đối đầu trực diện: {soDoiDau} | Vi phạm giãn cách an toàn: {soGianCach}";

            // Chuyển đổi dữ liệu sang ViewModel hiển thị trên DataGrid
            var listItems = new List<XungDotItemViewModel>();
            int stt = 1;
            foreach (var x in dsXungDot)
            {
                string tauKhac = (x.MaChuyenTau1 == maChuyenTauHienTai) ? x.SoHieuMacTau2 : x.SoHieuMacTau1;
                string loaiTauKhac = (x.MaChuyenTau1 == maChuyenTauHienTai) ? x.LoaiTau2 : x.LoaiTau1;
                bool laDoiDau = (x.LoaiXungDot == "DOI_DAU");

                string khuGian = (!string.IsNullOrEmpty(x.TenGaDau) && !string.IsNullOrEmpty(x.TenGaCuoi))
                    ? $"{x.TenGaDau} — {x.TenGaCuoi}"
                    : (!string.IsNullOrEmpty(x.TenGaDau) ? x.TenGaDau : "Khu gian chạy tàu");

                string lyTrinhStr = x.LyTrinhUocTinhKm > 0 ? $" (Km {x.LyTrinhUocTinhKm:F1})" : "";

                string giaiPhap = !string.IsNullOrEmpty(x.TenGaDeXuatTranh)
                    ? $"Cho tàu [{x.SoHieuTauBiTranh}] dừng đỗ tránh tại ga {x.TenGaDeXuatTranh} khoảng {x.SoPhutDungDeXuat} phút."
                    : (!string.IsNullOrEmpty(x.MoTaChiTiet) ? x.MoTaChiTiet : "Điều độ viên điều chỉnh giãn cách đỗ ga phù hợp.");

                listItems.Add(new XungDotItemViewModel
                {
                    STT = stt++,
                    LoaiXungDotText = laDoiDau ? "ĐỐI ĐẦU KHU GIAN ĐƠN" : "VI PHẠM GIÃN CÁCH",
                    LoaiBgColor = laDoiDau ? "#FEF2F2" : "#FFFBEB",
                    LoaiBorderColor = laDoiDau ? "#FCA5A5" : "#FDE68A",
                    LoaiFgColor = laDoiDau ? "#B91C1C" : "#B45309",
                    TauDoiDau = $"Tàu [{tauKhac}]",
                    ThoiDiem = x.ThoiDiemXungDot.ToString("HH:mm"),
                    KhuGianLyTrinh = $"{khuGian}{lyTrinhStr}",
                    GiaiPhapDeXuat = giaiPhap
                });
            }

            dgDanhSachXungDot.ItemsSource = listItems;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            YeuCauMoBieuDo = false;
            Close();
        }

        private void BtnMoBieuDo_Click(object sender, RoutedEventArgs e)
        {
            YeuCauMoBieuDo = true;
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }

    public class XungDotItemViewModel
    {
        public int STT { get; set; }
        public string LoaiXungDotText { get; set; } = "";
        public string LoaiBgColor { get; set; } = "#FEF2F2";
        public string LoaiBorderColor { get; set; } = "#FCA5A5";
        public string LoaiFgColor { get; set; } = "#B91C1C";
        public string TauDoiDau { get; set; } = "";
        public string ThoiDiem { get; set; } = "";
        public string KhuGianLyTrinh { get; set; } = "";
        public string GiaiPhapDeXuat { get; set; } = "";
    }
}
