using GUI.ViewModels.KyThuat;
using GUI.Helpers;
using GUI.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GUI.Views.Pages
{
    // =========================================================================
    // TAB 2: PHAN CONG KIP LAI THEO CHANG (nhansu.PhanCongKipLai)
    // Chon chuyen -> so do hanh trinh (chang da co kip / con trong) -> kip cua
    // chuyen -> them / sua / thay nguoi / huy, kiem tra len ban, nhan ban, ban giao.
    // =========================================================================
    public partial class NhanSuPage
    {
        private int? _maChuyenDangChon;

        private TinhTrangChuyen? ChuyenDangChon
            => _tinhTrangChuyen.FirstOrDefault(t => t.Chuyen.MaChuyenTau == _maChuyenDangChon);

        private KipLaiDong? TimKip(int maPhanCong)
            => _tinhTrangChuyen.SelectMany(t => t.Kip).FirstOrDefault(k => k.PhanCong.MaPhanCong == maPhanCong);

        private void LamMoiPhanCong(bool giuDongChon, int? maChuyenChon = null, int? maPhanCongChon = null)
        {
            int? maChuyen = maChuyenChon ?? (giuDongChon ? _maChuyenDangChon : null);
            int? maKip = maPhanCongChon ?? (giuDongChon ? (dgKip.SelectedItem as KipLaiDong)?.PhanCong.MaPhanCong : null);

            string loc = LayTag(cboLocChuyen) ?? "ACTIVE";
            var ds = _tinhTrangChuyen
                .Where(t => loc switch
                {
                    "THIEU" => t.ThieuKip,
                    "LOI" => t.SoKipCoLoi > 0,
                    "ALL" => true,
                    _ => t.ConHieuLuc
                })
                .OrderBy(t => t.ConHieuLuc ? 0 : 1).ThenBy(t => t.Chuyen.GioXuatPhatKH)
                .ToList();

            var chon = ds.FirstOrDefault(t => t.Chuyen.MaChuyenTau == maChuyen) ?? ds.FirstOrDefault();

            _dangNapDuLieu = true;
            dgChuyen.ItemsSource = ds;
            dgChuyen.SelectedItem = chon;
            if (chon != null) dgChuyen.ScrollIntoView(chon);
            _dangNapDuLieu = false;

            txtTrongChuyen.Visibility = ds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Huy hieu tab = so chuyen can xu ly (thieu kip hoac co kip loi)
            int canXuLy = _tinhTrangChuyen.Count(t => t.ThieuKip || t.SoKipCoLoi > 0);
            txtBadgeKip.Text = canXuLy.ToString();
            bdBadgeKip.Background = Mau(canXuLy > 0 ? "#FEE2E2" : "#E2E8F0");
            txtBadgeKip.Foreground = Mau(canXuLy > 0 ? "#B91C1C" : "#334155");
            bdBadgeKip.ToolTip = $"{canXuLy} chuyến thiếu kíp hoặc có kíp cần xử lý";

            HienThiChuyen(chon, maKip);
        }

        private void CboLocChuyen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChuaSanSang) return;
            LamMoiPhanCong(giuDongChon: true);
        }

        private void DgChuyen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangNapDuLieu) return;
            HienThiChuyen(dgChuyen.SelectedItem as TinhTrangChuyen, null);
        }

        private void HienThiChuyen(TinhTrangChuyen? tt, int? maKipChon)
        {
            _maChuyenDangChon = tt?.Chuyen.MaChuyenTau;

            if (tt == null)
            {
                txtCtMac.Text = "—";
                txtCtHanhTrinh.Text = string.Empty;
                txtCtTrangThai.Text = string.Empty;
                txtCtThongTin.Text = "Chọn một chuyến ở danh sách bên trái.";
                dgKip.ItemsSource = null;
                txtTrongKip.Visibility = Visibility.Collapsed;
                icVanDe.ItemsSource = null;
                txtTieuDeVanDe.Text = "VẤN ĐỀ CẦN XỬ LÝ";
                txtKhongVanDe.Visibility = Visibility.Collapsed;
                cvHanhTrinh.Children.Clear();
                CapNhatNutKip();
                return;
            }

            var ct = tt.Chuyen;
            txtCtMac.Text = ct.SoHieuMacTau;
            txtCtHanhTrinh.Text = ct.HanhTrinh;
            txtCtTrangThai.Text = ct.NhanTrangThai;
            txtCtTrangThai.Foreground = Mau(ct.MauTrangThai);
            txtCtThongTin.Text = $"Ngày chạy {ct.NgayXuatPhat:dd/MM/yyyy} · xuất phát {ct.GioXuatPhatKH:HH:mm dd/MM} · " +
                                 $"về đích {ct.GioVeDichKH:HH:mm dd/MM} · đầu máy {ct.NhanDauMay} · {ct.NhanLoaiTau.ToLower()}";

            dgKip.ItemsSource = tt.Kip;
            var kip = tt.Kip.FirstOrDefault(k => k.PhanCong.MaPhanCong == maKipChon) ?? tt.Kip.FirstOrDefault();
            dgKip.SelectedItem = kip;
            txtTrongKip.Visibility = tt.Kip.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtTrongKip.Text = ct.ConHieuLuc
                ? "Chuyến chưa có kíp nào. Bấm \"Thêm Kíp\" (F2) hoặc bấm vào đoạn trống trên sơ đồ."
                : "Chuyến đã kết thúc, không có kíp nào được ghi nhận.";

            icVanDe.ItemsSource = tt.VanDe;
            txtTieuDeVanDe.Text = $"VẤN ĐỀ CẦN XỬ LÝ ({tt.VanDe.Count})";
            txtKhongVanDe.Visibility = tt.VanDe.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtKhongVanDe.Text = ct.ConHieuLuc ? "Không có vấn đề nào — kíp đã phủ trọn hành trình." : "Chuyến đã kết thúc.";

            VeHanhTrinh();
            CapNhatNutKip();
        }

        private void DgKip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CapNhatNutKip();
            VeHanhTrinh();
        }

        private void CapNhatNutKip()
        {
            var tt = ChuyenDangChon;
            var kip = dgKip.SelectedItem as KipLaiDong;
            bool daPhanCong = kip?.LaDaPhanCong == true;

            btnThemKip.IsEnabled = tt?.ConHieuLuc == true;
            btnSuaKip.IsEnabled = daPhanCong;
            btnHuyKip.IsEnabled = daPhanCong;
            btnKiemTraKip.IsEnabled = daPhanCong;
            btnNhanBan.IsEnabled = daPhanCong;
            btnBanGiao.IsEnabled = kip?.PhanCong.TrangThai == PhanCongKipHienThi.DangThucHien;
        }

        // Mo mot kip tu noi khac (ho so nhan vien, bieu do ca truc)
        private void MoKip(int maChuyenTau, int maPhanCong)
        {
            tabNhanSu.SelectedIndex = 1;
            bool dangHien = (dgChuyen.ItemsSource as IEnumerable<TinhTrangChuyen>)?.Any(t => t.Chuyen.MaChuyenTau == maChuyenTau) == true;
            if (!dangHien) ChonTheoTag(cboLocChuyen, "ALL");
            LamMoiPhanCong(giuDongChon: false, maChuyenTau, maPhanCong);
        }

        // =====================================================================
        // SO DO HANH TRINH: ga dung cach deu, doan kip phia tren duong tuyen
        // =====================================================================

        private void CvHanhTrinh_SizeChanged(object sender, SizeChangedEventArgs e) => VeHanhTrinh();

        private void VeHanhTrinh()
        {
            cvHanhTrinh.Children.Clear();
            var tt = ChuyenDangChon;
            double rong = cvHanhTrinh.ActualWidth;
            if (tt == null || rong < 160) return;

            var diem = tt.Chuyen.LichDung;
            int n = diem.Count;
            if (n < 2) return;

            const double le = 46, yDai = 1, caoDai = 20, yTuyen = 33;
            double buoc = (rong - 2 * le) / (n - 1);
            double X(int i) => le + i * buoc;
            var kipChon = dgKip.SelectedItem as KipLaiDong;

            var tuyen = new Rectangle { Width = X(n - 1) - X(0), Height = 3, Fill = Mau("#94A3B8") };
            Canvas.SetLeft(tuyen, X(0));
            Canvas.SetTop(tuyen, yTuyen - 1.5);
            cvHanhTrinh.Children.Add(tuyen);

            foreach (var d in tt.Doan)
            {
                double x1 = X(d.TuViTri) + 3;
                double w = Math.Max(X(d.DenViTri) - 3 - x1, 8);
                FrameworkElement dai = d.Kip == null ? TaoDaiTrong(tt, d, w, caoDai) : TaoDaiKip(d, w, caoDai, kipChon);
                Canvas.SetLeft(dai, x1);
                Canvas.SetTop(dai, yDai);
                cvHanhTrinh.Children.Add(dai);
            }

            double rongNhan = Math.Min(buoc, 120);
            for (int i = 0; i < n; i++)
            {
                var ga = new Ellipse { Width = 9, Height = 9, Fill = Brushes.White, Stroke = Mau("#003B73"), StrokeThickness = 2 };
                Canvas.SetLeft(ga, X(i) - 4.5);
                Canvas.SetTop(ga, yTuyen - 4.5);
                cvHanhTrinh.Children.Add(ga);

                var ten = new TextBlock
                {
                    Text = diem[i].TenGa, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Mau("#0F172A"),
                    Width = rongNhan, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis
                };
                Canvas.SetLeft(ten, X(i) - rongNhan / 2);
                Canvas.SetTop(ten, yTuyen + 7);
                cvHanhTrinh.Children.Add(ten);

                DateTime gio = i == n - 1 ? diem[i].GioDenKeHoach : diem[i].GioDiKeHoach;
                var tbGio = new TextBlock
                {
                    Text = gio.ToString("HH:mm"), FontSize = 9, Foreground = Mau("#64748B"),
                    Width = rongNhan, TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(tbGio, X(i) - rongNhan / 2);
                Canvas.SetTop(tbGio, yTuyen + 20);
                cvHanhTrinh.Children.Add(tbGio);
            }

            // Vi tri tau uoc tinh theo gio ke hoach (chuyen dang chay)
            if (tt.Chuyen.DaXuatPhat && _duLieu != null)
            {
                double? x = ViTriTau(tt.Chuyen, DuLieu.BayGio, X);
                if (x.HasValue)
                {
                    var tau = new Polygon
                    {
                        Points = new PointCollection { new(0, 0), new(10, 0), new(5, 7) },
                        Fill = Mau("#DC2626"),
                        ToolTip = $"Vị trí ước tính theo giờ kế hoạch lúc {DuLieu.BayGio:HH:mm}"
                    };
                    Canvas.SetLeft(tau, x.Value - 5);
                    Canvas.SetTop(tau, yTuyen - 11);
                    cvHanhTrinh.Children.Add(tau);
                }
            }
        }

        private static double? ViTriTau(ChuyenTauNhanSu ct, DateTime bayGio, Func<int, double> X)
        {
            var d = ct.LichDung;
            if (bayGio < d[0].GioDiKeHoach) return null;
            for (int i = 0; i < d.Count - 1; i++)
            {
                if (bayGio <= d[i].GioDiKeHoach) return X(i);
                DateTime den = d[i + 1].GioDenKeHoach;
                if (bayGio < den)
                {
                    double tiLe = (bayGio - d[i].GioDiKeHoach).TotalMinutes / Math.Max(1, (den - d[i].GioDiKeHoach).TotalMinutes);
                    return X(i) + tiLe * (X(i + 1) - X(i));
                }
            }
            return X(d.Count - 1);
        }

        private FrameworkElement TaoDaiKip(DoanPhuKip d, double w, double cao, KipLaiDong? kipChon)
        {
            var k = d.Kip!;
            bool dangChon = kipChon?.PhanCong.MaPhanCong == k.PhanCong.MaPhanCong;

            (string nen, string vien, string chu) = k.PhanCong.TrangThai switch
            {
                PhanCongKipHienThi.DangThucHien => ("#2563EB", "#1D4ED8", "#FFFFFF"),
                PhanCongKipHienThi.HoanThanh => ("#E2E8F0", "#94A3B8", "#334155"),
                _ => ("#DBEAFE", "#60A5FA", "#1E3A8A")
            };
            if (d.Chong) (nen, vien, chu) = ("#FEE2E2", "#DC2626", "#991B1B");
            else if (k.LaDaPhanCong && k.CoLoi) vien = "#DC2626";

            string nhan = d.Chong
                ? "Nhiều kíp chồng chặng"
                : $"{k.TenKip} · {TenGoi(k.LaiTau)} / {TenGoi(k.PhuLai)} / {TenGoi(k.TruongTau)}";

            var dai = new Border
            {
                Width = w, Height = cao,
                Background = Mau(nen),
                BorderBrush = Mau(dangChon ? "#0F172A" : vien),
                BorderThickness = new Thickness(dangChon ? 2 : 1),
                CornerRadius = new CornerRadius(2),
                Cursor = Cursors.Hand,
                Child = new TextBlock
                {
                    Text = nhan, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Mau(chu),
                    Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                },
                ToolTip = $"{k.TenKip}: {k.PhanCong.Chang}\n{k.PhanCong.KhungGio}\n" +
                          $"Lái tàu: {k.LaiTau.HoTen}\nPhụ lái: {k.PhuLai.HoTen}\nTrưởng tàu: {k.TruongTau.HoTen}\n{k.TinhTrang}"
            };
            dai.MouseLeftButtonUp += (_, e) =>
            {
                e.Handled = true;
                dgKip.SelectedItem = k;
            };
            return dai;
        }

        private FrameworkElement TaoDaiTrong(TinhTrangChuyen tt, DoanPhuKip d, double w, double cao)
        {
            var gaNhan = tt.Chuyen.LichDung[d.TuViTri];
            var gaGiao = tt.Chuyen.LichDung[d.DenViTri];
            bool coThePhanCong = tt.ConHieuLuc;

            var o = new Grid
            {
                Width = w, Height = cao,
                Cursor = coThePhanCong ? Cursors.Hand : Cursors.Arrow,
                ToolTip = coThePhanCong
                    ? $"Chặng {gaNhan.TenGa} → {gaGiao.TenGa} chưa có kíp. Bấm để phân công."
                    : "Không có kíp được ghi nhận"
            };
            o.Children.Add(new Rectangle
            {
                Fill = Mau(coThePhanCong ? "#FEF2F2" : "#F8FAFC"),
                Stroke = Mau(coThePhanCong ? "#F87171" : "#CBD5E1"),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 2 },
                RadiusX = 2, RadiusY = 2
            });
            o.Children.Add(new TextBlock
            {
                Text = coThePhanCong ? "Chưa có kíp" : "Không có kíp",
                FontSize = 10, FontWeight = FontWeights.SemiBold,
                Foreground = Mau(coThePhanCong ? "#B91C1C" : "#94A3B8"),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            if (coThePhanCong)
            {
                o.MouseLeftButtonUp += (_, e) =>
                {
                    e.Handled = true;
                    ThemKip(gaNhan.MaGa, gaGiao.MaGa);
                };
            }
            return o;
        }

        // Ten goi (chu cuoi cua ho ten) cho nhan tren so do
        private static string TenGoi(ThanhVienKip tv)
        {
            string ten = tv.NhanVien?.HoTen.Trim() ?? "?";
            int i = ten.LastIndexOf(' ');
            return i >= 0 ? ten[(i + 1)..] : ten;
        }

        // =====================================================================
        // THAO TAC TREN KIP
        // =====================================================================

        private void BtnThemKip_Click(object sender, RoutedEventArgs e) => ThemKip();

        private void BtnSuaKip_Click(object sender, RoutedEventArgs e) => SuaKip();

        private void DongKip_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgKip.SelectedItem is KipLaiDong k && k.LaDaPhanCong) SuaKip(k);
        }

        private void BtnHuyKip_Click(object sender, RoutedEventArgs e) => HuyKip();

        private void BtnKiemTraKip_Click(object sender, RoutedEventArgs e)
        {
            if (dgKip.SelectedItem is KipLaiDong k) KiemTraKip(k);
        }

        private void BtnNhanBan_Click(object sender, RoutedEventArgs e)
        {
            if (dgKip.SelectedItem is KipLaiDong k) NhanBan(k);
        }

        private void BtnBanGiao_Click(object sender, RoutedEventArgs e)
        {
            if (dgKip.SelectedItem is KipLaiDong k) BanGiao(k);
        }

        private void ThemKip(int? maGaNhan = null, int? maGaGiao = null)
        {
            tabNhanSu.SelectedIndex = 1;
            var tt = ChuyenDangChon;
            if (tt == null) return;
            if (!tt.ConHieuLuc)
            {
                ThongBaoDialog.ThongTin($"Chuyến {tt.Chuyen.SoHieuMacTau} ngày {tt.Chuyen.NgayXuatPhat:dd/MM/yyyy} đã kết thúc, không phân công thêm kíp.",
                                        "Phân công kíp lái");
                return;
            }

            // Mac dinh chon chang trong dau tien
            if (maGaNhan == null)
            {
                var trong = tt.Doan.FirstOrDefault(d => d.Kip == null);
                if (trong != null)
                {
                    maGaNhan = tt.Chuyen.LichDung[trong.TuViTri].MaGa;
                    maGaGiao = tt.Chuyen.LichDung[trong.DenViTri].MaGa;
                }
            }

            var dlg = new PhanCongKipDialog(DuLieu, tt.Chuyen, null, maGaNhan, maGaGiao) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var pc = dlg.KetQua;
            pc.MaPhanCong = _maTamKeTiep--;
            pc.LaThayDoiTam = true;
            _phanCongTam[pc.MaPhanCong] = pc;
            LamMoiSauKhiDoiKip(pc);

            string tenKip = TimKip(pc.MaPhanCong)?.TenKip.ToLower() ?? "kíp";
            ThongBaoDialog.ThanhCong(
                $"Đã phân công {tenKip} cho chặng {pc.Chang} của chuyến {pc.Chuyen.SoHieuMacTau} ({pc.KhungGio}).\n\n" +
                $"Trước giờ nhận ban {pc.GioNhanBan:HH:mm dd/MM}, cả 3 người phải qua kiểm tra lên ban.\n" +
                "Kíp đang hiển thị trên màn hình, chưa ghi vào CSDL.",
                "Phân công kíp lái");
        }

        private void SuaKip(KipLaiDong? dong = null, string? vaiTroThay = null)
        {
            dong ??= dgKip.SelectedItem as KipLaiDong;
            if (dong == null) return;
            if (!dong.LaDaPhanCong)
            {
                ThongBaoDialog.ThongTin($"{dong.TenKip} đã {dong.PhanCong.NhanTrangThai.ToLower()}, không đổi được thành phần kíp.",
                                        "Sửa kíp lái");
                return;
            }

            var goc = dong.PhanCong;
            var dlg = new PhanCongKipDialog(DuLieu, dong.Chuyen, goc.SaoChep(), null, null, vaiTroThay) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua == null) return;

            var pc = dlg.KetQua;
            pc.LaThayDoiTam = true;
            _phanCongTam[pc.MaPhanCong] = pc;
            LamMoiSauKhiDoiKip(pc);

            var thay = QuyTacKipLai.CacVaiTro
                .Where(vt => goc.MaNhanVienTheoVaiTro(vt) != pc.MaNhanVienTheoVaiTro(vt))
                .Select(vt => $"• {QuyTacKipLai.TenVaiTro(vt)}: {DuLieu.TimNhanVien(goc.MaNhanVienTheoVaiTro(vt))?.HoTen} → " +
                              $"{DuLieu.TimNhanVien(pc.MaNhanVienTheoVaiTro(vt))?.HoTen}")
                .ToList();

            ThongBaoDialog.ThanhCong(
                $"Đã cập nhật {dong.TenKip.ToLower()} chuyến {pc.Chuyen.SoHieuMacTau} ({pc.Chang}).\n" +
                (thay.Count > 0
                    ? $"{string.Join("\n", thay)}\n\nNgười mới phải qua kiểm tra lên ban trước {pc.GioNhanBan:HH:mm dd/MM}.\n"
                    : "\n") +
                "Thay đổi đang hiển thị trên màn hình, chưa ghi vào CSDL.",
                "Cập nhật kíp lái");
        }

        private void HuyKip()
        {
            if (dgKip.SelectedItem is not KipLaiDong dong || !dong.LaDaPhanCong) return;

            bool dongY = ThongBaoDialog.XacNhan(
                $"Hủy {dong.TenKip.ToLower()} ({dong.PhanCong.Chang}) của chuyến {dong.Chuyen.SoHieuMacTau}?\n" +
                "Chặng này sẽ trở lại trạng thái chưa có kíp. Kết quả kiểm tra đã ghi vẫn giữ trong sổ.",
                "Hủy phân công kíp", nutDongY: "Hủy Kíp", nutHuy: "Giữ Lại", laHanhDongXoa: true);
            if (!dongY) return;

            int ma = dong.PhanCong.MaPhanCong;
            _phanCongTam.Remove(ma);
            if (ma > 0) _phanCongDaHuy.Add(ma);
            LamMoiTatCa(maChuyenChon: dong.Chuyen.MaChuyenTau);
        }

        private void LamMoiSauKhiDoiKip(PhanCongKipHienThi pc)
        {
            LamMoiTatCa(maChuyenChon: pc.MaChuyenTau, maPhanCongChon: pc.MaPhanCong);

            // Bo loc danh sach chuyen an mat chuyen vua sua
            if (_maChuyenDangChon != pc.MaChuyenTau)
            {
                ChonTheoTag(cboLocChuyen, pc.Chuyen.ConHieuLuc ? "ACTIVE" : "ALL");
                LamMoiPhanCong(giuDongChon: false, pc.MaChuyenTau, pc.MaPhanCong);
            }
        }

        // Ghi ket qua kiem tra len ban cho kip; sau do goi y thay nguoi / nhan ban
        private void KiemTraKip(KipLaiDong dong)
        {
            if (!dong.LaDaPhanCong) return;

            var dlg = new KiemTraLenBanDialog(DuLieu, dong.PhanCong, dong.SoThuTu) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true || dlg.KetQua.Count == 0) return;

            foreach (var kt in dlg.KetQua)
            {
                kt.MaKiemTra = _maTamKeTiep--;
                kt.LaThayDoiTam = true;
                _kiemTraTam[kt.MaKiemTra] = kt;
            }

            int maPhanCong = dong.PhanCong.MaPhanCong;
            LamMoiTatCa(maChuyenChon: dong.Chuyen.MaChuyenTau, maPhanCongChon: maPhanCong);

            var kip = TimKip(maPhanCong);
            if (kip == null) return;

            var vuaKhongDat = kip.ThanhVien
                .Where(t => t.NhanVien != null && dlg.KetQua.Any(k => k.MaNhanVien == t.NhanVien.MaNhanVien && !k.DuDieuKien))
                .ToList();

            if (vuaKhongDat.Count > 0)
            {
                string ds = string.Join("\n", vuaKhongDat.Select(t => $"• {t.TenVaiTro} {t.HoTen}: {t.KiemTra.LanKhongDat?.LyDoKhongDat}"));
                bool thay = ThongBaoDialog.XacNhan(
                    $"Kết quả kiểm tra lên ban {kip.NhanChuyen} · {kip.TenKip}:\n{ds}\n\n" +
                    "Người không đạt không được nhận ban chuyến này. Chọn người thay ngay?",
                    "Không đủ điều kiện lên ban", nutDongY: "Thay Người", nutHuy: "Để Sau");
                if (thay) SuaKip(kip, vuaKhongDat[0].VaiTro);
                return;
            }

            if (kip.CoTheNhanBan)
            {
                bool nhan = ThongBaoDialog.XacNhan(
                    $"Cả 3 thành viên {kip.TenKip.ToLower()} chuyến {kip.Chuyen.SoHieuMacTau} đạt kiểm tra lên ban " +
                    "và kíp không còn lỗi phân công.\n\nXác nhận kíp nhận ban ngay?",
                    "Đủ điều kiện nhận ban", nutDongY: "Nhận Ban", nutHuy: "Để Sau");
                if (nhan) NhanBan(kip, hoiXacNhan: false);
                return;
            }

            var conCho = kip.ThanhVien.Where(t => t.KiemTra.ChuaKiemTra).Select(t => $"{t.TenVaiTro.ToLower()} {t.HoTen}").ToList();
            var loi = kip.VanDe.Where(v => v.MucDo == MucDoVanDe.NghiemTrong).Select(v => "• " + v.NoiDung).ToList();
            ThongBaoDialog.ThongTin(
                $"Đã ghi {dlg.KetQua.Count} kết quả kiểm tra (đạt).\n" +
                (conCho.Count > 0 ? $"Còn chờ kiểm tra: {string.Join(", ", conCho)}.\n" : "") +
                (loi.Count > 0 ? $"\nKíp còn lỗi phân công, chưa nhận ban được:\n{string.Join("\n", loi)}\n" : "") +
                "\nKết quả đang hiển thị trên màn hình, chưa ghi vào CSDL.",
                "Ghi kết quả kiểm tra");
        }

        private void NhanBan(KipLaiDong dong, bool hoiXacNhan = true)
        {
            if (!dong.LaDaPhanCong) return;

            if (!dong.CoTheNhanBan)
            {
                var lyDo = dong.VanDe.Where(v => v.MucDo == MucDoVanDe.NghiemTrong).Select(v => v.NoiDung)
                    .Concat(dong.ThanhVien.Where(t => t.KiemTra.ChuaKiemTra)
                                          .Select(t => $"{t.TenVaiTro} {t.HoTen}: chưa kiểm tra lên ban"))
                    .Distinct()
                    .Select(l => "• " + l);
                ThongBaoDialog.CanhBao(
                    $"{dong.NhanChuyen} · {dong.TenKip} ({dong.PhanCong.Chang}) chưa đủ điều kiện nhận ban:\n{string.Join("\n", lyDo)}",
                    "Chưa thể nhận ban");
                return;
            }

            if (hoiXacNhan && !ThongBaoDialog.XacNhan(
                    $"Xác nhận {dong.TenKip.ToLower()} chuyến {dong.Chuyen.SoHieuMacTau} nhận ban tại {dong.PhanCong.TenGaNhanBan} " +
                    $"lúc {DateTime.Now:HH:mm}?\n\nLái tàu {dong.LaiTau.HoTen}, phụ lái {dong.PhuLai.HoTen}, " +
                    $"trưởng tàu {dong.TruongTau.HoTen} chuyển sang \"Đang làm nhiệm vụ\".",
                    "Xác nhận nhận ban", nutDongY: "Nhận Ban", nutHuy: "Hủy Bỏ"))
                return;

            var pc = dong.PhanCong.SaoChep();
            pc.TrangThai = PhanCongKipHienThi.DangThucHien;
            pc.LaThayDoiTam = true;
            _phanCongTam[pc.MaPhanCong] = pc;
            DoiTrangThaiThanhVien(dong, NhanVienHienThi.DangLam);
            LamMoiSauKhiDoiKip(pc);

            ThongBaoDialog.ThanhCong(
                $"{dong.TenKip} chuyến {pc.Chuyen.SoHieuMacTau} đã nhận ban tại {pc.TenGaNhanBan}.\n" +
                $"Bàn giao dự kiến tại {pc.TenGaBanGiao} lúc {pc.GioBanGiao:HH:mm dd/MM}.\n\nChưa ghi vào CSDL.",
                "Nhận ban");
        }

        private void BanGiao(KipLaiDong dong)
        {
            if (dong.PhanCong.TrangThai != PhanCongKipHienThi.DangThucHien) return;

            bool dongY = ThongBaoDialog.XacNhan(
                $"Xác nhận {dong.TenKip.ToLower()} chuyến {dong.Chuyen.SoHieuMacTau} đã bàn giao tại {dong.PhanCong.TenGaBanGiao}?\n\n" +
                $"3 người chuyển sang \"Nghỉ ngơi\" và phải nghỉ tối thiểu {QuyTacKipLai.SoGioNghiToiThieu:0} giờ " +
                "trước khi nhận ban tiếp theo.",
                "Xác nhận bàn giao", nutDongY: "Bàn Giao", nutHuy: "Hủy Bỏ");
            if (!dongY) return;

            var pc = dong.PhanCong.SaoChep();
            pc.TrangThai = PhanCongKipHienThi.HoanThanh;
            pc.LaThayDoiTam = true;
            _phanCongTam[pc.MaPhanCong] = pc;
            DoiTrangThaiThanhVien(dong, NhanVienHienThi.NghiNgoi);
            LamMoiSauKhiDoiKip(pc);
        }

        private void DoiTrangThaiThanhVien(KipLaiDong dong, string trangThai)
        {
            foreach (var tv in dong.ThanhVien)
            {
                if (tv.NhanVien == null) continue;
                var nv = tv.NhanVien.SaoChep();
                nv.TrangThai = trangThai;
                nv.LaThayDoiTam = true;
                _nhanVienTam[nv.MaNhanVien] = nv;
            }
        }
    }
}
