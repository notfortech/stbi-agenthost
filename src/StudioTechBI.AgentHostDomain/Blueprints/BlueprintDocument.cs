using System.Text.Json.Serialization;

namespace StudioTechBI.AgentHostDomain.Blueprints;

/// <summary>
/// Analytics Deployment Blueprint. Schema mirrors resources/prompts/blueprint.system.md's
/// RETURN FORMAT exactly — see prompts/DashboardBlueprintSchema.md for the full field-by-field
/// specification and mandatory minimums.
/// </summary>
public sealed class BlueprintDocument
{
    [JsonPropertyName("meta")]
    public BlueprintMeta Meta { get; init; } = new();

    [JsonPropertyName("detection")]
    public BlueprintDetection Detection { get; init; } = new();

    [JsonPropertyName("capabilities")]
    public IReadOnlyList<string> Capabilities { get; init; } = [];

    [JsonPropertyName("data_model")]
    public DataModel DataModel { get; init; } = new();

    [JsonPropertyName("measures")]
    public IReadOnlyList<Measure> Measures { get; init; } = [];

    [JsonPropertyName("kpis")]
    public IReadOnlyList<Kpi> Kpis { get; init; } = [];

    [JsonPropertyName("pages")]
    public IReadOnlyList<DashboardPage> Pages { get; init; } = [];

    [JsonPropertyName("executive_questions")]
    public IReadOnlyList<string> ExecutiveQuestions { get; init; } = [];

    [JsonPropertyName("security")]
    public SecurityBlock Security { get; init; } = new();

    [JsonPropertyName("governance")]
    public GovernanceBlock Governance { get; init; } = new();

    [JsonPropertyName("semantic_notes")]
    public IReadOnlyList<string> SemanticNotes { get; init; } = [];

    [JsonPropertyName("quality_frameworks")]
    public QualityFrameworks QualityFrameworks { get; init; } = new();

    [JsonPropertyName("expected_targets")]
    public ExpectedTargets ExpectedTargets { get; init; } = new();

    [JsonPropertyName("self_review")]
    public SelfReview SelfReview { get; init; } = new();

    [JsonPropertyName("confidence")]
    public ConfidenceBlock Confidence { get; init; } = new();
}

// ── meta / detection ─────────────────────────────────────────────────────────

public sealed class BlueprintMeta
{
    [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;
    [JsonPropertyName("industry")] public string Industry { get; init; } = string.Empty;
    [JsonPropertyName("capability_domain")] public string CapabilityDomain { get; init; } = string.Empty;
    [JsonPropertyName("business_goal")] public string BusinessGoal { get; init; } = string.Empty;
    [JsonPropertyName("primary_audience")] public string PrimaryAudience { get; init; } = string.Empty;
    [JsonPropertyName("fiscal_year_start")] public string FiscalYearStart { get; init; } = string.Empty;
    [JsonPropertyName("fiscal_year_end")] public string FiscalYearEnd { get; init; } = string.Empty;
    [JsonPropertyName("currency")] public string Currency { get; init; } = string.Empty;
    [JsonPropertyName("refresh_cadence")] public string RefreshCadence { get; init; } = string.Empty;
    [JsonPropertyName("generated_at")] public string GeneratedAt { get; init; } = string.Empty;
}

public sealed class BlueprintDetection
{
    [JsonPropertyName("industry")] public string Industry { get; init; } = string.Empty;
    [JsonPropertyName("confidence")] public double Confidence { get; init; }
    [JsonPropertyName("tier")] public int Tier { get; init; }
    [JsonPropertyName("signals_matched")] public IReadOnlyList<string> SignalsMatched { get; init; } = [];
    [JsonPropertyName("pack_applied")] public string PackApplied { get; init; } = string.Empty;
    [JsonPropertyName("capability_domain")] public string CapabilityDomain { get; init; } = string.Empty;
    [JsonPropertyName("domain_confidence")] public double DomainConfidence { get; init; }
}

// ── data_model ────────────────────────────────────────────────────────────────

public sealed class DataModel
{
    [JsonPropertyName("fact_tables")] public IReadOnlyList<FactTable> FactTables { get; init; } = [];
    [JsonPropertyName("dimension_tables")] public IReadOnlyList<DimensionTable> DimensionTables { get; init; } = [];
    [JsonPropertyName("relationships")] public IReadOnlyList<DataModelRelationship> Relationships { get; init; } = [];
    [JsonPropertyName("date_table")] public DateTableSpec DateTable { get; init; } = new();
}

public sealed class FactTable
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("grain")] public string Grain { get; init; } = string.Empty;
    [JsonPropertyName("source")] public string Source { get; init; } = string.Empty;
    [JsonPropertyName("columns")] public IReadOnlyList<TableColumn> Columns { get; init; } = [];
}

