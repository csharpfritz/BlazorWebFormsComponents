# Project Context

- **Owner:** Jeffrey T. Fritz
- **Project:** BlazorWebFormsComponents  Blazor components emulating ASP.NET Web Forms controls for migration
- **Stack:** C#, Blazor, .NET, ASP.NET Web Forms, bUnit, xUnit, MkDocs, Playwright
- **Created:** 2026-02-10

## Learnings

<!--  Summarized 2026-02-27 by Scribe  covers M1M16 -->

<!-- ⚠ Summarized 2026-03-06 by Scribe — older entries archived -->

### Archived Sessions

- Core Context (2026-02-10 through 2026-02-27)
- M17-M20 Wave 1 Context (2026-02-27 through 2026-03-01)
- M20 Theming through Migration Benchmarks (2026-03-01 through 2026-03-04)
- Script & Toolkit Summary (2026-03-02 through 2026-03-04)
- GetRouteUrl, Run 5 & Toolkit Sync Summary (2026-03-04 through 2026-03-05)

<!-- ⚠ Summarized 2026-03-07 by Scribe — entries from 2026-03-05 through 2026-03-07 (pre-Run 11) archived -->

- Run 6 Script Enhancements (2026-03-05)
- @rendermode Scaffold Fix (2026-03-05)
- WebFormsPageBase Implementation (2026-03-05)
- WebFormsPage IPageService Consolidation (2026-03-05)
- LoginView Migration Script Fix (2026-03-06)
- Run 9 Script Fixes — 9 RF items (2026-03-06)
- Layer 2 AfterWingtipToys Build Conversion (2026-03-06)

### Summary (2026-03-05 through 2026-03-07 pre-Run 11)

Run 6: 4 script enhancements (TFM, SelectMethod TODO, wwwroot copy, stubs). @rendermode fix: removed standalone directive from _Imports.razor scaffold. WebFormsPageBase: `ComponentBase` subclass with `Page => this`. WebFormsPage consolidation: merged Page.razor head rendering via Option B. LoginView script fix: `<asp:LoginView>` to `<LoginView>` (not AuthorizeView). Run 9: 9 script fixes (Models copy, DbContext transform, EF6 to EF Core, redirect detection, Program.cs boilerplate, Page Title extraction, QueryString/RouteData annotations, ListView GroupItemCount, csproj packages). Layer 2: full AfterWingtipToys conversion. Auth pages use plain HTML forms with HTTP endpoints.

<!-- Summarized 2026-03-09 by Scribe  entries from 2026-03-07 through 2026-03-08 (Runs 11-15) archived -->

- Run 11  Complete WingtipToys Migration from Scratch (2026-03-07)
- Run 11 Script Fixes  Invoke-ScriptAutoDetection + Convert-TemplatePlaceholders (2026-03-07)
- Run 12  Full Migration with Layer 2 Manual Fixes (2026-03-07)
- LoginView Namespace Fix (2026-03-07)
- Run 13  Full Pipeline 25/25 Tests (2026-03-08)
- Run 13 Fixes Baked Into Migration Script  4 new functions (2026-03-08)
- Run 15  Layer 2 Bulk-Apply from cef51da3 Reference (2026-03-08)

### Summary (2026-03-07 through 2026-03-08, Runs 11-15)

**Run 11:** First fresh WingtipToys migration from scratch (no FreshWingtipToys reference). 105 files, 0 errors. Key patterns: root-level `_Imports.razor` needed for pages outside `Components/`; code-behind partials must NOT specify `: ComponentBase` with `@inherits WebFormsPageBase`; auth pages use plain HTML forms to HTTP endpoints; CartStateService with cookie-based cart ID; image paths preserved from source. Script fixes added `Invoke-ScriptAutoDetection` (JS copy + `<script>` injection with dependency ordering) and `Convert-TemplatePlaceholders` (Placeholder elements to `@context`).

**Run 12:** Full migration with Layer 2 manual fixes  16 categories of changes (csproj, _Imports, Models, Program.cs, MainLayout, all pages). Established dual DbContext registration (later simplified). LoginView namespace fix: added `@using BlazorWebFormsComponents.LoginControls` to both sample and script template.

