using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models;
using StudioTechBI.AgentHostDomain.Blueprints;

namespace StudioTechBI.AgentHostInfrastructure.Persistence;

/// <summary>
/// Generates a structured PDF report for a blueprint using QuestPDF and saves it to disk.
/// PDFs are stored in a "pdf" sub-folder under the configured BlueprintFolder.
/// </summary>
public sealed class BlueprintPdfService : IBlueprintPdfService
{
    private static readonly object _licenseInit = new();
    private static bool _licenseSet;

    private readonly string _pdfFolder;
    private readonly ILogger<BlueprintPdfService> _logger;

    public BlueprintPdfService(IOptions<AgentOptions> agentOptions, ILogger<BlueprintPdfService> logger)
    {
        _logger = logger;

        var baseFolder = agentOptions.Value.BlueprintFolder;
        var root = Path.IsPathRooted(baseFolder)
            ? baseFolder
            : Path.Combine(AppContext.BaseDirectory, baseFolder);
        _pdfFolder = Path.Combine(root, "pdf");

        EnsureLicense();
    }

    public string GetFilePath(Guid requestId)
    {
        // Returns a search-friendly path; actual file has a datetime prefix.
        // DownloadPdf endpoint scans the folder for any matching requestId.
        return Path.Combine(_pdfFolder, $"*_{requestId:N}.pdf");
    }

    public Task<string> SaveAsync(Guid requestId, BlueprintDocument document, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_pdfFolder);

