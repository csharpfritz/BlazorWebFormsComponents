# Project Context

- **Owner:** Jeffrey T. Fritz
- **Project:** BlazorWebFormsComponents — Blazor components emulating ASP.NET Web Forms controls for migration
- **Stack:** C#, Blazor, .NET, ASP.NET Web Forms, bUnit, xUnit, MkDocs, Playwright
- **Created:** 2026-02-10

## Project Learnings (from import)

- The project has two skill locations: `.ai-team/skills/` (team-earned skills) and `migration-toolkit/skills/` (shipped migration skills for end users)
- Existing migration skills: bwfc-migration, bwfc-data-migration, bwfc-identity-migration, migration-standards
- Existing team skills: base-class-upgrade, blazor-parameter-aliases, component-documentation, migration-standards, sample-pages, shared-base-extraction, squad-conventions, status-reconciliation, webforms-html-audit
- A known critical failure mode: agents consistently replace BWFC controls with plain HTML during migration (Layer 2 problem). Skills must have mandatory rules to prevent this.
- The migration-toolkit also contains: scripts (bwfc-migrate.ps1), copilot-instructions-template.md, METHODOLOGY.md, CHECKLIST.md, CONTROL-COVERAGE.md
- Skills use SKILL.md format with confidence levels: low, medium, high
- The BWFC component library has 110+ components that must be preserved during migration

## Learnings

### 2026-03-06: Run 7 Skill Updates

**Skills updated:**
- `migration-standards/SKILL.md` — Major update with 6 specific changes from Run 7 and Jeff's directives:
  - `WebFormsPageBase` replaces `ComponentBase` as canonical base class for migrated pages
  - LoginView is now a native BWFC component — removed the old LoginView → AuthorizeView conversion
  - Page_Load → OnInitializedAsync codified as DEFAULT RULE
  - CSS `<link>` elements MUST go to App.razor, not layout
  - MasterPage migration preserves BWFC semantics
  - Fixed "Using Page as Base Class" anti-pattern to show `WebFormsPageBase`
  - Added Runtime Gotchas table (4 issues discovered in benchmarks)

**Skills created:**
- `blazor-auth-migration/SKILL.md` (medium confidence) — Scoped AuthenticationStateProvider + cookie auth pattern. Singleton providers cause session bleed. Discovered in Run 7 Iteration 2.
- `blazor-form-submission/SKILL.md` (low confidence) — Blazor strips onclick from buttons during enhanced navigation. Two patterns: anchor-based POST for auth forms, EditForm for in-component handling. First observation from Run 7 Iteration 3.

**Key patterns codified:**
- `@inherits WebFormsPageBase` in `_Imports.razor` is a scaffold requirement — without it Page.Title, IsPostBack, GetRouteUrl all fail
- Cookie auth registration order matters: AddAuthentication → AddCookie → AddScoped<Provider> → AddScoped<AuthenticationStateProvider>(factory)
- The anchor-based form submit (`<a role="button">`) is a workaround, not necessarily the long-term pattern — marked as low confidence

 Team update (2026-03-06): WebFormsPageBase is the canonical base class for all migrated pages (not ComponentBase). All agents must use WebFormsPageBase  decided by Jeffrey T. Fritz
 Team update (2026-03-06): LoginView is a native BWFC component  do NOT convert to AuthorizeView. Strip asp: prefix only  decided by Jeffrey T. Fritz

### 2026-03-30: Phase 4 Migration Skills — L1 Frozen, L2 Skills Created

**Strategic decision:** L1 PowerShell script (`bwfc-migrate.ps1`) is **frozen at Phase 3**. It handles ~70% of migration work (deterministic transforms). All remaining work shifts to **Layer 2 (L2) Copilot skills** for contextual, AI-guided transforms.

**Rationale:**
- Deterministic regex/AST transforms have reached their practical limit (~70% coverage)
- Remaining gaps require contextual reasoning: architecture decisions, cross-file understanding, domain knowledge
- L1 can *detect* patterns but cannot reliably *convert* them without risking incorrect migrations
- Skills provide decision trees, before/after examples, and "What Developers Must Do Manually" guidance

