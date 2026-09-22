using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using BUS.Services;
using ET.HaTang;
using GUI.Helpers;

namespace GUI.Profiles
{
    // Đối tượng dòng dữ liệu xem trước khi nhập Ga đường sắt
    
    public class ImportGaItem : ImportRowBase
    {
        public int MaGa { get; set; } = 0;
        public string MaGaCode { get; set; } = "";
        public string TenGa { get; set; } = "";
        public decimal LyTrinhKm { get; set; }
        public string LyTrinhHienThi => $"Km {LyTrinhKm:N2}";
        public string TinhThanh { get; set; } = "";
        public string HangGa { get; set; } = "HANG_3";
        public string HangGaHienThi => HangGa == "HANG_1" ? "Hạng I" : (HangGa == "HANG_2" ? "Hạng II" : "Hạng III");
        public bool CoCauQuay { get; set; } = false;
        public string CauQuayHienThi => CoCauQuay ? "Có" : "Không";
        public bool DangKhaiThac { get; set; } = true;
        public string KhaiThacHienThi => DangKhaiThac ? "Khai thác" : "Tạm ngừng";
    }

    // Hồ sơ cấu hình Import danh sách Ga Đường Sắt hàng loạt 
    public class GaImportProfile : IImportProfile
    {
        private readonly GaService _gaService;
        private readonly Dictionary<string, (int MaGa, string TenGa, decimal LyTrinh)> _existingGaByCode = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<decimal, (int MaGa, string TenGa, string Code)> _existingGaByKm = new();
        private readonly HashSet<string> _seenCodesInFile = new(StringComparer.OrdinalIgnoreCase);

        public GaImportProfile(GaService? gaService = null)
        {
            _gaService = gaService ?? new GaService();
        }

        public string TieuDeDialog => "NHẬP DANH SÁCH GA ĐƯỜNG SẮT HÀNG LOẠT (IMPORT EXCEL/CSV)";

        public string TenTepMauMacDinh => "Mau_Nhap_DanhSach_Ga.xlsx";

        public List<string> TieuDeCotMau => new()
        {
            "Mã Ga", "Tên Ga", "Lý Trình (km)", "Tỉnh Thành", "Phân Cấp", "Cầu Quay", "Đang Khai Thác"
        };

        public List<string[]> CacDongDuLieuMau => new()
        {
            new[] { "HN", "Hà Nội", "0.00", "Hà Nội", "HANG_1", "1", "1" },
            new[] { "GBA", "Giáp Bát", "5.00", "Hà Nội", "HANG_2", "0", "1" },
            new[] { "VDI", "Văn Điển", "8.90", "Hà Nội", "HANG_3", "0", "1" },
            new[] { "PLY", "Phủ Lý", "56.00", "Hà Nam", "HANG_2", "1", "1" },
            new[] { "NDI", "Nam Định", "87.00", "Nam Định", "HANG_2", "0", "1" },
            new[] { "THO", "Thanh Hóa", "176.00", "Thanh Hóa", "HANG_1", "1", "1" },
            new[] { "VIN", "Vinh", "319.00", "Nghệ An", "HANG_1", "1", "1" },
            new[] { "HUE", "Huế", "688.00", "Thừa Thiên Huế", "HANG_1", "1", "1" },
            new[] { "DAN", "Đà Nẵng", "791.00", "Đà Nẵng", "HANG_1", "1", "1" },
            new[] { "NTR", "Nha Trang", "1315.00", "Khánh Hòa", "HANG_1", "1", "1" },
            new[] { "SGO", "Sài Gòn", "1726.00", "TP. Hồ Chí Minh", "HANG_1", "1", "1" }
        };

        public List<string>? GhiChuCotMau => new()
        {
            "Mã Ga viết hoa không dấu (tối đa 10 ký tự, ví dụ: HN, SGO, DAN)",
            "Tên đầy đủ của Ga đường sắt (ví dụ: Hà Nội, Sài Gòn)",
            "Lý trình tính từ Ga Hà Nội (Km 0.00)",
            "Tỉnh hoặc Thành phố trực thuộc Trung ương",
            "Phân cấp: HANG_1 (Hạng I), HANG_2 (Hạng II), HANG_3 (Hạng III)",
            "Có thiết bị quay đầu đầu máy: 1 (Có), 0 (Không)",
            "Tình trạng khai thác: 1 (Đang khai thác), 0 (Tạm ngừng)"
        };

