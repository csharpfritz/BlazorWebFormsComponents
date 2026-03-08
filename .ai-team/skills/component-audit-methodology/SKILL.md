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

### 1b. WingtipToys Migration Inventory

For the WingtipToys migration specifically:
- Scan `samples/WingtipToys/WingtipToys/` for `<asp:(\w+)` patterns across .aspx, .master, .ascx files
- Scan `samples/AfterWingtipToys/` for BWFC component usage
- Note: AfterWingtipToys uses far fewer BWFC components than expected — the migration script converts simple controls (Button, TextBox, HyperLink) to native HTML. Only complex controls (ListView, LoginView, etc.) remain as BWFC components.

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

Known undocumented categories: style sub-components, infrastructure, helpers.
Field columns now documented (`docs/DataControls/FieldColumns.md` added 2026-03-08).

### 4. HTML Fidelity

**Source:** `audit-output/diff-report-post-fix.md`

Classify divergences:
- **Structural** (tag changes, missing elements) — 🔴 breaks CSS/JS
- **Attribute** (missing `id`, style differences) — 🟡 may break targeting
- **Data** (different sample content) — ⚪ audit artifact, not real gap
- **Missing variants** (❌ Missing in source B) — sample coverage gap

### 5. Migration Script Effectiveness

**Source:** `dev-docs/migration-tests/wingtiptoys-run{N}.md` and `wingtiptoys-run{N}-{date}/`

Track across runs:
- Tests passing (target: all pass)
- Layer 1 manual fixes (target: 0 — achieved as of Run 14)
- Layer 2 semantic fixes (code-behind transforms requiring context)
- Layer 1 execution time
- Transforms applied count

**Important:** Distinguish Layer 1 (mechanical regex/script transforms) from Layer 2 (semantic code-behind transforms). Layer 1 was fully automated as of Run 14. Layer 2 has 3 stable semantic gaps since Run 12.

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

## Lessons Learned

- **Style sub-component count:** Use `*Style.razor` + `*PagerSettings.razor` pattern to count (66 as of 2026-03-08). Previous audit reported 63 — the discrepancy was in the audit narrative, not the code.
- **Substitution status changed** between baseline and refresh — always re-verify deferred control status by checking actual .razor files.
- **ID rendering tracking:** Check for `IDRendering.razor` test files to count which components support `id` attribute rendering. 9 components had it as of the refresh audit.
- **CONTROL-COVERAGE.md is the authority** for migration toolkit coverage — verify it against actual component inventory each audit.
