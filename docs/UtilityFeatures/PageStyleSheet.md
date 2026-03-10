# PageStyleSheet

PageStyleSheet dynamically loads and unloads CSS stylesheets, providing a clean migration path for Web Forms page-specific CSS patterns. It solves the "HeadContent doesn't work in layouts" limitation in Blazor.

## Background

In ASP.NET Web Forms, pages could inject CSS into the `<head>` using a `ContentPlaceHolder` in the master page:

```aspx
<%-- Site.Master --%>
<head>
    <asp:ContentPlaceHolder ID="head" runat="server" />
</head>

<%-- Courses.aspx --%>
<asp:Content ContentPlaceHolderID="head" runat="server">
    <link href="CSS/CSS_Courses.css" rel="stylesheet" />
</asp:Content>
```

This pattern let each page include its own CSS that would naturally swap when navigating between pages.

### The Blazor Challenge

Blazor's built-in `<HeadContent>` component works in **pages** but **not in layouts**. If you try to use `HeadContent` in a layout component (like `MainLayout.razor`), it silently fails — the content never appears in the document head.

This creates problems for migrated applications where:

- Layout CSS should persist across page navigations
- Page CSS should swap when users navigate
- Nested components might need their own CSS

## Blazor Implementation

PageStyleSheet uses a registry-based lifecycle that tracks CSS references across all components:

```razor
@* In any component, page, or layout *@
<PageStyleSheet Href="CSS/page-styles.css" />
```

### How It Works

1. **Reference counting** — A JavaScript registry tracks how many components reference each stylesheet
2. **Layout CSS persists** — Because the layout component stays alive, its CSS stays loaded
3. **Page CSS swaps** — Old page unregisters, new page registers, orphaned CSS is cleaned up
4. **100ms debounced cleanup** — Handles navigation transitions smoothly without flicker

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `Href` | `string` | **Required.** URL of the stylesheet. Can be relative (`CSS/page.css`) or absolute. |
| `Media` | `string?` | Optional media query (e.g., `"screen"`, `"print"`, `"(max-width: 600px)"`). |
| `Id` | `string?` | Optional ID for the link element. Auto-generated if not provided. |
| `Integrity` | `string?` | Optional SRI hash for CDN resources. |
| `CrossOrigin` | `string?` | Optional crossorigin attribute. Typically `"anonymous"` when using integrity. |

## Migration Patterns

### Basic Page CSS

**Before (Web Forms):**

```aspx
<%-- Products.aspx --%>
<asp:Content ContentPlaceHolderID="head" runat="server">
    <link href="CSS/Products.css" rel="stylesheet" />
</asp:Content>
```

**After (Blazor):**

```razor
@* Products.razor *@
@page "/products"

<PageStyleSheet Href="CSS/Products.css" />

<h1>Products</h1>
@* ... rest of page ... *@
```

### Layout CSS (Persistent)

CSS in layouts persists across all page navigations because the layout component stays alive:

```razor
@* MainLayout.razor *@
@inherits LayoutComponentBase

<PageStyleSheet Href="CSS/Layout.css" />
<PageStyleSheet Href="CSS/Navigation.css" />

<nav>
    <!-- Navigation markup -->
</nav>

<main>
    @Body
</main>
```

### Page CSS (Swaps on Navigation)

CSS in page components unloads when you navigate away:

```razor
@* About.razor *@
@page "/about"

<PageStyleSheet Href="CSS/About.css" />

<h1>About Us</h1>
```

When you navigate from `/about` to `/products`:
1. `About.razor` disposes → unregisters `CSS/About.css`
2. `Products.razor` renders → registers `CSS/Products.css`
3. After 100ms, the registry cleans up orphaned `CSS/About.css`

### Shared CSS (Multiple Components)

Multiple components can reference the same stylesheet. It stays loaded until all components unregister:

```razor
@* Both pages use the same shared styles *@

@* PageA.razor *@
<PageStyleSheet Href="CSS/shared-forms.css" />

@* PageB.razor *@
<PageStyleSheet Href="CSS/shared-forms.css" />
```

If both pages are displayed simultaneously (e.g., in tabs or nested layouts), the CSS remains loaded. It only unloads when the *last* referencing component disposes.

### CDN Resources with SRI

