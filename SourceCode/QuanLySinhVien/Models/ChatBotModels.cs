using System.Text.Json.Serialization;

namespace QuanLySinhVien.Models
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatTurn>? History { get; set; }
    }

    public class ChatTurn
    {
        public string Role { get; set; } = "user"; // "user" | "model"
        public string Text { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public bool Success { get; set; } = true;
        public string Reply { get; set; } = string.Empty;
        public string? Error { get; set; }
        public List<string>? ToolsUsed { get; set; }
    }

    public class GeminiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.0-flash";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
        public int MaxToolIterations { get; set; } = 6;
        public double Temperature { get; set; } = 0.2;
    }
}
