---
name: "migration-standards"
description: "Canonical standards for migrating ASP.NET Web Forms applications to Blazor using BWFC"
domain: "migration"
confidence: "high"
source: "earned"
---

<!-- Updated 2026-03-09: Added ASPX URL rewriting standard (ContosoUniversity Run 04) -->
<!-- Updated 2026-03-09: Added required DI services (ContosoUniversity Run 05) -->

## Context

When migrating an ASP.NET Web Forms application to Blazor using BlazorWebFormsComponents, these standards define the canonical target architecture, tooling choices, and migration patterns. Established through nine WingtipToys migration benchmark runs (Runs 8–16) and three ContosoUniversity benchmark runs, codified as a directive by Jeffrey T. Fritz.

Apply these standards to:
- Migration script (`bwfc-migrate.ps1` + `bwfc-migrate-layer2.ps1`) enhancements
- Copilot-assisted Layer 2 work
- Migration documentation and checklists
- Any new migration test runs

## Critical Migration Requirements

<!-- Updated 2026-03-08: Added ContosoUniversity Run 02-03 learnings -->

These requirements are essential for migration success. Missing any of them will cause acceptance test failures or build errors.

### Navigation Link IDs

**Always generate `id` attributes on navigation links.** Acceptance tests locate nav links by ID (e.g., `id="home"`, `id="about"`, `id="students"`). Without IDs, tests like `NavLink_NavigatesToCorrectPage` fail.

**When converting Site.Master to MainLayout.razor:**
- Preserve existing IDs from source nav links
- If no ID exists, derive one from the link text (lowercase, alphanumeric only)
- Example: "About Us" → `id="aboutus"`, "Home" → `id="home"`

```razor
@* WRONG — missing IDs *@
<a href="/Home">Home</a>
<a href="/About">About</a>

@* RIGHT — IDs present for test compatibility *@
<a id="home" href="/Home">Home</a>
<a id="about" href="/About">About</a>
```

The Layer 1 script's `Add-NavLinkIds` function handles this automatically.

### GridView Field Wrapping

**BoundField, TemplateField, ButtonField, HyperLinkField, ImageField, CommandField, and CheckBoxField MUST be wrapped in `<Columns>`.** Direct children of GridView cause RZ9996 errors.

```razor
@* WRONG — direct child causes build error *@
<GridView DataSource="@data">
    <BoundField DataField="Name" HeaderText="Name" />
</GridView>

@* RIGHT — wrapped in Columns *@
<GridView DataSource="@data">
    <Columns>
        <BoundField DataField="Name" HeaderText="Name" />
    </Columns>
</GridView>
```

The Layer 1 script's `Wrap-GridViewColumns` function handles this automatically.

### Generic Type Parameters

**All generic BWFC components require their type parameter.** Missing types cause RZ10001 errors.

| Component | Type Parameter | Example |
|-----------|---------------|---------|
| GridView | `ItemType` | `<GridView ItemType="Course" ...>` |
| DropDownList | `TItem` | `<DropDownList TItem="Department" ...>` |
| BoundField | `ItemType` | `<BoundField ItemType="Course" ...>` |
| DetailsView | `ItemType` | `<DetailsView ItemType="Student" ...>` |
| ListView | `ItemType` | `<ListView ItemType="Product" ...>` |
| FormView | `ItemType` | `<FormView ItemType="Order" ...>` |

### Required DI Services

**The following services MUST be registered for BWFC components to work correctly:**

```csharp
// Program.cs — required service registrations

var builder = WebApplication.CreateBuilder(args);

// Required for BWFC components (GridView, DetailsView, etc.)
builder.Services.AddHttpContextAccessor();

// BWFC services — call AFTER AddHttpContextAccessor
builder.Services.AddBlazorWebFormsComponents();

// Entity Framework — initialize schema at startup
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<YourDbContext>();
    db.Database.EnsureCreated();
}
```

**Why `AddHttpContextAccessor()` is required:**
- BWFC's GridView, DetailsView, and other components inject `IHttpContextAccessor`
- Without this registration, pages using BWFC data controls fail with HTTP 500
- Error: "Cannot provide a value for property 'HttpContextAccessor' on type 'GridView'"

**Why `EnsureCreated()` at startup:**
- Creates database schema from EF Core model if it doesn't exist
- Without this, database queries fail on empty/non-existent database
- For production, consider EF Core migrations instead

**Derive the type from:**
1. The `ItemType` attribute in Web Forms markup (if present)
2. The `SelectMethod` return type in the code-behind
3. The data source property type