public sealed class TableColumn
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; init; } = string.Empty;
}

public sealed class DimensionTable
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
    [JsonPropertyName("scd_justification")] public string? ScdJustification { get; init; }
    [JsonPropertyName("hierarchies")] public IReadOnlyList<string> Hierarchies { get; init; } = [];
    [JsonPropertyName("key_columns")] public IReadOnlyList<string> KeyColumns { get; init; } = [];
}

public sealed class DataModelRelationship
{
    [JsonPropertyName("from")] public string From { get; init; } = string.Empty;
    [JsonPropertyName("to")] public string To { get; init; } = string.Empty;
    [JsonPropertyName("cardinality")] public string Cardinality { get; init; } = string.Empty;
    [JsonPropertyName("direction")] public string Direction { get; init; } = string.Empty;
    [JsonPropertyName("active")] public bool Active { get; init; }
    [JsonPropertyName("notes")] public string? Notes { get; init; }
}

public sealed class DateTableSpec
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("spine")] public string Spine { get; init; } = string.Empty;
    [JsonPropertyName("fiscal_offset")] public int FiscalOffset { get; init; }
    [JsonPropertyName("key_columns")] public IReadOnlyList<string> KeyColumns { get; init; } = [];
}

// ── measures / kpis ───────────────────────────────────────────────────────────

public sealed class Measure
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("table")] public string Table { get; init; } = string.Empty;
    [JsonPropertyName("format")] public string Format { get; init; } = string.Empty;
    [JsonPropertyName("dax")] public string Dax { get; init; } = string.Empty;
    [JsonPropertyName("dependencies")] public IReadOnlyList<string> Dependencies { get; init; } = [];
    [JsonPropertyName("display_folder")] public string DisplayFolder { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; init; } = string.Empty;
    [JsonPropertyName("business_goal_ref")] public string BusinessGoalRef { get; init; } = string.Empty;
}

public sealed class Kpi
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("measure_ref")] public string MeasureRef { get; init; } = string.Empty;
    [JsonPropertyName("target_logic")] public string TargetLogic { get; init; } = string.Empty;
    [JsonPropertyName("thresholds")] public KpiThresholds Thresholds { get; init; } = new();
    [JsonPropertyName("owner")] public string Owner { get; init; } = string.Empty;
    [JsonPropertyName("cadence")] public string Cadence { get; init; } = string.Empty;
    [JsonPropertyName("actionability")] public string Actionability { get; init; } = string.Empty;
    [JsonPropertyName("business_goal_ref")] public string BusinessGoalRef { get; init; } = string.Empty;
    [JsonPropertyName("data_source_ref")] public string DataSourceRef { get; init; } = string.Empty;
}

public sealed class KpiThresholds
{
    [JsonPropertyName("good")] public string Good { get; init; } = string.Empty;
    [JsonPropertyName("warning")] public string Warning { get; init; } = string.Empty;
    [JsonPropertyName("critical")] public string Critical { get; init; } = string.Empty;
}

// ── pages ─────────────────────────────────────────────────────────────────────

