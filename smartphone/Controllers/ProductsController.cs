using Microsoft.AspNetCore.Mvc;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;
using smartphone.Data;

namespace smartphone.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly GoogleAI _googleAi;

    public ProductsController(AppDbContext context, GoogleAI googleAi)
    {
        _context = context;
        _googleAi = googleAi;
    }

    [HttpPost("{id}/generatedescription")]
    public async Task<IActionResult> GenerateDescription(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "Product not found" });

        try
        {
            var model = _googleAi.GenerativeModel(
                model: "gemini-3.8-flash",
                systemInstruction: new Content("You are a professional copywriter. Write an attractive product description of at most 80 words based on the product name, category, and features.")
            );

            var prompt = $"Product Name: {product.ProductName}, Category: {product.Category}, Features: {product.Features}";
            var response = await model.GenerateContent(prompt);

            product.Description = response.Text?.Trim();
            await _context.SaveChangesAsync();

            return Ok(product);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error communicating with Gemini AI", error = ex.Message });
        }
    }
}