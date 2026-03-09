# Migration Methodology: The Three-Layer Pipeline

**Why three layers, not one?** Because migration work falls into three fundamentally different categories — and trying to handle them all with one tool (or one person, or one AI session) is how migrations stall.

---

## Pipeline Overview

```
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│    Layer 1           │    │    Layer 2           │    │    Layer 3           │
│    AUTOMATED         │───▶│    SCRIPT + OVERLAY  │───▶│    ARCHITECTURE      │
│                      │    │                      │    │                      │
│  bwfc-migrate.ps1    │    │  bwfc-migrate-       │    │  Human + Copilot     │
│  ~40% of work        │    │  layer2.ps1 +        │    │  ~15% of work        │
│  ~3 seconds          │    │  manual overlay       │    │  ~8–12 hours         │
│  100% accuracy       │    │  ~45% of work        │    │  Requires judgment   │
└─────────────────────┘    │  ~3 minutes          │    └─────────────────────┘
         │                  │  High accuracy       │             │
    Mechanical              └─────────────────────┘        Semantic
    transforms                       │                     decisions
                                Structural
                                transforms
```

Each layer handles a different *kind* of work, not just a different *amount*. The boundary between layers is defined by what type of intelligence is required:

| Layer | Intelligence Required | Tool | Error Rate |
|---|---|---|---|
| Layer 1 | None — pure regex/pattern matching | PowerShell script (`bwfc-migrate.ps1`) | ~0% (deterministic) |
| Layer 2 | Pattern recognition — knows BWFC control mappings | PowerShell script (`bwfc-migrate-layer2.ps1`) + Copilot with migration skill + manual overlay | Low (guided by rules) |
| Layer 3 | Judgment — understands your app's architecture | Human + Copilot with data migration skill | Varies (depends on decisions) |

---

## Layer 0: Assessment (Before You Start)

Before migrating anything, scan your project to understand what you're working with.

### ⛔ MIGRATION BOUNDARIES — NEVER Violate

These rules apply across ALL layers. Violating them causes migration rejection:

1. **NEVER change database technology** — SQL Server LocalDB → LocalDB (NOT SQLite). Migrate the EF API, not the provider.
2. **NEVER replace asp: controls with raw HTML** — BWFC provides Blazor equivalents for all controls.
3. **NEVER use Blazor's `<PageTitle>`** — Use BWFC's `Page.Title` pattern (WebFormsPageBase + `<BlazorWebFormsComponents.Page />` in layout).
4. **NEVER rewrite `OnClick` to `@onclick`** — Preserve the attribute name exactly (BWFC components expose matching EventCallback parameters).
5. **NEVER add URL prefixes** — Routes match original URLs exactly (no `/AppName/` prefix).

**Tool:** [`scripts/bwfc-scan.ps1`](../scripts/bwfc-scan.ps1)

**Input:** Your Web Forms project directory
**Output:** A readiness report showing:
- File inventory (`.aspx`, `.ascx`, `.master` count)
- Control usage (which `asp:` controls, how many instances)
- DataSource controls (these need manual replacement)
- Migration readiness score (percentage of controls covered by BWFC)

**Example:**
```powershell
.\scripts\bwfc-scan.ps1 -Path .\MyWebFormsApp -OutputFormat Markdown -OutputFile scan-report.md
```

The scan report tells you whether BWFC is a good fit before you invest time in migration. If your app is heavy on DataSource controls, Wizard, or Web Parts, you'll know upfront.

---

## Layer 1: Automated Transforms

**Tool:** [`scripts/bwfc-migrate.ps1`](../scripts/bwfc-migrate.ps1)

Layer 1 handles every transform that can be expressed as a regex find-and-replace. These are mechanical, deterministic, and 100% accurate. No human judgment needed.

### What Layer 1 Does

| Transform | Count (WingtipToys PoC) | Accuracy |
|---|---|---|
| `asp:` tag prefix removals | 147+ | 100% |
| `runat="server"` attribute removals | 165+ | 100% |
| Expression conversions (`<%: %>` → `@()`) | ~35 | 100% |
| `ItemType` → `TItem` conversions | 8 | 100% |
| Content wrapper removals (`<asp:Content>`) | 28 | 100% |
| URL conversions (`~/` → `/`) | All | 100% |
| File renaming (`.aspx` → `.razor`) | 33 | 100% |
| Project scaffold (`.csproj`, `Program.cs`, `_Imports.razor`, `App.razor`) | Full | ✅ |

> **Note:** Counts above are from the WingtipToys PoC (33 pages, 230+ control instances). Your counts will vary by application size and complexity.

