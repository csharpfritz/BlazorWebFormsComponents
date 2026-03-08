# Per-Page Migration Checklist

<!-- Updated 2026-03-08: Reflects 2-script pipeline, -TestMode switch, SSR architecture -->

**Copy this template for each page you migrate.** Use it as a GitHub issue body, a markdown checklist in your tracking doc, or paste it into your project management tool.

The checklist is organized by the [three-layer pipeline](METHODOLOGY.md). Work top to bottom — each section assumes the previous one is complete.

**Tooling:** The migration toolkit provides two scripts to automate Layers 1 and 2:
- **`bwfc-migrate.ps1`** — Fully automates Layer 1 (0 manual fixes for 5 consecutive runs). Use `-TestMode` to generate `ProjectReference` for local BWFC development.
- **`bwfc-migrate-layer2.ps1`** — Partially automates Layer 2 (Program.cs generation, code-behind scaffolding).

See the [migration-standards skill](skills/migration-standards/SKILL.md) for the full architecture reference.

---

## Template

```markdown
## Page: [PageName.aspx] → [PageName.razor]

**Source:** `[path/to/PageName.aspx]`
**Target:** `[path/to/PageName.razor]`
**Complexity:** [Trivial / Easy / Medium / Complex]
**Notes:** [Any page-specific context — what this page does, key controls used]

### Layer 1 — Automated (bwfc-migrate.ps1)

> Run `pwsh -File bwfc-migrate.ps1 -Path <source> -Output <target>` (add `-TestMode` for local BWFC dev). All items below are automated by the script.

- [ ] File renamed (.aspx → .razor, .ascx → .razor, .master → .razor)
- [ ] `<%@ Page %>` / `<%@ Control %>` / `<%@ Master %>` directive removed
- [ ] `@page "/route"` directive added (uses RelPath for subdirectory pages)
- [ ] `asp:` prefixes removed from all controls
- [ ] `runat="server"` removed from all elements
- [ ] Expressions converted (`<%: %>` → `@()`, `<%# %>` → `@context.`)
- [ ] URL references converted (`~/` → `/`)
- [ ] `<asp:Content>` wrappers removed (page body unwrapped)
- [ ] `ItemType` → `TItem` converted
- [ ] RouteData → `[Parameter]` converted (TODO on separate line)
- [ ] Enhanced navigation bypass added (`data-enhance-nav="false"` on API links)
- [ ] Code-behind file copied (.aspx.cs → .razor.cs) with TODO annotations

### Layer 2 — Copilot-Assisted / bwfc-migrate-layer2.ps1 (Structural Transforms)

> Run `pwsh -File bwfc-migrate-layer2.ps1 -Path <blazor-output>` to automate Program.cs generation (Pattern C) and code-behind scaffolding (Pattern A). Auth form simplification (Pattern B) and remaining items below need manual/Copilot-assisted work.

- [ ] `SelectMethod` → `Items` (or `DataItem`) binding wired
- [ ] Data loading moved to `OnInitializedAsync`
- [ ] Template `Context="Item"` variables added to all templates
- [ ] Event handlers converted to Blazor signatures (remove `sender`, `EventArgs`)
- [ ] `Page_Load` → `OnInitializedAsync`; `if (!IsPostBack)` works AS-IS via `WebFormsPageBase` (optionally simplify)
- [ ] Navigation calls converted (`Response.Redirect` → `NavigationManager.NavigateTo`)
- [ ] `<form runat="server">` removed (or converted to `<EditForm>` if validators present)
- [ ] `Session["key"]` references identified and marked for Layer 3
- [ ] Query parameters converted (`[QueryString]` → `[SupplyParameterFromQuery]`)
- [ ] Route parameters converted (`[RouteData]` → `[Parameter]` with `@page` route)
- [ ] `@using` statements added for model namespaces
- [ ] `@inject` statements added for required services

### Layer 3 — Architecture Decisions

- [ ] Data access pattern decided (injected service, EF Core, Dapper, etc.)
- [ ] Data service implemented and registered in `Program.cs`
- [ ] Session state replaced with appropriate Blazor pattern (scoped service / ProtectedSessionStorage)
- [ ] Authentication/authorization wired (if page requires auth)
- [ ] Third-party integrations ported (API calls, payment, etc.)
- [ ] Route registered and tested (`@page` directive matches expected URL)
- [ ] ViewState-dependent logic converted to component fields

### Verification

- [ ] Page builds without errors (`dotnet build`)
- [ ] Page renders in browser without exceptions
- [ ] Visual layout matches original Web Forms page
- [ ] All interactive features work (buttons, forms, navigation, sorting, paging)
- [ ] No JavaScript console errors in browser dev tools
- [ ] Data displays correctly (correct records, correct formatting)
- [ ] Form submissions work (validation fires, data saves)
```

---

## Usage Tips

### For GitHub Issues

Create one issue per page (or per group of related pages). Paste the template above and fill in the header fields. As you work through the migration, check items off. This gives your team visibility into migration progress.

### For Tracking Documents

Create a single `MIGRATION-TRACKING.md` in your project. Paste one copy of the checklist per page. Use it as a daily standup reference:

```markdown
# Migration Tracking

## Completed
- [x] Default.aspx → Default.razor (Trivial) — Done 2026-03-01
- [x] About.aspx → About.razor (Trivial) — Done 2026-03-01

## In Progress
<!-- EXAMPLE — replace with your actual pages -->
- [ ] ItemList.aspx → ItemList.razor (Medium) — Layer 2

## Not Started
<!-- EXAMPLE — replace with your actual pages -->
- [ ] Dashboard.aspx → Dashboard.razor (Medium)
- [ ] Login.aspx → Login.razor (Complex)
```

### Recommended Migration Order

Migrate pages in this order to minimize blocked work:

1. **Layout** — `Site.Master` → `MainLayout.razor` (everything depends on this)
2. **Leaf pages** — About, Contact, Error pages (trivial, builds confidence)
3. **Read-only data pages** — Product list, catalog (medium, tests data binding)
4. **CRUD pages** — Cart, admin, forms (medium-complex, tests event handling)
5. **Auth-dependent pages** — Login, account management (complex, requires Identity setup)
6. **Integration pages** — Checkout, payment, external APIs (complex, requires Layer 3)

---

## Cross-References

- [QUICKSTART.md](QUICKSTART.md) — the full step-by-step walkthrough
- [METHODOLOGY.md](METHODOLOGY.md) — why the checklist is organized by layer
- [CONTROL-COVERAGE.md](CONTROL-COVERAGE.md) — complexity ratings for deciding page complexity
