using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BUS.Services;
using GUI.Views.Dialogs;

namespace GUI.Views.Pages
{
    // [Đạt 30/10] Nút "Chuyển Trạng Thái" cho chuyến tàu đang chọn ở Lịch trình. Đặt file riêng để không đụng code-behind của A Bá.
    // Sang Sẵn sàng / Đang chạy: ChuyenTauService.DoiTrangThai gọi KiemTraXuatBenService (hợp đồng §7.2, KE_HOACH_BACK_END):
    // chưa đủ điều kiện thì báo lý do, không ghi gì. Không có mục Hủy chuyến ở đây (hủy cần lý do + xử lý vé đã bán).
    public partial class LichTrinhPage
    {
        private bool _daGanNutDoiTrangThai;

        private void BtnDoiTrangThai_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_daGanNutDoiTrangThai)
            {
                // Đăng ký sau DgChuyenTau_SelectionChanged (gắn trong XAML) nên chạy sau, đọc được _currentTrangThai mới
                dgChuyenTau.SelectionChanged += (_, _) => CapNhatNutDoiTrangThai();
                _daGanNutDoiTrangThai = true;
            }
            CapNhatNutDoiTrangThai();
        }

        private void CapNhatNutDoiTrangThai()
        {
            bool coChuyen = !_isAddingNew && _selectedMaChuyenTau > 0 && dgChuyenTau.SelectedItem != null;
            btnDoiTrangThai.Visibility = coChuyen && TrangThaiDoiDuoc(_currentTrangThai).Count > 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private static List<string> TrangThaiDoiDuoc(string trangThaiHienTai)
            => ChuyenTauService.LayTrangThaiKeTiep(trangThaiHienTai).Where(t => t != "DA_HUY").ToList();

        private void BtnDoiTrangThai_Click(object sender, RoutedEventArgs e)
        {
            if (_isAddingNew || _selectedMaChuyenTau <= 0) return;

            var menu = new ContextMenu { PlacementTarget = btnDoiTrangThai, Placement = PlacementMode.Top };
            foreach (string tt in TrangThaiDoiDuoc(_currentTrangThai))
            {
                string nhan = DinhDangTrangThai(tt);
                if (ChuyenTauService.CanKiemTraXuatBen(_currentTrangThai, tt)) nhan += "   (kiểm tra xuất bến)";
                var mi = new MenuItem { Header = nhan, Tag = tt };
                mi.Click += (_, _) => DoiTrangThaiChuyen(tt);
                menu.Items.Add(mi);
            }
            menu.IsOpen = true;
        }

        private void DoiTrangThaiChuyen(string trangThaiMoi)
        {
            int maChuyen = _selectedMaChuyenTau;
            string trangThaiCu = _currentTrangThai;
            string soHieu = (dgChuyenTau.SelectedItem as DataRowView)?["SoHieuMacTau"]?.ToString() ?? $"#{maChuyen}";

            try
            {
                // Kiểm tra xuất bến trước khi hỏi xác nhận: bị chặn thì báo lý do ngay
                string? lyDo = _chuyenTauService.LyDoChanXuatBen(maChuyen, trangThaiCu, trangThaiMoi);
                if (lyDo != null)
                {
                    ThongBaoDialog.CanhBao(
                        $"Chuyến {soHieu} chưa chuyển sang [{DinhDangTrangThai(trangThaiMoi)}] được.\n\n{lyDo}\n\n" +
                        "Xử lý ở phân hệ Bảo trì & Sửa chữa (khám xe, duyệt an toàn, bảo dưỡng) và Nhân sự (kíp lái) rồi chuyển lại.",
                        "Chưa Được Xuất Bến");
                    return;
                }

                if (!ThongBaoDialog.XacNhan(
                        $"Chuyển chuyến {soHieu} từ [{DinhDangTrangThai(trangThaiCu)}] sang [{DinhDangTrangThai(trangThaiMoi)}]?",
                        "Chuyển Trạng Thái Chuyến", "Chuyển")) return;

                // Service đọc lại trạng thái trong CSDL và kiểm tra lại lần nữa trước khi ghi
                if (!_chuyenTauService.DoiTrangThai(maChuyen, trangThaiMoi, out string loi))
                {
                    ThongBaoDialog.CanhBao(loi, "Không Chuyển Được Trạng Thái");
                    NapLaiGiuChuyen(maChuyen);
                    return;
                }
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Lỗi khi chuyển trạng thái chuyến tàu: {ex.Message}", "Lỗi CSDL");
                return;
            }

            _currentTrangThai = trangThaiMoi;
            ThongBaoDialog.ThanhCong($"Đã chuyển chuyến {soHieu} sang [{DinhDangTrangThai(trangThaiMoi)}].", "Chuyển Trạng Thái");
            NapLaiGiuChuyen(maChuyen);
        }

        // LoadData gán lại ItemsSource -> mất dòng đang chọn (mã chuyến về 0) nên chọn lại đúng chuyến vừa đổi
        private void NapLaiGiuChuyen(int maChuyen)
        {
            LoadData();
            foreach (var item in dgChuyenTau.Items)
            {
                if (item is DataRowView r && Convert.ToInt32(r["MaChuyenTau"]) == maChuyen)
                {
                    dgChuyenTau.SelectedItem = item;
                    dgChuyenTau.ScrollIntoView(item);
                    return;
                }
            }
        }
    }
}
