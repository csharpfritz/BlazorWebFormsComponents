---
name: bwfc-usercontrol-migration
description: "Migrate ASP.NET Web Forms User Controls (.ascx files) to Blazor components. Covers control properties → [Parameter], FindControl patterns → @ref, parent/child communication, event delegation, and Register directive conversion. WHEN: \"migrate .ascx\", \"user control to component\", \"FindControl to @ref\", \"ascx conversion\", \"control parameters\"."
confidence: low
version: 1.0.0
---

# Web Forms User Controls → Blazor Components

This skill covers migrating ASP.NET Web Forms User Controls (`.ascx` files) to Blazor components. These are **Layer 2 (L2) contextual transforms** that require understanding component communication patterns and state management.

**Related skills:**
- `/bwfc-migration` — Core markup migration (controls, expressions, layouts)
- `/bwfc-data-migration` — Architecture decisions, service injection
- `/bwfc-identity-migration` — Authentication and authorization migration

---

## When to Use This Skill

Use this skill when you encounter:
- `.ascx` files (User Controls) in the Web Forms project
- `<%@ Control %>` directives
- Public properties in user control code-behind
- `FindControl("controlId")` method calls
- `<%@ Register TagPrefix="uc" TagName="Ctrl" Src="~/Controls/MyCtrl.ascx" %>` directives

---

## 1. User Control → Blazor Component

### Web Forms Pattern

```html
<%@ Control Language="C#" CodeBehind="ProductCard.ascx.cs" Inherits="MyApp.Controls.ProductCard" %>

<div class="product-card">
    <h3><%= ProductName %></h3>
    <p class="price">$<%= Price.ToString("F2") %></p>
    <asp:Image ID="imgProduct" runat="server" />
    <asp:Button ID="btnAddToCart" Text="Add to Cart" OnClick="btnAddToCart_Click" runat="server" />
</div>
```

```csharp
// ProductCard.ascx.cs
namespace MyApp.Controls
{
    public partial class ProductCard : System.Web.UI.UserControl
    {
        // Public properties for parent to set
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        
        // Event for parent to handle
        public event EventHandler<int> AddToCartClicked;
        
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                imgProduct.ImageUrl = ImageUrl;
            }
        }
        
        protected void btnAddToCart_Click(object sender, EventArgs e)
        {
            AddToCartClicked?.Invoke(this, ProductId);
        }
    }
}
```

### Blazor Pattern: Component with Parameters

```razor
@* ProductCard.razor *@

<div class="product-card">
    <h3>@ProductName</h3>
    <p class="price">$@Price.ToString("F2")</p>
    <Image id="imgProduct" ImageUrl="@ImageUrl" />
    <Button id="btnAddToCart" Text="Add to Cart" OnClick="@HandleAddToCart" />
</div>

@code {
    // Public properties → [Parameter] properties
    [Parameter] public int ProductId { get; set; }
    [Parameter] public string ProductName { get; set; } = string.Empty;
    [Parameter] public decimal Price { get; set; }
    [Parameter] public string ImageUrl { get; set; } = string.Empty;
    
    // Event → EventCallback parameter
    [Parameter] public EventCallback<int> OnAddToCart { get; set; }
    
    private async Task HandleAddToCart()
    {
        await OnAddToCart.InvokeAsync(ProductId);
    }
}
```

### Property Migration Rules

| Web Forms User Control | Blazor Component | Notes |
|----------------------|------------------|-------|
| `public string Name { get; set; }` | `[Parameter] public string Name { get; set; }` | Add `[Parameter]` attribute |
| `public int Count { get; set; }` | `[Parameter] public int Count { get; set; }` | Works for all types |
| `protected string PrivateProp { get; set; }` | `private string PrivateProp { get; set; }` | Internal state — no `[Parameter]` |
| `public event EventHandler Clicked;` | `[Parameter] public EventCallback OnClick { get; set; }` | Event → EventCallback |
| `public event EventHandler<int> ItemSelected;` | `[Parameter] public EventCallback<int> OnItemSelected { get; set; }` | Typed EventCallback |

---

## 2. FindControl Pattern → @ref

### Web Forms Pattern

