# Project Context

- **Owner:** Jeffrey T. Fritz
- **Project:** BlazorWebFormsComponents — Blazor components emulating ASP.NET Web Forms controls for migration
- **Stack:** C#, Blazor, .NET, ASP.NET Web Forms, bUnit, xUnit, MkDocs, Playwright
- **Created:** 2026-02-10

## Core Context

<!-- Summarized 2026-03-08 by Scribe — originals in history-archive.md -->

Documentation & migration reporting agent. M1–M16 docs shipped (all control categories). Doc structure: title → intro → Features → NOT Supported → syntax → HTML Output → Migration Notes → Examples. mkdocs.yml nav alphabetical within categories. Migration reports: standalone `{project-name}-runNN.md` format, zero-padded. Executive report pattern: blockquote bottom line → timeline → screenshots → before/after code. Skills files in `.ai-team/skills/` and `migration-toolkit/skills/` must be updated together. LoginView is native BWFC — never replace with AuthorizeView. Functional tests passing ≠ migration success — visual regression is ship-blocking. Run 9 RCA rules: Static Asset Path Preservation + CSS Reference Verification.

## Learnings

### Run 10 Failure Report (2026-03-07)

- **Report location:** `dev-docs/migration-tests/wingtiptoys-run10-2026-03-07/REPORT.md`
- **Status:** ❌ FAILED — Coordinator Process Violation
- **Branch:** `squad/run8-improvements` (same as Run 9)
- **What happened:** Phases 1 and 2 completed successfully (Layer 1 in 4.6s, Layer 2 in ~15 min with 0 build errors). The Coordinator then violated Squad protocol by hand-editing Razor files, installing `npm playwright`, creating throwaway Node.js scripts, using wrong .NET SDK (10.0.200-preview vs 10.0.100), and not setting `ASPNETCORE_ENVIRONMENT=Development`. ~30 minutes wasted on ad-hoc debugging.
- **Test results:** 20/25 passed (14 functional + 11 visual integrity). 5 failed: 3 cart timeouts, 1 auth E2E, 1 blazor.web.js 500.
- **Root causes:** RC-1 Coordinator did domain work instead of routing to agents; RC-2 Layer 2 output had null collections and missing ItemType params; RC-3 Environment not configured for Development mode.
- **Key learning:** The Squad system works when agents do domain work and the Coordinator coordinates. When the Coordinator performs domain work directly, quality controls are bypassed and time is wasted. Process discipline is the #1 priority for Run 11.
- **Pattern across runs:** Run 8 ✅, Run 9 ❌ (visual regression), Run 10 ❌ (process violation). Automated pipeline (Layers 1-2) is improving each run; Phase 3 execution remains the weak link.
- **Decision filed:** `.ai-team/decisions/inbox/beast-run10-failure.md` — Coordinator must never hand-edit application source files.

 Team update (2026-03-07): Run 10 declared FAILED  coordinator violated protocol. Phase 1 skill updates applied correctly. Key issues for next run: missing ItemType param, null Products list, ASPNETCORE_ENVIRONMENT=Development required.  decided by Jeffrey T. Fritz

### Run 11 Skill Fixes — migration-standards SKILL.md (2026-03-07)

