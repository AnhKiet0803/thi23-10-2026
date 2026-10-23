using Microsoft.AspNetCore.Mvc;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;
using smartphone.Data;
using smartphone.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace smartphone.Controllers;

public record AnalyzeReviewRequest(int ProductId, string CustomerName, string Content, int Rating);

public class SentimentResult
{
    [JsonPropertyName("sentiment")]
    public string Sentiment { get; set; } = "Neutral";

    [JsonPropertyName("score")]
    public float Score { get; set; } = 0.5f;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly GoogleAI _googleAi;

    public ReviewsController(AppDbContext context, GoogleAI googleAi)
    {
        _context = context;
        _googleAi = googleAi;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeReview([FromBody] AnalyzeReviewRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
            return BadRequest(new { message = "Invalid ProductId. Product not found." });

        SentimentResult? analysis = null;

        try
        {
            var model = _googleAi.GenerativeModel(
                model: "gemini-3.8-flash",
                systemInstruction: new Content("You are an AI customer review analyzer. You must output raw JSON ONLY without markdown wrapping or backticks. Schema: {\"sentiment\": \"Positive\" | \"Negative\" | \"Neutral\", \"score\": 0.95, \"summary\": \"short summary\"}.")
            );

            var prompt = $"Product: {product.ProductName}\nCustomer: {request.CustomerName}\nRating: {request.Rating}/5\nReview: \"{request.Content}\"";
            var response = await model.GenerateContent(prompt);

            var rawJson = response.Text?.Trim()
                .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                .Replace("```", "")
                .Trim() ?? "{}";

            analysis = JsonSerializer.Deserialize<SentimentResult>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            analysis = new SentimentResult
            {
                Sentiment = request.Rating >= 4 ? "Positive" : (request.Rating <= 2 ? "Negative" : "Neutral"),
                Score = 0.5f,
                Summary = request.Content.Length > 50 ? request.Content[..50] + "..." : request.Content
            };
        }

        var review = new Review
        {
            ProductId = request.ProductId,
            CustomerName = request.CustomerName,
            Content = request.Content,
            Rating = request.Rating,
            Sentiment = analysis?.Sentiment ?? "Neutral",
            Score = analysis?.Score ?? 0.5f,
            Summary = analysis?.Summary ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return Ok(review);
    }
}