using System.Data;
using System.Windows;
using GUI.Views.Dialogs;

namespace GUI.Views.Pages
{
    // =========================================================================
    // [13/11 Dat] CHONG GHI DE KHI HAI NGUOI CUNG SUA MOT CHUYEN (partial cua trang Lich trinh - A Ba)
    //
    // Trang giu vanhanh.ChuyenTau.PhienBan cua chuyen dang mo (_phienBanChuyen) va truyen vao moi thao tac ghi
    // (ChuyenTauService ... ref phienBan). Service so trong khoa chuyen: khac -> khong ghi, VuaXungDotPhienBan = true
    // -> trang bao "Chuyen vua duoc thay doi" va nap lai ban moi nhat. Ghi xong Service tra PhienBan moi.
    // =========================================================================
    public partial class LichTrinhPage
    {
        private byte[]? _phienBanChuyen;

        private void GhiNhoPhienBan(DataRowView row)
            => _phienBanChuyen = row.Row.Table.Columns.Contains("PhienBan") && row["PhienBan"] is byte[] pb ? pb : null;

        // Thao tac ghi vua bi tu choi vi chuyen da bi thay doi o noi khac -> bao + nap lai, tra true.
        // Loi khac (rang buoc nghiep vu) -> tra false de noi goi bao nhu cu.
        private bool XuLyXungDotPhienBan(string loi)
        {
            if (!_chuyenTauService.VuaXungDotPhienBan) return false;

            ThongBaoDialog.CanhBao(loi, "Chuyến Vừa Được Thay Đổi");
            if (pnlFormDiemDung != null) pnlFormDiemDung.Visibility = Visibility.Collapsed;
            _editingMaDiemDung = 0;
            if (_selectedMaChuyenTau > 0) NapLaiGiuChuyen(_selectedMaChuyenTau);
            else LoadData();
            return true;
        }
    }
}
