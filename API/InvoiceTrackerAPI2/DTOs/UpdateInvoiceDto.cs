using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerAPI2.DTOs;

public record UpdateInvoiceDto
{
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Client name must be between 2 and 200 characters.")]
    public string? ClientName { get; init; }

    [EmailAddress(ErrorMessage = "Client email must be a valid email address.")]
    [StringLength(200, ErrorMessage = "Client email must not exceed 200 characters.")]
    public string? ClientEmail { get; init; }

    public DateTime? IssueDate { get; init; }

    public DateTime? DueDate { get; init; }

    [Range(0, 1, ErrorMessage = "VAT rate must be between 0 and 1 (e.g. 0.15 for 15%).")]
    public decimal? VatRate { get; init; }

    [Range(0, double.MaxValue, ErrorMessage = "Discount cannot be negative.")]
    public decimal? Discount { get; init; }

    [Range(0, double.MaxValue, ErrorMessage = "Late fee cannot be negative.")]
    public decimal? LateFee { get; init; }

    [StringLength(50, ErrorMessage = "Payment terms must not exceed 50 characters.")]
    public string? PaymentTerms { get; init; }

    [StringLength(10, ErrorMessage = "Currency code must not exceed 10 characters.")]
    public string? Currency { get; init; }

    [StringLength(1000, ErrorMessage = "Notes must not exceed 1000 characters.")]
    public string? Notes { get; init; }

    [MinLength(1, ErrorMessage = "At least one line item is required.")]
    public List<CreateLineItemDto>? LineItems { get; init; }
}