public sealed class DashboardPage
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("purpose")] public string Purpose { get; init; } = string.Empty;
    [JsonPropertyName("audience")] public string Audience { get; init; } = string.Empty;
    [JsonPropertyName("layout")] public string Layout { get; init; } = string.Empty;
    [JsonPropertyName("storytelling_flow")] public string StorytellingFlow { get; init; } = string.Empty;
    [JsonPropertyName("slicers")] public IReadOnlyList<PageSlicer> Slicers { get; init; } = [];
    [JsonPropertyName("visuals")] public IReadOnlyList<PageVisual> Visuals { get; init; } = [];
    [JsonPropertyName("drill_through")] public DrillThrough? DrillThrough { get; init; }
}

public sealed class PageSlicer
{
    [JsonPropertyName("field")] public string Field { get; init; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
    [JsonPropertyName("synced")] public bool Synced { get; init; }
}

public sealed class PageVisual
{
    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
    [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;
    [JsonPropertyName("position")] public string Position { get; init; } = string.Empty;
    [JsonPropertyName("measures")] public IReadOnlyList<string> Measures { get; init; } = [];
    [JsonPropertyName("notes")] public string? Notes { get; init; }
}

public sealed class DrillThrough
{
    [JsonPropertyName("target_page")] public string TargetPage { get; init; } = string.Empty;
    [JsonPropertyName("trigger_fields")] public IReadOnlyList<string> TriggerFields { get; init; } = [];
}

// ── security / governance ──────────────────────────────────────────────────────

public sealed class SecurityBlock
{
    [JsonPropertyName("rls_required")] public bool RlsRequired { get; init; }
    [JsonPropertyName("roles")] public IReadOnlyList<SecurityRole> Roles { get; init; } = [];
    [JsonPropertyName("sensitivity_label")] public string SensitivityLabel { get; init; } = string.Empty;
    [JsonPropertyName("pii_columns")] public IReadOnlyList<string> PiiColumns { get; init; } = [];
    [JsonPropertyName("compliance_obligations")] public IReadOnlyList<string> ComplianceObligations { get; init; } = [];
    [JsonPropertyName("data_retention_notes")] public IReadOnlyList<string> DataRetentionNotes { get; init; } = [];
    [JsonPropertyName("audit_trail_requirements")] public IReadOnlyList<string> AuditTrailRequirements { get; init; } = [];
    [JsonPropertyName("notes")] public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed class SecurityRole
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("filter_table")] public string FilterTable { get; init; } = string.Empty;
    [JsonPropertyName("filter_dax")] public string FilterDax { get; init; } = string.Empty;
    [JsonPropertyName("business_owner")] public string BusinessOwner { get; init; } = string.Empty;
    [JsonPropertyName("access_level")] public string AccessLevel { get; init; } = string.Empty;
}

public sealed class GovernanceBlock
{
    [JsonPropertyName("data_owner")] public string DataOwner { get; init; } = string.Empty;
    [JsonPropertyName("kpi_owner")] public string KpiOwner { get; init; } = string.Empty;
    [JsonPropertyName("report_owner")] public string ReportOwner { get; init; } = string.Empty;
    [JsonPropertyName("business_steward")] public string BusinessSteward { get; init; } = string.Empty;
    [JsonPropertyName("access_steward")] public string AccessSteward { get; init; } = string.Empty;
    [JsonPropertyName("review_cadence")] public string ReviewCadence { get; init; } = string.Empty;
    [JsonPropertyName("change_control")] public string ChangeControl { get; init; } = string.Empty;
    [JsonPropertyName("roles_and_responsibilities")] public IReadOnlyList<GovernanceRole> RolesAndResponsibilities { get; init; } = [];
}

public sealed class GovernanceRole
{
    [JsonPropertyName("role")] public string Role { get; init; } = string.Empty;
    [JsonPropertyName("responsibility")] public string Responsibility { get; init; } = string.Empty;
    [JsonPropertyName("named_owner")] public string NamedOwner { get; init; } = string.Empty;
}

// ── quality frameworks ─────────────────────────────────────────────────────────

public sealed class QualityFrameworks
{
    [JsonPropertyName("audit_readiness")] public AuditReadiness AuditReadiness { get; init; } = new();
    [JsonPropertyName("dashboard_quality")] public DashboardQuality DashboardQuality { get; init; } = new();
    [JsonPropertyName("kpi_quality")] public KpiQuality KpiQuality { get; init; } = new();
    [JsonPropertyName("semantic_model_quality")] public SemanticModelQuality SemanticModelQuality { get; init; } = new();
    [JsonPropertyName("governance_framework")] public GovernanceFrameworkAssessment GovernanceFramework { get; init; } = new();
}

public sealed class ScoredDimension
{
    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("notes")] public string Notes { get; init; } = string.Empty;
}

