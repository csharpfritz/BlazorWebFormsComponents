### 2026-03-09: Migration tooling directives
**By:** Jeff Fritz (via Copilot)
**What:**
1. **Project reference argument** — Always use `-BwfcProjectPath` when running migrations in test mode
2. **OnClick pattern** — Follow documented pattern (BWFC Button uses `OnClick` not `@onclick`)
3. **Database provider** — Strong preference to maintain same provider (e.g., localdb) unless explicitly directed otherwise
4. **EF6 → EF Core cleanup** — Need post-migration cleanup scripts
5. **Unsupported controls** — Always list in migration reports for library prioritization
6. **AddHttpContextAccessor** — ALWAYS add to Program.cs in every migration
7. **ItemType parameter** — Investigate what's actually wrong (GridView/BoundField need explicit type)

**Why:** Run 06 revealed script gaps that should have been caught. These are permanent rules for the migration toolkit.
