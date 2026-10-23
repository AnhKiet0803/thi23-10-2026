using Microsoft.EntityFrameworkCore;
using smartphone.Models;

namespace smartphone.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().HasData(
            new Product { ProductId = 1, ProductName = "Galaxy S25", Category = "Phone", Price = 22990000m, Features = "6.2 inch AMOLED, Snapdragon 8 Elite, 50MP camera" },
            new Product { ProductId = 2, ProductName = "MacBook Air M4", Category = "Laptop", Price = 28490000m, Features = "13.6 inch, 16GB RAM, 18 hours battery" },
            new Product { ProductId = 3, ProductName = "AirPods Pro 3", Category = "Accessory", Price = 6790000m, Features = "Active noise cancellation, USB-C, IP57" },
            new Product { ProductId = 4, ProductName = "Xiaomi Redmi Note 15", Category = "Phone", Price = 6490000m, Features = "120Hz display, 5500mAh battery, 108MP camera" }
        );
    }
}