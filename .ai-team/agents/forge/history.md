# Project Context

- **Owner:** Jeffrey T. Fritz
- **Project:** BlazorWebFormsComponents  Blazor components emulating ASP.NET Web Forms controls for migration
- **Stack:** C#, Blazor, .NET, ASP.NET Web Forms, bUnit, xUnit, MkDocs, Playwright
- **Created:** 2026-02-10

## Core Context

<!-- Summarized 2026-03-04 by Scribe — originals in history-archive.md -->

M1–M16: 6 PRs reviewed, Calendar/FileUpload rejected, ImageMap/PageService approved, ASCX/Snippets shelved. M2–M3 shipped (50/53 controls, 797 tests). Chart.js for Chart. DataBoundStyledComponent<T> recommended. Key patterns: Enums/ with int values, On-prefix events, feature branches→upstream/dev, ComponentCatalog.cs. Deployment: Docker+NBGV, dual NuGet, Azure webhook. M7–M14 milestone plans. HTML audit: 3 tiers, M11–M13. M15 fidelity: 132→131 divergences, 5 fixable bugs. Data controls: 90%+ sample parity, 4 remaining bugs. M17 AJAX: 6 controls shipped.

## Learnings

<!-- Summarized 2026-03-02 by Scribe -- covers M17 gate review through Themes roadmap -->

<!-- ⚠ Summarized 2026-03-06 by Scribe — older entries archived -->

### Archived Sessions

- M17-M18 Audit & Themes Roadmap Summary (2026-02-28 through 2026-03-01)
- Build/Release & M22 Migration Summary (2026-03-02)
- CSS Fidelity & WingtipToys Schedule Summary (2026-03-02 through 2026-03-03)
- Migration Toolkit Design & Restructure Summary (2026-03-03)
- Run 4-5 Review & BWFC Capabilities Analysis (2026-03-04 through 2026-03-05)

<!-- ⚠ Summarized 2026-03-06 by Scribe — Run 5→6 and Page Architecture entries archived -->

### Archived Sessions (cont.)

- Run 5→6 Analysis & Run 6 Benchmark (2026-03-04 through 2026-03-05)
- Page Base Class Architecture Analysis (2026-03-05)
- Page Consolidation Analysis (2026-03-05)

### Run 5→6 + Page Architecture Summary (2026-03-04 through 2026-03-05)

Run 5→6: 8 enhancements identified, top 4 implemented (TFM net10.0, SelectMethod BWFC TODO, wwwroot copy, compilable stubs). Run 6: 32 files → clean build in ~4.5 min (55% reduction). Bugs found: @rendermode in _Imports invalid, Test-UnconvertiblePage misses .aspx.cs. EF Core 10.0.3 mandated. @rendermode belongs in App.razor only.

WebFormsPageBase: Option C chosen — `WebFormsPageBase : ComponentBase` with `Page => this` self-reference, Title/MetaDescription/MetaKeywords delegating to IPageService, `IsPostBack => false`. Eliminates 27 @inject lines, 12+ manual fixes for WingtipToys. Deliberately omits Request/Response/Session.

Page Consolidation: Option B — merged Page.razor head rendering into WebFormsPage. `<PageTitle>`/`<HeadContent>` work anywhere in render tree. Min setup: `@inherits WebFormsPageBase` + `<WebFormsPage>@Body</WebFormsPage>`. WebFormsPageBase must NOT inherit NamingContainer (breaks tests, adds overhead). Page.razor remains standalone.

� Team update (2026-03-05): WebFormsPage now includes IPageService head rendering (title + meta tags), merging Page.razor capability per Option B consolidation. Layout simplified to single <WebFormsPage> component. Page.razor remains standalone.  decided by Forge, implemented by Cyclops


 Team update (2026-03-06): CRITICAL  Git workflow: feature branches from dev, PRs target dev. NEVER push to or merge into upstream main (production releases only).  directed by Jeff Fritz

<!-- ⚠ Summarized 2026-03-07 by Scribe — entries from 2026-03-06 archived -->

- Full Library Audit (2026-03-06)
- Run 8 Post-Mortem & Run 9 Preparation (2026-03-06)
- Run 9 CSS/Image Failure RCA (2026-03-06)
- Fix 1a + Fix 1b Implementation — Run 9 RCA Remediation (2026-03-06)

### Summary (2026-03-06)

