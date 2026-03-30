# Code Transformation Rules

Patterns for migrating Web Forms code-behind, data binding, and master pages to Blazor equivalents.

**Parent skill:** `SKILL.md` (bwfc-migration)

---

## Code-Behind Migration

### Lifecycle Methods

| Web Forms | Blazor | Notes |
|-----------|--------|-------|
| `Page_Load(object sender, EventArgs e)` | `protected override async Task OnInitializedAsync()` | First load |
| `Page_PreRender(...)` | `protected override async Task OnParametersSetAsync()` | Before each render |
| `Page_Init(...)` | `protected override void OnInitialized()` | Sync initialization |
| `if (!IsPostBack)` | **L1 auto-unwraps** simple guards; works unchanged via `WebFormsPageBase` if left in | Always enters the block — correct for first-render code |
| `if (IsPostBack)` (without `!`) | **Dead code — flag for manual review** | Never enters the block in Blazor; move logic to event handlers |

```csharp
// Web Forms
protected void Page_Load(object sender, EventArgs e)
{
    if (!IsPostBack)
    {
        products = GetProducts();
        GridView1.DataBind();
    }
}

// After L1 (simple guard auto-unwrapped):
protected void Page_Load(object sender, EventArgs e)
{
    // BWFC: IsPostBack guard unwrapped — Blazor re-renders on every state change
    products = GetProducts();
    GridView1.DataBind();
}

// After L2 (Blazor lifecycle):
protected override async Task OnInitializedAsync()
{
    products = await ProductService.GetProductsAsync();
}
```

#### IsPostBack Guard Handling (L1 Automated)

The L1 script (`bwfc-migrate.ps1`) applies `Remove-IsPostBackGuards` to every code-behind file:

**Simple guards** (no `else` clause): The `if (!IsPostBack)` / `if (!Page.IsPostBack)` / `if (!this.IsPostBack)` / `if (IsPostBack == false)` wrapper is removed and the body is extracted and dedented. A comment replaces the guard:

```csharp
// BWFC: IsPostBack guard unwrapped — Blazor re-renders on every state change
LoadCategories();
BindGrid();
```

**Complex guards** (with `else` clause): A TODO comment is inserted above the guard for manual review:

```csharp
// TODO: BWFC — IsPostBack guard with else clause. In Blazor, OnInitializedAsync runs once (no postback).
// Review: move 'if' body to OnInitializedAsync and 'else' body to an event handler or remove.
if (!IsPostBack)
{
    LoadInitialData();
}
else
{
    ProcessPostBackData();
}
```

**Single-statement guards** (no braces): Flagged with a TODO comment — the script does not attempt to parse braceless `if` statements.

**Layer 2 action:** After L1, convert `Page_Load` → `OnInitializedAsync` and move the unwrapped body into the async method. For complex guards, move the `if` body into `OnInitializedAsync` and the `else` body into event handlers or remove it.

### Event Handlers

```csharp
// Web Forms
protected void SubmitBtn_Click(object sender, EventArgs e)
{
    Response.Redirect("~/Confirmation");
}

// Blazor — no sender/EventArgs parameters
private void SubmitBtn_Click()
{
    NavigationManager.NavigateTo("/Confirmation");
}
```

### Navigation

| Web Forms | Blazor |
|-----------|--------|
| `Response.Redirect("~/path")` | `NavigationManager.NavigateTo("/path")` |
| `Response.RedirectToRoute(...)` | `NavigationManager.NavigateTo($"/path/{param}")` |
| `Server.Transfer("~/page.aspx")` | `NavigationManager.NavigateTo("/page")` |

### `.aspx` URL Cleanup (L1 Automated)

The L1 script automatically rewrites `.aspx` URL string literals in code-behind files. This runs after `Response.Redirect` → `NavigationManager.NavigateTo` conversion.

| Pattern | Before | After |
|---------|--------|-------|
| Tilde + query string | `"~/Products.aspx?id=5"` | `"/Products?id=5"` |
| Tilde, no query | `"~/Products.aspx"` | `"/Products"` |
| Relative in NavigateTo + query | `NavigationManager.NavigateTo("Products.aspx?q=x")` | `NavigationManager.NavigateTo("/Products?q=x")` |
| Relative in NavigateTo | `NavigationManager.NavigateTo("Products.aspx")` | `NavigationManager.NavigateTo("/Products")` |

> **Note:** `.aspx` references in markup (href attributes) are preserved — `AspxRewriteMiddleware` can handle those at runtime. The L1 URL cleanup targets **code-behind string literals** only.

### Query String / Route Parameters

