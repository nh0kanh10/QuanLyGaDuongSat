using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;

namespace BUS.AI
{
    // =========================================================================
    // KENH GOI MO HINH NGON NGU (16/11). Tro ly AI tu van ve: tro chuyen nhieu luot + "goi ham" (function calling):
    // moi luot gui lich su chat + danh sach cong cu; mo hinh hoac tra loi ngay, hoac tra ve lenh goi cong cu
    // (ten + tham so JSON) -> TroLyChatService chay truy van that roi gui ket qua o lan goi sau.
    // Kieu du lieu o day trung lap voi nha cung cap (test dung kenh gia); KenhOpenAI doi sang kieu cua thu vien OpenAI.
    // =========================================================================
    public enum VaiTroTinNhan { NguoiDung, TroLy, KetQuaCongCu }

    public sealed record LenhGoiCongCu(string Ma, string Ten, string ThamSoJson);

    public sealed class TinNhanChat
    {
        public VaiTroTinNhan VaiTro { get; init; }
        public string NoiDung { get; init; } = "";
        public IReadOnlyList<LenhGoiCongCu> CacLenh { get; init; } = Array.Empty<LenhGoiCongCu>();   // TroLy goi cong cu
        public string MaLenh { get; init; } = "";                                                     // KetQuaCongCu: tra loi lenh nao

        public static TinNhanChat NguoiDung(string noiDung) => new() { VaiTro = VaiTroTinNhan.NguoiDung, NoiDung = noiDung };
        public static TinNhanChat TroLy(string noiDung, IReadOnlyList<LenhGoiCongCu>? lenh = null)
            => new() { VaiTro = VaiTroTinNhan.TroLy, NoiDung = noiDung, CacLenh = lenh ?? Array.Empty<LenhGoiCongCu>() };
        public static TinNhanChat KetQua(string maLenh, string noiDung)
            => new() { VaiTro = VaiTroTinNhan.KetQuaCongCu, MaLenh = maLenh, NoiDung = noiDung };
    }

    // Cong cu mo hinh duoc goi: ten, mo ta ngan, JSON schema tham so (strict: moi thuoc tinh deu required, additionalProperties false).
    public sealed record CongCuMoHinh(string Ten, string MoTa, IReadOnlyDictionary<string, JsonElement> SchemaThamSo);

    public sealed record PhanHoiMoHinh(string NoiDung, IReadOnlyList<LenhGoiCongCu> CacLenh);

    public interface IKenhMoHinhNgonNgu
    {
        string TenMoHinh { get; }

        // Loi (mang, khoa sai, het han muc, tu choi...) -> nem LoiMoHinhNgonNguException voi cau tieng Viet.
        Task<PhanHoiMoHinh> TroChuyenAsync(string heThong, IReadOnlyList<TinNhanChat> lichSu,
                                           IReadOnlyList<CongCuMoHinh> congCu, CancellationToken ct);
    }

    public sealed class LoiMoHinhNgonNguException : Exception
    {
        public LoiMoHinhNgonNguException(string thongDiep, Exception? goc = null) : base(thongDiep, goc) { }
    }

    // Kenh that: thu vien chinh thuc OpenAI cho .NET (NuGet "OpenAI"), Chat Completions + function calling (strict).
    public sealed class KenhOpenAI : IKenhMoHinhNgonNgu
    {
        // Tran so token tra ve (mo hinh suy luan tinh ca token suy luan vao day).
        private const int SoTokenToiDa = 8000;

        private readonly ChatClient _client;

        public KenhOpenAI(string khoaApi, string moHinh, TimeSpan thoiGianCho)
        {
            if (string.IsNullOrWhiteSpace(khoaApi)) throw new ArgumentException("Thiếu khóa API OpenAI.", nameof(khoaApi));
            TenMoHinh = string.IsNullOrWhiteSpace(moHinh) ? DTO.Common.CauHinhUngDung.MoHinhAIMacDinh : moHinh.Trim();
            var tuyChon = new OpenAIClientOptions
            {
                NetworkTimeout = thoiGianCho,
                RetryPolicy = new ClientRetryPolicy(maxRetries: 1),
            };
            _client = new ChatClient(TenMoHinh, new ApiKeyCredential(khoaApi), tuyChon);
        }

        public string TenMoHinh { get; }

