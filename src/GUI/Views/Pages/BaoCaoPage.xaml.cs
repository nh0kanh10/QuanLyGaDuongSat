using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BUS.Services;
using ClosedXML.Excel;
using DTO.BaoCao;
using GUI.Views.Dialogs;

namespace GUI.Views.Pages
{
    // =========================================================================
    // TRANG BAO CAO THONG KE TONG HOP (Sprint 3, Dat - FR-7.3). Can quyen REPORT_VIEW.
    // So lieu tu BaoCaoService (view baocao.vw_*); bieu do ve bang WPF (khong them thu vien);
    // xuat Excel bang ClosedXML (goi da co trong GUI.csproj).
    // =========================================================================
    public partial class BaoCaoPage : Page
    {
        private readonly BaoCaoService _sv = new();
        private KetQuaBaoCao? _kq;
        private bool _dangNap = true;
        private bool _daNap;

        private sealed record Cot(string Nhan, string GiaTri, double DoDai, Brush Mau, string ChuThich);

        public BaoCaoPage()
        {
            InitializeComponent();
            DatKy("14");
            _dangNap = false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_daNap) return;
            _daNap = true;
            _dangNap = true;
            cboMacTau.Items.Clear();
            cboMacTau.Items.Add(new ComboBoxItem { Content = "Tất cả mác", Tag = null });
            if (_sv.LayDanhSachMacTau(out var mac, out _))
                foreach (var m in mac) cboMacTau.Items.Add(new ComboBoxItem { Content = m, Tag = m });
            cboMacTau.SelectedIndex = 0;
            _dangNap = false;
            LapBaoCao();
        }

        private void DatKy(string ky)
        {
            var hom = DateTime.Today;
            (DateTime tu, DateTime den) = ky switch
            {
                "7" => (hom.AddDays(-6), hom),
                "30" => (hom.AddDays(-29), hom),
                "THANG" => (new DateTime(hom.Year, hom.Month, 1), hom),
                _ => (hom.AddDays(-13), hom)
            };
            dpTu.SelectedDate = tu;
            dpDen.SelectedDate = den;
        }

