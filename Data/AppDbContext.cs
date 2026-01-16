using loginAPI.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace loginAPI.Data;

public class AppDbContext : DbContext
{   
    public AppDbContext(DbContextOptions<AppDbContext> options) :base(options)
    {
    }
    
    public DbSet<User> Users {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var hasher = new PasswordHasher<string>();
        var adminPassword = hasher.HashPassword(null, "LoginSwaggerAPISecretKeyForJwtToken2026");

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            Password = adminPassword,
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        });
    }
    
}