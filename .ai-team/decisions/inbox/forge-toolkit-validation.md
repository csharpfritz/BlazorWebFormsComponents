### 2026-03-09: Migration skills updated — SSR is canonical render mode

**By:** Forge
**What:** All migration-toolkit skills and team migration-standards updated to reflect SSR (Static Server Rendering) as the canonical render mode, replacing Global Server Interactive. Skills now document the 2-script pipeline (`bwfc-migrate.ps1` + `bwfc-migrate-layer2.ps1`), `-TestMode` switch, RelPath route generation, `[Parameter]` TODO separate-line format, and Layer 2 patterns (A/B/C). Confidence upgraded from "medium" to "high" based on 9 runs and 5 consecutive 100% results.
**Why:** The render mode guidance was stale since Run 12 (6 runs ago). New migration users following the old "Global Server Interactive" advice would hit HttpContext/cookie/session failures that SSR was specifically chosen to eliminate. The 2-script pipeline from Run 16 and script fixes from Runs 14–15 were also not documented in any skill.

### 2026-03-09: CONTROL-COVERAGE.md should list Xml as deferred

**By:** Forge
**What:** Added Xml control to CONTROL-COVERAGE.md Editor Controls table with "⏸️ Deferred" status. Previously Xml was only documented in `docs/DeferredControls.md` but not mentioned in the primary coverage reference.
**Why:** Completeness — developers checking CONTROL-COVERAGE.md to assess migration feasibility should see all known controls, including deferred ones, in one place.
