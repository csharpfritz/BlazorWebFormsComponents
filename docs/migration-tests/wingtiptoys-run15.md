# WingtipToys Run 15

| Metric | Value |
|--------|-------|
| **Date** | 2026-03-09 |
| **Branch** | squad/audit-docs-perf |
| **Score** | 25/25 (100%) |
| **Render Mode** | SSR (default) |
| **Total Time** | ~30s |

## Summary

Validation run confirming the existing known-good WingtipToys migration passes all acceptance tests.

## Key Results

- ✅ 25/25 acceptance tests pass
- ✅ BWFC validation: 35 files scanned, 0 violations
- ✅ 5 BWFC components in use: Button, Label, ListView, LoginView, Panel
- ⚠️ 19 stub pages need manual implementation
- ⚠️ ShoppingCart uses HTML table (should use GridView)

## What Worked

- Full CSS styling loads correctly
- ListView displays products with images and prices
- Add to Cart / Remove functionality works
- Session-based shopping cart persists items
- Login/Register forms render correctly

## Issues Found

1. Layer 2 script generates invalid class names for dotted filenames
2. Duplicate code-behinds conflict with @code blocks
3. Stub pages get code-behinds that reference non-existent entities

## Full Report

See [dev-docs/migration-tests/wingtiptoys-run15-2026-03-09/REPORT.md](../dev-docs/migration-tests/wingtiptoys-run15-2026-03-09/REPORT.md)