```csharp
// Web Forms — code-behind
protected void Page_Load(object sender, EventArgs e)
{
    var txtName = (TextBox)FindControl("txtName");
    txtName.Text = "Default Value";
    
    var lblMessage = (Label)FindControl("lblMessage");
    lblMessage.Text = "Welcome!";
}

protected void btnSave_Click(object sender, EventArgs e)
{
    var txtName = (TextBox)FindControl("txtName");
    var name = txtName.Text;
    SaveToDatabase(name);
}
```

### Blazor Pattern: Component References with @ref

```razor
@* Blazor component *@

<TextBox @ref="txtName" id="txtName" />
<Label @ref="lblMessage" id="lblMessage" />
<Button id="btnSave" Text="Save" OnClick="@HandleSave" />

@code {
    private TextBox txtName = default!;
    private Label lblMessage = default!;
    
    protected override void OnInitialized()
    {
        // Can't access refs here — they're not set until after render
    }
    
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            // Refs are available here
            txtName.Text = "Default Value";
            lblMessage.Text = "Welcome!";
            StateHasChanged();
        }
    }
    
    private async Task HandleSave()
    {
        var name = txtName.Text;
        await SaveToDatabase(name);
    }
}
```

### Component Reference Patterns

| Pattern | Web Forms | Blazor |
|---------|-----------|--------|
| Reference control | `(TextBox)FindControl("txtName")` | `@ref="txtName"` in markup + `private TextBox txtName;` in code |
| Set property | `ctrl.Text = "value"` | `ctrl.Text = "value"` (after render) |
| Get property | `var val = ctrl.Text` | `var val = ctrl.Text` |
| Call method | `ctrl.MethodName()` | `ctrl.MethodName()` |
| Timing | Available in `Page_Load` | Available in `OnAfterRender` (NOT `OnInitialized`) |

### Critical: @ref Timing

> **Important:** Component references (`@ref`) are **null** in `OnInitialized` and `OnInitializedAsync`. They are only available in `OnAfterRender` and `OnAfterRenderAsync` (after the component has rendered).

```csharp
// ❌ WRONG — txtName is null here
protected override void OnInitialized()
{
    txtName.Text = "Default"; // NullReferenceException!
}

// ✅ RIGHT — txtName is available here
protected override void OnAfterRender(bool firstRender)
{
    if (firstRender)
    {
        txtName.Text = "Default";
        StateHasChanged(); // Required if you modify state after render
    }
}
```

---

## 3. Parent/Child Communication Patterns

### Pattern 1: Parent Sets Child Properties

```html
<!-- Web Forms — Parent page (.aspx) -->
<%@ Register TagPrefix="uc" TagName="ProductCard" Src="~/Controls/ProductCard.ascx" %>

<uc:ProductCard ID="card1" runat="server"
    ProductId="101"
    ProductName="Widget"
    Price="29.99"
    ImageUrl="~/images/widget.jpg" />
```

```csharp
// Web Forms — Parent code-behind (dynamic property setting)
protected void Page_Load(object sender, EventArgs e)
{
    card1.ProductId = 101;
    card1.ProductName = "Widget";
    card1.Price = 29.99m;
}
```

```razor
@* Blazor — Parent page (.razor) *@

<ProductCard ProductId="101"
             ProductName="Widget"
             Price="29.99m"
             ImageUrl="/images/widget.jpg" />
```

```razor
@* Blazor — Parent with dynamic values *@

@code {
    private Product product = new() { Id = 101, Name = "Widget", Price = 29.99m };
}

<ProductCard ProductId="@product.Id"
             ProductName="@product.Name"
             Price="@product.Price"
             ImageUrl="/images/widget.jpg" />
```

### Pattern 2: Child Raises Event → Parent Handles

```html
<!-- Web Forms — Parent page -->
<uc:ProductCard ID="card1" runat="server"
    ProductId="101"
    ProductName="Widget"
    OnAddToCartClicked="card1_AddToCartClicked" />
```

```csharp
// Web Forms — Parent code-behind
protected void card1_AddToCartClicked(object sender, int productId)
{
    AddToCart(productId);
    lblMessage.Text = "Added to cart!";
}
```

