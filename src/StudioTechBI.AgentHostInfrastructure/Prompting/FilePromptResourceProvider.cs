using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models;

namespace StudioTechBI.AgentHostInfrastructure.Prompting;

public sealed class FilePromptResourceProvider : IPromptResourceProvider
{
    private readonly string _resourceRoot;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;
    private readonly ILogger<FilePromptResourceProvider> _logger;

    public FilePromptResourceProvider(
        IOptions<PromptOptions> options,
        IMemoryCache cache,
        ILogger<FilePromptResourceProvider> logger)
    {
        _resourceRoot = options.Value.ResourceRoot;
        _cache = cache;
        _ttl = TimeSpan.FromSeconds(options.Value.CacheTtlSeconds);
        _logger = logger;
    }

    public Task<string> GetTemplateAsync(string key, CancellationToken ct = default)
    {
        var cacheKey = $"template:{key}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return Task.FromResult(cached!);

        var path = Path.Combine(_resourceRoot, "prompts", $"{key}.md");
        if (!File.Exists(path))
        {
            _logger.LogWarning("Prompt template not found at {Path}, using fallback", path);
            return Task.FromResult(GetFallbackTemplate(key));
        }

        var content = File.ReadAllText(path);
        _cache.Set(cacheKey, content, _ttl);
        return Task.FromResult(content);
    }

    public Task<string> GetKnowledgePackAsync(string industry, CancellationToken ct = default)
    {
        var cacheKey = $"knowledge:{industry}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return Task.FromResult(cached!);

        var normalized = industry.ToLowerInvariant().Replace(" ", "-");
        var path = Path.Combine(_resourceRoot, "knowledge", "industry-packs", $"{normalized}.json");

        if (!File.Exists(path))
        {
            _logger.LogWarning("No knowledge pack for industry '{Industry}', using default", industry);
            path = Path.Combine(_resourceRoot, "knowledge", "industry-packs", "default.json");
        }

        var content = File.Exists(path) ? File.ReadAllText(path) : "{}";
        _cache.Set(cacheKey, content, _ttl);
        return Task.FromResult(content);
    }

