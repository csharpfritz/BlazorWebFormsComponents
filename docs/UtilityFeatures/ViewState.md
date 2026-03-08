# ViewState

Every BWFC component inherits a `ViewState` property — a `Dictionary<string, object>` that lets migrated code using `ViewState["key"] = value` patterns compile and run without changes.

Original Microsoft implementation: [StateBag (System.Web.UI)](https://docs.microsoft.com/en-us/dotnet/api/system.web.ui.statebag?view=netframework-4.8)

## Background

In ASP.NET Web Forms, every `Page` and `Control` carried a `ViewState` object (a `StateBag`) that stored key/value pairs across postbacks. The framework serialized this data into a hidden `<input>` field, sent it to the browser, and deserialized it on the next request — preserving state across the stateless HTTP protocol.

Developers commonly used ViewState as a general-purpose key/value store:

```csharp
// Store a value
ViewState["SortDirection"] = "ASC";
ViewState.Add("CurrentPage", 1);

// Retrieve a value
string direction = (string)ViewState["SortDirection"];
int page = (int)ViewState["CurrentPage"];
```

While powerful, ViewState was often over-used — leading to bloated hidden fields containing megabytes of serialized data that slowed page loads.

## Blazor Implementation

BlazorWebFormsComponents provides a syntax-compatible `ViewState` property on `BaseWebFormsComponent` so that migrated code-behind files compile without modification. The implementation is intentionally simple:

```csharp
[Obsolete("ViewState is supported for compatibility and is discouraged for future use")]
public Dictionary<string, object> ViewState { get; } = new Dictionary<string, object>();
```

Every component that inherits from `BaseWebFormsComponent` — which includes all BWFC components — has this property available automatically.

### EnableViewState Parameter

BWFC also accepts the `EnableViewState` parameter on every component for markup compatibility. This parameter is parsed but **does nothing** — it exists solely so that migrated markup containing `EnableViewState="false"` compiles without errors.

```razor
@* This compiles but EnableViewState has no effect *@
<Label ID="lblTotal" Text="@Total" EnableViewState="false" />
```

## Web Forms Usage

```csharp
// Web Forms code-behind (.aspx.cs)
protected void Page_Load(object sender, EventArgs e)
{
    if (!IsPostBack)
    {
        ViewState["SortDirection"] = "ASC";
    }
}

protected void GridView1_Sorting(object sender, GridViewSortEventArgs e)
{
    string direction = (string)ViewState["SortDirection"];
    ViewState["SortDirection"] = direction == "ASC" ? "DESC" : "ASC";
    BindGrid(e.SortExpression, (string)ViewState["SortDirection"]);
}
```

## Blazor Usage (BWFC)

The same ViewState syntax works in a Blazor component that inherits from a BWFC base:

```razor
@inherits BaseWebFormsComponent

<GridView DataSource="@Items" OnSorting="HandleSorting" />

@code {
    private List<Instructor> Items { get; set; }

    protected override void OnInitialized()
    {
        #pragma warning disable CS0618 // Suppress Obsolete warning during migration
        ViewState["SortDirection"] = "ASC";
        #pragma warning restore CS0618
        LoadData();
    }

    private void HandleSorting(GridViewSortEventArgs e)
    {
        #pragma warning disable CS0618
        string direction = (string)ViewState["SortDirection"];
        ViewState["SortDirection"] = direction == "ASC" ? "DESC" : "ASC";
        #pragma warning restore CS0618
        LoadData(e.SortExpression);
    }
}
```

!!! note "Compiler Warnings"
    The `ViewState` property is marked `[Obsolete]`, so your code will emit compiler warnings. This is intentional — it reminds you to refactor away from ViewState after your initial migration. Use `#pragma warning disable CS0618` to suppress the warnings temporarily.

## Migration Path

Migrating code that uses ViewState is straightforward:

| Step | Action |
|---|---|
| 1 | Copy your code-behind into a `.razor` or `.razor.cs` file |
| 2 | Ensure the component inherits from a BWFC base component |
| 3 | `ViewState["key"] = value` and `ViewState["key"]` syntax works as-is |
| 4 | Suppress `CS0618` warnings if needed during the transition |
| 5 | When ready, refactor to strongly-typed properties (see Moving On) |

!!! warning "In-Memory Only — Not Persisted Across Requests"
    Unlike Web Forms ViewState, the BWFC implementation is an **in-memory dictionary** — it is **not** serialized to a hidden field or persisted across HTTP requests. It works for component-scoped state that lives for the duration of the component's lifetime (which, in Blazor Server, persists across user interactions within the same circuit). It does **not** survive page navigation, circuit disconnection, or app restarts.

## Limitations

| Web Forms ViewState | BWFC ViewState |
|---|---|
| Serialized to hidden `<input>` field | In-memory `Dictionary<string, object>` |
| Survived postbacks (round-trips) | Survives for component lifetime only |
| Available on Page and all Controls | Available on all BWFC components |
| `StateBag` with type tracking | Plain `Dictionary` — no type metadata |
| `EnableViewState` controlled serialization | `EnableViewState` accepted but ignored |
| ViewState encryption supported | No encryption (in-memory only) |
| Could cause page bloat | No serialization overhead |

## Practical Example: Sort Direction Toggle

This is a common Web Forms pattern — storing sort direction in ViewState so it persists across postback-triggered sorts.

**Before (Web Forms):**

```csharp
// Instructors.aspx.cs
public partial class Instructors : Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            ViewState["SortDir"] = "ASC";
            ViewState["SortCol"] = "LastName";
            BindGrid();
        }
    }

    protected void gvInstructors_Sorting(object sender, GridViewSortEventArgs e)
    {
        string currentDir = (string)ViewState["SortDir"];
        ViewState["SortDir"] = currentDir == "ASC" ? "DESC" : "ASC";
        ViewState["SortCol"] = e.SortExpression;
        BindGrid();
    }
}
```

**After (Blazor with BWFC):**

```razor
@* Instructors.razor *@
@inherits BaseWebFormsComponent

@code {
    protected override void OnInitialized()
    {
        #pragma warning disable CS0618
        ViewState["SortDir"] = "ASC";
        ViewState["SortCol"] = "LastName";
        #pragma warning restore CS0618
        BindGrid();
    }

    private void HandleSorting(GridViewSortEventArgs e)
    {
        #pragma warning disable CS0618
        string currentDir = (string)ViewState["SortDir"];
        ViewState["SortDir"] = currentDir == "ASC" ? "DESC" : "ASC";
        ViewState["SortCol"] = e.SortExpression;
        #pragma warning restore CS0618
        BindGrid();
    }
}
```

The code-behind logic migrates with minimal changes — `Page_Load` becomes `OnInitialized`, and the `ViewState` dictionary syntax is identical.

## Cross-Request State Alternatives

For state that must survive beyond the component's lifetime, use Blazor's built-in state management:

| Scenario | Blazor Approach |
|---|---|
| State shared between components | [Cascading values](https://learn.microsoft.com/aspnet/core/blazor/components/cascading-values-and-parameters) |
| State scoped to the user session | Scoped services registered in DI |
| State that survives page navigation | `ProtectedSessionStorage` or `ProtectedLocalStorage` |
| Application-wide state | Singleton services |
| State that survives app restarts | Database or external storage |

!!! tip "Best Practice"
    Use BWFC's `ViewState` as a **migration stepping stone**. It gets your code compiling quickly. Then refactor to strongly-typed fields and Blazor state management at your own pace.

## Moving On

ViewState is not a feature you should continue to use after migrating. While BWFC eliminates the serialization/deserialization performance penalty of Web Forms ViewState, we recommend moving to class-scoped fields and properties that are validated and strongly-typed. This also eliminates the boxing/unboxing overhead that comes with `Dictionary<string, object>`.

**Refactored version of the sort example above:**

```csharp
// Strongly-typed properties replace ViewState
private string SortDirection { get; set; } = "ASC";
private string SortColumn { get; set; } = "LastName";

private void HandleSorting(GridViewSortEventArgs e)
{
    SortDirection = SortDirection == "ASC" ? "DESC" : "ASC";
    SortColumn = e.SortExpression;
    BindGrid();
}
```

Benefits of refactoring away from ViewState:

- **Type safety** — No casting from `object`; compiler catches type errors
- **No boxing** — Value types like `int` and `bool` avoid boxing/unboxing
- **IntelliSense** — Properties appear in editor autocomplete
- **Clearer intent** — `SortDirection` is more readable than `ViewState["SortDir"]`

## See Also

- [WebFormsPage](WebFormsPage.md) — Page-level wrapper providing naming and theming
- [NamingContainer](NamingContainer.md) — Naming scope for component IDs
- [Databinder](Databinder.md) — Another Web Forms compatibility utility
- [Migration Guide](../Migration/readme.md) — Getting started with migration
- [Custom Controls Migration](../Migration/Custom-Controls.md) — ViewState considerations for custom controls

