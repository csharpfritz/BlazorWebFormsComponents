# Decision: Layer 2 Documentation Must Use Three-State Model

**By:** Beast
**Date:** 2026-03-08
**Context:** Run 16 migration report and toolkit documentation update

## Decision

When documenting Layer 2 automation, use a **three-state model** instead of binary (automated/manual):

| State | Meaning | Example |
|-------|---------|---------|
| ✅ Fully automated | Script handles end-to-end, no manual work | Pattern C (Program.cs) |
| ⚠️ Partially automated | Script creates correct structure, content needs overlay | Pattern A (code-behinds) |
| ❌ Not yet automated | Script doesn't detect candidates, full manual overlay needed | Pattern B (auth forms) |

## Why

The Layer 2 script (`bwfc-migrate-layer2.ps1`) introduced in Run 16 creates a new documentation challenge. The pipeline is no longer "script does mechanical work, humans do semantic work." Now there's a middle ground where the script does *some* semantic work correctly (scaffolding) but not all of it (entity types, parameters). Documenting this as simply "automated" would set wrong expectations; documenting it as "manual" would undersell the progress.

## Impact

- All migration-toolkit docs (README, METHODOLOGY, QUICKSTART) updated to reflect this model
- Future run reports should track each pattern's state independently
- As patterns move from ❌ → ⚠️ → ✅, the documentation should be updated accordingly
