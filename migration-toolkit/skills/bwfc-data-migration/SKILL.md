---
name: bwfc-data-migration
description: "Migrate Web Forms data access and application architecture to Blazor Server. Covers Entity Framework 6 to EF Core, DataSource controls to service injection, Session state to scoped services, Global.asax to Program.cs, Web.config to appsettings.json, routing, HTTP handlers to middleware, and third-party integrations. Use for Layer 3 architecture decisions during Web Forms migration."
---

# Web Forms Data Access & Architecture Migration

This skill covers migrating Web Forms data access patterns and application architecture to Blazor Server. These are the **Layer 3 architecture decisions** that require project-specific judgment.

**Related skills:**
- `/bwfc-migration` — Core markup migration (controls, expressions, layouts)
- `/bwfc-identity-migration` — Authentication and authorization migration

---

## When to Use This Skill

Use this skill when you need to:
- Replace `SelectMethod`/`DataSource` controls with service injection
- Migrate Entity Framework 6 to EF Core
- Convert `Session`/`ViewState`/`Application` state to Blazor patterns
- Migrate `Global.asax` to `Program.cs`
- Convert `Web.config` to `appsettings.json`
- Replace HTTP Handlers/Modules with middleware
- Wire up third-party integrations

---

## ⚠️ Session State Under Interactive Server Mode

> **Note:** If you follow the recommended SSR (Static Server Rendering) architecture (see `/migration-standards`), most session/HttpContext issues are avoided because every page load is a real HTTP request. The guidance below applies when using global `InteractiveServer` mode.

> **CRITICAL:** When using `<Routes @rendermode="InteractiveServer" />` (global interactive server mode), `HttpContext.Session` is **NULL** during WebSocket rendering. Any code that accesses `HttpContext.Session` inside a Blazor component event handler or lifecycle method will throw a `NullReferenceException` or silently fail.

**Why this happens:** After the initial HTTP request establishes the SignalR circuit, Blazor communicates over WebSocket. There is no HTTP request/response — and therefore no session middleware processing — during component interactions.

**Options for session-dependent operations (shopping cart, user preferences, etc.):**

### Option A: Minimal API Endpoints (most reliable for HTTP-dependent state)

Use the same `<form method="post">` → minimal API pattern used for auth. The endpoint has a real `HttpContext` with session access.

```csharp
// Program.cs — state-modifying operation via minimal API
app.MapPost("/State/Update", async (HttpContext context, YourStateService stateService) =>
{
    var form = await context.Request.ReadFormAsync();
    if (int.TryParse(form["itemId"], out var itemId))
        stateService.AddItem(itemId);
    return Results.Redirect("/Items");
}).DisableAntiforgery();
```

```razor
@* Blazor page — form submits via HTTP POST, not a Blazor event *@
<form method="post" action="/State/Update">
    <input type="hidden" name="itemId" value="@item.ItemID" />
    <button type="submit">Add Item</button>
</form>
```

> **Example (e-commerce app):** Replace `Session["CartId"]` with a minimal API endpoint at `/Cart/Add` that accepts `productId` and delegates to a `CartService`. The endpoint redirects back to the shopping cart page after adding the item.

> **Important:** The endpoint MUST call `.DisableAntiforgery()` because Blazor's HTML rendering does not include antiforgery tokens. Example: `app.MapPost("/endpoint", handler).DisableAntiforgery();`

### Option B: Scoped Services (in-memory, per-circuit)

Replace `Session["key"]` with a scoped DI service. State lives in server memory for the duration of the SignalR circuit.

```csharp
// YourStateService.cs — registered as AddScoped<YourStateService>()
public class YourStateService
{
    public List<SelectedItem> Items { get; set; } = new();
    public void AddItem(int itemId) { /* ... */ }
}
```

> **Example (e-commerce app):** `Session["ShoppingCart"]` → scoped `CartService` with `ShoppingCart Cart` property and `AddItem(int productId)` method.

**Trade-off:** State is lost if the user refreshes the page or the circuit disconnects. Good for transient UI state, not for durable business data.

### Option C: Database-Backed State (most durable)

Store state in the database, keyed by user ID or a cookie-based session token. Survives circuit disconnects, page refreshes, and server restarts.

