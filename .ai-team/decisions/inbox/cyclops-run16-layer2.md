# Decision: Layer 2 Script Bug Fixes and Capability Assessment

**Author:** Cyclops (Component Dev)
**Date:** 2026-03-09
**Run:** 16

## Context

The Layer 2 automation script (`bwfc-migrate-layer2.ps1`) had two bugs preventing reliable use:
1. `$listField` variable uninitialized in single-item code paths, causing terminating errors under `Set-StrictMode`
2. `-TestMode` parameter redirected output to `Layer2Output/` subdirectory — inconsistent with Layer 1's `-TestMode` semantics

## Decisions

### 1. Initialize `$listField` before branching
Added `$listField = '_items'` default before the `if ($isSingleItem)` block. This ensures the variable exists in all code paths, particularly the fallback `else` branch of `OnInitializedAsync` generation.

### 2. Remove `-TestMode` output redirect entirely
Layer 2 always modifies in-place after Layer 1 output. The `Layer2Output/` redirect created confusion and wasn't useful — developers should use `-WhatIf` for dry runs instead. Removed: parameter, `Get-OutputPath` redirect, directory creation, log path redirect, and conditional code-behind removal guards.

### 3. Known-good overlay remains necessary for Run 16
Layer 2 Pattern A generates non-compilable code (broken parameter declarations, wrong entity types for non-data pages). All 26 generated code-behinds required replacement from cef51da3. The script's Pattern B detected 0 auth pages. Only Pattern C (Program.cs) produced usable output.

## Impact

- Layer 2 script no longer crashes under strict mode
- `-TestMode` behavior is now consistent between Layer 1 and Layer 2 (Layer 2 simply doesn't have it)
- Future work needed: Pattern A parameter parsing and entity type detection need significant improvement before the script can produce build-ready output autonomously
