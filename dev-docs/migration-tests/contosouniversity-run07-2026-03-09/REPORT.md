# ContosoUniversity Migration Run 07

**Date:** 2026-03-09
**Branch:** `squad/audit-docs-perf`
**Commit:** `f08c7a6f`
**Score:** **40/40 (100%)** ✅

## Executive Summary

Run 07 of the ContosoUniversity migration achieved 100% acceptance test passage (40/40). The migration scripts (Layer 1 + Layer 2) completed in under 2 seconds total. Manual fixes required ~20 minutes, primarily addressing a BWFC DetailsView incompatibility and Blazor circuit timing issues in tests.

## Timing

| Phase | Duration | Notes |
|-------|----------|-------|
| **Layer 1 (markup)** | 0.99 seconds | 72 transforms, 6 files |
| **Layer 2 (code-behind)** | 0.40 seconds | 7 transforms |
| **Manual fixes** | ~20 minutes | Build errors, model fixes, test timing |
| **Total automated** | **1.39 seconds** | |

## Test Results

| Category | Passed | Failed | Notes |
|----------|--------|--------|-------|
| About Page | 5/5 | 0 | Enrollment stats display |
| Instructors Page | 5/5 | 0 | GridView with sorting |
| Courses Page | 6/6 | 0 | Search + DetailsView fix |
| Students Page | 9/9 | 0 | CRUD operations |
| Navigation | 7/7 | 0 | Routing + nav links |
| Home Page | 8/8 | 0 | Welcome page |
| **TOTAL** | **40/40** | **0** | **100%** |

## Issues Discovered & Fixed

### 1. DetailsView BoundField Incompatibility (Critical)

**Problem:** BWFC `DetailsView` uses an internal field system (`DetailsViewField` subclasses) rather than the standard `BoundField` component used by `GridView`. Placing `BoundField` inside `<Fields>` causes:
```
NullReferenceException: Object reference not set to an instance of an object.
   at BlazorWebFormsComponents.BaseColumn`1.OnInitialized()
```

**Root Cause:** `BoundField` looks for a `CascadingParameter(Name = "ColumnCollection")` but `DetailsView` provides `DetailsViewFieldCollection` instead.

**Solution:** Replaced DetailsView with plain HTML table:
```razor
<!-- Before (broken) -->
<DetailsView ID="dtlCourses" DataItem="_selectedCourse">
    <Fields>
        <BoundField DataField="CourseID" HeaderText="Course ID" />
    </Fields>
</DetailsView>

<!-- After (working) -->
<table id="dtlCourses" class="details">
    <tr><td>Course ID</td><td>@_selectedCourse.CourseID</td></tr>
</table>
```

**Recommendation:** Document that DetailsView `<Fields>` parameter expects `DetailsViewAutoField` or custom implementations, NOT `BoundField`.

### 2. Button OnClick Handler Signature

**Problem:** Button `OnClick` handlers in InteractiveServer mode need `MouseEventArgs` parameter:
```csharp
// Wrong - causes EventCallback type mismatch
private async Task SearchCourseByName() { }

// Correct
private async Task SearchCourseByName(MouseEventArgs args) { }
```

**Solution:** Added `using Microsoft.AspNetCore.Components.Web` and `MouseEventArgs` to handler signatures.

### 3. Nullable String Properties

**Problem:** Database has NULL values but models had non-nullable `string` with `= ""` initializers, causing `SqlNullValueException`.

**Solution:** Changed all string properties to `string?`:
```csharp
public string? CourseName { get; set; }
```

### 4. LINQ Contains with Null Check

**Problem:** `Contains()` query on nullable string column fails:
```csharp
// Wrong
c.CourseName.Contains(_searchCourse)

// Correct
c.CourseName != null && c.CourseName.Contains(_searchCourse)
```

### 5. Playwright Test Timing for Blazor

**Problem:** Tests for InteractiveServer pages fail because Blazor SignalR circuit isn't established before interactions.

**Solution:** Added explicit waits:
```csharp
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
await page.WaitForTimeoutAsync(1000);  // Wait for Blazor circuit
// ... interact ...
await page.WaitForTimeoutAsync(500);   // Wait for re-render
```

## Files Modified

### New Files Created
- `Models/ContosoUniversityContext.cs` - EF Core DbContext with SQL Server
- `Pages/About.razor` + `.cs` - About page with enrollment stats
- `Pages/Courses.razor` + `.cs` - Courses page with search
- `Pages/Home.razor` - Home page
- `Pages/Instructors.razor` + `.cs` - Instructors with GridView sorting
- `Pages/Students.razor` + `.cs` - Student CRUD operations
- `appsettings.Development.json` - Detailed error logging

### Modified Files
- `ContosoUniversity.csproj` - Added EF Core SQL Server package
- `Program.cs` - DbContextFactory, URL rewriting, middleware
- `_Imports.razor` - Added model namespace
- `Components/Layout/MainLayout.razor` - Fixed nav links

### Test Files Modified
- `src/ContosoUniversity.AcceptanceTests/CoursesPageTests.cs` - Added Blazor circuit timing waits

## Key Learnings

1. **BWFC DetailsView is not column-compatible with GridView** - They share similar syntax but use different internal field systems
2. **InteractiveServer handlers need MouseEventArgs** - EventCallback<MouseEventArgs> requires the parameter
3. **Playwright tests for Blazor need explicit timing** - NetworkIdle isn't enough; must wait for SignalR circuit
4. **SQL Server nullable columns need nullable C# types** - Even with empty string initializers, EF Core can return null

## Comparison to Previous Runs

| Run | Score | Key Improvement |
|-----|-------|----------------|
| Run 01-03 | 31/40 (77.5%) | Initial migration |
| Run 04 | 36/40 (90%) | URL rewriting |
| Run 05 | 40/40 (100%) | SQL Server, InteractiveServer |
| **Run 07** | **40/40 (100%)** | DetailsView fix, test timing |

## Recommendations

1. **Update migration skills** to document DetailsView limitations
2. **Add DetailsViewBoundField component** to BWFC library (or document the limitation)
3. **Update Layer 2 script** to add MouseEventArgs to button handlers automatically
4. **Add Blazor circuit wait helper** to acceptance test infrastructure
