using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using GUI.Helpers;

namespace GUI.Views.Dialogs
{
    public partial class BaoCaoDoanhThuBanVeDialog : Window
    {
        private DataTable? _currentTable;

        public BaoCaoDoanhThuBanVeDialog()
        {
            InitializeComponent();
            dpTuNgay.SelectedDate = DateTime.Today.AddDays(-30);
            dpDenNgay.SelectedDate = DateTime.Today;
            Loaded += (s, e) => TaiDuLieuBaoCao();
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
            Close();
        }

        private void CboMauBaoCao_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                TaiDuLieuBaoCao();
            }
        }

        private void BtnXemBaoCao_Click(object sender, RoutedEventArgs e)
        {
            TaiDuLieuBaoCao();
        }

        private void TaiDuLieuBaoCao()
        {
            DateTime tuNgay = dpTuNgay.SelectedDate?.Date ?? DateTime.Today.AddDays(-30);
            DateTime denNgay = (dpDenNgay.SelectedDate?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);

            int mau = cboMauBaoCao.SelectedIndex;

            try
            {
                // 1. Tải KPI tổng thể
                string kpiSql = @"
                    SELECT COUNT(*) AS TongVe,
                           ISNULL(SUM(v.GiaVeThucThu), 0) AS TongThu,
                           ISNULL(SUM(hh.SoTienThucHoan), 0) AS TongHoan
                    FROM vantai.Ve v
                    LEFT JOIN vantai.HoanHuyVe hh ON v.MaVe = hh.MaVe
                    WHERE v.ThoiDiemXuatVe >= @tu AND v.ThoiDiemXuatVe <= @den";

                DataTable dtKpi = DatabaseHelper.ExecuteQuery(kpiSql, new[]
                {
                    new SqlParameter("@tu", tuNgay),
                    new SqlParameter("@den", denNgay)
                });

                if (dtKpi.Rows.Count > 0)
                {
                    int tongVe = Convert.ToInt32(dtKpi.Rows[0]["TongVe"]);
                    decimal tongThu = Convert.ToDecimal(dtKpi.Rows[0]["TongThu"]);
                    decimal tongHoan = Convert.ToDecimal(dtKpi.Rows[0]["TongHoan"]);
                    decimal thuan = tongThu - tongHoan;

                    kpiTongVe.Text = $"{tongVe:N0} vé";
                    kpiTongThu.Text = $"{tongThu:N0} VNĐ";
                    kpiTongHoan.Text = $"{tongHoan:N0} VNĐ";
                    kpiDoanhThuThuan.Text = $"{thuan:N0} VNĐ";
                }

                // 2. Tải bảng dữ liệu theo từng mẫu
                if (mau == 0) // Theo Ngày
                {
                    lblTieuDeBang.Text = $"BẢNG TỔNG HỢP DOANH THU THEO NGÀY ({tuNgay:dd/MM/yyyy} - {dpDenNgay.SelectedDate:dd/MM/yyyy})";
                    string sql = @"
                        SELECT CAST(v.ThoiDiemXuatVe AS DATE) AS Ngay,
                               COUNT(*) AS TongSoVe,
                               ISNULL(SUM(v.GiaVeGoc), 0) AS TongGiaGoc,
                               ISNULL(SUM(v.SoTienGiam), 0) AS TongGiamGia,
                               ISNULL(SUM(v.GiaVeThucThu), 0) AS TongThucThu,
                               SUM(CASE WHEN v.TrangThai = 'DA_HOAN_VE' THEN 1 ELSE 0 END) AS SoVeHoan,
                               ISNULL(SUM(hh.SoTienThucHoan), 0) AS TongTienHoan,
                               ISNULL(SUM(v.GiaVeThucThu), 0) - ISNULL(SUM(hh.SoTienThucHoan), 0) AS DoanhThuThuan
                        FROM vantai.Ve v
                        LEFT JOIN vantai.HoanHuyVe hh ON v.MaVe = hh.MaVe
                        WHERE v.ThoiDiemXuatVe >= @tu AND v.ThoiDiemXuatVe <= @den
                        GROUP BY CAST(v.ThoiDiemXuatVe AS DATE)
                        ORDER BY Ngay DESC";

                    _currentTable = DatabaseHelper.ExecuteQuery(sql, new[]
                    {
                        new SqlParameter("@tu", tuNgay),
                        new SqlParameter("@den", denNgay)
                    });

                    HienThiMauNgay();
                }
                else if (mau == 1) // Theo Mác Tàu
                {
                    lblTieuDeBang.Text = $"BẢNG TỔNG HỢP DOANH THU THEO MÁC TÀU ({tuNgay:dd/MM/yyyy} - {dpDenNgay.SelectedDate:dd/MM/yyyy})";
                    string sql = @"
                        SELECT mt.SoHieuMacTau,
                               COUNT(*) AS TongSoVe,
                               ISNULL(SUM(v.GiaVeThucThu), 0) AS TongDoanhThu,
                               SUM(CASE WHEN v.TrangThai = 'DA_HOAN_VE' THEN 1 ELSE 0 END) AS SoVeHoan,
                               SUM(CASE WHEN v.TrangThai IN ('DA_DAT', 'DA_LEN_TAU') THEN 1 ELSE 0 END) AS SoVeHieuLuc
                        FROM vantai.Ve v
                        JOIN vanhanh.ChuyenTau ct ON v.MaChuyenTau = ct.MaChuyenTau
                        JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                        WHERE v.ThoiDiemXuatVe >= @tu AND v.ThoiDiemXuatVe <= @den
                        GROUP BY mt.SoHieuMacTau
                        ORDER BY TongDoanhThu DESC";

                    _currentTable = DatabaseHelper.ExecuteQuery(sql, new[]
                    {
                        new SqlParameter("@tu", tuNgay),
                        new SqlParameter("@den", denNgay)
                    });

                    HienThiMauMacTau();
                }
                else // Theo Chặng Tuyến
                {
                    lblTieuDeBang.Text = $"BẢNG TỔNG HỢP DOANH THU THEO CHẶNG TUYẾN ({tuNgay:dd/MM/yyyy} - {dpDenNgay.SelectedDate:dd/MM/yyyy})";
                    string sql = @"
                        SELECT (g1.TenGa + N' ➔ ' + g2.TenGa) AS ChangDi,
                               COUNT(*) AS TongSoVe,
                               ISNULL(SUM(v.GiaVeThucThu), 0) AS TongDoanhThu,
                               ISNULL(AVG(v.GiaVeThucThu), 0) AS GiaVeTrungBinh
                        FROM vantai.Ve v
                        JOIN hatang.Ga g1 ON v.MaGaDi = g1.MaGa
                        JOIN hatang.Ga g2 ON v.MaGaDen = g2.MaGa
                        WHERE v.ThoiDiemXuatVe >= @tu AND v.ThoiDiemXuatVe <= @den
                        GROUP BY g1.TenGa, g2.TenGa
                        ORDER BY TongDoanhThu DESC";

                    _currentTable = DatabaseHelper.ExecuteQuery(sql, new[]
                    {
                        new SqlParameter("@tu", tuNgay),
                        new SqlParameter("@den", denNgay)
                    });

                    HienThiMauChang();
                }

                lblThongTinTongKet.Text = $"Đã tải {_currentTable?.Rows.Count ?? 0} dòng dữ liệu ({tuNgay:dd/MM/yyyy} - {dpDenNgay.SelectedDate:dd/MM/yyyy})";
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi kết xuất báo cáo doanh thu: {ex.Message}", "Lỗi CSDL");
            }
        }

        private void HienThiMauNgay()
        {
            dgBaoCao.Columns.Clear();
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Ngày Bán", Binding = new System.Windows.Data.Binding("Ngay") { StringFormat = "dd/MM/yyyy" }, Width = 100 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Số Vé Bán", Binding = new System.Windows.Data.Binding("TongSoVe") { StringFormat = "{0:N0}" }, Width = 80 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Tiền Gốc (VNĐ)", Binding = new System.Windows.Data.Binding("TongGiaGoc") { StringFormat = "{0:N0}" }, Width = 120 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Giảm Giá (VNĐ)", Binding = new System.Windows.Data.Binding("TongGiamGia") { StringFormat = "{0:N0}" }, Width = 110 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Thực Thu (VNĐ)", Binding = new System.Windows.Data.Binding("TongThucThu") { StringFormat = "{0:N0}" }, Width = 125 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Vé Hoàn", Binding = new System.Windows.Data.Binding("SoVeHoan") { StringFormat = "{0:N0}" }, Width = 80 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Tiền Hoàn (VNĐ)", Binding = new System.Windows.Data.Binding("TongTienHoan") { StringFormat = "{0:N0}" }, Width = 120 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Doanh Thu Thuần (VNĐ)", Binding = new System.Windows.Data.Binding("DoanhThuThuan") { StringFormat = "{0:N0}" }, Width = 140 });

            dgBaoCao.ItemsSource = _currentTable?.DefaultView;
        }

        private void HienThiMauMacTau()
        {
            dgBaoCao.Columns.Clear();
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Mác Tàu", Binding = new System.Windows.Data.Binding("SoHieuMacTau"), Width = 100 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Tổng Vé Phát Hành", Binding = new System.Windows.Data.Binding("TongSoVe") { StringFormat = "{0:N0}" }, Width = 120 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Vé Hiệu Lực", Binding = new System.Windows.Data.Binding("SoVeHieuLuc") { StringFormat = "{0:N0}" }, Width = 110 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Vé Đã Hoàn", Binding = new System.Windows.Data.Binding("SoVeHoan") { StringFormat = "{0:N0}" }, Width = 100 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Tổng Doanh Thu (VNĐ)", Binding = new System.Windows.Data.Binding("TongDoanhThu") { StringFormat = "{0:N0}" }, Width = 160 });

            dgBaoCao.ItemsSource = _currentTable?.DefaultView;
        }

        private void HienThiMauChang()
        {
            dgBaoCao.Columns.Clear();
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Chặng Tuyến", Binding = new System.Windows.Data.Binding("ChangDi"), Width = 220 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Tổng Số Vé", Binding = new System.Windows.Data.Binding("TongSoVe") { StringFormat = "{0:N0}" }, Width = 120 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Giá Vé TB (VNĐ)", Binding = new System.Windows.Data.Binding("GiaVeTrungBinh") { StringFormat = "{0:N0}" }, Width = 130 });
            dgBaoCao.Columns.Add(new DataGridTextColumn { Header = "Tổng Doanh Thu (VNĐ)", Binding = new System.Windows.Data.Binding("TongDoanhThu") { StringFormat = "{0:N0}" }, Width = 160 });

            dgBaoCao.ItemsSource = _currentTable?.DefaultView;
        }

        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTable == null || _currentTable.Rows.Count == 0)
            {
                ThongBaoDialog.ThongTin("Không có dữ liệu báo cáo để xuất Excel!", "Thông Báo");
                return;
            }

            int mau = cboMauBaoCao.SelectedIndex;
            var mapping = new Dictionary<string, string>();

            if (mau == 0)
            {
                mapping["Ngay"] = "Ngày";
                mapping["TongSoVe"] = "Số Vé Bán";
                mapping["TongGiaGoc"] = "Tiền Gốc (VNĐ)";
                mapping["TongGiamGia"] = "Giảm Giá (VNĐ)";
                mapping["TongThucThu"] = "Thực Thu (VNĐ)";
                mapping["SoVeHoan"] = "Số Vé Hoàn";
                mapping["TongTienHoan"] = "Tiền Hoàn (VNĐ)";
                mapping["DoanhThuThuan"] = "Doanh Thu Thuần (VNĐ)";
            }
            else if (mau == 1)
            {
                mapping["SoHieuMacTau"] = "Mác Tàu";
                mapping["TongSoVe"] = "Tổng Vé Phát Hành";
                mapping["SoVeHieuLuc"] = "Vé Hiệu Lực";
                mapping["SoVeHoan"] = "Vé Đã Hoàn";
                mapping["TongDoanhThu"] = "Tổng Doanh Thu (VNĐ)";
            }
            else
            {
                mapping["ChangDi"] = "Chặng Tuyến";
                mapping["TongSoVe"] = "Tổng Số Vé";
                mapping["GiaVeTrungBinh"] = "Giá Vé TB (VNĐ)";
                mapping["TongDoanhThu"] = "Tổng Doanh Thu (VNĐ)";
            }

            FileExchangeHelper.XuatExcel(
                _currentTable.DefaultView, 
                mapping, 
                $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmm}", 
                lblTieuDeBang.Text, 
                "DoanhThu");
        }

        private void BtnInBaoCao_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    printDlg.PrintVisual(printArea, lblTieuDeBang.Text);
                    ThongBaoDialog.ThanhCong("Đã gửi lệnh in báo cáo tới máy in!", "In Báo Cáo");
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi khi in báo cáo: {ex.Message}", "Lỗi In Ấn");
            }
        }
    }
}
