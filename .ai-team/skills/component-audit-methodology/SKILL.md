---
name: "component-audit-methodology"
description: "Reusable methodology for auditing BWFC library completeness, coverage, and fidelity"
domain: "project-management"
confidence: "high"
source: "earned"
---

## Context

Periodic audits of the BlazorWebFormsComponents library track progress across 6 dimensions: component inventory, Web Forms coverage, documentation coverage, HTML fidelity, migration script effectiveness, and overall health. This skill captures the methodology so future audits are consistent and comparable.

## Audit Dimensions

### 1. Component Inventory

**Source:** `glob src/BlazorWebFormsComponents/**/*.razor` (exclude `_Imports.razor`)

Categorize all .razor files into:
- **Primary controls** — Direct Web Forms control equivalents (the ones in status.md)
- **Style sub-components** — Match pattern `*Style.razor` or `*PagerSettings.razor`
- **Field columns** — BoundField, ButtonField, HyperLinkField, TemplateField
- **Child/structural** — MenuItem, TreeNode, GridViewRow, etc.
- **Infrastructure** — Content, ContentPlaceHolder, MasterPage, WebFormsPage, Page, NamingContainer, EmptyLayout
- **Helpers** — HelperComponents/ directory
- **Theming** — Theming/ directory
- **Validation infra** — Validations/ directory (not the validators themselves)

Count: Expect ~153 total (87 non-style, 66 style/pager).

### 2. Web Forms Coverage Matrix

Compare implemented controls against ASP.NET Web Forms 4.8 control categories:
- Editor Controls (~28)
- Data Controls (~8)
- Validation Controls (~6-8)
- Navigation Controls (~3)
- Login Controls (~7)
- AJAX Controls (~5-6)

Status values: ✅ Complete, ⚠️ Partial, ❌ Missing, ⏸️ Deferred

### 3. Documentation Coverage

**Source:** `mkdocs.yml` nav section → list all documented components.
Cross-reference against implemented .razor files to find:
- Implementation without docs (gap)
- Docs without matching implementation (stale)

Known undocumented categories: field columns, style sub-components, infrastructure, helpers.

### 4. HTML Fidelity

**Source:** `audit-output/diff-report-post-fix.md`

Classify divergences:
- **Structural** (tag changes, missing elements) — 🔴 breaks CSS/JS
- **Attribute** (missing `id`, style differences) — 🟡 may break targeting
- **Data** (different sample content) — ⚪ audit artifact, not real gap
- **Missing variants** (❌ Missing in source B) — sample coverage gap

### 5. Migration Script Effectiveness

**Source:** `dev-docs/migration-tests/wingtiptoys-run{N}-{date}/`

Track across runs:
- Tests passing (target: all pass)
- Manual fixes required (target: 0)
- Total migration time
- Transforms applied count

### 6. Health Metrics

Key numbers to report every audit:
- Total .razor count
- Primary control count
- Coverage % (implemented / feasible targets)
- Test count
- Manual migration fixes remaining

## Output Template

Save to `dev-docs/component-audit-{date}.md` with sections:
1. Executive Summary
2. Component Inventory (tables)
3. Web Forms Coverage (matrix)
4. Documentation Coverage (gap analysis)
5. HTML Fidelity Summary (pattern analysis)
6. Migration Script Coverage (run comparison)
7. Overall Assessment (summary + top 5 priorities)

## Frequency

Run audit after each major milestone (sprint completion, migration run improvements, architecture changes).