### Code-Behind Base Class

**All code-behinds must use the correct base class:**

| File Type | Base Class | Notes |
|-----------|-----------|-------|
| Page code-behind | `ComponentBase` or `WebFormsPageBase` | Use `WebFormsPageBase` for Page.Title/IsPostBack compat |
| Layout code-behind | `LayoutComponentBase` | Or delete if unused |
| Component code-behind | `ComponentBase` | Standard Blazor |

**Never use `System.Web.UI.Page`** — it doesn't exist in .NET Core:

```csharp
// WRONG — Web Forms base class
using System.Web.UI;
public partial class About : System.Web.UI.Page { }

// RIGHT — Blazor base class
using Microsoft.AspNetCore.Components;
public partial class About : ComponentBase { }
```

### Using Statement Conversion

**Replace all `System.Web.*` usings with Blazor equivalents:**

| Remove | Add Instead |
|--------|-------------|
| `using System.Web;` | — (not needed) |
| `using System.Web.UI;` | `using Microsoft.AspNetCore.Components;` |
| `using System.Web.UI.WebControls;` | `using BlazorWebFormsComponents;` |
| `using System.Web.Security;` | `using Microsoft.AspNetCore.Identity;` |

### Event Handler Preservation

**Preserve event handler attributes and wire them to methods.** Do NOT strip `OnClick`, `OnCommand`, etc.:

```razor
@* WRONG — handler stripped, button does nothing *@
<Button Text="Add" />

@* RIGHT — handler preserved, method signature updated *@
<Button Text="Add" OnClick="Add_Click" />

@code {
    private void Add_Click(MouseEventArgs e)
    {
        // Converted handler logic
    }
}
```

### ASPX URL Backward Compatibility

**Use URL Rewriting middleware instead of duplicate `@page` directives.** When users have bookmarked or linked to `.aspx` URLs, preserve them using `RewriteOptions.AddRedirect` in Program.cs — NOT by adding multiple `@page` directives to Razor files.

**Add to Program.cs (before `app.UseRouting()`):**

```csharp
using Microsoft.AspNetCore.Rewrite;

// ASPX URL backward compatibility — redirect .aspx URLs to Blazor routes
var rewriteOptions = new RewriteOptions()
    .AddRedirect(@"^Default\.aspx$", "/", statusCode: 301)
    .AddRedirect(@"^(.+)\.aspx$", "$1", statusCode: 301);
app.UseRewriter(rewriteOptions);
```

**Why 301 redirects over `@page` directives:**
- **SEO-friendly** — 301 tells search engines the URL has permanently moved
- **Single source of truth** — one rule handles all pages, not scattered across Razor files
- **Query strings preserved** — `AddRedirect` automatically preserves query strings
- **Case-insensitive** — both `/Products.ASPX` and `/products.aspx` are handled
- **Removable** — easy to delete when all legacy URLs have been updated

**Do NOT do this:**
```razor
@* WRONG — duplicate @page directives clutter every file *@
@page "/About"
@page "/About.aspx"
```

The migration script should inject the `RewriteOptions` snippet into Program.cs when ASPX backward compatibility is needed.

## Patterns

### Target Architecture

