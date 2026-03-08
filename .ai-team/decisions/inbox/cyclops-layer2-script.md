### 2026-03-09: Layer 2 automation script architecture
**By:** Cyclops
**What:** Created `bwfc-migrate-layer2.ps1` as a separate script (not merged into Layer 1). It applies 3 semantic transforms: FormView→ComponentBase+DI, auth form simplification, Program.cs bootstrap generation. Uses `// Layer2-transformed` marker for idempotency. Accepts same `-Path`, `-TestMode`, `-WhatIf` flags as Layer 1 for consistent UX.
**Why:** Layer 2 transforms require cross-file awareness (reading Models/ to detect DbContext, scanning for Identity patterns) that doesn't fit Layer 1's per-file pipeline. Keeping it separate means Layer 1 can improve independently without risking Layer 2 regressions. The 3 patterns have been stable across Runs 12–15, making them safe to automate.

### 2026-03-09: Route generation uses RelPath not FileName
**By:** Cyclops
**What:** `ConvertFrom-PageDirective` now builds routes from `$RelPath` (e.g., `Account/Login.aspx` → `/Account/Login`) instead of `$FileName` (which only gave `/Login`). Default/Index handling works at any directory depth.
**Why:** Pages in subdirectories (Account/, Admin/, Checkout/) were getting wrong routes. The fix aligns with `New-CompilableStub` which already used `$relativePath`.
