using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using GUI.Views.KyThuat.Models;

namespace GUI.Views.KyThuat.Dialogs
{
    // =========================================================================
    // DIALOG PHAN CONG / SUA / THAY NGUOI TRONG KIP LAI (nhansu.PhanCongKipLai)
    //
    // Chuyen co dinh (mo tu chuyen dang chon). Chon ga nhan ban -> ga ban giao,
    // 3 vi tri lai tau / phu lai / truong tau. Moi lan doi, doi chieu lai R1..R10
    // cho tung nguoi (DanhGiaKipLai.DoiChieu) va hien bang doi chieu ben phai.
    // Con loi nghiem trong thi khong luu duoc.
    // =========================================================================
    public partial class PhanCongKipDialog : Window
    {
        private readonly DuLieuNhanSu _duLieu;
        private readonly ChuyenTauNhanSu _chuyen;
        private readonly PhanCongKipHienThi? _banGoc;   // null = kip moi
        private readonly bool _dangKhoiTao;
        private bool _dangNapUngVien;

        public PhanCongKipHienThi? KetQua { get; private set; }

        public PhanCongKipDialog(DuLieuNhanSu duLieu, ChuyenTauNhanSu chuyen, PhanCongKipHienThi? banGoc,
                                 int? maGaNhanMacDinh, int? maGaGiaoMacDinh, string? vaiTroThay = null)
        {
            _dangKhoiTao = true;
            InitializeComponent();

            _duLieu = duLieu;
            _chuyen = chuyen;
            _banGoc = banGoc;

            txtChuyen.Text = chuyen.TenHienThi;
            txtChuyenPhu.Text = $"Đầu máy {chuyen.NhanDauMay} · xuất phát {chuyen.GioXuatPhatKH:HH:mm dd/MM} · " +
                                $"về đích {chuyen.GioVeDichKH:HH:mm dd/MM} · {chuyen.NhanTrangThai.ToLower()}" +
                                (chuyen.CoLichDungGa ? "" : " · chưa có lịch dừng ga");

            var diem = chuyen.LichDung;
            cboGaNhan.ItemsSource = diem.Take(diem.Count - 1).ToList();

            int gaNhan, gaGiao;
            if (banGoc == null)
            {
                txtTieuDe.Text = $"PHÂN CÔNG KÍP LÁI — {chuyen.SoHieuMacTau} · {chuyen.NgayXuatPhat:dd/MM/yyyy}";
                txtTieuDePhu.Text = "Chọn chặng và 3 thành viên kíp; điều kiện được đối chiếu ngay khi chọn";
                txtNutLuu.Text = "Lưu Phân Công";
                gaNhan = maGaNhanMacDinh ?? diem[0].MaGa;
                gaGiao = maGaGiaoMacDinh ?? diem[^1].MaGa;
            }
            else
            {
                int soThuTu = duLieu.SoThuTuKip(banGoc);
                txtTieuDe.Text = vaiTroThay == null
                    ? $"CẬP NHẬT KÍP {soThuTu} — {chuyen.SoHieuMacTau} · {chuyen.NgayXuatPhat:dd/MM/yyyy}"
                    : $"THAY {QuyTacKipLai.TenVaiTro(vaiTroThay).ToUpper()} — KÍP {soThuTu} · {chuyen.SoHieuMacTau}";
                txtTieuDePhu.Text = "Chỉ sửa được kíp chưa nhận ban";
                txtNutLuu.Text = "Lưu Thay Đổi";
                gaNhan = banGoc.MaGaNhanBan;
                gaGiao = banGoc.MaGaBanGiao;
            }

            cboGaNhan.SelectedItem = diem.FirstOrDefault(d => d.MaGa == gaNhan) ?? diem[0];
            NapGaGiao(gaGiao);
            NapUngVien();

            if (banGoc != null && vaiTroThay != null) HienLyDoThay(banGoc, vaiTroThay);

            _dangKhoiTao = false;
            CapNhatDoiChieu();

            Loaded += (_, _) =>
            {
                ComboBox oDau = vaiTroThay switch
                {
                    QuyTacKipLai.LaiTau => cboLaiTau,
                    QuyTacKipLai.PhuLai => cboPhuLai,
                    QuyTacKipLai.TruongTau => cboTruongTau,
                    _ => cboLaiTau.SelectedItem == null ? cboLaiTau : cboGaNhan
                };
                oDau.Focus();
            };
        }

