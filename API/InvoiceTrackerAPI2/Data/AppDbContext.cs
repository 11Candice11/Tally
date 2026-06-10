using InvoiceTrackerAPI2.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerAPI2.Data;

// all queries go through the repository, controllers and services never touch AppDbContext directly
// EnsureCreated on startup creates the schema if it doesnt exist, no migrations needed
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>           Users           => Set<User>();
    public DbSet<Client>         Clients         => Set<Client>();
    public DbSet<Invoice>        Invoices        => Set<Invoice>();
    public DbSet<LineItem>       LineItems       => Set<LineItem>();
    public DbSet<RefreshToken>   RefreshTokens   => Set<RefreshToken>();
    public DbSet<InvoiceCounter> InvoiceCounters => Set<InvoiceCounter>();

// relational rules in code rather than attributes
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // make sure email is unique - enforced at DB lvl
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            // cascading delete - deleting a user deletes the invoice
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.Status)
            // store enum as string 
            .HasConversion<string>();

        // all queries filter by UserId first — most selective index
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.UserId);

        // covers ORDER BY IssueDate and date range filters (From/To)
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => new { i.UserId, i.IssueDate });

        // covers status filter tab/dropdown queries
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => new { i.UserId, i.Status });

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<LineItem>()
            .HasOne(l => l.Invoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.TokenHash)
            .IsUnique();

        modelBuilder.Entity<InvoiceCounter>()
            .HasKey(c => new { c.UserId, c.YearMonth });

        modelBuilder.Entity<Client>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // one user cannot have two clients with the same email
        modelBuilder.Entity<Client>()
            .HasIndex(c => new { c.UserId, c.Email })
            .IsUnique();

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Client)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
