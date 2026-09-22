using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using BUS.Services;
using ET.HaTang;
using GUI.Helpers;

namespace GUI.Profiles
{
    // Đối tượng dòng dữ liệu xem trước khi nhập Phân đoạn Khu Gian
    
    public class ImportKhuGianRow : ImportRowBase
    {
        public int MaKhuGian { get; set; } = 0;
        public string GaDauCode { get; set; } = "";
        public string GaCuoiCode { get; set; } = "";
        public string GaDauHienThi { get; set; } = "";
        public string GaCuoiHienThi { get; set; } = "";
        public int MaGaDau { get; set; }
        public int MaGaCuoi { get; set; }
        public decimal CuLyKm { get; set; }
        public string CuLyHienThi => $"{CuLyKm:N2} km";
        public int TocDoKhach { get; set; } = 80;
        public string TocDoKhachHienThi => $"{TocDoKhach} km/h";
        public int TocDoHang { get; set; } = 60;
        public string TocDoHangHienThi => $"{TocDoHang} km/h";
        public decimal DoDoc { get; set; } = 0m;
        public string DoDocHienThi => $"{DoDoc:N1} ‰";
        public bool CanMayDay { get; set; } = false;
        public string MayDayHienThi => CanMayDay ? "Có" : "Không";
    }

    // Hồ sơ cấu hình Import danh sách Khu Gian hàng loạt   
    public class KhuGianImportProfile : IImportProfile
    {
        private readonly KhuGianService _khuGianService;
        private readonly GaService _gaService;

        private readonly Dictionary<string, (int MaGa, string TenGa, decimal LyTrinh)> _dicGaByCode = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (int MaGa, string TenGa, decimal LyTrinh)> _dicGaByTen = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, (int MaKhuGian, decimal CuLy, int VmaxK, int VmaxH)> _dictKhuGianTonTai = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _setKhuGianTrongTep = new(StringComparer.OrdinalIgnoreCase);

        public KhuGianImportProfile(KhuGianService? khuGianService = null, GaService? gaService = null)
        {
            _khuGianService = khuGianService ?? new KhuGianService();
            _gaService = gaService ?? new GaService();
        }

        public string TieuDeDialog => "NHẬP DANH SÁCH KHU GIAN ĐƯỜNG SẮT HÀNG LOẠT (IMPORT EXCEL/CSV)";

        public string TenTepMauMacDinh => "Mau_Nhap_Khu_Gian.xlsx";

        public List<string> TieuDeCotMau => new()
        {
            "Mã Ga Đầu", "Mã Ga Cuối", "Cự Ly (km)", "Vmax Khách (km/h)", "Vmax Hàng (km/h)", "Độ Dốc (‰)", "Cần Máy Đẩy"
        };

        public List<string[]> CacDongDuLieuMau => new()
        {
            new[] { "HN", "GBA", "5.00", "80", "60", "0.0", "0" },
            new[] { "GBA", "VDI", "3.90", "80", "60", "0.0", "0" },
            new[] { "VDI", "PLY", "47.10", "90", "70", "0.0", "0" },
            new[] { "PLY", "NDI", "31.00", "90", "70", "0.0", "0" },
            new[] { "NDI", "THO", "89.00", "90", "70", "0.0", "0" },
            new[] { "THO", "VIN", "143.00", "90", "70", "0.0", "0" }
        };

        public List<string>? GhiChuCotMau => new()
        {
            "Mã Ga điểm đầu hoặc Tên ga (ví dụ: HN hoặc Hà Nội)",
            "Mã Ga điểm cuối hoặc Tên ga (ví dụ: GBA hoặc Giáp Bát)",
            "Khoảng cách thực tế giữa 2 ga (km, số thập phân)",
            "Tốc độ tối đa cho tàu khách (km/h, mặc định 80)",
            "Tốc độ tối đa cho tàu hàng (km/h, mặc định 60)",
            "Độ dốc tối đa phân đoạn (đơn vị phần nghìn ‰, ví dụ 0.0)",
            "Cần máy đẩy trợ lực: 1 (Có), 0 (Không)"
        };

        public string NoiDungCsvMau =>
            @"Mã Ga Đầu,Mã Ga Cuối,Cự Ly (km),Vmax Khách (km/h),Vmax Hàng (km/h),Độ Dốc (‰),Cần Máy Đẩy
            HN,GBA,5.00,80,60,0.0,0
            GBA,VDI,3.90,80,60,0.0,0
            VDI,PLY,47.10,90,70,0.0,0
            PLY,NDI,31.00,90,70,0.0,0
            NDI,THO,89.00,90,70,0.0,0
            THO,VIN,143.00,90,70,0.0,0";

