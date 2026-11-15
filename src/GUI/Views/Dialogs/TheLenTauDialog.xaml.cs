using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using QRCoder;

namespace GUI.Views.Dialogs
{
    public partial class TheLenTauDialog : Window
    {
        public TheLenTauDialog()
        {
            InitializeComponent();
        }

        public void SetThongTinVe(
            string maPNR,
            string maVeCode,
            string macTau,
            string gaDi,
            string gaDen,
            string gioDi,
            string tenKhach,
            string cccd,
            string toaXe,
            int soGhe,
            decimal giaVe)
        {
            txtMaPNR.Text = $"Mã Đơn: {maPNR}";
            txtMaVeCode.Text = maVeCode;
            txtMacTau.Text = macTau;
            txtGaDi.Text = gaDi.ToUpper();
            txtGaDen.Text = gaDen.ToUpper();
            txtGioDi.Text = gioDi;
            txtTenKhach.Text = tenKhach.ToUpper();
            txtCCCD.Text = cccd;
            txtToa.Text = toaXe;
            txtSoGhe.Text = $"Ghế số {soGhe}";
            txtGiaVe.Text = $"{giaVe:N0} VNĐ";

            TaoMaQrChoVe(maVeCode, cccd, macTau, soGhe);
        }

        private void TaoMaQrChoVe(string maVe, string cccd, string tau, int ghe)
        {
            try
            {
                string payload = $"VNR|TICKET|{maVe}|{tau}|GHE:{ghe}|CCCD:{cccd}";
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(10);

                var bitmap = new BitmapImage();
                using (var ms = new MemoryStream(qrBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                }
                bitmap.Freeze();
                imgQrCode.Source = bitmap;
            }
            catch
            {
                // Neu loi QR thi bo qua, khong lam crash dialog
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnInVe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    printDlg.PrintVisual(bdInVe, $"TheLenTau_{txtMaVeCode.Text}");
                    ThongBaoDialog.ThanhCong("Lệnh in thẻ lên tàu đã được gửi thành công!", "Thông Báo");
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể thực hiện in: {ex.Message}", "Lỗi In Ấn");
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
