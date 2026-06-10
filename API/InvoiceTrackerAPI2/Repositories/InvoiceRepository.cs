using InvoiceTrackerAPI2.Data;
using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Models;
using InvoiceTrackerAPI2.Models.Enums;
using InvoiceTrackerAPI2.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerAPI2.Repositories;

// implements interface with EF Core
public class InvoiceRepository(AppDbContext db) : IInvoiceRepository
{
    // builds query incrementally
    public async Task<(IEnumerable<Invoice> Items, int Total)> GetAllAsync(int userId, InvoiceFilterDto filter)
    {
        var query = db.Invoices
            .Include(i => i.LineItems)
            .Where(i => i.UserId == userId);

        if (filter.Status is not null)
            query = query.Where(i => i.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.ClientName))
            query = query.Where(i => i.ClientName.Contains(filter.ClientName));

        if (filter.From is not null)
            query = query.Where(i => i.IssueDate >= filter.From);

        if (filter.To is not null)
            query = query.Where(i => i.IssueDate <= filter.To);

        var ordered = query.OrderByDescending(i => i.IssueDate);
        var total   = await ordered.CountAsync();
        var items   = await ordered
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task<Invoice?> GetByIdAsync(int id, int userId) =>
    // evry query that returns invoices will include line items
    // called eager loading
        db.Invoices.Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    public async Task<Invoice?> UpdateAsync(Invoice invoice)
    {
        db.Invoices.Update(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        // does a fetch and then delete not just delete
        // less efficient but respects userId ownership check
        // you cant delete someone else's invoice
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        if (invoice is null) return false;
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync();
        return true;
    }

    // ── Summary ───────────────────────────────────────────────────────────────

    public async Task<InvoiceSummaryDto> GetSummaryAsync(int userId)
    {
        var now       = DateTime.UtcNow;
        var thisStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastStart = thisStart.AddMonths(-1);
        var lastEnd   = thisStart;
        var nextStart = thisStart.AddMonths(1);

        // Loads only the 3 columns needed for aggregation — no line item data, no full entity.
        // EF Core cannot reliably translate grouped subquery aggregations (e.g. PaidThisMonth)
        // to SQL for complex total expressions, so we project minimally and aggregate in C#.
        var rows = await db.Invoices
            .Where(i => i.UserId == userId)
            .Select(i => new
            {
                i.Status,
                i.IssueDate,
                Total = i.LineItems.Sum(l => l.Qty * (decimal)l.UnitPrice) * (1 + i.VatRate)
                        - i.Discount + i.LateFee,
            })
            .ToListAsync();

        return new InvoiceSummaryDto
        {
            TotalRevenue  = rows.Sum(r => r.Total),
            Outstanding   = rows.Where(r => r.Status == InvoiceStatus.Sent || r.Status == InvoiceStatus.Overdue).Sum(r => r.Total),
            OverdueTotals = rows.Where(r => r.Status == InvoiceStatus.Overdue).Sum(r => r.Total),
            OverdueCount  = rows.Count(r => r.Status == InvoiceStatus.Overdue),
            PendingCount  = rows.Count(r => r.Status == InvoiceStatus.Sent),
            PaidThisMonth = rows.Where(r => r.Status == InvoiceStatus.Paid && r.IssueDate >= thisStart && r.IssueDate < nextStart).Sum(r => r.Total),
            PaidLastMonth = rows.Where(r => r.Status == InvoiceStatus.Paid && r.IssueDate >= lastStart  && r.IssueDate < lastEnd) .Sum(r => r.Total),
        };
    }

    // ── Admin methods (no userId ownership filter) ────────────────────────────

    public async Task<(IEnumerable<Invoice> Items, int Total)> GetAllAdminAsync(InvoiceFilterDto filter)
    {
        var query = db.Invoices.Include(i => i.LineItems).AsQueryable();

        if (filter.Status is not null)
            query = query.Where(i => i.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.ClientName))
            query = query.Where(i => i.ClientName.Contains(filter.ClientName));

        if (filter.From is not null)
            query = query.Where(i => i.IssueDate >= filter.From);

        if (filter.To is not null)
            query = query.Where(i => i.IssueDate <= filter.To);

        var ordered = query.OrderByDescending(i => i.IssueDate);
        var total   = await ordered.CountAsync();
        var items   = await ordered
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task<Invoice?> GetByIdAdminAsync(int id) =>
        db.Invoices.Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<bool> DeleteAdminAsync(int id)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice is null) return false;
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync();
        return true;
    }
}
