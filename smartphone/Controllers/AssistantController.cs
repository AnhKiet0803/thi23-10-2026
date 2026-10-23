using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smartphone.Data;

namespace smartphone.Controllers;

public record AssistantChatRequest(string SessionId, string Message);

[ApiController]
[Route("api/assistant")]
public class AssistantController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    // Lưu trữ lịch sử hội thoại cho từng SessionId trong RAM
    private static readonly ConcurrentDictionary<string, List<object>> ChatHistories = new();

    public AssistantController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "SessionId and Message are required." });

        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(500, new { message = "Gemini API key is not configured." });

        try
        {
            // Lấy danh sách sản phẩm từ DB tạo catalogue ngữ cảnh
            var products = await _context.Products.AsNoTracking().ToListAsync();
            var catalog = string.Join("\n", products.Select(p =>
                $"- Name: {p.ProductName} | Category: {p.Category} | Price: {p.Price:N0} VND | Features: {p.Features} | Description: {p.Description ?? "No description"}"));

            var systemInstruction = $@"You are the AI shopping assistant of Smart Shop.
You ONLY answer questions using the following product catalog:
{catalog}

Rules:
1. If the question is not about the products above, politely decline to answer.
2. Maintain conversational context so users can ask follow-up questions.";

            // Lấy hoặc tạo mới lịch sử chat của session
            var history = ChatHistories.GetOrAdd(request.SessionId, _ => new List<object>());

            lock (history)
            {
                // Thêm câu hỏi hiện tại của user vào lịch sử
                history.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = request.Message } }
                });
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={apiKey}";

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents = history.ToArray()
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { message = "Gemini API error", detail = responseBody });
            }

            using var doc = JsonDocument.Parse(responseBody);
            var reply = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()?.Trim();

            lock (history)
            {
                history.Add(new
                {
                    role = "model",
                    parts = new[] { new { text = reply } }
                });
            }

            return Ok(new { reply });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error during assistant conversation", error = ex.Message });
        }
    }
}