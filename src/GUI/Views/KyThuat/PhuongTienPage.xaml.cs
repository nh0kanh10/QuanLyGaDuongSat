using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BUS.Services;
using GUI.Views.Dialogs;
using GUI.Views.KyThuat.Dialogs;
using GUI.Views.KyThuat.Helpers;
using GUI.Views.KyThuat.Models;
// Chi lay rieng SymbolRegular de tranh trung ten TextBlock / ComboBox
// giua Wpf.Ui.Controls va System.Windows.Controls.
using SymbolRegular = Wpf.Ui.Controls.SymbolRegular;

namespace GUI.Views.KyThuat
{
    // =========================================================================
    // PHAN HE QUAN LY PHUONG TIEN (Doan tau, Dau may, Toa xe & Ghe)
    //
    // Lop nay chi lam nhiem vu GIAO DIEN: nap du lieu qua PhuongTienService,
    // chuyen thanh cac lop hien thi trong namespace Models roi gan vao control.
    // Moi quy tac nghiep vu nam o tang BUS.
    //
    // Giai doan dung giao dien: them / sua / lap tau keo-tha chua ghi CSDL.
    // Cac thay doi duoc giu trong bo nho ("thay doi tam") va tron vao du lieu
    // doc tu CSDL moi lan nap lai danh sach.
    // =========================================================================
    public partial class PhuongTienPage : Page
    {
        private readonly PhuongTienService _service = new();

        private DataTable _bangDauMay = new();
        private DataTable _bangChuyenTau = new();

        private bool _dangNapDuLieu;
        private bool _coKetNoiCsdl;

        // --- Thay doi tam tren giao dien (chua ghi CSDL) ---
        // Khoa am = ban ghi them moi, khoa duong = ban ghi CSDL da bi sua
        private readonly Dictionary<int, DauMayHienThi> _dauMayTam = new();
        private readonly Dictionary<int, string> _trangThaiGocDauMay = new();   // de dieu chinh chi so "san sang"
        private readonly Dictionary<int, ToaXeHienThi> _toaXeTam = new();
        private readonly Dictionary<int, KetQuaLapTau> _phuongAnLapTau = new(); // khoa: MaChuyenTau (0 = chua chon chuyen)
        private int _maTamKeTiep = -1;

        private readonly Dictionary<int, string> _maChungLoaiSangCode = new();

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

        // Doc CSDL an toan: mat ket noi thi tra ve bang rong de giao dien van dung duoc
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
                bdCanhBaoKetNoi.Visibility = Visibility.Visible;
                txtCanhBaoKetNoi.Text =
                    "Không kết nối được máy chủ SQL Server. Đã thử các instance: .\\SQLEXPRESS, . , localhost, (localdb)\\MSSQLLocalDB. " +
                    "Màn hình vẫn dùng được với dữ liệu mẫu và thay đổi tạm.";
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

            CapNhatNhanThayDoiTam();
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

            // Cong them phan thay doi tam de chi so khop voi danh sach dang hien thi
            foreach (var (ma, dm) in _dauMayTam)
            {
                bool sanSang = dm.TrangThai == "SAN_SANG";
                if (ma < 0)
                {
                    tongDauMay++;
                    if (sanSang) dauMaySanSang++;
                }
                else if (_trangThaiGocDauMay.TryGetValue(ma, out string? goc))
                {
                    dauMaySanSang += (sanSang ? 1 : 0) - (goc == "SAN_SANG" ? 1 : 0);
                }
            }
            tongToaXe += _toaXeTam.Keys.Count(k => k < 0);

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

            // Xi nghiep moi chi co trong thay doi tam
            foreach (var dm in _dauMayTam.Values)
                DamBaoCoXiNghiepTrongBoLoc(dm.DonViQuanLy);

            cboDepot.SelectedIndex = 0;
        }

        private void DamBaoCoXiNghiepTrongBoLoc(string ten)
        {
            if (string.IsNullOrWhiteSpace(ten)) return;

            if (cboDepot.Items.Count == 0)
                cboDepot.Items.Add(new ComboBoxItem { Content = "Tất cả xí nghiệp", Tag = "ALL", IsSelected = true });

            foreach (object muc in cboDepot.Items)
            {
                if (muc is ComboBoxItem cbi && cbi.Tag?.ToString() == ten) return;
            }
            cboDepot.Items.Add(new ComboBoxItem { Content = ten, Tag = ten });
        }

