using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Models;

namespace SmartAgenda.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Event>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId);

        modelBuilder.Entity<Expense>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId);
        modelBuilder.Entity<Expense>()
            .Property(e => e.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Budget>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(b => b.UserId);
        modelBuilder.Entity<Budget>()
            .Property(b => b.Amount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Budget>()
            .HasIndex(b => new { b.UserId, b.Year, b.Month })
            .IsUnique();
    }
}
