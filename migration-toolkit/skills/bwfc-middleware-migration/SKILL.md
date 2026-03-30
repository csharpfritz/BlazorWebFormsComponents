---
name: bwfc-middleware-migration
description: "Migrate Web Forms HttpModule and Global.asax application events to ASP.NET Core middleware and Program.cs. Covers HttpModule → middleware patterns, Global.asax event mapping (Application_Start, Session_Start, Application_Error), and pipeline registration order. WHEN: \"migrate HttpModule\", \"Global.asax to Program.cs\", \"Application_Start migration\", \"convert IHttpModule\", \"application event handlers\"."
confidence: low
version: 1.0.0
---

# Web Forms HttpModule & Global.asax → Blazor Middleware

This skill covers migrating Web Forms HTTP pipeline customization patterns to ASP.NET Core middleware. These are **Layer 2 (L2) contextual transforms** that require understanding request processing logic and application lifecycle events.

**Related skills:**
- `/bwfc-migration` — Core markup migration (controls, expressions, layouts)
- `/bwfc-data-migration` — Architecture decisions, EF6 → EF Core
- `/bwfc-identity-migration` — Authentication and authorization migration

---

## When to Use This Skill

Use this skill when you encounter:
- Classes implementing `IHttpModule` in the Web Forms project
- `<httpModules>` or `<modules>` sections in `web.config`
- `Global.asax` / `Global.asax.cs` files with application event handlers
- `Application_Start`, `Application_End`, `Application_BeginRequest`, `Application_Error`, `Session_Start`, etc.

---

## 1. HttpModule → ASP.NET Core Middleware

### Web Forms Pattern

```csharp
// Web Forms — CustomHeaderModule.cs
public class CustomHeaderModule : IHttpModule
{
    public void Init(HttpApplication context)
    {
        context.BeginRequest += OnBeginRequest;
        context.EndRequest += OnEndRequest;
        context.AuthenticateRequest += OnAuthenticateRequest;
    }
    
    private void OnBeginRequest(object sender, EventArgs e)
    {
        var app = (HttpApplication)sender;
        app.Context.Response.Headers.Add("X-Custom-Header", "MyValue");
    }
    
    private void OnEndRequest(object sender, EventArgs e)
    {
        var app = (HttpApplication)sender;
        // Log request completion
    }
    
    private void OnAuthenticateRequest(object sender, EventArgs e)
    {
        var app = (HttpApplication)sender;
        // Custom authentication logic
    }
    
    public void Dispose() { }
}

// Web Forms — web.config
<system.webServer>
  <modules>
    <add name="CustomHeader" type="MyApp.CustomHeaderModule" />
  </modules>
</system.webServer>
```

### Blazor Pattern: Middleware Class

```csharp
// Blazor — Middleware/CustomHeaderMiddleware.cs
public class CustomHeaderMiddleware
{
    private readonly RequestDelegate _next;
    
    public CustomHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // BeginRequest equivalent — runs BEFORE downstream middleware
        context.Response.Headers.Add("X-Custom-Header", "MyValue");
        
        // Call next middleware in pipeline
        await _next(context);
        
        // EndRequest equivalent — runs AFTER downstream middleware
        // Log request completion
    }
}

// Extension method for registration
public static class CustomHeaderMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomHeader(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CustomHeaderMiddleware>();
    }
}

// Program.cs — Register in pipeline
var app = builder.Build();

app.UseCustomHeader(); // ← Add middleware here
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorComponents<App>();
```

### HttpModule Event → Middleware Position Mapping

| Web Forms Event | ASP.NET Core Middleware Position | Notes |
|-----------------|----------------------------------|-------|
| `BeginRequest` | Before `await _next(context)` | First code in `InvokeAsync()` |
| `AuthenticateRequest` | Before `UseAuthentication()` | Register custom auth middleware before standard auth |
| `AuthorizeRequest` | Before `UseAuthorization()` | Register custom authz middleware before standard authz |
| `ResolveRequestCache` | Before routing | Output caching — use `UseOutputCache()` |
| `AcquireRequestState` | After `UseAuthentication()` | Session state loading |
| `PreRequestHandlerExecute` | Before `MapRazorComponents()` | Last chance to modify request |
| `PostRequestHandlerExecute` | After `MapRazorComponents()` | Response has been generated |
| `EndRequest` | After `await _next(context)` | Last code in `InvokeAsync()` |
| `Error` | Use `UseExceptionHandler()` | See Application_Error pattern below |

---

## 2. Global.asax Events → Program.cs

### Web Forms Pattern

