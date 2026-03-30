# Event Handler Wiring Migration

The migration script automatically adds the `@` prefix to event handler attributes in markup, enabling proper Blazor event binding. This transforms `OnClick="Handler"` into `OnClick="@Handler"` so the Razor compiler can resolve the method reference.

## Overview

**What it does:**
- Adds `@` prefix to event handler attribute values in converted markup
- Targets `On[A-Z]*` attributes with bare method name values
- Skips attributes that already have `@` or contain non-identifier characters

**Why it matters:**
In Web Forms, `OnClick="Button_Click"` in markup is a string that the runtime resolves at page compile time. In Blazor, the `@` prefix is required to tell the Razor compiler to treat the value as a C# expression (a method group), not a string literal. Without `@`, you get compilation errors.

## The Transform

### Before (Web Forms)

```html
<asp:Button ID="btnSave" Text="Save" OnClick="Save_Click" runat="server" />
<asp:DropDownList ID="ddlCategory" OnSelectedIndexChanged="Category_Changed" runat="server" />
<asp:GridView ID="gvItems" OnRowCommand="Grid_RowCommand" runat="server" />
```

### After (Blazor — Automated)

```html
<Button id="btnSave" Text="Save" OnClick="@Save_Click" />
<DropDownList id="ddlCategory" OnSelectedIndexChanged="@Category_Changed" />
<GridView id="gvItems" OnRowCommand="@Grid_RowCommand" />
```

## Supported Event Attributes

The transform handles all Web Forms event attributes that follow the `On[A-Z]*` naming convention:

| Event Attribute | Controls | Description |
|----------------|----------|-------------|
| `OnClick` | Button, LinkButton, ImageButton | Click handler |
| `OnTextChanged` | TextBox | Text change handler |
| `OnSelectedIndexChanged` | DropDownList, ListBox, RadioButtonList, CheckBoxList | Selection change |
| `OnCheckedChanged` | CheckBox, RadioButton | Check state change |
| `OnRowCommand` | GridView | Row command handler |
| `OnRowEditing` | GridView | Row edit begin |
| `OnRowUpdating` | GridView | Row update handler |
| `OnRowDeleting` | GridView | Row delete handler |
| `OnRowCancelingEdit` | GridView | Row edit cancel |
| `OnPageIndexChanging` | GridView | Paging handler |
| `OnSorting` | GridView | Sort handler |
| `OnItemCommand` | Repeater, DataList | Item command handler |

## Combined with Signature Transforms

This markup-side transform works together with the [Event Handler Signatures](Phase2-EventHandlerSignatures.md) code-behind transform. Together they handle the full event migration:

1. **Markup:** `OnClick="Save_Click"` → `OnClick="@Save_Click"` (this transform)
2. **Code-behind:** `protected void Save_Click(object sender, EventArgs e)` → `protected void Save_Click()` (signature transform)

## How the Script Detects Event Handlers

The regex pattern `(On[A-Z]\w+)="([A-Za-z_]\w*)"` matches:

- **Attribute name** starts with `On` followed by an uppercase letter (e.g., `OnClick`, `OnRowCommand`)
- **Attribute value** is a bare C# identifier (e.g., `Save_Click`, `Grid_RowCommand`)

Values that are **not** transformed:
- Already prefixed: `OnClick="@Save_Click"` — no change
- String values: `OnClick="javascript:doSomething()"` — contains `:()`, not an identifier
- Empty values: `OnClick=""` — nothing to prefix
- Expressions: `OnClick="@(() => HandleClick())"` — starts with `@`

## Test Coverage

- **TC20** — Standard event handlers (OnClick on Button, LinkButton)
- **TC21** — Specialized event handlers (OnRowCommand, OnPageIndexChanging on GridView)
- **TC24** — Multiple event types (OnTextChanged, OnSelectedIndexChanged, OnCheckedChanged, OnClick)
- **TC25** — Combined with DataBind pattern (event wiring + data binding on same page)

## See Also

- [Event Handler Signatures](Phase2-EventHandlerSignatures.md) — Code-behind signature transforms
- [DataBind Pattern Conversion](Phase3-DataBindConversion.md) — DataSource/DataBind → Items binding
- [Automated Migration Guide](AutomatedMigration.md) — Full list of automated transforms
