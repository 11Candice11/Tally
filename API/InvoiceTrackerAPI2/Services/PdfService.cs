using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceTrackerAPI2.Services;

public class PdfService : IPdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(InvoiceDto inv, string toEmail)
    {
        var subtotal = inv.LineItems.Sum(l => l.Qty * l.UnitPrice);
        var vat      = subtotal * inv.VatRate;
        var total    = subtotal + vat;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().Background("#1e293b").Padding(20).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("INVOICE").FontSize(9).FontColor("#94a3b8").LetterSpacing(1);
                            c.Item().Text(inv.InvoiceNumber).FontSize(20).Bold().FontColor("#ffffff");
                        });
                    });

                    col.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("BILLED TO").FontSize(8).FontColor("#999999").LetterSpacing(1);
                            c.Item().Text(inv.ClientName).FontSize(14).Bold().FontColor("#1a1a1a");
                            c.Item().Text(toEmail).FontSize(11).FontColor("#999999");
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("ISSUE DATE").FontSize(8).FontColor("#999999").LetterSpacing(1);
                            c.Item().Text(inv.IssueDate.ToString("dd MMM yyyy")).FontSize(11).FontColor("#1a1a1a");
                            c.Item().PaddingTop(8).Text("DUE DATE").FontSize(8).FontColor("#999999").LetterSpacing(1);
                            c.Item().Text(inv.DueDate.ToString("dd MMM yyyy")).FontSize(11).FontColor("#1a1a1a");
                        });
                    });

                    // Line items table
                    col.Item().PaddingTop(24).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        // Header row
                        static IContainer HeaderCell(IContainer c) =>
                            c.BorderBottom(2).BorderColor("#e0e0e0").PaddingBottom(6);

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("DESCRIPTION").FontSize(8).FontColor("#999999").Bold().LetterSpacing(0.5f);
                            h.Cell().Element(HeaderCell).AlignCenter().Text("QTY").FontSize(8).FontColor("#999999").Bold().LetterSpacing(0.5f);
                            h.Cell().Element(HeaderCell).AlignRight().Text("UNIT PRICE").FontSize(8).FontColor("#999999").Bold().LetterSpacing(0.5f);
                            h.Cell().Element(HeaderCell).AlignRight().Text("TOTAL").FontSize(8).FontColor("#999999").Bold().LetterSpacing(0.5f);
                        });

                        static IContainer DataCell(IContainer c) =>
                            c.BorderBottom(1).BorderColor("#f0f0f0").PaddingVertical(10);

                        foreach (var item in inv.LineItems)
                        {
                            table.Cell().Element(DataCell).Text(item.Description).FontColor("#1a1a1a");
                            table.Cell().Element(DataCell).AlignCenter().Text(item.Qty.ToString()).FontColor("#1a1a1a");
                            table.Cell().Element(DataCell).AlignRight().Text($"R {item.UnitPrice:N2}").FontColor("#1a1a1a");
                            table.Cell().Element(DataCell).AlignRight().Text($"R {item.Qty * item.UnitPrice:N2}").Bold().FontColor("#1a1a1a");
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(16).AlignRight().Column(c =>
                    {
                        c.Item().Text($"Subtotal: R {subtotal:N2}").FontSize(11).FontColor("#666666");
                        c.Item().PaddingTop(4).Text($"VAT ({inv.VatRate * 100:0}%): R {vat:N2}").FontSize(11).FontColor("#666666");
                        c.Item().PaddingTop(8).Text($"Total: R {total:N2}").FontSize(16).Bold().FontColor("#1a1a1a");
                    });

                    // Notes
                    if (inv.Notes is not null)
                    {
                        col.Item().PaddingTop(24).Background("#f9f9f9").Padding(12).Column(c =>
                        {
                            c.Item().Text("NOTES").FontSize(8).FontColor("#999999").LetterSpacing(1);
                            c.Item().PaddingTop(6).Text(inv.Notes).FontSize(11).FontColor("#666666");
                        });
                    }
                });

                page.Footer().AlignCenter()
                    .Text("Sent via TALLY").FontSize(10).FontColor("#999999");
            });
        }).GeneratePdf();
    }
}
