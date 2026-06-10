using InvoiceTrackerAPI2.Models.Enums;

namespace InvoiceTrackerAPI2.DTOs;

public record InvoiceDto
{
    public int Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string ClientEmail { get; init; } = string.Empty;
    public InvoiceStatus Status { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime DueDate { get; init; }
    public decimal VatRate { get; init; }
    public decimal Discount { get; init; }
    public decimal LateFee { get; init; }
    public string PaymentTerms { get; init; } = "Net 30";
    public string Currency { get; init; } = "ZAR";
    public string? Notes { get; init; }
    public List<LineItemDto> LineItems { get; init; } = [];
    // Computed — calculated by AutoMapper
    public decimal Subtotal { get; init; }
    public decimal VatAmount { get; init; }
    public decimal Total { get; init; }
}