```csharp
public class StatePersistenceService(IDbContextFactory<AppDbContext> factory)
{
    public async Task AddItemAsync(string userId, int itemId)
    {
        using var db = factory.CreateDbContext();
        db.SavedItems.Add(new SavedItem { UserId = userId, ItemId = itemId });
        await db.SaveChangesAsync();
    }
}
```

> **Example (e-commerce app):** `CartService` backed by `IDbContextFactory<AppDbContext>` stores cart items in a `CartItems` table, keyed by user ID.

**Recommendation:** For business-critical state (shopping carts, form drafts, user selections), prefer Option A (minimal API) or Option C (database). Use Option B only for transient UI state that can be safely lost.

---

## 1. Entity Framework 6 → EF Core

> **⛔ CRITICAL: NEVER change the database provider!** If the source uses SQL Server LocalDB, the target uses SQL Server LocalDB. If the source uses SQL Server, the target uses SQL Server. Only migrate the EF API (EF6 → EF Core), NOT the underlying database technology.

**Web Forms:** EF6 with `DbContext` instantiated directly in code-behind or via `SelectMethod`.
**Blazor:** EF Core **10.0.3** (latest .NET 10) with `IDbContextFactory` registered in DI.

> **Always use the latest .NET 10 EF Core packages** (currently 10.0.3): `Microsoft.EntityFrameworkCore`, `.SqlServer` (if source used SQL Server), `.Tools`, `.Design`.

```csharp
// Web Forms — direct DbContext in code-behind
public IQueryable<YourEntity> GetItems()
{
    var db = new AppDbContext();
    return db.Items;
}
```

```csharp
// Blazor — Program.cs (KEEP the same database provider!)
// Source used LocalDB → Target uses LocalDB
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// ✅ UseSqlServer → UseSqlServer (CORRECT)
// ❌ UseSqlServer → UseSqlite (FORBIDDEN - don't change provider!)
```

```csharp
// Blazor — Service layer
public class EntityService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<YourEntity>> GetItemsAsync()
    {
        using var db = factory.CreateDbContext();
        return await db.Items.ToListAsync();
    }

    public async Task<YourEntity?> GetItemAsync(int id)
    {
        using var db = factory.CreateDbContext();
        return await db.Items.FindAsync(id);
    }
}
```

> **Critical:** Use `IDbContextFactory`, NOT `AddDbContext`, for Blazor Server. Blazor circuits are long-lived — a single `DbContext` accumulates stale data and tracking issues.

### EF6 → EF Core API Changes

| EF6 | EF Core | Notes |
|-----|---------|-------|
| `using System.Data.Entity;` | `using Microsoft.EntityFrameworkCore;` | Namespace change |
| `DbModelBuilder` in `OnModelCreating` | `ModelBuilder` | Same concepts, different API |
| `HasRequired()` / `HasOptional()` | Navigation properties + `IsRequired()` | Simpler relationship config |
| `Database.SetInitializer(...)` | `Database.EnsureCreated()` or Migrations | Different init strategy |
| `db.Products.Include("Category")` | `db.Products.Include(p => p.Category)` | Prefer lambda includes |
| `WillCascadeOnDelete(false)` | `.OnDelete(DeleteBehavior.Restrict)` | Cascade config |
| `.HasDatabaseGeneratedOption(...)` | `.ValueGeneratedOnAdd()` | Key generation |

### Connection String Migration

```xml
<!-- Web Forms — Web.config -->
<connectionStrings>
  <add name="DefaultConnection"
       connectionString="Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=MyApp;Integrated Security=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

```json
// Blazor — appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=MyApp;Integrated Security=True"
  }
}
```

---

## 2. DataSource Controls → Service Injection

Web Forms `DataSource` controls have **no BWFC equivalent**. Replace with injected services.

```xml
<!-- Web Forms — declarative data binding -->
<asp:SqlDataSource ID="ProductsDS" runat="server"
    ConnectionString="<%$ ConnectionStrings:DefaultConnection %>"
    SelectCommand="SELECT * FROM Products" />
<asp:GridView DataSourceID="ProductsDS" runat="server" />
```

```razor
@* Blazor — service injection *@
@inject IEntityService EntityService

<GridView Items="items" TItem="YourEntity" AutoGenerateColumns="true" />