**Run 13:** Full pipeline  25/25 tests, 0 errors, ~22 min total. Confirmed: SSR default works (no `@rendermode` on HeadOutlet/Routes), `data-enhance-nav="false"` required on minimal API links, `data-enhance="false"` on auth forms, logout must use `<a>` not `<button>`, `AddDbContextFactory` only (no dual registration), middleware order UseAuthentication then UseAuthorization then UseAntiforgery. Baked all 3 manual fixes into script: `Add-EnhancedNavDisable`, `Add-ReadOnlyWarning`, `ConvertFrom-LoginStatus`, `Convert-LogoutFormToLink`.

**Run 15:** Bulk-applied Layer 2 from cef51da3 reference commit  68 files, 25/25 tests, 3.1 min. Confirmed recurring Layer 1 issues: `[Parameter] // TODO:` swallows signatures, System.Web.UI.Page stubs need full rewrite, FormView doesn't work in SSR. Efficiency insight: bulk-extract from known-good commit when Layer 2 fixes are stable.

Team updates received: Coordinator domain work ban, FreshWingtipToys ban, migration-standards SKILL update, migration order directive, SSR default, enhanced nav bypass, DbContext simplification, middleware order, logout link pattern, audit priorities, docs refresh.


### Layer 2 Automation Script + Script Fixes (2026-03-09)

**Task 1: Fixed [Parameter] // TODO comment bug (bwfc-migrate.ps1 line 1240)**
The `[RouteData]` → `[Parameter]` conversion put a `// TODO` comment on the same line as `[Parameter]`, which swallowed everything after it when used on method parameters. Fix: moved TODO to a separate line above `[Parameter]` using `\n` in the replacement string.

**Task 3: Fixed route generation for subdirectory pages (bwfc-migrate.ps1 line 621)**
`ConvertFrom-PageDirective` used `$FileName` (just the filename) for route generation, producing `/Login` instead of `/Account/Login`. Fix: switched to `$RelPath` with backslash-to-forward-slash conversion, matching the pattern already used in `New-CompilableStub`. Also handles Default/Index at any directory depth.

**Task 2: Created bwfc-migrate-layer2.ps1 — Layer 2 automation**
New script applying 3 persistent semantic transforms stable across Runs 12–15:
- **Pattern A:** Detects Layer 1 code-behinds with FormView/SelectMethod/Page_Load patterns → rewrites to `ComponentBase` with `IDbContextFactory<T>` injection, `[SupplyParameterFromQuery]`, and `OnInitializedAsync`
- **Pattern B:** Detects auth .razor files with SignInManager/UserManager → generates simplified forms with individual `[SupplyParameterFromForm]` string properties, `<AntiforgeryToken />`, and `@formname`
- **Pattern C:** Detects presence of .razor files → generates .NET 9 SSR `Program.cs` with configurable DbProvider (SQLite default), Identity support auto-detected from `IdentityDbContext`, seed data auto-detected from initializer classes

Key design decisions:
- Idempotency via `// Layer2-transformed` marker — checked before every write
- SupportsShouldProcess for `-WhatIf` dry-run
- Auto-detects DbContext name, namespace, and Identity context from project files
- Transform log exported to `layer2-transforms.log`

### Run 16 — Layer 2 Script Bug Fixes + WingtipToys Migration (2026-03-09)

**Completed:** Fixed 2 bugs in bwfc-migrate-layer2.ps1, re-ran full pipeline. **25/25 acceptance tests passed.**

**Bug 1 fix: `$listField` uninitialized in single-item code path**
In Pattern A code-behind generation, `$listField` was only assigned inside the `else` branch (when `$isSingleItem` is false). The `OnInitializedAsync` else-branch at the end used `$listField` unconditionally. With `Set-StrictMode -Version Latest`, this caused a terminating error for single-item pages falling through to the default query path. Fix: initialized `$listField = '_items'` before the if/else block.