Library audit: 153 Razor components + 197 C# classes (CONTROL-COVERAGE.md was listing 58 — corrected). ContentPlaceHolder reclassified from "Not Supported" to Infrastructure. Run 8 post-mortem: 22 fixes identified (3 P0, 11 P1, 8 P2); HTTP Session + Interactive Server is #1 blocker (HttpContext null during WebSocket). Run 9 CSS/image RCA: 3 root causes — (1) script doesn't extract `<webopt:bundlereference>`, (2) Layer 2 rewrote image paths without moving files, (3) tests don't verify visual output. Fix 1a: `<webopt:bundlereference>` extraction + CDN link preservation in ConvertFrom-MasterPage. Fix 1b: new `Invoke-CssAutoDetection` function scans wwwroot/Content/ for .css files and injects `<link>` tags into App.razor.


 Team update (2026-03-07): Coordinator must not perform domain work  all code changes must route through specialist agents  decided by Jeffrey T. Fritz, Beast
 Team update (2026-03-07): Run 11 script fixes: Invoke-ScriptAutoDetection and Convert-TemplatePlaceholders added to bwfc-migrate.ps1  decided by Cyclops
 Team update (2026-03-07): migration-standards SKILL.md updated with 3 new sections for Run 11 gaps  decided by Beast
 Team update (2026-03-07): Migration order directive  fresh Blazor project first, then apply BWFC  decided by Jeffrey T. Fritz

 Team update (2026-03-08): Default to SSR (Static Server Rendering) with per-component InteractiveServer opt-in; eliminates HttpContext/cookie/session problems  decided by Forge

 Team update (2026-03-08): Run 12 migration patterns: auth via plain HTML forms with data-enhance=false, dual DbContext, LoginView _userName from cascading auth state  decided by Cyclops

 Team update (2026-03-08): Enhanced navigation must be bypassed for minimal API endpoints  `data-enhance-nav="false"` required (consolidated decision)  decided by Cyclops
 Team update (2026-03-08): DbContext registration simplified  `AddDbContextFactory` only, no dual registration (supersedes Run 12 dual pattern)  decided by Cyclops
 Team update (2026-03-08): Middleware order: UseAuthentication  UseAuthorization  UseAntiforgery  decided by Cyclops
 Team update (2026-03-08): Logout must use `<a>` link not `<button>` in navbar  decided by Cyclops

### Comprehensive Component Audit (2026-03-08)

**Audit report:** `dev-docs/component-audit-2026-03-08.md`

Key findings from post-Run 12/13 audit:

1. **Library completeness:** 153 Razor components, 54 enums, 52 fully implemented Web Forms controls + 3 stubs + 2 deferred = 96% coverage of feasible controls. No new controls need to be built.

2. **HTML fidelity gaps:** Only 1/132 audit variants achieves exact HTML match. The #1 pattern is missing `id` attributes (~30+ controls). 5 controls have structural divergences that break CSS/JS: BulletedList (`<ol>`→`<ul>`), Panel (missing `<fieldset>`), ListView (DOM restructuring), Calendar (missing IDs), Label (`<span>`/`<label>` inconsistency).

3. **Migration script convergence:** Run 11 (8+ fixes) → Run 12 (6) → Run 13 (3). SSR architecture was transformative. Three remaining gaps: `data-enhance-nav="false"` for API links, `readonly` removal on cart inputs, logout form→link conversion. All tractable — Run 14 should hit 0.

4. **Documentation is strong** at primary control level (59 mkdocs.yml pages) but weak for infrastructure: field columns (BoundField, etc.), 63 style sub-components, and infrastructure components (ContentPlaceHolder, NamingContainer) have no standalone docs.

5. **Top 5 priorities:** (1) Fix 3 migration script gaps, (2) Fix BulletedList `<ol>` rendering, (3) Add `id` rendering to key controls, (4) Document field columns, (5) Fix Panel GroupingText `<fieldset>` rendering.

 Team update (2026-03-08): Layer 2 bulk-extract from known-good commit is default approach until Layer 1 output changes structurally  decided by Cyclops

 Team update (2026-03-08): Documentation refreshed  Runs 8-13 summaries, SSR-as-default guidance, package pinning, component coverage gap analysis  decided by Beast

### Audit Refresh (2026-03-08)

**Report:** `dev-docs/component-audit-2026-03-08-refresh.md`

Key changes since baseline audit:

1. **Substitution is now implemented** — deferred controls reduced from 2 to 1 (only Xml remains). Coverage: 98.2% of feasible controls.
2. **ID attribute rendering** added to 9 components (BulletedList, Button, Calendar, CheckBox, FileUpload, Label, LinkButton, Panel, TextBox). ~20+ data controls still need it.
3. **BulletedList `<ol>` and Panel `<fieldset>/<legend>`** fidelity issues confirmed resolved. Structural divergences reduced from 5 to 3 (ListView, Calendar, Label).
4. **Field column docs** (FieldColumns.md) and **ID rendering docs** (IDRendering.md) added — closes top documentation gaps from baseline.
5. **Migration script at 0 Layer 1 manual fixes** for 4 consecutive runs (12–15). Layer 2 has 3 stable semantic fixes.
6. **RouteData script bug discovered** in Run 15 — `[Parameter] // TODO` comment absorbs closing parenthesis, causing build failures. Fix priority: P1.
7. **CONTROL-COVERAGE.md is accurate** — verified against actual BWFC inventory. No discrepancies.
8. **Style sub-component count** is 66 (was reported as 63 in baseline — discrepancy was in the audit, not the code). CONTROL-COVERAGE.md correct at 66.
9. **AfterWingtipToys uses only 4 BWFC components directly** (Label, Panel, ListView, LoginView) — migration script converts most simple controls to native HTML.


 Team update (2026-03-09): Layer 2 script created (bwfc-migrate-layer2.ps1)  separate from Layer 1, 3 semantic transforms, idempotent via marker. Route generation fixed (). Layer 2 Pattern A not yet production-quality (known-good overlay still needed)  decided by Cyclops
