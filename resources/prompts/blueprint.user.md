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

Generate the complete Analytics Deployment Blueprint JSON for this organisation, following the schema and mandatory
design rules exactly. Use Australian business terminology throughout. Be specific to the detected industry — do not
produce a generic dashboard. Include realistic, industry-specific DAX measures, KPIs, pages, executive questions,
a full governance framework, all five quality framework assessments, expected_targets, and the complete nine-gate
self-review. Return only the JSON object — no markdown fences, no prose before or after.
