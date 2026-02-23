### 2026-02-23: Label AssociatedControlID switches rendered element
**By:** Cyclops
**What:** Label renders `<label for="{AssociatedControlID}">` when AssociatedControlID is set, `<span>` when not. All style/id/accesskey attributes apply to whichever element renders.
**Why:** Matches Web Forms behavior exactly. Important for accessibility — screen readers use `<label for>` to associate labels with inputs. No breaking change — default behavior (no AssociatedControlID) still renders `<span>`.

### 2026-02-23: Login controls inherit BaseStyledComponent (Option A)
**By:** Cyclops
**What:** Login, ChangePassword, and CreateUserWizard changed from `BaseWebFormsComponent` to `BaseStyledComponent`. Outer `<table>` elements now render CssClass and computed IStyle inline styles alongside the existing `border-collapse:collapse;`.
**Why:** Option A (base class change) was chosen over Option B (direct IStyle implementation) because `BaseStyledComponent` extends `BaseWebFormsComponent` — no functionality is lost. The `[Parameter]` style properties on the outer control do NOT conflict with `[CascadingParameter]` sub-styles (TitleTextStyle, LabelStyle, etc.) because they operate through completely different Blazor mechanisms. PasswordRecovery should follow the same pattern when it's ready.
