### 2026-03-09: Migration validation requirements
**By:** Jeff Fritz (via Copilot)

**What:**
1. ContosoUniversity migrations must include running `src/ContosoUniversity.AcceptanceTests` (40 tests) as a validation step
2. WingtipToys migrations must include running `src/WingtipToys.AcceptanceTests` (25 tests) as a validation step
3. All migrations must run `bwfc-validate.ps1` to verify asp: controls are converted to BWFC components — NOT plain HTML
4. Migration is NOT considered complete until:
   - All acceptance tests pass
   - BWFC validator reports no violations
   - No unconverted asp: controls remain

**Implementation:**
- Created `migration-toolkit/scripts/bwfc-validate.ps1` — validates BWFC component usage
- Updated `webforms-migration` skill with Steps 9-10 for validation and testing

**Why:** User directive — ensures migration quality, BWFC component usage, and functional correctness