        private DiemDungNhanSu? GaNhan => cboGaNhan.SelectedItem as DiemDungNhanSu;
        private DiemDungNhanSu? GaGiao => cboGaGiao.SelectedItem as DiemDungNhanSu;

        private ComboBox OChon(string vaiTro) => vaiTro switch
        {
            QuyTacKipLai.LaiTau => cboLaiTau,
            QuyTacKipLai.PhuLai => cboPhuLai,
            _ => cboTruongTau
        };

        private TextBlock OLoi(string vaiTro) => vaiTro switch
        {
            QuyTacKipLai.LaiTau => txtLoiLaiTau,
            QuyTacKipLai.PhuLai => txtLoiPhuLai,
            _ => txtLoiTruongTau
        };

        // Ga ban giao: cac ga sau ga nhan ban; giu lua chon neu con hop le
        private void NapGaGiao(int? maGaMuonChon)
        {
            int viTriNhan = GaNhan == null ? 0 : _chuyen.ViTri(GaNhan.MaGa);
            var ds = _chuyen.LichDung.Skip(viTriNhan + 1).ToList();
            int? giu = maGaMuonChon ?? GaGiao?.MaGa;

            cboGaGiao.ItemsSource = ds;
            cboGaGiao.SelectedItem = ds.FirstOrDefault(d => d.MaGa == giu) ?? ds.LastOrDefault();
        }

        // Nap lai danh sach ung vien theo chang dang chon, giu nguoi dang chon
        private void NapUngVien()
        {
            if (GaNhan == null || GaGiao == null) return;

            _dangNapUngVien = true;
            foreach (string vaiTro in QuyTacKipLai.CacVaiTro)
            {
                var cbo = OChon(vaiTro);
                int? maDangChon = (cbo.SelectedItem as UngVienKip)?.NhanVien.MaNhanVien
                                  ?? (cbo.ItemsSource == null ? _banGoc?.MaNhanVienTheoVaiTro(vaiTro) : null);

                var ds = DanhGiaKipLai.LayUngVien(_duLieu, vaiTro, _chuyen, GaNhan.MaGa, GaGiao.MaGa, _banGoc?.MaPhanCong);

                // Nguoi dang trong kip nhung khong con trong danh sach (da nghi viec / doi chuc danh): van hien de thay ly do
                if (maDangChon.HasValue && ds.All(u => u.NhanVien.MaNhanVien != maDangChon))
                {
                    var nv = _duLieu.TimNhanVien(maDangChon.Value);
                    if (nv != null)
                        ds.Add(new UngVienKip
                        {
                            NhanVien = nv,
                            DieuKien = DanhGiaKipLai.DoiChieu(_duLieu, nv, vaiTro, _chuyen, GaNhan.MaGa, GaGiao.MaGa, _banGoc?.MaPhanCong)
                        });
                }

                cbo.ItemsSource = ds;
                cbo.SelectedItem = ds.FirstOrDefault(u => u.NhanVien.MaNhanVien == maDangChon);
            }
            _dangNapUngVien = false;
        }