        public List<ImportColumnDefinition> CacCotPreview => new()
        {
            new ImportColumnDefinition("Ga Đầu", nameof(ImportKhuGianRow.GaDauHienThi), 130, TextAlignment.Left, isBold: true),
            new ImportColumnDefinition("Ga Cuối", nameof(ImportKhuGianRow.GaCuoiHienThi), 130, TextAlignment.Left, isBold: true),
            new ImportColumnDefinition("Cự Ly", nameof(ImportKhuGianRow.CuLyHienThi), 95, TextAlignment.Right, isConsolas: true),
            new ImportColumnDefinition("Vmax Khách", nameof(ImportKhuGianRow.TocDoKhachHienThi), 95, TextAlignment.Right, isConsolas: true),
            new ImportColumnDefinition("Vmax Hàng", nameof(ImportKhuGianRow.TocDoHangHienThi), 95, TextAlignment.Right, isConsolas: true),
            new ImportColumnDefinition("Độ Dốc", nameof(ImportKhuGianRow.DoDocHienThi), 85, TextAlignment.Right, isConsolas: true),
            new ImportColumnDefinition("Cần Đẩy", nameof(ImportKhuGianRow.MayDayHienThi), 75, TextAlignment.Center)
        };

        public List<SmartColumnTarget> CacCotMucTieu => new()
        {
            new SmartColumnTarget("GaDauCode", new[] { "mã ga đầu", "ga đầu", "mã ga 1", "ga 1", "magadau", "magadaucode", "mã ga đi", "ga đi", "tên ga đầu" }, isRequired: true),
            new SmartColumnTarget("GaCuoiCode", new[] { "mã ga cuối", "ga cuối", "mã ga 2", "ga 2", "magacuoi", "magacuoicode", "mã ga đến", "ga đến", "tên ga cuối" }, isRequired: true),
            new SmartColumnTarget("CuLyKm", new[] { "cự ly (km)", "cự ly", "khoảng cách", "culy", "culykm", "km", "khoảng cách km" }),
            new SmartColumnTarget("TocDoKhach", new[] { "vmax khách (km/h)", "vmax khách", "tốc độ khách", "tocdokhach", "vmax" }),
            new SmartColumnTarget("TocDoHang", new[] { "vmax hàng (km/h)", "vmax hàng", "tốc độ hàng", "tocdohang" }),
            new SmartColumnTarget("DoDoc", new[] { "độ dốc (‰)", "độ dốc", "dốc", "dopermil", "dodoc", "độ dốc permil" }),
            new SmartColumnTarget("CanMayDay", new[] { "cần máy đẩy", "máy đẩy", "canmayday", "đầu máy đẩy", "trợ lực" })
        };

        public bool HoTroCapNhat => true;

        public void ChuanBiDuLieuDoiChieu()
        {
            _dicGaByCode.Clear();
            _dicGaByTen.Clear();
            _dictKhuGianTonTai.Clear();
            _setKhuGianTrongTep.Clear();

            try
            {
                var dtGa = _gaService.LayDanhSach();
                if (dtGa != null)
                {
                    foreach (DataRow r in dtGa.Rows)
                    {
                        int maGa = Convert.ToInt32(r["MaGa"]);
                        string code = r["MaGaCode"]?.ToString()?.Trim() ?? "";
                        string ten = r["TenGa"]?.ToString()?.Trim() ?? "";
                        decimal lyTrinh = r["LyTrinhKm"] != DBNull.Value ? Convert.ToDecimal(r["LyTrinhKm"]) : 0m;

                        if (!string.IsNullOrEmpty(code) && !_dicGaByCode.ContainsKey(code))
                            _dicGaByCode[code] = (maGa, ten, lyTrinh);

                        if (!string.IsNullOrEmpty(ten) && !_dicGaByTen.ContainsKey(ten))
                            _dicGaByTen[ten] = (maGa, ten, lyTrinh);
                    }
                }

                var dtKg = _khuGianService.LayDanhSach();
                if (dtKg != null)
                {
                    foreach (DataRow r in dtKg.Rows)
                    {
                        int maKg = Convert.ToInt32(r["MaKhuGian"]);
                        int d = Convert.ToInt32(r["MaGaDau"]);
                        int c = Convert.ToInt32(r["MaGaCuoi"]);
                        decimal culy = Convert.ToDecimal(r["CuLyKm"]);
                        int vk = Convert.ToInt32(r["TocDoToiDaKhach"]);
                        int vh = Convert.ToInt32(r["TocDoToiDaHang"]);

                        int minG = Math.Min(d, c);
                        int maxG = Math.Max(d, c);
                        _dictKhuGianTonTai[$"{minG}_{maxG}"] = (maKg, culy, vk, vh);
                    }
                }
            }
            catch
            {
            }
        }

