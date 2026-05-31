using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QuanLySinhVien.Models;

namespace QuanLySinhVien.Services
{
    public interface IGeminiChatService
    {
        Task<ChatResponse> AskAsync(string message, List<ChatTurn>? history, string? username, string? role);
    }

    public class GeminiChatService : IGeminiChatService
    {
        private readonly HttpClient _http;
        private readonly GeminiOptions _opt;
        private readonly ChatBotTools _tools;
        private readonly ILogger<GeminiChatService> _log;

        private const string SystemPrompt = @"Bạn là 'Trợ lý QNU' — chatbot tiếng Việt của hệ thống Quản lý Sinh viên Đại học Quy Nhơn (QNU).

NHIỆM VỤ:
- Trả lời câu hỏi về sinh viên, giảng viên, môn học, lớp học, điểm, học phí, lịch học, học kỳ.
- LUÔN dùng các tool có sẵn để truy vấn dữ liệu thật từ database. TUYỆT ĐỐI KHÔNG được bịa số liệu.

QUY TẮC:
1. Khi user hỏi 'của tôi/của em/của mình' → GỌI getCurrentUser TRƯỚC để biết MSSV/MaGv của họ.
2. Nếu user cung cấp MSSV (chuỗi số) → dùng trực tiếp với các tool getStudent*.
3. Nếu user cho tên → dùng searchStudents trước để lấy MSSV.
4. Nếu tool trả về notFound=true → báo cho user biết rõ rằng không tìm thấy, KHÔNG bịa dữ liệu.
5. Format câu trả lời bằng Markdown: dùng **đậm**, danh sách bằng dấu -, bảng `| col |` khi liệt kê >2 hàng có nhiều cột.
6. Trả lời ngắn gọn, tự nhiên, thân thiện. Không cần liệt kê tool đã gọi.
7. Đơn vị tiền tệ: VND, hiển thị có dấu phẩy (vd: 250,000 VND).
8. Ngày dạng dd/MM/yyyy.

NẾU user chào hỏi hoặc hỏi 'làm được gì' → giải thích ngắn gọn khả năng của bạn.";

        public GeminiChatService(HttpClient http, IOptions<GeminiOptions> opt, ChatBotTools tools, ILogger<GeminiChatService> log)
        {
            _http = http;
            _opt = opt.Value;
            _tools = tools;
            _log = log;
        }

        public async Task<ChatResponse> AskAsync(string message, List<ChatTurn>? history, string? username, string? role)
        {
            if (string.IsNullOrWhiteSpace(_opt.ApiKey) || _opt.ApiKey.StartsWith("PASTE_"))
                return new ChatResponse { Success = false, Reply = "Chưa cấu hình Gemini API Key. Vui lòng thêm vào appsettings.json -> Gemini:ApiKey.", Error = "MissingApiKey" };

            // Bổ sung context user vào system prompt
            var systemInstr = SystemPrompt;
            if (!string.IsNullOrEmpty(username))
                systemInstr += $"\n\nUSER ĐANG ĐĂNG NHẬP: tên đăng nhập = '{username}', vai trò = '{role ?? "?"}'.";

            // Xây list contents (lịch sử + câu mới)
            var contents = new List<object>();
            if (history != null)
            {
                foreach (var t in history.TakeLast(10))
                {
                    contents.Add(new
                    {
                        role = t.Role == "model" ? "model" : "user",
                        parts = new object[] { new { text = t.Text } }
                    });
                }
            }
            contents.Add(new
            {
                role = "user",
                parts = new object[] { new { text = message } }
            });

            var toolsUsed = new List<string>();

            for (int iter = 0; iter < _opt.MaxToolIterations; iter++)
            {
                var payload = new
                {
                    system_instruction = new { parts = new[] { new { text = systemInstr } } },
                    contents,
                    tools = new[] { new { function_declarations = ChatBotTools.Declarations } },
                    tool_config = new { function_calling_config = new { mode = "AUTO" } },
                    generation_config = new { temperature = _opt.Temperature }
                };

                var url = $"{_opt.BaseUrl}/models/{_opt.Model}:generateContent?key={_opt.ApiKey}";
                JsonElement root;
                try
                {
                    using var resp = await _http.PostAsJsonAsync(url, payload);
                    var body = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode)
                    {
                        _log.LogWarning("Gemini API error {Status}: {Body}", resp.StatusCode, body);
                        return new ChatResponse
                        {
                            Success = false,
                            Reply = $"Gemini API lỗi ({(int)resp.StatusCode}). Kiểm tra API key / quota.",
                            Error = body
                        };
                    }
                    using var doc = JsonDocument.Parse(body);
                    root = doc.RootElement.Clone();
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Gọi Gemini thất bại");
                    return new ChatResponse { Success = false, Reply = "Không kết nối được Gemini API: " + ex.Message, Error = ex.Message };
                }

                if (!root.TryGetProperty("candidates", out var cands) || cands.GetArrayLength() == 0)
                {
                    return new ChatResponse { Success = false, Reply = "Gemini không trả về câu trả lời nào.", Error = root.ToString() };
                }

                var content = cands[0].GetProperty("content");
                var parts = content.GetProperty("parts");

                // Gom tất cả functionCalls trong response (có thể có nhiều)
                var fnCalls = new List<(string name, JsonElement args)>();
                var textBuilder = new System.Text.StringBuilder();

                foreach (var p in parts.EnumerateArray())
                {
                    if (p.TryGetProperty("functionCall", out var fc))
                    {
                        var name = fc.GetProperty("name").GetString() ?? "";
                        var args = fc.TryGetProperty("args", out var a) ? a.Clone() : default;
                        fnCalls.Add((name, args));
                    }
                    else if (p.TryGetProperty("text", out var t))
                    {
                        textBuilder.Append(t.GetString());
                    }
                }

                // Nếu không có function call → đây là câu trả lời cuối
                if (fnCalls.Count == 0)
                {
                    var reply = textBuilder.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(reply))
                        reply = "Mình chưa nắm rõ câu hỏi, anh/chị thử diễn đạt lại giúp em nhé.";
                    return new ChatResponse { Success = true, Reply = reply, ToolsUsed = toolsUsed };
                }

                // Có function call → thêm model turn + thực thi từng tool và gắn functionResponse
                var modelParts = new List<object>();
                foreach (var (n, a) in fnCalls)
                {
                    modelParts.Add(new { functionCall = new { name = n, args = ToObject(a) } });
                }
                contents.Add(new { role = "model", parts = modelParts });

                var fnRespParts = new List<object>();
                foreach (var (n, a) in fnCalls)
                {
                    toolsUsed.Add(n);
                    var result = await _tools.ExecuteAsync(n, a, username, role);
                    fnRespParts.Add(new
                    {
                        functionResponse = new
                        {
                            name = n,
                            response = new { result }
                        }
                    });
                }
                contents.Add(new { role = "user", parts = fnRespParts });
            }

            return new ChatResponse
            {
                Success = false,
                Reply = "Câu hỏi quá phức tạp — đã vượt số lần truy vấn tối đa.",
                ToolsUsed = toolsUsed
            };
        }

        /// <summary>Chuyển JsonElement → object để serialize lại sang JSON khi gửi đi.</summary>
        private static object? ToObject(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var p in el.EnumerateObject()) dict[p.Name] = ToObject(p.Value);
                    return dict;
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var i in el.EnumerateArray()) list.Add(ToObject(i));
                    return list;
                case JsonValueKind.String: return el.GetString();
                case JsonValueKind.Number:
                    if (el.TryGetInt64(out var l)) return l;
                    return el.GetDouble();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default: return null;
            }
        }
    }
}
