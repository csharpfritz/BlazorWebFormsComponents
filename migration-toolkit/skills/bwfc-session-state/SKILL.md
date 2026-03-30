---
name: bwfc-session-state
description: "Migrate Web Forms Application state, Cache, and HttpContext.Current patterns to Blazor Server equivalents. Covers Application[\"key\"] → singleton services, Cache[\"key\"] → IMemoryCache, and HttpContext.Current → IHttpContextAccessor. WHEN: \"migrate Application state\", \"Cache to IMemoryCache\", \"HttpContext.Current migration\", \"session state patterns\", \"global application variables\"."
confidence: low
version: 1.0.0
---

# Web Forms Application State & HttpContext Migration

This skill covers migrating Web Forms application-level state patterns to Blazor Server equivalents. These are **Layer 2 (L2) contextual transforms** that require understanding the data's lifecycle and access patterns.

**Related skills:**
- `/bwfc-migration` — Core markup migration (controls, expressions, layouts)
- `/bwfc-data-migration` — EF6 → EF Core, Session state, architecture decisions
- `/bwfc-identity-migration` — Authentication and authorization migration

---

## When to Use This Skill

Use this skill when you encounter:
- `Application["key"]` dictionary access in code-behind
- `Cache["key"]` or `Cache.Insert()` / `Cache.Add()` calls
- `HttpContext.Current` static property access
- `Context.Application` or `Context.Cache` in pages or modules

These patterns represent **shared, application-wide state** that must be migrated to Blazor's dependency injection model.

---

## 1. Application State: `Application["key"]` → Singleton Service

### Web Forms Pattern

```csharp
// Web Forms — Global.asax.cs
protected void Application_Start(object sender, EventArgs e)
{
    Application["SiteTitle"] = "My Application";
    Application["StartTime"] = DateTime.UtcNow;
    Application["VisitorCount"] = 0;
}

// Web Forms — Page code-behind
protected void Page_Load(object sender, EventArgs e)
{
    var title = (string)Application["SiteTitle"];
    var count = (int)Application["VisitorCount"];
    Application["VisitorCount"] = count + 1;
}
```

### Blazor Pattern: Scoped or Singleton Service

**Decision:** Is the data truly application-wide (same for all users) or per-user?

#### If Application-Wide (Singleton):

```csharp
// Services/ApplicationStateService.cs
public class ApplicationStateService
{
    private readonly ConcurrentDictionary<string, object?> _state = new();
    
    public DateTime StartTime { get; } = DateTime.UtcNow;
    public string SiteTitle { get; set; } = "My Application";
    
    private int _visitorCount;
    public int VisitorCount => _visitorCount;
    public void IncrementVisitorCount() => Interlocked.Increment(ref _visitorCount);
    
    // For generic key-value access (preserve dictionary-style API):
    public object? this[string key]
    {
        get => _state.TryGetValue(key, out var value) ? value : null;
        set => _state[key] = value;
    }
}

// Program.cs — register as singleton
builder.Services.AddSingleton<ApplicationStateService>();
```

```csharp
// Blazor component or page
@inject ApplicationStateService AppState

@code {
    protected override void OnInitialized()
    {
        var title = AppState.SiteTitle;
        AppState.IncrementVisitorCount();
        
        // Or dictionary-style:
        var startTime = (DateTime?)AppState["StartTime"];
    }
}
```

#### If Per-User State (Scoped):

> **Warning:** Don't use singleton services for per-user data. Use scoped services instead.

```csharp
// Services/UserStateService.cs
public class UserStateService
{
    // Per-user data lives in server memory for the circuit duration
    public Dictionary<string, object?> Data { get; } = new();
}

// Program.cs — register as scoped (per-circuit)
builder.Services.AddScoped<UserStateService>();
```

---

## 2. Cache: `Cache["key"]` → IMemoryCache

### Web Forms Pattern

```csharp
// Web Forms — Page code-behind
protected void Page_Load(object sender, EventArgs e)
{
    var products = (List<Product>)Cache["ProductList"];
    if (products == null)
    {
        products = GetProductsFromDatabase();
        Cache.Insert("ProductList", products, null, DateTime.Now.AddMinutes(20), TimeSpan.Zero);
    }
}

// Or with dependencies:
Cache.Insert("ProductList", products, new CacheDependency("~/App_Data/products.xml"), 
    DateTime.MaxValue, TimeSpan.Zero);
```

### Blazor Pattern: IMemoryCache