        private void NapDanhSachDauMay()
        {
            string tuKhoa = txtTimDauMay.Text?.Trim() ?? string.Empty;
            string depot = LayTagDangChon(cboDepot) ?? "ALL";
            string trangThai = LayTagDangChon(cboTrangThaiDauMay) ?? "ALL";

            _bangDauMay = LayBang(() => _service.LayDanhSachDauMay(tuKhoa, depot, trangThai));

            var danhSach = new List<DauMayHienThi>();
            var maDaCo = new HashSet<int>();

            foreach (DataRow r in _bangDauMay.Rows)
            {
                var dm = DauMayHienThi.TuDongDuLieu(r);
                maDaCo.Add(dm.MaDauMay);

                if (_dauMayTam.TryGetValue(dm.MaDauMay, out var banTam)) dm = banTam;
                if (KhopBoLocDauMay(dm, tuKhoa, depot, trangThai)) danhSach.Add(dm);
            }

            // Ban ghi tam khong co trong ket qua CSDL: them moi, hoac da sua sang gia tri khop bo loc
            foreach (var dm in _dauMayTam.Values)
            {
                if (!maDaCo.Contains(dm.MaDauMay) && KhopBoLocDauMay(dm, tuKhoa, depot, trangThai))
                    danhSach.Add(dm);
            }

            dgDauMay.ItemsSource = danhSach;
            txtBadgeDauMay.Text = danhSach.Count.ToString();
        }

        // Cung dieu kien voi PhuongTienRepository.LayDanhSachDauMay
        private static bool KhopBoLocDauMay(DauMayHienThi dm, string tuKhoa, string depot, string trangThai)
        {
            if (tuKhoa.Length > 0 &&
                !dm.SoHieuDauMay.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !dm.MaDongCode.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !dm.DonViQuanLy.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !dm.NhaSanXuat.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
                return false;

            if (depot != "ALL" && dm.DonViQuanLy != depot) return false;
            if (trangThai != "ALL" && dm.TrangThai != trangThai) return false;
            return true;
        }

        // Toan bo dau may (khong loc) da tron thay doi tam
        private List<DauMayHienThi> LayToanBoDauMay()
        {
            DataTable dt = LayBang(() => _service.LayDanhSachDauMay(null, "ALL", "ALL"));

            var ds = new List<DauMayHienThi>();
            var maDaCo = new HashSet<int>();
            foreach (DataRow r in dt.Rows)
            {
                var dm = DauMayHienThi.TuDongDuLieu(r);
                maDaCo.Add(dm.MaDauMay);
                ds.Add(_dauMayTam.TryGetValue(dm.MaDauMay, out var banTam) ? banTam : dm);
            }
            ds.AddRange(_dauMayTam.Values.Where(dm => !maDaCo.Contains(dm.MaDauMay)));
            return ds;
        }

        private void DgDauMay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool coChon = dgDauMay.SelectedItem is DauMayHienThi;
            btnSuaDauMay.IsEnabled = coChon;
            btnSuaChiTietDauMay.IsEnabled = coChon;

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
            txtChiTietMaDauMay.Text = dm.MaDauMay > 0 ? $"ID: #{dm.MaDauMay}" : "MỚI · chưa ghi CSDL";

            // --- Tien do bao duong ---
            var (cap, moc, tyLe, mucCanhBao) = PhuongTienService.TinhTienDoBaoDuong(dm.SoKmTichLuy);

            txtBaoDuongCap.Text = $"Cấp kế tiếp: {cap} tại mốc {moc:N0} km";
            txtBaoDuongTyLe.Text = $"{tyLe:N1}%";

            var mauVach = (SolidColorBrush)new BrushConverter().ConvertFrom(DauMayHienThi.LayMauTheoMucCanhBao(mucCanhBao))!;
            bdBaoDuongVach.Background = mauVach;
            DatTyLeVach(colBaoDuongDaChay, colBaoDuongConLai, (double)tyLe);

            decimal conLai = moc - dm.SoKmTichLuy;
            txtBaoDuongChiTiet.Text = mucCanhBao switch
            {
                "QUA_HAN" => "Đã vượt mốc đại tu Ro (300.000 km). Cần đưa vào xưởng kiểm tra tổng thể.",
                "KHAN_CAP" => $"Khẩn cấp: chỉ còn {conLai:N0} km là tới mốc bảo dưỡng {cap}. Cần lên kế hoạch đưa vào xưởng ngay.",
                "CANH_BAO" => $"Cảnh báo: còn {conLai:N0} km là tới mốc bảo dưỡng {cap}. Nên chuẩn bị kế hoạch.",
                _ => $"Bình thường: còn {conLai:N0} km nữa mới tới mốc bảo dưỡng {cap}."
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

            // --- Lich su van dung (dau may moi them thi chua co) ---
            DataTable ls = dm.MaDauMay > 0 ? LayBang(() => _service.LayLichSuVanDung(dm.MaDauMay)) : new DataTable();
            var danhSachLs = new List<LichSuVanDungHienThi>();
            foreach (DataRow r in ls.Rows)
                danhSachLs.Add(LichSuVanDungHienThi.TuDongDuLieu(r));

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

        // --- Them / sua dau may ---

        private void BtnThemDauMay_Click(object sender, RoutedEventArgs e) => ThemDauMay();

        private void BtnSuaDauMay_Click(object sender, RoutedEventArgs e) => SuaDauMay();

        private void DongDauMay_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SuaDauMay();

        private void ThemDauMay()
        {
            tabPhuongTien.SelectedIndex = 0;

            var dlg = new DauMayDialog(null, LayToanBoDauMay().Select(d => d.SoHieuDauMay), LayDanhSachXiNghiep())
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var dm = dlg.KetQua;
            dm.MaDauMay = _maTamKeTiep--;
            _dauMayTam[dm.MaDauMay] = dm;

            LamMoiSauKhiDoiDauMay(dm);
        }

        private void SuaDauMay()
        {
            if (dgDauMay.SelectedItem is not DauMayHienThi dangChon) return;

            var dlg = new DauMayDialog(dangChon.SaoChep(), Array.Empty<string>(), LayDanhSachXiNghiep())
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var dm = dlg.KetQua;

            // Ghi nho trang thai goc trong CSDL o lan sua dau tien (de tinh lai chi so "san sang")
            if (dm.MaDauMay > 0 && !_dauMayTam.ContainsKey(dm.MaDauMay))
                _trangThaiGocDauMay[dm.MaDauMay] = dangChon.TrangThai;

            _dauMayTam[dm.MaDauMay] = dm;
            LamMoiSauKhiDoiDauMay(dm);
        }

        private void LamMoiSauKhiDoiDauMay(DauMayHienThi dm)
        {
            DamBaoCoXiNghiepTrongBoLoc(dm.DonViQuanLy);

            ChayAnToan(() =>
            {
                NapChiSoTongQuan();
                NapDanhSachDauMay();

                // Bi bo loc hien tai an mat thi xoa loc de nguoi dung thay ngay ban ghi vua luu
                if (!ChonDongTheoMa(dgDauMay, (DauMayHienThi d) => d.MaDauMay == dm.MaDauMay))
                {
                    BtnXoaLocDauMay_Click(this, new RoutedEventArgs());
                    ChonDongTheoMa(dgDauMay, (DauMayHienThi d) => d.MaDauMay == dm.MaDauMay);
                }
            }, "cập nhật danh sách đầu máy");

            CapNhatNhanThayDoiTam();
        }

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
            _maChungLoaiSangCode.Clear();
            cboChungLoaiToa.Items.Add(new ComboBoxItem { Content = "Tất cả chủng loại", Tag = "0", IsSelected = true });

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    int ma = Convert.ToInt32(r["MaChungLoai"]);
                    string code = r["MaChungLoaiCode"].ToString() ?? "";
                    _maChungLoaiSangCode[ma] = code;
                    cboChungLoaiToa.Items.Add(new ComboBoxItem { Content = $"{code} — {r["TenMoTa"]}", Tag = ma.ToString() });
                }
            }
            else
            {
                // Khong co CSDL: dung danh muc mau de bo loc van hoat dong voi toa them tam
                foreach (var cl in DanhMucMauPhuongTien.ChungLoaiToa)
                {
                    _maChungLoaiSangCode[cl.MaChungLoai] = cl.MaChungLoaiCode;
                    cboChungLoaiToa.Items.Add(new ComboBoxItem { Content = cl.TenHienThi, Tag = cl.MaChungLoai.ToString() });
                }
            }

