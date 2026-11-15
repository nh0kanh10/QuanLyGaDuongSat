using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BUS.Services;
using DTO.Common;
using GUI.Views.Dialogs;

namespace GUI.Views.Dialogs
{
    public partial class DoiGheDialog : Window
    {
        private readonly VeService _veService = new();

        private int _maVeCu;
        private string _maVeCodeCu = "";
        private string _maPNR = "";
        private int _maChuyenTau;
        private int _maGaDi;
        private int _maGaDen;
        private decimal _giaVeCu;
        private int _maChoMoi = 0;
        private int _soGheMoi = 0;
        private string _tenToaMoi = "";
        private string _tenHK = "";
        private string _loaiTau = "TAU_CHO";
        private decimal _giaVeMoi = 0;      // gia goc ghe moi (truoc giam) - truyen xuong giao dich
        private decimal _giaGocCu;          // gia goc + so tien giam cua ve cu: ghe moi giu cung ty le giam
        private decimal _giamCu;
        private decimal _chenhLech = 0;     // tinh bang VeService.TinhGiaDoiGhe, khop so ghi CSDL
        private Button? _btnGheDangChon = null;

        public string MaVeMoi { get; private set; } = "";

        public DoiGheDialog()
        {
            InitializeComponent();
        }

        public void KhoiTao(
            int maVe, 
            string maVeCode, 
            string maPNR, 
            int maChuyenTau, 
            int maGaDi, 
            int maGaDen, 
            string tenGaDi, 
            string tenGaDen, 
            string macTau, 
            string tenHK, 
            string toaHienTai, 
            int gheHienTai, 
            decimal giaVeHienTai,
            string loaiTau = "TAU_CHO",
            decimal? giaGocCu = null,
            decimal giamCu = 0)
        {
            _maVeCu = maVe;
            _maVeCodeCu = maVeCode;
            _maPNR = maPNR;
            _maChuyenTau = maChuyenTau;
            _maGaDi = maGaDi;
            _maGaDen = maGaDen;
            _giaVeCu = giaVeHienTai;
            _loaiTau = loaiTau;
            _giaGocCu = giaGocCu ?? giaVeHienTai;
            _giamCu = giaGocCu.HasValue ? giamCu : 0m;
            _tenHK = tenHK;

            lblMaVeCode.Text = maVeCode;
            lblMaPNR.Text = maPNR;
            lblHanhKhach.Text = $"HK: {tenHK}";
            lblHanhTrinh.Text = $"{tenGaDi} ➔ {tenGaDen}";
            lblMacTau.Text = macTau;
            lblChoHienTai.Text = $"{toaHienTai} - Ghế {gheHienTai}";
            lblGiaVeCu.Text = $"{_giaVeCu:N0} VNĐ";
            lblGiaVeMoi.Text = "Chưa chọn chỗ mới";
            lblChenhLech.Text = "0 VNĐ";

            LoadDanhSachToa();
        }