```razor
@* Blazor — Parent page *@

<ProductCard ProductId="101"
             ProductName="Widget"
             OnAddToCart="@HandleAddToCart" />

<Label @ref="lblMessage" id="lblMessage" />

@code {
    private Label lblMessage = default!;
    
    private async Task HandleAddToCart(int productId)
    {
        await AddToCart(productId);
        lblMessage.Text = "Added to cart!";
    }
}
```

### Pattern 3: Parent Calls Child Method

```csharp
// Web Forms — Parent calls public method on child
protected void btnReset_Click(object sender, EventArgs e)
{
    card1.Reset(); // Calls public method on user control
}
```

```razor
@* Blazor — Parent page *@

<ProductCard @ref="card1" ProductId="101" ProductName="Widget" />
<Button Text="Reset" OnClick="@HandleReset" />

@code {
    private ProductCard card1 = default!;
    
    private void HandleReset()
    {
        card1.Reset(); // Calls public method on component
    }
}
```

```razor
@* ProductCard.razor — child component *@

@code {
    // Public method that parent can call
    public void Reset()
    {
        ProductName = string.Empty;
        Price = 0;
        StateHasChanged();
    }
}
```

---

## 4. Two-Way Binding with Child Components

### Web Forms Pattern (Property + Event)

```csharp
// Web Forms — User Control
public string SelectedValue { get; set; }
public event EventHandler SelectedValueChanged;

protected void dropdown_SelectedIndexChanged(object sender, EventArgs e)
{
    SelectedValue = dropdown.SelectedValue;
    SelectedValueChanged?.Invoke(this, EventArgs.Empty);
}
```

```html
<!-- Web Forms — Parent -->
<uc:CategorySelector ID="selector" runat="server"
    SelectedValue="<%# BindCategory() %>"
    OnSelectedValueChanged="selector_Changed" />
```

### Blazor Pattern: @bind-Property

```razor
@* CategorySelector.razor — child component *@

<DropDownList Items="@categories"
              SelectedValue="@SelectedValue"
              OnSelectedIndexChanged="@HandleSelectionChanged" />

@code {
    [Parameter] public string? SelectedValue { get; set; }
    [Parameter] public EventCallback<string?> SelectedValueChanged { get; set; }
    
    private List<string> categories = new() { "Electronics", "Clothing", "Books" };
    
    private async Task HandleSelectionChanged(ChangeEventArgs e)
    {
        SelectedValue = e.Value?.ToString();
        await SelectedValueChanged.InvokeAsync(SelectedValue);
    }
}
```

```razor
@* Parent page — two-way binding with @bind *@

<CategorySelector @bind-SelectedValue="selectedCategory" />

<p>Selected: @selectedCategory</p>

@code {
    private string? selectedCategory = "Electronics";
}
```

### Two-Way Binding Naming Convention

For `@bind-PropertyName` to work, the component must have:
1. `[Parameter] public T PropertyName { get; set; }` — The property
2. `[Parameter] public EventCallback<T> PropertyNameChanged { get; set; }` — The event

The event parameter name **must** be the property name + `Changed` suffix. This is a Blazor naming convention.

---

## 5. Register Directive → _Imports.razor

### Web Forms Pattern

```html
<!-- Every page that uses the control -->
<%@ Register TagPrefix="uc" TagName="ProductCard" Src="~/Controls/ProductCard.ascx" %>
<%@ Register TagPrefix="uc" TagName="CategorySelector" Src="~/Controls/CategorySelector.ascx" %>

<uc:ProductCard ... />
<uc:CategorySelector ... />
```

### Blazor Pattern: _Imports.razor

```razor
@* _Imports.razor — global component registration *@

@using BlazorWebFormsComponents
@using BlazorWebFormsComponents.Enums
@using MyApp.Components
@using MyApp.Components.Products
@using MyApp.Components.Admin
@inherits BlazorWebFormsComponents.WebFormsPageBase
```

```razor
@* Parent page — no @using needed, just use the component *@

<ProductCard ... />
<CategorySelector ... />
```

### Namespace Organization

Organize components by feature:

