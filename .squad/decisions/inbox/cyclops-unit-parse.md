# Decision: Unit string operator — implicit + Parse() delegation

**Date:** 2026-07-25
**Author:** Cyclops
**Issue:** #440
**PR:** #70

## Context

The `Unit` struct had an explicit string operator that only handled integer strings (e.g., `"100"`) and threw on CSS unit strings like `"125px"`. This was fixed in prior commits (`c3cc972e`, `e8446caa`) by switching to an implicit operator that delegates to `Unit.Parse()`.

## Decision

The implicit string-to-Unit operator delegates to `Unit.Parse()`, which handles all CSS unit formats (px, pt, pc, in, mm, cm, %, em, ex), decimals, negatives, and whitespace. This matches the .NET Framework `System.Web.UI.WebControls.Unit` behavior.

## Rationale

- `Unit.Parse()` already contained the full parsing logic (digit extraction, suffix-to-UnitType mapping, culture-aware numeric conversion)
- Duplicating that logic in the operator was unnecessary and error-prone
- Implicit (not explicit) matches Web Forms behavior where string-typed attributes bind directly to Unit properties

## Impact

Any component property of type `Unit` can now accept CSS strings directly in markup (e.g., `Width="125px"`) without requiring explicit casts or `Unit.Parse()` calls.
