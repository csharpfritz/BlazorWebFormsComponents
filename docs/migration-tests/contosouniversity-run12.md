# ContosoUniversity Run 12

| Metric | Value |
|--------|-------|
| **Date** | 2026-03-09 |
| **Branch** | squad/audit-docs-perf |
| **Score** | 40/40 (100%) |
| **Render Mode** | SSR + InteractiveServer |
| **Total Time** | ~44s |

## Summary

ContosoUniversity migration achieves 100% functional test pass rate. All CRUD operations work. CSS layout is broken but functionality is complete.

## Key Results

- ✅ 40/40 acceptance tests pass
- ✅ BWFC validation: 9 files scanned, 0 violations
- ✅ 5 BWFC components in use: Button, DetailsView, DropDownList, GridView, TextBox
- ⚠️ CSS layout broken (nav vertical, footer mispositioned)

## What Worked

- Students, Courses, Instructors pages all functional
- GridView displays data correctly
- DropDownList filtering works (filter by department)
- Search functionality works
- Add/Edit/Delete operations work
- Uses original SQL Server LocalDB (not forced to SQLite)

## Issues Found

1. **CSS layout broken** - Original CSS selectors don't match Blazor HTML structure
   - Navigation renders as vertical bullets instead of horizontal bar
   - Footer appears in wrong position
   - CSS files load (verified) but rules don't apply correctly

2. **Root cause** - Web Forms Site.Master HTML structure differs from Blazor MainLayout.razor

## Recommendations

- Manual CSS fix needed to add explicit list styling
- Or add wrapper divs to match original HTML structure

## Full Report

See [dev-docs/migration-tests/contosouniversity-run12-2026-03-09/REPORT.md](../dev-docs/migration-tests/contosouniversity-run12-2026-03-09/REPORT.md)