```csharp
// Web Forms (Model Binding)
public IQueryable<Product> GetProducts([QueryString] int? categoryId) { ... }

// Blazor
[SupplyParameterFromQuery] public int? CategoryId { get; set; }
```

```csharp
// Web Forms (RouteData)
public void GetProduct([RouteData] int productId) { ... }

// Blazor
@page "/Products/{ProductId:int}"
[Parameter] public int ProductId { get; set; }
```

---

## Data Binding Migration

### Collection-Bound Controls

For GridView, ListView, Repeater, DataList, DataGrid:

| Web Forms Pattern | BWFC Pattern |
|-------------------|-------------|
| `SelectMethod="GetProducts"` | `SelectMethod="@productService.GetProducts"` (convert string to `SelectHandler<ItemType>` delegate — BWFC auto-populates `Items`) |
| `ItemType="Namespace.Product"` | `ItemType="Product"` (strip namespace only) |
| `DataSource=<%# GetItems() %>` + `DataBind()` | `Items="items"` |
| `DataKeyNames="ProductID"` | `DataKeyNames="ProductID"` (preserved) |

> **How SelectMethod works in BWFC:** `DataBoundComponent<ItemType>` has a `SelectMethod` parameter of type `SelectHandler<ItemType>` — a delegate with signature `(int maxRows, int startRowIndex, string sortByExpression, out int totalRowCount) → IQueryable<ItemType>`. When set, `OnAfterRenderAsync` automatically calls the delegate to populate `Items`. This mirrors how Web Forms `SelectMethod` worked.

### Single-Record Controls

For FormView, DetailsView:

| Web Forms Pattern | BWFC Pattern |
|-------------------|-------------|
| `SelectMethod="GetProduct"` | `SelectMethod="@productService.GetProduct"` (convert string to delegate) or `DataItem="product"` (load in `OnInitializedAsync`) |
| `ItemType="Namespace.Product"` | `ItemType="Product"` (strip namespace only) |

### Template Binding

| Web Forms | BWFC | Notes |
|-----------|------|-------|
| `<%# Item.Name %>` | `@Item.Name` | Add `Context="Item"` to template element |
| `<%# Eval("Name") %>` | `@Item.Name` | Direct property access replaces reflection |
| `<%# Bind("Name") %>` | `@bind-Value="Item.Name"` | Two-way in edit templates |

---

## Master Page → Layout Migration

### Web Forms Master Page

```html
<%@ Master Language="C#" CodeBehind="Site.master.cs" Inherits="MyApp.SiteMaster" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title><%: Page.Title %></title>
    <asp:ContentPlaceHolder ID="HeadContent" runat="server" />
</head>
<body>
    <form runat="server">
        <asp:ScriptManager runat="server" />
        <header>
            <nav><asp:Menu ID="MainMenu" runat="server" ... /></nav>
        </header>
        <main>
            <asp:ContentPlaceHolder ID="MainContent" runat="server" />
        </main>
        <footer>© <%: DateTime.Now.Year %></footer>
    </form>
</body>
</html>
```

### Blazor Layout Equivalent

```razor
@inherits LayoutComponentBase

<header>
    <nav><Menu ... /></nav>
</header>
<main>
    @Body
</main>
<footer>© @DateTime.Now.Year</footer>
```

**Key changes:**
- `<%@ Master %>` → `@inherits LayoutComponentBase`
- `<form runat="server">` → replaced with `<div>` (preserves `id` attribute and CSS block formatting context)
- `<asp:ContentPlaceHolder ID="MainContent">` → `@Body`
- `<asp:ScriptManager>` → `<ScriptManager />` (renders nothing)
- CSS `<link>` elements from master page `<head>` → `App.razor` `<head>` section (relative `href` paths must be rewritten to absolute, e.g., `CSS/style.css` → `/CSS/style.css`, because `<HeadContent>` resolves from the page URL)
- `<head runat="server">` content → `<HeadContent>` in layout or `App.razor`

> **Alternative:** For a more gradual migration, BWFC provides `<MasterPage>`, `<Content>`, and `<ContentPlaceHolder>` components that preserve Web Forms-style markup. Use these as a stepping stone, then refactor to native Blazor layouts when ready.

> **Tip:** Use `<WebFormsPage>@Body</WebFormsPage>` as the layout wrapper instead of plain `@Body` to get NamingContainer (ID scoping), theming, and head rendering in one component.

### Nested Master Pages → Nested Layouts

```razor
@inherits LayoutComponentBase
@layout MainLayout

<div class="child-wrapper">
    @Body
</div>
```

---

## Phase 2 Transforms

Phase 2 (Layer 2, Copilot-assisted) converts compile-compatible code into functionally correct Blazor code.