```csharp
// Web Forms — Global.asax.cs
public class Global : HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        // Application initialization
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
        
        // Register services
        var container = new UnityContainer();
        container.RegisterType<IProductService, ProductService>();
        
        // Load application state
        Application["SiteTitle"] = ConfigurationManager.AppSettings["SiteTitle"];
        Application["StartTime"] = DateTime.UtcNow;
    }
    
    protected void Application_End(object sender, EventArgs e)
    {
        // Cleanup logic
        LogManager.Shutdown();
    }
    
    protected void Application_Error(object sender, EventArgs e)
    {
        var exception = Server.GetLastError();
        // Log error
        LogManager.LogError(exception);
    }
    
    protected void Session_Start(object sender, EventArgs e)
    {
        Session["SessionId"] = Guid.NewGuid().ToString();
        Session["StartTime"] = DateTime.UtcNow;
    }
    
    protected void Session_End(object sender, EventArgs e)
    {
        // Session cleanup
    }
    
    protected void Application_BeginRequest(object sender, EventArgs e)
    {
        // Per-request initialization
        if (Request.IsSecureConnection == false)
        {
            var secureUrl = Request.Url.ToString().Replace("http://", "https://");
            Response.Redirect(secureUrl);
        }
    }
    
    protected void Application_EndRequest(object sender, EventArgs e)
    {
        // Per-request cleanup
    }
}
```

### Blazor Pattern: Program.cs + Middleware

```csharp
// Blazor — Program.cs
var builder = WebApplication.CreateBuilder(args);

// ────────────────────────────────────────────────────────────────
// Application_Start equivalent — runs at app startup
// ────────────────────────────────────────────────────────────────

// Register services (DI replaces UnityContainer)
builder.Services.AddScoped<IProductService, ProductService>();

// Load application state (singleton service)
builder.Services.AddSingleton<ApplicationStateService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new ApplicationStateService
    {
        SiteTitle = config["SiteTitle"] ?? "Default Title",
        StartTime = DateTime.UtcNow
    };
});

// Add Blazor services
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddBlazorWebFormsComponents();

var app = builder.Build();

// ────────────────────────────────────────────────────────────────
// Application_BeginRequest / EndRequest → Middleware
// ────────────────────────────────────────────────────────────────

// Redirect HTTP → HTTPS (replaces BeginRequest redirect)
app.UseHttpsRedirection();

// Application_Error → exception handling middleware
app.UseExceptionHandler("/Error");

// Standard pipeline
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseBlazorWebFormsComponents(); // BWFC middleware
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// ────────────────────────────────────────────────────────────────
// Application_End equivalent — IHostApplicationLifetime
// ────────────────────────────────────────────────────────────────

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    // Cleanup logic (LogManager.Shutdown(), etc.)
    Console.WriteLine("Application shutting down...");
});

app.Run();
```

### Global.asax Event Mapping Table

| Global.asax Event | ASP.NET Core Equivalent | Migration Pattern |
|-------------------|------------------------|-------------------|
| `Application_Start` | Top-level statements in `Program.cs` **before** `builder.Build()` | Service registration, config loading |
| `Application_End` | `IHostApplicationLifetime.ApplicationStopping` event | Cleanup, shutdown logging |
| `Application_BeginRequest` | Middleware **before** `await _next()` | HTTPS redirect, request logging |
| `Application_EndRequest` | Middleware **after** `await _next()` | Response logging, cleanup |
| `Application_Error` | `app.UseExceptionHandler("/Error")` | See pattern below |
| `Session_Start` | Custom middleware or `IDistributedCache` | See pattern below |
| `Session_End` | No direct equivalent | Session cleanup via cache expiration |
| `Application_AuthenticateRequest` | Custom middleware before `UseAuthentication()` | Custom auth logic |
| `Application_AuthorizeRequest` | Custom middleware before `UseAuthorization()` | Custom authz logic |

---

## 3. Application_Error → Exception Handling Middleware

### Web Forms Pattern

```csharp
// Web Forms — Global.asax.cs
protected void Application_Error(object sender, EventArgs e)
{
    var exception = Server.GetLastError();
    var httpException = exception as HttpException;
    
    if (httpException != null)
    {
        switch (httpException.GetHttpCode())
        {
            case 404:
                Response.Redirect("~/Errors/NotFound");
                break;
            case 500:
                Response.Redirect("~/Errors/ServerError");
                break;
        }
    }
    
    // Log all errors
    LogManager.LogError(exception);
    Server.ClearError();
}
```

### Blazor Pattern: Exception Handler Middleware + Error Page

```csharp
// Program.cs
app.UseExceptionHandler("/Error"); // ← Redirects to Error.razor on unhandled exceptions
app.UseStatusCodePagesWithReExecute("/Error/{0}"); // ← Handles 404, 500, etc.
```