        public string NoiDungCsvMau => 
             @"Mã Ga,Tên Ga,Lý Trình (km),Tỉnh Thành,Phân Cấp,Cầu Quay,Đang Khai Thác
             HN,Hà Nội,0.00,Hà Nội,HANG_1,1,1
             GBA,Giáp Bát,5.00,Hà Nội,HANG_2,0,1
             VDI,Văn Điển,8.90,Hà Nội,HANG_3,0,1
             PLY,Phủ Lý,56.00,Hà Nam,HANG_2,1,1
             NDI,Nam Định,87.00,Nam Định,HANG_2,0,1
             THO,Thanh Hóa,176.00,Thanh Hóa,HANG_1,1,1
             VIN,Vinh,319.00,Nghệ An,HANG_1,1,1
             HUE,Huế,688.00,Thừa Thiên Huế,HANG_1,1,1
             DAN,Đà Nẵng,791.00,Đà Nẵng,HANG_1,1,1
             NTR,Nha Trang,1315.00,Khánh Hòa,HANG_1,1,1
             SGO,Sài Gòn,1726.00,TP. Hồ Chí Minh,HANG_1,1,1";

        public List<ImportColumnDefinition> CacCotPreview => new()
        {
            new ImportColumnDefinition("Mã Ga", nameof(ImportGaItem.MaGaCode), 85, TextAlignment.Center, isBold: true),
            new ImportColumnDefinition("Tên Ga", nameof(ImportGaItem.TenGa), 150, TextAlignment.Left, isBold: true),
            new ImportColumnDefinition("Lý Trình", nameof(ImportGaItem.LyTrinhHienThi), 105, TextAlignment.Right, isConsolas: true),
            new ImportColumnDefinition("Tỉnh / Thành Phố", nameof(ImportGaItem.TinhThanh), 140, TextAlignment.Left),
            new ImportColumnDefinition("Phân Cấp", nameof(ImportGaItem.HangGaHienThi), 90, TextAlignment.Center),
            new ImportColumnDefinition("Cầu Quay", nameof(ImportGaItem.CauQuayHienThi), 75, TextAlignment.Center),
            new ImportColumnDefinition("Trạng Thái", nameof(ImportGaItem.KhaiThacHienThi), 90, TextAlignment.Center)
        };

        public List<SmartColumnTarget> CacCotMucTieu => new()
        {
            new SmartColumnTarget("MaGaCode", new[] { "mã ga", "mã code", "mã ga code", "mã", "code", "magacode", "maga", "station code" }, isRequired: true),
            new SmartColumnTarget("TenGa", new[] { "tên ga", "tên", "tenga", "tên trạm", "ga", "station name" }, isRequired: true),
            new SmartColumnTarget("LyTrinhKm", new[] { "lý trình", "lý trình (km)", "lytrinh", "km", "lytrinhkm", "cự ly" }),
            new SmartColumnTarget("TinhThanh", new[] { "tỉnh / thành phố", "tỉnh thành", "tỉnh", "thành phố", "tinhthanh", "province" }),
            new SmartColumnTarget("HangGa", new[] { "phân cấp ga", "phân cấp", "hạng ga", "hạng", "hangga", "cấp ga", "class" }),
            new SmartColumnTarget("CoCauQuay", new[] { "cầu quay", "cocauquay", "cauquay", "quay đầu", "turntable" }),
            new SmartColumnTarget("DangKhaiThac", new[] { "trạng thái khai thác", "khai thác", "trạng thái", "dangkhaithac", "hoạt động", "status" })
        };

        public bool HoTroCapNhat => true;

