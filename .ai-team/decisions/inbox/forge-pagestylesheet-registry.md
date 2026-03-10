# Decision: PageStyleSheet Registry Architecture

**By:** Forge  
**Date:** 2026-03-11  
**Type:** Architecture Design

## Summary

Replacing the dispose-based CSS unloading with a **registry-based "last page wins" model**. CSS persists until no PageStyleSheet component in the render tree references it anymore.

## The Problem

Current implementation unloads CSS on `DisposeAsync`, but:
- In SSR, dispose fires immediately after render (CSS disappears before user sees page)
- Current fix (static `<link>` + smart disposal) is a band-aid
- No support for ref counting when multiple components reference same CSS

## Decision

### 1. Registry Lives in JavaScript

The stylesheet registry will be a JS object in `Basepage.module.js`:
- Persists across enhanced navigations (same document)
- Can adopt static `<link>` tags from SSR
- Direct DOM access for manipulation

### 2. Reference Counting by Href

```javascript
refs: new Map() // href -> Set<componentId>
```

CSS is only removed when its ref count drops to zero.

### 3. Debounced Cleanup (100ms)

After any `unregister()`, schedule cleanup with 100ms debounce. This:
- Handles rapid navigation (unregister old → register new)
- Lets new page components register before cleanup runs
- Is invisible to users

### 4. C# Components Register/Unregister

Components call:
- `registry.register(componentId, href, ...)` in `OnAfterRenderAsync`
- `registry.unregister(componentId, href)` in `DisposeAsync`

### 5. SSR Static Links Adopted

When `register()` finds an existing `<link>` with matching href, it adopts it into the registry instead of creating a duplicate.

## Trade-offs

| Decision | Trade-off |
|----------|-----------|
| JS-primary registry | Harder to unit test, but works in all render modes |
| 100ms debounce | Brief window where CSS is "orphaned" but not removed |
| Href-based deduplication | Must handle URL normalization |

## Alternatives Rejected

1. **C#-only registry** — Scoped services reset on SSR requests, can't adopt static links
2. **NavigationManager cleanup** — Doesn't fire in static SSR
3. **SectionContent aggregation** — Requires App.razor changes, steeper learning curve

## Implementation

See full spec: `dev-docs/proposals/pagestylesheet-registry-design.md`

### Files to Change

1. `Basepage.module.js` — Add `stylesheetRegistry`
2. `PageStyleSheet.razor.cs` — Use registry instead of direct load/unload
3. `PageStyleSheetTests.cs` — Add registry tests

### No Breaking Changes

The component API is unchanged:
```razor
<PageStyleSheet Href="CSS/Page.css" />
```

## References

- `dev-docs/proposals/pagestylesheet-registry-design.md` — Full architecture
- `dev-docs/proposals/dynamic-css-loader-design.md` — Original design
- `.ai-team/decisions/inbox/copilot-directive-20260310-pagestylesheet-strategy.md` — Jeff's directive