### Page Lifecycle Method Transforms

Convert Web Forms lifecycle methods to Blazor equivalents. Apply **after** Phase 1 has unwrapped IsPostBack guards.

**Page_Load → OnInitializedAsync:**

```csharp
// Before (Phase 1 output):
protected void Page_Load(object sender, EventArgs e)
{
    categories = GetCategories();
    BindGrid();
}

// After (Phase 2):
protected override async Task OnInitializedAsync()
{
    categories = await GetCategoriesAsync();
    // DataBound controls auto-bind via SelectMethod — remove explicit BindGrid()
}
```

**Page_Init → OnInitialized:**

```csharp
// Before:
protected void Page_Init(object sender, EventArgs e)
{
    theme = "Default";
    ViewState["Initialized"] = true;
}

// After:
protected override void OnInitialized()
{
    theme = "Default";
    // ViewState removed — use component fields instead
}
```

**Page_PreRender → OnAfterRenderAsync:**

```csharp
// Before:
protected void Page_PreRender(object sender, EventArgs e)
{
    lblTotal.Text = cart.GetTotal().ToString("C");
}

// After:
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        lblTotal.Text = cart.GetTotal().ToString("C");
        StateHasChanged();
    }
}
```

> **Key differences:** `OnAfterRenderAsync` runs *after* render (not before), so UI updates require `StateHasChanged()`. Guard with `if (firstRender)` to avoid infinite render loops.

### Event Handler Signature Transforms

Strip **standard** `EventArgs` (carries no data). Preserve **specialized** EventArgs (carries command data, selection data, etc.).

**Standard EventArgs — strip both parameters:**

```csharp
// Before:
protected void SaveBtn_Click(object sender, EventArgs e)
{
    SaveRecord();
}

// After:
private void SaveBtn_Click()
{
    SaveRecord();
}
```

**Specialized EventArgs — keep the BWFC equivalent:**

```csharp
// Before:
protected void ProductList_ItemCommand(object sender, ListViewCommandEventArgs e)
{
    if (e.CommandName == "AddToCart")
    {
        var productId = e.CommandArgument.ToString();
        AddToCart(productId);
    }
}

// After:
private void ProductList_ItemCommand(CommandEventArgs e)
{
    if (e.CommandName == "AddToCart")
    {
        var productId = e.CommandArgument.ToString();
        AddToCart(productId);
    }
}
```

**Decision table:**

| Original Parameter Type | Action | BWFC Equivalent |
|---|---|---|
| `EventArgs` | **Strip** both params | No parameter |
| `CommandEventArgs` / `GridViewCommandEventArgs` / `ListViewCommandEventArgs` | **Keep** — map to BWFC type | `CommandEventArgs` |
| `GridViewEditEventArgs` / `GridViewDeleteEventArgs` | **Keep** — map to BWFC type | Check BWFC component API |
| `RepeaterCommandEventArgs` | **Keep** — map to BWFC type | `CommandEventArgs` |

## Phase 3 Transforms

Phase 3 automates two critical transforms that previously required manual intervention: event handler wiring in markup and DataSource/DataBind pattern conversion.

### Event Handler Wiring (Markup)

L1 script adds `@` prefix to event handler attributes so Razor compiles them as method references:

```html
<!-- Before (Phase 1 output): -->
<Button id="btnSave" Text="Save" OnClick="Save_Click" />
<GridView id="gvItems" OnRowCommand="Grid_RowCommand" />

<!-- After (Phase 3): -->
<Button id="btnSave" Text="Save" OnClick="@Save_Click" />
<GridView id="gvItems" OnRowCommand="@Grid_RowCommand" />
```

**Pattern:** `On[A-Z]*="MethodName"` → `On[A-Z]*="@MethodName"`

### DataBind Pattern Conversion (Cross-File)

L1 script detects `DataSource`/`DataBind()` in code-behind and correlates with markup:

**Code-behind transform:**

```csharp
// Before:
gvProducts.DataSource = GetProducts();
gvProducts.DataBind();

// After:
private IEnumerable<object> _gvProductsData;
// In OnInitializedAsync:
_gvProductsData = GetProducts();
```

**Markup transform (correlated by control ID):**

```html
<!-- Before: -->
<GridView id="gvProducts" />

<!-- After: -->
<GridView id="gvProducts" Items="@_gvProductsData" />
```

**Field naming:** `controlName` → `_controlNameData` (lowercase first char, append `Data`)

**Manual follow-up:** Change `IEnumerable<object>` to the actual typed collection (e.g., `IEnumerable<Product>`).

---

## Phase 4: Skills-Based Transforms (L2 — AI-Guided)