- **Scope:** 3 new sections added to `.ai-team/skills/migration-standards/SKILL.md` based on Run 11 WingtipToys benchmark failures.
- **Fix 3 — Static Asset Migration Checklist:** Added comprehensive table of ALL common folders (Content/, Scripts/, Images/, Catalog/, fonts/, favicon.ico) that must be copied to `wwwroot/`. Includes verification checklist for CSS, JS, images, logos, fonts. Documents the common miss: `Scripts/` folder is easy to forget because CSS breakage is more visually obvious.
- **Fix 4 — ListView Template Placeholder Conversion:** Added full conversion guide for LayoutTemplate/GroupTemplate placeholder elements → `@context`. This was the #1 failure cause in Run 11 (5 of 8 test failures). Documents the Web Forms placeholder pattern (`<tr id="groupPlaceholder">`), the Blazor `RenderFragment<RenderFragment>` equivalent, migration rule table, with/without GroupItemCount examples, and diagnostic tip.
- **Fix 5 — Preserving Action Links in Detail Pages:** Added section on verifying action links (Add to Cart, Edit, Delete) survive Layer 1 conversion. Documents the `@context.PropertyName` pattern for data-bound link values.
- **Why needed:** Run 11 exposed gaps where the migration script converts template structure but doesn't handle placeholder-to-@context conversion, and static asset copying was incomplete (Scripts/ folder missed).


 Team update (2026-03-07): Coordinator must not perform domain work  all code changes must route through specialist agents  decided by Jeffrey T. Fritz, Beast
 Team update (2026-03-07): Run 11 script fixes: Invoke-ScriptAutoDetection (JS files) and Convert-TemplatePlaceholders (placeholder@context) added to bwfc-migrate.ps1  decided by Cyclops
 Team update (2026-03-07): Run 11 migration decisions: root-level _Imports.razor required, partial class base class conflict pattern, auth endpoint pattern  decided by Cyclops

 Team update (2026-03-08): Default to SSR (Static Server Rendering) with per-component InteractiveServer opt-in; eliminates HttpContext/cookie/session problems  decided by Forge

 Team update (2026-03-08): @using BlazorWebFormsComponents.LoginControls must be in every generated _Imports.razor  decided by Cyclops

 Team update (2026-03-08): Run 12 migration patterns: auth via plain HTML forms with data-enhance=false, dual DbContext, LoginView _userName from cascading auth state  decided by Cyclops

### Documentation Refresh — Runs 8–13 (2026-03-08)

- **AutomatedMigration.md updated:** Added SSR-as-default section, package version pinning, enhanced navigation (`data-enhance-nav="false"`) guidance, updated pipeline table (Script layer from ~40% to ~60%), added pipeline convergence note (56% → 100% across 13 runs), expanded transform table with JS/CSS detection, placeholder conversion, and static asset copying.
- **6 new run summary pages created:** `docs/migration-tests/wingtiptoys-run{8-13}.md` — each with date, score, render mode, key changes, remaining fixes, and link to full report in dev-docs.
- **migration-tests/README.md rewritten:** Full run history table with all 10 published runs (1-4, 8-13), plus convergence summary section.
- **mkdocs.yml updated:** Added all 6 new run pages, Runs 5-6 (previously missing), and component-coverage.md to the Migration Tests nav section.
- **component-coverage.md created:** Gap analysis of 52 components vs docs. Result: 100% coverage — no gaps found. Sub-components (View, RoleGroup, MenuItem, TreeNode, etc.) are documented within parent pages.
- **Patterns followed:** Run summary pages use consistent table format (metric/value), root cause abbreviations where applicable, and link to full dev-docs report. Followed existing migration-tests page style from Runs 1-4.
- **Key decisions documented:** SSR default render mode, package version pinning, `data-enhance-nav="false"` for non-Blazor endpoints.

### Run 14 Migration Report (2026-03-08)

- **Report location:** `docs/migration-tests/wingtiptoys-run14.md`
- **Score:** ✅ 25/25 (third consecutive 100%)
- **Key narrative:** Layer 1 (script) reached zero manual intervention — all 3 Run 13 fixes baked in. Layer 2 still has 3 semantic code-behind fixes that can't be regex-automated.
- **New elements documented:** `-TestMode` switch, 8-component `id` rendering fix, component audit (153 components, 96% coverage), automation ceiling concept (Layer 1 mechanical vs Layer 2 semantic).
- **Format followed:** Matched Run 13 structure — metric table, executive blockquote, what's new, execution details, progression table, lessons learned. Added "Key Insight: The Automation Ceiling" section to capture the Layer 1/Layer 2 boundary discovery.
- **mkdocs.yml updated:** Added Run 14 nav entry between Run 13 and Component Coverage.

### Run 15 Migration Report (2026-03-08)