@code {
    private List<YourEntity> items = new();

    protected override async Task OnInitializedAsync()
    {
        items = await EntityService.GetItemsAsync();
    }
}
```

### Service Registration Pattern

```csharp
// Program.cs
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IEntityService, EntityService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

### SelectMethod → Service Method Mapping

| Web Forms SelectMethod | Blazor Service Call |
|----------------------|---------------------|
| `SelectMethod="GetItems"` | `items = await EntityService.GetItemsAsync();` |
| `SelectMethod="GetItem"` | `item = await EntityService.GetItemAsync(id);` |
| `InsertMethod="InsertItem"` | `await EntityService.InsertAsync(item);` |
| `UpdateMethod="UpdateItem"` | `await EntityService.UpdateAsync(item);` |
| `DeleteMethod="DeleteItem"` | `await EntityService.DeleteAsync(id);` |

---

## 3. Session State → Scoped Services

**Web Forms:** `Session["key"]` dictionary accessed anywhere.
**Blazor:** Scoped services via DI. For browser persistence, use `ProtectedSessionStorage`.

```csharp
// Web Forms
Session["MyData"] = data;
var data = (MyDataType)Session["MyData"];
```

```csharp
// Blazor — Scoped service (in-memory, per-circuit)
public class MyDataService
{
    public MyDataType Data { get; set; } = new();
    public void Update(string key, object value) { ... }
    public T Get<T>(string key) { ... }
}

// Program.cs
builder.Services.AddScoped<MyDataService>();

// Component
@inject MyDataService DataService
```

> **Example (e-commerce app):** Replace `Session["ShoppingCart"]` with a scoped `CartService` holding a `ShoppingCart` object. Register as `AddScoped<CartService>()` and inject via `@inject CartService Cart`. The cart service exposes `AddItem`, `RemoveItem`, and `GetTotal` methods.

### State Storage Options

| Web Forms | Blazor Equivalent | Scope |
|-----------|------------------|-------|
| `Session["key"]` | Scoped service | Per-circuit (lost on disconnect) |
| `Session["key"]` (persistent) | `ProtectedSessionStorage` | Browser session tab |
| `Application["key"]` | Singleton service | App-wide |
| `Cache["key"]` | `IMemoryCache` or `IDistributedCache` | Configurable |
| `ViewState["key"]` | Component fields/properties | Per-component |
| `TempData["key"]` | `ProtectedSessionStorage` | One read |
| `Cookies` | `ProtectedLocalStorage` or HTTP endpoints | Browser |

### ProtectedSessionStorage Example

```razor
@inject ProtectedSessionStorage SessionStorage

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var result = await SessionStorage.GetAsync<MyDataType>("mydata");
            cart = result.Success ? result.Value! : new MyDataType();
        }
    }

    private async Task SaveCart()
    {
        await SessionStorage.SetAsync("mydata", cart);
    }
}
```

> **Note:** `ProtectedSessionStorage` only works after the first render (it requires JS interop). Always check in `OnAfterRenderAsync`, not `OnInitializedAsync`.

---

## 4. Global.asax → Program.cs

```csharp
// Web Forms — Global.asax
protected void Application_Start(object sender, EventArgs e)
{
    RouteConfig.RegisterRoutes(RouteTable.Routes);
    BundleConfig.RegisterBundles(BundleTable.Bundles);
}

protected void Application_Error(object sender, EventArgs e)
{
    var ex = Server.GetLastError();
    Logger.LogError(ex);
}

protected void Session_Start(object sender, EventArgs e)
{
    Session["UserState"] = new UserState();
}
```

```csharp
// Blazor — Program.cs
var builder = WebApplication.CreateBuilder(args);

// Services (replaces Application_Start registrations)
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddBlazorWebFormsComponents();
builder.Services.AddDbContextFactory<AppDbContext>(options => ...);
builder.Services.AddScoped<UserStateService>(); // replaces Session_Start

var app = builder.Build();

// Middleware pipeline
app.UseExceptionHandler("/Error"); // replaces Application_Error
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
```

### Global.asax Event → Blazor Equivalent

