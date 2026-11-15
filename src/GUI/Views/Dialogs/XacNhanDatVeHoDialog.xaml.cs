using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BUS.AI;
using BUS.Services;
using DTO.AI;

namespace GUI.Views.Dialogs
{
    // =========================================================================
    // XAC NHAN DAT VE HO (16/11, Dat): mo khi AI goi cong cu dat_ve hoac bam "Dat Ve Ngay" tren the phuong an.
    // Hien thong tin chuyen + tung ve (gia goc, giam, thuc thu theo doi tuong), nhan vien nhap ho ten / CCCD tung hanh khach,
    // nguoi mua + SDT + hinh thuc thanh toan -> Xac nhan -> DatVeHoService.Dat (VeService.DatVeTheoDon cua A Ba).
    // Loi (ghe vua bi ban, CCCD sai...) -> giu cua so mo de sua. Huy -> KetQua.NguoiDungHuy.
    // =========================================================================
    public partial class XacNhanDatVeHoDialog : Window
    {
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
        private readonly YeuCauDatVeHo _yc;
        private readonly List<DongHanhKhach> _dong;
        private readonly DatVeHoService _sv = new();

        public KetQuaDatVeHo KetQua { get; private set; } = new() { NguoiDungHuy = true };

        public XacNhanDatVeHoDialog(YeuCauDatVeHo yc, bool doAIDeNghi)
        {
            InitializeComponent();
            _yc = yc;
            var pa = yc.PhuongAn;
            if (doAIDeNghi) txtPhuDe.Text = "Trợ lý AI đề nghị đặt phương án này — kiểm tra, nhập hành khách rồi xác nhận";

            txtMacTau.Text = pa.SoHieuMacTau;
            txtLoaiTau.Text = $"{QuyTacMoHinhNgonNgu.TenLoaiTau(pa.LoaiTau)} · {pa.TenLoaiCho}";
            txtNgay.Text = $"{QuyTacMoHinhNgonNgu.TenThu(pa.GioDi)} {pa.GioDi:dd/MM/yyyy}";
            txtGaDi.Text = yc.TenGaDi;
            txtGioDi.Text = $"{pa.GioDi:HH:mm} · {pa.GioDi:dd/MM}";
            txtGaDen.Text = yc.TenGaDen;
            txtGioDen.Text = $"{pa.GioDen:HH:mm} · {pa.GioDen:dd/MM}";
            txtHanhTrinh.Text = $"{pa.NhanGhe} · {pa.CuLyKm.ToString("N0", Vi)} km · khoảng {pa.SoGioChay:0.#} giờ" +
                                (pa.QuaDem ? " · chạy qua đêm" : "");

            _dong = DatVeHoService.TaoHanhKhach(pa, VeService.LayTyLeGiamGia)
                .Select(h => new DongHanhKhach(h, pa.LoaiToa, DoiTong)).ToList();
            icHanhKhach.ItemsSource = _dong;

            cboHinhThuc.ItemsSource = DatVeHoService.CacHinhThucTT.Select(x => new { x.Ma, x.Ten }).ToList();
            cboHinhThuc.SelectedIndex = 0;
            DoiTong();
            Loaded += (_, _) =>
            {
                var o = TimTextBoxDau();
                o?.Focus();
            };
        }

        private System.Windows.Controls.TextBox? TimTextBoxDau()
        {
            icHanhKhach.UpdateLayout();
            if (icHanhKhach.ItemContainerGenerator.ContainerFromIndex(0) is not DependencyObject goc) return null;
            return TimCon<System.Windows.Controls.TextBox>(goc);
        }