```csharp
// Services/ProductService.cs
using Microsoft.Extensions.Caching.Memory;

public class ProductService
{
    private readonly IMemoryCache _cache;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    
    public ProductService(IMemoryCache cache, IDbContextFactory<AppDbContext> dbFactory)
    {
        _cache = cache;
        _dbFactory = dbFactory;
    }
    
    public async Task<List<Product>> GetProductsAsync()
    {
        // Try get from cache first
        if (_cache.TryGetValue("ProductList", out List<Product>? cachedProducts))
        {
            return cachedProducts!;
        }
        
        // Cache miss — load from database
        using var db = _dbFactory.CreateDbContext();
        var products = await db.Products.ToListAsync();
        
        // Store in cache with absolute expiration
        _cache.Set("ProductList", products, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20),
            SlidingExpiration = null
        });
        
        return products;
    }
}

// Program.cs — IMemoryCache is registered by default, but ensure it's there:
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ProductService>();
```

### Cache Pattern Mapping

| Web Forms Cache API | Blazor IMemoryCache API |
|---------------------|------------------------|
| `Cache["key"]` | `_cache.TryGetValue(key, out var value)` |
| `Cache.Insert(key, value, ...)` | `_cache.Set(key, value, options)` |
| `Cache.Add(key, value, ...)` | `_cache.GetOrCreate(key, entry => { ... })` |
| `Cache.Remove(key)` | `_cache.Remove(key)` |
| Absolute expiration | `options.AbsoluteExpirationRelativeToNow` |
| Sliding expiration | `options.SlidingExpiration` |
| `CacheDependency` (file) | Use file system watcher + `_cache.Remove()` |

### File-Based Cache Invalidation

Web Forms `CacheDependency` on files has no direct equivalent. Use `FileSystemWatcher`:

```csharp
public class ProductService
{
    private readonly IMemoryCache _cache;
    private readonly FileSystemWatcher _watcher;
    
    public ProductService(IMemoryCache cache, IWebHostEnvironment env)
    {
        _cache = cache;
        
        // Watch for changes to products.xml
        _watcher = new FileSystemWatcher(Path.Combine(env.ContentRootPath, "App_Data"));
        _watcher.Filter = "products.xml";
        _watcher.Changed += (s, e) => _cache.Remove("ProductList");
        _watcher.EnableRaisingEvents = true;
    }
}
```

---

## 3. HttpContext.Current → IHttpContextAccessor

### Web Forms Pattern

```csharp
// Web Forms — anywhere in code (static access)
var userName = HttpContext.Current.User.Identity.Name;
var isAuthenticated = HttpContext.Current.User.Identity.IsAuthenticated;
var ipAddress = HttpContext.Current.Request.UserHostAddress;
var requestUrl = HttpContext.Current.Request.Url.ToString();
```

### Blazor Pattern: IHttpContextAccessor Injection

> **Critical:** `HttpContext` is **null** during WebSocket circuits (interactive server mode). Only use `IHttpContextAccessor` in scenarios where you know an HTTP request is active (initial render, HTTP endpoints, middleware).

```csharp
// Blazor component — inject accessor
@inject IHttpContextAccessor HttpContextAccessor

@code {
    protected override void OnInitialized()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var userName = httpContext.User.Identity?.Name;
            var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        }
        else
        {
            // WebSocket circuit — HttpContext is null
            // Use AuthenticationStateProvider for user info instead
        }
    }
}
```

### User Authentication in Components: Use AuthenticationStateProvider

For user identity in Blazor components, **do not use HttpContext** — use `AuthenticationStateProvider` instead:

```csharp
// Blazor component — proper pattern for auth
@inject AuthenticationStateProvider AuthStateProvider

@code {
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            var userName = user.Identity.Name;
            var isAdmin = user.IsInRole("Administrator");
        }
    }
}
```

### HttpContext Access Pattern Decision Tree

```
Is the code in a component/page?
├─ YES → Use AuthenticationStateProvider for user info
├─ YES → HttpContextAccessor only for initial render (SSR)
└─ NO → Is it in middleware/HTTP endpoint?
    ├─ YES → HttpContext is available directly (no accessor needed)
    └─ NO → Is it in a service?
        ├─ YES → Inject IHttpContextAccessor, check for null
        └─ NO → Refactor to accept HttpContext as parameter
```

---

## Common Migration Patterns

### Pattern 1: Application["SiteConfig"] Dictionary