```razor
@* Error.razor — Exception handler page *@
@page "/Error"
@page "/Error/{StatusCode:int}"
@inject ILogger<Error> Logger

<h1>Error</h1>

@if (StatusCode.HasValue)
{
    switch (StatusCode.Value)
    {
        case 404:
            <p>The page you're looking for doesn't exist.</p>
            break;
        case 500:
            <p>An internal server error occurred.</p>
            break;
        default:
            <p>An error occurred (status code: @StatusCode).</p>
            break;
    }
}
else if (Exception != null)
{
    <p>An unexpected error occurred.</p>
}

@code {
    [Parameter] public int? StatusCode { get; set; }
    
    [CascadingParameter] public HttpContext? HttpContext { get; set; }
    
    private Exception? Exception { get; set; }
    
    protected override void OnInitialized()
    {
        // Get exception from HttpContext (set by UseExceptionHandler)
        Exception = HttpContext?.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        
        if (Exception != null)
        {
            Logger.LogError(Exception, "Unhandled exception");
        }
    }
}
```

### Custom Exception Middleware (Advanced)

For more control (e.g., conditional redirects, API error responses):

```csharp
// Middleware/ExceptionHandlerMiddleware.cs
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    
    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (HttpException httpEx)
        {
            _logger.LogError(httpEx, "HTTP exception");
            
            switch (httpEx.GetHttpCode())
            {
                case 404:
                    context.Response.Redirect("/Errors/NotFound");
                    break;
                case 500:
                    context.Response.Redirect("/Errors/ServerError");
                    break;
                default:
                    context.Response.Redirect("/Error");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.Redirect("/Error");
        }
    }
}

// Program.cs
app.UseMiddleware<ExceptionHandlerMiddleware>();
```

---

## 4. Session_Start → Session State Initialization

### Web Forms Pattern

```csharp
// Web Forms — Global.asax.cs
protected void Session_Start(object sender, EventArgs e)
{
    Session["SessionId"] = Guid.NewGuid().ToString();
    Session["UserPreferences"] = new UserPreferences();
    Session["ShoppingCart"] = new ShoppingCart();
}
```

### Blazor Pattern: Scoped Services (No Direct Equivalent)

> **Important:** Blazor Server does **not** have a Session_Start event. State initialization happens when services are created (scoped services are created per-circuit).

```csharp
// Services/UserSessionService.cs
public class UserSessionService
{
    public string SessionId { get; } = Guid.NewGuid().ToString();
    public UserPreferences Preferences { get; set; } = new();
    public ShoppingCart Cart { get; set; } = new();
    
    public UserSessionService()
    {
        // Constructor runs once when service is first injected (circuit start)
    }
}

// Program.cs
builder.Services.AddScoped<UserSessionService>(); // ← Per-circuit (like per-session)

// Component usage
@inject UserSessionService Session

@code {
    protected override void OnInitialized()
    {
        var sessionId = Session.SessionId;
        var cart = Session.Cart;
    }
}
```

### Persistent Session State (Across Browser Refresh)

For session state that survives page refresh (like Web Forms Session):

```csharp
// Use ProtectedSessionStorage (browser sessionStorage with encryption)
@inject ProtectedSessionStorage SessionStorage

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var result = await SessionStorage.GetAsync<string>("SessionId");
            if (!result.Success)
            {
                var newId = Guid.NewGuid().ToString();
                await SessionStorage.SetAsync("SessionId", newId);
            }
        }
    }
}
```

---

## 5. Middleware Pipeline Order (Critical)

Middleware order matters in ASP.NET Core. Here's the correct order for migrated apps:

```csharp
var app = builder.Build();

// 1. Exception handling (must be FIRST to catch downstream errors)
app.UseExceptionHandler("/Error");

// 2. HTTPS redirection (security)
app.UseHttpsRedirection();

// 3. Static files (before routing)
app.UseStaticFiles();

// 4. Routing (must be before auth)
app.UseRouting(); // ← If using endpoint routing

// 5. CORS (if applicable)
app.UseCors();

// 6. Authentication (BEFORE authorization)
app.UseAuthentication();

// 7. Authorization (AFTER authentication)
app.UseAuthorization();

// 8. Custom middleware (e.g., BWFC .aspx rewriting)
app.UseBlazorWebFormsComponents();

// 9. Endpoints (LAST)
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
```

**Wrong order causes failures:**
- Auth after endpoints → `[Authorize]` doesn't work
- Exception handler after other middleware → errors not caught
- Static files after routing → wwwroot files require auth

---

## Common Migration Patterns