| Setting | Standard |
|---|---|
| Framework | **.NET 10** (or latest LTS/.NET preview) |
| Project template | `dotnet new blazor --interactivity Server` |
| Render mode | **SSR (Static Server Rendering)** with per-component `InteractiveServer` opt-in (see [Render Mode](#render-mode--ssr-default) below) |
| Base class | `WebFormsPageBase` for pages (`@inherits` in `_Imports.razor`); `ComponentBase` for non-page components |
| Layout | `MainLayout.razor` with `@inherits LayoutComponentBase` and `@Body` |

### Render Mode — SSR Default

> **Default to SSR (Static Server Rendering)** with per-component `InteractiveServer` opt-in. This was established in Run 12 and confirmed stable through Run 16 (5 consecutive 100% results). SSR eliminates `HttpContext`/cookie/session problems that plagued the InteractiveServer approach in Runs 8–11.

**`App.razor`** — do NOT add `@rendermode` to `<Routes>` or `<HeadOutlet>` (SSR is the default when no render mode is specified):

```razor
<HeadOutlet />
...
<Routes />
```

**Per-component interactivity** — add `@rendermode` only to components that need interactive behavior:

```razor
<MyInteractiveComponent @rendermode="InteractiveServer" />
```

**`_Imports.razor`** — add the static using for convenience when applying per-component render modes:

```razor
@using static Microsoft.AspNetCore.Components.Web.RenderMode
```

> **Do NOT place `@rendermode InteractiveServer` as a standalone line in `_Imports.razor`** — `@rendermode` is a directive attribute, not a standalone directive. It will cause build errors (RZ10003, CS0103, RZ10024).

> **Why SSR over global InteractiveServer:** Under global InteractiveServer, `HttpContext` is NULL during WebSocket circuits. This breaks cookie auth, session state, and any middleware-dependent operations. SSR preserves a real HTTP request/response for every page load, making auth endpoints, session cookies, and middleware work correctly. Add `InteractiveServer` only to specific components that need real-time updates.

> **Reference:** [ASP.NET Core Blazor render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes)

### Page Base Class

`WebFormsPageBase` eliminates per-page boilerplate when migrating Web Forms code-behind. Instead of injecting `IPageService` into every page, a single `@inherits` directive in `_Imports.razor` gives all pages access to familiar Web Forms properties.

**One-time setup:**

1. **`_Imports.razor`** — add the base class directive:

```razor
@inherits BlazorWebFormsComponents.WebFormsPageBase
```

2. **Layout (`MainLayout.razor`)** — add the Page render component (renders `<PageTitle>` and `<meta>` tags):

```razor
<BlazorWebFormsComponents.Page />
```

**Properties available on every page:**

| Property | Behavior |
|---|---|
| `Title` | Delegates to `IPageService.Title` — `Page.Title = "X"` works unchanged |
| `MetaDescription` | Delegates to `IPageService.MetaDescription` |
| `MetaKeywords` | Delegates to `IPageService.MetaKeywords` |
| `IsPostBack` | Always returns `false` — `if (!IsPostBack)` always enters block |
| `Page` | Returns `this` — enables `Page.Title = "X"` dot syntax |

**What is NOT provided (forces proper Blazor migration):**

- `Page.Request` — use `IHttpContextAccessor` or `NavigationManager`
- `Page.Response` — use `NavigationManager` for redirects
- `Page.Session` — use scoped DI services

**When to still use `@inject IPageService`:** Non-page components (e.g., a shared header or sidebar) that need access to page metadata should inject `IPageService` directly. `WebFormsPageBase` only applies to routable pages.

### Database Migration

- **Always** migrate EF6 → EF Core using the **latest .NET 10 packages** (currently **10.0.3**)
- Required packages: `Microsoft.EntityFrameworkCore` (10.0.3), `.SqlServer` / `.Sqlite`, `.Tools`, `.Design`
- Prefer SQLite for local dev / demos; SQL Server for production
- Replace `DropCreateDatabaseIfModelChanges` with `EnsureCreated` + idempotent seed
- Use `IDbContextFactory<T>` or scoped `DbContext` injection
- Models: nullable reference types, file-scoped namespaces, modern init patterns

### Identity Migration

- When ASP.NET Identity is present → prefer **ASP.NET Core Identity**
- OWIN middleware → ASP.NET Core middleware pipeline
- Postback-based auth → HTTP endpoints + cookie auth
- Use `dotnet aspnet-codegenerator identity` for scaffolding
- `SignInManager` / `UserManager` APIs change — full subsystem replacement

### Event Handler Strategy

BWFC components already expose EventCallback parameters with **matching Web Forms names**:

| Web Forms | BWFC | Action |
|---|---|---|
| `OnClick="Handler"` | `OnClick` (EventCallback<MouseEventArgs>) | **Preserve attribute verbatim** — only update handler signature |
| `OnCommand="Handler"` | `OnCommand` (EventCallback<CommandEventArgs>) | Preserve, update signature |
| `OnSelectedIndexChanged="Handler"` | `OnSelectedIndexChanged` (EventCallback<ChangeEventArgs>) | Preserve, update signature |
| `OnTextChanged="Handler"` | `OnTextChanged` (EventCallback<ChangeEventArgs>) | Preserve, update signature |
| `OnCheckedChanged="Handler"` | `OnCheckedChanged` (EventCallback<ChangeEventArgs>) | Preserve, update signature |

**Signature change pattern:**
```csharp
// Web Forms
protected void Button1_Click(object sender, EventArgs e) { ... }

// Blazor (BWFC)
private void Button1_Click(MouseEventArgs e) { ... }
// or
private async Task Button1_Click(MouseEventArgs e) { ... }
```

The script should preserve the attribute and annotate the signature change needed.

### Data Control Strategy — Prefer BWFC Over Raw HTML

| Web Forms Control | BWFC Component | Use Instead Of |
|---|---|---|
| `<asp:ListView>` | `<ListView Items="@data">` with `ItemTemplate` | `@foreach` + HTML table |
| `<asp:GridView>` | `<GridView Items="@data">` with columns | `@foreach` + `<table>` |
| `<asp:FormView>` | `<FormView Items="@data">` with `ItemTemplate` | Direct HTML rendering |
| `<asp:Repeater>` | `<Repeater Items="@data">` with `ItemTemplate` | `@foreach` loops |
| `<asp:DetailsView>` | `<DetailsView Items="@data">` with fields | Manual field rendering |
| `<asp:DataList>` | `<DataList Items="@data">` with `ItemTemplate` | `@foreach` + grid HTML |

**SelectMethod → Items:** Replace `SelectMethod="GetMethodName"` with `Items="@_items"` where `_items` is populated in `OnInitializedAsync` via an injected service or DbContext.

### Session State → Scoped Services

- Replace `Session["key"]` with a scoped DI service
- Use `IHttpContextAccessor` for cookie-based persistence when needed
- Register in `Program.cs` with `builder.Services.AddScoped<TService>()`
- Example: `Session["CartId"]` → `CartStateService` with cookie-based cart ID

### Blazor Enhanced Navigation

When linking to minimal API endpoints from Blazor pages, use `<form method="post">` or add `data-enhance-nav="false"` to prevent Blazor's enhanced navigation from intercepting the request. Enhanced navigation handles `<a href>` clicks as client-side SPA navigation, which breaks links to server endpoints (the request never reaches the server). This applies to all auth endpoints, cart operations, file downloads, and any other minimal API routes.

### Static Asset Relocation

- All static files → `wwwroot/`
- CSS bundles (`BundleConfig.cs`) → explicit `<link>` tags in `App.razor`
- JS bundles → explicit `<script>` tags in `App.razor`
- Image paths update: `~/Images/` → `/Images/`
- Font paths: same pattern

### Page Lifecycle Mapping

| Web Forms | Blazor | Notes |
|---|---|---|
| `Page_Load` | `OnInitializedAsync` | One-time init |
| `Page_PreInit` | `OnInitializedAsync` (early) | Theme setup |
| `Page_PreRender` | `OnAfterRenderAsync` | Post-render logic |
| `IsPostBack` check | `if (!IsPostBack)` works AS-IS via `WebFormsPageBase` | Always enters block; `if (IsPostBack)` without `!` is dead code — flag for review |
| `Page.Title` | `Page.Title = "X"` works AS-IS via `WebFormsPageBase` | `WebFormsPageBase` delegates to `IPageService`. `<BlazorWebFormsComponents.Page />` in layout renders `<PageTitle>` and `<meta>` tags. |
| `Response.Redirect` | `NavigationManager.NavigateTo()` | Inject `NavigationManager` |

### 2-Script Pipeline (Layer 1 + Layer 2)

<!-- Updated 2026-03-08: Reflects Run 16 pipeline evolution -->

The migration pipeline uses **two scripts** plus targeted manual overlay:

| Stage | Script | What It Handles |
|-------|--------|----------------|
| **Layer 1** | `bwfc-migrate.ps1` | Mechanical markup transforms (100% automated since Run 14) |
| **Layer 2** | `bwfc-migrate-layer2.ps1` | Semantic code-behind transforms (partially automated since Run 16) |
| **Manual overlay** | — | Business-logic-specific fixes that resist automation |

#### Layer 1 — `bwfc-migrate.ps1`

**Script handles (fully automated, 0 manual fixes for 5 consecutive runs):**
- `asp:` prefix stripping (preserves BWFC tags)
- Data-binding expression conversion (5 variants)
- LoginView → **preserve as BWFC LoginView** — do NOT rewrite as AuthorizeView. The BWFC `LoginView` injects `AuthenticationStateProvider` natively and uses the same template names (`AnonymousTemplate`, `LoggedInTemplate`). The migration script handles this automatically.
- Master page → MainLayout.razor
- Scaffold generation (csproj, Program.cs, etc.)
- Route generation using **RelPath** for subdirectory pages (e.g., `Account/Login.aspx` → `@page "/Account/Login"`)
- SelectMethod/GetRouteUrl flagging
- Register directive cleanup
- RouteData → `[Parameter]` conversion with TODO comment on a **separate line** (fixed in Run 16)
- Enhanced navigation bypass (`data-enhance-nav="false"` on API links)
- ReadOnly attribute warnings
- Logout form → link conversion

**`-TestMode` switch:** Generates `ProjectReference` to local BWFC source instead of NuGet `PackageReference`, enabling rapid iteration during development runs:

```powershell
pwsh -File bwfc-migrate.ps1 -Path <source> -Output <target> -TestMode
```

#### Layer 2 — `bwfc-migrate-layer2.ps1`

The Layer 2 script targets three patterns of semantic transforms:

| Pattern | Target | Status (Run 16) |
|---------|--------|-----------------|
| **Pattern A** — Code-behinds | Page → ComponentBase + DI rewrite | ⚠️ Scaffolding automated, entity types need overlay |
| **Pattern B** — Auth forms | Login/Register form simplification | ❌ Detection needs improvement — manual overlay |
| **Pattern C** — Program.cs | Full .NET SSR bootstrap generation | ✅ Fully automated |

```powershell
pwsh -File bwfc-migrate-layer2.ps1 -Path <blazor-output-dir>
```

#### Manual Overlay (Layer 2 remainder)

Three persistent semantic gaps that require business-logic understanding:

1. **FormView SSR workaround** — `FormView.CurrentItem` doesn't trigger re-render in SSR; replace with direct `ComponentBase` + `IDbContextFactory` + `SupplyParameterFromQuery`
2. **Auth form model simplification** — complex `[SupplyParameterFromForm]` with nested models → individual string properties with explicit `name` attributes
3. **Program.cs application bootstrap** — Global.asax → .NET SSR middleware (Pattern C now automates this)

**Always manual (not scriptable):**
- EF6 → EF Core (models, DbContext, seed)
- Identity/Auth subsystem
- Session → scoped services
- Business logic (checkout, payment, admin CRUD)
- Complex data-binding with arithmetic/method chains

## Examples

### Preserving a ListView (CORRECT)

```razor
@* Web Forms *@
<asp:ListView ID="itemList" runat="server"
    DataKeyNames="ItemID" GroupItemCount="4"
    ItemType="YourApp.Models.YourEntity"
    SelectMethod="GetItems">
    <ItemTemplate>
        <td><%#: Item.Name %></td>
    </ItemTemplate>
</asp:ListView>

@* After migration (BWFC preserved) *@
<ListView Items="@_items" GroupItemCount="4">
    <ItemTemplate>
        <td>@context.Name</td>
    </ItemTemplate>
</ListView>

@code {
    [Inject] private AppDbContext Db { get; set; }
    private List<YourEntity> _items;

    protected override async Task OnInitializedAsync()
    {
        _items = await Db.Items.ToListAsync();
    }
}
```

### Preserving Event Handlers (CORRECT)

```razor
@* Web Forms *@
<asp:Button ID="btnRemove" runat="server" Text="Remove"
    OnClick="RemoveItem_Click" CommandArgument='<%# Item.ItemId %>' />

@* After migration (BWFC preserved) *@
<Button Text="Remove"
    OnClick="RemoveItem_Click" CommandArgument="@context.ItemId" />

@code {
    // Only signature changes — method name stays the same
    private async Task RemoveItem_Click(MouseEventArgs e) { ... }
}
```

## Anti-Patterns

### ❌ Replacing BWFC Data Controls with Raw HTML

```razor
@* WRONG — loses all BWFC functionality *@
@foreach (var item in _items)
{
    <tr>
        <td>@item.Name</td>
    </tr>
}

@* RIGHT — use BWFC ListView *@
<ListView Items="@_items">
    <ItemTemplate>
        <tr><td>@context.Name</td></tr>
    </ItemTemplate>
</ListView>
```

### ❌ Stripping Event Handler Attributes

```razor
@* WRONG — strips the handler, requires manual re-wiring *@
<Button Text="Submit" />
@* TODO: re-add click handler *@

@* RIGHT — preserve the attribute, only annotate signature change *@
<Button Text="Submit" OnClick="Submit_Click" />
@* TODO: Update Submit_Click signature: (object, EventArgs) → (MouseEventArgs) *@
```

### ❌ Using System.Web.UI.Page as Base Class

```csharp
// WRONG — Web Forms base class
public partial class ProductList : Page { }

// RIGHT — BWFC page base class (provides Page.Title, IsPostBack, etc.)
// Set via @inherits WebFormsPageBase in _Imports.razor
public partial class ProductList : WebFormsPageBase { }

// ALSO RIGHT — for non-page components
public partial class MyComponent : ComponentBase { }
```
