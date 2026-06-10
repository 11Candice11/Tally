using InvoiceTrackerAPI2.Data;
using InvoiceTrackerAPI2.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerAPI2.Services;

// background service that runs at midnight every day and marks overdue invoices
// BackgroundService is the built in .net way to do this, runs on a separate thread
public class OverdueInvoiceJob(IServiceScopeFactory scopeFactory, ILogger<OverdueInvoiceJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitUntilMidnightUtcAsync(stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await MarkOverdueAsync(stoppingToken);
        }
    }

    internal async Task RunMarkOverdueAsync(CancellationToken ct) => await MarkOverdueAsync(ct);

    private async Task MarkOverdueAsync(CancellationToken ct)
    {
        try
        {
            // need a new scope here because DbContext is scoped not singleton
            // cant inject it directly into a background service
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var today = DateTime.UtcNow.Date;
            var count = await db.Invoices
                .Where(i => i.Status == InvoiceStatus.Sent && i.DueDate < today)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, InvoiceStatus.Overdue), ct);

            if (count > 0)
                logger.LogInformation("Marked {Count} invoice(s) as Overdue.", count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark overdue invoices.");
        }
    }

    private static async Task WaitUntilMidnightUtcAsync(CancellationToken ct)
    {
        var now   = DateTime.UtcNow;
        var delay = now.Date.AddDays(1) - now;
        await Task.Delay(delay, ct).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
}