        public void LamMoiTrangThaiDuyetTep()
        {
            _setKhuGianTrongTep.Clear();
        }

        public ImportRowBase KiemTraVaPhanTichDongThongMinh(SmartRowData row, int soDong, bool choPhepCapNhat = false)
        {
            var item = new ImportKhuGianRow
            {
                Dong = soDong,
                HopLe = true,
                ThongBaoKiemTra = "Hợp lệ — Sẵn sàng thêm mới vào CSDL"
            };

            item.GaDauCode = row.Get("GaDauCode");
            item.GaCuoiCode = row.Get("GaCuoiCode");

            if (string.IsNullOrWhiteSpace(item.GaDauCode) || string.IsNullOrWhiteSpace(item.GaCuoiCode))
            {
                item.HopLe = false;
                item.ThongBaoKiemTra = "Thiếu thông tin Ga Đầu hoặc Ga Cuối";
                return item;
            }

            // 1. Kiểm tra Ga Đầu
            int maGaDau = 0;
            string tenGaDau = item.GaDauCode;
            decimal lyTrinhDau = 0m;

            if (_dicGaByCode.TryGetValue(item.GaDauCode, out var gaDauInfo) || _dicGaByTen.TryGetValue(item.GaDauCode, out gaDauInfo))
            {
                maGaDau = gaDauInfo.MaGa;
                tenGaDau = gaDauInfo.TenGa;
                lyTrinhDau = gaDauInfo.LyTrinh;
                item.GaDauHienThi = $"[{gaDauInfo.MaGa}] {tenGaDau}";
            }
            else
            {
                item.HopLe = false;
                item.ThongBaoKiemTra = $"Ga đầu '{item.GaDauCode}' không tồn tại trong CSDL.";
                item.GaDauHienThi = item.GaDauCode;
            }

            // 2. Kiểm tra Ga Cuối
            int maGaCuoi = 0;
            string tenGaCuoi = item.GaCuoiCode;
            decimal lyTrinhCuoi = 0m;

            if (_dicGaByCode.TryGetValue(item.GaCuoiCode, out var gaCuoiInfo) || _dicGaByTen.TryGetValue(item.GaCuoiCode, out gaCuoiInfo))
            {
                maGaCuoi = gaCuoiInfo.MaGa;
                tenGaCuoi = gaCuoiInfo.TenGa;
                lyTrinhCuoi = gaCuoiInfo.LyTrinh;
                item.GaCuoiHienThi = $"[{gaCuoiInfo.MaGa}] {tenGaCuoi}";
            }
            else
            {
                item.HopLe = false;
                item.ThongBaoKiemTra = $"Ga cuối '{item.GaCuoiCode}' không tồn tại trong CSDL.";
                item.GaCuoiHienThi = item.GaCuoiCode;
            }

            item.MaGaDau = maGaDau;
            item.MaGaCuoi = maGaCuoi;

            // 3. Ga đầu trùng ga cuối
            if (item.HopLe && maGaDau == maGaCuoi)
            {
                item.HopLe = false;
                item.ThongBaoKiemTra = "Ga đầu và Ga cuối trùng nhau.";
            }

            // 4. Cự ly (km)
            string rawKm = row.Get("CuLyKm");
            if (!string.IsNullOrWhiteSpace(rawKm))
            {
                if (FileExchangeHelper.ThuChuyenDoiDecimal(rawKm, out decimal culy) && culy > 0)
                {
                    item.CuLyKm = culy;
                }
                else
                {
                    item.HopLe = false;
                    item.ThongBaoKiemTra = $"Cự ly '{row.Get("CuLyKm")}' không hợp lệ (phải là số > 0).";
                }
            }
            else if (item.HopLe)
            {
                // Tự động tính chênh lệch lý trình nếu không nhập cự ly
                decimal autoKm = Math.Abs(lyTrinhCuoi - lyTrinhDau);
                if (autoKm > 0)
                {
                    item.CuLyKm = autoKm;
                }
                else
                {
                    item.HopLe = false;
                    item.ThongBaoKiemTra = "Cự ly phân đoạn không hợp lệ (phải > 0 km).";
                }
            }

            // 5. Tốc độ khách
            string rawVk = row.Get("TocDoKhach");
            if (FileExchangeHelper.ThuChuyenDoiInt(rawVk, out int vk) && vk > 0)
                item.TocDoKhach = vk;
            else
                item.TocDoKhach = 80;

            // 6. Tốc độ hàng
            string rawVh = row.Get("TocDoHang");
            if (FileExchangeHelper.ThuChuyenDoiInt(rawVh, out int vh) && vh > 0)
                item.TocDoHang = vh;
            else
                item.TocDoHang = 60;

            // 7. Độ dốc
            string rawDd = row.Get("DoDoc");
            if (FileExchangeHelper.ThuChuyenDoiDecimal(rawDd, out decimal dd) && dd >= 0)
                item.DoDoc = dd;
            else
                item.DoDoc = 0m;

            // 8. Cần máy đẩy
            string rawMd = row.Get("CanMayDay").ToLowerInvariant();
            item.CanMayDay = (rawMd == "1" || rawMd == "true" || rawMd.Contains("có") || rawMd.Contains("co") || rawMd == "yes");

            // 9. Kiểm tra trùng lặp trong tệp & CSDL
            if (item.HopLe)
            {
                int minG = Math.Min(maGaDau, maGaCuoi);
                int maxG = Math.Max(maGaDau, maGaCuoi);
                string keyKhuGian = $"{minG}_{maxG}";

                if (!_setKhuGianTrongTep.Add(keyKhuGian))
                {
                    item.HopLe = false;
                    item.ThongBaoKiemTra = "Trùng lặp với một dòng phân đoạn khác trong cùng tệp tải lên.";
                }
                else if (_dictKhuGianTonTai.TryGetValue(keyKhuGian, out var existingKg))
                {
                    item.MaKhuGian = existingKg.MaKhuGian;

                    if (!choPhepCapNhat)
                    {
                        item.HopLe = false;
                        item.LaCapNhat = false;
                        item.ThongBaoKiemTra = $"Phân đoạn giữa [{tenGaDau}] và [{tenGaCuoi}] đã có trong CSDL — Bị trùng lặp";
                    }
                    else
                    {
                        item.HopLe = true;
                        item.LaCapNhat = true;
                        item.ThongBaoKiemTra = $"Đã có trong CSDL — Sẵn sàng cập nhật thông số (Vmax, Cự ly, Độ dốc)";
                    }
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
                if (row is not ImportKhuGianRow item) continue;

                var kg = new KhuGian
                {
                    MaKhuGian = item.MaKhuGian,
                    MaGaDau = item.MaGaDau,
                    MaGaCuoi = item.MaGaCuoi,
                    CuLyKm = item.CuLyKm,
                    TocDoToiDaKhach = item.TocDoKhach,
                    TocDoToiDaHang = item.TocDoHang,
                    DoDocPermil = item.DoDoc,
                    CanDauMayDay = item.CanMayDay,
                    TrangThai = "RONG"
                };

                if (item.LaCapNhat && choPhepCapNhat && item.MaKhuGian > 0)
                {
                    if (_khuGianService.CapNhat(kg, out string err))
                    {
                        thanhCongCapNhat++;
                    }
                    else
                    {
                        loiList.Add($"Cập nhật dòng {item.Dong} ({item.GaDauCode} - {item.GaCuoiCode}): {err}");
                    }
                }
                else
                {
                    if (_khuGianService.Them(kg, out string err))
                    {
                        thanhCongThem++;
                        int minG = Math.Min(item.MaGaDau, item.MaGaCuoi);
                        int maxG = Math.Max(item.MaGaDau, item.MaGaCuoi);
                        _dictKhuGianTonTai[$"{minG}_{maxG}"] = (kg.MaKhuGian, kg.CuLyKm, kg.TocDoToiDaKhach, kg.TocDoToiDaHang);
                    }
                    else
                    {
                        loiList.Add($"Thêm mới dòng {item.Dong} ({item.GaDauCode} - {item.GaCuoiCode}): {err}");
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
                thongBao = $"Hoàn tất: Đã thêm mới {thanhCongThem} phân đoạn, Cập nhật {thanhCongCapNhat} phân đoạn vào CSDL.";
            }

            return tongThanhCong;
        }

        public int LuuDuLieuVaoCSDL(IEnumerable<ImportRowBase> danhSachHopLe, out string thongBao)
        {
            return LuuDuLieuVaoCSDL(danhSachHopLe, false, out thongBao);
        }
    }
}
