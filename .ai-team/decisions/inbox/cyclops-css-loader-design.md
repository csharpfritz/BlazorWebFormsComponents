# Decision: PageStyleSheet Component Design

**By:** Cyclops  
**Date:** 2026-03-10  
**Type:** Component Design

## Context

When migrating Web Forms `Site.Master` CSS references to Blazor, developers naturally place them in `MainLayout.razor`. However, Blazor's `<HeadContent>` in layouts is **not injected** into `<HeadOutlet>` — only page-level HeadContent works. This is a known Blazor limitation (GitHub issues #45904, #51864).

## Decision

Created a `PageStyleSheet` component that:
1. Dynamically loads CSS via JavaScript interop when the component renders
2. Automatically unloads CSS when the component disposes (on navigation)
3. Works in layouts, pages, or any component
4. Supports standard link attributes: `Href`, `Media`, `Integrity`, `CrossOrigin`

### Usage Pattern

```razor
@* In any component or page *@
<PageStyleSheet Href="CSS/CSS_Courses.css" />
<PageStyleSheet Href="CSS/Print.css" Media="print" />
```

### Technical Approach

- Uses JS interop (`loadStyleSheet`/`unloadStyleSheet` in Basepage.module.js)
- Generates unique IDs for each link element to allow multiple stylesheets
- Gracefully handles SSR/prerender scenarios where JS is not available
- Implements `IAsyncDisposable` for proper cleanup

## Why This Approach

1. **HeadContent limitations are by design** — Blazor uses "last writer wins", not aggregation
2. **SectionContent workaround requires App.razor changes** — not ideal for migration scripts
3. **JS interop is reliable** — works across all render modes
4. **Matches Web Forms mental model** — page-specific CSS that goes away on navigation

## Implementation

- `PageStyleSheet.razor` / `PageStyleSheet.razor.cs` — the component
- `Basepage.module.js` — added `loadStyleSheet` and `unloadStyleSheet` functions
- Full design doc: `dev-docs/proposals/dynamic-css-loader-design.md`

## Impact

- Migration scripts can now handle `<asp:Content ContentPlaceHolderID="head">` CSS references
- Master page CSS should still go in `App.razor` (static), page CSS uses `<PageStyleSheet>`
- No changes to existing BWFC components or services