public sealed class AuditReadiness
{
    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("rating")] public string Rating { get; init; } = string.Empty;
    [JsonPropertyName("strengths")] public IReadOnlyList<string> Strengths { get; init; } = [];
    [JsonPropertyName("risks")] public IReadOnlyList<string> Risks { get; init; } = [];
    [JsonPropertyName("missing_requirements")] public IReadOnlyList<string> MissingRequirements { get; init; } = [];
    [JsonPropertyName("checklist")] public IReadOnlyList<AuditChecklistItem> Checklist { get; init; } = [];
}

public sealed class AuditChecklistItem
{
    [JsonPropertyName("item")] public string Item { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("evidence")] public string Evidence { get; init; } = string.Empty;
    [JsonPropertyName("priority")] public string Priority { get; init; } = string.Empty;
}

public sealed class DashboardQuality
{
    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("rating")] public string Rating { get; init; } = string.Empty;
    [JsonPropertyName("dimensions")] public DashboardQualityDimensions Dimensions { get; init; } = new();
    [JsonPropertyName("strengths")] public IReadOnlyList<string> Strengths { get; init; } = [];
    [JsonPropertyName("risks")] public IReadOnlyList<string> Risks { get; init; } = [];
    [JsonPropertyName("recommendations")] public IReadOnlyList<string> Recommendations { get; init; } = [];
}

public sealed class DashboardQualityDimensions
{
    [JsonPropertyName("executive_clarity")] public ScoredDimension ExecutiveClarity { get; init; } = new();
    [JsonPropertyName("kpi_alignment")] public ScoredDimension KpiAlignment { get; init; } = new();
    [JsonPropertyName("business_goal_alignment")] public ScoredDimension BusinessGoalAlignment { get; init; } = new();
    [JsonPropertyName("visual_density")] public ScoredDimension VisualDensity { get; init; } = new();
    [JsonPropertyName("navigation_structure")] public ScoredDimension NavigationStructure { get; init; } = new();
    [JsonPropertyName("drill_through_design")] public ScoredDimension DrillThroughDesign { get; init; } = new();
    [JsonPropertyName("slicer_strategy")] public ScoredDimension SlicerStrategy { get; init; } = new();
    [JsonPropertyName("storytelling_quality")] public ScoredDimension StorytellingQuality { get; init; } = new();
    [JsonPropertyName("actionability")] public ScoredDimension Actionability { get; init; } = new();
}

public sealed class KpiQuality
{
    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("rating")] public string Rating { get; init; } = string.Empty;
    [JsonPropertyName("kpi_assessments")] public IReadOnlyList<KpiAssessment> KpiAssessments { get; init; } = [];
    [JsonPropertyName("coverage_assessment")] public string CoverageAssessment { get; init; } = string.Empty;
    [JsonPropertyName("missing_ownership")] public IReadOnlyList<string> MissingOwnership { get; init; } = [];
    [JsonPropertyName("missing_targets")] public IReadOnlyList<string> MissingTargets { get; init; } = [];
    [JsonPropertyName("recommendations")] public IReadOnlyList<string> Recommendations { get; init; } = [];
}

public sealed class KpiAssessment
{
    [JsonPropertyName("kpi_name")] public string KpiName { get; init; } = string.Empty;
    [JsonPropertyName("ownership_defined")] public bool OwnershipDefined { get; init; }
    [JsonPropertyName("business_relevant")] public bool BusinessRelevant { get; init; }
    [JsonPropertyName("actionable")] public bool Actionable { get; init; }
    [JsonPropertyName("target_defined")] public bool TargetDefined { get; init; }
    [JsonPropertyName("traceable_to_source")] public bool TraceableToSource { get; init; }
    [JsonPropertyName("goal_aligned")] public bool GoalAligned { get; init; }
    [JsonPropertyName("cadence_appropriate")] public bool CadenceAppropriate { get; init; }
    [JsonPropertyName("risk")] public string Risk { get; init; } = string.Empty;
}

