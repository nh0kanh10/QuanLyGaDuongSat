using GUI.ViewModels.KyThuat;
using GUI.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using DTO.Common;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // DIALOG GHI KET QUA KIEM TRA DIEU KIEN LEN BAN (nhansu.KiemTraSucKhoe)
    //
    // Mot dong / thanh vien kip. Nguoi da co ket qua cho chuyen nay chi hien lai
    // (khong sua). Ket luan tinh y cot PERSISTED DuDieuKien:
    //   NongDoConMgL = 0.00 AND SoGioNghiTruocCa >= 8.0
    // Gio nghi dien san tu lich phan cong (gio ban giao ca truoc -> gio nhan ban).
    // =========================================================================
    public partial class KiemTraLenBanDialog : Window
    {
        private sealed class DongNhap
        {
            public string VaiTro = "";
            public NhanVienHienThi NhanVien = null!;
            public KetQuaKiemTraNguoi KetQuaCu = null!;
            public CheckBox? Chk;
            public TextBox? Con;
            public TextBox? Nghi;
            public TextBlock KetLuan = null!;

            public bool CoTheNhap => KetQuaCu.ChuaKiemTra;
            public bool DuocChon => CoTheNhap && Chk?.IsChecked == true;
        }

        private readonly DuLieuNhanSu _duLieu;
        private readonly PhanCongKipHienThi _kip;
        private readonly List<DongNhap> _dong = new();

        public List<KiemTraLenBanHienThi> KetQua { get; } = new();

        public KiemTraLenBanDialog(DuLieuNhanSu duLieu, PhanCongKipHienThi kip, int soThuTuKip)
        {
            InitializeComponent();
            _duLieu = duLieu;
            _kip = kip;

            var ct = kip.Chuyen;
            txtTieuDe.Text = $"KIỂM TRA ĐIỀU KIỆN LÊN BAN — {ct.SoHieuMacTau} · KÍP {soThuTuKip}";
            txtTieuDePhu.Text = $"{kip.Chang} · {kip.KhungGio} · đo nồng độ cồn và đối chiếu giờ nghỉ trước khi nhận lệnh lái tàu";

            double phutConLai = (kip.GioNhanBan - duLieu.BayGio).TotalMinutes;
            txtGioNhanBan.Text = $"Nhận ban {kip.GioNhanBan:HH:mm dd/MM} tại {kip.TenGaNhanBan} · " +
                                 (phutConLai >= 0 ? $"còn {DinhDangThoiLuong(phutConLai)}" : $"đã qua {DinhDangThoiLuong(-phutConLai)}");
            txtGioNhanBan.Foreground = Mau(phutConLai >= 0 ? "#334155" : "#B45309");

            TaoBang();
            CapNhatKetLuan();

            Loaded += (_, _) =>
            {
                var dau = _dong.FirstOrDefault(d => d.CoTheNhap);
                dau?.Con?.Focus();
            };
        }

        private static string DinhDangThoiLuong(double phut)
        {
            int gio = (int)(phut / 60), p = (int)(phut % 60);
            return gio > 0 ? $"{gio} giờ {p:00} phút" : $"{p} phút";
        }

        // =====================================================================
        // BANG NHAP
        // =====================================================================

        private void TaoBang()
        {
            var g = gridNhap;
            double[] rong = { 34, 78, -1, 178, 96, 96, 132 };   // -1 = co gian
            foreach (double w in rong)
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = w < 0 ? new GridLength(1, GridUnitType.Star) : new GridLength(w) });

            // Tieu de
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Dat(new Border { Background = Mau("#F1F5F9"), BorderBrush = Mau("#CBD5E1"), BorderThickness = new Thickness(0, 0, 0, 1) }, 0, 0, 7);
            string[] tieuDe = { "Ghi", "Vị trí", "Nhân viên", "Ca trước (theo lịch)", "Nồng độ cồn (mg/L)", "Nghỉ trước ca (giờ)", "Kết luận" };
            for (int c = 0; c < tieuDe.Length; c++)
            {
                Dat(new TextBlock
                {
                    Text = tieuDe[c], FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Mau("#334155"),
                    Margin = new Thickness(c == 0 ? 8 : 6, 6, 4, 6), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center
                }, 0, c);
            }

            int hang = 1;
            foreach (string vaiTro in QuyTacKipLai.CacVaiTro)
            {
                var nv = _duLieu.TimNhanVien(_kip.MaNhanVienTheoVaiTro(vaiTro));
                if (nv == null) continue;

                var d = new DongNhap
                {
                    VaiTro = vaiTro,
                    NhanVien = nv,
                    KetQuaCu = _duLieu.KetQuaKiemTra(nv.MaNhanVien, _kip.MaChuyenTau)
                };

                g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
                if (hang > 1)
                    Dat(new Border { BorderBrush = Mau("#E2E8F0"), BorderThickness = new Thickness(0, 1, 0, 0) }, hang, 0, 7);

                // Vi tri + nhan vien
                Dat(new TextBlock { Text = QuyTacKipLai.TenVaiTro(vaiTro), FontSize = 11, Foreground = Mau("#475569"),
                                    Margin = new Thickness(6, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center }, hang, 1);
                var tbNv = new TextBlock { FontSize = 11, Margin = new Thickness(6, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center,
                                           TextTrimming = TextTrimming.CharacterEllipsis };
                tbNv.Inlines.Add(new Run(nv.HoTen) { FontWeight = FontWeights.SemiBold, Foreground = Mau("#0F172A") });
                tbNv.Inlines.Add(new LineBreak());
                tbNv.Inlines.Add(new Run($"{nv.MaNVCode} · {nv.DonViChuQuan}") { FontSize = 10, Foreground = Mau("#64748B") });
                Dat(tbNv, hang, 2);

                // Ca truoc theo lich phan cong
                var caTruoc = _duLieu.CaTruoc(nv.MaNhanVien, _kip.Chuyen, _kip.GioNhanBan, _kip.MaPhanCong);
                var tbCa = new TextBlock { FontSize = 10, Foreground = Mau("#475569"), Margin = new Thickness(6, 0, 4, 0),
                                           VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
                tbCa.Text = caTruoc != null
                    ? $"{caTruoc.Chuyen.SoHieuMacTau} · giao {caTruoc.GioBanGiao:HH:mm dd/MM} tại {caTruoc.TenGaBanGiao}"
                    : d.CoTheNhap ? "Không có ca trước trong lịch — nhập theo sổ giao ca" : "Không có ca trước trong lịch";
                Dat(tbCa, hang, 3);

                d.KetLuan = new TextBlock { FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(6, 0, 6, 0),
                                            VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
                Dat(d.KetLuan, hang, 6);

                if (d.CoTheNhap)
                {
                    d.Chk = new CheckBox { IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(9, 0, 0, 0),
                                           ToolTip = "Bỏ chọn nếu người này chưa có mặt để đo" };
                    d.Chk.Checked += (_, _) => DoiChon(d);
                    d.Chk.Unchecked += (_, _) => DoiChon(d);
                    Dat(d.Chk, hang, 0);

                    d.Con = new TextBox { MaxLength = 4, Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center,
                                          ToolTip = "Nồng độ cồn trong khí thở, 2 chữ số thập phân (ví dụ 0.00)" };
                    ONhapSoThucHelper.Gan(d.Con, 2);
                    d.Con.TextChanged += (_, _) => { txtLoi.Text = ""; CapNhatKetLuan(); };
                    Dat(d.Con, hang, 4);

                    d.Nghi = new TextBox { MaxLength = 5, Margin = new Thickness(6, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center,
                                           ToolTip = "Số giờ nghỉ liên tục trước ca, 1 chữ số thập phân" };
                    ONhapSoThucHelper.Gan(d.Nghi, 1);
                    if (caTruoc != null)
                    {
                        decimal gio = Math.Min(QuyTacKipLai.SoGio(caTruoc.GioBanGiao, _kip.GioNhanBan), QuyTacKipLai.SoGioNghiGioiHan);
                        d.Nghi.Text = gio.ToString("0.0", FormatHelper.TechnicalCulture);
                    }
                    d.Nghi.TextChanged += (_, _) => { txtLoi.Text = ""; CapNhatKetLuan(); };
                    Dat(d.Nghi, hang, 5);
                }
                else
                {
                    // Da co ket qua cho chuyen nay: hien lai, khong sua
                    var kt = d.KetQuaCu.LanKhongDat ?? d.KetQuaCu.MoiNhat!;
                    Dat(new TextBlock { Text = $"{kt.NongDoConMgL:0.00}", FontSize = 11, FontWeight = FontWeights.SemiBold,
                                        Foreground = Mau(kt.MauNongDoCon), Margin = new Thickness(12, 0, 6, 0),
                                        VerticalAlignment = VerticalAlignment.Center }, hang, 4);
                    Dat(new TextBlock { Text = $"{kt.SoGioNghiTruocCa:0.0}", FontSize = 11, FontWeight = FontWeights.SemiBold,
                                        Foreground = Mau(kt.MauGioNghi), Margin = new Thickness(12, 0, 6, 0),
                                        VerticalAlignment = VerticalAlignment.Center }, hang, 5);
                }

                _dong.Add(d);
                hang++;
            }
        }

        private void Dat(UIElement e, int hang, int cot, int soCot = 1)
        {
            Grid.SetRow(e, hang);
            Grid.SetColumn(e, cot);
            Grid.SetColumnSpan(e, soCot);
            gridNhap.Children.Add(e);
        }

        private void DoiChon(DongNhap d)
        {
            bool chon = d.Chk?.IsChecked == true;
            if (d.Con != null) d.Con.IsEnabled = chon;
            if (d.Nghi != null) d.Nghi.IsEnabled = chon;
            txtLoi.Text = "";
            CapNhatKetLuan();
        }

        // =====================================================================
        // KET LUAN TRUC TIEP
        // =====================================================================

        // true dat / false khong dat / null chua du so lieu
        private static bool? DanhGia(DongNhap d)
        {
            if (!d.CoTheNhap) return d.KetQuaCu.DaDat;
            if (!d.DuocChon) return null;
            decimal? con = ONhapSoThucHelper.Lay(d.Con!);
            decimal? nghi = ONhapSoThucHelper.Lay(d.Nghi!);
            if (!con.HasValue || !nghi.HasValue) return null;
            return QuyTacKipLai.DuDieuKienLenBan(con.Value, nghi.Value);
        }

        private void CapNhatKetLuan()
        {
            foreach (var d in _dong)
            {
                if (!d.CoTheNhap)
                {
                    var kt = d.KetQuaCu.LanKhongDat ?? d.KetQuaCu.MoiNhat!;
                    DatKetLuanDong(d, d.KetQuaCu.KhongDat
                        ? ($"Không đạt lúc {kt.ThoiDiemKiemTra:HH:mm} — phải thay người", "#B91C1C")
                        : ($"Đạt lúc {kt.ThoiDiemKiemTra:HH:mm} (đã ghi)", "#15803D"));
                    continue;
                }
                if (!d.DuocChon)
                {
                    DatKetLuanDong(d, ("Không ghi lần này", "#94A3B8"));
                    continue;
                }

                decimal? con = ONhapSoThucHelper.Lay(d.Con!);
                decimal? nghi = ONhapSoThucHelper.Lay(d.Nghi!);
                if (!con.HasValue || !nghi.HasValue)
                {
                    DatKetLuanDong(d, ("Chờ nhập", "#94A3B8"));
                    continue;
                }

                var lyDo = new List<string>();
                if (con.Value != QuyTacKipLai.NongDoConChoPhep) lyDo.Add("có cồn");
                if (nghi.Value < QuyTacKipLai.SoGioNghiToiThieu) lyDo.Add($"nghỉ < {QuyTacKipLai.SoGioNghiToiThieu:0} giờ");
                DatKetLuanDong(d, lyDo.Count == 0 ? ("Đủ điều kiện", "#15803D") : ($"Không đạt: {string.Join(", ", lyDo)}", "#B91C1C"));
            }

            // Tong hop sau khi luu
            var ketQua = _dong.Select(d => (Dong: d, Dat: DanhGia(d))).ToList();
            var khongDat = ketQua.Where(x => x.Dat == false).Select(x => $"{QuyTacKipLai.TenVaiTro(x.Dong.VaiTro).ToLower()} {x.Dong.NhanVien.HoTen}").ToList();
            int soDat = ketQua.Count(x => x.Dat == true);

            if (khongDat.Count > 0)
                DatTomTat("#FEF2F2", "#FCA5A5", "#B91C1C", "SAU KHI GHI: CẦN THAY NGƯỜI",
                          $"Không đạt: {string.Join(", ", khongDat)}. Người không đạt không được nhận ban chuyến này.");
            else if (soDat == _dong.Count && _dong.Count == 3)
                DatTomTat("#F0FDF4", "#86EFAC", "#15803D", "SAU KHI GHI: CẢ KÍP ĐẠT KIỂM TRA",
                          "Có thể xác nhận nhận ban nếu kíp không còn lỗi phân công.");
            else
                DatTomTat("#FFFBEB", "#FDE68A", "#B45309", $"SAU KHI GHI: {soDat}/{_dong.Count} NGƯỜI ĐẠT",
                          "Kíp chỉ được nhận ban khi cả 3 người có kết quả đạt.");
        }

        private static void DatKetLuanDong(DongNhap d, (string Chu, string Mau) kl)
        {
            d.KetLuan.Text = kl.Chu;
            d.KetLuan.Foreground = Mau(kl.Mau);
        }

        private void DatTomTat(string nen, string vien, string chu, string tieuDe, string moTa)
        {
            bdTomTat.Background = Mau(nen);
            bdTomTat.BorderBrush = Mau(vien);
            txtTomTat.Text = tieuDe;
            txtTomTat.Foreground = Mau(chu);
            txtTomTatPhu.Text = moTa;
        }

        // =====================================================================
        // LUU / HUY
        // =====================================================================

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            var chon = _dong.Where(d => d.DuocChon).ToList();
            if (chon.Count == 0)
            {
                txtLoi.Text = _dong.Any(d => d.CoTheNhap)
                    ? "Chọn ít nhất một người để ghi kết quả."
                    : "Cả kíp đã có kết quả kiểm tra cho chuyến này, không còn gì để ghi.";
                return;
            }

            var loi = new List<string>();
            Control? oLoi = null;
            var thoiDiem = DateTime.Now;
            var ketQua = new List<KiemTraLenBanHienThi>();

            foreach (var d in chon)
            {
                string ten = d.NhanVien.HoTen;
                decimal? con = ONhapSoThucHelper.Lay(d.Con!);
                decimal? nghi = ONhapSoThucHelper.Lay(d.Nghi!);
                DatVienLoi(d.Con!, false);
                DatVienLoi(d.Nghi!, false);

                // NongDoConMgL DECIMAL(3,2) CHECK >= 0
                string? loiCon = !con.HasValue ? "chưa nhập nồng độ cồn"
                               : con < 0 || con > QuyTacKipLai.NongDoConGioiHan ? $"nồng độ cồn phải từ 0 đến {QuyTacKipLai.NongDoConGioiHan:0.00}"
                               : ONhapSoThucHelper.QuaSoChuSoLe(con.Value, 2) ? "nồng độ cồn chỉ ghi 2 chữ số thập phân"
                               : null;
                // SoGioNghiTruocCa DECIMAL(4,1) CHECK >= 0
                string? loiNghi = !nghi.HasValue ? "chưa nhập giờ nghỉ"
                                : nghi < 0 || nghi > QuyTacKipLai.SoGioNghiGioiHan ? $"giờ nghỉ phải từ 0 đến {QuyTacKipLai.SoGioNghiGioiHan:0.0}"
                                : ONhapSoThucHelper.QuaSoChuSoLe(nghi.Value, 1) ? "giờ nghỉ chỉ ghi 1 chữ số thập phân"
                                : null;

                if (loiCon != null) { loi.Add($"{ten}: {loiCon}"); DatVienLoi(d.Con!, true); oLoi ??= d.Con; }
                if (loiNghi != null) { loi.Add($"{ten}: {loiNghi}"); DatVienLoi(d.Nghi!, true); oLoi ??= d.Nghi; }
                if (loiCon != null || loiNghi != null) continue;

                ketQua.Add(new KiemTraLenBanHienThi
                {
                    MaNhanVien = d.NhanVien.MaNhanVien,
                    MaChuyenTau = _kip.MaChuyenTau,
                    NongDoConMgL = con!.Value,
                    SoGioNghiTruocCa = nghi!.Value,
                    ThoiDiemKiemTra = thoiDiem
                });
            }

            if (loi.Count > 0)
            {
                txtLoi.Text = string.Join(" · ", loi) + ".";
                oLoi?.Focus();
                return;
            }

            KetQua.AddRange(ketQua);
            DialogResult = true;
        }

        private static void DatVienLoi(TextBox tb, bool loi)
        {
            if (loi) tb.BorderBrush = Mau("#DC2626");
            else tb.ClearValue(Control.BorderBrushProperty);
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                BtnLuu_Click(this, new RoutedEventArgs());
            }
        }

        private static SolidColorBrush Mau(string ma) => (SolidColorBrush)new BrushConverter().ConvertFrom(ma)!;
    }
}