```
MyApp/
├─ Components/
│  ├─ Shared/         → common components (Header, Footer, etc.)
│  ├─ Products/       → product-related components (ProductCard, ProductList)
│  ├─ Admin/          → admin components
│  └─ Account/        → account components
└─ Pages/
   └─ Products.razor
```

Add to `_Imports.razor`:

```razor
@using MyApp.Components.Shared
@using MyApp.Components.Products
@using MyApp.Components.Admin
@using MyApp.Components.Account
```

---

## 6. Complex Data Binding in User Controls

### Web Forms Pattern (DataSource + Template)

```html
<%@ Control Language="C#" CodeBehind="ProductList.ascx.cs" ... %>

<asp:Repeater ID="rptProducts" runat="server">
    <ItemTemplate>
        <div class="product">
            <h3><%# Eval("Name") %></h3>
            <p>$<%# Eval("Price") %></p>
        </div>
    </ItemTemplate>
</asp:Repeater>
```

```csharp
// ProductList.ascx.cs
public List<Product> Products { get; set; }

protected void Page_Load(object sender, EventArgs e)
{
    if (!IsPostBack)
    {
        rptProducts.DataSource = Products;
        rptProducts.DataBind();
    }
}
```

### Blazor Pattern (Items Parameter)

```razor
@* ProductList.razor *@

<div class="product-list">
    @foreach (var product in Products)
    {
        <div class="product">
            <h3>@product.Name</h3>
            <p>$@product.Price</p>
        </div>
    }
</div>

@code {
    [Parameter] public List<Product> Products { get; set; } = new();
}
```

### Using BWFC Repeater in Component

```razor
@* ProductList.razor — using BWFC Repeater *@

<Repeater Items="@Products" ItemType="Product">
    <ItemTemplate Context="product">
        <div class="product">
            <h3>@product.Name</h3>
            <p>$@product.Price</p>
        </div>
    </ItemTemplate>
</Repeater>

@code {
    [Parameter] public List<Product> Products { get; set; } = new();
}
```

---

## Common Migration Patterns

### Pattern 1: Simple Display Component

```html
<!-- Web Forms — UserInfo.ascx -->
<%@ Control ... %>
<div class="user-info">
    <span class="username"><%= UserName %></span>
    <span class="role"><%= UserRole %></span>
</div>
```

```csharp
// UserInfo.ascx.cs
public string UserName { get; set; }
public string UserRole { get; set; }
```

```razor
@* Blazor — UserInfo.razor *@

<div class="user-info">
    <span class="username">@UserName</span>
    <span class="role">@UserRole</span>
</div>

@code {
    [Parameter] public string UserName { get; set; } = string.Empty;
    [Parameter] public string UserRole { get; set; } = string.Empty;
}
```

### Pattern 2: Form Input Component with Validation

```html
<!-- Web Forms — EmailInput.ascx -->
<asp:TextBox ID="txtEmail" runat="server" />
<asp:RequiredFieldValidator ControlToValidate="txtEmail" ErrorMessage="Required" runat="server" />
<asp:RegularExpressionValidator ControlToValidate="txtEmail" 
    ValidationExpression="^\w+@\w+\.\w+$" 
    ErrorMessage="Invalid email" runat="server" />
```

```csharp
// EmailInput.ascx.cs
public string Email
{
    get => txtEmail.Text;
    set => txtEmail.Text = value;
}
```

```razor
@* Blazor — EmailInput.razor *@

<TextBox @ref="txtEmail" id="txtEmail" @bind-Text="@Email" />
<RequiredFieldValidator ControlToValidate="txtEmail" ErrorMessage="Required" />
<RegularExpressionValidator ControlToValidate="txtEmail"
    ValidationExpression="^\w+@\w+\.\w+$"
    ErrorMessage="Invalid email" />

@code {
    private TextBox txtEmail = default!;
    
    [Parameter] public string Email { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> EmailChanged { get; set; }
}
```

### Pattern 3: Component with Service Injection

```csharp
// Web Forms — ProductSearch.ascx.cs (service access via static/property)
public partial class ProductSearch : UserControl
{
    protected void btnSearch_Click(object sender, EventArgs e)
    {
        var service = ServiceLocator.GetService<IProductService>();
        var results = service.Search(txtQuery.Text);
        rptResults.DataSource = results;
        rptResults.DataBind();
    }
}
```

