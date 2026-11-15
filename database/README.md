# Thư mục `database/` — dựng CSDL `QuanLyDuongSatV2`

## Dựng lại từ đầu (một lệnh)

Tắt ứng dụng trước (script gốc xoá và tạo lại CSDL), rồi mở **cmd**:

```
database\tao_lai_csdl.cmd                         :: ngày dữ liệu = hôm nay (giờ máy)
database\tao_lai_csdl.cmd 2026-11-10              :: dựng trước cho ngày demo 10/11
database\tao_lai_csdl.cmd 2026-11-10 QLDS_Thu     :: dựng CSDL khác để thử
database\tao_lai_csdl.cmd 2026-11-10 QuanLyDuongSatV2 .\SQLEXPRESS   :: máy chủ khác (mặc định ".")
```

Lệnh sẽ:
1. sao lưu CSDL đang có (thư mục Backup của SQL Server, `<CSDL>_truoc_tao_lai_<ngày giờ>.bak`);
2. chạy `000_tao_csdl_goc.sql` (script chung của nhóm, thay ngày dữ liệu bằng ngày truyền vào);
3. chạy lần lượt các file trong `thu_tu_chay.txt` (migration, seed);
4. chạy script kiểm tra dữ liệu (`tests\`) — có vi phạm thì chỉ cảnh báo.

Dữ liệu chuyến tàu, kíp lái, vé… **tính theo ngày dựng** → sáng hôm demo nên dựng lại.
Nhật ký từng bước: `%TEMP%\qlds_tao_lai_<CSDL>\`.

## Tài khoản demo

Mật khẩu chung: **`Demo@123`** (chỉ là dữ liệu demo, đặt ở seed `S20261108_1100`).

| Tài khoản | Vai trò | Thấy / làm được |
|---|---|---|
| `admin` | Quản trị viên | toàn quyền, trang Quản trị |
| `quanly` | Cán bộ quản lý | xem mọi phân hệ + Báo cáo, không sửa |
| `dieudo` | Trực ban điều độ | lịch trình, biểu đồ, phong tỏa, báo cáo |
| `truongga` | Trực ban ga | hạ tầng, soát vé |
| `nv_mai` | Bán vé & vận đơn | bán / hoàn vé, soát vé, hàng hóa, Tư vấn AI |
| `nv_dung`, `nv_hung`, `nv_son` | Khám xe kỹ thuật | Bảo trì (khám xe, duyệt an toàn, nhiên liệu, bảo dưỡng) |
| `laptau` | Quản đốc lập tàu | Phương tiện, lập đoàn tàu |
| `nhansu` | Cán bộ nhân sự | hồ sơ nhân viên |
| `phancong` | Quản đốc kíp lái | phân kíp, kiểm tra lên ban, giao nhận ca |
| `laitau` | Lái tàu (hồ sơ LT_001) | chỉ xem lịch trình, lịch ca, bảo trì |

Sai mật khẩu **5 lần liên tiếp** → tài khoản bị khóa **15 phút** (tự mở). Quản trị viên mở ngay ở
trang Quản trị → Tài khoản → **Mở Khóa**. Lỡ khóa chính `admin` mà không còn quản trị viên nào khác:

```sql
UPDATE phanquyen.TaiKhoan SET SoLanSaiLienTiep = 0, KhoaDenLuc = NULL WHERE TenDangNhap = 'admin';
```

## Thêm migration / seed mới

- Tên: `migrations\VyyyyMMdd_HHmm__<người>__<mô-tả>.sql`, `seed\SyyyyMMdd_HHmm__<người>__<mô-tả>.sql`.
- Mỗi file **chạy lại được** (kiểm `COL_LENGTH`, `OBJECT_ID`, dữ liệu đánh dấu…), không có `USE`, đầu file ghi tác giả + lý do.
- **Không sửa file đã merge** — đổi thì viết file mới.
- Thêm một dòng vào `thu_tu_chay.txt` (trước dòng `tests\`) cùng PR.
- Chạy riêng một file: đứng trong thư mục của file,
  `sqlcmd -S . -E -I -f 65001 -d QuanLyDuongSatV2 -i <tên file>` (phải có `-I`; sqlcmd hiểu `/` trong đường dẫn là tham số
  nên `cd` vào thư mục rồi truyền tên file trần).

## Danh mục quyền (24 mã, migration `V20261108_1000`)

Mỗi phân hệ 1 quyền **xem** + các quyền **ghi**. Tab hiện khi có quyền xem hoặc một quyền ghi của phân hệ;
Service kiểm lại quyền ghi trước khi ghi CSDL.

| Nhóm | Quyền xem | Quyền ghi |
|---|---|---|
| Quản trị | — | `SYS_CONFIG` |
| Báo cáo | `REPORT_VIEW` | — |
| Trợ lý AI | `AI_ADVISOR` | — |
| Mạng lưới & điều độ | `NETWORK_VIEW` | `INFRA_EDIT`, `TRAIN_SCHEDULE_EDIT`, `BLOCK_ORDER_APPROVE`, `DISPATCH_INCIDENT_REPORT` |
| Vé | `TICKET_VIEW` | `TICKET_ISSUE`, `TICKET_REFUND`, `TICKET_CHECK` |
| Hàng hóa | `CARGO_VIEW` | `CARGO_DISPATCH` |
| Phương tiện | `VEHICLE_VIEW` | `VEHICLE_EDIT`, `TRAIN_FORMATION_EDIT` |
| Bảo trì | `MAINT_VIEW` | `TECH_SAFETY_APPROVE`, `MAINTENANCE_LOG` |
| Nhân sự | `STAFF_VIEW` | `STAFF_EDIT`, `CREW_ASSIGN`, `CREW_CHECKIN` |

Hằng số trong mã: `DTO/Common/PhienLamViec.cs` (lớp `MaQuyen`).

## View báo cáo (schema `baocao`, migration `V20261108_1300`)

`vw_HanhTrinhChuyen`, `vw_DoanhThuVe`, `vw_HeSoSuDungGhe`, `vw_DungGioChuyen`, `vw_SanLuongHang` — chỉ đọc,
định nghĩa chỉ số ghi ở đầu file migration.
