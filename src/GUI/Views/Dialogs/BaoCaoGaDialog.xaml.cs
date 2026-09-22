using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using DAL.Connection;
using DTO.Common;

namespace GUI.Views.Dialogs
{
    public partial class BaoCaoGaDialog : Window
    {
        private readonly DataTable _gocTable;
        private DataTable? _hienTaiTable;
        private bool _isInitialized = false;

        public BaoCaoGaDialog(DataTable sourceTable)
        {
            InitializeComponent();
            _gocTable = sourceTable.Copy();

            // Chuẩn hóa cột phụ trợ nếu chưa có
            if (!_gocTable.Columns.Contains("STT")) _gocTable.Columns.Add("STT", typeof(int));
            if (!_gocTable.Columns.Contains("LyTrinhKmFormatted")) _gocTable.Columns.Add("LyTrinhKmFormatted", typeof(string));
            if (!_gocTable.Columns.Contains("HangGaDisplay")) _gocTable.Columns.Add("HangGaDisplay", typeof(string));
            if (!_gocTable.Columns.Contains("SoDuongRayDisplay")) _gocTable.Columns.Add("SoDuongRayDisplay", typeof(string));
            if (!_gocTable.Columns.Contains("CauQuayText")) _gocTable.Columns.Add("CauQuayText", typeof(string));
            if (!_gocTable.Columns.Contains("KhaiThacText")) _gocTable.Columns.Add("KhaiThacText", typeof(string));
            if (!_gocTable.Columns.Contains("NgayTaoDisplay")) _gocTable.Columns.Add("NgayTaoDisplay", typeof(string));
            if (!_gocTable.Columns.Contains("NgayCapNhatDisplay")) _gocTable.Columns.Add("NgayCapNhatDisplay", typeof(string));

            for (int i = 0; i < _gocTable.Rows.Count; i++)
            {
                var r = _gocTable.Rows[i];
                r["STT"] = i + 1;

                decimal km = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m;
                r["LyTrinhKmFormatted"] = FormatHelper.FormatKm(km);

                string hang = r["HangGa"]?.ToString() ?? "";
                r["HangGaDisplay"] = hang == "HANG_1" ? "Hạng I" : (hang == "HANG_2" ? "Hạng II" : "Hạng III");

                int rays = _gocTable.Columns.Contains("SoDuongRay") && r["SoDuongRay"] != DBNull.Value ? Convert.ToInt32(r["SoDuongRay"]) : 3;
                r["SoDuongRayDisplay"] = $"{rays} ray";

                bool cq = r["CoCauQuay"] != DBNull.Value && Convert.ToBoolean(r["CoCauQuay"]);
                r["CauQuayText"] = cq ? "Có" : "—";

                bool kt = r["DangKhaiThac"] != DBNull.Value && Convert.ToBoolean(r["DangKhaiThac"]);
                r["KhaiThacText"] = kt ? "Đang mở" : "Tạm ngừng";

                if (_gocTable.Columns.Contains("NgayTao") && r["NgayTao"] != DBNull.Value)
                {
                    r["NgayTaoDisplay"] = FormatHelper.FormatDateTime(Convert.ToDateTime(r["NgayTao"]), fromUtc: true);
                }
                else r["NgayTaoDisplay"] = "—";

                if (_gocTable.Columns.Contains("NgayCapNhat") && r["NgayCapNhat"] != DBNull.Value)
                {
                    r["NgayCapNhatDisplay"] = FormatHelper.FormatDateTime(Convert.ToDateTime(r["NgayCapNhat"]), fromUtc: true);
                }
                else r["NgayCapNhatDisplay"] = "Chưa sửa";
            }

            _isInitialized = true;
            ApDungBoLocVaTaoCot();
            Loaded += (s, e) => LoadDanhSachBaoCaoCu();
        }