**Skills created:**
- `bwfc-session-state/SKILL.md` (low confidence) — Application["key"] → singleton/scoped services, Cache["key"] → IMemoryCache, HttpContext.Current → IHttpContextAccessor. Covers items #13, #14, #15, #16 from migration-shim-analysis.
- `bwfc-middleware-migration/SKILL.md` (low confidence) — HttpModule → middleware, Global.asax events → Program.cs (Application_Start, Session_Start, Application_Error). Covers items #22, #23, #24.
- `bwfc-usercontrol-migration/SKILL.md` (low confidence) — .ascx → component with [Parameter], FindControl → @ref patterns, parent/child communication, event delegation. Covers items #30, #31, #8.

**Skills enhanced:**
- `bwfc-identity-migration/SKILL.md` (v2.0.0, low → low confidence) — Added comprehensive FormsAuthentication, Membership provider, and Roles provider migration patterns. Includes:
  - FormsAuthentication.SetAuthCookie → HttpContext.SignInAsync in minimal API endpoints
  - Membership.CreateUser → UserManager<T>.CreateAsync with password hash compatibility notes
  - Roles.IsUserInRole → UserManager<T>.IsInRoleAsync + policy-based authorization
  - Database schema migration table (Membership → Identity)
  - Migration cheat sheet for detecting auth system in Web Forms projects
  - Covers items #25, #26, #27 from migration-shim-analysis.

**Updated documentation:**
- `bwfc-migration/CODE-TRANSFORMS.md` — Added "Phase 4: Skills-Based Transforms" section explaining:
  - Why L1 is frozen (deterministic vs contextual)
  - L1 vs L2 decision table
  - Three-layer migration strategy (L1 automated, L2 AI-guided, L3 developer judgment)
  - Links to each Phase 4 skill

**Key patterns identified:**
- **Application state decisions:** L1 can detect `Application["key"]`, but only developers can decide if data is global (singleton) or per-user (scoped)
- **Cache lifetime decisions:** L1 can detect `Cache["key"]`, but only developers know if data should use IMemoryCache or IDistributedCache
- **HttpModule event mapping:** L1 can detect IHttpModule classes, but only developers understand the business logic to map correctly to middleware
- **FindControl patterns:** L1 can detect FindControl() calls, but understanding component tree structure requires human reasoning
- **Authentication system detection:** Projects may use Identity (OWIN), Membership, or FormsAuthentication — each requires different migration paths

**Confidence levels:**
- All new skills marked as "low confidence" — these are first-draft guides based on Forge's analysis
- Skills will iterate based on real-world migration feedback
- "What Developers Must Do Manually" sections explicitly document non-automatable tasks

**Team decision recorded:** L1 PowerShell frozen at Phase 3. Phase 4 = skills-based L2 transforms only.

**Skills created:**
- wfc-session-state/SKILL.md  Application[], Cache[], HttpContext.Current state migration decisions (singleton vs scoped, IMemoryCache vs IDistributedCache)
- wfc-middleware-migration/SKILL.md  HttpModule, Global.asax  middleware conversion (event  middleware position mapping, pipeline order)
- wfc-usercontrol-migration/SKILL.md  .ascx, FindControl, [Parameter]  component conversion (public property vs internal state, @ref timing)
- wfc-identity-migration/SKILL.md (v2)  Added FormsAuthentication, Membership, Roles sections (auth system detection, password hash compatibility)

**Documentation updated:**
- migration-toolkit/skills/bwfc-migration/CODE-TRANSFORMS.md  Added "Phase 4: Skills-Based Transforms" section
- .ai-team/agents/psylocke/history.md  Recorded Phase 4 learnings

**Strategic outcome:** Three-layer migration model formalized and locked in:
1. L1 (PowerShell)  70% deterministic automation  **FROZEN at Phase 3**
2. L2 (Skills)  25% contextual AI-guided transforms  **Phase 4 active**
3. L3 (Developer)  5% architectural decisions  **always requires human review**
