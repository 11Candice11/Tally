namespace InvoiceTrackerAPI2.Services.Interfaces;

public interface IInvoiceNumberAllocator
{
    Task<string> NextAsync(int userId);
}