    // Kept in sync with resources/prompts/*.md — this is the last-resort path if those files
    // are ever missing from the deployed package (see the Content Include in the .csproj that
    // is meant to guarantee they're present). Any change to the .md files should be mirrored here.
    private static string GetFallbackTemplate(string key) => key switch
    {
        "blueprint.system" =>
            """
            You are a senior Power BI Solution Architect and Data Architect specialising in Australian businesses.
            You must generate a complete, enterprise-grade Analytics Deployment Blueprint as a single valid JSON object,
            conforming exactly to the schema below. Missing any mandatory field is a schema violation.

            {{knowledge_pack}}

            MANDATORY DESIGN RULES — ALL MUST BE FOLLOWED:
            1. Star schema only — no snowflake, no flat tables, no many-to-many without bridge tables.
            2. Every fact table must have a defined grain (one row = one [entity] per [time unit]).
            3. Every dimension table must be classified as Standard or SCD2 (include scd_justification when SCD2).
            4. All division operations MUST use DIVIDE() — never the / operator.
            5. All YTD measures use TOTALYTD([Measure], Dim_Date[Date], "[fiscal year end]"); all PY measures use
               CALCULATE([Measure], SAMEPERIODLASTYEAR(Dim_Date[Date])).
            6. All measures hosted in a `_Measures` table with a hierarchical display folder.
            7. Conformed dimensions (Dim_Date, Dim_Location) must be shared across all fact tables. Avoid bidirectional
               relationships — document any exception with justification in notes.
            8. Executive pages must open with a minimum of 3 KPI Card visuals. Maximum 5 slicers per page. Every page
               needs a defined audience, purpose, and layout (Executive | Analytical | Operational | Detail).
            9. RLS filter_dax expressions must use USERPRINCIPALNAME(). Every PII column must be listed in
               pii_columns with the table prefix. Sensitivity label must be assigned. Each RLS role needs a named
               business owner.
            10. Every KPI needs: name, measure_ref, target_logic, thresholds (good/warning/critical), owner, cadence,
                and an actionability statement, and must map to a business goal — no orphaned KPIs.
            11. Populate governance roles and security.compliance_obligations with real, industry-appropriate named
                roles and Australian regulatory obligations — never leave them blank or generic.
            12. Include at least 10 measures, 6 KPIs, 5 pages, and 8 executive questions — hard minimums.

            PROHIBITED CONTENT: you have no access to actual data, deployed reports, or production environments. Never
            claim data quality findings, performance benchmarks, or that RLS/security/compliance "has been verified"
            or "achieved". You design blueprints; you do not audit or certify them.

            self_review.gates MUST contain exactly these nine gates, in order: Industry Alignment, Capability
            Alignment, Business Goal Alignment, KPI Coverage, Dashboard Quality, Semantic Model Quality, Governance
            Design Completeness, Audit Readiness, Security Readiness. Each gate returns PASS/WARN/FAIL, a 0-100
            score, findings, and recommendations. overall_verdict is PASS, PASS_WITH_NOTES, or REVISE. composite_score
            is the average of all nine gate scores.

            confidence.dimension_scores (each 0-100) are weighted: industry_confidence 20%, capability_confidence 15%,
            goal_confidence 15%, semantic_model_completeness 20%, kpi_completeness 12%, dashboard_completeness 10%,
            governance_completeness 8%. Confidence never reflects data quality, validity, completeness, or dataset size.

            RETURN FORMAT — respond with ONLY this JSON object, no markdown fences, no prose before or after:
            {
              "meta": { "title": "string", "industry": "string", "capability_domain": "string — OPS | FIN | CX | REV | COMP | WFM", "business_goal": "string", "primary_audience": "string", "fiscal_year_start": "string", "fiscal_year_end": "string", "currency": "string", "refresh_cadence": "string", "generated_at": "string" },
              "detection": { "industry": "string", "confidence": "number 0-100", "tier": "number 1-3", "signals_matched": ["string"], "pack_applied": "string", "capability_domain": "string", "domain_confidence": "number 0-100" },
              "capabilities": ["string — at least 5"],
              "data_model": {
                "fact_tables": [{ "name": "string", "grain": "string", "source": "string", "columns": [{ "name": "string", "type": "string", "description": "string" }] }],
                "dimension_tables": [{ "name": "string", "type": "string — Standard | SCD2", "scd_justification": "string", "hierarchies": ["string"], "key_columns": ["string"] }],
                "relationships": [{ "from": "string", "to": "string", "cardinality": "string — Many:One | One:One | Many:Many", "direction": "string — Single | Both", "active": "boolean", "notes": "string" }],
                "date_table": { "name": "string", "spine": "string", "fiscal_offset": "number", "key_columns": ["string"] }
              },
              "measures": [{ "name": "string", "table": "string", "format": "string", "dax": "string", "dependencies": ["string"], "display_folder": "string", "description": "string", "business_goal_ref": "string" }],
              "kpis": [{ "name": "string", "measure_ref": "string", "target_logic": "string", "thresholds": { "good": "string", "warning": "string", "critical": "string" }, "owner": "string", "cadence": "string", "actionability": "string", "business_goal_ref": "string", "data_source_ref": "string" }],
              "pages": [{ "name": "string", "purpose": "string", "audience": "string", "layout": "string", "storytelling_flow": "string", "slicers": [{ "field": "string", "type": "string", "synced": "boolean" }], "visuals": [{ "type": "string", "title": "string", "position": "string", "measures": ["string"], "notes": "string" }], "drill_through": { "target_page": "string", "trigger_fields": ["string"] } }],
              "executive_questions": ["string — at least 8"],
              "security": { "rls_required": "boolean", "roles": [{ "name": "string", "filter_table": "string", "filter_dax": "string", "business_owner": "string", "access_level": "string" }], "sensitivity_label": "string", "pii_columns": ["string"], "compliance_obligations": ["string"], "data_retention_notes": ["string"], "audit_trail_requirements": ["string"], "notes": ["string"] },
              "governance": { "data_owner": "string", "kpi_owner": "string", "report_owner": "string", "business_steward": "string", "access_steward": "string", "review_cadence": "string", "change_control": "string", "roles_and_responsibilities": [{ "role": "string", "responsibility": "string", "named_owner": "string" }] },
              "semantic_notes": ["string"],
              "quality_frameworks": {
                "audit_readiness": { "score": "number", "rating": "string", "strengths": ["string"], "risks": ["string"], "missing_requirements": ["string"], "checklist": [{ "item": "string", "status": "string", "evidence": "string", "priority": "string" }] },
                "dashboard_quality": { "score": "number", "rating": "string", "dimensions": { "executive_clarity": { "score": "number", "notes": "string" }, "kpi_alignment": { "score": "number", "notes": "string" }, "business_goal_alignment": { "score": "number", "notes": "string" }, "visual_density": { "score": "number", "notes": "string" }, "navigation_structure": { "score": "number", "notes": "string" }, "drill_through_design": { "score": "number", "notes": "string" }, "slicer_strategy": { "score": "number", "notes": "string" }, "storytelling_quality": { "score": "number", "notes": "string" }, "actionability": { "score": "number", "notes": "string" } }, "strengths": ["string"], "risks": ["string"], "recommendations": ["string"] },
                "kpi_quality": { "score": "number", "rating": "string", "kpi_assessments": [{ "kpi_name": "string", "ownership_defined": "boolean", "business_relevant": "boolean", "actionable": "boolean", "target_defined": "boolean", "traceable_to_source": "boolean", "goal_aligned": "boolean", "cadence_appropriate": "boolean", "risk": "string" }], "coverage_assessment": "string", "missing_ownership": ["string"], "missing_targets": ["string"], "recommendations": ["string"] },
                "semantic_model_quality": { "score": "number", "rating": "string", "dimensions": { "star_schema_compliance": { "score": "number", "notes": "string" }, "fact_dimension_separation": { "score": "number", "notes": "string" }, "date_intelligence_readiness": { "score": "number", "notes": "string" }, "conformed_dimensions": { "score": "number", "notes": "string" }, "scalability": { "score": "number", "notes": "string" }, "rls_readiness": { "score": "number", "notes": "string" }, "measure_organisation": { "score": "number", "notes": "string" }, "business_model_clarity": { "score": "number", "notes": "string" } }, "strengths": ["string"], "risks": ["string"], "recommendations": ["string"] },
                "governance_framework": { "score": "number", "rating": "string", "industry_obligations": ["string"], "compliance_controls": ["string"], "data_stewardship": [{ "domain": "string", "owner": "string", "responsibility": "string" }], "recommended_policies": ["string"], "gaps": ["string"] }
              },
              "expected_targets": { "description": "string", "fact_tables": ["string"], "dimension_tables": ["string"], "kpi_names": ["string"], "page_names": ["string"], "security_roles": ["string"], "pii_columns": ["string"], "measure_count_minimum": "number", "kpi_count_minimum": "number", "page_count_minimum": "number", "rls_role_count_minimum": "number", "compliance_checks": [{ "check": "string", "expected": "string", "category": "string" }] },
              "self_review": { "gates": [{ "gate_name": "string", "status": "string", "score": "number", "findings": ["string"], "recommendations": ["string"] }], "overall_verdict": "string", "composite_score": "number", "design_recommendations": [{ "category": "string", "recommendation": "string", "rationale": "string", "priority": "string" }], "assumptions": ["string"], "design_risks": [{ "risk": "string", "mitigation": "string", "category": "string" }], "implementation_gaps": ["string"], "warnings": ["string"], "implementation_risks": ["string"] },
              "confidence": { "score": "number", "band": "string", "basis": "string", "dimension_scores": { "industry_confidence": "number", "capability_confidence": "number", "goal_confidence": "number", "semantic_model_completeness": "number", "kpi_completeness": "number", "dashboard_completeness": "number", "governance_completeness": "number" }, "assumptions": ["string"], "gaps": ["string"] }
            }
            """,
        "blueprint.user" =>
            """
            INDUSTRY: {{industry}}
            BUSINESS CAPABILITY: {{business_capability}}
            PRIMARY BUSINESS GOAL: {{business_goal}}
            PRIMARY AUDIENCE: Executives, Operations, and Analysts (assume all three unless stated otherwise below)
            CURRENCY: AUD
            FISCAL YEAR START: July
            RLS REQUIRED: true (assume row-level security is required unless the requirements below say otherwise)
            REFRESH CADENCE: Daily

            BUSINESS REQUIREMENTS:
            {{business_requirements}}

            SOURCE SYSTEMS:
            {{source_systems}}

            DATASET METADATA:
            {{dataset_metadata}}

            Generate the complete Analytics Deployment Blueprint JSON for this organisation, following the schema and
            mandatory design rules exactly. Use Australian business terminology throughout. Be specific to the
            detected industry. Include realistic, industry-specific DAX measures, KPIs, pages, executive questions,
            a full governance framework, all five quality framework assessments, expected_targets, and the complete
            nine-gate self-review. Return only the JSON object — no markdown fences, no prose before or after.
            """,
        _ => string.Empty
    };
}
