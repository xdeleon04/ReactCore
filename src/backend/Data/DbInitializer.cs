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

        if (context.Users.Any())
        {
            return;   // DB has been seeded
        }

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
}
