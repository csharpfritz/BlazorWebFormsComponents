### 2026-02-23: CausesValidation on non-button controls follows ButtonBaseComponent pattern
**By:** Cyclops
**What:** CheckBox, RadioButton, and TextBox now have `CausesValidation`, `ValidationGroup`, and `ValidationGroupCoordinator` cascading parameter — same 3-property pattern used by ButtonBaseComponent. Validation fires in existing `HandleChange` methods for CheckBox/RadioButton. TextBox has the parameters but no trigger wiring because it lacks an `@onchange` binding.
**Why:** Web Forms exposes CausesValidation/ValidationGroup on all postback-capable controls. Following the exact ButtonBaseComponent pattern (same property names, same cascading parameter name, same coordinator call) ensures consistency and lets the existing ValidationGroupProvider work with these controls without modification.

### 2026-02-23: Menu Orientation uses CSS class approach, not inline styles
**By:** Cyclops
**What:** Menu horizontal layout is achieved by adding a `horizontal` CSS class to the top-level `<ul>` and a scoped CSS rule `ul.horizontal > li { display: inline-block; }`. The `Orientation` enum lives at `Enums/Orientation.cs` (Horizontal=0, Vertical=1). Default is Vertical.
**Why:** CSS class approach is cleaner than inline styles and matches how Web Forms Menu generates different class-based layouts for orientation. The enum follows project convention (explicit integer values, file in Enums/). Default Vertical matches Web Forms default.