For stylesheets loaded from CDNs, use integrity checking:

```razor
<PageStyleSheet 
    Href="https://cdn.example.com/bootstrap.min.css"
    Integrity="sha384-xxx..."
    CrossOrigin="anonymous" />
```

### Print Stylesheets

Use the `Media` parameter for conditional stylesheets:

```razor
<PageStyleSheet Href="CSS/print.css" Media="print" />
<PageStyleSheet Href="CSS/mobile.css" Media="(max-width: 768px)" />
```

## Render Mode Behavior

PageStyleSheet adapts to Blazor's different rendering modes:

| Mode | Load | Unload | Notes |
|------|------|--------|-------|
| **Static SSR** | Static `<link>` tag | Browser (full page nav) | No JS interop needed |
| **Prerendering** | Static `<link>` tag | JS manages after hydration | Tag adopted by registry |
| **InteractiveServer** | JS interop | JS on dispose | Full lifecycle control |
| **InteractiveWebAssembly** | JS interop | JS on dispose | Same as Server |

!!! info "SSR Mode"
    In pure SSR mode (no interactivity), PageStyleSheet renders a static `<link>` tag directly into the HTML. The stylesheet persists until the user performs a full-page navigation. This is the expected behavior — SSR pages don't have component lifecycle events.

### Prerendering Transition

During prerendering:

1. **Server prerender**: A static `<link>` tag is rendered
2. **Hydration**: Blazor becomes interactive
3. **Adoption**: The JS registry "adopts" the existing `<link>` element
4. **Lifecycle**: Normal register/unregister behavior kicks in

This ensures no flicker or duplicate stylesheets during the prerender-to-interactive transition.

## Nested Component Usage

PageStyleSheet works in any component, not just pages:

```razor
@* DataTable.razor (reusable component) *@
<PageStyleSheet Href="CSS/DataTable.css" />

<table class="data-table">
    @ChildContent
</table>

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
```

When the `DataTable` component is added to the page, its CSS loads. When removed, the CSS unloads (unless other instances still reference it).

## Why Not HeadContent?

Blazor's built-in `<HeadContent>` has limitations that PageStyleSheet addresses:

| Feature | HeadContent | PageStyleSheet |
|---------|-------------|----------------|
| Works in pages | ✅ Yes | ✅ Yes |
| Works in layouts | ❌ No | ✅ Yes |
| Works in nested components | ⚠️ Limited | ✅ Yes |
| Cleans up on dispose | ❌ No | ✅ Yes |
| Reference counting | ❌ No | ✅ Yes |
| SSR compatible | ✅ Yes | ✅ Yes |

`HeadContent` is designed to add content to `<head>` during page render, but it doesn't track lifecycle or work reliably outside of page components.

## Best Practices

1. **One PageStyleSheet per CSS file** — Don't combine multiple CSS files in one component; use separate PageStyleSheet components for each

2. **Layout vs Page CSS** — Put persistent styles in layouts, page-specific styles in pages

3. **Avoid duplicate registrations** — The registry handles duplicates gracefully, but it's cleaner to organize CSS logically

4. **Use relative paths** — Relative paths (`CSS/page.css`) are resolved against the app base, just like Web Forms

5. **Consider bundling** — For production, consider bundling related CSS files rather than loading many small files

## Troubleshooting

### CSS Not Loading

1. Verify the `Href` path is correct (check browser Network tab)
2. Ensure the CSS file is in `wwwroot` and accessible
3. For SSR, check that the static `<link>` tag appears in page source

### CSS Not Unloading

1. Check browser console for `[BWFC]` debug messages
2. Verify the component is actually disposing (log in `DisposeAsync`)
3. Ensure no other component still references the same CSS

### Flicker During Navigation

If you see CSS flash during page transitions:

1. The 100ms debounce should prevent this — verify JS is loaded
2. Check that layout CSS is in the layout component, not pages
3. Consider CSS architecture to minimize page-specific overrides

## See Also

- [JavaScript Setup](JavaScriptSetup.md) — Required JS for PageStyleSheet
- [WebFormsPage](WebFormsPage.md) — Page-level wrapper for Web Forms features
- [Master Pages Migration](../Migration/MasterPages.md) — Migrating layout patterns
