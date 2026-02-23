# Decision: Milestone 6 Work Plan — Feature Gap Closure

**By:** Forge
**Date:** 2026-02-14
**Status:** Proposed

## What

Milestone 6 work plan with 54 work items across 3 priority tiers, targeting ~345 feature gaps identified in the 53-control audit (SUMMARY.md). Full plan at `planning-docs/MILESTONE6-PLAN.md`.

### P0 — Base Class Fixes (18 WIs, ~180 gaps)
Seven base class changes that sweep across many controls:
1. `AccessKey` on `BaseWebFormsComponent` (~40 gaps)
2. `ToolTip` on `BaseWebFormsComponent` (~35 gaps)
3. `DataBoundComponent<T>` → inherit `BaseStyledComponent` (~70 gaps)
4. `Display` enum on `BaseValidator` (6 gaps)
5. `SetFocusOnError` on `BaseValidator` (6 gaps)
6. `Image` → `BaseStyledComponent` (11 gaps)
7. `Label` → `BaseStyledComponent` (11 gaps)

### P1 — Individual Control Improvements (28 WIs, ~120 gaps)
- GridView overhaul: paging, sorting, inline row editing (most-used data control, currently 20.7% health)
- Calendar: string styles → TableItemStyle sub-components + enum conversion
- FormView: CssClass, header/footer, empty data templates
- HyperLink: `NavigationUrl` → `NavigateUrl` rename (migration blocker)
- ValidationSummary: HeaderText, ShowSummary, ValidationGroup
- PasswordRecovery audit doc re-run (was 0% due to pre-merge timing)
- Docs + integration tests for all changed controls

### P2 — Nice-to-Have (8 WIs, ~45 gaps)
ListControl format strings, Menu Orientation, Label AssociatedControlID, Login controls outer styles, CausesValidation on CheckBox/RadioButton/TextBox.

## Key Scope Decisions
- **Login controls outer styles → P2** (not P1): These controls use CascadingParameter sub-styles by convention. Outer wrapper styling is useful but lower priority than GridView/Calendar/FormView.
- **Skip Substitution and Xml**: Per existing team decision, both remain permanently deferred.
- **sprint3 merge is DONE**: DetailsView and PasswordRecovery are on the branch. Only the PasswordRecovery audit doc needs updating.

## Why

The audit shows 66.3% overall health with 597 missing features. P0 base class fixes are the highest-ROI work — 7 changes close ~180 gaps. GridView at 20.7% is the single biggest migration blocker and must be addressed. Expected outcome: overall health rises to ~85%.

## Agents

All 6 agents involved: Cyclops (implementation), Rogue (bUnit tests), Jubilee (samples), Beast (docs), Colossus (integration tests), Forge (PasswordRecovery re-audit + review).
