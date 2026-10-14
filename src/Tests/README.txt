KIỂM THỬ ĐƠN VỊ (Tests)

Chạy (không cần CSDL):
    cd src
    dotnet test

Đang có (119 ca, 12/10/2026):
  PhuongTien\QuyTacLapTauTests     luật lập đoàn tàu: mỗi hạng mục một ca đạt / một ca lỗi,
                                   chặng lớn nhất khi cắt / nối toa dọc đường
  PhuongTien\TienDoBaoDuongTests   tiến độ bảo dưỡng R1 / R2 theo chu kỳ
  NhanSu\MaNhanVienTests           mã nhân viên theo chức danh (LT_001, PL_001...)
  NhanSu\DanhGiaKipLaiTests        luật phân kíp: ô chọn chỉ hiện người đủ điều kiện, lý do bị ẩn,
                                   trưởng tàu đi tiếp chặng sau, chặng chồng nhau,
                                   phân ngẫu nhiên 40 vòng không ra ca đỏ
  NhanSu\NhanSuServiceTests        kiểm tra hồ sơ, trạng thái, mốc ghi kiểm tra lên ban

Dữ liệu mẫu nằm trong bộ nhớ (MauLapTau.cs, MauNhanSu.cs), không đọc CSDL.
Muốn thêm kiểm thử cho phần của mình: tạo thư mục con theo phân hệ, đặt tên lớp ...Tests.

Kiểm thử ghi CSDL và giao diện chưa đưa vào đây (vẫn chạy bằng các chương trình ở tools\ ngoài repo).
