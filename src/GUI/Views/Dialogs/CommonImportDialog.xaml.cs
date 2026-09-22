using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GUI.Helpers;
using Microsoft.Win32;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace GUI.Views.Dialogs
{
    /// <summary>
    /// Hộp thoại Nhập dữ liệu CSV hàng loạt dùng chung toàn hệ thống (Generic Import Dialog).
    /// Đáp ứng chuẩn thiết kế:
    /// - Option 2 (Status Dot Badges) trên thanh công cụ
    /// - Option 4 (Left-Border Accent Cards) trên từng dòng xem trước
    /// - Tự động tải tệp mẫu, kiểm tra tính hợp lệ và lưu hàng loạt vào CSDL.
    /// </summary>
    public partial class CommonImportDialog : Window
    {
        private readonly IImportProfile _profile;
        private readonly ObservableCollection<ImportRowBase> _danhSachPreview = new();
        private SmartTableResult? _cachedTableResult;

        public int SoBanGhiDaNapThanhCong { get; private set; } = 0;

        public CommonImportDialog(IImportProfile profile)
        {
            InitializeComponent();
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));

            Loaded += CommonImportDialog_Loaded;
            MouseDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void CommonImportDialog_Loaded(object sender, RoutedEventArgs e)
        {
            txtHeaderTitle.Text = _profile.TieuDeDialog;
            dgPreview.ItemsSource = _danhSachPreview;

            chkCapNhat.Visibility = _profile.HoTroCapNhat ? Visibility.Visible : Visibility.Collapsed;

            KhoiTaoCacCotDataGrid();
        }

        private void KhoiTaoCacCotDataGrid()
        {
            dgPreview.Columns.Clear();

            // 1. Cột STT dòng
            var colDong = new DataGridTextColumn
            {
                Header = "Dòng",
                Binding = new Binding("Dong"),
                Width = 50
            };
            var styleDong = new Style(typeof(TextBlock));
            styleDong.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            styleDong.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"))));
            styleDong.Setters.Add(new Setter(TextBlock.FontSizeProperty, 11.0));
            styleDong.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            colDong.ElementStyle = styleDong;
            dgPreview.Columns.Add(colDong);

            // 2. Các cột dữ liệu theo Profile
            foreach (var colDef in _profile.CacCotPreview)
            {
                var col = new DataGridTextColumn
                {
                    Header = colDef.TieuDe,
                    Binding = new Binding(colDef.TenThuocTinh),
                    Width = colDef.ChieuRong
                };

                var style = new Style(typeof(TextBlock));
                var hAlign = colDef.CanLe switch
                {
                    TextAlignment.Center => HorizontalAlignment.Center,
                    TextAlignment.Right => HorizontalAlignment.Right,
                    _ => HorizontalAlignment.Left
                };

                style.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, hAlign));
                style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                style.Setters.Add(new Setter(TextBlock.FontSizeProperty, 11.0));

                if (colDef.IsBold)
                {
                    style.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
                }

                if (colDef.IsConsolas)
                {
                    style.Setters.Add(new Setter(TextBlock.FontFamilyProperty, new FontFamily("Consolas")));
                }

                if (colDef.CanLe == TextAlignment.Right)
                {
                    style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(0, 0, 8, 0)));
                }
                else
                {
                    style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(6, 0, 0, 0)));
                }

                col.ElementStyle = style;
                dgPreview.Columns.Add(col);
            }

            // 3. Cột Tình trạng kiểm tra (Option 4: Left-Border Accent Card)
            var colStatus = new DataGridTemplateColumn
            {
                Header = "Tình Trạng Hợp Lệ",
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            };

            // Dựng DataTemplate cho Option 4
            var template = new DataTemplate();
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(8, 3, 8, 3));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(3, 1, 1, 1));
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.White);
            borderFactory.SetBinding(Border.BorderBrushProperty, new Binding("TinhTrangBorderBrush"));
            borderFactory.SetValue(Border.MarginProperty, new Thickness(4, 2, 4, 2));
            borderFactory.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Left);

            var stackFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackFactory.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);

            var iconFactory = new FrameworkElementFactory(typeof(SymbolIcon));
            iconFactory.SetBinding(SymbolIcon.SymbolProperty, new Binding("TinhTrangSymbol"));
            iconFactory.SetValue(SymbolIcon.FontSizeProperty, 12.0);
            iconFactory.SetBinding(SymbolIcon.ForegroundProperty, new Binding("TinhTrangColor"));
            iconFactory.SetValue(SymbolIcon.MarginProperty, new Thickness(0, 0, 6, 0));
            iconFactory.SetValue(SymbolIcon.VerticalAlignmentProperty, VerticalAlignment.Center);

            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetBinding(TextBlock.TextProperty, new Binding("ThongBaoKiemTra"));
            textFactory.SetValue(TextBlock.FontSizeProperty, 11.0);
            textFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            textFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")));
            textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

            stackFactory.AppendChild(iconFactory);
            stackFactory.AppendChild(textFactory);
            borderFactory.AppendChild(stackFactory);
            template.VisualTree = borderFactory;

            colStatus.CellTemplate = template;
            dgPreview.Columns.Add(colStatus);
        }

        private void BtnTaiMau_Click(object sender, RoutedEventArgs e)
        {
            FileExchangeHelper.TaoTepMau(
                _profile.TenTepMauMacDinh,
                _profile.TieuDeDialog,
                _profile.TieuDeCotMau,
                _profile.CacDongDuLieuMau,
                _profile.GhiChuCotMau);
        }

        private void BtnChonTep_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Chọn tệp Excel hoặc CSV danh sách dữ liệu",
                Filter = "Tệp Excel & CSV (*.xlsx;*.csv)|*.xlsx;*.csv|Tệp Microsoft Excel (*.xlsx)|*.xlsx|Tệp CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*"
            };

            if (ofd.ShowDialog() != true) return;

            try
            {
                _profile.ChuanBiDuLieuDoiChieu();
                _danhSachPreview.Clear();

                var allRows = FileExchangeHelper.DocTepDuLieu(ofd.FileName);
                if (allRows.Count == 0)
                {
                    ThongBaoDialog.CanhBao("Tệp dữ liệu rỗng, không có bản ghi nào.", "Cảnh Báo");
                    return;
                }

                // Phân tích bảng thông minh: tự động tìm dòng tiêu đề và ánh xạ tên cột
                _cachedTableResult = FileExchangeHelper.PhanTichBangThongMinh(allRows, _profile.CacCotMucTieu);

                if (!_cachedTableResult.HeaderFound)
                {
                    ThongBaoDialog.CanhBao(
                        "Không tìm thấy dòng tiêu đề cột phù hợp trong tệp dữ liệu.\n\n" +
                        "Hệ thống không thể nhận diện các cột cần thiết. Vui lòng kiểm tra lại cấu trúc bảng hoặc tải Tệp Mẫu chuẩn.", 
                        "Không Nhận Diện Được Tiêu Đề");
                    return;
                }

                if (_cachedTableResult.DataRows.Count == 0)
                {
                    ThongBaoDialog.CanhBao("Tệp dữ liệu không chứa dòng dữ liệu nào sau dòng tiêu đề cột.", "Không Có Dữ Liệu");
                    return;
                }

                // Cập nhật thông tin nhận diện thông minh ở footer
                txtThongTinNhanDien.Text = $"Nhận diện tiêu đề tại dòng {_cachedTableResult.HeaderRowIndex + 1} (khớp {_cachedTableResult.MatchedColumnCount}/{_profile.CacCotMucTieu.Count} cột). Bỏ qua {_cachedTableResult.PreambleRowsSkipped} dòng tiêu đề báo cáo và {_cachedTableResult.FooterRowsSkipped} dòng tổng kết/chú thích.";

                DanhGiaLaiDuLieu();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi("Lỗi khi đọc tệp dữ liệu: " + ex.Message, "Lỗi Đọc Tệp");
            }
        }

        private void ChkCapNhat_Changed(object sender, RoutedEventArgs e)
        {
            DanhGiaLaiDuLieu();
        }

        private void DanhGiaLaiDuLieu()
        {
            if (_cachedTableResult == null || _cachedTableResult.DataRows.Count == 0) return;

            _profile.LamMoiTrangThaiDuyetTep();
            _danhSachPreview.Clear();
            bool choPhepCapNhat = chkCapNhat.IsChecked == true;
            int hopLeCount = 0;
            int capNhatCount = 0;
            int loiCount = 0;

            int stt = 0;
            foreach (var row in _cachedTableResult.DataRows)
            {
                stt++;
                var rowItem = _profile.KiemTraVaPhanTichDongThongMinh(row, stt, choPhepCapNhat);
                if (rowItem.HopLe)
                {
                    if (rowItem.LaCapNhat)
                    {
                        capNhatCount++;
                    }
                    else
                    {
                        hopLeCount++;
                    }
                }
                else
                {
                    loiCount++;
                }

                _danhSachPreview.Add(rowItem);
            }

            txtTongDong.Text = $"Tổng: {_danhSachPreview.Count} dòng";
            txtHopLe.Text = $"Thêm mới: {hopLeCount}";
            txtCapNhat.Text = $"Cập nhật: {capNhatCount}";
            bdCapNhat.Visibility = (capNhatCount > 0 || choPhepCapNhat) ? Visibility.Visible : Visibility.Collapsed;
            txtLoi.Text = $"Lỗi / Bỏ qua: {loiCount}";

            pnlPlaceholder.Visibility = _danhSachPreview.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            btnNapVaoCSDL.IsEnabled = (hopLeCount + capNhatCount) > 0;
        }

        private void BtnNapVaoCSDL_Click(object sender, RoutedEventArgs e)
        {
            var cacDongHopLe = _danhSachPreview.Where(x => x.HopLe).ToList();
            if (cacDongHopLe.Count == 0)
            {
                ThongBaoDialog.CanhBao("Không có dòng dữ liệu hợp lệ nào để nạp vào CSDL.", "Thông Báo");
                return;
            }

            int loiCount = _danhSachPreview.Count - cacDongHopLe.Count;
            int themMoiCount = cacDongHopLe.Count(x => !x.LaCapNhat);
            int capNhatCount = cacDongHopLe.Count(x => x.LaCapNhat);

            string msg = $"Phát hiện {cacDongHopLe.Count} bản ghi ĐƯỢC PHÉP NẠP:\n" +
                         $"• Thêm mới: {themMoiCount} bản ghi\n" +
                         $"• Cập nhật thông số: {capNhatCount} bản ghi";

            if (loiCount > 0)
            {
                msg += $"\n\nLƯU Ý: Có {loiCount} dòng lỗi hoặc trùng lặp sẽ bị BỎ QUA tự động để bảo vệ toàn vẹn dữ liệu.";
            }
            msg += "\n\nBạn có chắc chắn muốn nạp toàn bộ các bản ghi này vào Cơ sở dữ liệu không?";

            bool xacNhan = ThongBaoDialog.XacNhan(msg, "Xác Nhận Nạp Dữ Liệu Hàng Loạt");
            if (!xacNhan) return;

            try
            {
                bool choPhepCapNhat = chkCapNhat.IsChecked == true;
                int thanhCong = _profile.LuuDuLieuVaoCSDL(cacDongHopLe, choPhepCapNhat, out string thongBao);
                SoBanGhiDaNapThanhCong = thanhCong;

                if (thanhCong > 0)
                {
                    ThongBaoDialog.ThanhCong($"Đã xử lý thành công {thanhCong} bản ghi vào Cơ sở dữ liệu!\n\n{thongBao}", "Nhập Dữ Liệu Thành Công");
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ThongBaoDialog.Loi($"Không thể lưu dữ liệu: {thongBao}", "Lỗi Nạp Dữ Liệu");
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Sự cố khi lưu dữ liệu vào CSDL:\n{ex.Message}", "Lỗi Hệ Thống");
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
