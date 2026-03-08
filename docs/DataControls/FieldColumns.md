# Field Column Components

Field column components define how individual columns are rendered inside data controls such as [GridView](GridView.md), [DataGrid](DataGrid.md), and [DetailsView](DetailsView.md). In ASP.NET Web Forms, these are the `<asp:BoundField>`, `<asp:TemplateField>`, `<asp:ButtonField>`, and `<asp:HyperLinkField>` controls that appear as children of a `<Columns>` element. This library provides Blazor equivalents that preserve the same names and attribute signatures.

Original Microsoft documentation:

- [BoundField](https://docs.microsoft.com/en-us/dotnet/api/system.web.ui.webcontrols.boundfield?view=netframework-4.8)
- [TemplateField](https://docs.microsoft.com/en-us/dotnet/api/system.web.ui.webcontrols.templatefield?view=netframework-4.8)
- [ButtonField](https://docs.microsoft.com/en-us/dotnet/api/system.web.ui.webcontrols.buttonfield?view=netframework-4.8)
- [HyperLinkField](https://docs.microsoft.com/en-us/dotnet/api/system.web.ui.webcontrols.hyperlinkfield?view=netframework-4.8)

## Shared Features (All Field Columns)

Every field column component inherits from `BaseColumn<ItemType>` and supports these common parameters:

| Parameter | Type | Description |
|-----------|------|-------------|
| `HeaderText` | `string` | Text displayed in the column header. |
| `SortExpression` | `string` | Expression used when the parent control supports sorting. |
| `Visible` | `bool` | Controls whether the column is rendered. Defaults to `true`. |

### Blazor Notes

- **ItemType cascading** — The generic `ItemType` parameter is automatically cascaded from the parent data control (GridView, DataGrid) to all child field columns. You do not need to specify it on each column unless you want to be explicit.
- **No `runat="server"`** — As with all Blazor components, the `runat="server"` attribute is not used.
- **Column ordering** — Columns render in the order they are declared, just like in Web Forms.

---

## BoundField

Displays the value of a data field as text. This is the most common column type and is the Blazor equivalent of `<asp:BoundField>`.

### Features Supported in Blazor

- Bind to a data property via `DataField`
- Format output with `DataFormatString`
- Nested (dot-notation) property access (e.g., `DataField="Address.City"`)
- Read-only mode in edit scenarios via `ReadOnly`
- Sort expression defaults to `DataField` when not explicitly set

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `DataField` | `string` | Name of the data property to display. Supports dot-notation for nested properties. |
| `DataFormatString` | `string` | A .NET format string applied to the value (e.g., `{0:C}` for currency, `{0:d}` for short date). |
| `ReadOnly` | `bool` | When `true`, the field renders as text even when the row is in edit mode. |
| `HeaderText` | `string` | Column header text. |
| `SortExpression` | `string` | Sort expression. Defaults to `DataField` if not set. |
| `Visible` | `bool` | Show or hide the column. |

### Web Forms Syntax

```html
<asp:BoundField
    DataField="Price"
    DataFormatString="{0:C}"
    HeaderText="Unit Price"
    ReadOnly="True"
    SortExpression="Price"
    runat="server" />
```

### Blazor Syntax

```razor
<BoundField
    DataField="Price"
    DataFormatString="{0:C}"
    HeaderText="Unit Price"
    ReadOnly="true"
    SortExpression="Price" />
```

### Example

```razor
<GridView DataSource="@Products" AutoGenerateColumns="false">
    <Columns>
        <BoundField DataField="Id" HeaderText="ID" />
        <BoundField DataField="Name" HeaderText="Product Name" />
        <BoundField DataField="Price" HeaderText="Price" DataFormatString="{0:C}" />
        <BoundField DataField="CreatedDate" HeaderText="Created" DataFormatString="{0:d}" />
    </Columns>
</GridView>

@code {
    private List<Product> Products = new()
    {
        new Product { Id = 1, Name = "Widget", Price = 9.99m, CreatedDate = DateTime.Now },
        new Product { Id = 2, Name = "Gadget", Price = 24.50m, CreatedDate = DateTime.Now.AddDays(-7) }
    };

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
```

---

## TemplateField

Allows fully custom content inside a column using Blazor `RenderFragment` templates. This is the Blazor equivalent of `<asp:TemplateField>`.

### Features Supported in Blazor

- Custom display content via `<ItemTemplate>`
- Custom edit-mode content via `<EditItemTemplate>`
- Access to the current row item through the template context variable
- Full Blazor component nesting inside templates

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `ItemTemplate` | `RenderFragment<ItemType>` | Template for rendering the cell in display mode. |
| `EditItemTemplate` | `RenderFragment<ItemType>` | Template for rendering the cell in edit mode. Falls back to `ItemTemplate` if not specified. |
| `HeaderText` | `string` | Column header text. |
| `SortExpression` | `string` | Sort expression for the column. |
| `Visible` | `bool` | Show or hide the column. |

### Blazor Notes

!!! note "Context Attribute"
    When using `<ItemTemplate>` or `<EditItemTemplate>`, use the `Context` attribute to name the template variable. For example, `Context="Item"` lets you reference `@Item.PropertyName`. Without it, the default name is `@context`.

### Web Forms Syntax

```html
<asp:TemplateField HeaderText="Actions">
    <ItemTemplate>
        <asp:HyperLink
            NavigateUrl='<%# "~/Products/" + Eval("Id") %>'
            Text="View"
            runat="server" />
    </ItemTemplate>
    <EditItemTemplate>
        <asp:TextBox Text='<%# Bind("Name") %>' runat="server" />
    </EditItemTemplate>
</asp:TemplateField>
```

### Blazor Syntax

```razor
<TemplateField HeaderText="Actions">
    <ItemTemplate Context="Item">
        <a href="/products/@Item.Id">View</a>
    </ItemTemplate>
    <EditItemTemplate Context="Item">
        <input type="text" value="@Item.Name" />
    </EditItemTemplate>
</TemplateField>
```

### Example

```razor
<GridView DataSource="@Products" AutoGenerateColumns="false">
    <Columns>
        <BoundField DataField="Name" HeaderText="Product" />
        <BoundField DataField="Price" HeaderText="Price" DataFormatString="{0:C}" />
        <TemplateField HeaderText="In Stock">
            <ItemTemplate Context="Item">
                @if (Item.InStock)
                {
                    <span style="color:green">&#10003; Yes</span>
                }
                else
                {
                    <span style="color:red">&#10007; No</span>
                }
            </ItemTemplate>
        </TemplateField>
        <TemplateField HeaderText="Actions">
            <ItemTemplate Context="Item">
                <a href="/products/@Item.Id">Details</a>
            </ItemTemplate>
        </TemplateField>
    </Columns>
</GridView>

@code {
    private List<Product> Products = new()
    {
        new Product { Id = 1, Name = "Widget", Price = 9.99m, InStock = true },
        new Product { Id = 2, Name = "Gadget", Price = 24.50m, InStock = false }
    };

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public bool InStock { get; set; }
    }
}
```

---

## ButtonField

Displays a button (push button, link button, or image button) in each row of a data control. This is the Blazor equivalent of `<asp:ButtonField>`. Clicking the button raises the parent control's `RowCommand` event.

### Features Supported in Blazor

- Three button styles via `ButtonType`: `ButtonType.Button`, `ButtonType.Link`, `ButtonType.Image`
- Command name and argument for server-side handling
- Data-bound button text with format strings
- Image buttons via `ImageUrl`

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `ButtonType` | `ButtonType` | The style of button to render. Options: `ButtonType.Button`, `ButtonType.Link` (default), `ButtonType.Image`. |
| `CommandName` | `string` | The command name passed to the parent control's row command event. |
| `DataTextField` | `string` | Data field used for button text. Supports comma-separated fields when used with `DataTextFormatString`. |
| `DataTextFormatString` | `string` | Format string applied to the data-bound text. |
| `ImageUrl` | `string` | URL of the image to display when `ButtonType` is `ButtonType.Image`. |
| `Text` | `string` | Static button text. Overridden by `DataTextFormatString` if specified. |
| `HeaderText` | `string` | Column header text. |
| `SortExpression` | `string` | Sort expression for the column. |
| `Visible` | `bool` | Show or hide the column. |

### Web Forms Syntax

```html
<asp:ButtonField
    ButtonType="Button"
    CommandName="Select"
    HeaderText="Actions"
    Text="Select"
    runat="server" />
```

### Blazor Syntax

```razor
<ButtonField
    ButtonType="ButtonType.Button"
    CommandName="Select"
    HeaderText="Actions"
    Text="Select" />
```

### Web Forms Features NOT Supported

- **CausesValidation** — Validation integration is not implemented for ButtonField.
- **ValidationGroup** — Not supported; use TemplateField with a Button component for validation scenarios.

### Example

```razor
<GridView DataSource="@Products"
          AutoGenerateColumns="false"
          OnRowCommand="HandleRowCommand">
    <Columns>
        <BoundField DataField="Name" HeaderText="Product" />
        <BoundField DataField="Price" HeaderText="Price" DataFormatString="{0:C}" />
        <ButtonField CommandName="Select" Text="Select" HeaderText="" />
        <ButtonField CommandName="Delete" Text="Remove" ButtonType="ButtonType.Button" HeaderText="" />
    </Columns>
</GridView>

@code {
    private List<Product> Products = new()
    {
        new Product { Name = "Widget", Price = 9.99m },
        new Product { Name = "Gadget", Price = 24.50m }
    };

    private void HandleRowCommand(GridViewCommandEventArgs e)
    {
        var commandName = e.CommandName;   // "Select" or "Delete"
        var rowIndex = e.CommandArgument;   // Row index
    }

    public class Product
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
```

---

## HyperLinkField

Displays a hyperlink in each row of a data control. Both the URL and the display text can be data-bound or static. This is the Blazor equivalent of `<asp:HyperLinkField>`.

### Features Supported in Blazor

- Static or data-bound URL via `NavigateUrl` / `DataNavigateUrlFormatString`
- Static or data-bound display text via `Text` / `DataTextFormatString`
- Multiple data fields in URL and text format strings
- Target window/frame control

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `DataNavigateUrlFields` | `string` | Comma-separated list of data field names used as arguments in `DataNavigateUrlFormatString`. |
| `DataNavigateUrlFormatString` | `string` | Format string for the URL (e.g., `"/products/{0}"`). |
| `DataTextField` | `string` | Data field used for the link display text. |
| `DataTextFormatString` | `string` | Format string for the display text. |
| `NavigateUrl` | `string` | Static URL. Overridden by `DataNavigateUrlFormatString` if specified. |
| `Target` | `string` | The target window or frame (e.g., `_blank`, `_self`). |
| `Text` | `string` | Static link text. Overridden by `DataTextFormatString` if specified. |
| `HeaderText` | `string` | Column header text. |
| `SortExpression` | `string` | Sort expression for the column. |
| `Visible` | `bool` | Show or hide the column. |

### Web Forms Syntax

```html
<asp:HyperLinkField
    DataNavigateUrlFields="Id"
    DataNavigateUrlFormatString="/products/{0}"
    DataTextField="Name"
    HeaderText="Product"
    Target="_blank"
    runat="server" />
```

### Blazor Syntax

```razor
<HyperLinkField
    DataNavigateUrlFields="Id"
    DataNavigateUrlFormatString="/products/{0}"
    DataTextField="Name"
    HeaderText="Product"
    Target="_blank" />
```

### Example

```razor
<GridView DataSource="@Products" AutoGenerateColumns="false">
    <Columns>
        <HyperLinkField
            DataTextField="Name"
            DataNavigateUrlFields="Id"
            DataNavigateUrlFormatString="/products/{0}"
            HeaderText="Product" />
        <BoundField DataField="Price" HeaderText="Price" DataFormatString="{0:C}" />
        <HyperLinkField
            Text="Search"
            DataNavigateUrlFields="Name,Category"
            DataNavigateUrlFormatString="https://example.com/search?name={0}&cat={1}"
            HeaderText="External Link"
            Target="_blank" />
    </Columns>
</GridView>

@code {
    private List<Product> Products = new()
    {
        new Product { Id = 1, Name = "Widget", Price = 9.99m, Category = "Tools" },
        new Product { Id = 2, Name = "Gadget", Price = 24.50m, Category = "Electronics" }
    };

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
    }
}
```

---

## Field Columns Not Yet Implemented

The following ASP.NET Web Forms field column types are **not yet available** in this library:

- **CommandField** — Provides Edit, Delete, and Select buttons. Use a [TemplateField](#templatefield) with [Button](../EditorControls/Button.md) or [LinkButton](../EditorControls/LinkButton.md) components as an alternative.
- **CheckBoxField** — Displays a checkbox for boolean values. Use a [TemplateField](#templatefield) with a [CheckBox](../EditorControls/CheckBox.md) component instead.
- **ImageField** — Displays an image from a URL field. Use a [TemplateField](#templatefield) with an [Image](../EditorControls/Image.md) component instead.

!!! tip "TemplateField Workaround"
    For any field type that is not yet implemented, `TemplateField` can be used to achieve the same result with full control over the rendered output.

    ```razor
    @* CheckBoxField equivalent *@
    <TemplateField HeaderText="Active">
        <ItemTemplate Context="Item">
            <CheckBox Checked="@Item.IsActive" Enabled="false" />
        </ItemTemplate>
    </TemplateField>

    @* ImageField equivalent *@
    <TemplateField HeaderText="Photo">
        <ItemTemplate Context="Item">
            <Image ImageUrl="@Item.PhotoUrl" AlternateText="@Item.Name" />
        </ItemTemplate>
    </TemplateField>

    @* CommandField equivalent *@
    <TemplateField HeaderText="Actions">
        <ItemTemplate Context="Item">
            <LinkButton CommandName="Edit" Text="Edit" />
            <LinkButton CommandName="Delete" Text="Delete" />
        </ItemTemplate>
    </TemplateField>
    ```

## Migration Notes

### Migrating from Web Forms

1. **Remove `asp:` prefix** — Change `<asp:BoundField>` to `<BoundField>`, `<asp:TemplateField>` to `<TemplateField>`, etc.
2. **Remove `runat="server"`** — Not needed in Blazor.
3. **Replace `Eval()` / `Bind()`** — Use the template context variable instead. For example, `<%# Eval("Name") %>` becomes `@Item.Name` (with `Context="Item"` on the template).
4. **Replace `CommandField`** — Use `TemplateField` with `Button` or `LinkButton` components.
5. **Replace `CheckBoxField`** — Use `TemplateField` with a `CheckBox` component.
6. **Update `ButtonType`** — Web Forms uses string values (`"Button"`, `"Link"`, `"Image"`). Blazor uses `ButtonType.Button`, `ButtonType.Link`, `ButtonType.Image`.

### Before (Web Forms)

```html
<asp:GridView ID="GridView1" runat="server" DataSourceID="SqlDataSource1"
              AutoGenerateColumns="False">
    <Columns>
        <asp:BoundField DataField="Name" HeaderText="Name" />
        <asp:BoundField DataField="Price" HeaderText="Price" DataFormatString="{0:C}" />
        <asp:TemplateField HeaderText="Details">
            <ItemTemplate>
                <asp:HyperLink NavigateUrl='<%# "~/Products/" + Eval("Id") %>'
                               Text="View" runat="server" />
            </ItemTemplate>
        </asp:TemplateField>
        <asp:CommandField ShowEditButton="True" ShowDeleteButton="True" />
    </Columns>
</asp:GridView>
```

### After (Blazor with BWFC)

```razor
<GridView DataSource="@Products" AutoGenerateColumns="false">
    <Columns>
        <BoundField DataField="Name" HeaderText="Name" />
        <BoundField DataField="Price" HeaderText="Price" DataFormatString="{0:C}" />
        <TemplateField HeaderText="Details">
            <ItemTemplate Context="Item">
                <a href="/products/@Item.Id">View</a>
            </ItemTemplate>
        </TemplateField>
        <TemplateField HeaderText="Actions">
            <ItemTemplate Context="Item">
                <LinkButton CommandName="Edit" Text="Edit" />
                <LinkButton CommandName="Delete" Text="Delete" />
            </ItemTemplate>
        </TemplateField>
    </Columns>
</GridView>
```

## See Also

- [GridView](GridView.md) — The most common data control that uses field columns
- [DataGrid](DataGrid.md) — Legacy data grid with field column support
- [DetailsView](DetailsView.md) — Detail view using BoundField and TemplateField
- [Databinder](../UtilityFeatures/Databinder.md) — The data binding utility used by field columns