        private static T? TimCon<T>(DependencyObject goc) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(goc); i++)
            {
                var con = System.Windows.Media.VisualTreeHelper.GetChild(goc, i);
                if (con is T t) return t;
                if (TimCon<T>(con) is { } sau) return sau;
            }
            return null;
        }

        private void DoiTong()
        {
            if (txtTongTien == null || _dong == null) return;
            decimal goc = _dong.Sum(d => d.Goc.GiaGoc), giam = _dong.Sum(d => d.Goc.SoTienGiam), thu = _dong.Sum(d => d.Goc.ThucThu);
            txtTongTien.Text = thu.ToString("N0", Vi) + " đ";
            txtChiTietTong.Text = $"{_dong.Count} vé · giá gốc {goc.ToString("N0", Vi)} đ" + (giam > 0 ? $" · giảm {giam.ToString("N0", Vi)} đ" : "");
        }

        private void BtnXacNhan_Click(object sender, RoutedEventArgs e)
        {
            txtLoi.Text = "";
            string ten = txtNguoiMua.Text.Trim();
            if (ten.Length == 0) ten = _dong.Select(d => d.HoTen.Trim()).FirstOrDefault(x => x.Length > 0) ?? "";
            var ds = _dong.Select(d => d.Goc).ToList();
            var loi = DatVeHoService.KiemTra(ten, txtSdt.Text, ds);
            if (loi.Count > 0) { txtLoi.Text = string.Join("\n", loi); return; }

            btnXacNhan.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            KetQuaDatVeHo kq;
            try { kq = _sv.Dat(_yc, ten, txtSdt.Text, cboHinhThuc.SelectedValue as string ?? "TIEN_MAT", ds); }
            finally { Mouse.OverrideCursor = null; btnXacNhan.IsEnabled = true; }

            if (!kq.DaDat)
            {
                txtLoi.Text = "Chưa đặt được: " + kq.Loi;
                return;
            }
            KetQua = kq;
            ThongBaoDialog.ThanhCong($"Đã đặt đơn {kq.MaPnr}: {kq.SoVe} vé ({string.Join(", ", kq.CacMaVe)}).\n" +
                                     $"Tổng thu {kq.TongTien.ToString("N0", Vi)} đ.", "Đặt Vé Thành Công", this);
            DialogResult = true;
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void ChiNhapSo(object sender, TextCompositionEventArgs e) => e.Handled = !e.Text.All(char.IsDigit);

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { e.Handled = true; DialogResult = false; }
            else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control) { e.Handled = true; BtnXacNhan_Click(this, new RoutedEventArgs()); }
        }

        // --- Mot dong hanh khach (binding 2 chieu) ---------------------------------
        public sealed record DoiTuong(string Ma, string Ten);

        public sealed class DongHanhKhach : INotifyPropertyChanged
        {
            private readonly Action _doiTong;

            public DongHanhKhach(HanhKhachDatVe goc, string loaiToa, Action doiTong)
            {
                Goc = goc;
                _doiTong = doiTong;
                NhanCho = (loaiToa is "AN" or "BN" ? "Giường " : "Ghế ") + goc.SoGhe;
            }

            public HanhKhachDatVe Goc { get; }
            public string NhanCho { get; }

            public IReadOnlyList<DoiTuong> CacDoiTuong { get; } = new[]
            {
                new DoiTuong("THUONG", "Người lớn"), new DoiTuong("TRE_EM", "Trẻ em"),
                new DoiTuong("NGUOI_GIA", "Người cao tuổi"), new DoiTuong("SINH_VIEN", "Học sinh / sinh viên"),
            };

            public string HoTen { get => Goc.HoTen; set => Goc.HoTen = value ?? ""; }
            public string Cccd { get => Goc.Cccd; set => Goc.Cccd = value ?? ""; }

            public string LoaiKhach
            {
                get => Goc.LoaiKhach;
                set
                {
                    if (string.IsNullOrEmpty(value) || value == Goc.LoaiKhach) return;
                    Goc.LoaiKhach = value;
                    Goc.SoTienGiam = DatVeHoService.TinhGiam(Goc.GiaGoc, value, VeService.LayTyLeGiamGia);
                    BaoDoi(nameof(NhanThucThu));
                    BaoDoi(nameof(NhanGiam));
                    _doiTong();
                }
            }

            public string NhanThucThu => Goc.ThucThu.ToString("N0", Vi) + " đ";
            public string NhanGiam => Goc.SoTienGiam > 0 ? $"gốc {Goc.GiaGoc.ToString("N0", Vi)} · giảm {Goc.SoTienGiam.ToString("N0", Vi)}" : "";

            public event PropertyChangedEventHandler? PropertyChanged;
            private void BaoDoi([CallerMemberName] string? ten = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ten));
        }
    }
}
