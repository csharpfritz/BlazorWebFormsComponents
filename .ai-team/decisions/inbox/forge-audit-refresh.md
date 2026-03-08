# Decision: Audit Refresh Findings

**By:** Forge  
**Date:** 2026-03-08  
**Context:** Component audit refresh after Run 14/15

## Findings for Team

1. **Substitution is no longer deferred** — it has a working implementation. CONTROL-COVERAGE.md already reflects this. Only Xml remains deferred.

2. **RouteData script bug is P1** — Run 15 revealed that `[Parameter] // TODO: Verify RouteData` conversion absorbs the closing parenthesis in code-behind files (ProductDetails.razor.cs:36, ProductList.razor.cs:37). This causes build failures. Cyclops should fix before Run 16.

3. **ID rendering needs to extend to data controls** — 9 components have it, but DetailsView, GridView, DropDownList, FormView, DataList, DataGrid, ListView, HiddenField still don't render `id` attributes. These are WingtipToys-active controls.

4. **Layer 2 automation is now feasible** — the same 3 semantic fixes have been stable across 4 runs. A Layer 2 script or Copilot skill could eliminate all remaining manual fixes.

5. **Style sub-component documentation gap** — 66 components with zero standalone docs. A single "Styling Components" utility page would be high-value/low-effort.

6. **RadioButton sample page is missing** from AfterBlazorServerSide (RadioButtonList exists but not RadioButton).
