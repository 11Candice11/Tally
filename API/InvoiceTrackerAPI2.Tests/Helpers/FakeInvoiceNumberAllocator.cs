using InvoiceTrackerAPI2.Services.Interfaces;

namespace InvoiceTrackerAPI2.Tests.Helpers;

public class FakeInvoiceNumberAllocator : IInvoiceNumberAllocator
{
    private int _counter;

    public Task<string> NextAsync(int userId)
    {
        var n = Interlocked.Increment(ref _counter);
        return Task.FromResult($"INV-{DateTime.UtcNow:yyyyMM}-{userId}-{n:D4}");
    }
}
