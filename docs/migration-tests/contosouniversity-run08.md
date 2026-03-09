# ContosoUniversity Run 08

## Migration Summary

| Metric | Value |
|--------|-------|
| Date | 2026-03-09 |
| Branch | `squad/audit-docs-perf` |
| Score | **40/40 (100%)** |
| Render Mode | InteractiveServer |
| Total Time | ~30 minutes |

## Key Changes from Run 07

1. **DetailsView Items Binding** — Fixed: Use `Items="new[] { item }"` instead of `DataItem`
2. **Search Improvements** — Added partial matching, proper element IDs
3. **Test Timing** — Added `WaitForBlazorCircuit()` to search tests

## What Went Right

- All 40 acceptance tests pass
- EF Core migration from EF6 EDMX worked smoothly
- URL rewriting provides seamless legacy URL support
- GridView sorting renders clickable headers

## What Went Wrong

- DetailsView doesn't support `DataItem` property — must wrap single items in array
- Tests needed timing updates for Blazor circuit initialization
- Button `UseSubmitBehavior="true"` (default) can interfere with Blazor event handling

## Full Report

See [dev-docs/migration-tests/contosouniversity-run08-2026-03-09/REPORT.md](../../dev-docs/migration-tests/contosouniversity-run08-2026-03-09/REPORT.md) for complete details.
