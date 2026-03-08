### Second sample project: Build "EventManager" Control Gallery

**By:** Forge
**What:** Recommend building a purpose-built "EventManager" sample application targeting ~12-15 pages with SQL Server LocalDB, exercising controls WingtipToys doesn't cover: TreeView, Menu, SiteMapPath, SiteMapDataSource, Wizard, Calendar, DataList, Repeater, RadioButtonList, CheckBoxList, BulletedList, AdRotator, Panel with GroupingText, MultiView/View. Location: `samples/EventManager/` (Web Forms) + `samples/AfterEventManager/` (Blazor migrated). No suitable existing open-source Web Forms sample was found after extensive search.
**Why:** WingtipToys covers data display/input/login controls but doesn't exercise navigation controls, wizard flows, calendar, or list controls. The open-source Web Forms sample landscape is barren — every candidate either overlaps with WingtipToys or requires building from scratch. A purpose-built sample gives us zero license risk, controlled complexity, and exact control coverage targeting.

### ASPX URL rewriting: Document in migration-toolkit, not BWFC NuGet

**By:** Forge
**What:** ASPX URL preservation should be a documented `RewriteOptions.AddRedirect` snippet in `migration-toolkit/METHODOLOGY.md` and `migration-toolkit/CHECKLIST.md`, not a NuGet-installable middleware in the BWFC library. Recommended code: `new RewriteOptions().AddRedirect(@"^(.+)\.aspx$", "$1", statusCode: 301)` placed before `app.UseRouting()`. Includes `Default.aspx → /` special case. Query strings are automatically preserved. 301 redirect preferred over transparent rewrite for SEO.
**Why:** URL rewriting is a migration infrastructure concern (~20 lines of code), not a Blazor component. It belongs in the migration toolkit as guidance developers understand, apply, and eventually remove. The BWFC NuGet package should remain focused on components. No existing NuGet package targets this use case. All five approaches were evaluated (RewriteOptions, custom middleware, IRule, @page directive, catch-all route) — RewriteOptions with AddRedirect is the simplest and most correct.