```razor
@* Blazor — ProductSearch.razor (dependency injection) *@

@inject IProductService ProductService

<TextBox @ref="txtQuery" id="txtQuery" />
<Button Text="Search" OnClick="@HandleSearch" />

<Repeater Items="@results" ItemType="Product">
    <ItemTemplate Context="product">
        <div>@product.Name</div>
    </ItemTemplate>
</Repeater>

@code {
    private TextBox txtQuery = default!;
    private List<Product> results = new();
    
    private async Task HandleSearch()
    {
        results = await ProductService.SearchAsync(txtQuery.Text);
    }
}
```

---

## Known Failure Modes

### 1. @ref Null in OnInitialized

**Problem:** Attempting to access component references (`@ref`) in `OnInitialized` causes `NullReferenceException`.

**Solution:** Move ref access to `OnAfterRender(bool firstRender)` and guard with `if (firstRender)`.

### 2. Forgetting [Parameter] Attribute

**Problem:** Public properties without `[Parameter]` can't be set by parent.

```csharp
// ❌ WRONG — Property can't be set by parent
public string Title { get; set; }

// ✅ RIGHT — Property can be set by parent
[Parameter] public string Title { get; set; }
```

### 3. EventCallback Not Awaited

**Problem:** Not awaiting `EventCallback.InvokeAsync()` can cause state update issues.

```csharp
// ❌ WRONG — Not awaited
private void HandleClick()
{
    OnClick.InvokeAsync(); // Fire and forget — may cause issues
}

// ✅ RIGHT — Awaited
private async Task HandleClick()
{
    await OnClick.InvokeAsync();
}
```

### 4. Two-Way Binding Naming Mismatch

**Problem:** Two-way binding requires exact naming: `PropertyName` + `PropertyNameChanged`.

```csharp
// ❌ WRONG — Won't work with @bind-SelectedValue
[Parameter] public string SelectedValue { get; set; }
[Parameter] public EventCallback<string> ValueChanged { get; set; } // Wrong name!

// ✅ RIGHT — Works with @bind-SelectedValue
[Parameter] public string SelectedValue { get; set; }
[Parameter] public EventCallback<string> SelectedValueChanged { get; set; } // Correct name
```

---

## What Developers Must Do Manually

1. **Review public properties** — Decide which should be `[Parameter]` (settable by parent) vs private (internal state).
2. **Convert FindControl to @ref** — Each `FindControl()` call must be replaced with component reference pattern.
3. **Test event handling** — EventCallback semantics differ from Web Forms events (async, no sender parameter).
4. **Refactor complex controls** — Large user controls may benefit from splitting into multiple smaller components.

---

## L1 Script Support (Future Enhancement)

The L1 PowerShell script could detect user control patterns and add guidance:

```csharp
// TODO: BWFC — Public property detected in user control.
//       Add [Parameter] attribute to make this settable by parent component.
//       See bwfc-usercontrol-migration skill for examples.
public string ProductName { get; set; }

// TODO: BWFC — FindControl() detected.
//       Replace with @ref pattern: add '@ref="txtName"' to markup,
//       declare 'private TextBox txtName;' in code, access in OnAfterRender.
//       See bwfc-usercontrol-migration skill for timing rules.
var ctrl = (TextBox)FindControl("txtName");

// TODO: BWFC — Custom event detected in user control.
//       Convert to EventCallback<T> parameter for Blazor parent communication.
//       See bwfc-usercontrol-migration skill for event patterns.
public event EventHandler<int> ItemSelected;
```

---

## References

- [Blazor components](https://learn.microsoft.com/aspnet/core/blazor/components/)
- [Component parameters](https://learn.microsoft.com/aspnet/core/blazor/components/#component-parameters)
- [EventCallback](https://learn.microsoft.com/aspnet/core/blazor/components/event-handling#eventcallback)
- [Component references](https://learn.microsoft.com/aspnet/core/blazor/components/#capture-references-to-components)
- [Two-way binding](https://learn.microsoft.com/aspnet/core/blazor/components/data-binding#bind-across-more-than-two-components)
