namespace InvoiceTrackerAPI2.DTOs;

public record ClientDto
{
    public int     Id           { get; init; }
    public string  Name         { get; init; } = string.Empty;
    public string  Email        { get; init; } = string.Empty;
    public string? Phone        { get; init; }
    public int     InvoiceCount { get; init; }
    public decimal TotalBilled  { get; init; }
    public string? LastInvoice  { get; init; }
}