        public void ChuanBiDuLieuDoiChieu()
        {
            _existingGaByCode.Clear();
            _existingGaByKm.Clear();
            _seenCodesInFile.Clear();

            try
            {
                var dt = _gaService.LayDanhSach();
                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        int maGa = Convert.ToInt32(r["MaGa"]);
                        string code = r["MaGaCode"]?.ToString()?.Trim() ?? "";
                        string ten = r["TenGa"]?.ToString()?.Trim() ?? "";
                        decimal km = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m;

                        if (!string.IsNullOrEmpty(code))
                        {
                            _existingGaByCode[code] = (maGa, ten, km);
                        }

                        if (!_existingGaByKm.ContainsKey(km))
                        {
                            _existingGaByKm[km] = (maGa, ten, code);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public void LamMoiTrangThaiDuyetTep()
        {
            _seenCodesInFile.Clear();
        }

        public ImportRowBase KiemTraVaPhanTichDongThongMinh(SmartRowData row, int soDong, bool choPhepCapNhat = false)
        {
            var item = new ImportGaItem
            {
                Dong = soDong,
                HopLe = true,
                ThongBaoKiemTra = "Hợp lệ — Sẵn sàng thêm mới vào CSDL"
            };

            // 1. Trích xuất toàn bộ dữ liệu trường hiển thị trước để bảng Preview luôn đầy đủ
            string rawCode = row.Get("MaGaCode").ToUpperInvariant();
            item.MaGaCode = rawCode;

            string rawTen = row.Get("TenGa");
            item.TenGa = rawTen;

            string rawKm = row.Get("LyTrinhKm");
            bool kmHopLe = false;
            if (!string.IsNullOrWhiteSpace(rawKm))
            {
                if (FileExchangeHelper.ThuChuyenDoiDecimal(rawKm, out decimal kmVal) && kmVal >= 0)
                {
                    item.LyTrinhKm = kmVal;
                    kmHopLe = true;
                }
            }
            else
            {
                kmHopLe = true; // Cho phép lý trình rỗng nếu cần
            }

            item.TinhThanh = row.Get("TinhThanh");
            if (string.IsNullOrWhiteSpace(item.TinhThanh))
            {
                item.TinhThanh = "Chưa xác định";
            }

            string rawHang = row.Get("HangGa").ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(rawHang))
            {
                item.HangGa = (rawHang == "1" || rawHang.Contains("HANG_1") || rawHang.Contains("HẠNG 1") || rawHang.Contains("HẠNG I") || rawHang == "I") 
                    ? "HANG_1" 
                    : ((rawHang == "2" || rawHang.Contains("HANG_2") || rawHang.Contains("HẠNG 2") || rawHang.Contains("HẠNG II") || rawHang == "II") 
                        ? "HANG_2" 
                        : "HANG_3");
            }

            string rawQuay = row.Get("CoCauQuay").ToLowerInvariant();
            item.CoCauQuay = (rawQuay == "1" || rawQuay == "true" || rawQuay == "có" || rawQuay == "co" || rawQuay == "yes");

            string rawKt = row.Get("DangKhaiThac").ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(rawKt))
            {
                item.DangKhaiThac = !(rawKt == "0" || rawKt == "false" || rawKt.Contains("ngừng") || rawKt.Contains("ngung") || rawKt == "no" || rawKt.Contains("đóng"));
            }

            // 2. Thẩm định tính hợp lệ
            if (string.IsNullOrWhiteSpace(rawCode))
            {
                item.HopLe = false;
                item.ThongBaoKiemTra = "Mã Ga không được để trống";
                return item;
            }

            if (rawCode.Length > 10)
            {
                item.HopLe = false;
                item.ThongBaoKiemTra = $"Mã Ga '{rawCode}' vượt quá độ dài tối đa 10 ký tự";
                return item;
            }

            if (!Regex.IsMatch(rawCode, @"^[A-Z0-9_\-]+$"))
            {
                item.HopLe = false;
                item.ThongBaoKiemTra = "Mã Ga chỉ chứa chữ in hoa không dấu, số hoặc gạch ngang";
                return item;
            }

            // Kiểm tra trùng lặp trong nội bộ file (được reset sạch mỗi lần duyệt)
            if (!_seenCodesInFile.Add(rawCode))
            {
                item.HopLe = false;
                item.ThongBaoKiemTra = $"Mã Ga '{rawCode}' bị trùng lặp nhiều lần trong tệp này";
                return item;
            }

            if (string.IsNullOrWhiteSpace(rawTen))
            {
                item.HopLe = false;
                item.ThongBaoKiemTra = "Tên Ga không được để trống";
                return item;
            }

            if (!kmHopLe)
            {
                item.HopLe = false;
                item.ThongBaoKiemTra = $"Lý trình '{rawKm}' không hợp lệ (phải là số >= 0)";
                return item;
            }

            // 3. Kiểm tra đối chiếu với CSDL và chế độ cập nhật
            if (_existingGaByCode.TryGetValue(rawCode, out var existingGa))
            {
                item.MaGa = existingGa.MaGa;

                if (!choPhepCapNhat)
                {
                    item.HopLe = false;
                    item.LaCapNhat = false;
                    item.ThongBaoKiemTra = $"Mã Ga '{rawCode}' đã có trong CSDL ({existingGa.TenGa} - Km {existingGa.LyTrinh:N2}) — Bị trùng lặp";
                }
                else
                {
                    item.HopLe = true;
                    item.LaCapNhat = true;
                    item.ThongBaoKiemTra = $"Đã có trong CSDL ({existingGa.TenGa}) — Sẵn sàng cập nhật thông số";
                }
            }
            else
            {
                // Ga mới: kiểm tra xem Lý trình có bị đè lên ga khác trên tuyến không
                if (_existingGaByKm.TryGetValue(item.LyTrinhKm, out var clashGa))
                {
                    item.HopLe = false;
                    item.ThongBaoKiemTra = $"Lý trình Km {item.LyTrinhKm:N2} đã trùng với Ga '{clashGa.TenGa}' [{clashGa.Code}] trên tuyến";
                }
                else
                {
                    item.HopLe = true;
                    item.LaCapNhat = false;
                    item.ThongBaoKiemTra = "Hợp lệ — Sẵn sàng thêm mới vào CSDL";
                }
            }

            return item;
        }

        public ImportRowBase KiemTraVaPhanTichDong(string[] tokens, int soDong)
        {
            var smartRow = new SmartRowData { SourceLineNumber = soDong, RawTokens = tokens };
            for (int i = 0; i < CacCotMucTieu.Count && i < tokens.Length; i++)
            {
                smartRow.Fields[CacCotMucTieu[i].PropertyKey] = tokens[i];
            }
            return KiemTraVaPhanTichDongThongMinh(smartRow, soDong, false);
        }

        public int LuuDuLieuVaoCSDL(IEnumerable<ImportRowBase> danhSachHopLe, bool choPhepCapNhat, out string thongBao)
        {
            int thanhCongThem = 0;
            int thanhCongCapNhat = 0;
            var loiList = new List<string>();

            foreach (var row in danhSachHopLe)
            {
                if (row is not ImportGaItem item) continue;

                var ga = new Ga
                {
                    MaGa = item.MaGa,
                    MaTuyen = 1, // Tuyến Đường Sắt Bắc Nam
                    MaGaCode = item.MaGaCode,
                    TenGa = item.TenGa,
                    LyTrinhKm = item.LyTrinhKm,
                    TinhThanh = item.TinhThanh,
                    HangGa = item.HangGa,
                    CoCauQuay = item.CoCauQuay,
                    DangKhaiThac = item.DangKhaiThac,
                    NguoiTao = "import_excel",
                    NguoiCapNhat = "import_excel"
                };

                if (item.LaCapNhat && choPhepCapNhat && item.MaGa > 0)
                {
                    if (_gaService.CapNhat(ga, out string err))
                    {
                        thanhCongCapNhat++;
                    }
                    else
                    {
                        loiList.Add($"Cập nhật dòng {item.Dong} ({item.MaGaCode}): {err}");
                    }
                }
                else
                {
                    if (_gaService.Them(ga, out string err))
                    {
                        thanhCongThem++;
                        _existingGaByCode[item.MaGaCode] = (ga.MaGa, item.TenGa, item.LyTrinhKm);
                    }
                    else
                    {
                        loiList.Add($"Thêm mới dòng {item.Dong} ({item.MaGaCode}): {err}");
                    }
                }
            }

            int tongThanhCong = thanhCongThem + thanhCongCapNhat;

            if (loiList.Count > 0)
            {
                thongBao = $"Thành công: {tongThanhCong} (Thêm: {thanhCongThem}, Cập nhật: {thanhCongCapNhat}), Gặp lỗi: {loiList.Count}\n" + string.Join("\n", loiList.Take(5));
            }
            else
            {
                thongBao = $"Hoàn tất thành công: Thêm mới {thanhCongThem} ga, Cập nhật {thanhCongCapNhat} ga vào mạng lưới đường sắt.";
            }

            return tongThanhCong;
        }

        public int LuuDuLieuVaoCSDL(IEnumerable<ImportRowBase> danhSachHopLe, out string thongBao)
        {
            return LuuDuLieuVaoCSDL(danhSachHopLe, false, out thongBao);
        }
    }
}
