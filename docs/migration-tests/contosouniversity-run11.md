# ContosoUniversity Run 11

| Metric | Value |
|--------|-------|
| Date | 2026-03-09 |
| Branch | `squad/audit-docs-perf` |
| Score | **Build succeeds (0 errors)** |
| Render Mode | Static SSR |
| Time | Layer 1: 0.80s, Layer 2: 0.48s |

## Key Changes

- Validated existing AfterContosoUniversity sample
- Fresh migration demonstrates Pattern D ItemType injection
- Color attribute fix eliminated 47 build errors

## What Went Right

- Build succeeds with only 4 warnings
- Color attributes properly converted
- Pattern D injected ItemType into 4 .razor files
- EDMX scaffold command generated for DB-first migration

## What Needs Work

- DropDownList uses TItem (not ItemType) — Pattern D needs update
- DB-first apps need manual EF Core package addition
- ContentTemplate component not yet in BWFC

## Comparison with Previous Runs

| Run | Errors | Notes |
|-----|--------|-------|
| Run 09 | 68 | Color attributes breaking Razor |
| Run 10 | 21 | After color fix |
| **Run 11** | 0 | Build succeeds |

## Full Report

See [`dev-docs/migration-tests/contosouniversity-run11-2026-03-09/REPORT.md`](../../dev-docs/migration-tests/contosouniversity-run11-2026-03-09/REPORT.md)
