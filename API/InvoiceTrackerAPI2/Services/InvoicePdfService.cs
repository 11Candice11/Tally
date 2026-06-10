using InvoiceTrackerAPI2.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceTrackerAPI2.Services;

public static class InvoicePdfService
{
    static InvoicePdfService()
    {
        // Community licence — free for revenue under $1M/year
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(InvoiceDto inv)
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
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ── Header ────────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("INVOICE")
                                .FontSize(9).FontColor("#94a3b8").LetterSpacing(0.1f);
                            c.Item().Text(inv.InvoiceNumber)
                                .FontSize(24).Bold().FontColor("#1e293b");
                        });

                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("TALLY").Bold().FontSize(14).FontColor("#1e293b");
                            c.Item().Text($"Issued: {inv.IssueDate:dd MMM yyyy}").FontColor("#666666");
                            c.Item().Text($"Due: {inv.DueDate:dd MMM yyyy}").FontColor("#666666");
                        });
                    });

                    col.Item().PaddingVertical(16).LineHorizontal(1).LineColor("#e0e0e0");

                    // ── Billed to ─────────────────────────────────────────────
                    col.Item().PaddingBottom(20).Column(c =>
                    {
                        c.Item().Text("BILLED TO")
                            .FontSize(9).FontColor("#999999").LetterSpacing(0.1f);
                        c.Item().Text(inv.ClientName).Bold().FontSize(13);
                        c.Item().Text(inv.ClientEmail).FontColor("#666666");
                    });

                    // ── Line items table ──────────────────────────────────────
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4); // description
                            cols.RelativeColumn(1); // qty
                            cols.RelativeColumn(2); // unit price
                            cols.RelativeColumn(2); // total
                        });

                        // Header row
                        static IContainer HeaderCell(IContainer c) =>
                            c.BorderBottom(1).BorderColor("#e0e0e0")
                             .PaddingVertical(6).PaddingHorizontal(4);

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("DESCRIPTION")
                                .FontSize(9).FontColor("#999999").Bold();
                            h.Cell().Element(HeaderCell).AlignCenter().Text("QTY")
                                .FontSize(9).FontColor("#999999").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("UNIT PRICE")
                                .FontSize(9).FontColor("#999999").Bold();
                            h.Cell().Element(HeaderCell).AlignRight().Text("TOTAL")
                                .FontSize(9).FontColor("#999999").Bold();
                        });

                        // Data rows
                        foreach (var item in inv.LineItems)
                        {
                            static IContainer DataCell(IContainer c) =>
                                c.BorderBottom(1).BorderColor("#f0f0f0")
                                 .PaddingVertical(10).PaddingHorizontal(4);

                            table.Cell().Element(DataCell).Text(item.Description);
                            table.Cell().Element(DataCell).AlignCenter().Text(item.Qty.ToString());
                            table.Cell().Element(DataCell).AlignRight().Text($"R {item.UnitPrice:N2}");
                            table.Cell().Element(DataCell).AlignRight()
                                .Text($"R {item.Qty * item.UnitPrice:N2}").Bold();
                        }
                    });

                    // ── Totals ────────────────────────────────────────────────
                    col.Item().PaddingTop(16).AlignRight().Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(120).Text("Subtotal").FontColor("#666666");
                            r.ConstantItem(100).AlignRight().Text($"R {subtotal:N2}");
                        });
                        c.Item().PaddingTop(4).Row(r =>
                        {
                            r.ConstantItem(120).Text($"VAT ({inv.VatRate * 100:0}%)").FontColor("#666666");
                            r.ConstantItem(100).AlignRight().Text($"R {vat:N2}");
                        });
                        c.Item().PaddingTop(8).BorderTop(1).BorderColor("#e0e0e0").Row(r =>
                        {
                            r.ConstantItem(120).Text("TOTAL DUE").Bold().FontSize(12);
                            r.ConstantItem(100).AlignRight().Text($"R {total:N2}").Bold().FontSize(12);
                        });
                    });

                    // ── Notes ─────────────────────────────────────────────────
                    if (!string.IsNullOrWhiteSpace(inv.Notes))
                    {
                        col.Item().PaddingTop(24).Column(c =>
                        {
                            c.Item().Text("NOTES")
                                .FontSize(9).FontColor("#999999").LetterSpacing(0.1f);
                            c.Item().PaddingTop(4).Background("#f9f9f9")
                                .Padding(10).Text(inv.Notes).FontColor("#666666");
                        });
                    }
                });

                // ── Footer ────────────────────────────────────────────────────
                page.Footer().AlignCenter()
                    .Text("Sent via Tally · Invoice Management")
                    .FontSize(9).FontColor("#cccccc");
            });
        }).GeneratePdf();
    }
}