        // Bộ lọc và tạo cột bản in
        private void ApDungBoLocVaTaoCot()
        {
            if (!_isInitialized) return;

            // 1. Thiết lập cấu trúc cột theo Checkbox
            CapNhatCotDataGrid();

            // 2. Lọc dữ liệu theo điều kiện
            var conditions = new List<string>();

            // Dải lý trình
            if (decimal.TryParse(txtTuKm.Text, out decimal tuKm) && decimal.TryParse(txtDenKm.Text, out decimal denKm))
            {
                conditions.Add($"LyTrinhKm >= {tuKm.ToString(System.Globalization.CultureInfo.InvariantCulture)} AND LyTrinhKm <= {denKm.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }

            // Phân cấp ga
            var hangList = new List<string>();
            if (chkHang1.IsChecked == true) hangList.Add("'HANG_1'");
            if (chkHang2.IsChecked == true) hangList.Add("'HANG_2'");
            if (chkHang3.IsChecked == true) hangList.Add("'HANG_3'");
            if (hangList.Count > 0 && hangList.Count < 3)
            {
                conditions.Add($"HangGa IN ({string.Join(",", hangList)})");
            }
            else if (hangList.Count == 0)
            {
                conditions.Add("1 = 0"); // Không chọn cấp nào thì không ra ga nào
            }

            // Cầu quay
            if (chkChiCauQuay.IsChecked == true)
            {
                conditions.Add("CoCauQuay = true");
            }

            // Lọc theo Số lượng đường ray bằng TextBox có ràng buộc
            if (_gocTable.Columns.Contains("SoDuongRay") && cboToanTuRay != null && cboToanTuRay.SelectedIndex > 0)
            {
                if (int.TryParse(txtSoRay.Text.Trim(), out int soRay) && soRay >= 0)
                {
                    switch (cboToanTuRay.SelectedIndex)
                    {
                        case 1: // >= Tối thiểu
                            conditions.Add($"SoDuongRay >= {soRay}");
                            break;
                        case 2: // <= Tối đa
                            conditions.Add($"SoDuongRay <= {soRay}");
                            break;
                        case 3: // = Đúng số
                            conditions.Add($"SoDuongRay = {soRay}");
                            break;
                    }
                }
            }

            // Trạng thái khai thác
            if (cboLocKhaiThac.SelectedIndex == 1) conditions.Add("DangKhaiThac = true");
            else if (cboLocKhaiThac.SelectedIndex == 2) conditions.Add("DangKhaiThac = false");

            // Áp dụng RowFilter
            string filterStr = conditions.Count > 0 ? string.Join(" AND ", conditions) : "";
            _gocTable.DefaultView.RowFilter = filterStr;
            _hienTaiTable = _gocTable.DefaultView.ToTable();

            // Đánh lại STT liên tục cho bảng đã lọc
            for (int i = 0; i < _hienTaiTable.Rows.Count; i++)
            {
                _hienTaiTable.Rows[i]["STT"] = i + 1;
            }

            dgBaoCao.ItemsSource = _hienTaiTable.DefaultView;

            // Cập nhật nhãn thống kê & KPI
            int tongGa = _hienTaiTable.Rows.Count;
            int tongRay = 0;
            int soCauQuay = 0;

            foreach (DataRow r in _hienTaiTable.Rows)
            {
                if (r.Table.Columns.Contains("SoDuongRay") && r["SoDuongRay"] != DBNull.Value)
                {
                    tongRay += Convert.ToInt32(r["SoDuongRay"]);
                }
                if (r["CoCauQuay"] != DBNull.Value && Convert.ToBoolean(r["CoCauQuay"]))
                {
                    soCauQuay++;
                }
            }

            txtSoBanGhiHienThi.Text = $"{tongGa} ga";
            txtKpiTongGa.Text = $"{tongGa} ga";
            txtKpiTongRay.Text = $"{tongRay} ray";
            txtKpiCauQuay.Text = $"{soCauQuay} ga";

            txtNgayXuat.Text = $"Thời điểm lập biểu: {DateTime.Now:dd/MM/yyyy HH:mm:ss} (Giờ Việt Nam UTC+7)";
            txtPhamViKm.Text = $"Khu đoạn kiểm tra: Km {txtTuKm.Text} — Km {txtDenKm.Text} (Tổng số {tongGa} ga)";
        }

        private void CapNhatCotDataGrid()
        {
            dgBaoCao.Columns.Clear();

            // Cột STT luôn hiện diện
            dgBaoCao.Columns.Add(new DataGridTextColumn
            {
                Header = "STT",
                Binding = new Binding("STT"),
                Width = 45,
                ElementStyle = CenterStyle()
            });

            if (chkColMaGa.IsChecked == true)
            {
                dgBaoCao.Columns.Add(new DataGridTextColumn
                {
                    Header = "Mã Ga",
                    Binding = new Binding("MaGaCode"),
                    Width = 65,
                    ElementStyle = CenterBoldStyle("#003B73")
                });
            }

            if (chkColTenGa.IsChecked == true)
            {
                dgBaoCao.Columns.Add(new DataGridTextColumn
                {
                    Header = "Tên Ga",
                    Binding = new Binding("TenGa"),
                    Width = 120,
                    ElementStyle = BoldStyle("#0F172A")
                });
            }

            if (chkColLyTrinh.IsChecked == true)
            {
                dgBaoCao.Columns.Add(new DataGridTextColumn
                {
                    Header = "Lý Trình (km)",
                    Binding = new Binding("LyTrinhKmFormatted"),
                    Width = 90,
                    ElementStyle = RightStyle()
                });
            }

            if (chkColTinhThanh.IsChecked == true)
            {
                dgBaoCao.Columns.Add(new DataGridTextColumn
                {
                    Header = "Tỉnh / Thành Phố",
                    Binding = new Binding("TinhThanh"),
                    Width = 110,
                    ElementStyle = NormalStyle()
                });
            }

            if (chkColHangGa.IsChecked == true)
            {
                dgBaoCao.Columns.Add(new DataGridTextColumn
                {
                    Header = "Phân Cấp Ga",
                    Binding = new Binding("HangGaDisplay"),
                    Width = 95,
                    ElementStyle = CenterStyle()
                });
            }

            if (chkColSoRay.IsChecked == true)
            {
                dgBaoCao.Columns.Add(new DataGridTextColumn
                {
                    Header = "Số Ray",
                    Binding = new Binding("SoDuongRayDisplay"),
                    Width = 70,
                    ElementStyle = CenterBoldStyle("#0284C7")
                });
            }

            if (chkColCauQuay.IsChecked == true)
            {
                dgBaoCao.Columns.Add(new DataGridTextColumn
                {
                    Header = "Cầu Quay",
                    Binding = new Binding("CauQuayText"),
                    Width = 75,
                    ElementStyle = CenterStyle()
                });
            }

            if (chkColKhaiThac.IsChecked == true)
            {
                dgBaoCao.Columns.Add(new DataGridTextColumn
                {
                    Header = "Trạng Thái",
                    Binding = new Binding("KhaiThacText"),
                    Width = 85,
                    ElementStyle = CenterStyle()
                });
            }

            if (chkColNguoiTao.IsChecked == true)
            {
                dgBaoCao.Columns.Add(new DataGridTextColumn
                {
                    Header = "Người Tạo",
                    Binding = new Binding("NguoiTao"),
                    Width = 85,
                    ElementStyle = NormalStyle()
                });
            }

            if (chkColNgayCapNhat.IsChecked == true)
            {
                dgBaoCao.Columns.Add(new DataGridTextColumn
                {
                    Header = "Cập Nhật",
                    Binding = new Binding("NgayCapNhatDisplay"),
                    Width = 130,
                    ElementStyle = NormalStyle()
                });
            }
        }

        private static Style NormalStyle()
        {
            var s = new Style(typeof(TextBlock));
            s.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(6, 0, 6, 0)));
            return s;
        }