**Strategic Decision:** As of Phase 3, the L1 PowerShell script (`bwfc-migrate.ps1`) is **frozen**. It handles ~70% of migration work — the deterministic, pattern-based transforms that regex and AST manipulation can reliably automate. Everything remaining requires **contextual reasoning** — understanding data flow, application architecture, and developer intent. These transforms are now handled by **Copilot skills** that guide Layer 2 (L2) manual migrations.

### Why L1 is Frozen

Deterministic transforms have reached their practical limit. The remaining migration gaps fall into three categories that L1 cannot reliably handle:

1. **Architecture decisions** — Is `Application["key"]` global state or per-user? Should `Cache["key"]` use `IMemoryCache` or `IDistributedCache`? Is this HttpModule a logging module or authentication logic?
2. **Cross-file reasoning** — Converting `FindControl("id")` requires understanding the component tree. Mapping `Global.asax` events to `Program.cs` requires analyzing startup logic flow.
3. **Domain knowledge** — Migrating Membership provider passwords requires understanding hash compatibility. Converting `Session_Start` logic requires knowing Blazor circuit lifetime.

L1 can *detect* these patterns and flag them. But automating the conversion risks incorrect migrations that developers will spend hours debugging.

### Phase 4 Skills

Each skill provides:
- **When to apply** — Trigger patterns for Copilot to detect
- **Before/after examples** — Concrete code samples for each pattern
- **Decision trees** — Guidance for context-dependent choices
- **Common gotchas** — Known failure modes to avoid

| Skill | Covers | Related Items |
|-------|--------|---------------|
| **bwfc-session-state** | `Application["key"]` → singleton services, `Cache["key"]` → IMemoryCache, `HttpContext.Current` → IHttpContextAccessor | #13, #14, #15, #16 |
| **bwfc-middleware-migration** | HttpModule → middleware, Global.asax events → Program.cs | #22, #23, #24 |
| **bwfc-usercontrol-migration** | .ascx → component with [Parameter], FindControl → @ref patterns | #30, #31, #8 |
| **bwfc-identity-migration** (enhanced) | FormsAuthentication → cookie auth, Membership → Identity, Roles provider → policy-based authz | #25, #26, #27 |

### Using Phase 4 Skills

**For developers:**
1. Run L1 script to complete Phase 1–3 transforms
2. Review TODO comments flagged by L1 for manual work
3. Invoke `/bwfc-session-state`, `/bwfc-middleware-migration`, `/bwfc-usercontrol-migration`, or `/bwfc-identity-migration` skills in Copilot based on the pattern
4. Follow the skill's guidance for contextual transforms

**For AI agents:**
- Phase 4 skills are invoked during Layer 2 migration when Copilot detects specific patterns (e.g., `Application["key"]`, `IHttpModule`, `.ascx` files, `FormsAuthentication`)
- Skills provide decision trees and examples for context-dependent transforms
- Skills document "What Developers Must Do Manually" for tasks that cannot be automated

### L1 vs L2 Decision Table

| Transform | L1 (Deterministic) | L2 (Skill-Guided) | Reason |
|-----------|-------------------|------------------|--------|
| `asp:Button` → `<Button>` | ✅ L1 | | Regex-based, no context needed |
| `Response.Redirect` → `NavigationManager.NavigateTo` | ✅ L1 | | String literal replacement |
| `Application["key"]` detection | ✅ L1 (flag) | ✅ L2 (convert) | Requires deciding singleton vs scoped |
| `Cache["key"]` → `IMemoryCache` | ✅ L1 (flag) | ✅ L2 (convert) | Requires understanding cache lifetime |
| `Global.asax` event → `Program.cs` | ✅ L1 (flag) | ✅ L2 (convert) | Requires understanding startup logic flow |
| `FindControl("id")` → `@ref` | ✅ L1 (flag) | ✅ L2 (convert) | Requires understanding component tree |
| `.ascx` public property → `[Parameter]` | ✅ L1 (flag) | ✅ L2 (convert) | Requires understanding parent/child communication |

---

## Summary: Three-Layer Migration Strategy

1. **Layer 1 (L1) — PowerShell Script (`bwfc-migrate.ps1`):** Automated, deterministic transforms. Handles ~70% of migration work. **Frozen at Phase 3.**
2. **Layer 2 (L2) — Copilot Skills:** AI-guided, contextual transforms. Provides decision trees and examples for manual work. **Active development (Phase 4).**
3. **Layer 3 (L3) — Developer Judgment:** Project-specific architecture decisions that no skill can automate (e.g., choosing a database provider, designing service layer). Always requires human review.

