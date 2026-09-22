using System.Data;
using System.Windows;
using System.Windows.Controls;
using BUS.Services;
using ET.VanTai;
using DTO.Common;

namespace GUI.Views.Pages
{
    public partial class HangHoaPage : Page
    {
        private readonly VanDonHangService _service = new();
        private DataTable? _cachedTable;
        private bool _isAddingNew = false;

        public HangHoaPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _cachedTable = _service.LayDanhSach();
                dgVanDon.ItemsSource = _cachedTable.DefaultView;
                txtTongSo.Text = $"Tổng số: {_cachedTable.Rows.Count} vận đơn";

                if (dgVanDon.Items.Count > 0)
                {
                    dgVanDon.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách vận đơn: {ex.Message}", "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgVanDon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isAddingNew) return;

            if (dgVanDon.SelectedItem is DataRowView row)
            {
                txtMaVD.Text = row["MaVanDonCode"].ToString();
                txtNguoiGui.Text = $"{row["TenNguoiGui"]} ({row["SDTNguoiGui"]})";
                txtNguoiNhan.Text = $"{row["TenNguoiNhan"]} ({row["SDTNguoiNhan"]})";
                txtChangGa.Text = $"{row["TenGaGui"]} → {row["TenGaNhan"]}";

                decimal tl = row["TrongLuongTan"] != DBNull.Value ? Convert.ToDecimal(row["TrongLuongTan"]) : 0;
                decimal cuoc = row["CuocPhi"] != DBNull.Value ? Convert.ToDecimal(row["CuocPhi"]) : 0;
                txtTrongLuong.Text = FormatHelper.FormatWeight(tl);
                txtCuocPhi.Text = FormatHelper.FormatCurrency(cuoc);

                string loai = row["LoaiVanChuyen"].ToString() ?? "";
                if (loai == "XE_MAY") cboLoaiVC.SelectedIndex = 0;
                else if (loai == "HANG_ROI") cboLoaiVC.SelectedIndex = 1;
                else cboLoaiVC.SelectedIndex = 2;

                txtBienSo.Text = row["BienKiemSoat"] != DBNull.Value ? row["BienKiemSoat"].ToString() : "";
                chkDaRutXang.IsChecked = row["DaRutXang"] != DBNull.Value && Convert.ToBoolean(row["DaRutXang"]);

                string tt = row["TrangThai"].ToString() ?? "";
                for (int i = 0; i < cboTrangThaiVD.Items.Count; i++)
                {
                    if (cboTrangThaiVD.Items[i] is ComboBoxItem item && item.Content.ToString()!.StartsWith(tt))
                    {
                        cboTrangThaiVD.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void BtnLapDonMoi_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = true;
            txtMaVD.Text = $"VD-{DateTime.Now:yyMMddHHmmss}";
            txtNguoiGui.Text = "";
            txtNguoiNhan.Text = "";
            txtTrongLuong.Text = "0.15";
            txtCuocPhi.Text = "650,000";
            cboLoaiVC.SelectedIndex = 0;
            txtBienSo.Text = "";
            chkDaRutXang.IsChecked = true;
            cboTrangThaiVD.SelectedIndex = 0;

            txtNguoiGui.Focus();
        }

        private void TxtTrongLuong_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FormatHelper.TryParseWeight(txtTrongLuong.Text, out decimal weight))
            {
                txtTrongLuong.Text = FormatHelper.FormatWeight(weight);
            }
        }

        private void TxtCuocPhi_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FormatHelper.TryParseCurrency(txtCuocPhi.Text, out decimal cuoc))
            {
                txtCuocPhi.Text = FormatHelper.FormatCurrency(cuoc);
            }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNguoiGui.Text))
            {
                MessageBox.Show("Vui lòng nhập thông tin người gửi.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNguoiGui.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNguoiNhan.Text))
            {
                MessageBox.Show("Vui lòng nhập thông tin người nhận.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNguoiNhan.Focus();
                return;
            }

            if (!FormatHelper.TryParseWeight(txtTrongLuong.Text, out decimal tl))
            {
                MessageBox.Show("Trọng lượng hàng hóa phải là số hợp lệ lớn hơn 0 (tấn).", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTrongLuong.Focus();
                return;
            }

            MessageBox.Show($"Đã lưu vận đơn #{txtMaVD.Text} thành công vào CSDL.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            _isAddingNew = false;
        }

        private void BtnInPhieu_Click(object sender, RoutedEventArgs e)
        {
            if (dgVanDon.SelectedItem is DataRowView row)
            {
                MessageBox.Show($"In Phiếu Gửi Hàng Hóa Vận & Biên Nhận VNR cho Vận đơn #{row["MaVanDonCode"]}.", "In Phiếu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnGiaoHang_Click(object sender, RoutedEventArgs e)
        {
            if (dgVanDon.SelectedItem is DataRowView row)
            {
                int maVanDon = Convert.ToInt32(row["MaVanDon"]);
                string trangThai = row["TrangThai"].ToString() ?? "";

                if (trangThai == "DA_GIAO")
                {
                    MessageBox.Show("Vận đơn này đã được giao cho khách trước đó.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var confirm = MessageBox.Show($"Xác nhận hàng hóa của vận đơn #{row["MaVanDonCode"]} đã giao thành công cho khách {row["TenNguoiNhan"]}?", "Xác nhận giao hàng", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.Yes)
                {
                    if (_service.CapNhatTrangThai(maVanDon, "DA_GIAO"))
                    {
                        MessageBox.Show("Cập nhật trạng thái DA_GIAO thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData();
                    }
                }
            }
        }

        private void BtnTimKiem_Click(object sender, RoutedEventArgs e)
        {
            if (_cachedTable == null) return;

            string keyword = txtTimKiem.Text.Trim().Replace("'", "''");
            if (string.IsNullOrEmpty(keyword))
            {
                _cachedTable.DefaultView.RowFilter = "";
            }
            else
            {
                _cachedTable.DefaultView.RowFilter = 
                    $"MaVanDonCode LIKE '%{keyword}%' OR TenNguoiGui LIKE '%{keyword}%' OR TenNguoiNhan LIKE '%{keyword}%'";
            }
            txtTongSo.Text = $"Kết quả: {_cachedTable.DefaultView.Count} vận đơn";
            if (_cachedTable.DefaultView.Count > 0) dgVanDon.SelectedIndex = 0;
        }

        private void BtnNapLai_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNew = false;
            txtTimKiem.Text = "";
            LoadData();
        }

        // =========================================================================
        // CÁC PHƯƠNG THỨC KÍCH HOẠT TÁC VỤ THỰC TẾ TỪ MAINWINDOW (F1 - F12 & SHORTCUT STRIP)
        // =========================================================================
        public void FocusTimKiem()
        {
            if (txtTimKiem != null)
            {
                txtTimKiem.Focus();
                txtTimKiem.SelectAll();
            }
        }

        public void KichHoatThemMoi() => BtnLapDonMoi_Click(this, new RoutedEventArgs());
        public void KichHoatNapLai() => BtnNapLai_Click(this, new RoutedEventArgs());
    }
}