        private void HienLyDoThay(PhanCongKipHienThi kip, string vaiTro)
        {
            var nv = _duLieu.TimNhanVien(kip.MaNhanVienTheoVaiTro(vaiTro));
            if (nv == null) return;

            var kq = _duLieu.KetQuaKiemTra(nv.MaNhanVien, kip.MaChuyenTau);
            string lyDo = kq.KhongDat
                ? $"không đạt kiểm tra lên ban lúc {kq.LanKhongDat!.ThoiDiemKiemTra:HH:mm dd/MM} ({kq.LanKhongDat.LyDoKhongDat})"
                : DanhGiaKipLai.DoiChieu(_duLieu, nv, vaiTro, _chuyen, kip.MaGaNhanBan, kip.MaGaBanGiao, kip.MaPhanCong)
                               .FirstOrDefault(d => d.LaLoi)?.MoTa ?? "cần thay theo yêu cầu";

            txtThay.Text = $"Cần thay {QuyTacKipLai.TenVaiTro(vaiTro).ToLower()} {nv.HoTen}: {lyDo}. " +
                           $"Chọn người khác ở ô {QuyTacKipLai.TenVaiTro(vaiTro)} bên dưới.";
            bdThay.Visibility = Visibility.Visible;
        }

        // =====================================================================
        // THAY DOI LUA CHON
        // =====================================================================

        private void CboGaNhan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            NapGaGiao(null);
            NapUngVien();
            CapNhatDoiChieu();
        }