| Global.asax Event | Blazor Equivalent |
|-------------------|-------------------|
| `Application_Start` | `Program.cs` — service registration and app configuration |
| `Application_Error` | `app.UseExceptionHandler(...)` middleware |
| `Session_Start` | Scoped service constructor (lazy init) |
| `Session_End` | `IDisposable` on scoped services or circuit handler |
| `Application_BeginRequest` | Custom middleware |
| `Application_EndRequest` | Custom middleware |

---

## 5. Web.config → appsettings.json

```xml
<!-- Web Forms — Web.config -->
<appSettings>
  <add key="PayPal:Mode" value="sandbox" />
  <add key="MaxItemsPerPage" value="20" />
</appSettings>
```

```json
// Blazor — appsettings.json
{
  "PayPal": {
    "Mode": "sandbox"
  },
  "MaxItemsPerPage": 20
}
```

```csharp
// Web Forms access
var mode = ConfigurationManager.AppSettings["PayPal:Mode"];

// Blazor access — IConfiguration
@inject IConfiguration Config
var mode = Config["PayPal:Mode"];

// Blazor access — Options pattern (recommended)
builder.Services.Configure<PayPalOptions>(builder.Configuration.GetSection("PayPal"));
@inject IOptions<PayPalOptions> PayPalOptions
var mode = PayPalOptions.Value.Mode;
```

---

## 6. Route Table → @page Directives

```csharp
// Web Forms — RouteConfig.cs
routes.MapPageRoute("DetailRoute", "Items/{itemId}", "~/ItemDetail.aspx");
routes.MapPageRoute("ListRoute", "Categories/{categoryId}", "~/ItemList.aspx");
```

```razor
@* Blazor — ItemDetail.razor *@
@page "/Items/{ItemId:int}"
@code {
    [Parameter] public int ItemId { get; set; }
}

@* Blazor — ItemList.razor *@
@page "/Categories/{CategoryId:int}"
@code {
    [Parameter] public int CategoryId { get; set; }
}
```

### URL Pattern Conversion

| Web Forms Route Pattern | Blazor @page Pattern |
|------------------------|---------------------|
| `{id}` | `{Id:int}` (add type constraint) |
| `{name}` | `{Name}` (string, no constraint needed) |
| `{category}/{subcategory}` | `{Category}/{Subcategory}` |
| Optional: `{id?}` | `{Id:int?}` |
| Default: `{action=Index}` | Multiple `@page` directives |

### Friendly URLs

```csharp
// Web Forms — FriendlyUrls
routes.EnableFriendlyUrls();
// Maps Items.aspx → /Items, Items/Details/5 → Items.aspx?id=5

// Blazor — direct @page mapping
@page "/Items"
@page "/Items/Details/{Id:int}"
```

---

## 7. HTTP Handlers/Modules → Middleware

```csharp
// Web Forms — IHttpHandler
public class ImageHandler : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        var id = context.Request.QueryString["id"];
        // serve image
    }
    public bool IsReusable => true;
}
```

```csharp
// Blazor — Minimal API endpoint
app.MapGet("/api/images/{id}", async (int id, ImageService svc) =>
{
    var image = await svc.GetImageAsync(id);
    return Results.File(image.Data, image.ContentType);
});
```

```csharp
// Web Forms — IHttpModule
public class LoggingModule : IHttpModule
{
    public void Init(HttpApplication context)
    {
        context.BeginRequest += (s, e) => Log("Begin: " + context.Request.Url);
    }
}
```

```csharp
// Blazor — Middleware
app.Use(async (context, next) =>
{
    Log($"Begin: {context.Request.Path}");
    await next(context);
});
```

---

## 8. Third-Party Integrations → HttpClient

```csharp
// Web Forms — WebRequest/WebClient
var request = WebRequest.Create("https://api.paypal.com/v1/payments");
request.Method = "POST";
// ... manual serialization and error handling
```

```csharp
// Blazor — Program.cs
builder.Services.AddHttpClient("PayPal", client =>
{
    client.BaseAddress = new Uri("https://api.paypal.com/v1/");
});

// Blazor — Service
public class PayPalService(IHttpClientFactory factory)
{
    public async Task<PaymentResult> CreatePaymentAsync(Order order)
    {
        var client = factory.CreateClient("PayPal");
        var response = await client.PostAsJsonAsync("payments", order);
        return await response.Content.ReadFromJsonAsync<PaymentResult>()!;
    }
}
```

