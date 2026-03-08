# BWFC Migration Toolkit

**Migrate your ASP.NET Web Forms application to Blazor — systematically, not heroically.**

This toolkit packages everything you need to take a Web Forms app and bring it to Blazor using the [BlazorWebFormsComponents](https://www.nuget.org/packages/Fritz.BlazorWebFormsComponents) (BWFC) library. It combines automated scripts, Copilot skills, and a decision-making agent into a three-layer pipeline that handles ~85% of migration work mechanically or with AI assistance, leaving you to focus on the architecture decisions that actually need a human.

> **Latest (Run 13):** 25/25 integration tests passing. The migration script now bakes in 3 previously-manual fixes — SSR enhanced-navigation disabling, ReadOnly attribute warnings, and LoginStatus→logout link conversion — targeting **0 manual fixes** for Run 14. SSR (Static Server Rendering) is the default architecture; package version pinned to **stable 10.0.0**.

- **NuGet Package:** <https://www.nuget.org/packages/Fritz.BlazorWebFormsComponents>
- **GitHub Repository:** <https://github.com/FritzAndFriends/BlazorWebFormsComponents>

---

## Who This Is For

You're a .NET developer who owns a Web Forms application and wants to migrate it to Blazor. You know Web Forms well. You may not know Blazor deeply — that's fine. You have GitHub Copilot available and are willing to use it.

---

## Prerequisites

| Requirement | Version | Why |
|---|---|---|
| .NET SDK | 10.0+ | Blazor Server target framework (.NET 10 — SSR default) |
| PowerShell | 7.0+ | Migration scripts require PowerShell Core |
| BWFC NuGet package | 10.0.0 (stable) | `dotnet add package Fritz.BlazorWebFormsComponents --version 10.0.0` |
| GitHub Copilot | Any tier | Used for Layer 2 structural transforms |

---

## How to Use This Toolkit

1. **Copy the `skills/` folder** into your project's `.github/skills/` directory.
2. **Copy the `scripts/` folder** to your project root.
3. Install the BWFC NuGet package: `dotnet add package Fritz.BlazorWebFormsComponents --version 10.0.0`
4. Follow the [QUICKSTART.md](QUICKSTART.md) guide.

---

## What's Included

### `skills/` — Copilot Skills

Copy these into your project's `.github/skills/` directory so Copilot can use them during migration.

| Skill | Description |
|---|---|
| [`bwfc-migration/SKILL.md`](skills/bwfc-migration/SKILL.md) | **Core markup migration** — control translation, expression conversion, data binding, code-behind lifecycle, and Master Page to Layout conversion. Covers Layer 2 structural transforms (~45% of migration work). |
| [`bwfc-identity-migration/SKILL.md`](skills/bwfc-identity-migration/SKILL.md) | **Identity & auth migration** — OWIN to ASP.NET Core auth middleware, login page migration, BWFC login controls, role-based authorization, and AuthorizeView patterns. |
| [`bwfc-data-migration/SKILL.md`](skills/bwfc-data-migration/SKILL.md) | **Data access & architecture migration** — EF6 to EF Core, DataSource controls to service injection, Session state to scoped services, Global.asax to Program.cs, Web.config to appsettings.json. Covers Layer 3 architecture decisions. |

### `scripts/` — PowerShell Migration Scripts

Copy these to your project root. Requires PowerShell 7.0+.

| Script | Description |
|---|---|
| [`bwfc-scan.ps1`](scripts/bwfc-scan.ps1) | **Scanner** — inventories your Web Forms project, identifies controls, counts pages, and outputs a migration readiness report. Run this first. |
| [`bwfc-migrate.ps1`](scripts/bwfc-migrate.ps1) | **Mechanical transformer** — Layer 1 automated transforms: strips `asp:` prefixes, removes `runat="server"`, converts expressions, renames `.aspx`→`.razor`. Also handles SSR enhanced-nav disabling, ReadOnly attribute warnings, LoginStatus→logout link conversion, and logout form→link conversion. Handles ~40% of migration work deterministically. |

---

## The Three-Layer Pipeline

Migration isn't one step — it's three layers that handle different kinds of work:

| Layer | What | How | Coverage |
|---|---|---|---|
| **Layer 1** — Automated | Tag prefix removal, `runat` removal, expression conversion, file renaming | [`scripts/bwfc-migrate.ps1`](scripts/bwfc-migrate.ps1) | ~40% of work |
| **Layer 2** — Copilot-Assisted | Data binding rewiring, layout conversion, lifecycle method migration | [`skills/bwfc-migration/SKILL.md`](skills/bwfc-migration/SKILL.md) | ~45% of work |
| **Layer 3** — Architecture Decisions | Identity, EF Core, session state, third-party integrations | [`skills/bwfc-data-migration/SKILL.md`](skills/bwfc-data-migration/SKILL.md) + human judgment | ~15% of work |

**Start here:** [QUICKSTART.md](QUICKSTART.md) — the linear "just tell me what to do" path.

---

## Quick Overview

```
1. Scan     →  ./scripts/bwfc-scan.ps1 -Path ./MyWebFormsApp -OutputFormat Markdown
2. Transform →  ./scripts/bwfc-migrate.ps1 -Path ./MyWebFormsApp -Output ./MyBlazorApp
3. Guide     →  Open in editor with Copilot + BWFC migration skill
4. Verify    →  dotnet build && dotnet run
```

---

## Documentation

| Document | What It Covers |
|---|---|
| [**README.md**](README.md) | You are here — overview and entry point |
| [**QUICKSTART.md**](QUICKSTART.md) | Step-by-step: scan → migrate → verify |
| [**CONTROL-COVERAGE.md**](CONTROL-COVERAGE.md) | Full 58-component coverage table with complexity ratings |
| [**METHODOLOGY.md**](METHODOLOGY.md) | Three-layer pipeline deep-dive |
| [**CHECKLIST.md**](CHECKLIST.md) | Per-page migration checklist template |
| [**copilot-instructions-template.md**](copilot-instructions-template.md) | Drop-in `.github/copilot-instructions.md` for your project |

---

## What's New (Run 13)

The migration script now automates 3 fixes that previously required manual intervention:

| Fix | Function | What It Does |
|---|---|---|
| **SSR enhanced-nav disabling** | `Add-EnhancedNavDisable` | Adds `data-enhance-nav="false"` to `<a>` links targeting API endpoints, logout, and cart actions — prevents SSR enhanced navigation from intercepting 302 redirects |
| **ReadOnly attribute warnings** | `Add-ReadOnlyWarning` | Inserts `<!-- MIGRATION NOTE -->` comments for `ReadOnly="True"` on TextBox and `readonly` on `<input>`, flagging for developer review |
| **LoginStatus → logout link** | `ConvertFrom-LoginStatus` | Converts `<asp:LoginStatus>` to an `<a>` logout link with `data-enhance-nav="false"`, extracting `LogoutText`/`LogoutPageUrl` attributes |
| **Logout form → link** | `Convert-LogoutFormToLink` | Detects logout `<form>`+`<button>` patterns and converts to `<a>` links, avoiding Playwright button-ordering conflicts |

**Key changes:**

- **SSR is the default architecture** — the pipeline targets Static Server Rendering with selective interactivity, matching the .NET 10 Blazor template defaults
- **Package version pinned to 10.0.0** — use `dotnet add package Fritz.BlazorWebFormsComponents --version 10.0.0` for stable, reproducible builds
- **Test results:** Run 13 achieved **25/25 integration tests passing** (305 transforms). Run 14 targets **0 manual fixes**.

---

## What BWFC Doesn't Cover

Be honest with yourself about scope. BWFC provides 58 drop-in components, but it does **not** cover:

- **DataSource controls** — `SqlDataSource`, `ObjectDataSource`, `EntityDataSource` have no Blazor equivalents. Replace with injected services.
- **Wizard control** — No BWFC equivalent. Implement as a multi-step Blazor component.
- **AJAX Control Toolkit extenders** — Third-party extenders (ModalPopup, AutoComplete, etc.) need Blazor-native replacements.
- **Web Parts** — No equivalent. Redesign as Blazor components.
- **ASP.NET Identity plumbing** — BWFC provides Login/LoginView UI components, but the underlying identity system must be migrated to ASP.NET Core Identity separately.

See [CONTROL-COVERAGE.md](CONTROL-COVERAGE.md) for the full supported/unsupported breakdown.

---

## How Long Will This Take?

Based on the [WingtipToys proof-of-concept](../planning-docs/WINGTIPTOYS-MIGRATION-EXECUTIVE-REPORT.md) (33 pages, 230+ control instances):

| Approach | Estimated Time | Per-Page Average |
|---|---|---|
| Manual rewrite (no BWFC) | 60–80 hours | ~2–2.5 hours |
| **BWFC + three-layer pipeline** | **18–26 hours** | **~35–45 minutes** |
| BWFC + pipeline + AI agents | ~4.5 hours | ~8 minutes |

That's a **55–70% reduction** in migration effort.

---

## Next Steps

1. **[Read the Quickstart](QUICKSTART.md)** — get your first page migrated
2. **[Check control coverage](CONTROL-COVERAGE.md)** — verify your controls are supported
3. **[Copy the Copilot instructions template](copilot-instructions-template.md)** — set up Copilot for your project
