using ReactCore.Backend.Models;

namespace ReactCore.Backend.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // EnsureCreated creates the DB if not exists, but doesn't run migrations if DB exists.
        // Since we use migrations, we should use Migrate().
        // But for seeding, we can check if users exist.

        // context.Database.Migrate(); // Usually handled in Program.cs or separate script

        if (!context.Users.Any())
        {
            var users = new User[]
            {
                new User
                {
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = "admin"
                },
                new User
                {
                    Email = "user@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                    Role = "user"
                }
            };

            foreach (var u in users)
            {
                context.Users.Add(u);
            }

            context.SaveChanges();
        }

        if (!context.Products.Any())
        {
            var now = DateTime.UtcNow;
            var products = new Product[]
            {
                new Product
                {
                    Name = "Laptop 15\"",
                    Description = "High-performance laptop",
                    Price = 999.99m,
                    Category = "Electronics",
                    ImageUrl = "https://example.com/laptop.jpg",
                    Specifications = "{\"cpu\":\"Intel i7\",\"ram\":\"16GB\",\"storage\":\"512GB SSD\"}",
                    StockQuantity = 10,
                    ReorderLevel = 5,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Product
                {
                    Name = "USB-C Cable",
                    Description = "Durable braided USB-C cable",
                    Price = 19.99m,
                    Category = "Electronics",
                    ImageUrl = "https://example.com/cable.jpg",
                    Specifications = "{\"length\":\"1m\",\"connector\":\"USB-C\"}",
                    StockQuantity = 2,
                    ReorderLevel = 5,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Product
                {
                    Name = "Laptop Stand",
                    Description = "Adjustable aluminum stand",
                    Price = 49.99m,
                    Category = "Electronics",
                    ImageUrl = "https://example.com/stand.jpg",
                    Specifications = "{\"material\":\"aluminum\",\"adjustable\":true}",
                    StockQuantity = 25,
                    ReorderLevel = 5,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Product
                {
                    Name = "Running Shoes",
                    Description = "Lightweight running shoes",
                    Price = 89.99m,
                    Category = "Sports",
                    ImageUrl = "https://example.com/shoes.jpg",
                    Specifications = "{\"sizeRange\":\"7-12\",\"color\":\"Black\"}",
                    StockQuantity = 12,
                    ReorderLevel = 5,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Product
                {
                    Name = "Water Bottle",
                    Description = "Insulated stainless steel bottle",
                    Price = 24.99m,
                    Category = "Sports",
                    ImageUrl = "https://example.com/bottle.jpg",
                    Specifications = "{\"capacity\":\"750ml\",\"insulated\":true}",
                    StockQuantity = 30,
                    ReorderLevel = 5,
                    IsDeleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            };

            foreach (var p in products)
            {
                context.Products.Add(p);
            }

            context.SaveChanges();
        }
    }
}
