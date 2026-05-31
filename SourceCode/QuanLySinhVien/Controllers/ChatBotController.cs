using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySinhVien.Models;
using QuanLySinhVien.Services;
using System.Security.Claims;

namespace QuanLySinhVien.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ChatBotController : Controller
    {
        private readonly IGeminiChatService _chat;

        public ChatBotController(IGeminiChatService chat) { _chat = chat; }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask([FromBody] ChatRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Message))
                return Json(new ChatResponse { Success = false, Reply = "Bạn chưa nhập câu hỏi." });

            var username = User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role);

            var res = await _chat.AskAsync(req.Message.Trim(), req.History, username, role);
            return Json(res);
        }
    }
}