- Run 15 report written to dev-docs/migration-tests/ (not docs/ — internal dev docs)

 Team update (2026-03-08): Layer 2 bulk-extract from known-good commit is default approach until Layer 1 output changes structurally  decided by Cyclops

 Team update (2026-03-08): Migration script gained 4 new functions (EnhancedNavDisable, ReadOnlyWarning, LoginStatus conversion, LogoutFormToLink)  targets 0 manual fixes  decided by Cyclops

 Team update (2026-03-08): Component audit priorities  BulletedList/Panel/id-rendering fixes, field column docs, zero-touch migration script  decided by Forge

### Run 16 Migration Report & Toolkit Docs (2026-03-08)

- **Run 16 report:** `dev-docs/migration-tests/wingtiptoys-run16.md` — fifth consecutive 100% (25/25). First Layer 2 automation attempt. Layer 1 at 2.50s (12% faster). Layer 2 script handles Pattern C (Program.cs) fully, Pattern A (code-behinds) scaffolding, Pattern B (auth) not yet detected.
- **migration-toolkit/README.md:** Updated "Latest" banner to Run 16, added `bwfc-migrate-layer2.ps1` to scripts table, updated pipeline table to reflect "Script + Overlay" for Layer 2, rewrote "What's New" section for Run 16, updated Quick Overview to include Layer 2 step.
- **migration-toolkit/METHODOLOGY.md:** Updated pipeline diagram (COPILOT-ASSISTED → SCRIPT + OVERLAY), updated intelligence/tool table, rewrote Layer 2 section to document 2-script pipeline and 3 patterns (A/B/C), updated time estimates (Layer 1 now ~3s, Layer 2 now ~3 min with agents).
- **migration-toolkit/QUICKSTART.md:** Added Step 4 (Layer 2 script), renumbered Steps 5–10, added warning about Pattern A/B partial automation.
- **Key learning:** Layer 2 documentation must distinguish between "script-automated" and "needs overlay" — the pipeline is no longer binary (automated vs manual). Three-state model: fully automated (Pattern C), partially automated (Pattern A), not yet automated (Pattern B).


 Team update (2026-03-09): Run 16 complete  25/25 tests, Layer 2 script bugs fixed ( init, TestMode removed), known-good overlay still required. Audit consolidated with updated priorities  decided by Cyclops, Forge

### Migration-Tests Reorganization for Multi-Project Testing (2026-03-09)

- **Scope:** Rewrote `dev-docs/migration-tests/README.md` to support multiple test projects and include all 16 runs.
- **What changed:** Added Test Projects table (WingtipToys + placeholder for next project), full 16-run history table with L1 time/L1 manual fixes/L2 fixes/score/render mode, Pipeline Evolution section with convergence summary and key milestones, updated Key Conclusions (5 consecutive 100%, Layer 2 automation frontier), "Adding a New Test Project" guidance section, and Report Archive cataloging all 12 old run folders.
- **Naming convention established:** Standalone `{project-name}-runNN.md` is the canonical report format going forward. Old run folders preserved as historical archives.
- **Data added:** Runs 5, 6, 14, 15, 16 were missing from the README — all now included with metrics extracted from their respective report files. Run 7 confirmed non-existent (skipped).
- **Key learning:** The old run folders (runs 1–6, 8–13) contain valuable raw data (screenshots, build output, scan results) that the standalone reports don't capture. Worth keeping as archives but not worth migrating to standalone format.

 Team update (2026-03-08): Three P0 HTML fidelity fixes identified  CheckBox needs span wrapper, BaseValidator needs id/class, FormView needs class on table. Audit scored 87%.  decided by Forge


 Team update (2026-03-08): P0 HTML fidelity fixes complete  CheckBox span wrapper, BaseValidator id/class, FormView CssClass. 1488 tests pass.  decided by Cyclops, Forge
 Team update (2026-03-08): Second sample project will be purpose-built 'EventManager' Control Gallery targeting ~12-15 pages with controls WingtipToys doesn't cover  decided by Forge
 Team update (2026-03-08): ASPX URL rewriting goes in migration-toolkit docs (RewriteOptions.AddRedirect snippet), not BWFC NuGet  decided by Forge