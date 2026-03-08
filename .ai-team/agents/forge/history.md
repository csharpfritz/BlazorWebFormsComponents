# Project Context

- **Owner:** Jeffrey T. Fritz
- **Project:** BlazorWebFormsComponents  Blazor components emulating ASP.NET Web Forms controls for migration
- **Stack:** C#, Blazor, .NET, ASP.NET Web Forms, bUnit, xUnit, MkDocs, Playwright
- **Created:** 2026-02-10

## Core Context

<!-- Summarized 2026-03-08 by Scribe — originals in history-archive.md -->

M1–M16: 6 PRs reviewed, Calendar/FileUpload rejected, ImageMap/PageService approved, ASCX/Snippets shelved. M2–M3 shipped (50/53 controls, 797 tests). Chart.js for Chart. DataBoundStyledComponent<T> recommended. Key patterns: Enums/ with int values, On-prefix events, feature branches→upstream/dev, ComponentCatalog.cs. Deployment: Docker+NBGV, dual NuGet, Azure webhook. M7–M14 milestone plans. HTML audit: 3 tiers, M11–M13. M15 fidelity: 132→131 divergences, 5 fixable bugs. Data controls: 90%+ sample parity, 4 remaining bugs. M17 AJAX: 6 controls shipped. Library: 153 Razor components + 197 C# classes. WebFormsPageBase (Option C) + WebFormsPage (Option B consolidation) — `@inherits WebFormsPageBase` + `<WebFormsPage>@Body</WebFormsPage>`. @rendermode in App.razor only. EF Core 10.0.3. CRITICAL: feature branches from dev, PRs target dev — NEVER push to upstream main. Run 8 post-mortem: HttpContext null under Interactive Server is #1 blocker → SSR chosen. CSS auto-detection: `Invoke-CssAutoDetection` + `<webopt:bundlereference>` extraction.

## Learnings


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

### Migration Toolkit Skill Validation (2026-03-09)

**Task:** Validated all skills in `migration-toolkit/skills/` and `.ai-team/skills/` against Runs 14–16 findings.

**Changes made:**

1. **migration-standards (distributable + team):** Updated render mode from "Global Server Interactive" to "SSR with per-component InteractiveServer opt-in" — the InteractiveServer guidance was stale since Run 12. Updated run count from 5 to 9. Added 2-script pipeline section documenting `bwfc-migrate-layer2.ps1` and its three patterns (A: code-behinds, B: auth forms, C: Program.cs). Added `-TestMode` switch docs, RelPath route generation, and `[Parameter]` TODO fix. Bumped confidence from "medium" to "high" based on 5 consecutive 100% passes.

2. **bwfc-migration:** Updated component count to show 9 categories and 153 total. Added SSR recommendation note. Updated render mode installation section. Added Layer 2 script reference to Migration Workflow. Added RelPath route generation and `[Parameter]` separate-line TODO note. Added deferred Xml control note.

3. **bwfc-data-migration:** Added SSR context note before the InteractiveServer HttpContext warning — under SSR most session issues are avoided.

4. **bwfc-identity-migration:** Added SSR context note before the cookie auth warning. Added `UseAntiforgery()` to middleware order.

5. **CONTROL-COVERAGE.md:** Added Xml as deferred control in Editor Controls table. Updated Coverage Summary with migration pipeline status and run data. Cross-referenced against fresh audit (`dev-docs/component-audit-2026-03-08-refresh.md`) — no discrepancies.

6. **CHECKLIST.md:** Added 2-script pipeline intro. Updated Layer 1 section with RelPath, `[Parameter]`, enhanced nav items. Updated Layer 2 section to reference `bwfc-migrate-layer2.ps1`.

**Key finding:** The single biggest stale issue was render mode guidance — all skills still said "Global Server Interactive" while the team switched to SSR in Run 12 (6 runs ago). This would have caused new migration users to hit the HttpContext/cookie issues that SSR was specifically chosen to avoid.

### HTML Output Fidelity Audit (2026-03-08)

**Report:** `dev-docs/html-output-audit-2026-03-08.md`

Comprehensive HTML output fidelity audit of all 58 primary BWFC components. Key findings:

1. **Overall score: 87%** — 49 of 58 components produce correct or near-correct HTML matching Web Forms output.
2. **3 P0 structural bugs:** (a) CheckBox missing wrapper `<span>` (RadioButton has it — inconsistent), (b) BaseValidator `<span>` missing `id` and `class` attributes (affects all 5 validators), (c) FormView `<table>` missing `class="@CssClass"`.
3. **4 P1 missing IDs:** LoginName, AdRotator, ValidationSummary, ModelErrorMessage — all trivial fixes.
4. **1 P2 DataPager:** Uses modern `<div>` instead of Web Forms `<table>` pager — low priority.
5. **Previous audit issues resolved:** BulletedList `<ol>`, Panel `<fieldset>`, Label dual-render, ListView DOM, Calendar IDs — all confirmed fixed.
6. **Test coverage excellent:** 1488 tests passing, 425 test .razor files, all major components have Find/FindAll HTML assertions.
7. **ID rendering now on 46/51 auditable components** (90.2%) — massive improvement from ~0 at M11.

Key learning: CheckBox and RadioButton should share the same wrapper pattern — both need `<span>` wrapper for CssClass/Style targeting. The inconsistency is the single most impactful fidelity bug remaining.

 Team update (2026-03-08): Migration-test reports use standalone `{project-name}-runNN.md` files (zero-padded). Old run folders are read-only archives.  decided by Beast

### Research: Second Sample Project & ASPX URL Rewriting (2026-03-09)

**Report:** `dev-docs/research-second-sample-and-url-rewriting.md`

Two research items completed for Jeff:

1. **Second sample project:** Evaluated 5 candidates for a Web Forms sample app that exercises controls WingtipToys doesn't (TreeView, Menu, Wizard, Calendar, DataList, Repeater, etc.). The open-source Web Forms sample landscape is barren — no suitable existing app was found. Recommended building a purpose-built "EventManager" Control Gallery (~12-15 pages, LocalDB, Event Management domain) at `samples/EventManager/`.

2. **ASPX URL rewriting:** Evaluated 5 approaches for preserving `.aspx` URLs during migration (RewriteOptions, custom middleware, IRule, @page directive, catch-all route). Recommended `RewriteOptions.AddRedirect` with 301 status as a documented snippet in migration-toolkit, not as a NuGet package in BWFC. Query strings are automatically preserved. Default.aspx → / requires a special-case rule.

Key learning: The Web Forms sample ecosystem is effectively dead — WingtipToys is the only maintained Microsoft sample. Any second sample will need to be purpose-built.
Key learning: `Microsoft.AspNetCore.Rewrite` regex rules operate on path only, not query string — this means query string preservation is automatic and free.


 Team update (2026-03-08): P0 HTML fidelity fixes complete  CheckBox span wrapper, BaseValidator id/class, FormView CssClass. 1488 tests pass.  decided by Cyclops, Forge