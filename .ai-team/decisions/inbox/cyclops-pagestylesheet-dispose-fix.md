# Decision: PageStyleSheet CSS Timing Fix

**By:** Cyclops  
**Date:** 2026-03-11  
**Type:** Bug Fix

## Problem

PageStyleSheet had a critical timing bug with CSS unloading. The `DisposeAsync` method would unload CSS at different times depending on render mode:

| Render Mode | When Dispose Fires | Original Behavior |
|-------------|-------------------|-------------------|
| Static SSR | After render completes | ❌ CSS removed immediately (never visible) |
| Prerendering | After prerender completes | ❌ CSS removed too early |
| InteractiveServer | When user navigates | ✅ Correct |
| InteractiveWebAssembly | When user navigates | ✅ Correct |

The root cause: in SSR/prerendering, `OnAfterRenderAsync` doesn't run (no JS interop), and the component disposes when rendering finishes—not when the user navigates away.

## Analysis

Evaluated three options:

1. **Option A: Don't unload on dispose** — Let browser handle cleanup naturally
2. **Option B: Track render mode and skip unload for SSR** — Use `_isLoaded` flag to guard
3. **Option C: Emit static `<link>` tag in SSR mode** — Render CSS directly into HTML stream

**Findings:**
- The existing `_isLoaded` guard (Option B) was already in place, but the component rendered NOTHING in SSR mode—so CSS was never loaded at all
- For CSS, the browser naturally handles cleanup during navigation—explicit unload is only needed in SPA-style interactive navigation
- Blazor's `RendererInfo.IsInteractive` property reliably detects prerendering vs interactive mode

## Decision: Hybrid Approach (Options B + C)

Implemented a two-pronged fix:

### 1. Static `<link>` Tag for Non-Interactive Renders

```razor
@if (!string.IsNullOrEmpty(Href) && !RendererInfo.IsInteractive)
{
    <link rel="stylesheet" href="@Href" id="@GetLinkId()" 
          media="@Media" integrity="@Integrity" crossorigin="@CrossOrigin" />
}
```

When `RendererInfo.IsInteractive` is `false` (SSR or prerendering pass), the component renders a static `<link>` tag directly into the HTML. This ensures CSS is present in the initial response.

### 2. Smart Disposal Logic

```csharp
// Track if we reached interactive state
_wasEverInteractive = RendererInfo.IsInteractive;

// Only unload if we:
// 1. Successfully loaded via JS interop (not static render)
// 2. Were actually in interactive mode (not disposing after SSR)
if (_isLoadedViaJs && _wasEverInteractive && _module is not null)
{
    await _module.InvokeVoidAsync("unloadStyleSheet", _linkId);
}
```

This prevents the bug where CSS is removed before the user sees the page.

### 3. Idempotent JS Loading

The `loadStyleSheet` JS function already checks `document.getElementById(id)` before creating a new link. After prerender → interactive hydration, the JS will find the existing link and skip re-creation.

## Behavior By Render Mode (After Fix)

| Render Mode | CSS Load | CSS Unload |
|-------------|----------|------------|
| Static SSR | Static `<link>` in HTML | Browser handles (full navigation) |
| Prerendering | Static `<link>` in HTML | Browser handles (dispose fires early) |
| Interactive (after prerender) | JS finds existing link | JS removes on navigation |
| InteractiveServer (no prerender) | JS creates link | JS removes on navigation |
| InteractiveWebAssembly | JS creates link | JS removes on navigation |

## Key Questions Answered

1. **Should PageStyleSheet unload CSS at all?**  
   Yes, but ONLY in truly interactive scenarios where the component lifecycle matches user navigation. For SSR, let the browser handle it.

2. **For SSR, should we render a static `<link>` tag?**  
   Yes. This is the only way CSS works in static SSR—JS interop is unavailable.

3. **How do we detect SSR vs Interactive mode?**  
   Use `RendererInfo.IsInteractive` (available in .NET 8+). It's `false` during prerendering AND static SSR.

## Files Changed

- `PageStyleSheet.razor` — Added conditional static `<link>` rendering
- `PageStyleSheet.razor.cs` — Added `_wasEverInteractive` tracking, smart disposal logic, updated XML docs

## Testing Scenarios

Mental walkthrough confirms correct behavior:

1. **Static SSR page** → `<link>` rendered in HTML, user sees CSS, component disposes silently, browser handles cleanup on next navigation ✅
2. **Prerender → Interactive** → `<link>` rendered in prerender HTML, hydration finds existing link, navigation triggers JS unload ✅  
3. **InteractiveServer (no prerender)** → JS loads CSS, navigation triggers JS unload ✅