### Pattern 1: Request Logging Module

```csharp
// Web Forms — RequestLogModule.cs
public class RequestLogModule : IHttpModule
{
    public void Init(HttpApplication context)
    {
        context.BeginRequest += (s, e) => LogRequest(context.Context);
    }
    
    private void LogRequest(HttpContext context)
    {
        var log = $"{context.Request.HttpMethod} {context.Request.Url}";
        LogManager.Log(log);
    }
}

// Blazor — Middleware/RequestLoggingMiddleware.cs
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("{Method} {Path}", context.Request.Method, context.Request.Path);
        await _next(context);
    }
}

// Program.cs
app.UseMiddleware<RequestLoggingMiddleware>();
```

### Pattern 2: Custom Authentication Module

```csharp
// Web Forms — TokenAuthModule.cs
public class TokenAuthModule : IHttpModule
{
    public void Init(HttpApplication context)
    {
        context.AuthenticateRequest += OnAuthenticateRequest;
    }
    
    private void OnAuthenticateRequest(object sender, EventArgs e)
    {
        var token = HttpContext.Current.Request.Headers["X-Auth-Token"];
        if (ValidateToken(token))
        {
            var identity = new GenericIdentity("user@example.com");
            HttpContext.Current.User = new GenericPrincipal(identity, new[] { "User" });
        }
    }
}

// Blazor — Middleware/TokenAuthMiddleware.cs
public class TokenAuthMiddleware
{
    private readonly RequestDelegate _next;
    
    public TokenAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["X-Auth-Token"].ToString();
        if (ValidateToken(token))
        {
            var identity = new GenericIdentity("user@example.com");
            context.User = new GenericPrincipal(identity, new[] { "User" });
        }
        
        await _next(context);
    }
    
    private bool ValidateToken(string token) { /* ... */ return true; }
}

// Program.cs — BEFORE UseAuthentication()
app.UseMiddleware<TokenAuthMiddleware>();
app.UseAuthentication();
```

### Pattern 3: Response Header Injection

```csharp
// Web Forms — SecurityHeaderModule.cs
public class SecurityHeaderModule : IHttpModule
{
    public void Init(HttpApplication context)
    {
        context.PreSendRequestHeaders += (s, e) =>
        {
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        };
    }
}

// Blazor — Middleware/SecurityHeadersMiddleware.cs
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return Task.CompletedTask;
        });
        
        await _next(context);
    }
}

// Program.cs — early in pipeline
app.UseMiddleware<SecurityHeadersMiddleware>();
```

---

## Known Failure Modes

### 1. Module Event Order Mismatch

**Problem:** Web Forms module events fire in a specific order. ASP.NET Core middleware order is explicit — you must get it right.

**Solution:** Map each Web Forms event to the correct middleware position (see table above).

### 2. Session_Start Has No Equivalent

**Problem:** `Session_Start` fires when a browser session starts. Blazor Server circuits start on page load, not HTTP session start.

**Solution:** Use scoped service constructors for per-circuit initialization, or `ProtectedSessionStorage` for browser session state.

### 3. Application State Is Per-Server

**Problem:** `Application["key"]` was per-IIS-process. Singleton services are also per-process — won't work in multi-server deployments.

**Solution:** Use distributed cache (`IDistributedCache` with Redis) for multi-server state.

---

## What Developers Must Do Manually

1. **Review module logic** — Understand what each `IHttpModule` does before converting. Some may be obsolete (e.g., URL rewriting now built-in).
2. **Test middleware order** — ASP.NET Core pipeline order is explicit. Test auth, exception handling, and routing thoroughly.
3. **Refactor Session_Start logic** — No direct equivalent exists. Decide if state should be scoped service, browser storage, or database-backed.
4. **Application_End cleanup** — Register cleanup logic with `IHostApplicationLifetime.ApplicationStopping`.

---

## L1 Script Support (Future Enhancement)

The L1 PowerShell script could detect `Global.asax.cs` and extract event handlers:

```csharp
// TODO: BWFC — Application_Start logic detected in Global.asax.
//       Move service registration to Program.cs (before builder.Build()).
//       Move application state initialization to singleton service.
//       See bwfc-middleware-migration skill for examples.

// TODO: BWFC — Application_Error detected in Global.asax.
//       Add app.UseExceptionHandler("/Error") to Program.cs.
//       Create Error.razor page with exception logging.
//       See bwfc-middleware-migration skill for pattern.
```

---

## References

- [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/)
- [Migrate HTTP handlers and modules](https://learn.microsoft.com/aspnet/core/migration/http-modules)
- [App startup in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/startup)
- [Exception handling](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling)