            cboChungLoaiToa.SelectedIndex = 0;
        }

        private void NapDanhSachToaXeDoi()
        {
            string tuKhoa = txtTimToaXe.Text?.Trim() ?? string.Empty;
            int maChungLoai = int.TryParse(LayTagDangChon(cboChungLoaiToa), out int cl) ? cl : 0;
            string? codeLoc = maChungLoai > 0 && _maChungLoaiSangCode.TryGetValue(maChungLoai, out string? c) ? c : null;

            DataTable dt = LayBang(() => _service.LayDanhSachToaXeDoi(tuKhoa, maChungLoai));

            var danhSach = new List<ToaXeHienThi>();
            var maDaCo = new HashSet<int>();

            foreach (DataRow r in dt.Rows)
            {
                var tx = ToaXeHienThi.TuDongDuLieu(r);
                maDaCo.Add(tx.MaToaXe);

                if (_toaXeTam.TryGetValue(tx.MaToaXe, out var banTam)) tx = banTam;
                if (KhopBoLocToaXe(tx, tuKhoa, codeLoc)) danhSach.Add(tx);
            }

            foreach (var tx in _toaXeTam.Values)
            {
                if (!maDaCo.Contains(tx.MaToaXe) && KhopBoLocToaXe(tx, tuKhoa, codeLoc))
                    danhSach.Add(tx);
            }

            dgToaXeDoi.ItemsSource = danhSach;
            txtBadgeToaXe.Text = danhSach.Count.ToString();
        }

        // Cung dieu kien voi PhuongTienRepository.LayDanhSachToaXeDoi
        private static bool KhopBoLocToaXe(ToaXeHienThi tx, string tuKhoa, string? codeLoc)
        {
            if (tuKhoa.Length > 0 &&
                !tx.SoHieuToaXe.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !tx.MaChungLoaiCode.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase) &&
                !tx.TenMoTa.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
                return false;

            return codeLoc == null || string.Equals(tx.MaChungLoaiCode, codeLoc, StringComparison.OrdinalIgnoreCase);
        }

        private List<ToaXeHienThi> LayToanBoToaXe()
        {
            DataTable dt = LayBang(() => _service.LayDanhSachToaXeDoi(null, 0));

            var ds = new List<ToaXeHienThi>();
            var maDaCo = new HashSet<int>();
            foreach (DataRow r in dt.Rows)
            {
                var tx = ToaXeHienThi.TuDongDuLieu(r);
                maDaCo.Add(tx.MaToaXe);
                ds.Add(_toaXeTam.TryGetValue(tx.MaToaXe, out var banTam) ? banTam : tx);
            }
            ds.AddRange(_toaXeTam.Values.Where(tx => !maDaCo.Contains(tx.MaToaXe)));
            return ds;
        }

        private void NapDanhSachChungLoaiToa()
        {
            DataTable dt = LayBang(_service.LayDanhSachChungLoaiToa);

            var ds = new List<ChungLoaiToaThongKe>();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    ds.Add(new ChungLoaiToaThongKe(
                        r["MaChungLoaiCode"].ToString() ?? "",
                        r["TenMoTa"].ToString() ?? "",
                        Convert.ToDecimal(r["ChieuDaiChuanM"]),
                        Convert.ToInt32(r["SoTruc"]),
                        Convert.ToInt32(r["SoToaTrongDoi"])));
                }
            }
            else
            {
                ds.AddRange(DanhMucMauPhuongTien.ChungLoaiToa.Select(cl =>
                    new ChungLoaiToaThongKe(cl.MaChungLoaiCode, cl.TenMoTa, cl.ChieuDaiChuanM, cl.SoTruc, 0)));
            }

            // Cong toa them tam vao dung chung loai
            var themTheoLoai = _toaXeTam.Where(kv => kv.Key < 0)
                                        .GroupBy(kv => kv.Value.MaChungLoaiCode)
                                        .ToDictionary(g => g.Key, g => g.Count());
            for (int i = 0; i < ds.Count; i++)
            {
                if (themTheoLoai.TryGetValue(ds[i].MaChungLoaiCode, out int soThem))
                    ds[i] = ds[i] with { SoToaTrongDoi = ds[i].SoToaTrongDoi + soThem };
            }

            icChungLoaiToa.ItemsSource = ds;
        }

        private void DgToaXeDoi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnSuaToaXe.IsEnabled = dgToaXeDoi.SelectedItem is ToaXeHienThi;
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
            ChayAnToan(() => { NapDanhSachToaXeDoi(); NapDanhSachChungLoaiToa(); }, "nạp lại đội toa xe");
        }

        // --- Them / sua toa xe ---

        private void BtnThemToaXe_Click(object sender, RoutedEventArgs e) => ThemToaXe();

        private void BtnSuaToaXe_Click(object sender, RoutedEventArgs e) => SuaToaXe();

        private void DongToaXe_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SuaToaXe();

        private void ThemToaXe()
        {
            tabPhuongTien.SelectedIndex = 1;

            var dlg = new ToaXeDialog(null, LayToanBoToaXe().Select(t => t.SoHieuToaXe))
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var tx = dlg.KetQua;
            tx.MaToaXe = _maTamKeTiep--;
            _toaXeTam[tx.MaToaXe] = tx;

            LamMoiSauKhiDoiToaXe(tx);
        }

        private void SuaToaXe()
        {
            if (dgToaXeDoi.SelectedItem is not ToaXeHienThi dangChon) return;

            var dlg = new ToaXeDialog(dangChon.SaoChep(), Array.Empty<string>())
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var tx = dlg.KetQua;
            _toaXeTam[tx.MaToaXe] = tx;
            LamMoiSauKhiDoiToaXe(tx);
        }

        private void LamMoiSauKhiDoiToaXe(ToaXeHienThi tx)
        {
            ChayAnToan(() =>
            {
                NapChiSoTongQuan();
                NapDanhSachToaXeDoi();
                NapDanhSachChungLoaiToa();

                if (!ChonDongTheoMa(dgToaXeDoi, (ToaXeHienThi t) => t.MaToaXe == tx.MaToaXe))
                {
                    _dangNapDuLieu = true;
                    txtTimToaXe.Text = string.Empty;
                    cboChungLoaiToa.SelectedIndex = 0;
                    _dangNapDuLieu = false;

                    NapDanhSachToaXeDoi();
                    ChonDongTheoMa(dgToaXeDoi, (ToaXeHienThi t) => t.MaToaXe == tx.MaToaXe);
                }
            }, "cập nhật danh sách toa xe");

            CapNhatNhanThayDoiTam();
        }

        // =====================================================================
        // TAB 3: LAP DOAN TAU
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

        private DataRow? TimDongChuyenTau(int maChuyenTau)
        {
            foreach (DataRow r in _bangChuyenTau.Rows)
            {
                if (Convert.ToInt32(r["MaChuyenTau"]) == maChuyenTau) return r;
            }
            return null;
        }

        private int LayMaChuyenTauLapTau()
            => int.TryParse(LayTagDangChon(cboChuyenTauLapTau), out int v) ? v : 0;

        private void CboChuyenTauLapTau_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            HienThiChuyenTauLapTau();
        }

        private void HienThiChuyenTauLapTau()
        {
            int ma = LayMaChuyenTauLapTau();
            DataRow? dong = ma > 0 ? TimDongChuyenTau(ma) : null;

            ChayAnToan(() =>
            {
                if (dong != null)
                {
                    HienThiThongSoDoanTau(dong);
                    NapBienCheToa(ma);
                    ChayKiemTraAnToan(dong);
                }

                // Co phuong an keo-tha cho chuyen nay thi hien de len du lieu CSDL
                if (_phuongAnLapTau.TryGetValue(ma, out var phuongAn))
                    ApDungPhuongAnLapTau(phuongAn);
                else
                    bdPhuongAnMoPhong.Visibility = Visibility.Collapsed;
            }, "nạp thông tin lập đoàn tàu");
        }

        private void HienThiThongSoDoanTau(DataRow r)
        {
            var gioDi = Convert.ToDateTime(r["GioXuatPhatKH"]);
            var gioDen = Convert.ToDateTime(r["GioVeDichKH"]);
            txtTomTatHanhTrinh.Text =
                $"{r["LoaiTau"]} · Khởi hành {gioDi:HH:mm dd/MM} · Về đích {gioDen:HH:mm dd/MM} · Trạng thái: {r["TrangThai"]}";

            bool coDoanTau = r["MaDoanTau"] != DBNull.Value;

            if (!coDoanTau)
            {
                txtDauMayChinh.Text = "—";
                txtDauMayChinhPhu.Text = "Chuyến này chưa lập đoàn tàu";
                txtDauMayDay.Text = "—";
                txtDauMayDayPhu.Text = "Không biên chế máy đẩy";
                txtTongChieuDai.Text = "—";
                txtTongSoToa.Text = string.Empty;
                txtChieuDaiPhu.Text = "Chưa có dữ liệu";
                txtTongTrongLuong.Text = "—";
                txtTrongLuongPhu.Text = "Chưa có dữ liệu";
                return;
            }

            // Dau may keo chinh
            if (r["SoHieuDauMayChinh"] != DBNull.Value)
            {
                txtDauMayChinh.Text = r["SoHieuDauMayChinh"].ToString();
                txtDauMayChinhPhu.Text = $"Dòng {r["DongDauMayChinh"]} · Sức kéo {LayDecimal(r, "SucKeoChinhTan"):N0} tấn";
            }
            else
            {
                txtDauMayChinh.Text = "—";
                txtDauMayChinhPhu.Text = "Chưa chỉ định đầu máy kéo";
            }

            // Dau may day
            if (r["SoHieuDauMayDay"] != DBNull.Value)
            {
                txtDauMayDay.Text = r["SoHieuDauMayDay"].ToString();
                txtDauMayDayPhu.Text = $"Dòng {r["DongDauMayDay"]} · Hỗ trợ vượt đèo dốc";
            }
            else
            {
                txtDauMayDay.Text = "—";
                txtDauMayDayPhu.Text = "Không biên chế máy đẩy";
            }

            // Chieu dai
            decimal tongDai = LayDecimal(r, "TongChieuDaiM");
            int tongToa = LayInt(r, "TongSoToa");
            int soToaThucTe = LayInt(r, "SoToaThucTe");

            txtTongChieuDai.Text = $"{tongDai:N1} m";
            txtTongSoToa.Text = $"· {tongToa} toa";
            txtChieuDaiPhu.Text = soToaThucTe > 0
                ? $"Đã biên chế {soToaThucTe} toa khách trong hệ thống"
                : "Chưa biên chế toa khách nào";

            // Trong luong
            decimal tongTrongLuong = LayDecimal(r, "TongTrongLuongTan");
            txtTongTrongLuong.Text = $"{tongTrongLuong:N1} tấn";

            decimal? sucKeo = r["SucKeoChinhTan"] != DBNull.Value ? Convert.ToDecimal(r["SucKeoChinhTan"]) : null;
            var (_, tyLeSucKeo, thongDiepSucKeo) = PhuongTienService.DoiChieuSucKeo(tongTrongLuong, sucKeo);
            txtTrongLuongPhu.Text = tyLeSucKeo > 0 ? $"Chiếm {tyLeSucKeo:N1}% sức kéo định mức" : thongDiepSucKeo;
        }

        private void NapBienCheToa(int maChuyenTau)
        {
            DataTable dt = LayBang(() => _service.LayBienCheToaXe(maChuyenTau));

            var danhSach = new List<ToaBienCheHienThi>();
            foreach (DataRow r in dt.Rows)
                danhSach.Add(ToaBienCheHienThi.TuDongDuLieu(r));

            dgBienCheToa.ItemsSource = danhSach;
        }

        private void ChayKiemTraAnToan(DataRow r)
        {
            var hangMuc = new List<HangMucAnToanHienThi>();
            bool coDoanTau = r["MaDoanTau"] != DBNull.Value;

            if (!coDoanTau)
            {
                hangMuc.Add(HangMucAnToanHienThi.Loi(
                    "Lập đoàn tàu",
                    "Chuyến tàu này chưa được lập đoàn. Cần chỉ định đầu máy và biên chế toa xe trước khi kiểm tra an toàn."));

                CapNhatKetLuanAnToan(false, "CHƯA LẬP ĐOÀN TÀU", "Chuyến chưa có hồ sơ đoàn tàu trong hệ thống");
                icHangMucAnToan.ItemsSource = hangMuc;
                return;
            }

            decimal tongDai = LayDecimal(r, "TongChieuDaiM");
            decimal tongTrongLuong = LayDecimal(r, "TongTrongLuongTan");
            int tongToa = LayInt(r, "TongSoToa");
            bool daDuyet = r["DaDuyetAnToan"] != DBNull.Value && Convert.ToBoolean(r["DaDuyetAnToan"]);

            // 1. Dau may keo chinh
            if (r["SoHieuDauMayChinh"] != DBNull.Value)
                hangMuc.Add(HangMucAnToanHienThi.Dat("Đầu máy kéo chính",
                    $"Đã chỉ định {r["SoHieuDauMayChinh"]} (dòng {r["DongDauMayChinh"]})."));
            else
                hangMuc.Add(HangMucAnToanHienThi.Loi("Đầu máy kéo chính",
                    "Chưa chỉ định đầu máy kéo chính cho đoàn tàu."));

            // 2. Chieu dai vs duong tranh
            decimal? duongTranh = r["DuongTranhNganNhatM"] != DBNull.Value
                ? Convert.ToDecimal(r["DuongTranhNganNhatM"])
                : null;

            var (datChieuDai, thongDiepChieuDai) = PhuongTienService.DoiChieuChieuDaiDuongTranh(tongDai, duongTranh);
            hangMuc.Add(datChieuDai
                ? HangMucAnToanHienThi.Dat("Chiều dài đoàn tàu vs đường tránh", $"Đoàn tàu dài {tongDai:N1} m. {thongDiepChieuDai}")
                : HangMucAnToanHienThi.Loi("Chiều dài đoàn tàu vs đường tránh", $"Đoàn tàu dài {tongDai:N1} m. {thongDiepChieuDai}"));

            // 3. Trong luong vs suc keo
            decimal? sucKeo = r["SucKeoChinhTan"] != DBNull.Value ? Convert.ToDecimal(r["SucKeoChinhTan"]) : null;
            var (datSucKeo, _, thongDiepSucKeo) = PhuongTienService.DoiChieuSucKeo(tongTrongLuong, sucKeo);
            hangMuc.Add(datSucKeo
                ? HangMucAnToanHienThi.Dat("Trọng lượng vs sức kéo đầu máy", $"Đoàn tàu nặng {tongTrongLuong:N1} tấn. {thongDiepSucKeo}")
                : HangMucAnToanHienThi.Loi("Trọng lượng vs sức kéo đầu máy", $"Đoàn tàu nặng {tongTrongLuong:N1} tấn. {thongDiepSucKeo}"));

            // 4. So toa trong nguong quy chuan
            if (tongToa is > 0 and <= 20)
                hangMuc.Add(HangMucAnToanHienThi.Dat("Số toa trong đoàn",
                    $"Biên chế {tongToa} toa, nằm trong ngưỡng quy chuẩn 1–20 toa."));
            else
                hangMuc.Add(HangMucAnToanHienThi.Loi("Số toa trong đoàn",
                    $"Biên chế {tongToa} toa, vượt ngoài ngưỡng quy chuẩn 1–20 toa."));

            // 5. Trang thai duyet an toan
            if (daDuyet)
                hangMuc.Add(HangMucAnToanHienThi.Dat("Xác nhận duyệt an toàn",
                    "Đoàn tàu đã được duyệt điều kiện an toàn kỹ thuật."));
            else
                hangMuc.Add(HangMucAnToanHienThi.CanhBao("Xác nhận duyệt an toàn",
                    "Đoàn tàu chưa có xác nhận duyệt an toàn của quản đốc lập tàu."));

            icHangMucAnToan.ItemsSource = hangMuc;

            int soLoi = hangMuc.Count(h => h.MucDo == "LOI");
            int soCanhBao = hangMuc.Count(h => h.MucDo == "CANH_BAO");

            if (soLoi > 0)
                CapNhatKetLuanAnToan(false, "KHÔNG ĐỦ ĐIỀU KIỆN", $"Có {soLoi} hạng mục không đạt, cần xử lý trước khi xuất ga");
            else if (soCanhBao > 0)
                CapNhatKetLuanAnToan(null, "ĐẠT CÓ LƯU Ý", $"Toàn bộ hạng mục kỹ thuật đạt, còn {soCanhBao} lưu ý thủ tục");
            else
                CapNhatKetLuanAnToan(true, "ĐỦ ĐIỀU KIỆN XUẤT GA", "Toàn bộ hạng mục đối chiếu đều đạt yêu cầu");
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

            _dangNapDuLieu = true;
            NapDanhSachChuyenTau();
            _dangNapDuLieu = false;

            ChonLaiTheoTag(cboChuyenTauLapTau, maDangChon.ToString());
        }

        // --- Lap tau keo - tha ---

        private void BtnLapTauKeoTha_Click(object sender, RoutedEventArgs e) => MoLapTauKeoTha();

        private void MoLapTauKeoTha()
        {
            tabPhuongTien.SelectedIndex = 2;

            int ma = LayMaChuyenTauLapTau();
            DataRow? dong = ma > 0 ? TimDongChuyenTau(ma) : null;
            _phuongAnLapTau.TryGetValue(ma, out var phuongAnCu);

            // Doan tau ban dau: phuong an da lap truoc do, hoac bien che dang hien thi cua chuyen
            List<ToaLapTau> doanTau = phuongAnCu != null
                ? phuongAnCu.DanhSachToa.ToList()
                : (dgBienCheToa.ItemsSource as IEnumerable<ToaBienCheHienThi>)?.Select(ToaLapTau.TuBienChe).ToList()
                  ?? new List<ToaLapTau>();

            // Bai toa: toa san sang trong doi + toa mau, bo cac so hieu da nam trong doan
            var soHieuDaDung = new HashSet<string>(doanTau.Select(t => t.SoHieu), StringComparer.OrdinalIgnoreCase);
            var baiToa = new List<ToaLapTau>();
            foreach (var tx in LayToanBoToaXe().Where(t => t.TrangThai == "SAN_SANG"))
            {
                if (soHieuDaDung.Add(tx.SoHieuToaXe)) baiToa.Add(ToaLapTau.TuToaXe(tx));
            }
            foreach (var (soHieu, maCode) in DanhMucMauPhuongTien.BaiToaMau)
            {
                if (soHieuDaDung.Add(soHieu)) baiToa.Add(ToaLapTau.TuMau(soHieu, maCode));
            }

            var dauVao = new ThongTinLapTau
            {
                MaChuyenTau = ma,
                TenChuyen = dong != null ? TaoNhanChuyenTau(dong) : "Chưa chọn chuyến tàu — lập phương án mô phỏng tự do",
                DuongTranhNganNhatM = dong != null && dong["DuongTranhNganNhatM"] != DBNull.Value
                    ? Convert.ToDecimal(dong["DuongTranhNganNhatM"])
                    : null,
                SoHieuDauMayDangChon = phuongAnCu?.DauMay?.SoHieu
                    ?? (dong != null && dong["SoHieuDauMayChinh"] != DBNull.Value ? dong["SoHieuDauMayChinh"].ToString() : null),
                DanhSachDauMay = LayDauMayChoLapTau(),
                DoanTauBanDau = doanTau,
                BaiToa = baiToa
            };

            var dlg = new LapTauKeoThaDialog(dauVao) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var kq = dlg.KetQua;
            _phuongAnLapTau[ma] = kq;
            ApDungPhuongAnLapTau(kq);
            CapNhatNhanThayDoiTam();

            ThongBaoDialog.ThanhCong(
                $"Đã áp dụng phương án lập tàu gồm {kq.DanhSachToa.Count} toa, dài {kq.TongChieuDaiM:N1} m, " +
                $"đầu máy {kq.DauMay?.SoHieu ?? "—"}.\n\n" +
                "Phương án đang hiển thị ở tab Lập Đoàn Tàu (chưa ghi vào CSDL).",
                "Lập tàu thành công");
        }

        private List<DauMayLapTau> LayDauMayChoLapTau()
        {
            var ds = LayToanBoDauMay().Select(DauMayLapTau.TuDauMay).ToList();
            if (ds.Count > 0) return ds;

            // Khong co CSDL: dung dau may mau trung seed data
            foreach (var (soHieu, maDong, donVi, trangThai) in DanhMucMauPhuongTien.DauMayMau)
            {
                var dong = DanhMucMauPhuongTien.TimDongDauMay(maDong);
                if (dong == null) continue;
                ds.Add(new DauMayLapTau
                {
                    SoHieu = soHieu,
                    MaDongCode = maDong,
                    DonViQuanLy = donVi,
                    TrangThai = trangThai,
                    SucKeoTan = dong.SucKeoToiDaTan,
                    ChieuDaiM = dong.ChieuDaiM,
                    TrongLuongTan = dong.TrongLuongTan
                });
            }
            return ds;
        }

        // Hien phuong an keo-tha len 4 the thong so, bang bien che va bang kiem tra an toan
        private void ApDungPhuongAnLapTau(KetQuaLapTau kq)
        {
            bdPhuongAnMoPhong.Visibility = Visibility.Visible;

            var dm = kq.DauMay;
            txtDauMayChinh.Text = dm?.SoHieu ?? "—";
            txtDauMayChinhPhu.Text = dm != null
                ? $"Dòng {dm.MaDongCode} · Sức kéo {dm.SucKeoTan:N0} tấn"
                : "Chưa chỉ định đầu máy kéo";

            txtTongChieuDai.Text = $"{kq.TongChieuDaiM:N1} m";
            txtTongSoToa.Text = $"· {kq.DanhSachToa.Count} toa";
            txtChieuDaiPhu.Text = "Theo phương án kéo – thả (mô phỏng)";

            txtTongTrongLuong.Text = $"{kq.TongTrongLuongTan:N1} tấn";
            var (_, tyLe, thongDiep) = PhuongTienService.DoiChieuSucKeo(kq.TongTrongLuongTan, dm?.SucKeoTan);
            txtTrongLuongPhu.Text = tyLe > 0 ? $"Chiếm {tyLe:N1}% sức kéo định mức" : thongDiep;

            for (int i = 0; i < kq.DanhSachToa.Count; i++) kq.DanhSachToa[i].ThuTu = i + 1;
            dgBienCheToa.ItemsSource = kq.DanhSachToa.Select(ToaBienCheHienThi.TuToaLapTau).ToList();

            icHangMucAnToan.ItemsSource = kq.HangMuc;
            int soLoi = kq.HangMuc.Count(h => h.MucDo == "LOI");
            int soCanhBao = kq.HangMuc.Count(h => h.MucDo == "CANH_BAO");

            if (soLoi > 0)
                CapNhatKetLuanAnToan(false, "KHÔNG ĐỦ ĐIỀU KIỆN", $"Phương án mô phỏng còn {soLoi} hạng mục không đạt");
            else if (soCanhBao > 0)
                CapNhatKetLuanAnToan(null, "ĐẠT CÓ LƯU Ý", $"Phương án mô phỏng còn {soCanhBao} lưu ý");
            else
                CapNhatKetLuanAnToan(true, "ĐỦ ĐIỀU KIỆN XUẤT GA", "Phương án mô phỏng đạt toàn bộ hạng mục");
        }

        private void BtnBoPhuongAn_Click(object sender, RoutedEventArgs e)
        {
            _phuongAnLapTau.Remove(LayMaChuyenTauLapTau());
            HienThiChuyenTauLapTau();
            CapNhatNhanThayDoiTam();
        }

        // =====================================================================
        // TAB 4: SO DO CHO NGOI
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

            var danhSachGhe = new List<GheHienThi>();
            foreach (DataRow r in dtGhe.Rows)
                danhSachGhe.Add(GheHienThi.TuDongDuLieu(r));

            var danhSachKhach = new List<HanhKhachTrenToaHienThi>();
            foreach (DataRow r in dtKhach.Rows)
                danhSachKhach.Add(HanhKhachTrenToaHienThi.TuDongDuLieu(r));

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
        // THAY DOI TAM
        // =====================================================================

        private void CapNhatNhanThayDoiTam()
        {
            int soThayDoi = _dauMayTam.Count + _toaXeTam.Count + _phuongAnLapTau.Count;
            bdThayDoiTam.Visibility = soThayDoi > 0 ? Visibility.Visible : Visibility.Collapsed;
            txtThayDoiTam.Text = $"{soThayDoi} thay đổi tạm · chưa ghi CSDL";
        }

        private void BtnHuyThayDoiTam_Click(object sender, RoutedEventArgs e)
        {
            bool dongY = ThongBaoDialog.XacNhan(
                "Bỏ toàn bộ đầu máy / toa xe vừa thêm, các chỉnh sửa và phương án lập tàu kéo – thả?\n" +
                "Màn hình sẽ hiển thị lại đúng dữ liệu đang có trong CSDL.",
                "Hoàn tác thay đổi tạm", nutDongY: "Hoàn tác", nutHuy: "Giữ lại");
            if (!dongY) return;

            _dauMayTam.Clear();
            _trangThaiGocDauMay.Clear();
            _toaXeTam.Clear();
            _phuongAnLapTau.Clear();

            NapToanBoDuLieu();
            HienThiChuyenTauLapTau();
        }

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

        private static decimal LayDecimal(DataRow r, string cot)
            => r[cot] != DBNull.Value ? Convert.ToDecimal(r[cot]) : 0m;

        private static int LayInt(DataRow r, string cot)
            => r[cot] != DBNull.Value ? Convert.ToInt32(r[cot]) : 0;

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