`_Imports.razor` includes `@inherits BlazorWebFormsComponents.WebFormsPageBase` so that all converted pages get `Page.Title`, `Page.MetaDescription`, `Page.MetaKeywords`, and `IsPostBack` without per-page injection. The layout scaffold includes `<BlazorWebFormsComponents.Page />` to render `<PageTitle>` and `<meta>` tags.

### What Layer 1 Does NOT Do

- Adapt `SelectMethod` signatures (script adds TODOs; developers must add 4 parameters to match the `SelectHandler<T>` delegate — see Layer 2)
- Wire `InsertMethod`, `UpdateMethod`, `DeleteMethod` (not yet supported in BWFC — requires manual conversion)
- Convert code-behind lifecycle methods (requires semantic understanding)
- Replace DataSource controls (requires architecture decisions)
- Wire authentication (requires knowing your auth strategy)
- Convert Master Pages to layouts (partially — removes directives but doesn't create `@Body`)

These are intentionally left for Layer 2 and Layer 3.

### Layer 1 Output

After Layer 1, pages fall into three readiness categories:

| Status | Typical % | Meaning |
|---|---|---|
| ✅ Markup-complete | ~12% | Ready to compile and run — no further work needed |
| ⚠️ Needs Layer 2 | ~64% | Structural transforms needed — Copilot handles these |
| ❌ Needs Layer 3 | ~24% | Architecture decisions required — human judgment needed |

> These percentages are from the [WingtipToys proof-of-concept](../planning-docs/WINGTIPTOYS-MIGRATION-EXECUTIVE-REPORT.md) and are representative, not absolute. Your mileage will vary based on how much DataSource/auth/session-state your app uses.

---

## Layer 2: Script-Assisted Semantic Transforms

**Tools:** [`scripts/bwfc-migrate-layer2.ps1`](../scripts/bwfc-migrate-layer2.ps1) + [Copilot migration skill](skills/bwfc-migration/SKILL.md) + manual overlay

Layer 2 handles transforms that require understanding control semantics — data binding patterns, code-behind lifecycle methods, auth form structures, and application bootstrap. As of Run 16, a **Layer 2 script** automates a portion of this work, with Copilot and manual overlay handling the remainder.

### The 2-Script Pipeline

Layer 2 now uses a dedicated automation script before falling back to Copilot-assisted work:

```
bwfc-migrate.ps1 (Layer 1)  →  bwfc-migrate-layer2.ps1  →  Manual Overlay  →  Copilot
         ~3 seconds                   ~2 seconds              ~3 minutes        as needed
```

### Layer 2 Script: Three Patterns

The `bwfc-migrate-layer2.ps1` script targets three semantic patterns:

| Pattern | What It Does | Current Status |
|---------|-------------|----------------|
| **Pattern C** — Program.cs | Generates full .NET SSR bootstrap with SQLite, Identity, seed data | ✅ Fully automated |
| **Pattern A** — Code-behinds | Scaffolds ComponentBase + DI code-behind files from page analysis | ⚠️ Structure correct, entity types need manual overlay |
| **Pattern B** — Auth forms | Detects Login/Register forms and simplifies `[SupplyParameterFromForm]` | ❌ Detection needs refinement — manual overlay still required |

Run the Layer 2 script after Layer 1:

```powershell
.\scripts\bwfc-migrate-layer2.ps1 -Path "C:\src\MyBlazorApp"
```

### Manual Overlay

After the Layer 2 script, some files may need manual overlay from a known-good reference or Copilot-assisted refinement. The typical overlay targets are:

- **Code-behind entity types** — the script creates correct file structure but may use wrong entity types and parameters
- **Auth form patterns** — Login/Register pages may need individual `[SupplyParameterFromForm]` string properties

### Copilot-Assisted Transforms (Remaining Work)

For transforms not yet handled by the Layer 2 script, use Copilot with the migration skill:

### What Layer 2 Handles

| Transform | Before | After |
|---|---|---|
| Data binding | `SelectMethod="GetProducts"` | Keep `SelectMethod="GetProducts"` — adapt method signature to `SelectHandler<T>` delegate (see below) |
| Template context | `<%#: Item.Name %>` | `@Item.Name` with `Context="Item"` |
| Lifecycle methods | `Page_Load` with `IsPostBack` check | `OnInitializedAsync` |
| Event handlers | `void Btn_Click(object sender, EventArgs e)` | `void Btn_Click()` |
| Navigation | `Response.Redirect("~/path")` | `NavigationManager.NavigateTo("/path")` |
| Form wrappers | `<form runat="server">` | Removed (or `<EditForm>` where needed) |
| Layout conversion | `<asp:ContentPlaceHolder ID="MainContent">` | `@Body` |
| Query parameters | `[QueryString] int? id` | `[SupplyParameterFromQuery]` |
| Route parameters | `[RouteData] int id` | `@page "/path/{id:int}"` + `[Parameter]` |

### How to Use Layer 2

1. Copy the [copilot-instructions-template.md](copilot-instructions-template.md) into your project's `.github/copilot-instructions.md`
2. Open each migrated `.razor` file with Copilot
3. Ask Copilot to apply the migration skill to the file
4. Review and accept the transforms

Or, if using Copilot Chat directly, reference the skill file:

```
@workspace Use the rules in .github/skills/bwfc-migration/SKILL.md to complete
the migration of this file. Look for TODO comments and unresolved patterns.
```

### Layer 2 Quality

Layer 2 is "high accuracy" rather than "100% accuracy" because:
- Data binding patterns vary by application (Copilot needs context about your data layer)
- Some event handler signatures have application-specific parameters
- Navigation routes depend on your URL structure

Always review Copilot's changes before committing.

---

## Layer 3: Architecture Decisions

**Tool:** [Data migration skill](skills/bwfc-data-migration/SKILL.md) + your own judgment

Layer 3 is the ~15% of migration work that requires understanding your application's architecture. No script or AI can make these decisions for you — but the data migration skill and Copilot can guide you through the options and trade-offs.

### Common Layer 3 Decisions

| Decision | Web Forms Pattern | Blazor Options |
|---|---|---|
| **Data access** | `SqlDataSource`, inline `DbContext` | EF Core + injected service, Dapper, repository pattern |
| **Session state** | `Session["key"]` | Scoped service, `ProtectedSessionStorage`, circuit state |
| **Authentication** | ASP.NET Membership / Identity | ASP.NET Core Identity, external provider, cookie auth |
| **Global.asax** | `Application_Start`, `Application_Error` | `Program.cs` middleware pipeline |
| **Web.config** | `<connectionStrings>`, `<appSettings>` | `appsettings.json`, user secrets, environment variables |
| **HTTP handlers** | `IHttpHandler`, `IHttpModule` | ASP.NET Core middleware |
| **Third-party APIs** | Direct `WebRequest`/`WebClient` calls | `HttpClient` via DI with `IHttpClientFactory` |

### Using the Data Migration Skill

The data migration skill is designed for interactive Copilot sessions. Point Copilot at your scan report and your partially-migrated files:

1. Share the `bwfc-scan.ps1` output
2. Share the `bwfc-migrate.ps1` output directory
3. Copilot identifies remaining `TODO` markers and decision points
4. Walk through each decision interactively

The skill provides decision frameworks for common architecture patterns — see the [full skill reference](skills/bwfc-data-migration/SKILL.md).

---

## Why This Ordering Matters

Layers must run in order: 1 → 2 → 3. Each layer assumes the previous one has completed.

- **Layer 1 before Layer 2:** Copilot expects files to already have `asp:` prefixes removed and expressions converted. If Layer 1 hasn't run, Copilot wastes time on mechanical transforms.
- **Layer 2 before Layer 3:** Architecture decisions are easier when the markup is already in Blazor syntax. You can see what's left to wire up instead of mentally translating Web Forms markup.

Don't skip layers. Don't try to do Layer 3 work in Layer 1. The pipeline is designed so that each layer makes the next layer's job easier.

---

## Time Estimates

Based on the [WingtipToys proof-of-concept](../planning-docs/WINGTIPTOYS-MIGRATION-EXECUTIVE-REPORT.md) and other sample app migrations (33 pages, 230+ control instances):

| Layer | Solo Developer | With Copilot/Agents |
|---|---|---|
| Layer 0 (scan) | 5 minutes | 5 minutes |
| Layer 1 (automated) | ~3 seconds | ~3 seconds |
| Layer 2 (script + overlay) | 8–12 hours | ~3 minutes (script) + 1–3 hours (overlay) |
| Layer 3 (architecture) | 10–14 hours | 8–12 hours |
| **Total** | **18–26 hours** | **8–15 hours** |

> **Note:** These estimates are from the WingtipToys PoC and are representative benchmarks, not absolute guarantees. Layer 3 time varies the most because it depends on your application's complexity. A simple CRUD app with no auth may have almost no Layer 3 work. An enterprise app with custom session state, complex auth, and third-party integrations will spend most of its time in Layer 3.

---

## Cross-References

- [QUICKSTART.md](QUICKSTART.md) — the linear "just do it" path through all three layers
- [CONTROL-COVERAGE.md](CONTROL-COVERAGE.md) — what's covered at each complexity level
- [CHECKLIST.md](CHECKLIST.md) — per-page tracking template organized by layer
- [Executive report](../planning-docs/WINGTIPTOYS-MIGRATION-EXECUTIVE-REPORT.md) — WingtipToys PoC metrics source (representative benchmarks for the pipeline)
