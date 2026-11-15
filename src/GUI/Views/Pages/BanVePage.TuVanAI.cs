using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DTO.AI;
using DTO.Common;
using GUI.Helpers;
using GUI.Views.Dialogs;

namespace GUI.Views.Pages
{
    // =========================================================================
    // NUT "TU VAN AI" TREN TRANG BAN VE (Sprint 3, Dat - file rieng, partial cua BanVePage cua A Ba).
    // Hop dong §7.4: mo TuVanVeDialog (chi doc), nhan PhuongAnDuocChon roi dung chinh luong ban ve cua A Ba
    // (tim chuyen -> chon chuyen -> chon toa -> chon ghe) de dua ghe vao gio. Khong giu cho, khong ghi vantai.*:
    // thanh toan van qua BtnThanhToan_Click / VeService.DatVeTheoDon (sp_DatVeTheoChang) nhu ban binh thuong.
    // =========================================================================
    public partial class BanVePage
    {
        private async void BtnTuVanAI_Click(object sender, RoutedEventArgs e)
        {
            if (!QuyenGiaoDienHelper.KiemTra(MaQuyen.DungTroLyAI, "dùng trợ lý AI tư vấn vé")) return;

            int? gaDi = cboGaDi.SelectedValue is null ? null : Convert.ToInt32(cboGaDi.SelectedValue);
            int? gaDen = cboGaDen.SelectedValue is null ? null : Convert.ToInt32(cboGaDen.SelectedValue);
            var dlg = new TuVanVeDialog(gaDi, gaDen, dpNgayDi.SelectedDate) { Owner = Window.GetWindow(this) };
            bool? kq = dlg.ShowDialog();
            // [16/11] Da dat ve ngay trong cua so tu van (AI dat ho / nut "Dat Ve Ngay") -> nap lai cho trong + so tra cuu
            if (dlg.DaDatVe)
            {
                if (_currentMaChuyenTau > 0) LoadDanhSachToaXe();
                LoadDanhSachVeTab2();
            }
            if (kq != true || dlg.PhuongAn is not { } pa) return;
            await ApDungPhuongAnAI(pa);
        }

        private async Task ApDungPhuongAnAI(PhuongAnDuocChon pa)
        {
            cboGaDi.SelectedValue = pa.MaGaDi;
            cboGaDen.SelectedValue = pa.MaGaDen;
            dpNgayDi.SelectedDate = pa.NgayDi;
            await TimChuyenTauAsync();

            // Bo loc nang cao (gia, gio...) co the dang an chuyen -> bao ro thay vi im lang
            var dong = (dgChuyenTau.ItemsSource as DataView)?.Cast<DataRowView>()
                .FirstOrDefault(r => Convert.ToInt32(r["MaChuyenTau"]) == pa.MaChuyenTau);
            if (dong == null)
            {
                ThongBaoDialog.CanhBao("Chuyến AI gợi ý không có trong danh sách sau khi tìm (bộ lọc đang ẩn chuyến, hoặc chuyến vừa đổi trạng thái).\n" +
                                       "Bỏ bộ lọc rồi chọn lại.", "Tư Vấn AI");
                return;
            }
            dgChuyenTau.SelectedItem = dong;      // -> DgChuyenTau_SelectionChanged nap danh sach toa
            dgChuyenTau.ScrollIntoView(dong);

            var toa = _veService.LayDanhSachToaTheoChuyen(pa.MaChuyenTau, pa.MaGaDi, pa.MaGaDen).Rows.Cast<DataRow>()
                .FirstOrDefault(r => Convert.ToInt32(r["MaToaXeKhach"]) == pa.MaToaXeKhach);
            if (toa == null)
            {
                ThongBaoDialog.CanhBao("Toa AI gợi ý không còn trong biên chế chuyến. Hãy chọn toa khác.", "Tư Vấn AI");
                return;
            }
            ChonToaXe(pa.MaToaXeKhach, toa["NhanHieuToa"].ToString() ?? "", toa["LoaiToa"].ToString() ?? "",
                      toa["TenLoaiToa"].ToString() ?? "", Convert.ToInt32(toa["SoChoTrong"]), Convert.ToInt32(toa["TongSoCho"]));
            if (_mapCardToa.TryGetValue(pa.MaToaXeKhach, out var the)) HighlightCardToa(the);

            // Chon ghe: chi chon ghe con trong tren chang (co the vua bi nguoi khac dat sau khi tu van)
            var soDo = _veService.LaySoDoGhe(pa.MaToaXeKhach, pa.MaChuyenTau, pa.MaGaDi, pa.MaGaDen).Rows.Cast<DataRow>()
                .GroupBy(r => Convert.ToInt32(r["MaChoNgoi"]))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => Convert.ToInt32(r["DaDat"])).First());
            int daChon = 0, daBan = 0;
            var loaiDaDat = new System.Collections.Generic.List<string>();
            for (int i = 0; i < pa.CacMaChoNgoi.Count; i++)
            {
                int ma = pa.CacMaChoNgoi[i];
                if (!soDo.TryGetValue(ma, out var ghe) || Convert.ToInt32(ghe["DaDat"]) == 1) { daBan++; continue; }
                if (_gioVe.Any(x => x.MaChoNgoi == ma)) { daChon++; continue; }
                int? tang = ghe["TangGiuong"] == DBNull.Value ? null : Convert.ToInt32(ghe["TangGiuong"]);
                ToggleChonGhe(ma, Convert.ToInt32(ghe["SoGhe"]), tang);
                daChon++;

                // [12/11] Dat san doi tuong theo ho so khach da khai voi AI (giam gia nhu chon tay o gio ve)
                string loai = pa.CacLoaiKhach != null && i < pa.CacLoaiKhach.Count ? pa.CacLoaiKhach[i] : "THUONG";
                var item = _gioVe.FirstOrDefault(x => x.MaChoNgoi == ma);
                if (item != null && loai != "THUONG")
                {
                    item.LoaiKhach = loai;
                    item.SoTienGiam = _veService.TinhMucGiamGia(item.GiaGoc, loai);
                    item.GiaThucThu = Math.Max(0, item.GiaGoc - item.SoTienGiam);
                    loaiDaDat.Add(loai);
                }
            }
            if (loaiDaDat.Count > 0)
            {
                RenderGioVe();
                CapNhatTongTien();
            }

            if (daBan > 0)
                ThongBaoDialog.CanhBao($"{daBan} chỗ AI gợi ý vừa được bán cho chặng này. Đã chọn {daChon} chỗ còn lại — chọn thêm chỗ khác trên sơ đồ.", "Tư Vấn AI");
            else
                ShowToast($"Đã đưa {daChon} chỗ do AI gợi ý vào giỏ vé" +
                          (loaiDaDat.Count > 0 ? $" (đã đặt đối tượng: {BUS.AI.QuyTacTuVanVe.MoTaLoaiKhach(loaiDaDat)} — sửa từng vé nếu khác)" : "") +
                          ". Nhập thông tin hành khách rồi thanh toán.");
        }
    }
}
