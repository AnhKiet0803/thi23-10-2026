using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smartphone.Models;

public class Review
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReviewId { get; set; }

    public int ProductId { get; set; }

    [MaxLength(64)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public int Rating { get; set; }

    [MaxLength(16)]
    public string? Sentiment { get; set; }

    public float? Score { get; set; }

    [MaxLength(256)]
    public string? Summary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product? Product { get; set; }
}