```csharp
// Web Forms — Global.asax
Application["MaxUploadSize"] = 5242880; // 5MB
Application["SupportEmail"] = "support@example.com";

// Blazor — Singleton service + Configuration
public class SiteConfigService
{
    public int MaxUploadSize { get; }
    public string SupportEmail { get; }
    
    public SiteConfigService(IConfiguration config)
    {
        MaxUploadSize = config.GetValue<int>("MaxUploadSize", 5242880);
        SupportEmail = config["SupportEmail"] ?? "support@example.com";
    }
}

// Program.cs
builder.Services.AddSingleton<SiteConfigService>();

// appsettings.json
{
  "MaxUploadSize": 5242880,
  "SupportEmail": "support@example.com"
}
```

### Pattern 2: Visitor Counter (Singleton)

```csharp
// Web Forms
Application["VisitorCount"] = (int)Application["VisitorCount"] + 1;

// Blazor — Thread-safe singleton
public class VisitorCounterService
{
    private int _count;
    public int Count => _count;
    public void Increment() => Interlocked.Increment(ref _count);
}

// Program.cs
builder.Services.AddSingleton<VisitorCounterService>();
```

### Pattern 3: Cached Data with Refresh

```csharp
// Web Forms
if (Cache["Categories"] == null || Request.QueryString["refresh"] == "true")
{
    Cache["Categories"] = LoadCategories();
}

// Blazor
public async Task<List<Category>> GetCategoriesAsync(bool forceRefresh = false)
{
    if (forceRefresh)
    {
        _cache.Remove("Categories");
    }
    
    return await _cache.GetOrCreateAsync("Categories", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        return await LoadCategoriesAsync();
    });
}
```

---

## Known Failure Modes

### 1. HttpContext.Current in Background Tasks

**Problem:** Background tasks (hosted services, timers) have no HTTP context.

```csharp
// WRONG — throws NullReferenceException
Task.Run(() => {
    var user = HttpContext.Current.User; // NULL!
});
```

**Solution:** Pass necessary data explicitly:

```csharp
// RIGHT
var userName = User.Identity?.Name;
Task.Run(() => {
    ProcessData(userName);
});
```

### 2. Application State in Distributed Scenarios

**Problem:** Singleton services are per-server instance. If you scale to multiple servers, each has its own copy.

**Solution:** Use distributed cache (`IDistributedCache` with Redis) instead of `IMemoryCache` for data that must be consistent across servers.

### 3. Cache Items Larger Than Memory

**Problem:** `IMemoryCache` stores in server RAM. Large cached datasets can cause OOM.

**Solution:** Use distributed cache or database-backed cache for large datasets.

---

## What Developers Must Do Manually

1. **Decide between singleton and scoped** — The skill can't know if `Application["key"]` data is truly global or per-user.
2. **Distributed cache for scale** — If the app will run on multiple servers, convert to `IDistributedCache`.
3. **File-based cache dependencies** — Requires custom `FileSystemWatcher` setup (no automatic conversion).
4. **HttpContext access in services** — Review null checks and decide if `IHttpContextAccessor` is appropriate or if refactoring is needed.

---

## L1 Script Support (Future Enhancement)

The L1 PowerShell script could detect these patterns and add guidance comments:

```csharp
// TODO: BWFC — Application["SiteTitle"] detected.
//       Migrate to singleton service (ApplicationStateService) if data is global,
//       or scoped service if data is per-user.
//       See bwfc-session-state skill for examples.
var title = Application["SiteTitle"];

// TODO: BWFC — Cache["ProductList"] detected.
//       Inject IMemoryCache and use _cache.GetOrCreate() pattern.
//       See bwfc-session-state skill for cache migration patterns.
var products = Cache["ProductList"];

// TODO: BWFC — HttpContext.Current detected.
//       In components: use AuthenticationStateProvider for user info.
//       In services: inject IHttpContextAccessor (check for null in WebSocket circuits).
//       See bwfc-session-state skill for decision tree.
var user = HttpContext.Current.User;
```

---

## References

- [ASP.NET Core memory cache](https://learn.microsoft.com/aspnet/core/performance/caching/memory)
- [Distributed caching](https://learn.microsoft.com/aspnet/core/performance/caching/distributed)
- [IHttpContextAccessor in services](https://learn.microsoft.com/aspnet/core/fundamentals/http-context#ihttpcontextaccessor)
- [Blazor authentication state](https://learn.microsoft.com/aspnet/core/blazor/security/#authenticationstateprovider-service)
