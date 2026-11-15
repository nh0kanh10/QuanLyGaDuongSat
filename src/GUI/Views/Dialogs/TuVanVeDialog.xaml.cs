using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BUS.AI;
using BUS.Services;
using DTO.AI;
using DTO.Common;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // TRO LY AI TU VAN VE (Sprint 3, Dat - FR-8.1 / UC-05). Mo tu trang Ban ve (nut "Tu van AI").
    // [16/11] 2 che do o cot trai:
    //   - "Hoi AI": tro chuyen tu do, nho hoi thoai trong cua so (TroLyChatService). AI tu tra du lieu that bang cong cu
    //     (tim chuyen, lich trinh, cho trong + gia, tu van MCDM) va co the dat ve ho (mo XacNhanDatVeHoDialog).
    //   - "Loc nang cao" (truoc goi "Nhap form", Dat doi ten 16/11): nhap tieu chi -> cham diem MCDM, chay offline nhu truoc
    //     (khong co khoa API van dung duoc).
    // Cot phai: 3 phuong an (tu AI hoac loc nang cao). Moi the: "Dat Ve Ngay" (cua so xac nhan) hoac "Dua Vao Gio Ve" (trang Ban ve).
    // =========================================================================
    public partial class TuVanVeDialog : Window
    {
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
        private readonly TuVanVeService _sv = new();
        private readonly TroLyChatService _chat = new();
        private readonly CancellationTokenSource _huy = new();
        private readonly ObservableCollection<TinNhanHienThi> _tinNhan = new();
        private readonly Dictionary<int, KetQuaDatVeHo> _daDatThe = new();      // so thu tu phuong an dang hien -> da dat
        private readonly bool _choPhepChon;
        private readonly bool _coQuyenBanVe;
        private YeuCauTuVanVe? _ycHienTai;
        private KetQuaTuVanVe? _kqHienTai;
        private bool _dangGui;

        public PhuongAnDuocChon? PhuongAn { get; private set; }

        // Co dat ve trong cua so nay -> trang Ban ve nap lai so do ghe khi dong.
        public bool DaDatVe { get; private set; }

        private sealed record MucGa(int MaGa, string Nhan);

        public TuVanVeDialog(int? maGaDi = null, int? maGaDen = null, DateTime? ngay = null, bool choPhepChon = true)
        {
            InitializeComponent();
            _choPhepChon = choPhepChon;
            _coQuyenBanVe = PhanQuyenService.CoQuyen(MaQuyen.BanVe);
            if (_sv.LayDanhSachGa(out var dsGa, out string loiGa))
            {
                var ga = dsGa.Select(g => new MucGa(g.MaGa, $"{g.TenGa} ({g.MaGaCode})")).ToList();
                cboGaDi.ItemsSource = ga;
                cboGaDen.ItemsSource = ga;
                cboGaDi.SelectedItem = ga.FirstOrDefault(g => g.MaGa == maGaDi) ?? ga.FirstOrDefault();
                cboGaDen.SelectedItem = ga.FirstOrDefault(g => g.MaGa == maGaDen) ?? ga.LastOrDefault();
            }
            else txtLoi.Text = loiGa;
            dpNgayDi.SelectedDate = ngay.HasValue && ngay.Value.Date >= DateTime.Today ? ngay.Value.Date : DateTime.Today;
            dpNgayDi.DisplayDateStart = DateTime.Today;
            HienTrongSo(QuyTacTuVanVe.TrongSoMacDinh);
            DoiTuong_Changed(this, new RoutedEventArgs());

            icChat.ItemsSource = _tinNhan;
            if (_chat.SanSang)
            {
                _chat.XacNhanDatVe = yc => Dispatcher.InvokeAsync(() => MoXacNhanDatVe(yc, doAIDeNghi: true)).Task;
                txtPhuDeHeader.Text = $"Trò chuyện với AI ({_chat.TenMoHinh}) · AI tự tra dữ liệu thật · chấm điểm đa tiêu chí (MCDM) · đặt vé hộ";
                txtChanTrang.Text = "Câu hỏi và dữ liệu chuyến / giá được gửi tới OpenAI (đã ẩn dãy số dài như CCCD / SĐT); thông tin hành khách " +
                                    "chỉ nhập ở cửa sổ xác nhận, không gửi cho AI. Đặt vé luôn phải qua bước xác nhận. Mỗi câu hỏi được ghi nhật ký.";
                LoiChao();
                Loaded += (_, _) => txtCauHoi.Focus();
            }
            else
            {
                rdoHoiAI.IsEnabled = false;
                rdoHoiAI.ToolTip = _chat.LyDoChuaSanSang;
                rdoLocNangCao.IsChecked = true;
                _tinNhan.Add(TinNhanHienThi.HeThong(_chat.LyDoChuaSanSang + " Dùng chế độ \"Lọc nâng cao\" để tư vấn (chạy offline)."));
            }
            if (!choPhepChon) txtChanTrang.Text += " Mở từ trang Bán vé để đưa phương án vào giỏ.";
            Closed += (_, _) => _huy.Cancel();
        }

        private void LoiChao()
            => _tinNhan.Add(TinNhanHienThi.TroLy(
                "Chào anh/chị, em là trợ lý AI của quầy vé. Anh/chị cứ hỏi tự nhiên, ví dụ:\n" +
                "- Hai ông bà đi Đà Nẵng tối thứ Sáu, tối đa 1 triệu một vé\n" +
                "- Mai có tàu nào từ Hà Nội vào Vinh buổi sáng không?\n" +
                "- Tàu SE3 dừng những ga nào?\n" +
                "Cần đặt thì nói \"đặt phương án 1\", em sẽ mở cửa sổ xác nhận.", new List<string>(), ""));

        private void CheDo_Checked(object sender, RoutedEventArgs e)
        {
            if (pnlChat == null || pnlLocNangCao == null) return;      // dang dung cay XAML
            bool chat = rdoHoiAI.IsChecked == true;
            pnlChat.Visibility = chat ? Visibility.Visible : Visibility.Collapsed;
            pnlLocNangCao.Visibility = chat ? Visibility.Collapsed : Visibility.Visible;
            if (chat && IsLoaded) txtCauHoi.Focus();
        }

        // --- [16/11] Tro chuyen -----------------------------------------------------
        private async void BtnGui_Click(object sender, RoutedEventArgs e)
        {
            if (_dangGui || !_chat.SanSang) return;
            string cau = txtCauHoi.Text.Trim();
            if (cau.Length == 0) { txtCauHoi.Focus(); return; }

            _dangGui = true;
            btnGui.IsEnabled = false;
            btnChatMoi.IsEnabled = false;
            txtCauHoi.Clear();
            _tinNhan.Add(TinNhanHienThi.NguoiDung(QuyTacMoHinhNgonNgu.AnDuLieuCaNhan(cau)));
            CuonXuong();
            txtTrangThaiChat.Visibility = Visibility.Visible;
            var tienDo = new Progress<string>(s => txtTrangThaiChat.Text = s);
            try
            {
                var kq = await _chat.GuiAsync(cau, tienDo, _huy.Token);
                if (!IsLoaded) return;
                if (!kq.ThanhCong)
                {
                    _tinNhan.Add(TinNhanHienThi.Loi("Chưa trả lời được: " + kq.Loi));
                }
                else
                {
                    var buoc = new List<string>(kq.CacBuocTraCuu) { kq.SoLanGoiMoHinh == 1 ? "1 lần gọi AI (không cần tra dữ liệu)" : $"{kq.SoLanGoiMoHinh} lần gọi AI" };
                    string canhBao = kq.SoLieuChuaDoiChieu.Count > 0
                        ? "Số chưa đối chiếu được với dữ liệu hệ thống: " + string.Join(", ", kq.SoLieuChuaDoiChieu) + " — kiểm tra lại trước khi báo khách."
                        : "";
                    _tinNhan.Add(TinNhanHienThi.TroLy(kq.TraLoi, buoc, canhBao));
                    if (kq.TuVan != null && kq.YeuCauTuVan != null) HienKetQua(kq.YeuCauTuVan, kq.TuVan, "AI");
                }
                CuonXuong();
            }
            catch (OperationCanceledException) { /* dialog da dong */ }
            finally
            {
                _dangGui = false;
                if (IsLoaded)
                {
                    btnGui.IsEnabled = true;
                    btnChatMoi.IsEnabled = true;
                    txtTrangThaiChat.Visibility = Visibility.Collapsed;
                    txtCauHoi.Focus();
                }
            }
        }

        private void TxtCauHoi_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Enter gui, Shift+Enter xuong dong (AcceptsReturn)
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                BtnGui_Click(sender, new RoutedEventArgs());
            }
        }

        private void BtnChatMoi_Click(object sender, RoutedEventArgs e)
        {
            if (_dangGui) return;
            _chat.BatDauLai();
            _tinNhan.Clear();
            LoiChao();
            if (_kqHienTai != null && _ycHienTai != null) _chat.GhiNhanTuVanTuForm(_ycHienTai, _kqHienTai);   // the dang hien van dat duoc
            txtCauHoi.Focus();
        }

        private void CuonXuong() => Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => svChat.ScrollToEnd()));

        // --- Dat ve ho ----------------------------------------------------------------
        private KetQuaDatVeHo MoXacNhanDatVe(YeuCauDatVeHo yc, bool doAIDeNghi)
        {
            var dlg = new XacNhanDatVeHoDialog(yc, doAIDeNghi) { Owner = this };
            dlg.ShowDialog();
            var kq = dlg.KetQua;
            if (kq.DaDat)
            {
                DaDatVe = true;
                if (_kqHienTai != null && _kqHienTai.CacPhuongAn.Contains(yc.PhuongAn))
                {
                    _daDatThe[yc.PhuongAn.XepHang] = kq;
                    VeLaiThe();
                }
            }
            return kq;
        }

        private void BtnDatVe_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not TheHienThi the || _ycHienTai == null || _kqHienTai == null || _dangGui) return;
            var kq = MoXacNhanDatVe(new YeuCauDatVeHo
            {
                PhuongAn = the.Goc,
                YeuCau = _ycHienTai,
                TenGaDi = _kqHienTai.TenGaDi,
                TenGaDen = _kqHienTai.TenGaDen,
            }, doAIDeNghi: false);
            if (!kq.DaDat) return;
            _chat.GhiNhanDaDatNgoaiChat(the.Goc.XepHang, kq);
            _tinNhan.Add(TinNhanHienThi.HeThong($"Đã đặt phương án {the.Goc.XepHang} ({the.Goc.SoHieuMacTau}): đơn {kq.MaPnr}, {kq.SoVe} vé, " +
                                                $"tổng {kq.TongTien.ToString("N0", Vi)} đ."));
            CuonXuong();
        }

        // --- Loc nang cao (offline) -------------------------------------------------------------
        // [12/11] Hien loai khach du kien (gio ve dat san doi tuong + gia sau giam theo dong nay)
        private void DoiTuong_Changed(object sender, RoutedEventArgs e)
        {
            if (txtLoaiKhachDuKien == null || cboSoKhach == null || chkCaoTuoi == null || chkTreEm == null || chkSinhVien == null) return;
            var ds = QuyTacTuVanVe.LoaiKhachDuKien(cboSoKhach.SelectedIndex + 1, chkCaoTuoi.IsChecked == true,
                                                  chkTreEm.IsChecked == true, chkSinhVien.IsChecked == true);
            txtLoaiKhachDuKien.Text = "Vé dự kiến: " + QuyTacTuVanVe.MoTaLoaiKhach(ds) + " (giá so ngân sách đã trừ giảm giá)";
        }

        private YeuCauTuVanVe DocYeuCau()
        {
            decimal? toiDa = decimal.TryParse(txtNganSachToiDa.Text.Trim().Replace(",", "").Replace(".", "").Replace(" ", ""), out var v) && v > 0 ? v : null;
            return new YeuCauTuVanVe
            {
                MaGaDi = (cboGaDi.SelectedItem as MucGa)?.MaGa ?? 0,
                MaGaDen = (cboGaDen.SelectedItem as MucGa)?.MaGa ?? 0,
                NgayDi = dpNgayDi.SelectedDate ?? DateTime.Today,
                SoNguoi = cboSoKhach.SelectedIndex + 1,
                CoNguoiCaoTuoi = chkCaoTuoi.IsChecked == true,
                CoTreEm = chkTreEm.IsChecked == true,
                LaSinhVien = chkSinhVien.IsChecked == true,
                MangXeMay = chkXeMay.IsChecked == true,
                MucNganSach = (cboNganSach.SelectedItem as ComboBoxItem)?.Tag as string ?? MucNganSach.TieuChuan,
                NganSachToiDaMoiNguoi = toiDa,
                KhungGio = (cboKhungGio.SelectedItem as ComboBoxItem)?.Tag as string ?? KhungGioUuTien.KhongUuTien,
                TrongSo = chkTuChinh.IsChecked == true ? new[] { sldW0.Value, sldW1.Value, sldW2.Value, sldW3.Value } : null
            };
        }

        private void BtnTuVan_Click(object sender, RoutedEventArgs e)
        {
            txtLoi.Text = "";
            // [12/11] Dan chu vao o ngan sach (PreviewTextInput khong chan dan) -> truoc day bi bo qua im lang = khong gioi han
            string ns = txtNganSachToiDa.Text.Trim().Replace(",", "").Replace(".", "").Replace(" ", "");
            if (ns.Length > 0 && (!ns.All(char.IsDigit) || !decimal.TryParse(ns, out var nsSo) || nsSo <= 0))
            {
                txtLoi.Text = "Ngân sách tối đa mỗi vé chỉ gồm chữ số, lớn hơn 0 (ví dụ 900000). Bỏ trống nếu không giới hạn.";
                txtNganSachToiDa.Focus();
                txtNganSachToiDa.SelectAll();
                return;
            }
            var yc = DocYeuCau();
            Mouse.OverrideCursor = Cursors.Wait;
            bool ok;
            KetQuaTuVanVe kq;
            string loi;
            try { ok = _sv.TuVan(yc, out kq, out loi); }
            finally { Mouse.OverrideCursor = null; }
            if (!ok) { txtLoi.Text = loi; return; }

            HienKetQua(yc, kq, "loc");
            if (chkTuChinh.IsChecked != true) HienTrongSo(kq.TrongSoDaDung);
            _chat.GhiNhanTuVanTuForm(yc, kq);       // AI biet cac phuong an dang hien -> "dat phuong an 2" dung bo nay
        }

        // --- Cot phai: phuong an -----------------------------------------------------------
        private void HienKetQua(YeuCauTuVanVe yc, KetQuaTuVanVe kq, string nguon)
        {
            _ycHienTai = yc;
            _kqHienTai = kq;
            _daDatThe.Clear();
            txtKqTieuDe.Text = kq.CacPhuongAn.Count == 0
                ? $"Không tìm được phương án cho {kq.TenGaDi} → {kq.TenGaDen}"
                : $"{kq.CacPhuongAn.Count} phương án gợi ý · {kq.TenGaDi} → {kq.TenGaDen} · {kq.CuLyKm.ToString("N0", Vi)} km";
            txtKqPhu.Text = $"Theo {(nguon == "AI" ? "yêu cầu AI đọc từ cuộc trò chuyện" : "lọc nâng cao")}: {yc.SoNguoi} khách, ngày {yc.NgayDi:dd/MM/yyyy} · " +
                            $"đã xét {kq.SoPhuongAnDaXet} phương án (chuyến × loại chỗ × tầng) trên {kq.SoChuyenDaXet} chuyến · trọng số " +
                            string.Join(" · ", kq.TrongSoDaDung.Select((w, i) => $"{QuyTacTuVanVe.TenTieuChi[i].ToLower(Vi)} {w:P0}"));
            icGhiChu.ItemsSource = kq.GhiChu;
            VeLaiThe();
        }

        private void VeLaiThe()
        {
            if (_kqHienTai == null) return;
            icPhuongAn.ItemsSource = _kqHienTai.CacPhuongAn
                .Select(p => new TheHienThi(p, _choPhepChon, _coQuyenBanVe, _kqHienTai.TrongSoDaDung,
                                            _daDatThe.TryGetValue(p.XepHang, out var d) ? d : null))
                .ToList();
        }

        private void BtnChon_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is not TheHienThi the || _ycHienTai == null) return;
            var p = the.Goc;
            PhuongAn = new PhuongAnDuocChon(p.MaChuyenTau, _ycHienTai.MaGaDi, _ycHienTai.MaGaDen, _ycHienTai.NgayDi,
                                            p.MaToaXeKhach, p.LoaiToa, p.CacMaChoNgoi, p.CacLoaiKhach);
            DialogResult = true;
        }

        // --- Trong so ----------------------------------------------------------
        private void ChkTuChinh_Changed(object sender, RoutedEventArgs e)
        {
            pnlTrongSo.IsEnabled = chkTuChinh.IsChecked == true;
            txtGoiYTrongSo.Text = chkTuChinh.IsChecked == true
                ? "Kéo thanh trượt — trọng số được chuẩn hóa để tổng = 100%."
                : "Để trống: AI tự gán trọng số theo hồ sơ khách (ngân sách, người cao tuổi, khung giờ).";
            CapNhatNhanTrongSo();
        }

        private void TrongSo_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => CapNhatNhanTrongSo();

        private void HienTrongSo(double[] w)
        {
            sldW0.Value = w[0] * 100; sldW1.Value = w[1] * 100; sldW2.Value = w[2] * 100; sldW3.Value = w[3] * 100;
            CapNhatNhanTrongSo();
        }

        private void CapNhatNhanTrongSo()
        {
            if (sldW0 == null || sldW1 == null || sldW2 == null || sldW3 == null || txtW3 == null) return;   // dang dung cay XAML
            var w = QuyTacTuVanVe.ChuanHoa(new[] { sldW0.Value, sldW1.Value, sldW2.Value, sldW3.Value });
            txtW0.Text = $"{w[0]:P0}"; txtW1.Text = $"{w[1]:P0}"; txtW2.Text = $"{w[2]:P0}"; txtW3.Text = $"{w[3]:P0}";
        }

        private void ChiNhapSo(object sender, TextCompositionEventArgs e) => e.Handled = !e.Text.All(char.IsDigit);

        private void BtnDong_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { e.Handled = true; DialogResult = false; }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                if (rdoHoiAI.IsChecked == true && _chat.SanSang) BtnGui_Click(this, new RoutedEventArgs());
                else BtnTuVan_Click(this, new RoutedEventArgs());
            }
        }

        // --- Bong bong chat --------------------------------------------------------------
        private sealed class TinNhanHienThi
        {
            public string NoiDung { get; init; } = "";
            public HorizontalAlignment CanLe { get; init; } = HorizontalAlignment.Left;
            public Brush MauNen { get; init; } = Brushes.White;
            public Brush MauVien { get; init; } = Brushes.Transparent;
            public Brush MauChu { get; init; } = Brushes.Black;
            public List<string> CacBuoc { get; init; } = new();
            public string CanhBao { get; init; } = "";
            public Visibility HienBongBong => string.IsNullOrEmpty(NoiDung) ? Visibility.Collapsed : Visibility.Visible;
            public Visibility HienCanhBao => string.IsNullOrEmpty(CanhBao) ? Visibility.Collapsed : Visibility.Visible;

            public static TinNhanHienThi NguoiDung(string s) => new()
            {
                NoiDung = s, CanLe = HorizontalAlignment.Right, MauNen = Mau("#0369A1"), MauVien = Mau("#0369A1"), MauChu = Brushes.White
            };
            public static TinNhanHienThi TroLy(string s, List<string> buoc, string canhBao) => new()
            {
                NoiDung = s, MauNen = Mau("#F1F5F9"), MauVien = Mau("#E2E8F0"), MauChu = Mau("#0F172A"), CacBuoc = buoc, CanhBao = canhBao
            };
            public static TinNhanHienThi Loi(string s) => new()
            {
                NoiDung = s, MauNen = Mau("#FEF2F2"), MauVien = Mau("#FCA5A5"), MauChu = Mau("#991B1B")
            };
            public static TinNhanHienThi HeThong(string s) => new()
            {
                NoiDung = s, MauNen = Mau("#F0FDF4"), MauVien = Mau("#86EFAC"), MauChu = Mau("#166534")
            };
        }

        // --- The ket qua ---------------------------------------------------------
        private sealed class TheHienThi
        {
            public TheHienThi(PhuongAnVe p, bool choPhepChon, bool coQuyenBanVe, double[] w, KetQuaDatVeHo? daDat)
            {
                Goc = p;
                DaDat = daDat;
                HienNutChon = choPhepChon && daDat == null ? Visibility.Visible : Visibility.Collapsed;
                HienNutDatVe = coQuyenBanVe && daDat == null ? Visibility.Visible : Visibility.Collapsed;
                TieuChi = Enumerable.Range(0, 4).Select(i => new TieuChiHienThi(
                    $"{TenNgan[i]} ({w[i]:P0})", p.DiemTieuChi[i])).ToList();
            }
            public PhuongAnVe Goc { get; }
            public KetQuaDatVeHo? DaDat { get; }
            public Visibility HienNutChon { get; }
            public Visibility HienNutDatVe { get; }
            public Visibility HienDaDat => DaDat != null ? Visibility.Visible : Visibility.Collapsed;
            public string NhanDaDat => DaDat == null ? "" : $"ĐÃ ĐẶT · đơn {DaDat.MaPnr} · {DaDat.SoVe} vé · {DaDat.TongTien.ToString("N0", Vi)} đ";
            public List<TieuChiHienThi> TieuChi { get; }
            public List<string> LyDo => Goc.LyDo;
            public string NhanHang => Goc.XepHang == 1 ? "KHUYÊN DÙNG" : $"PHƯƠNG ÁN {Goc.XepHang}";
            public Brush MauHang => Goc.XepHang == 1 ? Mau("#15803D") : Mau("#0284C7");
            public Brush MauVien => DaDat != null ? Mau("#86EFAC") : Goc.XepHang == 1 ? Mau("#16A34A") : Mau("#CBD5E1");
            public Thickness DoDayVien => Goc.XepHang == 1 ? new Thickness(2) : new Thickness(1);
            public string TieuDe => $"{Goc.SoHieuMacTau} · {Goc.TenLoaiCho}";
            public string DongGio => $"Đi {Goc.GioDi:HH:mm dd/MM} → đến {Goc.GioDen:HH:mm dd/MM} · khoảng {Goc.SoGioChay:0.#} giờ" +
                                     (Goc.QuaDem ? " · chạy qua đêm" : "");
            public string DongGhe => Goc.NhanGhe;
            public string NhanDiem => $"{Goc.Diem:0.00}/10";
            public string NhanGia => $"{Goc.GiaMotVe.ToString("N0", Vi)} đ/vé";
            // [12/11] Tong tien da tru giam gia theo doi tuong (gio ve dat san loai khach giong the nay)
            public string NhanTongTien => Goc.CacMaChoNgoi.Count > 1
                    ? $"{Goc.CacMaChoNgoi.Count} vé: {Goc.TongTien.ToString("N0", Vi)} đ" + (Goc.CoGiamGia ? " (đã giảm)" : "") +
                      (Goc.VuotNganSach ? " · vượt ngân sách" : "")
                : Goc.CoGiamGia ? $"sau giảm {Goc.TongTien.ToString("N0", Vi)} đ" + (Goc.VuotNganSach ? " · vượt ngân sách" : "")
                : Goc.VuotNganSach ? "vượt ngân sách" : "";
        }

        private static readonly string[] TenNgan = { "Tiện nghi", "Ngân sách", "Giờ / ngủ đêm", "Sức khỏe" };

        private sealed record TieuChiHienThi(string Ten, double Diem)
        {
            public double DoRong => Math.Max(0, Diem) * 8.5;      // thanh 85 px = 10 diem
            public string NhanDiem => Diem.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static SolidColorBrush Mau(string ma) => (SolidColorBrush)new BrushConverter().ConvertFrom(ma)!;
    }
}
