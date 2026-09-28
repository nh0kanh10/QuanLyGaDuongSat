using System.Data;
using Microsoft.Data.SqlClient;
using DAL.Connection;

namespace DAL.Repositories
{
    public class DieuDoRepository
    {
        public DataTable LayDuLieuBieuDo(DateTime ngay)
        {
            string sql = @"
                SELECT 
                    ct.MaChuyenTau, ct.NgayXuatPhat, ct.GioXuatPhatKH, ct.GioVeDichKH, ct.TrangThai,
                    mt.SoHieuMacTau, mt.LoaiTau, mt.HuongChay, mt.MucUuTien,
                    gDi.TenGa AS TenGaDi, gDen.TenGa AS TenGaDen,
                    ISNULL(dt.TongChieuDaiM, 250.0) AS TongChieuDaiM,
                    ld.MaDiemDung, ld.ThuTuDung, ld.MaGa, ld.GioDenKeHoach, ld.GioDiKeHoach, 
                    ld.LaDiemTranh, ld.MaDuongRay,
                    g.TenGa, g.MaGaCode, g.LyTrinhKm,
                    dr.SoHieuDuong AS SoHieuDuongRay, dr.LoaiDuong
                FROM vanhanh.ChuyenTau ct
                JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau
                JOIN hatang.Ga gDi ON mt.MaGaDi = gDi.MaGa
                JOIN hatang.Ga gDen ON mt.MaGaDen = gDen.MaGa
                LEFT JOIN vanhanh.DoanTau dt ON ct.MaChuyenTau = dt.MaChuyenTau
                JOIN vanhanh.LichDungGa ld ON ct.MaChuyenTau = ld.MaChuyenTau
                JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                LEFT JOIN hatang.DuongRayGa dr ON ld.MaDuongRay = dr.MaDuongRay
                WHERE (ct.NgayXuatPhat = @ngay 
                       OR CAST(ct.GioXuatPhatKH AS DATE) = @ngay 
                       OR (@ngay >= CAST(ct.GioXuatPhatKH AS DATE) AND @ngay <= CAST(ct.GioVeDichKH AS DATE)))
                  AND ct.TrangThai <> 'DA_HUY'
                ORDER BY ct.MaChuyenTau, ld.ThuTuDung ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@ngay", ngay.Date) });
        }

        public DateTime LayNgayBieuDoMacDinh()
        {
            // 1. Kiểm tra xem ngày hôm nay có chuyến tàu hoạt động không
            string sqlCheckToday = @"
                SELECT COUNT(1) 
                FROM vanhanh.ChuyenTau ct
                WHERE (CAST(ct.NgayXuatPhat AS DATE) = CAST(GETDATE() AS DATE)
                       OR CAST(GETDATE() AS DATE) BETWEEN CAST(ct.GioXuatPhatKH AS DATE) AND CAST(ct.GioVeDichKH AS DATE))
                  AND ct.TrangThai <> 'DA_HUY'";
            object? count = DatabaseHelper.ExecuteScalar(sqlCheckToday);
            if (count != null && Convert.ToInt32(count) > 0)
            {
                return DateTime.Today;
            }

            // 2. Nếu hôm nay không có tàu, tự động lấy ngày gần nhất trong CSDL có chuyến tàu khai thác
            string sqlNearest = @"
                SELECT TOP 1 CAST(ct.NgayXuatPhat AS DATE)
                FROM vanhanh.ChuyenTau ct
                WHERE ct.TrangThai <> 'DA_HUY'
                ORDER BY ABS(DATEDIFF(day, ct.NgayXuatPhat, GETDATE())) ASC";
            object? obj = DatabaseHelper.ExecuteScalar(sqlNearest);
            if (obj != null && obj != DBNull.Value)
            {
                return Convert.ToDateTime(obj);
            }

            return DateTime.Today;
        }

