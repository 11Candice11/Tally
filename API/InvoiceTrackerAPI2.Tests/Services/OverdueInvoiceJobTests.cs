using InvoiceTrackerAPI2.Models;
using InvoiceTrackerAPI2.Models.Enums;
using InvoiceTrackerAPI2.Services;
using InvoiceTrackerAPI2.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceTrackerAPI2.Tests.Services;

public class OverdueInvoiceJobTests
{
    private static IServiceScopeFactory BuildScopeFactory(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<InvoiceTrackerAPI2.Data.AppDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task MarkOverdue_SentAndPastDue_MarkedAsOverdue()
    {
        var dbName = Guid.NewGuid().ToString();
        var db     = TestDbContext.Create(dbName);

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

        var job = new OverdueInvoiceJob(BuildScopeFactory(dbName), NullLogger<OverdueInvoiceJob>.Instance);
        await job.RunMarkOverdueAsync(CancellationToken.None);

        var invoice = await db.Invoices.FirstAsync();
        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
    }

    [Fact]
    public async Task MarkOverdue_SentAndNotYetDue_Unchanged()
    {
        var dbName = Guid.NewGuid().ToString();
        var db     = TestDbContext.Create(dbName);

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

        var job = new OverdueInvoiceJob(BuildScopeFactory(dbName), NullLogger<OverdueInvoiceJob>.Instance);
        await job.RunMarkOverdueAsync(CancellationToken.None);

        var invoice = await db.Invoices.FirstAsync();
        Assert.Equal(InvoiceStatus.Sent, invoice.Status);
    }

    [Fact]
    public async Task MarkOverdue_PaidAndPastDue_Unchanged()
    {
        var dbName = Guid.NewGuid().ToString();
        var db     = TestDbContext.Create(dbName);

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

        var job = new OverdueInvoiceJob(BuildScopeFactory(dbName), NullLogger<OverdueInvoiceJob>.Instance);
        await job.RunMarkOverdueAsync(CancellationToken.None);

        var invoice = await db.Invoices.FirstAsync();
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }
}