**Bug 2 fix: `-TestMode` output redirect removed**
The `-TestMode` switch redirected all output to a `Layer2Output/` subdirectory instead of modifying in-place. This was inconsistent with Layer 1's `-TestMode` (which means "use ProjectReference instead of PackageReference"). Removed: parameter declaration, `Get-OutputPath` redirect logic, Layer2Output directory creation, log path redirect, and code-behind removal guards that checked `-not $TestMode`.

**Layer 2 script capabilities assessment:**
- ✅ Pattern A correctly applies to 26 code-behind files (all detected and rewritten)
- ✅ Pattern C generates a functional Program.cs with correct SQLite/Identity/Seed config
- ❌ Pattern A generates broken parameter declarations — `public or private { get; set; }` for route parameters parsed from comment annotations
- ❌ Pattern A cannot distinguish data-bound pages (ProductList, ProductDetails) from simple info pages (About, Contact) — over-applies DI/query logic to all code-behinds
- ❌ Pattern A uses `object` as entity type when no SelectMethod or entity hint is found
- ❌ Pattern B detected 0 candidates — auth pages don't match the detection heuristic after Layer 1 transforms
- ❌ Generated code-behinds don't build without manual fixes (100% of Pattern A output had CS1585 errors)

**Conclusion:** Layer 2 automation handles scaffolding structure (namespaces, class declarations, DI injection pattern) but ALL generated code-behinds required replacement with known-good versions from cef51da3. The script is useful as a starting template but not yet production-quality for generating compilable output.

**Pipeline timing:** Layer 1: ~2.5s, Layer 2: ~1s, known-good overlay: ~1s, build: ~2s, tests: ~23s. Total: ~45s.

**Files changed:** migration-toolkit/scripts/bwfc-migrate-layer2.ps1 (bug fixes), samples/AfterWingtipToys/ (full migration output)


 Team update (2026-03-09): Audit consolidated  RouteData bug fixed (P1 resolved), ID rendering gap flagged for data controls, Layer 2 automation feasibility confirmed  decided by Forge

 Team update (2026-03-08): Three P0 HTML fidelity fixes identified  CheckBox needs span wrapper, BaseValidator needs id/class, FormView needs class on table. Audit scored 87%.  decided by Forge

### P0 HTML Output Fidelity Fixes (2026-03-09)

**Fixed 3 P0 structural divergences from Forge's HTML output audit.**

**Bug 1: CheckBox missing wrapper `<span>`** — Added `<span class="@CssClass" style="@Style" title="@ToolTip">` around `<input>` + `<label>` when Text is present, matching RadioButton's pattern. No-text path unchanged (styles stay on input directly). CssClass/Style/ToolTip moved from `<input>` to wrapper `<span>`.

**Bug 2: BaseValidator `<span>` missing `id` and `class`** — Added `id="@ClientID"` and `class="@CssClass"` to the validator `<span>` element in `BaseValidator.razor`. All 5 validators (RequiredField, RegularExpression, Compare, Range, Custom) inherit this fix.

**Bug 3: FormView `<table>` missing `class="@CssClass"`** — Added `class="@CssClass"` to the outer `<table>` element, matching GridView/DetailsView pattern.

**Tests updated:** 4 test files (NoSpanWrapper.razor, TextAlign.razor, Style.razor, ToolTipTests.razor) updated to match corrected HTML. 1488 tests pass.

**Key learning:** When CheckBox and RadioButton are in the same family, their HTML structure should match. RadioButton already had the span wrapper — CheckBox was the inconsistency.


 Team update (2026-03-08): Second sample project will be purpose-built 'EventManager' Control Gallery targeting ~12-15 pages with controls WingtipToys doesn't cover  decided by Forge
 Team update (2026-03-08): ContosoUniversity acceptance test patterns  partial ID selectors, cascading fallbacks, CONTOSO_BASE_URL env var  decided by Rogue

 Team update (2026-03-08): ContosoUniversity local setup  LocalDB connection strings, AjaxControlToolkit HintPath fix, NBGV block — decided by Colossus

📌 Team update (2026-03-08): ContosoUniversity Run 01 review — GridView missing PageIndexChanging event (library gap, HIGH priority), SelectMethod should be preserved by migration scripts, Pattern A entity detection hardcoded to WingtipToys (CRITICAL) — decided by Forge
