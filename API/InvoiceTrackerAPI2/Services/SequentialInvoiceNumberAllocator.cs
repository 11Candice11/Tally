using InvoiceTrackerAPI2.Data;
using InvoiceTrackerAPI2.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerAPI2.Services;

// postgres upsert is atomic so no race conditions even if two requests come in at the same time
// had to use ToListAsync instead of SingleAsync because EF cant compose on raw SQL with RETURNING
public class SequentialInvoiceNumberAllocator(AppDbContext db) : IInvoiceNumberAllocator
{
    public async Task<string> NextAsync(int userId)
    {
        var yearMonth = DateTime.UtcNow.ToString("yyyyMM");

        // INSERT ... ON CONFLICT DO UPDATE is atomic — no race condition,
        // no retry, no gap even under concurrent requests.
        var results = await db.Database.SqlQueryRaw<int>(
            """
            INSERT INTO "InvoiceCounters" ("UserId", "YearMonth", "Counter")
            VALUES ({0}, {1}, 1)
            ON CONFLICT ("UserId", "YearMonth") DO UPDATE
                SET "Counter" = "InvoiceCounters"."Counter" + 1
            RETURNING "Counter"
            """,
            userId, yearMonth)
            .ToListAsync();

        var next = results[0];

        return $"INV-{yearMonth}-{userId}-{next:D4}";
    }
}
