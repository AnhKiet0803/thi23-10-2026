using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smartphone.Data;

namespace smartphone.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviewReport()
    {
        var rawData = await _context.Products
            .Select(p => new
            {
                p.ProductName,
                TotalReviews = _context.Reviews.Count(r => r.ProductId == p.ProductId),
                Positive = _context.Reviews.Count(r => r.ProductId == p.ProductId && r.Sentiment == "Positive"),
                Negative = _context.Reviews.Count(r => r.ProductId == p.ProductId && r.Sentiment == "Negative"),
                Neutral = _context.Reviews.Count(r => r.ProductId == p.ProductId && r.Sentiment == "Neutral"),
                AverageRating = _context.Reviews.Where(r => r.ProductId == p.ProductId).Select(r => (double?)r.Rating).Average() ?? 0.0
            })
            .ToListAsync();

        var report = rawData.Select((item, idx) => new
        {
            No = idx + 1,
            item.ProductName,
            item.TotalReviews,
            item.Positive,
            item.Negative,
            item.Neutral,
            AverageRating = Math.Round(item.AverageRating, 1)
        });

        return Ok(report);
    }
}