public sealed class SemanticModelQuality
{
    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("rating")] public string Rating { get; init; } = string.Empty;
    [JsonPropertyName("dimensions")] public SemanticModelQualityDimensions Dimensions { get; init; } = new();
    [JsonPropertyName("strengths")] public IReadOnlyList<string> Strengths { get; init; } = [];
    [JsonPropertyName("risks")] public IReadOnlyList<string> Risks { get; init; } = [];
    [JsonPropertyName("recommendations")] public IReadOnlyList<string> Recommendations { get; init; } = [];
}

public sealed class SemanticModelQualityDimensions
{
    [JsonPropertyName("star_schema_compliance")] public ScoredDimension StarSchemaCompliance { get; init; } = new();
    [JsonPropertyName("fact_dimension_separation")] public ScoredDimension FactDimensionSeparation { get; init; } = new();
    [JsonPropertyName("date_intelligence_readiness")] public ScoredDimension DateIntelligenceReadiness { get; init; } = new();
    [JsonPropertyName("conformed_dimensions")] public ScoredDimension ConformedDimensions { get; init; } = new();
    [JsonPropertyName("scalability")] public ScoredDimension Scalability { get; init; } = new();
    [JsonPropertyName("rls_readiness")] public ScoredDimension RlsReadiness { get; init; } = new();
    [JsonPropertyName("measure_organisation")] public ScoredDimension MeasureOrganisation { get; init; } = new();
    [JsonPropertyName("business_model_clarity")] public ScoredDimension BusinessModelClarity { get; init; } = new();
}

public sealed class GovernanceFrameworkAssessment
{
    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("rating")] public string Rating { get; init; } = string.Empty;
    [JsonPropertyName("industry_obligations")] public IReadOnlyList<string> IndustryObligations { get; init; } = [];
    [JsonPropertyName("compliance_controls")] public IReadOnlyList<string> ComplianceControls { get; init; } = [];
    [JsonPropertyName("data_stewardship")] public IReadOnlyList<DataStewardshipEntry> DataStewardship { get; init; } = [];
    [JsonPropertyName("recommended_policies")] public IReadOnlyList<string> RecommendedPolicies { get; init; } = [];
    [JsonPropertyName("gaps")] public IReadOnlyList<string> Gaps { get; init; } = [];
}

public sealed class DataStewardshipEntry
{
    [JsonPropertyName("domain")] public string Domain { get; init; } = string.Empty;
    [JsonPropertyName("owner")] public string Owner { get; init; } = string.Empty;
    [JsonPropertyName("responsibility")] public string Responsibility { get; init; } = string.Empty;
}

// ── expected targets (Audit Agent compatibility) ───────────────────────────────

public sealed class ExpectedTargets
{
    [JsonPropertyName("description")] public string Description { get; init; } = string.Empty;
    [JsonPropertyName("fact_tables")] public IReadOnlyList<string> FactTables { get; init; } = [];
    [JsonPropertyName("dimension_tables")] public IReadOnlyList<string> DimensionTables { get; init; } = [];
    [JsonPropertyName("kpi_names")] public IReadOnlyList<string> KpiNames { get; init; } = [];
    [JsonPropertyName("page_names")] public IReadOnlyList<string> PageNames { get; init; } = [];
    [JsonPropertyName("security_roles")] public IReadOnlyList<string> SecurityRoles { get; init; } = [];
    [JsonPropertyName("pii_columns")] public IReadOnlyList<string> PiiColumns { get; init; } = [];
    [JsonPropertyName("measure_count_minimum")] public int MeasureCountMinimum { get; init; }
    [JsonPropertyName("kpi_count_minimum")] public int KpiCountMinimum { get; init; }
    [JsonPropertyName("page_count_minimum")] public int PageCountMinimum { get; init; }
    [JsonPropertyName("rls_role_count_minimum")] public int RlsRoleCountMinimum { get; init; }
    [JsonPropertyName("compliance_checks")] public IReadOnlyList<ComplianceCheck> ComplianceChecks { get; init; } = [];
}