        public async Task<PhanHoiMoHinh> TroChuyenAsync(string heThong, IReadOnlyList<TinNhanChat> lichSu,
                                                        IReadOnlyList<CongCuMoHinh> congCu, CancellationToken ct)
        {
            var tinNhan = new List<ChatMessage> { new SystemChatMessage(heThong) };
            foreach (var t in lichSu)
            {
                switch (t.VaiTro)
                {
                    case VaiTroTinNhan.NguoiDung:
                        tinNhan.Add(new UserChatMessage(t.NoiDung));
                        break;
                    case VaiTroTinNhan.TroLy when t.CacLenh.Count > 0:
                        tinNhan.Add(new AssistantChatMessage(t.CacLenh.Select(l =>
                            ChatToolCall.CreateFunctionToolCall(l.Ma, l.Ten, BinaryData.FromString(l.ThamSoJson)))));
                        break;
                    case VaiTroTinNhan.TroLy:
                        tinNhan.Add(new AssistantChatMessage(t.NoiDung));
                        break;
                    case VaiTroTinNhan.KetQuaCongCu:
                        tinNhan.Add(new ToolChatMessage(t.MaLenh, t.NoiDung));
                        break;
                }
            }

            var tuyChon = new ChatCompletionOptions { MaxOutputTokenCount = SoTokenToiDa };
            foreach (var c in congCu)
            {
                tuyChon.Tools.Add(ChatTool.CreateFunctionTool(
                    functionName: c.Ten,
                    functionDescription: c.MoTa,
                    functionParameters: BinaryData.FromString(JsonSerializer.Serialize(c.SchemaThamSo)),
                    functionSchemaIsStrict: true));
            }

            ChatCompletion traLoi;
            try
            {
                traLoi = (await _client.CompleteChatAsync(tinNhan, tuyChon, ct).ConfigureAwait(false)).Value;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (ClientResultException ex) when (ex.Status != 0)
            {
                throw new LoiMoHinhNgonNguException(ex.Status switch
                {
                    401 => "Khóa API OpenAI không hợp lệ hoặc đã bị thu hồi (kiểm tra \"KhoaApi\" trong appsettings.Local.json).",
                    403 => $"Khóa API không có quyền dùng mô hình {TenMoHinh} (hoặc khu vực bị chặn).",
                    404 => $"Không có mô hình \"{TenMoHinh}\" hoặc tài khoản chưa được dùng — kiểm tra \"MoHinh\" trong appsettings.json.",
                    429 => "OpenAI đang quá tải hoặc tài khoản đã hết hạn mức / hết tiền — thử lại sau hoặc kiểm tra trang Billing.",
                    400 => "OpenAI từ chối yêu cầu: " + ex.Message,
                    >= 500 => "Máy chủ OpenAI đang lỗi — thử lại sau.",
                    _ => "Lỗi khi gọi OpenAI: " + ex.Message
                }, ex);
            }
            catch (Exception ex) when (ex is ClientResultException or HttpRequestException or System.IO.IOException)
            {
                throw new LoiMoHinhNgonNguException("Không kết nối được tới OpenAI — kiểm tra mạng.", ex);
            }
            catch (OperationCanceledException ex)
            {
                // Het thoi gian cho cua kenh (ct cua ta chua huy).
                throw new LoiMoHinhNgonNguException("OpenAI trả lời quá lâu — thử lại.", ex);
            }

            if (!string.IsNullOrEmpty(traLoi.Refusal))
                throw new LoiMoHinhNgonNguException("Mô hình từ chối trả lời câu này.");
            if (traLoi.FinishReason == ChatFinishReason.Length)
                throw new LoiMoHinhNgonNguException("Câu trả lời bị cắt giữa chừng — thử lại hoặc hỏi ngắn hơn.");
            if (traLoi.FinishReason == ChatFinishReason.ContentFilter)
                throw new LoiMoHinhNgonNguException("Câu trả lời bị bộ lọc nội dung của OpenAI chặn.");

            var lenh = traLoi.ToolCalls
                .Select(tc => new LenhGoiCongCu(tc.Id, tc.FunctionName, tc.FunctionArguments?.ToString() ?? "{}"))
                .ToList();
            string vanBan = string.Concat(traLoi.Content.Select(p => p.Text ?? "")).Trim();
            if (lenh.Count == 0 && vanBan.Length == 0) throw new LoiMoHinhNgonNguException("Mô hình không trả về nội dung.");
            return new PhanHoiMoHinh(vanBan, lenh);
        }
    }
}
