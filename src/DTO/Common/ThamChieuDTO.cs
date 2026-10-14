namespace DTO.Common
{
    // =========================================================================
    // Du lieu dang tro toi mot ban ghi truoc khi xoa (dem tren TOAN bang, khong theo
    // cua so ngay): dau may / toa xe (Phuong tien), nhan vien (Nhan su).
    // Loai: DOAN_TAU | CAP_NHIEN_LIEU | BAO_DUONG | VAN_DON | KIP_LAI | KIEM_TRA | TAI_KHOAN | LICH_SU_MA
    // ViDu: vai muc gan nhat de nguoi dung nhan ra ("SE1 09/10, SE3 10/10"), co the rong.
    // Tat ca SoLuong = 0 -> ban ghi chua duoc dung o dau (nhap nham) -> xoa duoc.
    // =========================================================================
    public record SoThamChieuDTO(string Loai, int SoLuong, string ViDu);
}
