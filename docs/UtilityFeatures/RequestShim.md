# Request Shim

The `RequestShim` class provides compatibility with the ASP.NET Web Forms `Request` object (`HttpRequest`). It wraps ASP.NET Core's `HttpContext.Request` and `NavigationManager` so that migrated code-behind using `Request.QueryString["id"]`, `Request.Cookies["name"]`, and `Request.Url` compiles and works correctly — with graceful degradation when `HttpContext` is unavailable during interactive rendering.

Original Microsoft implementation: https://docs.microsoft.com/en-us/dotnet/api/system.web.httprequest?view=netframework-4.8

## Background

In ASP.NET Web Forms, the `Request` object was available on every `Page` and `UserControl`:

```csharp
// Web Forms code-behind
string id = Request.QueryString["id"];
string sessionCookie = Request.Cookies["session"].Value;
Uri currentUrl = Request.Url;
```

These properties provided direct access to the incoming HTTP request data.

## Blazor Implementation

In Blazor, HTTP request data is only available during server-side rendering (SSR) via `HttpContext`. During interactive WebSocket rendering, there is no HTTP request. The `RequestShim` bridges this gap by:

1. **`QueryString["key"]`** — Reads from `HttpContext.Request.Query` in SSR; falls back to parsing `NavigationManager.Uri` in interactive mode
2. **`Cookies["key"]`** — Reads from `HttpContext.Request.Cookies` in SSR; returns an empty collection in interactive mode (with a logged warning)
3. **`Url`** — Reads from `HttpContext.Request` in SSR; falls back to `NavigationManager.Uri` in interactive mode

### Availability

`RequestShim` is automatically available when your page inherits from `WebFormsPageBase`:

```razor
@inherits WebFormsPageBase

@code {
    private string _productId = "";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _productId = Request.QueryString["id"];
    }
}
```

## Graceful Degradation

Blazor Server components can run in two modes:

| Mode | `HttpContext` | `QueryString` | `Cookies` | `Url` |
|---|---|---|---|---|
| SSR / Pre-render | ✅ Available | From HTTP request | From HTTP request | From HTTP request |
| Interactive (WebSocket) | ❌ Unavailable | Parsed from `NavigationManager.Uri` | Empty collection (warning logged) | From `NavigationManager.Uri` |

Use the `IsHttpContextAvailable` guard when cookie access is critical:

```razor
@inherits WebFormsPageBase

@code {
    private string _theme = "default";

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (IsHttpContextAvailable)
        {
            // Safe — HttpContext is present
            _theme = Request.Cookies["theme"] ?? "default";
        }

        // QueryString works in both modes (no guard needed)
        var page = Request.QueryString["page"];
    }
}
```

## Web Forms Usage

```csharp
// Query string access
string productId = Request.QueryString["id"];
string category = Request.QueryString["cat"];

// Cookie access
string session = Request.Cookies["session"].Value;
string theme = Request.Cookies["theme"]?.Value ?? "default";

// URL access
Uri currentUrl = Request.Url;
string fullPath = Request.Url.AbsolutePath;
```

## Blazor Usage

```razor
@inherits WebFormsPageBase

<p>Product: @_productId</p>
<p>URL: @_currentUrl</p>

@code {
    private string _productId = "";
    private Uri? _currentUrl;
    private string _sessionId = "";

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Query strings — works in both SSR and interactive modes
        _productId = Request.QueryString["id"] ?? "";

        // URL — works in both modes
        _currentUrl = Request.Url;

        // Cookies — only reliable in SSR mode
        if (IsHttpContextAvailable)
        {
            _sessionId = Request.Cookies["session"] ?? "";
        }
    }
}
```

## Migration Path

| Web Forms | BWFC Shim | Native Blazor |
|---|---|---|
| `Request.QueryString["id"]` | `Request.QueryString["id"]` | `NavigationManager.Uri` + parse, or `[SupplyParameterFromQuery]` |
| `Request.Cookies["name"]` | `Request.Cookies["name"]` | `IHttpContextAccessor` (SSR only) |
| `Request.Url` | `Request.Url` | `NavigationManager.ToAbsoluteUri(Nav.Uri)` |
| `Request.Url.AbsolutePath` | `Request.Url.AbsolutePath` | `new Uri(Nav.Uri).AbsolutePath` |

## Moving On

`RequestShim` is a migration bridge. As you refactor:

1. **Replace `QueryString` with `[SupplyParameterFromQuery]`** — Blazor's built-in attribute binds query parameters directly to component properties
2. **Replace `Url` with `NavigationManager`** — Inject `NavigationManager` and use `Uri` or `ToAbsoluteUri()`
3. **Replace `Cookies` with proper state management** — Use cascading parameters, `ProtectedSessionStorage`, or server-side services instead of cookies

```razor
@* Before (migration shim) *@
@inherits WebFormsPageBase
@code {
    string id = Request.QueryString["id"];
    Uri url = Request.Url;
}

@* After (native Blazor) *@
@inject NavigationManager Nav
@code {
    [SupplyParameterFromQuery] public string Id { get; set; }
    Uri url => Nav.ToAbsoluteUri(Nav.Uri);
}
```

## See Also

- [WebFormsPage](WebFormsPage.md) — Page-level base class providing the `Request` property
- [Response.Redirect](ResponseRedirect.md) — Companion shim for navigation
- [L2 Automation Shims](L2AutomationShims.md) — Overview of all migration automation features
