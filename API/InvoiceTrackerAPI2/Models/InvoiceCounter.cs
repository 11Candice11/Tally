namespace InvoiceTrackerAPI2.Models;

public class InvoiceCounter
{
    public int    UserId    { get; set; }
    public string YearMonth { get; set; } = string.Empty; // "yyyyMM"
    public int    Counter   { get; set; }
}
