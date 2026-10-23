using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using smartphone.Data;

namespace smartphone.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public ProductsController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
    }

    [HttpPost("{id}/generatedescription")]
    public async Task<IActionResult> GenerateDescription(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(500, new { message = "Gemini API key is not configured." });

        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key={apiKey}";

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = "You are a professional copywriter. Write an attractive product description of at most 80 words based on the product name, category, and features." } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = $"Product Name: {product.ProductName}, Category: {product.Category}, Features: {product.Features}" } }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, jsonContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { message = "Gemini API error", detail = responseBody });
            }

            using var doc = JsonDocument.Parse(responseBody);
            var generatedText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()?.Trim();

            product.Description = generatedText;
            await _context.SaveChangesAsync();

            return Ok(product);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error communicating with Gemini AI", error = ex.Message });
        }
    }
}