        var now = DateTimeOffset.UtcNow;
        var fileName = $"{now:yyyyMMdd_HHmmss}_{requestId:N}.pdf";
        var filePath = Path.Combine(_pdfFolder, fileName);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => RenderHeader(c, document, requestId, now));
                page.Content().Element(c => RenderContent(c, document));
                page.Footer().Element(c => RenderFooter(c, requestId));
            });
        }).GeneratePdf(filePath);

        _logger.LogInformation("Blueprint PDF saved | RequestId={RequestId} | Path={Path}", requestId, filePath);
        return Task.FromResult(filePath);
    }

    // ── Sections ────────────────────────────────────────────────────────────

    private static void RenderHeader(IContainer container, BlueprintDocument doc, Guid requestId, DateTimeOffset generated)
    {
        container
            .BorderBottom(1).BorderColor(Colors.Blue.Darken3)
            .PaddingBottom(8)
            .Column(col =>
            {
                col.Item()
                    .Text(doc.Overview.Title.Length > 0 ? doc.Overview.Title : "Analytics Deployment Blueprint")
                    .FontSize(18).Bold().FontColor(Colors.Blue.Darken3);

                col.Item()
                    .Text($"{doc.Overview.Industry}  ·  {doc.Overview.BusinessCapability}")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);

                col.Item()
                    .Text($"Generated {generated:yyyy-MM-dd HH:mm} UTC  ·  Request {requestId}")
                    .FontSize(8).FontColor(Colors.Grey.Lighten1);
            });
    }

    private static void RenderContent(IContainer container, BlueprintDocument doc)
    {
        container.Column(col =>
        {
            col.Spacing(16);

            // 1. Overview
            RenderSectionTitle(col, "1. Overview");
            col.Item().Text(doc.Overview.Summary).FontSize(10);

            // 2. Dashboards
            if (doc.Dashboards.Count > 0)
            {
                RenderSectionTitle(col, "2. Dashboards");
                foreach (var (dashboard, di) in doc.Dashboards.Select((d, i) => (d, i)))
                {
                    col.Item().Column(inner =>
                    {
                        inner.Item().Text($"{di + 1}. {dashboard.Title}").Bold();
                        inner.Item().Text($"Purpose: {dashboard.Purpose}").FontColor(Colors.Grey.Darken2);
                        inner.Item().Text($"Audience: {dashboard.TargetAudience}").FontColor(Colors.Grey.Darken2);

                        foreach (var pg in dashboard.Pages)
                        {
                            inner.Item().PaddingTop(4).Column(pageCol =>
                            {
                                pageCol.Item().Text($"  Page: {pg.Name}  ({pg.Layout})").Italic();
                                if (pg.Visuals.Count > 0)
                                {
                                    pageCol.Item().PaddingLeft(8).Table(table =>
                                    {
                                        table.ColumnsDefinition(c =>
                                        {
                                            c.RelativeColumn(2);
                                            c.RelativeColumn(3);
                                            c.RelativeColumn(3);
                                        });

                                        table.Header(h =>
                                        {
                                            h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Type").Bold().FontSize(9);
                                            h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Title").Bold().FontSize(9);
                                            h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Metrics").Bold().FontSize(9);
                                        });

                                        foreach (var v in pg.Visuals)
                                        {
                                            TableCell(table, v.Type);
                                            TableCell(table, v.Title);
                                            TableCell(table, string.Join(", ", v.MetricRefs));
                                        }
                                    });
                                }
                            });
                        }
                    });
                }
            }

            // 3. Datasets
            if (doc.Datasets.Count > 0)
            {
                RenderSectionTitle(col, "3. Datasets");
                foreach (var ds in doc.Datasets)
                {
                    col.Item().Column(inner =>
                    {
                        inner.Item().Text($"{ds.Name}  ·  {ds.SourceSystem}  ·  Grain: {ds.Grain}").Bold();
                        if (ds.Columns.Count > 0)
                        {
                            inner.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(3);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Column").Bold().FontSize(9);
                                    h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Data Type").Bold().FontSize(9);
                                    h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Role").Bold().FontSize(9);
                                    h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Description").Bold().FontSize(9);
                                });

                                foreach (var c2 in ds.Columns)
                                {
                                    TableCell(table, c2.Name);
                                    TableCell(table, c2.DataType);
                                    TableCell(table, c2.Role);
                                    TableCell(table, c2.Description ?? string.Empty);
                                }
                            });
                        }
                    });
                }
            }

            // 4. Metrics
            if (doc.Metrics.Count > 0)
            {
                RenderSectionTitle(col, "4. Metrics");
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(3);
                        c.RelativeColumn(3);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Name").Bold().FontSize(9);
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Definition").Bold().FontSize(9);
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Expression").Bold().FontSize(9);
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Format").Bold().FontSize(9);
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Category").Bold().FontSize(9);
                    });

                    foreach (var m in doc.Metrics)
                    {
                        TableCell(table, m.Name);
                        TableCell(table, m.Definition);
                        TableCell(table, m.Expression);
                        TableCell(table, m.Format);
                        TableCell(table, m.Category);
                    }
                });
            }

            // 5. Relationships
            if (doc.Relationships.Count > 0)
            {
                RenderSectionTitle(col, "5. Relationships");
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(4);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("From").Bold().FontSize(9);
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("To").Bold().FontSize(9);
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Cardinality").Bold().FontSize(9);
                        h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Keys").Bold().FontSize(9);
                    });

                    foreach (var r in doc.Relationships)
                    {
                        TableCell(table, r.FromDataset);
                        TableCell(table, r.ToDataset);
                        TableCell(table, r.Cardinality);
                        TableCell(table, string.Join(", ", r.Keys));
                    }
                });
            }

            // 6. Recommendations
            if (doc.Recommendations.Count > 0)
            {
                RenderSectionTitle(col, "6. Recommendations");
                col.Item().Column(inner =>
                {
                    foreach (var rec in doc.Recommendations)
                        inner.Item().Text($"• {rec}");
                });
            }
        });
    }

    private static void RenderFooter(IContainer container, Guid requestId)
    {
        container
            .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
            .PaddingTop(4)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text($"Request ID: {requestId}")
                    .FontSize(7).FontColor(Colors.Grey.Lighten1);

                row.ConstantItem(60).AlignRight().Text(x =>
                {
                    x.Span("Page ").FontSize(7).FontColor(Colors.Grey.Lighten1);
                    x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Lighten1);
                    x.Span(" of ").FontSize(7).FontColor(Colors.Grey.Lighten1);
                    x.TotalPages().FontSize(7).FontColor(Colors.Grey.Lighten1);
                });
            });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static void RenderSectionTitle(ColumnDescriptor col, string title) =>
        col.Item()
            .BorderBottom(1).BorderColor(Colors.Blue.Lighten3)
            .PaddingBottom(2)
            .Text(title).FontSize(12).Bold().FontColor(Colors.Blue.Darken2);

    private static void TableCell(TableDescriptor table, string text) =>
        table.Cell()
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
            .Padding(3)
            .Text(text).FontSize(9);

    private static void EnsureLicense()
    {
        if (_licenseSet) return;
        lock (_licenseInit)
        {
            if (_licenseSet) return;
            QuestPDF.Settings.License = LicenseType.Community;
            _licenseSet = true;
        }
    }
}