        private static Style CenterStyle()
        {
            var s = NormalStyle();
            s.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            return s;
        }

        private static Style RightStyle()
        {
            var s = NormalStyle();
            s.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
            s.Setters.Add(new Setter(TextBlock.FontFamilyProperty, new System.Windows.Media.FontFamily("Consolas")));
            return s;
        }

        private static Style BoldStyle(string colorHex)
        {
            var s = NormalStyle();
            s.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            s.Setters.Add(new Setter(TextBlock.ForegroundProperty, new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex))));
            return s;
        }

        private static Style CenterBoldStyle(string colorHex)
        {
            var s = CenterStyle();
            s.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            s.Setters.Add(new Setter(TextBlock.ForegroundProperty, new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex))));
            return s;
        }

        // Mẫu báo cáo nhanh
        private void BtnMauHaTang_Click(object sender, RoutedEventArgs e)
        {
            txtTieuDeBaoCao.Text = "BÁO CÁO NĂNG LỰC HẠ TẦNG & SỐ ĐƯỜNG RAY TRÁNH VƯỢT TÀU";
            chkColMaGa.IsChecked = true;
            chkColTenGa.IsChecked = true;
            chkColLyTrinh.IsChecked = true;
            chkColTinhThanh.IsChecked = false;
            chkColHangGa.IsChecked = true;
            chkColSoRay.IsChecked = true;
            chkColCauQuay.IsChecked = true;
            chkColKhaiThac.IsChecked = false;
            chkColNguoiTao.IsChecked = false;
            chkColNgayCapNhat.IsChecked = false;

            txtTuKm.Text = "0.00";
            txtDenKm.Text = "1726.20";
            chkHang1.IsChecked = true;
            chkHang2.IsChecked = true;
            chkHang3.IsChecked = true;
            chkChiCauQuay.IsChecked = false;
            if (cboToanTuRay != null)
            {
                cboToanTuRay.SelectedIndex = 1; // >= Tối thiểu
                txtSoRay.Text = "3";
                txtSoRay.IsEnabled = true;
            }
            cboLocKhaiThac.SelectedIndex = 0;

            ApDungBoLocVaTaoCot();
        }

        private void BtnMauKhaiThac_Click(object sender, RoutedEventArgs e)
        {
            txtTieuDeBaoCao.Text = "BÁO CÁO HIỆN TRẠNG VẬN HÀNH & KHAI THÁC MẠNG LƯỚI GA";
            chkColMaGa.IsChecked = true;
            chkColTenGa.IsChecked = true;
            chkColLyTrinh.IsChecked = true;
            chkColTinhThanh.IsChecked = true;
            chkColHangGa.IsChecked = true;
            chkColSoRay.IsChecked = false;
            chkColCauQuay.IsChecked = false;
            chkColKhaiThac.IsChecked = true;
            chkColNguoiTao.IsChecked = false;
            chkColNgayCapNhat.IsChecked = false;

            cboLocKhaiThac.SelectedIndex = 1; // Chỉ ga đang khai thác
            ApDungBoLocVaTaoCot();
        }

        private void BtnMauKiemToan_Click(object sender, RoutedEventArgs e)
        {
            txtTieuDeBaoCao.Text = "BÁO CÁO TỔNG HỢP KIỂM TOÁN DỮ LIỆU & LỊCH SỬ CẬP NHẬT GA";
            chkColMaGa.IsChecked = true;
            chkColTenGa.IsChecked = true;
            chkColLyTrinh.IsChecked = true;
            chkColTinhThanh.IsChecked = false;
            chkColHangGa.IsChecked = false;
            chkColSoRay.IsChecked = false;
            chkColCauQuay.IsChecked = false;
            chkColKhaiThac.IsChecked = false;
            chkColNguoiTao.IsChecked = true;
            chkColNgayCapNhat.IsChecked = true;

            ApDungBoLocVaTaoCot();
        }

        // Sự kiện thay đổi bộ lọc & cột
        private void FilterInput_Changed(object sender, RoutedEventArgs e) => ApDungBoLocVaTaoCot();
        private void CboLocKhaiThac_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApDungBoLocVaTaoCot();
        private void CboToanTuRay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtSoRay != null)
            {
                txtSoRay.IsEnabled = cboToanTuRay.SelectedIndex > 0;
            }
            ApDungBoLocVaTaoCot();
        }
        private void TxtSoRay_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Ràng buộc số: chỉ cho phép nhập ký tự [0-9]
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9]+$");
        }
        private void ColumnCheck_Changed(object sender, RoutedEventArgs e) => ApDungBoLocVaTaoCot();

        private void BtnChonTatCaCot_Click(object sender, RoutedEventArgs e)
        {
            chkColMaGa.IsChecked = true;
            chkColTenGa.IsChecked = true;
            chkColLyTrinh.IsChecked = true;
            chkColTinhThanh.IsChecked = true;
            chkColHangGa.IsChecked = true;
            chkColSoRay.IsChecked = true;
            chkColCauQuay.IsChecked = true;
            chkColKhaiThac.IsChecked = true;
            chkColNguoiTao.IsChecked = true;
            chkColNgayCapNhat.IsChecked = true;
            ApDungBoLocVaTaoCot();
        }

        private void BtnApDungLoc_Click(object sender, RoutedEventArgs e) => ApDungBoLocVaTaoCot();

        private void BtnDatLaiLoc_Click(object sender, RoutedEventArgs e)
        {
            txtTuKm.Text = "0.00";
            txtDenKm.Text = "1726.20";
            chkHang1.IsChecked = true;
            chkHang2.IsChecked = true;
            chkHang3.IsChecked = true;
            chkChiCauQuay.IsChecked = false;
            if (cboToanTuRay != null)
            {
                cboToanTuRay.SelectedIndex = 0;
                txtSoRay.Text = "2";
                txtSoRay.IsEnabled = false;
            }
            cboLocKhaiThac.SelectedIndex = 0;
            ApDungBoLocVaTaoCot();
        }

        // In báo cáo và xuất file Excel
        private void BtnIn_Click(object sender, RoutedEventArgs e)
        {
            if (_hienTaiTable == null || _hienTaiTable.Rows.Count == 0)
            {
                ThongBaoDialog.CanhBao("Báo cáo hiện tại không có dữ liệu để in.", "Thông Báo", this);
                return;
            }

            try
            {
                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    printArea.UpdateLayout();
                    printDlg.PrintVisual(printArea, txtTieuDeBaoCao.Text);
                    ThongBaoDialog.ThanhCong("Đã gửi tài liệu in thành công tới máy in / xuất file PDF!", "In Báo Cáo Thành Công", this);
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể thực hiện in: {ex.Message}", "Lỗi In Ấn", this);
            }
        }

        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_hienTaiTable == null || _hienTaiTable.Rows.Count == 0)
            {
                ThongBaoDialog.CanhBao("Không có dữ liệu trong báo cáo để xuất file.", "Thông Báo", this);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "Xuất Dữ Liệu Báo Cáo Ra Excel",
                FileName = $"BaoCaoGa_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                Filter = "File Excel CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
                DefaultExt = ".csv"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(sfd.FileName, false, new UTF8Encoding(true)))
                    {
                        // Header cột theo các cột đang hiển thị
                        var activeCols = dgBaoCao.Columns.Where(c => c.Visibility == Visibility.Visible).ToList();
                        writer.WriteLine(string.Join(",", activeCols.Select(c => $"\"{c.Header}\"")));

                        foreach (DataRow r in _hienTaiTable.Rows)
                        {
                            var vals = new List<string>();
                            foreach (var col in activeCols)
                            {
                                if (col is DataGridTextColumn tc && tc.Binding is Binding b)
                                {
                                    string prop = b.Path.Path;
                                    string v = r.Table.Columns.Contains(prop) && r[prop] != DBNull.Value ? r[prop].ToString()! : "";
                                    vals.Add($"\"{v.Replace("\"", "\"\"")}\"");
                                }
                            }
                            writer.WriteLine(string.Join(",", vals));
                        }
                    }

                    bool moFile = ThongBaoDialog.XacNhan(
                        $"Đã xuất thành công {_hienTaiTable.Rows.Count} dòng dữ liệu ra file:\n{sfd.FileName}\n\nBạn có muốn mở file này ngay không?",
                        "Xuất File Thành Công",
                        nutDongY: "Mở file ngay",
                        nutHuy: "Đóng",
                        owner: this);

                    if (moFile)
                    {
                        Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    ThongBaoDialog.Loi($"Lỗi khi xuất file: {ex.Message}", "Lỗi Xuất File", this);
                }
            }
        }

        // Lưu snapshot báo cáo vào CSDL
        private void BtnLuuBaoCao_Click(object sender, RoutedEventArgs e)
        {
            if (_hienTaiTable == null || _hienTaiTable.Rows.Count == 0)
            {
                ThongBaoDialog.CanhBao("Không có dữ liệu trong báo cáo để lưu.", "Thông Báo", this);
                return;
            }

            txtTieuDeLuuMoi.Text = $"{txtTieuDeBaoCao.Text} ({DateTime.Now:dd/MM/yyyy HH:mm})";
            pnlNhapTenBaoCao.Visibility = Visibility.Visible;
            txtTieuDeLuuMoi.Focus();
            txtTieuDeLuuMoi.SelectAll();
        }

        private void BtnHuyLuuSnapshot_Click(object sender, RoutedEventArgs e)
        {
            pnlNhapTenBaoCao.Visibility = Visibility.Collapsed;
        }

        private void BtnXacNhanLuuSnapshot_Click(object sender, RoutedEventArgs e)
        {
            string tieuDe = txtTieuDeLuuMoi.Text.Trim();
            if (string.IsNullOrEmpty(tieuDe))
            {
                ThongBaoDialog.CanhBao("Vui lòng nhập tiêu đề cho bản lưu báo cáo.", "Thiếu Tiêu Đề", this);
                txtTieuDeLuuMoi.Focus();
                return;
            }

            try
            {
                // Thu thập danh sách cột được chọn
                var danhSachCot = new List<string>();
                if (chkColMaGa.IsChecked == true) danhSachCot.Add("MaGa");
                if (chkColTenGa.IsChecked == true) danhSachCot.Add("TenGa");
                if (chkColLyTrinh.IsChecked == true) danhSachCot.Add("LyTrinh");
                if (chkColTinhThanh.IsChecked == true) danhSachCot.Add("TinhThanh");
                if (chkColHangGa.IsChecked == true) danhSachCot.Add("HangGa");
                if (chkColSoRay.IsChecked == true) danhSachCot.Add("SoRay");
                if (chkColCauQuay.IsChecked == true) danhSachCot.Add("CauQuay");
                if (chkColKhaiThac.IsChecked == true) danhSachCot.Add("KhaiThac");
                if (chkColNguoiTao.IsChecked == true) danhSachCot.Add("NguoiTao");
                if (chkColNgayCapNhat.IsChecked == true) danhSachCot.Add("NgayCapNhat");

                // Serialize snapshot DataTable
                var listData = new List<Dictionary<string, object?>>();
                foreach (DataRow r in _hienTaiTable!.Rows)
                {
                    var d = new Dictionary<string, object?>();
                    foreach (DataColumn c in _hienTaiTable.Columns)
                    {
                        d[c.ColumnName] = r[c] == DBNull.Value ? null : r[c];
                    }
                    listData.Add(d);
                }

                string snapshotJson = JsonSerializer.Serialize(listData);
                string tieuChiLoc = txtPhamViKm.Text;
                string dsCotStr = string.Join(",", danhSachCot);

                string sql = @"
                    INSERT INTO kiemtoan.BaoCaoLuuTru (TieuDe, LoaiBaoCao, ThoiDiem, NguoiLap, SoBanGhi, TieuChiLoc, DanhSachCot, SnapshotJson)
                    VALUES (@tieuDe, 'BAOCAO_GA', SYSUTCDATETIME(), 'admin', @soBanGhi, @tieuChi, @dsCot, @json)";

                DatabaseHelper.ExecuteNonQuery(sql, new[]
                {
                    new SqlParameter("@tieuDe", tieuDe),
                    new SqlParameter("@soBanGhi", _hienTaiTable.Rows.Count),
                    new SqlParameter("@tieuChi", tieuChiLoc),
                    new SqlParameter("@dsCot", dsCotStr),
                    new SqlParameter("@json", snapshotJson)
                });

                pnlNhapTenBaoCao.Visibility = Visibility.Collapsed;
                ThongBaoDialog.ThanhCong($"Đã lưu trữ snapshot báo cáo thành công vào CSDL với tiêu đề:\n'{tieuDe}'", "Lưu Trữ Thành Công", this);
                LoadDanhSachBaoCaoCu();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi khi lưu snapshot báo cáo: {ex.Message}", "Lỗi Lưu Trữ", this);
            }
        }

        // Kho lưu trữ báo cáo cũ
        private DataTable? _tableBaoCaoCu;

        private void DamBaoBangLuuTruTonTai()
        {
            try
            {
                string initSql = @"
                    IF SCHEMA_ID('kiemtoan') IS NULL EXEC('CREATE SCHEMA kiemtoan');
                    IF OBJECT_ID('kiemtoan.BaoCaoLuuTru', 'U') IS NULL
                    BEGIN
                        CREATE TABLE kiemtoan.BaoCaoLuuTru (
                            MaBaoCao INT IDENTITY(1,1) PRIMARY KEY,
                            TieuDe NVARCHAR(250) NOT NULL,
                            LoaiBaoCao VARCHAR(50) NOT NULL DEFAULT 'BAOCAO_GA',
                            ThoiDiem DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                            NguoiLap NVARCHAR(100) NOT NULL DEFAULT 'admin',
                            SoBanGhi INT NOT NULL DEFAULT 0,
                            TieuChiLoc NVARCHAR(500) NULL,
                            DanhSachCot NVARCHAR(500) NULL,
                            SnapshotJson NVARCHAR(MAX) NOT NULL
                        );
                    END";
                DatabaseHelper.ExecuteNonQuery(initSql);
            }
            catch
            {
                // Phòng vệ nếu user phân quyền hạn chế DDL
            }
        }

        private void LoadDanhSachBaoCaoCu()
        {
            try
            {
                DamBaoBangLuuTruTonTai();
                string sql = "SELECT MaBaoCao, ThoiDiem, TieuDe, NguoiLap, SoBanGhi, TieuChiLoc, DanhSachCot, SnapshotJson FROM kiemtoan.BaoCaoLuuTru ORDER BY ThoiDiem DESC";
                _tableBaoCaoCu = DatabaseHelper.ExecuteQuery(sql);

                _tableBaoCaoCu.Columns.Add("ThoiGianDisplay", typeof(string));
                foreach (DataRow r in _tableBaoCaoCu.Rows)
                {
                    if (r["ThoiDiem"] != DBNull.Value)
                    {
                        DateTime dt = Convert.ToDateTime(r["ThoiDiem"]);
                        r["ThoiGianDisplay"] = FormatHelper.FormatDateTime(dt, fromUtc: true);
                    }
                    else r["ThoiGianDisplay"] = "—";
                }

                dgBaoCaoCu.ItemsSource = _tableBaoCaoCu.DefaultView;
            }
            catch (Exception ex)
            {
                if (IsLoaded && IsVisible)
                {
                    ThongBaoDialog.Loi($"Không thể tải danh sách báo cáo cũ: {ex.Message}", "Lỗi Truy Vấn", this);
                }
            }
        }

        private void BtnLamMoiBaoCaoCu_Click(object sender, RoutedEventArgs e)
        {
            LoadDanhSachBaoCaoCu();
        }

        private void TxtTimBaoCaoCu_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_tableBaoCaoCu == null) return;
            string q = txtTimBaoCaoCu.Text.Trim();
            if (string.IsNullOrEmpty(q))
            {
                _tableBaoCaoCu.DefaultView.RowFilter = "";
            }
            else
            {
                _tableBaoCaoCu.DefaultView.RowFilter = $"TieuDe LIKE '%{q}%' OR NguoiLap LIKE '%{q}%' OR TieuChiLoc LIKE '%{q}%'";
            }
        }

        // Xem lại Snapshot bất biến
        private void BtnXemLaiSnapshot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DataRowView drv)
            {
                try
                {
                    string tieuDe = drv["TieuDe"]?.ToString() ?? "BÁO CÁO CŨ";
                    string json = drv["SnapshotJson"]?.ToString() ?? "";
                    string dsCot = drv["DanhSachCot"]?.ToString() ?? "";

                    if (string.IsNullOrEmpty(json))
                    {
                        ThongBaoDialog.CanhBao("Báo cáo này không có dữ liệu snapshot hợp lệ.", "Cảnh Báo", this);
                        return;
                    }

                    var listData = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(json);
                    if (listData == null || listData.Count == 0)
                    {
                        ThongBaoDialog.CanhBao("Không thể đọc dữ liệu snapshot.", "Cảnh Báo", this);
                        return;
                    }

                    // Tái thiết lập các Checkbox cột theo bản snapshot
                    chkColMaGa.IsChecked = dsCot.Contains("MaGa");
                    chkColTenGa.IsChecked = dsCot.Contains("TenGa");
                    chkColLyTrinh.IsChecked = dsCot.Contains("LyTrinh");
                    chkColTinhThanh.IsChecked = dsCot.Contains("TinhThanh");
                    chkColHangGa.IsChecked = dsCot.Contains("HangGa");
                    chkColSoRay.IsChecked = dsCot.Contains("SoRay");
                    chkColCauQuay.IsChecked = dsCot.Contains("CauQuay");
                    chkColKhaiThac.IsChecked = dsCot.Contains("KhaiThac");
                    chkColNguoiTao.IsChecked = dsCot.Contains("NguoiTao");
                    chkColNgayCapNhat.IsChecked = dsCot.Contains("NgayCapNhat");

                    CapNhatCotDataGrid();

                    // Dựng DataTable từ snapshot JSON
                    DataTable snapDt = _gocTable.Clone();
                    foreach (var dict in listData)
                    {
                        DataRow r = snapDt.NewRow();
                        foreach (var kvp in dict)
                        {
                            if (snapDt.Columns.Contains(kvp.Key))
                            {
                                if (kvp.Value is JsonElement elem)
                                {
                                    if (elem.ValueKind == JsonValueKind.Number)
                                    {
                                        if (snapDt.Columns[kvp.Key]?.DataType == typeof(decimal))
                                            r[kvp.Key] = elem.GetDecimal();
                                        else
                                            r[kvp.Key] = elem.GetInt32();
                                    }
                                    else if (elem.ValueKind == JsonValueKind.True || elem.ValueKind == JsonValueKind.False)
                                        r[kvp.Key] = elem.GetBoolean();
                                    else
                                        r[kvp.Key] = elem.GetString();
                                }
                                else
                                {
                                    r[kvp.Key] = kvp.Value ?? DBNull.Value;
                                }
                            }
                        }
                        snapDt.Rows.Add(r);
                    }

                    _hienTaiTable = snapDt;
                    dgBaoCao.ItemsSource = _hienTaiTable.DefaultView;

                    txtTieuDeBaoCao.Text = tieuDe.ToUpper();
                    txtPhamViKm.Text = drv["TieuChiLoc"]?.ToString() ?? "";
                    txtNgayXuat.Text = $"Snapshot lưu lúc: {drv["ThoiGianDisplay"]} (Bản ghi lịch sử bất biến)";

                    int tongGa = _hienTaiTable.Rows.Count;
                    int tongRay = 0;
                    int soCauQuay = 0;
                    foreach (DataRow r in _hienTaiTable.Rows)
                    {
                        if (r.Table.Columns.Contains("SoDuongRay") && r["SoDuongRay"] != DBNull.Value)
                            tongRay += Convert.ToInt32(r["SoDuongRay"]);
                        if (r["CoCauQuay"] != DBNull.Value && Convert.ToBoolean(r["CoCauQuay"]))
                            soCauQuay++;
                    }

                    txtSoBanGhiHienThi.Text = $"{tongGa} ga";
                    txtKpiTongGa.Text = $"{tongGa} ga";
                    txtKpiTongRay.Text = $"{tongRay} ray";
                    txtKpiCauQuay.Text = $"{soCauQuay} ga";

                    // Chuyển sang Tab 1 để xem bản in
                    tabControlChinh.SelectedIndex = 0;

                    ThongBaoDialog.ThanhCong($"Đã nạp thành công bản snapshot lịch sử:\n'{tieuDe}'\n\nBạn đang xem đúng nguyên trạng số liệu thời điểm lưu.", "Xem Lại Báo Cáo", this);
                }
                catch (Exception ex)
                {
                    ThongBaoDialog.Loi($"Lỗi khi mở snapshot: {ex.Message}", "Lỗi Nạp Snapshot", this);
                }
            }
        }

        // Xóa báo cáo cũ
        private void BtnXoaBaoCaoCu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DataRowView drv)
            {
                int maBaoCao = Convert.ToInt32(drv["MaBaoCao"]);
                string tieuDe = drv["TieuDe"]?.ToString() ?? "";

                bool xacNhan = ThongBaoDialog.XacNhan(
                    $"Bạn có chắc chắn muốn xóa bản lưu báo cáo:\n'{tieuDe}' (Mã #{maBaoCao}) khỏi CSDL không?",
                    "Xác Nhận Xóa Báo Cáo",
                    nutDongY: "Xóa vĩnh viễn",
                    nutHuy: "Hủy bỏ",
                    laHanhDongXoa: true,
                    owner: this);

                if (xacNhan)
                {
                    try
                    {
                        DatabaseHelper.ExecuteNonQuery("DELETE FROM kiemtoan.BaoCaoLuuTru WHERE MaBaoCao = @id", new[]
                        {
                            new SqlParameter("@id", maBaoCao)
                        });
                        ThongBaoDialog.ThanhCong("Đã xóa báo cáo cũ thành công.", "Đã Xóa", this);
                        LoadDanhSachBaoCaoCu();
                    }
                    catch (Exception ex)
                    {
                        ThongBaoDialog.Loi($"Lỗi khi xóa báo cáo: {ex.Message}", "Lỗi Thao Tác", this);
                    }
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