public sealed class ComplianceCheck
{
    [JsonPropertyName("check")] public string Check { get; init; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; init; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; init; } = string.Empty;
}

// ── self review ─────────────────────────────────────────────────────────────────

public sealed class SelfReview
{
    [JsonPropertyName("gates")] public IReadOnlyList<SelfReviewGate> Gates { get; init; } = [];
    [JsonPropertyName("overall_verdict")] public string OverallVerdict { get; init; } = string.Empty;
    [JsonPropertyName("composite_score")] public double CompositeScore { get; init; }
    [JsonPropertyName("design_recommendations")] public IReadOnlyList<DesignRecommendation> DesignRecommendations { get; init; } = [];
    [JsonPropertyName("assumptions")] public IReadOnlyList<string> Assumptions { get; init; } = [];
    [JsonPropertyName("design_risks")] public IReadOnlyList<DesignRisk> DesignRisks { get; init; } = [];
    [JsonPropertyName("implementation_gaps")] public IReadOnlyList<string> ImplementationGaps { get; init; } = [];
    [JsonPropertyName("warnings")] public IReadOnlyList<string> Warnings { get; init; } = [];
    [JsonPropertyName("implementation_risks")] public IReadOnlyList<string> ImplementationRisks { get; init; } = [];
}

public sealed class SelfReviewGate
{
    [JsonPropertyName("gate_name")] public string GateName { get; init; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("findings")] public IReadOnlyList<string> Findings { get; init; } = [];
    [JsonPropertyName("recommendations")] public IReadOnlyList<string> Recommendations { get; init; } = [];
}

public sealed class DesignRecommendation
{
    [JsonPropertyName("category")] public string Category { get; init; } = string.Empty;
    [JsonPropertyName("recommendation")] public string Recommendation { get; init; } = string.Empty;
    [JsonPropertyName("rationale")] public string Rationale { get; init; } = string.Empty;
    [JsonPropertyName("priority")] public string Priority { get; init; } = string.Empty;
}

public sealed class DesignRisk
{
    [JsonPropertyName("risk")] public string Risk { get; init; } = string.Empty;
    [JsonPropertyName("mitigation")] public string Mitigation { get; init; } = string.Empty;
    [JsonPropertyName("category")] public string Category { get; init; } = string.Empty;
}

// ── confidence ─────────────────────────────────────────────────────────────────

public sealed class ConfidenceBlock
{
    [JsonPropertyName("score")] public double Score { get; init; }
    [JsonPropertyName("band")] public string Band { get; init; } = string.Empty;
    [JsonPropertyName("basis")] public string Basis { get; init; } = string.Empty;
    [JsonPropertyName("dimension_scores")] public ConfidenceDimensionScores DimensionScores { get; init; } = new();
    [JsonPropertyName("assumptions")] public IReadOnlyList<string> Assumptions { get; init; } = [];
    [JsonPropertyName("gaps")] public IReadOnlyList<string> Gaps { get; init; } = [];
}

public sealed class ConfidenceDimensionScores
{
    [JsonPropertyName("industry_confidence")] public double IndustryConfidence { get; init; }
    [JsonPropertyName("capability_confidence")] public double CapabilityConfidence { get; init; }
    [JsonPropertyName("goal_confidence")] public double GoalConfidence { get; init; }
    [JsonPropertyName("semantic_model_completeness")] public double SemanticModelCompleteness { get; init; }
    [JsonPropertyName("kpi_completeness")] public double KpiCompleteness { get; init; }
    [JsonPropertyName("dashboard_completeness")] public double DashboardCompleteness { get; init; }
    [JsonPropertyName("governance_completeness")] public double GovernanceCompleteness { get; init; }
}
