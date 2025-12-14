using Microsoft.EntityFrameworkCore;
using ReactCore.Backend.Models;

namespace ReactCore.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<InventoryAudit> InventoryAudits { get; set; }
    public DbSet<NotificationPreferences> NotificationPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<LoginAttempt>()
            .HasIndex(l => new { l.Email, l.Timestamp });

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Category);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.StockQuantity);

        modelBuilder.Entity<ProductImage>()
            .HasIndex(pi => new { pi.ProductId, pi.DisplayOrder })
            .IsUnique();

        modelBuilder.Entity<Cart>()
            .HasIndex(c => c.UserId)
            .IsUnique();

        modelBuilder.Entity<CartItem>()
            .HasIndex(ci => ci.CartId);

        modelBuilder.Entity<CartItem>()
            .HasIndex(ci => ci.ProductId);

        modelBuilder.Entity<CartItem>()
            .HasIndex(ci => new { ci.CartId, ci.ProductId })
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderNumber)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.UserId);

        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => oi.OrderId);

        modelBuilder.Entity<InventoryAudit>()
            .HasIndex(ia => new { ia.ProductId, ia.Timestamp });

        modelBuilder.Entity<NotificationPreferences>()
            .HasIndex(np => np.UserId);

        modelBuilder.Entity<NotificationPreferences>()
            .HasIndex(np => np.ProductId);
    }
}
