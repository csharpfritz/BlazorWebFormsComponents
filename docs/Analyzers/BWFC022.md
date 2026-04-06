# BWFC022: Page.ClientScript Usage

**Diagnostic ID:** `BWFC022`  
**Severity:** ⚠️ Warning  
**Category:** Migration  
**Status:** Active

---

## What It Detects

This analyzer warns when your code uses `Page.ClientScript` or `ClientScriptManager` — Web Forms' API for dynamically registering client-side JavaScript.

**Detected patterns:**
- `Page.ClientScript.RegisterStartupScript(...)`
- `Page.ClientScript.RegisterClientScriptBlock(...)`
- `Page.ClientScript.RegisterClientScriptInclude(...)`
- `Page.ClientScript.GetPostBackEventReference(...)`
- Any `ClientScriptManager` method call

---

## Example

```csharp
protected void Page_Load(object sender, EventArgs e)
{
    // ⚠️ BWFC022: Page.ClientScript is not available in Blazor.
    // See: docs/Migration/ClientScriptMigrationGuide.md
    Page.ClientScript.RegisterStartupScript(
        this.GetType(),
        "InitializeUI",
        "console.log('Page loaded');",
        addScriptTags: true);
}
```

---

## Why It Matters

In Web Forms, `Page.ClientScript` is the standard way to inject JavaScript into pages. In Blazor:

- **There is no `Page` object** — Blazor is component-based, not page-based
- **No automatic script injection** — JavaScript must be explicitly referenced
- **Different lifecycle** — Instead of page load, use component initialization hooks like `OnAfterRenderAsync()`

Without addressing `Page.ClientScript` usage, your migrated code will **compile errors or runtime failures**.

---

## How to Fix

### Recommended: ClientScriptShim (Easiest)

**For a zero-rewrite migration, use the `ClientScriptShim`** included in BWFC. It provides the exact same API as `Page.ClientScript`:

```csharp
// Web Forms
Page.ClientScript.RegisterStartupScript(GetType(), "init", "...", true);

// Blazor with ClientScriptShim — identical call!
ClientScript.RegisterStartupScript(GetType(), "init", "...", true);
```

See ["ClientScriptShim (Zero-Rewrite Path)"](../Migration/ClientScriptMigrationGuide.md#-recommended-clientscriptshim-zero-rewrite-path) in the migration guide for details.

---

### Alternative: Manual Rewrite

If you prefer to modernize your code now, rewrite to `IJSRuntime` directly. The fix depends on **which** ClientScript method you're using. See [ClientScriptMigrationGuide.md](../Migration/ClientScriptMigrationGuide.md) for detailed before/after examples.

### Quick Reference

| Pattern | Blazor Equivalent | Difficulty |
|---------|-------------------|-----------|
| `RegisterStartupScript()` | `ClientScriptShim` (⭐ Easy) or `OnAfterRenderAsync()` + `IJSRuntime` (⭐⭐ Moderate) | ⭐ Easy |
| `RegisterClientScriptInclude()` | `ClientScriptShim` (⭐ Easy) or `<script>` tag in layout (⭐ Easy) | ⭐ Easy |
| `RegisterClientScriptBlock()` | `ClientScriptShim` (⭐ Easy) or JS module (⭐ Easy) | ⭐ Easy |
| `GetPostBackEventReference()` | `@onclick` or `EventCallback<T>` | ⭐⭐ Medium |

### Common Fix: Startup Script

=== "Web Forms (Before)"
    ```csharp
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            Page.ClientScript.RegisterStartupScript(
                this.GetType(),
                "InitializeUI",
                "$(function() { applyTheme('dark'); });",
                addScriptTags: true);
        }
    }
    ```

=== "Blazor (After)"
    ```razor
    @inject IJSRuntime JS
    
    @code {
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var module = await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./app.js");
                await module.InvokeVoidAsync("applyTheme", "dark");
            }
        }
    }
    ```

### Common Fix: Script Include

=== "Web Forms (Before)"
    ```csharp
    protected void Page_Load(object sender, EventArgs e)
    {
        Page.ClientScript.RegisterClientScriptInclude(
            "jquery-ui",
            ResolveUrl("~/lib/jquery-ui/jquery-ui.min.js"));
    }
    ```

=== "Blazor (After)"
    ```html
    <!-- In layout (index.html or _Layout.html) -->
    <script src="lib/jquery/jquery.min.js"></script>
    <script src="lib/jquery-ui/jquery-ui.min.js"></script>
    ```

### Common Fix: PostBack Event Reference

=== "Web Forms (Before)"
    ```csharp
    public string GetDeleteButtonScript()
    {
        return Page.ClientScript.GetPostBackEventReference(
            new PostBackOptions(btnDelete, "clicked"));
    }
    ```

=== "Blazor (After)"
    ```razor
    <button @onclick="HandleDelete">Delete</button>
    
    @code {
        private async Task HandleDelete()
        {
            await DeleteItemAsync();
        }
    }
    ```

---

## Detailed Migration Paths

For **comprehensive migration guidance** with code examples for each ClientScript method, see:

📖 **[ClientScriptMigrationGuide.md](../Migration/ClientScriptMigrationGuide.md)**

Sections:
1. **Startup Scripts** — Most common pattern (Section 1)
2. **Script Includes** — External `.js` files (Section 2)
3. **Script Blocks** — Inline JavaScript (Section 3)
4. **Postback Events** — Dynamic event references (Section 4)
5. **Form Validation** — `Page.IsValid` patterns (Section 5)

---

## Common Mistakes

### ❌ Don't: Use `eval()` for Complex Scripts

```csharp
// ❌ WRONG: Embedding complex logic in eval()
await JS.InvokeVoidAsync("eval", @"
    function processData() {
        // 50 lines of logic...
    }
    processData();
");
```

### ✅ Do: Define Functions in JavaScript Modules

```javascript
// app.js
export function processData() {
    // 50 lines of logic...
}
```

```csharp
// Component
var module = await JS.InvokeAsync<IJSObjectReference>("import", "./app.js");
await module.InvokeVoidAsync("processData");
```

### ❌ Don't: Skip the `firstRender` Guard

```csharp
// ❌ WRONG: Script runs on every render
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    await JS.InvokeVoidAsync("applyTheme");
}
```

### ✅ Do: Guard with `if (firstRender)`

```csharp
// ✅ CORRECT: Script runs only on first render
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await JS.InvokeVoidAsync("applyTheme");
    }
}
```

---

## Related Analyzers

- **[BWFC023](BWFC023.md)** — IPostBackEventHandler usage
- **[BWFC024](BWFC024.md)** — ScriptManager code-behind usage

---

## Configuration

To suppress this warning for a specific line:

```csharp
#pragma warning disable BWFC022
Page.ClientScript.RegisterStartupScript(/* ... */);
#pragma warning restore BWFC022
```

Or in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.BWFC022.severity = silent
```

---

## See Also

- 📖 [ClientScriptMigrationGuide.md](../Migration/ClientScriptMigrationGuide.md) — Comprehensive migration guide
- 📖 [IJSRuntime Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability) — Blazor JS interop
- 📖 [Component Lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle) — OnAfterRenderAsync and friends

---

**Status:** ✅ Active  
**Last Updated:** 2026-07-30  
**Owner:** Beast (Technical Writer)