        public DataTable LayDanhSachNgayCoTau()
        {
            string sql = @"
                SELECT TOP 4 
                    CAST(ct.NgayXuatPhat AS DATE) AS Ngay,
                    COUNT(ct.MaChuyenTau) AS SoChuyenTau
                FROM vanhanh.ChuyenTau ct
                WHERE ct.TrangThai <> 'DA_HUY'
                GROUP BY CAST(ct.NgayXuatPhat AS DATE)
                ORDER BY Ngay DESC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable LayDanhSachGaTuyen()
        {
            string sql = @"SELECT g.MaGa, g.MaGaCode, g.TenGa, g.LyTrinhKm, g.HangGa
                           FROM hatang.Ga g
                           WHERE g.DangKhaiThac = 1
                           ORDER BY g.LyTrinhKm ASC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable LayDanhSachKhuGian()
        {
            string sql = @"
                SELECT kg.MaKhuGian, kg.MaGaDau, kg.MaGaCuoi, kg.CuLyKm, kg.TocDoToiDaKhach, kg.TocDoToiDaHang,
                       g1.TenGa AS TenGaDau, g1.MaGaCode AS MaGaDauCode, g1.LyTrinhKm AS LyTrinhDauKm,
                       g2.TenGa AS TenGaCuoi, g2.MaGaCode AS MaGaCuoiCode, g2.LyTrinhKm AS LyTrinhCuoiKm
                FROM hatang.KhuGian kg
                JOIN hatang.Ga g1 ON kg.MaGaDau = g1.MaGa
                JOIN hatang.Ga g2 ON kg.MaGaCuoi = g2.MaGa
                ORDER BY g1.LyTrinhKm ASC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public DataTable LayTatCaGaCoDuongTranh(decimal chieuDaiM)
        {
            string sql = @"
                SELECT g.MaGa, g.MaGaCode, g.TenGa, g.LyTrinhKm,
                       dr.MaDuongRay, dr.SoHieuDuong, dr.ChieuDaiHuuDungM
                FROM hatang.Ga g
                JOIN hatang.DuongRayGa dr ON g.MaGa = dr.MaGa
                WHERE g.DangKhaiThac = 1
                  AND dr.LoaiDuong = 'DUONG_TRANH'
                  AND dr.ChieuDaiHuuDungM >= @chieuDai
                ORDER BY g.LyTrinhKm ASC, dr.ChieuDaiHuuDungM ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@chieuDai", chieuDaiM) });
        }

        public DataTable LayDuongTranhKhaDung(int maGa, decimal chieuDaiM)
        {
            string sql = @"
                SELECT TOP 1 dr.MaDuongRay, dr.SoHieuDuong, dr.ChieuDaiHuuDungM, dr.LoaiDuong
                FROM hatang.DuongRayGa dr
                WHERE dr.MaGa = @maGa
                  AND dr.LoaiDuong = 'DUONG_TRANH'
                  AND dr.ChieuDaiHuuDungM >= @chieuDai
                ORDER BY dr.ChieuDaiHuuDungM ASC";

            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@maGa", maGa),
                new SqlParameter("@chieuDai", chieuDaiM)
            });
        }

        public bool CapNhatLichTranhTau(int maChuyenTau, int maGa, DateTime gioDenMoi, DateTime gioDiMoi, int? maDuongRay, int soPhutTang, string lyDo)
        {
            string sql = @"
                BEGIN TRANSACTION;
                BEGIN TRY
                    -- Vô hiệu hóa tạm thời trigger kiểm tra thời gian để thực hiện cập nhật tịnh tiến đồng bộ
                    ALTER TABLE vanhanh.LichDungGa DISABLE TRIGGER trg_LichDungGa_CheckThoiGian;

                    DECLARE @thuTuHienTai INT = NULL;
                    SELECT @thuTuHienTai = ThuTuDung 
                    FROM vanhanh.LichDungGa 
                    WHERE MaChuyenTau = @ct AND MaGa = @ga;

                    IF @thuTuHienTai IS NOT NULL
                    BEGIN
                        -- Ga này đã có sẵn trong lịch
                        -- 1. Tịnh tiến giờ các ga phía sau TRƯỚC
                        IF @soPhutTang > 0
                        BEGIN
                            UPDATE vanhanh.LichDungGa
                            SET GioDenKeHoach = DATEADD(MINUTE, @soPhutTang, GioDenKeHoach),
                                GioDiKeHoach = DATEADD(MINUTE, @soPhutTang, GioDiKeHoach)
                            WHERE MaChuyenTau = @ct AND ThuTuDung > @thuTuHienTai;
                        END

                        -- 2. Cập nhật tại ga dừng tránh
                        UPDATE vanhanh.LichDungGa
                        SET GioDenKeHoach = @gioDen,
                            GioDiKeHoach = @gioDi,
                            LaDiemTranh = 1,
                            MaDuongRay = @ray
                        WHERE MaChuyenTau = @ct AND MaGa = @ga;
                    END
                    ELSE
                    BEGIN
                        -- Ga này chưa có trong lịch (ga kỹ thuật/ga đường tránh mới trên tuyến) -> Chèn mới vào đúng thứ tự lý trình
                        DECLARE @lyTrinhGa DECIMAL(7,2);
                        SELECT @lyTrinhGa = LyTrinhKm FROM hatang.Ga WHERE MaGa = @ga;

                        DECLARE @huongChay VARCHAR(20);
                        SELECT @huongChay = mt.HuongChay 
                        FROM vanhanh.ChuyenTau ct 
                        JOIN vanhanh.MacTauMau mt ON ct.MaMacTau = mt.MaMacTau 
                        WHERE ct.MaChuyenTau = @ct;

                        IF @huongChay = 'BAC_NAM'
                        BEGIN
                            -- Hướng Bắc -> Nam: LyTrinhKm tăng dần, tìm điểm dừng có LyTrinhKm nhỏ hơn @lyTrinhGa lớn nhất
                            SELECT TOP 1 @thuTuHienTai = ld.ThuTuDung + 1
                            FROM vanhanh.LichDungGa ld
                            JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                            WHERE ld.MaChuyenTau = @ct AND g.LyTrinhKm < @lyTrinhGa
                            ORDER BY g.LyTrinhKm DESC;
                        END
                        ELSE
                        BEGIN
                            -- Hướng Nam -> Bắc: LyTrinhKm giảm dần, tìm điểm dừng có LyTrinhKm lớn hơn @lyTrinhGa nhỏ nhất
                            SELECT TOP 1 @thuTuHienTai = ld.ThuTuDung + 1
                            FROM vanhanh.LichDungGa ld
                            JOIN hatang.Ga g ON ld.MaGa = g.MaGa
                            WHERE ld.MaChuyenTau = @ct AND g.LyTrinhKm > @lyTrinhGa
                            ORDER BY g.LyTrinhKm ASC;
                        END

                        IF @thuTuHienTai IS NULL SET @thuTuHienTai = 1;

                        -- 1. Tịnh tiến giờ các ga sau TRƯỚC (để giờ đến của ga sau luôn lớn hơn giờ đi của ga chèn mới)
                        IF @soPhutTang > 0
                        BEGIN
                            UPDATE vanhanh.LichDungGa
                            SET GioDenKeHoach = DATEADD(MINUTE, @soPhutTang, GioDenKeHoach),
                                GioDiKeHoach = DATEADD(MINUTE, @soPhutTang, GioDiKeHoach)
                            WHERE MaChuyenTau = @ct AND ThuTuDung >= @thuTuHienTai;
                        END

                        -- 2. Đẩy thứ tự các ga sau lên +1
                        UPDATE vanhanh.LichDungGa
                        SET ThuTuDung = ThuTuDung + 1
                        WHERE MaChuyenTau = @ct AND ThuTuDung >= @thuTuHienTai;

                        -- 3. Chèn điểm dừng tránh mới
                        INSERT INTO vanhanh.LichDungGa (MaChuyenTau, MaGa, ThuTuDung, GioDenKeHoach, GioDiKeHoach, LaDiemTranh, MaDuongRay)
                        VALUES (@ct, @ga, @thuTuHienTai, @gioDen, @gioDi, 1, @ray);
                    END

                    -- Cập nhật giờ về đích và số phút trễ lũy kế của chuyến tàu
                    IF @soPhutTang > 0
                    BEGIN
                        UPDATE vanhanh.ChuyenTau
                        SET GioVeDichKH = DATEADD(MINUTE, @soPhutTang, GioVeDichKH),
                            SoPhutTreLuyKe = SoPhutTreLuyKe + @soPhutTang
                        WHERE MaChuyenTau = @ct;
                    END

                    -- Ghi nhật ký vận hành
                    INSERT INTO vanhanh.NhatKyChamGio (MaChuyenTau, MaGa, LoaiSuCo, SoPhutCham, GhiChuXuLy, ThoiDiemBaoCao)
                    VALUES (@ct, @ga, 'CHO_TRANH_TAU', @soPhutTang, @lyDo, SYSDATETIME());

                    -- Kích hoạt lại trigger sau khi hoàn thành cập nhật toàn vẹn
                    ALTER TABLE vanhanh.LichDungGa ENABLE TRIGGER trg_LichDungGa_CheckThoiGian;

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    -- Đảm bảo trigger luôn được kích hoạt lại nếu xảy ra lỗi
                    ALTER TABLE vanhanh.LichDungGa ENABLE TRIGGER trg_LichDungGa_CheckThoiGian;
                    ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;";

            var parameters = new[]
            {
                new SqlParameter("@ct", maChuyenTau),
                new SqlParameter("@ga", maGa),
                new SqlParameter("@gioDen", gioDenMoi),
                new SqlParameter("@gioDi", gioDiMoi),
                new SqlParameter("@ray", (object?)maDuongRay ?? DBNull.Value),
                new SqlParameter("@soPhutTang", soPhutTang),
                new SqlParameter("@lyDo", lyDo)
            };

            return DatabaseHelper.ExecuteNonQuery(sql, parameters) > 0;
        }
    }
}