        private void LoadDanhSachToa()
        {
            try
            {
                DataTable dtToa = _veService.LayDanhSachToaTheoChuyen(_maChuyenTau, _maGaDi, _maGaDen);
                cboToaXe.ItemsSource = dtToa.DefaultView;
                cboToaXe.DisplayMemberPath = "NhanHieuToa";
                cboToaXe.SelectedValuePath = "MaToaXeKhach";

                if (dtToa.Rows.Count > 0)
                {
                    cboToaXe.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi tải danh sách toa xe: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void CboToaXe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboToaXe.SelectedItem is DataRowView row)
            {
                int maToa = Convert.ToInt32(row["MaToaXeKhach"]);
                string loaiToa = row["LoaiToa"].ToString() ?? "";
                _tenToaMoi = row["NhanHieuToa"].ToString() ?? "";
                LoadSoDoGhe(maToa, loaiToa);
            }
        }

        private void LoadSoDoGhe(int maToa, string loaiToa)
        {
            pnlDanhSachGhe.Children.Clear();
            _maChoMoi = 0;
            _btnGheDangChon = null;
            btnXacNhanDoiGhe.IsEnabled = false;
            lblGiaVeMoi.Text = "Chưa chọn chỗ mới";
            lblChenhLech.Text = "0 VNĐ";

            try
            {
                DataTable dtGhe = _veService.LaySoDoGhe(maToa, _maChuyenTau, _maGaDi, _maGaDen);

                foreach (DataRow row in dtGhe.Rows)
                {
                    int maCho = Convert.ToInt32(row["MaChoNgoi"]);
                    int soGhe = Convert.ToInt32(row["SoGhe"]);
                    string trangThai = row.Table.Columns.Contains("TrangThai") && row["TrangThai"] != DBNull.Value
                        ? row["TrangThai"].ToString() ?? "CON_TRONG"
                        : (row.Table.Columns.Contains("DaDat") && Convert.ToInt32(row["DaDat"]) == 1 ? "DA_DAT" : "CON_TRONG");
                    int? tangGiuong = row["TangGiuong"] != DBNull.Value ? Convert.ToInt32(row["TangGiuong"]) : null;

                    bool daBan = (trangThai == "DA_DAT" || trangThai == "DA_BAN");

                    var btn = new Button
                    {
                        Width = 56,
                        Height = 44,
                        Margin = new Thickness(4),
                        Tag = row,
                        Cursor = daBan ? Cursors.No : Cursors.Hand,
                        IsEnabled = !daBan
                    };

                    var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    var txtGhe = new TextBlock
                    {
                        Text = $"Ghế {soGhe}",
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    sp.Children.Add(txtGhe);

                    if (tangGiuong.HasValue)
                    {
                        var txtTang = new TextBlock
                        {
                            Text = $"T.{tangGiuong.Value}",
                            FontSize = 9,
                            Foreground = daBan ? Brushes.Gray : Brushes.DarkSlateGray,
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        sp.Children.Add(txtTang);
                    }

                    btn.Content = sp;

                    if (daBan)
                    {
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                        btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
                        btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

                        string tenGaDi = row.Table.Columns.Contains("TenGaDiVe") && row["TenGaDiVe"] != DBNull.Value ? row["TenGaDiVe"].ToString()! : "";
                        string tenGaDen = row.Table.Columns.Contains("TenGaDenVe") && row["TenGaDenVe"] != DBNull.Value ? row["TenGaDenVe"].ToString()! : "";
                        if (!string.IsNullOrEmpty(tenGaDi) && !string.IsNullOrEmpty(tenGaDen))
                        {
                            btn.ToolTip = $"Ghế số {soGhe}: Đã có khách đặt ({tenGaDi} → {tenGaDen}) trên chặng này";
                        }
                        else
                        {
                            btn.ToolTip = $"Ghế số {soGhe}: Đã có khách đặt trên chặng này";
                        }
                    }
                    else
                    {
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
                        btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                        btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                        btn.ToolTip = $"Ghế số {soGhe} (Còn trống - Click để chọn)";

                        btn.Click += (s, ev) =>
                        {
                            ChonGheMoi(btn, maCho, soGhe, loaiToa, tangGiuong);
                        };
                    }

                    pnlDanhSachGhe.Children.Add(btn);
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi tải sơ đồ ghế: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void ChonGheMoi(Button btn, int maCho, int soGhe, string loaiToa, int? tangGiuong)
        {
            // Reset button ghe cu
            if (_btnGheDangChon != null)
            {
                _btnGheDangChon.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
                _btnGheDangChon.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                _btnGheDangChon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
            }

            // Highlight button ghe moi
            _btnGheDangChon = btn;
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#002850"));
            btn.Foreground = Brushes.White;

            _maChoMoi = maCho;
            _soGheMoi = soGhe;

            try
            {
                _giaVeMoi = _veService.TinhGiaVe(loaiToa, _maGaDi, _maGaDen, tangGiuong, _loaiTau);
                var gia = VeService.TinhGiaDoiGhe(_giaGocCu, _giamCu, _giaVeMoi);
                _chenhLech = gia.ChenhLech;
                lblGiaVeMoi.Text = gia.GiamMoi > 0
                    ? $"{gia.ThucThuMoi:N0} VNĐ (giá {_giaVeMoi:N0} − giảm {gia.GiamMoi:N0}; {_tenToaMoi} - Ghế {soGhe})"
                    : $"{gia.ThucThuMoi:N0} VNĐ ({_tenToaMoi} - Ghế {soGhe})";

                decimal diff = _chenhLech;
                if (diff > 0)
                {
                    lblTieuDeChenhLech.Text = "Phụ thu nâng hạng:";
                    lblChenhLech.Text = $"+{diff:N0} VNĐ";
                    lblChenhLech.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C"));
                }
                else if (diff < 0)
                {
                    lblTieuDeChenhLech.Text = "Hoàn lại chênh lệch:";
                    lblChenhLech.Text = $"{diff:N0} VNĐ";
                    lblChenhLech.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                }
                else
                {
                    lblTieuDeChenhLech.Text = "Chênh lệch:";
                    lblChenhLech.Text = "0 VNĐ (Cùng hạng chỗ)";
                    lblChenhLech.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#003B73"));
                }

                btnXacNhanDoiGhe.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi tính giá vé mới: {ex.Message}", "Lỗi Giá Vé");
            }
        }

        private void BtnXacNhanDoiGhe_Click(object sender, RoutedEventArgs e)
        {
            if (_maChoMoi <= 0)
            {
                ThongBaoDialog.CanhBao("Vui lòng click chọn một ghế còn trống trên sơ đồ!", "Chưa Chọn Ghế");
                return;
            }

            decimal diff = _chenhLech;
            string chiPhiMsg = diff == 0 
                ? "Không phát sinh chi phí đổi chỗ."
                : (diff > 0 ? $"Phụ thu thêm: {diff:N0} VNĐ" : $"Hoàn lại cho khách: {Math.Abs(diff):N0} VNĐ");

            bool dongY = ThongBaoDialog.XacNhan(
                $"Xác nhận đổi chỗ ngồi cho hành khách {_tenHK}?\n\n" +
                $"• Chỗ ngồi mới: {_tenToaMoi} — Ghế {_soGheMoi}\n" +
                $"• Chi phí: {chiPhiMsg}\n\n" +
                "Chỗ ngồi cũ sẽ được mở lại để bán cho khách khác.",
                "Xác Nhận Đổi Ghế",
                "Đổi Ghế",
                "Bỏ Qua");

            if (!dongY) return;

            int maNhanVien = PhanQuyenService.MaTaiKhoanHienTai /* cot FK toi phanquyen.TaiKhoan, khong phai NhanVien */;
            if (_veService.DoiGheCungChuyen(_maVeCu, _maChoMoi, _giaVeMoi, maNhanVien, out string maVeMoi, out string thongBaoLoi))
            {
                MaVeMoi = maVeMoi;
                ThongBaoDialog.ThanhCong(
                    $"Đã đổi chỗ ngồi thành công!\n\n" +
                    $"• Chỗ ngồi mới: {_tenToaMoi} — Ghế {_soGheMoi}\n" +
                    $"• Mã vé mới: {maVeMoi}\n\n" +
                    "Thẻ lên tàu điện tử đã được cập nhật theo vị trí mới.",
                    "Đổi Chỗ Thành Công");

                DialogResult = true;
                Close();
            }
            else
            {
                ThongBaoDialog.Loi($"Đổi chỗ thất bại: {thongBaoLoi}", "Thất Bại");
            }
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
            DialogResult = false;
            Close();
        }
    }
}