---

## Files to Create During Migration

| File | Purpose | Replaces |
|------|---------|----------|
| `Program.cs` | Service registration, middleware | `Global.asax`, `Startup.cs`, `RouteConfig.cs` |
| `appsettings.json` | Configuration | `Web.config` `<appSettings>` and `<connectionStrings>` |
| `App.razor` | Root component with Router | `Default.aspx` (entry point) |
| `_Imports.razor` | Global usings | `Web.config` `<namespaces>` |
| `Components/Layout/MainLayout.razor` | Application layout | `Site.Master` |
| `Components/Pages/*.razor` | Pages | `*.aspx` files |
| `Services/*.cs` | Data access services | `SelectMethod`s, DataSource controls, code-behind queries |
| `Models/*.cs` | Domain models | Copy from Web Forms project |

---

## Common Data Migration Gotchas

### DbContext Lifetime
Blazor Server circuits are long-lived. Always use `IDbContextFactory` and create short-lived `DbContext` instances per operation.

### No Page-Level Transaction Scope
Web Forms `SelectMethod` runs inside a page lifecycle. Blazor doesn't have this. Use explicit transaction scopes in services if needed:
```csharp
using var db = factory.CreateDbContext();
using var transaction = await db.Database.BeginTransactionAsync();
// ... operations
await transaction.CommitAsync();
```

### Async All the Way
Web Forms `SelectMethod` returns `IQueryable` synchronously. Blazor services should be async:
```csharp
// WRONG: return db.Products.ToList();
// RIGHT: return await db.Products.ToListAsync();
```

### No ConfigurationManager
`ConfigurationManager.AppSettings["key"]` doesn't exist. Inject `IConfiguration` or use the Options pattern.

### Static Helpers with HttpContext
Web Forms often has static helper classes that access `HttpContext.Current`. These must be refactored to accept dependencies via constructor injection.

---

## Blazor Enhanced Navigation

Blazor's **enhanced navigation** intercepts `<a href>` clicks and handles them as client-side SPA navigation. This is seamless for navigating between Blazor pages, but it **breaks links to minimal API endpoints** because the request never actually hits the server as an HTTP request.

### The Problem

```razor
@* ❌ BROKEN — Blazor intercepts the click, attempts client-side navigation *@
<a href="/AddToCart?productID=@product.ProductID">Add to Cart</a>

@* The user sees a blank page or "not found" because Blazor tries to render
   "/AddToCart" as a Razor component, but it's a minimal API endpoint *@
```

### Workaround Options

**Option 1: Use `<form method="post">` (Recommended)**

```razor
@* ✅ CORRECT — form POST is a full HTTP request, not intercepted by Blazor *@
<form method="post" action="/Cart/Add">
    <input type="hidden" name="productId" value="@product.ProductID" />
    <button type="submit" class="btn btn-primary">Add to Cart</button>
</form>
```

> **Important:** The endpoint MUST call `.DisableAntiforgery()` because Blazor's HTML rendering does not include antiforgery tokens. Example: `app.MapPost("/endpoint", handler).DisableAntiforgery();`

**Option 2: Add `data-enhance-nav="false"` to the link**

```razor
@* ✅ CORRECT — disables enhanced navigation for this specific link *@
<a href="/AddToCart?productID=@product.ProductID" data-enhance-nav="false">Add to Cart</a>
```

This tells Blazor to let the browser handle the navigation normally (full HTTP request).

**Option 3: JavaScript workaround**

```razor
@* ✅ Works — forces a full page navigation via JavaScript *@
<a href="/AddToCart?productID=@product.ProductID"
   onclick="window.location.href=this.href; return false;">Add to Cart</a>
```

### Which Workaround to Use

| Scenario | Recommended Approach |
|----------|---------------------|
| Auth operations (login/register/logout) | `<form method="post">` — always |
| Cart operations (add/remove/update) | `<form method="post">` — most reliable |
| Simple GET redirects to API endpoints | `data-enhance-nav="false"` on the `<a>` tag |
| Download links to file endpoints | `data-enhance-nav="false"` on the `<a>` tag |

> **Rule of thumb:** Any link that targets a minimal API endpoint (not a Blazor page) needs either `<form method="post">` or `data-enhance-nav="false"` to work correctly.
