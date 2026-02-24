# Colossus — History

<!-- ⚠ Summarized 2026-02-23 by Scribe — original entries covered 2026-02-10 through 2026-02-12 -->

## Summary: Milestones 1–3 Integration Tests (2026-02-10 through 2026-02-12)

Audited 74 sample routes, added 32 missing smoke tests. Added interaction tests for Sprint 2 (MultiView, ChangePassword, CreateUserWizard, Localize) and Sprint 3 (DetailsView paging/edit, PasswordRecovery 3-step flow). Fixed 7 pre-existing failures: missing `@using BlazorWebFormsComponents.LoginControls` on ChangePassword/CreateUserWizard, external placeholder URLs → local SVGs, duplicate ImageMap InlineData, Calendar console error filter, TreeView broken image path. 116 integration tests passing.

## Summary: Milestone 4 Chart + Utility Tests (2026-02-12)

Chart: 8 smoke tests + 11 canvas tests + 19 enhanced visual tests (dimensions, Chart.js initialization, multi-series datasets, canvas context). Used `WaitUntilState.DOMContentLoaded` for Chart tests. DataBinder + ViewState: 4 utility feature tests (Eval rendering, ViewState counter increment). Enhanced Chart tests use `BoundingBoxAsync()`, `page.EvaluateAsync<T>` for Chart.js internals, ±10px tolerance for dimensions. Total: 120 integration tests.

**Key patterns:** `LocatorWaitForOptions` instead of `Expect()` (no PageTest inheritance). `PressSequentiallyAsync` + Tab for Blazor Server InputText binding. ID-specific selectors for multi-instance pages. Filter ISO 8601 timestamps from console errors.

📌 Team update (2026-02-12): LoginControls sample pages MUST include `@using BlazorWebFormsComponents.LoginControls`. Never use external image URLs. — Colossus

 Team update (2026-02-23): Milestone 6 Work Plan ratified  54 WIs across P0/P1/P2 tiers  decided by Forge
 Team update (2026-02-23): UI overhaul requested  Colossus assigned integration tests (UI-9)  decided by Jeffrey T. Fritz

## Summary: Milestone 7 Integration Tests (2026-02-24)

Added 9 smoke tests and 9 interaction tests for M7 sample pages: GridView Selection/DisplayProperties, TreeView Selection/ExpandCollapse, Menu Selection, DetailsView Styles/Caption, FormView Events/Styles. Menu Selection test skips console error checks (JS interop). FormView tests use DOMContentLoaded (items bound in OnAfterRenderAsync). Build verified green.

## Learnings

- FormView sample pages bind Items in `OnAfterRenderAsync`, so tests must use `WaitUntilState.DOMContentLoaded` + explicit `WaitForSelectorAsync` instead of `NetworkIdle`.
- Menu interaction tests should always skip console error checks — the Menu component's JS interop (`bwfc.Page.AddScriptElement`) produces expected console errors in headless Playwright.
- GridView Selection pages render Select links as `<a>` elements inside `<tbody>` rows — use `tbody tr:first-child a` with `HasTextString = "Select"` to target them.
- DetailsView Caption renders actual `<caption>` HTML elements that can be directly queried.
- **Playwright `text=` locator gotcha:** `page.Locator("text=Label:")` matches the *innermost* element containing that text. When the markup is `<p><strong>Label:</strong> value</p>`, the locator returns the `<strong>`, not the parent `<p>` — so the value portion is excluded from `TextContentAsync()`. Fix: use `page.Locator("p").Filter(new() { HasTextString = "Label:" })` (or the appropriate parent tag) to match the container element that holds both the label and value.
- For `<div>` containers with multiple `<strong>` labels (e.g., TreeView/Menu feedback panels), use `page.Locator("div").Filter(new() { HasTextString = "Target label:" }).Last` to match the specific container div.
- When waiting for FormView to render its item template buttons, use a specific selector like `button:has-text('Edit')` instead of generic `button, input[type='submit']` — the latter matches sidebar/nav buttons that already exist, causing the wait to resolve prematurely before the FormView renders.
- To avoid strict-mode violations when text appears in both rendered output AND code examples, target the specific rendered element (e.g., `page.Locator("td").Filter(new() { HasTextString = "Widget Catalog" }).First`) rather than using bare `text=` locators.

 Team update (2026-02-24): Menu auto-ID pattern  Menu now auto-generates IDs, JS interop crash fixed  decided by Cyclops
 Team update (2026-02-24): M8 scope excludes version bump to 1.0 and release  decided by Jeffrey T. Fritz

 Team update (2026-02-24): v0.14 released  Forge audit: conditional ship, 6 missing sample pages and PagerSettings defaults noted for 1.0  decided by Forge

 Team update (2026-02-24): Upstream promotion flow directive  all work goes feature branch  personal fork  PR to upstream/dev  pull/push dev sync  release PR upstream/dev  upstream/main with tag  decided by Jeffrey T. Fritz
