using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PolilamInventory.Web.ViewModels;

namespace PolilamInventory.Web.Services;

public class ReportExportService
{
    // ─── CSV ───────────────────────────────────────────────────────────────

    public byte[] GenerateInventoryCsv(List<InventoryReportRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("Pattern,W\u00d7L\u00d7T,In Stock,Last Adj.,On Order,Order Date,ETA,Committed Before Arrival,Projected at Arrival,Total Committed,Projected Balance,Re-Order?,Sheet Value,Stock Value,On Order Value\r\n");

        foreach (var row in rows)
        {
            sb.Append($"{CsvField(row.PatternName)},{CsvField(row.SizeDisplay)},{row.InStock},{(row.LastAdjDate.HasValue ? row.LastAdjDate.Value.ToString("yyyy-MM-dd") : "")},{row.OnOrder},{(row.OrderDate.HasValue ? row.OrderDate.Value.ToString("yyyy-MM-dd") : "")},{(row.Eta.HasValue ? row.Eta.Value.ToString("yyyy-MM-dd") : "")},{row.CommittedBeforeArrival},{row.ProjectedAtArrival},{row.TotalCommitted},{row.ProjectedBalance},{(row.NeedsReorder ? "Yes" : "")},{row.SheetValue:F2},{row.StockValue:F2},{row.OnOrderValue:F2}\r\n");
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        return preamble.Concat(content).ToArray();
    }

    public byte[] GenerateRemovalCsv(List<RemovalReportRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("Pattern,W\u00d7L\u00d7T,Sheets Removed,Last Removal Date\r\n");

        foreach (var row in rows)
        {
            sb.Append($"{CsvField(row.PatternName)},{CsvField(row.SizeDisplay)},{row.SheetsRemoved},{(row.LastRemovalDate.HasValue ? row.LastRemovalDate.Value.ToString("yyyy-MM-dd") : "")}\r\n");
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        return preamble.Concat(content).ToArray();
    }

    public byte[] GenerateTransactionCsv(List<TransactionReportRow> rows)
    {
        var sb = new StringBuilder();
        sb.Append("Date,Type,Pattern,W\u00d7L\u00d7T,Quantity,PO/SO,Note\r\n");

        foreach (var row in rows)
        {
            sb.Append($"{row.Date:yyyy-MM-dd},{CsvField(row.Type)},{CsvField(row.PatternName)},{CsvField(row.SizeDisplay)},{row.Quantity},{CsvField(row.PoSoNumber ?? "")},{CsvField(row.Note ?? "")}\r\n");
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        return preamble.Concat(content).ToArray();
    }

    // ─── PDF ───────────────────────────────────────────────────────────────

    public byte[] GenerateInventoryPdf(List<InventoryReportRow> rows, string? patternFilter)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15);
                page.Content().Column(col =>
                {
                    col.Item().Text("Polilam Inventory \u2014 Inventory Report")
                        .FontSize(12).Bold();
                    col.Item().Text($"Generated: {DateTime.Today:yyyy-MM-dd}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrWhiteSpace(patternFilter))
                        col.Item().Text($"Pattern: {patternFilter}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);

                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);  // Pattern
                            columns.RelativeColumn(2);  // W×L×T
                            columns.RelativeColumn(1);  // In Stock
                            columns.RelativeColumn(1.5f); // Last Adj.
                            columns.RelativeColumn(1);  // On Order
                            columns.RelativeColumn(1.5f); // Order Date
                            columns.RelativeColumn(1.5f); // ETA
                            columns.RelativeColumn(1.5f); // Committed Before Arrival
                            columns.RelativeColumn(1.5f); // Projected at Arrival
                            columns.RelativeColumn(1.5f); // Total Committed
                            columns.RelativeColumn(1.5f); // Projected Balance
                            columns.RelativeColumn(1);  // Re-Order?
                            columns.RelativeColumn(1);  // Sheet Value
                            columns.RelativeColumn(1.2f); // Stock Value
                            columns.RelativeColumn(1.2f); // On Order Value
                        });

                        table.Header(header =>
                        {
                            var headerBg = Colors.Grey.Darken3;
                            header.Cell().Background(headerBg).Padding(2).Text("Pattern").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("W\u00d7L\u00d7T").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("In Stock").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Last Adj.").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("On Order").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Order Date").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("ETA").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Cmtd. Before Arrival").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Proj. at Arrival").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Total Cmtd.").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Proj. Balance").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Re-Order?").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Sheet $").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Stock $").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Order $").FontColor(Colors.White).FontSize(9).Bold();
                        });

                        for (int i = 0; i < rows.Count; i++)
                        {
                            var row = rows[i];
                            var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;
                            table.Cell().Background(bg).Padding(2).Text(row.PatternName).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.SizeDisplay).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.InStock.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.LastAdjDate.HasValue ? row.LastAdjDate.Value.ToString("yyyy-MM-dd") : "").FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.OnOrder.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.OrderDate.HasValue ? row.OrderDate.Value.ToString("yyyy-MM-dd") : "").FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.Eta.HasValue ? row.Eta.Value.ToString("yyyy-MM-dd") : "").FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.CommittedBeforeArrival.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.ProjectedAtArrival.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.TotalCommitted.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.ProjectedBalance.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.NeedsReorder ? "Yes" : "").FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.SheetValue > 0 ? row.SheetValue.ToString("C2") : "").FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.SheetValue > 0 ? row.StockValue.ToString("C0") : "").FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.SheetValue > 0 ? row.OnOrderValue.ToString("C0") : "").FontSize(9);
                        }

                        // Grand total row
                        var totalBg = Colors.Grey.Lighten2;
                        for (int c = 0; c < 13; c++)
                            table.Cell().Background(totalBg).Padding(2).Text(c == 0 ? "Grand Total" : "").FontSize(9).Bold();
                        table.Cell().Background(totalBg).Padding(2).Text(rows.Sum(r => r.StockValue).ToString("C0")).FontSize(9).Bold();
                        table.Cell().Background(totalBg).Padding(2).Text(rows.Sum(r => r.OnOrderValue).ToString("C0")).FontSize(9).Bold();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateRemovalPdf(List<RemovalReportRow> rows, DateTime? start, DateTime? end)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15);
                page.Content().Column(col =>
                {
                    col.Item().Text("Polilam Inventory \u2014 Removal Report")
                        .FontSize(12).Bold();
                    col.Item().Text($"Generated: {DateTime.Today:yyyy-MM-dd}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    if (start.HasValue || end.HasValue)
                        col.Item().Text($"Period: {(start.HasValue ? start.Value.ToString("yyyy-MM-dd") : "?")} \u2013 {(end.HasValue ? end.Value.ToString("yyyy-MM-dd") : "?")}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);

                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);  // Pattern
                            columns.RelativeColumn(3);  // W×L×T
                            columns.RelativeColumn(2);  // Sheets Removed
                            columns.RelativeColumn(2);  // Last Removal Date
                        });

                        table.Header(header =>
                        {
                            var headerBg = Colors.Grey.Darken3;
                            header.Cell().Background(headerBg).Padding(2).Text("Pattern").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("W\u00d7L\u00d7T").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Sheets Removed").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Last Removal Date").FontColor(Colors.White).FontSize(9).Bold();
                        });

                        for (int i = 0; i < rows.Count; i++)
                        {
                            var row = rows[i];
                            var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;
                            table.Cell().Background(bg).Padding(2).Text(row.PatternName).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.SizeDisplay).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.SheetsRemoved.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.LastRemovalDate.HasValue ? row.LastRemovalDate.Value.ToString("yyyy-MM-dd") : "").FontSize(9);
                        }
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateTransactionPdf(List<TransactionReportRow> rows, string? patternFilter, DateTime? start, DateTime? end)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15);
                page.Content().Column(col =>
                {
                    col.Item().Text("Polilam Inventory \u2014 Transactions Report")
                        .FontSize(12).Bold();
                    col.Item().Text($"Generated: {DateTime.Today:yyyy-MM-dd}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrWhiteSpace(patternFilter))
                        col.Item().Text($"Pattern: {patternFilter}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                    if (start.HasValue || end.HasValue)
                        col.Item().Text($"Period: {(start.HasValue ? start.Value.ToString("yyyy-MM-dd") : "?")} \u2013 {(end.HasValue ? end.Value.ToString("yyyy-MM-dd") : "?")}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);

                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f); // Date
                            columns.RelativeColumn(1.5f); // Type
                            columns.RelativeColumn(2.5f); // Pattern
                            columns.RelativeColumn(2.5f); // W×L×T
                            columns.RelativeColumn(1);    // Quantity
                            columns.RelativeColumn(2);    // PO/SO
                            columns.RelativeColumn(3);    // Note
                        });

                        table.Header(header =>
                        {
                            var headerBg = Colors.Grey.Darken3;
                            header.Cell().Background(headerBg).Padding(2).Text("Date").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Type").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Pattern").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("W\u00d7L\u00d7T").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Quantity").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("PO/SO").FontColor(Colors.White).FontSize(9).Bold();
                            header.Cell().Background(headerBg).Padding(2).Text("Note").FontColor(Colors.White).FontSize(9).Bold();
                        });

                        for (int i = 0; i < rows.Count; i++)
                        {
                            var row = rows[i];
                            var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;
                            table.Cell().Background(bg).Padding(2).Text(row.Date.ToString("yyyy-MM-dd")).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.Type).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.PatternName).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.SizeDisplay).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.Quantity.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.PoSoNumber ?? "").FontSize(9);
                            table.Cell().Background(bg).Padding(2).Text(row.Note ?? "").FontSize(9);
                        }
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    private static string CsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
