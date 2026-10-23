using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;
using smartphone.Data;
using System.Collections.Concurrent;

namespace smartphone.Controllers;

public record AssistantChatRequest(string SessionId, string Message);

[ApiController]
[Route("api/assistant")]
public class AssistantController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly GoogleAI _googleAi;
    private static readonly ConcurrentDictionary<string, ChatSession> Sessions = new();

    public AssistantController(AppDbContext context, GoogleAI googleAi)
    {
        _context = context;
        _googleAi = googleAi;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AssistantChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "SessionId and Message are required." });

        try
        {
            if (!Sessions.TryGetValue(request.SessionId, out var chatSession))
            {
                var products = await _context.Products.AsNoTracking().ToListAsync();
                var catalog = string.Join("\n", products.Select(p =>
                    $"- Name: {p.ProductName} | Category: {p.Category} | Price: {p.Price:N0} VND | Features: {p.Features} | Description: {p.Description ?? "No description"}"));

                var systemInstruction = $@"You are the AI shopping assistant of Smart Shop.
You ONLY answer questions using the following product catalog:
{catalog}

Rules:
1. If the question is not about the products above, politely decline to answer.
2. Maintain conversational context so users can ask follow-up questions.";

                var model = _googleAi.GenerativeModel(
                    model: "gemini-3.8-flash",
                    systemInstruction: new Content(systemInstruction)
                );
            }

            var response = await chatSession.SendMessage(request.Message);
            return Ok(new { reply = response.Text });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error during assistant conversation", error = ex.Message });
        }
    }
}