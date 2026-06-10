using InvoiceTrackerAPI2.DTOs;

namespace InvoiceTrackerAPI2.Services.Interfaces;

public interface IEmailService
{
    Task SendInvoiceAsync(string toEmail, InvoiceDto invoice);
}
