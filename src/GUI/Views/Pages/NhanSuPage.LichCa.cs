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
    // TAB 4: LICH CA TRUC - 3 ngay (hom truoc, ngay chon, hom sau) theo nguoi.
    // Moi ca = doan [gio nhan ban, gio ban giao]; sau moi ca la vung nghi toi
    // thieu 8 gio (R4). Ca vi pham (R3 R4 R6 R7 R8) vien do.
    // =========================================================================
    public partial class NhanSuPage
    {
        private DateTime _ngayGiuaLich = DateTime.Today;

        private const double RongCotTen = 196;
        private const double CaoHangLich = 36;

        private void BtnNgayTruoc_Click(object sender, RoutedEventArgs e)
        {
            _ngayGiuaLich = _ngayGiuaLich.AddDays(-1);
            VeLichCa();
        }

        private void BtnHomNay_Click(object sender, RoutedEventArgs e)
        {
            _ngayGiuaLich = DateTime.Today;
            VeLichCa();
        }

        private void BtnNgaySau_Click(object sender, RoutedEventArgs e)
        {
            _ngayGiuaLich = _ngayGiuaLich.AddDays(1);
            VeLichCa();
        }

        private void BoLocLich_Changed(object sender, RoutedEventArgs e)
        {
            if (ChuaSanSang) return;
            VeLichCa();
        }

        private void SvLich_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged) VeLichCa();
        }

        private void VeLichCa()
        {
            if (_duLieu == null) return;

            DateTime tu = _ngayGiuaLich.AddDays(-1);
            DateTime den = _ngayGiuaLich.AddDays(2);
            txtKhoangNgay.Text = $"{tu:dd/MM} – {den.AddDays(-1):dd/MM/yyyy}";

            cvLich.Children.Clear();
            cvLichTieuDe.Children.Clear();

            double rong = svLich.ViewportWidth > 0 ? svLich.ViewportWidth : svLich.ActualWidth;
            if (rong < RongCotTen + 200) return;

            double rongTruc = rong - RongCotTen - 12;
            double tongGio = (den - tu).TotalHours;
            double X(DateTime t) => RongCotTen + Math.Clamp((t - tu).TotalHours, 0, tongGio) / tongGio * rongTruc;

            string donVi = LayTag(cboDonViLich) ?? "ALL";
            string chucDanh = LayTag(cboChucDanhLich) ?? "ALL";
            bool chiCoCa = chkChiCoCa.IsChecked == true;

            var hang = DuLieu.NhanVien
                .Where(n => n.LaToTau && n.TrangThai != NhanVienHienThi.DaNghiViec
                            && (donVi == "ALL" || n.DonViChuQuan == donVi)
                            && (chucDanh == "ALL" || n.ChucDanh == chucDanh))
                .Select(n => (NhanVien: n, Kip: DuLieu.KipCuaNhanVien(n.MaNhanVien).OrderBy(p => p.GioNhanBan).ToList()))
                .Where(x => !chiCoCa || x.Kip.Any(p => p.GioBanGiao > tu && p.GioNhanBan < den))
                .OrderBy(x => x.NhanVien.DonViChuQuan)
                .ThenBy(x => ThuTuChucDanh(x.NhanVien.ChucDanh))
                .ThenBy(x => x.NhanVien.MaNVCode)
                .ToList();

            txtLichTrong.Visibility = hang.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            VeTieuDeLich(tu, den, rong, X);

            double cao = Math.Max(hang.Count * CaoHangLich, 1);
            cvLich.Width = rong;
            cvLich.Height = cao;

            // --- Nen hang + cot ten ---
            string? donViTruoc = null;
            for (int i = 0; i < hang.Count; i++)
            {
                var nv = hang[i].NhanVien;
                double y = i * CaoHangLich;

                ThemVao(cvLich, new Rectangle { Width = rong, Height = CaoHangLich, Fill = Mau(i % 2 == 0 ? "#FFFFFF" : "#F8FAFC") }, 0, y);
                if (donViTruoc != null && donViTruoc != nv.DonViChuQuan)
                    ThemVao(cvLich, new Rectangle { Width = rong, Height = 1, Fill = Mau("#94A3B8") }, 0, y);
                donViTruoc = nv.DonViChuQuan;

                var ten = new StackPanel { Width = RongCotTen - 18, Cursor = Cursors.Hand, Background = Brushes.Transparent, ToolTip = "Mở hồ sơ" };
                ten.Children.Add(new TextBlock
                {
                    Text = nv.HoTen, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Mau("#0F172A"),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                ten.Children.Add(new TextBlock
                {
                    Text = nv.LaBanLaiMay ? $"{nv.ChucDanh} · {nv.NhanBangLai} · {nv.DonViChuQuan}" : $"{nv.ChucDanh} · {nv.DonViChuQuan}",
                    FontSize = 10, Foreground = Mau("#64748B"), TextTrimming = TextTrimming.CharacterEllipsis
                });
                int maNhanVien = nv.MaNhanVien;
                ten.MouseLeftButtonUp += (_, _) => MoHoSo(maNhanVien);
                ThemVao(cvLich, ten, 10, y + 3);
            }

            // --- Luoi doc: moc 6 gio, vach ngay dam hon ---
            ThemVao(cvLich, new Rectangle { Width = 1, Height = cao, Fill = Mau("#CBD5E1") }, RongCotTen, 0);
            for (DateTime t = tu; t <= den; t = t.AddHours(6))
            {
                ThemVao(cvLich, new Rectangle { Width = 1, Height = cao, Fill = Mau(t.Hour == 0 ? "#CBD5E1" : "#EEF2F6") }, X(t), 0);
            }

            // --- Vung nghi toi thieu + ca ---
            var nenNghi = (Brush)FindResource("NsNenNghi");
            for (int i = 0; i < hang.Count; i++)
            {
                var (nv, kip) = hang[i];
                double y = i * CaoHangLich;

                foreach (var p in kip)
                {
                    DateTime nghiDen = p.GioBanGiao.AddHours((double)QuyTacKipLai.SoGioNghiToiThieu);
                    if (nghiDen > tu && p.GioBanGiao < den)
                    {
                        ThemVao(cvLich, new Rectangle
                        {
                            Width = Math.Max(X(nghiDen) - X(p.GioBanGiao), 0), Height = 7, Fill = nenNghi,
                            ToolTip = $"Nghỉ tối thiểu sau ca {p.Chuyen.SoHieuMacTau}: tới {nghiDen:HH:mm dd/MM}"
                        }, X(p.GioBanGiao), y + CaoHangLich - 10);
                    }
                }

                foreach (var p in kip.Where(p => p.GioBanGiao > tu && p.GioNhanBan < den))
                    ThemVao(cvLich, TaoThanhCa(nv, p, Math.Max(X(p.GioBanGiao) - X(p.GioNhanBan), 4)), X(p.GioNhanBan), y + 6);
            }

            // --- Vach hien tai ---
            if (DuLieu.BayGio > tu && DuLieu.BayGio < den)
            {
                double xNow = X(DuLieu.BayGio);
                ThemVao(cvLich, new Rectangle { Width = 2, Height = cao, Fill = Mau("#DC2626"), Opacity = 0.8 }, xNow - 1, 0);
                ThemVao(cvLichTieuDe, new Rectangle { Width = 2, Height = 36, Fill = Mau("#DC2626"), Opacity = 0.8 }, xNow - 1, 0);
            }
        }

        private void VeTieuDeLich(DateTime tu, DateTime den, double rong, Func<DateTime, double> X)
        {
            ThemVao(cvLichTieuDe, new Rectangle { Width = rong, Height = 36, Fill = Mau("#F1F5F9") }, 0, 0);
            ThemVao(cvLichTieuDe, new Rectangle { Width = rong, Height = 1, Fill = Mau("#CBD5E1") }, 0, 35);
            ThemVao(cvLichTieuDe, new TextBlock
            {
                Text = "NHÂN VIÊN", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Mau("#0F172A")
            }, 10, 10);

            for (DateTime ngay = tu; ngay < den; ngay = ngay.AddDays(1))
            {
                double x1 = X(ngay), x2 = X(ngay.AddDays(1));
                bool homNay = ngay == DateTime.Today;
                ThemVao(cvLichTieuDe, new Rectangle { Width = 1, Height = 36, Fill = Mau("#CBD5E1") }, x1, 0);
                ThemVao(cvLichTieuDe, new TextBlock
                {
                    Text = homNay ? $"{ngay:dddd dd/MM} · hôm nay" : ngay.ToString("dddd dd/MM"),
                    FontSize = 11, FontWeight = homNay ? FontWeights.Bold : FontWeights.SemiBold,
                    Foreground = Mau(homNay ? "#003B73" : "#334155"),
                    Width = x2 - x1, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis
                }, x1, 3);

                for (int gio = 6; gio < 24; gio += 6)
                {
                    double x = X(ngay.AddHours(gio));
                    ThemVao(cvLichTieuDe, new Rectangle { Width = 1, Height = 5, Fill = Mau("#94A3B8") }, x, 31);
                    ThemVao(cvLichTieuDe, new TextBlock
                    {
                        Text = $"{gio:00}h", FontSize = 9, Foreground = Mau("#64748B"), Width = 30, TextAlignment = TextAlignment.Center
                    }, x - 15, 19);
                }
            }
        }

        private Border TaoThanhCa(NhanVienHienThi nv, PhanCongKipHienThi p, double rong)
        {
            string vaiTro = p.VaiTroCua(nv.MaNhanVien) ?? QuyTacKipLai.LaiTau;

            // Chi kip chua nhan ban moi doi chieu lai dieu kien
            var loi = p.TrangThai == PhanCongKipHienThi.DaPhanCong
                ? DanhGiaKipLai.DoiChieu(DuLieu, nv, vaiTro, p.Chuyen, p.MaGaNhanBan, p.MaGaBanGiao, p.MaPhanCong)
                               .Where(d => d.LaLoi).ToList()
                : new List<DieuKienUngVien>();

            (string nen, string vien, string chu) = p.TrangThai switch
            {
                PhanCongKipHienThi.DangThucHien => ("#2563EB", "#1D4ED8", "#FFFFFF"),
                PhanCongKipHienThi.HoanThanh => ("#E2E8F0", "#94A3B8", "#334155"),
                _ => ("#DBEAFE", "#60A5FA", "#1E3A8A")
            };
            if (loi.Count > 0) (nen, vien, chu) = ("#FEF2F2", "#DC2626", "#991B1B");

            bool quaGioBanGiao = p.TrangThai == PhanCongKipHienThi.DangThucHien && DuLieu.BayGio > p.GioBanGiao;
            if (quaGioBanGiao) vien = "#F59E0B";

            string tip = $"{p.Chuyen.SoHieuMacTau} · {QuyTacKipLai.TenVaiTro(vaiTro)} · {p.NhanTrangThai}\n{p.Chang}\n{p.KhungGio}";
            if (quaGioBanGiao) tip += $"\n\nĐã quá giờ bàn giao dự kiến {p.GioBanGiao:HH:mm dd/MM}, chưa xác nhận bàn giao.";
            if (loi.Count > 0) tip += "\n\n" + string.Join("\n", loi.Select(l => "• " + l.MoTa));

            var thanh = new Border
            {
                Width = rong, Height = 17,
                Background = Mau(nen), BorderBrush = Mau(vien),
                BorderThickness = new Thickness(loi.Count > 0 || quaGioBanGiao ? 2 : 1),
                CornerRadius = new CornerRadius(2),
                Cursor = Cursors.Hand,
                ToolTip = tip,
                Child = new TextBlock
                {
                    Text = $"{p.Chuyen.SoHieuMacTau} {p.TenGaNhanBan} → {p.TenGaBanGiao}",
                    FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = Mau(chu),
                    Margin = new Thickness(4, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                }
            };
            thanh.MouseLeftButtonUp += (_, e) =>
            {
                e.Handled = true;
                MoKip(p.MaChuyenTau, p.MaPhanCong);
            };
            return thanh;
        }

        private static int ThuTuChucDanh(string chucDanh) => chucDanh switch
        {
            "Lái tàu" => 0,
            "Phụ lái" => 1,
            _ => 2
        };

        private static void ThemVao(Canvas cv, UIElement phanTu, double x, double y)
        {
            Canvas.SetLeft(phanTu, x);
            Canvas.SetTop(phanTu, y);
            cv.Children.Add(phanTu);
        }

        // Mo ho so tu bieu do ca: xoa loc neu ho so dang bi an
        private void MoHoSo(int maNhanVien)
        {
            tabNhanSu.SelectedIndex = 0;
            if (ChonNhanVien(maNhanVien)) return;

            BtnXoaLocNhanVien_Click(this, new RoutedEventArgs());
            ChonNhanVien(maNhanVien);
        }
    }
}
