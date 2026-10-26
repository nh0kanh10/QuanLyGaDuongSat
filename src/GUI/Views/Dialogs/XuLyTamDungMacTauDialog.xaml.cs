using System;
using System.Data;
using System.Windows;
using System.Windows.Input;

namespace GUI.Views.Dialogs
{
    public partial class XuLyTamDungMacTauDialog : Window
    {
        public int MaMacTau { get; }
        public string SoHieuMacTau { get; }
        public bool CoXacNhan { get; private set; } = false;
        public bool HuyChuyenTuongLai => radHuyChuyen?.IsChecked == true;
        public string LyDoHuy => txtLyDoHuy?.Text.Trim() ?? string.Empty;

        public XuLyTamDungMacTauDialog(int maMacTau, string soHieu, string hanhTrinh, DataTable dtChuyenTuongLai)
        {
            InitializeComponent();
            MaMacTau = maMacTau;
            SoHieuMacTau = soHieu;

            txtSoHieuMac.Text = soHieu;
            txtHanhTrinh.Text = hanhTrinh;
            txtLyDoHuy.Text = $"Tạm ngừng khai thác mác tàu {soHieu}";

            int soChuyen = dtChuyenTuongLai?.Rows.Count ?? 0;
            txtSoChuyenTuongLai.Text = $"{soChuyen} chuyến";
            txtMoTaCanhBao.Text = $"Phát hiện có {soChuyen} chuyến tàu kế hoạch trong tương lai chưa xuất phát. Mác tàu sau khi tạm ngừng sẽ bị khóa tạo chuyến mới. Vui lòng chọn cách xử lý các chuyến tàu này bên dưới:";

            if (dtChuyenTuongLai != null)
            {
                // Thêm cột hiển thị thân thiện nếu chưa có
                if (!dtChuyenTuongLai.Columns.Contains("TrangThaiHienThi"))
                {
                    dtChuyenTuongLai.Columns.Add("TrangThaiHienThi", typeof(string));
                    foreach (DataRow r in dtChuyenTuongLai.Rows)
                    {
                        string tt = r["TrangThai"]?.ToString() ?? "";
                        r["TrangThaiHienThi"] = tt switch
                        {
                            "DA_LEN_LICH" => "Đã lên lịch",
                            "SAN_SANG" => "Sẵn sàng",
                            _ => tt
                        };
                    }
                }

                dgChuyenTuongLai.ItemsSource = dtChuyenTuongLai.DefaultView;
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CoXacNhan = false;
                DialogResult = false;
                Close();
            }
        }

        private void RadPhuongAn_Checked(object sender, RoutedEventArgs e)
        {
            if (pnlLyDoHuy == null || txtNutXacNhan == null) return;

            if (radHuyChuyen?.IsChecked == true)
            {
                pnlLyDoHuy.Visibility = Visibility.Visible;
                txtNutXacNhan.Text = "Hủy Chuyến & Tạm Ngừng Mác";
            }
            else
            {
                pnlLyDoHuy.Visibility = Visibility.Collapsed;
                txtNutXacNhan.Text = "Chỉ Tạm Ngừng Mác Tàu";
            }
        }

        private void BtnXacNhan_Click(object sender, RoutedEventArgs e)
        {
            if (HuyChuyenTuongLai && string.IsNullOrWhiteSpace(LyDoHuy))
            {
                ThongBaoDialog.CanhBao("Vui lòng nhập lý do hủy chuyến tàu khi chọn Phương án B!", "Thiếu Thông Tin");
                txtLyDoHuy.Focus();
                return;
            }

            CoXacNhan = true;
            DialogResult = true;
            Close();
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            CoXacNhan = false;
            DialogResult = false;
            Close();
        }
    }
}
