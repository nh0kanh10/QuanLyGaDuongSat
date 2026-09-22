using System;
using System.Data;
using System.Text.Json;
using System.Windows;
using Microsoft.Data.SqlClient;
using DAL.Connection;
using DTO.Common;

namespace GUI.Views.Dialogs
{
    public partial class LichSuGaDialog : Window
    {
        public LichSuGaDialog(string maGaCode, string tenGa)
        {
            InitializeComponent();
            txtTieuDe.Text = $"NHẬT KÝ THAY ĐỔI: GA {tenGa.ToUpper()} ({maGaCode.ToUpper()})";
            LoadLichSu(maGaCode);
        }

        private void LoadLichSu(string maGaCode)
        {
            try
            {
                string sql = @"
                    SELECT n.MaNhatKy, 
                           n.ThoiDiem, 
                           n.MaTaiKhoan, 
                           n.HanhDong, 
                           n.TenBang, 
                           n.KhoaChinhRecord, 
                           n.DuLieuCu, 
                           n.DuLieuMoi, 
                           n.DiaChiIP
                    FROM kiemtoan.NhatKyHeThong n
                    WHERE n.TenBang = 'hatang.Ga'
                      AND (n.DuLieuMoi LIKE @code OR n.DuLieuCu LIKE @code)
                    ORDER BY n.ThoiDiem DESC";

                var dt = DatabaseHelper.ExecuteQuery(sql, new[]
                {
                    new SqlParameter("@code", "%\"" + maGaCode + "\"%")
                });

                // Bổ sung các cột hiển thị thân thiện cho DataGrid
                dt.Columns.Add("ThoiGianDisplay", typeof(string));
                dt.Columns.Add("TenDangNhap", typeof(string));
                dt.Columns.Add("ThaoTac", typeof(string));
                dt.Columns.Add("GhiChu", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    // 1. Giờ UTC -> Giờ Việt Nam UTC+7
                    if (row["ThoiDiem"] != DBNull.Value)
                    {
                        DateTime dtUtc = (DateTime)row["ThoiDiem"];
                        row["ThoiGianDisplay"] = FormatHelper.FormatDateTime(dtUtc, fromUtc: true);
                    }
                    else
                    {
                        row["ThoiGianDisplay"] = "—";
                    }

                    // 2. Thao tác điều độ
                    string hanhDong = row["HanhDong"]?.ToString() ?? "";
                    row["ThaoTac"] = hanhDong switch
                    {
                        "SUA_GA" => "Cập nhật ga",
                        "THEM_GA" => "Thêm mới ga",
                        "XOA_GA" => "Xóa ga",
                        _ => hanhDong
                    };

                    // 3. Tài khoản thao tác & Nội dung tóm tắt từ JSON
                    string duLieuMoiStr = row["DuLieuMoi"]?.ToString() ?? "";
                    string nguoiDung = "admin";
                    string tomTat = "";

                    if (!string.IsNullOrEmpty(duLieuMoiStr))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(duLieuMoiStr);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("NguoiCapNhat", out var propUpd) && !string.IsNullOrEmpty(propUpd.GetString()))
                            {
                                nguoiDung = propUpd.GetString()!;
                            }
                            else if (root.TryGetProperty("NguoiTao", out var propTao) && !string.IsNullOrEmpty(propTao.GetString()))
                            {
                                nguoiDung = propTao.GetString()!;
                            }

                            string ten = root.TryGetProperty("TenGa", out var pTen) ? pTen.GetString() ?? "" : "";
                            string lyTrinh = root.TryGetProperty("LyTrinhKm", out var pKm) ? pKm.GetRawText() : "0";
                            bool khaiThac = root.TryGetProperty("DangKhaiThac", out var pKt) && pKt.GetBoolean();
                            bool cauQuay = root.TryGetProperty("CoCauQuay", out var pCq) && pCq.GetBoolean();

                            tomTat = $"Ga {ten} (Km {lyTrinh}) | Trạng thái: {(khaiThac ? "Đang mở" : "Tạm ngừng")} | Cầu quay: {(cauQuay ? "Có" : "Không")}";
                        }
                        catch
                        {
                            tomTat = duLieuMoiStr;
                        }
                    }
                    else
                    {
                        tomTat = "Bản ghi đã bị xóa khỏi hệ thống";
                    }

                    row["TenDangNhap"] = nguoiDung;
                    row["GhiChu"] = tomTat;
                }

                dgNhatKy.ItemsSource = dt.DefaultView;
                txtSoBanGhi.Text = $"{dt.Rows.Count} bản ghi";
            }
            catch (Exception ex)
            {
                ThongBaoDialog.Loi($"Không thể tải lịch sử nhật ký: {ex.Message}", "Lỗi Truy Vấn", this);
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

