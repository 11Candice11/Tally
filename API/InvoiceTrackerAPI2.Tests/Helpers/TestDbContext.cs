using InvoiceTrackerAPI2.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerAPI2.Tests.Helpers;

public static class TestDbContext
{
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