        private void CboKy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNap || cboKy.SelectedItem is not ComboBoxItem { Tag: string ky } || ky == "TU_CHON") return;
            _dangNap = true;
            DatKy(ky);
            _dangNap = false;
        }

        private void NgayThayDoi(object? sender, SelectionChangedEventArgs e)
        {
            if (_dangNap) return;
            _dangNap = true;
            cboKy.SelectedItem = cboKy.Items.Cast<ComboBoxItem>().First(i => (string)i.Tag == "TU_CHON");
            _dangNap = false;
        }

        private void BtnLapBaoCao_Click(object sender, RoutedEventArgs e) => LapBaoCao();

        private void LapBaoCao()
        {
            var loc = new BoLocBaoCao
            {
                TuNgay = dpTu.SelectedDate ?? DateTime.Today.AddDays(-13),
                DenNgay = dpDen.SelectedDate ?? DateTime.Today,
                SoHieuMacTau = (cboMacTau.SelectedItem as ComboBoxItem)?.Tag as string
            };
            Mouse.OverrideCursor = Cursors.Wait;
            bool ok;
            KetQuaBaoCao kq;
            string loi;
            try { ok = _sv.LayBaoCao(loc, out kq, out loi); }
            finally { Mouse.OverrideCursor = null; }
            if (!ok) { ThongBaoDialog.Loi(loi, "Không Lập Được Báo Cáo"); return; }
            _kq = kq;
            HienBaoCao(kq);
        }

        private void HienBaoCao(KetQuaBaoCao kq)
        {
            var t = kq.TongQuan;
            txtKpiDoanhThu.Text = $"{t.DoanhThuThuan / 1_000_000m:N1} triệu";
            txtKpiDoanhThuPhu.Text = $"{t.SoChuyen} chuyến · bình quân {(t.SoChuyen == 0 ? 0 : t.DoanhThuThuan / t.SoChuyen / 1_000_000m):N1} tr/chuyến";
            txtKpiSoVe.Text = t.SoVe.ToString("N0");
            txtKpiSoVePhu.Text = $"Hoàn {t.SoVeHoan:N0} vé ({t.TyLeHoan:P1})";
            txtKpiHeSo.Text = t.HeSoGhe.ToString("P1");
            txtKpiHeSoPhu.Text = "ghế·km bán / ghế·km cung ứng";
            txtKpiDungGio.Text = t.SoChuyenCoGio == 0 ? "—" : t.TyLeDungGio.ToString("P1");
            txtKpiDungGioPhu.Text = t.SoChuyenCoGio == 0 ? "Chưa có giờ thực tế" : $"{t.SoChuyenDungGio}/{t.SoChuyenCoGio} chuyến · trễ TB {t.TreTrungBinh:N1}'";
            txtKpiHang.Text = $"{t.TanKm:N0} tấn·km";
            txtKpiHangPhu.Text = $"{t.SoVanDon} vận đơn · {t.SanLuongTan:N1} tấn · cước {t.CuocHang / 1_000_000m:N1} tr";
            // [12/11] Van don chua gan chuyen tau -> san luong hang khong loc duoc theo mac: noi ro thay vi de so dung yen
            if (!string.IsNullOrEmpty(kq.BoLoc.SoHieuMacTau))
                txtKpiHangPhu.Text = "Toàn mạng (vận đơn chưa gắn mác tàu) · " + txtKpiHangPhu.Text;
            txtKpiHang.ToolTip = txtKpiHangPhu.ToolTip = "Sản lượng hàng theo ngày lập vận đơn, tính cho toàn mạng.\n" +
                "Vận đơn chưa gắn với chuyến tàu nên không lọc được theo mác tàu.";

            // Cot doanh thu theo ngay (dien du moi ngay trong ky, ngay khong co tau = 0)
            var theoNgay = kq.TheoNgay.ToDictionary(d => d.Ngay.Date);
            var ngay = Enumerable.Range(0, (int)(kq.BoLoc.DenNgay.Date - kq.BoLoc.TuNgay.Date).TotalDays + 1)
                                 .Select(i => kq.BoLoc.TuNgay.Date.AddDays(i)).ToList();
            decimal max = Math.Max(1, kq.TheoNgay.Select(d => d.DoanhThu).DefaultIfEmpty(0).Max());
            var xanh = Mau("#0284C7");
            bool hienSo = ngay.Count <= 21;
            icCotNgay.ItemsSource = ngay.Select(n =>
            {
                var d = theoNgay.GetValueOrDefault(n);
                decimal dt = d?.DoanhThu ?? 0;
                return new Cot(n.ToString("dd/MM"), hienSo && dt > 0 ? (dt / 1_000_000m).ToString("N0") : "",
                               (double)(dt / max) * 150, xanh, $"{n:dd/MM/yyyy}: {dt:N0} đ · {d?.SoVe ?? 0} vé");
            }).ToList();
            txtTieuDeBieuDo.Text = $"DOANH THU THUẦN THEO NGÀY TÀU CHẠY (triệu đồng) · {kq.BoLoc.TuNgay:dd/MM} – {kq.BoLoc.DenNgay:dd/MM/yyyy}";

            dgMacTau.ItemsSource = kq.TheoMacTau;
            dgGa.ItemsSource = kq.TheoGa;
            dgChuyen.ItemsSource = kq.TheoChuyen;
            dgDungGio.ItemsSource = kq.TheoMacTau.Where(m => m.SoChuyenCoGio > 0).OrderByDescending(m => m.TyLeDungGio).ToList();
            dgHangHoa.ItemsSource = kq.HangHoa;

            const double rong = 230;
            icHeSoMac.ItemsSource = kq.TheoMacTau.Where(m => m.SoChuyen > 0).OrderByDescending(m => m.HeSoGhe)
                .Select(m => new Cot(m.SoHieuMacTau, m.HeSoGhe.ToString("P1"), m.HeSoGhe * rong,
                                     m.HeSoGhe >= 0.5 ? Mau("#15803D") : m.HeSoGhe >= 0.35 ? Mau("#0284C7") : Mau("#B45309"),
                                     $"{m.SoChuyen} chuyến · {m.SoVe:N0} vé")).ToList();
            icDungGioMac.ItemsSource = kq.TheoMacTau.Where(m => m.SoChuyenCoGio > 0).OrderByDescending(m => m.TyLeDungGio)
                .Select(m => new Cot(m.SoHieuMacTau, $"{m.TyLeDungGio:P0} ({m.SoChuyenDungGio}/{m.SoChuyenCoGio})", m.TyLeDungGio * rong,
                                     m.TyLeDungGio >= 0.8 ? Mau("#15803D") : m.TyLeDungGio >= 0.6 ? Mau("#0284C7") : Mau("#B91C1C"),
                                     $"Trễ trung bình {m.TreTrungBinh:N1} phút · trễ nhất {m.TreLonNhat} phút")).ToList();

            txtTrangThai.Text = $"Lập lúc {DateTime.Now:HH:mm:ss}";
        }

        // =====================================================================
        // XUAT EXCEL
        // =====================================================================
        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e) => KichHoatXuatExcel();

        public void KichHoatXuatExcel()
        {
            if (_kq == null) { ThongBaoDialog.CanhBao("Chưa có báo cáo để xuất. Bấm \"Lập Báo Cáo\" trước."); return; }
            var hop = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Tệp Excel (*.xlsx)|*.xlsx",
                FileName = $"BaoCaoThongKe_{_kq.BoLoc.TuNgay:yyyyMMdd}_{_kq.BoLoc.DenNgay:yyyyMMdd}.xlsx"
            };
            if (hop.ShowDialog(Window.GetWindow(this)) != true) return;
            try
            {
                GhiExcel(_kq, hop.FileName);
                ThongBaoDialog.ThanhCong($"Đã xuất báo cáo ra:\n{hop.FileName}", "Xuất Excel");
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                ThongBaoDialog.Loi($"Không ghi được tệp (tệp đang mở trong Excel?):\n{ex.Message}", "Xuất Excel");
            }
        }

        // Ghi toan bo bao cao ra tep .xlsx (6 sheet). Tach rieng de kiem thu khong can hop thoai luu tep.
        public static void GhiExcel(KetQuaBaoCao k, string duongDan)
        {
            {
                using var wb = new XLWorkbook();
                var tq = wb.AddWorksheet("Tổng quan");
                tq.Cell(1, 1).Value = "BÁO CÁO THỐNG KÊ TỔNG HỢP";
                tq.Cell(1, 1).Style.Font.Bold = true;
                tq.Cell(1, 1).Style.Font.FontSize = 14;
                tq.Cell(2, 1).Value = $"Từ {k.BoLoc.TuNgay:dd/MM/yyyy} đến {k.BoLoc.DenNgay:dd/MM/yyyy}" + (k.BoLoc.SoHieuMacTau != null ? $" · mác {k.BoLoc.SoHieuMacTau}" : " · mọi mác");
                var dong = new (string, object)[]
                {
                    ("Số chuyến", k.TongQuan.SoChuyen), ("Doanh thu thuần vé (đ)", k.TongQuan.DoanhThuThuan),
                    ("Số vé", k.TongQuan.SoVe), ("Số vé hoàn", k.TongQuan.SoVeHoan), ("Tỷ lệ hoàn", k.TongQuan.TyLeHoan),
                    ("Hệ số sử dụng ghế", k.TongQuan.HeSoGhe), ("Chuyến có giờ thực tế", k.TongQuan.SoChuyenCoGio),
                    ("Chuyến đúng giờ (≤ 5 phút)", k.TongQuan.SoChuyenDungGio), ("Tỷ lệ đúng giờ", k.TongQuan.TyLeDungGio),
                    ("Trễ trung bình (phút)", k.TongQuan.TreTrungBinh), ("Số vận đơn", k.TongQuan.SoVanDon),
                    ("Sản lượng (tấn)", k.TongQuan.SanLuongTan), ("Sản lượng (tấn·km)", k.TongQuan.TanKm), ("Cước hàng (đ)", k.TongQuan.CuocHang),
                    ("Người lập", $"{DTO.Common.PhienLamViec.HoTen} ({DTO.Common.PhienLamViec.TenDangNhap})"), ("Lập lúc", DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                };
                for (int i = 0; i < dong.Length; i++)
                {
                    tq.Cell(4 + i, 1).Value = dong[i].Item1;
                    tq.Cell(4 + i, 2).Value = XLCellValue.FromObject(dong[i].Item2);
                    if (dong[i].Item1.StartsWith("Tỷ lệ") || dong[i].Item1.StartsWith("Hệ số")) tq.Cell(4 + i, 2).Style.NumberFormat.Format = "0.0%";
                    else if (dong[i].Item2 is decimal) tq.Cell(4 + i, 2).Style.NumberFormat.Format = "#,##0";
                }
                tq.Columns().AdjustToContents();

                Bang(wb, "Theo mác tàu", new[] { "Mác tàu", "Số chuyến", "Số vé", "Doanh thu thuần (đ)", "Hệ số ghế", "Chuyến có giờ", "Đúng giờ", "Tỷ lệ đúng giờ", "Trễ TB (phút)", "Trễ nhất" },
                     k.TheoMacTau.Select(m => new object[] { m.SoHieuMacTau, m.SoChuyen, m.SoVe, m.DoanhThu, m.HeSoGhe, m.SoChuyenCoGio, m.SoChuyenDungGio, m.TyLeDungGio, m.TreTrungBinh, m.TreLonNhat }));
                Bang(wb, "Theo ngày", new[] { "Ngày tàu chạy", "Số vé", "Doanh thu thuần (đ)" },
                     k.TheoNgay.Select(d => new object[] { d.Ngay, d.SoVe, d.DoanhThu }));
                Bang(wb, "Theo ga đi", new[] { "Ga đi", "Số vé", "Doanh thu thuần (đ)" },
                     k.TheoGa.Select(d => new object[] { d.TenGa, d.SoVe, d.DoanhThu }));
                Bang(wb, "Theo chuyến", new[] { "Ngày chạy", "Mác", "Cự ly (km)", "Số ghế", "Số vé", "Hệ số ghế", "Trễ ga cuối (phút)", "Sự cố" },
                     k.TheoChuyen.Select(c => new object[] { c.NgayChay, c.SoHieuMacTau, c.CuLyKm, c.SoGhe, c.SoVe, c.HeSo, (object?)c.SoPhutTre ?? "", c.SoSuCo }));
                Bang(wb, "Hàng hóa", new[] { "Loại hàng", "Số vận đơn", "Tấn", "Tấn·km", "Cước (đ)" },
                     k.HangHoa.Select(h => new object[] { h.TenHangHoa, h.SoVanDon, h.Tan, h.TanKm, h.Cuoc }));
                wb.SaveAs(duongDan);
            }
        }

        private static void Bang(XLWorkbook wb, string ten, string[] cot, IEnumerable<object[]> dong)
        {
            var ws = wb.AddWorksheet(ten);
            for (int c = 0; c < cot.Length; c++)
            {
                ws.Cell(1, c + 1).Value = cot[c];
                ws.Cell(1, c + 1).Style.Font.Bold = true;
                ws.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
            }
            int r = 2;
            foreach (var d in dong)
            {
                for (int c = 0; c < d.Length; c++)
                {
                    var o = ws.Cell(r, c + 1);
                    o.Value = XLCellValue.FromObject(d[c]);
                    if (d[c] is DateTime) o.Style.NumberFormat.Format = "dd/MM/yyyy";
                    else if (d[c] is decimal) o.Style.NumberFormat.Format = "#,##0.##";
                    else if (d[c] is double && (cot[c].Contains("Hệ số") || cot[c].Contains("Tỷ lệ"))) o.Style.NumberFormat.Format = "0.0%";
                    else if (d[c] is double) o.Style.NumberFormat.Format = "0.0";
                }
                r++;
            }
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();
        }

        // --- MainWindow goi ---
        public void KichHoatNapLai() => LapBaoCao();
        public void FocusTimKiem() => dpTu.Focus();

        private static SolidColorBrush Mau(string ma) => (SolidColorBrush)new BrushConverter().ConvertFrom(ma)!;
    }
}
