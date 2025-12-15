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

    public DbSet<AdminAction> AdminActions { get; set; }

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

        modelBuilder.Entity<AdminAction>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.ActionType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(a => a.EntityId)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(a => a.Timestamp)
                .IsRequired();

            entity.Property(a => a.IpAddress)
                .HasMaxLength(45);

            entity.Property(a => a.Reason)
                .HasMaxLength(500);

            entity.HasOne(a => a.Admin)
                .WithMany(u => u.AdminActions)
                .HasForeignKey(a => a.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => new { a.AdminUserId, a.Timestamp });
            entity.HasIndex(a => a.Timestamp);
        });
    }
}
