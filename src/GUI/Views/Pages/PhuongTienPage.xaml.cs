using GUI.ViewModels.KyThuat;
using GUI.Helpers;
using GUI.Views.Dialogs;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BUS.Services;
using DTO.PhuongTien;
// Chi lay rieng SymbolRegular de tranh trung ten TextBlock / ComboBox
// giua Wpf.Ui.Controls va System.Windows.Controls.
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.Pages
{
    // =========================================================================
    // PHAN HE QUAN LY PHUONG TIEN (Doan tau, Dau may, Toa xe & Ghe)
    //
    // Lop nay chi lam nhiem vu GIAO DIEN: doc / ghi qua PhuongTienService va
    // LapTauService, chuyen thanh cac lop hien thi roi gan vao control.
    // Moi quy tac nghiep vu nam o tang BUS (PhuongTienService, QuyTacLapTau).
    //
    // Tu Sprint 1 (09/10): them / sua dau may, toa xe va lap doan tau GHI THANG CSDL
    // (kem nhat ky he thong). Khong con "thay doi tam" tren giao dien.
    // =========================================================================
    public partial class PhuongTienPage : Page
    {
        private readonly PhuongTienService _service = new();
        private readonly LapTauService _lapTauService = new();

        private DataTable _bangChuyenTau = new();
        private BoiCanhLapTauDTO? _boiCanhDangXem;

        private bool _dangNapDuLieu;
        private bool _coKetNoiCsdl;

        private Window? _cuaSoCha;

        public PhuongTienPage()
        {
            // Bat co TRUOC InitializeComponent: cac ComboBoxItem co IsSelected="True"
            // trong XAML se ban SelectionChanged ngay trong qua trinh dung cay giao dien,
            // luc do chua ket noi CSDL nen phai chan lai.
            _dangNapDuLieu = true;
            InitializeComponent();
            _dangNapDuLieu = false;

            Loaded += PhuongTienPage_Loaded;
            Loaded += (_, _) => GanPhimTat();
            Unloaded += (_, _) => GoPhimTat();
        }

        // Boc moi thao tac cham CSDL: neu hong thi hien dai canh bao thay vi lam sap ung dung
        private void ChayAnToan(Action hanhDong, string moTaThaoTac)
        {
            try
            {
                hanhDong();
            }
            catch (Exception ex)
            {
                HienCanhBao($"Lỗi khi {moTaThaoTac}: {ex.Message}");
            }
        }

        // Doc CSDL an toan: mat ket noi thi tra ve bang rong de giao dien van mo duoc
        // (khong goi lai SQL Server moi lan go phim, tranh treo cho het thoi gian cho ket noi)
        private DataTable LayBang(Func<DataTable> truyVan)
        {
            if (!_coKetNoiCsdl) return new DataTable();
            try
            {
                return truyVan();
            }
            catch (Exception ex)
            {
                HienCanhBao($"Lỗi truy vấn CSDL: {ex.Message}");
                return new DataTable();
            }
        }

        private void PhuongTienPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Chi nap mot lan dau tien
            Loaded -= PhuongTienPage_Loaded;

            _coKetNoiCsdl = KetNoiCsdlHelper.DamBaoKetNoi();
            if (!_coKetNoiCsdl)
            {
                HienCanhBao($"Không kết nối được máy chủ SQL Server ({KetNoiCsdlHelper.MayChuDangDung}). " +
                            $"{KetNoiCsdlHelper.MoTaCauHinh}. Kiểm tra appsettings.Local.json rồi mở lại ứng dụng.");
                btnThemDauMay.IsEnabled = btnThemToaXe.IsEnabled = btnLapTauKeoTha.IsEnabled = false;
            }

            NapToanBoDuLieu();
        }

        // =====================================================================
        // PHIM TAT F2
        // MainWindow an nut F2 o phan he nay va bo qua phim, nen trang tu bat
        // phim tren cua so cha - khong can sua MainWindow (file dung chung).
        // =====================================================================

        private void GanPhimTat()
        {
            GoPhimTat();
            _cuaSoCha = Window.GetWindow(this);
            if (_cuaSoCha != null) _cuaSoCha.PreviewKeyDown += CuaSoCha_PreviewKeyDown;
        }

        private void GoPhimTat()
        {
            if (_cuaSoCha != null) _cuaSoCha.PreviewKeyDown -= CuaSoCha_PreviewKeyDown;
            _cuaSoCha = null;
        }

        private void CuaSoCha_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || !IsVisible) return;
            if (e.Key != Key.F2 || Keyboard.Modifiers != ModifierKeys.None) return;

            e.Handled = true;
            KichHoatThemMoi();
        }

        // =====================================================================
        // NAP DU LIEU CHUNG
        // =====================================================================

        public void NapToanBoDuLieu()
        {
            _dangNapDuLieu = true;
            try
            {
                NapChiSoTongQuan();
                NapBoLocDauMay();
                NapDanhSachDauMay();
                NapBoLocChungLoaiToa();
                NapDanhSachToaXeDoi();
                NapDanhSachChungLoaiToa();
                NapDanhSachChuyenTau();
            }
            catch (Exception ex)
            {
                HienCanhBao($"Lỗi nạp dữ liệu phân hệ phương tiện: {ex.Message}");
            }
            finally
            {
                _dangNapDuLieu = false;
            }

            // Chon mac dinh sau khi da nap xong bo loc
            if (cboChuyenTauLapTau.Items.Count > 0 && cboChuyenTauLapTau.SelectedIndex < 0)
                cboChuyenTauLapTau.SelectedIndex = 0;

            if (cboChuyenTauSoDo.Items.Count > 0 && cboChuyenTauSoDo.SelectedIndex < 0)
                cboChuyenTauSoDo.SelectedIndex = 0;

            if (dgDauMay.Items.Count > 0 && dgDauMay.SelectedIndex < 0)
                dgDauMay.SelectedIndex = 0;
        }

        private void HienCanhBao(string thongDiep)
        {
            bdCanhBaoKetNoi.Visibility = Visibility.Visible;
            txtCanhBaoKetNoi.Text = thongDiep;
        }

        private void NapChiSoTongQuan()
        {
            int tongDauMay = 0, dauMaySanSang = 0, tongToaXe = 0;
            int doanTauDuyet = 0, tongDoanTau = 0, tongChoNgoi = 0;

            DataTable dt = LayBang(_service.LayThongKeTongQuan);
            if (dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];
                tongDauMay = Convert.ToInt32(r["TongDauMay"]);
                dauMaySanSang = Convert.ToInt32(r["DauMaySanSang"]);
                tongToaXe = Convert.ToInt32(r["TongToaXe"]);
                doanTauDuyet = Convert.ToInt32(r["DoanTauDaDuyet"]);
                tongDoanTau = Convert.ToInt32(r["TongDoanTau"]);
                tongChoNgoi = Convert.ToInt32(r["TongChoNgoi"]);
            }

            txtKpiDauMaySanSang.Text = dauMaySanSang.ToString("N0");
            txtKpiDauMayTong.Text = "/" + tongDauMay.ToString("N0");
            txtKpiToaXe.Text = tongToaXe.ToString("N0");
            txtKpiDoanTauDuyet.Text = doanTauDuyet.ToString("N0");
            txtKpiDoanTauTong.Text = "/" + tongDoanTau.ToString("N0");
            txtKpiChoNgoi.Text = tongChoNgoi.ToString("N0");
        }

        // =====================================================================
        // TAB 1: HO SO DAU MAY
        // =====================================================================

        private void NapBoLocDauMay()
        {
            DataTable dt = LayBang(_service.LayDanhSachDonViQuanLy);

            cboDepot.Items.Clear();
            cboDepot.Items.Add(new ComboBoxItem { Content = "Tất cả xí nghiệp", Tag = "ALL", IsSelected = true });

            foreach (DataRow r in dt.Rows)
            {
                string ten = r["DonViQuanLy"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(ten))
                    cboDepot.Items.Add(new ComboBoxItem { Content = ten, Tag = ten });
            }

            cboDepot.SelectedIndex = 0;
        }

        private void NapDanhSachDauMay()
        {
            string tuKhoa = txtTimDauMay.Text?.Trim() ?? string.Empty;
            string depot = LayTagDangChon(cboDepot) ?? "ALL";
            string trangThai = LayTagDangChon(cboTrangThaiDauMay) ?? "ALL";

            DataTable bang = LayBang(() => _service.LayDanhSachDauMay(tuKhoa, depot, trangThai));

            var danhSach = bang.Rows.Cast<DataRow>().Select(DauMayHienThi.TuDongDuLieu).ToList();
            dgDauMay.ItemsSource = danhSach;
            txtBadgeDauMay.Text = danhSach.Count.ToString();
        }

        private void DgDauMay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool coChon = dgDauMay.SelectedItem is DauMayHienThi;
            btnSuaDauMay.IsEnabled = coChon && _coKetNoiCsdl;
            btnSuaChiTietDauMay.IsEnabled = coChon && _coKetNoiCsdl;

            if (dgDauMay.SelectedItem is not DauMayHienThi dm)
            {
                XoaTrangChiTietDauMay();
                return;
            }

            ChayAnToan(() => HienThiChiTietDauMay(dm), "hiển thị lý lịch đầu máy");
        }

        private void XoaTrangChiTietDauMay()
        {
            txtChiTietTenDauMay.Text = "LÝ LỊCH ĐẦU MÁY";
            txtChiTietMoTaDauMay.Text = "Chọn một đầu máy để xem thông số kỹ thuật";
            txtChiTietMaDauMay.Text = "—";
            pnlThongSoKyThuat.Children.Clear();
            dgLichSuVanDung.ItemsSource = null;
            txtTrongLichSu.Visibility = Visibility.Collapsed;

            txtBaoDuongCap.Text = "Cấp kế tiếp: —";
            txtBaoDuongTyLe.Text = "0%";
            txtBaoDuongChiTiet.Text = "Chưa có dữ liệu.";
            DatTyLeVach(colBaoDuongDaChay, colBaoDuongConLai, 0);
        }

        private void HienThiChiTietDauMay(DauMayHienThi dm)
        {
            txtChiTietTenDauMay.Text = dm.SoHieuDauMay;
            txtChiTietMoTaDauMay.Text = $"{dm.TenMoTaDong} · {dm.DonViQuanLy}";
            txtChiTietMaDauMay.Text = $"ID: #{dm.MaDauMay}";

            // --- Tien do bao duong: chu ky tu lan bao duong gan nhat (khop man hinh Bao tri) ---
            var td = dm.TienDoBaoDuong;

            txtBaoDuongCap.Text = $"Cấp cần chú ý: {td.Cap} · mốc {td.MocKm:N0} km";
            txtBaoDuongTyLe.Text = $"{td.TyLePhanTram:N1}%";

            var mauVach = (SolidColorBrush)new BrushConverter().ConvertFrom(DauMayHienThi.LayMauTheoMucCanhBao(td.MucCanhBao))!;
            bdBaoDuongVach.Background = mauVach;
            DatTyLeVach(colBaoDuongDaChay, colBaoDuongConLai, (double)td.TyLePhanTram);

            string tuLan = td.CoLichSu
                ? $"chu kỳ {td.ChuKyKm:N0} km tính từ lần bảo dưỡng gần nhất"
                : $"chưa có lịch sử bảo dưỡng {td.Cap}, chu kỳ {td.ChuKyKm:N0} km tính từ 0 km";
            txtBaoDuongChiTiet.Text = td.MucCanhBao switch
            {
                "QUA_HAN" => $"Quá hạn bảo dưỡng {td.Cap}: {td.MoTaConLai} ({tuLan}). Cần đưa vào xưởng.",
                "SAP_TOI" => $"Sắp tới hạn bảo dưỡng {td.Cap}: {td.MoTaConLai} ({tuLan}). Nên lên kế hoạch.",
                _ => $"Bình thường: {td.MoTaConLai} nữa mới tới mốc {td.Cap} ({tuLan})."
            };

            // --- Thong so ky thuat dong may ---
            pnlThongSoKyThuat.Children.Clear();
            ThemDongThongSo("Mã dòng máy", dm.MaDongCode);
            ThemDongThongSo("Nhà sản xuất", dm.NhaSanXuat);
            ThemDongThongSo("Công suất định mức", $"{dm.CongSuatHP:N0} HP");
            ThemDongThongSo("Tốc độ cấu tạo", $"{dm.TocDoToiDaKmh:N0} km/h");
            ThemDongThongSo("Sức kéo tối đa", $"{dm.SucKeoToiDaTan:N0} tấn");
            ThemDongThongSo("Dung tích bồn dầu", $"{dm.DungTichBonDauLit:N0} lít");
            ThemDongThongSo("Trọng lượng đầu máy", $"{dm.TrongLuongTan:N1} tấn");
            ThemDongThongSo("Chiều dài đầu máy", $"{dm.ChieuDaiM:N1} m");
            ThemDongThongSo("Năm sản xuất", $"{dm.NamSanXuat} ({dm.TuoiKhaiThac} năm khai thác)");
            ThemDongThongSo("Số km tích lũy", $"{dm.SoKmTichLuy:N0} km", laDongCuoi: true);

            // --- Lich su van dung ---
            DataTable ls = LayBang(() => _service.LayLichSuVanDung(dm.MaDauMay));
            var danhSachLs = ls.Rows.Cast<DataRow>().Select(LichSuVanDungHienThi.TuDongDuLieu).ToList();

            dgLichSuVanDung.ItemsSource = danhSachLs;
            dgLichSuVanDung.Visibility = danhSachLs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            txtTrongLichSu.Visibility = danhSachLs.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ThemDongThongSo(string nhan, string giaTri, bool laDongCuoi = false)
        {
            var hang = new Grid { Height = 26 };
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hang.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tbNhan = new TextBlock
            {
                Text = nhan,
                Style = (Style)FindResource("SpecLabel")
            };
            Grid.SetColumn(tbNhan, 0);

            var tbGiaTri = new TextBlock
            {
                Text = giaTri,
                Style = (Style)FindResource("SpecValue"),
                MaxWidth = 190
            };
            Grid.SetColumn(tbGiaTri, 1);

            hang.Children.Add(tbNhan);
            hang.Children.Add(tbGiaTri);

            var vien = new Border
            {
                Child = hang,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#F1F5F9")!,
                BorderThickness = new Thickness(0, 0, 0, laDongCuoi ? 0 : 1)
            };

            pnlThongSoKyThuat.Children.Add(vien);
        }

        private void TxtTimDauMay_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            ChayAnToan(NapDanhSachDauMay, "lọc danh sách đầu máy");
        }

        private void BoLocDauMay_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            ChayAnToan(NapDanhSachDauMay, "lọc danh sách đầu máy");
        }

        private void BtnXoaLocDauMay_Click(object sender, RoutedEventArgs e)
        {
            _dangNapDuLieu = true;
            txtTimDauMay.Text = string.Empty;
            cboDepot.SelectedIndex = 0;
            cboTrangThaiDauMay.SelectedIndex = 0;
            _dangNapDuLieu = false;

            ChayAnToan(NapDanhSachDauMay, "xóa bộ lọc đầu máy");
        }

        private void BtnNapLaiDauMay_Click(object sender, RoutedEventArgs e)
        {
            ChayAnToan(() => { NapChiSoTongQuan(); NapDanhSachDauMay(); }, "nạp lại dữ liệu đầu máy");
        }

        // --- Them / sua dau may (ghi CSDL qua PhuongTienService) ---

        private void BtnThemDauMay_Click(object sender, RoutedEventArgs e) => ThemDauMay();

        private void BtnSuaDauMay_Click(object sender, RoutedEventArgs e) => SuaDauMay();

        private void DongDauMay_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SuaDauMay();

        private void ThemDauMay()
        {
            if (!_coKetNoiCsdl) return;
            tabPhuongTien.SelectedIndex = 0;

            string? Luu(DauMayHienThi dm)
            {
                var et = dm.ToET();
                if (!_service.ThemDauMay(et, out string loi)) return loi;
                dm.MaDauMay = et.MaDauMay;
                return null;
            }

            List<DongDauMayMau> dongDauMay;
            try
            {
                dongDauMay = LayDongDauMay();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không đọc được danh mục dòng đầu máy: {ex.Message}", "Lỗi CSDL");
                return;
            }

            var dlg = new DauMayDialog(null, dongDauMay, LayDanhSachXiNghiep(), Luu) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            LamMoiSauKhiLuuDauMay(dlg.KetQua.MaDauMay);
            ThongBaoDialog.ThanhCong($"Đã thêm đầu máy {dlg.KetQua.SoHieuDauMay} vào đội phương tiện.", "Thêm đầu máy");
        }

        private void SuaDauMay()
        {
            if (!_coKetNoiCsdl || dgDauMay.SelectedItem is not DauMayHienThi dangChon) return;

            // Ban ghi luc nguoi dung mo dialog: Service dung de phat hien nguoi khac sua chen giua
            var goc = dangChon.ToET();

            string? Luu(DauMayHienThi dm)
                => _service.CapNhatDauMay(dm.ToET(), goc, out string loi) ? null : loi;

            List<DongDauMayMau> dongDauMay;
            try
            {
                dongDauMay = LayDongDauMay();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không đọc được danh mục dòng đầu máy: {ex.Message}", "Lỗi CSDL");
                return;
            }

            var dlg = new DauMayDialog(dangChon.SaoChep(), dongDauMay, LayDanhSachXiNghiep(), Luu)
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            LamMoiSauKhiLuuDauMay(dangChon.MaDauMay);
        }

        private void LamMoiSauKhiLuuDauMay(int maDauMay)
        {
            ChayAnToan(() =>
            {
                NapChiSoTongQuan();

                // Xi nghiep moi (neu co) phai vao bo loc: nap lai bo loc, giu lua chon
                string? depot = LayTagDangChon(cboDepot);
                _dangNapDuLieu = true;
                NapBoLocDauMay();
                ChonLaiTheoTag(cboDepot, depot ?? "ALL");
                _dangNapDuLieu = false;

                NapDanhSachDauMay();

                // Bi bo loc hien tai an mat thi xoa loc de nguoi dung thay ngay ban ghi vua luu
                if (!ChonDongTheoMa(dgDauMay, (DauMayHienThi d) => d.MaDauMay == maDauMay))
                {
                    BtnXoaLocDauMay_Click(this, new RoutedEventArgs());
                    ChonDongTheoMa(dgDauMay, (DauMayHienThi d) => d.MaDauMay == maDauMay);
                }

                // Doi dau may co the doi ket qua kiem tra cua chuyen dang xem o tab 3
                HienThiChuyenTauLapTau();
            }, "cập nhật danh sách đầu máy");
        }

        private List<DongDauMayMau> LayDongDauMay()
            => _service.LayDanhSachDongDauMay().Rows.Cast<DataRow>().Select(DanhMucMauPhuongTien.TuDongCsdl).ToList();

        private List<string> LayDanhSachXiNghiep()
        {
            var ds = new List<string>();
            foreach (object muc in cboDepot.Items)
            {
                if (muc is ComboBoxItem cbi && cbi.Tag?.ToString() is string tag && tag != "ALL")
                    ds.Add(tag);
            }
            return ds;
        }

        // =====================================================================
        // TAB 2: DOI TOA XE
        // =====================================================================

        private void NapBoLocChungLoaiToa()
        {
            DataTable dt = LayBang(_service.LayDanhSachChungLoaiToa);

            cboChungLoaiToa.Items.Clear();
            cboChungLoaiToa.Items.Add(new ComboBoxItem { Content = "Tất cả chủng loại", Tag = "0", IsSelected = true });

            foreach (DataRow r in dt.Rows)
            {
                int ma = Convert.ToInt32(r["MaChungLoai"]);
                string code = r["MaChungLoaiCode"].ToString() ?? "";
                cboChungLoaiToa.Items.Add(new ComboBoxItem { Content = $"{code} — {r["TenMoTa"]}", Tag = ma.ToString() });
            }

            cboChungLoaiToa.SelectedIndex = 0;
        }

        private void NapDanhSachToaXeDoi()
        {
            string tuKhoa = txtTimToaXe.Text?.Trim() ?? string.Empty;
            int maChungLoai = int.TryParse(LayTagDangChon(cboChungLoaiToa), out int cl) ? cl : 0;

            DataTable dt = LayBang(() => _service.LayDanhSachToaXeDoi(tuKhoa, maChungLoai));

            var danhSach = dt.Rows.Cast<DataRow>().Select(ToaXeHienThi.TuDongDuLieu).ToList();
            dgToaXeDoi.ItemsSource = danhSach;
            txtBadgeToaXe.Text = danhSach.Count.ToString();
        }

        private void NapDanhSachChungLoaiToa()
        {
            DataTable dt = LayBang(_service.LayDanhSachChungLoaiToa);

            icChungLoaiToa.ItemsSource = dt.Rows.Cast<DataRow>().Select(r => new ChungLoaiToaThongKe(
                r["MaChungLoaiCode"].ToString() ?? "",
                r["TenMoTa"].ToString() ?? "",
                Convert.ToDecimal(r["ChieuDaiChuanM"]),
                Convert.ToInt32(r["SoTruc"]),
                Convert.ToInt32(r["SoToaTrongDoi"]))).ToList();
        }

        private void DgToaXeDoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnSuaToaXe.IsEnabled = dgToaXeDoi.SelectedItem is ToaXeHienThi && _coKetNoiCsdl;
        }

        private void TxtTimToaXe_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            ChayAnToan(NapDanhSachToaXeDoi, "lọc danh sách toa xe");
        }

        private void BoLocToaXe_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            ChayAnToan(NapDanhSachToaXeDoi, "lọc danh sách toa xe");
        }

        private void BtnNapLaiToaXe_Click(object sender, RoutedEventArgs e)
        {
            ChayAnToan(() => { NapChiSoTongQuan(); NapDanhSachToaXeDoi(); NapDanhSachChungLoaiToa(); }, "nạp lại đội toa xe");
        }

        // --- Them / sua toa xe (ghi CSDL qua PhuongTienService) ---

        private void BtnThemToaXe_Click(object sender, RoutedEventArgs e) => ThemToaXe();

        private void BtnSuaToaXe_Click(object sender, RoutedEventArgs e) => SuaToaXe();

        private void DongToaXe_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SuaToaXe();

        private void ThemToaXe()
        {
            if (!_coKetNoiCsdl) return;
            tabPhuongTien.SelectedIndex = 1;

            string? Luu(ToaXeHienThi tx)
            {
                var et = tx.ToET();
                if (!_service.ThemToaXe(et, out string loi)) return loi;
                tx.MaToaXe = et.MaToaXe;
                return null;
            }

            List<ChungLoaiToaMau> chungLoai;
            List<string> soHieuDaCo;
            try
            {
                chungLoai = LayChungLoaiToa();
                soHieuDaCo = _service.LayDanhSachToaXeDoi(null, 0).Rows.Cast<DataRow>()
                                     .Select(r => r["SoHieuToaXe"].ToString() ?? "").ToList();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không đọc được danh mục toa xe: {ex.Message}", "Lỗi CSDL");
                return;
            }

            var dlg = new ToaXeDialog(null, chungLoai, soHieuDaCo, Luu) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            LamMoiSauKhiLuuToaXe(dlg.KetQua.MaToaXe);
            ThongBaoDialog.ThanhCong($"Đã thêm toa {dlg.KetQua.SoHieuToaXe} vào đội toa xe.", "Thêm toa xe");
        }

        private void SuaToaXe()
        {
            if (!_coKetNoiCsdl || dgToaXeDoi.SelectedItem is not ToaXeHienThi dangChon) return;

            var goc = dangChon.ToET();

            string? Luu(ToaXeHienThi tx)
                => _service.CapNhatToaXe(tx.ToET(), goc, out string loi) ? null : loi;

            List<ChungLoaiToaMau> chungLoai;
            try
            {
                chungLoai = LayChungLoaiToa();
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không đọc được danh mục chủng loại toa: {ex.Message}", "Lỗi CSDL");
                return;
            }

            var dlg = new ToaXeDialog(dangChon.SaoChep(), chungLoai, Array.Empty<string>(), Luu)
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            LamMoiSauKhiLuuToaXe(dangChon.MaToaXe);
        }

        private void LamMoiSauKhiLuuToaXe(int maToaXe)
        {
            ChayAnToan(() =>
            {
                NapChiSoTongQuan();
                NapDanhSachToaXeDoi();
                NapDanhSachChungLoaiToa();

                if (!ChonDongTheoMa(dgToaXeDoi, (ToaXeHienThi t) => t.MaToaXe == maToaXe))
                {
                    _dangNapDuLieu = true;
                    txtTimToaXe.Text = string.Empty;
                    cboChungLoaiToa.SelectedIndex = 0;
                    _dangNapDuLieu = false;

                    NapDanhSachToaXeDoi();
                    ChonDongTheoMa(dgToaXeDoi, (ToaXeHienThi t) => t.MaToaXe == maToaXe);
                }

                HienThiChuyenTauLapTau();
            }, "cập nhật danh sách toa xe");
        }

        private List<ChungLoaiToaMau> LayChungLoaiToa()
            => _service.LayDanhSachChungLoaiToa().Rows.Cast<DataRow>().Select(DanhMucMauPhuongTien.TuChungLoaiCsdl).ToList();

        // =====================================================================
        // TAB 3: LAP DOAN TAU (vanhanh.DoanTau + ChiTietDoanTau)
        // =====================================================================

        private void NapDanhSachChuyenTau()
        {
            _bangChuyenTau = LayBang(_service.LayDanhSachChuyenTauLapTau);

            cboChuyenTauLapTau.Items.Clear();
            cboChuyenTauSoDo.Items.Clear();

            foreach (DataRow r in _bangChuyenTau.Rows)
            {
                string nhan = TaoNhanChuyenTau(r);
                string ma = r["MaChuyenTau"].ToString() ?? "0";

                cboChuyenTauLapTau.Items.Add(new ComboBoxItem { Content = nhan, Tag = ma });
                cboChuyenTauSoDo.Items.Add(new ComboBoxItem { Content = nhan, Tag = ma });
            }

            txtBadgeDoanTau.Text = _bangChuyenTau.Rows.Count.ToString();
        }

        private static string TaoNhanChuyenTau(DataRow r)
        {
            var ngay = Convert.ToDateTime(r["NgayXuatPhat"]);
            return $"{r["SoHieuMacTau"]} · {ngay:dd/MM/yyyy} · {r["TenGaDi"]} → {r["TenGaDen"]}";
        }

        private int LayMaChuyenTauLapTau()
            => int.TryParse(LayTagDangChon(cboChuyenTauLapTau), out int v) ? v : 0;

        private void CboChuyenTauLapTau_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            HienThiChuyenTauLapTau();
        }

        // Nap boi canh (doan tau, hanh trinh, dau may, toa) cua chuyen dang chon roi hien len tab 3
        private void HienThiChuyenTauLapTau()
        {
            int ma = LayMaChuyenTauLapTau();
            _boiCanhDangXem = null;

            ChayAnToan(() =>
            {
                _boiCanhDangXem = ma > 0 && _coKetNoiCsdl ? _lapTauService.LayBoiCanh(ma) : null;
                HienThiDoanTau(_boiCanhDangXem);
            }, "nạp thông tin lập đoàn tàu");
        }

        private void HienThiDoanTau(BoiCanhLapTauDTO? bc)
        {
            if (bc == null)
            {
                txtTomTatHanhTrinh.Text = "";
                bdChiXem.Visibility = Visibility.Collapsed;
                btnLapTauKeoTha.IsEnabled = false;
                HienTheTrong("Chưa chọn chuyến tàu");
                dgBienCheToa.ItemsSource = null;
                txtTrongBienChe.Text = "Chưa chọn chuyến tàu.";
                txtTrongBienChe.Visibility = Visibility.Visible;
                icHangMucAnToan.ItemsSource = null;
                CapNhatKetLuanAnToan(null, "CHƯA CHỌN CHUYẾN TÀU", "Chọn một chuyến để chạy kiểm tra");
                return;
            }

            txtTomTatHanhTrinh.Text =
                $"{bc.LoaiTau} · Khởi hành {bc.GioXuatPhatKH:HH:mm dd/MM} · Về đích {bc.GioVeDichKH:HH:mm dd/MM} · " +
                $"{bc.HanhTrinh.Count} ga dừng · Trạng thái: {bc.TrangThaiChuyen}";

            // Chuyen dang chay / da xong / da huy: chi xem
            bdChiXem.Visibility = bc.ChoPhepSua ? Visibility.Collapsed : Visibility.Visible;
            txtChiXem.Text = bc.TrangThaiChuyen switch
            {
                "DANG_CHAY" or "TRE_GIO" => "Đang chạy · chỉ xem",
                "HOAN_THANH" => "Đã hoàn thành · chỉ xem",
                "DA_HUY" => "Đã hủy",
                _ => "Chỉ xem"
            };
            bdChiXem.ToolTip = bc.LyDoKhongChoSua;
            btnLapTauKeoTha.IsEnabled = bc.ChoPhepSua && _coKetNoiCsdl;
            btnLapTauKeoTha.ToolTip = bc.ChoPhepSua
                ? (bc.MaDoanTau == null ? "Lập đoàn tàu bằng kéo – thả toa xe, lưu vào CSDL (F2)"
                                        : "Sửa đoàn tàu bằng kéo – thả toa xe, lưu vào CSDL (F2)")
                : bc.LyDoKhongChoSua;

            if (bc.MaDoanTau == null)
            {
                HienTheTrong("Chuyến này chưa lập đoàn tàu");
                dgBienCheToa.ItemsSource = null;
                txtTrongBienChe.Text = bc.ChoPhepSua
                    ? "Chuyến này chưa lập đoàn tàu. Bấm \"Lập Tàu Kéo – Thả\" để chọn đầu máy và xếp toa."
                    : "Chuyến này không có hồ sơ đoàn tàu.";
                txtTrongBienChe.Visibility = Visibility.Visible;

                icHangMucAnToan.ItemsSource = new List<HangMucAnToanHienThi>
                {
                    HangMucAnToanHienThi.Loi("Lập đoàn tàu",
                        "Chuyến tàu này chưa được lập đoàn. Cần chỉ định đầu máy và biên chế toa xe trước khi kiểm tra an toàn.")
                };
                CapNhatKetLuanAnToan(false, "CHƯA LẬP ĐOÀN TÀU", "Chuyến chưa có hồ sơ đoàn tàu trong hệ thống");
                return;
            }

            // Kiem tra doan tau dang luu bang chinh bo luat dung khi lap tau
            var kq = LapTauService.KiemTra(bc, LapTauService.PhuongAnHienTai(bc));
            HienTheThongSo(bc, kq);
            HienBienChe(bc);

            var hangMuc = kq.HangMuc.Select(HangMucAnToanHienThi.TuDTO).ToList();
            hangMuc.Add(bc.DaDuyetAnToan
                ? HangMucAnToanHienThi.Dat("Xác nhận duyệt an toàn", "Đoàn tàu đã được duyệt điều kiện an toàn kỹ thuật.")
                : HangMucAnToanHienThi.CanhBao("Xác nhận duyệt an toàn",
                    "Đoàn tàu chưa được duyệt an toàn (mới lập hoặc vừa đổi thành phần). Bước duyệt thuộc phân hệ Bảo trì."));
            icHangMucAnToan.ItemsSource = hangMuc;

            int soLoi = hangMuc.Count(h => h.MucDo == "LOI");
            int soCanhBao = hangMuc.Count(h => h.MucDo == "CANH_BAO");

            if (soLoi > 0)
                CapNhatKetLuanAnToan(false, "KHÔNG ĐỦ ĐIỀU KIỆN", $"Có {soLoi} hạng mục không đạt, cần xử lý trước khi xuất ga");
            else if (soCanhBao > 0)
                CapNhatKetLuanAnToan(null, "ĐẠT CÓ LƯU Ý", $"Các hạng mục kỹ thuật đạt, còn {soCanhBao} lưu ý");
            else
                CapNhatKetLuanAnToan(true, "ĐỦ ĐIỀU KIỆN XUẤT GA", "Toàn bộ hạng mục đối chiếu đều đạt yêu cầu");
        }

        private void HienTheTrong(string lyDo)
        {
            txtDauMayChinh.Text = "—";
            txtDauMayChinhPhu.Text = lyDo;
            txtDauMayDay.Text = "—";
            txtDauMayDayPhu.Text = "Không biên chế máy đẩy";
            txtTongChieuDai.Text = "—";
            txtTongSoToa.Text = string.Empty;
            txtChieuDaiPhu.Text = "Chưa có dữ liệu";
            txtTongTrongLuong.Text = "—";
            txtTrongLuongPhu.Text = "Chưa có dữ liệu";
        }

        // 4 the thong so: so lieu tinh tu toa that (lon nhat tren cac chang)
        private void HienTheThongSo(BoiCanhLapTauDTO bc, KetQuaKiemTraLapTauDTO kq)
        {
            var dm = bc.TimDauMay(bc.MaDauMayChinh);
            txtDauMayChinh.Text = dm?.SoHieuDauMay ?? "—";
            txtDauMayChinhPhu.Text = dm != null
                ? $"Dòng {dm.MaDongCode} · Sức kéo {dm.SucKeoToiDaTan:N0} tấn"
                : "Chưa chỉ định đầu máy kéo";

            var day = bc.TimDauMay(bc.MaDauMayDay);
            txtDauMayDay.Text = day?.SoHieuDauMay ?? "—";
            txtDauMayDayPhu.Text = day != null
                ? $"Dòng {day.MaDongCode} · Hỗ trợ vượt đèo dốc"
                : bc.KhuGianCanDay.Count > 0 ? $"Không biên chế · hành trình qua {bc.KhuGianCanDay.Count} khu gian dốc" : "Không biên chế máy đẩy";

            bool coToa = bc.DoanHienTai.Count > 0;
            txtTongChieuDai.Text = coToa ? $"{kq.ChieuDaiLonNhatM:N1} m" : "—";
            txtTongSoToa.Text = coToa ? (kq.CoCatNoi ? $"· tối đa {kq.SoToaLonNhat} toa" : $"· {kq.SoToaLonNhat} toa") : "";
            txtChieuDaiPhu.Text = !coToa
                ? "Đoàn tàu chưa có toa nào"
                : kq.CoCatNoi
                    ? $"Dài nhất đoạn {kq.ChangDaiNhat} · cắt / nối {kq.MoTaCatNoi}"
                    : "Đoàn tàu chạy suốt hành trình";

            // So lieu luu trong DoanTau lech so tinh tu toa that (vd man hinh khac ghi de): bao ro
            bool lechCsdl = coToa && (kq.SoToaLonNhat != bc.TongSoToa || kq.ChieuDaiLonNhatM != bc.TongChieuDaiM ||
                                      kq.TrongLuongLonNhatTan != bc.TongTrongLuongTan);
            if (lechCsdl)
                txtChieuDaiPhu.Text = $"CSDL đang ghi {bc.TongSoToa} toa · {bc.TongChieuDaiM:N1} m, khác số tính từ toa thật";

            txtTongTrongLuong.Text = coToa ? $"{kq.TrongLuongLonNhatTan:N1} tấn" : "—";
            var (_, tyLe, thongDiep) = PhuongTienService.DoiChieuSucKeo(kq.TrongLuongLonNhatTan, dm?.SucKeoToiDaTan);
            txtTrongLuongPhu.Text = !coToa ? "Chưa có dữ liệu"
                : tyLe <= 0 ? thongDiep
                : kq.CoCatNoi ? $"Chiếm {tyLe:N1}% sức kéo · đoạn nặng nhất {kq.ChangNangNhat}"
                : $"Chiếm {tyLe:N1}% sức kéo định mức";
        }

        // Bang bien che: toa that theo thu tu moc noi, kem pham vi chay
        private void HienBienChe(BoiCanhLapTauDTO bc)
        {
            string MaGa(int? maGa, string macDinh) => maGa == null
                ? macDinh
                : bc.HanhTrinh.FirstOrDefault(d => d.MaGa == maGa)?.MaGaCode ?? $"#{maGa}";

            string gaDau = bc.HanhTrinh.Count > 0 ? bc.HanhTrinh[0].MaGaCode : bc.MaGaDiCode;
            string gaCuoi = bc.HanhTrinh.Count > 0 ? bc.HanhTrinh[^1].MaGaCode : bc.MaGaDenCode;

            var dong = new List<ToaBienCheHienThi>();
            for (int i = 0; i < bc.DoanHienTai.Count; i++)
            {
                var t = bc.DoanHienTai[i];
                var toa = bc.TimToa(t.MaToaXe);
                if (toa == null) continue;

                bool rieng = t.MaGaNoi != null || t.MaGaCat != null;
                string phamVi = rieng ? $"{MaGa(t.MaGaNoi, gaDau)} → {MaGa(t.MaGaCat, gaCuoi)}" : "Suốt hành trình";
                dong.Add(ToaBienCheHienThi.TuToa(i + 1, toa, phamVi, rieng));
            }

            dgBienCheToa.ItemsSource = dong;
            txtTrongBienChe.Text = "Đoàn tàu chưa có toa nào trong CSDL. Bấm \"Lập Tàu Kéo – Thả\" để xếp toa.";
            txtTrongBienChe.Visibility = dong.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // dat = true: xanh la | dat = null: vang | dat = false: do
        private void CapNhatKetLuanAnToan(bool? dat, string tieuDe, string moTa)
        {
            string nen, vien, chu;
            SymbolRegular bieuTuong;

            if (dat == true)
            {
                nen = "#F0FDF4"; vien = "#BBF7D0"; chu = "#15803D";
                bieuTuong = SymbolRegular.CheckmarkCircle24;
            }
            else if (dat == null)
            {
                nen = "#FFFBEB"; vien = "#FDE68A"; chu = "#B45309";
                bieuTuong = SymbolRegular.Warning24;
            }
            else
            {
                nen = "#FEF2F2"; vien = "#FECACA"; chu = "#B91C1C";
                bieuTuong = SymbolRegular.DismissCircle24;
            }

            var conv = new BrushConverter();
            bdKetLuanAnToan.Background = (Brush)conv.ConvertFrom(nen)!;
            bdKetLuanAnToan.BorderBrush = (Brush)conv.ConvertFrom(vien)!;
            icoKetLuanAnToan.Symbol = bieuTuong;
            icoKetLuanAnToan.Foreground = (Brush)conv.ConvertFrom(chu)!;
            txtKetLuanAnToan.Text = tieuDe;
            txtKetLuanAnToan.Foreground = (Brush)conv.ConvertFrom(chu)!;
            txtKetLuanAnToanPhu.Text = moTa;
        }

        private void BtnNapLaiDoanTau_Click(object sender, RoutedEventArgs e)
        {
            int maDangChon = LayMaChuyenTauLapTau();

            ChayAnToan(() =>
            {
                NapLaiDanhSachChuyenTauGiuChon(maDangChon);
                HienThiChuyenTauLapTau();
            }, "nạp lại danh sách chuyến tàu");
        }

        // Nap lai danh sach chuyen (dung chung cho tab 3 va tab 4) ma van giu chuyen dang chon o ca hai tab.
        // Truoc day chi chon lai o tab 3 -> o chuyen tau tab 4 bi trong, o toa van giu toa cua lan truoc.
        private void NapLaiDanhSachChuyenTauGiuChon(int maChuyenLapTau)
        {
            string tagSoDo = LayTagDangChon(cboChuyenTauSoDo) ?? "";

            _dangNapDuLieu = true;
            NapDanhSachChuyenTau();
            _dangNapDuLieu = false;

            ChonLaiTheoTag(cboChuyenTauLapTau, maChuyenLapTau.ToString());
            ChonLaiTheoTag(cboChuyenTauSoDo, tagSoDo);   // kich hoat nap lai toa + so do ghe
        }

        // --- Lap tau keo - tha (ghi CSDL qua LapTauService) ---

        private void BtnLapTauKeoTha_Click(object sender, RoutedEventArgs e) => MoLapTauKeoTha();

        private void MoLapTauKeoTha()
        {
            if (!_coKetNoiCsdl) return;
            tabPhuongTien.SelectedIndex = 2;

            int ma = LayMaChuyenTauLapTau();
            if (ma <= 0)
            {
                ThongBaoDialog.ThongTin("Hãy chọn một chuyến tàu trước khi lập đoàn tàu.", "Chưa chọn chuyến");
                return;
            }

            // Nap lai boi canh moi nhat ngay truoc khi mo (PhienBan dung de chong ghi de)
            BoiCanhLapTauDTO? bc;
            try
            {
                bc = _lapTauService.LayBoiCanh(ma);
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không đọc được dữ liệu lập tàu: {ex.Message}", "Lỗi CSDL");
                return;
            }

            if (bc == null)
            {
                ThongBaoDialog.CanhBao("Không tìm thấy chuyến tàu (có thể đã bị xóa). Hãy nạp lại danh sách.", "Không có chuyến");
                return;
            }
            if (!bc.ChoPhepSua)
            {
                ThongBaoDialog.ThongTin(bc.LyDoKhongChoSua, "Không sửa được đoàn tàu");
                return;
            }

            var dauVao = TaoDauVaoLapTau(bc);
            var dlg = new LapTauKeoThaDialog(dauVao) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var kq = dlg.KetQua;

            // Nap lai danh sach chuyen + tab 3 tu CSDL (thay doi da ghi)
            ChayAnToan(() =>
            {
                NapChiSoTongQuan();
                NapLaiDanhSachChuyenTauGiuChon(ma);
                HienThiChuyenTauLapTau();
                NapDanhSachDauMay();
            }, "nạp lại đoàn tàu sau khi lưu");

            if (kq.KhongThayDoi)
            {
                ThongBaoDialog.ThongTin("Phương án không khác đoàn tàu đang lưu, không có gì để ghi.", "Không có thay đổi");
                return;
            }

            string moTa = $"{(kq.LaLapMoi ? "Đã lập" : "Đã lưu")} đoàn tàu {bc.SoHieuMacTau} gồm {kq.SoToaTrongPhuongAn} toa" +
                          (kq.MoTaCatNoi.Length > 0 ? $", có cắt / nối dọc đường ({kq.MoTaCatNoi})" : "") + ".\n" +
                          $"Lớn nhất {kq.TongSoToa} toa · dài {kq.TongChieuDaiM:N1} m · nặng {kq.TongTrongLuongTan:N1} t.";
            if (kq.DaHuyDuyetAnToan)
                moTa += "\n\nThành phần đoàn tàu đã đổi nên cờ \"đã duyệt an toàn\" được bỏ: cần duyệt lại trước khi xuất ga.";
            ThongBaoDialog.ThanhCong(moTa, kq.LaLapMoi ? "Lập đoàn tàu" : "Lưu đoàn tàu");
        }

        private ThongTinLapTau TaoDauVaoLapTau(BoiCanhLapTauDTO bc)
        {
            // Hanh trinh: can it nhat 1 ga trung gian (>= 3 ga dung) moi cat / noi duoc
            var hanhTrinh = bc.HanhTrinh.Count >= 3 ? DiemDungLapTau.TaoHanhTrinh(bc.HanhTrinh) : new List<DiemDungLapTau>();
            string ghiChu = bc.HanhTrinh.Count == 0
                ? "Chuyến chưa có lịch dừng ga: đoàn tàu chạy suốt, không cắt / nối toa dọc đường."
                : bc.HanhTrinh.Count < 3
                    ? "Chuyến không có ga dừng trung gian: đoàn tàu chạy suốt, không cắt / nối toa dọc đường."
                    : "";

            // Doan tau dang luu (toa that + pham vi)
            var doanTau = new List<ToaLapTau>();
            var maTrongDoan = new HashSet<int>();
            foreach (var t in bc.DoanHienTai)
            {
                var toaDto = bc.TimToa(t.MaToaXe);
                if (toaDto == null) continue;

                var toa = ToaLapTau.TuToaXe(toaDto);
                if (t.MaGaNoi != null || t.MaGaCat != null)
                {
                    string Ma(int? maGa) => maGa == null ? "" : bc.HanhTrinh.FirstOrDefault(d => d.MaGa == maGa)?.MaGaCode ?? "";
                    toa.DatPhamVi(t.MaGaNoi, Ma(t.MaGaNoi), t.MaGaCat, Ma(t.MaGaCat));
                }
                doanTau.Add(toa);
                maTrongDoan.Add(t.MaToaXe);
            }

            // Bai toa: khong bao duong, khong thuoc doan tau cua chuyen khac chong khung gio
            var baiToa = bc.ToaXe
                .Where(t => t.TrangThai != "BAO_DUONG" && t.ChuyenKhac.Count == 0 && !maTrongDoan.Contains(t.MaToaXe))
                .Select(ToaLapTau.TuToaXe)
                .ToList();

            return new ThongTinLapTau
            {
                BoiCanh = bc,
                HanhTrinh = hanhTrinh,
                GhiChuHanhTrinh = ghiChu,
                DanhSachDauMay = bc.DauMay.Select(d => DauMayLapTau.TuDTO(d, bc)).ToList(),
                DoanTauBanDau = doanTau,
                BaiToa = baiToa,
                Luu = pa => _lapTauService.LuuPhuongAn(pa, out string loi, out var kq) ? (kq, null) : (null, loi)
            };
        }

        // =====================================================================
        // TAB 4: SO DO CHO NGOI (toa ban ve vantai.ToaXeKhach - phan he Ban ve)
        // =====================================================================

        private void CboChuyenTauSoDo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;

            int maChuyen = int.TryParse(LayTagDangChon(cboChuyenTauSoDo), out int v) ? v : 0;
            if (maChuyen <= 0) return;

            ChayAnToan(() => NapDanhSachToaChoSoDo(maChuyen), "nạp danh sách toa xe của chuyến");
        }

        private void NapDanhSachToaChoSoDo(int maChuyen)
        {
            DataTable dt = _service.LayBienCheToaXe(maChuyen);

            _dangNapDuLieu = true;
            cboToaSoDo.Items.Clear();
            foreach (DataRow r in dt.Rows)
            {
                cboToaSoDo.Items.Add(new ComboBoxItem
                {
                    Content = $"Toa {r["ThuTuToa"]} · {r["NhanHieuToa"]} ({r["SucChua"]} chỗ)",
                    Tag = r["MaToaXeKhach"].ToString()
                });
            }
            _dangNapDuLieu = false;

            if (cboToaSoDo.Items.Count > 0)
            {
                cboToaSoDo.SelectedIndex = 0;
            }
            else
            {
                icSoDoGhe.ItemsSource = null;
                dgHanhKhachTrenToa.ItemsSource = null;
                txtTrongSoDo.Visibility = Visibility.Visible;
                txtTrongSoDo.Text = "Chuyến tàu này chưa biên chế toa xe khách nào.";
                txtTenToaThongKe.Text = "Chưa có toa";
                DatSoLieuSoDo(0, 0);
                txtTieuDeSoDo.Text = "SƠ ĐỒ MẶT BẰNG CHỖ NGỒI";
            }
        }

        private void CboToaSoDo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            ChayAnToan(NapSoDoGhe, "nạp sơ đồ chỗ ngồi");
        }

        private void NapSoDoGhe()
        {
            int maChuyen = int.TryParse(LayTagDangChon(cboChuyenTauSoDo), out int c) ? c : 0;
            int maToa = int.TryParse(LayTagDangChon(cboToaSoDo), out int t) ? t : 0;

            if (maChuyen <= 0 || maToa <= 0) return;

            DataTable dtGhe = _service.LaySoDoGhe(maToa, maChuyen);
            DataTable dtKhach = _service.LayHanhKhachTrenToa(maToa, maChuyen);

            var danhSachGhe = dtGhe.Rows.Cast<DataRow>().Select(GheHienThi.TuDongDuLieu).ToList();
            var danhSachKhach = dtKhach.Rows.Cast<DataRow>().Select(HanhKhachTrenToaHienThi.TuDongDuLieu).ToList();

            icSoDoGhe.ItemsSource = danhSachGhe;
            dgHanhKhachTrenToa.ItemsSource = danhSachKhach;

            txtTrongSoDo.Visibility = danhSachGhe.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            if (danhSachGhe.Count == 0)
                txtTrongSoDo.Text = "Toa này chưa thiết lập chỗ ngồi trong cơ sở dữ liệu.";

            dgHanhKhachTrenToa.Visibility = danhSachKhach.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            txtTrongHanhKhach.Visibility = danhSachKhach.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

            if (cboToaSoDo.SelectedItem is ComboBoxItem mucToa)
                txtTenToaThongKe.Text = mucToa.Content?.ToString() ?? "—";

            if (danhSachGhe.Count > 0)
            {
                string loaiToa = danhSachGhe[0].LoaiToa;
                txtTieuDeSoDo.Text = $"SƠ ĐỒ MẶT BẰNG — {MoTaLoaiToa(loaiToa)}";
            }

            int gheDaChiem = danhSachGhe.Count(g => g.DaBan);
            int gheNhieuChang = danhSachGhe.Count(g => g.BanNhieuChang);
            DatSoLieuSoDo(danhSachGhe.Count, gheDaChiem, danhSachKhach.Count, gheNhieuChang);
        }

        // tongGhe   : tong so cho trong toa
        // gheDaChiem: so cho da co it nhat mot ve
        // tongVe    : tong so ve (co the lon hon gheDaChiem do ban nhieu chang tren cung mot cho)
        private void DatSoLieuSoDo(int tongGhe, int gheDaChiem, int tongVe = 0, int gheNhieuChang = 0)
        {
            int conTrong = tongGhe - gheDaChiem;

            txtSoDoTongGhe.Text = tongGhe.ToString("N0");
            txtSoDoDaBan.Text = gheDaChiem.ToString("N0");
            txtSoDoConTrong.Text = conTrong.ToString("N0");

            double tyLe = tongGhe > 0 ? (double)gheDaChiem / tongGhe * 100.0 : 0.0;
            txtHeSoSuDung.Text = $"{tyLe:N1}%";
            txtHeSoSuDungNhan.Text = tongGhe > 0
                ? $"{gheDaChiem}/{tongGhe} chỗ · {tongVe} vé"
                : "Chưa có dữ liệu";

            // Ghi chu mo hinh ban ve theo chang
            if (gheNhieuChang > 0)
            {
                bdGhiChuNhieuChang.Visibility = Visibility.Visible;
                txtGhiChuNhieuChang.Visibility = Visibility.Visible;
                txtGhiChuNhieuChang.Text =
                    $"Có {gheNhieuChang} chỗ được bán cho nhiều hành khách trên các chặng không giao nhau " +
                    "(ô màu cam trên sơ đồ).";
            }
            else
            {
                bdGhiChuNhieuChang.Visibility = Visibility.Collapsed;
            }

            string mau = tyLe switch
            {
                >= 90 => "#B91C1C",
                >= 70 => "#B45309",
                >= 40 => "#1D4ED8",
                _ => "#15803D"
            };
            bdHeSoSuDungVach.Background = (Brush)new BrushConverter().ConvertFrom(mau)!;
            DatTyLeVach(colHeSoDaBan, colHeSoConTrong, tyLe);
        }

        private static string MoTaLoaiToa(string loaiToa) => loaiToa switch
        {
            "NC" => "TOA NGỒI CỨNG",
            "NML" => "TOA NGỒI MỀM ĐIỀU HÒA",
            "BN" => "TOA GIƯỜNG NẰM KHOANG 6",
            "AN" => "TOA GIƯỜNG NẰM KHOANG 4",
            _ => "TOA XE KHÁCH"
        };

        // =====================================================================
        // TIEN ICH DUNG CHUNG
        // =====================================================================

        // Dat ty le cho cap cot star-sizing dung lam thanh tien do
        private static void DatTyLeVach(ColumnDefinition cotDaChay, ColumnDefinition cotConLai, double phanTram)
        {
            if (phanTram < 0) phanTram = 0;
            if (phanTram > 100) phanTram = 100;

            cotDaChay.Width = new GridLength(phanTram, GridUnitType.Star);
            cotConLai.Width = new GridLength(100 - phanTram, GridUnitType.Star);
        }

        // Chon va cuon toi dong thoa dieu kien; tra ve false neu dong khong co trong danh sach
        private static bool ChonDongTheoMa<T>(DataGrid dg, Func<T, bool> dieuKien)
        {
            if (dg.ItemsSource is not IEnumerable<T> ds) return false;

            var muc = ds.FirstOrDefault(dieuKien);
            if (muc == null) return false;

            dg.SelectedItem = muc;
            dg.ScrollIntoView(muc);
            return true;
        }

        private static string? LayTagDangChon(ComboBox cbo)
            => (cbo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

        private static void ChonLaiTheoTag(ComboBox cbo, string tag)
        {
            foreach (object muc in cbo.Items)
            {
                if (muc is ComboBoxItem cbi && cbi.Tag?.ToString() == tag)
                {
                    cbo.SelectedItem = cbi;
                    return;
                }
            }
            if (cbo.Items.Count > 0) cbo.SelectedIndex = 0;
        }

        // =====================================================================
        // CAC HAM MainWindow GOI QUA (dong bo voi cac Page khac)
        // =====================================================================

        public void FocusTimKiem()
        {
            tabPhuongTien.SelectedIndex = 0;
            txtTimDauMay.Focus();
            txtTimDauMay.SelectAll();
        }

        public void KichHoatThemMoi()
        {
            switch (tabPhuongTien.SelectedIndex)
            {
                case 0: ThemDauMay(); break;
                case 1: ThemToaXe(); break;
                case 2: MoLapTauKeoTha(); break;
            }
        }

        public void KichHoatNapLai() => NapToanBoDuLieu();

        public void KichHoatHuy() => BtnXoaLocDauMay_Click(this, new RoutedEventArgs());
    }
}
