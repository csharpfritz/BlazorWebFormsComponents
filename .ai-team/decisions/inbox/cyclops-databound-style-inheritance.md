# Decision: BaseDataBoundComponent inherits BaseStyledComponent

**By:** Cyclops
**Date:** 2026-02-23
**Work Item:** WI-07

## What

Changed the inheritance chain from:
```
DataBoundComponent<T> → BaseDataBoundComponent → BaseWebFormsComponent
```
To:
```
DataBoundComponent<T> → BaseDataBoundComponent → BaseStyledComponent → BaseWebFormsComponent
```

This gives all data-bound controls the full IStyle property set (BackColor, BorderColor, BorderStyle, BorderWidth, CssClass, ForeColor, Font, Height, Width) from the base class.

## Controls affected

Removed duplicate IStyle declarations and style properties from:
- **GridView** — removed CssClass
- **DetailsView** — removed CssClass
- **DataGrid** — removed CssClass
- **DataList** — removed IStyle + 9 style properties; kept `new string Style` parameter
- **TreeView** — removed IStyle + 9 style properties
- **AdRotator** — removed IStyle + 9 style properties + Style computed property
- **BulletedList** — removed IStyle + 9 style properties + Style computed property
- **CheckBoxList** — removed IStyle + 9 style properties + Style computed property
- **DropDownList** — removed IStyle + 9 style properties + Style computed property
- **ListBox** — removed IStyle + 9 style properties + Style computed property
- **RadioButtonList** — removed IStyle + 9 style properties + Style computed property

No changes needed:
- **FormView** — no duplicate properties
- **ListView** — only added `new` keyword to existing obsolete Style parameter
- **Repeater** — no duplicate properties

## Why

Data controls in Web Forms inherit from `DataBoundControl → WebControl`, which provides style properties. Our `BaseDataBoundComponent` was missing this, forcing each control to implement IStyle independently with duplicate property declarations. This caused ~70 style property gaps and made maintenance harder.

## Impact

- 949/949 tests pass — zero regressions
- All existing style rendering in templates (DataList, DetailsView, etc.) continues to work unchanged
- Controls that don't yet render styles in their templates can add rendering later per-control
