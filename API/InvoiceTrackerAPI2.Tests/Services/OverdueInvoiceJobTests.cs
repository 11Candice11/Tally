using InvoiceTrackerAPI2.Data;
using InvoiceTrackerAPI2.Models;
using InvoiceTrackerAPI2.Models.Enums;
using InvoiceTrackerAPI2.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceTrackerAPI2.Tests.Services;

/// <summary>
/// OverdueInvoiceJob uses ExecuteUpdateAsync (bulk SQL), which the EF Core
/// in-memory provider silently ignores. These tests use SQLite so the bulk
/// update executes correctly.
/// </summary>
public class OverdueInvoiceJobTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"overdue_test_{Guid.NewGuid()}.db");

    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private IServiceScopeFactory BuildScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={_dbPath}"));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    /// <summary>Seed a user so Invoice.UserId FK is satisfied.</summary>
    private static async Task SeedUserAsync(AppDbContext db, int userId = 1)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            db.Users.Add(new User
            {
                Id           = userId,
                Name         = "Test User",
                Email        = $"user{userId}@test.com",
                PasswordHash = "hash",
            });
            await db.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task MarkOverdue_SentAndPastDue_MarkedAsOverdue()
    {
        using var db = CreateDb();
        await SeedUserAsync(db);
        db.Invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-001",
            ClientName    = "Acme",
            ClientEmail   = "a@acme.com",
            Status        = InvoiceStatus.Sent,
            IssueDate     = DateTime.UtcNow.AddDays(-60),
            DueDate       = DateTime.UtcNow.AddDays(-1),
            UserId        = 1,
        });
        await db.SaveChangesAsync();

        var job = new OverdueInvoiceJob(BuildScopeFactory(), NullLogger<OverdueInvoiceJob>.Instance);
        await job.RunMarkOverdueAsync(CancellationToken.None);

        await db.Entry(db.Invoices.First()).ReloadAsync();
        Assert.Equal(InvoiceStatus.Overdue, db.Invoices.First().Status);
    }

    [Fact]
    public async Task MarkOverdue_SentAndNotYetDue_Unchanged()
    {
        using var db = CreateDb();
        await SeedUserAsync(db);
        db.Invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-002",
            ClientName    = "Acme",
            ClientEmail   = "a@acme.com",
            Status        = InvoiceStatus.Sent,
            IssueDate     = DateTime.UtcNow.AddDays(-5),
            DueDate       = DateTime.UtcNow.AddDays(10),
            UserId        = 1,
        });
        await db.SaveChangesAsync();

        var job = new OverdueInvoiceJob(BuildScopeFactory(), NullLogger<OverdueInvoiceJob>.Instance);
        await job.RunMarkOverdueAsync(CancellationToken.None);

        await db.Entry(db.Invoices.First()).ReloadAsync();
        Assert.Equal(InvoiceStatus.Sent, db.Invoices.First().Status);
    }

    [Fact]
    public async Task MarkOverdue_PaidAndPastDue_Unchanged()
    {
        using var db = CreateDb();
        await SeedUserAsync(db);
        db.Invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-003",
            ClientName    = "Acme",
            ClientEmail   = "a@acme.com",
            Status        = InvoiceStatus.Paid,
            IssueDate     = DateTime.UtcNow.AddDays(-60),
            DueDate       = DateTime.UtcNow.AddDays(-1),
            UserId        = 1,
        });
        await db.SaveChangesAsync();

        var job = new OverdueInvoiceJob(BuildScopeFactory(), NullLogger<OverdueInvoiceJob>.Instance);
        await job.RunMarkOverdueAsync(CancellationToken.None);

        await db.Entry(db.Invoices.First()).ReloadAsync();
        Assert.Equal(InvoiceStatus.Paid, db.Invoices.First().Status);
    }

    [Fact]
    public async Task MarkOverdue_DraftAndPastDue_Unchanged()
    {
        using var db = CreateDb();
        await SeedUserAsync(db);
        db.Invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-004",
            ClientName    = "Acme",
            ClientEmail   = "a@acme.com",
            Status        = InvoiceStatus.Draft,
            IssueDate     = DateTime.UtcNow.AddDays(-60),
            DueDate       = DateTime.UtcNow.AddDays(-1),
            UserId        = 1,
        });
        await db.SaveChangesAsync();

        var job = new OverdueInvoiceJob(BuildScopeFactory(), NullLogger<OverdueInvoiceJob>.Instance);
        await job.RunMarkOverdueAsync(CancellationToken.None);

        await db.Entry(db.Invoices.First()).ReloadAsync();
        Assert.Equal(InvoiceStatus.Draft, db.Invoices.First().Status);
    }

    [Fact]
    public async Task MarkOverdue_MultipleInvoices_OnlyMarksEligibleOnes()
    {
        using var db = CreateDb();
        await SeedUserAsync(db);
        db.Invoices.AddRange(
            new Invoice
            {
                InvoiceNumber = "INV-005",
                ClientName    = "Acme",
                ClientEmail   = "a@acme.com",
                Status        = InvoiceStatus.Sent,
                IssueDate     = DateTime.UtcNow.AddDays(-60),
                DueDate       = DateTime.UtcNow.AddDays(-1), // past due → Overdue
                UserId        = 1,
            },
            new Invoice
            {
                InvoiceNumber = "INV-006",
                ClientName    = "Beta",
                ClientEmail   = "b@beta.com",
                Status        = InvoiceStatus.Sent,
                IssueDate     = DateTime.UtcNow.AddDays(-5),
                DueDate       = DateTime.UtcNow.AddDays(10), // not yet due → stays Sent
                UserId        = 1,
            }
        );
        await db.SaveChangesAsync();

        var job = new OverdueInvoiceJob(BuildScopeFactory(), NullLogger<OverdueInvoiceJob>.Instance);
        await job.RunMarkOverdueAsync(CancellationToken.None);

        // Reload from DB to pick up the bulk update
        var invoices = await db.Invoices
            .AsNoTracking()
            .OrderBy(i => i.InvoiceNumber)
            .ToListAsync();

        Assert.Equal(InvoiceStatus.Overdue, invoices[0].Status);
        Assert.Equal(InvoiceStatus.Sent,    invoices[1].Status);
    }
}
