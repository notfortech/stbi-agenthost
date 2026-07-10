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
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => RenderHeader(c, document, requestId, now));
                page.Content().Element(c => RenderContent(c, document));
                page.Footer().Element(c => RenderFooter(c, requestId));
            });
        }).GeneratePdf(filePath);

        _logger.LogInformation("Blueprint PDF saved | RequestId={RequestId} | Path={Path}", requestId, filePath);
        return Task.FromResult(filePath);
    }

    // ── Header / Footer ─────────────────────────────────────────────────────

    private static void RenderHeader(IContainer container, BlueprintDocument doc, Guid requestId, DateTimeOffset generated)
    {
        container
            .BorderBottom(1).BorderColor(Colors.Blue.Darken3)
            .PaddingBottom(8)
            .Column(col =>
            {
                col.Item()
                    .Text(doc.Meta.Title.Length > 0 ? doc.Meta.Title : "Analytics Deployment Blueprint")
                    .FontSize(18).Bold().FontColor(Colors.Blue.Darken3);

                col.Item()
                    .Text($"{doc.Meta.Industry}  ·  {doc.Meta.CapabilityDomain}  ·  {doc.Meta.PrimaryAudience}")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);

                col.Item()
                    .Text($"Generated {generated:yyyy-MM-dd HH:mm} UTC  ·  Request {requestId}")
                    .FontSize(8).FontColor(Colors.Grey.Lighten1);
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

    // ── Content ──────────────────────────────────────────────────────────────

    private static void RenderContent(IContainer container, BlueprintDocument doc)
    {
        container.Column(col =>
        {
            col.Spacing(16);

            RenderSummaryStats(col, doc);
            RenderCapabilities(col, doc);
            RenderSemanticModel(col, doc);
            RenderPages(col, doc);
            RenderKpis(col, doc);
            RenderMeasures(col, doc);
            RenderExecutiveQuestions(col, doc);
            RenderSecurity(col, doc);
            RenderGovernance(col, doc);
            RenderQualityFrameworks(col, doc);
            RenderSelfReview(col, doc);
            RenderConfidence(col, doc);
        });
    }

    private static void RenderSummaryStats(ColumnDescriptor col, BlueprintDocument doc)
    {
        RenderSectionTitle(col, "Summary");
        col.Item().Row(row =>
        {
            StatTile(row, "Confidence", $"{doc.Confidence.Score:0}", doc.Confidence.Band);
            StatTile(row, "Fact Tables", doc.DataModel.FactTables.Count.ToString(), null);
            StatTile(row, "Dimensions", doc.DataModel.DimensionTables.Count.ToString(), null);
            StatTile(row, "Measures", doc.Measures.Count.ToString(), null);
            StatTile(row, "KPIs", doc.Kpis.Count.ToString(), null);
            StatTile(row, "Pages", doc.Pages.Count.ToString(), null);
            StatTile(row, "Self-Review", doc.SelfReview.OverallVerdict, $"{doc.SelfReview.CompositeScore:0}/100");
        });

        if (doc.Meta.BusinessGoal.Length > 0)
            col.Item().Text(doc.Meta.BusinessGoal).FontSize(10).Italic();
    }

    private static void StatTile(RowDescriptor row, string label, string value, string? sub)
    {
        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant()).FontSize(7).FontColor(Colors.Grey.Darken1);
            c.Item().Text(value).FontSize(15).Bold().FontColor(Colors.Blue.Darken2);
            if (!string.IsNullOrWhiteSpace(sub))
                c.Item().Text(sub).FontSize(7).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void RenderCapabilities(ColumnDescriptor col, BlueprintDocument doc)
    {
        if (doc.Capabilities.Count == 0) return;
        RenderSectionTitle(col, "Core Capabilities");
        col.Item().Column(inner =>
        {
            foreach (var c in doc.Capabilities)
                inner.Item().Text($"•  {c}").FontSize(9);
        });
    }

    // ── Semantic model ───────────────────────────────────────────────────────

    private static void RenderSemanticModel(ColumnDescriptor col, BlueprintDocument doc)
    {
        var dm = doc.DataModel;
        if (dm.FactTables.Count == 0 && dm.DimensionTables.Count == 0 && dm.Relationships.Count == 0) return;

        RenderSectionTitle(col, "Semantic Model");

        if (dm.FactTables.Count > 0)
        {
            col.Item().Text("Fact Tables").Bold().FontSize(10);
            foreach (var f in dm.FactTables)
            {
                col.Item().PaddingLeft(8).Column(inner =>
                {
                    inner.Item().Text($"{f.Name}  ·  {f.Source}  ·  Grain: {f.Grain}").Bold().FontSize(9);
                    if (f.Columns.Count > 0)
                        RenderColumnsTable(inner, f.Columns);
                });
            }
        }

        if (dm.DimensionTables.Count > 0)
        {
            col.Item().PaddingTop(4).Text("Dimension Tables").Bold().FontSize(10);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(3); c.RelativeColumn(2);
                });
                table.Header(h =>
                {
                    HeaderCell(h, "Name"); HeaderCell(h, "Type"); HeaderCell(h, "Hierarchies"); HeaderCell(h, "Key Columns");
                });
                foreach (var d in dm.DimensionTables)
                {
                    TableCell(table, d.Name);
                    TableCell(table, d.Type);
                    TableCell(table, string.Join(", ", d.Hierarchies));
                    TableCell(table, string.Join(", ", d.KeyColumns));
                }
            });
        }

        if (dm.Relationships.Count > 0)
        {
            col.Item().PaddingTop(4).Text("Relationships").Bold().FontSize(10);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3); c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(3);
                });
                table.Header(h =>
                {
                    HeaderCell(h, "From"); HeaderCell(h, "To"); HeaderCell(h, "Cardinality"); HeaderCell(h, "Active"); HeaderCell(h, "Notes");
                });
                foreach (var r in dm.Relationships)
                {
                    TableCell(table, r.From);
                    TableCell(table, r.To);
                    TableCell(table, r.Cardinality);
                    TableCell(table, r.Active ? "Yes" : "No");
                    TableCell(table, r.Notes ?? string.Empty);
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(dm.DateTable.Name))
        {
            col.Item().PaddingTop(4)
                .Text($"Date Table: {dm.DateTable.Name}  ·  Spine: {dm.DateTable.Spine}  ·  Fiscal offset: {dm.DateTable.FiscalOffset}")
                .FontSize(9).FontColor(Colors.Grey.Darken2);
        }
    }

    private static void RenderColumnsTable(ColumnDescriptor inner, IReadOnlyList<TableColumn> columns)
    {
        inner.Item().PaddingLeft(8).Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(3); });
            table.Header(h => { HeaderCell(h, "Column"); HeaderCell(h, "Type"); HeaderCell(h, "Description"); });
            foreach (var c in columns)
            {
                TableCell(table, c.Name);
                TableCell(table, c.Type);
                TableCell(table, c.Description);
            }
        });
    }

    // ── Pages ────────────────────────────────────────────────────────────────

    private static void RenderPages(ColumnDescriptor col, BlueprintDocument doc)
    {
        if (doc.Pages.Count == 0) return;
        RenderSectionTitle(col, "Report Pages");

        foreach (var (page, i) in doc.Pages.Select((p, i) => (p, i)))
        {
            col.Item().Column(inner =>
            {
                inner.Item().Text($"{i + 1}. {page.Name}  ({page.Layout})").Bold().FontSize(10);
                inner.Item().Text($"Purpose: {page.Purpose}").FontColor(Colors.Grey.Darken2);
                inner.Item().Text($"Audience: {page.Audience}").FontColor(Colors.Grey.Darken2);
                if (!string.IsNullOrWhiteSpace(page.StorytellingFlow))
                    inner.Item().Text($"Narrative: {page.StorytellingFlow}").FontColor(Colors.Grey.Darken2).Italic();

                if (page.Slicers.Count > 0)
                    inner.Item().Text($"Slicers: {string.Join(", ", page.Slicers.Select(s => s.Field))}").FontSize(8);

                if (page.Visuals.Count > 0)
                {
                    inner.Item().PaddingLeft(8).Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(3); });
                        table.Header(h => { HeaderCell(h, "Type"); HeaderCell(h, "Title"); HeaderCell(h, "Measures"); });
                        foreach (var v in page.Visuals)
                        {
                            TableCell(table, v.Type);
                            TableCell(table, v.Title);
                            TableCell(table, string.Join(", ", v.Measures));
                        }
                    });
                }

                if (page.DrillThrough is not null)
                    inner.Item().Text($"Drill-through → {page.DrillThrough.TargetPage}").FontSize(8).Italic();
            });
        }
    }

    // ── KPIs / Measures ──────────────────────────────────────────────────────

    private static void RenderKpis(ColumnDescriptor col, BlueprintDocument doc)
    {
        if (doc.Kpis.Count == 0) return;
        RenderSectionTitle(col, "KPI Definitions & Thresholds");

        foreach (var k in doc.Kpis)
        {
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Column(inner =>
            {
                inner.Item().Text(k.Name).Bold().FontSize(10);
                inner.Item().Text($"Target logic: {k.TargetLogic}").FontSize(8).FontColor(Colors.Grey.Darken2);
                inner.Item().Row(row =>
                {
                    row.RelativeItem().Text(x => { x.Span("Good ").FontColor(Colors.Green.Darken2).Bold(); x.Span(k.Thresholds.Good).FontSize(8); });
                    row.RelativeItem().Text(x => { x.Span("Warning ").FontColor(Colors.Orange.Darken2).Bold(); x.Span(k.Thresholds.Warning).FontSize(8); });
                    row.RelativeItem().Text(x => { x.Span("Critical ").FontColor(Colors.Red.Darken2).Bold(); x.Span(k.Thresholds.Critical).FontSize(8); });
                });
                inner.Item().Text($"{k.Owner} · {k.Cadence}").FontSize(8).FontColor(Colors.Grey.Darken1);
                if (!string.IsNullOrWhiteSpace(k.Actionability))
                    inner.Item().Text($"Action when Critical: {k.Actionability}").FontSize(8);
            });
        }
    }

    private static void RenderMeasures(ColumnDescriptor col, BlueprintDocument doc)
    {
        if (doc.Measures.Count == 0) return;
        RenderSectionTitle(col, "DAX Measure Catalogue");

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(4);
            });
            table.Header(h => { HeaderCell(h, "Measure"); HeaderCell(h, "Format"); HeaderCell(h, "Folder"); HeaderCell(h, "DAX"); });
            foreach (var m in doc.Measures)
            {
                TableCell(table, m.Name);
                TableCell(table, m.Format);
                TableCell(table, m.DisplayFolder);
                TableCell(table, m.Dax);
            }
        });
    }

    private static void RenderExecutiveQuestions(ColumnDescriptor col, BlueprintDocument doc)
    {
        if (doc.ExecutiveQuestions.Count == 0) return;
        RenderSectionTitle(col, "Executive Questions Answered");
        col.Item().Column(inner =>
        {
            foreach (var q in doc.ExecutiveQuestions)
                inner.Item().Text($"•  {q}").FontSize(9);
        });
    }

    // ── Security / Governance ────────────────────────────────────────────────

    private static void RenderSecurity(ColumnDescriptor col, BlueprintDocument doc)
    {
        var s = doc.Security;
        RenderSectionTitle(col, "Security");

        col.Item().Text($"RLS Required: {(s.RlsRequired ? "Yes" : "No")}  ·  Sensitivity: {s.SensitivityLabel}").FontSize(9);

        if (s.Roles.Count > 0)
        {
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(2); });
                table.Header(h => { HeaderCell(h, "Role"); HeaderCell(h, "Owner"); HeaderCell(h, "Filter DAX"); HeaderCell(h, "Access"); });
                foreach (var r in s.Roles)
                {
                    TableCell(table, r.Name);
                    TableCell(table, r.BusinessOwner);
                    TableCell(table, r.FilterDax);
                    TableCell(table, r.AccessLevel);
                }
            });
        }

        if (s.PiiColumns.Count > 0)
            col.Item().Text($"PII Columns: {string.Join(", ", s.PiiColumns)}").FontSize(8);

        if (s.ComplianceObligations.Count > 0)
        {
            col.Item().PaddingTop(2).Text("Compliance Obligations").Bold().FontSize(9);
            col.Item().Column(inner =>
            {
                foreach (var c in s.ComplianceObligations)
                    inner.Item().Text($"•  {c}").FontSize(8);
            });
        }
    }

    private static void RenderGovernance(ColumnDescriptor col, BlueprintDocument doc)
    {
        var g = doc.Governance;
        RenderSectionTitle(col, "Governance");

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(3); });
            LabelValueRow(table, "Data Owner", g.DataOwner);
            LabelValueRow(table, "KPI Owner", g.KpiOwner);
            LabelValueRow(table, "Report Owner", g.ReportOwner);
            LabelValueRow(table, "Business Steward", g.BusinessSteward);
            LabelValueRow(table, "Access Steward", g.AccessSteward);
            LabelValueRow(table, "Review Cadence", g.ReviewCadence);
            LabelValueRow(table, "Change Control", g.ChangeControl);
        });
    }

    private static void LabelValueRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Padding(3).Text(label).Bold().FontSize(9);
        table.Cell().Padding(3).Text(value).FontSize(9);
    }

    // ── Quality frameworks ────────────────────────────────────────────────────

    private static void RenderQualityFrameworks(ColumnDescriptor col, BlueprintDocument doc)
    {
        RenderSectionTitle(col, "Quality Frameworks");
        var qf = doc.QualityFrameworks;

        RenderScoreCard(col, "Audit Readiness", qf.AuditReadiness.Score, qf.AuditReadiness.Rating,
            qf.AuditReadiness.Strengths, qf.AuditReadiness.Risks);

        RenderScoreCard(col, "Dashboard Quality", qf.DashboardQuality.Score, qf.DashboardQuality.Rating,
            qf.DashboardQuality.Strengths, qf.DashboardQuality.Risks);

        RenderScoreCard(col, "KPI Quality", qf.KpiQuality.Score, qf.KpiQuality.Rating,
            [], []);
        if (!string.IsNullOrWhiteSpace(qf.KpiQuality.CoverageAssessment))
            col.Item().Text(qf.KpiQuality.CoverageAssessment).FontSize(8).Italic();

        RenderScoreCard(col, "Semantic Model Quality", qf.SemanticModelQuality.Score, qf.SemanticModelQuality.Rating,
            qf.SemanticModelQuality.Strengths, qf.SemanticModelQuality.Risks);

        RenderScoreCard(col, "Governance Framework", qf.GovernanceFramework.Score, qf.GovernanceFramework.Rating,
            [], qf.GovernanceFramework.Gaps);
    }

    private static void RenderScoreCard(
        ColumnDescriptor col, string title, double score, string rating,
        IReadOnlyList<string> strengths, IReadOnlyList<string> risksOrGaps)
    {
        col.Item().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Column(inner =>
        {
            inner.Item().Row(row =>
            {
                row.RelativeItem().Text(title).Bold().FontSize(10);
                row.ConstantItem(60).AlignRight().Text($"{score:0}/100").Bold()
                    .FontColor(score >= 80 ? Colors.Green.Darken2 : score >= 60 ? Colors.Orange.Darken2 : Colors.Red.Darken2);
            });
            inner.Item().Text(rating).FontSize(8).FontColor(Colors.Grey.Darken2);

            if (strengths.Count > 0)
                inner.Item().Text($"Strengths: {string.Join("; ", strengths)}").FontSize(8);
            if (risksOrGaps.Count > 0)
                inner.Item().Text($"Risks: {string.Join("; ", risksOrGaps)}").FontSize(8);
        });
    }

    // ── Self review ────────────────────────────────────────────────────────────

    private static void RenderSelfReview(ColumnDescriptor col, BlueprintDocument doc)
    {
        var sr = doc.SelfReview;
        RenderSectionTitle(col, "Self-Review Report");

        col.Item().Row(row =>
        {
            row.RelativeItem().Text($"Verdict: {sr.OverallVerdict}").Bold().FontSize(11)
                .FontColor(sr.OverallVerdict == "PASS" ? Colors.Green.Darken2
                    : sr.OverallVerdict == "REVISE" ? Colors.Red.Darken2 : Colors.Orange.Darken2);
            row.RelativeItem().AlignRight().Text($"Composite: {sr.CompositeScore:0}/100").Bold().FontSize(11);
        });

        if (sr.Gates.Count > 0)
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(5); });
                table.Header(h => { HeaderCell(h, "Gate"); HeaderCell(h, "Status"); HeaderCell(h, "Score"); HeaderCell(h, "Findings"); });
                foreach (var g in sr.Gates)
                {
                    TableCell(table, g.GateName);
                    TableCellColored(table, g.Status,
                        g.Status == "PASS" ? Colors.Green.Darken2 : g.Status == "FAIL" ? Colors.Red.Darken2 : Colors.Orange.Darken2);
                    TableCell(table, $"{g.Score:0}");
                    TableCell(table, string.Join("; ", g.Findings));
                }
            });
        }

        if (sr.DesignRecommendations.Count > 0)
        {
            col.Item().PaddingTop(4).Text("Design Recommendations").Bold().FontSize(9);
            col.Item().Column(inner =>
            {
                foreach (var r in sr.DesignRecommendations)
                    inner.Item().Text($"•  [{r.Priority}] {r.Recommendation} — {r.Rationale}").FontSize(8);
            });
        }

        RenderBulletList(col, "Assumptions", sr.Assumptions);
        if (sr.DesignRisks.Count > 0)
        {
            col.Item().PaddingTop(4).Text("Design Risks").Bold().FontSize(9);
            col.Item().Column(inner =>
            {
                foreach (var r in sr.DesignRisks)
                    inner.Item().Text($"•  {r.Risk} — Mitigation: {r.Mitigation}").FontSize(8);
            });
        }
        RenderBulletList(col, "Implementation Gaps", sr.ImplementationGaps);
        RenderBulletList(col, "Warnings", sr.Warnings);
    }

    private static void RenderConfidence(ColumnDescriptor col, BlueprintDocument doc)
    {
        var c = doc.Confidence;
        RenderSectionTitle(col, "Confidence Assessment");

        col.Item().Row(row =>
        {
            row.RelativeItem().Text($"{c.Score:0}/100").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
            row.RelativeItem().AlignRight().Text(c.Band).Bold().FontSize(11);
        });
        if (!string.IsNullOrWhiteSpace(c.Basis))
            col.Item().Text(c.Basis).FontSize(8).Italic();

        var d = c.DimensionScores;
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); cd.RelativeColumn(); });
            DimensionCell(table, "Industry", d.IndustryConfidence);
            DimensionCell(table, "Capability", d.CapabilityConfidence);
            DimensionCell(table, "Goal", d.GoalConfidence);
            DimensionCell(table, "Semantic Model", d.SemanticModelCompleteness);
            DimensionCell(table, "KPI", d.KpiCompleteness);
            DimensionCell(table, "Dashboard", d.DashboardCompleteness);
            DimensionCell(table, "Governance", d.GovernanceCompleteness);
        });

        RenderBulletList(col, "Assumptions", c.Assumptions);
        RenderBulletList(col, "Gaps", c.Gaps);
    }

    private static void DimensionCell(TableDescriptor table, string label, double score)
    {
        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Column(c =>
        {
            c.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken1);
            c.Item().Text($"{score:0}").FontSize(11).Bold();
        });
    }

    private static void RenderBulletList(ColumnDescriptor col, string title, IReadOnlyList<string> items)
    {
        if (items.Count == 0) return;
        col.Item().PaddingTop(4).Text(title).Bold().FontSize(9);
        col.Item().Column(inner =>
        {
            foreach (var i in items)
                inner.Item().Text($"•  {i}").FontSize(8);
        });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static void RenderSectionTitle(ColumnDescriptor col, string title) =>
        col.Item()
            .BorderBottom(1).BorderColor(Colors.Blue.Lighten3)
            .PaddingBottom(2)
            .Text(title).FontSize(12).Bold().FontColor(Colors.Blue.Darken2);

    private static void HeaderCell(TableDescriptor table, string text) =>
        table.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text(text).Bold().FontSize(8);

    private static void TableCell(TableDescriptor table, string text) =>
        table.Cell()
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
            .Padding(3)
            .Text(text).FontSize(8);

    private static void TableCellColored(TableDescriptor table, string text, string color) =>
        table.Cell()
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
            .Padding(3)
            .Text(text).FontSize(8).Bold().FontColor(color);

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
