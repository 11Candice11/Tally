using InvoiceTrackerAPI2.DTOs;

namespace InvoiceTrackerAPI2.Services.Interfaces;

public interface IPdfService
{
    byte[] GenerateInvoicePdf(InvoiceDto invoice, string toEmail);
}
