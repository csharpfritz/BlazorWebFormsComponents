# ListView

The ListView component is meant to emulate the asp:ListView control in markup and is defined in the [System.Web.UI.WebControls.ListView class](https://docs.microsoft.com/en-us/dotnet/api/system.web.ui.webcontrols.listview?view=netframework-4.8)

[Usage Notes](#usage-notes) | [Web Forms Syntax](#web-forms-declarative-syntax) | [Blazor Syntax](#blazor-syntax) | [CRUD Events](#crud-events)

## Features supported in Blazor
 - Alternating Item Templates
 - Alternating Item Styles
 - Empty Data Template
 - Empty Item Template
 - Grouping
 - Item Templates
 - Item Styles
 - Model Binding
   - OnSelect Method
 - LayoutTemplate
 - DataBinder within the ItemTemplate and AlternatingItemTemplate
 - **ListViewItem and ListViewDataItem** for event handling
 - **CRUD Operations** (Edit, Delete, Insert, Update, Cancel)
   - EditItemTemplate
   - InsertItemTemplate
   - EditIndex
   - InsertItemPosition
 - **CRUD Events** — 16 lifecycle events for data operations:
   - **Insert:** ItemInserting / ItemInserted
   - **Update:** ItemUpdating / ItemUpdated
   - **Delete:** ItemDeleting / ItemDeleted
   - **Edit / Cancel:** ItemEditing / ItemCanceling
   - **Sort:** Sorting / Sorted
   - **Paging:** PagePropertiesChanging / PagePropertiesChanged
   - **Selection:** SelectedIndexChanging / SelectedIndexChanged
   - **Lifecycle:** OnLayoutCreated / ItemCreated / OnItemDataBound (DataBound override)

##### [Back to top](#listview)

## ListViewItem and ListViewDataItem

The `OnItemDataBound` event provides a `ListViewItemEventArgs` object that contains a `ListViewItem` instance. For data items, this will be a `ListViewDataItem` object with the following properties:

- **ItemType** - The type of item (DataItem, InsertItem, or EmptyItem)
- **DisplayIndex** - The position of the item as displayed in the ListView
- **DataItemIndex** - The index of the data item in the underlying data source
- **DataItem** - The underlying data object bound to this item

**Example:**

```csharp
void ItemDataBound(ListViewItemEventArgs e)
{
    if (e.Item.ItemType == ListViewItemType.DataItem)
    {
        var dataItem = (ListViewDataItem)e.Item;
        var widget = (Widget)dataItem.DataItem;
        // Access widget.Name, widget.Price, etc.
    }
}
```

##### [Back to top](#listview)

## Usage Notes

- **LayoutTemplate** - Requires a `Context` attribute that defines the placeholder for the items
- **Context attribute** - For Web Forms compatibility, use `Context="Item"` on the ListView to access the current item as `@Item` in ItemTemplate and AlternatingItemTemplate instead of Blazor's default `@context`
- **ItemType attribute** - Required to specify the type of items in the collection

##### [Back to top](#listview)

## Web Forms Declarative Syntax

```html
<asp:ListView  
    ConvertEmptyStringToNull="True|False"  
    DataKeyNames="string"  
    DataMember="string"  
    DataSource="string"  
    DataSourceID="string"  
    EditIndex="integer"  
    Enabled="True|False"  
    EnableTheming="True|False"  
    EnableViewState="True|False"  
    GroupPlaceholderID="string"  
    GroupItemCount="integer"  
    ID="string"  
    InsertItemPosition="None|FirstItem|LastItem"  
    ItemPlaceholderID="string"  
    OnDataBinding="DataBinding event handler"  
    OnDataBound="DataBound event handler"  
    OnDisposed="Disposed event handler"  
    OnInit="Init event handler"  
    OnItemCanceling="ItemCanceling event handler"  
    OnItemCommand="ItemCommand event handler"  
    OnItemCreated="ItemCreated event handler"  
    OnItemDataBound="ItemDataBound event handler"  
    OnItemDeleted="ItemDeleted event handler"  
    OnItemDeleting="ItemDeleting event handler"  
    OnItemEditing="ItemEditing event handler"  
    OnItemInserted="ItemInserted event handler"  
    OnItemInserting="ItemInserting event handler"  
    OnItemUpdated="ItemUpdated event handler"  
    OnItemUpdating="ItemUpdating event handler"  
    OnLayoutCreated="LayoutCreated event handler"  
    OnLoad="Load event handler"  
    OnPagePropertiesChanged="PagePropertiesChanged event handler"  
    OnPagePropertiesChanging="PagePropertiesChanging event handler"  
    OnPreRender="PreRender event handler"  
    OnSelectedIndexChanged="SelectedIndexChanged event handler"  
    OnSelectedIndexChanging="SelectedIndexChanging event handler"  
    OnSorted="Sorted event handler"  
    OnSorting="Sorting event handler"  
    OnUnload="Unload event handler"  
    runat="server"  
    SelectedIndex="integer"  
    SkinID="string"  
    Style="string"  
    Visible="True|False">  
    <AlternatingItemTemplate>  
        <!-- child controls -->  
    </AlternatingItemTemplate>  
    <EditItemTemplate>  
        <!-- child controls -->  
    </EditItemTemplate>  
    <EmptyDataTemplate>  
        <!-- child controls -->  
    </EmptyDataTemplate>  
    <EmptyItemTemplate>  
        <!-- child controls -->  
    </EmptyItemTemplate>  
    <GroupSeparatorTemplate>  
        <!-- child controls -->  
    </GroupSeparatorTemplate>  
    <GroupTemplate>  
        <!-- child controls -->  
    </GroupTemplate>  
    <InsertItemTemplate>  
        <!-- child controls -->  
    </InsertItemTemplate>  
    <ItemSeparatorTemplate>  
        <!-- child controls -->  
    </ItemSeparatorTemplate>  
    <ItemTemplate>  
        <!-- child controls -->  
    </ItemTemplate>  
    <LayoutTemplate>  
            <!-- child controls -->  
    </LayoutTemplate>  
    <SelectedItemTemplate>  
        <!-- child controls -->  
    </SelectedItemTemplate>  
</asp:ListView>
```

##### [Back to top](#listview)

## Blazor Syntax

```razor
<ListView @ref="listView" Context="Item"
          ItemType="Widget"
          EditIndex="@editIndex"
          InsertItemPosition="InsertItemPosition.LastItem"
          SelectedIndex="@selectedIndex"
          ItemEditing="OnItemEditing"
          ItemUpdating="OnItemUpdating"
          ItemUpdated="OnItemUpdated"
          ItemCanceling="OnItemCanceling"
          ItemDeleting="OnItemDeleting"
          ItemDeleted="OnItemDeleted"
          ItemInserting="OnItemInserting"
          ItemInserted="OnItemInserted"
          Sorting="OnSorting"
          Sorted="OnSorted"
          PagePropertiesChanging="OnPagePropertiesChanging"
          PagePropertiesChanged="OnPagePropertiesChanged"
          OnLayoutCreated="OnLayoutCreated"
          SelectedIndexChanging="OnSelectedIndexChanging"
          SelectedIndexChanged="OnSelectedIndexChanged"
          ItemCreated="OnItemCreated"
          OnItemDataBound="OnItemDataBound">
    <ItemTemplate>
        <tr>
            <td>@Item.Name</td>
            <td>
                <button @onclick="() => listView.HandleCommand("edit", null, index)">Edit</button>
                <button @onclick="() => listView.HandleCommand("delete", null, index)">Delete</button>
            </td>
        </tr>
    </ItemTemplate>
    <EditItemTemplate>
        <tr>
            <td><input @bind="editName" /></td>
            <td>
                <button @onclick="() => listView.HandleCommand("update", null, editIndex)">Update</button>
                <button @onclick="() => listView.HandleCommand("cancel", null, editIndex)">Cancel</button>
            </td>
        </tr>
    </EditItemTemplate>
    <InsertItemTemplate>
        <tr>
            <td><input @bind="newName" /></td>
            <td>
                <button @onclick="() => listView.HandleCommand("insert", null, 0)">Insert</button>
            </td>
        </tr>
    </InsertItemTemplate>
    <EmptyDataTemplate>No items available.</EmptyDataTemplate>
</ListView>
```

##### [Back to top](#listview)

## CRUD Operations

The ListView supports full Create, Read, Update, and Delete operations through a set of command events. Use `HandleCommand` on the ListView reference to trigger operations.

### Events

| Event | EventArgs | Description |
|-------|-----------|-------------|
| `ItemEditing` | `ListViewEditEventArgs` | Fires when an Edit command is requested. Set `NewEditIndex` to control which item enters edit mode. |
| `ItemUpdating` | `ListViewUpdateEventArgs` | Fires when an Update command is requested. `ItemIndex` identifies the item being updated. |
| `ItemCanceling` | `ListViewCancelEventArgs` | Fires when a Cancel command is requested. |
| `ItemDeleting` | `ListViewDeleteEventArgs` | Fires before an item is deleted. Set `Cancel = true` to prevent deletion. |
| `ItemDeleted` | `ListViewDeletedEventArgs` | Fires after a delete operation completes. |
| `ItemInserting` | `ListViewInsertEventArgs` | Fires before an item is inserted. Set `Cancel = true` to prevent insertion. |
| `ItemInserted` | `ListViewInsertedEventArgs` | Fires after an insert operation completes. |

### EditItemTemplate

When `EditIndex` is set to a valid item index, the `EditItemTemplate` is rendered for that item instead of the `ItemTemplate`. Set `EditIndex = -1` to exit edit mode.

### InsertItemTemplate

The `InsertItemTemplate` renders an insert row when `InsertItemPosition` is set to `FirstItem` or `LastItem`. Set to `None` (default) to hide the insert row.

### Migration Example

**Before (Web Forms):**
```html
<asp:ListView ID="lvItems" runat="server"
    OnItemEditing="lvItems_ItemEditing"
    OnItemUpdating="lvItems_ItemUpdating"
    OnItemDeleting="lvItems_ItemDeleting"
    OnItemInserting="lvItems_ItemInserting"
    InsertItemPosition="LastItem">
    <ItemTemplate>
        <tr>
            <td><%# Eval("Name") %></td>
            <td>
                <asp:Button runat="server" CommandName="Edit" Text="Edit" />
                <asp:Button runat="server" CommandName="Delete" Text="Delete" />
            </td>
        </tr>
    </ItemTemplate>
    <EditItemTemplate>
        <tr>
            <td><asp:TextBox ID="txtName" runat="server" Text='<%# Bind("Name") %>' /></td>
            <td>
                <asp:Button runat="server" CommandName="Update" Text="Update" />
                <asp:Button runat="server" CommandName="Cancel" Text="Cancel" />
            </td>
        </tr>
    </EditItemTemplate>
</asp:ListView>
```

**After (Blazor):**
```razor
<ListView @ref="listView" Context="Item" ItemType="Widget"
    EditIndex="@editIndex"
    InsertItemPosition="InsertItemPosition.LastItem"
    ItemEditing="OnItemEditing"
    ItemUpdating="OnItemUpdating"
    ItemDeleting="OnItemDeleting"
    ItemInserting="OnItemInserting">
    <ItemTemplate>
        <tr>
            <td>@Item.Name</td>
            <td>
                <button @onclick="() => listView.HandleCommand("edit", null, idx)">Edit</button>
                <button @onclick="() => listView.HandleCommand("delete", null, idx)">Delete</button>
            </td>
        </tr>
    </ItemTemplate>
    <EditItemTemplate>
        <tr>
            <td><input @bind="editName" /></td>
            <td>
                <button @onclick="() => listView.HandleCommand("update", null, editIndex)">Update</button>
                <button @onclick="() => listView.HandleCommand("cancel", null, editIndex)">Cancel</button>
            </td>
        </tr>
    </EditItemTemplate>
</ListView>
```

##### [Back to top](#listview)

## CRUD Events

The ListView fires 16 lifecycle events that mirror the original Web Forms control's event model. These events use `EventCallback<T>` parameters and follow the familiar *-ing / *-ed pattern: the *-ing event fires before the operation (and can cancel it), while the *-ed event fires after the operation completes.

!!! tip "Command Routing"
    All CRUD events are triggered through the `HandleCommand` method on the ListView reference. Call `listView.HandleCommand("edit", null, index)` to trigger the edit flow, `"delete"` for delete, `"update"` for update, `"insert"` for insert, `"cancel"` for cancel, `"sort"` for sort, and `"select"` for selection.

### Event Reference

#### Insert Events

| Event | EventArgs | Description |
|-------|-----------|-------------|
| `ItemInserting` | `ListViewInsertEventArgs` | Fires before an item is inserted. Set `Cancel = true` to prevent insertion. |
| `ItemInserted` | `ListViewInsertedEventArgs` | Fires after the insert operation completes. |

**`ListViewInsertEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Cancel` | `bool` | Set to `true` to cancel the insert operation. |
| `Item` | `object` | Gets or sets the item to be inserted. |

**`ListViewInsertedEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `AffectedRows` | `int` | The number of rows affected by the insert. |
| `Exception` | `Exception` | The exception raised during insert, if any. |
| `ExceptionHandled` | `bool` | Set to `true` to indicate the exception has been handled. |
| `KeepInsertedValues` | `bool` | Set to `true` to retain the values in the insert form after insertion. |

**Web Forms → Blazor comparison:**

=== "Web Forms"

    ```html
    <asp:ListView ID="lv" runat="server"
        OnItemInserting="lv_ItemInserting"
        OnItemInserted="lv_ItemInserted">
    ```
    ```csharp
    protected void lv_ItemInserting(object sender, ListViewInsertEventArgs e)
    {
        // Validate before insert
        if (string.IsNullOrEmpty(txtName.Text))
        {
            e.Cancel = true;
        }
    }

    protected void lv_ItemInserted(object sender, ListViewInsertedEventArgs e)
    {
        if (e.Exception != null)
        {
            e.ExceptionHandled = true;
            lblError.Text = "Insert failed.";
        }
    }
    ```

=== "Blazor"

    ```razor
    <ListView @ref="listView" Context="Item" ItemType="Product"
        InsertItemPosition="InsertItemPosition.LastItem"
        ItemInserting="OnItemInserting"
        ItemInserted="OnItemInserted">
    ```
    ```csharp
    @code {
        void OnItemInserting(ListViewInsertEventArgs e)
        {
            if (string.IsNullOrEmpty(newName))
            {
                e.Cancel = true;
            }
        }

        void OnItemInserted(ListViewInsertedEventArgs e)
        {
            if (e.Exception != null)
            {
                e.ExceptionHandled = true;
                errorMessage = "Insert failed.";
            }
        }
    }
    ```

##### [Back to top](#listview)

#### Update Events

| Event | EventArgs | Description |
|-------|-----------|-------------|
| `ItemUpdating` | `ListViewUpdateEventArgs` | Fires before an item is updated. Set `Cancel = true` to prevent the update. |
| `ItemUpdated` | `ListViewUpdatedEventArgs` | Fires after the update operation completes. |

**`ListViewUpdateEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `ItemIndex` | `int` | The index of the item being updated. |
| `Cancel` | `bool` | Set to `true` to cancel the update operation. |

**`ListViewUpdatedEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `AffectedRows` | `int` | The number of rows affected by the update. |
| `Exception` | `Exception` | The exception raised during update, if any. |
| `ExceptionHandled` | `bool` | Set to `true` to indicate the exception has been handled. |
| `KeepInEditMode` | `bool` | Set to `true` to keep the row in edit mode after the update. |

**Web Forms → Blazor comparison:**

=== "Web Forms"

    ```html
    <asp:ListView ID="lv" runat="server"
        OnItemUpdating="lv_ItemUpdating"
        OnItemUpdated="lv_ItemUpdated">
    ```
    ```csharp
    protected void lv_ItemUpdating(object sender, ListViewUpdateEventArgs e)
    {
        // e.ItemIndex identifies which item is being updated
    }

    protected void lv_ItemUpdated(object sender, ListViewUpdatedEventArgs e)
    {
        if (e.Exception != null)
        {
            e.ExceptionHandled = true;
        }
    }
    ```

=== "Blazor"

    ```razor
    <ListView @ref="listView" Context="Item" ItemType="Product"
        EditIndex="@editIndex"
        ItemUpdating="OnItemUpdating"
        ItemUpdated="OnItemUpdated">
    ```
    ```csharp
    @code {
        void OnItemUpdating(ListViewUpdateEventArgs e)
        {
            var product = products[e.ItemIndex];
            product.Name = editName;
            product.Price = editPrice;
        }

        void OnItemUpdated(ListViewUpdatedEventArgs e)
        {
            if (e.Exception != null)
            {
                e.ExceptionHandled = true;
                errorMessage = "Update failed.";
            }
            // Row exits edit mode automatically unless KeepInEditMode = true
        }
    }
    ```

##### [Back to top](#listview)

#### Delete Events

| Event | EventArgs | Description |
|-------|-----------|-------------|
| `ItemDeleting` | `ListViewDeleteEventArgs` | Fires before an item is deleted. Set `Cancel = true` to prevent deletion. |
| `ItemDeleted` | `ListViewDeletedEventArgs` | Fires after the delete operation completes. |

**`ListViewDeleteEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `ItemIndex` | `int` | The index of the item being deleted. |
| `Cancel` | `bool` | Set to `true` to cancel the delete operation. |

**`ListViewDeletedEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `AffectedRows` | `int` | The number of rows affected by the delete. |
| `Exception` | `Exception` | The exception raised during delete, if any. |
| `ExceptionHandled` | `bool` | Set to `true` to indicate the exception has been handled. |

**Web Forms → Blazor comparison:**

=== "Web Forms"

    ```html
    <asp:ListView ID="lv" runat="server"
        OnItemDeleting="lv_ItemDeleting"
        OnItemDeleted="lv_ItemDeleted">
    ```
    ```csharp
    protected void lv_ItemDeleting(object sender, ListViewDeleteEventArgs e)
    {
        // Confirm deletion; set e.Cancel = true to abort
    }

    protected void lv_ItemDeleted(object sender, ListViewDeletedEventArgs e)
    {
        if (e.AffectedRows == 1)
        {
            // Refresh data
        }
    }
    ```

=== "Blazor"

    ```razor
    <ListView @ref="listView" Context="Item" ItemType="Product"
        ItemDeleting="OnItemDeleting"
        ItemDeleted="OnItemDeleted">
    ```
    ```csharp
    @code {
        void OnItemDeleting(ListViewDeleteEventArgs e)
        {
            products.RemoveAt(e.ItemIndex);
        }

        void OnItemDeleted(ListViewDeletedEventArgs e)
        {
            statusMessage = $"Deleted. {e.AffectedRows} row(s) affected.";
        }
    }
    ```

##### [Back to top](#listview)

#### Edit and Cancel Events

| Event | EventArgs | Description |
|-------|-----------|-------------|
| `ItemEditing` | `ListViewEditEventArgs` | Fires when an Edit command is requested. Set `NewEditIndex` to control which item enters edit mode. |
| `ItemCanceling` | `ListViewCancelEventArgs` | Fires when a Cancel command is requested. `CancelMode` indicates whether canceling an edit or an insert. |

**`ListViewEditEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `NewEditIndex` | `int` | The index of the item entering edit mode. |
| `Cancel` | `bool` | Set to `true` to cancel the edit operation. |

**`ListViewCancelEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `ItemIndex` | `int` | The index of the item containing the Cancel button. |
| `CancelMode` | `ListViewCancelMode` | Indicates whether canceling an edit (`CancelingEdit`) or an insert (`CancelingInsert`). |
| `Cancel` | `bool` | Set to `true` to prevent the cancel operation. |

**Web Forms → Blazor comparison:**

=== "Web Forms"

    ```html
    <asp:ListView ID="lv" runat="server"
        OnItemEditing="lv_ItemEditing"
        OnItemCanceling="lv_ItemCanceling">
    ```
    ```csharp
    protected void lv_ItemEditing(object sender, ListViewEditEventArgs e)
    {
        lv.EditIndex = e.NewEditIndex;
        BindData();
    }

    protected void lv_ItemCanceling(object sender, ListViewCancelEventArgs e)
    {
        lv.EditIndex = -1;
        BindData();
    }
    ```

=== "Blazor"

    ```razor
    <ListView @ref="listView" Context="Item" ItemType="Product"
        EditIndex="@editIndex"
        ItemEditing="OnItemEditing"
        ItemCanceling="OnItemCanceling">
    ```
    ```csharp
    @code {
        private int editIndex = -1;

        void OnItemEditing(ListViewEditEventArgs e)
        {
            editIndex = e.NewEditIndex;
        }

        void OnItemCanceling(ListViewCancelEventArgs e)
        {
            editIndex = -1;
            // e.CancelMode tells you if canceling an edit or insert
        }
    }
    ```

!!! note "Automatic EditIndex Management"
    The ListView automatically sets `EditIndex` to the `NewEditIndex` from `ListViewEditEventArgs` after `ItemEditing` fires (unless cancelled), and resets `EditIndex` to `-1` after `ItemCanceling` fires. You can still set `EditIndex` manually in your handler for custom behavior.

##### [Back to top](#listview)

#### Sorting Events

| Event | EventArgs | Description |
|-------|-----------|-------------|
| `Sorting` | `ListViewSortEventArgs` | Fires before a sort operation. Set `Cancel = true` to prevent sorting. |
| `Sorted` | `ListViewSortEventArgs` | Fires after the sort operation completes. |

**`ListViewSortEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `SortExpression` | `string` | The expression (typically a property name) to sort by. |
| `SortDirection` | `SortDirection` | The sort direction: `Ascending` or `Descending`. Automatically toggles. |
| `Cancel` | `bool` | Set to `true` to cancel the sort operation. |

**Web Forms → Blazor comparison:**

=== "Web Forms"

    ```html
    <asp:ListView ID="lv" runat="server"
        OnSorting="lv_Sorting"
        OnSorted="lv_Sorted">
    ```
    ```csharp
    protected void lv_Sorting(object sender, ListViewSortEventArgs e)
    {
        // e.SortExpression, e.SortDirection available
    }
    ```

=== "Blazor"

    ```razor
    <ListView @ref="listView" Context="Item" ItemType="Product"
        Sorting="OnSorting"
        Sorted="OnSorted">
    ```
    ```csharp
    @code {
        void OnSorting(ListViewSortEventArgs e)
        {
            // Sort direction toggles automatically between Ascending/Descending
            // Sort your data source using e.SortExpression and e.SortDirection
        }

        void OnSorted(ListViewSortEventArgs e)
        {
            statusMessage = $"Sorted by {e.SortExpression} ({e.SortDirection})";
        }
    }
    ```

!!! tip "Sort Direction Toggle"
    The ListView automatically toggles `SortDirection` between `Ascending` and `Descending` when the same `SortExpression` is used consecutively, matching Web Forms behavior.

##### [Back to top](#listview)

#### Paging Events

| Event | EventArgs | Description |
|-------|-----------|-------------|
| `PagePropertiesChanging` | `ListViewPagePropertiesChangingEventArgs` | Fires when page properties are about to change. |
| `PagePropertiesChanged` | `EventArgs` | Fires after the page properties have changed. |

**`ListViewPagePropertiesChangingEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `StartRowIndex` | `int` | The index of the first item on the new page. |
| `MaximumRows` | `int` | The maximum number of items to display per page. |

!!! note "Paging via SetPageProperties"
    Unlike the CRUD events which are triggered by `HandleCommand`, paging events fire when you call `listView.SetPageProperties(startRowIndex, maximumRows)`. This mirrors Web Forms' `DataPager` interaction pattern.

**Web Forms → Blazor comparison:**

=== "Web Forms"

    ```html
    <asp:ListView ID="lv" runat="server"
        OnPagePropertiesChanging="lv_PagePropertiesChanging"
        OnPagePropertiesChanged="lv_PagePropertiesChanged">
    ```
    ```csharp
    protected void lv_PagePropertiesChanging(object sender,
        PagePropertiesChangingEventArgs e)
    {
        // e.StartRowIndex, e.MaximumRows available
    }
    ```

=== "Blazor"

    ```razor
    <ListView @ref="listView" Context="Item" ItemType="Product"
        PagePropertiesChanging="OnPagePropertiesChanging"
        PagePropertiesChanged="OnPagePropertiesChanged">
    ```
    ```csharp
    @code {
        void OnPagePropertiesChanging(ListViewPagePropertiesChangingEventArgs e)
        {
            // e.StartRowIndex and e.MaximumRows reflect the new page
        }

        void OnPagePropertiesChanged()
        {
            // Page properties have been applied — refresh UI if needed
        }
    }
    ```

##### [Back to top](#listview)

#### Selection Events

| Event | EventArgs | Description |
|-------|-----------|-------------|
| `SelectedIndexChanging` | `ListViewSelectEventArgs` | Fires before the selected index changes. Set `Cancel = true` to prevent the change. |
| `SelectedIndexChanged` | `EventArgs` | Fires after the selected index has changed. |

**`ListViewSelectEventArgs` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `NewSelectedIndex` | `int` | The index of the newly selected item. |
| `Cancel` | `bool` | Set to `true` to cancel the selection change. |

**Web Forms → Blazor comparison:**

=== "Web Forms"

    ```html
    <asp:ListView ID="lv" runat="server"
        OnSelectedIndexChanging="lv_SelectedIndexChanging"
        OnSelectedIndexChanged="lv_SelectedIndexChanged">
    ```
    ```csharp
    protected void lv_SelectedIndexChanging(object sender, ListViewSelectEventArgs e)
    {
        // e.NewSelectedIndex identifies the item being selected
    }
    ```

=== "Blazor"

    ```razor
    <ListView @ref="listView" Context="Item" ItemType="Product"
        SelectedIndex="@selectedIndex"
        SelectedIndexChanging="OnSelectedIndexChanging"
        SelectedIndexChanged="OnSelectedIndexChanged">
    ```
    ```csharp
    @code {
        private int selectedIndex;

        void OnSelectedIndexChanging(ListViewSelectEventArgs e)
        {
            // Validate selection; set e.Cancel = true to prevent
        }

        void OnSelectedIndexChanged()
        {
            statusMessage = $"Selected item index: {selectedIndex}";
        }
    }
    ```

##### [Back to top](#listview)

#### Lifecycle Events

| Event | EventArgs | Description |
|-------|-----------|-------------|
| `OnLayoutCreated` | `EventArgs` | Fires after the layout template has been created. |
| `ItemCreated` | (none) | Fires when an item template is instantiated (after first render). |
| `OnItemDataBound` | `ListViewItemEventArgs` | Fires when an item is data-bound (existing event, listed here for completeness). |

These lifecycle events fire during the component's rendering lifecycle rather than in response to user commands.

**Web Forms → Blazor comparison:**

=== "Web Forms"

    ```html
    <asp:ListView ID="lv" runat="server"
        OnLayoutCreated="lv_LayoutCreated"
        OnItemCreated="lv_ItemCreated"
        OnItemDataBound="lv_ItemDataBound">
    ```

=== "Blazor"

    ```razor
    <ListView @ref="listView" Context="Item" ItemType="Product"
        OnLayoutCreated="HandleLayoutCreated"
        ItemCreated="HandleItemCreated"
        OnItemDataBound="HandleItemDataBound">
    ```
    ```csharp
    @code {
        void HandleLayoutCreated()
        {
            // Layout template has been rendered
        }

        void HandleItemCreated()
        {
            // Item template instantiated — runs once after first render
        }

        void HandleItemDataBound(ListViewItemEventArgs e)
        {
            if (e.Item.ItemType == ListViewItemType.DataItem)
            {
                var dataItem = (ListViewDataItem)e.Item;
                // Access dataItem.DataItem, dataItem.DisplayIndex, etc.
            }
        }
    }
    ```

##### [Back to top](#listview)

### Complete CRUD Example

This example demonstrates a full insert/update/delete workflow with all relevant events wired up:

```razor
<ListView @ref="listView" Context="Item" ItemType="Product"
          EditIndex="@editIndex"
          InsertItemPosition="InsertItemPosition.LastItem"
          ItemEditing="OnItemEditing"
          ItemUpdating="OnItemUpdating"
          ItemUpdated="OnItemUpdated"
          ItemCanceling="OnItemCanceling"
          ItemDeleting="OnItemDeleting"
          ItemDeleted="OnItemDeleted"
          ItemInserting="OnItemInserting"
          ItemInserted="OnItemInserted">
    <LayoutTemplate>
        <table class="table">
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Price</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>@context</tbody>
        </table>
    </LayoutTemplate>
    <ItemTemplate>
        <tr>
            <td>@Item.Name</td>
            <td>@Item.Price.ToString("C")</td>
            <td>
                <button @onclick="() => listView.HandleCommand("edit", null, Item.Id)">
                    Edit
                </button>
                <button @onclick="() => listView.HandleCommand("delete", null, Item.Id)">
                    Delete
                </button>
            </td>
        </tr>
    </ItemTemplate>
    <EditItemTemplate>
        <tr>
            <td><input @bind="editName" /></td>
            <td><input @bind="editPrice" type="number" step="0.01" /></td>
            <td>
                <button @onclick="() => listView.HandleCommand("update", null, editIndex)">
                    Update
                </button>
                <button @onclick="() => listView.HandleCommand("cancel", null, editIndex)">
                    Cancel
                </button>
            </td>
        </tr>
    </EditItemTemplate>
    <InsertItemTemplate>
        <tr>
            <td><input @bind="newName" placeholder="Product name" /></td>
            <td><input @bind="newPrice" type="number" step="0.01" placeholder="0.00" /></td>
            <td>
                <button @onclick="() => listView.HandleCommand("insert", null, 0)">
                    Insert
                </button>
            </td>
        </tr>
    </InsertItemTemplate>
    <EmptyDataTemplate>
        <p>No products found. Use the insert row to add one.</p>
    </EmptyDataTemplate>
</ListView>

@if (!string.IsNullOrEmpty(statusMessage))
{
    <div class="alert alert-info">@statusMessage</div>
}

@code {
    private ListView<Product> listView;
    private List<Product> products = new();
    private int editIndex = -1;
    private string editName, newName, statusMessage;
    private decimal editPrice, newPrice;

    void OnItemEditing(ListViewEditEventArgs e)
    {
        var product = products.First(p => p.Id == e.NewEditIndex);
        editName = product.Name;
        editPrice = product.Price;
        editIndex = e.NewEditIndex;
    }

    void OnItemUpdating(ListViewUpdateEventArgs e)
    {
        var product = products.First(p => p.Id == e.ItemIndex);
        product.Name = editName;
        product.Price = editPrice;
    }

    void OnItemUpdated(ListViewUpdatedEventArgs e)
    {
        statusMessage = "Product updated successfully.";
    }

    void OnItemCanceling(ListViewCancelEventArgs e)
    {
        editIndex = -1;
    }

    void OnItemDeleting(ListViewDeleteEventArgs e)
    {
        products.RemoveAll(p => p.Id == e.ItemIndex);
    }

    void OnItemDeleted(ListViewDeletedEventArgs e)
    {
        statusMessage = "Product deleted.";
    }

    void OnItemInserting(ListViewInsertEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            e.Cancel = true;
            statusMessage = "Name is required.";
            return;
        }
        products.Add(new Product
        {
            Id = products.Any() ? products.Max(p => p.Id) + 1 : 1,
            Name = newName,
            Price = newPrice
        });
        newName = "";
        newPrice = 0;
    }

    void OnItemInserted(ListViewInsertedEventArgs e)
    {
        statusMessage = "Product inserted.";
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}
```

## See Also

- [FormView](FormView.md) — Similar CRUD event model for single-record views
- [DetailsView](DetailsView.md) — Single-record display with edit support
- [GridView](GridView.md) — Tabular data display
- [DataPager](DataPager.md) — Paging companion for ListView
- [Microsoft Docs: ListView Class](https://docs.microsoft.com/en-us/dotnet/api/system.web.ui.webcontrols.listview?view=netframework-4.8)