        private void CboGaGiao_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao) return;
            NapUngVien();
            CapNhatDoiChieu();
        }

        private void CboThanhVien_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dangKhoiTao || _dangNapUngVien) return;
            CapNhatDoiChieu();
        }

        private string? KiemTraChang()
        {
            if (GaNhan == null || GaGiao == null) return "Chọn ga nhận ban và ga bàn giao.";

            int i1 = _chuyen.ViTri(GaNhan.MaGa), i2 = _chuyen.ViTri(GaGiao.MaGa);
            if (i1 >= i2) return "Ga bàn giao phải nằm sau ga nhận ban trên hành trình.";

            var chong = _duLieu.KipCuaChuyen(_chuyen.MaChuyenTau)
                               .Where(p => p.MaPhanCong != _banGoc?.MaPhanCong)
                               .FirstOrDefault(p => p.ViTriNhan < i2 && i1 < p.ViTriGiao);
            if (chong != null)
                return $"Chặng chồng lên kíp {_duLieu.SoThuTuKip(chong)} ({chong.Chang}). Mỗi đoạn hành trình chỉ do một kíp đảm nhận.";

            return null;
        }

        private void CapNhatDoiChieu()
        {
            // --- Chang ---
            string? loiChang = KiemTraChang();
            txtLoiChang.Text = loiChang ?? "";
            if (GaNhan != null && GaGiao != null && loiChang == null)
            {
                DateTime gioNhan = _chuyen.GioNhanBan(GaNhan.MaGa), gioGiao = _chuyen.GioBanGiao(GaGiao.MaGa);
                txtKhungGio.Text = $"Nhận ban {gioNhan:HH:mm dd/MM} tại {GaNhan.TenGa} → bàn giao {gioGiao:HH:mm dd/MM} " +
                                   $"tại {GaGiao.TenGa} · {QuyTacKipLai.SoGio(gioNhan, gioGiao):0.0} giờ";
            }
            else
            {
                txtKhungGio.Text = string.Empty;
            }

            // --- Tung vi tri ---
            var chon = QuyTacKipLai.CacVaiTro.Select(vt => OChon(vt).SelectedItem as UngVienKip).ToArray();
            for (int i = 0; i < 3; i++)
            {
                var loi = chon[i]?.DieuKien.FirstOrDefault(d => d.LaLoi);
                OLoi(QuyTacKipLai.CacVaiTro[i]).Text = loi == null ? "" : VietHoaDau(loi.MoTa) + ".";
            }

            VeBangDoiChieu(chon);

            // --- Ket luan ---
            var loiNguoi = chon.Where(u => u != null).SelectMany(u => u!.DieuKien.Where(d => d.LaLoi)
                                                                   .Select(d => $"{QuyTacKipLai.TenVaiTro(VaiTroCua(u, chon))} {u.HoTen}: {d.MoTa}"))
                               .ToList();
            var canhBao = chon.Where(u => u != null).SelectMany(u => u!.DieuKien.Where(d => d.Dat == false && !d.LaLoi && d.MucDo == MucDoVanDe.CanhBao)
                                                                   .Select(d => $"{u.HoTen}: {d.MoTa}"))
                              .ToList();
            int soTrong = chon.Count(u => u == null);

            if (loiChang != null || loiNguoi.Count > 0)
            {
                var ds = new List<string>();
                if (loiChang != null) ds.Add(loiChang);
                ds.AddRange(loiNguoi);
                DatKetLuan("#FEF2F2", "#FCA5A5", "#B91C1C", $"CÒN {ds.Count} LỖI — CHƯA LƯU ĐƯỢC",
                           string.Join("\n", ds.Take(4).Select(l => "• " + l)));
            }
            else if (soTrong > 0)
            {
                DatKetLuan("#F8FAFC", "#E2E8F0", "#475569", "CHƯA ĐỦ THÀNH VIÊN",
                           $"Còn {soTrong} vị trí chưa chọn người. Kíp bắt buộc đủ lái tàu, phụ lái và trưởng tàu.");
            }
            else if (canhBao.Count > 0)
            {
                DatKetLuan("#FFFBEB", "#FDE68A", "#B45309", $"LƯU ĐƯỢC · {canhBao.Count} CẢNH BÁO",
                           string.Join("\n", canhBao.Take(3).Select(l => "• " + l)));
            }
            else
            {
                var luuY = chon.Where(u => u != null)
                               .SelectMany(u => u!.DieuKien.Where(d => d.Dat == false && d.MucDo == MucDoVanDe.LuuY)
                                                           .Select(d => $"{u.HoTen}: {d.MoTa}"))
                               .ToList();
                DatKetLuan("#F0FDF4", "#86EFAC", "#15803D", "ĐỦ ĐIỀU KIỆN PHÂN CÔNG",
                           (luuY.Count > 0 ? "Lưu ý: " + string.Join("; ", luuY) + ".\n" : "") +
                           "Sau khi lưu, cả 3 người phải đạt kiểm tra lên ban trước giờ nhận ban.");
            }
        }

        private static string VaiTroCua(UngVienKip u, UngVienKip?[] chon)
            => QuyTacKipLai.CacVaiTro[Array.IndexOf(chon, u)];

        private void DatKetLuan(string nen, string vien, string chu, string tieuDe, string moTa)
        {
            bdKetLuan.Background = Mau(nen);
            bdKetLuan.BorderBrush = Mau(vien);
            txtKetLuan.Text = tieuDe;
            txtKetLuan.Foreground = Mau(chu);
            txtKetLuanPhu.Text = moTa;
        }

        // Bang doi chieu: hang = dieu kien, cot = 3 vi tri
        private void VeBangDoiChieu(UngVienKip?[] chon)
        {
            var g = gridDoiChieu;
            g.Children.Clear();
            g.ColumnDefinitions.Clear();
            g.RowDefinitions.Clear();

            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(118) });
            for (int c = 0; c < 3; c++) g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Hang tieu de
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            DatO(new Border { Background = Mau("#F1F5F9") }, 0, 0, 4);
            DatO(new TextBlock { Text = "Điều kiện", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Mau("#334155"),
                                 Margin = new Thickness(8, 5, 4, 5), VerticalAlignment = VerticalAlignment.Center }, 0, 0);
            for (int c = 0; c < 3; c++)
            {
                var tb = new TextBlock { FontSize = 10, Margin = new Thickness(6, 5, 4, 5), TextTrimming = TextTrimming.CharacterEllipsis };
                tb.Inlines.Add(new Run(QuyTacKipLai.TenVaiTro(QuyTacKipLai.CacVaiTro[c])) { FontWeight = FontWeights.Bold, Foreground = Mau("#334155") });
                tb.Inlines.Add(new LineBreak());
                tb.Inlines.Add(new Run(chon[c]?.HoTen ?? "(chưa chọn)") { Foreground = Mau("#64748B") });
                DatO(tb, 0, c + 1);
            }

            // Hang dieu kien
            for (int r = 0; r < DanhGiaKipLai.ThuTuDieuKien.Length; r++)
            {
                string ten = DanhGiaKipLai.ThuTuDieuKien[r];
                int hang = r + 1;
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                DatO(new Border { BorderBrush = Mau("#E2E8F0"), BorderThickness = new Thickness(0, 1, 0, 0),
                                  Background = Mau(r % 2 == 0 ? "#FFFFFF" : "#F8FAFC") }, hang, 0, 4);
                DatO(new TextBlock { Text = ten, FontSize = 10, Foreground = Mau("#475569"), TextWrapping = TextWrapping.Wrap,
                                     Margin = new Thickness(8, 4, 4, 4), VerticalAlignment = VerticalAlignment.Center }, hang, 0);

                for (int c = 0; c < 3; c++)
                {
                    var dk = chon[c]?.DieuKien.FirstOrDefault(d => d.TenDieuKien == ten);
                    var tb = new TextBlock { FontSize = 10, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(6, 4, 4, 4),
                                             VerticalAlignment = VerticalAlignment.Center };
                    if (dk == null)
                    {
                        tb.Text = "";
                    }
                    else
                    {
                        tb.Inlines.Add(new Run(dk.KyHieu + " ") { FontWeight = FontWeights.Bold, Foreground = Mau(dk.MauChu) });
                        tb.Inlines.Add(new Run(dk.GiaTri) { Foreground = Mau(dk.Dat == true ? "#334155" : dk.MauChu) });
                        if (!string.IsNullOrEmpty(dk.MoTa)) tb.ToolTip = VietHoaDau(dk.MoTa);
                    }
                    DatO(tb, hang, c + 1);
                }
            }
        }

        private void DatO(UIElement e, int hang, int cot, int soCot = 1)
        {
            Grid.SetRow(e, hang);
            Grid.SetColumn(e, cot);
            Grid.SetColumnSpan(e, soCot);
            gridDoiChieu.Children.Add(e);
        }

        // =====================================================================
        // LUU / HUY
        // =====================================================================

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!KiemTraHopLe(out var chon)) return;

            var kq = _banGoc?.SaoChep() ?? new PhanCongKipHienThi
            {
                MaChuyenTau = _chuyen.MaChuyenTau,
                TrangThai = PhanCongKipHienThi.DaPhanCong
            };
            kq.MaGaNhanBan = GaNhan!.MaGa;
            kq.MaGaBanGiao = GaGiao!.MaGa;
            for (int i = 0; i < 3; i++) kq.GanNhanVien(QuyTacKipLai.CacVaiTro[i], chon[i].NhanVien.MaNhanVien);

            KetQua = kq;
            DialogResult = true;
        }

        private bool KiemTraHopLe(out UngVienKip[] chon)
        {
            bool hopLe = true;
            Control? oLoiDauTien = null;
            chon = QuyTacKipLai.CacVaiTro.Select(vt => (OChon(vt).SelectedItem as UngVienKip)!).ToArray();

            string? loiChang = KiemTraChang();
            if (loiChang != null)
            {
                txtLoiChang.Text = loiChang;
                hopLe = false;
                oLoiDauTien = cboGaNhan;
            }

            for (int i = 0; i < 3; i++)
            {
                string vaiTro = QuyTacKipLai.CacVaiTro[i];
                var u = chon[i];
                string? loi = null;

                if (u == null)
                    loi = $"Vui lòng chọn {QuyTacKipLai.TenVaiTro(vaiTro).ToLower()}.";
                else if (chon.Where((x, j) => j != i && x != null).Any(x => x.NhanVien.MaNhanVien == u.NhanVien.MaNhanVien))
                    loi = "Một người không giữ hai vị trí trong cùng kíp (CK_KipLai_KhacNhanVien).";
                else if (u.DieuKien.FirstOrDefault(d => d.LaLoi) is { } dk)
                    loi = VietHoaDau(dk.MoTa) + ".";

                if (loi == null) continue;
                OLoi(vaiTro).Text = loi;
                hopLe = false;
                oLoiDauTien ??= OChon(vaiTro);
            }

            if (!hopLe) oLoiDauTien?.Focus();
            return hopLe;
        }

        private static string VietHoaDau(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

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
